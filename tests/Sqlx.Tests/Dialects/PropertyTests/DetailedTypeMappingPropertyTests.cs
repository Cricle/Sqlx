// -----------------------------------------------------------------------
// <copyright file="DetailedTypeMappingPropertyTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using FsCheck.Xunit;
using Xunit;
using Sqlx.Generator;

namespace Sqlx.Tests.Dialects.PropertyTests;

/// <summary>
/// Property-based tests for detailed type mapping correctness across all database dialects.
/// Covers Properties 24-27 for numeric, string, datetime, and special type mappings.
/// </summary>
public class DetailedTypeMappingPropertyTests
{
    private static readonly MySqlDialectProvider MySqlProvider = new();
    private static readonly PostgreSqlDialectProvider PostgreSqlProvider = new();
    private static readonly SqlServerDialectProvider SqlServerProvider = new();
    private static readonly SQLiteDialectProvider SQLiteProvider = new();

    #region Numeric Type Definitions

    /// <summary>
    /// All numeric .NET types for Property 24 tests.
    /// </summary>
    private static readonly Type[] NumericTypes = new[]
    {
        typeof(short),   // Int16
        typeof(int),     // Int32
        typeof(long),    // Int64
        typeof(float),   // Single
        typeof(double),  // Double
        typeof(decimal)  // Decimal
    };

    /// <summary>
    /// Expected numeric type mappings for each dialect.
    /// </summary>
    private static readonly Dictionary<string, Dictionary<Type, string[]>> ExpectedNumericMappings = new()
    {
        ["MySQL"] = new Dictionary<Type, string[]>
        {
            [typeof(short)] = new[] { "SMALLINT", "INT" },
            [typeof(int)] = new[] { "INT", "INTEGER" },
            [typeof(long)] = new[] { "BIGINT" },
            [typeof(float)] = new[] { "FLOAT", "REAL" },
            [typeof(double)] = new[] { "DOUBLE", "DOUBLE PRECISION" },
            [typeof(decimal)] = new[] { "DECIMAL", "NUMERIC" }
        },
        ["PostgreSQL"] = new Dictionary<Type, string[]>
        {
            [typeof(short)] = new[] { "SMALLINT", "INT2" },
            [typeof(int)] = new[] { "INTEGER", "INT", "INT4" },
            [typeof(long)] = new[] { "BIGINT", "INT8" },
            [typeof(float)] = new[] { "REAL", "FLOAT4" },
            [typeof(double)] = new[] { "DOUBLE PRECISION", "FLOAT8" },
            [typeof(decimal)] = new[] { "DECIMAL", "NUMERIC" }
        },
        ["SQL Server"] = new Dictionary<Type, string[]>
        {
            [typeof(short)] = new[] { "SMALLINT" },
            [typeof(int)] = new[] { "INT" },
            [typeof(long)] = new[] { "BIGINT" },
            [typeof(float)] = new[] { "REAL" },
            [typeof(double)] = new[] { "FLOAT" },
            [typeof(decimal)] = new[] { "DECIMAL", "NUMERIC" }
        },
        ["SQLite"] = new Dictionary<Type, string[]>
        {
            [typeof(short)] = new[] { "INTEGER", "SMALLINT" },
            [typeof(int)] = new[] { "INTEGER", "INT" },
            [typeof(long)] = new[] { "INTEGER", "BIGINT" },
            [typeof(float)] = new[] { "REAL" },
            [typeof(double)] = new[] { "REAL" },
            [typeof(decimal)] = new[] { "REAL", "NUMERIC" }
        }
    };

    #endregion

    #region String Type Definitions

    /// <summary>
    /// String-related .NET types for Property 25 tests.
    /// </summary>
    private static readonly Type[] StringTypes = new[]
    {
        typeof(string),
        typeof(char)
    };

