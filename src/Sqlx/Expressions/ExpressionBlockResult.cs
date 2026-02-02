// -----------------------------------------------------------------------
// <copyright file="ExpressionBlockResult.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Sqlx.Expressions
{
    /// <summary>
    /// Represents the result of parsing an expression, containing both SQL and parameters.
    /// This class provides a unified way to parse expressions for WHERE and UPDATE scenarios,
    /// avoiding duplicate parsing and improving performance.
    /// </summary>
    public sealed class ExpressionBlockResult
    {
        /// <summary>
        /// Gets the generated SQL fragment.
        /// </summary>
        public string Sql { get; }

        /// <summary>
        /// Gets the extracted parameters from the expression.
        /// </summary>
        public Dictionary<string, object?> Parameters { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpressionBlockResult"/> class.
        /// </summary>
        /// <param name="sql">The generated SQL fragment.</param>
        /// <param name="parameters">The extracted parameters.</param>
        public ExpressionBlockResult(string sql, Dictionary<string, object?> parameters)
        {
            Sql = sql ?? string.Empty;
            Parameters = parameters ?? new Dictionary<string, object?>();
        }

        /// <summary>
        /// Parses a WHERE predicate expression into SQL and parameters.
        /// </summary>
        /// <param name="expression">The expression to parse (typically from Expression&lt;Func&lt;T, bool&gt;&gt;.Body).</param>
        /// <param name="dialect">The SQL dialect to use.</param>
        /// <returns>An ExpressionBlockResult containing the SQL and parameters.</returns>
        public static ExpressionBlockResult Parse(Expression? expression, SqlDialect dialect)
        {
            if (expression == null)
            {
                return new ExpressionBlockResult(string.Empty, new Dictionary<string, object?>());
            }

            var parameters = new Dictionary<string, object?>();
            var parser = new ExpressionParser(dialect, parameters, true);
            var sql = parser.Parse(expression);

            return new ExpressionBlockResult(sql, parameters);
        }

        /// <summary>
        /// Parses an UPDATE expression into SQL SET clause and parameters.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="updateExpression">The update expression (e.g., u => new User { Name = "John" }).</param>
        /// <param name="dialect">The SQL dialect to use.</param>
        /// <returns>An ExpressionBlockResult containing the SET clause SQL and parameters.</returns>
        public static ExpressionBlockResult ParseUpdate<T>(Expression<Func<T, T>>? updateExpression, SqlDialect dialect)
        {
            if (updateExpression == null)
            {
                return new ExpressionBlockResult(string.Empty, new Dictionary<string, object?>());
            }

            // The body should be a MemberInitExpression (new T { ... })
            if (updateExpression.Body is not MemberInitExpression memberInit)
            {
                return new ExpressionBlockResult(string.Empty, new Dictionary<string, object?>());
            }

            var parameters = new Dictionary<string, object?>();
            var parser = new ExpressionParser(dialect, parameters, true);
            var setClauses = new List<string>(memberInit.Bindings.Count);

            foreach (var binding in memberInit.Bindings)
            {
                if (binding is not MemberAssignment assignment)
                {
                    continue;
                }

                // Get column name from property name
                var propertyName = assignment.Member.Name;
                var columnName = ExpressionHelper.ConvertToSnakeCase(propertyName);
                var wrappedColumn = dialect.WrapColumn(columnName);

                // Parse the value expression
                var valueExpression = assignment.Expression;
                var valueSql = parser.ParseRaw(valueExpression);

                setClauses.Add($"{wrappedColumn} = {valueSql}");
            }

            var sql = string.Join(", ", setClauses);
            return new ExpressionBlockResult(sql, parameters);
        }

        /// <summary>
        /// Creates an empty result.
        /// </summary>
        public static ExpressionBlockResult Empty => new ExpressionBlockResult(string.Empty, new Dictionary<string, object?>());
    }
}
