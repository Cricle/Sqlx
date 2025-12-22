// -----------------------------------------------------------------------
// <copyright file="ParameterPrefixPropertyTests.cs" company="Cricle">
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
/// Property-based tests for parameter prefix consistency across all database dialects.
/// **Feature: sql-semantic-tdd-validation, Property 2: Parameter Prefix Consistency**
/// **Validates: Requirements 2.1, 2.2, 2.3, 2.4, 2.5**
/// </summary>
public class ParameterPrefixPropertyTests
{
    /// <summary>
    /// **Property 2: Parameter Prefix Consistency**
    /// *For any* parameter name and *for any* database dialect, 
    /// generated SQL SHALL use the correct parameter prefix.
    /// 
    /// MySQL: @
    /// PostgreSQL: $ (or @)
    /// SQL Server: @
    /// SQLite: @
    /// Oracle: :
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(ParameterArbitraries) })]
    public Property ParameterPrefix_ForAnyParameterName_ShouldUseDialectSpecificPrefix(
        string paramName, DialectWithConfig dialectConfig)
    {
        // Skip empty parameter names
        if (string.IsNullOrEmpty(paramName))
            return true.ToProperty();

        var dialect = dialectConfig.Dialect;
        var config = dialectConfig.Config;

        // Act - Create parameter using dialect's prefix
        var result = dialect.ParameterPrefix + paramName;

        // Assert
        return (result.StartsWith(config.ExpectedParameterPrefix) &&
                result.EndsWith(paramName) &&
                result.Length == config.ExpectedParameterPrefix.Length + paramName.Length)
            .Label($"Dialect: {config.DialectName}, ParamName: '{paramName}', Result: '{result}', " +
                   $"Expected prefix: '{config.ExpectedParameterPrefix}'");
    }

    /// <summary>
    /// Property: Parameter prefix should be non-empty for all dialects.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(ParameterArbitraries) })]
    public Property ParameterPrefix_ForAllDialects_ShouldBeNonEmpty(DialectWithConfig dialectConfig)
    {
        var dialect = dialectConfig.Dialect;

        // Assert
        return (!string.IsNullOrEmpty(dialect.ParameterPrefix))
            .Label($"Dialect: {dialectConfig.Config.DialectName}, Prefix: '{dialect.ParameterPrefix}'");
    }

    /// <summary>
    /// Property: Parameter prefix should match expected configuration.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(ParameterArbitraries) })]
    public Property ParameterPrefix_ShouldMatchExpectedConfiguration(DialectWithConfig dialectConfig)
    {
        var dialect = dialectConfig.Dialect;
        var config = dialectConfig.Config;

        // Assert
        return (dialect.ParameterPrefix == config.ExpectedParameterPrefix)
            .Label($"Dialect: {config.DialectName}, Actual: '{dialect.ParameterPrefix}', Expected: '{config.ExpectedParameterPrefix}'");
    }

    /// <summary>
    /// Property: MySQL should use @ as parameter prefix.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(ParameterArbitraries) })]
    public Property MySQL_ParameterPrefix_ShouldUseAtSign(string paramName)
    {
        if (string.IsNullOrEmpty(paramName))
            return true.ToProperty();

        // Act
        var result = GenSqlDefine.MySql.ParameterPrefix + paramName;

        // Assert
        return (result.StartsWith("@"))
            .Label($"MySQL parameter: '{paramName}', Result: '{result}'");
    }

    /// <summary>
    /// Property: PostgreSQL should use $ as parameter prefix.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(ParameterArbitraries) })]
    public Property PostgreSQL_ParameterPrefix_ShouldUseDollarSign(string paramName)
    {
        if (string.IsNullOrEmpty(paramName))
            return true.ToProperty();

        // Act
        var result = GenSqlDefine.PostgreSql.ParameterPrefix + paramName;

        // Assert
        return (result.StartsWith("$"))
            .Label($"PostgreSQL parameter: '{paramName}', Result: '{result}'");
    }

    /// <summary>
    /// Property: SQL Server should use @ as parameter prefix.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(ParameterArbitraries) })]
    public Property SqlServer_ParameterPrefix_ShouldUseAtSign(string paramName)
    {
        if (string.IsNullOrEmpty(paramName))
            return true.ToProperty();

        // Act
        var result = GenSqlDefine.SqlServer.ParameterPrefix + paramName;

        // Assert
        return (result.StartsWith("@"))
            .Label($"SQL Server parameter: '{paramName}', Result: '{result}'");
    }

    /// <summary>
    /// Property: SQLite should use @ as parameter prefix.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(ParameterArbitraries) })]
    public Property SQLite_ParameterPrefix_ShouldUseAtSign(string paramName)
    {
        if (string.IsNullOrEmpty(paramName))
            return true.ToProperty();

        // Act
        var result = GenSqlDefine.SQLite.ParameterPrefix + paramName;

        // Assert
        return (result.StartsWith("@"))
            .Label($"SQLite parameter: '{paramName}', Result: '{result}'");
    }

    /// <summary>
    /// Property: Oracle should use : as parameter prefix.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(ParameterArbitraries) })]
    public Property Oracle_ParameterPrefix_ShouldUseColon(string paramName)
    {
        if (string.IsNullOrEmpty(paramName))
            return true.ToProperty();

        // Act
        var result = GenSqlDefine.Oracle.ParameterPrefix + paramName;

        // Assert
        return (result.StartsWith(":"))
            .Label($"Oracle parameter: '{paramName}', Result: '{result}'");
    }

    /// <summary>
    /// Property: Parameter name should be preserved after prefix.
    /// *For any* parameter name, the generated parameter should contain the original name unchanged.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(ParameterArbitraries) })]
    public Property ParameterPrefix_ShouldPreserveParameterName(string paramName, DialectWithConfig dialectConfig)
    {
        if (string.IsNullOrEmpty(paramName))
            return true.ToProperty();

        var dialect = dialectConfig.Dialect;
        var config = dialectConfig.Config;

        // Act
        var result = dialect.ParameterPrefix + paramName;

        // Extract the parameter name after prefix
        var extractedName = result.Substring(config.ExpectedParameterPrefix.Length);

        // Assert
        return (extractedName == paramName)
            .Label($"Dialect: {config.DialectName}, Original: '{paramName}', Extracted: '{extractedName}'");
    }

    /// <summary>
    /// Property: Dialects with same prefix should be grouped correctly.
    /// MySQL, SQL Server, and SQLite all use @ prefix.
    /// </summary>
    [Fact]
    public void AtPrefixDialects_ShouldAllUseAtSign()
    {
        var atPrefixDialects = new[]
        {
            GenSqlDefine.MySql,
            GenSqlDefine.SqlServer,
            GenSqlDefine.SQLite
        };

        foreach (var dialect in atPrefixDialects)
        {
            Assert.Equal("@", dialect.ParameterPrefix);
        }
    }

    /// <summary>
    /// Property: PostgreSQL should use $ prefix.
    /// </summary>
    [Fact]
    public void PostgreSQL_ShouldUseDollarPrefix()
    {
        Assert.Equal("$", GenSqlDefine.PostgreSql.ParameterPrefix);
    }

    /// <summary>
    /// Property: Oracle should use : prefix.
    /// </summary>
    [Fact]
    public void Oracle_ShouldUseColonPrefix()
    {
        Assert.Equal(":", GenSqlDefine.Oracle.ParameterPrefix);
    }
}

/// <summary>
/// Custom arbitraries for parameter prefix property tests.
/// </summary>
public static class ParameterArbitraries
{
    private static readonly char[] FirstChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
    private static readonly char[] RestChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_".ToCharArray();

    /// <summary>
    /// Generates valid SQL parameter names (letters, numbers, underscore, starting with letter).
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
