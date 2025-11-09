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

        /// <summary>
        /// 处理LIMIT占位符 - 多数据库支持 (简化版本)
        /// 支持预定义模式、分页偏移量、智能默认值、参数化形式
        /// </summary>
        public static string ProcessLimitPlaceholder(string type, string options, SqlDefine dialect, Microsoft.CodeAnalysis.IMethodSymbol method = null)
        {
            // 自动检测 limit 参数（如果没有明确指定 --param）
            var paramMatch = System.Text.RegularExpressions.Regex.Match(options ?? "", @"--param\s+(\w+)");
            string paramName = null;

            if (paramMatch.Success)
            {
                paramName = paramMatch.Groups[1].Value;
            }
            else if (method != null)
            {
                // 自动检测名为 "limit" 的参数
                var limitParam = method.Parameters.FirstOrDefault(p =>
                    p.Name.Equals("limit", System.StringComparison.OrdinalIgnoreCase));
                if (limitParam != null)
                {
                    paramName = limitParam.Name;
                }
            }

            // 如果找到了参数名，生成参数化的 LIMIT
            if (!string.IsNullOrEmpty(paramName))
            {
                // 返回参数化的LIMIT（由方法参数提供值）
                // 使用DatabaseType字符串区分数据库，因为SQLite和SQL Server有相同的结构但不同的行为
                var dbType = dialect.DatabaseType;
                if (dbType == "SqlServer")
                {
                    // SQL Server: 使用 OFFSET...FETCH 语法（需要 ORDER BY）
                    // 生成运行时占位符，让代码生成器处理
                    return $"{{RUNTIME_LIMIT_{paramName}}}";
                }
                else if (dbType == "Oracle")
                {
                    return $"ROWNUM <= {dialect.ParameterPrefix}{paramName}";
                }
                else
                {
                    // MySQL, PostgreSQL, SQLite
                    return $"LIMIT {dialect.ParameterPrefix}{paramName}";
                }
            }

            // 检查预定义模式
            var (sqlServer, oracle, others) = type.ToLowerInvariant() switch
            {
                "tiny" => ("OFFSET 0 ROWS FETCH NEXT 5 ROWS ONLY", "OFFSET 0 ROWS FETCH NEXT 5 ROWS ONLY", "LIMIT 5"),
                "small" => ("OFFSET 0 ROWS FETCH NEXT 10 ROWS ONLY", "OFFSET 0 ROWS FETCH NEXT 10 ROWS ONLY", "LIMIT 10"),
                "medium" => ("OFFSET 0 ROWS FETCH NEXT 50 ROWS ONLY", "OFFSET 0 ROWS FETCH NEXT 50 ROWS ONLY", "LIMIT 50"),
                "large" => ("OFFSET 0 ROWS FETCH NEXT 100 ROWS ONLY", "OFFSET 0 ROWS FETCH NEXT 100 ROWS ONLY", "LIMIT 100"),
                "page" => ("OFFSET 0 ROWS FETCH NEXT 20 ROWS ONLY", "OFFSET 0 ROWS FETCH NEXT 20 ROWS ONLY", "LIMIT 20"),
                "default" => ("OFFSET 0 ROWS FETCH NEXT 20 ROWS ONLY", "OFFSET 0 ROWS FETCH NEXT 20 ROWS ONLY", "LIMIT 20"),
                _ => (null, null, null)
            };

            if (sqlServer != null)
            {
                return dialect.Equals(SqlDefine.SqlServer) ? sqlServer :
                       dialect.Equals(SqlDefine.Oracle) ? oracle :
                       others;
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
            // 注意：检查 IsNullOrEmpty 而不是 != null，因为 ExtractOption 可能返回空字符串
            if (targetDialect.Equals(SqlDefine.SqlServer))
            {
                return !string.IsNullOrEmpty(offset)
                    ? $"OFFSET {offset} ROWS FETCH NEXT {count} ROWS ONLY"
                    : $"TOP {count}";
            }
            else if (targetDialect.Equals(SqlDefine.Oracle))
            {
                return !string.IsNullOrEmpty(offset)
                    ? $"OFFSET {offset} ROWS FETCH NEXT {count} ROWS ONLY"
                    : $"ROWNUM <= {count}";
            }
            else
            {
                // MySQL, PostgreSQL, SQLite
                return !string.IsNullOrEmpty(offset)
                    ? $"LIMIT {count} OFFSET {offset}"
                    : $"LIMIT {count}";
            }
        }

        /// <summary>
        /// 处理OFFSET占位符 - 多数据库支持
        /// 支持参数化形式、智能默认值
        /// </summary>
        public static string ProcessOffsetPlaceholder(string type, string options, SqlDefine dialect)
        {
            // 检查是否是参数化形式: --param paramName
            var paramMatch = System.Text.RegularExpressions.Regex.Match(options ?? "", @"--param\s+(\w+)");
            if (paramMatch.Success)
            {
                var paramName = paramMatch.Groups[1].Value;
                // 返回参数化的OFFSET（由方法参数提供值）
                var dbType = dialect.DatabaseType;
                if (dbType == "SqlServer" || dbType == "Oracle")
                {
                    return $"OFFSET {dialect.ParameterPrefix}{paramName} ROWS";
                }
                else
                {
                    // MySQL, PostgreSQL, SQLite
                    return $"OFFSET {dialect.ParameterPrefix}{paramName}";
                }
            }

            // 智能选项解析
            var offset = ExtractOption(options, "offset", null) ??
                        ExtractOption(options, "skip", null) ??
                        "0";

            // 根据数据库生成OFFSET语句
            var dbType2 = dialect.DatabaseType;
            if (dbType2 == "SqlServer" || dbType2 == "Oracle")
            {
                return $"OFFSET {offset} ROWS";
            }
            else
            {
                // MySQL, PostgreSQL, SQLite
                return $"OFFSET {offset}";
            }
        }

        /// <summary>
        /// 处理聚合函数占位符 - 多数据库支持 (简化版本)
        /// 支持多种聚合函数、DISTINCT操作、列名智能解析
        /// </summary>
        public static string ProcessAggregateFunction(string function, string type, string options, SqlDefine dialect)
        {
            // 检查预定义模式
            var pattern = $"{function.ToLower()}_{type}".ToLowerInvariant() switch
            {
                "count_all" => "COUNT(*)",
                "count_rows" => "COUNT(*)",
                "sum_all" => "SUM(*)",
                "avg_all" => "AVG(*)",
                "max_all" => "MAX(*)",
                "min_all" => "MIN(*)",
                _ => null
            };

            if (pattern != null) return pattern;

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

        /// <summary>
        /// 通用占位符处理器 - 多数据库支持 (简化版本)
        /// 提供更丰富的占位符支持和智能默认值
        /// </summary>
        public static string ProcessGenericPlaceholder(string placeholderName, string type, string options, SqlDefine dialect)
        {
            // 简单映射
            var simpleResult = placeholderName.ToLowerInvariant() switch
            {
                "all" => "*",
                "any" => "1=1",
                "none" => "1=0",
                "true" => "1",
                "false" => "0",
                "null" => "NULL",
                _ => null
            };

            if (simpleResult != null) return simpleResult;

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

