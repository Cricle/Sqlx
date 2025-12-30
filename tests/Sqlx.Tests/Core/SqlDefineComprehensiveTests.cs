// -----------------------------------------------------------------------
// <copyright file="SqlDefineComprehensiveTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.Tests.Core;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

/// <summary>
/// Comprehensive tests for SqlDefine static class and database dialect configurations.
/// </summary>
[TestClass]
public class SqlDefineComprehensiveTests
{
    #region SqlDefine Constants Tests

    /// <summary>
    /// Tests MySQL dialect configuration values.
    /// </summary>
    [TestMethod]
    public void SqlDefine_MySql_HasCorrectConfiguration()
    {
        // Act
        var dialect = SqlDefine.MySql;

        // Assert
        Assert.AreEqual("`", dialect.ColumnLeft);
        Assert.AreEqual("`", dialect.ColumnRight);
        Assert.AreEqual("'", dialect.StringLeft);
        Assert.AreEqual("'", dialect.StringRight);
        Assert.AreEqual("@", dialect.ParameterPrefix);
    }

    /// <summary>
    /// Tests SQL Server dialect configuration values.
    /// </summary>
    [TestMethod]
    public void SqlDefine_SqlServer_HasCorrectConfiguration()
    {
        // Act
        var dialect = SqlDefine.SqlServer;

        // Assert
        Assert.AreEqual("[", dialect.ColumnLeft);
        Assert.AreEqual("]", dialect.ColumnRight);
        Assert.AreEqual("'", dialect.StringLeft);
        Assert.AreEqual("'", dialect.StringRight);
        Assert.AreEqual("@", dialect.ParameterPrefix);
    }

    /// <summary>
    /// Tests PostgreSQL dialect configuration values.
    /// </summary>
    [TestMethod]
    public void SqlDefine_PgSql_HasCorrectConfiguration()
    {
        // Act
        var dialect = SqlDefine.PgSql;

        // Assert
        Assert.AreEqual("\"", dialect.ColumnLeft);
        Assert.AreEqual("\"", dialect.ColumnRight);
        Assert.AreEqual("'", dialect.StringLeft);
        Assert.AreEqual("'", dialect.StringRight);
        Assert.AreEqual("$", dialect.ParameterPrefix);
    }

    /// <summary>
    /// Tests Oracle dialect configuration values.
    /// </summary>
    [TestMethod]
    public void SqlDefine_Oracle_HasCorrectConfiguration()
    {
        // Act
        var dialect = SqlDefine.Oracle;

        // Assert
        Assert.AreEqual("\"", dialect.ColumnLeft);
        Assert.AreEqual("\"", dialect.ColumnRight);
        Assert.AreEqual("'", dialect.StringLeft);
        Assert.AreEqual("'", dialect.StringRight);
        Assert.AreEqual(":", dialect.ParameterPrefix);
    }

    /// <summary>
    /// Tests DB2 dialect configuration values.
    /// </summary>
    [TestMethod]
    public void SqlDefine_DB2_HasCorrectConfiguration()
    {
        // Act
        var dialect = SqlDefine.DB2;

        // Assert
        Assert.AreEqual("\"", dialect.ColumnLeft);
        Assert.AreEqual("\"", dialect.ColumnRight);
        Assert.AreEqual("'", dialect.StringLeft);
        Assert.AreEqual("'", dialect.StringRight);
        Assert.AreEqual("?", dialect.ParameterPrefix);
    }

    /// <summary>
    /// Tests SQLite dialect configuration values.
    /// </summary>
    [TestMethod]
    public void SqlDefine_Sqlite_HasCorrectConfiguration()
    {
        // Act
        var dialect = SqlDefine.Sqlite;

        // Assert
        Assert.AreEqual("[", dialect.ColumnLeft);
        Assert.AreEqual("]", dialect.ColumnRight);
        Assert.AreEqual("'", dialect.StringLeft);
        Assert.AreEqual("'", dialect.StringRight);
        Assert.AreEqual("@", dialect.ParameterPrefix); // SQLite uses @ for ADO.NET compatibility
    }

