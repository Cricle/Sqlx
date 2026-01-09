// -----------------------------------------------------------------------
// <copyright file="IExpressionUpdateRepositoryE2ETests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using Sqlx.Annotations;
using Sqlx.Tests.Infrastructure;

namespace Sqlx.Tests.E2E;

#region Test Entity

/// <summary>
/// Test entity for IExpressionUpdateRepository E2E tests.
/// </summary>
[TableName("expr_update_users")]
public class ExprUpdateUser
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public int Age { get; set; }
    public decimal Salary { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

#endregion

#region Repository Implementations

// SQLite
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IExpressionUpdateRepository<ExprUpdateUser, long>))]
public partial class ExprUpdateUserRepository_SQLite(IDbConnection connection) 
    : IExpressionUpdateRepository<ExprUpdateUser, long>
{
    protected readonly IDbConnection connection = connection;
}

// MySQL
[SqlDefine(SqlDefineTypes.MySql)]
[RepositoryFor(typeof(IExpressionUpdateRepository<ExprUpdateUser, long>))]
public partial class ExprUpdateUserRepository_MySQL(IDbConnection connection) 
    : IExpressionUpdateRepository<ExprUpdateUser, long>
{
    protected readonly IDbConnection connection = connection;
}

// PostgreSQL
[SqlDefine(SqlDefineTypes.PostgreSql)]
[RepositoryFor(typeof(IExpressionUpdateRepository<ExprUpdateUser, long>))]
public partial class ExprUpdateUserRepository_PostgreSQL(IDbConnection connection) 
    : IExpressionUpdateRepository<ExprUpdateUser, long>
{
    protected readonly IDbConnection connection = connection;
}

// SQL Server
[SqlDefine(SqlDefineTypes.SqlServer)]
[RepositoryFor(typeof(IExpressionUpdateRepository<ExprUpdateUser, long>))]
public partial class ExprUpdateUserRepository_SqlServer(IDbConnection connection) 
    : IExpressionUpdateRepository<ExprUpdateUser, long>
{
    protected readonly IDbConnection connection = connection;
}

#endregion

#region Test Base Class

/// <summary>
/// Base class for IExpressionUpdateRepository E2E tests.
/// </summary>
public abstract class IExpressionUpdateRepositoryE2ETestBase : IDisposable
{
    private static readonly Dictionary<string, DbConnection> _sharedConnections = new();
    private static readonly Dictionary<string, bool> _tablesCreated = new();
    private static readonly object _lock = new();

    protected DbConnection? Connection { get; set; }
    protected abstract SqlDefineTypes Dialect { get; }
    protected abstract string TestClassName { get; }

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
        
        if (!_tablesCreated.ContainsKey(connectionKey))
        {
            await CreateTablesAsync();
            lock (_lock)
            {
                _tablesCreated[connectionKey] = true;
            }
        }
        
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
                CREATE TABLE IF NOT EXISTS expr_update_users (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL,
                    email TEXT,
                    age INTEGER NOT NULL,
                    salary REAL NOT NULL,
                    is_active INTEGER NOT NULL,
                    created_at TEXT NOT NULL,
                    updated_at TEXT
                )",
            
            SqlDefineTypes.MySql => @"
                CREATE TABLE IF NOT EXISTS expr_update_users (
                    id BIGINT PRIMARY KEY AUTO_INCREMENT,
                    name VARCHAR(255) NOT NULL,
                    email VARCHAR(255),
                    age INT NOT NULL,
                    salary DECIMAL(18,2) NOT NULL,
                    is_active TINYINT(1) NOT NULL,
                    created_at DATETIME NOT NULL,
                    updated_at DATETIME
                )",
            
            SqlDefineTypes.PostgreSql => @"
                CREATE TABLE IF NOT EXISTS expr_update_users (
                    id BIGSERIAL PRIMARY KEY,
                    name VARCHAR(255) NOT NULL,
                    email VARCHAR(255),
                    age INTEGER NOT NULL,
                    salary DECIMAL(18,2) NOT NULL,
                    is_active BOOLEAN NOT NULL,
                    created_at TIMESTAMP NOT NULL,
                    updated_at TIMESTAMP
                )",
            
