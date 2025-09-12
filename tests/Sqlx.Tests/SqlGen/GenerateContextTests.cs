// -----------------------------------------------------------------------
// <copyright file="GenerateContextTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.SqlGen;

namespace Sqlx.Tests.SqlGen
{
    /// <summary>
    /// Tests for GenerateContext record classes to achieve 100% coverage.
    /// </summary>
    [TestClass]
    public class GenerateContextTests : CodeGenerationTestBase
    {
        [TestMethod]
        public void GenerateContext_GetColumnName_WithPascalCase_ReturnsSnakeCase()
        {
            // Act
            var result = GenerateContext.GetColumnName("UserId");

            // Assert
            Assert.AreEqual("user_id", result);
        }

        [TestMethod]
        public void GenerateContext_GetColumnName_WithCamelCase_ReturnsSnakeCase()
        {
            // Act
            var result = GenerateContext.GetColumnName("firstName");

            // Assert
            Assert.AreEqual("first_name", result);
        }

        [TestMethod]
        public void GenerateContext_GetColumnName_WithUPPER_CASE_ReturnsLowerCase()
        {
            // Act
            var result = GenerateContext.GetColumnName("USER_ID");

            // Assert
            Assert.AreEqual("user_id", result);
        }

        [TestMethod]
        public void GenerateContext_GetColumnName_WithSingleWord_ReturnsLowerCase()
        {
            // Act
            var result = GenerateContext.GetColumnName("Name");

            // Assert
            Assert.AreEqual("name", result);
        }

        [TestMethod]
        public void GenerateContext_GetColumnName_WithEmptyString_ReturnsEmpty()
        {
            // Act
            var result = GenerateContext.GetColumnName("");

            // Assert
            Assert.AreEqual("", result);
        }

        [TestMethod]
        public void GenerateContext_GetColumnName_WithNull_ReturnsNull()
        {
            // Act
            var result = GenerateContext.GetColumnName(null!);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public void GenerateContext_GetColumnName_WithComplexName_ReturnsCorrectSnakeCase()
        {
            // Act
            var result = GenerateContext.GetColumnName("CreatedDateTime");

            // Assert
            Assert.AreEqual("created_date_time", result);
        }

        [TestMethod]
        public void GenerateContext_GetColumnName_WithAcronym_HandlesCorrectly()
        {
            // Act
            var result = GenerateContext.GetColumnName("XMLHttpRequest");

            // Assert
            Assert.AreEqual("x_m_l_http_request", result);
        }

        [TestMethod]
        public void GenerateContext_GetColumnName_WithNumbers_HandlesCorrectly()
        {
            // Act
            var result = GenerateContext.GetColumnName("User123Id");

            // Assert
            Assert.AreEqual("user123_id", result);
        }

        [TestMethod]
        public void GenerateContext_GetColumnName_WithUnderscoreAndNumbers_ReturnsLowerCase()
        {
            // Act
            var result = GenerateContext.GetColumnName("USER_123_ID");

            // Assert
            Assert.AreEqual("user_123_id", result);
        }

        [TestMethod]
        public void GenerateContext_GetParameterName_WithPrefix_ReturnsCorrectFormat()
        {
            // Act
            var result = GenerateContext.GetParamterName("@", "UserId");

            // Assert
            Assert.AreEqual("@user_id", result);
        }

        [TestMethod]
        public void GenerateContext_GetParameterName_WithDollarPrefix_ReturnsCorrectFormat()
        {
            // Act
            var result = GenerateContext.GetParamterName("$", "FirstName");

            // Assert
            Assert.AreEqual("$first_name", result);
        }

        // Complex record tests with mocked Roslyn symbols would be here
        // but they require extensive setup and the main logic is in static methods

        // Note: Testing the complex record constructors requires full Roslyn symbol setup
        // which is complex for unit tests. These records are mainly data containers
        // and their primary logic is in the static GetColumnName method which is tested above.
    }
}
