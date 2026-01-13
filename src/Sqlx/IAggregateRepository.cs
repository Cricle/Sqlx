// -----------------------------------------------------------------------
// <copyright file="IAggregateRepository.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx
{
    // Aggregate operations (Count) are now included in IQueryRepository.
    // For custom aggregate operations (SUM, AVG, MAX, MIN), define methods in your repository interface.
    //
    // Example:
    // [SqlTemplate("SELECT SUM({{wrap Amount}}) FROM {{table}} {{where --param predicate}}")]
    // Task<decimal> SumAmountAsync([ExpressionToSql] Expression<Func<Order, bool>>? predicate = null, CancellationToken ct = default);
}
