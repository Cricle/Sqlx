// -----------------------------------------------------------------------
// <copyright file="MetricsCollector.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using SqlxPerformanceAnalyzer.Models;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace SqlxPerformanceAnalyzer.Core;

/// <summary>
/// Collects system and application performance metrics.
/// </summary>
public class MetricsCollector
{
    private readonly ILogger<MetricsCollector> _logger;

    public MetricsCollector(ILogger<MetricsCollector> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Starts continuous metrics collection.
    /// </summary>
    public async Task StartCollectionAsync(int intervalMs, CancellationToken cancellationToken)
    {
        _logger.LogInformation("📊 Starting metrics collection every {Interval}ms", intervalMs);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var metrics = await CollectSystemMetricsAsync();
                LogMetrics(metrics);
                
                await Task.Delay(intervalMs, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error collecting metrics");
                await Task.Delay(1000, cancellationToken); // Brief delay before retry
            }
        }

        _logger.LogInformation("⏹️ Metrics collection stopped");
    }

    /// <summary>
    /// Collects current system metrics.
    /// </summary>
    public async Task<SystemMetrics> CollectSystemMetricsAsync()
    {
        var metrics = new SystemMetrics
        {
            Timestamp = DateTime.UtcNow
        };

        try
        {
            // Collect process metrics
            var process = Process.GetCurrentProcess();
            
            metrics.MemoryUsedBytes = process.WorkingSet64;
            metrics.MemoryAvailableBytes = GetAvailableMemory();
            metrics.MemoryUsagePercent = CalculateMemoryUsagePercent(metrics.MemoryUsedBytes, metrics.MemoryAvailableBytes);

            // Simulate CPU usage (in real implementation, use PerformanceCounter)
            metrics.CpuUsagePercent = await SimulateCpuUsageAsync();

            // Simulate other metrics
            var random = new Random();
            metrics.DiskIoReadMBps = random.NextDouble() * 50;
            metrics.DiskIoWriteMBps = random.NextDouble() * 25;
            metrics.NetworkIoMBps = random.NextDouble() * 100;
            metrics.ActiveConnections = random.Next(5, 50);
            metrics.TotalConnections = metrics.ActiveConnections + random.Next(0, 10);
            metrics.QueriesPerSecond = random.NextDouble() * 1000;
            metrics.TransactionsPerSecond = random.NextDouble() * 500;
            metrics.CacheHits = random.Next(1000, 10000);
            metrics.CacheMisses = random.Next(10, 1000);

            // Generate alerts based on thresholds
            CheckForSystemAlerts(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting system metrics");
        }

        return metrics;
    }

    private long GetAvailableMemory()
    {
        try
        {
            // Simplified approach - in real implementation, use Windows API or performance counters
            var totalMemory = GC.GetTotalMemory(false);
            return Math.Max(1024 * 1024 * 1024, totalMemory * 4); // Simulate available memory
        }
        catch
        {
            return 1024 * 1024 * 1024; // 1GB default
        }
    }

    private double CalculateMemoryUsagePercent(long usedBytes, long availableBytes)
    {
        if (availableBytes <= 0) return 0;
        return Math.Min(100, (double)usedBytes / availableBytes * 100);
    }

    private Task<double> SimulateCpuUsageAsync()
    {
        // Simulate CPU usage measurement
        var random = new Random();
        var baseCpuUsage = random.NextDouble() * 40 + 5; // 5-45% base usage
        
        // Add some spikes occasionally
        if (random.NextDouble() > 0.9)
        {
            baseCpuUsage += random.NextDouble() * 40; // Spike up to 85%
        }

        return Task.FromResult(Math.Min(100, baseCpuUsage));
    }

    private void CheckForSystemAlerts(SystemMetrics metrics)
    {
        // Check for high CPU usage
        if (metrics.CpuUsagePercent > 80)
        {
            metrics.Alerts.Add($"High CPU usage: {metrics.CpuUsagePercent:F1}%");
        }

        // Check for high memory usage
        if (metrics.MemoryUsagePercent > 85)
        {
            metrics.Alerts.Add($"High memory usage: {metrics.MemoryUsagePercent:F1}%");
        }

        // Check for low cache hit ratio
        if (metrics.CacheHitRatio < 80)
        {
            metrics.Alerts.Add($"Low cache hit ratio: {metrics.CacheHitRatio:F1}%");
        }

        // Check for high disk I/O
        if (metrics.DiskIoReadMBps + metrics.DiskIoWriteMBps > 100)
        {
            metrics.Alerts.Add($"High disk I/O: {metrics.DiskIoReadMBps + metrics.DiskIoWriteMBps:F1} MB/s");
        }
    }

    private void LogMetrics(SystemMetrics metrics)
    {
        _logger.LogDebug("📊 Metrics - CPU: {Cpu:F1}%, Memory: {Memory:F1}%, Cache Hit: {Cache:F1}%, Alerts: {Alerts}",
            metrics.CpuUsagePercent,
            metrics.MemoryUsagePercent,
            metrics.CacheHitRatio,
            metrics.Alerts.Count);

        foreach (var alert in metrics.Alerts)
        {
            _logger.LogWarning("⚠️ System Alert: {Alert}", alert);
        }
    }
}
