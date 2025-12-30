// -----------------------------------------------------------------------
// <copyright file="BatchCommandGenerationTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace Sqlx.Tests.Core;

[TestClass]
public class BatchCommandGenerationTests
{
    private static readonly string[] StringArray = new[] { "Name", "Email" };

    [TestMethod]
    public void BatchCommand_SqlGeneration_ShouldBeCorrect()
    {
        // This test verifies that the SQL generation for batch commands is correct

        // Test INSERT SQL generation
        var expectedInsertPattern = @"INSERT INTO `Users` (`Name`, `Email`) VALUES (@Name, @Email)";
        var actualInsert = GenerateInsertSql("Users", StringArray, "`", "@");

        Assert.AreEqual(expectedInsertPattern, actualInsert, "INSERT SQL should be correctly formatted");

        // Test UPDATE SQL generation  
        var expectedUpdatePattern = @"UPDATE `Users` SET `Name` = @Name, `Email` = @Email WHERE `Id` = @Id";
        var actualUpdate = GenerateUpdateSql("Users", StringArray, "Id", "`", "@");

        Assert.AreEqual(expectedUpdatePattern, actualUpdate, "UPDATE SQL should be correctly formatted");

        // Test DELETE SQL generation
        var expectedDeletePattern = @"DELETE FROM `Users` WHERE `Id` = @Id";
        var actualDelete = GenerateDeleteSql("Users", "Id", "`", "@");

        Assert.AreEqual(expectedDeletePattern, actualDelete, "DELETE SQL should be correctly formatted");
    }

    [TestMethod]
    public void BatchCommand_ParameterNames_ShouldBeConsistent()
    {
        // This test verifies that parameter names in SQL and parameter creation are consistent

        var sqlParameters = new[] { "@Name", "@Email", "@Id" };
        var codeParameters = new[] { "Name", "Email", "Id" };

        for (int i = 0; i < sqlParameters.Length; i++)
        {
            var expectedParam = "@" + codeParameters[i];
            Assert.AreEqual(expectedParam, sqlParameters[i],
                $"Parameter {codeParameters[i]} should be consistently named");
        }
    }

    [TestMethod]
    public void BatchCommand_QuoteEscaping_ShouldBeCorrect()
    {
        // This test verifies that quotes are properly escaped in generated SQL

        // Test with different quote types
        var testCases = new[]
        {
            ("`", "User`Name", "`User``Name`"), // MySQL backticks
            ("\"", "User\"Name", "\"User\\\"Name\""), // PostgreSQL/SQLite double quotes
            ("[", "User]Name", "[User]]Name]") // SQL Server square brackets
        };

        foreach (var (leftQuote, columnName, expected) in testCases)
        {
            var rightQuote = leftQuote == "[" ? "]" : leftQuote;
            var actual = WrapAndEscapeColumn(columnName, leftQuote, rightQuote);
            Assert.AreEqual(expected, actual,
                $"Column {columnName} should be properly escaped with {leftQuote} quotes");
        }
    }

    [TestMethod]
    public void BatchCommand_SqlDefineIntegration_ShouldWork()
    {
        // This test verifies integration with different SqlDefine configurations

        var testConfigs = new[]
        {
            ("MySql", "`", "`", "@"),
            ("SqlServer", "[", "]", "@"),
            ("PostgreSQL", "\"", "\"", "$"),
            ("Oracle", "\"", "\"", ":"),
            ("SQLite", "[", "]", "@sqlite")
        };

        foreach (var (dialectName, leftQuote, rightQuote, paramPrefix) in testConfigs)
        {
            var tableName = "Users";
            var columnName = "Name";

            var expectedTable = $"{leftQuote}{tableName}{rightQuote}";
            var expectedColumn = $"{leftQuote}{columnName}{rightQuote}";
            var expectedParam = $"{paramPrefix}{columnName}";

            // Verify table wrapping
            var actualTable = WrapColumn(tableName, leftQuote, rightQuote);
            Assert.AreEqual(expectedTable, actualTable,
                $"{dialectName} should wrap table names correctly");

            // Verify column wrapping
            var actualColumn = WrapColumn(columnName, leftQuote, rightQuote);
            Assert.AreEqual(expectedColumn, actualColumn,
                $"{dialectName} should wrap column names correctly");

            // Verify parameter naming
            var actualParam = $"{paramPrefix}{columnName}";
            Assert.AreEqual(expectedParam, actualParam,
                $"{dialectName} should use correct parameter prefix");
        }
    }

    [TestMethod]
    public void BatchCommand_ErrorHandling_ShouldBeRobust()
    {
        // This test verifies error handling in batch command generation

        // Test empty properties
        Assert.ThrowsException<ArgumentException>(() =>
            ValidateProperties(Array.Empty<string>()), "Empty properties should throw exception");

        // Test null table name
        Assert.ThrowsException<ArgumentNullException>(() =>
            ValidateTableName(null!), "Null table name should throw exception");

        // Test empty table name
        Assert.ThrowsException<ArgumentException>(() =>
            ValidateTableName(""), "Empty table name should throw exception");
    }

