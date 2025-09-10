// -----------------------------------------------------------------------
// <copyright file="OracleDialectProviderTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.Tests.Core;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Core;
using Sqlx.SqlGen;
using System;

[TestClass]
public class OracleDialectProviderTests
{
    private OracleDialectProvider _provider = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _provider = new OracleDialectProvider();
    }

    [TestMethod]
    public void GetDatabaseTypeName_WithString_ShouldReturnNVarchar2()
    {
        // Act
        var result = _provider.GetDatabaseTypeName(typeof(string));

        // Assert
        Assert.AreEqual("NVARCHAR2(4000)", result);
    }

    [TestMethod]
    public void GetDatabaseTypeName_WithInt_ShouldReturnNumber()
    {
        // Act
        var result = _provider.GetDatabaseTypeName(typeof(int));

        // Assert
        Assert.AreEqual("NUMBER(10)", result);
    }

    [TestMethod]
    public void GetDatabaseTypeName_WithLong_ShouldReturnNumber()
    {
        // Act
        var result = _provider.GetDatabaseTypeName(typeof(long));

        // Assert
        Assert.AreEqual("NUMBER(19)", result);
    }

    [TestMethod]
    public void GetDatabaseTypeName_WithDecimal_ShouldReturnNumber()
    {
        // Act
        var result = _provider.GetDatabaseTypeName(typeof(decimal));

        // Assert
        Assert.AreEqual("NUMBER(18,2)", result);
    }

    [TestMethod]
    public void GetDatabaseTypeName_WithDouble_ShouldReturnBinaryDouble()
    {
        // Act
        var result = _provider.GetDatabaseTypeName(typeof(double));

        // Assert
        Assert.AreEqual("BINARY_DOUBLE", result);
    }

    [TestMethod]
    public void GetDatabaseTypeName_WithFloat_ShouldReturnBinaryFloat()
    {
        // Act
        var result = _provider.GetDatabaseTypeName(typeof(float));

        // Assert
        Assert.AreEqual("BINARY_FLOAT", result);
    }

    [TestMethod]
    public void GetDatabaseTypeName_WithBool_ShouldReturnNumber1()
    {
        // Act
        var result = _provider.GetDatabaseTypeName(typeof(bool));

        // Assert
        Assert.AreEqual("NUMBER(1)", result);
    }

    [TestMethod]
    public void GetDatabaseTypeName_WithDateTime_ShouldReturnTimestamp()
    {
        // Act
        var result = _provider.GetDatabaseTypeName(typeof(DateTime));

        // Assert
        Assert.AreEqual("TIMESTAMP", result);
    }

    [TestMethod]
    public void GetDatabaseTypeName_WithByteArray_ShouldReturnNVarchar2()
    {
        // Act
        var result = _provider.GetDatabaseTypeName(typeof(byte[]));

        // Assert
        Assert.AreEqual("NVARCHAR2(4000)", result);
    }

    [TestMethod]
    public void GetDatabaseTypeName_WithGuid_ShouldReturnRaw16()
    {
        // Act
        var result = _provider.GetDatabaseTypeName(typeof(Guid));

        // Assert
        Assert.AreEqual("RAW(16)", result);
    }

    [TestMethod]
    public void GetDatabaseTypeName_WithUnsupportedType_ShouldReturnNVarchar2()
    {
        // Act
        var result = _provider.GetDatabaseTypeName(typeof(object));

        // Assert
        Assert.AreEqual("NVARCHAR2(4000)", result);
    }

    [TestMethod]
    public void GetCurrentDateTimeSyntax_ShouldReturnSysTimestamp()
    {
        // Act
        var result = _provider.GetCurrentDateTimeSyntax();

        // Assert
        Assert.AreEqual("SYSTIMESTAMP", result);
    }

    [TestMethod]
    public void GenerateLimitClause_WithCountOnly_ShouldReturnFetch()
    {
        // Act
        var result = _provider.GenerateLimitClause(10, null);

        // Assert
        Assert.AreEqual("FETCH FIRST 10 ROWS ONLY", result);
    }

    [TestMethod]
    public void GenerateLimitClause_WithOffsetAndCount_ShouldReturnOffsetFetch()
    {
        // Act
        var result = _provider.GenerateLimitClause(10, 5);

        // Assert
        Assert.AreEqual("OFFSET 5 ROWS FETCH NEXT 10 ROWS ONLY", result);
    }

    [TestMethod]
    public void GenerateLimitClause_WithZeroCount_ShouldReturnFetch()
    {
        // Act
        var result = _provider.GenerateLimitClause(0, null);

        // Assert
        Assert.AreEqual("FETCH FIRST 0 ROWS ONLY", result);
    }

    [TestMethod]
    public void DialectType_ShouldReturnOracle()
    {
        // Act
        var result = _provider.DialectType;

        // Assert
        Assert.AreEqual(SqlDefineTypes.Oracle, result);
    }

    [TestMethod]
    public void SqlDefine_ShouldReturnOracleDefine()
    {
        // Act
        var result = _provider.SqlDefine;

        // Assert
        Assert.AreEqual(SqlDefine.Oracle, result);
    }

    [TestMethod]
    public void FormatDateTime_WithDateTime_ShouldReturnFormattedString()
    {
        // Arrange
        var dateTime = new DateTime(2023, 12, 25, 14, 30, 0);

        // Act
        var result = _provider.FormatDateTime(dateTime);

        // Assert
        Assert.IsTrue(result.Contains("2023"));
        Assert.IsTrue(result.Contains("12"));
        Assert.IsTrue(result.Contains("25"));
    }

    [TestMethod]
    public void GetConcatenationSyntax_WithMultipleExpressions_ShouldReturnConcatenated()
    {
        // Act
        var result = _provider.GetConcatenationSyntax("expr1", "expr2", "expr3");

        // Assert
        Assert.AreEqual("expr1 || expr2 || expr3", result);
    }

    [TestMethod]
    public void GetConcatenationSyntax_WithSingleExpression_ShouldReturnSame()
    {
        // Act
        var result = _provider.GetConcatenationSyntax("expr1");

        // Assert
        Assert.AreEqual("expr1", result);
    }

    [TestMethod]
    public void GetConcatenationSyntax_WithNoExpressions_ShouldReturnEmpty()
    {
        // Act
        var result = _provider.GetConcatenationSyntax();

        // Assert
        Assert.AreEqual(string.Empty, result);
    }
}
