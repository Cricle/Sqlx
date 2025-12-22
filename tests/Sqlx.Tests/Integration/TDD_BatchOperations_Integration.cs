// -----------------------------------------------------------------------
// <copyright file="TDD_BatchOperations_Integration.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Sqlx;
using Sqlx.Annotations;
using FullFeatureDemo;

namespace Sqlx.Tests.Integration;

/// <summary>
/// 批量操作占位符集成测试
/// 测试: {{batch_values}}, {{group_concat}}, {{groupby}}
/// </summary>
[TestClass]
public class TDD_BatchOperations_Integration
{
    private DatabaseFixture _fixture = null!;

    [TestInitialize]
    public void Initialize()
    {
        _fixture = new DatabaseFixture();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _fixture?.Dispose();
    }

    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("BatchOperations")]
    public async Task BatchOperations_BatchInsert_SQLite()
    {
        // Arrange
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        var logRepo = new LogRepository(connection);
        var logs = IntegrationTestHelpers.GenerateTestLogs(1000);

        // Act - 使用 {{batch_values}} 占位符
        var stopwatch = Stopwatch.StartNew();
        var inserted = await logRepo.BatchInsertAsync(logs);
        stopwatch.Stop();

        // Assert
        Assert.AreEqual(1000, inserted, "应该插入1000条记录");
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 5000, $"批量插入应该在5秒内完成，实际: {stopwatch.ElapsedMilliseconds}ms");

        // 验证数据
        var count = await logRepo.CountByLevelAsync("INFO");
        Assert.IsTrue(count > 0, "应该有INFO级别的日志");
    }

    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("BatchOperations")]
    public async Task BatchOperations_GroupConcat_SQLite()
    {
        // Arrange
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        var logRepo = new LogRepository(connection);
        var logs = IntegrationTestHelpers.GenerateTestLogs(100);
        await logRepo.BatchInsertAsync(logs);

        // Act - 使用 {{group_concat message, ', '}} 占位符
        var summary = await logRepo.GetLogSummaryAsync(3);

        // Assert
        Assert.AreEqual(3, summary.Count, "应该有3个级别的日志摘要");
        
        foreach (var item in summary)
        {
            var level = item["level"]?.ToString();
            var messages = item["messages"]?.ToString();
            
            Assert.IsNotNull(level, "级别不应该为空");
            Assert.IsNotNull(messages, "消息不应该为空");
            Assert.IsTrue(messages.Contains("日志消息"), "消息应该包含'日志消息'");
        }
    }

    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("BatchOperations")]
    public async Task BatchOperations_GroupBy_SQLite()
    {
        // Arrange
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        var logRepo = new LogRepository(connection);
        var logs = IntegrationTestHelpers.GenerateTestLogs(90); // 30 INFO, 30 WARN, 30 ERROR
        await logRepo.BatchInsertAsync(logs);

        // Act - 使用 {{groupby level}} 占位符
        var summary = await logRepo.GetLogSummaryAsync(5);

        // Assert
        Assert.AreEqual(3, summary.Count, "应该有3个不同的级别");
        
        var levels = summary.Select(s => s["level"]?.ToString()).ToList();
        Assert.IsTrue(levels.Contains("INFO"));
        Assert.IsTrue(levels.Contains("WARN"));
        Assert.IsTrue(levels.Contains("ERROR"));
    }

    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("BatchOperations")]
    public async Task BatchOperations_BatchDelete_SQLite()
    {
        // Arrange
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        var logRepo = new LogRepository(connection);
        var logs = IntegrationTestHelpers.GenerateTestLogs(100);
        await logRepo.BatchInsertAsync(logs);

        // Act - 批量删除旧日志
        var before = DateTime.Now.AddSeconds(-50);
        var deleted = await logRepo.DeleteOldLogsAsync(before);

        // Assert
        Assert.IsTrue(deleted > 0, "应该删除一些旧日志");
        
        // 验证剩余日志
        var recent = await logRepo.GetRecentAsync(100);
        Assert.IsTrue(recent.Count < 100, "应该有一些日志被删除");
    }

    [TestMethod]
    [TestCategory("Integration")]
    [TestCategory("BatchOperations")]
    [TestCategory("Performance")]
    public async Task BatchOperations_Performance_SQLite()
    {
        // Arrange
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        var logRepo = new LogRepository(connection);
        var logs = IntegrationTestHelpers.GenerateTestLogs(1000);

        // Act
        var stopwatch = Stopwatch.StartNew();
        await logRepo.BatchInsertAsync(logs);
        stopwatch.Stop();

        // Assert
        var avgTime = stopwatch.ElapsedMilliseconds / 1000.0;
        Assert.IsTrue(avgTime < 1.0, $"平均每条记录应该小于1ms，实际: {avgTime:F4}ms");
        
        Console.WriteLine($"批量插入1000条记录耗时: {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"平均每条: {avgTime:F4}ms");
    }
}
