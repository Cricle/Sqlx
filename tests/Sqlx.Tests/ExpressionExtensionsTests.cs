// <copyright file="ExpressionExtensionsTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using System;
using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sqlx.Tests;

/// <summary>
/// High-quality unit tests for ExpressionExtensions.ToWhereClause.
/// Tests the direct Expression to SQL conversion used by RepositoryGenerator.
/// </summary>
[TestClass]
public class ExpressionExtensionsTests
{
    public class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public decimal Price { get; set; }
    }

    #region Basic Equality Tests

    [TestMethod]
    public void ToWhereClause_SimpleEquality_GeneratesCorrectSql()
    {
        Expression<Func<TestEntity, bool>> predicate = e => e.Id == 1;
        var sql = predicate.ToWhereClause(SqlDefine.SQLite);
        Assert.AreEqual("[id] = 1", sql);
    }

    [TestMethod]
    public void ToWhereClause_StringEquality_GeneratesCorrectSql()
    {
        Expression<Func<TestEntity, bool>> predicate = e => e.Name == "test";
        var sql = predicate.ToWhereClause(SqlDefine.SQLite);
        Assert.AreEqual("[name] = 'test'", sql);
    }

    [TestMethod]
    public void ToWhereClause_BooleanTrue_GeneratesCorrectSql()
    {
        Expression<Func<TestEntity, bool>> predicate = e => e.IsActive == true;
        var sql = predicate.ToWhereClause(SqlDefine.SQLite);
        Assert.AreEqual("[is_active] = 1", sql);
    }

    [TestMethod]
    public void ToWhereClause_BooleanFalse_GeneratesCorrectSql()
    {
        Expression<Func<TestEntity, bool>> predicate = e => e.IsActive == false;
        var sql = predicate.ToWhereClause(SqlDefine.SQLite);
        Assert.AreEqual("[is_active] = 0", sql);
    }

    #endregion

    #region Comparison Operators Tests

    [TestMethod]
    public void ToWhereClause_GreaterThan_GeneratesCorrectSql()
    {
        Expression<Func<TestEntity, bool>> predicate = e => e.Id > 10;
        var sql = predicate.ToWhereClause(SqlDefine.SQLite);
        Assert.AreEqual("[id] > 10", sql);
    }

    [TestMethod]
    public void ToWhereClause_GreaterThanOrEqual_GeneratesCorrectSql()
    {
        Expression<Func<TestEntity, bool>> predicate = e => e.Id >= 10;
        var sql = predicate.ToWhereClause(SqlDefine.SQLite);
        Assert.AreEqual("[id] >= 10", sql);
    }

    [TestMethod]
    public void ToWhereClause_LessThan_GeneratesCorrectSql()
    {
        Expression<Func<TestEntity, bool>> predicate = e => e.Id < 10;
        var sql = predicate.ToWhereClause(SqlDefine.SQLite);
        Assert.AreEqual("[id] < 10", sql);
    }

    [TestMethod]
    public void ToWhereClause_LessThanOrEqual_GeneratesCorrectSql()
    {
        Expression<Func<TestEntity, bool>> predicate = e => e.Id <= 10;
        var sql = predicate.ToWhereClause(SqlDefine.SQLite);
        Assert.AreEqual("[id] <= 10", sql);
    }

    [TestMethod]
    public void ToWhereClause_NotEqual_GeneratesCorrectSql()
    {
        Expression<Func<TestEntity, bool>> predicate = e => e.Id != 10;
        var sql = predicate.ToWhereClause(SqlDefine.SQLite);
        Assert.AreEqual("[id] <> 10", sql);
    }

    #endregion

    #region Logical Operators Tests

    [TestMethod]
    public void ToWhereClause_AndAlso_GeneratesCorrectSql()
    {
        Expression<Func<TestEntity, bool>> predicate = e => e.Id > 5 && e.IsActive == true;
        var sql = predicate.ToWhereClause(SqlDefine.SQLite);
        Assert.AreEqual("([id] > 5 AND [is_active] = 1)", sql);
    }

    [TestMethod]
    public void ToWhereClause_OrElse_GeneratesCorrectSql()
    {
        Expression<Func<TestEntity, bool>> predicate = e => e.Id == 1 || e.Id == 2;
        var sql = predicate.ToWhereClause(SqlDefine.SQLite);
        Assert.AreEqual("([id] = 1 OR [id] = 2)", sql);
    }

    [TestMethod]
    public void ToWhereClause_ComplexLogical_GeneratesCorrectSql()
    {
        Expression<Func<TestEntity, bool>> predicate = e => (e.Id > 5 && e.IsActive == true) || e.Name == "admin";
        var sql = predicate.ToWhereClause(SqlDefine.SQLite);
        Assert.IsTrue(sql.Contains("AND"));
        Assert.IsTrue(sql.Contains("OR"));
    }

    #endregion

    #region Dialect-Specific Tests

    [TestMethod]
    public void ToWhereClause_MySqlDialect_UsesBackticks()
    {
        Expression<Func<TestEntity, bool>> predicate = e => e.Id == 1;
        var sql = predicate.ToWhereClause(SqlDefine.MySql);
        Assert.AreEqual("`id` = 1", sql);
    }

    [TestMethod]
    public void ToWhereClause_PostgreSqlDialect_UsesDoubleQuotes()
    {
        Expression<Func<TestEntity, bool>> predicate = e => e.Id == 1;
        var sql = predicate.ToWhereClause(SqlDefine.PostgreSql);
        Assert.AreEqual("\"id\" = 1", sql);
    }

    [TestMethod]
    public void ToWhereClause_SqlServerDialect_UsesBrackets()
    {
        Expression<Func<TestEntity, bool>> predicate = e => e.Id == 1;
        var sql = predicate.ToWhereClause(SqlDefine.SqlServer);
        Assert.AreEqual("[id] = 1", sql);
    }

    [TestMethod]
    public void ToWhereClause_OracleDialect_NotEqual_UsesBangEquals()
    {
        Expression<Func<TestEntity, bool>> predicate = e => e.Id != 1;
        var sql = predicate.ToWhereClause(SqlDefine.Oracle);
        Assert.AreEqual("\"id\" != 1", sql);
    }

    [TestMethod]
    public void ToWhereClause_PostgreSqlDialect_BooleanTrue_UsesTrue()
    {
        Expression<Func<TestEntity, bool>> predicate = e => e.IsActive == true;
        var sql = predicate.ToWhereClause(SqlDefine.PostgreSql);
        Assert.AreEqual("\"is_active\" = true", sql);
    }

    #endregion

    #region Null Handling Tests

    [TestMethod]
    public void ToWhereClause_NullPredicate_ReturnsEmptyString()
    {
        Expression<Func<TestEntity, bool>>? predicate = null;
        var sql = predicate!.ToWhereClause(SqlDefine.SQLite);
        Assert.AreEqual(string.Empty, sql);
    }

    [TestMethod]
    public void ToWhereClause_DefaultDialect_UsesSQLite()
    {
        Expression<Func<TestEntity, bool>> predicate = e => e.Id == 1;
        var sql = predicate.ToWhereClause();
        Assert.AreEqual("[id] = 1", sql);
    }

    #endregion

    #region Boolean Member Access Tests

    [TestMethod]
    public void ToWhereClause_BooleanMemberDirect_GeneratesCorrectSql()
    {
        Expression<Func<TestEntity, bool>> predicate = e => e.IsActive;
        var sql = predicate.ToWhereClause(SqlDefine.SQLite);
        Assert.AreEqual("[is_active] = 1", sql);
    }

    [TestMethod]
    public void ToWhereClause_NegatedBooleanMember_GeneratesCorrectSql()
    {
        Expression<Func<TestEntity, bool>> predicate = e => !e.IsActive;
        var sql = predicate.ToWhereClause(SqlDefine.SQLite);
        Assert.AreEqual("[is_active] = 0", sql);
    }

    #endregion

    #region All Dialects Coverage Tests

    [TestMethod]
    [DataRow("MySql", "`id` = 1")]
    [DataRow("SqlServer", "[id] = 1")]
    [DataRow("PostgreSql", "\"id\" = 1")]
    [DataRow("SQLite", "[id] = 1")]
    [DataRow("Oracle", "\"id\" = 1")]
    [DataRow("DB2", "\"id\" = 1")]
    public void ToWhereClause_AllDialects_GeneratesCorrectColumnQuoting(string dialectName, string expected)
    {
        var dialect = dialectName switch
        {
            "MySql" => SqlDefine.MySql,
            "SqlServer" => SqlDefine.SqlServer,
            "PostgreSql" => SqlDefine.PostgreSql,
            "SQLite" => SqlDefine.SQLite,
            "Oracle" => SqlDefine.Oracle,
            "DB2" => SqlDefine.DB2,
            _ => throw new ArgumentException($"Unknown dialect: {dialectName}")
        };

        Expression<Func<TestEntity, bool>> predicate = e => e.Id == 1;
        var sql = predicate.ToWhereClause(dialect);
        Assert.AreEqual(expected, sql);
    }

    #endregion

    #region GetParameters Tests

    [TestMethod]
    public void GetParameters_SimpleEquality_ExtractsValue()
    {
        Expression<Func<TestEntity, bool>> predicate = e => e.Id == 42;
        var parameters = predicate.GetParameters();
        Assert.IsTrue(parameters.Count > 0);
        Assert.IsTrue(parameters.ContainsValue(42));
    }

    [TestMethod]
    public void GetParameters_NullPredicate_ReturnsEmptyDictionary()
    {
        Expression<Func<TestEntity, bool>>? predicate = null;
        var parameters = predicate!.GetParameters();
        Assert.AreEqual(0, parameters.Count);
    }

    #endregion
}
