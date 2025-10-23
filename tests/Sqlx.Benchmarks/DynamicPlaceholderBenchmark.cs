using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Sqlx;
using Sqlx.Validation;
using System;

namespace Sqlx.Benchmarks;

/// <summary>
/// 动态占位符验证性能基准测试
/// </summary>
[SimpleJob(RuntimeMoniker.Net90)]
[MemoryDiagnoser]
[RankColumn]
public class DynamicPlaceholderBenchmark
{
    private string _validIdentifier = null!;
    private string _validFragment = null!;
    private string _validTablePart = null!;
    private string _invalidIdentifier = null!;

    [GlobalSetup]
    public void Setup()
    {
        _validIdentifier = "users";
        _validFragment = "age > 18 AND status = 'active'";
        _validTablePart = "202410";
        _invalidIdentifier = "users'; DROP TABLE";
    }

    #region 验证性能测试

    [Benchmark(Description = "IsValidIdentifier - 有效输入")]
    public bool IsValidIdentifier_Valid()
    {
        return SqlValidator.IsValidIdentifier(_validIdentifier.AsSpan());
    }

    [Benchmark(Description = "IsValidIdentifier - 无效输入")]
    public bool IsValidIdentifier_Invalid()
    {
        return SqlValidator.IsValidIdentifier(_invalidIdentifier.AsSpan());
    }

    [Benchmark(Description = "IsValidFragment - 有效输入")]
    public bool IsValidFragment_Valid()
    {
        return SqlValidator.IsValidFragment(_validFragment.AsSpan());
    }

    [Benchmark(Description = "IsValidTablePart - 有效输入")]
    public bool IsValidTablePart_Valid()
    {
        return SqlValidator.IsValidTablePart(_validTablePart.AsSpan());
    }

    [Benchmark(Description = "ContainsDangerousKeyword - 无危险关键字")]
    public bool ContainsDangerousKeyword_Safe()
    {
        return SqlValidator.ContainsDangerousKeyword(_validFragment.AsSpan());
    }

    [Benchmark(Description = "ContainsDangerousKeyword - 有危险关键字")]
    public bool ContainsDangerousKeyword_Dangerous()
    {
        return SqlValidator.ContainsDangerousKeyword(_invalidIdentifier.AsSpan());
    }

    #endregion

    #region 完整验证流程

    [Benchmark(Baseline = true, Description = "完整验证流程 - Identifier")]
    public void FullValidation_Identifier()
    {
        if (!SqlValidator.IsValidIdentifier(_validIdentifier.AsSpan()))
        {
            throw new ArgumentException($"Invalid identifier: {_validIdentifier}");
        }
    }

    [Benchmark(Description = "完整验证流程 - Fragment")]
    public void FullValidation_Fragment()
    {
        if (!SqlValidator.IsValidFragment(_validFragment.AsSpan()))
        {
            throw new ArgumentException($"Invalid fragment: {_validFragment}");
        }
    }

    [Benchmark(Description = "完整验证流程 - TablePart")]
    public void FullValidation_TablePart()
    {
        if (!SqlValidator.IsValidTablePart(_validTablePart.AsSpan()))
        {
            throw new ArgumentException($"Invalid table part: {_validTablePart}");
        }
    }

    #endregion

    #region 与字符串操作对比

    [Benchmark(Description = "参考 - 字符串长度检查")]
    public bool Reference_StringLength()
    {
        return _validIdentifier.Length > 0 && _validIdentifier.Length <= 128;
    }

    [Benchmark(Description = "参考 - 字符串包含检查")]
    public bool Reference_StringContains()
    {
        return _validIdentifier.Contains("users");
    }

    #endregion
}

