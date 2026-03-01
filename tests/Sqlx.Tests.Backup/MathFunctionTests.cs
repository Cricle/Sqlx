// -----------------------------------------------------------------------
// <copyright file="MathFunctionTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace Sqlx.Tests;

/// <summary>
/// Tests for Math function expressions in LINQ queries.
/// </summary>
[TestClass]
public class MathFunctionTests
{
    public class MathEntity
    {
        public int Id { get; set; }
        public double Value { get; set; }
        public double Value2 { get; set; }
        public int IntValue { get; set; }
    }

    [TestMethod]
    public void MathAbs_GeneratesCorrectSql()
    {
        // Arrange
        var dialect = SqlDefine.SQLite;
        Expression<Func<MathEntity, bool>> expr = x => Math.Abs(x.Value) > 10;

        // Act
        var sql = ExpressionExtensions.ToWhereClause(expr, dialect);

        // Assert
        Assert.IsTrue(sql.Contains("ABS"), "Should contain ABS function");
        Assert.IsTrue(sql.Contains("value") || sql.Contains("Value"), "Should reference value column");
    }

    [TestMethod]
    public void MathSign_GeneratesCorrectSql()
    {
        // Arrange
        var dialect = SqlDefine.SQLite;
        Expression<Func<MathEntity, bool>> expr = x => Math.Sign(x.Value) == 1;

        // Act
        var sql = ExpressionExtensions.ToWhereClause(expr, dialect);

        // Assert
        Assert.IsTrue(sql.Contains("SIGN"), "Should contain SIGN function");
    }

    [TestMethod]
    public void MathRound_GeneratesCorrectSql()
    {
        // Arrange
        var dialect = SqlDefine.SQLite;
        Expression<Func<MathEntity, bool>> expr = x => Math.Round(x.Value) > 5;

        // Act
        var sql = ExpressionExtensions.ToWhereClause(expr, dialect);

        // Assert
        Assert.IsTrue(sql.Contains("ROUND"), "Should contain ROUND function");
    }

    [TestMethod]
    public void MathRoundWithDecimals_GeneratesCorrectSql()
    {
        // Arrange
        var dialect = SqlDefine.SQLite;
        Expression<Func<MathEntity, bool>> expr = x => Math.Round(x.Value, 2) > 5.5;

        // Act
        var sql = ExpressionExtensions.ToWhereClause(expr, dialect);

        // Assert
        Assert.IsTrue(sql.Contains("ROUND"), "Should contain ROUND function");
        Assert.IsTrue(sql.Contains("2"), "Should include decimal places");
    }

    [TestMethod]
    public void MathFloor_GeneratesCorrectSql()
    {
        // Arrange
        var dialect = SqlDefine.SQLite;
        Expression<Func<MathEntity, bool>> expr = x => Math.Floor(x.Value) == 10;

        // Act
        var sql = ExpressionExtensions.ToWhereClause(expr, dialect);

        // Assert
        Assert.IsTrue(sql.Contains("FLOOR") || sql.Contains("floor"), "Should contain FLOOR function");
    }

    [TestMethod]
    public void MathCeiling_GeneratesCorrectSql()
    {
        // Arrange
        var dialect = SqlDefine.SQLite;
        Expression<Func<MathEntity, bool>> expr = x => Math.Ceiling(x.Value) == 11;

        // Act
        var sql = ExpressionExtensions.ToWhereClause(expr, dialect);

        // Assert
        Assert.IsTrue(sql.Contains("CEIL"), "Should contain CEIL function");
    }

    [TestMethod]
    public void MathTruncate_GeneratesCorrectSql()
    {
        // Arrange
        var dialect = SqlDefine.SQLite;
        Expression<Func<MathEntity, bool>> expr = x => Math.Truncate(x.Value) == 10;

        // Act
        var sql = ExpressionExtensions.ToWhereClause(expr, dialect);

        // Assert
        Assert.IsTrue(sql.Contains("TRUNCATE") || sql.Contains("TRUNC"), "Should contain TRUNCATE function");
    }

