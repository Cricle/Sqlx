using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;

namespace Sqlx.Tests;

/// <summary>
/// Boundary and edge case tests for SQL dialects.
/// </summary>
[TestClass]
public class SqlDialectBoundaryTests
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
        // This tests how dialects handle quotes in column names
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
        // Standard SQL doesn't escape backslashes
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
        Assert.AreEqual(Annotations.SqlDefineTypes.SQLite, SqlDefine.SQLite.DbType);
        Assert.AreEqual(Annotations.SqlDefineTypes.MySql, SqlDefine.MySql.DbType);
        Assert.AreEqual(Annotations.SqlDefineTypes.PostgreSql, SqlDefine.PostgreSql.DbType);
        Assert.AreEqual(Annotations.SqlDefineTypes.SqlServer, SqlDefine.SqlServer.DbType);
        Assert.AreEqual(Annotations.SqlDefineTypes.Oracle, SqlDefine.Oracle.DbType);
        Assert.AreEqual(Annotations.SqlDefineTypes.DB2, SqlDefine.DB2.DbType);
    }

    #endregion

    #region Oracle-Specific DateAdd Tests

    [TestMethod]
    public void Oracle_DateAdd_DifferentIntervals_UseCorrectSyntax()
    {
        Assert.AreEqual("(col + 1)", SqlDefine.Oracle.DateAdd("DAY", "1", "col"));
        Assert.AreEqual("ADD_MONTHS(col, 1)", SqlDefine.Oracle.DateAdd("MONTH", "1", "col"));
        Assert.AreEqual("ADD_MONTHS(col, 1 * 12)", SqlDefine.Oracle.DateAdd("YEAR", "1", "col"));
        Assert.AreEqual("(col + 1/24)", SqlDefine.Oracle.DateAdd("HOUR", "1", "col"));
        Assert.AreEqual("(col + 1/1440)", SqlDefine.Oracle.DateAdd("MINUTE", "1", "col"));
        Assert.AreEqual("(col + 1/86400)", SqlDefine.Oracle.DateAdd("SECOND", "1", "col"));
    }

    #endregion

    #region SQLite-Specific DatePart Tests

    [TestMethod]
    public void SQLite_DatePart_UsesStrftime()
    {
        Assert.IsTrue(SqlDefine.SQLite.DatePart("YEAR", "col").Contains("STRFTIME"));
        Assert.IsTrue(SqlDefine.SQLite.DatePart("MONTH", "col").Contains("STRFTIME"));
        Assert.IsTrue(SqlDefine.SQLite.DatePart("DAY", "col").Contains("STRFTIME"));
    }

    #endregion

    #region DB2-Specific Tests

    [TestMethod]
    public void DB2_DateDiff_UsesTimestampDiffCodes()
    {
        // DB2 uses numeric codes for TIMESTAMPDIFF
        var result = SqlDefine.DB2.DateDiff("DAY", "start", "end");
        Assert.IsTrue(result.Contains("TIMESTAMPDIFF"));
        Assert.IsTrue(result.Contains("16")); // DAY code
    }

    #endregion
}
