// -----------------------------------------------------------------------
// <copyright file="TestHelper.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Helper class for unit tests with common utilities and test data.
/// </summary>
public static class TestHelper
{
    /// <summary>
    /// Common test data for name mapping tests.
    /// </summary>
    public static readonly (string Input, string Expected)[] NameMappingTestCases =
    {
        ("personId", "person_id"),
        ("name", "name"),
        ("Name", "name"),
        ("PersonId", "person_id"),
        ("firstName", "first_name"),
        ("FirstName", "first_name"),
        ("userId", "user_id"),
        ("UserId", "user_id"),
        ("ID", "id"),
        ("URL", "url"),
        ("API", "api"),
        ("isActive", "is_active"),
        ("IsActive", "is_active"),
        ("createdAt", "created_at"),
        ("CreatedAt", "created_at"),
        ("updatedAt", "updated_at"),
        ("UpdatedAt", "updated_at"),
        (string.Empty, string.Empty),
        ("a", "a"),
        ("A", "a"),
        ("camelCase", "camel_case"),
        ("CamelCase", "camel_case"),
        ("PascalCase", "pascal_case"),
    };

    /// <summary>
    /// SQL define test cases for different database types.
    /// </summary>
    public static readonly (string Input, string MySqlExpected, string SqlServerExpected, string PostgreSqlExpected)[] SqlDefineTestCases =
    {
        ("columnName", "`columnName`", "[columnName]", "\"columnName\""),
        ("user_id", "`user_id`", "[user_id]", "\"user_id\""),
        (string.Empty, "``", "[]", "\"\""),
        ("table", "`table`", "[table]", "\"table\""),
    };

    /// <summary>
    /// Validates that two strings are equal with detailed error message.
    /// </summary>
    /// <param name="expected">The expected string value.</param>
    /// <param name="actual">The actual string value.</param>
    /// <param name="message">Optional custom message.</param>
    public static void AssertStringEqual(string expected, string actual, string? message = null)
    {
        if (string.IsNullOrEmpty(message))
        {
            message = $"Expected: '{expected}', Actual: '{actual}'";
        }

        Assert.AreEqual(expected, actual, message);
    }

    /// <summary>
    /// Validates that a string is not null or empty.
    /// </summary>
    /// <param name="value">The string value to check.</param>
    /// <param name="parameterName">The parameter name for error messages.</param>
    public static void AssertNotNullOrEmpty(string value, string parameterName = "value")
    {
        Assert.IsNotNull(value, $"{parameterName} should not be null");
        Assert.IsTrue(value.Length > 0, $"{parameterName} should not be empty");
    }
}
