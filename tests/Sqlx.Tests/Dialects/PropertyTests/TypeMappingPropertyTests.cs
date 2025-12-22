// -----------------------------------------------------------------------
// <copyright file="TypeMappingPropertyTests.cs" company="Cricle">
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
/// Property-based tests for Data Type Mapping correctness across all database dialects.
/// **Feature: sql-semantic-tdd-validation, Property 11: Data Type Mapping Correctness**
/// **Validates: Requirements 9.1, 9.2, 9.3, 9.4, 9.5, 9.6, 9.7**
/// </summary>
public class TypeMappingPropertyTests
{
    private static readonly MySqlDialectProvider MySqlProvider = new();
    private static readonly PostgreSqlDialectProvider PostgreSqlProvider = new();
    private static readonly SqlServerDialectProvider SqlServerProvider = new();
    private static readonly SQLiteDialectProvider SQLiteProvider = new();

    /// <summary>
    /// All supported .NET types for type mapping tests.
    /// </summary>
    private static readonly Type[] SupportedTypes = new[]
    {
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
        typeof(Guid),
        typeof(byte[])
    };

    /// <summary>
    /// Expected type mappings for each dialect.
    /// </summary>
    private static readonly Dictionary<string, Dictionary<Type, string[]>> ExpectedTypeMappings = new()
    {
        ["MySQL"] = new Dictionary<Type, string[]>
        {
            [typeof(int)] = new[] { "INT", "INTEGER" },
            [typeof(long)] = new[] { "BIGINT" },
            [typeof(short)] = new[] { "SMALLINT", "INT" },
            [typeof(byte)] = new[] { "TINYINT" },
            [typeof(float)] = new[] { "FLOAT", "REAL" },
            [typeof(double)] = new[] { "DOUBLE", "DOUBLE PRECISION" },
            [typeof(decimal)] = new[] { "DECIMAL", "NUMERIC" },
            [typeof(string)] = new[] { "VARCHAR", "TEXT", "NVARCHAR" },
            [typeof(bool)] = new[] { "BOOLEAN", "BIT", "TINYINT" },
            [typeof(DateTime)] = new[] { "DATETIME", "TIMESTAMP" },
            [typeof(Guid)] = new[] { "CHAR(36)", "VARCHAR(36)", "UUID" },
            [typeof(byte[])] = new[] { "BLOB", "VARBINARY", "LONGBLOB" }
        },
        ["PostgreSQL"] = new Dictionary<Type, string[]>
        {
            [typeof(int)] = new[] { "INTEGER", "INT", "INT4" },
            [typeof(long)] = new[] { "BIGINT", "INT8" },
            [typeof(short)] = new[] { "SMALLINT", "INT2" },
            [typeof(byte)] = new[] { "SMALLINT", "INT2" },
            [typeof(float)] = new[] { "REAL", "FLOAT4" },
            [typeof(double)] = new[] { "DOUBLE PRECISION", "FLOAT8" },
            [typeof(decimal)] = new[] { "DECIMAL", "NUMERIC" },
            [typeof(string)] = new[] { "VARCHAR", "TEXT", "CHARACTER VARYING" },
            [typeof(bool)] = new[] { "BOOLEAN", "BOOL" },
            [typeof(DateTime)] = new[] { "TIMESTAMP", "TIMESTAMPTZ" },
            [typeof(Guid)] = new[] { "UUID" },
            [typeof(byte[])] = new[] { "BYTEA" }
        },
        ["SQL Server"] = new Dictionary<Type, string[]>
        {
            [typeof(int)] = new[] { "INT" },
            [typeof(long)] = new[] { "BIGINT" },
            [typeof(short)] = new[] { "SMALLINT" },
            [typeof(byte)] = new[] { "TINYINT" },
            [typeof(float)] = new[] { "REAL" },
            [typeof(double)] = new[] { "FLOAT" },
            [typeof(decimal)] = new[] { "DECIMAL", "NUMERIC" },
            [typeof(string)] = new[] { "NVARCHAR", "VARCHAR", "NTEXT" },
            [typeof(bool)] = new[] { "BIT" },
            [typeof(DateTime)] = new[] { "DATETIME", "DATETIME2" },
            [typeof(Guid)] = new[] { "UNIQUEIDENTIFIER" },
            [typeof(byte[])] = new[] { "VARBINARY", "IMAGE" }
        },
        ["SQLite"] = new Dictionary<Type, string[]>
        {
            [typeof(int)] = new[] { "INTEGER", "INT" },
            [typeof(long)] = new[] { "INTEGER", "BIGINT" },
            [typeof(short)] = new[] { "INTEGER", "SMALLINT" },
            [typeof(byte)] = new[] { "INTEGER", "TINYINT" },
            [typeof(float)] = new[] { "REAL" },
            [typeof(double)] = new[] { "REAL" },
            [typeof(decimal)] = new[] { "REAL", "NUMERIC" },
            [typeof(string)] = new[] { "TEXT" },
            [typeof(bool)] = new[] { "INTEGER", "BOOLEAN" },
            [typeof(DateTime)] = new[] { "TEXT", "DATETIME" },
            [typeof(Guid)] = new[] { "TEXT", "BLOB" },
            [typeof(byte[])] = new[] { "BLOB" }
        }
    };