            SqlDefineTypes.SqlServer => @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'expr_update_users')
                CREATE TABLE expr_update_users (
                    id BIGINT PRIMARY KEY IDENTITY(1,1),
                    name NVARCHAR(255) NOT NULL,
                    email NVARCHAR(255),
                    age INT NOT NULL,
                    salary DECIMAL(18,2) NOT NULL,
                    is_active BIT NOT NULL,
                    created_at DATETIME2 NOT NULL,
                    updated_at DATETIME2
                )",
            
            _ => throw new NotSupportedException($"Dialect {Dialect} not supported")
        };
    }

    protected virtual async Task ClearTableAsync()
    {
        if (Connection == null) return;

        var sql = Dialect == SqlDefineTypes.SqlServer 
            ? "DELETE FROM expr_update_users; DBCC CHECKIDENT ('expr_update_users', RESEED, 0);"
            : "DELETE FROM expr_update_users";
        
        using var cmd = Connection.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync();
    }

    protected async Task<long> InsertTestUserAsync(string name, int age, decimal salary, bool isActive)
    {
        if (Connection == null) throw new InvalidOperationException("Connection not initialized");

        var sql = Dialect switch
        {
            SqlDefineTypes.SQLite => @"
                INSERT INTO expr_update_users (name, email, age, salary, is_active, created_at)
                VALUES (@name, @email, @age, @salary, @isActive, @createdAt);
                SELECT last_insert_rowid();",
            
            SqlDefineTypes.MySql => @"
                INSERT INTO expr_update_users (name, email, age, salary, is_active, created_at)
                VALUES (@name, @email, @age, @salary, @isActive, @createdAt);
                SELECT LAST_INSERT_ID();",
            
            SqlDefineTypes.PostgreSql => @"
                INSERT INTO expr_update_users (name, email, age, salary, is_active, created_at)
                VALUES (@name, @email, @age, @salary, @isActive, @createdAt)
                RETURNING id;",
            
            SqlDefineTypes.SqlServer => @"
                INSERT INTO expr_update_users (name, email, age, salary, is_active, created_at)
                OUTPUT INSERTED.id
                VALUES (@name, @email, @age, @salary, @isActive, @createdAt);",
            
            _ => throw new NotSupportedException($"Dialect {Dialect} not supported")
        };

        using var cmd = Connection.CreateCommand();
        cmd.CommandText = sql;
        
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
        isActiveParam.Value = isActive;
        cmd.Parameters.Add(isActiveParam);
        
        var createdAtParam = cmd.CreateParameter();
        createdAtParam.ParameterName = "@createdAt";
        createdAtParam.Value = DateTime.UtcNow;
        cmd.Parameters.Add(createdAtParam);

        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt64(result);
    }

    protected async Task<ExprUpdateUser?> GetUserByIdAsync(long id)
    {
        if (Connection == null) return null;

        var sql = "SELECT id, name, email, age, salary, is_active, created_at, updated_at FROM expr_update_users WHERE id = @id";
        
        using var cmd = Connection.CreateCommand();
        cmd.CommandText = sql;
        
        var idParam = cmd.CreateParameter();
        idParam.ParameterName = "@id";
        idParam.Value = id;
        cmd.Parameters.Add(idParam);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new ExprUpdateUser
            {
                Id = reader.GetInt64(0),
                Name = reader.GetString(1),
                Email = reader.IsDBNull(2) ? null : reader.GetString(2),
                Age = reader.GetInt32(3),
                Salary = reader.GetDecimal(4),
                IsActive = Dialect == SqlDefineTypes.SQLite ? reader.GetInt64(5) != 0 : reader.GetBoolean(5),
                CreatedAt = reader.GetDateTime(6),
                UpdatedAt = reader.IsDBNull(7) ? null : reader.GetDateTime(7)
            };
        }

        return null;
    }

    public virtual void Dispose()
    {
        // Don't dispose shared connection - it will be cleaned up at assembly cleanup
        GC.SuppressFinalize(this);
    }
}

