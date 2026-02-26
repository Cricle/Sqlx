// <copyright file="MathFunctionsE2ETests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Tests.E2E.Infrastructure;
using System.Data.Common;

namespace Sqlx.Tests.E2E.MathFunctions;

/// <summary>
/// E2E tests for SQL math functions (ABS, ROUND, CEIL, FLOOR, MOD, POWER).
/// </summary>
[TestClass]
public class MathFunctionsE2ETests : E2ETestBase
{
    private static string GetTestSchema(DatabaseType dbType)
    {
        return dbType switch
        {
            DatabaseType.MySQL => @"
                CREATE TABLE test_numbers (
                    id BIGINT AUTO_INCREMENT PRIMARY KEY,
                    value DECIMAL(10, 4) NOT NULL,
                    description VARCHAR(100)
                )",
            DatabaseType.PostgreSQL => @"
                CREATE TABLE test_numbers (
                    id BIGSERIAL PRIMARY KEY,
                    value DECIMAL(10, 4) NOT NULL,
                    description VARCHAR(100)
                )",
            DatabaseType.SqlServer => @"
                CREATE TABLE test_numbers (
                    id BIGINT IDENTITY(1,1) PRIMARY KEY,
                    value DECIMAL(10, 4) NOT NULL,
                    description NVARCHAR(100)
                )",
            DatabaseType.SQLite => @"
                CREATE TABLE test_numbers (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    value REAL NOT NULL,
                    description TEXT
                )",
            _ => throw new NotSupportedException($"Database type {dbType} is not supported"),
        };
    }

