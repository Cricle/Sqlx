// -----------------------------------------------------------------------
// <copyright file="DialectComprehensiveTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;

namespace Sqlx.Tests;

/// <summary>
/// Comprehensive tests for all SQL dialect implementations to achieve 100% branch coverage.
/// Tests focus on uncovered branches and edge cases.
/// </summary>
[TestClass]
public class DialectComprehensiveTests
{
    #region DB2 Dialect - Uncovered Branches

    [TestMethod]
    public void DB2_CurrentDate_ReturnsCorrectSyntax()
    {
        // Arrange & Act
        var result = SqlDefine.DB2.CurrentDate;

        // Assert
        Assert.AreEqual("CURRENT DATE", result);
    }

    [TestMethod]
    public void DB2_CurrentTime_ReturnsCorrectSyntax()
    {
        // Arrange & Act
        var result = SqlDefine.DB2.CurrentTime;

        // Assert
        Assert.AreEqual("CURRENT TIME", result);
    }

    [TestMethod]
    public void DB2_DateAdd_ReturnsCorrectSyntax()
    {
        // Arrange & Act
        var result = SqlDefine.DB2.DateAdd("DAY", "5", "col");

        // Assert
        Assert.AreEqual("(col + 5 DAY)", result);
    }

    [TestMethod]
    public void DB2_LastInsertedId_ReturnsCorrectSyntax()
    {
        // Arrange & Act
        var result = SqlDefine.DB2.LastInsertedId;

        // Assert
        Assert.AreEqual("SELECT IDENTITY_VAL_LOCAL() FROM SYSIBM.SYSDUMMY1", result);
    }

    [TestMethod]
    public void DB2_DateDiff_Year_ReturnsCorrectCode()
    {
        // Arrange & Act
        var result = SqlDefine.DB2.DateDiff("YEAR", "start", "end");

        // Assert
        Assert.IsTrue(result.Contains("TIMESTAMPDIFF(256"));
    }

    [TestMethod]
    public void DB2_DateDiff_Month_ReturnsCorrectCode()
    {
        // Arrange & Act
        var result = SqlDefine.DB2.DateDiff("MONTH", "start", "end");

        // Assert
        Assert.IsTrue(result.Contains("TIMESTAMPDIFF(64"));
    }

    [TestMethod]
    public void DB2_DateDiff_Day_ReturnsCorrectCode()
    {
        // Arrange & Act
        var result = SqlDefine.DB2.DateDiff("DAY", "start", "end");

        // Assert
        Assert.IsTrue(result.Contains("TIMESTAMPDIFF(16"));
    }

    [TestMethod]
    public void DB2_DateDiff_Hour_ReturnsCorrectCode()
    {
        // Arrange & Act
        var result = SqlDefine.DB2.DateDiff("HOUR", "start", "end");

        // Assert
        Assert.IsTrue(result.Contains("TIMESTAMPDIFF(8"));
    }

    [TestMethod]
    public void DB2_DateDiff_Minute_ReturnsCorrectCode()
    {
        // Arrange & Act
        var result = SqlDefine.DB2.DateDiff("MINUTE", "start", "end");

        // Assert
        Assert.IsTrue(result.Contains("TIMESTAMPDIFF(4"));
    }

    [TestMethod]
    public void DB2_DateDiff_Second_ReturnsCorrectCode()
    {
        // Arrange & Act
        var result = SqlDefine.DB2.DateDiff("SECOND", "start", "end");

        // Assert
        Assert.IsTrue(result.Contains("TIMESTAMPDIFF(2"));
    }

    [TestMethod]
    public void DB2_DateDiff_UnknownInterval_ReturnsDefaultCode()
    {
        // Arrange & Act
        var result = SqlDefine.DB2.DateDiff("UNKNOWN", "start", "end");

        // Assert
        Assert.IsTrue(result.Contains("TIMESTAMPDIFF(16")); // Default to days (16)
    }

    [TestMethod]
    public void DB2_DateDiff_LowercaseInterval_ReturnsCorrectCode()
    {
        // Arrange & Act
        var result = SqlDefine.DB2.DateDiff("year", "start", "end");

        // Assert
        Assert.IsTrue(result.Contains("TIMESTAMPDIFF(256"));
    }