#endregion

#region SQLite Tests

[TestClass]
public class IExpressionUpdateRepositoryE2ETests_SQLite : IExpressionUpdateRepositoryE2ETestBase
{
    private ExprUpdateUserRepository_SQLite? _repository;

    protected override SqlDefineTypes Dialect => SqlDefineTypes.SQLite;
    protected override string TestClassName => nameof(IExpressionUpdateRepositoryE2ETests_SQLite);

    [TestInitialize]
    public async Task TestInitialize()
    {
        await InitializeAsync();
        _repository = new ExprUpdateUserRepository_SQLite(Connection!);
    }

    [TestMethod]
    [TestCategory("SQLite")]
    [TestCategory("IExpressionUpdateRepository")]
    public async Task UpdateFieldsAsync_SingleField_UpdatesCorrectly()
    {
        // Arrange
        var id = await InsertTestUserAsync("Alice", 25, 50000, true);

        // Act
        var affected = await _repository!.UpdateFieldsAsync(id, u => new ExprUpdateUser { Name = "Alice Updated" });

        // Assert
        Assert.AreEqual(1, affected);
        var user = await GetUserByIdAsync(id);
        Assert.IsNotNull(user);
        Assert.AreEqual("Alice Updated", user.Name);
        Assert.AreEqual(25, user.Age); // Other fields unchanged
    }

    [TestMethod]
    [TestCategory("SQLite")]
    [TestCategory("IExpressionUpdateRepository")]
    public async Task UpdateFieldsAsync_MultipleFields_UpdatesCorrectly()
    {
        // Arrange
        var id = await InsertTestUserAsync("Bob", 30, 60000, true);

        // Act
        var affected = await _repository!.UpdateFieldsAsync(id, u => new ExprUpdateUser 
        { 
            Name = "Bob Updated",
            Age = 31,
            Salary = 65000
        });

        // Assert
        Assert.AreEqual(1, affected);
        var user = await GetUserByIdAsync(id);
        Assert.IsNotNull(user);
        Assert.AreEqual("Bob Updated", user.Name);
        Assert.AreEqual(31, user.Age);
        Assert.AreEqual(65000, user.Salary);
    }

    [TestMethod]
    [TestCategory("SQLite")]
    [TestCategory("IExpressionUpdateRepository")]
    public async Task UpdateFieldsAsync_NonExistentId_ReturnsZero()
    {
        // Act
        var affected = await _repository!.UpdateFieldsAsync(99999, u => new ExprUpdateUser { Name = "Test" });

        // Assert
        Assert.AreEqual(0, affected);
    }

    [TestMethod]
    [TestCategory("SQLite")]
    [TestCategory("IExpressionUpdateRepository")]
    public async Task UpdateFieldsWhereAsync_MatchingPredicate_UpdatesMultiple()
    {
        // Arrange
        await InsertTestUserAsync("User1", 25, 50000, true);
        await InsertTestUserAsync("User2", 30, 60000, true);
        await InsertTestUserAsync("User3", 35, 70000, false);

        // Act - Update all active users
        var affected = await _repository!.UpdateFieldsWhereAsync(
            u => u.IsActive,
            u => new ExprUpdateUser { Salary = 55000 });

        // Assert
        Assert.AreEqual(2, affected);
    }

    [TestMethod]
    [TestCategory("SQLite")]
    [TestCategory("IExpressionUpdateRepository")]
    public async Task UpdateFieldsWhereAsync_NoMatches_ReturnsZero()
    {
        // Arrange
        await InsertTestUserAsync("User1", 25, 50000, true);

        // Act
        var affected = await _repository!.UpdateFieldsWhereAsync(
            u => u.Age > 100,
            u => new ExprUpdateUser { Name = "Test" });

        // Assert
        Assert.AreEqual(0, affected);
    }

