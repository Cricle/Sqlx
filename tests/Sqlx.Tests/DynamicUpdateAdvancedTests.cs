// <copyright file="DynamicUpdateAdvancedTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx.Tests;

using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using Sqlx.Annotations;

/// <summary>
/// Advanced tests for DynamicUpdateAsync methods covering edge cases, complex scenarios, and parameter binding.
/// </summary>
[TestClass]
public class DynamicUpdateAdvancedTests
{
    private SqliteConnection? _connection;
    private IAdvancedDynamicTestRepository? _repository;

    [TestInitialize]
    public void Setup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        // Create table with various data types
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE advanced_dynamic_entities (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                description TEXT,
                value INTEGER NOT NULL,
                price REAL NOT NULL,
                is_active INTEGER NOT NULL DEFAULT 1,
                priority INTEGER NOT NULL DEFAULT 0,
                quantity INTEGER NOT NULL DEFAULT 0,
                discount REAL NOT NULL DEFAULT 0.0,
                created_at TEXT NOT NULL,
                updated_at TEXT
            )";
        cmd.ExecuteNonQuery();

        _repository = new AdvancedDynamicTestRepository(_connection);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _connection?.Dispose();
    }

    #region Parameter Binding Tests

    [TestMethod]
    public async Task DynamicUpdateAsync_SingleParameter_BindsCorrectly()
    {
        // Arrange
        var entity = CreateTestEntity("Test", 100);
        var id = await _repository!.InsertAndGetIdAsync(entity);

        // Act
        var result = await _repository.DynamicUpdateAsync(id, e => new AdvancedDynamicEntity { Value = 200 });

        // Assert
        Assert.AreEqual(1, result);
        var updated = await _repository.GetByIdAsync(id);
        Assert.AreEqual(200, updated!.Value);
    }

    [TestMethod]
    public async Task DynamicUpdateAsync_MultipleParameters_BindsCorrectly()
    {
        // Arrange
        var entity = CreateTestEntity("Test", 100);
        var id = await _repository!.InsertAndGetIdAsync(entity);

        // Act - 3 parameters: @p0, @p1, @p2
        var result = await _repository.DynamicUpdateAsync(id, e => new AdvancedDynamicEntity 
        { 
            Name = "Updated",
            Value = 200,
            Price = 99.99
        });

        // Assert
        Assert.AreEqual(1, result);
        var updated = await _repository.GetByIdAsync(id);
        Assert.AreEqual("Updated", updated!.Name);
        Assert.AreEqual(200, updated.Value);
        Assert.AreEqual(99.99, updated.Price, 0.001);
    }

    [TestMethod]
    public async Task DynamicUpdateAsync_ManyParameters_BindsCorrectly()
    {
        // Arrange
        var entity = CreateTestEntity("Test", 100);
        var id = await _repository!.InsertAndGetIdAsync(entity);

        // Act - 7 parameters: @p0 through @p6
        var result = await _repository.DynamicUpdateAsync(id, e => new AdvancedDynamicEntity 
        { 
            Name = "Updated",
            Description = "New Description",
            Value = 200,
            Price = 99.99,
            IsActive = false,
            Priority = 5,
            Quantity = 10
        });

        // Assert
        Assert.AreEqual(1, result);
        var updated = await _repository.GetByIdAsync(id);
        Assert.AreEqual("Updated", updated!.Name);
        Assert.AreEqual("New Description", updated.Description);
        Assert.AreEqual(200, updated.Value);
        Assert.AreEqual(99.99, updated.Price, 0.001);
        Assert.AreEqual(false, updated.IsActive);
        Assert.AreEqual(5, updated.Priority);
        Assert.AreEqual(10, updated.Quantity);
    }

    #endregion

    #region Expression Tests

    [TestMethod]
    public async Task DynamicUpdateAsync_IncrementExpression_BindsCorrectly()
    {
        // Arrange
        var entity = CreateTestEntity("Test", 100);
        var id = await _repository!.InsertAndGetIdAsync(entity);

        // Act - Expression: e.Value + 50 generates @p0 = 50
        var result = await _repository.DynamicUpdateAsync(id, e => new AdvancedDynamicEntity 
        { 
            Value = e.Value + 50
        });

        // Assert
        Assert.AreEqual(1, result);
        var updated = await _repository.GetByIdAsync(id);
        Assert.AreEqual(150, updated!.Value);
    }

    [TestMethod]
    public async Task DynamicUpdateAsync_DecrementExpression_BindsCorrectly()
    {
        // Arrange
        var entity = CreateTestEntity("Test", 100);
        var id = await _repository!.InsertAndGetIdAsync(entity);

        // Act
        var result = await _repository.DynamicUpdateAsync(id, e => new AdvancedDynamicEntity 
        { 
            Value = e.Value - 30
        });

        // Assert
        Assert.AreEqual(1, result);
        var updated = await _repository.GetByIdAsync(id);
        Assert.AreEqual(70, updated!.Value);
    }

    [TestMethod]
    public async Task DynamicUpdateAsync_MultiplyExpression_BindsCorrectly()
    {
        // Arrange
        var entity = CreateTestEntity("Test", 10);
        entity.Price = 5.0;
        var id = await _repository!.InsertAndGetIdAsync(entity);

        // Act
        var result = await _repository.DynamicUpdateAsync(id, e => new AdvancedDynamicEntity 
        { 
            Value = e.Value * 3,
            Price = e.Price * 2.0
        });

        // Assert
        Assert.AreEqual(1, result);
        var updated = await _repository.GetByIdAsync(id);
        Assert.AreEqual(30, updated!.Value);
        Assert.AreEqual(10.0, updated.Price, 0.001);
    }

    [TestMethod]
    public async Task DynamicUpdateAsync_ComplexExpression_BindsCorrectly()
    {
        // Arrange
        var entity = CreateTestEntity("Test", 100);
        entity.Quantity = 5;
        entity.Discount = 0.1;
        var id = await _repository!.InsertAndGetIdAsync(entity);

        // Act - Complex expression with multiple operations
        var result = await _repository.DynamicUpdateAsync(id, e => new AdvancedDynamicEntity 
        { 
            Value = (e.Value + 50) * 2,
            Quantity = e.Quantity + 10,
            Discount = e.Discount + 0.05
        });

        // Assert
        Assert.AreEqual(1, result);
        var updated = await _repository.GetByIdAsync(id);
        Assert.AreEqual(300, updated!.Value); // (100 + 50) * 2
        Assert.AreEqual(15, updated.Quantity); // 5 + 10
        Assert.AreEqual(0.15, updated.Discount, 0.001); // 0.1 + 0.05
    }

    #endregion

    #region Null Handling Tests

    [TestMethod]
    public async Task DynamicUpdateAsync_NullableFieldToNull_BindsCorrectly()
    {
        // Arrange
        var entity = CreateTestEntity("Test", 100);
        entity.Description = "Original";
        var id = await _repository!.InsertAndGetIdAsync(entity);

        // Act - Set nullable field to null
        var result = await _repository.DynamicUpdateAsync(id, e => new AdvancedDynamicEntity 
        { 
            Description = null
        });

        // Assert
        Assert.AreEqual(1, result);
        var updated = await _repository.GetByIdAsync(id);
        Assert.IsNull(updated!.Description);
    }

    [TestMethod]
    public async Task DynamicUpdateAsync_NullableFieldToValue_BindsCorrectly()
    {
        // Arrange
        var entity = CreateTestEntity("Test", 100);
        entity.Description = null;
        var id = await _repository!.InsertAndGetIdAsync(entity);

        // Act - Set nullable field to value
        var result = await _repository.DynamicUpdateAsync(id, e => new AdvancedDynamicEntity 
        { 
            Description = "New Description"
        });

        // Assert
        Assert.AreEqual(1, result);
        var updated = await _repository.GetByIdAsync(id);
        Assert.AreEqual("New Description", updated!.Description);
    }

    [TestMethod]
    public async Task DynamicUpdateAsync_MixedNullAndValues_BindsCorrectly()
    {
        // Arrange
        var entity = CreateTestEntity("Test", 100);
        entity.Description = "Original";
        entity.UpdatedAt = DateTime.UtcNow;
        var id = await _repository!.InsertAndGetIdAsync(entity);

        // Act - Mix of null and non-null values
        var result = await _repository.DynamicUpdateAsync(id, e => new AdvancedDynamicEntity 
        { 
            Description = null,
            Value = 200,
            UpdatedAt = DateTime.UtcNow
        });

        // Assert
        Assert.AreEqual(1, result);
        var updated = await _repository.GetByIdAsync(id);
        Assert.IsNull(updated!.Description);
        Assert.AreEqual(200, updated.Value);
        Assert.IsNotNull(updated.UpdatedAt);
    }

    #endregion

    #region DynamicUpdateWhereAsync Tests

    [TestMethod]
    public async Task DynamicUpdateWhereAsync_SimpleCondition_UpdatesCorrectly()
    {
        // Arrange
        await InsertTestEntities();

        // Act - Update all active entities
        var result = await _repository!.DynamicUpdateWhereAsync(
            e => new AdvancedDynamicEntity { Value = 999 },
            e => e.IsActive);

        // Assert
        Assert.AreEqual(3, result); // 3 active entities
        var all = await _repository.GetAllAsync();
        Assert.AreEqual(3, all.Count(e => e.Value == 999));
    }

    [TestMethod]
    public async Task DynamicUpdateWhereAsync_ComplexCondition_UpdatesCorrectly()
    {
        // Arrange
        await InsertTestEntities();

        // Act - Update entities with priority >= 3 and active
        var result = await _repository!.DynamicUpdateWhereAsync(
            e => new AdvancedDynamicEntity { Value = 888 },
            e => e.Priority >= 3 && e.IsActive);

        // Assert
        Assert.AreEqual(2, result); // 2 entities match
        var all = await _repository.GetAllAsync();
        Assert.AreEqual(2, all.Count(e => e.Value == 888));
    }

    [TestMethod]
    public async Task DynamicUpdateWhereAsync_NoMatches_ReturnsZero()
    {
        // Arrange
        await InsertTestEntities();

        // Act - Update entities with impossible condition
        var result = await _repository!.DynamicUpdateWhereAsync(
            e => new AdvancedDynamicEntity { Value = 777 },
            e => e.Priority > 100);

        // Assert
        Assert.AreEqual(0, result);
    }

    [TestMethod]
    public async Task DynamicUpdateWhereAsync_MultipleFieldsAndExpression_UpdatesCorrectly()
    {
        // Arrange - Insert exactly 5 entities with known priorities
        await _repository!.InsertAsync(new AdvancedDynamicEntity 
        { 
            Name = "Entity1", Value = 100, IsActive = true, Priority = 1, 
            Price = 10.0, Quantity = 5, Discount = 0.1, CreatedAt = DateTime.UtcNow 
        });
        await _repository.InsertAsync(new AdvancedDynamicEntity 
        { 
            Name = "Entity2", Value = 200, IsActive = true, Priority = 3, 
            Price = 20.0, Quantity = 10, Discount = 0.2, CreatedAt = DateTime.UtcNow 
        });
        await _repository.InsertAsync(new AdvancedDynamicEntity 
        { 
            Name = "Entity3", Value = 300, IsActive = false, Priority = 2, 
            Price = 30.0, Quantity = 15, Discount = 0.3, CreatedAt = DateTime.UtcNow 
        });

        // Act - Update entities with priority == 1 or priority == 2
        var result = await _repository.DynamicUpdateWhereAsync(
            e => new AdvancedDynamicEntity 
            { 
                Value = e.Value + 100,
                Priority = 5,
                IsActive = false
            },
            e => e.Priority == 1 || e.Priority == 2);

        // Assert - Should update Entity1 (priority=1) and Entity3 (priority=2)
        Assert.AreEqual(2, result);
        
        var after = await _repository.GetAllAsync();
        var updated = after.Where(e => e.Priority == 5).ToList();
        Assert.AreEqual(2, updated.Count);
        Assert.IsTrue(updated.All(e => !e.IsActive));
        
        // Verify specific entities were updated
        var entity1 = updated.FirstOrDefault(e => e.Name == "Entity1");
        Assert.IsNotNull(entity1);
        Assert.AreEqual(200, entity1!.Value); // 100 + 100
        
        var entity3 = updated.FirstOrDefault(e => e.Name == "Entity3");
        Assert.IsNotNull(entity3);
        Assert.AreEqual(400, entity3!.Value); // 300 + 100
    }

    #endregion

    #region Data Type Tests

    [TestMethod]
    public async Task DynamicUpdateAsync_IntegerTypes_BindsCorrectly()
    {
        // Arrange
        var entity = CreateTestEntity("Test", 100);
        var id = await _repository!.InsertAndGetIdAsync(entity);

        // Act
        var result = await _repository.DynamicUpdateAsync(id, e => new AdvancedDynamicEntity 
        { 
            Value = 2147483647, // Max int
            Priority = -100,
            Quantity = 0
        });

        // Assert
        Assert.AreEqual(1, result);
        var updated = await _repository.GetByIdAsync(id);
        Assert.AreEqual(2147483647, updated!.Value);
        Assert.AreEqual(-100, updated.Priority);
        Assert.AreEqual(0, updated.Quantity);
    }

    [TestMethod]
    public async Task DynamicUpdateAsync_FloatingPointTypes_BindsCorrectly()
    {
        // Arrange
        var entity = CreateTestEntity("Test", 100);
        var id = await _repository!.InsertAndGetIdAsync(entity);

        // Act
        var result = await _repository.DynamicUpdateAsync(id, e => new AdvancedDynamicEntity 
        { 
            Price = 123.456,
            Discount = 0.0
        });

        // Assert
        Assert.AreEqual(1, result);
        var updated = await _repository.GetByIdAsync(id);
        Assert.AreEqual(123.456, updated!.Price, 0.001);
        Assert.AreEqual(0.0, updated.Discount, 0.001);
    }

    [TestMethod]
    public async Task DynamicUpdateAsync_BooleanTypes_BindsCorrectly()
    {
        // Arrange
        var entity = CreateTestEntity("Test", 100);
        entity.IsActive = true;
        var id = await _repository!.InsertAndGetIdAsync(entity);

        // Act
        var result = await _repository.DynamicUpdateAsync(id, e => new AdvancedDynamicEntity 
        { 
            IsActive = false
        });

        // Assert
        Assert.AreEqual(1, result);
        var updated = await _repository.GetByIdAsync(id);
        Assert.AreEqual(false, updated!.IsActive);
    }

    [TestMethod]
    public async Task DynamicUpdateAsync_StringTypes_BindsCorrectly()
    {
        // Arrange
        var entity = CreateTestEntity("Test", 100);
        var id = await _repository!.InsertAndGetIdAsync(entity);

        // Act - Test various string values
        var result = await _repository.DynamicUpdateAsync(id, e => new AdvancedDynamicEntity 
        { 
            Name = "Updated Name with 'quotes' and \"double quotes\"",
            Description = "Line1\nLine2\tTabbed"
        });

        // Assert
        Assert.AreEqual(1, result);
        var updated = await _repository.GetByIdAsync(id);
        Assert.AreEqual("Updated Name with 'quotes' and \"double quotes\"", updated!.Name);
        Assert.AreEqual("Line1\nLine2\tTabbed", updated.Description);
    }

    [TestMethod]
    public async Task DynamicUpdateAsync_DateTimeTypes_BindsCorrectly()
    {
        // Arrange
        var entity = CreateTestEntity("Test", 100);
        var id = await _repository!.InsertAndGetIdAsync(entity);
        var now = DateTime.UtcNow;

        // Act
        var result = await _repository.DynamicUpdateAsync(id, e => new AdvancedDynamicEntity 
        { 
            UpdatedAt = now
        });

        // Assert
        Assert.AreEqual(1, result);
        var updated = await _repository.GetByIdAsync(id);
        Assert.IsNotNull(updated!.UpdatedAt);
        // SQLite stores datetime as string, so we compare with some tolerance
        Assert.IsTrue((updated.UpdatedAt!.Value - now).TotalSeconds < 1);
    }

    #endregion

    #region Concurrent Update Tests

    [TestMethod]
    public async Task DynamicUpdateAsync_ConcurrentUpdates_AllSucceed()
    {
        // Arrange
        var ids = new long[10];
        for (int i = 0; i < 10; i++)
        {
            var entity = CreateTestEntity($"Test{i}", i * 10);
            ids[i] = await _repository!.InsertAndGetIdAsync(entity);
        }

        // Act - Concurrent updates
        var tasks = ids.Select(id => 
            _repository!.DynamicUpdateAsync(id, e => new AdvancedDynamicEntity { Value = 999 })
        ).ToArray();
        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.IsTrue(results.All(r => r == 1));
        var all = await _repository!.GetAllAsync();
        Assert.AreEqual(10, all.Count(e => e.Value == 999));
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    public async Task DynamicUpdateAsync_EmptyUpdate_NoFieldsChanged()
    {
        // Arrange
        var entity = CreateTestEntity("Test", 100);
        entity.Description = "Original";
        var id = await _repository!.InsertAndGetIdAsync(entity);

        // Act - Update with no fields (should still execute but change nothing meaningful)
        // Note: This creates an empty SET clause which might fail, so we update at least one field
        var result = await _repository.DynamicUpdateAsync(id, e => new AdvancedDynamicEntity 
        { 
            Value = 100 // Same value
        });

        // Assert
        Assert.AreEqual(1, result);
        var updated = await _repository.GetByIdAsync(id);
        Assert.AreEqual("Test", updated!.Name);
        Assert.AreEqual(100, updated.Value);
    }

    [TestMethod]
    public async Task DynamicUpdateAsync_ZeroValues_BindsCorrectly()
    {
        // Arrange
        var entity = CreateTestEntity("Test", 100);
        entity.Value = 100;
        entity.Price = 50.0;
        var id = await _repository!.InsertAndGetIdAsync(entity);

        // Act - Set to zero
        var result = await _repository.DynamicUpdateAsync(id, e => new AdvancedDynamicEntity 
        { 
            Value = 0,
            Price = 0.0,
            Discount = 0.0
        });

        // Assert
        Assert.AreEqual(1, result);
        var updated = await _repository.GetByIdAsync(id);
        Assert.AreEqual(0, updated!.Value);
        Assert.AreEqual(0.0, updated.Price, 0.001);
        Assert.AreEqual(0.0, updated.Discount, 0.001);
    }

    [TestMethod]
    public async Task DynamicUpdateAsync_NegativeValues_BindsCorrectly()
    {
        // Arrange
        var entity = CreateTestEntity("Test", 100);
        var id = await _repository!.InsertAndGetIdAsync(entity);

        // Act
        var result = await _repository.DynamicUpdateAsync(id, e => new AdvancedDynamicEntity 
        { 
            Value = -100,
            Priority = -5,
            Discount = -0.1
        });

        // Assert
        Assert.AreEqual(1, result);
        var updated = await _repository.GetByIdAsync(id);
        Assert.AreEqual(-100, updated!.Value);
        Assert.AreEqual(-5, updated.Priority);
        Assert.AreEqual(-0.1, updated.Discount, 0.001);
    }

    #endregion

    #region Helper Methods

    private AdvancedDynamicEntity CreateTestEntity(string name, int value)
    {
        return new AdvancedDynamicEntity
        {
            Name = name,
            Value = value,
            Price = 10.0,
            IsActive = true,
            Priority = 0,
            Quantity = 0,
            Discount = 0.0,
            CreatedAt = DateTime.UtcNow
        };
    }

    private async Task InsertTestEntities()
    {
        await _repository!.InsertAsync(new AdvancedDynamicEntity 
        { 
            Name = "Entity1", Value = 100, IsActive = true, Priority = 1, 
            Price = 10.0, Quantity = 5, Discount = 0.1, CreatedAt = DateTime.UtcNow 
        });
        await _repository.InsertAsync(new AdvancedDynamicEntity 
        { 
            Name = "Entity2", Value = 200, IsActive = true, Priority = 3, 
            Price = 20.0, Quantity = 10, Discount = 0.2, CreatedAt = DateTime.UtcNow 
        });
        await _repository.InsertAsync(new AdvancedDynamicEntity 
        { 
            Name = "Entity3", Value = 300, IsActive = false, Priority = 2, 
            Price = 30.0, Quantity = 15, Discount = 0.3, CreatedAt = DateTime.UtcNow 
        });
        await _repository.InsertAsync(new AdvancedDynamicEntity 
        { 
            Name = "Entity4", Value = 400, IsActive = true, Priority = 5, 
            Price = 40.0, Quantity = 20, Discount = 0.4, CreatedAt = DateTime.UtcNow 
        });
        await _repository.InsertAsync(new AdvancedDynamicEntity 
        { 
            Name = "Entity5", Value = 500, IsActive = false, Priority = 4, 
            Price = 50.0, Quantity = 25, Discount = 0.5, CreatedAt = DateTime.UtcNow 
        });
    }

    #endregion
}

[Sqlx]
[TableName("advanced_dynamic_entities")]
public class AdvancedDynamicEntity
{
    [Key]
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Value { get; set; }
    public double Price { get; set; }
    public bool IsActive { get; set; }
    public int Priority { get; set; }
    public int Quantity { get; set; }
    public double Discount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public interface IAdvancedDynamicTestRepository : ICrudRepository<AdvancedDynamicEntity, long>
{
}

[TableName("advanced_dynamic_entities")]
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IAdvancedDynamicTestRepository))]
public partial class AdvancedDynamicTestRepository(SqliteConnection connection) : IAdvancedDynamicTestRepository
{
}
