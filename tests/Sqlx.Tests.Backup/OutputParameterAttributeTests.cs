// <copyright file="OutputParameterAttributeTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;
using System;
using System.Data;
using System.Linq;
using System.Reflection;

namespace Sqlx.Tests;

/// <summary>
/// Unit tests for OutputParameterAttribute functionality.
/// </summary>
[TestClass]
public class OutputParameterAttributeTests
{
    [TestMethod]
    public void Constructor_WithDbType_SetsDbTypeProperty()
    {
        // Arrange & Act
        var attribute = new OutputParameterAttribute(DbType.Int32);

        // Assert
        Assert.AreEqual(DbType.Int32, attribute.DbType);
    }

    [TestMethod]
    public void Constructor_WithStringDbType_SetsDbTypeProperty()
    {
        // Arrange & Act
        var attribute = new OutputParameterAttribute(DbType.String);

        // Assert
        Assert.AreEqual(DbType.String, attribute.DbType);
    }

    [TestMethod]
    public void Constructor_WithDateTimeDbType_SetsDbTypeProperty()
    {
        // Arrange & Act
        var attribute = new OutputParameterAttribute(DbType.DateTime);

        // Assert
        Assert.AreEqual(DbType.DateTime, attribute.DbType);
    }

    [TestMethod]
    public void Size_DefaultValue_IsZero()
    {
        // Arrange & Act
        var attribute = new OutputParameterAttribute(DbType.String);

        // Assert
        Assert.AreEqual(0, attribute.Size);
    }

    [TestMethod]
    public void Size_CanBeSet_StoresValue()
    {
        // Arrange
        var attribute = new OutputParameterAttribute(DbType.String);

        // Act
        attribute.Size = 255;

        // Assert
        Assert.AreEqual(255, attribute.Size);
    }

    [TestMethod]
    public void Size_CanBeSetMultipleTimes_StoresLatestValue()
    {
        // Arrange
        var attribute = new OutputParameterAttribute(DbType.String);

        // Act
        attribute.Size = 100;
        attribute.Size = 200;
        attribute.Size = 300;

        // Assert
        Assert.AreEqual(300, attribute.Size);
    }

    [TestMethod]
    public void AttributeUsage_AllowsParameterTarget()
    {
        // Arrange
        var attributeType = typeof(OutputParameterAttribute);

        // Act
        var usageAttribute = attributeType.GetCustomAttribute<AttributeUsageAttribute>();

        // Assert
        Assert.IsNotNull(usageAttribute);
        Assert.AreEqual(AttributeTargets.Parameter, usageAttribute.ValidOn);
    }

    [TestMethod]
    public void AttributeUsage_DoesNotAllowMultiple()
    {
        // Arrange
        var attributeType = typeof(OutputParameterAttribute);

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
        var attributeType = typeof(OutputParameterAttribute);

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
        var attributeType = typeof(OutputParameterAttribute);

        // Act & Assert
        Assert.IsTrue(attributeType.IsSealed);
    }

    [TestMethod]
    public void Attribute_InheritsFromAttribute()
    {
        // Arrange
        var attributeType = typeof(OutputParameterAttribute);

        // Act & Assert
        Assert.IsTrue(typeof(Attribute).IsAssignableFrom(attributeType));
    }

    [TestMethod]
    public void Constructor_WithAllDbTypes_AllSupported()
    {
        // Arrange & Act & Assert
        foreach (DbType dbType in Enum.GetValues(typeof(DbType)))
        {
            var attribute = new OutputParameterAttribute(dbType);
            Assert.AreEqual(dbType, attribute.DbType);
        }
    }

    [TestMethod]
    public void Size_WithNegativeValue_StoresValue()
    {
        // Arrange
        var attribute = new OutputParameterAttribute(DbType.String);

        // Act
        attribute.Size = -1;

        // Assert
        Assert.AreEqual(-1, attribute.Size);
    }

    [TestMethod]
    public void Size_WithMaxValue_StoresValue()
    {
        // Arrange
        var attribute = new OutputParameterAttribute(DbType.String);

        // Act
        attribute.Size = int.MaxValue;

        // Assert
        Assert.AreEqual(int.MaxValue, attribute.Size);
    }

