// -----------------------------------------------------------------------
// <copyright file="DiagnosticAnalyzer.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Sqlx.Core;

/// <summary>
/// Advanced diagnostic and error analysis for Sqlx source generation.
/// </summary>
internal static class DiagnosticAnalyzer
{
    private static readonly Dictionary<string, DiagnosticDescriptor> _diagnostics = new();

    static DiagnosticAnalyzer()
    {
        InitializeDiagnostics();
    }

    /// <summary>
    /// Analyzes source generation context for potential issues.
    /// </summary>
    public static IEnumerable<Diagnostic> AnalyzeContext(GeneratorExecutionContext context, ISqlxSyntaxReceiver receiver)
    {
        var diagnostics = new List<Diagnostic>();

        // Analyze performance issues
        diagnostics.AddRange(AnalyzePerformanceIssues(receiver));

        // Analyze memory usage patterns
        diagnostics.AddRange(AnalyzeMemoryPatterns(receiver));

        // Analyze async/await patterns
        diagnostics.AddRange(AnalyzeAsyncPatterns(receiver));

        // Analyze SQL injection risks
        diagnostics.AddRange(AnalyzeSqlInjectionRisks(receiver));

        return diagnostics;
    }

    /// <summary>
    /// Creates a performance warning diagnostic.
    /// </summary>
    public static Diagnostic CreatePerformanceWarning(Location? location, string message, params object[] args)
    {
        return Diagnostic.Create(
            _diagnostics["SQLX_PERF_001"],
            location,
            string.Format(message, args)
        );
    }

    /// <summary>
    /// Creates a memory usage warning diagnostic.
    /// </summary>
    public static Diagnostic CreateMemoryWarning(Location? location, string message, params object[] args)
    {
        return Diagnostic.Create(
            _diagnostics["SQLX_MEM_001"],
            location,
            string.Format(message, args)
        );
    }

    /// <summary>
    /// Creates a security warning diagnostic.
    /// </summary>
    public static Diagnostic CreateSecurityWarning(Location? location, string message, params object[] args)
    {
        return Diagnostic.Create(
            _diagnostics["SQLX_SEC_001"],
            location,
            string.Format(message, args)
        );
    }

