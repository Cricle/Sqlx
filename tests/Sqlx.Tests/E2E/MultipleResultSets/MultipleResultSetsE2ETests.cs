// <copyright file="MultipleResultSetsE2ETests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using Sqlx.Tests.E2E.Infrastructure;
using Sqlx.Annotations;
using System.Data.Common;

namespace Sqlx.Tests.E2E.MultipleResultSets;

#region Test Repositories

public interface IMultiResultE2ERepository
{
    [SqlTemplate(@"
        INSERT INTO test_users (name, age, email) VALUES (@name, @age, @email);
        SELECT COUNT(*) FROM test_users
    ")]
    [ResultSetMapping(0, "rowsAffected")]
    [ResultSetMapping(1, "totalUsers")]
    Task<(int rowsAffected, int totalUsers)> InsertAndCountAsync(string name, int age, string email);

    [SqlTemplate(@"
        SELECT COUNT(*) FROM test_users;
        SELECT COALESCE(MAX(id), 0) FROM test_users;
        SELECT COALESCE(MIN(id), 0) FROM test_users
    ")]
    [ResultSetMapping(0, "totalUsers")]
    [ResultSetMapping(1, "maxId")]
    [ResultSetMapping(2, "minId")]
    Task<(int totalUsers, long maxId, long minId)> GetStatsAsync();

    [SqlTemplate(@"
        INSERT INTO test_users (name, age, email) VALUES (@name, @age, @email);
        SELECT COALESCE(MAX(id), 0) FROM test_users;
        SELECT COUNT(*) FROM test_users
    ")]
    [ResultSetMapping(0, "userId")]
    [ResultSetMapping(1, "totalUsers")]
    int InsertWithCounter(
        string name,
        int age,
        string email,
        ref int totalUsersSnapshot);

    [SqlTemplate(@"
        INSERT INTO test_users (name, age, email) VALUES (@name, @age, @email);
        SELECT COALESCE(MAX(id), 0) FROM test_users;
        SELECT COUNT(*) FROM test_users
    ")]
    [ResultSetMapping(0, "userId")]
    [ResultSetMapping(1, "totalUsers")]
    Task<(long userId, int totalUsers)> InsertWithCounterAsync(
        string name,
        int age,
        string email,
        OutputParameter<int> totalUsersSnapshot);
}

[RepositoryFor(typeof(IMultiResultE2ERepository), TableName = "test_users")]
public partial class MySqlMultiResultE2ERepository : IMultiResultE2ERepository
{
    private readonly DbConnection _connection;

    public MySqlMultiResultE2ERepository(DbConnection connection, SqlDialect dialect)
    {
        _connection = connection;
        _dialect = dialect;
    }
}

[RepositoryFor(typeof(IMultiResultE2ERepository), TableName = "test_users")]
public partial class PostgreSqlMultiResultE2ERepository : IMultiResultE2ERepository
{
    private readonly DbConnection _connection;

    public PostgreSqlMultiResultE2ERepository(DbConnection connection, SqlDialect dialect)
    {
        _connection = connection;
        _dialect = dialect;
    }
}

[RepositoryFor(typeof(IMultiResultE2ERepository), TableName = "test_users")]
public partial class SqlServerMultiResultE2ERepository : IMultiResultE2ERepository
{
    private readonly DbConnection _connection;

    public SqlServerMultiResultE2ERepository(DbConnection connection, SqlDialect dialect)
    {
        _connection = connection;
        _dialect = dialect;
    }
}

[RepositoryFor(typeof(IMultiResultE2ERepository), TableName = "test_users")]
public partial class SQLiteMultiResultE2ERepository : IMultiResultE2ERepository
{
    private readonly DbConnection _connection;

    public SQLiteMultiResultE2ERepository(DbConnection connection, SqlDialect dialect)
    {
        _connection = connection;
        _dialect = dialect;
    }
}

#endregion

/// <summary>
/// E2E tests for multiple result sets functionality across all supported databases.
/// </summary>
[TestClass]
public class MultipleResultSetsE2ETests : E2ETestBase
{
    /// <summary>
    /// Creates the test_users table schema for the specified database type.
    /// </summary>
    private static string GetTestUsersSchema(DatabaseType dbType)
    {
        return dbType switch
        {
            DatabaseType.MySQL => @"
                CREATE TABLE test_users (
                    id BIGINT AUTO_INCREMENT PRIMARY KEY,
                    name VARCHAR(255) NOT NULL,
                    age INT NOT NULL,
                    email VARCHAR(255) NOT NULL
                )",
            DatabaseType.PostgreSQL => @"
                CREATE TABLE test_users (
                    id BIGSERIAL PRIMARY KEY,
                    name VARCHAR(255) NOT NULL,
                    age INT NOT NULL,
                    email VARCHAR(255) NOT NULL
                )",
            DatabaseType.SqlServer => @"
                CREATE TABLE test_users (
                    id BIGINT IDENTITY(1,1) PRIMARY KEY,
                    name NVARCHAR(255) NOT NULL,
                    age INT NOT NULL,
                    email NVARCHAR(255) NOT NULL
                )",
            DatabaseType.SQLite => @"
                CREATE TABLE test_users (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL,
                    age INTEGER NOT NULL,
                    email TEXT NOT NULL
                )",
            _ => throw new NotSupportedException($"Database type {dbType} is not supported"),
        };
    }

    // ==================== MySQL Multiple Result Sets Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("MultipleResultSets")]
    [TestCategory("MySQL")]
    public async Task MySQL_MultipleResultSets_InsertAndCount_ReturnsCorrectValues()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.MySQL));
        var repo = new MySqlMultiResultE2ERepository(fixture.Connection, SqlDefine.MySql);

        // Act
        var (rows1, total1) = await repo.InsertAndCountAsync("Alice", 25, "alice@example.com");
        var (rows2, total2) = await repo.InsertAndCountAsync("Bob", 30, "bob@example.com");

        // Assert
        Assert.AreEqual(1, rows1);
        Assert.AreEqual(1, total1);
        Assert.AreEqual(1, rows2);
        Assert.AreEqual(2, total2);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("MultipleResultSets")]
    [TestCategory("MySQL")]
    public async Task MySQL_MultipleResultSets_GetStats_ReturnsCorrectAggregates()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.MySQL));
        var repo = new MySqlMultiResultE2ERepository(fixture.Connection, SqlDefine.MySql);

        await repo.InsertAndCountAsync("Alice", 25, "alice@example.com");
        await repo.InsertAndCountAsync("Bob", 30, "bob@example.com");
        await repo.InsertAndCountAsync("Charlie", 35, "charlie@example.com");

        // Act
        var (total, maxId, minId) = await repo.GetStatsAsync();

        // Assert
        Assert.AreEqual(3, total);
        Assert.IsTrue(maxId >= 3);
        Assert.IsTrue(minId >= 1);
        Assert.IsTrue(maxId > minId);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("MultipleResultSets")]
    [TestCategory("MySQL")]
    public async Task MySQL_MultipleResultSets_EmptyTable_ReturnsZeroStats()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.MySQL));
        var repo = new MySqlMultiResultE2ERepository(fixture.Connection, SqlDefine.MySql);

