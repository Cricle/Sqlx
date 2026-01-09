// -----------------------------------------------------------------------
// <copyright file="IBatchRepository.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Sqlx
{
    /// <summary>
    /// Batch repository interface for high-performance bulk data operations.
    /// </summary>
    /// <typeparam name="TEntity">Entity type</typeparam>
    /// <typeparam name="TKey">Primary key type</typeparam>
    public interface IBatchRepository<TEntity, TKey>
        where TEntity : class
    {
        // ===== Batch Insert =====

        /// <summary>Batch inserts multiple entities (10-50x faster than looping InsertAsync).</summary>
        /// <param name="entities">List of entities to insert</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Total rows affected</returns>
        /// <remarks>
        /// Generated SQL: INSERT INTO table (col1, col2) VALUES (@col1_0, @col2_0), (@col1_1, @col2_1), ...
        /// Automatically handles database parameter limits (typically 2100 for SQL Server).
        /// </remarks>
        [SqlTemplate("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES {{batch_values --exclude Id}}")]
        [BatchOperation(MaxBatchSize = 1000)]
        Task<int> BatchInsertAsync(IList<TEntity> entities, CancellationToken cancellationToken = default);

        /// <summary>Batch inserts and returns all generated primary keys.</summary>
        /// <param name="entities">List of entities to insert</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of generated primary key values (in same order as input)</returns>
        /// <remarks>
        /// This method inserts multiple entities and returns their generated primary keys.
        /// The order of returned IDs matches the order of input entities.
        /// 
        /// Performance: 10-50x faster than looping InsertAndGetIdAsync.
        /// 
        /// Database-specific implementation:
        /// - SQLite: Uses last_insert_rowid() and calculates ID range
        /// - MySQL: Uses LAST_INSERT_ID() and calculates ID range
        /// - SQL Server: Uses SCOPE_IDENTITY() and calculates ID range
        /// - PostgreSQL: Uses RETURNING clause (future enhancement)
        /// 
        /// Note: This assumes auto-increment IDs are sequential.
        /// </remarks>
        [SqlTemplate("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES {{batch_values --exclude Id}}")]
        [BatchOperation(MaxBatchSize = 1000)]
        [ReturnInsertedId]
        Task<List<TKey>> BatchInsertAndGetIdsAsync(IList<TEntity> entities, CancellationToken cancellationToken = default);

        // ===== Batch Update =====

        /// <summary>Batch updates multiple entities by their primary keys.</summary>
        /// <param name="entities">List of entities to update (must contain Ids)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Total rows affected</returns>
        /// <remarks>
        /// Implementation varies by database:
        /// - Single UPDATE with CASE: UPDATE SET col = CASE WHEN id=1 THEN val1 WHEN id=2 THEN val2 END
        /// - Multiple UPDATE statements in transaction
        /// </remarks>
        [SqlTemplate("{{batch_update}}")]
        [BatchOperation(MaxBatchSize = 500)]
        Task<int> BatchUpdateAsync(IList<TEntity> entities, CancellationToken cancellationToken = default);

        /// <summary>Updates all entities matching condition with same values.</summary>
        /// <typeparam name="TUpdates">Type containing the properties to update (can be anonymous type or named type)</typeparam>
        /// <param name="predicate">Expression predicate for WHERE clause</param>
        /// <param name="updates">Object with column-value pairs to update (properties are analyzed at compile time)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Rows affected</returns>
        /// <remarks>
        /// <para>
        /// ⚠️ AOT LIMITATION: This method uses method-level generics which cannot be fully analyzed
        /// at compile time by the source generator. For full AOT compatibility, use one of these alternatives:
        /// </para>
        /// <para>
        /// 1. <see cref="IPartialUpdateRepository{TEntity, TKey, TUpdates}"/> - Interface-level generics
        /// 2. <see cref="IExpressionUpdateRepository{TEntity, TKey}"/> - Expression-based updates
        /// </para>
        /// </remarks>
        /// <example>
        /// // For AOT compatibility, prefer IExpressionUpdateRepository:
        /// await repo.UpdateFieldsWhereAsync(
        ///     x =&gt; x.Status == "Pending" &amp;&amp; x.CreatedAt &lt; DateTime.Now.AddDays(-7),
        ///     u =&gt; new Entity { Status = "Expired", UpdatedAt = DateTime.Now }
        /// );
        /// </example>
        [SqlTemplate("UPDATE {{table}} SET {{set --from updates}} {{where}}")]
        Task<int> BatchUpdateWhereAsync<TUpdates>([ExpressionToSql] Expression<Func<TEntity, bool>> predicate, TUpdates updates, CancellationToken cancellationToken = default);

        // ===== Batch Delete =====

        /// <summary>Batch deletes entities by primary keys.</summary>
        /// <param name="ids">List of primary key values</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Total rows affected</returns>
        /// <remarks>Generated SQL: DELETE FROM table WHERE id IN (@id0, @id1, @id2, ...)</remarks>
        [SqlTemplate("DELETE FROM {{table}} WHERE id IN {{values --param ids}}")]
        [BatchOperation(MaxBatchSize = 1000)]
        Task<int> BatchDeleteAsync(IList<TKey> ids, CancellationToken cancellationToken = default);

        /// <summary>Batch soft deletes entities by primary keys.</summary>
        /// <param name="ids">List of primary key values</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Total rows affected</returns>
        /// <remarks>Requires [SoftDelete] attribute on entity class.</remarks>
        [SqlTemplate("UPDATE {{table}} SET is_deleted = 1, deleted_at = @now WHERE id IN {{values --param ids}}")]
        [BatchOperation(MaxBatchSize = 1000)]
        Task<int> BatchSoftDeleteAsync(IList<TKey> ids, CancellationToken cancellationToken = default);

        // ===== Batch Upsert =====

        /// <summary>Batch upsert (insert or update) entities.</summary>
        /// <param name="entities">List of entities to upsert</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Total rows affected</returns>
        /// <remarks>
        /// Database-specific implementation:
        /// - MySQL: INSERT ... ON DUPLICATE KEY UPDATE
        /// - PostgreSQL: INSERT ... ON CONFLICT DO UPDATE
        /// - SQLite: INSERT OR REPLACE
        /// - SQL Server: MERGE
        /// </remarks>
        [SqlTemplate("{{batch_upsert}}")]
        [BatchOperation(MaxBatchSize = 500)]
        Task<int> BatchUpsertAsync(IList<TEntity> entities, CancellationToken cancellationToken = default);

        // ===== Batch Query =====

        /// <summary>Checks existence of multiple IDs.</summary>
        /// <param name="ids">List of primary key values to check</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of booleans indicating existence (same order as input)</returns>
        /// <example>
        /// var exists = await repo.BatchExistsAsync(new List&lt;long&gt; { 1, 2, 99999 });
        /// // [true, true, false]
        /// </example>
        [SqlTemplate("{{batch_exists}}")]
        Task<List<bool>> BatchExistsAsync(IList<TKey> ids, CancellationToken cancellationToken = default);
    }
}


