// <copyright file="SqlTemplateAttributeTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Sqlx.Tests;

/// <summary>
/// Unit tests for SqlTemplateAttribute functionality.
/// </summary>
[TestClass]
public class SqlTemplateAttributeTests
{
    [TestMethod]
    public void Constructor_WithValidTemplate_SetsTemplateProperty()
    {
        // Arrange & Act
        var attribute = new SqlTemplateAttribute("SELECT * FROM Users");

        // Assert
        Assert.AreEqual("SELECT * FROM Users", attribute.Template);
    }

    [TestMethod]
    public void Constructor_WithValidTemplate_SetsDefaultValues()
    {
        // Arrange & Act
        var attribute = new SqlTemplateAttribute("SELECT * FROM Users");

        // Assert
        Assert.AreEqual(SqlDefineTypes.SqlServer, attribute.Dialect);
        Assert.IsTrue(attribute.SafeMode);
        Assert.IsTrue(attribute.ValidateParameters);
        Assert.IsTrue(attribute.EnableCaching);
        Assert.IsNull(attribute.Name);
    }

    [TestMethod]
    public void Constructor_WithNullTemplate_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        var exception = Assert.ThrowsException<ArgumentNullException>(() =>
            new SqlTemplateAttribute(null!));

        Assert.AreEqual("template", exception.ParamName);
    }

    [TestMethod]
    public void Template_IsReadOnly_CannotBeChanged()
    {
        // Arrange
        var attribute = new SqlTemplateAttribute("SELECT * FROM Users");

        // Act
        var property = typeof(SqlTemplateAttribute).GetProperty(nameof(SqlTemplateAttribute.Template));

        // Assert
        Assert.IsNotNull(property);
        Assert.IsTrue(property.CanRead);
        Assert.IsFalse(property.CanWrite);
        Assert.IsNotNull(property.GetMethod);
        Assert.IsNull(property.SetMethod);
    }

    [TestMethod]
    public void Dialect_DefaultValue_IsSqlServer()
    {
        // Arrange & Act
        var attribute = new SqlTemplateAttribute("SELECT 1");

        // Assert
        Assert.AreEqual(SqlDefineTypes.SqlServer, attribute.Dialect);
    }

    [TestMethod]
    public void Dialect_CanBeSet_StoresValue()
    {
        // Arrange
        var attribute = new SqlTemplateAttribute("SELECT 1");

        // Act
        attribute.Dialect = SqlDefineTypes.MySql;

        // Assert
        Assert.AreEqual(SqlDefineTypes.MySql, attribute.Dialect);
    }

    [TestMethod]
    public void Dialect_CanBeSetToAllValues()
    {
        // Arrange
        var attribute = new SqlTemplateAttribute("SELECT 1");

        // Act & Assert
        foreach (SqlDefineTypes dialectType in Enum.GetValues(typeof(SqlDefineTypes)))
        {
            attribute.Dialect = dialectType;
            Assert.AreEqual(dialectType, attribute.Dialect);
        }
    }

    [TestMethod]
    public void SafeMode_DefaultValue_IsTrue()
    {
        // Arrange & Act
        var attribute = new SqlTemplateAttribute("SELECT 1");

        // Assert
        Assert.IsTrue(attribute.SafeMode);
    }

    [TestMethod]
    public void SafeMode_CanBeSetToFalse_StoresFalse()
    {
        // Arrange
        var attribute = new SqlTemplateAttribute("SELECT 1");

        // Act
        attribute.SafeMode = false;

        // Assert
        Assert.IsFalse(attribute.SafeMode);
    }

    [TestMethod]
    public void ValidateParameters_DefaultValue_IsTrue()
    {
        // Arrange & Act
        var attribute = new SqlTemplateAttribute("SELECT 1");

        // Assert
        Assert.IsTrue(attribute.ValidateParameters);
    }

    [TestMethod]
    public void ValidateParameters_CanBeSetToFalse_StoresFalse()
    {
        // Arrange
        var attribute = new SqlTemplateAttribute("SELECT 1");

        // Act
        attribute.ValidateParameters = false;

        // Assert
        Assert.IsFalse(attribute.ValidateParameters);
    }

    [TestMethod]
    public void EnableCaching_DefaultValue_IsTrue()
    {
        // Arrange & Act
        var attribute = new SqlTemplateAttribute("SELECT 1");

        // Assert
        Assert.IsTrue(attribute.EnableCaching);
    }

    [TestMethod]
    public void EnableCaching_CanBeSetToFalse_StoresFalse()
    {
        // Arrange
        var attribute = new SqlTemplateAttribute("SELECT 1");

        // Act
        attribute.EnableCaching = false;

        // Assert
        Assert.IsFalse(attribute.EnableCaching);
    }

    [TestMethod]
    public void Name_DefaultValue_IsNull()
    {
        // Arrange & Act
        var attribute = new SqlTemplateAttribute("SELECT 1");

        // Assert
        Assert.IsNull(attribute.Name);
    }

    [TestMethod]
    public void Name_CanBeSet_StoresValue()
    {
        // Arrange
        var attribute = new SqlTemplateAttribute("SELECT 1");

        // Act
        attribute.Name = "GetUsers";

        // Assert
        Assert.AreEqual("GetUsers", attribute.Name);
    }

    [TestMethod]
    public void Name_CanBeSetToNull_StoresNull()
    {
        // Arrange
        var attribute = new SqlTemplateAttribute("SELECT 1");
        attribute.Name = "Test";

        // Act
        attribute.Name = null;

        // Assert
        Assert.IsNull(attribute.Name);
    }

    [TestMethod]
    public void AttributeUsage_AllowsMethodTarget()
    {
        // Arrange
        var attributeType = typeof(SqlTemplateAttribute);

        // Act
        var usageAttribute = attributeType.GetCustomAttribute<AttributeUsageAttribute>();

        // Assert
        Assert.IsNotNull(usageAttribute);
        Assert.AreEqual(AttributeTargets.Method, usageAttribute.ValidOn);
    }

    [TestMethod]
    public void AttributeUsage_DoesNotAllowMultiple()
    {
        // Arrange
        var attributeType = typeof(SqlTemplateAttribute);

        // Act
        var usageAttribute = attributeType.GetCustomAttribute<AttributeUsageAttribute>();

        // Assert
        Assert.IsNotNull(usageAttribute);
        Assert.IsFalse(usageAttribute.AllowMultiple);
    }

    [TestMethod]
    public void AttributeUsage_IsNotInherited()
    {
        // Arrange
        var attributeType = typeof(SqlTemplateAttribute);

        // Act
        var usageAttribute = attributeType.GetCustomAttribute<AttributeUsageAttribute>();

        // Assert
        Assert.IsNotNull(usageAttribute);
        Assert.IsFalse(usageAttribute.Inherited);
    }

    [TestMethod]
    public void Attribute_IsSealed()
    {
        // Arrange
        var attributeType = typeof(SqlTemplateAttribute);

        // Act & Assert
        Assert.IsTrue(attributeType.IsSealed);
    }

    [TestMethod]
    public void Attribute_InheritsFromAttribute()
    {
        // Arrange
        var attributeType = typeof(SqlTemplateAttribute);

        // Act & Assert
        Assert.IsTrue(typeof(Attribute).IsAssignableFrom(attributeType));
    }

    [TestMethod]
    public void MultipleInstances_AreIndependent()
    {
        // Arrange & Act
        var attribute1 = new SqlTemplateAttribute("SELECT * FROM Users")
        {
            Dialect = SqlDefineTypes.MySql,
            SafeMode = false,
            ValidateParameters = false,
            EnableCaching = false,
            Name = "Query1"
        };
        var attribute2 = new SqlTemplateAttribute("SELECT * FROM Orders")
        {
            Dialect = SqlDefineTypes.PostgreSql,
            SafeMode = true,
            ValidateParameters = true,
            EnableCaching = true,
            Name = "Query2"
        };

        // Assert
        Assert.AreEqual("SELECT * FROM Users", attribute1.Template);
        Assert.AreEqual(SqlDefineTypes.MySql, attribute1.Dialect);
        Assert.IsFalse(attribute1.SafeMode);
        Assert.IsFalse(attribute1.ValidateParameters);
        Assert.IsFalse(attribute1.EnableCaching);
        Assert.AreEqual("Query1", attribute1.Name);

        Assert.AreEqual("SELECT * FROM Orders", attribute2.Template);
        Assert.AreEqual(SqlDefineTypes.PostgreSql, attribute2.Dialect);
        Assert.IsTrue(attribute2.SafeMode);
        Assert.IsTrue(attribute2.ValidateParameters);
        Assert.IsTrue(attribute2.EnableCaching);
        Assert.AreEqual("Query2", attribute2.Name);

        Assert.AreNotSame(attribute1, attribute2);
    }

    // Test interface with methods
    private interface ITestRepository
    {
        [SqlTemplate("SELECT * FROM Users WHERE Id = @{userId}")]
        Task<object> GetUserAsync(int userId);

        [SqlTemplate("INSERT INTO Users (Name) VALUES (@{name})", 
            Dialect = SqlDefineTypes.PostgreSql, 
            SafeMode = false,
            ValidateParameters = false,
            EnableCaching = false,
            Name = "InsertUser")]
        Task InsertUserAsync(string name);
    }

    [TestMethod]
    public void Attribute_CanBeAppliedToMethod()
    {
        // Arrange
        var methodInfo = typeof(ITestRepository).GetMethod(nameof(ITestRepository.GetUserAsync));
        Assert.IsNotNull(methodInfo);

        // Act
        var attribute = methodInfo.GetCustomAttribute<SqlTemplateAttribute>();

        // Assert
        Assert.IsNotNull(attribute);
        Assert.AreEqual("SELECT * FROM Users WHERE Id = @{userId}", attribute.Template);
        Assert.AreEqual(SqlDefineTypes.SqlServer, attribute.Dialect);
        Assert.IsTrue(attribute.SafeMode);
        Assert.IsTrue(attribute.ValidateParameters);
        Assert.IsTrue(attribute.EnableCaching);
        Assert.IsNull(attribute.Name);
    }

    [TestMethod]
    public void Attribute_WithAllProperties_CanBeAppliedToMethod()
    {
        // Arrange
        var methodInfo = typeof(ITestRepository).GetMethod(nameof(ITestRepository.InsertUserAsync));
        Assert.IsNotNull(methodInfo);

        // Act
        var attribute = methodInfo.GetCustomAttribute<SqlTemplateAttribute>();

        // Assert
        Assert.IsNotNull(attribute);
        Assert.AreEqual("INSERT INTO Users (Name) VALUES (@{name})", attribute.Template);
        Assert.AreEqual(SqlDefineTypes.PostgreSql, attribute.Dialect);
        Assert.IsFalse(attribute.SafeMode);
        Assert.IsFalse(attribute.ValidateParameters);
        Assert.IsFalse(attribute.EnableCaching);
        Assert.AreEqual("InsertUser", attribute.Name);
    }

    [TestMethod]
    public void Constructor_WithEmptyString_CreatesAttribute()
    {
        // Arrange & Act
        var attribute = new SqlTemplateAttribute(string.Empty);

        // Assert
        Assert.AreEqual(string.Empty, attribute.Template);
    }

    [TestMethod]
    public void Constructor_WithWhitespaceString_CreatesAttribute()
    {
        // Arrange & Act
        var attribute = new SqlTemplateAttribute("   ");

        // Assert
        Assert.AreEqual("   ", attribute.Template);
    }

    [TestMethod]
    public void Constructor_WithComplexTemplate_StoresTemplate()
    {
        // Arrange
        var complexTemplate = @"
            SELECT u.*, o.*
            FROM Users u
            LEFT JOIN Orders o ON u.Id = o.UserId
            WHERE u.Id = @{userId}
            AND o.Status = @{status}
            ORDER BY o.CreatedAt DESC";

        // Act
        var attribute = new SqlTemplateAttribute(complexTemplate);

        // Assert
        Assert.AreEqual(complexTemplate, attribute.Template);
    }

    [TestMethod]
    public void Properties_CanBeSetInObjectInitializer()
    {
        // Arrange & Act
        var attribute = new SqlTemplateAttribute("SELECT 1")
        {
            Dialect = SqlDefineTypes.SQLite,
            SafeMode = false,
            ValidateParameters = false,
            EnableCaching = false,
            Name = "TestQuery"
        };

        // Assert
        Assert.AreEqual(SqlDefineTypes.SQLite, attribute.Dialect);
        Assert.IsFalse(attribute.SafeMode);
        Assert.IsFalse(attribute.ValidateParameters);
        Assert.IsFalse(attribute.EnableCaching);
        Assert.AreEqual("TestQuery", attribute.Name);
    }

    [TestMethod]
    public void AllProperties_CanBeSetMultipleTimes()
    {
        // Arrange
        var attribute = new SqlTemplateAttribute("SELECT 1");

        // Act & Assert
        attribute.Dialect = SqlDefineTypes.MySql;
        Assert.AreEqual(SqlDefineTypes.MySql, attribute.Dialect);

        attribute.Dialect = SqlDefineTypes.Oracle;
        Assert.AreEqual(SqlDefineTypes.Oracle, attribute.Dialect);

        attribute.SafeMode = false;
        Assert.IsFalse(attribute.SafeMode);

        attribute.SafeMode = true;
        Assert.IsTrue(attribute.SafeMode);

        attribute.ValidateParameters = false;
        Assert.IsFalse(attribute.ValidateParameters);

        attribute.ValidateParameters = true;
        Assert.IsTrue(attribute.ValidateParameters);

        attribute.EnableCaching = false;
        Assert.IsFalse(attribute.EnableCaching);

        attribute.EnableCaching = true;
        Assert.IsTrue(attribute.EnableCaching);

        attribute.Name = "Name1";
        Assert.AreEqual("Name1", attribute.Name);

        attribute.Name = "Name2";
        Assert.AreEqual("Name2", attribute.Name);
    }

    [TestMethod]
    public void Constructor_WithUnicodeTemplate_StoresTemplate()
    {
        // Arrange & Act
        var attribute = new SqlTemplateAttribute("SELECT * FROM 用户表 WHERE 姓名 = @{name}");

        // Assert
        Assert.AreEqual("SELECT * FROM 用户表 WHERE 姓名 = @{name}", attribute.Template);
    }

    [TestMethod]
    public void Constructor_WithVeryLongTemplate_StoresTemplate()
    {
        // Arrange
        var longTemplate = new string('A', 10000);

        // Act
        var attribute = new SqlTemplateAttribute(longTemplate);

        // Assert
        Assert.AreEqual(longTemplate, attribute.Template);
    }
}
