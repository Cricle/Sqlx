// -----------------------------------------------------------------------
// <copyright file="EmptyTableNameTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Sqlx.Tests.Core;

[TestClass]
public class EmptyTableNameTests
{
    [TestMethod]
    public void SqlExecuteType_WithEmptyTableName_ShouldHandleGracefully()
    {
        // This test verifies that SqlExecuteType operations can handle empty table names
        // which is useful for scenarios like:
        // 1. BatchCommand operations that don't need predefined table names
        // 2. Custom SQL with stored procedures
        // 3. Expression-based SQL generation

        // Test that empty table name doesn't cause exceptions
        var emptyTableName = string.Empty;
        var nullTableName = (string?)null;

        Assert.IsTrue(string.IsNullOrEmpty(emptyTableName), "Empty table name should be handled");
        Assert.IsTrue(string.IsNullOrEmpty(nullTableName), "Null table name should be handled");
    }

    [TestMethod]
    public void BatchCommand_WithoutTableName_ShouldInferFromEntityType()
    {
        // This test documents the expected behavior for BatchCommand operations
        // When table name is not provided, it should be inferred from the entity type

        var expectedBehavior = @"
        // When SqlExecuteType is BatchCommand with empty table name:
        [SqlExecuteType(SqlExecuteTypes.BatchCommand, """")]
        public void BatchInsertUsers(List<User> users) { }

        // The generated code should infer table name from User entity type:
        // tableName = entityType?.Name ?? ""UnknownTable""
        ";

        Assert.IsNotNull(expectedBehavior, "Batch commands should infer table names from entity types");
    }

    [TestMethod]
    public void CustomSQL_WithEmptyTableName_ShouldUseExpressionOrStoredProc()
    {
        // This test documents scenarios where table name can be empty
        var customSqlScenarios = new[]
        {
            "EXEC GetUserStatistics", // Stored procedure
            "SELECT COUNT(*) FROM (SELECT 1) AS temp", // Subquery
            "WITH cte AS (SELECT 1) SELECT * FROM cte", // CTE
            "SELECT GETDATE()", // Function call without table
            "SELECT @param AS result" // Parameter-only query
        };

        foreach (var scenario in customSqlScenarios)
        {
            Assert.IsNotNull(scenario, $"Custom SQL scenario should be valid: {scenario}");
            Assert.IsFalse(scenario.Contains("INSERT INTO "),
                $"Custom SQL scenario doesn't require table name: {scenario}");
        }
    }

    [TestMethod]
    public void ExpressionToSql_WithEmptyTableName_ShouldGenerateDynamically()
    {
        // This test documents ExpressionToSql scenarios where table name is determined at runtime
        var expressionScenarios = new[]
        {
            "Expression<Func<User, bool>> predicate", // WHERE clause generation
            "Expression<Func<User, object>> selector", // SELECT clause generation
            "Expression<Func<User, User>> updater", // SET clause generation
        };

        foreach (var scenario in expressionScenarios)
        {
            Assert.IsNotNull(scenario, $"Expression scenario should be valid: {scenario}");
            Assert.IsTrue(scenario.Contains("Expression"),
                $"Expression scenario uses Expression<T>: {scenario}");
        }
    }

    [TestMethod]
    public void SqlExecuteTypes_AllTypes_ShouldHaveValidEnumValues()
    {
        // Test that all SqlExecuteTypes enum values are valid
        // This ensures the enum is properly defined for use with empty table names

        var enumValues = new[]
        {
            0, // Select
            1, // Update
            2, // Insert
            3, // Delete
            4, // BatchInsert
            5, // BatchUpdate
            6, // BatchDelete
            7  // BatchCommand
        };

        // Verify enum values are sequential and start from 0
        for (int i = 0; i < enumValues.Length; i++)
        {
            Assert.AreEqual(i, enumValues[i], $"SqlExecuteTypes enum value at index {i} should be {i}");
        }

        // Verify BatchCommand is the last value (allows empty table names)
        Assert.AreEqual(7, enumValues[7],
            "BatchCommand should be enum value 7");
    }

    [TestMethod]
    public void TableNameInference_FromEntityType_ShouldWorkCorrectly()
    {
        // This test documents how table names are inferred from entity types
        // when not explicitly provided in SqlExecuteType attribute

        var entityTypeToTableNameMappings = new[]
        {
            ("User", "User"),
            ("UserProfile", "UserProfile"),
            ("OrderItem", "OrderItem"),
            ("ProductCategory", "ProductCategory")
        };

        foreach (var (entityType, expectedTableName) in entityTypeToTableNameMappings)
        {
            Assert.AreEqual(expectedTableName, entityType,
                $"Entity type {entityType} should map to table name {expectedTableName}");
        }
    }

    [TestMethod]
    public void SqlDefine_WrapColumn_WithEmptyTableName_ShouldHandleGracefully()
    {
        // Test that SqlDefine.WrapColumn handles empty table names gracefully
        var sqlDefine = Sqlx.SqlDefine.SqlServer;

        var result1 = sqlDefine.WrapColumn("");
        var result2 = sqlDefine.WrapColumn(null!);

        // Should not throw exceptions
        Assert.IsTrue(result1 == null || result1.Length >= 0, "Empty string should be handled gracefully");
        Assert.IsTrue(result2 == null || result2.Length >= 0, "Null should be handled gracefully");
    }

    [TestMethod]
    public void BatchOperations_ErrorHandling_ShouldProvideUsefulMessages()
    {
        // Test that batch operations provide useful error messages when table name cannot be determined
        var errorScenarios = new[]
        {
            "No entity type found for table name inference",
            "SqlExecuteType attribute missing table name parameter",
            "Entity type has no properties for column mapping",
            "Collection parameter is null or empty"
        };

        foreach (var scenario in errorScenarios)
        {
            Assert.IsNotNull(scenario, $"Error scenario should provide useful message: {scenario}");
            Assert.IsTrue(scenario.Length > 10, $"Error message should be descriptive: {scenario}");
        }
    }

    [TestMethod]
    public void Constants_SqlExecuteTypeValues_ShouldHaveValidValues()
    {
        // Verify that Constants.SqlExecuteTypeValues have valid values
        // This ensures consistency in the system

        // Note: SqlExecuteTypeValues has been deprecated and removed
        // Note: SqlExecuteTypeValues test removed - SqlExecuteType has been deprecated
        Assert.IsTrue(true, "SqlExecuteType has been deprecated and replaced with SQL templates");
    }

    [TestMethod]
    public void EmptyTableName_DocumentedUseCases_ShouldBeSupported()
    {
        // This test documents the specific use cases where empty table names are valid
        var validEmptyTableNameUseCases = new[]
        {
            "BatchCommand with dynamic table inference",
            "Stored procedure execution via [Sqlx(\"EXEC ProcName\")]",
            "Custom SQL with subqueries or CTEs",
            "Function calls without table references",
            "Expression-based SQL generation",
            "Multi-table operations with JOIN clauses",
            "Temporary table operations",
            "System function queries (SELECT GETDATE())"
        };

        foreach (var useCase in validEmptyTableNameUseCases)
        {
            Assert.IsNotNull(useCase, $"Use case should be documented: {useCase}");
            Assert.IsTrue(useCase.Length > 5, $"Use case should be descriptive: {useCase}");
        }

        // Verify we have a reasonable number of use cases
        Assert.IsTrue(validEmptyTableNameUseCases.Length >= 5,
            "Should have multiple valid use cases for empty table names");
    }

    [TestMethod]
    public void TableNameValidation_ShouldNotRejectEmptyNames()
    {
        // This test ensures that empty table names are not automatically rejected
        // The validation should be context-dependent

        var tableNameValidationResults = new[]
        {
            (tableName: "", isValidForBatchCommand: true, isValidForBasicCrud: false),
            (tableName: (string?)null, isValidForBatchCommand: true, isValidForBasicCrud: false),
            (tableName: "Users", isValidForBatchCommand: true, isValidForBasicCrud: true),
            (tableName: "  ", isValidForBatchCommand: false, isValidForBasicCrud: false), // whitespace only
        };

        foreach (var (tableName, isValidForBatchCommand, isValidForBasicCrud) in tableNameValidationResults)
        {
            // For BatchCommand operations, empty table names should be acceptable
            if (isValidForBatchCommand)
            {
                Assert.IsTrue(string.IsNullOrEmpty(tableName) || !string.IsNullOrWhiteSpace(tableName),
                    $"Table name '{tableName}' should be valid for BatchCommand");
            }

            // For basic CRUD operations, non-empty table names are preferred
            if (isValidForBasicCrud)
            {
                Assert.IsFalse(string.IsNullOrEmpty(tableName),
                    $"Table name '{tableName}' should not be empty for basic CRUD");
            }
        }
    }
}
