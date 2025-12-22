// -----------------------------------------------------------------------
// <copyright file="BooleanLiteralPropertyTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq;
using FsCheck;
using FsCheck.Xunit;
using Xunit;
using Sqlx.Generator;
using GenSqlDefine = Sqlx.Generator.SqlDefine;

namespace Sqlx.Tests.Dialects.PropertyTests;

/// <summary>
/// Property-based tests for Boolean literal correctness across all database dialects.
/// **Feature: sql-semantic-tdd-validation, Property 7: Boolean Literal Correctness**
/// **Validates: Requirements 6.1, 6.2, 6.3, 6.4, 6.5, 6.6, 6.7, 6.8**
/// </summary>
public class BooleanLiteralPropertyTests
{
    private static readonly MySqlDialectProvider MySqlProvider = new();
    private static readonly PostgreSqlDialectProvider PostgreSqlProvider = new();
    private static readonly SqlServerDialectProvider SqlServerProvider = new();
    private static readonly SQLiteDialectProvider SQLiteProvider = new();

    /// <summary>
    /// **Property 7: Boolean Literal Correctness**
    /// *For any* database dialect, GetBoolTrueLiteral and GetBoolFalseLiteral 
    /// SHALL return correct boolean representation.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(BooleanArbitraries) })]
    public Property GetBoolLiterals_ForAnyDialect_ShouldReturnCorrectRepresentation(
        BooleanDialectProviderWithConfig providerConfig)
    {
        var config = providerConfig.Config;

        // Act
        var trueLiteral = providerConfig.GetBoolTrueLiteral();
        var falseLiteral = providerConfig.GetBoolFalseLiteral();

        // Assert
        var trueMatches = trueLiteral == config.ExpectedBoolTrue;
        var falseMatches = falseLiteral == config.ExpectedBoolFalse;

        return (trueMatches && falseMatches)
            .Label($"Dialect: {config.DialectName}, " +
                   $"True: '{trueLiteral}' (expected: '{config.ExpectedBoolTrue}'), " +
                   $"False: '{falseLiteral}' (expected: '{config.ExpectedBoolFalse}')");
    }

    /// <summary>
    /// Property: Boolean literals should be non-empty strings.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(BooleanArbitraries) })]
    public Property GetBoolLiterals_ShouldReturnNonEmptyStrings(
        BooleanDialectProviderWithConfig providerConfig)
    {
        // Act
        var trueLiteral = providerConfig.GetBoolTrueLiteral();
        var falseLiteral = providerConfig.GetBoolFalseLiteral();

        // Assert
        return (!string.IsNullOrEmpty(trueLiteral) && !string.IsNullOrEmpty(falseLiteral))
            .Label($"Dialect: {providerConfig.Config.DialectName}, " +
                   $"True: '{trueLiteral}', False: '{falseLiteral}'");
    }

    /// <summary>
    /// Property: Boolean true and false literals should be different.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(BooleanArbitraries) })]
    public Property GetBoolLiterals_TrueAndFalse_ShouldBeDifferent(
        BooleanDialectProviderWithConfig providerConfig)
    {
        // Act
        var trueLiteral = providerConfig.GetBoolTrueLiteral();
        var falseLiteral = providerConfig.GetBoolFalseLiteral();

        // Assert
        return (trueLiteral != falseLiteral)
            .Label($"Dialect: {providerConfig.Config.DialectName}, " +
                   $"True: '{trueLiteral}', False: '{falseLiteral}'");
    }

    /// <summary>
    /// Property: MySQL should return "1" for true and "0" for false.
    /// </summary>
    [Fact]
    public void MySQL_GetBoolLiterals_ShouldReturn1And0()
    {
        // Act
        var trueLiteral = MySqlProvider.GetBoolTrueLiteral();
        var falseLiteral = MySqlProvider.GetBoolFalseLiteral();

        // Assert
        Assert.Equal("1", trueLiteral);
        Assert.Equal("0", falseLiteral);
    }

    /// <summary>
    /// Property: PostgreSQL should return "true" for true and "false" for false.
    /// </summary>
    [Fact]
    public void PostgreSQL_GetBoolLiterals_ShouldReturnTrueAndFalse()
    {
        // Act
        var trueLiteral = PostgreSqlProvider.GetBoolTrueLiteral();
        var falseLiteral = PostgreSqlProvider.GetBoolFalseLiteral();

        // Assert
        Assert.Equal("true", trueLiteral);
        Assert.Equal("false", falseLiteral);
    }

    /// <summary>
    /// Property: SQL Server should return "1" for true and "0" for false.
    /// </summary>
    [Fact]
    public void SqlServer_GetBoolLiterals_ShouldReturn1And0()
    {
        // Act
        var trueLiteral = SqlServerProvider.GetBoolTrueLiteral();
        var falseLiteral = SqlServerProvider.GetBoolFalseLiteral();

        // Assert
        Assert.Equal("1", trueLiteral);
        Assert.Equal("0", falseLiteral);
    }