    /// <summary>
    /// Expected string type mappings for each dialect.
    /// </summary>
    private static readonly Dictionary<string, Dictionary<Type, string[]>> ExpectedStringMappings = new()
    {
        ["MySQL"] = new Dictionary<Type, string[]>
        {
            [typeof(string)] = new[] { "VARCHAR", "TEXT", "NVARCHAR" },
            [typeof(char)] = new[] { "CHAR", "VARCHAR" }
        },
        ["PostgreSQL"] = new Dictionary<Type, string[]>
        {
            [typeof(string)] = new[] { "VARCHAR", "TEXT", "CHARACTER VARYING" },
            [typeof(char)] = new[] { "CHAR", "VARCHAR", "CHARACTER" }
        },
        ["SQL Server"] = new Dictionary<Type, string[]>
        {
            [typeof(string)] = new[] { "NVARCHAR", "VARCHAR", "NTEXT" },
            [typeof(char)] = new[] { "NCHAR", "NVARCHAR", "CHAR" }
        },
        ["SQLite"] = new Dictionary<Type, string[]>
        {
            [typeof(string)] = new[] { "TEXT" },
            [typeof(char)] = new[] { "TEXT" }
        }
    };

    #endregion

    #region DateTime Type Definitions

    /// <summary>
    /// Date/time .NET types for Property 26 tests.
    /// </summary>
    private static readonly Type[] DateTimeTypes = new[]
    {
        typeof(DateTime),
        typeof(DateTimeOffset),
        typeof(TimeSpan)
    };

    /// <summary>
    /// Expected datetime type mappings for each dialect.
    /// </summary>
    private static readonly Dictionary<string, Dictionary<Type, string[]>> ExpectedDateTimeMappings = new()
    {
        ["MySQL"] = new Dictionary<Type, string[]>
        {
            [typeof(DateTime)] = new[] { "DATETIME", "TIMESTAMP" },
            [typeof(DateTimeOffset)] = new[] { "DATETIME", "TIMESTAMP", "VARCHAR" },
            [typeof(TimeSpan)] = new[] { "TIME", "BIGINT", "VARCHAR" }
        },
        ["PostgreSQL"] = new Dictionary<Type, string[]>
        {
            [typeof(DateTime)] = new[] { "TIMESTAMP", "TIMESTAMPTZ" },
            [typeof(DateTimeOffset)] = new[] { "TIMESTAMPTZ", "TIMESTAMP WITH TIME ZONE", "VARCHAR" },
            [typeof(TimeSpan)] = new[] { "INTERVAL", "TIME", "VARCHAR" }
        },
        ["SQL Server"] = new Dictionary<Type, string[]>
        {
            [typeof(DateTime)] = new[] { "DATETIME", "DATETIME2" },
            [typeof(DateTimeOffset)] = new[] { "DATETIMEOFFSET", "DATETIME2", "NVARCHAR" },
            [typeof(TimeSpan)] = new[] { "TIME", "BIGINT", "NVARCHAR" }
        },
        ["SQLite"] = new Dictionary<Type, string[]>
        {
            [typeof(DateTime)] = new[] { "TEXT", "DATETIME" },
            [typeof(DateTimeOffset)] = new[] { "TEXT" },
            [typeof(TimeSpan)] = new[] { "TEXT", "INTEGER" }
        }
    };

    #endregion

    #region Special Type Definitions

    /// <summary>
    /// Special .NET types for Property 27 tests.
    /// </summary>
    private static readonly Type[] SpecialTypes = new[]
    {
        typeof(Guid),
        typeof(byte[]),
        typeof(bool)
    };

    /// <summary>
    /// Expected special type mappings for each dialect.
    /// </summary>
    private static readonly Dictionary<string, Dictionary<Type, string[]>> ExpectedSpecialMappings = new()
    {
        ["MySQL"] = new Dictionary<Type, string[]>
        {
            [typeof(Guid)] = new[] { "CHAR(36)", "VARCHAR(36)", "UUID" },
            [typeof(byte[])] = new[] { "BLOB", "VARBINARY", "LONGBLOB" },
            [typeof(bool)] = new[] { "BOOLEAN", "BIT", "TINYINT" }
        },
        ["PostgreSQL"] = new Dictionary<Type, string[]>
        {
            [typeof(Guid)] = new[] { "UUID" },
            [typeof(byte[])] = new[] { "BYTEA" },
            [typeof(bool)] = new[] { "BOOLEAN", "BOOL" }
        },
        ["SQL Server"] = new Dictionary<Type, string[]>
        {
            [typeof(Guid)] = new[] { "UNIQUEIDENTIFIER" },
            [typeof(byte[])] = new[] { "VARBINARY", "IMAGE" },
            [typeof(bool)] = new[] { "BIT" }
        },
        ["SQLite"] = new Dictionary<Type, string[]>
        {
            [typeof(Guid)] = new[] { "TEXT", "BLOB" },
            [typeof(byte[])] = new[] { "BLOB" },
            [typeof(bool)] = new[] { "INTEGER", "BOOLEAN" }
        }
    };

