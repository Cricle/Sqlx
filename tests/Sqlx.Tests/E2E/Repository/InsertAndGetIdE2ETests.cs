// <copyright file="InsertAndGetIdE2ETests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using System.Data.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;
using Sqlx.Tests.E2E.Infrastructure;

namespace Sqlx.Tests.E2E.Repository;

[Sqlx]
public class InsertAndGetIdEntity
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
}

public interface IInsertAndGetIdRepository : ICrudRepository<InsertAndGetIdEntity, long>
{
}

[RepositoryFor(typeof(IInsertAndGetIdRepository), TableName = "insert_users")]
public partial class InsertAndGetIdRepository : IInsertAndGetIdRepository
{
    private readonly DbConnection _connection;

    public InsertAndGetIdRepository(DbConnection connection, SqlDialect dialect)
    {
        _connection = connection;
        _dialect = dialect;
    }
}

[TestClass]
public class InsertAndGetIdE2ETests : E2ETestBase
{
    private static string GetSchema(DatabaseType dbType)
    {
        return dbType switch
        {
            DatabaseType.MySQL => @"
                CREATE TABLE insert_users (
                    id BIGINT AUTO_INCREMENT PRIMARY KEY,
                    name VARCHAR(100) NOT NULL,
                    age INT NOT NULL
                )",
            DatabaseType.PostgreSQL => @"
                CREATE TABLE insert_users (
                    id BIGSERIAL PRIMARY KEY,
                    name VARCHAR(100) NOT NULL,
                    age INT NOT NULL
                )",
            DatabaseType.SqlServer => @"
                CREATE TABLE insert_users (
                    id BIGINT IDENTITY(1,1) PRIMARY KEY,
                    name NVARCHAR(100) NOT NULL,
                    age INT NOT NULL
                )",
            DatabaseType.SQLite => @"
                CREATE TABLE insert_users (
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

    private static async Task RunAsyncInsertScenario(DatabaseType dbType, InsertAndGetIdE2ETests test)
    {
        await using var fixture = await test.CreateFixtureAsync(dbType);
        await fixture.CreateSchemaAsync(GetSchema(dbType));
        var repo = new InsertAndGetIdRepository(fixture.Connection, GetDialect(dbType));

        var firstId = await repo.InsertAndGetIdAsync(new InsertAndGetIdEntity { Name = "Alice", Age = 25 }, default);
        var secondId = await repo.InsertAndGetIdAsync(new InsertAndGetIdEntity { Name = "Bob", Age = 30 }, default);

        Assert.AreEqual(1L, firstId);
        Assert.AreEqual(2L, secondId);

        var inserted = await repo.GetByIdAsync(secondId, default);
        Assert.IsNotNull(inserted);
        Assert.AreEqual("Bob", inserted!.Name);
        Assert.AreEqual(30, inserted.Age);
    }

    private static Task RunSyncInsertScenario(DatabaseType dbType, InsertAndGetIdE2ETests test)
    {
        return RunSyncInsertScenarioCore(dbType, test);
    }

    private static async Task RunSyncInsertScenarioCore(DatabaseType dbType, InsertAndGetIdE2ETests test)
    {
        await using var fixture = await test.CreateFixtureAsync(dbType);
        await fixture.CreateSchemaAsync(GetSchema(dbType));
        var repo = new InsertAndGetIdRepository(fixture.Connection, GetDialect(dbType));

        var firstId = repo.InsertAndGetId(new InsertAndGetIdEntity { Name = "Carol", Age = 28 });
        var secondId = repo.InsertAndGetId(new InsertAndGetIdEntity { Name = "Dave", Age = 33 });

        Assert.AreEqual(1L, firstId);
        Assert.AreEqual(2L, secondId);

        var inserted = repo.GetById(secondId);
        Assert.IsNotNull(inserted);
        Assert.AreEqual("Dave", inserted!.Name);
        Assert.AreEqual(33, inserted.Age);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Repository")]
    [TestCategory("InsertAndGetId")]
    [TestCategory("MySQL")]
    public async Task MySQL_InsertAndGetIdAsync_ReturnsGeneratedIds()
    {
        await RunAsyncInsertScenario(DatabaseType.MySQL, this);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Repository")]
    [TestCategory("InsertAndGetId")]
    [TestCategory("MySQL")]
    public async Task MySQL_InsertAndGetId_Sync_ReturnsGeneratedIds()
    {
        await RunSyncInsertScenario(DatabaseType.MySQL, this);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Repository")]
    [TestCategory("InsertAndGetId")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_InsertAndGetIdAsync_ReturnsGeneratedIds()
    {
        await RunAsyncInsertScenario(DatabaseType.PostgreSQL, this);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Repository")]
    [TestCategory("InsertAndGetId")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_InsertAndGetId_Sync_ReturnsGeneratedIds()
    {
        await RunSyncInsertScenario(DatabaseType.PostgreSQL, this);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Repository")]
    [TestCategory("InsertAndGetId")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_InsertAndGetIdAsync_ReturnsGeneratedIds()
    {
        await RunAsyncInsertScenario(DatabaseType.SqlServer, this);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Repository")]
    [TestCategory("InsertAndGetId")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_InsertAndGetId_Sync_ReturnsGeneratedIds()
    {
        await RunSyncInsertScenario(DatabaseType.SqlServer, this);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Repository")]
    [TestCategory("InsertAndGetId")]
    [TestCategory("SQLite")]
    public async Task SQLite_InsertAndGetIdAsync_ReturnsGeneratedIds()
    {
        await RunAsyncInsertScenario(DatabaseType.SQLite, this);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("Repository")]
    [TestCategory("InsertAndGetId")]
    [TestCategory("SQLite")]
    public async Task SQLite_InsertAndGetId_Sync_ReturnsGeneratedIds()
    {
        await RunSyncInsertScenario(DatabaseType.SQLite, this);
    }
}
