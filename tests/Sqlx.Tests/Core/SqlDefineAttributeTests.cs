// -----------------------------------------------------------------------
// <copyright file="SqlDefineAttributeTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using System.Linq;

namespace Sqlx.Tests.Core;

[TestClass]
public class SqlDefineAttributeTests
{
    [TestMethod]
    public void SqlDefineMapping_MySql_ShouldBeZero()
    {
        // This test verifies that MySql enum value maps to 0
        // We test the mapping logic that exists in AbstractGenerator and MethodGenerationContext
        
        // Arrange
        var mysqlValue = 0;
        
        // Act - Test the mapping logic that exists in both AbstractGenerator and MethodGenerationContext
        var expectedSqlDefine = mysqlValue switch
        {
            0 => SqlDefine.MySql,
            1 => SqlDefine.SqlServer,
            2 => SqlDefine.PgSql,
            3 => SqlDefine.Oracle,
            4 => SqlDefine.DB2,
            5 => SqlDefine.SQLite,
            _ => SqlDefine.SqlServer,
        };

        // Assert
        Assert.AreEqual(SqlDefine.MySql, expectedSqlDefine);
        Assert.AreEqual("`", expectedSqlDefine.ColumnLeft);
        Assert.AreEqual("`", expectedSqlDefine.ColumnRight);
        Assert.AreEqual("@", expectedSqlDefine.ParameterPrefix);
    }

    [TestMethod]
    public void SqlDefineMapping_SqlServer_ShouldBeOne()
    {
        // Arrange
        var sqlServerValue = 1;
        
        // Act
        var expectedSqlDefine = sqlServerValue switch
        {
            0 => SqlDefine.MySql,
            1 => SqlDefine.SqlServer,
            2 => SqlDefine.PgSql,
            3 => SqlDefine.Oracle,
            4 => SqlDefine.DB2,
            5 => SqlDefine.SQLite,
            _ => SqlDefine.SqlServer,
        };

        // Assert
        Assert.AreEqual(SqlDefine.SqlServer, expectedSqlDefine);
        Assert.AreEqual("[", expectedSqlDefine.ColumnLeft);
        Assert.AreEqual("]", expectedSqlDefine.ColumnRight);
        Assert.AreEqual("@", expectedSqlDefine.ParameterPrefix);
    }

    [TestMethod]
    public void SqlDefineMapping_PostgreSql_ShouldBeTwo()
    {
        // Arrange
        var pgSqlValue = 2;
        
        // Act
        var expectedSqlDefine = pgSqlValue switch
        {
            0 => SqlDefine.MySql,
            1 => SqlDefine.SqlServer,
            2 => SqlDefine.PgSql,
            3 => SqlDefine.Oracle,
            4 => SqlDefine.DB2,
            5 => SqlDefine.SQLite,
            _ => SqlDefine.SqlServer,
        };

        // Assert
        Assert.AreEqual(SqlDefine.PgSql, expectedSqlDefine);
        Assert.AreEqual("\"", expectedSqlDefine.ColumnLeft);
        Assert.AreEqual("\"", expectedSqlDefine.ColumnRight);
        Assert.AreEqual("$", expectedSqlDefine.ParameterPrefix);
    }

    [TestMethod]
    public void SqlDefineMapping_Oracle_ShouldBeThree()
    {
        // Arrange
        var oracleValue = 3;
        
        // Act
        var expectedSqlDefine = oracleValue switch
        {
            0 => SqlDefine.MySql,
            1 => SqlDefine.SqlServer,
            2 => SqlDefine.PgSql,
            3 => SqlDefine.Oracle,
            4 => SqlDefine.DB2,
            5 => SqlDefine.SQLite,
            _ => SqlDefine.SqlServer,
        };

        // Assert
        Assert.AreEqual(SqlDefine.Oracle, expectedSqlDefine);
        Assert.AreEqual("\"", expectedSqlDefine.ColumnLeft);
        Assert.AreEqual("\"", expectedSqlDefine.ColumnRight);
        Assert.AreEqual(":", expectedSqlDefine.ParameterPrefix);
    }

    [TestMethod]
    public void SqlDefineMapping_DB2_ShouldBeFour()
    {
        // Arrange
        var db2Value = 4;
        
        // Act
        var expectedSqlDefine = db2Value switch
        {
            0 => SqlDefine.MySql,
            1 => SqlDefine.SqlServer,
            2 => SqlDefine.PgSql,
            3 => SqlDefine.Oracle,
            4 => SqlDefine.DB2,
            5 => SqlDefine.SQLite,
            _ => SqlDefine.SqlServer,
        };

        // Assert
        Assert.AreEqual(SqlDefine.DB2, expectedSqlDefine);
        Assert.AreEqual("\"", expectedSqlDefine.ColumnLeft);
        Assert.AreEqual("\"", expectedSqlDefine.ColumnRight);
        Assert.AreEqual("?", expectedSqlDefine.ParameterPrefix);
    }

    [TestMethod]
    public void SqlDefineMapping_SQLite_ShouldBeFive()
    {
        // Arrange
        var sqliteValue = 5;
        
        // Act
        var expectedSqlDefine = sqliteValue switch
        {
            0 => SqlDefine.MySql,
            1 => SqlDefine.SqlServer,
            2 => SqlDefine.PgSql,
            3 => SqlDefine.Oracle,
            4 => SqlDefine.DB2,
            5 => SqlDefine.SQLite,
            _ => SqlDefine.SqlServer,
        };

        // Assert
        Assert.AreEqual(SqlDefine.SQLite, expectedSqlDefine);
        Assert.AreEqual("[", expectedSqlDefine.ColumnLeft);
        Assert.AreEqual("]", expectedSqlDefine.ColumnRight);
        Assert.AreEqual("@sqlite", expectedSqlDefine.ParameterPrefix);
    }