    /// <summary>
    /// **Property 11: Data Type Mapping Correctness**
    /// *For any* .NET type and *for any* database dialect, 
    /// GetDatabaseTypeName SHALL return valid database type name.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(TypeMappingArbitraries) })]
    public Property GetDatabaseTypeName_ForAnyTypeAndDialect_ShouldReturnValidTypeName(
        TypeMappingDialectProviderWithConfig providerConfig,
        Type dotNetType)
    {
        if (dotNetType == null || !SupportedTypes.Contains(dotNetType))
            return true.ToProperty();

        var config = providerConfig.Config;

        // Act
        var result = providerConfig.GetDatabaseTypeName(dotNetType);

        // Assert - result should be non-empty and valid for the dialect
        var isNonEmpty = !string.IsNullOrWhiteSpace(result);
        var isValidType = ExpectedTypeMappings.TryGetValue(config.DialectName, out var dialectMappings) &&
                          dialectMappings.TryGetValue(dotNetType, out var validTypes) &&
                          validTypes.Any(vt => result.StartsWith(vt, StringComparison.OrdinalIgnoreCase));

        return (isNonEmpty && isValidType)
            .Label($"Dialect: {config.DialectName}, " +
                   $".NET Type: {dotNetType.Name}, " +
                   $"Result: '{result}'");
    }

    /// <summary>
    /// Property: Type mapping should return non-empty string for all supported types.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(TypeMappingArbitraries) })]
    public Property GetDatabaseTypeName_ShouldReturnNonEmptyString(
        TypeMappingDialectProviderWithConfig providerConfig,
        Type dotNetType)
    {
        if (dotNetType == null || !SupportedTypes.Contains(dotNetType))
            return true.ToProperty();

        // Act
        var result = providerConfig.GetDatabaseTypeName(dotNetType);

        // Assert
        return (!string.IsNullOrWhiteSpace(result))
            .Label($"Dialect: {providerConfig.Config.DialectName}, " +
                   $".NET Type: {dotNetType.Name}, " +
                   $"Result: '{result}'");
    }

    /// <summary>
    /// Property: Type mapping should be consistent across multiple calls.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = new[] { typeof(TypeMappingArbitraries) })]
    public Property GetDatabaseTypeName_ShouldBeConsistentAcrossMultipleCalls(
        TypeMappingDialectProviderWithConfig providerConfig,
        Type dotNetType)
    {
        if (dotNetType == null || !SupportedTypes.Contains(dotNetType))
            return true.ToProperty();

        // Act
        var result1 = providerConfig.GetDatabaseTypeName(dotNetType);
        var result2 = providerConfig.GetDatabaseTypeName(dotNetType);

        // Assert
        return (result1 == result2)
            .Label($"Dialect: {providerConfig.Config.DialectName}, " +
                   $".NET Type: {dotNetType.Name}, " +
                   $"Result1: '{result1}', Result2: '{result2}'");
    }

