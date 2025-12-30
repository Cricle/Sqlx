// -----------------------------------------------------------------------
// <copyright file="LimitOffsetClausePropertyTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using FsCheck;
using FsCheck.Xunit;
using Xunit;
using Sqlx.Generator;

namespace Sqlx.Tests.Dialects.PropertyTests;

/// <summary>
/// Property-based tests for parameterized LIMIT/OFFSET clause generation across all database dialects.
/// **Feature: sql-semantic-tdd-validation, Property 14: LIMIT/OFFSET Clause with Parameters**
/// **Validates: Requirements 12.1, 12.2, 12.3, 12.4, 12.5**
/// </summary>
public class LimitOffsetClausePropertyTests
{
    private static readonly MySqlDialectProvider MySqlProvider = new();
    private static readonly PostgreSqlDialectProvider PostgreSqlProvider = new();
    private static readonly SqlServerDialectProvider SqlServerProvider = new();
    private static readonly SQLiteDialectProvider SQLiteProvider = new();

    /// <summary>
    /// **Property 14: LIMIT/OFFSET Clause with Parameters**
    /// *For any* limit and offset parameter names and *for any* database dialect,
    /// GenerateLimitOffsetClause SHALL return correct parameterized pagination clause.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(LimitOffsetArbitraries) })]
    public Property GenerateLimitOffsetClause_ForAnyParameterNames_ShouldReturnCorrectClause(
        string limitParam, string offsetParam, LimitOffsetDialectProvider provider)
    {
        // Skip if parameter names are invalid
        if (string.IsNullOrEmpty(limitParam) || string.IsNullOrEmpty(offsetParam))
            return true.ToProperty();

        // Act
        var result = provider.GenerateLimitOffsetClause(limitParam, offsetParam, out bool requiresOrderBy);

        // Assert - should contain both parameter names
        var containsLimitParam = result.Contains(limitParam);
        var containsOffsetParam = result.Contains(offsetParam);

        return (containsLimitParam && containsOffsetParam)
            .Label($"Dialect: {provider.DialectName}, LimitParam: '{limitParam}', OffsetParam: '{offsetParam}', Result: '{result}', RequiresOrderBy: {requiresOrderBy}");
    }

    /// <summary>
    /// Property: MySQL GenerateLimitOffsetClause should return "LIMIT @limit OFFSET @offset" and requiresOrderBy=false.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(LimitOffsetArbitraries) })]
    public Property MySQL_GenerateLimitOffsetClause_ShouldReturnLimitOffsetSyntax(
        string limitParam, string offsetParam)
    {
        if (string.IsNullOrEmpty(limitParam) || string.IsNullOrEmpty(offsetParam))
            return true.ToProperty();

        // Act
        var result = MySqlProvider.GenerateLimitOffsetClause(limitParam, offsetParam, out bool requiresOrderBy);

        // Assert
        var expectedResult = $"LIMIT {limitParam} OFFSET {offsetParam}";
        var resultMatches = result == expectedResult;
        var orderByNotRequired = !requiresOrderBy;

        return (resultMatches && orderByNotRequired)
            .Label($"MySQL LimitParam: '{limitParam}', OffsetParam: '{offsetParam}', Expected: '{expectedResult}', Got: '{result}', RequiresOrderBy: {requiresOrderBy}");
    }

    /// <summary>
    /// Property: PostgreSQL GenerateLimitOffsetClause should return "LIMIT @limit OFFSET @offset" and requiresOrderBy=false.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(LimitOffsetArbitraries) })]
    public Property PostgreSQL_GenerateLimitOffsetClause_ShouldReturnLimitOffsetSyntax(
        string limitParam, string offsetParam)
    {
        if (string.IsNullOrEmpty(limitParam) || string.IsNullOrEmpty(offsetParam))
            return true.ToProperty();

        // Act
        var result = PostgreSqlProvider.GenerateLimitOffsetClause(limitParam, offsetParam, out bool requiresOrderBy);

        // Assert
        var expectedResult = $"LIMIT {limitParam} OFFSET {offsetParam}";
        var resultMatches = result == expectedResult;
        var orderByNotRequired = !requiresOrderBy;

        return (resultMatches && orderByNotRequired)
            .Label($"PostgreSQL LimitParam: '{limitParam}', OffsetParam: '{offsetParam}', Expected: '{expectedResult}', Got: '{result}', RequiresOrderBy: {requiresOrderBy}");
    }

