// -----------------------------------------------------------------------
// <copyright file="SqlTemplateSimpleTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;
using System.Data.Common;

namespace Sqlx.Tests.Core;

/// <summary>
/// Simple tests for SqlTemplate record struct.
/// </summary>
[TestClass]
public class SqlTemplateSimpleTests : TestBase
{
    [TestMethod]
    public void SqlTemplate_Constructor_SetsProperties()
    {
        // Arrange
        string sql = "SELECT * FROM Users";
        var parameters = new DbParameter[0];

        // Act
        var template = new SqlTemplate(sql, parameters);

        // Assert
        Assert.AreEqual(sql, template.Sql);
        Assert.AreEqual(parameters, template.Parameters);
    }

    [TestMethod]
    public void SqlTemplate_WithOperator_CreatesNewInstance()
    {
        // Arrange
        var original = new SqlTemplate("SELECT 1", new DbParameter[0]);

        // Act
        var modified = original with { Sql = "SELECT 2" };

        // Assert
        Assert.AreNotEqual(original, modified);
        Assert.AreEqual("SELECT 1", original.Sql);
        Assert.AreEqual("SELECT 2", modified.Sql);
    }

    [TestMethod]
    public void SqlTemplate_Equality_WorksCorrectly()
    {
        // Arrange
        var template1 = new SqlTemplate("SELECT 1", new DbParameter[0]);
        var template2 = new SqlTemplate("SELECT 1", new DbParameter[0]);
        var template3 = new SqlTemplate("SELECT 2", new DbParameter[0]);

        // Act & Assert
        Assert.AreEqual(template1, template2);
        Assert.AreNotEqual(template1, template3);
    }

    [TestMethod]
    public void SqlTemplate_ToString_ReturnsString()
    {
        // Arrange
        var template = new SqlTemplate("SELECT 1", new DbParameter[0]);

        // Act
        string result = template.ToString();

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Length > 0);
    }

    [TestMethod]
    public void SqlTemplate_GetHashCode_WorksCorrectly()
    {
        // Arrange
        var template1 = new SqlTemplate("SELECT 1", new DbParameter[0]);
        var template2 = new SqlTemplate("SELECT 1", new DbParameter[0]);

        // Act
        int hash1 = template1.GetHashCode();
        int hash2 = template2.GetHashCode();

        // Assert
        Assert.AreEqual(hash1, hash2);
    }
}

