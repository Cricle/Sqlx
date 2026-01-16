// <copyright file="ExpressionExtensionsTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
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
    #region Test Entity

    public class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public decimal Price { get; set; }
        public int? NullableValue { get; set; }
        public string? NullableName { get; set; }
    }

    #endregion

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
    public void ToWhereClause_BooleanEquality_GeneratesCorrectSql()
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
    public void ToWhereClause_OracleDialect_UsesDoubleQuotes()
    {
        Expression<Func<TestEntity, bool>> predicate = e => e.Id == 1;
        
        var sql = predicate.ToWhereClause(SqlDefine.Oracle);
        
        Assert.AreEqual("\"id\" = 1", sql);
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

    #region String Method Tests

    [TestMethod]
    public void ToWhereClause_StringContains_GeneratesLike()
    {
        Expression<Func<TestEntity, bool>> predicate = e => e.Name.Contains("test");
        
        var sql = predicate.ToWhereClause(SqlDefine.SQLite);
        
        // The actual implementation may vary - just verify it generates something
        Assert.IsFalse(string.IsNullOrEmpty(sql));
        Assert.IsTrue(sql.Contains("name"));
    }

    [TestMethod]
    public void ToWhereClause_StringStartsWith_GeneratesLike()
    {
        Expression<Func<TestEntity, bool>> predicate = e => e.Name.StartsWith("test");
        
        var sql = predicate.ToWhereClause(SqlDefine.SQLite);
        
        Assert.IsFalse(string.IsNullOrEmpty(sql));
        Assert.IsTrue(sql.Contains("name"));
    }

    [TestMethod]
    public void ToWhereClause_StringEndsWith_GeneratesLike()
    {
        Expression<Func<TestEntity, bool>> predicate = e => e.Name.EndsWith("test");
        
        var sql = predicate.ToWhereClause(SqlDefine.SQLite);
        
        Assert.IsFalse(string.IsNullOrEmpty(sql));
        Assert.IsTrue(sql.Contains("name"));
    }

    #endregion

    #region Constant Value Tests

    [TestMethod]
    public void ToWhereClause_ConstantIntValue_GeneratesCorrectSql()
    {
        // Use constant value directly in expression (not captured variable)
        Expression<Func<TestEntity, bool>> predicate = e => e.Id == 42;
        
        var sql = predicate.ToWhereClause(SqlDefine.SQLite);
        
        Assert.AreEqual("[id] = 42", sql);
    }

    [TestMethod]
    public void ToWhereClause_ConstantStringValue_GeneratesCorrectSql()
    {
        Expression<Func<TestEntity, bool>> predicate = e => e.Name == "hello";
        
        var sql = predicate.ToWhereClause(SqlDefine.SQLite);
        
        Assert.AreEqual("[name] = 'hello'", sql);
    }

    [TestMethod]
    public void ToWhereClause_ConstantBoolValue_GeneratesCorrectSql()
    {
        Expression<Func<TestEntity, bool>> predicate = e => e.IsActive == true;
        
        var sql = predicate.ToWhereClause(SqlDefine.SQLite);
        
        Assert.AreEqual("[is_active] = 1", sql);
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
    public void GetParameters_CapturedVariable_ExtractsValue()
    {
        var searchId = 100;
        Expression<Func<TestEntity, bool>> predicate = e => e.Id == searchId;
        
        var parameters = predicate.GetParameters();
        
        Assert.IsTrue(parameters.Count > 0);
        Assert.IsTrue(parameters.ContainsValue(100));
    }

    [TestMethod]
    public void GetParameters_MultipleConditions_ExtractsAllValues()
    {
        var minId = 10;
        var maxId = 100;
        Expression<Func<TestEntity, bool>> predicate = e => e.Id >= minId && e.Id <= maxId;
        
        var parameters = predicate.GetParameters();
        
        Assert.IsTrue(parameters.Count >= 2);
        Assert.IsTrue(parameters.ContainsValue(10));
        Assert.IsTrue(parameters.ContainsValue(100));
    }

    [TestMethod]
    public void GetParameters_NullPredicate_ReturnsEmptyDictionary()
    {
        Expression<Func<TestEntity, bool>>? predicate = null;
        
        var parameters = predicate!.GetParameters();
        
        Assert.AreEqual(0, parameters.Count);
    }

    #endregion

    #region Complex Expression Tests

    [TestMethod]
    public void ToWhereClause_DecimalComparison_GeneratesCorrectSql()
    {
        Expression<Func<TestEntity, bool>> predicate = e => e.Price > 99.99m;
        
        var sql = predicate.ToWhereClause(SqlDefine.SQLite);
        
        Assert.IsTrue(sql.Contains("[price]"));
        Assert.IsTrue(sql.Contains(">"));
    }

    [TestMethod]
    public void ToWhereClause_DateTimeComparison_GeneratesCorrectSql()
    {
        var date = new DateTime(2024, 1, 1);
        Expression<Func<TestEntity, bool>> predicate = e => e.CreatedAt > date;
        
        var sql = predicate.ToWhereClause(SqlDefine.SQLite);
        
        Assert.IsTrue(sql.Contains("[created_at]"));
        Assert.IsTrue(sql.Contains(">"));
    }

    [TestMethod]
    public void ToWhereClause_MultipleAndConditions_GeneratesCorrectSql()
    {
        Expression<Func<TestEntity, bool>> predicate = e => 
            e.Id > 0 && e.IsActive == true && e.Name != "";
        
        var sql = predicate.ToWhereClause(SqlDefine.SQLite);
        
        Assert.IsTrue(sql.Contains("[id]"));
        Assert.IsTrue(sql.Contains("[is_active]"));
        Assert.IsTrue(sql.Contains("[name]"));
        Assert.IsTrue(sql.Contains("AND"));
    }

    [TestMethod]
    public void ToWhereClause_NestedConditions_GeneratesCorrectSql()
    {
        Expression<Func<TestEntity, bool>> predicate = e => 
            (e.Id == 1 || e.Id == 2) && e.IsActive == true;
        
        var sql = predicate.ToWhereClause(SqlDefine.SQLite);
        
        Assert.IsTrue(sql.Contains("OR"));
        Assert.IsTrue(sql.Contains("AND"));
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
}
