// -----------------------------------------------------------------------
// <copyright file="DateTimeFormattingPropertyTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Globalization;
using System.Text.RegularExpressions;
using FsCheck;
using FsCheck.Xunit;
using Xunit;
using Sqlx.Generator;

namespace Sqlx.Tests.Dialects.PropertyTests;

/// <summary>
/// Property-based tests for DateTime formatting across all database dialects.
/// **Feature: sql-semantic-tdd-validation, Property 13: DateTime Formatting**
/// **Validates: Requirements 11.1, 11.2, 11.3, 11.4, 11.5**
/// </summary>
public class DateTimeFormattingPropertyTests
{
    private static readonly MySqlDialectProvider MySqlProvider = new();
    private static readonly PostgreSqlDialectProvider PostgreSqlProvider = new();
    private static readonly SqlServerDialectProvider SqlServerProvider = new();
    private static readonly SQLiteDialectProvider SQLiteProvider = new();

    /// <summary>
    /// **Property 13: DateTime Formatting**
    /// *For any* DateTime value and *for any* database dialect,
    /// FormatDateTime SHALL return properly formatted date string.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(DateTimeArbitraries) })]
    public Property FormatDateTime_ForAnyDateTimeAndDialect_ShouldReturnProperlyFormattedString(
        DateTime dateTime, DateTimeDialectProvider provider)
    {
        // Act
        var result = provider.FormatDateTime(dateTime);

        // Assert - should be non-empty and contain date components
        var isNonEmpty = !string.IsNullOrEmpty(result);
        var containsYear = result.Contains(dateTime.Year.ToString());
        var containsMonth = result.Contains(dateTime.Month.ToString("D2"));
        var containsDay = result.Contains(dateTime.Day.ToString("D2"));

        return (isNonEmpty && containsYear && containsMonth && containsDay)
            .Label($"Dialect: {provider.DialectName}, DateTime: {dateTime}, Result: '{result}'");
    }

    /// <summary>
    /// Property: FormatDateTime should return string wrapped in quotes.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(DateTimeArbitraries) })]
    public Property FormatDateTime_ShouldReturnQuotedString(
        DateTime dateTime, DateTimeDialectProvider provider)
    {
        // Act
        var result = provider.FormatDateTime(dateTime);

        // Assert - should start and end with single quote (for most dialects)
        var startsWithQuote = result.StartsWith("'");
        
        return startsWithQuote
            .Label($"Dialect: {provider.DialectName}, DateTime: {dateTime}, Result: '{result}'");
    }

    /// <summary>
    /// Property: MySQL FormatDateTime should use 'yyyy-MM-dd HH:mm:ss' format.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(DateTimeArbitraries) })]
    public Property MySQL_FormatDateTime_ShouldUseCorrectFormat(DateTime dateTime)
    {
        // Act
        var result = MySqlProvider.FormatDateTime(dateTime);

        // Assert - should match 'yyyy-MM-dd HH:mm:ss' format
        var expectedFormat = $"'{dateTime:yyyy-MM-dd HH:mm:ss}'";
        
        return (result == expectedFormat)
            .Label($"MySQL DateTime: {dateTime}, Expected: '{expectedFormat}', Got: '{result}'");
    }

    /// <summary>
    /// Property: PostgreSQL FormatDateTime should use 'yyyy-MM-dd HH:mm:ss.fff'::timestamp format.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(DateTimeArbitraries) })]
    public Property PostgreSQL_FormatDateTime_ShouldUseTimestampCast(DateTime dateTime)
    {
        // Act
        var result = PostgreSqlProvider.FormatDateTime(dateTime);

        // Assert - should contain ::timestamp cast
        var containsTimestampCast = result.Contains("::timestamp");
        var containsYear = result.Contains(dateTime.Year.ToString());

        return (containsTimestampCast && containsYear)
            .Label($"PostgreSQL DateTime: {dateTime}, Result: '{result}'");
    }

