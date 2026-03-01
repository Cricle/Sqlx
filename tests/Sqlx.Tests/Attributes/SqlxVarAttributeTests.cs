// <copyright file="SqlxVarAttributeTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;
using System;
using System.Reflection;

namespace Sqlx.Tests.Attributes;

/// <summary>
/// Unit tests for SqlxVarAttribute functionality.
/// </summary>
[TestClass]
public class SqlxVarAttributeTests
{
    [TestMethod]
    public void Constructor_WithValidVariableName_SetsVariableNameProperty()
    {
        // Arrange & Act
        var attribute = new SqlxVarAttribute("tenantId");

        // Assert
        Assert.AreEqual("tenantId", attribute.VariableName);
    }

    [TestMethod]
    public void Constructor_WithNullVariableName_ThrowsArgumentException()
    {
        // Arrange, Act & Assert
        var exception = Assert.ThrowsException<ArgumentException>(() =>
            new SqlxVarAttribute(null!));

        Assert.AreEqual("variableName", exception.ParamName);
        Assert.IsTrue(exception.Message.Contains("Variable name cannot be null, empty, or whitespace"));
    }

    [TestMethod]
    public void Constructor_WithEmptyVariableName_ThrowsArgumentException()
    {
        // Arrange, Act & Assert
        var exception = Assert.ThrowsException<ArgumentException>(() =>
            new SqlxVarAttribute(string.Empty));

        Assert.AreEqual("variableName", exception.ParamName);
        Assert.IsTrue(exception.Message.Contains("Variable name cannot be null, empty, or whitespace"));
    }

    [TestMethod]
    public void Constructor_WithWhitespaceVariableName_ThrowsArgumentException()
    {
        // Arrange, Act & Assert
        var exception = Assert.ThrowsException<ArgumentException>(() =>
            new SqlxVarAttribute("   "));

        Assert.AreEqual("variableName", exception.ParamName);
        Assert.IsTrue(exception.Message.Contains("Variable name cannot be null, empty, or whitespace"));
    }

    [TestMethod]
    public void Constructor_WithTabsAndSpaces_ThrowsArgumentException()
    {
        // Arrange, Act & Assert
        var exception = Assert.ThrowsException<ArgumentException>(() =>
            new SqlxVarAttribute("\t\n\r "));

        Assert.AreEqual("variableName", exception.ParamName);
    }

    [TestMethod]
    public void VariableName_IsReadOnly_CannotBeChanged()
    {
        // Arrange
        var attribute = new SqlxVarAttribute("test");

        // Act
        var property = typeof(SqlxVarAttribute).GetProperty(nameof(SqlxVarAttribute.VariableName));

        // Assert
        Assert.IsNotNull(property);
        Assert.IsTrue(property.CanRead);
        Assert.IsFalse(property.CanWrite);
        Assert.IsNotNull(property.GetMethod);
        Assert.IsNull(property.SetMethod);
    }

    [TestMethod]
    public void AttributeUsage_AllowsMethodTarget()
    {
        // Arrange
        var attributeType = typeof(SqlxVarAttribute);

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
        var attributeType = typeof(SqlxVarAttribute);

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
        var attributeType = typeof(SqlxVarAttribute);

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
        var attributeType = typeof(SqlxVarAttribute);

        // Act & Assert
        Assert.IsTrue(attributeType.IsSealed);
    }

    [TestMethod]
    public void Attribute_InheritsFromAttribute()
    {
        // Arrange
        var attributeType = typeof(SqlxVarAttribute);

        // Act & Assert
        Assert.IsTrue(typeof(Attribute).IsAssignableFrom(attributeType));
    }

    [TestMethod]
    public void MultipleInstances_WithSameVariableName_AreIndependent()
    {
        // Arrange & Act
        var attribute1 = new SqlxVarAttribute("tenantId");
        var attribute2 = new SqlxVarAttribute("tenantId");

        // Assert
        Assert.AreEqual("tenantId", attribute1.VariableName);
        Assert.AreEqual("tenantId", attribute2.VariableName);
        Assert.AreNotSame(attribute1, attribute2);
    }

    [TestMethod]
    public void MultipleInstances_WithDifferentVariableNames_AreIndependent()
    {
        // Arrange & Act
        var attribute1 = new SqlxVarAttribute("tenantId");
        var attribute2 = new SqlxVarAttribute("userId");

        // Assert
        Assert.AreEqual("tenantId", attribute1.VariableName);
        Assert.AreEqual("userId", attribute2.VariableName);
        Assert.AreNotSame(attribute1, attribute2);
    }

    // Test class with methods
    public class TestClass
    {
        [SqlxVar("tenantId")]
        public string GetTenantId() => "tenant123";

        [SqlxVar("userId")]
        public string GetUserId() => "user456";

        [SqlxVar("schema")]
        public string GetSchema() => "public";
    }

    [TestMethod]
    public void Attribute_CanBeAppliedToPublicMethod()
    {
        // Arrange
        var methodInfo = typeof(TestClass).GetMethod("GetTenantId");
        Assert.IsNotNull(methodInfo);

        // Act
        var attribute = methodInfo.GetCustomAttribute<SqlxVarAttribute>();

        // Assert
        Assert.IsNotNull(attribute);
        Assert.AreEqual("tenantId", attribute.VariableName);
    }