    /// <summary>
    /// Property: SQL Server GenerateLimitOffsetClause should return "OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY" and requiresOrderBy=true.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(LimitOffsetArbitraries) })]
    public Property SqlServer_GenerateLimitOffsetClause_ShouldReturnOffsetFetchSyntax(
        string limitParam, string offsetParam)
    {
        if (string.IsNullOrEmpty(limitParam) || string.IsNullOrEmpty(offsetParam))
            return true.ToProperty();

        // Act
        var result = SqlServerProvider.GenerateLimitOffsetClause(limitParam, offsetParam, out bool requiresOrderBy);

        // Assert
        var expectedResult = $"OFFSET {offsetParam} ROWS FETCH NEXT {limitParam} ROWS ONLY";
        var resultMatches = result == expectedResult;
        var orderByRequired = requiresOrderBy;

        return (resultMatches && orderByRequired)
            .Label($"SQL Server LimitParam: '{limitParam}', OffsetParam: '{offsetParam}', Expected: '{expectedResult}', Got: '{result}', RequiresOrderBy: {requiresOrderBy}");
    }

    /// <summary>
    /// Property: SQLite GenerateLimitOffsetClause should return "LIMIT @limit OFFSET @offset" and requiresOrderBy=false.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(LimitOffsetArbitraries) })]
    public Property SQLite_GenerateLimitOffsetClause_ShouldReturnLimitOffsetSyntax(
        string limitParam, string offsetParam)
    {
        if (string.IsNullOrEmpty(limitParam) || string.IsNullOrEmpty(offsetParam))
            return true.ToProperty();

        // Act
        var result = SQLiteProvider.GenerateLimitOffsetClause(limitParam, offsetParam, out bool requiresOrderBy);

        // Assert
        var expectedResult = $"LIMIT {limitParam} OFFSET {offsetParam}";
        var resultMatches = result == expectedResult;
        var orderByNotRequired = !requiresOrderBy;

        return (resultMatches && orderByNotRequired)
            .Label($"SQLite LimitParam: '{limitParam}', OffsetParam: '{offsetParam}', Expected: '{expectedResult}', Got: '{result}', RequiresOrderBy: {requiresOrderBy}");
    }

    /// <summary>
    /// Property: LIMIT/OFFSET dialects should not require ORDER BY.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(LimitOffsetArbitraries) })]
    public Property LimitOffsetDialects_ShouldNotRequireOrderBy(
        string limitParam, string offsetParam)
    {
        if (string.IsNullOrEmpty(limitParam) || string.IsNullOrEmpty(offsetParam))
            return true.ToProperty();

        // Test MySQL, PostgreSQL, SQLite
        MySqlProvider.GenerateLimitOffsetClause(limitParam, offsetParam, out bool mysqlRequiresOrderBy);
        PostgreSqlProvider.GenerateLimitOffsetClause(limitParam, offsetParam, out bool pgRequiresOrderBy);
        SQLiteProvider.GenerateLimitOffsetClause(limitParam, offsetParam, out bool sqliteRequiresOrderBy);

        return (!mysqlRequiresOrderBy && !pgRequiresOrderBy && !sqliteRequiresOrderBy)
            .Label($"MySQL: {mysqlRequiresOrderBy}, PostgreSQL: {pgRequiresOrderBy}, SQLite: {sqliteRequiresOrderBy}");
    }

    /// <summary>
    /// Property: FETCH NEXT dialects should require ORDER BY.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(LimitOffsetArbitraries) })]
    public Property FetchNextDialects_ShouldRequireOrderBy(
        string limitParam, string offsetParam)
    {
        if (string.IsNullOrEmpty(limitParam) || string.IsNullOrEmpty(offsetParam))
            return true.ToProperty();

        // Test SQL Server
        SqlServerProvider.GenerateLimitOffsetClause(limitParam, offsetParam, out bool sqlServerRequiresOrderBy);

        return sqlServerRequiresOrderBy
            .Label($"SQL Server RequiresOrderBy: {sqlServerRequiresOrderBy}");
    }

    /// <summary>
    /// Property: GenerateLimitOffsetClause result should contain LIMIT or OFFSET keyword.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(LimitOffsetArbitraries) })]
    public Property GenerateLimitOffsetClause_ShouldContainPaginationKeywords(
        string limitParam, string offsetParam, LimitOffsetDialectProvider provider)
    {
        if (string.IsNullOrEmpty(limitParam) || string.IsNullOrEmpty(offsetParam))
            return true.ToProperty();

        // Act
        var result = provider.GenerateLimitOffsetClause(limitParam, offsetParam, out _);

        // Assert - should contain LIMIT or OFFSET or FETCH
        var containsLimitKeyword = result.Contains("LIMIT");
        var containsOffsetKeyword = result.Contains("OFFSET");
        var containsFetchKeyword = result.Contains("FETCH");

        return (containsLimitKeyword || containsOffsetKeyword || containsFetchKeyword)
            .Label($"Dialect: {provider.DialectName}, Result: '{result}'");
    }

