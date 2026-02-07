// -----------------------------------------------------------------------
// <copyright file="DB2OracleDialectTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;
using System;

namespace Sqlx.Tests;

/// <summary>
/// Comprehensive tests for DB2 and Oracle dialect-specific features.
/// </summary>
[TestClass]
public class DB2OracleDialectTests
{
    [Sqlx]
    [TableName("test_table")]
    public class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Age { get; set; }
        public decimal Amount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    #region DB2 Dialect Tests

    [TestMethod]
    public void DB2_BasicSelect_GeneratesCorrectSql()
    {
        var sql = SqlQuery<TestEntity>.For(SqlDefine.DB2)
            .ToSql();

        Assert.IsTrue(sql.Contains("SELECT"), $"SQL: {sql}");
        Assert.IsTrue(sql.Contains("FROM"), $"SQL: {sql}");
        Assert.IsTrue(sql.Contains("test_table") || sql.Contains("TestEntity"), $"SQL: {sql}");
    }

    [TestMethod]
    public void DB2_Where_GeneratesCorrectSql()
    {
        var sql = SqlQuery<TestEntity>.For(SqlDefine.DB2)
            .Where(e => e.Id == 1)
            .ToSql();

        Assert.IsTrue(sql.Contains("WHERE"), $"SQL: {sql}");
        Assert.IsTrue(sql.Contains("\"id\""), $"SQL: {sql}");
    }

    [TestMethod]
    public void DB2_OrderBy_GeneratesCorrectSql()
    {
        var sql = SqlQuery<TestEntity>.For(SqlDefine.DB2)
            .OrderBy(e => e.Name)
            .ToSql();

        Assert.IsTrue(sql.Contains("ORDER BY"), $"SQL: {sql}");
        Assert.IsTrue(sql.Contains("\"name\""), $"SQL: {sql}");
    }

    [TestMethod]
    public void DB2_Limit_GeneratesCorrectSyntax()
    {
        var sql = SqlQuery<TestEntity>.For(SqlDefine.DB2)
            .Take(10)
            .ToSql();

        Assert.IsTrue(sql.Contains("FETCH FIRST") && sql.Contains("ROWS ONLY"), $"SQL: {sql}");
    }

    [TestMethod]
    public void DB2_Offset_GeneratesCorrectSyntax()
    {
        var sql = SqlQuery<TestEntity>.For(SqlDefine.DB2)
            .Skip(5)
            .ToSql();

        Assert.IsTrue(sql.Contains("OFFSET") || sql.Contains("5"), $"SQL: {sql}");
    }

    [TestMethod]
    public void DB2_LimitAndOffset_GeneratesCorrectSyntax()
    {
        var sql = SqlQuery<TestEntity>.For(SqlDefine.DB2)
            .Skip(5)
            .Take(10)
            .ToSql();

        Assert.IsTrue(sql.Contains("OFFSET") && sql.Contains("ROWS"), $"SQL: {sql}");
        Assert.IsTrue(sql.Contains("FETCH FIRST") && sql.Contains("ROWS ONLY"), $"SQL: {sql}");
    }

    [TestMethod]
    public void DB2_StringFunctions_ToUpper()
    {
        var sql = SqlQuery<TestEntity>.For(SqlDefine.DB2)
            .Where(e => e.Name.ToUpper() == "TEST")
            .ToSql();

        Assert.IsTrue(sql.Contains("UPPER"), $"SQL: {sql}");
    }

    [TestMethod]
    public void DB2_StringFunctions_ToLower()
    {
        var sql = SqlQuery<TestEntity>.For(SqlDefine.DB2)
            .Where(e => e.Name.ToLower() == "test")
            .ToSql();

        Assert.IsTrue(sql.Contains("LOWER"), $"SQL: {sql}");
    }

    [TestMethod]
    public void DB2_StringFunctions_Trim()
    {
        var sql = SqlQuery<TestEntity>.For(SqlDefine.DB2)
            .Where(e => e.Name.Trim() == "test")
            .ToSql();

        Assert.IsTrue(sql.Contains("TRIM"), $"SQL: {sql}");
    }

    [TestMethod]
    public void DB2_StringFunctions_Length()
    {
        var sql = SqlQuery<TestEntity>.For(SqlDefine.DB2)
            .Where(e => e.Name.Length > 5)
            .ToSql();

        Assert.IsTrue(sql.Contains("LENGTH"), $"SQL: {sql}");
    }

    [TestMethod]
    public void DB2_StringFunctions_Substring()
    {
        var sql = SqlQuery<TestEntity>.For(SqlDefine.DB2)
            .Where(e => e.Name.Substring(0, 3) == "tes")
            .ToSql();

        Assert.IsTrue(sql.Contains("SUBSTR"), $"SQL: {sql}");
    }

