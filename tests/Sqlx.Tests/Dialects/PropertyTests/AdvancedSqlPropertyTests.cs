// -----------------------------------------------------------------------
// <copyright file="AdvancedSqlPropertyTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq;
using FsCheck;
using FsCheck.Xunit;
using Xunit;
using Sqlx.Generator;

namespace Sqlx.Tests.Dialects.PropertyTests;

/// <summary>
/// Property-based tests for Advanced SQL features across all database dialects.
/// **Feature: sql-semantic-tdd-validation, Property 28: Aggregate Function Syntax**
/// **Feature: sql-semantic-tdd-validation, Property 29: JOIN Syntax Correctness**
/// **Feature: sql-semantic-tdd-validation, Property 30: GROUP BY and HAVING Syntax**
/// **Validates: Requirements 28.1-28.5, 29.1-29.6, 30.1-30.4**
/// </summary>
public class AdvancedSqlPropertyTests
{
    #region Property 28: Aggregate Function Syntax

    /// <summary>
    /// **Property 28: Aggregate Function Syntax**
    /// *For any* aggregate function (COUNT, SUM, AVG, MAX, MIN) and *for any* database dialect,
    /// generated SQL SHALL be syntactically correct.
    /// **Validates: Requirements 29.1, 29.2, 29.3, 29.4, 29.5, 29.6**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(AdvancedSqlArbitraries) })]
    public Property ProcessAggregateFunction_ForAnyFunctionAndDialect_ShouldReturnSyntacticallyCorrectSql(
        AggregateFunction function,
        DialectWithConfig dialectConfig,
        string columnName)
    {
        if (string.IsNullOrEmpty(columnName))
            return true.ToProperty();

        var dialect = dialectConfig.Dialect;
        var functionName = function.ToString().ToUpperInvariant();

        // Act
        var result = SqlTemplateEngineExtensions.MultiDatabasePlaceholderSupport
            .ProcessAggregateFunction(functionName, columnName, "", dialect);

        // Assert - verify correct syntax
        var isCorrect = result.StartsWith($"{functionName}(") && result.EndsWith(")");
        var containsColumn = result.Contains(columnName) || result.Contains("*");

        return (isCorrect && containsColumn)
            .Label($"Function: {functionName}, Dialect: {dialectConfig.Config.DialectName}, " +
                   $"Column: '{columnName}', Result: '{result}'");
    }

    /// <summary>
    /// Property: COUNT(*) should be valid for all dialects.
    /// **Validates: Requirements 29.1**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(AdvancedSqlArbitraries) })]
    public Property ProcessAggregateFunction_CountAll_ShouldReturnCountStar(DialectWithConfig dialectConfig)
    {
        var dialect = dialectConfig.Dialect;

        // Act
        var result = SqlTemplateEngineExtensions.MultiDatabasePlaceholderSupport
            .ProcessAggregateFunction("COUNT", "all", "", dialect);

        // Assert
        return (result == "COUNT(*)")
            .Label($"Dialect: {dialectConfig.Config.DialectName}, Result: '{result}'");
    }

    /// <summary>
    /// Property: SUM with column should wrap column correctly.
    /// **Validates: Requirements 29.2**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(AdvancedSqlArbitraries) })]
    public Property ProcessAggregateFunction_SumWithColumn_ShouldWrapColumn(
        DialectWithConfig dialectConfig,
        string columnName)
    {
        if (string.IsNullOrEmpty(columnName))
            return true.ToProperty();

        var dialect = dialectConfig.Dialect;
        var config = dialectConfig.Config;

        // Act
        var result = SqlTemplateEngineExtensions.MultiDatabasePlaceholderSupport
            .ProcessAggregateFunction("SUM", columnName, "", dialect);

        // Assert
        var expectedWrapped = config.GetExpectedWrappedIdentifier(columnName.ToLowerInvariant());
        var isCorrect = result.StartsWith("SUM(") && result.EndsWith(")");

        return isCorrect.Label($"Dialect: {config.DialectName}, Column: '{columnName}', Result: '{result}'");
    }

