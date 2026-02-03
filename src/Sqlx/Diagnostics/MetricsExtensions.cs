// <copyright file="MetricsExtensions.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx.Diagnostics;

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

/// <summary>
/// Extension methods for SQL template metrics collection.
/// Provides convenient helpers for measuring execution time.
/// </summary>
public static class MetricsExtensions
{
    /// <summary>
    /// Executes an action and records its metrics.
    /// </summary>
    /// <param name="templateKey">Unique identifier for the template.</param>
    /// <param name="action">The action to execute.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ExecuteWithMetrics(string templateKey, Action action)
    {
        var sw = Stopwatch.GetTimestamp();
        try
        {
            action();
            SqlTemplateMetrics.RecordExecution(templateKey, Stopwatch.GetTimestamp() - sw);
        }
        catch (Exception ex)
        {
            SqlTemplateMetrics.RecordError(templateKey, Stopwatch.GetTimestamp() - sw, ex);
            throw;
        }
    }

    /// <summary>
    /// Executes a function and records its metrics.
    /// </summary>
    /// <typeparam name="T">The return type.</typeparam>
    /// <param name="templateKey">Unique identifier for the template.</param>
    /// <param name="func">The function to execute.</param>
    /// <returns>The result of the function.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T ExecuteWithMetrics<T>(string templateKey, Func<T> func)
    {
        var sw = Stopwatch.GetTimestamp();
        try
        {
            var result = func();
            SqlTemplateMetrics.RecordExecution(templateKey, Stopwatch.GetTimestamp() - sw);
            return result;
        }
        catch (Exception ex)
        {
            SqlTemplateMetrics.RecordError(templateKey, Stopwatch.GetTimestamp() - sw, ex);
            throw;
        }
    }

    /// <summary>
    /// Creates a metrics scope that automatically records execution time.
    /// </summary>
    /// <param name="templateKey">Unique identifier for the template.</param>
    /// <returns>A disposable scope that records metrics on disposal.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MetricsScope BeginMetricsScope(string templateKey)
    {
        return new MetricsScope(templateKey);
    }
}

/// <summary>
/// Disposable scope for automatic metrics recording.
/// Measures execution time from creation to disposal.
/// </summary>
public readonly struct MetricsScope : IDisposable
{
    private readonly string _templateKey;
    private readonly long _startTimestamp;
    private readonly Exception? _exception;

    internal MetricsScope(string templateKey)
    {
        _templateKey = templateKey;
        _startTimestamp = Stopwatch.GetTimestamp();
        _exception = null;
    }

    private MetricsScope(string templateKey, long startTimestamp, Exception exception)
    {
        _templateKey = templateKey;
        _startTimestamp = startTimestamp;
        _exception = exception;
    }

    /// <summary>
    /// Marks the scope as failed with an exception.
    /// </summary>
    /// <param name="exception">The exception that occurred.</param>
    /// <returns>A new scope with the exception recorded.</returns>
    public MetricsScope WithException(Exception exception)
    {
        return new MetricsScope(_templateKey, _startTimestamp, exception);
    }

    /// <summary>
    /// Records the metrics when the scope is disposed.
    /// </summary>
    public void Dispose()
    {
        var elapsed = Stopwatch.GetTimestamp() - _startTimestamp;
        if (_exception != null)
            SqlTemplateMetrics.RecordError(_templateKey, elapsed, _exception);
        else
            SqlTemplateMetrics.RecordExecution(_templateKey, elapsed);
    }
}
