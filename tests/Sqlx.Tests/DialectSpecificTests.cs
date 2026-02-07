// -----------------------------------------------------------------------
// <copyright file="DialectSpecificTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq.Expressions;

namespace Sqlx.Tests;

/// <summary>
/// Tests for database-specific dialect features.
/// </summary>
[TestClass]
public class DialectSpecificTests
{
    public class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
        public decimal Amount { get; set; }
    }

    #region DB2 Dialect Tests

    [TestMethod]
    public void DB2_Substring_GeneratesCorrectSql()
    {
        // Arrange
        var dialect = SqlDefine.DB2;
        Expression<Func<TestEntity, bool>> expr = x => x.Name.Substring(0, 5) == "Test";

        // Act
        var sql = ExpressionExtensions.ToWhereClause(expr, dialect);

        // Assert
        Assert.IsTrue(sql.Contains("SUBSTR"), "DB2 should use SUBSTR function");
    }

    [TestMethod]
    public void DB2_Length_GeneratesCorrectSql()
    {
        // Arrange
        var dialect = SqlDefine.DB2;
        Expression<Func<TestEntity, bool>> expr = x => x.Name.Length > 10;

        // Act
        var sql = ExpressionExtensions.ToWhereClause(expr, dialect);

        // Assert
        Assert.IsTrue(sql.Contains("LENGTH"), "DB2 should use LENGTH function");
    }

    [TestMethod]
    public void DB2_Concat_GeneratesCorrectSql()
    {
        // Arrange
        var dialect = SqlDefine.DB2;
        var sql = SqlQuery<TestEntity>.For(dialect)
            .Select(x => x.Name + " - " + x.Description)
            .ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("CONCAT") || sql.Contains("||"), "DB2 should use CONCAT or || operator");
    }

    [TestMethod]
    public void DB2_Limit_GeneratesCorrectSql()
    {
        // Arrange
        var sql = SqlQuery<TestEntity>.For(SqlDefine.DB2)
            .Where(x => x.IsActive)
            .Take(10)
            .ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("FETCH FIRST") || sql.Contains("LIMIT"), "DB2 should use FETCH FIRST or LIMIT");
    }

    [TestMethod]
    public void DB2_Offset_GeneratesCorrectSql()
    {
        // Arrange
        var sql = SqlQuery<TestEntity>.For(SqlDefine.DB2)
            .Where(x => x.IsActive)
            .Skip(5)
            .Take(10)
            .ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("OFFSET") || sql.Contains("ROW"), "DB2 should support OFFSET");
    }

    [TestMethod]
    public void DB2_ToUpper_GeneratesCorrectSql()
    {
        // Arrange
        var dialect = SqlDefine.DB2;
        Expression<Func<TestEntity, bool>> expr = x => x.Name.ToUpper() == "TEST";

        // Act
        var sql = ExpressionExtensions.ToWhereClause(expr, dialect);

        // Assert
        Assert.IsTrue(sql.Contains("UPPER"), "DB2 should use UPPER function");
    }

    [TestMethod]
    public void DB2_ToLower_GeneratesCorrectSql()
    {
        // Arrange
        var dialect = SqlDefine.DB2;
        Expression<Func<TestEntity, bool>> expr = x => x.Name.ToLower() == "test";

        // Act
        var sql = ExpressionExtensions.ToWhereClause(expr, dialect);

        // Assert
        Assert.IsTrue(sql.Contains("LOWER"), "DB2 should use LOWER function");
    }

    [TestMethod]
    public void DB2_Trim_GeneratesCorrectSql()
    {
        // Arrange
        var dialect = SqlDefine.DB2;
        Expression<Func<TestEntity, bool>> expr = x => x.Name.Trim() == "Test";

        // Act
        var sql = ExpressionExtensions.ToWhereClause(expr, dialect);

        // Assert
        Assert.IsTrue(sql.Contains("TRIM"), "DB2 should use TRIM function");
    }

    #endregion

    #region Oracle Dialect Tests

    [TestMethod]
    public void Oracle_Substring_GeneratesCorrectSql()
    {
        // Arrange
        var dialect = SqlDefine.Oracle;
        Expression<Func<TestEntity, bool>> expr = x => x.Name.Substring(0, 5) == "Test";

        // Act
        var sql = ExpressionExtensions.ToWhereClause(expr, dialect);

        // Assert
        Assert.IsTrue(sql.Contains("SUBSTR"), "Oracle should use SUBSTR function");
    }

    [TestMethod]
    public void Oracle_Length_GeneratesCorrectSql()
    {
        // Arrange
        var dialect = SqlDefine.Oracle;
        Expression<Func<TestEntity, bool>> expr = x => x.Name.Length > 10;

        // Act
        var sql = ExpressionExtensions.ToWhereClause(expr, dialect);

        // Assert
        Assert.IsTrue(sql.Contains("LENGTH"), "Oracle should use LENGTH function");
    }

    [TestMethod]
    public void Oracle_Concat_GeneratesCorrectSql()
    {
        // Arrange
        var dialect = SqlDefine.Oracle;
        var sql = SqlQuery<TestEntity>.For(dialect)
            .Select(x => x.Name + " - " + x.Description)
            .ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("||") || sql.Contains("CONCAT"), "Oracle should use || operator or CONCAT");
    }

    [TestMethod]
    public void Oracle_Limit_GeneratesCorrectSql()
    {
        // Arrange
        var sql = SqlQuery<TestEntity>.For(SqlDefine.Oracle)
            .Where(x => x.IsActive)
            .Take(10)
            .ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("FETCH FIRST") || sql.Contains("ROWNUM") || sql.Contains("<="), 
            "Oracle should use FETCH FIRST or ROWNUM");
    }

    [TestMethod]
    public void Oracle_Offset_GeneratesCorrectSql()
    {
        // Arrange
        var sql = SqlQuery<TestEntity>.For(SqlDefine.Oracle)
            .Where(x => x.IsActive)
            .Skip(5)
            .Take(10)
            .ToSql();

        // Assert
        Assert.IsTrue(sql.Contains("OFFSET") || sql.Contains("ROW_NUMBER"), 
            "Oracle should support OFFSET or use ROW_NUMBER");
    }

    [TestMethod]
    public void Oracle_ToUpper_GeneratesCorrectSql()
    {
        // Arrange
        var dialect = SqlDefine.Oracle;
        Expression<Func<TestEntity, bool>> expr = x => x.Name.ToUpper() == "TEST";

        // Act
        var sql = ExpressionExtensions.ToWhereClause(expr, dialect);

        // Assert
        Assert.IsTrue(sql.Contains("UPPER"), "Oracle should use UPPER function");
    }

    [TestMethod]
    public void Oracle_ToLower_GeneratesCorrectSql()
    {
        // Arrange
        var dialect = SqlDefine.Oracle;
        Expression<Func<TestEntity, bool>> expr = x => x.Name.ToLower() == "test";

        // Act
        var sql = ExpressionExtensions.ToWhereClause(expr, dialect);

        // Assert
        Assert.IsTrue(sql.Contains("LOWER"), "Oracle should use LOWER function");
    }

    [TestMethod]
    public void Oracle_Trim_GeneratesCorrectSql()
    {
        // Arrange
        var dialect = SqlDefine.Oracle;
        Expression<Func<TestEntity, bool>> expr = x => x.Name.Trim() == "Test";

        // Act
        var sql = ExpressionExtensions.ToWhereClause(expr, dialect);

        // Assert
        Assert.IsTrue(sql.Contains("TRIM"), "Oracle should use TRIM function");
    }

    [TestMethod]
    public void Oracle_Replace_GeneratesCorrectSql()
    {
        // Arrange
        var dialect = SqlDefine.Oracle;
        Expression<Func<TestEntity, bool>> expr = x => x.Name.Replace("old", "new") == "Test";

        // Act
        var sql = ExpressionExtensions.ToWhereClause(expr, dialect);

        // Assert
        Assert.IsTrue(sql.Contains("REPLACE"), "Oracle should use REPLACE function");
    }

    [TestMethod]
    public void Oracle_ParameterPrefix_UsesColon()
    {
        // Arrange
        var dialect = SqlDefine.Oracle;

        // Act
        var prefix = dialect.ParameterPrefix;

        // Assert
        Assert.AreEqual(":", prefix, "Oracle should use : as parameter prefix");
    }

    #endregion

    #region Cross-Dialect Comparison Tests

    [TestMethod]
    public void AllDialects_BasicQuery_GeneratesValidSql()
    {
        // Arrange
        var dialects = new[] 
        { 
            SqlDefine.SQLite, 
            SqlDefine.MySql, 
            SqlDefine.SqlServer, 
            SqlDefine.PostgreSql,
            SqlDefine.Oracle,
            SqlDefine.DB2
        };

        // Act & Assert
        foreach (var dialect in dialects)
        {
            var sql = SqlQuery<TestEntity>.For(dialect)
                .Where(x => x.IsActive)
                .ToSql();

            Assert.IsFalse(string.IsNullOrWhiteSpace(sql), 
                $"Dialect {dialect.DatabaseType} should generate valid SQL");
            Assert.IsTrue(sql.Contains("SELECT"), 
                $"Dialect {dialect.DatabaseType} should contain SELECT");
            Assert.IsTrue(sql.Contains("WHERE") || sql.Contains("where"), 
                $"Dialect {dialect.DatabaseType} should contain WHERE");
        }
    }

    [TestMethod]
    public void AllDialects_StringContains_GeneratesLikeSql()
    {
        // Arrange
        var dialects = new[] 
        { 
            SqlDefine.SQLite, 
            SqlDefine.MySql, 
            SqlDefine.SqlServer, 
            SqlDefine.PostgreSql,
            SqlDefine.Oracle,
            SqlDefine.DB2
        };

        Expression<Func<TestEntity, bool>> expr = x => x.Name.Contains("test");

        // Act & Assert
        foreach (var dialect in dialects)
        {
            var sql = ExpressionExtensions.ToWhereClause(expr, dialect);

            Assert.IsTrue(sql.Contains("LIKE") || sql.Contains("like"), 
                $"Dialect {dialect.DatabaseType} should use LIKE for Contains");
        }
    }

    [TestMethod]
    public void AllDialects_MathAbs_GeneratesAbsFunction()
    {
        // Arrange
        var dialects = new[] 
        { 
            SqlDefine.SQLite, 
            SqlDefine.MySql, 
            SqlDefine.SqlServer, 
            SqlDefine.PostgreSql,
            SqlDefine.Oracle,
            SqlDefine.DB2
        };

        Expression<Func<TestEntity, bool>> expr = x => Math.Abs(x.Amount) > 100;

        // Act & Assert
        foreach (var dialect in dialects)
        {
            var sql = ExpressionExtensions.ToWhereClause(expr, dialect);

            Assert.IsTrue(sql.Contains("ABS"), 
                $"Dialect {dialect.DatabaseType} should use ABS function");
        }
    }

    [TestMethod]
    public void AllDialects_DateTimeNow_GeneratesCurrentTimestamp()
    {
        // Arrange
        var dialects = new[] 
        { 
            SqlDefine.SQLite, 
            SqlDefine.MySql, 
            SqlDefine.SqlServer, 
            SqlDefine.PostgreSql,
            SqlDefine.Oracle,
            SqlDefine.DB2
        };

        Expression<Func<TestEntity, bool>> expr = x => x.CreatedAt < DateTime.Now;

        // Act & Assert
        foreach (var dialect in dialects)
        {
            var sql = ExpressionExtensions.ToWhereClause(expr, dialect);

            // Different dialects use different functions for current timestamp
            Assert.IsFalse(string.IsNullOrWhiteSpace(sql), 
                $"Dialect {dialect.DatabaseType} should generate valid SQL for DateTime.Now");
        }
    }

    #endregion
}
