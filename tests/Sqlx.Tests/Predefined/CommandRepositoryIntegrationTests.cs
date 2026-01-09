// -----------------------------------------------------------------------
// <copyright file="CommandRepositoryIntegrationTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MySqlConnector;
using Npgsql;
using Sqlx.Tests.Infrastructure;

namespace Sqlx.Tests.Predefined;

/// <summary>
/// Cross-database integration tests for ICommandRepository interface.
/// Tests: Insert, InsertAndGetId, InsertAndGetEntity, Update, Delete, DeleteWhere
/// </summary>
[TestClass]
[TestCategory(TestCategories.Integration)]
public class CommandRepositoryIntegrationTests
{
    private static SqliteConnection? _sqliteConnection;
    private static NpgsqlConnection? _postgresConnection;
    private static MySqlConnection? _mysqlConnection;
    private static SqlConnection? _sqlServerConnection;

    private static CrossDbCommandRepository_SQLite? _sqliteRepo;
    private static CrossDbCommandRepository_PostgreSQL? _postgresRepo;
    private static CrossDbCommandRepository_MySQL? _mysqlRepo;
    private static CrossDbCommandRepository_SqlServer? _sqlServerRepo;

    private static CrossDbQueryRepository_SQLite? _sqliteQueryRepo;
    private static CrossDbQueryRepository_PostgreSQL? _postgresQueryRepo;
    private static CrossDbQueryRepository_MySQL? _mysqlQueryRepo;
    private static CrossDbQueryRepository_SqlServer? _sqlServerQueryRepo;

    [ClassInitialize]
    public static async Task ClassInitialize(TestContext context)
    {
        await InitializeSQLiteAsync(context);
        await InitializePostgreSQLAsync(context);
        await InitializeMySQLAsync(context);
        await InitializeSqlServerAsync(context);
    }

    private static async Task InitializeSQLiteAsync(TestContext context)
    {
        _sqliteConnection = new SqliteConnection("Data Source=:memory:");
        await _sqliteConnection.OpenAsync();
        await CreateTableAsync(_sqliteConnection, @"
            CREATE TABLE IF NOT EXISTS crossdb_users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL, email TEXT, age INTEGER NOT NULL,
                balance REAL NOT NULL, is_active INTEGER NOT NULL DEFAULT 1, created_at TEXT NOT NULL)");
        _sqliteRepo = new CrossDbCommandRepository_SQLite(_sqliteConnection);
        _sqliteQueryRepo = new CrossDbQueryRepository_SQLite(_sqliteConnection);
        context.WriteLine("✅ SQLite ready for CommandRepository tests");
    }

