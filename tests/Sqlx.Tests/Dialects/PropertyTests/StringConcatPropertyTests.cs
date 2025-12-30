// -----------------------------------------------------------------------
// <copyright file="StringConcatPropertyTests.cs" company="Cricle">
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
/// Property-based tests for String Concatenation syntax correctness across all database dialects.
/// **Feature: sql-semantic-tdd-validation, Property 9: String Concatenation Syntax**
/// **Feature: sql-semantic-tdd-validation, Property 10: String Concatenation Edge Cases**
/// **Validates: Requirements 8.1, 8.2, 8.3, 8.4, 8.5, 8.6, 8.7**
/// </summary>
public class StringConcatPropertyTests
{
    private static readonly MySqlDialectProvider MySqlProvider = new();
    private static readonly PostgreSqlDialectProvider PostgreSqlProvider = new();
    private static readonly SqlServerDialectProvider SqlServerProvider = new();
    private static readonly SQLiteDialectProvider SQLiteProvider = new();

    /// <summary>
    /// **Property 9: String Concatenation Syntax**
    /// *For any* array of expressions and *for any* database dialect, 
    /// GetConcatenationSyntax SHALL return correct concatenation syntax.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(StringConcatArbitraries) })]
    public Property GetConcatenationSyntax_ForAnyDialectAndExpressions_ShouldReturnCorrectSyntax(
        StringConcatDialectProviderWithConfig providerConfig,
        string[] expressions)
    {
        if (expressions == null || expressions.Length < 2)
            return true.ToProperty();

        var config = providerConfig.Config;

        // Act
        var result = providerConfig.GetConcatenationSyntax(expressions);

        // Assert - verify correct syntax based on dialect
        bool isCorrect = config.ExpectedConcatOperator switch
        {
            "CONCAT" => result.StartsWith("CONCAT(") && result.EndsWith(")"),
            "||" => result.Contains(" || "),
            "+" => result.Contains(" + "),
            _ => false
        };

        return isCorrect.Label($"Dialect: {config.DialectName}, " +
                               $"Expressions: [{string.Join(", ", expressions)}], " +
                               $"Result: '{result}', " +
                               $"Expected operator: '{config.ExpectedConcatOperator}'");
    }

    /// <summary>
    /// Property: MySQL should use CONCAT() function for string concatenation.
    /// **Validates: Requirements 8.1**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(StringConcatArbitraries) })]
    public Property MySQL_GetConcatenationSyntax_ShouldUseConcatFunction(string[] expressions)
    {
        if (expressions == null || expressions.Length < 2)
            return true.ToProperty();

        // Act
        var result = MySqlProvider.GetConcatenationSyntax(expressions);

        // Assert
        var usesConcat = result.StartsWith("CONCAT(") && result.EndsWith(")");
        var containsAllExpressions = expressions.All(e => result.Contains(e));

        return (usesConcat && containsAllExpressions)
            .Label($"Expressions: [{string.Join(", ", expressions)}], Result: '{result}'");
    }

    /// <summary>
    /// Property: PostgreSQL should use || operator for string concatenation.
    /// **Validates: Requirements 8.2**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(StringConcatArbitraries) })]
    public Property PostgreSQL_GetConcatenationSyntax_ShouldUsePipeOperator(string[] expressions)
    {
        if (expressions == null || expressions.Length < 2)
            return true.ToProperty();

        // Act
        var result = PostgreSqlProvider.GetConcatenationSyntax(expressions);

        // Assert
        var usesPipeOperator = result.Contains(" || ");
        var containsAllExpressions = expressions.All(e => result.Contains(e));

        return (usesPipeOperator && containsAllExpressions)
            .Label($"Expressions: [{string.Join(", ", expressions)}], Result: '{result}'");
    }

