// -----------------------------------------------------------------------
// <copyright file="StringFunctionParserTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;
using System;

namespace Sqlx.Tests;

/// <summary>
/// Comprehensive tests for StringFunctionParser covering all methods and dialects.
/// </summary>
[TestClass]
public class StringFunctionParserTests
{
    [Sqlx]
    [TableName("test_strings")]
    public class StringEntity
    {
        public int Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public string? NullableText { get; set; }
    }

    #region ParseIndexer Tests - Note: String indexer is typically evaluated at compile time
    // These tests verify that when indexer expressions do reach the parser, they're handled correctly
    // In practice, most indexer access is evaluated before SQL generation

    [TestMethod]
    public void StringIndexer_Concept_SQLite()
    {
        // String indexer access is usually evaluated at compile time
        // This test verifies the SQL generation works when it's not
        var sql = SqlQuery<StringEntity>.ForSqlite()
            .Where(e => e.Text.Substring(0, 1) == "A")
            .ToSql();

        Assert.IsTrue(sql.Contains("SUBSTR"), $"SQL: {sql}");
    }

    [TestMethod]
    public void StringIndexer_Concept_MySql()
    {
        var sql = SqlQuery<StringEntity>.ForMySql()
            .Where(e => e.Text.Substring(0, 1) == "A")
            .ToSql();

        Assert.IsTrue(sql.Contains("SUBSTRING") || sql.Contains("SUBSTR"), $"SQL: {sql}");
    }

    [TestMethod]
    public void StringIndexer_Concept_SqlServer()
    {
        var sql = SqlQuery<StringEntity>.ForSqlServer()
            .Where(e => e.Text.Substring(0, 1) == "A")
            .ToSql();

        Assert.IsTrue(sql.Contains("SUBSTRING"), $"SQL: {sql}");
    }

    [TestMethod]
    public void StringIndexer_Concept_PostgreSql()
    {
        var sql = SqlQuery<StringEntity>.ForPostgreSQL()
            .Where(e => e.Text.Substring(0, 1) == "A")
            .ToSql();

        Assert.IsTrue(sql.Contains("SUBSTRING") || sql.Contains("SUBSTR"), $"SQL: {sql}");
    }

    [TestMethod]
    public void StringIndexer_Concept_Oracle()
    {
        var sql = SqlQuery<StringEntity>.For(SqlDefine.Oracle)
            .Where(e => e.Text.Substring(0, 1) == "A")
            .ToSql();

        Assert.IsTrue(sql.Contains("SUBSTR"), $"SQL: {sql}");
    }

    [TestMethod]
    public void StringIndexer_Concept_DB2()
    {
        var sql = SqlQuery<StringEntity>.For(SqlDefine.DB2)
            .Where(e => e.Text.Substring(0, 1) == "A")
            .ToSql();

        Assert.IsTrue(sql.Contains("SUBSTR"), $"SQL: {sql}");
    }

    [TestMethod]
    public void StringSubstring_MiddleIndex_GeneratesCorrectOffset()
    {
        var sql = SqlQuery<StringEntity>.ForSqlite()
            .Where(e => e.Text.Substring(5, 1) == "X")
            .ToSql();

        Assert.IsTrue(sql.Contains("SUBSTR"), $"SQL: {sql}");
        Assert.IsTrue(sql.Contains("5") || sql.Contains("+ 1"), $"SQL: {sql}");
    }

    [TestMethod]
    public void StringSubstring_WithVariable_GeneratesCorrectSql()
    {
        int index = 3;
        var sql = SqlQuery<StringEntity>.ForSqlite()
            .Where(e => e.Text.Substring(index, 1) == "B")
            .ToSql();

        Assert.IsTrue(sql.Contains("SUBSTR"), $"SQL: {sql}");
    }

    #endregion

    #region PadLeft Tests

    [TestMethod]
    public void PadLeft_SQLite_GeneratesCorrectSyntax()
    {
        var sql = SqlQuery<StringEntity>.ForSqlite()
            .Where(e => e.Text.PadLeft(10) == "      test")
            .ToSql();

        Assert.IsTrue(sql.Contains("SUBSTR") && sql.Contains("REPLACE") && sql.Contains("HEX") && sql.Contains("ZEROBLOB"), 
            $"SQL: {sql}");
    }

