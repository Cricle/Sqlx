// <copyright file="NumericBoundaryE2ETests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Tests.E2E.Infrastructure;
using System.Data.Common;

namespace Sqlx.Tests.E2E.Numeric;

/// <summary>
/// E2E tests for numeric boundary value handling in Sqlx across all supported databases.
/// Tests focus on Sqlx's parameter binding and type conversion with boundary numeric values.
/// </summary>
[TestClass]
public class NumericBoundaryE2ETests : E2ETestBase
{
    private static string GetTestSchema(DatabaseType dbType)
    {
        return dbType switch
        {
            DatabaseType.MySQL => @"
                CREATE TABLE numeric_test (
                    id BIGINT AUTO_INCREMENT PRIMARY KEY,
                    int_value INT,
                    bigint_value BIGINT,
                    decimal_value DECIMAL(28, 10),
                    double_value DOUBLE,
                    float_value FLOAT,
                    description VARCHAR(200)
                )",
            DatabaseType.PostgreSQL => @"
                CREATE TABLE numeric_test (
                    id BIGSERIAL PRIMARY KEY,
                    int_value INTEGER,
                    bigint_value BIGINT,
                    decimal_value NUMERIC(28, 10),
                    double_value DOUBLE PRECISION,
                    float_value REAL,
                    description VARCHAR(200)
                )",
            DatabaseType.SqlServer => @"
                CREATE TABLE numeric_test (
                    id BIGINT IDENTITY(1,1) PRIMARY KEY,
                    int_value INT,
                    bigint_value BIGINT,
                    decimal_value DECIMAL(28, 10),
                    double_value FLOAT(53),
                    float_value REAL,
                    description NVARCHAR(200)
                )",
            DatabaseType.SQLite => @"
                CREATE TABLE numeric_test (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    int_value INTEGER,
                    bigint_value INTEGER,
                    decimal_value REAL,
                    double_value REAL,
                    float_value REAL,
                    description TEXT
                )",
            _ => throw new NotSupportedException($"Database type {dbType} is not supported"),
        };
    }

    private static void AddParameter(DbCommand cmd, string name, object? value)
    {
        var param = cmd.CreateParameter();
        param.ParameterName = name;
        param.Value = value ?? DBNull.Value;
        cmd.Parameters.Add(param);
    }