    /// <summary>
    /// Property: SQL Server should use + operator for string concatenation.
    /// **Validates: Requirements 8.3**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(StringConcatArbitraries) })]
    public Property SqlServer_GetConcatenationSyntax_ShouldUsePlusOperator(string[] expressions)
    {
        if (expressions == null || expressions.Length < 2)
            return true.ToProperty();

        // Act
        var result = SqlServerProvider.GetConcatenationSyntax(expressions);

        // Assert
        var usesPlusOperator = result.Contains(" + ");
        var containsAllExpressions = expressions.All(e => result.Contains(e));

        return (usesPlusOperator && containsAllExpressions)
            .Label($"Expressions: [{string.Join(", ", expressions)}], Result: '{result}'");
    }

    /// <summary>
    /// Property: SQLite should use || operator for string concatenation.
    /// **Validates: Requirements 8.4**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(StringConcatArbitraries) })]
    public Property SQLite_GetConcatenationSyntax_ShouldUsePipeOperator(string[] expressions)
    {
        if (expressions == null || expressions.Length < 2)
            return true.ToProperty();

        // Act
        var result = SQLiteProvider.GetConcatenationSyntax(expressions);

        // Assert
        var usesPipeOperator = result.Contains(" || ");
        var containsAllExpressions = expressions.All(e => result.Contains(e));

        return (usesPipeOperator && containsAllExpressions)
            .Label($"Expressions: [{string.Join(", ", expressions)}], Result: '{result}'");
    }

    /// <summary>
    /// Property: Concatenation result should contain all input expressions.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(StringConcatArbitraries) })]
    public Property GetConcatenationSyntax_ShouldContainAllExpressions(
        StringConcatDialectProviderWithConfig providerConfig,
        string[] expressions)
    {
        if (expressions == null || expressions.Length == 0)
            return true.ToProperty();

        // Act
        var result = providerConfig.GetConcatenationSyntax(expressions);

        // Assert
        var containsAll = expressions.All(e => result.Contains(e));

        return containsAll.Label($"Dialect: {providerConfig.Config.DialectName}, " +
                                 $"Expressions: [{string.Join(", ", expressions)}], " +
                                 $"Result: '{result}'");
    }

    /// <summary>
    /// Property: Concatenation should be consistent across multiple calls.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(StringConcatArbitraries) })]
    public Property GetConcatenationSyntax_ShouldBeConsistentAcrossMultipleCalls(
        StringConcatDialectProviderWithConfig providerConfig,
        string[] expressions)
    {
        if (expressions == null || expressions.Length == 0)
            return true.ToProperty();

        // Act
        var result1 = providerConfig.GetConcatenationSyntax(expressions);
        var result2 = providerConfig.GetConcatenationSyntax(expressions);

        // Assert
        return (result1 == result2)
            .Label($"Dialect: {providerConfig.Config.DialectName}, " +
                   $"Result1: '{result1}', Result2: '{result2}'");
    }

    /// <summary>
    /// Unit test: MySQL concatenation with specific expressions.
    /// </summary>
    [Fact]
    public void MySQL_GetConcatenationSyntax_WithTwoExpressions_ShouldReturnConcatFunction()
    {
        // Arrange
        var expressions = new[] { "a", "b" };

        // Act
        var result = MySqlProvider.GetConcatenationSyntax(expressions);

        // Assert
        Assert.Equal("CONCAT(a, b)", result);
    }

    /// <summary>
    /// Unit test: PostgreSQL concatenation with specific expressions.
    /// </summary>
    [Fact]
    public void PostgreSQL_GetConcatenationSyntax_WithTwoExpressions_ShouldReturnPipeOperator()
    {
        // Arrange
        var expressions = new[] { "a", "b" };

        // Act
        var result = PostgreSqlProvider.GetConcatenationSyntax(expressions);

        // Assert
        Assert.Equal("a || b", result);
    }

    /// <summary>
    /// Unit test: SQL Server concatenation with specific expressions.
    /// </summary>
    [Fact]
    public void SqlServer_GetConcatenationSyntax_WithTwoExpressions_ShouldReturnPlusOperator()
    {
        // Arrange
        var expressions = new[] { "a", "b" };

        // Act
        var result = SqlServerProvider.GetConcatenationSyntax(expressions);

        // Assert
        Assert.Equal("a + b", result);
    }

