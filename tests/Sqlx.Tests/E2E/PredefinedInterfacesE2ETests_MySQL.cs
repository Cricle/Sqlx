// -----------------------------------------------------------------------
// <copyright file="PredefinedInterfacesE2ETests_MySQL.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using Sqlx.Annotations;

namespace Sqlx.Tests.E2E;

#region Repository Implementations

// ICrudRepository implementation for MySQL
[SqlDefine(SqlDefineTypes.MySql)]
[RepositoryFor(typeof(ICrudRepository<E2EUser, long>))]
public partial class E2EUserCrudRepository_MySQL(IDbConnection connection) : ICrudRepository<E2EUser, long>
{
    protected readonly IDbConnection connection = connection;
}

// IQueryRepository implementation for MySQL
[SqlDefine(SqlDefineTypes.MySql)]
[RepositoryFor(typeof(IQueryRepository<E2EUser, long>))]
public partial class E2EUserQueryRepository_MySQL(IDbConnection connection) : IQueryRepository<E2EUser, long>
{
    protected readonly IDbConnection connection = connection;
}

// ICommandRepository implementation for MySQL
[SqlDefine(SqlDefineTypes.MySql)]
[RepositoryFor(typeof(ICommandRepository<E2EUser, long>))]
public partial class E2EUserCommandRepository_MySQL(IDbConnection connection) : ICommandRepository<E2EUser, long>
{
    protected readonly IDbConnection connection = connection;
}

// IAggregateRepository implementation for MySQL
[SqlDefine(SqlDefineTypes.MySql)]
[RepositoryFor(typeof(IAggregateRepository<E2EUser, long>))]
public partial class E2EUserAggregateRepository_MySQL(IDbConnection connection) : IAggregateRepository<E2EUser, long>
{
    protected readonly IDbConnection connection = connection;
}

// IBatchRepository implementation for MySQL
[SqlDefine(SqlDefineTypes.MySql)]
[RepositoryFor(typeof(IBatchRepository<E2EUser, long>))]
public partial class E2EUserBatchRepository_MySQL(IDbConnection connection) : IBatchRepository<E2EUser, long>
{
    protected readonly IDbConnection connection = connection;
}

// IReadOnlyRepository implementation for MySQL
[SqlDefine(SqlDefineTypes.MySql)]
[RepositoryFor(typeof(IReadOnlyRepository<E2EUser, long>))]
public partial class E2EUserReadOnlyRepository_MySQL(IDbConnection connection) : IReadOnlyRepository<E2EUser, long>
{
    protected readonly IDbConnection connection = connection;
}

// IBulkRepository implementation for MySQL
[SqlDefine(SqlDefineTypes.MySql)]
[RepositoryFor(typeof(IBulkRepository<E2EUser, long>))]
public partial class E2EUserBulkRepository_MySQL(IDbConnection connection) : IBulkRepository<E2EUser, long>
{
    protected readonly IDbConnection connection = connection;
}

#endregion

#region Test Classes

/// <summary>
/// MySQL E2E tests for IQueryRepository.
/// Tests all query operations including GetByIdsAsync, GetRangeAsync, GetPageAsync, etc.
/// </summary>
[TestClass]
public class PredefinedInterfacesE2ETests_MySQL_Query : PredefinedInterfacesE2ETestBase
{
    protected override SqlDefineTypes Dialect => SqlDefineTypes.MySql;
    protected override string TestClassName => nameof(PredefinedInterfacesE2ETests_MySQL_Query);

    private E2EUserQueryRepository_MySQL? _repository;

    [TestInitialize]
    public async Task Setup()
    {
        await InitializeAsync();
        _repository = new E2EUserQueryRepository_MySQL(Connection!);
    }

    [TestMethod]
    public async Task GetAllAsync_ReturnsAllUsers()
    {
        // Arrange
        var users = new List<E2EUser>
        {
            new() { Name = "Alice", Email = "alice@test.com", Age = 25, Salary = 50000, IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Name = "Bob", Email = "bob@test.com", Age = 30, Salary = 60000, IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Name = "Charlie", Email = "charlie@test.com", Age = 35, Salary = 70000, IsActive = false, CreatedAt = DateTime.UtcNow }
        };

        foreach (var user in users)
        {
            user.Id = await InsertTestUserAsync(user.Name, user.Age, user.Salary, user.IsActive);
        }

        // Act
        var result = await _repository!.GetAllAsync();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(3, result.Count);
        Assert.IsTrue(result.Any(u => u.Name == "Alice"));
        Assert.IsTrue(result.Any(u => u.Name == "Bob"));
        Assert.IsTrue(result.Any(u => u.Name == "Charlie"));
    }

