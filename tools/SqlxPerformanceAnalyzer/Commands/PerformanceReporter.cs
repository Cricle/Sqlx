// -----------------------------------------------------------------------
// <copyright file="PerformanceReporter.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using SqlxPerformanceAnalyzer.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace SqlxPerformanceAnalyzer.Commands;

/// <summary>
/// Generates comprehensive performance reports from monitoring data.
/// </summary>
public class PerformanceReporter
{
    private readonly ILogger<PerformanceReporter> _logger;

    public PerformanceReporter(ILogger<PerformanceReporter> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Generates a comprehensive performance report.
    /// </summary>
    public async Task GenerateReportAsync(string inputDirectory, string? outputFile, ReportFormat format, ReportPeriod period)
    {
        _logger.LogInformation("📊 Generating performance report");
        _logger.LogInformation("📁 Input directory: {Directory}", inputDirectory);
        _logger.LogInformation("📅 Period: {Period}", period);

        try
        {
            if (!Directory.Exists(inputDirectory))
            {
                throw new DirectoryNotFoundException($"Input directory not found: {inputDirectory}");
            }

            var monitoringFiles = Directory.GetFiles(inputDirectory, "monitoring_*.json")
                .OrderBy(f => f)
                .ToArray();

            if (!monitoringFiles.Any())
            {
                _logger.LogWarning("No monitoring data files found in {Directory}", inputDirectory);
                return;
            }

            _logger.LogInformation("📄 Found {Count} monitoring data files", monitoringFiles.Length);

            // Filter files by period
            var filteredFiles = FilterFilesByPeriod(monitoringFiles, period);
            _logger.LogInformation("📅 Using {Count} files for the specified period", filteredFiles.Length);

            // Generate report
            var report = await CreateComprehensiveReportAsync(filteredFiles);
            
            // Output report
            await OutputReportAsync(report, outputFile, format);

            _logger.LogInformation("✅ Report generation completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Report generation failed");
            throw;
        }
    }

    private string[] FilterFilesByPeriod(string[] files, ReportPeriod period)
    {
        var cutoffTime = period switch
        {
            ReportPeriod.LastHour => DateTime.UtcNow.AddHours(-1),
            ReportPeriod.LastDay => DateTime.UtcNow.AddDays(-1),
            ReportPeriod.LastWeek => DateTime.UtcNow.AddDays(-7),
            ReportPeriod.LastMonth => DateTime.UtcNow.AddDays(-30),
            ReportPeriod.All => DateTime.MinValue,
            _ => DateTime.UtcNow.AddDays(-1)
        };

        return files.Where(f => File.GetCreationTimeUtc(f) >= cutoffTime).ToArray();
    }

    private async Task<PerformanceReport> CreateComprehensiveReportAsync(string[] monitoringFiles)
    {
        var report = new PerformanceReport
        {
            ReportId = Guid.NewGuid().ToString(),
            GeneratedAt = DateTime.UtcNow,
            DatabaseName = "Monitored Database"
        };

        var allSystemMetrics = new List<SystemMetrics>();
        var allQueryMetrics = new List<QueryPerformanceMetrics>();

        // Process each monitoring file
        foreach (var file in monitoringFiles)
        {
            try
            {
                var content = await File.ReadAllTextAsync(file);
                var data = JsonSerializer.Deserialize<MonitoringData>(content);

                if (data != null)
                {
                    if (data.SystemMetrics != null)
                        allSystemMetrics.Add(data.SystemMetrics);
                    
                    if (data.QueryMetrics != null)
                        allQueryMetrics.AddRange(data.QueryMetrics);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse monitoring file: {File}", file);
            }
        }

        // Set report period
        if (allSystemMetrics.Any())
        {
            report.PeriodStart = allSystemMetrics.Min(m => m.Timestamp);
            report.PeriodEnd = allSystemMetrics.Max(m => m.Timestamp);
        }

        // Aggregate query metrics
        var aggregatedQueries = AggregateQueryMetrics(allQueryMetrics);
        
        report.TopQueries = aggregatedQueries.Take(20).ToList();
        report.SlowestQueries = aggregatedQueries.OrderByDescending(q => q.AverageExecutionTimeMs).Take(10).ToList();
        report.MostFrequentQueries = aggregatedQueries.OrderByDescending(q => q.ExecutionCount).Take(10).ToList();
        report.ProblematicQueries = aggregatedQueries.Where(q => q.Rating == PerformanceRating.Poor || q.Rating == PerformanceRating.Critical).ToList();

        // System metrics history
        report.SystemMetricsHistory = allSystemMetrics.OrderBy(m => m.Timestamp).ToList();

        // Generate summary
        report.Summary = CreateReportSummary(aggregatedQueries, allSystemMetrics);

        return report;
    }

    private List<QueryPerformanceMetrics> AggregateQueryMetrics(List<QueryPerformanceMetrics> allMetrics)
    {
        var grouped = allMetrics.GroupBy(m => m.SqlHash);
        var aggregated = new List<QueryPerformanceMetrics>();

        foreach (var group in grouped)
        {
            var metrics = group.ToList();
            var totalExecutions = metrics.Sum(m => m.ExecutionCount);
            var totalTime = metrics.Sum(m => m.TotalExecutionTimeMs);

            var aggregatedMetric = new QueryPerformanceMetrics
            {
                QueryPattern = metrics.First().QueryPattern,
                SqlHash = group.Key,
                ExecutionCount = totalExecutions,
                TotalExecutionTimeMs = totalTime,
                AverageExecutionTimeMs = totalExecutions > 0 ? totalTime / totalExecutions : 0,
                MinExecutionTimeMs = metrics.Min(m => m.MinExecutionTimeMs),
                MaxExecutionTimeMs = metrics.Max(m => m.MaxExecutionTimeMs),
                TotalRowsAffected = metrics.Sum(m => m.TotalRowsAffected),
                TotalBytesReturned = metrics.Sum(m => m.TotalBytesReturned),
                SuccessCount = metrics.Sum(m => m.SuccessCount),
                ErrorCount = metrics.Sum(m => m.ErrorCount),
                TimeoutCount = metrics.Sum(m => m.TimeoutCount),
                FirstSeen = metrics.Min(m => m.FirstSeen),
                LastSeen = metrics.Max(m => m.LastSeen),
                Rating = DetermineRating(totalExecutions > 0 ? totalTime / totalExecutions : 0)
            };

            aggregated.Add(aggregatedMetric);
        }

        return aggregated.OrderByDescending(m => m.TotalExecutionTimeMs).ToList();
    }

    private ReportSummary CreateReportSummary(List<QueryPerformanceMetrics> queries, List<SystemMetrics> systemMetrics)
    {
        var totalExecutions = queries.Sum(q => q.ExecutionCount);
        var totalErrors = queries.Sum(q => q.ErrorCount);

        var summary = new ReportSummary
        {
            TotalQueries = totalExecutions,
            UniqueQueries = queries.Count,
            TotalExecutionTimeMs = queries.Sum(q => q.TotalExecutionTimeMs),
            AverageExecutionTimeMs = queries.Any() ? queries.Average(q => q.AverageExecutionTimeMs) : 0,
            SlowQueriesCount = queries.Count(q => q.AverageExecutionTimeMs > 1000),
            ErrorCount = totalErrors,
            ErrorRate = totalExecutions > 0 ? (double)totalErrors / totalExecutions * 100 : 0,
            PerformanceScore = CalculatePerformanceScore(queries),
            OverallRating = DetermineOverallRating(queries),
            RatingDistribution = queries.GroupBy(q => q.Rating).ToDictionary(g => g.Key, g => g.Count())
        };

        return summary;
    }

    private double CalculatePerformanceScore(List<QueryPerformanceMetrics> queries)
    {
        if (!queries.Any()) return 100;

        var weights = new Dictionary<PerformanceRating, double>
        {
            { PerformanceRating.Excellent, 100 },
            { PerformanceRating.Good, 80 },
            { PerformanceRating.Fair, 60 },
            { PerformanceRating.Poor, 30 },
            { PerformanceRating.Critical, 0 }
        };

        var totalExecutions = queries.Sum(q => q.ExecutionCount);
        var weightedScore = queries.Sum(q => weights[q.Rating] * q.ExecutionCount);

        return Math.Round(weightedScore / totalExecutions, 1);
    }

    private PerformanceRating DetermineOverallRating(List<QueryPerformanceMetrics> queries)
    {
        var score = CalculatePerformanceScore(queries);
        
        return score switch
        {
            >= 90 => PerformanceRating.Excellent,
            >= 70 => PerformanceRating.Good,
            >= 50 => PerformanceRating.Fair,
            >= 25 => PerformanceRating.Poor,
            _ => PerformanceRating.Critical
        };
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

    private async Task OutputReportAsync(PerformanceReport report, string? outputFile, ReportFormat format)
    {
        string content = format switch
        {
            ReportFormat.Json => JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }),
            ReportFormat.Html => GenerateHtmlReport(report),
            ReportFormat.Csv => GenerateCsvReport(report),
            _ => GenerateConsoleReport(report)
        };

        if (!string.IsNullOrEmpty(outputFile))
        {
            await File.WriteAllTextAsync(outputFile, content);
            _logger.LogInformation("📄 Report saved to: {Path}", outputFile);
        }
        else
        {
            Console.WriteLine(content);
        }
    }

    private string GenerateConsoleReport(PerformanceReport report)
    {
        var lines = new List<string>
        {
            "📊 COMPREHENSIVE PERFORMANCE REPORT",
            "===================================",
            $"Report ID: {report.ReportId}",
            $"Generated: {report.GeneratedAt:yyyy-MM-dd HH:mm:ss}",
            $"Period: {report.PeriodStart:yyyy-MM-dd HH:mm:ss} - {report.PeriodEnd:yyyy-MM-dd HH:mm:ss}",
            "",
            "📈 EXECUTIVE SUMMARY",
            "-------------------",
            $"Performance Score: {report.Summary.PerformanceScore:F1}/100",
            $"Overall Rating: {report.Summary.OverallRating}",
            $"Total Queries: {report.Summary.TotalQueries:N0}",
            $"Unique Queries: {report.Summary.UniqueQueries:N0}",
            $"Average Execution Time: {report.Summary.AverageExecutionTimeMs:F2}ms",
            $"Error Rate: {report.Summary.ErrorRate:F2}%",
            ""
        };

        // Rating distribution
        if (report.Summary.RatingDistribution.Any())
        {
            lines.Add("🎯 PERFORMANCE DISTRIBUTION");
            lines.Add("---------------------------");
            foreach (var rating in report.Summary.RatingDistribution.OrderBy(kvp => kvp.Key))
            {
                lines.Add($"{rating.Key}: {rating.Value} queries");
            }
            lines.Add("");
        }

        // Top problematic queries
        if (report.ProblematicQueries.Any())
        {
            lines.Add("🚨 PROBLEMATIC QUERIES (TOP 5)");
            lines.Add("------------------------------");
            foreach (var query in report.ProblematicQueries.Take(5))
            {
                lines.Add($"• {query.Rating} - {query.AverageExecutionTimeMs:F2}ms avg, {query.ErrorRate:F1}% errors");
                lines.Add($"  Pattern: {TruncateQuery(query.QueryPattern)}");
                lines.Add("");
            }
        }

        // System performance trends
        if (report.SystemMetricsHistory.Any())
        {
            var avgCpu = report.SystemMetricsHistory.Average(m => m.CpuUsagePercent);
            var avgMemory = report.SystemMetricsHistory.Average(m => m.MemoryUsagePercent);
            var maxCpu = report.SystemMetricsHistory.Max(m => m.CpuUsagePercent);
            var maxMemory = report.SystemMetricsHistory.Max(m => m.MemoryUsagePercent);

            lines.Add("🖥️ SYSTEM PERFORMANCE");
            lines.Add("--------------------");
            lines.Add($"CPU Usage - Average: {avgCpu:F1}%, Peak: {maxCpu:F1}%");
            lines.Add($"Memory Usage - Average: {avgMemory:F1}%, Peak: {maxMemory:F1}%");
            lines.Add("");
        }

        return string.Join("\n", lines);
    }

    private string GenerateHtmlReport(PerformanceReport report)
    {
        var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
        return $@"
<!DOCTYPE html>
<html>
<head>
    <title>Sqlx Performance Report</title>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: 'Segoe UI', Arial, sans-serif; margin: 20px; background: #f5f5f5; }}
        .container {{ max-width: 1200px; margin: 0 auto; background: white; padding: 20px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        h1 {{ color: #2c3e50; border-bottom: 3px solid #3498db; padding-bottom: 10px; }}
        .summary {{ display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 15px; margin: 20px 0; }}
        .metric {{ background: #ecf0f1; padding: 15px; border-radius: 5px; text-align: center; }}
        .metric-value {{ font-size: 24px; font-weight: bold; color: #2c3e50; }}
        .metric-label {{ color: #7f8c8d; margin-top: 5px; }}
        .rating-excellent {{ color: #27ae60; }}
        .rating-good {{ color: #f39c12; }}
        .rating-fair {{ color: #e67e22; }}
        .rating-poor {{ color: #e74c3c; }}
        .rating-critical {{ color: #c0392b; }}
        pre {{ background: #2c3e50; color: #ecf0f1; padding: 15px; border-radius: 5px; overflow-x: auto; }}
    </style>
</head>
<body>
    <div class='container'>
        <h1>🔬 Sqlx Performance Report</h1>
        <p><strong>Generated:</strong> {report.GeneratedAt:yyyy-MM-dd HH:mm:ss} UTC</p>
        <p><strong>Period:</strong> {report.PeriodStart:yyyy-MM-dd HH:mm:ss} - {report.PeriodEnd:yyyy-MM-dd HH:mm:ss}</p>
        
        <div class='summary'>
            <div class='metric'>
                <div class='metric-value rating-{report.Summary.OverallRating.ToString().ToLower()}'>{report.Summary.PerformanceScore:F1}/100</div>
                <div class='metric-label'>Performance Score</div>
            </div>
            <div class='metric'>
                <div class='metric-value'>{report.Summary.TotalQueries:N0}</div>
                <div class='metric-label'>Total Queries</div>
            </div>
            <div class='metric'>
                <div class='metric-value'>{report.Summary.AverageExecutionTimeMs:F1}ms</div>
                <div class='metric-label'>Avg Execution Time</div>
            </div>
            <div class='metric'>
                <div class='metric-value'>{report.Summary.ErrorRate:F1}%</div>
                <div class='metric-label'>Error Rate</div>
            </div>
        </div>

        <h2>📊 Detailed Data</h2>
        <pre>{json}</pre>
    </div>
</body>
</html>";
    }

    private string GenerateCsvReport(PerformanceReport report)
    {
        var lines = new List<string>
        {
            "QueryPattern,ExecutionCount,AvgTimeMs,MinTimeMs,MaxTimeMs,ErrorRate,Rating,FirstSeen,LastSeen"
        };

        foreach (var query in report.TopQueries)
        {
            lines.Add($"\"{query.QueryPattern.Replace("\"", "\"\"")}\",{query.ExecutionCount},{query.AverageExecutionTimeMs:F2},{query.MinExecutionTimeMs:F2},{query.MaxExecutionTimeMs:F2},{query.ErrorRate:F2},{query.Rating},{query.FirstSeen:yyyy-MM-dd HH:mm:ss},{query.LastSeen:yyyy-MM-dd HH:mm:ss}");
        }

        return string.Join("\n", lines);
    }

    private string TruncateQuery(string query)
    {
        const int maxLength = 80;
        if (query.Length <= maxLength) return query;
        return query.Substring(0, maxLength - 3) + "...";
    }

    private class MonitoringData
    {
        public DateTime Timestamp { get; set; }
        public SystemMetrics? SystemMetrics { get; set; }
        public List<QueryPerformanceMetrics>? QueryMetrics { get; set; }
    }
}
