// -----------------------------------------------------------------------
// <copyright file="AnalysisModels.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace SqlxMigration.Models;

/// <summary>
/// Result of code analysis for migration opportunities.
/// </summary>
public class AnalysisResult
{
    public string ProjectPath { get; set; } = string.Empty;
    public DateTime AnalyzedAt { get; set; }
    public List<ProjectAnalysis> Projects { get; set; } = new();

    [JsonIgnore]
    public int TotalMigrationOpportunities => Projects.Sum(p => p.TotalOpportunities);
}

/// <summary>
/// Analysis result for a single project.
/// </summary>
public class ProjectAnalysis
{
    public string ProjectPath { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public List<FileAnalysis> Files { get; set; } = new();

    [JsonIgnore]
    public int TotalOpportunities => Files.Sum(f => f.TotalOpportunities);
}

/// <summary>
/// Analysis result for a single source file.
/// </summary>
public class FileAnalysis
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public List<DapperUsage> DapperUsages { get; set; } = new();
    public List<EntityFrameworkUsage> EntityFrameworkUsages { get; set; } = new();
    public List<SqlStringUsage> SqlStrings { get; set; } = new();
    public List<RepositoryPattern> RepositoryPatterns { get; set; } = new();

    [JsonIgnore]
    public bool HasMigrationOpportunities => TotalOpportunities > 0;

    [JsonIgnore]
    public int TotalOpportunities => DapperUsages.Count + EntityFrameworkUsages.Count +
                                   SqlStrings.Count + RepositoryPatterns.Count;
}

/// <summary>
/// Represents a Dapper usage in the code.
/// </summary>
public class DapperUsage
{
    public DapperUsageType Type { get; set; }
    public string? MethodName { get; set; }
    public int LineNumber { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? SqlCode { get; set; }
    public MigrationComplexity Complexity { get; set; } = MigrationComplexity.Medium;
    public string SuggestedSqlxCode { get; set; } = string.Empty;
}

/// <summary>
/// Types of Dapper usage.
/// </summary>
public enum DapperUsageType
{
    UsingStatement,
    Query,
    Execute,
    QueryMultiple,
    Other
}

/// <summary>
/// Represents Entity Framework usage in the code.
/// </summary>
public class EntityFrameworkUsage
{
    public EFUsageType Type { get; set; }
    public string? ClassName { get; set; }
    public string? PropertyName { get; set; }
    public int LineNumber { get; set; }
    public string Code { get; set; } = string.Empty;
    public MigrationComplexity Complexity { get; set; } = MigrationComplexity.High;
    public string SuggestedSqlxCode { get; set; } = string.Empty;
}

/// <summary>
/// Types of Entity Framework usage.
/// </summary>
public enum EFUsageType
{
    DbContext,
    DbSet,
    LinqQuery,
    Migration,
    ModelConfiguration,
    Other
}

/// <summary>
/// Represents a SQL string found in the code.
/// </summary>
public class SqlStringUsage
{
    public string SqlCode { get; set; } = string.Empty;
    public int LineNumber { get; set; }
    public SqlOperationType OperationType { get; set; }
    public MigrationComplexity Complexity { get; set; } = MigrationComplexity.Low;
    public string SuggestedSqlxCode { get; set; } = string.Empty;
}

/// <summary>
/// Types of SQL operations.
/// </summary>
public enum SqlOperationType
{
    Select,
    Insert,
    Update,
    Delete,
    Other
}

/// <summary>
/// Represents a repository pattern found in the code.
/// </summary>
public class RepositoryPattern
{
    public string InterfaceName { get; set; } = string.Empty;
    public int LineNumber { get; set; }
    public int MethodCount { get; set; }
    public string[] Methods { get; set; } = Array.Empty<string>();
    public MigrationComplexity Complexity { get; set; } = MigrationComplexity.Low;
    public string SuggestedSqlxCode { get; set; } = string.Empty;
}

/// <summary>
/// Complexity level of migration.
/// </summary>
public enum MigrationComplexity
{
    /// <summary>Simple migration, mostly automated.</summary>
    Low,

    /// <summary>Moderate migration, some manual work required.</summary>
    Medium,

    /// <summary>Complex migration, significant manual work required.</summary>
    High,

    /// <summary>Very complex, may require redesign.</summary>
    VeryHigh
}

/// <summary>
/// Migration recommendation for a specific code element.
/// </summary>
public class MigrationRecommendation
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public MigrationComplexity Complexity { get; set; }
    public string[] Steps { get; set; } = Array.Empty<string>();
    public string? BeforeCode { get; set; }
    public string? AfterCode { get; set; }
    public string[] Prerequisites { get; set; } = Array.Empty<string>();
    public string[] Benefits { get; set; } = Array.Empty<string>();
    public string[] Considerations { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Performance comparison between old and new code.
/// </summary>
public class PerformanceComparison
{
    public string Metric { get; set; } = string.Empty;
    public string OldValue { get; set; } = string.Empty;
    public string NewValue { get; set; } = string.Empty;
    public double ImprovementPercentage { get; set; }
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Migration statistics and metrics.
/// </summary>
public class MigrationStatistics
{
    public int TotalFiles { get; set; }
    public int FilesWithChanges { get; set; }
    public int TotalMethods { get; set; }
    public int MigratedMethods { get; set; }
    public Dictionary<DapperUsageType, int> DapperUsageBreakdown { get; set; } = new();
    public Dictionary<EFUsageType, int> EFUsageBreakdown { get; set; } = new();
    public Dictionary<MigrationComplexity, int> ComplexityBreakdown { get; set; } = new();
    public TimeSpan MigrationDuration { get; set; }
    public List<PerformanceComparison> PerformanceImprovements { get; set; } = new();
}

/// <summary>
/// Detailed migration plan for a project.
/// </summary>
public class MigrationPlan
{
    public string ProjectName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<MigrationRecommendation> Recommendations { get; set; } = new();
    public MigrationStatistics Statistics { get; set; } = new();
    public string[] Prerequisites { get; set; } = Array.Empty<string>();
    public string[] PostMigrationTasks { get; set; } = Array.Empty<string>();
    public EstimatedEffort Effort { get; set; } = new();
}

/// <summary>
/// Estimated effort for migration.
/// </summary>
public class EstimatedEffort
{
    public TimeSpan AutomatedMigrationTime { get; set; }
    public TimeSpan ManualWorkTime { get; set; }
    public TimeSpan TestingTime { get; set; }
    public TimeSpan TotalTime => AutomatedMigrationTime + ManualWorkTime + TestingTime;
    public int DeveloperHours => (int)Math.Ceiling(TotalTime.TotalHours);
    public string[] RequiredSkills { get; set; } = Array.Empty<string>();
    public string[] PotentialRisks { get; set; } = Array.Empty<string>();
}
