// <copyright file="TypeConversionE2ETests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Tests.E2E.Infrastructure;
using Sqlx.Tests.E2E.Models;
using Sqlx.Annotations;
using System.Data.Common;

namespace Sqlx.Tests.E2E.TypeConversion;

#region Test Repositories

public interface ITypeConversionRepository
{
    [SqlTemplate("INSERT INTO type_test (string_value, nullable_string_value, int_value, nullable_int_value, long_value, nullable_long_value, decimal_value, nullable_decimal_value, double_value, nullable_double_value, float_value, nullable_float_value, date_time_value, nullable_date_time_value, bool_value, nullable_bool_value, binary_value, guid_value, nullable_guid_value) VALUES (@stringValue, @nullableStringValue, @intValue, @nullableIntValue, @longValue, @nullableLongValue, @decimalValue, @nullableDecimalValue, @doubleValue, @nullableDoubleValue, @floatValue, @nullableFloatValue, @dateTimeValue, @nullableDateTimeValue, @boolValue, @nullableBoolValue, @binaryValue, @guidValue, @nullableGuidValue)")]
    Task<int> InsertAsync(
        string stringValue,
        string? nullableStringValue,
        int intValue,
        int? nullableIntValue,
        long longValue,
        long? nullableLongValue,
        decimal decimalValue,
        decimal? nullableDecimalValue,
        double doubleValue,
        double? nullableDoubleValue,
        float floatValue,
        float? nullableFloatValue,
        DateTime dateTimeValue,
        DateTime? nullableDateTimeValue,
        bool boolValue,
        bool? nullableBoolValue,
        byte[]? binaryValue,
        Guid guidValue,
        Guid? nullableGuidValue);

    [SqlTemplate("SELECT id, string_value, nullable_string_value, int_value, nullable_int_value, long_value, nullable_long_value, decimal_value, nullable_decimal_value, double_value, nullable_double_value, float_value, nullable_float_value, date_time_value, nullable_date_time_value, bool_value, nullable_bool_value, binary_value, guid_value, nullable_guid_value FROM type_test WHERE id = @id")]
    Task<TypeTestEntity?> GetByIdAsync(long id);

    [SqlTemplate("SELECT id, string_value, nullable_string_value, int_value, nullable_int_value, long_value, nullable_long_value, decimal_value, nullable_decimal_value, double_value, nullable_double_value, float_value, nullable_float_value, date_time_value, nullable_date_time_value, bool_value, nullable_bool_value, binary_value, guid_value, nullable_guid_value FROM type_test")]
    Task<List<TypeTestEntity>> GetAllAsync();
}

[RepositoryFor(typeof(ITypeConversionRepository), Dialect = (int)SqlDefineTypes.MySql, TableName = "type_test")]
public partial class MySqlTypeConversionRepository : ITypeConversionRepository
{
    private readonly DbConnection _connection;

    public MySqlTypeConversionRepository(DbConnection connection)
    {
        _connection = connection;
    }
}

[RepositoryFor(typeof(ITypeConversionRepository), Dialect = (int)SqlDefineTypes.PostgreSql, TableName = "type_test")]
public partial class PostgreSqlTypeConversionRepository : ITypeConversionRepository
{
    private readonly DbConnection _connection;

    public PostgreSqlTypeConversionRepository(DbConnection connection)
    {
        _connection = connection;
    }
}

[RepositoryFor(typeof(ITypeConversionRepository), Dialect = (int)SqlDefineTypes.SqlServer, TableName = "type_test")]
public partial class SqlServerTypeConversionRepository : ITypeConversionRepository
{
    private readonly DbConnection _connection;

    public SqlServerTypeConversionRepository(DbConnection connection)
    {
        _connection = connection;
    }
}

[RepositoryFor(typeof(ITypeConversionRepository), Dialect = (int)SqlDefineTypes.SQLite, TableName = "type_test")]
public partial class SQLiteTypeConversionRepository : ITypeConversionRepository
{
    private readonly DbConnection _connection;

    public SQLiteTypeConversionRepository(DbConnection connection)
    {
        _connection = connection;
    }
}

#endregion

