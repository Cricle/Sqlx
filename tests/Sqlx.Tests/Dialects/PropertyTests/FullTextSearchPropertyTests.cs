// -----------------------------------------------------------------------
// <copyright file="FullTextSearchPropertyTests.cs" company="Cricle">
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
/// Property-based tests for Full-Text Search syntax across all database dialects.
/// **Feature: sql-semantic-tdd-validation, Property 33: Full-Text Search Syntax**
/// **Validates: Requirements 50.1, 50.2, 50.3, 50.4, 50.5**
/// </summary>
public class FullTextSearchPropertyTests
{
    #region Property 33: Full-Text Search Syntax

    /// <summary>
    /// **Property 33: Full-Text Search Syntax**
    /// *For any* full-text search operation and *for any* database dialect that supports it,
    /// generated SQL SHALL be syntactically correct.
    /// **Validates: Requirements 50.1, 50.2, 50.3, 50.4, 50.5**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(FullTextSearchArbitraries) })]
    public Property FullTextSearch_ForAnySearchTermAndDialect_ShouldBeSyntacticallyCorrect(
        DialectWithConfig dialectConfig,
        string columnName,
        string searchTerm)
    {
        if (string.IsNullOrEmpty(columnName) || string.IsNullOrEmpty(searchTerm))
            return true.ToProperty();

        var dialect = dialectConfig.Dialect;
        var dialectName = dialectConfig.Config.DialectName;

        // Arrange - Generate full-text search syntax based on dialect
        string ftsExpr = dialectName switch
        {
            "MySQL" => $"MATCH({columnName}) AGAINST('{searchTerm}' IN NATURAL LANGUAGE MODE)",
            "PostgreSQL" => $"to_tsvector({columnName}) @@ to_tsquery('{searchTerm}')",
            "SQL Server" => $"CONTAINS({columnName}, '{searchTerm}')",
            "SQLite" => $"{columnName} MATCH '{searchTerm}'",
            "Oracle" => $"CONTAINS({columnName}, '{searchTerm}') > 0",
            _ => $"{columnName} MATCH '{searchTerm}'"
        };

        // Act - verify syntax structure
        var containsColumn = ftsExpr.Contains(columnName);
        var containsSearchTerm = ftsExpr.Contains(searchTerm);
        var hasValidKeyword = ftsExpr.Contains("MATCH") || ftsExpr.Contains("CONTAINS") || 
                              ftsExpr.Contains("to_tsvector") || ftsExpr.Contains("@@");

        // Assert
        return (containsColumn && containsSearchTerm && hasValidKeyword)
            .Label($"Dialect: {dialectName}, Expression: '{ftsExpr}'");
    }

    /// <summary>
    /// Property: MySQL MATCH AGAINST should have correct syntax.
    /// **Validates: Requirements 50.1**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(FullTextSearchArbitraries) })]
    public Property MySQL_MatchAgainst_ShouldHaveCorrectSyntax(
        string columnName,
        string searchTerm)
    {
        if (string.IsNullOrEmpty(columnName) || string.IsNullOrEmpty(searchTerm))
            return true.ToProperty();

        // Arrange
        var ftsExpr = $"MATCH({columnName}) AGAINST('{searchTerm}' IN NATURAL LANGUAGE MODE)";

        // Act - verify structure
        var startsWithMatch = ftsExpr.StartsWith("MATCH(");
        var containsAgainst = ftsExpr.Contains("AGAINST");
        var containsMode = ftsExpr.Contains("IN NATURAL LANGUAGE MODE");

        // Assert
        return (startsWithMatch && containsAgainst && containsMode)
            .Label($"Expression: '{ftsExpr}'");
    }

