// <copyright file="SetExpressionDialectTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using System;
using System.Linq.Expressions;

namespace Sqlx.Tests;

/// <summary>
/// Tests for SetExpressionExtensions across different SQL dialects.
/// </summary>
[TestClass]
public class SetExpressionDialectTests
{
    public class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
    }

    // ========== SQLite 方言测试 ==========

    [TestMethod]
    public void ToSetClause_SQLite_UsesSquareBrackets()
    {
        // Arrange
        Expression<Func<TestEntity, TestEntity>> expr = e => new TestEntity { Name = "test" };

        // Act
        var result = expr.ToSetClause(SqlDefine.SQLite);

        // Assert
        Assert.IsTrue(result.StartsWith("[name]"));
        Assert.IsTrue(result.Contains("@p0"));
    }

    [TestMethod]
    public void ToSetClause_SQLite_StringConcat_UsesPipe()
    {
        // Arrange
        Expression<Func<TestEntity, TestEntity>> expr = e => new TestEntity 
        { 
            Name = e.Name + " suffix" 
        };

        // Act
        var result = expr.ToSetClause(SqlDefine.SQLite);

        // Assert
        Assert.IsTrue(result.Contains("||"));
        Assert.IsFalse(result.Contains("CONCAT"));
    }

    [TestMethod]
    public void ToSetClause_SQLite_MathMax_UsesGreatest()
    {
        // Arrange
        Expression<Func<TestEntity, TestEntity>> expr = e => new TestEntity 
        { 
            Age = Math.Max(e.Age, 18) 
        };

        // Act
        var result = expr.ToSetClause(SqlDefine.SQLite);

        // Assert
        Assert.IsTrue(result.Contains("GREATEST"));
    }

    // ========== PostgreSQL 方言测试 ==========

    [TestMethod]
    public void ToSetClause_PostgreSQL_UsesDoubleQuotes()
    {
        // Arrange
        Expression<Func<TestEntity, TestEntity>> expr = e => new TestEntity { Name = "test" };

        // Act
        var result = expr.ToSetClause(SqlDefine.PostgreSql);

        // Assert
        Assert.IsTrue(result.StartsWith("\"name\""));
        Assert.IsTrue(result.Contains("$p0"));
    }

    [TestMethod]
    public void ToSetClause_PostgreSQL_StringConcat_UsesPipe()
    {
        // Arrange
        Expression<Func<TestEntity, TestEntity>> expr = e => new TestEntity 
        { 
            Name = e.Name + " suffix" 
        };

        // Act
        var result = expr.ToSetClause(SqlDefine.PostgreSql);

        // Assert
        Assert.IsTrue(result.Contains("||"));
        Assert.IsFalse(result.Contains("CONCAT"));
    }

    [TestMethod]
    public void ToSetClause_PostgreSQL_MathMax_UsesGreatest()
    {
        // Arrange
        Expression<Func<TestEntity, TestEntity>> expr = e => new TestEntity 
        { 
            Age = Math.Max(e.Age, 18) 
        };

        // Act
        var result = expr.ToSetClause(SqlDefine.PostgreSql);

        // Assert
        Assert.IsTrue(result.Contains("GREATEST"));
    }

    // ========== MySQL 方言测试 ==========

    [TestMethod]
    public void ToSetClause_MySQL_UsesBackticks()
    {
        // Arrange
        Expression<Func<TestEntity, TestEntity>> expr = e => new TestEntity { Name = "test" };

        // Act
        var result = expr.ToSetClause(SqlDefine.MySql);

        // Assert
        Assert.IsTrue(result.StartsWith("`name`"));
        Assert.IsTrue(result.Contains("@p0"));
    }

    [TestMethod]
    public void ToSetClause_MySQL_StringConcat_UsesConcat()
    {
        // Arrange
        Expression<Func<TestEntity, TestEntity>> expr = e => new TestEntity 
        { 
            Name = e.Name + " suffix" 
        };

        // Act
        var result = expr.ToSetClause(SqlDefine.MySql);

        // Assert
        Assert.IsTrue(result.Contains("CONCAT"));
    }

    [TestMethod]
    public void ToSetClause_MySQL_MathMax_UsesGreatest()
    {
        // Arrange
        Expression<Func<TestEntity, TestEntity>> expr = e => new TestEntity 
        { 
            Age = Math.Max(e.Age, 18) 
        };

        // Act
        var result = expr.ToSetClause(SqlDefine.MySql);

        // Assert
        Assert.IsTrue(result.Contains("GREATEST"));
    }

    // ========== SQL Server 方言测试 ==========

    [TestMethod]
    public void ToSetClause_SqlServer_UsesSquareBrackets()
    {
        // Arrange
        Expression<Func<TestEntity, TestEntity>> expr = e => new TestEntity { Name = "test" };

        // Act
        var result = expr.ToSetClause(SqlDefine.SqlServer);

        // Assert
        Assert.IsTrue(result.StartsWith("[name]"));
        Assert.IsTrue(result.Contains("@p0"));
    }

    [TestMethod]
    public void ToSetClause_SqlServer_StringConcat_UsesPlus()
    {
        // Arrange
        Expression<Func<TestEntity, TestEntity>> expr = e => new TestEntity 
        { 
            Name = e.Name + " suffix" 
        };

        // Act
        var result = expr.ToSetClause(SqlDefine.SqlServer);

        // Assert
        Assert.IsTrue(result.Contains("+"));
        Assert.IsFalse(result.Contains("||"));
        Assert.IsFalse(result.Contains("CONCAT"));
    }

    // ========== Oracle 方言测试 ==========

    [TestMethod]
    public void ToSetClause_Oracle_UsesDoubleQuotes()
    {
        // Arrange
        Expression<Func<TestEntity, TestEntity>> expr = e => new TestEntity { Name = "test" };

        // Act
        var result = expr.ToSetClause(SqlDefine.Oracle);

        // Assert
        Assert.IsTrue(result.StartsWith("\"name\""));
        Assert.IsTrue(result.Contains(":p0"));
    }

    [TestMethod]
    public void ToSetClause_Oracle_StringConcat_UsesPipe()
    {
        // Arrange
        Expression<Func<TestEntity, TestEntity>> expr = e => new TestEntity 
        { 
            Name = e.Name + " suffix" 
        };

        // Act
        var result = expr.ToSetClause(SqlDefine.Oracle);

        // Assert
        Assert.IsTrue(result.Contains("||"));
    }

    // ========== 跨方言一致性测试 ==========

    [TestMethod]
    public void ToSetClause_AllDialects_GenerateValidSql()
    {
        // Arrange
        Expression<Func<TestEntity, TestEntity>> expr = e => new TestEntity 
        { 
            Name = "test",
            Age = e.Age + 1
        };

        var dialects = new[]
        {
            SqlDefine.SQLite,
            SqlDefine.PostgreSql,
            SqlDefine.MySql,
            SqlDefine.SqlServer,
            SqlDefine.Oracle
        };

        // Act & Assert
        foreach (var dialect in dialects)
        {
            var result = expr.ToSetClause(dialect);
            
            // 所有方言都应该生成非空结果
            Assert.IsFalse(string.IsNullOrEmpty(result), $"Dialect {dialect.DatabaseType} generated empty SQL");
            
            // 所有方言都应该包含两个字段
            Assert.IsTrue(result.Contains(","), $"Dialect {dialect.DatabaseType} missing comma separator");
            
            // 所有方言都应该包含参数
            Assert.IsTrue(result.Contains("p0") || result.Contains("p1"), 
                $"Dialect {dialect.DatabaseType} missing parameters");
        }
    }

    [TestMethod]
    public void ToSetClause_AllDialects_StringFunctions_Work()
    {
        // Arrange
        Expression<Func<TestEntity, TestEntity>> expr = e => new TestEntity 
        { 
            Name = e.Name.ToUpper() 
        };

        var dialects = new[]
        {
            SqlDefine.SQLite,
            SqlDefine.PostgreSql,
            SqlDefine.MySql,
            SqlDefine.SqlServer,
            SqlDefine.Oracle
        };

        // Act & Assert
        foreach (var dialect in dialects)
        {
            var result = expr.ToSetClause(dialect);
            Assert.IsTrue(result.Contains("UPPER"), 
                $"Dialect {dialect.DatabaseType} doesn't support UPPER function");
        }
    }

    [TestMethod]
    public void ToSetClause_AllDialects_MathFunctions_Work()
    {
        // Arrange
        Expression<Func<TestEntity, TestEntity>> expr = e => new TestEntity 
        { 
            Age = Math.Abs(e.Age) 
        };

        var dialects = new[]
        {
            SqlDefine.SQLite,
            SqlDefine.PostgreSql,
            SqlDefine.MySql,
            SqlDefine.SqlServer,
            SqlDefine.Oracle
        };

        // Act & Assert
        foreach (var dialect in dialects)
        {
            var result = expr.ToSetClause(dialect);
            Assert.IsTrue(result.Contains("ABS"), 
                $"Dialect {dialect.DatabaseType} doesn't support ABS function");
        }
    }

    // ========== 参数前缀测试 ==========

    [TestMethod]
    public void ToSetClause_ParameterPrefix_MatchesDialect()
    {
        // Arrange
        Expression<Func<TestEntity, TestEntity>> expr = e => new TestEntity { Name = "test" };

        // Act & Assert
        Assert.IsTrue(expr.ToSetClause(SqlDefine.SQLite).Contains("@p0"));
        Assert.IsTrue(expr.ToSetClause(SqlDefine.PostgreSql).Contains("$p0"));
        Assert.IsTrue(expr.ToSetClause(SqlDefine.MySql).Contains("@p0"));
        Assert.IsTrue(expr.ToSetClause(SqlDefine.SqlServer).Contains("@p0"));
        Assert.IsTrue(expr.ToSetClause(SqlDefine.Oracle).Contains(":p0"));
    }

    // ========== 列名包装测试 ==========

    [TestMethod]
    public void ToSetClause_ColumnWrapping_MatchesDialect()
    {
        // Arrange
        Expression<Func<TestEntity, TestEntity>> expr = e => new TestEntity { Name = "test" };

        // Act
        var sqlite = expr.ToSetClause(SqlDefine.SQLite);
        var postgres = expr.ToSetClause(SqlDefine.PostgreSql);
        var mysql = expr.ToSetClause(SqlDefine.MySql);
        var sqlserver = expr.ToSetClause(SqlDefine.SqlServer);
        var oracle = expr.ToSetClause(SqlDefine.Oracle);

        // Assert
        Assert.IsTrue(sqlite.Contains("[name]"));
        Assert.IsTrue(postgres.Contains("\"name\""));
        Assert.IsTrue(mysql.Contains("`name`"));
        Assert.IsTrue(sqlserver.Contains("[name]"));
        Assert.IsTrue(oracle.Contains("\"name\""));
    }
}
