// -----------------------------------------------------------------------
// <copyright file="TDD_ConstructorSupport_Validation.cs" company="Cricle">
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

// ==================== 验证和约束测试实体 ====================

public class ValidatedUser
{
    public long Id { get; set; }
    public string Email { get; set; } = "";
    public int Age { get; set; }
    public decimal Balance { get; set; }
    public string? PhoneNumber { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UniqueEntity
{
    public long Id { get; set; }
    public string UniqueCode { get; set; } = "";
    public string Value { get; set; } = "";
}

public class CheckConstraintEntity
{
    public long Id { get; set; }
    public string Status { get; set; } = "";
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}

// ==================== 验证仓储接口 ====================

public partial interface IValidatedUserRepository
{
    [SqlTemplate("INSERT INTO validated_users (email, age, balance, phone_number, created_at) VALUES (@email, @age, @balance, @phoneNumber, @createdAt)")]
    [ReturnInsertedId]
    Task<long> InsertAsync(string email, int age, decimal balance, string? phoneNumber, DateTime createdAt, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM validated_users WHERE email = @email")]
    Task<ValidatedUser?> GetByEmailAsync(string email, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM validated_users WHERE age BETWEEN @minAge AND @maxAge")]
    Task<List<ValidatedUser>> GetByAgeRangeAsync(int minAge, int maxAge, CancellationToken ct = default);

    [SqlTemplate("UPDATE validated_users SET balance = balance + @amount WHERE id = @id AND balance + @amount >= 0")]
    Task<int> AddBalanceAsync(long id, decimal amount, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM validated_users WHERE phone_number IS NOT NULL")]
    Task<List<ValidatedUser>> GetUsersWithPhoneAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT COUNT(*) FROM validated_users WHERE email LIKE '%@' || @domain")]
    Task<int> CountByEmailDomainAsync(string domain, CancellationToken ct = default);
}

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IValidatedUserRepository))]
public partial class ValidatedUserRepository(DbConnection connection) : IValidatedUserRepository
{
}

public partial interface IUniqueEntityRepository
{
    [SqlTemplate("INSERT INTO unique_entities (unique_code, value) VALUES (@uniqueCode, @value)")]
    [ReturnInsertedId]
    Task<long> InsertAsync(string uniqueCode, string value, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM unique_entities WHERE unique_code = @uniqueCode")]
    Task<UniqueEntity?> GetByCodeAsync(string uniqueCode, CancellationToken ct = default);

    [SqlTemplate("SELECT COUNT(*) FROM unique_entities WHERE unique_code = @uniqueCode")]
    Task<int> CountByCodeAsync(string uniqueCode, CancellationToken ct = default);
}

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IUniqueEntityRepository))]
public partial class UniqueEntityRepository(DbConnection connection) : IUniqueEntityRepository
{
}

public partial interface ICheckConstraintRepository
{
    [SqlTemplate("INSERT INTO check_entities (status, quantity, price) VALUES (@status, @quantity, @price)")]
    [ReturnInsertedId]
    Task<long> InsertAsync(string status, int quantity, decimal price, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM check_entities WHERE id = @id")]
    Task<CheckConstraintEntity?> GetByIdAsync(long id, CancellationToken ct = default);

    [SqlTemplate("UPDATE check_entities SET quantity = @quantity WHERE id = @id")]
    Task<int> UpdateQuantityAsync(long id, int quantity, CancellationToken ct = default);

    [SqlTemplate("UPDATE check_entities SET status = @status WHERE id = @id")]
    Task<int> UpdateStatusAsync(long id, string status, CancellationToken ct = default);
}

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(ICheckConstraintRepository))]
public partial class CheckConstraintRepository(DbConnection connection) : ICheckConstraintRepository
{
}

// ==================== 测试类 ====================

/// <summary>
/// 主构造函数和验证约束测试
/// </summary>
[TestClass]
public class TDD_ConstructorSupport_Validation : IDisposable
{
    private readonly DbConnection _connection;