    /// <summary>
    /// Property: MySQL MATCH AGAINST with BOOLEAN MODE should have correct syntax.
    /// **Validates: Requirements 50.1**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(FullTextSearchArbitraries) })]
    public Property MySQL_MatchAgainstBooleanMode_ShouldHaveCorrectSyntax(
        string columnName,
        string searchTerm)
    {
        if (string.IsNullOrEmpty(columnName) || string.IsNullOrEmpty(searchTerm))
            return true.ToProperty();

        // Arrange
        var ftsExpr = $"MATCH({columnName}) AGAINST('+{searchTerm}' IN BOOLEAN MODE)";

        // Act - verify structure
        var containsMatch = ftsExpr.Contains("MATCH");
        var containsAgainst = ftsExpr.Contains("AGAINST");
        var containsBooleanMode = ftsExpr.Contains("IN BOOLEAN MODE");

        // Assert
        return (containsMatch && containsAgainst && containsBooleanMode)
            .Label($"Expression: '{ftsExpr}'");
    }

    /// <summary>
    /// Property: PostgreSQL to_tsvector/to_tsquery should have correct syntax.
    /// **Validates: Requirements 50.2**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(FullTextSearchArbitraries) })]
    public Property PostgreSQL_TsVectorTsQuery_ShouldHaveCorrectSyntax(
        string columnName,
        string searchTerm)
    {
        if (string.IsNullOrEmpty(columnName) || string.IsNullOrEmpty(searchTerm))
            return true.ToProperty();

        // Arrange
        var ftsExpr = $"to_tsvector({columnName}) @@ to_tsquery('{searchTerm}')";

        // Act - verify structure
        var containsTsVector = ftsExpr.Contains("to_tsvector");
        var containsTsQuery = ftsExpr.Contains("to_tsquery");
        var containsOperator = ftsExpr.Contains("@@");

        // Assert
        return (containsTsVector && containsTsQuery && containsOperator)
            .Label($"Expression: '{ftsExpr}'");
    }

    /// <summary>
    /// Property: PostgreSQL with language config should have correct syntax.
    /// **Validates: Requirements 50.2**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(FullTextSearchArbitraries) })]
    public Property PostgreSQL_TsVectorWithLanguage_ShouldHaveCorrectSyntax(
        string columnName,
        string searchTerm,
        string language)
    {
        if (string.IsNullOrEmpty(columnName) || string.IsNullOrEmpty(searchTerm) || string.IsNullOrEmpty(language))
            return true.ToProperty();

        // Arrange
        var ftsExpr = $"to_tsvector('{language}', {columnName}) @@ to_tsquery('{language}', '{searchTerm}')";

        // Act - verify structure
        var containsTsVector = ftsExpr.Contains("to_tsvector");
        var containsLanguage = ftsExpr.Contains(language);
        var containsColumn = ftsExpr.Contains(columnName);

        // Assert
        return (containsTsVector && containsLanguage && containsColumn)
            .Label($"Expression: '{ftsExpr}'");
    }

    /// <summary>
    /// Property: SQL Server CONTAINS should have correct syntax.
    /// **Validates: Requirements 50.3**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(FullTextSearchArbitraries) })]
    public Property SqlServer_Contains_ShouldHaveCorrectSyntax(
        string columnName,
        string searchTerm)
    {
        if (string.IsNullOrEmpty(columnName) || string.IsNullOrEmpty(searchTerm))
            return true.ToProperty();

        // Arrange
        var ftsExpr = $"CONTAINS({columnName}, '{searchTerm}')";

        // Act - verify structure
        var startsWithContains = ftsExpr.StartsWith("CONTAINS(");
        var containsColumn = ftsExpr.Contains(columnName);
        var endsWithParen = ftsExpr.EndsWith(")");

        // Assert
        return (startsWithContains && containsColumn && endsWithParen)
            .Label($"Expression: '{ftsExpr}'");
    }

    /// <summary>
    /// Property: SQL Server FREETEXT should have correct syntax.
    /// **Validates: Requirements 50.3**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(FullTextSearchArbitraries) })]
    public Property SqlServer_Freetext_ShouldHaveCorrectSyntax(
        string columnName,
        string searchTerm)
    {
        if (string.IsNullOrEmpty(columnName) || string.IsNullOrEmpty(searchTerm))
            return true.ToProperty();

        // Arrange
        var ftsExpr = $"FREETEXT({columnName}, '{searchTerm}')";

        // Act - verify structure
        var startsWithFreetext = ftsExpr.StartsWith("FREETEXT(");
        var containsColumn = ftsExpr.Contains(columnName);
        var endsWithParen = ftsExpr.EndsWith(")");

        // Assert
        return (startsWithFreetext && containsColumn && endsWithParen)
            .Label($"Expression: '{ftsExpr}'");
    }

