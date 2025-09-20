// -----------------------------------------------------------------------
// <copyright file="TemplateValidator.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using Sqlx.Generator.Core;

namespace Sqlx.Generator.Tools;

/// <summary>
/// Simplified template validation tool.
/// </summary>
public class TemplateValidator
{
    private readonly ISqlTemplateEngine _engine;

    /// <summary>
    /// Initializes a new instance of the TemplateValidator class.
    /// </summary>
    /// <param name="engine">Template engine to use for validation.</param>
    public TemplateValidator(ISqlTemplateEngine? engine = null)
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
                report.Parameters.AddRange(templateResult.Parameters);
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
    public IMethodSymbol? Method { get; set; }
    public INamedTypeSymbol? EntityType { get; set; }
    public string? TableName { get; set; }
}

/// <summary>
/// Template information for validation.
/// </summary>
public class TemplateInfo
{
    public string Name { get; set; } = string.Empty;
    public string Template { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
}

/// <summary>
/// Template validation report.
/// </summary>
public class TemplateValidationReport
{
    public string Name { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Template { get; set; } = string.Empty;
    public bool IsValid { get; set; } = true;
    public DateTime ValidationTime { get; set; }
    public string ProcessedSql { get; set; } = string.Empty;
    public bool HasDynamicFeatures { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<string> Suggestions { get; set; } = new();
    public List<ParameterMapping> Parameters { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Batch validation report.
/// </summary>
public class BatchValidationReport
{
    public DateTime ValidationTime { get; set; }
    public int TotalCount { get; set; }
    public int PassedCount { get; set; }
    public int FailedCount { get; set; }
    public double SuccessRate { get; set; }
    public List<TemplateValidationReport> TemplateReports { get; set; } = new();
}