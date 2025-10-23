// -----------------------------------------------------------------------
// <copyright file="ExpressionToSqlBaseBatchTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using System;
using System.Collections.Generic;

namespace Sqlx.Tests.BatchOperations;

[TestClass]
public class ExpressionToSqlBaseBatchTests
{
    private class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public bool IsActive { get; set; }
    }

    private class TestExpressionToSql : ExpressionToSqlBase
    {
        public TestExpressionToSql(SqlDialect dialect) : base(dialect, typeof(TestEntity)) { }

        public override string ToSql() => string.Empty;
        public override SqlTemplate ToTemplate() => new SqlTemplate();
        
        // Public accessors for testing internal fields
        public new List<string> _whereConditions => base._whereConditions;
        public new Dictionary<string, object?> _parameters => base._parameters;
        public new ExpressionToSqlBase? _whereExpression => base._whereExpression;
        public new List<Dictionary<string, object?>>? _batchParameters 
        {
            get => base._batchParameters;
            set => base._batchParameters = value;
        }
        
        public new string GetMergedWhereConditions() => base.GetMergedWhereConditions();
        public new Dictionary<string, object?> GetMergedParameters() => base.GetMergedParameters();
    }

    [TestMethod]
    public void WhereFrom_WithNullExpression_ThrowsArgumentNullException()
    {
        // Arrange
        var expr = new TestExpressionToSql(SqlDefine.SqlServer);

        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => expr.WhereFrom(null!));
    }

    [TestMethod]
    public void WhereFrom_WithValidExpression_SetsWhereExpression()
    {
        // Arrange
        var expr1 = new TestExpressionToSql(SqlDefine.SqlServer);
        var expr2 = new TestExpressionToSql(SqlDefine.SqlServer);

        // Act
        var result = expr1.WhereFrom(expr2);

        // Assert
        Assert.AreSame(expr1, result); // Fluent API
        Assert.AreSame(expr2, expr1._whereExpression);
    }

    [TestMethod]
    public void GetMergedWhereConditions_WithNoConditions_ReturnsEmptyString()
    {
        // Arrange
        var expr = new TestExpressionToSql(SqlDefine.SqlServer);

        // Act
        var result = expr.GetMergedWhereConditions();

        // Assert
        Assert.AreEqual("", result);
    }

    [TestMethod]
    public void GetMergedWhereConditions_WithOwnConditions_ReturnsConditions()
    {
        // Arrange
        var expr = new TestExpressionToSql(SqlDefine.SqlServer);
        expr._whereConditions.Add("Age > 18");
        expr._whereConditions.Add("IsActive = 1");

        // Act
        var result = expr.GetMergedWhereConditions();

        // Assert
        Assert.AreEqual("Age > 18 AND IsActive = 1", result);
    }

    [TestMethod]
    public void GetMergedWhereConditions_WithExternalConditions_MergesConditions()
    {
        // Arrange
        var expr1 = new TestExpressionToSql(SqlDefine.SqlServer);
        var expr2 = new TestExpressionToSql(SqlDefine.SqlServer);

        expr1._whereConditions.Add("Age > 18");
        expr2._whereConditions.Add("IsActive = 1");
        expr2._whereConditions.Add("Name IS NOT NULL");

        expr1.WhereFrom(expr2);

        // Act
        var result = expr1.GetMergedWhereConditions();

        // Assert
        Assert.AreEqual("Age > 18 AND IsActive = 1 AND Name IS NOT NULL", result);
    }

    [TestMethod]
    public void GetMergedParameters_WithNoParameters_ReturnsEmptyDictionary()
    {
        // Arrange
        var expr = new TestExpressionToSql(SqlDefine.SqlServer);

        // Act
        var result = expr.GetMergedParameters();

        // Assert
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public void GetMergedParameters_WithOwnParameters_ReturnsParameters()
    {
        // Arrange
        var expr = new TestExpressionToSql(SqlDefine.SqlServer);
        expr._parameters["@age"] = 18;
        expr._parameters["@name"] = "John";

        // Act
        var result = expr.GetMergedParameters();

        // Assert
        Assert.AreEqual(2, result.Count);
        Assert.AreEqual(18, result["@age"]);
        Assert.AreEqual("John", result["@name"]);
    }

    [TestMethod]
    public void GetMergedParameters_WithExternalParameters_MergesParameters()
    {
        // Arrange
        var expr1 = new TestExpressionToSql(SqlDefine.SqlServer);
        var expr2 = new TestExpressionToSql(SqlDefine.SqlServer);

        expr1._parameters["@age"] = 18;
        expr2._parameters["@status"] = "Active";
        expr2._parameters["@limit"] = 10;

        expr1.WhereFrom(expr2);

        // Act
        var result = expr1.GetMergedParameters();

        // Assert
        Assert.AreEqual(3, result.Count);
        Assert.AreEqual(18, result["@age"]);
        Assert.AreEqual("Active", result["@status"]);
        Assert.AreEqual(10, result["@limit"]);
    }

    [TestMethod]
    public void GetMergedParameters_WithDuplicateKeys_PrefixesExternalKeys()
    {
        // Arrange
        var expr1 = new TestExpressionToSql(SqlDefine.SqlServer);
        var expr2 = new TestExpressionToSql(SqlDefine.SqlServer);

        expr1._parameters["@age"] = 18;
        expr2._parameters["@age"] = 25; // Duplicate key

        expr1.WhereFrom(expr2);

        // Act
        var result = expr1.GetMergedParameters();

        // Assert
        Assert.AreEqual(2, result.Count);
        Assert.AreEqual(18, result["@age"]); // Original value
        Assert.AreEqual(25, result["__ext_@age"]); // Prefixed external value
    }

    [TestMethod]
    public void GetMergedWhereConditions_WithMultipleLevels_MergesAllConditions()
    {
        // Arrange
        var expr1 = new TestExpressionToSql(SqlDefine.SqlServer);
        var expr2 = new TestExpressionToSql(SqlDefine.SqlServer);
        var expr3 = new TestExpressionToSql(SqlDefine.SqlServer);

        expr1._whereConditions.Add("Level1");
        expr2._whereConditions.Add("Level2");
        expr3._whereConditions.Add("Level3");

        expr2.WhereFrom(expr3);
        expr1.WhereFrom(expr2);

        // Act
        var result = expr1.GetMergedWhereConditions();

        // Assert
        // Only merges one level (by design)
        Assert.AreEqual("Level1 AND Level2", result);
    }

    [TestMethod]
    public void BatchParameters_InitiallyNull()
    {
        // Arrange
        var expr = new TestExpressionToSql(SqlDefine.SqlServer);

        // Assert
        Assert.IsNull(expr._batchParameters);
    }

    [TestMethod]
    public void BatchParameters_CanBeInitialized()
    {
        // Arrange
        var expr = new TestExpressionToSql(SqlDefine.SqlServer);
        expr._batchParameters = new List<Dictionary<string, object?>>
        {
            new Dictionary<string, object?> { ["@name"] = "John", ["@age"] = 30 },
            new Dictionary<string, object?> { ["@name"] = "Jane", ["@age"] = 25 }
        };

        // Assert
        Assert.IsNotNull(expr._batchParameters);
        Assert.AreEqual(2, expr._batchParameters.Count);
        Assert.AreEqual("John", expr._batchParameters[0]["@name"]);
        Assert.AreEqual(25, expr._batchParameters[1]["@age"]);
    }
}

