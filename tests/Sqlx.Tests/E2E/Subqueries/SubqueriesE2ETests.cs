// <copyright file="SubqueriesE2ETests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Tests.E2E.Infrastructure;
using System.Data.Common;

namespace Sqlx.Tests.E2E.Subqueries;

/// <summary>
/// E2E tests for SQL subqueries (scalar, IN, EXISTS, correlated).
/// </summary>
[TestClass]
public class SubqueriesE2ETests : E2ETestBase
{
    private static string GetTestSchema(DatabaseType dbType)
    {
        var usersTable = dbType switch
        {
            DatabaseType.MySQL => @"
                CREATE TABLE test_users (
                    id BIGINT AUTO_INCREMENT PRIMARY KEY,
                    name VARCHAR(100) NOT NULL,
                    department_id INT NOT NULL,
                    salary DECIMAL(10, 2) NOT NULL
                )",
            DatabaseType.PostgreSQL => @"
                CREATE TABLE test_users (
                    id BIGSERIAL PRIMARY KEY,
                    name VARCHAR(100) NOT NULL,
                    department_id INT NOT NULL,
                    salary DECIMAL(10, 2) NOT NULL
                )",
            DatabaseType.SqlServer => @"
                CREATE TABLE test_users (
                    id BIGINT IDENTITY(1,1) PRIMARY KEY,
                    name NVARCHAR(100) NOT NULL,
                    department_id INT NOT NULL,
                    salary DECIMAL(10, 2) NOT NULL
                )",
            DatabaseType.SQLite => @"
                CREATE TABLE test_users (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL,
                    department_id INTEGER NOT NULL,
                    salary REAL NOT NULL
                )",
            _ => throw new NotSupportedException($"Database type {dbType} is not supported"),
        };

        var departmentsTable = dbType switch
        {
            DatabaseType.MySQL => @"
                CREATE TABLE test_departments (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    name VARCHAR(100) NOT NULL,
                    budget DECIMAL(10, 2) NOT NULL
                )",
            DatabaseType.PostgreSQL => @"
                CREATE TABLE test_departments (
                    id SERIAL PRIMARY KEY,
                    name VARCHAR(100) NOT NULL,
                    budget DECIMAL(10, 2) NOT NULL
                )",
            DatabaseType.SqlServer => @"
                CREATE TABLE test_departments (
                    id INT IDENTITY(1,1) PRIMARY KEY,
                    name NVARCHAR(100) NOT NULL,
                    budget DECIMAL(10, 2) NOT NULL
                )",
            DatabaseType.SQLite => @"
                CREATE TABLE test_departments (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL,
                    budget REAL NOT NULL
                )",
            _ => throw new NotSupportedException($"Database type {dbType} is not supported"),
        };

        return usersTable + ";\n" + departmentsTable;
    }

    private static async Task InsertTestDataAsync(DbConnection connection, DatabaseType dbType)
    {
        // Insert departments
        var departments = new[] { ("Engineering", 500000m), ("Sales", 300000m), ("HR", 150000m) };
        for (int i = 0; i < departments.Length; i++)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "INSERT INTO test_departments (name, budget) VALUES (@name, @budget)";
            AddParameter(cmd, "@name", departments[i].Item1);
            AddParameter(cmd, "@budget", departments[i].Item2);
            await cmd.ExecuteNonQueryAsync();
        }

        // Insert users
        var users = new[]
        {
            ("Alice", 1, 80000m),
            ("Bob", 1, 90000m),
            ("Charlie", 2, 70000m),
            ("David", 2, 75000m),
            ("Eve", 3, 60000m),
        };

        foreach (var (name, deptId, salary) in users)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "INSERT INTO test_users (name, department_id, salary) VALUES (@name, @dept_id, @salary)";
            AddParameter(cmd, "@name", name);
            AddParameter(cmd, "@dept_id", deptId);
            AddParameter(cmd, "@salary", salary);
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

