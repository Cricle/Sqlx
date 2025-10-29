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
    /// Standard CRUD repository interface combining query, command, and aggregate operations.
    /// </summary>
    /// <typeparam name="TEntity">Entity type</typeparam>
    /// <typeparam name="TKey">Primary key type (int, long, Guid, etc.)</typeparam>
    /// <remarks>
    /// This interface combines IQueryRepository, ICommandRepository, and IAggregateRepository
    /// for standard CRUD scenarios. For more operations, use IRepository or individual interfaces.
    /// All queries use explicit column names (no SELECT *), parameterized queries for SQL injection prevention,
    /// and support CancellationToken for operation cancellation.
    /// </remarks>
    public interface ICrudRepository<TEntity, TKey> :
        IQueryRepository<TEntity, TKey>,
        ICommandRepository<TEntity, TKey>,
        IAggregateRepository<TEntity, TKey>
        where TEntity : class
    {
        // This interface inherits all methods from:
        // - IQueryRepository: GetById, GetAll, GetWhere, GetPage, Exists, etc.
        // - ICommandRepository: Insert, Update, Delete, SoftDelete, Upsert, etc.
        // - IAggregateRepository: Count, Sum, Avg, Max, Min, etc.

        // Legacy methods below for backward compatibility (v0.4)
        // New code should use the inherited methods instead
        /// <summary>Gets entity by primary key.</summary>
        /// <param name="id">Primary key value</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Entity or null if not found</returns>
        /// <remarks>
        /// Generated SQL: SELECT id, name, ... FROM table WHERE id = @id
        /// Uses primary key index for optimal performance.
        /// </remarks>
        [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
        Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);

        /// <summary>Gets all entities with pagination.</summary>
        /// <param name="limit">Max rows to return (default 100)</param>
        /// <param name="offset">Rows to skip (for pagination)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of entities</returns>
        /// <remarks>
        /// Generated SQL: SELECT id, name, ... FROM table ORDER BY id LIMIT @limit OFFSET @offset
        /// Always uses LIMIT to prevent loading too much data. Ordered by primary key for stable results.
        /// </remarks>
        [SqlTemplate("SELECT {{columns}} FROM {{table}} ORDER BY id {{limit --param limit}} {{offset --param offset}}")]
        Task<List<TEntity>> GetAllAsync(int limit = 100, int offset = 0, CancellationToken cancellationToken = default);

        /// <summary>Inserts new entity.</summary>
        /// <param name="entity">Entity to insert</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Rows affected (1 on success)</returns>
        /// <remarks>
        /// Generated SQL: INSERT INTO table (name, email, ...) VALUES (@name, @email, ...)
        /// Excludes Id column (assumed to be auto-increment primary key).
        /// </remarks>
        [SqlTemplate("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})")]
        Task<int> InsertAsync(TEntity entity, CancellationToken cancellationToken = default);

        /// <summary>Updates entity by primary key.</summary>
        /// <param name="entity">Entity to update (must contain Id)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Rows affected (1 if found, 0 if not found)</returns>
        /// <remarks>
        /// Generated SQL: UPDATE table SET name = @name, ... WHERE id = @id
        /// Excludes Id column (primary key should not be updated).
        /// </remarks>
        [SqlTemplate("UPDATE {{table}} SET {{set --exclude Id}} WHERE id = @id")]
        Task<int> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

        /// <summary>Deletes entity by primary key.</summary>
        /// <param name="id">Primary key of entity to delete</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Rows affected (1 if found, 0 if not found)</returns>
        /// <remarks>
        /// Generated SQL: DELETE FROM table WHERE id = @id
        /// This is a physical delete and cannot be undone. Consider soft delete with is_deleted flag.
        /// </remarks>
        [SqlTemplate("DELETE FROM {{table}} WHERE id = @id")]
        Task<int> DeleteAsync(TKey id, CancellationToken cancellationToken = default);

        /// <summary>Gets total count of entities.</summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Total row count</returns>
        /// <remarks>
        /// Generated SQL: SELECT COUNT(*) FROM table
        /// For large tables, consider using approximations.
        /// </remarks>
        [SqlTemplate("SELECT COUNT(*) FROM {{table}}")]
        Task<int> CountAsync(CancellationToken cancellationToken = default);

        /// <summary>Checks if entity exists.</summary>
        /// <param name="id">Primary key value</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if exists, false otherwise</returns>
        /// <remarks>
        /// Generated SQL: SELECT CASE WHEN EXISTS(SELECT 1 FROM table WHERE id = @id) THEN 1 ELSE 0 END
        /// Uses EXISTS for better performance than COUNT(*).
        /// </remarks>
        [SqlTemplate("SELECT CASE WHEN EXISTS(SELECT 1 FROM {{table}} WHERE id = @id) THEN 1 ELSE 0 END")]
        Task<bool> ExistsAsync(TKey id, CancellationToken cancellationToken = default);

        /// <summary>Batch inserts multiple entities (high performance).</summary>
        /// <param name="entities">Entities to insert</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Rows affected</returns>
        /// <remarks>
        /// Generated SQL: INSERT INTO table (name, ...) VALUES (@name_0, ...), (@name_1, ...), (@name_2, ...)
        /// 10-50x faster than looping InsertAsync. Be aware of database parameter limits (typically 2100).
        /// </remarks>
        [SqlTemplate("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES {{batch_values --exclude Id}}")]
        [BatchOperation]
        Task<int> BatchInsertAsync(List<TEntity> entities, CancellationToken cancellationToken = default);
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
        IAggregateRepository<TEntity, TKey>,
        IAdvancedRepository<TEntity, TKey>
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
