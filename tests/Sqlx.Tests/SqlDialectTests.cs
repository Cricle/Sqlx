using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;

namespace Sqlx.Tests;

[TestClass]
public class SqlDialectTests
{
    #region Identifier Quoting

    [TestMethod]
    public void SQLite_WrapColumn_UsesBrackets()
    {
        var result = SqlDefine.SQLite.WrapColumn("column_name");
        Assert.AreEqual("[column_name]", result);
    }

    [TestMethod]
    public void MySql_WrapColumn_UsesBackticks()
    {
        var result = SqlDefine.MySql.WrapColumn("column_name");
        Assert.AreEqual("`column_name`", result);
    }

    [TestMethod]
    public void PostgreSql_WrapColumn_UsesDoubleQuotes()
    {
        var result = SqlDefine.PostgreSql.WrapColumn("column_name");
        Assert.AreEqual("\"column_name\"", result);
    }

    [TestMethod]
    public void SqlServer_WrapColumn_UsesBrackets()
    {
        var result = SqlDefine.SqlServer.WrapColumn("column_name");
        Assert.AreEqual("[column_name]", result);
    }

    [TestMethod]
    public void Oracle_WrapColumn_UsesDoubleQuotes()
    {
        var result = SqlDefine.Oracle.WrapColumn("column_name");
        Assert.AreEqual("\"column_name\"", result);
    }

    [TestMethod]
    public void DB2_WrapColumn_UsesDoubleQuotes()
    {
        var result = SqlDefine.DB2.WrapColumn("column_name");
        Assert.AreEqual("\"column_name\"", result);
    }

    #endregion

    #region Parameter Prefix

    [TestMethod]
    public void SQLite_CreateParameter_UsesAtSign()
    {
        var result = SqlDefine.SQLite.CreateParameter("param");
        Assert.AreEqual("@param", result);
    }

    [TestMethod]
    public void PostgreSql_CreateParameter_UsesDollarSign()
    {
        var result = SqlDefine.PostgreSql.CreateParameter("param");
        Assert.AreEqual("$param", result);
    }

    [TestMethod]
    public void Oracle_CreateParameter_UsesColon()
    {
        var result = SqlDefine.Oracle.CreateParameter("param");
        Assert.AreEqual(":param", result);
    }

    [TestMethod]
    public void SqlServer_CreateParameter_UsesAtSign()
    {
        var result = SqlDefine.SqlServer.CreateParameter("param");
        Assert.AreEqual("@param", result);
    }

    [TestMethod]
    public void MySql_CreateParameter_UsesAtSign()
    {
        var result = SqlDefine.MySql.CreateParameter("param");
        Assert.AreEqual("@param", result);
    }

    [TestMethod]
    public void DB2_CreateParameter_UsesQuestionMark()
    {
        var result = SqlDefine.DB2.CreateParameter("param");
        Assert.AreEqual("?param", result);
    }

    #endregion

    #region String Concatenation

    [TestMethod]
    public void SqlServer_Concat_UsesPlusOperator()
    {
        var result = SqlDefine.SqlServer.Concat("a", "b", "c");
        Assert.AreEqual("a + b + c", result);
    }

    [TestMethod]
    public void PostgreSql_Concat_UsesPipeOperator()
    {
        var result = SqlDefine.PostgreSql.Concat("a", "b", "c");
        Assert.AreEqual("a || b || c", result);
    }

    [TestMethod]
    public void MySql_Concat_UsesConcatFunction()
    {
        var result = SqlDefine.MySql.Concat("a", "b", "c");
        Assert.AreEqual("CONCAT(a, b, c)", result);
    }

    [TestMethod]
    public void SQLite_Concat_UsesPipeOperator()
    {
        var result = SqlDefine.SQLite.Concat("a", "b", "c");
        Assert.AreEqual("a || b || c", result);
    }

    [TestMethod]
    public void Oracle_Concat_UsesPipeOperator()
    {
        var result = SqlDefine.Oracle.Concat("a", "b", "c");
        Assert.AreEqual("a || b || c", result);
    }

    [TestMethod]
    public void DB2_Concat_UsesConcatFunction()
    {
        var result = SqlDefine.DB2.Concat("a", "b", "c");
        Assert.AreEqual("CONCAT(a, b, c)", result);
    }

    #endregion

    #region String Functions

    [TestMethod]
    public void SqlServer_Length_UsesLen()
    {
        var result = SqlDefine.SqlServer.Length("col");
        Assert.AreEqual("LEN(col)", result);
    }

    [TestMethod]
    public void MySql_Length_UsesCharLength()
    {
        var result = SqlDefine.MySql.Length("col");
        Assert.AreEqual("CHAR_LENGTH(col)", result);
    }

