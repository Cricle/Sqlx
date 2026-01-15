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

        /// <summary>
        /// Gets the entity provider for type T (cached in SqlQuery&lt;T&gt;).
        /// </summary>
        private IEntityProvider? GetEntityProvider()
        {
            return SqlQuery<T>.EntityProvider;
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
            
            // If no cached reader and this is a Select expression that needs a dynamic reader, create one
            if (cachedReader == null && ShouldCreateDynamicReader<TElement>(expression, out var columnNames))
            {
                // Create dynamic ResultReader and cache it
                cachedReader = new DynamicResultReader<TElement>(columnNames);
                SqlQuery<TElement>.ResultReader = cachedReader;
            }
            
            // Create a new provider for TElement
            var newProvider = new SqlxQueryProvider<TElement>(Dialect)
            {
                Connection = Connection,
                ResultReader = cachedReader
            };
            return new SqlxQueryable<TElement>(newProvider, expression);
        }

        /// <summary>
        /// Determines if a dynamic reader should be created for the given type and expression.
        /// </summary>
        private bool ShouldCreateDynamicReader<TElement>(Expression expression, out string[] columnNames)
        {
            columnNames = Array.Empty<string>();
            
            var elementType = typeof(TElement);
            
            // Don't create dynamic readers for:
            // 1. Primitive types (int, string, etc.)
            // 2. Value types without properties (like simple structs)
            // 3. Types that already have entity providers
            // 4. IGrouping types (LINQ grouping interface)
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
            
            // Check if this is a Select expression
            if (!IsSelectExpression(expression, out columnNames))
            {
                return false;
            }
            
            // Only create dynamic reader for complex types (anonymous types or classes with properties)
            return columnNames.Length > 0;
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

        /// <summary>
        /// Checks if the expression is a Select method call and extracts column names.
        /// </summary>
        private bool IsSelectExpression(Expression expression, out string[] columnNames)
        {
            // Walk the expression tree to find the Select call
            if (FindSelectExpression(expression, out var selectCall) && selectCall != null)
            {
                columnNames = ExtractColumnNames(selectCall);
                return columnNames.Length > 0;
            }

            columnNames = Array.Empty<string>();
            return false;
        }

        /// <summary>
        /// Recursively finds the Select method call in the expression tree.
        /// </summary>
        private bool FindSelectExpression(Expression expression, out MethodCallExpression? selectCall)
        {
            selectCall = null;

            if (expression is MethodCallExpression methodCall)
            {
                if (methodCall.Method.Name == "Select")
                {
                    selectCall = methodCall;
                    return true;
                }

                // Recursively check the source (first argument)
                if (methodCall.Arguments.Count > 0)
                {
                    return FindSelectExpression(methodCall.Arguments[0], out selectCall);
                }
            }

            return false;
        }

        /// <summary>
        /// Extracts column names from a Select method call expression.
        /// </summary>
        private string[] ExtractColumnNames(MethodCallExpression methodCall)
        {
            if (methodCall.Arguments.Count < 2)
                return Array.Empty<string>();

            var lambda = GetLambda(methodCall.Arguments[1]);
            if (lambda == null)
                return Array.Empty<string>();

            // Use ExpressionParser to extract columns
            var parser = new Expressions.ExpressionParser(Dialect, new Dictionary<string, object?>(), false);
            var columns = parser.ExtractColumns(lambda.Body);

            // Convert to database column names (snake_case)
            var columnNames = new string[columns.Count];
            for (int i = 0; i < columns.Count; i++)
            {
                // Remove dialect-specific wrapping (e.g., [column] or `column`)
                var col = columns[i];
                columnNames[i] = UnwrapColumnName(col);
            }

            return columnNames;
        }

        /// <summary>
        /// Removes dialect-specific column wrapping.
        /// </summary>
        private string UnwrapColumnName(string wrappedColumn)
        {
            // Remove common SQL wrapping characters
            var trimmed = wrappedColumn.Trim();
            if (trimmed.Length >= 2)
            {
                var first = trimmed[0];
                var last = trimmed[trimmed.Length - 1];

                // Handle [column], `column`, "column"
                if ((first == '[' && last == ']') ||
                    (first == '`' && last == '`') ||
                    (first == '"' && last == '"'))
                {
                    return trimmed.Substring(1, trimmed.Length - 2);
                }
            }

            return trimmed;
        }

        /// <summary>
        /// Extracts lambda expression from a quoted or unquoted expression.
        /// </summary>
        private static LambdaExpression? GetLambda(Expression expression)
        {
            return expression switch
            {
                LambdaExpression lambda => lambda,
                UnaryExpression { NodeType: ExpressionType.Quote, Operand: LambdaExpression lambda } => lambda,
                _ => null
            };
        }
    }
}
