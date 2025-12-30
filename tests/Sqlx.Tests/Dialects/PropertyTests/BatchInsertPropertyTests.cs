// -----------------------------------------------------------------------
// <copyright file="BatchInsertPropertyTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq;
using System.Text.RegularExpressions;
using FsCheck;
using FsCheck.Xunit;
using Xunit;
using Sqlx.Generator;

namespace Sqlx.Tests.Dialects.PropertyTests;

/// <summary>
/// Property-based tests for batch INSERT syntax across all database dialects.
/// **Feature: sql-semantic-tdd-validation, Property 12: Batch INSERT Syntax**
/// **Validates: Requirements 10.1, 10.2, 10.3, 10.4, 10.5**
/// </summary>
public class BatchInsertPropertyTests
{
    private static readonly MySqlDialectProvider MySqlProvider = new();
    private static readonly PostgreSqlDialectProvider PostgreSqlProvider = new();
    private static readonly SqlServerDialectProvider SqlServerProvider = new();
    private static readonly SQLiteDialectProvider SQLiteProvider = new();

    /// <summary>
    /// **Property 12: Batch INSERT Syntax**
    /// *For any* table name, columns, and batch size and *for any* database dialect,
    /// GenerateBatchInsert SHALL return syntactically correct multi-row INSERT.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(BatchInsertArbitraries) })]
    public Property GenerateBatchInsert_ForAnyTableColumnsAndBatchSize_ShouldReturnValidMultiRowInsert(
        string tableName, NonEmptyArray<string> columns, PositiveInt batchSize, BatchDialectProvider provider)
    {
        // Skip if table name or columns are invalid
        if (string.IsNullOrEmpty(tableName))
            return true.ToProperty();

        var columnArray = columns.Get.Where(c => !string.IsNullOrEmpty(c)).Distinct().ToArray();
        if (columnArray.Length == 0)
            return true.ToProperty();

        // Limit batch size to reasonable value for testing
        var actualBatchSize = Math.Min(batchSize.Get, 10);

        // Act
        var result = provider.GenerateBatchInsert(tableName, columnArray, actualBatchSize);

        // Assert - should contain INSERT INTO and VALUES
        var containsInsertInto = result.Contains("INSERT INTO");
        var containsValues = result.Contains("VALUES");

        return (containsInsertInto && containsValues)
            .Label($"Dialect: {provider.DialectName}, Table: '{tableName}', Columns: [{string.Join(", ", columnArray)}], BatchSize: {actualBatchSize}, Result: '{result}'");
    }

    /// <summary>
    /// Property: Batch INSERT should contain correct number of value groups.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(BatchInsertArbitraries) })]
    public Property GenerateBatchInsert_ShouldContainCorrectNumberOfValueGroups(
        string tableName, NonEmptyArray<string> columns, PositiveInt batchSize, BatchDialectProvider provider)
    {
        if (string.IsNullOrEmpty(tableName))
            return true.ToProperty();

        var columnArray = columns.Get.Where(c => !string.IsNullOrEmpty(c)).Distinct().ToArray();
        if (columnArray.Length == 0)
            return true.ToProperty();

        // Limit batch size to reasonable value for testing
        var actualBatchSize = Math.Min(batchSize.Get, 10);

        // Act
        var result = provider.GenerateBatchInsert(tableName, columnArray, actualBatchSize);

        // Count value groups by counting opening parentheses after VALUES
        var valuesIndex = result.IndexOf("VALUES");
        if (valuesIndex < 0)
            return false.Label($"No VALUES found in result: '{result}'");

        var valuesSection = result.Substring(valuesIndex);
        var valueGroupCount = Regex.Matches(valuesSection, @"\(").Count;

        return (valueGroupCount == actualBatchSize)
            .Label($"Dialect: {provider.DialectName}, Expected {actualBatchSize} value groups, got {valueGroupCount}, Result: '{result}'");
    }