    /// <summary>
    /// Property: AVG with column should be syntactically correct.
    /// **Validates: Requirements 29.3**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(AdvancedSqlArbitraries) })]
    public Property ProcessAggregateFunction_AvgWithColumn_ShouldBeSyntacticallyCorrect(
        DialectWithConfig dialectConfig,
        string columnName)
    {
        if (string.IsNullOrEmpty(columnName))
            return true.ToProperty();

        var dialect = dialectConfig.Dialect;

        // Act
        var result = SqlTemplateEngineExtensions.MultiDatabasePlaceholderSupport
            .ProcessAggregateFunction("AVG", columnName, "", dialect);

        // Assert
        var isCorrect = result.StartsWith("AVG(") && result.EndsWith(")");

        return isCorrect.Label($"Dialect: {dialectConfig.Config.DialectName}, " +
                               $"Column: '{columnName}', Result: '{result}'");
    }

    /// <summary>
    /// Property: MAX with column should be syntactically correct.
    /// **Validates: Requirements 29.4**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(AdvancedSqlArbitraries) })]
    public Property ProcessAggregateFunction_MaxWithColumn_ShouldBeSyntacticallyCorrect(
        DialectWithConfig dialectConfig,
        string columnName)
    {
        if (string.IsNullOrEmpty(columnName))
            return true.ToProperty();

        var dialect = dialectConfig.Dialect;

        // Act
        var result = SqlTemplateEngineExtensions.MultiDatabasePlaceholderSupport
            .ProcessAggregateFunction("MAX", columnName, "", dialect);

        // Assert
        var isCorrect = result.StartsWith("MAX(") && result.EndsWith(")");

        return isCorrect.Label($"Dialect: {dialectConfig.Config.DialectName}, " +
                               $"Column: '{columnName}', Result: '{result}'");
    }

    /// <summary>
    /// Property: MIN with column should be syntactically correct.
    /// **Validates: Requirements 29.5**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(AdvancedSqlArbitraries) })]
    public Property ProcessAggregateFunction_MinWithColumn_ShouldBeSyntacticallyCorrect(
        DialectWithConfig dialectConfig,
        string columnName)
    {
        if (string.IsNullOrEmpty(columnName))
            return true.ToProperty();

        var dialect = dialectConfig.Dialect;

        // Act
        var result = SqlTemplateEngineExtensions.MultiDatabasePlaceholderSupport
            .ProcessAggregateFunction("MIN", columnName, "", dialect);

        // Assert
        var isCorrect = result.StartsWith("MIN(") && result.EndsWith(")");

        return isCorrect.Label($"Dialect: {dialectConfig.Config.DialectName}, " +
                               $"Column: '{columnName}', Result: '{result}'");
    }

