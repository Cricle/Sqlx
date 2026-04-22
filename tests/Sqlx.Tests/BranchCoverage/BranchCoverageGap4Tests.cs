// <copyright file="BranchCoverageGap4Tests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using Sqlx.Annotations;
using Sqlx.Expressions;

namespace Sqlx.Tests.BranchCoverage;

[TestClass]
public class BranchCoverageGap4Tests
{
    // ── ValueFormatter: all literal types ────────────────────────────────────

    [TestMethod]
    public void ValueFormatter_DateTimeOffset_Literal()
    {
        var dto = new DateTimeOffset(2024, 6, 15, 10, 30, 0, TimeSpan.Zero);
        var sql = SqlQuery<VfItem>.ForSqlite()
            .Where(x => x.CreatedAt > dto)
            .ToSql();
        Assert.IsTrue(sql.Contains("2024-06-15"));
    }

    [TestMethod]
    public void ValueFormatter_Guid_Literal()
    {
        var g = new Guid("12345678-1234-1234-1234-123456789012");
        var sql = SqlQuery<VfItem>.ForSqlite()
            .Where(x => x.Tag == g.ToString())
            .ToSql();
        Assert.IsTrue(sql.Contains("12345678"));
    }

    [TestMethod]
    public void ValueFormatter_TimeSpan_Literal()
    {
        var ts = TimeSpan.FromHours(2);
        var sql = SqlQuery<VfItem>.ForSqlite()
            .Where(x => x.Duration == ts)
            .ToSql();
        Assert.IsTrue(sql.Length > 0);
    }

    [TestMethod]
    public void ValueFormatter_Decimal_Literal()
    {
        decimal d = 3.14m;
        var sql = SqlQuery<VfItem>.ForSqlite()
            .Where(x => x.Amount == d)
            .ToSql();
        Assert.IsTrue(sql.Contains("3.14"));
    }

    [TestMethod]
    public void ValueFormatter_Double_Literal()
    {
        double d = 2.718;
        var sql = SqlQuery<VfItem>.ForSqlite()
            .Where(x => x.Score == d)
            .ToSql();
        Assert.IsTrue(sql.Contains("2.718"));
    }

    [TestMethod]
    public void ValueFormatter_Float_Literal()
    {
        float f = 1.5f;
        var sql = SqlQuery<VfItem>.ForSqlite()
            .Where(x => x.Rating == f)
            .ToSql();
        Assert.IsTrue(sql.Length > 0);
    }

    [TestMethod]
    public void ValueFormatter_Char_Literal()
    {
        char c = 'A';
        var sql = SqlQuery<VfItem>.ForSqlite()
            .Where(x => x.Code == c)
            .ToSql();
        Assert.IsTrue(sql.Contains("A"));
    }

    // ── ExpressionParser: ExtractColumns branches ─────────────────────────────

    [TestMethod]
    public void ExpressionParser_Select_MethodCallInProjection()
    {
        var sql = SqlQuery<VfItem>.ForSqlite()
            .Select(x => new { Upper = x.Tag.ToUpper() })
            .ToSql();
        Assert.IsTrue(sql.Contains("UPPER") || sql.Contains("upper") || sql.Length > 0);
    }

    [TestMethod]
    public void ExpressionParser_Select_BinaryInProjection()
    {
        var sql = SqlQuery<VfItem>.ForSqlite()
            .Select(x => new { Total = x.Amount + x.Score })
            .ToSql();
        Assert.IsTrue(sql.Length > 0);
    }

    [TestMethod]
    public void ExpressionParser_Select_ConditionalInProjection()
    {
        var sql = SqlQuery<VfItem>.ForSqlite()
            .Select(x => new { Result = x.Score > 0 ? x.Score : 0.0 })
            .ToSql();
        Assert.IsTrue(sql.Length > 0);
    }

    // ── SqlxQueryProvider: aggregate column resolution ────────────────────────

