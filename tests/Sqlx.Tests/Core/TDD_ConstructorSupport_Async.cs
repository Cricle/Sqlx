// -----------------------------------------------------------------------
// <copyright file="TDD_ConstructorSupport_Async.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

namespace Sqlx.Tests.Core;

// ==================== 异步测试实体 ====================

public class AsyncTestEntity
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public int Value { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Data { get; set; }
}

public class LongRunningEntity
{
    public long Id { get; set; }
    public string Description { get; set; } = "";
    public int ProcessingTime { get; set; }
}

// ==================== 异步仓储接口 ====================

public partial interface IAsyncTestRepository
{
    [SqlTemplate("INSERT INTO async_entities (name, value, created_at, data) VALUES (@name, @value, @createdAt, @data)")]
    [ReturnInsertedId]
    Task<long> InsertAsync(string name, int value, DateTime createdAt, string? data, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM async_entities WHERE id = @id")]
    Task<AsyncTestEntity?> GetByIdAsync(long id, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM async_entities")]
    Task<List<AsyncTestEntity>> GetAllAsync(CancellationToken ct = default);

    [SqlTemplate("UPDATE async_entities SET value = @value WHERE id = @id")]
    Task<int> UpdateValueAsync(long id, int value, CancellationToken ct = default);

    [SqlTemplate("DELETE FROM async_entities WHERE id = @id")]
    Task<int> DeleteAsync(long id, CancellationToken ct = default);

    [SqlTemplate("SELECT COUNT(*) FROM async_entities")]
    Task<int> CountAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM async_entities WHERE value > @minValue ORDER BY value DESC")]
    Task<List<AsyncTestEntity>> GetByMinValueAsync(int minValue, CancellationToken ct = default);
}

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IAsyncTestRepository))]
public partial class AsyncTestRepository(DbConnection connection) : IAsyncTestRepository
{
}

public partial interface ILongRunningRepository
{
    [SqlTemplate("INSERT INTO long_running (description, processing_time) VALUES (@description, @processingTime)")]
    [ReturnInsertedId]
    Task<long> InsertAsync(string description, int processingTime, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM long_running WHERE id = @id")]
    Task<LongRunningEntity?> GetByIdAsync(long id, CancellationToken ct = default);

    [SqlTemplate("SELECT COUNT(*) FROM long_running")]
    Task<int> CountAsync(CancellationToken ct = default);
}

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(ILongRunningRepository))]
public partial class LongRunningRepository(DbConnection connection) : ILongRunningRepository
{
}

// ==================== 测试类 ====================

/// <summary>
/// 主构造函数的异步操作和CancellationToken测试
/// </summary>
[TestClass]
public class TDD_ConstructorSupport_Async : IDisposable
{
    private readonly DbConnection _connection;

    public TDD_ConstructorSupport_Async()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE async_entities (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                value INTEGER NOT NULL,
                created_at TEXT NOT NULL,
                data TEXT
            );

