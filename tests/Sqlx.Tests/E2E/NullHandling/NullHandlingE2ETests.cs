// <copyright file="NullHandlingE2ETests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Tests.E2E.Infrastructure;
using System.Data.Common;

namespace Sqlx.Tests.E2E.NullHandling;

/// <summary>
/// E2E tests for NULL value handling (IS NULL, IS NOT NULL, COALESCE, NULLIF).
/// </summary>
[TestClass]
public class NullHandlingE2ETests : E2ETestBase
{
    private static string GetTestSchema(DatabaseType dbType)
    {
        return dbType switch
        {
            DatabaseType.MySQL => @"
                CREATE TABLE test_data (
                    id BIGINT AUTO_INCREMENT PRIMARY KEY,
                    name VARCHAR(100) NOT NULL,
                    email VARCHAR(100),
                    phone VARCHAR(20),
                    score INT
                )",
            DatabaseType.PostgreSQL => @"
                CREATE TABLE test_data (
                    id BIGSERIAL PRIMARY KEY,
                    name VARCHAR(100) NOT NULL,
                    email VARCHAR(100),
                    phone VARCHAR(20),
                    score INT
                )",
            DatabaseType.SqlServer => @"
                CREATE TABLE test_data (
                    id BIGINT IDENTITY(1,1) PRIMARY KEY,
                    name NVARCHAR(100) NOT NULL,
                    email NVARCHAR(100),
                    phone NVARCHAR(20),
                    score INT
                )",
            DatabaseType.SQLite => @"
                CREATE TABLE test_data (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL,
                    email TEXT,
                    phone TEXT,
                    score INTEGER
                )",
            _ => throw new NotSupportedException($"Database type {dbType} is not supported"),
        };
    }