    #endregion

    #region Oracle Dialect - Uncovered Branches

    [TestMethod]
    public void Oracle_CurrentDate_ReturnsCorrectSyntax()
    {
        // Arrange & Act
        var result = SqlDefine.Oracle.CurrentDate;

        // Assert
        Assert.AreEqual("SYSDATE", result);
    }

    [TestMethod]
    public void Oracle_CurrentTime_ReturnsCorrectSyntax()
    {
        // Arrange & Act
        var result = SqlDefine.Oracle.CurrentTime;

        // Assert
        Assert.AreEqual("TO_CHAR(SYSDATE, 'HH24:MI:SS')", result);
    }

    [TestMethod]
    public void Oracle_DateAdd_Day_ReturnsCorrectSyntax()
    {
        // Arrange & Act
        var result = SqlDefine.Oracle.DateAdd("DAY", "5", "col");

        // Assert
        Assert.AreEqual("(col + 5)", result);
    }

    [TestMethod]
    public void Oracle_DateAdd_Month_ReturnsCorrectSyntax()
    {
        // Arrange & Act
        var result = SqlDefine.Oracle.DateAdd("MONTH", "3", "col");

        // Assert
        Assert.AreEqual("ADD_MONTHS(col, 3)", result);
    }

    [TestMethod]
    public void Oracle_DateAdd_Year_ReturnsCorrectSyntax()
    {
        // Arrange & Act
        var result = SqlDefine.Oracle.DateAdd("YEAR", "2", "col");

        // Assert
        Assert.AreEqual("ADD_MONTHS(col, 2 * 12)", result);
    }

    [TestMethod]
    public void Oracle_DateAdd_Hour_ReturnsCorrectSyntax()
    {
        // Arrange & Act
        var result = SqlDefine.Oracle.DateAdd("HOUR", "6", "col");

        // Assert
        Assert.AreEqual("(col + 6/24)", result);
    }

    [TestMethod]
    public void Oracle_DateAdd_Minute_ReturnsCorrectSyntax()
    {
        // Arrange & Act
        var result = SqlDefine.Oracle.DateAdd("MINUTE", "30", "col");

        // Assert
        Assert.AreEqual("(col + 30/1440)", result);
    }

    [TestMethod]
    public void Oracle_DateAdd_Second_ReturnsCorrectSyntax()
    {
        // Arrange & Act
        var result = SqlDefine.Oracle.DateAdd("SECOND", "45", "col");

        // Assert
        Assert.AreEqual("(col + 45/86400)", result);
    }

    [TestMethod]
    public void Oracle_DateAdd_UnknownInterval_ReturnsDefaultSyntax()
    {
        // Arrange & Act
        var result = SqlDefine.Oracle.DateAdd("WEEK", "2", "col");

        // Assert
        Assert.AreEqual("(col + INTERVAL '2' WEEK)", result);
    }

    [TestMethod]
    public void Oracle_LastInsertedId_ReturnsCorrectSyntax()
    {
        // Arrange & Act
        var result = SqlDefine.Oracle.LastInsertedId;

        // Assert
        Assert.AreEqual("SELECT SEQ.CURRVAL FROM DUAL", result);
    }

    [TestMethod]
    public void Oracle_InsertReturningIdSuffix_ReturnsCorrectSyntax()
    {
        // Arrange & Act
        var result = SqlDefine.Oracle.InsertReturningIdSuffix;

        // Assert
        Assert.AreEqual(" RETURNING id INTO :id", result);
    }

    [TestMethod]
    public void Oracle_DateDiff_Day_ReturnsCorrectSyntax()
    {
        // Arrange & Act
        var result = SqlDefine.Oracle.DateDiff("DAY", "start", "end");

        // Assert
        Assert.AreEqual("(end - start)", result);
    }

    [TestMethod]
    public void Oracle_DateDiff_Month_ReturnsCorrectSyntax()
    {
        // Arrange & Act
        var result = SqlDefine.Oracle.DateDiff("MONTH", "start", "end");

        // Assert
        Assert.AreEqual("MONTHS_BETWEEN(end, start)", result);
    }