    [TestMethod]
    public void MathSqrt_GeneratesCorrectSql()
    {
        // Arrange
        var dialect = SqlDefine.SQLite;
        Expression<Func<MathEntity, bool>> expr = x => Math.Sqrt(x.Value) > 3;

        // Act
        var sql = ExpressionExtensions.ToWhereClause(expr, dialect);

        // Assert
        Assert.IsTrue(sql.Contains("SQRT"), "Should contain SQRT function");
    }

    [TestMethod]
    public void MathPow_GeneratesCorrectSql()
    {
        // Arrange
        var dialect = SqlDefine.SQLite;
        Expression<Func<MathEntity, bool>> expr = x => Math.Pow(x.Value, 2) > 100;

        // Act
        var sql = ExpressionExtensions.ToWhereClause(expr, dialect);

        // Assert
        Assert.IsTrue(sql.Contains("POWER") || sql.Contains("POW"), "Should contain POWER function");
    }

    [TestMethod]
    public void MathExp_GeneratesCorrectSql()
    {
        // Arrange
        var dialect = SqlDefine.SQLite;
        Expression<Func<MathEntity, bool>> expr = x => Math.Exp(x.Value) > 10;

        // Act
        var sql = ExpressionExtensions.ToWhereClause(expr, dialect);

        // Assert
        Assert.IsTrue(sql.Contains("EXP"), "Should contain EXP function");
    }

    [TestMethod]
    public void MathLog_GeneratesCorrectSql()
    {
        // Arrange
        var dialect = SqlDefine.SQLite;
        Expression<Func<MathEntity, bool>> expr = x => Math.Log(x.Value) > 1;

        // Act
        var sql = ExpressionExtensions.ToWhereClause(expr, dialect);

        // Assert
        Assert.IsTrue(sql.Contains("LOG") || sql.Contains("LN"), "Should contain LOG function");
    }

    [TestMethod]
    public void MathLogWithBase_GeneratesCorrectSql()
    {
        // Arrange
        var dialect = SqlDefine.SQLite;
        Expression<Func<MathEntity, bool>> expr = x => Math.Log(x.Value, 2) > 3;

        // Act
        var sql = ExpressionExtensions.ToWhereClause(expr, dialect);

        // Assert
        Assert.IsTrue(sql.Contains("LOG"), "Should contain LOG function");
    }

    [TestMethod]
    public void MathLog10_GeneratesCorrectSql()
    {
        // Arrange
        var dialect = SqlDefine.SQLite;
        Expression<Func<MathEntity, bool>> expr = x => Math.Log10(x.Value) > 2;

        // Act
        var sql = ExpressionExtensions.ToWhereClause(expr, dialect);

        // Assert
        Assert.IsTrue(sql.Contains("LOG10"), "Should contain LOG10 function");
    }

    [TestMethod]
    public void MathSin_GeneratesCorrectSql()
    {
        // Arrange
        var dialect = SqlDefine.SQLite;
        Expression<Func<MathEntity, bool>> expr = x => Math.Sin(x.Value) > 0.5;

        // Act
        var sql = ExpressionExtensions.ToWhereClause(expr, dialect);

        // Assert
        Assert.IsTrue(sql.Contains("SIN"), "Should contain SIN function");
    }

    [TestMethod]
    public void MathCos_GeneratesCorrectSql()
    {
        // Arrange
        var dialect = SqlDefine.SQLite;
        Expression<Func<MathEntity, bool>> expr = x => Math.Cos(x.Value) > 0.5;

        // Act
        var sql = ExpressionExtensions.ToWhereClause(expr, dialect);

        // Assert
        Assert.IsTrue(sql.Contains("COS"), "Should contain COS function");
    }

    [TestMethod]
    public void MathTan_GeneratesCorrectSql()
    {
        // Arrange
        var dialect = SqlDefine.SQLite;
        Expression<Func<MathEntity, bool>> expr = x => Math.Tan(x.Value) > 1;

        // Act
        var sql = ExpressionExtensions.ToWhereClause(expr, dialect);

        // Assert
        Assert.IsTrue(sql.Contains("TAN"), "Should contain TAN function");
    }

