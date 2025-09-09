// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Sqlx.Core;

namespace Sqlx.PerformanceBenchmarks;

/// <summary>
/// Performance benchmark runner for Sqlx.
/// </summary>
internal class Program
{
    private static async Task Main(string[] args)
    {
        Console.WriteLine("🚀 Sqlx Performance Benchmark Suite");
        Console.WriteLine("=====================================");
        Console.WriteLine($"Started at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine();

        try
        {
            // Run comprehensive benchmarks
            using var benchmarks = new SqlxBenchmarks();
            var report = await benchmarks.RunAllBenchmarksAsync();

            // Display detailed results
            DisplayBenchmarkReport(report);

            // Generate basic diagnostic info (simplified since ISqlxSyntaxReceiver is internal)
            var performanceStats = PerformanceMonitor.GetStatistics();
            var diagnosticReport = $"Performance Statistics:\nTotal operations: {performanceStats.TotalOperations}\nAverage execution time: {performanceStats.AverageExecutionTimeMs:F2}ms";

            Console.WriteLine("\n📋 Diagnostic Report:");
            Console.WriteLine(diagnosticReport);

            Console.WriteLine("\n✅ All benchmarks completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Benchmark failed: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            Environment.Exit(1);
        }

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }

    private static void DisplayBenchmarkReport(BenchmarkReport report)
    {
        Console.WriteLine("\n📊 Benchmark Summary:");
        Console.WriteLine("=====================");

        Console.WriteLine($"🏆 Fastest Operation: {report.FastestOperation.Name}");
        Console.WriteLine($"   Execution Time: {report.FastestOperation.ExecutionTimeMs:F3}ms");
        Console.WriteLine($"   Iterations: {report.FastestOperation.Iterations:N0}");

        Console.WriteLine($"\n🐌 Slowest Operation: {report.SlowestOperation.Name}");
        Console.WriteLine($"   Execution Time: {report.SlowestOperation.ExecutionTimeMs:F3}ms");
        Console.WriteLine($"   Iterations: {report.SlowestOperation.Iterations:N0}");

        Console.WriteLine($"\n💾 Most Memory Efficient: {report.MostMemoryEfficient.Name}");
        Console.WriteLine($"   Memory Allocated: {report.MostMemoryEfficient.MemoryAllocated:N0} bytes");

        Console.WriteLine($"\n📈 Overall Statistics:");
        Console.WriteLine($"   Average Execution Time: {report.AverageExecutionTime:F3}ms");
        Console.WriteLine($"   Total Memory Allocated: {report.TotalMemoryAllocated:N0} bytes");
        Console.WriteLine($"   Total Benchmarks: {report.Results.Count}");

        Console.WriteLine("\n📋 Detailed Results:");
        Console.WriteLine("Operation".PadRight(35) + "Avg Time (ms)".PadRight(15) + "Iterations".PadRight(12) + "Memory (bytes)");
        Console.WriteLine(new string('-', 80));

        foreach (var result in report.Results)
        {
            Console.WriteLine(
                result.Name.PadRight(35) +
                $"{result.ExecutionTimeMs:F3}".PadRight(15) +
                $"{result.Iterations:N0}".PadRight(12) +
                $"{result.MemoryAllocated:N0}"
            );
        }

        Console.WriteLine($"\n🧠 Memory Statistics:");
        Console.WriteLine($"   Total Memory: {report.MemoryStats.TotalMemoryBytes / 1024 / 1024:F1} MB");
        Console.WriteLine($"   After GC: {report.MemoryStats.TotalMemoryAfterGC / 1024 / 1024:F1} MB");
        Console.WriteLine($"   GC Collections: Gen0={report.MemoryStats.Gen0Collections}, " +
                         $"Gen1={report.MemoryStats.Gen1Collections}, Gen2={report.MemoryStats.Gen2Collections}");

        // Performance insights
        DisplayPerformanceInsights(report);
    }

    private static void DisplayPerformanceInsights(BenchmarkReport report)
    {
        Console.WriteLine("\n💡 Performance Insights:");
        Console.WriteLine("========================");

        // Find cache efficiency
        var cacheMiss = report.Results.FirstOrDefault(r => r.Name.Contains("Cache Miss"));
        var cacheHit = report.Results.FirstOrDefault(r => r.Name.Contains("Cache Hit"));

        if (cacheMiss != null && cacheHit != null)
        {
            var speedup = cacheMiss.ExecutionTimeMs / cacheHit.ExecutionTimeMs;
            Console.WriteLine($"🔥 Cache Speedup: {speedup:F1}x faster with cache hits");
        }

        // Memory efficiency analysis
        var mostEfficient = report.Results.OrderBy(r => r.MemoryAllocated).Take(3);
        Console.WriteLine("\n🏅 Top 3 Memory Efficient Operations:");
        foreach (var op in mostEfficient)
        {
            Console.WriteLine($"   • {op.Name}: {op.MemoryAllocated:N0} bytes");
        }

        // Performance recommendations
        Console.WriteLine("\n📋 Recommendations:");
        
        var highMemoryOps = report.Results.Where(r => r.MemoryAllocated > 10000).ToList();
        if (highMemoryOps.Any())
        {
            Console.WriteLine("   ⚠️  High memory allocation operations detected:");
            foreach (var op in highMemoryOps)
            {
                Console.WriteLine($"      • {op.Name}: {op.MemoryAllocated:N0} bytes");
            }
            Console.WriteLine("      💡 Consider using object pooling or reducing allocations");
        }

        var slowOps = report.Results.Where(r => r.ExecutionTimeMs > 10.0).ToList();
        if (slowOps.Any())
        {
            Console.WriteLine("   ⚠️  Slow operations detected:");
            foreach (var op in slowOps)
            {
                Console.WriteLine($"      • {op.Name}: {op.ExecutionTimeMs:F2}ms");
            }
            Console.WriteLine("      💡 Consider optimizing these operations or using async patterns");
        }

        if (!highMemoryOps.Any() && !slowOps.Any())
        {
            Console.WriteLine("   ✅ All operations are performing within optimal ranges!");
        }
    }
}

/// <summary>
/// Mock syntax receiver for diagnostic testing.
/// Note: ISqlxSyntaxReceiver is internal, so we'll use a simple mock approach
/// </summary>
internal class MockSyntaxReceiver
{
    public List<object> Methods { get; } = new();
    public List<object> RepositoryClasses { get; } = new();
}
