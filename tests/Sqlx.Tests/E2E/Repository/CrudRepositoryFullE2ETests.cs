// <copyright file="CrudRepositoryFullE2ETests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using System.Data.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using Sqlx.Annotations;
using Sqlx.Tests.E2E.Infrastructure;

namespace Sqlx.Tests.E2E.Repository;

// ── Entity & Repository ───────────────────────────────────────────────────────

[Sqlx, TableName("crud_users")]
public class CrudUser
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public bool IsActive { get; set; }
    public string? Email { get; set; }
}

public interface ICrudUserRepository : ICrudRepository<CrudUser, long>
{
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE age >= @minAge ORDER BY id")]
    Task<List<CrudUser>> GetAdultsAsync(int minAge);

    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE age >= @minAge ORDER BY id")]
    List<CrudUser> GetAdults(int minAge);
}

[RepositoryFor(typeof(ICrudUserRepository), TableName = "crud_users")]
public partial class SQLiteCrudUserRepository : ICrudUserRepository
{
    private readonly DbConnection _connection;
    public SQLiteCrudUserRepository(DbConnection c, SqlDialect d) { _connection = c; _dialect = d; }
}

[RepositoryFor(typeof(ICrudUserRepository), TableName = "crud_users")]
public partial class PostgreSqlCrudUserRepository : ICrudUserRepository
{
    private readonly DbConnection _connection;
    public PostgreSqlCrudUserRepository(DbConnection c, SqlDialect d) { _connection = c; _dialect = d; }
}

[RepositoryFor(typeof(ICrudUserRepository), TableName = "crud_users")]
public partial class MySqlCrudUserRepository : ICrudUserRepository
{
    private readonly DbConnection _connection;
    public MySqlCrudUserRepository(DbConnection c, SqlDialect d) { _connection = c; _dialect = d; }
}

[RepositoryFor(typeof(ICrudUserRepository), TableName = "crud_users")]
public partial class SqlServerCrudUserRepository : ICrudUserRepository
{
    private readonly DbConnection _connection;
    public SqlServerCrudUserRepository(DbConnection c, SqlDialect d) { _connection = c; _dialect = d; }
}

// ── Tests ─────────────────────────────────────────────────────────────────────