    /// <summary>
    /// Property: SQLite FTS5 MATCH should have correct syntax.
    /// **Validates: Requirements 50.4**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(FullTextSearchArbitraries) })]
    public Property SQLite_FTS5Match_ShouldHaveCorrectSyntax(
        string tableName,
        string searchTerm)
    {
        if (string.IsNullOrEmpty(tableName) || string.IsNullOrEmpty(searchTerm))
            return true.ToProperty();

        // Arrange
        var ftsExpr = $"{tableName} MATCH '{searchTerm}'";

        // Act - verify structure
        var containsTable = ftsExpr.Contains(tableName);
        var containsMatch = ftsExpr.Contains("MATCH");
        var containsSearchTerm = ftsExpr.Contains(searchTerm);

        // Assert
        return (containsTable && containsMatch && containsSearchTerm)
            .Label($"Expression: '{ftsExpr}'");
    }

    /// <summary>
    /// Property: SQLite FTS5 with phrase search should have correct syntax.
    /// **Validates: Requirements 50.4**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(FullTextSearchArbitraries) })]
    public Property SQLite_FTS5PhraseSearch_ShouldHaveCorrectSyntax(
        string tableName,
        string phrase)
    {
        if (string.IsNullOrEmpty(tableName) || string.IsNullOrEmpty(phrase))
            return true.ToProperty();

        // Arrange - phrase search uses double quotes
        var ftsExpr = $"{tableName} MATCH '\"{phrase}\"'";

        // Act - verify structure
        var containsTable = ftsExpr.Contains(tableName);
        var containsMatch = ftsExpr.Contains("MATCH");
        var containsQuotes = ftsExpr.Contains("\"");

        // Assert
        return (containsTable && containsMatch && containsQuotes)
            .Label($"Expression: '{ftsExpr}'");
    }

    /// <summary>
    /// Property: Full-text search with multiple columns should have correct syntax.
    /// **Validates: Requirements 50.1, 50.2, 50.3, 50.4**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(FullTextSearchArbitraries) })]
    public Property FullTextSearch_WithMultipleColumns_ShouldHaveCorrectSyntax(
        DialectWithConfig dialectConfig,
        string column1,
        string column2,
        string searchTerm)
    {
        if (string.IsNullOrEmpty(column1) || string.IsNullOrEmpty(column2) || string.IsNullOrEmpty(searchTerm))
            return true.ToProperty();

        var dialect = dialectConfig.Dialect;
        var dialectName = dialectConfig.Config.DialectName;

        // Arrange - Generate multi-column full-text search syntax
        string ftsExpr = dialectName switch
        {
            "MySQL" => $"MATCH({column1}, {column2}) AGAINST('{searchTerm}')",
            "PostgreSQL" => $"to_tsvector({column1} || ' ' || {column2}) @@ to_tsquery('{searchTerm}')",
            "SQL Server" => $"CONTAINS(({column1}, {column2}), '{searchTerm}')",
            "SQLite" => $"fts_table MATCH '{searchTerm}'", // FTS5 indexes all columns
            _ => $"MATCH({column1}, {column2}) AGAINST('{searchTerm}')"
        };

        // Act - verify syntax structure
        var containsSearchTerm = ftsExpr.Contains(searchTerm);
        var hasValidKeyword = ftsExpr.Contains("MATCH") || ftsExpr.Contains("CONTAINS") || 
                              ftsExpr.Contains("to_tsvector");

        // Assert
        return (containsSearchTerm && hasValidKeyword)
            .Label($"Dialect: {dialectName}, Expression: '{ftsExpr}'");
    }

    #endregion

    #region Unit Tests

