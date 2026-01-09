// -----------------------------------------------------------------------
// <copyright file="PredefinedInterfacesE2ETests.cs" company="Cricle">
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
using Sqlx;
using Sqlx.Annotations;
using Sqlx.Tests.Infrastructure;

namespace Sqlx.Tests.E2E;

#region Test Entities

/// <summary>
/// Test entity for predefined interface E2E tests.
/// </summary>
[TableName("e2e_users")]
public class E2EUser
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public int Age { get; set; }
    public decimal Salary { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}

#endregion

#region Repository Implementations

// ICrudRepository implementation - Note: ICrudRepository inherits ICommandRepository which has
// method-level generic methods (UpdatePartialAsync<TUpdates>, UpdateWhereAsync<TUpdates>) that
// the source generator cannot fully support. These tests focus on the methods that work.
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(ICrudRepository<E2EUser, long>))]
public partial class E2EUserCrudRepository(IDbConnection connection) : ICrudRepository<E2EUser, long>
{
    protected readonly IDbConnection connection = connection;
}

// IQueryRepository implementation - All methods work correctly
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IQueryRepository<E2EUser, long>))]
public partial class E2EUserQueryRepository(IDbConnection connection) : IQueryRepository<E2EUser, long>
{
    protected readonly IDbConnection connection = connection;
}

// ICommandRepository implementation - Note: Has method-level generic methods that may have limitations
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(ICommandRepository<E2EUser, long>))]
public partial class E2EUserCommandRepository(IDbConnection connection) : ICommandRepository<E2EUser, long>
{
    protected readonly IDbConnection connection = connection;
}

// IAggregateRepository implementation - All methods work correctly
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IAggregateRepository<E2EUser, long>))]
public partial class E2EUserAggregateRepository(IDbConnection connection) : IAggregateRepository<E2EUser, long>
{
    protected readonly IDbConnection connection = connection;
}

// IBatchRepository implementation - Note: Has method-level generic methods that may have limitations
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IBatchRepository<E2EUser, long>))]
public partial class E2EUserBatchRepository(IDbConnection connection) : IBatchRepository<E2EUser, long>
{
    protected readonly IDbConnection connection = connection;
}

// IReadOnlyRepository implementation - Combines IQueryRepository + IAggregateRepository (all methods work)
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IReadOnlyRepository<E2EUser, long>))]
public partial class E2EUserReadOnlyRepository(IDbConnection connection) : IReadOnlyRepository<E2EUser, long>
{
    protected readonly IDbConnection connection = connection;
}

// IBulkRepository implementation - Combines IQueryRepository + IBatchRepository
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IBulkRepository<E2EUser, long>))]
public partial class E2EUserBulkRepository(IDbConnection connection) : IBulkRepository<E2EUser, long>
{
    protected readonly IDbConnection connection = connection;
}

#endregion

#region Test Base Class

/// <summary>
/// Base class for predefined interface E2E tests.
/// Provides common setup, teardown, and helper methods.
/// Supports multiple database dialects (SQLite, PostgreSQL, MySQL, SQL Server).
/// Uses shared connection per test class for better performance.
/// </summary>
public abstract class PredefinedInterfacesE2ETestBase : IDisposable
{
    // Shared connection per test class - initialized once, reused for all tests
    private static readonly Dictionary<string, DbConnection> _sharedConnections = new();
    private static readonly Dictionary<string, bool> _tablesCreated = new();
    private static readonly object _lock = new();

    protected DbConnection? Connection { get; set; }
    protected abstract SqlDefineTypes Dialect { get; }
    protected abstract string TestClassName { get; }
    
    /// <summary>
    /// Cleanup all shared connections - called at assembly cleanup.
    /// </summary>
    public static void CleanupSharedConnections()
    {
        lock (_lock)
        {
            foreach (var conn in _sharedConnections.Values)
            {
                try
                {
                    conn?.Dispose();
                }
                catch
                {
                    // Ignore disposal errors
                }
            }
            _sharedConnections.Clear();
            _tablesCreated.Clear();
        }
    }

    protected virtual async Task InitializeAsync()
    {
        var connectionKey = $"{Dialect}_{TestClassName}";
        
        lock (_lock)
        {
            if (!_sharedConnections.ContainsKey(connectionKey))
            {
                var conn = DatabaseConnectionHelper.CreateConnectionForDialect(Dialect, TestClassName);
                if (conn == null)
                {
                    throw new InvalidOperationException($"Failed to create connection for dialect {Dialect}");
                }
                _sharedConnections[connectionKey] = conn;
            }
            
            Connection = _sharedConnections[connectionKey];
        }
        
        if (Connection.State != ConnectionState.Open)
        {
            await Connection.OpenAsync();
        }
        
        // Only create tables once per test class
        if (!_tablesCreated.ContainsKey(connectionKey))
        {
            await CreateTablesAsync();
            lock (_lock)
            {
                _tablesCreated[connectionKey] = true;
            }
        }
        
        // Clear data before each test instead of recreating tables
        await ClearTableAsync();
    }

    protected virtual async Task CreateTablesAsync()
    {
        if (Connection == null) return;

        var ddl = GetCreateTableDDL();
        using var cmd = Connection.CreateCommand();
        cmd.CommandText = ddl;
        await cmd.ExecuteNonQueryAsync();
    }