    /// <summary>
    /// Property: GenerateLimitOffsetClause should be consistent for same inputs.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(LimitOffsetArbitraries) })]
    public Property GenerateLimitOffsetClause_ShouldBeConsistent(
        string limitParam, string offsetParam, LimitOffsetDialectProvider provider)
    {
        if (string.IsNullOrEmpty(limitParam) || string.IsNullOrEmpty(offsetParam))
            return true.ToProperty();

        // Act - call twice
        var result1 = provider.GenerateLimitOffsetClause(limitParam, offsetParam, out bool requiresOrderBy1);
        var result2 = provider.GenerateLimitOffsetClause(limitParam, offsetParam, out bool requiresOrderBy2);

        // Assert - should be identical
        return (result1 == result2 && requiresOrderBy1 == requiresOrderBy2)
            .Label($"Dialect: {provider.DialectName}, Result1: '{result1}', Result2: '{result2}'");
    }

    /// <summary>
    /// Property: GenerateLimitOffsetClause with different parameter names should produce different results.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(LimitOffsetArbitraries) })]
    public Property GenerateLimitOffsetClause_DifferentParams_ShouldProduceDifferentResults(
        string limitParam1, string limitParam2, string offsetParam, LimitOffsetDialectProvider provider)
    {
        if (string.IsNullOrEmpty(limitParam1) || string.IsNullOrEmpty(limitParam2) || 
            string.IsNullOrEmpty(offsetParam) || limitParam1 == limitParam2)
            return true.ToProperty();

        // Act
        var result1 = provider.GenerateLimitOffsetClause(limitParam1, offsetParam, out _);
        var result2 = provider.GenerateLimitOffsetClause(limitParam2, offsetParam, out _);

        // Assert - should be different
        return (result1 != result2)
            .Label($"Dialect: {provider.DialectName}, Param1: '{limitParam1}', Param2: '{limitParam2}', Result1: '{result1}', Result2: '{result2}'");
    }
}

/// <summary>
/// Delegate for GenerateLimitOffsetClause method.
/// </summary>
public delegate string GenerateLimitOffsetClauseDelegate(string limitParam, string offsetParam, out bool requiresOrderBy);

/// <summary>
/// Wrapper class for LIMIT/OFFSET dialect provider.
/// </summary>
public class LimitOffsetDialectProvider
{
    private readonly GenerateLimitOffsetClauseDelegate _generateLimitOffsetClause;
    public string DialectName { get; }

    public LimitOffsetDialectProvider(GenerateLimitOffsetClauseDelegate generateLimitOffsetClause, string dialectName)
    {
        _generateLimitOffsetClause = generateLimitOffsetClause;
        DialectName = dialectName;
    }

    public string GenerateLimitOffsetClause(string limitParam, string offsetParam, out bool requiresOrderBy)
    {
        return _generateLimitOffsetClause(limitParam, offsetParam, out requiresOrderBy);
    }

    public override string ToString() => DialectName;
}

/// <summary>
/// Custom arbitraries for LIMIT/OFFSET property tests.
/// </summary>
public static class LimitOffsetArbitraries
{
    private static readonly MySqlDialectProvider MySqlProvider = new();
    private static readonly PostgreSqlDialectProvider PostgreSqlProvider = new();
    private static readonly SqlServerDialectProvider SqlServerProvider = new();
    private static readonly SQLiteDialectProvider SQLiteProvider = new();

    private static readonly char[] FirstChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
    private static readonly char[] RestChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_".ToCharArray();

    /// <summary>
    /// Generates valid parameter names (letters, numbers, underscore, starting with letter or @).
    /// </summary>
    public static Arbitrary<string> String()
    {
        var firstChar = Gen.Elements(FirstChars);
        var restChars = Gen.Elements(RestChars);

        var gen = from size in Gen.Choose(1, 15)
                  from first in firstChar
                  from rest in Gen.ArrayOf(size - 1, restChars)
                  from prefix in Gen.Elements("@", "")
                  select prefix + first + new string(rest);

        return gen.ToArbitrary();
    }

    /// <summary>
    /// Generates LIMIT/OFFSET dialect provider.
    /// </summary>
    public static Arbitrary<LimitOffsetDialectProvider> LimitOffsetDialectProvider()
    {
        return Gen.Elements(
            new LimitOffsetDialectProvider(MySqlProvider.GenerateLimitOffsetClause, "MySQL"),
            new LimitOffsetDialectProvider(PostgreSqlProvider.GenerateLimitOffsetClause, "PostgreSQL"),
            new LimitOffsetDialectProvider(SqlServerProvider.GenerateLimitOffsetClause, "SQL Server"),
            new LimitOffsetDialectProvider(SQLiteProvider.GenerateLimitOffsetClause, "SQLite")
        ).ToArbitrary();
    }
}