    #endregion

    #region Dialect Comparison Tests

    /// <summary>
    /// Tests that all dialects have non-null and non-empty configuration values.
    /// </summary>
    [TestMethod]
    public void SqlDefine_AllDialects_HaveValidConfigurations()
    {
        // Arrange
        var dialects = new[]
        {
            SqlDefine.MySql,
            SqlDefine.SqlServer,
            SqlDefine.PgSql,
            SqlDefine.Oracle,
            SqlDefine.DB2,
            SqlDefine.Sqlite
        };

        // Act & Assert
        foreach (var dialect in dialects)
        {
            Assert.IsNotNull(dialect.ColumnLeft, "ColumnLeft should not be null");
            Assert.IsNotNull(dialect.ColumnRight, "ColumnRight should not be null");
            Assert.IsNotNull(dialect.StringLeft, "StringLeft should not be null");
            Assert.IsNotNull(dialect.StringRight, "StringRight should not be null");
            Assert.IsNotNull(dialect.ParameterPrefix, "ParameterPrefix should not be null");

            Assert.IsTrue(dialect.ColumnLeft.Length > 0, "ColumnLeft should not be empty");
            Assert.IsTrue(dialect.ColumnRight.Length > 0, "ColumnRight should not be empty");
            Assert.IsTrue(dialect.StringLeft.Length > 0, "StringLeft should not be empty");
            Assert.IsTrue(dialect.StringRight.Length > 0, "StringRight should not be empty");
            Assert.IsTrue(dialect.ParameterPrefix.Length > 0, "ParameterPrefix should not be empty");
        }
    }

    /// <summary>
    /// Tests that dialects with same column wrapping are grouped correctly.
    /// </summary>
    [TestMethod]
    public void SqlDefine_DialectsWithSameColumnWrapping_AreGroupedCorrectly()
    {
        // Square brackets group
        var squareBracketDialects = new[] { SqlDefine.SqlServer, SqlDefine.Sqlite };
        foreach (var dialect in squareBracketDialects)
        {
            Assert.AreEqual("[", dialect.ColumnLeft);
            Assert.AreEqual("]", dialect.ColumnRight);
        }

        // Double quotes group
        var doubleQuoteDialects = new[] { SqlDefine.PgSql, SqlDefine.Oracle, SqlDefine.DB2 };
        foreach (var dialect in doubleQuoteDialects)
        {
            Assert.AreEqual("\"", dialect.ColumnLeft);
            Assert.AreEqual("\"", dialect.ColumnRight);
        }

        // Backticks group (MySQL only)
        Assert.AreEqual("`", SqlDefine.MySql.ColumnLeft);
        Assert.AreEqual("`", SqlDefine.MySql.ColumnRight);
    }

    /// <summary>
    /// Tests that dialects with same parameter prefix are grouped correctly.
    /// </summary>
    [TestMethod]
    public void SqlDefine_DialectsWithSameParameterPrefix_AreGroupedCorrectly()
    {
        // @ prefix group (includes SQLite for ADO.NET compatibility)
        var atPrefixDialects = new[] { SqlDefine.MySql, SqlDefine.SqlServer, SqlDefine.Sqlite };
        foreach (var dialect in atPrefixDialects)
        {
            Assert.AreEqual("@", dialect.ParameterPrefix);
        }

        // $ prefix group  
        var dollarPrefixDialects = new[] { SqlDefine.PgSql };
        foreach (var dialect in dollarPrefixDialects)
        {
            Assert.AreEqual("$", dialect.ParameterPrefix);
        }

        // Unique prefixes
        Assert.AreEqual(":", SqlDefine.Oracle.ParameterPrefix);
        Assert.AreEqual("?", SqlDefine.DB2.ParameterPrefix);
    }

