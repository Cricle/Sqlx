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
            var a = m.Arguments;
            return (m.Method.Name, a.Count) switch
            {
                // Basic
                ("Abs", 1) => $"ABS({p.ParseRaw(a[0])})",
                ("Sign", 1) => $"SIGN({p.ParseRaw(a[0])})",

                // Rounding
                ("Round", 1) => $"ROUND({p.ParseRaw(a[0])})",
                ("Round", 2) => $"ROUND({p.ParseRaw(a[0])}, {p.ParseRaw(a[1])})",
                ("Floor", 1) => $"FLOOR({p.ParseRaw(a[0])})",
                ("Ceiling", 1) => db == "PostgreSql" ? $"CEIL({p.ParseRaw(a[0])})" : $"CEILING({p.ParseRaw(a[0])})",
                ("Truncate", 1) => GetTruncate(db, p.ParseRaw(a[0])),

                // Power & Root
                ("Sqrt", 1) => $"SQRT({p.ParseRaw(a[0])})",
                ("Pow", 2) => db == "MySql" ? $"POW({p.ParseRaw(a[0])}, {p.ParseRaw(a[1])})" : $"POWER({p.ParseRaw(a[0])}, {p.ParseRaw(a[1])})",
                ("Exp", 1) => $"EXP({p.ParseRaw(a[0])})",

                // Logarithm
                ("Log", 1) => db == "SqlServer" ? $"LOG({p.ParseRaw(a[0])})" : $"LN({p.ParseRaw(a[0])})",
                ("Log", 2) => $"LOG({p.ParseRaw(a[1])}, {p.ParseRaw(a[0])})", // Math.Log(value, base) -> LOG(base, value)
                ("Log10", 1) => $"LOG10({p.ParseRaw(a[0])})",

                // Trigonometric
                ("Sin", 1) => $"SIN({p.ParseRaw(a[0])})",
                ("Cos", 1) => $"COS({p.ParseRaw(a[0])})",
                ("Tan", 1) => $"TAN({p.ParseRaw(a[0])})",
                ("Asin", 1) => $"ASIN({p.ParseRaw(a[0])})",
                ("Acos", 1) => $"ACOS({p.ParseRaw(a[0])})",
                ("Atan", 1) => $"ATAN({p.ParseRaw(a[0])})",
                ("Atan2", 2) => GetAtan2(db, p.ParseRaw(a[0]), p.ParseRaw(a[1])),

                // Min/Max
                ("Min", 2) => $"LEAST({p.ParseRaw(a[0])}, {p.ParseRaw(a[1])})",
                ("Max", 2) => $"GREATEST({p.ParseRaw(a[0])}, {p.ParseRaw(a[1])})",

                _ => "1"
            };
        }

        private static string GetTruncate(string db, string arg) => db switch
        {
            "SqlServer" => $"ROUND({arg}, 0, 1)",
            "PostgreSql" => $"TRUNC({arg})",
            "Oracle" => $"TRUNC({arg})",
            _ => $"TRUNCATE({arg}, 0)"
        };

        private static string GetAtan2(string db, string y, string x) => db switch
        {
            "SqlServer" => $"ATN2({y}, {x})",
            _ => $"ATAN2({y}, {x})"
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
            var db = p.DatabaseType;
            var d = p.Dialect;
            var args = m.Arguments;

            return (m.Method.Name, args.Count) switch
            {
                // Pattern matching
                ("Contains", 1) => $"{obj} LIKE {d.Concat("'%'", WrapArg(p, args[0]), "'%'")}",
                ("StartsWith", 1) => $"{obj} LIKE {d.Concat(WrapArg(p, args[0]), "'%'")}",
                ("EndsWith", 1) => $"{obj} LIKE {d.Concat("'%'", WrapArg(p, args[0]))}",

                // Case conversion
                ("ToUpper", 0) => $"UPPER({obj})",
                ("ToLower", 0) => $"LOWER({obj})",

                // Trimming
                ("Trim", 0) => $"TRIM({obj})",
                ("TrimStart", 0) => GetTrimStart(db, obj),
                ("TrimEnd", 0) => GetTrimEnd(db, obj),

                // Replace
                ("Replace", 2) => $"REPLACE({obj}, {WrapArg(p, args[0])}, {WrapArg(p, args[1])})",

                // Substring
                ("Substring", 1) => GetSubstring(db, obj, p.ParseRaw(args[0]), null),
                ("Substring", 2) => GetSubstring(db, obj, p.ParseRaw(args[0]), p.ParseRaw(args[1])),

                // Length
                ("Length", 0) => db == "SqlServer" ? $"LEN({obj})" : $"LENGTH({obj})",

                // Padding
                ("PadLeft", 1) => GetPadLeft(db, obj, p.ParseRaw(args[0]), "' '"),
                ("PadLeft", 2) => GetPadLeft(db, obj, p.ParseRaw(args[0]), WrapArg(p, args[1])),
                ("PadRight", 1) => GetPadRight(db, obj, p.ParseRaw(args[0]), "' '"),
                ("PadRight", 2) => GetPadRight(db, obj, p.ParseRaw(args[0]), WrapArg(p, args[1])),

                // IndexOf
                ("IndexOf", 1) => GetIndexOf(db, obj, WrapArg(p, args[0])),
                ("IndexOf", 2) => GetIndexOfWithStart(db, obj, WrapArg(p, args[0]), p.ParseRaw(args[1])),

                _ => obj
            };
        }

        /// <summary>Parses string indexer access (str[index]).</summary>
        public static string ParseIndexer(ExpressionParser p, MethodCallExpression m)
        {
            if (m.Object == null || m.Arguments.Count != 1) return "NULL";
            var obj = p.ParseRaw(m.Object);
            var idx = p.ParseRaw(m.Arguments[0]);
            var db = p.DatabaseType;
            // SQL is 1-based, C# is 0-based
            return db switch
            {
                "SQLite" => $"SUBSTR({obj}, {idx} + 1, 1)",
                "SqlServer" => $"SUBSTRING({obj}, {idx} + 1, 1)",
                "MySql" => $"SUBSTRING({obj}, {idx} + 1, 1)",
                "PostgreSql" => $"SUBSTRING({obj} FROM {idx} + 1 FOR 1)",
                "Oracle" => $"SUBSTR({obj}, {idx} + 1, 1)",
                "DB2" => $"SUBSTR({obj}, {idx} + 1, 1)",
                _ => $"SUBSTRING({obj}, {idx} + 1, 1)"
            };
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

        private static string GetTrimStart(string db, string obj) => db switch
        {
            "SqlServer" => $"LTRIM({obj})",
            "MySql" => $"LTRIM({obj})",
            "PostgreSql" => $"LTRIM({obj})",
            "Oracle" => $"LTRIM({obj})",
            "DB2" => $"LTRIM({obj})",
            "SQLite" => $"LTRIM({obj})",
            _ => $"LTRIM({obj})"
        };

        private static string GetTrimEnd(string db, string obj) => db switch
        {
            "SqlServer" => $"RTRIM({obj})",
            "MySql" => $"RTRIM({obj})",
            "PostgreSql" => $"RTRIM({obj})",
            "Oracle" => $"RTRIM({obj})",
            "DB2" => $"RTRIM({obj})",
            "SQLite" => $"RTRIM({obj})",
            _ => $"RTRIM({obj})"
        };

        private static string GetSubstring(string db, string obj, string start, string? length) => db switch
        {
            "SQLite" => length != null ? $"SUBSTR({obj}, {start} + 1, {length})" : $"SUBSTR({obj}, {start} + 1)",
            "Oracle" => length != null ? $"SUBSTR({obj}, {start} + 1, {length})" : $"SUBSTR({obj}, {start} + 1)",
            "DB2" => length != null ? $"SUBSTR({obj}, {start} + 1, {length})" : $"SUBSTR({obj}, {start} + 1)",
            "PostgreSql" => length != null ? $"SUBSTRING({obj} FROM {start} + 1 FOR {length})" : $"SUBSTRING({obj} FROM {start} + 1)",
            _ => length != null ? $"SUBSTRING({obj}, {start} + 1, {length})" : $"SUBSTRING({obj}, {start} + 1)"
        };

        private static string GetPadLeft(string db, string obj, string totalWidth, string padChar) => db switch
        {
            "SqlServer" => $"RIGHT(REPLICATE({padChar}, {totalWidth}) + {obj}, {totalWidth})",
            "MySql" => $"LPAD({obj}, {totalWidth}, {padChar})",
            "PostgreSql" => $"LPAD({obj}, {totalWidth}, {padChar})",
            "Oracle" => $"LPAD({obj}, {totalWidth}, {padChar})",
            "DB2" => $"LPAD({obj}, {totalWidth}, {padChar})",
            "SQLite" => $"SUBSTR(REPLACE(HEX(ZEROBLOB({totalWidth})), '00', {padChar}) || {obj}, -({totalWidth}))",
            _ => $"LPAD({obj}, {totalWidth}, {padChar})"
        };

        private static string GetPadRight(string db, string obj, string totalWidth, string padChar) => db switch
        {
            "SqlServer" => $"LEFT({obj} + REPLICATE({padChar}, {totalWidth}), {totalWidth})",
            "MySql" => $"RPAD({obj}, {totalWidth}, {padChar})",
            "PostgreSql" => $"RPAD({obj}, {totalWidth}, {padChar})",
            "Oracle" => $"RPAD({obj}, {totalWidth}, {padChar})",
            "DB2" => $"RPAD({obj}, {totalWidth}, {padChar})",
            "SQLite" => $"SUBSTR({obj} || REPLACE(HEX(ZEROBLOB({totalWidth})), '00', {padChar}), 1, {totalWidth})",
            _ => $"RPAD({obj}, {totalWidth}, {padChar})"
        };

        private static string GetIndexOf(string db, string obj, string search) => db switch
        {
            "SqlServer" => $"(CHARINDEX({search}, {obj}) - 1)",
            "MySql" => $"(LOCATE({search}, {obj}) - 1)",
            "PostgreSql" => $"(POSITION({search} IN {obj}) - 1)",
            "Oracle" => $"(INSTR({obj}, {search}) - 1)",
            "DB2" => $"(LOCATE({search}, {obj}) - 1)",
            "SQLite" => $"(INSTR({obj}, {search}) - 1)",
            _ => $"(CHARINDEX({search}, {obj}) - 1)"
        };

        private static string GetIndexOfWithStart(string db, string obj, string search, string start) => db switch
        {
            "SqlServer" => $"(CHARINDEX({search}, {obj}, {start} + 1) - 1)",
            "MySql" => $"(LOCATE({search}, {obj}, {start} + 1) - 1)",
            "PostgreSql" => $"(POSITION({search} IN SUBSTRING({obj} FROM {start} + 1)) + {start} - 1)",
            "Oracle" => $"(INSTR({obj}, {search}, {start} + 1) - 1)",
            "DB2" => $"(LOCATE({search}, {obj}, {start} + 1) - 1)",
            "SQLite" => $"(INSTR(SUBSTR({obj}, {start} + 1), {search}) + {start} - 1)",
            _ => $"(CHARINDEX({search}, {obj}, {start} + 1) - 1)"
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
            char c => d.WrapString(c.ToString()),
            _ => v?.ToString() ?? "NULL"
        };

        public static string GetBooleanLiteral(SqlDialect d, bool v) =>
            d.DatabaseType == "PostgreSql" ? (v ? "true" : "false") : (v ? "1" : "0");
    }
}
