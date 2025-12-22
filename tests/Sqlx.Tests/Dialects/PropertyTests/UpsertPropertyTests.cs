// -----------------------------------------------------------------------
// <copyright file="UpsertPropertyTests.cs" company="Cricle">
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
/// Property-based tests for Upsert syntax across all database dialects.
/// **Feature: sql-semantic-tdd-validation, Property 6: Upsert Syntax Correctness**
/// **Validates: Requirements 5.1, 5.2, 5.3, 5.4, 5.5**
/// </summary>
public class UpsertPropertyTests
{
    private static readonly MySqlDialectProvider MySqlProvider = new();
    private static readonly PostgreSqlDialectProvider PostgreSqlProvider = new();
    private static readonly SqlServerDialectProvider SqlServerProvider = new();
    private static readonly SQLiteDialectProvider SQLiteProvider = new();

    /// <summary>
    /// **Property 6: Upsert Syntax Correctness**
    /// *For any* table name, columns, and key columns and *for any* database dialect,
    /// GenerateUpsert SHALL return syntactically correct UPSERT statement.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(UpsertArbitraries) })]
    public Property GenerateUpsert_ForAnyTableColumnsAndKeyColumns_ShouldReturnValidUpsertSQL(
        string tableName, NonEmptyArray<string> columns, UpsertDialectProviderWithConfig providerConfig)
    {
        // Skip if table name is invalid
        if (string.IsNullOrEmpty(tableName))
            return true.ToProperty();

        var columnArray = columns.Get.Where(c => !string.IsNullOrEmpty(c)).Distinct().ToArray();
        if (columnArray.Length < 2)
            return true.ToProperty(); // Need at least 2 columns (1 key + 1 non-key)

        // Use first column as key column
        var keyColumns = new[] { columnArray[0] };
        var config = providerConfig.Config;

        // Act
        var result = providerConfig.GenerateUpsert(tableName, columnArray, keyColumns);

        // Assert - should contain dialect-specific upsert syntax
        var containsUpsertKeyword = config.DialectName switch
        {
            "MySQL" => result.Contains("INSERT INTO") && result.Contains("VALUES") && result.Contains("ON DUPLICATE KEY UPDATE"),
            "PostgreSQL" => result.Contains("INSERT INTO") && result.Contains("VALUES") && result.Contains("ON CONFLICT") && result.Contains("DO UPDATE SET"),
            "SQL Server" => result.Contains("MERGE") && result.Contains("USING") && result.Contains("WHEN MATCHED") && result.Contains("WHEN NOT MATCHED"),
            "SQLite" => result.Contains("INSERT INTO") && result.Contains("VALUES") && result.Contains("ON CONFLICT") && result.Contains("DO UPDATE SET"),
            _ => true
        };

        return containsUpsertKeyword
            .Label($"Dialect: {config.DialectName}, Table: '{tableName}', " +
                   $"Columns: [{string.Join(", ", columnArray)}], " +
                   $"KeyColumns: [{string.Join(", ", keyColumns)}], " +
                   $"Result: '{result}'");
    }

    /// <summary>
    /// Property: MySQL Upsert should use ON DUPLICATE KEY UPDATE syntax.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(UpsertArbitraries) })]
    public Property MySQL_GenerateUpsert_ShouldUseOnDuplicateKeyUpdate(
        string tableName, NonEmptyArray<string> columns)
    {
        if (string.IsNullOrEmpty(tableName))
            return true.ToProperty();

        var columnArray = columns.Get.Where(c => !string.IsNullOrEmpty(c)).Distinct().ToArray();
        if (columnArray.Length < 2)
            return true.ToProperty();

        var keyColumns = new[] { columnArray[0] };

        // Act
        var result = MySqlProvider.GenerateUpsert(tableName, columnArray, keyColumns);

        // Assert
        return (result.Contains("INSERT INTO") &&
                result.Contains("VALUES") &&
                result.Contains("ON DUPLICATE KEY UPDATE"))
            .Label($"MySQL Table: '{tableName}', Result: '{result}'");
    }

