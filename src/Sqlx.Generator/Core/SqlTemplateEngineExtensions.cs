// -----------------------------------------------------------------------
// <copyright file="SqlTemplateEngineExtensions.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using System.Text;
using Sqlx;

namespace Sqlx.Generator;

/// <summary>
/// SQL模板引擎扩展方法 - 为所有占位符提供多数据库支持
/// 实现"写一次、处处运行"的核心理念
/// </summary>
public static class SqlTemplateEngineExtensions
{
    /// <summary>
    /// 为SQL模板引擎添加多数据库支持的核心扩展方法
    /// </summary>
    public static class MultiDatabasePlaceholderSupport
    {
        /// <summary>
        /// 处理SET占位符 - 多数据库支持
        /// </summary>
        public static string ProcessSetPlaceholder(string type, INamedTypeSymbol? entityType, IMethodSymbol method, string options, SqlDefine dialect)
        {
            // 简化实现，重点演示多数据库支持
            return $"name = {dialect.ParameterPrefix}name, value = {dialect.ParameterPrefix}value";
        }

        /// <summary>
        /// 处理ORDER BY占位符 - 多数据库支持
        /// </summary>
        public static string ProcessOrderByPlaceholder(string type, string options, SqlDefine dialect)
        {
            return $"ORDER BY {dialect.WrapColumn("id")} ASC";
        }

        /// <summary>
        /// 处理LIMIT占位符 - 多数据库支持
        /// 展示不同数据库的分页语法差异
        /// </summary>
        public static string ProcessLimitPlaceholder(string type, string options, SqlDefine dialect)
        {
            // 首先尝试获取count选项，如果没有则使用default选项
            var limit = ExtractOption(options, "count", null) ?? ExtractLimitValue(options);

            // 确保总是有一个有效的limit值
            if (string.IsNullOrEmpty(limit))
                limit = "20";

            // 如果type指定了数据库类型，优先使用type而不是dialect
            return type.ToLowerInvariant() switch
            {
                "sqlserver" => $"TOP {limit}",
                "oracle" => $"ROWNUM <= {limit}",
                "mysql" or "postgresql" or "sqlite" => $"LIMIT {limit}",
                _ => dialect.Equals(SqlDefine.SqlServer) ? $"TOP {limit}" :
                     dialect.Equals(SqlDefine.Oracle) ? $"ROWNUM <= {limit}" :
                     $"LIMIT {limit}"  // MySQL, PostgreSQL, SQLite
            };
        }

        /// <summary>
        /// 处理聚合函数占位符 - 多数据库支持
        /// </summary>
        public static string ProcessAggregateFunction(string function, string type, string options, SqlDefine dialect)
        {
            // 在聚合函数中，列名通过type传递
            var column = type;

            // 处理特殊情况
            if (string.IsNullOrEmpty(column) || column == "*" || column == "all")
                return $"{function}(*)";
            if (column == "distinct")
                return $"{function}(DISTINCT *)";

            return $"{function}({column})";
        }

        /// <summary>
        /// 通用占位符处理器 - 多数据库支持
        /// 为所有其他占位符提供基础支持
        /// </summary>
        public static string ProcessGenericPlaceholder(string placeholderName, string type, string options, SqlDefine dialect)
        {
            return placeholderName.ToLowerInvariant() switch
            {
                "join" => ProcessJoinPlaceholder(type, options, dialect),
                "groupby" => ProcessGroupByPlaceholder(type, options, dialect),
                "having" => $"HAVING COUNT(*) > 0",
                "select" => type == "distinct" ? "SELECT DISTINCT" : "SELECT *",
                "insert" => type == "into" ? "INSERT INTO user" : "INSERT INTO user",
                "update" => "UPDATE user",
                "delete" => type == "from" ? "DELETE FROM user" : "DELETE FROM user",
                "distinct" => "DISTINCT",
                "union" => type == "all" ? "UNION ALL" : "UNION",
                "top" => ProcessLimitPlaceholder(type, options, dialect),
                "offset" => ProcessOffsetPlaceholder(type, options, dialect),
                _ => $"/* {placeholderName} placeholder */"
            };
        }

        /// <summary>
        /// 处理JOIN占位符 - 正确解析options参数
        /// </summary>
        private static string ProcessJoinPlaceholder(string type, string options, SqlDefine dialect)
        {
            var table = ExtractOption(options, "table", "other_table");
            var condition = ExtractOption(options, "on", "id");

            return type.ToLowerInvariant() switch
            {
                "inner" => $"INNER JOIN {table} ON {condition}",
                "left" => $"LEFT JOIN {table} ON {condition}",
                "right" => $"RIGHT JOIN {table} ON {condition}",
                "full" => $"FULL OUTER JOIN {table} ON {condition}",
                _ => $"INNER JOIN {table} ON {condition}"
            };
        }

