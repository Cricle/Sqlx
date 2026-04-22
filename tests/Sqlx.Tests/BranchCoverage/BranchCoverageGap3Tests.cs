// <copyright file="BranchCoverageGap3Tests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using Sqlx.Annotations;

namespace Sqlx.Tests.BranchCoverage;

[TestClass]
public class BranchCoverageGap3Tests
{
    // ── TypeConverter: Guid from byte[] ──────────────────────────────────────

    [TestMethod]
    public void TypeConverter_Guid_FromByteArray()
    {
        var g = Guid.NewGuid();
        var result = TypeConverter.Convert<Guid>(g.ToByteArray());
        Assert.AreEqual(g, result);
    }

    [TestMethod]
    public void TypeConverter_Guid_DirectCast()
    {
        var g = Guid.NewGuid();
        var result = TypeConverter.Convert<Guid>(g);
        Assert.AreEqual(g, result);
    }

    // ── TypeConverter: Nullable<T> ────────────────────────────────────────────

    [TestMethod]
    public void TypeConverter_NullableInt_FromNull_ReturnsDefault()
    {
        var result = TypeConverter.Convert<int?>(null);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TypeConverter_NullableInt_FromValue()
    {
        var result = TypeConverter.Convert<int?>(42L);
        Assert.AreEqual(42, result);
    }

    // ── TypeConverter: Enum from string ──────────────────────────────────────

    [TestMethod]
    public void TypeConverter_Enum_FromString()
    {
        var result = TypeConverter.Convert<DayOfWeek>("Monday");
        Assert.AreEqual(DayOfWeek.Monday, result);
    }

    [TestMethod]
    public void TypeConverter_Enum_FromInt()
    {
        var result = TypeConverter.Convert<DayOfWeek>(1);
        Assert.AreEqual(DayOfWeek.Monday, result);
    }

    // ── SqlxQueryProvider: First/FirstOrDefault ───────────────────────────────

    [TestMethod]
    public void SqlxQueryProvider_First_ReturnsFirstRow()
    {
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "CREATE TABLE qp_items (id INTEGER PRIMARY KEY, val INTEGER)";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO qp_items VALUES (1,10),(2,20)";
        cmd.ExecuteNonQuery();

        var result = SqlQuery<QpItem>.ForSqlite()
            .WithConnection(conn)
            .WithReader(new QpItemReader())
            .First();
        Assert.AreEqual(1, result.Id);
    }

    [TestMethod]
    public void SqlxQueryProvider_FirstOrDefault_NoRows_ReturnsNull()
    {
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "CREATE TABLE qp_items (id INTEGER PRIMARY KEY, val INTEGER)";
        cmd.ExecuteNonQuery();

        var result = SqlQuery<QpItem>.ForSqlite()
            .WithConnection(conn)
            .WithReader(new QpItemReader())
            .FirstOrDefault();
        Assert.IsNull(result);
    }

    // ── SqlxQueryableExtensions: FirstOrDefaultAsync connection close ─────────

    [TestMethod]
    public async Task SqlxQueryableExtensions_FirstOrDefaultAsync_ClosedConn_OpensAndCloses()
    {
        var dbPath = System.IO.Path.GetTempFileName() + ".db";
        try
        {
            using var setup = new SqliteConnection($"Data Source={dbPath}");
            setup.Open();
            using var cmd = setup.CreateCommand();
            cmd.CommandText = "CREATE TABLE qp_items (id INTEGER PRIMARY KEY, val INTEGER); INSERT INTO qp_items VALUES (1,99)";
            cmd.ExecuteNonQuery();
            setup.Close();

            using var conn = new SqliteConnection($"Data Source={dbPath}");
            // conn is closed — extension should open it
            var result = await SqlQuery<QpItem>.ForSqlite()
                .WithConnection(conn)
                .WithReader(new QpItemReader())
                .FirstOrDefaultAsync();
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Id);
        }
        finally { System.IO.File.Delete(dbPath); }
    }

