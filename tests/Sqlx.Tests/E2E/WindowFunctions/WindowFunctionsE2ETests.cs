// <copyright file="WindowFunctionsE2ETests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Tests.E2E.Infrastructure;
using System.Data.Common;

namespace Sqlx.Tests.E2E.WindowFunctions;

/// <summary>
/// E2E tests for SQL window functions (ROW_NUMBER, RANK, DENSE_RANK, LAG, LEAD).
/// </summary>
[TestClass]
public class WindowFunctionsE2ETests : E2ETestBase
{
    private static string GetTestSchema(DatabaseType dbType)
    {
        return dbType switch
        {
            DatabaseType.MySQL => @"
                CREATE TABLE test_sales (
                    id BIGINT AUTO_INCREMENT PRIMARY KEY,
                    employee_name VARCHAR(100) NOT NULL,
                    department VARCHAR(50) NOT NULL,
                    sale_amount DECIMAL(10, 2) NOT NULL,
                    sale_date DATE NOT NULL
                )",
            DatabaseType.PostgreSQL => @"
                CREATE TABLE test_sales (
                    id BIGSERIAL PRIMARY KEY,
                    employee_name VARCHAR(100) NOT NULL,
                    department VARCHAR(50) NOT NULL,
                    sale_amount DECIMAL(10, 2) NOT NULL,
                    sale_date DATE NOT NULL
                )",
            DatabaseType.SqlServer => @"
                CREATE TABLE test_sales (
                    id BIGINT IDENTITY(1,1) PRIMARY KEY,
                    employee_name NVARCHAR(100) NOT NULL,
                    department NVARCHAR(50) NOT NULL,
                    sale_amount DECIMAL(10, 2) NOT NULL,
                    sale_date DATE NOT NULL
                )",
            DatabaseType.SQLite => @"
                CREATE TABLE test_sales (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    employee_name TEXT NOT NULL,
                    department TEXT NOT NULL,
                    sale_amount REAL NOT NULL,
                    sale_date TEXT NOT NULL
                )",
            _ => throw new NotSupportedException($"Database type {dbType} is not supported"),
        };
    }

