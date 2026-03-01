// -----------------------------------------------------------------------
// <copyright file="DialectTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using Sqlx.Annotations;
using System;
using System.Linq.Expressions;

namespace Sqlx.Tests.Dialects;

/// <summary>
/// Consolidated tests for all SQL dialect implementations.
/// </summary>
[TestClass]
public class DialectTests
{
    /// <summary>
    /// Tests for basic dialect functionality across all dialects.
    /// </summary>
    [TestClass]
    public class AllDialectsTests
    {
        #region Identifier Quoting

        [TestMethod]
        [DataRow("SQLite", "[column_name]")]
        [DataRow("SqlServer", "[column_name]")]
        [DataRow("MySql", "`column_name`")]
        [DataRow("PostgreSql", "\"column_name\"")]
        [DataRow("Oracle", "\"column_name\"")]
        [DataRow("DB2", "\"column_name\"")]
        public void WrapColumn_UsesCorrectQuotes(string dialect, string expected) =>
            Assert.AreEqual(expected, GetDialect(dialect).WrapColumn("column_name"));

        #endregion

        #region Parameter Prefix

        [TestMethod]
        [DataRow("SQLite", "@param")]
        [DataRow("SqlServer", "@param")]
        [DataRow("MySql", "@param")]
        [DataRow("PostgreSql", "@param")]
        [DataRow("Oracle", ":param")]
        [DataRow("DB2", "?param")]
        public void CreateParameter_UsesCorrectPrefix(string dialect, string expected) =>
            Assert.AreEqual(expected, GetDialect(dialect).CreateParameter("param"));

        #endregion

        #region String Concatenation

        [TestMethod]
        [DataRow("SqlServer", "a + b + c")]
        [DataRow("PostgreSql", "a || b || c")]
        [DataRow("SQLite", "a || b || c")]
        [DataRow("Oracle", "a || b || c")]
        [DataRow("MySql", "CONCAT(a, b, c)")]
        [DataRow("DB2", "CONCAT(a, b, c)")]
        public void Concat_UsesCorrectSyntax(string dialect, string expected) =>
            Assert.AreEqual(expected, GetDialect(dialect).Concat("a", "b", "c"));

        #endregion

        #region String Functions

        [TestMethod]
        [DataRow("SqlServer", "LEN(col)")]
        [DataRow("MySql", "CHAR_LENGTH(col)")]
        [DataRow("PostgreSql", "LENGTH(col)")]
        public void Length_UsesCorrectFunction(string dialect, string expected) =>
            Assert.AreEqual(expected, GetDialect(dialect).Length("col"));

        [TestMethod]
        [DataRow("SqlServer")]
        [DataRow("MySql")]
        [DataRow("PostgreSql")]
        public void StringFunctions_ReturnCorrectFormat(string dialect)
        {
            var d = GetDialect(dialect);
            Assert.AreEqual("UPPER(col)", d.Upper("col"));
            Assert.AreEqual("LOWER(col)", d.Lower("col"));
            Assert.AreEqual("TRIM(col)", d.Trim("col"));
        }

        #endregion

        #region Date/Time Functions

        [TestMethod]
        [DataRow("SqlServer", "GETDATE()")]
        [DataRow("MySql", "NOW()")]
        [DataRow("PostgreSql", "CURRENT_TIMESTAMP")]
        [DataRow("SQLite", "CURRENT_TIMESTAMP")]
        [DataRow("Oracle", "SYSTIMESTAMP")]
        [DataRow("DB2", "CURRENT TIMESTAMP")]
        public void CurrentTimestamp_ReturnsCorrectSyntax(string dialect, string expected) =>
            Assert.AreEqual(expected, GetDialect(dialect).CurrentTimestamp);

        #endregion

        #region Pagination

        [TestMethod]
        [DataRow("SqlServer", "TOP 10")]
        [DataRow("MySql", "LIMIT 10")]
        [DataRow("PostgreSql", "LIMIT 10")]
        [DataRow("Oracle", "FETCH FIRST 10 ROWS ONLY")]
        public void Limit_ReturnsCorrectSyntax(string dialect, string expected) =>
            Assert.AreEqual(expected, GetDialect(dialect).Limit("10"));

        #endregion

        #region Null Handling

        [TestMethod]
        [DataRow("SqlServer", "ISNULL(col, 'default')")]
        [DataRow("MySql", "IFNULL(col, 'default')")]
        [DataRow("PostgreSql", "COALESCE(col, 'default')")]
        [DataRow("Oracle", "NVL(col, 'default')")]
        [DataRow("SQLite", "IFNULL(col, 'default')")]
        public void IfNull_ReturnsCorrectFunction(string dialect, string expected) =>
            Assert.AreEqual(expected, GetDialect(dialect).IfNull("col", "'default'"));

        #endregion

        #region Conditional

        [TestMethod]
        [DataRow("SqlServer", "IIF(a > b, 1, 0)")]
        [DataRow("MySql", "IF(a > b, 1, 0)")]
        [DataRow("PostgreSql", "CASE WHEN a > b THEN 1 ELSE 0 END")]
        public void Iif_ReturnsCorrectSyntax(string dialect, string expected) =>
            Assert.AreEqual(expected, GetDialect(dialect).Iif("a > b", "1", "0"));