    [TestMethod]
    [TestCategory("SQLite")]
    [TestCategory("IExpressionUpdateRepository")]
    public async Task UpdateFieldsWhereAsync_ComplexPredicate_UpdatesCorrectly()
    {
        // Arrange
        await InsertTestUserAsync("User1", 25, 50000, true);
        await InsertTestUserAsync("User2", 30, 60000, true);
        await InsertTestUserAsync("User3", 35, 70000, true);

        // Act - Update users with age between 26 and 34
        var affected = await _repository!.UpdateFieldsWhereAsync(
            u => u.Age >= 26 && u.Age <= 34,
            u => new ExprUpdateUser { Salary = 65000 });

        // Assert
        Assert.AreEqual(1, affected); // Only User2
    }
}

#endregion

// Continue with MySQL, PostgreSQL, and SQL Server tests...

#region MySQL Tests

[TestClass]
public class IExpressionUpdateRepositoryE2ETests_MySQL : IExpressionUpdateRepositoryE2ETestBase
{
    private ExprUpdateUserRepository_MySQL? _repository;

    protected override SqlDefineTypes Dialect => SqlDefineTypes.MySql;
    protected override string TestClassName => nameof(IExpressionUpdateRepositoryE2ETests_MySQL);

    [TestInitialize]
    public async Task TestInitialize()
    {
        await InitializeAsync();
        _repository = new ExprUpdateUserRepository_MySQL(Connection!);
    }

    [TestMethod]
    [TestCategory("MySQL")]
    [TestCategory("IExpressionUpdateRepository")]
    public async Task UpdateFieldsAsync_SingleField_UpdatesCorrectly()
    {
        // Arrange
        var id = await InsertTestUserAsync("Alice", 25, 50000, true);

        // Act
        var affected = await _repository!.UpdateFieldsAsync(id, u => new ExprUpdateUser { Name = "Alice Updated" });

        // Assert
        Assert.AreEqual(1, affected);
        var user = await GetUserByIdAsync(id);
        Assert.IsNotNull(user);
        Assert.AreEqual("Alice Updated", user.Name);
        Assert.AreEqual(25, user.Age);
    }

    [TestMethod]
    [TestCategory("MySQL")]
    [TestCategory("IExpressionUpdateRepository")]
    public async Task UpdateFieldsAsync_MultipleFields_UpdatesCorrectly()
    {
        // Arrange
        var id = await InsertTestUserAsync("Bob", 30, 60000, true);

        // Act
        var affected = await _repository!.UpdateFieldsAsync(id, u => new ExprUpdateUser 
        { 
            Name = "Bob Updated",
            Age = 31,
            Salary = 65000,
            UpdatedAt = DateTime.UtcNow
        });

        // Assert
        Assert.AreEqual(1, affected);
        var user = await GetUserByIdAsync(id);
        Assert.IsNotNull(user);
        Assert.AreEqual("Bob Updated", user.Name);
        Assert.AreEqual(31, user.Age);
        Assert.AreEqual(65000, user.Salary);
        Assert.IsNotNull(user.UpdatedAt);
    }

    [TestMethod]
    [TestCategory("MySQL")]
    [TestCategory("IExpressionUpdateRepository")]
    public async Task UpdateFieldsAsync_NonExistentId_ReturnsZero()
    {
        // Act
        var affected = await _repository!.UpdateFieldsAsync(99999, u => new ExprUpdateUser { Name = "Test" });

        // Assert
        Assert.AreEqual(0, affected);
    }

    [TestMethod]
    [TestCategory("MySQL")]
    [TestCategory("IExpressionUpdateRepository")]
    public async Task UpdateFieldsWhereAsync_MatchingPredicate_UpdatesMultiple()
    {
        // Arrange
        await InsertTestUserAsync("User1", 25, 50000, true);
        await InsertTestUserAsync("User2", 30, 60000, true);
        await InsertTestUserAsync("User3", 35, 70000, false);

        // Act - Update all active users
        var affected = await _repository!.UpdateFieldsWhereAsync(
            u => u.IsActive,
            u => new ExprUpdateUser { Salary = 55000 });

        // Assert
        Assert.AreEqual(2, affected);
    }

