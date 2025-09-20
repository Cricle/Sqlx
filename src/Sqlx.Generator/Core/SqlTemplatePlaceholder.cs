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
    /// SQL template placeholder processor, supports dynamic replacement of placeholders like {{columns}}, {{table}}, {{where}} etc
    /// </summary>
    public static class SqlTemplatePlaceholder
    {
        private static readonly Regex PlaceholderRegex = new Regex(@"\{\{(\w+)(?::([^}]+))?\}\}", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Supported placeholder types
        /// </summary>
        public static class Placeholders
        {
            /// <summary>
            /// Column name placeholder, used to replace table column name list.
            /// </summary>
            public const string Columns = "columns";
            /// <summary>
            /// Table name placeholder, used to replace database table name.
            /// </summary>
            public const string Table = "table";
            /// <summary>
            /// WHERE clause placeholder, used to replace query conditions.
            /// </summary>
            public const string Where = "where";
            /// <summary>
            /// ORDER BY clause placeholder, used to replace sorting conditions.
            /// </summary>
            public const string OrderBy = "orderby";
            /// <summary>
            /// INSERT statement placeholder, used to replace insert column names.
            /// </summary>
            public const string Insert = "insert";
            /// <summary>
            /// UPDATE statement placeholder, used to replace update column names.
            /// </summary>
            public const string Update = "update";
            /// <summary>
            /// VALUES clause placeholder, used to replace insert values.
            /// </summary>
            public const string Values = "values";
            /// <summary>
            /// JOIN clause placeholder, used to replace table joins.
            /// </summary>
            public const string Joins = "joins";
            /// <summary>
            /// SELECT clause placeholder, used to replace query column names.
            /// </summary>
            public const string Select = "select";
            /// <summary>
            /// COUNT function placeholder, used to replace count queries.
            /// </summary>
            public const string Count = "count";
        }

        /// <summary>
        /// Process placeholder replacement in SQL templates
        /// </summary>
        /// <param name="sqlTemplate">Original SQL template</param>
        /// <param name="context">Replacement context</param>
        /// <returns>Processed SQL string</returns>
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
                    _ => match.Value // Keep unknown placeholders
                };
            });
        }

        /// <summary>
        /// Check if SQL template contains placeholders
        /// </summary>
        /// <param name="sqlTemplate">SQL template</param>
        /// <returns>Whether it contains placeholders</returns>
        public static bool ContainsPlaceholders(string sqlTemplate)
        {
            return !string.IsNullOrEmpty(sqlTemplate) && PlaceholderRegex.IsMatch(sqlTemplate);
        }

        /// <summary>
        /// Get all placeholders in SQL template
        /// </summary>
        /// <param name="sqlTemplate">SQL template</param>
        /// <returns>List of placeholders</returns>
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
        /// Process {{columns}} placeholder
        /// </summary>
        private static string ProcessColumnsPlaceholder(SqlPlaceholderContext context, string? args)
        {
            if (context.EntityType == null)
                return "*";

            var columns = GetEntityColumns(context.EntityType, context.SqlDefine);

            // Support parameters: {{columns:exclude=Id,CreatedAt}}
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
        /// Process {{table}} placeholder
        /// </summary>
        private static string ProcessTablePlaceholder(SqlPlaceholderContext context, string? args)
        {
            var tableName = context.TableName ?? context.EntityType?.Name ?? "UnknownTable";

            // Support parameters: {{table:alias=u}}
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
        /// Process {{where}} placeholder
        /// </summary>
        private static string ProcessWherePlaceholder(SqlPlaceholderContext context, string? args)
        {
            // Support parameters: {{where:default=1=1}}
            if (!string.IsNullOrEmpty(args))
            {
                var argsDict = ParseArgs(args);
                if (argsDict.TryGetValue("default", out var defaultValue))
                {
                    return defaultValue;
                }
            }

            return "1=1"; // Default safe WHERE condition
        }

        /// <summary>
        /// Process {{orderby}} placeholder
        /// </summary>
        private static string ProcessOrderByPlaceholder(SqlPlaceholderContext context, string? args)
        {
            // Support parameters: {{orderby:default=Id ASC}}
            if (!string.IsNullOrEmpty(args))
            {
                var argsDict = ParseArgs(args);
                if (argsDict.TryGetValue("default", out var defaultValue))
                {
                    return $"ORDER BY {defaultValue}";
                }
            }

            // Try to use primary key field as default sorting
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
        /// Process {{insert}} placeholder
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
        /// Process {{update}} placeholder
        /// </summary>
        private static string ProcessUpdatePlaceholder(SqlPlaceholderContext context, string? args)
        {
            if (context.EntityType == null)
                return "";

            var tableName = context.TableName ?? context.EntityType.Name;
            return $"UPDATE {context.SqlDefine.WrapColumn(tableName)} SET";
        }

        /// <summary>
        /// Process {{values}} placeholder
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
        /// Process {{joins}} placeholder
        /// </summary>
        private static string ProcessJoinsPlaceholder(SqlPlaceholderContext context, string? args)
        {
            // Support parameters: {{joins:type=INNER,table=Department,on=u.DepartmentId=d.Id,alias=d}}
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
        /// Process {{select}} placeholder
        /// </summary>
        private static string ProcessSelectPlaceholder(SqlPlaceholderContext context, string? args)
        {
            var columns = ProcessColumnsPlaceholder(context, args);
            return $"SELECT {columns}";
        }

        /// <summary>
        /// Process {{count}} placeholder
        /// </summary>
        private static string ProcessCountPlaceholder(SqlPlaceholderContext context, string? args)
        {
            // Support parameters: {{count:column=Id}}
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
        /// Get all columns of the entity type
        /// </summary>
        private static List<string> GetEntityColumns(INamedTypeSymbol entityType, SqlDefine sqlDefine, bool excludeIdentity = false)
        {
            var properties = entityType.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(p => p.CanBeReferencedByName && p.DeclaredAccessibility == Accessibility.Public)
                .ToList();

            if (excludeIdentity)
            {
                // Exclude possible identity columns (Id, auto-increment columns, etc.)
                properties = properties.Where(p =>
                    !p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase) ||
                    p.GetAttributes().Any(attr => attr.AttributeClass?.Name == "KeyAttribute")).ToList();
            }

            return properties.Select(p => sqlDefine.WrapColumn(p.Name)).ToList();
        }

        /// <summary>
        /// Get primary key columns
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
        /// Parse placeholder parameters
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
        /// Get column name with wrapper removed
        /// </summary>
        private static string GetColumnNameWithoutWrapper(string wrappedColumn)
        {
            if (string.IsNullOrEmpty(wrappedColumn))
                return wrappedColumn;

            // Remove various database column name wrapper symbols
            return wrappedColumn.Trim('[', ']', '`', '"');
        }
    }

    /// <summary>
    /// SQL placeholder replacement context
    /// </summary>
    public class SqlPlaceholderContext
    {
        /// <summary>
        /// Get or set the entity type symbol.
        /// </summary>
        public INamedTypeSymbol? EntityType { get; set; }
        /// <summary>
        /// Get or set the database table name.
        /// </summary>
        public string? TableName { get; set; }
        /// <summary>
        /// Get or set the SQL dialect definition.
        /// </summary>
        public SqlDefine SqlDefine { get; set; }
        /// <summary>
        /// Get or set the current method symbol being processed.
        /// </summary>
        public IMethodSymbol? Method { get; set; }
        /// <summary>
        /// Get or set additional context data.
        /// </summary>
        public Dictionary<string, object?> AdditionalData { get; set; } = new();

        /// <summary>
        /// Initialize a new instance of the SqlPlaceholderContext class.
        /// </summary>
        /// <param name="sqlDefine">SQL dialect definition.</param>
        public SqlPlaceholderContext(SqlDefine sqlDefine)
        {
            SqlDefine = sqlDefine;
        }
    }
}
