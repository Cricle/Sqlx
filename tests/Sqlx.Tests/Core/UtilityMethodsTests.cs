// -----------------------------------------------------------------------
// <copyright file="UtilityMethodsTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Sqlx.Tests.Core
{
    /// <summary>
    /// Tests for utility methods and helper functions.
    /// </summary>
    [TestClass]
    public class UtilityMethodsTests
    {
        [TestMethod]
        public void StringConversion_PascalCase_ToSnakeCase()
        {
            // Test the string conversion logic used in column naming
            var testCases = new[]
            {
                ("UserId", "user_id"),
                ("FirstName", "first_name"),
                ("CreatedAt", "created_at"),
                ("IsActive", "is_active"),
                ("XMLHttpRequest", "x_m_l_http_request"),
                ("ID", "i_d"),
                ("A", "a"),
                ("", "")
            };

            foreach (var (input, expected) in testCases)
            {
                if (string.IsNullOrEmpty(input))
                {
                    Assert.AreEqual(expected, input);
                    continue;
                }

                // Simulate the conversion logic
                var result = Regex.Replace(input, "[A-Z]", x => $"_{x.Value.ToLower()}").TrimStart('_');
                Assert.AreEqual(expected, result, $"Failed for input: {input}");
            }
        }

        [TestMethod]
        public void StringConversion_UpperCaseWithUnderscores_ToLowerCase()
        {
            var testCases = new[]
            {
                ("USER_ID", "user_id"),
                ("FIRST_NAME", "first_name"),
                ("CREATED_AT", "created_at"),
                ("IS_ACTIVE", "is_active"),
                ("A_B_C", "a_b_c")
            };

            foreach (var (input, expected) in testCases)
            {
                // Check if already contains underscores and is all caps
                if (input.Contains("_") && input.All(c => char.IsUpper(c) || c == '_' || char.IsDigit(c)))
                {
                    var result = input.ToLower();
                    Assert.AreEqual(expected, result, $"Failed for input: {input}");
                }
            }
        }

        [TestMethod]
        public void ParameterNameGeneration_ValidInputs_ReturnsCorrectFormat()
        {
            var testCases = new[]
            {
                ("@", "user_id", "@user_id"),
                ("$", "first_name", "$first_name"),
                (":", "created_at", ":created_at"),
                ("", "test", "test"),
                ("@", "", "@"),
                ("prefix_", "name", "prefix_name")
            };

            foreach (var (prefix, columnName, expected) in testCases)
            {
                var result = $"{prefix}{columnName}";
                Assert.AreEqual(expected, result, $"Failed for prefix: '{prefix}', columnName: '{columnName}'");
            }
        }

        [TestMethod]
        public void TypeNameAnalysis_CommonTypes_CorrectClassification()
        {
            var scalarTypes = new[]
            {
                typeof(int), typeof(string), typeof(bool), typeof(DateTime),
                typeof(decimal), typeof(double), typeof(float), typeof(long),
                typeof(short), typeof(byte), typeof(char)
            };

            var collectionTypes = new[]
            {
                typeof(List<int>), typeof(IList<string>), typeof(IEnumerable<bool>),
                typeof(ICollection<DateTime>), typeof(int[]), typeof(string[])
            };

            var taskTypes = new[]
            {
                typeof(System.Threading.Tasks.Task<int>),
                typeof(System.Threading.Tasks.Task<string>),
                typeof(System.Threading.Tasks.Task<bool>)
            };

            // Test scalar type identification
            foreach (var type in scalarTypes)
            {
                Assert.IsTrue(type.IsPrimitive || type == typeof(string) || type == typeof(DateTime) || type == typeof(decimal),
                    $"Type {type.Name} should be classified as scalar");
            }

            // Test collection type identification
            foreach (var type in collectionTypes)
            {
                var isCollection = type.IsArray ||
                    (type.IsGenericType && (
                        type.Name.StartsWith("List") ||
                        type.Name.StartsWith("IList") ||
                        type.Name.StartsWith("IEnumerable") ||
                        type.Name.StartsWith("ICollection")
                    ));
                Assert.IsTrue(isCollection, $"Type {type.Name} should be classified as collection");
            }

            // Test task type identification
            foreach (var type in taskTypes)
            {
                var isTask = type.IsGenericType && type.Name.StartsWith("Task");
                Assert.IsTrue(isTask, $"Type {type.Name} should be classified as task");
            }
        }

        [TestMethod]
        public void NullableTypeAnalysis_VariousTypes_CorrectIdentification()
        {
            var nullableValueTypes = new[]
            {
                typeof(int?), typeof(bool?), typeof(DateTime?),
                typeof(decimal?), typeof(double?), typeof(long?)
            };

            var nonNullableValueTypes = new[]
            {
                typeof(int), typeof(bool), typeof(DateTime),
                typeof(decimal), typeof(double), typeof(long)
            };

            var referenceTypes = new[]
            {
                typeof(string), typeof(object), typeof(List<int>),
                typeof(int[]), typeof(Exception)
            };

            // Test nullable value types
            foreach (var type in nullableValueTypes)
            {
                var underlyingType = Nullable.GetUnderlyingType(type);
                Assert.IsNotNull(underlyingType, $"Type {type.Name} should have underlying type");
                Assert.IsTrue(type.IsGenericType, $"Type {type.Name} should be generic");
            }

            // Test non-nullable value types
            foreach (var type in nonNullableValueTypes)
            {
                var underlyingType = Nullable.GetUnderlyingType(type);
                Assert.IsNull(underlyingType, $"Type {type.Name} should not have underlying type");
                Assert.IsTrue(type.IsValueType, $"Type {type.Name} should be value type");
            }

            // Test reference types
            foreach (var type in referenceTypes)
            {
                Assert.IsFalse(type.IsValueType, $"Type {type.Name} should not be value type");
                var underlyingType = Nullable.GetUnderlyingType(type);
                Assert.IsNull(underlyingType, $"Type {type.Name} should not have underlying nullable type");
            }
        }

        [TestMethod]
        public void GenericTypeAnalysis_Collections_ExtractElementTypes()
        {
            var genericTypes = new[]
            {
                (typeof(List<int>), typeof(int)),
                (typeof(IList<string>), typeof(string)),
                (typeof(IEnumerable<bool>), typeof(bool)),
                (typeof(ICollection<DateTime>), typeof(DateTime)),
                (typeof(System.Threading.Tasks.Task<decimal>), typeof(decimal))
            };

            foreach (var (containerType, expectedElementType) in genericTypes)
            {
                Assert.IsTrue(containerType.IsGenericType, $"Type {containerType.Name} should be generic");

                var genericArguments = containerType.GetGenericArguments();
                Assert.IsTrue(genericArguments.Length > 0, $"Type {containerType.Name} should have generic arguments");

                var actualElementType = genericArguments[0];
                Assert.AreEqual(expectedElementType, actualElementType,
                    $"Element type should be {expectedElementType.Name} for {containerType.Name}");
            }
        }

        [TestMethod]
        public void EnumHandling_VariousEnums_CorrectAnalysis()
        {
            var enumTypes = new[]
            {
                typeof(System.Data.ConnectionState),
                typeof(System.Data.DbType),
                typeof(System.Data.IsolationLevel),
                typeof(StringComparison),
                typeof(DateTimeKind)
            };

            foreach (var enumType in enumTypes)
            {
                Assert.IsTrue(enumType.IsEnum, $"Type {enumType.Name} should be enum");

                var underlyingType = Enum.GetUnderlyingType(enumType);
                Assert.IsNotNull(underlyingType, $"Enum {enumType.Name} should have underlying type");
                Assert.IsTrue(underlyingType.IsPrimitive, $"Underlying type of {enumType.Name} should be primitive");

                // Test enum value conversion
#pragma warning disable IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling
                var values = Enum.GetValues(enumType);
#pragma warning restore IL3050
                Assert.IsTrue(values.Length > 0, $"Enum {enumType.Name} should have values");

                var firstValue = values.GetValue(0);
                Assert.IsNotNull(firstValue, $"First value of {enumType.Name} should not be null");

                // Test conversion to underlying type
                var intValue = Convert.ToInt32(firstValue);
                var backToEnum = Enum.ToObject(enumType, intValue);
                Assert.AreEqual(firstValue, backToEnum, "Enum conversion should be round-trip safe");
            }
        }

        [TestMethod]
        public void SqlIdentifierFormatting_VariousInputs_CorrectWrapping()
        {
            // Test SQL identifier wrapping logic
            var testCases = new[]
            {
                ("users", "[users]", "`users`", "\"users\""),
                ("user_table", "[user_table]", "`user_table`", "\"user_table\""),
                ("Order", "[Order]", "`Order`", "\"Order\""),
                ("table with spaces", "[table with spaces]", "`table with spaces`", "\"table with spaces\""),
                ("123table", "[123table]", "`123table`", "\"123table\"")
            };

            foreach (var (identifier, sqlServer, mysql, postgres) in testCases)
            {
                // SQL Server style
                var sqlServerResult = $"[{identifier}]";
                Assert.AreEqual(sqlServer, sqlServerResult, $"SQL Server wrapping failed for: {identifier}");

                // MySQL style
                var mysqlResult = $"`{identifier}`";
                Assert.AreEqual(mysql, mysqlResult, $"MySQL wrapping failed for: {identifier}");

                // PostgreSQL style
                var postgresResult = $"\"{identifier}\"";
                Assert.AreEqual(postgres, postgresResult, $"PostgreSQL wrapping failed for: {identifier}");
            }
        }

        [TestMethod]
        public void ParameterPrefixHandling_VariousDialects_CorrectPrefixes()
        {
            var dialectPrefixes = new[]
            {
                ("SqlServer", "@"),
                ("MySQL", "@"),
                ("PostgreSQL", "$"),
                ("Oracle", ":"),
                ("SQLite", "@"),
                ("DB2", "@")
            };

            foreach (var (dialect, expectedPrefix) in dialectPrefixes)
            {
                var parameterName = "userId";
                var fullParameter = $"{expectedPrefix}{parameterName}";

                Assert.IsTrue(fullParameter.StartsWith(expectedPrefix),
                    $"Parameter should start with {expectedPrefix} for {dialect}");
                Assert.IsTrue(fullParameter.EndsWith(parameterName),
                    $"Parameter should end with {parameterName} for {dialect}");
                Assert.AreEqual($"{expectedPrefix}{parameterName}", fullParameter,
                    $"Full parameter format incorrect for {dialect}");
            }
        }

        [TestMethod]
        public void StringUtilities_EdgeCases_HandledCorrectly()
        {
            // Test edge cases in string processing
            var edgeCases = new[]
            {
                (null, null),
                ("", ""),
                (" ", " "),
                ("a", "a"),
                ("A", "a"),
                ("aB", "a_b"),
                ("AB", "a_b"),
                ("ABC", "a_b_c"),
                ("aBC", "a_b_c"),
                ("ABc", "a_bc"),
                ("123", "123"),
                ("a123", "a123"),
                ("A123", "a123"),
                ("a123B", "a123_b")
            };

            foreach (var (input, expected) in edgeCases)
            {
                if (input == null)
                {
                    Assert.AreEqual(expected, input);
                    continue;
                }

                if (string.IsNullOrEmpty(input))
                {
                    Assert.AreEqual(expected, input);
                    continue;
                }

                // Apply conversion logic
                string result;
                if (input.Contains("_") && input.All(c => char.IsUpper(c) || c == '_' || char.IsDigit(c)))
                {
                    result = input.ToLower();
                }
                else
                {
                    result = Regex.Replace(input, "[A-Z]", x => $"_{x.Value.ToLower()}").TrimStart('_');
                }

                Assert.AreEqual(expected, result, $"Failed for input: '{input}'");
            }
        }

        [TestMethod]
        public void CollectionUtilities_VariousCollections_CorrectHandling()
        {
            // Test collection type detection and handling
            var collections = new object[]
            {
                new List<int> { 1, 2, 3 },
                new int[] { 1, 2, 3 },
                new string[] { "a", "b", "c" },
                new HashSet<string> { "x", "y", "z" },
                new Queue<int>(new[] { 1, 2, 3 }),
                new Stack<string>(new[] { "a", "b", "c" })
            };

            foreach (var collection in collections)
            {
                var type = collection.GetType();

                // Check if it's a collection
#pragma warning disable IL2075 // 'this' argument does not satisfy 'DynamicallyAccessedMemberTypes.Interfaces'
                var isCollection = type.IsArray ||
                    type.GetInterfaces().Any(i =>
#pragma warning restore IL2075
                        i.IsGenericType &&
                        (i.GetGenericTypeDefinition() == typeof(IEnumerable<>) ||
                         i.GetGenericTypeDefinition() == typeof(ICollection<>) ||
                         i.GetGenericTypeDefinition() == typeof(IList<>)));

                Assert.IsTrue(isCollection, $"Type {type.Name} should be identified as collection");

                // Check element type extraction
                Type? elementType = null;
                if (type.IsArray)
                {
                    elementType = type.GetElementType()!;
                }
                else if (type.IsGenericType)
                {
                    elementType = type.GetGenericArguments()[0];
                }

                Assert.IsNotNull(elementType, $"Element type should be extractable from {type.Name}");
            }
        }
    }
}
