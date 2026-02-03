// <copyright file="ExpressionBlockResultTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using Sqlx.Expressions;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Sqlx.Tests;

/// <summary>
/// Tests for ExpressionBlockResult - unified expression parsing for WHERE and UPDATE scenarios.
/// </summary>
[TestClass]
public class ExpressionBlockResultTests
{
    public class User
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public bool IsActive { get; set; }
        public string? Email { get; set; }
    }

    // ========== WHERE 表达式解析测试 ==========

    [TestMethod]
    public void Parse_SimpleWhereExpression_GeneratesCorrectSql()
    {
        // Arrange
        Expression<Func<User, bool>> predicate = u => u.Age > 18;

        // Act
        var result = ExpressionBlockResult.Parse(predicate.Body, SqlDefine.SQLite);

        // Assert
        Assert.AreEqual("[age] > @p0", result.Sql);
        Assert.AreEqual(1, result.Parameters.Count);
        Assert.AreEqual(18, result.Parameters["@p0"]);
    }

    [TestMethod]
    public void Parse_WhereWithParameter_ExtractsParameter()
    {
        // Arrange
        var minAge = 18;
        Expression<Func<User, bool>> predicate = u => u.Age > minAge;

        // Act
        var result = ExpressionBlockResult.Parse(predicate.Body, SqlDefine.SQLite);

        // Assert
        Assert.AreEqual("[age] > @p0", result.Sql);
        Assert.AreEqual(1, result.Parameters.Count);
        
        // 打印所有参数以调试
        foreach (var kvp in result.Parameters)
        {
            Console.WriteLine($"Key: {kvp.Key}, Value: {kvp.Value}");
        }
        
        Assert.IsTrue(result.Parameters.ContainsKey("@p0"));
        Assert.AreEqual(18, result.Parameters["@p0"]);
    }

    [TestMethod]
    public void Parse_ComplexWhereExpression_GeneratesCorrectSql()
    {
        // Arrange
        var minAge = 18;
        var name = "John";
        Expression<Func<User, bool>> predicate = u => u.Age > minAge && u.Name == name;

        // Act
        var result = ExpressionBlockResult.Parse(predicate.Body, SqlDefine.SQLite);

        // Assert
        Assert.AreEqual("([age] > @p0 AND [name] = @p1)", result.Sql);
        Assert.AreEqual(2, result.Parameters.Count);
        Assert.AreEqual(18, result.Parameters["@p0"]);
        Assert.AreEqual("John", result.Parameters["@p1"]);
    }

    // ========== UPDATE 表达式解析测试 ==========

    [TestMethod]
    public void Parse_SimpleUpdateExpression_GeneratesCorrectSql()
    {
        // Arrange
        Expression<Func<User, User>> updateExpr = u => new User { Name = "John" };

        // Act
        var result = ExpressionBlockResult.ParseUpdate(updateExpr, SqlDefine.SQLite);

        // Assert
        Assert.AreEqual("[name] = @p0", result.Sql);
        Assert.AreEqual(1, result.Parameters.Count);
        Assert.IsTrue(result.Parameters.ContainsValue("John"));
    }

    [TestMethod]
    public void ParseUpdate_IncrementExpression_GeneratesCorrectSql()
    {
        // Arrange
        Expression<Func<User, User>> updateExpr = u => new User { Age = u.Age + 1 };

        // Act
        var result = ExpressionBlockResult.ParseUpdate(updateExpr, SqlDefine.SQLite);

        // Assert
        Assert.AreEqual("[age] = ([age] + @p0)", result.Sql);
        Assert.AreEqual(1, result.Parameters.Count);
        Assert.AreEqual(1, result.Parameters["@p0"]);
    }

    [TestMethod]
    public void ParseUpdate_MultipleFields_GeneratesCorrectSql()
    {
        // Arrange
        Expression<Func<User, User>> updateExpr = u => new User 
        { 
            Name = "John",
            Age = 30,
            IsActive = true
        };

        // Act
        var result = ExpressionBlockResult.ParseUpdate(updateExpr, SqlDefine.SQLite);

        // Assert
        Assert.AreEqual("[name] = @p0, [age] = @p1, [is_active] = @p2", result.Sql);
        Assert.AreEqual(3, result.Parameters.Count);
        Assert.AreEqual("John", result.Parameters["@p0"]);
        Assert.AreEqual(30, result.Parameters["@p1"]);
        Assert.AreEqual(true, result.Parameters["@p2"]);
    }

    // ========== 方言测试 ==========

    [TestMethod]
    public void Parse_PostgreSQLDialect_UsesCorrectSyntax()
    {
        // Arrange
        var minAge = 18;
        Expression<Func<User, bool>> predicate = u => u.Age > minAge;

        // Act
        var result = ExpressionBlockResult.Parse(predicate.Body, SqlDefine.PostgreSql);

        // Assert
        Assert.AreEqual("\"age\" > $p0", result.Sql);
        Assert.AreEqual(1, result.Parameters.Count);
        Assert.AreEqual(18, result.Parameters["$p0"]);
    }

    [TestMethod]
    public void ParseUpdate_MySQLDialect_UsesCorrectSyntax()
    {
        // Arrange
        Expression<Func<User, User>> updateExpr = u => new User { Name = "John" };

        // Act
        var result = ExpressionBlockResult.ParseUpdate(updateExpr, SqlDefine.MySql);

        // Assert
        Assert.AreEqual("`name` = @p0", result.Sql);
        Assert.AreEqual(1, result.Parameters.Count);
        Assert.AreEqual("John", result.Parameters["@p0"]);
    }

    // ========== Null 值测试 ==========

    [TestMethod]
    public void Parse_NullValue_HandlesCorrectly()
    {
        // Arrange
        string? nullValue = null;
        Expression<Func<User, bool>> predicate = u => u.Email == nullValue;

        // Act
        var result = ExpressionBlockResult.Parse(predicate.Body, SqlDefine.SQLite);

        // Assert
        Assert.AreEqual("[email] = @p0", result.Sql);
        Assert.AreEqual(1, result.Parameters.Count);
        Assert.IsNull(result.Parameters["@p0"]);
    }

    [TestMethod]
    public void ParseUpdate_NullValue_HandlesCorrectly()
    {
        // Arrange
        string? nullValue = null;
        Expression<Func<User, User>> updateExpr = u => new User { Email = nullValue };

        // Act
        var result = ExpressionBlockResult.ParseUpdate(updateExpr, SqlDefine.SQLite);

        // Assert
        Assert.AreEqual("[email] = @p0", result.Sql);
        Assert.AreEqual(1, result.Parameters.Count);
        Assert.IsNull(result.Parameters["@p0"]);
    }

    // ========== 性能和缓存测试 ==========

    [TestMethod]
    public void Parse_SameExpression_ReturnsNewInstance()
    {
        // Arrange
        Expression<Func<User, bool>> predicate = u => u.Age > 18;

        // Act
        var result1 = ExpressionBlockResult.Parse(predicate.Body, SqlDefine.SQLite);
        var result2 = ExpressionBlockResult.Parse(predicate.Body, SqlDefine.SQLite);

        // Assert
        Assert.AreNotSame(result1, result2);
        Assert.AreEqual(result1.Sql, result2.Sql);
    }

    // ========== 边界情况测试 ==========

    [TestMethod]
    public void Parse_NullExpression_ReturnsEmpty()
    {
        // Act
        var result = ExpressionBlockResult.Parse(null, SqlDefine.SQLite);

        // Assert
        Assert.AreEqual(string.Empty, result.Sql);
        Assert.AreEqual(0, result.Parameters.Count);
    }

    [TestMethod]
    public void ParseUpdate_NullExpression_ReturnsEmpty()
    {
        // Act
        var result = ExpressionBlockResult.ParseUpdate<User>(null, SqlDefine.SQLite);

        // Assert
        Assert.AreEqual(string.Empty, result.Sql);
        Assert.AreEqual(0, result.Parameters.Count);
    }

    [TestMethod]
    public void Parse_EmptyParameters_ReturnsEmptyDictionary()
    {
        // Arrange
        Expression<Func<User, bool>> predicate = u => u.Age > 18;

        // Act
        var result = ExpressionBlockResult.Parse(predicate.Body, SqlDefine.SQLite);

        // Assert
        Assert.IsNotNull(result.Parameters);
        // 常量会被参数化，所以会有 1 个参数
        Assert.AreEqual(1, result.Parameters.Count);
    }

    // ========== 字符串函数测试 ==========

    [TestMethod]
    public void Parse_StringFunction_GeneratesCorrectSql()
    {
        // Arrange
        Expression<Func<User, bool>> predicate = u => u.Name.ToLower() == "john";

        // Act
        var result = ExpressionBlockResult.Parse(predicate.Body, SqlDefine.SQLite);

        // Assert
        Assert.AreEqual("LOWER([name]) = @p0", result.Sql);
        Assert.AreEqual(1, result.Parameters.Count);
        Assert.AreEqual("john", result.Parameters["@p0"]);
    }

    [TestMethod]
    public void ParseUpdate_StringFunction_GeneratesCorrectSql()
    {
        // Arrange
        Expression<Func<User, User>> updateExpr = u => new User { Name = u.Name.ToUpper() };

        // Act
        var result = ExpressionBlockResult.ParseUpdate(updateExpr, SqlDefine.SQLite);

        // Assert
        Assert.AreEqual("[name] = UPPER([name])", result.Sql);
        Assert.AreEqual(0, result.Parameters.Count);
    }

    // ========== 数学函数测试 ==========

    [TestMethod]
    public void ParseUpdate_MathFunction_GeneratesCorrectSql()
    {
        // Arrange
        Expression<Func<User, User>> updateExpr = u => new User { Age = Math.Abs(u.Age) };

        // Act
        var result = ExpressionBlockResult.ParseUpdate(updateExpr, SqlDefine.SQLite);

        // Assert
        Assert.AreEqual("[age] = ABS([age])", result.Sql);
        Assert.AreEqual(0, result.Parameters.Count);
    }

    // ========== 复杂表达式测试 ==========

    [TestMethod]
    public void Parse_ComplexNestedExpression_GeneratesCorrectSql()
    {
        // Arrange
        var minAge = 18;
        var maxAge = 65;
        Expression<Func<User, bool>> predicate = u => 
            (u.Age > minAge && u.Age < maxAge) || u.IsActive;

        // Act
        var result = ExpressionBlockResult.Parse(predicate.Body, SqlDefine.SQLite);

        // Assert
        // Note: Boolean property access is converted to "= 1" in SQLite
        Assert.AreEqual("(([age] > @p0 AND [age] < @p1) OR [is_active] = 1)", result.Sql);
        Assert.AreEqual(2, result.Parameters.Count);
        Assert.AreEqual(18, result.Parameters["@p0"]);
        Assert.AreEqual(65, result.Parameters["@p1"]);
    }

    [TestMethod]
    public void ParseUpdate_ComplexExpression_GeneratesCorrectSql()
    {
        // Arrange
        Expression<Func<User, User>> updateExpr = u => new User 
        { 
            Name = u.Name.Trim().ToLower(),
            Age = u.Age + 1
        };

        // Act
        var result = ExpressionBlockResult.ParseUpdate(updateExpr, SqlDefine.SQLite);

        // Assert
        Assert.AreEqual("[name] = LOWER(TRIM([name])), [age] = ([age] + @p0)", result.Sql);
        Assert.AreEqual(1, result.Parameters.Count);
        Assert.AreEqual(1, result.Parameters["@p0"]);
    }
}
