// <copyright file="TypeConverterAdvancedTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using System;
using System.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sqlx.Tests;

public enum Priority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

[Flags]
public enum Permissions
{
    None = 0,
    Read = 1,
    Write = 2,
    Execute = 4,
    All = Read | Write | Execute
}

[TestClass]
public class TypeConverterAdvancedTests
{
    [TestMethod]
    public void Convert_SameType_ReturnsValueDirectly()
    {
        int value = 42;
        var result = TypeConverter.Convert<int>(value);
        Assert.AreEqual(42, result);
    }

    [TestMethod]
    public void Convert_NullToReferenceType_ReturnsDefault()
    {
        var result = TypeConverter.Convert<string?>(null);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Convert_DBNullToReferenceType_ReturnsDefault()
    {
        var result = TypeConverter.Convert<string>(DBNull.Value);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Convert_DBNullToValueType_ReturnsDefault()
    {
        var result = TypeConverter.Convert<int>(DBNull.Value);
        Assert.AreEqual(0, result);
    }

    [TestMethod]
    public void Convert_DBNullToNullableValueType_ReturnsNull()
    {
        var result = TypeConverter.Convert<int?>(DBNull.Value);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Convert_Int16ToInt32_Success()
    {
        short value = 100;
        var result = TypeConverter.Convert<int>(value);
        Assert.AreEqual(100, result);
    }

    [TestMethod]
    public void Convert_Int32ToInt16_Success()
    {
        int value = 100;
        var result = TypeConverter.Convert<short>(value);
        Assert.AreEqual((short)100, result);
    }

    [TestMethod]
    public void Convert_ByteToInt32_Success()
    {
        byte value = 255;
        var result = TypeConverter.Convert<int>(value);
        Assert.AreEqual(255, result);
    }

    [TestMethod]
    public void Convert_Int32ToByte_Success()
    {
        int value = 200;
        var result = TypeConverter.Convert<byte>(value);
        Assert.AreEqual((byte)200, result);
    }

    [TestMethod]
    public void Convert_FloatToDouble_Success()
    {
        float value = 3.14f;
        var result = TypeConverter.Convert<double>(value);
        Assert.AreEqual(3.14, result, 0.001);
    }

    [TestMethod]
    public void Convert_DoubleToFloat_Success()
    {
        double value = 3.14;
        var result = TypeConverter.Convert<float>(value);
        Assert.AreEqual(3.14f, result, 0.001f);
    }

    [TestMethod]
    public void Convert_DecimalToDouble_Success()
    {
        decimal value = 123.45m;
        var result = TypeConverter.Convert<double>(value);
        Assert.AreEqual(123.45, result, 0.001);
    }

    [TestMethod]
    public void Convert_DoubleToDecimal_Success()
    {
        double value = 123.45;
        var result = TypeConverter.Convert<decimal>(value);
        Assert.AreEqual(123.45m, result);
    }

    [TestMethod]
    public void Convert_StringToBoolean_Success()
    {
        var resultTrue = TypeConverter.Convert<bool>("True");
        var resultFalse = TypeConverter.Convert<bool>("False");
        
        Assert.IsTrue(resultTrue);
        Assert.IsFalse(resultFalse);
    }

    [TestMethod]
    public void Convert_Int32ToBoolean_Success()
    {
        var resultTrue = TypeConverter.Convert<bool>(1);
        var resultFalse = TypeConverter.Convert<bool>(0);
        
        Assert.IsTrue(resultTrue);
        Assert.IsFalse(resultFalse);
    }

    [TestMethod]
    public void Convert_BooleanToInt32_Success()
    {
        var resultTrue = TypeConverter.Convert<int>(true);
        var resultFalse = TypeConverter.Convert<int>(false);
        
        Assert.AreEqual(1, resultTrue);
        Assert.AreEqual(0, resultFalse);
    }

    [TestMethod]
    public void Convert_StringToDateTime_Success()
    {
        var dateStr = "2024-01-15 10:30:00";
        var result = TypeConverter.Convert<DateTime>(dateStr);
        
        Assert.AreEqual(2024, result.Year);
        Assert.AreEqual(1, result.Month);
        Assert.AreEqual(15, result.Day);
    }

    [TestMethod]
    public void Convert_StringToDecimal_Success()
    {
        var result = TypeConverter.Convert<decimal>("123.45");
        Assert.AreEqual(123.45m, result);
    }

    [TestMethod]
    public void Convert_StringToDouble_Success()
    {
        var result = TypeConverter.Convert<double>("123.45");
        Assert.AreEqual(123.45, result, 0.001);
    }

    [TestMethod]
    public void Convert_EnumToInt32_Success()
    {
        var result = TypeConverter.Convert<int>(Priority.High);
        Assert.AreEqual(2, result);
    }

    [TestMethod]
    public void Convert_Int32ToEnum_Success()
    {
        var result = TypeConverter.Convert<Priority>(2);
        Assert.AreEqual(Priority.High, result);
    }

    [TestMethod]
    public void Convert_Int64ToEnum_Success()
    {
        long value = 3L;
        var result = TypeConverter.Convert<Priority>(value);
        Assert.AreEqual(Priority.Critical, result);
    }

    [TestMethod]
    public void Convert_StringToEnum_Success()
    {
        var result = TypeConverter.Convert<Priority>("Medium");
        Assert.AreEqual(Priority.Medium, result);
    }

    [TestMethod]
    public void Convert_StringToEnumIgnoreCase_Success()
    {
        var result = TypeConverter.Convert<Priority>("high");
        Assert.AreEqual(Priority.High, result);
    }

    [TestMethod]
    public void Convert_Int32ToFlagsEnum_Success()
    {
        var result = TypeConverter.Convert<Permissions>(3);
        Assert.AreEqual(Permissions.Read | Permissions.Write, result);
    }

    [TestMethod]
    public void Convert_StringToGuid_Success()
    {
        var guid = Guid.NewGuid();
        var result = TypeConverter.Convert<Guid>(guid.ToString());
        Assert.AreEqual(guid, result);
    }

    [TestMethod]
    public void Convert_ByteArrayToGuid_Success()
    {
        var guid = Guid.NewGuid();
        var bytes = guid.ToByteArray();
        var result = TypeConverter.Convert<Guid>(bytes);
        Assert.AreEqual(guid, result);
    }

    [TestMethod]
    public void Convert_GuidToString_Success()
    {
        var guid = Guid.NewGuid();
        var result = TypeConverter.Convert<string>(guid.ToString());
        Assert.AreEqual(guid.ToString(), result);
    }

    [TestMethod]
    public void Convert_StringToByteArray_Success()
    {
        var base64 = Convert.ToBase64String(new byte[] { 1, 2, 3, 4, 5 });
        var result = TypeConverter.Convert<byte[]>(base64);
        
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4, 5 }, result);
    }

    [TestMethod]
    public void Convert_NullableInt32ToInt32_Success()
    {
        int? value = 42;
        var result = TypeConverter.Convert<int>(value);
        Assert.AreEqual(42, result);
    }

    [TestMethod]
    public void Convert_Int32ToNullableInt32_Success()
    {
        int value = 42;
        var result = TypeConverter.Convert<int?>(value);
        Assert.AreEqual(42, result!.Value);
    }

    [TestMethod]
    public void Convert_NullableEnumToInt32_Success()
    {
        Priority? value = Priority.High;
        var result = TypeConverter.Convert<int>(value);
        Assert.AreEqual(2, result);
    }

    [TestMethod]
    public void Convert_Int32ToNullableEnum_Success()
    {
        int value = 2;
        var result = TypeConverter.Convert<Priority?>(value);
        Assert.AreEqual(Priority.High, result!.Value);
    }

    [TestMethod]
    public void Convert_StringToNullableGuid_Success()
    {
        var guid = Guid.NewGuid();
        var result = TypeConverter.Convert<Guid?>(guid.ToString());
        Assert.AreEqual(guid, result!.Value);
    }

    [TestMethod]
    public void Convert_NullToNullableGuid_ReturnsNull()
    {
        var result = TypeConverter.Convert<Guid?>(null!);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Convert_ZeroToEnum_Success()
    {
        var result = TypeConverter.Convert<Priority>(0);
        Assert.AreEqual(Priority.Low, result);
    }

    [TestMethod]
    public void Convert_NegativeToEnum_Success()
    {
        var result = TypeConverter.Convert<Priority>(-1);
        Assert.AreEqual((Priority)(-1), result);
    }
}