    private static async Task InsertTestDataAsync(DbConnection connection, DatabaseType dbType)
    {
        var numbers = new[] { -15.7m, 23.456m, 100.0m, -8.2m, 45.999m };

        foreach (var num in numbers)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "INSERT INTO test_numbers (value, description) VALUES (@value, @desc)";
            AddParameter(cmd, "@value", num);
            AddParameter(cmd, "@desc", $"Test value {num}");
            await cmd.ExecuteNonQueryAsync();
        }
    }

    private static void AddParameter(DbCommand cmd, string name, object? value)
    {
        var param = cmd.CreateParameter();
        param.ParameterName = name;
        param.Value = value ?? DBNull.Value;
        cmd.Parameters.Add(param);
    }

    // ==================== ABS Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("MathFunctions")]
    [TestCategory("MySQL")]
    public async Task MySQL_Abs_ReturnsAbsoluteValue()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.MySQL);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT ABS(value) FROM test_numbers WHERE value < 0 ORDER BY value";

        var results = new List<decimal>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(reader.GetDecimal(0));
        }

        // Assert
        Assert.AreEqual(2, results.Count);
        Assert.AreEqual(15.7m, results[0]);
        Assert.AreEqual(8.2m, results[1]);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("MathFunctions")]
    [TestCategory("SQLite")]
    public async Task SQLite_Abs_ReturnsAbsoluteValue()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.SQLite);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT ABS(value) FROM test_numbers WHERE value < 0 ORDER BY value";

        var results = new List<double>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(reader.GetDouble(0));
        }

        // Assert
        Assert.AreEqual(2, results.Count);
        Assert.AreEqual(15.7, results[0], 0.01);
        Assert.AreEqual(8.2, results[1], 0.01);
    }

    // ==================== ROUND Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("MathFunctions")]
    [TestCategory("MySQL")]
    public async Task MySQL_Round_RoundsCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.MySQL);

        // Act - Round to 1 decimal place
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT ROUND(value, 1) FROM test_numbers WHERE value = 23.456";
        var result = Convert.ToDecimal(await cmd.ExecuteScalarAsync());

        // Assert
        Assert.AreEqual(23.5m, result);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("MathFunctions")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_Round_RoundsCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.PostgreSQL);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT ROUND(value, 1) FROM test_numbers WHERE value = 23.456";
        var result = Convert.ToDecimal(await cmd.ExecuteScalarAsync());

        // Assert
        Assert.AreEqual(23.5m, result);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("MathFunctions")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_Round_RoundsCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.SqlServer);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT ROUND(value, 1) FROM test_numbers WHERE value = 23.456";
        var result = Convert.ToDecimal(await cmd.ExecuteScalarAsync());

        // Assert
        Assert.AreEqual(23.5m, result);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("MathFunctions")]
    [TestCategory("SQLite")]
    public async Task SQLite_Round_RoundsCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.SQLite);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT ROUND(value, 1) FROM test_numbers WHERE value > 23 AND value < 24";
        var result = Convert.ToDouble(await cmd.ExecuteScalarAsync());

        // Assert
        Assert.AreEqual(23.5, result, 0.01);
    }

    // ==================== CEIL/CEILING Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("MathFunctions")]
    [TestCategory("MySQL")]
    public async Task MySQL_Ceiling_RoundsUp()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.MySQL);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT CEILING(value) FROM test_numbers WHERE value = 23.456";
        var result = Convert.ToDecimal(await cmd.ExecuteScalarAsync());

        // Assert
        Assert.AreEqual(24m, result);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("MathFunctions")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_Ceiling_RoundsUp()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.PostgreSQL);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT CEILING(value) FROM test_numbers WHERE value = 23.456";
        var result = Convert.ToDouble(await cmd.ExecuteScalarAsync());

        // Assert
        Assert.AreEqual(24.0, result, 0.01);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("MathFunctions")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_Ceiling_RoundsUp()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.SqlServer);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT CEILING(value) FROM test_numbers WHERE value = 23.456";
        var result = Convert.ToDecimal(await cmd.ExecuteScalarAsync());

        // Assert
        Assert.AreEqual(24m, result);
    }

    // ==================== FLOOR Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("MathFunctions")]
    [TestCategory("MySQL")]
    public async Task MySQL_Floor_RoundsDown()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.MySQL);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT FLOOR(value) FROM test_numbers WHERE value = 45.999";
        var result = Convert.ToDecimal(await cmd.ExecuteScalarAsync());

        // Assert
        Assert.AreEqual(45m, result);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("MathFunctions")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_Floor_RoundsDown()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.PostgreSQL);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT FLOOR(value) FROM test_numbers WHERE value = 45.999";
        var result = Convert.ToDouble(await cmd.ExecuteScalarAsync());

        // Assert
        Assert.AreEqual(45.0, result, 0.01);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("MathFunctions")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_Floor_RoundsDown()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.SqlServer);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT FLOOR(value) FROM test_numbers WHERE value = 45.999";
        var result = Convert.ToDecimal(await cmd.ExecuteScalarAsync());

        // Assert
        Assert.AreEqual(45m, result);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("MathFunctions")]
    [TestCategory("SQLite")]
    public async Task SQLite_Floor_RoundsDown()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.SQLite);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT CAST(value AS INTEGER) FROM test_numbers WHERE value > 45 AND value < 46";
        var result = Convert.ToInt64(await cmd.ExecuteScalarAsync());

        // Assert
        Assert.AreEqual(45, result);
    }

    // ==================== MOD Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("MathFunctions")]
    [TestCategory("MySQL")]
    public async Task MySQL_Mod_CalculatesRemainder()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.MySQL);

        // Act - 23 MOD 5 = 3
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT MOD(23, 5)";
        var result = Convert.ToInt32(await cmd.ExecuteScalarAsync());

        // Assert
        Assert.AreEqual(3, result);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("MathFunctions")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_Mod_CalculatesRemainder()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.PostgreSQL);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT MOD(23, 5)";
        var result = Convert.ToInt32(await cmd.ExecuteScalarAsync());

        // Assert
        Assert.AreEqual(3, result);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("MathFunctions")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_Mod_CalculatesRemainder()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.SqlServer);

        // Act - SQL Server uses % operator
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT 23 % 5";
        var result = Convert.ToInt32(await cmd.ExecuteScalarAsync());

        // Assert
        Assert.AreEqual(3, result);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("MathFunctions")]
    [TestCategory("SQLite")]
    public async Task SQLite_Mod_CalculatesRemainder()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.SQLite);

        // Act - SQLite uses % operator
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT 23 % 5";
        var result = Convert.ToInt64(await cmd.ExecuteScalarAsync());

        // Assert
        Assert.AreEqual(3, result);
    }

    // ==================== POWER Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("MathFunctions")]
    [TestCategory("MySQL")]
    public async Task MySQL_Power_CalculatesExponent()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.MySQL);

        // Act - 2^3 = 8
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT POWER(2, 3)";
        var result = Convert.ToDouble(await cmd.ExecuteScalarAsync());

        // Assert
        Assert.AreEqual(8.0, result, 0.01);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("MathFunctions")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_Power_CalculatesExponent()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.PostgreSQL);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT POWER(2, 3)";
        var result = Convert.ToDouble(await cmd.ExecuteScalarAsync());

        // Assert
        Assert.AreEqual(8.0, result, 0.01);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("MathFunctions")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_Power_CalculatesExponent()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.SqlServer);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT POWER(2, 3)";
        var result = Convert.ToDouble(await cmd.ExecuteScalarAsync());

        // Assert
        Assert.AreEqual(8.0, result, 0.01);
    }
}