    [TestMethod]
    public void Oracle_DateDiff_UnknownInterval_ReturnsDefaultSyntax()
    {
        // Arrange & Act
        var result = SqlDefine.Oracle.DateDiff("YEAR", "start", "end");

        // Assert
        Assert.AreEqual("(end - start)", result);
    }

    #endregion

    #region MySQL Dialect - Uncovered Branches

    [TestMethod]
    public void MySql_CurrentDate_ReturnsCorrectSyntax()
    {
        // Arrange & Act
        var result = SqlDefine.MySql.CurrentDate;

        // Assert
        Assert.AreEqual("CURDATE()", result);
    }

    [TestMethod]
    public void MySql_CurrentTime_ReturnsCorrectSyntax()
    {
        // Arrange & Act
        var result = SqlDefine.MySql.CurrentTime;

        // Assert
        Assert.AreEqual("CURTIME()", result);
    }

    [TestMethod]
    public void MySql_LastInsertedId_ReturnsCorrectSyntax()
    {
        // Arrange & Act
        var result = SqlDefine.MySql.LastInsertedId;

        // Assert
        Assert.AreEqual("SELECT LAST_INSERT_ID()", result);
    }

    [TestMethod]
    public void MySql_DateDiff_ReturnsCorrectSyntax()
    {
        // Arrange & Act
        var result = SqlDefine.MySql.DateDiff("DAY", "start", "end");

        // Assert
        Assert.IsTrue(result.Contains("TIMESTAMPDIFF"));
        Assert.IsTrue(result.Contains("DAY"));
    }

    [TestMethod]
    public void MySql_DatePart_ReturnsCorrectSyntax()
    {
        // Arrange & Act
        var result = SqlDefine.MySql.DatePart("YEAR", "col");

        // Assert
        Assert.IsTrue(result.Contains("EXTRACT"));
        Assert.IsTrue(result.Contains("YEAR"));
    }

    [TestMethod]
    public void MySql_Mod_ReturnsCorrectSyntax()
    {
        // Arrange & Act
        var result = SqlDefine.MySql.Mod("10", "3");

        // Assert
        Assert.AreEqual("MOD(10, 3)", result);
    }

    [TestMethod]
    public void MySql_Iif_ReturnsCorrectSyntax()
    {
        // Arrange & Act
        var result = SqlDefine.MySql.Iif("a > b", "1", "0");

        // Assert
        Assert.AreEqual("IF(a > b, 1, 0)", result);
    }

    [TestMethod]
    public void MySql_IfNull_ReturnsCorrectSyntax()
    {
        // Arrange & Act
        var result = SqlDefine.MySql.IfNull("col", "'default'");

        // Assert
        Assert.AreEqual("IFNULL(col, 'default')", result);
    }

    #endregion

    #region PostgreSQL Dialect - Uncovered Branches

    [TestMethod]
    public void PostgreSql_CurrentDate_ReturnsCorrectSyntax()
    {
        // Arrange & Act
        var result = SqlDefine.PostgreSql.CurrentDate;

        // Assert
        Assert.AreEqual("CURRENT_DATE", result);
    }

    [TestMethod]
    public void PostgreSql_CurrentTime_ReturnsCorrectSyntax()
    {
        // Arrange & Act
        var result = SqlDefine.PostgreSql.CurrentTime;

        // Assert
        Assert.AreEqual("CURRENT_TIME", result);
    }

    [TestMethod]
    public void PostgreSql_LastInsertedId_ReturnsCorrectSyntax()
    {
        // Arrange & Act
        var result = SqlDefine.PostgreSql.LastInsertedId;

        // Assert
        Assert.AreEqual("SELECT lastval()", result);
    }

    [TestMethod]
    public void PostgreSql_InsertReturningIdSuffix_ReturnsCorrectSyntax()
    {
        // Arrange & Act
        var result = SqlDefine.PostgreSql.InsertReturningIdSuffix;

        // Assert
        Assert.AreEqual(" RETURNING id", result);
    }