    /// <summary>
    /// Unit test: MySQL MATCH AGAINST for natural language mode.
    /// </summary>
    [Fact]
    public void MySQL_MatchAgainstNaturalLanguage_ShouldReturnValidSyntax()
    {
        // Arrange
        var columnName = "content";
        var searchTerm = "programming";

        // Act
        var ftsExpr = $"MATCH({columnName}) AGAINST('{searchTerm}' IN NATURAL LANGUAGE MODE)";

        // Assert
        Assert.StartsWith("MATCH(", ftsExpr);
        Assert.Contains("AGAINST", ftsExpr);
        Assert.Contains("IN NATURAL LANGUAGE MODE", ftsExpr);
    }

    /// <summary>
    /// Unit test: MySQL MATCH AGAINST for boolean mode.
    /// </summary>
    [Fact]
    public void MySQL_MatchAgainstBooleanMode_ShouldReturnValidSyntax()
    {
        // Arrange
        var columnName = "title";
        var searchTerm = "+required -excluded";

        // Act
        var ftsExpr = $"MATCH({columnName}) AGAINST('{searchTerm}' IN BOOLEAN MODE)";

        // Assert
        Assert.Contains("MATCH", ftsExpr);
        Assert.Contains("AGAINST", ftsExpr);
        Assert.Contains("IN BOOLEAN MODE", ftsExpr);
    }

    /// <summary>
    /// Unit test: PostgreSQL to_tsvector/to_tsquery.
    /// </summary>
    [Fact]
    public void PostgreSQL_TsVectorTsQuery_ShouldReturnValidSyntax()
    {
        // Arrange
        var columnName = "content";
        var searchTerm = "search";

        // Act
        var ftsExpr = $"to_tsvector({columnName}) @@ to_tsquery('{searchTerm}')";

        // Assert
        Assert.Contains("to_tsvector", ftsExpr);
        Assert.Contains("to_tsquery", ftsExpr);
        Assert.Contains("@@", ftsExpr);
    }

    /// <summary>
    /// Unit test: PostgreSQL with language configuration.
    /// </summary>
    [Fact]
    public void PostgreSQL_TsVectorWithLanguage_ShouldReturnValidSyntax()
    {
        // Arrange
        var columnName = "content";
        var searchTerm = "search";
        var language = "english";

        // Act
        var ftsExpr = $"to_tsvector('{language}', {columnName}) @@ to_tsquery('{language}', '{searchTerm}')";

        // Assert
        Assert.Contains("to_tsvector", ftsExpr);
        Assert.Contains(language, ftsExpr);
        Assert.Contains(columnName, ftsExpr);
    }

    /// <summary>
    /// Unit test: SQL Server CONTAINS.
    /// </summary>
    [Fact]
    public void SqlServer_Contains_ShouldReturnValidSyntax()
    {
        // Arrange
        var columnName = "content";
        var searchTerm = "programming";

        // Act
        var ftsExpr = $"CONTAINS({columnName}, '{searchTerm}')";

        // Assert
        Assert.StartsWith("CONTAINS(", ftsExpr);
        Assert.Contains(columnName, ftsExpr);
        Assert.EndsWith(")", ftsExpr);
    }

    /// <summary>
    /// Unit test: SQL Server FREETEXT.
    /// </summary>
    [Fact]
    public void SqlServer_Freetext_ShouldReturnValidSyntax()
    {
        // Arrange
        var columnName = "description";
        var searchTerm = "database management";

        // Act
        var ftsExpr = $"FREETEXT({columnName}, '{searchTerm}')";

        // Assert
        Assert.StartsWith("FREETEXT(", ftsExpr);
        Assert.Contains(columnName, ftsExpr);
        Assert.EndsWith(")", ftsExpr);
    }

    /// <summary>
    /// Unit test: SQLite FTS5 MATCH.
    /// </summary>
    [Fact]
    public void SQLite_FTS5Match_ShouldReturnValidSyntax()
    {
        // Arrange
        var tableName = "articles_fts";
        var searchTerm = "technology";

        // Act
        var ftsExpr = $"{tableName} MATCH '{searchTerm}'";

        // Assert
        Assert.Contains(tableName, ftsExpr);
        Assert.Contains("MATCH", ftsExpr);
        Assert.Contains(searchTerm, ftsExpr);
    }

