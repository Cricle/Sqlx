// <copyright file="IncludeRepositoryAttributeTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;
using System;
using System.Linq;
using System.Reflection;

namespace Sqlx.Tests.Attributes;

/// <summary>
/// Unit tests for IncludeRepositoryAttribute functionality.
/// </summary>
[TestClass]
public class IncludeRepositoryAttributeTests
{
    // Test repository types for testing
    private class TestRepository { }
    private class AnotherRepository { }
    private interface ITestRepository { }

    [TestMethod]
    public void Constructor_WithValidType_SetsRepositoryTypeProperty()
    {
        // Arrange & Act
        var attribute = new IncludeRepositoryAttribute(typeof(TestRepository));

        // Assert
        Assert.AreEqual(typeof(TestRepository), attribute.RepositoryType);
    }

    [TestMethod]
    public void Constructor_WithNullType_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        var exception = Assert.ThrowsException<ArgumentNullException>(() =>
            new IncludeRepositoryAttribute(null!));

        Assert.AreEqual("repositoryType", exception.ParamName);
    }

    [TestMethod]
    public void Constructor_WithInterfaceType_SetsRepositoryTypeProperty()
    {
        // Arrange & Act
        var attribute = new IncludeRepositoryAttribute(typeof(ITestRepository));

        // Assert
        Assert.AreEqual(typeof(ITestRepository), attribute.RepositoryType);
    }

    [TestMethod]
    public void Constructor_WithGenericType_SetsRepositoryTypeProperty()
    {
        // Arrange & Act
        var attribute = new IncludeRepositoryAttribute(typeof(System.Collections.Generic.List<string>));

        // Assert
        Assert.AreEqual(typeof(System.Collections.Generic.List<string>), attribute.RepositoryType);
    }

    [TestMethod]
    public void RepositoryType_IsReadOnly_CannotBeChanged()
    {
        // Arrange
        var attribute = new IncludeRepositoryAttribute(typeof(TestRepository));

        // Act - Try to get property setter (should not exist)
        var property = typeof(IncludeRepositoryAttribute).GetProperty(nameof(IncludeRepositoryAttribute.RepositoryType));

        // Assert
        Assert.IsNotNull(property);
        Assert.IsNull(property.SetMethod);
        Assert.IsTrue(property.CanRead);
        Assert.IsFalse(property.CanWrite);
    }

    [TestMethod]
    public void AttributeUsage_AllowsClassTarget()
    {
        // Arrange
        var attributeType = typeof(IncludeRepositoryAttribute);

        // Act
        var usageAttribute = attributeType.GetCustomAttribute<AttributeUsageAttribute>();

        // Assert
        Assert.IsNotNull(usageAttribute);
        Assert.AreEqual(AttributeTargets.Class, usageAttribute.ValidOn);
    }

    [TestMethod]
    public void AttributeUsage_AllowsMultiple()
    {
        // Arrange
        var attributeType = typeof(IncludeRepositoryAttribute);

        // Act
        var usageAttribute = attributeType.GetCustomAttribute<AttributeUsageAttribute>();

        // Assert
        Assert.IsNotNull(usageAttribute);
        Assert.IsTrue(usageAttribute.AllowMultiple);
    }

    [TestMethod]
    public void AttributeUsage_IsNotInherited()
    {
        // Arrange
        var attributeType = typeof(IncludeRepositoryAttribute);

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
        var attributeType = typeof(IncludeRepositoryAttribute);

        // Act & Assert
        Assert.IsTrue(attributeType.IsSealed);
    }

    [TestMethod]
    public void Attribute_InheritsFromAttribute()
    {
        // Arrange
        var attributeType = typeof(IncludeRepositoryAttribute);

        // Act & Assert
        Assert.IsTrue(typeof(Attribute).IsAssignableFrom(attributeType));
    }

    [TestMethod]
    public void MultipleInstances_WithSameType_AreIndependent()
    {
        // Arrange & Act
        var attribute1 = new IncludeRepositoryAttribute(typeof(TestRepository));
        var attribute2 = new IncludeRepositoryAttribute(typeof(TestRepository));

        // Assert
        Assert.AreEqual(typeof(TestRepository), attribute1.RepositoryType);
        Assert.AreEqual(typeof(TestRepository), attribute2.RepositoryType);
        Assert.AreNotSame(attribute1, attribute2);
    }

    [TestMethod]
    public void MultipleInstances_WithDifferentTypes_AreIndependent()
    {
        // Arrange & Act
        var attribute1 = new IncludeRepositoryAttribute(typeof(TestRepository));
        var attribute2 = new IncludeRepositoryAttribute(typeof(AnotherRepository));

        // Assert
        Assert.AreEqual(typeof(TestRepository), attribute1.RepositoryType);
        Assert.AreEqual(typeof(AnotherRepository), attribute2.RepositoryType);
        Assert.AreNotSame(attribute1, attribute2);
    }

    // Test class with single attribute
    [IncludeRepository(typeof(TestRepository))]
    private class TestContextSingle { }

    [TestMethod]
    public void Attribute_CanBeAppliedToClass()
    {
        // Arrange
        var classType = typeof(TestContextSingle);

        // Act
        var attribute = classType.GetCustomAttribute<IncludeRepositoryAttribute>();

        // Assert
        Assert.IsNotNull(attribute);
        Assert.AreEqual(typeof(TestRepository), attribute.RepositoryType);
    }

    // Test class with multiple attributes
    [IncludeRepository(typeof(TestRepository))]
    [IncludeRepository(typeof(AnotherRepository))]
    private class TestContextMultiple { }

    [TestMethod]
    public void Attribute_CanBeAppliedMultipleTimes()
    {
        // Arrange
        var classType = typeof(TestContextMultiple);

        // Act
        var attributes = classType.GetCustomAttributes<IncludeRepositoryAttribute>().ToArray();

        // Assert
        Assert.AreEqual(2, attributes.Length);
        Assert.IsTrue(attributes.Any(a => a.RepositoryType == typeof(TestRepository)));
        Assert.IsTrue(attributes.Any(a => a.RepositoryType == typeof(AnotherRepository)));
    }

    // Test class hierarchy
    [IncludeRepository(typeof(TestRepository))]
    private class BaseContext { }

    private class DerivedContext : BaseContext { }

    [TestMethod]
    public void Attribute_IsNotInherited_DerivedClassDoesNotHaveAttribute()
    {
        // Arrange
        var derivedType = typeof(DerivedContext);

        // Act
        var attribute = derivedType.GetCustomAttribute<IncludeRepositoryAttribute>(inherit: false);

        // Assert
        Assert.IsNull(attribute);
    }

    [TestMethod]
    public void Attribute_IsNotInherited_BaseClassHasAttribute()
    {
        // Arrange
        var baseType = typeof(BaseContext);

        // Act
        var attribute = baseType.GetCustomAttribute<IncludeRepositoryAttribute>();

        // Assert
        Assert.IsNotNull(attribute);
        Assert.AreEqual(typeof(TestRepository), attribute.RepositoryType);
    }

    [TestMethod]
    public void Constructor_WithAbstractType_SetsRepositoryTypeProperty()
    {
        // Arrange & Act
        var attribute = new IncludeRepositoryAttribute(typeof(System.IO.Stream));

        // Assert
        Assert.AreEqual(typeof(System.IO.Stream), attribute.RepositoryType);
    }

    [TestMethod]
    public void Constructor_WithValueType_SetsRepositoryTypeProperty()
    {
        // Arrange & Act
        var attribute = new IncludeRepositoryAttribute(typeof(int));

        // Assert
        Assert.AreEqual(typeof(int), attribute.RepositoryType);
    }

    [TestMethod]
    public void Constructor_WithNestedType_SetsRepositoryTypeProperty()
    {
        // Arrange & Act
        var attribute = new IncludeRepositoryAttribute(typeof(TestRepository));

        // Assert
        Assert.AreEqual(typeof(TestRepository), attribute.RepositoryType);
        Assert.IsTrue(attribute.RepositoryType.IsNested);
    }

    [TestMethod]
    public void RepositoryType_AfterConstruction_IsNotNull()
    {
        // Arrange & Act
        var attribute = new IncludeRepositoryAttribute(typeof(TestRepository));

        // Assert
        Assert.IsNotNull(attribute.RepositoryType);
    }

    [TestMethod]
    public void Constructor_WithOpenGenericType_SetsRepositoryTypeProperty()
    {
        // Arrange & Act
        var attribute = new IncludeRepositoryAttribute(typeof(System.Collections.Generic.List<>));

        // Assert
        Assert.AreEqual(typeof(System.Collections.Generic.List<>), attribute.RepositoryType);
        Assert.IsTrue(attribute.RepositoryType.IsGenericTypeDefinition);
    }

    [TestMethod]
    public void Constructor_WithClosedGenericType_SetsRepositoryTypeProperty()
    {
        // Arrange & Act
        var attribute = new IncludeRepositoryAttribute(typeof(System.Collections.Generic.List<int>));

        // Assert
        Assert.AreEqual(typeof(System.Collections.Generic.List<int>), attribute.RepositoryType);
        Assert.IsTrue(attribute.RepositoryType.IsGenericType);
        Assert.IsFalse(attribute.RepositoryType.IsGenericTypeDefinition);
    }

    [TestMethod]
    public void Constructor_WithArrayType_SetsRepositoryTypeProperty()
    {
        // Arrange & Act
        var attribute = new IncludeRepositoryAttribute(typeof(int[]));

        // Assert
        Assert.AreEqual(typeof(int[]), attribute.RepositoryType);
        Assert.IsTrue(attribute.RepositoryType.IsArray);
    }

    [TestMethod]
    public void Constructor_WithPointerType_SetsRepositoryTypeProperty()
    {
        // Arrange & Act
        var pointerType = typeof(int).MakePointerType();
        var attribute = new IncludeRepositoryAttribute(pointerType);

        // Assert
        Assert.AreEqual(pointerType, attribute.RepositoryType);
        Assert.IsTrue(attribute.RepositoryType.IsPointer);
    }

    [TestMethod]
    public void Constructor_WithByRefType_SetsRepositoryTypeProperty()
    {
        // Arrange & Act
        var byRefType = typeof(int).MakeByRefType();
        var attribute = new IncludeRepositoryAttribute(byRefType);

        // Assert
        Assert.AreEqual(byRefType, attribute.RepositoryType);
        Assert.IsTrue(attribute.RepositoryType.IsByRef);
    }
}