    #endregion


    #region Property 24: Numeric Type Mapping Tests

    /// <summary>
    /// **Property 24: Numeric Type Mapping**
    /// *For any* numeric .NET type and *for any* database dialect, 
    /// GetDatabaseTypeName SHALL return appropriate numeric database type.
    /// **Validates: Requirements 37.1, 37.2, 37.3, 37.4, 37.5, 37.6**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(DetailedTypeMappingArbitraries) })]
    public Property NumericTypeMapping_ForAnyNumericTypeAndDialect_ShouldReturnAppropriateNumericType(
        NumericTypeMappingTestCase testCase)
    {
        // Act
        var result = testCase.GetDatabaseTypeName(testCase.DotNetType);

        // Assert - result should be non-empty and match expected numeric types
        var isNonEmpty = !string.IsNullOrWhiteSpace(result);
        var isValidNumericType = ExpectedNumericMappings.TryGetValue(testCase.DialectName, out var dialectMappings) &&
                                  dialectMappings.TryGetValue(testCase.DotNetType, out var validTypes) &&
                                  validTypes.Any(vt => result.StartsWith(vt, StringComparison.OrdinalIgnoreCase));

        return (isNonEmpty && isValidNumericType)
            .Label($"Dialect: {testCase.DialectName}, " +
                   $".NET Type: {testCase.DotNetType.Name}, " +
                   $"Result: '{result}', " +
                   $"Expected one of: [{string.Join(", ", ExpectedNumericMappings.GetValueOrDefault(testCase.DialectName)?.GetValueOrDefault(testCase.DotNetType) ?? Array.Empty<string>())}]");
    }

    /// <summary>
    /// Property 24: Numeric type mapping should be consistent across multiple calls.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(DetailedTypeMappingArbitraries) })]
    public Property NumericTypeMapping_ShouldBeConsistent(NumericTypeMappingTestCase testCase)
    {
        // Act
        var result1 = testCase.GetDatabaseTypeName(testCase.DotNetType);
        var result2 = testCase.GetDatabaseTypeName(testCase.DotNetType);

        // Assert
        return (result1 == result2)
            .Label($"Dialect: {testCase.DialectName}, Type: {testCase.DotNetType.Name}, " +
                   $"Result1: '{result1}', Result2: '{result2}'");
    }

    #region Numeric Type Unit Tests

    [Fact]
    public void MySQL_Int16_ShouldReturnSmallInt()
    {
        var result = MySqlProvider.GetDatabaseTypeName(typeof(short));
        Assert.Equal("SMALLINT", result);
    }

    [Fact]
    public void PostgreSQL_Int16_ShouldReturnSmallInt()
    {
        var result = PostgreSqlProvider.GetDatabaseTypeName(typeof(short));
        Assert.Equal("SMALLINT", result);
    }

    [Fact]
    public void SqlServer_Int16_ShouldReturnSmallInt()
    {
        var result = SqlServerProvider.GetDatabaseTypeName(typeof(short));
        Assert.Equal("SMALLINT", result);
    }

    [Fact]
    public void SQLite_Int16_ShouldReturnInteger()
    {
        var result = SQLiteProvider.GetDatabaseTypeName(typeof(short));
        Assert.Equal("INTEGER", result);
    }

    [Fact]
    public void MySQL_Single_ShouldReturnFloat()
    {
        var result = MySqlProvider.GetDatabaseTypeName(typeof(float));
        Assert.Equal("FLOAT", result);
    }