    [TestMethod]
    public void MathAsin_GeneratesCorrectSql()
    {
        // Arrange
        var dialect = SqlDefine.SQLite;
        Expression<Func<MathEntity, bool>> expr = x => Math.Asin(x.Value) > 0.5;

        // Act
        var sql = ExpressionExtensions.ToWhereClause(expr, dialect);

        // Assert
        Assert.IsTrue(sql.Contains("ASIN"), "Should contain ASIN function");
    }

    [TestMethod]
    public void MathAcos_GeneratesCorrectSql()
    {
        // Arrange
        var dialect = SqlDefine.SQLite;
        Expression<Func<MathEntity, bool>> expr = x => Math.Acos(x.Value) > 0.5;

        // Act
        var sql = ExpressionExtensions.ToWhereClause(expr, dialect);

        // Assert
        Assert.IsTrue(sql.Contains("ACOS"), "Should contain ACOS function");
    }

    [TestMethod]
    public void MathAtan_GeneratesCorrectSql()
    {
        // Arrange
        var dialect = SqlDefine.SQLite;
        Expression<Func<MathEntity, bool>> expr = x => Math.Atan(x.Value) > 0.5;

        // Act
        var sql = ExpressionExtensions.ToWhereClause(expr, dialect);

        // Assert
        Assert.IsTrue(sql.Contains("ATAN"), "Should contain ATAN function");
    }

    [TestMethod]
    public void MathAtan2_GeneratesCorrectSql()
    {
        // Arrange
        var dialect = SqlDefine.SQLite;
        Expression<Func<MathEntity, bool>> expr = x => Math.Atan2(x.Value, x.Value2) > 0.5;

        // Act
        var sql = ExpressionExtensions.ToWhereClause(expr, dialect);

        // Assert
        Assert.IsTrue(sql.Contains("ATAN2") || sql.Contains("ATN2"), "Should contain ATAN2 function");
    }

    [TestMethod]
    public void MathMin_GeneratesCorrectSql()
    {
        // Arrange
        var dialect = SqlDefine.SQLite;
        Expression<Func<MathEntity, bool>> expr = x => Math.Min(x.Value, x.Value2) > 5;

        // Act
        var sql = ExpressionExtensions.ToWhereClause(expr, dialect);

        // Assert
        Assert.IsTrue(sql.Contains("LEAST"), "Should contain LEAST function");
    }

    [TestMethod]
    public void MathMax_GeneratesCorrectSql()
    {
        // Arrange
        var dialect = SqlDefine.SQLite;
        Expression<Func<MathEntity, bool>> expr = x => Math.Max(x.Value, x.Value2) < 100;

        // Act
        var sql = ExpressionExtensions.ToWhereClause(expr, dialect);

        // Assert
        Assert.IsTrue(sql.Contains("GREATEST"), "Should contain GREATEST function");
    }

    [TestMethod]
    public void MathFunctions_DifferentDialects_GenerateCorrectSql()
    {
        // Test with different dialects
        var dialects = new[] 
        { 
            SqlDefine.SQLite, 
            SqlDefine.MySql, 
            SqlDefine.SqlServer, 
            SqlDefine.PostgreSql 
        };

        Expression<Func<MathEntity, bool>> expr = x => Math.Abs(x.Value) > 10;

        foreach (var dialect in dialects)
        {
            // Act
            var sql = ExpressionExtensions.ToWhereClause(expr, dialect);

            // Assert
            Assert.IsTrue(sql.Contains("ABS"), $"Should contain ABS for {dialect.DatabaseType}");
        }
    }

    [TestMethod]
    public void MathFunctions_ComplexExpression_GeneratesCorrectSql()
    {
        // Arrange
        var dialect = SqlDefine.SQLite;
        Expression<Func<MathEntity, bool>> expr = x => 
            Math.Sqrt(Math.Pow(x.Value, 2) + Math.Pow(x.Value2, 2)) > 10;

        // Act
        var sql = ExpressionExtensions.ToWhereClause(expr, dialect);

        // Assert
        Assert.IsTrue(sql.Contains("SQRT"), "Should contain SQRT function");
        Assert.IsTrue(sql.Contains("POWER") || sql.Contains("POW"), "Should contain POWER function");
    }
}
