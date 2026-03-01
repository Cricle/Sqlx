// <copyright file="SqlxExceptionTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Data;

namespace Sqlx.Tests.Core;

/// <summary>
/// Tests for SqlxException class covering all constructors, properties, and initialization scenarios.
/// </summary>
[TestClass]
public class SqlxExceptionTests
{
    [TestMethod]
    public void Constructor_WithMessage_SetsMessageAndTimestamp()
    {
        // Arrange
        var message = "Test error message";
        var beforeCreation = DateTimeOffset.UtcNow;

        // Act
        var exception = new SqlxException(message);

        // Assert
        var afterCreation = DateTimeOffset.UtcNow;
        Assert.AreEqual(message, exception.Message);
        Assert.IsNull(exception.InnerException);
        Assert.IsTrue(exception.Timestamp >= beforeCreation && exception.Timestamp <= afterCreation);
    }

    [TestMethod]
    public void Constructor_WithMessageAndInnerException_SetsAllProperties()
    {
        // Arrange
        var message = "Test error message";
        var innerException = new InvalidOperationException("Inner error");
        var beforeCreation = DateTimeOffset.UtcNow;

        // Act
        var exception = new SqlxException(message, innerException);

        // Assert
        var afterCreation = DateTimeOffset.UtcNow;
        Assert.AreEqual(message, exception.Message);
        Assert.AreSame(innerException, exception.InnerException);
        Assert.IsTrue(exception.Timestamp >= beforeCreation && exception.Timestamp <= afterCreation);
    }

    [TestMethod]
    public void Constructor_WithNullInnerException_AllowsNull()
    {
        // Arrange
        var message = "Test error message";

        // Act
        var exception = new SqlxException(message, null);

        // Assert
        Assert.AreEqual(message, exception.Message);
        Assert.IsNull(exception.InnerException);
    }

    [TestMethod]
    public void SqlProperty_CanBeInitialized()
    {
        // Arrange & Act
        var exception = new SqlxException("Error") { Sql = "SELECT * FROM Users" };

        // Assert
        Assert.AreEqual("SELECT * FROM Users", exception.Sql);
    }

    [TestMethod]
    public void SqlProperty_DefaultsToNull()
    {
        // Arrange & Act
        var exception = new SqlxException("Error");

        // Assert
        Assert.IsNull(exception.Sql);
    }

    [TestMethod]
    public void ParametersProperty_CanBeInitialized()
    {
        // Arrange
        var parameters = new Dictionary<string, object?> { ["id"] = 123, ["name"] = "Test" };

        // Act
        var exception = new SqlxException("Error") { Parameters = parameters };

        // Assert
        Assert.AreSame(parameters, exception.Parameters);
        Assert.AreEqual(2, exception.Parameters.Count);
    }

    [TestMethod]
    public void ParametersProperty_DefaultsToNull()
    {
        // Arrange & Act
        var exception = new SqlxException("Error");

        // Assert
        Assert.IsNull(exception.Parameters);
    }

    [TestMethod]
    public void MethodNameProperty_CanBeInitialized()
    {
        // Arrange & Act
        var exception = new SqlxException("Error") { MethodName = "GetUserByIdAsync" };

        // Assert
        Assert.AreEqual("GetUserByIdAsync", exception.MethodName);
    }

    [TestMethod]
    public void DurationProperty_CanBeInitialized()
    {
        // Arrange & Act
        var exception = new SqlxException("Error") { Duration = TimeSpan.FromMilliseconds(250) };

        // Assert
        Assert.AreEqual(TimeSpan.FromMilliseconds(250), exception.Duration);
    }

    [TestMethod]
    public void CorrelationIdProperty_CanBeInitialized()
    {
        // Arrange & Act
        var exception = new SqlxException("Error") { CorrelationId = "trace-id-12345" };

        // Assert
        Assert.AreEqual("trace-id-12345", exception.CorrelationId);
    }

    [TestMethod]
    public void TransactionIsolationLevelProperty_CanBeInitialized()
    {
        // Arrange & Act
        var exception = new SqlxException("Error") { TransactionIsolationLevel = IsolationLevel.Serializable };

        // Assert
        Assert.AreEqual(IsolationLevel.Serializable, exception.TransactionIsolationLevel);
    }

    [TestMethod]
    public void AllProperties_CanBeInitializedTogether()
    {
        // Arrange
        var message = "Database operation failed";
        var innerException = new TimeoutException("Connection timeout");
        var sql = "UPDATE Users SET Name = @name WHERE Id = @id";
        var parameters = new Dictionary<string, object?> { ["id"] = 42, ["name"] = "John" };
        var methodName = "UpdateUserAsync";
        var duration = TimeSpan.FromSeconds(5);
        var correlationId = "correlation-123";
        var timestamp = new DateTimeOffset(2024, 2, 20, 14, 30, 0, TimeSpan.Zero);
        var isolationLevel = IsolationLevel.ReadCommitted;

        // Act
        var exception = new SqlxException(message, innerException)
        {
            Sql = sql,
            Parameters = parameters,
            MethodName = methodName,
            Duration = duration,
            CorrelationId = correlationId,
            Timestamp = timestamp,
            TransactionIsolationLevel = isolationLevel
        };

        // Assert
        Assert.AreEqual(message, exception.Message);
        Assert.AreSame(innerException, exception.InnerException);
        Assert.AreEqual(sql, exception.Sql);
        Assert.AreSame(parameters, exception.Parameters);
        Assert.AreEqual(methodName, exception.MethodName);
        Assert.AreEqual(duration, exception.Duration);
        Assert.AreEqual(correlationId, exception.CorrelationId);
        Assert.AreEqual(timestamp, exception.Timestamp);
        Assert.AreEqual(isolationLevel, exception.TransactionIsolationLevel);
    }

    [TestMethod]
    public void Exception_InheritsFromException()
    {
        // Arrange & Act
        var exception = new SqlxException("Test error");

        // Assert
        Assert.IsInstanceOfType(exception, typeof(Exception));
    }
}
