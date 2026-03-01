// <copyright file="SetExpressionFunctionTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using System;
using System.Linq.Expressions;

namespace Sqlx.Tests;

/// <summary>
/// Tests for SetExpressionExtensions with string and math functions.
/// </summary>
[TestClass]
public class SetExpressionFunctionTests
{
    public class TestUser
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Age { get; set; }
        public decimal Salary { get; set; }
        public double Score { get; set; }
    }

    // ========== 字符串函数测试 ==========

    [TestMethod]
    public void ToSetClause_StringToLower_GeneratesCorrectSql()
    {
        // Arrange
        Expression<Func<TestUser, TestUser>> expr = u => new TestUser 
        { 
            Email = u.Email.ToLower() 
        };

        // Act
        var result = expr.ToSetClause();

        // Assert - SQLite uses LOWER()
        Assert.IsTrue(result.Contains("LOWER"));
        Assert.IsTrue(result.Contains("[email]"));
    }

    [TestMethod]
    public void ToSetClause_StringToUpper_GeneratesCorrectSql()
    {
        // Arrange
        Expression<Func<TestUser, TestUser>> expr = u => new TestUser 
        { 
            Name = u.Name.ToUpper() 
        };

        // Act
        var result = expr.ToSetClause();

        // Assert - SQLite uses UPPER()
        Assert.IsTrue(result.Contains("UPPER"));
        Assert.IsTrue(result.Contains("[name]"));
    }

    [TestMethod]
    public void ToSetClause_StringTrim_GeneratesCorrectSql()
    {
        // Arrange
        Expression<Func<TestUser, TestUser>> expr = u => new TestUser 
        { 
            Name = u.Name.Trim() 
        };

        // Act
        var result = expr.ToSetClause();

        // Assert - SQLite uses TRIM()
        Assert.IsTrue(result.Contains("TRIM"));
        Assert.IsTrue(result.Contains("[name]"));
    }

    [TestMethod]
    public void ToSetClause_StringSubstring_GeneratesCorrectSql()
    {
        // Arrange
        Expression<Func<TestUser, TestUser>> expr = u => new TestUser 
        { 
            Name = u.Name.Substring(0, 10) 
        };

        // Act
        var result = expr.ToSetClause();

        // Assert - SQLite uses SUBSTR()
        Assert.IsTrue(result.Contains("SUBSTR") || result.Contains("SUBSTRING"));
        Assert.IsTrue(result.Contains("[name]"));
    }

    [TestMethod]
    public void ToSetClause_StringReplace_GeneratesCorrectSql()
    {
        // Arrange
        Expression<Func<TestUser, TestUser>> expr = u => new TestUser 
        { 
            Name = u.Name.Replace("old", "new") 
        };

        // Act
        var result = expr.ToSetClause();

        // Assert - SQLite uses REPLACE()
        Assert.IsTrue(result.Contains("REPLACE"));
        Assert.IsTrue(result.Contains("[name]"));
    }

    [TestMethod]
    public void ToSetClause_StringConcat_GeneratesCorrectSql()
    {
        // Arrange
        Expression<Func<TestUser, TestUser>> expr = u => new TestUser 
        { 
            Name = u.Name + " (updated)" 
        };

        // Act
        var result = expr.ToSetClause();

        // Assert - SQLite uses || for concatenation
        Assert.IsTrue(result.Contains("[name]"));
        Assert.IsTrue(result.Contains("||") || result.Contains("CONCAT"));
    }

    // ========== 数学函数测试 ==========

    [TestMethod]
    public void ToSetClause_MathAbs_GeneratesCorrectSql()
    {
        // Arrange
        Expression<Func<TestUser, TestUser>> expr = u => new TestUser 
        { 
            Age = Math.Abs(u.Age) 
        };

        // Act
        var result = expr.ToSetClause();

        // Assert
        Assert.IsTrue(result.Contains("ABS"));
        Assert.IsTrue(result.Contains("[age]"));
    }

    [TestMethod]
    public void ToSetClause_MathRound_GeneratesCorrectSql()
    {
        // Arrange
        Expression<Func<TestUser, TestUser>> expr = u => new TestUser 
        { 
            Score = Math.Round(u.Score) 
        };

        // Act
        var result = expr.ToSetClause();

        // Assert
        Assert.IsTrue(result.Contains("ROUND"));
        Assert.IsTrue(result.Contains("[score]"));
    }

    [TestMethod]
    public void ToSetClause_MathCeiling_GeneratesCorrectSql()
    {
        // Arrange
        Expression<Func<TestUser, TestUser>> expr = u => new TestUser 
        { 
            Score = Math.Ceiling(u.Score) 
        };

        // Act
        var result = expr.ToSetClause();

        // Assert
        Assert.IsTrue(result.Contains("CEIL"));
        Assert.IsTrue(result.Contains("[score]"));
    }

    [TestMethod]
    public void ToSetClause_MathFloor_GeneratesCorrectSql()
    {
        // Arrange
        Expression<Func<TestUser, TestUser>> expr = u => new TestUser 
        { 
            Score = Math.Floor(u.Score) 
        };

        // Act
        var result = expr.ToSetClause();

        // Assert
        Assert.IsTrue(result.Contains("FLOOR"));
        Assert.IsTrue(result.Contains("[score]"));
    }

    [TestMethod]
    public void ToSetClause_MathPow_GeneratesCorrectSql()
    {
        // Arrange
        Expression<Func<TestUser, TestUser>> expr = u => new TestUser 
        { 
            Score = Math.Pow(u.Score, 2) 
        };

        // Act
        var result = expr.ToSetClause();

        // Assert
        Assert.IsTrue(result.Contains("POWER") || result.Contains("POW"));
        Assert.IsTrue(result.Contains("[score]"));
    }

    [TestMethod]
    public void ToSetClause_MathSqrt_GeneratesCorrectSql()
    {
        // Arrange
        Expression<Func<TestUser, TestUser>> expr = u => new TestUser 
        { 
            Score = Math.Sqrt(u.Score) 
        };

        // Act
        var result = expr.ToSetClause();

        // Assert
        Assert.IsTrue(result.Contains("SQRT"));
        Assert.IsTrue(result.Contains("[score]"));
    }

    // ========== 复杂组合测试 ==========

    [TestMethod]
    public void ToSetClause_MixedStringAndMath_GeneratesCorrectSql()
    {
        // Arrange
        Expression<Func<TestUser, TestUser>> expr = u => new TestUser 
        { 
            Name = u.Name.ToUpper(),
            Age = Math.Abs(u.Age) + 1
        };

        // Act
        var result = expr.ToSetClause();

        // Assert
        Assert.IsTrue(result.Contains("UPPER"));
        Assert.IsTrue(result.Contains("ABS"));
        Assert.IsTrue(result.Contains("[name]"));
        Assert.IsTrue(result.Contains("[age]"));
    }

    [TestMethod]
    public void ToSetClause_NestedFunctions_GeneratesCorrectSql()
    {
        // Arrange
        Expression<Func<TestUser, TestUser>> expr = u => new TestUser 
        { 
            Name = u.Name.Trim().ToLower()
        };

        // Act
        var result = expr.ToSetClause();

        // Assert
        Assert.IsTrue(result.Contains("LOWER"));
        Assert.IsTrue(result.Contains("TRIM"));
        Assert.IsTrue(result.Contains("[name]"));
    }

    [TestMethod]
    public void ToSetClause_ComplexMathExpression_GeneratesCorrectSql()
    {
        // Arrange
        Expression<Func<TestUser, TestUser>> expr = u => new TestUser 
        { 
            Score = Math.Round(u.Score * 1.1) 
        };

        // Act
        var result = expr.ToSetClause();

        // Assert
        Assert.IsTrue(result.Contains("ROUND"));
        Assert.IsTrue(result.Contains("[score]"));
    }

    // ========== 方言测试 ==========

    [TestMethod]
    public void ToSetClause_StringFunctions_PostgreSQL_GeneratesCorrectSql()
    {
        // Arrange
        Expression<Func<TestUser, TestUser>> expr = u => new TestUser 
        { 
            Name = u.Name.ToLower() 
        };

        // Act
        var result = expr.ToSetClause(SqlDefine.PostgreSql);

        // Assert
        Assert.IsTrue(result.Contains("LOWER"));
        Assert.IsTrue(result.Contains("\"name\""));
    }

    [TestMethod]
    public void ToSetClause_MathFunctions_MySQL_GeneratesCorrectSql()
    {
        // Arrange
        Expression<Func<TestUser, TestUser>> expr = u => new TestUser 
        { 
            Age = Math.Abs(u.Age) 
        };

        // Act
        var result = expr.ToSetClause(SqlDefine.MySql);

        // Assert
        Assert.IsTrue(result.Contains("ABS"));
        Assert.IsTrue(result.Contains("`age`"));
    }
}
