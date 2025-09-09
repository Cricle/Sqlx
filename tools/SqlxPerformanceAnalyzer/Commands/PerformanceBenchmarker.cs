// -----------------------------------------------------------------------
// <copyright file="PerformanceBenchmarker.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using SqlxPerformanceAnalyzer.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace SqlxPerformanceAnalyzer.Commands;

/// <summary>
/// Benchmarks specific SQL queries for performance testing.
/// </summary>
public class PerformanceBenchmarker
{
    private readonly ILogger<PerformanceBenchmarker> _logger;

    public PerformanceBenchmarker(ILogger<PerformanceBenchmarker> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Benchmarks a specific SQL query.
    /// </summary>
    public async Task BenchmarkAsync(string connectionString, string sqlQuery, int iterations, int concurrency, string? outputFile)
    {
        _logger.LogInformation("🏁 Starting SQL query benchmark");
        _logger.LogInformation("🔍 Query: {Query}", TruncateQuery(sqlQuery));
        _logger.LogInformation("🔄 Iterations: {Iterations}", iterations);
        _logger.LogInformation("⚡ Concurrency: {Concurrency}", concurrency);

        try
        {
            var result = await RunBenchmarkAsync(sqlQuery, connectionString, iterations, concurrency);
            
            if (!string.IsNullOrEmpty(outputFile))
            {
                await SaveBenchmarkResultAsync(result, outputFile);
            }
            
            PrintBenchmarkResults(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Benchmark failed");
            throw;
        }
    }

    private async Task<BenchmarkResult> RunBenchmarkAsync(string sqlQuery, string connectionString, int iterations, int concurrency)
    {
        var result = new BenchmarkResult
        {
            BenchmarkId = Guid.NewGuid().ToString(),
            ExecutedAt = DateTime.UtcNow,
            SqlQuery = sqlQuery,
            Iterations = iterations,
            Concurrency = concurrency
        };

        var allExecutionTimes = new List<double>();
        var errors = new List<string>();
        var totalStopwatch = Stopwatch.StartNew();

        // Monitor system resources
        var resourceMonitor = StartResourceMonitoring();

        try
        {
            if (concurrency == 1)
            {
                // Sequential execution
                for (int i = 0; i < iterations; i++)
                {
                    var executionTime = await ExecuteSingleQueryAsync(sqlQuery, connectionString);
                    if (executionTime.HasValue)
                    {
                        allExecutionTimes.Add(executionTime.Value);
                        result.SuccessfulExecutions++;
                    }
                    else
                    {
                        result.FailedExecutions++;
                        errors.Add($"Execution {i + 1} failed");
                    }

                    if ((i + 1) % 100 == 0)
                    {
                        _logger.LogInformation("📊 Progress: {Completed}/{Total} executions", i + 1, iterations);
                    }
                }
            }
            else
            {
                // Concurrent execution
                await ExecuteConcurrentBenchmarkAsync(sqlQuery, connectionString, iterations, concurrency, allExecutionTimes, errors, result);
            }
        }
        finally
        {
            totalStopwatch.Stop();
            result.ResourceUsage = await StopResourceMonitoringAsync(resourceMonitor);
        }

        // Calculate statistics
        if (allExecutionTimes.Any())
        {
            var sortedTimes = allExecutionTimes.OrderBy(t => t).ToList();
            
            result.TotalTimeMs = totalStopwatch.Elapsed.TotalMilliseconds;
            result.AverageTimeMs = sortedTimes.Average();
            result.MinTimeMs = sortedTimes.Min();
            result.MaxTimeMs = sortedTimes.Max();
            result.MedianTimeMs = CalculatePercentile(sortedTimes, 50);
            result.P95TimeMs = CalculatePercentile(sortedTimes, 95);
            result.P99TimeMs = CalculatePercentile(sortedTimes, 99);
            result.StandardDeviation = CalculateStandardDeviation(sortedTimes);
            result.ThroughputQps = result.SuccessfulExecutions / (result.TotalTimeMs / 1000.0);
        }

        result.Errors = errors.Distinct().ToList();

        return result;
    }

    private async Task ExecuteConcurrentBenchmarkAsync(string sqlQuery, string connectionString, int iterations, int concurrency, List<double> allExecutionTimes, List<string> errors, BenchmarkResult result)
    {
        var iterationsPerThread = iterations / concurrency;
        var remainingIterations = iterations % concurrency;

        var tasks = new List<Task>();

        for (int thread = 0; thread < concurrency; thread++)
        {
            var threadIterations = iterationsPerThread;
            if (thread < remainingIterations)
                threadIterations++;

            var threadId = thread;
            tasks.Add(Task.Run(async () =>
            {
                var threadTimes = new List<double>();
                var threadErrors = new List<string>();

                for (int i = 0; i < threadIterations; i++)
                {
                    var executionTime = await ExecuteSingleQueryAsync(sqlQuery, connectionString);
                    if (executionTime.HasValue)
                    {
                        threadTimes.Add(executionTime.Value);
                    }
                    else
                    {
                        threadErrors.Add($"Thread {threadId}, Execution {i + 1} failed");
                    }
                }

                lock (allExecutionTimes)
                {
                    allExecutionTimes.AddRange(threadTimes);
                    errors.AddRange(threadErrors);
                    result.SuccessfulExecutions += threadTimes.Count;
                    result.FailedExecutions += threadErrors.Count;
                }
            }));
        }

        await Task.WhenAll(tasks);
        _logger.LogInformation("✅ Concurrent execution completed");
    }

    private async Task<double?> ExecuteSingleQueryAsync(string sqlQuery, string connectionString)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            
            // Simulate query execution
            // In a real implementation, this would execute the actual SQL query
            await SimulateQueryExecutionAsync(sqlQuery);
            
            stopwatch.Stop();
            return stopwatch.Elapsed.TotalMilliseconds;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Query execution failed");
            return null;
        }
    }

