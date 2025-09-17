// -----------------------------------------------------------------------
// <copyright file="SqlDefineTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;

namespace Sqlx.Tests.Core;

/// <summary>
/// Comprehensive tests for SqlDefine class and SqlDialect record struct.
/// </summary>
[TestClass]
public class SqlDefineTests : TestBase
{
    [TestMethod]
    public void MySql_HasCorrectConfiguration()
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

    [TestMethod]
    public void SqlServer_HasCorrectConfiguration()
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

    [TestMethod]
    public void PgSql_HasCorrectConfiguration()
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

    [TestMethod]
    public void Oracle_HasCorrectConfiguration()
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

    [TestMethod]
    public void DB2_HasCorrectConfiguration()
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

    [TestMethod]
    public void Sqlite_HasCorrectConfiguration()
    {
        // Act
        var dialect = SqlDefine.Sqlite;

        // Assert
        Assert.AreEqual("[", dialect.ColumnLeft);
        Assert.AreEqual("]", dialect.ColumnRight);
        Assert.AreEqual("'", dialect.StringLeft);
        Assert.AreEqual("'", dialect.StringRight);
        Assert.AreEqual("$", dialect.ParameterPrefix);
    }

    [TestMethod]
    public void SqlDialect_Constructor_SetsAllProperties()
    {
        // Arrange
        string columnLeft = "<";
        string columnRight = ">";
        string stringLeft = "\"";
        string stringRight = "\"";
        string parameterPrefix = "#";
        
        // Act
        var dialect = new SqlDialect(columnLeft, columnRight, stringLeft, stringRight, parameterPrefix);
        
        // Assert
        Assert.AreEqual(columnLeft, dialect.ColumnLeft);
        Assert.AreEqual(columnRight, dialect.ColumnRight);
        Assert.AreEqual(stringLeft, dialect.StringLeft);
        Assert.AreEqual(stringRight, dialect.StringRight);
        Assert.AreEqual(parameterPrefix, dialect.ParameterPrefix);
    }

    [TestMethod]
    public void SqlDialect_WrapColumn_WithValidName_WrapsCorrectly()
    {
        // Arrange
        var dialect = SqlDefine.SqlServer;
        string columnName = "UserName";
        
        // Act
        string result = dialect.WrapColumn(columnName);
        
        // Assert
        Assert.AreEqual("[UserName]", result);
    }

    [TestMethod]
    public void SqlDialect_WrapColumn_WithMySql_WrapsCorrectly()
    {
        // Arrange
        var dialect = SqlDefine.MySql;
        string columnName = "UserName";
        
        // Act
        string result = dialect.WrapColumn(columnName);
        
        // Assert
        Assert.AreEqual("`UserName`", result);
    }

    [TestMethod]
    public void SqlDialect_WrapColumn_WithPostgreSQL_WrapsCorrectly()
    {
        // Arrange
        var dialect = SqlDefine.PgSql;
        string columnName = "UserName";
        
        // Act
        string result = dialect.WrapColumn(columnName);
        
        // Assert
        Assert.AreEqual("\"UserName\"", result);
    }

    [TestMethod]
    public void SqlDialect_WrapColumn_WithNullName_ReturnsEmpty()
    {
        // Arrange
        var dialect = SqlDefine.SqlServer;
        
        // Act
        string result = dialect.WrapColumn(null!);
        
        // Assert
        Assert.AreEqual("", result);
    }

    [TestMethod]
    public void SqlDialect_WrapColumn_WithEmptyName_ReturnsWrappedEmpty()
    {
        // Arrange
        var dialect = SqlDefine.SqlServer;
        
        // Act
        string result = dialect.WrapColumn("");
        
        // Assert
        Assert.AreEqual("[]", result);
    }

    [TestMethod]
    public void SqlDialect_WrapColumn_WithWhitespaceName_WrapsWhitespace()
    {
        // Arrange
        var dialect = SqlDefine.SqlServer;
        
        // Act
        string result = dialect.WrapColumn("  ");
        
        // Assert
        Assert.AreEqual("[  ]", result);
    }

    [TestMethod]
    public void SqlDialect_WrapString_WithValidValue_WrapsCorrectly()
    {
        // Arrange
        var dialect = SqlDefine.SqlServer;
        string value = "test value";
        
        // Act
        string result = dialect.WrapString(value);
        
        // Assert
        Assert.AreEqual("'test value'", result);
    }

    [TestMethod]
    public void SqlDialect_WrapString_WithNullValue_ReturnsEmpty()
    {
        // Arrange
        var dialect = SqlDefine.SqlServer;
        
        // Act
        string result = dialect.WrapString(null!);
        
        // Assert
        Assert.AreEqual("NULL", result);
    }

    [TestMethod]
    public void SqlDialect_WrapString_WithEmptyValue_ReturnsWrappedEmpty()
    {
        // Arrange
        var dialect = SqlDefine.SqlServer;
        
        // Act
        string result = dialect.WrapString("");
        
        // Assert
        Assert.AreEqual("''", result);
    }