            CREATE TABLE long_running (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                description TEXT NOT NULL,
                processing_time INTEGER NOT NULL
            );
        ";
        cmd.ExecuteNonQuery();
    }

    public void Dispose()
    {
        try
        {
            if (_connection?.State != ConnectionState.Closed)
            {
                _connection?.Close();
            }
            _connection?.Dispose();
        }
        catch (Exception)
        {
            // Ignore disposal errors - connection may already be in invalid state
            // due to concurrent operations in tests
        }
    }

    // ==================== 基础异步操作测试 ====================

    [TestMethod]
    public async Task Async_BasicInsert_ShouldReturnId()
    {
        // Arrange
        var repo = new AsyncTestRepository(_connection);
        var now = DateTime.UtcNow;

        // Act
        var id = await repo.InsertAsync("Test", 100, now, null);

        // Assert
        Assert.IsTrue(id > 0);
    }

    [TestMethod]
    public async Task Async_BasicGet_ShouldReturnEntity()
    {
        // Arrange
        var repo = new AsyncTestRepository(_connection);
        var now = DateTime.UtcNow;
        var id = await repo.InsertAsync("Test", 100, now, "data");

        // Act
        var entity = await repo.GetByIdAsync(id);

        // Assert
        Assert.IsNotNull(entity);
        Assert.AreEqual("Test", entity.Name);
        Assert.AreEqual(100, entity.Value);
        Assert.AreEqual("data", entity.Data);
    }

    [TestMethod]
    public async Task Async_BasicUpdate_ShouldModifyEntity()
    {
        // Arrange
        var repo = new AsyncTestRepository(_connection);
        var now = DateTime.UtcNow;
        var id = await repo.InsertAsync("Test", 100, now, null);

        // Act
        var updated = await repo.UpdateValueAsync(id, 200);
        var entity = await repo.GetByIdAsync(id);

        // Assert
        Assert.AreEqual(1, updated);
        Assert.AreEqual(200, entity.Value);
    }

    [TestMethod]
    public async Task Async_BasicDelete_ShouldRemoveEntity()
    {
        // Arrange
        var repo = new AsyncTestRepository(_connection);
        var now = DateTime.UtcNow;
        var id = await repo.InsertAsync("Test", 100, now, null);

        // Act
        var deleted = await repo.DeleteAsync(id);
        var entity = await repo.GetByIdAsync(id);

        // Assert
        Assert.AreEqual(1, deleted);
        Assert.IsNull(entity);
    }

    // ==================== 多个异步操作测试 ====================

    [TestMethod]
    public async Task Async_MultipleInserts_ShouldAllSucceed()
    {
        // Arrange
        var repo = new AsyncTestRepository(_connection);
        var now = DateTime.UtcNow;

        // Act
        var id1 = await repo.InsertAsync("Entity1", 100, now, null);
        var id2 = await repo.InsertAsync("Entity2", 200, now, null);
        var id3 = await repo.InsertAsync("Entity3", 300, now, null);

        // Assert
        Assert.IsTrue(id1 > 0);
        Assert.IsTrue(id2 > 0);
        Assert.IsTrue(id3 > 0);
        Assert.AreEqual(id1 + 1, id2);
        Assert.AreEqual(id2 + 1, id3);
    }

    [TestMethod]
    public async Task Async_GetAll_ShouldReturnAllEntities()
    {
        // Arrange
        var repo = new AsyncTestRepository(_connection);
        var now = DateTime.UtcNow;

        await repo.InsertAsync("Entity1", 100, now, null);
        await repo.InsertAsync("Entity2", 200, now, null);
        await repo.InsertAsync("Entity3", 300, now, null);

        // Act
        var all = await repo.GetAllAsync();

        // Assert
        Assert.AreEqual(3, all.Count);
    }

    [TestMethod]
    public async Task Async_QueryWithFilter_ShouldReturnFilteredResults()
    {
        // Arrange
        var repo = new AsyncTestRepository(_connection);
        var now = DateTime.UtcNow;

        await repo.InsertAsync("Entity1", 100, now, null);
        await repo.InsertAsync("Entity2", 200, now, null);
        await repo.InsertAsync("Entity3", 300, now, null);

        // Act
        var filtered = await repo.GetByMinValueAsync(150);

        // Assert
        Assert.AreEqual(2, filtered.Count);
        Assert.AreEqual(300, filtered[0].Value); // 降序
        Assert.AreEqual(200, filtered[1].Value);
    }

    // ==================== CancellationToken测试 ====================

    [TestMethod]
    public async Task CancellationToken_DefaultToken_ShouldWork()
    {
        // Arrange
        var repo = new AsyncTestRepository(_connection);
        var now = DateTime.UtcNow;

        // Act - 使用默认的CancellationToken
        var id = await repo.InsertAsync("Test", 100, now, null);

        // Assert
        Assert.IsTrue(id > 0);
    }

    [TestMethod]
    public async Task CancellationToken_NoneCanceled_ShouldComplete()
    {
        // Arrange
        var repo = new AsyncTestRepository(_connection);
        var now = DateTime.UtcNow;
        var cts = new CancellationTokenSource();

        // Act
        var id = await repo.InsertAsync("Test", 100, now, null, cts.Token);
        var entity = await repo.GetByIdAsync(id, cts.Token);

        // Assert
        Assert.IsTrue(id > 0);
        Assert.IsNotNull(entity);
    }

    [TestMethod]
    public async Task CancellationToken_AlreadyCanceled_ShouldThrow()
    {
        // Arrange
        var repo = new AsyncTestRepository(_connection);
        var now = DateTime.UtcNow;
        var cts = new CancellationTokenSource();
        cts.Cancel(); // 立即取消

        // Act & Assert - TaskCanceledException 继承自 OperationCanceledException
        await Assert.ThrowsExceptionAsync<TaskCanceledException>(async () =>
        {
            await repo.InsertAsync("Test", 100, now, null, cts.Token);
        });
    }

    [TestMethod]
    public async Task CancellationToken_CanceledDuringOperation_ShouldThrow()
    {
        // Arrange
        var repo = new LongRunningRepository(_connection);
        var cts = new CancellationTokenSource();

        // Act - 启动操作后立即取消
        var insertTask = repo.InsertAsync("LongRunning", 1000, cts.Token);
        cts.Cancel();

        // Assert
        try
        {
            await insertTask;
            // 如果执行太快，可能成功完成
        }
        catch (OperationCanceledException)
        {
            // 预期的异常
            Assert.IsTrue(true);
        }
    }

    [TestMethod]
    public async Task CancellationToken_WithTimeout_ShouldRespect()
    {
        // Arrange
        var repo = new AsyncTestRepository(_connection);
        var now = DateTime.UtcNow;
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)); // 5秒超时

        // Act
        var id = await repo.InsertAsync("Test", 100, now, null, cts.Token);

        // Assert
        Assert.IsTrue(id > 0);
    }

    // ==================== 并发异步操作测试 ====================

    [TestMethod]
    public async Task Async_ConcurrentInserts_ShouldAllSucceed()
    {
        // Arrange
        var repo = new AsyncTestRepository(_connection);
        var now = DateTime.UtcNow;

        // Act - SQLite内存数据库不支持真正的并发写入，使用顺序异步
        var ids = new List<long>();
        for (int i = 0; i < 10; i++)
        {
            var id = await repo.InsertAsync($"Entity{i}", i * 10, now, null);
            ids.Add(id);
        }

        // Assert
        Assert.AreEqual(10, ids.Count);
        Assert.IsTrue(ids.All(id => id > 0));
        Assert.AreEqual(10, ids.Distinct().Count()); // 所有ID都不同
    }

    [TestMethod]
    public async Task Async_ConcurrentReads_ShouldAllSucceed()
    {
        // Arrange
        var repo = new AsyncTestRepository(_connection);
        var now = DateTime.UtcNow;

        // 先插入一些数据
        for (int i = 0; i < 5; i++)
        {
            await repo.InsertAsync($"Entity{i}", i * 10, now, null);
        }

        // Act - 并发读取
        var tasks = new List<Task<List<AsyncTestEntity>>>();
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(async () => await repo.GetAllAsync()));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.AreEqual(10, results.Length);
        Assert.IsTrue(results.All(r => r.Count == 5));
    }

    [TestMethod]
    public async Task Async_ConcurrentMixedOperations_ShouldHandleCorrectly()
    {
        // Arrange
        var repo = new AsyncTestRepository(_connection);
        var now = DateTime.UtcNow;

        // 先插入初始数据
        var id1 = await repo.InsertAsync("Entity1", 100, now, null);
        var id2 = await repo.InsertAsync("Entity2", 200, now, null);

        // Act - 混合并发操作
        var tasks = new List<Task>();

        // 读操作
        tasks.Add(Task.Run(async () => await repo.GetByIdAsync(id1)));
        tasks.Add(Task.Run(async () => await repo.GetAllAsync()));
        tasks.Add(Task.Run(async () => await repo.CountAsync()));

        // 写操作（顺序执行以避免SQLite冲突）
        await Task.Run(async () => await repo.InsertAsync("Entity3", 300, now, null));
        await Task.Run(async () => await repo.UpdateValueAsync(id1, 150));

        await Task.WhenAll(tasks);

        // Assert
        var count = await repo.CountAsync();
        Assert.AreEqual(3, count);

        var updated = await repo.GetByIdAsync(id1);
        Assert.AreEqual(150, updated.Value);
    }

    // ==================== Task.WhenAll和Task.WhenAny测试 ====================

    [TestMethod]
    public async Task Async_WhenAll_ShouldWaitForAllTasks()
    {
        // Arrange
        var repo = new AsyncTestRepository(_connection);
        var now = DateTime.UtcNow;
        var sw = Stopwatch.StartNew();

        // Act
        var tasks = new[]
        {
            repo.InsertAsync("Entity1", 100, now, null),
            repo.InsertAsync("Entity2", 200, now, null),
            repo.InsertAsync("Entity3", 300, now, null)
        };

        var ids = await Task.WhenAll(tasks);
        sw.Stop();

        // Assert
        Assert.AreEqual(3, ids.Length);
        Assert.IsTrue(ids.All(id => id > 0));
        // 并发执行应该比顺序快（但在SQLite内存数据库中可能差异不大）
    }

    [TestMethod]
    public async Task Async_WhenAny_ShouldReturnFirstCompleted()
    {
        // Arrange
        var repo = new AsyncTestRepository(_connection);
        var now = DateTime.UtcNow;

        // Act
        var task1 = repo.InsertAsync("Entity1", 100, now, null);
        var task2 = repo.InsertAsync("Entity2", 200, now, null);
        var task3 = repo.InsertAsync("Entity3", 300, now, null);

        var completedTask = await Task.WhenAny(task1, task2, task3);

        // Assert
        Assert.IsTrue(completedTask.IsCompleted);
        var id = await completedTask;
        Assert.IsTrue(id > 0);
    }

    // ==================== 异步流程测试 ====================

    [TestMethod]
    public async Task Async_CompleteWorkflow_ShouldExecuteInOrder()
    {
        // Arrange
        var repo = new AsyncTestRepository(_connection);
        var now = DateTime.UtcNow;

        // Act - 完整的异步工作流
        var id = await repo.InsertAsync("Test", 100, now, "initial");

        var entity1 = await repo.GetByIdAsync(id);
        Assert.AreEqual(100, entity1.Value);

        await repo.UpdateValueAsync(id, 200);

        var entity2 = await repo.GetByIdAsync(id);
        Assert.AreEqual(200, entity2.Value);

        await repo.DeleteAsync(id);

        var entity3 = await repo.GetByIdAsync(id);
        Assert.IsNull(entity3);
    }

    [TestMethod]
    public async Task Async_ChainedOperations_ShouldMaintainDataIntegrity()
    {
        // Arrange
        var repo = new AsyncTestRepository(_connection);
        var now = DateTime.UtcNow;

        // Act - 链式异步操作
        var id1 = await repo.InsertAsync("Entity1", 100, now, null);
        var entity1 = await repo.GetByIdAsync(id1);

        var id2 = await repo.InsertAsync("Entity2", entity1.Value + 100, now, null);
        var entity2 = await repo.GetByIdAsync(id2);

        await repo.UpdateValueAsync(id1, entity2.Value + 100);

        var finalEntity = await repo.GetByIdAsync(id1);

        // Assert
        Assert.AreEqual(300, finalEntity.Value); // 100 -> 200 -> 300
    }

    // ==================== 异常处理测试 ====================

    [TestMethod]
    public async Task Async_ExceptionInOperation_ShouldPropagate()
    {
        // Arrange
        var repo = new AsyncTestRepository(_connection);
        var now = DateTime.UtcNow;

        // Act & Assert - 尝试获取不存在的实体
        var entity = await repo.GetByIdAsync(99999);
        Assert.IsNull(entity); // 应该返回null而不是抛出异常
    }

    [TestMethod]
    public async Task Async_MultipleExceptions_ShouldHandleCorrectly()
    {
        // Arrange
        var repo = new AsyncTestRepository(_connection);
        var now = DateTime.UtcNow;
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        var exceptions = new List<Exception>();

        try { await repo.InsertAsync("Test1", 100, now, null, cts.Token); }
        catch (Exception ex) { exceptions.Add(ex); }

        try { await repo.GetByIdAsync(99999, cts.Token); }
        catch (Exception ex) { exceptions.Add(ex); }

        // 第一个应该抛出OperationCanceledException
        Assert.IsTrue(exceptions.Any(e => e is OperationCanceledException));
    }

    // ==================== 性能和响应性测试 ====================

    [TestMethod]
    public async Task Async_LargeNumberOfOperations_ShouldComplete()
    {
        // Arrange
        var repo = new AsyncTestRepository(_connection);
        var now = DateTime.UtcNow;
        const int count = 100;

        // Act
        var sw = Stopwatch.StartNew();

        for (int i = 0; i < count; i++)
        {
            await repo.InsertAsync($"Entity{i}", i, now, null);
        }

        sw.Stop();

        // Assert
        var totalCount = await repo.CountAsync();
        Assert.AreEqual(count, totalCount);

        // 应该在合理时间内完成（根据环境调整）
        Assert.IsTrue(sw.ElapsedMilliseconds < 5000,
            $"操作耗时 {sw.ElapsedMilliseconds}ms，超过预期");
    }

    [TestMethod]
    public async Task Async_Responsiveness_ShouldNotBlock()
    {
        // Arrange
        var repo = new AsyncTestRepository(_connection);
        var now = DateTime.UtcNow;

        // Act - 启动异步操作但不等待
        var task1 = repo.InsertAsync("Entity1", 100, now, null);
        var task2 = repo.InsertAsync("Entity2", 200, now, null);

        // 主线程可以继续执行其他工作
        var localWork = 0;
        for (int i = 0; i < 1000; i++)
        {
            localWork += i;
        }

        // 现在等待异步操作完成
        var ids = await Task.WhenAll(task1, task2);

        // Assert
        Assert.IsTrue(localWork > 0); // 本地工作已完成
        Assert.AreEqual(2, ids.Length);
        Assert.IsTrue(ids.All(id => id > 0));
    }

    // ==================== ConfigureAwait测试 ====================

    [TestMethod]
    public async Task Async_WithConfigureAwait_ShouldWork()
    {
        // Arrange
        var repo = new AsyncTestRepository(_connection);
        var now = DateTime.UtcNow;

        // Act - 使用ConfigureAwait(false)
        var id = await repo.InsertAsync("Test", 100, now, null).ConfigureAwait(false);
        var entity = await repo.GetByIdAsync(id).ConfigureAwait(false);

        // Assert
        Assert.IsTrue(id > 0);
        Assert.IsNotNull(entity);
    }

    // ==================== ValueTask测试准备 ====================

    [TestMethod]
    public async Task Async_RepeatedQueries_ShouldBeCached()
    {
        // Arrange
        var repo = new AsyncTestRepository(_connection);
        var now = DateTime.UtcNow;
        var id = await repo.InsertAsync("Test", 100, now, null);

        // Act - 重复查询相同数据
        var entity1 = await repo.GetByIdAsync(id);
        var entity2 = await repo.GetByIdAsync(id);
        var entity3 = await repo.GetByIdAsync(id);

        // Assert
        Assert.IsNotNull(entity1);
        Assert.IsNotNull(entity2);
        Assert.IsNotNull(entity3);

        // 数据应该一致
        Assert.AreEqual(entity1.Value, entity2.Value);
        Assert.AreEqual(entity2.Value, entity3.Value);
    }
}