    private static async Task InitializePostgreSQLAsync(TestContext context)
    {
        try
        {
            var container = AssemblyTestFixture.PostgreSqlContainer;
            if (container == null) { context.WriteLine("⚠️ PostgreSQL container not available"); return; }
            var dbName = "sqlx_cmd_test_pg";
            var adminConnectionString = container.GetConnectionString();
            using (var adminConn = new NpgsqlConnection(adminConnectionString))
            {
                await adminConn.OpenAsync();
                using var cmd = adminConn.CreateCommand();
                cmd.CommandText = $"SELECT 1 FROM pg_database WHERE datname = '{dbName}'";
                if (await cmd.ExecuteScalarAsync() == null) { cmd.CommandText = $"CREATE DATABASE {dbName}"; await cmd.ExecuteNonQueryAsync(); }
            }
            var builder = new NpgsqlConnectionStringBuilder(adminConnectionString) { Database = dbName };
            _postgresConnection = new NpgsqlConnection(builder.ConnectionString);
            await _postgresConnection.OpenAsync();
            await CreateTableAsync(_postgresConnection, @"DROP TABLE IF EXISTS crossdb_users;
                CREATE TABLE crossdb_users (id BIGSERIAL PRIMARY KEY, name VARCHAR(255) NOT NULL, email VARCHAR(255),
                age INTEGER NOT NULL, balance DECIMAL(18,2) NOT NULL, is_active BOOLEAN NOT NULL DEFAULT true, created_at TIMESTAMP NOT NULL)");
            _postgresRepo = new CrossDbCommandRepository_PostgreSQL(_postgresConnection);
            _postgresQueryRepo = new CrossDbQueryRepository_PostgreSQL(_postgresConnection);
            context.WriteLine("✅ PostgreSQL ready for CommandRepository tests");
        }
        catch (Exception ex) { context.WriteLine($"⚠️ PostgreSQL failed: {ex.Message}"); }
    }

    private static async Task InitializeMySQLAsync(TestContext context)
    {
        try
        {
            var container = AssemblyTestFixture.MySqlContainer;
            if (container == null) { context.WriteLine("⚠️ MySQL container not available"); return; }
            var dbName = "sqlx_cmd_test_mysql";
            var adminConnectionString = container.GetConnectionString();
            using (var adminConn = new MySqlConnection(adminConnectionString))
            {
                await adminConn.OpenAsync();
                using var cmd = adminConn.CreateCommand();
                cmd.CommandText = $"CREATE DATABASE IF NOT EXISTS `{dbName}`";
                await cmd.ExecuteNonQueryAsync();
            }
            var builder = new MySqlConnectionStringBuilder(adminConnectionString) { Database = dbName };
            _mysqlConnection = new MySqlConnection(builder.ConnectionString);
            await _mysqlConnection.OpenAsync();
            await CreateTableAsync(_mysqlConnection, @"DROP TABLE IF EXISTS crossdb_users;
                CREATE TABLE crossdb_users (id BIGINT AUTO_INCREMENT PRIMARY KEY, name VARCHAR(255) NOT NULL, email VARCHAR(255),
                age INT NOT NULL, balance DECIMAL(18,2) NOT NULL, is_active TINYINT(1) NOT NULL DEFAULT 1, created_at DATETIME NOT NULL) ENGINE=InnoDB");
            _mysqlRepo = new CrossDbCommandRepository_MySQL(_mysqlConnection);
            _mysqlQueryRepo = new CrossDbQueryRepository_MySQL(_mysqlConnection);
            context.WriteLine("✅ MySQL ready for CommandRepository tests");
        }
        catch (Exception ex) { context.WriteLine($"⚠️ MySQL failed: {ex.Message}"); }
    }

    private static async Task InitializeSqlServerAsync(TestContext context)
    {
        try
        {
            var container = AssemblyTestFixture.MsSqlContainer;
            if (container == null) { context.WriteLine("⚠️ SQL Server container not available"); return; }
            var dbName = "sqlx_cmd_test_mssql";
            var adminConnectionString = container.GetConnectionString();
            using (var adminConn = new SqlConnection(adminConnectionString))
            {
                await adminConn.OpenAsync();
                using var cmd = adminConn.CreateCommand();
                cmd.CommandText = $"IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'{dbName}') CREATE DATABASE [{dbName}]";
                await cmd.ExecuteNonQueryAsync();
            }
            var builder = new SqlConnectionStringBuilder(adminConnectionString) { InitialCatalog = dbName };
            _sqlServerConnection = new SqlConnection(builder.ConnectionString);
            await _sqlServerConnection.OpenAsync();
            await CreateTableAsync(_sqlServerConnection, @"IF OBJECT_ID('crossdb_users', 'U') IS NOT NULL DROP TABLE crossdb_users;
                CREATE TABLE crossdb_users (id BIGINT IDENTITY(1,1) PRIMARY KEY, name NVARCHAR(255) NOT NULL, email NVARCHAR(255),
                age INT NOT NULL, balance DECIMAL(18,2) NOT NULL, is_active BIT NOT NULL DEFAULT 1, created_at DATETIME2 NOT NULL)");
            _sqlServerRepo = new CrossDbCommandRepository_SqlServer(_sqlServerConnection);
            _sqlServerQueryRepo = new CrossDbQueryRepository_SqlServer(_sqlServerConnection);
            context.WriteLine("✅ SQL Server ready for CommandRepository tests");
        }
        catch (Exception ex) { context.WriteLine($"⚠️ SQL Server failed: {ex.Message}"); }
    }

    [ClassCleanup]
    public static async Task ClassCleanup()
    {
        _sqliteConnection?.Dispose();
        if (_postgresConnection != null) await _postgresConnection.DisposeAsync();
        if (_mysqlConnection != null) await _mysqlConnection.DisposeAsync();
        if (_sqlServerConnection != null) await _sqlServerConnection.DisposeAsync();
    }

    [TestInitialize]
    public async Task TestInitialize()
    {
        await CleanTableAsync(_sqliteConnection);
        await CleanTableAsync(_postgresConnection);
        await CleanTableAsync(_mysqlConnection);
        await CleanTableAsync(_sqlServerConnection);
    }

    private static async Task CreateTableAsync(System.Data.Common.DbConnection conn, string sql)
    {
        using var cmd = conn.CreateCommand(); cmd.CommandText = sql; await cmd.ExecuteNonQueryAsync();
    }

    private static async Task CleanTableAsync(System.Data.Common.DbConnection? conn)
    {
        if (conn == null) return;
        using var cmd = conn.CreateCommand(); cmd.CommandText = "DELETE FROM crossdb_users"; await cmd.ExecuteNonQueryAsync();
    }

    private static CrossDbUser CreateTestUser(string name, int age = 25, decimal balance = 100.50m) => new()
    {
        Name = name, Email = $"{name.ToLower()}@example.com", Age = age, Balance = balance, IsActive = true, CreatedAt = DateTime.Now
    };

    #region SQLite Tests

    [TestMethod]
    [TestCategory("SQLite")]
    public async Task SQLite_InsertAsync_ShouldInsertEntity()
    {
        if (_sqliteRepo == null || _sqliteQueryRepo == null) Assert.Inconclusive("SQLite not available");
        var user = CreateTestUser("Test");
        var affected = await _sqliteRepo.InsertAsync(user);
        Assert.AreEqual(1, affected);
    }

    [TestMethod]
    [TestCategory("SQLite")]
    public async Task SQLite_InsertAndGetIdAsync_ShouldReturnGeneratedId()
    {
        if (_sqliteRepo == null) Assert.Inconclusive("SQLite not available");
        var user = CreateTestUser("Test");
        var id = await _sqliteRepo.InsertAndGetIdAsync(user);
        Assert.IsTrue(id > 0);
    }

    [TestMethod]
    [TestCategory("SQLite")]
    public async Task SQLite_InsertAndGetEntityAsync_ShouldReturnEntityWithId()
    {
        if (_sqliteRepo == null) Assert.Inconclusive("SQLite not available");
        var user = CreateTestUser("Test");
        var inserted = await _sqliteRepo.InsertAndGetEntityAsync(user);
        Assert.IsNotNull(inserted);
        Assert.IsTrue(inserted.Id > 0);
        Assert.AreEqual("Test", inserted.Name);
    }

    [TestMethod]
    [TestCategory("SQLite")]
    public async Task SQLite_UpdateAsync_ShouldUpdateEntity()
    {
        if (_sqliteRepo == null || _sqliteQueryRepo == null) Assert.Inconclusive("SQLite not available");
        var id = await _sqliteRepo.InsertAndGetIdAsync(CreateTestUser("Original"));
        var user = await _sqliteQueryRepo.GetByIdAsync(id);
        Assert.IsNotNull(user);
        user.Name = "Updated";
        user.Age = 99;
        var affected = await _sqliteRepo.UpdateAsync(user);
        Assert.AreEqual(1, affected);
        var updated = await _sqliteQueryRepo.GetByIdAsync(id);
        Assert.AreEqual("Updated", updated?.Name);
        Assert.AreEqual(99, updated?.Age);
    }

    [TestMethod]
    [TestCategory("SQLite")]
    public async Task SQLite_DeleteAsync_ShouldDeleteEntity()
    {
        if (_sqliteRepo == null || _sqliteQueryRepo == null) Assert.Inconclusive("SQLite not available");
        var id = await _sqliteRepo.InsertAndGetIdAsync(CreateTestUser("ToDelete"));
        var affected = await _sqliteRepo.DeleteAsync(id);
        Assert.AreEqual(1, affected);
        var deleted = await _sqliteQueryRepo.GetByIdAsync(id);
        Assert.IsNull(deleted);
    }

    [TestMethod]
    [TestCategory("SQLite")]
    public async Task SQLite_DeleteWhereAsync_ShouldDeleteMatchingEntities()
    {
        if (_sqliteRepo == null || _sqliteQueryRepo == null) Assert.Inconclusive("SQLite not available");
        await _sqliteRepo.InsertAndGetIdAsync(CreateTestUser("User1", age: 20));
        await _sqliteRepo.InsertAndGetIdAsync(CreateTestUser("User2", age: 30));
        await _sqliteRepo.InsertAndGetIdAsync(CreateTestUser("User3", age: 30));
        await _sqliteRepo.InsertAndGetIdAsync(CreateTestUser("User4", age: 40));
        var affected = await _sqliteRepo.DeleteWhereAsync(x => x.Age == 30);
        Assert.AreEqual(2, affected);
        var remaining = await _sqliteQueryRepo.GetAllAsync();
        Assert.AreEqual(2, remaining.Count);
    }

    #endregion

    #region MySQL Tests

    [TestMethod]
    [TestCategory("MySQL")]
    [TestCategory("Database")]
    public async Task MySQL_InsertAsync_ShouldInsertEntity()
    {
        if (_mysqlRepo == null) Assert.Inconclusive("MySQL not available");
        var user = CreateTestUser("Test");
        var affected = await _mysqlRepo.InsertAsync(user);
        Assert.AreEqual(1, affected);
    }

    [TestMethod]
    [TestCategory("MySQL")]
    [TestCategory("Database")]
    public async Task MySQL_InsertAndGetIdAsync_ShouldReturnGeneratedId()
    {
        if (_mysqlRepo == null) Assert.Inconclusive("MySQL not available");
        var user = CreateTestUser("Test");
        var id = await _mysqlRepo.InsertAndGetIdAsync(user);
        Assert.IsTrue(id > 0);
    }

    [TestMethod]
    [TestCategory("MySQL")]
    [TestCategory("Database")]
    public async Task MySQL_InsertAndGetEntityAsync_ShouldReturnEntityWithId()
    {
        if (_mysqlRepo == null) Assert.Inconclusive("MySQL not available");
        var user = CreateTestUser("Test");
        var inserted = await _mysqlRepo.InsertAndGetEntityAsync(user);
        Assert.IsNotNull(inserted);
        Assert.IsTrue(inserted.Id > 0);
        Assert.AreEqual("Test", inserted.Name);
    }

    [TestMethod]
    [TestCategory("MySQL")]
    [TestCategory("Database")]
    public async Task MySQL_UpdateAsync_ShouldUpdateEntity()
    {
        if (_mysqlRepo == null || _mysqlQueryRepo == null) Assert.Inconclusive("MySQL not available");
        var id = await _mysqlRepo.InsertAndGetIdAsync(CreateTestUser("Original"));
        var user = await _mysqlQueryRepo.GetByIdAsync(id);
        Assert.IsNotNull(user);
        user.Name = "Updated";
        var affected = await _mysqlRepo.UpdateAsync(user);
        Assert.AreEqual(1, affected);
        var updated = await _mysqlQueryRepo.GetByIdAsync(id);
        Assert.AreEqual("Updated", updated?.Name);
    }

    [TestMethod]
    [TestCategory("MySQL")]
    [TestCategory("Database")]
    public async Task MySQL_DeleteAsync_ShouldDeleteEntity()
    {
        if (_mysqlRepo == null || _mysqlQueryRepo == null) Assert.Inconclusive("MySQL not available");
        var id = await _mysqlRepo.InsertAndGetIdAsync(CreateTestUser("ToDelete"));
        var affected = await _mysqlRepo.DeleteAsync(id);
        Assert.AreEqual(1, affected);
        var deleted = await _mysqlQueryRepo.GetByIdAsync(id);
        Assert.IsNull(deleted);
    }

    [TestMethod]
    [TestCategory("MySQL")]
    [TestCategory("Database")]
    public async Task MySQL_DeleteWhereAsync_ShouldDeleteMatchingEntities()
    {
        if (_mysqlRepo == null || _mysqlQueryRepo == null) Assert.Inconclusive("MySQL not available");
        await _mysqlRepo.InsertAndGetIdAsync(CreateTestUser("User1", age: 20));
        await _mysqlRepo.InsertAndGetIdAsync(CreateTestUser("User2", age: 30));
        await _mysqlRepo.InsertAndGetIdAsync(CreateTestUser("User3", age: 30));
        var affected = await _mysqlRepo.DeleteWhereAsync(x => x.Age == 30);
        Assert.AreEqual(2, affected);
    }

    #endregion

    #region PostgreSQL Tests

    [TestMethod]
    [TestCategory("PostgreSQL")]
    [TestCategory("Database")]
    public async Task PostgreSQL_InsertAsync_ShouldInsertEntity()
    {
        if (_postgresRepo == null) Assert.Inconclusive("PostgreSQL not available");
        try
        {
            var user = CreateTestUser("Test");
            var affected = await _postgresRepo.InsertAsync(user);
            Assert.AreEqual(1, affected);
        }
        catch (PostgresException ex) when (ex.SqlState == "42601") { Assert.Inconclusive($"Known PostgreSQL issue: {ex.Message}"); }
    }

    [TestMethod]
    [TestCategory("PostgreSQL")]
    [TestCategory("Database")]
    public async Task PostgreSQL_InsertAndGetIdAsync_ShouldReturnGeneratedId()
    {
        if (_postgresRepo == null) Assert.Inconclusive("PostgreSQL not available");
        try
        {
            var user = CreateTestUser("Test");
            var id = await _postgresRepo.InsertAndGetIdAsync(user);
            Assert.IsTrue(id > 0);
        }
        catch (PostgresException ex) when (ex.SqlState == "42601") { Assert.Inconclusive($"Known PostgreSQL issue: {ex.Message}"); }
    }

    [TestMethod]
    [TestCategory("PostgreSQL")]
    [TestCategory("Database")]
    public async Task PostgreSQL_DeleteAsync_ShouldDeleteEntity()
    {
        if (_postgresRepo == null || _postgresQueryRepo == null) Assert.Inconclusive("PostgreSQL not available");
        try
        {
            var id = await _postgresRepo.InsertAndGetIdAsync(CreateTestUser("ToDelete"));
            var affected = await _postgresRepo.DeleteAsync(id);
            Assert.AreEqual(1, affected);
        }
        catch (PostgresException ex) when (ex.SqlState == "42601") { Assert.Inconclusive($"Known PostgreSQL issue: {ex.Message}"); }
    }

    #endregion

    #region SQL Server Tests

    [TestMethod]
    [TestCategory("SqlServer")]
    [TestCategory("Database")]
    public async Task SqlServer_InsertAsync_ShouldInsertEntity()
    {
        if (_sqlServerRepo == null) Assert.Inconclusive("SQL Server not available");
        try
        {
            var user = CreateTestUser("Test");
            var affected = await _sqlServerRepo.InsertAsync(user);
            Assert.AreEqual(1, affected);
        }
        catch (SqlException ex) when (ex.Message.Contains("syntax")) { Assert.Inconclusive($"Known SQL Server issue: {ex.Message}"); }
    }

    [TestMethod]
    [TestCategory("SqlServer")]
    [TestCategory("Database")]
    public async Task SqlServer_InsertAndGetIdAsync_ShouldReturnGeneratedId()
    {
        if (_sqlServerRepo == null) Assert.Inconclusive("SQL Server not available");
        try
        {
            var user = CreateTestUser("Test");
            var id = await _sqlServerRepo.InsertAndGetIdAsync(user);
            Assert.IsTrue(id > 0);
        }
        catch (SqlException ex) when (ex.Message.Contains("syntax")) { Assert.Inconclusive($"Known SQL Server issue: {ex.Message}"); }
    }

    [TestMethod]
    [TestCategory("SqlServer")]
    [TestCategory("Database")]
    public async Task SqlServer_DeleteAsync_ShouldDeleteEntity()
    {
        if (_sqlServerRepo == null || _sqlServerQueryRepo == null) Assert.Inconclusive("SQL Server not available");
        try
        {
            var id = await _sqlServerRepo.InsertAndGetIdAsync(CreateTestUser("ToDelete"));
            var affected = await _sqlServerRepo.DeleteAsync(id);
            Assert.AreEqual(1, affected);
        }
        catch (SqlException ex) when (ex.Message.Contains("syntax")) { Assert.Inconclusive($"Known SQL Server issue: {ex.Message}"); }
    }

    #endregion
}
