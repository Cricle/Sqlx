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
            var d = p.Dialect;
            var a = m.Arguments;
            return (m.Method.Name, a.Count) switch
            {
                // Basic
                ("Abs", 1) => d.Abs(p.ParseRaw(a[0])),
                ("Sign", 1) => $"SIGN({p.ParseRaw(a[0])})",

                // Rounding
                ("Round", 1) => $"ROUND({p.ParseRaw(a[0])})",
                ("Round", 2) => d.Round(p.ParseRaw(a[0]), p.ParseRaw(a[1])),
                ("Floor", 1) => d.Floor(p.ParseRaw(a[0])),
                ("Ceiling", 1) => d.Ceiling(p.ParseRaw(a[0])),
                ("Truncate", 1) => GetTruncate(d, p.ParseRaw(a[0])),

                // Power & Root
                ("Sqrt", 1) => $"SQRT({p.ParseRaw(a[0])})",
                ("Pow", 2) => d.DatabaseType == "MySql" ? $"POW({p.ParseRaw(a[0])}, {p.ParseRaw(a[1])})" : $"POWER({p.ParseRaw(a[0])}, {p.ParseRaw(a[1])})",
                ("Exp", 1) => $"EXP({p.ParseRaw(a[0])})",

                // Logarithm
                ("Log", 1) => d.DatabaseType == "SqlServer" ? $"LOG({p.ParseRaw(a[0])})" : $"LN({p.ParseRaw(a[0])})",
                ("Log", 2) => $"LOG({p.ParseRaw(a[1])}, {p.ParseRaw(a[0])})", // Math.Log(value, base) -> LOG(base, value)
                ("Log10", 1) => $"LOG10({p.ParseRaw(a[0])})",

                // Trigonometric
                ("Sin", 1) => $"SIN({p.ParseRaw(a[0])})",
                ("Cos", 1) => $"COS({p.ParseRaw(a[0])})",
                ("Tan", 1) => $"TAN({p.ParseRaw(a[0])})",
                ("Asin", 1) => $"ASIN({p.ParseRaw(a[0])})",
                ("Acos", 1) => $"ACOS({p.ParseRaw(a[0])})",
                ("Atan", 1) => $"ATAN({p.ParseRaw(a[0])})",
                ("Atan2", 2) => d.DatabaseType == "SqlServer" ? $"ATN2({p.ParseRaw(a[0])}, {p.ParseRaw(a[1])})" : $"ATAN2({p.ParseRaw(a[0])}, {p.ParseRaw(a[1])})",

                // Min/Max
                ("Min", 2) => $"LEAST({p.ParseRaw(a[0])}, {p.ParseRaw(a[1])})",
                ("Max", 2) => $"GREATEST({p.ParseRaw(a[0])}, {p.ParseRaw(a[1])})",

                _ => "1"
            };
        }

        private static string GetTruncate(SqlDialect d, string arg) => d.DatabaseType switch
        {
            "SqlServer" => d.Round(arg, "0, 1"),
            "PostgreSql" or "Oracle" => $"TRUNC({arg})",
            _ => $"TRUNCATE({arg}, 0)"
        };
    }

    /// <summary>
    /// Parses string method calls to SQL functions with multi-database support.
    /// </summary>
    internal static class StringFunctionParser
    {
        public static string Parse(ExpressionParser p, MethodCallExpression m)
        {
            var obj = m.Object != null ? p.ParseRaw(m.Object) : string.Empty;
            var d = p.Dialect;
            var args = m.Arguments;

            return (m.Method.Name, args.Count) switch
            {
                // Pattern matching
                ("Contains", 1) => $"{obj} LIKE {d.Concat("'%'", WrapArg(p, args[0]), "'%'")}",
                ("StartsWith", 1) => $"{obj} LIKE {d.Concat(WrapArg(p, args[0]), "'%'")}",
                ("EndsWith", 1) => $"{obj} LIKE {d.Concat("'%'", WrapArg(p, args[0]))}",

                // Case conversion
                ("ToUpper", 0) => d.Upper(obj),
                ("ToLower", 0) => d.Lower(obj),

                // Trimming
                ("Trim", 0) => d.Trim(obj),
                ("TrimStart", 0) => d.LTrim(obj),
                ("TrimEnd", 0) => d.RTrim(obj),

                // Replace
                ("Replace", 2) => d.Replace(obj, WrapArg(p, args[0]), WrapArg(p, args[1])),

                // Substring
                ("Substring", 1) => GetSubstring(d, obj, p.ParseRaw(args[0]), null),
                ("Substring", 2) => GetSubstring(d, obj, p.ParseRaw(args[0]), p.ParseRaw(args[1])),

                // Length
                ("Length", 0) => d.Length(obj),

                // Padding
                ("PadLeft", 1) => GetPadLeft(d, obj, p.ParseRaw(args[0]), "' '"),
                ("PadLeft", 2) => GetPadLeft(d, obj, p.ParseRaw(args[0]), WrapArg(p, args[1])),
                ("PadRight", 1) => GetPadRight(d, obj, p.ParseRaw(args[0]), "' '"),
                ("PadRight", 2) => GetPadRight(d, obj, p.ParseRaw(args[0]), WrapArg(p, args[1])),

                // IndexOf
                ("IndexOf", 1) => GetIndexOf(d, obj, WrapArg(p, args[0])),
                ("IndexOf", 2) => GetIndexOfWithStart(d, obj, WrapArg(p, args[0]), p.ParseRaw(args[1])),

                _ => obj
            };
        }

        /// <summary>Parses string indexer access (str[index]).</summary>
        public static string ParseIndexer(ExpressionParser p, MethodCallExpression m)
        {
            if (m.Object == null || m.Arguments.Count != 1) return "NULL";
            var obj = p.ParseRaw(m.Object);
            var idx = p.ParseRaw(m.Arguments[0]);
            // SQL is 1-based, C# is 0-based
            return p.Dialect.Substring(obj, $"{idx} + 1", "1");
        }

        private static string WrapArg(ExpressionParser p, Expression arg)
        {
            var raw = p.ParseRaw(arg);
            // If it's already a quoted string or a column reference, return as-is
            if (raw.StartsWith("'") || raw.StartsWith("[") || raw.StartsWith("\"") || raw.StartsWith("`") || raw.StartsWith("@") || raw.StartsWith("$") || raw.StartsWith(":"))
                return raw;
            // Otherwise wrap as string literal
            return p.Dialect.WrapString(raw.Replace("'", "''"));
        }

        private static string GetSubstring(SqlDialect d, string obj, string start, string? length)
        {
            // SQL is 1-based, C# is 0-based
            var sqlStart = $"{start} + 1";
            return length != null 
                ? d.Substring(obj, sqlStart, length) 
                : d.Substring(obj, sqlStart, $"LENGTH({obj})");
        }

        private static string GetPadLeft(SqlDialect d, string obj, string totalWidth, string padChar) => d.DatabaseType switch
        {
            "SqlServer" => $"RIGHT(REPLICATE({padChar}, {totalWidth}) + {obj}, {totalWidth})",
            "SQLite" => $"SUBSTR(REPLACE(HEX(ZEROBLOB({totalWidth})), '00', {padChar}) || {obj}, -({totalWidth}))",
            _ => $"LPAD({obj}, {totalWidth}, {padChar})"
        };

        private static string GetPadRight(SqlDialect d, string obj, string totalWidth, string padChar) => d.DatabaseType switch
        {
            "SqlServer" => $"LEFT({obj} + REPLICATE({padChar}, {totalWidth}), {totalWidth})",
            "SQLite" => $"SUBSTR({obj} || REPLACE(HEX(ZEROBLOB({totalWidth})), '00', {padChar}), 1, {totalWidth})",
            _ => $"RPAD({obj}, {totalWidth}, {padChar})"
        };

        private static string GetIndexOf(SqlDialect d, string obj, string search) => d.DatabaseType switch
        {
            "SqlServer" => $"(CHARINDEX({search}, {obj}) - 1)",
            "MySql" or "DB2" => $"(LOCATE({search}, {obj}) - 1)",
            "PostgreSql" => $"(POSITION({search} IN {obj}) - 1)",
            _ => $"(INSTR({obj}, {search}) - 1)"  // Oracle, SQLite
        };

        private static string GetIndexOfWithStart(SqlDialect d, string obj, string search, string start) => d.DatabaseType switch
        {
            "SqlServer" => $"(CHARINDEX({search}, {obj}, {start} + 1) - 1)",
            "MySql" or "DB2" => $"(LOCATE({search}, {obj}, {start} + 1) - 1)",
            "PostgreSql" => $"(POSITION({search} IN SUBSTRING({obj} FROM {start} + 1)) + {start} - 1)",
            "SQLite" => $"(INSTR(SUBSTR({obj}, {start} + 1), {search}) + {start} - 1)",
            _ => $"(INSTR({obj}, {search}, {start} + 1) - 1)"  // Oracle
        };
    }

    /// <summary>
    /// Parses DateTime method calls to SQL functions.
    /// </summary>
    internal static class DateTimeFunctionParser
    {
        public static string Parse(ExpressionParser p, MethodCallExpression m)
        {
            var obj = m.Object != null ? p.ParseRaw(m.Object) : string.Empty;
            var d = p.Dialect;
            if (m.Method.Name.StartsWith("Add") && m.Arguments.Count == 1)
            {
                var interval = m.Method.Name.Substring(3).ToUpperInvariant();
                var number = p.ParseRaw(m.Arguments[0]);
                return d.DateAdd(interval, number, obj);
            }
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
            var d = p.Dialect;
            var name = m.Method.Name;
            var hasArg = m.Arguments.Count > 1;
            
            // For Count with predicate, generate SUM(CASE WHEN condition THEN 1 ELSE 0 END)
            if (name == "Count" && hasArg)
            {
                var condition = p.ParseLambdaAsCondition(m.Arguments[1]);
                return $"SUM({d.CaseWhen(condition, "1", "0")})";
            }
            
            var arg = hasArg ? p.ParseLambda(m.Arguments[1]) : null;
            return name switch
            {
                "Count" => d.Count(),
                "CountDistinct" when hasArg => $"COUNT(DISTINCT {arg})",
                "Sum" when hasArg => d.Sum(arg!),
                "Average" or "Avg" when hasArg => d.Avg(arg!),
                "Max" when hasArg => d.Max(arg!),
                "Min" when hasArg => d.Min(arg!),
                "StringAgg" when m.Arguments.Count > 2 => GetStringAgg(d, arg!, p.ParseLambda(m.Arguments[2])),
                _ => throw new NotSupportedException($"Aggregate function {name} is not supported"),
            };
        }

        private static string GetStringAgg(SqlDialect d, string col, string sep) => d.DatabaseType switch
        {
            "MySql" => $"GROUP_CONCAT({col} SEPARATOR {sep})",
            "SQLite" => $"GROUP_CONCAT({col}, {sep})",
            "Oracle" => $"LISTAGG({col}, {sep}) WITHIN GROUP (ORDER BY {col})",
            _ => $"STRING_AGG({col}, {sep})"  // SqlServer, PostgreSQL and others
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
            char c => d.WrapString(c.ToString()),
            _ => v?.ToString() ?? "NULL"
        };

        public static string GetBooleanLiteral(SqlDialect d, bool v) =>
            v ? d.BoolTrue : d.BoolFalse;
    }
}