    [Fact]
    public void PostgreSQL_Single_ShouldReturnReal()
    {
        var result = PostgreSqlProvider.GetDatabaseTypeName(typeof(float));
        Assert.Equal("REAL", result);
    }

    [Fact]
    public void SqlServer_Single_ShouldReturnReal()
    {
        var result = SqlServerProvider.GetDatabaseTypeName(typeof(float));
        Assert.Equal("REAL", result);
    }

    [Fact]
    public void SQLite_Single_ShouldReturnReal()
    {
        var result = SQLiteProvider.GetDatabaseTypeName(typeof(float));
        Assert.Equal("REAL", result);
    }

    [Fact]
    public void MySQL_Double_ShouldReturnDouble()
    {
        var result = MySqlProvider.GetDatabaseTypeName(typeof(double));
        Assert.Equal("DOUBLE", result);
    }

    [Fact]
    public void PostgreSQL_Double_ShouldReturnDoublePrecision()
    {
        var result = PostgreSqlProvider.GetDatabaseTypeName(typeof(double));
        Assert.Equal("DOUBLE PRECISION", result);
    }

    [Fact]
    public void SqlServer_Double_ShouldReturnFloat()
    {
        var result = SqlServerProvider.GetDatabaseTypeName(typeof(double));
        Assert.Equal("FLOAT", result);
    }

    [Fact]
    public void SQLite_Double_ShouldReturnReal()
    {
        var result = SQLiteProvider.GetDatabaseTypeName(typeof(double));
        Assert.Equal("REAL", result);
    }

    #endregion

    #endregion


    #region Property 25: String Type Mapping Tests

    /// <summary>
    /// **Property 25: String Type Mapping**
    /// *For any* string .NET type and *for any* database dialect, 
    /// GetDatabaseTypeName SHALL return appropriate string database type.
    /// **Validates: Requirements 38.1, 38.2, 38.3, 38.4, 38.5**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(DetailedTypeMappingArbitraries) })]
    public Property StringTypeMapping_ForAnyStringTypeAndDialect_ShouldReturnAppropriateStringType(
        StringTypeMappingTestCase testCase)
    {
        // Act
        var result = testCase.GetDatabaseTypeName(testCase.DotNetType);

        // Assert - result should be non-empty and match expected string types
        var isNonEmpty = !string.IsNullOrWhiteSpace(result);
        var isValidStringType = ExpectedStringMappings.TryGetValue(testCase.DialectName, out var dialectMappings) &&
                                 dialectMappings.TryGetValue(testCase.DotNetType, out var validTypes) &&
                                 validTypes.Any(vt => result.StartsWith(vt, StringComparison.OrdinalIgnoreCase));

        return (isNonEmpty && isValidStringType)
            .Label($"Dialect: {testCase.DialectName}, " +
                   $".NET Type: {testCase.DotNetType.Name}, " +
                   $"Result: '{result}', " +
                   $"Expected one of: [{string.Join(", ", ExpectedStringMappings.GetValueOrDefault(testCase.DialectName)?.GetValueOrDefault(testCase.DotNetType) ?? Array.Empty<string>())}]");
    }

    /// <summary>
    /// Property 25: String type mapping should be consistent across multiple calls.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(DetailedTypeMappingArbitraries) })]
    public Property StringTypeMapping_ShouldBeConsistent(StringTypeMappingTestCase testCase)
    {
        // Act
        var result1 = testCase.GetDatabaseTypeName(testCase.DotNetType);
        var result2 = testCase.GetDatabaseTypeName(testCase.DotNetType);

        // Assert
        return (result1 == result2)
            .Label($"Dialect: {testCase.DialectName}, Type: {testCase.DotNetType.Name}, " +
                   $"Result1: '{result1}', Result2: '{result2}'");
    }

    #region String Type Unit Tests

    [Fact]
    public void MySQL_String_ShouldReturnVarchar()
    {
        var result = MySqlProvider.GetDatabaseTypeName(typeof(string));
        Assert.StartsWith("VARCHAR", result);
    }