    #region Int32 Type Mapping Tests (Requirement 9.1)

    /// <summary>
    /// Property: Int32 should map to appropriate integer type.
    /// **Validates: Requirements 9.1**
    /// </summary>
    [Fact]
    public void MySQL_GetDatabaseTypeName_Int32_ShouldReturnInt()
    {
        var result = MySqlProvider.GetDatabaseTypeName(typeof(int));
        Assert.Equal("INT", result);
    }

    [Fact]
    public void PostgreSQL_GetDatabaseTypeName_Int32_ShouldReturnInteger()
    {
        var result = PostgreSqlProvider.GetDatabaseTypeName(typeof(int));
        Assert.Equal("INTEGER", result);
    }

    [Fact]
    public void SqlServer_GetDatabaseTypeName_Int32_ShouldReturnInt()
    {
        var result = SqlServerProvider.GetDatabaseTypeName(typeof(int));
        Assert.Equal("INT", result);
    }

    [Fact]
    public void SQLite_GetDatabaseTypeName_Int32_ShouldReturnInteger()
    {
        var result = SQLiteProvider.GetDatabaseTypeName(typeof(int));
        Assert.Equal("INTEGER", result);
    }

    #endregion

    #region Int64 Type Mapping Tests (Requirement 9.2)

    /// <summary>
    /// Property: Int64 should map to appropriate bigint type.
    /// **Validates: Requirements 9.2**
    /// </summary>
    [Fact]
    public void MySQL_GetDatabaseTypeName_Int64_ShouldReturnBigInt()
    {
        var result = MySqlProvider.GetDatabaseTypeName(typeof(long));
        Assert.Equal("BIGINT", result);
    }

    [Fact]
    public void PostgreSQL_GetDatabaseTypeName_Int64_ShouldReturnBigInt()
    {
        var result = PostgreSqlProvider.GetDatabaseTypeName(typeof(long));
        Assert.Equal("BIGINT", result);
    }

    [Fact]
    public void SqlServer_GetDatabaseTypeName_Int64_ShouldReturnBigInt()
    {
        var result = SqlServerProvider.GetDatabaseTypeName(typeof(long));
        Assert.Equal("BIGINT", result);
    }

    [Fact]
    public void SQLite_GetDatabaseTypeName_Int64_ShouldReturnInteger()
    {
        var result = SQLiteProvider.GetDatabaseTypeName(typeof(long));
        Assert.Equal("INTEGER", result);
    }

    #endregion

    #region String Type Mapping Tests (Requirement 9.3)

    /// <summary>
    /// Property: String should map to appropriate varchar/text type.
    /// **Validates: Requirements 9.3**
    /// </summary>
    [Fact]
    public void MySQL_GetDatabaseTypeName_String_ShouldReturnVarchar()
    {
        var result = MySqlProvider.GetDatabaseTypeName(typeof(string));
        Assert.StartsWith("VARCHAR", result);
    }

    [Fact]
    public void PostgreSQL_GetDatabaseTypeName_String_ShouldReturnVarchar()
    {
        var result = PostgreSqlProvider.GetDatabaseTypeName(typeof(string));
        Assert.StartsWith("VARCHAR", result);
    }

    [Fact]
    public void SqlServer_GetDatabaseTypeName_String_ShouldReturnNVarchar()
    {
        var result = SqlServerProvider.GetDatabaseTypeName(typeof(string));
        Assert.StartsWith("NVARCHAR", result);
    }

