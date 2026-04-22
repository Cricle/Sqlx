// <copyright file="FeatureCoverageE2ETests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using System.Data.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using Sqlx.Annotations;
using Sqlx.Tests.E2E.Infrastructure;

namespace Sqlx.Tests.E2E.Features;

// ── Entity & Repository ───────────────────────────────────────────────────────

[Sqlx, TableName("feat_items")]
public class FeatItem
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Score { get; set; }
    public string? Tag { get; set; }
}

public interface IFeatItemRepository : ICrudRepository<FeatItem, long>
{
    [SqlTemplate("SELECT {{columns}} FROM {{table}} {{paginate --limit pageSize --offset offset}}")]
    Task<List<FeatItem>> GetPageAsync(int pageSize, int offset);
}

[RepositoryFor(typeof(IFeatItemRepository), TableName = "feat_items")]
public partial class SQLiteFeatItemRepository : IFeatItemRepository
{
    private readonly DbConnection _connection;
    public SQLiteFeatItemRepository(DbConnection c, SqlDialect d) { _connection = c; _dialect = d; }
}

[RepositoryFor(typeof(IFeatItemRepository), TableName = "feat_items")]
public partial class PostgreSqlFeatItemRepository : IFeatItemRepository
{
    private readonly DbConnection _connection;
    public PostgreSqlFeatItemRepository(DbConnection c, SqlDialect d) { _connection = c; _dialect = d; }
}

[RepositoryFor(typeof(IFeatItemRepository), TableName = "feat_items")]
public partial class MySqlFeatItemRepository : IFeatItemRepository
{
    private readonly DbConnection _connection;
    public MySqlFeatItemRepository(DbConnection c, SqlDialect d) { _connection = c; _dialect = d; }
}

[RepositoryFor(typeof(IFeatItemRepository), TableName = "feat_items")]
public partial class SqlServerFeatItemRepository : IFeatItemRepository
{
    private readonly DbConnection _connection;
    public SqlServerFeatItemRepository(DbConnection c, SqlDialect d) { _connection = c; _dialect = d; }
}

// ── Tests ─────────────────────────────────────────────────────────────────────

