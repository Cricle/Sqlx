// <copyright file="ReturnInsertedEntityAttributeTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Sqlx.Tests;

/// <summary>
/// Unit tests for ReturnInsertedEntityAttribute functionality.
/// </summary>
[TestClass]
public class ReturnInsertedEntityAttributeTests
{
    [TestMethod]
    public void Constructor_CreatesInstance_WithDefaultValues()
    {
        // Arrange & Act
        var attribute = new ReturnInsertedEntityAttribute();

        // Assert
        Assert.IsNotNull(attribute);
        Assert.AreEqual("Id", attribute.IdColumnName);
        Assert.IsFalse(attribute.UseValueTask);
        Assert.IsFalse(attribute.CreateNewInstance);
    }

    [TestMethod]
    public void IdColumnName_DefaultValue_IsId()
    {
        // Arrange & Act
        var attribute = new ReturnInsertedEntityAttribute();

        // Assert
        Assert.AreEqual("Id", attribute.IdColumnName);
    }

    [TestMethod]
    public void IdColumnName_CanBeSet_StoresValue()
    {
        // Arrange
        var attribute = new ReturnInsertedEntityAttribute();

        // Act
        attribute.IdColumnName = "EntityId";

        // Assert
        Assert.AreEqual("EntityId", attribute.IdColumnName);
    }

    [TestMethod]
    public void IdColumnName_CanBeSetToNull_StoresNull()
    {
        // Arrange
        var attribute = new ReturnInsertedEntityAttribute();

        // Act
        attribute.IdColumnName = null!;

        // Assert
        Assert.IsNull(attribute.IdColumnName);
    }

    [TestMethod]
    public void UseValueTask_DefaultValue_IsFalse()
    {
        // Arrange & Act
        var attribute = new ReturnInsertedEntityAttribute();

        // Assert
        Assert.IsFalse(attribute.UseValueTask);
    }

    [TestMethod]
    public void UseValueTask_CanBeSetToTrue_StoresTrue()
    {
        // Arrange
        var attribute = new ReturnInsertedEntityAttribute();

        // Act
        attribute.UseValueTask = true;

        // Assert
        Assert.IsTrue(attribute.UseValueTask);
    }

    [TestMethod]
    public void CreateNewInstance_DefaultValue_IsFalse()
    {
        // Arrange & Act
        var attribute = new ReturnInsertedEntityAttribute();

        // Assert
        Assert.IsFalse(attribute.CreateNewInstance);
    }

    [TestMethod]
    public void CreateNewInstance_CanBeSetToTrue_StoresTrue()
    {
        // Arrange
        var attribute = new ReturnInsertedEntityAttribute();

        // Act
        attribute.CreateNewInstance = true;

        // Assert
        Assert.IsTrue(attribute.CreateNewInstance);
    }

    [TestMethod]
    public void CreateNewInstance_CanBeSetToFalse_StoresFalse()
    {
        // Arrange
        var attribute = new ReturnInsertedEntityAttribute();
        attribute.CreateNewInstance = true;

        // Act
        attribute.CreateNewInstance = false;

        // Assert
        Assert.IsFalse(attribute.CreateNewInstance);
    }

    [TestMethod]
    public void AllProperties_CanBeSetIndependently()
    {
        // Arrange
        var attribute = new ReturnInsertedEntityAttribute();

        // Act
        attribute.IdColumnName = "CustomId";
        attribute.UseValueTask = true;
        attribute.CreateNewInstance = true;

        // Assert
        Assert.AreEqual("CustomId", attribute.IdColumnName);
        Assert.IsTrue(attribute.UseValueTask);
        Assert.IsTrue(attribute.CreateNewInstance);
    }

    [TestMethod]
    public void AttributeUsage_AllowsMethodTarget()
    {
        // Arrange
        var attributeType = typeof(ReturnInsertedEntityAttribute);

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
        var attributeType = typeof(ReturnInsertedEntityAttribute);

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
        var attributeType = typeof(ReturnInsertedEntityAttribute);

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
        var attributeType = typeof(ReturnInsertedEntityAttribute);

        // Act & Assert
        Assert.IsTrue(attributeType.IsSealed);
    }

    [TestMethod]
    public void Attribute_InheritsFromAttribute()
    {
        // Arrange
        var attributeType = typeof(ReturnInsertedEntityAttribute);

        // Act & Assert
        Assert.IsTrue(typeof(Attribute).IsAssignableFrom(attributeType));
    }

    [TestMethod]
    public void MultipleInstances_AreIndependent()
    {
        // Arrange & Act
        var attribute1 = new ReturnInsertedEntityAttribute
        {
            IdColumnName = "Id1",
            UseValueTask = true,
            CreateNewInstance = true
        };
        var attribute2 = new ReturnInsertedEntityAttribute
        {
            IdColumnName = "Id2",
            UseValueTask = false,
            CreateNewInstance = false
        };

        // Assert
        Assert.AreEqual("Id1", attribute1.IdColumnName);
        Assert.IsTrue(attribute1.UseValueTask);
        Assert.IsTrue(attribute1.CreateNewInstance);

        Assert.AreEqual("Id2", attribute2.IdColumnName);
        Assert.IsFalse(attribute2.UseValueTask);
        Assert.IsFalse(attribute2.CreateNewInstance);

        Assert.AreNotSame(attribute1, attribute2);
    }

