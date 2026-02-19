// <copyright file="OutputParameterTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using System;

namespace Sqlx.Tests;

/// <summary>
/// Unit tests for OutputParameter{T} wrapper class.
/// </summary>
[TestClass]
public class OutputParameterTests
{
    #region Constructor Tests

    [TestMethod]
    public void DefaultConstructor_CreatesInstance_WithNoValue()
    {
        // Arrange & Act
        var param = new OutputParameter<int>();

        // Assert
        Assert.IsNotNull(param);
        Assert.IsFalse(param.HasValue);
        Assert.AreEqual(default(int), param.Value);
    }

    [TestMethod]
    public void DefaultConstructor_WithReferenceType_CreatesInstance_WithNullValue()
    {
        // Arrange & Act
        var param = new OutputParameter<string>();

        // Assert
        Assert.IsNotNull(param);
        Assert.IsFalse(param.HasValue);
        Assert.IsNull(param.Value);
    }

    [TestMethod]
    public void ConstructorWithValue_CreatesInstance_WithValue()
    {
        // Arrange & Act
        var param = new OutputParameter<int>(42);

        // Assert
        Assert.IsNotNull(param);
        Assert.IsTrue(param.HasValue);
        Assert.AreEqual(42, param.Value);
    }

    [TestMethod]
    public void ConstructorWithValue_WithReferenceType_CreatesInstance_WithValue()
    {
        // Arrange & Act
        var param = new OutputParameter<string>("test");

        // Assert
        Assert.IsNotNull(param);
        Assert.IsTrue(param.HasValue);
        Assert.AreEqual("test", param.Value);
    }

    [TestMethod]
    public void ConstructorWithValue_WithNull_CreatesInstance_WithNullValue()
    {
        // Arrange & Act
        var param = new OutputParameter<string?>(null);

        // Assert
        Assert.IsNotNull(param);
        Assert.IsTrue(param.HasValue); // HasValue is true even if value is null
        Assert.IsNull(param.Value);
    }

    [TestMethod]
    public void ConstructorWithValue_WithZero_CreatesInstance_WithZeroValue()
    {
        // Arrange & Act
        var param = new OutputParameter<int>(0);

        // Assert
        Assert.IsNotNull(param);
        Assert.IsTrue(param.HasValue);
        Assert.AreEqual(0, param.Value);
    }

    [TestMethod]
    public void ConstructorWithValue_WithNegativeValue_CreatesInstance_WithNegativeValue()
    {
        // Arrange & Act
        var param = new OutputParameter<int>(-100);

        // Assert
        Assert.IsNotNull(param);
        Assert.IsTrue(param.HasValue);
        Assert.AreEqual(-100, param.Value);
    }

    [TestMethod]
    public void ConstructorWithValue_WithMaxValue_CreatesInstance_WithMaxValue()
    {
        // Arrange & Act
        var param = new OutputParameter<int>(int.MaxValue);

        // Assert
        Assert.IsNotNull(param);
        Assert.IsTrue(param.HasValue);
        Assert.AreEqual(int.MaxValue, param.Value);
    }

    [TestMethod]
    public void ConstructorWithValue_WithMinValue_CreatesInstance_WithMinValue()
    {
        // Arrange & Act
        var param = new OutputParameter<int>(int.MinValue);

        // Assert
        Assert.IsNotNull(param);
        Assert.IsTrue(param.HasValue);
        Assert.AreEqual(int.MinValue, param.Value);
    }

    #endregion

    #region WithValue Factory Method Tests

    [TestMethod]
    public void WithValue_CreatesInstance_WithValue()
    {
        // Arrange & Act
        var param = OutputParameter<int>.WithValue(42);

        // Assert
        Assert.IsNotNull(param);
        Assert.IsTrue(param.HasValue);
        Assert.AreEqual(42, param.Value);
    }

    [TestMethod]
    public void WithValue_WithReferenceType_CreatesInstance_WithValue()
    {
        // Arrange & Act
        var param = OutputParameter<string>.WithValue("test");

        // Assert
        Assert.IsNotNull(param);
        Assert.IsTrue(param.HasValue);
        Assert.AreEqual("test", param.Value);
    }

