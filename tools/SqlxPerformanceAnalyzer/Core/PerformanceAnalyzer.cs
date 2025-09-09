// -----------------------------------------------------------------------
// <copyright file="PerformanceAnalyzer.cs" company="Cricle">
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

namespace SqlxPerformanceAnalyzer.Core;

/// <summary>
/// Analyzes performance data and generates insights.
/// </summary>
public class PerformanceAnalyzer
{
    private readonly ILogger<PerformanceAnalyzer> _logger;
    private readonly QueryOptimizer _optimizer;

    public PerformanceAnalyzer(ILogger<PerformanceAnalyzer> logger, QueryOptimizer optimizer)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _optimizer = optimizer ?? throw new ArgumentNullException(nameof(optimizer));
    }

    /// <summary>
    /// Analyzes performance data and generates insights.
    /// </summary>
    public async Task AnalyzeAsync(string inputFile, string? outputFile, ReportFormat format, double slowThresholdMs)
    {
        _logger.LogInformation("🔬 Starting performance analysis");
        _logger.LogInformation("📄 Input: {Input}", inputFile);
        _logger.LogInformation("🐌 Slow query threshold: {Threshold}ms", slowThresholdMs);

        try
        {
            // Load execution data
            var executions = await LoadExecutionDataAsync(inputFile);
            _logger.LogInformation("📊 Loaded {Count} query executions", executions.Count);

            if (!executions.Any())
            {
                _logger.LogWarning("⚠️ No execution data found");
                return;
            }

            // Generate metrics
            var metrics = GenerateQueryMetrics(executions);
            _logger.LogInformation("📈 Generated metrics for {Count} unique queries", metrics.Count);

            // Identify problematic queries
            var problematicQueries = IdentifyProblematicQueries(metrics, slowThresholdMs);
            _logger.LogInformation("🚨 Found {Count} problematic queries", problematicQueries.Count);

            // Generate optimization suggestions
            await GenerateOptimizationSuggestionsAsync(metrics);
            _logger.LogInformation("💡 Generated optimization suggestions");

            // Create analysis report
            var report = CreateAnalysisReport(metrics, problematicQueries, slowThresholdMs);
            
            // Output report
            await OutputReportAsync(report, outputFile, format);

            PrintAnalysisSummary(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Analysis failed");
            throw;
        }
    }

    private async Task<List<QueryExecution>> LoadExecutionDataAsync(string inputFile)
    {
        if (!File.Exists(inputFile))
        {
            throw new FileNotFoundException($"Input file not found: {inputFile}");
        }

        var jsonContent = await File.ReadAllTextAsync(inputFile);
        var executions = JsonSerializer.Deserialize<List<QueryExecution>>(jsonContent);
        
        return executions ?? new List<QueryExecution>();
    }

    private List<QueryPerformanceMetrics> GenerateQueryMetrics(List<QueryExecution> executions)
    {
        var groupedExecutions = executions.GroupBy(e => e.SqlHash);
        var metrics = new List<QueryPerformanceMetrics>();

        foreach (var group in groupedExecutions)
        {
            var execList = group.ToList();
            var executionTimes = execList.Select(e => e.ExecutionTimeMs).OrderBy(t => t).ToList();

            var metric = new QueryPerformanceMetrics
            {
                QueryPattern = execList.First().SqlText,
                SqlHash = group.Key,
                ExecutionCount = execList.Count,
                TotalExecutionTimeMs = executionTimes.Sum(),
                AverageExecutionTimeMs = executionTimes.Average(),
                MinExecutionTimeMs = executionTimes.Min(),
                MaxExecutionTimeMs = executionTimes.Max(),
                MedianExecutionTimeMs = CalculatePercentile(executionTimes, 50),
                P95ExecutionTimeMs = CalculatePercentile(executionTimes, 95),
                P99ExecutionTimeMs = CalculatePercentile(executionTimes, 99),
                StandardDeviation = CalculateStandardDeviation(executionTimes),
                TotalRowsAffected = execList.Sum(e => e.RowsAffected),
                TotalBytesReturned = execList.Sum(e => e.BytesReturned),
                SuccessCount = execList.Count(e => e.Status == ExecutionStatus.Success),
                ErrorCount = execList.Count(e => e.Status == ExecutionStatus.Failed),
                TimeoutCount = execList.Count(e => e.Status == ExecutionStatus.Timeout),
                FirstSeen = execList.Min(e => e.ExecutedAt),
                LastSeen = execList.Max(e => e.ExecutedAt),
                CommonErrors = execList.Where(e => !string.IsNullOrEmpty(e.ErrorMessage))
                                     .GroupBy(e => e.ErrorMessage!)
                                     .OrderByDescending(g => g.Count())
                                     .Take(5)
                                     .Select(g => g.Key)
                                     .ToList(),
                Rating = DeterminePerformanceRating(executionTimes.Average())
            };

            metrics.Add(metric);
        }

        return metrics.OrderByDescending(m => m.TotalExecutionTimeMs).ToList();
    }

    private List<QueryPerformanceMetrics> IdentifyProblematicQueries(
        List<QueryPerformanceMetrics> metrics, 
        double slowThresholdMs)
    {
        return metrics.Where(m => 
            m.AverageExecutionTimeMs > slowThresholdMs ||
            m.ErrorRate > 5.0 || // More than 5% error rate
            m.P95ExecutionTimeMs > slowThresholdMs * 2 || // High P95
            m.Rating == PerformanceRating.Critical ||
            m.Rating == PerformanceRating.Poor
        ).ToList();
    }

    private async Task GenerateOptimizationSuggestionsAsync(List<QueryPerformanceMetrics> metrics)
    {
        foreach (var metric in metrics)
        {
            metric.Suggestions = await _optimizer.AnalyzeQueryAsync(metric.QueryPattern, metric);
        }
    }

    private PerformanceReport CreateAnalysisReport(
        List<QueryPerformanceMetrics> metrics,
        List<QueryPerformanceMetrics> problematicQueries,
        double slowThresholdMs)
    {
        var report = new PerformanceReport
        {
            ReportId = Guid.NewGuid().ToString(),
            GeneratedAt = DateTime.UtcNow,
            PeriodStart = metrics.Any() ? metrics.Min(m => m.FirstSeen) : DateTime.UtcNow,
            PeriodEnd = metrics.Any() ? metrics.Max(m => m.LastSeen) : DateTime.UtcNow,
            TopQueries = metrics.Take(10).ToList(),
            SlowestQueries = metrics.OrderByDescending(m => m.AverageExecutionTimeMs).Take(10).ToList(),
            MostFrequentQueries = metrics.OrderByDescending(m => m.ExecutionCount).Take(10).ToList(),
            ProblematicQueries = problematicQueries,
            GlobalSuggestions = GenerateGlobalSuggestions(metrics),
            Summary = CreateReportSummary(metrics, slowThresholdMs)
        };

        return report;
    }

    private ReportSummary CreateReportSummary(List<QueryPerformanceMetrics> metrics, double slowThresholdMs)
    {
        var totalExecutions = metrics.Sum(m => m.ExecutionCount);
        var totalErrors = metrics.Sum(m => m.ErrorCount);
        var slowQueries = metrics.Count(m => m.AverageExecutionTimeMs > slowThresholdMs);

        var summary = new ReportSummary
        {
            TotalQueries = totalExecutions,
            UniqueQueries = metrics.Count,
            TotalExecutionTimeMs = metrics.Sum(m => m.TotalExecutionTimeMs),
            AverageExecutionTimeMs = metrics.Average(m => m.AverageExecutionTimeMs),
            SlowQueriesCount = slowQueries,
            ErrorCount = totalErrors,
            ErrorRate = totalExecutions > 0 ? (double)totalErrors / totalExecutions * 100 : 0,
            PerformanceScore = CalculatePerformanceScore(metrics),
            OverallRating = DetermineOverallRating(metrics),
            RatingDistribution = metrics.GroupBy(m => m.Rating)
                                       .ToDictionary(g => g.Key, g => g.Count()),
            TopOperationTypes = AnalyzeOperationTypes(metrics)
        };

        return summary;
    }

    private List<OptimizationSuggestion> GenerateGlobalSuggestions(List<QueryPerformanceMetrics> metrics)
    {
        var suggestions = new List<OptimizationSuggestion>();

        // Analyze patterns across all queries
        var slowQueries = metrics.Where(m => m.Rating == PerformanceRating.Poor || 
                                           m.Rating == PerformanceRating.Critical).ToList();

        if (slowQueries.Count > metrics.Count * 0.2) // More than 20% are slow
        {
            suggestions.Add(new OptimizationSuggestion
            {
                Type = SuggestionType.OptimizeQuery,
                Title = "High Number of Slow Queries",
                Description = $"{slowQueries.Count} out of {metrics.Count} unique queries are performing poorly",
                Recommendation = "Consider reviewing database indexing strategy and query patterns",
                Impact = ImpactLevel.High,
                Priority = 1
            });
        }

        var highErrorRate = metrics.Where(m => m.ErrorRate > 5).ToList();
        if (highErrorRate.Any())
        {
            suggestions.Add(new OptimizationSuggestion
            {
                Type = SuggestionType.OptimizeQuery,
                Title = "Queries with High Error Rates",
                Description = $"{highErrorRate.Count} queries have error rates above 5%",
                Recommendation = "Review and fix queries with high error rates to improve reliability",
                Impact = ImpactLevel.Critical,
                Priority = 1
            });
        }

        return suggestions;
    }

    private double CalculatePercentile(List<double> values, double percentile)
    {
        if (values.Count == 0) return 0;
        
        var index = (percentile / 100.0) * (values.Count - 1);
        var lower = (int)Math.Floor(index);
        var upper = (int)Math.Ceiling(index);
        
        if (lower == upper) return values[lower];
        
        var weight = index - lower;
        return values[lower] * (1 - weight) + values[upper] * weight;
    }

    private double CalculateStandardDeviation(List<double> values)
    {
        if (values.Count <= 1) return 0;
        
        var mean = values.Average();
        var sumOfSquares = values.Sum(v => Math.Pow(v - mean, 2));
        return Math.Sqrt(sumOfSquares / (values.Count - 1));
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

    private double CalculatePerformanceScore(List<QueryPerformanceMetrics> metrics)
    {
        if (!metrics.Any()) return 100;

        var weights = new Dictionary<PerformanceRating, double>
        {
            { PerformanceRating.Excellent, 100 },
            { PerformanceRating.Good, 80 },
            { PerformanceRating.Fair, 60 },
            { PerformanceRating.Poor, 30 },
            { PerformanceRating.Critical, 0 }
        };

        var totalExecutions = metrics.Sum(m => m.ExecutionCount);
        var weightedScore = metrics.Sum(m => weights[m.Rating] * m.ExecutionCount);

        return Math.Round(weightedScore / totalExecutions, 1);
    }

    private PerformanceRating DetermineOverallRating(List<QueryPerformanceMetrics> metrics)
    {
        var score = CalculatePerformanceScore(metrics);
        
        return score switch
        {
            >= 90 => PerformanceRating.Excellent,
            >= 70 => PerformanceRating.Good,
            >= 50 => PerformanceRating.Fair,
            >= 25 => PerformanceRating.Poor,
            _ => PerformanceRating.Critical
        };
    }

    private Dictionary<string, double> AnalyzeOperationTypes(List<QueryPerformanceMetrics> metrics)
    {
        var operations = new Dictionary<string, double>();

        foreach (var metric in metrics)
        {
            var sql = metric.QueryPattern.Trim().ToUpperInvariant();
            var operation = sql.Split(' ')[0];
            
            if (operations.ContainsKey(operation))
            {
                operations[operation] += metric.TotalExecutionTimeMs;
            }
            else
            {
                operations[operation] = metric.TotalExecutionTimeMs;
            }
        }

        var total = operations.Values.Sum();
        return operations.ToDictionary(kvp => kvp.Key, kvp => Math.Round(kvp.Value / total * 100, 1));
    }

    private async Task OutputReportAsync(PerformanceReport report, string? outputFile, ReportFormat format)
    {
        string content = format switch
        {
            ReportFormat.Json => JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }),
            ReportFormat.Console => FormatConsoleReport(report),
            ReportFormat.Html => await FormatHtmlReportAsync(report),
            ReportFormat.Csv => FormatCsvReport(report),
            _ => FormatConsoleReport(report)
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

    private string FormatConsoleReport(PerformanceReport report)
    {
        var lines = new List<string>
        {
            "📊 PERFORMANCE ANALYSIS REPORT",
            "==============================",
            $"Generated: {report.GeneratedAt:yyyy-MM-dd HH:mm:ss}",
            $"Period: {report.PeriodStart:yyyy-MM-dd HH:mm:ss} - {report.PeriodEnd:yyyy-MM-dd HH:mm:ss}",
            "",
            "📈 SUMMARY",
            "----------",
            $"Total Queries: {report.Summary.TotalQueries:N0}",
            $"Unique Queries: {report.Summary.UniqueQueries:N0}",
            $"Average Execution Time: {report.Summary.AverageExecutionTimeMs:F2}ms",
            $"Slow Queries: {report.Summary.SlowQueriesCount:N0}",
            $"Error Rate: {report.Summary.ErrorRate:F2}%",
            $"Performance Score: {report.Summary.PerformanceScore:F1}/100",
            $"Overall Rating: {report.Summary.OverallRating}",
            ""
        };

        if (report.SlowestQueries.Any())
        {
            lines.Add("🐌 SLOWEST QUERIES");
            lines.Add("------------------");
            foreach (var query in report.SlowestQueries.Take(5))
            {
                lines.Add($"• {query.AverageExecutionTimeMs:F2}ms avg ({query.ExecutionCount} executions) - {TruncateQuery(query.QueryPattern)}");
            }
            lines.Add("");
        }

        if (report.ProblematicQueries.Any())
        {
            lines.Add("🚨 PROBLEMATIC QUERIES");
            lines.Add("----------------------");
            foreach (var query in report.ProblematicQueries.Take(5))
            {
                lines.Add($"• Rating: {query.Rating}, Avg: {query.AverageExecutionTimeMs:F2}ms, Errors: {query.ErrorRate:F1}%");
                lines.Add($"  SQL: {TruncateQuery(query.QueryPattern)}");
            }
            lines.Add("");
        }

        if (report.GlobalSuggestions.Any())
        {
            lines.Add("💡 OPTIMIZATION SUGGESTIONS");
            lines.Add("---------------------------");
            foreach (var suggestion in report.GlobalSuggestions.Take(5))
            {
                lines.Add($"• {suggestion.Title} ({suggestion.Impact} impact)");
                lines.Add($"  {suggestion.Recommendation}");
            }
        }

        return string.Join("\n", lines);
    }

    private Task<string> FormatHtmlReportAsync(PerformanceReport report)
    {
        // For now, return JSON wrapped in HTML
        var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
        return Task.FromResult($@"
<!DOCTYPE html>
<html>
<head>
    <title>Sqlx Performance Report</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; }}
        pre {{ background: #f5f5f5; padding: 10px; border-radius: 5px; }}
    </style>
</head>
<body>
    <h1>🔬 Sqlx Performance Analysis Report</h1>
    <p>Generated: {report.GeneratedAt:yyyy-MM-dd HH:mm:ss}</p>
    <pre>{json}</pre>
</body>
</html>");
    }

    private string FormatCsvReport(PerformanceReport report)
    {
        var lines = new List<string>
        {
            "QueryPattern,ExecutionCount,AvgTimeMs,MinTimeMs,MaxTimeMs,ErrorRate,Rating"
        };

        foreach (var query in report.TopQueries)
        {
            lines.Add($"\"{query.QueryPattern.Replace("\"", "\"\"")}\",{query.ExecutionCount},{query.AverageExecutionTimeMs:F2},{query.MinExecutionTimeMs:F2},{query.MaxExecutionTimeMs:F2},{query.ErrorRate:F2},{query.Rating}");
        }

        return string.Join("\n", lines);
    }

    private string TruncateQuery(string query)
    {
        const int maxLength = 80;
        if (query.Length <= maxLength) return query;
        return query.Substring(0, maxLength - 3) + "...";
    }

    private void PrintAnalysisSummary(PerformanceReport report)
    {
        _logger.LogInformation("✅ Analysis completed");
        _logger.LogInformation("📊 Summary: {Total} queries, {Score:F1}/100 score, {Rating} rating", 
            report.Summary.TotalQueries, report.Summary.PerformanceScore, report.Summary.OverallRating);
        
        if (report.ProblematicQueries.Any())
        {
            _logger.LogWarning("⚠️ Found {Count} problematic queries requiring attention", 
                report.ProblematicQueries.Count);
        }
        
        if (report.GlobalSuggestions.Any())
        {
            _logger.LogInformation("💡 Generated {Count} optimization suggestions", 
                report.GlobalSuggestions.Count);
        }
    }
}
