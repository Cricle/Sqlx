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

        // ===== Multiple Entity Queries =====

        /// <summary>Gets multiple entities by IDs.</summary>
        /// <param name="ids">List of primary key values</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of entities</returns>
        /// <remarks>Generated SQL: SELECT * FROM table WHERE id IN (@id0, @id1, @id2...)</remarks>
        [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id IN {{values --param ids}}")]
        Task<List<TEntity>> GetByIdsAsync(List<TKey> ids, CancellationToken cancellationToken = default);

        /// <summary>Gets all entities with automatic limit.</summary>
        /// <param name="limit">Maximum number of rows to return (default 1000, max 10000 recommended)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of entities (up to limit)</returns>
        /// <remarks>Always limited to prevent memory issues. For large datasets, use pagination (GetPageAsync or GetRangeAsync).</remarks>
        [SqlTemplate("SELECT {{columns}} FROM {{table}} {{limit --param limit}}")]
        Task<List<TEntity>> GetAllAsync(int limit = 1000, CancellationToken cancellationToken = default);

        /// <summary>Gets top N entities.</summary>
        /// <param name="limit">Maximum number of rows to return</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of entities (up to limit)</returns>
        [SqlTemplate("SELECT {{columns}} FROM {{table}} {{limit --param limit}}")]
        Task<List<TEntity>> GetTopAsync(int limit, CancellationToken cancellationToken = default);

        // ===== Pagination =====

        /// <summary>Gets entities with offset/limit pagination.</summary>
        /// <param name="limit">Maximum number of rows to return</param>
        /// <param name="offset">Number of rows to skip</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of entities</returns>
        [SqlTemplate("SELECT {{columns}} FROM {{table}} {{limit --param limit}} {{offset --param offset}}")]
        Task<List<TEntity>> GetRangeAsync(int limit = 100, int offset = 0, CancellationToken cancellationToken = default);

        /// <summary>Gets a page of entities with total count.</summary>
        /// <param name="pageNumber">Page number (1-based)</param>
        /// <param name="pageSize">Number of items per page</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Paged result with items and pagination metadata</returns>
        Task<PagedResult<TEntity>> GetPageAsync(int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default);

        // ===== Condition-based Queries =====

        /// <summary>Gets entities matching expression predicate.</summary>
        /// <param name="predicate">Expression predicate (e.g., x =&gt; x.Age &gt; 18 &amp;&amp; x.IsActive)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of matching entities</returns>
        /// <example>
        /// var users = await repo.GetWhereAsync(x =&gt; x.Age &gt;= 18 &amp;&amp; x.IsActive);
        /// </example>
        [SqlTemplate("SELECT {{columns}} FROM {{table}} {{where}}")]
        Task<List<TEntity>> GetWhereAsync([ExpressionToSql] Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

        /// <summary>Gets first entity matching expression predicate.</summary>
        /// <param name="predicate">Expression predicate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>First matching entity or null</returns>
        [SqlTemplate("SELECT {{columns}} FROM {{table}} {{where}} LIMIT 1")]
        Task<TEntity?> GetFirstWhereAsync([ExpressionToSql] Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

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
        [SqlTemplate("SELECT CASE WHEN EXISTS(SELECT 1 FROM {{table}} {{where}}) THEN 1 ELSE 0 END")]
        Task<bool> ExistsWhereAsync([ExpressionToSql] Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

        // ===== Additional Useful Methods =====

        /// <summary>Gets random N entities.</summary>
        /// <param name="count">Number of random entities to return</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of random entities</returns>
        /// <remarks>
        /// Uses database-specific random function:
        /// - SQL Server: ORDER BY NEWID()
        /// - MySQL: ORDER BY RAND()
        /// - PostgreSQL: ORDER BY RANDOM()
        /// - SQLite: ORDER BY RANDOM()
        /// Oracle: ORDER BY DBMS_RANDOM.VALUE
        /// </remarks>
        [SqlTemplate("SELECT {{columns}} FROM {{table}} ORDER BY RANDOM() LIMIT @count")]
        Task<List<TEntity>> GetRandomAsync(int count, CancellationToken cancellationToken = default);

        // TODO: GetDistinctValuesAsync需要源生成器特殊支持来处理非实体返回类型
        // /// <summary>Gets distinct values from a column.</summary>
        // /// <param name="column">Column name to get distinct values from</param>
        // /// <param name="limit">Maximum number of distinct values to return (default 1000)</param>
        // /// <param name="cancellationToken">Cancellation token</param>
        // /// <returns>List of distinct values</returns>
        // /// <example>
        // /// var statuses = await repo.GetDistinctValuesAsync("status");
        // /// // ["active", "inactive", "pending"]
        // /// </example>
        // [SqlTemplate("SELECT DISTINCT {{column}} FROM {{table}} WHERE {{column}} IS NOT NULL ORDER BY {{column}} {{limit --param limit}}")]
        // Task<List<string>> GetDistinctValuesAsync([DynamicSql(Type = DynamicSqlType.Identifier)] string column, int limit = 1000, CancellationToken cancellationToken = default);
    }
}