        [TestMethod]
        [DataRow("SqlServer")]
        [DataRow("MySql")]
        [DataRow("PostgreSql")]
        public void CaseWhen_ReturnsCaseExpression(string dialect) =>
            Assert.AreEqual("CASE WHEN a > b THEN 1 ELSE 0 END", GetDialect(dialect).CaseWhen("a > b", "1", "0"));

        #endregion

        #region Last Inserted ID

        [TestMethod]
        [DataRow("SqlServer", "SELECT SCOPE_IDENTITY()")]
        [DataRow("MySql", "SELECT LAST_INSERT_ID()")]
        [DataRow("PostgreSql", "SELECT lastval()")]
        [DataRow("SQLite", "SELECT last_insert_rowid()")]
        public void LastInsertedId_ReturnsCorrectSyntax(string dialect, string expected) =>
            Assert.AreEqual(expected, GetDialect(dialect).LastInsertedId);

        #endregion

        #region Boolean Literals

        [TestMethod]
        public void SqlServer_BoolLiterals_ReturnsNumeric()
        {
            Assert.AreEqual("1", SqlDefine.SqlServer.BoolTrue);
            Assert.AreEqual("0", SqlDefine.SqlServer.BoolFalse);
        }

        [TestMethod]
        public void PostgreSql_BoolLiterals_ReturnsKeywords()
        {
            Assert.AreEqual("true", SqlDefine.PostgreSql.BoolTrue);
            Assert.AreEqual("false", SqlDefine.PostgreSql.BoolFalse);
        }

        #endregion

        #region Aggregate Functions

        [TestMethod]
        [DataRow("SqlServer")]
        [DataRow("MySql")]
        public void AggregateFunctions_ReturnCorrectFormat(string dialect)
        {
            var d = GetDialect(dialect);
            Assert.AreEqual("COUNT(*)", d.Count());
            Assert.AreEqual("COUNT(col)", d.Count("col"));
            Assert.AreEqual("SUM(col)", d.Sum("col"));
            Assert.AreEqual("AVG(col)", d.Avg("col"));
            Assert.AreEqual("MIN(col)", d.Min("col"));
            Assert.AreEqual("MAX(col)", d.Max("col"));
        }

        #endregion

        #region Numeric Functions

        [TestMethod]
        [DataRow("SqlServer")]
        [DataRow("MySql")]
        public void NumericFunctions_ReturnCorrectFormat(string dialect)
        {
            var d = GetDialect(dialect);
            Assert.AreEqual("ABS(col)", d.Abs("col"));
            Assert.AreEqual("ROUND(col, 2)", d.Round("col", "2"));
        }

        [TestMethod]
        public void SqlServer_Ceiling_ReturnsCeiling() =>
            Assert.AreEqual("CEILING(col)", SqlDefine.SqlServer.Ceiling("col"));

        [TestMethod]
        public void PostgreSql_Ceiling_ReturnsCeil() =>
            Assert.AreEqual("CEIL(col)", SqlDefine.PostgreSql.Ceiling("col"));

        #endregion

        #region GetDialect

        [TestMethod]
        public void GetDialect_ReturnsCorrectDialect()
        {
            Assert.AreEqual(SqlDefine.SQLite, SqlDefine.GetDialect(SqlDefineTypes.SQLite));
            Assert.AreEqual(SqlDefine.MySql, SqlDefine.GetDialect(SqlDefineTypes.MySql));
            Assert.AreEqual(SqlDefine.PostgreSql, SqlDefine.GetDialect(SqlDefineTypes.PostgreSql));
            Assert.AreEqual(SqlDefine.SqlServer, SqlDefine.GetDialect(SqlDefineTypes.SqlServer));
            Assert.AreEqual(SqlDefine.Oracle, SqlDefine.GetDialect(SqlDefineTypes.Oracle));
            Assert.AreEqual(SqlDefine.DB2, SqlDefine.GetDialect(SqlDefineTypes.DB2));
        }

        [TestMethod]
        public void GetDialect_ThrowsForUnsupportedType() =>
            Assert.ThrowsException<System.NotSupportedException>(() => SqlDefine.GetDialect((SqlDefineTypes)999));

        #endregion

        #region Aliases

        [TestMethod]
        public void PgSql_IsAliasForPostgreSql() => Assert.AreSame(SqlDefine.PostgreSql, SqlDefine.PgSql);

        [TestMethod]
        public void Sqlite_IsAliasForSQLite() => Assert.AreSame(SqlDefine.SQLite, SqlDefine.Sqlite);

        #endregion

        #region WrapString

        [TestMethod]
        [DataRow("SqlServer")]
        [DataRow("MySql")]
        [DataRow("PostgreSql")]
        public void WrapString_WrapsWithQuotes(string dialect) =>
            Assert.AreEqual("'test'", GetDialect(dialect).WrapString("test"));

        [TestMethod]
        [DataRow("SqlServer")]
        [DataRow("MySql")]
        public void WrapString_EscapesSingleQuotes(string dialect) =>
            Assert.AreEqual("'it''s'", GetDialect(dialect).WrapString("it's"));

        [TestMethod]
        [DataRow("SqlServer")]
        [DataRow("MySql")]
        public void WrapString_ReturnsNullForNull(string dialect) =>
            Assert.AreEqual("NULL", GetDialect(dialect).WrapString(null!));

        #endregion

