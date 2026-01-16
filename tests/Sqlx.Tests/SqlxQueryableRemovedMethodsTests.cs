// <copyright file="SqlxQueryableRemovedMethodsTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sqlx.Tests;

/// <summary>
/// Tests to verify that ToWhereSql and ToWhereSqlWithParameters methods have been removed
/// from SqlxQueryable, and that ExpressionExtensions.ToWhereClause is the correct replacement.
/// </summary>
[TestClass]
public class SqlxQueryableRemovedMethodsTests
{
    #region Test Entity

    public class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    #endregion

    #region Method Removal Verification Tests

    [TestMethod]
    public void SqlxQueryable_ToWhereSql_MethodDoesNotExist()
    {
        var type = typeof(SqlxQueryable<TestEntity>);
        var method = type.GetMethod("ToWhereSql", BindingFlags.Public | BindingFlags.Instance);
        
        Assert.IsNull(method, "ToWhereSql method should have been removed from SqlxQueryable");
    }

    [TestMethod]
    public void SqlxQueryable_ToWhereSqlWithParameters_MethodDoesNotExist()
    {
        var type = typeof(SqlxQueryable<TestEntity>);
        var method = type.GetMethod("ToWhereSqlWithParameters", BindingFlags.Public | BindingFlags.Instance);
        
        Assert.IsNull(method, "ToWhereSqlWithParameters method should have been removed from SqlxQueryable");
    }

    #endregion

    #region Replacement Method Tests

    [TestMethod]
    public void ExpressionExtensions_ToWhereClause_IsAvailable()
    {
        var type = typeof(ExpressionExtensions);
        var method = type.GetMethod("ToWhereClause", BindingFlags.Public | BindingFlags.Static);
        
        Assert.IsNotNull(method, "ToWhereClause should be available as the replacement method");
    }

    [TestMethod]
    public void ExpressionExtensions_ToWhereClause_WorksWithSimpleExpression()
    {
        Expression<Func<TestEntity, bool>> predicate = e => e.Id == 1;
        
        var sql = predicate.ToWhereClause(SqlDefine.SQLite);
        
        Assert.IsFalse(string.IsNullOrEmpty(sql));
        Assert.IsTrue(sql.Contains("id"));
        Assert.IsTrue(sql.Contains("1"));
    }

    [TestMethod]
    public void ExpressionExtensions_ToWhereClause_WorksWithComplexExpression()
    {
        Expression<Func<TestEntity, bool>> predicate = e => e.Id > 5 && e.IsActive == true;
        
        var sql = predicate.ToWhereClause(SqlDefine.SQLite);
        
        Assert.IsFalse(string.IsNullOrEmpty(sql));
        Assert.IsTrue(sql.Contains("id"));
        Assert.IsTrue(sql.Contains("is_active"));
        Assert.IsTrue(sql.Contains("AND"));
    }

    #endregion

    #region SqlxQueryable Core Functionality Tests

    [TestMethod]
    public void SqlxQueryable_StillHasDialectProperty()
    {
        var queryable = SqlQuery<TestEntity>.For(SqlDefine.SQLite) as SqlxQueryable<TestEntity>;
        
        Assert.IsNotNull(queryable);
        Assert.AreEqual(SqlDefine.SQLite.DatabaseType, queryable.Dialect.DatabaseType);
    }

    [TestMethod]
    public void SqlxQueryable_StillImplementsIQueryable()
    {
        var queryable = SqlQuery<TestEntity>.For(SqlDefine.SQLite);
        
        Assert.IsInstanceOfType(queryable, typeof(IQueryable<TestEntity>));
    }

    [TestMethod]
    public void SqlxQueryable_StillSupportsWhereClause()
    {
        var queryable = SqlQuery<TestEntity>.For(SqlDefine.SQLite)
            .Where(e => e.Id == 1);
        
        Assert.IsNotNull(queryable);
        Assert.IsInstanceOfType(queryable, typeof(IQueryable<TestEntity>));
    }

    [TestMethod]
    public void SqlxQueryable_StillSupportsOrderBy()
    {
        var queryable = SqlQuery<TestEntity>.For(SqlDefine.SQLite)
            .OrderBy(e => e.Id);
        
        Assert.IsNotNull(queryable);
    }

    [TestMethod]
    public void SqlxQueryable_StillSupportsSelect()
    {
        var queryable = SqlQuery<TestEntity>.For(SqlDefine.SQLite)
            .Select(e => new { e.Id, e.Name });
        
        Assert.IsNotNull(queryable);
    }

    #endregion

    #region Migration Path Tests

    [TestMethod]
    public void MigrationPath_ExpressionToWhereClause_ProducesValidSql()
    {
        // Old way (removed): queryable.Where(e => e.Id == 1).ToWhereSql()
        // New way: predicate.ToWhereClause(dialect)
        
        Expression<Func<TestEntity, bool>> predicate = e => e.Id == 1;
        var sql = predicate.ToWhereClause(SqlDefine.SQLite);
        
        Assert.AreEqual("[id] = 1", sql);
    }

    [TestMethod]
    public void MigrationPath_ExpressionToWhereClause_HandlesAllDialects()
    {
        Expression<Func<TestEntity, bool>> predicate = e => e.Id == 1;
        
        var sqliteSql = predicate.ToWhereClause(SqlDefine.SQLite);
        var mysqlSql = predicate.ToWhereClause(SqlDefine.MySql);
        var pgsqlSql = predicate.ToWhereClause(SqlDefine.PostgreSql);
        var sqlserverSql = predicate.ToWhereClause(SqlDefine.SqlServer);
        
        Assert.AreEqual("[id] = 1", sqliteSql);
        Assert.AreEqual("`id` = 1", mysqlSql);
        Assert.AreEqual("\"id\" = 1", pgsqlSql);
        Assert.AreEqual("[id] = 1", sqlserverSql);
    }

    [TestMethod]
    public void MigrationPath_ExpressionToWhereClause_HandlesNullPredicate()
    {
        Expression<Func<TestEntity, bool>>? predicate = null;
        
        var sql = predicate!.ToWhereClause(SqlDefine.SQLite);
        
        Assert.AreEqual(string.Empty, sql);
    }

    #endregion
}
