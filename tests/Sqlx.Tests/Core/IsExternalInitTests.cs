// -----------------------------------------------------------------------
// <copyright file="IsExternalInitTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Reflection;

namespace Sqlx.Tests.Core;

/// <summary>
/// Tests for IsExternalInit class that enables init-only properties.
/// </summary>
[TestClass]
public class IsExternalInitTests : TestBase
{
    [TestMethod]
    public void IsExternalInitClass_Exists_InSqlxAssembly()
    {
        // Arrange
        var sqlxAssembly = typeof(Sqlx.ExpressionToSql<>).Assembly;
        
        // Act
        var isExternalInitType = sqlxAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "IsExternalInit");
        
        // Assert
        Assert.IsNotNull(isExternalInitType, "IsExternalInit class should exist in Sqlx assembly");
        Assert.IsTrue(isExternalInitType.IsClass, "IsExternalInit should be a class");
    }

    [TestMethod]
    public void IsExternalInitClass_IsInternal()
    {
        // Arrange
        var sqlxAssembly = typeof(Sqlx.ExpressionToSql<>).Assembly;
        
        // Act
        var isExternalInitType = sqlxAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "IsExternalInit");
        
        // Assert
        Assert.IsNotNull(isExternalInitType);
        Assert.IsFalse(isExternalInitType.IsPublic, "IsExternalInit should be internal");
    }

    [TestMethod]
    public void IsExternalInitClass_InCorrectNamespace()
    {
        // Arrange
        var sqlxAssembly = typeof(Sqlx.ExpressionToSql<>).Assembly;
        
        // Act
        var isExternalInitType = sqlxAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "IsExternalInit");
        
        // Assert
        Assert.IsNotNull(isExternalInitType);
        Assert.AreEqual("System.Runtime.CompilerServices", isExternalInitType.Namespace);
    }

    [TestMethod]
    public void IsExternalInit_EnablesInitOnlyProperties()
    {
        // This test verifies that init-only properties work with record types
        // We'll test this using the SqlTemplate record which uses init-only properties
        
        // Arrange & Act
        var template = new Sqlx.Annotations.SqlTemplate("SELECT 1", new System.Data.Common.DbParameter[0]);
        
        // This should compile because SqlTemplate uses init-only properties
        var newTemplate = template with { Sql = "SELECT 2" };
        
        // Assert
        Assert.AreEqual("SELECT 1", template.Sql);
        Assert.AreEqual("SELECT 2", newTemplate.Sql);
    }

    [TestMethod]
    public void RecordTypes_WorkWithIsExternalInit()
    {
        // Test that record types in the project work correctly
        // This is an indirect test of IsExternalInit functionality
        
        // Arrange
        var original = new Sqlx.Annotations.SqlTemplate("SELECT * FROM Users", null!);
        
        // Act
        var modified = original with { Sql = "SELECT * FROM Products" };
        
        // Assert
        Assert.AreNotEqual(original, modified);
        Assert.AreEqual("SELECT * FROM Users", original.Sql);
        Assert.AreEqual("SELECT * FROM Products", modified.Sql);
    }
}