    // ==================== Scalar Subquery Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Subqueries")]
    [TestCategory("MySQL")]
    public async Task MySQL_ScalarSubquery_ReturnsCorrectValue()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.MySQL);

        // Act - Get users with salary above average
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT name, salary
            FROM test_users
            WHERE salary > (SELECT AVG(salary) FROM test_users)
            ORDER BY name";

        var results = new List<(string Name, decimal Salary)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.GetDecimal(1)));
        }

        // Assert - Average is 75000, so Bob (90000) and Alice (80000) are above
        Assert.AreEqual(2, results.Count);
        Assert.AreEqual("Alice", results[0].Name);
        Assert.AreEqual(80000m, results[0].Salary);
        Assert.AreEqual("Bob", results[1].Name);
        Assert.AreEqual(90000m, results[1].Salary);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Subqueries")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_ScalarSubquery_ReturnsCorrectValue()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.PostgreSQL);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT name, salary
            FROM test_users
            WHERE salary > (SELECT AVG(salary) FROM test_users)
            ORDER BY name";

        var results = new List<(string Name, decimal Salary)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.GetDecimal(1)));
        }

        // Assert
        Assert.AreEqual(2, results.Count);
        Assert.AreEqual("Alice", results[0].Name);
        Assert.AreEqual("Bob", results[1].Name);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Subqueries")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_ScalarSubquery_ReturnsCorrectValue()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.SqlServer);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT name, salary
            FROM test_users
            WHERE salary > (SELECT AVG(salary) FROM test_users)
            ORDER BY name";

        var results = new List<(string Name, decimal Salary)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.GetDecimal(1)));
        }

        // Assert
        Assert.AreEqual(2, results.Count);
        Assert.AreEqual("Alice", results[0].Name);
        Assert.AreEqual("Bob", results[1].Name);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Subqueries")]
    [TestCategory("SQLite")]
    public async Task SQLite_ScalarSubquery_ReturnsCorrectValue()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.SQLite);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT name, salary
            FROM test_users
            WHERE salary > (SELECT AVG(salary) FROM test_users)
            ORDER BY name";

        var results = new List<(string Name, double Salary)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.GetDouble(1)));
        }

        // Assert
        Assert.AreEqual(2, results.Count);
        Assert.AreEqual("Alice", results[0].Name);
        Assert.AreEqual("Bob", results[1].Name);
    }

    // ==================== IN Subquery Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Subqueries")]
    [TestCategory("MySQL")]
    public async Task MySQL_InSubquery_FiltersCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.MySQL);

        // Act - Get users in departments with budget > 200000
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT name, department_id
            FROM test_users
            WHERE department_id IN (
                SELECT id FROM test_departments WHERE budget > 200000
            )
            ORDER BY name";

        var results = new List<(string Name, int DeptId)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.GetInt32(1)));
        }

        // Assert - Engineering (500000) and Sales (300000) qualify
        Assert.AreEqual(4, results.Count);
        Assert.AreEqual("Alice", results[0].Name);
        Assert.AreEqual(1, results[0].DeptId);
        Assert.AreEqual("Bob", results[1].Name);
        Assert.AreEqual("Charlie", results[2].Name);
        Assert.AreEqual(2, results[2].DeptId);
        Assert.AreEqual("David", results[3].Name);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Subqueries")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_InSubquery_FiltersCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.PostgreSQL);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT name, department_id
            FROM test_users
            WHERE department_id IN (
                SELECT id FROM test_departments WHERE budget > 200000
            )
            ORDER BY name";

        var results = new List<(string Name, int DeptId)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.GetInt32(1)));
        }

        // Assert
        Assert.AreEqual(4, results.Count);
        Assert.AreEqual("Alice", results[0].Name);
        Assert.AreEqual("Charlie", results[2].Name);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Subqueries")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_InSubquery_FiltersCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.SqlServer);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT name, department_id
            FROM test_users
            WHERE department_id IN (
                SELECT id FROM test_departments WHERE budget > 200000
            )
            ORDER BY name";

        var results = new List<(string Name, int DeptId)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.GetInt32(1)));
        }

        // Assert
        Assert.AreEqual(4, results.Count);
        Assert.AreEqual("Alice", results[0].Name);
        Assert.AreEqual("Charlie", results[2].Name);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Subqueries")]
    [TestCategory("SQLite")]
    public async Task SQLite_InSubquery_FiltersCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.SQLite);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT name, department_id
            FROM test_users
            WHERE department_id IN (
                SELECT id FROM test_departments WHERE budget > 200000
            )
            ORDER BY name";

        var results = new List<(string Name, long DeptId)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.GetInt64(1)));
        }

        // Assert
        Assert.AreEqual(4, results.Count);
        Assert.AreEqual("Alice", results[0].Name);
        Assert.AreEqual("Charlie", results[2].Name);
    }

    // ==================== EXISTS Subquery Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Subqueries")]
    [TestCategory("MySQL")]
    public async Task MySQL_ExistsSubquery_FiltersCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.MySQL);

        // Act - Get departments that have users
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT name
            FROM test_departments d
            WHERE EXISTS (
                SELECT 1 FROM test_users u WHERE u.department_id = d.id
            )
            ORDER BY name";

        var results = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(reader.GetString(0));
        }

        // Assert - All 3 departments have users
        Assert.AreEqual(3, results.Count);
        Assert.AreEqual("Engineering", results[0]);
        Assert.AreEqual("HR", results[1]);
        Assert.AreEqual("Sales", results[2]);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Subqueries")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_ExistsSubquery_FiltersCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.PostgreSQL);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT name
            FROM test_departments d
            WHERE EXISTS (
                SELECT 1 FROM test_users u WHERE u.department_id = d.id
            )
            ORDER BY name";

        var results = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(reader.GetString(0));
        }

        // Assert
        Assert.AreEqual(3, results.Count);
        Assert.AreEqual("Engineering", results[0]);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Subqueries")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_ExistsSubquery_FiltersCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.SqlServer);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT name
            FROM test_departments d
            WHERE EXISTS (
                SELECT 1 FROM test_users u WHERE u.department_id = d.id
            )
            ORDER BY name";

        var results = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(reader.GetString(0));
        }

        // Assert
        Assert.AreEqual(3, results.Count);
        Assert.AreEqual("Engineering", results[0]);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Subqueries")]
    [TestCategory("SQLite")]
    public async Task SQLite_ExistsSubquery_FiltersCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.SQLite);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT name
            FROM test_departments d
            WHERE EXISTS (
                SELECT 1 FROM test_users u WHERE u.department_id = d.id
            )
            ORDER BY name";

        var results = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(reader.GetString(0));
        }

        // Assert
        Assert.AreEqual(3, results.Count);
        Assert.AreEqual("Engineering", results[0]);
    }

    // ==================== Correlated Subquery Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Subqueries")]
    [TestCategory("MySQL")]
    public async Task MySQL_CorrelatedSubquery_CalculatesCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.MySQL);

        // Act - Get users with salary above their department average
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT u.name, u.salary, u.department_id
            FROM test_users u
            WHERE u.salary > (
                SELECT AVG(u2.salary)
                FROM test_users u2
                WHERE u2.department_id = u.department_id
            )
            ORDER BY u.name";

        var results = new List<(string Name, decimal Salary, int DeptId)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.GetDecimal(1), reader.GetInt32(2)));
        }

        // Assert - Bob (90000 > 85000 avg in Engineering), David (75000 > 72500 avg in Sales)
        Assert.AreEqual(2, results.Count);
        Assert.AreEqual("Bob", results[0].Name);
        Assert.AreEqual(90000m, results[0].Salary);
        Assert.AreEqual("David", results[1].Name);
        Assert.AreEqual(75000m, results[1].Salary);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Subqueries")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_CorrelatedSubquery_CalculatesCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.PostgreSQL);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT u.name, u.salary, u.department_id
            FROM test_users u
            WHERE u.salary > (
                SELECT AVG(u2.salary)
                FROM test_users u2
                WHERE u2.department_id = u.department_id
            )
            ORDER BY u.name";

        var results = new List<(string Name, decimal Salary, int DeptId)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.GetDecimal(1), reader.GetInt32(2)));
        }

        // Assert
        Assert.AreEqual(2, results.Count);
        Assert.AreEqual("Bob", results[0].Name);
        Assert.AreEqual("David", results[1].Name);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Subqueries")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_CorrelatedSubquery_CalculatesCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.SqlServer);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT u.name, u.salary, u.department_id
            FROM test_users u
            WHERE u.salary > (
                SELECT AVG(u2.salary)
                FROM test_users u2
                WHERE u2.department_id = u.department_id
            )
            ORDER BY u.name";

        var results = new List<(string Name, decimal Salary, int DeptId)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.GetDecimal(1), reader.GetInt32(2)));
        }

        // Assert
        Assert.AreEqual(2, results.Count);
        Assert.AreEqual("Bob", results[0].Name);
        Assert.AreEqual("David", results[1].Name);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Subqueries")]
    [TestCategory("SQLite")]
    public async Task SQLite_CorrelatedSubquery_CalculatesCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.SQLite);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT u.name, u.salary, u.department_id
            FROM test_users u
            WHERE u.salary > (
                SELECT AVG(u2.salary)
                FROM test_users u2
                WHERE u2.department_id = u.department_id
            )
            ORDER BY u.name";

        var results = new List<(string Name, double Salary, long DeptId)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.GetDouble(1), reader.GetInt64(2)));
        }

        // Assert
        Assert.AreEqual(2, results.Count);
        Assert.AreEqual("Bob", results[0].Name);
        Assert.AreEqual("David", results[1].Name);
    }

    // ==================== NOT IN Subquery Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Subqueries")]
    [TestCategory("SQLite")]
    public async Task SQLite_NotInSubquery_FiltersCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));
        await InsertTestDataAsync(fixture.Connection, DatabaseType.SQLite);

        // Act - Get users NOT in high-budget departments (budget > 400000)
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT name, department_id
            FROM test_users
            WHERE department_id NOT IN (
                SELECT id FROM test_departments WHERE budget > 400000
            )
            ORDER BY name";

        var results = new List<(string Name, long DeptId)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.GetInt64(1)));
        }

        // Assert - Only Engineering (500000) is excluded, so Sales and HR users remain
        Assert.AreEqual(3, results.Count);
        Assert.AreEqual("Charlie", results[0].Name);
        Assert.AreEqual(2, results[0].DeptId); // Sales
        Assert.AreEqual("David", results[1].Name);
        Assert.AreEqual("Eve", results[2].Name);
        Assert.AreEqual(3, results[2].DeptId); // HR
    }
}
