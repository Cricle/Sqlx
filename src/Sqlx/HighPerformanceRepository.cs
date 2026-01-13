// <copyright file="HighPerformanceRepository.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx;

using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// High-performance repository base class with pre-created commands and parameters.
/// Reuses DbCommand and DbParameter objects to minimize allocations.
/// </summary>
/// <remarks>
/// This pattern works with all ADO.NET providers:
/// - SQLite (Microsoft.Data.Sqlite)
/// - MySQL (MySqlConnector)
/// - PostgreSQL (Npgsql) - also supports Prepare() for even better performance
/// - SQL Server (Microsoft.Data.SqlClient)
/// - Oracle (Oracle.ManagedDataAccess)
/// </remarks>
public abstract class HighPerformanceRepository<TConnection> : IDisposable
    where TConnection : DbConnection
{
    private readonly TConnection _connection;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="HighPerformanceRepository{TConnection}"/> class.
    /// </summary>
    protected HighPerformanceRepository(TConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    /// <summary>
    /// Gets the database connection.
    /// </summary>
    protected TConnection Connection => _connection;

    /// <summary>
    /// Creates a pre-configured command with parameters.
    /// </summary>
    protected DbCommand CreateCommand(string sql, params string[] parameterNames)
    {
        var cmd = _connection.CreateCommand();
        cmd.CommandText = sql;

        foreach (var name in parameterNames)
        {
            var param = cmd.CreateParameter();
            param.ParameterName = name;
            cmd.Parameters.Add(param);
        }

        return cmd;
    }

    /// <summary>
    /// Creates a pre-configured command with typed parameters.
    /// </summary>
    protected DbCommand CreateCommand(string sql, params (string Name, DbType Type)[] parameters)
    {
        var cmd = _connection.CreateCommand();
        cmd.CommandText = sql;

        foreach (var (name, type) in parameters)
        {
            var param = cmd.CreateParameter();
            param.ParameterName = name;
            param.DbType = type;
            cmd.Parameters.Add(param);
        }

        return cmd;
    }

    /// <summary>
    /// Sets parameter value by index (fastest).
    /// </summary>
    protected static void SetParam(DbCommand cmd, int index, object? value)
    {
        cmd.Parameters[index].Value = value ?? DBNull.Value;
    }

    /// <summary>
    /// Sets parameter value by name.
    /// </summary>
    protected static void SetParam(DbCommand cmd, string name, object? value)
    {
        cmd.Parameters[name].Value = value ?? DBNull.Value;
    }

    /// <summary>
    /// Executes a query and returns a single entity.
    /// </summary>
    protected async Task<T?> QuerySingleAsync<T>(DbCommand cmd, Func<DbDataReader, T> reader, CancellationToken ct = default)
    {
        using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        if (await r.ReadAsync(ct).ConfigureAwait(false))
        {
            return reader(r);
        }
        return default;
    }

    /// <summary>
    /// Executes a query and returns a scalar value.
    /// </summary>
    protected async Task<T> ExecuteScalarAsync<T>(DbCommand cmd, CancellationToken ct = default)
    {
        var result = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
        return (T)result!;
    }

    /// <summary>
    /// Executes a non-query command.
    /// </summary>
    protected async Task<int> ExecuteNonQueryAsync(DbCommand cmd, CancellationToken ct = default)
    {
        return await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes managed resources.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            // Subclasses should dispose their pre-created commands here
            _disposed = true;
        }
    }
}
