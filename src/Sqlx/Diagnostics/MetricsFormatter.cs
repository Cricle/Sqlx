// <copyright file="MetricsFormatter.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx.Diagnostics;

using System;
using System.Linq;
using System.Text;

/// <summary>
/// Formats SQL template metrics for display and logging.
/// </summary>
public static class MetricsFormatter
{
    /// <summary>
    /// Formats global metrics as a human-readable string.
    /// </summary>
    /// <param name="metrics">The global metrics snapshot.</param>
    /// <returns>Formatted string representation.</returns>
    public static string FormatGlobalMetrics(GlobalMetricSnapshot metrics)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== Global SQL Template Metrics ===");
        sb.AppendLine($"Total Executions:  {metrics.TotalExecutions:N0}");
        sb.AppendLine($"Total Errors:      {metrics.TotalErrors:N0}");
        sb.AppendLine($"Success Rate:      {metrics.SuccessRate:F2}%");
        sb.AppendLine($"Avg Execution:     {metrics.AverageExecutionMs:F3} ms");
        sb.AppendLine($"Template Count:    {metrics.TemplateCount:N0}");
        return sb.ToString();
    }

    /// <summary>
    /// Formats template metrics as a human-readable string.
    /// </summary>
    /// <param name="metrics">The template metrics snapshot.</param>
    /// <returns>Formatted string representation.</returns>
    public static string FormatTemplateMetrics(TemplateMetricSnapshot metrics)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"=== Template: {metrics.TemplateKey} ===");
        sb.AppendLine($"Executions:        {metrics.ExecutionCount:N0}");
        sb.AppendLine($"Errors:            {metrics.ErrorCount:N0}");
        sb.AppendLine($"Success Rate:      {metrics.SuccessRate:F2}%");
        sb.AppendLine($"Avg Execution:     {metrics.AverageExecutionMs:F3} ms");
        sb.AppendLine($"Min Execution:     {metrics.MinExecutionMs:F3} ms");
        sb.AppendLine($"Max Execution:     {metrics.MaxExecutionMs:F3} ms");
        sb.AppendLine($"Total Time:        {metrics.TotalExecutionMs:F3} ms");
        sb.AppendLine($"First Execution:   {metrics.FirstExecutionUtc:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"Last Execution:    {metrics.LastExecutionUtc:yyyy-MM-dd HH:mm:ss} UTC");
        if (metrics.ErrorTypes.Count > 0)
        {
            sb.AppendLine("Error Types:");
            foreach (var (type, count) in metrics.ErrorTypes.OrderByDescending(kv => kv.Value))
                sb.AppendLine($"  {type}: {count:N0}");
        }
        return sb.ToString();
    }

    /// <summary>
    /// Formats multiple template metrics as a table.
    /// </summary>
    /// <param name="metrics">Array of template metrics.</param>
    /// <returns>Formatted table string.</returns>
    public static string FormatMetricsTable(TemplateMetricSnapshot[] metrics)
    {
        if (metrics.Length == 0) return "No metrics available.";
        var sb = new StringBuilder();
        sb.AppendLine("Template Key                          | Executions | Errors | Success% | Avg(ms) | Min(ms) | Max(ms)");
        sb.AppendLine("--------------------------------------|------------|--------|----------|---------|---------|--------");
        foreach (var m in metrics)
        {
            var key = m.TemplateKey.Length > 37 ? m.TemplateKey.Substring(0, 34) + "..." : m.TemplateKey.PadRight(37);
            sb.AppendLine($"{key} | {m.ExecutionCount,10:N0} | {m.ErrorCount,6:N0} | {m.SuccessRate,7:F2}% | {m.AverageExecutionMs,7:F3} | {m.MinExecutionMs,7:F3} | {m.MaxExecutionMs,7:F3}");
        }
        return sb.ToString();
    }

    /// <summary>
    /// Formats metrics as JSON (simple, AOT-compatible).
    /// </summary>
    /// <param name="metrics">The template metrics snapshot.</param>
    /// <returns>JSON string representation.</returns>
    public static string FormatAsJson(TemplateMetricSnapshot metrics)
    {
        var sb = new StringBuilder();
        sb.Append("{");
        sb.Append($"\"templateKey\":\"{EscapeJson(metrics.TemplateKey)}\",");
        sb.Append($"\"executionCount\":{metrics.ExecutionCount},");
        sb.Append($"\"errorCount\":{metrics.ErrorCount},");
        sb.Append($"\"successRate\":{metrics.SuccessRate:F2},");
        sb.Append($"\"averageExecutionMs\":{metrics.AverageExecutionMs:F3},");
        sb.Append($"\"minExecutionMs\":{metrics.MinExecutionMs:F3},");
        sb.Append($"\"maxExecutionMs\":{metrics.MaxExecutionMs:F3},");
        sb.Append($"\"totalExecutionMs\":{metrics.TotalExecutionMs:F3},");
        sb.Append($"\"firstExecutionUtc\":\"{metrics.FirstExecutionUtc:O}\",");
        sb.Append($"\"lastExecutionUtc\":\"{metrics.LastExecutionUtc:O}\"");
        if (metrics.ErrorTypes.Count > 0)
        {
            sb.Append(",\"errorTypes\":{");
            sb.Append(string.Join(",", metrics.ErrorTypes.Select(kv => $"\"{EscapeJson(kv.Key)}\":{kv.Value}")));
            sb.Append("}");
        }
        sb.Append("}");
        return sb.ToString();
    }

    /// <summary>
    /// Formats global metrics as JSON.
    /// </summary>
    /// <param name="metrics">The global metrics snapshot.</param>
    /// <returns>JSON string representation.</returns>
    public static string FormatAsJson(GlobalMetricSnapshot metrics)
    {
        return $"{{\"totalExecutions\":{metrics.TotalExecutions},\"totalErrors\":{metrics.TotalErrors},\"successRate\":{metrics.SuccessRate:F2},\"averageExecutionMs\":{metrics.AverageExecutionMs:F3},\"templateCount\":{metrics.TemplateCount}}}";
    }

    private static string EscapeJson(string value)
    {
        return value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
    }
}
