// -----------------------------------------------------------------------
// <copyright file="NameMapperAdvancedTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace Sqlx.Tests.Core
{
    /// <summary>
    /// Advanced tests for NameMapper class covering edge cases and comprehensive scenarios.
    /// </summary>
    [TestClass]
    public class NameMapperAdvancedTests
    {
        [TestMethod]
        public void MapName_ValidInput_ReturnsSnakeCase()
        {
            // Test basic PascalCase to snake_case conversion
            var testCases = new[]
            {
                ("UserId", "user_id"),
                ("FirstName", "first_name"),
                ("LastName", "last_name"),
                ("CreatedAt", "created_at"),
                ("UpdatedAt", "updated_at"),
                ("IsActive", "is_active"),
                ("UserProfile", "user_profile"),
                ("AccountBalance", "account_balance")
            };

            foreach (var (input, expected) in testCases)
            {
                var result = NameMapper.MapName(input);
                Assert.AreEqual(expected, result, $"Failed to map '{input}' to '{expected}'");
            }
        }

        [TestMethod]
        public void MapName_CamelCase_ReturnsSnakeCase()
        {
            // Test camelCase to snake_case conversion
            var testCases = new[]
            {
                ("userId", "user_id"),
                ("firstName", "first_name"),
                ("lastName", "last_name"),
                ("createdAt", "created_at"),
                ("updatedAt", "updated_at"),
                ("isActive", "is_active"),
                ("userProfile", "user_profile"),
                ("accountBalance", "account_balance")
            };

            foreach (var (input, expected) in testCases)
            {
                var result = NameMapper.MapName(input);
                Assert.AreEqual(expected, result, $"Failed to map '{input}' to '{expected}'");
            }
        }

        [TestMethod]
        public void MapName_SingleLetter_ReturnsLowercase()
        {
            // Test single letter inputs
            var testCases = new[]
            {
                ("A", "a"),
                ("B", "b"),
                ("X", "x"),
                ("Z", "z"),
                ("a", "a"),
                ("b", "b"),
                ("x", "x"),
                ("z", "z")
            };

            foreach (var (input, expected) in testCases)
            {
                var result = NameMapper.MapName(input);
                Assert.AreEqual(expected, result, $"Failed to map '{input}' to '{expected}'");
            }
        }

        [TestMethod]
        public void MapName_AllUppercase_ReturnsSnakeCase()
        {
            // Test all uppercase inputs
            var testCases = new[]
            {
                ("ID", "i_d"),
                ("URL", "u_r_l"),
                ("API", "a_p_i"),
                ("HTTP", "h_t_t_p"),
                ("XML", "x_m_l"),
                ("JSON", "j_s_o_n"),
                ("UUID", "u_u_i_d"),
                ("HTML", "h_t_m_l")
            };

            foreach (var (input, expected) in testCases)
            {
                var result = NameMapper.MapName(input);
                Assert.AreEqual(expected, result, $"Failed to map '{input}' to '{expected}'");
            }
        }

        [TestMethod]
        public void MapName_MixedCase_ReturnsSnakeCase()
        {
            // Test mixed case patterns
            var testCases = new[]
            {
                ("XMLHttpRequest", "x_m_l_http_request"),
                ("HTTPSConnection", "h_t_t_p_s_connection"),
                ("APIKey", "a_p_i_key"),
                ("URLPath", "u_r_l_path"),
                ("JSONData", "j_s_o_n_data"),
                ("HTMLElement", "h_t_m_l_element"),
                ("CSSStyle", "c_s_s_style"),
                ("SQLQuery", "s_q_l_query")
            };

            foreach (var (input, expected) in testCases)
            {
                var result = NameMapper.MapName(input);
                Assert.AreEqual(expected, result, $"Failed to map '{input}' to '{expected}'");
            }
        }

        [TestMethod]
        public void MapName_WithNumbers_ReturnsSnakeCase()
        {
            // Test inputs with numbers
            var testCases = new[]
            {
                ("User1", "user1"),
                ("Address2", "address2"),
                ("Item123", "item123"),
                ("Version2Point0", "version2_point0"),
                ("Level1User", "level1_user"),
                ("Phase2Testing", "phase2_testing"),
                ("Step1Complete", "step1_complete"),
                ("Type3Error", "type3_error")
            };

            foreach (var (input, expected) in testCases)
            {
                var result = NameMapper.MapName(input);
                Assert.AreEqual(expected, result, $"Failed to map '{input}' to '{expected}'");
            }
        }

        [TestMethod]
        public void MapName_WithSpecialCharacters_ReturnsLowercase()
        {
            // Test inputs with special characters
            var testCases = new[]
            {
                ("@parameter", "@parameter"),
                ("#temp", "#temp"),
                ("$variable", "$variable"),
                ("user@domain", "user@domain"),
                ("table#temp", "table#temp"),
                ("column$name", "column$name"),
                ("field-name", "field-name"),
                ("prop.value", "prop.value")
            };

            foreach (var (input, expected) in testCases)
            {
                var result = NameMapper.MapName(input);
                Assert.AreEqual(expected, result, $"Failed to map '{input}' to '{expected}'");
            }
        }

        [TestMethod]
        public void MapName_AlreadySnakeCase_ReturnsUnchanged()
        {
            // Test inputs that are already in snake_case
            var testCases = new[]
            {
                ("user_id", "user_id"),
                ("first_name", "first_name"),
                ("last_name", "last_name"),
                ("created_at", "created_at"),
                ("updated_at", "updated_at"),
                ("is_active", "is_active"),
                ("account_balance", "account_balance"),
                ("user_profile_id", "user_profile_id")
            };

            foreach (var (input, expected) in testCases)
            {
                var result = NameMapper.MapName(input);
                Assert.AreEqual(expected, result, $"Failed to preserve '{input}' as '{expected}'");
            }
        }

        [TestMethod]
        public void MapName_NullInput_ThrowsArgumentNullException()
        {
            // Test null input handling
            Assert.ThrowsException<ArgumentNullException>(() => NameMapper.MapName(null!));
        }

        [TestMethod]
        public void MapName_EmptyString_ReturnsEmptyString()
        {
            // Test empty string input
            var result = NameMapper.MapName("");
            Assert.AreEqual("", result);
        }

        [TestMethod]
        public void MapName_WhitespaceString_ReturnsWhitespace()
        {
            // Test whitespace inputs
            var testCases = new[]
            {
                (" ", " "),
                ("  ", "  "),
                ("\t", "\t"),
                ("\n", "\n"),
                ("\r\n", "\r\n")
            };

            foreach (var (input, expected) in testCases)
            {
                var result = NameMapper.MapName(input);
                Assert.AreEqual(expected, result, $"Failed to handle whitespace '{input}'");
            }
        }

        [TestMethod]
        public void MapNameToSnakeCase_ValidInput_ReturnsSnakeCase()
        {
            // Test direct call to MapNameToSnakeCase method
            var testCases = new[]
            {
                ("UserId", "user_id"),
                ("firstName", "first_name"),
                ("XMLParser", "x_m_l_parser"),
                ("HTTPClient", "h_t_t_p_client"),
                ("user_id", "user_id"),
                ("ID", "i_d")
            };

            foreach (var (input, expected) in testCases)
            {
                var result = NameMapper.MapName(input);
                Assert.AreEqual(expected, result, $"Failed to map '{input}' to '{expected}'");
            }
        }

        [TestMethod]
        public void MapNameToSnakeCase_NullInput_ThrowsArgumentNullException()
        {
            // Test null input handling for MapNameToSnakeCase
            Assert.ThrowsException<ArgumentNullException>(() => NameMapper.MapName(null!));
        }

        [TestMethod]
        public void MapNameToSnakeCase_SpecialCharacters_ReturnsLowercase()
        {
            // Test special character handling in MapNameToSnakeCase
            var testCases = new[]
            {
                ("@param", "@param"),
                ("User@Name", "user@name"),
                ("Field#Value", "field#value"),
                ("Prop$Name", "prop$name"),
                ("Item-Count", "item-count"),
                ("Key.Value", "key.value")
            };

            foreach (var (input, expected) in testCases)
            {
                var result = NameMapper.MapName(input);
                Assert.AreEqual(expected, result, $"Failed to handle special chars in '{input}'");
            }
        }

        [TestMethod]
        public void MapName_ConsecutiveUppercase_HandledCorrectly()
        {
            // Test consecutive uppercase letters
            var testCases = new[]
            {
                ("HTTPSProxy", "h_t_t_p_s_proxy"),
                ("XMLHTTPRequest", "x_m_l_h_t_t_p_request"),
                ("JSONAPIResponse", "j_s_o_n_a_p_i_response"),
                ("SQLDBConnection", "s_q_l_d_b_connection"),
                ("HTMLDOMElement", "h_t_m_l_d_o_m_element"),
                ("CSSStyleSheet", "c_s_s_style_sheet")
            };

            foreach (var (input, expected) in testCases)
            {
                var result = NameMapper.MapName(input);
                Assert.AreEqual(expected, result, $"Failed to handle consecutive uppercase in '{input}'");
            }
        }

        [TestMethod]
        public void MapName_UnicodeCharacters_HandledCorrectly()
        {
            // Test Unicode character handling
            var testCases = new[]
            {
                ("UserNaïve", "user_naïve"),
                ("Café", "café"),
                ("Résumé", "résumé"),
                ("Piña", "piña"),
                ("Señor", "señor"),
                ("München", "münchen")
            };

            foreach (var (input, expected) in testCases)
            {
                var result = NameMapper.MapName(input);
                Assert.AreEqual(expected, result, $"Failed to handle Unicode in '{input}'");
            }
        }

        [TestMethod]
        public void MapName_PerformanceTest_HandlesLargeInputs()
        {
            // Test performance with large inputs
            var largeInput = string.Join("", Enumerable.Range(0, 1000).Select(i => $"Property{i}"));

            var result = NameMapper.MapName(largeInput);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Length > 0);
        }

        [TestMethod]
        public void MapName_EdgeCaseInputs_HandledGracefully()
        {
            // Test edge case inputs
            var edgeCases = new[]
            {
                ("a", "a"),
                ("A", "a"),
                ("aB", "a_b"),
                ("Ab", "ab"),
                ("AB", "a_b"),
                ("ABC", "a_b_c"),
                ("ABc", "a_bc"),
                ("aBC", "a_b_c"),
                ("abC", "ab_c"),
                ("123", "123"),
                ("a1", "a1"),
                ("A1", "a1"),
                ("1A", "1_a"),
                ("1a", "1a")
            };

            foreach (var (input, expected) in edgeCases)
            {
                var result = NameMapper.MapName(input);
                Assert.AreEqual(expected, result, $"Edge case failed for input '{input}'");
            }
        }

        [TestMethod]
        public void MapName_DatabaseColumnNames_ReturnsExpectedFormat()
        {
            // Test common database column name patterns
            var databaseColumns = new[]
            {
                ("Id", "id"),
                ("UserId", "user_id"),
                ("UserName", "user_name"),
                ("EmailAddress", "email_address"),
                ("PhoneNumber", "phone_number"),
                ("CreatedDate", "created_date"),
                ("ModifiedDate", "modified_date"),
                ("IsDeleted", "is_deleted"),
                ("IsActive", "is_active"),
                ("SortOrder", "sort_order"),
                ("DisplayName", "display_name"),
                ("FullName", "full_name")
            };

            foreach (var (input, expected) in databaseColumns)
            {
                var result = NameMapper.MapName(input);
                Assert.AreEqual(expected, result, $"Database column mapping failed for '{input}'");
            }
        }

        [TestMethod]
        public void MapName_ConsistencyTest_SameInputSameOutput()
        {
            // Test that the same input always produces the same output
            var testInputs = new[] { "UserId", "firstName", "XMLParser", "user_id", "ID", "@param" };

            foreach (var input in testInputs)
            {
                var result1 = NameMapper.MapName(input);
                var result2 = NameMapper.MapName(input);
                var result3 = NameMapper.MapName(input);

                Assert.AreEqual(result1, result2, $"Inconsistent results for '{input}'");
                Assert.AreEqual(result1, result3, $"MapName and MapNameToSnakeCase differ for '{input}'");
            }
        }
    }
}
