// -----------------------------------------------------------------------
// <copyright file="SqlxQueryProvider.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
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
        private DbConnection? _connection;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlxQueryProvider"/> class.
        /// </summary>
        /// <param name="dialect">The SQL dialect.</param>
        public SqlxQueryProvider(SqlDialect dialect)
        {
            _dialect = dialect ?? throw new ArgumentNullException(nameof(dialect));
        }

        /// <summary>
        /// Gets the SQL dialect.
        /// </summary>
        public SqlDialect Dialect => _dialect;

        /// <summary>
        /// Gets or sets the database connection for query execution.
        /// </summary>
        internal DbConnection? Connection
        {
            get => _connection;
            set => _connection = value;
        }

        /// <inheritdoc/>
        public IQueryable CreateQuery(Expression expression)
        {
            throw new NotSupportedException("Use CreateQuery<T> for AOT compatibility.");
        }

        /// <inheritdoc/>
        public IQueryable<TElement> CreateQuery<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
            TElement>(Expression expression)
        {
            return new SqlxQueryable<TElement>(this, expression);
        }

        /// <inheritdoc/>
        public object? Execute(Expression expression)
        {
            throw new NotSupportedException("Use Execute<TResult> for AOT compatibility.");
        }

        /// <inheritdoc/>
        public TResult Execute<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
            TResult>(Expression expression)
        {
            // This method is called by LINQ methods like First(), Single(), Count(), etc.
            // For SqlxQueryable, we handle execution directly in GetEnumerator/GetAsyncEnumerator
            // This path is only reached when using standard LINQ operators
            throw new NotSupportedException(
                "Direct Execute is not supported. Use ToList(), FirstOrDefault(), etc. on SqlxQueryable with WithConnection() and WithReader().");
        }

        /// <summary>
        /// Generates SQL from the expression.
        /// </summary>
        /// <param name="expression">The expression tree.</param>
        /// <param name="parameterized">Whether to generate parameterized SQL.</param>
        /// <returns>The generated SQL string.</returns>
        public string ToSql(Expression expression, bool parameterized = false)
        {
            return new SqlExpressionVisitor(_dialect, parameterized).GenerateSql(expression);
        }

        /// <summary>
        /// Generates parameterized SQL and parameters from the expression.
        /// </summary>
        /// <param name="expression">The expression tree.</param>
        /// <returns>A tuple containing the SQL string and parameters.</returns>
        public (string Sql, IEnumerable<KeyValuePair<string, object?>> Parameters) ToSqlWithParameters(Expression expression)
        {
            var visitor = new SqlExpressionVisitor(_dialect, parameterized: true);
            var sql = visitor.GenerateSql(expression);
            return (sql, visitor.GetParameters());
        }
    }
}