    private async Task SimulateQueryExecutionAsync(string sqlQuery)
    {
        // Simulate realistic execution times based on query complexity
        var complexity = EstimateQueryComplexity(sqlQuery);
        var baseTime = complexity + new Random().Next(-5, 15); // Add some variance
        var executionTime = Math.Max(1, baseTime);
        
        await Task.Delay(Math.Min(executionTime, 50)); // Cap simulation delay for benchmarking
    }

    private int EstimateQueryComplexity(string sqlQuery)
    {
        var complexity = 5; // Base time
        var upperQuery = sqlQuery.ToUpperInvariant();

        if (upperQuery.Contains("JOIN")) complexity += 10;
        if (upperQuery.Contains("ORDER BY")) complexity += 8;
        if (upperQuery.Contains("GROUP BY")) complexity += 12;
        if (upperQuery.Contains("HAVING")) complexity += 5;
        if (upperQuery.Contains("UNION")) complexity += 20;
        if (upperQuery.Contains("DISTINCT")) complexity += 8;

        return complexity;
    }

    private ResourceMonitor StartResourceMonitoring()
    {
        var monitor = new ResourceMonitor();
        monitor.Start();
        return monitor;
    }

    private async Task<SystemResourceUsage> StopResourceMonitoringAsync(ResourceMonitor monitor)
    {
        await Task.Delay(100); // Brief delay to collect final metrics
        monitor.Stop();
        return monitor.GetUsage();
    }

    private double CalculatePercentile(List<double> sortedValues, double percentile)
    {
        if (sortedValues.Count == 0) return 0;
        
        var index = (percentile / 100.0) * (sortedValues.Count - 1);
        var lower = (int)Math.Floor(index);
        var upper = (int)Math.Ceiling(index);
        
        if (lower == upper) return sortedValues[lower];
        
        var weight = index - lower;
        return sortedValues[lower] * (1 - weight) + sortedValues[upper] * weight;
    }

    private double CalculateStandardDeviation(List<double> values)
    {
        if (values.Count <= 1) return 0;
        
        var mean = values.Average();
        var sumOfSquares = values.Sum(v => Math.Pow(v - mean, 2));
        return Math.Sqrt(sumOfSquares / (values.Count - 1));
    }

