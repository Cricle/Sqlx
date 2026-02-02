// <copyright file="SetExpressionFunctionOutputTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using System;
using System.Linq.Expressions;

namespace Sqlx.Tests;

/// <summary>
/// Tests to verify exact SQL output for function expressions.
/// </summary>
[TestClass]
public class SetExpressionFunctionOutputTests
{
    public class TestUser
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Age { get; set; }
        public double Score { get; set; }
    }

    [TestMethod]
    public void ToSetClause_StringToLower_ExactOutput()
    {
        // Arrange
        Expression<Func<TestUser, TestUser>> expr = u => new TestUser 
        { 
            Email = u.Email.ToLower() 
        };

        // Act
        var result = expr.ToSetClause();

        // Assert
        Console.WriteLine($"Generated SQL: {result}");
        Assert.AreEqual("[email] = LOWER([email])", result);
    }

    [TestMethod]
    public void ToSetClause_StringToUpper_ExactOutput()
    {
        // Arrange
        Expression<Func<TestUser, TestUser>> expr = u => new TestUser 
        { 
            Name = u.Name.ToUpper() 
        };

        // Act
        var result = expr.ToSetClause();

        // Assert
        Console.WriteLine($"Generated SQL: {result}");
        Assert.AreEqual("[name] = UPPER([name])", result);
    }

    [TestMethod]
    public void ToSetClause_StringTrim_ExactOutput()
    {
        // Arrange
        Expression<Func<TestUser, TestUser>> expr = u => new TestUser 
        { 
            Name = u.Name.Trim() 
        };

        // Act
        var result = expr.ToSetClause();

        // Assert
        Console.WriteLine($"Generated SQL: {result}");
        Assert.AreEqual("[name] = TRIM([name])", result);
    }

    [TestMethod]
    public void ToSetClause_MathAbs_ExactOutput()
    {
        // Arrange
        Expression<Func<TestUser, TestUser>> expr = u => new TestUser 
        { 
            Age = Math.Abs(u.Age) 
        };

        // Act
        var result = expr.ToSetClause();

        // Assert
        Console.WriteLine($"Generated SQL: {result}");
        Assert.AreEqual("[age] = ABS([age])", result);
    }

    [TestMethod]
    public void ToSetClause_MathRound_ExactOutput()
    {
        // Arrange
        Expression<Func<TestUser, TestUser>> expr = u => new TestUser 
        { 
            Score = Math.Round(u.Score) 
        };

        // Act
        var result = expr.ToSetClause();

        // Assert
        Console.WriteLine($"Generated SQL: {result}");
        Assert.AreEqual("[score] = ROUND([score])", result);
    }

    [TestMethod]
    public void ToSetClause_StringConcat_ExactOutput()
    {
        // Arrange
        Expression<Func<TestUser, TestUser>> expr = u => new TestUser 
        { 
            Name = u.Name + " (updated)" 
        };

        // Act
        var result = expr.ToSetClause();

        // Assert
        Console.WriteLine($"Generated SQL: {result}");
        // SQLite uses || for concatenation (without extra parentheses)
        Assert.AreEqual("[name] = [name] || @p0", result);
    }

    [TestMethod]
    public void ToSetClause_ComplexExpression_ExactOutput()
    {
        // Arrange
        Expression<Func<TestUser, TestUser>> expr = u => new TestUser 
        { 
            Name = u.Name.Trim().ToLower(),
            Age = Math.Abs(u.Age) + 1
        };

        // Act
        var result = expr.ToSetClause();

        // Assert
        Console.WriteLine($"Generated SQL: {result}");
        Assert.AreEqual("[name] = LOWER(TRIM([name])), [age] = (ABS([age]) + @p0)", result);
    }

    [TestMethod]
    public void ToSetClause_MixedWithConstants_ExactOutput()
    {
        // Arrange
        Expression<Func<TestUser, TestUser>> expr = u => new TestUser 
        { 
            Name = "Prefix: " + u.Name.ToUpper(),
            Age = Math.Max(u.Age, 18)
        };

        // Act
        var result = expr.ToSetClause();

        // Assert
        Console.WriteLine($"Generated SQL: {result}");
        // SQLite uses GREATEST for Math.Max
        Assert.AreEqual("[name] = @p0 || UPPER([name]), [age] = GREATEST([age], @p1)", result);
    }
}
