// -----------------------------------------------------------------------
// <copyright file="NameMapperTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.Tests;

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

[TestClass]
public class NameMapperTests
{
    [DataTestMethod]
    [DataRow("personId", "person_id")]
    [DataRow("name", "name")]
    [DataRow("Name", "name")]
    [DataRow("PersonId", "person_id")]
    [DataRow("", "")]
    [DataRow("a", "a")]
    [DataRow("A", "a")]
    [DataRow("userName", "user_name")]
    [DataRow("UserName", "user_name")]
    [DataRow("firstName", "first_name")]
    [DataRow("FirstName", "first_name")]
    [DataRow("lastName", "last_name")]
    [DataRow("LastName", "last_name")]
    [DataRow("emailAddress", "email_address")]
    [DataRow("EmailAddress", "email_address")]
    [DataRow("phoneNumber", "phone_number")]
    [DataRow("PhoneNumber", "phone_number")]
    [DataRow("isActive", "is_active")]
    [DataRow("IsActive", "is_active")]
    [DataRow("hasPermission", "has_permission")]
    [DataRow("HasPermission", "has_permission")]
    public void MapName_VariousInputs_ReturnsExpectedOutput(string parameterName, string expectedStoredProcedureParameter)
    {
        var storedProcedureParameter = NameMapper.MapName(parameterName);

        Assert.AreEqual(expectedStoredProcedureParameter, storedProcedureParameter);
    }

    /// <summary>
    /// Tests edge case with single uppercase letter.
    /// </summary>
    [TestMethod]
    public void MapName_SingleUppercaseLetter_ReturnsLowercase()
    {
        var result = NameMapper.MapName("A");
        Assert.AreEqual("a", result);
    }

    /// <summary>
    /// Tests edge case with single lowercase letter.
    /// </summary>
    [TestMethod]
    public void MapName_SingleLowercaseLetter_ReturnsSame()
    {
        var result = NameMapper.MapName("a");
        Assert.AreEqual("a", result);
    }

    /// <summary>
    /// Tests edge case with empty string.
    /// </summary>
    [TestMethod]
    public void MapName_EmptyString_ReturnsEmptyString()
    {
        var result = NameMapper.MapName("");
        Assert.AreEqual("", result);
    }

    /// <summary>
    /// Tests complex camelCase scenario.
    /// </summary>
    [TestMethod]
    public void MapName_ComplexCamelCase_ReturnsCorrectFormat()
    {
        var result = NameMapper.MapName("veryLongCamelCaseName");
        Assert.AreEqual("very_long_camel_case_name", result);
    }

    /// <summary>
    /// Tests edge case with numbers in the name.
    /// </summary>
    [TestMethod]
    public void MapName_WithNumbers_HandlesCorrectly()
    {
        var result = NameMapper.MapName("user123Name");
        Assert.AreEqual("user123_name", result);
    }

    /// <summary>
    /// Tests edge case with consecutive uppercase letters.
    /// </summary>
    [TestMethod]
    public void MapName_ConsecutiveUppercase_HandlesCorrectly()
    {
        var result = NameMapper.MapName("userID");
        Assert.AreEqual("user_i_d", result);
    }

    /// <summary>
    /// Tests edge case with all uppercase letters.
    /// </summary>
    [TestMethod]
    public void MapName_AllUppercase_HandlesCorrectly()
    {
        var result = NameMapper.MapName("USERNAME");
        Assert.AreEqual("u_s_e_r_n_a_m_e", result);
    }

    /// <summary>
    /// Tests edge case with mixed case and numbers.
    /// </summary>
    [TestMethod]
    public void MapName_MixedCaseAndNumbers_HandlesCorrectly()
    {
        var result = NameMapper.MapName("user123Name456");
        Assert.AreEqual("user123_name456", result);
    }

    /// <summary>
    /// Tests edge case with special characters (should be treated as lowercase).
    /// </summary>
    [TestMethod]
    public void MapName_WithSpecialCharacters_HandlesCorrectly()
    {
        var result = NameMapper.MapName("user@name");
        Assert.AreEqual("user@name", result);
    }

    /// <summary>
    /// Tests edge case with underscores already present.
    /// </summary>
    [TestMethod]
    public void MapName_WithUnderscores_HandlesCorrectly()
    {
        var result = NameMapper.MapName("user_name");
        Assert.AreEqual("user_name", result);
    }

    /// <summary>
    /// Tests edge case with hyphens (should be treated as lowercase).
    /// </summary>
    [TestMethod]
    public void MapName_WithHyphens_HandlesCorrectly()
    {
        var result = NameMapper.MapName("user-name");
        Assert.AreEqual("user-name", result);
    }

