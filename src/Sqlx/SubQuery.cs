// -----------------------------------------------------------------------
// <copyright file="SubQuery.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Linq;
#if NET5_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace Sqlx
{
    /// <summary>
    /// Placeholder class for marking subqueries in Select projections.
    /// The actual parsing is handled by ExpressionParser.
    /// </summary>
    public static class SubQuery
    {
        /// <summary>
        /// Placeholder method for creating a subquery. Returns null but is never actually called at runtime.
        /// The actual SQL generation is handled by ExpressionParser when it detects this method call in the expression tree.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <returns>Null (this method is only used as a marker in expression trees).</returns>
        public static IQueryable<T> For<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
            T>() => null!;
    }
}
