// <copyright file="DbBatchExecutor.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx;

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Batch executor for multiple entities.
/// Uses DbBatch on .NET 6+ with fallback to loop execution on older frameworks.
/// </summary>
public static class DbBatchExecutor
{
    /// <summary>
    /// Default batch size for batch operations.
    /// </summary>
    public const int DefaultBatchSize = 1000;

    /// <summary>
    /// Batch executes a SQL command for multiple entities.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="connection">The database connection.</param>
    /// <param name="transaction">Optional transaction.</param>
    /// <param name="sql">The SQL command text.</param>
    /// <param name="entities">The entities to process.</param>
    /// <param name="binder">The parameter binder.</param>
    /// <param name="parameterPrefix">The parameter prefix (default: @).</param>
    /// <param name="batchSize">Max commands per batch (default: 1000).</param>
    /// <param name="commandTimeout">Command timeout in seconds (null = use default).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Total rows affected.</returns>
    public static async Task<int> ExecuteAsync<TEntity>(
        DbConnection connection,
        DbTransaction? transaction,
        string sql,
        List<TEntity> entities,
        IParameterBinder<TEntity> binder,
        string parameterPrefix = "@",
        int batchSize = DefaultBatchSize,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
    {
        if (entities == null || entities.Count == 0) return 0;
        if (batchSize <= 0) batchSize = DefaultBatchSize;

        // Always use loop execution (DbBatch requires parameter factory which needs reflection)
        // This is the reflection-free path
        var total = 0;
        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Transaction = transaction;
        if (commandTimeout.HasValue) cmd.CommandTimeout = commandTimeout.Value;

        foreach (var entity in entities)
        {
            cmd.Parameters.Clear();
            binder.BindEntity(cmd, entity, parameterPrefix);
            total += await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        return total;
    }

#if NET6_0_OR_GREATER
    /// <summary>
    /// Batch executes a SQL command for multiple entities using DbBatch.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TParameter">The DbParameter type.</typeparam>
    /// <param name="connection">The database connection.</param>
    /// <param name="transaction">Optional transaction.</param>
    /// <param name="sql">The SQL command text.</param>
    /// <param name="entities">The entities to process.</param>
    /// <param name="binder">The parameter binder.</param>
    /// <param name="parameterPrefix">The parameter prefix (default: @).</param>
    /// <param name="batchSize">Max commands per batch (default: 1000).</param>
    /// <param name="commandTimeout">Command timeout in seconds (null = use default).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Total rows affected.</returns>
    public static async Task<int> ExecuteAsync<TEntity, TParameter>(
        DbConnection connection,
        DbTransaction? transaction,
        string sql,
        List<TEntity> entities,
        IParameterBinder<TEntity> binder,
        string parameterPrefix = "@",
        int batchSize = DefaultBatchSize,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
        where TParameter : DbParameter, new()
    {
        if (entities == null || entities.Count == 0) return 0;
        if (batchSize <= 0) batchSize = DefaultBatchSize;

        var total = 0;

        if (connection.CanCreateBatch)
        {
            // Use DbBatch with typed parameter factory (no reflection)
            Func<DbParameter> parameterFactory = static () => new TParameter();

            for (var i = 0; i < entities.Count; i += batchSize)
            {
                using var batch = connection.CreateBatch();
                batch.Transaction = transaction;
                if (commandTimeout.HasValue) batch.Timeout = commandTimeout.Value;

                var end = Math.Min(i + batchSize, entities.Count);
                for (var j = i; j < end; j++)
                {
                    var batchCmd = batch.CreateBatchCommand();
                    batchCmd.CommandText = sql;
                    binder.BindEntity(batchCmd, entities[j], parameterFactory, parameterPrefix);
                    batch.BatchCommands.Add(batchCmd);
                }

                total += await batch.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
            return total;
        }

        // Fallback: loop execution
        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Transaction = transaction;
        if (commandTimeout.HasValue) cmd.CommandTimeout = commandTimeout.Value;

        foreach (var entity in entities)
        {
            cmd.Parameters.Clear();
            binder.BindEntity(cmd, entity, parameterPrefix);
            total += await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        return total;
    }
#endif
}
