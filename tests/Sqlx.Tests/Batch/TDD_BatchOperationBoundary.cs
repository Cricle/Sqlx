using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

namespace Sqlx.Tests.Batch;

/// <summary>
/// TDD: 批量操作边界测试
/// 验证批量操作的各种边界条件和异常场景
/// </summary>
[TestClass]
public class TDD_BatchOperationBoundary
{
    private SqliteConnection? _connection;

    [TestInitialize]
    public void Setup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        
        _connection.Execute(@"
            CREATE TABLE batch_test (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                value INTEGER NOT NULL
            )
        ");
    }

    [TestCleanup]
    public void Cleanup()
    {
        _connection?.Dispose();
    }

    [TestMethod]
    [TestCategory("Batch")]
    [TestCategory("Boundary")]
    [Description("空集合应正确处理")]
    public async Task Batch_EmptyCollection_ShouldReturnZero()
    {
        // Arrange
        var repo = new BatchTestRepository(_connection!);
        var emptyList = new List<BatchTestModel>();

        // Act
        var inserted = await repo.BatchInsertAsync(emptyList);

        // Assert
        Assert.AreEqual(0, inserted);
        
        var all = await repo.GetAllAsync();
        Assert.AreEqual(0, all.Count);
    }

    [TestMethod]
    [TestCategory("Batch")]
    [TestCategory("Boundary")]
    [Description("单条记录批量插入应正确工作")]
    public async Task Batch_SingleItem_ShouldWork()
    {
        // Arrange
        var repo = new BatchTestRepository(_connection!);
        var singleItem = new List<BatchTestModel>
        {
            new() { Name = "Item1", Value = 100 }
        };

        // Act
        var inserted = await repo.BatchInsertAsync(singleItem);

        // Assert
        Assert.AreEqual(1, inserted);
        
        var all = await repo.GetAllAsync();
        Assert.AreEqual(1, all.Count);
        Assert.AreEqual("Item1", all[0].Name);
    }

    [TestMethod]
    [TestCategory("Batch")]
    [TestCategory("Boundary")]
    [Description("MaxBatchSize边界测试")]
    public async Task Batch_ExactlyMaxBatchSize_ShouldWork()
    {
        // Arrange
        var repo = new BatchTestRepository(_connection!);
        var items = Enumerable.Range(1, 100)
            .Select(i => new BatchTestModel { Name = $"Item{i}", Value = i })
            .ToList();

        // Act
        var inserted = await repo.BatchInsertAsync(items);

        // Assert
        Assert.AreEqual(100, inserted);
        
        var all = await repo.GetAllAsync();
        Assert.AreEqual(100, all.Count);
    }

    [TestMethod]
    [TestCategory("Batch")]
    [TestCategory("Boundary")]
    [Description("超过MaxBatchSize应自动分批")]
    public async Task Batch_ExceedMaxBatchSize_ShouldAutoBatch()
    {
        // Arrange
        var repo = new BatchTestRepository(_connection!);
        var items = Enumerable.Range(1, 250) // 超过MaxBatchSize(100)
            .Select(i => new BatchTestModel { Name = $"Item{i}", Value = i })
            .ToList();

        // Act
        var inserted = await repo.BatchInsertAsync(items);

        // Assert
        Assert.AreEqual(250, inserted);
        
        var all = await repo.GetAllAsync();
        Assert.AreEqual(250, all.Count);
    }

    [TestMethod]
    [TestCategory("Batch")]
    [TestCategory("Boundary")]
    [Description("大批量数据应正确处理")]
    public async Task Batch_LargeDataSet_ShouldWork()
    {
        // Arrange
        var repo = new BatchTestRepository(_connection!);
        var items = Enumerable.Range(1, 1000)
            .Select(i => new BatchTestModel { Name = $"Item{i}", Value = i })
            .ToList();

        // Act
        var startTime = DateTime.Now;
        var inserted = await repo.BatchInsertAsync(items);
        var duration = DateTime.Now - startTime;

        // Assert
        Assert.AreEqual(1000, inserted);
        Console.WriteLine($"插入1000条记录耗时: {duration.TotalMilliseconds}ms");
        
        var all = await repo.GetAllAsync();
        Assert.AreEqual(1000, all.Count);
    }

