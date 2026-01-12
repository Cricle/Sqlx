// -----------------------------------------------------------------------
// <copyright file="ICrudRepository.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Sqlx
{
    /// <summary>
    /// Standard CRUD repository interface combining query and command operations.
    /// </summary>
    /// <typeparam name="TEntity">Entity type</typeparam>
    /// <typeparam name="TKey">Primary key type (int, long, Guid, etc.)</typeparam>
    /// <remarks>
    /// This interface combines IQueryRepository and ICommandRepository
    /// for standard CRUD scenarios. For aggregate operations, use IAggregateRepository separately.
    /// For all operations combined, use IRepository interface.
    /// All queries use explicit column names (no SELECT *), parameterized queries for SQL injection prevention,
    /// and support CancellationToken for operation cancellation.
    /// </remarks>
    public interface ICrudRepository<TEntity, TKey> :
        IQueryRepository<TEntity, TKey>,
        ICommandRepository<TEntity, TKey>
        where TEntity : class
    {
        // This interface inherits all methods from:
        // - IQueryRepository: GetById, GetAll, GetWhere, GetPage, Exists, etc.
        // - ICommandRepository: Insert, Update, Delete, SoftDelete, Upsert, etc.
        //
        // For aggregate operations (Sum, Avg, Max, Min), use IAggregateRepository
        // For batch operations, use IBatchRepository

        // ===== Common Aggregate (for convenience) =====

        /// <summary>Gets total count of entities.</summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Total row count</returns>
        /// <remarks>
        /// This is a convenience method also available in IAggregateRepository.
        /// Generated SQL: SELECT COUNT(*) FROM table
        /// </remarks>
        [SqlTemplate("SELECT COUNT(*) FROM {{table}}")]
        Task<long> CountAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Complete repository interface with all operations (50+ methods).
    /// </summary>
    /// <typeparam name="TEntity">Entity type</typeparam>
    /// <typeparam name="TKey">Primary key type</typeparam>
    public interface IRepository<TEntity, TKey> :
        IQueryRepository<TEntity, TKey>,
        ICommandRepository<TEntity, TKey>,
        IBatchRepository<TEntity, TKey>,
        IAggregateRepository<TEntity, TKey>
        where TEntity : class
    {
        // This interface inherits all methods from all repository interfaces
    }

    /// <summary>
    /// Read-only repository interface - contains only query and aggregate operations.
    /// Suitable for reporting, data display, read replicas, and CQRS query models.
    /// </summary>
    /// <typeparam name="TEntity">Entity type</typeparam>
    /// <typeparam name="TKey">Primary key type</typeparam>
    public interface IReadOnlyRepository<TEntity, TKey> :
        IQueryRepository<TEntity, TKey>,
        IAggregateRepository<TEntity, TKey>
        where TEntity : class
    {
        // This interface inherits methods from:
        // - IQueryRepository: GetById, GetAll, GetWhere, GetPage, Exists, etc.
        // - IAggregateRepository: Count, Sum, Avg, Max, Min, etc.
    }

    /// <summary>
    /// Bulk operations repository interface for high-performance data manipulation.
    /// </summary>
    /// <typeparam name="TEntity">Entity type</typeparam>
    /// <typeparam name="TKey">Primary key type</typeparam>
    public interface IBulkRepository<TEntity, TKey> :
        IQueryRepository<TEntity, TKey>,
        IBatchRepository<TEntity, TKey>
        where TEntity : class
    {
        // This interface inherits methods from:
        // - IQueryRepository: For reading data
        // - IBatchRepository: For bulk insert/update/delete operations
    }

    /// <summary>
    /// Write-only repository interface for command side in CQRS pattern.
    /// </summary>
    /// <typeparam name="TEntity">Entity type</typeparam>
    /// <typeparam name="TKey">Primary key type</typeparam>
    public interface IWriteOnlyRepository<TEntity, TKey> :
        ICommandRepository<TEntity, TKey>,
        IBatchRepository<TEntity, TKey>
        where TEntity : class
    {
        // This interface inherits methods from:
        // - ICommandRepository: For single entity operations
        // - IBatchRepository: For bulk operations
    }
}
