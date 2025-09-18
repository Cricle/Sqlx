// -----------------------------------------------------------------------
// <copyright file="SqlTemplatePlaceholder.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Sqlx;

namespace Sqlx.Generator.Core
{
    /// <summary>
    /// SQL 模板占位符处理器，支持动态替换占位符如 {{columns}}, {{table}}, {{where}} 等
    /// </summary>
    public static class SqlTemplatePlaceholder
    {
        private static readonly Regex PlaceholderRegex = new Regex(@"\{\{(\w+)(?::([^}]+))?\}\}", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// 支持的占位符类型
        /// </summary>
        public static class Placeholders
        {
            /// <summary>
            /// 列名占位符，用于替换表的列名列表。
            /// </summary>
            public const string Columns = "columns";
            /// <summary>
            /// 表名占位符，用于替换数据库表名。
            /// </summary>
            public const string Table = "table";
            /// <summary>
            /// WHERE 子句占位符，用于替换查询条件。
            /// </summary>
            public const string Where = "where";
            /// <summary>
            /// ORDER BY 子句占位符，用于替换排序条件。
            /// </summary>
            public const string OrderBy = "orderby";
            /// <summary>
            /// INSERT 语句占位符，用于替换插入列名。
            /// </summary>
            public const string Insert = "insert";
            /// <summary>
            /// UPDATE 语句占位符，用于替换更新列名。
            /// </summary>
            public const string Update = "update";
            /// <summary>
            /// VALUES 子句占位符，用于替换插入值。
            /// </summary>
            public const string Values = "values";
            /// <summary>
            /// JOIN 子句占位符，用于替换表连接。
            /// </summary>
            public const string Joins = "joins";
            /// <summary>
            /// SELECT 子句占位符，用于替换查询列名。
            /// </summary>
            public const string Select = "select";
            /// <summary>
            /// COUNT 函数占位符，用于替换计数查询。
            /// </summary>
            public const string Count = "count";
        }

        /// <summary>
        /// 处理 SQL 模板中的占位符替换
        /// </summary>
        /// <param name="sqlTemplate">原始 SQL 模板</param>
        /// <param name="context">替换上下文</param>
        /// <returns>处理后的 SQL 字符串</returns>
        public static string ProcessTemplate(string sqlTemplate, SqlPlaceholderContext context)
        {
            if (string.IsNullOrEmpty(sqlTemplate))
                return sqlTemplate;

            return PlaceholderRegex.Replace(sqlTemplate, match =>
            {
                var placeholderName = match.Groups[1].Value.ToLowerInvariant();
                var placeholderArgs = match.Groups[2].Success ? match.Groups[2].Value : null;

                return placeholderName switch
                {
                    Placeholders.Columns => ProcessColumnsPlaceholder(context, placeholderArgs),
                    Placeholders.Table => ProcessTablePlaceholder(context, placeholderArgs),
                    Placeholders.Where => ProcessWherePlaceholder(context, placeholderArgs),
                    Placeholders.OrderBy => ProcessOrderByPlaceholder(context, placeholderArgs),
                    Placeholders.Insert => ProcessInsertPlaceholder(context, placeholderArgs),
                    Placeholders.Update => ProcessUpdatePlaceholder(context, placeholderArgs),
                    Placeholders.Values => ProcessValuesPlaceholder(context, placeholderArgs),
                    Placeholders.Joins => ProcessJoinsPlaceholder(context, placeholderArgs),
                    Placeholders.Select => ProcessSelectPlaceholder(context, placeholderArgs),
                    Placeholders.Count => ProcessCountPlaceholder(context, placeholderArgs),
                    _ => match.Value // 保留未知占位符
                };
            });
        }

        /// <summary>
        /// 检查 SQL 模板是否包含占位符
        /// </summary>
        /// <param name="sqlTemplate">SQL 模板</param>
        /// <returns>是否包含占位符</returns>
        public static bool ContainsPlaceholders(string sqlTemplate)
        {
            return !string.IsNullOrEmpty(sqlTemplate) && PlaceholderRegex.IsMatch(sqlTemplate);
        }

        /// <summary>
        /// 获取 SQL 模板中所有的占位符
        /// </summary>
        /// <param name="sqlTemplate">SQL 模板</param>
        /// <returns>占位符列表</returns>
        public static List<string> GetPlaceholders(string sqlTemplate)
        {
            if (string.IsNullOrEmpty(sqlTemplate))
                return new List<string>();

            return PlaceholderRegex.Matches(sqlTemplate)
                .Cast<Match>()
                .Select(m => m.Groups[1].Value.ToLowerInvariant())
                .Distinct()
                .ToList();
        }

        /// <summary>
        /// 处理 {{columns}} 占位符
        /// </summary>
        private static string ProcessColumnsPlaceholder(SqlPlaceholderContext context, string? args)
        {
            if (context.EntityType == null)
                return "*";

            var columns = GetEntityColumns(context.EntityType, context.SqlDefine);

            // 支持参数: {{columns:exclude=Id,CreatedAt}}
            if (!string.IsNullOrEmpty(args))
            {
                var argsDict = ParseArgs(args);
                if (argsDict.TryGetValue("exclude", out var excludeValue))
                {
                    var excludeColumns = new HashSet<string>(excludeValue.Split(',').Select(s => s.Trim()));
                    columns = columns.Where(c => !excludeColumns.Contains(GetColumnNameWithoutWrapper(c))).ToList();
                }
                if (argsDict.TryGetValue("include", out var includeValue))
                {
                    var includeColumns = new HashSet<string>(includeValue.Split(',').Select(s => s.Trim()));
                    columns = columns.Where(c => includeColumns.Contains(GetColumnNameWithoutWrapper(c))).ToList();
                }
            }

            return columns.Any() ? string.Join(", ", columns) : "*";
        }

        /// <summary>
        /// 处理 {{table}} 占位符
        /// </summary>
        private static string ProcessTablePlaceholder(SqlPlaceholderContext context, string? args)
        {
            var tableName = context.TableName ?? context.EntityType?.Name ?? "UnknownTable";

            // 支持参数: {{table:alias=u}}
            if (!string.IsNullOrEmpty(args))
            {
                var argsDict = ParseArgs(args);
                if (argsDict.TryGetValue("alias", out var aliasValue))
                {
                    return $"{context.SqlDefine.WrapColumn(tableName)} {aliasValue}";
                }
            }

            return context.SqlDefine.WrapColumn(tableName);
        }

        /// <summary>
        /// 处理 {{where}} 占位符
        /// </summary>
        private static string ProcessWherePlaceholder(SqlPlaceholderContext context, string? args)
        {
            // 支持参数: {{where:default=1=1}}
            if (!string.IsNullOrEmpty(args))
            {
                var argsDict = ParseArgs(args);
                if (argsDict.TryGetValue("default", out var defaultValue))
                {
                    return defaultValue;
                }
            }

            return "1=1"; // 默认的安全 WHERE 条件
        }

        /// <summary>
        /// 处理 {{orderby}} 占位符
        /// </summary>
        private static string ProcessOrderByPlaceholder(SqlPlaceholderContext context, string? args)
        {
            // 支持参数: {{orderby:default=Id ASC}}
            if (!string.IsNullOrEmpty(args))
            {
                var argsDict = ParseArgs(args);
                if (argsDict.TryGetValue("default", out var defaultValue))
                {
                    return $"ORDER BY {defaultValue}";
                }
            }

            // 尝试使用主键字段作为默认排序
            if (context.EntityType != null)
            {
                var primaryKey = GetPrimaryKeyColumn(context.EntityType, context.SqlDefine);
                if (!string.IsNullOrEmpty(primaryKey))
                {
                    return $"ORDER BY {primaryKey}";
                }
            }

            return "";
        }

        /// <summary>
        /// 处理 {{insert}} 占位符
        /// </summary>
        private static string ProcessInsertPlaceholder(SqlPlaceholderContext context, string? args)
        {
            if (context.EntityType == null)
                return "";

            var columns = GetEntityColumns(context.EntityType, context.SqlDefine, excludeIdentity: true);
            var tableName = context.TableName ?? context.EntityType.Name;

            return $"INSERT INTO {context.SqlDefine.WrapColumn(tableName)} ({string.Join(", ", columns)})";
        }

        /// <summary>
        /// 处理 {{update}} 占位符
        /// </summary>
        private static string ProcessUpdatePlaceholder(SqlPlaceholderContext context, string? args)
        {
            if (context.EntityType == null)
                return "";

            var tableName = context.TableName ?? context.EntityType.Name;
            return $"UPDATE {context.SqlDefine.WrapColumn(tableName)} SET";
        }

        /// <summary>
        /// 处理 {{values}} 占位符
        /// </summary>
        private static string ProcessValuesPlaceholder(SqlPlaceholderContext context, string? args)
        {
            if (context.EntityType == null)
                return "VALUES (?)";

            var columns = GetEntityColumns(context.EntityType, context.SqlDefine, excludeIdentity: true);
            var parameters = columns.Select((_, index) => $"@p{index}").ToList();

            return $"VALUES ({string.Join(", ", parameters)})";
        }

        /// <summary>
        /// 处理 {{joins}} 占位符
        /// </summary>
        private static string ProcessJoinsPlaceholder(SqlPlaceholderContext context, string? args)
        {
            // 支持参数: {{joins:type=INNER,table=Department,on=u.DepartmentId=d.Id,alias=d}}
            if (!string.IsNullOrEmpty(args))
            {
                var argsDict = ParseArgs(args);
                var joinType = argsDict.ContainsKey("type") ? argsDict["type"] : "INNER";
                var joinTable = argsDict.ContainsKey("table") ? argsDict["table"] : "";
                var joinOn = argsDict.ContainsKey("on") ? argsDict["on"] : "";
                var joinAlias = argsDict.ContainsKey("alias") ? argsDict["alias"] : "";

                if (!string.IsNullOrEmpty(joinTable) && !string.IsNullOrEmpty(joinOn))
                {
                    var tableWithAlias = string.IsNullOrEmpty(joinAlias)
                        ? context.SqlDefine.WrapColumn(joinTable)
                        : $"{context.SqlDefine.WrapColumn(joinTable)} {joinAlias}";

                    return $"{joinType} JOIN {tableWithAlias} ON {joinOn}";
                }
            }

            return "";
        }

        /// <summary>
        /// 处理 {{select}} 占位符
        /// </summary>
        private static string ProcessSelectPlaceholder(SqlPlaceholderContext context, string? args)
        {
            var columns = ProcessColumnsPlaceholder(context, args);
            return $"SELECT {columns}";
        }

        /// <summary>
        /// 处理 {{count}} 占位符
        /// </summary>
        private static string ProcessCountPlaceholder(SqlPlaceholderContext context, string? args)
        {
            // 支持参数: {{count:column=Id}}
            if (!string.IsNullOrEmpty(args))
            {
                var argsDict = ParseArgs(args);
                if (argsDict.TryGetValue("column", out var columnValue))
                {
                    return $"COUNT({context.SqlDefine.WrapColumn(columnValue)})";
                }
            }

            return "COUNT(*)";
        }

        /// <summary>
        /// 获取实体类型的所有列
        /// </summary>
        private static List<string> GetEntityColumns(INamedTypeSymbol entityType, SqlDefine sqlDefine, bool excludeIdentity = false)
        {
            var properties = entityType.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(p => p.CanBeReferencedByName && p.DeclaredAccessibility == Accessibility.Public)
                .ToList();

            if (excludeIdentity)
            {
                // 排除可能的身份列（Id, 自增列等）
                properties = properties.Where(p =>
                    !p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase) ||
                    p.GetAttributes().Any(attr => attr.AttributeClass?.Name == "KeyAttribute")).ToList();
            }

            return properties.Select(p => sqlDefine.WrapColumn(p.Name)).ToList();
        }

