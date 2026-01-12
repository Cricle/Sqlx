// -----------------------------------------------------------------------
// <copyright file="SqlTemplateEngineExtensions.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Sqlx.Generator.Placeholders;

namespace Sqlx.Generator;

/// <summary>
/// SQL模板引擎扩展 - 提供向后兼容的静态方法.
/// </summary>
public static class SqlTemplateEngineExtensions
{
    /// <summary>
    /// 多数据库占位符支持.
    /// </summary>
    public static class MultiDatabasePlaceholderSupport
    {
        /// <summary>
        /// 处理聚合函数.
        /// </summary>
        public static string ProcessAggregateFunction(string functionName, string columnName, string options, SqlDefine dialect)
        {
            var func = functionName.ToUpperInvariant();
            var col = columnName?.ToLowerInvariant() ?? "*";

            if (col == "all" || col == "*")
            {
                col = "*";
            }
            else
            {
                col = SharedCodeGenerationUtilities.ConvertToSnakeCase(col);
            }

            var isDistinct = options?.Contains("distinct=true") == true;

            return isDistinct ? $"{func}(DISTINCT {col})" : $"{func}({col})";
        }

        /// <summary>
        /// 处理通用占位符.
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

            return PlaceholderRegistry.Default.Process(context);
        }
    }
}
