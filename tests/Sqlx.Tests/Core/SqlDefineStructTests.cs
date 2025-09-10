// -----------------------------------------------------------------------
// <copyright file="SqlDefineStructTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sqlx.Tests.Core;

[TestClass]
public class SqlDefineStructTests
{
    [TestMethod]
    public void SqlDefine_Constructor_SetsAllProperties()
    {
        // Arrange
        var columnLeft = "[";
        var columnRight = "]";
        var stringLeft = "'";
        var stringRight = "'";
        var parameterPrefix = "@";

        // Act
        var sqlDefine = new SqlDefine(columnLeft, columnRight, stringLeft, stringRight, parameterPrefix);

        // Assert
        Assert.AreEqual(columnLeft, sqlDefine.ColumnLeft);
        Assert.AreEqual(columnRight, sqlDefine.ColumnRight);
        Assert.AreEqual(stringLeft, sqlDefine.StringLeft);
        Assert.AreEqual(stringRight, sqlDefine.StringRight);
        Assert.AreEqual(parameterPrefix, sqlDefine.ParameterPrefix);
    }

    [TestMethod]
    public void SqlDefine_Deconstruct_ReturnsAllValues()
    {
        // Arrange
        var sqlDefine = SqlDefine.MySql;

        // Act
        var (columnLeft, columnRight, stringLeft, stringRight, parameterPrefix) = sqlDefine;

        // Assert
        Assert.AreEqual("`", columnLeft);
        Assert.AreEqual("`", columnRight);
        Assert.AreEqual("'", stringLeft);
        Assert.AreEqual("'", stringRight);
        Assert.AreEqual("@", parameterPrefix);
    }

    [TestMethod]
    public void SqlDefine_MySql_HasCorrectValues()
    {
        // Arrange & Act
        var sqlDefine = SqlDefine.MySql;

        // Assert
        Assert.AreEqual("`", sqlDefine.ColumnLeft);
        Assert.AreEqual("`", sqlDefine.ColumnRight);
        Assert.AreEqual("'", sqlDefine.StringLeft);
        Assert.AreEqual("'", sqlDefine.StringRight);
        Assert.AreEqual("@", sqlDefine.ParameterPrefix);
    }

    [TestMethod]
    public void SqlDefine_SqlServer_HasCorrectValues()
    {
        // Arrange & Act
        var sqlDefine = SqlDefine.SqlServer;

        // Assert
        Assert.AreEqual("[", sqlDefine.ColumnLeft);
        Assert.AreEqual("]", sqlDefine.ColumnRight);
        Assert.AreEqual("'", sqlDefine.StringLeft);
        Assert.AreEqual("'", sqlDefine.StringRight);
        Assert.AreEqual("@", sqlDefine.ParameterPrefix);
    }

    [TestMethod]
    public void SqlDefine_PgSql_HasCorrectValues()
    {
        // Arrange & Act
        var sqlDefine = SqlDefine.PgSql;

        // Assert
        Assert.AreEqual("\"", sqlDefine.ColumnLeft);
        Assert.AreEqual("\"", sqlDefine.ColumnRight);
        Assert.AreEqual("'", sqlDefine.StringLeft);
        Assert.AreEqual("'", sqlDefine.StringRight);
        Assert.AreEqual("$", sqlDefine.ParameterPrefix);
    }

    [TestMethod]
    public void SqlDefine_Oracle_HasCorrectValues()
    {
        // Arrange & Act
        var sqlDefine = SqlDefine.Oracle;

        // Assert
        Assert.AreEqual("\"", sqlDefine.ColumnLeft);
        Assert.AreEqual("\"", sqlDefine.ColumnRight);
        Assert.AreEqual("'", sqlDefine.StringLeft);
        Assert.AreEqual("'", sqlDefine.StringRight);
        Assert.AreEqual(":", sqlDefine.ParameterPrefix);
    }

    [TestMethod]
    public void SqlDefine_DB2_HasCorrectValues()
    {
        // Arrange & Act
        var sqlDefine = SqlDefine.DB2;

        // Assert
        Assert.AreEqual("\"", sqlDefine.ColumnLeft);
        Assert.AreEqual("\"", sqlDefine.ColumnRight);
        Assert.AreEqual("'", sqlDefine.StringLeft);
        Assert.AreEqual("'", sqlDefine.StringRight);
        Assert.AreEqual("?", sqlDefine.ParameterPrefix);
    }

