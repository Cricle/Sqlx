// <copyright file="SqlxContextAttributeTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;
using System;
using System.Linq;
using System.Reflection;

namespace Sqlx.Tests;

/// <summary>
/// Unit tests for SqlxContextAttribute functionality.
/// </summary>
[TestClass]
public class SqlxContextAttributeTests
{
    [TestMethod]
    public void Constructor_CreatesInstance()
    {
        // Arrange & Act
        var attribute = new SqlxContextAttribute();

        // Assert
        Assert.IsNotNull(attribute);
    }

    [TestMethod]
    public void Constructor_WithNoParameters_CreatesValidInstance()
    {
        // Arrange & Act
        var attribute = new SqlxContextAttribute();

        // Assert
        Assert.IsNotNull(attribute);
        Assert.IsInstanceOfType(attribute, typeof(SqlxContextAttribute));
        Assert.IsInstanceOfType(attribute, typeof(Attribute));
    }

    [TestMethod]
    public void AttributeUsage_AllowsClassTarget()
    {
        // Arrange
        var attributeType = typeof(SqlxContextAttribute);

        // Act
        var usageAttribute = attributeType.GetCustomAttribute<AttributeUsageAttribute>();

        // Assert
        Assert.IsNotNull(usageAttribute);
        Assert.AreEqual(AttributeTargets.Class, usageAttribute.ValidOn);
    }

    [TestMethod]
    public void AttributeUsage_DoesNotAllowMultiple()
    {
        // Arrange
        var attributeType = typeof(SqlxContextAttribute);

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
        var attributeType = typeof(SqlxContextAttribute);

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
        var attributeType = typeof(SqlxContextAttribute);

        // Act & Assert
        Assert.IsTrue(attributeType.IsSealed);
    }

    [TestMethod]
    public void Attribute_InheritsFromAttribute()
    {
        // Arrange
        var attributeType = typeof(SqlxContextAttribute);

        // Act & Assert
        Assert.IsTrue(typeof(Attribute).IsAssignableFrom(attributeType));
    }

    [TestMethod]
    public void MultipleInstances_AreIndependent()
    {
        // Arrange & Act
        var attribute1 = new SqlxContextAttribute();
        var attribute2 = new SqlxContextAttribute();

        // Assert
        Assert.IsNotNull(attribute1);
        Assert.IsNotNull(attribute2);
        Assert.AreNotSame(attribute1, attribute2);
    }

    // Test class with attribute
    [SqlxContext]
    private class TestContext { }

    [TestMethod]
    public void Attribute_CanBeAppliedToClass()
    {
        // Arrange
        var classType = typeof(TestContext);

        // Act
        var attribute = classType.GetCustomAttribute<SqlxContextAttribute>();

        // Assert
        Assert.IsNotNull(attribute);
    }

    // Test class hierarchy
    [SqlxContext]
    private class BaseContext { }

    private class DerivedContext : BaseContext { }

    [TestMethod]
    public void Attribute_IsNotInherited_DerivedClassDoesNotHaveAttribute()
    {
        // Arrange
        var derivedType = typeof(DerivedContext);

        // Act
        var attribute = derivedType.GetCustomAttribute<SqlxContextAttribute>(inherit: false);

        // Assert
        Assert.IsNull(attribute);
    }

    [TestMethod]
    public void Attribute_IsNotInherited_BaseClassHasAttribute()
    {
        // Arrange
        var baseType = typeof(BaseContext);

        // Act
        var attribute = baseType.GetCustomAttribute<SqlxContextAttribute>();

        // Assert
        Assert.IsNotNull(attribute);
    }

    // Test multiple attributes on same class (should not be allowed)
    [TestMethod]
    public void Attribute_CannotBeAppliedMultipleTimes()
    {
        // This test verifies that AllowMultiple = false is enforced
        // We can't actually apply it multiple times in code, but we verify the attribute usage
        var attributeType = typeof(SqlxContextAttribute);
        var usageAttribute = attributeType.GetCustomAttribute<AttributeUsageAttribute>();

        Assert.IsNotNull(usageAttribute);
        Assert.IsFalse(usageAttribute.AllowMultiple);
    }

