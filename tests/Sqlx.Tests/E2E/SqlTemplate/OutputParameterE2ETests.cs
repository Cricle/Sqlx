// <copyright file="OutputParameterE2ETests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using System.Data.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;
using Sqlx.Tests.E2E.Infrastructure;

namespace Sqlx.Tests.E2E.SqlTemplate;

public interface IMySqlOutputParameterRepository
{
    [SqlTemplate("INSERT INTO output_items (name) VALUES (@name); SELECT LAST_INSERT_ID()")]
    int InsertAndGetId(string name, out long id);

    [SqlTemplate("INSERT INTO output_items (name) VALUES (@name); SELECT LAST_INSERT_ID()")]
    Task<int> InsertAndGetIdAsync(string name, OutputParameter<long> id);
}

[RepositoryFor(typeof(IMySqlOutputParameterRepository), TableName = "output_items")]
public partial class MySqlOutputParameterRepository : IMySqlOutputParameterRepository
{
    private readonly DbConnection _connection;

    public MySqlOutputParameterRepository(DbConnection connection, SqlDialect dialect)
    {
        _connection = connection;
        _dialect = dialect;
    }
}

public interface IPostgreSqlOutputParameterRepository
{
    [SqlTemplate("INSERT INTO output_items (name) VALUES (@name) RETURNING id")]
    int InsertAndGetId(string name, out long id);

    [SqlTemplate("INSERT INTO output_items (name) VALUES (@name) RETURNING id")]
    Task<int> InsertAndGetIdAsync(string name, OutputParameter<long> id);
}

[RepositoryFor(typeof(IPostgreSqlOutputParameterRepository), TableName = "output_items")]
public partial class PostgreSqlOutputParameterRepository : IPostgreSqlOutputParameterRepository
{
    private readonly DbConnection _connection;

    public PostgreSqlOutputParameterRepository(DbConnection connection, SqlDialect dialect)
    {
        _connection = connection;
        _dialect = dialect;
    }
}

public interface ISqlServerOutputParameterRepository
{
    [SqlTemplate("INSERT INTO output_items (name) VALUES (@name); SELECT CAST(SCOPE_IDENTITY() AS BIGINT)")]
    int InsertAndGetId(string name, out long id);

    [SqlTemplate("INSERT INTO output_items (name) VALUES (@name); SELECT CAST(SCOPE_IDENTITY() AS BIGINT)")]
    Task<int> InsertAndGetIdAsync(string name, OutputParameter<long> id);
}

[RepositoryFor(typeof(ISqlServerOutputParameterRepository), TableName = "output_items")]
public partial class SqlServerOutputParameterRepository : ISqlServerOutputParameterRepository
{
    private readonly DbConnection _connection;

    public SqlServerOutputParameterRepository(DbConnection connection, SqlDialect dialect)
    {
        _connection = connection;
        _dialect = dialect;
    }
}

public interface ISqliteOutputParameterRepository
{
    [SqlTemplate("INSERT INTO output_items (name) VALUES (@name); SELECT last_insert_rowid()")]
    int InsertAndGetId(string name, out long id);

    [SqlTemplate("INSERT INTO output_items (name) VALUES (@name); SELECT last_insert_rowid()")]
    Task<int> InsertAndGetIdAsync(string name, OutputParameter<long> id);
}

[RepositoryFor(typeof(ISqliteOutputParameterRepository), TableName = "output_items")]
public partial class SqliteOutputParameterRepository : ISqliteOutputParameterRepository
{
    private readonly DbConnection _connection;

    public SqliteOutputParameterRepository(DbConnection connection, SqlDialect dialect)
    {
        _connection = connection;
        _dialect = dialect;
    }
}

public interface ICounterOutputParameterRepository
{
    [SqlTemplate("UPDATE counters SET value = value + 1 WHERE name = @name; SELECT value FROM counters WHERE name = @name")]
    int IncrementCounter(string name, ref int value);

    [SqlTemplate("UPDATE counters SET value = value + 1 WHERE name = @name; SELECT value FROM counters WHERE name = @name")]
    Task<int> IncrementCounterAsync(string name, OutputParameter<int> value);
}

[RepositoryFor(typeof(ICounterOutputParameterRepository), TableName = "counters")]
public partial class CounterOutputParameterRepository : ICounterOutputParameterRepository
{
    private readonly DbConnection _connection;

    public CounterOutputParameterRepository(DbConnection connection, SqlDialect dialect)
    {
        _connection = connection;
        _dialect = dialect;
    }
}