    // ==================== INT Boundary Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Numeric")]
    [TestCategory("MySQL")]
    public async Task MySQL_Sqlx_IntMinValue_PreservesExactValue()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));
        var minInt = NumericDataGenerator.GetMinInt();

        // Act - Test Sqlx parameter binding with INT min value
        using var insertCmd = fixture.Connection.CreateCommand();
        insertCmd.CommandText = "INSERT INTO numeric_test (int_value, description) VALUES (@val, @desc)";
        AddParameter(insertCmd, "@val", minInt);
        AddParameter(insertCmd, "@desc", "INT min");
        await insertCmd.ExecuteNonQueryAsync();

        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT int_value FROM numeric_test WHERE description = @desc";
        AddParameter(selectCmd, "@desc", "INT min");
        var result = await selectCmd.ExecuteScalarAsync();

        // Assert - Verify Sqlx correctly bound and retrieved INT min value
        Assert.IsNotNull(result);
        Assert.AreEqual(minInt, Convert.ToInt32(result), "Sqlx should preserve INT min value");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Numeric")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_Sqlx_IntMinValue_PreservesExactValue()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));
        var minInt = NumericDataGenerator.GetMinInt();

        using var insertCmd = fixture.Connection.CreateCommand();
        insertCmd.CommandText = "INSERT INTO numeric_test (int_value, description) VALUES (@val, @desc)";
        AddParameter(insertCmd, "@val", minInt);
        AddParameter(insertCmd, "@desc", "INT min");
        await insertCmd.ExecuteNonQueryAsync();

        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT int_value FROM numeric_test WHERE description = @desc";
        AddParameter(selectCmd, "@desc", "INT min");
        var result = await selectCmd.ExecuteScalarAsync();

        Assert.IsNotNull(result);
        Assert.AreEqual(minInt, Convert.ToInt32(result), "Sqlx should preserve INT min value");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Numeric")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_Sqlx_IntMinValue_PreservesExactValue()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));
        var minInt = NumericDataGenerator.GetMinInt();

        using var insertCmd = fixture.Connection.CreateCommand();
        insertCmd.CommandText = "INSERT INTO numeric_test (int_value, description) VALUES (@val, @desc)";
        AddParameter(insertCmd, "@val", minInt);
        AddParameter(insertCmd, "@desc", "INT min");
        await insertCmd.ExecuteNonQueryAsync();

        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT int_value FROM numeric_test WHERE description = @desc";
        AddParameter(selectCmd, "@desc", "INT min");
        var result = await selectCmd.ExecuteScalarAsync();

        Assert.IsNotNull(result);
        Assert.AreEqual(minInt, Convert.ToInt32(result), "Sqlx should preserve INT min value");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Numeric")]
    [TestCategory("SQLite")]
    public async Task SQLite_Sqlx_IntMinValue_PreservesExactValue()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));
        var minInt = NumericDataGenerator.GetMinInt();

        using var insertCmd = fixture.Connection.CreateCommand();
        insertCmd.CommandText = "INSERT INTO numeric_test (int_value, description) VALUES (@val, @desc)";
        AddParameter(insertCmd, "@val", minInt);
        AddParameter(insertCmd, "@desc", "INT min");
        await insertCmd.ExecuteNonQueryAsync();

        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT int_value FROM numeric_test WHERE description = @desc";
        AddParameter(selectCmd, "@desc", "INT min");
        var result = await selectCmd.ExecuteScalarAsync();

        Assert.IsNotNull(result);
        Assert.AreEqual(minInt, Convert.ToInt32(result), "Sqlx should preserve INT min value");
    }

    // ==================== INT Max Value Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Numeric")]
    [TestCategory("MySQL")]
    public async Task MySQL_Sqlx_IntMaxValue_PreservesExactValue()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));
        var maxInt = NumericDataGenerator.GetMaxInt();

        using var insertCmd = fixture.Connection.CreateCommand();
        insertCmd.CommandText = "INSERT INTO numeric_test (int_value, description) VALUES (@val, @desc)";
        AddParameter(insertCmd, "@val", maxInt);
        AddParameter(insertCmd, "@desc", "INT max");
        await insertCmd.ExecuteNonQueryAsync();

        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT int_value FROM numeric_test WHERE description = @desc";
        AddParameter(selectCmd, "@desc", "INT max");
        var result = await selectCmd.ExecuteScalarAsync();

        Assert.IsNotNull(result);
        Assert.AreEqual(maxInt, Convert.ToInt32(result), "Sqlx should preserve INT max value");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Numeric")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_Sqlx_IntMaxValue_PreservesExactValue()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));
        var maxInt = NumericDataGenerator.GetMaxInt();

        using var insertCmd = fixture.Connection.CreateCommand();
        insertCmd.CommandText = "INSERT INTO numeric_test (int_value, description) VALUES (@val, @desc)";
        AddParameter(insertCmd, "@val", maxInt);
        AddParameter(insertCmd, "@desc", "INT max");
        await insertCmd.ExecuteNonQueryAsync();

        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT int_value FROM numeric_test WHERE description = @desc";
        AddParameter(selectCmd, "@desc", "INT max");
        var result = await selectCmd.ExecuteScalarAsync();

        Assert.IsNotNull(result);
        Assert.AreEqual(maxInt, Convert.ToInt32(result), "Sqlx should preserve INT max value");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Numeric")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_Sqlx_IntMaxValue_PreservesExactValue()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));
        var maxInt = NumericDataGenerator.GetMaxInt();

        using var insertCmd = fixture.Connection.CreateCommand();
        insertCmd.CommandText = "INSERT INTO numeric_test (int_value, description) VALUES (@val, @desc)";
        AddParameter(insertCmd, "@val", maxInt);
        AddParameter(insertCmd, "@desc", "INT max");
        await insertCmd.ExecuteNonQueryAsync();

        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT int_value FROM numeric_test WHERE description = @desc";
        AddParameter(selectCmd, "@desc", "INT max");
        var result = await selectCmd.ExecuteScalarAsync();

        Assert.IsNotNull(result);
        Assert.AreEqual(maxInt, Convert.ToInt32(result), "Sqlx should preserve INT max value");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Numeric")]
    [TestCategory("SQLite")]
    public async Task SQLite_Sqlx_IntMaxValue_PreservesExactValue()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));
        var maxInt = NumericDataGenerator.GetMaxInt();

        using var insertCmd = fixture.Connection.CreateCommand();
        insertCmd.CommandText = "INSERT INTO numeric_test (int_value, description) VALUES (@val, @desc)";
        AddParameter(insertCmd, "@val", maxInt);
        AddParameter(insertCmd, "@desc", "INT max");
        await insertCmd.ExecuteNonQueryAsync();

        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT int_value FROM numeric_test WHERE description = @desc";
        AddParameter(selectCmd, "@desc", "INT max");
        var result = await selectCmd.ExecuteScalarAsync();

        Assert.IsNotNull(result);
        Assert.AreEqual(maxInt, Convert.ToInt32(result), "Sqlx should preserve INT max value");
    }

    // ==================== BIGINT Boundary Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Numeric")]
    [TestCategory("MySQL")]
    public async Task MySQL_Sqlx_BigIntMinValue_PreservesExactValue()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));
        var minBigInt = NumericDataGenerator.GetMinBigInt();

        using var insertCmd = fixture.Connection.CreateCommand();
        insertCmd.CommandText = "INSERT INTO numeric_test (bigint_value, description) VALUES (@val, @desc)";
        AddParameter(insertCmd, "@val", minBigInt);
        AddParameter(insertCmd, "@desc", "BIGINT min");
        await insertCmd.ExecuteNonQueryAsync();

        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT bigint_value FROM numeric_test WHERE description = @desc";
        AddParameter(selectCmd, "@desc", "BIGINT min");
        var result = await selectCmd.ExecuteScalarAsync();

        Assert.IsNotNull(result);
        Assert.AreEqual(minBigInt, Convert.ToInt64(result), "Sqlx should preserve BIGINT min value");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Numeric")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_Sqlx_BigIntMinValue_PreservesExactValue()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));
        var minBigInt = NumericDataGenerator.GetMinBigInt();

        using var insertCmd = fixture.Connection.CreateCommand();
        insertCmd.CommandText = "INSERT INTO numeric_test (bigint_value, description) VALUES (@val, @desc)";
        AddParameter(insertCmd, "@val", minBigInt);
        AddParameter(insertCmd, "@desc", "BIGINT min");
        await insertCmd.ExecuteNonQueryAsync();

        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT bigint_value FROM numeric_test WHERE description = @desc";
        AddParameter(selectCmd, "@desc", "BIGINT min");
        var result = await selectCmd.ExecuteScalarAsync();

        Assert.IsNotNull(result);
        Assert.AreEqual(minBigInt, Convert.ToInt64(result), "Sqlx should preserve BIGINT min value");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Numeric")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_Sqlx_BigIntMinValue_PreservesExactValue()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));
        var minBigInt = NumericDataGenerator.GetMinBigInt();

        using var insertCmd = fixture.Connection.CreateCommand();
        insertCmd.CommandText = "INSERT INTO numeric_test (bigint_value, description) VALUES (@val, @desc)";
        AddParameter(insertCmd, "@val", minBigInt);
        AddParameter(insertCmd, "@desc", "BIGINT min");
        await insertCmd.ExecuteNonQueryAsync();

        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT bigint_value FROM numeric_test WHERE description = @desc";
        AddParameter(selectCmd, "@desc", "BIGINT min");
        var result = await selectCmd.ExecuteScalarAsync();

        Assert.IsNotNull(result);
        Assert.AreEqual(minBigInt, Convert.ToInt64(result), "Sqlx should preserve BIGINT min value");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Numeric")]
    [TestCategory("SQLite")]
    public async Task SQLite_Sqlx_BigIntMinValue_PreservesExactValue()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));
        var minBigInt = NumericDataGenerator.GetMinBigInt();

        using var insertCmd = fixture.Connection.CreateCommand();
        insertCmd.CommandText = "INSERT INTO numeric_test (bigint_value, description) VALUES (@val, @desc)";
        AddParameter(insertCmd, "@val", minBigInt);
        AddParameter(insertCmd, "@desc", "BIGINT min");
        await insertCmd.ExecuteNonQueryAsync();

        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT bigint_value FROM numeric_test WHERE description = @desc";
        AddParameter(selectCmd, "@desc", "BIGINT min");
        var result = await selectCmd.ExecuteScalarAsync();

        Assert.IsNotNull(result);
        Assert.AreEqual(minBigInt, Convert.ToInt64(result), "Sqlx should preserve BIGINT min value");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Numeric")]
    [TestCategory("MySQL")]
    public async Task MySQL_Sqlx_BigIntMaxValue_PreservesExactValue()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));
        var maxBigInt = NumericDataGenerator.GetMaxBigInt();

        using var insertCmd = fixture.Connection.CreateCommand();
        insertCmd.CommandText = "INSERT INTO numeric_test (bigint_value, description) VALUES (@val, @desc)";
        AddParameter(insertCmd, "@val", maxBigInt);
        AddParameter(insertCmd, "@desc", "BIGINT max");
        await insertCmd.ExecuteNonQueryAsync();

        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT bigint_value FROM numeric_test WHERE description = @desc";
        AddParameter(selectCmd, "@desc", "BIGINT max");
        var result = await selectCmd.ExecuteScalarAsync();

        Assert.IsNotNull(result);
        Assert.AreEqual(maxBigInt, Convert.ToInt64(result), "Sqlx should preserve BIGINT max value");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Numeric")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_Sqlx_BigIntMaxValue_PreservesExactValue()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));
        var maxBigInt = NumericDataGenerator.GetMaxBigInt();

        using var insertCmd = fixture.Connection.CreateCommand();
        insertCmd.CommandText = "INSERT INTO numeric_test (bigint_value, description) VALUES (@val, @desc)";
        AddParameter(insertCmd, "@val", maxBigInt);
        AddParameter(insertCmd, "@desc", "BIGINT max");
        await insertCmd.ExecuteNonQueryAsync();

        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT bigint_value FROM numeric_test WHERE description = @desc";
        AddParameter(selectCmd, "@desc", "BIGINT max");
        var result = await selectCmd.ExecuteScalarAsync();

        Assert.IsNotNull(result);
        Assert.AreEqual(maxBigInt, Convert.ToInt64(result), "Sqlx should preserve BIGINT max value");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Numeric")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_Sqlx_BigIntMaxValue_PreservesExactValue()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));
        var maxBigInt = NumericDataGenerator.GetMaxBigInt();

        using var insertCmd = fixture.Connection.CreateCommand();
        insertCmd.CommandText = "INSERT INTO numeric_test (bigint_value, description) VALUES (@val, @desc)";
        AddParameter(insertCmd, "@val", maxBigInt);
        AddParameter(insertCmd, "@desc", "BIGINT max");
        await insertCmd.ExecuteNonQueryAsync();

        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT bigint_value FROM numeric_test WHERE description = @desc";
        AddParameter(selectCmd, "@desc", "BIGINT max");
        var result = await selectCmd.ExecuteScalarAsync();

        Assert.IsNotNull(result);
        Assert.AreEqual(maxBigInt, Convert.ToInt64(result), "Sqlx should preserve BIGINT max value");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Numeric")]
    [TestCategory("SQLite")]
    public async Task SQLite_Sqlx_BigIntMaxValue_PreservesExactValue()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));
        var maxBigInt = NumericDataGenerator.GetMaxBigInt();

        using var insertCmd = fixture.Connection.CreateCommand();
        insertCmd.CommandText = "INSERT INTO numeric_test (bigint_value, description) VALUES (@val, @desc)";
        AddParameter(insertCmd, "@val", maxBigInt);
        AddParameter(insertCmd, "@desc", "BIGINT max");
        await insertCmd.ExecuteNonQueryAsync();

        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT bigint_value FROM numeric_test WHERE description = @desc";
        AddParameter(selectCmd, "@desc", "BIGINT max");
        var result = await selectCmd.ExecuteScalarAsync();

        Assert.IsNotNull(result);
        Assert.AreEqual(maxBigInt, Convert.ToInt64(result), "Sqlx should preserve BIGINT max value");
    }

    // ==================== Zero and Negative Value Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Numeric")]
    [TestCategory("MySQL")]
    public async Task MySQL_Sqlx_ZeroAndNegativeValues_PreserveCorrectly()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        // Test zero
        using var insertZeroCmd = fixture.Connection.CreateCommand();
        insertZeroCmd.CommandText = "INSERT INTO numeric_test (int_value, bigint_value, decimal_value, description) VALUES (@int, @bigint, @dec, @desc)";
        AddParameter(insertZeroCmd, "@int", NumericDataGenerator.GetZeroInt());
        AddParameter(insertZeroCmd, "@bigint", NumericDataGenerator.GetZeroBigInt());
        AddParameter(insertZeroCmd, "@dec", NumericDataGenerator.GetZeroDecimal());
        AddParameter(insertZeroCmd, "@desc", "Zero values");
        await insertZeroCmd.ExecuteNonQueryAsync();

        // Test negative
        using var insertNegCmd = fixture.Connection.CreateCommand();
        insertNegCmd.CommandText = "INSERT INTO numeric_test (int_value, bigint_value, decimal_value, description) VALUES (@int, @bigint, @dec, @desc)";
        AddParameter(insertNegCmd, "@int", NumericDataGenerator.GetNegativeInt());
        AddParameter(insertNegCmd, "@bigint", NumericDataGenerator.GetNegativeBigInt());
        AddParameter(insertNegCmd, "@dec", NumericDataGenerator.GetNegativeDecimal());
        AddParameter(insertNegCmd, "@desc", "Negative values");
        await insertNegCmd.ExecuteNonQueryAsync();

        // Verify zero
        using (var selectZeroCmd = fixture.Connection.CreateCommand())
        {
            selectZeroCmd.CommandText = "SELECT int_value, bigint_value, decimal_value FROM numeric_test WHERE description = @desc";
            AddParameter(selectZeroCmd, "@desc", "Zero values");
            using var zeroReader = await selectZeroCmd.ExecuteReaderAsync();
            Assert.IsTrue(await zeroReader.ReadAsync());
            Assert.AreEqual(0, zeroReader.GetInt32(0));
            Assert.AreEqual(0L, zeroReader.GetInt64(1));
            Assert.AreEqual(0m, zeroReader.GetDecimal(2));
        }

        // Verify negative
        using (var selectNegCmd = fixture.Connection.CreateCommand())
        {
            selectNegCmd.CommandText = "SELECT int_value, bigint_value, decimal_value FROM numeric_test WHERE description = @desc";
            AddParameter(selectNegCmd, "@desc", "Negative values");
            using var negReader = await selectNegCmd.ExecuteReaderAsync();
            Assert.IsTrue(await negReader.ReadAsync());
            Assert.AreEqual(NumericDataGenerator.GetNegativeInt(), negReader.GetInt32(0));
            Assert.AreEqual(NumericDataGenerator.GetNegativeBigInt(), negReader.GetInt64(1));
            Assert.AreEqual(NumericDataGenerator.GetNegativeDecimal(), negReader.GetDecimal(2));
        }
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Numeric")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_Sqlx_ZeroAndNegativeValues_PreserveCorrectly()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        using var insertZeroCmd = fixture.Connection.CreateCommand();
        insertZeroCmd.CommandText = "INSERT INTO numeric_test (int_value, bigint_value, decimal_value, description) VALUES (@int, @bigint, @dec, @desc)";
        AddParameter(insertZeroCmd, "@int", NumericDataGenerator.GetZeroInt());
        AddParameter(insertZeroCmd, "@bigint", NumericDataGenerator.GetZeroBigInt());
        AddParameter(insertZeroCmd, "@dec", NumericDataGenerator.GetZeroDecimal());
        AddParameter(insertZeroCmd, "@desc", "Zero values");
        await insertZeroCmd.ExecuteNonQueryAsync();

        using var insertNegCmd = fixture.Connection.CreateCommand();
        insertNegCmd.CommandText = "INSERT INTO numeric_test (int_value, bigint_value, decimal_value, description) VALUES (@int, @bigint, @dec, @desc)";
        AddParameter(insertNegCmd, "@int", NumericDataGenerator.GetNegativeInt());
        AddParameter(insertNegCmd, "@bigint", NumericDataGenerator.GetNegativeBigInt());
        AddParameter(insertNegCmd, "@dec", NumericDataGenerator.GetNegativeDecimal());
        AddParameter(insertNegCmd, "@desc", "Negative values");
        await insertNegCmd.ExecuteNonQueryAsync();

        using (var selectZeroCmd = fixture.Connection.CreateCommand())
        {
            selectZeroCmd.CommandText = "SELECT int_value, bigint_value, decimal_value FROM numeric_test WHERE description = @desc";
            AddParameter(selectZeroCmd, "@desc", "Zero values");
            using var zeroReader = await selectZeroCmd.ExecuteReaderAsync();
            Assert.IsTrue(await zeroReader.ReadAsync());
            Assert.AreEqual(0, zeroReader.GetInt32(0));
            Assert.AreEqual(0L, zeroReader.GetInt64(1));
            Assert.AreEqual(0m, zeroReader.GetDecimal(2));
        }

        using (var selectNegCmd = fixture.Connection.CreateCommand())
        {
            selectNegCmd.CommandText = "SELECT int_value, bigint_value, decimal_value FROM numeric_test WHERE description = @desc";
            AddParameter(selectNegCmd, "@desc", "Negative values");
            using var negReader = await selectNegCmd.ExecuteReaderAsync();
            Assert.IsTrue(await negReader.ReadAsync());
            Assert.AreEqual(NumericDataGenerator.GetNegativeInt(), negReader.GetInt32(0));
            Assert.AreEqual(NumericDataGenerator.GetNegativeBigInt(), negReader.GetInt64(1));
            Assert.AreEqual(NumericDataGenerator.GetNegativeDecimal(), negReader.GetDecimal(2));
        }
    }

    // ==================== Decimal Precision Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Numeric")]
    [TestCategory("MySQL")]
    public async Task MySQL_Sqlx_HighPrecisionDecimal_PreservesCorrectly()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));
        var highPrecisionDecimal = NumericDataGenerator.GetHighPrecisionDecimal();

        using var insertCmd = fixture.Connection.CreateCommand();
        insertCmd.CommandText = "INSERT INTO numeric_test (decimal_value, description) VALUES (@val, @desc)";
        AddParameter(insertCmd, "@val", highPrecisionDecimal);
        AddParameter(insertCmd, "@desc", "High precision decimal");
        await insertCmd.ExecuteNonQueryAsync();

        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT decimal_value FROM numeric_test WHERE description = @desc";
        AddParameter(selectCmd, "@desc", "High precision decimal");
        var result = await selectCmd.ExecuteScalarAsync();

        Assert.IsNotNull(result);
        Assert.AreEqual(highPrecisionDecimal, Convert.ToDecimal(result), "Sqlx should preserve high precision decimal");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Numeric")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_Sqlx_HighPrecisionDecimal_PreservesCorrectly()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));
        var highPrecisionDecimal = NumericDataGenerator.GetHighPrecisionDecimal();

        using var insertCmd = fixture.Connection.CreateCommand();
        insertCmd.CommandText = "INSERT INTO numeric_test (decimal_value, description) VALUES (@val, @desc)";
        AddParameter(insertCmd, "@val", highPrecisionDecimal);
        AddParameter(insertCmd, "@desc", "High precision decimal");
        await insertCmd.ExecuteNonQueryAsync();

        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT decimal_value FROM numeric_test WHERE description = @desc";
        AddParameter(selectCmd, "@desc", "High precision decimal");
        var result = await selectCmd.ExecuteScalarAsync();

        Assert.IsNotNull(result);
        Assert.AreEqual(highPrecisionDecimal, Convert.ToDecimal(result), "Sqlx should preserve high precision decimal");
    }

    // ==================== Float/Double Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Numeric")]
    [TestCategory("MySQL")]
    public async Task MySQL_Sqlx_FloatPrecision_RoundTrip()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        var floatValue = 3.14159f;

        using var insertCmd = fixture.Connection.CreateCommand();
        insertCmd.CommandText = "INSERT INTO numeric_test (float_value, description) VALUES (@val, @desc)";
        AddParameter(insertCmd, "@val", floatValue);
        AddParameter(insertCmd, "@desc", "Float precision");
        await insertCmd.ExecuteNonQueryAsync();

        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT float_value FROM numeric_test WHERE description = @desc";
        AddParameter(selectCmd, "@desc", "Float precision");
        using var reader = await selectCmd.ExecuteReaderAsync();
        Assert.IsTrue(await reader.ReadAsync());
        var result = reader.GetFloat(0);

        // Float precision may vary slightly
        Assert.AreEqual(floatValue, result, 0.0001f);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Numeric")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_Sqlx_FloatPrecision_RoundTrip()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        var floatValue = 3.14159f;

        using var insertCmd = fixture.Connection.CreateCommand();
        insertCmd.CommandText = "INSERT INTO numeric_test (float_value, description) VALUES (@val, @desc)";
        AddParameter(insertCmd, "@val", floatValue);
        AddParameter(insertCmd, "@desc", "Float precision");
        await insertCmd.ExecuteNonQueryAsync();

        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT float_value FROM numeric_test WHERE description = @desc";
        AddParameter(selectCmd, "@desc", "Float precision");
        using var reader = await selectCmd.ExecuteReaderAsync();
        Assert.IsTrue(await reader.ReadAsync());
        var result = reader.GetFloat(0);

        Assert.AreEqual(floatValue, result, 0.0001f);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Numeric")]
    [TestCategory("MySQL")]
    public async Task MySQL_Sqlx_DoublePrecision_RoundTrip()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        var doubleValue = 3.141592653589793;

        using var insertCmd = fixture.Connection.CreateCommand();
        insertCmd.CommandText = "INSERT INTO numeric_test (double_value, description) VALUES (@val, @desc)";
        AddParameter(insertCmd, "@val", doubleValue);
        AddParameter(insertCmd, "@desc", "Double precision");
        await insertCmd.ExecuteNonQueryAsync();

        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT double_value FROM numeric_test WHERE description = @desc";
        AddParameter(selectCmd, "@desc", "Double precision");
        using var reader = await selectCmd.ExecuteReaderAsync();
        Assert.IsTrue(await reader.ReadAsync());
        var result = reader.GetDouble(0);

        // Double precision should be very close
        Assert.AreEqual(doubleValue, result, 0.000000000001);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Numeric")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_Sqlx_DoublePrecision_RoundTrip()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        var doubleValue = 3.141592653589793;

        using var insertCmd = fixture.Connection.CreateCommand();
        insertCmd.CommandText = "INSERT INTO numeric_test (double_value, description) VALUES (@val, @desc)";
        AddParameter(insertCmd, "@val", doubleValue);
        AddParameter(insertCmd, "@desc", "Double precision");
        await insertCmd.ExecuteNonQueryAsync();

        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT double_value FROM numeric_test WHERE description = @desc";
        AddParameter(selectCmd, "@desc", "Double precision");
        using var reader = await selectCmd.ExecuteReaderAsync();
        Assert.IsTrue(await reader.ReadAsync());
        var result = reader.GetDouble(0);

        Assert.AreEqual(doubleValue, result, 0.000000000001);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Numeric")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_Sqlx_DoublePrecision_RoundTrip()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));

        var doubleValue = 3.141592653589793;

        using var insertCmd = fixture.Connection.CreateCommand();
        insertCmd.CommandText = "INSERT INTO numeric_test (double_value, description) VALUES (@val, @desc)";
        AddParameter(insertCmd, "@val", doubleValue);
        AddParameter(insertCmd, "@desc", "Double precision");
        await insertCmd.ExecuteNonQueryAsync();

        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT double_value FROM numeric_test WHERE description = @desc";
        AddParameter(selectCmd, "@desc", "Double precision");
        using var reader = await selectCmd.ExecuteReaderAsync();
        Assert.IsTrue(await reader.ReadAsync());
        var result = reader.GetDouble(0);

        Assert.AreEqual(doubleValue, result, 0.000000000001);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Numeric")]
    [TestCategory("SQLite")]
    public async Task SQLite_Sqlx_DoublePrecision_RoundTrip()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));

        var doubleValue = 3.141592653589793;

        using var insertCmd = fixture.Connection.CreateCommand();
        insertCmd.CommandText = "INSERT INTO numeric_test (double_value, description) VALUES (@val, @desc)";
        AddParameter(insertCmd, "@val", doubleValue);
        AddParameter(insertCmd, "@desc", "Double precision");
        await insertCmd.ExecuteNonQueryAsync();

        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT double_value FROM numeric_test WHERE description = @desc";
        AddParameter(selectCmd, "@desc", "Double precision");
        using var reader = await selectCmd.ExecuteReaderAsync();
        Assert.IsTrue(await reader.ReadAsync());
        var result = reader.GetDouble(0);

        Assert.AreEqual(doubleValue, result, 0.000000000001);
    }
}