    /// <summary>
    /// Property: SQL Server FormatDateTime should use 'yyyy-MM-dd HH:mm:ss.fff' format.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(DateTimeArbitraries) })]
    public Property SqlServer_FormatDateTime_ShouldUseCorrectFormat(DateTime dateTime)
    {
        // Act
        var result = SqlServerProvider.FormatDateTime(dateTime);

        // Assert - should match 'yyyy-MM-dd HH:mm:ss.fff' format
        var expectedFormat = $"'{dateTime:yyyy-MM-dd HH:mm:ss.fff}'";

        return (result == expectedFormat)
            .Label($"SQL Server DateTime: {dateTime}, Expected: '{expectedFormat}', Got: '{result}'");
    }

    /// <summary>
    /// Property: SQLite FormatDateTime should use 'yyyy-MM-dd HH:mm:ss.fff' format.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(DateTimeArbitraries) })]
    public Property SQLite_FormatDateTime_ShouldUseCorrectFormat(DateTime dateTime)
    {
        // Act
        var result = SQLiteProvider.FormatDateTime(dateTime);

        // Assert - should match 'yyyy-MM-dd HH:mm:ss.fff' format
        var expectedFormat = $"'{dateTime:yyyy-MM-dd HH:mm:ss.fff}'";

        return (result == expectedFormat)
            .Label($"SQLite DateTime: {dateTime}, Expected: '{expectedFormat}', Got: '{result}'");
    }

    /// <summary>
    /// Property: FormatDateTime should preserve date components.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(DateTimeArbitraries) })]
    public Property FormatDateTime_ShouldPreserveDateComponents(
        DateTime dateTime, DateTimeDialectProvider provider)
    {
        // Act
        var result = provider.FormatDateTime(dateTime);

        // Assert - should contain all date components
        var containsYear = result.Contains(dateTime.Year.ToString());
        var containsMonth = result.Contains(dateTime.Month.ToString("D2"));
        var containsDay = result.Contains(dateTime.Day.ToString("D2"));
        var containsHour = result.Contains(dateTime.Hour.ToString("D2"));
        var containsMinute = result.Contains(dateTime.Minute.ToString("D2"));
        var containsSecond = result.Contains(dateTime.Second.ToString("D2"));

        return (containsYear && containsMonth && containsDay && containsHour && containsMinute && containsSecond)
            .Label($"Dialect: {provider.DialectName}, DateTime: {dateTime}, Result: '{result}'");
    }

