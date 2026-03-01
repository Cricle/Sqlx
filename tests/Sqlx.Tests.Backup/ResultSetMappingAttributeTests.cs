// <copyright file="ResultSetMappingAttributeTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;
using System;

namespace Sqlx.Tests;

[TestClass]
public class ResultSetMappingAttributeTests
{
    [TestMethod]
    public void Constructor_ValidParameters_SetsProperties()
    {
        // Arrange & Act
        var attribute = new ResultSetMappingAttribute(1, "userId");

        // Assert
        Assert.AreEqual(1, attribute.Index);
        Assert.AreEqual("userId", attribute.Name);
    }

    [TestMethod]
    public void Constructor_IndexZero_IsValid()
    {
        // Arrange & Act
        var attribute = new ResultSetMappingAttribute(0, "rowsAffected");

        // Assert
        Assert.AreEqual(0, attribute.Index);
        Assert.AreEqual("rowsAffected", attribute.Name);
    }

    [TestMethod]
    public void Constructor_NegativeIndex_ThrowsArgumentOutOfRangeException()
    {
        // Arrange, Act & Assert
        var ex = Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
            new ResultSetMappingAttribute(-1, "test"));
        Assert.AreEqual("index", ex.ParamName);
        Assert.IsTrue(ex.Message.Contains("must be non-negative"));
    }

    [TestMethod]
    public void Constructor_NullName_ThrowsArgumentException()
    {
        // Arrange, Act & Assert
        var ex = Assert.ThrowsException<ArgumentException>(() =>
            new ResultSetMappingAttribute(0, null!));
        Assert.AreEqual("name", ex.ParamName);
        Assert.IsTrue(ex.Message.Contains("cannot be null or whitespace"));
    }

    [TestMethod]
    public void Constructor_EmptyName_ThrowsArgumentException()
    {
        // Arrange, Act & Assert
        var ex = Assert.ThrowsException<ArgumentException>(() =>
            new ResultSetMappingAttribute(0, string.Empty));
        Assert.AreEqual("name", ex.ParamName);
        Assert.IsTrue(ex.Message.Contains("cannot be null or whitespace"));
    }

    [TestMethod]
    public void Constructor_WhitespaceName_ThrowsArgumentException()
    {
        // Arrange, Act & Assert
        var ex = Assert.ThrowsException<ArgumentException>(() =>
            new ResultSetMappingAttribute(0, "   "));
        Assert.AreEqual("name", ex.ParamName);
        Assert.IsTrue(ex.Message.Contains("cannot be null or whitespace"));
    }

    [TestMethod]
    public void AttributeUsage_AllowsMultiple()
    {
        // Arrange
        var attributeType = typeof(ResultSetMappingAttribute);

        // Act
        var usage = (AttributeUsageAttribute)Attribute.GetCustomAttribute(
            attributeType, typeof(AttributeUsageAttribute))!;

        // Assert
        Assert.IsNotNull(usage);
        Assert.IsTrue(usage.AllowMultiple);
    }

    [TestMethod]
    public void AttributeUsage_NotInherited()
    {
        // Arrange
        var attributeType = typeof(ResultSetMappingAttribute);

        // Act
        var usage = (AttributeUsageAttribute)Attribute.GetCustomAttribute(
            attributeType, typeof(AttributeUsageAttribute))!;

        // Assert
        Assert.IsNotNull(usage);
        Assert.IsFalse(usage.Inherited);
    }

    [TestMethod]
    public void AttributeUsage_TargetsMethod()
    {
        // Arrange
        var attributeType = typeof(ResultSetMappingAttribute);

        // Act
        var usage = (AttributeUsageAttribute)Attribute.GetCustomAttribute(
            attributeType, typeof(AttributeUsageAttribute))!;

        // Assert
        Assert.IsNotNull(usage);
        Assert.AreEqual(AttributeTargets.Method, usage.ValidOn);
    }
}