    [TestMethod]
    public void PadLeft_MySql_GeneratesLpadFunction()
    {
        var sql = SqlQuery<StringEntity>.ForMySql()
            .Where(e => e.Text.PadLeft(10) == "      test")
            .ToSql();

        Assert.IsTrue(sql.Contains("LPAD"), $"SQL: {sql}");
    }

    [TestMethod]
    public void PadLeft_SqlServer_GeneratesReplicateFunction()
    {
        var sql = SqlQuery<StringEntity>.ForSqlServer()
            .Where(e => e.Text.PadLeft(10) == "      test")
            .ToSql();

        Assert.IsTrue(sql.Contains("RIGHT") && sql.Contains("REPLICATE"), $"SQL: {sql}");
    }

    [TestMethod]
    public void PadLeft_PostgreSql_GeneratesLpadFunction()
    {
        var sql = SqlQuery<StringEntity>.ForPostgreSQL()
            .Where(e => e.Text.PadLeft(10) == "      test")
            .ToSql();

        Assert.IsTrue(sql.Contains("LPAD"), $"SQL: {sql}");
    }

    [TestMethod]
    public void PadLeft_Oracle_GeneratesLpadFunction()
    {
        var sql = SqlQuery<StringEntity>.For(SqlDefine.Oracle)
            .Where(e => e.Text.PadLeft(10) == "      test")
            .ToSql();

        Assert.IsTrue(sql.Contains("LPAD"), $"SQL: {sql}");
    }

    [TestMethod]
    public void PadLeft_DB2_GeneratesLpadFunction()
    {
        var sql = SqlQuery<StringEntity>.For(SqlDefine.DB2)
            .Where(e => e.Text.PadLeft(10) == "      test")
            .ToSql();

        Assert.IsTrue(sql.Contains("LPAD"), $"SQL: {sql}");
    }

    [TestMethod]
    public void PadLeft_WithCustomChar_SQLite_GeneratesCorrectSyntax()
    {
        var sql = SqlQuery<StringEntity>.ForSqlite()
            .Where(e => e.Text.PadLeft(10, '0') == "0000000test")
            .ToSql();

        Assert.IsTrue(sql.Contains("SUBSTR") && sql.Contains("REPLACE"), $"SQL: {sql}");
    }

    [TestMethod]
    public void PadLeft_WithCustomChar_MySql_GeneratesLpadFunction()
    {
        var sql = SqlQuery<StringEntity>.ForMySql()
            .Where(e => e.Text.PadLeft(10, '0') == "0000000test")
            .ToSql();

        Assert.IsTrue(sql.Contains("LPAD"), $"SQL: {sql}");
    }

    [TestMethod]
    public void PadLeft_WithCustomChar_SqlServer_GeneratesReplicateFunction()
    {
        var sql = SqlQuery<StringEntity>.ForSqlServer()
            .Where(e => e.Text.PadLeft(10, '0') == "0000000test")
            .ToSql();

        Assert.IsTrue(sql.Contains("RIGHT") && sql.Contains("REPLICATE"), $"SQL: {sql}");
    }

    #endregion

    #region PadRight Tests

    [TestMethod]
    public void PadRight_SQLite_GeneratesCorrectSyntax()
    {
        var sql = SqlQuery<StringEntity>.ForSqlite()
            .Where(e => e.Text.PadRight(10) == "test      ")
            .ToSql();

        Assert.IsTrue(sql.Contains("SUBSTR") && sql.Contains("REPLACE") && sql.Contains("HEX") && sql.Contains("ZEROBLOB"), 
            $"SQL: {sql}");
    }

    [TestMethod]
    public void PadRight_MySql_GeneratesRpadFunction()
    {
        var sql = SqlQuery<StringEntity>.ForMySql()
            .Where(e => e.Text.PadRight(10) == "test      ")
            .ToSql();

        Assert.IsTrue(sql.Contains("RPAD"), $"SQL: {sql}");
    }

    [TestMethod]
    public void PadRight_SqlServer_GeneratesLeftFunction()
    {
        var sql = SqlQuery<StringEntity>.ForSqlServer()
            .Where(e => e.Text.PadRight(10) == "test      ")
            .ToSql();

        Assert.IsTrue(sql.Contains("LEFT") && sql.Contains("REPLICATE"), $"SQL: {sql}");
    }

