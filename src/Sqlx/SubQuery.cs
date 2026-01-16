// -----------------------------------------------------------------------
// <copyright file="SubQuery.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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
        /// Creates a subquery marker for use in Select projections.
        /// This returns a placeholder IQueryable that captures the expression tree.
        /// The actual SQL generation is handled by ExpressionParser.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <returns>A placeholder IQueryable for building subquery expressions.</returns>
        public static IQueryable<T> For<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
            T>() => new SubQueryable<T>();
    }

    /// <summary>
    /// Marker IQueryable for subqueries. Only used to build expression trees.
    /// </summary>
    internal sealed class SubQueryable<T> : IQueryable<T>
    {
        private readonly SubQueryProvider _provider;

        public SubQueryable()
        {
            _provider = new SubQueryProvider(typeof(T));
            Expression = Expression.Call(
                typeof(SubQuery).GetMethod(nameof(SubQuery.For))!.MakeGenericMethod(typeof(T)));
        }

        internal SubQueryable(SubQueryProvider provider, Expression expression)
        {
            _provider = provider;
            Expression = expression;
        }

        public Type ElementType => typeof(T);
        public Expression Expression { get; }
        public IQueryProvider Provider => _provider;

        public IEnumerator<T> GetEnumerator() => 
            throw new InvalidOperationException("SubQuery cannot be enumerated. It is only used in Select projections.");

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    /// <summary>
    /// Query provider for SubQueryable. Builds expression trees for subqueries.
    /// </summary>
    internal sealed class SubQueryProvider : IQueryProvider
    {
        private readonly Type _elementType;

        public SubQueryProvider(Type elementType)
        {
            _elementType = elementType;
        }

        public IQueryable CreateQuery(Expression expression)
        {
            var elementType = expression.Type.GetGenericArguments().FirstOrDefault() ?? _elementType;
            var queryableType = typeof(SubQueryable<>).MakeGenericType(elementType);
            return (IQueryable)Activator.CreateInstance(queryableType, this, expression)!;
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new SubQueryable<TElement>(new SubQueryProvider(typeof(TElement)), expression);
        }

        public object? Execute(Expression expression) =>
            throw new InvalidOperationException("SubQuery cannot be executed. It is only used in Select projections.");

        public TResult Execute<TResult>(Expression expression) =>
            throw new InvalidOperationException("SubQuery cannot be executed. It is only used in Select projections.");
    }
}