    [TestMethod]
    public void PostgreSql_Length_UsesLength()
    {
        var result = SqlDefine.PostgreSql.Length("col");
        Assert.AreEqual("LENGTH(col)", result);
    }

    [TestMethod]
    public void AllDialects_Upper_ReturnsUpperFunction()
    {
        Assert.AreEqual("UPPER(col)", SqlDefine.SqlServer.Upper("col"));
        Assert.AreEqual("UPPER(col)", SqlDefine.MySql.Upper("col"));
        Assert.AreEqual("UPPER(col)", SqlDefine.PostgreSql.Upper("col"));
    }

    [TestMethod]
    public void AllDialects_Lower_ReturnsLowerFunction()
    {
        Assert.AreEqual("LOWER(col)", SqlDefine.SqlServer.Lower("col"));
        Assert.AreEqual("LOWER(col)", SqlDefine.MySql.Lower("col"));
        Assert.AreEqual("LOWER(col)", SqlDefine.PostgreSql.Lower("col"));
    }

    [TestMethod]
    public void AllDialects_Trim_ReturnsTrimFunction()
    {
        Assert.AreEqual("TRIM(col)", SqlDefine.SqlServer.Trim("col"));
        Assert.AreEqual("TRIM(col)", SqlDefine.MySql.Trim("col"));
        Assert.AreEqual("TRIM(col)", SqlDefine.PostgreSql.Trim("col"));
    }

    #endregion

    #region Date/Time Functions

    [TestMethod]
    public void SqlServer_CurrentTimestamp_ReturnsGetDate()
    {
        Assert.AreEqual("GETDATE()", SqlDefine.SqlServer.CurrentTimestamp);
    }

    [TestMethod]
    public void MySql_CurrentTimestamp_ReturnsNow()
    {
        Assert.AreEqual("NOW()", SqlDefine.MySql.CurrentTimestamp);
    }

    [TestMethod]
    public void PostgreSql_CurrentTimestamp_ReturnsCurrentTimestamp()
    {
        Assert.AreEqual("CURRENT_TIMESTAMP", SqlDefine.PostgreSql.CurrentTimestamp);
    }

    [TestMethod]
    public void SQLite_CurrentTimestamp_ReturnsCurrentTimestamp()
    {
        Assert.AreEqual("CURRENT_TIMESTAMP", SqlDefine.SQLite.CurrentTimestamp);
    }

    [TestMethod]
    public void Oracle_CurrentTimestamp_ReturnsSysTimestamp()
    {
        Assert.AreEqual("SYSTIMESTAMP", SqlDefine.Oracle.CurrentTimestamp);
    }

    [TestMethod]
    public void DB2_CurrentTimestamp_ReturnsCurrentTimestamp()
    {
        Assert.AreEqual("CURRENT TIMESTAMP", SqlDefine.DB2.CurrentTimestamp);
    }

    [TestMethod]
    public void SqlServer_DateAdd_ReturnsDateAddFunction()
    {
        var result = SqlDefine.SqlServer.DateAdd("DAY", "1", "col");
        Assert.AreEqual("DATEADD(DAY, 1, col)", result);
    }

    [TestMethod]
    public void MySql_DateAdd_ReturnsDateAddFunction()
    {
        var result = SqlDefine.MySql.DateAdd("DAY", "1", "col");
        Assert.AreEqual("DATE_ADD(col, INTERVAL 1 DAY)", result);
    }

    [TestMethod]
    public void PostgreSql_DateAdd_ReturnsIntervalExpression()
    {
        var result = SqlDefine.PostgreSql.DateAdd("DAY", "1", "col");
        Assert.AreEqual("(col + INTERVAL '1 DAY')", result);
    }

    #endregion

    #region Pagination

    [TestMethod]
    public void SqlServer_Limit_ReturnsTop()
    {
        var result = SqlDefine.SqlServer.Limit("10");
        Assert.AreEqual("TOP 10", result);
    }

    [TestMethod]
    public void MySql_Limit_ReturnsLimit()
    {
        var result = SqlDefine.MySql.Limit("10");
        Assert.AreEqual("LIMIT 10", result);
    }

    [TestMethod]
    public void PostgreSql_Limit_ReturnsLimit()
    {
        var result = SqlDefine.PostgreSql.Limit("10");
        Assert.AreEqual("LIMIT 10", result);
    }

    [TestMethod]
    public void Oracle_Limit_ReturnsFetchFirst()
    {
        var result = SqlDefine.Oracle.Limit("10");
        Assert.AreEqual("FETCH FIRST 10 ROWS ONLY", result);
    }