    /// <summary>
    /// Property: Batch INSERT should contain all column names.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(BatchInsertArbitraries) })]
    public Property GenerateBatchInsert_ShouldContainAllColumnNames(
        string tableName, NonEmptyArray<string> columns, PositiveInt batchSize, BatchDialectProvider provider)
    {
        if (string.IsNullOrEmpty(tableName))
            return true.ToProperty();

        var columnArray = columns.Get.Where(c => !string.IsNullOrEmpty(c)).Distinct().ToArray();
        if (columnArray.Length == 0)
            return true.ToProperty();

        var actualBatchSize = Math.Min(batchSize.Get, 10);

        // Act
        var result = provider.GenerateBatchInsert(tableName, columnArray, actualBatchSize);

        // Assert - each column should appear in the result (possibly wrapped with quotes)
        var allColumnsPresent = columnArray.All(col => result.Contains(col));

        return allColumnsPresent
            .Label($"Dialect: {provider.DialectName}, Columns: [{string.Join(", ", columnArray)}], Result: '{result}'");
    }

    /// <summary>
    /// Property: MySQL batch INSERT should use multi-row VALUES syntax with @ parameters.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(BatchInsertArbitraries) })]
    public Property MySQL_GenerateBatchInsert_ShouldUseMultiRowValuesWithAtParameters(
        string tableName, NonEmptyArray<string> columns, PositiveInt batchSize)
    {
        if (string.IsNullOrEmpty(tableName))
            return true.ToProperty();

        var columnArray = columns.Get.Where(c => !string.IsNullOrEmpty(c)).Distinct().ToArray();
        if (columnArray.Length == 0)
            return true.ToProperty();

        var actualBatchSize = Math.Min(batchSize.Get, 10);

        // Act
        var result = MySqlProvider.GenerateBatchInsert(tableName, columnArray, actualBatchSize);

        // Assert - should use @ parameters
        var usesAtParameters = result.Contains("@");
        var containsInsertInto = result.Contains("INSERT INTO");
        var containsValues = result.Contains("VALUES");

        return (usesAtParameters && containsInsertInto && containsValues)
            .Label($"MySQL Table: '{tableName}', Result: '{result}'");
    }

    /// <summary>
    /// Property: PostgreSQL batch INSERT should use multi-row VALUES syntax with positional parameters.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(BatchInsertArbitraries) })]
    public Property PostgreSQL_GenerateBatchInsert_ShouldUseMultiRowValuesWithPositionalParameters(
        string tableName, NonEmptyArray<string> columns, PositiveInt batchSize)
    {
        if (string.IsNullOrEmpty(tableName))
            return true.ToProperty();

        var columnArray = columns.Get.Where(c => !string.IsNullOrEmpty(c)).Distinct().ToArray();
        if (columnArray.Length == 0)
            return true.ToProperty();

        var actualBatchSize = Math.Min(batchSize.Get, 10);

        // Act
        var result = PostgreSqlProvider.GenerateBatchInsert(tableName, columnArray, actualBatchSize);

        // Assert - should use $N positional parameters
        var usesPositionalParameters = Regex.IsMatch(result, @"\$\d+");
        var containsInsertInto = result.Contains("INSERT INTO");
        var containsValues = result.Contains("VALUES");

        return (usesPositionalParameters && containsInsertInto && containsValues)
            .Label($"PostgreSQL Table: '{tableName}', Result: '{result}'");
    }

    /// <summary>
    /// Property: SQL Server batch INSERT should use multi-row VALUES syntax.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(BatchInsertArbitraries) })]
    public Property SqlServer_GenerateBatchInsert_ShouldUseMultiRowValuesSyntax(
        string tableName, NonEmptyArray<string> columns, PositiveInt batchSize)
    {
        if (string.IsNullOrEmpty(tableName))
            return true.ToProperty();

        var columnArray = columns.Get.Where(c => !string.IsNullOrEmpty(c)).Distinct().ToArray();
        if (columnArray.Length == 0)
            return true.ToProperty();

        var actualBatchSize = Math.Min(batchSize.Get, 10);

        // Act
        var result = SqlServerProvider.GenerateBatchInsert(tableName, columnArray, actualBatchSize);

        // Assert
        var containsInsertInto = result.Contains("INSERT INTO");
        var containsValues = result.Contains("VALUES");
        var usesAtParameters = result.Contains("@");

        return (containsInsertInto && containsValues && usesAtParameters)
            .Label($"SQL Server Table: '{tableName}', Result: '{result}'");
    }

