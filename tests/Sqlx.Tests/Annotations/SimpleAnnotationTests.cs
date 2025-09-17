// -----------------------------------------------------------------------
// <copyright file="SimpleAnnotationTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;
using System;

namespace Sqlx.Tests.Annotations;

/// <summary>
/// Simple tests for annotation classes.
/// </summary>
[TestClass]
public class SimpleAnnotationTests : TestBase
{
    [TestMethod]
    public void SqlxAttribute_DefaultConstructor_Works()
    {
        // Act
        var attr = new SqlxAttribute();

        // Assert
        Assert.IsNotNull(attr);
        Assert.AreEqual(string.Empty, attr.StoredProcedureName);
        Assert.AreEqual(string.Empty, attr.Sql);
    }

    [TestMethod]
    public void SqlxAttribute_WithStoredProcedure_SetsValue()
    {
        // Arrange
        string spName = "sp_GetUsers";

        // Act
        var attr = new SqlxAttribute(spName);

        // Assert
        Assert.AreEqual(spName, attr.StoredProcedureName);
    }

    [TestMethod]
    public void SqlExecuteTypeAttribute_Constructor_SetsValues()
    {
        // Arrange
        var operation = SqlOperation.Select;
        string tableName = "Users";

        // Act
        var attr = new SqlExecuteTypeAttribute(operation, tableName);

        // Assert
        Assert.AreEqual(operation, attr.ExecuteType);
        Assert.AreEqual(tableName, attr.TableName);
    }

    [TestMethod]
    public void TableNameAttribute_Constructor_SetsTableName()
    {
        // Arrange
        string tableName = "CustomUsers";

        // Act
        var attr = new TableNameAttribute(tableName);

        // Assert
        Assert.AreEqual(tableName, attr.TableName);
    }

    [TestMethod]
    public void RepositoryForAttribute_Constructor_SetsServiceType()
    {
        // Arrange
        var serviceType = typeof(ITestService);

        // Act
        var attr = new RepositoryForAttribute(serviceType);

        // Assert
        Assert.AreEqual(serviceType, attr.ServiceType);
    }

    [TestMethod]
    public void DbSetTypeAttribute_Constructor_SetsEntityType()
    {
        // Arrange
        var entityType = typeof(TestEntity);

        // Act
        var attr = new DbSetTypeAttribute(entityType);

        // Assert
        Assert.AreEqual(entityType, attr.Type);
    }

    [TestMethod]
    public void SqlDefineAttribute_WithDialectType_SetsDialect()
    {
        // Act
        var attr = new SqlDefineAttribute(SqlDefineTypes.MySql);

        // Assert
        Assert.AreEqual(SqlDefineTypes.MySql, attr.DialectType);
        Assert.AreEqual("MySql", attr.DialectName);
    }

    [TestMethod]
    public void SqlDefineAttribute_WithCustomValues_SetsValues()
    {
        // Act
        var attr = new SqlDefineAttribute("\"", "\"", "'", "'", "$");

        // Assert
        Assert.AreEqual("\"", attr.ColumnLeft);
        Assert.AreEqual("\"", attr.ColumnRight);
        Assert.AreEqual("'", attr.StringLeft);
        Assert.AreEqual("'", attr.StringRight);
        Assert.AreEqual("$", attr.ParameterPrefix);
    }

    [TestMethod]
    public void ExpressionToSqlAttribute_Constructor_Works()
    {
        // Act
        var attr = new ExpressionToSqlAttribute();

        // Assert
        Assert.IsNotNull(attr);
    }

    [TestMethod]
    public void SqlOperation_HasAllValues()
    {
        // Act & Assert
        Assert.AreEqual(0, (int)SqlOperation.Select);
        Assert.AreEqual(1, (int)SqlOperation.Insert);
        Assert.AreEqual(2, (int)SqlOperation.Update);
        Assert.AreEqual(3, (int)SqlOperation.Delete);
    }

    [TestMethod]
    public void SqlDefineTypes_HasAllValues()
    {
        // Act
        var values = Enum.GetValues<SqlDefineTypes>();

        // Assert
        Assert.AreEqual(6, values.Length);
        Assert.IsTrue(Array.Exists(values, v => v == SqlDefineTypes.MySql));
        Assert.IsTrue(Array.Exists(values, v => v == SqlDefineTypes.SqlServer));
        Assert.IsTrue(Array.Exists(values, v => v == SqlDefineTypes.PostgreSql));
        Assert.IsTrue(Array.Exists(values, v => v == SqlDefineTypes.Oracle));
        Assert.IsTrue(Array.Exists(values, v => v == SqlDefineTypes.DB2));
        Assert.IsTrue(Array.Exists(values, v => v == SqlDefineTypes.SQLite));
    }

    // Test interfaces and classes
    private interface ITestService { }

    private class TestEntity
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }
}
