// -----------------------------------------------------------------------
// <copyright file="PaginationPropertyTests.cs" company="Cricle">
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
/// Property-based tests for pagination syntax across all database dialects.
/// **Feature: sql-semantic-tdd-validation, Property 3: Pagination Syntax Correctness**
/// **Validates: Requirements 3.1, 3.2, 3.3, 3.4, 3.5**
/// </summary>
public class PaginationPropertyTests
{
    private static readonly MySqlDialectProvider MySqlProvider = new();
    private static readonly PostgreSqlDialectProvider PostgreSqlProvider = new();
    private static readonly SqlServerDialectProvider SqlServerProvider = new();
    private static readonly SQLiteDialectProvider SQLiteProvider = new();

    /// <summary>
    /// **Property 3: Pagination Syntax Correctness**
    /// *For any* valid limit and offset values and *for any* database dialect,
    /// GenerateLimitClause SHALL return syntactically correct pagination SQL.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(PaginationArbitraries) })]
    public Property GenerateLimitClause_ForAnyValidLimitAndOffset_ShouldReturnSyntacticallyCorrectSQL(
        PositiveInt limit, NonNegativeInt offset, DialectProviderWithConfig providerConfig)
    {
        var config = providerConfig.Config;

        // Act
        var result = providerConfig.GenerateLimitClause(limit.Get, offset.Get);

        // Assert based on dialect type
        if (config.UsesLimitOffset)
        {
            // MySQL, PostgreSQL, SQLite use LIMIT n OFFSET m
            return (result.Contains("LIMIT") && result.Contains("OFFSET"))
                .Label($"Dialect: {config.DialectName}, Limit: {limit.Get}, Offset: {offset.Get}, Result: '{result}'");
        }
        else
        {
            // SQL Server, Oracle use OFFSET m ROWS FETCH NEXT n ROWS ONLY
            return (result.Contains("OFFSET") && result.Contains("FETCH") && result.Contains("ROWS"))
                .Label($"Dialect: {config.DialectName}, Limit: {limit.Get}, Offset: {offset.Get}, Result: '{result}'");
        }
    }

    /// <summary>
    /// Property: GenerateLimitClause with limit only should return valid SQL.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(PaginationArbitraries) })]
    public Property GenerateLimitClause_WithLimitOnly_ShouldReturnValidSQL(
        PositiveInt limit, DialectProviderWithConfig providerConfig)
    {
        var config = providerConfig.Config;

        // Act
        var result = providerConfig.GenerateLimitClause(limit.Get, null);

        // Assert - should contain limit-related keywords
        if (config.UsesLimitOffset)
        {
            return (result.Contains("LIMIT") || result.Contains(limit.Get.ToString()))
                .Label($"Dialect: {config.DialectName}, Limit: {limit.Get}, Result: '{result}'");
        }
        else
        {
            // SQL Server uses OFFSET 0 ROWS FETCH NEXT n ROWS ONLY for limit only
            return (result.Contains("FETCH") || result.Contains("OFFSET"))
                .Label($"Dialect: {config.DialectName}, Limit: {limit.Get}, Result: '{result}'");
        }
    }

    /// <summary>
    /// Property: GenerateLimitClause with null limit and null offset should return empty string.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(PaginationArbitraries) })]
    public Property GenerateLimitClause_WithNullLimitAndOffset_ShouldReturnEmpty(
        DialectProviderWithConfig providerConfig)
    {
        // Act
        var result = providerConfig.GenerateLimitClause(null, null);

        // Assert
        return string.IsNullOrEmpty(result)
            .Label($"Dialect: {providerConfig.Config.DialectName}, Result: '{result}'");
    }

    /// <summary>
    /// Property: MySQL pagination should use LIMIT n OFFSET m syntax.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(PaginationArbitraries) })]
    public Property MySQL_GenerateLimitClause_ShouldUseLimitOffsetSyntax(
        PositiveInt limit, NonNegativeInt offset)
    {
        // Act
        var result = MySqlProvider.GenerateLimitClause(limit.Get, offset.Get);

        // Assert
        return (result == $"LIMIT {limit.Get} OFFSET {offset.Get}")
            .Label($"MySQL Limit: {limit.Get}, Offset: {offset.Get}, Result: '{result}'");
    }

