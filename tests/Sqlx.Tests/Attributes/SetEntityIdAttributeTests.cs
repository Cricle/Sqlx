// <copyright file="SetEntityIdAttributeTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Sqlx.Tests.Attributes;

/// <summary>
/// Unit tests for SetEntityIdAttribute functionality.
/// </summary>
[TestClass]
public class SetEntityIdAttributeTests
{
    [TestMethod]
    public void Constructor_CreatesInstance_WithDefaultValues()
    {
        // Arrange & Act
        var attribute = new SetEntityIdAttribute();

        // Assert
        Assert.IsNotNull(attribute);
        Assert.AreEqual("Id", attribute.IdColumnName);
    }

    [TestMethod]
    public void IdColumnName_DefaultValue_IsId()
    {
        // Arrange & Act
        var attribute = new SetEntityIdAttribute();

        // Assert
        Assert.AreEqual("Id", attribute.IdColumnName);
    }

    [TestMethod]
    public void IdColumnName_CanBeSet_StoresValue()
    {
        // Arrange
        var attribute = new SetEntityIdAttribute();

        // Act
        attribute.IdColumnName = "EntityId";

        // Assert
        Assert.AreEqual("EntityId", attribute.IdColumnName);
    }

    [TestMethod]
    public void IdColumnName_CanBeSetToNull_StoresNull()
    {
        // Arrange
        var attribute = new SetEntityIdAttribute();

        // Act
        attribute.IdColumnName = null!;

        // Assert
        Assert.IsNull(attribute.IdColumnName);
    }

    [TestMethod]
    public void IdColumnName_CanBeSetToEmptyString_StoresEmptyString()
    {
        // Arrange
        var attribute = new SetEntityIdAttribute();

        // Act
        attribute.IdColumnName = string.Empty;

        // Assert
        Assert.AreEqual(string.Empty, attribute.IdColumnName);
    }

    [TestMethod]
    public void IdColumnName_CanBeSetMultipleTimes_StoresLatestValue()
    {
        // Arrange
        var attribute = new SetEntityIdAttribute();

        // Act
        attribute.IdColumnName = "FirstId";
        attribute.IdColumnName = "SecondId";
        attribute.IdColumnName = "ThirdId";

        // Assert
        Assert.AreEqual("ThirdId", attribute.IdColumnName);
    }

    [TestMethod]
    public void AttributeUsage_AllowsMethodTarget()
    {
        // Arrange
        var attributeType = typeof(SetEntityIdAttribute);

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
        var attributeType = typeof(SetEntityIdAttribute);

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
        var attributeType = typeof(SetEntityIdAttribute);

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
        var attributeType = typeof(SetEntityIdAttribute);

        // Act & Assert
        Assert.IsTrue(attributeType.IsSealed);
    }

    [TestMethod]
    public void Attribute_InheritsFromAttribute()
    {
        // Arrange
        var attributeType = typeof(SetEntityIdAttribute);

        // Act & Assert
        Assert.IsTrue(typeof(Attribute).IsAssignableFrom(attributeType));
    }

    [TestMethod]
    public void MultipleInstances_AreIndependent()
    {
        // Arrange & Act
        var attribute1 = new SetEntityIdAttribute { IdColumnName = "Id1" };
        var attribute2 = new SetEntityIdAttribute { IdColumnName = "Id2" };

        // Assert
        Assert.AreEqual("Id1", attribute1.IdColumnName);
        Assert.AreEqual("Id2", attribute2.IdColumnName);
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
        [SetEntityId]
        Task InsertAsync(TestEntity entity);

        [SetEntityId(IdColumnName = "CustomId")]
        Task InsertWithCustomIdAsync(TestEntity entity);

        [SetEntityId]
        void InsertSync(TestEntity entity);
    }

    [TestMethod]
    public void Attribute_CanBeAppliedToMethod()
    {
        // Arrange
        var methodInfo = typeof(ITestRepository).GetMethod(nameof(ITestRepository.InsertAsync));
        Assert.IsNotNull(methodInfo);

        // Act
        var attribute = methodInfo.GetCustomAttribute<SetEntityIdAttribute>();

        // Assert
        Assert.IsNotNull(attribute);
        Assert.AreEqual("Id", attribute.IdColumnName);
    }

    [TestMethod]
    public void Attribute_WithCustomIdColumnName_CanBeAppliedToMethod()
    {
        // Arrange
        var methodInfo = typeof(ITestRepository).GetMethod(nameof(ITestRepository.InsertWithCustomIdAsync));
        Assert.IsNotNull(methodInfo);

        // Act
        var attribute = methodInfo.GetCustomAttribute<SetEntityIdAttribute>();

        // Assert
        Assert.IsNotNull(attribute);
        Assert.AreEqual("CustomId", attribute.IdColumnName);
    }

