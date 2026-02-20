// <copyright file="MultipleResultSetsE2ETests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MySqlConnector;
using Npgsql;
using Sqlx;
using Sqlx.Annotations;
using System.Data.Common;
using System.Threading.Tasks;
using Testcontainers.MsSql;
using Testcontainers.MySql;
using Testcontainers.PostgreSql;

namespace Sqlx.Tests;

/// <summary>
/// E2E tests for multiple result sets functionality across different databases using Testcontainers.
/// These tests require Docker to be running. If Docker is not available, tests will be marked as Inconclusive.
/// </summary>
[TestClass]
public class MultipleResultSetsE2ETests
{
    private static MySqlContainer? _mySqlContainer;
    private static PostgreSqlContainer? _postgreSqlContainer;
    private static MsSqlContainer? _msSqlContainer;

    [ClassInitialize]
    public static async Task ClassInitialize(TestContext context)
    {
        // Check if Docker is available before starting containers
        if (!IsDockerAvailable())
        {
            context.WriteLine("Docker is not available. Skipping E2E test container initialization.");
            context.WriteLine("To run these tests, ensure Docker Desktop is running.");
            return;
        }

        try
        {
            // Start MySQL container
            _mySqlContainer = new MySqlBuilder()
                .WithImage("mysql:8.0")
                .WithDatabase("testdb")
                .WithUsername("testuser")
                .WithPassword("testpass")
                .Build();
            await _mySqlContainer.StartAsync();

            // Start PostgreSQL container
            _postgreSqlContainer = new PostgreSqlBuilder()
                .WithImage("postgres:16")
                .WithDatabase("testdb")
                .WithUsername("testuser")
                .WithPassword("testpass")
                .Build();
            await _postgreSqlContainer.StartAsync();

            // Start SQL Server container
            _msSqlContainer = new MsSqlBuilder()
                .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
                .WithPassword("Test@1234")
                .Build();
            await _msSqlContainer.StartAsync();
        }
        catch (Exception ex)
        {
            context.WriteLine($"Failed to start containers: {ex.Message}");
            context.WriteLine("Ensure Docker is running and has sufficient resources.");
            throw;
        }
    }

    private static bool IsDockerAvailable()
    {
        try
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = "info",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            process.WaitForExit(5000); // 5 second timeout
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    [ClassCleanup]
    public static async Task ClassCleanup()
    {
        if (_mySqlContainer != null)
            await _mySqlContainer.DisposeAsync();
        if (_postgreSqlContainer != null)
            await _postgreSqlContainer.DisposeAsync();
        if (_msSqlContainer != null)
            await _msSqlContainer.DisposeAsync();
    }

    #region MySQL Tests

    [TestMethod]
    public async Task MySQL_InsertAndGetStatsAsync_ReturnsCorrectValues()
    {
        // Skip if Docker is not available
        if (_mySqlContainer == null)
        {
            Assert.Inconclusive("Docker is not available. Test skipped.");
            return;
        }

        // Arrange
        using var connection = new MySqlConnection(_mySqlContainer.GetConnectionString());
        await connection.OpenAsync();
        await CreateMySqlTableAsync(connection);
        var repo = new MySqlMultiResultRepository(connection);

        // Act
        var (rows, userId, total) = await repo.InsertAndGetStatsAsync("Alice", 25);

        // Assert
        Assert.AreEqual(1, rows);
        Assert.IsTrue(userId > 0);
        Assert.AreEqual(1, total);
    }

    [TestMethod]
    public async Task MySQL_GetStatsAsync_ReturnsCorrectStats()
    {
        // Skip if Docker is not available
        if (_mySqlContainer == null)
        {
            Assert.Inconclusive("Docker is not available. Test skipped.");
            return;
        }

        // Arrange
        using var connection = new MySqlConnection(_mySqlContainer.GetConnectionString());
        await connection.OpenAsync();
        await CreateMySqlTableAsync(connection);
        var repo = new MySqlMultiResultRepository(connection);
        await repo.InsertAndGetStatsAsync("Alice", 25);
        await repo.InsertAndGetStatsAsync("Bob", 30);
        await repo.InsertAndGetStatsAsync("Charlie", 35);

        // Act
        var (total, maxId, minId) = await repo.GetStatsAsync();

        // Assert
        Assert.AreEqual(3, total);
        Assert.IsTrue(maxId >= minId);
    }

    [TestMethod]
    public void MySQL_InsertAndGetStats_SyncMethod_ReturnsCorrectValues()
    {
        // Skip if Docker is not available
        if (_mySqlContainer == null)
        {
            Assert.Inconclusive("Docker is not available. Test skipped.");
            return;
        }

        // Arrange
        using var connection = new MySqlConnection(_mySqlContainer.GetConnectionString());
        connection.Open();
        CreateMySqlTable(connection);
        var repo = new MySqlMultiResultRepository(connection);

        // Act
        var (rows, userId, total) = repo.InsertAndGetStats("Alice", 25);

        // Assert
        Assert.AreEqual(1, rows);
        Assert.IsTrue(userId > 0);
        Assert.AreEqual(1, total);
    }

    private static async Task CreateMySqlTableAsync(DbConnection connection)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS test_users (
                id BIGINT AUTO_INCREMENT PRIMARY KEY,
                name VARCHAR(255) NOT NULL,
                age INT
            )";
        await cmd.ExecuteNonQueryAsync();

        // Clear existing data
        cmd.CommandText = "DELETE FROM test_users";
        await cmd.ExecuteNonQueryAsync();
    }

