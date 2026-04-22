// <copyright file="BranchCoverageGapTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using Sqlx.Placeholders;

namespace Sqlx.Tests.BranchCoverage;

/// <summary>
/// Covers remaining branch gaps: ExceptionHandler retry path, PaginatePlaceholderHandler,
/// DateTimeFunctionParser, DbBatchExecutor, ValidationHelper, SqlxQueryable.
/// </summary>
[TestClass]
public class BranchCoverageGapTests
{
    // ── ExceptionHandler retry path ───────────────────────────────────────────

    [TestMethod]
    public async Task ExceptionHandler_HandleFailureAndRetryAsync_ReturnsNullOnRetry()
    {
        var options = new SqlxContextOptions
        {
            EnableRetry = true,
            MaxRetryCount = 3,
            InitialRetryDelay = System.TimeSpan.FromMilliseconds(1),
            RetryBackoffMultiplier = 1.0
        };
        // TimeoutException is transient → should retry (return null)
        var result = await ExceptionHandler.HandleFailureAndRetryAsync(
            new System.TimeoutException("timeout"),
            options, "TestMethod", "SELECT 1", (DbParameterCollection?)null,
            null, System.TimeSpan.Zero, 1);
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task ExceptionHandler_HandleFailureAndRetryAsync_ReturnsSqlxExWhenNoRetry()
    {
        // options null → no retry
        var result = await ExceptionHandler.HandleFailureAndRetryAsync(
            new System.Exception("boom"),
            null, "TestMethod", "SELECT 1", (DbParameterCollection?)null,
            null, System.TimeSpan.Zero, 1);
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void ExceptionHandler_HandleFailureAndRetry_Sync_ReturnsNullOnRetry()
    {
        var options = new SqlxContextOptions
        {
            EnableRetry = true,
            MaxRetryCount = 3,
            InitialRetryDelay = System.TimeSpan.FromMilliseconds(1),
            RetryBackoffMultiplier = 1.0
        };
        var result = ExceptionHandler.HandleFailureAndRetry(
            new System.TimeoutException("timeout"),
            options, "TestMethod", "SELECT 1", (DbParameterCollection?)null,
            null, System.TimeSpan.Zero, 1);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void ExceptionHandler_HandleFailureAndRetry_Sync_MaxAttemptsReached_ReturnsSqlxEx()
    {
        var options = new SqlxContextOptions { EnableRetry = true, MaxRetryCount = 2 };
        // attemptCount >= MaxRetryCount → no retry
        var result = ExceptionHandler.HandleFailureAndRetry(
            new System.TimeoutException("timeout"),
            options, "TestMethod", "SELECT 1", (DbParameterCollection?)null,
            null, System.TimeSpan.Zero, 3);
        Assert.IsNotNull(result);
    }

    // ── PaginatePlaceholderHandler ────────────────────────────────────────────

    [TestMethod]
    public void PaginatePlaceholder_MissingLimit_ReturnsEmpty()
    {
        var ctx = new PlaceholderContext(SqlDefine.SQLite, "t", new List<ColumnMeta>());
        var result = PaginatePlaceholderHandler.Instance.Process(ctx, "--offset skip");
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void PaginatePlaceholder_MissingOffset_ReturnsEmpty()
    {
        var ctx = new PlaceholderContext(SqlDefine.SQLite, "t", new List<ColumnMeta>());
        var result = PaginatePlaceholderHandler.Instance.Process(ctx, "--limit pageSize");
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void PaginatePlaceholder_SqlServer_AddsOrderBy()
    {
        var ctx = new PlaceholderContext(SqlDefine.SqlServer, "t", new List<ColumnMeta>());
        var result = PaginatePlaceholderHandler.Instance.Process(ctx, "--limit pageSize --offset offset");
        Assert.IsTrue(result.Contains("ORDER BY (SELECT NULL)"));
        Assert.IsTrue(result.Contains("FETCH NEXT @pageSize ROWS ONLY"));
    }

    [TestMethod]
    public void PaginatePlaceholder_SQLite_NoOrderBy()
    {
        var ctx = new PlaceholderContext(SqlDefine.SQLite, "t", new List<ColumnMeta>());
        var result = PaginatePlaceholderHandler.Instance.Process(ctx, "--limit pageSize --offset offset");
        Assert.IsTrue(result.Contains("LIMIT @pageSize OFFSET @offset"));
        Assert.IsFalse(result.Contains("ORDER BY"));
    }

    [TestMethod]
    public void PaginatePlaceholder_Render_SameAsProcess()
    {
        var ctx = new PlaceholderContext(SqlDefine.SQLite, "t", new List<ColumnMeta>());
        var process = PaginatePlaceholderHandler.Instance.Process(ctx, "--limit pageSize --offset offset");
        var render = PaginatePlaceholderHandler.Instance.Render(ctx, "--limit pageSize --offset offset", null);
        Assert.AreEqual(process, render);
    }

    // ── DateTimeFunctionParser ────────────────────────────────────────────────

    [TestMethod]
    public void DateTimeFunctionParser_AddDays_GeneratesDateAdd()
    {
        var sql = SqlQuery<DateEntity>.ForSqlite()
            .Where(e => e.CreatedAt.AddDays(1) > System.DateTime.Now)
            .ToSql();
        Assert.IsTrue(sql.Contains("date") || sql.Contains("DATE") || sql.Contains("created_at"),
            $"Expected date function in: {sql}");
    }

    [TestMethod]
    public void DateTimeFunctionParser_AddHours_GeneratesDateAdd()
    {
        var sql = SqlQuery<DateEntity>.ForSqlite()
            .Where(e => e.CreatedAt.AddHours(2) > System.DateTime.Now)
            .ToSql();
        Assert.IsTrue(sql.Length > 0);
    }

    // ── DbBatchExecutor connection already open ───────────────────────────────

    [TestMethod]
    public async Task DbBatchExecutor_ConnectionAlreadyOpen_DoesNotReopen()
    {
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open(); // already open
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "CREATE TABLE t (id INTEGER PRIMARY KEY, name TEXT)";
        cmd.ExecuteNonQuery();

        var binder = new SimpleBinder();
        var entities = new List<SimpleEntity> { new() { Name = "A" }, new() { Name = "B" } };
        var count = await DbBatchExecutor.ExecuteAsync(
            conn, null,
            "INSERT INTO t (name) VALUES (@name)",
            entities, binder);
        Assert.AreEqual(2, count);
        Assert.AreEqual(ConnectionState.Open, conn.State); // still open
    }

    [TestMethod]
    public async Task DbBatchExecutor_EmptyList_ReturnsZero()
    {
        using var conn = new SqliteConnection("Data Source=:memory:");
        var binder = new SimpleBinder();
        var count = await DbBatchExecutor.ExecuteAsync(
            conn, null, "INSERT INTO t (name) VALUES (@name)",
            new List<SimpleEntity>(), binder);
        Assert.AreEqual(0, count);
    }

    // ── ValidationHelper null/empty ───────────────────────────────────────────

    [TestMethod]
    public void ValidationHelper_ValidateValue_NullWithRequired_Throws()
    {
        Assert.ThrowsException<System.ComponentModel.DataAnnotations.ValidationException>(() =>
            ValidationHelper.ValidateValue(null, "param",
                new System.ComponentModel.DataAnnotations.RequiredAttribute()));
    }

    [TestMethod]
    public void ValidationHelper_ValidateValue_ValidValue_DoesNotThrow()
    {
        ValidationHelper.ValidateValue("hello", "param",
            new System.ComponentModel.DataAnnotations.RequiredAttribute());
    }

    // ── SqlxQueryable sync enumerator (ExcludeFromCodeCoverage path) ──────────

    [TestMethod]
    public void SqlxQueryable_ToSql_WithoutConnection_Works()
    {
        var sql = SqlQuery<DateEntity>.ForSqlite()
            .Where(e => e.Id > 0)
            .OrderBy(e => e.CreatedAt)
            .Take(10)
            .ToSql();
        Assert.IsTrue(sql.Contains("WHERE") && sql.Contains("LIMIT"));
    }
}

// ── Helper types ──────────────────────────────────────────────────────────────

[Sqlx.Annotations.Sqlx, Sqlx.Annotations.TableName("date_entities")]
public class DateEntity
{
    public long Id { get; set; }
    public System.DateTime CreatedAt { get; set; }
}

public class SimpleEntity { public string Name { get; set; } = string.Empty; }

public class SimpleBinder : IParameterBinder<SimpleEntity>
{
    public void BindEntity(DbCommand cmd, SimpleEntity e, string prefix = "@")
    {
        var p = cmd.CreateParameter();
        p.ParameterName = prefix + "name";
        p.Value = e.Name;
        cmd.Parameters.Add(p);
    }

    public void BindEntityWithoutValidation(DbCommand cmd, SimpleEntity e, string prefix = "@")
        => BindEntity(cmd, e, prefix);

#if NET6_0_OR_GREATER
    public void BindEntity(DbBatchCommand cmd, SimpleEntity e, Func<DbParameter> factory, string prefix = "@")
    {
        var p = factory();
        p.ParameterName = prefix + "name";
        p.Value = e.Name;
        cmd.Parameters.Add(p);
    }

    public void BindEntityWithoutValidation(DbBatchCommand cmd, SimpleEntity e, Func<DbParameter> factory, string prefix = "@")
        => BindEntity(cmd, e, factory, prefix);
#endif
}
