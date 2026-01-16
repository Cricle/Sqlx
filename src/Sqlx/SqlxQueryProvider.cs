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
        /// <param name="sourceEntityProvider">Optional source entity provider (for preserving entity metadata after projections).</param>
        public SqlxQueryProvider(SqlDialect dialect, IEntityProvider? sourceEntityProvider = null)
        {
            Dialect = dialect ?? throw new ArgumentNullException(nameof(dialect));
            SourceEntityProvider = sourceEntityProvider;
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

        /// <summary>
        /// Gets the source entity provider (preserved from the original query before projections like GroupBy).
        /// </summary>
        internal IEntityProvider? SourceEntityProvider { get; }

        /// <summary>
        /// Gets the entity provider for type T (cached in SqlQuery&lt;T&gt;).
        /// Falls back to SourceEntityProvider if T's EntityProvider is null (e.g., after GroupBy).
        /// </summary>
        private IEntityProvider? GetEntityProvider()
        {
            return SqlQuery<T>.EntityProvider ?? SourceEntityProvider;
        }

        /// <inheritdoc/>
        public IQueryable CreateQuery(Expression expression)
        {
            throw new NotSupportedException("Use CreateQuery<T> for AOT compatibility.");
        }

        /// <inheritdoc/>
        public IQueryable<TElement> CreateQuery<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)]
#endif
        TElement>(Expression expression)
        {
            // If TElement is the same as T, reuse this provider
            if (typeof(TElement) == typeof(T))
            {
                var reader = ResultReader as IResultReader<TElement>;
                return new SqlxQueryable<TElement>(this as SqlxQueryProvider<TElement> ?? throw new InvalidOperationException(), expression, Connection, reader);
            }
            
            // Check if we have a cached reader for TElement (from source generator or previous dynamic creation)
            var cachedReader = SqlQuery<TElement>.ResultReader;
            
            // If no cached reader and this is a type that needs a dynamic reader, create one
            if (cachedReader == null && ShouldCreateDynamicReader<TElement>())
            {
                // Create dynamic ResultReader using type's property names and cache it
                cachedReader = new DynamicResultReader<TElement>();
                SqlQuery<TElement>.ResultReader = cachedReader;
            }
            
            // Preserve the source entity provider for operations like GroupBy that change the element type
            // but still need access to the original entity metadata for SQL generation
            var sourceEntityProvider = GetEntityProvider();
            
            // Create a new provider for TElement
            var newProvider = new SqlxQueryProvider<TElement>(Dialect, sourceEntityProvider)
            {
                Connection = Connection,
                ResultReader = cachedReader
            };
            return new SqlxQueryable<TElement>(newProvider, expression);
        }

        /// <summary>
        /// Determines if a dynamic reader should be created for the given type.
        /// </summary>
        private static bool ShouldCreateDynamicReader<TElement>()
        {
            var elementType = typeof(TElement);
            
            // Don't create dynamic readers for primitive/simple types or IGrouping
            if (elementType.IsPrimitive || 
                elementType == typeof(string) ||
                elementType == typeof(decimal) ||
                elementType == typeof(DateTime) ||
                elementType == typeof(DateTimeOffset) ||
                elementType == typeof(Guid) ||
                elementType == typeof(TimeSpan) ||
                (elementType.IsGenericType && elementType.GetGenericTypeDefinition() == typeof(IGrouping<,>)))
            {
                return false;
            }
            
            // Create dynamic reader for anonymous types or classes with properties
            return true;
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

            // Check if this is a method call expression
            if (expression is MethodCallExpression methodCall)
            {
                var methodName = methodCall.Method.Name;

                switch (methodName)
                {
                    case "First":
                    case "FirstOrDefault":
                        {
                            if (ResultReader == null)
                            {
                                throw new InvalidOperationException($"ResultReader is not set for type {typeof(TResult).Name}.");
                            }
                            var (sql, parameters) = ToSqlWithParameters(expression);
                            var result = DbExecutor.ExecuteReader(Connection, sql, parameters, (IResultReader<TResult>)ResultReader);
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
        public string ToSql(Expression expression, bool parameterized = false) => new SqlExpressionVisitor(Dialect, parameterized, GetEntityProvider()).GenerateSql(expression);

        /// <summary>
        /// Generates parameterized SQL and parameters from the expression.
        /// </summary>
        /// <param name="expression">The expression tree.</param>
        /// <returns>A tuple containing the SQL string and parameters.</returns>
        public (string Sql, IEnumerable<KeyValuePair<string, object?>> Parameters) ToSqlWithParameters(Expression expression)
        {
            var visitor = new SqlExpressionVisitor(Dialect, parameterized: true, GetEntityProvider());
            return (visitor.GenerateSql(expression), visitor.GetParameters());
        }
    }
}