[TestClass]
public class OutputParameterE2ETests : E2ETestBase
{
    private static string GetOutputItemsSchema(DatabaseType dbType)
    {
        return dbType switch
        {
            DatabaseType.MySQL => @"
                CREATE TABLE output_items (
                    id BIGINT AUTO_INCREMENT PRIMARY KEY,
                    name VARCHAR(100) NOT NULL
                )",
            DatabaseType.PostgreSQL => @"
                CREATE TABLE output_items (
                    id BIGSERIAL PRIMARY KEY,
                    name VARCHAR(100) NOT NULL
                )",
            DatabaseType.SqlServer => @"
                CREATE TABLE output_items (
                    id BIGINT IDENTITY(1,1) PRIMARY KEY,
                    name NVARCHAR(100) NOT NULL
                )",
            DatabaseType.SQLite => @"
                CREATE TABLE output_items (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL
                )",
            _ => throw new NotSupportedException($"Database type {dbType} is not supported"),
        };
    }

    private static string GetCountersSchema(DatabaseType dbType)
    {
        return dbType switch
        {
            DatabaseType.MySQL => @"
                CREATE TABLE counters (
                    name VARCHAR(100) PRIMARY KEY,
                    value INT NOT NULL
                )",
            DatabaseType.PostgreSQL => @"
                CREATE TABLE counters (
                    name VARCHAR(100) PRIMARY KEY,
                    value INT NOT NULL
                )",
            DatabaseType.SqlServer => @"
                CREATE TABLE counters (
                    name NVARCHAR(100) PRIMARY KEY,
                    value INT NOT NULL
                )",
            DatabaseType.SQLite => @"
                CREATE TABLE counters (
                    name TEXT PRIMARY KEY,
                    value INTEGER NOT NULL
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

    private static async Task<long> CountRowsAsync(DbConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM output_items";
        return Convert.ToInt64(await command.ExecuteScalarAsync());
    }

    private static async Task SeedCounterAsync(DbConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO counters (name, value) VALUES (@name, @value)";
        AddParameter(command, "@name", "page_views");
        AddParameter(command, "@value", 100);
        await command.ExecuteNonQueryAsync();
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
    [TestCategory("OutputParameter")]
    [TestCategory("MySQL")]
    public async Task MySQL_OutputParameter_Out_ReturnsInsertedId()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetOutputItemsSchema(DatabaseType.MySQL));
        var repo = new MySqlOutputParameterRepository(fixture.Connection, SqlDefine.MySql);

        var rows = repo.InsertAndGetId("sync-mysql", out var id);

        Assert.AreEqual(1, rows);
        Assert.AreEqual(1L, id);
        Assert.AreEqual(1L, await CountRowsAsync(fixture.Connection));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("OutputParameter")]
    [TestCategory("MySQL")]
    public async Task MySQL_OutputParameter_AsyncWrapper_ReturnsInsertedId()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetOutputItemsSchema(DatabaseType.MySQL));
        var repo = new MySqlOutputParameterRepository(fixture.Connection, SqlDefine.MySql);
        var output = new OutputParameter<long>();

        var rows = await repo.InsertAndGetIdAsync("async-mysql", output);

        Assert.AreEqual(1, rows);
        Assert.IsTrue(output.HasValue);
        Assert.AreEqual(1L, output.Value);
        Assert.AreEqual(1L, await CountRowsAsync(fixture.Connection));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("OutputParameter")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_OutputParameter_Out_ReturnsInsertedId()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetOutputItemsSchema(DatabaseType.PostgreSQL));
        var repo = new PostgreSqlOutputParameterRepository(fixture.Connection, SqlDefine.PostgreSql);

        var rows = repo.InsertAndGetId("sync-pg", out var id);

