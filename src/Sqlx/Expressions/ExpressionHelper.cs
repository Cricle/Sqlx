// -----------------------------------------------------------------------
// <copyright file="ExpressionHelper.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Sqlx.Expressions
{
    /// <summary>
    /// Helper methods for expression analysis and evaluation.
    /// </summary>
    internal static class ExpressionHelper
    {
        // Cache for snake_case conversions to avoid repeated allocations
        private static readonly ConcurrentDictionary<string, string> SnakeCaseCache = new();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEntityProperty(MemberExpression m) =>
            m.Expression is ParameterExpression ||
            (m.Expression is UnaryExpression { NodeType: ExpressionType.Convert, Operand: ParameterExpression }) ||
            IsNestedEntityProperty(m.Expression);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsBooleanMember(Expression e) => e is MemberExpression { Type: var t } && t == typeof(bool);

        public static Type? FindRootParameterType(Expression? expr)
        {
            while (expr != null)
            {
                switch (expr)
                {
                    case ParameterExpression parameter:
                        return parameter.Type;
                    case MemberExpression member:
                        expr = member.Expression;
                        break;
                    case UnaryExpression { NodeType: ExpressionType.Convert } unary:
                        expr = unary.Operand;
                        break;
                    case MethodCallExpression methodCall when methodCall.Object != null:
                        expr = methodCall.Object;
                        break;
                    default:
                        return null;
                }
            }

            return null;
        }

        /// <summary>
        /// Checks if the expression is a nested member access chain that ultimately leads to a parameter.
        /// This handles cases like x.User.Name where x is the parameter, User is an anonymous type property,
        /// and Name is the actual column we want to access.
        /// </summary>
        private static bool IsNestedEntityProperty(Expression? expr)
        {
            while (expr != null)
            {
                switch (expr)
                {
                    case ParameterExpression:
                        return true;
                    case MemberExpression m:
                        expr = m.Expression;
                        break;
                    case UnaryExpression { NodeType: ExpressionType.Convert } u:
                        expr = u.Operand;
                        break;
                    default:
                        return false;
                }
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsStringType(Type t) => t == typeof(string);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsStringConcatenation(BinaryExpression b) => b.Type == typeof(string) && b.NodeType == ExpressionType.Add;

        public static bool IsStringPropertyAccess(MemberExpression m) =>
            m.Member.Name == "Length" && m.Expression is MemberExpression { Type: var t } && t == typeof(string) && IsEntityProperty((MemberExpression)m.Expression);

        public static bool IsCollectionType(Type t) =>
            t.IsArray || (t.IsGenericType && t.GetGenericTypeDefinition() is var g &&
                (g == typeof(List<>) || g == typeof(IEnumerable<>) || g == typeof(ICollection<>) || g == typeof(IList<>)));

        public static bool IsAggregateContext(MethodCallExpression m) =>
            m.Method.DeclaringType != typeof(Math) &&
            m.Method.Name is "Count" or "CountDistinct" or "Sum" or "Average" or "Avg" or "Max" or "Min" or "StringAgg";

        public static object? GetMemberValueOptimized(MemberExpression m)
        {
            if (TryEvaluateMemberValue(m, out var value))
            {
                return value;
            }

            return EvaluateExpression(m);
        }

        public static object? EvaluateExpression(Expression e)
        {
            // Fast path for constants
            if (e is ConstantExpression c) return c.Value;
            return Expression.Lambda<Func<object?>>(Expression.Convert(e, typeof(object))).Compile()();
        }

        private static bool TryEvaluateMemberValue(Expression? expression, out object? value)
        {
            switch (expression)
            {
                case null:
                    value = null;
                    return true;

                case ConstantExpression constant:
                    value = constant.Value;
                    return true;

                case MemberExpression member:
                    if (!TryEvaluateMemberValue(member.Expression, out var target))
                    {
                        value = null;
                        return false;
                    }

                    switch (member.Member)
                    {
                        case FieldInfo field:
                            value = field.GetValue(target);
                            return true;

                        case PropertyInfo property when property.GetIndexParameters().Length == 0:
                            value = property.GetValue(target);
                            return true;

                        default:
                            value = null;
                            return false;
                    }

                case UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } unary:
                    return TryEvaluateMemberValue(unary.Operand, out value);

                default:
                    value = null;
                    return false;
            }
        }

        public static string ConvertToSnakeCase(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            if (SnakeCaseCache.TryGetValue(name, out var cached)) return cached;

            // Quick check if conversion is needed
            if (!ContainsUppercase(name))
            {
                SnakeCaseCache.TryAdd(name, name);
                return name;
            }

            var result = ConvertToSnakeCaseCore(name);
            SnakeCaseCache.TryAdd(name, result);
            return result;
        }

        internal static string ConvertToSnakeCaseCore(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;

            // Use stackalloc for small strings to avoid heap allocation
            var maxLen = name.Length * 2;
            Span<char> buffer = maxLen <= 128 ? stackalloc char[maxLen] : new char[maxLen];
            var pos = 0;

            for (var i = 0; i < name.Length; i++)
            {
                var c = name[i];
                if (char.IsUpper(c))
                {
                    if (i > 0)
                    {
                        var prevChar = name[i - 1];
                        var nextChar = i + 1 < name.Length ? name[i + 1] : '\0';

                        if (char.IsLower(prevChar) || char.IsDigit(prevChar) ||
                            (char.IsUpper(prevChar) && char.IsLower(nextChar)))
                        {
                            buffer[pos++] = '_';
                        }
                    }

                    buffer[pos++] = char.ToLowerInvariant(c);
                }
                else
                {
                    buffer[pos++] = c;
                }
            }

            return new string(buffer.Slice(0, pos));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string RemoveOuterParentheses(string s) =>
            s.Length >= 2 && s[0] == '(' && s[s.Length - 1] == ')' 
                ? s.Substring(1, s.Length - 2)
                : s;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool ContainsUppercase(string name)
        {
            for (var i = 0; i < name.Length; i++)
            {
                if (char.IsUpper(name[i]))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