[TestClass]
public class CrudRepositoryFullE2ETests : E2ETestBase
{
    private static string GetSchema(DatabaseType dbType) => dbType switch
    {
        DatabaseType.MySQL => @"CREATE TABLE crud_users (
            id BIGINT AUTO_INCREMENT PRIMARY KEY,
            name VARCHAR(100) NOT NULL,
            age INT NOT NULL,
            is_active TINYINT(1) NOT NULL DEFAULT 1,
            email VARCHAR(200) NULL)",
        DatabaseType.PostgreSQL => @"CREATE TABLE crud_users (
            id BIGSERIAL PRIMARY KEY,
            name VARCHAR(100) NOT NULL,
            age INT NOT NULL,
            is_active BOOLEAN NOT NULL DEFAULT TRUE,
            email VARCHAR(200) NULL)",
        DatabaseType.SqlServer => @"CREATE TABLE crud_users (
            id BIGINT IDENTITY(1,1) PRIMARY KEY,
            name NVARCHAR(100) NOT NULL,
            age INT NOT NULL,
            is_active BIT NOT NULL DEFAULT 1,
            email NVARCHAR(200) NULL)",
        DatabaseType.SQLite => @"CREATE TABLE crud_users (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            name TEXT NOT NULL,
            age INTEGER NOT NULL,
            is_active INTEGER NOT NULL DEFAULT 1,
            email TEXT NULL)",
        _ => throw new NotSupportedException()
    };

    private static SqlDialect GetDialect(DatabaseType dbType) => dbType switch
    {
        DatabaseType.MySQL => SqlDefine.MySql,
        DatabaseType.PostgreSQL => SqlDefine.PostgreSql,
        DatabaseType.SqlServer => SqlDefine.SqlServer,
        DatabaseType.SQLite => SqlDefine.SQLite,
        _ => throw new NotSupportedException()
    };

    private static ICrudUserRepository CreateRepo(DbConnection conn, DatabaseType dbType) => dbType switch
    {
        DatabaseType.MySQL => new MySqlCrudUserRepository(conn, GetDialect(dbType)),
        DatabaseType.PostgreSQL => new PostgreSqlCrudUserRepository(conn, GetDialect(dbType)),
        DatabaseType.SqlServer => new SqlServerCrudUserRepository(conn, GetDialect(dbType)),
        DatabaseType.SQLite => new SQLiteCrudUserRepository(conn, GetDialect(dbType)),
        _ => throw new NotSupportedException()
    };

    private async Task<(IDatabaseFixture fixture, ICrudUserRepository repo)> SetupAsync(DatabaseType dbType)
    {
        var fixture = await CreateFixtureAsync(dbType);
        await fixture.CreateSchemaAsync(GetSchema(dbType));
        var repo = CreateRepo(fixture.Connection, dbType);
        return (fixture, repo);
    }

    // ── InsertAndGetId + GetById ──────────────────────────────────────────────

    [DataTestMethod]
    [DataRow(DatabaseType.SQLite)]
    [DataRow(DatabaseType.PostgreSQL)]
    [DataRow(DatabaseType.MySQL)]
    [DataRow(DatabaseType.SqlServer)]
    public async Task AllDatabases_InsertAndGetId_ThenGetById_Works(DatabaseType dbType)
    {
        var (fixture, repo) = await SetupAsync(dbType);
        await using var _ = fixture;
        var id = await repo.InsertAndGetIdAsync(new CrudUser { Name = "Alice", Age = 30, IsActive = true });
        Assert.IsTrue(id > 0);
        var user = await repo.GetByIdAsync(id);
        Assert.IsNotNull(user);
        Assert.AreEqual("Alice", user.Name);
        Assert.AreEqual(30, user.Age);
    }

    // ── Insert + GetAll ───────────────────────────────────────────────────────

    [DataTestMethod]
    [DataRow(DatabaseType.SQLite)]
    [DataRow(DatabaseType.PostgreSQL)]
    [DataRow(DatabaseType.MySQL)]
    [DataRow(DatabaseType.SqlServer)]
    public async Task AllDatabases_Insert_ThenGetAll_ReturnsAll(DatabaseType dbType)
    {
        var (fixture, repo) = await SetupAsync(dbType);
        await using var _ = fixture;
        await repo.InsertAsync(new CrudUser { Name = "Bob", Age = 25, IsActive = true });
        await repo.InsertAsync(new CrudUser { Name = "Carol", Age = 35, IsActive = false });
        var all = await repo.GetAllAsync();
        Assert.AreEqual(2, all.Count);
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [DataTestMethod]
    [DataRow(DatabaseType.SQLite)]
    [DataRow(DatabaseType.PostgreSQL)]
    [DataRow(DatabaseType.MySQL)]
    [DataRow(DatabaseType.SqlServer)]
    public async Task AllDatabases_Update_ModifiesRecord(DatabaseType dbType)
    {
        var (fixture, repo) = await SetupAsync(dbType);
        await using var _ = fixture;
        var id = await repo.InsertAndGetIdAsync(new CrudUser { Name = "Dave", Age = 20, IsActive = true });
        await repo.UpdateAsync(new CrudUser { Id = id, Name = "Dave Updated", Age = 21, IsActive = false });
        var user = await repo.GetByIdAsync(id);
        Assert.AreEqual("Dave Updated", user!.Name);
        Assert.AreEqual(21, user.Age);
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [DataTestMethod]
    [DataRow(DatabaseType.SQLite)]
    [DataRow(DatabaseType.PostgreSQL)]
    [DataRow(DatabaseType.MySQL)]
    [DataRow(DatabaseType.SqlServer)]
    public async Task AllDatabases_Delete_RemovesRecord(DatabaseType dbType)
    {
        var (fixture, repo) = await SetupAsync(dbType);
        await using var _ = fixture;
        var id = await repo.InsertAndGetIdAsync(new CrudUser { Name = "Eve", Age = 28, IsActive = true });
        await repo.DeleteAsync(id);
        var user = await repo.GetByIdAsync(id);
        Assert.IsNull(user);
    }

    // ── BatchInsert ───────────────────────────────────────────────────────────

    [DataTestMethod]
    [DataRow(DatabaseType.SQLite)]
    [DataRow(DatabaseType.PostgreSQL)]
    [DataRow(DatabaseType.MySQL)]
    [DataRow(DatabaseType.SqlServer)]
    public async Task AllDatabases_BatchInsert_InsertsAll(DatabaseType dbType)
    {
        var (fixture, repo) = await SetupAsync(dbType);
        await using var _ = fixture;
        var users = new List<CrudUser>
        {
            new() { Name = "U1", Age = 20, IsActive = true },
            new() { Name = "U2", Age = 25, IsActive = true },
            new() { Name = "U3", Age = 30, IsActive = false },
        };
        await repo.BatchInsertAsync(users);
        var all = await repo.GetAllAsync();
        Assert.AreEqual(3, all.Count);
    }

    // ── BatchUpdate ───────────────────────────────────────────────────────────

    [DataTestMethod]
    [DataRow(DatabaseType.SQLite)]
    [DataRow(DatabaseType.PostgreSQL)]
    [DataRow(DatabaseType.MySQL)]
    [DataRow(DatabaseType.SqlServer)]
    public async Task AllDatabases_BatchUpdate_UpdatesAll(DatabaseType dbType)
    {
        var (fixture, repo) = await SetupAsync(dbType);
        await using var _ = fixture;
        var id1 = await repo.InsertAndGetIdAsync(new CrudUser { Name = "A", Age = 10, IsActive = true });
        var id2 = await repo.InsertAndGetIdAsync(new CrudUser { Name = "B", Age = 20, IsActive = true });
        await repo.BatchUpdateAsync(new List<CrudUser>
        {
            new() { Id = id1, Name = "A2", Age = 11, IsActive = false },
            new() { Id = id2, Name = "B2", Age = 21, IsActive = false },
        });
        var u1 = await repo.GetByIdAsync(id1);
        Assert.AreEqual("A2", u1!.Name);
    }

    // ── GetByIds ──────────────────────────────────────────────────────────────

    [DataTestMethod]
    [DataRow(DatabaseType.SQLite)]
    [DataRow(DatabaseType.PostgreSQL)]
    [DataRow(DatabaseType.MySQL)]
    [DataRow(DatabaseType.SqlServer)]
    public async Task AllDatabases_GetByIds_ReturnsMatchingRecords(DatabaseType dbType)
    {
        var (fixture, repo) = await SetupAsync(dbType);
        await using var _ = fixture;
        var id1 = await repo.InsertAndGetIdAsync(new CrudUser { Name = "X", Age = 10, IsActive = true });
        var id2 = await repo.InsertAndGetIdAsync(new CrudUser { Name = "Y", Age = 20, IsActive = true });
        await repo.InsertAsync(new CrudUser { Name = "Z", Age = 30, IsActive = true });
        var result = await repo.GetByIdsAsync(new List<long> { id1, id2 });
        Assert.AreEqual(2, result.Count);
    }

    // ── GetWhere ──────────────────────────────────────────────────────────────

    [DataTestMethod]
    [DataRow(DatabaseType.SQLite)]
    [DataRow(DatabaseType.PostgreSQL)]
    [DataRow(DatabaseType.MySQL)]
    [DataRow(DatabaseType.SqlServer)]
    public async Task AllDatabases_GetWhere_FiltersCorrectly(DatabaseType dbType)
    {
        var (fixture, repo) = await SetupAsync(dbType);
        await using var _ = fixture;
        await repo.InsertAsync(new CrudUser { Name = "Active1", Age = 25, IsActive = true });
        await repo.InsertAsync(new CrudUser { Name = "Inactive1", Age = 30, IsActive = false });
        await repo.InsertAsync(new CrudUser { Name = "Active2", Age = 35, IsActive = true });
        var active = await repo.GetWhereAsync(u => u.IsActive);
        Assert.AreEqual(2, active.Count);
    }

    // ── GetFirstWhere ─────────────────────────────────────────────────────────

    [DataTestMethod]
    [DataRow(DatabaseType.SQLite)]
    [DataRow(DatabaseType.PostgreSQL)]
    [DataRow(DatabaseType.MySQL)]
    [DataRow(DatabaseType.SqlServer)]
    public async Task AllDatabases_GetFirstWhere_ReturnsFirstMatch(DatabaseType dbType)
    {
        var (fixture, repo) = await SetupAsync(dbType);
        await using var _ = fixture;
        await repo.InsertAsync(new CrudUser { Name = "First", Age = 18, IsActive = true });
        await repo.InsertAsync(new CrudUser { Name = "Second", Age = 25, IsActive = true });
        var first = await repo.GetFirstWhereAsync(u => u.IsActive);
        Assert.IsNotNull(first);
    }

    // ── GetPaged ──────────────────────────────────────────────────────────────

    [DataTestMethod]
    [DataRow(DatabaseType.SQLite)]
    [DataRow(DatabaseType.PostgreSQL)]
    [DataRow(DatabaseType.MySQL)]
    [DataRow(DatabaseType.SqlServer)]
    public async Task AllDatabases_GetPaged_ReturnsPaginatedResults(DatabaseType dbType)
    {
        var (fixture, repo) = await SetupAsync(dbType);
        await using var _ = fixture;
        for (int i = 1; i <= 5; i++)
            await repo.InsertAsync(new CrudUser { Name = $"User{i}", Age = 20 + i, IsActive = true });
        var page = await repo.GetPagedAsync(2, 0);
        Assert.AreEqual(2, page.Count);
    }

    // ── GetPagedWhere ─────────────────────────────────────────────────────────

    [DataTestMethod]
    [DataRow(DatabaseType.SQLite)]
    [DataRow(DatabaseType.PostgreSQL)]
    [DataRow(DatabaseType.MySQL)]
    [DataRow(DatabaseType.SqlServer)]
    public async Task AllDatabases_GetPagedWhere_ReturnsPaginatedFilteredResults(DatabaseType dbType)
    {
        var (fixture, repo) = await SetupAsync(dbType);
        await using var _ = fixture;
        for (int i = 1; i <= 6; i++)
            await repo.InsertAsync(new CrudUser { Name = $"U{i}", Age = 20 + i, IsActive = i % 2 == 0 });
        var page = await repo.GetPagedWhereAsync(u => u.IsActive, 1, 2);
        Assert.IsTrue(page.Count <= 2);
    }

    // ── ExistsById ────────────────────────────────────────────────────────────

    [DataTestMethod]
    [DataRow(DatabaseType.SQLite)]
    [DataRow(DatabaseType.PostgreSQL)]
    [DataRow(DatabaseType.MySQL)]
    [DataRow(DatabaseType.SqlServer)]
    public async Task AllDatabases_ExistsById_ReturnsTrueAndFalse(DatabaseType dbType)
    {
        var (fixture, repo) = await SetupAsync(dbType);
        await using var _ = fixture;
        var id = await repo.InsertAndGetIdAsync(new CrudUser { Name = "Exists", Age = 30, IsActive = true });
        Assert.IsTrue(await repo.ExistsByIdAsync(id));
        Assert.IsFalse(await repo.ExistsByIdAsync(99999L));
    }

    // ── Exists (predicate) ────────────────────────────────────────────────────

    [DataTestMethod]
    [DataRow(DatabaseType.SQLite)]
    [DataRow(DatabaseType.PostgreSQL)]
    [DataRow(DatabaseType.MySQL)]
    [DataRow(DatabaseType.SqlServer)]
    public async Task AllDatabases_Exists_WithPredicate_Works(DatabaseType dbType)
    {
        var (fixture, repo) = await SetupAsync(dbType);
        await using var _ = fixture;
        await repo.InsertAsync(new CrudUser { Name = "Test", Age = 40, IsActive = true });
        Assert.IsTrue(await repo.ExistsAsync(u => u.Age == 40));
        Assert.IsFalse(await repo.ExistsAsync(u => u.Age == 999));
    }

    // ── Count + CountWhere ────────────────────────────────────────────────────

    [DataTestMethod]
    [DataRow(DatabaseType.SQLite)]
    [DataRow(DatabaseType.PostgreSQL)]
    [DataRow(DatabaseType.MySQL)]
    [DataRow(DatabaseType.SqlServer)]
    public async Task AllDatabases_Count_And_CountWhere_Work(DatabaseType dbType)
    {
        var (fixture, repo) = await SetupAsync(dbType);
        await using var _ = fixture;
        await repo.InsertAsync(new CrudUser { Name = "C1", Age = 20, IsActive = true });
        await repo.InsertAsync(new CrudUser { Name = "C2", Age = 30, IsActive = false });
        Assert.AreEqual(2, await repo.CountAsync());
        Assert.AreEqual(1, await repo.CountWhereAsync(u => u.IsActive));
    }

    // ── UpdateWhere ───────────────────────────────────────────────────────────

    [DataTestMethod]
    [DataRow(DatabaseType.SQLite)]
    [DataRow(DatabaseType.PostgreSQL)]
    [DataRow(DatabaseType.MySQL)]
    [DataRow(DatabaseType.SqlServer)]
    public async Task AllDatabases_UpdateWhere_UpdatesMatchingRecords(DatabaseType dbType)
    {
        var (fixture, repo) = await SetupAsync(dbType);
        await using var _ = fixture;
        await repo.InsertAsync(new CrudUser { Name = "Old1", Age = 20, IsActive = true });
        await repo.InsertAsync(new CrudUser { Name = "Old2", Age = 25, IsActive = true });
        await repo.InsertAsync(new CrudUser { Name = "Keep", Age = 30, IsActive = false });
        await repo.UpdateWhereAsync(new CrudUser { IsActive = false, Name = "Updated", Age = 0 }, u => u.IsActive);
        Assert.AreEqual(0, await repo.CountWhereAsync(u => u.IsActive));
    }

    // ── DynamicUpdate ─────────────────────────────────────────────────────────

    [DataTestMethod]
    [DataRow(DatabaseType.SQLite)]
    [DataRow(DatabaseType.PostgreSQL)]
    [DataRow(DatabaseType.MySQL)]
    [DataRow(DatabaseType.SqlServer)]
    public async Task AllDatabases_DynamicUpdate_UpdatesSpecificFields(DatabaseType dbType)
    {
        var (fixture, repo) = await SetupAsync(dbType);
        await using var _ = fixture;
        var id = await repo.InsertAndGetIdAsync(new CrudUser { Name = "Original", Age = 20, IsActive = true });
        await repo.DynamicUpdateAsync(id, u => new CrudUser { Name = "Updated", Age = 21 });
        var user = await repo.GetByIdAsync(id);
        Assert.AreEqual("Updated", user!.Name);
        Assert.AreEqual(21, user.Age);
    }

    // ── DynamicUpdateWhere ────────────────────────────────────────────────────

    [DataTestMethod]
    [DataRow(DatabaseType.SQLite)]
    [DataRow(DatabaseType.PostgreSQL)]
    [DataRow(DatabaseType.MySQL)]
    [DataRow(DatabaseType.SqlServer)]
    public async Task AllDatabases_DynamicUpdateWhere_UpdatesMatchingRecords(DatabaseType dbType)
    {
        var (fixture, repo) = await SetupAsync(dbType);
        await using var _ = fixture;
        await repo.InsertAsync(new CrudUser { Name = "Active1", Age = 20, IsActive = true });
        await repo.InsertAsync(new CrudUser { Name = "Active2", Age = 25, IsActive = true });
        await repo.InsertAsync(new CrudUser { Name = "Inactive", Age = 30, IsActive = false });
        await repo.DynamicUpdateWhereAsync(u => new CrudUser { IsActive = false }, u => u.IsActive);
        Assert.AreEqual(0, await repo.CountWhereAsync(u => u.IsActive));
    }

    // ── DeleteByIds ───────────────────────────────────────────────────────────

    [DataTestMethod]
    [DataRow(DatabaseType.SQLite)]
    [DataRow(DatabaseType.PostgreSQL)]
    [DataRow(DatabaseType.MySQL)]
    [DataRow(DatabaseType.SqlServer)]
    public async Task AllDatabases_DeleteByIds_RemovesSpecifiedRecords(DatabaseType dbType)
    {
        var (fixture, repo) = await SetupAsync(dbType);
        await using var _ = fixture;
        var id1 = await repo.InsertAndGetIdAsync(new CrudUser { Name = "Del1", Age = 20, IsActive = true });
        var id2 = await repo.InsertAndGetIdAsync(new CrudUser { Name = "Del2", Age = 25, IsActive = true });
        await repo.InsertAsync(new CrudUser { Name = "Keep", Age = 30, IsActive = true });
        await repo.DeleteByIdsAsync(new List<long> { id1, id2 });
        Assert.AreEqual(1, await repo.CountAsync());
    }

    // ── DeleteWhere ───────────────────────────────────────────────────────────

    [DataTestMethod]
    [DataRow(DatabaseType.SQLite)]
    [DataRow(DatabaseType.PostgreSQL)]
    [DataRow(DatabaseType.MySQL)]
    [DataRow(DatabaseType.SqlServer)]
    public async Task AllDatabases_DeleteWhere_RemovesMatchingRecords(DatabaseType dbType)
    {
        var (fixture, repo) = await SetupAsync(dbType);
        await using var _ = fixture;
        await repo.InsertAsync(new CrudUser { Name = "Del", Age = 20, IsActive = false });
        await repo.InsertAsync(new CrudUser { Name = "Keep", Age = 25, IsActive = true });
        await repo.DeleteWhereAsync(u => !u.IsActive);
        Assert.AreEqual(1, await repo.CountAsync());
    }

    // ── DeleteAll ─────────────────────────────────────────────────────────────

    [DataTestMethod]
    [DataRow(DatabaseType.SQLite)]
    [DataRow(DatabaseType.PostgreSQL)]
    [DataRow(DatabaseType.MySQL)]
    [DataRow(DatabaseType.SqlServer)]
    public async Task AllDatabases_DeleteAll_RemovesAllRecords(DatabaseType dbType)
    {
        var (fixture, repo) = await SetupAsync(dbType);
        await using var _ = fixture;
        await repo.InsertAsync(new CrudUser { Name = "A", Age = 20, IsActive = true });
        await repo.InsertAsync(new CrudUser { Name = "B", Age = 25, IsActive = true });
        await repo.DeleteAllAsync();
        Assert.AreEqual(0, await repo.CountAsync());
    }

    // ── AsQueryable ───────────────────────────────────────────────────────────

    [DataTestMethod]
    [DataRow(DatabaseType.SQLite)]
    [DataRow(DatabaseType.PostgreSQL)]
    [DataRow(DatabaseType.MySQL)]
    [DataRow(DatabaseType.SqlServer)]
    public async Task AllDatabases_AsQueryable_SupportsLinqQueries(DatabaseType dbType)
    {
        var (fixture, repo) = await SetupAsync(dbType);
        await using var _ = fixture;
        await repo.InsertAsync(new CrudUser { Name = "Q1", Age = 20, IsActive = true });
        await repo.InsertAsync(new CrudUser { Name = "Q2", Age = 30, IsActive = true });
        await repo.InsertAsync(new CrudUser { Name = "Q3", Age = 40, IsActive = false });
        var results = await repo.AsQueryable()
            .Where(u => u.IsActive && u.Age >= 25)
            .WithConnection(fixture.Connection)
            .ToListAsync();
        Assert.AreEqual(1, results.Count);
        Assert.AreEqual("Q2", results[0].Name);
    }

    // ── Custom SqlTemplate ────────────────────────────────────────────────────

    [DataTestMethod]
    [DataRow(DatabaseType.SQLite)]
    [DataRow(DatabaseType.PostgreSQL)]
    [DataRow(DatabaseType.MySQL)]
    [DataRow(DatabaseType.SqlServer)]
    public async Task AllDatabases_CustomSqlTemplate_ReturnsFilteredResults(DatabaseType dbType)
    {
        var (fixture, repo) = await SetupAsync(dbType);
        await using var _ = fixture;
        await repo.InsertAsync(new CrudUser { Name = "Adult1", Age = 18, IsActive = true });
        await repo.InsertAsync(new CrudUser { Name = "Adult2", Age = 25, IsActive = true });
        await repo.InsertAsync(new CrudUser { Name = "Child", Age = 10, IsActive = true });
        var adults = await ((ICrudUserRepository)repo).GetAdultsAsync(18);
        Assert.AreEqual(2, adults.Count);
    }

    // ── Sync methods ──────────────────────────────────────────────────────────

    [DataTestMethod]
    [DataRow(DatabaseType.SQLite)]
    [DataRow(DatabaseType.PostgreSQL)]
    [DataRow(DatabaseType.MySQL)]
    [DataRow(DatabaseType.SqlServer)]
    public async Task AllDatabases_SyncMethods_Work(DatabaseType dbType)
    {
        var (fixture, repo) = await SetupAsync(dbType);
        await using var _ = fixture;
        var id = repo.InsertAndGetId(new CrudUser { Name = "Sync", Age = 30, IsActive = true });
        Assert.IsTrue(id > 0);
        var user = repo.GetById(id);
        Assert.IsNotNull(user);
        Assert.AreEqual("Sync", user.Name);
        var all = repo.GetAll();
        Assert.AreEqual(1, all.Count);
        var count = repo.Count();
        Assert.AreEqual(1, count);
        var exists = repo.ExistsById(id);
        Assert.IsTrue(exists);
        repo.Delete(id);
        Assert.IsNull(repo.GetById(id));
    }

    // ── Transaction ───────────────────────────────────────────────────────────

    [DataTestMethod]
    [DataRow(DatabaseType.SQLite)]
    [DataRow(DatabaseType.PostgreSQL)]
    [DataRow(DatabaseType.MySQL)]
    [DataRow(DatabaseType.SqlServer)]
    public async Task AllDatabases_Transaction_CommitAndRollback_Work(DatabaseType dbType)
    {
        var (fixture, repo) = await SetupAsync(dbType);
        await using var _ = fixture;

        // Commit
        await using (var tx = await fixture.Connection.BeginTransactionAsync())
        {
            ((ISqlxRepository)repo).Transaction = tx;
            await repo.InsertAsync(new CrudUser { Name = "Committed", Age = 25, IsActive = true });
            await tx.CommitAsync();
        }
        ((ISqlxRepository)repo).Transaction = null;
        Assert.AreEqual(1, await repo.CountAsync());

        // Rollback
        await using (var tx = await fixture.Connection.BeginTransactionAsync())
        {
            ((ISqlxRepository)repo).Transaction = tx;
            await repo.InsertAsync(new CrudUser { Name = "Rolled", Age = 30, IsActive = true });
            await tx.RollbackAsync();
        }
        ((ISqlxRepository)repo).Transaction = null;
        Assert.AreEqual(1, await repo.CountAsync());
    }

    // ── Null handling ─────────────────────────────────────────────────────────

    [DataTestMethod]
    [DataRow(DatabaseType.SQLite)]
    [DataRow(DatabaseType.PostgreSQL)]
    [DataRow(DatabaseType.MySQL)]
    [DataRow(DatabaseType.SqlServer)]
    public async Task AllDatabases_NullableField_HandlesNullAndValue(DatabaseType dbType)
    {
        var (fixture, repo) = await SetupAsync(dbType);
        await using var _ = fixture;
        var id1 = await repo.InsertAndGetIdAsync(new CrudUser { Name = "WithEmail", Age = 25, IsActive = true, Email = "test@test.com" });
        var id2 = await repo.InsertAndGetIdAsync(new CrudUser { Name = "NoEmail", Age = 30, IsActive = true, Email = null });
        var u1 = await repo.GetByIdAsync(id1);
        var u2 = await repo.GetByIdAsync(id2);
        Assert.AreEqual("test@test.com", u1!.Email);
        Assert.IsNull(u2!.Email);
    }
}