    [TestMethod]
    public void PostgreSql_DateDiff_ReturnsCorrectSyntax()
    {
        // Arrange & Act
        var result = SqlDefine.PostgreSql.DateDiff("DAY", "start", "end");

        // Assert
        Assert.IsTrue(result.Contains("EXTRACT"));
        Assert.IsTrue(result.Contains("DAY"));
    }

    [TestMethod]
    public void PostgreSql_DatePart_ReturnsCorrectSyntax()
    {
        // Arrange & Act
        var result = SqlDefine.PostgreSql.DatePart("YEAR", "col");

        // Assert
        Assert.IsTrue(result.Contains("EXTRACT"));
        Assert.IsTrue(result.Contains("YEAR"));
    }

    [TestMethod]
    public void PostgreSql_DateAdd_ReturnsCorrectSyntax()
    {
        // Arrange & Act
        var result = SqlDefine.PostgreSql.DateAdd("DAY", "5", "col");

        // Assert
        Assert.AreEqual("(col + INTERVAL '5 DAY')", result);
    }

    [TestMethod]
    public void PostgreSql_Ceiling_ReturnsCorrectSyntax()
    {
        // Arrange & Act
        var result = SqlDefine.PostgreSql.Ceiling("col");

        // Assert
        Assert.AreEqual("CEIL(col)", result);
    }

    [TestMethod]
    public void PostgreSql_Mod_ReturnsCorrectSyntax()
    {
        // Arrange & Act
        var result = SqlDefine.PostgreSql.Mod("10", "3");

        // Assert
        Assert.AreEqual("MOD(10, 3)", result);
    }

    [TestMethod]
    public void PostgreSql_Cast_ReturnsCorrectSyntax()
    {
        // Arrange & Act
        var result = SqlDefine.PostgreSql.Cast("col", "VARCHAR(100)");

        // Assert
        Assert.AreEqual("(col)::VARCHAR(100)", result);
    }

    [TestMethod]
    public void PostgreSql_BoolTrue_ReturnsCorrectSyntax()
    {
        // Arrange & Act
        var result = SqlDefine.PostgreSql.BoolTrue;

        // Assert
        Assert.AreEqual("true", result);
    }

    [TestMethod]
    public void PostgreSql_BoolFalse_ReturnsCorrectSyntax()
    {
        // Arrange & Act
        var result = SqlDefine.PostgreSql.BoolFalse;

        // Assert
        Assert.AreEqual("false", result);
    }

    #endregion

    #region SQLite Dialect - Uncovered Branches

    [TestMethod]
    public void SQLite_CurrentDate_ReturnsCorrectSyntax()
    {
        // Arrange & Act
        var result = SqlDefine.SQLite.CurrentDate;

        // Assert
        Assert.AreEqual("DATE('now')", result);
    }

    [TestMethod]
    public void SQLite_CurrentTime_ReturnsCorrectSyntax()
    {
        // Arrange & Act
        var result = SqlDefine.SQLite.CurrentTime;

        // Assert
        Assert.AreEqual("TIME('now')", result);
    }

    [TestMethod]
    public void SQLite_LastInsertedId_ReturnsCorrectSyntax()
    {
        // Arrange & Act
        var result = SqlDefine.SQLite.LastInsertedId;

        // Assert
        Assert.AreEqual("SELECT last_insert_rowid()", result);
    }

    [TestMethod]
    public void SQLite_DateAdd_ReturnsCorrectSyntax()
    {
        // Arrange & Act
        var result = SqlDefine.SQLite.DateAdd("DAY", "5", "col");

        // Assert
        Assert.AreEqual("DATETIME(col, '+5 DAY')", result);
    }

    [TestMethod]
    public void SQLite_DateDiff_ReturnsCorrectSyntax()
    {
        // Arrange & Act
        var result = SqlDefine.SQLite.DateDiff("DAY", "start", "end");

        // Assert
        Assert.IsTrue(result.Contains("JULIANDAY"));
    }

    [TestMethod]
    public void SQLite_DatePart_Year_ReturnsCorrectSyntax()
    {
        // Arrange & Act
        var result = SqlDefine.SQLite.DatePart("YEAR", "col");

        // Assert
        Assert.IsTrue(result.Contains("STRFTIME"));
        Assert.IsTrue(result.Contains("%Y"));
    }

