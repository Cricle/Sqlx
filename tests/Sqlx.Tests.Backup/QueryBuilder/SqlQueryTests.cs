// -----------------------------------------------------------------------
// <copyright file="SqlQueryTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using Sqlx.Annotations;

namespace Sqlx.Tests.QueryBuilder;

[Sqlx]
public partial class TestUser
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public bool IsActive { get; set; }
    public decimal Salary { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Email { get; set; }
}

/// <summary>
/// SqlQuery 查询构建器基础测试 - 工厂方法、接口实现、基本查询
/// </summary>
[TestClass]
public class SqlQueryTests
{
    #region 工厂方法测试

    [TestMethod]
    public void ForSqlite_ReturnsQueryable()
    {
        var query = SqlQuery<TestUser>.ForSqlite();
        
        Assert.IsNotNull(query);
        Assert.IsInstanceOfType(query, typeof(IQueryable<TestUser>));
    }

    [TestMethod]
    public void ForSqlServer_ReturnsQueryable()
    {
        var query = SqlQuery<TestUser>.ForSqlServer();
        
        Assert.IsNotNull(query);
        Assert.IsInstanceOfType(query, typeof(IQueryable<TestUser>));
    }

    [TestMethod]
    public void ForMySql_ReturnsQueryable()
    {
        var query = SqlQuery<TestUser>.ForMySql();
        
        Assert.IsNotNull(query);
        Assert.IsInstanceOfType(query, typeof(IQueryable<TestUser>));
    }

    [TestMethod]
    public void ForPostgreSQL_ReturnsQueryable()
    {
        var query = SqlQuery<TestUser>.ForPostgreSQL();
        
        Assert.IsNotNull(query);
        Assert.IsInstanceOfType(query, typeof(IQueryable<TestUser>));
    }

    [TestMethod]
    public void ForOracle_ReturnsQueryable()
    {
        var query = SqlQuery<TestUser>.ForOracle();
        
        Assert.IsNotNull(query);
        Assert.IsInstanceOfType(query, typeof(IQueryable<TestUser>));
    }

    [TestMethod]
    public void ForDB2_ReturnsQueryable()
    {
        var query = SqlQuery<TestUser>.ForDB2();
        
        Assert.IsNotNull(query);
        Assert.IsInstanceOfType(query, typeof(IQueryable<TestUser>));
    }

    [TestMethod]
    public void For_WithDialect_ReturnsQueryable()
    {
        var query = SqlQuery<TestUser>.For(SqlDefine.SQLite);
        
        Assert.IsNotNull(query);
        Assert.IsInstanceOfType(query, typeof(IQueryable<TestUser>));
    }

    [TestMethod]
    public void For_WithEntityProvider_ReturnsQueryable()
    {
        var provider = TestUserEntityProvider.Default;
        var query = SqlQuery<TestUser>.For(SqlDefine.SQLite, provider);
        
        Assert.IsNotNull(query);
        Assert.IsInstanceOfType(query, typeof(IQueryable<TestUser>));
    }

    #endregion

    #region 接口实现测试

    [TestMethod]
    public void Query_ImplementsIQueryable()
    {
        var query = SqlQuery<TestUser>.ForSqlite();
        
        Assert.IsInstanceOfType(query, typeof(IQueryable<TestUser>));
    }

    [TestMethod]
    public void Query_ElementType_ReturnsCorrectType()
    {
        var query = SqlQuery<TestUser>.ForSqlite();
        
        Assert.AreEqual(typeof(TestUser), query.ElementType);
    }

    [TestMethod]
    public void Query_Expression_IsNotNull()
    {
        var query = SqlQuery<TestUser>.ForSqlite();
        
        Assert.IsNotNull(query.Expression);
    }

    [TestMethod]
    public void Query_Provider_IsNotNull()
    {
        var query = SqlQuery<TestUser>.ForSqlite();
        
        Assert.IsNotNull(query.Provider);
        Assert.IsInstanceOfType(query.Provider, typeof(SqlxQueryProvider<TestUser>));
    }

    [TestMethod]
    public void OrderedQuery_ImplementsIOrderedQueryable()
    {
        var query = SqlQuery<TestUser>.ForSqlite().OrderBy(u => u.Name);
        
        Assert.IsInstanceOfType(query, typeof(IOrderedQueryable<TestUser>));
    }

    #endregion

    #region 基本查询生成测试

    [TestMethod]
    public void ToSql_NoConditions_GeneratesSelectAll()
    {
        var sql = SqlQuery<TestUser>.ForSqlite().ToSql();
        
        Assert.IsTrue(sql.Contains("SELECT"));
        Assert.IsTrue(sql.Contains("[id]"));
        Assert.IsTrue(sql.Contains("[name]"));
        Assert.IsTrue(sql.Contains("FROM [TestUser]"));
    }