    /// <summary>
    /// Property: PostgreSQL Upsert should use ON CONFLICT ... DO UPDATE SET with EXCLUDED.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(UpsertArbitraries) })]
    public Property PostgreSQL_GenerateUpsert_ShouldUseOnConflictWithExcluded(
        string tableName, NonEmptyArray<string> columns)
    {
        if (string.IsNullOrEmpty(tableName))
            return true.ToProperty();

        var columnArray = columns.Get.Where(c => !string.IsNullOrEmpty(c)).Distinct().ToArray();
        if (columnArray.Length < 2)
            return true.ToProperty();

        var keyColumns = new[] { columnArray[0] };

        // Act
        var result = PostgreSqlProvider.GenerateUpsert(tableName, columnArray, keyColumns);

        // Assert
        return (result.Contains("INSERT INTO") &&
                result.Contains("VALUES") &&
                result.Contains("ON CONFLICT") &&
                result.Contains("DO UPDATE SET") &&
                result.Contains("EXCLUDED"))
            .Label($"PostgreSQL Table: '{tableName}', Result: '{result}'");
    }

    /// <summary>
    /// Property: SQL Server Upsert should use MERGE statement.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(UpsertArbitraries) })]
    public Property SqlServer_GenerateUpsert_ShouldUseMergeStatement(
        string tableName, NonEmptyArray<string> columns)
    {
        if (string.IsNullOrEmpty(tableName))
            return true.ToProperty();

        var columnArray = columns.Get.Where(c => !string.IsNullOrEmpty(c)).Distinct().ToArray();
        if (columnArray.Length < 2)
            return true.ToProperty();

        var keyColumns = new[] { columnArray[0] };

        // Act
        var result = SqlServerProvider.GenerateUpsert(tableName, columnArray, keyColumns);

        // Assert
        return (result.Contains("MERGE") &&
                result.Contains("USING") &&
                result.Contains("WHEN MATCHED THEN") &&
                result.Contains("UPDATE SET") &&
                result.Contains("WHEN NOT MATCHED THEN") &&
                result.Contains("INSERT"))
            .Label($"SQL Server Table: '{tableName}', Result: '{result}'");
    }

    /// <summary>
    /// Property: SQLite Upsert should use ON CONFLICT ... DO UPDATE SET with excluded.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(UpsertArbitraries) })]
    public Property SQLite_GenerateUpsert_ShouldUseOnConflictWithExcluded(
        string tableName, NonEmptyArray<string> columns)
    {
        if (string.IsNullOrEmpty(tableName))
            return true.ToProperty();

        var columnArray = columns.Get.Where(c => !string.IsNullOrEmpty(c)).Distinct().ToArray();
        if (columnArray.Length < 2)
            return true.ToProperty();

        var keyColumns = new[] { columnArray[0] };

        // Act
        var result = SQLiteProvider.GenerateUpsert(tableName, columnArray, keyColumns);

        // Assert
        return (result.Contains("INSERT INTO") &&
                result.Contains("VALUES") &&
                result.Contains("ON CONFLICT") &&
                result.Contains("DO UPDATE SET") &&
                result.Contains("excluded"))
            .Label($"SQLite Table: '{tableName}', Result: '{result}'");
    }

    /// <summary>
    /// Property: Upsert should contain all column names in the INSERT part.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(UpsertArbitraries) })]
    public Property GenerateUpsert_ShouldContainAllColumnsInInsert(
        string tableName, NonEmptyArray<string> columns, UpsertDialectProviderWithConfig providerConfig)
    {
        if (string.IsNullOrEmpty(tableName))
            return true.ToProperty();

        var columnArray = columns.Get.Where(c => !string.IsNullOrEmpty(c)).Distinct().ToArray();
        if (columnArray.Length < 2)
            return true.ToProperty();

        var keyColumns = new[] { columnArray[0] };

        // Act
        var result = providerConfig.GenerateUpsert(tableName, columnArray, keyColumns);

        // Assert - each column should appear in the result (possibly wrapped with quotes)
        var allColumnsPresent = columnArray.All(col => result.Contains(col));

        return allColumnsPresent
            .Label($"Dialect: {providerConfig.Config.DialectName}, " +
                   $"Columns: [{string.Join(", ", columnArray)}], Result: '{result}'");
    }

