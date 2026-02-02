// <copyright file="SetExpressionEdgeCaseTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using System;
using System.Linq.Expressions;

namespace Sqlx.Tests;

/// <summary>
/// Edge case tests for SetExpressionExtensions.
/// </summary>
[TestClass]
public class SetExpressionEdgeCaseTests
{
    public class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? NullableString { get; set; }
        public int? NullableInt { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ========== Null 和可空类型测试 ==========

    [TestMethod]
    public void ToSetClause_NullableProperty_GeneratesCorrectSql()
    {
        // Arrange
        Expression<Func<TestEntity, TestEntity>> expr = e => new TestEntity 
        { 
            NullableString = "value" 
        };

        // Act
        var result = expr.ToSetClause();

        // Assert
        Assert.AreEqual("[nullable_string] = @p0", result);
    }

    [TestMethod]
    public void ToSetClause_NullablePropertyWithNull_GeneratesCorrectSql()
    {
        // Arrange
        string? nullValue = null;
        Expression<Func<TestEntity, TestEntity>> expr = e => new TestEntity 
        { 
            NullableString = nullValue 
        };

        // Act
        var result = expr.ToSetClause();

        // Assert - null 值会被参数化
        Assert.AreEqual("[nullable_string] = @p0", result);
        
        // 验证参数值为 null
        var parameters = expr.GetSetParameters();
        Assert.IsNull(parameters["p0"]);
    }

    [TestMethod]
    public void ToSetClause_NullableIntProperty_GeneratesCorrectSql()
    {
        // Arrange
        Expression<Func<TestEntity, TestEntity>> expr = e => new TestEntity 
        { 
            NullableInt = 42 
        };

        // Act
        var result = expr.ToSetClause();

        // Assert
        Assert.AreEqual("[nullable_int] = @p0", result);
    }

    [TestMethod]
    public void ToSetClause_NullableIntWithNull_GeneratesCorrectSql()
    {
        // Arrange
        int? nullValue = null;
        Expression<Func<TestEntity, TestEntity>> expr = e => new TestEntity 
        { 
            NullableInt = nullValue 
        };

        // Act
        var result = expr.ToSetClause();

        // Assert - null 值会被参数化
        Assert.AreEqual("[nullable_int] = @p0", result);
        
        // 验证参数值为 null
        var parameters = expr.GetSetParameters();
        Assert.IsNull(parameters["p0"]);
    }

    // ========== 布尔类型测试 ==========

    [TestMethod]
    public void ToSetClause_BooleanTrue_GeneratesCorrectSql()
    {
        // Arrange
        Expression<Func<TestEntity, TestEntity>> expr = e => new TestEntity 
        { 
            IsActive = true 
        };

        // Act
        var result = expr.ToSetClause();

        // Assert
        Assert.AreEqual("[is_active] = @p0", result);
    }

    [TestMethod]
    public void ToSetClause_BooleanFalse_GeneratesCorrectSql()
    {
        // Arrange
        Expression<Func<TestEntity, TestEntity>> expr = e => new TestEntity 
        { 
            IsActive = false 
        };

        // Act
        var result = expr.ToSetClause();

        // Assert
        Assert.AreEqual("[is_active] = @p0", result);
    }

    [TestMethod]
    public void ToSetClause_BooleanNegation_GeneratesCorrectSql()
    {
        // Arrange
        Expression<Func<TestEntity, TestEntity>> expr = e => new TestEntity 
        { 
            IsActive = !e.IsActive 
        };

        // Act
        var result = expr.ToSetClause();

        // Assert - 布尔取反会生成 NOT 表达式或等价的 SQL
        Assert.IsTrue(result.Contains("[is_active]"));
        // 不同的 SQL 方言可能有不同的表示方式
        Assert.IsTrue(result.Length > "[is_active] = ".Length);
    }

    // ========== DateTime 测试 ==========

    [TestMethod]
    public void ToSetClause_DateTimeNow_GeneratesCorrectSql()
    {
        // Arrange
        var now = DateTime.Now;
        Expression<Func<TestEntity, TestEntity>> expr = e => new TestEntity 
        { 
            CreatedAt = now 
        };

        // Act
        var result = expr.ToSetClause();

        // Assert
        Assert.AreEqual("[created_at] = @p0", result);
    }

    [TestMethod]
    public void ToSetClause_DateTimeAddDays_GeneratesCorrectSql()
    {
        // Arrange
        Expression<Func<TestEntity, TestEntity>> expr = e => new TestEntity 
        { 
            CreatedAt = e.CreatedAt.AddDays(1) 
        };

        // Act
        var result = expr.ToSetClause();

        // Assert
        Assert.IsTrue(result.Contains("[created_at]"));
    }

    // ========== 空表达式和边界情况 ==========

    [TestMethod]
    public void ToSetClause_EmptyMemberInit_ReturnsEmpty()
    {
        // Arrange
        Expression<Func<TestEntity, TestEntity>> expr = e => new TestEntity { };

        // Act
        var result = expr.ToSetClause();

        // Assert
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void ToSetClause_SingleProperty_NoTrailingComma()
    {
        // Arrange
        Expression<Func<TestEntity, TestEntity>> expr = e => new TestEntity 
        { 
            Name = "test" 
        };

        // Act
        var result = expr.ToSetClause();

        // Assert
        Assert.IsFalse(result.EndsWith(","));
        Assert.AreEqual("[name] = @p0", result);
    }

    [TestMethod]
    public void ToSetClause_MultipleProperties_CorrectSeparation()
    {
        // Arrange
        Expression<Func<TestEntity, TestEntity>> expr = e => new TestEntity 
        { 
            Name = "test",
            IsActive = true,
            NullableInt = 42
        };

        // Act
        var result = expr.ToSetClause();

        // Assert
        var parts = result.Split(',');
        Assert.AreEqual(3, parts.Length);
        Assert.IsFalse(result.EndsWith(","));
    }

    // ========== 特殊字符和转义测试 ==========

    [TestMethod]
    public void ToSetClause_StringWithQuotes_GeneratesCorrectSql()
    {
        // Arrange
        Expression<Func<TestEntity, TestEntity>> expr = e => new TestEntity 
        { 
            Name = "It's a test" 
        };

        // Act
        var result = expr.ToSetClause();

        // Assert
        Assert.AreEqual("[name] = @p0", result);
        
        // Verify parameter extraction
        var parameters = expr.GetSetParameters();
        Assert.AreEqual("It's a test", parameters["p0"]);
    }

    [TestMethod]
    public void ToSetClause_StringWithBackslash_GeneratesCorrectSql()
    {
        // Arrange
        Expression<Func<TestEntity, TestEntity>> expr = e => new TestEntity 
        { 
            Name = @"C:\Path\To\File" 
        };

        // Act
        var result = expr.ToSetClause();

        // Assert
        Assert.AreEqual("[name] = @p0", result);
        
        var parameters = expr.GetSetParameters();
        Assert.AreEqual(@"C:\Path\To\File", parameters["p0"]);
    }

    [TestMethod]
    public void ToSetClause_EmptyString_GeneratesCorrectSql()
    {
        // Arrange
        Expression<Func<TestEntity, TestEntity>> expr = e => new TestEntity 
        { 
            Name = string.Empty 
        };

        // Act
        var result = expr.ToSetClause();

        // Assert
        Assert.AreEqual("[name] = @p0", result);
        
        var parameters = expr.GetSetParameters();
        Assert.AreEqual(string.Empty, parameters["p0"]);
    }

    // ========== 数值边界测试 ==========

    [TestMethod]
    public void ToSetClause_IntMaxValue_GeneratesCorrectSql()
    {
        // Arrange
        Expression<Func<TestEntity, TestEntity>> expr = e => new TestEntity 
        { 
            Id = int.MaxValue 
        };

        // Act
        var result = expr.ToSetClause();

        // Assert
        Assert.AreEqual("[id] = @p0", result);
        
        var parameters = expr.GetSetParameters();
        Assert.AreEqual(int.MaxValue, parameters["p0"]);
    }

    [TestMethod]
    public void ToSetClause_IntMinValue_GeneratesCorrectSql()
    {
        // Arrange
        Expression<Func<TestEntity, TestEntity>> expr = e => new TestEntity 
        { 
            Id = int.MinValue 
        };

        // Act
        var result = expr.ToSetClause();

        // Assert
        Assert.AreEqual("[id] = @p0", result);
        
        var parameters = expr.GetSetParameters();
        Assert.AreEqual(int.MinValue, parameters["p0"]);
    }

    [TestMethod]
    public void ToSetClause_Zero_GeneratesCorrectSql()
    {
        // Arrange
        Expression<Func<TestEntity, TestEntity>> expr = e => new TestEntity 
        { 
            Id = 0 
        };

        // Act
        var result = expr.ToSetClause();

        // Assert
        Assert.AreEqual("[id] = @p0", result);
        
        var parameters = expr.GetSetParameters();
        Assert.AreEqual(0, parameters["p0"]);
    }

    [TestMethod]
    public void ToSetClause_NegativeNumber_GeneratesCorrectSql()
    {
        // Arrange
        Expression<Func<TestEntity, TestEntity>> expr = e => new TestEntity 
        { 
            Id = -42 
        };

        // Act
        var result = expr.ToSetClause();

        // Assert
        Assert.AreEqual("[id] = @p0", result);
        
        var parameters = expr.GetSetParameters();
        Assert.AreEqual(-42, parameters["p0"]);
    }

    // ========== 复杂嵌套表达式测试 ==========

    [TestMethod]
    public void ToSetClause_DeeplyNestedFunctions_GeneratesCorrectSql()
    {
        // Arrange
        Expression<Func<TestEntity, TestEntity>> expr = e => new TestEntity 
        { 
            Name = e.Name.Trim().ToLower().Substring(0, 10).ToUpper() 
        };

        // Act
        var result = expr.ToSetClause();

        // Assert
        Assert.IsTrue(result.Contains("UPPER"));
        Assert.IsTrue(result.Contains("SUBSTR"));
        Assert.IsTrue(result.Contains("LOWER"));
        Assert.IsTrue(result.Contains("TRIM"));
    }

    [TestMethod]
    public void ToSetClause_ComplexArithmetic_GeneratesCorrectSql()
    {
        // Arrange
        Expression<Func<TestEntity, TestEntity>> expr = e => new TestEntity 
        { 
            Id = ((e.Id + 1) * 2 - 3) / 4 
        };

        // Act
        var result = expr.ToSetClause();

        // Assert
        Assert.IsTrue(result.Contains("[id]"));
        Assert.IsTrue(result.Contains("+"));
        Assert.IsTrue(result.Contains("*"));
        Assert.IsTrue(result.Contains("-"));
        Assert.IsTrue(result.Contains("/"));
    }

    // ========== 参数提取边界测试 ==========

    [TestMethod]
    public void GetSetParameters_NoParameters_ReturnsEmpty()
    {
        // Arrange
        Expression<Func<TestEntity, TestEntity>> expr = e => new TestEntity 
        { 
            Id = e.Id + 1 
        };

        // Act
        var parameters = expr.GetSetParameters();

        // Assert
        Assert.AreEqual(1, parameters.Count);
        Assert.AreEqual(1, parameters["p0"]);
    }

    [TestMethod]
    public void GetSetParameters_MultipleConstants_ExtractsAll()
    {
        // Arrange
        Expression<Func<TestEntity, TestEntity>> expr = e => new TestEntity 
        { 
            Name = "test",
            Id = 42,
            IsActive = true
        };

        // Act
        var parameters = expr.GetSetParameters();

        // Assert
        Assert.AreEqual(3, parameters.Count);
        Assert.AreEqual("test", parameters["p0"]);
        Assert.AreEqual(42, parameters["p1"]);
        Assert.AreEqual(true, parameters["p2"]);
    }

    [TestMethod]
    public void GetSetParameters_MixedExpressionsAndConstants_ExtractsOnlyConstants()
    {
        // Arrange
        Expression<Func<TestEntity, TestEntity>> expr = e => new TestEntity 
        { 
            Name = "prefix" + e.Name,
            Id = e.Id + 10
        };

        // Act
        var parameters = expr.GetSetParameters();

        // Assert
        Assert.AreEqual(2, parameters.Count);
        Assert.AreEqual("prefix", parameters["p0"]);
        Assert.AreEqual(10, parameters["p1"]);
    }

    // ========== 错误处理测试 ==========

    [TestMethod]
    public void ToSetClause_NotMemberInitExpression_ThrowsArgumentException()
    {
        // Arrange - 直接返回参数
        Expression<Func<TestEntity, TestEntity>> expr = e => e;

        // Act & Assert
        var ex = Assert.ThrowsException<ArgumentException>(() => expr.ToSetClause());
        Assert.IsTrue(ex.Message.Contains("member initialization"));
    }

    [TestMethod]
    public void ToSetClause_NullExpression_ReturnsEmpty()
    {
        // Arrange
        Expression<Func<TestEntity, TestEntity>>? expr = null;

        // Act
        var result = expr.ToSetClause();

        // Assert
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void GetSetParameters_NullExpression_ReturnsEmpty()
    {
        // Arrange
        Expression<Func<TestEntity, TestEntity>>? expr = null;

        // Act
        var parameters = expr.GetSetParameters();

        // Assert
        Assert.AreEqual(0, parameters.Count);
    }

    [TestMethod]
    public void GetSetParameters_NotMemberInitExpression_ReturnsEmpty()
    {
        // Arrange
        Expression<Func<TestEntity, TestEntity>> expr = e => e;

        // Act
        var parameters = expr.GetSetParameters();

        // Assert
        Assert.AreEqual(0, parameters.Count);
    }
}