    [TestMethod]
    public void DB2_StringFunctions_Replace()
    {
        var sql = SqlQuery<TestEntity>.For(SqlDefine.DB2)
            .Where(e => e.Name.Replace("old", "new") == "test")
            .ToSql();

        Assert.IsTrue(sql.Contains("REPLACE"), $"SQL: {sql}");
    }

    [TestMethod]
    public void DB2_StringFunctions_Concat()
    {
        var sql = SqlQuery<TestEntity>.For(SqlDefine.DB2)
            .Where(e => (e.Name + " " + e.Description) == "test desc")
            .ToSql();

        Assert.IsTrue(sql.Contains("||") || sql.Contains("CONCAT"), $"SQL: {sql}");
    }

    [TestMethod]
    public void DB2_StringFunctions_IndexOf()
    {
        var sql = SqlQuery<TestEntity>.For(SqlDefine.DB2)
            .Where(e => e.Name.IndexOf("test") >= 0)
            .ToSql();

        Assert.IsTrue(sql.Contains("LOCATE"), $"SQL: {sql}");
    }

    [TestMethod]
    public void DB2_StringFunctions_PadLeft()
    {
        var sql = SqlQuery<TestEntity>.For(SqlDefine.DB2)
            .Where(e => e.Name.PadLeft(10) == "      test")
            .ToSql();

        Assert.IsTrue(sql.Contains("LPAD"), $"SQL: {sql}");
    }

    [TestMethod]
    public void DB2_StringFunctions_PadRight()
    {
        var sql = SqlQuery<TestEntity>.For(SqlDefine.DB2)
            .Where(e => e.Name.PadRight(10) == "test      ")
            .ToSql();

        Assert.IsTrue(sql.Contains("RPAD"), $"SQL: {sql}");
    }

    [TestMethod]
    public void DB2_MathFunctions_Abs()
    {
        var sql = SqlQuery<TestEntity>.For(SqlDefine.DB2)
            .Where(e => Math.Abs(e.Age) > 10)
            .ToSql();

        Assert.IsTrue(sql.Contains("ABS"), $"SQL: {sql}");
    }

    [TestMethod]
    public void DB2_MathFunctions_Round()
    {
        var sql = SqlQuery<TestEntity>.For(SqlDefine.DB2)
            .Where(e => Math.Round(e.Amount) > 100)
            .ToSql();

        Assert.IsTrue(sql.Contains("ROUND"), $"SQL: {sql}");
    }

    [TestMethod]
    public void DB2_MathFunctions_Floor()
    {
        var sql = SqlQuery<TestEntity>.For(SqlDefine.DB2)
            .Where(e => Math.Floor(e.Amount) > 100)
            .ToSql();

        Assert.IsTrue(sql.Contains("FLOOR"), $"SQL: {sql}");
    }

    [TestMethod]
    public void DB2_MathFunctions_Ceiling()
    {
        var sql = SqlQuery<TestEntity>.For(SqlDefine.DB2)
            .Where(e => Math.Ceiling(e.Amount) > 100)
            .ToSql();

        Assert.IsTrue(sql.Contains("CEILING") || sql.Contains("CEIL"), $"SQL: {sql}");
    }

    [TestMethod]
    public void DB2_ComplexQuery_WithMultipleConditions()
    {
        var sql = SqlQuery<TestEntity>.For(SqlDefine.DB2)
            .Where(e => e.Age > 18 && e.Name.Contains("test"))
            .OrderBy(e => e.CreatedAt)
            .Skip(10)
            .Take(20)
            .ToSql();

        Assert.IsTrue(sql.Contains("WHERE"), $"SQL: {sql}");
        Assert.IsTrue(sql.Contains("ORDER BY"), $"SQL: {sql}");
        Assert.IsTrue(sql.Contains("OFFSET"), $"SQL: {sql}");
        Assert.IsTrue(sql.Contains("FETCH FIRST"), $"SQL: {sql}");
    }

    #endregion

    #region Oracle Dialect Tests

    [TestMethod]
    public void Oracle_BasicSelect_GeneratesCorrectSql()
    {
        var sql = SqlQuery<TestEntity>.For(SqlDefine.Oracle)
            .ToSql();

        Assert.IsTrue(sql.Contains("SELECT"), $"SQL: {sql}");
        Assert.IsTrue(sql.Contains("FROM"), $"SQL: {sql}");
        Assert.IsTrue(sql.Contains("test_table") || sql.Contains("TestEntity"), $"SQL: {sql}");
    }

