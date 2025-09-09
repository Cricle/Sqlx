// -----------------------------------------------------------------------
// <copyright file="PerformanceMonitor.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Threading;

namespace Sqlx.Core;

/// <summary>
/// Simplified performance monitoring for Sqlx operations.
/// </summary>
public static class PerformanceMonitor
{
    private static long _totalOperations = 0;

    /// <summary>
    /// Records a successful operation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RecordOperation()
    {
        Interlocked.Increment(ref _totalOperations);
    }

    /// <summary>
    /// Gets the total number of operations monitored.
    /// </summary>
    public static long TotalOperations => _totalOperations;

    /// <summary>
    /// Clears all performance monitoring data.
    /// </summary>
    public static void Clear()
    {
        Interlocked.Exchange(ref _totalOperations, 0);
    }
}