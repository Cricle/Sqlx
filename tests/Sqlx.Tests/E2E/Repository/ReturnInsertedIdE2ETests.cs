// <copyright file="ReturnInsertedIdE2ETests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using System.Data.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;
using Sqlx.Tests.E2E.Infrastructure;

namespace Sqlx.Tests.E2E.Repository;

[Sqlx]
public class ReturnInsertedIdEntity
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
}

public interface IReturnInsertedIdRepository
{
    [SqlTemplate("INSERT INTO return_id_users (name, age) VALUES (@name, @age)")]
    [ReturnInsertedId]
    long InsertAndReturnId(string name, int age);

    [SqlTemplate("INSERT INTO return_id_users (name, age) VALUES (@name, @age)")]
    [ReturnInsertedId]
    Task<long> InsertAndReturnIdAsync(string name, int age, CancellationToken cancellationToken = default);

    [SqlTemplate("SELECT id, name, age FROM return_id_users WHERE id = @id")]
    Task<ReturnInsertedIdEntity?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
}

[RepositoryFor(typeof(IReturnInsertedIdRepository), TableName = "return_id_users")]
public partial class ReturnInsertedIdRepository : IReturnInsertedIdRepository
{
    private readonly DbConnection _connection;

    public ReturnInsertedIdRepository(DbConnection connection, SqlDialect dialect)
    {
        _connection = connection;
        _dialect = dialect;
    }
}

[TestClass]
public class ReturnInsertedIdE2ETests : E2ETestBase
{
    private static string GetSchema(DatabaseType dbType)
    {
        return dbType switch
        {
            DatabaseType.MySQL => @"
                CREATE TABLE return_id_users (
                    id BIGINT AUTO_INCREMENT PRIMARY KEY,
                    name VARCHAR(100) NOT NULL,
                    age INT NOT NULL
                )",
            DatabaseType.PostgreSQL => @"
                CREATE TABLE return_id_users (
                    id BIGSERIAL PRIMARY KEY,
                    name VARCHAR(100) NOT NULL,
                    age INT NOT NULL
                )",
            DatabaseType.SqlServer => @"
                CREATE TABLE return_id_users (
                    id BIGINT IDENTITY(1,1) PRIMARY KEY,
                    name NVARCHAR(100) NOT NULL,
                    age INT NOT NULL
                )",
            DatabaseType.SQLite => @"
                CREATE TABLE return_id_users (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL,
                    age INTEGER NOT NULL
                )",
            _ => throw new NotSupportedException($"Database type {dbType} is not supported"),
        };
    }

    private static SqlDialect GetDialect(DatabaseType dbType)
    {
        return dbType switch
        {
            DatabaseType.MySQL => SqlDefine.MySql,
            DatabaseType.PostgreSQL => SqlDefine.PostgreSql,
            DatabaseType.SqlServer => SqlDefine.SqlServer,
            DatabaseType.SQLite => SqlDefine.SQLite,
            _ => throw new NotSupportedException($"Database type {dbType} is not supported"),
        };
    }

    private static async Task RunAsyncScenario(DatabaseType dbType, ReturnInsertedIdE2ETests test)
    {
        await using var fixture = await test.CreateFixtureAsync(dbType);
        await fixture.CreateSchemaAsync(GetSchema(dbType));
        var repo = new ReturnInsertedIdRepository(fixture.Connection, GetDialect(dbType));

        var firstId = await repo.InsertAndReturnIdAsync("Alice", 25, default);
        var secondId = await repo.InsertAndReturnIdAsync("Bob", 30, default);
        var inserted = await repo.GetByIdAsync(secondId, default);

        Assert.AreEqual(1L, firstId);
        Assert.AreEqual(2L, secondId);
        Assert.IsNotNull(inserted);
        Assert.AreEqual("Bob", inserted!.Name);
        Assert.AreEqual(30, inserted.Age);
    }

    private static async Task RunSyncScenario(DatabaseType dbType, ReturnInsertedIdE2ETests test)
    {
        await using var fixture = await test.CreateFixtureAsync(dbType);
        await fixture.CreateSchemaAsync(GetSchema(dbType));
        var repo = new ReturnInsertedIdRepository(fixture.Connection, GetDialect(dbType));

        var firstId = repo.InsertAndReturnId("Carol", 28);
        var secondId = repo.InsertAndReturnId("Dave", 33);
        var inserted = await repo.GetByIdAsync(secondId, default);

        Assert.AreEqual(1L, firstId);
        Assert.AreEqual(2L, secondId);
        Assert.IsNotNull(inserted);
        Assert.AreEqual("Dave", inserted!.Name);
        Assert.AreEqual(33, inserted.Age);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Repository")]
    [TestCategory("ReturnInsertedId")]
    [TestCategory("MySQL")]
    public async Task MySQL_ReturnInsertedId_Async_ReturnsGeneratedIds()
    {
        await RunAsyncScenario(DatabaseType.MySQL, this);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Repository")]
    [TestCategory("ReturnInsertedId")]
    [TestCategory("MySQL")]
    public async Task MySQL_ReturnInsertedId_Sync_ReturnsGeneratedIds()
    {
        await RunSyncScenario(DatabaseType.MySQL, this);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Repository")]
    [TestCategory("ReturnInsertedId")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_ReturnInsertedId_Async_ReturnsGeneratedIds()
    {
        await RunAsyncScenario(DatabaseType.PostgreSQL, this);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Repository")]
    [TestCategory("ReturnInsertedId")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_ReturnInsertedId_Sync_ReturnsGeneratedIds()
    {
        await RunSyncScenario(DatabaseType.PostgreSQL, this);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Repository")]
    [TestCategory("ReturnInsertedId")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_ReturnInsertedId_Async_ReturnsGeneratedIds()
    {
        await RunAsyncScenario(DatabaseType.SqlServer, this);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Repository")]
    [TestCategory("ReturnInsertedId")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_ReturnInsertedId_Sync_ReturnsGeneratedIds()
    {
        await RunSyncScenario(DatabaseType.SqlServer, this);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Repository")]
    [TestCategory("ReturnInsertedId")]
    [TestCategory("SQLite")]
    public async Task SQLite_ReturnInsertedId_Async_ReturnsGeneratedIds()
    {
        await RunAsyncScenario(DatabaseType.SQLite, this);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Repository")]
    [TestCategory("ReturnInsertedId")]
    [TestCategory("SQLite")]
    public async Task SQLite_ReturnInsertedId_Sync_ReturnsGeneratedIds()
    {
        await RunSyncScenario(DatabaseType.SQLite, this);
    }
}
