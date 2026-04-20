// <copyright file="SqlxVarE2ETests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using System.Data.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;
using Sqlx.Tests.E2E.Infrastructure;

namespace Sqlx.Tests.E2E.SqlTemplate;

[Sqlx]
public class TenantScopedUser
{
    public long Id { get; set; }
    public int TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
}

public interface ITenantScopedRepository
{
    [SqlTemplate("SELECT id, tenant_id, name FROM {{table}} WHERE tenant_id = {{var --name tenantId}} ORDER BY id")]
    Task<List<TenantScopedUser>> GetCurrentTenantUsersAsync();

    [SqlTemplate("SELECT id, tenant_id, name FROM {{table}} WHERE tenant_id = {{var --name tenantId}} AND id = @id")]
    Task<TenantScopedUser?> GetCurrentTenantUserByIdAsync(long id);

    [SqlTemplate("UPDATE {{table}} SET name = @name WHERE tenant_id = {{var --name tenantId}} AND id = @id")]
    Task<int> RenameCurrentTenantUserAsync(long id, string name);
}

[RepositoryFor(typeof(ITenantScopedRepository), TableName = "tenant_users")]
public partial class MySqlTenantScopedRepository : ITenantScopedRepository
{
    private readonly DbConnection _connection;

    public MySqlTenantScopedRepository(DbConnection connection, SqlDialect dialect)
    {
        _connection = connection;
        _dialect = dialect;
    }

    [SqlxVar("tenantId")]
    private static string GetTenantId() => "1";
}

[RepositoryFor(typeof(ITenantScopedRepository), TableName = "tenant_users")]
public partial class PostgreSqlTenantScopedRepository : ITenantScopedRepository
{
    private readonly DbConnection _connection;

    public PostgreSqlTenantScopedRepository(DbConnection connection, SqlDialect dialect)
    {
        _connection = connection;
        _dialect = dialect;
    }

    [SqlxVar("tenantId")]
    private static string GetTenantId() => "1";
}

[RepositoryFor(typeof(ITenantScopedRepository), TableName = "tenant_users")]
public partial class SqlServerTenantScopedRepository : ITenantScopedRepository
{
    private readonly DbConnection _connection;

    public SqlServerTenantScopedRepository(DbConnection connection, SqlDialect dialect)
    {
        _connection = connection;
        _dialect = dialect;
    }

    [SqlxVar("tenantId")]
    private static string GetTenantId() => "1";
}

[RepositoryFor(typeof(ITenantScopedRepository), TableName = "tenant_users")]
public partial class SqliteTenantScopedRepository : ITenantScopedRepository
{
    private readonly DbConnection _connection;

    public SqliteTenantScopedRepository(DbConnection connection, SqlDialect dialect)
    {
        _connection = connection;
        _dialect = dialect;
    }

    [SqlxVar("tenantId")]
    private static string GetTenantId() => "1";
}

[TestClass]
public class SqlxVarE2ETests : E2ETestBase
{
    private static string GetSchema(DatabaseType dbType)
    {
        return dbType switch
        {
            DatabaseType.MySQL => @"
                CREATE TABLE tenant_users (
                    id BIGINT AUTO_INCREMENT PRIMARY KEY,
                    tenant_id INT NOT NULL,
                    name VARCHAR(100) NOT NULL
                )",
            DatabaseType.PostgreSQL => @"
                CREATE TABLE tenant_users (
                    id BIGSERIAL PRIMARY KEY,
                    tenant_id INT NOT NULL,
                    name VARCHAR(100) NOT NULL
                )",
            DatabaseType.SqlServer => @"
                CREATE TABLE tenant_users (
                    id BIGINT IDENTITY(1,1) PRIMARY KEY,
                    tenant_id INT NOT NULL,
                    name NVARCHAR(100) NOT NULL
                )",
            DatabaseType.SQLite => @"
                CREATE TABLE tenant_users (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    tenant_id INTEGER NOT NULL,
                    name TEXT NOT NULL
                )",
            _ => throw new NotSupportedException($"Database type {dbType} is not supported"),
        };
    }

    private static async Task SeedUsersAsync(DbConnection connection)
    {
        var users = new[]
        {
            (1, "Tenant 1 - Alice"),
            (1, "Tenant 1 - Bob"),
            (2, "Tenant 2 - Charlie"),
        };

        foreach (var (tenantId, name) in users)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "INSERT INTO tenant_users (tenant_id, name) VALUES (@tenantId, @name)";
            AddParameter(command, "@tenantId", tenantId);
            AddParameter(command, "@name", name);
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

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxVar")]
    [TestCategory("MySQL")]
    public async Task MySQL_SqlxVar_FiltersCurrentTenantRows()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetSchema(DatabaseType.MySQL));
        await SeedUsersAsync(fixture.Connection);
        var repo = new MySqlTenantScopedRepository(fixture.Connection, SqlDefine.MySql);

        var users = await repo.GetCurrentTenantUsersAsync();