    [TestMethod]
    public void WithValue_WithNull_CreatesInstance_WithNullValue()
    {
        // Arrange & Act
        var param = OutputParameter<string?>.WithValue(null);

        // Assert
        Assert.IsNotNull(param);
        Assert.IsTrue(param.HasValue);
        Assert.IsNull(param.Value);
    }

    [TestMethod]
    public void WithValue_WithDecimal_CreatesInstance_WithDecimalValue()
    {
        // Arrange & Act
        var param = OutputParameter<decimal>.WithValue(123.45m);

        // Assert
        Assert.IsNotNull(param);
        Assert.IsTrue(param.HasValue);
        Assert.AreEqual(123.45m, param.Value);
    }

    [TestMethod]
    public void WithValue_WithDateTime_CreatesInstance_WithDateTimeValue()
    {
        // Arrange
        var now = DateTime.UtcNow;

        // Act
        var param = OutputParameter<DateTime>.WithValue(now);

        // Assert
        Assert.IsNotNull(param);
        Assert.IsTrue(param.HasValue);
        Assert.AreEqual(now, param.Value);
    }

    [TestMethod]
    public void WithValue_WithGuid_CreatesInstance_WithGuidValue()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var param = OutputParameter<Guid>.WithValue(guid);

        // Assert
        Assert.IsNotNull(param);
        Assert.IsTrue(param.HasValue);
        Assert.AreEqual(guid, param.Value);
    }

    #endregion

    #region Value Property Tests

    [TestMethod]
    public void Value_CanBeSet_AfterConstruction()
    {
        // Arrange
        var param = new OutputParameter<int>();

        // Act
        param.Value = 42;

        // Assert
        Assert.AreEqual(42, param.Value);
    }

    [TestMethod]
    public void Value_CanBeChanged_MultipleTimes()
    {
        // Arrange
        var param = new OutputParameter<int>(10);

        // Act
        param.Value = 20;
        param.Value = 30;
        param.Value = 40;

        // Assert
        Assert.AreEqual(40, param.Value);
    }

    [TestMethod]
    public void Value_CanBeSetToNull_ForReferenceType()
    {
        // Arrange
        var param = new OutputParameter<string>("initial");

        // Act
        param.Value = null;

        // Assert
        Assert.IsNull(param.Value);
    }

    [TestMethod]
    public void Value_CanBeSetToNull_ForNullableValueType()
    {
        // Arrange
        var param = new OutputParameter<int?>(42);

        // Act
        param.Value = null;

        // Assert
        Assert.IsNull(param.Value);
    }

    #endregion

    #region HasValue Property Tests

    [TestMethod]
    public void HasValue_IsFalse_AfterDefaultConstruction()
    {
        // Arrange & Act
        var param = new OutputParameter<int>();

        // Assert
        Assert.IsFalse(param.HasValue);
    }

    [TestMethod]
    public void HasValue_IsTrue_AfterConstructionWithValue()
    {
        // Arrange & Act
        var param = new OutputParameter<int>(42);

        // Assert
        Assert.IsTrue(param.HasValue);
    }

    [TestMethod]
    public void HasValue_IsTrue_AfterWithValue()
    {
        // Arrange & Act
        var param = OutputParameter<int>.WithValue(42);

        // Assert
        Assert.IsTrue(param.HasValue);
    }

    [TestMethod]
    public void HasValue_IsTrue_EvenWhenValueIsNull()
    {
        // Arrange & Act
        var param = new OutputParameter<string?>(null);

        // Assert
        Assert.IsTrue(param.HasValue);
    }

    [TestMethod]
    public void HasValue_IsTrue_EvenWhenValueIsZero()
    {
        // Arrange & Act
        var param = new OutputParameter<int>(0);

        // Assert
        Assert.IsTrue(param.HasValue);
    }

    [TestMethod]
    public void HasValue_RemainsTrue_AfterValueChange()
    {
        // Arrange
        var param = new OutputParameter<int>(10);

        // Act
        param.Value = 20;

        // Assert
        Assert.IsTrue(param.HasValue);
    }

    #endregion

    #region Implicit Conversion Tests

    [TestMethod]
    public void ImplicitConversion_ToInt_ReturnsValue()
    {
        // Arrange
        var param = new OutputParameter<int>(42);

        // Act
        int value = param;

        // Assert
        Assert.AreEqual(42, value);
    }

    [TestMethod]
    public void ImplicitConversion_ToString_ReturnsValue()
    {
        // Arrange
        var param = new OutputParameter<string>("test");

        // Act
        string? value = param;

        // Assert
        Assert.AreEqual("test", value);
    }

    [TestMethod]
    public void ImplicitConversion_WithNullValue_ReturnsNull()
    {
        // Arrange
        var param = new OutputParameter<string?>(null);

        // Act
        string? value = param;

        // Assert
        Assert.IsNull(value);
    }

    [TestMethod]
    public void ImplicitConversion_WithDefaultConstructor_ReturnsDefault()
    {
        // Arrange
        var param = new OutputParameter<int>();

        // Act
        int value = param;

        // Assert
        Assert.AreEqual(default(int), value);
    }

    [TestMethod]
    public void ImplicitConversion_ToDecimal_ReturnsValue()
    {
        // Arrange
        var param = new OutputParameter<decimal>(123.45m);

        // Act
        decimal value = param;

        // Assert
        Assert.AreEqual(123.45m, value);
    }

    [TestMethod]
    public void ImplicitConversion_ToDateTime_ReturnsValue()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var param = new OutputParameter<DateTime>(now);

        // Act
        DateTime value = param;

        // Assert
        Assert.AreEqual(now, value);
    }

    [TestMethod]
    public void ImplicitConversion_ToGuid_ReturnsValue()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var param = new OutputParameter<Guid>(guid);

        // Act
        Guid value = param;

        // Assert
        Assert.AreEqual(guid, value);
    }

    [TestMethod]
    public void ImplicitConversion_ToBool_ReturnsValue()
    {
        // Arrange
        var param = new OutputParameter<bool>(true);

        // Act
        bool value = param;

        // Assert
        Assert.IsTrue(value);
    }

    [TestMethod]
    public void ImplicitConversion_ToLong_ReturnsValue()
    {
        // Arrange
        var param = new OutputParameter<long>(9876543210L);

        // Act
        long value = param;

        // Assert
        Assert.AreEqual(9876543210L, value);
    }

    [TestMethod]
    public void ImplicitConversion_ToDouble_ReturnsValue()
    {
        // Arrange
        var param = new OutputParameter<double>(3.14159);

        // Act
        double value = param;

        // Assert
        Assert.AreEqual(3.14159, value);
    }

    #endregion

    #region Type Safety Tests

    [TestMethod]
    public void Class_IsSealed()
    {
        // Arrange
        var type = typeof(OutputParameter<int>);

        // Act & Assert
        Assert.IsTrue(type.IsSealed);
    }

    [TestMethod]
    public void Class_IsGeneric()
    {
        // Arrange
        var type = typeof(OutputParameter<>);

        // Act & Assert
        Assert.IsTrue(type.IsGenericTypeDefinition);
    }

    [TestMethod]
    public void DifferentInstances_AreIndependent()
    {
        // Arrange
        var param1 = new OutputParameter<int>(10);
        var param2 = new OutputParameter<int>(20);

        // Act
        param1.Value = 100;

        // Assert
        Assert.AreEqual(100, param1.Value);
        Assert.AreEqual(20, param2.Value);
    }

    [TestMethod]
    public void DifferentTypes_AreIndependent()
    {
        // Arrange
        var intParam = new OutputParameter<int>(42);
        var stringParam = new OutputParameter<string>("test");

        // Act
        intParam.Value = 100;
        stringParam.Value = "modified";

        // Assert
        Assert.AreEqual(100, intParam.Value);
        Assert.AreEqual("modified", stringParam.Value);
    }

    #endregion

    #region Complex Type Tests

    [TestMethod]
    public void OutputParameter_WithCustomClass_WorksCorrectly()
    {
        // Arrange
        var obj = new TestClass { Id = 1, Name = "Test" };
        var param = new OutputParameter<TestClass>(obj);

        // Act & Assert
        Assert.IsTrue(param.HasValue);
        Assert.AreEqual(obj, param.Value);
        Assert.AreEqual(1, param.Value?.Id);
        Assert.AreEqual("Test", param.Value?.Name);
    }

    [TestMethod]
    public void OutputParameter_WithStruct_WorksCorrectly()
    {
        // Arrange
        var point = new TestStruct { X = 10, Y = 20 };
        var param = new OutputParameter<TestStruct>(point);

        // Act & Assert
        Assert.IsTrue(param.HasValue);
        Assert.AreEqual(point, param.Value);
        Assert.AreEqual(10, param.Value.X);
        Assert.AreEqual(20, param.Value.Y);
    }

    [TestMethod]
    public void OutputParameter_WithNullableStruct_WorksCorrectly()
    {
        // Arrange
        int? nullableValue = 42;
        var param = new OutputParameter<int?>(nullableValue);

        // Act & Assert
        Assert.IsTrue(param.HasValue);
        Assert.AreEqual(42, param.Value);
    }

    [TestMethod]
    public void OutputParameter_WithNullableStruct_WithNull_WorksCorrectly()
    {
        // Arrange
        int? nullableValue = null;
        var param = new OutputParameter<int?>(nullableValue);

        // Act & Assert
        Assert.IsTrue(param.HasValue);
        Assert.IsNull(param.Value);
    }

    [TestMethod]
    public void OutputParameter_WithByteArray_WorksCorrectly()
    {
        // Arrange
        var bytes = new byte[] { 1, 2, 3, 4, 5 };
        var param = new OutputParameter<byte[]>(bytes);

        // Act & Assert
        Assert.IsTrue(param.HasValue);
        CollectionAssert.AreEqual(bytes, param.Value);
    }

    #endregion

    #region Edge Case Tests

    [TestMethod]
    public void OutputParameter_WithEmptyString_WorksCorrectly()
    {
        // Arrange & Act
        var param = new OutputParameter<string>(string.Empty);

        // Assert
        Assert.IsTrue(param.HasValue);
        Assert.AreEqual(string.Empty, param.Value);
    }

    [TestMethod]
    public void OutputParameter_WithWhitespaceString_WorksCorrectly()
    {
        // Arrange & Act
        var param = new OutputParameter<string>("   ");

        // Assert
        Assert.IsTrue(param.HasValue);
        Assert.AreEqual("   ", param.Value);
    }

    [TestMethod]
    public void OutputParameter_WithLargeString_WorksCorrectly()
    {
        // Arrange
        var largeString = new string('x', 10000);
        var param = new OutputParameter<string>(largeString);

        // Act & Assert
        Assert.IsTrue(param.HasValue);
        Assert.AreEqual(largeString, param.Value);
        Assert.AreEqual(10000, param.Value?.Length);
    }

    [TestMethod]
    public void OutputParameter_WithDefaultDateTime_WorksCorrectly()
    {
        // Arrange & Act
        var param = new OutputParameter<DateTime>(default);

        // Assert
        Assert.IsTrue(param.HasValue);
        Assert.AreEqual(default(DateTime), param.Value);
    }

    [TestMethod]
    public void OutputParameter_WithEmptyGuid_WorksCorrectly()
    {
        // Arrange & Act
        var param = new OutputParameter<Guid>(Guid.Empty);

        // Assert
        Assert.IsTrue(param.HasValue);
        Assert.AreEqual(Guid.Empty, param.Value);
    }

    #endregion

    #region Helper Classes

    private class TestClass
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    private struct TestStruct
    {
        public int X { get; set; }
        public int Y { get; set; }
    }

    #endregion
}
