// <copyright file="CaseExpressionsE2ETests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Tests.E2E.Infrastructure;
using System.Data.Common;

namespace Sqlx.Tests.E2E.CaseExpressions;

/// <summary>
/// E2E tests for SQL CASE expressions (simple and searched).
/// </summary>
[TestClass]
public class CaseExpressionsE2ETests : E2ETestBase
{
    private static string GetTestSchema(DatabaseType dbType)
    {
        return dbType switch
        {
            DatabaseType.MySQL => @"
                CREATE TABLE test_employees (
                    id BIGINT AUTO_INCREMENT PRIMARY KEY,
                    name VARCHAR(100) NOT NULL,
                    salary DECIMAL(10, 2) NOT NULL,
                    department VARCHAR(50) NOT NULL,
                    performance_score INT NOT NULL
                )",
            DatabaseType.PostgreSQL => @"
                CREATE TABLE test_employees (
                    id BIGSERIAL PRIMARY KEY,
                    name VARCHAR(100) NOT NULL,
                    salary DECIMAL(10, 2) NOT NULL,
                    department VARCHAR(50) NOT NULL,
                    performance_score INT NOT NULL
                )",
            DatabaseType.SqlServer => @"
                CREATE TABLE test_employees (
                    id BIGINT IDENTITY(1,1) PRIMARY KEY,
                    name NVARCHAR(100) NOT NULL,
                    salary DECIMAL(10, 2) NOT NULL,
                    department NVARCHAR(50) NOT NULL,
                    performance_score INT NOT NULL
                )",
            DatabaseType.SQLite => @"
                CREATE TABLE test_employees (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL,
                    salary REAL NOT NULL,
                    department TEXT NOT NULL,
                    performance_score INTEGER NOT NULL
                )",
            _ => throw new NotSupportedException($"Database type {dbType} is not supported"),
        };
    }

    private static async Task InsertTestDataAsync(DbConnection connection, DatabaseType dbType)
    {
        var employees = new[]
        {
            ("Alice", 120000m, "Engineering", 95),
            ("Bob", 85000m, "Sales", 75),
            ("Charlie", 95000m, "Engineering", 88),
            ("David", 70000m, "Sales", 65),
            ("Eve", 110000m, "Engineering", 92),
            ("Frank", 60000m, "HR", 70),
        };

        foreach (var (name, salary, dept, score) in employees)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "INSERT INTO test_employees (name, salary, department, performance_score) VALUES (@name, @salary, @dept, @score)";
            AddParameter(cmd, "@name", name);
            AddParameter(cmd, "@salary", salary);
            AddParameter(cmd, "@dept", dept);
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

    // ==================== Simple CASE Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("CaseExpressions")]
    [TestCategory("MySQL")]
    public async Task MySQL_SimpleCaseExpression_CategorizesCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.MySQL);

        // Act - Categorize departments
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT name,
                   CASE department
                       WHEN 'Engineering' THEN 'Technical'
                       WHEN 'Sales' THEN 'Business'
                       ELSE 'Support'
                   END as category
            FROM test_employees
            ORDER BY name";

