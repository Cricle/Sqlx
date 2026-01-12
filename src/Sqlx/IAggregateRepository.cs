// -----------------------------------------------------------------------
// <copyright file="IAggregateRepository.cs" company="Cricle">
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
    /// Aggregate repository interface for count operations.
    /// For other aggregate operations (SUM, AVG, MAX, MIN), define custom methods in your repository interface.
    /// </summary>
    /// <typeparam name="TEntity">Entity type</typeparam>
    /// <typeparam name="TKey">Primary key type</typeparam>
    public interface IAggregateRepository<TEntity, TKey>
        where TEntity : class
    {
        /// <summary>Gets total count of all entities.</summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Count of all entities</returns>
        /// <example>
        /// // Count all entities
        /// long total = await repo.CountAsync();
        /// </example>
        [SqlTemplate("SELECT COUNT(*) FROM {{table}}")]
        Task<long> CountAsync(CancellationToken cancellationToken = default);

        /// <summary>Gets count of entities filtered by a predicate.</summary>
        /// <param name="predicate">Expression predicate for filtering</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Count of filtered entities</returns>
        /// <example>
        /// // Count with filter
        /// long activeCount = await repo.CountWhereAsync(x => x.IsActive);
        /// 
        /// // Count with complex condition
        /// long count = await repo.CountWhereAsync(x => x.IsActive &amp;&amp; x.Age &gt;= 18);
        /// </example>
        [SqlTemplate("SELECT COUNT(*) FROM {{table}} {{where --param predicate}}")]
        Task<long> CountWhereAsync([ExpressionToSql] Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
    }
}
