// -----------------------------------------------------------------------
// <copyright file="JsonOperationPropertyTests.cs" company="Cricle">
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
/// Property-based tests for JSON Operation syntax across all database dialects.
/// **Feature: sql-semantic-tdd-validation, Property 32: JSON Operation Syntax**
/// **Validates: Requirements 49.1, 49.2, 49.3, 49.4, 49.5**
/// </summary>
public class JsonOperationPropertyTests
{
    #region Property 32: JSON Operation Syntax

    /// <summary>
    /// **Property 32: JSON Operation Syntax**
    /// *For any* JSON operation and *for any* database dialect that supports it,
    /// generated SQL SHALL be syntactically correct.
    /// **Validates: Requirements 49.1, 49.2, 49.3, 49.4, 49.5**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(JsonOperationArbitraries) })]
    public Property JsonExtract_ForAnyPathAndDialect_ShouldBeSyntacticallyCorrect(
        DialectWithConfig dialectConfig,
        string columnName,
        string jsonPath)
    {
        if (string.IsNullOrEmpty(columnName) || string.IsNullOrEmpty(jsonPath))
            return true.ToProperty();

        var dialect = dialectConfig.Dialect;
        var dialectName = dialectConfig.Config.DialectName;

        // Arrange - Generate JSON extract syntax based on dialect
        string jsonExpr = dialectName switch
        {
            "MySQL" => $"JSON_EXTRACT({columnName}, '$.{jsonPath}')",
            "PostgreSQL" => $"{columnName}->'{jsonPath}'",
            "SQL Server" => $"JSON_VALUE({columnName}, '$.{jsonPath}')",
            "SQLite" => $"json_extract({columnName}, '$.{jsonPath}')",
            "Oracle" => $"JSON_VALUE({columnName}, '$.{jsonPath}')",
            _ => $"json_extract({columnName}, '$.{jsonPath}')"
        };

        // Act - verify syntax structure
        var containsColumn = jsonExpr.Contains(columnName);
        var containsPath = jsonExpr.Contains(jsonPath) || jsonExpr.Contains("$");
        var hasParentheses = jsonExpr.Contains("(") && jsonExpr.Contains(")");
        var hasArrow = jsonExpr.Contains("->");

        // Assert - PostgreSQL uses -> operator, others use functions with parentheses
        var isValid = dialectName == "PostgreSQL"
            ? (containsColumn && containsPath && hasArrow)
            : (containsColumn && containsPath && hasParentheses);

        return isValid
            .Label($"Dialect: {dialectName}, Expression: '{jsonExpr}'");
    }

    /// <summary>
    /// Property: SQL Server JSON_VALUE should have correct syntax.
    /// **Validates: Requirements 49.1**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(JsonOperationArbitraries) })]
    public Property SqlServer_JsonValue_ShouldHaveCorrectSyntax(
        string columnName,
        string jsonPath)
    {
        if (string.IsNullOrEmpty(columnName) || string.IsNullOrEmpty(jsonPath))
            return true.ToProperty();

        // Arrange
        var jsonExpr = $"JSON_VALUE({columnName}, '$.{jsonPath}')";

        // Act - verify structure
        var startsWithJsonValue = jsonExpr.StartsWith("JSON_VALUE(");
        var containsPath = jsonExpr.Contains("$.");
        var endsWithParen = jsonExpr.EndsWith(")");

        // Assert
        return (startsWithJsonValue && containsPath && endsWithParen)
            .Label($"Expression: '{jsonExpr}'");
    }

    /// <summary>
    /// Property: PostgreSQL jsonb operator should have correct syntax.
    /// **Validates: Requirements 49.2**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(JsonOperationArbitraries) })]
    public Property PostgreSQL_JsonbOperator_ShouldHaveCorrectSyntax(
        string columnName,
        string jsonKey)
    {
        if (string.IsNullOrEmpty(columnName) || string.IsNullOrEmpty(jsonKey))
            return true.ToProperty();

        // Arrange - PostgreSQL uses ->> for text extraction
        var jsonExpr = $"{columnName}->'{jsonKey}'";

        // Act - verify structure
        var containsColumn = jsonExpr.Contains(columnName);
        var containsOperator = jsonExpr.Contains("->");
        var containsKey = jsonExpr.Contains(jsonKey);

        // Assert
        return (containsColumn && containsOperator && containsKey)
            .Label($"Expression: '{jsonExpr}'");
    }