    /// <summary>
    /// Generates comprehensive diagnostic report.
    /// </summary>
    public static string GenerateDiagnosticReport(ISqlxSyntaxReceiver receiver, PerformanceStatistics performance)
    {
        var report = new StringBuilder();
        
        report.AppendLine("=== Sqlx Diagnostic Report ===");
        report.AppendLine($"Generated at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        report.AppendLine();

        // Code generation statistics
        report.AppendLine("Code Generation Statistics:");
        report.AppendLine($"  Methods processed: {receiver.Methods.Count}");
        report.AppendLine($"  Repository classes: {receiver.RepositoryClasses.Count}");
        report.AppendLine();

        // Performance statistics
        report.AppendLine("Performance Statistics:");
        report.AppendLine($"  Total operations: {performance.TotalOperations}");
        report.AppendLine($"  Average execution time: {performance.AverageExecutionTimeMs:F2}ms");
        report.AppendLine($"  Total execution time: {performance.TotalExecutionTimeMs:F2}ms");
        report.AppendLine();

        // Method-level statistics
        if (performance.MethodStatistics.Any())
        {
            report.AppendLine("Method Performance:");
            foreach (var method in performance.MethodStatistics.Values.OrderByDescending(m => m.TotalExecutions))
            {
                report.AppendLine($"  {method.MethodName}:");
                report.AppendLine($"    Executions: {method.TotalExecutions}");
                report.AppendLine($"    Success rate: {method.SuccessRate:P1}");
                report.AppendLine($"    Avg time: {method.AverageExecutionTimeMs:F2}ms");
                report.AppendLine($"    Min/Max: {method.MinExecutionTimeMs:F2}ms / {method.MaxExecutionTimeMs:F2}ms");
                report.AppendLine();
            }
        }

        // Memory statistics
        var memoryStats = MemoryMonitor.GetMemoryStatistics();
        report.AppendLine("Memory Statistics:");
        report.AppendLine($"  Total memory: {memoryStats.TotalMemoryBytes / 1024 / 1024:F1} MB");
        report.AppendLine($"  After GC: {memoryStats.TotalMemoryAfterGC / 1024 / 1024:F1} MB");
        report.AppendLine($"  GC Collections: Gen0={memoryStats.Gen0Collections}, Gen1={memoryStats.Gen1Collections}, Gen2={memoryStats.Gen2Collections}");

        return report.ToString();
    }

    private static void InitializeDiagnostics()
    {
        _diagnostics["SQLX_PERF_001"] = new DiagnosticDescriptor(
            "SQLX_PERF_001",
            "Performance Warning",
            "{0}",
            "Performance",
            DiagnosticSeverity.Warning,
            true,
            "Potential performance issue detected in generated code."
        );

        _diagnostics["SQLX_MEM_001"] = new DiagnosticDescriptor(
            "SQLX_MEM_001",
            "Memory Usage Warning",
            "{0}",
            "Memory",
            DiagnosticSeverity.Warning,
            true,
            "Potential memory usage issue detected."
        );

        _diagnostics["SQLX_SEC_001"] = new DiagnosticDescriptor(
            "SQLX_SEC_001",
            "Security Warning",
            "{0}",
            "Security",
            DiagnosticSeverity.Warning,
            true,
            "Potential security issue detected."
        );

        _diagnostics["SQLX_ASYNC_001"] = new DiagnosticDescriptor(
            "SQLX_ASYNC_001",
            "Async Pattern Warning",
            "{0}",
            "Async",
            DiagnosticSeverity.Info,
            true,
            "Async/await pattern could be optimized."
        );
    }

    private static IEnumerable<Diagnostic> AnalyzePerformanceIssues(ISqlxSyntaxReceiver receiver)
    {
        var diagnostics = new List<Diagnostic>();

        // Check for excessive method count that might impact compilation time
        if (receiver.Methods.Count > 100)
        {
            diagnostics.Add(CreatePerformanceWarning(
                null,
                "Large number of methods ({0}) may impact compilation performance. Consider splitting into multiple classes.",
                receiver.Methods.Count
            ));
        }

        // Analyze method complexity
        foreach (var method in receiver.Methods)
        {
            if (method.Parameters.Length > 10)
            {
                diagnostics.Add(CreatePerformanceWarning(
                    method.Locations.FirstOrDefault(),
                    "Method '{0}' has many parameters ({1}). Consider using a parameter object for better performance.",
                    method.Name,
                    method.Parameters.Length
                ));
            }
        }

        return diagnostics;
    }

    private static IEnumerable<Diagnostic> AnalyzeMemoryPatterns(ISqlxSyntaxReceiver receiver)
    {
        var diagnostics = new List<Diagnostic>();

        // Check for potential memory allocation issues
        var stringReturnMethods = receiver.Methods.Where(m => 
            m.ReturnType.SpecialType == SpecialType.System_String).ToList();

        if (stringReturnMethods.Count > 20)
        {
            diagnostics.Add(CreateMemoryWarning(
                null,
                "Many methods return strings ({0}). Consider using StringBuilder or spans for better memory efficiency.",
                stringReturnMethods.Count
            ));
        }

        return diagnostics;
    }

    private static IEnumerable<Diagnostic> AnalyzeAsyncPatterns(ISqlxSyntaxReceiver receiver)
    {
        var diagnostics = new List<Diagnostic>();

        foreach (var method in receiver.Methods)
        {
            var isAsync = TypeAnalyzer.IsAsyncType(method.ReturnType);
            
            // Check for missing ConfigureAwait
            if (isAsync)
            {
                diagnostics.Add(Diagnostic.Create(
                    _diagnostics["SQLX_ASYNC_001"],
                    method.Locations.FirstOrDefault(),
                    $"Async method '{method.Name}' should use ConfigureAwait(false) for better performance in library code."
                ));
            }

            // Check for sync-over-async patterns
            if (!isAsync && method.Parameters.Any(p => p.Type.Name == "CancellationToken"))
            {
                diagnostics.Add(Diagnostic.Create(
                    _diagnostics["SQLX_ASYNC_001"],
                    method.Locations.FirstOrDefault(),
                    $"Method '{method.Name}' accepts CancellationToken but is not async. Consider making it async for better cancellation support."
                ));
            }
        }

        return diagnostics;
    }

    private static IEnumerable<Diagnostic> AnalyzeSqlInjectionRisks(ISqlxSyntaxReceiver receiver)
    {
        var diagnostics = new List<Diagnostic>();

        foreach (var method in receiver.Methods)
        {
            // Check for string parameters that might be used for SQL
            var stringParams = method.Parameters.Where(p => p.Type.SpecialType == SpecialType.System_String).ToList();
            
            foreach (var param in stringParams)
            {
                if (param.Name.ToLowerInvariant().Contains("sql") && 
                    !param.GetAttributes().Any(attr => attr.AttributeClass?.Name == "RawSqlAttribute"))
                {
                    diagnostics.Add(CreateSecurityWarning(
                        method.Locations.FirstOrDefault(),
                        "Parameter '{0}' in method '{1}' appears to contain SQL but lacks [RawSql] attribute. Ensure proper parameterization.",
                        param.Name,
                        method.Name
                    ));
                }
            }
        }

        return diagnostics;
    }
}

/// <summary>
/// Code quality analyzer for generated code.
/// </summary>
internal static class CodeQualityAnalyzer
{
    /// <summary>
    /// Analyzes generated code quality and suggests improvements.
    /// </summary>
    public static CodeQualityReport AnalyzeGeneratedCode(string generatedCode, string methodName)
    {
        var issues = new List<CodeQualityIssue>();
        var metrics = new CodeMetrics();

        // Analyze code metrics
        AnalyzeCodeMetrics(generatedCode, metrics);

        // Check for code smells
        issues.AddRange(DetectCodeSmells(generatedCode, methodName));

        // Check for performance issues
        issues.AddRange(DetectPerformanceIssues(generatedCode));

        // Check for maintainability issues
        issues.AddRange(DetectMaintainabilityIssues(generatedCode));

        return new CodeQualityReport(methodName, metrics, issues);
    }

