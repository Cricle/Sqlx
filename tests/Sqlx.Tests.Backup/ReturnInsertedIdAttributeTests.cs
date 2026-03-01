// <copyright file="ReturnInsertedIdAttributeTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Sqlx.Tests;

/// <summary>
/// Unit tests for ReturnInsertedIdAttribute functionality.
/// </summary>
[TestClass]
public class ReturnInsertedIdAttributeTests
{
    [TestMethod]
    public void Constructor_CreatesInstance_WithDefaultValues()
    {
        // Arrange & Act
        var attribute = new ReturnInsertedIdAttribute();

        // Assert
        Assert.IsNotNull(attribute);
        Assert.AreEqual("Id", attribute.IdColumnName);
        Assert.IsFalse(attribute.UseValueTask);
    }

    [TestMethod]
    public void IdColumnName_DefaultValue_IsId()
    {
        // Arrange & Act
        var attribute = new ReturnInsertedIdAttribute();

        // Assert
        Assert.AreEqual("Id", attribute.IdColumnName);
    }

    [TestMethod]
    public void IdColumnName_CanBeSet_StoresValue()
    {
        // Arrange
        var attribute = new ReturnInsertedIdAttribute();

        // Act
        attribute.IdColumnName = "UserId";

        // Assert
        Assert.AreEqual("UserId", attribute.IdColumnName);
    }

    [TestMethod]
    public void IdColumnName_CanBeSetToNull_StoresNull()
    {
        // Arrange
        var attribute = new ReturnInsertedIdAttribute();

        // Act
        attribute.IdColumnName = null!;

        // Assert
        Assert.IsNull(attribute.IdColumnName);
    }

    [TestMethod]
    public void IdColumnName_CanBeSetToEmptyString_StoresEmptyString()
    {
        // Arrange
        var attribute = new ReturnInsertedIdAttribute();

        // Act
        attribute.IdColumnName = string.Empty;

        // Assert
        Assert.AreEqual(string.Empty, attribute.IdColumnName);
    }

    [TestMethod]
    public void IdColumnName_CanBeSetMultipleTimes_StoresLatestValue()
    {
        // Arrange
        var attribute = new ReturnInsertedIdAttribute();

        // Act
        attribute.IdColumnName = "FirstId";
        attribute.IdColumnName = "SecondId";
        attribute.IdColumnName = "ThirdId";

        // Assert
        Assert.AreEqual("ThirdId", attribute.IdColumnName);
    }

    [TestMethod]
    public void UseValueTask_DefaultValue_IsFalse()
    {
        // Arrange & Act
        var attribute = new ReturnInsertedIdAttribute();

        // Assert
        Assert.IsFalse(attribute.UseValueTask);
    }

    [TestMethod]
    public void UseValueTask_CanBeSetToTrue_StoresTrue()
    {
        // Arrange
        var attribute = new ReturnInsertedIdAttribute();

        // Act
        attribute.UseValueTask = true;

        // Assert
        Assert.IsTrue(attribute.UseValueTask);
    }

    [TestMethod]
    public void UseValueTask_CanBeSetToFalse_StoresFalse()
    {
        // Arrange
        var attribute = new ReturnInsertedIdAttribute();
        attribute.UseValueTask = true;

        // Act
        attribute.UseValueTask = false;

        // Assert
        Assert.IsFalse(attribute.UseValueTask);
    }

    [TestMethod]
    public void UseValueTask_CanBeToggledMultipleTimes_StoresLatestValue()
    {
        // Arrange
        var attribute = new ReturnInsertedIdAttribute();

        // Act & Assert
        attribute.UseValueTask = true;
        Assert.IsTrue(attribute.UseValueTask);

        attribute.UseValueTask = false;
        Assert.IsFalse(attribute.UseValueTask);

        attribute.UseValueTask = true;
        Assert.IsTrue(attribute.UseValueTask);
    }

    [TestMethod]
    public void AttributeUsage_AllowsMethodTarget()
    {
        // Arrange
        var attributeType = typeof(ReturnInsertedIdAttribute);

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
        var attributeType = typeof(ReturnInsertedIdAttribute);

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
        var attributeType = typeof(ReturnInsertedIdAttribute);

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
        var attributeType = typeof(ReturnInsertedIdAttribute);

        // Act & Assert
        Assert.IsTrue(attributeType.IsSealed);
    }

    [TestMethod]
    public void Attribute_InheritsFromAttribute()
    {
        // Arrange
        var attributeType = typeof(ReturnInsertedIdAttribute);

        // Act & Assert
        Assert.IsTrue(typeof(Attribute).IsAssignableFrom(attributeType));
    }

    [TestMethod]
    public void MultipleInstances_AreIndependent()
    {
        // Arrange & Act
        var attribute1 = new ReturnInsertedIdAttribute { IdColumnName = "Id1", UseValueTask = true };
        var attribute2 = new ReturnInsertedIdAttribute { IdColumnName = "Id2", UseValueTask = false };

        // Assert
        Assert.AreEqual("Id1", attribute1.IdColumnName);
        Assert.IsTrue(attribute1.UseValueTask);
        Assert.AreEqual("Id2", attribute2.IdColumnName);
        Assert.IsFalse(attribute2.UseValueTask);
        Assert.AreNotSame(attribute1, attribute2);
    }

