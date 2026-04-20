// <copyright file="DbConnectionLightweightE2ETests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using System.Data.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;
using Sqlx.Tests.E2E.Infrastructure;

namespace Sqlx.Tests.E2E.SqlxCore;

[TestClass]
public class DbConnectionLightweightE2ETests : E2ETestBase
{
    [Sqlx]
    [TableName("light_users")]
    public class LightUser
    {
        public long Id { get; set; }
        public int TenantId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
    }

    public class LightUserDto
    {
        public long Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
    }

    private static string GetSchema(DatabaseType dbType)
    {
        return dbType switch
        {
            DatabaseType.MySQL => @"
                CREATE TABLE light_users (
                    id BIGINT AUTO_INCREMENT PRIMARY KEY,
                    tenant_id INT NOT NULL,
                    name VARCHAR(100) NOT NULL,
                    age INT NOT NULL
                )",
            DatabaseType.PostgreSQL => @"
                CREATE TABLE light_users (
                    id BIGSERIAL PRIMARY KEY,
                    tenant_id INT NOT NULL,
                    name VARCHAR(100) NOT NULL,
                    age INT NOT NULL
                )",
            DatabaseType.SqlServer => @"
                CREATE TABLE light_users (
                    id BIGINT IDENTITY(1,1) PRIMARY KEY,
                    tenant_id INT NOT NULL,
                    name NVARCHAR(100) NOT NULL,
                    age INT NOT NULL
                )",
            DatabaseType.SQLite => @"
                CREATE TABLE light_users (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    tenant_id INTEGER NOT NULL,
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

    private static async Task SeedUsersAsync(DbConnection connection)
    {
        var users = new[]
        {
            (1, "Alice", 25),
            (1, "Bob", 31),
            (2, "Charlie", 40),
        };

        foreach (var (tenantId, name, age) in users)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "INSERT INTO light_users (tenant_id, name, age) VALUES (@tenantId, @name, @age)";
            AddParameter(command, "@tenantId", tenantId);
            AddParameter(command, "@name", name);
            AddParameter(command, "@age", age);
            await command.ExecuteNonQueryAsync();
        }
    }

    private static void AddParameter(DbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    [DataTestMethod]
    [DataRow(DatabaseType.MySQL)]
    [DataRow(DatabaseType.PostgreSQL)]
    [DataRow(DatabaseType.SqlServer)]
    [DataRow(DatabaseType.SQLite)]
    [TestCategory("E2E")]
    [TestCategory("LightweightApi")]
    public async Task LightweightApi_QueryAsync_ReturnsFilteredEntities(DatabaseType dbType)
    {
        await using var fixture = await CreateFixtureAsync(dbType);
        await fixture.CreateSchemaAsync(GetSchema(dbType));
        await SeedUsersAsync(fixture.Connection);

        var users = await fixture.Connection.SqlxQueryAsync<LightUser>(
            "SELECT {{columns}} FROM {{table}} WHERE tenant_id = @tenantId ORDER BY id",
            GetDialect(dbType),
            new { tenantId = 1 });

        Assert.AreEqual(2, users.Count);
        Assert.IsTrue(users.All(x => x.TenantId == 1));
        CollectionAssert.AreEqual(new[] { "Alice", "Bob" }, users.Select(x => x.Name).ToArray());
    }

    [DataTestMethod]
    [DataRow(DatabaseType.MySQL)]
    [DataRow(DatabaseType.PostgreSQL)]
    [DataRow(DatabaseType.SqlServer)]
    [DataRow(DatabaseType.SQLite)]
    [TestCategory("E2E")]
    [TestCategory("LightweightApi")]
    public async Task LightweightApi_QueryFirstOrDefaultAsync_ReturnsSingleEntity(DatabaseType dbType)
    {
        await using var fixture = await CreateFixtureAsync(dbType);
        await fixture.CreateSchemaAsync(GetSchema(dbType));
        await SeedUsersAsync(fixture.Connection);

        var user = await fixture.Connection.SqlxQueryFirstOrDefaultAsync<LightUser>(
            "SELECT {{columns}} FROM {{table}} WHERE name = @name",
            GetDialect(dbType),
            new { name = "Charlie" });

        Assert.IsNotNull(user);
        Assert.AreEqual("Charlie", user!.Name);
        Assert.AreEqual(2, user.TenantId);
    }

    [DataTestMethod]
    [DataRow(DatabaseType.MySQL)]
    [DataRow(DatabaseType.PostgreSQL)]
    [DataRow(DatabaseType.SqlServer)]
    [DataRow(DatabaseType.SQLite)]
    [TestCategory("E2E")]
    [TestCategory("LightweightApi")]
    public async Task LightweightApi_QuerySingle_SyncPath_ReturnsSingleEntity(DatabaseType dbType)
    {
        await using var fixture = await CreateFixtureAsync(dbType);
        await fixture.CreateSchemaAsync(GetSchema(dbType));
        await SeedUsersAsync(fixture.Connection);

        var user = fixture.Connection.SqlxQuerySingle<LightUser>(
            "SELECT {{columns}} FROM {{table}} WHERE id = @id",
            GetDialect(dbType),
            new { id = 2L });

        Assert.AreEqual("Bob", user.Name);
        Assert.AreEqual(1, user.TenantId);
        Assert.AreEqual(31, user.Age);
    }

    [DataTestMethod]
    [DataRow(DatabaseType.MySQL)]
    [DataRow(DatabaseType.PostgreSQL)]
    [DataRow(DatabaseType.SqlServer)]
    [DataRow(DatabaseType.SQLite)]
    [TestCategory("E2E")]
    [TestCategory("LightweightApi")]
    public async Task LightweightApi_QueryAsync_WithDtoProjection_ReturnsProjectedRows(DatabaseType dbType)
    {
        await using var fixture = await CreateFixtureAsync(dbType);
        await fixture.CreateSchemaAsync(GetSchema(dbType));
        await SeedUsersAsync(fixture.Connection);

        var users = await fixture.Connection.SqlxQueryAsync<LightUserDto, LightUser>(
            "SELECT id AS Id, name AS DisplayName FROM {{table}} WHERE age >= @minAge ORDER BY id",
            GetDialect(dbType),
            new { minAge = 30 });

        Assert.AreEqual(2, users.Count);
        CollectionAssert.AreEqual(new[] { "Bob", "Charlie" }, users.Select(x => x.DisplayName).ToArray());
    }

    [DataTestMethod]
    [DataRow(DatabaseType.MySQL)]
    [DataRow(DatabaseType.PostgreSQL)]
    [DataRow(DatabaseType.SqlServer)]
    [DataRow(DatabaseType.SQLite)]
    [TestCategory("E2E")]
    [TestCategory("LightweightApi")]
    public async Task LightweightApi_ExecuteAsync_UpdatesMatchingRow(DatabaseType dbType)
    {
        await using var fixture = await CreateFixtureAsync(dbType);
        await fixture.CreateSchemaAsync(GetSchema(dbType));
        await SeedUsersAsync(fixture.Connection);

        var affected = await fixture.Connection.SqlxExecuteAsync<LightUser>(
            "UPDATE {{table}} SET tenant_id = @tenantId, name = @name, age = @age WHERE id = @id",
            GetDialect(dbType),
            new { id = 2L, tenantId = 1, name = "Bobby", age = 32 });

        Assert.AreEqual(1, affected);

        var updated = await fixture.Connection.SqlxQuerySingleAsync<LightUser>(
            "SELECT {{columns}} FROM {{table}} WHERE id = @id",
            GetDialect(dbType),
            new { id = 2L });

        Assert.AreEqual("Bobby", updated.Name);
        Assert.AreEqual(32, updated.Age);
    }

    [DataTestMethod]
    [DataRow(DatabaseType.MySQL)]
    [DataRow(DatabaseType.PostgreSQL)]
    [DataRow(DatabaseType.SqlServer)]
    [DataRow(DatabaseType.SQLite)]
    [TestCategory("E2E")]
    [TestCategory("LightweightApi")]
    public async Task LightweightApi_QueryAsync_WithTransaction_SeesUncommittedRow(DatabaseType dbType)
    {
        await using var fixture = await CreateFixtureAsync(dbType);
        await fixture.CreateSchemaAsync(GetSchema(dbType));
        await SeedUsersAsync(fixture.Connection);

        await using var transaction = await fixture.Connection.BeginTransactionAsync();
        using (var command = fixture.Connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = "INSERT INTO light_users (tenant_id, name, age) VALUES (@tenantId, @name, @age)";
            AddParameter(command, "@tenantId", 1);
            AddParameter(command, "@name", "Transient");
            AddParameter(command, "@age", 29);
            await command.ExecuteNonQueryAsync();
        }

        var users = await fixture.Connection.SqlxQueryAsync<LightUser>(
            "SELECT {{columns}} FROM {{table}} WHERE name = @name",
            GetDialect(dbType),
            new { name = "Transient" },
            transaction);

        Assert.AreEqual(1, users.Count);
        Assert.AreEqual("Transient", users[0].Name);
        await transaction.RollbackAsync();
    }

    [DataTestMethod]
    [DataRow(DatabaseType.MySQL)]
    [DataRow(DatabaseType.PostgreSQL)]
    [DataRow(DatabaseType.SqlServer)]
    [DataRow(DatabaseType.SQLite)]
    [TestCategory("E2E")]
    [TestCategory("LightweightApi")]
    public async Task LightweightApi_ScalarAsync_ReturnsCount(DatabaseType dbType)
    {
        await using var fixture = await CreateFixtureAsync(dbType);
        await fixture.CreateSchemaAsync(GetSchema(dbType));
        await SeedUsersAsync(fixture.Connection);

        var count = await fixture.Connection.SqlxScalarAsync<long, LightUser>(
            "SELECT COUNT(*) FROM {{table}} WHERE tenant_id = @tenantId",
            GetDialect(dbType),
            new { tenantId = 1 });

        Assert.AreEqual(2L, count);
    }
}