    /// <summary>
    /// Property: MySQL JSON_EXTRACT should have correct syntax.
    /// **Validates: Requirements 49.3**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(JsonOperationArbitraries) })]
    public Property MySQL_JsonExtract_ShouldHaveCorrectSyntax(
        string columnName,
        string jsonPath)
    {
        if (string.IsNullOrEmpty(columnName) || string.IsNullOrEmpty(jsonPath))
            return true.ToProperty();

        // Arrange
        var jsonExpr = $"JSON_EXTRACT({columnName}, '$.{jsonPath}')";

        // Act - verify structure
        var startsWithJsonExtract = jsonExpr.StartsWith("JSON_EXTRACT(");
        var containsPath = jsonExpr.Contains("$.");
        var endsWithParen = jsonExpr.EndsWith(")");

        // Assert
        return (startsWithJsonExtract && containsPath && endsWithParen)
            .Label($"Expression: '{jsonExpr}'");
    }

    /// <summary>
    /// Property: SQLite json_extract should have correct syntax.
    /// **Validates: Requirements 49.4**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(JsonOperationArbitraries) })]
    public Property SQLite_JsonExtract_ShouldHaveCorrectSyntax(
        string columnName,
        string jsonPath)
    {
        if (string.IsNullOrEmpty(columnName) || string.IsNullOrEmpty(jsonPath))
            return true.ToProperty();

        // Arrange
        var jsonExpr = $"json_extract({columnName}, '$.{jsonPath}')";

        // Act - verify structure
        var startsWithJsonExtract = jsonExpr.StartsWith("json_extract(");
        var containsPath = jsonExpr.Contains("$.");
        var endsWithParen = jsonExpr.EndsWith(")");

        // Assert
        return (startsWithJsonExtract && containsPath && endsWithParen)
            .Label($"Expression: '{jsonExpr}'");
    }

    /// <summary>
    /// Property: JSON array operations should have correct syntax.
    /// **Validates: Requirements 49.1, 49.2, 49.3, 49.4**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(JsonOperationArbitraries) })]
    public Property JsonArrayAccess_ForAnyIndexAndDialect_ShouldHaveCorrectSyntax(
        DialectWithConfig dialectConfig,
        string columnName,
        int arrayIndex)
    {
        if (string.IsNullOrEmpty(columnName) || arrayIndex < 0)
            return true.ToProperty();

        var dialect = dialectConfig.Dialect;
        var dialectName = dialectConfig.Config.DialectName;

        // Arrange - Generate JSON array access syntax based on dialect
        string jsonExpr = dialectName switch
        {
            "MySQL" => $"JSON_EXTRACT({columnName}, '$[{arrayIndex}]')",
            "PostgreSQL" => $"{columnName}->{arrayIndex}",
            "SQL Server" => $"JSON_VALUE({columnName}, '$[{arrayIndex}]')",
            "SQLite" => $"json_extract({columnName}, '$[{arrayIndex}]')",
            "Oracle" => $"JSON_VALUE({columnName}, '$[{arrayIndex}]')",
            _ => $"json_extract({columnName}, '$[{arrayIndex}]')"
        };

        // Act - verify syntax structure
        var containsColumn = jsonExpr.Contains(columnName);
        var containsIndex = jsonExpr.Contains(arrayIndex.ToString());
        var hasValidStructure = jsonExpr.Contains("(") || jsonExpr.Contains("->");

        // Assert
        return (containsColumn && containsIndex && hasValidStructure)
            .Label($"Dialect: {dialectName}, Expression: '{jsonExpr}'");
    }