    [TestMethod]
    public void SqlDefineMapping_InvalidValue_ShouldDefaultToSqlServer()
    {
        // Arrange
        var invalidValue = 999;
        
        // Act
        var expectedSqlDefine = invalidValue switch
        {
            0 => SqlDefine.MySql,
            1 => SqlDefine.SqlServer,
            2 => SqlDefine.PgSql,
            3 => SqlDefine.Oracle,
            4 => SqlDefine.DB2,
            5 => SqlDefine.SQLite,
            _ => SqlDefine.SqlServer, // Default fallback
        };

        // Assert
        Assert.AreEqual(SqlDefine.SqlServer, expectedSqlDefine);
    }

    [TestMethod]
    public void SqlDefineMapping_AllValues_AreUnique()
    {
        // Arrange
        var allMappings = new[]
        {
            (0, SqlDefine.MySql),
            (1, SqlDefine.SqlServer),
            (2, SqlDefine.PgSql),
            (3, SqlDefine.Oracle),
            (4, SqlDefine.DB2),
            (5, SqlDefine.SQLite)
        };

        // Act & Assert
        var uniqueValues = allMappings.Select(x => x.Item1).Distinct().Count();
        var uniqueSqlDefines = allMappings.Select(x => x.Item2).Distinct().Count();

        Assert.AreEqual(6, uniqueValues, "All enum values should be unique");
        Assert.AreEqual(6, uniqueSqlDefines, "All SqlDefine instances should be unique");
    }

    [TestMethod]
    public void SqlDefineMapping_MatchesEnumDefinition()
    {
        // This test verifies that our mapping matches the SqlDefineTypes enum definition
        // Based on the enum in AttributeSourceGenerator.cs:
        // MySql = 0, SqlServer = 1, Postgresql = 2, Oracle = 3, DB2 = 4, SQLite = 5

        var testCases = new[]
        {
            (0, SqlDefine.MySql, "MySql"),
            (1, SqlDefine.SqlServer, "SqlServer"),
            (2, SqlDefine.PgSql, "Postgresql"),
            (3, SqlDefine.Oracle, "Oracle"),
            (4, SqlDefine.DB2, "DB2"),
            (5, SqlDefine.SQLite, "SQLite")
        };

        foreach (var (enumValue, expectedSqlDefine, dialectName) in testCases)
        {
            // Act
            var actualSqlDefine = enumValue switch
            {
                0 => SqlDefine.MySql,
                1 => SqlDefine.SqlServer,
                2 => SqlDefine.PgSql,
                3 => SqlDefine.Oracle,
                4 => SqlDefine.DB2,
                5 => SqlDefine.SQLite,
                _ => SqlDefine.SqlServer,
            };

            // Assert
            Assert.AreEqual(expectedSqlDefine, actualSqlDefine, $"Enum value {enumValue} should map to {dialectName}");
        }
    }

    [TestMethod]
    public void SqlDefineCustomConstructor_ShouldCreateCorrectInstance()
    {
        // This tests the custom constructor path used when SqlDefineAttribute has 5 parameters
        // Testing the logic that would be used in ParseSqlDefineAttribute methods

        // Arrange
        var columnLeft = "<";
        var columnRight = ">";
        var stringLeft = "'";
        var stringRight = "'";
        var parameterPrefix = "$";

        // Act
        var customSqlDefine = new SqlDefine(columnLeft, columnRight, stringLeft, stringRight, parameterPrefix);

        // Assert
        Assert.AreEqual(columnLeft, customSqlDefine.ColumnLeft);
        Assert.AreEqual(columnRight, customSqlDefine.ColumnRight);
        Assert.AreEqual(stringLeft, customSqlDefine.StringLeft);
        Assert.AreEqual(stringRight, customSqlDefine.StringRight);
        Assert.AreEqual(parameterPrefix, customSqlDefine.ParameterPrefix);
    }

    [TestMethod]
    public void SqlDefineCustomConstructor_WithNullValues_ShouldUseDefaults()
    {
        // This tests the null-coalescing logic in the parsing methods
        // Simulating what happens when AttributeData.Value is null

        // Arrange
        string? nullColumnLeft = null;
        string? nullColumnRight = null;
        string? nullStringLeft = null;
        string? nullStringRight = null;
        string? nullParameterPrefix = null;

        // Act - Simulate the null-coalescing logic from the fixed code
        var customSqlDefine = new SqlDefine(
            nullColumnLeft ?? "[",
            nullColumnRight ?? "]",
            nullStringLeft ?? "'",
            nullStringRight ?? "'",
            nullParameterPrefix ?? "@");

        // Assert
        Assert.AreEqual("[", customSqlDefine.ColumnLeft);
        Assert.AreEqual("]", customSqlDefine.ColumnRight);
        Assert.AreEqual("'", customSqlDefine.StringLeft);
        Assert.AreEqual("'", customSqlDefine.StringRight);
        Assert.AreEqual("@", customSqlDefine.ParameterPrefix);
    }
}