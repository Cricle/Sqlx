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
#if NET5_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace Sqlx
{
    /// <summary>
    /// IQueryable implementation for SQL generation (AOT-friendly).
    /// </summary>
    public class SqlxQueryable<
#if NET5_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
    T> : IQueryable<T>, IOrderedQueryable<T>
    {
        private readonly SqlxQueryProvider _provider;
        private readonly Expression _expression;

        /// <summary>Creates a new SqlxQueryable with the specified provider.</summary>
        internal SqlxQueryable(SqlxQueryProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _expression = Expression.Constant(this);
        }

        /// <summary>Creates a new SqlxQueryable with the specified provider and expression.</summary>
        internal SqlxQueryable(SqlxQueryProvider provider, Expression expression)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _expression = expression ?? throw new ArgumentNullException(nameof(expression));
        }

        /// <summary>Gets the element type.</summary>
        public Type ElementType => typeof(T);

        /// <summary>Gets the expression tree.</summary>
        public Expression Expression => _expression;

        /// <summary>Gets the query provider.</summary>
        public IQueryProvider Provider => _provider;

        /// <summary>Gets the SQL dialect.</summary>
        public SqlDialect Dialect => _provider.Dialect;

        /// <summary>Not supported - this is for SQL generation only.</summary>
        public IEnumerator<T> GetEnumerator() =>
            throw new NotSupportedException("SqlxQueryable is for SQL generation only. Use ToSql() to get the SQL string.");

        /// <summary>Not supported - this is for SQL generation only.</summary>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
