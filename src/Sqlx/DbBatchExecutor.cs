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

        var total = 0;
        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Transaction = transaction;
        if (commandTimeout.HasValue) cmd.CommandTimeout = commandTimeout.Value;

        for (var i = 0; i < entities.Count; i++)
        {
            cmd.Parameters.Clear();
            binder.BindEntity(cmd, entities[i], parameterPrefix);
            total += await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }
        return total;
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

        // Fallback to loop if batch not supported
        if (!connection.CanCreateBatch)
        {
            var total = 0;
            using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.Transaction = transaction;
            if (commandTimeout.HasValue) cmd.CommandTimeout = commandTimeout.Value;

            for (var i = 0; i < entities.Count; i++)
            {
                cmd.Parameters.Clear();
                binder.BindEntity(cmd, entities[i], parameterPrefix);
                total += await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            }
            return total;
        }

        // Use DbBatch with typed parameter factory
        var size = batchSize > 0 ? batchSize : DefaultBatchSize;
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
#endif
}
