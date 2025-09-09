// -----------------------------------------------------------------------
// <copyright file="PerformanceMonitor.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Sqlx.Core;

/// <summary>
/// High-performance monitoring and profiling utilities for Sqlx operations.
/// </summary>
public static class PerformanceMonitor
{
    private static readonly ConcurrentDictionary<string, MethodMetrics> _methodMetrics = new();
    internal static readonly ThreadLocal<Stopwatch> _stopwatch = new(() => new Stopwatch());
    private static long _totalOperations = 0;
    private static long _totalExecutionTime = 0;

    /// <summary>
    /// Starts performance monitoring for a method.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PerformanceScope StartMonitoring(string methodName)
    {
        return new PerformanceScope(methodName);
    }

    /// <summary>
    /// Records execution metrics for a method.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void RecordExecution(string methodName, long elapsedTicks, bool success)
    {
        Interlocked.Increment(ref _totalOperations);
        Interlocked.Add(ref _totalExecutionTime, elapsedTicks);

        _methodMetrics.AddOrUpdate(methodName, 
            new MethodMetrics(methodName, elapsedTicks, success),
            (key, existing) => existing.AddExecution(elapsedTicks, success));
    }

    /// <summary>
    /// Gets performance statistics for all monitored methods.
    /// </summary>
    public static PerformanceStatistics GetStatistics()
    {
        var methodStats = new Dictionary<string, MethodStatistics>();
        
        foreach (var kvp in _methodMetrics)
        {
            var metrics = kvp.Value;
            methodStats[kvp.Key] = new MethodStatistics(
                metrics.MethodName,
                metrics.TotalExecutions,
                metrics.SuccessfulExecutions,
                metrics.TotalExecutionTime,
                metrics.MinExecutionTime,
                metrics.MaxExecutionTime,
                metrics.TotalExecutions > 0 ? metrics.TotalExecutionTime / metrics.TotalExecutions : 0
            );
        }

        return new PerformanceStatistics(
            _totalOperations,
            _totalExecutionTime,
            methodStats
        );
    }

    /// <summary>
    /// Clears all performance monitoring data.
    /// </summary>
    public static void Reset()
    {
        _methodMetrics.Clear();
        Interlocked.Exchange(ref _totalOperations, 0);
        Interlocked.Exchange(ref _totalExecutionTime, 0);
    }

    /// <summary>
    /// Generates optimized monitoring code for injection into generated methods.
    /// </summary>
    internal static void GenerateMonitoringCode(IndentedStringBuilder sb, string methodName)
    {
        sb.AppendLine("// Performance monitoring");
        sb.AppendLine("using var __perfScope__ = global::Sqlx.Core.PerformanceMonitor.StartMonitoring(");
        sb.AppendLine($"    \"{methodName}\");");
    }
}

/// <summary>
/// Performance monitoring scope for automatic timing.
/// </summary>
public readonly struct PerformanceScope : IDisposable
{
    private readonly string _methodName;
    private readonly long _startTicks;
    private readonly Stopwatch _stopwatch;

    internal PerformanceScope(string methodName)
    {
        _methodName = methodName;
        _stopwatch = PerformanceMonitor._stopwatch.Value!;
        _stopwatch.Restart();
        _startTicks = Stopwatch.GetTimestamp();
    }

    /// <summary>
    /// Disposes the performance scope and records successful execution.
    /// </summary>
    public void Dispose()
    {
        _stopwatch.Stop();
        var elapsedTicks = Stopwatch.GetTimestamp() - _startTicks;
        PerformanceMonitor.RecordExecution(_methodName, elapsedTicks, true);
    }

    /// <summary>
    /// Records a failed execution.
    /// </summary>
    public void RecordFailure()
    {
        _stopwatch.Stop();
        var elapsedTicks = Stopwatch.GetTimestamp() - _startTicks;
        PerformanceMonitor.RecordExecution(_methodName, elapsedTicks, false);
    }
}

/// <summary>
/// Thread-safe method metrics collector.
/// </summary>
internal class MethodMetrics
{
    private long _totalExecutions;
    private long _successfulExecutions;
    private long _totalExecutionTime;
    private long _minExecutionTime = long.MaxValue;
    private long _maxExecutionTime;

    public MethodMetrics(string methodName, long executionTime, bool success)
    {
        MethodName = methodName;
        _totalExecutions = 1;
        _successfulExecutions = success ? 1 : 0;
        _totalExecutionTime = executionTime;
        _minExecutionTime = executionTime;
        _maxExecutionTime = executionTime;
    }

    public string MethodName { get; }
    public long TotalExecutions => _totalExecutions;
    public long SuccessfulExecutions => _successfulExecutions;
    public long TotalExecutionTime => _totalExecutionTime;
    public long MinExecutionTime => _minExecutionTime;
    public long MaxExecutionTime => _maxExecutionTime;