    [TestMethod]
    [TestCategory("MySQL")]
    [TestCategory("IExpressionUpdateRepository")]
    public async Task UpdateFieldsWhereAsync_NoMatches_ReturnsZero()
    {
        // Arrange
        await InsertTestUserAsync("User1", 25, 50000, true);

        // Act
        var affected = await _repository!.UpdateFieldsWhereAsync(
            u => u.Age > 100,
            u => new ExprUpdateUser { Name = "Test" });

        // Assert
        Assert.AreEqual(0, affected);
    }

    [TestMethod]
    [TestCategory("MySQL")]
    [TestCategory("IExpressionUpdateRepository")]
    public async Task UpdateFieldsWhereAsync_ComplexPredicate_UpdatesCorrectly()
    {
        // Arrange
        await InsertTestUserAsync("User1", 25, 50000, true);
        await InsertTestUserAsync("User2", 30, 60000, true);
        await InsertTestUserAsync("User3", 35, 70000, true);

        // Act - Update users with age between 26 and 34
        var affected = await _repository!.UpdateFieldsWhereAsync(
            u => u.Age >= 26 && u.Age <= 34,
            u => new ExprUpdateUser { Salary = 65000 });

        // Assert
        Assert.AreEqual(1, affected);
    }

    [TestMethod]
    [TestCategory("MySQL")]
    [TestCategory("IExpressionUpdateRepository")]
    public async Task UpdateFieldsWhereAsync_StringContains_UpdatesCorrectly()
    {
        // Arrange
        await InsertTestUserAsync("Alice Smith", 25, 50000, true);
        await InsertTestUserAsync("Bob Jones", 30, 60000, true);
        await InsertTestUserAsync("Alice Johnson", 35, 70000, true);

        // Act - Update all users with "Alice" in name
        var affected = await _repository!.UpdateFieldsWhereAsync(
            u => u.Name.Contains("Alice"),
            u => new ExprUpdateUser { IsActive = false });

        // Assert
        Assert.AreEqual(2, affected);
    }
}

#endregion

#region PostgreSQL Tests

[TestClass]
public class IExpressionUpdateRepositoryE2ETests_PostgreSQL : IExpressionUpdateRepositoryE2ETestBase
{
    private ExprUpdateUserRepository_PostgreSQL? _repository;

    protected override SqlDefineTypes Dialect => SqlDefineTypes.PostgreSql;
    protected override string TestClassName => nameof(IExpressionUpdateRepositoryE2ETests_PostgreSQL);

    [TestInitialize]
    public async Task TestInitialize()
    {
        await InitializeAsync();
        _repository = new ExprUpdateUserRepository_PostgreSQL(Connection!);
    }

    [TestMethod]
    [TestCategory("PostgreSQL")]
    [TestCategory("IExpressionUpdateRepository")]
    public async Task UpdateFieldsAsync_SingleField_UpdatesCorrectly()
    {
        // Arrange
        var id = await InsertTestUserAsync("Alice", 25, 50000, true);

        // Act
        var affected = await _repository!.UpdateFieldsAsync(id, u => new ExprUpdateUser { Name = "Alice Updated" });

        // Assert
        Assert.AreEqual(1, affected);
        var user = await GetUserByIdAsync(id);
        Assert.IsNotNull(user);
        Assert.AreEqual("Alice Updated", user.Name);
        Assert.AreEqual(25, user.Age);
    }

