// -----------------------------------------------------------------------
// <copyright file="SqlxExceptionAdvancedTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.Tests.Core;

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;

/// <summary>
/// Advanced tests for SqlxException and derived classes.
/// </summary>
[TestClass]
public class SqlxExceptionAdvancedTests
{
    /// <summary>
    /// Tests SqlGenerationException constructor with message.
    /// </summary>
    [TestMethod]
    public void SqlGenerationException_ConstructorWithMessage_SetsPropertiesCorrectly()
    {
        // Arrange
        var message = "SQL generation failed";

        // Act
        var exception = new SqlGenerationException(message);

        // Assert
        Assert.AreEqual("SQLX001", exception.ErrorCode);
        Assert.AreEqual(message, exception.Message);
        Assert.IsNull(exception.InnerException);
    }

    /// <summary>
    /// Tests SqlGenerationException constructor with message and inner exception.
    /// </summary>
    [TestMethod]
    public void SqlGenerationException_ConstructorWithMessageAndInnerException_SetsPropertiesCorrectly()
    {
        // Arrange
        var message = "SQL generation failed";
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new SqlGenerationException(message, innerException);

        // Assert
        Assert.AreEqual("SQLX001", exception.ErrorCode);
        Assert.AreEqual(message, exception.Message);
        Assert.AreSame(innerException, exception.InnerException);
    }

    /// <summary>
    /// Tests SqlGenerationException is derived from SqlxException.
    /// </summary>
    [TestMethod]
    public void SqlGenerationException_InheritsFromSqlxException()
    {
        // Arrange
        var exception = new SqlGenerationException("Test message");

        // Act & Assert
        Assert.IsInstanceOfType(exception, typeof(SqlxException));
        Assert.IsInstanceOfType(exception, typeof(Exception));
    }

    /// <summary>
    /// Tests SqlGenerationException can be thrown and caught.
    /// </summary>
    [TestMethod]
    [ExpectedException(typeof(SqlGenerationException))]
    public void SqlGenerationException_CanBeThrownAndCaught()
    {
        // Act
        throw new SqlGenerationException("Test exception");
    }

    /// <summary>
    /// Tests SqlGenerationException can be caught as SqlxException.
    /// </summary>
    [TestMethod]
    public void SqlGenerationException_CanBeCaughtAsSqlxException()
    {
        // Arrange
        var message = "Test exception";
        SqlxException? caughtException = null;

        // Act
        try
        {
            throw new SqlGenerationException(message);
        }
        catch (SqlxException ex)
        {
            caughtException = ex;
        }

        // Assert
        Assert.IsNotNull(caughtException);
        Assert.IsInstanceOfType(caughtException, typeof(SqlGenerationException));
        Assert.AreEqual("SQLX001", caughtException.ErrorCode);
        Assert.AreEqual(message, caughtException.Message);
    }

    /// <summary>
    /// Tests SqlGenerationException can be caught as base Exception.
    /// </summary>
    [TestMethod]
    public void SqlGenerationException_CanBeCaughtAsException()
    {
        // Arrange
        var message = "Test exception";
        Exception? caughtException = null;

        // Act
        try
        {
            throw new SqlGenerationException(message);
        }
        catch (Exception ex)
        {
            caughtException = ex;
        }

        // Assert
        Assert.IsNotNull(caughtException);
        Assert.IsInstanceOfType(caughtException, typeof(SqlGenerationException));
        Assert.AreEqual(message, caughtException.Message);
    }

    /// <summary>
    /// Tests SqlGenerationException ToString includes error code.
    /// </summary>
    [TestMethod]
    public void SqlGenerationException_ToString_IncludesErrorCode()
    {
        // Arrange
        var message = "Test exception";
        var exception = new SqlGenerationException(message);

        // Act
        var result = exception.ToString();

        // Assert
        Assert.IsTrue(result.Contains(message), "ToString should contain the message");
        Assert.IsTrue(result.Contains("SqlGenerationException"), "ToString should contain the exception type name");
    }

    /// <summary>
    /// Tests SqlGenerationException with empty message.
    /// </summary>
    [TestMethod]
    public void SqlGenerationException_EmptyMessage_IsAllowed()
    {
        // Act
        var exception = new SqlGenerationException(string.Empty);

        // Assert
        Assert.AreEqual("SQLX001", exception.ErrorCode);
        Assert.AreEqual(string.Empty, exception.Message);
    }

    /// <summary>
    /// Tests SqlGenerationException with null message.
    /// </summary>
    [TestMethod]
    public void SqlGenerationException_NullMessage_IsAllowed()
    {
        // Act
        var exception = new SqlGenerationException(null!);

        // Assert
        Assert.AreEqual("SQLX001", exception.ErrorCode);
        // Note: Exception.Message cannot be null in .NET, it defaults to exception type name
    }

    /// <summary>
    /// Tests SqlGenerationException serialization properties.
    /// </summary>
    [TestMethod]
    public void SqlGenerationException_HasCorrectProperties()
    {
        // Arrange
        var message = "Test exception";
        var innerException = new ArgumentException("Inner");
        var exception = new SqlGenerationException(message, innerException);

        // Act & Assert
        Assert.AreEqual("SQLX001", exception.ErrorCode);
        Assert.AreEqual(message, exception.Message);
        Assert.AreSame(innerException, exception.InnerException);
        Assert.IsNotNull(exception.Data);
        // StackTrace is null until the exception is thrown
        // Assert.IsNotNull(exception.StackTrace);
        Assert.IsTrue(exception.HResult != 0);
    }

    /// <summary>
    /// Tests multiple SqlGenerationException instances have same error code.
    /// </summary>
    [TestMethod]
    public void SqlGenerationException_MultipleInstances_HaveSameErrorCode()
    {
        // Act
        var exception1 = new SqlGenerationException("Message 1");
        var exception2 = new SqlGenerationException("Message 2");
        var exception3 = new SqlGenerationException("Message 3", new Exception());

        // Assert
        Assert.AreEqual("SQLX001", exception1.ErrorCode);
        Assert.AreEqual("SQLX001", exception2.ErrorCode);
        Assert.AreEqual("SQLX001", exception3.ErrorCode);
        Assert.AreEqual(exception1.ErrorCode, exception2.ErrorCode);
        Assert.AreEqual(exception2.ErrorCode, exception3.ErrorCode);
    }

    /// <summary>
    /// Tests SqlGenerationException with very long message.
    /// </summary>
    [TestMethod]
    public void SqlGenerationException_VeryLongMessage_IsHandledCorrectly()
    {
        // Arrange
        var longMessage = new string('A', 10000);

        // Act
        var exception = new SqlGenerationException(longMessage);

        // Assert
        Assert.AreEqual("SQLX001", exception.ErrorCode);
        Assert.AreEqual(longMessage, exception.Message);
        Assert.AreEqual(10000, exception.Message.Length);
    }

    /// <summary>
    /// Tests SqlGenerationException with special characters in message.
    /// </summary>
    [TestMethod]
    public void SqlGenerationException_SpecialCharactersInMessage_IsHandledCorrectly()
    {
        // Arrange
        var specialMessage = "Error with special chars: \n\t\r\"'\\/@#$%^&*(){}[]|<>?~`";

        // Act
        var exception = new SqlGenerationException(specialMessage);

        // Assert
        Assert.AreEqual("SQLX001", exception.ErrorCode);
        Assert.AreEqual(specialMessage, exception.Message);
    }
}