    /// <summary>
    /// Property: PostgreSQL pagination should use LIMIT n OFFSET m syntax.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(PaginationArbitraries) })]
    public Property PostgreSQL_GenerateLimitClause_ShouldUseLimitOffsetSyntax(
        PositiveInt limit, NonNegativeInt offset)
    {
        // Act
        var result = PostgreSqlProvider.GenerateLimitClause(limit.Get, offset.Get);

        // Assert
        return (result.Contains($"LIMIT {limit.Get}") && result.Contains($"OFFSET {offset.Get}"))
            .Label($"PostgreSQL Limit: {limit.Get}, Offset: {offset.Get}, Result: '{result}'");
    }

    /// <summary>
    /// Property: SQL Server pagination should use OFFSET m ROWS FETCH NEXT n ROWS ONLY syntax.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(PaginationArbitraries) })]
    public Property SqlServer_GenerateLimitClause_ShouldUseOffsetFetchSyntax(
        PositiveInt limit, NonNegativeInt offset)
    {
        // Act
        var result = SqlServerProvider.GenerateLimitClause(limit.Get, offset.Get);

        // Assert
        return (result.Contains($"OFFSET {offset.Get} ROWS") && 
                result.Contains($"FETCH NEXT {limit.Get} ROWS ONLY"))
            .Label($"SQL Server Limit: {limit.Get}, Offset: {offset.Get}, Result: '{result}'");
    }

    /// <summary>
    /// Property: SQLite pagination should use LIMIT n OFFSET m syntax.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(PaginationArbitraries) })]
    public Property SQLite_GenerateLimitClause_ShouldUseLimitOffsetSyntax(
        PositiveInt limit, NonNegativeInt offset)
    {
        // Act
        var result = SQLiteProvider.GenerateLimitClause(limit.Get, offset.Get);

        // Assert
        return (result == $"LIMIT {limit.Get} OFFSET {offset.Get}")
            .Label($"SQLite Limit: {limit.Get}, Offset: {offset.Get}, Result: '{result}'");
    }

    /// <summary>
    /// Property: Pagination result should contain the actual limit value.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(PaginationArbitraries) })]
    public Property GenerateLimitClause_ShouldContainLimitValue(
        PositiveInt limit, NonNegativeInt offset, DialectProviderWithConfig providerConfig)
    {
        // Act
        var result = providerConfig.GenerateLimitClause(limit.Get, offset.Get);

        // Assert
        return result.Contains(limit.Get.ToString())
            .Label($"Dialect: {providerConfig.Config.DialectName}, Limit: {limit.Get}, Result: '{result}'");
    }

    /// <summary>
    /// Property: Pagination result should contain the actual offset value.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(PaginationArbitraries) })]
    public Property GenerateLimitClause_ShouldContainOffsetValue(
        PositiveInt limit, NonNegativeInt offset, DialectProviderWithConfig providerConfig)
    {
        // Act
        var result = providerConfig.GenerateLimitClause(limit.Get, offset.Get);

        // Assert
        return result.Contains(offset.Get.ToString())
            .Label($"Dialect: {providerConfig.Config.DialectName}, Offset: {offset.Get}, Result: '{result}'");
    }
}

/// <summary>
/// **Property 4: MySQL OFFSET Requires LIMIT**
/// *For any* offset value without limit in MySQL, GenerateLimitClause SHALL throw ArgumentException.
/// **Validates: Requirements 3.6**
/// </summary>
public class MySqlOffsetRequiresLimitPropertyTests
{
    private static readonly MySqlDialectProvider MySqlProvider = new();

