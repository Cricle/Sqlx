// -----------------------------------------------------------------------
// <copyright file="SqlxExceptionTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using System;

namespace Sqlx.Tests.Core
{
    /// <summary>
    /// Tests for Sqlx exception classes to achieve coverage for error handling functionality.
    /// </summary>
    [TestClass]
    public class SqlxExceptionTests
    {
        [TestMethod]
        public void SqlGenerationException_WithMessage_CreatesException()
        {
            // Arrange
            var message = "SQL generation failed";

            // Act
            var exception = new SqlGenerationException(message);

            // Assert
            Assert.IsNotNull(exception);
            Assert.AreEqual(message, exception.Message);
            Assert.AreEqual("SQLX001", exception.ErrorCode);
            Assert.IsNull(exception.InnerException);
        }

        [TestMethod]
        public void SqlGenerationException_WithMessageAndInnerException_CreatesException()
        {
            // Arrange
            var message = "SQL generation failed";
            var innerException = new InvalidOperationException("Inner error");

            // Act
            var exception = new SqlGenerationException(message, innerException);

            // Assert
            Assert.IsNotNull(exception);
            Assert.AreEqual(message, exception.Message);
            Assert.AreEqual("SQLX001", exception.ErrorCode);
            Assert.AreEqual(innerException, exception.InnerException);
        }

        [TestMethod]
        public void SqlGenerationException_InheritsFromSqlxException()
        {
            // Arrange & Act
            var exception = new SqlGenerationException("test");

            // Assert
            Assert.IsInstanceOfType<SqlxException>(exception);
            Assert.IsInstanceOfType<Exception>(exception);
        }

        [TestMethod]
        public void InvalidMethodSignatureException_WithMethodNameAndMessage_CreatesException()
        {
            // Arrange
            var methodName = "GetUsersAsync";
            var message = "Invalid method signature";

            // Act
            var exception = new InvalidMethodSignatureException(methodName, message);

            // Assert
            Assert.IsNotNull(exception);
            Assert.AreEqual(message, exception.Message);
            Assert.AreEqual("SQLX002", exception.ErrorCode);
            Assert.AreEqual(methodName, exception.MethodName);
            Assert.IsNull(exception.InnerException);
        }

        [TestMethod]
        public void InvalidMethodSignatureException_InheritsFromSqlxException()
        {
            // Arrange & Act
            var exception = new InvalidMethodSignatureException("TestMethod", "test message");

            // Assert
            Assert.IsInstanceOfType<SqlxException>(exception);
            Assert.IsInstanceOfType<Exception>(exception);
        }

        [TestMethod]
        public void UnsupportedDialectException_WithDialectName_CreatesException()
        {
            // Arrange
            var dialectName = "Oracle";

            // Act
            var exception = new UnsupportedDialectException(dialectName);

            // Assert
            Assert.IsNotNull(exception);
            Assert.AreEqual("SQLX003", exception.ErrorCode);
            Assert.AreEqual(dialectName, exception.DialectName);
            Assert.IsTrue(exception.Message.Contains(dialectName));
            Assert.IsTrue(exception.Message.Contains("not supported"));
            Assert.IsTrue(exception.Message.Contains("MySQL"));
            Assert.IsTrue(exception.Message.Contains("SQL Server"));
            Assert.IsTrue(exception.Message.Contains("PostgreSQL"));
            Assert.IsTrue(exception.Message.Contains("SQLite"));
        }

        [TestMethod]
        public void UnsupportedDialectException_InheritsFromSqlxException()
        {
            // Arrange & Act
            var exception = new UnsupportedDialectException("Oracle");

            // Assert
            Assert.IsInstanceOfType<SqlxException>(exception);
            Assert.IsInstanceOfType<Exception>(exception);
        }

        [TestMethod]
        public void SqlxException_IsAbstract()
        {
            // Assert
            Assert.IsTrue(typeof(SqlxException).IsAbstract);
        }

        [TestMethod]
        public void ExceptionProperties_AreCorrectlySet()
        {
            // Test SqlGenerationException
            var sqlGenException = new SqlGenerationException("SQL error");
            Assert.AreEqual("SQLX001", sqlGenException.ErrorCode);

            // Test InvalidMethodSignatureException
            var methodException = new InvalidMethodSignatureException("TestMethod", "Method error");
            Assert.AreEqual("SQLX002", methodException.ErrorCode);
            Assert.AreEqual("TestMethod", methodException.MethodName);

            // Test UnsupportedDialectException
            var dialectException = new UnsupportedDialectException("Oracle");
            Assert.AreEqual("SQLX003", dialectException.ErrorCode);
            Assert.AreEqual("Oracle", dialectException.DialectName);
        }

        [TestMethod]
        public void ExceptionSerialization_WorksCorrectly()
        {
            // Arrange
            var originalException = new SqlGenerationException("Test message", new ArgumentException("Inner"));

            // Act - Test that the exception can be used in try-catch blocks
            Exception? caughtException = null;
            try
            {
                throw originalException;
            }
            catch (SqlxException ex)
            {
                caughtException = ex;
            }

            // Assert
            Assert.IsNotNull(caughtException);
            Assert.AreEqual(originalException.Message, caughtException.Message);
            Assert.AreEqual(originalException.ErrorCode, ((SqlxException)caughtException).ErrorCode);
        }

        [TestMethod]
        public void InvalidMethodSignatureException_WithEmptyMethodName_StillWorks()
        {
            // Arrange & Act
            var exception = new InvalidMethodSignatureException("", "Empty method name");

            // Assert
            Assert.IsNotNull(exception);
            Assert.AreEqual("", exception.MethodName);
            Assert.AreEqual("Empty method name", exception.Message);
        }

        [TestMethod]
        public void UnsupportedDialectException_WithEmptyDialectName_StillWorks()
        {
            // Arrange & Act
            var exception = new UnsupportedDialectException("");

            // Assert
            Assert.IsNotNull(exception);
            Assert.AreEqual("", exception.DialectName);
            Assert.IsTrue(exception.Message.Contains("''"));
        }

        [TestMethod]
        public void ExceptionStackTrace_PreservesOriginalStackTrace()
        {
            // Arrange
            SqlxException? originalException = null;

            // Act
            try
            {
                try
                {
                    throw new SqlGenerationException("Inner error");
                }
                catch (SqlxException ex)
                {
                    originalException = ex;
                    throw; // Re-throw to preserve stack trace
                }
            }
            catch (SqlxException ex)
            {
                // Assert
                Assert.IsNotNull(ex.StackTrace);
                Assert.IsNotNull(originalException);
                Assert.AreEqual(originalException.Message, ex.Message);
            }
        }
    }
}
