// -----------------------------------------------------------------------
// <copyright file="NameMapperExtendedTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Sqlx.Tests.Core;

[TestClass]
public class NameMapperExtendedTests
{
    [TestMethod]
    public void MapName_WithSimpleName_ReturnsLowercase()
    {
        // Arrange
        var parameterName = "username";

        // Act
        var result = NameMapper.MapName(parameterName);

        // Assert
        Assert.AreEqual("username", result);
    }

    [TestMethod]
    public void MapName_WithPascalCase_ReturnsSnakeCase()
    {
        // Arrange
        var parameterName = "UserName";

        // Act
        var result = NameMapper.MapName(parameterName);

        // Assert
        Assert.AreEqual("user_name", result);
    }

    [TestMethod]
    public void MapName_WithCamelCase_ReturnsSnakeCase()
    {
        // Arrange
        var parameterName = "firstName";

        // Act
        var result = NameMapper.MapName(parameterName);

        // Assert
        Assert.AreEqual("first_name", result);
    }

    [TestMethod]
    public void MapName_WithMultipleWords_ReturnsSnakeCase()
    {
        // Arrange
        var parameterName = "UserFirstName";

        // Act
        var result = NameMapper.MapName(parameterName);

        // Assert
        Assert.AreEqual("user_first_name", result);
    }

    [TestMethod]
    public void MapName_WithLongCamelCase_ReturnsSnakeCase()
    {
        // Arrange
        var parameterName = "userAccountCreationDate";

        // Act
        var result = NameMapper.MapName(parameterName);

        // Assert
        Assert.AreEqual("user_account_creation_date", result);
    }

    [TestMethod]
    public void MapName_WithSpecialCharacters_ReturnsLowercase()
    {
        // Arrange
        var parameterName = "@UserName";

        // Act
        var result = NameMapper.MapName(parameterName);

        // Assert
        Assert.AreEqual("@username", result);
    }

    [TestMethod]
    public void MapName_WithHashPrefix_ReturnsLowercase()
    {
        // Arrange
        var parameterName = "#TempTable";

        // Act
        var result = NameMapper.MapName(parameterName);

        // Assert
        Assert.AreEqual("#temptable", result);
    }

    [TestMethod]
    public void MapName_WithUnderscores_ReturnsLowercase()
    {
        // Arrange
        var parameterName = "User_Name";

        // Act
        var result = NameMapper.MapName(parameterName);

        // Assert
        Assert.AreEqual("user__name", result);
    }

    [TestMethod]
    public void MapName_WithNumbers_HandlesCorrectly()
    {
        // Arrange
        var parameterName = "User123Name";

        // Act
        var result = NameMapper.MapName(parameterName);

        // Assert
        Assert.AreEqual("user123_name", result);
    }

    [TestMethod]
    public void MapName_WithNumbersAtEnd_HandlesCorrectly()
    {
        // Arrange
        var parameterName = "UserName123";

        // Act
        var result = NameMapper.MapName(parameterName);

        // Assert
        Assert.AreEqual("user_name123", result);
    }

    [TestMethod]
    public void MapName_WithAllUppercase_ReturnsLowercase()
    {
        // Arrange
        var parameterName = "USERNAME";

        // Act
        var result = NameMapper.MapName(parameterName);

        // Assert
        Assert.AreEqual("u_s_e_r_n_a_m_e", result);
    }

    [TestMethod]
    public void MapName_WithSingleCharacter_ReturnsLowercase()
    {
        // Arrange
        var parameterName = "A";

        // Act
        var result = NameMapper.MapName(parameterName);

        // Assert
        Assert.AreEqual("a", result);
    }

    [TestMethod]
    public void MapName_WithSingleLowercaseCharacter_ReturnsSame()
    {
        // Arrange
        var parameterName = "a";

        // Act
        var result = NameMapper.MapName(parameterName);

        // Assert
        Assert.AreEqual("a", result);
    }

    [TestMethod]
    public void MapName_WithEmptyString_ReturnsEmpty()
    {
        // Arrange
        var parameterName = string.Empty;

        // Act
        var result = NameMapper.MapName(parameterName);

        // Assert
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void MapName_WithNullParameter_ThrowsArgumentNullException()
    {
        // Arrange
        string? parameterName = null;

        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => NameMapper.MapName(parameterName!));
    }

    [TestMethod]
    public void MapName_WithMixedCaseAndNumbers_HandlesCorrectly()
    {
        // Arrange
        var parameterName = "user123Account456Name";

        // Act
        var result = NameMapper.MapName(parameterName);

        // Assert
        Assert.AreEqual("user123_account456_name", result);
    }

    [TestMethod]
    public void MapName_WithConsecutiveUppercase_HandlesCorrectly()
    {
        // Arrange
        var parameterName = "XMLHttpRequest";

        // Act
        var result = NameMapper.MapName(parameterName);

        // Assert
        Assert.AreEqual("x_m_l_http_request", result);
    }

    [TestMethod]
    public void MapName_WithDatabaseParameterName_ReturnsLowercase()
    {
        // Arrange
        var parameterName = "@UserID";

        // Act
        var result = NameMapper.MapName(parameterName);

        // Assert
        Assert.AreEqual("@userid", result);
    }

    [TestMethod]
    public void MapName_WithComplexSpecialCharacters_ReturnsLowercase()
    {
        // Arrange
        var parameterName = "@User$Name#123";

        // Act
        var result = NameMapper.MapName(parameterName);

        // Assert
        Assert.AreEqual("@user$name#123", result);
    }

    [TestMethod]
    public void MapName_WithOnlyNumbers_ReturnsLowercase()
    {
        // Arrange
        var parameterName = "123456";

        // Act
        var result = NameMapper.MapName(parameterName);

        // Assert
        Assert.AreEqual("123456", result);
    }

    [TestMethod]
    public void MapName_WithStartingNumber_HandlesCorrectly()
    {
        // Arrange
        var parameterName = "1stUserName";

        // Act
        var result = NameMapper.MapName(parameterName);

        // Assert
        Assert.AreEqual("1st_user_name", result);
    }

    [TestMethod]
    public void MapName_WithAcronyms_HandlesCorrectly()
    {
        // Arrange
        var parameterName = "HTTPSConnection";

        // Act
        var result = NameMapper.MapName(parameterName);

        // Assert
        Assert.AreEqual("h_t_t_p_s_connection", result);
    }

    [TestMethod]
    public void MapName_WithLongParameterName_HandlesCorrectly()
    {
        // Arrange
        var parameterName = "VeryLongParameterNameWithMultipleWordsAndNumbers123";

        // Act
        var result = NameMapper.MapName(parameterName);

        // Assert
        Assert.AreEqual("very_long_parameter_name_with_multiple_words_and_numbers123", result);
    }
}
