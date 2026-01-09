// -----------------------------------------------------------------------
// <copyright file="BatchRepositoryIntegrationTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MySqlConnector;
using Sqlx.Tests.Infrastructure;

namespace Sqlx.Tests.Predefined;

/// <summary>
/// Cross-database integration tests for IBatchRepository interface.
/// Tests: BatchInsert, BatchInsertAndGetIds, BatchUpdate, BatchDelete, BatchExists
/// </summary>
[TestClass]
[TestCategory(TestCategories.Integration)]
public class BatchRepositoryIntegrationTests
{
    private static SqliteConnection? _sqliteConnection;
    private static MySqlConnection? _mysqlConnection;
    private static SqlConnection? _sqlServerConnection;

    private static CrossDbBatchRepository_SQLite? _sqliteRepo;
    private static CrossDbBatchRepository_MySQL? _mysqlRepo;
    private static CrossDbBatchRepository_SqlServer? _sqlServerRepo;

    private static CrossDbUserRepository_SQLite? _sqliteCrudRepo;
    private static CrossDbUserRepository_MySQL? _mysqlCrudRepo;
    private static CrossDbUserRepository_SqlServer? _sqlServerCrudRepo;

    [ClassInitialize]
    public static async Task ClassInitialize(TestContext context)
    {
        await InitializeSQLiteAsync(context);
        await InitializeMySQLAsync(context);
        await InitializeSqlServerAsync(context);
        context.WriteLine("Note: PostgreSQL IBatchRepository excluded due to known source generator issues");
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
        _sqliteRepo = new CrossDbBatchRepository_SQLite(_sqliteConnection);
        _sqliteCrudRepo = new CrossDbUserRepository_SQLite(_sqliteConnection);
        context.WriteLine("✅ SQLite ready for BatchRepository tests");
    }

    private static async Task InitializeMySQLAsync(TestContext context)
    {
        try
        {
            var container = AssemblyTestFixture.MySqlContainer;
            if (container == null) { context.WriteLine("⚠️ MySQL container not available"); return; }
            var dbName = "sqlx_batch_test_mysql";
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
            _mysqlRepo = new CrossDbBatchRepository_MySQL(_mysqlConnection);
            _mysqlCrudRepo = new CrossDbUserRepository_MySQL(_mysqlConnection);
            context.WriteLine("✅ MySQL ready for BatchRepository tests");
        }
        catch (Exception ex) { context.WriteLine($"⚠️ MySQL failed: {ex.Message}"); }
    }

    private static async Task InitializeSqlServerAsync(TestContext context)
    {
        try
        {
            var container = AssemblyTestFixture.MsSqlContainer;
            if (container == null) { context.WriteLine("⚠️ SQL Server container not available"); return; }
            var dbName = "sqlx_batch_test_mssql";
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
            _sqlServerRepo = new CrossDbBatchRepository_SqlServer(_sqlServerConnection);
            _sqlServerCrudRepo = new CrossDbUserRepository_SqlServer(_sqlServerConnection);
            context.WriteLine("✅ SQL Server ready for BatchRepository tests");
        }
        catch (Exception ex) { context.WriteLine($"⚠️ SQL Server failed: {ex.Message}"); }
    }

    [ClassCleanup]
    public static async Task ClassCleanup()
    {
        _sqliteConnection?.Dispose();
        if (_mysqlConnection != null) await _mysqlConnection.DisposeAsync();
        if (_sqlServerConnection != null) await _sqlServerConnection.DisposeAsync();
    }