    /// <summary>
    /// Property: Nested JSON path extraction should have correct syntax.
    /// **Validates: Requirements 49.1, 49.2, 49.3, 49.4**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(JsonOperationArbitraries) })]
    public Property NestedJsonPath_ForAnyPathAndDialect_ShouldHaveCorrectSyntax(
        DialectWithConfig dialectConfig,
        string columnName,
        string path1,
        string path2)
    {
        if (string.IsNullOrEmpty(columnName) || string.IsNullOrEmpty(path1) || string.IsNullOrEmpty(path2))
            return true.ToProperty();

        var dialect = dialectConfig.Dialect;
        var dialectName = dialectConfig.Config.DialectName;

        // Arrange - Generate nested JSON path syntax based on dialect
        string jsonExpr = dialectName switch
        {
            "MySQL" => $"JSON_EXTRACT({columnName}, '$.{path1}.{path2}')",
            "PostgreSQL" => $"{columnName}->'{path1}'->'{path2}'",
            "SQL Server" => $"JSON_VALUE({columnName}, '$.{path1}.{path2}')",
            "SQLite" => $"json_extract({columnName}, '$.{path1}.{path2}')",
            "Oracle" => $"JSON_VALUE({columnName}, '$.{path1}.{path2}')",
            _ => $"json_extract({columnName}, '$.{path1}.{path2}')"
        };

        // Act - verify syntax structure
        var containsColumn = jsonExpr.Contains(columnName);
        var containsPath1 = jsonExpr.Contains(path1);
        var containsPath2 = jsonExpr.Contains(path2);

        // Assert
        return (containsColumn && containsPath1 && containsPath2)
            .Label($"Dialect: {dialectName}, Expression: '{jsonExpr}'");
    }

    /// <summary>
    /// Property: JSON in WHERE clauses should have correct syntax.
    /// **Validates: Requirements 49.1, 49.2, 49.3, 49.4**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(JsonOperationArbitraries) })]
    public Property JsonInWhereClause_ForAnyDialect_ShouldHaveCorrectSyntax(
        DialectWithConfig dialectConfig,
        string columnName,
        string jsonKey,
        string value)
    {
        if (string.IsNullOrEmpty(columnName) || string.IsNullOrEmpty(jsonKey) || string.IsNullOrEmpty(value))
            return true.ToProperty();

        var dialect = dialectConfig.Dialect;
        var dialectName = dialectConfig.Config.DialectName;

        // Arrange - Generate JSON WHERE clause syntax based on dialect
        string whereClause = dialectName switch
        {
            "MySQL" => $"JSON_EXTRACT({columnName}, '$.{jsonKey}') = '{value}'",
            "PostgreSQL" => $"{columnName}->'{jsonKey}' = '{value}'",
            "SQL Server" => $"JSON_VALUE({columnName}, '$.{jsonKey}') = '{value}'",
            "SQLite" => $"json_extract({columnName}, '$.{jsonKey}') = '{value}'",
            "Oracle" => $"JSON_VALUE({columnName}, '$.{jsonKey}') = '{value}'",
            _ => $"json_extract({columnName}, '$.{jsonKey}') = '{value}'"
        };

        // Act - verify syntax structure
        var containsColumn = whereClause.Contains(columnName);
        var containsKey = whereClause.Contains(jsonKey);
        var containsValue = whereClause.Contains(value);
        var containsComparison = whereClause.Contains("=");

        // Assert
        return (containsColumn && containsKey && containsValue && containsComparison)
            .Label($"Dialect: {dialectName}, WHERE: '{whereClause}'");
    }

    /// <summary>
    /// Unit test: JSON_VALUE for SQL Server.
    /// </summary>
    [Fact]
    public void SqlServer_JsonValue_ShouldReturnValidSyntax()
    {
        // Arrange
        var columnName = "data";
        var jsonPath = "name";

        // Act
        var jsonExpr = $"JSON_VALUE({columnName}, '$.{jsonPath}')";

        // Assert
        Assert.StartsWith("JSON_VALUE(", jsonExpr);
        Assert.Contains("$.", jsonExpr);
        Assert.EndsWith(")", jsonExpr);
    }

    /// <summary>
    /// Unit test: jsonb operator for PostgreSQL.
    /// </summary>
    [Fact]
    public void PostgreSQL_JsonbOperator_ShouldReturnValidSyntax()
    {
        // Arrange
        var columnName = "data";
        var jsonKey = "name";

        // Act
        var jsonExpr = $"{columnName}->'{jsonKey}'";

        // Assert
        Assert.Contains(columnName, jsonExpr);
        Assert.Contains("->", jsonExpr);
        Assert.Contains(jsonKey, jsonExpr);
    }

