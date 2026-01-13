// <copyright file="PreparedCommandCache.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx;

using System;
using System.Data;
using System.Data.Common;

/// <summary>
/// Caches pre-created DbCommand with parameters for high-performance scenarios.
/// Thread-safe for single connection usage.
/// </summary>
/// <remarks>
/// Usage:
/// <code>
/// // Create once
/// var cache = new PreparedCommandCache(connection, "SELECT * FROM users WHERE id = @id", "@id");
/// 
/// // Reuse many times
/// cache.SetParam(0, userId);
/// using var reader = await cache.Command.ExecuteReaderAsync();
/// </code>
/// </remarks>
public sealed class PreparedCommandCache : IDisposable
{
    private readonly DbCommand _command;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance with parameter names.
    /// </summary>
    public PreparedCommandCache(DbConnection connection, string sql, params string[] parameterNames)
    {
        _command = connection.CreateCommand();
        _command.CommandText = sql;

        foreach (var name in parameterNames)
        {
            var param = _command.CreateParameter();
            param.ParameterName = name;
            _command.Parameters.Add(param);
        }
    }

    /// <summary>
    /// Initializes a new instance with typed parameters.
    /// </summary>
    public PreparedCommandCache(DbConnection connection, string sql, params (string Name, DbType Type)[] parameters)
    {
        _command = connection.CreateCommand();
        _command.CommandText = sql;

        foreach (var (name, type) in parameters)
        {
            var param = _command.CreateParameter();
            param.ParameterName = name;
            param.DbType = type;
            _command.Parameters.Add(param);
        }
    }

    /// <summary>
    /// Gets the underlying command.
    /// </summary>
    public DbCommand Command => _command;

    /// <summary>
    /// Sets parameter value by index (fastest).
    /// </summary>
    public void SetParam(int index, object? value)
    {
        _command.Parameters[index].Value = value ?? DBNull.Value;
    }

    /// <summary>
    /// Sets parameter value by name.
    /// </summary>
    public void SetParam(string name, object? value)
    {
        _command.Parameters[name].Value = value ?? DBNull.Value;
    }

    /// <summary>
    /// Sets multiple parameter values by index.
    /// </summary>
    public void SetParams(params object?[] values)
    {
        for (int i = 0; i < values.Length; i++)
        {
            _command.Parameters[i].Value = values[i] ?? DBNull.Value;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!_disposed)
        {
            _command.Dispose();
            _disposed = true;
        }
    }
}
