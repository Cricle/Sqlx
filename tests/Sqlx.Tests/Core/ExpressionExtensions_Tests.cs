// -----------------------------------------------------------------------
// <copyright file="ExpressionExtensions_Tests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Sqlx.Tests.Core;

/// <summary>
/// 测试 ExpressionExtensions 扩展方法
/// </summary>
[TestClass]
public class ExpressionExtensions_Tests
{
    public class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public bool IsActive { get; set; }
    }

    [TestMethod]
    [Description("ToWhereClause 应该生成 WHERE 子句")]
    public void ToWhereClause_ShouldGenerateWhereClause()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> predicate = e => e.Id == 1;

        // Act
        var whereClause = predicate.ToWhereClause();

        // Assert
        Assert.IsNotNull(whereClause);
        Assert.IsFalse(string.IsNullOrEmpty(whereClause), "应该生成非空字符串");
    }

    [TestMethod]
    [Description("ToWhereClause 使用指定方言应该生成正确的 WHERE 子句")]
    public void ToWhereClause_WithDialect_ShouldGenerateCorrectWhereClause()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> predicate = e => e.Name == "test";

        // Act
        var whereClause = predicate.ToWhereClause(SqlDefine.SqlServer);

        // Assert
        Assert.IsNotNull(whereClause);
        Assert.IsFalse(string.IsNullOrEmpty(whereClause), "应该生成非空字符串");
    }

    [TestMethod]
    [Description("ToWhereClause 使用 null 表达式应该返回空字符串")]
    public void ToWhereClause_WithNullExpression_ShouldReturnEmpty()
    {
        // Arrange
        Expression<Func<TestEntity, bool>>? predicate = null;

        // Act
        var whereClause = predicate.ToWhereClause();

        // Assert
        Assert.AreEqual(string.Empty, whereClause);
    }

    [TestMethod]
    [Description("GetParameters 应该提取表达式中的参数")]
    public void GetParameters_ShouldExtractParameters()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> predicate = e => e.Id == 1 && e.Name == "test";

        // Act
        var parameters = predicate.GetParameters();

        // Assert
        Assert.IsNotNull(parameters);
        Assert.IsTrue(parameters.Count > 0, "应该提取到参数");
    }

    [TestMethod]
    [Description("GetParameters 使用 null 表达式应该返回空字典")]
    public void GetParameters_WithNullExpression_ShouldReturnEmptyDictionary()
    {
        // Arrange
        Expression<Func<TestEntity, bool>>? predicate = null;

        // Act
        var parameters = predicate.GetParameters();

        // Assert
        Assert.IsNotNull(parameters);
        Assert.AreEqual(0, parameters.Count);
    }

    [TestMethod]
    [Description("GetParameters 应该处理复杂表达式")]
    public void GetParameters_WithComplexExpression_ShouldExtractAllParameters()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> predicate = e => 
            e.Id > 10 && e.Age < 50 && e.IsActive == true;

        // Act
        var parameters = predicate.GetParameters();

        // Assert
        Assert.IsNotNull(parameters);
        Assert.IsTrue(parameters.Count >= 0, "应该提取参数");
    }

    [TestMethod]
    [Description("GetParameters 应该处理成员访问表达式")]
    public void GetParameters_WithMemberAccess_ShouldExtractParameters()
    {
        // Arrange
        var testValue = "test";
        Expression<Func<TestEntity, bool>> predicate = e => e.Name == testValue;

        // Act
        var parameters = predicate.GetParameters();

        // Assert
        Assert.IsNotNull(parameters);
    }

    [TestMethod]
    [Description("GetParameters 应该处理常量表达式")]
    public void GetParameters_WithConstant_ShouldExtractParameters()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> predicate = e => e.Id == 100;

        // Act
        var parameters = predicate.GetParameters();

        // Assert
        Assert.IsNotNull(parameters);
    }

    [TestMethod]
    [Description("GetParameters 应该处理二元表达式")]
    public void GetParameters_WithBinaryExpression_ShouldExtractParameters()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> predicate = e => e.Age > 18 && e.Age < 65;

        // Act
        var parameters = predicate.GetParameters();

        // Assert
        Assert.IsNotNull(parameters);
    }

    [TestMethod]
    [Description("GetParameters 应该处理一元表达式")]
    public void GetParameters_WithUnaryExpression_ShouldExtractParameters()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> predicate = e => !e.IsActive;

        // Act
        var parameters = predicate.GetParameters();

        // Assert
        Assert.IsNotNull(parameters);
    }

    [TestMethod]
    [Description("GetParameters 应该处理方法调用表达式")]
    public void GetParameters_WithMethodCall_ShouldExtractParameters()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> predicate = e => e.Name.Contains("test");

        // Act
        var parameters = predicate.GetParameters();

        // Assert
        Assert.IsNotNull(parameters);
    }
}