    /// <summary>
    /// **Property 4: MySQL OFFSET Requires LIMIT**
    /// *For any* offset value without a limit value in MySQL, 
    /// GenerateLimitClause SHALL throw ArgumentException.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(PaginationArbitraries) })]
    public Property MySQL_GenerateLimitClause_WithOffsetOnly_ShouldThrowArgumentException(
        NonNegativeInt offset)
    {
        // Act & Assert
        try
        {
            var result = MySqlProvider.GenerateLimitClause(null, offset.Get);
            // If we get here without exception, the property fails
            return false.Label($"Expected ArgumentException but got result: '{result}'");
        }
        catch (ArgumentException)
        {
            // Expected behavior
            return true.Label($"Correctly threw ArgumentException for offset: {offset.Get}");
        }
        catch (Exception ex)
        {
            // Wrong exception type
            return false.Label($"Expected ArgumentException but got {ex.GetType().Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Property: MySQL with limit and offset should not throw.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(PaginationArbitraries) })]
    public Property MySQL_GenerateLimitClause_WithLimitAndOffset_ShouldNotThrow(
        PositiveInt limit, NonNegativeInt offset)
    {
        // Act & Assert
        try
        {
            var result = MySqlProvider.GenerateLimitClause(limit.Get, offset.Get);
            return (!string.IsNullOrEmpty(result))
                .Label($"Result: '{result}'");
        }
        catch (Exception ex)
        {
            return false.Label($"Unexpected exception: {ex.GetType().Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Property: MySQL with limit only should not throw.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(PaginationArbitraries) })]
    public Property MySQL_GenerateLimitClause_WithLimitOnly_ShouldNotThrow(PositiveInt limit)
    {
        // Act & Assert
        try
        {
            var result = MySqlProvider.GenerateLimitClause(limit.Get, null);
            return (!string.IsNullOrEmpty(result))
                .Label($"Result: '{result}'");
        }
        catch (Exception ex)
        {
            return false.Label($"Unexpected exception: {ex.GetType().Name}: {ex.Message}");
        }
    }
}

/// <summary>
/// Wrapper class to pair a dialect provider with its test configuration.
/// Uses a delegate to invoke the GenerateLimitClause method since providers are internal.
/// </summary>
public class DialectProviderWithConfig
{
    public Func<int?, int?, string> GenerateLimitClause { get; }
    public Func<string, string[], string> GenerateInsertWithReturning { get; }
    public DialectTestConfig Config { get; }

    public DialectProviderWithConfig(
        Func<int?, int?, string> generateLimitClause,
        Func<string, string[], string> generateInsertWithReturning,
        DialectTestConfig config)
    {
        GenerateLimitClause = generateLimitClause;
        GenerateInsertWithReturning = generateInsertWithReturning;
        Config = config;
    }

    public override string ToString() => Config.DialectName;
}

/// <summary>
/// Custom arbitraries for pagination property tests.
/// </summary>
public static class PaginationArbitraries
{
    private static readonly MySqlDialectProvider MySqlProvider = new();
    private static readonly PostgreSqlDialectProvider PostgreSqlProvider = new();
    private static readonly SqlServerDialectProvider SqlServerProvider = new();
    private static readonly SQLiteDialectProvider SQLiteProvider = new();

    /// <summary>
    /// Generates dialect provider with its corresponding test configuration.
    /// </summary>
    public static Arbitrary<DialectProviderWithConfig> DialectProviderWithConfig()
    {
        return Gen.Elements(
            new DialectProviderWithConfig(
                MySqlProvider.GenerateLimitClause,
                MySqlProvider.GenerateInsertWithReturning,
                DialectTestConfig.MySql),
            new DialectProviderWithConfig(
                PostgreSqlProvider.GenerateLimitClause,
                PostgreSqlProvider.GenerateInsertWithReturning,
                DialectTestConfig.PostgreSql),
            new DialectProviderWithConfig(
                SqlServerProvider.GenerateLimitClause,
                SqlServerProvider.GenerateInsertWithReturning,
                DialectTestConfig.SqlServer),
            new DialectProviderWithConfig(
                SQLiteProvider.GenerateLimitClause,
                SQLiteProvider.GenerateInsertWithReturning,
                DialectTestConfig.SQLite)
        ).ToArbitrary();
    }

    /// <summary>
    /// Generates valid positive limit values.
    /// </summary>
    public static Arbitrary<PositiveInt> PositiveInt()
    {
        return Gen.Choose(1, 10000).Select(i => FsCheck.PositiveInt.NewPositiveInt(i)).ToArbitrary();
    }

    /// <summary>
    /// Generates valid non-negative offset values.
    /// </summary>
    public static Arbitrary<NonNegativeInt> NonNegativeInt()
    {
        return Gen.Choose(0, 100000).Select(i => FsCheck.NonNegativeInt.NewNonNegativeInt(i)).ToArbitrary();
    }
}


/// <summary>
/// Property-based tests for INSERT with returning ID syntax across all database dialects.
/// **Feature: sql-semantic-tdd-validation, Property 5: INSERT Returning ID Syntax**
/// **Validates: Requirements 4.1, 4.2, 4.3, 4.4, 4.5**
/// </summary>
public class InsertReturningIdPropertyTests
{
    private static readonly MySqlDialectProvider MySqlProvider = new();
    private static readonly PostgreSqlDialectProvider PostgreSqlProvider = new();
    private static readonly SqlServerDialectProvider SqlServerProvider = new();
    private static readonly SQLiteDialectProvider SQLiteProvider = new();

    /// <summary>
    /// **Property 5: INSERT Returning ID Syntax**
    /// *For any* table name and columns and *for any* database dialect,
    /// GenerateInsertWithReturning SHALL return syntactically correct INSERT statement.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(InsertArbitraries) })]
    public Property GenerateInsertWithReturning_ForAnyTableAndColumns_ShouldReturnValidInsertSQL(
        string tableName, NonEmptyArray<string> columns, DialectProviderWithConfig providerConfig)
    {
        // Skip if table name or columns are invalid
        if (string.IsNullOrEmpty(tableName) || columns.Get.Length == 0)
            return true.ToProperty();

        var config = providerConfig.Config;
        var columnArray = columns.Get.Where(c => !string.IsNullOrEmpty(c)).ToArray();
        
        if (columnArray.Length == 0)
            return true.ToProperty();

        // Act
        var result = providerConfig.GenerateInsertWithReturning(tableName, columnArray);

        // Assert - should contain INSERT INTO and VALUES
        return (result.Contains("INSERT INTO") && result.Contains("VALUES"))
            .Label($"Dialect: {config.DialectName}, Table: '{tableName}', Columns: [{string.Join(", ", columnArray)}], Result: '{result}'");
    }

    /// <summary>
    /// Property: MySQL INSERT with returning should use LAST_INSERT_ID().
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(InsertArbitraries) })]
    public Property MySQL_GenerateInsertWithReturning_ShouldUseLastInsertId(
        string tableName, NonEmptyArray<string> columns)
    {
        if (string.IsNullOrEmpty(tableName))
            return true.ToProperty();

        var columnArray = columns.Get.Where(c => !string.IsNullOrEmpty(c)).ToArray();
        if (columnArray.Length == 0)
            return true.ToProperty();

        // Act
        var result = MySqlProvider.GenerateInsertWithReturning(tableName, columnArray);

        // Assert
        return (result.Contains("INSERT INTO") && 
                result.Contains("VALUES") && 
                result.Contains("LAST_INSERT_ID()"))
            .Label($"MySQL Table: '{tableName}', Result: '{result}'");
    }

    /// <summary>
    /// Property: PostgreSQL INSERT with returning should use RETURNING clause.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(InsertArbitraries) })]
    public Property PostgreSQL_GenerateInsertWithReturning_ShouldUseReturningClause(
        string tableName, NonEmptyArray<string> columns)
    {
        if (string.IsNullOrEmpty(tableName))
            return true.ToProperty();

        var columnArray = columns.Get.Where(c => !string.IsNullOrEmpty(c)).ToArray();
        if (columnArray.Length == 0)
            return true.ToProperty();

        // Act
        var result = PostgreSqlProvider.GenerateInsertWithReturning(tableName, columnArray);

        // Assert
        return (result.Contains("INSERT INTO") && 
                result.Contains("VALUES") && 
                result.Contains("RETURNING"))
            .Label($"PostgreSQL Table: '{tableName}', Result: '{result}'");
    }

    /// <summary>
    /// Property: SQL Server INSERT with returning should use OUTPUT INSERTED clause.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(InsertArbitraries) })]
    public Property SqlServer_GenerateInsertWithReturning_ShouldUseOutputInserted(
        string tableName, NonEmptyArray<string> columns)
    {
        if (string.IsNullOrEmpty(tableName))
            return true.ToProperty();

        var columnArray = columns.Get.Where(c => !string.IsNullOrEmpty(c)).ToArray();
        if (columnArray.Length == 0)
            return true.ToProperty();

        // Act
        var result = SqlServerProvider.GenerateInsertWithReturning(tableName, columnArray);

        // Assert
        return (result.Contains("INSERT INTO") && 
                result.Contains("VALUES") && 
                result.Contains("OUTPUT INSERTED"))
            .Label($"SQL Server Table: '{tableName}', Result: '{result}'");
    }

    /// <summary>
    /// Property: SQLite INSERT with returning should use last_insert_rowid().
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(InsertArbitraries) })]
    public Property SQLite_GenerateInsertWithReturning_ShouldUseLastInsertRowid(
        string tableName, NonEmptyArray<string> columns)
    {
        if (string.IsNullOrEmpty(tableName))
            return true.ToProperty();

        var columnArray = columns.Get.Where(c => !string.IsNullOrEmpty(c)).ToArray();
        if (columnArray.Length == 0)
            return true.ToProperty();

        // Act
        var result = SQLiteProvider.GenerateInsertWithReturning(tableName, columnArray);

        // Assert
        return (result.Contains("INSERT INTO") && 
                result.Contains("VALUES") && 
                result.Contains("last_insert_rowid()"))
            .Label($"SQLite Table: '{tableName}', Result: '{result}'");
    }