    [Fact]
    public void SQLite_GetDatabaseTypeName_String_ShouldReturnText()
    {
        var result = SQLiteProvider.GetDatabaseTypeName(typeof(string));
        Assert.Equal("TEXT", result);
    }

    #endregion

    #region DateTime Type Mapping Tests (Requirement 9.4)

    /// <summary>
    /// Property: DateTime should map to appropriate datetime/timestamp type.
    /// **Validates: Requirements 9.4**
    /// </summary>
    [Fact]
    public void MySQL_GetDatabaseTypeName_DateTime_ShouldReturnDatetime()
    {
        var result = MySqlProvider.GetDatabaseTypeName(typeof(DateTime));
        Assert.Equal("DATETIME", result);
    }

    [Fact]
    public void PostgreSQL_GetDatabaseTypeName_DateTime_ShouldReturnTimestamp()
    {
        var result = PostgreSqlProvider.GetDatabaseTypeName(typeof(DateTime));
        Assert.Equal("TIMESTAMP", result);
    }

    [Fact]
    public void SqlServer_GetDatabaseTypeName_DateTime_ShouldReturnDatetime2()
    {
        var result = SqlServerProvider.GetDatabaseTypeName(typeof(DateTime));
        Assert.Equal("DATETIME2", result);
    }

    [Fact]
    public void SQLite_GetDatabaseTypeName_DateTime_ShouldReturnText()
    {
        var result = SQLiteProvider.GetDatabaseTypeName(typeof(DateTime));
        Assert.Equal("TEXT", result);
    }

    #endregion

    #region Boolean Type Mapping Tests (Requirement 9.5)

    /// <summary>
    /// Property: Boolean should map to appropriate boolean/bit/integer type.
    /// **Validates: Requirements 9.5**
    /// </summary>
    [Fact]
    public void MySQL_GetDatabaseTypeName_Boolean_ShouldReturnBoolean()
    {
        var result = MySqlProvider.GetDatabaseTypeName(typeof(bool));
        Assert.Equal("BOOLEAN", result);
    }

    [Fact]
    public void PostgreSQL_GetDatabaseTypeName_Boolean_ShouldReturnBoolean()
    {
        var result = PostgreSqlProvider.GetDatabaseTypeName(typeof(bool));
        Assert.Equal("BOOLEAN", result);
    }

    [Fact]
    public void SqlServer_GetDatabaseTypeName_Boolean_ShouldReturnBit()
    {
        var result = SqlServerProvider.GetDatabaseTypeName(typeof(bool));
        Assert.Equal("BIT", result);
    }

    [Fact]
    public void SQLite_GetDatabaseTypeName_Boolean_ShouldReturnInteger()
    {
        var result = SQLiteProvider.GetDatabaseTypeName(typeof(bool));
        Assert.Equal("INTEGER", result);
    }

    #endregion

    #region Guid Type Mapping Tests (Requirement 9.6)

    /// <summary>
    /// Property: Guid should map to appropriate uuid/uniqueidentifier/char type.
    /// **Validates: Requirements 9.6**
    /// </summary>
    [Fact]
    public void MySQL_GetDatabaseTypeName_Guid_ShouldReturnChar36()
    {
        var result = MySqlProvider.GetDatabaseTypeName(typeof(Guid));
        Assert.Equal("CHAR(36)", result);
    }

    [Fact]
    public void PostgreSQL_GetDatabaseTypeName_Guid_ShouldReturnUuid()
    {
        var result = PostgreSqlProvider.GetDatabaseTypeName(typeof(Guid));
        Assert.Equal("UUID", result);
    }

    [Fact]
    public void SqlServer_GetDatabaseTypeName_Guid_ShouldReturnUniqueIdentifier()
    {
        var result = SqlServerProvider.GetDatabaseTypeName(typeof(Guid));
        Assert.Equal("UNIQUEIDENTIFIER", result);
    }

