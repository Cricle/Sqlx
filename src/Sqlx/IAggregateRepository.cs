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
        /// <summary>Gets count of entities, optionally filtered by a predicate.</summary>
        /// <param name="predicate">Optional expression predicate for filtering (null = count all entities)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Count of entities</returns>
        /// <example>
        /// // Count all entities
        /// long total = await repo.CountAsync();
        /// 
        /// // Count with filter
        /// long activeCount = await repo.CountAsync(x => x.IsActive);
        /// 
        /// // Count with complex condition
        /// long count = await repo.CountAsync(x => x.IsActive &amp;&amp; x.Age &gt;= 18);
        /// </example>
        [SqlTemplate("SELECT COUNT(*) FROM {{table}} {{*ifnotnull predicate}}{{where --param predicate}}{{/ifnotnull}}")]
        Task<long> CountAsync([ExpressionToSql] Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default);
    }
}
