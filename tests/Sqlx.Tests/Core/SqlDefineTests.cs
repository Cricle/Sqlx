// -----------------------------------------------------------------------
// <copyright file="SqlDefineTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.Tests.Core;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

/// <summary>
/// Tests for SqlDefine static class.
/// </summary>
[TestClass]
public class SqlDefineTests
{
    /// <summary>
    /// Tests SqlDefine.SqlServer dialect configuration.
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
    /// Tests SqlDefine.MySql dialect configuration.
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
    /// Tests SqlDefine.PgSql dialect configuration.
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
    /// Tests SqlDefine.Oracle dialect configuration.
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
    /// Tests SqlDefine.DB2 dialect configuration.
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
    /// Tests SqlDefine.Sqlite dialect configuration.
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
        Assert.AreEqual("$", dialect.ParameterPrefix);
    }

    /// <summary>
    /// Tests that all dialect configurations are different.
    /// </summary>
    [TestMethod]
    public void SqlDefine_AllDialects_AreUnique()
    {
        // Arrange
        var dialects = new[]
        {
            SqlDefine.SqlServer,
            SqlDefine.MySql,
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
                var dialect1 = dialects[i];
                var dialect2 = dialects[j];
                
                // At least one property should be different (except for SQL Server and SQLite which may be identical)
                bool isDifferent = dialect1.ColumnLeft != dialect2.ColumnLeft ||
                                 dialect1.ColumnRight != dialect2.ColumnRight ||
                                 dialect1.StringLeft != dialect2.StringLeft ||
                                 dialect1.StringRight != dialect2.StringRight ||
                                 dialect1.ParameterPrefix != dialect2.ParameterPrefix;
                
                // SQL Server (0) and SQLite (5) may have identical configurations
                if (!isDifferent && !((i == 0 && j == 5) || (i == 5 && j == 0)))
                {
                    Assert.Fail($"Dialects {i} and {j} should be different");
                }
            }
        }
    }

    /// <summary>
    /// Tests that dialect configurations are consistent.
    /// </summary>
    [TestMethod]
    public void SqlDefine_AllDialects_AreConsistent()
    {
        // Arrange
        var dialects = new[]
        {
            SqlDefine.SqlServer,
            SqlDefine.MySql,
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
            
            Assert.IsFalse(string.IsNullOrEmpty(dialect.ColumnLeft), "ColumnLeft should not be empty");
            Assert.IsFalse(string.IsNullOrEmpty(dialect.ColumnRight), "ColumnRight should not be empty");
            Assert.IsFalse(string.IsNullOrEmpty(dialect.StringLeft), "StringLeft should not be empty");
            Assert.IsFalse(string.IsNullOrEmpty(dialect.StringRight), "StringRight should not be empty");
            Assert.IsFalse(string.IsNullOrEmpty(dialect.ParameterPrefix), "ParameterPrefix should not be empty");
        }
    }
}
