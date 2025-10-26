using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

namespace Sqlx.Tests.Runtime;

/// <summary>
/// 批量操作运行时测试
/// 目的：确保批量操作在实际数据库中正确执行
/// </summary>
[TestClass]
[TestCategory("Runtime")]
[TestCategory("BatchOperations")]
public class TDD_BatchOperations_Runtime
{
    private IDbConnection _connection = null!;
    private IBatchTestRepository _repo = null!;

    [TestInitialize]
    public void Setup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        // 创建测试表
        ExecuteSql("CREATE TABLE users (id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT NOT NULL, age INTEGER NOT NULL)");
        ExecuteSql("CREATE TABLE logs (id INTEGER PRIMARY KEY AUTOINCREMENT, level TEXT, message TEXT, created_at TEXT)");

        _repo = new BatchTestRepository(_connection);
    }

    [TestCleanup]
    public void TearDown()
    {
        _connection?.Dispose();
    }

    private void ExecuteSql(string sql)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    #region 基础批量插入测试

    [TestMethod]
    public async Task BatchInsert_SmallBatch_ShouldInsertAll()
    {
        // Arrange
        var users = new[]
        {
            new UserBatchItem { Name = "Alice", Age = 25 },
            new UserBatchItem { Name = "Bob", Age = 30 },
            new UserBatchItem { Name = "Charlie", Age = 35 }
        };

        // Act
        var affected = await _repo.BatchInsertUsersAsync(users);

        // Assert
        Assert.AreEqual(3, affected, "应该插入3条记录");
        
        var count = GetCount("users");
        Assert.AreEqual(3, count, "数据库应该有3条记录");
    }

    [TestMethod]
    public async Task BatchInsert_LargeBatch_ShouldInsertAll()
    {
        // Arrange: 生成1000条数据
        var users = Enumerable.Range(1, 1000)
            .Select(i => new UserBatchItem { Name = $"User{i}", Age = 20 + (i % 50) })
            .ToArray();

        // Act
        var affected = await _repo.BatchInsertUsersAsync(users);

        // Assert
        Assert.AreEqual(1000, affected, "应该插入1000条记录");
        
        var count = GetCount("users");
        Assert.AreEqual(1000, count, "数据库应该有1000条记录");
    }

    [TestMethod]
    public async Task BatchInsert_EmptyCollection_ShouldInsertZero()
    {
        // Arrange
        var users = Array.Empty<UserBatchItem>();

        // Act
        var affected = await _repo.BatchInsertUsersAsync(users);

        // Assert
        Assert.AreEqual(0, affected, "空集合应该插入0条记录");
    }

    #endregion

    #region batch_values 占位符测试

    [TestMethod]
    public async Task BatchInsert_WithBatchValuesPlaceholder_ShouldGenerateCorrectSQL()
    {
        // Arrange
        var logs = new[]
        {
            new LogBatchItem { Level = "INFO", Message = "Test1", CreatedAt = "2025-01-01 12:00:00" },
            new LogBatchItem { Level = "WARN", Message = "Test2", CreatedAt = "2025-01-01 12:01:00" }
        };

        // Act
        var affected = await _repo.BatchInsertLogsAsync(logs);

        // Assert
        Assert.AreEqual(2, affected, "应该插入2条日志");
        
        var count = GetCount("logs");
        Assert.AreEqual(2, count, "数据库应该有2条日志");
    }

    [TestMethod]
    public async Task BatchInsert_VerifyNoDuplicateVALUES_ShouldSucceed()
    {
        // 这个测试确保不会出现 "VALUES VALUES" 的SQL错误
        // Arrange
        var logs = new[]
        {
            new LogBatchItem { Level = "ERROR", Message = "Critical", CreatedAt = "2025-01-01" }
        };

        // Act - 如果SQL有 "VALUES VALUES"，这里会抛出异常
        var affected = await _repo.BatchInsertLogsAsync(logs);

        // Assert
        Assert.AreEqual(1, affected);
    }

    #endregion

    #region 参数化查询和SQL注入防护

    [TestMethod]
    public async Task BatchInsert_WithSpecialCharacters_ShouldEscape()
    {
        // Arrange: 包含特殊字符的数据
        var users = new[]
        {
            new UserBatchItem { Name = "O'Brien", Age = 25 },
            new UserBatchItem { Name = "Alice \"Wonder\" Smith", Age = 30 },
            new UserBatchItem { Name = "Bob; DROP TABLE users;--", Age = 35 }
        };

        // Act
        var affected = await _repo.BatchInsertUsersAsync(users);

        // Assert
        Assert.AreEqual(3, affected, "特殊字符应该被正确转义");
        
        // 验证表仍然存在（没有被SQL注入攻击）
        var count = GetCount("users");
        Assert.AreEqual(3, count);
    }

    [TestMethod]
    public async Task BatchInsert_WithUnicodeCharacters_ShouldPreserve()
    {
        // Arrange: Unicode字符
        var users = new[]
        {
            new UserBatchItem { Name = "张三", Age = 25 },
            new UserBatchItem { Name = "李四", Age = 30 },
            new UserBatchItem { Name = "Müller", Age = 35 },
            new UserBatchItem { Name = "José", Age = 40 }
        };

        // Act
        var affected = await _repo.BatchInsertUsersAsync(users);

        // Assert
        Assert.AreEqual(4, affected);
        
        // 验证Unicode字符被保留
        var names = GetNames();
        CollectionAssert.Contains(names.ToList(), "张三");
        CollectionAssert.Contains(names.ToList(), "李四");
        CollectionAssert.Contains(names.ToList(), "Müller");
        CollectionAssert.Contains(names.ToList(), "José");
    }

    #endregion

    #region 性能测试（基准）

    [TestMethod]
    public async Task BatchInsert_10000Records_ShouldCompleteQuickly()
    {
        // Arrange
        var users = Enumerable.Range(1, 10000)
            .Select(i => new UserBatchItem { Name = $"User{i}", Age = 20 + (i % 50) })
            .ToArray();

        // Act
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var affected = await _repo.BatchInsertUsersAsync(users);
        sw.Stop();

        // Assert
        Assert.AreEqual(10000, affected);
        Assert.IsTrue(sw.ElapsedMilliseconds < 2000, 
            $"10000条记录应该在2秒内完成，实际: {sw.ElapsedMilliseconds}ms");
    }

    #endregion

    #region Helper Methods

    private int GetCount(string tableName)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = $"SELECT COUNT(*) FROM {tableName}";
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private IEnumerable<string> GetNames()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT name FROM users";
        using var reader = cmd.ExecuteReader();
        var names = new List<string>();
        while (reader.Read())
        {
            names.Add(reader.GetString(0));
        }
        return names;
    }

    #endregion
}

#region Test Repository

public interface IBatchTestRepository
{
    [SqlTemplate("INSERT INTO users (name, age) VALUES {{batch_values}}")]
    [BatchOperation(MaxBatchSize = 1000)]
    Task<int> BatchInsertUsersAsync(IEnumerable<UserBatchItem> users);

    [SqlTemplate("INSERT INTO logs (level, message, created_at) VALUES {{batch_values}}")]
    [BatchOperation]
    Task<int> BatchInsertLogsAsync(IEnumerable<LogBatchItem> logs);
}

public class UserBatchItem
{
    public string Name { get; set; } = "";
    public int Age { get; set; }
}

public class LogBatchItem
{
    public string Level { get; set; } = "";
    public string Message { get; set; } = "";
    public string CreatedAt { get; set; } = "";
}

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IBatchTestRepository))]
public partial class BatchTestRepository(IDbConnection connection) : IBatchTestRepository { }

#endregion