    // Test interface with method
    private interface ITestRepository
    {
        [ReturnInsertedId]
        Task<long> InsertAsync();

        [ReturnInsertedId(IdColumnName = "CustomId", UseValueTask = true)]
        ValueTask<int> InsertWithCustomIdAsync();
    }

    [TestMethod]
    public void Attribute_CanBeAppliedToMethod()
    {
        // Arrange
        var methodInfo = typeof(ITestRepository).GetMethod(nameof(ITestRepository.InsertAsync));
        Assert.IsNotNull(methodInfo);

        // Act
        var attribute = methodInfo.GetCustomAttribute<ReturnInsertedIdAttribute>();

        // Assert
        Assert.IsNotNull(attribute);
        Assert.AreEqual("Id", attribute.IdColumnName);
        Assert.IsFalse(attribute.UseValueTask);
    }

    [TestMethod]
    public void Attribute_WithCustomProperties_CanBeAppliedToMethod()
    {
        // Arrange
        var methodInfo = typeof(ITestRepository).GetMethod(nameof(ITestRepository.InsertWithCustomIdAsync));
        Assert.IsNotNull(methodInfo);

        // Act
        var attribute = methodInfo.GetCustomAttribute<ReturnInsertedIdAttribute>();

        // Assert
        Assert.IsNotNull(attribute);
        Assert.AreEqual("CustomId", attribute.IdColumnName);
        Assert.IsTrue(attribute.UseValueTask);
    }

    [TestMethod]
    public void IdColumnName_WithSpecialCharacters_StoresValue()
    {
        // Arrange
        var attribute = new ReturnInsertedIdAttribute();

        // Act
        attribute.IdColumnName = "User_Id_123";

        // Assert
        Assert.AreEqual("User_Id_123", attribute.IdColumnName);
    }

    [TestMethod]
    public void IdColumnName_WithUnicodeCharacters_StoresValue()
    {
        // Arrange
        var attribute = new ReturnInsertedIdAttribute();

        // Act
        attribute.IdColumnName = "用户ID";

        // Assert
        Assert.AreEqual("用户ID", attribute.IdColumnName);
    }

    [TestMethod]
    public void IdColumnName_WithLongString_StoresValue()
    {
        // Arrange
        var attribute = new ReturnInsertedIdAttribute();
        var longName = new string('A', 1000);

        // Act
        attribute.IdColumnName = longName;

        // Assert
        Assert.AreEqual(longName, attribute.IdColumnName);
    }

    [TestMethod]
    public void Properties_CanBeSetInObjectInitializer()
    {
        // Arrange & Act
        var attribute = new ReturnInsertedIdAttribute
        {
            IdColumnName = "EntityId",
            UseValueTask = true
        };

        // Assert
        Assert.AreEqual("EntityId", attribute.IdColumnName);
        Assert.IsTrue(attribute.UseValueTask);
    }

    [TestMethod]
    public void Properties_CanBeSetInAttributeConstructor()
    {
        // This test verifies that properties can be set via attribute syntax
        // The actual verification is done through reflection on the test interface
        var methodInfo = typeof(ITestRepository).GetMethod(nameof(ITestRepository.InsertWithCustomIdAsync));
        Assert.IsNotNull(methodInfo);

        var attribute = methodInfo.GetCustomAttribute<ReturnInsertedIdAttribute>();
        Assert.IsNotNull(attribute);
        Assert.AreEqual("CustomId", attribute.IdColumnName);
        Assert.IsTrue(attribute.UseValueTask);
    }

    // Test class hierarchy
    private interface IBaseRepository
    {
        [ReturnInsertedId]
        Task<long> BaseInsertAsync();
    }

    private interface IDerivedRepository : IBaseRepository
    {
        Task<long> DerivedInsertAsync();
    }

    [TestMethod]
    public void Attribute_IsNotInherited_DerivedMethodDoesNotHaveAttribute()
    {
        // Arrange
        var baseMethod = typeof(IBaseRepository).GetMethod(nameof(IBaseRepository.BaseInsertAsync));
        var derivedMethod = typeof(IDerivedRepository).GetMethod(nameof(IDerivedRepository.DerivedInsertAsync));

        // Act
        var baseAttribute = baseMethod?.GetCustomAttribute<ReturnInsertedIdAttribute>();
        var derivedAttribute = derivedMethod?.GetCustomAttribute<ReturnInsertedIdAttribute>();

        // Assert
        Assert.IsNotNull(baseAttribute);
        Assert.IsNull(derivedAttribute);
    }

    [TestMethod]
    public void IdColumnName_Property_IsReadWrite()
    {
        // Arrange
        var property = typeof(ReturnInsertedIdAttribute).GetProperty(nameof(ReturnInsertedIdAttribute.IdColumnName));

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
        var property = typeof(ReturnInsertedIdAttribute).GetProperty(nameof(ReturnInsertedIdAttribute.UseValueTask));

        // Assert
        Assert.IsNotNull(property);
        Assert.IsTrue(property.CanRead);
        Assert.IsTrue(property.CanWrite);
        Assert.IsNotNull(property.GetMethod);
        Assert.IsNotNull(property.SetMethod);
    }
}