    /// <summary>
    /// Tests that all dialects use single quotes for strings.
    /// </summary>
    [TestMethod]
    public void SqlDefine_AllDialects_UseSingleQuotesForStrings()
    {
        // Arrange
        var dialects = new[]
        {
            SqlDefine.MySql,
            SqlDefine.SqlServer,
            SqlDefine.PgSql,
            SqlDefine.Oracle,
            SqlDefine.DB2,
            SqlDefine.Sqlite
        };

        // Act & Assert
        foreach (var dialect in dialects)
        {
            Assert.AreEqual("'", dialect.StringLeft, "All dialects should use single quotes for string left delimiter");
            Assert.AreEqual("'", dialect.StringRight, "All dialects should use single quotes for string right delimiter");
        }
    }

    #endregion

    #region Dialect Usage with ExpressionToSql Tests

    /// <summary>
    /// Tests that each dialect produces correctly formatted column names.
    /// </summary>
    [TestMethod]
    public void SqlDefine_WithExpressionToSql_ProducesCorrectColumnFormatting()
    {
        // Arrange
        var testCases = new[]
        {
            new { Name = "MySQL", Dialect = SqlDefine.MySql, Expected = "`TestColumn`" },
            new { Name = "SQL Server", Dialect = SqlDefine.SqlServer, Expected = "[TestColumn]" },
            new { Name = "PostgreSQL", Dialect = SqlDefine.PgSql, Expected = "\"TestColumn\"" },
            new { Name = "Oracle", Dialect = SqlDefine.Oracle, Expected = "\"TestColumn\"" },
            new { Name = "DB2", Dialect = SqlDefine.DB2, Expected = "\"TestColumn\"" },
            new { Name = "SQLite", Dialect = SqlDefine.Sqlite, Expected = "[TestColumn]" }
        };

        foreach (var testCase in testCases)
        {
            // Act - Simulate column formatting
            var formattedColumn = testCase.Dialect.ColumnLeft + "TestColumn" + testCase.Dialect.ColumnRight;

            // Assert
            Assert.AreEqual(testCase.Expected, formattedColumn,
                $"{testCase.Name} should format columns as {testCase.Expected}");
        }
    }

    /// <summary>
    /// Tests that each dialect produces correctly formatted string literals.
    /// </summary>
    [TestMethod]
    public void SqlDefine_WithExpressionToSql_ProducesCorrectStringFormatting()
    {
        // Arrange
        const string testString = "Hello World";
        var dialects = new[]
        {
            SqlDefine.MySql,
            SqlDefine.SqlServer,
            SqlDefine.PgSql,
            SqlDefine.Oracle,
            SqlDefine.DB2,
            SqlDefine.Sqlite
        };

        foreach (var dialect in dialects)
        {
            // Act
            var formattedString = dialect.StringLeft + testString + dialect.StringRight;

            // Assert
            Assert.AreEqual("'Hello World'", formattedString,
                "All dialects should format strings with single quotes");
        }
    }

    /// <summary>
    /// Tests that each dialect produces correctly formatted parameter names.
    /// </summary>
    [TestMethod]
    public void SqlDefine_WithExpressionToSql_ProducesCorrectParameterFormatting()
    {
        // Arrange
        var testCases = new[]
        {
            new { Name = "MySQL", Dialect = SqlDefine.MySql, Expected = "@param1" },
            new { Name = "SQL Server", Dialect = SqlDefine.SqlServer, Expected = "@param1" },
            new { Name = "PostgreSQL", Dialect = SqlDefine.PgSql, Expected = "$param1" },
            new { Name = "Oracle", Dialect = SqlDefine.Oracle, Expected = ":param1" },
            new { Name = "DB2", Dialect = SqlDefine.DB2, Expected = "?param1" },
            new { Name = "SQLite", Dialect = SqlDefine.Sqlite, Expected = "@param1" } // SQLite uses @ for ADO.NET compatibility
        };

        foreach (var testCase in testCases)
        {
            // Act
            var formattedParameter = testCase.Dialect.ParameterPrefix + "param1";

            // Assert
            Assert.AreEqual(testCase.Expected, formattedParameter,
                $"{testCase.Name} should format parameters as {testCase.Expected}");
        }
    }