        #region Cross-Dialect Comparison

        [TestMethod]
        public void AllDialects_CurrentTimestamp_ReturnsDifferentSyntax()
        {
            var sqlServer = SqlDefine.SqlServer.CurrentTimestamp;
            var mySql = SqlDefine.MySql.CurrentTimestamp;
            var postgreSql = SqlDefine.PostgreSql.CurrentTimestamp;
            var sqlite = SqlDefine.SQLite.CurrentTimestamp;
            var oracle = SqlDefine.Oracle.CurrentTimestamp;
            var db2 = SqlDefine.DB2.CurrentTimestamp;

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
            var sqlServer = SqlDefine.SqlServer.ParameterPrefix;
            var mySql = SqlDefine.MySql.ParameterPrefix;
            var postgreSql = SqlDefine.PostgreSql.ParameterPrefix;
            var sqlite = SqlDefine.SQLite.ParameterPrefix;
            var oracle = SqlDefine.Oracle.ParameterPrefix;
            var db2 = SqlDefine.DB2.ParameterPrefix;

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
            var sqlServerLeft = SqlDefine.SqlServer.ColumnLeft;
            var sqlServerRight = SqlDefine.SqlServer.ColumnRight;
            var mySqlLeft = SqlDefine.MySql.ColumnLeft;
            var mySqlRight = SqlDefine.MySql.ColumnRight;
            var postgreSqlLeft = SqlDefine.PostgreSql.ColumnLeft;
            var postgreSqlRight = SqlDefine.PostgreSql.ColumnRight;

            Assert.AreEqual("[", sqlServerLeft);
            Assert.AreEqual("]", sqlServerRight);
            Assert.AreEqual("`", mySqlLeft);
            Assert.AreEqual("`", mySqlRight);
            Assert.AreEqual("\"", postgreSqlLeft);
            Assert.AreEqual("\"", postgreSqlRight);
        }

        #endregion

        private static SqlDialect GetDialect(string name) => name switch
        {
            "SQLite" => SqlDefine.SQLite,
            "SqlServer" => SqlDefine.SqlServer,
            "MySql" => SqlDefine.MySql,
            "PostgreSql" => SqlDefine.PostgreSql,
            "Oracle" => SqlDefine.Oracle,
            "DB2" => SqlDefine.DB2,
            _ => throw new ArgumentException($"Unknown dialect: {name}")
        };
    }

    /// <summary>
    /// SQL Server specific tests.
    /// </summary>
    [TestClass]
    public class SqlServerSpecificTests
    {
        [TestMethod]
        public void SqlServer_CurrentDate_ReturnsCorrectSyntax()
        {
            var result = SqlDefine.SqlServer.CurrentDate;
            Assert.AreEqual("CAST(GETDATE() AS DATE)", result);
        }

        [TestMethod]
        public void SqlServer_CurrentTime_ReturnsCorrectSyntax()
        {
            var result = SqlDefine.SqlServer.CurrentTime;
            Assert.AreEqual("CAST(GETDATE() AS TIME)", result);
        }

        [TestMethod]
        public void SqlServer_DateAdd_ReturnsDateAddFunction() =>
            Assert.AreEqual("DATEADD(DAY, 1, col)", SqlDefine.SqlServer.DateAdd("DAY", "1", "col"));

        [TestMethod]
        public void SqlServer_DateDiff_ReturnsCorrectSyntax()
        {
            var result = SqlDefine.SqlServer.DateDiff("DAY", "start", "end");
            Assert.IsTrue(result.Contains("DATEDIFF"));
            Assert.IsTrue(result.Contains("DAY"));
        }

        [TestMethod]
        public void SqlServer_DatePart_ReturnsCorrectSyntax()
        {
            var result = SqlDefine.SqlServer.DatePart("YEAR", "col");
            Assert.IsTrue(result.Contains("DATEPART"));
            Assert.IsTrue(result.Contains("YEAR"));
        }

        [TestMethod]
        public void SqlServer_Paginate_ReturnsOffsetFetch() =>
            Assert.AreEqual("OFFSET 20 ROWS FETCH NEXT 10 ROWS ONLY", SqlDefine.SqlServer.Paginate("10", "20"));

        [TestMethod]
        public void SqlServer_Cast_ReturnsCastExpression() =>
            Assert.AreEqual("CAST(col AS VARCHAR(100))", SqlDefine.SqlServer.Cast("col", "VARCHAR(100)"));

        [TestMethod]
        public void SqlServer_Mod_ReturnsCorrectSyntax()
        {
            var result = SqlDefine.SqlServer.Mod("10", "3");
            Assert.AreEqual("MOD(10, 3)", result);
        }
    }

    /// <summary>
    /// PostgreSQL specific tests.
    /// </summary>
    [TestClass]
    public class PostgreSqlSpecificTests
    {
        [TestMethod]
        public void PostgreSql_CurrentDate_ReturnsCorrectSyntax()
        {
            var result = SqlDefine.PostgreSql.CurrentDate;
            Assert.AreEqual("CURRENT_DATE", result);
        }

        [TestMethod]
        public void PostgreSql_CurrentTime_ReturnsCorrectSyntax()
        {
            var result = SqlDefine.PostgreSql.CurrentTime;
            Assert.AreEqual("CURRENT_TIME", result);
        }

