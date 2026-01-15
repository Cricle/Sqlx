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
using System.Threading;
using System.Xml.Linq;


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
        /// <summary>
        /// Initializes a new instance of the <see cref="SqlxQueryProvider"/> class.
        /// </summary>
        /// <param name="dialect">The SQL dialect.</param>
        public SqlxQueryProvider(SqlDialect dialect)
        {
            Dialect = dialect ?? throw new ArgumentNullException(nameof(dialect));
        }

        /// <summary>
        /// Gets the SQL dialect.
        /// </summary>
        public SqlDialect Dialect { get; }

        /// <summary>
        /// Gets or sets the database connection for query execution.
        /// </summary>
        internal DbConnection? Connection { get; set; }

        /// <summary>
        /// Gets or sets the result reader (stored as object for non-generic provider).
        /// </summary>
        internal object? ResultReader { get; set; }

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
            var reader = ResultReader as IResultReader<TElement>;
            return new SqlxQueryable<TElement>(this, expression, Connection, reader);
        }

        /// <inheritdoc/>
        public object? Execute(Expression expression) => throw new NotSupportedException("Use Execute<TResult> for AOT compatibility.");

        /// <inheritdoc/>
        public TResult Execute<TResult>(Expression expression)
        {
            // This method is called by LINQ methods like First(), Single(), Count(), etc.
            if (Connection == null)
            {
                throw new InvalidOperationException("Connection is not set. Use WithConnection() before executing queries.");
            }

            if (ResultReader == null)
            {
                throw new InvalidOperationException("ResultReader is not set. Use WithReader() before executing queries.");
            }

            // Check if this is a method call expression
            if (expression is MethodCallExpression methodCall)
            {
                var methodName = methodCall.Method.Name;

                // Handle First/FirstOrDefault
                switch (methodName)
                {
                    case "First":
                    case "FirstOrDefault":
                        {
                            var (sql, parameters) = ToSqlWithParameters(expression);
                            var result = DbExecutor.ExecuteReader(Connection, sql, parameters, (IResultReader<TResult>)ResultReader!);
                            return methodName== "First"
                                ? result.First()
                                : result.FirstOrDefault()!;
                        }
                    case "Count":
                    case "LongCount":
                        {
                            // Generate the base query
                            var visitor = new SqlExpressionVisitor(Dialect, parameterized: true);
                            var baseSql = visitor.GenerateSql(methodCall.Arguments[0]); // Get the source query without Count
                            var parameters = visitor.GetParameters();
                            
                            // Wrap in COUNT query
                            var sql = $"SELECT COUNT(*) FROM ({baseSql}) AS CountQuery";
                            
                            var result = DbExecutor.ExecuteScalar(Connection, sql, parameters);
                            return (TResult)Convert.ChangeType(result!, typeof(TResult));
                        }
                }
            }

            throw new NotSupportedException(
                $"Execute is not supported for expression type '{expression.NodeType}'. Supported methods: First, FirstOrDefault, Count, LongCount, ToList.");
        }

        /// <summary>
        /// Generates SQL from the expression.
        /// </summary>
        /// <param name="expression">The expression tree.</param>
        /// <param name="parameterized">Whether to generate parameterized SQL.</param>
        /// <returns>The generated SQL string.</returns>
        public string ToSql(Expression expression, bool parameterized = false) => new SqlExpressionVisitor(Dialect, parameterized).GenerateSql(expression);

        /// <summary>
        /// Generates parameterized SQL and parameters from the expression.
        /// </summary>
        /// <param name="expression">The expression tree.</param>
        /// <returns>A tuple containing the SQL string and parameters.</returns>
        public (string Sql, IEnumerable<KeyValuePair<string, object?>> Parameters) ToSqlWithParameters(Expression expression)
        {
            var visitor = new SqlExpressionVisitor(Dialect, parameterized: true);
            return (visitor.GenerateSql(expression), visitor.GetParameters());
        }
    }
}