    [Fact]
    public void PostgreSQL_String_ShouldReturnVarchar()
    {
        var result = PostgreSqlProvider.GetDatabaseTypeName(typeof(string));
        Assert.StartsWith("VARCHAR", result);
    }

    [Fact]
    public void SqlServer_String_ShouldReturnNVarchar()
    {
        var result = SqlServerProvider.GetDatabaseTypeName(typeof(string));
        Assert.StartsWith("NVARCHAR", result);
    }

    [Fact]
    public void SQLite_String_ShouldReturnText()
    {
        var result = SQLiteProvider.GetDatabaseTypeName(typeof(string));
        Assert.Equal("TEXT", result);
    }

    #endregion

    #endregion

    #region Property 26: DateTime Type Mapping Tests

    /// <summary>
    /// **Property 26: DateTime Type Mapping**
    /// *For any* date/time .NET type and *for any* database dialect, 
    /// GetDatabaseTypeName SHALL return appropriate temporal database type.
    /// **Validates: Requirements 39.1, 39.2, 39.3, 39.4, 39.5**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(DetailedTypeMappingArbitraries) })]
    public Property DateTimeTypeMapping_ForAnyDateTimeTypeAndDialect_ShouldReturnAppropriateTemporalType(
        DateTimeTypeMappingTestCase testCase)
    {
        // Act
        var result = testCase.GetDatabaseTypeName(testCase.DotNetType);

        // Assert - result should be non-empty and match expected datetime types
        var isNonEmpty = !string.IsNullOrWhiteSpace(result);
        var isValidDateTimeType = ExpectedDateTimeMappings.TryGetValue(testCase.DialectName, out var dialectMappings) &&
                                   dialectMappings.TryGetValue(testCase.DotNetType, out var validTypes) &&
                                   validTypes.Any(vt => result.StartsWith(vt, StringComparison.OrdinalIgnoreCase));

        return (isNonEmpty && isValidDateTimeType)
            .Label($"Dialect: {testCase.DialectName}, " +
                   $".NET Type: {testCase.DotNetType.Name}, " +
                   $"Result: '{result}', " +
                   $"Expected one of: [{string.Join(", ", ExpectedDateTimeMappings.GetValueOrDefault(testCase.DialectName)?.GetValueOrDefault(testCase.DotNetType) ?? Array.Empty<string>())}]");
    }

    /// <summary>
    /// Property 26: DateTime type mapping should be consistent across multiple calls.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(DetailedTypeMappingArbitraries) })]
    public Property DateTimeTypeMapping_ShouldBeConsistent(DateTimeTypeMappingTestCase testCase)
    {
        // Act
        var result1 = testCase.GetDatabaseTypeName(testCase.DotNetType);
        var result2 = testCase.GetDatabaseTypeName(testCase.DotNetType);

        // Assert
        return (result1 == result2)
            .Label($"Dialect: {testCase.DialectName}, Type: {testCase.DotNetType.Name}, " +
                   $"Result1: '{result1}', Result2: '{result2}'");
    }

    #region DateTime Type Unit Tests

    [Fact]
    public void MySQL_DateTime_ShouldReturnDatetime()
    {
        var result = MySqlProvider.GetDatabaseTypeName(typeof(DateTime));
        Assert.Equal("DATETIME", result);
    }

    [Fact]
    public void PostgreSQL_DateTime_ShouldReturnTimestamp()
    {
        var result = PostgreSqlProvider.GetDatabaseTypeName(typeof(DateTime));
        Assert.Equal("TIMESTAMP", result);
    }

    [Fact]
    public void SqlServer_DateTime_ShouldReturnDatetime2()
    {
        var result = SqlServerProvider.GetDatabaseTypeName(typeof(DateTime));
        Assert.Equal("DATETIME2", result);
    }

    [Fact]
    public void SQLite_DateTime_ShouldReturnText()
    {
        var result = SQLiteProvider.GetDatabaseTypeName(typeof(DateTime));
        Assert.Equal("TEXT", result);
    }

    #endregion

