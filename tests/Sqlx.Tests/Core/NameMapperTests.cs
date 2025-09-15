// -----------------------------------------------------------------------
// <copyright file="NameMapperTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.Tests.Core;

using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests for NameMapper functionality.
/// Tests parameter name mapping and snake_case conversion.
/// </summary>
[TestClass]
public class NameMapperTests
{
    /// <summary>
    /// Tests MapName method with various naming patterns.
    /// </summary>
    [TestMethod]
    public void NameMapper_MapName_ConvertsPascalCaseToSnakeCase()
    {
        // Test basic PascalCase conversion
        Assert.AreEqual("user_id", NameMapper.MapName("UserId"),
            "Should convert PascalCase to snake_case");
        Assert.AreEqual("first_name", NameMapper.MapName("FirstName"),
            "Should convert FirstName to snake_case");
        Assert.AreEqual("email_address", NameMapper.MapName("EmailAddress"),
            "Should convert EmailAddress to snake_case");
        Assert.AreEqual("created_date_time", NameMapper.MapName("CreatedDateTime"),
            "Should convert CreatedDateTime to snake_case");

        // Test camelCase conversion
        Assert.AreEqual("user_id", NameMapper.MapName("userId"),
            "Should convert camelCase to snake_case");
        Assert.AreEqual("first_name", NameMapper.MapName("firstName"),
            "Should convert firstName to snake_case");
        Assert.AreEqual("email_address", NameMapper.MapName("emailAddress"),
            "Should convert emailAddress to snake_case");

        // Test single words
        Assert.AreEqual("name", NameMapper.MapName("Name"),
            "Should convert single PascalCase word");
        Assert.AreEqual("name", NameMapper.MapName("name"),
            "Should handle single camelCase word");
        Assert.AreEqual("id", NameMapper.MapName("Id"),
            "Should convert Id to lowercase");
        Assert.AreEqual("i_d", NameMapper.MapName("ID"),
            "Should convert ID to i_d");
    }

    /// <summary>
    /// Tests MapName with special characters and edge cases.
    /// </summary>
    [TestMethod]
    public void NameMapper_MapName_HandlesSpecialCharacters()
    {
        // Test with database parameter prefixes
        Assert.AreEqual("@userid", NameMapper.MapName("@UserId"),
            "Should convert parameter with @ prefix to lowercase");
        Assert.AreEqual("#tempvalue", NameMapper.MapName("#TempValue"),
            "Should convert parameter with # prefix to lowercase");
        Assert.AreEqual("$paramname", NameMapper.MapName("$ParamName"),
            "Should convert parameter with $ prefix to lowercase");

        // Test with underscores already present
        Assert.AreEqual("user__id__value", NameMapper.MapName("User_Id_Value"),
            "Should handle existing underscores");
        Assert.AreEqual("__private_field", NameMapper.MapName("_PrivateField"),
            "Should handle leading underscore");

        // Test with numbers
        Assert.AreEqual("user2_name", NameMapper.MapName("User2Name"),
            "Should handle numbers in name");
        Assert.AreEqual("address1", NameMapper.MapName("Address1"),
            "Should handle trailing numbers");
        Assert.AreEqual("item_id2", NameMapper.MapName("ItemId2"),
            "Should handle numbers at end");
    }

    /// <summary>
    /// Tests MapNameToSnakeCase method directly.
    /// </summary>
    [TestMethod]
    public void NameMapper_MapNameToSnakeCase_ConvertsCorrectly()
    {
        // Test comprehensive snake_case conversion
        Assert.AreEqual("simple", NameMapper.MapNameToSnakeCase("Simple"),
            "Should convert simple word");
        Assert.AreEqual("user_name", NameMapper.MapNameToSnakeCase("UserName"),
            "Should convert two words");
        Assert.AreEqual("user_profile_image", NameMapper.MapNameToSnakeCase("UserProfileImage"),
            "Should convert multiple words");
        Assert.AreEqual("x_m_l_http_request", NameMapper.MapNameToSnakeCase("XMLHttpRequest"),
            "Should handle acronyms");

        // Test with existing lowercase
        Assert.AreEqual("already_lowercase", NameMapper.MapNameToSnakeCase("already_lowercase"),
            "Should handle already snake_case");
        Assert.AreEqual("mixed_case_string", NameMapper.MapNameToSnakeCase("mixedCaseString"),
            "Should convert mixed case");

        // Test edge cases
        Assert.AreEqual("a", NameMapper.MapNameToSnakeCase("A"),
            "Should convert single letter");
        Assert.AreEqual("a_b", NameMapper.MapNameToSnakeCase("AB"),
            "Should convert two letters");
        Assert.AreEqual("a_b_c", NameMapper.MapNameToSnakeCase("ABC"),
            "Should convert three letters");
    }

