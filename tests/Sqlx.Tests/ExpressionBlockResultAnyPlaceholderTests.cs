// <copyright file="ExpressionBlockResultAnyPlaceholderTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using Sqlx.Expressions;
using Sqlx.Tests.Helpers;
using System;
using System.Linq.Expressions;

namespace Sqlx.Tests;

/// <summary>
/// Tests for ExpressionBlockResult with Any placeholder support.
/// </summary>
[TestClass]
public class ExpressionBlockResultAnyPlaceholderTests
{
    public class User
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public bool IsActive { get; set; }
        public string? Email { get; set; }
    }

    [TestMethod]
    public void Parse_WithSingleAnyPlaceholder_GeneratesCorrectSql()
    {
        // Arrange
        Expression<Func<User, bool>> predicate = u => u.Age > Any.Value<int>("minAge");

        // Act
        var result = ExpressionBlockResult.Parse(predicate.Body, SqlDefine.SQLite);

        // Assert
        Assert.AreEqual("[age] > @minAge", result.Sql);
        Assert.AreEqual(0, result.Parameters.Count); // No parameters yet
        Assert.AreEqual(1, result.GetPlaceholderNames().Count);
        Assert.AreEqual("minAge", result.GetPlaceholderNames()[0]);
        Assert.IsFalse(result.AreAllPlaceholdersFilled());
    }

    [TestMethod]
    public void Parse_WithSingleAnyPlaceholder_WithParameter_FillsParameter()
    {
        // Arrange
        Expression<Func<User, bool>> predicate = u => u.Age > Any.Value<int>("minAge");

        // Act
        var result = ExpressionBlockResult.Parse(predicate.Body, SqlDefine.SQLite)
            .WithParameter("minAge", 18);

        // Assert
        Assert.AreEqual("[age] > @minAge", result.Sql);
        Assert.AreEqual(1, result.Parameters.Count);
        SqlAssertions.AssertParametersContain(result.Parameters, "@minAge", 18);
        Assert.IsTrue(result.AreAllPlaceholdersFilled());
    }

    [TestMethod]
    public void Parse_WithMultipleAnyPlaceholders_GeneratesCorrectSql()
    {
        // Arrange
        Expression<Func<User, bool>> predicate = u => 
            u.Age > Any.Value<int>("minAge") && u.Age < Any.Value<int>("maxAge");

        // Act
        var result = ExpressionBlockResult.Parse(predicate.Body, SqlDefine.SQLite);

        // Assert
        Assert.AreEqual("([age] > @minAge AND [age] < @maxAge)", result.Sql);
        Assert.AreEqual(0, result.Parameters.Count);
        Assert.AreEqual(2, result.GetPlaceholderNames().Count);
        Assert.IsTrue(result.GetPlaceholderNames().Contains("minAge"));
        Assert.IsTrue(result.GetPlaceholderNames().Contains("maxAge"));
    }

    [TestMethod]
    public void Parse_WithMultipleAnyPlaceholders_WithParameters_FillsAllParameters()
    {
        // Arrange
        Expression<Func<User, bool>> predicate = u => 
            u.Age > Any.Value<int>("minAge") && u.Age < Any.Value<int>("maxAge");

        // Act
        var result = ExpressionBlockResult.Parse(predicate.Body, SqlDefine.SQLite)
            .WithParameter("minAge", 18)
            .WithParameter("maxAge", 65);

        // Assert
        Assert.AreEqual("([age] > @minAge AND [age] < @maxAge)", result.Sql);
        Assert.AreEqual(2, result.Parameters.Count);
        SqlAssertions.AssertParametersContain(result.Parameters, "@minAge", 18);
        SqlAssertions.AssertParametersContain(result.Parameters, "@maxAge", 65);
        Assert.IsTrue(result.AreAllPlaceholdersFilled());
    }

    [TestMethod]
    public void Parse_WithMixedAnyAndConstant_GeneratesCorrectSql()
    {
        // Arrange
        var constantAge = 30;
        Expression<Func<User, bool>> predicate = u => 
            u.Age > Any.Value<int>("minAge") && u.Age < constantAge;

        // Act
        var result = ExpressionBlockResult.Parse(predicate.Body, SqlDefine.SQLite)
            .WithParameter("minAge", 18);

        // Assert
        Assert.AreEqual("([age] > @minAge AND [age] < @p0)", result.Sql);
        Assert.AreEqual(2, result.Parameters.Count);
        SqlAssertions.AssertParametersContain(result.Parameters, "@minAge", 18);
        SqlAssertions.AssertParametersContain(result.Parameters, "@p0", 30);
    }

    [TestMethod]
    public void Parse_WithStringAnyPlaceholder_GeneratesCorrectSql()
    {
        // Arrange
        Expression<Func<User, bool>> predicate = u => u.Name == Any.Value<string>("userName");

        // Act
        var result = ExpressionBlockResult.Parse(predicate.Body, SqlDefine.SQLite)
            .WithParameter("userName", "John");

        // Assert
        Assert.AreEqual("[name] = @userName", result.Sql);
        Assert.AreEqual(1, result.Parameters.Count);
        SqlAssertions.AssertParametersContain(result.Parameters, "@userName", "John");
    }

    [TestMethod]
    public void Parse_WithComplexExpression_GeneratesCorrectSql()
    {
        // Arrange
        Expression<Func<User, bool>> predicate = u => 
            (u.Age > Any.Value<int>("minAge") && u.Age < Any.Value<int>("maxAge")) || 
            u.Name == Any.Value<string>("userName");

        // Act
        var result = ExpressionBlockResult.Parse(predicate.Body, SqlDefine.SQLite)
            .WithParameter("minAge", 18)
            .WithParameter("maxAge", 65)
            .WithParameter("userName", "John");

        // Assert
        Assert.AreEqual("(([age] > @minAge AND [age] < @maxAge) OR [name] = @userName)", result.Sql);
        Assert.AreEqual(3, result.Parameters.Count);
        SqlAssertions.AssertParametersContain(result.Parameters, "@minAge", 18);
        SqlAssertions.AssertParametersContain(result.Parameters, "@maxAge", 65);
        SqlAssertions.AssertParametersContain(result.Parameters, "@userName", "John");
    }

    [TestMethod]
    public void ParseUpdate_WithAnyPlaceholder_GeneratesCorrectSql()
    {
        // Arrange
        Expression<Func<User, User>> updateExpr = u => new User 
        { 
            Age = Any.Value<int>("newAge")
        };

        // Act
        var result = ExpressionBlockResult.ParseUpdate(updateExpr, SqlDefine.SQLite)
            .WithParameter("newAge", 25);

        // Assert
        Assert.AreEqual("[age] = @newAge", result.Sql);
        Assert.AreEqual(1, result.Parameters.Count);
        SqlAssertions.AssertParametersContain(result.Parameters, "@newAge", 25);
    }

    [TestMethod]
    public void ParseUpdate_WithMultipleAnyPlaceholders_GeneratesCorrectSql()
    {
        // Arrange
        Expression<Func<User, User>> updateExpr = u => new User 
        { 
            Name = Any.Value<string>("newName"),
            Age = Any.Value<int>("newAge")
        };

        // Act
        var result = ExpressionBlockResult.ParseUpdate(updateExpr, SqlDefine.SQLite)
            .WithParameter("newName", "Jane")
            .WithParameter("newAge", 30);

        // Assert
        Assert.AreEqual("[name] = @newName, [age] = @newAge", result.Sql);
        Assert.AreEqual(2, result.Parameters.Count);
        SqlAssertions.AssertParametersContain(result.Parameters, "@newName", "Jane");
        SqlAssertions.AssertParametersContain(result.Parameters, "@newAge", 30);
    }

    [TestMethod]
    public void WithParameters_WithDictionary_FillsAllParameters()
    {
        // Arrange
        Expression<Func<User, bool>> predicate = u => 
            u.Age > Any.Value<int>("minAge") && u.Name == Any.Value<string>("userName");

        var parameters = new System.Collections.Generic.Dictionary<string, object?>
        {
            ["minAge"] = 18,
            ["userName"] = "John"
        };

        // Act
        var result = ExpressionBlockResult.Parse(predicate.Body, SqlDefine.SQLite)
            .WithParameters(parameters);

        // Assert
        Assert.AreEqual("([age] > @minAge AND [name] = @userName)", result.Sql);
        Assert.AreEqual(2, result.Parameters.Count);
        SqlAssertions.AssertParametersContain(result.Parameters, "@minAge", 18);
        SqlAssertions.AssertParametersContain(result.Parameters, "@userName", "John");
        Assert.IsTrue(result.AreAllPlaceholdersFilled());
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void WithParameter_InvalidPlaceholderName_ThrowsException()
    {
        // Arrange
        Expression<Func<User, bool>> predicate = u => u.Age > Any.Value<int>("minAge");

        // Act
        var result = ExpressionBlockResult.Parse(predicate.Body, SqlDefine.SQLite)
            .WithParameter("invalidName", 18); // Should throw
    }

    [TestMethod]
    public void Parse_PostgreSQLDialect_UsesCorrectParameterPrefix()
    {
        // Arrange
        Expression<Func<User, bool>> predicate = u => u.Age > Any.Value<int>("minAge");

        // Act
        var result = ExpressionBlockResult.Parse(predicate.Body, SqlDefine.PostgreSql)
            .WithParameter("minAge", 18);

        // Assert
        Assert.AreEqual("\"age\" > @minAge", result.Sql);
        Assert.AreEqual(1, result.Parameters.Count);
        SqlAssertions.AssertParametersContain(result.Parameters, "@minAge", 18);
    }

    [TestMethod]
    public void Parse_MySQLDialect_UsesCorrectParameterPrefix()
    {
        // Arrange
        Expression<Func<User, bool>> predicate = u => u.Age > Any.Value<int>("minAge");

        // Act
        var result = ExpressionBlockResult.Parse(predicate.Body, SqlDefine.MySql)
            .WithParameter("minAge", 18);

        // Assert
        Assert.AreEqual("`age` > @minAge", result.Sql);
        Assert.AreEqual(1, result.Parameters.Count);
        SqlAssertions.AssertParametersContain(result.Parameters, "@minAge", 18);
    }

    [TestMethod]
    public void Parse_OracleDialect_UsesCorrectParameterPrefix()
    {
        // Arrange
        Expression<Func<User, bool>> predicate = u => u.Age > Any.Value<int>("minAge");

        // Act
        var result = ExpressionBlockResult.Parse(predicate.Body, SqlDefine.Oracle)
            .WithParameter("minAge", 18);

        // Assert
        Assert.AreEqual("\"age\" > :minAge", result.Sql);
        Assert.AreEqual(1, result.Parameters.Count);
        SqlAssertions.AssertParametersContain(result.Parameters, ":minAge", 18);
    }

    [TestMethod]
    public void Parse_WithNullPlaceholderValue_HandlesCorrectly()
    {
        // Arrange
        Expression<Func<User, bool>> predicate = u => u.Email == Any.Value<string?>("email");

        // Act
        var result = ExpressionBlockResult.Parse(predicate.Body, SqlDefine.SQLite)
            .WithParameter("email", null);

        // Assert
        Assert.AreEqual("[email] = @email", result.Sql);
        Assert.AreEqual(1, result.Parameters.Count);
        SqlAssertions.AssertParametersContain(result.Parameters, "@email", null);
    }

    [TestMethod]
    public void AnyValue_DirectCall_ThrowsException()
    {
        // Act & Assert
        var exception = Assert.ThrowsException<InvalidOperationException>(() => 
            Any.Value<int>("test"));

        Assert.IsTrue(exception.Message.Contains("placeholder"));
        Assert.IsTrue(exception.Message.Contains("should not be executed directly"));
    }
}