    #endregion


    #region Property 27: Special Type Mapping Tests

    /// <summary>
    /// **Property 27: Special Type Mapping**
    /// *For any* special .NET type (Guid, byte[], Boolean) and *for any* database dialect, 
    /// GetDatabaseTypeName SHALL return appropriate database type.
    /// **Validates: Requirements 40.1, 40.2, 40.3, 40.4, 40.5**
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(DetailedTypeMappingArbitraries) })]
    public Property SpecialTypeMapping_ForAnySpecialTypeAndDialect_ShouldReturnAppropriateType(
        SpecialTypeMappingTestCase testCase)
    {
        // Act
        var result = testCase.GetDatabaseTypeName(testCase.DotNetType);

        // Assert - result should be non-empty and match expected special types
        var isNonEmpty = !string.IsNullOrWhiteSpace(result);
        var isValidSpecialType = ExpectedSpecialMappings.TryGetValue(testCase.DialectName, out var dialectMappings) &&
                                  dialectMappings.TryGetValue(testCase.DotNetType, out var validTypes) &&
                                  validTypes.Any(vt => result.StartsWith(vt, StringComparison.OrdinalIgnoreCase));

        return (isNonEmpty && isValidSpecialType)
            .Label($"Dialect: {testCase.DialectName}, " +
                   $".NET Type: {testCase.DotNetType.Name}, " +
                   $"Result: '{result}', " +
                   $"Expected one of: [{string.Join(", ", ExpectedSpecialMappings.GetValueOrDefault(testCase.DialectName)?.GetValueOrDefault(testCase.DotNetType) ?? Array.Empty<string>())}]");
    }

    /// <summary>
    /// Property 27: Special type mapping should be consistent across multiple calls.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(DetailedTypeMappingArbitraries) })]
    public Property SpecialTypeMapping_ShouldBeConsistent(SpecialTypeMappingTestCase testCase)
    {
        // Act
        var result1 = testCase.GetDatabaseTypeName(testCase.DotNetType);
        var result2 = testCase.GetDatabaseTypeName(testCase.DotNetType);

        // Assert
        return (result1 == result2)
            .Label($"Dialect: {testCase.DialectName}, Type: {testCase.DotNetType.Name}, " +
                   $"Result1: '{result1}', Result2: '{result2}'");
    }

    #region Special Type Unit Tests

    [Fact]
    public void MySQL_Guid_ShouldReturnChar36()
    {
        var result = MySqlProvider.GetDatabaseTypeName(typeof(Guid));
        Assert.Equal("CHAR(36)", result);
    }

    [Fact]
    public void PostgreSQL_Guid_ShouldReturnUuid()
    {
        var result = PostgreSqlProvider.GetDatabaseTypeName(typeof(Guid));
        Assert.Equal("UUID", result);
    }

    [Fact]
    public void SqlServer_Guid_ShouldReturnUniqueIdentifier()
    {
        var result = SqlServerProvider.GetDatabaseTypeName(typeof(Guid));
        Assert.Equal("UNIQUEIDENTIFIER", result);
    }

    [Fact]
    public void SQLite_Guid_ShouldReturnText()
    {
        var result = SQLiteProvider.GetDatabaseTypeName(typeof(Guid));
        Assert.Equal("TEXT", result);
    }

    [Fact]
    public void MySQL_ByteArray_ShouldReturnBlob()
    {
        var result = MySqlProvider.GetDatabaseTypeName(typeof(byte[]));
        Assert.Equal("BLOB", result);
    }

    [Fact]
    public void PostgreSQL_ByteArray_ShouldReturnBytea()
    {
        var result = PostgreSqlProvider.GetDatabaseTypeName(typeof(byte[]));
        Assert.Equal("BYTEA", result);
    }

    [Fact]
    public void SqlServer_ByteArray_ShouldReturnVarbinary()
    {
        var result = SqlServerProvider.GetDatabaseTypeName(typeof(byte[]));
        Assert.StartsWith("VARBINARY", result);
    }

    [Fact]
    public void SQLite_ByteArray_ShouldReturnBlob()
    {
        var result = SQLiteProvider.GetDatabaseTypeName(typeof(byte[]));
        Assert.Equal("BLOB", result);
    }

    [Fact]
    public void MySQL_Boolean_ShouldReturnBoolean()
    {
        var result = MySqlProvider.GetDatabaseTypeName(typeof(bool));
        Assert.Equal("BOOLEAN", result);
    }

    [Fact]
    public void PostgreSQL_Boolean_ShouldReturnBoolean()
    {
        var result = PostgreSqlProvider.GetDatabaseTypeName(typeof(bool));
        Assert.Equal("BOOLEAN", result);
    }

    [Fact]
    public void SqlServer_Boolean_ShouldReturnBit()
    {
        var result = SqlServerProvider.GetDatabaseTypeName(typeof(bool));
        Assert.Equal("BIT", result);
    }

    [Fact]
    public void SQLite_Boolean_ShouldReturnInteger()
    {
        var result = SQLiteProvider.GetDatabaseTypeName(typeof(bool));
        Assert.Equal("INTEGER", result);
    }

    #endregion

    #endregion
}

