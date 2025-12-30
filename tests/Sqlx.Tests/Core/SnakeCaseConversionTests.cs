// -----------------------------------------------------------------------
// <copyright file="SnakeCaseConversionTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Generator;

namespace Sqlx.Tests.Core;

/// <summary>
/// Tests for snake_case conversion functionality in shared utilities.
/// </summary>
[TestClass]
public class SnakeCaseConversionTests
{
    [TestMethod]
    public void ConvertToSnakeCase_PascalCase_ConvertsCorrectly()
    {
        // Arrange & Act & Assert
        Assert.AreEqual("user_id", SharedCodeGenerationUtilities.ConvertToSnakeCase("UserId"));
        Assert.AreEqual("department_name", SharedCodeGenerationUtilities.ConvertToSnakeCase("DepartmentName"));
        Assert.AreEqual("is_active", SharedCodeGenerationUtilities.ConvertToSnakeCase("IsActive"));
        Assert.AreEqual("created_at", SharedCodeGenerationUtilities.ConvertToSnakeCase("CreatedAt"));
        Assert.AreEqual("last_modified_date", SharedCodeGenerationUtilities.ConvertToSnakeCase("LastModifiedDate"));
    }

    [TestMethod]
    public void ConvertToSnakeCase_CamelCase_ConvertsCorrectly()
    {
        // Arrange & Act & Assert
        Assert.AreEqual("user_name", SharedCodeGenerationUtilities.ConvertToSnakeCase("userName"));
        Assert.AreEqual("email_address", SharedCodeGenerationUtilities.ConvertToSnakeCase("emailAddress"));
        Assert.AreEqual("is_verified", SharedCodeGenerationUtilities.ConvertToSnakeCase("isVerified"));
    }

    [TestMethod]
    public void ConvertToSnakeCase_SingleWord_ConvertsToLowerCase()
    {
        // Arrange & Act & Assert
        Assert.AreEqual("id", SharedCodeGenerationUtilities.ConvertToSnakeCase("Id"));
        Assert.AreEqual("name", SharedCodeGenerationUtilities.ConvertToSnakeCase("Name"));
        Assert.AreEqual("email", SharedCodeGenerationUtilities.ConvertToSnakeCase("Email"));
        Assert.AreEqual("age", SharedCodeGenerationUtilities.ConvertToSnakeCase("Age"));
    }

    [TestMethod]
    public void ConvertToSnakeCase_AlreadySnakeCase_ReturnsAsIs()
    {
        // Arrange & Act & Assert
        Assert.AreEqual("user_id", SharedCodeGenerationUtilities.ConvertToSnakeCase("user_id"));
        Assert.AreEqual("department_name", SharedCodeGenerationUtilities.ConvertToSnakeCase("department_name"));
        Assert.AreEqual("is_active", SharedCodeGenerationUtilities.ConvertToSnakeCase("is_active"));
    }

    [TestMethod]
    public void ConvertToSnakeCase_AllCapsWithUnderscores_ConvertsToLowerCase()
    {
        // Arrange & Act & Assert
        Assert.AreEqual("user_id", SharedCodeGenerationUtilities.ConvertToSnakeCase("USER_ID"));
        Assert.AreEqual("max_length", SharedCodeGenerationUtilities.ConvertToSnakeCase("MAX_LENGTH"));
        Assert.AreEqual("default_value", SharedCodeGenerationUtilities.ConvertToSnakeCase("DEFAULT_VALUE"));
    }

    [TestMethod]
    public void ConvertToSnakeCase_ConsecutiveCapitals_HandlesCorrectly()
    {
        // Arrange & Act & Assert
        Assert.AreEqual("htmlcontent", SharedCodeGenerationUtilities.ConvertToSnakeCase("HTMLContent"));
        Assert.AreEqual("xmldata", SharedCodeGenerationUtilities.ConvertToSnakeCase("XMLData"));
        Assert.AreEqual("apikey", SharedCodeGenerationUtilities.ConvertToSnakeCase("APIKey"));
    }

    [TestMethod]
    public void ConvertToSnakeCase_WithNumbers_HandlesCorrectly()
    {
        // Arrange & Act & Assert
        Assert.AreEqual("user_id2", SharedCodeGenerationUtilities.ConvertToSnakeCase("UserId2"));
        Assert.AreEqual("field1_name", SharedCodeGenerationUtilities.ConvertToSnakeCase("Field1Name"));
        Assert.AreEqual("version2_0", SharedCodeGenerationUtilities.ConvertToSnakeCase("Version2_0"));
    }

    [TestMethod]
    public void ConvertToSnakeCase_EmptyOrNull_HandlesGracefully()
    {
        // Arrange & Act & Assert
        Assert.AreEqual("", SharedCodeGenerationUtilities.ConvertToSnakeCase(""));
        // ConvertToSnakeCase should handle null by returning empty string or the input
        var result = SharedCodeGenerationUtilities.ConvertToSnakeCase(null!);
        Assert.IsTrue(string.IsNullOrEmpty(result) || result == null, "Should handle null gracefully");
    }

    [TestMethod]
    public void ConvertToSnakeCase_ComplexCases_HandlesCorrectly()
    {
        // Arrange & Act & Assert
        Assert.AreEqual("performance_rating", SharedCodeGenerationUtilities.ConvertToSnakeCase("PerformanceRating"));
        Assert.AreEqual("hire_date", SharedCodeGenerationUtilities.ConvertToSnakeCase("HireDate"));
        Assert.AreEqual("department_id", SharedCodeGenerationUtilities.ConvertToSnakeCase("DepartmentId"));
        Assert.AreEqual("user_profile_image_url", SharedCodeGenerationUtilities.ConvertToSnakeCase("UserProfileImageUrl"));
    }

    [TestMethod]
    public void ConvertToSnakeCase_DatabaseColumnExamples_MatchesExpectedOutput()
    {
        // Test real-world database column name conversions
        var testCases = new[]
        {
            ("Id", "id"),
            ("Name", "name"),
            ("Email", "email"),
            ("Age", "age"),
            ("Salary", "salary"),
            ("DepartmentId", "department_id"),
            ("IsActive", "is_active"),
            ("HireDate", "hire_date"),
            ("Bonus", "bonus"),
            ("PerformanceRating", "performance_rating"),
            ("CreatedAt", "created_at"),
            ("UpdatedAt", "updated_at"),
            ("FirstName", "first_name"),
            ("LastName", "last_name")
        };

        foreach (var (input, expected) in testCases)
        {
            var actual = SharedCodeGenerationUtilities.ConvertToSnakeCase(input);
            Assert.AreEqual(expected, actual, $"Failed for input: {input}");
        }
    }
}
