// -----------------------------------------------------------------------
// <copyright file="CurrentDateTimePropertyTests.cs" company="Cricle">
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
/// Property-based tests for Current DateTime function correctness across all database dialects.
/// **Feature: sql-semantic-tdd-validation, Property 8: Current DateTime Function Correctness**
/// **Validates: Requirements 7.1, 7.2, 7.3, 7.4, 7.5**
/// </summary>
public class CurrentDateTimePropertyTests
{
    private static readonly MySqlDialectProvider MySqlProvider = new();
    private static readonly PostgreSqlDialectProvider PostgreSqlProvider = new();
    private static readonly SqlServerDialectProvider SqlServerProvider = new();
    private static readonly SQLiteDialectProvider SQLiteProvider = new();

    /// <summary>
    /// **Property 8: Current DateTime Function Correctness**
    /// *For any* database dialect, GetCurrentDateTimeSyntax SHALL return correct function name.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(CurrentDateTimeArbitraries) })]
    public Property GetCurrentDateTimeSyntax_ForAnyDialect_ShouldReturnCorrectFunction(
        CurrentDateTimeDialectProviderWithConfig providerConfig)
    {
        var config = providerConfig.Config;

        // Act
        var result = providerConfig.GetCurrentDateTimeSyntax();

        // Assert
        var matches = result == config.ExpectedCurrentTimestamp;

        return matches
            .Label($"Dialect: {config.DialectName}, " +
                   $"Result: '{result}' (expected: '{config.ExpectedCurrentTimestamp}')");
    }

    /// <summary>
    /// Property: Current DateTime syntax should be non-empty string.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(CurrentDateTimeArbitraries) })]
    public Property GetCurrentDateTimeSyntax_ShouldReturnNonEmptyString(
        CurrentDateTimeDialectProviderWithConfig providerConfig)
    {
        // Act
        var result = providerConfig.GetCurrentDateTimeSyntax();

        // Assert
        return (!string.IsNullOrEmpty(result))
            .Label($"Dialect: {providerConfig.Config.DialectName}, Result: '{result}'");
    }

    /// <summary>
    /// Property: Current DateTime syntax should be consistent across multiple calls.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(CurrentDateTimeArbitraries) })]
    public Property GetCurrentDateTimeSyntax_ShouldBeConsistentAcrossMultipleCalls(
        CurrentDateTimeDialectProviderWithConfig providerConfig)
    {
        // Act - call multiple times
        var result1 = providerConfig.GetCurrentDateTimeSyntax();
        var result2 = providerConfig.GetCurrentDateTimeSyntax();
        var result3 = providerConfig.GetCurrentDateTimeSyntax();

        // Assert
        return (result1 == result2 && result2 == result3)
            .Label($"Dialect: {providerConfig.Config.DialectName}, " +
                   $"Calls: '{result1}', '{result2}', '{result3}'");
    }

    /// <summary>
    /// Property: MySQL should return "NOW()".
    /// </summary>
    [Fact]
    public void MySQL_GetCurrentDateTimeSyntax_ShouldReturnNow()
    {
        // Act
        var result = MySqlProvider.GetCurrentDateTimeSyntax();

        // Assert
        Assert.Equal("NOW()", result);
    }

    /// <summary>
    /// Property: PostgreSQL should return "CURRENT_TIMESTAMP".
    /// </summary>
    [Fact]
    public void PostgreSQL_GetCurrentDateTimeSyntax_ShouldReturnCurrentTimestamp()
    {
        // Act
        var result = PostgreSqlProvider.GetCurrentDateTimeSyntax();

        // Assert
        Assert.Equal("CURRENT_TIMESTAMP", result);
    }

    /// <summary>
    /// Property: SQL Server should return "GETDATE()".
    /// </summary>
    [Fact]
    public void SqlServer_GetCurrentDateTimeSyntax_ShouldReturnGetDate()
    {
        // Act
        var result = SqlServerProvider.GetCurrentDateTimeSyntax();

        // Assert
        Assert.Equal("GETDATE()", result);
    }

    /// <summary>
    /// Property: SQLite should return "datetime('now')".
    /// </summary>
    [Fact]
    public void SQLite_GetCurrentDateTimeSyntax_ShouldReturnDatetimeNow()
    {
        // Act
        var result = SQLiteProvider.GetCurrentDateTimeSyntax();

        // Assert
        Assert.Equal("datetime('now')", result);
    }