        [TestMethod]
        public void PostgreSql_DateAdd_ReturnsIntervalExpression() =>
            Assert.AreEqual("(col + INTERVAL '1 DAY')", SqlDefine.PostgreSql.DateAdd("DAY", "1", "col"));

        [TestMethod]
        public void PostgreSql_DateDiff_ReturnsCorrectSyntax()
        {
            var result = SqlDefine.PostgreSql.DateDiff("DAY", "start", "end");
            Assert.IsTrue(result.Contains("EXTRACT"));
            Assert.IsTrue(result.Contains("DAY"));
        }

        [TestMethod]
        public void PostgreSql_DatePart_ReturnsCorrectSyntax()
        {
            var result = SqlDefine.PostgreSql.DatePart("YEAR", "col");
            Assert.IsTrue(result.Contains("EXTRACT"));
            Assert.IsTrue(result.Contains("YEAR"));
        }

        [TestMethod]
        public void PostgreSql_InsertReturningIdSuffix_ReturnsCorrectSyntax()
        {
            var result = SqlDefine.PostgreSql.InsertReturningIdSuffix;
            Assert.AreEqual(" RETURNING id", result);
        }

        [TestMethod]
        public void PostgreSql_Cast_ReturnsDoubleColonSyntax() =>
            Assert.AreEqual("(col)::VARCHAR(100)", SqlDefine.PostgreSql.Cast("col", "VARCHAR(100)"));

        [TestMethod]
        public void PostgreSql_Mod_ReturnsCorrectSyntax()
        {
            var result = SqlDefine.PostgreSql.Mod("10", "3");
            Assert.AreEqual("MOD(10, 3)", result);
        }
    }

    /// <summary>
    /// MySQL specific tests.
    /// </summary>
    [TestClass]
    public class MySqlSpecificTests
    {
        [TestMethod]
        public void MySql_CurrentDate_ReturnsCorrectSyntax()
        {
            var result = SqlDefine.MySql.CurrentDate;
            Assert.AreEqual("CURDATE()", result);
        }

        [TestMethod]
        public void MySql_CurrentTime_ReturnsCorrectSyntax()
        {
            var result = SqlDefine.MySql.CurrentTime;
            Assert.AreEqual("CURTIME()", result);
        }

        [TestMethod]
        public void MySql_DateAdd_ReturnsDateAddFunction() =>
            Assert.AreEqual("DATE_ADD(col, INTERVAL 1 DAY)", SqlDefine.MySql.DateAdd("DAY", "1", "col"));

        [TestMethod]
        public void MySql_DateDiff_ReturnsCorrectSyntax()
        {
            var result = SqlDefine.MySql.DateDiff("DAY", "start", "end");
            Assert.IsTrue(result.Contains("TIMESTAMPDIFF"));
            Assert.IsTrue(result.Contains("DAY"));
        }

        [TestMethod]
        public void MySql_DatePart_ReturnsCorrectSyntax()
        {
            var result = SqlDefine.MySql.DatePart("YEAR", "col");
            Assert.IsTrue(result.Contains("EXTRACT"));
            Assert.IsTrue(result.Contains("YEAR"));
        }

        [TestMethod]
        public void MySql_Paginate_ReturnsLimitOffset() =>
            Assert.AreEqual("LIMIT 10 OFFSET 20", SqlDefine.MySql.Paginate("10", "20"));

        [TestMethod]
        public void MySql_Mod_ReturnsCorrectSyntax()
        {
            var result = SqlDefine.MySql.Mod("10", "3");
            Assert.AreEqual("MOD(10, 3)", result);
        }

        [TestMethod]
        public void MySql_IfNull_ReturnsCorrectSyntax()
        {
            var result = SqlDefine.MySql.IfNull("col", "'default'");
            Assert.AreEqual("IFNULL(col, 'default')", result);
        }
    }

    /// <summary>
    /// SQLite specific tests.
    /// </summary>
    [TestClass]
    public class SQLiteSpecificTests
    {
        [TestMethod]
        public void SQLite_CurrentDate_ReturnsCorrectSyntax()
        {
            var result = SqlDefine.SQLite.CurrentDate;
            Assert.AreEqual("DATE('now')", result);
        }

        [TestMethod]
        public void SQLite_CurrentTime_ReturnsCorrectSyntax()
        {
            var result = SqlDefine.SQLite.CurrentTime;
            Assert.AreEqual("TIME('now')", result);
        }

        [TestMethod]
        public void SQLite_DateAdd_ReturnsCorrectSyntax()
        {
            var result = SqlDefine.SQLite.DateAdd("DAY", "5", "col");
            Assert.AreEqual("DATETIME(col, '+5 DAY')", result);
        }

        [TestMethod]
        public void SQLite_DateDiff_ReturnsCorrectSyntax()
        {
            var result = SqlDefine.SQLite.DateDiff("DAY", "start", "end");
            Assert.IsTrue(result.Contains("JULIANDAY"));
        }

        [TestMethod]
        public void SQLite_DatePart_Year_ReturnsCorrectSyntax()
        {
            var result = SqlDefine.SQLite.DatePart("YEAR", "col");
            Assert.IsTrue(result.Contains("STRFTIME"));
            Assert.IsTrue(result.Contains("%Y"));
        }