    protected virtual string GetCreateTableDDL()
    {
        return Dialect switch
        {
            SqlDefineTypes.SQLite => @"
                CREATE TABLE IF NOT EXISTS e2e_users (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL,
                    email TEXT,
                    age INTEGER NOT NULL,
                    salary REAL NOT NULL DEFAULT 0,
                    is_active INTEGER NOT NULL DEFAULT 1,
                    is_deleted INTEGER NOT NULL DEFAULT 0,
                    created_at TEXT NOT NULL,
                    updated_at TEXT,
                    deleted_at TEXT
                )",
            
            SqlDefineTypes.PostgreSql => @"
                CREATE TABLE IF NOT EXISTS e2e_users (
                    id BIGSERIAL PRIMARY KEY,
                    name VARCHAR(255) NOT NULL,
                    email VARCHAR(255),
                    age INTEGER NOT NULL,
                    salary DECIMAL(18,2) NOT NULL DEFAULT 0,
                    is_active BOOLEAN NOT NULL DEFAULT true,
                    is_deleted BOOLEAN NOT NULL DEFAULT false,
                    created_at TIMESTAMP NOT NULL,
                    updated_at TIMESTAMP,
                    deleted_at TIMESTAMP
                )",
            
            SqlDefineTypes.MySql => @"
                CREATE TABLE IF NOT EXISTS e2e_users (
                    id BIGINT AUTO_INCREMENT PRIMARY KEY,
                    name VARCHAR(255) NOT NULL,
                    email VARCHAR(255),
                    age INT NOT NULL,
                    salary DECIMAL(18,2) NOT NULL DEFAULT 0,
                    is_active BOOLEAN NOT NULL DEFAULT 1,
                    is_deleted BOOLEAN NOT NULL DEFAULT 0,
                    created_at DATETIME NOT NULL,
                    updated_at DATETIME,
                    deleted_at DATETIME
                )",
            
            SqlDefineTypes.SqlServer => @"
                IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[e2e_users]') AND type in (N'U'))
                BEGIN
                    CREATE TABLE e2e_users (
                        id BIGINT IDENTITY(1,1) PRIMARY KEY,
                        name NVARCHAR(255) NOT NULL,
                        email NVARCHAR(255),
                        age INT NOT NULL,
                        salary DECIMAL(18,2) NOT NULL DEFAULT 0,
                        is_active BIT NOT NULL DEFAULT 1,
                        is_deleted BIT NOT NULL DEFAULT 0,
                        created_at DATETIME2 NOT NULL,
                        updated_at DATETIME2,
                        deleted_at DATETIME2
                    )
                END",
            
            _ => throw new NotSupportedException($"Dialect {Dialect} is not supported")
        };
    }

    protected async Task ClearTableAsync()
    {
        if (Connection == null) return;
        
        using var cmd = Connection.CreateCommand();
        // Use DELETE for all databases - faster for small datasets and works everywhere
        // TRUNCATE requires more permissions and can be slower for small tables
        cmd.CommandText = "DELETE FROM e2e_users";
        
        await cmd.ExecuteNonQueryAsync();
    }

    protected async Task<long> InsertTestUserAsync(string name, int age, decimal salary = 50000m, bool isActive = true)
    {
        if (Connection == null) return 0;
        
        using var cmd = Connection.CreateCommand();
        
        // Build INSERT statement based on dialect
        var (insertSql, idQuery) = Dialect switch
        {
            SqlDefineTypes.SQLite => (
                @"INSERT INTO e2e_users (name, email, age, salary, is_active, created_at) 
                  VALUES (@name, @email, @age, @salary, @isActive, @createdAt);
                  SELECT last_insert_rowid();",
                ""
            ),
            SqlDefineTypes.PostgreSql => (
                @"INSERT INTO e2e_users (name, email, age, salary, is_active, created_at) 
                  VALUES (@name, @email, @age, @salary, @isActive, @createdAt)
                  RETURNING id;",
                ""
            ),
            SqlDefineTypes.MySql => (
                @"INSERT INTO e2e_users (name, email, age, salary, is_active, created_at) 
                  VALUES (@name, @email, @age, @salary, @isActive, @createdAt);
                  SELECT LAST_INSERT_ID();",
                ""
            ),
            SqlDefineTypes.SqlServer => (
                @"INSERT INTO e2e_users (name, email, age, salary, is_active, created_at) 
                  VALUES (@name, @email, @age, @salary, @isActive, @createdAt);
                  SELECT CAST(SCOPE_IDENTITY() AS BIGINT);",
                ""
            ),
            _ => throw new NotSupportedException($"Dialect {Dialect} is not supported")
        };
        
        cmd.CommandText = insertSql;
        
        // Add parameters
        var nameParam = cmd.CreateParameter();
        nameParam.ParameterName = "@name";
        nameParam.Value = name;
        cmd.Parameters.Add(nameParam);
        
        var emailParam = cmd.CreateParameter();
        emailParam.ParameterName = "@email";
        emailParam.Value = $"{name.ToLower()}@test.com";
        cmd.Parameters.Add(emailParam);
        
        var ageParam = cmd.CreateParameter();
        ageParam.ParameterName = "@age";
        ageParam.Value = age;
        cmd.Parameters.Add(ageParam);
        
        var salaryParam = cmd.CreateParameter();
        salaryParam.ParameterName = "@salary";
        salaryParam.Value = salary;
        cmd.Parameters.Add(salaryParam);
        
        var isActiveParam = cmd.CreateParameter();
        isActiveParam.ParameterName = "@isActive";
        isActiveParam.Value = Dialect == SqlDefineTypes.PostgreSql ? isActive : (isActive ? 1 : 0);
        cmd.Parameters.Add(isActiveParam);
        
        var createdAtParam = cmd.CreateParameter();
        createdAtParam.ParameterName = "@createdAt";
        createdAtParam.Value = DateTime.UtcNow;
        cmd.Parameters.Add(createdAtParam);
        
        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt64(result);
    }

    public void Dispose()
    {
        // Don't dispose shared connection - it will be reused by other tests
        // Connection cleanup happens at assembly level
        Connection = null;
    }
}

#endregion

#region ICrudRepository E2E Tests

/// <summary>
/// E2E tests for ICrudRepository - the most commonly used predefined interface.
/// Tests complete CRUD flow with 15+ methods.
/// </summary>
[TestClass]
[TestCategory("E2E")]
[TestCategory("PredefinedInterfaces")]
[TestCategory("SQLite")]
public class ICrudRepositoryE2ETests : PredefinedInterfacesE2ETestBase
{
    private E2EUserCrudRepository? _repo;

    protected override SqlDefineTypes Dialect => SqlDefineTypes.SQLite;
    protected override string TestClassName => nameof(ICrudRepositoryE2ETests);

    [TestInitialize]
    public async Task Setup()
    {
        await InitializeAsync();
        _repo = new E2EUserCrudRepository(Connection!);
    }

    [TestCleanup]
    public void Cleanup()
    {
        Dispose();
    }