    /// <summary>
    /// Tests edge case with spaces (should be treated as lowercase).
    /// </summary>
    [TestMethod]
    public void MapName_WithSpaces_HandlesCorrectly()
    {
        var result = NameMapper.MapName("user name");
        Assert.AreEqual("user name", result);
    }

    /// <summary>
    /// Tests edge case with leading uppercase letter.
    /// </summary>
    [TestMethod]
    public void MapName_LeadingUppercase_HandlesCorrectly()
    {
        var result = NameMapper.MapName("UserName");
        Assert.AreEqual("user_name", result);
    }

    /// <summary>
    /// Tests edge case with trailing uppercase letter.
    /// </summary>
    [TestMethod]
    public void MapName_TrailingUppercase_HandlesCorrectly()
    {
        var result = NameMapper.MapName("userNameA");
        Assert.AreEqual("user_name_a", result);
    }

    /// <summary>
    /// Tests edge case with single uppercase letter in middle.
    /// </summary>
    [TestMethod]
    public void MapName_SingleUppercaseInMiddle_HandlesCorrectly()
    {
        var result = NameMapper.MapName("userName");
        Assert.AreEqual("user_name", result);
    }

    /// <summary>
    /// Tests edge case with multiple consecutive uppercase letters.
    /// </summary>
    [TestMethod]
    public void MapName_MultipleConsecutiveUppercase_HandlesCorrectly()
    {
        var result = NameMapper.MapName("userIDName");
        Assert.AreEqual("user_i_d_name", result);
    }

    /// <summary>
    /// Tests edge case with numbers at the beginning.
    /// </summary>
    [TestMethod]
    public void MapName_NumbersAtBeginning_HandlesCorrectly()
    {
        var result = NameMapper.MapName("123UserName");
        Assert.AreEqual("123_user_name", result);
    }

    /// <summary>
    /// Tests edge case with numbers at the end.
    /// </summary>
    [TestMethod]
    public void MapName_NumbersAtEnd_HandlesCorrectly()
    {
        var result = NameMapper.MapName("UserName123");
        Assert.AreEqual("user_name123", result);
    }

    /// <summary>
    /// Tests edge case with mixed case and special characters.
    /// </summary>
    [TestMethod]
    public void MapName_MixedCaseAndSpecialCharacters_HandlesCorrectly()
    {
        var result = NameMapper.MapName("user@Name#123");
        Assert.AreEqual("user@name#123", result);
    }

    /// <summary>
    /// Tests edge case with only numbers.
    /// </summary>
    [TestMethod]
    public void MapName_OnlyNumbers_HandlesCorrectly()
    {
        var result = NameMapper.MapName("123");
        Assert.AreEqual("123", result);
    }

    /// <summary>
    /// Tests edge case with only special characters.
    /// </summary>
    [TestMethod]
    public void MapName_OnlySpecialCharacters_HandlesCorrectly()
    {
        var result = NameMapper.MapName("@#$");
        Assert.AreEqual("@#$", result);
    }

    /// <summary>
    /// Tests edge case with null input (should throw ArgumentNullException).
    /// </summary>
    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void MapName_NullInput_ThrowsArgumentNullException()
    {
        NameMapper.MapName(null);
    }

    /// <summary>
    /// Tests edge case with null input (should throw ArgumentNullException) - alternative approach.
    /// </summary>
    [TestMethod]
    public void MapName_NullInput_ThrowsArgumentNullException_Alternative()
    {
        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentNullException>(() => NameMapper.MapName(null));
        Assert.AreEqual("parameterName", exception.ParamName);
    }

    /// <summary>
    /// Tests edge case with null input (should throw ArgumentNullException) - using try-catch.
    /// </summary>
    [TestMethod]
    public void MapName_NullInput_ThrowsArgumentNullException_TryCatch()
    {
        // Arrange
        string? nullInput = null;

        // Act & Assert
        try
        {
            NameMapper.MapName(nullInput);
            Assert.Fail("Should have thrown ArgumentNullException");
        }
        catch (ArgumentNullException ex)
        {
            Assert.AreEqual("parameterName", ex.ParamName);
        }
    }

    /// <summary>
    /// Tests edge case with null input (should throw ArgumentNullException) - using Assert.ThrowsException.
    /// </summary>
    [TestMethod]
    public void MapName_NullInput_ThrowsArgumentNullException_AssertThrows()
    {
        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentNullException>(() => NameMapper.MapName(null));
        Assert.AreEqual("parameterName", exception.ParamName);
    }
}
