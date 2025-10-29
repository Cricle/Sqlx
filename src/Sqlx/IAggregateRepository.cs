// -----------------------------------------------------------------------
// <copyright file="IAggregateRepository.cs" company="Cricle">
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
    /// Aggregate repository interface for statistical and aggregate operations (COUNT, SUM, AVG, MAX, MIN).
    /// </summary>
    /// <typeparam name="TEntity">Entity type</typeparam>
    /// <typeparam name="TKey">Primary key type</typeparam>
    public interface IAggregateRepository<TEntity, TKey>
        where TEntity : class
    {
        // ===== Count Operations =====

        /// <summary>Gets total count of all entities.</summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Total row count</returns>
        [SqlTemplate("SELECT COUNT(*) FROM {{table}}")]
        Task<long> CountAsync(CancellationToken cancellationToken = default);

        /// <summary>Gets count of entities matching expression predicate.</summary>
        /// <param name="predicate">Expression predicate for WHERE clause</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Count of matching rows</returns>
        /// <example>
        /// long count = await repo.CountWhereAsync(x =&gt; x.IsActive &amp;&amp; x.Age &gt;= 18);
        /// </example>
        [SqlTemplate("SELECT COUNT(*) FROM {{table}} {{where}}")]
        Task<long> CountWhereAsync([ExpressionToSql] Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

        /// <summary>Gets count grouped by column value.</summary>
        /// <param name="column">Column name to group by</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Dictionary mapping column value to count</returns>
        /// <example>
        /// var counts = await repo.CountByAsync("status");
        /// // { "active": 100, "inactive": 50, "banned": 10 }
        /// </example>
        [SqlTemplate("SELECT @column, COUNT(*) FROM {{table}} GROUP BY @column")]
        Task<Dictionary<string, long>> CountByAsync(string column, CancellationToken cancellationToken = default);

        // ===== Sum Operations =====

        /// <summary>Sums a numeric column.</summary>
        /// <param name="column">Column name to sum</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Sum of column values</returns>
        /// <example>
        /// decimal totalPrice = await repo.SumAsync("price");
        /// </example>
        [SqlTemplate("SELECT COALESCE(SUM(@column), 0) FROM {{table}}")]
        Task<decimal> SumAsync(string column, CancellationToken cancellationToken = default);

        /// <summary>Sums column for entities matching expression predicate.</summary>
        /// <param name="column">Column name to sum</param>
        /// <param name="predicate">Expression predicate for WHERE clause</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Sum of matching rows</returns>
        /// <example>
        /// decimal paidTotal = await repo.SumWhereAsync("amount", x =&gt; x.Status == "Paid");
        /// </example>
        [SqlTemplate("SELECT COALESCE(SUM(@column), 0) FROM {{table}} {{where}}")]
        Task<decimal> SumWhereAsync(string column, [ExpressionToSql] Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

        // ===== Average Operations =====

        /// <summary>Gets average of a numeric column.</summary>
        /// <param name="column">Column name to average</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Average of column values</returns>
        /// <example>
        /// decimal avgAge = await repo.AvgAsync("age");
        /// </example>
        [SqlTemplate("SELECT COALESCE(AVG(@column), 0) FROM {{table}}")]
        Task<decimal> AvgAsync(string column, CancellationToken cancellationToken = default);

        /// <summary>Gets average for entities matching expression predicate.</summary>
        /// <param name="column">Column name to average</param>
        /// <param name="predicate">Expression predicate for WHERE clause</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Average of matching rows</returns>
        [SqlTemplate("SELECT COALESCE(AVG(@column), 0) FROM {{table}} {{where}}")]
        Task<decimal> AvgWhereAsync(string column, [ExpressionToSql] Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

        // ===== Min/Max Operations =====

        /// <summary>Gets maximum value of a column.</summary>
        /// <typeparam name="T">Column type</typeparam>
        /// <param name="column">Column name</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Maximum value</returns>
        /// <example>
        /// decimal maxPrice = await repo.MaxAsync&lt;decimal&gt;("price");
        /// </example>
        [SqlTemplate("SELECT MAX(@column) FROM {{table}}")]
        Task<T> MaxAsync<T>(string column, CancellationToken cancellationToken = default);

        /// <summary>Gets minimum value of a column.</summary>
        /// <typeparam name="T">Column type</typeparam>
        /// <param name="column">Column name</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Minimum value</returns>
        [SqlTemplate("SELECT MIN(@column) FROM {{table}}")]
        Task<T> MinAsync<T>(string column, CancellationToken cancellationToken = default);

        /// <summary>Gets min and max values in a single query.</summary>
        /// <typeparam name="T">Column type</typeparam>
        /// <param name="column">Column name</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Tuple with minimum and maximum values</returns>
        /// <example>
        /// var (min, max) = await repo.MinMaxAsync&lt;decimal&gt;("price");
        /// </example>
        [SqlTemplate("SELECT MIN(@column), MAX(@column) FROM {{table}}")]
        Task<(T Min, T Max)> MinMaxAsync<T>(string column, CancellationToken cancellationToken = default);

        // ===== Custom Aggregate =====

        /// <summary>Executes custom aggregate function.</summary>
        /// <typeparam name="T">Return type</typeparam>
        /// <param name="function">SQL aggregate function name (e.g., "STRING_AGG", "GROUP_CONCAT")</param>
        /// <param name="column">Column name</param>
        /// <param name="separator">Optional separator for string aggregation functions</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Aggregate result</returns>
        /// <example>
        /// // PostgreSQL: string allNames = await repo.AggregateAsync&lt;string&gt;("STRING_AGG", "name", ", ");
        /// // MySQL: string allNames = await repo.AggregateAsync&lt;string&gt;("GROUP_CONCAT", "name", ", ");
        /// </example>
        [SqlTemplate("SELECT @function(@column, @separator) FROM {{table}}")]
        Task<T> AggregateAsync<T>(string function, string column, string? separator = null, CancellationToken cancellationToken = default);
    }
}