    [TestMethod]
    public void Oracle_Where_GeneratesCorrectSql()
    {
        var sql = SqlQuery<TestEntity>.For(SqlDefine.Oracle)
            .Where(e => e.Id == 1)
            .ToSql();

        Assert.IsTrue(sql.Contains("WHERE"), $"SQL: {sql}");
        Assert.IsTrue(sql.Contains("\"id\""), $"SQL: {sql}");
    }

    [TestMethod]
    public void Oracle_OrderBy_GeneratesCorrectSql()
    {
        var sql = SqlQuery<TestEntity>.For(SqlDefine.Oracle)
            .OrderBy(e => e.Name)
            .ToSql();

        Assert.IsTrue(sql.Contains("ORDER BY"), $"SQL: {sql}");
        Assert.IsTrue(sql.Contains("\"name\""), $"SQL: {sql}");
    }

    [TestMethod]
    public void Oracle_Limit_GeneratesCorrectSyntax()
    {
        var sql = SqlQuery<TestEntity>.For(SqlDefine.Oracle)
            .Take(10)
            .ToSql();

        Assert.IsTrue(sql.Contains("FETCH FIRST") && sql.Contains("ROWS ONLY"), $"SQL: {sql}");
    }

    [TestMethod]
    public void Oracle_Offset_GeneratesCorrectSyntax()
    {
        var sql = SqlQuery<TestEntity>.For(SqlDefine.Oracle)
            .Skip(5)
            .ToSql();

        Assert.IsTrue(sql.Contains("OFFSET") || sql.Contains("5"), $"SQL: {sql}");
    }

    [TestMethod]
    public void Oracle_LimitAndOffset_GeneratesCorrectSyntax()
    {
        var sql = SqlQuery<TestEntity>.For(SqlDefine.Oracle)
            .Skip(5)
            .Take(10)
            .ToSql();

        Assert.IsTrue(sql.Contains("OFFSET") || sql.Contains("5"), $"SQL: {sql}");
        Assert.IsTrue(sql.Contains("FETCH") || sql.Contains("10"), $"SQL: {sql}");
    }

    [TestMethod]
    public void Oracle_StringFunctions_ToUpper()
    {
        var sql = SqlQuery<TestEntity>.For(SqlDefine.Oracle)
            .Where(e => e.Name.ToUpper() == "TEST")
            .ToSql();

        Assert.IsTrue(sql.Contains("UPPER"), $"SQL: {sql}");
    }

    [TestMethod]
    public void Oracle_StringFunctions_ToLower()
    {
        var sql = SqlQuery<TestEntity>.For(SqlDefine.Oracle)
            .Where(e => e.Name.ToLower() == "test")
            .ToSql();

        Assert.IsTrue(sql.Contains("LOWER"), $"SQL: {sql}");
    }

    [TestMethod]
    public void Oracle_StringFunctions_Trim()
    {
        var sql = SqlQuery<TestEntity>.For(SqlDefine.Oracle)
            .Where(e => e.Name.Trim() == "test")
            .ToSql();

        Assert.IsTrue(sql.Contains("TRIM"), $"SQL: {sql}");
    }

    [TestMethod]
    public void Oracle_StringFunctions_Length()
    {
        var sql = SqlQuery<TestEntity>.For(SqlDefine.Oracle)
            .Where(e => e.Name.Length > 5)
            .ToSql();

        Assert.IsTrue(sql.Contains("LENGTH"), $"SQL: {sql}");
    }

    [TestMethod]
    public void Oracle_StringFunctions_Substring()
    {
        var sql = SqlQuery<TestEntity>.For(SqlDefine.Oracle)
            .Where(e => e.Name.Substring(0, 3) == "tes")
            .ToSql();

        Assert.IsTrue(sql.Contains("SUBSTR"), $"SQL: {sql}");
    }

    [TestMethod]
    public void Oracle_StringFunctions_Replace()
    {
        var sql = SqlQuery<TestEntity>.For(SqlDefine.Oracle)
            .Where(e => e.Name.Replace("old", "new") == "test")
            .ToSql();

        Assert.IsTrue(sql.Contains("REPLACE"), $"SQL: {sql}");
    }

    [TestMethod]
    public void Oracle_StringFunctions_Concat()
    {
        var sql = SqlQuery<TestEntity>.For(SqlDefine.Oracle)
            .Where(e => (e.Name + " " + e.Description) == "test desc")
            .ToSql();

        Assert.IsTrue(sql.Contains("||") || sql.Contains("CONCAT"), $"SQL: {sql}");
    }

