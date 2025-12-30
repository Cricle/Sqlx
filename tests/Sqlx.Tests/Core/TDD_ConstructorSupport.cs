// -----------------------------------------------------------------------
// <copyright file="TDD_ConstructorSupport.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

namespace Sqlx.Tests.Core;

// ==================== 测试实体 ====================

public class ConstructorUser
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public string? Email { get; set; }
}

public record ConstructorUserRecord(long Id, string Name, int Age, string? Email);

// ==================== 场景1: 主构造函数 (C# 12+) ====================

public partial interface IPrimaryConstructorRepo
{
    [SqlTemplate("SELECT {{columns}} FROM users WHERE id = @id")]
    Task<ConstructorUser?> GetByIdAsync(long id, CancellationToken ct = default);

    [SqlTemplate("INSERT INTO users (name, age, email) VALUES (@name, @age, @email)")]
    [ReturnInsertedId]
    Task<long> InsertAsync(string name, int age, string? email = null, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM users")]
    Task<List<ConstructorUser>> GetAllAsync(CancellationToken ct = default);
}

// 使用主构造函数（C# 12+ 语法）
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IPrimaryConstructorRepo))]
public partial class PrimaryConstructorRepo(DbConnection connection) : IPrimaryConstructorRepo
{
}

// ==================== 场景2: 传统有参构造函数 ====================

public partial interface IParameterizedConstructorRepo
{
    [SqlTemplate("SELECT {{columns}} FROM users WHERE age >= @minAge")]
    Task<List<ConstructorUser>> GetByMinAgeAsync(int minAge, CancellationToken ct = default);

    [SqlTemplate("INSERT INTO users (name, age, email) VALUES (@name, @age, @email)")]
    [ReturnInsertedId]
    Task<long> InsertAsync(string name, int age, string? email = null, CancellationToken ct = default);

    [SqlTemplate("UPDATE users SET name = @name, age = @age WHERE id = @id")]
    Task<int> UpdateAsync(long id, string name, int age, CancellationToken ct = default);

    [SqlTemplate("DELETE FROM users WHERE id = @id")]
    Task<int> DeleteAsync(long id, CancellationToken ct = default);
}

// 使用传统的有参构造函数
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IParameterizedConstructorRepo))]
public partial class ParameterizedConstructorRepo(DbConnection connection) : IParameterizedConstructorRepo
{
    // 验证连接参数
    private DbConnection ValidatedConnection => connection ?? throw new ArgumentNullException(nameof(connection));
}

// ==================== 场景3: 多个参数的构造函数 ====================

public partial interface IMultiParamConstructorRepo
{
    [SqlTemplate("SELECT {{columns}} FROM users")]
    Task<List<ConstructorUser>> GetAllAsync(CancellationToken ct = default);

    [SqlTemplate("INSERT INTO users (name, age, email) VALUES (@name, @age, @email)")]
    [ReturnInsertedId]
    Task<long> InsertAsync(string name, int age, string? email = null, CancellationToken ct = default);
}

// 多个参数的构造函数
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IMultiParamConstructorRepo))]
public partial class MultiParamConstructorRepo : IMultiParamConstructorRepo
{
    private readonly DbConnection _connection;
    private readonly string _prefix;

    public MultiParamConstructorRepo(DbConnection connection, string prefix = "")
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _prefix = prefix;
    }

    public string GetPrefix() => _prefix;
}

// ==================== 场景4: 主构造函数 + 依赖注入 ====================

public interface IConstructorLogger
{
    void Log(string message);
}

public class TestConstructorLogger : IConstructorLogger
{
    public List<string> Messages { get; } = new();
    public void Log(string message) => Messages.Add(message);
}

public partial interface IPrimaryConstructorWithDIRepo
{
    [SqlTemplate("SELECT {{columns}} FROM users WHERE id = @id")]
    Task<ConstructorUser?> GetByIdAsync(long id, CancellationToken ct = default);

    [SqlTemplate("INSERT INTO users (name, age, email) VALUES (@name, @age, @email)")]
    [ReturnInsertedId]
    Task<long> InsertAsync(string name, int age, string? email = null, CancellationToken ct = default);
}