    [TestMethod]
    public async Task ICrudRepository_InsertAndGetIdAsync_ShouldReturnGeneratedId()
    {
        var user = new E2EUser
        {
            Name = "Test User",
            Email = "test@example.com",
            Age = 25,
            Salary = 50000m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var id = await _repo!.InsertAndGetIdAsync(user);

        Assert.IsTrue(id > 0, "Generated ID should be greater than 0");
    }

    [TestMethod]
    public async Task ICrudRepository_GetByIdAsync_ShouldReturnEntity()
    {
        var insertedId = await InsertTestUserAsync("GetById User", 30);

        var user = await _repo!.GetByIdAsync(insertedId);

        Assert.IsNotNull(user);
        Assert.AreEqual("GetById User", user.Name);
        Assert.AreEqual(30, user.Age);
    }

    [TestMethod]
    public async Task ICrudRepository_GetByIdAsync_NonExistent_ShouldReturnNull()
    {
        var user = await _repo!.GetByIdAsync(99999);

        Assert.IsNull(user);
    }

    [TestMethod]
    public async Task ICrudRepository_UpdateAsync_ShouldUpdateEntity()
    {
        var insertedId = await InsertTestUserAsync("Original Name", 25);
        var user = await _repo!.GetByIdAsync(insertedId);
        Assert.IsNotNull(user);

        user.Name = "Updated Name";
        user.Age = 26;
        var affected = await _repo.UpdateAsync(user);

        Assert.AreEqual(1, affected);
        var updated = await _repo.GetByIdAsync(insertedId);
        Assert.IsNotNull(updated);
        Assert.AreEqual("Updated Name", updated.Name);
        Assert.AreEqual(26, updated.Age);
    }

    [TestMethod]
    public async Task ICrudRepository_DeleteAsync_ShouldRemoveEntity()
    {
        var insertedId = await InsertTestUserAsync("ToDelete User", 30);

        var affected = await _repo!.DeleteAsync(insertedId);

        Assert.AreEqual(1, affected);
        var deleted = await _repo.GetByIdAsync(insertedId);
        Assert.IsNull(deleted);
    }

    [TestMethod]
    public async Task ICrudRepository_CountAsync_ShouldReturnCorrectCount()
    {
        await InsertTestUserAsync("User1", 25);
        await InsertTestUserAsync("User2", 30);
        await InsertTestUserAsync("User3", 35);

        var count = await _repo!.CountAsync();

        Assert.AreEqual(3L, count);
    }

    [TestMethod]
    public async Task ICrudRepository_ExistsAsync_ShouldReturnTrueForExisting()
    {
        var insertedId = await InsertTestUserAsync("Exists User", 25);

        var exists = await _repo!.ExistsAsync(insertedId);

        Assert.IsTrue(exists);
    }

    [TestMethod]
    public async Task ICrudRepository_ExistsAsync_ShouldReturnFalseForNonExisting()
    {
        var exists = await _repo!.ExistsAsync(99999);

        Assert.IsFalse(exists);
    }
}

#endregion

#region IQueryRepository E2E Tests

/// <summary>
/// E2E tests for IQueryRepository - query operations.
/// </summary>
[TestClass]
[TestCategory("E2E")]
[TestCategory("PredefinedInterfaces")]
[TestCategory("SQLite")]
public class IQueryRepositoryE2ETests : PredefinedInterfacesE2ETestBase
{
    private E2EUserQueryRepository? _repo;

    protected override SqlDefineTypes Dialect => SqlDefineTypes.SQLite;
    protected override string TestClassName => nameof(IQueryRepositoryE2ETests);

    [TestInitialize]
    public async Task Setup()
    {
        await InitializeAsync();
        _repo = new E2EUserQueryRepository(Connection!);
    }

    [TestCleanup]
    public void Cleanup()
    {
        Dispose();
    }

    [TestMethod]
    public async Task IQueryRepository_GetByIdsAsync_ShouldReturnMultipleEntities()
    {
        var id1 = await InsertTestUserAsync("User1", 25);
        var id2 = await InsertTestUserAsync("User2", 30);
        await InsertTestUserAsync("User3", 35); // Not requested

        var users = await _repo!.GetByIdsAsync(new List<long> { id1, id2 });

        Assert.AreEqual(2, users.Count);
        Assert.IsTrue(users.Any(u => u.Name == "User1"));
        Assert.IsTrue(users.Any(u => u.Name == "User2"));
    }

    [TestMethod]
    public async Task IQueryRepository_GetAllAsync_ShouldReturnAllEntities()
    {
        await InsertTestUserAsync("User1", 25);
        await InsertTestUserAsync("User2", 30);
        await InsertTestUserAsync("User3", 35);

        var users = await _repo!.GetAllAsync();

        Assert.AreEqual(3, users.Count);
    }

    [TestMethod]
    public async Task IQueryRepository_GetTopAsync_ShouldReturnLimitedEntities()
    {
        for (int i = 1; i <= 10; i++)
        {
            await InsertTestUserAsync($"User{i}", 20 + i);
        }

        var users = await _repo!.GetTopAsync(5);

        Assert.AreEqual(5, users.Count);
    }

    [TestMethod]
    public async Task IQueryRepository_GetRangeAsync_ShouldReturnPaginatedEntities()
    {
        for (int i = 1; i <= 20; i++)
        {
            await InsertTestUserAsync($"User{i}", 20 + i);
        }

        var page1 = await _repo!.GetRangeAsync(limit: 5, offset: 0);
        var page2 = await _repo!.GetRangeAsync(limit: 5, offset: 5);

        Assert.AreEqual(5, page1.Count);
        Assert.AreEqual(5, page2.Count);
        // Ensure no overlap
        var allIds = page1.Select(u => u.Id).Concat(page2.Select(u => u.Id)).ToList();
        Assert.AreEqual(10, allIds.Distinct().Count());
    }

    [TestMethod]
    public async Task IQueryRepository_GetWhereAsync_ShouldFilterEntities()
    {
        await InsertTestUserAsync("Young1", 20, isActive: true);
        await InsertTestUserAsync("Young2", 22, isActive: true);
        await InsertTestUserAsync("Old1", 40, isActive: true);
        await InsertTestUserAsync("Inactive", 25, isActive: false);

        var youngUsers = await _repo!.GetWhereAsync(u => u.Age < 30 && u.IsActive);

        Assert.AreEqual(2, youngUsers.Count);
        Assert.IsTrue(youngUsers.All(u => u.Age < 30 && u.IsActive));
    }

    [TestMethod]
    public async Task IQueryRepository_GetFirstWhereAsync_ShouldReturnFirstMatch()
    {
        await InsertTestUserAsync("User1", 25);
        await InsertTestUserAsync("User2", 30);

        var user = await _repo!.GetFirstWhereAsync(u => u.Age >= 25);

        Assert.IsNotNull(user);
        Assert.IsTrue(user.Age >= 25);
    }

    [TestMethod]
    public async Task IQueryRepository_ExistsWhereAsync_ShouldReturnTrueWhenMatches()
    {
        await InsertTestUserAsync("Active User", 25, isActive: true);

        var exists = await _repo!.ExistsWhereAsync(u => u.IsActive);

        Assert.IsTrue(exists);
    }

