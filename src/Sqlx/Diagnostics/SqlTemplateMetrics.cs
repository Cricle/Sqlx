// <copyright file="SqlTemplateMetrics.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx.Diagnostics;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

/// <summary>
/// High-performance, AOT-compatible metrics collector for SQL template execution.
/// Tracks execution count, duration, errors, and provides statistical analysis.
/// </summary>
/// <remarks>
/// Thread-safe and lock-free for high-throughput scenarios.
/// Uses struct-based metrics to minimize allocations.
/// </remarks>
public sealed class SqlTemplateMetrics
{
    private static readonly ConcurrentDictionary<string, TemplateMetricEntry> _metrics = new();
    private static long _globalExecutionCount;
    private static long _globalErrorCount;
    private static long _globalTotalTicks;

    /// <summary>
    /// Records a successful SQL template execution.
    /// </summary>
    /// <param name="templateKey">Unique identifier for the template (e.g., method name or SQL hash).</param>
    /// <param name="elapsedTicks">Execution duration in ticks (from Stopwatch).</param>
    public static void RecordExecution(string templateKey, long elapsedTicks)
    {
        Interlocked.Increment(ref _globalExecutionCount);
        Interlocked.Add(ref _globalTotalTicks, elapsedTicks);
        var entry = _metrics.GetOrAdd(templateKey, _ => new TemplateMetricEntry(templateKey));
        entry.RecordSuccess(elapsedTicks);
    }

    /// <summary>
    /// Records a failed SQL template execution.
    /// </summary>
    /// <param name="templateKey">Unique identifier for the template.</param>
    /// <param name="elapsedTicks">Execution duration in ticks before failure.</param>
    /// <param name="exception">The exception that occurred.</param>
    public static void RecordError(string templateKey, long elapsedTicks, Exception exception)
    {
        Interlocked.Increment(ref _globalExecutionCount);
        Interlocked.Increment(ref _globalErrorCount);
        Interlocked.Add(ref _globalTotalTicks, elapsedTicks);
        var entry = _metrics.GetOrAdd(templateKey, _ => new TemplateMetricEntry(templateKey));
        entry.RecordError(elapsedTicks, exception);
    }

    /// <summary>
    /// Gets metrics for a specific template.
    /// </summary>
    /// <param name="templateKey">The template identifier.</param>
    /// <returns>Snapshot of metrics, or null if template not found.</returns>
    public static TemplateMetricSnapshot? GetMetrics(string templateKey)
    {
        return _metrics.TryGetValue(templateKey, out var entry) ? entry.GetSnapshot() : null;
    }

    /// <summary>
    /// Gets metrics for all templates.
    /// </summary>
    /// <returns>Array of metric snapshots for all templates.</returns>
    public static TemplateMetricSnapshot[] GetAllMetrics()
    {
        return _metrics.Values.Select(e => e.GetSnapshot()).ToArray();
    }

    /// <summary>
    /// Gets global aggregated metrics across all templates.
    /// </summary>
    /// <returns>Global metric snapshot.</returns>
    public static GlobalMetricSnapshot GetGlobalMetrics()
    {
        var totalExecutions = Interlocked.Read(ref _globalExecutionCount);
        var totalErrors = Interlocked.Read(ref _globalErrorCount);
        var totalTicks = Interlocked.Read(ref _globalTotalTicks);
        var avgMs = totalExecutions > 0 ? (totalTicks * 1000.0 / Stopwatch.Frequency) / totalExecutions : 0;
        return new GlobalMetricSnapshot(
            TotalExecutions: totalExecutions,
            TotalErrors: totalErrors,
            SuccessRate: totalExecutions > 0 ? (totalExecutions - totalErrors) * 100.0 / totalExecutions : 100.0,
            AverageExecutionMs: avgMs,
            TemplateCount: _metrics.Count);
    }

    /// <summary>
    /// Resets all metrics. Use with caution in production.
    /// </summary>
    public static void Reset()
    {
        _metrics.Clear();
        Interlocked.Exchange(ref _globalExecutionCount, 0);
        Interlocked.Exchange(ref _globalErrorCount, 0);
        Interlocked.Exchange(ref _globalTotalTicks, 0);
    }

    /// <summary>
    /// Gets top N slowest templates.
    /// </summary>
    /// <param name="count">Number of templates to return.</param>
    /// <returns>Array of slowest templates ordered by average execution time.</returns>
    public static TemplateMetricSnapshot[] GetSlowestTemplates(int count = 10)
    {
        return _metrics.Values
            .Select(e => e.GetSnapshot())
            .OrderByDescending(s => s.AverageExecutionMs)
            .Take(count)
            .ToArray();
    }

    /// <summary>
    /// Gets top N most frequently executed templates.
    /// </summary>
    /// <param name="count">Number of templates to return.</param>
    /// <returns>Array of most executed templates ordered by execution count.</returns>
    public static TemplateMetricSnapshot[] GetMostExecutedTemplates(int count = 10)
    {
        return _metrics.Values
            .Select(e => e.GetSnapshot())
            .OrderByDescending(s => s.ExecutionCount)
            .Take(count)
            .ToArray();
    }