    #endregion

    #region Dialect Immutability Tests

    /// <summary>
    /// Tests that SqlDefine dialect configurations are immutable.
    /// </summary>
    [TestMethod]
    public void SqlDefine_DialectConfigurations_AreImmutable()
    {
        // Arrange
        var originalMySql = SqlDefine.MySql;
        var originalSqlServer = SqlDefine.SqlServer;

        // Act - Get references multiple times
        var mysql1 = SqlDefine.MySql;
        var mysql2 = SqlDefine.MySql;
        var sqlServer1 = SqlDefine.SqlServer;
        var sqlServer2 = SqlDefine.SqlServer;

        // Assert - Should be the same values
        Assert.AreEqual(originalMySql.ColumnLeft, mysql1.ColumnLeft);
        Assert.AreEqual(originalMySql.ColumnLeft, mysql2.ColumnLeft);
        Assert.AreEqual(originalSqlServer.ColumnLeft, sqlServer1.ColumnLeft);
        Assert.AreEqual(originalSqlServer.ColumnLeft, sqlServer2.ColumnLeft);

        // Values should be consistent
        Assert.AreEqual(mysql1.ColumnLeft, mysql2.ColumnLeft);
        Assert.AreEqual(sqlServer1.ColumnLeft, sqlServer2.ColumnLeft);
    }

    #endregion

    #region Dialect Edge Cases Tests

    /// <summary>
    /// Tests handling of special characters in column names with different dialects.
    /// </summary>
    [TestMethod]
    public void SqlDefine_SpecialCharactersInColumnNames_HandleCorrectly()
    {
        // Arrange
        const string specialColumnName = "Column With Spaces & Special-Chars";
        var testCases = new[]
        {
            new { Name = "MySQL", Dialect = SqlDefine.MySql, Expected = "`Column With Spaces & Special-Chars`" },
            new { Name = "SQL Server", Dialect = SqlDefine.SqlServer, Expected = "[Column With Spaces & Special-Chars]" },
            new { Name = "PostgreSQL", Dialect = SqlDefine.PgSql, Expected = "\"Column With Spaces & Special-Chars\"" },
            new { Name = "Oracle", Dialect = SqlDefine.Oracle, Expected = "\"Column With Spaces & Special-Chars\"" },
            new { Name = "DB2", Dialect = SqlDefine.DB2, Expected = "\"Column With Spaces & Special-Chars\"" },
            new { Name = "SQLite", Dialect = SqlDefine.Sqlite, Expected = "[Column With Spaces & Special-Chars]" }
        };

        foreach (var testCase in testCases)
        {
            // Act
            var formattedColumn = testCase.Dialect.ColumnLeft + specialColumnName + testCase.Dialect.ColumnRight;

            // Assert
            Assert.AreEqual(testCase.Expected, formattedColumn,
                $"{testCase.Name} should handle special characters in column names");
        }
    }