    [TestMethod]
    public void SqlDefine_SQLite_HasCorrectValues()
    {
        // Arrange & Act
        var sqlDefine = SqlDefine.SQLite;

        // Assert
        Assert.AreEqual("[", sqlDefine.ColumnLeft);
        Assert.AreEqual("]", sqlDefine.ColumnRight);
        Assert.AreEqual("'", sqlDefine.StringLeft);
        Assert.AreEqual("'", sqlDefine.StringRight);
        Assert.AreEqual("@sqlite", sqlDefine.ParameterPrefix);
    }

    [TestMethod]
    public void SqlDefine_WrapColumn_WithSimpleColumn_ReturnsWrapped()
    {
        // Arrange
        var sqlDefine = SqlDefine.MySql;
        var columnName = "user_id";

        // Act
        var result = sqlDefine.WrapColumn(columnName);

        // Assert
        Assert.AreEqual("`user_id`", result);
    }

    [TestMethod]
    public void SqlDefine_WrapColumn_WithEmptyString_ReturnsWrapped()
    {
        // Arrange
        var sqlDefine = SqlDefine.SqlServer;
        var columnName = string.Empty;

        // Act
        var result = sqlDefine.WrapColumn(columnName);

        // Assert
        Assert.AreEqual("[]", result);
    }

    [TestMethod]
    public void SqlDefine_WrapString_WithSimpleString_ReturnsWrapped()
    {
        // Arrange
        var sqlDefine = SqlDefine.PgSql;
        var value = "test value";

        // Act
        var result = sqlDefine.WrapString(value);

        // Assert
        Assert.AreEqual("'test value'", result);
    }

    [TestMethod]
    public void SqlDefine_WrapString_WithEmptyString_ReturnsWrapped()
    {
        // Arrange
        var sqlDefine = SqlDefine.Oracle;
        var value = string.Empty;

        // Act
        var result = sqlDefine.WrapString(value);

        // Assert
        Assert.AreEqual("''", result);
    }

    [TestMethod]
    public void SqlDefine_Equality_SameValues_ReturnsTrue()
    {
        // Arrange
        var sqlDefine1 = new SqlDefine("[", "]", "'", "'", "@");
        var sqlDefine2 = new SqlDefine("[", "]", "'", "'", "@");

        // Act & Assert
        Assert.IsTrue(sqlDefine1.Equals(sqlDefine2));
        Assert.IsTrue(sqlDefine1 == sqlDefine2);
        Assert.IsFalse(sqlDefine1 != sqlDefine2);
    }

    [TestMethod]
    public void SqlDefine_Equality_DifferentValues_ReturnsFalse()
    {
        // Arrange
        var sqlDefine1 = SqlDefine.MySql;
        var sqlDefine2 = SqlDefine.SqlServer;

        // Act & Assert
        Assert.IsFalse(sqlDefine1.Equals(sqlDefine2));
        Assert.IsFalse(sqlDefine1 == sqlDefine2);
        Assert.IsTrue(sqlDefine1 != sqlDefine2);
    }

    [TestMethod]
    public void SqlDefine_GetHashCode_SameValues_ReturnsSameHash()
    {
        // Arrange
        var sqlDefine1 = new SqlDefine("[", "]", "'", "'", "@");
        var sqlDefine2 = new SqlDefine("[", "]", "'", "'", "@");

        // Act
        var hash1 = sqlDefine1.GetHashCode();
        var hash2 = sqlDefine2.GetHashCode();

        // Assert
        Assert.AreEqual(hash1, hash2);
    }

    [TestMethod]
    public void SqlDefine_GetHashCode_DifferentValues_ReturnsDifferentHash()
    {
        // Arrange
        var sqlDefine1 = SqlDefine.MySql;
        var sqlDefine2 = SqlDefine.SqlServer;

        // Act
        var hash1 = sqlDefine1.GetHashCode();
        var hash2 = sqlDefine2.GetHashCode();

        // Assert
        Assert.AreNotEqual(hash1, hash2);
    }

    [TestMethod]
    public void SqlDefine_ToString_ReturnsReadableString()
    {
        // Arrange
        var sqlDefine = SqlDefine.MySql;

        // Act
        var result = sqlDefine.ToString();

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Length > 0);
        // Should contain the actual values
        Assert.IsTrue(result.Contains("`") || result.Contains("@"));
    }
}
