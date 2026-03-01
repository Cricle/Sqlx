// <copyright file="TypeConversionTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using System;
using System.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sqlx.Tests;

[TestClass]
public class TypeConversionTests
{
    [TestMethod]
    public void Convert_Int64ToInt32_Success()
    {
        long value = 42L;
        var result = TypeConverter.Convert<int>(value);
        Assert.AreEqual(42, result);
    }

    [TestMethod]
    public void Convert_Int32ToInt64_Success()
    {
        int value = 42;
        var result = TypeConverter.Convert<long>(value);
        Assert.AreEqual(42L, result);
    }

    [TestMethod]
    public void Convert_DoubleToInt32_Success()
    {
        double value = 42.7;
        var result = TypeConverter.Convert<int>(value);
        Assert.AreEqual(43, result); // Convert.ChangeType rounds to nearest even
    }

    [TestMethod]
    public void Convert_StringToInt32_Success()
    {
        string value = "42";
        var result = TypeConverter.Convert<int>(value);
        Assert.AreEqual(42, result);
    }

    [TestMethod]
    public void Convert_StringToGuid_Success()
    {
        var guid = Guid.NewGuid();
        string value = guid.ToString();
        var result = TypeConverter.Convert<Guid>(value);
        Assert.AreEqual(guid, result);
    }

    [TestMethod]
    public void Convert_ByteArrayToGuid_Success()
    {
        var guid = Guid.NewGuid();
        byte[] value = guid.ToByteArray();
        var result = TypeConverter.Convert<Guid>(value);
        Assert.AreEqual(guid, result);
    }

    [TestMethod]
    public void Convert_Int32ToBoolean_Success()
    {
        int value1 = 1;
        int value0 = 0;
        var result1 = TypeConverter.Convert<bool>(value1);
        var result0 = TypeConverter.Convert<bool>(value0);
        Assert.IsTrue(result1);
        Assert.IsFalse(result0);
    }

    [TestMethod]
    public void Convert_StringToEnum_Success()
    {
        string value = "Default";
        var result = TypeConverter.Convert<CommandBehavior>(value);
        Assert.AreEqual(CommandBehavior.Default, result);
    }

    [TestMethod]
    public void Convert_Int32ToEnum_Success()
    {
        int value = 2; // SingleResult = 2
        var result = TypeConverter.Convert<CommandBehavior>(value);
        Assert.AreEqual(2, (int)result);
    }

    [TestMethod]
    public void Convert_DBNullToInt32_ReturnsDefault()
    {
        var result = TypeConverter.Convert<int>(DBNull.Value);
        Assert.AreEqual(0, result);
    }

    [TestMethod]
    public void Convert_DBNullToNullableInt32_ReturnsNull()
    {
        var result = TypeConverter.Convert<int?>(DBNull.Value);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Convert_SameType_ReturnsValue()
    {
        int value = 42;
        var result = TypeConverter.Convert<int>(value);
        Assert.AreEqual(42, result);
    }

    [TestMethod]
    public void Convert_DecimalToDouble_Success()
    {
        decimal value = 42.5m;
        var result = TypeConverter.Convert<double>(value);
        Assert.AreEqual(42.5, result);
    }

    [TestMethod]
    public void Convert_FloatToDecimal_Success()
    {
        float value = 42.5f;
        var result = TypeConverter.Convert<decimal>(value);
        Assert.AreEqual(42.5m, result);
    }
}
