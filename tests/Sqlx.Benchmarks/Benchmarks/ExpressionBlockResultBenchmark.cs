// <copyright file="ExpressionBlockResultBenchmark.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Sqlx;
using Sqlx.Expressions;

namespace Sqlx.Benchmarks.Benchmarks;

/// <summary>
/// Benchmarks comparing ExpressionBlockResult vs traditional separate parsing.
/// Tests the performance advantage of unified expression parsing.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class ExpressionBlockResultBenchmark
{
    private class TestEntity
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public bool IsActive { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int Version { get; set; }
    }

    private Expression<Func<TestEntity, TestEntity>> _updateExpr = null!;
    private Expression<Func<TestEntity, bool>> _whereExpr = null!;
    private SqlDialect _dialect = null!;

    [GlobalSetup]
    public void Setup()
    {
        _dialect = SqlDefine.SQLite;
        
        // 复杂的 UPDATE 表达式
        _updateExpr = e => new TestEntity
        {
            Name = e.Name.Trim().ToLower(),
            Age = e.Age + 1,
            Version = e.Version + 1,
            UpdatedAt = DateTime.UtcNow
        };

        // 复杂的 WHERE 表达式
        var minAge = 18;
        var maxAge = 65;
        _whereExpr = e => e.Age > minAge && e.Age < maxAge && e.IsActive;
    }

    // ========== UPDATE 表达式解析 ==========

    [Benchmark(Description = "Traditional: ToSetClause + GetSetParameters")]
    public (string sql, Dictionary<string, object?> parameters) TraditionalUpdateParsing()
    {
        var sql = _updateExpr.ToSetClause(_dialect);
        var parameters = _updateExpr.GetSetParameters();
        return (sql, parameters);
    }

    [Benchmark(Description = "ExpressionBlockResult: ParseUpdate")]
    public ExpressionBlockResult UnifiedUpdateParsing()
    {
        return ExpressionBlockResult.ParseUpdate(_updateExpr, _dialect);
    }

    // ========== WHERE 表达式解析 ==========

    [Benchmark(Description = "Traditional: ToWhereClause + GetParameters")]
    public (string sql, Dictionary<string, object?> parameters) TraditionalWhereParsing()
    {
        var sql = _whereExpr.ToWhereClause(_dialect);
        var parameters = _whereExpr.GetParameters();
        return (sql, parameters);
    }

    [Benchmark(Description = "ExpressionBlockResult: Parse")]
    public ExpressionBlockResult UnifiedWhereParsing()
    {
        return ExpressionBlockResult.Parse(_whereExpr.Body, _dialect);
    }

    // ========== 完整场景：UPDATE + WHERE ==========

    [Benchmark(Description = "Traditional: Separate parsing (4 passes)")]
    public (string updateSql, Dictionary<string, object?> updateParams, string whereSql, Dictionary<string, object?> whereParams) TraditionalFullScenario()
    {
        // 解析 UPDATE 表达式（2 次遍历）
        var updateSql = _updateExpr.ToSetClause(_dialect);
        var updateParams = _updateExpr.GetSetParameters();

        // 解析 WHERE 表达式（2 次遍历）
        var whereSql = _whereExpr.ToWhereClause(_dialect);
        var whereParams = _whereExpr.GetParameters();

        return (updateSql, updateParams, whereSql, whereParams);
    }

    [Benchmark(Description = "ExpressionBlockResult: Unified parsing (2 passes)", Baseline = true)]
    public (ExpressionBlockResult updateResult, ExpressionBlockResult whereResult) UnifiedFullScenario()
    {
        // 解析 UPDATE 表达式（1 次遍历）
        var updateResult = ExpressionBlockResult.ParseUpdate(_updateExpr, _dialect);

        // 解析 WHERE 表达式（1 次遍历）
        var whereResult = ExpressionBlockResult.Parse(_whereExpr.Body, _dialect);

        return (updateResult, whereResult);
    }

    // ========== 简单表达式性能测试 ==========

    [Benchmark(Description = "Simple UPDATE: Traditional")]
    public (string sql, Dictionary<string, object?> parameters) SimpleUpdateTraditional()
    {
        Expression<Func<TestEntity, TestEntity>> expr = e => new TestEntity { Name = "John", Age = 30 };
        var sql = expr.ToSetClause(_dialect);
        var parameters = expr.GetSetParameters();
        return (sql, parameters);
    }

    [Benchmark(Description = "Simple UPDATE: ExpressionBlockResult")]
    public ExpressionBlockResult SimpleUpdateUnified()
    {
        Expression<Func<TestEntity, TestEntity>> expr = e => new TestEntity { Name = "John", Age = 30 };
        return ExpressionBlockResult.ParseUpdate(expr, _dialect);
    }

    [Benchmark(Description = "Simple WHERE: Traditional")]
    public (string sql, Dictionary<string, object?> parameters) SimpleWhereTraditional()
    {
        var minAge = 18;
        Expression<Func<TestEntity, bool>> expr = e => e.Age > minAge;
        var sql = expr.ToWhereClause(_dialect);
        var parameters = expr.GetParameters();
        return (sql, parameters);
    }

    [Benchmark(Description = "Simple WHERE: ExpressionBlockResult")]
    public ExpressionBlockResult SimpleWhereUnified()
    {
        var minAge = 18;
        Expression<Func<TestEntity, bool>> expr = e => e.Age > minAge;
        return ExpressionBlockResult.Parse(expr.Body, _dialect);
    }

    // ========== 内存分配测试 ==========

    [Benchmark(Description = "Memory: Traditional UPDATE")]
    public void MemoryTraditionalUpdate()
    {
        for (int i = 0; i < 100; i++)
        {
            var sql = _updateExpr.ToSetClause(_dialect);
            var parameters = _updateExpr.GetSetParameters();
        }
    }

    [Benchmark(Description = "Memory: ExpressionBlockResult UPDATE")]
    public void MemoryUnifiedUpdate()
    {
        for (int i = 0; i < 100; i++)
        {
            var result = ExpressionBlockResult.ParseUpdate(_updateExpr, _dialect);
        }
    }
}