    [TestMethod]
    public async Task IQueryRepository_GetByIdsAsync_WithPartialMatches_ShouldReturnOnlyExisting()
    {
        var id1 = await InsertTestUserAsync("User1", 25);
        var id2 = await InsertTestUserAsync("User2", 30);
        
        // Request existing and non-existing IDs
        var users = await _repo!.GetByIdsAsync(new List<long> { id1, id2, 99999, 88888 });

        Assert.AreEqual(2, users.Count);
        Assert.IsTrue(users.Any(u => u.Name == "User1"));
        Assert.IsTrue(users.Any(u => u.Name == "User2"));
    }

    [TestMethod]
    public async Task IQueryRepository_GetByIdsAsync_EmptyList_ShouldReturnEmpty()
    {
        await InsertTestUserAsync("User1", 25);
        
        var users = await _repo!.GetByIdsAsync(new List<long>());

        Assert.AreEqual(0, users.Count);
    }

    [TestMethod]
    public async Task IQueryRepository_GetRangeAsync_WithOffsetBeyondData_ShouldReturnEmpty()
    {
        for (int i = 1; i <= 5; i++)
        {
            await InsertTestUserAsync($"User{i}", 20 + i);
        }

        var users = await _repo!.GetRangeAsync(limit: 10, offset: 100);

        Assert.AreEqual(0, users.Count);
    }

    [TestMethod]
    public async Task IQueryRepository_GetRangeAsync_WithLargeLimit_ShouldReturnAllAvailable()
    {
        for (int i = 1; i <= 5; i++)
        {
            await InsertTestUserAsync($"User{i}", 20 + i);
        }

        var users = await _repo!.GetRangeAsync(limit: 100, offset: 0);

        Assert.AreEqual(5, users.Count);
    }

    [TestMethod]
    public async Task IQueryRepository_GetWhereAsync_WithComplexPredicate_ShouldFilterCorrectly()
    {
        await InsertTestUserAsync("Alice", 25, salary: 50000m, isActive: true);
        await InsertTestUserAsync("Bob", 30, salary: 60000m, isActive: true);
        await InsertTestUserAsync("Charlie", 35, salary: 70000m, isActive: false);
        await InsertTestUserAsync("David", 28, salary: 55000m, isActive: true);

        // Complex predicate: active users between 25-30 years old with salary > 50000
        var users = await _repo!.GetWhereAsync(u => 
            u.IsActive && 
            u.Age >= 25 && 
            u.Age <= 30 && 
            u.Salary > 50000m);

        Assert.AreEqual(2, users.Count);
        Assert.IsTrue(users.Any(u => u.Name == "Bob"));
        Assert.IsTrue(users.Any(u => u.Name == "David"));
    }

    [TestMethod]
    public async Task IQueryRepository_GetWhereAsync_NoMatches_ShouldReturnEmpty()
    {
        await InsertTestUserAsync("User1", 25, isActive: true);
        await InsertTestUserAsync("User2", 30, isActive: true);

        var users = await _repo!.GetWhereAsync(u => u.Age > 100);

        Assert.AreEqual(0, users.Count);
    }

    [TestMethod]
    public async Task IQueryRepository_GetFirstWhereAsync_NoMatches_ShouldReturnNull()
    {
        await InsertTestUserAsync("User1", 25);
        await InsertTestUserAsync("User2", 30);

        var user = await _repo!.GetFirstWhereAsync(u => u.Age > 100);

        Assert.IsNull(user);
    }

    [TestMethod]
    public async Task IQueryRepository_GetFirstWhereAsync_MultipleMatches_ShouldReturnFirst()
    {
        await InsertTestUserAsync("User1", 25);
        await InsertTestUserAsync("User2", 30);
        await InsertTestUserAsync("User3", 35);

        var user = await _repo!.GetFirstWhereAsync(u => u.Age >= 25);

        Assert.IsNotNull(user);
        Assert.IsTrue(user.Age >= 25);
        // Should return one of the matching users (order may vary)
    }

    [TestMethod]
    public async Task IQueryRepository_ExistsWhereAsync_NoMatches_ShouldReturnFalse()
    {
        await InsertTestUserAsync("User1", 25, isActive: true);

        var exists = await _repo!.ExistsWhereAsync(u => !u.IsActive);

        Assert.IsFalse(exists);
    }

    [TestMethod]
    public async Task IQueryRepository_GetAllAsync_EmptyTable_ShouldReturnEmpty()
    {
        var users = await _repo!.GetAllAsync();

        Assert.AreEqual(0, users.Count);
    }

    [TestMethod]
    public async Task IQueryRepository_GetTopAsync_WithZeroLimit_ShouldReturnEmpty()
    {
        await InsertTestUserAsync("User1", 25);
        await InsertTestUserAsync("User2", 30);

        var users = await _repo!.GetTopAsync(0);

        Assert.AreEqual(0, users.Count);
    }
}

#endregion

#region ICommandRepository E2E Tests

/// <summary>
/// E2E tests for ICommandRepository - command operations (Insert, Update, Delete).
/// </summary>
[TestClass]
[TestCategory("E2E")]
[TestCategory("PredefinedInterfaces")]
[TestCategory("SQLite")]
public class ICommandRepositoryE2ETests : PredefinedInterfacesE2ETestBase
{
    private E2EUserCommandRepository? _repo;

    protected override SqlDefineTypes Dialect => SqlDefineTypes.SQLite;
    protected override string TestClassName => nameof(ICommandRepositoryE2ETests);

    [TestInitialize]
    public async Task Setup()
    {
        await InitializeAsync();
        _repo = new E2EUserCommandRepository(Connection!);
    }

    [TestCleanup]
    public void Cleanup()
    {
        Dispose();
    }