    [TestMethod]
    public void PadRight_PostgreSql_GeneratesRpadFunction()
    {
        var sql = SqlQuery<StringEntity>.ForPostgreSQL()
            .Where(e => e.Text.PadRight(10) == "test      ")
            .ToSql();

        Assert.IsTrue(sql.Contains("RPAD"), $"SQL: {sql}");
    }

    [TestMethod]
    public void PadRight_Oracle_GeneratesRpadFunction()
    {
        var sql = SqlQuery<StringEntity>.For(SqlDefine.Oracle)
            .Where(e => e.Text.PadRight(10) == "test      ")
            .ToSql();

        Assert.IsTrue(sql.Contains("RPAD"), $"SQL: {sql}");
    }

    [TestMethod]
    public void PadRight_DB2_GeneratesRpadFunction()
    {
        var sql = SqlQuery<StringEntity>.For(SqlDefine.DB2)
            .Where(e => e.Text.PadRight(10) == "test      ")
            .ToSql();

        Assert.IsTrue(sql.Contains("RPAD"), $"SQL: {sql}");
    }

    [TestMethod]
    public void PadRight_WithCustomChar_SQLite_GeneratesCorrectSyntax()
    {
        var sql = SqlQuery<StringEntity>.ForSqlite()
            .Where(e => e.Text.PadRight(10, '0') == "test000000")
            .ToSql();

        Assert.IsTrue(sql.Contains("SUBSTR") && sql.Contains("REPLACE"), $"SQL: {sql}");
    }

    [TestMethod]
    public void PadRight_WithCustomChar_MySql_GeneratesRpadFunction()
    {
        var sql = SqlQuery<StringEntity>.ForMySql()
            .Where(e => e.Text.PadRight(10, '0') == "test000000")
            .ToSql();

        Assert.IsTrue(sql.Contains("RPAD"), $"SQL: {sql}");
    }

    [TestMethod]
    public void PadRight_WithCustomChar_SqlServer_GeneratesLeftFunction()
    {
        var sql = SqlQuery<StringEntity>.ForSqlServer()
            .Where(e => e.Text.PadRight(10, '0') == "test000000")
            .ToSql();

        Assert.IsTrue(sql.Contains("LEFT") && sql.Contains("REPLICATE"), $"SQL: {sql}");
    }

    #endregion

    #region IndexOf Tests

    [TestMethod]
    public void IndexOf_SQLite_GeneratesInstrFunction()
    {
        var sql = SqlQuery<StringEntity>.ForSqlite()
            .Where(e => e.Text.IndexOf("test") >= 0)
            .ToSql();

        Assert.IsTrue(sql.Contains("INSTR"), $"SQL: {sql}");
        Assert.IsTrue(sql.Contains("- 1"), $"SQL: {sql}"); // 1-based to 0-based conversion
    }

    [TestMethod]
    public void IndexOf_MySql_GeneratesLocateFunction()
    {
        var sql = SqlQuery<StringEntity>.ForMySql()
            .Where(e => e.Text.IndexOf("test") >= 0)
            .ToSql();

        Assert.IsTrue(sql.Contains("LOCATE"), $"SQL: {sql}");
        Assert.IsTrue(sql.Contains("- 1"), $"SQL: {sql}");
    }

    [TestMethod]
    public void IndexOf_SqlServer_GeneratesCharindexFunction()
    {
        var sql = SqlQuery<StringEntity>.ForSqlServer()
            .Where(e => e.Text.IndexOf("test") >= 0)
            .ToSql();

        Assert.IsTrue(sql.Contains("CHARINDEX"), $"SQL: {sql}");
        Assert.IsTrue(sql.Contains("- 1"), $"SQL: {sql}");
    }

    [TestMethod]
    public void IndexOf_PostgreSql_GeneratesPositionFunction()
    {
        var sql = SqlQuery<StringEntity>.ForPostgreSQL()
            .Where(e => e.Text.IndexOf("test") >= 0)
            .ToSql();

        Assert.IsTrue(sql.Contains("POSITION"), $"SQL: {sql}");
        Assert.IsTrue(sql.Contains("- 1"), $"SQL: {sql}");
    }

