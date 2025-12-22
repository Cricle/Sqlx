// -----------------------------------------------------------------------
// <copyright file="IdentifierQuotingPropertyTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq;
using FsCheck;
using FsCheck.Xunit;
using Xunit;
using GenSqlDefine = Sqlx.Generator.SqlDefine;

namespace Sqlx.Tests.Dialects.PropertyTests;

/// <summary>
/// Property-based tests for identifier quoting across all database dialects.
/// **Feature: sql-semantic-tdd-validation, Property 1: Identifier Quoting Consistency**
/// **Validates: Requirements 1.1, 1.2, 1.3, 1.4, 1.5**
/// </summary>
public class IdentifierQuotingPropertyTests
{
    /// <summary>
    /// **Property 1: Identifier Quoting Consistency**
    /// *For any* valid identifier name and *for any* database dialect, 
    /// WrapColumn SHALL return the identifier wrapped with dialect-specific quote characters.
    /// 
    /// MySQL: backticks (`)
    /// PostgreSQL: double quotes (")
    /// SQL Server: square brackets ([])
    /// SQLite: square brackets ([])
    /// Oracle: double quotes (")
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(IdentifierArbitraries) })]
    public Property WrapColumn_ForAnyValidIdentifier_ShouldWrapWithDialectSpecificQuotes(
        string identifier, DialectWithConfig dialectConfig)
    {
        // Skip empty identifiers (WrapColumn returns empty for empty input)
        if (string.IsNullOrEmpty(identifier))
            return true.ToProperty();

        var dialect = dialectConfig.Dialect;
        var config = dialectConfig.Config;

        // Act
        var result = dialect.WrapColumn(identifier);

        // Assert
        return (result.StartsWith(config.ExpectedIdentifierQuoteLeft) &&
                result.EndsWith(config.ExpectedIdentifierQuoteRight) &&
                result.Contains(identifier))
            .Label($"Dialect: {config.DialectName}, Identifier: '{identifier}', Result: '{result}', " +
                   $"Expected quotes: {config.ExpectedIdentifierQuoteLeft}...{config.ExpectedIdentifierQuoteRight}");
    }

    /// <summary>
    /// Property: WrapColumn should preserve the original identifier inside the quotes.
    /// *For any* valid identifier, the wrapped result should contain the original identifier unchanged.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(IdentifierArbitraries) })]
    public Property WrapColumn_ShouldPreserveIdentifierContent(string identifier, DialectWithConfig dialectConfig)
    {
        if (string.IsNullOrEmpty(identifier))
            return true.ToProperty();

        var dialect = dialectConfig.Dialect;
        var config = dialectConfig.Config;

        // Act
        var result = dialect.WrapColumn(identifier);

        // Extract the content between quotes
        var expectedContent = result.Substring(
            config.ExpectedIdentifierQuoteLeft.Length,
            result.Length - config.ExpectedIdentifierQuoteLeft.Length - config.ExpectedIdentifierQuoteRight.Length);

        // Assert
        return (expectedContent == identifier)
            .Label($"Dialect: {config.DialectName}, Original: '{identifier}', Extracted: '{expectedContent}'");
    }

    /// <summary>
    /// Property: WrapColumn should return empty string for null or empty input.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(IdentifierArbitraries) })]
    public Property WrapColumn_ForEmptyInput_ShouldReturnEmpty(DialectWithConfig dialectConfig)
    {
        var dialect = dialectConfig.Dialect;

        // Act
        var resultEmpty = dialect.WrapColumn("");
        var resultNull = dialect.WrapColumn(null!);

        // Assert
        return (string.IsNullOrEmpty(resultEmpty) && string.IsNullOrEmpty(resultNull))
            .Label($"Dialect: {dialectConfig.Config.DialectName}, Empty result: '{resultEmpty}', Null result: '{resultNull}'");
    }

    /// <summary>
    /// Property: MySQL should use backticks for identifier quoting.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(IdentifierArbitraries) })]
    public Property MySQL_WrapColumn_ShouldUseBackticks(string identifier)
    {
        if (string.IsNullOrEmpty(identifier))
            return true.ToProperty();

        // Act
        var result = GenSqlDefine.MySql.WrapColumn(identifier);

        // Assert
        return (result.StartsWith("`") && result.EndsWith("`"))
            .Label($"MySQL identifier: '{identifier}', Result: '{result}'");
    }

    /// <summary>
    /// Property: PostgreSQL should use double quotes for identifier quoting.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(IdentifierArbitraries) })]
    public Property PostgreSQL_WrapColumn_ShouldUseDoubleQuotes(string identifier)
    {
        if (string.IsNullOrEmpty(identifier))
            return true.ToProperty();

        // Act
        var result = GenSqlDefine.PostgreSql.WrapColumn(identifier);

        // Assert
        return (result.StartsWith("\"") && result.EndsWith("\""))
            .Label($"PostgreSQL identifier: '{identifier}', Result: '{result}'");
    }

    /// <summary>
    /// Property: SQL Server should use square brackets for identifier quoting.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(IdentifierArbitraries) })]
    public Property SqlServer_WrapColumn_ShouldUseSquareBrackets(string identifier)
    {
        if (string.IsNullOrEmpty(identifier))
            return true.ToProperty();

        // Act
        var result = GenSqlDefine.SqlServer.WrapColumn(identifier);

        // Assert
        return (result.StartsWith("[") && result.EndsWith("]"))
            .Label($"SQL Server identifier: '{identifier}', Result: '{result}'");
    }

    /// <summary>
    /// Property: SQLite should use square brackets for identifier quoting.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(IdentifierArbitraries) })]
    public Property SQLite_WrapColumn_ShouldUseSquareBrackets(string identifier)
    {
        if (string.IsNullOrEmpty(identifier))
            return true.ToProperty();

        // Act
        var result = GenSqlDefine.SQLite.WrapColumn(identifier);

        // Assert
        return (result.StartsWith("[") && result.EndsWith("]"))
            .Label($"SQLite identifier: '{identifier}', Result: '{result}'");
    }

    /// <summary>
    /// Property: Oracle should use double quotes for identifier quoting.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(IdentifierArbitraries) })]
    public Property Oracle_WrapColumn_ShouldUseDoubleQuotes(string identifier)
    {
        if (string.IsNullOrEmpty(identifier))
            return true.ToProperty();

        // Act
        var result = GenSqlDefine.Oracle.WrapColumn(identifier);

        // Assert
        return (result.StartsWith("\"") && result.EndsWith("\""))
            .Label($"Oracle identifier: '{identifier}', Result: '{result}'");
    }
}