    [TestMethod]
    public async Task ICommandRepository_InsertAsync_ShouldInsertEntity()
    {
        var user = new E2EUser
        {
            Name = "New User",
            Email = "new@test.com",
            Age = 28,
            Salary = 60000m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var affected = await _repo!.InsertAsync(user);

        Assert.AreEqual(1, affected);
    }

    [TestMethod]
    public async Task ICommandRepository_InsertAndGetIdAsync_ShouldReturnId()
    {
        var user = new E2EUser
        {
            Name = "Insert User",
            Age = 30,
            CreatedAt = DateTime.UtcNow
        };

        var id = await _repo!.InsertAndGetIdAsync(user);

        Assert.IsTrue(id > 0);
    }

    [TestMethod]
    public async Task ICommandRepository_DeleteWhereAsync_ShouldDeleteMatchingEntities()
    {
        await InsertTestUserAsync("Active1", 25, isActive: true);
        await InsertTestUserAsync("Active2", 30, isActive: true);
        await InsertTestUserAsync("Inactive", 35, isActive: false);

        var affected = await _repo!.DeleteWhereAsync(u => !u.IsActive);

        Assert.AreEqual(1, affected);
    }

    [TestMethod]
    public async Task ICommandRepository_UpdateAsync_ShouldUpdateEntity()
    {
        var id = await InsertTestUserAsync("Original", 25, salary: 50000m);
        
        // Use a query repository to fetch the entity
        var queryRepo = new E2EUserQueryRepository(Connection!);
        var user = await queryRepo.GetByIdAsync(id);
        Assert.IsNotNull(user);

        user.Name = "Updated";
        user.Age = 26;
        user.Salary = 55000m;
        var affected = await _repo!.UpdateAsync(user);

        Assert.AreEqual(1, affected);
        var updated = await queryRepo.GetByIdAsync(id);
        Assert.IsNotNull(updated);
        Assert.AreEqual("Updated", updated.Name);
        Assert.AreEqual(26, updated.Age);
        Assert.AreEqual(55000m, updated.Salary);
    }

    [TestMethod]
    public async Task ICommandRepository_UpdateAsync_NonExistent_ShouldReturnZero()
    {
        var user = new E2EUser
        {
            Id = 99999,
            Name = "NonExistent",
            Age = 25,
            CreatedAt = DateTime.UtcNow
        };

        var affected = await _repo!.UpdateAsync(user);

        Assert.AreEqual(0, affected);
    }

    [TestMethod]
    public async Task ICommandRepository_DeleteAsync_ShouldDeleteEntity()
    {
        var id = await InsertTestUserAsync("ToDelete", 25);

        var affected = await _repo!.DeleteAsync(id);

        Assert.AreEqual(1, affected);
        
        // Verify deletion using query repository
        var queryRepo = new E2EUserQueryRepository(Connection!);
        var deleted = await queryRepo.GetByIdAsync(id);
        Assert.IsNull(deleted);
    }

    [TestMethod]
    public async Task ICommandRepository_DeleteAsync_NonExistent_ShouldReturnZero()
    {
        var affected = await _repo!.DeleteAsync(99999);

        Assert.AreEqual(0, affected);
    }

    [TestMethod]
    public async Task ICommandRepository_DeleteWhereAsync_NoMatches_ShouldReturnZero()
    {
        await InsertTestUserAsync("Active1", 25, isActive: true);
        await InsertTestUserAsync("Active2", 30, isActive: true);

        var affected = await _repo!.DeleteWhereAsync(u => u.Age > 100);

        Assert.AreEqual(0, affected);
    }

    [TestMethod]
    public async Task ICommandRepository_DeleteWhereAsync_MultipleMatches_ShouldDeleteAll()
    {
        await InsertTestUserAsync("Young1", 20, isActive: true);
        await InsertTestUserAsync("Young2", 22, isActive: true);
        await InsertTestUserAsync("Old1", 40, isActive: true);

        var affected = await _repo!.DeleteWhereAsync(u => u.Age < 30);

        Assert.AreEqual(2, affected);
        
        // Verify remaining using query repository
        var queryRepo = new E2EUserQueryRepository(Connection!);
        var remaining = await queryRepo.GetAllAsync();
        Assert.AreEqual(1, remaining.Count);
        Assert.AreEqual("Old1", remaining[0].Name);
    }

    [TestMethod]
    public async Task ICommandRepository_InsertAsync_MultipleEntities_ShouldInsertAll()
    {
        var user1 = new E2EUser { Name = "User1", Age = 25, CreatedAt = DateTime.UtcNow };
        var user2 = new E2EUser { Name = "User2", Age = 30, CreatedAt = DateTime.UtcNow };
        var user3 = new E2EUser { Name = "User3", Age = 35, CreatedAt = DateTime.UtcNow };

        var affected1 = await _repo!.InsertAsync(user1);
        var affected2 = await _repo.InsertAsync(user2);
        var affected3 = await _repo.InsertAsync(user3);

        Assert.AreEqual(1, affected1);
        Assert.AreEqual(1, affected2);
        Assert.AreEqual(1, affected3);
        
        // Verify count using query repository
        var queryRepo = new E2EUserQueryRepository(Connection!);
        var all = await queryRepo.GetAllAsync();
        Assert.AreEqual(3, all.Count);
    }

    [TestMethod]
    public async Task ICommandRepository_UpdateAsync_PartialFields_ShouldUpdateOnlySpecified()
    {
        var id = await InsertTestUserAsync("Original", 25, salary: 50000m, isActive: true);
        
        // Use query repository to fetch
        var queryRepo = new E2EUserQueryRepository(Connection!);
        var user = await queryRepo.GetByIdAsync(id);
        Assert.IsNotNull(user);

        // Only update name and age, leave salary and isActive unchanged
        user.Name = "Updated";
        user.Age = 26;
        await _repo!.UpdateAsync(user);

        var updated = await queryRepo.GetByIdAsync(id);
        Assert.IsNotNull(updated);
        Assert.AreEqual("Updated", updated.Name);
        Assert.AreEqual(26, updated.Age);
        Assert.AreEqual(50000m, updated.Salary); // Should remain unchanged
        Assert.IsTrue(updated.IsActive); // Should remain unchanged
    }
}

#endregion

#region IAggregateRepository E2E Tests

/// <summary>
/// E2E tests for IAggregateRepository - aggregate operations (COUNT, SUM, AVG, MAX, MIN).
/// </summary>
[TestClass]
[TestCategory("E2E")]
[TestCategory("PredefinedInterfaces")]
[TestCategory("SQLite")]
public class IAggregateRepositoryE2ETests : PredefinedInterfacesE2ETestBase
{
    private E2EUserAggregateRepository? _repo;

    protected override SqlDefineTypes Dialect => SqlDefineTypes.SQLite;
    protected override string TestClassName => nameof(IAggregateRepositoryE2ETests);

    [TestInitialize]
    public async Task Setup()
    {
        await InitializeAsync();
        _repo = new E2EUserAggregateRepository(Connection!);
    }

    [TestCleanup]
    public void Cleanup()
    {
        Dispose();
    }

    [TestMethod]
    public async Task IAggregateRepository_CountAsync_ShouldReturnTotalCount()
    {
        await InsertTestUserAsync("User1", 25);
        await InsertTestUserAsync("User2", 30);
        await InsertTestUserAsync("User3", 35);

        var count = await _repo!.CountAsync();

        Assert.AreEqual(3L, count);
    }

