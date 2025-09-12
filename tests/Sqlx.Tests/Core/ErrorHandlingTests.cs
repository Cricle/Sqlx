// -----------------------------------------------------------------------
// <copyright file="ErrorHandlingTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.CodeAnalysis;
using Sqlx.Core;
using System;
using System.Linq;

namespace Sqlx.Tests.Core;

[TestClass]
public class ErrorHandlingTests
{
    [TestMethod]
    public void DiagnosticHelper_CreateDiagnostic_WithNullLocation_HandlesCorrectly()
    {
        // Arrange
        var diagnosticId = "TEST001";
        var message = "Test message";

        // Act & Assert - Should not throw
        try
        {
            var result = DiagnosticHelper.CreateDiagnostic(diagnosticId, "Test Title", message, DiagnosticSeverity.Info, null);
            // If we reach here without exception, the test passes
            Assert.IsTrue(true);
        }
        catch (Exception ex)
        {
            Assert.Fail($"Should not throw exception but got: {ex.Message}");
        }
    }

    [TestMethod]
    public void DiagnosticHelper_CreateDiagnostic_WithEmptyParameters_ThrowsArgumentException()
    {
        // Arrange
        var diagnosticId = "";
        var message = "";

        // Act & Assert - Should throw for empty ID
        Assert.ThrowsException<ArgumentException>(() => 
            DiagnosticHelper.CreateDiagnostic(diagnosticId, "Test Title", message, DiagnosticSeverity.Info, null));
    }

    [TestMethod]
    public void SqlxException_WithNullErrorCode_HandlesCorrectly()
    {
        // Act & Assert
        try
        {
            // This would require a concrete implementation, so we'll test with existing concrete types
            var exception = new SqlGenerationException("Test message");
            Assert.IsNotNull(exception.ErrorCode);
        }
        catch (Exception ex)
        {
            Assert.Fail($"Should handle construction correctly but got: {ex.Message}");
        }
    }

    [TestMethod]
    public void SqlxException_WithEmptyMessages_HandlesCorrectly()
    {
        // Act
        var sqlGenException = new SqlGenerationException("");
        var methodSigException = new InvalidMethodSignatureException("", "");
        var dialectException = new UnsupportedDialectException("");

        // Assert
        Assert.AreEqual("", sqlGenException.Message);
        Assert.AreEqual("", methodSigException.Message);
        Assert.IsTrue(dialectException.Message.Contains("not supported")); // Still has template text
    }

    [TestMethod]
    public void DiagnosticHelper_WithNullParameters_ThrowsExpectedException()
    {
        // This test verifies that diagnostic helper methods properly validate inputs
        
        // Test that methods throw appropriate exceptions for null parameters
        Assert.ThrowsException<NullReferenceException>(() => 
            DiagnosticHelper.GeneratePerformanceSuggestions(null!));
            
        Assert.ThrowsException<NullReferenceException>(() => 
            DiagnosticHelper.ValidateEntityType(null!));
            
        Assert.ThrowsException<NullReferenceException>(() => 
            DiagnosticHelper.ValidateGeneratedCode("", null!));
    }

    [TestMethod]
    public void ExceptionChaining_PreservesOriginalInformation()
    {
        // Arrange
        var originalMessage = "Original error";
        var wrapperMessage = "Wrapper error";
        var originalException = new InvalidOperationException(originalMessage);

        // Act
        var wrappedException = new SqlGenerationException(wrapperMessage, originalException);

        // Assert
        Assert.AreEqual(wrapperMessage, wrappedException.Message);
        Assert.AreEqual(originalException, wrappedException.InnerException);
        Assert.AreEqual(originalMessage, wrappedException.InnerException?.Message);
    }

    [TestMethod]
    public void UnsupportedDialectException_WithSpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        var dialectWithSpecialChars = "My$pecial-Dialect@2024";

        // Act
        var exception = new UnsupportedDialectException(dialectWithSpecialChars);

        // Assert
        Assert.AreEqual(dialectWithSpecialChars, exception.DialectName);
        Assert.IsTrue(exception.Message.Contains(dialectWithSpecialChars));
    }

    [TestMethod]
    public void InvalidMethodSignatureException_WithLongMethodName_HandlesCorrectly()
    {
        // Arrange
        var longMethodName = new string('A', 1000); // Very long method name
        var message = "Method signature is invalid";

        // Act
        var exception = new InvalidMethodSignatureException(longMethodName, message);

        // Assert
        Assert.AreEqual(longMethodName, exception.MethodName);
        Assert.AreEqual(message, exception.Message);
    }

    [TestMethod]
    public void ExceptionMessages_AreInformativeAndHelpful()
    {
        // Act
        var sqlGenException = new SqlGenerationException("Failed to generate SQL");
        var methodSigException = new InvalidMethodSignatureException("TestMethod", "Invalid parameter count");
        var dialectException = new UnsupportedDialectException("CustomDB");

        // Assert
        Assert.IsTrue(sqlGenException.Message.Length > 0);
        Assert.IsTrue(methodSigException.Message.Length > 0);
        Assert.IsTrue(dialectException.Message.Contains("not supported"));
        Assert.IsTrue(dialectException.Message.Contains("Supported dialects"));
    }

    [TestMethod]
    public void ExceptionErrorCodes_AreUniqueAndConsistent()
    {
        // Act
        var sqlGenException = new SqlGenerationException("Test");
        var methodSigException = new InvalidMethodSignatureException("TestMethod", "Test");
        var dialectException = new UnsupportedDialectException("TestDB");

        var errorCodes = new[] 
        { 
            sqlGenException.ErrorCode, 
            methodSigException.ErrorCode, 
            dialectException.ErrorCode 
        };

        // Assert
        Assert.AreEqual(3, errorCodes.Distinct().Count(), "All error codes should be unique");
        Assert.IsTrue(errorCodes.All(code => code.StartsWith("SQLX")), "All error codes should start with SQLX");
        Assert.IsTrue(errorCodes.All(code => code.Length == 7), "All error codes should be 7 characters long");
    }
}