// 主构造函数 + 多个依赖
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IPrimaryConstructorWithDIRepo))]
public partial class PrimaryConstructorWithDIRepo(DbConnection connection, IConstructorLogger logger)
    : IPrimaryConstructorWithDIRepo
{
    public IConstructorLogger Logger => logger;

    public void LogOperation(string operation)
    {
        logger.Log($"Operation: {operation}");
    }
}

// ==================== 场景5: Record 类型作为实体 ====================
// TODO: Record支持需要修复生成器的构造函数调用逻辑
/*
public partial interface IRecordEntityRepo
{
    [SqlTemplate("SELECT id, name, age, email FROM users WHERE id = @id")]
    Task<ConstructorUserRecord?> GetByIdAsync(long id, CancellationToken ct = default);

    [SqlTemplate("SELECT id, name, age, email FROM users")]
    Task<List<ConstructorUserRecord>> GetAllAsync(CancellationToken ct = default);

    [SqlTemplate("INSERT INTO users (name, age, email) VALUES (@name, @age, @email)")]
    [ReturnInsertedId]
    Task<long> InsertAsync(string name, int age, string? email = null, CancellationToken ct = default);
}

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IRecordEntityRepo))]
public partial class RecordEntityRepo(DbConnection connection) : IRecordEntityRepo
{
}
*/

// ==================== 场景6: 混合构造函数 ====================

public partial interface IMixedConstructorRepo
{
    [SqlTemplate("SELECT {{columns}} FROM users")]
    Task<List<ConstructorUser>> GetAllAsync(CancellationToken ct = default);
}

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IMixedConstructorRepo))]
public partial class MixedConstructorRepo(DbConnection connection, string? tag = null)
    : IMixedConstructorRepo
{
    private readonly string _tag = tag ?? "default";

    public MixedConstructorRepo(DbConnection connection) : this(connection, null)
    {
    }

    public string GetTag() => _tag;
}

// ==================== 测试类 ====================

/// <summary>
/// 测试主构造函数和有参构造函数的支持
/// </summary>
[TestClass]
public class TDD_ConstructorSupport : IDisposable
{
    private readonly DbConnection _connection;

