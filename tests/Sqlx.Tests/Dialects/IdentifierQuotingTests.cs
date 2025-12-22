// -----------------------------------------------------------------------
// <copyright file="IdentifierQuotingTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Generator;

namespace Sqlx.Tests.Dialects;

/// <summary>
/// 标识符引用语法验证测试
/// 验证各数据库方言的WrapColumn方法使用正确的引用符号
/// Requirements: 1.1, 1.2, 1.3, 1.4, 1.5
/// </summary>
[TestClass]
public class IdentifierQuotingTests
{
    #region 2.1 MySQL标识符引用测试 - Requirements 1.1

    /// <summary>
    /// MySQL应该使用反引号(`)包裹标识符
    /// </summary>
    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("MySQL")]
    [TestCategory("IdentifierQuoting")]
    public void MySQL_WrapColumn_ShouldUseBackticks()
    {
        // Arrange
        var dialect = SqlDefine.MySql;
        var columnName = "user_name";

        // Act
        var result = dialect.WrapColumn(columnName);

        // Assert
        Assert.AreEqual("`user_name`", result, "MySQL应该使用反引号包裹标识符");
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("MySQL")]
    public void MySQL_WrapColumn_TableName_ShouldUseBackticks()
    {
        var dialect = SqlDefine.MySql;
        var tableName = "users";
        var result = dialect.WrapColumn(tableName);
        Assert.AreEqual("`users`", result);
    }


    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("MySQL")]
    public void MySQL_WrapColumn_ReservedWord_ShouldUseBackticks()
    {
        var dialect = SqlDefine.MySql;
        var reservedWord = "select";
        var result = dialect.WrapColumn(reservedWord);
        Assert.AreEqual("`select`", result);
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("MySQL")]
    public void MySQL_ColumnDelimiters_ShouldBeBackticks()
    {
        var dialect = SqlDefine.MySql;
        Assert.AreEqual("`", dialect.ColumnLeft);
        Assert.AreEqual("`", dialect.ColumnRight);
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("MySQL")]
    public void MySQL_WrapColumn_EmptyString_ShouldReturnEmpty()
    {
        var dialect = SqlDefine.MySql;
        var result = dialect.WrapColumn("");
        Assert.AreEqual("", result);
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("MySQL")]
    public void MySQL_WrapColumn_WithUnderscore_ShouldUseBackticks()
    {
        var dialect = SqlDefine.MySql;
        var columnName = "created_at";
        var result = dialect.WrapColumn(columnName);
        Assert.AreEqual("`created_at`", result);
    }

    #endregion

    #region 2.2 PostgreSQL标识符引用测试 - Requirements 1.2

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("PostgreSQL")]
    public void PostgreSQL_WrapColumn_ShouldUseDoubleQuotes()
    {
        var dialect = SqlDefine.PostgreSql;
        var columnName = "user_name";
        var result = dialect.WrapColumn(columnName);
        Assert.AreEqual("\"user_name\"", result);
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("PostgreSQL")]
    public void PostgreSQL_WrapColumn_TableName_ShouldUseDoubleQuotes()
    {
        var dialect = SqlDefine.PostgreSql;
        var tableName = "users";
        var result = dialect.WrapColumn(tableName);
        Assert.AreEqual("\"users\"", result);
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("PostgreSQL")]
    public void PostgreSQL_ColumnDelimiters_ShouldBeDoubleQuotes()
    {
        var dialect = SqlDefine.PostgreSql;
        Assert.AreEqual("\"", dialect.ColumnLeft);
        Assert.AreEqual("\"", dialect.ColumnRight);
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("PostgreSQL")]
    public void PostgreSQL_WrapColumn_ReservedWord_ShouldUseDoubleQuotes()
    {
        var dialect = SqlDefine.PostgreSql;
        var reservedWord = "select";
        var result = dialect.WrapColumn(reservedWord);
        Assert.AreEqual("\"select\"", result);
    }

    #endregion

    #region 2.3 SQL Server标识符引用测试 - Requirements 1.3

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("SqlServer")]
    public void SqlServer_WrapColumn_ShouldUseSquareBrackets()
    {
        var dialect = SqlDefine.SqlServer;
        var columnName = "user_name";
        var result = dialect.WrapColumn(columnName);
        Assert.AreEqual("[user_name]", result);
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("SqlServer")]
    public void SqlServer_WrapColumn_TableName_ShouldUseSquareBrackets()
    {
        var dialect = SqlDefine.SqlServer;
        var tableName = "users";
        var result = dialect.WrapColumn(tableName);
        Assert.AreEqual("[users]", result);
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("SqlServer")]
    public void SqlServer_ColumnDelimiters_ShouldBeSquareBrackets()
    {
        var dialect = SqlDefine.SqlServer;
        Assert.AreEqual("[", dialect.ColumnLeft);
        Assert.AreEqual("]", dialect.ColumnRight);
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("SqlServer")]
    public void SqlServer_WrapColumn_ReservedWord_ShouldUseSquareBrackets()
    {
        var dialect = SqlDefine.SqlServer;
        var reservedWord = "select";
        var result = dialect.WrapColumn(reservedWord);
        Assert.AreEqual("[select]", result);
    }

    #endregion


    #region 2.4 SQLite标识符引用测试 - Requirements 1.4

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("SQLite")]
    public void SQLite_WrapColumn_ShouldUseSquareBracketsOrDoubleQuotes()
    {
        var dialect = SqlDefine.SQLite;
        var columnName = "user_name";
        var result = dialect.WrapColumn(columnName);
        Assert.IsTrue(
            result == "[user_name]" || result == "\"user_name\"",
            $"SQLite应该使用方括号或双引号包裹标识符，实际结果: {result}");
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("SQLite")]
    public void SQLite_WrapColumn_TableName_ShouldUseSquareBracketsOrDoubleQuotes()
    {
        var dialect = SqlDefine.SQLite;
        var tableName = "users";
        var result = dialect.WrapColumn(tableName);
        Assert.IsTrue(
            result == "[users]" || result == "\"users\"",
            $"SQLite应该使用方括号或双引号包裹表名，实际结果: {result}");
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("SQLite")]
    public void SQLite_ColumnDelimiters_ShouldBeSquareBracketsOrDoubleQuotes()
    {
        var dialect = SqlDefine.SQLite;
        Assert.IsTrue(
            (dialect.ColumnLeft == "[" && dialect.ColumnRight == "]") ||
            (dialect.ColumnLeft == "\"" && dialect.ColumnRight == "\""),
            $"SQLite应该使用方括号或双引号，实际: Left={dialect.ColumnLeft}, Right={dialect.ColumnRight}");
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("SQLite")]
    public void SQLite_WrapColumn_ReservedWord_ShouldUseSquareBracketsOrDoubleQuotes()
    {
        var dialect = SqlDefine.SQLite;
        var reservedWord = "select";
        var result = dialect.WrapColumn(reservedWord);
        Assert.IsTrue(
            result == "[select]" || result == "\"select\"",
            $"SQLite应该使用方括号或双引号包裹保留字，实际结果: {result}");
    }

    #endregion

    #region Oracle标识符引用测试 - Requirements 1.5

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("Oracle")]
    public void Oracle_WrapColumn_ShouldUseDoubleQuotes()
    {
        var dialect = SqlDefine.Oracle;
        var columnName = "user_name";
        var result = dialect.WrapColumn(columnName);
        Assert.AreEqual("\"user_name\"", result);
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("Oracle")]
    public void Oracle_ColumnDelimiters_ShouldBeDoubleQuotes()
    {
        var dialect = SqlDefine.Oracle;
        Assert.AreEqual("\"", dialect.ColumnLeft);
        Assert.AreEqual("\"", dialect.ColumnRight);
    }

    #endregion

    #region 跨方言对比测试

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("MultiDialect")]
    public void AllDialects_WrapColumn_ShouldWrapIdentifiersCorrectly()
    {
        var testCases = new[]
        {
            (Dialect: SqlDefine.MySql, Name: "MySQL", ExpectedLeft: "`", ExpectedRight: "`"),
            (Dialect: SqlDefine.PostgreSql, Name: "PostgreSQL", ExpectedLeft: "\"", ExpectedRight: "\""),
            (Dialect: SqlDefine.SqlServer, Name: "SQL Server", ExpectedLeft: "[", ExpectedRight: "]"),
            (Dialect: SqlDefine.SQLite, Name: "SQLite", ExpectedLeft: "[", ExpectedRight: "]"),
            (Dialect: SqlDefine.Oracle, Name: "Oracle", ExpectedLeft: "\"", ExpectedRight: "\""),
        };

        var columnName = "test_column";

        foreach (var testCase in testCases)
        {
            var result = testCase.Dialect.WrapColumn(columnName);
            var expected = $"{testCase.ExpectedLeft}{columnName}{testCase.ExpectedRight}";
            Assert.AreEqual(expected, result, $"{testCase.Name}应该正确包裹标识符");
        }
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("MultiDialect")]
    public void AllDialects_WrapColumn_EmptyString_ShouldReturnEmpty()
    {
        var dialects = new[]
        {
            SqlDefine.MySql,
            SqlDefine.PostgreSql,
            SqlDefine.SqlServer,
            SqlDefine.SQLite,
            SqlDefine.Oracle,
        };

        foreach (var dialect in dialects)
        {
            var result = dialect.WrapColumn("");
            Assert.AreEqual("", result, $"{dialect.DatabaseType}对空字符串应该返回空字符串");
        }
    }

    #endregion
}