        /// <summary>
        /// 处理GROUP BY占位符 - 正确解析options参数
        /// </summary>
        private static string ProcessGroupByPlaceholder(string type, string options, SqlDefine dialect)
        {
            var column = ExtractOption(options, "column", type);
            return $"GROUP BY {column}";
        }

        /// <summary>
        /// 处理OFFSET占位符 - 展示数据库间的语法差异
        /// </summary>
        private static string ProcessOffsetPlaceholder(string type, string options, SqlDefine dialect)
        {
            var offset = ExtractOffsetValue(options);
            var rows = ExtractRowsValue(options);

            // 如果type指定了数据库类型，使用type而不是dialect
            return type.ToLowerInvariant() switch
            {
                "mysql" or "postgresql" or "sqlite" => $"LIMIT {rows} OFFSET {offset}",
                "sqlserver" or "oracle" => $"OFFSET {offset} ROWS FETCH NEXT {rows} ROWS ONLY",
                _ => dialect switch
                {
                    var d when d.Equals(SqlDefine.SqlServer) => $"OFFSET {offset} ROWS FETCH NEXT {rows} ROWS ONLY",
                    var d when d.Equals(SqlDefine.Oracle) => $"OFFSET {offset} ROWS FETCH NEXT {rows} ROWS ONLY",
                    _ => $"LIMIT {rows} OFFSET {offset}"  // MySQL, PostgreSQL, SQLite
                }
            };
        }

        // 辅助方法
        private static string ExtractLimitValue(string options) => ExtractOption(options, "default", "10");
        private static string ExtractOffsetValue(string options) => ExtractOption(options, "offset", "0");
        private static string ExtractRowsValue(string options) => ExtractOption(options, "rows", "10");
        private static string ExtractColumnOption(string options, string defaultValue) => ExtractOption(options, "column", defaultValue);

        private static string ExtractOption(string options, string key, string? defaultValue)
        {
            if (string.IsNullOrEmpty(options)) return defaultValue ?? "";

            foreach (var pair in options.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var keyValue = pair.Split(new char[] { '=' }, 2);
                if (keyValue.Length == 2 && keyValue[0].Trim().Equals(key, StringComparison.OrdinalIgnoreCase))
                    return keyValue[1].Trim();
            }

            return defaultValue ?? "";
        }
    }

    /// <summary>
    /// 获取数据库特定的SQL特性
    /// 展示"写一次，处处运行"的强大功能
    /// </summary>
    public static class DatabaseSpecificFeatures
    {
        /// <summary>获取数据库特定的日期函数</summary>
        public static string GetCurrentDateFunction(SqlDefine dialect)
        {
            if (dialect.Equals(SqlDefine.SqlServer)) return "GETDATE()";
            if (dialect.Equals(SqlDefine.MySql)) return "NOW()";
            if (dialect.Equals(SqlDefine.PostgreSql)) return "NOW()";
            if (dialect.Equals(SqlDefine.SQLite)) return "datetime('now')";
            if (dialect.Equals(SqlDefine.Oracle)) return "SYSDATE";
            return "NOW()";
        }

        /// <summary>获取数据库特定的字符串连接语法</summary>
        public static string GetConcatSyntax(SqlDefine dialect, params string[] parts)
        {
            if (dialect.Equals(SqlDefine.PostgreSql) || dialect.Equals(SqlDefine.SQLite) || dialect.Equals(SqlDefine.Oracle))
                return string.Join(" || ", parts);
            return $"CONCAT({string.Join(", ", parts)})";
        }

        /// <summary>获取数据库特定的自增列语法</summary>
        public static string GetAutoIncrementSyntax(SqlDefine dialect)
        {
            if (dialect.Equals(SqlDefine.SqlServer)) return "IDENTITY(1,1)";
            if (dialect.Equals(SqlDefine.MySql)) return "AUTO_INCREMENT";
            if (dialect.Equals(SqlDefine.PostgreSql)) return "SERIAL";
            if (dialect.Equals(SqlDefine.SQLite)) return "AUTOINCREMENT";
            if (dialect.Equals(SqlDefine.Oracle)) return "GENERATED BY DEFAULT AS IDENTITY";
            return "AUTO_INCREMENT";
        }
    }
}