    private static void AnalyzeCodeMetrics(string code, CodeMetrics metrics)
    {
        metrics.LinesOfCode = code.Split('\n').Length;
        metrics.CyclomaticComplexity = CountCyclomaticComplexity(code);
        metrics.NestingDepth = CalculateMaxNestingDepth(code);
        metrics.MethodLength = metrics.LinesOfCode; // Simplified for single method
    }

    private static int CountCyclomaticComplexity(string code)
    {
        // Simplified cyclomatic complexity calculation
        var complexity = 1; // Base complexity
        var keywords = new[] { "if", "else", "while", "for", "foreach", "switch", "case", "catch", "&&", "||", "?" };
        
        foreach (var keyword in keywords)
        {
            complexity += CountOccurrences(code, keyword);
        }

        return complexity;
    }

    private static int CalculateMaxNestingDepth(string code)
    {
        var maxDepth = 0;
        var currentDepth = 0;

        foreach (var ch in code)
        {
            if (ch == '{')
            {
                currentDepth++;
                maxDepth = Math.Max(maxDepth, currentDepth);
            }
            else if (ch == '}')
            {
                currentDepth--;
            }
        }

        return maxDepth;
    }

    private static int CountOccurrences(string text, string pattern)
    {
        var count = 0;
        var index = 0;

        while ((index = text.IndexOf(pattern, index, StringComparison.OrdinalIgnoreCase)) != -1)
        {
            count++;
            index += pattern.Length;
        }

        return count;
    }

