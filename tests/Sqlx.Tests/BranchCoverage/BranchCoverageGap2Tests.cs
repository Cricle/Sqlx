// <copyright file="BranchCoverageGap2Tests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using Sqlx.Placeholders;

namespace Sqlx.Tests.BranchCoverage;

[TestClass]
public class BranchCoverageGap2Tests
{
    // ── TypeConverter: byte[] branches ───────────────────────────────────────

    [TestMethod]
    public void TypeConverter_ByteArray_FromBase64String()
    {
        var b64 = Convert.ToBase64String(new byte[] { 1, 2, 3 });
        var result = TypeConverter.Convert<byte[]>(b64);
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, result);
    }

    [TestMethod]
    public void TypeConverter_ByteArray_DirectCast()
    {
        var bytes = new byte[] { 4, 5, 6 };
        var result = TypeConverter.Convert<byte[]>(bytes);
        CollectionAssert.AreEqual(bytes, result);
    }

    // ── TypeConverter: DateTimeOffset branches ────────────────────────────────

    [TestMethod]
    public void TypeConverter_DateTimeOffset_FromDateTime()
    {
        var dt = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        var result = TypeConverter.Convert<DateTimeOffset>(dt);
        Assert.AreEqual(dt, result.UtcDateTime);
    }

    [TestMethod]
    public void TypeConverter_DateTimeOffset_DirectCast()
    {
        var dto = new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero);
        var result = TypeConverter.Convert<DateTimeOffset>(dto);
        Assert.AreEqual(dto, result);
    }

    // ── TypeConverter: TimeSpan branches ─────────────────────────────────────

    [TestMethod]
    public void TypeConverter_TimeSpan_FromLong()
    {
        var ticks = TimeSpan.FromHours(2).Ticks;
        var result = TypeConverter.Convert<TimeSpan>(ticks);
        Assert.AreEqual(TimeSpan.FromHours(2), result);
    }

    [TestMethod]
    public void TypeConverter_TimeSpan_DirectCast()
    {
        var ts = TimeSpan.FromMinutes(30);
        var result = TypeConverter.Convert<TimeSpan>(ts);
        Assert.AreEqual(ts, result);
    }

    // ── TypeConverter: DateOnly/TimeOnly ─────────────────────────────────────

    [TestMethod]
    public void TypeConverter_DateOnly_FromDateTime()
    {
        var dt = new DateTime(2024, 6, 15);
        var result = TypeConverter.Convert<DateOnly>(dt);
        Assert.AreEqual(new DateOnly(2024, 6, 15), result);
    }

    [TestMethod]
    public void TypeConverter_DateOnly_FromDateTimeOffset()
    {
        var dto = new DateTimeOffset(2024, 6, 15, 0, 0, 0, TimeSpan.Zero);
        var result = TypeConverter.Convert<DateOnly>(dto);
        Assert.AreEqual(new DateOnly(2024, 6, 15), result);
    }

    [TestMethod]
    public void TypeConverter_TimeOnly_FromTimeSpan()
    {
        var ts = new TimeSpan(14, 30, 0);
        var result = TypeConverter.Convert<TimeOnly>(ts);
        Assert.AreEqual(new TimeOnly(14, 30, 0), result);
    }

    [TestMethod]
    public void TypeConverter_TimeOnly_FromDateTime()
    {
        var dt = new DateTime(2024, 1, 1, 14, 30, 0);
        var result = TypeConverter.Convert<TimeOnly>(dt);
        Assert.AreEqual(new TimeOnly(14, 30, 0), result);
    }

    [TestMethod]
    public void TypeConverter_TimeOnly_FromDateTimeOffset()
    {
        var dto = new DateTimeOffset(2024, 1, 1, 14, 30, 0, TimeSpan.Zero);
        var result = TypeConverter.Convert<TimeOnly>(dto);
        Assert.AreEqual(new TimeOnly(14, 30, 0), result);
    }

    // ── ValidationHelper: empty attributes ───────────────────────────────────

    [TestMethod]
    public void ValidationHelper_ValidateValue_EmptyAttributes_DoesNotThrow()
    {
        // attributes.Length == 0 → early return
        ValidationHelper.ValidateValue("anything", "param");
    }

    [TestMethod]
    public void ValidationHelper_ValidateObject_NullInstance_Throws()
    {
        Assert.ThrowsException<ArgumentNullException>(() =>
            ValidationHelper.ValidateObject(null!, "method"));
    }

    // ── SetPlaceholderHandler: null param ─────────────────────────────────────

    [TestMethod]
    public void SetPlaceholder_RenderDynamicParam_NullValue_ReturnsEmpty()
    {
        var ctx = new PlaceholderContext(SqlDefine.SQLite, "t", new List<ColumnMeta>());
        var handler = SetPlaceholderHandler.Instance;
        // Render with parameters dict containing null value → paramValue is null → returns empty
        var parameters = new Dictionary<string, object?> { ["predicate"] = null };
        var result = handler.Render(ctx, "--param predicate", parameters);
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void SetPlaceholder_RenderDynamicParam_WithValue_ReturnsString()
    {
        var ctx = new PlaceholderContext(SqlDefine.SQLite, "t", new List<ColumnMeta>());
        var handler = SetPlaceholderHandler.Instance;
        var parameters = new Dictionary<string, object?> { ["predicate"] = "name = @name" };
        var result = handler.Render(ctx, "--param predicate", parameters);
        Assert.AreEqual("name = @name", result);
    }

    // ── ExceptionHandler: HandleExceptionAsync ────────────────────────────────

    [TestMethod]
    public async Task ExceptionHandler_HandleExceptionAsync_NoRetry_ReturnsSqlxEx()
    {
        // options null → no retry → returns SqlxException (not null)
        var result = await ExceptionHandler.HandleExceptionAsync(
            new Exception("test"),
            null, "Method", "SELECT 1", (DbParameterCollection?)null, null, TimeSpan.Zero);
        Assert.IsNotNull(result);
        Assert.AreEqual("Method", result.MethodName);
    }

    // ── RepositoryForAttribute / TableNameAttribute null guard ────────────────

    [TestMethod]
    public void RepositoryForAttribute_NullType_Throws()
    {
        Assert.ThrowsException<ArgumentNullException>(() =>
            new Sqlx.Annotations.RepositoryForAttribute(null!));
    }

    [TestMethod]
    public void TableNameAttribute_NullName_Throws()
    {
        Assert.ThrowsException<ArgumentNullException>(() =>
            new Sqlx.Annotations.TableNameAttribute(null!));
    }

    // ── DbBatchExecutor: connection closed path ───────────────────────────────

    [TestMethod]
    public async Task DbBatchExecutor_ClosedConnection_OpensAndCloses()
    {
        // Use file-based SQLite so schema persists across open/close
        var dbPath = System.IO.Path.GetTempFileName() + ".db";
        try
        {
            using var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath}");
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "CREATE TABLE bt (name TEXT)";
            cmd.ExecuteNonQuery();
            conn.Close(); // close so executor must reopen

            var binder = new SimpleBinder();
            var count = await DbBatchExecutor.ExecuteAsync(
                conn, null, "INSERT INTO bt (name) VALUES (@name)",
                new List<SimpleEntity> { new() { Name = "X" } }, binder);
            Assert.AreEqual(1, count);
        }
        finally
        {
            System.IO.File.Delete(dbPath);
        }
    }
}
