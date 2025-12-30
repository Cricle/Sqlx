using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Sqlx
{
    /// <summary>
    /// Extension methods for Expression to SQL conversion
    /// </summary>
    public static class ExpressionExtensions
    {
        /// <summary>
        /// Converts a predicate expression to a SQL WHERE clause
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <param name="predicate">The predicate expression</param>
        /// <param name="dialect">SQL dialect (default: SQLite)</param>
        /// <returns>SQL WHERE clause without "WHERE" keyword</returns>
        public static string ToWhereClause<T>(
            this Expression<Func<T, bool>> predicate,
            SqlDialect? dialect = null)
        {
            if (predicate == null)
            {
                return string.Empty;
            }

            var actualDialect = dialect ?? SqlDefine.SQLite;
            var converter = ExpressionToSql<T>.Create(actualDialect);
            converter.Where(predicate);
            
            // Use ToWhereClause() to get just the WHERE condition
            return converter.ToWhereClause();
        }

        /// <summary>
        /// Extracts parameters from a predicate expression
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <param name="predicate">The predicate expression</param>
        /// <returns>Dictionary of parameter names and values</returns>
        public static Dictionary<string, object?> GetParameters<T>(
            this Expression<Func<T, bool>> predicate)
        {
            var parameters = new Dictionary<string, object?>();
            
            if (predicate == null)
            {
                return parameters;
            }

            // Extract parameters from the expression
            ExtractParameters(predicate.Body, parameters);
            
            return parameters;
        }

        private static void ExtractParameters(Expression expression, Dictionary<string, object?> parameters)
        {
            if (expression == null)
            {
                return;
            }

            switch (expression.NodeType)
            {
                case ExpressionType.Equal:
                case ExpressionType.NotEqual:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                    var binary = (BinaryExpression)expression;
                    ExtractParameters(binary.Left, parameters);
                    ExtractParameters(binary.Right, parameters);
                    break;

                case ExpressionType.AndAlso:
                case ExpressionType.OrElse:
                    var logicalBinary = (BinaryExpression)expression;
                    ExtractParameters(logicalBinary.Left, parameters);
                    ExtractParameters(logicalBinary.Right, parameters);
                    break;

                case ExpressionType.Constant:
                    var constant = (ConstantExpression)expression;
                    if (constant.Value != null)
                    {
                        var paramName = $"p{parameters.Count}";
                        parameters[paramName] = constant.Value;
                    }
                    break;

                case ExpressionType.MemberAccess:
                    var member = (MemberExpression)expression;
                    if (member.Expression != null && member.Expression.NodeType == ExpressionType.Constant)
                    {
                        var constExpr = (ConstantExpression)member.Expression;
                        var value = GetMemberValue(member, constExpr.Value);
                        if (value != null)
                        {
                            var paramName = $"p{parameters.Count}";
                            parameters[paramName] = value;
                        }
                    }
                    break;

                case ExpressionType.Call:
                    var methodCall = (MethodCallExpression)expression;
                    if (methodCall.Object != null)
                    {
                        ExtractParameters(methodCall.Object, parameters);
                    }
                    foreach (var arg in methodCall.Arguments)
                    {
                        ExtractParameters(arg, parameters);
                    }
                    break;

                case ExpressionType.Not:
                    var unary = (UnaryExpression)expression;
                    ExtractParameters(unary.Operand, parameters);
                    break;
            }
        }

        private static object? GetMemberValue(MemberExpression member, object? container)
        {
            if (container == null)
            {
                return null;
            }

            if (member.Member is System.Reflection.FieldInfo field)
            {
                return field.GetValue(container);
            }

            if (member.Member is System.Reflection.PropertyInfo property)
            {
                return property.GetValue(container);
            }

            return null;
        }
    }
}