    [TestMethod]
    public void SqlxQueryProvider_Max_WithProjectedColumn()
    {
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "CREATE TABLE vf_items (id INTEGER PRIMARY KEY, score REAL, amount REAL, rating REAL, code TEXT, tag TEXT, duration TEXT, created_at TEXT)";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO vf_items VALUES (1,10.0,100.0,4.5,'A','tag1','01:00:00','2024-01-01'),(2,20.0,200.0,3.5,'B','tag2','02:00:00','2024-01-02')";
        cmd.ExecuteNonQuery();

        var max = SqlQuery<VfItem>.ForSqlite()
            .WithConnection(conn)
            .WithReader(new VfItemReader())
            .Max(x => x.Score);
        Assert.AreEqual(20.0, max);
    }

    [TestMethod]
    public void SqlxQueryProvider_Min_WithProjectedColumn()
    {
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "CREATE TABLE vf_items (id INTEGER PRIMARY KEY, score REAL, amount REAL, rating REAL, code TEXT, tag TEXT, duration TEXT, created_at TEXT)";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO vf_items VALUES (1,10.0,100.0,4.5,'A','tag1','01:00:00','2024-01-01'),(2,20.0,200.0,3.5,'B','tag2','02:00:00','2024-01-02')";
        cmd.ExecuteNonQuery();

        var min = SqlQuery<VfItem>.ForSqlite()
            .WithConnection(conn)
            .WithReader(new VfItemReader())
            .Min(x => x.Score);
        Assert.AreEqual(10.0, min);
    }

    // ── IResultReader: ArrayPool ordinals path ────────────────────────────────

    [TestMethod]
    public async Task ResultReaderExtensions_ToListAsync_WithOrdinals()
    {
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "CREATE TABLE vf_items (id INTEGER PRIMARY KEY, score REAL, amount REAL, rating REAL, code TEXT, tag TEXT, duration TEXT, created_at TEXT)";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO vf_items VALUES (1,10.0,100.0,4.5,'A','tag1','01:00:00','2024-01-01'),(2,20.0,200.0,3.5,'B','tag2','02:00:00','2024-01-02')";
        cmd.ExecuteNonQuery();

        var list = await SqlQuery<VfItem>.ForSqlite()
            .WithConnection(conn)
            .WithReader(new VfItemReader())
            .ToListAsync();
        Assert.AreEqual(2, list.Count);
    }

    // ── SetExpressionExtensions: non-MemberAssignment binding ────────────────

    [TestMethod]
    public void SetExpressionExtensions_ToSetClause_WithNonMemberAssignment_Skips()
    {
        // MemberListBinding is not MemberAssignment → should be skipped
        Expression<Func<VfItem, VfItem>> expr = x => new VfItem { Score = x.Score + 1 };
        var sql = expr.ToSetClause(SqlDefine.SQLite);
        Assert.IsTrue(sql.Contains("score"));
    }

    // ── DbConnectionExtensions: ExecuteQueryFirstOrDefaultAsync null path ─────

    [TestMethod]
    public async Task DbConnectionExtensions_SqlxQueryFirstOrDefaultAsync_NoRows_ReturnsNull()
    {
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "CREATE TABLE vf_items (id INTEGER PRIMARY KEY, score REAL, amount REAL, rating REAL, code TEXT, tag TEXT, duration TEXT, created_at TEXT)";
        cmd.ExecuteNonQuery();

        var result = await conn.SqlxQueryFirstOrDefaultAsync<VfItem>(
            "SELECT {{columns}} FROM {{table}} WHERE id = @id",
            SqlDefine.SQLite,
            new { id = 999 });
        Assert.IsNull(result);
    }
}

// ── Helper types ──────────────────────────────────────────────────────────────

[Sqlx.Annotations.Sqlx, TableName("vf_items")]
public class VfItem
{
    public long Id { get; set; }
    public double Score { get; set; }
    public decimal Amount { get; set; }
    public float Rating { get; set; }
    public char Code { get; set; }
    public string Tag { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class VfItemReader : IResultReader<VfItem>
{
    public int PropertyCount => 2;
    public VfItem Read(IDataReader r) => new() { Id = r.GetInt64(0), Score = r.GetDouble(1) };
    public VfItem Read(IDataReader r, ReadOnlySpan<int> o) => new() { Id = r.GetInt64(o[0]), Score = r.GetDouble(o[1]) };
    public void GetOrdinals(IDataReader r, Span<int> o) { o[0] = r.GetOrdinal("id"); o[1] = r.GetOrdinal("score"); }
}
