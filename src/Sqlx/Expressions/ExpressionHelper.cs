// -----------------------------------------------------------------------
// <copyright file="ExpressionHelper.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Sqlx.Expressions
{
    /// <summary>
    /// Helper methods for expression analysis and evaluation.
    /// </summary>
    internal static class ExpressionHelper
    {
        public static bool IsEntityProperty(MemberExpression m) => m.Expression is ParameterExpression;
        public static bool IsBooleanMember(Expression e) => e is MemberExpression { Type: var t } && t == typeof(bool);
        public static bool IsConstantTrue(Expression e) => e is ConstantExpression { Value: true };
        public static bool IsConstantFalse(Expression e) => e is ConstantExpression { Value: false };
        public static bool IsStringType(Type t) => t == typeof(string);
        public static bool IsStringConcatenation(BinaryExpression b) => b.Type == typeof(string) && b.NodeType == ExpressionType.Add;

        public static bool IsStringPropertyAccess(MemberExpression m) =>
            m.Member.Name == "Length" && m.Expression is MemberExpression { Type: var t } sm && t == typeof(string) && IsEntityProperty(sm);

        public static bool IsCollectionType(Type t) =>
            t.IsArray || (t.IsGenericType && t.GetGenericTypeDefinition() is var g &&
                (g == typeof(List<>) || g == typeof(IEnumerable<>) || g == typeof(ICollection<>) || g == typeof(IList<>)));

        public static bool IsAggregateContext(MethodCallExpression m) =>
            m.Method.Name is "Count" or "CountDistinct" or "Sum" or "Average" or "Avg" or "Max" or "Min" or "StringAgg";

        public static bool IsAnyPlaceholder(MethodCallExpression m) =>
            m.Method.DeclaringType?.Name == "Any" && m.Method.DeclaringType?.Namespace == "Sqlx";

        public static object? GetMemberValueOptimized(MemberExpression m) =>
            m.Type.IsValueType ? GetDefaultValueForValueType(m.Type) : null;

        public static object? GetDefaultValueForValueType(Type t)
        {
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>)) return null;
            if (t == typeof(int) || t == typeof(long) || t == typeof(short) || t == typeof(byte)) return 0;
            if (t == typeof(bool)) return false;
            if (t == typeof(DateTime)) return DateTime.MinValue;
            if (t == typeof(Guid)) return Guid.Empty;
            if (t == typeof(decimal)) return 0m;
            if (t == typeof(double)) return 0.0;
            if (t == typeof(float)) return 0f;
            return t.IsValueType ? (t.IsGenericType ? null : 0) : null;
        }

        public static object? EvaluateExpression(Expression e) => e switch
        {
            ConstantExpression c => c.Value,
            MemberExpression m when m.Expression is ConstantExpression c => m.Member switch
            {
                FieldInfo f => f.GetValue(c.Value),
                PropertyInfo p => p.GetValue(c.Value),
                _ => throw new NotSupportedException($"Member type {m.Member.GetType()} is not supported")
            },
            _ => Expression.Lambda<Func<object?>>(Expression.Convert(e, typeof(object))).Compile()()
        };

        public static string ConvertToSnakeCase(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            var hasUpper = false;
            for (var i = 0; i < name.Length; i++) if (char.IsUpper(name[i])) { hasUpper = true; break; }
            if (!hasUpper) return name;

            var sb = new StringBuilder(name.Length + 4);
            for (var i = 0; i < name.Length; i++)
            {
                var c = name[i];
                if (char.IsUpper(c)) { if (i > 0) sb.Append('_'); sb.Append(char.ToLowerInvariant(c)); }
                else sb.Append(c);
            }
            return sb.ToString();
        }

        public static string RemoveOuterParentheses(string s) =>
            s.StartsWith("(") && s.EndsWith(")") ? s.Substring(1, s.Length - 2) : s;
    }
}