    /// <summary>
    /// Unit test: SQLite concatenation with specific expressions.
    /// </summary>
    [Fact]
    public void SQLite_GetConcatenationSyntax_WithTwoExpressions_ShouldReturnPipeOperator()
    {
        // Arrange
        var expressions = new[] { "a", "b" };

        // Act
        var result = SQLiteProvider.GetConcatenationSyntax(expressions);

        // Assert
        Assert.Equal("a || b", result);
    }

    /// <summary>
    /// Unit test: MySQL concatenation with three expressions.
    /// </summary>
    [Fact]
    public void MySQL_GetConcatenationSyntax_WithThreeExpressions_ShouldReturnConcatFunction()
    {
        // Arrange
        var expressions = new[] { "a", "b", "c" };

        // Act
        var result = MySqlProvider.GetConcatenationSyntax(expressions);

        // Assert
        Assert.Equal("CONCAT(a, b, c)", result);
    }

    #region Property 10: String Concatenation Edge Cases

    /// <summary>
    /// **Property 10: String Concatenation Edge Cases**
    /// *For any* single expression, GetConcatenationSyntax SHALL return expression unchanged.
    /// **Validates: Requirements 8.6**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(StringConcatArbitraries) })]
    public Property GetConcatenationSyntax_WithSingleExpression_ShouldReturnUnchanged(
        StringConcatDialectProviderWithConfig providerConfig)
    {
        var singleExpressions = new[] { "a", "col1", "name", "value", "column_name" };
        
        return Prop.ForAll(
            Gen.Elements(singleExpressions).ToArbitrary(),
            expression =>
            {
                // Act
                var result = providerConfig.GetConcatenationSyntax(new[] { expression });

                // Assert
                return (result == expression)
                    .Label($"Dialect: {providerConfig.Config.DialectName}, " +
                           $"Expression: '{expression}', Result: '{result}'");
            });
    }

    /// <summary>
    /// **Property 10: String Concatenation Edge Cases**
    /// *For any* empty array, GetConcatenationSyntax SHALL return empty string.
    /// **Validates: Requirements 8.7**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(StringConcatArbitraries) })]
    public Property GetConcatenationSyntax_WithEmptyArray_ShouldReturnEmptyString(
        StringConcatDialectProviderWithConfig providerConfig)
    {
        // Act
        var result = providerConfig.GetConcatenationSyntax(Array.Empty<string>());

        // Assert
        return (result == string.Empty)
            .Label($"Dialect: {providerConfig.Config.DialectName}, " +
                   $"Result: '{result}', Expected: ''");
    }

    /// <summary>
    /// Unit test: All dialects should return single expression unchanged.
    /// **Validates: Requirements 8.6**
    /// </summary>
    [Theory]
    [InlineData("a")]
    [InlineData("column_name")]
    [InlineData("'literal'")]
    public void AllDialects_GetConcatenationSyntax_WithSingleExpression_ShouldReturnUnchanged(string expression)
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
            // Act
            var result = provider.GetConcatenationSyntax(expression);