    /// <summary>
    /// Tests naming conventions for database compatibility.
    /// </summary>
    [TestMethod]
    public void NameMapper_DatabaseCompatibility_FollowsConventions()
    {
        // Test common database field patterns
        var testCases = new[]
        {
            ("UserId", "user_id"),
            ("CustomerId", "customer_id"),
            ("OrderId", "order_id"),
            ("FirstName", "first_name"),
            ("LastName", "last_name"),
            ("EmailAddress", "email_address"),
            ("PhoneNumber", "phone_number"),
            ("CreatedAt", "created_at"),
            ("UpdatedAt", "updated_at"),
            ("IsActive", "is_active"),
            ("IsDeleted", "is_deleted"),
            ("DateOfBirth", "date_of_birth"),
            ("AccountBalance", "account_balance"),
            ("TotalAmount", "total_amount")
        };

        foreach (var (input, expected) in testCases)
        {
            var result = NameMapper.MapName(input);
            Assert.AreEqual(expected, result,
                $"Database field mapping for '{input}' should follow conventions");
        }
    }

    /// <summary>
    /// Tests handling of complex naming scenarios.
    /// </summary>
    [TestMethod]
    public void NameMapper_ComplexScenarios_HandledCorrectly()
    {
        // Test API and HTTP related terms
        Assert.AreEqual("a_p_i_key", NameMapper.MapName("APIKey"),
            "Should handle API acronym");
        Assert.AreEqual("h_t_t_p_response", NameMapper.MapName("HTTPResponse"),
            "Should handle HTTP acronym");
        Assert.AreEqual("u_r_l_path", NameMapper.MapName("URLPath"),
            "Should handle URL acronym");
        Assert.AreEqual("j_s_o_n_data", NameMapper.MapName("JSONData"),
            "Should handle JSON acronym");

        // Test business domain terms
        Assert.AreEqual("customer_billing_address", NameMapper.MapName("CustomerBillingAddress"),
            "Should handle long compound names");
        Assert.AreEqual("product_category_name", NameMapper.MapName("ProductCategoryName"),
            "Should handle business terms");
        Assert.AreEqual("order_line_item_quantity", NameMapper.MapName("OrderLineItemQuantity"),
            "Should handle very long names");

        // Test technical terms
        Assert.AreEqual("database_connection_string", NameMapper.MapName("DatabaseConnectionString"),
            "Should handle technical terms");
        Assert.AreEqual("encryption_algorithm_type", NameMapper.MapName("EncryptionAlgorithmType"),
            "Should handle algorithm names");
    }

    /// <summary>
    /// Tests null and empty string handling.
    /// </summary>
    [TestMethod]
    public void NameMapper_NullAndEmpty_ThrowsAppropriateExceptions()
    {
        // Test null parameter
        Assert.ThrowsException<ArgumentNullException>(() => NameMapper.MapName(null!),
            "Should throw ArgumentNullException for null parameter");
        Assert.ThrowsException<ArgumentNullException>(() => NameMapper.MapNameToSnakeCase(null!),
            "Should throw ArgumentNullException for null parameter in MapNameToSnakeCase");

        // Test empty string
        Assert.AreEqual("", NameMapper.MapName(""),
            "Should handle empty string gracefully");
        Assert.AreEqual("", NameMapper.MapNameToSnakeCase(""),
            "Should handle empty string in MapNameToSnakeCase");

        // Test whitespace
        Assert.AreEqual(" ", NameMapper.MapName(" "),
            "Should handle whitespace");
        Assert.AreEqual("  ", NameMapper.MapName("  "),
            "Should handle multiple spaces");
    }

