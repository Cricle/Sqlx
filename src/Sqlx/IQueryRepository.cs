// -----------------------------------------------------------------------
// <copyright file="IQueryRepository.cs" company="Cricle">
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
    /// Query repository interface for reading data with various query methods.
    /// </summary>
    /// <typeparam name="TEntity">Entity type</typeparam>
    /// <typeparam name="TKey">Primary key type</typeparam>
    public interface IQueryRepository<TEntity, TKey>
        where TEntity : class
    {
        // ===== Single Entity Queries =====

        /// <summary>Gets entity by primary key.</summary>
        /// <param name="id">Primary key value</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Entity or null if not found</returns>
        [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
        Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);

        /// <summary>Gets first entity (throws if not found).</summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>First entity</returns>
        /// <exception cref="InvalidOperationException">Thrown when no entity found</exception>
        [SqlTemplate("SELECT {{columns}} FROM {{table}} {{limit --value 1}}")]
        Task<TEntity> GetFirstAsync(CancellationToken cancellationToken = default);

        /// <summary>Gets first entity or null if not found.</summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>First entity or null</returns>
        [SqlTemplate("SELECT {{columns}} FROM {{table}} {{limit --value 1}}")]
        Task<TEntity?> GetFirstOrDefaultAsync(CancellationToken cancellationToken = default);

        /// <summary>Gets single entity (throws if 0 or multiple found).</summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Single entity</returns>
        /// <exception cref="InvalidOperationException">Thrown when 0 or multiple entities found</exception>
        [SqlTemplate("SELECT {{columns}} FROM {{table}}")]
        Task<TEntity> GetSingleAsync(CancellationToken cancellationToken = default);

        /// <summary>Gets single entity or null (throws if multiple found).</summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Single entity or null</returns>
        /// <exception cref="InvalidOperationException">Thrown when multiple entities found</exception>
        [SqlTemplate("SELECT {{columns}} FROM {{table}}")]
        Task<TEntity?> GetSingleOrDefaultAsync(CancellationToken cancellationToken = default);

        // ===== Multiple Entity Queries =====

        /// <summary>Gets multiple entities by IDs.</summary>
        /// <param name="ids">List of primary key values</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of entities</returns>
        /// <remarks>Generated SQL: SELECT * FROM table WHERE id IN (@id0, @id1, @id2...)</remarks>
        [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id IN {{values --param ids}}")]
        Task<List<TEntity>> GetByIdsAsync(List<TKey> ids, CancellationToken cancellationToken = default);

        /// <summary>Gets all entities with optional ordering.</summary>
        /// <param name="orderBy">ORDER BY clause (e.g., "name ASC, created_at DESC"). Null for no ordering.</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of entities</returns>
        [SqlTemplate("SELECT {{columns}} FROM {{table}} {{orderby --param orderBy}}")]
        Task<List<TEntity>> GetAllAsync(string? orderBy = null, CancellationToken cancellationToken = default);

        /// <summary>Gets top N entities.</summary>
        /// <param name="limit">Maximum number of rows to return</param>
        /// <param name="orderBy">ORDER BY clause. Null for no specific order.</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of entities (up to limit)</returns>
        [SqlTemplate("SELECT {{columns}} FROM {{table}} {{orderby --param orderBy}} {{limit --param limit}}")]
        Task<List<TEntity>> GetTopAsync(int limit, string? orderBy = null, CancellationToken cancellationToken = default);

        // ===== Pagination =====

        /// <summary>Gets entities with offset/limit pagination.</summary>
        /// <param name="limit">Maximum number of rows to return</param>
        /// <param name="offset">Number of rows to skip</param>
        /// <param name="orderBy">ORDER BY clause. Null defaults to primary key order.</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of entities</returns>
        [SqlTemplate("SELECT {{columns}} FROM {{table}} {{orderby --param orderBy}} {{limit --param limit}} {{offset --param offset}}")]
        Task<List<TEntity>> GetRangeAsync(int limit = 100, int offset = 0, string? orderBy = null, CancellationToken cancellationToken = default);

        /// <summary>Gets a page of entities with total count.</summary>
        /// <param name="pageNumber">Page number (1-based)</param>
        /// <param name="pageSize">Number of items per page</param>
        /// <param name="orderBy">ORDER BY clause. Null defaults to primary key order.</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Paged result with items and pagination metadata</returns>
        Task<PagedResult<TEntity>> GetPageAsync(int pageNumber = 1, int pageSize = 20, string? orderBy = null, CancellationToken cancellationToken = default);

        // ===== Condition-based Queries =====

        /// <summary>Gets entities matching expression predicate.</summary>
        /// <param name="predicate">Expression predicate (e.g., x => x.Age > 18 && x.IsActive)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of matching entities</returns>
        /// <example>
        /// var users = await repo.GetWhereAsync(x => x.Age >= 18 && x.IsActive);
        /// </example>
        [ExpressionToSql]
        Task<List<TEntity>> GetWhereAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

        /// <summary>Gets first entity matching expression predicate.</summary>
        /// <param name="predicate">Expression predicate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>First matching entity or null</returns>
        [ExpressionToSql]
        Task<TEntity?> GetFirstWhereAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

        // ===== Existence Checks =====

        /// <summary>Checks if entity exists by primary key.</summary>
        /// <param name="id">Primary key value</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if exists, false otherwise</returns>
        [SqlTemplate("SELECT CASE WHEN EXISTS(SELECT 1 FROM {{table}} WHERE id = @id) THEN 1 ELSE 0 END")]
        Task<bool> ExistsAsync(TKey id, CancellationToken cancellationToken = default);

        /// <summary>Checks if any entity matches expression predicate.</summary>
        /// <param name="predicate">Expression predicate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if any match found, false otherwise</returns>
        [ExpressionToSql]
        Task<bool> ExistsWhereAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
    }
}