    [TestMethod]
    public void Attribute_CanBeAppliedToInstanceMethod()
    {
        // Arrange
        var methodInfo = typeof(TestClass).GetMethod("GetUserId");
        Assert.IsNotNull(methodInfo);

        // Act
        var attribute = methodInfo.GetCustomAttribute<SqlxVarAttribute>();

        // Assert
        Assert.IsNotNull(attribute);
        Assert.AreEqual("userId", attribute.VariableName);
    }

    [TestMethod]
    public void Attribute_CanBeAppliedToAnotherMethod()
    {
        // Arrange
        var methodInfo = typeof(TestClass).GetMethod("GetSchema");
        Assert.IsNotNull(methodInfo);

        // Act
        var attribute = methodInfo.GetCustomAttribute<SqlxVarAttribute>();

        // Assert
        Assert.IsNotNull(attribute);
        Assert.AreEqual("schema", attribute.VariableName);
    }

    [TestMethod]
    public void Constructor_WithSpecialCharacters_StoresVariableName()
    {
        // Arrange & Act
        var attribute = new SqlxVarAttribute("tenant_id_123");

        // Assert
        Assert.AreEqual("tenant_id_123", attribute.VariableName);
    }

    [TestMethod]
    public void Constructor_WithUnicodeCharacters_StoresVariableName()
    {
        // Arrange & Act
        var attribute = new SqlxVarAttribute("租户ID");

        // Assert
        Assert.AreEqual("租户ID", attribute.VariableName);
    }

    [TestMethod]
    public void Constructor_WithCamelCase_StoresVariableName()
    {
        // Arrange & Act
        var attribute = new SqlxVarAttribute("tenantId");

        // Assert
        Assert.AreEqual("tenantId", attribute.VariableName);
    }

    [TestMethod]
    public void Constructor_WithPascalCase_StoresVariableName()
    {
        // Arrange & Act
        var attribute = new SqlxVarAttribute("TenantId");

        // Assert
        Assert.AreEqual("TenantId", attribute.VariableName);
    }

    [TestMethod]
    public void Constructor_WithSnakeCase_StoresVariableName()
    {
        // Arrange & Act
        var attribute = new SqlxVarAttribute("tenant_id");

        // Assert
        Assert.AreEqual("tenant_id", attribute.VariableName);
    }

    [TestMethod]
    public void Constructor_WithKebabCase_StoresVariableName()
    {
        // Arrange & Act
        var attribute = new SqlxVarAttribute("tenant-id");

        // Assert
        Assert.AreEqual("tenant-id", attribute.VariableName);
    }

    [TestMethod]
    public void Constructor_WithNumbers_StoresVariableName()
    {
        // Arrange & Act
        var attribute = new SqlxVarAttribute("var123");

        // Assert
        Assert.AreEqual("var123", attribute.VariableName);
    }

    [TestMethod]
    public void Constructor_WithLongVariableName_StoresVariableName()
    {
        // Arrange
        var longName = new string('A', 500);

        // Act
        var attribute = new SqlxVarAttribute(longName);

        // Assert
        Assert.AreEqual(longName, attribute.VariableName);
    }

    [TestMethod]
    public void Constructor_WithSingleCharacter_StoresVariableName()
    {
        // Arrange & Act
        var attribute = new SqlxVarAttribute("x");

        // Assert
        Assert.AreEqual("x", attribute.VariableName);
    }

    [TestMethod]
    public void VariableName_AfterConstruction_IsNotNull()
    {
        // Arrange & Act
        var attribute = new SqlxVarAttribute("test");

        // Assert
        Assert.IsNotNull(attribute.VariableName);
    }

    // Test class hierarchy
    public class BaseClass
    {
        [SqlxVar("baseVar")]
        public virtual string GetBaseVar() => "base";
    }

    public class DerivedClass : BaseClass
    {
        public override string GetBaseVar() => "derived";
    }

    [TestMethod]
    public void Attribute_IsNotInherited_OverriddenMethodDoesNotHaveAttribute()
    {
        // Arrange
        var baseMethod = typeof(BaseClass).GetMethod("GetBaseVar");
        var derivedMethod = typeof(DerivedClass).GetMethod("GetBaseVar");

        // Act
        var baseAttribute = baseMethod?.GetCustomAttribute<SqlxVarAttribute>();
        var derivedAttribute = derivedMethod?.GetCustomAttribute<SqlxVarAttribute>(inherit: false);

        // Assert
        Assert.IsNotNull(baseAttribute);
        Assert.IsNull(derivedAttribute);
    }

    [TestMethod]
    public void Attribute_HasOnlyOneProperty()
    {
        // Arrange
        var attributeType = typeof(SqlxVarAttribute);

        // Act
        var properties = attributeType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        // Assert
        Assert.AreEqual(1, properties.Length);
        Assert.AreEqual(nameof(SqlxVarAttribute.VariableName), properties[0].Name);
    }

    [TestMethod]
    public void Attribute_TypeIsPublic()
    {
        // Arrange
        var attributeType = typeof(SqlxVarAttribute);

        // Act & Assert
        Assert.IsTrue(attributeType.IsPublic);
    }

    [TestMethod]
    public void Attribute_IsInCorrectNamespace()
    {
        // Arrange
        var attributeType = typeof(SqlxVarAttribute);

        // Act & Assert
        Assert.AreEqual("Sqlx.Annotations", attributeType.Namespace);
    }
}
