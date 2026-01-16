// -----------------------------------------------------------------------
// <copyright file="SubQueryComprehensiveTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Sqlx.Tests;

/// <summary>
/// SubQuery 全面测试 - 所有数据库方言、多功能组合、复杂场景、边界情况。
/// </summary>
[TestClass]
public class SubQueryComprehensiveTests
{
    #region Test Entities

    [Sqlx]
    public partial class SqUser
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int DepartmentId { get; set; }
        public int Age { get; set; }
        public bool IsActive { get; set; }
        public decimal Salary { get; set; }
    }

    [Sqlx]
    public partial class SqOrder
    {
        [Key]
        public int Id { get; set; }
        public int UserId { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = "";
        public int Quantity { get; set; }
    }

    [Sqlx]
    public partial class SqDepartment
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    #endregion

    #region SQLite Dialect Tests

    [TestMethod]
    public void SQLite_SimpleCount()
    {
        var sql = SqlQuery<SqUser>.ForSqlite()
            .Select(x => new { x.Id, Total = SubQuery.For<SqOrder>().Count() })
            .ToSql();

        Assert.AreEqual(
            "SELECT [id], (SELECT COUNT(*) FROM (SELECT * FROM [SqOrder]) AS sq) AS Total FROM [SqUser]",
            sql);
    }

    [TestMethod]
    public void SQLite_CountWithWhere()
    {
        var sql = SqlQuery<SqUser>.ForSqlite()
            .Select(x => new { x.Id, Active = SubQuery.For<SqOrder>().Where(o => o.Status == "active").Count() })
            .ToSql();

        Assert.AreEqual(
            "SELECT [id], (SELECT COUNT(*) FROM (SELECT * FROM [SqOrder] WHERE [status] = 'active') AS sq) AS Active FROM [SqUser]",
            sql);
    }

    [TestMethod]
    public void SQLite_CountWithNumericComparison()
    {
        var sql = SqlQuery<SqUser>.ForSqlite()
            .Select(x => new { x.Id, Big = SubQuery.For<SqOrder>().Where(o => o.Amount > 100).Count() })
            .ToSql();

        Assert.AreEqual(
            "SELECT [id], (SELECT COUNT(*) FROM (SELECT * FROM [SqOrder] WHERE [amount] > 100) AS sq) AS Big FROM [SqUser]",
            sql);
    }

    [TestMethod]
    public void SQLite_CountWithAndCondition()
    {
        var sql = SqlQuery<SqUser>.ForSqlite()
            .Select(x => new { x.Id, Filtered = SubQuery.For<SqOrder>().Where(o => o.Amount > 100 && o.Status == "completed").Count() })
            .ToSql();

        Assert.AreEqual(
            "SELECT [id], (SELECT COUNT(*) FROM (SELECT * FROM [SqOrder] WHERE ([amount] > 100 AND [status] = 'completed')) AS sq) AS Filtered FROM [SqUser]",
            sql);
    }

    [TestMethod]
    public void SQLite_CountWithOrCondition()
    {
        var sql = SqlQuery<SqUser>.ForSqlite()
            .Select(x => new { x.Id, Either = SubQuery.For<SqOrder>().Where(o => o.Status == "pending" || o.Status == "processing").Count() })
            .ToSql();

        Assert.AreEqual(
            "SELECT [id], (SELECT COUNT(*) FROM (SELECT * FROM [SqOrder] WHERE ([status] = 'pending' OR [status] = 'processing')) AS sq) AS Either FROM [SqUser]",
            sql);
    }

    [TestMethod]
    public void SQLite_MultipleSubQueries()
    {
        var sql = SqlQuery<SqUser>.ForSqlite()
            .Select(x => new { 
                x.Id, 
                Users = SubQuery.For<SqUser>().Count(),
                Orders = SubQuery.For<SqOrder>().Count(),
                Depts = SubQuery.For<SqDepartment>().Count()
            })
            .ToSql();

        Assert.AreEqual(
            "SELECT [id], (SELECT COUNT(*) FROM (SELECT * FROM [SqUser]) AS sq) AS Users, (SELECT COUNT(*) FROM (SELECT * FROM [SqOrder]) AS sq) AS Orders, (SELECT COUNT(*) FROM (SELECT * FROM [SqDepartment]) AS sq) AS Depts FROM [SqUser]",
            sql);
    }

    [TestMethod]
    public void SQLite_WithMainQueryWhere()
    {
        var sql = SqlQuery<SqUser>.ForSqlite()
            .Where(x => x.IsActive)
            .Select(x => new { x.Id, Total = SubQuery.For<SqOrder>().Count() })
            .ToSql();

        Assert.AreEqual(
            "SELECT [id], (SELECT COUNT(*) FROM (SELECT * FROM [SqOrder]) AS sq) AS Total FROM [SqUser] WHERE [is_active] = 1",
            sql);
    }

    [TestMethod]
    public void SQLite_WithMainQueryWhereAndSubQueryWhere()
    {
        var sql = SqlQuery<SqUser>.ForSqlite()
            .Where(x => x.Age > 18)
            .Select(x => new { x.Id, Active = SubQuery.For<SqOrder>().Where(o => o.Status == "active").Count() })
            .ToSql();

        Assert.AreEqual(
            "SELECT [id], (SELECT COUNT(*) FROM (SELECT * FROM [SqOrder] WHERE [status] = 'active') AS sq) AS Active FROM [SqUser] WHERE [age] > 18",
            sql);
    }

    [TestMethod]
    public void SQLite_ChainedWhereInSubQuery()
    {
        var sql = SqlQuery<SqUser>.ForSqlite()
            .Select(x => new { 
                x.Id, 
                Filtered = SubQuery.For<SqOrder>()
                    .Where(o => o.Amount > 100)
                    .Where(o => o.Status == "completed")
                    .Count() 
            })
            .ToSql();

        Assert.AreEqual(
            "SELECT [id], (SELECT COUNT(*) FROM (SELECT * FROM [SqOrder] WHERE [amount] > 100 AND [status] = 'completed') AS sq) AS Filtered FROM [SqUser]",
            sql);
    }

    #endregion

    #region SQL Server Dialect Tests

    [TestMethod]
    public void SqlServer_SimpleCount()
    {
        var sql = SqlQuery<SqUser>.ForSqlServer()
            .Select(x => new { x.Id, Total = SubQuery.For<SqOrder>().Count() })
            .ToSql();

        Assert.AreEqual(
            "SELECT [id], (SELECT COUNT(*) FROM (SELECT * FROM [SqOrder]) AS sq) AS Total FROM [SqUser]",
            sql);
    }

    [TestMethod]
    public void SqlServer_CountWithWhere()
    {
        var sql = SqlQuery<SqUser>.ForSqlServer()
            .Select(x => new { x.Id, Active = SubQuery.For<SqOrder>().Where(o => o.Status == "active").Count() })
            .ToSql();

        Assert.AreEqual(
            "SELECT [id], (SELECT COUNT(*) FROM (SELECT * FROM [SqOrder] WHERE [status] = 'active') AS sq) AS Active FROM [SqUser]",
            sql);
    }

    [TestMethod]
    public void SqlServer_MultipleSubQueries()
    {
        var sql = SqlQuery<SqUser>.ForSqlServer()
            .Select(x => new { 
                x.Id, 
                Users = SubQuery.For<SqUser>().Count(),
                Orders = SubQuery.For<SqOrder>().Count()
            })
            .ToSql();

        Assert.AreEqual(
            "SELECT [id], (SELECT COUNT(*) FROM (SELECT * FROM [SqUser]) AS sq) AS Users, (SELECT COUNT(*) FROM (SELECT * FROM [SqOrder]) AS sq) AS Orders FROM [SqUser]",
            sql);
    }

    [TestMethod]
    public void SqlServer_WithPagination()
    {
        var sql = SqlQuery<SqUser>.ForSqlServer()
            .OrderBy(x => x.Id)
            .Skip(10)
            .Take(20)
            .Select(x => new { x.Id, Total = SubQuery.For<SqOrder>().Count() })
            .ToSql();

        Assert.IsTrue(sql.Contains("OFFSET 10 ROWS"));
        Assert.IsTrue(sql.Contains("FETCH NEXT 20 ROWS ONLY"));
        Assert.IsTrue(sql.Contains("(SELECT COUNT(*)"));
    }

    #endregion

    #region MySQL Dialect Tests

    [TestMethod]
    public void MySql_SimpleCount()
    {
        var sql = SqlQuery<SqUser>.ForMySql()
            .Select(x => new { x.Id, Total = SubQuery.For<SqOrder>().Count() })
            .ToSql();

        Assert.AreEqual(
            "SELECT `id`, (SELECT COUNT(*) FROM (SELECT * FROM `SqOrder`) AS sq) AS Total FROM `SqUser`",
            sql);
    }

    [TestMethod]
    public void MySql_CountWithWhere()
    {
        var sql = SqlQuery<SqUser>.ForMySql()
            .Select(x => new { x.Id, Active = SubQuery.For<SqOrder>().Where(o => o.Status == "active").Count() })
            .ToSql();

        Assert.AreEqual(
            "SELECT `id`, (SELECT COUNT(*) FROM (SELECT * FROM `SqOrder` WHERE `status` = 'active') AS sq) AS Active FROM `SqUser`",
            sql);
    }

    [TestMethod]
    public void MySql_MultipleSubQueries()
    {
        var sql = SqlQuery<SqUser>.ForMySql()
            .Select(x => new { 
                x.Id, 
                Users = SubQuery.For<SqUser>().Count(),
                Orders = SubQuery.For<SqOrder>().Count()
            })
            .ToSql();

        Assert.AreEqual(
            "SELECT `id`, (SELECT COUNT(*) FROM (SELECT * FROM `SqUser`) AS sq) AS Users, (SELECT COUNT(*) FROM (SELECT * FROM `SqOrder`) AS sq) AS Orders FROM `SqUser`",
            sql);
    }

    [TestMethod]
    public void MySql_WithPagination()
    {
        var sql = SqlQuery<SqUser>.ForMySql()
            .Skip(10)
            .Take(20)
            .Select(x => new { x.Id, Total = SubQuery.For<SqOrder>().Count() })
            .ToSql();

        Assert.IsTrue(sql.Contains("LIMIT 20"));
        Assert.IsTrue(sql.Contains("OFFSET 10"));
        Assert.IsTrue(sql.Contains("(SELECT COUNT(*)"));
    }

    #endregion

    #region PostgreSQL Dialect Tests

    [TestMethod]
    public void PostgreSQL_SimpleCount()
    {
        var sql = SqlQuery<SqUser>.ForPostgreSQL()
            .Select(x => new { x.Id, Total = SubQuery.For<SqOrder>().Count() })
            .ToSql();

        Assert.AreEqual(
            "SELECT \"id\", (SELECT COUNT(*) FROM (SELECT * FROM \"SqOrder\") AS sq) AS Total FROM \"SqUser\"",
            sql);
    }

    [TestMethod]
    public void PostgreSQL_CountWithWhere()
    {
        var sql = SqlQuery<SqUser>.ForPostgreSQL()
            .Select(x => new { x.Id, Active = SubQuery.For<SqOrder>().Where(o => o.Status == "active").Count() })
            .ToSql();

        Assert.AreEqual(
            "SELECT \"id\", (SELECT COUNT(*) FROM (SELECT * FROM \"SqOrder\" WHERE \"status\" = 'active') AS sq) AS Active FROM \"SqUser\"",
            sql);
    }

    [TestMethod]
    public void PostgreSQL_BooleanCondition()
    {
        var sql = SqlQuery<SqUser>.ForPostgreSQL()
            .Where(x => x.IsActive)
            .Select(x => new { x.Id, Total = SubQuery.For<SqOrder>().Count() })
            .ToSql();

        Assert.AreEqual(
            "SELECT \"id\", (SELECT COUNT(*) FROM (SELECT * FROM \"SqOrder\") AS sq) AS Total FROM \"SqUser\" WHERE \"is_active\" = true",
            sql);
    }

    [TestMethod]
    public void PostgreSQL_MultipleSubQueries()
    {
        var sql = SqlQuery<SqUser>.ForPostgreSQL()
            .Select(x => new { 
                x.Id, 
                Users = SubQuery.For<SqUser>().Count(),
                Orders = SubQuery.For<SqOrder>().Count()
            })
            .ToSql();

        Assert.AreEqual(
            "SELECT \"id\", (SELECT COUNT(*) FROM (SELECT * FROM \"SqUser\") AS sq) AS Users, (SELECT COUNT(*) FROM (SELECT * FROM \"SqOrder\") AS sq) AS Orders FROM \"SqUser\"",
            sql);
    }

    #endregion

    #region Oracle Dialect Tests

    [TestMethod]
    public void Oracle_SimpleCount()
    {
        var sql = SqlQuery<SqUser>.ForOracle()
            .Select(x => new { x.Id, Total = SubQuery.For<SqOrder>().Count() })
            .ToSql();

        Assert.AreEqual(
            "SELECT \"id\", (SELECT COUNT(*) FROM (SELECT * FROM \"SqOrder\") AS sq) AS Total FROM \"SqUser\"",
            sql);
    }

    [TestMethod]
    public void Oracle_CountWithWhere()
    {
        var sql = SqlQuery<SqUser>.ForOracle()
            .Select(x => new { x.Id, Active = SubQuery.For<SqOrder>().Where(o => o.Status == "active").Count() })
            .ToSql();

        Assert.AreEqual(
            "SELECT \"id\", (SELECT COUNT(*) FROM (SELECT * FROM \"SqOrder\" WHERE \"status\" = 'active') AS sq) AS Active FROM \"SqUser\"",
            sql);
    }

    [TestMethod]
    public void Oracle_WithPagination()
    {
        var sql = SqlQuery<SqUser>.ForOracle()
            .OrderBy(x => x.Id)
            .Skip(10)
            .Take(20)
            .Select(x => new { x.Id, Total = SubQuery.For<SqOrder>().Count() })
            .ToSql();

        Assert.IsTrue(sql.Contains("OFFSET 10 ROWS"));
        Assert.IsTrue(sql.Contains("FETCH NEXT 20 ROWS ONLY"));
        Assert.IsTrue(sql.Contains("(SELECT COUNT(*)"));
    }

    #endregion

    #region DB2 Dialect Tests

    [TestMethod]
    public void DB2_SimpleCount()
    {
        var sql = SqlQuery<SqUser>.ForDB2()
            .Select(x => new { x.Id, Total = SubQuery.For<SqOrder>().Count() })
            .ToSql();

        Assert.AreEqual(
            "SELECT \"id\", (SELECT COUNT(*) FROM (SELECT * FROM \"SqOrder\") AS sq) AS Total FROM \"SqUser\"",
            sql);
    }

    [TestMethod]
    public void DB2_CountWithWhere()
    {
        var sql = SqlQuery<SqUser>.ForDB2()
            .Select(x => new { x.Id, Active = SubQuery.For<SqOrder>().Where(o => o.Status == "active").Count() })
            .ToSql();

        Assert.AreEqual(
            "SELECT \"id\", (SELECT COUNT(*) FROM (SELECT * FROM \"SqOrder\" WHERE \"status\" = 'active') AS sq) AS Active FROM \"SqUser\"",
            sql);
    }

    #endregion

    #region Complex Combination Tests

    [TestMethod]
    public void Complex_WhereOrderByTakeWithSubQuery()
    {
        var sql = SqlQuery<SqUser>.ForSqlite()
            .Where(x => x.IsActive && x.Age > 18)
            .OrderBy(x => x.Name)
            .Take(10)
            .Select(x => new { x.Id, x.Name, Orders = SubQuery.For<SqOrder>().Count() })
            .ToSql();

        Assert.AreEqual(
            "SELECT [id], [name], (SELECT COUNT(*) FROM (SELECT * FROM [SqOrder]) AS sq) AS Orders FROM [SqUser] WHERE ([is_active] = 1 AND [age] > 18) ORDER BY [name] ASC LIMIT 10",
            sql);
    }

    [TestMethod]
    public void Complex_GroupByWithSubQuery()
    {
        var sql = SqlQuery<SqUser>.ForSqlite()
            .GroupBy(x => x.DepartmentId)
            .Select(x => new { 
                DeptId = x.Key, 
                UserCount = x.Count(),
                TotalOrders = SubQuery.For<SqOrder>().Count()
            })
            .ToSql();

        Assert.AreEqual(
            "SELECT [department_id] AS DeptId, COUNT(*) AS UserCount, (SELECT COUNT(*) FROM (SELECT * FROM [SqOrder]) AS sq) AS TotalOrders FROM [SqUser] GROUP BY [department_id]",
            sql);
    }

    [TestMethod]
    public void Complex_GroupByWithWhereAndSubQuery()
    {
        var sql = SqlQuery<SqUser>.ForSqlite()
            .Where(x => x.IsActive)
            .GroupBy(x => x.DepartmentId)
            .Select(x => new { 
                DeptId = x.Key, 
                Count = x.Count(),
                ActiveOrders = SubQuery.For<SqOrder>().Where(o => o.Status == "active").Count()
            })
            .ToSql();

        Assert.AreEqual(
            "SELECT [department_id] AS DeptId, COUNT(*) AS Count, (SELECT COUNT(*) FROM (SELECT * FROM [SqOrder] WHERE [status] = 'active') AS sq) AS ActiveOrders FROM [SqUser] WHERE [is_active] = 1 GROUP BY [department_id]",
            sql);
    }

    [TestMethod]
    public void Complex_DistinctWithSubQuery()
    {
        var sql = SqlQuery<SqUser>.ForSqlite()
            .Select(x => x.DepartmentId)
            .Distinct()
            .ToSql();

        Assert.AreEqual(
            "SELECT DISTINCT [department_id] FROM [SqUser]",
            sql);
    }

    [TestMethod]
    public void Complex_MultipleWhereWithMultipleSubQueries()
    {
        var sql = SqlQuery<SqUser>.ForSqlite()
            .Where(x => x.IsActive)
            .Where(x => x.Age >= 18)
            .Where(x => x.Salary > 50000)
            .Select(x => new { 
                x.Id, 
                x.Name,
                PendingOrders = SubQuery.For<SqOrder>().Where(o => o.Status == "pending").Count(),
                CompletedOrders = SubQuery.For<SqOrder>().Where(o => o.Status == "completed").Count(),
                TotalDepts = SubQuery.For<SqDepartment>().Count()
            })
            .ToSql();

        Assert.AreEqual(
            "SELECT [id], [name], (SELECT COUNT(*) FROM (SELECT * FROM [SqOrder] WHERE [status] = 'pending') AS sq) AS PendingOrders, (SELECT COUNT(*) FROM (SELECT * FROM [SqOrder] WHERE [status] = 'completed') AS sq) AS CompletedOrders, (SELECT COUNT(*) FROM (SELECT * FROM [SqDepartment]) AS sq) AS TotalDepts FROM [SqUser] WHERE [is_active] = 1 AND [age] >= 18 AND [salary] > 50000",
            sql);
    }

    [TestMethod]
    public void Complex_OrderByDescendingWithSubQuery()
    {
        var sql = SqlQuery<SqUser>.ForSqlite()
            .OrderByDescending(x => x.Age)
            .ThenBy(x => x.Name)
            .Select(x => new { x.Id, Total = SubQuery.For<SqOrder>().Count() })
            .ToSql();

        Assert.AreEqual(
            "SELECT [id], (SELECT COUNT(*) FROM (SELECT * FROM [SqOrder]) AS sq) AS Total FROM [SqUser] ORDER BY [age] DESC, [name] ASC",
            sql);
    }

    [TestMethod]
    public void Complex_SkipTakeWithSubQuery()
    {
        var sql = SqlQuery<SqUser>.ForSqlite()
            .Skip(20)
            .Take(10)
            .Select(x => new { x.Id, Total = SubQuery.For<SqOrder>().Count() })
            .ToSql();

        Assert.AreEqual(
            "SELECT [id], (SELECT COUNT(*) FROM (SELECT * FROM [SqOrder]) AS sq) AS Total FROM [SqUser] LIMIT 10 OFFSET 20",
            sql);
    }

    #endregion

    #region SubQuery Where Condition Tests

    [TestMethod]
    public void SubQueryWhere_GreaterThan()
    {
        var sql = SqlQuery<SqUser>.ForSqlite()
            .Select(x => new { x.Id, Big = SubQuery.For<SqOrder>().Where(o => o.Amount > 1000).Count() })
            .ToSql();

        Assert.AreEqual(
            "SELECT [id], (SELECT COUNT(*) FROM (SELECT * FROM [SqOrder] WHERE [amount] > 1000) AS sq) AS Big FROM [SqUser]",
            sql);
    }

    [TestMethod]
    public void SubQueryWhere_GreaterThanOrEqual()
    {
        var sql = SqlQuery<SqUser>.ForSqlite()
            .Select(x => new { x.Id, Count = SubQuery.For<SqOrder>().Where(o => o.Quantity >= 5).Count() })
            .ToSql();

        Assert.AreEqual(
            "SELECT [id], (SELECT COUNT(*) FROM (SELECT * FROM [SqOrder] WHERE [quantity] >= 5) AS sq) AS Count FROM [SqUser]",
            sql);
    }

    [TestMethod]
    public void SubQueryWhere_LessThan()
    {
        var sql = SqlQuery<SqUser>.ForSqlite()
            .Select(x => new { x.Id, Small = SubQuery.For<SqOrder>().Where(o => o.Amount < 50).Count() })
            .ToSql();

        Assert.AreEqual(
            "SELECT [id], (SELECT COUNT(*) FROM (SELECT * FROM [SqOrder] WHERE [amount] < 50) AS sq) AS Small FROM [SqUser]",
            sql);
    }

    [TestMethod]
    public void SubQueryWhere_LessThanOrEqual()
    {
        var sql = SqlQuery<SqUser>.ForSqlite()
            .Select(x => new { x.Id, Count = SubQuery.For<SqOrder>().Where(o => o.Quantity <= 10).Count() })
            .ToSql();

        Assert.AreEqual(
            "SELECT [id], (SELECT COUNT(*) FROM (SELECT * FROM [SqOrder] WHERE [quantity] <= 10) AS sq) AS Count FROM [SqUser]",
            sql);
    }

    [TestMethod]
    public void SubQueryWhere_NotEqual()
    {
        var sql = SqlQuery<SqUser>.ForSqlite()
            .Select(x => new { x.Id, NotCancelled = SubQuery.For<SqOrder>().Where(o => o.Status != "cancelled").Count() })
            .ToSql();

        Assert.AreEqual(
            "SELECT [id], (SELECT COUNT(*) FROM (SELECT * FROM [SqOrder] WHERE [status] <> 'cancelled') AS sq) AS NotCancelled FROM [SqUser]",
            sql);
    }

    [TestMethod]
    public void SubQueryWhere_ComplexAnd()
    {
        var sql = SqlQuery<SqUser>.ForSqlite()
            .Select(x => new { 
                x.Id, 
                Filtered = SubQuery.For<SqOrder>()
                    .Where(o => o.Amount > 100 && o.Amount < 1000 && o.Status == "completed")
                    .Count() 
            })
            .ToSql();

        Assert.AreEqual(
            "SELECT [id], (SELECT COUNT(*) FROM (SELECT * FROM [SqOrder] WHERE (([amount] > 100 AND [amount] < 1000) AND [status] = 'completed')) AS sq) AS Filtered FROM [SqUser]",
            sql);
    }

    [TestMethod]
    public void SubQueryWhere_ComplexOr()
    {
        var sql = SqlQuery<SqUser>.ForSqlite()
            .Select(x => new { 
                x.Id, 
                Multiple = SubQuery.For<SqOrder>()
                    .Where(o => o.Status == "pending" || o.Status == "processing" || o.Status == "shipped")
                    .Count() 
            })
            .ToSql();

        Assert.AreEqual(
            "SELECT [id], (SELECT COUNT(*) FROM (SELECT * FROM [SqOrder] WHERE (([status] = 'pending' OR [status] = 'processing') OR [status] = 'shipped')) AS sq) AS Multiple FROM [SqUser]",
            sql);
    }

    [TestMethod]
    public void SubQueryWhere_MixedAndOr()
    {
        var sql = SqlQuery<SqUser>.ForSqlite()
            .Select(x => new { 
                x.Id, 
                Mixed = SubQuery.For<SqOrder>()
                    .Where(o => (o.Status == "completed" || o.Status == "shipped") && o.Amount > 100)
                    .Count() 
            })
            .ToSql();

        Assert.AreEqual(
            "SELECT [id], (SELECT COUNT(*) FROM (SELECT * FROM [SqOrder] WHERE (([status] = 'completed' OR [status] = 'shipped') AND [amount] > 100)) AS sq) AS Mixed FROM [SqUser]",
            sql);
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    public void Edge_EmptyMainQueryCondition()
    {
        var sql = SqlQuery<SqUser>.ForSqlite()
            .Where(x => x.Id < 0)
            .Select(x => new { x.Id, Total = SubQuery.For<SqOrder>().Count() })
            .ToSql();

        Assert.AreEqual(
            "SELECT [id], (SELECT COUNT(*) FROM (SELECT * FROM [SqOrder]) AS sq) AS Total FROM [SqUser] WHERE [id] < 0",
            sql);
    }

    [TestMethod]
    public void Edge_OnlySubQueryInSelect()
    {
        var sql = SqlQuery<SqUser>.ForSqlite()
            .Select(x => new { Total = SubQuery.For<SqOrder>().Count() })
            .ToSql();

        Assert.AreEqual(
            "SELECT (SELECT COUNT(*) FROM (SELECT * FROM [SqOrder]) AS sq) AS Total FROM [SqUser]",
            sql);
    }

    [TestMethod]
    public void Edge_SubQueryWithTakeInSubQuery()
    {
        var sql = SqlQuery<SqUser>.ForSqlite()
            .Select(x => new { x.Id, Limited = SubQuery.For<SqOrder>().Take(100).Count() })
            .ToSql();

        Assert.AreEqual(
            "SELECT [id], (SELECT COUNT(*) FROM (SELECT * FROM [SqOrder] LIMIT 100) AS sq) AS Limited FROM [SqUser]",
            sql);
    }

    [TestMethod]
    public void Edge_SubQueryWithSkipInSubQuery()
    {
        var sql = SqlQuery<SqUser>.ForSqlite()
            .Select(x => new { x.Id, Skipped = SubQuery.For<SqOrder>().Skip(10).Count() })
            .ToSql();

        Assert.AreEqual(
            "SELECT [id], (SELECT COUNT(*) FROM (SELECT * FROM [SqOrder] OFFSET 10) AS sq) AS Skipped FROM [SqUser]",
            sql);
    }

    [TestMethod]
    public void Edge_SubQueryWithOrderByInSubQuery()
    {
        var sql = SqlQuery<SqUser>.ForSqlite()
            .Select(x => new { x.Id, Ordered = SubQuery.For<SqOrder>().OrderBy(o => o.Amount).Count() })
            .ToSql();

        Assert.AreEqual(
            "SELECT [id], (SELECT COUNT(*) FROM (SELECT * FROM [SqOrder] ORDER BY [amount] ASC) AS sq) AS Ordered FROM [SqUser]",
            sql);
    }

    [TestMethod]
    public void Edge_SubQueryWithDistinctInSubQuery()
    {
        var sql = SqlQuery<SqUser>.ForSqlite()
            .Select(x => new { x.Id, Distinct = SubQuery.For<SqOrder>().Select(o => o.UserId).Distinct().Count() })
            .ToSql();

        Assert.AreEqual(
            "SELECT [id], (SELECT COUNT(*) FROM (SELECT DISTINCT [user_id] FROM [SqOrder]) AS sq) AS Distinct FROM [SqUser]",
            sql);
    }

    [TestMethod]
    public void Edge_ManySubQueries()
    {
        var sql = SqlQuery<SqUser>.ForSqlite()
            .Select(x => new { 
                x.Id,
                A = SubQuery.For<SqOrder>().Where(o => o.Status == "a").Count(),
                B = SubQuery.For<SqOrder>().Where(o => o.Status == "b").Count(),
                C = SubQuery.For<SqOrder>().Where(o => o.Status == "c").Count(),
                D = SubQuery.For<SqOrder>().Where(o => o.Status == "d").Count(),
                E = SubQuery.For<SqOrder>().Where(o => o.Status == "e").Count()
            })
            .ToSql();

        Assert.IsTrue(sql.Contains("AS A"));
        Assert.IsTrue(sql.Contains("AS B"));
        Assert.IsTrue(sql.Contains("AS C"));
        Assert.IsTrue(sql.Contains("AS D"));
        Assert.IsTrue(sql.Contains("AS E"));
        Assert.AreEqual(5, sql.Split("SELECT COUNT(*)").Length - 1);
    }

    [TestMethod]
    public void Edge_SubQuerySameTableAsMain()
    {
        var sql = SqlQuery<SqUser>.ForSqlite()
            .Select(x => new { x.Id, TotalUsers = SubQuery.For<SqUser>().Count() })
            .ToSql();

        Assert.AreEqual(
            "SELECT [id], (SELECT COUNT(*) FROM (SELECT * FROM [SqUser]) AS sq) AS TotalUsers FROM [SqUser]",
            sql);
    }

    [TestMethod]
    public void Edge_SubQueryWithBooleanFalse()
    {
        var sql = SqlQuery<SqUser>.ForSqlite()
            .Select(x => new { x.Id, Inactive = SubQuery.For<SqUser>().Where(u => u.IsActive == false).Count() })
            .ToSql();

        Assert.AreEqual(
            "SELECT [id], (SELECT COUNT(*) FROM (SELECT * FROM [SqUser] WHERE [is_active] = 0) AS sq) AS Inactive FROM [SqUser]",
            sql);
    }

    [TestMethod]
    public void Edge_SubQueryWithNegation()
    {
        var sql = SqlQuery<SqUser>.ForSqlite()
            .Select(x => new { x.Id, NotActive = SubQuery.For<SqUser>().Where(u => !u.IsActive).Count() })
            .ToSql();

        Assert.AreEqual(
            "SELECT [id], (SELECT COUNT(*) FROM (SELECT * FROM [SqUser] WHERE [is_active] = 0) AS sq) AS NotActive FROM [SqUser]",
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
                is_active INTEGER NOT NULL,
                salary REAL NOT NULL
            );

            CREATE TABLE departments (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL
            );

            CREATE TABLE orders (
                id INTEGER PRIMARY KEY,
                user_id INTEGER NOT NULL,
                amount REAL NOT NULL,
                status TEXT NOT NULL,
                quantity INTEGER NOT NULL
            );

            INSERT INTO departments (id, name) VALUES 
                (1, 'Engineering'), (2, 'Sales'), (3, 'HR');

            INSERT INTO users (id, name, department_id, age, is_active, salary) VALUES 
                (1, 'Alice', 1, 30, 1, 80000),
                (2, 'Bob', 1, 25, 1, 60000),
                (3, 'Charlie', 2, 35, 1, 70000),
                (4, 'Diana', 2, 28, 0, 55000),
                (5, 'Eve', 3, 40, 1, 90000);

            INSERT INTO orders (id, user_id, amount, status, quantity) VALUES 
                (1, 1, 100.00, 'completed', 2),
                (2, 1, 200.00, 'completed', 1),
                (3, 2, 150.00, 'pending', 3),
                (4, 3, 300.00, 'completed', 1),
                (5, 3, 50.00, 'cancelled', 5),
                (6, 4, 250.00, 'completed', 2),
                (7, 5, 175.00, 'pending', 1),
                (8, 1, 500.00, 'shipped', 4),
                (9, 2, 75.00, 'processing', 2),
                (10, 3, 1000.00, 'completed', 10);
        ";
        cmd.ExecuteNonQuery();

        return connection;
    }

    [TestMethod]
    public async Task Exec_SimpleCount()
    {
        using var conn = CreateTestDatabase();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, (SELECT COUNT(*) FROM (SELECT * FROM orders) AS sq) AS Total FROM users";
        
        using var reader = await cmd.ExecuteReaderAsync();
        int count = 0;
        while (await reader.ReadAsync())
        {
            Assert.AreEqual(10, reader.GetInt32(1)); // 10 orders total
            count++;
        }
        Assert.AreEqual(5, count); // 5 users
    }

    [TestMethod]
    public async Task Exec_CountWithWhere()
    {
        using var conn = CreateTestDatabase();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, (SELECT COUNT(*) FROM (SELECT * FROM orders WHERE status = 'completed') AS sq) AS Completed FROM users";
        
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            Assert.AreEqual(5, reader.GetInt32(1)); // 5 completed orders
        }
    }

    [TestMethod]
    public async Task Exec_CountWithAmountFilter()
    {
        using var conn = CreateTestDatabase();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, (SELECT COUNT(*) FROM (SELECT * FROM orders WHERE amount > 200) AS sq) AS Big FROM users";
        
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            Assert.AreEqual(4, reader.GetInt32(1)); // 300, 250, 500, 1000 = 4 orders > 200
        }
    }

    [TestMethod]
    public async Task Exec_MultipleSubQueries()
    {
        using var conn = CreateTestDatabase();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT id, 
                (SELECT COUNT(*) FROM (SELECT * FROM users) AS sq) AS TotalUsers,
                (SELECT COUNT(*) FROM (SELECT * FROM orders) AS sq) AS TotalOrders,
                (SELECT COUNT(*) FROM (SELECT * FROM departments) AS sq) AS TotalDepts
            FROM users";
        
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            Assert.AreEqual(5, reader.GetInt32(1));  // 5 users
            Assert.AreEqual(10, reader.GetInt32(2)); // 10 orders
            Assert.AreEqual(3, reader.GetInt32(3));  // 3 departments
        }
    }

    [TestMethod]
    public async Task Exec_WithMainWhere()
    {
        using var conn = CreateTestDatabase();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, (SELECT COUNT(*) FROM (SELECT * FROM orders) AS sq) AS Total FROM users WHERE is_active = 1";
        
        using var reader = await cmd.ExecuteReaderAsync();
        int count = 0;
        while (await reader.ReadAsync())
        {
            Assert.AreEqual(10, reader.GetInt32(1));
            count++;
        }
        Assert.AreEqual(4, count); // 4 active users
    }

    [TestMethod]
    public async Task Exec_GroupByWithSubQuery()
    {
        using var conn = CreateTestDatabase();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT department_id, COUNT(*) AS UserCount, 
                (SELECT COUNT(*) FROM (SELECT * FROM orders) AS sq) AS TotalOrders
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
        Assert.AreEqual((1, 2, 10), results[0]); // Dept 1: 2 users
        Assert.AreEqual((2, 2, 10), results[1]); // Dept 2: 2 users
        Assert.AreEqual((3, 1, 10), results[2]); // Dept 3: 1 user
    }

    [TestMethod]
    public async Task Exec_ComplexConditions()
    {
        using var conn = CreateTestDatabase();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT id, 
                (SELECT COUNT(*) FROM (SELECT * FROM orders WHERE amount > 100 AND status = 'completed') AS sq) AS BigCompleted
            FROM users WHERE is_active = 1 AND age > 25";
        
        using var reader = await cmd.ExecuteReaderAsync();
        int count = 0;
        while (await reader.ReadAsync())
        {
            // Orders > 100 AND completed: 200, 300, 250, 1000 = 4
            Assert.AreEqual(4, reader.GetInt32(1));
            count++;
        }
        Assert.AreEqual(3, count); // Alice(30), Charlie(35), Eve(40) are active and > 25
    }

    [TestMethod]
    public async Task Exec_GeneratedSqlWorks()
    {
        using var conn = CreateTestDatabase();

        // Generate SQL
        var generatedSql = SqlQuery<SqUser>.ForSqlite()
            .Where(x => x.IsActive)
            .Select(x => new { x.Id, CompletedOrders = SubQuery.For<SqOrder>().Where(o => o.Status == "completed").Count() })
            .ToSql();

        // Replace entity names with actual table names
        var executableSql = generatedSql
            .Replace("[SqUser]", "users")
            .Replace("[SqOrder]", "orders");

        using var cmd = conn.CreateCommand();
        cmd.CommandText = executableSql;
        
        using var reader = await cmd.ExecuteReaderAsync();
        int count = 0;
        while (await reader.ReadAsync())
        {
            Assert.AreEqual(5, reader.GetInt32(1)); // 5 completed orders
            count++;
        }
        Assert.AreEqual(4, count); // 4 active users
    }

    #endregion

    #region SubQuery Count with Predicate Tests

    [TestMethod]
    public void SubQuery_CountWithPredicate_SimpleCondition()
    {
        var sql = SqlQuery<SqUser>.ForSqlite()
            .Select(x => new { x.Id, MatchCount = SubQuery.For<SqOrder>().Count(o => o.UserId == 1) })
            .ToSql();

        Assert.AreEqual(
            "SELECT [id], (SELECT COUNT(*) FROM (SELECT * FROM [SqOrder]) AS sq WHERE [user_id] = 1) AS MatchCount FROM [SqUser]",
            sql);
    }

    [TestMethod]
    public void SubQuery_CountWithPredicate_ComplexCondition()
    {
        var sql = SqlQuery<SqUser>.ForSqlite()
            .Select(x => new { x.Id, MatchCount = SubQuery.For<SqOrder>().Count(o => o.Amount > 100 && o.Status == "completed") })
            .ToSql();

        Assert.AreEqual(
            "SELECT [id], (SELECT COUNT(*) FROM (SELECT * FROM [SqOrder]) AS sq WHERE ([amount] > 100 AND [status] = 'completed')) AS MatchCount FROM [SqUser]",
            sql);
    }

    [TestMethod]
    public void SubQuery_CountWithPredicate_WithWhereChain()
    {
        var sql = SqlQuery<SqUser>.ForSqlite()
            .Select(x => new { x.Id, MatchCount = SubQuery.For<SqOrder>().Where(o => o.Status == "active").Count(o => o.Amount > 50) })
            .ToSql();

        Assert.AreEqual(
            "SELECT [id], (SELECT COUNT(*) FROM (SELECT * FROM [SqOrder] WHERE [status] = 'active') AS sq WHERE [amount] > 50) AS MatchCount FROM [SqUser]",
            sql);
    }

    [TestMethod]
    public void SubQuery_CountWithPredicate_AllDialects()
    {
        // SQLite
        var sqliteSql = SqlQuery<SqUser>.ForSqlite()
            .Select(x => new { x.Id, Count = SubQuery.For<SqOrder>().Count(o => o.UserId == 1) })
            .ToSql();
        Assert.IsTrue(sqliteSql.Contains("WHERE [user_id] = 1"), $"SQLite: {sqliteSql}");

        // MySQL
        var mysqlSql = SqlQuery<SqUser>.ForMySql()
            .Select(x => new { x.Id, Count = SubQuery.For<SqOrder>().Count(o => o.UserId == 1) })
            .ToSql();
        Assert.IsTrue(mysqlSql.Contains("WHERE `user_id` = 1"), $"MySQL: {mysqlSql}");

        // PostgreSQL
        var pgSql = SqlQuery<SqUser>.ForPostgreSQL()
            .Select(x => new { x.Id, Count = SubQuery.For<SqOrder>().Count(o => o.UserId == 1) })
            .ToSql();
        Assert.IsTrue(pgSql.Contains("WHERE \"user_id\" = 1"), $"PostgreSQL: {pgSql}");
    }

    [TestMethod]
    public void SubQuery_GroupByWithCountPredicate()
    {
        var sql = SqlQuery<SqUser>.ForSqlite()
            .GroupBy(x => x.DepartmentId)
            .Select(x => new { 
                DeptId = x.Key, 
                UserCount = x.Count(),
                MatchingOrders = SubQuery.For<SqOrder>().Count(o => o.Amount > 100)
            })
            .ToSql();

        Assert.AreEqual(
            "SELECT [department_id] AS DeptId, COUNT(*) AS UserCount, (SELECT COUNT(*) FROM (SELECT * FROM [SqOrder]) AS sq WHERE [amount] > 100) AS MatchingOrders FROM [SqUser] GROUP BY [department_id]",
            sql);
    }

    [TestMethod]
    public void SubQuery_GroupByWithCountPredicateReferencingOuterCount()
    {
        var sql = SqlQuery<SqUser>.ForSqlite()
            .GroupBy(x => x.Id % 3)
            .Select(x => new { 
                Key = x.Key, 
                A = SubQuery.For<SqUser>().Count(y => y.Id == x.Count())
            })
            .ToSql();

        Assert.AreEqual(
            "SELECT ([id] % 3) AS Key, (SELECT COUNT(*) FROM (SELECT * FROM [SqUser]) AS sq WHERE [id] = COUNT(*)) AS A FROM [SqUser] GROUP BY ([id] % 3)",
            sql);
    }

    [TestMethod]
    public void SubQuery_WithToList_GeneratesSubQuerySql()
    {
        var sql = SqlQuery<SqUser>.ForSqlite()
            .GroupBy(x => x.Id % 3)
            .Select(x => new { 
                Key = x.Key, 
                A = SubQuery.For<SqUser>().Where(e => e.Id == 1).OrderBy(q => q.Name).ToList()
            })
            .ToSql();

        Assert.AreEqual(
            "SELECT ([id] % 3) AS Key, (SELECT * FROM [SqUser] WHERE [id] = 1 ORDER BY [name] ASC) AS A FROM [SqUser] GROUP BY ([id] % 3)",
            sql);
    }

    [TestMethod]
    public void SubQuery_WithToArray_GeneratesSubQuerySql()
    {
        var sql = SqlQuery<SqUser>.ForSqlite()
            .Select(x => new { 
                x.Id, 
                Orders = SubQuery.For<SqOrder>().Where(o => o.Status == "active").ToArray()
            })
            .ToSql();

        Assert.AreEqual(
            "SELECT [id], (SELECT * FROM [SqOrder] WHERE [status] = 'active') AS Orders FROM [SqUser]",
            sql);
    }

    [TestMethod]
    public void SubQuery_First_GeneratesLimitOne()
    {
        var sql = SqlQuery<SqUser>.ForSqlite()
            .Select(x => new { 
                x.Id, 
                FirstOrder = SubQuery.For<SqOrder>().Where(o => o.UserId == 1).First()
            })
            .ToSql();

        Assert.AreEqual(
            "SELECT [id], (SELECT * FROM [SqOrder] WHERE [user_id] = 1 LIMIT 1) AS FirstOrder FROM [SqUser]",
            sql);
    }

    [TestMethod]
    public void SubQuery_FirstWithPredicate_GeneratesWhereAndLimit()
    {
        var sql = SqlQuery<SqUser>.ForSqlite()
            .Select(x => new { 
                x.Id, 
                FirstBigOrder = SubQuery.For<SqOrder>().First(o => o.Amount > 100)
            })
            .ToSql();

        Assert.AreEqual(
            "SELECT [id], (SELECT * FROM [SqOrder] WHERE [amount] > 100 LIMIT 1) AS FirstBigOrder FROM [SqUser]",
            sql);
    }

    [TestMethod]
    public void SubQuery_Any_GeneratesExists()
    {
        var sql = SqlQuery<SqUser>.ForSqlite()
            .Select(x => new { 
                x.Id, 
                HasOrders = SubQuery.For<SqOrder>().Any()
            })
            .ToSql();

        Assert.AreEqual(
            "SELECT [id], (SELECT CASE WHEN EXISTS(SELECT * FROM [SqOrder]) THEN 1 ELSE 0 END) AS HasOrders FROM [SqUser]",
            sql);
    }

    [TestMethod]
    public void SubQuery_AnyWithPredicate_GeneratesExistsWithWhere()
    {
        var sql = SqlQuery<SqUser>.ForSqlite()
            .Select(x => new { 
                x.Id, 
                HasBigOrders = SubQuery.For<SqOrder>().Any(o => o.Amount > 1000)
            })
            .ToSql();

        Assert.AreEqual(
            "SELECT [id], (SELECT CASE WHEN EXISTS(SELECT 1 FROM (SELECT * FROM [SqOrder]) AS sq WHERE [amount] > 1000) THEN 1 ELSE 0 END) AS HasBigOrders FROM [SqUser]",
            sql);
    }

    [TestMethod]
    public void SubQuery_All_GeneratesNotExists()
    {
        var sql = SqlQuery<SqUser>.ForSqlite()
            .Select(x => new { 
                x.Id, 
                AllCompleted = SubQuery.For<SqOrder>().All(o => o.Status == "completed")
            })
            .ToSql();

        Assert.AreEqual(
            "SELECT [id], (SELECT CASE WHEN NOT EXISTS(SELECT 1 FROM (SELECT * FROM [SqOrder]) AS sq WHERE NOT ([status] = 'completed')) THEN 1 ELSE 0 END) AS AllCompleted FROM [SqUser]",
            sql);
    }

    #endregion
}
