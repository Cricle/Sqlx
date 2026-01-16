// -----------------------------------------------------------------------
// <copyright file="SqlxJoinOrderBySubQueryTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using Sqlx.Annotations;

namespace Sqlx.Tests;

/// <summary>
/// Join、OrderBy、SubQuery 的严格单元测试。
/// </summary>
[TestClass]
public class SqlxJoinOrderBySubQueryTests
{
    [Sqlx]
    public partial class JoinTestUser
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int DepartmentId { get; set; }
        public int Age { get; set; }
        public bool IsActive { get; set; }
    }

    [Sqlx]
    public partial class JoinTestDepartment
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int ManagerId { get; set; }
    }

    [Sqlx]
    public partial class JoinTestOrder
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public decimal Amount { get; set; }
        public DateTime OrderDate { get; set; }
    }

    #region OrderBy Tests

    [TestMethod]
    public void OrderBy_SingleColumn_GeneratesCorrectSql()
    {
        var sql = SqlQuery<JoinTestUser>.ForSqlite()
            .OrderBy(x => x.Name)
            .ToSql();

        Assert.IsTrue(sql.Contains("ORDER BY"));
        Assert.IsTrue(sql.Contains("[name] ASC"));
    }

    [TestMethod]
    public void OrderByDescending_SingleColumn_GeneratesCorrectSql()
    {
        var sql = SqlQuery<JoinTestUser>.ForSqlite()
            .OrderByDescending(x => x.Age)
            .ToSql();

        Assert.IsTrue(sql.Contains("ORDER BY"));
        Assert.IsTrue(sql.Contains("[age] DESC"));
    }

    [TestMethod]
    public void OrderBy_ThenBy_MultipleColumns_GeneratesCorrectSql()
    {
        var sql = SqlQuery<JoinTestUser>.ForSqlite()
            .OrderBy(x => x.Name)
            .ThenBy(x => x.Age)
            .ToSql();

        Assert.IsTrue(sql.Contains("ORDER BY"));
        Assert.IsTrue(sql.Contains("[name] ASC"));
        Assert.IsTrue(sql.Contains("[age] ASC"));
    }

    [TestMethod]
    public void OrderBy_ThenByDescending_MixedOrder_GeneratesCorrectSql()
    {
        var sql = SqlQuery<JoinTestUser>.ForSqlite()
            .OrderBy(x => x.Name)
            .ThenByDescending(x => x.Age)
            .ToSql();

        Assert.IsTrue(sql.Contains("[name] ASC"));
        Assert.IsTrue(sql.Contains("[age] DESC"));
    }

    [TestMethod]
    public void OrderByDescending_ThenBy_MixedOrder_GeneratesCorrectSql()
    {
        var sql = SqlQuery<JoinTestUser>.ForSqlite()
            .OrderByDescending(x => x.Name)
            .ThenBy(x => x.Age)
            .ToSql();

        Assert.IsTrue(sql.Contains("[name] DESC"));
        Assert.IsTrue(sql.Contains("[age] ASC"));
    }

    [TestMethod]
    public void OrderBy_WithWhere_GeneratesCorrectSql()
    {
        var sql = SqlQuery<JoinTestUser>.ForSqlite()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .ToSql();

        Assert.IsTrue(sql.Contains("WHERE"));
        Assert.IsTrue(sql.Contains("ORDER BY"));
        Assert.IsTrue(sql.IndexOf("WHERE") < sql.IndexOf("ORDER BY"));
    }

    [TestMethod]
    public void OrderBy_WithSkipTake_GeneratesCorrectSql()
    {
        var sql = SqlQuery<JoinTestUser>.ForSqlite()
            .OrderBy(x => x.Id)
            .Skip(10)
            .Take(20)
            .ToSql();

        Assert.IsTrue(sql.Contains("ORDER BY"));
        Assert.IsTrue(sql.Contains("LIMIT 20"));
        Assert.IsTrue(sql.Contains("OFFSET 10"));
    }

    [TestMethod]
    [DataRow("SQLite", "LIMIT", "OFFSET")]
    [DataRow("SqlServer", "OFFSET", "FETCH NEXT")]
    [DataRow("MySql", "LIMIT", "OFFSET")]
    [DataRow("PostgreSQL", "LIMIT", "OFFSET")]
    public void OrderBy_Pagination_AllDialects(string dialectName, string limitKeyword, string offsetKeyword)
    {
        var sql = GetQueryForDialect<JoinTestUser>(dialectName)
            .OrderBy(x => x.Id)
            .Skip(5)
            .Take(10)
            .ToSql();

        Assert.IsTrue(sql.Contains("ORDER BY"), $"{dialectName} should have ORDER BY");
        Assert.IsTrue(sql.Contains(limitKeyword) || sql.Contains(offsetKeyword), 
            $"{dialectName} should have pagination keywords");
    }

    #endregion

    #region Join Tests

    [TestMethod]
    public void Join_InnerJoin_GeneratesCorrectSql()
    {
        var users = SqlQuery<JoinTestUser>.ForSqlite();
        var departments = SqlQuery<JoinTestDepartment>.ForSqlite();

        var sql = users
            .Join(departments, u => u.DepartmentId, d => d.Id, (u, d) => new { u.Name, DeptName = d.Name })
            .ToSql();

        Assert.IsTrue(sql.Contains("INNER JOIN"));
        Assert.IsTrue(sql.Contains("ON"));
    }

    [TestMethod]
    public void Join_SelectBothTables_GeneratesCorrectSql()
    {
        var users = SqlQuery<JoinTestUser>.ForSqlite();
        var departments = SqlQuery<JoinTestDepartment>.ForSqlite();

        var sql = users
            .Join(departments, u => u.DepartmentId, d => d.Id, (u, d) => new { UserId = u.Id, UserName = u.Name, DeptId = d.Id, DeptName = d.Name })
            .ToSql();

        Assert.IsTrue(sql.Contains("INNER JOIN"));
        Assert.IsTrue(sql.Contains("AS UserId") || sql.Contains("AS UserName") || sql.Contains("AS DeptName"));
    }

    [TestMethod]
    public void GroupJoin_LeftJoin_GeneratesCorrectSql()
    {
        var users = SqlQuery<JoinTestUser>.ForSqlite();
        var departments = SqlQuery<JoinTestDepartment>.ForSqlite();

        var sql = users
            .GroupJoin(departments, u => u.DepartmentId, d => d.Id, (u, depts) => new { u.Name })
            .ToSql();

        Assert.IsTrue(sql.Contains("LEFT JOIN"));
    }

    #endregion

    #region SubQuery Tests

    [TestMethod]
    public void SubQuery_WhereOnSubQuery_GeneratesCorrectSql()
    {
        var subQuery = SqlQuery<JoinTestUser>.ForSqlite()
            .Where(x => x.IsActive);

        var sql = subQuery.AsSubQuery()
            .Where(x => x.Age > 18)
            .ToSql();

        Assert.IsTrue(sql.Contains("FROM (SELECT"));
        Assert.IsTrue(sql.Contains(") AS [sq]"));
        Assert.IsTrue(sql.Contains("[age] > 18"));
    }

    [TestMethod]
    public void SubQuery_SelectOnSubQuery_GeneratesCorrectSql()
    {
        var subQuery = SqlQuery<JoinTestUser>.ForSqlite()
            .Where(x => x.Age > 20)
            .Select(x => new { x.Id, x.Name });

        var sql = subQuery.AsSubQuery()
            .Where(x => x.Id > 10)
            .ToSql();

        Assert.IsTrue(sql.Contains("FROM (SELECT"));
        Assert.IsTrue(sql.Contains("[id]"));
        Assert.IsTrue(sql.Contains("[name]"));
    }

    [TestMethod]
    public void SubQuery_OrderByOnSubQuery_GeneratesCorrectSql()
    {
        var subQuery = SqlQuery<JoinTestUser>.ForSqlite()
            .Where(x => x.IsActive)
            .Take(100);

        var sql = subQuery.AsSubQuery()
            .OrderBy(x => x.Name)
            .ToSql();

        Assert.IsTrue(sql.Contains("FROM (SELECT"));
        Assert.IsTrue(sql.Contains("LIMIT 100"));
        Assert.IsTrue(sql.Contains("ORDER BY"));
    }

    [TestMethod]
    public void SubQuery_GroupByOnSubQuery_GeneratesCorrectSql()
    {
        var subQuery = SqlQuery<JoinTestUser>.ForSqlite()
            .Where(x => x.IsActive);

        var sql = subQuery.AsSubQuery()
            .GroupBy(x => x.DepartmentId)
            .Select(x => new { x.Key, Count = x.Count() })
            .ToSql();

        Assert.IsTrue(sql.Contains("FROM (SELECT"));
        Assert.IsTrue(sql.Contains("GROUP BY"));
        Assert.IsTrue(sql.Contains("COUNT(*)"));
    }

    [TestMethod]
    public void SubQuery_NestedSubQuery_GeneratesCorrectSql()
    {
        var innerSubQuery = SqlQuery<JoinTestUser>.ForSqlite()
            .Where(x => x.Age > 18);

        var outerSubQuery = innerSubQuery.AsSubQuery()
            .Where(x => x.IsActive);

        var sql = outerSubQuery.AsSubQuery()
            .Take(10)
            .ToSql();

        // Should have nested subqueries
        Assert.IsTrue(sql.Contains("FROM (SELECT"));
        Assert.IsTrue(sql.Contains("LIMIT 10"));
    }

    [TestMethod]
    public void SubQuery_WithDistinct_GeneratesCorrectSql()
    {
        var subQuery = SqlQuery<JoinTestUser>.ForSqlite()
            .Select(x => x.DepartmentId)
            .Distinct();

        var sql = subQuery.AsSubQuery()
            .ToSql();

        Assert.IsTrue(sql.Contains("DISTINCT"));
        Assert.IsTrue(sql.Contains("FROM (SELECT"));
    }

    #endregion

    #region Complex Combination Tests

    [TestMethod]
    public void WhereOrderBySkipTake_FullPipeline_GeneratesCorrectSql()
    {
        var sql = SqlQuery<JoinTestUser>.ForSqlite()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .ThenByDescending(x => x.Age)
            .Skip(20)
            .Take(10)
            .ToSql();

        Assert.IsTrue(sql.Contains("WHERE"));
        Assert.IsTrue(sql.Contains("ORDER BY"));
        Assert.IsTrue(sql.Contains("[name] ASC"));
        Assert.IsTrue(sql.Contains("[age] DESC"));
        Assert.IsTrue(sql.Contains("LIMIT 10"));
        Assert.IsTrue(sql.Contains("OFFSET 20"));
    }

    [TestMethod]
    public void SelectWhereOrderBy_FullPipeline_GeneratesCorrectSql()
    {
        var sql = SqlQuery<JoinTestUser>.ForSqlite()
            .Where(x => x.Age > 18 && x.IsActive)
            .Select(x => new { x.Id, x.Name, x.Age })
            .OrderBy(x => x.Name)
            .ToSql();

        Assert.IsTrue(sql.Contains("WHERE"));
        Assert.IsTrue(sql.Contains("[age] > 18"));
        Assert.IsTrue(sql.Contains("ORDER BY"));
    }

    [TestMethod]
    public void GroupByOrderBy_GeneratesCorrectSql()
    {
        var sql = SqlQuery<JoinTestUser>.ForSqlite()
            .GroupBy(x => x.DepartmentId)
            .Select(x => new { x.Key, Count = x.Count() })
            .OrderBy(x => x.Key)
            .ToSql();

        Assert.IsTrue(sql.Contains("GROUP BY"));
        Assert.IsTrue(sql.Contains("COUNT(*)"));
        Assert.IsTrue(sql.Contains("ORDER BY"));
    }

    [TestMethod]
    public void WhereGroupBySelectOrderByTake_FullPipeline_GeneratesCorrectSql()
    {
        var sql = SqlQuery<JoinTestUser>.ForSqlite()
            .Where(x => x.IsActive)
            .GroupBy(x => x.DepartmentId)
            .Select(x => new { DeptId = x.Key, Total = x.Count() })
            .OrderByDescending(x => x.Total)
            .Take(5)
            .ToSql();

        Assert.IsTrue(sql.Contains("WHERE"));
        Assert.IsTrue(sql.Contains("GROUP BY"));
        Assert.IsTrue(sql.Contains("COUNT(*)"));
        Assert.IsTrue(sql.Contains("ORDER BY"));
        Assert.IsTrue(sql.Contains("DESC"));
        Assert.IsTrue(sql.Contains("LIMIT 5"));
    }

    [TestMethod]
    public void Distinct_WithOrderBy_GeneratesCorrectSql()
    {
        var sql = SqlQuery<JoinTestUser>.ForSqlite()
            .Select(x => x.DepartmentId)
            .Distinct()
            .OrderBy(x => x)
            .ToSql();

        Assert.IsTrue(sql.Contains("SELECT DISTINCT"));
        Assert.IsTrue(sql.Contains("ORDER BY"));
    }

    #endregion

    #region Cross-Dialect Tests

    [TestMethod]
    [DataRow("SQLite")]
    [DataRow("SqlServer")]
    [DataRow("MySql")]
    [DataRow("PostgreSQL")]
    public void OrderBy_AllDialects_GeneratesValidSql(string dialectName)
    {
        var sql = GetQueryForDialect<JoinTestUser>(dialectName)
            .OrderBy(x => x.Name)
            .ThenByDescending(x => x.Age)
            .ToSql();

        Assert.IsTrue(sql.Contains("ORDER BY"), $"{dialectName} should have ORDER BY");
        Assert.IsTrue(sql.Contains("ASC"), $"{dialectName} should have ASC");
        Assert.IsTrue(sql.Contains("DESC"), $"{dialectName} should have DESC");
    }

    [TestMethod]
    [DataRow("SQLite")]
    [DataRow("SqlServer")]
    [DataRow("MySql")]
    [DataRow("PostgreSQL")]
    public void SubQuery_AllDialects_GeneratesValidSql(string dialectName)
    {
        var subQuery = GetQueryForDialect<JoinTestUser>(dialectName)
            .Where(x => x.IsActive);

        var sql = subQuery.AsSubQuery()
            .Take(10)
            .ToSql();

        Assert.IsTrue(sql.Contains("FROM (SELECT"), $"{dialectName} should have subquery");
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    public void OrderBy_MultipleColumns_ThreeOrMore_GeneratesCorrectSql()
    {
        var sql = SqlQuery<JoinTestUser>.ForSqlite()
            .OrderBy(x => x.DepartmentId)
            .ThenBy(x => x.Name)
            .ThenByDescending(x => x.Age)
            .ThenBy(x => x.Id)
            .ToSql();

        Assert.IsTrue(sql.Contains("[department_id] ASC"));
        Assert.IsTrue(sql.Contains("[name] ASC"));
        Assert.IsTrue(sql.Contains("[age] DESC"));
        Assert.IsTrue(sql.Contains("[id] ASC"));
    }

    [TestMethod]
    public void Take_WithoutOrderBy_GeneratesCorrectSql()
    {
        var sql = SqlQuery<JoinTestUser>.ForSqlite()
            .Take(10)
            .ToSql();

        Assert.IsTrue(sql.Contains("LIMIT 10"));
        Assert.IsFalse(sql.Contains("ORDER BY"));
    }

    [TestMethod]
    public void Skip_WithoutTake_GeneratesCorrectSql()
    {
        var sql = SqlQuery<JoinTestUser>.ForSqlite()
            .Skip(10)
            .ToSql();

        Assert.IsTrue(sql.Contains("OFFSET 10"));
    }

    [TestMethod]
    public void First_GeneratesLimit1()
    {
        var sql = SqlQuery<JoinTestUser>.ForSqlite()
            .Where(x => x.IsActive)
            .ToSql();

        // First is terminal operation, but we can test Take(1)
        var sqlWithTake = SqlQuery<JoinTestUser>.ForSqlite()
            .Where(x => x.IsActive)
            .Take(1)
            .ToSql();

        Assert.IsTrue(sqlWithTake.Contains("LIMIT 1"));
    }

    #endregion

    #region Helper Methods

    private static IQueryable<T> GetQueryForDialect<T>(string dialectName) => dialectName switch
    {
        "SQLite" => SqlQuery<T>.ForSqlite(),
        "SqlServer" => SqlQuery<T>.ForSqlServer(),
        "MySql" => SqlQuery<T>.ForMySql(),
        "PostgreSQL" => SqlQuery<T>.ForPostgreSQL(),
        "Oracle" => SqlQuery<T>.ForOracle(),
        "DB2" => SqlQuery<T>.ForDB2(),
        _ => throw new ArgumentException($"Unknown dialect: {dialectName}")
    };

    #endregion

    #region Scalar SubQuery in Select Tests

    [TestMethod]
    public void Select_WithScalarSubQuery_Count_GeneratesCorrectSql()
    {
        var sql = SqlQuery<JoinTestUser>.ForSqlite()
            .Select(x => new { x.Id, Total = SubQuery.For<JoinTestOrder>().Count() })
            .ToSql();

        Assert.AreEqual("SELECT [id], (SELECT COUNT(*) FROM (SELECT * FROM [JoinTestOrder]) AS sq) AS Total FROM [JoinTestUser]", sql);
    }

    [TestMethod]
    public void Select_WithScalarSubQuery_CountWithWhere_GeneratesCorrectSql()
    {
        var sql = SqlQuery<JoinTestUser>.ForSqlite()
            .Select(x => new { x.Id, OrderCount = SubQuery.For<JoinTestOrder>().Where(o => o.Amount > 100).Count() })
            .ToSql();

        Assert.AreEqual("SELECT [id], (SELECT COUNT(*) FROM (SELECT * FROM [JoinTestOrder] WHERE [amount] > 100) AS sq) AS OrderCount FROM [JoinTestUser]", sql);
    }

    [TestMethod]
    public void GroupBy_WithScalarSubQuery_GeneratesCorrectSql()
    {
        var sql = SqlQuery<JoinTestUser>.ForSqlite()
            .GroupBy(x => x.DepartmentId)
            .Select(x => new { x.Key, TotalOrders = SubQuery.For<JoinTestOrder>().Count() })
            .ToSql();

        Assert.AreEqual("SELECT [department_id] AS Key, (SELECT COUNT(*) FROM (SELECT * FROM [JoinTestOrder]) AS sq) AS TotalOrders FROM [JoinTestUser] GROUP BY [department_id]", sql);
    }

    [TestMethod]
    public void Select_WithMultipleScalarSubQueries_GeneratesCorrectSql()
    {
        var sql = SqlQuery<JoinTestUser>.ForSqlite()
            .Select(x => new { 
                x.Id, 
                UserCount = SubQuery.For<JoinTestUser>().Count(),
                OrderCount = SubQuery.For<JoinTestOrder>().Count()
            })
            .ToSql();

        Assert.AreEqual("SELECT [id], (SELECT COUNT(*) FROM (SELECT * FROM [JoinTestUser]) AS sq) AS UserCount, (SELECT COUNT(*) FROM (SELECT * FROM [JoinTestOrder]) AS sq) AS OrderCount FROM [JoinTestUser]", sql);
    }

    #endregion
}