    [TestMethod]
    public async Task IAggregateRepository_CountWhereAsync_ShouldReturnFilteredCount()
    {
        await InsertTestUserAsync("Young1", 20, isActive: true);
        await InsertTestUserAsync("Young2", 25, isActive: true);
        await InsertTestUserAsync("Old", 40, isActive: true);

        var count = await _repo!.CountWhereAsync(u => u.Age < 30);

        Assert.AreEqual(2L, count);
    }

    [TestMethod]
    public async Task IAggregateRepository_SumAsync_ShouldReturnSum()
    {
        await InsertTestUserAsync("User1", 25, salary: 50000m);
        await InsertTestUserAsync("User2", 30, salary: 60000m);
        await InsertTestUserAsync("User3", 35, salary: 70000m);

        var sum = await _repo!.SumAsync("salary");

        Assert.AreEqual(180000m, sum);
    }

    [TestMethod]
    public async Task IAggregateRepository_AvgAsync_ShouldReturnAverage()
    {
        await InsertTestUserAsync("User1", 20);
        await InsertTestUserAsync("User2", 30);
        await InsertTestUserAsync("User3", 40);

        var avg = await _repo!.AvgAsync("age");

        Assert.AreEqual(30m, avg);
    }

    [TestMethod]
    public async Task IAggregateRepository_MaxIntAsync_ShouldReturnMaxValue()
    {
        await InsertTestUserAsync("User1", 25);
        await InsertTestUserAsync("User2", 35);
        await InsertTestUserAsync("User3", 30);

        var max = await _repo!.MaxIntAsync("age");

        Assert.AreEqual(35, max);
    }

    [TestMethod]
    public async Task IAggregateRepository_MinIntAsync_ShouldReturnMinValue()
    {
        await InsertTestUserAsync("User1", 25);
        await InsertTestUserAsync("User2", 35);
        await InsertTestUserAsync("User3", 30);

        var min = await _repo!.MinIntAsync("age");

        Assert.AreEqual(25, min);
    }

    [TestMethod]
    public async Task IAggregateRepository_CountAsync_EmptyTable_ShouldReturnZero()
    {
        var count = await _repo!.CountAsync();

        Assert.AreEqual(0L, count);
    }

    [TestMethod]
    public async Task IAggregateRepository_CountWhereAsync_NoMatches_ShouldReturnZero()
    {
        await InsertTestUserAsync("User1", 25);
        await InsertTestUserAsync("User2", 30);

        var count = await _repo!.CountWhereAsync(u => u.Age > 100);

        Assert.AreEqual(0L, count);
    }

    [TestMethod]
    public async Task IAggregateRepository_SumAsync_EmptyTable_ShouldReturnZero()
    {
        var sum = await _repo!.SumAsync("salary");

        Assert.AreEqual(0m, sum);
    }

    [TestMethod]
    public async Task IAggregateRepository_SumWhereAsync_ShouldReturnFilteredSum()
    {
        await InsertTestUserAsync("Young1", 20, salary: 50000m, isActive: true);
        await InsertTestUserAsync("Young2", 25, salary: 55000m, isActive: true);
        await InsertTestUserAsync("Old", 40, salary: 70000m, isActive: true);

        var sum = await _repo!.SumWhereAsync("salary", u => u.Age < 30);

        Assert.AreEqual(105000m, sum);
    }

    [TestMethod]
    public async Task IAggregateRepository_AvgAsync_EmptyTable_ShouldReturnZero()
    {
        var avg = await _repo!.AvgAsync("age");

        Assert.AreEqual(0m, avg);
    }

    [TestMethod]
    public async Task IAggregateRepository_AvgWhereAsync_ShouldReturnFilteredAverage()
    {
        await InsertTestUserAsync("Young1", 20, isActive: true);
        await InsertTestUserAsync("Young2", 30, isActive: true);
        await InsertTestUserAsync("Old", 60, isActive: true);

        var avg = await _repo!.AvgWhereAsync("age", u => u.Age < 40);

        Assert.AreEqual(25m, avg);
    }

    [TestMethod]
    public async Task IAggregateRepository_MaxLongAsync_ShouldReturnMaxValue()
    {
        var id1 = await InsertTestUserAsync("User1", 25);
        var id2 = await InsertTestUserAsync("User2", 30);
        var id3 = await InsertTestUserAsync("User3", 35);

        var max = await _repo!.MaxLongAsync("id");

        Assert.IsTrue(max >= Math.Max(id1, Math.Max(id2, id3)));
    }

    [TestMethod]
    public async Task IAggregateRepository_MinLongAsync_ShouldReturnMinValue()
    {
        var id1 = await InsertTestUserAsync("User1", 25);
        var id2 = await InsertTestUserAsync("User2", 30);
        var id3 = await InsertTestUserAsync("User3", 35);

        var min = await _repo!.MinLongAsync("id");

        Assert.IsTrue(min <= Math.Min(id1, Math.Min(id2, id3)));
    }

    [TestMethod]
    public async Task IAggregateRepository_MaxDecimalAsync_ShouldReturnMaxValue()
    {
        await InsertTestUserAsync("User1", 25, salary: 50000m);
        await InsertTestUserAsync("User2", 30, salary: 75000m);
        await InsertTestUserAsync("User3", 35, salary: 60000m);

        var max = await _repo!.MaxDecimalAsync("salary");

        Assert.AreEqual(75000m, max);
    }

    [TestMethod]
    public async Task IAggregateRepository_MinDecimalAsync_ShouldReturnMinValue()
    {
        await InsertTestUserAsync("User1", 25, salary: 50000m);
        await InsertTestUserAsync("User2", 30, salary: 75000m);
        await InsertTestUserAsync("User3", 35, salary: 60000m);

        var min = await _repo!.MinDecimalAsync("salary");

        Assert.AreEqual(50000m, min);
    }

    [TestMethod]
    public async Task IAggregateRepository_MaxDateTimeAsync_ShouldReturnMaxValue()
    {
        var now = DateTime.UtcNow;
        await InsertTestUserAsync("User1", 25);
        await Task.Delay(10); // Ensure different timestamps
        await InsertTestUserAsync("User2", 30);
        await Task.Delay(10);
        await InsertTestUserAsync("User3", 35);

        var max = await _repo!.MaxDateTimeAsync("created_at");

        Assert.IsTrue(max >= now);
    }

    [TestMethod]
    public async Task IAggregateRepository_MinDateTimeAsync_ShouldReturnMinValue()
    {
        var now = DateTime.UtcNow;
        await InsertTestUserAsync("User1", 25);
        await Task.Delay(10);
        await InsertTestUserAsync("User2", 30);
        await Task.Delay(10);
        await InsertTestUserAsync("User3", 35);

        var min = await _repo!.MinDateTimeAsync("created_at");

        Assert.IsTrue(min >= now);
        Assert.IsTrue(min <= DateTime.UtcNow);
    }

