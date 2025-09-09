// -----------------------------------------------------------------------
// <copyright file="BaseRepository.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Sqlx.RepositoryExample.Core;

/// <summary>
/// High-performance base repository with optimized database operations.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TKey">The primary key type.</typeparam>
public abstract class BaseRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
    where TKey : IEquatable<TKey>
{
    /// <summary>
    /// The database connection.
    /// </summary>
    protected readonly DbConnection connection;
    private readonly SemaphoreSlim connectionSemaphore = new(1, 1);

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseRepository{TEntity, TKey}"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    protected BaseRepository(DbConnection connection)
    {
        this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    /// <summary>
    /// Ensures the database connection is open.
    /// </summary>
    protected async Task EnsureConnectionOpenAsync(CancellationToken cancellationToken = default)
    {
        if (connection.State == System.Data.ConnectionState.Open) return;

        await connectionSemaphore.WaitAsync(cancellationToken);
        try
        {
            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync(cancellationToken);
            }
        }
        finally
        {
            connectionSemaphore.Release();
        }
    }

    /// <summary>
    /// Creates and configures a database command.
    /// </summary>
    protected DbCommand CreateCommand(string sql, params (string name, object? value)[] parameters)
    {
        var command = connection.CreateCommand();
        command.CommandText = sql;

        foreach (var (name, value) in parameters)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name.StartsWith("@") ? name : $"@{name}";
            parameter.Value = value ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }

        return command;
    }

    /// <summary>
    /// Executes a query and maps results to entities.
    /// </summary>
    protected async Task<List<TEntity>> QueryAsync<T>(string sql, Func<DbDataReader, T> mapper, CancellationToken cancellationToken = default, params (string name, object? value)[] parameters)
        where T : class
    {
        await EnsureConnectionOpenAsync(cancellationToken);
        
        using var command = CreateCommand(sql, parameters);
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        
        var results = new List<T>();
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(mapper(reader));
        }
        
        return results as List<TEntity> ?? new List<TEntity>();
    }

    /// <summary>
    /// Executes a query and returns the first result or null.
    /// </summary>
    protected async Task<TEntity?> QueryFirstOrDefaultAsync(string sql, Func<DbDataReader, TEntity> mapper, CancellationToken cancellationToken = default, params (string name, object? value)[] parameters)
    {
        await EnsureConnectionOpenAsync(cancellationToken);
        
        using var command = CreateCommand(sql, parameters);
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        
        return await reader.ReadAsync(cancellationToken) ? mapper(reader) : null;
    }

    /// <summary>
    /// Executes a non-query command (INSERT, UPDATE, DELETE).
    /// </summary>
    protected async Task<int> ExecuteAsync(string sql, CancellationToken cancellationToken = default, params (string name, object? value)[] parameters)
    {
        await EnsureConnectionOpenAsync(cancellationToken);
        
        using var command = CreateCommand(sql, parameters);
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// Executes a scalar query.
    /// </summary>
    protected async Task<T?> ExecuteScalarAsync<T>(string sql, CancellationToken cancellationToken = default, params (string name, object? value)[] parameters)
    {
        await EnsureConnectionOpenAsync(cancellationToken);
        
        using var command = CreateCommand(sql, parameters);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        
        return result is T typedResult ? typedResult : default;
    }

    /// <summary>
    /// Begins a database transaction.
    /// </summary>
    protected async Task<DbTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        await EnsureConnectionOpenAsync(cancellationToken);
        return await connection.BeginTransactionAsync(cancellationToken);
    }

    /// <summary>
    /// Disposes the repository and its resources.
    /// </summary>
    public virtual void Dispose()
    {
        connectionSemaphore?.Dispose();
        connection?.Dispose();
    }
}

/// <summary>
/// Interface for entities with a primary key.
/// </summary>
/// <typeparam name="TKey">The primary key type.</typeparam>
public interface IEntity<TKey> where TKey : IEquatable<TKey>
{
    /// <summary>
    /// Gets or sets the entity's primary key.
    /// </summary>
    TKey Id { get; set; }
}
