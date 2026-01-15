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
    /// <typeparam name="T">The entity type.</typeparam>
    public class SqlxQueryProvider<
#if NET5_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
        T> : IQueryProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SqlxQueryProvider{T}"/> class.
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
            // If TElement is the same as T, reuse this provider
            if (typeof(TElement) == typeof(T))
            {
                var reader = ResultReader as IResultReader<TElement>;
                return new SqlxQueryable<TElement>(this as SqlxQueryProvider<TElement> ?? throw new InvalidOperationException(), expression, Connection, reader);
            }
            
            // Otherwise create a new provider for TElement
            var newProvider = new SqlxQueryProvider<TElement>(Dialect)
            {
                Connection = Connection,
                ResultReader = ResultReader
            };
            return new SqlxQueryable<TElement>(newProvider, expression);
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

                switch (methodName)
                {
                    case "First":
                    case "FirstOrDefault":
                        {
                            var (sql, parameters) = ToSqlWithParameters(expression);
                            var result = DbExecutor.ExecuteReader(Connection, sql, parameters, (IResultReader<TResult>)ResultReader!);
                            return methodName == "First" ? result.First() : result.FirstOrDefault()!;
                        }
                    case "Count":
                    case "LongCount":
                        {
                            var sourceExpression = methodCall.Arguments[0];
                            var (sourceSql, parameters) = ToSqlWithParameters(sourceExpression);
                            var sql = $"SELECT COUNT(*) FROM ({sourceSql}) AS CountQuery";
                            var result = DbExecutor.ExecuteScalar(Connection, sql, parameters);
                            return (TResult)Convert.ChangeType(result!, typeof(TResult));
                        }
                    case "Min":
                    case "Max":
                    case "Sum":
                    case "Average":
                        {
                            var sourceExpression = methodCall.Arguments[0];
                            var (sourceSql, parameters) = ToSqlWithParameters(sourceExpression);
                            
                            string columnExpression = "*";
                            if (methodCall.Arguments.Count > 1)
                            {
                                var selectorArg = methodCall.Arguments[1];
                                // Unwrap Quote if present
                                if (selectorArg is UnaryExpression { NodeType: ExpressionType.Quote } unary)
                                {
                                    selectorArg = unary.Operand;
                                }
                                
                                if (selectorArg is LambdaExpression lambda && lambda.Body is MemberExpression member)
                                {
                                    // Get column name from cached metadata
                                    var columnMeta = SqlQuery<T>.GetColumnByProperty(member.Member.Name);
                                    var columnName = columnMeta?.Name ?? member.Member.Name;
                                    columnExpression = Dialect.WrapColumn(columnName);
                                }
                            }
                            
                            var aggregateFunc = methodName switch
                            {
                                "Min" => Dialect.Min(columnExpression),
                                "Max" => Dialect.Max(columnExpression),
                                "Sum" => Dialect.Sum(columnExpression),
                                "Average" => Dialect.Avg(columnExpression),
                                _ => throw new NotSupportedException($"Aggregate function '{methodName}' is not supported.")
                            };
                            var sql = $"SELECT {aggregateFunc} FROM ({sourceSql}) AS AggregateQuery";
                            var result = DbExecutor.ExecuteScalar(Connection, sql, parameters);
                            return (TResult)Convert.ChangeType(result!, typeof(TResult));
                        }
                }
            }

            throw new NotSupportedException(
                $"Execute is not supported for expression type '{expression.NodeType}'. Supported methods: First, FirstOrDefault, Count, LongCount, Min, Max, Sum, Average, ToList.");
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
