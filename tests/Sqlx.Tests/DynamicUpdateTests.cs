// <copyright file="DynamicUpdateTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx.Tests;

using System;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using Sqlx.Annotations;

/// <summary>
/// Tests for DynamicUpdateAsync methods in ICrudRepository.
/// </summary>
[TestClass]
public class DynamicUpdateTests
{
    private SqliteConnection? _connection;
    private IDynamicTestRepository? _repository;

    [TestInitialize]
    public void Setup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        // Create table
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE dynamic_test_entities (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                value INTEGER NOT NULL,
                is_active INTEGER NOT NULL DEFAULT 1,
                created_at TEXT NOT NULL
            )";
        cmd.ExecuteNonQuery();

        _repository = new DynamicTestRepository(_connection);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _connection?.Dispose();
    }

    [TestMethod]
    public async Task DynamicUpdateAsync_SingleField_UpdatesSuccessfully()
    {
        // Arrange
        var entity = new DynamicTestEntity
        {
            Name = "Test",
            Value = 100,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var id = await _repository!.InsertAndGetIdAsync(entity);

        // Act
        var result = await _repository.DynamicUpdateAsync(id, e => new DynamicTestEntity { Value = 200 });

        // Assert
        Assert.AreEqual(1, result);
        var updated = await _repository.GetByIdAsync(id);
        Assert.IsNotNull(updated);
        Assert.AreEqual("Test", updated.Name); // Unchanged
        Assert.AreEqual(200, updated.Value); // Changed
    }

    [TestMethod]
    public async Task DynamicUpdateAsync_MultipleFields_UpdatesSuccessfully()
    {
        // Arrange
        var entity = new DynamicTestEntity
        {
            Name = "Test",
            Value = 100,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var id = await _repository!.InsertAndGetIdAsync(entity);

        // Act
        var result = await _repository.DynamicUpdateAsync(id, e => new DynamicTestEntity 
        { 
            Name = "Updated",
            Value = 200,
            IsActive = false
        });

        // Assert
        Assert.AreEqual(1, result);
        var updated = await _repository.GetByIdAsync(id);
        Assert.IsNotNull(updated);
        Assert.AreEqual("Updated", updated.Name);
        Assert.AreEqual(200, updated.Value);
        Assert.AreEqual(false, updated.IsActive);
    }

    [TestMethod]
    public async Task DynamicUpdateAsync_WithExpression_UpdatesSuccessfully()
    {
        // Arrange
        var entity = new DynamicTestEntity
        {
            Name = "Test",
            Value = 100,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var id = await _repository!.InsertAndGetIdAsync(entity);

        // Act - increment value
        var result = await _repository.DynamicUpdateAsync(id, e => new DynamicTestEntity 
        { 
            Value = e.Value + 50
        });

        // Assert
        Assert.AreEqual(1, result);
        var updated = await _repository.GetByIdAsync(id);
        Assert.IsNotNull(updated);
        Assert.AreEqual(150, updated.Value);
    }

    [TestMethod]
    public async Task DynamicUpdateWhereAsync_UpdatesMatchingEntities()
    {
        // Arrange
        await _repository!.InsertAsync(new DynamicTestEntity { Name = "Test1", Value = 100, IsActive = true, CreatedAt = DateTime.UtcNow });
        await _repository.InsertAsync(new DynamicTestEntity { Name = "Test2", Value = 200, IsActive = true, CreatedAt = DateTime.UtcNow });
        await _repository.InsertAsync(new DynamicTestEntity { Name = "Test3", Value = 300, IsActive = false, CreatedAt = DateTime.UtcNow });

        // Act - update all active entities
        var result = await _repository.DynamicUpdateWhereAsync(
            e => new DynamicTestEntity { Value = 999 },
            e => e.IsActive);

        // Assert
        Assert.AreEqual(2, result);
        var all = await _repository.GetAllAsync();
        Assert.AreEqual(999, all.First(e => e.Name == "Test1").Value);
        Assert.AreEqual(999, all.First(e => e.Name == "Test2").Value);
        Assert.AreEqual(300, all.First(e => e.Name == "Test3").Value); // Unchanged
    }

    [TestMethod]
    public async Task DynamicUpdateAsync_NonExistentId_ReturnsZero()
    {
        // Act
        var result = await _repository!.DynamicUpdateAsync(99999, e => new DynamicTestEntity { Value = 200 });

        // Assert
        Assert.AreEqual(0, result);
    }
}

[Sqlx]
[TableName("dynamic_test_entities")]
public class DynamicTestEntity
{
    [Key]
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public interface IDynamicTestRepository : ICrudRepository<DynamicTestEntity, long>
{
}

[TableName("dynamic_test_entities")]
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IDynamicTestRepository))]
public partial class DynamicTestRepository(SqliteConnection connection) : IDynamicTestRepository
{
}
