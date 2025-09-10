// -----------------------------------------------------------------------
// <copyright file="PerformanceTestBase.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Sqlx.Tests;

/// <summary>
/// Base class for performance tests, providing common utilities and patterns.
/// </summary>
public abstract class PerformanceTestBase
{
    /// <summary>
    /// Measures the execution time of an operation.
    /// </summary>
    /// <param name="operation">The operation to measure</param>
    /// <param name="iterations">Number of iterations to run</param>
    /// <returns>Elapsed milliseconds</returns>
    protected static long MeasureOperation(Action operation, int iterations = 1)
    {
        var stopwatch = Stopwatch.StartNew();

        for (int i = 0; i < iterations; i++)
        {
            operation();
        }

        stopwatch.Stop();
        return stopwatch.ElapsedMilliseconds;
    }

    /// <summary>
    /// Measures the execution time of an async operation.
    /// </summary>
    /// <param name="operation">The async operation to measure</param>
    /// <param name="iterations">Number of iterations to run</param>
    /// <returns>Elapsed milliseconds</returns>
    protected static async Task<long> MeasureOperationAsync(Func<Task> operation, int iterations = 1)
    {
        var stopwatch = Stopwatch.StartNew();

        for (int i = 0; i < iterations; i++)
        {
            await operation();
        }

        stopwatch.Stop();
        return stopwatch.ElapsedMilliseconds;
    }

    /// <summary>
    /// Runs concurrent operations and measures their execution time.
    /// </summary>
    /// <param name="operation">The operation to run concurrently</param>
    /// <param name="concurrentTasks">Number of concurrent tasks</param>
    /// <param name="operationsPerTask">Number of operations per task</param>
    /// <returns>Total elapsed milliseconds</returns>
    protected static long MeasureConcurrentOperations(Action operation, int concurrentTasks, int operationsPerTask)
    {
        var stopwatch = Stopwatch.StartNew();

        var tasks = Enumerable.Range(0, concurrentTasks).Select(_ => Task.Run(() =>
        {
            for (int i = 0; i < operationsPerTask; i++)
            {
                operation();
            }
        })).ToArray();

        Task.WaitAll(tasks);
        stopwatch.Stop();

        return stopwatch.ElapsedMilliseconds;
    }

    /// <summary>
    /// Runs concurrent operations and collects results.
    /// </summary>
    /// <typeparam name="T">Result type</typeparam>
    /// <param name="operation">The operation to run concurrently</param>
    /// <param name="concurrentTasks">Number of concurrent tasks</param>
    /// <param name="operationsPerTask">Number of operations per task</param>
    /// <returns>Collection of results and elapsed time</returns>
    protected static (ConcurrentBag<T> results, long elapsedMs) RunConcurrentOperations<T>(
        Func<T> operation, int concurrentTasks, int operationsPerTask)
    {
        var results = new ConcurrentBag<T>();
        var stopwatch = Stopwatch.StartNew();

        var tasks = Enumerable.Range(0, concurrentTasks).Select(_ => Task.Run(() =>
        {
            for (int i = 0; i < operationsPerTask; i++)
            {
                try
                {
                    var result = operation();
                    results.Add(result);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Operation failed: {ex.Message}");
                }
            }
        })).ToArray();

        Task.WaitAll(tasks);
        stopwatch.Stop();

        return (results, stopwatch.ElapsedMilliseconds);
    }

    /// <summary>
    /// Measures memory usage before and after an operation.
    /// </summary>
    /// <param name="operation">The operation to measure</param>
    /// <returns>Memory increase in bytes</returns>
    protected static long MeasureMemoryUsage(Action operation)
    {
        // Force garbage collection to get accurate baseline
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var initialMemory = GC.GetTotalMemory(false);

        operation();

        var finalMemory = GC.GetTotalMemory(false);
        return finalMemory - initialMemory;
    }

    /// <summary>
    /// Asserts that an operation completes within the specified time limit.
    /// </summary>
    /// <param name="operation">The operation to test</param>
    /// <param name="maxTimeMs">Maximum allowed time in milliseconds</param>
    /// <param name="operationName">Name of the operation for error messages</param>
    protected static void AssertPerformance(Action operation, double maxTimeMs, string operationName = "Operation")
    {
        var elapsedMs = MeasureOperation(operation);
        Assert.IsTrue(elapsedMs <= maxTimeMs,
            $"{operationName} should complete within {maxTimeMs}ms, but took {elapsedMs}ms");
    }

    /// <summary>
    /// Asserts that an async operation completes within the specified time limit.
    /// </summary>
    /// <param name="operation">The async operation to test</param>
    /// <param name="maxTimeMs">Maximum allowed time in milliseconds</param>
    /// <param name="operationName">Name of the operation for error messages</param>
    protected static async Task AssertPerformanceAsync(Func<Task> operation, double maxTimeMs, string operationName = "Operation")
    {
        var elapsedMs = await MeasureOperationAsync(operation);
        Assert.IsTrue(elapsedMs <= maxTimeMs,
            $"{operationName} should complete within {maxTimeMs}ms, but took {elapsedMs}ms");
    }

    /// <summary>
    /// Asserts that memory usage is within acceptable limits.
    /// </summary>
    /// <param name="operation">The operation to test</param>
    /// <param name="maxMemoryMB">Maximum allowed memory increase in MB</param>
    /// <param name="operationName">Name of the operation for error messages</param>
    protected static void AssertMemoryUsage(Action operation, double maxMemoryMB, string operationName = "Operation")
    {
        var memoryIncrease = MeasureMemoryUsage(operation);
        var memoryMB = memoryIncrease / 1024.0 / 1024.0;

        Assert.IsTrue(memoryMB <= maxMemoryMB,
            $"{operationName} should use less than {maxMemoryMB}MB, but used {memoryMB:F2}MB");
    }

    /// <summary>
    /// Logs performance results to the console.
    /// </summary>
    /// <param name="testName">Name of the test</param>
    /// <param name="elapsedMs">Elapsed time in milliseconds</param>
    /// <param name="iterations">Number of iterations</param>
    protected static void LogPerformanceResults(string testName, long elapsedMs, int iterations = 1)
    {
        var avgTime = iterations > 1 ? (double)elapsedMs / iterations : elapsedMs;
        Console.WriteLine($"🚀 {testName}:");
        Console.WriteLine($"   Total time: {elapsedMs}ms");
        if (iterations > 1)
        {
            Console.WriteLine($"   Iterations: {iterations}");
            Console.WriteLine($"   Average time: {avgTime:F2}ms per operation");
        }
    }
}
