// -----------------------------------------------------------------------
// <copyright file="ICommandRepository.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Sqlx
{
    /// <summary>
    /// Command repository interface for modifying data (Insert, Update, Delete).
    /// </summary>
    /// <typeparam name="TEntity">Entity type</typeparam>
    /// <typeparam name="TKey">Primary key type</typeparam>
    public interface ICommandRepository<TEntity, TKey>
        where TEntity : class
    {
        // ===== Insert Operations =====

        /// <summary>Inserts new entity.</summary>
        /// <param name="entity">Entity to insert</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Rows affected (1 on success)</returns>
        [SqlTemplate("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})")]
        Task<int> InsertAsync(TEntity entity, CancellationToken cancellationToken = default);

        /// <summary>Inserts entity and returns generated primary key.</summary>
        /// <param name="entity">Entity to insert</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Generated primary key value</returns>
        [SqlTemplate("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})")]
        [ReturnInsertedId]
        Task<TKey> InsertAndGetIdAsync(TEntity entity, CancellationToken cancellationToken = default);

        /// <summary>Inserts entity and returns the complete inserted entity (with generated ID).</summary>
        /// <param name="entity">Entity to insert</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Inserted entity with generated values</returns>
        [SqlTemplate("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})")]
        [ReturnInsertedEntity]
        Task<TEntity> InsertAndGetEntityAsync(TEntity entity, CancellationToken cancellationToken = default);

        // ===== Update Operations =====

        /// <summary>Updates entity by primary key.</summary>
        /// <param name="entity">Entity to update (must contain Id)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Rows affected (0 = not found, 1 = success)</returns>
        [SqlTemplate("UPDATE {{table}} SET {{set --exclude Id}} WHERE id = @id")]
        Task<int> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

        /// <summary>Updates specific columns only (partial update).</summary>
        /// <param name="id">Entity primary key</param>
        /// <param name="updates">Anonymous object with column-value pairs to update</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Rows affected (0 = not found, 1 = success)</returns>
        /// <example>
        /// await repo.UpdatePartialAsync(userId, new { Name = "Alice", UpdatedAt = DateTime.Now });
        /// </example>
        [SqlTemplate("UPDATE {{table}} SET {{set --from updates}} WHERE id = @id")]
        Task<int> UpdatePartialAsync(TKey id, object updates, CancellationToken cancellationToken = default);

        /// <summary>Updates entities matching expression predicate.</summary>
        /// <param name="predicate">Expression predicate for WHERE clause</param>
        /// <param name="updates">Anonymous object with column-value pairs to update</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Rows affected</returns>
        /// <example>
        /// await repo.UpdateWhereAsync(x => x.Status == "Pending", new { Status = "Active" });
        /// </example>
        [ExpressionToSql(Target = "where")]
        [SqlTemplate("UPDATE {{table}} SET {{set --from updates}} {{where}}")]
        Task<int> UpdateWhereAsync(Expression<Func<TEntity, bool>> predicate, object updates, CancellationToken cancellationToken = default);

        /// <summary>Inserts if not exists, updates if exists (UPSERT/MERGE).</summary>
        /// <param name="entity">Entity to upsert</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Rows affected</returns>
        /// <remarks>
        /// Database-specific implementation:
        /// - MySQL: INSERT ... ON DUPLICATE KEY UPDATE
        /// - PostgreSQL: INSERT ... ON CONFLICT DO UPDATE
        /// - SQLite: INSERT OR REPLACE
        /// - SQL Server: MERGE
        /// </remarks>
        Task<int> UpsertAsync(TEntity entity, CancellationToken cancellationToken = default);

        // ===== Delete Operations =====

        /// <summary>Deletes entity by primary key (physical delete).</summary>
        /// <param name="id">Primary key value</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Rows affected (0 = not found, 1 = success)</returns>
        /// <remarks>This is a permanent delete and cannot be undone.</remarks>
        [SqlTemplate("DELETE FROM {{table}} WHERE id = @id")]
        Task<int> DeleteAsync(TKey id, CancellationToken cancellationToken = default);

        /// <summary>Deletes entities matching expression predicate.</summary>
        /// <param name="predicate">Expression predicate for WHERE clause</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Rows affected</returns>
        /// <example>
        /// await repo.DeleteWhereAsync(x => x.CreatedAt < DateTime.Now.AddYears(-1));
        /// </example>
        [ExpressionToSql]
        [SqlTemplate("DELETE FROM {{table}} {{where}}")]
        Task<int> DeleteWhereAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

        // ===== Soft Delete Operations =====

        /// <summary>Soft deletes entity (sets IsDeleted flag).</summary>
        /// <param name="id">Primary key value</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Rows affected (0 = not found, 1 = success)</returns>
        /// <remarks>Requires [SoftDelete] attribute on entity class.</remarks>
        [SqlTemplate("UPDATE {{table}} SET is_deleted = 1, deleted_at = @now WHERE id = @id")]
        Task<int> SoftDeleteAsync(TKey id, CancellationToken cancellationToken = default);

        /// <summary>Restores soft-deleted entity.</summary>
        /// <param name="id">Primary key value</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Rows affected</returns>
        [SqlTemplate("UPDATE {{table}} SET is_deleted = 0, deleted_at = NULL WHERE id = @id")]
        Task<int> RestoreAsync(TKey id, CancellationToken cancellationToken = default);

        /// <summary>Permanently deletes all soft-deleted entities.</summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Rows affected</returns>
        [SqlTemplate("DELETE FROM {{table}} WHERE is_deleted = 1")]
        Task<int> PurgeDeletedAsync(CancellationToken cancellationToken = default);
    }
}