        Assert.AreEqual(1, rows);
        Assert.AreEqual(1L, id);
        Assert.AreEqual(1L, await CountRowsAsync(fixture.Connection));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("OutputParameter")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_OutputParameter_AsyncWrapper_ReturnsInsertedId()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetOutputItemsSchema(DatabaseType.PostgreSQL));
        var repo = new PostgreSqlOutputParameterRepository(fixture.Connection, SqlDefine.PostgreSql);
        var output = new OutputParameter<long>();

        var rows = await repo.InsertAndGetIdAsync("async-pg", output);

        Assert.AreEqual(1, rows);
        Assert.IsTrue(output.HasValue);
        Assert.AreEqual(1L, output.Value);
        Assert.AreEqual(1L, await CountRowsAsync(fixture.Connection));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("OutputParameter")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_OutputParameter_Out_ReturnsInsertedId()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetOutputItemsSchema(DatabaseType.SqlServer));
        var repo = new SqlServerOutputParameterRepository(fixture.Connection, SqlDefine.SqlServer);

        var rows = repo.InsertAndGetId("sync-sqlserver", out var id);

        Assert.AreEqual(1, rows);
        Assert.AreEqual(1L, id);
        Assert.AreEqual(1L, await CountRowsAsync(fixture.Connection));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("OutputParameter")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_OutputParameter_AsyncWrapper_ReturnsInsertedId()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetOutputItemsSchema(DatabaseType.SqlServer));
        var repo = new SqlServerOutputParameterRepository(fixture.Connection, SqlDefine.SqlServer);
        var output = new OutputParameter<long>();

        var rows = await repo.InsertAndGetIdAsync("async-sqlserver", output);

        Assert.AreEqual(1, rows);
        Assert.IsTrue(output.HasValue);
        Assert.AreEqual(1L, output.Value);
        Assert.AreEqual(1L, await CountRowsAsync(fixture.Connection));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("OutputParameter")]
    [TestCategory("SQLite")]
    public async Task SQLite_OutputParameter_Out_ReturnsInsertedId()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetOutputItemsSchema(DatabaseType.SQLite));
        var repo = new SqliteOutputParameterRepository(fixture.Connection, SqlDefine.SQLite);

        var rows = repo.InsertAndGetId("sync-sqlite", out var id);

        Assert.AreEqual(1, rows);
        Assert.AreEqual(1L, id);
        Assert.AreEqual(1L, await CountRowsAsync(fixture.Connection));
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("OutputParameter")]
    [TestCategory("SQLite")]
    public async Task SQLite_OutputParameter_AsyncWrapper_ReturnsInsertedId()
    {
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetOutputItemsSchema(DatabaseType.SQLite));
        var repo = new SqliteOutputParameterRepository(fixture.Connection, SqlDefine.SQLite);
        var output = new OutputParameter<long>();

        var rows = await repo.InsertAndGetIdAsync("async-sqlite", output);

        Assert.AreEqual(1, rows);
        Assert.IsTrue(output.HasValue);
        Assert.AreEqual(1L, output.Value);
        Assert.AreEqual(1L, await CountRowsAsync(fixture.Connection));
    }

    [DataTestMethod]
    [DataRow(DatabaseType.MySQL)]
    [DataRow(DatabaseType.PostgreSQL)]
    [DataRow(DatabaseType.SqlServer)]
    [DataRow(DatabaseType.SQLite)]
    [TestCategory("E2E")]
    [TestCategory("OutputParameter")]
    public async Task OutputParameter_Ref_SyncInputOutput_ReturnsUpdatedCounter(DatabaseType dbType)
    {
        await using var fixture = await CreateFixtureAsync(dbType);
        await fixture.CreateSchemaAsync(GetCountersSchema(dbType));
        await SeedCounterAsync(fixture.Connection);
        var repo = new CounterOutputParameterRepository(fixture.Connection, GetDialect(dbType));

        int counter = 100;
        var rows = repo.IncrementCounter("page_views", ref counter);

        Assert.AreEqual(1, rows);
        Assert.AreEqual(101, counter);
    }

    [DataTestMethod]
    [DataRow(DatabaseType.MySQL)]
    [DataRow(DatabaseType.PostgreSQL)]
    [DataRow(DatabaseType.SqlServer)]
    [DataRow(DatabaseType.SQLite)]
    [TestCategory("E2E")]
    [TestCategory("OutputParameter")]
    public async Task OutputParameter_AsyncWrapper_InputOutput_ReturnsUpdatedCounter(DatabaseType dbType)
    {
        await using var fixture = await CreateFixtureAsync(dbType);
        await fixture.CreateSchemaAsync(GetCountersSchema(dbType));
        await SeedCounterAsync(fixture.Connection);
        var repo = new CounterOutputParameterRepository(fixture.Connection, GetDialect(dbType));
        var counter = OutputParameter<int>.WithValue(100);

        var rows = await repo.IncrementCounterAsync("page_views", counter);

        Assert.AreEqual(1, rows);
        Assert.IsTrue(counter.HasValue);
        Assert.AreEqual(101, counter.Value);
    }
}