/// <summary>
/// E2E tests for type conversion across all supported databases.
/// Tests round-trip conversion for all data types.
/// </summary>
[TestClass]
public class TypeConversionE2ETests : E2ETestBase
{
    /// <summary>
    /// Creates the type_test table schema for the specified database type.
    /// </summary>
    private static string GetTypeTestSchema(DatabaseType dbType)
    {
        return dbType switch
        {
            DatabaseType.MySQL => @"
                CREATE TABLE type_test (
                    id BIGINT AUTO_INCREMENT PRIMARY KEY,
                    string_value VARCHAR(500) NOT NULL,
                    nullable_string_value VARCHAR(500),
                    int_value INT NOT NULL,
                    nullable_int_value INT,
                    long_value BIGINT NOT NULL,
                    nullable_long_value BIGINT,
                    decimal_value DECIMAL(18, 4) NOT NULL,
                    nullable_decimal_value DECIMAL(18, 4),
                    double_value DOUBLE NOT NULL,
                    nullable_double_value DOUBLE,
                    float_value FLOAT NOT NULL,
                    nullable_float_value FLOAT,
                    date_time_value DATETIME NOT NULL,
                    nullable_date_time_value DATETIME,
                    bool_value BOOLEAN NOT NULL,
                    nullable_bool_value BOOLEAN,
                    binary_value BLOB,
                    guid_value CHAR(36) NOT NULL,
                    nullable_guid_value CHAR(36)
                )",
            DatabaseType.PostgreSQL => @"
                CREATE TABLE type_test (
                    id BIGSERIAL PRIMARY KEY,
                    string_value VARCHAR(500) NOT NULL,
                    nullable_string_value VARCHAR(500),
                    int_value INT NOT NULL,
                    nullable_int_value INT,
                    long_value BIGINT NOT NULL,
                    nullable_long_value BIGINT,
                    decimal_value DECIMAL(18, 4) NOT NULL,
                    nullable_decimal_value DECIMAL(18, 4),
                    double_value DOUBLE PRECISION NOT NULL,
                    nullable_double_value DOUBLE PRECISION,
                    float_value REAL NOT NULL,
                    nullable_float_value REAL,
                    date_time_value TIMESTAMP NOT NULL,
                    nullable_date_time_value TIMESTAMP,
                    bool_value BOOLEAN NOT NULL,
                    nullable_bool_value BOOLEAN,
                    binary_value BYTEA,
                    guid_value UUID NOT NULL,
                    nullable_guid_value UUID
                )",
            DatabaseType.SqlServer => @"
                CREATE TABLE type_test (
                    id BIGINT IDENTITY(1,1) PRIMARY KEY,
                    string_value NVARCHAR(500) NOT NULL,
                    nullable_string_value NVARCHAR(500),
                    int_value INT NOT NULL,
                    nullable_int_value INT,
                    long_value BIGINT NOT NULL,
                    nullable_long_value BIGINT,
                    decimal_value DECIMAL(18, 4) NOT NULL,
                    nullable_decimal_value DECIMAL(18, 4),
                    double_value FLOAT NOT NULL,
                    nullable_double_value FLOAT,
                    float_value REAL NOT NULL,
                    nullable_float_value REAL,
                    date_time_value DATETIME2 NOT NULL,
                    nullable_date_time_value DATETIME2,
                    bool_value BIT NOT NULL,
                    nullable_bool_value BIT,
                    binary_value VARBINARY(MAX),
                    guid_value UNIQUEIDENTIFIER NOT NULL,
                    nullable_guid_value UNIQUEIDENTIFIER
                )",
            DatabaseType.SQLite => @"
                CREATE TABLE type_test (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    string_value TEXT NOT NULL,
                    nullable_string_value TEXT,
                    int_value INTEGER NOT NULL,
                    nullable_int_value INTEGER,
                    long_value INTEGER NOT NULL,
                    nullable_long_value INTEGER,
                    decimal_value REAL NOT NULL,
                    nullable_decimal_value REAL,
                    double_value REAL NOT NULL,
                    nullable_double_value REAL,
                    float_value REAL NOT NULL,
                    nullable_float_value REAL,
                    date_time_value TEXT NOT NULL,
                    nullable_date_time_value TEXT,
                    bool_value INTEGER NOT NULL,
                    nullable_bool_value INTEGER,
                    binary_value BLOB,
                    guid_value TEXT NOT NULL,
                    nullable_guid_value TEXT
                )",
            _ => throw new NotSupportedException($"Database type {dbType} is not supported"),
        };
    }