    [TestMethod]
    public void Attribute_HasNoPublicProperties()
    {
        // Arrange
        var attributeType = typeof(SqlxContextAttribute);

        // Act
        var properties = attributeType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        // Assert
        Assert.AreEqual(0, properties.Length);
    }

    [TestMethod]
    public void Attribute_HasParameterlessConstructor()
    {
        // Arrange
        var attributeType = typeof(SqlxContextAttribute);

        // Act
        var constructor = attributeType.GetConstructor(Type.EmptyTypes);

        // Assert
        Assert.IsNotNull(constructor);
        Assert.IsTrue(constructor.IsPublic);
    }

    [TestMethod]
    public void Constructor_CanBeCalledMultipleTimes()
    {
        // Arrange & Act
        var attribute1 = new SqlxContextAttribute();
        var attribute2 = new SqlxContextAttribute();
        var attribute3 = new SqlxContextAttribute();

        // Assert
        Assert.IsNotNull(attribute1);
        Assert.IsNotNull(attribute2);
        Assert.IsNotNull(attribute3);
        Assert.AreNotSame(attribute1, attribute2);
        Assert.AreNotSame(attribute2, attribute3);
        Assert.AreNotSame(attribute1, attribute3);
    }

    // Test with partial class
    [SqlxContext]
    private partial class PartialTestContext { }

    [TestMethod]
    public void Attribute_CanBeAppliedToPartialClass()
    {
        // Arrange
        var classType = typeof(PartialTestContext);

        // Act
        var attribute = classType.GetCustomAttribute<SqlxContextAttribute>();

        // Assert
        Assert.IsNotNull(attribute);
    }

    // Test with abstract class
    [SqlxContext]
    private abstract class AbstractTestContext { }

    [TestMethod]
    public void Attribute_CanBeAppliedToAbstractClass()
    {
        // Arrange
        var classType = typeof(AbstractTestContext);

        // Act
        var attribute = classType.GetCustomAttribute<SqlxContextAttribute>();

        // Assert
        Assert.IsNotNull(attribute);
    }

    // Test with sealed class
    [SqlxContext]
    private sealed class SealedTestContext { }

    [TestMethod]
    public void Attribute_CanBeAppliedToSealedClass()
    {
        // Arrange
        var classType = typeof(SealedTestContext);

        // Act
        var attribute = classType.GetCustomAttribute<SqlxContextAttribute>();

        // Assert
        Assert.IsNotNull(attribute);
    }

    // Test with generic class
    [SqlxContext]
    private class GenericTestContext<T> { }

    [TestMethod]
    public void Attribute_CanBeAppliedToGenericClass()
    {
        // Arrange
        var classType = typeof(GenericTestContext<>);

        // Act
        var attribute = classType.GetCustomAttribute<SqlxContextAttribute>();

        // Assert
        Assert.IsNotNull(attribute);
    }

    // Test with nested class
    private class OuterClass
    {
        [SqlxContext]
        public class NestedContext { }
    }

    [TestMethod]
    public void Attribute_CanBeAppliedToNestedClass()
    {
        // Arrange
        var classType = typeof(OuterClass.NestedContext);

        // Act
        var attribute = classType.GetCustomAttribute<SqlxContextAttribute>();

        // Assert
        Assert.IsNotNull(attribute);
    }

    [TestMethod]
    public void Attribute_TypeIsPublic()
    {
        // Arrange
        var attributeType = typeof(SqlxContextAttribute);

        // Act & Assert
        Assert.IsTrue(attributeType.IsPublic);
    }

    [TestMethod]
    public void Attribute_IsInCorrectNamespace()
    {
        // Arrange
        var attributeType = typeof(SqlxContextAttribute);

        // Act & Assert
        Assert.AreEqual("Sqlx.Annotations", attributeType.Namespace);
    }
}
