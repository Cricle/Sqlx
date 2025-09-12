// -----------------------------------------------------------------------
// <copyright file="NameMapperExtendedTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Sqlx;

namespace Sqlx.Tests
{
    /// <summary>
    /// Extended tests for NameMapper class to improve coverage.
    /// </summary>
    [TestClass]
    public class NameMapperExtendedTests
    {
        [TestMethod]
        public void MapName_WithValidParameterName_ReturnsSnakeCase()
        {
            // Arrange
            var parameterName = "UserName";

            // Act
            var result = NameMapper.MapName(parameterName);

            // Assert
            Assert.AreEqual("user_name", result);
        }

        [TestMethod]
        public void MapName_WithNullParameterName_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() => NameMapper.MapName(null!));
        }

        [TestMethod]
        public void MapNameToSnakeCase_WithNullParameterName_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() => NameMapper.MapNameToSnakeCase(null!));
        }

        [TestMethod]
        public void MapNameToSnakeCase_WithSpecialCharacters_ReturnsLowerCase()
        {
            // Arrange
            var parameterName = "@UserName";

            // Act
            var result = NameMapper.MapNameToSnakeCase(parameterName);

            // Assert
            Assert.AreEqual("@username", result);
        }

        [TestMethod]
        public void MapNameToSnakeCase_WithHashCharacter_ReturnsLowerCase()
        {
            // Arrange
            var parameterName = "#TempValue";

            // Act
            var result = NameMapper.MapNameToSnakeCase(parameterName);

            // Assert
            Assert.AreEqual("#tempvalue", result);
        }

        [TestMethod]
        public void MapNameToSnakeCase_WithDashCharacter_ReturnsLowerCase()
        {
            // Arrange
            var parameterName = "user-name";

            // Act
            var result = NameMapper.MapNameToSnakeCase(parameterName);

            // Assert
            Assert.AreEqual("user-name", result);
        }

        [TestMethod]
        public void MapNameToSnakeCase_WithDotCharacter_ReturnsLowerCase()
        {
            // Arrange
            var parameterName = "user.name";

            // Act
            var result = NameMapper.MapNameToSnakeCase(parameterName);

            // Assert
            Assert.AreEqual("user.name", result);
        }

        [TestMethod]
        public void MapNameToSnakeCase_WithAllUpperCase_ReturnsSnakeCase()
        {
            // Arrange
            var parameterName = "USERNAME";

            // Act
            var result = NameMapper.MapNameToSnakeCase(parameterName);

            // Assert
            Assert.AreEqual("u_s_e_r_n_a_m_e", result);
        }

        [TestMethod]
        public void MapNameToSnakeCase_WithCamelCaseStartingLowercase_ReturnsSnakeCase()
        {
            // Arrange
            var parameterName = "userNameId";

            // Act
            var result = NameMapper.MapNameToSnakeCase(parameterName);

            // Assert
            Assert.AreEqual("user_name_id", result);
        }

        [TestMethod]
        public void MapNameToSnakeCase_WithPascalCase_ReturnsSnakeCase()
        {
            // Arrange
            var parameterName = "UserNameId";

            // Act
            var result = NameMapper.MapNameToSnakeCase(parameterName);

            // Assert
            Assert.AreEqual("user_name_id", result);
        }

        [TestMethod]
        public void MapNameToSnakeCase_WithSingleWord_ReturnsLowerCase()
        {
            // Arrange
            var parameterName = "name";

            // Act
            var result = NameMapper.MapNameToSnakeCase(parameterName);

            // Assert
            Assert.AreEqual("name", result);
        }

        [TestMethod]
        public void MapNameToSnakeCase_WithSingleUpperCaseWord_ReturnsLowerCase()
        {
            // Arrange
            var parameterName = "Name";

            // Act
            var result = NameMapper.MapNameToSnakeCase(parameterName);

            // Assert
            Assert.AreEqual("name", result);
        }

        [TestMethod]
        public void MapNameToSnakeCase_WithEmptyString_ReturnsEmptyString()
        {
            // Arrange
            var parameterName = "";

            // Act
            var result = NameMapper.MapNameToSnakeCase(parameterName);

            // Assert
            Assert.AreEqual("", result);
        }

        [TestMethod]
        public void MapNameToSnakeCase_WithNumbersAndLetters_ReturnsSnakeCase()
        {
            // Arrange
            var parameterName = "User123Name";

            // Act
            var result = NameMapper.MapNameToSnakeCase(parameterName);

            // Assert
            Assert.AreEqual("user123_name", result);
        }

        [TestMethod]
        public void MapNameToSnakeCase_WithUnderscores_PreservesUnderscores()
        {
            // Arrange
            var parameterName = "user_name_id";

            // Act
            var result = NameMapper.MapNameToSnakeCase(parameterName);

            // Assert
            Assert.AreEqual("user_name_id", result);
        }

        [TestMethod]
        public void MapNameToSnakeCase_WithMixedCaseAndUnderscores_ReturnsSnakeCase()
        {
            // Arrange
            var parameterName = "User_NameId";

            // Act
            var result = NameMapper.MapNameToSnakeCase(parameterName);

            // Assert
            Assert.AreEqual("user__name_id", result);
        }

        [TestMethod]
        public void MapNameToSnakeCase_WithConsecutiveUpperCase_ReturnsSnakeCase()
        {
            // Arrange
            var parameterName = "HTMLParser";

            // Act
            var result = NameMapper.MapNameToSnakeCase(parameterName);

            // Assert
            Assert.AreEqual("h_t_m_l_parser", result);
        }
    }
}