    private static void CreateMySqlTable(DbConnection connection)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS test_users (
                id BIGINT AUTO_INCREMENT PRIMARY KEY,
                name VARCHAR(255) NOT NULL,
                age INT
            )";
        cmd.ExecuteNonQuery();

        // Clear existing data
        cmd.CommandText = "DELETE FROM test_users";
        cmd.ExecuteNonQuery();
    }

    #endregion

    #region PostgreSQL Tests

    [TestMethod]
    public async Task PostgreSQL_InsertAndGetStatsAsync_ReturnsCorrectValues()
    {
        // Skip if Docker is not available
        if (_postgreSqlContainer == null)
        {
            Assert.Inconclusive("Docker is not available. Test skipped.");
            return;
        }

        // Arrange
        using var connection = new NpgsqlConnection(_postgreSqlContainer.GetConnectionString());
        await connection.OpenAsync();
        await CreatePostgreSqlTableAsync(connection);
        var repo = new PostgreSqlMultiResultRepository(connection);

        // Act
        var (rows, userId, total) = await repo.InsertAndGetStatsAsync("Alice", 25);

        // Assert
        Assert.AreEqual(1, rows);
        Assert.IsTrue(userId > 0);
        Assert.AreEqual(1, total);
    }

    [TestMethod]
    public async Task PostgreSQL_GetStatsAsync_ReturnsCorrectStats()
    {
        // Skip if Docker is not available
        if (_postgreSqlContainer == null)
        {
            Assert.Inconclusive("Docker is not available. Test skipped.");
            return;
        }

        // Arrange
        using var connection = new NpgsqlConnection(_postgreSqlContainer.GetConnectionString());
        await connection.OpenAsync();
        await CreatePostgreSqlTableAsync(connection);
        var repo = new PostgreSqlMultiResultRepository(connection);
        await repo.InsertAndGetStatsAsync("Alice", 25);
        await repo.InsertAndGetStatsAsync("Bob", 30);
        await repo.InsertAndGetStatsAsync("Charlie", 35);

        // Act
        var (total, maxId, minId) = await repo.GetStatsAsync();

        // Assert
        Assert.AreEqual(3, total);
        Assert.IsTrue(maxId >= minId);
    }

    [TestMethod]
    public void PostgreSQL_InsertAndGetStats_SyncMethod_ReturnsCorrectValues()
    {
        // Skip if Docker is not available
        if (_postgreSqlContainer == null)
        {
            Assert.Inconclusive("Docker is not available. Test skipped.");
            return;
        }

        // Arrange
        using var connection = new NpgsqlConnection(_postgreSqlContainer.GetConnectionString());
        connection.Open();
        CreatePostgreSqlTable(connection);
        var repo = new PostgreSqlMultiResultRepository(connection);

        // Act
        var (rows, userId, total) = repo.InsertAndGetStats("Alice", 25);

        // Assert
        Assert.AreEqual(1, rows);
        Assert.IsTrue(userId > 0);
        Assert.AreEqual(1, total);
    }

    private static async Task CreatePostgreSqlTableAsync(DbConnection connection)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS test_users (
                id BIGSERIAL PRIMARY KEY,
                name VARCHAR(255) NOT NULL,
                age INT
            )";
        await cmd.ExecuteNonQueryAsync();

        // Clear existing data
        cmd.CommandText = "DELETE FROM test_users";
        await cmd.ExecuteNonQueryAsync();
    }

    private static void CreatePostgreSqlTable(DbConnection connection)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS test_users (
                id BIGSERIAL PRIMARY KEY,
                name VARCHAR(255) NOT NULL,
                age INT
            )";
        cmd.ExecuteNonQuery();

        // Clear existing data
        cmd.CommandText = "DELETE FROM test_users";
        cmd.ExecuteNonQuery();
    }

    #endregion

    #region SQL Server Tests

    [TestMethod]
    public async Task SqlServer_InsertAndGetStatsAsync_ReturnsCorrectValues()
    {
        // Skip if Docker is not available
        if (_msSqlContainer == null)
        {
            Assert.Inconclusive("Docker is not available. Test skipped.");
            return;
        }

        // Arrange
        using var connection = new SqlConnection(_msSqlContainer.GetConnectionString());
        await connection.OpenAsync();
        await CreateSqlServerTableAsync(connection);
        var repo = new SqlServerMultiResultRepository(connection);

        // Act
        var (rows, userId, total) = await repo.InsertAndGetStatsAsync("Alice", 25);

        // Assert
        Assert.AreEqual(1, rows);
        Assert.IsTrue(userId > 0);
        Assert.AreEqual(1, total);
    }

    [TestMethod]
    public async Task SqlServer_GetStatsAsync_ReturnsCorrectStats()
    {
        // Skip if Docker is not available
        if (_msSqlContainer == null)
        {
            Assert.Inconclusive("Docker is not available. Test skipped.");
            return;
        }

        // Arrange
        using var connection = new SqlConnection(_msSqlContainer.GetConnectionString());
        await connection.OpenAsync();
        await CreateSqlServerTableAsync(connection);
        var repo = new SqlServerMultiResultRepository(connection);
        await repo.InsertAndGetStatsAsync("Alice", 25);
        await repo.InsertAndGetStatsAsync("Bob", 30);
        await repo.InsertAndGetStatsAsync("Charlie", 35);

        // Act
        var (total, maxId, minId) = await repo.GetStatsAsync();

        // Assert
        Assert.AreEqual(3, total);
        Assert.IsTrue(maxId >= minId);
    }

    [TestMethod]
    public void SqlServer_InsertAndGetStats_SyncMethod_ReturnsCorrectValues()
    {
        // Skip if Docker is not available
        if (_msSqlContainer == null)
        {
            Assert.Inconclusive("Docker is not available. Test skipped.");
            return;
        }

        // Arrange
        using var connection = new SqlConnection(_msSqlContainer.GetConnectionString());
        connection.Open();
        CreateSqlServerTable(connection);
        var repo = new SqlServerMultiResultRepository(connection);

        // Act
        var (rows, userId, total) = repo.InsertAndGetStats("Alice", 25);

        // Assert
        Assert.AreEqual(1, rows);
        Assert.IsTrue(userId > 0);
        Assert.AreEqual(1, total);
    }

    private static async Task CreateSqlServerTableAsync(DbConnection connection)
    {
        using var cmd = connection.CreateCommand();
        
        // Drop table if exists
        cmd.CommandText = "IF OBJECT_ID('test_users', 'U') IS NOT NULL DROP TABLE test_users";
        await cmd.ExecuteNonQueryAsync();

        // Create table
        cmd.CommandText = @"
            CREATE TABLE test_users (
                id BIGINT IDENTITY(1,1) PRIMARY KEY,
                name NVARCHAR(255) NOT NULL,
                age INT
            )";
        await cmd.ExecuteNonQueryAsync();
    }

    private static void CreateSqlServerTable(DbConnection connection)
    {
        using var cmd = connection.CreateCommand();
        
        // Drop table if exists
        cmd.CommandText = "IF OBJECT_ID('test_users', 'U') IS NOT NULL DROP TABLE test_users";
        cmd.ExecuteNonQuery();

        // Create table
        cmd.CommandText = @"
            CREATE TABLE test_users (
                id BIGINT IDENTITY(1,1) PRIMARY KEY,
                name NVARCHAR(255) NOT NULL,
                age INT
            )";
        cmd.ExecuteNonQuery();
    }

    #endregion
}