#region Test Case Classes

/// <summary>
/// Test case for numeric type mapping property tests.
/// </summary>
public class NumericTypeMappingTestCase
{
    public Func<Type, string> GetDatabaseTypeName { get; }
    public string DialectName { get; }
    public Type DotNetType { get; }

    public NumericTypeMappingTestCase(
        Func<Type, string> getDatabaseTypeName,
        string dialectName,
        Type dotNetType)
    {
        GetDatabaseTypeName = getDatabaseTypeName;
        DialectName = dialectName;
        DotNetType = dotNetType;
    }

    public override string ToString() => $"{DialectName}:{DotNetType.Name}";
}

/// <summary>
/// Test case for string type mapping property tests.
/// </summary>
public class StringTypeMappingTestCase
{
    public Func<Type, string> GetDatabaseTypeName { get; }
    public string DialectName { get; }
    public Type DotNetType { get; }

    public StringTypeMappingTestCase(
        Func<Type, string> getDatabaseTypeName,
        string dialectName,
        Type dotNetType)
    {
        GetDatabaseTypeName = getDatabaseTypeName;
        DialectName = dialectName;
        DotNetType = dotNetType;
    }

    public override string ToString() => $"{DialectName}:{DotNetType.Name}";
}

/// <summary>
/// Test case for datetime type mapping property tests.
/// </summary>
public class DateTimeTypeMappingTestCase
{
    public Func<Type, string> GetDatabaseTypeName { get; }
    public string DialectName { get; }
    public Type DotNetType { get; }

    public DateTimeTypeMappingTestCase(
        Func<Type, string> getDatabaseTypeName,
        string dialectName,
        Type dotNetType)
    {
        GetDatabaseTypeName = getDatabaseTypeName;
        DialectName = dialectName;
        DotNetType = dotNetType;
    }

    public override string ToString() => $"{DialectName}:{DotNetType.Name}";
}

/// <summary>
/// Test case for special type mapping property tests.
/// </summary>
public class SpecialTypeMappingTestCase
{
    public Func<Type, string> GetDatabaseTypeName { get; }
    public string DialectName { get; }
    public Type DotNetType { get; }

    public SpecialTypeMappingTestCase(
        Func<Type, string> getDatabaseTypeName,
        string dialectName,
        Type dotNetType)
    {
        GetDatabaseTypeName = getDatabaseTypeName;
        DialectName = dialectName;
        DotNetType = dotNetType;
    }

    public override string ToString() => $"{DialectName}:{DotNetType.Name}";
}

#endregion

#region Arbitraries

/// <summary>
/// Custom arbitraries for detailed type mapping property tests.
/// </summary>
public static class DetailedTypeMappingArbitraries
{
    private static readonly MySqlDialectProvider MySqlProvider = new();
    private static readonly PostgreSqlDialectProvider PostgreSqlProvider = new();
    private static readonly SqlServerDialectProvider SqlServerProvider = new();
    private static readonly SQLiteDialectProvider SQLiteProvider = new();