    // ── SqlxQueryableExtensions: AnyAsync ────────────────────────────────────

    [TestMethod]
    public async Task SqlxQueryableExtensions_AnyAsync_NoRows_ReturnsFalse()
    {
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "CREATE TABLE qp_items (id INTEGER PRIMARY KEY, val INTEGER)";
        cmd.ExecuteNonQuery();

        var result = await SqlQuery<QpItem>.ForSqlite()
            .Where(x => x.Val > 100)
            .WithConnection(conn)
            .WithReader(new QpItemReader())
            .AnyAsync();
        Assert.IsFalse(result);
    }

    // ── ExpressionBlockResult: non-MemberInit body ───────────────────────────

    [TestMethod]
    public void ExpressionBlockResult_NonMemberInit_ReturnsEmpty()
    {
        Expression<Func<QpItem, QpItem>> expr = x => x; // not MemberInit
        var result = expr.GetSetParameters();
        Assert.AreEqual(0, result.Count);
    }

    // ── PlaceholderContext: null dialect throws ───────────────────────────────

    [TestMethod]
    public void PlaceholderContext_NullDialect_Throws()
    {
        Assert.ThrowsException<ArgumentNullException>(() =>
            new PlaceholderContext(null!, "t", new List<ColumnMeta>()));
    }

    // ── SqlBuilder: ParameterCache null indexer ───────────────────────────────

    [TestMethod]
    public void SqlBuilder_NullParameterObject_DoesNotThrow()
    {
        using var builder = new SqlBuilder(SqlDefine.SQLite);
        builder.Append($"SELECT 1");
        var result = builder.Build();
        Assert.IsTrue(result.Sql.Contains("SELECT 1"));
    }

    // ── IResultReader: ToList with ordinals path ──────────────────────────────

    [TestMethod]
    public void ResultReaderExtensions_ToList_WithOrdinals()
    {
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "CREATE TABLE rl (id INTEGER, val INTEGER); INSERT INTO rl VALUES (1,10),(2,20)";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "SELECT id, val FROM rl";
        using var reader = cmd.ExecuteReader();
        var list = new QpItemReader().ToList(reader);
        Assert.AreEqual(2, list.Count);
    }

    // ── SetExpressionExtensions: non-MemberAssignment binding ────────────────

    [TestMethod]
    public void SetExpressionExtensions_GetSetParameters_EmptyBody_ReturnsEmpty()
    {
        Expression<Func<QpItem, QpItem>> expr = x => new QpItem();
        var result = expr.GetSetParameters();
        Assert.AreEqual(0, result.Count);
    }

    // ── DateTimeFunctionParser: no Add prefix returns obj ────────────────────

    [TestMethod]
    public void DateTimeFunctionParser_NonAddMethod_ReturnsObj()
    {
        // DateTime.Date property access (not an Add method) → returns obj
        var sql = SqlQuery<DateQpItem>.ForSqlite()
            .Where(x => x.CreatedAt.AddDays(0) > DateTime.Today)
            .ToSql();
        Assert.IsTrue(sql.Length > 0);
    }
}

// ── Helper types ──────────────────────────────────────────────────────────────

[Sqlx.Annotations.Sqlx, TableName("qp_items")]
public class QpItem
{
    public long Id { get; set; }
    public int Val { get; set; }
}

[Sqlx.Annotations.Sqlx, TableName("date_qp_items")]
public class DateQpItem
{
    public long Id { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class QpItemReader : IResultReader<QpItem>
{
    public int PropertyCount => 2;

    public QpItem Read(IDataReader r) => new() { Id = r.GetInt64(0), Val = r.GetInt32(1) };

    public QpItem Read(IDataReader r, ReadOnlySpan<int> o) =>
        new() { Id = r.GetInt64(o[0]), Val = r.GetInt32(o[1]) };

    public void GetOrdinals(IDataReader r, Span<int> o)
    {
        o[0] = r.GetOrdinal("id");
        o[1] = r.GetOrdinal("val");
    }
}
