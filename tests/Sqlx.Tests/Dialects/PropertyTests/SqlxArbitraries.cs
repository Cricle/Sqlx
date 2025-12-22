// -----------------------------------------------------------------------
// <copyright file="SqlxArbitraries.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FsCheck;
using System;
using System.Linq;

namespace Sqlx.Tests.Dialects.PropertyTests;

/// <summary>
/// FsCheck arbitrary generators for SQL dialect property testing.
/// Provides generators for valid identifiers, table names, column names, and dialects.
/// </summary>
public static class SqlxArbitraries
{
    /// <summary>
    /// All supported database dialects for property testing.
    /// </summary>
    public static readonly Sqlx.Generator.SqlDefine[] AllDialects = new[]
    {
        Sqlx.Generator.SqlDefine.MySql,
        Sqlx.Generator.SqlDefine.PostgreSql,
        Sqlx.Generator.SqlDefine.SqlServer,
        Sqlx.Generator.SqlDefine.SQLite,
        Sqlx.Generator.SqlDefine.Oracle
    };

    /// <summary>
    /// Generates a valid SQL identifier (letters, numbers, underscore, starting with letter).
    /// </summary>
    public static Arbitrary<string> ValidIdentifier()
    {
        var firstChar = Gen.Elements('a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j',
            'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z',
            'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P',
            'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z');

        var restChars = Gen.Elements(
            'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p',
            'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z',
            'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P',
            'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z',
            '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '_');

        return Gen.Sized(size =>
        {
            var length = Math.Min(Math.Max(size, 1), 63); // SQL identifiers typically max 63-128 chars
            return from first in firstChar
                   from rest in Gen.ArrayOf(length - 1, restChars)
                   select first + new string(rest);
        }).ToArbitrary();
    }

    /// <summary>
    /// Generates a valid table name (same rules as identifier).
    /// </summary>
    public static Arbitrary<string> ValidTableName() => ValidIdentifier();

    /// <summary>
    /// Generates a valid column name (same rules as identifier).
    /// </summary>
    public static Arbitrary<string> ValidColumnName() => ValidIdentifier();

    /// <summary>
    /// Generates a random database dialect.
    /// </summary>
    public static Arbitrary<Sqlx.Generator.SqlDefine> Dialect()
    {
        return Gen.Elements(AllDialects).ToArbitrary();
    }

    /// <summary>
    /// Generates a valid parameter name (alphanumeric, starting with letter).
    /// </summary>
    public static Arbitrary<string> ValidParameterName()
    {
        return ValidIdentifier();
    }

    /// <summary>
    /// Generates a valid positive limit value for pagination.
    /// </summary>
    public static Arbitrary<int> ValidLimit()
    {
        return Gen.Choose(1, 10000).ToArbitrary();
    }

    /// <summary>
    /// Generates a valid non-negative offset value for pagination.
    /// </summary>
    public static Arbitrary<int> ValidOffset()
    {
        return Gen.Choose(0, 100000).ToArbitrary();
    }

    /// <summary>
    /// Generates a non-empty array of valid column names.
    /// </summary>
    public static Arbitrary<string[]> ValidColumnNames()
    {
        return Gen.Sized(size =>
        {
            var count = Math.Max(1, Math.Min(size, 20)); // 1-20 columns
            return Gen.ArrayOf(count, ValidIdentifier().Generator);
        }).Where(arr => arr.Length > 0 && arr.All(s => !string.IsNullOrEmpty(s)))
          .ToArbitrary();
    }

    /// <summary>
    /// Generates a valid .NET type for type mapping tests.
    /// </summary>
    public static Arbitrary<Type> ValidDotNetType()
    {
        return Gen.Elements(
            typeof(int),
            typeof(long),
            typeof(short),
            typeof(byte),
            typeof(float),
            typeof(double),
            typeof(decimal),
            typeof(string),
            typeof(bool),
            typeof(DateTime),
            typeof(DateTimeOffset),
            typeof(Guid),
            typeof(byte[])
        ).ToArbitrary();
    }

    /// <summary>
    /// Generates a random DateTime value.
    /// </summary>
    public static Arbitrary<DateTime> ValidDateTime()
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
    /// Generates SQL injection attack strings for security testing.
    /// </summary>
    public static Arbitrary<string> SqlInjectionAttempt()
    {
        return Gen.Elements(
            "'; DROP TABLE users; --",
            "1; DELETE FROM users",
            "1 OR 1=1",
            "admin'--",
            "1; EXEC xp_cmdshell('dir')",
            "/* comment */ DROP TABLE",
            "'; TRUNCATE TABLE users; --",
            "1; UPDATE users SET admin=1",
            "UNION SELECT * FROM passwords",
            "'; INSERT INTO users VALUES('hacker')"
        ).ToArbitrary();
    }

    /// <summary>
    /// Generates strings with special characters that need escaping.
    /// </summary>
    public static Arbitrary<string> StringWithSpecialChars()
    {
        return Gen.Elements(
            "O'Brien",
            "Test\"Quote",
            "Back`tick",
            "Square[Bracket]",
            "Semi;colon",
            "New\nLine",
            "Tab\tChar",
            "Percent%Sign",
            "Under_score",
            "Dash-Name"
        ).ToArbitrary();
    }
}
