// -----------------------------------------------------------------------
// <copyright file="CompileTimeSqlTemplate.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Sqlx.Annotations;

namespace Sqlx
{
    /// <summary>
    /// 编译时 SQL 模板处理器，提供安全的 SQL 生成功能
    /// 只在编译时使用，运行时不执行动态 SQL 拼接
    /// </summary>
    public static class CompileTimeSqlTemplate
    {
        /// <summary>
        /// 参数占位符的正则表达式模式
        /// 匹配 @{参数名} 格式的占位符
        /// </summary>
        private static readonly Regex ParameterPattern = new Regex(
            @"@\{([a-zA-Z_][a-zA-Z0-9_]*)\}",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// SQL 关键字列表，用于安全检查
        /// </summary>
        private static readonly HashSet<string> SqlKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "SELECT", "INSERT", "UPDATE", "DELETE", "DROP", "CREATE", "ALTER", "TRUNCATE",
            "EXEC", "EXECUTE", "UNION", "SCRIPT", "SHUTDOWN", "xp_cmdshell"
        };

        /// <summary>
        /// 危险字符串列表，用于 SQL 注入检查
        /// </summary>
        private static readonly string[] DangerousStrings = { ";", "'", "\"", "--", "/*", "*/", "xp_" };

        /// <summary>
        /// 解析 SQL 模板，提取参数信息
        /// </summary>
        /// <param name="template">SQL 模板字符串</param>
        /// <returns>解析结果</returns>
        public static SqlTemplateParseResult ParseTemplate(string template)
        {
            if (string.IsNullOrEmpty(template))
                throw new ArgumentException("SQL template cannot be null or empty", nameof(template));

            var parameters = new List<string>();
            var matches = ParameterPattern.Matches(template);

            foreach (Match match in matches)
            {
                var paramName = match.Groups[1].Value;
                if (!parameters.Contains(paramName))
                {
                    parameters.Add(paramName);
                }
            }

            return new SqlTemplateParseResult
            {
                Template = template,
                Parameters = parameters,
                IsValid = ValidateTemplate(template),
                ParameterCount = parameters.Count
            };
        }

        /// <summary>
        /// 验证 SQL 模板的安全性
        /// </summary>
        /// <param name="template">SQL 模板</param>
        /// <returns>是否安全</returns>
        public static bool ValidateTemplate(string template)
        {
            if (string.IsNullOrEmpty(template))
                return false;

            // 检查是否包含危险的 SQL 注入模式
            var templateUpper = template.ToUpperInvariant();

            // 检查多语句执行
            if (templateUpper.Contains(";") &&
                (templateUpper.Contains("DROP") || templateUpper.Contains("DELETE") || templateUpper.Contains("TRUNCATE")))
            {
                return false;
            }

            // 检查注释注入
            if (template.Contains("--") || template.Contains("/*"))
            {
                return false;
            }

            // 检查动态 SQL 执行
            if (templateUpper.Contains("EXEC") || templateUpper.Contains("EXECUTE") || templateUpper.Contains("SP_EXECUTESQL"))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 生成编译时安全的 SQL 代码
        /// </summary>
        /// <param name="parseResult">解析结果</param>
        /// <param name="dialect">数据库方言</param>
        /// <returns>生成的 SQL 代码</returns>
        public static string GenerateCompileTimeCode(SqlTemplateParseResult parseResult, SqlDialectType dialect)
        {
            if (!parseResult.IsValid)
                throw new InvalidOperationException("Invalid SQL template cannot be compiled");

            var sqlDialect = GetSqlDialect(dialect);
            var codeBuilder = new StringBuilder();

            // 生成参数化查询代码
            codeBuilder.AppendLine("// Generated compile-time SQL template code");
            codeBuilder.AppendLine("var sql = @\"" + EscapeStringForCode(parseResult.Template) + "\";");

            if (parseResult.Parameters.Count > 0)
            {
                codeBuilder.AppendLine("var parameters = new Dictionary<string, object?>");
                codeBuilder.AppendLine("{");

                foreach (var param in parseResult.Parameters)
                {
                    var parameterPrefix = sqlDialect.ParameterPrefix;
                    codeBuilder.AppendLine($"    {{\"{parameterPrefix}{param}\", {param}}},");
                }

                codeBuilder.AppendLine("};");

                // 替换占位符为实际的参数标记
                codeBuilder.AppendLine("// Replace placeholders with dialect-specific parameter markers");
                foreach (var param in parseResult.Parameters)
                {
                    var placeholder = $"@{{{param}}}";
                    var parameterMark = $"{sqlDialect.ParameterPrefix}{param}";
                    codeBuilder.AppendLine($"sql = sql.Replace(\"{placeholder}\", \"{parameterMark}\");");
                }
            }

            codeBuilder.AppendLine("return new ParameterizedSql(sql, parameters ?? new Dictionary<string, object?>());");

            return codeBuilder.ToString();
        }

        /// <summary>
        /// 获取 SQL 方言配置
        /// </summary>
        private static SqlDialect GetSqlDialect(SqlDialectType dialectType)
        {
            return dialectType switch
            {
                SqlDialectType.SqlServer => SqlDefine.SqlServer,
                SqlDialectType.MySql => SqlDefine.MySql,
                SqlDialectType.PostgreSql => SqlDefine.PostgreSql,
                SqlDialectType.SQLite => SqlDefine.SQLite,
                SqlDialectType.Oracle => SqlDefine.Oracle,
                SqlDialectType.DB2 => SqlDefine.DB2,
                _ => SqlDefine.SqlServer
            };
        }

        /// <summary>
        /// 转义字符串用于代码生成
        /// </summary>
        private static string EscapeStringForCode(string input)
        {
            return input.Replace("\"", "\"\"").Replace("\r\n", "\\r\\n").Replace("\n", "\\n");
        }
    }

    /// <summary>
    /// SQL 模板解析结果
    /// </summary>
    public class SqlTemplateParseResult
    {
        /// <summary>
        /// 原始模板
        /// </summary>
        public string Template { get; set; } = string.Empty;

        /// <summary>
        /// 提取的参数列表
        /// </summary>
        public List<string> Parameters { get; set; } = new List<string>();

        /// <summary>
        /// 模板是否有效和安全
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// 参数数量
        /// </summary>
        public int ParameterCount { get; set; }

        /// <summary>
        /// 错误信息（如果有）
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
}
