// -----------------------------------------------------------------------
// <copyright file="DB2DialectProviderTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.Tests.Core;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Core;
using Sqlx.SqlGen;
using System;

[TestClass]
public class DB2DialectProviderTests
{
    private DB2DialectProvider _provider = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _provider = new DB2DialectProvider();
    }

    [TestMethod]
    public void GetDatabaseTypeName_WithString_ShouldReturnVarchar()
    {
        // Act
        var result = _provider.GetDatabaseTypeName(typeof(string));

        // Assert
        Assert.AreEqual("VARCHAR(4000)", result);
    }

    [TestMethod]
    public void GetDatabaseTypeName_WithInt_ShouldReturnInteger()
    {
        // Act
        var result = _provider.GetDatabaseTypeName(typeof(int));

        // Assert
        Assert.AreEqual("INTEGER", result);
    }

    [TestMethod]
    public void GetDatabaseTypeName_WithLong_ShouldReturnBigint()
    {
        // Act
        var result = _provider.GetDatabaseTypeName(typeof(long));

        // Assert
        Assert.AreEqual("BIGINT", result);
    }

    [TestMethod]
    public void GetDatabaseTypeName_WithDecimal_ShouldReturnDecimal()
    {
        // Act
        var result = _provider.GetDatabaseTypeName(typeof(decimal));

        // Assert
        Assert.AreEqual("DECIMAL(18,2)", result);
    }

    [TestMethod]
    public void GetDatabaseTypeName_WithDouble_ShouldReturnDouble()
    {
        // Act
        var result = _provider.GetDatabaseTypeName(typeof(double));

        // Assert
        Assert.AreEqual("DOUBLE", result);
    }

    [TestMethod]
    public void GetDatabaseTypeName_WithFloat_ShouldReturnReal()
    {
        // Act
        var result = _provider.GetDatabaseTypeName(typeof(float));

        // Assert
        Assert.AreEqual("REAL", result);
    }

    [TestMethod]
    public void GetDatabaseTypeName_WithBool_ShouldReturnBoolean()
    {
        // Act
        var result = _provider.GetDatabaseTypeName(typeof(bool));

        // Assert
        Assert.AreEqual("BOOLEAN", result);
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
    public void GetDatabaseTypeName_WithByteArray_ShouldReturnVarchar()
    {
        // Act
        var result = _provider.GetDatabaseTypeName(typeof(byte[]));

        // Assert
        Assert.AreEqual("VARCHAR(4000)", result);
    }

    [TestMethod]
    public void GetDatabaseTypeName_WithGuid_ShouldReturnChar()
    {
        // Act
        var result = _provider.GetDatabaseTypeName(typeof(Guid));

        // Assert
        Assert.AreEqual("CHAR(36)", result);
    }

    [TestMethod]
    public void GetDatabaseTypeName_WithUnsupportedType_ShouldReturnVarchar()
    {
        // Act
        var result = _provider.GetDatabaseTypeName(typeof(object));

        // Assert
        Assert.AreEqual("VARCHAR(4000)", result);
    }

    [TestMethod]
    public void GetCurrentDateTimeSyntax_ShouldReturnCurrentTimestamp()
    {
        // Act
        var result = _provider.GetCurrentDateTimeSyntax();

        // Assert
        Assert.AreEqual("CURRENT_TIMESTAMP", result);
    }

    [TestMethod]
    public void GenerateLimitClause_WithCountOnly_ShouldReturnLimit()
    {
        // Act
        var result = _provider.GenerateLimitClause(10, null);

        // Assert
        Assert.AreEqual("LIMIT 10", result);
    }

    [TestMethod]
    public void GenerateLimitClause_WithOffsetAndCount_ShouldReturnOffsetLimit()
    {
        // Act
        var result = _provider.GenerateLimitClause(10, 5);

        // Assert
        Assert.AreEqual("OFFSET 5 LIMIT 10", result);
    }

    [TestMethod]
    public void GenerateLimitClause_WithZeroCount_ShouldReturnLimit0()
    {
        // Act
        var result = _provider.GenerateLimitClause(0, null);

        // Assert
        Assert.AreEqual("LIMIT 0", result);
    }

    [TestMethod]
    public void DialectType_ShouldReturnDB2()
    {
        // Act
        var result = _provider.DialectType;

        // Assert
        Assert.AreEqual(SqlDefineTypes.DB2, result);
    }

    [TestMethod]
    public void SqlDefine_ShouldReturnDB2Define()
    {
        // Act
        var result = _provider.SqlDefine;

        // Assert
        Assert.AreEqual(SqlDefine.DB2, result);
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
        Assert.IsTrue(result.Contains("expr1") && result.Contains("expr2") && result.Contains("expr3"));
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