    [TestMethod]
    public void MultipleInstances_WithSameDbType_AreIndependent()
    {
        // Arrange & Act
        var attribute1 = new OutputParameterAttribute(DbType.Int32);
        var attribute2 = new OutputParameterAttribute(DbType.Int32);
        attribute1.Size = 100;
        attribute2.Size = 200;

        // Assert
        Assert.AreEqual(DbType.Int32, attribute1.DbType);
        Assert.AreEqual(DbType.Int32, attribute2.DbType);
        Assert.AreEqual(100, attribute1.Size);
        Assert.AreEqual(200, attribute2.Size);
    }

    [TestMethod]
    public void MultipleInstances_WithDifferentDbTypes_AreIndependent()
    {
        // Arrange & Act
        var attribute1 = new OutputParameterAttribute(DbType.Int32);
        var attribute2 = new OutputParameterAttribute(DbType.String);
        attribute1.Size = 100;
        attribute2.Size = 200;

        // Assert
        Assert.AreEqual(DbType.Int32, attribute1.DbType);
        Assert.AreEqual(DbType.String, attribute2.DbType);
        Assert.AreEqual(100, attribute1.Size);
        Assert.AreEqual(200, attribute2.Size);
    }

    // Test method to verify attribute can be applied to parameters
    private void TestMethod([OutputParameter(DbType.Int32)] out int value)
    {
        value = 0;
    }

    [TestMethod]
    public void Attribute_CanBeAppliedToParameter()
    {
        // Arrange
        var method = GetType().GetMethod("TestMethod", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(method);
        var parameter = method.GetParameters().First();

        // Act
        var attribute = parameter.GetCustomAttribute<OutputParameterAttribute>();

        // Assert
        Assert.IsNotNull(attribute);
        Assert.AreEqual(DbType.Int32, attribute.DbType);
    }

    // Test method with Size property
    private void TestMethodWithSize([OutputParameter(DbType.String, Size = 255)] out string value)
    {
        value = string.Empty;
    }

    [TestMethod]
    public void Attribute_WithSizeProperty_CanBeAppliedToParameter()
    {
        // Arrange
        var method = GetType().GetMethod("TestMethodWithSize", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(method);
        var parameter = method.GetParameters().First();

        // Act
        var attribute = parameter.GetCustomAttribute<OutputParameterAttribute>();

        // Assert
        Assert.IsNotNull(attribute);
        Assert.AreEqual(DbType.String, attribute.DbType);
        Assert.AreEqual(255, attribute.Size);
    }

    // Test method with ref parameter (InputOutput mode)
    private void TestMethodWithRefParameter([OutputParameter(DbType.Int32)] ref int value)
    {
        value = value + 1;
    }

    [TestMethod]
    public void Attribute_CanBeAppliedToRefParameter()
    {
        // Arrange
        var method = GetType().GetMethod("TestMethodWithRefParameter", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(method);
        var parameter = method.GetParameters().First();

        // Act
        var attribute = parameter.GetCustomAttribute<OutputParameterAttribute>();

        // Assert
        Assert.IsNotNull(attribute);
        Assert.AreEqual(DbType.Int32, attribute.DbType);
        Assert.IsTrue(parameter.ParameterType.IsByRef);
    }

    // Test method with ref string parameter
    private void TestMethodWithRefString([OutputParameter(DbType.String, Size = 100)] ref string value)
    {
        value = value + " modified";
    }

    [TestMethod]
    public void Attribute_CanBeAppliedToRefStringParameter()
    {
        // Arrange
        var method = GetType().GetMethod("TestMethodWithRefString", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(method);
        var parameter = method.GetParameters().First();

        // Act
        var attribute = parameter.GetCustomAttribute<OutputParameterAttribute>();

        // Assert
        Assert.IsNotNull(attribute);
        Assert.AreEqual(DbType.String, attribute.DbType);
        Assert.AreEqual(100, attribute.Size);
        Assert.IsTrue(parameter.ParameterType.IsByRef);
    }
}
