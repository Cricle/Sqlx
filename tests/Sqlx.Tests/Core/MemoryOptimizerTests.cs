// -----------------------------------------------------------------------
// <copyright file="MemoryOptimizerTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Core;

namespace Sqlx.Tests.Core
{
    /// <summary>
    /// Tests for MemoryOptimizer to improve code coverage
    /// </summary>
    [TestClass]
    public class MemoryOptimizerTests
    {
        [TestInitialize]
        public void Setup()
        {
            // Clear any existing state
        }

        [TestCleanup]
        public void Cleanup()
        {
            // Clean up after tests
        }

        [TestMethod]
        public void MemoryOptimizer_CreateOptimizedStringBuilder_WithValidSize_ReturnsStringBuilder()
        {
            // Arrange
            var estimatedSize = 1024;

            // Act
            var result = MemoryOptimizer.CreateOptimizedStringBuilder(estimatedSize);

            // Assert
            Assert.IsNotNull(result, "Should return a valid StringBuilder");
            Assert.IsTrue(result.Capacity >= estimatedSize, "Capacity should be at least the estimated size");
        }

        [TestMethod]
        public void MemoryOptimizer_CreateOptimizedStringBuilder_WithZeroSize_ReturnsStringBuilder()
        {
            // Arrange
            var estimatedSize = 0;

            // Act
            var result = MemoryOptimizer.CreateOptimizedStringBuilder(estimatedSize);

            // Assert
            Assert.IsNotNull(result, "Should return a valid StringBuilder even with zero size");
            Assert.IsTrue(result.Capacity >= 256, "Should have minimum capacity");
        }

        [TestMethod]
        public void MemoryOptimizer_ConcatenateEfficiently_WithMultipleStrings_ReturnsCorrectResult()
        {
            // Arrange
            var strings = new[] { "Hello", " ", "World", "!" };

            // Act
            var result = MemoryOptimizer.ConcatenateEfficiently(strings);

            // Assert
            Assert.AreEqual("Hello World!", result, "Should concatenate strings correctly");
        }

        [TestMethod]
        public void MemoryOptimizer_ConcatenateEfficiently_WithEmptyArray_ReturnsEmpty()
        {
            // Arrange
            var strings = new string[0];

            // Act
            var result = MemoryOptimizer.ConcatenateEfficiently(strings);

            // Assert
            Assert.AreEqual(string.Empty, result, "Empty array should return empty string");
        }

        [TestMethod]
        public void MemoryOptimizer_ConcatenateEfficiently_WithSingleString_ReturnsSameString()
        {
            // Arrange
            var strings = new[] { "Hello World" };

            // Act
            var result = MemoryOptimizer.ConcatenateEfficiently(strings);

            // Assert
            Assert.AreEqual("Hello World", result, "Single string should be returned as-is");
        }

        [TestMethod]
        public void MemoryOptimizer_ConcatenateEfficiently_WithNullStrings_HandlesGracefully()
        {
            // Arrange
            var strings = new string?[] { "Hello", null, "World" };

            // Act
            var result = MemoryOptimizer.ConcatenateEfficiently(strings!);

            // Assert
            Assert.AreEqual("HelloWorld", result, "Should handle null strings gracefully");
        }

        [TestMethod]
        public void MemoryOptimizer_BuildString_WithValidAction_ReturnsBuiltString()
        {
            // Arrange
            var expectedResult = "Test String Builder";

            // Act
            var result = MemoryOptimizer.BuildString(sb => sb.Append(expectedResult));

            // Assert
            Assert.AreEqual(expectedResult, result, "Should build string correctly");
        }

        [TestMethod]
        public void MemoryOptimizer_BuildString_WithComplexAction_ReturnsCorrectString()
        {
            // Arrange
            var expectedResult = "Line1" + Environment.NewLine + "Line2" + Environment.NewLine + "Line3";

            // Act
            var result = MemoryOptimizer.BuildString(sb =>
            {
                sb.AppendLine("Line1");
                sb.AppendLine("Line2");
                sb.Append("Line3");
            });

            // Assert
            Assert.AreEqual(expectedResult, result, "Should handle complex string building");
        }
    }
}
