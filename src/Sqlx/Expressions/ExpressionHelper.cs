// -----------------------------------------------------------------------
// <copyright file="ExpressionHelper.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
        
        // Cache for compiled expression evaluators to avoid repeated compilation
        private static readonly ConcurrentDictionary<Expression, Func<object?>> ExpressionCache = new();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEntityProperty(MemberExpression m) =>
            m.Expression is ParameterExpression ||
            (m.Expression is UnaryExpression { NodeType: ExpressionType.Convert, Operand: ParameterExpression }) ||
            IsNestedEntityProperty(m.Expression);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsBooleanMember(Expression e) => e is MemberExpression { Type: var t } && t == typeof(bool);

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object? GetMemberValueOptimized(MemberExpression m) =>
            m.Type.IsValueType ? GetDefaultValueForValueType(m.Type) : null;

        public static object? GetDefaultValueForValueType(Type t)
        {
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>)) return null;
            
            var typeCode = Type.GetTypeCode(t);
            return typeCode switch
            {
                TypeCode.Int32 or TypeCode.Int64 or TypeCode.Int16 or TypeCode.Byte => 0,
                TypeCode.Boolean => false,
                TypeCode.DateTime => DateTime.MinValue,
                TypeCode.Decimal => 0m,
                TypeCode.Double => 0.0,
                TypeCode.Single => 0f,
                _ => t == typeof(Guid) ? Guid.Empty : (t.IsValueType ? (t.IsGenericType ? null : 0) : null)
            };
        }

        public static object? EvaluateExpression(Expression e)
        {
            // Fast path for constants
            if (e is ConstantExpression c) return c.Value;
            
            // Check cache first to avoid repeated compilation
            if (!ExpressionCache.TryGetValue(e, out var evaluator))
            {
                // Compile and cache the expression evaluator
                evaluator = Expression.Lambda<Func<object?>>(Expression.Convert(e, typeof(object))).Compile();
                ExpressionCache.TryAdd(e, evaluator);
            }
            
            return evaluator();
        }

        public static string ConvertToSnakeCase(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            if (SnakeCaseCache.TryGetValue(name, out var cached)) return cached;

            // Quick check if conversion is needed
            if (!name.Any(char.IsUpper))
            {
                SnakeCaseCache.TryAdd(name, name);
                return name;
            }

            // Use stackalloc for small strings to avoid heap allocation
            var maxLen = name.Length * 2;
            Span<char> buffer = maxLen <= 128 ? stackalloc char[maxLen] : new char[maxLen];
            var pos = 0;

            for (var i = 0; i < name.Length; i++)
            {
                var c = name[i];
                if (char.IsUpper(c))
                {
                    if (i > 0) buffer[pos++] = '_';
                    buffer[pos++] = char.ToLowerInvariant(c);
                }
                else
                {
                    buffer[pos++] = c;
                }
            }

            var result = new string(buffer.Slice(0, pos));
            SnakeCaseCache.TryAdd(name, result);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string RemoveOuterParentheses(string s) =>
            s.Length >= 2 && s[0] == '(' && s[s.Length - 1] == ')' 
                ? s.Substring(1, s.Length - 2)
                : s;
    }
}