    [TestMethod]
    public void Oracle_StringFunctions_IndexOf()
    {
        var sql = SqlQuery<TestEntity>.For(SqlDefine.Oracle)
            .Where(e => e.Name.IndexOf("test") >= 0)
            .ToSql();

        Assert.IsTrue(sql.Contains("INSTR"), $"SQL: {sql}");
    }

    [TestMethod]
    public void Oracle_StringFunctions_PadLeft()
    {
        var sql = SqlQuery<TestEntity>.For(SqlDefine.Oracle)
            .Where(e => e.Name.PadLeft(10) == "      test")
            .ToSql();

        Assert.IsTrue(sql.Contains("LPAD"), $"SQL: {sql}");
    }

    [TestMethod]
    public void Oracle_StringFunctions_PadRight()
    {
        var sql = SqlQuery<TestEntity>.For(SqlDefine.Oracle)
            .Where(e => e.Name.PadRight(10) == "test      ")
            .ToSql();

        Assert.IsTrue(sql.Contains("RPAD"), $"SQL: {sql}");
    }

    [TestMethod]
    public void Oracle_MathFunctions_Abs()
    {
        var sql = SqlQuery<TestEntity>.For(SqlDefine.Oracle)
            .Where(e => Math.Abs(e.Age) > 10)
            .ToSql();

        Assert.IsTrue(sql.Contains("ABS"), $"SQL: {sql}");
    }

    [TestMethod]
    public void Oracle_MathFunctions_Round()
    {
        var sql = SqlQuery<TestEntity>.For(SqlDefine.Oracle)
            .Where(e => Math.Round(e.Amount) > 100)
            .ToSql();

        Assert.IsTrue(sql.Contains("ROUND"), $"SQL: {sql}");
    }

    [TestMethod]
    public void Oracle_MathFunctions_Floor()
    {
        var sql = SqlQuery<TestEntity>.For(SqlDefine.Oracle)
            .Where(e => Math.Floor(e.Amount) > 100)
            .ToSql();

        Assert.IsTrue(sql.Contains("FLOOR"), $"SQL: {sql}");
    }

    [TestMethod]
    public void Oracle_MathFunctions_Ceiling()
    {
        var sql = SqlQuery<TestEntity>.For(SqlDefine.Oracle)
            .Where(e => Math.Ceiling(e.Amount) > 100)
            .ToSql();

        Assert.IsTrue(sql.Contains("CEILING") || sql.Contains("CEIL"), $"SQL: {sql}");
    }

    [TestMethod]
    public void Oracle_ComplexQuery_WithMultipleConditions()
    {
        var sql = SqlQuery<TestEntity>.For(SqlDefine.Oracle)
            .Where(e => e.Age > 18 && e.Name.Contains("test"))
            .OrderBy(e => e.CreatedAt)
            .Skip(10)
            .Take(20)
            .ToSql();

        Assert.IsTrue(sql.Contains("WHERE"), $"SQL: {sql}");
        Assert.IsTrue(sql.Contains("ORDER BY"), $"SQL: {sql}");
        Assert.IsTrue(sql.Contains("OFFSET") || sql.Contains("10"), $"SQL: {sql}");
        Assert.IsTrue(sql.Contains("FETCH") || sql.Contains("20"), $"SQL: {sql}");
    }

    #endregion

    #region Comparison Tests

    [TestMethod]
    public void DB2_vs_Oracle_ColumnQuoting()
    {
        var db2Sql = SqlQuery<TestEntity>.For(SqlDefine.DB2)
            .ToSql();

        var oracleSql = SqlQuery<TestEntity>.For(SqlDefine.Oracle)
            .ToSql();

        // Both use double quotes
        Assert.IsTrue(db2Sql.Contains("\""), $"DB2 SQL: {db2Sql}");
        Assert.IsTrue(oracleSql.Contains("\""), $"Oracle SQL: {oracleSql}");
    }

    [TestMethod]
    public void DB2_vs_Oracle_LimitSyntax()
    {
        var db2Sql = SqlQuery<TestEntity>.For(SqlDefine.DB2)
            .Take(10)
            .ToSql();

        var oracleSql = SqlQuery<TestEntity>.For(SqlDefine.Oracle)
            .Take(10)
            .ToSql();

        // Both use FETCH FIRST ... ROWS ONLY
        Assert.IsTrue(db2Sql.Contains("FETCH FIRST"), $"DB2 SQL: {db2Sql}");
        Assert.IsTrue(oracleSql.Contains("FETCH FIRST"), $"Oracle SQL: {oracleSql}");
    }

    #endregion
}