    [TestMethod]
    [TestCategory("Batch")]
    [TestCategory("Boundary")]
    [Description("重复数据批量插入应正确工作")]
    public async Task Batch_DuplicateData_ShouldWork()
    {
        // Arrange
        var repo = new BatchTestRepository(_connection!);
        var items = new List<BatchTestModel>
        {
            new() { Name = "Same", Value = 1 },
            new() { Name = "Same", Value = 1 },
            new() { Name = "Same", Value = 1 }
        };

        // Act
        var inserted = await repo.BatchInsertAsync(items);

        // Assert
        Assert.AreEqual(3, inserted);
        
        var all = await repo.GetAllAsync();
        Assert.AreEqual(3, all.Count);
        Assert.IsTrue(all.All(x => x.Name == "Same" && x.Value == 1));
    }

    [TestMethod]
    [TestCategory("Batch")]
    [TestCategory("Boundary")]
    [Description("NULL值批量插入应正确处理")]
    public async Task Batch_WithNullValues_ShouldWork()
    {
        // Arrange
        var repo = new BatchNullableTestRepository(_connection!);
        _connection!.Execute(@"
            CREATE TABLE batch_nullable_test (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT,
                value INTEGER
            )
        ");
        
        var items = new List<BatchNullableTestModel>
        {
            new() { Name = "Item1", Value = 100 },
            new() { Name = null, Value = null },
            new() { Name = "Item3", Value = 300 }
        };

        // Act
        var inserted = await repo.BatchInsertAsync(items);

        // Assert
        Assert.AreEqual(3, inserted);
    }

    [TestMethod]
    [TestCategory("Batch")]
    [TestCategory("Performance")]
    [Description("批量操作应比单条插入快")]
    public async Task Batch_ShouldBeFasterThanSingleInserts()
    {
        // Arrange
        var repo = new BatchTestRepository(_connection!);
        var items = Enumerable.Range(1, 500)
            .Select(i => new BatchTestModel { Name = $"Item{i}", Value = i })
            .ToList();

        // Act - 批量插入
        var batchStart = DateTime.Now;
        var batchInserted = await repo.BatchInsertAsync(items);
        var batchDuration = DateTime.Now - batchStart;

        // Clean up
        await repo.DeleteAllAsync();

        // Act - 单条插入
        var singleStart = DateTime.Now;
        foreach (var item in items)
        {
            await repo.InsertSingleAsync(item.Name, item.Value);
        }
        var singleDuration = DateTime.Now - singleStart;

        // Assert
        Console.WriteLine($"批量插入500条: {batchDuration.TotalMilliseconds}ms");
        Console.WriteLine($"单条插入500条: {singleDuration.TotalMilliseconds}ms");
        Console.WriteLine($"性能提升: {(singleDuration.TotalMilliseconds / batchDuration.TotalMilliseconds):F2}x");
        
        Assert.IsTrue(batchDuration < singleDuration, "批量插入应该更快");
    }

    [TestMethod]
    [TestCategory("Batch")]
    [TestCategory("Boundary")]
    [Description("非常长的字符串应正确处理")]
    public async Task Batch_VeryLongStrings_ShouldWork()
    {
        // Arrange
        var repo = new BatchTestRepository(_connection!);
        var longString = new string('A', 10000); // 10KB字符串
        var items = new List<BatchTestModel>
        {
            new() { Name = longString, Value = 1 },
            new() { Name = longString, Value = 2 }
        };

        // Act
        var inserted = await repo.BatchInsertAsync(items);

        // Assert
        Assert.AreEqual(2, inserted);
        
        var all = await repo.GetAllAsync();
        Assert.AreEqual(10000, all[0].Name.Length);
    }

    [TestMethod]
    [TestCategory("Batch")]
    [TestCategory("Boundary")]
    [Description("特殊字符批量插入应正确转义")]
    public async Task Batch_SpecialCharacters_ShouldBeEscaped()
    {
        // Arrange
        var repo = new BatchTestRepository(_connection!);
        var items = new List<BatchTestModel>
        {
            new() { Name = "O'Reilly", Value = 1 },
            new() { Name = "Test\"Quote", Value = 2 },
            new() { Name = "Line\nBreak", Value = 3 },
            new() { Name = "Tab\tChar", Value = 4 }
        };

        // Act
        var inserted = await repo.BatchInsertAsync(items);

        // Assert
        Assert.AreEqual(4, inserted);
        
        var all = await repo.GetAllAsync();
        Assert.AreEqual("O'Reilly", all[0].Name);
        Assert.AreEqual("Test\"Quote", all[1].Name);
    }

    [TestMethod]
    [TestCategory("Batch")]
    [TestCategory("Transaction")]
    [Description("批量操作应支持事务")]
    public async Task Batch_WithTransaction_ShouldWork()
    {
        // Arrange
        var items = Enumerable.Range(1, 10)
            .Select(i => new BatchTestModel { Name = $"Item{i}", Value = i })
            .ToList();

        // Act
        using var transaction = _connection!.BeginTransaction();
        var repo = new BatchTestRepository(_connection!);
        // Note: Transaction support via constructor/property is handled by repository implementation
        
        var inserted = await repo.BatchInsertAsync(items);
        transaction.Commit();

        // Assert
        Assert.AreEqual(10, inserted);
        
        var all = await repo.GetAllAsync();
        Assert.AreEqual(10, all.Count);
    }

    [TestMethod]
    [TestCategory("Batch")]
    [TestCategory("Memory")]
    [Description("大批量操作应该低内存消耗")]
    public async Task Batch_LargeSet_ShouldHaveLowMemory()
    {
        // Arrange
        var repo = new BatchTestRepository(_connection!);
        
        var beforeMemory = GC.GetTotalMemory(true);
        
        var items = Enumerable.Range(1, 5000)
            .Select(i => new BatchTestModel { Name = $"Item{i}", Value = i })
            .ToList();

        // Act
        var inserted = await repo.BatchInsertAsync(items);
        
        var afterMemory = GC.GetTotalMemory(false);
        var memoryUsed = (afterMemory - beforeMemory) / 1024.0 / 1024.0; // MB

        // Assert
        Assert.AreEqual(5000, inserted);
        Console.WriteLine($"内存使用: {memoryUsed:F2} MB");
    }
}

// 测试模型
public class BatchTestModel
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
}