        [TestMethod]
        public void SQLite_DatePart_Month_ReturnsCorrectSyntax()
        {
            var result = SqlDefine.SQLite.DatePart("MONTH", "col");
            Assert.IsTrue(result.Contains("STRFTIME"));
            Assert.IsTrue(result.Contains("%m"));
        }

        [TestMethod]
        public void SQLite_DatePart_Day_ReturnsCorrectSyntax()
        {
            var result = SqlDefine.SQLite.DatePart("DAY", "col");
            Assert.IsTrue(result.Contains("STRFTIME"));
            Assert.IsTrue(result.Contains("%d"));
        }

        [TestMethod]
        public void SQLite_DatePart_Hour_ReturnsCorrectSyntax()
        {
            var result = SqlDefine.SQLite.DatePart("HOUR", "col");
            Assert.IsTrue(result.Contains("STRFTIME"));
            Assert.IsTrue(result.Contains("%H"));
        }

        [TestMethod]
        public void SQLite_DatePart_Minute_ReturnsCorrectSyntax()
        {
            var result = SqlDefine.SQLite.DatePart("MINUTE", "col");
            Assert.IsTrue(result.Contains("STRFTIME"));
            Assert.IsTrue(result.Contains("%M"));
        }

        [TestMethod]
        public void SQLite_DatePart_Second_ReturnsCorrectSyntax()
        {
            var result = SqlDefine.SQLite.DatePart("SECOND", "col");
            Assert.IsTrue(result.Contains("STRFTIME"));
            Assert.IsTrue(result.Contains("%S"));
        }

        [TestMethod]
        public void SQLite_DatePart_UnknownPart_ReturnsDefaultSyntax()
        {
            var result = SqlDefine.SQLite.DatePart("UNKNOWN", "col");
            Assert.IsTrue(result.Contains("STRFTIME"));
            Assert.IsTrue(result.Contains("%UNKNOWN"));
        }

        [TestMethod]
        public void SQLite_IfNull_ReturnsCorrectSyntax()
        {
            var result = SqlDefine.SQLite.IfNull("col", "'default'");
            Assert.AreEqual("IFNULL(col, 'default')", result);
        }

        [TestMethod]
        public void SQLite_Mod_ReturnsCorrectSyntax()
        {
            var result = SqlDefine.SQLite.Mod("10", "3");
            Assert.AreEqual("MOD(10, 3)", result);
        }
    }

    /// <summary>
    /// Oracle specific tests.
    /// </summary>
    [TestClass]
    public class OracleSpecificTests
    {
        [TestMethod]
        public void Oracle_CurrentDate_ReturnsCorrectSyntax()
        {
            var result = SqlDefine.Oracle.CurrentDate;
            Assert.AreEqual("SYSDATE", result);
        }

        [TestMethod]
        public void Oracle_CurrentTime_ReturnsCorrectSyntax()
        {
            var result = SqlDefine.Oracle.CurrentTime;
            Assert.AreEqual("TO_CHAR(SYSDATE, 'HH24:MI:SS')", result);
        }

        [TestMethod]
        public void Oracle_DateAdd_Day_ReturnsCorrectSyntax()
        {
            var result = SqlDefine.Oracle.DateAdd("DAY", "5", "col");
            Assert.AreEqual("(col + 5)", result);
        }

        [TestMethod]
        public void Oracle_DateAdd_Month_ReturnsCorrectSyntax()
        {
            var result = SqlDefine.Oracle.DateAdd("MONTH", "3", "col");
            Assert.AreEqual("ADD_MONTHS(col, 3)", result);
        }

        [TestMethod]
        public void Oracle_DateAdd_Year_ReturnsCorrectSyntax()
        {
            var result = SqlDefine.Oracle.DateAdd("YEAR", "2", "col");
            Assert.AreEqual("ADD_MONTHS(col, 2 * 12)", result);
        }

        [TestMethod]
        public void Oracle_DateAdd_Hour_ReturnsCorrectSyntax()
        {
            var result = SqlDefine.Oracle.DateAdd("HOUR", "6", "col");
            Assert.AreEqual("(col + 6/24)", result);
        }

        [TestMethod]
        public void Oracle_DateAdd_Minute_ReturnsCorrectSyntax()
        {
            var result = SqlDefine.Oracle.DateAdd("MINUTE", "30", "col");
            Assert.AreEqual("(col + 30/1440)", result);
        }

        [TestMethod]
        public void Oracle_DateAdd_Second_ReturnsCorrectSyntax()
        {
            var result = SqlDefine.Oracle.DateAdd("SECOND", "45", "col");
            Assert.AreEqual("(col + 45/86400)", result);
        }

        [TestMethod]
        public void Oracle_DateAdd_UnknownInterval_ReturnsDefaultSyntax()
        {
            var result = SqlDefine.Oracle.DateAdd("WEEK", "2", "col");
            Assert.AreEqual("(col + INTERVAL '2' WEEK)", result);
        }

        [TestMethod]
        public void Oracle_DateDiff_Day_ReturnsCorrectSyntax()
        {
            var result = SqlDefine.Oracle.DateDiff("DAY", "start", "end");
            Assert.AreEqual("(end - start)", result);
        }

        [TestMethod]
        public void Oracle_DateDiff_Month_ReturnsCorrectSyntax()
        {
            var result = SqlDefine.Oracle.DateDiff("MONTH", "start", "end");
            Assert.AreEqual("MONTHS_BETWEEN(end, start)", result);
        }

