// -----------------------------------------------------------------------
// <copyright file="FunctionParsers.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq.Expressions;

namespace Sqlx.Expressions
{
    /// <summary>
    /// Parses Math.* method calls to SQL functions.
    /// </summary>
    internal static class MathFunctionParser
    {
        public static string Parse(ExpressionParser p, MethodCallExpression m)
        {
            var db = p.DatabaseType;
            return (m.Method.Name, m.Arguments.Count) switch
            {
                ("Abs", 1) => $"ABS({p.ParseRaw(m.Arguments[0])})",
                ("Round", 1) => $"ROUND({p.ParseRaw(m.Arguments[0])})",
                ("Round", 2) => $"ROUND({p.ParseRaw(m.Arguments[0])}, {p.ParseRaw(m.Arguments[1])})",
                ("Floor", 1) => $"FLOOR({p.ParseRaw(m.Arguments[0])})",
                ("Sqrt", 1) => $"SQRT({p.ParseRaw(m.Arguments[0])})",
                ("Ceiling", 1) => db == "PostgreSql" ? $"CEIL({p.ParseRaw(m.Arguments[0])})" : $"CEILING({p.ParseRaw(m.Arguments[0])})",
                ("Min", 2) => $"LEAST({p.ParseRaw(m.Arguments[0])}, {p.ParseRaw(m.Arguments[1])})",
                ("Max", 2) => $"GREATEST({p.ParseRaw(m.Arguments[0])}, {p.ParseRaw(m.Arguments[1])})",
                ("Pow", 2) => db == "MySql" ? $"POW({p.ParseRaw(m.Arguments[0])}, {p.ParseRaw(m.Arguments[1])})" : $"POWER({p.ParseRaw(m.Arguments[0])}, {p.ParseRaw(m.Arguments[1])})",
                _ => "1"
            };
        }
    }

    /// <summary>
    /// Parses string method calls to SQL functions.
    /// </summary>
    internal static class StringFunctionParser
    {
        public static string Parse(ExpressionParser p, MethodCallExpression m)
        {
            var obj = m.Object != null ? p.ParseRaw(m.Object) : string.Empty;
            var db = p.DatabaseType;
            var d = p.Dialect;
            return (m.Method.Name, m.Arguments.Count) switch
            {
                ("Contains", 1) => $"{obj} LIKE {d.Concat("'%'", p.ParseRaw(m.Arguments[0]), "'%'")}",
                ("StartsWith", 1) => $"{obj} LIKE {d.Concat(p.ParseRaw(m.Arguments[0]), "'%'")}",
                ("EndsWith", 1) => $"{obj} LIKE {d.Concat("'%'", p.ParseRaw(m.Arguments[0]))}",
                ("ToUpper", 0) => $"UPPER({obj})",
                ("ToLower", 0) => $"LOWER({obj})",
                ("Trim", 0) => $"TRIM({obj})",
                ("Replace", 2) => $"REPLACE({obj}, {p.ParseRaw(m.Arguments[0])}, {p.ParseRaw(m.Arguments[1])})",
                ("Substring", 1) => db == "SQLite" ? $"SUBSTR({obj}, {p.ParseRaw(m.Arguments[0])})" : $"SUBSTRING({obj}, {p.ParseRaw(m.Arguments[0])})",
                ("Substring", 2) => db == "SQLite" ? $"SUBSTR({obj}, {p.ParseRaw(m.Arguments[0])}, {p.ParseRaw(m.Arguments[1])})" : $"SUBSTRING({obj}, {p.ParseRaw(m.Arguments[0])}, {p.ParseRaw(m.Arguments[1])})",
                ("Length", 0) => db == "SqlServer" ? $"LEN({obj})" : $"LENGTH({obj})",
                _ => obj
            };
        }
    }

    /// <summary>
    /// Parses DateTime method calls to SQL functions.
    /// </summary>
    internal static class DateTimeFunctionParser
    {
        public static string Parse(ExpressionParser p, MethodCallExpression m)
        {
            var obj = m.Object != null ? p.ParseRaw(m.Object) : string.Empty;
            if (p.DatabaseType == "SqlServer" && m.Method.Name.StartsWith("Add") && m.Arguments.Count == 1)
                return $"DATEADD({m.Method.Name.Substring(3).ToUpperInvariant()}, {p.ParseRaw(m.Arguments[0])}, {obj})";
            return obj;
        }
    }

    /// <summary>
    /// Parses aggregate function calls to SQL.
    /// </summary>
    internal static class AggregateParser
    {
        public static string Parse(ExpressionParser p, MethodCallExpression m)
        {
            var name = m.Method.Name;
            var hasArg = m.Arguments.Count > 1;
            var arg = hasArg ? p.ParseLambda(m.Arguments[1]) : null;
            return name switch
            {
                "Count" => "COUNT(*)",
                "CountDistinct" when hasArg => $"COUNT(DISTINCT {arg})",
                "Sum" when hasArg => $"SUM({arg})",
                "Average" or "Avg" when hasArg => $"AVG({arg})",
                "Max" when hasArg => $"MAX({arg})",
                "Min" when hasArg => $"MIN({arg})",
                "StringAgg" when m.Arguments.Count > 2 => GetStringAgg(p.DatabaseType, arg!, p.ParseLambda(m.Arguments[2])),
                _ => throw new NotSupportedException($"Aggregate function {name} is not supported"),
            };
        }

        private static string GetStringAgg(string db, string col, string sep) => db switch
        {
            "MySql" => $"GROUP_CONCAT({col} SEPARATOR {sep})",
            "SQLite" or "SqlServer" => $"GROUP_CONCAT({col}, {sep})",
            "Oracle" => $"LISTAGG({col}, {sep}) WITHIN GROUP (ORDER BY {col})",
            _ => $"STRING_AGG({col}, {sep})"
        };
    }

    /// <summary>
    /// Formats values as SQL literals.
    /// </summary>
    internal static class ValueFormatter
    {
        public static string FormatAsLiteral(SqlDialect d, object? v) => v switch
        {
            null => "NULL",
            string s => d.WrapString(s.Replace("'", "''")),
            bool b => GetBooleanLiteral(d, b),
            DateTime dt => d.WrapString(dt.ToString("yyyy-MM-dd HH:mm:ss")),
            Guid g => d.WrapString(g.ToString()),
            decimal or double or float => v.ToString()!,
            _ => v?.ToString() ?? "NULL"
        };

        public static string GetBooleanLiteral(SqlDialect d, bool v) =>
            d.DatabaseType == "PostgreSql" ? (v ? "true" : "false") : (v ? "1" : "0");
    }
}