            // Assert
            Assert.Equal(expression, result);
        }
    }

    /// <summary>
    /// Unit test: All dialects should return empty string for empty array.
    /// **Validates: Requirements 8.7**
    /// </summary>
    [Fact]
    public void AllDialects_GetConcatenationSyntax_WithEmptyArray_ShouldReturnEmptyString()
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
            // Act
            var result = provider.GetConcatenationSyntax(Array.Empty<string>());

            // Assert
            Assert.Equal(string.Empty, result);
        }
    }

    /// <summary>
    /// Unit test: MySQL single expression edge case.
    /// </summary>
    [Fact]
    public void MySQL_GetConcatenationSyntax_WithSingleExpression_ShouldReturnUnchanged()
    {
        // Arrange
        var expression = "test_column";

        // Act
        var result = MySqlProvider.GetConcatenationSyntax(expression);

        // Assert
        Assert.Equal(expression, result);
    }

    /// <summary>
    /// Unit test: MySQL empty array edge case.
    /// </summary>
    [Fact]
    public void MySQL_GetConcatenationSyntax_WithEmptyArray_ShouldReturnEmptyString()
    {
        // Act
        var result = MySqlProvider.GetConcatenationSyntax(Array.Empty<string>());

        // Assert
        Assert.Equal(string.Empty, result);
    }

    /// <summary>
    /// Unit test: PostgreSQL single expression edge case.
    /// </summary>
    [Fact]
    public void PostgreSQL_GetConcatenationSyntax_WithSingleExpression_ShouldReturnUnchanged()
    {
        // Arrange
        var expression = "test_column";

        // Act
        var result = PostgreSqlProvider.GetConcatenationSyntax(expression);

        // Assert
        Assert.Equal(expression, result);
    }

    /// <summary>
    /// Unit test: SQL Server single expression edge case.
    /// </summary>
    [Fact]
    public void SqlServer_GetConcatenationSyntax_WithSingleExpression_ShouldReturnUnchanged()
    {
        // Arrange
        var expression = "test_column";

        // Act
        var result = SqlServerProvider.GetConcatenationSyntax(expression);

        // Assert
        Assert.Equal(expression, result);
    }

    /// <summary>
    /// Unit test: SQLite single expression edge case.
    /// </summary>
    [Fact]
    public void SQLite_GetConcatenationSyntax_WithSingleExpression_ShouldReturnUnchanged()
    {
        // Arrange
        var expression = "test_column";

        // Act
        var result = SQLiteProvider.GetConcatenationSyntax(expression);

        // Assert
        Assert.Equal(expression, result);
    }

    #endregion
}


/// <summary>
/// Wrapper class to pair a dialect provider with its test configuration for String Concatenation tests.
/// </summary>
public class StringConcatDialectProviderWithConfig
{
    public Func<string[], string> GetConcatenationSyntax { get; }
    public DialectTestConfig Config { get; }

    public StringConcatDialectProviderWithConfig(
        Func<string[], string> getConcatenationSyntax,
        DialectTestConfig config)
    {
        GetConcatenationSyntax = getConcatenationSyntax;
        Config = config;
    }

    public override string ToString() => Config.DialectName;
}

/// <summary>
/// Custom arbitraries for String Concatenation property tests.
/// </summary>
public static class StringConcatArbitraries
{
    private static readonly MySqlDialectProvider MySqlProvider = new();
    private static readonly PostgreSqlDialectProvider PostgreSqlProvider = new();
    private static readonly SqlServerDialectProvider SqlServerProvider = new();
    private static readonly SQLiteDialectProvider SQLiteProvider = new();

    /// <summary>
    /// Generates dialect provider with its corresponding test configuration.
    /// </summary>
    public static Arbitrary<StringConcatDialectProviderWithConfig> StringConcatDialectProviderWithConfig()
    {
        return Gen.Elements(
            new StringConcatDialectProviderWithConfig(
                MySqlProvider.GetConcatenationSyntax,
                DialectTestConfig.MySql),
            new StringConcatDialectProviderWithConfig(
                PostgreSqlProvider.GetConcatenationSyntax,
                DialectTestConfig.PostgreSql),
            new StringConcatDialectProviderWithConfig(
                SqlServerProvider.GetConcatenationSyntax,
                DialectTestConfig.SqlServer),
            new StringConcatDialectProviderWithConfig(
                SQLiteProvider.GetConcatenationSyntax,
                DialectTestConfig.SQLite)
        ).ToArbitrary();
    }

    /// <summary>
    /// Generates valid SQL expression string arrays for concatenation tests.
    /// </summary>
    public static Arbitrary<string[]> ValidExpressionArray()
    {
        var expressions = new[] { "a", "b", "c", "col1", "col2", "name", "value", "column_name" };
        return Gen.Sized(size =>
        {
            var count = Math.Max(2, Math.Min(size, 5)); // 2-5 expressions for meaningful concatenation
            return Gen.ArrayOf(count, Gen.Elements(expressions));
        }).Where(arr => arr.Length >= 2)
          .ToArbitrary();
    }
}
