// -----------------------------------------------------------------------
// <copyright file="SqlTemplateEngineExtensions.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Linq;
using Sqlx.Generator.Placeholders;

namespace Sqlx.Generator;

/// <summary>
/// SQL模板引擎扩展 - 提供向后兼容的静态方法
/// </summary>
public static class SqlTemplateEngineExtensions
{
    /// <summary>
    /// 多数据库占位符支持
    /// </summary>
    public static class MultiDatabasePlaceholderSupport
    {
        /// <summary>
        /// 处理聚合函数
        /// </summary>
        public static string ProcessAggregateFunction(string functionName, string columnName, string options, SqlDefine dialect)
        {
            var func = functionName.ToUpperInvariant();
            var col = columnName?.ToLowerInvariant() ?? "*";
            
            // 处理特殊情况
            if (col == "all" || col == "*")
            {
                col = "*";
            }
            else
            {
                col = SharedCodeGenerationUtilities.ConvertToSnakeCase(col);
            }

            // 检查DISTINCT选项
            var isDistinct = options?.Contains("distinct=true") == true;
            
            return isDistinct ? $"{func}(DISTINCT {col})" : $"{func}({col})";
        }

        /// <summary>
        /// 处理通用占位符
        /// </summary>
        public static string ProcessGenericPlaceholder(string name, string type, string options, SqlDefine dialect)
        {
            var result = new SqlTemplateResult();
            var context = new PlaceholderContext
            {
                Name = name.ToLowerInvariant(),
                Type = type,
                Options = options,
                Dialect = dialect,
                Result = result
            };

            // 解析选项
            var opts = new PlaceholderOptions(options);

            return name.ToLowerInvariant() switch
            {
                "join" or "inner_join" or "left_join" or "right_join" or "full_join" => ProcessJoin(name, type, opts, dialect),
                "groupby" => ProcessGroupBy(type, dialect),
                "having" => ProcessHaving(type, opts, dialect),
                _ => PlaceholderRegistry.Default.Process(context)
            };
        }

        private static string ProcessJoin(string name, string type, PlaceholderOptions opts, SqlDefine dialect)
        {
            var tableName = opts.Get("table", "");
            var onCondition = opts.Get("on", "id");
            
            var joinType = name.ToLowerInvariant() switch
            {
                "inner_join" => "INNER",
                "left_join" => "LEFT",
                "right_join" => "RIGHT",
                "full_join" => "FULL",
                _ => type?.ToUpperInvariant() ?? "INNER"
            };

            var wrappedTable = dialect.WrapColumn(SharedCodeGenerationUtilities.ConvertToSnakeCase(tableName));
            var wrappedOn = dialect.WrapColumn(SharedCodeGenerationUtilities.ConvertToSnakeCase(onCondition));

            return $"{joinType} JOIN {wrappedTable} ON {wrappedOn}";
        }

        private static string ProcessGroupBy(string columns, SqlDefine dialect)
        {
            if (string.IsNullOrEmpty(columns))
                return "GROUP BY";

            var cols = columns.Split(',')
                .Select(c => c.Trim())
                .Where(c => !string.IsNullOrEmpty(c))
                .Select(c => dialect.WrapColumn(SharedCodeGenerationUtilities.ConvertToSnakeCase(c)));

            return $"GROUP BY {string.Join(", ", cols)}";
        }

        private static string ProcessHaving(string type, PlaceholderOptions opts, SqlDefine dialect)
        {
            var func = type?.ToUpperInvariant() ?? "COUNT";
            var column = opts.Get("column", "*");
            var min = opts.Get("min", "0");

            if (column != "*")
            {
                column = dialect.WrapColumn(SharedCodeGenerationUtilities.ConvertToSnakeCase(column));
            }

            return $"HAVING {func}({column}) >= {min}";
        }
    }
}