    /// <summary>
    /// Tests performance with many conversions.
    /// </summary>
    [TestMethod]
    public void NameMapper_Performance_HandlesManyCalls()
    {
        var testNames = new[]
        {
            "UserId", "FirstName", "LastName", "EmailAddress", "PhoneNumber",
            "CreatedDateTime", "UpdatedDateTime", "IsActive", "IsDeleted",
            "CustomerBillingAddress", "ProductCategoryName", "OrderLineItemQuantity"
        };

        var startTime = DateTime.UtcNow;

        // Perform many name mappings to test performance
        for (int i = 0; i < 1000; i++)
        {
            foreach (var name in testNames)
            {
                var mapped = NameMapper.MapName(name);
                var snakeCase = NameMapper.MapNameToSnakeCase(name);

                // Use results to ensure calls aren't optimized away
                Assert.IsNotNull(mapped, "Mapped name should not be null");
                Assert.IsNotNull(snakeCase, "Snake case name should not be null");
            }
        }

        var endTime = DateTime.UtcNow;
        var elapsed = endTime - startTime;

        Assert.IsTrue(elapsed.TotalSeconds < 5,
            $"Many name mappings should complete quickly. Took: {elapsed.TotalSeconds} seconds");
    }

    /// <summary>
    /// Tests consistency between MapName and MapNameToSnakeCase.
    /// </summary>
    [TestMethod]
    public void NameMapper_Consistency_BetweenMethods()
    {
        var testNames = new[]
        {
            "UserId", "FirstName", "EmailAddress", "CreatedDateTime",
            "userName", "emailAddress", "createdAt", "isActive"
        };

        foreach (var name in testNames)
        {
            var mapNameResult = NameMapper.MapName(name);
            var mapNameToSnakeCaseResult = NameMapper.MapNameToSnakeCase(name);

            Assert.AreEqual(mapNameToSnakeCaseResult, mapNameResult,
                $"MapName and MapNameToSnakeCase should return same result for '{name}'");
        }
    }

    /// <summary>
    /// Tests reversibility of common patterns.
    /// </summary>
    [TestMethod]
    public void NameMapper_CommonPatterns_AreConsistent()
    {
        // Test that similar patterns produce similar results
        var patterns = new[]
        {
            ("Id", "id"),
            ("Name", "name"),
            ("Email", "email"),
            ("UserId", "user_id"),
            ("UserName", "user_name"),
            ("UserEmail", "user_email")
        };

        foreach (var (input, expectedOutput) in patterns)
        {
            var result = NameMapper.MapName(input);
            Assert.AreEqual(expectedOutput, result,
                $"Pattern '{input}' should consistently map to '{expectedOutput}'");
        }

        // Test that adding prefixes to the same base produces consistent patterns
        var baseWord = "User";
        var userResult = NameMapper.MapName(baseWord);

        var userId = NameMapper.MapName(baseWord + "Id");
        var userName = NameMapper.MapName(baseWord + "Name");
        var userEmail = NameMapper.MapName(baseWord + "Email");

        Assert.IsTrue(userId.StartsWith(userResult),
            "UserId should start with the mapped base word");
        Assert.IsTrue(userName.StartsWith(userResult),
            "UserName should start with the mapped base word");
        Assert.IsTrue(userEmail.StartsWith(userResult),
            "UserEmail should start with the mapped base word");
    }

    /// <summary>
    /// Tests handling of already snake_case names.
    /// </summary>
    [TestMethod]
    public void NameMapper_AlreadySnakeCase_PreservedCorrectly()
    {
        var snakeCaseNames = new[]
        {
            "user_id",
            "first_name",
            "email_address",
            "created_at",
            "is_active",
            "order_line_item"
        };

        foreach (var snakeCaseName in snakeCaseNames)
        {
            var result = NameMapper.MapName(snakeCaseName);
            Assert.AreEqual(snakeCaseName, result,
                $"Already snake_case name '{snakeCaseName}' should be preserved");
        }
    }

    /// <summary>
    /// Tests SQL parameter prefix handling.
    /// </summary>
    [TestMethod]
    public void NameMapper_SqlParameterPrefixes_HandledCorrectly()
    {
        // Test different SQL parameter prefix styles
        var parameterCases = new[]
        {
            ("@UserId", "@userid"),
            ("@FirstName", "@firstname"),
            ("#TempTable", "#temptable"),
            ("$PostgresParam", "$postgresparam"),
            (":OracleParam", ":oracleparam")
        };

        foreach (var (input, expected) in parameterCases)
        {
            var result = NameMapper.MapName(input);
            Assert.AreEqual(expected, result,
                $"SQL parameter '{input}' should be mapped to '{expected}'");
        }
    }
}