    public TDD_ConstructorSupport()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        // 创建测试表
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                age INTEGER NOT NULL,
                email TEXT
            )";
        cmd.ExecuteNonQuery();
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }

    [TestMethod]
    public async Task PrimaryConstructor_Should_Work()
    {
        // Arrange
        var repo = new PrimaryConstructorRepo(_connection);

        // Act - 插入数据
        var userId = await repo.InsertAsync("Alice", 25, "alice@example.com");

        // Assert
        Assert.IsTrue(userId > 0);

        // Act - 查询数据
        var user = await repo.GetByIdAsync(userId);

        // Assert
        Assert.IsNotNull(user);
        Assert.AreEqual("Alice", user.Name);
        Assert.AreEqual(25, user.Age);
        Assert.AreEqual("alice@example.com", user.Email);
    }

    [TestMethod]
    public async Task PrimaryConstructor_MultipleOperations_Should_Work()
    {
        // Arrange
        var repo = new PrimaryConstructorRepo(_connection);

        // Act - 插入多条数据
        var id1 = await repo.InsertAsync("Bob", 30);
        var id2 = await repo.InsertAsync("Charlie", 35, "charlie@example.com");

        // Act - 查询所有数据
        var users = await repo.GetAllAsync();

        // Assert
        Assert.AreEqual(2, users.Count);
        Assert.IsTrue(users.Any(u => u.Name == "Bob"));
        Assert.IsTrue(users.Any(u => u.Name == "Charlie"));
    }

    [TestMethod]
    public async Task ParameterizedConstructor_Should_Work()
    {
        // Arrange
        var repo = new ParameterizedConstructorRepo(_connection);

        // Act - 插入数据
        var userId = await repo.InsertAsync("David", 28, "david@example.com");

        // Assert
        Assert.IsTrue(userId > 0);

        // Act - 查询数据
        var users = await repo.GetByMinAgeAsync(25);

        // Assert
        Assert.AreEqual(1, users.Count);
        Assert.AreEqual("David", users[0].Name);
    }

    [TestMethod]
    public async Task ParameterizedConstructor_CRUD_Should_Work()
    {
        // Arrange
        var repo = new ParameterizedConstructorRepo(_connection);

        // Act - 插入
        var userId = await repo.InsertAsync("Eve", 32);

        // Act - 更新
        var updated = await repo.UpdateAsync(userId, "Eve Updated", 33);
        Assert.AreEqual(1, updated);

        // Act - 查询
        var users = await repo.GetByMinAgeAsync(30);
        Assert.AreEqual(1, users.Count);
        Assert.AreEqual("Eve Updated", users[0].Name);
        Assert.AreEqual(33, users[0].Age);

        // Act - 删除
        var deleted = await repo.DeleteAsync(userId);
        Assert.AreEqual(1, deleted);

        // Act - 验证删除
        var afterDelete = await repo.GetByMinAgeAsync(0);
        Assert.AreEqual(0, afterDelete.Count);
    }

    [TestMethod]
    public async Task MultiParamConstructor_Should_Work()
    {
        // Arrange
        var repo = new MultiParamConstructorRepo(_connection, "Test");

        // Assert - 验证构造函数参数
        Assert.AreEqual("Test", repo.GetPrefix());

        // Act - 使用仓储
        var userId = await repo.InsertAsync("Frank", 40);
        Assert.IsTrue(userId > 0);

        var users = await repo.GetAllAsync();
        Assert.AreEqual(1, users.Count);
        Assert.AreEqual("Frank", users[0].Name);
    }

    [TestMethod]
    public async Task PrimaryConstructorWithDI_Should_Work()
    {
        // Arrange
        var logger = new TestConstructorLogger();
        var repo = new PrimaryConstructorWithDIRepo(_connection, logger);

        // Assert - 验证依赖注入
        Assert.IsNotNull(repo.Logger);

        // Act - 使用仓储
        repo.LogOperation("Insert");
        var userId = await repo.InsertAsync("Grace", 29);

        // Assert
        Assert.IsTrue(userId > 0);
        Assert.AreEqual(1, logger.Messages.Count);
        Assert.AreEqual("Operation: Insert", logger.Messages[0]);

        // Act - 查询
        repo.LogOperation("Get");
        var user = await repo.GetByIdAsync(userId);

        // Assert
        Assert.IsNotNull(user);
        Assert.AreEqual("Grace", user.Name);
        Assert.AreEqual(2, logger.Messages.Count);
    }

    // TODO: Record支持需要修复生成器
    /*
    [TestMethod]
    public async Task RecordEntity_With_PrimaryConstructor_Should_Work()
    {
        // Arrange
        var repo = new RecordEntityRepo(_connection);

        // Act - 插入数据
        var userId = await repo.InsertAsync("Henry", 45, "henry@example.com");

        // Act - 查询单条
        var user = await repo.GetByIdAsync(userId);

        // Assert
        Assert.IsNotNull(user);
        Assert.AreEqual("Henry", user.Name);
        Assert.AreEqual(45, user.Age);
        Assert.AreEqual("henry@example.com", user.Email);

        // Act - 查询所有
        var users = await repo.GetAllAsync();
        Assert.AreEqual(1, users.Count);
    }
    */

    [TestMethod]
    public async Task MixedConstructor_Should_Work()
    {
        // Arrange - 使用主构造函数
        var repo1 = new MixedConstructorRepo(_connection, "custom");
        Assert.AreEqual("custom", repo1.GetTag());

        // Arrange - 使用重载构造函数
        var repo2 = new MixedConstructorRepo(_connection);
        Assert.AreEqual("default", repo2.GetTag());

        // Act & Assert - 两个实例都应该工作
        var users1 = await repo1.GetAllAsync();
        var users2 = await repo2.GetAllAsync();

        Assert.IsNotNull(users1);
        Assert.IsNotNull(users2);
    }
}
