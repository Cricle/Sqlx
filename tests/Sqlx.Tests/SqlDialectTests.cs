using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;

namespace Sqlx.Tests;

[TestClass]
public class SqlDialectTests
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
    [DataRow("PostgreSql", "$param")]
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

    [TestMethod]
    public void SqlServer_DateAdd_ReturnsDateAddFunction() =>
        Assert.AreEqual("DATEADD(DAY, 1, col)", SqlDefine.SqlServer.DateAdd("DAY", "1", "col"));

    [TestMethod]
    public void MySql_DateAdd_ReturnsDateAddFunction() =>
        Assert.AreEqual("DATE_ADD(col, INTERVAL 1 DAY)", SqlDefine.MySql.DateAdd("DAY", "1", "col"));

    [TestMethod]
    public void PostgreSql_DateAdd_ReturnsIntervalExpression() =>
        Assert.AreEqual("(col + INTERVAL '1 DAY')", SqlDefine.PostgreSql.DateAdd("DAY", "1", "col"));

    #endregion

    #region Pagination

    [TestMethod]
    [DataRow("SqlServer", "TOP 10")]
    [DataRow("MySql", "LIMIT 10")]
    [DataRow("PostgreSql", "LIMIT 10")]
    [DataRow("Oracle", "FETCH FIRST 10 ROWS ONLY")]
    public void Limit_ReturnsCorrectSyntax(string dialect, string expected) =>
        Assert.AreEqual(expected, GetDialect(dialect).Limit("10"));

    [TestMethod]
    public void SqlServer_Paginate_ReturnsOffsetFetch() =>
        Assert.AreEqual("OFFSET 20 ROWS FETCH NEXT 10 ROWS ONLY", SqlDefine.SqlServer.Paginate("10", "20"));

    [TestMethod]
    public void MySql_Paginate_ReturnsLimitOffset() =>
        Assert.AreEqual("LIMIT 10 OFFSET 20", SqlDefine.MySql.Paginate("10", "20"));

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

    #region Type Casting

    [TestMethod]
    public void SqlServer_Cast_ReturnsCastExpression() =>
        Assert.AreEqual("CAST(col AS VARCHAR(100))", SqlDefine.SqlServer.Cast("col", "VARCHAR(100)"));

    [TestMethod]
    public void PostgreSql_Cast_ReturnsDoubleColonSyntax() =>
        Assert.AreEqual("(col)::VARCHAR(100)", SqlDefine.PostgreSql.Cast("col", "VARCHAR(100)"));

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
        Assert.AreEqual(SqlDefine.SQLite, SqlDefine.GetDialect(Annotations.SqlDefineTypes.SQLite));
        Assert.AreEqual(SqlDefine.MySql, SqlDefine.GetDialect(Annotations.SqlDefineTypes.MySql));
        Assert.AreEqual(SqlDefine.PostgreSql, SqlDefine.GetDialect(Annotations.SqlDefineTypes.PostgreSql));
        Assert.AreEqual(SqlDefine.SqlServer, SqlDefine.GetDialect(Annotations.SqlDefineTypes.SqlServer));
        Assert.AreEqual(SqlDefine.Oracle, SqlDefine.GetDialect(Annotations.SqlDefineTypes.Oracle));
        Assert.AreEqual(SqlDefine.DB2, SqlDefine.GetDialect(Annotations.SqlDefineTypes.DB2));
    }

    [TestMethod]
    public void GetDialect_ThrowsForUnsupportedType() =>
        Assert.ThrowsException<System.NotSupportedException>(() => SqlDefine.GetDialect((Annotations.SqlDefineTypes)999));

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
