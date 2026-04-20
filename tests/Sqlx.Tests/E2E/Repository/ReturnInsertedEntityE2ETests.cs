// <copyright file="ReturnInsertedEntityE2ETests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using System.Data.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;
using Sqlx.Tests.E2E.Infrastructure;

namespace Sqlx.Tests.E2E.Repository;

[Sqlx]
public class ReturnInsertedEntityTestEntity
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
}

public interface IReturnInsertedEntityRepository
{
    [SqlTemplate("INSERT INTO return_entity_users (name, age) VALUES (@name, @age)")]
    [ReturnInsertedEntity]
    ReturnInsertedEntityTestEntity InsertAndReturn(ReturnInsertedEntityTestEntity entity);

    [SqlTemplate("INSERT INTO return_entity_users (name, age) VALUES (@name, @age)")]
    [ReturnInsertedEntity]
    Task<ReturnInsertedEntityTestEntity> InsertAndReturnAsync(ReturnInsertedEntityTestEntity entity, CancellationToken cancellationToken = default);

    [SqlTemplate("SELECT id, name, age FROM return_entity_users WHERE id = @id")]
    Task<ReturnInsertedEntityTestEntity?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
}

[RepositoryFor(typeof(IReturnInsertedEntityRepository), TableName = "return_entity_users")]
public partial class ReturnInsertedEntityRepository : IReturnInsertedEntityRepository
{
    private readonly DbConnection _connection;

    public ReturnInsertedEntityRepository(DbConnection connection, SqlDialect dialect)
    {
        _connection = connection;
        _dialect = dialect;
    }
}

[TestClass]
public class ReturnInsertedEntityE2ETests : E2ETestBase
{
    private static string GetSchema(DatabaseType dbType)
    {
        return dbType switch
        {
            DatabaseType.MySQL => @"
                CREATE TABLE return_entity_users (
                    id BIGINT AUTO_INCREMENT PRIMARY KEY,
                    name VARCHAR(100) NOT NULL,
                    age INT NOT NULL
                )",
            DatabaseType.PostgreSQL => @"
                CREATE TABLE return_entity_users (
                    id BIGSERIAL PRIMARY KEY,
                    name VARCHAR(100) NOT NULL,
                    age INT NOT NULL
                )",
            DatabaseType.SqlServer => @"
                CREATE TABLE return_entity_users (
                    id BIGINT IDENTITY(1,1) PRIMARY KEY,
                    name NVARCHAR(100) NOT NULL,
                    age INT NOT NULL
                )",
            DatabaseType.SQLite => @"
                CREATE TABLE return_entity_users (
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

    private static async Task RunAsyncScenario(DatabaseType dbType, ReturnInsertedEntityE2ETests test)
    {
        await using var fixture = await test.CreateFixtureAsync(dbType);
        await fixture.CreateSchemaAsync(GetSchema(dbType));
        var repo = new ReturnInsertedEntityRepository(fixture.Connection, GetDialect(dbType));

        var original = new ReturnInsertedEntityTestEntity { Name = "Alice", Age = 25 };
        var returned = await repo.InsertAndReturnAsync(original, default);

        Assert.AreSame(original, returned);
        Assert.AreEqual(1L, returned.Id);
        Assert.AreEqual("Alice", returned.Name);
        Assert.AreEqual(25, returned.Age);

        var stored = await repo.GetByIdAsync(returned.Id, default);
        Assert.IsNotNull(stored);
        Assert.AreEqual("Alice", stored!.Name);
    }

    private static async Task RunSyncScenario(DatabaseType dbType, ReturnInsertedEntityE2ETests test)
    {
        await using var fixture = await test.CreateFixtureAsync(dbType);
        await fixture.CreateSchemaAsync(GetSchema(dbType));
        var repo = new ReturnInsertedEntityRepository(fixture.Connection, GetDialect(dbType));

        var original = new ReturnInsertedEntityTestEntity { Name = "Bob", Age = 30 };
        var returned = repo.InsertAndReturn(original);

        Assert.AreSame(original, returned);
        Assert.AreEqual(1L, returned.Id);
        Assert.AreEqual("Bob", returned.Name);
        Assert.AreEqual(30, returned.Age);

        var stored = await repo.GetByIdAsync(returned.Id, default);
        Assert.IsNotNull(stored);
        Assert.AreEqual("Bob", stored!.Name);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Repository")]
    [TestCategory("ReturnInsertedEntity")]
    [TestCategory("MySQL")]
    public async Task MySQL_ReturnInsertedEntity_Async_ReturnsSameEntityWithId()
    {
        await RunAsyncScenario(DatabaseType.MySQL, this);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Repository")]
    [TestCategory("ReturnInsertedEntity")]
    [TestCategory("MySQL")]
    public async Task MySQL_ReturnInsertedEntity_Sync_ReturnsSameEntityWithId()
    {
        await RunSyncScenario(DatabaseType.MySQL, this);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Repository")]
    [TestCategory("ReturnInsertedEntity")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_ReturnInsertedEntity_Async_ReturnsSameEntityWithId()
    {
        await RunAsyncScenario(DatabaseType.PostgreSQL, this);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Repository")]
    [TestCategory("ReturnInsertedEntity")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_ReturnInsertedEntity_Sync_ReturnsSameEntityWithId()
    {
        await RunSyncScenario(DatabaseType.PostgreSQL, this);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Repository")]
    [TestCategory("ReturnInsertedEntity")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_ReturnInsertedEntity_Async_ReturnsSameEntityWithId()
    {
        await RunAsyncScenario(DatabaseType.SqlServer, this);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Repository")]
    [TestCategory("ReturnInsertedEntity")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_ReturnInsertedEntity_Sync_ReturnsSameEntityWithId()
    {
        await RunSyncScenario(DatabaseType.SqlServer, this);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Repository")]
    [TestCategory("ReturnInsertedEntity")]
    [TestCategory("SQLite")]
    public async Task SQLite_ReturnInsertedEntity_Async_ReturnsSameEntityWithId()
    {
        await RunAsyncScenario(DatabaseType.SQLite, this);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Repository")]
    [TestCategory("ReturnInsertedEntity")]
    [TestCategory("SQLite")]
    public async Task SQLite_ReturnInsertedEntity_Sync_ReturnsSameEntityWithId()
    {
        await RunSyncScenario(DatabaseType.SQLite, this);
    }
}