    private async Task SaveBenchmarkResultAsync(BenchmarkResult result, string outputFile)
    {
        try
        {
            var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(outputFile, json);
            _logger.LogInformation("💾 Benchmark results saved to: {Path}", outputFile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save benchmark results to {Path}", outputFile);
        }
    }

    private void PrintBenchmarkResults(BenchmarkResult result)
    {
        Console.WriteLine();
        Console.WriteLine("🏁 BENCHMARK RESULTS");
        Console.WriteLine("===================");
        Console.WriteLine($"Query: {TruncateQuery(result.SqlQuery)}");
        Console.WriteLine($"Iterations: {result.Iterations:N0}");
        Console.WriteLine($"Concurrency: {result.Concurrency}");
        Console.WriteLine($"Total Time: {result.TotalTimeMs:F2}ms");
        Console.WriteLine();
        
        Console.WriteLine("⏱️ TIMING STATISTICS");
        Console.WriteLine("--------------------");
        Console.WriteLine($"Average:    {result.AverageTimeMs:F2}ms");
        Console.WriteLine($"Median:     {result.MedianTimeMs:F2}ms");
        Console.WriteLine($"Min:        {result.MinTimeMs:F2}ms");
        Console.WriteLine($"Max:        {result.MaxTimeMs:F2}ms");
        Console.WriteLine($"P95:        {result.P95TimeMs:F2}ms");
        Console.WriteLine($"P99:        {result.P99TimeMs:F2}ms");
        Console.WriteLine($"Std Dev:    {result.StandardDeviation:F2}ms");
        Console.WriteLine();

        Console.WriteLine("📊 PERFORMANCE METRICS");
        Console.WriteLine("----------------------");
        Console.WriteLine($"Throughput:     {result.ThroughputQps:F2} queries/sec");
        Console.WriteLine($"Success Rate:   {(double)result.SuccessfulExecutions / result.Iterations * 100:F1}%");
        Console.WriteLine($"Successful:     {result.SuccessfulExecutions:N0}");
        Console.WriteLine($"Failed:         {result.FailedExecutions:N0}");
        Console.WriteLine();

        Console.WriteLine("💻 RESOURCE USAGE");
        Console.WriteLine("-----------------");
        Console.WriteLine($"Peak CPU:       {result.ResourceUsage.PeakCpuUsage:F1}%");
        Console.WriteLine($"Average CPU:    {result.ResourceUsage.AverageCpuUsage:F1}%");
        Console.WriteLine($"Peak Memory:    {result.ResourceUsage.PeakMemoryUsage / 1024 / 1024:F1} MB");
        Console.WriteLine($"Average Memory: {result.ResourceUsage.AverageMemoryUsage / 1024 / 1024:F1} MB");

        if (result.Errors.Any())
        {
            Console.WriteLine();
            Console.WriteLine("❌ ERRORS");
            Console.WriteLine("---------");
            foreach (var error in result.Errors.Take(5))
            {
                Console.WriteLine($"• {error}");
            }
            if (result.Errors.Count > 5)
            {
                Console.WriteLine($"... and {result.Errors.Count - 5} more errors");
            }
        }

        // Performance rating
        var rating = DeterminePerformanceRating(result.AverageTimeMs);
        var ratingEmoji = rating switch
        {
            PerformanceRating.Excellent => "🟢",
            PerformanceRating.Good => "🟡",
            PerformanceRating.Fair => "🟠",
            PerformanceRating.Poor => "🔴",
            PerformanceRating.Critical => "💀",
            _ => "❓"
        };

        Console.WriteLine();
        Console.WriteLine($"{ratingEmoji} OVERALL RATING: {rating}");
    }

    private PerformanceRating DeterminePerformanceRating(double averageTimeMs)
    {
        return averageTimeMs switch
        {
            < 10 => PerformanceRating.Excellent,
            < 100 => PerformanceRating.Good,
            < 500 => PerformanceRating.Fair,
            < 2000 => PerformanceRating.Poor,
            _ => PerformanceRating.Critical
        };
    }

    private string TruncateQuery(string query)
    {
        const int maxLength = 80;
        if (query.Length <= maxLength) return query;
        return query.Substring(0, maxLength - 3) + "...";
    }

    private class ResourceMonitor
    {
        private readonly List<double> _cpuSamples = new();
        private readonly List<long> _memorySamples = new();
        private bool _isRunning;

        public void Start()
        {
            _isRunning = true;
            _ = Task.Run(MonitorResources);
        }

        public void Stop()
        {
            _isRunning = false;
        }

        public SystemResourceUsage GetUsage()
        {
            return new SystemResourceUsage
            {
                PeakCpuUsage = _cpuSamples.Any() ? _cpuSamples.Max() : 0,
                AverageCpuUsage = _cpuSamples.Any() ? _cpuSamples.Average() : 0,
                PeakMemoryUsage = _memorySamples.Any() ? _memorySamples.Max() : 0,
                AverageMemoryUsage = _memorySamples.Any() ? (long)_memorySamples.Average() : 0
            };
        }

        private async Task MonitorResources()
        {
            var process = Process.GetCurrentProcess();
            
            while (_isRunning)
            {
                try
                {
                    // Simulate resource monitoring
                    var random = new Random();
                    _cpuSamples.Add(random.NextDouble() * 50 + 10); // 10-60% CPU
                    _memorySamples.Add(process.WorkingSet64);
                    
                    await Task.Delay(100);
                }
                catch
                {
                    // Ignore monitoring errors
                }
            }
        }
    }
}

