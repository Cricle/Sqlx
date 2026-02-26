// <copyright file="DateTimeDataGenerator.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx.Tests.E2E.Infrastructure;

/// <summary>
/// Generates date-time boundary values for E2E testing.
/// </summary>
public static class DateTimeDataGenerator
{
    public static DateTime GetMinDate(DatabaseType dbType)
    {
        return dbType switch
        {
            DatabaseType.MySQL => new DateTime(1000, 1, 1),
            DatabaseType.PostgreSQL => new DateTime(1, 1, 1),
            DatabaseType.SqlServer => new DateTime(1753, 1, 1), // SQL Server DATETIME2 minimum
            DatabaseType.SQLite => new DateTime(1, 1, 1),
            _ => throw new NotSupportedException($"Database type {dbType} is not supported"),
        };
    }

    public static DateTime GetMaxDate(DatabaseType dbType)
    {
        return dbType switch
        {
            DatabaseType.MySQL => new DateTime(9999, 12, 31, 23, 59, 59),
            DatabaseType.PostgreSQL => new DateTime(9999, 12, 31, 23, 59, 59),
            DatabaseType.SqlServer => new DateTime(9999, 12, 31, 23, 59, 59),
            DatabaseType.SQLite => new DateTime(9999, 12, 31, 23, 59, 59),
            _ => throw new NotSupportedException($"Database type {dbType} is not supported"),
        };
    }

    public static DateTime GetMidnightDate() => new DateTime(2024, 1, 1, 0, 0, 0);

    public static DateTime GetEndOfDayDate() => new DateTime(2024, 1, 1, 23, 59, 59);

    public static DateTime GetLeapYearDate() => new DateTime(2024, 2, 29);
}