        Assert.AreEqual(2, users.Count);
        Assert.IsTrue(users.All(x => x.TenantId == 1));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxVar")]
    [TestCategory("MySQL")]
    public async Task MySQL_SqlxVar_WithRegularParameter_RestrictsByTenantAndId()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetSchema(DatabaseType.MySQL));
        await SeedUsersAsync(fixture.Connection);
        var repo = new MySqlTenantScopedRepository(fixture.Connection, SqlDefine.MySql);

        var allowed = await repo.GetCurrentTenantUserByIdAsync(1);
        var denied = await repo.GetCurrentTenantUserByIdAsync(3);

        Assert.IsNotNull(allowed);
        Assert.IsNull(denied);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxVar")]
    [TestCategory("MySQL")]
    public async Task MySQL_SqlxVar_Update_OnlyTouchesCurrentTenantRows()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetSchema(DatabaseType.MySQL));
        await SeedUsersAsync(fixture.Connection);
        var repo = new MySqlTenantScopedRepository(fixture.Connection, SqlDefine.MySql);

        var affected = await repo.RenameCurrentTenantUserAsync(1, "Tenant 1 - Renamed");
        var updated = await repo.GetCurrentTenantUserByIdAsync(1);

        Assert.AreEqual(1, affected);
        Assert.IsNotNull(updated);
        Assert.AreEqual("Tenant 1 - Renamed", updated!.Name);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxVar")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_SqlxVar_FiltersCurrentTenantRows()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetSchema(DatabaseType.PostgreSQL));
        await SeedUsersAsync(fixture.Connection);
        var repo = new PostgreSqlTenantScopedRepository(fixture.Connection, SqlDefine.PostgreSql);

        var users = await repo.GetCurrentTenantUsersAsync();

        Assert.AreEqual(2, users.Count);
        Assert.IsTrue(users.All(x => x.TenantId == 1));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxVar")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_SqlxVar_WithRegularParameter_RestrictsByTenantAndId()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetSchema(DatabaseType.PostgreSQL));
        await SeedUsersAsync(fixture.Connection);
        var repo = new PostgreSqlTenantScopedRepository(fixture.Connection, SqlDefine.PostgreSql);

        var allowed = await repo.GetCurrentTenantUserByIdAsync(1);
        var denied = await repo.GetCurrentTenantUserByIdAsync(3);

        Assert.IsNotNull(allowed);
        Assert.IsNull(denied);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxVar")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_SqlxVar_Update_OnlyTouchesCurrentTenantRows()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetSchema(DatabaseType.PostgreSQL));
        await SeedUsersAsync(fixture.Connection);
        var repo = new PostgreSqlTenantScopedRepository(fixture.Connection, SqlDefine.PostgreSql);

        var affected = await repo.RenameCurrentTenantUserAsync(1, "Tenant 1 - Renamed");
        var updated = await repo.GetCurrentTenantUserByIdAsync(1);

        Assert.AreEqual(1, affected);
        Assert.IsNotNull(updated);
        Assert.AreEqual("Tenant 1 - Renamed", updated!.Name);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxVar")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_SqlxVar_FiltersCurrentTenantRows()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetSchema(DatabaseType.SqlServer));
        await SeedUsersAsync(fixture.Connection);
        var repo = new SqlServerTenantScopedRepository(fixture.Connection, SqlDefine.SqlServer);

        var users = await repo.GetCurrentTenantUsersAsync();

        Assert.AreEqual(2, users.Count);
        Assert.IsTrue(users.All(x => x.TenantId == 1));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxVar")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_SqlxVar_WithRegularParameter_RestrictsByTenantAndId()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetSchema(DatabaseType.SqlServer));
        await SeedUsersAsync(fixture.Connection);
        var repo = new SqlServerTenantScopedRepository(fixture.Connection, SqlDefine.SqlServer);

        var allowed = await repo.GetCurrentTenantUserByIdAsync(1);
        var denied = await repo.GetCurrentTenantUserByIdAsync(3);

        Assert.IsNotNull(allowed);
        Assert.IsNull(denied);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxVar")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_SqlxVar_Update_OnlyTouchesCurrentTenantRows()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetSchema(DatabaseType.SqlServer));
        await SeedUsersAsync(fixture.Connection);
        var repo = new SqlServerTenantScopedRepository(fixture.Connection, SqlDefine.SqlServer);

        var affected = await repo.RenameCurrentTenantUserAsync(1, "Tenant 1 - Renamed");
        var updated = await repo.GetCurrentTenantUserByIdAsync(1);

        Assert.AreEqual(1, affected);
        Assert.IsNotNull(updated);
        Assert.AreEqual("Tenant 1 - Renamed", updated!.Name);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxVar")]
    [TestCategory("SQLite")]
    public async Task SQLite_SqlxVar_FiltersCurrentTenantRows()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetSchema(DatabaseType.SQLite));
        await SeedUsersAsync(fixture.Connection);
        var repo = new SqliteTenantScopedRepository(fixture.Connection, SqlDefine.SQLite);

        var users = await repo.GetCurrentTenantUsersAsync();

        Assert.AreEqual(2, users.Count);
        Assert.IsTrue(users.All(x => x.TenantId == 1));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxVar")]
    [TestCategory("SQLite")]
    public async Task SQLite_SqlxVar_WithRegularParameter_RestrictsByTenantAndId()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetSchema(DatabaseType.SQLite));
        await SeedUsersAsync(fixture.Connection);
        var repo = new SqliteTenantScopedRepository(fixture.Connection, SqlDefine.SQLite);

        var allowed = await repo.GetCurrentTenantUserByIdAsync(1);
        var denied = await repo.GetCurrentTenantUserByIdAsync(3);

        Assert.IsNotNull(allowed);
        Assert.IsNull(denied);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxVar")]
    [TestCategory("SQLite")]
    public async Task SQLite_SqlxVar_Update_OnlyTouchesCurrentTenantRows()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetSchema(DatabaseType.SQLite));
        await SeedUsersAsync(fixture.Connection);
        var repo = new SqliteTenantScopedRepository(fixture.Connection, SqlDefine.SQLite);

        var affected = await repo.RenameCurrentTenantUserAsync(1, "Tenant 1 - Renamed");
        var updated = await repo.GetCurrentTenantUserByIdAsync(1);

        Assert.AreEqual(1, affected);
        Assert.IsNotNull(updated);
        Assert.AreEqual("Tenant 1 - Renamed", updated!.Name);
    }
}