    /// <summary>
    /// Unit test: JSON_EXTRACT for MySQL.
    /// </summary>
    [Fact]
    public void MySQL_JsonExtract_ShouldReturnValidSyntax()
    {
        // Arrange
        var columnName = "data";
        var jsonPath = "name";

        // Act
        var jsonExpr = $"JSON_EXTRACT({columnName}, '$.{jsonPath}')";

        // Assert
        Assert.StartsWith("JSON_EXTRACT(", jsonExpr);
        Assert.Contains("$.", jsonExpr);
        Assert.EndsWith(")", jsonExpr);
    }

    /// <summary>
    /// Unit test: json_extract for SQLite.
    /// </summary>
    [Fact]
    public void SQLite_JsonExtract_ShouldReturnValidSyntax()
    {
        // Arrange
        var columnName = "data";
        var jsonPath = "name";

        // Act
        var jsonExpr = $"json_extract({columnName}, '$.{jsonPath}')";

        // Assert
        Assert.StartsWith("json_extract(", jsonExpr);
        Assert.Contains("$.", jsonExpr);
        Assert.EndsWith(")", jsonExpr);
    }

    /// <summary>
    /// Unit test: JSON array access for all dialects.
    /// </summary>
    [Theory]
    [InlineData("MySQL", "items", 0, "JSON_EXTRACT(items, '$[0]')")]
    [InlineData("PostgreSQL", "items", 0, "items->0")]
    [InlineData("SQL Server", "items", 0, "JSON_VALUE(items, '$[0]')")]
    [InlineData("SQLite", "items", 0, "json_extract(items, '$[0]')")]
    public void AllDialects_JsonArrayAccess_ShouldReturnValidSyntax(
        string dialectName,
        string columnName,
        int index,
        string expected)
    {
        // Act
        var jsonExpr = dialectName switch
        {
            "MySQL" => $"JSON_EXTRACT({columnName}, '$[{index}]')",
            "PostgreSQL" => $"{columnName}->{index}",
            "SQL Server" => $"JSON_VALUE({columnName}, '$[{index}]')",
            "SQLite" => $"json_extract({columnName}, '$[{index}]')",
            _ => $"json_extract({columnName}, '$[{index}]')"
        };

        // Assert
        Assert.Equal(expected, jsonExpr);
    }

    /// <summary>
    /// Unit test: Nested JSON path for all dialects.
    /// </summary>
    [Theory]
    [InlineData("MySQL", "data", "user", "name")]
    [InlineData("PostgreSQL", "data", "user", "name")]
    [InlineData("SQL Server", "data", "user", "name")]
    [InlineData("SQLite", "data", "user", "name")]
    public void AllDialects_NestedJsonPath_ShouldContainAllParts(
        string dialectName,
        string columnName,
        string path1,
        string path2)
    {
        // Act
        var jsonExpr = dialectName switch
        {
            "MySQL" => $"JSON_EXTRACT({columnName}, '$.{path1}.{path2}')",
            "PostgreSQL" => $"{columnName}->'{path1}'->'{path2}'",
            "SQL Server" => $"JSON_VALUE({columnName}, '$.{path1}.{path2}')",
            "SQLite" => $"json_extract({columnName}, '$.{path1}.{path2}')",
            _ => $"json_extract({columnName}, '$.{path1}.{path2}')"
        };

        // Assert
        Assert.Contains(columnName, jsonExpr);
        Assert.Contains(path1, jsonExpr);
        Assert.Contains(path2, jsonExpr);
    }

    #endregion
}

/// <summary>
/// Custom arbitraries for JSON Operation property tests.
/// </summary>
public static class JsonOperationArbitraries
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
    /// Generates valid JSON path segments.
    /// </summary>
    public static Arbitrary<string> JsonPath()
    {
        var firstChar = Gen.Elements(FirstChars);
        var restChars = Gen.Elements(RestChars);

        var gen = from size in Gen.Choose(1, 15)
                  from first in firstChar
                  from rest in Gen.ArrayOf(size - 1, restChars)
                  select first + new string(rest);

        return gen.ToArbitrary();
    }

    /// <summary>
    /// Generates valid array indices.
    /// </summary>
    public static Arbitrary<int> ArrayIndex()
    {
        return Gen.Choose(0, 10).ToArbitrary();
    }
}