    private static async Task InsertTestDataAsync(DbConnection connection)
    {
        var sales = new[]
        {
            ("Alice", "Sales", 5000m, "2024-01-15"),
            ("Bob", "Sales", 7000m, "2024-01-20"),
            ("Charlie", "Sales", 5000m, "2024-01-25"),
            ("David", "Engineering", 8000m, "2024-01-10"),
            ("Eve", "Engineering", 9000m, "2024-01-18"),
            ("Frank", "Engineering", 8000m, "2024-01-22"),
        };

        foreach (var (name, dept, amount, date) in sales)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "INSERT INTO test_sales (employee_name, department, sale_amount, sale_date) VALUES (@name, @dept, @amount, @date)";
            AddParameter(cmd, "@name", name);
            AddParameter(cmd, "@dept", dept);
            AddParameter(cmd, "@amount", amount);
            AddParameter(cmd, "@date", date);
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

    // ==================== ROW_NUMBER Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("WindowFunctions")]
    [TestCategory("MySQL")]
    public async Task MySQL_RowNumber_AssignsSequentialNumbers()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT employee_name, sale_amount,
                   ROW_NUMBER() OVER (PARTITION BY department ORDER BY sale_amount DESC) as row_num
            FROM test_sales
            ORDER BY department, row_num";

        var results = new List<(string Name, decimal Amount, long RowNum)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.GetDecimal(1), reader.GetInt64(2)));
        }

        // Assert
        Assert.AreEqual(6, results.Count);
        var engResults = results.Where(r => r.Name is "David" or "Eve" or "Frank").ToList();
        Assert.AreEqual(1, engResults[0].RowNum); // Eve: 9000
        Assert.AreEqual(2, engResults[1].RowNum); // David or Frank: 8000
        Assert.AreEqual(3, engResults[2].RowNum); // David or Frank: 8000
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("WindowFunctions")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_RowNumber_AssignsSequentialNumbers()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.PostgreSQL));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT employee_name, sale_amount,
                   ROW_NUMBER() OVER (PARTITION BY department ORDER BY sale_amount DESC) as row_num
            FROM test_sales
            ORDER BY department, row_num";

        var results = new List<(string Name, decimal Amount, long RowNum)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.GetDecimal(1), reader.GetInt64(2)));
        }

        // Assert
        Assert.AreEqual(6, results.Count);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("WindowFunctions")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_RowNumber_AssignsSequentialNumbers()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SqlServer));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT employee_name, sale_amount,
                   ROW_NUMBER() OVER (PARTITION BY department ORDER BY sale_amount DESC) as row_num
            FROM test_sales
            ORDER BY department, row_num";

        var results = new List<(string Name, decimal Amount, long RowNum)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.GetDecimal(1), reader.GetInt64(2)));
        }

        // Assert
        Assert.AreEqual(6, results.Count);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("WindowFunctions")]
    [TestCategory("SQLite")]
    public async Task SQLite_RowNumber_AssignsSequentialNumbers()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT employee_name, sale_amount,
                   ROW_NUMBER() OVER (PARTITION BY department ORDER BY sale_amount DESC) as row_num
            FROM test_sales
            ORDER BY department, row_num";

        var results = new List<(string Name, double Amount, long RowNum)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.GetDouble(1), reader.GetInt64(2)));
        }

        // Assert
        Assert.AreEqual(6, results.Count);
    }

    // ==================== RANK Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("WindowFunctions")]
    [TestCategory("MySQL")]
    public async Task MySQL_Rank_HandlesGaps()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT employee_name, sale_amount,
                   RANK() OVER (PARTITION BY department ORDER BY sale_amount DESC) as rank_num
            FROM test_sales
            WHERE department = 'Engineering'
            ORDER BY rank_num, employee_name";

        var results = new List<(string Name, decimal Amount, long Rank)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.GetDecimal(1), reader.GetInt64(2)));
        }

        // Assert - Eve: rank 1, David and Frank: rank 2 (gap to 4 if there were more)
        Assert.AreEqual(3, results.Count);
        Assert.AreEqual("Eve", results[0].Name);
        Assert.AreEqual(1, results[0].Rank);
        Assert.AreEqual(2, results[1].Rank); // David or Frank
        Assert.AreEqual(2, results[2].Rank); // David or Frank
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("WindowFunctions")]
    [TestCategory("SQLite")]
    public async Task SQLite_Rank_HandlesGaps()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT employee_name, sale_amount,
                   RANK() OVER (PARTITION BY department ORDER BY sale_amount DESC) as rank_num
            FROM test_sales
            WHERE department = 'Engineering'
            ORDER BY rank_num, employee_name";

        var results = new List<(string Name, double Amount, long Rank)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.GetDouble(1), reader.GetInt64(2)));
        }

        // Assert
        Assert.AreEqual(3, results.Count);
        Assert.AreEqual(1, results[0].Rank);
        Assert.AreEqual(2, results[1].Rank);
    }

    // ==================== DENSE_RANK Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("WindowFunctions")]
    [TestCategory("MySQL")]
    public async Task MySQL_DenseRank_NoGaps()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT employee_name, sale_amount,
                   DENSE_RANK() OVER (PARTITION BY department ORDER BY sale_amount DESC) as dense_rank_num
            FROM test_sales
            WHERE department = 'Engineering'
            ORDER BY dense_rank_num, employee_name";

        var results = new List<(string Name, decimal Amount, long DenseRank)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.GetDecimal(1), reader.GetInt64(2)));
        }

        // Assert - Eve: 1, David and Frank: 2 (no gap)
        Assert.AreEqual(3, results.Count);
        Assert.AreEqual("Eve", results[0].Name);
        Assert.AreEqual(1, results[0].DenseRank);
        Assert.AreEqual(2, results[1].DenseRank);
        Assert.AreEqual(2, results[2].DenseRank);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("WindowFunctions")]
    [TestCategory("SQLite")]
    public async Task SQLite_DenseRank_NoGaps()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT employee_name, sale_amount,
                   DENSE_RANK() OVER (PARTITION BY department ORDER BY sale_amount DESC) as dense_rank_num
            FROM test_sales
            WHERE department = 'Engineering'
            ORDER BY dense_rank_num, employee_name";

        var results = new List<(string Name, double Amount, long DenseRank)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.GetDouble(1), reader.GetInt64(2)));
        }

        // Assert
        Assert.AreEqual(3, results.Count);
        Assert.AreEqual(1, results[0].DenseRank);
        Assert.AreEqual(2, results[1].DenseRank);
    }

    // ==================== LAG Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("WindowFunctions")]
    [TestCategory("MySQL")]
    public async Task MySQL_Lag_GetsPreviousValue()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT employee_name, sale_amount,
                   LAG(sale_amount, 1) OVER (PARTITION BY department ORDER BY sale_date) as prev_amount
            FROM test_sales
            WHERE department = 'Sales'
            ORDER BY sale_date";

        var results = new List<(string Name, decimal Amount, decimal? PrevAmount)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var prevAmount = reader.IsDBNull(2) ? (decimal?)null : reader.GetDecimal(2);
            results.Add((reader.GetString(0), reader.GetDecimal(1), prevAmount));
        }

        // Assert
        Assert.AreEqual(3, results.Count);
        Assert.IsNull(results[0].PrevAmount); // First row has no previous
        Assert.IsNotNull(results[1].PrevAmount);
        Assert.IsNotNull(results[2].PrevAmount);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("WindowFunctions")]
    [TestCategory("SQLite")]
    public async Task SQLite_Lag_GetsPreviousValue()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT employee_name, sale_amount,
                   LAG(sale_amount, 1) OVER (PARTITION BY department ORDER BY sale_date) as prev_amount
            FROM test_sales
            WHERE department = 'Sales'
            ORDER BY sale_date";

        var results = new List<(string Name, double Amount, double? PrevAmount)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var prevAmount = reader.IsDBNull(2) ? (double?)null : reader.GetDouble(2);
            results.Add((reader.GetString(0), reader.GetDouble(1), prevAmount));
        }

        // Assert
        Assert.AreEqual(3, results.Count);
        Assert.IsNull(results[0].PrevAmount);
    }

    // ==================== LEAD Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("WindowFunctions")]
    [TestCategory("MySQL")]
    public async Task MySQL_Lead_GetsNextValue()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.MySQL));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT employee_name, sale_amount,
                   LEAD(sale_amount, 1) OVER (PARTITION BY department ORDER BY sale_date) as next_amount
            FROM test_sales
            WHERE department = 'Sales'
            ORDER BY sale_date";

        var results = new List<(string Name, decimal Amount, decimal? NextAmount)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var nextAmount = reader.IsDBNull(2) ? (decimal?)null : reader.GetDecimal(2);
            results.Add((reader.GetString(0), reader.GetDecimal(1), nextAmount));
        }

        // Assert
        Assert.AreEqual(3, results.Count);
        Assert.IsNotNull(results[0].NextAmount);
        Assert.IsNotNull(results[1].NextAmount);
        Assert.IsNull(results[2].NextAmount); // Last row has no next
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("WindowFunctions")]
    [TestCategory("SQLite")]
    public async Task SQLite_Lead_GetsNextValue()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestSchema(DatabaseType.SQLite));
        await InsertTestDataAsync(fixture.Connection);

        // Act
        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = @"
            SELECT employee_name, sale_amount,
                   LEAD(sale_amount, 1) OVER (PARTITION BY department ORDER BY sale_date) as next_amount
            FROM test_sales
            WHERE department = 'Sales'
            ORDER BY sale_date";

        var results = new List<(string Name, double Amount, double? NextAmount)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var nextAmount = reader.IsDBNull(2) ? (double?)null : reader.GetDouble(2);
            results.Add((reader.GetString(0), reader.GetDouble(1), nextAmount));
        }

        // Assert
        Assert.AreEqual(3, results.Count);
        Assert.IsNull(results[2].NextAmount);
    }
}