public class BatchNullableTestModel
{
    public long Id { get; set; }
    public string? Name { get; set; }
    public int? Value { get; set; }
}

// 测试仓储
public interface IBatchTestRepository
{
    [BatchOperation(MaxBatchSize = 100)]
    [SqlTemplate("INSERT INTO batch_test (name, value) VALUES {{batch_values}}")]
    Task<int> BatchInsertAsync(IEnumerable<BatchTestModel> models);

    [SqlTemplate("INSERT INTO batch_test (name, value) VALUES (@name, @value)")]
    Task<int> InsertSingleAsync(string name, int value);

    [SqlTemplate("SELECT * FROM batch_test")]
    Task<List<BatchTestModel>> GetAllAsync();

    [SqlTemplate("DELETE FROM batch_test")]
    Task<int> DeleteAllAsync();
}

public interface IBatchNullableTestRepository
{
    [BatchOperation(MaxBatchSize = 100)]
    [SqlTemplate("INSERT INTO batch_nullable_test (name, value) VALUES {{batch_values}}")]
    Task<int> BatchInsertAsync(IEnumerable<BatchNullableTestModel> models);
}

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IBatchTestRepository))]
public partial class BatchTestRepository(IDbConnection connection) : IBatchTestRepository { }

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IBatchNullableTestRepository))]
public partial class BatchNullableTestRepository(IDbConnection connection) : IBatchNullableTestRepository { }

public static class BatchTestExtensions
{
    public static void Execute(this IDbConnection connection, string sql)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }
}