        [TestMethod]
        public void Oracle_DateDiff_UnknownInterval_ReturnsDefaultSyntax()
        {
            var result = SqlDefine.Oracle.DateDiff("YEAR", "start", "end");
            Assert.AreEqual("(end - start)", result);
        }

        [TestMethod]
        public void Oracle_InsertReturningIdSuffix_ReturnsCorrectSyntax()
        {
            var result = SqlDefine.Oracle.InsertReturningIdSuffix;
            Assert.AreEqual(" RETURNING id INTO :id", result);
        }

        [TestMethod]
        public void Oracle_ParameterPrefix_UsesColon()
        {
            var dialect = SqlDefine.Oracle;
            var prefix = dialect.ParameterPrefix;
            Assert.AreEqual(":", prefix, "Oracle should use : as parameter prefix");
        }
    }

    /// <summary>
    /// DB2 specific tests.
    /// </summary>
    [TestClass]
    public class DB2SpecificTests
    {
        [TestMethod]
        public void DB2_CurrentDate_ReturnsCorrectSyntax()
        {
            var result = SqlDefine.DB2.CurrentDate;
            Assert.AreEqual("CURRENT DATE", result);
        }

        [TestMethod]
        public void DB2_CurrentTime_ReturnsCorrectSyntax()
        {
            var result = SqlDefine.DB2.CurrentTime;
            Assert.AreEqual("CURRENT TIME", result);
        }

        [TestMethod]
        public void DB2_DateAdd_ReturnsCorrectSyntax()
        {
            var result = SqlDefine.DB2.DateAdd("DAY", "5", "col");
            Assert.AreEqual("(col + 5 DAY)", result);
        }

        [TestMethod]
        public void DB2_LastInsertedId_ReturnsCorrectSyntax()
        {
            var result = SqlDefine.DB2.LastInsertedId;
            Assert.AreEqual("SELECT IDENTITY_VAL_LOCAL() FROM SYSIBM.SYSDUMMY1", result);
        }

        [TestMethod]
        public void DB2_DateDiff_Year_ReturnsCorrectCode()
        {
            var result = SqlDefine.DB2.DateDiff("YEAR", "start", "end");
            Assert.IsTrue(result.Contains("TIMESTAMPDIFF(256"));
        }

        [TestMethod]
        public void DB2_DateDiff_Month_ReturnsCorrectCode()
        {
            var result = SqlDefine.DB2.DateDiff("MONTH", "start", "end");
            Assert.IsTrue(result.Contains("TIMESTAMPDIFF(64"));
        }

        [TestMethod]
        public void DB2_DateDiff_Day_ReturnsCorrectCode()
        {
            var result = SqlDefine.DB2.DateDiff("DAY", "start", "end");
            Assert.IsTrue(result.Contains("TIMESTAMPDIFF(16"));
        }

        [TestMethod]
        public void DB2_DateDiff_Hour_ReturnsCorrectCode()
        {
            var result = SqlDefine.DB2.DateDiff("HOUR", "start", "end");
            Assert.IsTrue(result.Contains("TIMESTAMPDIFF(8"));
        }

        [TestMethod]
        public void DB2_DateDiff_Minute_ReturnsCorrectCode()
        {
            var result = SqlDefine.DB2.DateDiff("MINUTE", "start", "end");
            Assert.IsTrue(result.Contains("TIMESTAMPDIFF(4"));
        }

        [TestMethod]
        public void DB2_DateDiff_Second_ReturnsCorrectCode()
        {
            var result = SqlDefine.DB2.DateDiff("SECOND", "start", "end");
            Assert.IsTrue(result.Contains("TIMESTAMPDIFF(2"));
        }

        [TestMethod]
        public void DB2_DateDiff_UnknownInterval_ReturnsDefaultCode()
        {
            var result = SqlDefine.DB2.DateDiff("UNKNOWN", "start", "end");
            Assert.IsTrue(result.Contains("TIMESTAMPDIFF(16")); // Default to days (16)
        }

        [TestMethod]
        public void DB2_DateDiff_LowercaseInterval_ReturnsCorrectCode()
        {
            var result = SqlDefine.DB2.DateDiff("year", "start", "end");
            Assert.IsTrue(result.Contains("TIMESTAMPDIFF(256"));
        }
    }

    /// <summary>
    /// Boundary and edge case tests for SQL dialects.
    /// </summary>
    [TestClass]
    public class BoundaryTests
    {
        #region Empty and Null Input Tests

        [TestMethod]
        public void WrapColumn_EmptyString_ReturnsEmptyQuoted()
        {
            var result = SqlDefine.SQLite.WrapColumn("");
            Assert.AreEqual("", result);
        }

        [TestMethod]
        public void WrapString_EmptyString_ReturnsEmptyQuoted()
        {
            var result = SqlDefine.SQLite.WrapString("");
            Assert.AreEqual("''", result);
        }

        [TestMethod]
        public void CreateParameter_EmptyString_ReturnsPrefix()
        {
            var result = SqlDefine.SQLite.CreateParameter("");
            Assert.AreEqual("@", result);
        }

        #endregion

        #region Special Characters in Identifiers

        [TestMethod]
        public void WrapColumn_WithSpaces_QuotedCorrectly()
        {
            var result = SqlDefine.SQLite.WrapColumn("column name");
            Assert.AreEqual("[column name]", result);
        }

