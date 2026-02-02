// -----------------------------------------------------------------------
// <copyright file="SetExpressionExtensions.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Sqlx.Expressions;

namespace Sqlx
{
    /// <summary>
    /// Extension methods for converting update expressions to SQL SET clauses.
    /// </summary>
    public static class SetExpressionExtensions
    {
        /// <summary>
        /// Converts an update expression to a SQL SET clause.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="updateExpression">The update expression (e.g., u => new User { Name = "John", Version = u.Version + 1 }).</param>
        /// <param name="dialect">SQL dialect (default: SQLite).</param>
        /// <returns>SQL SET clause without "SET" keyword (e.g., "[name] = @p0, [version] = ([version] + @p1)").</returns>
        /// <example>
        /// <code>
        /// // Simple field update
        /// Expression&lt;Func&lt;User, User&gt;&gt; expr = u => new User { Name = "John" };
        /// var setClause = expr.ToSetClause(); // "[name] = @p0"
        /// 
        /// // Increment expression
        /// Expression&lt;Func&lt;User, User&gt;&gt; expr = u => new User { Version = u.Version + 1 };
        /// var setClause = expr.ToSetClause(); // "[version] = ([version] + @p0)"
        /// 
        /// // Mixed update
        /// Expression&lt;Func&lt;User, User&gt;&gt; expr = u => new User { Name = "John", Version = u.Version + 1 };
        /// var setClause = expr.ToSetClause(); // "[name] = @p0, [version] = ([version] + @p1)"
        /// </code>
        /// </example>
        public static string ToSetClause<T>(
            this Expression<Func<T, T>> updateExpression,
            SqlDialect? dialect = null)
        {
            if (updateExpression == null)
            {
                return string.Empty;
            }

            var d = dialect ?? SqlDefine.SQLite;
            var parameters = new Dictionary<string, object?>();
            var parser = new ExpressionParser(d, parameters, true);

            // The body should be a MemberInitExpression (new T { ... })
            if (updateExpression.Body is not MemberInitExpression memberInit)
            {
                throw new ArgumentException(
                    "Update expression must be a member initialization expression (e.g., u => new User { Name = \"John\" })",
                    nameof(updateExpression));
            }

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
                var wrappedColumn = d.WrapColumn(columnName);

                // Parse the value expression
                var valueExpression = assignment.Expression;
                var valueSql = parser.ParseRaw(valueExpression);

                setClauses.Add($"{wrappedColumn} = {valueSql}");
            }

            return string.Join(", ", setClauses);
        }

        /// <summary>
        /// Extracts parameters from an update expression.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="updateExpression">The update expression.</param>
        /// <returns>Dictionary of parameter names and values.</returns>
        public static Dictionary<string, object?> GetSetParameters<T>(
            this Expression<Func<T, T>> updateExpression)
        {
            var parameters = new Dictionary<string, object?>();

            if (updateExpression?.Body is not MemberInitExpression memberInit)
            {
                return parameters;
            }

            foreach (var binding in memberInit.Bindings)
            {
                if (binding is not MemberAssignment assignment)
                {
                    continue;
                }

                ExtractParameters(assignment.Expression, parameters);
            }

            return parameters;
        }

        private static void ExtractParameters(Expression? expression, Dictionary<string, object?> parameters)
        {
            if (expression == null)
            {
                return;
            }

            switch (expression)
            {
                case BinaryExpression binary:
                    ExtractParameters(binary.Left, parameters);
                    ExtractParameters(binary.Right, parameters);
                    break;

                case ConstantExpression constant:
                    // 包括 null 值
                    parameters[$"p{parameters.Count}"] = constant.Value;
                    break;

                case MemberExpression { Expression: not ParameterExpression } member:
                    var value = ExpressionHelper.EvaluateExpression(member);
                    // 包括 null 值
                    parameters[$"p{parameters.Count}"] = value;
                    break;

                case MethodCallExpression methodCall:
                    if (methodCall.Object != null)
                    {
                        ExtractParameters(methodCall.Object, parameters);
                    }

                    foreach (var arg in methodCall.Arguments)
                    {
                        ExtractParameters(arg, parameters);
                    }

                    break;

                case UnaryExpression unary:
                    ExtractParameters(unary.Operand, parameters);
                    break;
            }
        }
    }
}
