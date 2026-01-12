// -----------------------------------------------------------------------
// <copyright file="AdvancedSqlPropertyTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using FsCheck;
using FsCheck.Xunit;
using Sqlx.Generator;
using Xunit;
using GenSqlDefine = Sqlx.Generator.SqlDefine;

namespace Sqlx.Tests.Dialects.PropertyTests;

/// <summary>
/// Property-based tests for Advanced SQL features across all database dialects.
/// Property 28: Aggregate Function Syntax.
/// </summary>
public class AdvancedSqlPropertyTests
{
    #region Property 28: Aggregate Function Syntax

    /// <summary>
    /// Property 28: Aggregate Function Syntax.
    /// For any aggregate function (COUNT, SUM, AVG, MAX, MIN) and for any database dialect,
    /// generated SQL SHALL be syntactically correct.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(AggregateArbitraries) })]
    public Property ProcessAggregateFunction_ForAnyFunctionAndDialect_ShouldReturnSyntacticallyCorrectSql(
        AggregateFunction function,
        DialectWithConfig dialectConfig,
        string columnName)
    {
        if (string.IsNullOrEmpty(columnName))
        {
            return true.ToProperty();
        }

        var dialect = dialectConfig.Dialect;
        var functionName = function.ToString().ToUpperInvariant();

        var result = SqlTemplateEngineExtensions.MultiDatabasePlaceholderSupport
            .ProcessAggregateFunction(functionName, columnName, "", dialect);

        var isCorrect = result.StartsWith($"{functionName}(") && result.EndsWith(")");
        var containsColumn = result.Contains(columnName, StringComparison.OrdinalIgnoreCase) || result.Contains("*");

        return (isCorrect && containsColumn)
            .Label($"Function: {functionName}, Dialect: {dialectConfig.Config.DialectName}, " +
                   $"Column: '{columnName}', Result: '{result}'");
    }

    /// <summary>
    /// Property: COUNT(*) should be valid for all dialects.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(AggregateArbitraries) })]
    public Property ProcessAggregateFunction_CountAll_ShouldReturnCountStar(DialectWithConfig dialectConfig)
    {
        var dialect = dialectConfig.Dialect;

        var result = SqlTemplateEngineExtensions.MultiDatabasePlaceholderSupport
            .ProcessAggregateFunction("COUNT", "all", "", dialect);

        return (result == "COUNT(*)")
            .Label($"Dialect: {dialectConfig.Config.DialectName}, Result: '{result}'");
    }

    /// <summary>
    /// Property: SUM with column should wrap column correctly.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(AggregateArbitraries) })]
    public Property ProcessAggregateFunction_SumWithColumn_ShouldWrapColumn(
        DialectWithConfig dialectConfig,
        string columnName)
    {
        if (string.IsNullOrEmpty(columnName))
        {
            return true.ToProperty();
        }

        var dialect = dialectConfig.Dialect;
        var config = dialectConfig.Config;

        var result = SqlTemplateEngineExtensions.MultiDatabasePlaceholderSupport
            .ProcessAggregateFunction("SUM", columnName, "", dialect);

        var isCorrect = result.StartsWith("SUM(") && result.EndsWith(")");

        return isCorrect.Label($"Dialect: {config.DialectName}, Column: '{columnName}', Result: '{result}'");
    }

    /// <summary>
    /// Property: AVG with column should be syntactically correct.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(AggregateArbitraries) })]
    public Property ProcessAggregateFunction_AvgWithColumn_ShouldBeSyntacticallyCorrect(
        DialectWithConfig dialectConfig,
        string columnName)
    {
        if (string.IsNullOrEmpty(columnName))
        {
            return true.ToProperty();
        }

        var dialect = dialectConfig.Dialect;

        var result = SqlTemplateEngineExtensions.MultiDatabasePlaceholderSupport
            .ProcessAggregateFunction("AVG", columnName, "", dialect);

        var isCorrect = result.StartsWith("AVG(") && result.EndsWith(")");

        return isCorrect.Label($"Dialect: {dialectConfig.Config.DialectName}, " +
                               $"Column: '{columnName}', Result: '{result}'");
    }

    /// <summary>
    /// Property: MAX with column should be syntactically correct.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(AggregateArbitraries) })]
    public Property ProcessAggregateFunction_MaxWithColumn_ShouldBeSyntacticallyCorrect(
        DialectWithConfig dialectConfig,
        string columnName)
    {
        if (string.IsNullOrEmpty(columnName))
        {
            return true.ToProperty();
        }

        var dialect = dialectConfig.Dialect;

        var result = SqlTemplateEngineExtensions.MultiDatabasePlaceholderSupport
            .ProcessAggregateFunction("MAX", columnName, "", dialect);

        var isCorrect = result.StartsWith("MAX(") && result.EndsWith(")");

        return isCorrect.Label($"Dialect: {dialectConfig.Config.DialectName}, " +
                               $"Column: '{columnName}', Result: '{result}'");
    }