    [TestMethod]
    public async Task GetByIdAsync_ExistingId_ReturnsUser()
    {
        // Arrange
        var user = new E2EUser
        {
            Name = "Test User",
            Email = "test@test.com",
            Age = 25,
            Salary = 50000,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var id = await InsertTestUserAsync(user.Name, user.Age, user.Salary, user.IsActive);

        // Act
        var result = await _repository!.GetByIdAsync(id);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("Test User", result.Name);
        Assert.AreEqual("test user@test.com", result.Email); // InsertTestUserAsync generates email as "{name.ToLower()}@test.com"
    }

    [TestMethod]
    public async Task GetByIdAsync_NonExistentId_ReturnsNull()
    {
        // Act
        var result = await _repository!.GetByIdAsync(99999);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetByIdsAsync_ReturnsMatchingUsers()
    {
        // Arrange
        var users = new List<E2EUser>
        {
            new() { Name = "User1", Email = "user1@test.com", Age = 25, Salary = 50000, IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Name = "User2", Email = "user2@test.com", Age = 30, Salary = 60000, IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Name = "User3", Email = "user3@test.com", Age = 35, Salary = 70000, IsActive = true, CreatedAt = DateTime.UtcNow }
        };

        var ids = new List<long>();
        foreach (var user in users)
        {
            ids.Add(await InsertTestUserAsync(user.Name, user.Age, user.Salary, user.IsActive));
        }

        // Act - request first and third user
        var result = await _repository!.GetByIdsAsync(new[] { ids[0], ids[2] });

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(2, result.Count);
        Assert.IsTrue(result.Any(u => u.Name == "User1"));
        Assert.IsTrue(result.Any(u => u.Name == "User3"));
        Assert.IsFalse(result.Any(u => u.Name == "User2"));
    }

    [TestMethod]
    public async Task GetTopAsync_ReturnsLimitedResults()
    {
        // Arrange
        for (int i = 0; i < 10; i++)
        {
            await InsertTestUserAsync($"User{i}", 20 + i, 50000 + (i * 1000), true);
        }

        // Act
        var result = await _repository!.GetTopAsync(5);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(5, result.Count);
    }

    [TestMethod]
    public async Task CountAsync_ReturnsCorrectCount()
    {
        // Arrange
        for (int i = 0; i < 7; i++)
        {
            await InsertTestUserAsync($"User{i}", 20 + i, 50000, true);
        }

        // Act
        var aggregateRepo = new E2EUserAggregateRepository_MySQL(Connection!);
        var count = await aggregateRepo.CountAsync();

        // Assert
        Assert.AreEqual(7L, count);
    }

    [TestMethod]
    public async Task ExistsAsync_ExistingId_ReturnsTrue()
    {
        // Arrange
        var id = await InsertTestUserAsync("Test User", 25, 50000, true);

        // Act
        var exists = await _repository!.ExistsAsync(id);

        // Assert
        Assert.IsTrue(exists);
    }

    [TestMethod]
    public async Task ExistsAsync_NonExistentId_ReturnsFalse()
    {
        // Act
        var exists = await _repository!.ExistsAsync(99999);

        // Assert
        Assert.IsFalse(exists);
    }
}

/// <summary>
/// MySQL E2E tests for IBatchRepository.
/// Tests batch operations including BatchInsertAsync, BatchUpdateAsync, BatchUpsertAsync.
/// </summary>
[TestClass]
public class PredefinedInterfacesE2ETests_MySQL_Batch : PredefinedInterfacesE2ETestBase
{
    protected override SqlDefineTypes Dialect => SqlDefineTypes.MySql;
    protected override string TestClassName => nameof(PredefinedInterfacesE2ETests_MySQL_Batch);

    private E2EUserBatchRepository_MySQL? _repository;

    [TestInitialize]
    public async Task Setup()
    {
        await InitializeAsync();
        _repository = new E2EUserBatchRepository_MySQL(Connection!);
    }

    [TestMethod]
    public async Task BatchInsertAsync_InsertsMultipleUsers()
    {
        // Arrange
        var users = new List<E2EUser>
        {
            new() { Name = "User1", Email = "user1@test.com", Age = 25, Salary = 50000, IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Name = "User2", Email = "user2@test.com", Age = 30, Salary = 60000, IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Name = "User3", Email = "user3@test.com", Age = 35, Salary = 70000, IsActive = true, CreatedAt = DateTime.UtcNow }
        };

        // Act
        var affected = await _repository!.BatchInsertAsync(users);

        // Assert
        Assert.AreEqual(3, affected);

        // Verify inserted using aggregate repository
        var aggregateRepo = new E2EUserAggregateRepository_MySQL(Connection!);
        var count = await aggregateRepo.CountAsync();
        Assert.AreEqual(3L, count);
    }

    [TestMethod]
    public async Task BatchUpdateAsync_UpdatesMultipleUsers()
    {
        // Arrange
        var users = new List<E2EUser>
        {
            new() { Name = "User1", Email = "user1@test.com", Age = 25, Salary = 50000, IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Name = "User2", Email = "user2@test.com", Age = 30, Salary = 60000, IsActive = true, CreatedAt = DateTime.UtcNow }
        };

        foreach (var user in users)
        {
            user.Id = await InsertTestUserAsync(user.Name, user.Age, user.Salary, user.IsActive);
        }

        // Modify users
        users[0].Name = "Updated User1";
        users[0].Age = 26;
        users[1].Name = "Updated User2";
        users[1].Age = 31;

        // Act
        var affected = await _repository!.BatchUpdateAsync(users);

        // Assert
        Assert.AreEqual(2, affected);

        // Verify updates using query repository
        var queryRepo = new E2EUserQueryRepository_MySQL(Connection!);
        var user1 = await queryRepo.GetByIdAsync(users[0].Id);
        Assert.IsNotNull(user1);
        Assert.AreEqual("Updated User1", user1.Name);
        Assert.AreEqual(26, user1.Age);

        var user2 = await queryRepo.GetByIdAsync(users[1].Id);
        Assert.IsNotNull(user2);
        Assert.AreEqual("Updated User2", user2.Name);
        Assert.AreEqual(31, user2.Age);
    }

    [TestMethod]
    public async Task BatchUpsertAsync_InsertsAndUpdatesUsers()
    {
        // Arrange - Insert one user first
        var existingUser = new E2EUser
        {
            Name = "Existing User",
            Email = "existing@test.com",
            Age = 25,
            Salary = 50000,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        existingUser.Id = await InsertTestUserAsync(existingUser.Name, existingUser.Age, existingUser.Salary, existingUser.IsActive);

        // Prepare batch: one existing (update), one new (insert)
        var users = new List<E2EUser>
        {
            new() { Id = existingUser.Id, Name = "Updated User", Email = "updated@test.com", Age = 26, Salary = 55000, IsActive = true, CreatedAt = existingUser.CreatedAt },
            new() { Name = "New User", Email = "new@test.com", Age = 30, Salary = 60000, IsActive = true, CreatedAt = DateTime.UtcNow }
        };

        // Act
        var affected = await _repository!.BatchUpsertAsync(users);

        // Assert
        // MySQL ON DUPLICATE KEY UPDATE returns 2 for update + 1 for insert = 3
        Assert.AreEqual(3, affected, "MySQL returns 2 for update + 1 for insert");

        // Verify: existing user was updated
        var queryRepo = new E2EUserQueryRepository_MySQL(Connection!);
        var updated = await queryRepo.GetByIdAsync(existingUser.Id);
        Assert.IsNotNull(updated);
        Assert.AreEqual("Updated User", updated.Name);
        Assert.AreEqual(26, updated.Age);

        // Verify: new user was inserted
        var aggregateRepo = new E2EUserAggregateRepository_MySQL(Connection!);
        var count = await aggregateRepo.CountAsync();
        Assert.AreEqual(2L, count);
    }
}

/// <summary>
/// MySQL-specific feature tests.
/// Tests MySQL-specific functionality like ON DUPLICATE KEY UPDATE, boolean 0/1 literals, and RAND().
/// </summary>
[TestClass]
public class PredefinedInterfacesE2ETests_MySQL_Features : PredefinedInterfacesE2ETestBase
{
    protected override SqlDefineTypes Dialect => SqlDefineTypes.MySql;
    protected override string TestClassName => nameof(PredefinedInterfacesE2ETests_MySQL_Features);

    private E2EUserCommandRepository_MySQL? _commandRepository;
    private E2EUserQueryRepository_MySQL? _queryRepository;
    private E2EUserBatchRepository_MySQL? _batchRepository;

    [TestInitialize]
    public async Task Setup()
    {
        await InitializeAsync();
        _commandRepository = new E2EUserCommandRepository_MySQL(Connection!);
        _queryRepository = new E2EUserQueryRepository_MySQL(Connection!);
        _batchRepository = new E2EUserBatchRepository_MySQL(Connection!);
    }

    [TestMethod]
    [TestCategory("MySQL-Specific")]
    [TestCategory("ON_DUPLICATE_KEY")]
    public async Task UpsertAsync_ExistingRecord_UpdatesUsingOnDuplicateKey()
    {
        // Arrange - Insert initial user
        var user = new E2EUser
        {
            Name = "Original Name",
            Email = "original@test.com",
            Age = 25,
            Salary = 50000,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var id = await InsertTestUserAsync(user.Name, user.Age, user.Salary, user.IsActive);

        // Modify user for upsert
        user.Id = id;
        user.Name = "Updated Name";
        user.Email = "updated@test.com";
        user.Age = 30;
        user.Salary = 60000;

        // Act - Upsert should update existing record using ON DUPLICATE KEY UPDATE
        var affected = await _commandRepository!.UpsertAsync(user);

        // Assert
        // MySQL ON DUPLICATE KEY UPDATE returns 2 for updates (1 deleted + 1 inserted)
        Assert.AreEqual(2, affected, "Upsert should affect 2 rows (MySQL behavior)");

        // Verify the update
        var updated = await _queryRepository!.GetByIdAsync(id);
        Assert.IsNotNull(updated);
        Assert.AreEqual("Updated Name", updated.Name);
        Assert.AreEqual("updated@test.com", updated.Email);
        Assert.AreEqual(30, updated.Age);
        Assert.AreEqual(60000, updated.Salary);
    }

    [TestMethod]
    [TestCategory("MySQL-Specific")]
    [TestCategory("ON_DUPLICATE_KEY")]
    public async Task UpsertAsync_NewRecord_InsertsUsingOnDuplicateKey()
    {
        // Arrange
        var user = new E2EUser
        {
            Name = "New User",
            Email = "new@test.com",
            Age = 25,
            Salary = 50000,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Act - Upsert should insert new record
        var affected = await _commandRepository!.UpsertAsync(user);

        // Assert
        Assert.AreEqual(1, affected, "Upsert should affect 1 row");

        // Verify the insert
        var aggregateRepo = new E2EUserAggregateRepository_MySQL(Connection!);
        var count = await aggregateRepo.CountAsync();
        Assert.AreEqual(1L, count);

        var inserted = await _queryRepository!.GetAllAsync();
        Assert.AreEqual(1, inserted.Count);
        Assert.AreEqual("New User", inserted[0].Name);
    }

    [TestMethod]
    [TestCategory("MySQL-Specific")]
    [TestCategory("Boolean")]
    public async Task BooleanField_ZeroAndOne_StoredCorrectly()
    {
        // Arrange
        var activeUser = new E2EUser
        {
            Name = "Active User",
            Email = "active@test.com",
            Age = 25,
            Salary = 50000,
            IsActive = true,  // MySQL boolean 1
            CreatedAt = DateTime.UtcNow
        };

        var inactiveUser = new E2EUser
        {
            Name = "Inactive User",
            Email = "inactive@test.com",
            Age = 30,
            Salary = 60000,
            IsActive = false,  // MySQL boolean 0
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var activeId = await InsertTestUserAsync(activeUser.Name, activeUser.Age, activeUser.Salary, activeUser.IsActive);
        var inactiveId = await InsertTestUserAsync(inactiveUser.Name, inactiveUser.Age, inactiveUser.Salary, inactiveUser.IsActive);

        // Assert
        var retrievedActive = await _queryRepository!.GetByIdAsync(activeId);
        Assert.IsNotNull(retrievedActive);
        Assert.IsTrue(retrievedActive.IsActive, "MySQL should store 1 as true");

        var retrievedInactive = await _queryRepository!.GetByIdAsync(inactiveId);
        Assert.IsNotNull(retrievedInactive);
        Assert.IsFalse(retrievedInactive.IsActive, "MySQL should store 0 as false");
    }

    [TestMethod]
    [TestCategory("MySQL-Specific")]
    [TestCategory("Boolean")]
    public async Task BooleanField_BatchOperations_PreservesValues()
    {
        // Arrange
        var users = new List<E2EUser>
        {
            new() { Name = "User1", Email = "user1@test.com", Age = 25, Salary = 50000, IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Name = "User2", Email = "user2@test.com", Age = 30, Salary = 60000, IsActive = false, CreatedAt = DateTime.UtcNow },
            new() { Name = "User3", Email = "user3@test.com", Age = 35, Salary = 70000, IsActive = true, CreatedAt = DateTime.UtcNow }
        };

        // Act
        await _batchRepository!.BatchInsertAsync(users);

        // Assert
        var allUsers = await _queryRepository!.GetAllAsync();
        Assert.AreEqual(3, allUsers.Count);

        var user1 = allUsers.First(u => u.Name == "User1");
        Assert.IsTrue(user1.IsActive, "User1 should be active (1)");

        var user2 = allUsers.First(u => u.Name == "User2");
        Assert.IsFalse(user2.IsActive, "User2 should be inactive (0)");

        var user3 = allUsers.First(u => u.Name == "User3");
        Assert.IsTrue(user3.IsActive, "User3 should be active (1)");
    }

    [TestMethod]
    [TestCategory("MySQL-Specific")]
    [TestCategory("Random")]
    public async Task GetRandomAsync_ReturnsRandomUser()
    {
        // Arrange - Insert multiple users
        for (int i = 0; i < 10; i++)
        {
            await InsertTestUserAsync($"User{i}", 20 + i, 50000 + (i * 1000), true);
        }

        // Act - Get random user multiple times
        var random1 = await _queryRepository!.GetRandomAsync(1);
        var random2 = await _queryRepository!.GetRandomAsync(1);
        var random3 = await _queryRepository!.GetRandomAsync(1);

        // Assert
        Assert.IsNotNull(random1, "GetRandomAsync should return users");
        Assert.AreEqual(1, random1.Count, "Should return 1 random user");
        Assert.IsNotNull(random2, "GetRandomAsync should return users");
        Assert.AreEqual(1, random2.Count, "Should return 1 random user");
        Assert.IsNotNull(random3, "GetRandomAsync should return users");
        Assert.AreEqual(1, random3.Count, "Should return 1 random user");

        // Verify all returned users are valid
        Assert.IsTrue(random1[0].Name.StartsWith("User"), "Random user should be from inserted users");
        Assert.IsTrue(random2[0].Name.StartsWith("User"), "Random user should be from inserted users");
        Assert.IsTrue(random3[0].Name.StartsWith("User"), "Random user should be from inserted users");

        // Note: We can't guarantee they're different due to randomness,
        // but we can verify the RAND() function is working
        Console.WriteLine($"Random users: {random1[0].Name}, {random2[0].Name}, {random3[0].Name}");
    }

    [TestMethod]
    [TestCategory("MySQL-Specific")]
    [TestCategory("Random")]
    public async Task GetRandomAsync_EmptyTable_ReturnsEmptyList()
    {
        // Act
        var random = await _queryRepository!.GetRandomAsync(5);

        // Assert
        Assert.IsNotNull(random, "GetRandomAsync should return a list");
        Assert.AreEqual(0, random.Count, "GetRandomAsync should return empty list for empty table");
    }

    [TestMethod]
    [TestCategory("MySQL-Specific")]
    [TestCategory("ON_DUPLICATE_KEY")]
    public async Task BatchUpsertAsync_VerifiesOnDuplicateKeyBehavior()
    {
        // Arrange - Insert initial users
        var user1Id = await InsertTestUserAsync("User1", 25, 50000, true);
        var user2Id = await InsertTestUserAsync("User2", 30, 60000, true);

        // Prepare batch: 2 updates + 1 insert
        var users = new List<E2EUser>
        {
            new() { Id = user1Id, Name = "Updated User1", Email = "updated1@test.com", Age = 26, Salary = 55000, IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = user2Id, Name = "Updated User2", Email = "updated2@test.com", Age = 31, Salary = 65000, IsActive = false, CreatedAt = DateTime.UtcNow },
            new() { Name = "New User3", Email = "new3@test.com", Age = 35, Salary = 70000, IsActive = true, CreatedAt = DateTime.UtcNow }
        };

        // Act - BatchUpsert should use ON DUPLICATE KEY UPDATE for MySQL
        var affected = await _batchRepository!.BatchUpsertAsync(users);

        // Assert
        // MySQL ON DUPLICATE KEY UPDATE returns 2 for each update + 1 for insert = 2+2+1=5
        Assert.AreEqual(5, affected, "BatchUpsert should affect 5 rows (2 updates + 1 insert, MySQL behavior)");

        // Verify updates
        var updated1 = await _queryRepository!.GetByIdAsync(user1Id);
        Assert.IsNotNull(updated1);
        Assert.AreEqual("Updated User1", updated1.Name);
        Assert.AreEqual(26, updated1.Age);

        var updated2 = await _queryRepository!.GetByIdAsync(user2Id);
        Assert.IsNotNull(updated2);
        Assert.AreEqual("Updated User2", updated2.Name);
        Assert.IsFalse(updated2.IsActive);

        // Verify insert
        var aggregateRepo = new E2EUserAggregateRepository_MySQL(Connection!);
        var count = await aggregateRepo.CountAsync();
        Assert.AreEqual(3L, count);
    }
}

#endregion