    /// <summary>
    /// Gets templates with highest error rates.
    /// </summary>
    /// <param name="count">Number of templates to return.</param>
    /// <returns>Array of templates with errors ordered by error count.</returns>
    public static TemplateMetricSnapshot[] GetMostErrorProneTemplates(int count = 10)
    {
        return _metrics.Values
            .Select(e => e.GetSnapshot())
            .Where(s => s.ErrorCount > 0)
            .OrderByDescending(s => s.ErrorCount)
            .Take(count)
            .ToArray();
    }
}

/// <summary>
/// Thread-safe metric entry for a single SQL template.
/// Uses lock-free operations for high performance.
/// </summary>
internal sealed class TemplateMetricEntry
{
    private readonly string _templateKey;
    private long _executionCount;
    private long _errorCount;
    private long _totalTicks;
    private long _minTicks = long.MaxValue;
    private long _maxTicks;
    private readonly ConcurrentDictionary<string, int> _errorTypes = new();
    private DateTime _firstExecutionUtc;
    private DateTime _lastExecutionUtc;

    public TemplateMetricEntry(string templateKey)
    {
        _templateKey = templateKey;
        _firstExecutionUtc = DateTime.UtcNow;
        _lastExecutionUtc = DateTime.UtcNow;
    }

    public void RecordSuccess(long elapsedTicks)
    {
        Interlocked.Increment(ref _executionCount);
        Interlocked.Add(ref _totalTicks, elapsedTicks);
        UpdateMinMax(elapsedTicks);
        _lastExecutionUtc = DateTime.UtcNow;
    }

    public void RecordError(long elapsedTicks, Exception exception)
    {
        Interlocked.Increment(ref _executionCount);
        Interlocked.Increment(ref _errorCount);
        Interlocked.Add(ref _totalTicks, elapsedTicks);
        UpdateMinMax(elapsedTicks);
        _errorTypes.AddOrUpdate(exception.GetType().Name, 1, (_, count) => count + 1);
        _lastExecutionUtc = DateTime.UtcNow;
    }

    private void UpdateMinMax(long ticks)
    {
        long currentMin;
        do { currentMin = Interlocked.Read(ref _minTicks); }
        while (ticks < currentMin && Interlocked.CompareExchange(ref _minTicks, ticks, currentMin) != currentMin);
        long currentMax;
        do { currentMax = Interlocked.Read(ref _maxTicks); }
        while (ticks > currentMax && Interlocked.CompareExchange(ref _maxTicks, ticks, currentMax) != currentMax);
    }

    public TemplateMetricSnapshot GetSnapshot()
    {
        var execCount = Interlocked.Read(ref _executionCount);
        var errCount = Interlocked.Read(ref _errorCount);
        var totalTicks = Interlocked.Read(ref _totalTicks);
        var minTicks = Interlocked.Read(ref _minTicks);
        var maxTicks = Interlocked.Read(ref _maxTicks);
        var avgMs = execCount > 0 ? (totalTicks * 1000.0 / Stopwatch.Frequency) / execCount : 0;
        var minMs = minTicks != long.MaxValue ? minTicks * 1000.0 / Stopwatch.Frequency : 0;
        var maxMs = maxTicks * 1000.0 / Stopwatch.Frequency;
        var successRate = execCount > 0 ? (execCount - errCount) * 100.0 / execCount : 100.0;
        return new TemplateMetricSnapshot(
            TemplateKey: _templateKey,
            ExecutionCount: execCount,
            ErrorCount: errCount,
            SuccessRate: successRate,
            AverageExecutionMs: avgMs,
            MinExecutionMs: minMs,
            MaxExecutionMs: maxMs,
            TotalExecutionMs: totalTicks * 1000.0 / Stopwatch.Frequency,
            ErrorTypes: new Dictionary<string, int>(_errorTypes),
            FirstExecutionUtc: _firstExecutionUtc,
            LastExecutionUtc: _lastExecutionUtc);
    }
}

/// <summary>
/// Immutable snapshot of metrics for a single SQL template.
/// </summary>
public readonly record struct TemplateMetricSnapshot(
    string TemplateKey,
    long ExecutionCount,
    long ErrorCount,
    double SuccessRate,
    double AverageExecutionMs,
    double MinExecutionMs,
    double MaxExecutionMs,
    double TotalExecutionMs,
    IReadOnlyDictionary<string, int> ErrorTypes,
    DateTime FirstExecutionUtc,
    DateTime LastExecutionUtc);

/// <summary>
/// Immutable snapshot of global aggregated metrics.
/// </summary>
public readonly record struct GlobalMetricSnapshot(
    long TotalExecutions,
    long TotalErrors,
    double SuccessRate,
    double AverageExecutionMs,
    int TemplateCount);