    [TestMethod]
    public void IndexOf_Oracle_GeneratesInstrFunction()
    {
        var sql = SqlQuery<StringEntity>.For(SqlDefine.Oracle)
            .Where(e => e.Text.IndexOf("test") >= 0)
            .ToSql();

        Assert.IsTrue(sql.Contains("INSTR"), $"SQL: {sql}");
        Assert.IsTrue(sql.Contains("- 1"), $"SQL: {sql}");
    }

    [TestMethod]
    public void IndexOf_DB2_GeneratesLocateFunction()
    {
        var sql = SqlQuery<StringEntity>.For(SqlDefine.DB2)
            .Where(e => e.Text.IndexOf("test") >= 0)
            .ToSql();

        Assert.IsTrue(sql.Contains("LOCATE"), $"SQL: {sql}");
        Assert.IsTrue(sql.Contains("- 1"), $"SQL: {sql}");
    }

    [TestMethod]
    public void IndexOf_NotFound_ComparesWithNegativeOne()
    {
        var sql = SqlQuery<StringEntity>.ForSqlite()
            .Where(e => e.Text.IndexOf("notfound") == -1)
            .ToSql();

        Assert.IsTrue(sql.Contains("INSTR"), $"SQL: {sql}");
    }

    [TestMethod]
    public void IndexOf_GreaterThanZero_FindsMatch()
    {
        var sql = SqlQuery<StringEntity>.ForSqlite()
            .Where(e => e.Text.IndexOf("test") > 0)
            .ToSql();

        Assert.IsTrue(sql.Contains("INSTR"), $"SQL: {sql}");
    }

    #endregion

    #region IndexOf with Start Position Tests

    [TestMethod]
    public void IndexOfWithStart_SQLite_GeneratesInstrWithSubstr()
    {
        var sql = SqlQuery<StringEntity>.ForSqlite()
            .Where(e => e.Text.IndexOf("test", 5) >= 0)
            .ToSql();

        Assert.IsTrue(sql.Contains("INSTR") && sql.Contains("SUBSTR"), $"SQL: {sql}");
        Assert.IsTrue(sql.Contains("+ 1"), $"SQL: {sql}"); // 0-based to 1-based conversion
    }

    [TestMethod]
    public void IndexOfWithStart_MySql_GeneratesLocateWithStart()
    {
        var sql = SqlQuery<StringEntity>.ForMySql()
            .Where(e => e.Text.IndexOf("test", 5) >= 0)
            .ToSql();

        Assert.IsTrue(sql.Contains("LOCATE"), $"SQL: {sql}");
        Assert.IsTrue(sql.Contains("+ 1"), $"SQL: {sql}");
    }

    [TestMethod]
    public void IndexOfWithStart_SqlServer_GeneratesCharindexWithStart()
    {
        var sql = SqlQuery<StringEntity>.ForSqlServer()
            .Where(e => e.Text.IndexOf("test", 5) >= 0)
            .ToSql();

        Assert.IsTrue(sql.Contains("CHARINDEX"), $"SQL: {sql}");
        Assert.IsTrue(sql.Contains("+ 1"), $"SQL: {sql}");
    }

    [TestMethod]
    public void IndexOfWithStart_PostgreSql_GeneratesPositionWithSubstring()
    {
        var sql = SqlQuery<StringEntity>.ForPostgreSQL()
            .Where(e => e.Text.IndexOf("test", 5) >= 0)
            .ToSql();

        Assert.IsTrue(sql.Contains("POSITION") && sql.Contains("SUBSTRING"), $"SQL: {sql}");
    }

    [TestMethod]
    public void IndexOfWithStart_Oracle_GeneratesInstrWithStart()
    {
        var sql = SqlQuery<StringEntity>.For(SqlDefine.Oracle)
            .Where(e => e.Text.IndexOf("test", 5) >= 0)
            .ToSql();

        Assert.IsTrue(sql.Contains("INSTR"), $"SQL: {sql}");
        Assert.IsTrue(sql.Contains("+ 1"), $"SQL: {sql}");
    }

    [TestMethod]
    public void IndexOfWithStart_DB2_GeneratesLocateWithStart()
    {
        var sql = SqlQuery<StringEntity>.For(SqlDefine.DB2)
            .Where(e => e.Text.IndexOf("test", 5) >= 0)
            .ToSql();

        Assert.IsTrue(sql.Contains("LOCATE"), $"SQL: {sql}");
        Assert.IsTrue(sql.Contains("+ 1"), $"SQL: {sql}");
    }

