// <copyright file="DbBatchExecutor.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx;

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Batch executor for multiple entities (AOT-friendly, reflection-free).
/// </summary>
public static class DbBatchExecutor
{
    /// <summary>Default batch size.</summary>
    public const int DefaultBatchSize = 1000;

    /// <summary>Batch executes SQL for multiple entities using loop execution.</summary>
    public static async Task<int> ExecuteAsync<TEntity>(
        DbConnection connection,
        DbTransaction? transaction,
        string sql,
        IReadOnlyList<TEntity> entities,
        IParameterBinder<TEntity> binder,
        string parameterPrefix = "@",
        int batchSize = DefaultBatchSize,
        int? commandTimeout = null,
        CancellationToken ct = default)
    {
        if (entities == null || entities.Count == 0) return 0;

        return await ExecuteLoopAsync(
            connection,
            transaction,
            sql,
            entities,
            binder,
            parameterPrefix,
            batchSize,
            commandTimeout,
            ct).ConfigureAwait(false);
    }

#if NET6_0_OR_GREATER
    /// <summary>Batch executes SQL using DbBatch with typed parameter factory (best performance).</summary>
    public static async Task<int> ExecuteAsync<TEntity, TParameter>(
        DbConnection connection,
        DbTransaction? transaction,
        string sql,
        IReadOnlyList<TEntity> entities,
        IParameterBinder<TEntity> binder,
        string parameterPrefix = "@",
        int batchSize = DefaultBatchSize,
        int? commandTimeout = null,
        CancellationToken ct = default)
        where TParameter : DbParameter, new()
    {
        if (entities == null || entities.Count == 0) return 0;

        if (!connection.CanCreateBatch)
        {
            return await ExecuteAsync(
                connection,
                transaction,
                sql,
                entities,
                binder,
                parameterPrefix,
                batchSize,
                commandTimeout,
                ct).ConfigureAwait(false);
        }

        var size = batchSize > 0 ? batchSize : DefaultBatchSize;
        var shouldCloseConnection = await EnsureConnectionOpenAsync(connection, ct).ConfigureAwait(false);

        try
        {
            // Use DbBatch with typed parameter factory
            var result = 0;
            static DbParameter Factory() => new TParameter();

            for (var i = 0; i < entities.Count; i += size)
            {
                using var batch = connection.CreateBatch();
                batch.Transaction = transaction;
                if (commandTimeout.HasValue) batch.Timeout = commandTimeout.Value;

                var end = Math.Min(i + size, entities.Count);
                for (var j = i; j < end; j++)
                {
                    var cmd = batch.CreateBatchCommand();
                    cmd.CommandText = sql;
                    binder.BindEntity(cmd, entities[j], Factory, parameterPrefix);
                    batch.BatchCommands.Add(cmd);
                }
                result += await batch.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            }

            return result;
        }
        finally
        {
            CloseConnection(connection, transaction, shouldCloseConnection);
        }
    }
#endif

    private static async Task<int> ExecuteLoopAsync<TEntity>(
        DbConnection connection,
        DbTransaction? transaction,
        string sql,
        IReadOnlyList<TEntity> entities,
        IParameterBinder<TEntity> binder,
        string parameterPrefix,
        int batchSize,
        int? commandTimeout,
        CancellationToken ct)
    {
        var size = batchSize > 0 ? batchSize : DefaultBatchSize;
        var shouldCloseConnection = await EnsureConnectionOpenAsync(connection, ct).ConfigureAwait(false);

        try
        {
            var total = 0;
            using var cmd = CreateCommand(connection, transaction, sql, commandTimeout);

            for (var i = 0; i < entities.Count; i += size)
            {
                var end = Math.Min(i + size, entities.Count);
                for (var j = i; j < end; j++)
                {
                    cmd.Parameters.Clear();
                    binder.BindEntity(cmd, entities[j], parameterPrefix);
                    total += await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                }
            }

            return total;
        }
        finally
        {
            CloseConnection(connection, transaction, shouldCloseConnection);
        }
    }

    private static async Task<bool> EnsureConnectionOpenAsync(DbConnection connection, CancellationToken ct)
    {
        if (connection.State == ConnectionState.Open)
        {
            return false;
        }

        await connection.OpenAsync(ct).ConfigureAwait(false);
        return true;
    }

    private static DbCommand CreateCommand(
        DbConnection connection,
        DbTransaction? transaction,
        string sql,
        int? commandTimeout)
    {
        var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Transaction = transaction;
        if (commandTimeout.HasValue) command.CommandTimeout = commandTimeout.Value;
        return command;
    }

    private static void CloseConnection(DbConnection connection, DbTransaction? transaction, bool shouldCloseConnection)
    {
        if (shouldCloseConnection && transaction == null && connection.State != ConnectionState.Closed) connection.Close();
    }
}
