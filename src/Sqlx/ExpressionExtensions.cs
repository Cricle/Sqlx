// -----------------------------------------------------------------------
// <copyright file="ExpressionExtensions.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Sqlx
{
    /// <summary>
    /// Extension methods for Expression to SQL conversion.
    /// </summary>
    public static class ExpressionExtensions
    {
        /// <summary>
        /// Converts a predicate expression to a SQL WHERE clause.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="predicate">The predicate expression.</param>
        /// <param name="dialect">SQL dialect (default: SQLite).</param>
        /// <returns>SQL WHERE clause without "WHERE" keyword.</returns>
        public static string ToWhereClause<T>(
            this Expression<Func<T, bool>> predicate,
            SqlDialect? dialect = null)
        {
            if (predicate == null)
            {
                return string.Empty;
            }

            return ExpressionToSql<T>.Create(dialect ?? SqlDefine.SQLite)
                .Where(predicate)
                .ToWhereClause();
        }

        /// <summary>
        /// Extracts parameters from a predicate expression.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="predicate">The predicate expression.</param>
        /// <returns>Dictionary of parameter names and values.</returns>
        public static Dictionary<string, object?> GetParameters<T>(
            this Expression<Func<T, bool>> predicate)
        {
            var parameters = new Dictionary<string, object?>();

            if (predicate != null)
            {
                ExtractParameters(predicate.Body, parameters);
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

                case ConstantExpression { Value: not null } constant:
                    parameters[$"p{parameters.Count}"] = constant.Value;
                    break;

                case MemberExpression { Expression: ConstantExpression } member:
                    var value = EvaluateExpression(member);
                    if (value != null)
                    {
                        parameters[$"p{parameters.Count}"] = value;
                    }

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

        private static object? EvaluateExpression(Expression expression)
        {
            try
            {
                return Expression.Lambda(expression).Compile().DynamicInvoke();
            }
            catch
            {
                return null;
            }
        }
    }
}