    [TestMethod]
    public void ToSql_WithWhere_GeneratesWhereClause()
    {
        var sql = SqlQuery<TestUser>.ForSqlite()
            .Where(u => u.Id == 1)
            .ToSql();
        
        Assert.IsTrue(sql.Contains("WHERE"));
        Assert.IsTrue(sql.Contains("[id] = 1"));
    }

    [TestMethod]
    public void ToSql_WithOrderBy_GeneratesOrderByClause()
    {
        var sql = SqlQuery<TestUser>.ForSqlite()
            .OrderBy(u => u.Name)
            .ToSql();
        
        Assert.IsTrue(sql.Contains("ORDER BY [name] ASC"));
    }

    [TestMethod]
    public void ToSql_WithTake_GeneratesLimitClause()
    {
        var sql = SqlQuery<TestUser>.ForSqlite()
            .Take(10)
            .ToSql();
        
        Assert.IsTrue(sql.Contains("LIMIT 10"));
    }

    [TestMethod]
    public void ToSql_WithSkip_GeneratesOffsetClause()
    {
        var sql = SqlQuery<TestUser>.ForSqlite()
            .Skip(20)
            .ToSql();
        
        Assert.IsTrue(sql.Contains("OFFSET 20"));
    }

    #endregion

    #region 方法链测试

    [TestMethod]
    public void MethodChaining_PreservesExpression()
    {
        var query = SqlQuery<TestUser>.ForSqlite()
            .Where(u => u.IsActive)
            .OrderBy(u => u.Name)
            .Take(10);
        
        Assert.IsNotNull(query.Expression);
        var sql = query.ToSql();
        Assert.IsTrue(sql.Contains("WHERE"));
        Assert.IsTrue(sql.Contains("ORDER BY"));
        Assert.IsTrue(sql.Contains("LIMIT"));
    }

    [TestMethod]
    public void MultipleWhere_ChainsWithAnd()
    {
        var sql = SqlQuery<TestUser>.ForSqlite()
            .Where(u => u.IsActive)
            .Where(u => u.Age > 18)
            .ToSql();
        
        Assert.IsTrue(sql.Contains("AND"));
    }

    [TestMethod]
    public void ComplexChain_GeneratesCorrectSQL()
    {
        var sql = SqlQuery<TestUser>.ForSqlite()
            .Where(u => u.IsActive)
            .Where(u => u.Age >= 18)
            .OrderBy(u => u.Name)
            .ThenByDescending(u => u.CreatedAt)
            .Skip(10)
            .Take(20)
            .ToSql();
        
        Assert.IsTrue(sql.Contains("WHERE"));
        Assert.IsTrue(sql.Contains("AND"));
        Assert.IsTrue(sql.Contains("ORDER BY"));
        Assert.IsTrue(sql.Contains("LIMIT 20"));
        Assert.IsTrue(sql.Contains("OFFSET 10"));
    }

    #endregion

    #region 错误处理测试

    [TestMethod]
    public void GetEnumerator_WithoutConnection_ThrowsException()
    {
        var query = SqlQuery<TestUser>.ForSqlite();
        
        Assert.ThrowsException<InvalidOperationException>(() => query.GetEnumerator());
    }

    [TestMethod]
    public void ToSql_OnNonSqlxQueryable_ThrowsException()
    {
        var list = new[] { new TestUser() }.AsQueryable();
        
        Assert.ThrowsException<InvalidOperationException>(() => list.ToSql());
    }

    #endregion

    #region 静态缓存测试

    [TestMethod]
    public void EntityProvider_StaticCache_IsSingleton()
    {
        var provider1 = SqlQuery<TestUser>.EntityProvider;
        var provider2 = SqlQuery<TestUser>.EntityProvider;
        
        Assert.AreSame(provider1, provider2);
    }

    [TestMethod]
    public void ResultReader_StaticCache_IsSingleton()
    {
        var reader1 = SqlQuery<TestUser>.ResultReader;
        var reader2 = SqlQuery<TestUser>.ResultReader;
        
        Assert.AreSame(reader1, reader2);
    }

    [TestMethod]
    public void ParameterBinder_StaticCache_IsSingleton()
    {
        var binder1 = SqlQuery<TestUser>.ParameterBinder;
        var binder2 = SqlQuery<TestUser>.ParameterBinder;
        
        Assert.AreSame(binder1, binder2);
    }

    #endregion
}