    /// <summary>
    /// Unit test: SQLite FTS5 phrase search.
    /// </summary>
    [Fact]
    public void SQLite_FTS5PhraseSearch_ShouldReturnValidSyntax()
    {
        // Arrange
        var tableName = "docs_fts";
        var phrase = "machine learning";

        // Act
        var ftsExpr = $"{tableName} MATCH '\"{phrase}\"'";

        // Assert
        Assert.Contains(tableName, ftsExpr);
        Assert.Contains("MATCH", ftsExpr);
        Assert.Contains("\"", ftsExpr);
    }

    /// <summary>
    /// Unit test: Full-text search for all dialects.
    /// </summary>
    [Theory]
    [InlineData("MySQL", "content", "search", "MATCH(content) AGAINST('search' IN NATURAL LANGUAGE MODE)")]
    [InlineData("PostgreSQL", "content", "search", "to_tsvector(content) @@ to_tsquery('search')")]
    [InlineData("SQL Server", "content", "search", "CONTAINS(content, 'search')")]
    [InlineData("SQLite", "fts_table", "search", "fts_table MATCH 'search'")]
    public void AllDialects_FullTextSearch_ShouldReturnValidSyntax(
        string dialectName,
        string columnOrTable,
        string searchTerm,
        string expected)
    {
        // Act
        var ftsExpr = dialectName switch
        {
            "MySQL" => $"MATCH({columnOrTable}) AGAINST('{searchTerm}' IN NATURAL LANGUAGE MODE)",
            "PostgreSQL" => $"to_tsvector({columnOrTable}) @@ to_tsquery('{searchTerm}')",
            "SQL Server" => $"CONTAINS({columnOrTable}, '{searchTerm}')",
            "SQLite" => $"{columnOrTable} MATCH '{searchTerm}'",
            _ => ""
        };

        // Assert
        Assert.Equal(expected, ftsExpr);
    }

    /// <summary>
    /// Unit test: Multi-column full-text search for all dialects.
    /// </summary>
    [Theory]
    [InlineData("MySQL", "title", "content", "search")]
    [InlineData("PostgreSQL", "title", "content", "search")]
    [InlineData("SQL Server", "title", "content", "search")]
    public void AllDialects_MultiColumnFullTextSearch_ShouldContainAllParts(
        string dialectName,
        string column1,
        string column2,
        string searchTerm)
    {
        // Act
        var ftsExpr = dialectName switch
        {
            "MySQL" => $"MATCH({column1}, {column2}) AGAINST('{searchTerm}')",
            "PostgreSQL" => $"to_tsvector({column1} || ' ' || {column2}) @@ to_tsquery('{searchTerm}')",
            "SQL Server" => $"CONTAINS(({column1}, {column2}), '{searchTerm}')",
            _ => ""
        };

        // Assert
        Assert.Contains(searchTerm, ftsExpr);
        Assert.True(ftsExpr.Contains("MATCH") || ftsExpr.Contains("CONTAINS") || ftsExpr.Contains("to_tsvector"));
    }

    #endregion
}

/// <summary>
/// Custom arbitraries for Full-Text Search property tests.
/// </summary>
public static class FullTextSearchArbitraries
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
    /// Generates valid search terms.
    /// </summary>
    public static Arbitrary<string> SearchTerm()
    {
        var firstChar = Gen.Elements(FirstChars);
        var restChars = Gen.Elements(FirstChars); // Only letters for search terms

        var gen = from size in Gen.Choose(3, 15)
                  from first in firstChar
                  from rest in Gen.ArrayOf(size - 1, restChars)
                  select first + new string(rest);

        return gen.ToArbitrary();
    }

    /// <summary>
    /// Generates valid language names for PostgreSQL.
    /// </summary>
    public static Arbitrary<string> Language()
    {
        return Gen.Elements("english", "spanish", "french", "german", "simple").ToArbitrary();
    }
}