    public TDD_ConstructorSupport_Validation()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE validated_users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                email TEXT NOT NULL CHECK (email LIKE '%@%'),
                age INTEGER NOT NULL CHECK (age >= 0 AND age <= 150),
                balance DECIMAL(10,2) NOT NULL DEFAULT 0 CHECK (balance >= 0),
                phone_number TEXT,
                created_at TEXT NOT NULL
            );

            CREATE TABLE unique_entities (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                unique_code TEXT NOT NULL UNIQUE,
                value TEXT NOT NULL
            );

            CREATE TABLE check_entities (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                status TEXT NOT NULL CHECK (status IN ('Active', 'Inactive', 'Pending')),
                quantity INTEGER NOT NULL CHECK (quantity >= 0),
                price DECIMAL(10,2) NOT NULL CHECK (price > 0)
            );
        ";
        cmd.ExecuteNonQuery();
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }

    // ==================== Email验证测试 ====================

    [TestMethod]
    public async Task EmailValidation_ValidEmail_ShouldInsert()
    {
        // Arrange
        var repo = new ValidatedUserRepository(_connection);
        var now = DateTime.UtcNow;

        // Act
        var userId = await repo.InsertAsync("valid@example.com", 25, 100m, null, now);

        // Assert
        Assert.IsTrue(userId > 0);

        var user = await repo.GetByEmailAsync("valid@example.com");
        Assert.IsNotNull(user);
        Assert.AreEqual("valid@example.com", user.Email);
    }

    [TestMethod]
    public async Task EmailValidation_InvalidEmail_ShouldThrow()
    {
        // Arrange
        var repo = new ValidatedUserRepository(_connection);
        var now = DateTime.UtcNow;

        // Act & Assert
        await Assert.ThrowsExceptionAsync<SqliteException>(async () =>
        {
            await repo.InsertAsync("invalid-email", 25, 100m, null, now);
        });
    }

    [TestMethod]
    public async Task EmailValidation_EmptyEmail_ShouldThrow()
    {
        // Arrange
        var repo = new ValidatedUserRepository(_connection);
        var now = DateTime.UtcNow;

        // Act & Assert
        await Assert.ThrowsExceptionAsync<SqliteException>(async () =>
        {
            await repo.InsertAsync("", 25, 100m, null, now);
        });
    }

    [TestMethod]
    public async Task EmailValidation_SearchByDomain_ShouldFilter()
    {
        // Arrange
        var repo = new ValidatedUserRepository(_connection);
        var now = DateTime.UtcNow;

        await repo.InsertAsync("user1@example.com", 25, 100m, null, now);
        await repo.InsertAsync("user2@example.com", 30, 200m, null, now);
        await repo.InsertAsync("user3@test.com", 35, 300m, null, now);

        // Act
        var exampleCount = await repo.CountByEmailDomainAsync("example.com");
        var testCount = await repo.CountByEmailDomainAsync("test.com");

        // Assert
        Assert.AreEqual(2, exampleCount);
        Assert.AreEqual(1, testCount);
    }

    // ==================== 年龄验证测试 ====================

    [TestMethod]
    public async Task AgeValidation_ValidAge_ShouldInsert()
    {
        // Arrange
        var repo = new ValidatedUserRepository(_connection);
        var now = DateTime.UtcNow;

        // Act
        var userId = await repo.InsertAsync("user@test.com", 25, 100m, null, now);

        // Assert
        Assert.IsTrue(userId > 0);
    }

    [TestMethod]
    public async Task AgeValidation_NegativeAge_ShouldThrow()
    {
        // Arrange
        var repo = new ValidatedUserRepository(_connection);
        var now = DateTime.UtcNow;

        // Act & Assert
        await Assert.ThrowsExceptionAsync<SqliteException>(async () =>
        {
            await repo.InsertAsync("user@test.com", -1, 100m, null, now);
        });
    }

    [TestMethod]
    public async Task AgeValidation_TooOld_ShouldThrow()
    {
        // Arrange
        var repo = new ValidatedUserRepository(_connection);
        var now = DateTime.UtcNow;

        // Act & Assert
        await Assert.ThrowsExceptionAsync<SqliteException>(async () =>
        {
            await repo.InsertAsync("user@test.com", 151, 100m, null, now);
        });
    }

    [TestMethod]
    public async Task AgeValidation_BoundaryValues_ShouldWork()
    {
        // Arrange
        var repo = new ValidatedUserRepository(_connection);
        var now = DateTime.UtcNow;

        // Act
        var id1 = await repo.InsertAsync("user1@test.com", 0, 100m, null, now);    // 最小值
        var id2 = await repo.InsertAsync("user2@test.com", 150, 100m, null, now);  // 最大值

        // Assert
        Assert.IsTrue(id1 > 0);
        Assert.IsTrue(id2 > 0);
    }

    [TestMethod]
    public async Task AgeValidation_RangeQuery_ShouldFilter()
    {
        // Arrange
        var repo = new ValidatedUserRepository(_connection);
        var now = DateTime.UtcNow;

        await repo.InsertAsync("user1@test.com", 18, 100m, null, now);
        await repo.InsertAsync("user2@test.com", 25, 100m, null, now);
        await repo.InsertAsync("user3@test.com", 30, 100m, null, now);
        await repo.InsertAsync("user4@test.com", 40, 100m, null, now);

        // Act
        var youngAdults = await repo.GetByAgeRangeAsync(18, 30);
        var middleAged = await repo.GetByAgeRangeAsync(31, 50);

        // Assert
        Assert.AreEqual(3, youngAdults.Count);
        Assert.AreEqual(1, middleAged.Count);
    }

    // ==================== 余额验证测试 ====================

    [TestMethod]
    public async Task BalanceValidation_PositiveBalance_ShouldInsert()
    {
        // Arrange
        var repo = new ValidatedUserRepository(_connection);
        var now = DateTime.UtcNow;

        // Act
        var userId = await repo.InsertAsync("user@test.com", 25, 1000.50m, null, now);

        // Assert
        Assert.IsTrue(userId > 0);
    }

    [TestMethod]
    public async Task BalanceValidation_ZeroBalance_ShouldInsert()
    {
        // Arrange
        var repo = new ValidatedUserRepository(_connection);
        var now = DateTime.UtcNow;

        // Act
        var userId = await repo.InsertAsync("user@test.com", 25, 0m, null, now);

        // Assert
        Assert.IsTrue(userId > 0);
    }

    [TestMethod]
    public async Task BalanceValidation_NegativeBalance_ShouldThrow()
    {
        // Arrange
        var repo = new ValidatedUserRepository(_connection);
        var now = DateTime.UtcNow;

        // Act & Assert
        await Assert.ThrowsExceptionAsync<SqliteException>(async () =>
        {
            await repo.InsertAsync("user@test.com", 25, -100m, null, now);
        });
    }

    [TestMethod]
    public async Task BalanceValidation_AddBalance_ShouldWork()
    {
        // Arrange
        var repo = new ValidatedUserRepository(_connection);
        var now = DateTime.UtcNow;

        var userId = await repo.InsertAsync("user@test.com", 25, 100m, null, now);

        // Act
        var updated = await repo.AddBalanceAsync(userId, 50m);

        // Assert
        Assert.AreEqual(1, updated);

        var user = await repo.GetByEmailAsync("user@test.com");
        Assert.AreEqual(150m, user.Balance);
    }

    [TestMethod]
    public async Task BalanceValidation_SubtractBalance_WithinLimit_ShouldWork()
    {
        // Arrange
        var repo = new ValidatedUserRepository(_connection);
        var now = DateTime.UtcNow;

        var userId = await repo.InsertAsync("user@test.com", 25, 100m, null, now);

        // Act
        var updated = await repo.AddBalanceAsync(userId, -50m);

        // Assert
        Assert.AreEqual(1, updated);

        var user = await repo.GetByEmailAsync("user@test.com");
        Assert.AreEqual(50m, user.Balance);
    }

    [TestMethod]
    public async Task BalanceValidation_SubtractBalance_ExceedsLimit_ShouldNotUpdate()
    {
        // Arrange
        var repo = new ValidatedUserRepository(_connection);
        var now = DateTime.UtcNow;

        var userId = await repo.InsertAsync("user@test.com", 25, 100m, null, now);

        // Act
        var updated = await repo.AddBalanceAsync(userId, -150m);

        // Assert
        Assert.AreEqual(0, updated); // 不应该更新

        var user = await repo.GetByEmailAsync("user@test.com");
        Assert.AreEqual(100m, user.Balance); // 余额不变
    }

    // ==================== 可选字段测试 ====================

    [TestMethod]
    public async Task OptionalField_WithValue_ShouldStore()
    {
        // Arrange
        var repo = new ValidatedUserRepository(_connection);
        var now = DateTime.UtcNow;

        // Act
        var userId = await repo.InsertAsync("user@test.com", 25, 100m, "1234567890", now);

        // Assert
        var user = await repo.GetByEmailAsync("user@test.com");
        Assert.IsNotNull(user);
        Assert.AreEqual("1234567890", user.PhoneNumber);
    }

    [TestMethod]
    public async Task OptionalField_WithNull_ShouldStore()
    {
        // Arrange
        var repo = new ValidatedUserRepository(_connection);
        var now = DateTime.UtcNow;

        // Act
        var userId = await repo.InsertAsync("user@test.com", 25, 100m, null, now);

        // Assert
        var user = await repo.GetByEmailAsync("user@test.com");
        Assert.IsNotNull(user);
        Assert.IsNull(user.PhoneNumber);
    }

    [TestMethod]
    public async Task OptionalField_FilterByNull_ShouldWork()
    {
        // Arrange
        var repo = new ValidatedUserRepository(_connection);
        var now = DateTime.UtcNow;

        await repo.InsertAsync("user1@test.com", 25, 100m, "1234567890", now);
        await repo.InsertAsync("user2@test.com", 30, 200m, null, now);
        await repo.InsertAsync("user3@test.com", 35, 300m, "0987654321", now);

        // Act
        var usersWithPhone = await repo.GetUsersWithPhoneAsync();

        // Assert
        Assert.AreEqual(2, usersWithPhone.Count);
        Assert.IsTrue(usersWithPhone.All(u => u.PhoneNumber != null));
    }

    // ==================== UNIQUE约束测试 ====================

    [TestMethod]
    public async Task UniqueConstraint_InsertUnique_ShouldWork()
    {
        // Arrange
        var repo = new UniqueEntityRepository(_connection);

        // Act
        var id = await repo.InsertAsync("CODE001", "Value 1");

        // Assert
        Assert.IsTrue(id > 0);

        var entity = await repo.GetByCodeAsync("CODE001");
        Assert.IsNotNull(entity);
        Assert.AreEqual("Value 1", entity.Value);
    }

    [TestMethod]
    public async Task UniqueConstraint_InsertDuplicate_ShouldThrow()
    {
        // Arrange
        var repo = new UniqueEntityRepository(_connection);
        await repo.InsertAsync("CODE001", "Value 1");

        // Act & Assert
        await Assert.ThrowsExceptionAsync<SqliteException>(async () =>
        {
            await repo.InsertAsync("CODE001", "Value 2");
        });
    }

    [TestMethod]
    public async Task UniqueConstraint_CheckExists_ShouldWork()
    {
        // Arrange
        var repo = new UniqueEntityRepository(_connection);
        await repo.InsertAsync("CODE001", "Value 1");

        // Act
        var count1 = await repo.CountByCodeAsync("CODE001");
        var count2 = await repo.CountByCodeAsync("CODE002");

        // Assert
        Assert.AreEqual(1, count1);
        Assert.AreEqual(0, count2);
    }

    // ==================== CHECK约束测试 ====================

    [TestMethod]
    public async Task CheckConstraint_ValidStatus_ShouldInsert()
    {
        // Arrange
        var repo = new CheckConstraintRepository(_connection);

        // Act
        var id1 = await repo.InsertAsync("Active", 10, 99.99m);
        var id2 = await repo.InsertAsync("Inactive", 20, 149.99m);
        var id3 = await repo.InsertAsync("Pending", 30, 199.99m);

        // Assert
        Assert.IsTrue(id1 > 0);
        Assert.IsTrue(id2 > 0);
        Assert.IsTrue(id3 > 0);
    }

    [TestMethod]
    public async Task CheckConstraint_InvalidStatus_ShouldThrow()
    {
        // Arrange
        var repo = new CheckConstraintRepository(_connection);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<SqliteException>(async () =>
        {
            await repo.InsertAsync("Invalid", 10, 99.99m);
        });
    }

    [TestMethod]
    public async Task CheckConstraint_NegativeQuantity_ShouldThrow()
    {
        // Arrange
        var repo = new CheckConstraintRepository(_connection);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<SqliteException>(async () =>
        {
            await repo.InsertAsync("Active", -1, 99.99m);
        });
    }

    [TestMethod]
    public async Task CheckConstraint_ZeroQuantity_ShouldInsert()
    {
        // Arrange
        var repo = new CheckConstraintRepository(_connection);

        // Act
        var id = await repo.InsertAsync("Active", 0, 99.99m);

        // Assert
        Assert.IsTrue(id > 0);
    }

    [TestMethod]
    public async Task CheckConstraint_ZeroPrice_ShouldThrow()
    {
        // Arrange
        var repo = new CheckConstraintRepository(_connection);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<SqliteException>(async () =>
        {
            await repo.InsertAsync("Active", 10, 0m);
        });
    }

    [TestMethod]
    public async Task CheckConstraint_NegativePrice_ShouldThrow()
    {
        // Arrange
        var repo = new CheckConstraintRepository(_connection);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<SqliteException>(async () =>
        {
            await repo.InsertAsync("Active", 10, -99.99m);
        });
    }

    [TestMethod]
    public async Task CheckConstraint_UpdateToInvalidStatus_ShouldThrow()
    {
        // Arrange
        var repo = new CheckConstraintRepository(_connection);
        var id = await repo.InsertAsync("Active", 10, 99.99m);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<SqliteException>(async () =>
        {
            await repo.UpdateStatusAsync(id, "InvalidStatus");
        });
    }

    [TestMethod]
    public async Task CheckConstraint_UpdateToValidStatus_ShouldWork()
    {
        // Arrange
        var repo = new CheckConstraintRepository(_connection);
        var id = await repo.InsertAsync("Active", 10, 99.99m);

        // Act
        var updated = await repo.UpdateStatusAsync(id, "Inactive");

        // Assert
        Assert.AreEqual(1, updated);

        var entity = await repo.GetByIdAsync(id);
        Assert.AreEqual("Inactive", entity.Status);
    }

    [TestMethod]
    public async Task CheckConstraint_UpdateToNegativeQuantity_ShouldThrow()
    {
        // Arrange
        var repo = new CheckConstraintRepository(_connection);
        var id = await repo.InsertAsync("Active", 10, 99.99m);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<SqliteException>(async () =>
        {
            await repo.UpdateQuantityAsync(id, -5);
        });
    }

    // ==================== 复合验证测试 ====================

    [TestMethod]
    public async Task ComplexValidation_AllConstraints_ShouldEnforce()
    {
        // Arrange
        var userRepo = new ValidatedUserRepository(_connection);
        var uniqueRepo = new UniqueEntityRepository(_connection);
        var checkRepo = new CheckConstraintRepository(_connection);
        var now = DateTime.UtcNow;

        // Act & Assert - 用户仓储
        var userId = await userRepo.InsertAsync("valid@test.com", 25, 100m, "1234567890", now);
        Assert.IsTrue(userId > 0);

        await Assert.ThrowsExceptionAsync<SqliteException>(async () =>
        {
            await userRepo.InsertAsync("invalid", 25, 100m, null, now); // 无效邮箱
        });

        // Act & Assert - UNIQUE仓储
        var uniqueId = await uniqueRepo.InsertAsync("UNIQUE001", "Value");
        Assert.IsTrue(uniqueId > 0);

        await Assert.ThrowsExceptionAsync<SqliteException>(async () =>
        {
            await uniqueRepo.InsertAsync("UNIQUE001", "Different Value"); // 重复code
        });

        // Act & Assert - CHECK仓储
        var checkId = await checkRepo.InsertAsync("Active", 10, 99.99m);
        Assert.IsTrue(checkId > 0);

        await Assert.ThrowsExceptionAsync<SqliteException>(async () =>
        {
            await checkRepo.InsertAsync("Invalid", 10, 99.99m); // 无效状态
        });
    }

    [TestMethod]
    public async Task ComplexValidation_TransactionRollback_ShouldRevertAll()
    {
        // Arrange
        var repo = new ValidatedUserRepository(_connection);
        var now = DateTime.UtcNow;

        using var transaction = _connection.BeginTransaction();

        // Act
        var userId = await repo.InsertAsync("user@test.com", 25, 100m, null, now);
        Assert.IsTrue(userId > 0);

        transaction.Rollback();

        // Assert
        var user = await repo.GetByEmailAsync("user@test.com");
        Assert.IsNull(user); // 应该被回滚
    }
}

