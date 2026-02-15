// -----------------------------------------------------------------------
// <copyright file="SqlBuilderParameterBenchmark.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;

namespace Sqlx.Benchmarks.Benchmarks;

/// <summary>
/// Benchmark for SqlBuilder.AppendTemplate parameter handling performance.
/// Compares optimized Expression tree approach vs reflection-based approach.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class SqlBuilderParameterBenchmark
{
    private PlaceholderContext _context = null!;

    [GlobalSetup]
    public void Setup()
    {
        var columns = new List<ColumnMeta>
        {
            new("id", "Id", DbType.Int64, false),
            new("name", "Name", DbType.String, false),
            new("email", "Email", DbType.String, false),
            new("age", "Age", DbType.Int32, false)
        };
        _context = new PlaceholderContext(
            dialect: SqlDefine.SQLite,
            tableName: "users",
            columns: columns);
    }

    // ========== Small Anonymous Object (2 properties) ==========

    [Benchmark(Description = "2 props: Optimized (Expression tree)", Baseline = true)]
    public string SmallAnonymousObject_Optimized()
    {
        using var builder = new SqlBuilder(_context);
        builder.AppendTemplate("SELECT {{columns}} FROM {{table}} WHERE age >= @minAge AND status = @status",
            new { minAge = 18, status = "active" });
        return builder.Build().Sql;
    }

    [Benchmark(Description = "2 props: Reflection (baseline)")]
    public string SmallAnonymousObject_Reflection()
    {
        using var builder = new SqlBuilder(_context);
        var parameters = new { minAge = 18, status = "active" };
        var dict = ConvertUsingReflection(parameters);
        builder.AppendTemplate("SELECT {{columns}} FROM {{table}} WHERE age >= @minAge AND status = @status", dict);
        return builder.Build().Sql;
    }

    [Benchmark(Description = "2 props: Dictionary (no conversion)")]
    public string SmallDictionary_Direct()
    {
        using var builder = new SqlBuilder(_context);
        var dict = new Dictionary<string, object?> 
        { 
            { "minAge", 18 }, 
            { "status", "active" } 
        };
        builder.AppendTemplate("SELECT {{columns}} FROM {{table}} WHERE age >= @minAge AND status = @status", dict);
        return builder.Build().Sql;
    }

    // ========== Medium Anonymous Object (5 properties) ==========

    [Benchmark(Description = "5 props: Optimized (Expression tree)")]
    public string MediumAnonymousObject_Optimized()
    {
        using var builder = new SqlBuilder(_context);
        builder.AppendTemplate(
            "SELECT {{columns}} FROM {{table}} WHERE age >= @minAge AND status = @status AND city = @city AND country = @country AND verified = @verified",
            new { minAge = 18, status = "active", city = "Seattle", country = "USA", verified = true });
        return builder.Build().Sql;
    }

    [Benchmark(Description = "5 props: Reflection (baseline)")]
    public string MediumAnonymousObject_Reflection()
    {
        using var builder = new SqlBuilder(_context);
        var parameters = new { minAge = 18, status = "active", city = "Seattle", country = "USA", verified = true };
        var dict = ConvertUsingReflection(parameters);
        builder.AppendTemplate(
            "SELECT {{columns}} FROM {{table}} WHERE age >= @minAge AND status = @status AND city = @city AND country = @country AND verified = @verified",
            dict);
        return builder.Build().Sql;
    }

    [Benchmark(Description = "5 props: Dictionary (no conversion)")]
    public string MediumDictionary_Direct()
    {
        using var builder = new SqlBuilder(_context);
        var dict = new Dictionary<string, object?> 
        { 
            { "minAge", 18 }, 
            { "status", "active" },
            { "city", "Seattle" },
            { "country", "USA" },
            { "verified", true }
        };
        builder.AppendTemplate(
            "SELECT {{columns}} FROM {{table}} WHERE age >= @minAge AND status = @status AND city = @city AND country = @country AND verified = @verified",
            dict);
        return builder.Build().Sql;
    }

    // ========== Large Anonymous Object (10 properties) ==========

    [Benchmark(Description = "10 props: Optimized (Expression tree)")]
    public string LargeAnonymousObject_Optimized()
    {
        using var builder = new SqlBuilder(_context);
        builder.AppendTemplate(
            "SELECT {{columns}} FROM {{table}} WHERE p1 = @p1 AND p2 = @p2 AND p3 = @p3 AND p4 = @p4 AND p5 = @p5 AND p6 = @p6 AND p7 = @p7 AND p8 = @p8 AND p9 = @p9 AND p10 = @p10",
            new { p1 = 1, p2 = 2, p3 = 3, p4 = 4, p5 = 5, p6 = 6, p7 = 7, p8 = 8, p9 = 9, p10 = 10 });
        return builder.Build().Sql;
    }

    [Benchmark(Description = "10 props: Reflection (baseline)")]
    public string LargeAnonymousObject_Reflection()
    {
        using var builder = new SqlBuilder(_context);
        var parameters = new { p1 = 1, p2 = 2, p3 = 3, p4 = 4, p5 = 5, p6 = 6, p7 = 7, p8 = 8, p9 = 9, p10 = 10 };
        var dict = ConvertUsingReflection(parameters);
        builder.AppendTemplate(
            "SELECT {{columns}} FROM {{table}} WHERE p1 = @p1 AND p2 = @p2 AND p3 = @p3 AND p4 = @p4 AND p5 = @p5 AND p6 = @p6 AND p7 = @p7 AND p8 = @p8 AND p9 = @p9 AND p10 = @p10",
            dict);
        return builder.Build().Sql;
    }

    [Benchmark(Description = "10 props: Dictionary (no conversion)")]
    public string LargeDictionary_Direct()
    {
        using var builder = new SqlBuilder(_context);
        var dict = new Dictionary<string, object?> 
        { 
            { "p1", 1 }, { "p2", 2 }, { "p3", 3 }, { "p4", 4 }, { "p5", 5 },
            { "p6", 6 }, { "p7", 7 }, { "p8", 8 }, { "p9", 9 }, { "p10", 10 }
        };
        builder.AppendTemplate(
            "SELECT {{columns}} FROM {{table}} WHERE p1 = @p1 AND p2 = @p2 AND p3 = @p3 AND p4 = @p4 AND p5 = @p5 AND p6 = @p6 AND p7 = @p7 AND p8 = @p8 AND p9 = @p9 AND p10 = @p10",
            dict);
        return builder.Build().Sql;
    }

    // ========== Multiple Calls (tests Expression tree caching) ==========

    [Benchmark(Description = "Multiple calls: Optimized (cached Expression)")]
    public string MultipleCalls_Optimized()
    {
        string result = string.Empty;
        for (int i = 0; i < 10; i++)
        {
            using var builder = new SqlBuilder(_context);
            builder.AppendTemplate("SELECT {{columns}} FROM {{table}} WHERE age >= @minAge AND status = @status",
                new { minAge = 18 + i, status = "active" });
            result = builder.Build().Sql;
        }
        return result;
    }

    [Benchmark(Description = "Multiple calls: Reflection (no cache)")]
    public string MultipleCalls_Reflection()
    {
        string result = string.Empty;
        for (int i = 0; i < 10; i++)
        {
            using var builder = new SqlBuilder(_context);
            var parameters = new { minAge = 18 + i, status = "active" };
            var dict = ConvertUsingReflection(parameters);
            builder.AppendTemplate("SELECT {{columns}} FROM {{table}} WHERE age >= @minAge AND status = @status", dict);
            result = builder.Build().Sql;
        }
        return result;
    }

    // ========== Helper: Reflection-based conversion (baseline) ==========

    private static Dictionary<string, object?> ConvertUsingReflection<T>(T obj)
    {
        var dict = new Dictionary<string, object?>();
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var prop in properties)
        {
            dict[prop.Name] = prop.GetValue(obj);
        }
        return dict;
    }
}
