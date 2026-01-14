// <copyright file="DbConnectionExtensions.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx;

using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Extension methods for DbConnection.
/// </summary>
public static class DbConnectionExtensions
{
    /// <summary>
    /// Batch executes a SQL command for multiple entities.
    /// Uses loop execution (reflection-free).
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="connection">The database connection.</param>
    /// <param name="sql">The SQL command text.</param>
    /// <param name="entities">The entities to process.</param>
    /// <param name="binder">The parameter binder.</param>
    /// <param name="transaction">Optional transaction.</param>
    /// <param name="parameterPrefix">The parameter prefix (default: @).</param>
    /// <param name="batchSize">Max commands per batch (default: 1000).</param>
    /// <param name="commandTimeout">Command timeout in seconds (null = use default).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Total rows affected.</returns>
    public static Task<int> ExecuteBatchAsync<TEntity>(
        this DbConnection connection,
        string sql,
        List<TEntity> entities,
        IParameterBinder<TEntity> binder,
        DbTransaction? transaction = null,
        string parameterPrefix = "@",
        int batchSize = DbBatchExecutor.DefaultBatchSize,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
    {
        return DbBatchExecutor.ExecuteAsync(
            connection,
            transaction,
            sql,
            entities,
            binder,
            parameterPrefix,
            batchSize,
            commandTimeout,
            cancellationToken);
    }

#if NET6_0_OR_GREATER
    /// <summary>
    /// Batch executes a SQL command for multiple entities using DbBatch.
    /// Uses typed parameter factory (reflection-free).
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TParameter">The DbParameter type for the database provider.</typeparam>
    /// <param name="connection">The database connection.</param>
    /// <param name="sql">The SQL command text.</param>
    /// <param name="entities">The entities to process.</param>
    /// <param name="binder">The parameter binder.</param>
    /// <param name="transaction">Optional transaction.</param>
    /// <param name="parameterPrefix">The parameter prefix (default: @).</param>
    /// <param name="batchSize">Max commands per batch (default: 1000).</param>
    /// <param name="commandTimeout">Command timeout in seconds (null = use default).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Total rows affected.</returns>
    public static Task<int> ExecuteBatchAsync<TEntity, TParameter>(
        this DbConnection connection,
        string sql,
        List<TEntity> entities,
        IParameterBinder<TEntity> binder,
        DbTransaction? transaction = null,
        string parameterPrefix = "@",
        int batchSize = DbBatchExecutor.DefaultBatchSize,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
        where TParameter : DbParameter, new()
    {
        return DbBatchExecutor.ExecuteAsync<TEntity, TParameter>(
            connection,
            transaction,
            sql,
            entities,
            binder,
            parameterPrefix,
            batchSize,
            commandTimeout,
            cancellationToken);
    }
#endif
}