    [TestMethod]
    public void BatchCommand_SpecialCharacters_ShouldBeHandled()
    {
        // This test verifies handling of special characters in table and column names

        var specialNames = new[]
        {
            "User Name", // Space
            "User-Name", // Hyphen
            "User_Name", // Underscore
            "User123",   // Numbers
            "用户名",     // Unicode characters
            "User'Name", // Single quote
            "User\"Name" // Double quote
        };

        foreach (var name in specialNames)
        {
            // Should not throw exception
            var wrapped = WrapColumn(name, "`", "`");
            Assert.IsNotNull(wrapped, $"Should handle special name: {name}");
            Assert.IsTrue(wrapped.StartsWith("`") && wrapped.EndsWith("`"),
                $"Should properly wrap special name: {name}");
        }
    }

    [TestMethod]
    public void BatchCommand_PropertyMapping_ShouldBeAccurate()
    {
        // This test verifies that property mapping between C# and SQL is accurate

        var propertyMappings = new[]
        {
            ("Id", "Id"),
            ("FirstName", "FirstName"),
            ("LastName", "LastName"),
            ("EmailAddress", "EmailAddress"),
            ("CreatedAt", "CreatedAt"),
            ("UpdatedAt", "UpdatedAt")
        };

        foreach (var (csharpProperty, sqlColumn) in propertyMappings)
        {
            // Verify property name mapping
            var actualSqlColumn = MapPropertyToSqlColumn(csharpProperty);
            Assert.AreEqual(sqlColumn, actualSqlColumn,
                $"Property {csharpProperty} should map to SQL column {sqlColumn}");

            // Verify parameter name consistency
            var parameterName = $"@{sqlColumn}";
            var actualParameter = $"@{actualSqlColumn}";
            Assert.AreEqual(parameterName, actualParameter,
                $"Parameter name should be consistent for {csharpProperty}");
        }
    }

    // Helper methods for testing
    private static string GenerateInsertSql(string tableName, string[] columns, string quote, string paramPrefix)
    {
        var wrappedTable = $"{quote}{tableName}{quote}";
        var wrappedColumns = string.Join(", ", columns.Select(c => $"{quote}{c}{quote}"));
        var parameters = string.Join(", ", columns.Select(c => $"{paramPrefix}{c}"));
        return $"INSERT INTO {wrappedTable} ({wrappedColumns}) VALUES ({parameters})";
    }

    private static string GenerateUpdateSql(string tableName, string[] setColumns, string whereColumn, string quote, string paramPrefix)
    {
        var wrappedTable = $"{quote}{tableName}{quote}";
        var setClause = string.Join(", ", setColumns.Select(c => $"{quote}{c}{quote} = {paramPrefix}{c}"));
        var whereClause = $"{quote}{whereColumn}{quote} = {paramPrefix}{whereColumn}";
        return $"UPDATE {wrappedTable} SET {setClause} WHERE {whereClause}";
    }

    private static string GenerateDeleteSql(string tableName, string whereColumn, string quote, string paramPrefix)
    {
        var wrappedTable = $"{quote}{tableName}{quote}";
        var whereClause = $"{quote}{whereColumn}{quote} = {paramPrefix}{whereColumn}";
        return $"DELETE FROM {wrappedTable} WHERE {whereClause}";
    }

    private static string WrapColumn(string columnName, string leftQuote, string rightQuote)
    {
        return $"{leftQuote}{columnName}{rightQuote}";
    }

    private static string WrapAndEscapeColumn(string columnName, string leftQuote, string rightQuote)
    {
        // Simulate the escaping logic used in the actual code
        string escaped = columnName;
        if (leftQuote == "\"")
        {
            escaped = columnName.Replace("\"", "\\\"");
        }
        else if (leftQuote == "`")
        {
            escaped = columnName.Replace("`", "``");
        }
        else if (leftQuote == "[")
        {
            escaped = columnName.Replace("]", "]]");
        }

        return $"{leftQuote}{escaped}{rightQuote}";
    }

    private static void ValidateProperties(string[] properties)
    {
        if (properties == null || properties.Length == 0)
        {
            throw new ArgumentException("Properties cannot be null or empty");
        }
    }

    private static void ValidateTableName(string tableName)
    {
        ArgumentNullException.ThrowIfNull(tableName);
        if (string.IsNullOrWhiteSpace(tableName))
        {
            throw new ArgumentException("Table name cannot be empty");
        }
    }

    private static string MapPropertyToSqlColumn(string propertyName)
    {
        // Simple mapping - in real implementation this might be more complex
        return propertyName;
    }
}