/// <summary>
/// Wrapper class to pair a dialect with its test configuration.
/// </summary>
public class DialectWithConfig
{
    public GenSqlDefine Dialect { get; }
    public DialectTestConfig Config { get; }

    public DialectWithConfig(GenSqlDefine dialect, DialectTestConfig config)
    {
        Dialect = dialect;
        Config = config;
    }

    public override string ToString() => Config.DialectName;
}

/// <summary>
/// Custom arbitraries for identifier quoting property tests.
/// </summary>
public static class IdentifierArbitraries
{
    private static readonly char[] FirstChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
    private static readonly char[] RestChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_".ToCharArray();

    /// <summary>
    /// Generates valid SQL identifiers (letters, numbers, underscore, starting with letter).
    /// </summary>
    public static Arbitrary<string> String()
    {
        var firstChar = Gen.Elements(FirstChars);
        var restChars = Gen.Elements(RestChars);

        var gen = from size in Gen.Choose(1, 30)
                  from first in firstChar
                  from rest in Gen.ArrayOf(size - 1, restChars)
                  select first + new string(rest);

        return gen.ToArbitrary();
    }

    /// <summary>
    /// Generates dialect with its corresponding test configuration.
    /// </summary>
    public static Arbitrary<DialectWithConfig> DialectWithConfig()
    {
        return Gen.Elements(
            new DialectWithConfig(GenSqlDefine.MySql, PropertyTests.DialectTestConfig.MySql),
            new DialectWithConfig(GenSqlDefine.PostgreSql, PropertyTests.DialectTestConfig.PostgreSql),
            new DialectWithConfig(GenSqlDefine.SqlServer, PropertyTests.DialectTestConfig.SqlServer),
            new DialectWithConfig(GenSqlDefine.SQLite, PropertyTests.DialectTestConfig.SQLite),
            new DialectWithConfig(GenSqlDefine.Oracle, PropertyTests.DialectTestConfig.Oracle)
        ).ToArbitrary();
    }
}
