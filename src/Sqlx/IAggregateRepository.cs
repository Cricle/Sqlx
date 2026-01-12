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
        [SqlTemplate("SELECT {{wrap column}}, COUNT(*) FROM {{table}} GROUP BY {{wrap column}}")]
        Task<Dictionary<string, long>> CountByAsync([DynamicSql(Type = DynamicSqlType.Identifier)] string column, CancellationToken cancellationToken = default);

        // ===== Sum Operations =====

        /// <summary>Sums a numeric column.</summary>
        /// <param name="column">Column name to sum</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Sum of column values</returns>
        /// <example>
        /// decimal totalPrice = await repo.SumAsync("price");
        /// </example>
        [SqlTemplate("SELECT COALESCE(SUM({{wrap column}}), 0) FROM {{table}}")]
        Task<decimal> SumAsync([DynamicSql(Type = DynamicSqlType.Identifier)] string column, CancellationToken cancellationToken = default);

        /// <summary>Sums column for entities matching expression predicate.</summary>
        /// <param name="column">Column name to sum</param>
        /// <param name="predicate">Expression predicate for WHERE clause</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Sum of matching rows</returns>
        /// <example>
        /// decimal paidTotal = await repo.SumWhereAsync("amount", x =&gt; x.Status == "Paid");
        /// </example>
        [SqlTemplate("SELECT COALESCE(SUM({{wrap column}}), 0) FROM {{table}} {{where --param predicate}}")]
        Task<decimal> SumWhereAsync([DynamicSql(Type = DynamicSqlType.Identifier)] string column, [ExpressionToSql] Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

        // ===== Average Operations =====

        /// <summary>Gets average of a numeric column.</summary>
        /// <param name="column">Column name to average</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Average of column values</returns>
        /// <example>
        /// decimal avgAge = await repo.AvgAsync("age");
        /// </example>
        [SqlTemplate("SELECT COALESCE(AVG({{wrap column}}), 0) FROM {{table}}")]
        Task<decimal> AvgAsync([DynamicSql(Type = DynamicSqlType.Identifier)] string column, CancellationToken cancellationToken = default);

        /// <summary>Gets average for entities matching expression predicate.</summary>
        /// <param name="column">Column name to average</param>
        /// <param name="predicate">Expression predicate for WHERE clause</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Average of matching rows</returns>
        [SqlTemplate("SELECT COALESCE(AVG({{wrap column}}), 0) FROM {{table}} {{where --param predicate}}")]
        Task<decimal> AvgWhereAsync([DynamicSql(Type = DynamicSqlType.Identifier)] string column, [ExpressionToSql] Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

        // ===== Min/Max Operations =====

        /// <summary>Gets maximum integer value of a column.</summary>
        /// <param name="column">Column name</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Maximum value</returns>
        [SqlTemplate("SELECT MAX({{wrap column}}) FROM {{table}}")]
        Task<int> MaxIntAsync([DynamicSql(Type = DynamicSqlType.Identifier)] string column, CancellationToken cancellationToken = default);

        /// <summary>Gets maximum long value of a column.</summary>
        /// <param name="column">Column name</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Maximum value</returns>
        [SqlTemplate("SELECT MAX({{wrap column}}) FROM {{table}}")]
        Task<long> MaxLongAsync([DynamicSql(Type = DynamicSqlType.Identifier)] string column, CancellationToken cancellationToken = default);

        /// <summary>Gets maximum decimal value of a column.</summary>
        /// <param name="column">Column name</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Maximum value</returns>
        /// <example>
        /// decimal maxPrice = await repo.MaxDecimalAsync("price");
        /// </example>
        [SqlTemplate("SELECT MAX({{wrap column}}) FROM {{table}}")]
        Task<decimal> MaxDecimalAsync([DynamicSql(Type = DynamicSqlType.Identifier)] string column, CancellationToken cancellationToken = default);

        /// <summary>Gets maximum DateTime value of a column.</summary>
        /// <param name="column">Column name</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Maximum value</returns>
        [SqlTemplate("SELECT MAX({{wrap column}}) FROM {{table}}")]
        Task<DateTime> MaxDateTimeAsync([DynamicSql(Type = DynamicSqlType.Identifier)] string column, CancellationToken cancellationToken = default);

        /// <summary>Gets minimum integer value of a column.</summary>
        /// <param name="column">Column name</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Minimum value</returns>
        [SqlTemplate("SELECT MIN({{wrap column}}) FROM {{table}}")]
        Task<int> MinIntAsync([DynamicSql(Type = DynamicSqlType.Identifier)] string column, CancellationToken cancellationToken = default);

        /// <summary>Gets minimum long value of a column.</summary>
        /// <param name="column">Column name</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Minimum value</returns>
        [SqlTemplate("SELECT MIN({{wrap column}}) FROM {{table}}")]
        Task<long> MinLongAsync([DynamicSql(Type = DynamicSqlType.Identifier)] string column, CancellationToken cancellationToken = default);

        /// <summary>Gets minimum decimal value of a column.</summary>
        /// <param name="column">Column name</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Minimum value</returns>
        [SqlTemplate("SELECT MIN({{wrap column}}) FROM {{table}}")]
        Task<decimal> MinDecimalAsync([DynamicSql(Type = DynamicSqlType.Identifier)] string column, CancellationToken cancellationToken = default);

        /// <summary>Gets minimum DateTime value of a column.</summary>
        /// <param name="column">Column name</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Minimum value</returns>
        [SqlTemplate("SELECT MIN({{wrap column}}) FROM {{table}}")]
        Task<DateTime> MinDateTimeAsync([DynamicSql(Type = DynamicSqlType.Identifier)] string column, CancellationToken cancellationToken = default);
    }
}

