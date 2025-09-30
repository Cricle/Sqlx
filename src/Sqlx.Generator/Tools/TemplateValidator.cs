// -----------------------------------------------------------------------
// <copyright file="TemplateValidator.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sqlx.Generator.Tools;

/// <summary>
/// Simplified template validation tool.
/// </summary>
public class TemplateValidator
{
    private readonly SqlTemplateEngine _engine;

    /// <summary>
    /// Initializes a new instance of the TemplateValidator class.
    /// </summary>
    /// <param name="engine">Template engine to use for validation.</param>
    public TemplateValidator(SqlTemplateEngine? engine = null)
    {
        _engine = engine ?? new SqlTemplateEngine();
    }

    /// <summary>
    /// Validates a single template and returns basic results.
    /// </summary>
    /// <param name="template">Template to validate.</param>
    /// <param name="context">Optional validation context.</param>
    /// <returns>Validation report.</returns>
    public TemplateValidationReport ValidateTemplate(string template, ValidationContext? context = null)
    {
        var report = new TemplateValidationReport
        {
            Template = template,
            ValidationTime = DateTime.UtcNow
        };

        try
        {
            // Basic syntax validation
            var basicValidation = _engine.ValidateTemplate(template);
            report.IsValid = basicValidation.IsValid;
            report.Errors.AddRange(basicValidation.Errors);
            report.Warnings.AddRange(basicValidation.Warnings);
            report.Suggestions.AddRange(basicValidation.Suggestions);

            // Process template if context provided
            if (context != null)
            {
                var templateResult = _engine.ProcessTemplate(template, context.Method!, context.EntityType, context.TableName ?? "test_table");
                report.ProcessedSql = templateResult.ProcessedSql;
                report.HasDynamicFeatures = templateResult.HasDynamicFeatures;
                // 将字典转换为ParameterMapping列表
                foreach (var param in templateResult.Parameters)
                {
                    report.Parameters.Add(new ParameterMapping
                    {
                        Name = param.Key,
                        Type = "object",
                        Value = param.Value,
                        IsNullable = param.Value == null
                    });
                }
                report.Warnings.AddRange(templateResult.Warnings);
                report.Errors.AddRange(templateResult.Errors);
            }
        }
        catch (Exception ex)
        {
            report.IsValid = false;
            report.Errors.Add($"Validation failed: {ex.Message}");
        }

        return report;
    }

    /// <summary>
    /// Validates multiple templates and returns a batch report.
    /// </summary>
    /// <param name="templates">Templates to validate.</param>
    /// <returns>Batch validation report.</returns>
    public BatchValidationReport ValidateTemplates(IEnumerable<TemplateInfo> templates)
    {
        var report = new BatchValidationReport
        {
            ValidationTime = DateTime.UtcNow
        };

        foreach (var template in templates)
        {
            var templateReport = ValidateTemplate(template.Template);
            templateReport.Name = template.Name;
            templateReport.Source = template.Source;
            report.TemplateReports.Add(templateReport);
        }

        report.TotalCount = report.TemplateReports.Count;
        report.PassedCount = report.TemplateReports.Count(r => r.IsValid);
        report.FailedCount = report.TotalCount - report.PassedCount;
        report.SuccessRate = report.TotalCount > 0 ? (report.PassedCount * 100.0) / report.TotalCount : 0;

        return report;
    }
}

/// <summary>
/// Validation context information.
/// </summary>
public class ValidationContext
{
    /// <summary>方法符号信息</summary>
    public IMethodSymbol? Method { get; set; }
    /// <summary>实体类型符号</summary>
    public INamedTypeSymbol? EntityType { get; set; }
    /// <summary>表名称</summary>
    public string? TableName { get; set; }
}

/// <summary>
/// Template information for validation.
/// </summary>
public class TemplateInfo
{
    /// <summary>模板名称</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>模板内容</summary>
    public string Template { get; set; } = string.Empty;
    /// <summary>模板来源</summary>
    public string Source { get; set; } = string.Empty;
}

/// <summary>
/// Template validation report.
/// </summary>
public class TemplateValidationReport
{
    /// <summary>模板名称</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>模板来源</summary>
    public string Source { get; set; } = string.Empty;
    /// <summary>模板内容</summary>
    public string Template { get; set; } = string.Empty;
    /// <summary>验证是否成功</summary>
    public bool IsValid { get; set; } = true;
    /// <summary>验证时间</summary>
    public DateTime ValidationTime { get; set; }
    /// <summary>处理后的SQL</summary>
    public string ProcessedSql { get; set; } = string.Empty;
    /// <summary>是否包含动态特性</summary>
    public bool HasDynamicFeatures { get; set; }
    /// <summary>错误列表</summary>
    public List<string> Errors { get; set; } = new();
    /// <summary>警告列表</summary>
    public List<string> Warnings { get; set; } = new();
    /// <summary>建议列表</summary>
    public List<string> Suggestions { get; set; } = new();
    /// <summary>参数映射列表</summary>
    public List<ParameterMapping> Parameters { get; set; } = new();
    /// <summary>元数据字典</summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Batch validation report.
/// </summary>
public class BatchValidationReport
{
    /// <summary>批量验证时间</summary>
    public DateTime ValidationTime { get; set; }
    /// <summary>总模板数量</summary>
    public int TotalCount { get; set; }
    /// <summary>验证通过数量</summary>
    public int PassedCount { get; set; }
    /// <summary>验证失败数量</summary>
    public int FailedCount { get; set; }
    /// <summary>成功率</summary>
    public double SuccessRate { get; set; }
    /// <summary>模板验证报告列表</summary>
    public List<TemplateValidationReport> TemplateReports { get; set; } = new();
}