    /// <summary>
    /// Property: INSERT statement should contain all column names.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(InsertArbitraries) })]
    public Property GenerateInsertWithReturning_ShouldContainAllColumns(
        string tableName, NonEmptyArray<string> columns, DialectProviderWithConfig providerConfig)
    {
        if (string.IsNullOrEmpty(tableName))
            return true.ToProperty();

        var columnArray = columns.Get.Where(c => !string.IsNullOrEmpty(c)).ToArray();
        if (columnArray.Length == 0)
            return true.ToProperty();

        // Act
        var result = providerConfig.GenerateInsertWithReturning(tableName, columnArray);

        // Assert - each column should appear in the result (possibly wrapped with quotes)
        var allColumnsPresent = columnArray.All(col => result.Contains(col));
        
        return allColumnsPresent
            .Label($"Dialect: {providerConfig.Config.DialectName}, Columns: [{string.Join(", ", columnArray)}], Result: '{result}'");
    }

    /// <summary>
    /// Property: INSERT statement should have matching number of parameters and columns.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(InsertArbitraries) })]
    public Property GenerateInsertWithReturning_ShouldHaveMatchingParameterCount(
        string tableName, NonEmptyArray<string> columns, DialectProviderWithConfig providerConfig)
    {
        if (string.IsNullOrEmpty(tableName))
            return true.ToProperty();

        var columnArray = columns.Get.Where(c => !string.IsNullOrEmpty(c)).ToArray();
        if (columnArray.Length == 0)
            return true.ToProperty();

        // Act
        var result = providerConfig.GenerateInsertWithReturning(tableName, columnArray);

        // Count parameters (@ or $ prefixed)
        var parameterCount = result.Split(new[] { '@', '$' }, StringSplitOptions.RemoveEmptyEntries).Length - 1;
        
        // For PostgreSQL, count $1, $2, etc.
        if (providerConfig.Config.DialectName == "PostgreSQL")
        {
            parameterCount = System.Text.RegularExpressions.Regex.Matches(result, @"\$\d+").Count;
        }
        else
        {
            parameterCount = System.Text.RegularExpressions.Regex.Matches(result, @"@\w+").Count;
        }

        return (parameterCount == columnArray.Length)
            .Label($"Dialect: {providerConfig.Config.DialectName}, Columns: {columnArray.Length}, Parameters: {parameterCount}, Result: '{result}'");
    }
}