    /// <summary>
    /// Property: FormatDateTime should use ISO 8601 date format (yyyy-MM-dd).
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(DateTimeArbitraries) })]
    public Property FormatDateTime_ShouldUseIso8601DateFormat(
        DateTime dateTime, DateTimeDialectProvider provider)
    {
        // Act
        var result = provider.FormatDateTime(dateTime);

        // Assert - should contain ISO 8601 date format
        var isoDatePattern = @"\d{4}-\d{2}-\d{2}";
        var containsIsoDate = Regex.IsMatch(result, isoDatePattern);

        return containsIsoDate
            .Label($"Dialect: {provider.DialectName}, DateTime: {dateTime}, Result: '{result}'");
    }

    /// <summary>
    /// Property: FormatDateTime should handle edge case dates correctly.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(DateTimeArbitraries) })]
    public Property FormatDateTime_ShouldHandleEdgeCaseDates(DateTimeDialectProvider provider)
    {
        // Test with specific edge case dates
        var edgeCases = new[]
        {
            new DateTime(2000, 1, 1, 0, 0, 0),      // Y2K
            new DateTime(2020, 2, 29, 12, 30, 45),  // Leap year
            new DateTime(2023, 12, 31, 23, 59, 59), // End of year
            new DateTime(2024, 1, 1, 0, 0, 0),      // Start of year
        };

        foreach (var dateTime in edgeCases)
        {
            var result = provider.FormatDateTime(dateTime);
            if (string.IsNullOrEmpty(result))
                return false.Label($"Empty result for edge case: {dateTime}");
            if (!result.Contains(dateTime.Year.ToString()))
                return false.Label($"Missing year for edge case: {dateTime}, Result: '{result}'");
        }

        return true.Label($"All edge cases passed for {provider.DialectName}");
    }

    /// <summary>
    /// Property: FormatDateTime result should be parseable back to DateTime.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(DateTimeArbitraries) })]
    public Property FormatDateTime_ResultShouldBeParseable(
        DateTime dateTime, DateTimeDialectProvider provider)
    {
        // Act
        var result = provider.FormatDateTime(dateTime);

        // Extract the date string (remove quotes and type cast)
        var dateString = result.Trim('\'');
        if (dateString.Contains("::"))
        {
            dateString = dateString.Split(new[] { "::" }, StringSplitOptions.None)[0].Trim('\'');
        }

        // Try to parse the date string
        var canParse = DateTime.TryParse(dateString, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate);

        // Assert - should be parseable and match original date (ignoring milliseconds for some dialects)
        if (!canParse)
            return false.Label($"Cannot parse: '{dateString}' from '{result}'");

        var datesMatch = parsedDate.Year == dateTime.Year &&
                        parsedDate.Month == dateTime.Month &&
                        parsedDate.Day == dateTime.Day &&
                        parsedDate.Hour == dateTime.Hour &&
                        parsedDate.Minute == dateTime.Minute &&
                        parsedDate.Second == dateTime.Second;

        return datesMatch
            .Label($"Dialect: {provider.DialectName}, Original: {dateTime}, Parsed: {parsedDate}, Result: '{result}'");
    }
}

/// <summary>
/// Wrapper class for DateTime dialect provider.
/// </summary>
public class DateTimeDialectProvider
{
    public Func<DateTime, string> FormatDateTime { get; }
    public string DialectName { get; }

    public DateTimeDialectProvider(Func<DateTime, string> formatDateTime, string dialectName)
    {
        FormatDateTime = formatDateTime;
        DialectName = dialectName;
    }

    public override string ToString() => DialectName;
}

/// <summary>
/// Custom arbitraries for DateTime property tests.
/// </summary>
public static class DateTimeArbitraries
{
    private static readonly MySqlDialectProvider MySqlProvider = new();
    private static readonly PostgreSqlDialectProvider PostgreSqlProvider = new();
    private static readonly SqlServerDialectProvider SqlServerProvider = new();
    private static readonly SQLiteDialectProvider SQLiteProvider = new();

    /// <summary>
    /// Generates valid DateTime values within a reasonable range.
    /// </summary>
    public static Arbitrary<DateTime> DateTime()
    {
        return Gen.Choose(2000, 2030).SelectMany(year =>
            Gen.Choose(1, 12).SelectMany(month =>
                Gen.Choose(1, 28).SelectMany(day =>
                    Gen.Choose(0, 23).SelectMany(hour =>
                        Gen.Choose(0, 59).SelectMany(minute =>
                            Gen.Choose(0, 59).Select(second =>
                                new DateTime(year, month, day, hour, minute, second)))))))
            .ToArbitrary();
    }

    /// <summary>
    /// Generates DateTime dialect provider.
    /// </summary>
    public static Arbitrary<DateTimeDialectProvider> DateTimeDialectProvider()
    {
        return Gen.Elements(
            new DateTimeDialectProvider(MySqlProvider.FormatDateTime, "MySQL"),
            new DateTimeDialectProvider(PostgreSqlProvider.FormatDateTime, "PostgreSQL"),
            new DateTimeDialectProvider(SqlServerProvider.FormatDateTime, "SQL Server"),
            new DateTimeDialectProvider(SQLiteProvider.FormatDateTime, "SQLite")
        ).ToArbitrary();
    }
}
