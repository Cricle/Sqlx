// -----------------------------------------------------------------------
// <copyright file="SqlTemplateSafetyValidator.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Sqlx
{
    /// <summary>
    /// SQL 模板安全性验证器，用于编译时检查 SQL 注入风险
    /// </summary>
    public static class SqlTemplateSafetyValidator
    {
        /// <summary>
        /// 危险的 SQL 关键字模式
        /// </summary>
        private static readonly string[] DangerousPatterns =
        {
            @"\b(exec|execute|sp_executesql)\b",
            @"\b(xp_cmdshell|xp_regwrite|xp_regread)\b",
            @"\b(openrowset|opendatasource)\b",
            @"\b(shutdown|drop\s+database)\b",
            @"--[^\r\n]*",
            @"/\*.*?\*/",
            @";\s*(drop|delete|truncate|alter|create)\b",
            @"'\s*;\s*(drop|delete|truncate)\b",
            @"\bunion\s+select\b",
            @"'\s*union\s+select\b"
        };

        /// <summary>
        /// 编译的正则表达式缓存
        /// </summary>
        private static readonly Regex[] CompiledPatterns = DangerousPatterns
            .Select(pattern => new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled))
            .ToArray();

        /// <summary>
        /// 验证 SQL 模板的安全性
        /// </summary>
        /// <param name="sqlTemplate">SQL 模板字符串</param>
        /// <returns>验证结果</returns>
        public static SqlSafetyValidationResult ValidateSqlTemplate(string sqlTemplate)
        {
            if (string.IsNullOrWhiteSpace(sqlTemplate))
            {
                return new SqlSafetyValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "SQL 模板不能为空"
                };
            }

            var violations = new List<string>();

            // 检查危险模式
            for (int i = 0; i < CompiledPatterns.Length; i++)
            {
                var pattern = CompiledPatterns[i];
                if (pattern.IsMatch(sqlTemplate))
                {
                    violations.Add($"检测到危险模式: {DangerousPatterns[i]}");
                }
            }

            // 检查参数占位符格式
            var parameterValidation = ValidateParameterPlaceholders(sqlTemplate);
            if (!parameterValidation.IsValid)
            {
                violations.Add(parameterValidation.ErrorMessage!);
            }

            // 检查 SQL 语句结构
            var structureValidation = ValidateSqlStructure(sqlTemplate);
            if (!structureValidation.IsValid)
            {
                violations.Add(structureValidation.ErrorMessage!);
            }

            return new SqlSafetyValidationResult
            {
                IsValid = violations.Count == 0,
                ErrorMessage = violations.Count > 0 ? string.Join("; ", violations) : null,
                Violations = violations
            };
        }

        /// <summary>
        /// 验证参数占位符格式
        /// </summary>
        private static SqlSafetyValidationResult ValidateParameterPlaceholders(string sqlTemplate)
        {
            // 检查是否使用了正确的参数占位符格式 @{paramName}
            var validPlaceholderPattern = new Regex(@"@\{([a-zA-Z_][a-zA-Z0-9_]*)\}", RegexOptions.Compiled);
            var invalidPlaceholderPattern = new Regex(@"@[a-zA-Z_][a-zA-Z0-9_]*(?!\})", RegexOptions.Compiled);

            var invalidMatches = invalidPlaceholderPattern.Matches(sqlTemplate);
            var validMatches = validPlaceholderPattern.Matches(sqlTemplate);

            // 排除已经被有效占位符覆盖的无效匹配
            var reallyInvalidMatches = invalidMatches.Cast<Match>()
                .Where(invalid => !validMatches.Cast<Match>()
                    .Any(valid => valid.Index <= invalid.Index &&
                                 valid.Index + valid.Length >= invalid.Index + invalid.Length))
                .ToList();

            if (reallyInvalidMatches.Any())
            {
                return new SqlSafetyValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "请使用 @{参数名} 格式的参数占位符，而不是直接的 @参数名"
                };
            }

            return new SqlSafetyValidationResult { IsValid = true };
        }

        /// <summary>
        /// 验证 SQL 语句结构
        /// </summary>
        private static SqlSafetyValidationResult ValidateSqlStructure(string sqlTemplate)
        {
            var trimmedSql = sqlTemplate.Trim().ToUpperInvariant();

            // 检查是否以合法的 SQL 语句开头
            var validStarters = new[] { "SELECT", "INSERT", "UPDATE", "DELETE", "WITH" };
            if (!validStarters.Any(starter => trimmedSql.StartsWith(starter)))
            {
                return new SqlSafetyValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "SQL 模板必须以 SELECT、INSERT、UPDATE、DELETE 或 WITH 开头"
                };
            }

            // 检查是否包含多个语句（分号分隔）
            var statements = sqlTemplate.Split(';')
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToArray();

            if (statements.Length > 1)
            {
                return new SqlSafetyValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "为了安全起见，SQL 模板不允许包含多个语句"
                };
            }

            return new SqlSafetyValidationResult { IsValid = true };
        }

        /// <summary>
        /// 提取 SQL 模板中的参数名
        /// </summary>
        /// <param name="sqlTemplate">SQL 模板</param>
        /// <returns>参数名列表</returns>
        public static List<string> ExtractParameterNames(string sqlTemplate)
        {
            var parameterPattern = new Regex(@"@\{([a-zA-Z_][a-zA-Z0-9_]*)\}", RegexOptions.Compiled);
            var matches = parameterPattern.Matches(sqlTemplate);

            return matches.Cast<Match>()
                .Select(m => m.Groups[1].Value)
                .Distinct()
                .ToList();
        }
    }

    /// <summary>
    /// SQL 安全性验证结果
    /// </summary>
    public class SqlSafetyValidationResult
    {
        /// <summary>
        /// 是否通过验证
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 违规详情列表
        /// </summary>
        public List<string> Violations { get; set; } = new List<string>();
    }
}
