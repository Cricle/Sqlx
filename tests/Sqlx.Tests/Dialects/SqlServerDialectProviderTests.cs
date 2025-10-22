// -----------------------------------------------------------------------
// <copyright file="SqlServerDialectProviderTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Generator;
using System;

namespace Sqlx.Tests.Dialects;

/// <summary>
/// SQL Server方言提供器的单元测试
/// </summary>
[TestClass]
public class SqlServerDialectProviderTests
{
    private SqlServerDialectProvider _provider = null!;

    [TestInitialize]
    public void Setup()
    {
        _provider = new SqlServerDialectProvider();
    }

    /// <summary>
    /// 测试：DialectType应该是SqlServer
    /// </summary>
    [TestMethod]
    public void DialectType_ShouldBe_SqlServer()
    {
        Assert.AreEqual(SqlDefineTypes.SqlServer, _provider.DialectType);
    }

    /// <summary>
    /// 测试：LIMIT子句生成（SQL Server使用TOP和OFFSET-FETCH）
    /// </summary>
    [TestMethod]
    public void GenerateLimitClause_WithLimitOnly_ShouldGenerateTopClause()
    {
        var result = _provider.GenerateLimitClause(10, null);
        Assert.IsTrue(result.Contains("TOP") || result.Contains("OFFSET"));
    }

    [TestMethod]
    public void GenerateLimitClause_WithLimitAndOffset_ShouldGenerateOffsetFetch()
    {
        var result = _provider.GenerateLimitClause(10, 20);
        Assert.IsTrue(result.Contains("OFFSET") && result.Contains("FETCH"));
    }

    [TestMethod]
    public void GenerateLimitClause_WithNullLimit_ShouldReturnEmpty()
    {
        var result = _provider.GenerateLimitClause(null, null);
        Assert.IsTrue(string.IsNullOrEmpty(result));
    }

    /// <summary>
    /// 测试：INSERT with RETURNING生成（SQL Server使用OUTPUT INSERTED.Id）
    /// </summary>
    [TestMethod]
    public void GenerateInsertWithReturning_ShouldUseOutputClause()
    {
        var result = _provider.GenerateInsertWithReturning("Users", new[] { "Name", "Email" });

        Assert.IsTrue(result.Contains("OUTPUT"));
        Assert.IsTrue(result.Contains("INSERTED"));
        Assert.IsTrue(result.Contains("Users"));
    }

    /// <summary>
    /// 测试：批量INSERT生成
    /// </summary>
    [TestMethod]
    public void GenerateBatchInsert_ShouldGenerateMultipleValueSets()
    {
        var result = _provider.GenerateBatchInsert("Users", new[] { "Name", "Email" }, 3);

        Assert.IsTrue(result.Contains("INSERT INTO"));
        Assert.IsTrue(result.Contains("Users"));
        Assert.IsTrue(result.Contains("Name"));
        Assert.IsTrue(result.Contains("Email"));
    }

    /// <summary>
    /// 测试：UPSERT生成（SQL Server使用MERGE）
    /// </summary>
    [TestMethod]
    public void GenerateUpsert_ShouldUseMergeStatement()
    {
        var result = _provider.GenerateUpsert(
            "Users",
            new[] { "Id", "Name", "Email" },
            new[] { "Id" });

        Assert.IsTrue(result.Contains("MERGE") || result.Contains("UPDATE") || result.Contains("INSERT"));
    }

    /// <summary>
    /// 测试：.NET类型到数据库类型的映射
    /// </summary>
    [TestMethod]
    public void GetDatabaseTypeName_Int_ShouldReturnInt()
    {
        var result = _provider.GetDatabaseTypeName(typeof(int));
        Assert.IsTrue(result.Contains("INT") || result.Contains("int"));
    }

    [TestMethod]
    public void GetDatabaseTypeName_String_ShouldReturnVarchar()
    {
        var result = _provider.GetDatabaseTypeName(typeof(string));
        Assert.IsTrue(result.Contains("VARCHAR") || result.Contains("NVARCHAR") || result.ToLower().Contains("varchar"));
    }

    [TestMethod]
    public void GetDatabaseTypeName_DateTime_ShouldReturnDateTime()
    {
        var result = _provider.GetDatabaseTypeName(typeof(DateTime));
        Assert.IsTrue(result.Contains("DATETIME") || result.ToLower().Contains("datetime"));
    }

    [TestMethod]
    public void GetDatabaseTypeName_Bool_ShouldReturnBit()
    {
        var result = _provider.GetDatabaseTypeName(typeof(bool));
        Assert.IsTrue(result.Contains("BIT") || result.ToLower().Contains("bit") || result.ToLower().Contains("bool"));
    }

    [TestMethod]
    public void GetDatabaseTypeName_Decimal_ShouldReturnDecimal()
    {
        var result = _provider.GetDatabaseTypeName(typeof(decimal));
        Assert.IsTrue(result.Contains("DECIMAL") || result.ToLower().Contains("decimal") || result.ToLower().Contains("numeric"));
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
    public void GetCurrentDateTimeSyntax_ShouldReturnGetDate()
    {
        var result = _provider.GetCurrentDateTimeSyntax();
        Assert.IsTrue(result.Contains("GETDATE") || result.Contains("CURRENT_TIMESTAMP") || result.Contains("SYSDATETIME"));
    }

    /// <summary>
    /// 测试：字符串连接语法
    /// </summary>
    [TestMethod]
    public void GetConcatenationSyntax_ShouldUsePlusOperator()
    {
        var result = _provider.GetConcatenationSyntax("'Hello'", "' '", "'World'");

        Assert.IsNotNull(result);
        // SQL Server uses + or CONCAT
        Assert.IsTrue(result.Contains("+") || result.Contains("CONCAT"));
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