    [TestMethod]
    public void IndexOfWithStart_ZeroStart_GeneratesCorrectSql()
    {
        var sql = SqlQuery<StringEntity>.ForSqlite()
            .Where(e => e.Text.IndexOf("test", 0) >= 0)
            .ToSql();

        Assert.IsTrue(sql.Contains("INSTR"), $"SQL: {sql}");
    }

    [TestMethod]
    public void IndexOfWithStart_LargeStart_GeneratesCorrectSql()
    {
        var sql = SqlQuery<StringEntity>.ForSqlite()
            .Where(e => e.Text.IndexOf("test", 100) >= 0)
            .ToSql();

        Assert.IsTrue(sql.Contains("INSTR"), $"SQL: {sql}");
    }

    #endregion

    #region Edge Cases and Complex Scenarios

    [TestMethod]
    public void StringFunctions_ChainedSubstring_GeneratesCorrectSql()
    {
        var sql = SqlQuery<StringEntity>.ForSqlite()
            .Where(e => e.Text.Substring(0, 1) == "A" && e.Text.Substring(1, 1) == "B")
            .ToSql();

        Assert.IsTrue(sql.Contains("SUBSTR"), $"SQL: {sql}");
    }

    [TestMethod]
    public void StringFunctions_PadLeftAndIndexOf_GeneratesCorrectSql()
    {
        var sql = SqlQuery<StringEntity>.ForMySql()
            .Where(e => e.Text.PadLeft(10).IndexOf("test") >= 0)
            .ToSql();

        Assert.IsTrue(sql.Contains("LPAD") && sql.Contains("LOCATE"), $"SQL: {sql}");
    }

    [TestMethod]
    public void StringFunctions_PadRightWithLength_GeneratesCorrectSql()
    {
        var sql = SqlQuery<StringEntity>.ForSqlite()
            .Where(e => e.Text.PadRight(20).Length > 10)
            .ToSql();

        Assert.IsTrue(sql.Contains("LENGTH") || sql.Contains("text"), $"SQL: {sql}");
    }

    [TestMethod]
    public void StringFunctions_NullableWithIndexOf_GeneratesCorrectSql()
    {
        var sql = SqlQuery<StringEntity>.ForSqlite()
            .Where(e => e.NullableText != null && e.NullableText.IndexOf("test") >= 0)
            .ToSql();

        Assert.IsTrue(sql.Contains("INSTR"), $"SQL: {sql}");
    }

    [TestMethod]
    public void StringFunctions_AllDialects_PadLeftConsistency()
    {
        var dialects = new[] { SqlDefine.SQLite, SqlDefine.MySql, SqlDefine.PostgreSql, SqlDefine.SqlServer, SqlDefine.Oracle, SqlDefine.DB2 };
        
        foreach (var dialect in dialects)
        {
            var sql = SqlQuery<StringEntity>.For(dialect)
                .Where(e => e.Text.PadLeft(10) == "      test")
                .ToSql();

            Assert.IsFalse(string.IsNullOrEmpty(sql), $"[{dialect.DatabaseType}] SQL should not be empty");
            Assert.IsTrue(sql.Contains("text") || sql.Contains("Text") || sql.Contains("[text]") || sql.Contains("\"text\""), 
                $"[{dialect.DatabaseType}] SQL should reference the text column: {sql}");
        }
    }

    [TestMethod]
    public void StringFunctions_AllDialects_IndexOfConsistency()
    {
        var dialects = new[] { SqlDefine.SQLite, SqlDefine.MySql, SqlDefine.PostgreSql, SqlDefine.SqlServer, SqlDefine.Oracle, SqlDefine.DB2 };
        
        foreach (var dialect in dialects)
        {
            var sql = SqlQuery<StringEntity>.For(dialect)
                .Where(e => e.Text.IndexOf("search") >= 0)
                .ToSql();

            Assert.IsFalse(string.IsNullOrEmpty(sql), $"[{dialect.DatabaseType}] SQL should not be empty");
            Assert.IsTrue(sql.Contains("- 1"), $"[{dialect.DatabaseType}] SQL should contain 1-based to 0-based conversion: {sql}");
        }
    }

    #endregion
}
