// -----------------------------------------------------------------------
// <copyright file="PerformanceModels.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SqlxPerformanceAnalyzer.Models;

/// <summary>
/// Represents a single SQL query execution measurement.
/// </summary>
public class QueryExecution
{
    public string QueryId { get; set; } = string.Empty;
    public string SqlText { get; set; } = string.Empty;
    public string SqlHash { get; set; } = string.Empty;
    public DateTime ExecutedAt { get; set; }
    public double ExecutionTimeMs { get; set; }
    public long RowsAffected { get; set; }
    public long BytesReturned { get; set; }
    public string DatabaseName { get; set; } = string.Empty;
    public string? Parameters { get; set; }
    public ExecutionStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public double CpuTimeMs { get; set; }
    public double IoTimeMs { get; set; }
    public double LockTimeMs { get; set; }
    public long MemoryUsedBytes { get; set; }
    public string ConnectionInfo { get; set; } = string.Empty;
    public string StackTrace { get; set; } = string.Empty;
}

/// <summary>
/// Status of query execution.
/// </summary>
public enum ExecutionStatus
{
    Success,
    Failed,
    Timeout,
    Cancelled
}

/// <summary>
/// Aggregated performance metrics for a specific query pattern.
/// </summary>
public class QueryPerformanceMetrics
{
    public string QueryPattern { get; set; } = string.Empty;
    public string SqlHash { get; set; } = string.Empty;
    public int ExecutionCount { get; set; }
    public double TotalExecutionTimeMs { get; set; }
    public double AverageExecutionTimeMs { get; set; }
    public double MinExecutionTimeMs { get; set; }
    public double MaxExecutionTimeMs { get; set; }
    public double MedianExecutionTimeMs { get; set; }
    public double P95ExecutionTimeMs { get; set; }
    public double P99ExecutionTimeMs { get; set; }
    public double StandardDeviation { get; set; }
    public long TotalRowsAffected { get; set; }
    public long TotalBytesReturned { get; set; }
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public int TimeoutCount { get; set; }
    public double ErrorRate => ExecutionCount > 0 ? (double)ErrorCount / ExecutionCount * 100 : 0;
    public DateTime FirstSeen { get; set; }
    public DateTime LastSeen { get; set; }
    public List<string> CommonErrors { get; set; } = new();
    public PerformanceRating Rating { get; set; }
    public List<OptimizationSuggestion> Suggestions { get; set; } = new();
}

/// <summary>
/// Performance rating for queries.
/// </summary>
public enum PerformanceRating
{
    Excellent,   // < 10ms
    Good,        // < 100ms
    Fair,        // < 500ms
    Poor,        // < 2000ms
    Critical     // >= 2000ms
}

/// <summary>
/// Performance optimization suggestion.
/// </summary>
public class OptimizationSuggestion
{
    public SuggestionType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
    public ImpactLevel Impact { get; set; }
    public int Priority { get; set; }
    public string? SqlExample { get; set; }
}

/// <summary>
/// Types of optimization suggestions.
/// </summary>
public enum SuggestionType
{
    AddIndex,
    OptimizeQuery,
    ReduceColumns,
    AddWhere,
    UseBatch,
    CacheResult,
    PartitionTable,
    OptimizeJoins,
    ReduceSubqueries,
    UseStoredProcedure
}