    /// <summary>
    /// Numeric .NET types.
    /// </summary>
    private static readonly Type[] NumericTypes = new[]
    {
        typeof(short),
        typeof(int),
        typeof(long),
        typeof(float),
        typeof(double),
        typeof(decimal)
    };

    /// <summary>
    /// String .NET types.
    /// </summary>
    private static readonly Type[] StringTypes = new[]
    {
        typeof(string)
    };

    /// <summary>
    /// DateTime .NET types.
    /// </summary>
    private static readonly Type[] DateTimeTypes = new[]
    {
        typeof(DateTime)
    };

    /// <summary>
    /// Special .NET types.
    /// </summary>
    private static readonly Type[] SpecialTypes = new[]
    {
        typeof(Guid),
        typeof(byte[]),
        typeof(bool)
    };

    /// <summary>
    /// Generates test cases for numeric type mapping.
    /// </summary>
    public static Arbitrary<NumericTypeMappingTestCase> NumericTypeMappingTestCase()
    {
        var providers = new (Func<Type, string> func, string name)[]
        {
            (MySqlProvider.GetDatabaseTypeName, "MySQL"),
            (PostgreSqlProvider.GetDatabaseTypeName, "PostgreSQL"),
            (SqlServerProvider.GetDatabaseTypeName, "SQL Server"),
            (SQLiteProvider.GetDatabaseTypeName, "SQLite")
        };

        var testCases = from provider in providers
                        from type in NumericTypes
                        select new NumericTypeMappingTestCase(provider.func, provider.name, type);

        return Gen.Elements(testCases.ToArray()).ToArbitrary();
    }

    /// <summary>
    /// Generates test cases for string type mapping.
    /// </summary>
    public static Arbitrary<StringTypeMappingTestCase> StringTypeMappingTestCase()
    {
        var providers = new (Func<Type, string> func, string name)[]
        {
            (MySqlProvider.GetDatabaseTypeName, "MySQL"),
            (PostgreSqlProvider.GetDatabaseTypeName, "PostgreSQL"),
            (SqlServerProvider.GetDatabaseTypeName, "SQL Server"),
            (SQLiteProvider.GetDatabaseTypeName, "SQLite")
        };

        var testCases = from provider in providers
                        from type in StringTypes
                        select new StringTypeMappingTestCase(provider.func, provider.name, type);

        return Gen.Elements(testCases.ToArray()).ToArbitrary();
    }

    /// <summary>
    /// Generates test cases for datetime type mapping.
    /// </summary>
    public static Arbitrary<DateTimeTypeMappingTestCase> DateTimeTypeMappingTestCase()
    {
        var providers = new (Func<Type, string> func, string name)[]
        {
            (MySqlProvider.GetDatabaseTypeName, "MySQL"),
            (PostgreSqlProvider.GetDatabaseTypeName, "PostgreSQL"),
            (SqlServerProvider.GetDatabaseTypeName, "SQL Server"),
            (SQLiteProvider.GetDatabaseTypeName, "SQLite")
        };

        var testCases = from provider in providers
                        from type in DateTimeTypes
                        select new DateTimeTypeMappingTestCase(provider.func, provider.name, type);

        return Gen.Elements(testCases.ToArray()).ToArbitrary();
    }

    /// <summary>
    /// Generates test cases for special type mapping.
    /// </summary>
    public static Arbitrary<SpecialTypeMappingTestCase> SpecialTypeMappingTestCase()
    {
        var providers = new (Func<Type, string> func, string name)[]
        {
            (MySqlProvider.GetDatabaseTypeName, "MySQL"),
            (PostgreSqlProvider.GetDatabaseTypeName, "PostgreSQL"),
            (SqlServerProvider.GetDatabaseTypeName, "SQL Server"),
            (SQLiteProvider.GetDatabaseTypeName, "SQLite")
        };

        var testCases = from provider in providers
                        from type in SpecialTypes
                        select new SpecialTypeMappingTestCase(provider.func, provider.name, type);

        return Gen.Elements(testCases.ToArray()).ToArbitrary();
    }
}

#endregion
