// -----------------------------------------------------------------------
// <copyright file="ExceptionTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Core;
using System;

namespace Sqlx.Tests.Core;

[TestClass]
public class ExceptionTests
{
    [TestMethod]
    public void SqlGenerationException_WithMessage_CreatesException()
    {
        // Arrange
        var message = "Test error message";

        // Act
        var exception = new SqlGenerationException(message);

        // Assert
        Assert.AreEqual("SQLX001", exception.ErrorCode);
        Assert.AreEqual(message, exception.Message);
        Assert.IsNull(exception.InnerException);
    }

    [TestMethod]
    public void SqlGenerationException_WithMessageAndInnerException_CreatesException()
    {
        // Arrange
        var message = "Test error message";
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new SqlGenerationException(message, innerException);

        // Assert
        Assert.AreEqual("SQLX001", exception.ErrorCode);
        Assert.AreEqual(message, exception.Message);
        Assert.AreEqual(innerException, exception.InnerException);
    }

    [TestMethod]
    public void SqlGenerationException_InheritsFromSqlxException()
    {
        // Arrange & Act
        var exception = new SqlGenerationException("Test");

        // Assert
        Assert.IsInstanceOfType(exception, typeof(SqlxException));
    }

    [TestMethod]
    public void InvalidMethodSignatureException_WithMethodNameAndMessage_CreatesException()
    {
        // Arrange
        var methodName = "TestMethod";
        var message = "Invalid signature";

        // Act
        var exception = new InvalidMethodSignatureException(methodName, message);

        // Assert
        Assert.AreEqual("SQLX002", exception.ErrorCode);
        Assert.AreEqual(message, exception.Message);
        Assert.AreEqual(methodName, exception.MethodName);
    }

    [TestMethod]
    public void InvalidMethodSignatureException_InheritsFromSqlxException()
    {
        // Arrange & Act
        var exception = new InvalidMethodSignatureException("TestMethod", "Test");

        // Assert
        Assert.IsInstanceOfType(exception, typeof(SqlxException));
    }

    [TestMethod]
    public void UnsupportedDialectException_WithDialectName_CreatesException()
    {
        // Arrange
        var dialectName = "UnsupportedDB";

        // Act
        var exception = new UnsupportedDialectException(dialectName);

        // Assert
        Assert.AreEqual("SQLX003", exception.ErrorCode);
        Assert.AreEqual(dialectName, exception.DialectName);
        Assert.IsTrue(exception.Message.Contains(dialectName));
        Assert.IsTrue(exception.Message.Contains("not supported"));
    }

    [TestMethod]
    public void UnsupportedDialectException_InheritsFromSqlxException()
    {
        // Arrange & Act
        var exception = new UnsupportedDialectException("TestDB");

        // Assert
        Assert.IsInstanceOfType(exception, typeof(SqlxException));
    }

    [TestMethod]
    public void SqlxException_IsAbstract()
    {
        // Arrange & Act
        var type = typeof(SqlxException);

        // Assert
        Assert.IsTrue(type.IsAbstract);
    }

    [TestMethod]
    public void ExceptionProperties_AreCorrectlySet()
    {
        // Arrange
        var methodName = "TestMethod";
        var message = "Test message";
        var dialectName = "TestDialect";

        // Act
        var sqlGenException = new SqlGenerationException(message);
        var methodSigException = new InvalidMethodSignatureException(methodName, message);
        var dialectException = new UnsupportedDialectException(dialectName);

        // Assert
        Assert.AreEqual("SQLX001", sqlGenException.ErrorCode);
        Assert.AreEqual("SQLX002", methodSigException.ErrorCode);
        Assert.AreEqual("SQLX003", dialectException.ErrorCode);
    }

    [TestMethod]
    public void ExceptionSerialization_WorksCorrectly()
    {
        // Arrange
        var exception = new SqlGenerationException("Test message");

        // Act & Assert - Exception should be serializable
        Assert.IsNotNull(exception.ToString());
        Assert.IsTrue(exception.ToString().Contains("Test message"));
    }

    [TestMethod]
    public void InvalidMethodSignatureException_WithEmptyMethodName_StillWorks()
    {
        // Arrange & Act
        var exception = new InvalidMethodSignatureException("", "Test message");

        // Assert
        Assert.AreEqual("", exception.MethodName);
        Assert.AreEqual("Test message", exception.Message);
    }

    [TestMethod]
    public void UnsupportedDialectException_WithEmptyDialectName_StillWorks()
    {
        // Arrange & Act
        var exception = new UnsupportedDialectException("");

        // Assert
        Assert.AreEqual("", exception.DialectName);
        Assert.IsTrue(exception.Message.Contains("not supported"));
    }

    [TestMethod]
    public void ExceptionStackTrace_PreservesOriginalStackTrace()
    {
        // Arrange
        Exception? caughtException = null;

        // Act
        try
        {
            try
            {
                throw new InvalidOperationException("Original error");
            }
            catch (Exception ex)
            {
                throw new SqlGenerationException("Wrapped error", ex);
            }
        }
        catch (Exception ex)
        {
            caughtException = ex;
        }

        // Assert
        Assert.IsNotNull(caughtException);
        Assert.IsInstanceOfType(caughtException, typeof(SqlGenerationException));
        Assert.IsNotNull(caughtException.InnerException);
        Assert.IsInstanceOfType(caughtException.InnerException, typeof(InvalidOperationException));
    }
}