[TestClass]
public class FeatureCoverageE2ETests : E2ETestBase
{
    private static string GetSchema(DatabaseType dbType) => dbType switch
    {
        DatabaseType.MySQL => @"CREATE TABLE feat_items (
            id BIGINT AUTO_INCREMENT PRIMARY KEY,
            name VARCHAR(100) NOT NULL,
            score INT NOT NULL,
            tag VARCHAR(50) NULL)",
        DatabaseType.PostgreSQL => @"CREATE TABLE feat_items (
            id BIGSERIAL PRIMARY KEY,
            name VARCHAR(100) NOT NULL,
            score INT NOT NULL,
            tag VARCHAR(50) NULL)",
        DatabaseType.SqlServer => @"CREATE TABLE feat_items (
            id BIGINT IDENTITY(1,1) PRIMARY KEY,
            name NVARCHAR(100) NOT NULL,
            score INT NOT NULL,
            tag NVARCHAR(50) NULL)",
        DatabaseType.SQLite => @"CREATE TABLE feat_items (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            name TEXT NOT NULL,
            score INTEGER NOT NULL,
            tag TEXT NULL)",
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

    private static IFeatItemRepository CreateRepo(DbConnection conn, DatabaseType dbType) => dbType switch
    {
        DatabaseType.MySQL => new MySqlFeatItemRepository(conn, GetDialect(dbType)),
        DatabaseType.PostgreSQL => new PostgreSqlFeatItemRepository(conn, GetDialect(dbType)),
        DatabaseType.SqlServer => new SqlServerFeatItemRepository(conn, GetDialect(dbType)),
        DatabaseType.SQLite => new SQLiteFeatItemRepository(conn, GetDialect(dbType)),
        _ => throw new NotSupportedException()
    };

    // ── 1. SqlxQueryable execution ────────────────────────────────────────────

    [DataTestMethod]
    [DataRow(DatabaseType.SQLite)]
    [DataRow(DatabaseType.PostgreSQL)]
    [DataRow(DatabaseType.MySQL)]
    [DataRow(DatabaseType.SqlServer)]
    public async Task SqlxQueryable_Where_ReturnsFilteredResults(DatabaseType dbType)
    {
        var fixture = await CreateFixtureAsync(dbType);
        await using var _ = fixture;
        await fixture.CreateSchemaAsync(GetSchema(dbType));
        var repo = CreateRepo(fixture.Connection, dbType);

        await repo.InsertAsync(new FeatItem { Name = "Alice", Score = 10, Tag = "A" });
        await repo.InsertAsync(new FeatItem { Name = "Bob", Score = 20, Tag = "B" });
        await repo.InsertAsync(new FeatItem { Name = "Carol", Score = 30, Tag = "A" });

        var results = await SqlQuery<FeatItem>.For(GetDialect(dbType))
            .WithConnection(fixture.Connection)
            .Where(x => x.Score >= 20)
            .ToListAsync();

        Assert.AreEqual(2, results.Count);
        Assert.IsTrue(results.All(x => x.Score >= 20));
    }

    // ── 2. SqlxQueryable CountAsync ───────────────────────────────────────────

    [DataTestMethod]
    [DataRow(DatabaseType.SQLite)]
    [DataRow(DatabaseType.PostgreSQL)]
    [DataRow(DatabaseType.MySQL)]
    [DataRow(DatabaseType.SqlServer)]
    public async Task SqlxQueryable_CountAsync_ReturnsCorrectCount(DatabaseType dbType)
    {
        var fixture = await CreateFixtureAsync(dbType);
        await using var _ = fixture;
        await fixture.CreateSchemaAsync(GetSchema(dbType));
        var repo = CreateRepo(fixture.Connection, dbType);

        await repo.InsertAsync(new FeatItem { Name = "Alice", Score = 10 });
        await repo.InsertAsync(new FeatItem { Name = "Bob", Score = 20 });
        await repo.InsertAsync(new FeatItem { Name = "Carol", Score = 30 });

        var count = await SqlQuery<FeatItem>.For(GetDialect(dbType))
            .WithConnection(fixture.Connection)
            .Where(x => x.Score >= 20)
            .CountAsync();

        Assert.AreEqual(2L, count);
    }

    // ── 3. SqlxQueryable AnyAsync ─────────────────────────────────────────────

    [DataTestMethod]
    [DataRow(DatabaseType.SQLite)]
    [DataRow(DatabaseType.PostgreSQL)]
    [DataRow(DatabaseType.MySQL)]
    [DataRow(DatabaseType.SqlServer)]
    public async Task SqlxQueryable_AnyAsync_ReturnsCorrectResult(DatabaseType dbType)
    {
        var fixture = await CreateFixtureAsync(dbType);
        await using var _ = fixture;
        await fixture.CreateSchemaAsync(GetSchema(dbType));
        var repo = CreateRepo(fixture.Connection, dbType);

        await repo.InsertAsync(new FeatItem { Name = "Alice", Score = 10 });
        await repo.InsertAsync(new FeatItem { Name = "Bob", Score = 20 });

        var exists = await SqlQuery<FeatItem>.For(GetDialect(dbType))
            .WithConnection(fixture.Connection)
            .Where(x => x.Name == "Bob")
            .AnyAsync();

        var notExists = await SqlQuery<FeatItem>.For(GetDialect(dbType))
            .WithConnection(fixture.Connection)
            .Where(x => x.Name == "Charlie")
            .AnyAsync();

        Assert.IsTrue(exists);
        Assert.IsFalse(notExists);
    }

    // ── 4. SqlxQueryable FirstOrDefaultAsync ──────────────────────────────────

    [DataTestMethod]
    [DataRow(DatabaseType.SQLite)]
    [DataRow(DatabaseType.PostgreSQL)]
    [DataRow(DatabaseType.MySQL)]
    [DataRow(DatabaseType.SqlServer)]
    public async Task SqlxQueryable_FirstOrDefaultAsync_ReturnsFirstMatch(DatabaseType dbType)
    {
        var fixture = await CreateFixtureAsync(dbType);
        await using var _ = fixture;
        await fixture.CreateSchemaAsync(GetSchema(dbType));
        var repo = CreateRepo(fixture.Connection, dbType);

        await repo.InsertAsync(new FeatItem { Name = "Alice", Score = 10 });
        await repo.InsertAsync(new FeatItem { Name = "Bob", Score = 20 });
        await repo.InsertAsync(new FeatItem { Name = "Carol", Score = 30 });

        var result = await SqlQuery<FeatItem>.For(GetDialect(dbType))
            .WithConnection(fixture.Connection)
            .Where(x => x.Score >= 20)
            .OrderBy(x => x.Score)
            .FirstOrDefaultAsync();

        Assert.IsNotNull(result);
        Assert.AreEqual("Bob", result!.Name);
        Assert.AreEqual(20, result.Score);
    }

    // ── 5. SqlxQueryable OrderBy + Skip + Take ────────────────────────────────

    [DataTestMethod]
    [DataRow(DatabaseType.SQLite)]
    [DataRow(DatabaseType.PostgreSQL)]
    [DataRow(DatabaseType.MySQL)]
    [DataRow(DatabaseType.SqlServer)]
    public async Task SqlxQueryable_OrderBySkipTake_ReturnsPaginatedResults(DatabaseType dbType)
    {
        var fixture = await CreateFixtureAsync(dbType);
        await using var _ = fixture;
        await fixture.CreateSchemaAsync(GetSchema(dbType));
        var repo = CreateRepo(fixture.Connection, dbType);

        await repo.InsertAsync(new FeatItem { Name = "Item1", Score = 10 });
        await repo.InsertAsync(new FeatItem { Name = "Item2", Score = 20 });
        await repo.InsertAsync(new FeatItem { Name = "Item3", Score = 30 });
        await repo.InsertAsync(new FeatItem { Name = "Item4", Score = 40 });
        await repo.InsertAsync(new FeatItem { Name = "Item5", Score = 50 });

        var page2 = await SqlQuery<FeatItem>.For(GetDialect(dbType))
            .WithConnection(fixture.Connection)
            .OrderBy(x => x.Score)
            .Skip(2)
            .Take(2)
            .ToListAsync();

        Assert.AreEqual(2, page2.Count);
        Assert.AreEqual("Item3", page2[0].Name);
        Assert.AreEqual("Item4", page2[1].Name);
    }

    // ── 6. {{if notnull=param}} conditional (via SqlTemplate.Render) ──────────

    [DataTestMethod]
    [DataRow(DatabaseType.SQLite)]
    [DataRow(DatabaseType.PostgreSQL)]
    [DataRow(DatabaseType.MySQL)]
    [DataRow(DatabaseType.SqlServer)]
    public async Task SqlTemplate_IfNotnull_ConditionallyIncludesClause(DatabaseType dbType)
    {
        var fixture = await CreateFixtureAsync(dbType);
        await using var _ = fixture;
        await fixture.CreateSchemaAsync(GetSchema(dbType));
        var repo = CreateRepo(fixture.Connection, dbType);

        await repo.InsertAsync(new FeatItem { Name = "Alice", Score = 10 });
        await repo.InsertAsync(new FeatItem { Name = "Bob", Score = 20 });
        await repo.InsertAsync(new FeatItem { Name = "Carol", Score = 30 });

        var dialect = GetDialect(dbType);
        var context = PlaceholderContext.Create<FeatItem>(dialect);
        var template = Sqlx.SqlTemplate.Prepare(
            "SELECT {{columns}} FROM {{table}} WHERE 1=1 {{if notnull=name}}AND name = @name{{/if}}",
            context);

        // With null - returns all rows
        var sqlAll = template.Render(new Dictionary<string, object?> { ["name"] = null });
        var allResults = await fixture.Connection.SqlxQueryAsync<FeatItem, FeatItem>(sqlAll, dialect);
        Assert.AreEqual(3, allResults.Count);

        // With value - filters
        var sqlFiltered = template.Render(new Dictionary<string, object?> { ["name"] = "Bob" });
        var filtered = await fixture.Connection.SqlxQueryAsync<FeatItem, FeatItem>(
            sqlFiltered, dialect, new { name = "Bob" });
        Assert.AreEqual(1, filtered.Count);
        Assert.AreEqual("Bob", filtered[0].Name);
    }

    // ── 7. {{where --object filter}} ──────────────────────────────────────────

    [DataTestMethod]
    [DataRow(DatabaseType.SQLite)]
    [DataRow(DatabaseType.PostgreSQL)]
    [DataRow(DatabaseType.MySQL)]
    [DataRow(DatabaseType.SqlServer)]
    public async Task SqlTemplate_WhereObject_FiltersCorrectly(DatabaseType dbType)
    {
        var fixture = await CreateFixtureAsync(dbType);
        await using var _ = fixture;
        await fixture.CreateSchemaAsync(GetSchema(dbType));
        var repo = CreateRepo(fixture.Connection, dbType);

        await repo.InsertAsync(new FeatItem { Name = "Alice", Score = 10, Tag = "A" });
        await repo.InsertAsync(new FeatItem { Name = "Bob", Score = 20, Tag = "B" });
        await repo.InsertAsync(new FeatItem { Name = "Carol", Score = 30, Tag = "A" });

        var dialect = GetDialect(dbType);
        var context = PlaceholderContext.Create<FeatItem>(dialect);
        var template = Sqlx.SqlTemplate.Prepare(
            "SELECT {{columns}} FROM {{table}} WHERE {{where --object filter}}",
            context);

        // Empty dict - returns all
        var sqlAll = template.Render(new Dictionary<string, object?> { ["filter"] = new Dictionary<string, object?>() });
        var allResults = await fixture.Connection.SqlxQueryAsync<FeatItem, FeatItem>(sqlAll, dialect);
        Assert.AreEqual(3, allResults.Count);

        // With values - filters
        var filterDict = new Dictionary<string, object?> { ["Tag"] = "A" };
        var sqlFiltered = template.Render(new Dictionary<string, object?> { ["filter"] = filterDict });
        var filtered = await fixture.Connection.SqlxQueryAsync<FeatItem, FeatItem>(
            sqlFiltered, dialect, new { tag = "A" });
        Assert.AreEqual(2, filtered.Count);
        Assert.IsTrue(filtered.All(x => x.Tag == "A"));
    }

    // ── 8. {{paginate --limit pageSize --offset offset}} ──────────────────────

    [DataTestMethod]
    [DataRow(DatabaseType.SQLite)]
    [DataRow(DatabaseType.PostgreSQL)]
    [DataRow(DatabaseType.MySQL)]
    [DataRow(DatabaseType.SqlServer)]
    public async Task SqlTemplate_Paginate_ReturnsPaginatedResults(DatabaseType dbType)
    {
        var fixture = await CreateFixtureAsync(dbType);
        await using var _ = fixture;
        await fixture.CreateSchemaAsync(GetSchema(dbType));
        var repo = CreateRepo(fixture.Connection, dbType);

        await repo.InsertAsync(new FeatItem { Name = "Item1", Score = 10 });
        await repo.InsertAsync(new FeatItem { Name = "Item2", Score = 20 });
        await repo.InsertAsync(new FeatItem { Name = "Item3", Score = 30 });
        await repo.InsertAsync(new FeatItem { Name = "Item4", Score = 40 });
        await repo.InsertAsync(new FeatItem { Name = "Item5", Score = 50 });

        var page1 = await repo.GetPageAsync(2, 0);
        Assert.AreEqual(2, page1.Count);

        var page2 = await repo.GetPageAsync(2, 2);
        Assert.AreEqual(2, page2.Count);

        var page3 = await repo.GetPageAsync(2, 4);
        Assert.AreEqual(1, page3.Count);
    }

    // ── 9. SqlBuilder dynamic SQL ─────────────────────────────────────────────

    [DataTestMethod]
    [DataRow(DatabaseType.SQLite)]
    [DataRow(DatabaseType.PostgreSQL)]
    [DataRow(DatabaseType.MySQL)]
    [DataRow(DatabaseType.SqlServer)]
    public async Task SqlBuilder_DynamicWhere_FiltersCorrectly(DatabaseType dbType)
    {
        var fixture = await CreateFixtureAsync(dbType);
        await using var _ = fixture;
        await fixture.CreateSchemaAsync(GetSchema(dbType));
        var repo = CreateRepo(fixture.Connection, dbType);

        await repo.InsertAsync(new FeatItem { Name = "Alice", Score = 10 });
        await repo.InsertAsync(new FeatItem { Name = "Bob", Score = 20 });
        await repo.InsertAsync(new FeatItem { Name = "Carol", Score = 30 });

        using var builder = new Sqlx.SqlBuilder(GetDialect(dbType));
        builder.Append($"SELECT id, name, score, tag FROM feat_items WHERE score >= {20}");
        var template = builder.Build();

        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = template.Sql;
        foreach (var param in template.Parameters)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = param.Key;
            p.Value = param.Value ?? DBNull.Value;
            cmd.Parameters.Add(p);
        }

        var results = new List<FeatItem>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(new FeatItem
            {
                Id = reader.GetInt64(0),
                Name = reader.GetString(1),
                Score = reader.GetInt32(2),
                Tag = reader.IsDBNull(3) ? null : reader.GetString(3)
            });
        }

        Assert.AreEqual(2, results.Count);
        Assert.IsTrue(results.All(x => x.Score >= 20));
    }

    // ── 10. SqlBuilder subquery ───────────────────────────────────────────────

    [DataTestMethod]
    [DataRow(DatabaseType.SQLite)]
    [DataRow(DatabaseType.PostgreSQL)]
    [DataRow(DatabaseType.MySQL)]
    [DataRow(DatabaseType.SqlServer)]
    public async Task SqlBuilder_Subquery_FiltersCorrectly(DatabaseType dbType)
    {
        var fixture = await CreateFixtureAsync(dbType);
        await using var _ = fixture;
        await fixture.CreateSchemaAsync(GetSchema(dbType));
        var repo = CreateRepo(fixture.Connection, dbType);

        await repo.InsertAsync(new FeatItem { Name = "Alice", Score = 10 });
        await repo.InsertAsync(new FeatItem { Name = "Bob", Score = 20 });
        await repo.InsertAsync(new FeatItem { Name = "Carol", Score = 30 });

        using var subquery = new Sqlx.SqlBuilder(GetDialect(dbType));
        subquery.Append($"SELECT id FROM feat_items WHERE score >= {20}");

        using var mainQuery = new Sqlx.SqlBuilder(GetDialect(dbType));
        mainQuery.Append($"SELECT id, name, score, tag FROM feat_items WHERE id IN ");
        mainQuery.AppendSubquery(subquery);
        var template = mainQuery.Build();

        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = template.Sql;
        foreach (var param in template.Parameters)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = param.Key;
            p.Value = param.Value ?? DBNull.Value;
            cmd.Parameters.Add(p);
        }

        var results = new List<FeatItem>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(new FeatItem
            {
                Id = reader.GetInt64(0),
                Name = reader.GetString(1),
                Score = reader.GetInt32(2),
                Tag = reader.IsDBNull(3) ? null : reader.GetString(3)
            });
        }

        Assert.AreEqual(2, results.Count);
        Assert.IsTrue(results.All(x => x.Score >= 20));
    }
}