    /// <summary>
    /// Property: Current DateTime syntax should contain recognizable timestamp keywords.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(CurrentDateTimeArbitraries) })]
    public Property GetCurrentDateTimeSyntax_ShouldContainTimestampKeywords(
        CurrentDateTimeDialectProviderWithConfig providerConfig)
    {
        // Act
        var result = providerConfig.GetCurrentDateTimeSyntax().ToUpperInvariant();

        // Assert - should contain one of the common timestamp-related keywords
        var containsTimestampKeyword = 
            result.Contains("NOW") ||
            result.Contains("TIMESTAMP") ||
            result.Contains("DATE") ||
            result.Contains("GETDATE") ||
            result.Contains("SYSDATE");

        return containsTimestampKeyword
            .Label($"Dialect: {providerConfig.Config.DialectName}, Result: '{result}'");
    }

    /// <summary>
    /// Property: Function-based dialects should have parentheses in their syntax.
    /// </summary>
    [Fact]
    public void FunctionBasedDialects_ShouldHaveParentheses()
    {
        // MySQL uses NOW(), SQL Server uses GETDATE(), SQLite uses datetime('now')
        Assert.Contains("(", MySqlProvider.GetCurrentDateTimeSyntax());
        Assert.Contains(")", MySqlProvider.GetCurrentDateTimeSyntax());
        
        Assert.Contains("(", SqlServerProvider.GetCurrentDateTimeSyntax());
        Assert.Contains(")", SqlServerProvider.GetCurrentDateTimeSyntax());
        
        Assert.Contains("(", SQLiteProvider.GetCurrentDateTimeSyntax());
        Assert.Contains(")", SQLiteProvider.GetCurrentDateTimeSyntax());
    }

    /// <summary>
    /// Property: PostgreSQL uses keyword-based syntax without parentheses.
    /// </summary>
    [Fact]
    public void PostgreSQL_ShouldUseKeywordBasedSyntax()
    {
        var result = PostgreSqlProvider.GetCurrentDateTimeSyntax();
        
        // CURRENT_TIMESTAMP is a keyword, not a function call
        Assert.DoesNotContain("(", result);
        Assert.DoesNotContain(")", result);
        Assert.Equal("CURRENT_TIMESTAMP", result);
    }

    /// <summary>
    /// Property: All dialects should return valid SQL expressions.
    /// </summary>
    [Fact]
    public void AllDialects_ShouldReturnValidSqlExpressions()
    {
        var providers = new IDatabaseDialectProvider[]
        {
            MySqlProvider,
            PostgreSqlProvider,
            SqlServerProvider,
            SQLiteProvider
        };

        foreach (var provider in providers)
        {
            var result = provider.GetCurrentDateTimeSyntax();
            
            // Should not be empty
            Assert.False(string.IsNullOrWhiteSpace(result));
            
            // Should not contain SQL injection patterns
            Assert.DoesNotContain(";", result);
            Assert.DoesNotContain("--", result);
            Assert.DoesNotContain("/*", result);
        }
    }
}

/// <summary>
/// Wrapper class to pair a dialect provider with its test configuration for Current DateTime tests.
/// </summary>
public class CurrentDateTimeDialectProviderWithConfig
{
    public Func<string> GetCurrentDateTimeSyntax { get; }
    public DialectTestConfig Config { get; }

    public CurrentDateTimeDialectProviderWithConfig(
        Func<string> getCurrentDateTimeSyntax,
        DialectTestConfig config)
    {
        GetCurrentDateTimeSyntax = getCurrentDateTimeSyntax;
        Config = config;
    }

    public override string ToString() => Config.DialectName;
}

/// <summary>
/// Custom arbitraries for Current DateTime property tests.
/// </summary>
public static class CurrentDateTimeArbitraries
{
    private static readonly MySqlDialectProvider MySqlProvider = new();
    private static readonly PostgreSqlDialectProvider PostgreSqlProvider = new();
    private static readonly SqlServerDialectProvider SqlServerProvider = new();
    private static readonly SQLiteDialectProvider SQLiteProvider = new();

    /// <summary>
    /// Generates dialect provider with its corresponding test configuration.
    /// </summary>
    public static Arbitrary<CurrentDateTimeDialectProviderWithConfig> CurrentDateTimeDialectProviderWithConfig()
    {
        return Gen.Elements(
            new CurrentDateTimeDialectProviderWithConfig(
                MySqlProvider.GetCurrentDateTimeSyntax,
                DialectTestConfig.MySql),
            new CurrentDateTimeDialectProviderWithConfig(
                PostgreSqlProvider.GetCurrentDateTimeSyntax,
                DialectTestConfig.PostgreSql),
            new CurrentDateTimeDialectProviderWithConfig(
                SqlServerProvider.GetCurrentDateTimeSyntax,
                DialectTestConfig.SqlServer),
            new CurrentDateTimeDialectProviderWithConfig(
                SQLiteProvider.GetCurrentDateTimeSyntax,
                DialectTestConfig.SQLite)
        ).ToArbitrary();
    }
}