    [TestMethod]
    public void SqlServer_Paginate_ReturnsOffsetFetch()
    {
        var result = SqlDefine.SqlServer.Paginate("10", "20");
        Assert.AreEqual("OFFSET 20 ROWS FETCH NEXT 10 ROWS ONLY", result);
    }

    [TestMethod]
    public void MySql_Paginate_ReturnsLimitOffset()
    {
        var result = SqlDefine.MySql.Paginate("10", "20");
        Assert.AreEqual("LIMIT 10 OFFSET 20", result);
    }

    #endregion

    #region Null Handling

    [TestMethod]
    public void SqlServer_IfNull_ReturnsIsNull()
    {
        var result = SqlDefine.SqlServer.IfNull("col", "'default'");
        Assert.AreEqual("ISNULL(col, 'default')", result);
    }

    [TestMethod]
    public void MySql_IfNull_ReturnsIfNull()
    {
        var result = SqlDefine.MySql.IfNull("col", "'default'");
        Assert.AreEqual("IFNULL(col, 'default')", result);
    }

    [TestMethod]
    public void PostgreSql_IfNull_ReturnsCoalesce()
    {
        var result = SqlDefine.PostgreSql.IfNull("col", "'default'");
        Assert.AreEqual("COALESCE(col, 'default')", result);
    }

    [TestMethod]
    public void Oracle_IfNull_ReturnsNvl()
    {
        var result = SqlDefine.Oracle.IfNull("col", "'default'");
        Assert.AreEqual("NVL(col, 'default')", result);
    }

    [TestMethod]
    public void SQLite_IfNull_ReturnsIfNull()
    {
        var result = SqlDefine.SQLite.IfNull("col", "'default'");
        Assert.AreEqual("IFNULL(col, 'default')", result);
    }

    #endregion

    #region Conditional

    [TestMethod]
    public void SqlServer_Iif_ReturnsIif()
    {
        var result = SqlDefine.SqlServer.Iif("a > b", "1", "0");
        Assert.AreEqual("IIF(a > b, 1, 0)", result);
    }

    [TestMethod]
    public void MySql_Iif_ReturnsIf()
    {
        var result = SqlDefine.MySql.Iif("a > b", "1", "0");
        Assert.AreEqual("IF(a > b, 1, 0)", result);
    }

    [TestMethod]
    public void PostgreSql_Iif_ReturnsCaseWhen()
    {
        var result = SqlDefine.PostgreSql.Iif("a > b", "1", "0");
        Assert.AreEqual("CASE WHEN a > b THEN 1 ELSE 0 END", result);
    }

    [TestMethod]
    public void AllDialects_CaseWhen_ReturnsCaseExpression()
    {
        var expected = "CASE WHEN a > b THEN 1 ELSE 0 END";
        Assert.AreEqual(expected, SqlDefine.SqlServer.CaseWhen("a > b", "1", "0"));
        Assert.AreEqual(expected, SqlDefine.MySql.CaseWhen("a > b", "1", "0"));
        Assert.AreEqual(expected, SqlDefine.PostgreSql.CaseWhen("a > b", "1", "0"));
    }

    #endregion

    #region Type Casting

    [TestMethod]
    public void SqlServer_Cast_ReturnsCastExpression()
    {
        var result = SqlDefine.SqlServer.Cast("col", "VARCHAR(100)");
        Assert.AreEqual("CAST(col AS VARCHAR(100))", result);
    }

    [TestMethod]
    public void PostgreSql_Cast_ReturnsDoubleColonSyntax()
    {
        var result = SqlDefine.PostgreSql.Cast("col", "VARCHAR(100)");
        Assert.AreEqual("(col)::VARCHAR(100)", result);
    }

    #endregion

    #region Last Inserted ID

    [TestMethod]
    public void SqlServer_LastInsertedId_ReturnsScopeIdentity()
    {
        Assert.AreEqual("SELECT SCOPE_IDENTITY()", SqlDefine.SqlServer.LastInsertedId);
    }

    [TestMethod]
    public void MySql_LastInsertedId_ReturnsLastInsertId()
    {
        Assert.AreEqual("SELECT LAST_INSERT_ID()", SqlDefine.MySql.LastInsertedId);
    }

    [TestMethod]
    public void PostgreSql_LastInsertedId_ReturnsLastVal()
    {
        Assert.AreEqual("SELECT lastval()", SqlDefine.PostgreSql.LastInsertedId);
    }

    [TestMethod]
    public void SQLite_LastInsertedId_ReturnsLastInsertRowId()
    {
        Assert.AreEqual("SELECT last_insert_rowid()", SqlDefine.SQLite.LastInsertedId);
    }

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
    public void AllDialects_Count_ReturnsCountFunction()
    {
        Assert.AreEqual("COUNT(*)", SqlDefine.SqlServer.Count());
        Assert.AreEqual("COUNT(col)", SqlDefine.MySql.Count("col"));
    }

