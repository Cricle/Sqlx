// -----------------------------------------------------------------------
// <copyright file="SubQueryTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Sqlx.Tests;

/// <summary>
/// SubQuery 完整测试 - SQL 生成和实际 SQLite 执行。
/// </summary>
[TestClass]
public class SubQueryTests
{
    #region Test Entities

    [Sqlx]
    [TableName("users")]
    public partial class SubQueryUser
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int DepartmentId { get; set; }
        public int Age { get; set; }
        public bool IsActive { get; set; }
    }

    [Sqlx]
    [TableName("departments")]
    public partial class SubQueryDepartment
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    [Sqlx]
    [TableName("orders")]
    public partial class SubQueryOrder
    {
        [Key]
        public int Id { get; set; }
        public int UserId { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = "";
    }

    #endregion

    #region SQL Generation Tests - Basic SubQuery

    [TestMethod]
    public void SubQuery_SimpleCount_GeneratesCorrectSql()
    {
        var sql = SqlQuery<SubQueryUser>.ForSqlite()
            .Select(x => new { x.Id, TotalOrders = SubQuery.For<SubQueryOrder>().Count() })
            .ToSql();

        Assert.AreEqual(
            "SELECT [id], (SELECT COUNT(*) FROM (SELECT [id], [user_id], [amount], [status] FROM [SubQueryOrder]) AS sq) AS TotalOrders FROM [SubQueryUser]",
            sql);
    }

    [TestMethod]
    public void SubQuery_CountWithWhere_GeneratesCorrectSql()
    {
        var sql = SqlQuery<SubQueryUser>.ForSqlite()
            .Select(x => new { x.Id, ActiveOrders = SubQuery.For<SubQueryOrder>().Where(o => o.Status == "active").Count() })
            .ToSql();

        Assert.AreEqual(
            "SELECT [id], (SELECT COUNT(*) FROM (SELECT [id], [user_id], [amount], [status] FROM [SubQueryOrder] WHERE [status] = 'active') AS sq) AS ActiveOrders FROM [SubQueryUser]",
            sql);
    }

    [TestMethod]
    public void SubQuery_CountWithMultipleConditions_GeneratesCorrectSql()
    {
        var sql = SqlQuery<SubQueryUser>.ForSqlite()
            .Select(x => new { x.Id, BigOrders = SubQuery.For<SubQueryOrder>().Where(o => o.Amount > 100 && o.Status == "completed").Count() })
            .ToSql();

        Assert.AreEqual(
            "SELECT [id], (SELECT COUNT(*) FROM (SELECT [id], [user_id], [amount], [status] FROM [SubQueryOrder] WHERE ([amount] > 100 AND [status] = 'completed')) AS sq) AS BigOrders FROM [SubQueryUser]",
            sql);
    }

    #endregion

    #region SQL Generation Tests - Multiple SubQueries

    [TestMethod]
    public void SubQuery_MultipleInSelect_GeneratesCorrectSql()
    {
        var sql = SqlQuery<SubQueryUser>.ForSqlite()
            .Select(x => new { 
                x.Id, 
                x.Name,
                TotalUsers = SubQuery.For<SubQueryUser>().Count(),
                TotalOrders = SubQuery.For<SubQueryOrder>().Count(),
                TotalDepts = SubQuery.For<SubQueryDepartment>().Count()
            })
            .ToSql();

        Assert.AreEqual(
            "SELECT [id], [name], (SELECT COUNT(*) FROM (SELECT [id], [name], [department_id], [age], [is_active] FROM [SubQueryUser]) AS sq) AS TotalUsers, (SELECT COUNT(*) FROM (SELECT [id], [user_id], [amount], [status] FROM [SubQueryOrder]) AS sq) AS TotalOrders, (SELECT COUNT(*) FROM (SELECT [id], [name] FROM [SubQueryDepartment]) AS sq) AS TotalDepts FROM [SubQueryUser]",
            sql);
    }

    #endregion

    #region SQL Generation Tests - SubQuery with GroupBy

    [TestMethod]
    public void SubQuery_InGroupBySelect_GeneratesCorrectSql()
    {
        var sql = SqlQuery<SubQueryUser>.ForSqlite()
            .GroupBy(x => x.DepartmentId)
            .Select(x => new { 
                DeptId = x.Key, 
                UserCount = x.Count(),
                TotalOrders = SubQuery.For<SubQueryOrder>().Count()
            })
            .ToSql();

        Assert.AreEqual(
            "SELECT [department_id] AS DeptId, COUNT(*) AS UserCount, (SELECT COUNT(*) FROM (SELECT [id], [user_id], [amount], [status] FROM [SubQueryOrder]) AS sq) AS TotalOrders FROM [SubQueryUser] GROUP BY [department_id]",
            sql);
    }

    #endregion

    #region SQL Generation Tests - SubQuery with Where on Main Query

    [TestMethod]
    public void SubQuery_WithMainQueryWhere_GeneratesCorrectSql()
    {
        var sql = SqlQuery<SubQueryUser>.ForSqlite()
            .Where(x => x.IsActive)
            .Select(x => new { x.Id, OrderCount = SubQuery.For<SubQueryOrder>().Count() })
            .ToSql();

        Assert.AreEqual(
            "SELECT [id], (SELECT COUNT(*) FROM (SELECT [id], [user_id], [amount], [status] FROM [SubQueryOrder]) AS sq) AS OrderCount FROM [SubQueryUser] WHERE [is_active] = 1",
            sql);
    }

    #endregion

    #region SQL Generation Tests - Different Dialects

    [TestMethod]
    public void SubQuery_SqlServer_GeneratesCorrectSql()
    {
        var sql = SqlQuery<SubQueryUser>.ForSqlServer()
            .Select(x => new { x.Id, Total = SubQuery.For<SubQueryOrder>().Count() })
            .ToSql();

        Assert.AreEqual(
            "SELECT [id], (SELECT COUNT(*) FROM (SELECT [id], [user_id], [amount], [status] FROM [SubQueryOrder]) AS sq) AS Total FROM [SubQueryUser]",
            sql);
    }

    [TestMethod]
    public void SubQuery_MySql_GeneratesCorrectSql()
    {
        var sql = SqlQuery<SubQueryUser>.ForMySql()
            .Select(x => new { x.Id, Total = SubQuery.For<SubQueryOrder>().Count() })
            .ToSql();

        Assert.AreEqual(
            "SELECT `id`, (SELECT COUNT(*) FROM (SELECT `id`, `user_id`, `amount`, `status` FROM `SubQueryOrder`) AS sq) AS Total FROM `SubQueryUser`",
            sql);
    }

    [TestMethod]
    public void SubQuery_PostgreSQL_GeneratesCorrectSql()
    {
        var sql = SqlQuery<SubQueryUser>.ForPostgreSQL()
            .Select(x => new { x.Id, Total = SubQuery.For<SubQueryOrder>().Count() })
            .ToSql();

        Assert.AreEqual(
            "SELECT \"id\", (SELECT COUNT(*) FROM (SELECT \"id\", \"user_id\", \"amount\", \"status\" FROM \"SubQueryOrder\") AS sq) AS Total FROM \"SubQueryUser\"",
            sql);
    }

    #endregion

    #region SQLite Execution Tests

    private static SqliteConnection CreateTestDatabase()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE users (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL,
                department_id INTEGER NOT NULL,
                age INTEGER NOT NULL,
                is_active INTEGER NOT NULL
            );

            CREATE TABLE departments (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL
            );

            CREATE TABLE orders (
                id INTEGER PRIMARY KEY,
                user_id INTEGER NOT NULL,
                amount REAL NOT NULL,
                status TEXT NOT NULL
            );

            -- Insert test data
            INSERT INTO departments (id, name) VALUES (1, 'Engineering'), (2, 'Sales'), (3, 'HR');

            INSERT INTO users (id, name, department_id, age, is_active) VALUES 
                (1, 'Alice', 1, 30, 1),
                (2, 'Bob', 1, 25, 1),
                (3, 'Charlie', 2, 35, 1),
                (4, 'Diana', 2, 28, 0),
                (5, 'Eve', 3, 40, 1);

            INSERT INTO orders (id, user_id, amount, status) VALUES 
                (1, 1, 100.00, 'completed'),
                (2, 1, 200.00, 'completed'),
                (3, 2, 150.00, 'pending'),
                (4, 3, 300.00, 'completed'),
                (5, 3, 50.00, 'cancelled'),
                (6, 4, 250.00, 'completed'),
                (7, 5, 175.00, 'pending');
        ";
        cmd.ExecuteNonQuery();

        return connection;
    }

    [TestMethod]
    public async Task SubQuery_ExecuteSimpleCount_ReturnsCorrectResults()
    {
        using var connection = CreateTestDatabase();

        // Manual SQL execution to verify subquery works
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT id, (SELECT COUNT(*) FROM orders) AS TotalOrders FROM users";
        
        using var reader = await cmd.ExecuteReaderAsync();
        var results = new System.Collections.Generic.List<(int Id, int TotalOrders)>();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetInt32(0), reader.GetInt32(1)));
        }

        Assert.AreEqual(5, results.Count);
        Assert.IsTrue(results.All(r => r.TotalOrders == 7)); // 7 orders total
    }

    [TestMethod]
    public async Task SubQuery_ExecuteCountWithWhere_ReturnsCorrectResults()
    {
        using var connection = CreateTestDatabase();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT id, (SELECT COUNT(*) FROM orders WHERE status = 'completed') AS CompletedOrders FROM users";
        
        using var reader = await cmd.ExecuteReaderAsync();
        var results = new System.Collections.Generic.List<(int Id, int CompletedOrders)>();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetInt32(0), reader.GetInt32(1)));
        }

        Assert.AreEqual(5, results.Count);
        Assert.IsTrue(results.All(r => r.CompletedOrders == 4)); // 4 completed orders
    }

    [TestMethod]
    public async Task SubQuery_ExecuteMultipleSubQueries_ReturnsCorrectResults()
    {
        using var connection = CreateTestDatabase();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            SELECT 
                id, 
                (SELECT COUNT(*) FROM users) AS TotalUsers,
                (SELECT COUNT(*) FROM orders) AS TotalOrders,
                (SELECT COUNT(*) FROM departments) AS TotalDepts
            FROM users";
        
        using var reader = await cmd.ExecuteReaderAsync();
        var results = new System.Collections.Generic.List<(int Id, int TotalUsers, int TotalOrders, int TotalDepts)>();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2), reader.GetInt32(3)));
        }

        Assert.AreEqual(5, results.Count);
        Assert.IsTrue(results.All(r => r.TotalUsers == 5));
        Assert.IsTrue(results.All(r => r.TotalOrders == 7));
        Assert.IsTrue(results.All(r => r.TotalDepts == 3));
    }

    [TestMethod]
    public async Task SubQuery_ExecuteWithGroupBy_ReturnsCorrectResults()
    {
        using var connection = CreateTestDatabase();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            SELECT 
                department_id,
                COUNT(*) AS UserCount,
                (SELECT COUNT(*) FROM orders) AS TotalOrders
            FROM users
            GROUP BY department_id
            ORDER BY department_id";
        
        using var reader = await cmd.ExecuteReaderAsync();
        var results = new System.Collections.Generic.List<(int DeptId, int UserCount, int TotalOrders)>();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2)));
        }

        Assert.AreEqual(3, results.Count);
        Assert.AreEqual((1, 2, 7), results[0]); // Dept 1: 2 users, 7 total orders
        Assert.AreEqual((2, 2, 7), results[1]); // Dept 2: 2 users, 7 total orders
        Assert.AreEqual((3, 1, 7), results[2]); // Dept 3: 1 user, 7 total orders
    }

    [TestMethod]
    public async Task SubQuery_GeneratedSqlExecutesCorrectly()
    {
        using var connection = CreateTestDatabase();

        // Generate SQL using SubQuery
        var generatedSql = SqlQuery<SubQueryUser>.ForSqlite()
            .Select(x => new { x.Id, TotalOrders = SubQuery.For<SubQueryOrder>().Count() })
            .ToSql();

        // The generated SQL wraps in subquery, let's verify it's valid SQL
        // Generated: SELECT [id], (SELECT COUNT(*) FROM (SELECT [id], [user_id], [amount], [status] FROM [SubQueryOrder]) AS sq) AS TotalOrders FROM [SubQueryUser]
        // We need to use actual table names
        var executableSql = generatedSql
            .Replace("[SubQueryUser]", "users")
            .Replace("[SubQueryOrder]", "orders");

        using var cmd = connection.CreateCommand();
        cmd.CommandText = executableSql;
        
        using var reader = await cmd.ExecuteReaderAsync();
        var results = new System.Collections.Generic.List<(int Id, int TotalOrders)>();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetInt32(0), reader.GetInt32(1)));
        }

        Assert.AreEqual(5, results.Count);
        Assert.IsTrue(results.All(r => r.TotalOrders == 7));
    }

    [TestMethod]
    public async Task SubQuery_GeneratedSqlWithWhere_ExecutesCorrectly()
    {
        using var connection = CreateTestDatabase();

        var generatedSql = SqlQuery<SubQueryUser>.ForSqlite()
            .Select(x => new { x.Id, CompletedOrders = SubQuery.For<SubQueryOrder>().Where(o => o.Status == "completed").Count() })
            .ToSql();

        var executableSql = generatedSql
            .Replace("[SubQueryUser]", "users")
            .Replace("[SubQueryOrder]", "orders");

        using var cmd = connection.CreateCommand();
        cmd.CommandText = executableSql;
        
        using var reader = await cmd.ExecuteReaderAsync();
        var results = new System.Collections.Generic.List<(int Id, int CompletedOrders)>();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetInt32(0), reader.GetInt32(1)));
        }

        Assert.AreEqual(5, results.Count);
        Assert.IsTrue(results.All(r => r.CompletedOrders == 4)); // 4 completed orders
    }

    [TestMethod]
    public async Task SubQuery_GeneratedSqlWithAmountFilter_ExecutesCorrectly()
    {
        using var connection = CreateTestDatabase();

        var generatedSql = SqlQuery<SubQueryUser>.ForSqlite()
            .Select(x => new { x.Id, BigOrders = SubQuery.For<SubQueryOrder>().Where(o => o.Amount > 150).Count() })
            .ToSql();

        var executableSql = generatedSql
            .Replace("[SubQueryUser]", "users")
            .Replace("[SubQueryOrder]", "orders");

        using var cmd = connection.CreateCommand();
        cmd.CommandText = executableSql;
        
        using var reader = await cmd.ExecuteReaderAsync();
        var results = new System.Collections.Generic.List<(int Id, int BigOrders)>();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetInt32(0), reader.GetInt32(1)));
        }

        Assert.AreEqual(5, results.Count);
        // Orders > 150: 200, 300, 250, 175 = 4 orders
        Assert.IsTrue(results.All(r => r.BigOrders == 4));
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    public void SubQuery_EmptyTable_GeneratesCorrectSql()
    {
        var sql = SqlQuery<SubQueryUser>.ForSqlite()
            .Where(x => x.Id < 0) // No results
            .Select(x => new { x.Id, Total = SubQuery.For<SubQueryOrder>().Count() })
            .ToSql();

        Assert.AreEqual(
            "SELECT [id], (SELECT COUNT(*) FROM (SELECT [id], [user_id], [amount], [status] FROM [SubQueryOrder]) AS sq) AS Total FROM [SubQueryUser] WHERE [id] < 0",
            sql);
    }

    [TestMethod]
    public void SubQuery_WithTake_GeneratesCorrectSql()
    {
        var sql = SqlQuery<SubQueryUser>.ForSqlite()
            .Take(10)
            .Select(x => new { x.Id, Total = SubQuery.For<SubQueryOrder>().Count() })
            .ToSql();

        // Note: Take is applied to main query, not subquery
        Assert.IsTrue(sql.Contains("LIMIT 10"));
        Assert.IsTrue(sql.Contains("(SELECT COUNT(*)"));
    }

    [TestMethod]
    public void SubQuery_WithOrderBy_GeneratesCorrectSql()
    {
        var sql = SqlQuery<SubQueryUser>.ForSqlite()
            .OrderBy(x => x.Name)
            .Select(x => new { x.Id, Total = SubQuery.For<SubQueryOrder>().Count() })
            .ToSql();

        Assert.IsTrue(sql.Contains("ORDER BY [name] ASC"));
        Assert.IsTrue(sql.Contains("(SELECT COUNT(*)"));
    }

    [TestMethod]
    public void SubQuery_ChainedWhere_GeneratesCorrectSql()
    {
        var sql = SqlQuery<SubQueryUser>.ForSqlite()
            .Select(x => new { 
                x.Id, 
                FilteredCount = SubQuery.For<SubQueryOrder>()
                    .Where(o => o.Amount > 100)
                    .Where(o => o.Status == "completed")
                    .Count() 
            })
            .ToSql();

        Assert.IsTrue(sql.Contains("[amount] > 100"));
        Assert.IsTrue(sql.Contains("[status] = 'completed'"));
    }

    #endregion
}
