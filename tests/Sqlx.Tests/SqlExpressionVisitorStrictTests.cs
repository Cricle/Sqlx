// -----------------------------------------------------------------------
// <copyright file="SqlExpressionVisitorStrictTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;
using System;
using System.Linq;

namespace Sqlx.Tests;

[TestClass]
public class SqlExpressionVisitorStrictTests
{
    [Sqlx]
    public partial class SvUser
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int Age { get; set; }
        public int DepartmentId { get; set; }
        public bool IsActive { get; set; }
    }

    [Sqlx]
    public partial class SvDepartment
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int ManagerId { get; set; }
    }

    [Sqlx]
    public partial class SvOrder
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public decimal Amount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    [TestMethod]
    public void Select_AllColumns_NoSelectStar()
    {
        var sql = SqlQuery<SvUser>.ForSqlite().ToSql();
        Assert.IsFalse(sql.Contains("*"), "Should not contain SELECT *");
        Assert.AreEqual("SELECT [id], [name], [age], [department_id], [is_active] FROM [SvUser]", sql);
    }

    [TestMethod]
    public void Select_ExplicitColumns_GeneratesCorrectSql()
    {
        var sql = SqlQuery<SvUser>.ForSqlite().Select(u => new { u.Id, u.Name }).ToSql();
        Assert.AreEqual("SELECT [id], [name] FROM [SvUser]", sql);
    }

    [TestMethod]
    public void Where_SingleCondition_GeneratesCorrectSql()
    {
        var sql = SqlQuery<SvUser>.ForSqlite().Where(u => u.Id == 1).ToSql();
        Assert.AreEqual("SELECT [id], [name], [age], [department_id], [is_active] FROM [SvUser] WHERE [id] = 1", sql);
    }

    [TestMethod]
    public void OrderBy_SingleColumn_GeneratesCorrectSql()
    {
        var sql = SqlQuery<SvUser>.ForSqlite().OrderBy(u => u.Name).ToSql();
        Assert.AreEqual("SELECT [id], [name], [age], [department_id], [is_active] FROM [SvUser] ORDER BY [name] ASC", sql);
    }

    [TestMethod]
    public void Take_GeneratesCorrectSql_SQLite()
    {
        var sql = SqlQuery<SvUser>.ForSqlite().Take(10).ToSql();
        Assert.AreEqual("SELECT [id], [name], [age], [department_id], [is_active] FROM [SvUser] LIMIT 10", sql);
    }

    [TestMethod]
    public void GroupBy_WithSelect_GeneratesCorrectSql()
    {
        var sql = SqlQuery<SvUser>.ForSqlite().GroupBy(u => u.DepartmentId).Select(g => new { g.Key, Count = g.Count() }).ToSql();
        Assert.AreEqual("SELECT [department_id] AS Key, COUNT(*) AS Count FROM [SvUser] GROUP BY [department_id]", sql);
    }

    [TestMethod]
    public void Join_SimpleJoin_GeneratesCorrectAliases()
    {
        var sql = SqlQuery<SvUser>.ForSqlite()
            .Join(SqlQuery<SvDepartment>.ForSqlite(), u => u.DepartmentId, d => d.Id, (u, d) => new { UserId = u.Id, DeptName = d.Name })
            .ToSql();
        
        Assert.IsTrue(sql.Contains("[SvUser] AS [t1]"), "SvUser should have alias t1");
        Assert.IsTrue(sql.Contains("[SvDepartment] AS [t2]"), "SvDepartment should have alias t2");
        Assert.IsTrue(sql.Contains("[t1].[department_id] = [t2].[id]"), "ON condition should use correct aliases");
    }

    [TestMethod]
    public void SubQuery_InSelect_NoSelectStar()
    {
        var sql = SqlQuery<SvUser>.ForSqlite()
            .Select(u => new { u.Name, OrderCount = SubQuery.For<SvOrder>().Where(o => o.UserId == u.Id).Count() })
            .ToSql();
        
        Assert.IsTrue(sql.Contains("(SELECT COUNT(*)"), "Should contain subquery with COUNT");
        Assert.IsFalse(sql.Contains("SELECT *"), "Subquery should not use SELECT *");
    }
}