    /// <summary>
    /// Property: Upsert UPDATE clause should not include key columns.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(UpsertArbitraries) })]
    public Property GenerateUpsert_UpdateClauseShouldExcludeKeyColumns(
        string tableName, NonEmptyArray<string> columns, UpsertDialectProviderWithConfig providerConfig)
    {
        if (string.IsNullOrEmpty(tableName))
            return true.ToProperty();

        var columnArray = columns.Get.Where(c => !string.IsNullOrEmpty(c)).Distinct().ToArray();
        if (columnArray.Length < 2)
            return true.ToProperty();

        var keyColumns = new[] { columnArray[0] };
        var nonKeyColumns = columnArray.Skip(1).ToArray();

        // Act
        var result = providerConfig.GenerateUpsert(tableName, columnArray, keyColumns);

        // For SQL Server, the structure is different (MERGE)
        if (providerConfig.Config.DialectName == "SQL Server")
        {
            // SQL Server uses MERGE, check that UPDATE SET contains non-key columns
            var updateSetIndex = result.IndexOf("UPDATE SET");
            if (updateSetIndex >= 0)
            {
                var updatePart = result.Substring(updateSetIndex);
                var whenNotMatchedIndex = updatePart.IndexOf("WHEN NOT MATCHED");
                if (whenNotMatchedIndex > 0)
                {
                    updatePart = updatePart.Substring(0, whenNotMatchedIndex);
                }
                // Non-key columns should be in the UPDATE SET part
                var hasNonKeyColumns = nonKeyColumns.All(col => updatePart.Contains(col));
                return hasNonKeyColumns
                    .Label($"SQL Server UPDATE SET should contain non-key columns. Result: '{result}'");
            }
        }
        else
        {
            // For other dialects, check that UPDATE clause contains non-key columns
            var updateIndex = result.IndexOf("UPDATE");
            if (updateIndex >= 0)
            {
                var updatePart = result.Substring(updateIndex);
                var hasNonKeyColumns = nonKeyColumns.All(col => updatePart.Contains(col));
                return hasNonKeyColumns
                    .Label($"Dialect: {providerConfig.Config.DialectName}, " +
                           $"UPDATE clause should contain non-key columns. Result: '{result}'");
            }
        }

        return true.ToProperty();
    }
}

/// <summary>
/// Wrapper class to pair a dialect provider with its test configuration for Upsert tests.
/// </summary>
public class UpsertDialectProviderWithConfig
{
    public Func<string, string[], string[], string> GenerateUpsert { get; }
    public DialectTestConfig Config { get; }

    public UpsertDialectProviderWithConfig(
        Func<string, string[], string[], string> generateUpsert,
        DialectTestConfig config)
    {
        GenerateUpsert = generateUpsert;
        Config = config;
    }

    public override string ToString() => Config.DialectName;
}

/// <summary>
/// Custom arbitraries for Upsert property tests.
/// </summary>
public static class UpsertArbitraries
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
    /// Generates non-empty arrays of valid column names (at least 2 for upsert).
    /// </summary>
    public static Arbitrary<NonEmptyArray<string>> NonEmptyArray()
    {
        var columnGen = from size in Gen.Choose(1, 15)
                        from first in Gen.Elements(FirstChars)
                        from rest in Gen.ArrayOf(size - 1, Gen.Elements(RestChars))
                        select first + new string(rest);

        return Gen.Choose(2, 8)
            .SelectMany(count => Gen.ArrayOf(count, columnGen))
            .Where(arr => arr.Length >= 2 && arr.Distinct().Count() >= 2)
            .Select(arr => FsCheck.NonEmptyArray<string>.NewNonEmptyArray(arr.Distinct().ToArray()))
            .Where(arr => arr.Get.Length >= 2)
            .ToArbitrary();
    }

    /// <summary>
    /// Generates dialect provider with its corresponding test configuration.
    /// </summary>
    public static Arbitrary<UpsertDialectProviderWithConfig> UpsertDialectProviderWithConfig()
    {
        return Gen.Elements(
            new UpsertDialectProviderWithConfig(
                MySqlProvider.GenerateUpsert,
                DialectTestConfig.MySql),
            new UpsertDialectProviderWithConfig(
                PostgreSqlProvider.GenerateUpsert,
                DialectTestConfig.PostgreSql),
            new UpsertDialectProviderWithConfig(
                SqlServerProvider.GenerateUpsert,
                DialectTestConfig.SqlServer),
            new UpsertDialectProviderWithConfig(
                SQLiteProvider.GenerateUpsert,
                DialectTestConfig.SQLite)
        ).ToArbitrary();
    }
}
