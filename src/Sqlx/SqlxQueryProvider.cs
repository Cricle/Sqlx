// -----------------------------------------------------------------------
// <copyright file="SqlxQueryProvider.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
#if NET5_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace Sqlx
{
    /// <summary>
    /// IQueryProvider implementation for SQL generation (AOT-friendly, no reflection).
    /// </summary>
    public class SqlxQueryProvider : IQueryProvider
    {
        private readonly SqlDialect _dialect;

        public SqlxQueryProvider(SqlDialect dialect)
        {
            _dialect = dialect ?? throw new ArgumentNullException(nameof(dialect));
        }

        public SqlDialect Dialect => _dialect;

        public IQueryable CreateQuery(Expression expression)
            => throw new NotSupportedException("Use CreateQuery<T> for AOT compatibility.");

        public IQueryable<TElement> CreateQuery<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
            TElement>(Expression expression) => new SqlxQueryable<TElement>(this, expression);

        public object? Execute(Expression expression)
            => throw new NotSupportedException("Use GetEnumerator() on SqlxQueryable with WithConnection() and WithMapper().");

        public TResult Execute<TResult>(Expression expression) => (TResult)Execute(expression)!;

        public string ToSql(Expression expression, bool parameterized = false)
            => new SqlExpressionVisitor(_dialect, parameterized).GenerateSql(expression);

        public (string Sql, IEnumerable<KeyValuePair<string, object?>> Parameters) ToSqlWithParameters(Expression expression)
        {
            var visitor = new SqlExpressionVisitor(_dialect, parameterized: true);
            var sql = visitor.GenerateSql(expression);
            return (sql, visitor.GetParameters());
        }
    }
}