        [TestMethod]
        public void WrapColumn_WithSpecialChars_QuotedCorrectly()
        {
            var result = SqlDefine.SQLite.WrapColumn("col-name");
            Assert.AreEqual("[col-name]", result);
        }

        [TestMethod]
        public void WrapColumn_WithQuotes_QuotedCorrectly()
        {
            var result = SqlDefine.MySql.WrapColumn("col`name");
            Assert.AreEqual("`col`name`", result);
        }

        #endregion

        #region String Escaping Tests

        [TestMethod]
        public void WrapString_WithSingleQuote_EscapedCorrectly()
        {
            var result = SqlDefine.SQLite.WrapString("it's");
            Assert.AreEqual("'it''s'", result);
        }

        [TestMethod]
        public void WrapString_WithMultipleSingleQuotes_AllEscaped()
        {
            var result = SqlDefine.SQLite.WrapString("it's John's");
            Assert.AreEqual("'it''s John''s'", result);
        }

        [TestMethod]
        public void WrapString_WithBackslash_NotEscaped()
        {
            var result = SqlDefine.SQLite.WrapString("path\\to\\file");
            Assert.AreEqual("'path\\to\\file'", result);
        }

        [TestMethod]
        public void WrapString_WithNewline_PreservedInString()
        {
            var result = SqlDefine.SQLite.WrapString("line1\nline2");
            Assert.AreEqual("'line1\nline2'", result);
        }

        #endregion

        #region Concat Edge Cases

        [TestMethod]
        public void Concat_SinglePart_ReturnsSinglePart()
        {
            Assert.AreEqual("a", SqlDefine.SqlServer.Concat("a"));
            Assert.AreEqual("a", SqlDefine.PostgreSql.Concat("a"));
            Assert.AreEqual("CONCAT(a)", SqlDefine.MySql.Concat("a"));
        }

        [TestMethod]
        public void Concat_EmptyArray_ReturnsEmpty()
        {
            Assert.AreEqual("", SqlDefine.SqlServer.Concat());
            Assert.AreEqual("", SqlDefine.PostgreSql.Concat());
            Assert.AreEqual("CONCAT()", SqlDefine.MySql.Concat());
        }

        [TestMethod]
        public void Concat_ManyParts_AllJoined()
        {
            var parts = new[] { "a", "b", "c", "d", "e" };

            Assert.AreEqual("a + b + c + d + e", SqlDefine.SqlServer.Concat(parts));
            Assert.AreEqual("a || b || c || d || e", SqlDefine.PostgreSql.Concat(parts));
            Assert.AreEqual("CONCAT(a, b, c, d, e)", SqlDefine.MySql.Concat(parts));
        }

        #endregion

        #region Date Functions Edge Cases

        [TestMethod]
        public void DateAdd_NegativeNumber_HandledCorrectly()
        {
            var result = SqlDefine.SqlServer.DateAdd("DAY", "-7", "col");
            Assert.AreEqual("DATEADD(DAY, -7, col)", result);
        }

        [TestMethod]
        public void DateAdd_ZeroNumber_HandledCorrectly()
        {
            var result = SqlDefine.SqlServer.DateAdd("DAY", "0", "col");
            Assert.AreEqual("DATEADD(DAY, 0, col)", result);
        }

        [TestMethod]
        public void DatePart_AllParts_SupportedByAllDialects()
        {
            var parts = new[] { "YEAR", "MONTH", "DAY", "HOUR", "MINUTE", "SECOND" };
            var dialects = new[] { SqlDefine.SqlServer, SqlDefine.MySql, SqlDefine.PostgreSql, SqlDefine.SQLite, SqlDefine.Oracle, SqlDefine.DB2 };

            foreach (var dialect in dialects)
            {
                foreach (var part in parts)
                {
                    var result = dialect.DatePart(part, "col");
                    Assert.IsFalse(string.IsNullOrEmpty(result), $"{dialect.DatabaseType} should support {part}");
                }
            }
        }

        #endregion

        #region Pagination Edge Cases

        [TestMethod]
        public void Limit_Zero_GeneratesZeroLimit()
        {
            Assert.AreEqual("LIMIT 0", SqlDefine.SQLite.Limit("0"));
            Assert.AreEqual("TOP 0", SqlDefine.SqlServer.Limit("0"));
            Assert.AreEqual("FETCH FIRST 0 ROWS ONLY", SqlDefine.Oracle.Limit("0"));
        }

        [TestMethod]
        public void Offset_Zero_GeneratesZeroOffset()
        {
            Assert.AreEqual("OFFSET 0", SqlDefine.SQLite.Offset("0"));
            Assert.AreEqual("OFFSET 0", SqlDefine.MySql.Offset("0"));
        }

        [TestMethod]
        public void Paginate_BothZero_GeneratesCorrectSyntax()
        {
            Assert.AreEqual("LIMIT 0 OFFSET 0", SqlDefine.SQLite.Paginate("0", "0"));
            Assert.AreEqual("OFFSET 0 ROWS FETCH NEXT 0 ROWS ONLY", SqlDefine.SqlServer.Paginate("0", "0"));
        }