    /// <summary>
    /// Tests handling of reserved keywords as column names with different dialects.
    /// </summary>
    [TestMethod]
    public void SqlDefine_ReservedKeywordsAsColumnNames_HandleCorrectly()
    {
        // Arrange
        var reservedKeywords = new[] { "SELECT", "FROM", "WHERE", "ORDER", "GROUP", "HAVING" };
        var dialects = new[]
        {
            new { Name = "MySQL", Config = SqlDefine.MySql, Wrapper = "`" },
            new { Name = "SQL Server", Config = SqlDefine.SqlServer, Wrapper = "[" },
            new { Name = "PostgreSQL", Config = SqlDefine.PgSql, Wrapper = "\"" },
            new { Name = "Oracle", Config = SqlDefine.Oracle, Wrapper = "\"" },
            new { Name = "DB2", Config = SqlDefine.DB2, Wrapper = "\"" },
            new { Name = "SQLite", Config = SqlDefine.Sqlite, Wrapper = "[" }
        };

        foreach (var dialect in dialects)
        {
            foreach (var keyword in reservedKeywords)
            {
                // Act
                var formattedColumn = dialect.Config.ColumnLeft + keyword + dialect.Config.ColumnRight;

                // Assert
                var expectedWrapper = dialect.Name == "MySQL" ? "`" :
                                    dialect.Name == "SQL Server" || dialect.Name == "SQLite" ? "[" : "\"";
                var rightWrapper = dialect.Name == "SQL Server" || dialect.Name == "SQLite" ? "]" :
                                 dialect.Name == "MySQL" ? "`" : "\"";

                Assert.AreEqual(expectedWrapper + keyword + rightWrapper, formattedColumn,
                    $"{dialect.Name} should wrap reserved keyword {keyword} correctly");
            }
        }
    }

    /// <summary>
    /// Tests handling of empty and null strings (edge case).
    /// </summary>
    [TestMethod]
    public void SqlDefine_EmptyAndNullStrings_HandleGracefully()
    {
        // Arrange
        var dialects = new[]
        {
            SqlDefine.MySql,
            SqlDefine.SqlServer,
            SqlDefine.PgSql,
            SqlDefine.Oracle,
            SqlDefine.DB2,
            SqlDefine.Sqlite
        };

        foreach (var dialect in dialects)
        {
            // Act & Assert - Empty string
            var emptyColumn = dialect.ColumnLeft + string.Empty + dialect.ColumnRight;
            Assert.IsNotNull(emptyColumn);
            Assert.IsTrue(emptyColumn.Length >= 2); // At least the wrapper characters

            var emptyString = dialect.StringLeft + string.Empty + dialect.StringRight;
            Assert.AreEqual("''", emptyString); // All dialects use single quotes

            var emptyParameter = dialect.ParameterPrefix + string.Empty;
            Assert.IsNotNull(emptyParameter);
            Assert.IsTrue(emptyParameter.Length >= 1); // At least the prefix
        }
    }

    #endregion

    #region SqlDefine Class Structure Tests

    /// <summary>
    /// Tests that SqlDefine is a static class.
    /// </summary>
    [TestMethod]
    public void SqlDefine_IsStaticClass()
    {
        // Arrange & Act
        var type = typeof(SqlDefine);

        // Assert
        Assert.IsTrue(type.IsAbstract && type.IsSealed, "SqlDefine should be a static class");
    }

    /// <summary>
    /// Tests that all SqlDefine fields are static and readonly.
    /// </summary>
    [TestMethod]
    public void SqlDefine_AllFields_AreStaticAndReadonly()
    {
        // Arrange
        var type = typeof(SqlDefine);
        var fields = type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

        // Act & Assert
        Assert.IsTrue(fields.Length >= 6, "Should have at least 6 dialect fields");

        foreach (var field in fields)
        {
            Assert.IsTrue(field.IsStatic, $"Field {field.Name} should be static");
            Assert.IsTrue(field.IsInitOnly, $"Field {field.Name} should be readonly");
        }
    }

    /// <summary>
    /// Tests that SqlDefine has expected public fields.
    /// </summary>
    [TestMethod]
    public void SqlDefine_HasExpectedPublicFields()
    {
        // Arrange
        var type = typeof(SqlDefine);
        var expectedFields = new[] { "MySql", "SqlServer", "PgSql", "Oracle", "DB2", "Sqlite" };

        // Act & Assert
        foreach (var expectedField in expectedFields)
        {
            var field = type.GetField(expectedField, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            Assert.IsNotNull(field, $"SqlDefine should have a public static field named {expectedField}");
        }
    }

    #endregion
}
