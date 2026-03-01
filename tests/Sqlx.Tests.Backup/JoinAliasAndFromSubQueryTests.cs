// -----------------------------------------------------------------------
// <copyright file="JoinAliasAndFromSubQueryTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Sqlx.Tests;

/// <summary>
/// JOIN 别名和 FROM 子查询测试。
/// </summary>
[TestClass]
public class JoinAliasAndFromSubQueryTests
{
    #region Test Entities

    [Sqlx]
    public partial class JaUser
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int DepartmentId { get; set; }
        public bool IsActive { get; set; }
    }

    [Sqlx]
    public partial class JaDepartment
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    [Sqlx]
    public partial class JaOrder
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public decimal Amount { get; set; }
    }

    #endregion

    #region JOIN Alias Tests - SQLite

    [TestMethod]
    public void SQLite_Join_HasTableAliases()
    {
        var users = SqlQuery<JaUser>.ForSqlite();
        var departments = SqlQuery<JaDepartment>.ForSqlite();

        var sql = users
            .Join(departments,
                u => u.DepartmentId,
                d => d.Id,
                (u, d) => new { u.Name, DeptName = d.Name })
            .ToSql();

        // 主表应该有别名 t1，JOIN 表应该有别名 t2
        Assert.IsTrue(sql.Contains("FROM [JaUser] AS [t1]"), $"SQL: {sql}");
        Assert.IsTrue(sql.Contains("INNER JOIN [JaDepartment] AS [t2]"), $"SQL: {sql}");
        Assert.IsTrue(sql.Contains("ON [t1]"), $"SQL: {sql}");
    }

    [TestMethod]
    public void SQLite_MultipleJoins_HasCorrectAliases()
    {
        var users = SqlQuery<JaUser>.ForSqlite();
        var departments = SqlQuery<JaDepartment>.ForSqlite();
        var orders = SqlQuery<JaOrder>.ForSqlite();

        var sql = users
            .Join(departments,
                u => u.DepartmentId,
                d => d.Id,
                (u, d) => new { User = u, Dept = d })
            .Join(orders,
                x => x.User.Id,
                o => o.UserId,
                (x, o) => new { UserName = x.User.Name, DeptName = x.Dept.Name, o.Amount })
            .ToSql();

        // 应该有 t1, t2, t3 三个别名
        Assert.IsTrue(sql.Contains("AS [t1]"), $"SQL: {sql}");
        Assert.IsTrue(sql.Contains("AS [t2]"), $"SQL: {sql}");
        Assert.IsTrue(sql.Contains("AS [t3]"), $"SQL: {sql}");
    }

    #endregion

    #region JOIN with SubQuery Tests - SQLite

    [TestMethod]
    public void SQLite_JoinWithSubQuery_GeneratesCorrectSql()
    {
        var users = SqlQuery<JaUser>.ForSqlite();
        var activeUsers = SqlQuery<JaUser>.ForSqlite().Where(u => u.IsActive);

        var sql = users
            .Join(activeUsers,
                u => u.Id,
                au => au.Id,
                (u, au) => new { u.Name })
            .ToSql();

        // JOIN 应该包含子查询（子查询使用 SELECT * 因为没有 EntityProvider）
        Assert.AreEqual(
            "SELECT [name] FROM [JaUser] AS [t1] INNER JOIN (SELECT [id], [name], [department_id], [is_active] FROM [JaUser] WHERE [is_active] = 1) AS [t2] ON [t1].[id] = [t2].[id]",
            sql);
    }

    [TestMethod]
    public void SQLite_JoinWithFilteredSubQuery_GeneratesCorrectSql()
    {
        var users = SqlQuery<JaUser>.ForSqlite();
        var filteredDepts = SqlQuery<JaDepartment>.ForSqlite()
            .Where(d => d.Name != "HR")
            .Take(10);

        var sql = users
            .Join(filteredDepts,
                u => u.DepartmentId,
                d => d.Id,
                (u, d) => new { u.Name, DeptName = d.Name })
            .ToSql();

        // 子查询使用 SELECT * 因为没有 EntityProvider
        Assert.AreEqual(
            "SELECT [name], [name] AS DeptName FROM [JaUser] AS [t1] INNER JOIN (SELECT [id], [name] FROM [JaDepartment] WHERE [name] <> 'HR' LIMIT 10) AS [t2] ON [t1].[department_id] = [t2].[id]",
            sql);
    }

    #endregion

    #region FROM SubQuery Tests - SQLite

    [TestMethod]
    public void SQLite_FromSubQuery_SimpleWhere()
    {
        var subQuery = SqlQuery<JaUser>.ForSqlite().Where(u => u.IsActive);
        var sql = SqlQuery<JaUser>.For(SqlDefine.SQLite, subQuery).ToSql();

        // 子查询和外层查询都会展开列名（因为有 EntityProvider）
        Assert.AreEqual(
            "SELECT [id], [name], [department_id], [is_active] FROM (SELECT [id], [name], [department_id], [is_active] FROM [JaUser] WHERE [is_active] = 1) AS [sq]",
            sql);
    }

    [TestMethod]
    public void SQLite_FromSubQuery_WithSelect()
    {
        var subQuery = SqlQuery<JaUser>.ForSqlite()
            .Where(u => u.IsActive);

        var sql = SqlQuery<JaUser>.For(SqlDefine.SQLite, subQuery)
            .Select(u => new { u.Id, u.Name })
            .ToSql();

        Assert.AreEqual(
            "SELECT [id], [name] FROM (SELECT [id], [name], [department_id], [is_active] FROM [JaUser] WHERE [is_active] = 1) AS [sq]",
            sql);
    }

    [TestMethod]
    public void SQLite_FromSubQuery_WithOrderByAndTake()
    {
        var subQuery = SqlQuery<JaUser>.ForSqlite()
            .OrderBy(u => u.Name)
            .Take(100);

        var sql = SqlQuery<JaUser>.For(SqlDefine.SQLite, subQuery).ToSql();

        Assert.AreEqual(
            "SELECT [id], [name], [department_id], [is_active] FROM (SELECT [id], [name], [department_id], [is_active] FROM [JaUser] ORDER BY [name] ASC LIMIT 100) AS [sq]",
            sql);
    }

    [TestMethod]
    public void SQLite_FromSubQuery_WithOuterWhere()
    {
        var subQuery = SqlQuery<JaUser>.ForSqlite().Where(u => u.IsActive);
        var sql = SqlQuery<JaUser>.For(SqlDefine.SQLite, subQuery)
            .Where(u => u.DepartmentId == 1)
            .ToSql();

        Assert.AreEqual(
            "SELECT [id], [name], [department_id], [is_active] FROM (SELECT [id], [name], [department_id], [is_active] FROM [JaUser] WHERE [is_active] = 1) AS [sq] WHERE [department_id] = 1",
            sql);
    }

    [TestMethod]
    public void SQLite_FromSubQuery_NestedSubQuery()
    {
        var innerSubQuery = SqlQuery<JaUser>.ForSqlite().Where(u => u.IsActive);
        var outerSubQuery = SqlQuery<JaUser>.For(SqlDefine.SQLite, innerSubQuery).Take(50);
        var sql = SqlQuery<JaUser>.For(SqlDefine.SQLite, outerSubQuery).ToSql();

        Assert.AreEqual(
            "SELECT [id], [name], [department_id], [is_active] FROM (SELECT [id], [name], [department_id], [is_active] FROM (SELECT [id], [name], [department_id], [is_active] FROM [JaUser] WHERE [is_active] = 1) AS [sq] LIMIT 50) AS [sq]",
            sql);
    }

    #endregion

    #region Cross-Dialect FROM SubQuery Tests

    [TestMethod]
    public void SqlServer_FromSubQuery_SimpleWhere()
    {
        var subQuery = SqlQuery<JaUser>.ForSqlServer().Where(u => u.IsActive);
        var sql = SqlQuery<JaUser>.For(SqlDefine.SqlServer, subQuery).ToSql();

        Assert.AreEqual(
            "SELECT [id], [name], [department_id], [is_active] FROM (SELECT [id], [name], [department_id], [is_active] FROM [JaUser] WHERE [is_active] = 1) AS [sq]",
            sql);
    }

    [TestMethod]
    public void MySql_FromSubQuery_SimpleWhere()
    {
        var subQuery = SqlQuery<JaUser>.ForMySql().Where(u => u.IsActive);
        var sql = SqlQuery<JaUser>.For(SqlDefine.MySql, subQuery).ToSql();

        Assert.AreEqual(
            "SELECT `id`, `name`, `department_id`, `is_active` FROM (SELECT `id`, `name`, `department_id`, `is_active` FROM `JaUser` WHERE `is_active` = 1) AS `sq`",
            sql);
    }

    [TestMethod]
    public void PostgreSQL_FromSubQuery_SimpleWhere()
    {
        var subQuery = SqlQuery<JaUser>.ForPostgreSQL().Where(u => u.IsActive);
        var sql = SqlQuery<JaUser>.For(SqlDefine.PostgreSql, subQuery).ToSql();

        Assert.AreEqual(
            "SELECT \"id\", \"name\", \"department_id\", \"is_active\" FROM (SELECT \"id\", \"name\", \"department_id\", \"is_active\" FROM \"JaUser\" WHERE \"is_active\" = true) AS \"sq\"",
            sql);
    }

    [TestMethod]
    public void Oracle_FromSubQuery_SimpleWhere()
    {
        var subQuery = SqlQuery<JaUser>.ForOracle().Where(u => u.IsActive);
        var sql = SqlQuery<JaUser>.For(SqlDefine.Oracle, subQuery).ToSql();

        Assert.AreEqual(
            "SELECT \"id\", \"name\", \"department_id\", \"is_active\" FROM (SELECT \"id\", \"name\", \"department_id\", \"is_active\" FROM \"JaUser\" WHERE \"is_active\" = 1) AS \"sq\"",
            sql);
    }

    [TestMethod]
    public void DB2_FromSubQuery_SimpleWhere()
    {
        var subQuery = SqlQuery<JaUser>.ForDB2().Where(u => u.IsActive);
        var sql = SqlQuery<JaUser>.For(SqlDefine.DB2, subQuery).ToSql();

        Assert.AreEqual(
            "SELECT \"id\", \"name\", \"department_id\", \"is_active\" FROM (SELECT \"id\", \"name\", \"department_id\", \"is_active\" FROM \"JaUser\" WHERE \"is_active\" = 1) AS \"sq\"",
            sql);
    }

    #endregion

    #region Cross-Dialect JOIN Alias Tests

    [TestMethod]
    public void SqlServer_Join_HasTableAliases()
    {
        var users = SqlQuery<JaUser>.ForSqlServer();
        var departments = SqlQuery<JaDepartment>.ForSqlServer();

        var sql = users
            .Join(departments,
                u => u.DepartmentId,
                d => d.Id,
                (u, d) => new { u.Name, DeptName = d.Name })
            .ToSql();

        Assert.IsTrue(sql.Contains("FROM [JaUser] AS [t1]"), $"SQL: {sql}");
        Assert.IsTrue(sql.Contains("INNER JOIN [JaDepartment] AS [t2]"), $"SQL: {sql}");
    }

    [TestMethod]
    public void MySql_Join_HasTableAliases()
    {
        var users = SqlQuery<JaUser>.ForMySql();
        var departments = SqlQuery<JaDepartment>.ForMySql();

        var sql = users
            .Join(departments,
                u => u.DepartmentId,
                d => d.Id,
                (u, d) => new { u.Name, DeptName = d.Name })
            .ToSql();

        Assert.IsTrue(sql.Contains("FROM `JaUser` AS `t1`"), $"SQL: {sql}");
        Assert.IsTrue(sql.Contains("INNER JOIN `JaDepartment` AS `t2`"), $"SQL: {sql}");
    }

    [TestMethod]
    public void PostgreSQL_Join_HasTableAliases()
    {
        var users = SqlQuery<JaUser>.ForPostgreSQL();
        var departments = SqlQuery<JaDepartment>.ForPostgreSQL();

        var sql = users
            .Join(departments,
                u => u.DepartmentId,
                d => d.Id,
                (u, d) => new { u.Name, DeptName = d.Name })
            .ToSql();

        Assert.IsTrue(sql.Contains("FROM \"JaUser\" AS \"t1\""), $"SQL: {sql}");
        Assert.IsTrue(sql.Contains("INNER JOIN \"JaDepartment\" AS \"t2\""), $"SQL: {sql}");
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
                is_active INTEGER NOT NULL
            );

            CREATE TABLE departments (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL
            );

            CREATE TABLE orders (
                id INTEGER PRIMARY KEY,
                user_id INTEGER NOT NULL,
                amount REAL NOT NULL
            );

            INSERT INTO departments (id, name) VALUES 
                (1, 'Engineering'), (2, 'Sales'), (3, 'HR');

            INSERT INTO users (id, name, department_id, is_active) VALUES 
                (1, 'Alice', 1, 1),
                (2, 'Bob', 1, 1),
                (3, 'Charlie', 2, 1),
                (4, 'Diana', 2, 0),
                (5, 'Eve', 3, 1);

            INSERT INTO orders (id, user_id, amount) VALUES 
                (1, 1, 100.00),
                (2, 1, 200.00),
                (3, 2, 150.00),
                (4, 3, 300.00),
                (5, 4, 250.00);
        ";
        cmd.ExecuteNonQuery();

        return connection;
    }

    [TestMethod]
    public async Task Exec_FromSubQuery_ReturnsCorrectCount()
    {
        using var conn = CreateTestDatabase();
        using var cmd = conn.CreateCommand();
        
        // FROM 子查询：只选择活跃用户
        cmd.CommandText = "SELECT COUNT(*) FROM (SELECT * FROM users WHERE is_active = 1) AS sq";
        var count = (long)(await cmd.ExecuteScalarAsync())!;
        
        Assert.AreEqual(4, count); // 4 个活跃用户
    }

    [TestMethod]
    public async Task Exec_FromSubQuery_WithOuterWhere_ReturnsCorrectCount()
    {
        using var conn = CreateTestDatabase();
        using var cmd = conn.CreateCommand();
        
        // FROM 子查询 + 外层 WHERE
        cmd.CommandText = "SELECT COUNT(*) FROM (SELECT * FROM users WHERE is_active = 1) AS sq WHERE department_id = 1";
        var count = (long)(await cmd.ExecuteScalarAsync())!;
        
        Assert.AreEqual(2, count); // Engineering 部门有 2 个活跃用户
    }

    [TestMethod]
    public async Task Exec_JoinWithAlias_ReturnsCorrectData()
    {
        using var conn = CreateTestDatabase();
        using var cmd = conn.CreateCommand();
        
        // JOIN 带别名
        cmd.CommandText = @"
            SELECT t1.name, t2.name as dept_name 
            FROM users AS t1 
            INNER JOIN departments AS t2 ON t1.department_id = t2.id 
            WHERE t1.is_active = 1
            ORDER BY t1.name";
        
        using var reader = await cmd.ExecuteReaderAsync();
        var results = new System.Collections.Generic.List<(string Name, string DeptName)>();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.GetString(1)));
        }
        
        Assert.AreEqual(4, results.Count);
        Assert.AreEqual("Alice", results[0].Name);
        Assert.AreEqual("Engineering", results[0].DeptName);
    }

    [TestMethod]
    public async Task Exec_JoinWithSubQuery_ReturnsCorrectData()
    {
        using var conn = CreateTestDatabase();
        using var cmd = conn.CreateCommand();
        
        // JOIN 子查询
        cmd.CommandText = @"
            SELECT t1.name, t2.name as dept_name 
            FROM users AS t1 
            INNER JOIN (SELECT * FROM departments WHERE name <> 'HR') AS t2 ON t1.department_id = t2.id 
            WHERE t1.is_active = 1
            ORDER BY t1.name";
        
        using var reader = await cmd.ExecuteReaderAsync();
        var results = new System.Collections.Generic.List<string>();
        while (await reader.ReadAsync())
        {
            results.Add(reader.GetString(0));
        }
        
        Assert.AreEqual(3, results.Count); // Alice, Bob, Charlie (不包括 HR 部门的 Eve)
    }

    #endregion
}