    // Test entity class
    private class TestEntity
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    // Test interface with methods
    private interface ITestRepository
    {
        [ReturnInsertedEntity]
        Task<TestEntity> InsertAsync(TestEntity entity);

        [ReturnInsertedEntity(IdColumnName = "EntityId", UseValueTask = true, CreateNewInstance = true)]
        ValueTask<TestEntity> InsertWithOptionsAsync(TestEntity entity);
    }

    [TestMethod]
    public void Attribute_CanBeAppliedToMethod()
    {
        // Arrange
        var methodInfo = typeof(ITestRepository).GetMethod(nameof(ITestRepository.InsertAsync));
        Assert.IsNotNull(methodInfo);

        // Act
        var attribute = methodInfo.GetCustomAttribute<ReturnInsertedEntityAttribute>();

        // Assert
        Assert.IsNotNull(attribute);
        Assert.AreEqual("Id", attribute.IdColumnName);
        Assert.IsFalse(attribute.UseValueTask);
        Assert.IsFalse(attribute.CreateNewInstance);
    }

    [TestMethod]
    public void Attribute_WithAllProperties_CanBeAppliedToMethod()
    {
        // Arrange
        var methodInfo = typeof(ITestRepository).GetMethod(nameof(ITestRepository.InsertWithOptionsAsync));
        Assert.IsNotNull(methodInfo);

        // Act
        var attribute = methodInfo.GetCustomAttribute<ReturnInsertedEntityAttribute>();

        // Assert
        Assert.IsNotNull(attribute);
        Assert.AreEqual("EntityId", attribute.IdColumnName);
        Assert.IsTrue(attribute.UseValueTask);
        Assert.IsTrue(attribute.CreateNewInstance);
    }

    [TestMethod]
    public void Properties_CanBeSetInObjectInitializer()
    {
        // Arrange & Act
        var attribute = new ReturnInsertedEntityAttribute
        {
            IdColumnName = "PrimaryKey",
            UseValueTask = true,
            CreateNewInstance = true
        };

        // Assert
        Assert.AreEqual("PrimaryKey", attribute.IdColumnName);
        Assert.IsTrue(attribute.UseValueTask);
        Assert.IsTrue(attribute.CreateNewInstance);
    }

    [TestMethod]
    public void IdColumnName_Property_IsReadWrite()
    {
        // Arrange
        var property = typeof(ReturnInsertedEntityAttribute).GetProperty(nameof(ReturnInsertedEntityAttribute.IdColumnName));

        // Assert
        Assert.IsNotNull(property);
        Assert.IsTrue(property.CanRead);
        Assert.IsTrue(property.CanWrite);
        Assert.IsNotNull(property.GetMethod);
        Assert.IsNotNull(property.SetMethod);
    }

    [TestMethod]
    public void UseValueTask_Property_IsReadWrite()
    {
        // Arrange
        var property = typeof(ReturnInsertedEntityAttribute).GetProperty(nameof(ReturnInsertedEntityAttribute.UseValueTask));

        // Assert
        Assert.IsNotNull(property);
        Assert.IsTrue(property.CanRead);
        Assert.IsTrue(property.CanWrite);
        Assert.IsNotNull(property.GetMethod);
        Assert.IsNotNull(property.SetMethod);
    }

    [TestMethod]
    public void CreateNewInstance_Property_IsReadWrite()
    {
        // Arrange
        var property = typeof(ReturnInsertedEntityAttribute).GetProperty(nameof(ReturnInsertedEntityAttribute.CreateNewInstance));

        // Assert
        Assert.IsNotNull(property);
        Assert.IsTrue(property.CanRead);
        Assert.IsTrue(property.CanWrite);
        Assert.IsNotNull(property.GetMethod);
        Assert.IsNotNull(property.SetMethod);
    }

    [TestMethod]
    public void IdColumnName_WithSpecialCharacters_StoresValue()
    {
        // Arrange
        var attribute = new ReturnInsertedEntityAttribute();

        // Act
        attribute.IdColumnName = "Entity_Id_123";

        // Assert
        Assert.AreEqual("Entity_Id_123", attribute.IdColumnName);
    }

    [TestMethod]
    public void IdColumnName_WithEmptyString_StoresEmptyString()
    {
        // Arrange
        var attribute = new ReturnInsertedEntityAttribute();

        // Act
        attribute.IdColumnName = string.Empty;

        // Assert
        Assert.AreEqual(string.Empty, attribute.IdColumnName);
    }

    [TestMethod]
    public void Properties_CanBeSetMultipleTimes_StoreLatestValues()
    {
        // Arrange
        var attribute = new ReturnInsertedEntityAttribute();

        // Act & Assert
        attribute.IdColumnName = "First";
        Assert.AreEqual("First", attribute.IdColumnName);

        attribute.IdColumnName = "Second";
        Assert.AreEqual("Second", attribute.IdColumnName);

        attribute.UseValueTask = true;
        Assert.IsTrue(attribute.UseValueTask);

        attribute.UseValueTask = false;
        Assert.IsFalse(attribute.UseValueTask);

        attribute.CreateNewInstance = true;
        Assert.IsTrue(attribute.CreateNewInstance);

        attribute.CreateNewInstance = false;
        Assert.IsFalse(attribute.CreateNewInstance);
    }
}
