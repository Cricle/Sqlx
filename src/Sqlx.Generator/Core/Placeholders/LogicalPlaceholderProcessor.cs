// -----------------------------------------------------------------------
// <copyright file="LogicalPlaceholderProcessor.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Sqlx.Generator.Placeholders;

/// <summary>
/// 逻辑占位符处理器 - 处理条件块占位符
/// 支持: {{*ifnull}}, {{*ifnotnull}}, {{*ifempty}}, {{*ifnotempty}}
/// </summary>
public static class LogicalPlaceholderProcessor
{
    // 匹配逻辑占位符块
    private static readonly Regex LogicalRegex = new(
        @"\{\{\*(ifnull|ifnotnull|ifempty|ifnotempty)\s+(\w+)\}\}(.*?)\{\{/\1\}\}",
        RegexOptions.Compiled | RegexOptions.Singleline);

    /// <summary>
    /// 处理SQL中的逻辑占位符
    /// </summary>
    public static string Process(string sql, IMethodSymbol method, SqlTemplateResult result)
    {
        return LogicalRegex.Replace(sql, match =>
        {
            var condition = match.Groups[1].Value;
            var paramName = match.Groups[2].Value;
            var content = match.Groups[3].Value;

            // 验证参数存在
            var param = method.Parameters.FirstOrDefault(p =>
                p.Name.Equals(paramName, StringComparison.OrdinalIgnoreCase));

            if (param == null)
            {
                result.Warnings.Add($"Logical placeholder references unknown parameter: {paramName}");
                return "";
            }

            // 生成运行时标记
            var guid = Guid.NewGuid().ToString("N");
            var escapedContent = EscapeContent(content);
            
            return $"{{{{RUNTIME_LOGICAL_{condition.ToUpperInvariant()}_{paramName}_{guid}}}}}{{{{CONTENT:{escapedContent}}}}}{{{{/RUNTIME_LOGICAL}}}}";
        });
    }

    /// <summary>
    /// 生成逻辑条件代码
    /// </summary>
    public static string GenerateConditionCode(
        string conditionType,
        string paramName,
        IMethodSymbol method,
        IndentedStringBuilder sb)
    {
        var param = method.Parameters.FirstOrDefault(p =>
            p.Name.Equals(paramName, StringComparison.OrdinalIgnoreCase));

        if (param == null) return "";

        var varName = $"__{conditionType.ToLower()}_{paramName}__";
        var conditionExpr = GetConditionExpression(conditionType, paramName, param);

        sb.AppendLine($"// Logical: {conditionType} {paramName}");
        sb.AppendLine($"var {varName} = {conditionExpr};");

        return $"{{__{conditionType.ToUpperInvariant()}_{paramName}__}}";
    }

    /// <summary>
    /// 获取条件表达式
    /// </summary>
    private static string GetConditionExpression(string conditionType, string paramName, IParameterSymbol param)
    {
        var isString = param.Type.SpecialType == SpecialType.System_String;
        var isCollection = SharedCodeGenerationUtilities.IsEnumerableParameter(param);

        return conditionType.ToUpperInvariant() switch
        {
            "IFNULL" => $"{paramName} == null ? __content__ : \"\"",
            "IFNOTNULL" => $"{paramName} != null ? __content__ : \"\"",
            "IFEMPTY" => isString
                ? $"string.IsNullOrEmpty({paramName}) ? __content__ : \"\""
                : isCollection
                    ? $"({paramName} == null || !{paramName}.Any()) ? __content__ : \"\""
                    : $"{paramName} == null ? __content__ : \"\"",
            "IFNOTEMPTY" => isString
                ? $"!string.IsNullOrEmpty({paramName}) ? __content__ : \"\""
                : isCollection
                    ? $"({paramName} != null && {paramName}.Any()) ? __content__ : \"\""
                    : $"{paramName} != null ? __content__ : \"\"",
            _ => "\"\""
        };
    }

    private static string EscapeContent(string content) =>
        content.Replace("{{", "⟪⟪").Replace("}}", "⟫⟫");

    public static string UnescapeContent(string content) =>
        content.Replace("⟪⟪", "{{").Replace("⟫⟫", "}}");
}
