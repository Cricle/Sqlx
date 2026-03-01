// -----------------------------------------------------------------------
// <copyright file="ComplexQueryTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using Sqlx.Annotations;

namespace Sqlx.Tests;

/// <summary>
/// 复杂查询场景测试 - 多表JOIN、子查询JOIN、子查询GROUP、GROUP后JOIN等。
/// </summary>
[TestClass]
public class ComplexQueryTests
{
    #region Test Entities

    [Sqlx]
    public partial class CqUser
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int DepartmentId { get; set; }
        public int ManagerId { get; set; }
        public bool IsActive { get; set; }
    }

    [Sqlx]
    public partial class CqDepartment
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int CompanyId { get; set; }
    }

    [Sqlx]
    public partial class CqCompany
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    [Sqlx]
    public partial class CqOrder
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = "";
    }

    [Sqlx]
    public partial class CqProduct
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
        public int CategoryId { get; set; }
    }

    [Sqlx]
    public partial class CqCategory
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    #endregion

    #region Multiple JOIN Tests

    [TestMethod]
    public void MultiJoin_TwoTables_SQLite()
    {
        var users = SqlQuery<CqUser>.ForSqlite();
        var depts = SqlQuery<CqDepartment>.ForSqlite();

        var sql = users
            .Join(depts, u => u.DepartmentId, d => d.Id, (u, d) => new { u.Name, DeptName = d.Name })
            .ToSql();

        Assert.AreEqual(
            "SELECT [name], [name] AS DeptName FROM [CqUser] AS [t1] INNER JOIN [CqDepartment] AS [t2] ON [t1].[department_id] = [t2].[id]",
            sql);
    }

    [TestMethod]
    public void MultiJoin_ThreeTables_SQLite()
    {
        var users = SqlQuery<CqUser>.ForSqlite();
        var depts = SqlQuery<CqDepartment>.ForSqlite();
        var companies = SqlQuery<CqCompany>.ForSqlite();

        var sql = users
            .Join(depts, u => u.DepartmentId, d => d.Id, (u, d) => new { User = u, Dept = d })
            .Join(companies, x => x.Dept.CompanyId, c => c.Id, (x, c) => new { x.User.Name, DeptName = x.Dept.Name, CompanyName = c.Name })
            .ToSql();

        Assert.AreEqual(
            "SELECT [name], [name] AS DeptName, [name] AS CompanyName FROM [CqUser] AS [t1] INNER JOIN [CqDepartment] AS [t2] ON [t1].[department_id] = [t2].[id] INNER JOIN [CqCompany] AS [t3] ON [t2].[company_id] = [t3].[id]",
            sql);
    }

    [TestMethod]
    public void MultiJoin_FourTables_SQLite()
    {
        var users = SqlQuery<CqUser>.ForSqlite();
        var depts = SqlQuery<CqDepartment>.ForSqlite();
        var companies = SqlQuery<CqCompany>.ForSqlite();
        var orders = SqlQuery<CqOrder>.ForSqlite();

        var sql = users
            .Join(depts, u => u.DepartmentId, d => d.Id, (u, d) => new { User = u, Dept = d })
            .Join(companies, x => x.Dept.CompanyId, c => c.Id, (x, c) => new { x.User, x.Dept, Company = c })
            .Join(orders, x => x.User.Id, o => o.UserId, (x, o) => new { UserName = x.User.Name, CompanyName = x.Company.Name, o.Amount })
            .ToSql();

        Assert.AreEqual(
            "SELECT [name] AS UserName, [name] AS CompanyName, [amount] FROM [CqUser] AS [t1] INNER JOIN [CqDepartment] AS [t2] ON [t1].[department_id] = [t2].[id] INNER JOIN [CqCompany] AS [t3] ON [t2].[company_id] = [t3].[id] INNER JOIN [CqOrder] AS [t4] ON [t1].[id] = [t4].[user_id]",
            sql);
    }

    [TestMethod]
    public void MultiJoin_WithWhere_SQLite()
    {
        var users = SqlQuery<CqUser>.ForSqlite();
        var depts = SqlQuery<CqDepartment>.ForSqlite();

        var sql = users
            .Join(depts, u => u.DepartmentId, d => d.Id, (u, d) => new { u.Name, u.IsActive, DeptName = d.Name })
            .Where(x => x.IsActive)
            .ToSql();

        Assert.AreEqual(
            "SELECT [name], [is_active], [name] AS DeptName FROM [CqUser] AS [t1] INNER JOIN [CqDepartment] AS [t2] ON [t1].[department_id] = [t2].[id] WHERE [is_active] = 1",
            sql);
    }

    [TestMethod]
    public void MultiJoin_WithOrderByAndPagination_SQLite()
    {
        var users = SqlQuery<CqUser>.ForSqlite();
        var depts = SqlQuery<CqDepartment>.ForSqlite();

        var sql = users
            .Join(depts, u => u.DepartmentId, d => d.Id, (u, d) => new { u.Name, DeptName = d.Name })
            .OrderBy(x => x.Name)
            .Skip(10)
            .Take(20)
            .ToSql();

        Assert.AreEqual(
            "SELECT [name], [name] AS DeptName FROM [CqUser] AS [t1] INNER JOIN [CqDepartment] AS [t2] ON [t1].[department_id] = [t2].[id] ORDER BY [name] ASC LIMIT 20 OFFSET 10",
            sql);
    }

    #endregion

    #region JOIN with SubQuery Tests

    [TestMethod]
    public void JoinSubQuery_SimpleWhere_SQLite()
    {
        var users = SqlQuery<CqUser>.ForSqlite();
        var activeUsers = SqlQuery<CqUser>.ForSqlite().Where(u => u.IsActive);

        var sql = users
            .Join(activeUsers, u => u.ManagerId, au => au.Id, (u, au) => new { u.Name, ManagerName = au.Name })
            .ToSql();

        Assert.AreEqual(
            "SELECT [name], [name] AS ManagerName FROM [CqUser] AS [t1] INNER JOIN (SELECT [id], [name], [department_id], [manager_id], [is_active] FROM [CqUser] WHERE [is_active] = 1) AS [t2] ON [t1].[manager_id] = [t2].[id]",
            sql);
    }

    [TestMethod]
    public void JoinSubQuery_WithOrderByAndTake_SQLite()
    {
        var users = SqlQuery<CqUser>.ForSqlite();
        var topDepts = SqlQuery<CqDepartment>.ForSqlite().OrderBy(d => d.Name).Take(10);

        var sql = users
            .Join(topDepts, u => u.DepartmentId, d => d.Id, (u, d) => new { u.Name, DeptName = d.Name })
            .ToSql();

        Assert.AreEqual(
            "SELECT [name], [name] AS DeptName FROM [CqUser] AS [t1] INNER JOIN (SELECT [id], [name], [company_id] FROM [CqDepartment] ORDER BY [name] ASC LIMIT 10) AS [t2] ON [t1].[department_id] = [t2].[id]",
            sql);
    }

    [TestMethod]
    public void JoinSubQuery_BothSidesSubQuery_SQLite()
    {
        var activeUsers = SqlQuery<CqUser>.ForSqlite().Where(u => u.IsActive);
        var largeDepts = SqlQuery<CqDepartment>.ForSqlite().Where(d => d.CompanyId > 0);

        var sql = SqlQuery<CqUser>.For(SqlDefine.SQLite, activeUsers)
            .Join(largeDepts, u => u.DepartmentId, d => d.Id, (u, d) => new { u.Name, DeptName = d.Name })
            .ToSql();

        // FROM subquery uses [sq] alias, JOIN adds [t2] alias
        Assert.AreEqual(
            "SELECT [name], [name] AS DeptName FROM (SELECT [id], [name], [department_id], [manager_id], [is_active] FROM [CqUser] WHERE [is_active] = 1) AS [sq] INNER JOIN (SELECT [id], [name], [company_id] FROM [CqDepartment] WHERE [company_id] > 0) AS [t2] ON [sq].[department_id] = [t2].[id]",
            sql);
    }

    [TestMethod]
    public void JoinSubQuery_MultipleJoinsWithSubQuery_SQLite()
    {
        var users = SqlQuery<CqUser>.ForSqlite();
        var activeDepts = SqlQuery<CqDepartment>.ForSqlite().Where(d => d.CompanyId > 0);
        var orders = SqlQuery<CqOrder>.ForSqlite().Where(o => o.Amount > 100);

        var sql = users
            .Join(activeDepts, u => u.DepartmentId, d => d.Id, (u, d) => new { User = u, Dept = d })
            .Join(orders, x => x.User.Id, o => o.UserId, (x, o) => new { UserName = x.User.Name, DeptName = x.Dept.Name, o.Amount })
            .ToSql();

        Assert.AreEqual(
            "SELECT [name] AS UserName, [name] AS DeptName, [amount] FROM [CqUser] AS [t1] INNER JOIN (SELECT [id], [name], [company_id] FROM [CqDepartment] WHERE [company_id] > 0) AS [t2] ON [t1].[department_id] = [t2].[id] INNER JOIN (SELECT [id], [user_id], [amount], [status] FROM [CqOrder] WHERE [amount] > 100) AS [t3] ON [t1].[id] = [t3].[user_id]",
            sql);
    }

    #endregion

    #region FROM SubQuery Tests

    [TestMethod]
    public void FromSubQuery_WithJoin_SQLite()
    {
        var activeUsers = SqlQuery<CqUser>.ForSqlite().Where(u => u.IsActive);
        var depts = SqlQuery<CqDepartment>.ForSqlite();

        var sql = SqlQuery<CqUser>.For(SqlDefine.SQLite, activeUsers)
            .Join(depts, u => u.DepartmentId, d => d.Id, (u, d) => new { u.Name, DeptName = d.Name })
            .ToSql();

        // FROM subquery uses [sq] alias, JOIN adds [t2] alias
        Assert.AreEqual(
            "SELECT [name], [name] AS DeptName FROM (SELECT [id], [name], [department_id], [manager_id], [is_active] FROM [CqUser] WHERE [is_active] = 1) AS [sq] INNER JOIN [CqDepartment] AS [t2] ON [sq].[department_id] = [t2].[id]",
            sql);
    }

    [TestMethod]
    public void FromSubQuery_WithGroupBy_SQLite()
    {
        var activeUsers = SqlQuery<CqUser>.ForSqlite().Where(u => u.IsActive);

        var sql = SqlQuery<CqUser>.For(SqlDefine.SQLite, activeUsers)
            .GroupBy(u => u.DepartmentId)
            .Select(g => new { DeptId = g.Key, Count = g.Count() })
            .ToSql();

        Assert.AreEqual(
            "SELECT [department_id] AS DeptId, COUNT(*) AS Count FROM (SELECT [id], [name], [department_id], [manager_id], [is_active] FROM [CqUser] WHERE [is_active] = 1) AS [sq] GROUP BY [department_id]",
            sql);
    }

    [TestMethod]
    public void FromSubQuery_Nested_SQLite()
    {
        var inner = SqlQuery<CqUser>.ForSqlite().Where(u => u.IsActive);
        var middle = SqlQuery<CqUser>.For(SqlDefine.SQLite, inner).Where(u => u.DepartmentId > 0);
        
        var sql = SqlQuery<CqUser>.For(SqlDefine.SQLite, middle)
            .Select(u => new { u.Id, u.Name })
            .ToSql();

        Assert.AreEqual(
            "SELECT [id], [name] FROM (SELECT [id], [name], [department_id], [manager_id], [is_active] FROM (SELECT [id], [name], [department_id], [manager_id], [is_active] FROM [CqUser] WHERE [is_active] = 1) AS [sq] WHERE [department_id] > 0) AS [sq]",
            sql);
    }

    #endregion

    #region GroupBy with SubQuery Tests

    [TestMethod]
    public void GroupBy_WithScalarSubQuery_SQLite()
    {
        var sql = SqlQuery<CqUser>.ForSqlite()
            .GroupBy(u => u.DepartmentId)
            .Select(g => new { 
                DeptId = g.Key, 
                UserCount = g.Count(),
                TotalOrders = SubQuery.For<CqOrder>().Count()
            })
            .ToSql();

        Assert.AreEqual(
            "SELECT [department_id] AS DeptId, COUNT(*) AS UserCount, (SELECT COUNT(*) FROM (SELECT [id], [user_id], [amount], [status] FROM [CqOrder]) AS sq) AS TotalOrders FROM [CqUser] GROUP BY [department_id]",
            sql);
    }

    [TestMethod]
    public void GroupBy_WithFilteredSubQuery_SQLite()
    {
        var sql = SqlQuery<CqUser>.ForSqlite()
            .GroupBy(u => u.DepartmentId)
            .Select(g => new { 
                DeptId = g.Key, 
                UserCount = g.Count(),
                CompletedOrders = SubQuery.For<CqOrder>().Where(o => o.Status == "completed").Count()
            })
            .ToSql();

        Assert.AreEqual(
            "SELECT [department_id] AS DeptId, COUNT(*) AS UserCount, (SELECT COUNT(*) FROM (SELECT [id], [user_id], [amount], [status] FROM [CqOrder] WHERE [status] = 'completed') AS sq) AS CompletedOrders FROM [CqUser] GROUP BY [department_id]",
            sql);
    }

    [TestMethod]
    public void GroupBy_WithMultipleSubQueries_SQLite()
    {
        var sql = SqlQuery<CqUser>.ForSqlite()
            .GroupBy(u => u.DepartmentId)
            .Select(g => new { 
                DeptId = g.Key,
                PendingOrders = SubQuery.For<CqOrder>().Where(o => o.Status == "pending").Count(),
                CompletedOrders = SubQuery.For<CqOrder>().Where(o => o.Status == "completed").Count(),
                TotalProducts = SubQuery.For<CqProduct>().Count()
            })
            .ToSql();

        Assert.AreEqual(
            "SELECT [department_id] AS DeptId, (SELECT COUNT(*) FROM (SELECT [id], [user_id], [amount], [status] FROM [CqOrder] WHERE [status] = 'pending') AS sq) AS PendingOrders, (SELECT COUNT(*) FROM (SELECT [id], [user_id], [amount], [status] FROM [CqOrder] WHERE [status] = 'completed') AS sq) AS CompletedOrders, (SELECT COUNT(*) FROM (SELECT [id], [name], [price], [category_id] FROM [CqProduct]) AS sq) AS TotalProducts FROM [CqUser] GROUP BY [department_id]",
            sql);
    }

    [TestMethod]
    public void GroupBy_WithSubQueryAny_SQLite()
    {
        var sql = SqlQuery<CqUser>.ForSqlite()
            .GroupBy(u => u.DepartmentId)
            .Select(g => new { 
                DeptId = g.Key,
                HasOrders = SubQuery.For<CqOrder>().Any()
            })
            .ToSql();

        Assert.AreEqual(
            "SELECT [department_id] AS DeptId, (SELECT CASE WHEN EXISTS(SELECT [id], [user_id], [amount], [status] FROM [CqOrder]) THEN 1 ELSE 0 END) AS HasOrders FROM [CqUser] GROUP BY [department_id]",
            sql);
    }

    [TestMethod]
    public void GroupBy_WithSubQueryFirst_SQLite()
    {
        var sql = SqlQuery<CqUser>.ForSqlite()
            .GroupBy(u => u.DepartmentId)
            .Select(g => new { 
                DeptId = g.Key,
                FirstOrder = SubQuery.For<CqOrder>().OrderBy(o => o.Id).First()
            })
            .ToSql();

        Assert.AreEqual(
            "SELECT [department_id] AS DeptId, (SELECT [id], [user_id], [amount], [status] FROM [CqOrder] ORDER BY [id] ASC LIMIT 1) AS FirstOrder FROM [CqUser] GROUP BY [department_id]",
            sql);
    }

    #endregion

    #region Complex Combined Scenarios

    [TestMethod]
    public void Complex_JoinGroupBySubQuery_SQLite()
    {
        var users = SqlQuery<CqUser>.ForSqlite();
        var depts = SqlQuery<CqDepartment>.ForSqlite();

        var sql = users
            .Join(depts, u => u.DepartmentId, d => d.Id, (u, d) => new { u.Id, u.Name, u.DepartmentId, DeptName = d.Name })
            .GroupBy(x => x.DeptName)
            .Select(g => new { 
                DeptName = g.Key, 
                UserCount = g.Count(),
                TotalOrders = SubQuery.For<CqOrder>().Count()
            })
            .ToSql();

        Assert.AreEqual(
            "SELECT [name] AS DeptName, COUNT(*) AS UserCount, (SELECT COUNT(*) FROM (SELECT [id], [user_id], [amount], [status] FROM [CqOrder]) AS sq) AS TotalOrders FROM [CqUser] AS [t1] INNER JOIN [CqDepartment] AS [t2] ON [t1].[department_id] = [t2].[id] GROUP BY [name]",
            sql);
    }

    [TestMethod]
    public void Complex_SubQueryJoinGroupBy_SQLite()
    {
        var activeUsers = SqlQuery<CqUser>.ForSqlite().Where(u => u.IsActive);
        var depts = SqlQuery<CqDepartment>.ForSqlite();

        var sql = SqlQuery<CqUser>.For(SqlDefine.SQLite, activeUsers)
            .Join(depts, u => u.DepartmentId, d => d.Id, (u, d) => new { u.Id, u.Name, DeptName = d.Name })
            .GroupBy(x => x.DeptName)
            .Select(g => new { DeptName = g.Key, Count = g.Count() })
            .ToSql();

        // FROM subquery uses [sq] alias, GroupBy on DeptName (which is d.Name) uses [name]
        Assert.AreEqual(
            "SELECT [name] AS DeptName, COUNT(*) AS Count FROM (SELECT [id], [name], [department_id], [manager_id], [is_active] FROM [CqUser] WHERE [is_active] = 1) AS [sq] INNER JOIN [CqDepartment] AS [t2] ON [sq].[department_id] = [t2].[id] GROUP BY [name]",
            sql);
    }

    [TestMethod]
    public void Complex_WhereJoinGroupByOrderByPagination_SQLite()
    {
        var users = SqlQuery<CqUser>.ForSqlite();
        var depts = SqlQuery<CqDepartment>.ForSqlite();

        var sql = users
            .Where(u => u.IsActive)
            .Join(depts, u => u.DepartmentId, d => d.Id, (u, d) => new { u.Id, u.Name, DeptName = d.Name })
            .GroupBy(x => x.DeptName)
            .Select(g => new { DeptName = g.Key, Count = g.Count() })
            .OrderBy(x => x.Count)
            .Take(10)
            .ToSql();

        Assert.AreEqual(
            "SELECT [name] AS DeptName, COUNT(*) AS Count FROM [CqUser] AS [t1] INNER JOIN [CqDepartment] AS [t2] ON [t1].[department_id] = [t2].[id] WHERE [is_active] = 1 GROUP BY [name] ORDER BY COUNT(*) ASC LIMIT 10",
            sql);
    }

    [TestMethod]
    public void Complex_MultipleSubQueriesInSelect_SQLite()
    {
        var sql = SqlQuery<CqUser>.ForSqlite()
            .Where(u => u.IsActive)
            .Select(u => new { 
                u.Id,
                u.Name,
                OrderCount = SubQuery.For<CqOrder>().Where(o => o.UserId == u.Id).Count(),
                TotalAmount = SubQuery.For<CqOrder>().Where(o => o.UserId == u.Id).Sum(o => o.Amount),
                HasPendingOrders = SubQuery.For<CqOrder>().Where(o => o.UserId == u.Id).Any(o => o.Status == "pending")
            })
            .ToSql();

        Assert.AreEqual(
            "SELECT [id], [name], (SELECT COUNT(*) FROM (SELECT [id], [user_id], [amount], [status] FROM [CqOrder] WHERE [user_id] = [id]) AS sq) AS OrderCount, (SELECT SUM([amount]) FROM (SELECT [id], [user_id], [amount], [status] FROM [CqOrder] WHERE [user_id] = [id]) AS sq) AS TotalAmount, (SELECT CASE WHEN EXISTS(SELECT 1 FROM (SELECT [id], [user_id], [amount], [status] FROM [CqOrder] WHERE [user_id] = [id]) AS sq WHERE [status] = 'pending') THEN 1 ELSE 0 END) AS HasPendingOrders FROM [CqUser] WHERE [is_active] = 1",
            sql);
    }

    #endregion

    #region Cross-Dialect Tests

    [TestMethod]
    public void MultiJoin_SqlServer()
    {
        var users = SqlQuery<CqUser>.ForSqlServer();
        var depts = SqlQuery<CqDepartment>.ForSqlServer();
        var companies = SqlQuery<CqCompany>.ForSqlServer();

        var sql = users
            .Join(depts, u => u.DepartmentId, d => d.Id, (u, d) => new { User = u, Dept = d })
            .Join(companies, x => x.Dept.CompanyId, c => c.Id, (x, c) => new { x.User.Name, CompanyName = c.Name })
            .OrderBy(x => x.Name)
            .Skip(10)
            .Take(20)
            .ToSql();

        Assert.AreEqual(
            "SELECT [name], [name] AS CompanyName FROM [CqUser] AS [t1] INNER JOIN [CqDepartment] AS [t2] ON [t1].[department_id] = [t2].[id] INNER JOIN [CqCompany] AS [t3] ON [t2].[company_id] = [t3].[id] ORDER BY [name] ASC OFFSET 10 ROWS FETCH NEXT 20 ROWS ONLY",
            sql);
    }

    [TestMethod]
    public void MultiJoin_MySQL()
    {
        var users = SqlQuery<CqUser>.ForMySql();
        var depts = SqlQuery<CqDepartment>.ForMySql();

        var sql = users
            .Join(depts, u => u.DepartmentId, d => d.Id, (u, d) => new { u.Name, DeptName = d.Name })
            .Where(x => x.Name != "test")
            .ToSql();

        Assert.AreEqual(
            "SELECT `name`, `name` AS DeptName FROM `CqUser` AS `t1` INNER JOIN `CqDepartment` AS `t2` ON `t1`.`department_id` = `t2`.`id` WHERE `name` <> 'test'",
            sql);
    }

    [TestMethod]
    public void MultiJoin_PostgreSQL()
    {
        var users = SqlQuery<CqUser>.ForPostgreSQL();
        var depts = SqlQuery<CqDepartment>.ForPostgreSQL();

        var sql = users
            .Join(depts, u => u.DepartmentId, d => d.Id, (u, d) => new { u.Name, u.IsActive, DeptName = d.Name })
            .Where(x => x.IsActive)
            .ToSql();

        Assert.AreEqual(
            "SELECT \"name\", \"is_active\", \"name\" AS DeptName FROM \"CqUser\" AS \"t1\" INNER JOIN \"CqDepartment\" AS \"t2\" ON \"t1\".\"department_id\" = \"t2\".\"id\" WHERE \"is_active\" = true",
            sql);
    }

    [TestMethod]
    public void SubQueryInSelect_AllDialects()
    {
        // SQLite
        var sqliteSql = SqlQuery<CqUser>.ForSqlite()
            .Select(u => new { u.Id, OrderCount = SubQuery.For<CqOrder>().Count() })
            .ToSql();
        Assert.AreEqual(
            "SELECT [id], (SELECT COUNT(*) FROM (SELECT [id], [user_id], [amount], [status] FROM [CqOrder]) AS sq) AS OrderCount FROM [CqUser]",
            sqliteSql);

        // MySQL
        var mysqlSql = SqlQuery<CqUser>.ForMySql()
            .Select(u => new { u.Id, OrderCount = SubQuery.For<CqOrder>().Count() })
            .ToSql();
        Assert.AreEqual(
            "SELECT `id`, (SELECT COUNT(*) FROM (SELECT `id`, `user_id`, `amount`, `status` FROM `CqOrder`) AS sq) AS OrderCount FROM `CqUser`",
            mysqlSql);

        // PostgreSQL
        var pgSql = SqlQuery<CqUser>.ForPostgreSQL()
            .Select(u => new { u.Id, OrderCount = SubQuery.For<CqOrder>().Count() })
            .ToSql();
        Assert.AreEqual(
            "SELECT \"id\", (SELECT COUNT(*) FROM (SELECT \"id\", \"user_id\", \"amount\", \"status\" FROM \"CqOrder\") AS sq) AS OrderCount FROM \"CqUser\"",
            pgSql);
    }

    #endregion
}