    /// <summary>
    /// Property: SQLite batch INSERT should use multi-row VALUES syntax.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(BatchInsertArbitraries) })]
    public Property SQLite_GenerateBatchInsert_ShouldUseMultiRowValuesSyntax(
        string tableName, NonEmptyArray<string> columns, PositiveInt batchSize)
    {
        if (string.IsNullOrEmpty(tableName))
            return true.ToProperty();

        var columnArray = columns.Get.Where(c => !string.IsNullOrEmpty(c)).Distinct().ToArray();
        if (columnArray.Length == 0)
            return true.ToProperty();

        var actualBatchSize = Math.Min(batchSize.Get, 10);

        // Act
        var result = SQLiteProvider.GenerateBatchInsert(tableName, columnArray, actualBatchSize);

        // Assert
        var containsInsertInto = result.Contains("INSERT INTO");
        var containsValues = result.Contains("VALUES");
        var usesAtParameters = result.Contains("@");

        return (containsInsertInto && containsValues && usesAtParameters)
            .Label($"SQLite Table: '{tableName}', Result: '{result}'");
    }

    /// <summary>
    /// Property: Batch INSERT should have correct total parameter count.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(BatchInsertArbitraries) })]
    public Property GenerateBatchInsert_ShouldHaveCorrectTotalParameterCount(
        string tableName, NonEmptyArray<string> columns, PositiveInt batchSize, BatchDialectProvider provider)
    {
        if (string.IsNullOrEmpty(tableName))
            return true.ToProperty();

        var columnArray = columns.Get.Where(c => !string.IsNullOrEmpty(c)).Distinct().ToArray();
        if (columnArray.Length == 0)
            return true.ToProperty();

        var actualBatchSize = Math.Min(batchSize.Get, 10);
        var expectedParameterCount = columnArray.Length * actualBatchSize;

        // Act
        var result = provider.GenerateBatchInsert(tableName, columnArray, actualBatchSize);

        // Count parameters based on dialect
        int actualParameterCount;
        if (provider.DialectName == "PostgreSQL")
        {
            actualParameterCount = Regex.Matches(result, @"\$\d+").Count;
        }
        else
        {
            actualParameterCount = Regex.Matches(result, @"@\w+").Count;
        }

        return (actualParameterCount == expectedParameterCount)
            .Label($"Dialect: {provider.DialectName}, Expected {expectedParameterCount} parameters, got {actualParameterCount}, Result: '{result}'");
    }
}

/// <summary>
/// Wrapper class for batch insert dialect provider.
/// </summary>
public class BatchDialectProvider
{
    public Func<string, string[], int, string> GenerateBatchInsert { get; }
    public string DialectName { get; }

    public BatchDialectProvider(Func<string, string[], int, string> generateBatchInsert, string dialectName)
    {
        GenerateBatchInsert = generateBatchInsert;
        DialectName = dialectName;
    }

    public override string ToString() => DialectName;
}

/// <summary>
/// Custom arbitraries for batch INSERT property tests.
/// </summary>
public static class BatchInsertArbitraries
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
        var columnGen = from size in Gen.Choose(1, 15)
                        from first in Gen.Elements(FirstChars)
                        from rest in Gen.ArrayOf(size - 1, Gen.Elements(RestChars))
                        select first + new string(rest);

        return Gen.NonEmptyListOf(columnGen)
            .Select(list => FsCheck.NonEmptyArray<string>.NewNonEmptyArray(list.Take(5).ToArray())) // Limit to 5 columns
            .ToArbitrary();
    }

    /// <summary>
    /// Generates valid positive batch size values.
    /// </summary>
    public static Arbitrary<PositiveInt> PositiveInt()
    {
        return Gen.Choose(1, 10).Select(i => FsCheck.PositiveInt.NewPositiveInt(i)).ToArbitrary();
    }

    /// <summary>
    /// Generates batch dialect provider.
    /// </summary>
    public static Arbitrary<BatchDialectProvider> BatchDialectProvider()
    {
        return Gen.Elements(
            new BatchDialectProvider(MySqlProvider.GenerateBatchInsert, "MySQL"),
            new BatchDialectProvider(PostgreSqlProvider.GenerateBatchInsert, "PostgreSQL"),
            new BatchDialectProvider(SqlServerProvider.GenerateBatchInsert, "SQL Server"),
            new BatchDialectProvider(SQLiteProvider.GenerateBatchInsert, "SQLite")
        ).ToArbitrary();
    }
}