    [TestMethod]
    public void SQLite_DatePart_Month_ReturnsCorrectSyntax()
    {
        // Arrange & Act
        var result = SqlDefine.SQLite.DatePart("MONTH", "col");

        // Assert
        Assert.IsTrue(result.Contains("STRFTIME"));
        Assert.IsTrue(result.Contains("%m"));
    }

    [TestMethod]
    public void SQLite_DatePart_Day_ReturnsCorrectSyntax()
    {
        // Arrange & Act
        var result = SqlDefine.SQLite.DatePart("DAY", "col");

        // Assert
        Assert.IsTrue(result.Contains("STRFTIME"));
        Assert.IsTrue(result.Contains("%d"));
    }

    [TestMethod]
    public void SQLite_DatePart_Hour_ReturnsCorrectSyntax()
    {
        // Arrange & Act
        var result = SqlDefine.SQLite.DatePart("HOUR", "col");

        // Assert
        Assert.IsTrue(result.Contains("STRFTIME"));
        Assert.IsTrue(result.Contains("%H"));
    }

    [TestMethod]
    public void SQLite_DatePart_Minute_ReturnsCorrectSyntax()
    {
        // Arrange & Act
        var result = SqlDefine.SQLite.DatePart("MINUTE", "col");

        // Assert
        Assert.IsTrue(result.Contains("STRFTIME"));
        Assert.IsTrue(result.Contains("%M"));
    }

    [TestMethod]
    public void SQLite_DatePart_Second_ReturnsCorrectSyntax()
    {
        // Arrange & Act
        var result = SqlDefine.SQLite.DatePart("SECOND", "col");

        // Assert
        Assert.IsTrue(result.Contains("STRFTIME"));
        Assert.IsTrue(result.Contains("%S"));
    }

    [TestMethod]
    public void SQLite_DatePart_UnknownPart_ReturnsDefaultSyntax()
    {
        // Arrange & Act
        var result = SqlDefine.SQLite.DatePart("UNKNOWN", "col");

        // Assert
        Assert.IsTrue(result.Contains("STRFTIME"));
        Assert.IsTrue(result.Contains("%UNKNOWN"));
    }

    [TestMethod]
    public void SQLite_IfNull_ReturnsCorrectSyntax()
    {
        // Arrange & Act
        var result = SqlDefine.SQLite.IfNull("col", "'default'");

        // Assert
        Assert.AreEqual("IFNULL(col, 'default')", result);
    }

    [TestMethod]
    public void SQLite_Mod_ReturnsCorrectSyntax()
    {
        // Arrange & Act
        var result = SqlDefine.SQLite.Mod("10", "3");

        // Assert
        Assert.AreEqual("MOD(10, 3)", result);
    }

    #endregion

    #region SQL Server Dialect - Uncovered Branches

    [TestMethod]
    public void SqlServer_CurrentDate_ReturnsCorrectSyntax()
    {
        // Arrange & Act
        var result = SqlDefine.SqlServer.CurrentDate;

        // Assert
        Assert.AreEqual("CAST(GETDATE() AS DATE)", result);
    }

    [TestMethod]
    public void SqlServer_CurrentTime_ReturnsCorrectSyntax()
    {
        // Arrange & Act
        var result = SqlDefine.SqlServer.CurrentTime;

        // Assert
        Assert.AreEqual("CAST(GETDATE() AS TIME)", result);
    }

    [TestMethod]
    public void SqlServer_LastInsertedId_ReturnsCorrectSyntax()
    {
        // Arrange & Act
        var result = SqlDefine.SqlServer.LastInsertedId;

        // Assert
        Assert.AreEqual("SELECT SCOPE_IDENTITY()", result);
    }

    [TestMethod]
    public void SqlServer_DateDiff_ReturnsCorrectSyntax()
    {
        // Arrange & Act
        var result = SqlDefine.SqlServer.DateDiff("DAY", "start", "end");

        // Assert
        Assert.IsTrue(result.Contains("DATEDIFF"));
        Assert.IsTrue(result.Contains("DAY"));
    }