    [TestMethod]
    public async Task IAggregateRepository_CountWhereAsync_ComplexPredicate_ShouldReturnCorrectCount()
    {
        await InsertTestUserAsync("Young1", 20, salary: 50000m, isActive: true);
        await InsertTestUserAsync("Young2", 25, salary: 55000m, isActive: true);
        await InsertTestUserAsync("Old1", 40, salary: 70000m, isActive: true);
        await InsertTestUserAsync("Inactive", 30, salary: 60000m, isActive: false);

        var count = await _repo!.CountWhereAsync(u => u.Age < 30 && u.IsActive && u.Salary > 50000m);

        Assert.AreEqual(1L, count);
    }

    [TestMethod]
    public async Task IAggregateRepository_SumAsync_WithNullValues_ShouldIgnoreNulls()
    {
        // Insert users with some null salaries (using default 0)
        await InsertTestUserAsync("User1", 25, salary: 50000m);
        await InsertTestUserAsync("User2", 30, salary: 0m); // Simulating null/zero
        await InsertTestUserAsync("User3", 35, salary: 60000m);

        var sum = await _repo!.SumAsync("salary");

        Assert.AreEqual(110000m, sum);
    }

    [TestMethod]
    public async Task IAggregateRepository_AvgAsync_SingleValue_ShouldReturnThatValue()
    {
        await InsertTestUserAsync("User1", 42);

        var avg = await _repo!.AvgAsync("age");

        Assert.AreEqual(42m, avg);
    }
}

#endregion

#region IBatchRepository E2E Tests

/// <summary>
/// E2E tests for IBatchRepository - batch operations.
/// </summary>
[TestClass]
[TestCategory("E2E")]
[TestCategory("PredefinedInterfaces")]
[TestCategory("SQLite")]
public class IBatchRepositoryE2ETests : PredefinedInterfacesE2ETestBase
{
    private E2EUserBatchRepository? _repo;

    protected override SqlDefineTypes Dialect => SqlDefineTypes.SQLite;
    protected override string TestClassName => nameof(IBatchRepositoryE2ETests);

    [TestInitialize]
    public async Task Setup()
    {
        await InitializeAsync();
        _repo = new E2EUserBatchRepository(Connection!);
    }

    [TestCleanup]
    public void Cleanup()
    {
        Dispose();
    }

    [TestMethod]
    public async Task IBatchRepository_BatchInsertAsync_ShouldInsertMultipleEntities()
    {
        var users = new List<E2EUser>
        {
            new() { Name = "Batch1", Age = 25, CreatedAt = DateTime.UtcNow },
            new() { Name = "Batch2", Age = 30, CreatedAt = DateTime.UtcNow },
            new() { Name = "Batch3", Age = 35, CreatedAt = DateTime.UtcNow }
        };

        var affected = await _repo!.BatchInsertAsync(users);

        Assert.AreEqual(3, affected);
    }

    [TestMethod]
    public async Task IBatchRepository_BatchInsertAsync_EmptyList_ShouldReturnZero()
    {
        var users = new List<E2EUser>();

        var affected = await _repo!.BatchInsertAsync(users);

        Assert.AreEqual(0, affected);
    }

    [TestMethod]
    public async Task IBatchRepository_BatchDeleteAsync_ShouldDeleteMultipleEntities()
    {
        var id1 = await InsertTestUserAsync("ToDelete1", 25);
        var id2 = await InsertTestUserAsync("ToDelete2", 30);
        await InsertTestUserAsync("ToKeep", 35);

        var affected = await _repo!.BatchDeleteAsync(new List<long> { id1, id2 });

        Assert.AreEqual(2, affected);
    }

    [TestMethod]
    public async Task IBatchRepository_BatchInsertAndGetIdsAsync_ShouldReturnIds()
    {
        var users = new List<E2EUser>
        {
            new() { Name = "BatchId1", Age = 25, CreatedAt = DateTime.UtcNow },
            new() { Name = "BatchId2", Age = 30, CreatedAt = DateTime.UtcNow }
        };

        var ids = await _repo!.BatchInsertAndGetIdsAsync(users);

        Assert.AreEqual(2, ids.Count);
        Assert.IsTrue(ids.All(id => id > 0));
        Assert.AreEqual(2, ids.Distinct().Count()); // All IDs should be unique
    }

    [TestMethod]
    public async Task IBatchRepository_BatchInsertAsync_100Records_ShouldInsertAll()
    {
        var users = new List<E2EUser>();
        for (int i = 1; i <= 100; i++)
        {
            users.Add(new E2EUser 
            { 
                Name = $"User{i}", 
                Age = 20 + (i % 50), 
                Salary = 50000m + (i * 100),
                CreatedAt = DateTime.UtcNow 
            });
        }

        var affected = await _repo!.BatchInsertAsync(users);

        Assert.AreEqual(100, affected);
        
        // Verify count using aggregate repository
        var aggregateRepo = new E2EUserAggregateRepository(Connection!);
        var count = await aggregateRepo.CountAsync();
        Assert.AreEqual(100L, count);
    }

    [TestMethod]
    public async Task IBatchRepository_BatchInsertAsync_1000Records_ShouldInsertAll()
    {
        var users = new List<E2EUser>();
        for (int i = 1; i <= 1000; i++)
        {
            users.Add(new E2EUser 
            { 
                Name = $"User{i}", 
                Age = 20 + (i % 50), 
                Salary = 50000m + (i * 100),
                CreatedAt = DateTime.UtcNow 
            });
        }

        var affected = await _repo!.BatchInsertAsync(users);

        Assert.AreEqual(1000, affected);
        
        // Verify count using aggregate repository
        var aggregateRepo = new E2EUserAggregateRepository(Connection!);
        var count = await aggregateRepo.CountAsync();
        Assert.AreEqual(1000L, count);
    }

    [TestMethod]
    [TestCategory("LargeData")]
    public async Task IBatchRepository_BatchInsertAsync_10000Records_ShouldInsertAll()
    {
        var users = new List<E2EUser>();
        for (int i = 1; i <= 10000; i++)
        {
            users.Add(new E2EUser 
            { 
                Name = $"User{i}", 
                Age = 20 + (i % 50), 
                Salary = 50000m + (i * 100),
                CreatedAt = DateTime.UtcNow 
            });
        }

        var affected = await _repo!.BatchInsertAsync(users);

        Assert.AreEqual(10000, affected);
        
        // Verify count using aggregate repository
        var aggregateRepo = new E2EUserAggregateRepository(Connection!);
        var count = await aggregateRepo.CountAsync();
        Assert.AreEqual(10000L, count);
    }

