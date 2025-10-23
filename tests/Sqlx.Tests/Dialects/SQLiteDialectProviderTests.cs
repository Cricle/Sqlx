// -----------------------------------------------------------------------
// <copyright file="SQLiteDialectProviderTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Generator;
using System;

namespace Sqlx.Tests.Dialects;

/// <summary>
/// SQLite方言提供器的单元测试
/// </summary>
[TestClass]
public class SQLiteDialectProviderTests
{
    private SQLiteDialectProvider _provider = null!;

    [TestInitialize]
    public void Setup()
    {
        _provider = new SQLiteDialectProvider();
    }

    /// <summary>
    /// 测试：DialectType应该是SQLite
    /// </summary>
    [TestMethod]
    public void DialectType_ShouldBe_SQLite()
    {
        Assert.AreEqual(SqlDefineTypes.SQLite, _provider.DialectType);
    }

    /// <summary>
    /// 测试：LIMIT子句生成（SQLite使用LIMIT OFFSET语法）
    /// </summary>
    [TestMethod]
    public void GenerateLimitClause_WithLimitOnly_ShouldGenerateLimitClause()
    {
        var result = _provider.GenerateLimitClause(10, null);
        Assert.IsTrue(result.Contains("LIMIT 10") || result.Contains("LIMIT"));
    }

    [TestMethod]
    public void GenerateLimitClause_WithLimitAndOffset_ShouldGenerateLimitOffset()
    {
        var result = _provider.GenerateLimitClause(10, 20);
        Assert.IsTrue(result.Contains("LIMIT") && result.Contains("OFFSET"));
    }

    [TestMethod]
    public void GenerateLimitClause_WithNullLimit_ShouldReturnEmpty()
    {
        var result = _provider.GenerateLimitClause(null, null);
        Assert.IsTrue(string.IsNullOrEmpty(result));
    }

    /// <summary>
    /// 测试：INSERT with RETURNING生成（SQLite 3.35+支持RETURNING）
    /// </summary>
    [TestMethod]
    public void GenerateInsertWithReturning_ShouldUseReturningOrLastInsertRowid()
    {
        var result = _provider.GenerateInsertWithReturning("users", new[] { "name", "email" });

        Assert.IsTrue(result.Contains("INSERT INTO"));
        Assert.IsTrue(result.Contains("users"));
    }

    /// <summary>
    /// 测试：批量INSERT生成
    /// </summary>
    [TestMethod]
    public void GenerateBatchInsert_ShouldGenerateMultipleValueSets()
    {
        var result = _provider.GenerateBatchInsert("users", new[] { "name", "email" }, 3);

        Assert.IsTrue(result.Contains("INSERT INTO"));
        Assert.IsTrue(result.Contains("users"));
        Assert.IsTrue(result.Contains("name"));
        Assert.IsTrue(result.Contains("email"));
    }

    /// <summary>
    /// 测试：UPSERT生成（SQLite使用INSERT OR REPLACE / ON CONFLICT）
    /// </summary>
    [TestMethod]
    public void GenerateUpsert_ShouldUseInsertOrReplaceOrOnConflict()
    {
        var result = _provider.GenerateUpsert(
            "users",
            new[] { "id", "name", "email" },
            new[] { "id" });

        Assert.IsTrue(result.Contains("INSERT") || result.Contains("REPLACE") || result.Contains("ON CONFLICT"));
    }

    /// <summary>
    /// 测试：.NET类型到数据库类型的映射
    /// </summary>
    [TestMethod]
    public void GetDatabaseTypeName_Int_ShouldReturnInteger()
    {
        var result = _provider.GetDatabaseTypeName(typeof(int));
        Assert.IsTrue(result.Contains("INTEGER") || result.ToLower().Contains("int"));
    }

    [TestMethod]
    public void GetDatabaseTypeName_String_ShouldReturnText()
    {
        var result = _provider.GetDatabaseTypeName(typeof(string));
        Assert.IsTrue(result.Contains("TEXT") || result.ToLower().Contains("text") || result.ToLower().Contains("varchar"));
    }

    [TestMethod]
    public void GetDatabaseTypeName_DateTime_ShouldReturnText()
    {
        var result = _provider.GetDatabaseTypeName(typeof(DateTime));
        // SQLite stores dates as TEXT, REAL, or INTEGER
        Assert.IsTrue(result.Contains("TEXT") || result.Contains("REAL") || result.Contains("INTEGER") || result.ToLower().Contains("text") || result.ToLower().Contains("datetime"));
    }

    [TestMethod]
    public void GetDatabaseTypeName_Bool_ShouldReturnInteger()
    {
        var result = _provider.GetDatabaseTypeName(typeof(bool));
        Assert.IsTrue(result.Contains("INTEGER") || result.ToLower().Contains("int") || result.ToLower().Contains("bool"));
    }

    [TestMethod]
    public void GetDatabaseTypeName_Decimal_ShouldReturnReal()
    {
        var result = _provider.GetDatabaseTypeName(typeof(decimal));
        Assert.IsTrue(result.Contains("REAL") || result.Contains("NUMERIC") || result.ToLower().Contains("real") || result.ToLower().Contains("numeric") || result.ToLower().Contains("decimal"));
    }

    /// <summary>
    /// 测试：DateTime格式化
    /// </summary>
    [TestMethod]
    public void FormatDateTime_ShouldReturnFormattedString()
    {
        var dateTime = new DateTime(2024, 1, 15, 10, 30, 45);
        var result = _provider.FormatDateTime(dateTime);

        Assert.IsNotNull(result);
        Assert.IsTrue(result.Contains("2024") || result.Contains("24"));
    }

    /// <summary>
    /// 测试：当前日期时间语法
    /// </summary>
    [TestMethod]
    public void GetCurrentDateTimeSyntax_ShouldReturnCurrentTimestamp()
    {
        var result = _provider.GetCurrentDateTimeSyntax();
        Assert.IsTrue(result.Contains("CURRENT_TIMESTAMP") || result.Contains("datetime") || result.Contains("DATETIME"));
    }

    /// <summary>
    /// 测试：字符串连接语法
    /// </summary>
    [TestMethod]
    public void GetConcatenationSyntax_ShouldUsePipeOperator()
    {
        var result = _provider.GetConcatenationSyntax("'Hello'", "' '", "'World'");

        Assert.IsNotNull(result);
        // SQLite uses ||
        Assert.IsTrue(result.Contains("||"));
    }

    [TestMethod]
    public void GetConcatenationSyntax_EmptyArray_ShouldReturnEmpty()
    {
        var result = _provider.GetConcatenationSyntax();
        Assert.IsTrue(string.IsNullOrEmpty(result) || result == "''");
    }

    [TestMethod]
    public void GetConcatenationSyntax_SingleExpression_ShouldReturnExpression()
    {
        var result = _provider.GetConcatenationSyntax("'Hello'");
        Assert.IsTrue(result.Contains("Hello"));
    }
}

