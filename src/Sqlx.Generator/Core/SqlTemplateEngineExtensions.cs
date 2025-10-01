// -----------------------------------------------------------------------
// <copyright file="SqlTemplateEngineExtensions.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
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

        // 性能优化：预定义的LIMIT模式缓存
        private static readonly Dictionary<string, (string SqlServer, string Oracle, string Others)> LimitPatterns = new(StringComparer.OrdinalIgnoreCase)
        {
            ["tiny"] = ("OFFSET 0 ROWS FETCH NEXT 5 ROWS ONLY", "OFFSET 0 ROWS FETCH NEXT 5 ROWS ONLY", "LIMIT 5"),
            ["small"] = ("OFFSET 0 ROWS FETCH NEXT 10 ROWS ONLY", "OFFSET 0 ROWS FETCH NEXT 10 ROWS ONLY", "LIMIT 10"),
            ["medium"] = ("OFFSET 0 ROWS FETCH NEXT 50 ROWS ONLY", "OFFSET 0 ROWS FETCH NEXT 50 ROWS ONLY", "LIMIT 50"),
            ["large"] = ("OFFSET 0 ROWS FETCH NEXT 100 ROWS ONLY", "OFFSET 0 ROWS FETCH NEXT 100 ROWS ONLY", "LIMIT 100"),
            ["page"] = ("OFFSET 0 ROWS FETCH NEXT 20 ROWS ONLY", "OFFSET 0 ROWS FETCH NEXT 20 ROWS ONLY", "LIMIT 20"),
            ["default"] = ("OFFSET 0 ROWS FETCH NEXT 20 ROWS ONLY", "OFFSET 0 ROWS FETCH NEXT 20 ROWS ONLY", "LIMIT 20")
        };

        /// <summary>
        /// 处理LIMIT占位符 - 多数据库支持 (增强版本)
        /// 支持预定义模式、分页偏移量、智能默认值
        /// </summary>
        public static string ProcessLimitPlaceholder(string type, string options, SqlDefine dialect)
        {
            // 检查预定义模式 - 快速路径
            if (LimitPatterns.TryGetValue(type, out var patterns))
            {
                return dialect.Equals(SqlDefine.SqlServer) ? patterns.SqlServer :
                       dialect.Equals(SqlDefine.Oracle) ? patterns.Oracle :
                       patterns.Others;
            }

            // 智能选项解析
            var count = ExtractOption(options, "count", null) ??
                       ExtractOption(options, "limit", null) ??
                       ExtractOption(options, "size", null) ??
                       ExtractLimitValue(options) ??
                       "20";

            var offset = ExtractOption(options, "offset", null) ??
                        ExtractOption(options, "skip", null);

            // 如果type指定了数据库类型，优先使用type而不是dialect
            var targetDialect = type.ToLowerInvariant() switch
            {
                "sqlserver" => SqlDefine.SqlServer,
                "oracle" => SqlDefine.Oracle,
                "mysql" => SqlDefine.MySql,
                "postgresql" => SqlDefine.PostgreSql,
                "sqlite" => SqlDefine.SQLite,
                _ => dialect
            };

            // 根据目标数据库生成分页语句
            if (targetDialect.Equals(SqlDefine.SqlServer))
            {
                return offset != null
                    ? $"OFFSET {offset} ROWS FETCH NEXT {count} ROWS ONLY"
                    : $"TOP {count}";
            }
            else if (targetDialect.Equals(SqlDefine.Oracle))
            {
                return offset != null
                    ? $"OFFSET {offset} ROWS FETCH NEXT {count} ROWS ONLY"
                    : $"ROWNUM <= {count}";
            }
            else
            {
                // MySQL, PostgreSQL, SQLite
                return offset != null
                    ? $"LIMIT {count} OFFSET {offset}"
                    : $"LIMIT {count}";
            }
        }

        // 性能优化：预定义的聚合函数模式
        private static readonly Dictionary<string, string> AggregatePatterns = new(StringComparer.OrdinalIgnoreCase)
        {
            ["count_all"] = "COUNT(*)",
            ["count_rows"] = "COUNT(*)",
            ["sum_all"] = "SUM(*)",
            ["avg_all"] = "AVG(*)",
            ["max_all"] = "MAX(*)",
            ["min_all"] = "MIN(*)"
        };

        /// <summary>
        /// 处理聚合函数占位符 - 多数据库支持 (增强版本)
        /// 支持多种聚合函数、DISTINCT操作、列名智能解析
        /// </summary>
        public static string ProcessAggregateFunction(string function, string type, string options, SqlDefine dialect)
        {
            // 检查预定义模式
            var patternKey = $"{function.ToLower()}_{type}";
            if (AggregatePatterns.TryGetValue(patternKey, out var pattern))
                return pattern;

            // 从选项中提取设置
            var distinct = ExtractOption(options, "distinct", null) == "true";
            var columnOption = ExtractOption(options, "column", null);
            var column = !string.IsNullOrEmpty(columnOption) ? columnOption : type;

            // 处理特殊情况
            if (string.IsNullOrEmpty(column) || column == "*" || column == "all")
            {
                return distinct ? $"{function}(DISTINCT *)" : $"{function}(*)";
            }

            if (column == "distinct")
            {
                return $"{function}(DISTINCT *)";
            }

            // 智能列名处理
            var columnName = column.Contains('_')
                ? column // 已经是snake_case
                : SharedCodeGenerationUtilities.ConvertToSnakeCase(column);

            // 数据库特定优化
            var wrappedColumn = dialect.WrapColumn(columnName);

            return distinct
                ? $"{function}(DISTINCT {wrappedColumn})"
                : $"{function}({wrappedColumn})";
        }

        // 性能优化：通用占位符快速映射表
        private static readonly Dictionary<string, string> GenericPlaceholderMap = new(StringComparer.OrdinalIgnoreCase)
        {
            ["all"] = "*",
            ["any"] = "1=1",
            ["none"] = "1=0",
            ["true"] = "1",
            ["false"] = "0",
            ["null"] = "NULL"
        };

        /// <summary>
        /// 通用占位符处理器 - 多数据库支持 (增强版本)
        /// 提供更丰富的占位符支持和智能默认值
        /// </summary>
        public static string ProcessGenericPlaceholder(string placeholderName, string type, string options, SqlDefine dialect)
        {
            // 快速路径：检查简单映射
            if (GenericPlaceholderMap.TryGetValue(placeholderName, out var simpleResult))
                return simpleResult;

            return placeholderName.ToLowerInvariant() switch
            {
                // JOIN系列
                "join" => ProcessJoinPlaceholder(type, options, dialect),
                "inner_join" => ProcessJoinPlaceholder("inner", options, dialect),
                "left_join" => ProcessJoinPlaceholder("left", options, dialect),
                "right_join" => ProcessJoinPlaceholder("right", options, dialect),

                // 分组和聚合
                "groupby" => ProcessGroupByPlaceholder(type, options, dialect),
                "having" => ProcessHavingPlaceholder(type, options, dialect),

                // 基本SQL语句
                "select" => type == "distinct" ? "SELECT DISTINCT" : "SELECT",
                "insert" => type == "into" ? "INSERT INTO" : "INSERT",
                "update" => "UPDATE",
                "delete" => type == "from" ? "DELETE FROM" : "DELETE",

                // 集合操作
                "union" => type == "all" ? "UNION ALL" : "UNION",
                "intersect" => "INTERSECT",
                "except" => "EXCEPT",

                // DISTINCT操作
                "distinct" => ProcessDistinctField(type, options, dialect),

                // 分页相关
                "top" => ProcessLimitPlaceholder(type, options, dialect),
                "offset" => ProcessOffsetPlaceholder(type, options, dialect),

                // 数据库函数
                "uuid" => ProcessUuidFunction(dialect),
                "random" => ProcessRandomFunction(dialect),
                "now" => ProcessNowFunction(dialect),
                "today" => ProcessTodayFunction(dialect),

                // 默认处理
                _ => $"/* 未知占位符: {placeholderName} */"
            };
        }

        /// <summary>处理HAVING占位符 - 增强版本</summary>
        private static string ProcessHavingPlaceholder(string type, string options, SqlDefine dialect) =>
            type switch
            {
                "count" => $"HAVING COUNT(*) > {ExtractOption(options, "min", "0")}",
                "sum" => $"HAVING SUM({ExtractOption(options, "column", "amount")}) > {ExtractOption(options, "min", "0")}",
                "avg" => $"HAVING AVG({ExtractOption(options, "column", "value")}) > {ExtractOption(options, "min", "0")}",
                _ => "HAVING COUNT(*) > 0"
            };

        /// <summary>处理UUID函数 - 多数据库支持</summary>
        private static string ProcessUuidFunction(SqlDefine dialect) =>
            dialect.Equals(SqlDefine.SqlServer) ? "NEWID()" :
            dialect.Equals(SqlDefine.MySql) ? "UUID()" :
            dialect.Equals(SqlDefine.PostgreSql) ? "gen_random_uuid()" :
            "hex(randomblob(16))"; // SQLite

        /// <summary>处理随机函数 - 多数据库支持</summary>
        private static string ProcessRandomFunction(SqlDefine dialect) =>
            dialect.Equals(SqlDefine.SqlServer) ? "RAND()" :
            dialect.Equals(SqlDefine.MySql) ? "RAND()" :
            dialect.Equals(SqlDefine.PostgreSql) ? "RANDOM()" :
            "RANDOM()"; // SQLite

        /// <summary>处理当前时间函数 - 多数据库支持</summary>
        private static string ProcessNowFunction(SqlDefine dialect) =>
            dialect.Equals(SqlDefine.SqlServer) ? "GETDATE()" :
            dialect.Equals(SqlDefine.MySql) ? "NOW()" :
            dialect.Equals(SqlDefine.PostgreSql) ? "NOW()" :
            dialect.Equals(SqlDefine.Oracle) ? "SYSDATE" :
            "datetime('now')"; // SQLite

        /// <summary>处理当前日期函数 - 多数据库支持</summary>
        private static string ProcessTodayFunction(SqlDefine dialect) =>
            dialect.Equals(SqlDefine.SqlServer) ? "CAST(GETDATE() AS DATE)" :
            dialect.Equals(SqlDefine.MySql) ? "CURDATE()" :
            dialect.Equals(SqlDefine.PostgreSql) ? "CURRENT_DATE" :
            dialect.Equals(SqlDefine.Oracle) ? "TRUNC(SYSDATE)" :
            "date('now')"; // SQLite

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

        /// <summary>处理DISTINCT字段占位符</summary>
        public static string ProcessDistinctField(string type, string options, SqlDefine dialect)
        {
            var fieldOption = ExtractOption(options, "field", null);
            var field = !string.IsNullOrEmpty(fieldOption) ? fieldOption : type;

            // 确保有有效的字段名
            if (string.IsNullOrEmpty(field))
            {
                return "DISTINCT *";
            }

            // 防御性编程：使用简单的snake_case转换
            string columnName;
            if (field.Contains('_'))
            {
                columnName = field; // 已经是snake_case
            }
            else
            {
                // 简化的snake_case转换，避免依赖外部方法
                columnName = string.Concat(field.Select((c, i) =>
                    i > 0 && char.IsUpper(c) ? "_" + char.ToLowerInvariant(c) : char.ToLowerInvariant(c).ToString()));
            }

            // 再次检查转换结果
            if (string.IsNullOrEmpty(columnName))
            {
                return "DISTINCT *";
            }

            var wrappedColumn = dialect.WrapColumn(columnName);
            return $"DISTINCT {wrappedColumn}";
        }

    }

    /// <summary>
    /// 获取数据库特定的SQL特性
    /// 展示"写一次，处处运行"的强大功能
    /// </summary>
    public static class DatabaseSpecificFeatures
    {
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

