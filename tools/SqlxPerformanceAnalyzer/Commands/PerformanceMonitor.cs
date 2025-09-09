// -----------------------------------------------------------------------
// <copyright file="PerformanceMonitor.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using SqlxPerformanceAnalyzer.Core;
using SqlxPerformanceAnalyzer.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SqlxPerformanceAnalyzer.Commands;

/// <summary>
/// Continuous monitoring of SQL performance with alerting.
/// </summary>
public class PerformanceMonitor
{
    private readonly ILogger<PerformanceMonitor> _logger;
    private readonly MetricsCollector _metricsCollector;
    private readonly List<PerformanceAlert> _alerts = new();

    public PerformanceMonitor(ILogger<PerformanceMonitor> logger, MetricsCollector metricsCollector)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _metricsCollector = metricsCollector ?? throw new ArgumentNullException(nameof(metricsCollector));
    }

    /// <summary>
    /// Starts continuous monitoring with alerting.
    /// </summary>
    public async Task MonitorAsync(string connectionString, int intervalSeconds, double alertThresholdMs, string? outputDirectory)
    {
        _logger.LogInformation("📊 Starting continuous performance monitoring");
        _logger.LogInformation("⏱️ Monitoring interval: {Interval} seconds", intervalSeconds);
        _logger.LogInformation("🚨 Alert threshold: {Threshold}ms", alertThresholdMs);

        var cancellationTokenSource = new CancellationTokenSource();
        var monitoringStart = DateTime.UtcNow;

        // Handle Ctrl+C gracefully
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            cancellationTokenSource.Cancel();
            _logger.LogInformation("⏹️ Stopping monitoring...");
        };

        try
        {
            await MonitoringLoopAsync(connectionString, intervalSeconds, alertThresholdMs, outputDirectory, cancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("✅ Monitoring stopped gracefully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Monitoring failed");
            throw;
        }
        finally
        {
            var monitoringDuration = DateTime.UtcNow - monitoringStart;
            _logger.LogInformation("⏱️ Total monitoring time: {Duration}", monitoringDuration);
            await SaveMonitoringReportAsync(outputDirectory, monitoringStart, monitoringDuration);
        }
    }

    private async Task MonitoringLoopAsync(string connectionString, int intervalSeconds, double alertThresholdMs, string? outputDirectory, CancellationToken cancellationToken)
    {
        var intervalMs = intervalSeconds * 1000;
        var alertCounter = 0;

        _logger.LogInformation("🔄 Monitoring started. Press Ctrl+C to stop.");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var timestamp = DateTime.UtcNow;
                
                // Collect system metrics
                var systemMetrics = await _metricsCollector.CollectSystemMetricsAsync();
                
                // Simulate query performance monitoring
                var queryMetrics = await SimulateQueryMonitoringAsync(connectionString, alertThresholdMs);
                
                // Check for alerts
                var newAlerts = CheckForAlerts(queryMetrics, systemMetrics, alertThresholdMs);
                _alerts.AddRange(newAlerts);

                // Log status
                LogMonitoringStatus(systemMetrics, queryMetrics, newAlerts.Count);

                // Save periodic data
                if (outputDirectory != null)
                {
                    await SavePeriodicDataAsync(outputDirectory, timestamp, systemMetrics, queryMetrics);
                }

                foreach (var alert in newAlerts)
                {
                    alertCounter++;
                    LogAlert(alert, alertCounter);
                }

                await Task.Delay(intervalMs, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during monitoring cycle");
                await Task.Delay(1000, cancellationToken); // Brief delay before retry
            }
        }
    }

    private Task<List<QueryPerformanceMetrics>> SimulateQueryMonitoringAsync(string connectionString, double alertThresholdMs)
    {
        // In a real implementation, this would monitor actual query performance
        // For now, simulate some query metrics
        
        var random = new Random();
        var metrics = new List<QueryPerformanceMetrics>();

        // Simulate 3-5 active queries
        var queryCount = random.Next(3, 6);
        
        for (int i = 0; i < queryCount; i++)
        {
            var avgTime = random.NextDouble() * 2000; // 0-2000ms
            var isProblematic = avgTime > alertThresholdMs || random.NextDouble() > 0.9;

            var metric = new QueryPerformanceMetrics
            {
                QueryPattern = $"Query_{i + 1}",
                SqlHash = Guid.NewGuid().ToString()[..8],
                ExecutionCount = random.Next(10, 100),
                AverageExecutionTimeMs = avgTime,
                MinExecutionTimeMs = avgTime * 0.5,
                MaxExecutionTimeMs = avgTime * 2.5,
                ErrorCount = isProblematic ? random.Next(1, 5) : 0,
                Rating = DetermineRating(avgTime),
                FirstSeen = DateTime.UtcNow.AddMinutes(-random.Next(5, 30)),
                LastSeen = DateTime.UtcNow
            };

            metrics.Add(metric);
        }

        return Task.FromResult(metrics);
    }

    private List<PerformanceAlert> CheckForAlerts(List<QueryPerformanceMetrics> queryMetrics, SystemMetrics systemMetrics, double alertThresholdMs)
    {
        var alerts = new List<PerformanceAlert>();

        // Check for slow queries
        foreach (var query in queryMetrics)
        {
            if (query.AverageExecutionTimeMs > alertThresholdMs)
            {
                alerts.Add(new PerformanceAlert
                {
                    AlertId = Guid.NewGuid().ToString(),
                    Type = AlertType.SlowQuery,
                    Severity = AlertSeverity.Warning,
                    TriggeredAt = DateTime.UtcNow,
                    Title = "Slow Query Detected",
                    Description = $"Query '{query.QueryPattern}' averaging {query.AverageExecutionTimeMs:F2}ms",
                    ThresholdValue = alertThresholdMs,
                    ActualValue = query.AverageExecutionTimeMs,
                    QueryId = query.SqlHash
                });
            }

            if (query.ErrorRate > 10)
            {
                alerts.Add(new PerformanceAlert
                {
                    AlertId = Guid.NewGuid().ToString(),
                    Type = AlertType.HighErrorRate,
                    Severity = AlertSeverity.Error,
                    TriggeredAt = DateTime.UtcNow,
                    Title = "High Error Rate",
                    Description = $"Query '{query.QueryPattern}' has {query.ErrorRate:F1}% error rate",
                    ThresholdValue = 10,
                    ActualValue = query.ErrorRate,
                    QueryId = query.SqlHash
                });
            }
        }

        // Check system metrics
        if (systemMetrics.CpuUsagePercent > 80)
        {
            alerts.Add(new PerformanceAlert
            {
                AlertId = Guid.NewGuid().ToString(),
                Type = AlertType.HighCpuUsage,
                Severity = AlertSeverity.Warning,
                TriggeredAt = DateTime.UtcNow,
                Title = "High CPU Usage",
                Description = $"CPU usage at {systemMetrics.CpuUsagePercent:F1}%",
                ThresholdValue = 80,
                ActualValue = systemMetrics.CpuUsagePercent
            });
        }

        if (systemMetrics.MemoryUsagePercent > 85)
        {
            alerts.Add(new PerformanceAlert
            {
                AlertId = Guid.NewGuid().ToString(),
                Type = AlertType.HighMemoryUsage,
                Severity = AlertSeverity.Warning,
                TriggeredAt = DateTime.UtcNow,
                Title = "High Memory Usage",
                Description = $"Memory usage at {systemMetrics.MemoryUsagePercent:F1}%",
                ThresholdValue = 85,
                ActualValue = systemMetrics.MemoryUsagePercent
            });
        }

        return alerts;
    }

    private void LogMonitoringStatus(SystemMetrics systemMetrics, List<QueryPerformanceMetrics> queryMetrics, int alertCount)
    {
        var avgQueryTime = queryMetrics.Count > 0 ? 
            queryMetrics.Average(q => q.AverageExecutionTimeMs) : 0;

        _logger.LogInformation("📊 Status: CPU {Cpu:F1}%, Memory {Memory:F1}%, Avg Query {Query:F1}ms, Alerts {Alerts}",
            systemMetrics.CpuUsagePercent,
            systemMetrics.MemoryUsagePercent,
            avgQueryTime,
            alertCount);
    }

    private void LogAlert(PerformanceAlert alert, int alertNumber)
    {
        var emoji = alert.Severity switch
        {
            AlertSeverity.Info => "ℹ️",
            AlertSeverity.Warning => "⚠️",
            AlertSeverity.Error => "❌",
            AlertSeverity.Critical => "🚨",
            _ => "📢"
        };

        _logger.LogWarning("{Emoji} Alert #{Number}: {Title} - {Description}",
            emoji, alertNumber, alert.Title, alert.Description);
    }

    private async Task SavePeriodicDataAsync(string outputDirectory, DateTime timestamp, SystemMetrics systemMetrics, List<QueryPerformanceMetrics> queryMetrics)
    {
        try
        {
            Directory.CreateDirectory(outputDirectory);

            var data = new
            {
                Timestamp = timestamp,
                SystemMetrics = systemMetrics,
                QueryMetrics = queryMetrics
            };

            var fileName = $"monitoring_{timestamp:yyyyMMdd_HHmmss}.json";
            var filePath = Path.Combine(outputDirectory, fileName);
            
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(filePath, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save periodic monitoring data");
        }
    }

    private async Task SaveMonitoringReportAsync(string? outputDirectory, DateTime monitoringStart, TimeSpan duration)
    {
        if (outputDirectory == null) return;

        try
        {
            Directory.CreateDirectory(outputDirectory);

            var report = new
            {
                MonitoringSession = new
                {
                    StartTime = monitoringStart,
                    Duration = duration,
                    EndTime = monitoringStart + duration
                },
                TotalAlerts = _alerts.Count,
                AlertsByType = _alerts.GroupBy(a => a.Type).ToDictionary(g => g.Key.ToString(), g => g.Count()),
                AlertsBySeverity = _alerts.GroupBy(a => a.Severity).ToDictionary(g => g.Key.ToString(), g => g.Count()),
                Alerts = _alerts
            };

            var fileName = $"monitoring_report_{monitoringStart:yyyyMMdd_HHmmss}.json";
            var filePath = Path.Combine(outputDirectory, fileName);
            
            var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(filePath, json);

            _logger.LogInformation("📄 Monitoring report saved: {Path}", filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save monitoring report");
        }
    }

    private PerformanceRating DetermineRating(double averageTimeMs)
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
}