    [TestMethod]
    public void SqlServer_DatePart_ReturnsCorrectSyntax()
    {
        // Arrange & Act
        var result = SqlDefine.SqlServer.DatePart("YEAR", "col");

        // Assert
        Assert.IsTrue(result.Contains("DATEPART"));
        Assert.IsTrue(result.Contains("YEAR"));
    }

    [TestMethod]
    public void SqlServer_IfNull_ReturnsCorrectSyntax()
    {
        // Arrange & Act
        var result = SqlDefine.SqlServer.IfNull("col", "'default'");

        // Assert
        Assert.AreEqual("ISNULL(col, 'default')", result);
    }

    [TestMethod]
    public void SqlServer_Iif_ReturnsCorrectSyntax()
    {
        // Arrange & Act
        var result = SqlDefine.SqlServer.Iif("a > b", "1", "0");

        // Assert
        Assert.AreEqual("IIF(a > b, 1, 0)", result);
    }

    [TestMethod]
    public void SqlServer_Mod_ReturnsCorrectSyntax()
    {
        // Arrange & Act
        var result = SqlDefine.SqlServer.Mod("10", "3");

        // Assert
        Assert.AreEqual("MOD(10, 3)", result);
    }

    #endregion

    #region Cross-Dialect Comparison Tests

    [TestMethod]
    public void AllDialects_CurrentTimestamp_ReturnsDifferentSyntax()
    {
        // Arrange & Act
        var sqlServer = SqlDefine.SqlServer.CurrentTimestamp;
        var mySql = SqlDefine.MySql.CurrentTimestamp;
        var postgreSql = SqlDefine.PostgreSql.CurrentTimestamp;
        var sqlite = SqlDefine.SQLite.CurrentTimestamp;
        var oracle = SqlDefine.Oracle.CurrentTimestamp;
        var db2 = SqlDefine.DB2.CurrentTimestamp;

        // Assert
        Assert.AreEqual("GETDATE()", sqlServer);
        Assert.AreEqual("NOW()", mySql);
        Assert.AreEqual("CURRENT_TIMESTAMP", postgreSql);
        Assert.AreEqual("CURRENT_TIMESTAMP", sqlite);
        Assert.AreEqual("SYSTIMESTAMP", oracle);
        Assert.AreEqual("CURRENT TIMESTAMP", db2);
    }

    [TestMethod]
    public void AllDialects_ParameterPrefix_ReturnsDifferentSyntax()
    {
        // Arrange & Act
        var sqlServer = SqlDefine.SqlServer.ParameterPrefix;
        var mySql = SqlDefine.MySql.ParameterPrefix;
        var postgreSql = SqlDefine.PostgreSql.ParameterPrefix;
        var sqlite = SqlDefine.SQLite.ParameterPrefix;
        var oracle = SqlDefine.Oracle.ParameterPrefix;
        var db2 = SqlDefine.DB2.ParameterPrefix;

        // Assert
        Assert.AreEqual("@", sqlServer);
        Assert.AreEqual("@", mySql);
        Assert.AreEqual("@", postgreSql);
        Assert.AreEqual("@", sqlite);
        Assert.AreEqual(":", oracle);
        Assert.AreEqual("?", db2);
    }

    [TestMethod]
    public void AllDialects_ColumnQuoting_ReturnsDifferentSyntax()
    {
        // Arrange & Act
        var sqlServerLeft = SqlDefine.SqlServer.ColumnLeft;
        var sqlServerRight = SqlDefine.SqlServer.ColumnRight;
        var mySqlLeft = SqlDefine.MySql.ColumnLeft;
        var mySqlRight = SqlDefine.MySql.ColumnRight;
        var postgreSqlLeft = SqlDefine.PostgreSql.ColumnLeft;
        var postgreSqlRight = SqlDefine.PostgreSql.ColumnRight;

        // Assert
        Assert.AreEqual("[", sqlServerLeft);
        Assert.AreEqual("]", sqlServerRight);
        Assert.AreEqual("`", mySqlLeft);
        Assert.AreEqual("`", mySqlRight);
        Assert.AreEqual("\"", postgreSqlLeft);
        Assert.AreEqual("\"", postgreSqlRight);
    }

    #endregion
}