#region Repository Interfaces and Implementations

// MySQL Repository
public interface IMySqlMultiResultRepository
{
    [SqlTemplate(@"
        INSERT INTO test_users (name, age) VALUES (@name, @age);
        SELECT LAST_INSERT_ID();
        SELECT COUNT(*) FROM test_users
    ")]
    [ResultSetMapping(0, "rowsAffected")]
    [ResultSetMapping(1, "userId")]
    [ResultSetMapping(2, "totalUsers")]
    Task<(int rowsAffected, long userId, int totalUsers)> InsertAndGetStatsAsync(string name, int age);

    [SqlTemplate(@"
        INSERT INTO test_users (name, age) VALUES (@name, @age);
        SELECT LAST_INSERT_ID();
        SELECT COUNT(*) FROM test_users
    ")]
    [ResultSetMapping(0, "rowsAffected")]
    [ResultSetMapping(1, "userId")]
    [ResultSetMapping(2, "totalUsers")]
    (int rowsAffected, long userId, int totalUsers) InsertAndGetStats(string name, int age);

    [SqlTemplate(@"
        SELECT COUNT(*) FROM test_users;
        SELECT MAX(id) FROM test_users;
        SELECT MIN(id) FROM test_users
    ")]
    [ResultSetMapping(0, "totalUsers")]
    [ResultSetMapping(1, "maxId")]
    [ResultSetMapping(2, "minId")]
    Task<(int totalUsers, long maxId, long minId)> GetStatsAsync();
}

[RepositoryFor(typeof(IMySqlMultiResultRepository), Dialect = (int)SqlDefineTypes.MySql, TableName = "test_users")]
public partial class MySqlMultiResultRepository : IMySqlMultiResultRepository
{
    private readonly DbConnection _connection;

    public MySqlMultiResultRepository(DbConnection connection)
    {
        _connection = connection;
    }
}

// PostgreSQL Repository
public interface IPostgreSqlMultiResultRepository
{
    [SqlTemplate(@"
        INSERT INTO test_users (name, age) VALUES (@name, @age);
        SELECT currval(pg_get_serial_sequence('test_users', 'id'));
        SELECT COUNT(*) FROM test_users
    ")]
    [ResultSetMapping(0, "rowsAffected")]
    [ResultSetMapping(1, "userId")]
    [ResultSetMapping(2, "totalUsers")]
    Task<(int rowsAffected, long userId, int totalUsers)> InsertAndGetStatsAsync(string name, int age);

    [SqlTemplate(@"
        INSERT INTO test_users (name, age) VALUES (@name, @age);
        SELECT currval(pg_get_serial_sequence('test_users', 'id'));
        SELECT COUNT(*) FROM test_users
    ")]
    [ResultSetMapping(0, "rowsAffected")]
    [ResultSetMapping(1, "userId")]
    [ResultSetMapping(2, "totalUsers")]
    (int rowsAffected, long userId, int totalUsers) InsertAndGetStats(string name, int age);

    [SqlTemplate(@"
        SELECT COUNT(*) FROM test_users;
        SELECT MAX(id) FROM test_users;
        SELECT MIN(id) FROM test_users
    ")]
    [ResultSetMapping(0, "totalUsers")]
    [ResultSetMapping(1, "maxId")]
    [ResultSetMapping(2, "minId")]
    Task<(int totalUsers, long maxId, long minId)> GetStatsAsync();
}

[RepositoryFor(typeof(IPostgreSqlMultiResultRepository), Dialect = (int)SqlDefineTypes.PostgreSql, TableName = "test_users")]
public partial class PostgreSqlMultiResultRepository : IPostgreSqlMultiResultRepository
{
    private readonly DbConnection _connection;

    public PostgreSqlMultiResultRepository(DbConnection connection)
    {
        _connection = connection;
    }
}

// SQL Server Repository
public interface ISqlServerMultiResultRepository
{
    [SqlTemplate(@"
        INSERT INTO test_users (name, age) VALUES (@name, @age);
        SELECT SCOPE_IDENTITY();
        SELECT COUNT(*) FROM test_users
    ")]
    [ResultSetMapping(0, "rowsAffected")]
    [ResultSetMapping(1, "userId")]
    [ResultSetMapping(2, "totalUsers")]
    Task<(int rowsAffected, long userId, int totalUsers)> InsertAndGetStatsAsync(string name, int age);

    [SqlTemplate(@"
        INSERT INTO test_users (name, age) VALUES (@name, @age);
        SELECT SCOPE_IDENTITY();
        SELECT COUNT(*) FROM test_users
    ")]
    [ResultSetMapping(0, "rowsAffected")]
    [ResultSetMapping(1, "userId")]
    [ResultSetMapping(2, "totalUsers")]
    (int rowsAffected, long userId, int totalUsers) InsertAndGetStats(string name, int age);

    [SqlTemplate(@"
        SELECT COUNT(*) FROM test_users;
        SELECT MAX(id) FROM test_users;
        SELECT MIN(id) FROM test_users
    ")]
    [ResultSetMapping(0, "totalUsers")]
    [ResultSetMapping(1, "maxId")]
    [ResultSetMapping(2, "minId")]
    Task<(int totalUsers, long maxId, long minId)> GetStatsAsync();
}

[RepositoryFor(typeof(ISqlServerMultiResultRepository), Dialect = (int)SqlDefineTypes.SqlServer, TableName = "test_users")]
public partial class SqlServerMultiResultRepository : ISqlServerMultiResultRepository
{
    private readonly DbConnection _connection;

    public SqlServerMultiResultRepository(DbConnection connection)
    {
        _connection = connection;
    }
}

#endregion