    [TestMethod]
    public void Attribute_CanBeAppliedToSynchronousMethod()
    {
        // Arrange
        var methodInfo = typeof(ITestRepository).GetMethod(nameof(ITestRepository.InsertSync));
        Assert.IsNotNull(methodInfo);

        // Act
        var attribute = methodInfo.GetCustomAttribute<SetEntityIdAttribute>();

        // Assert
        Assert.IsNotNull(attribute);
        Assert.AreEqual("Id", attribute.IdColumnName);
    }

    [TestMethod]
    public void Properties_CanBeSetInObjectInitializer()
    {
        // Arrange & Act
        var attribute = new SetEntityIdAttribute
        {
            IdColumnName = "PrimaryKey"
        };

        // Assert
        Assert.AreEqual("PrimaryKey", attribute.IdColumnName);
    }

    [TestMethod]
    public void IdColumnName_Property_IsReadWrite()
    {
        // Arrange
        var property = typeof(SetEntityIdAttribute).GetProperty(nameof(SetEntityIdAttribute.IdColumnName));

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
        var attribute = new SetEntityIdAttribute();

        // Act
        attribute.IdColumnName = "Entity_Id_123";

        // Assert
        Assert.AreEqual("Entity_Id_123", attribute.IdColumnName);
    }

    [TestMethod]
    public void IdColumnName_WithUnicodeCharacters_StoresValue()
    {
        // Arrange
        var attribute = new SetEntityIdAttribute();

        // Act
        attribute.IdColumnName = "实体ID";

        // Assert
        Assert.AreEqual("实体ID", attribute.IdColumnName);
    }

    [TestMethod]
    public void IdColumnName_WithLongString_StoresValue()
    {
        // Arrange
        var attribute = new SetEntityIdAttribute();
        var longName = new string('X', 500);

        // Act
        attribute.IdColumnName = longName;

        // Assert
        Assert.AreEqual(longName, attribute.IdColumnName);
    }

    [TestMethod]
    public void Attribute_HasIdColumnNameProperty()
    {
        // Arrange
        var attributeType = typeof(SetEntityIdAttribute);

        // Act
        var properties = attributeType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        var idColumnNameProperty = properties.FirstOrDefault(p => p.Name == nameof(SetEntityIdAttribute.IdColumnName));

        // Assert
        Assert.IsNotNull(idColumnNameProperty);
        Assert.AreEqual(typeof(string), idColumnNameProperty.PropertyType);
    }

    // Test class hierarchy
    private interface IBaseRepository
    {
        [SetEntityId]
        Task BaseInsertAsync(TestEntity entity);
    }

    private interface IDerivedRepository : IBaseRepository
    {
        Task DerivedInsertAsync(TestEntity entity);
    }

    [TestMethod]
    public void Attribute_IsNotInherited_DerivedMethodDoesNotHaveAttribute()
    {
        // Arrange
        var baseMethod = typeof(IBaseRepository).GetMethod(nameof(IBaseRepository.BaseInsertAsync));
        var derivedMethod = typeof(IDerivedRepository).GetMethod(nameof(IDerivedRepository.DerivedInsertAsync));

        // Act
        var baseAttribute = baseMethod?.GetCustomAttribute<SetEntityIdAttribute>();
        var derivedAttribute = derivedMethod?.GetCustomAttribute<SetEntityIdAttribute>();

        // Assert
        Assert.IsNotNull(baseAttribute);
        Assert.IsNull(derivedAttribute);
    }

    [TestMethod]
    public void Constructor_WithNoParameters_CreatesValidInstance()
    {
        // Arrange & Act
        var attribute = new SetEntityIdAttribute();

        // Assert
        Assert.IsNotNull(attribute);
        Assert.IsInstanceOfType(attribute, typeof(SetEntityIdAttribute));
        Assert.IsInstanceOfType(attribute, typeof(Attribute));
    }

    [TestMethod]
    public void IdColumnName_AfterMultipleChanges_RetainsLatestValue()
    {
        // Arrange
        var attribute = new SetEntityIdAttribute();
        var values = new[] { "Id1", "Id2", "Id3", "Id4", "Id5" };

        // Act
        foreach (var value in values)
        {
            attribute.IdColumnName = value;
        }

        // Assert
        Assert.AreEqual("Id5", attribute.IdColumnName);
    }
}