    [Fact]
    public void SQLite_GetDatabaseTypeName_Guid_ShouldReturnText()
    {
        var result = SQLiteProvider.GetDatabaseTypeName(typeof(Guid));
        Assert.Equal("TEXT", result);
    }

    #endregion

    #region Decimal Type Mapping Tests (Requirement 9.7)

    /// <summary>
    /// Property: Decimal should map to appropriate decimal/numeric type.
    /// **Validates: Requirements 9.7**
    /// </summary>
    [Fact]
    public void MySQL_GetDatabaseTypeName_Decimal_ShouldReturnDecimal()
    {
        var result = MySqlProvider.GetDatabaseTypeName(typeof(decimal));
        Assert.StartsWith("DECIMAL", result);
    }

    [Fact]
    public void PostgreSQL_GetDatabaseTypeName_Decimal_ShouldReturnDecimal()
    {
        var result = PostgreSqlProvider.GetDatabaseTypeName(typeof(decimal));
        Assert.StartsWith("DECIMAL", result);
    }

    [Fact]
    public void SqlServer_GetDatabaseTypeName_Decimal_ShouldReturnDecimal()
    {
        var result = SqlServerProvider.GetDatabaseTypeName(typeof(decimal));
        Assert.StartsWith("DECIMAL", result);
    }

    [Fact]
    public void SQLite_GetDatabaseTypeName_Decimal_ShouldReturnReal()
    {
        var result = SQLiteProvider.GetDatabaseTypeName(typeof(decimal));
        Assert.Equal("REAL", result);
    }

    #endregion
}


/// <summary>
/// Wrapper class to pair a dialect provider with its test configuration for Type Mapping tests.
/// </summary>
public class TypeMappingDialectProviderWithConfig
{
    public Func<Type, string> GetDatabaseTypeName { get; }
    public DialectTestConfig Config { get; }

    public TypeMappingDialectProviderWithConfig(
        Func<Type, string> getDatabaseTypeName,
        DialectTestConfig config)
    {
        GetDatabaseTypeName = getDatabaseTypeName;
        Config = config;
    }

    public override string ToString() => Config.DialectName;
}

/// <summary>
/// Custom arbitraries for Type Mapping property tests.
/// </summary>
public static class TypeMappingArbitraries
{
    private static readonly MySqlDialectProvider MySqlProvider = new();
    private static readonly PostgreSqlDialectProvider PostgreSqlProvider = new();
    private static readonly SqlServerDialectProvider SqlServerProvider = new();
    private static readonly SQLiteDialectProvider SQLiteProvider = new();

    /// <summary>
    /// All supported .NET types for type mapping tests.
    /// </summary>
    private static readonly Type[] SupportedTypes = new[]
    {
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
        typeof(Guid),
        typeof(byte[])
    };

    /// <summary>
    /// Generates dialect provider with its corresponding test configuration.
    /// </summary>
    public static Arbitrary<TypeMappingDialectProviderWithConfig> TypeMappingDialectProviderWithConfig()
    {
        return Gen.Elements(
            new TypeMappingDialectProviderWithConfig(
                MySqlProvider.GetDatabaseTypeName,
                DialectTestConfig.MySql),
            new TypeMappingDialectProviderWithConfig(
                PostgreSqlProvider.GetDatabaseTypeName,
                DialectTestConfig.PostgreSql),
            new TypeMappingDialectProviderWithConfig(
                SqlServerProvider.GetDatabaseTypeName,
                DialectTestConfig.SqlServer),
            new TypeMappingDialectProviderWithConfig(
                SQLiteProvider.GetDatabaseTypeName,
                DialectTestConfig.SQLite)
        ).ToArbitrary();
    }

    /// <summary>
    /// Generates valid .NET types for type mapping tests.
    /// </summary>
    public static Arbitrary<Type> ValidDotNetType()
    {
        return Gen.Elements(SupportedTypes).ToArbitrary();
    }
}
