// -----------------------------------------------------------------------
// <copyright file="SqlTemplateRenderBenchmark.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Data;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;

namespace Sqlx.Benchmarks.Benchmarks;

/// <summary>
/// Benchmark for SqlTemplate.Render performance.
/// Tests the optimized single/double parameter overloads vs dictionary-based rendering.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class SqlTemplateRenderBenchmark
{
    private SqlTemplate _templateSingleParam = null!;
    private SqlTemplate _templateDoubleParam = null!;
    private Dictionary<string, object?> _singleParamDict = null!;
    private Dictionary<string, object?> _doubleParamDict = null!;

    [GlobalSetup]
    public void Setup()
    {
        var columns = new List<ColumnMeta>
        {
            new("id", "Id", DbType.Int64, false),
            new("name", "Name", DbType.String, false),
            new("email", "Email", DbType.String, true),
            new("age", "Age", DbType.Int32, false)
        };
        var context = new PlaceholderContext(
            dialect: SqlDefine.SQLite,
            tableName: "users",
            columns: columns);

        // Template with single dynamic parameter
        _templateSingleParam = SqlTemplate.Prepare(
            "SELECT {{columns}} FROM {{table}} WHERE {{where --param predicate}}",
            context);

        // Template with two dynamic parameters
        _templateDoubleParam = SqlTemplate.Prepare(
            "SELECT {{columns}} FROM {{table}} WHERE {{where --param predicate}} AND {{where --param filter}}",
            context);

        // Pre-create dictionaries for comparison
        _singleParamDict = new Dictionary<string, object?>(1)
        {
            ["predicate"] = "[age] > 18"
        };

        _doubleParamDict = new Dictionary<string, object?>(2)
        {
            ["predicate"] = "[age] > 18",
            ["filter"] = "[is_active] = 1"
        };
    }

    [Benchmark(Description = "Single param: Optimized Render(key, value)", Baseline = true)]
    public string SingleParam_Optimized()
    {
        return _templateSingleParam.Render("predicate", "[age] > 18");
    }

    [Benchmark(Description = "Single param: Dictionary Render")]
    public string SingleParam_Dictionary()
    {
        return _templateSingleParam.Render(_singleParamDict);
    }

    [Benchmark(Description = "Single param: New Dictionary each call")]
    public string SingleParam_NewDictionary()
    {
        var dict = new Dictionary<string, object?>(1)
        {
            ["predicate"] = "[age] > 18"
        };
        return _templateSingleParam.Render(dict);
    }

    [Benchmark(Description = "Double param: Optimized Render(k1,v1,k2,v2)")]
    public string DoubleParam_Optimized()
    {
        return _templateDoubleParam.Render("predicate", "[age] > 18", "filter", "[is_active] = 1");
    }

    [Benchmark(Description = "Double param: Dictionary Render")]
    public string DoubleParam_Dictionary()
    {
        return _templateDoubleParam.Render(_doubleParamDict);
    }

    [Benchmark(Description = "Double param: New Dictionary each call")]
    public string DoubleParam_NewDictionary()
    {
        var dict = new Dictionary<string, object?>(2)
        {
            ["predicate"] = "[age] > 18",
            ["filter"] = "[is_active] = 1"
        };
        return _templateDoubleParam.Render(dict);
    }
}