    /// <summary>
    /// Property: SQLite should return "1" for true and "0" for false.
    /// </summary>
    [Fact]
    public void SQLite_GetBoolLiterals_ShouldReturn1And0()
    {
        // Act
        var trueLiteral = SQLiteProvider.GetBoolTrueLiteral();
        var falseLiteral = SQLiteProvider.GetBoolFalseLiteral();

        // Assert
        Assert.Equal("1", trueLiteral);
        Assert.Equal("0", falseLiteral);
    }

    /// <summary>
    /// Property: Boolean literals should be consistent across multiple calls.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(BooleanArbitraries) })]
    public Property GetBoolLiterals_ShouldBeConsistentAcrossMultipleCalls(
        BooleanDialectProviderWithConfig providerConfig)
    {
        // Act - call multiple times
        var trueLiteral1 = providerConfig.GetBoolTrueLiteral();
        var trueLiteral2 = providerConfig.GetBoolTrueLiteral();
        var falseLiteral1 = providerConfig.GetBoolFalseLiteral();
        var falseLiteral2 = providerConfig.GetBoolFalseLiteral();

        // Assert
        return (trueLiteral1 == trueLiteral2 && falseLiteral1 == falseLiteral2)
            .Label($"Dialect: {providerConfig.Config.DialectName}, " +
                   $"True calls: '{trueLiteral1}' vs '{trueLiteral2}', " +
                   $"False calls: '{falseLiteral1}' vs '{falseLiteral2}'");
    }

    /// <summary>
    /// Property: All dialects using numeric boolean (1/0) should be consistent.
    /// </summary>
    [Fact]
    public void NumericBooleanDialects_ShouldUse1And0()
    {
        // MySQL, SQL Server, SQLite use numeric boolean
        var numericDialects = new IDatabaseDialectProvider[]
        {
            MySqlProvider,
            SqlServerProvider,
            SQLiteProvider
        };

        foreach (var provider in numericDialects)
        {
            Assert.Equal("1", provider.GetBoolTrueLiteral());
            Assert.Equal("0", provider.GetBoolFalseLiteral());
        }
    }

    /// <summary>
    /// Property: PostgreSQL uses native boolean literals.
    /// </summary>
    [Fact]
    public void PostgreSQL_ShouldUseNativeBooleanLiterals()
    {
        // PostgreSQL is the only dialect that uses native boolean literals
        Assert.Equal("true", PostgreSqlProvider.GetBoolTrueLiteral());
        Assert.Equal("false", PostgreSqlProvider.GetBoolFalseLiteral());
    }
}

/// <summary>
/// Wrapper class to pair a dialect provider with its test configuration for Boolean tests.
/// </summary>
public class BooleanDialectProviderWithConfig
{
    public Func<string> GetBoolTrueLiteral { get; }
    public Func<string> GetBoolFalseLiteral { get; }
    public DialectTestConfig Config { get; }

    public BooleanDialectProviderWithConfig(
        Func<string> getBoolTrueLiteral,
        Func<string> getBoolFalseLiteral,
        DialectTestConfig config)
    {
        GetBoolTrueLiteral = getBoolTrueLiteral;
        GetBoolFalseLiteral = getBoolFalseLiteral;
        Config = config;
    }

    public override string ToString() => Config.DialectName;
}

/// <summary>
/// Custom arbitraries for Boolean property tests.
/// </summary>
public static class BooleanArbitraries
{
    private static readonly MySqlDialectProvider MySqlProvider = new();
    private static readonly PostgreSqlDialectProvider PostgreSqlProvider = new();
    private static readonly SqlServerDialectProvider SqlServerProvider = new();
    private static readonly SQLiteDialectProvider SQLiteProvider = new();

    /// <summary>
    /// Generates dialect provider with its corresponding test configuration.
    /// </summary>
    public static Arbitrary<BooleanDialectProviderWithConfig> BooleanDialectProviderWithConfig()
    {
        return Gen.Elements(
            new BooleanDialectProviderWithConfig(
                MySqlProvider.GetBoolTrueLiteral,
                MySqlProvider.GetBoolFalseLiteral,
                DialectTestConfig.MySql),
            new BooleanDialectProviderWithConfig(
                PostgreSqlProvider.GetBoolTrueLiteral,
                PostgreSqlProvider.GetBoolFalseLiteral,
                DialectTestConfig.PostgreSql),
            new BooleanDialectProviderWithConfig(
                SqlServerProvider.GetBoolTrueLiteral,
                SqlServerProvider.GetBoolFalseLiteral,
                DialectTestConfig.SqlServer),
            new BooleanDialectProviderWithConfig(
                SQLiteProvider.GetBoolTrueLiteral,
                SQLiteProvider.GetBoolFalseLiteral,
                DialectTestConfig.SQLite)
        ).ToArbitrary();
    }
}
