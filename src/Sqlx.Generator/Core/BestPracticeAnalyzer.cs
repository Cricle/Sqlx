// -----------------------------------------------------------------------
// <copyright file="BestPracticeAnalyzer.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.Generator.Core;

using Microsoft.CodeAnalysis;
using System;
using System.Linq;

/// <summary>
/// SQL最佳实践分析器 - 检查SQL查询的潜在问题并提供诊断建议
/// </summary>
internal static class BestPracticeAnalyzer
{
    /// <summary>
    /// 执行所有最佳实践检查
    /// </summary>
    public static void Analyze(IMethodSymbol method, string? sql, GeneratorExecutionContext? context)
    {
        if (!context.HasValue) return;

        CheckAsyncBestPractices(method, context.Value);
        CheckSqlSafety(method, sql, context.Value);
        CheckPerformance(method, context.Value);
        CheckQueryPatterns(method, sql, context.Value);
    }

    private static void CheckAsyncBestPractices(IMethodSymbol method, GeneratorExecutionContext context)
    {
        var isAsync = method.ReturnType?.Name == "Task" || method.ReturnType?.Name == "IAsyncEnumerable";
        if (isAsync && !method.Parameters.Any(p => p.Type.IsCancellationToken()))
        {
            ReportDiagnostic(context, method, Messages.SP0024, method.Name);
        }
    }

    private static void CheckSqlSafety(IMethodSymbol method, string? sql, GeneratorExecutionContext context)
    {
        if (string.IsNullOrEmpty(sql)) return;

        var upperSql = sql.ToUpperInvariant();

        // DELETE without WHERE
        if (upperSql.Contains("DELETE") && !upperSql.Contains("WHERE"))
            ReportDiagnostic(context, method, Messages.SP0020);

        // UPDATE without WHERE
        if (upperSql.Contains("UPDATE") && !upperSql.Contains("WHERE"))
            ReportDiagnostic(context, method, Messages.SP0021);

        // SELECT *
        if (upperSql.Contains("SELECT *"))
            ReportDiagnostic(context, method, Messages.SP0016);
    }

    private static void CheckPerformance(IMethodSymbol method, GeneratorExecutionContext context)
    {
        // 连接参数建议已移除，避免过多警告
    }

    private static void CheckQueryPatterns(IMethodSymbol method, string? sql, GeneratorExecutionContext context)
    {
        if (string.IsNullOrEmpty(sql)) return;

        var upperSql = sql.ToUpperInvariant();

        // Complex JOINs
        if (CountOccurrences(upperSql, "JOIN") >= 3)
            ReportDiagnostic(context, method, Messages.SP0033);

        // Subquery optimization
        if (upperSql.Contains("IN (SELECT") || upperSql.Contains("EXISTS (SELECT"))
            ReportDiagnostic(context, method, Messages.SP0034, "Consider rewriting as JOIN");

        // Large result sets
        if (upperSql.Contains("SELECT") && !upperSql.Contains("LIMIT") && !upperSql.Contains("TOP") &&
            !upperSql.Contains("WHERE") && !upperSql.Contains("ROWNUM"))
            ReportDiagnostic(context, method, Messages.SP0032);

        // Query complexity
        var complexity = CountOccurrences(upperSql, "JOIN") * 2 +
                        CountOccurrences(upperSql, "UNION") * 3 +
                        CountOccurrences(upperSql, "CASE WHEN") +
                        CountOccurrences(upperSql, "GROUP BY") +
                        CountOccurrences(upperSql, "HAVING") * 2;

        if (complexity >= 8)
            ReportDiagnostic(context, method, Messages.SP0040);
    }

    private static int CountOccurrences(string text, string substring)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(substring)) return 0;

        int count = 0, index = 0;
        while ((index = text.IndexOf(substring, index, StringComparison.OrdinalIgnoreCase)) != -1)
        {
            count++;
            index += substring.Length;
        }
        return count;
    }

    private static void ReportDiagnostic(GeneratorExecutionContext context, IMethodSymbol method, DiagnosticDescriptor descriptor, params object[] args)
    {
        try
        {
            context.ReportDiagnostic(Diagnostic.Create(descriptor, method.Locations.FirstOrDefault(), args));
        }
        catch
        {
            // 忽略诊断报告错误
        }
    }
}