    [TestMethod]
    [TestCategory("PostgreSQL")]
    [TestCategory("IExpressionUpdateRepository")]
    public async Task UpdateFieldsAsync_MultipleFields_UpdatesCorrectly()
    {
        // Arrange
        var id = await InsertTestUserAsync("Bob", 30, 60000, true);

        // Act
        var affected = await _repository!.UpdateFieldsAsync(id, u => new ExprUpdateUser 
        { 
            Name = "Bob Updated",
            Age = 31,
            Salary = 65000,
            UpdatedAt = DateTime.UtcNow
        });

        // Assert
        Assert.AreEqual(1, affected);
        var user = await GetUserByIdAsync(id);
        Assert.IsNotNull(user);
        Assert.AreEqual("Bob Updated", user.Name);
        Assert.AreEqual(31, user.Age);
        Assert.AreEqual(65000, user.Salary);
        Assert.IsNotNull(user.UpdatedAt);
    }

    [TestMethod]
    [TestCategory("PostgreSQL")]
    [TestCategory("IExpressionUpdateRepository")]
    public async Task UpdateFieldsAsync_NonExistentId_ReturnsZero()
    {
        // Act
        var affected = await _repository!.UpdateFieldsAsync(99999, u => new ExprUpdateUser { Name = "Test" });

        // Assert
        Assert.AreEqual(0, affected);
    }

    [TestMethod]
    [TestCategory("PostgreSQL")]
    [TestCategory("IExpressionUpdateRepository")]
    public async Task UpdateFieldsWhereAsync_MatchingPredicate_UpdatesMultiple()
    {
        // Arrange
        await InsertTestUserAsync("User1", 25, 50000, true);
        await InsertTestUserAsync("User2", 30, 60000, true);
        await InsertTestUserAsync("User3", 35, 70000, false);

        // Act - Update all active users
        var affected = await _repository!.UpdateFieldsWhereAsync(
            u => u.IsActive,
            u => new ExprUpdateUser { Salary = 55000 });

        // Assert
        Assert.AreEqual(2, affected);
    }

    [TestMethod]
    [TestCategory("PostgreSQL")]
    [TestCategory("IExpressionUpdateRepository")]
    public async Task UpdateFieldsWhereAsync_NoMatches_ReturnsZero()
    {
        // Arrange
        await InsertTestUserAsync("User1", 25, 50000, true);

        // Act
        var affected = await _repository!.UpdateFieldsWhereAsync(
            u => u.Age > 100,
            u => new ExprUpdateUser { Name = "Test" });

        // Assert
        Assert.AreEqual(0, affected);
    }

    [TestMethod]
    [TestCategory("PostgreSQL")]
    [TestCategory("IExpressionUpdateRepository")]
    public async Task UpdateFieldsWhereAsync_ComplexPredicate_UpdatesCorrectly()
    {
        // Arrange
        await InsertTestUserAsync("User1", 25, 50000, true);
        await InsertTestUserAsync("User2", 30, 60000, true);
        await InsertTestUserAsync("User3", 35, 70000, true);

        // Act - Update users with age between 26 and 34
        var affected = await _repository!.UpdateFieldsWhereAsync(
            u => u.Age >= 26 && u.Age <= 34,
            u => new ExprUpdateUser { Salary = 65000 });

        // Assert
        Assert.AreEqual(1, affected);
    }

    [TestMethod]
    [TestCategory("PostgreSQL")]
    [TestCategory("IExpressionUpdateRepository")]
    public async Task UpdateFieldsWhereAsync_BooleanField_UpdatesCorrectly()
    {
        // Arrange
        await InsertTestUserAsync("User1", 25, 50000, true);
        await InsertTestUserAsync("User2", 30, 60000, true);
        await InsertTestUserAsync("User3", 35, 70000, false);

        // Act - Deactivate all users
        var affected = await _repository!.UpdateFieldsWhereAsync(
            u => u.Id > 0,
            u => new ExprUpdateUser { IsActive = false });

        // Assert
        Assert.AreEqual(3, affected);
    }

