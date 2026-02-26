// <copyright file="EdgeCasesE2ETests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Tests.E2E.Infrastructure;
using System.Data.Common;

namespace Sqlx.Tests.E2E.EdgeCases;

/// <summary>
/// E2E tests for edge cases and boundary conditions across all supported databases.
/// </summary>
[TestClass]
public class EdgeCasesE2ETests : E2ETestBase
{
    private static string GetTestSchema(DatabaseType dbType)
    {
        return dbType switch
        {
            DatabaseType.MySQL => @"
                CREATE TABLE edge_test (
                    id BIGINT AUTO_INCREMENT PRIMARY KEY,
                    text_col VARCHAR(1000),
                    int_col INT,
                    decimal_col DECIMAL(18, 6),
                    date_col DATETIME
                )",
            DatabaseType.PostgreSQL => @"
                CREATE TABLE edge_test (
                    id BIGSERIAL PRIMARY KEY,
                    text_col VARCHAR(1000),
                    int_col INT,
                    decimal_col DECIMAL(18, 6),
                    date_col TIMESTAMP
                )",
            DatabaseType.SqlServer => @"
                CREATE TABLE edge_test (
                    id BIGINT IDENTITY(1,1) PRIMARY KEY,
                    text_col NVARCHAR(1000),
                    int_col INT,
                    decimal_col DECIMAL(18, 6),
                    date_col DATETIME2
                )",
            DatabaseType.SQLite => @"
                CREATE TABLE edge_test (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    text_col TEXT,
                    int_col INTEGER,
                    decimal_col REAL,
                    date_col TEXT
                )",
            _ => throw new NotSupportedException($"Database type {dbType} is not supported"),
        };
    }

    // ==================== Empty String Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("EdgeCases")]
    [TestCategory("MySQL")]
    public async Task MySQL_EmptyString_StoredAndRetrievedCorrectly()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        // Insert empty string
        using var insertCmd = fixture.Connection.CreateCommand();
        insertCmd.CommandText = "INSERT INTO edge_test (text_col, int_col) VALUES (@text, @int)";
        insertCmd.Parameters.Add(new MySqlConnector.MySqlParameter("@text", string.Empty));
        insertCmd.Parameters.Add(new MySqlConnector.MySqlParameter("@int", 42));
        await insertCmd.ExecuteNonQueryAsync();

        // Retrieve and verify
        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT text_col FROM edge_test WHERE int_col = 42";
        var result = await selectCmd.ExecuteScalarAsync();
        
        Assert.IsNotNull(result);
        Assert.AreEqual(string.Empty, result.ToString());
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("EdgeCases")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_EmptyString_StoredAndRetrievedCorrectly()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        using var insertCmd = fixture.Connection.CreateCommand();
        insertCmd.CommandText = "INSERT INTO edge_test (text_col, int_col) VALUES (@text, @int)";
        insertCmd.Parameters.Add(new Npgsql.NpgsqlParameter("@text", string.Empty));
        insertCmd.Parameters.Add(new Npgsql.NpgsqlParameter("@int", 42));
        await insertCmd.ExecuteNonQueryAsync();

        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT text_col FROM edge_test WHERE int_col = 42";
        var result = await selectCmd.ExecuteScalarAsync();
        