    [TestMethod]
    public void AllDialects_Sum_ReturnsSumFunction()
    {
        Assert.AreEqual("SUM(col)", SqlDefine.SqlServer.Sum("col"));
        Assert.AreEqual("SUM(col)", SqlDefine.MySql.Sum("col"));
    }

    [TestMethod]
    public void AllDialects_Avg_ReturnsAvgFunction()
    {
        Assert.AreEqual("AVG(col)", SqlDefine.SqlServer.Avg("col"));
        Assert.AreEqual("AVG(col)", SqlDefine.MySql.Avg("col"));
    }

    [TestMethod]
    public void AllDialects_MinMax_ReturnsMinMaxFunctions()
    {
        Assert.AreEqual("MIN(col)", SqlDefine.SqlServer.Min("col"));
        Assert.AreEqual("MAX(col)", SqlDefine.SqlServer.Max("col"));
    }

    #endregion

    #region Numeric Functions

    [TestMethod]
    public void AllDialects_Abs_ReturnsAbsFunction()
    {
        Assert.AreEqual("ABS(col)", SqlDefine.SqlServer.Abs("col"));
        Assert.AreEqual("ABS(col)", SqlDefine.MySql.Abs("col"));
    }

    [TestMethod]
    public void AllDialects_Round_ReturnsRoundFunction()
    {
        Assert.AreEqual("ROUND(col, 2)", SqlDefine.SqlServer.Round("col", "2"));
        Assert.AreEqual("ROUND(col, 2)", SqlDefine.MySql.Round("col", "2"));
    }

    [TestMethod]
    public void SqlServer_Ceiling_ReturnsCeiling()
    {
        Assert.AreEqual("CEILING(col)", SqlDefine.SqlServer.Ceiling("col"));
    }

    [TestMethod]
    public void PostgreSql_Ceiling_ReturnsCeil()
    {
        Assert.AreEqual("CEIL(col)", SqlDefine.PostgreSql.Ceiling("col"));
    }

    #endregion

    #region GetDialect

    [TestMethod]
    public void GetDialect_ReturnsCorrectDialect()
    {
        Assert.AreEqual(SqlDefine.SQLite, SqlDefine.GetDialect(Annotations.SqlDefineTypes.SQLite));
        Assert.AreEqual(SqlDefine.MySql, SqlDefine.GetDialect(Annotations.SqlDefineTypes.MySql));
        Assert.AreEqual(SqlDefine.PostgreSql, SqlDefine.GetDialect(Annotations.SqlDefineTypes.PostgreSql));
        Assert.AreEqual(SqlDefine.SqlServer, SqlDefine.GetDialect(Annotations.SqlDefineTypes.SqlServer));
        Assert.AreEqual(SqlDefine.Oracle, SqlDefine.GetDialect(Annotations.SqlDefineTypes.Oracle));
        Assert.AreEqual(SqlDefine.DB2, SqlDefine.GetDialect(Annotations.SqlDefineTypes.DB2));
    }

    [TestMethod]
    public void GetDialect_ThrowsForUnsupportedType()
    {
        Assert.ThrowsException<System.NotSupportedException>(() =>
            SqlDefine.GetDialect((Annotations.SqlDefineTypes)999));
    }

    #endregion

    #region Aliases

    [TestMethod]
    public void PgSql_IsAliasForPostgreSql()
    {
        Assert.AreSame(SqlDefine.PostgreSql, SqlDefine.PgSql);
    }

    [TestMethod]
    public void Sqlite_IsAliasForSQLite()
    {
        Assert.AreSame(SqlDefine.SQLite, SqlDefine.Sqlite);
    }

    #endregion

    #region WrapString

    [TestMethod]
    public void AllDialects_WrapString_WrapsWithQuotes()
    {
        Assert.AreEqual("'test'", SqlDefine.SqlServer.WrapString("test"));
        Assert.AreEqual("'test'", SqlDefine.MySql.WrapString("test"));
        Assert.AreEqual("'test'", SqlDefine.PostgreSql.WrapString("test"));
    }

    [TestMethod]
    public void AllDialects_WrapString_EscapesSingleQuotes()
    {
        Assert.AreEqual("'it''s'", SqlDefine.SqlServer.WrapString("it's"));
        Assert.AreEqual("'it''s'", SqlDefine.MySql.WrapString("it's"));
    }

    [TestMethod]
    public void AllDialects_WrapString_ReturnsNullForNull()
    {
        Assert.AreEqual("NULL", SqlDefine.SqlServer.WrapString(null!));
        Assert.AreEqual("NULL", SqlDefine.MySql.WrapString(null!));
    }

    #endregion
}