    [TestMethod]
    [TestCategory("PostgreSQL")]
    [TestCategory("IExpressionUpdateRepository")]
    public async Task UpdateFieldsWhereAsync_DecimalField_UpdatesCorrectly()
    {
        // Arrange
        await InsertTestUserAsync("User1", 25, 50000, true);
        await InsertTestUserAsync("User2", 30, 60000, true);

        // Act - Give everyone a raise
        var affected = await _repository!.UpdateFieldsWhereAsync(
            u => u.Salary < 70000,
            u => new ExprUpdateUser { Salary = 75000.50m });

        // Assert
        Assert.AreEqual(2, affected);
    }
}

#endregion

#region SQL Server Tests

[TestClass]
public class IExpressionUpdateRepositoryE2ETests_SqlServer : IExpressionUpdateRepositoryE2ETestBase
{
    private ExprUpdateUserRepository_SqlServer? _repository;

    protected override SqlDefineTypes Dialect => SqlDefineTypes.SqlServer;
    protected override string TestClassName => nameof(IExpressionUpdateRepositoryE2ETests_SqlServer);

    [TestInitialize]
    public async Task TestInitialize()
    {
        await InitializeAsync();
        _repository = new ExprUpdateUserRepository_SqlServer(Connection!);
    }

    [TestMethod]
    [TestCategory("SqlServer")]
    [TestCategory("IExpressionUpdateRepository")]
    public async Task UpdateFieldsAsync_SingleField_UpdatesCorrectly()
    {
        // Arrange
        var id = await InsertTestUserAsync("Alice", 25, 50000, true);

        // Act
        var affected = await _repository!.UpdateFieldsAsync(id, u => new ExprUpdateUser { Name = "Alice Updated" });

        // Assert
        Assert.AreEqual(1, affected);
        var user = await GetUserByIdAsync(id);
        Assert.IsNotNull(user);
        Assert.AreEqual("Alice Updated", user.Name);
        Assert.AreEqual(25, user.Age);
    }

    [TestMethod]
    [TestCategory("SqlServer")]
    [TestCategory("IExpressionUpdateRepository")]
    public async Task UpdateFieldsAsync_MultipleFields_UpdatesCorrectly()
    {
        // Arrange
        var id = await InsertTestUserAsync("Bob", 30, 60000, true);

        // Act
        var affected = await _repository!.UpdateFieldsAsync(id, u => new ExprUpdateUser 
        { 
            Name = "Bob Updated",
            Age = 31,
            Salary = 65000,
            UpdatedAt = DateTime.UtcNow
        });

        // Assert
        Assert.AreEqual(1, affected);
        var user = await GetUserByIdAsync(id);
        Assert.IsNotNull(user);
        Assert.AreEqual("Bob Updated", user.Name);
        Assert.AreEqual(31, user.Age);
        Assert.AreEqual(65000, user.Salary);
        Assert.IsNotNull(user.UpdatedAt);
    }

    [TestMethod]
    [TestCategory("SqlServer")]
    [TestCategory("IExpressionUpdateRepository")]
    public async Task UpdateFieldsAsync_NonExistentId_ReturnsZero()
    {
        // Act
        var affected = await _repository!.UpdateFieldsAsync(99999, u => new ExprUpdateUser { Name = "Test" });

        // Assert
        Assert.AreEqual(0, affected);
    }

    [TestMethod]
    [TestCategory("SqlServer")]
    [TestCategory("IExpressionUpdateRepository")]
    public async Task UpdateFieldsWhereAsync_MatchingPredicate_UpdatesMultiple()
    {
        // Arrange
        await InsertTestUserAsync("User1", 25, 50000, true);
        await InsertTestUserAsync("User2", 30, 60000, true);
        await InsertTestUserAsync("User3", 35, 70000, false);

        // Act - Update all active users
        var affected = await _repository!.UpdateFieldsWhereAsync(
            u => u.IsActive,
            u => new ExprUpdateUser { Salary = 55000 });

        // Assert
        Assert.AreEqual(2, affected);
    }