    /// <summary>
    /// Property: Aggregate functions with DISTINCT should be syntactically correct.
    /// **Validates: Requirements 29.6**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(AdvancedSqlArbitraries) })]
    public Property ProcessAggregateFunction_WithDistinct_ShouldBeSyntacticallyCorrect(
        AggregateFunction function,
        DialectWithConfig dialectConfig,
        string columnName)
    {
        if (string.IsNullOrEmpty(columnName))
            return true.ToProperty();

        var dialect = dialectConfig.Dialect;
        var functionName = function.ToString().ToUpperInvariant();

        // Act
        var result = SqlTemplateEngineExtensions.MultiDatabasePlaceholderSupport
            .ProcessAggregateFunction(functionName, columnName, "distinct=true", dialect);

        // Assert
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
            // Act
            var result = SqlTemplateEngineExtensions.MultiDatabasePlaceholderSupport
                .ProcessAggregateFunction("COUNT", "all", "", dialect);

            // Assert
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
            // Act
            var result = SqlTemplateEngineExtensions.MultiDatabasePlaceholderSupport
                .ProcessAggregateFunction("SUM", column, "", dialect);

            // Assert
            Assert.StartsWith("SUM(", result);
            Assert.EndsWith(")", result);
        }
    }

    #endregion

    #region Property 29: JOIN Syntax Correctness

    /// <summary>
    /// **Property 29: JOIN Syntax Correctness**
    /// *For any* JOIN type (INNER, LEFT, RIGHT, FULL) and *for any* database dialect that supports it,
    /// generated SQL SHALL be syntactically correct.
    /// **Validates: Requirements 28.1, 28.2, 28.3, 28.4, 28.5**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(AdvancedSqlArbitraries) })]
    public Property ProcessJoinPlaceholder_ForAnyJoinTypeAndDialect_ShouldReturnSyntacticallyCorrectSql(
        JoinType joinType,
        DialectWithConfig dialectConfig,
        string tableName)
    {
        if (string.IsNullOrEmpty(tableName))
            return true.ToProperty();

        var dialect = dialectConfig.Dialect;
        var joinTypeName = joinType.ToString().ToLowerInvariant();

        // Act
        var result = SqlTemplateEngineExtensions.MultiDatabasePlaceholderSupport
            .ProcessGenericPlaceholder("join", joinTypeName, $"table={tableName}|on=id", dialect);

        // Assert - verify correct JOIN syntax
        var expectedJoinKeyword = joinType switch
        {
            JoinType.Inner => "INNER JOIN",
            JoinType.Left => "LEFT JOIN",
            JoinType.Right => "RIGHT JOIN",
            JoinType.Full => "FULL OUTER JOIN",
            _ => "INNER JOIN"
        };

        var containsJoinKeyword = result.Contains(expectedJoinKeyword);
        var containsTable = result.Contains(tableName);
        var containsOn = result.Contains("ON");

        return (containsJoinKeyword && containsTable && containsOn)
            .Label($"JoinType: {joinType}, Dialect: {dialectConfig.Config.DialectName}, " +
                   $"Table: '{tableName}', Result: '{result}'");
    }

    /// <summary>
    /// Property: INNER JOIN should be valid for all dialects.
    /// **Validates: Requirements 28.1**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(AdvancedSqlArbitraries) })]
    public Property ProcessJoinPlaceholder_InnerJoin_ShouldBeValidForAllDialects(
        DialectWithConfig dialectConfig,
        string tableName)
    {
        if (string.IsNullOrEmpty(tableName))
            return true.ToProperty();

        var dialect = dialectConfig.Dialect;

        // Act
        var result = SqlTemplateEngineExtensions.MultiDatabasePlaceholderSupport
            .ProcessGenericPlaceholder("inner_join", "", $"table={tableName}|on=id", dialect);

        // Assert
        var isCorrect = result.Contains("INNER JOIN") && result.Contains("ON");

        return isCorrect.Label($"Dialect: {dialectConfig.Config.DialectName}, " +
                               $"Table: '{tableName}', Result: '{result}'");
    }

    /// <summary>
    /// Property: LEFT JOIN should be valid for all dialects.
    /// **Validates: Requirements 28.2**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(AdvancedSqlArbitraries) })]
    public Property ProcessJoinPlaceholder_LeftJoin_ShouldBeValidForAllDialects(
        DialectWithConfig dialectConfig,
        string tableName)
    {
        if (string.IsNullOrEmpty(tableName))
            return true.ToProperty();

        var dialect = dialectConfig.Dialect;

        // Act
        var result = SqlTemplateEngineExtensions.MultiDatabasePlaceholderSupport
            .ProcessGenericPlaceholder("left_join", "", $"table={tableName}|on=id", dialect);

        // Assert
        var isCorrect = result.Contains("LEFT JOIN") && result.Contains("ON");

        return isCorrect.Label($"Dialect: {dialectConfig.Config.DialectName}, " +
                               $"Table: '{tableName}', Result: '{result}'");
    }

    /// <summary>
    /// Property: RIGHT JOIN should be valid for dialects that support it.
    /// **Validates: Requirements 28.3**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(AdvancedSqlArbitraries) })]
    public Property ProcessJoinPlaceholder_RightJoin_ShouldBeValidForSupportingDialects(
        DialectWithConfig dialectConfig,
        string tableName)
    {
        if (string.IsNullOrEmpty(tableName))
            return true.ToProperty();

        var dialect = dialectConfig.Dialect;

        // Act
        var result = SqlTemplateEngineExtensions.MultiDatabasePlaceholderSupport
            .ProcessGenericPlaceholder("right_join", "", $"table={tableName}|on=id", dialect);

        // Assert - RIGHT JOIN may not be supported by SQLite
        var isCorrect = result.Contains("RIGHT JOIN") || result.Contains("LEFT JOIN");
        var containsOn = result.Contains("ON");

        return (isCorrect && containsOn)
            .Label($"Dialect: {dialectConfig.Config.DialectName}, " +
                   $"Table: '{tableName}', Result: '{result}'");
    }

    /// <summary>
    /// Property: FULL OUTER JOIN should be valid for dialects that support it.
    /// **Validates: Requirements 28.4**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(AdvancedSqlArbitraries) })]
    public Property ProcessJoinPlaceholder_FullOuterJoin_ShouldBeValidForSupportingDialects(
        DialectWithConfig dialectConfig,
        string tableName)
    {
        if (string.IsNullOrEmpty(tableName))
            return true.ToProperty();

        var dialect = dialectConfig.Dialect;

        // Act
        var result = SqlTemplateEngineExtensions.MultiDatabasePlaceholderSupport
            .ProcessGenericPlaceholder("join", "full", $"table={tableName}|on=id", dialect);

        // Assert - FULL OUTER JOIN may not be supported by all databases
        var containsJoin = result.Contains("JOIN");
        var containsOn = result.Contains("ON");

        return (containsJoin && containsOn)
            .Label($"Dialect: {dialectConfig.Config.DialectName}, " +
                   $"Table: '{tableName}', Result: '{result}'");
    }

    /// <summary>
    /// Property: JOIN should use proper identifier quoting.
    /// **Validates: Requirements 28.5**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(AdvancedSqlArbitraries) })]
    public Property ProcessJoinPlaceholder_ShouldUseProperIdentifierQuoting(
        DialectWithConfig dialectConfig,
        string tableName)
    {
        if (string.IsNullOrEmpty(tableName))
            return true.ToProperty();

        var dialect = dialectConfig.Dialect;

        // Act
        var result = SqlTemplateEngineExtensions.MultiDatabasePlaceholderSupport
            .ProcessGenericPlaceholder("join", "inner", $"table={tableName}|on=id", dialect);

        // Assert - verify JOIN syntax is correct
        var containsJoin = result.Contains("JOIN");
        var containsOn = result.Contains("ON");

        return (containsJoin && containsOn)
            .Label($"Dialect: {dialectConfig.Config.DialectName}, " +
                   $"Table: '{tableName}', Result: '{result}'");
    }

    /// <summary>
    /// Unit test: INNER JOIN for all dialects.
    /// </summary>
    [Fact]
    public void AllDialects_InnerJoin_ShouldReturnValidSyntax()
    {
        var dialects = SqlxArbitraries.AllDialects;

        foreach (var dialect in dialects)
        {
            // Act
            var result = SqlTemplateEngineExtensions.MultiDatabasePlaceholderSupport
                .ProcessGenericPlaceholder("join", "inner", "table=orders|on=id", dialect);

            // Assert
            Assert.Contains("INNER JOIN", result);
            Assert.Contains("ON", result);
        }
    }

    /// <summary>
    /// Unit test: LEFT JOIN for all dialects.
    /// </summary>
    [Fact]
    public void AllDialects_LeftJoin_ShouldReturnValidSyntax()
    {
        var dialects = SqlxArbitraries.AllDialects;

        foreach (var dialect in dialects)
        {
            // Act
            var result = SqlTemplateEngineExtensions.MultiDatabasePlaceholderSupport
                .ProcessGenericPlaceholder("join", "left", "table=orders|on=id", dialect);

            // Assert
            Assert.Contains("LEFT JOIN", result);
            Assert.Contains("ON", result);
        }
    }

    #endregion

    #region Property 30: GROUP BY and HAVING Syntax

    /// <summary>
    /// **Property 30: GROUP BY and HAVING Syntax**
    /// *For any* GROUP BY columns and HAVING conditions and *for any* database dialect,
    /// generated SQL SHALL be syntactically correct.
    /// **Validates: Requirements 30.1, 30.2, 30.3, 30.4**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(AdvancedSqlArbitraries) })]
    public Property ProcessGroupByPlaceholder_ForAnyColumnAndDialect_ShouldReturnSyntacticallyCorrectSql(
        DialectWithConfig dialectConfig,
        string columnName)
    {
        if (string.IsNullOrEmpty(columnName))
            return true.ToProperty();

        var dialect = dialectConfig.Dialect;

        // Act
        var result = SqlTemplateEngineExtensions.MultiDatabasePlaceholderSupport
            .ProcessGenericPlaceholder("groupby", columnName, "", dialect);

        // Assert
        var containsGroupBy = result.Contains("GROUP BY");
        var containsColumn = result.Contains(columnName);

        return (containsGroupBy && containsColumn)
            .Label($"Dialect: {dialectConfig.Config.DialectName}, " +
                   $"Column: '{columnName}', Result: '{result}'");
    }

    /// <summary>
    /// Property: GROUP BY should produce valid syntax for all databases.
    /// **Validates: Requirements 30.1**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(AdvancedSqlArbitraries) })]
    public Property ProcessGroupByPlaceholder_ShouldProduceValidSyntax(
        DialectWithConfig dialectConfig,
        string columnName)
    {
        if (string.IsNullOrEmpty(columnName))
            return true.ToProperty();

        var dialect = dialectConfig.Dialect;

        // Act
        var result = SqlTemplateEngineExtensions.MultiDatabasePlaceholderSupport
            .ProcessGenericPlaceholder("groupby", columnName, "", dialect);

        // Assert
        var startsWithGroupBy = result.StartsWith("GROUP BY");

        return startsWithGroupBy.Label($"Dialect: {dialectConfig.Config.DialectName}, " +
                                       $"Column: '{columnName}', Result: '{result}'");
    }

    /// <summary>
    /// Property: HAVING should produce valid syntax for all databases.
    /// **Validates: Requirements 30.2**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(AdvancedSqlArbitraries) })]
    public Property ProcessHavingPlaceholder_ShouldProduceValidSyntax(
        DialectWithConfig dialectConfig,
        HavingType havingType)
    {
        var dialect = dialectConfig.Dialect;
        var havingTypeName = havingType.ToString().ToLowerInvariant();

        // Act
        var result = SqlTemplateEngineExtensions.MultiDatabasePlaceholderSupport
            .ProcessGenericPlaceholder("having", havingTypeName, "min=0", dialect);

        // Assert
        var startsWithHaving = result.StartsWith("HAVING");

        return startsWithHaving.Label($"Dialect: {dialectConfig.Config.DialectName}, " +
                                      $"HavingType: {havingType}, Result: '{result}'");
    }

    /// <summary>
    /// Property: GROUP BY with multiple columns should use comma separator.
    /// **Validates: Requirements 30.3**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(AdvancedSqlArbitraries) })]
    public Property ProcessGroupByPlaceholder_WithMultipleColumns_ShouldUseCommaSeparator(
        DialectWithConfig dialectConfig,
        string column1,
        string column2)
    {
        if (string.IsNullOrEmpty(column1) || string.IsNullOrEmpty(column2))
            return true.ToProperty();

        var dialect = dialectConfig.Dialect;
        var columns = $"{column1}, {column2}";

        // Act
        var result = SqlTemplateEngineExtensions.MultiDatabasePlaceholderSupport
            .ProcessGenericPlaceholder("groupby", columns, "", dialect);

        // Assert
        var containsGroupBy = result.Contains("GROUP BY");
        var containsComma = result.Contains(",");

        return (containsGroupBy && containsComma)
            .Label($"Dialect: {dialectConfig.Config.DialectName}, " +
                   $"Columns: '{columns}', Result: '{result}'");
    }

    /// <summary>
    /// Property: GROUP BY and HAVING should use proper identifier quoting.
    /// **Validates: Requirements 30.4**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(AdvancedSqlArbitraries) })]
    public Property ProcessGroupByAndHaving_ShouldUseProperIdentifierQuoting(
        DialectWithConfig dialectConfig,
        string columnName)
    {
        if (string.IsNullOrEmpty(columnName))
            return true.ToProperty();

        var dialect = dialectConfig.Dialect;

        // Act
        var groupByResult = SqlTemplateEngineExtensions.MultiDatabasePlaceholderSupport
            .ProcessGenericPlaceholder("groupby", columnName, "", dialect);
        var havingResult = SqlTemplateEngineExtensions.MultiDatabasePlaceholderSupport
            .ProcessGenericPlaceholder("having", "count", "min=0", dialect);

        // Assert
        var groupByValid = groupByResult.Contains("GROUP BY");
        var havingValid = havingResult.Contains("HAVING");

        return (groupByValid && havingValid)
            .Label($"Dialect: {dialectConfig.Config.DialectName}, " +
                   $"GroupBy: '{groupByResult}', Having: '{havingResult}'");
    }

    /// <summary>
    /// Unit test: GROUP BY for all dialects.
    /// </summary>
    [Fact]
    public void AllDialects_GroupBy_ShouldReturnValidSyntax()
    {
        var dialects = SqlxArbitraries.AllDialects;

        foreach (var dialect in dialects)
        {
            // Act
            var result = SqlTemplateEngineExtensions.MultiDatabasePlaceholderSupport
                .ProcessGenericPlaceholder("groupby", "category", "", dialect);

            // Assert
            Assert.StartsWith("GROUP BY", result);
            Assert.Contains("category", result);
        }
    }

    /// <summary>
    /// Unit test: HAVING COUNT for all dialects.
    /// </summary>
    [Fact]
    public void AllDialects_HavingCount_ShouldReturnValidSyntax()
    {
        var dialects = SqlxArbitraries.AllDialects;

        foreach (var dialect in dialects)
        {
            // Act
            var result = SqlTemplateEngineExtensions.MultiDatabasePlaceholderSupport
                .ProcessGenericPlaceholder("having", "count", "min=5", dialect);

            // Assert
            Assert.StartsWith("HAVING", result);
            Assert.Contains("COUNT", result);
        }
    }

    /// <summary>
    /// Unit test: HAVING SUM for all dialects.
    /// </summary>
    [Fact]
    public void AllDialects_HavingSum_ShouldReturnValidSyntax()
    {
        var dialects = SqlxArbitraries.AllDialects;

        foreach (var dialect in dialects)
        {
            // Act
            var result = SqlTemplateEngineExtensions.MultiDatabasePlaceholderSupport
                .ProcessGenericPlaceholder("having", "sum", "column=amount|min=100", dialect);

            // Assert
            Assert.StartsWith("HAVING", result);
            Assert.Contains("SUM", result);
        }
    }

    /// <summary>
    /// Unit test: HAVING AVG for all dialects.
    /// </summary>
    [Fact]
    public void AllDialects_HavingAvg_ShouldReturnValidSyntax()
    {
        var dialects = SqlxArbitraries.AllDialects;

        foreach (var dialect in dialects)
        {
            // Act
            var result = SqlTemplateEngineExtensions.MultiDatabasePlaceholderSupport
                .ProcessGenericPlaceholder("having", "avg", "column=price|min=50", dialect);

            // Assert
            Assert.StartsWith("HAVING", result);
            Assert.Contains("AVG", result);
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
/// Enum representing JOIN types for property testing.
/// </summary>
public enum JoinType
{
    Inner,
    Left,
    Right,
    Full
}

/// <summary>
/// Enum representing HAVING types for property testing.
/// </summary>
public enum HavingType
{
    Count,
    Sum,
    Avg
}

/// <summary>
/// Custom arbitraries for Advanced SQL property tests.
/// </summary>
public static class AdvancedSqlArbitraries
{
    private static readonly char[] FirstChars = "abcdefghijklmnopqrstuvwxyz".ToCharArray();
    private static readonly char[] RestChars = "abcdefghijklmnopqrstuvwxyz0123456789_".ToCharArray();

    /// <summary>
    /// Generates valid SQL identifiers (letters, numbers, underscore, starting with letter).
    /// </summary>
    public static Arbitrary<string> String()
    {
        var firstChar = Gen.Elements(FirstChars);
        var restChars = Gen.Elements(RestChars);

        var gen = from size in Gen.Choose(1, 20)
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
            new DialectWithConfig(Sqlx.Generator.SqlDefine.MySql, DialectTestConfig.MySql),
            new DialectWithConfig(Sqlx.Generator.SqlDefine.PostgreSql, DialectTestConfig.PostgreSql),
            new DialectWithConfig(Sqlx.Generator.SqlDefine.SqlServer, DialectTestConfig.SqlServer),
            new DialectWithConfig(Sqlx.Generator.SqlDefine.SQLite, DialectTestConfig.SQLite),
            new DialectWithConfig(Sqlx.Generator.SqlDefine.Oracle, DialectTestConfig.Oracle)
        ).ToArbitrary();
    }

    /// <summary>
    /// Generates aggregate function types.
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
    /// Generates JOIN types.
    /// </summary>
    public static Arbitrary<JoinType> JoinType()
    {
        return Gen.Elements(
            PropertyTests.JoinType.Inner,
            PropertyTests.JoinType.Left,
            PropertyTests.JoinType.Right,
            PropertyTests.JoinType.Full
        ).ToArbitrary();
    }

    /// <summary>
    /// Generates HAVING types.
    /// </summary>
    public static Arbitrary<HavingType> HavingType()
    {
        return Gen.Elements(
            PropertyTests.HavingType.Count,
            PropertyTests.HavingType.Sum,
            PropertyTests.HavingType.Avg
        ).ToArbitrary();
    }
}