    [TestMethod]
    public void SqlDialect_WrapString_WithSpecialCharacters_WrapsCorrectly()
    {
        // Arrange
        var dialect = SqlDefine.SqlServer;
        string value = "test'with\"quotes";
        
        // Act
        string result = dialect.WrapString(value);
        
        // Assert
        Assert.AreEqual("'test'with\"quotes'", result);
    }

    [TestMethod]
    public void SqlDialect_Equality_SameValues_ReturnsTrue()
    {
        // Arrange
        var dialect1 = new SqlDialect("[", "]", "'", "'", "@");
        var dialect2 = new SqlDialect("[", "]", "'", "'", "@");
        
        // Act & Assert
        Assert.AreEqual(dialect1, dialect2);
        Assert.IsTrue(dialect1.Equals(dialect2));
        Assert.IsTrue(dialect1 == dialect2);
        Assert.IsFalse(dialect1 != dialect2);
    }

    [TestMethod]
    public void SqlDialect_Equality_DifferentValues_ReturnsFalse()
    {
        // Arrange
        var dialect1 = SqlDefine.SqlServer;
        var dialect2 = SqlDefine.MySql;
        
        // Act & Assert
        Assert.AreNotEqual(dialect1, dialect2);
        Assert.IsFalse(dialect1.Equals(dialect2));
        Assert.IsFalse(dialect1 == dialect2);
        Assert.IsTrue(dialect1 != dialect2);
    }

    [TestMethod]
    public void SqlDialect_GetHashCode_SameValues_ReturnsSameHashCode()
    {
        // Arrange
        var dialect1 = new SqlDialect("[", "]", "'", "'", "@");
        var dialect2 = new SqlDialect("[", "]", "'", "'", "@");
        
        // Act
        int hash1 = dialect1.GetHashCode();
        int hash2 = dialect2.GetHashCode();
        
        // Assert
        Assert.AreEqual(hash1, hash2);
    }

    [TestMethod]
    public void SqlDialect_GetHashCode_DifferentValues_ReturnsDifferentHashCode()
    {
        // Arrange
        var dialect1 = SqlDefine.SqlServer;
        var dialect2 = SqlDefine.MySql;
        
        // Act
        int hash1 = dialect1.GetHashCode();
        int hash2 = dialect2.GetHashCode();
        
        // Assert
        Assert.AreNotEqual(hash1, hash2);
    }

    [TestMethod]
    public void SqlDialect_ToString_ReturnsFormattedString()
    {
        // Arrange
        var dialect = SqlDefine.SqlServer;
        
        // Act
        string result = dialect.ToString();
        
        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Contains("["));
        Assert.IsTrue(result.Contains("]"));
        Assert.IsTrue(result.Contains("@"));
    }

    [TestMethod]
    public void SqlDialect_WithOperator_UpdatesColumnLeft()
    {
        // Arrange
        var original = SqlDefine.SqlServer;
        
        // Act
        var updated = original with { ColumnLeft = "<" };
        
        // Assert
        Assert.AreEqual("<", updated.ColumnLeft);
        Assert.AreEqual("]", updated.ColumnRight);
        Assert.AreEqual("@", updated.ParameterPrefix);
        Assert.AreNotEqual(original, updated);
    }

    [TestMethod]
    public void SqlDialect_WithOperator_UpdatesMultipleProperties()
    {
        // Arrange
        var original = SqlDefine.SqlServer;
        
        // Act
        var updated = original with { ColumnLeft = "<", ColumnRight = ">", ParameterPrefix = "#" };
        
        // Assert
        Assert.AreEqual("<", updated.ColumnLeft);
        Assert.AreEqual(">", updated.ColumnRight);
        Assert.AreEqual("#", updated.ParameterPrefix);
        Assert.AreEqual("'", updated.StringLeft);
        Assert.AreEqual("'", updated.StringRight);
        Assert.AreNotEqual(original, updated);
    }

    [TestMethod]
    public void SqlDialect_Immutability_OriginalUnchangedAfterWith()
    {
        // Arrange
        var original = SqlDefine.SqlServer;
        
        // Act
        var updated = original with { ParameterPrefix = "#" };
        
        // Assert
        Assert.AreEqual("@", original.ParameterPrefix);
        Assert.AreEqual("#", updated.ParameterPrefix);
    }

    [TestMethod]
    public void AllDialects_AreDistinct()
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
        for (int i = 0; i < dialects.Length; i++)
        {
            for (int j = i + 1; j < dialects.Length; j++)
            {
                Assert.AreNotEqual(dialects[i], dialects[j], 
                    $"Dialect {i} should not equal dialect {j}");
            }
        }
    }

    [TestMethod]
    public void AllDialects_HaveValidParameterPrefixes()
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
            Assert.IsFalse(string.IsNullOrEmpty(dialect.ParameterPrefix), 
                "Parameter prefix should not be null or empty");
            Assert.AreEqual(1, dialect.ParameterPrefix.Length, 
                "Parameter prefix should be a single character");
        }
    }

    [TestMethod]
    public void AllDialects_HaveValidColumnWrappers()
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
            Assert.IsFalse(string.IsNullOrEmpty(dialect.ColumnLeft), 
                "ColumnLeft should not be empty");
            Assert.IsFalse(string.IsNullOrEmpty(dialect.ColumnRight), 
                "ColumnRight should not be empty");
        }
    }
}