    /// <summary>
    /// Property: MIN with column should be syntactically correct.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(AggregateArbitraries) })]
    public Property ProcessAggregateFunction_MinWithColumn_ShouldBeSyntacticallyCorrect(
        DialectWithConfig dialectConfig,
        string columnName)
    {
        if (string.IsNullOrEmpty(columnName))
        {
            return true.ToProperty();
        }

        var dialect = dialectConfig.Dialect;

        var result = SqlTemplateEngineExtensions.MultiDatabasePlaceholderSupport
            .ProcessAggregateFunction("MIN", columnName, "", dialect);

        var isCorrect = result.StartsWith("MIN(") && result.EndsWith(")");

        return isCorrect.Label($"Dialect: {dialectConfig.Config.DialectName}, " +
                               $"Column: '{columnName}', Result: '{result}'");
    }

    /// <summary>
    /// Property: Aggregate functions with DISTINCT should be syntactically correct.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(AggregateArbitraries) })]
    public Property ProcessAggregateFunction_WithDistinct_ShouldBeSyntacticallyCorrect(
        AggregateFunction function,
        DialectWithConfig dialectConfig,
        string columnName)
    {
        if (string.IsNullOrEmpty(columnName))
        {
            return true.ToProperty();
        }

        var dialect = dialectConfig.Dialect;
        var functionName = function.ToString().ToUpperInvariant();

        var result = SqlTemplateEngineExtensions.MultiDatabasePlaceholderSupport
            .ProcessAggregateFunction(functionName, columnName, "distinct=true", dialect);

        var isCorrect = result.StartsWith($"{functionName}(DISTINCT") && result.EndsWith(")");

        return isCorrect.Label($"Function: {functionName}, Dialect: {dialectConfig.Config.DialectName}, " +
                               $"Column: '{columnName}', Result: '{result}'");
    }

    /// <summary>
    /// Unit test: COUNT(*) for all dialects.
    /// </summary>
    [Fact]
    public void AllDialects_CountAll_ShouldReturnCountStar()
    {
        var dialects = SqlxArbitraries.AllDialects;

        foreach (var dialect in dialects)
        {
            var result = SqlTemplateEngineExtensions.MultiDatabasePlaceholderSupport
                .ProcessAggregateFunction("COUNT", "all", "", dialect);

            Assert.Equal("COUNT(*)", result);
        }
    }

    /// <summary>
    /// Unit test: SUM with specific column.
    /// </summary>
    [Theory]
    [InlineData("amount")]
    [InlineData("price")]
    [InlineData("quantity")]
    public void AllDialects_SumWithColumn_ShouldReturnSumFunction(string column)
    {
        var dialects = SqlxArbitraries.AllDialects;

        foreach (var dialect in dialects)
        {
            var result = SqlTemplateEngineExtensions.MultiDatabasePlaceholderSupport
                .ProcessAggregateFunction("SUM", column, "", dialect);

            Assert.StartsWith("SUM(", result);
            Assert.EndsWith(")", result);
        }
    }

    #endregion
}

/// <summary>
/// Enum representing aggregate functions for property testing.
/// </summary>
public enum AggregateFunction
{
    Count,
    Sum,
    Avg,
    Max,
    Min
}

/// <summary>
/// FsCheck arbitrary generators for aggregate function property testing.
/// </summary>
public static class AggregateArbitraries
{
    /// <summary>
    /// Generates a random aggregate function.
    /// </summary>
    public static Arbitrary<AggregateFunction> AggregateFunction()
    {
        return Gen.Elements(
            PropertyTests.AggregateFunction.Count,
            PropertyTests.AggregateFunction.Sum,
            PropertyTests.AggregateFunction.Avg,
            PropertyTests.AggregateFunction.Max,
            PropertyTests.AggregateFunction.Min
        ).ToArbitrary();
    }

    /// <summary>
    /// Generates a dialect with configuration.
    /// </summary>
    public static Arbitrary<DialectWithConfig> DialectWithConfig()
    {
        return IdentifierArbitraries.DialectWithConfig();
    }

    /// <summary>
    /// Generates a valid column name.
    /// </summary>
    public static Arbitrary<string> String()
    {
        return IdentifierArbitraries.String();
    }
}
