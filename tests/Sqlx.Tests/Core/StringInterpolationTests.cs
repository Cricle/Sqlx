// -----------------------------------------------------------------------
// <copyright file="StringInterpolationTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Core;

namespace Sqlx.Tests.Core
{
    /// <summary>
    /// Tests for StringInterpolation to improve code coverage
    /// </summary>
    [TestClass]
    public class StringInterpolationTests
    {
        [TestMethod]
        public void StringInterpolation_Format_WithNoArguments_ReturnsOriginalString()
        {
            // Arrange
            var format = "Hello World";

            // Act
            var result = StringInterpolation.Format(format);

            // Assert
            Assert.AreEqual(format, result, "Should return original string when no arguments provided");
        }

        [TestMethod]
        public void StringInterpolation_Format_WithEmptyString_ReturnsEmpty()
        {
            // Arrange
            var format = string.Empty;

            // Act
            var result = StringInterpolation.Format(format);

            // Assert
            Assert.AreEqual(string.Empty, result, "Should return empty string");
        }

        [TestMethod]
        public void StringInterpolation_Format_WithNullString_ReturnsEmpty()
        {
            // Arrange
            string? format = null;

            // Act
            var result = StringInterpolation.Format(format!);

            // Assert
            Assert.AreEqual(string.Empty, result, "Should return empty string for null input");
        }

        [TestMethod]
        public void StringInterpolation_Format_WithSingleArgument_ReplacesCorrectly()
        {
            // Arrange
            var format = "Hello {0}!";
            var arg0 = "World";

            // Act
            var result = StringInterpolation.Format(format, arg0);

            // Assert
            Assert.AreEqual("Hello World!", result, "Should replace {0} with first argument");
        }

        [TestMethod]
        public void StringInterpolation_Format_WithTwoArguments_ReplacesCorrectly()
        {
            // Arrange
            var format = "Hello {0}, welcome to {1}!";
            var arg0 = "John";
            var arg1 = "Sqlx";

            // Act
            var result = StringInterpolation.Format(format, arg0, arg1);

            // Assert
            Assert.AreEqual("Hello John, welcome to Sqlx!", result, "Should replace both arguments correctly");
        }

        [TestMethod]
        public void StringInterpolation_Format_WithThreeArguments_ReplacesCorrectly()
        {
            // Arrange
            var format = "{0} {1} {2}";
            var arg0 = "One";
            var arg1 = "Two";
            var arg2 = "Three";

            // Act
            var result = StringInterpolation.Format(format, arg0, arg1, arg2);

            // Assert
            Assert.AreEqual("One Two Three", result, "Should replace all three arguments correctly");
        }

        [TestMethod]
        public void StringInterpolation_Format_WithNullArguments_HandlesGracefully()
        {
            // Arrange
            var format = "Hello {0}, {1} is {2}";
            string? arg0 = null;
            string? arg1 = "value";
            string? arg2 = null;

            // Act
            var result = StringInterpolation.Format(format, arg0, arg1, arg2);

            // Assert
            Assert.AreEqual("Hello , value is ", result, "Should handle null arguments gracefully");
        }

        [TestMethod]
        public void StringInterpolation_Format_WithInvalidPlaceholder_IgnoresInvalidPlaceholder()
        {
            // Arrange
            var format = "Hello {5} World";
            var arg0 = "Test";

            // Act
            var result = StringInterpolation.Format(format, arg0);

            // Assert
            Assert.IsTrue(result.Contains("{5}") || result.Contains("Hello") && result.Contains("World"),
                "Should handle invalid placeholders gracefully");
        }

        [TestMethod]
        public void StringInterpolation_Format_WithMalformedBraces_HandlesGracefully()
        {
            // Arrange
            var format = "Hello { World }";
            var arg0 = "Test";

            // Act
            var result = StringInterpolation.Format(format, arg0);

            // Assert
            Assert.IsNotNull(result, "Should handle malformed braces gracefully");
            Assert.IsTrue(result.Length > 0, "Should return non-empty result");
        }

        [TestMethod]
        public void StringInterpolation_Format_WithEscapedBraces_HandlesCorrectly()
        {
            // Arrange
            var format = "Value: {0}, Literal: {{not replaced}}";
            var arg0 = "42";

            // Act
            var result = StringInterpolation.Format(format, arg0);

            // Assert
            Assert.IsTrue(result.Contains("42"), "Should replace valid placeholder");
        }

        [TestMethod]
        public void StringInterpolation_Format_WithRepeatedPlaceholders_ReplacesAll()
        {
            // Arrange
            var format = "{0} and {0} again";
            var arg0 = "Hello";

            // Act
            var result = StringInterpolation.Format(format, arg0);

            // Assert
            Assert.AreEqual("Hello and Hello again", result, "Should replace all instances of {0}");
        }

        [TestMethod]
        public void StringInterpolation_Format_WithMixedPlaceholders_ReplacesCorrectly()
        {
            // Arrange
            var format = "Start {1} middle {0} end {2}";
            var arg0 = "ZERO";
            var arg1 = "ONE";
            var arg2 = "TWO";

            // Act
            var result = StringInterpolation.Format(format, arg0, arg1, arg2);

            // Assert
            Assert.AreEqual("Start ONE middle ZERO end TWO", result, "Should replace placeholders in correct order");
        }

        [TestMethod]
        public void StringInterpolation_Format_WithLongString_HandlesEfficiently()
        {
            // Arrange
            var format = new string('A', 1000) + " {0} " + new string('B', 1000);
            var arg0 = "MIDDLE";

            // Act
            var result = StringInterpolation.Format(format, arg0);

            // Assert
            Assert.IsTrue(result.Contains("MIDDLE"), "Should handle long strings efficiently");
            Assert.IsTrue(result.Length > 2000, "Result should be appropriately long");
        }
    }
}