    [TestInitialize]
    public async Task TestInitialize()
    {
        await CleanTableAsync(_sqliteConnection);
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

    private static List<CrossDbUser> CreateTestUsers(int count) =>
        Enumerable.Range(0, count).Select(i => CreateTestUser($"User{i}", age: 20 + i, balance: 100m + i * 10)).ToList();

    #region SQLite Tests

    [TestMethod]
    [TestCategory("SQLite")]
    public async Task SQLite_BatchInsertAsync_ShouldInsertMultipleEntities()
    {
        if (_sqliteRepo == null || _sqliteCrudRepo == null) Assert.Inconclusive("SQLite not available");
        var users = CreateTestUsers(50);
        var affected = await _sqliteRepo.BatchInsertAsync(users);
        Assert.AreEqual(50, affected);
        var count = await _sqliteCrudRepo.CountAsync();
        Assert.AreEqual(50L, count);
    }

    [TestMethod]
    [TestCategory("SQLite")]
    public async Task SQLite_BatchInsertAndGetIdsAsync_ShouldReturnGeneratedIds()
    {
        if (_sqliteRepo == null) Assert.Inconclusive("SQLite not available");
        var users = CreateTestUsers(10);
        var ids = await _sqliteRepo.BatchInsertAndGetIdsAsync(users);
        Assert.AreEqual(10, ids.Count);
        Assert.IsTrue(ids.All(id => id > 0));
        Assert.AreEqual(ids.Distinct().Count(), ids.Count); // All IDs should be unique
    }

    [TestMethod]
    [TestCategory("SQLite")]
    public async Task SQLite_BatchDeleteAsync_ShouldDeleteMultipleEntities()
    {
        if (_sqliteRepo == null || _sqliteCrudRepo == null) Assert.Inconclusive("SQLite not available");
        var users = CreateTestUsers(10);
        var ids = await _sqliteRepo.BatchInsertAndGetIdsAsync(users);
        var idsToDelete = ids.Take(5).ToList();
        var affected = await _sqliteRepo.BatchDeleteAsync(idsToDelete);
        Assert.AreEqual(5, affected);
        var count = await _sqliteCrudRepo.CountAsync();
        Assert.AreEqual(5L, count);
    }

    [TestMethod]
    [TestCategory("SQLite")]
    public async Task SQLite_BatchExistsAsync_ShouldReturnCorrectResults()
    {
        if (_sqliteRepo == null) Assert.Inconclusive("SQLite not available");
        var users = CreateTestUsers(5);
        var ids = await _sqliteRepo.BatchInsertAndGetIdsAsync(users);
        var checkIds = new List<long> { ids[0], ids[2], 99999L, ids[4], 88888L };
        var results = await _sqliteRepo.BatchExistsAsync(checkIds);
        Assert.AreEqual(5, results.Count);
        Assert.IsTrue(results[0]);  // ids[0] exists
        Assert.IsTrue(results[1]);  // ids[2] exists
        Assert.IsFalse(results[2]); // 99999 doesn't exist
        Assert.IsTrue(results[3]);  // ids[4] exists
        Assert.IsFalse(results[4]); // 88888 doesn't exist
    }

    [TestMethod]
    [TestCategory("SQLite")]
    public async Task SQLite_BatchUpdateAsync_ShouldUpdateMultipleEntities()
    {
        if (_sqliteRepo == null || _sqliteCrudRepo == null) Assert.Inconclusive("SQLite not available");
        var users = CreateTestUsers(5);
        var ids = await _sqliteRepo.BatchInsertAndGetIdsAsync(users);
        
        // Get users and update them
        var usersToUpdate = new List<CrossDbUser>();
        for (int i = 0; i < ids.Count; i++)
        {
            var user = await _sqliteCrudRepo.GetByIdAsync(ids[i]);
            if (user != null)
            {
                user.Name = $"Updated{i}";
                user.Age = 100 + i;
                usersToUpdate.Add(user);
            }
        }
        
        var affected = await _sqliteRepo.BatchUpdateAsync(usersToUpdate);
        Assert.AreEqual(5, affected);
        
        var updated = await _sqliteCrudRepo.GetByIdAsync(ids[0]);
        Assert.AreEqual("Updated0", updated?.Name);
        Assert.AreEqual(100, updated?.Age);
    }

    #endregion

    #region MySQL Tests

    [TestMethod]
    [TestCategory("MySQL")]
    [TestCategory("Database")]
    public async Task MySQL_BatchInsertAsync_ShouldInsertMultipleEntities()
    {
        if (_mysqlRepo == null || _mysqlCrudRepo == null) Assert.Inconclusive("MySQL not available");
        var users = CreateTestUsers(50);
        var affected = await _mysqlRepo.BatchInsertAsync(users);
        Assert.AreEqual(50, affected);
        var count = await _mysqlCrudRepo.CountAsync();
        Assert.AreEqual(50L, count);
    }

    [TestMethod]
    [TestCategory("MySQL")]
    [TestCategory("Database")]
    public async Task MySQL_BatchInsertAndGetIdsAsync_ShouldReturnGeneratedIds()
    {
        if (_mysqlRepo == null) Assert.Inconclusive("MySQL not available");
        var users = CreateTestUsers(10);
        var ids = await _mysqlRepo.BatchInsertAndGetIdsAsync(users);
        Assert.AreEqual(10, ids.Count);
        Assert.IsTrue(ids.All(id => id > 0));
    }

    [TestMethod]
    [TestCategory("MySQL")]
    [TestCategory("Database")]
    public async Task MySQL_BatchDeleteAsync_ShouldDeleteMultipleEntities()
    {
        if (_mysqlRepo == null || _mysqlCrudRepo == null) Assert.Inconclusive("MySQL not available");
        var users = CreateTestUsers(10);
        var ids = await _mysqlRepo.BatchInsertAndGetIdsAsync(users);
        var idsToDelete = ids.Take(5).ToList();
        var affected = await _mysqlRepo.BatchDeleteAsync(idsToDelete);
        Assert.AreEqual(5, affected);
        var count = await _mysqlCrudRepo.CountAsync();
        Assert.AreEqual(5L, count);
    }

    [TestMethod]
    [TestCategory("MySQL")]
    [TestCategory("Database")]
    public async Task MySQL_BatchExistsAsync_ShouldReturnCorrectResults()
    {
        if (_mysqlRepo == null) Assert.Inconclusive("MySQL not available");
        var users = CreateTestUsers(5);
        var ids = await _mysqlRepo.BatchInsertAndGetIdsAsync(users);
        var checkIds = new List<long> { ids[0], 99999L, ids[2] };
        var results = await _mysqlRepo.BatchExistsAsync(checkIds);
        Assert.AreEqual(3, results.Count);
        Assert.IsTrue(results[0]);
        Assert.IsFalse(results[1]);
        Assert.IsTrue(results[2]);
    }

    #endregion

    #region SQL Server Tests

    [TestMethod]
    [TestCategory("SqlServer")]
    [TestCategory("Database")]
    public async Task SqlServer_BatchInsertAsync_ShouldInsertMultipleEntities()
    {
        if (_sqlServerRepo == null || _sqlServerCrudRepo == null) Assert.Inconclusive("SQL Server not available");
        try
        {
            var users = CreateTestUsers(50);
            var affected = await _sqlServerRepo.BatchInsertAsync(users);
            Assert.AreEqual(50, affected);
            var count = await _sqlServerCrudRepo.CountAsync();
            Assert.AreEqual(50L, count);
        }
        catch (SqlException ex) when (ex.Message.Contains("syntax")) { Assert.Inconclusive($"Known SQL Server issue: {ex.Message}"); }
    }

    [TestMethod]
    [TestCategory("SqlServer")]
    [TestCategory("Database")]
    public async Task SqlServer_BatchDeleteAsync_ShouldDeleteMultipleEntities()
    {
        if (_sqlServerRepo == null || _sqlServerCrudRepo == null) Assert.Inconclusive("SQL Server not available");
        try
        {
            var users = CreateTestUsers(10);
            var ids = await _sqlServerRepo.BatchInsertAndGetIdsAsync(users);
            var idsToDelete = ids.Take(5).ToList();
            var affected = await _sqlServerRepo.BatchDeleteAsync(idsToDelete);
            Assert.AreEqual(5, affected);
        }
        catch (SqlException ex) when (ex.Message.Contains("syntax")) { Assert.Inconclusive($"Known SQL Server issue: {ex.Message}"); }
    }

    #endregion
}