/// <summary>
/// Custom arbitraries for INSERT property tests.
/// </summary>
public static class InsertArbitraries
{
    private static readonly MySqlDialectProvider MySqlProvider = new();
    private static readonly PostgreSqlDialectProvider PostgreSqlProvider = new();
    private static readonly SqlServerDialectProvider SqlServerProvider = new();
    private static readonly SQLiteDialectProvider SQLiteProvider = new();

    private static readonly char[] FirstChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
    private static readonly char[] RestChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_".ToCharArray();

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
    /// Generates non-empty arrays of valid column names.
    /// </summary>
    public static Arbitrary<NonEmptyArray<string>> NonEmptyArray()
    {
        var columnGen = from size in Gen.Choose(1, 20)
                        from first in Gen.Elements(FirstChars)
                        from rest in Gen.ArrayOf(size - 1, Gen.Elements(RestChars))
                        select first + new string(rest);

        return Gen.NonEmptyListOf(columnGen)
            .Select(list => FsCheck.NonEmptyArray<string>.NewNonEmptyArray(list.ToArray()))
            .ToArbitrary();
    }

    /// <summary>
    /// Generates dialect provider with its corresponding test configuration.
    /// </summary>
    public static Arbitrary<DialectProviderWithConfig> DialectProviderWithConfig()
    {
        return Gen.Elements(
            new DialectProviderWithConfig(
                MySqlProvider.GenerateLimitClause,
                MySqlProvider.GenerateInsertWithReturning,
                DialectTestConfig.MySql),
            new DialectProviderWithConfig(
                PostgreSqlProvider.GenerateLimitClause,
                PostgreSqlProvider.GenerateInsertWithReturning,
                DialectTestConfig.PostgreSql),
            new DialectProviderWithConfig(
                SqlServerProvider.GenerateLimitClause,
                SqlServerProvider.GenerateInsertWithReturning,
                DialectTestConfig.SqlServer),
            new DialectProviderWithConfig(
                SQLiteProvider.GenerateLimitClause,
                SQLiteProvider.GenerateInsertWithReturning,
                DialectTestConfig.SQLite)
        ).ToArbitrary();
    }
}