    public MethodMetrics AddExecution(long executionTime, bool success)
    {
        Interlocked.Increment(ref _totalExecutions);
        if (success) Interlocked.Increment(ref _successfulExecutions);
        Interlocked.Add(ref _totalExecutionTime, executionTime);

        // Update min/max with thread-safe operations
        UpdateMinimum(ref _minExecutionTime, executionTime);
        UpdateMaximum(ref _maxExecutionTime, executionTime);

        return this;
    }

    private static void UpdateMinimum(ref long location, long value)
    {
        long current;
        do
        {
            current = location;
            if (value >= current) return;
        } while (Interlocked.CompareExchange(ref location, value, current) != current);
    }

    private static void UpdateMaximum(ref long location, long value)
    {
        long current;
        do
        {
            current = location;
            if (value <= current) return;
        } while (Interlocked.CompareExchange(ref location, value, current) != current);
    }
}

/// <summary>
/// Performance statistics for a specific method.
/// </summary>
public readonly record struct MethodStatistics(
    string MethodName,
    long TotalExecutions,
    long SuccessfulExecutions,
    long TotalExecutionTime,
    long MinExecutionTime,
    long MaxExecutionTime,
    long AverageExecutionTime
)
{
    /// <summary>Gets the success rate as a percentage.</summary>
    public double SuccessRate => TotalExecutions > 0 ? (double)SuccessfulExecutions / TotalExecutions : 0.0;
    
    /// <summary>Gets the average execution time in milliseconds.</summary>
    public double AverageExecutionTimeMs => AverageExecutionTime * 1000.0 / Stopwatch.Frequency;
    
    /// <summary>Gets the minimum execution time in milliseconds.</summary>
    public double MinExecutionTimeMs => MinExecutionTime * 1000.0 / Stopwatch.Frequency;
    
    /// <summary>Gets the maximum execution time in milliseconds.</summary>
    public double MaxExecutionTimeMs => MaxExecutionTime * 1000.0 / Stopwatch.Frequency;
}

/// <summary>
/// Overall performance statistics.
/// </summary>
public readonly record struct PerformanceStatistics(
    long TotalOperations,
    long TotalExecutionTime,
    IReadOnlyDictionary<string, MethodStatistics> MethodStatistics
)
{
    /// <summary>Gets the average execution time in milliseconds across all operations.</summary>
    public double AverageExecutionTimeMs => TotalOperations > 0 
        ? (TotalExecutionTime / TotalOperations) * 1000.0 / Stopwatch.Frequency 
        : 0.0;

    /// <summary>Gets the total execution time in milliseconds.</summary>
    public double TotalExecutionTimeMs => TotalExecutionTime * 1000.0 / Stopwatch.Frequency;
}

/// <summary>
/// Memory usage monitoring utilities.
/// </summary>
public static class MemoryMonitor
{
    /// <summary>
    /// Gets current memory usage statistics.
    /// </summary>
    public static MemoryStatistics GetMemoryStatistics()
    {
        GC.Collect(); // Force collection for accurate measurement
        GC.WaitForPendingFinalizers();
        GC.Collect();

        return new MemoryStatistics(
            GC.GetTotalMemory(false),
            GC.GetTotalMemory(true),
            GC.CollectionCount(0),
            GC.CollectionCount(1),
            GC.CollectionCount(2)
        );
    }

    /// <summary>
    /// Measures memory allocation for a specific operation.
    /// </summary>
    public static MemoryMeasurement MeasureAllocation(Action operation)
    {
        var beforeMemory = GC.GetTotalMemory(true);
        var beforeGen0 = GC.CollectionCount(0);
        var beforeGen1 = GC.CollectionCount(1);
        var beforeGen2 = GC.CollectionCount(2);

        operation();

        var afterMemory = GC.GetTotalMemory(false);
        var afterGen0 = GC.CollectionCount(0);
        var afterGen1 = GC.CollectionCount(1);
        var afterGen2 = GC.CollectionCount(2);

        return new MemoryMeasurement(
            afterMemory - beforeMemory,
            afterGen0 - beforeGen0,
            afterGen1 - beforeGen1,
            afterGen2 - beforeGen2
        );
    }
}

/// <summary>
/// Memory usage statistics.
/// </summary>
public readonly record struct MemoryStatistics(
    long TotalMemoryBytes,
    long TotalMemoryAfterGC,
    int Gen0Collections,
    int Gen1Collections,
    int Gen2Collections
);

/// <summary>
/// Memory allocation measurement for a specific operation.
/// </summary>
public readonly record struct MemoryMeasurement(
    long AllocatedBytes,
    int Gen0Collections,
    int Gen1Collections,
    int Gen2Collections
);
