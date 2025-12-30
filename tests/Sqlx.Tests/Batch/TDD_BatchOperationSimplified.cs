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
/// TDD: 简化的批量操作边界测试
/// 使用循环单条插入来测试边界条件
/// </summary>
[TestClass]
public class TDD_BatchOperationSimplified
{
    private SqliteConnection? _connection;

    [TestInitialize]
    public void Setup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        
        _connection.Execute(@"
            CREATE TABLE simple_batch_test (
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
    [TestCategory("Simplified")]
    [Description("空集合应正确处理")]
    public async Task SimpleBatch_EmptyCollection_ShouldWork()
    {
        // Arrange
        var repo = new SimpleBatchRepository(_connection!);
        
        // Act & Assert
        var all = await repo.GetAllAsync();
        Assert.AreEqual(0, all.Count);
    }

    [TestMethod]
    [TestCategory("Batch")]
    [TestCategory("Simplified")]
    [Description("单条记录应正确工作")]
    public async Task SimpleBatch_SingleItem_ShouldWork()
    {
        // Arrange
        var repo = new SimpleBatchRepository(_connection!);

        // Act
        var inserted = await repo.InsertAsync("Item1", 100);

        // Assert
        Assert.AreEqual(1, inserted);
        
        var all = await repo.GetAllAsync();
        Assert.AreEqual(1, all.Count);
        Assert.AreEqual("Item1", all[0].Name);
    }

    [TestMethod]
    [TestCategory("Batch")]
    [TestCategory("Simplified")]
    [Description("多条记录循环插入应正确工作")]
    public async Task SimpleBatch_MultipleItems_ShouldWork()
    {
        // Arrange
        var repo = new SimpleBatchRepository(_connection!);
        var items = Enumerable.Range(1, 10)
            .Select(i => new { Name = $"Item{i}", Value = i })
            .ToList();

        // Act
        var totalInserted = 0;
        foreach (var item in items)
        {
            totalInserted += await repo.InsertAsync(item.Name, item.Value);
        }

        // Assert
        Assert.AreEqual(10, totalInserted);
        
        var all = await repo.GetAllAsync();
        Assert.AreEqual(10, all.Count);
    }

    [TestMethod]
    [TestCategory("Batch")]
    [TestCategory("Simplified")]
    [Description("重复数据应正确插入")]
    public async Task SimpleBatch_DuplicateData_ShouldWork()
    {
        // Arrange
        var repo = new SimpleBatchRepository(_connection!);

        // Act
        await repo.InsertAsync("Same", 1);
        await repo.InsertAsync("Same", 1);
        await repo.InsertAsync("Same", 1);

        // Assert
        var all = await repo.GetAllAsync();
        Assert.AreEqual(3, all.Count);
        Assert.IsTrue(all.All(x => x.Name == "Same" && x.Value == 1));
    }

    [TestMethod]
    [TestCategory("Batch")]
    [TestCategory("Simplified")]
    [Description("特殊字符应正确转义")]
    public async Task SimpleBatch_SpecialCharacters_ShouldBeEscaped()
    {
        // Arrange
        var repo = new SimpleBatchRepository(_connection!);

        // Act
        await repo.InsertAsync("O'Reilly", 1);
        await repo.InsertAsync("Test\"Quote", 2);

        // Assert
        var all = await repo.GetAllAsync();
        Assert.AreEqual(2, all.Count);
        Assert.AreEqual("O'Reilly", all[0].Name);
        Assert.AreEqual("Test\"Quote", all[1].Name);
    }

    [TestMethod]
    [TestCategory("Batch")]
    [TestCategory("Simplified")]
    [Description("大量数据应正确处理")]
    public async Task SimpleBatch_LargeDataSet_ShouldWork()
    {
        // Arrange
        var repo = new SimpleBatchRepository(_connection!);
        var count = 100;

        // Act
        var startTime = DateTime.Now;
        for (int i = 1; i <= count; i++)
        {
            await repo.InsertAsync($"Item{i}", i);
        }
        var duration = DateTime.Now - startTime;

        // Assert
        Console.WriteLine($"插入{count}条记录耗时: {duration.TotalMilliseconds}ms");
        
        var all = await repo.GetAllAsync();
        Assert.AreEqual(count, all.Count);
    }

    [TestMethod]
    [TestCategory("Batch")]
    [TestCategory("Simplified")]
    [Description("长字符串应正确处理")]
    public async Task SimpleBatch_LongString_ShouldWork()
    {
        // Arrange
        var repo = new SimpleBatchRepository(_connection!);
        var longString = new string('A', 1000);

        // Act
        await repo.InsertAsync(longString, 1);

        // Assert
        var all = await repo.GetAllAsync();
        Assert.AreEqual(1000, all[0].Name.Length);
    }
}

// 测试模型
public class SimpleBatchModel
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
}

// 测试仓储
public interface ISimpleBatchRepository
{
    [SqlTemplate("INSERT INTO simple_batch_test (name, value) VALUES (@name, @value)")]
    Task<int> InsertAsync(string name, int value);

    [SqlTemplate("SELECT * FROM simple_batch_test")]
    Task<List<SimpleBatchModel>> GetAllAsync();
}

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(ISimpleBatchRepository))]
public partial class SimpleBatchRepository(IDbConnection connection) : ISimpleBatchRepository { }

public static class SimpleBatchExtensions
{
    public static void Execute(this IDbConnection connection, string sql)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }
}