        Assert.IsNotNull(result);
        Assert.AreEqual(string.Empty, result.ToString());
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("EdgeCases")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_EmptyString_StoredAndRetrievedCorrectly()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));

        using var insertCmd = fixture.Connection.CreateCommand();
        insertCmd.CommandText = "INSERT INTO edge_test (text_col, int_col) VALUES (@text, @int)";
        insertCmd.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@text", string.Empty));
        insertCmd.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@int", 42));
        await insertCmd.ExecuteNonQueryAsync();

        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT text_col FROM edge_test WHERE int_col = 42";
        var result = await selectCmd.ExecuteScalarAsync();
        
        Assert.IsNotNull(result);
        Assert.AreEqual(string.Empty, result.ToString());
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("EdgeCases")]
    [TestCategory("SQLite")]
    public async Task SQLite_EmptyString_StoredAndRetrievedCorrectly()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));

        using var insertCmd = fixture.Connection.CreateCommand();
        insertCmd.CommandText = "INSERT INTO edge_test (text_col, int_col) VALUES (@text, @int)";
        insertCmd.Parameters.Add(new Microsoft.Data.Sqlite.SqliteParameter("@text", string.Empty));
        insertCmd.Parameters.Add(new Microsoft.Data.Sqlite.SqliteParameter("@int", 42));
        await insertCmd.ExecuteNonQueryAsync();

        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT text_col FROM edge_test WHERE int_col = 42";
        var result = await selectCmd.ExecuteScalarAsync();
        
        Assert.IsNotNull(result);
        Assert.AreEqual(string.Empty, result.ToString());
    }

    // ==================== NULL Value Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("EdgeCases")]
    [TestCategory("MySQL")]
    public async Task MySQL_NullValues_HandledCorrectly()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        using var insertCmd = fixture.Connection.CreateCommand();
        insertCmd.CommandText = "INSERT INTO edge_test (text_col, int_col) VALUES (@text, @int)";
        insertCmd.Parameters.Add(new MySqlConnector.MySqlParameter("@text", DBNull.Value));
        insertCmd.Parameters.Add(new MySqlConnector.MySqlParameter("@int", 42));
        await insertCmd.ExecuteNonQueryAsync();

        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT text_col FROM edge_test WHERE int_col = 42";
        var result = await selectCmd.ExecuteScalarAsync();
        
        Assert.IsTrue(result == null || result == DBNull.Value);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("EdgeCases")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_NullValues_HandledCorrectly()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        using var insertCmd = fixture.Connection.CreateCommand();
        insertCmd.CommandText = "INSERT INTO edge_test (text_col, int_col) VALUES (@text, @int)";
        insertCmd.Parameters.Add(new Npgsql.NpgsqlParameter("@text", DBNull.Value));
        insertCmd.Parameters.Add(new Npgsql.NpgsqlParameter("@int", 42));
        await insertCmd.ExecuteNonQueryAsync();

        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT text_col FROM edge_test WHERE int_col = 42";
        var result = await selectCmd.ExecuteScalarAsync();
        
        Assert.IsTrue(result == null || result == DBNull.Value);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("EdgeCases")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_NullValues_HandledCorrectly()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));

        using var insertCmd = fixture.Connection.CreateCommand();
        insertCmd.CommandText = "INSERT INTO edge_test (text_col, int_col) VALUES (@text, @int)";
        insertCmd.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@text", DBNull.Value));
        insertCmd.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@int", 42));
        await insertCmd.ExecuteNonQueryAsync();

        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT text_col FROM edge_test WHERE int_col = 42";
        var result = await selectCmd.ExecuteScalarAsync();
        
        Assert.IsTrue(result == null || result == DBNull.Value);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("EdgeCases")]
    [TestCategory("SQLite")]
    public async Task SQLite_NullValues_HandledCorrectly()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));

        using var insertCmd = fixture.Connection.CreateCommand();
        insertCmd.CommandText = "INSERT INTO edge_test (text_col, int_col) VALUES (@text, @int)";
        insertCmd.Parameters.Add(new Microsoft.Data.Sqlite.SqliteParameter("@text", DBNull.Value));
        insertCmd.Parameters.Add(new Microsoft.Data.Sqlite.SqliteParameter("@int", 42));
        await insertCmd.ExecuteNonQueryAsync();

        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT text_col FROM edge_test WHERE int_col = 42";
        var result = await selectCmd.ExecuteScalarAsync();
        
        Assert.IsTrue(result == null || result == DBNull.Value);
    }

    // ==================== Large String Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("EdgeCases")]
    [TestCategory("MySQL")]
    public async Task MySQL_LargeString_StoredAndRetrievedCorrectly()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        var largeString = new string('A', 900); // 900 characters

        using var insertCmd = fixture.Connection.CreateCommand();
        insertCmd.CommandText = "INSERT INTO edge_test (text_col, int_col) VALUES (@text, @int)";
        insertCmd.Parameters.Add(new MySqlConnector.MySqlParameter("@text", largeString));
        insertCmd.Parameters.Add(new MySqlConnector.MySqlParameter("@int", 42));
        await insertCmd.ExecuteNonQueryAsync();

        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT text_col FROM edge_test WHERE int_col = 42";
        var result = await selectCmd.ExecuteScalarAsync();
        
        Assert.IsNotNull(result);
        Assert.AreEqual(largeString, result.ToString());
        Assert.AreEqual(900, result.ToString()!.Length);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("EdgeCases")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_LargeString_StoredAndRetrievedCorrectly()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        var largeString = new string('B', 900);

        using var insertCmd = fixture.Connection.CreateCommand();
        insertCmd.CommandText = "INSERT INTO edge_test (text_col, int_col) VALUES (@text, @int)";
        insertCmd.Parameters.Add(new Npgsql.NpgsqlParameter("@text", largeString));
        insertCmd.Parameters.Add(new Npgsql.NpgsqlParameter("@int", 42));
        await insertCmd.ExecuteNonQueryAsync();

        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT text_col FROM edge_test WHERE int_col = 42";
        var result = await selectCmd.ExecuteScalarAsync();
        
        Assert.IsNotNull(result);
        Assert.AreEqual(largeString, result.ToString());
        Assert.AreEqual(900, result.ToString()!.Length);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("EdgeCases")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_LargeString_StoredAndRetrievedCorrectly()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));

        var largeString = new string('C', 900);

        using var insertCmd = fixture.Connection.CreateCommand();
        insertCmd.CommandText = "INSERT INTO edge_test (text_col, int_col) VALUES (@text, @int)";
        insertCmd.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@text", largeString));
        insertCmd.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@int", 42));
        await insertCmd.ExecuteNonQueryAsync();

        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT text_col FROM edge_test WHERE int_col = 42";
        var result = await selectCmd.ExecuteScalarAsync();
        
        Assert.IsNotNull(result);
        Assert.AreEqual(largeString, result.ToString());
        Assert.AreEqual(900, result.ToString()!.Length);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("EdgeCases")]
    [TestCategory("SQLite")]
    public async Task SQLite_LargeString_StoredAndRetrievedCorrectly()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));

        var largeString = new string('D', 900);

        using var insertCmd = fixture.Connection.CreateCommand();
        insertCmd.CommandText = "INSERT INTO edge_test (text_col, int_col) VALUES (@text, @int)";
        insertCmd.Parameters.Add(new Microsoft.Data.Sqlite.SqliteParameter("@text", largeString));
        insertCmd.Parameters.Add(new Microsoft.Data.Sqlite.SqliteParameter("@int", 42));
        await insertCmd.ExecuteNonQueryAsync();

        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT text_col FROM edge_test WHERE int_col = 42";
        var result = await selectCmd.ExecuteScalarAsync();
        
        Assert.IsNotNull(result);
        Assert.AreEqual(largeString, result.ToString());
        Assert.AreEqual(900, result.ToString()!.Length);
    }

    // ==================== Decimal Precision Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("EdgeCases")]
    [TestCategory("MySQL")]
    public async Task MySQL_DecimalPrecision_PreservedCorrectly()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));

        var decimalValue = 123456.789012m;

        using var insertCmd = fixture.Connection.CreateCommand();
        insertCmd.CommandText = "INSERT INTO edge_test (decimal_col, int_col) VALUES (@decimal, @int)";
        insertCmd.Parameters.Add(new MySqlConnector.MySqlParameter("@decimal", decimalValue));
        insertCmd.Parameters.Add(new MySqlConnector.MySqlParameter("@int", 42));
        await insertCmd.ExecuteNonQueryAsync();

        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT decimal_col FROM edge_test WHERE int_col = 42";
        var result = await selectCmd.ExecuteScalarAsync();
        
        Assert.IsNotNull(result);
        Assert.AreEqual(decimalValue, Convert.ToDecimal(result));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("EdgeCases")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_DecimalPrecision_PreservedCorrectly()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));

        var decimalValue = 123456.789012m;

        using var insertCmd = fixture.Connection.CreateCommand();
        insertCmd.CommandText = "INSERT INTO edge_test (decimal_col, int_col) VALUES (@decimal, @int)";
        insertCmd.Parameters.Add(new Npgsql.NpgsqlParameter("@decimal", decimalValue));
        insertCmd.Parameters.Add(new Npgsql.NpgsqlParameter("@int", 42));
        await insertCmd.ExecuteNonQueryAsync();

        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT decimal_col FROM edge_test WHERE int_col = 42";
        var result = await selectCmd.ExecuteScalarAsync();
        
        Assert.IsNotNull(result);
        Assert.AreEqual(decimalValue, Convert.ToDecimal(result));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("EdgeCases")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_DecimalPrecision_PreservedCorrectly()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));

        var decimalValue = 123456.789012m;

        using var insertCmd = fixture.Connection.CreateCommand();
        insertCmd.CommandText = "INSERT INTO edge_test (decimal_col, int_col) VALUES (@decimal, @int)";
        insertCmd.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@decimal", decimalValue));
        insertCmd.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@int", 42));
        await insertCmd.ExecuteNonQueryAsync();

        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT decimal_col FROM edge_test WHERE int_col = 42";
        var result = await selectCmd.ExecuteScalarAsync();
        
        Assert.IsNotNull(result);
        Assert.AreEqual(decimalValue, Convert.ToDecimal(result));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("EdgeCases")]
    [TestCategory("SQLite")]
    public async Task SQLite_DecimalPrecision_PreservedWithinFloatLimits()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));

        // SQLite stores decimals as REAL (floating point), so precision is limited
        var decimalValue = 123456.78m; // Use fewer decimal places for SQLite

        using var insertCmd = fixture.Connection.CreateCommand();
        insertCmd.CommandText = "INSERT INTO edge_test (decimal_col, int_col) VALUES (@decimal, @int)";
        insertCmd.Parameters.Add(new Microsoft.Data.Sqlite.SqliteParameter("@decimal", decimalValue));
        insertCmd.Parameters.Add(new Microsoft.Data.Sqlite.SqliteParameter("@int", 42));
        await insertCmd.ExecuteNonQueryAsync();

        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT decimal_col FROM edge_test WHERE int_col = 42";
        var result = await selectCmd.ExecuteScalarAsync();
        
        Assert.IsNotNull(result);
        // SQLite may have slight precision loss, so we check within a tolerance
        var retrieved = Convert.ToDecimal(result);
        Assert.IsTrue(Math.Abs(decimalValue - retrieved) < 0.01m);
    }
}