    [TestMethod]
    [TestCategory("SqlServer")]
    [TestCategory("IExpressionUpdateRepository")]
    public async Task UpdateFieldsWhereAsync_NoMatches_ReturnsZero()
    {
        // Arrange
        await InsertTestUserAsync("User1", 25, 50000, true);

        // Act
        var affected = await _repository!.UpdateFieldsWhereAsync(
            u => u.Age > 100,
            u => new ExprUpdateUser { Name = "Test" });

        // Assert
        Assert.AreEqual(0, affected);
    }

    [TestMethod]
    [TestCategory("SqlServer")]
    [TestCategory("IExpressionUpdateRepository")]
    public async Task UpdateFieldsWhereAsync_ComplexPredicate_UpdatesCorrectly()
    {
        // Arrange
        await InsertTestUserAsync("User1", 25, 50000, true);
        await InsertTestUserAsync("User2", 30, 60000, true);
        await InsertTestUserAsync("User3", 35, 70000, true);

        // Act - Update users with age between 26 and 34
        var affected = await _repository!.UpdateFieldsWhereAsync(
            u => u.Age >= 26 && u.Age <= 34,
            u => new ExprUpdateUser { Salary = 65000 });

        // Assert
        Assert.AreEqual(1, affected);
    }

    [TestMethod]
    [TestCategory("SqlServer")]
    [TestCategory("IExpressionUpdateRepository")]
    public async Task UpdateFieldsWhereAsync_BooleanField_UpdatesCorrectly()
    {
        // Arrange
        await InsertTestUserAsync("User1", 25, 50000, true);
        await InsertTestUserAsync("User2", 30, 60000, true);
        await InsertTestUserAsync("User3", 35, 70000, false);

        // Act - Activate all users
        var affected = await _repository!.UpdateFieldsWhereAsync(
            u => u.Id > 0,
            u => new ExprUpdateUser { IsActive = true });

        // Assert
        Assert.AreEqual(3, affected);
    }

    [TestMethod]
    [TestCategory("SqlServer")]
    [TestCategory("IExpressionUpdateRepository")]
    public async Task UpdateFieldsWhereAsync_NullableField_UpdatesCorrectly()
    {
        // Arrange
        var id1 = await InsertTestUserAsync("User1", 25, 50000, true);
        var id2 = await InsertTestUserAsync("User2", 30, 60000, true);

        // Act - Set UpdatedAt for all users
        var now = DateTime.UtcNow;
        var affected = await _repository!.UpdateFieldsWhereAsync(
            u => u.Id > 0,
            u => new ExprUpdateUser { UpdatedAt = now });

        // Assert
        Assert.AreEqual(2, affected);
        var user1 = await GetUserByIdAsync(id1);
        var user2 = await GetUserByIdAsync(id2);
        Assert.IsNotNull(user1?.UpdatedAt);
        Assert.IsNotNull(user2?.UpdatedAt);
    }

    [TestMethod]
    [TestCategory("SqlServer")]
    [TestCategory("IExpressionUpdateRepository")]
    public async Task UpdateFieldsAsync_AllFieldTypes_UpdatesCorrectly()
    {
        // Arrange
        var id = await InsertTestUserAsync("Original", 20, 40000, false);

        // Act - Update all field types
        var now = DateTime.UtcNow;
        var affected = await _repository!.UpdateFieldsAsync(id, u => new ExprUpdateUser 
        { 
            Name = "Updated Name",
            Email = "updated@test.com",
            Age = 25,
            Salary = 55000.75m,
            IsActive = true,
            UpdatedAt = now
        });

        // Assert
        Assert.AreEqual(1, affected);
        var user = await GetUserByIdAsync(id);
        Assert.IsNotNull(user);
        Assert.AreEqual("Updated Name", user.Name);
        Assert.AreEqual("updated@test.com", user.Email);
        Assert.AreEqual(25, user.Age);
        Assert.AreEqual(55000.75m, user.Salary);
        Assert.IsTrue(user.IsActive);
        Assert.IsNotNull(user.UpdatedAt);
    }
}

#endregion
