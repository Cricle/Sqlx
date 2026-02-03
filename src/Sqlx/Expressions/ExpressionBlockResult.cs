// -----------------------------------------------------------------------
// <copyright file="ExpressionBlockResult.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
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
        /// Gets the placeholders that need to be replaced with actual values.
        /// Key: placeholder name, Value: parameter name in SQL (e.g., "@minAge")
        /// </summary>
        private Dictionary<string, string> Placeholders { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpressionBlockResult"/> class.
        /// </summary>
        /// <param name="sql">The generated SQL fragment.</param>
        /// <param name="parameters">The extracted parameters.</param>
        /// <param name="placeholders">The placeholders that need to be replaced.</param>
        private ExpressionBlockResult(string sql, Dictionary<string, object?> parameters, Dictionary<string, string>? placeholders = null)
        {
            Sql = sql ?? string.Empty;
            Parameters = parameters ?? new Dictionary<string, object?>();
            Placeholders = placeholders ?? new Dictionary<string, string>();
        }

        /// <summary>
        /// Copy the current result with optional new parameters.
        /// </summary>
        /// <param name="parameters">Override the paramters</param>
        /// <returns>The new result</returns>
        public ExpressionBlockResult Copy(Dictionary<string, object?>? parameters = null)
        {
            return new ExpressionBlockResult(Sql, new Dictionary<string, object?>(parameters ?? Parameters));
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
            var placeholders = new Dictionary<string, string>();
            var parser = new ExpressionParser(dialect, parameters, true, placeholders);
            var sql = parser.Parse(expression);

            return new ExpressionBlockResult(sql, parameters, placeholders);
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
            var placeholders = new Dictionary<string, string>();
            var parser = new ExpressionParser(dialect, parameters, true, placeholders);
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
            return new ExpressionBlockResult(sql, parameters, placeholders);
        }

        /// <summary>
        /// Replaces a placeholder with an actual parameter value.
        /// </summary>
        /// <param name="placeholderName">The name of the placeholder (e.g., "minAge").</param>
        /// <param name="value">The actual value to use.</param>
        /// <returns>The current ExpressionBlockResult for method chaining.</returns>
        /// <exception cref="ArgumentException">Thrown when the placeholder name is not found.</exception>
        public ExpressionBlockResult WithParameter(string placeholderName, object? value)
        {
            if (!Placeholders.TryGetValue(placeholderName, out var paramName))
            {
                throw new ArgumentException($"Placeholder '{placeholderName}' not found in expression. Available placeholders: {string.Join(", ", Placeholders.Keys)}", nameof(placeholderName));
            }

            // Add or update the parameter value
            Parameters[paramName] = value;

            return this;
        }

        /// <summary>
        /// Replaces multiple placeholders with actual parameter values.
        /// </summary>
        /// <param name="parameters">Dictionary of placeholder names and their values.</param>
        /// <returns>The current ExpressionBlockResult for method chaining.</returns>
        public ExpressionBlockResult WithParameters(Dictionary<string, object?> parameters)
        {
            if (parameters == null)
            {
                return this;
            }

            foreach (var kvp in parameters)
            {
                WithParameter(kvp.Key, kvp.Value);
            }

            return this;
        }

        /// <summary>
        /// Gets the list of placeholder names that need to be filled.
        /// </summary>
        /// <returns>A list of placeholder names.</returns>
        public IReadOnlyList<string> GetPlaceholderNames()
        {
            return Placeholders.Keys.ToList();
        }

        /// <summary>
        /// Checks if all placeholders have been filled with values.
        /// </summary>
        /// <returns>True if all placeholders are filled, false otherwise.</returns>
        public bool AreAllPlaceholdersFilled()
        {
            foreach (var paramName in Placeholders.Values)
            {
                if (!Parameters.ContainsKey(paramName))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Creates an empty result.
        /// </summary>
        public static ExpressionBlockResult Empty => new ExpressionBlockResult(string.Empty, new Dictionary<string, object?>());
    }

    /// <summary>
    /// Provides placeholder methods for use in expression templates.
    /// These methods are markers that will be replaced with actual parameters at runtime.
    /// </summary>
    public static class Any
    {
        /// <summary>
        /// Creates a placeholder for a value of type T.
        /// This method should only be used in expression trees and will be replaced at runtime.
        /// </summary>
        /// <typeparam name="T">The type of the placeholder value.</typeparam>
        /// <param name="name">The name of the placeholder.</param>
        /// <returns>Default value of T (this is never actually executed).</returns>
        /// <example>
        /// <code>
        /// Expression&lt;Func&lt;User, bool&gt;&gt; template = u => u.Age > Any&lt;int&gt;("minAge");
        /// var result = ExpressionBlockResult.Parse(template.Body, dialect)
        ///     .WithParameter("minAge", 18);
        /// </code>
        /// </example>
        public static T Value<T>(string name)
        {
            throw new InvalidOperationException($"Any.Value<{typeof(T).Name}>(\"{name}\") is a placeholder and should not be executed directly. It should only be used in expression trees.");
        }
    }
}
