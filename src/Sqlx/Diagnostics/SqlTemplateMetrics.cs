// <copyright file="SqlTemplateMetrics.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx.Diagnostics;

using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;

/// <summary>
/// Provides metrics instrumentation for SQL template execution using System.Diagnostics.Metrics.
/// </summary>
/// <remarks>
/// Metrics are exposed through the standard .NET Meter API and can be consumed by:
/// - OpenTelemetry
/// - Prometheus
/// - Application Insights
/// - Any other metrics provider that supports System.Diagnostics.Metrics
/// 
/// Tags recorded:
/// - repository.class: Full repository class name
/// - repository.method: Method name
/// - sql.template: SQL template field name
/// 
/// Note: Requires System.Diagnostics.DiagnosticSource 6.0.0 or later for Metrics support.
/// </remarks>
public static class SqlTemplateMetrics
{
    private static readonly Meter _meter = new("Sqlx.SqlTemplate", "1.0.0");
    
    private static readonly Histogram<double> _executionDuration = _meter.CreateHistogram<double>(
        name: "sqlx.template.duration",
        unit: "ms",
        description: "SQL template execution duration in milliseconds");
    
    private static readonly Counter<long> _executionCount = _meter.CreateCounter<long>(
        name: "sqlx.template.executions",
        unit: "{execution}",
        description: "Total number of SQL template executions");
    
    private static readonly Counter<long> _errorCount = _meter.CreateCounter<long>(
        name: "sqlx.template.errors",
        unit: "{error}",
        description: "Total number of SQL template execution errors");

    /// <summary>
    /// Records a successful SQL template execution.
    /// </summary>
    /// <param name="repositoryClass">Full repository class name.</param>
    /// <param name="methodName">Method name.</param>
    /// <param name="sqlTemplateField">SQL template field name.</param>
    /// <param name="elapsedTicks">Execution duration in ticks (from Stopwatch).</param>
    public static void RecordExecution(string repositoryClass, string methodName, string sqlTemplateField, long elapsedTicks)
    {
        var durationMs = elapsedTicks * 1000.0 / Stopwatch.Frequency;
        var tags = new TagList
        {
            { "repository.class", repositoryClass },
            { "repository.method", methodName },
            { "sql.template", sqlTemplateField }
        };
        
        _executionDuration.Record(durationMs, tags);
        _executionCount.Add(1, tags);
    }

    /// <summary>
    /// Records a failed SQL template execution.
    /// </summary>
    /// <param name="repositoryClass">Full repository class name.</param>
    /// <param name="methodName">Method name.</param>
    /// <param name="sqlTemplateField">SQL template field name.</param>
    /// <param name="elapsedTicks">Execution duration in ticks before failure.</param>
    /// <param name="exception">The exception that occurred.</param>
    public static void RecordError(string repositoryClass, string methodName, string sqlTemplateField, long elapsedTicks, Exception exception)
    {
        var durationMs = elapsedTicks * 1000.0 / Stopwatch.Frequency;
        var tags = new TagList
        {
            { "repository.class", repositoryClass },
            { "repository.method", methodName },
            { "sql.template", sqlTemplateField },
            { "error.type", exception.GetType().Name }
        };
        
        _executionDuration.Record(durationMs, tags);
        _errorCount.Add(1, tags);
    }
}