    private static IEnumerable<CodeQualityIssue> DetectCodeSmells(string code, string methodName)
    {
        var issues = new List<CodeQualityIssue>();

        // Long method
        var lineCount = code.Split('\n').Length;
        if (lineCount > 100)
        {
            issues.Add(new CodeQualityIssue(
                CodeQualityIssueType.CodeSmell,
                "Long Method",
                $"Method '{methodName}' has {lineCount} lines. Consider breaking it into smaller methods.",
                CodeQualityIssueSeverity.Warning
            ));
        }

        // Deep nesting
        var maxNesting = CalculateMaxNestingDepth(code);
        if (maxNesting > 4)
        {
            issues.Add(new CodeQualityIssue(
                CodeQualityIssueType.CodeSmell,
                "Deep Nesting",
                $"Maximum nesting depth is {maxNesting}. Consider extracting nested logic into separate methods.",
                CodeQualityIssueSeverity.Warning
            ));
        }

        return issues;
    }

    private static IEnumerable<CodeQualityIssue> DetectPerformanceIssues(string code)
    {
        var issues = new List<CodeQualityIssue>();

        // String concatenation in loops
        if (code.Contains("for") && code.Contains("+=") && code.Contains("string"))
        {
            issues.Add(new CodeQualityIssue(
                CodeQualityIssueType.Performance,
                "String Concatenation in Loop",
                "String concatenation in loops detected. Consider using StringBuilder.",
                CodeQualityIssueSeverity.Info
            ));
        }

        // Missing ConfigureAwait
        if (code.Contains("await") && !code.Contains("ConfigureAwait"))
        {
            issues.Add(new CodeQualityIssue(
                CodeQualityIssueType.Performance,
                "Missing ConfigureAwait",
                "Async calls should use ConfigureAwait(false) in library code.",
                CodeQualityIssueSeverity.Info
            ));
        }

        return issues;
    }

    private static IEnumerable<CodeQualityIssue> DetectMaintainabilityIssues(string code)
    {
        var issues = new List<CodeQualityIssue>();

        // Magic numbers
        var numbers = System.Text.RegularExpressions.Regex.Matches(code, @"\b\d{2,}\b");
        if (numbers.Count > 3)
        {
            issues.Add(new CodeQualityIssue(
                CodeQualityIssueType.Maintainability,
                "Magic Numbers",
                $"Found {numbers.Count} numeric literals. Consider using named constants.",
                CodeQualityIssueSeverity.Info
            ));
        }

        return issues;
    }
}

/// <summary>
/// Code quality report for a generated method.
/// </summary>
public record CodeQualityReport(
    string MethodName,
    CodeMetrics Metrics,
    IReadOnlyList<CodeQualityIssue> Issues
);

/// <summary>
/// Code metrics for quality analysis.
/// </summary>
public class CodeMetrics
{
    /// <summary>Gets or sets the total lines of code.</summary>
    public int LinesOfCode { get; set; }
    
    /// <summary>Gets or sets the cyclomatic complexity.</summary>
    public int CyclomaticComplexity { get; set; }
    
    /// <summary>Gets or sets the maximum nesting depth.</summary>
    public int NestingDepth { get; set; }
    
    /// <summary>Gets or sets the method length in lines.</summary>
    public int MethodLength { get; set; }
}

/// <summary>
/// Represents a code quality issue.
/// </summary>
public record CodeQualityIssue(
    CodeQualityIssueType Type,
    string Title,
    string Description,
    CodeQualityIssueSeverity Severity
);

/// <summary>
/// Types of code quality issues.
/// </summary>
public enum CodeQualityIssueType
{
    /// <summary>Code smell issue.</summary>
    CodeSmell,
    /// <summary>Performance issue.</summary>
    Performance,
    /// <summary>Security issue.</summary>
    Security,
    /// <summary>Maintainability issue.</summary>
    Maintainability
}

/// <summary>
/// Severity levels for code quality issues.
/// </summary>
public enum CodeQualityIssueSeverity
{
    /// <summary>Informational message.</summary>
    Info,
    /// <summary>Warning message.</summary>
    Warning,
    /// <summary>Error message.</summary>
    Error
}