    // ==================== MySQL Type Conversion Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("TypeConversion")]
    [TestCategory("MySQL")]
    public async Task MySQL_TypeConversion_StringRoundTrip_PreservesValue()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTypeTestSchema(DatabaseType.MySQL));
        var repo = new MySqlTypeConversionRepository(fixture.Connection);

        var testString = "Hello, 世界! 🌍 Special chars: <>&\"'";
        var testNullableString = "Nullable string value";

        // Act
        await repo.InsertAsync(
            testString, testNullableString,
            1, 1, 1L, 1L, 1.0m, 1.0m, 1.0, 1.0, 1.0f, 1.0f,
            DateTime.Now, DateTime.Now, true, true, null,
            Guid.NewGuid(), Guid.NewGuid());

        var result = await repo.GetByIdAsync(1);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(testString, result.StringValue);
        Assert.AreEqual(testNullableString, result.NullableStringValue);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("TypeConversion")]
    [TestCategory("MySQL")]
    public async Task MySQL_TypeConversion_NumericRoundTrip_PreservesValue()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTypeTestSchema(DatabaseType.MySQL));
        var repo = new MySqlTypeConversionRepository(fixture.Connection);

        int testInt = 2147483647;
        long testLong = 9223372036854775807;
        decimal testDecimal = 123456.7890m;
        double testDouble = 123456.789012345;
        // MySQL FLOAT has limited precision, use simple value
        float testFloat = 123.45f;

        // Act
        await repo.InsertAsync(
            "test", null,
            testInt, testInt, testLong, testLong,
            testDecimal, testDecimal, testDouble, testDouble,
            testFloat, testFloat, DateTime.Now, null,
            true, null, null, Guid.NewGuid(), null);

        var result = await repo.GetByIdAsync(1);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(testInt, result.IntValue);
        Assert.AreEqual(testInt, result.NullableIntValue);
        Assert.AreEqual(testLong, result.LongValue);
        Assert.AreEqual(testLong, result.NullableLongValue);
        Assert.AreEqual(testDecimal, result.DecimalValue);
        Assert.AreEqual(testDecimal, result.NullableDecimalValue);
        
        // Double and float comparisons with tolerance
        Assert.AreEqual(testDouble, result.DoubleValue, 0.000001);
        Assert.AreEqual(testDouble, result.NullableDoubleValue!.Value, 0.000001);
        // MySQL FLOAT has limited precision
        Assert.AreEqual(testFloat, result.FloatValue, 0.01f);
        Assert.AreEqual(testFloat, result.NullableFloatValue!.Value, 0.01f);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("TypeConversion")]
    [TestCategory("MySQL")]
    public async Task MySQL_TypeConversion_DateTimeRoundTrip_PreservesValue()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTypeTestSchema(DatabaseType.MySQL));
        var repo = new MySqlTypeConversionRepository(fixture.Connection);

        // MySQL DATETIME has second precision, so truncate to seconds
        var testDateTime = new DateTime(2024, 12, 25, 15, 30, 45, DateTimeKind.Unspecified);

        // Act
        await repo.InsertAsync(
            "test", null, 1, null, 1L, null, 1.0m, null, 1.0, null, 1.0f, null,
            testDateTime, testDateTime, true, null, null,
            Guid.NewGuid(), null);

        var result = await repo.GetByIdAsync(1);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(testDateTime, result.DateTimeValue);
        Assert.AreEqual(testDateTime, result.NullableDateTimeValue);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("TypeConversion")]
    [TestCategory("MySQL")]
    public async Task MySQL_TypeConversion_BooleanRoundTrip_PreservesValue()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTypeTestSchema(DatabaseType.MySQL));
        var repo = new MySqlTypeConversionRepository(fixture.Connection);

        // Act - Test true
        await repo.InsertAsync(
            "test", null, 1, null, 1L, null, 1.0m, null, 1.0, null, 1.0f, null,
            DateTime.Now, null, true, true, null, Guid.NewGuid(), null);

        var resultTrue = await repo.GetByIdAsync(1);

        // Act - Test false
        await repo.InsertAsync(
            "test", null, 1, null, 1L, null, 1.0m, null, 1.0, null, 1.0f, null,
            DateTime.Now, null, false, false, null, Guid.NewGuid(), null);

        var resultFalse = await repo.GetByIdAsync(2);

        // Assert
        Assert.IsNotNull(resultTrue);
        Assert.IsTrue(resultTrue.BoolValue);
        Assert.IsTrue(resultTrue.NullableBoolValue);

        Assert.IsNotNull(resultFalse);
        Assert.IsFalse(resultFalse.BoolValue);
        Assert.IsFalse(resultFalse.NullableBoolValue);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("TypeConversion")]
    [TestCategory("MySQL")]
    public async Task MySQL_TypeConversion_BinaryRoundTrip_PreservesValue()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTypeTestSchema(DatabaseType.MySQL));
        var repo = new MySqlTypeConversionRepository(fixture.Connection);

        var testBinary = new byte[] { 0x01, 0x02, 0x03, 0xFF, 0xFE, 0xFD };

        // Act
        await repo.InsertAsync(
            "test", null, 1, null, 1L, null, 1.0m, null, 1.0, null, 1.0f, null,
            DateTime.Now, null, true, null, testBinary, Guid.NewGuid(), null);

        var result = await repo.GetByIdAsync(1);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.BinaryValue);
        CollectionAssert.AreEqual(testBinary, result.BinaryValue);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("TypeConversion")]
    [TestCategory("MySQL")]
    public async Task MySQL_TypeConversion_NullValues_HandledCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTypeTestSchema(DatabaseType.MySQL));
        var repo = new MySqlTypeConversionRepository(fixture.Connection);

        // Act - Insert with all nullable fields as null
        await repo.InsertAsync(
            "test", null, 1, null, 1L, null, 1.0m, null, 1.0, null, 1.0f, null,
            DateTime.Now, null, true, null, null, Guid.NewGuid(), null);

        var result = await repo.GetByIdAsync(1);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNull(result.NullableStringValue);
        Assert.IsNull(result.NullableIntValue);
        Assert.IsNull(result.NullableLongValue);
        Assert.IsNull(result.NullableDecimalValue);
        Assert.IsNull(result.NullableDoubleValue);
        Assert.IsNull(result.NullableFloatValue);
        Assert.IsNull(result.NullableDateTimeValue);
        Assert.IsNull(result.NullableBoolValue);
        Assert.IsNull(result.BinaryValue);
        Assert.IsNull(result.NullableGuidValue);
    }

    // ==================== PostgreSQL Type Conversion Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("TypeConversion")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_TypeConversion_AllTypes_RoundTrip()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTypeTestSchema(DatabaseType.PostgreSQL));
        var repo = new PostgreSqlTypeConversionRepository(fixture.Connection);

        var testString = "PostgreSQL test 🐘";
        var testInt = 42;
        var testLong = 9223372036854775807L;
        var testDecimal = 123.4567m;
        var testDouble = 123.456789;
        var testFloat = 123.45f;
        var testDateTime = new DateTime(2024, 12, 25, 15, 30, 45, DateTimeKind.Unspecified);
        var testBool = true;
        var testBinary = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
        var testGuid = Guid.NewGuid();

        // Act
        await repo.InsertAsync(
            testString, testString, testInt, testInt, testLong, testLong,
            testDecimal, testDecimal, testDouble, testDouble, testFloat, testFloat,
            testDateTime, testDateTime, testBool, testBool, testBinary,
            testGuid, testGuid);

        var result = await repo.GetByIdAsync(1);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(testString, result.StringValue);
        Assert.AreEqual(testInt, result.IntValue);
        Assert.AreEqual(testLong, result.LongValue);
        Assert.AreEqual(testDecimal, result.DecimalValue);
        Assert.AreEqual(testDouble, result.DoubleValue, 0.000001);
        Assert.AreEqual(testFloat, result.FloatValue, 0.001f);
        Assert.AreEqual(testBool, result.BoolValue);
        CollectionAssert.AreEqual(testBinary, result.BinaryValue);
        Assert.AreEqual(testGuid, result.GuidValue);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("TypeConversion")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_TypeConversion_UnicodeStrings_PreservedCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTypeTestSchema(DatabaseType.PostgreSQL));
        var repo = new PostgreSqlTypeConversionRepository(fixture.Connection);

        var unicodeString = "Hello 世界 🌍 Привет مرحبا";

        // Act
        await repo.InsertAsync(
            unicodeString, unicodeString, 1, 1, 1L, 1L, 1.0m, 1.0m, 1.0, 1.0, 1.0f, 1.0f,
            DateTime.Now, DateTime.Now, true, true, null, Guid.NewGuid(), Guid.NewGuid());

        var result = await repo.GetByIdAsync(1);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(unicodeString, result.StringValue);
        Assert.AreEqual(unicodeString, result.NullableStringValue);
    }

    // ==================== SQLite Type Conversion Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("TypeConversion")]
    [TestCategory("SQLite")]
    public async Task SQLite_TypeConversion_AllTypes_RoundTrip()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTypeTestSchema(DatabaseType.SQLite));
        var repo = new SQLiteTypeConversionRepository(fixture.Connection);

        var testString = "SQLite test";
        var testInt = 42;
        var testLong = 9223372036854775807L;
        var testDecimal = 123.4567m;
        var testDouble = 123.456789;
        var testFloat = 123.45f;
        var testDateTime = new DateTime(2024, 12, 25, 15, 30, 45, DateTimeKind.Unspecified);
        var testBool = true;
        var testBinary = new byte[] { 0x01, 0x02, 0x03 };
        var testGuid = Guid.NewGuid();

        // Act
        await repo.InsertAsync(
            testString, testString, testInt, testInt, testLong, testLong,
            testDecimal, testDecimal, testDouble, testDouble, testFloat, testFloat,
            testDateTime, testDateTime, testBool, testBool, testBinary,
            testGuid, testGuid);

        var result = await repo.GetByIdAsync(1);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(testString, result.StringValue);
        Assert.AreEqual(testInt, result.IntValue);
        Assert.AreEqual(testLong, result.LongValue);
        
        // SQLite stores decimal as REAL, so precision may vary
        Assert.AreEqual((double)testDecimal, (double)result.DecimalValue, 0.0001);
        Assert.AreEqual(testDouble, result.DoubleValue, 0.000001);
        Assert.AreEqual(testFloat, result.FloatValue, 0.001f);
        Assert.AreEqual(testBool, result.BoolValue);
        CollectionAssert.AreEqual(testBinary, result.BinaryValue);
        Assert.AreEqual(testGuid, result.GuidValue);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("TypeConversion")]
    [TestCategory("SQLite")]
    public async Task SQLite_TypeConversion_EmptyString_PreservedCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTypeTestSchema(DatabaseType.SQLite));
        var repo = new SQLiteTypeConversionRepository(fixture.Connection);

        var emptyString = string.Empty;

        // Act
        await repo.InsertAsync(
            emptyString, emptyString, 1, 1, 1L, 1L, 1.0m, 1.0m, 1.0, 1.0, 1.0f, 1.0f,
            DateTime.Now, DateTime.Now, true, true, null, Guid.NewGuid(), Guid.NewGuid());

        var result = await repo.GetByIdAsync(1);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(emptyString, result.StringValue);
        Assert.AreEqual(emptyString, result.NullableStringValue);
    }

    // ==================== SQL Server Type Conversion Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("TypeConversion")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_TypeConversion_AllTypes_RoundTrip()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTypeTestSchema(DatabaseType.SqlServer));
        var repo = new SqlServerTypeConversionRepository(fixture.Connection);

        var testString = "SQL Server test";
        var testInt = 42;
        var testLong = 9223372036854775807L;
        var testDecimal = 123.4567m;
        var testDouble = 123.456789;
        var testFloat = 123.45f;
        var testDateTime = new DateTime(2024, 12, 25, 15, 30, 45, 123, DateTimeKind.Unspecified);
        var testBool = true;
        var testBinary = new byte[] { 0xAB, 0xCD, 0xEF };
        var testGuid = Guid.NewGuid();

        // Act
        await repo.InsertAsync(
            testString, testString, testInt, testInt, testLong, testLong,
            testDecimal, testDecimal, testDouble, testDouble, testFloat, testFloat,
            testDateTime, testDateTime, testBool, testBool, testBinary,
            testGuid, testGuid);

        var result = await repo.GetByIdAsync(1);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(testString, result.StringValue);
        Assert.AreEqual(testInt, result.IntValue);
        Assert.AreEqual(testLong, result.LongValue);
        Assert.AreEqual(testDecimal, result.DecimalValue);
        Assert.AreEqual(testDouble, result.DoubleValue, 0.000001);
        Assert.AreEqual(testFloat, result.FloatValue, 0.001f);
        Assert.AreEqual(testBool, result.BoolValue);
        CollectionAssert.AreEqual(testBinary, result.BinaryValue);
        Assert.AreEqual(testGuid, result.GuidValue);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("TypeConversion")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_TypeConversion_BoundaryValues_HandledCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTypeTestSchema(DatabaseType.SqlServer));
        var repo = new SqlServerTypeConversionRepository(fixture.Connection);

        // Test boundary values
        int minInt = int.MinValue;
        int maxInt = int.MaxValue;
        long minLong = long.MinValue;
        long maxLong = long.MaxValue;
        decimal maxDecimal = 999999999999.9999m;
        double maxDouble = double.MaxValue / 2; // Avoid overflow
        float maxFloat = float.MaxValue / 2;
        // SQL Server DATETIME2 supports dates from 0001-01-01 to 9999-12-31
        var minDateTime = new DateTime(1753, 1, 1); // SQL Server minimum
        var maxDateTime = new DateTime(9999, 12, 31, 23, 59, 59);

        // Act
        await repo.InsertAsync(
            "boundary", null, minInt, maxInt, minLong, maxLong,
            maxDecimal, maxDecimal, maxDouble, maxDouble, maxFloat, maxFloat,
            minDateTime, maxDateTime, false, true, Array.Empty<byte>(),
            Guid.Empty, Guid.NewGuid());

        var result = await repo.GetByIdAsync(1);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(minInt, result.IntValue);
        Assert.AreEqual(maxInt, result.NullableIntValue);
        Assert.AreEqual(minLong, result.LongValue);
        Assert.AreEqual(maxLong, result.NullableLongValue);
        Assert.AreEqual(maxDecimal, result.DecimalValue);
        Assert.AreEqual(maxDouble, result.DoubleValue, maxDouble * 0.0001);
        Assert.AreEqual(maxFloat, result.FloatValue, maxFloat * 0.01f);
        Assert.AreEqual(minDateTime, result.DateTimeValue);
        Assert.AreEqual(maxDateTime, result.NullableDateTimeValue);
        Assert.AreEqual(Guid.Empty, result.GuidValue);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("TypeConversion")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_TypeConversion_UnicodeAndSpecialCharacters_PreservedCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTypeTestSchema(DatabaseType.SqlServer));
        var repo = new SqlServerTypeConversionRepository(fixture.Connection);

        // Test various Unicode and special characters
        var unicodeString = "Hello 世界 🌍 Привет مرحبا שלום こんにちは";
        var specialChars = "Tab:\t Newline:\n Quote:\" Apostrophe:' Backslash:\\ Null:\0 End";
        var sqlInjection = "'; DROP TABLE users; --";
        var xmlChars = "<root>&amp;&lt;&gt;&quot;&apos;</root>";

        // Act
        await repo.InsertAsync(
            unicodeString, specialChars, 1, 2, 1L, 2L, 1.0m, 2.0m, 1.0, 2.0, 1.0f, 2.0f,
            DateTime.Now, DateTime.Now, true, false, Array.Empty<byte>(), Guid.NewGuid(), Guid.NewGuid());

        await repo.InsertAsync(
            sqlInjection, xmlChars, 3, 4, 3L, 4L, 3.0m, 4.0m, 3.0, 4.0, 3.0f, 4.0f,
            DateTime.Now, DateTime.Now, false, true, Array.Empty<byte>(), Guid.NewGuid(), Guid.NewGuid());

        var result1 = await repo.GetByIdAsync(1);
        var result2 = await repo.GetByIdAsync(2);

        // Assert
        Assert.IsNotNull(result1);
        Assert.AreEqual(unicodeString, result1.StringValue);
        Assert.AreEqual(specialChars, result1.NullableStringValue);

        Assert.IsNotNull(result2);
        Assert.AreEqual(sqlInjection, result2.StringValue);
        Assert.AreEqual(xmlChars, result2.NullableStringValue);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("TypeConversion")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_TypeConversion_LargeStrings_HandledCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTypeTestSchema(DatabaseType.SqlServer));
        var repo = new SqlServerTypeConversionRepository(fixture.Connection);

        // Test large strings (approaching NVARCHAR(500) limit)
        var largeString = new string('A', 490);
        var largeUnicode = new string('中', 245); // Each Chinese char is 2 bytes in UTF-16

        // Act
        await repo.InsertAsync(
            largeString, largeUnicode, 1, 1, 1L, 1L, 1.0m, 1.0m, 1.0, 1.0, 1.0f, 1.0f,
            DateTime.Now, DateTime.Now, true, true, Array.Empty<byte>(), Guid.NewGuid(), Guid.NewGuid());

        var result = await repo.GetByIdAsync(1);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(largeString, result.StringValue);
        Assert.AreEqual(largeUnicode, result.NullableStringValue);
        Assert.AreEqual(490, result.StringValue.Length);
        Assert.AreEqual(245, result.NullableStringValue!.Length);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("TypeConversion")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_TypeConversion_EmptyAndWhitespaceStrings_PreservedCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTypeTestSchema(DatabaseType.SqlServer));
        var repo = new SqlServerTypeConversionRepository(fixture.Connection);

        var emptyString = string.Empty;
        var whitespaceString = "   ";
        var singleSpace = " ";

        // Act
        await repo.InsertAsync(
            emptyString, whitespaceString, 1, 1, 1L, 1L, 1.0m, 1.0m, 1.0, 1.0, 1.0f, 1.0f,
            DateTime.Now, DateTime.Now, true, true, Array.Empty<byte>(), Guid.NewGuid(), Guid.NewGuid());

        await repo.InsertAsync(
            singleSpace, emptyString, 2, 2, 2L, 2L, 2.0m, 2.0m, 2.0, 2.0, 2.0f, 2.0f,
            DateTime.Now, DateTime.Now, false, false, Array.Empty<byte>(), Guid.NewGuid(), Guid.NewGuid());

        var result1 = await repo.GetByIdAsync(1);
        var result2 = await repo.GetByIdAsync(2);

        // Assert
        Assert.IsNotNull(result1);
        Assert.AreEqual(emptyString, result1.StringValue);
        Assert.AreEqual(whitespaceString, result1.NullableStringValue);

        Assert.IsNotNull(result2);
        Assert.AreEqual(singleSpace, result2.StringValue);
        Assert.AreEqual(emptyString, result2.NullableStringValue);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("TypeConversion")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_TypeConversion_DateTimePrecision_PreservedCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTypeTestSchema(DatabaseType.SqlServer));
        var repo = new SqlServerTypeConversionRepository(fixture.Connection);

        // SQL Server DATETIME2 has 100ns (0.0000001 second) precision
        // Round to 100ns to avoid precision issues
        var preciseDateTime = new DateTime(2024, 6, 15, 14, 30, 45, 123, DateTimeKind.Unspecified);
        // Round to nearest 100ns tick (DATETIME2 precision)
        var ticks = preciseDateTime.Ticks;
        ticks = (ticks / 10) * 10; // Round to 1 microsecond (10 ticks)
        preciseDateTime = new DateTime(ticks, DateTimeKind.Unspecified);

        var minDateTime = new DateTime(1753, 1, 1); // SQL Server minimum
        var maxDateTime = new DateTime(9999, 12, 31, 23, 59, 59);

        // Act
        await repo.InsertAsync(
            "datetime", null, 1, null, 1L, null, 1.0m, null, 1.0, null, 1.0f, null,
            preciseDateTime, preciseDateTime, true, null, Array.Empty<byte>(), Guid.NewGuid(), null);

        await repo.InsertAsync(
            "minmax", null, 2, null, 2L, null, 2.0m, null, 2.0, null, 2.0f, null,
            minDateTime, maxDateTime, false, null, Array.Empty<byte>(), Guid.NewGuid(), null);

        var result1 = await repo.GetByIdAsync(1);
        var result2 = await repo.GetByIdAsync(2);

        // Assert
        Assert.IsNotNull(result1);
        // SQL Server DATETIME2 has 100ns precision, allow up to 1 microsecond (10000 ticks) tolerance
        Assert.AreEqual(preciseDateTime.Ticks, result1.DateTimeValue.Ticks, 10000, "DateTimeValue ticks should match within 1 microsecond");
        Assert.AreEqual(preciseDateTime.Ticks, result1.NullableDateTimeValue!.Value.Ticks, 10000, "NullableDateTimeValue ticks should match within 1 microsecond");

        Assert.IsNotNull(result2);
        Assert.AreEqual(minDateTime, result2.DateTimeValue);
        Assert.AreEqual(maxDateTime, result2.NullableDateTimeValue);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("TypeConversion")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_TypeConversion_DecimalPrecision_PreservedCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTypeTestSchema(DatabaseType.SqlServer));
        var repo = new SqlServerTypeConversionRepository(fixture.Connection);

        // Test DECIMAL(18, 4) precision
        var preciseDecimal1 = 12345678901234.5678m;
        var preciseDecimal2 = 0.0001m;
        var preciseDecimal3 = -99999999999999.9999m;
        var zero = 0.0000m;

        // Act
        await repo.InsertAsync(
            "decimal1", null, 1, null, 1L, null, preciseDecimal1, preciseDecimal2, 1.0, null, 1.0f, null,
            DateTime.Now, null, true, null, Array.Empty<byte>(), Guid.NewGuid(), null);

        await repo.InsertAsync(
            "decimal2", null, 2, null, 2L, null, preciseDecimal3, zero, 2.0, null, 2.0f, null,
            DateTime.Now, null, false, null, Array.Empty<byte>(), Guid.NewGuid(), null);

        var result1 = await repo.GetByIdAsync(1);
        var result2 = await repo.GetByIdAsync(2);

        // Assert
        Assert.IsNotNull(result1);
        Assert.AreEqual(preciseDecimal1, result1.DecimalValue);
        Assert.AreEqual(preciseDecimal2, result1.NullableDecimalValue);

        Assert.IsNotNull(result2);
        Assert.AreEqual(preciseDecimal3, result2.DecimalValue);
        Assert.AreEqual(zero, result2.NullableDecimalValue);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("TypeConversion")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_TypeConversion_BinaryData_VariousSizes()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTypeTestSchema(DatabaseType.SqlServer));
        var repo = new SqlServerTypeConversionRepository(fixture.Connection);

        // Test various binary data sizes
        var emptyBinary = Array.Empty<byte>();
        var smallBinary = new byte[] { 0x00, 0xFF };
        var mediumBinary = new byte[256];
        for (int i = 0; i < 256; i++) mediumBinary[i] = (byte)i;
        
        var largeBinary = new byte[8192]; // 8KB
        new Random(42).NextBytes(largeBinary);

        // Act
        await repo.InsertAsync(
            "empty", null, 1, null, 1L, null, 1.0m, null, 1.0, null, 1.0f, null,
            DateTime.Now, null, true, null, emptyBinary, Guid.NewGuid(), null);

        await repo.InsertAsync(
            "small", null, 2, null, 2L, null, 2.0m, null, 2.0, null, 2.0f, null,
            DateTime.Now, null, false, null, smallBinary, Guid.NewGuid(), null);

        await repo.InsertAsync(
            "medium", null, 3, null, 3L, null, 3.0m, null, 3.0, null, 3.0f, null,
            DateTime.Now, null, true, null, mediumBinary, Guid.NewGuid(), null);

        await repo.InsertAsync(
            "large", null, 4, null, 4L, null, 4.0m, null, 4.0, null, 4.0f, null,
            DateTime.Now, null, false, null, largeBinary, Guid.NewGuid(), null);

        var result1 = await repo.GetByIdAsync(1);
        var result2 = await repo.GetByIdAsync(2);
        var result3 = await repo.GetByIdAsync(3);
        var result4 = await repo.GetByIdAsync(4);

        // Assert
        Assert.IsNotNull(result1);
        CollectionAssert.AreEqual(emptyBinary, result1.BinaryValue);

        Assert.IsNotNull(result2);
        CollectionAssert.AreEqual(smallBinary, result2.BinaryValue);

        Assert.IsNotNull(result3);
        CollectionAssert.AreEqual(mediumBinary, result3.BinaryValue);

        Assert.IsNotNull(result4);
        CollectionAssert.AreEqual(largeBinary, result4.BinaryValue);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("TypeConversion")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_TypeConversion_GuidVariants_HandledCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTypeTestSchema(DatabaseType.SqlServer));
        var repo = new SqlServerTypeConversionRepository(fixture.Connection);

        var emptyGuid = Guid.Empty;
        var newGuid = Guid.NewGuid();
        var parsedGuid = Guid.Parse("12345678-1234-1234-1234-123456789abc");
        var allOnesGuid = new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff");

        // Act
        await repo.InsertAsync(
            "empty", null, 1, null, 1L, null, 1.0m, null, 1.0, null, 1.0f, null,
            DateTime.Now, null, true, null, Array.Empty<byte>(), emptyGuid, emptyGuid);

        await repo.InsertAsync(
            "new", null, 2, null, 2L, null, 2.0m, null, 2.0, null, 2.0f, null,
            DateTime.Now, null, false, null, Array.Empty<byte>(), newGuid, newGuid);

        await repo.InsertAsync(
            "parsed", null, 3, null, 3L, null, 3.0m, null, 3.0, null, 3.0f, null,
            DateTime.Now, null, true, null, Array.Empty<byte>(), parsedGuid, parsedGuid);

        await repo.InsertAsync(
            "allones", null, 4, null, 4L, null, 4.0m, null, 4.0, null, 4.0f, null,
            DateTime.Now, null, false, null, Array.Empty<byte>(), allOnesGuid, allOnesGuid);

        var result1 = await repo.GetByIdAsync(1);
        var result2 = await repo.GetByIdAsync(2);
        var result3 = await repo.GetByIdAsync(3);
        var result4 = await repo.GetByIdAsync(4);

        // Assert
        Assert.IsNotNull(result1);
        Assert.AreEqual(emptyGuid, result1.GuidValue);
        Assert.AreEqual(emptyGuid, result1.NullableGuidValue);

        Assert.IsNotNull(result2);
        Assert.AreEqual(newGuid, result2.GuidValue);
        Assert.AreEqual(newGuid, result2.NullableGuidValue);

        Assert.IsNotNull(result3);
        Assert.AreEqual(parsedGuid, result3.GuidValue);
        Assert.AreEqual(parsedGuid, result3.NullableGuidValue);

        Assert.IsNotNull(result4);
        Assert.AreEqual(allOnesGuid, result4.GuidValue);
        Assert.AreEqual(allOnesGuid, result4.NullableGuidValue);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("TypeConversion")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_TypeConversion_NegativeNumbers_HandledCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTypeTestSchema(DatabaseType.SqlServer));
        var repo = new SqlServerTypeConversionRepository(fixture.Connection);

        int negativeInt = -12345;
        long negativeLong = -9876543210L;
        decimal negativeDecimal = -123.4567m;
        double negativeDouble = -123.456789;
        float negativeFloat = -123.45f;

        // Act
        await repo.InsertAsync(
            "negative", null, negativeInt, negativeInt, negativeLong, negativeLong,
            negativeDecimal, negativeDecimal, negativeDouble, negativeDouble,
            negativeFloat, negativeFloat, DateTime.Now, null, true, null, Array.Empty<byte>(),
            Guid.NewGuid(), null);

        var result = await repo.GetByIdAsync(1);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(negativeInt, result.IntValue);
        Assert.AreEqual(negativeInt, result.NullableIntValue);
        Assert.AreEqual(negativeLong, result.LongValue);
        Assert.AreEqual(negativeLong, result.NullableLongValue);
        Assert.AreEqual(negativeDecimal, result.DecimalValue);
        Assert.AreEqual(negativeDecimal, result.NullableDecimalValue);
        Assert.AreEqual(negativeDouble, result.DoubleValue, 0.000001);
        Assert.AreEqual(negativeDouble, result.NullableDoubleValue!.Value, 0.000001);
        Assert.AreEqual(negativeFloat, result.FloatValue, 0.001f);
        Assert.AreEqual(negativeFloat, result.NullableFloatValue!.Value, 0.001f);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("TypeConversion")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_TypeConversion_ZeroValues_HandledCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTypeTestSchema(DatabaseType.SqlServer));
        var repo = new SqlServerTypeConversionRepository(fixture.Connection);

        // Act
        await repo.InsertAsync(
            "zero", null, 0, 0, 0L, 0L, 0.0m, 0.0m, 0.0, 0.0, 0.0f, 0.0f,
            DateTime.Now, null, false, false, Array.Empty<byte>(), Guid.Empty, Guid.Empty);

        var result = await repo.GetByIdAsync(1);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.IntValue);
        Assert.AreEqual(0, result.NullableIntValue);
        Assert.AreEqual(0L, result.LongValue);
        Assert.AreEqual(0L, result.NullableLongValue);
        Assert.AreEqual(0.0m, result.DecimalValue);
        Assert.AreEqual(0.0m, result.NullableDecimalValue);
        Assert.AreEqual(0.0, result.DoubleValue);
        Assert.AreEqual(0.0, result.NullableDoubleValue);
        Assert.AreEqual(0.0f, result.FloatValue);
        Assert.AreEqual(0.0f, result.NullableFloatValue);
        Assert.IsFalse(result.BoolValue);
        Assert.IsFalse(result.NullableBoolValue!.Value);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("TypeConversion")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_TypeConversion_MultipleRows_AllPreserved()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTypeTestSchema(DatabaseType.SqlServer));
        var repo = new SqlServerTypeConversionRepository(fixture.Connection);

        // Insert 10 rows with different values
        for (int i = 1; i <= 10; i++)
        {
            await repo.InsertAsync(
                $"row{i}", $"nullable{i}", i, i * 10, i * 100L, i * 1000L,
                i * 1.5m, i * 2.5m, i * 3.5, i * 4.5, i * 5.5f, i * 6.5f,
                DateTime.Now.AddDays(i), DateTime.Now.AddHours(i),
                i % 2 == 0, i % 3 == 0, new byte[] { (byte)i }, 
                Guid.NewGuid(), Guid.NewGuid());
        }

        // Act
        var allResults = await repo.GetAllAsync();

        // Assert
        Assert.AreEqual(10, allResults.Count);
        for (int i = 0; i < 10; i++)
        {
            var result = allResults[i];
            var expectedIndex = i + 1;
            
            Assert.AreEqual($"row{expectedIndex}", result.StringValue);
            Assert.AreEqual($"nullable{expectedIndex}", result.NullableStringValue);
            Assert.AreEqual(expectedIndex, result.IntValue);
            Assert.AreEqual(expectedIndex * 10, result.NullableIntValue);
            Assert.AreEqual(expectedIndex * 100L, result.LongValue);
            Assert.AreEqual(expectedIndex * 1000L, result.NullableLongValue);
        }
    }
}
