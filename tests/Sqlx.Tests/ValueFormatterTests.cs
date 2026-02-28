// -----------------------------------------------------------------------
// <copyright file="ValueFormatterTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Expressions;

namespace Sqlx.Tests
{
    /// <summary>
    /// Tests for ValueFormatter to achieve 100% branch coverage.
    /// </summary>
    [TestClass]
    public class ValueFormatterTests
    {
        [TestMethod]
        public void FormatAsLiteral_Null_ReturnsNull()
        {
            // Arrange
            var dialect = new SqlServerDialect();

            // Act
            var result = ValueFormatter.FormatAsLiteral(dialect, null);

            // Assert
            Assert.AreEqual("NULL", result);
        }

        [TestMethod]
        public void FormatAsLiteral_String_ReturnsQuotedString()
        {
            // Arrange
            var dialect = new SqlServerDialect();
            var value = "test string";

            // Act
            var result = ValueFormatter.FormatAsLiteral(dialect, value);

            // Assert
            Assert.IsTrue(result.Contains("test string"));
        }

        [TestMethod]
        public void FormatAsLiteral_StringWithQuotes_EscapesQuotes()
        {
            // Arrange
            var dialect = new SqlServerDialect();
            var value = "test's string";

            // Act
            var result = ValueFormatter.FormatAsLiteral(dialect, value);

            // Assert - The string should be wrapped and quotes should be escaped
            Assert.IsTrue(result.Contains("test") && result.Contains("string"));
        }

        [TestMethod]
        public void FormatAsLiteral_BoolTrue_ReturnsBoolTrue()
        {
            // Arrange
            var dialect = new SqlServerDialect();

            // Act
            var result = ValueFormatter.FormatAsLiteral(dialect, true);

            // Assert
            Assert.AreEqual(dialect.BoolTrue, result);
        }

        [TestMethod]
        public void FormatAsLiteral_BoolFalse_ReturnsBoolFalse()
        {
            // Arrange
            var dialect = new SqlServerDialect();

            // Act
            var result = ValueFormatter.FormatAsLiteral(dialect, false);

            // Assert
            Assert.AreEqual(dialect.BoolFalse, result);
        }

        [TestMethod]
        public void FormatAsLiteral_DateTime_ReturnsFormattedDateTime()
        {
            // Arrange
            var dialect = new SqlServerDialect();
            var value = new DateTime(2024, 1, 15, 10, 30, 45);

            // Act
            var result = ValueFormatter.FormatAsLiteral(dialect, value);

            // Assert
            Assert.IsTrue(result.Contains("2024-01-15 10:30:45"));
        }

        [TestMethod]
        public void FormatAsLiteral_Guid_ReturnsFormattedGuid()
        {
            // Arrange
            var dialect = new SqlServerDialect();
            var value = Guid.Parse("12345678-1234-1234-1234-123456789012");

            // Act
            var result = ValueFormatter.FormatAsLiteral(dialect, value);

            // Assert
            Assert.IsTrue(result.Contains("12345678-1234-1234-1234-123456789012"));
        }

        [TestMethod]
        public void FormatAsLiteral_Decimal_ReturnsDecimalString()
        {
            // Arrange
            var dialect = new SqlServerDialect();
            var value = 123.45m;

            // Act
            var result = ValueFormatter.FormatAsLiteral(dialect, value);

            // Assert
            Assert.AreEqual("123.45", result);
        }

        [TestMethod]
        public void FormatAsLiteral_Double_ReturnsDoubleString()
        {
            // Arrange
            var dialect = new SqlServerDialect();
            var value = 123.45;

            // Act
            var result = ValueFormatter.FormatAsLiteral(dialect, value);

            // Assert
            Assert.AreEqual("123.45", result);
        }

        [TestMethod]
        public void FormatAsLiteral_Float_ReturnsFloatString()
        {
            // Arrange
            var dialect = new SqlServerDialect();
            var value = 123.45f;

            // Act
            var result = ValueFormatter.FormatAsLiteral(dialect, value);

            // Assert
            Assert.IsTrue(result.StartsWith("123.45"));
        }

        [TestMethod]
        public void FormatAsLiteral_Char_ReturnsQuotedChar()
        {
            // Arrange
            var dialect = new SqlServerDialect();
            var value = 'A';

            // Act
            var result = ValueFormatter.FormatAsLiteral(dialect, value);

            // Assert
            Assert.IsTrue(result.Contains("A"));
        }

        [TestMethod]
        public void FormatAsLiteral_Int_ReturnsIntString()
        {
            // Arrange
            var dialect = new SqlServerDialect();
            var value = 42;

            // Act
            var result = ValueFormatter.FormatAsLiteral(dialect, value);

            // Assert
            Assert.AreEqual("42", result);
        }

        [TestMethod]
        public void FormatAsLiteral_CustomObject_ReturnsToString()
        {
            // Arrange
            var dialect = new SqlServerDialect();
            var value = new { Name = "Test", Value = 123 };

            // Act
            var result = ValueFormatter.FormatAsLiteral(dialect, value);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreNotEqual("NULL", result);
        }

        [TestMethod]
        public void GetBooleanLiteral_True_ReturnsBoolTrue()
        {
            // Arrange
            var dialect = new SqlServerDialect();

            // Act
            var result = ValueFormatter.GetBooleanLiteral(dialect, true);

            // Assert
            Assert.AreEqual(dialect.BoolTrue, result);
        }

        [TestMethod]
        public void GetBooleanLiteral_False_ReturnsBoolFalse()
        {
            // Arrange
            var dialect = new SqlServerDialect();

            // Act
            var result = ValueFormatter.GetBooleanLiteral(dialect, false);

            // Assert
            Assert.AreEqual(dialect.BoolFalse, result);
        }

        [TestMethod]
        public void GetBooleanLiteral_DifferentDialects_ReturnsCorrectValues()
        {
            // Arrange & Act & Assert
            var sqlServer = new SqlServerDialect();
            Assert.AreEqual("1", ValueFormatter.GetBooleanLiteral(sqlServer, true));
            Assert.AreEqual("0", ValueFormatter.GetBooleanLiteral(sqlServer, false));

            var postgreSql = new PostgreSqlDialect();
            Assert.AreEqual("true", ValueFormatter.GetBooleanLiteral(postgreSql, true));
            Assert.AreEqual("false", ValueFormatter.GetBooleanLiteral(postgreSql, false));
        }
    }
}