        /// <summary>
        /// 获取主键列
        /// </summary>
        private static string? GetPrimaryKeyColumn(INamedTypeSymbol entityType, SqlDefine sqlDefine)
        {
            var keyProperty = entityType.GetMembers()
                .OfType<IPropertySymbol>()
                .FirstOrDefault(p => p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase) ||
                                   p.GetAttributes().Any(attr => attr.AttributeClass?.Name == "KeyAttribute"));

            return keyProperty != null ? sqlDefine.WrapColumn(keyProperty.Name) : null;
        }

        /// <summary>
        /// 解析占位符参数
        /// </summary>
        private static Dictionary<string, string> ParseArgs(string? args)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrEmpty(args))
                return result;

            var pairs = args!.Split(',');
            foreach (var pair in pairs)
            {
                var keyValue = pair.Split(new char[] { '=' }, 2);
                if (keyValue.Length == 2)
                {
                    result[keyValue[0].Trim()] = keyValue[1].Trim();
                }
            }

            return result;
        }

        /// <summary>
        /// 获取去除包装器的列名
        /// </summary>
        private static string GetColumnNameWithoutWrapper(string wrappedColumn)
        {
            if (string.IsNullOrEmpty(wrappedColumn))
                return wrappedColumn;

            // 移除各种数据库的列名包装符号
            return wrappedColumn.Trim('[', ']', '`', '"');
        }
    }

    /// <summary>
    /// SQL 占位符替换上下文
    /// </summary>
    public class SqlPlaceholderContext
    {
        /// <summary>
        /// 获取或设置实体类型符号。
        /// </summary>
        public INamedTypeSymbol? EntityType { get; set; }
        /// <summary>
        /// 获取或设置数据库表名。
        /// </summary>
        public string? TableName { get; set; }
        /// <summary>
        /// 获取或设置 SQL 方言定义。
        /// </summary>
        public SqlDefine SqlDefine { get; set; }
        /// <summary>
        /// 获取或设置当前处理的方法符号。
        /// </summary>
        public IMethodSymbol? Method { get; set; }
        /// <summary>
        /// 获取或设置额外的上下文数据。
        /// </summary>
        public Dictionary<string, object?> AdditionalData { get; set; } = new();

        /// <summary>
        /// 初始化 SqlPlaceholderContext 类的新实例。
        /// </summary>
        /// <param name="sqlDefine">SQL 方言定义。</param>
        public SqlPlaceholderContext(SqlDefine sqlDefine)
        {
            SqlDefine = sqlDefine;
        }
    }
}