        [TestMethod]
        public void Paginate_LargeNumbers_HandledCorrectly()
        {
            var largeLimit = "1000000";
            var largeOffset = "9999999";

            Assert.AreEqual($"LIMIT {largeLimit} OFFSET {largeOffset}", SqlDefine.SQLite.Paginate(largeLimit, largeOffset));
        }

        #endregion

        #region Null Handling Edge Cases

        [TestMethod]
        public void IfNull_NestedCalls_WorkCorrectly()
        {
            var inner = SqlDefine.SqlServer.IfNull("col1", "'default1'");
            var outer = SqlDefine.SqlServer.IfNull(inner, "'default2'");

            Assert.AreEqual("ISNULL(ISNULL(col1, 'default1'), 'default2')", outer);
        }

        [TestMethod]
        public void Coalesce_SingleExpression_ReturnsCoalesce()
        {
            var result = SqlDefine.SqlServer.Coalesce("col");
            Assert.AreEqual("COALESCE(col)", result);
        }

        [TestMethod]
        public void Coalesce_ManyExpressions_AllIncluded()
        {
            var result = SqlDefine.SqlServer.Coalesce("col1", "col2", "col3", "'default'");
            Assert.AreEqual("COALESCE(col1, col2, col3, 'default')", result);
        }

        [TestMethod]
        public void NullIf_AllDialects_ReturnNullIf()
        {
            var dialects = new[] { SqlDefine.SqlServer, SqlDefine.MySql, SqlDefine.PostgreSql, SqlDefine.SQLite, SqlDefine.Oracle, SqlDefine.DB2 };

            foreach (var dialect in dialects)
            {
                var result = dialect.NullIf("col", "0");
                Assert.AreEqual("NULLIF(col, 0)", result, $"Failed for {dialect.DatabaseType}");
            }
        }

        #endregion

        #region Type Casting Edge Cases

        [TestMethod]
        public void Cast_ComplexType_HandledCorrectly()
        {
            var result = SqlDefine.SqlServer.Cast("col", "DECIMAL(18,2)");
            Assert.AreEqual("CAST(col AS DECIMAL(18,2))", result);
        }

        [TestMethod]
        public void Cast_PostgreSql_UsesDoubleColonSyntax()
        {
            var result = SqlDefine.PostgreSql.Cast("col", "INTEGER");
            Assert.AreEqual("(col)::INTEGER", result);
        }

        #endregion

        #region Conditional Edge Cases

        [TestMethod]
        public void CaseWhen_NestedConditions_WorkCorrectly()
        {
            var inner = SqlDefine.SqlServer.CaseWhen("a > b", "1", "0");
            var outer = SqlDefine.SqlServer.CaseWhen($"{inner} = 1", "'yes'", "'no'");

            Assert.IsTrue(outer.Contains("CASE WHEN"));
            Assert.IsTrue(outer.Contains("THEN 'yes'"));
        }

        [TestMethod]
        public void Iif_ComplexCondition_HandledCorrectly()
        {
            var result = SqlDefine.SqlServer.Iif("a > b AND c < d", "1", "0");
            Assert.AreEqual("IIF(a > b AND c < d, 1, 0)", result);
        }

        #endregion

        #region Numeric Functions Edge Cases

        [TestMethod]
        public void Round_NegativeDecimals_HandledCorrectly()
        {
            var result = SqlDefine.SqlServer.Round("col", "-2");
            Assert.AreEqual("ROUND(col, -2)", result);
        }

        [TestMethod]
        public void Mod_AllDialects_ReturnMod()
        {
            var dialects = new[] { SqlDefine.SqlServer, SqlDefine.MySql, SqlDefine.PostgreSql, SqlDefine.SQLite, SqlDefine.Oracle, SqlDefine.DB2 };

            foreach (var dialect in dialects)
            {
                var result = dialect.Mod("10", "3");
                Assert.AreEqual("MOD(10, 3)", result, $"Failed for {dialect.DatabaseType}");
            }
        }

        #endregion

        #region Database Type Properties

        [TestMethod]
        public void AllDialects_HaveCorrectDatabaseType()
        {
            Assert.AreEqual("SQLite", SqlDefine.SQLite.DatabaseType);
            Assert.AreEqual("MySql", SqlDefine.MySql.DatabaseType);
            Assert.AreEqual("PostgreSql", SqlDefine.PostgreSql.DatabaseType);
            Assert.AreEqual("SqlServer", SqlDefine.SqlServer.DatabaseType);
            Assert.AreEqual("Oracle", SqlDefine.Oracle.DatabaseType);
            Assert.AreEqual("DB2", SqlDefine.DB2.DatabaseType);
        }

        [TestMethod]
        public void AllDialects_HaveCorrectDbType()
        {
            Assert.AreEqual(SqlDefineTypes.SQLite, SqlDefine.SQLite.DbType);
            Assert.AreEqual(SqlDefineTypes.MySql, SqlDefine.MySql.DbType);
            Assert.AreEqual(SqlDefineTypes.PostgreSql, SqlDefine.PostgreSql.DbType);
            Assert.AreEqual(SqlDefineTypes.SqlServer, SqlDefine.SqlServer.DbType);
            Assert.AreEqual(SqlDefineTypes.Oracle, SqlDefine.Oracle.DbType);
            Assert.AreEqual(SqlDefineTypes.DB2, SqlDefine.DB2.DbType);
        }

        #endregion
    }
}