/// <summary>
/// Impact level of suggestions.
/// </summary>
public enum ImpactLevel
{
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// System performance metrics at a point in time.
/// </summary>
public class SystemMetrics
{
    public DateTime Timestamp { get; set; }
    public double CpuUsagePercent { get; set; }
    public double MemoryUsagePercent { get; set; }
    public long MemoryUsedBytes { get; set; }
    public long MemoryAvailableBytes { get; set; }
    public double DiskIoReadMBps { get; set; }
    public double DiskIoWriteMBps { get; set; }
    public double NetworkIoMBps { get; set; }
    public int ActiveConnections { get; set; }
    public int TotalConnections { get; set; }
    public double QueriesPerSecond { get; set; }
    public double TransactionsPerSecond { get; set; }
    public long CacheHits { get; set; }
    public long CacheMisses { get; set; }
    public double CacheHitRatio => (CacheHits + CacheMisses) > 0 ? (double)CacheHits / (CacheHits + CacheMisses) * 100 : 0;
    public List<string> Alerts { get; set; } = new();
}

/// <summary>
/// Performance analysis report.
/// </summary>
public class PerformanceReport
{
    public string ReportId { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public string DatabaseName { get; set; } = string.Empty;
    public ReportSummary Summary { get; set; } = new();
    public List<QueryPerformanceMetrics> TopQueries { get; set; } = new();
    public List<QueryPerformanceMetrics> SlowestQueries { get; set; } = new();
    public List<QueryPerformanceMetrics> MostFrequentQueries { get; set; } = new();
    public List<QueryPerformanceMetrics> ProblematicQueries { get; set; } = new();
    public List<OptimizationSuggestion> GlobalSuggestions { get; set; } = new();
    public List<SystemMetrics> SystemMetricsHistory { get; set; } = new();
    public Dictionary<string, object> CustomMetrics { get; set; } = new();
}

/// <summary>
/// Summary statistics for a performance report.
/// </summary>
public class ReportSummary
{
    public int TotalQueries { get; set; }
    public int UniqueQueries { get; set; }
    public double TotalExecutionTimeMs { get; set; }
    public double AverageExecutionTimeMs { get; set; }
    public double QueriesPerSecond { get; set; }
    public int SlowQueriesCount { get; set; }
    public int ErrorCount { get; set; }
    public double ErrorRate { get; set; }
    public double PerformanceScore { get; set; } // 0-100 overall score
    public PerformanceRating OverallRating { get; set; }
    public Dictionary<PerformanceRating, int> RatingDistribution { get; set; } = new();
    public Dictionary<string, int> DatabaseUsage { get; set; } = new();
    public Dictionary<string, double> TopOperationTypes { get; set; } = new();
}

/// <summary>
/// Benchmark execution result.
/// </summary>
public class BenchmarkResult
{
    public string BenchmarkId { get; set; } = string.Empty;
    public DateTime ExecutedAt { get; set; }
    public string SqlQuery { get; set; } = string.Empty;
    public int Iterations { get; set; }
    public int Concurrency { get; set; }
    public double TotalTimeMs { get; set; }
    public double AverageTimeMs { get; set; }
    public double MinTimeMs { get; set; }
    public double MaxTimeMs { get; set; }
    public double MedianTimeMs { get; set; }
    public double P95TimeMs { get; set; }
    public double P99TimeMs { get; set; }
    public double StandardDeviation { get; set; }
    public double ThroughputQps { get; set; }
    public long TotalRowsReturned { get; set; }
    public long TotalBytesTransferred { get; set; }
    public int SuccessfulExecutions { get; set; }
    public int FailedExecutions { get; set; }
    public List<string> Errors { get; set; } = new();
    public SystemResourceUsage ResourceUsage { get; set; } = new();
}

/// <summary>
/// System resource usage during benchmark.
/// </summary>
public class SystemResourceUsage
{
    public double PeakCpuUsage { get; set; }
    public double AverageCpuUsage { get; set; }
    public long PeakMemoryUsage { get; set; }
    public long AverageMemoryUsage { get; set; }
    public double PeakDiskIo { get; set; }
    public double AverageDiskIo { get; set; }
    public double PeakNetworkIo { get; set; }
    public double AverageNetworkIo { get; set; }
}

/// <summary>
/// Alert configuration and instance.
/// </summary>
public class PerformanceAlert
{
    public string AlertId { get; set; } = string.Empty;
    public AlertType Type { get; set; }
    public AlertSeverity Severity { get; set; }
    public DateTime TriggeredAt { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double ThresholdValue { get; set; }
    public double ActualValue { get; set; }
    public string QueryId { get; set; } = string.Empty;
    public string? SqlText { get; set; }
    public bool IsResolved { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Types of performance alerts.
/// </summary>
public enum AlertType
{
    SlowQuery,
    HighErrorRate,
    ConnectionPoolExhaustion,
    HighCpuUsage,
    HighMemoryUsage,
    LockTimeout,
    DeadlockDetected,
    UnusualQueryPattern
}

/// <summary>
/// Alert severity levels.
/// </summary>
public enum AlertSeverity
{
    Info,
    Warning,
    Error,
    Critical
}