        var results = new List<(string Name, string Category)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.GetString(1)));
        }

        // Assert
        Assert.AreEqual(6, results.Count);
        Assert.AreEqual("Alice", results[0].Name);
        Assert.AreEqual("Technical", results[0].Category);
        Assert.AreEqual("Bob", results[1].Name);
        Assert.AreEqual("Business", results[1].Category);
        Assert.AreEqual("Frank", results[5].Name);
        Assert.AreEqual("Support", results[5].Category);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("CaseExpressions")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_SimpleCaseExpression_CategorizesCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.PostgreSQL);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT name,
                   CASE department
                       WHEN 'Engineering' THEN 'Technical'
                       WHEN 'Sales' THEN 'Business'
                       ELSE 'Support'
                   END as category
            FROM test_employees
            ORDER BY name";

        var results = new List<(string Name, string Category)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.GetString(1)));
        }

        // Assert
        Assert.AreEqual(6, results.Count);
        Assert.AreEqual("Technical", results[0].Category);
        Assert.AreEqual("Business", results[1].Category);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("CaseExpressions")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_SimpleCaseExpression_CategorizesCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.SqlServer);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT name,
                   CASE department
                       WHEN 'Engineering' THEN 'Technical'
                       WHEN 'Sales' THEN 'Business'
                       ELSE 'Support'
                   END as category
            FROM test_employees
            ORDER BY name";

        var results = new List<(string Name, string Category)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.GetString(1)));
        }

        // Assert
        Assert.AreEqual(6, results.Count);
        Assert.AreEqual("Technical", results[0].Category);
        Assert.AreEqual("Business", results[1].Category);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("CaseExpressions")]
    [TestCategory("SQLite")]
    public async Task SQLite_SimpleCaseExpression_CategorizesCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.SQLite);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT name,
                   CASE department
                       WHEN 'Engineering' THEN 'Technical'
                       WHEN 'Sales' THEN 'Business'
                       ELSE 'Support'
                   END as category
            FROM test_employees
            ORDER BY name";

        var results = new List<(string Name, string Category)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.GetString(1)));
        }

        // Assert
        Assert.AreEqual(6, results.Count);
        Assert.AreEqual("Technical", results[0].Category);
        Assert.AreEqual("Business", results[1].Category);
    }

    // ==================== Searched CASE Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("CaseExpressions")]
    [TestCategory("MySQL")]
    public async Task MySQL_SearchedCaseExpression_RangesCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.MySQL);

        // Act - Categorize by salary ranges
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT name, salary,
                   CASE
                       WHEN salary >= 100000 THEN 'High'
                       WHEN salary >= 80000 THEN 'Medium'
                       ELSE 'Low'
                   END as salary_band
            FROM test_employees
            ORDER BY salary DESC";

        var results = new List<(string Name, decimal Salary, string Band)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.GetDecimal(1), reader.GetString(2)));
        }

        // Assert
        Assert.AreEqual(6, results.Count);
        Assert.AreEqual("Alice", results[0].Name);
        Assert.AreEqual("High", results[0].Band);
        Assert.AreEqual("Charlie", results[2].Name);
        Assert.AreEqual("Medium", results[2].Band);
        Assert.AreEqual("Frank", results[5].Name);
        Assert.AreEqual("Low", results[5].Band);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("CaseExpressions")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_SearchedCaseExpression_RangesCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.PostgreSQL);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT name, salary,
                   CASE
                       WHEN salary >= 100000 THEN 'High'
                       WHEN salary >= 80000 THEN 'Medium'
                       ELSE 'Low'
                   END as salary_band
            FROM test_employees
            ORDER BY salary DESC";

        var results = new List<(string Name, decimal Salary, string Band)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.GetDecimal(1), reader.GetString(2)));
        }

        // Assert
        Assert.AreEqual(6, results.Count);
        Assert.AreEqual("High", results[0].Band);
        Assert.AreEqual("Medium", results[2].Band);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("CaseExpressions")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_SearchedCaseExpression_RangesCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.SqlServer);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT name, salary,
                   CASE
                       WHEN salary >= 100000 THEN 'High'
                       WHEN salary >= 80000 THEN 'Medium'
                       ELSE 'Low'
                   END as salary_band
            FROM test_employees
            ORDER BY salary DESC";

        var results = new List<(string Name, decimal Salary, string Band)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.GetDecimal(1), reader.GetString(2)));
        }

        // Assert
        Assert.AreEqual(6, results.Count);
        Assert.AreEqual("High", results[0].Band);
        Assert.AreEqual("Medium", results[2].Band);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("CaseExpressions")]
    [TestCategory("SQLite")]
    public async Task SQLite_SearchedCaseExpression_RangesCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.SQLite);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT name, salary,
                   CASE
                       WHEN salary >= 100000 THEN 'High'
                       WHEN salary >= 80000 THEN 'Medium'
                       ELSE 'Low'
                   END as salary_band
            FROM test_employees
            ORDER BY salary DESC";

        var results = new List<(string Name, double Salary, string Band)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.GetDouble(1), reader.GetString(2)));
        }

        // Assert
        Assert.AreEqual(6, results.Count);
        Assert.AreEqual("High", results[0].Band);
        Assert.AreEqual("Medium", results[2].Band);
    }

    // ==================== CASE in Aggregation Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("CaseExpressions")]
    [TestCategory("MySQL")]
    public async Task MySQL_CaseInAggregation_CountsConditionally()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.MySQL);

        // Act - Count high performers (score >= 90)
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT 
                COUNT(CASE WHEN performance_score >= 90 THEN 1 END) as high_performers,
                COUNT(CASE WHEN performance_score >= 70 AND performance_score < 90 THEN 1 END) as medium_performers,
                COUNT(CASE WHEN performance_score < 70 THEN 1 END) as low_performers
            FROM test_employees";

        using var reader = await cmd.ExecuteReaderAsync();
        await reader.ReadAsync();
        var high = Convert.ToInt32(reader.GetValue(0));
        var medium = Convert.ToInt32(reader.GetValue(1));
        var low = Convert.ToInt32(reader.GetValue(2));

        // Assert - 3 high (95, 92, 95), 2 medium (75, 88, 70), 1 low (65)
        Assert.AreEqual(3, high);
        Assert.AreEqual(3, medium);
        Assert.AreEqual(1, low);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("CaseExpressions")]
    [TestCategory("SQLite")]
    public async Task SQLite_CaseInAggregation_CountsConditionally()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.SQLite);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT 
                COUNT(CASE WHEN performance_score >= 90 THEN 1 END) as high_performers,
                COUNT(CASE WHEN performance_score >= 70 AND performance_score < 90 THEN 1 END) as medium_performers,
                COUNT(CASE WHEN performance_score < 70 THEN 1 END) as low_performers
            FROM test_employees";

        using var reader = await cmd.ExecuteReaderAsync();
        await reader.ReadAsync();
        var high = reader.GetInt64(0);
        var medium = reader.GetInt64(1);
        var low = reader.GetInt64(2);

        // Assert
        Assert.AreEqual(3, high);
        Assert.AreEqual(3, medium);
        Assert.AreEqual(1, low);
    }

    // ==================== Nested CASE Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("CaseExpressions")]
    [TestCategory("SQLite")]
    public async Task SQLite_NestedCaseExpression_EvaluatesCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.SQLite);

        // Act - Nested CASE for bonus calculation
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT name,
                   CASE department
                       WHEN 'Engineering' THEN
                           CASE
                               WHEN performance_score >= 90 THEN salary * 0.15
                               WHEN performance_score >= 80 THEN salary * 0.10
                               ELSE salary * 0.05
                           END
                       WHEN 'Sales' THEN
                           CASE
                               WHEN performance_score >= 80 THEN salary * 0.20
                               ELSE salary * 0.10
                           END
                       ELSE salary * 0.05
                   END as bonus
            FROM test_employees
            WHERE name = 'Alice'";

        using var reader = await cmd.ExecuteReaderAsync();
        await reader.ReadAsync();
        var name = reader.GetString(0);
        var bonus = reader.GetDouble(1);

        // Assert - Alice: Engineering, score 95 >= 90, bonus = 120000 * 0.15 = 18000
        Assert.AreEqual("Alice", name);
        Assert.AreEqual(18000.0, bonus, 0.01);
    }
}