        // Act
        var (total, maxId, minId) = await repo.GetStatsAsync();

        // Assert
        Assert.AreEqual(0, total);
        Assert.AreEqual(0L, maxId, "COALESCE(MAX(id), 0) should return 0 for empty table");
        Assert.AreEqual(0L, minId, "COALESCE(MIN(id), 0) should return 0 for empty table");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("MultipleResultSets")]
    [TestCategory("MySQL")]
    public async Task MySQL_MultipleResultSets_MultipleInserts_IncrementingCounts()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.MySQL));
        var repo = new MySqlMultiResultE2ERepository(fixture.Connection, SqlDefine.MySql);

        // Act & Assert
        var (rows1, total1) = await repo.InsertAndCountAsync("User1", 20, "user1@example.com");
        Assert.AreEqual(1, rows1);
        Assert.AreEqual(1, total1);

        var (rows2, total2) = await repo.InsertAndCountAsync("User2", 21, "user2@example.com");
        Assert.AreEqual(1, rows2);
        Assert.AreEqual(2, total2);

        var (rows3, total3) = await repo.InsertAndCountAsync("User3", 22, "user3@example.com");
        Assert.AreEqual(1, rows3);
        Assert.AreEqual(3, total3);

        var (rows4, total4) = await repo.InsertAndCountAsync("User4", 23, "user4@example.com");
        Assert.AreEqual(1, rows4);
        Assert.AreEqual(4, total4);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("MultipleResultSets")]
    [TestCategory("MySQL")]
    public async Task MySQL_MultipleResultSets_WithRefOutput_ReturnsTupleAndUpdatesRef()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.MySQL));
        var repo = new MySqlMultiResultE2ERepository(fixture.Connection, SqlDefine.MySql);

        int snapshot = 0;
        var userId = repo.InsertWithCounter("Alice", 25, "alice@example.com", ref snapshot);

        Assert.AreEqual(1L, userId);
        Assert.AreEqual(1, snapshot);
    }


    // ==================== PostgreSQL Multiple Result Sets Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("MultipleResultSets")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_MultipleResultSets_InsertAndCount_ReturnsCorrectValues()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.PostgreSQL));
        var repo = new PostgreSqlMultiResultE2ERepository(fixture.Connection, SqlDefine.PostgreSql);

        // Act
        var (rows1, total1) = await repo.InsertAndCountAsync("Alice", 25, "alice@example.com");
        var (rows2, total2) = await repo.InsertAndCountAsync("Bob", 30, "bob@example.com");

        // Assert
        Assert.AreEqual(1, rows1);
        Assert.AreEqual(1, total1);
        Assert.AreEqual(1, rows2);
        Assert.AreEqual(2, total2);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("MultipleResultSets")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_MultipleResultSets_GetStats_ReturnsCorrectAggregates()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.PostgreSQL));
        var repo = new PostgreSqlMultiResultE2ERepository(fixture.Connection, SqlDefine.PostgreSql);

        await repo.InsertAndCountAsync("Alice", 25, "alice@example.com");
        await repo.InsertAndCountAsync("Bob", 30, "bob@example.com");

        // Act
        var (total, maxId, minId) = await repo.GetStatsAsync();

        // Assert
        Assert.AreEqual(2, total);
        Assert.IsTrue(maxId >= 2);
        Assert.IsTrue(minId >= 1);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("MultipleResultSets")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_MultipleResultSets_EmptyTable_ReturnsZeroStats()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.PostgreSQL));
        var repo = new PostgreSqlMultiResultE2ERepository(fixture.Connection, SqlDefine.PostgreSql);

        // Act
        var (total, maxId, minId) = await repo.GetStatsAsync();

        // Assert
        Assert.AreEqual(0, total);
        Assert.AreEqual(0L, maxId, "COALESCE(MAX(id), 0) should return 0 for empty table");
        Assert.AreEqual(0L, minId, "COALESCE(MIN(id), 0) should return 0 for empty table");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("MultipleResultSets")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_MultipleResultSets_MultipleInserts_IncrementingCounts()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.PostgreSQL));
        var repo = new PostgreSqlMultiResultE2ERepository(fixture.Connection, SqlDefine.PostgreSql);

        // Act & Assert
        var (rows1, total1) = await repo.InsertAndCountAsync("User1", 20, "user1@example.com");
        Assert.AreEqual(1, rows1);
        Assert.AreEqual(1, total1);

        var (rows2, total2) = await repo.InsertAndCountAsync("User2", 21, "user2@example.com");
        Assert.AreEqual(1, rows2);
        Assert.AreEqual(2, total2);

        var (rows3, total3) = await repo.InsertAndCountAsync("User3", 22, "user3@example.com");
        Assert.AreEqual(1, rows3);
        Assert.AreEqual(3, total3);

        var (rows4, total4) = await repo.InsertAndCountAsync("User4", 23, "user4@example.com");
        Assert.AreEqual(1, rows4);
        Assert.AreEqual(4, total4);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("MultipleResultSets")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_MultipleResultSets_WithRefOutput_ReturnsTupleAndUpdatesRef()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.PostgreSQL));
        var repo = new PostgreSqlMultiResultE2ERepository(fixture.Connection, SqlDefine.PostgreSql);

        int snapshot = 0;
        var userId = repo.InsertWithCounter("Alice", 25, "alice@example.com", ref snapshot);

        Assert.AreEqual(1L, userId);
        Assert.AreEqual(1, snapshot);
    }


    // ==================== SQL Server Multiple Result Sets Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("MultipleResultSets")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_MultipleResultSets_InsertAndCount_ReturnsCorrectValues()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.SqlServer));
        var repo = new SqlServerMultiResultE2ERepository(fixture.Connection, SqlDefine.SqlServer);

        // Act
        var (rows1, total1) = await repo.InsertAndCountAsync("Alice", 25, "alice@example.com");
        var (rows2, total2) = await repo.InsertAndCountAsync("Bob", 30, "bob@example.com");

        // Assert
        Assert.AreEqual(1, rows1);
        Assert.AreEqual(1, total1);
        Assert.AreEqual(1, rows2);
        Assert.AreEqual(2, total2);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("MultipleResultSets")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_MultipleResultSets_GetStats_ReturnsCorrectAggregates()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.SqlServer));
        var repo = new SqlServerMultiResultE2ERepository(fixture.Connection, SqlDefine.SqlServer);

        await repo.InsertAndCountAsync("Alice", 25, "alice@example.com");
        await repo.InsertAndCountAsync("Bob", 30, "bob@example.com");
        await repo.InsertAndCountAsync("Charlie", 35, "charlie@example.com");

        // Act
        var (total, maxId, minId) = await repo.GetStatsAsync();

        // Assert
        Assert.AreEqual(3, total);
        Assert.IsTrue(maxId >= 3);
        Assert.IsTrue(minId >= 1);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("MultipleResultSets")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_MultipleResultSets_EmptyTable_ReturnsZeroStats()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.SqlServer));
        var repo = new SqlServerMultiResultE2ERepository(fixture.Connection, SqlDefine.SqlServer);

        // Act
        var (total, maxId, minId) = await repo.GetStatsAsync();

        // Assert
        Assert.AreEqual(0, total);
        Assert.AreEqual(0L, maxId, "COALESCE(MAX(id), 0) should return 0 for empty table");
        Assert.AreEqual(0L, minId, "COALESCE(MIN(id), 0) should return 0 for empty table");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("MultipleResultSets")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_MultipleResultSets_MultipleInserts_IncrementingCounts()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.SqlServer));
        var repo = new SqlServerMultiResultE2ERepository(fixture.Connection, SqlDefine.SqlServer);

        // Act & Assert
        var (rows1, total1) = await repo.InsertAndCountAsync("User1", 20, "user1@example.com");
        Assert.AreEqual(1, rows1);
        Assert.AreEqual(1, total1);

        var (rows2, total2) = await repo.InsertAndCountAsync("User2", 21, "user2@example.com");
        Assert.AreEqual(1, rows2);
        Assert.AreEqual(2, total2);

        var (rows3, total3) = await repo.InsertAndCountAsync("User3", 22, "user3@example.com");
        Assert.AreEqual(1, rows3);
        Assert.AreEqual(3, total3);

        var (rows4, total4) = await repo.InsertAndCountAsync("User4", 23, "user4@example.com");
        Assert.AreEqual(1, rows4);
        Assert.AreEqual(4, total4);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("MultipleResultSets")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_MultipleResultSets_WithRefOutput_ReturnsTupleAndUpdatesRef()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.SqlServer));
        var repo = new SqlServerMultiResultE2ERepository(fixture.Connection, SqlDefine.SqlServer);

        int snapshot = 0;
        var userId = repo.InsertWithCounter("Alice", 25, "alice@example.com", ref snapshot);

        Assert.AreEqual(1L, userId);
        Assert.AreEqual(1, snapshot);
    }


    // ==================== SQLite Multiple Result Sets Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("MultipleResultSets")]
    [TestCategory("SQLite")]
    public async Task SQLite_MultipleResultSets_InsertAndCount_ReturnsCorrectValues()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.SQLite));
        var repo = new SQLiteMultiResultE2ERepository(fixture.Connection, SqlDefine.SQLite);

        // Act
        var (rows1, total1) = await repo.InsertAndCountAsync("Alice", 25, "alice@example.com");
        var (rows2, total2) = await repo.InsertAndCountAsync("Bob", 30, "bob@example.com");

        // Assert
        Assert.AreEqual(1, rows1);
        Assert.AreEqual(1, total1);
        Assert.AreEqual(1, rows2);
        Assert.AreEqual(2, total2);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("MultipleResultSets")]
    [TestCategory("SQLite")]
    public async Task SQLite_MultipleResultSets_GetStats_ReturnsCorrectAggregates()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.SQLite));
        var repo = new SQLiteMultiResultE2ERepository(fixture.Connection, SqlDefine.SQLite);

        await repo.InsertAndCountAsync("Alice", 25, "alice@example.com");
        await repo.InsertAndCountAsync("Bob", 30, "bob@example.com");

        // Act
        var (total, maxId, minId) = await repo.GetStatsAsync();

        // Assert
        Assert.AreEqual(2, total);
        Assert.AreEqual(2L, maxId);
        Assert.AreEqual(1L, minId);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("MultipleResultSets")]
    [TestCategory("SQLite")]
    public async Task SQLite_MultipleResultSets_EmptyTable_ReturnsZeroStats()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.SQLite));
        var repo = new SQLiteMultiResultE2ERepository(fixture.Connection, SqlDefine.SQLite);

        // Act
        var (total, maxId, minId) = await repo.GetStatsAsync();

        // Assert
        Assert.AreEqual(0, total);
        Assert.AreEqual(0L, maxId, "COALESCE(MAX(id), 0) should return 0 for empty table");
        Assert.AreEqual(0L, minId, "COALESCE(MIN(id), 0) should return 0 for empty table");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("MultipleResultSets")]
    [TestCategory("SQLite")]
    public async Task SQLite_MultipleResultSets_MultipleInserts_IncrementingCounts()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.SQLite));
        var repo = new SQLiteMultiResultE2ERepository(fixture.Connection, SqlDefine.SQLite);

        // Act & Assert
        var (rows1, total1) = await repo.InsertAndCountAsync("User1", 20, "user1@example.com");
        Assert.AreEqual(1, rows1);
        Assert.AreEqual(1, total1);

        var (rows2, total2) = await repo.InsertAndCountAsync("User2", 21, "user2@example.com");
        Assert.AreEqual(1, rows2);
        Assert.AreEqual(2, total2);

        var (rows3, total3) = await repo.InsertAndCountAsync("User3", 22, "user3@example.com");
        Assert.AreEqual(1, rows3);
        Assert.AreEqual(3, total3);

        var (rows4, total4) = await repo.InsertAndCountAsync("User4", 23, "user4@example.com");
        Assert.AreEqual(1, rows4);
        Assert.AreEqual(4, total4);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("MultipleResultSets")]
    [TestCategory("SQLite")]
    public async Task SQLite_MultipleResultSets_WithRefOutput_ReturnsTupleAndUpdatesRef()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.SQLite));
        var repo = new SQLiteMultiResultE2ERepository(fixture.Connection, SqlDefine.SQLite);

        int snapshot = 0;
        var userId = repo.InsertWithCounter("Alice", 25, "alice@example.com", ref snapshot);

        Assert.AreEqual(1L, userId);
        Assert.AreEqual(1, snapshot);
    }

}
