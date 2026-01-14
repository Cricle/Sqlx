// -----------------------------------------------------------------------
// <copyright file="SqlxQueryable.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
#if NET5_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace Sqlx
{
    /// <summary>
    /// IQueryable implementation for SQL generation (AOT-friendly, no reflection).
    /// </summary>
    public class SqlxQueryable<
#if NET5_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
        T> : IQueryable<T>, IOrderedQueryable<T>, IAsyncEnumerable<T>
    {
        private readonly SqlxQueryProvider _provider;
        private readonly Expression _expression;

        internal SqlxQueryable(SqlxQueryProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _expression = Expression.Constant(this);
        }

        internal SqlxQueryable(SqlxQueryProvider provider, Expression expression)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _expression = expression ?? throw new ArgumentNullException(nameof(expression));
        }

        public Type ElementType => typeof(T);
        public Expression Expression => _expression;
        public IQueryProvider Provider => _provider;
        public SqlDialect Dialect => _provider.Dialect;

        public IEnumerator<T> GetEnumerator() => _provider.Execute<IEnumerable<T>>(_expression).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            => _provider.ExecuteAsync<T>(_expression, cancellationToken);
    }
}