    private static async Task InsertTestDataAsync(DbConnection connection)
    {
        var testData = new[]
        {
            ("Alice", "alice@example.com", "123-456-7890", (int?)100),
            ("Bob", null, "987-654-3210", (int?)85),
            ("Charlie", "charlie@example.com", null, (int?)90),
            ("David", null, null, (int?)null),
            ("Eve", "eve@example.com", "555-555-5555", (int?)95),
        };

        foreach (var (name, email, phone, score) in testData)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "INSERT INTO test_data (name, email, phone, score) VALUES (@name, @email, @phone, @score)";
            AddParameter(cmd, "@name", name);
            AddParameter(cmd, "@email", email);
            AddParameter(cmd, "@phone", phone);
            AddParameter(cmd, "@score", score);
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

    // ==================== IS NULL Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("NullHandling")]
    [TestCategory("MySQL")]
    public async Task MySQL_IsNull_FiltersCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));
        await InsertTestDataAsync(fixture.Connection);

        // Act - Get records with NULL email
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT name FROM test_data WHERE email IS NULL ORDER BY name";

        var results = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(reader.GetString(0));
        }

        // Assert - Bob and David have NULL email
        Assert.AreEqual(2, results.Count);
        Assert.AreEqual("Bob", results[0]);
        Assert.AreEqual("David", results[1]);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("NullHandling")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_IsNull_FiltersCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT name FROM test_data WHERE email IS NULL ORDER BY name";

        var results = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(reader.GetString(0));
        }

        // Assert
        Assert.AreEqual(2, results.Count);
        Assert.AreEqual("Bob", results[0]);
        Assert.AreEqual("David", results[1]);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("NullHandling")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_IsNull_FiltersCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT name FROM test_data WHERE email IS NULL ORDER BY name";

        var results = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(reader.GetString(0));
        }

        // Assert
        Assert.AreEqual(2, results.Count);
        Assert.AreEqual("Bob", results[0]);
        Assert.AreEqual("David", results[1]);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("NullHandling")]
    [TestCategory("SQLite")]
    public async Task SQLite_IsNull_FiltersCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT name FROM test_data WHERE email IS NULL ORDER BY name";

        var results = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(reader.GetString(0));
        }

        // Assert
        Assert.AreEqual(2, results.Count);
        Assert.AreEqual("Bob", results[0]);
        Assert.AreEqual("David", results[1]);
    }

    // ==================== IS NOT NULL Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("NullHandling")]
    [TestCategory("MySQL")]
    public async Task MySQL_IsNotNull_FiltersCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));
        await InsertTestDataAsync(fixture.Connection);

        // Act - Get records with non-NULL phone
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT name FROM test_data WHERE phone IS NOT NULL ORDER BY name";

        var results = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(reader.GetString(0));
        }

        // Assert - Alice, Bob, and Eve have phone numbers
        Assert.AreEqual(3, results.Count);
        Assert.AreEqual("Alice", results[0]);
        Assert.AreEqual("Bob", results[1]);
        Assert.AreEqual("Eve", results[2]);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("NullHandling")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_IsNotNull_FiltersCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT name FROM test_data WHERE phone IS NOT NULL ORDER BY name";

        var results = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(reader.GetString(0));
        }

        // Assert
        Assert.AreEqual(3, results.Count);
        Assert.AreEqual("Alice", results[0]);
        Assert.AreEqual("Bob", results[1]);
        Assert.AreEqual("Eve", results[2]);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("NullHandling")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_IsNotNull_FiltersCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT name FROM test_data WHERE phone IS NOT NULL ORDER BY name";

        var results = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(reader.GetString(0));
        }

        // Assert
        Assert.AreEqual(3, results.Count);
        Assert.AreEqual("Alice", results[0]);
        Assert.AreEqual("Bob", results[1]);
        Assert.AreEqual("Eve", results[2]);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("NullHandling")]
    [TestCategory("SQLite")]
    public async Task SQLite_IsNotNull_FiltersCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT name FROM test_data WHERE phone IS NOT NULL ORDER BY name";

        var results = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(reader.GetString(0));
        }

        // Assert
        Assert.AreEqual(3, results.Count);
        Assert.AreEqual("Alice", results[0]);
        Assert.AreEqual("Bob", results[1]);
        Assert.AreEqual("Eve", results[2]);
    }

    // ==================== COALESCE Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("NullHandling")]
    [TestCategory("MySQL")]
    public async Task MySQL_Coalesce_ReturnsFirstNonNull()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));
        await InsertTestDataAsync(fixture.Connection);

        // Act - Get email or phone or 'N/A'
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT name, COALESCE(email, phone, 'N/A') as contact FROM test_data ORDER BY name";

        var results = new List<(string Name, string Contact)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.GetString(1)));
        }

        // Assert
        Assert.AreEqual(5, results.Count);
        Assert.AreEqual("Alice", results[0].Name);
        Assert.AreEqual("alice@example.com", results[0].Contact);
        Assert.AreEqual("Bob", results[1].Name);
        Assert.AreEqual("987-654-3210", results[1].Contact); // phone (email is NULL)
        Assert.AreEqual("Charlie", results[2].Name);
        Assert.AreEqual("charlie@example.com", results[2].Contact);
        Assert.AreEqual("David", results[3].Name);
        Assert.AreEqual("N/A", results[3].Contact); // both NULL
        Assert.AreEqual("Eve", results[4].Name);
        Assert.AreEqual("eve@example.com", results[4].Contact);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("NullHandling")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_Coalesce_ReturnsFirstNonNull()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT name, COALESCE(email, phone, 'N/A') as contact FROM test_data ORDER BY name";

        var results = new List<(string Name, string Contact)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.GetString(1)));
        }

        // Assert
        Assert.AreEqual(5, results.Count);
        Assert.AreEqual("Bob", results[1].Name);
        Assert.AreEqual("987-654-3210", results[1].Contact);
        Assert.AreEqual("David", results[3].Name);
        Assert.AreEqual("N/A", results[3].Contact);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("NullHandling")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_Coalesce_ReturnsFirstNonNull()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT name, COALESCE(email, phone, 'N/A') as contact FROM test_data ORDER BY name";

        var results = new List<(string Name, string Contact)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.GetString(1)));
        }

        // Assert
        Assert.AreEqual(5, results.Count);
        Assert.AreEqual("Bob", results[1].Name);
        Assert.AreEqual("987-654-3210", results[1].Contact);
        Assert.AreEqual("David", results[3].Name);
        Assert.AreEqual("N/A", results[3].Contact);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("NullHandling")]
    [TestCategory("SQLite")]
    public async Task SQLite_Coalesce_ReturnsFirstNonNull()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT name, COALESCE(email, phone, 'N/A') as contact FROM test_data ORDER BY name";

        var results = new List<(string Name, string Contact)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.GetString(1)));
        }

        // Assert
        Assert.AreEqual(5, results.Count);
        Assert.AreEqual("Bob", results[1].Name);
        Assert.AreEqual("987-654-3210", results[1].Contact);
        Assert.AreEqual("David", results[3].Name);
        Assert.AreEqual("N/A", results[3].Contact);
    }

    // ==================== NULL in Aggregation Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("NullHandling")]
    [TestCategory("MySQL")]
    public async Task MySQL_AggregationWithNull_IgnoresNullValues()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));
        await InsertTestDataAsync(fixture.Connection);

        // Act - AVG should ignore NULL values
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*), COUNT(score), AVG(score) FROM test_data";
        using var reader = await cmd.ExecuteReaderAsync();
        await reader.ReadAsync();
        var totalCount = Convert.ToInt32(reader.GetValue(0));
        var scoreCount = Convert.ToInt32(reader.GetValue(1));
        var avgScore = Convert.ToDouble(reader.GetValue(2));

        // Assert - 5 total records, 4 with scores, avg = (100+85+90+95)/4 = 92.5
        Assert.AreEqual(5, totalCount);
        Assert.AreEqual(4, scoreCount);
        Assert.AreEqual(92.5, avgScore, 0.01);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("NullHandling")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_AggregationWithNull_IgnoresNullValues()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*), COUNT(score), AVG(score) FROM test_data";
        using var reader = await cmd.ExecuteReaderAsync();
        await reader.ReadAsync();
        var totalCount = reader.GetInt64(0);
        var scoreCount = reader.GetInt64(1);
        var avgScore = Convert.ToDouble(reader.GetValue(2));

        // Assert
        Assert.AreEqual(5, totalCount);
        Assert.AreEqual(4, scoreCount);
        Assert.AreEqual(92.5, avgScore, 0.01);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("NullHandling")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_AggregationWithNull_IgnoresNullValues()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*), COUNT(score), AVG(CAST(score AS FLOAT)) FROM test_data";
        using var reader = await cmd.ExecuteReaderAsync();
        await reader.ReadAsync();
        var totalCount = reader.GetInt32(0);
        var scoreCount = reader.GetInt32(1);
        var avgScore = reader.GetDouble(2);

        // Assert
        Assert.AreEqual(5, totalCount);
        Assert.AreEqual(4, scoreCount);
        Assert.AreEqual(92.5, avgScore, 0.01);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("NullHandling")]
    [TestCategory("SQLite")]
    public async Task SQLite_AggregationWithNull_IgnoresNullValues()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*), COUNT(score), AVG(score) FROM test_data";
        using var reader = await cmd.ExecuteReaderAsync();
        await reader.ReadAsync();
        var totalCount = reader.GetInt64(0);
        var scoreCount = reader.GetInt64(1);
        var avgScore = reader.GetDouble(2);

        // Assert
        Assert.AreEqual(5, totalCount);
        Assert.AreEqual(4, scoreCount);
        Assert.AreEqual(92.5, avgScore, 0.01);
    }

    // ==================== NULL Comparison Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("NullHandling")]
    [TestCategory("SQLite")]
    public async Task SQLite_NullComparison_BehavesCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));
        await InsertTestDataAsync(fixture.Connection);

        // Act - NULL = NULL is FALSE, must use IS NULL
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM test_data WHERE email = NULL"; // Wrong way
        var wrongCount = Convert.ToInt64(await cmd.ExecuteScalarAsync());

        using var cmd2 = fixture.Connection.CreateCommand();
        cmd2.CommandText = "SELECT COUNT(*) FROM test_data WHERE email IS NULL"; // Correct way
        var correctCount = Convert.ToInt64(await cmd2.ExecuteScalarAsync());

        // Assert
        Assert.AreEqual(0, wrongCount); // NULL = NULL is always FALSE
        Assert.AreEqual(2, correctCount); // IS NULL works correctly
    }
}