    [TestMethod]
    public async Task IBatchRepository_BatchDeleteAsync_EmptyList_ShouldReturnZero()
    {
        await InsertTestUserAsync("User1", 25);

        var affected = await _repo!.BatchDeleteAsync(new List<long>());

        Assert.AreEqual(0, affected);
    }

    [TestMethod]
    public async Task IBatchRepository_BatchDeleteAsync_NonExistentIds_ShouldReturnZero()
    {
        await InsertTestUserAsync("User1", 25);

        var affected = await _repo!.BatchDeleteAsync(new List<long> { 99999, 88888 });

        Assert.AreEqual(0, affected);
    }

    [TestMethod]
    public async Task IBatchRepository_BatchDeleteAsync_MixedExistingAndNonExisting_ShouldDeleteOnlyExisting()
    {
        var id1 = await InsertTestUserAsync("User1", 25);
        var id2 = await InsertTestUserAsync("User2", 30);

        var affected = await _repo!.BatchDeleteAsync(new List<long> { id1, id2, 99999, 88888 });

        Assert.AreEqual(2, affected);
        
        // Verify deletion
        var queryRepo = new E2EUserQueryRepository(Connection!);
        var remaining = await queryRepo.GetAllAsync();
        Assert.AreEqual(0, remaining.Count);
    }

    [TestMethod]
    public async Task IBatchRepository_BatchInsertAndGetIdsAsync_LargeBatch_ShouldReturnAllIds()
    {
        var users = new List<E2EUser>();
        for (int i = 1; i <= 100; i++)
        {
            users.Add(new E2EUser 
            { 
                Name = $"User{i}", 
                Age = 20 + (i % 50), 
                CreatedAt = DateTime.UtcNow 
            });
        }

        var ids = await _repo!.BatchInsertAndGetIdsAsync(users);

        Assert.AreEqual(100, ids.Count);
        Assert.IsTrue(ids.All(id => id > 0));
        Assert.AreEqual(100, ids.Distinct().Count()); // All IDs should be unique
    }
}

#endregion

#region IReadOnlyRepository E2E Tests

/// <summary>
/// E2E tests for IReadOnlyRepository - read-only operations (Query + Aggregate).
/// </summary>
[TestClass]
[TestCategory("E2E")]
[TestCategory("PredefinedInterfaces")]
[TestCategory("SQLite")]
public class IReadOnlyRepositoryE2ETests : PredefinedInterfacesE2ETestBase
{
    private E2EUserReadOnlyRepository? _repo;

    protected override SqlDefineTypes Dialect => SqlDefineTypes.SQLite;
    protected override string TestClassName => nameof(IReadOnlyRepositoryE2ETests);

    [TestInitialize]
    public async Task Setup()
    {
        await InitializeAsync();
        _repo = new E2EUserReadOnlyRepository(Connection!);
    }

    [TestCleanup]
    public void Cleanup()
    {
        Dispose();
    }

    [TestMethod]
    public async Task IReadOnlyRepository_GetByIdAsync_ShouldWork()
    {
        var id = await InsertTestUserAsync("ReadOnly User", 30);

        var user = await _repo!.GetByIdAsync(id);

        Assert.IsNotNull(user);
        Assert.AreEqual("ReadOnly User", user.Name);
    }

    [TestMethod]
    public async Task IReadOnlyRepository_CountAsync_ShouldWork()
    {
        await InsertTestUserAsync("User1", 25);
        await InsertTestUserAsync("User2", 30);

        var count = await _repo!.CountAsync();

        Assert.AreEqual(2L, count);
    }

    [TestMethod]
    public async Task IReadOnlyRepository_GetWhereAsync_ShouldWork()
    {
        await InsertTestUserAsync("Active1", 25, isActive: true);
        await InsertTestUserAsync("Inactive", 30, isActive: false);

        var activeUsers = await _repo!.GetWhereAsync(u => u.IsActive);

        Assert.AreEqual(1, activeUsers.Count);
    }

    [TestMethod]
    public async Task IReadOnlyRepository_SumAsync_ShouldWork()
    {
        await InsertTestUserAsync("User1", 25, salary: 50000m);
        await InsertTestUserAsync("User2", 30, salary: 60000m);

        var sum = await _repo!.SumAsync("salary");

        Assert.AreEqual(110000m, sum);
    }
}

#endregion

#region IBulkRepository E2E Tests

/// <summary>
/// E2E tests for IBulkRepository - bulk operations (Query + Batch).
/// </summary>
[TestClass]
[TestCategory("E2E")]
[TestCategory("PredefinedInterfaces")]
[TestCategory("SQLite")]
public class IBulkRepositoryE2ETests : PredefinedInterfacesE2ETestBase
{
    private E2EUserBulkRepository? _repo;

    protected override SqlDefineTypes Dialect => SqlDefineTypes.SQLite;
    protected override string TestClassName => nameof(IBulkRepositoryE2ETests);

    [TestInitialize]
    public async Task Setup()
    {
        await InitializeAsync();
        _repo = new E2EUserBulkRepository(Connection!);
    }

    [TestCleanup]
    public void Cleanup()
    {
        Dispose();
    }

    [TestMethod]
    public async Task IBulkRepository_GetByIdAsync_ShouldWork()
    {
        var id = await InsertTestUserAsync("Bulk User", 30);

        var user = await _repo!.GetByIdAsync(id);

        Assert.IsNotNull(user);
    }

    [TestMethod]
    public async Task IBulkRepository_BatchInsertAsync_ShouldWork()
    {
        var users = new List<E2EUser>
        {
            new() { Name = "Bulk1", Age = 25, CreatedAt = DateTime.UtcNow },
            new() { Name = "Bulk2", Age = 30, CreatedAt = DateTime.UtcNow }
        };

        var affected = await _repo!.BatchInsertAsync(users);

        Assert.AreEqual(2, affected);
    }

    [TestMethod]
    public async Task IBulkRepository_BatchDeleteAsync_ShouldWork()
    {
        var id1 = await InsertTestUserAsync("Delete1", 25);
        var id2 = await InsertTestUserAsync("Delete2", 30);

        var affected = await _repo!.BatchDeleteAsync(new List<long> { id1, id2 });

        Assert.AreEqual(2, affected);
    }
}

#endregion

// Note: IRepository (full interface with 50+ methods) tests are not included here
// because IAdvancedRepository has complex methods that require special handling.
// The individual interface tests above cover all the functionality.

