// -----------------------------------------------------------------------
// <copyright file="SqlTemplateProcessor.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using System;
using System.Text.RegularExpressions;

namespace Sqlx.Generator.Placeholders;

/// <summary>
/// SQL模板处理器 - 简化版本
/// 使用策略模式处理占位符，代码更简洁、可扩展
/// </summary>
public sealed class SqlTemplateProcessor
{
    // 占位符正则：匹配 {{name}} 或 {{name type}} 或 {{name --options}}
    private static readonly Regex PlaceholderRegex = new(
        @"\{\{(@)?(\w+)(?:\s+|\:)?([^}]*)\}\}",
        RegexOptions.Compiled);

    // SQL注入检测
    private static readonly Regex SqlInjectionRegex = new(
        @"(?i)(;\s*union\s+select|'\s*union\s+select|drop\s+table|exec\s*\(|execute\s*\(|sp_|xp_|--|\*\/|\/\*)",
        RegexOptions.Compiled);

    // 参数提取正则：匹配 @paramName, $paramName, :paramName
    private static readonly Regex ParameterRegex = new(
        @"[@$:](\w+)",
        RegexOptions.Compiled);

    private readonly PlaceholderRegistry _registry;
    private readonly SqlDefine _defaultDialect;

    public SqlTemplateProcessor(SqlDefine? defaultDialect = null, PlaceholderRegistry? registry = null)
    {
        _defaultDialect = defaultDialect ?? SqlDefine.SqlServer;
        _registry = registry ?? PlaceholderRegistry.Default;
    }

    /// <summary>
    /// 处理SQL模板
    /// </summary>
    public SqlTemplateResult Process(
        string templateSql,
        IMethodSymbol? method,
        INamedTypeSymbol? entityType,
        string tableName,
        SqlDefine? dialect = null)
    {
        var result = new SqlTemplateResult();
        var actualDialect = dialect ?? _defaultDialect;

        // 验证输入
        if (string.IsNullOrWhiteSpace(templateSql))
        {
            result.Warnings.Add("Empty SQL template provided");
            return new SqlTemplateResult { ProcessedSql = "SELECT 1", Warnings = result.Warnings };
        }

        // 安全检查
        if (!ValidateSecurity(templateSql, result))
        {
            return result;
        }

        // 推断实体类型（用于批量操作）
        entityType ??= InferEntityType(method);

        // 处理逻辑占位符
        var sql = method != null
            ? LogicalPlaceholderProcessor.Process(templateSql, method, result)
            : templateSql;

        // 处理普通占位符（支持嵌套，最多3次迭代）
        sql = ProcessPlaceholders(sql, method, entityType, tableName, actualDialect, result);

        // 提取参数
        ExtractParameters(sql, result);

        return new SqlTemplateResult
        {
            ProcessedSql = sql,
            Parameters = result.Parameters,
            Warnings = result.Warnings,
            Errors = result.Errors,
            HasDynamicFeatures = result.HasDynamicFeatures,
            ColumnOrder = result.ColumnOrder
        };
    }

    private static void ExtractParameters(string sql, SqlTemplateResult result)
    {
        var matches = ParameterRegex.Matches(sql);
        foreach (Match match in matches)
        {
            var paramName = match.Groups[1].Value;
            if (!result.Parameters.ContainsKey(paramName))
            {
                result.Parameters[paramName] = null;
            }
        }
    }

    private string ProcessPlaceholders(
        string sql,
        IMethodSymbol? method,
        INamedTypeSymbol? entityType,
        string tableName,
        SqlDefine dialect,
        SqlTemplateResult result)
    {
        string previousSql;
        int iteration = 0;
        const int maxIterations = 3;

        do
        {
            previousSql = sql;
            sql = PlaceholderRegex.Replace(sql, match =>
            {
                var isDynamic = match.Groups[1].Success;
                var name = match.Groups[2].Value.ToLowerInvariant();
                var content = match.Groups[3].Value.Trim();

                // 动态占位符 {{@varName}}
                if (isDynamic)
                {
                    result.HasDynamicFeatures = true;
                    return $"{{{match.Groups[2].Value}}}";
                }

                // 解析type和options
                var (type, options) = ParseTypeAndOptions(content);

                // 创建上下文
                var context = new PlaceholderContext
                {
                    Name = name,
                    Type = type,
                    Options = options,
                    Method = method,
                    EntityType = entityType,
                    TableName = tableName,
                    Dialect = dialect,
                    Result = result
                };

                // 使用注册表处理
                return _registry.Process(context);
            });

            iteration++;
        } while (sql != previousSql && iteration < maxIterations);

        return sql;
    }

    private static (string type, string options) ParseTypeAndOptions(string content)
    {
        if (string.IsNullOrEmpty(content))
            return ("", "");

        // 命令行风格: --exclude Id
        if (content.StartsWith("--"))
            return ("", content);

        // 冒号分隔: type|options
        var colonIndex = content.IndexOf(':');
        if (colonIndex > 0)
        {
            var type = content.Substring(0, colonIndex).Trim();
            var options = content.Substring(colonIndex + 1).Trim();
            return (type, options);
        }

        // 管道分隔: type|options
        var pipeIndex = content.IndexOf('|');
        if (pipeIndex > 0)
        {
            var type = content.Substring(0, pipeIndex).Trim();
            var options = content.Substring(pipeIndex + 1).Trim();
            return (type, options);
        }

        // 空格分隔: 第一个词是type，其余是options
        var spaceIndex = content.IndexOf(' ');
        if (spaceIndex > 0)
        {
            var firstWord = content.Substring(0, spaceIndex).Trim();
            var rest = content.Substring(spaceIndex + 1).Trim();

            // 如果rest以--开头，firstWord是type
            if (rest.StartsWith("--"))
                return (firstWord, rest);

            // 否则整个content作为options（用于简单格式如 {{limit 10}}）
            return ("", content);
        }

        // 单个词：可能是type或简单值
        return (content, "");
    }

    private static bool ValidateSecurity(string sql, SqlTemplateResult result)
    {
        // 移除占位符后检查SQL注入
        var sqlWithoutPlaceholders = PlaceholderRegex.Replace(sql, "__PLACEHOLDER__");

        if (SqlInjectionRegex.IsMatch(sqlWithoutPlaceholders))
        {
            result.Errors.Add("Template contains potential SQL injection patterns");
            return false;
        }

        return true;
    }

    private static INamedTypeSymbol? InferEntityType(IMethodSymbol? method)
    {
        if (method == null) return null;

        // 从IEnumerable<T>参数推断（用于批量操作）
        foreach (var param in method.Parameters)
        {
            if (SharedCodeGenerationUtilities.IsEnumerableParameter(param))
            {
                var paramType = param.Type as INamedTypeSymbol;
                if (paramType?.TypeArguments.Length > 0)
                {
                    return paramType.TypeArguments[0] as INamedTypeSymbol;
                }
            }
        }

        return null;
    }
}
