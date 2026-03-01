// <copyright file="SqlBuilderE2ETests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Tests.E2E.Infrastructure;
using Sqlx.Tests.E2E.Models;
using System.Data.Common;

namespace Sqlx.Tests.E2E.SqlBuilder;

/// <summary>
/// E2E tests for SqlBuilder operations across all supported databases.
/// </summary>
[TestClass]
public class SqlBuilderE2ETests : E2ETestBase
{
    /// <summary>
    /// Creates the test_users table schema for the specified database type.
    /// </summary>
    private static string GetTestUsersSchema(DatabaseType dbType)
    {
        return dbType switch
        {
            DatabaseType.MySQL => @"
                CREATE TABLE test_users (
                    id BIGINT AUTO_INCREMENT PRIMARY KEY,
                    name VARCHAR(255) NOT NULL,
                    age INT NOT NULL,
                    email VARCHAR(255) NOT NULL,
                    created_at DATETIME NOT NULL,
                    is_active TINYINT(1) NOT NULL
                )",
            DatabaseType.PostgreSQL => @"
                CREATE TABLE test_users (
                    id BIGSERIAL PRIMARY KEY,
                    name VARCHAR(255) NOT NULL,
                    age INT NOT NULL,
                    email VARCHAR(255) NOT NULL,
                    created_at TIMESTAMP NOT NULL,
                    is_active BOOLEAN NOT NULL
                )",
            DatabaseType.SqlServer => @"
                CREATE TABLE test_users (
                    id BIGINT IDENTITY(1,1) PRIMARY KEY,
                    name NVARCHAR(255) NOT NULL,
                    age INT NOT NULL,
                    email NVARCHAR(255) NOT NULL,
                    created_at DATETIME2 NOT NULL,
                    is_active BIT NOT NULL
                )",
            DatabaseType.SQLite => @"
                CREATE TABLE test_users (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL,
                    age INTEGER NOT NULL,
                    email TEXT NOT NULL,
                    created_at TEXT NOT NULL,
                    is_active INTEGER NOT NULL
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

    private static async Task<long> InsertUserAsync(DbConnection connection, E2ETestUser user, DatabaseType dbType)
    {
        using var cmd = connection.CreateCommand();

        if (dbType == DatabaseType.PostgreSQL)
        {
            cmd.CommandText = @"
                INSERT INTO test_users (name, age, email, created_at, is_active)
                VALUES (@name, @age, @email, @created_at, @is_active)
                RETURNING id";
        }
        else if (dbType == DatabaseType.SqlServer)
        {
            cmd.CommandText = @"
                INSERT INTO test_users (name, age, email, created_at, is_active)
                VALUES (@name, @age, @email, @created_at, @is_active);
                SELECT CAST(SCOPE_IDENTITY() AS BIGINT)";
        }
        else
        {
            cmd.CommandText = @"
                INSERT INTO test_users (name, age, email, created_at, is_active)
                VALUES (@name, @age, @email, @created_at, @is_active)";
        }

        AddParameter(cmd, "@name", user.Name);
        AddParameter(cmd, "@age", user.Age);
        AddParameter(cmd, "@email", user.Email);
        AddParameter(cmd, "@created_at", FormatDateTime(user.CreatedAt, dbType));
        AddParameter(cmd, "@is_active", FormatBoolean(user.IsActive, dbType));

        if (dbType == DatabaseType.PostgreSQL || dbType == DatabaseType.SqlServer)
        {
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt64(result);
        }
        else
        {
            await cmd.ExecuteNonQueryAsync();

            using var idCmd = connection.CreateCommand();
            idCmd.CommandText = dbType == DatabaseType.MySQL
                ? "SELECT LAST_INSERT_ID()"
                : "SELECT last_insert_rowid()";

            var idResult = await idCmd.ExecuteScalarAsync();
            return Convert.ToInt64(idResult);
        }
    }

    private static void AddParameter(DbCommand cmd, string name, object? value)
    {
        var param = cmd.CreateParameter();
        param.ParameterName = name;
        param.Value = value ?? DBNull.Value;
        cmd.Parameters.Add(param);
    }

    private static object FormatDateTime(DateTime dt, DatabaseType dbType)
    {
        return dbType == DatabaseType.SQLite
            ? dt.ToString("yyyy-MM-dd HH:mm:ss")
            : dt;
    }

    private static object FormatBoolean(bool value, DatabaseType dbType)
    {
        return dbType switch
        {
            DatabaseType.MySQL => value ? 1 : 0,
            DatabaseType.SQLite => value ? 1 : 0,
            _ => value,
        };
    }

    private static bool ParseBoolean(object value, DatabaseType dbType)
    {
        return dbType switch
        {
            DatabaseType.MySQL => Convert.ToInt32(value) != 0,
            DatabaseType.SQLite => Convert.ToInt32(value) != 0,
            _ => Convert.ToBoolean(value),
        };
    }

    private static DateTime ParseDateTime(object value, DatabaseType dbType)
    {
        return dbType == DatabaseType.SQLite
            ? DateTime.Parse(value.ToString()!)
            : Convert.ToDateTime(value);
    }

    private E2ETestUser GenerateRandomUser()
    {
        return new E2ETestUser
        {
            Name = DataGenerator.GenerateString(5, 50),
            Age = DataGenerator.GenerateInt(18, 100),
            Email = $"{DataGenerator.GenerateString(5, 20)}@example.com",
            CreatedAt = DataGenerator.GenerateDateTime(DateTime.Now.AddYears(-5), DateTime.Now),
            IsActive = DataGenerator.GenerateBool(),
        };
    }

    // ==================== MySQL SqlBuilder Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlBuilder")]
    [TestCategory("MySQL")]
    public async Task MySQL_SqlBuilder_WhereClause_FiltersCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.MySQL));
        
        var user1 = GenerateRandomUser();
        user1.Age = 25;
        var user2 = GenerateRandomUser();
        user2.Age = 35;
        
        await InsertUserAsync(fixture.Connection, user1, DatabaseType.MySQL);
        await InsertUserAsync(fixture.Connection, user2, DatabaseType.MySQL);

        // Act - Build SELECT query with WHERE clause
        using var builder = new Sqlx.SqlBuilder(GetDialect(DatabaseType.MySQL));
        builder.Append($"SELECT id, name, age FROM test_users WHERE age >= {30}");
        var template = builder.Build();

        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = template.Sql;
        foreach (var param in template.Parameters)
        {
            AddParameter(cmd, param.Key, param.Value);
        }

        var results = new List<(long Id, string Name, int Age)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetInt64(0), reader.GetString(1), reader.GetInt32(2)));
        }

        // Assert
        Assert.AreEqual(1, results.Count);
        Assert.AreEqual(35, results[0].Age);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlBuilder")]
    [TestCategory("MySQL")]
    public async Task MySQL_SqlBuilder_Insert_CreatesRecord()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.MySQL));
        var user = GenerateRandomUser();

        // Act - Build INSERT query
        using var builder = new Sqlx.SqlBuilder(GetDialect(DatabaseType.MySQL));
        builder.Append($"INSERT INTO test_users (name, age, email, created_at, is_active) VALUES ({user.Name}, {user.Age}, {user.Email}, {FormatDateTime(user.CreatedAt, DatabaseType.MySQL)}, {FormatBoolean(user.IsActive, DatabaseType.MySQL)})");
        var template = builder.Build();

        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = template.Sql;
        foreach (var param in template.Parameters)
        {
            AddParameter(cmd, param.Key, param.Value);
        }
        await cmd.ExecuteNonQueryAsync();

        // Assert - Verify record was created
        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT COUNT(*) FROM test_users WHERE name = @name";
        AddParameter(selectCmd, "@name", user.Name);
        var count = Convert.ToInt64(await selectCmd.ExecuteScalarAsync());
        Assert.AreEqual(1, count);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlBuilder")]
    [TestCategory("MySQL")]
    public async Task MySQL_SqlBuilder_Update_ModifiesRecords()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.MySQL));
        var user = GenerateRandomUser();
        var insertedId = await InsertUserAsync(fixture.Connection, user, DatabaseType.MySQL);

        // Act - Build UPDATE query
        using var builder = new Sqlx.SqlBuilder(GetDialect(DatabaseType.MySQL));
        builder.Append($"UPDATE test_users SET age = {99} WHERE id = {insertedId}");
        var template = builder.Build();

        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = template.Sql;
        foreach (var param in template.Parameters)
        {
            AddParameter(cmd, param.Key, param.Value);
        }
        var affectedRows = await cmd.ExecuteNonQueryAsync();

        // Assert
        Assert.AreEqual(1, affectedRows);
        
        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT age FROM test_users WHERE id = @id";
        AddParameter(selectCmd, "@id", insertedId);
        var age = Convert.ToInt32(await selectCmd.ExecuteScalarAsync());
        Assert.AreEqual(99, age);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlBuilder")]
    [TestCategory("MySQL")]
    public async Task MySQL_SqlBuilder_Delete_RemovesRecords()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.MySQL));
        var user = GenerateRandomUser();
        var insertedId = await InsertUserAsync(fixture.Connection, user, DatabaseType.MySQL);

        // Act - Build DELETE query
        using var builder = new Sqlx.SqlBuilder(GetDialect(DatabaseType.MySQL));
        builder.Append($"DELETE FROM test_users WHERE id = {insertedId}");
        var template = builder.Build();

        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = template.Sql;
        foreach (var param in template.Parameters)
        {
            AddParameter(cmd, param.Key, param.Value);
        }
        var affectedRows = await cmd.ExecuteNonQueryAsync();

        // Assert
        Assert.AreEqual(1, affectedRows);
        
        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT COUNT(*) FROM test_users WHERE id = @id";
        AddParameter(selectCmd, "@id", insertedId);
        var count = Convert.ToInt64(await selectCmd.ExecuteScalarAsync());
        Assert.AreEqual(0, count);
    }

    // ==================== PostgreSQL SqlBuilder Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlBuilder")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_SqlBuilder_WhereClause_FiltersCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.PostgreSQL));
        
        var user1 = GenerateRandomUser();
        user1.Age = 25;
        var user2 = GenerateRandomUser();
        user2.Age = 35;
        
        await InsertUserAsync(fixture.Connection, user1, DatabaseType.PostgreSQL);
        await InsertUserAsync(fixture.Connection, user2, DatabaseType.PostgreSQL);

        // Act
        using var builder = new Sqlx.SqlBuilder(GetDialect(DatabaseType.PostgreSQL));
        builder.Append($"SELECT id, name, age FROM test_users WHERE age >= {30}");
        var template = builder.Build();

        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = template.Sql;
        foreach (var param in template.Parameters)
        {
            AddParameter(cmd, param.Key, param.Value);
        }

        var results = new List<(long Id, string Name, int Age)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetInt64(0), reader.GetString(1), reader.GetInt32(2)));
        }

        // Assert
        Assert.AreEqual(1, results.Count);
        Assert.AreEqual(35, results[0].Age);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlBuilder")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_SqlBuilder_Update_ModifiesRecords()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.PostgreSQL));
        var user = GenerateRandomUser();
        var insertedId = await InsertUserAsync(fixture.Connection, user, DatabaseType.PostgreSQL);

        // Act
        using var builder = new Sqlx.SqlBuilder(GetDialect(DatabaseType.PostgreSQL));
        builder.Append($"UPDATE test_users SET age = {99} WHERE id = {insertedId}");
        var template = builder.Build();

        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = template.Sql;
        foreach (var param in template.Parameters)
        {
            AddParameter(cmd, param.Key, param.Value);
        }
        var affectedRows = await cmd.ExecuteNonQueryAsync();

        // Assert
        Assert.AreEqual(1, affectedRows);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlBuilder")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_SqlBuilder_Insert_CreatesRecord()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.PostgreSQL));
        var user = GenerateRandomUser();

        // Act
        using var builder = new Sqlx.SqlBuilder(GetDialect(DatabaseType.PostgreSQL));
        builder.Append($"INSERT INTO test_users (name, age, email, created_at, is_active) VALUES ({user.Name}, {user.Age}, {user.Email}, {FormatDateTime(user.CreatedAt, DatabaseType.PostgreSQL)}, {FormatBoolean(user.IsActive, DatabaseType.PostgreSQL)})");
        var template = builder.Build();

        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = template.Sql;
        foreach (var param in template.Parameters)
        {
            AddParameter(cmd, param.Key, param.Value);
        }
        await cmd.ExecuteNonQueryAsync();

        // Assert
        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT COUNT(*) FROM test_users WHERE name = @name";
        AddParameter(selectCmd, "@name", user.Name);
        var count = Convert.ToInt64(await selectCmd.ExecuteScalarAsync());
        Assert.AreEqual(1, count);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlBuilder")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_SqlBuilder_Delete_RemovesRecords()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.PostgreSQL));
        var user = GenerateRandomUser();
        var insertedId = await InsertUserAsync(fixture.Connection, user, DatabaseType.PostgreSQL);

        // Act
        using var builder = new Sqlx.SqlBuilder(GetDialect(DatabaseType.PostgreSQL));
        builder.Append($"DELETE FROM test_users WHERE id = {insertedId}");
        var template = builder.Build();

        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = template.Sql;
        foreach (var param in template.Parameters)
        {
            AddParameter(cmd, param.Key, param.Value);
        }
        var affectedRows = await cmd.ExecuteNonQueryAsync();

        // Assert
        Assert.AreEqual(1, affectedRows);
        
        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT COUNT(*) FROM test_users WHERE id = @id";
        AddParameter(selectCmd, "@id", insertedId);
        var count = Convert.ToInt64(await selectCmd.ExecuteScalarAsync());
        Assert.AreEqual(0, count);
    }

    // ==================== SQL Server SqlBuilder Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlBuilder")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_SqlBuilder_WhereClause_FiltersCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.SqlServer));
        
        var user1 = GenerateRandomUser();
        user1.Age = 25;
        var user2 = GenerateRandomUser();
        user2.Age = 35;
        
        await InsertUserAsync(fixture.Connection, user1, DatabaseType.SqlServer);
        await InsertUserAsync(fixture.Connection, user2, DatabaseType.SqlServer);

        // Act
        using var builder = new Sqlx.SqlBuilder(GetDialect(DatabaseType.SqlServer));
        builder.Append($"SELECT id, name, age FROM test_users WHERE age >= {30}");
        var template = builder.Build();

        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = template.Sql;
        foreach (var param in template.Parameters)
        {
            AddParameter(cmd, param.Key, param.Value);
        }

        var results = new List<(long Id, string Name, int Age)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetInt64(0), reader.GetString(1), reader.GetInt32(2)));
        }

        // Assert
        Assert.AreEqual(1, results.Count);
        Assert.AreEqual(35, results[0].Age);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlBuilder")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_SqlBuilder_Delete_RemovesRecords()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.SqlServer));
        var user = GenerateRandomUser();
        var insertedId = await InsertUserAsync(fixture.Connection, user, DatabaseType.SqlServer);

        // Act
        using var builder = new Sqlx.SqlBuilder(GetDialect(DatabaseType.SqlServer));
        builder.Append($"DELETE FROM test_users WHERE id = {insertedId}");
        var template = builder.Build();

        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = template.Sql;
        foreach (var param in template.Parameters)
        {
            AddParameter(cmd, param.Key, param.Value);
        }
        var affectedRows = await cmd.ExecuteNonQueryAsync();

        // Assert
        Assert.AreEqual(1, affectedRows);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlBuilder")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_SqlBuilder_Insert_CreatesRecord()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.SqlServer));
        var user = GenerateRandomUser();

        // Act
        using var builder = new Sqlx.SqlBuilder(GetDialect(DatabaseType.SqlServer));
        builder.Append($"INSERT INTO test_users (name, age, email, created_at, is_active) VALUES ({user.Name}, {user.Age}, {user.Email}, {FormatDateTime(user.CreatedAt, DatabaseType.SqlServer)}, {FormatBoolean(user.IsActive, DatabaseType.SqlServer)})");
        var template = builder.Build();

        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = template.Sql;
        foreach (var param in template.Parameters)
        {
            AddParameter(cmd, param.Key, param.Value);
        }
        await cmd.ExecuteNonQueryAsync();

        // Assert
        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT COUNT(*) FROM test_users WHERE name = @name";
        AddParameter(selectCmd, "@name", user.Name);
        var count = Convert.ToInt64(await selectCmd.ExecuteScalarAsync());
        Assert.AreEqual(1, count);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlBuilder")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_SqlBuilder_Update_ModifiesRecords()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.SqlServer));
        var user = GenerateRandomUser();
        var insertedId = await InsertUserAsync(fixture.Connection, user, DatabaseType.SqlServer);

        // Act
        using var builder = new Sqlx.SqlBuilder(GetDialect(DatabaseType.SqlServer));
        builder.Append($"UPDATE test_users SET age = {99} WHERE id = {insertedId}");
        var template = builder.Build();

        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = template.Sql;
        foreach (var param in template.Parameters)
        {
            AddParameter(cmd, param.Key, param.Value);
        }
        var affectedRows = await cmd.ExecuteNonQueryAsync();

        // Assert
        Assert.AreEqual(1, affectedRows);
        
        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT age FROM test_users WHERE id = @id";
        AddParameter(selectCmd, "@id", insertedId);
        var age = Convert.ToInt32(await selectCmd.ExecuteScalarAsync());
        Assert.AreEqual(99, age);
    }

    // ==================== SQLite SqlBuilder Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlBuilder")]
    [TestCategory("SQLite")]
    public async Task SQLite_SqlBuilder_WhereClause_FiltersCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.SQLite));
        
        var user1 = GenerateRandomUser();
        user1.Age = 25;
        var user2 = GenerateRandomUser();
        user2.Age = 35;
        
        await InsertUserAsync(fixture.Connection, user1, DatabaseType.SQLite);
        await InsertUserAsync(fixture.Connection, user2, DatabaseType.SQLite);

        // Act
        using var builder = new Sqlx.SqlBuilder(GetDialect(DatabaseType.SQLite));
        builder.Append($"SELECT id, name, age FROM test_users WHERE age >= {30}");
        var template = builder.Build();

        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = template.Sql;
        foreach (var param in template.Parameters)
        {
            AddParameter(cmd, param.Key, param.Value);
        }

        var results = new List<(long Id, string Name, int Age)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetInt64(0), reader.GetString(1), reader.GetInt32(2)));
        }

        // Assert
        Assert.AreEqual(1, results.Count);
        Assert.AreEqual(35, results[0].Age);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlBuilder")]
    [TestCategory("SQLite")]
    public async Task SQLite_SqlBuilder_Insert_CreatesRecord()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.SQLite));
        var user = GenerateRandomUser();

        // Act
        using var builder = new Sqlx.SqlBuilder(GetDialect(DatabaseType.SQLite));
        builder.Append($"INSERT INTO test_users (name, age, email, created_at, is_active) VALUES ({user.Name}, {user.Age}, {user.Email}, {FormatDateTime(user.CreatedAt, DatabaseType.SQLite)}, {FormatBoolean(user.IsActive, DatabaseType.SQLite)})");
        var template = builder.Build();

        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = template.Sql;
        foreach (var param in template.Parameters)
        {
            AddParameter(cmd, param.Key, param.Value);
        }
        await cmd.ExecuteNonQueryAsync();

        // Assert
        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT COUNT(*) FROM test_users WHERE name = @name";
        AddParameter(selectCmd, "@name", user.Name);
        var count = Convert.ToInt64(await selectCmd.ExecuteScalarAsync());
        Assert.AreEqual(1, count);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlBuilder")]
    [TestCategory("SQLite")]
    public async Task SQLite_SqlBuilder_Update_ModifiesRecords()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.SQLite));
        var user = GenerateRandomUser();
        var insertedId = await InsertUserAsync(fixture.Connection, user, DatabaseType.SQLite);

        // Act
        using var builder = new Sqlx.SqlBuilder(GetDialect(DatabaseType.SQLite));
        builder.Append($"UPDATE test_users SET age = {99} WHERE id = {insertedId}");
        var template = builder.Build();

        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = template.Sql;
        foreach (var param in template.Parameters)
        {
            AddParameter(cmd, param.Key, param.Value);
        }
        var affectedRows = await cmd.ExecuteNonQueryAsync();

        // Assert
        Assert.AreEqual(1, affectedRows);
        
        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT age FROM test_users WHERE id = @id";
        AddParameter(selectCmd, "@id", insertedId);
        var age = Convert.ToInt32(await selectCmd.ExecuteScalarAsync());
        Assert.AreEqual(99, age);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlBuilder")]
    [TestCategory("SQLite")]
    public async Task SQLite_SqlBuilder_Delete_RemovesRecords()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.SQLite));
        var user = GenerateRandomUser();
        var insertedId = await InsertUserAsync(fixture.Connection, user, DatabaseType.SQLite);

        // Act
        using var builder = new Sqlx.SqlBuilder(GetDialect(DatabaseType.SQLite));
        builder.Append($"DELETE FROM test_users WHERE id = {insertedId}");
        var template = builder.Build();

        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = template.Sql;
        foreach (var param in template.Parameters)
        {
            AddParameter(cmd, param.Key, param.Value);
        }
        var affectedRows = await cmd.ExecuteNonQueryAsync();

        // Assert
        Assert.AreEqual(1, affectedRows);
        
        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT COUNT(*) FROM test_users WHERE id = @id";
        AddParameter(selectCmd, "@id", insertedId);
        var count = Convert.ToInt64(await selectCmd.ExecuteScalarAsync());
        Assert.AreEqual(0, count);
    }

    // ==================== Additional SqlBuilder Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlBuilder")]
    [TestCategory("MySQL")]
    public async Task MySQL_SqlBuilder_MultipleConditions_FiltersCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.MySQL));
        
        var user1 = GenerateRandomUser();
        user1.Age = 25;
        user1.IsActive = true;
        var user2 = GenerateRandomUser();
        user2.Age = 35;
        user2.IsActive = true;
        var user3 = GenerateRandomUser();
        user3.Age = 35;
        user3.IsActive = false;
        
        await InsertUserAsync(fixture.Connection, user1, DatabaseType.MySQL);
        await InsertUserAsync(fixture.Connection, user2, DatabaseType.MySQL);
        await InsertUserAsync(fixture.Connection, user3, DatabaseType.MySQL);

        // Act - Build SELECT query with multiple WHERE conditions
        using var builder = new Sqlx.SqlBuilder(GetDialect(DatabaseType.MySQL));
        builder.Append($"SELECT id, name, age FROM test_users WHERE age >= {30} AND is_active = {FormatBoolean(true, DatabaseType.MySQL)}");
        var template = builder.Build();

        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = template.Sql;
        foreach (var param in template.Parameters)
        {
            AddParameter(cmd, param.Key, param.Value);
        }

        var results = new List<(long Id, string Name, int Age)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetInt64(0), reader.GetString(1), reader.GetInt32(2)));
        }

        // Assert
        Assert.AreEqual(1, results.Count);
        Assert.AreEqual(35, results[0].Age);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlBuilder")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_SqlBuilder_MultipleConditions_FiltersCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.PostgreSQL));
        
        var user1 = GenerateRandomUser();
        user1.Age = 25;
        user1.IsActive = true;
        var user2 = GenerateRandomUser();
        user2.Age = 35;
        user2.IsActive = true;
        var user3 = GenerateRandomUser();
        user3.Age = 35;
        user3.IsActive = false;
        
        await InsertUserAsync(fixture.Connection, user1, DatabaseType.PostgreSQL);
        await InsertUserAsync(fixture.Connection, user2, DatabaseType.PostgreSQL);
        await InsertUserAsync(fixture.Connection, user3, DatabaseType.PostgreSQL);

        // Act
        using var builder = new Sqlx.SqlBuilder(GetDialect(DatabaseType.PostgreSQL));
        builder.Append($"SELECT id, name, age FROM test_users WHERE age >= {30} AND is_active = {FormatBoolean(true, DatabaseType.PostgreSQL)}");
        var template = builder.Build();

        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = template.Sql;
        foreach (var param in template.Parameters)
        {
            AddParameter(cmd, param.Key, param.Value);
        }

        var results = new List<(long Id, string Name, int Age)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetInt64(0), reader.GetString(1), reader.GetInt32(2)));
        }

        // Assert
        Assert.AreEqual(1, results.Count);
        Assert.AreEqual(35, results[0].Age);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlBuilder")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_SqlBuilder_MultipleConditions_FiltersCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.SqlServer));
        
        var user1 = GenerateRandomUser();
        user1.Age = 25;
        user1.IsActive = true;
        var user2 = GenerateRandomUser();
        user2.Age = 35;
        user2.IsActive = true;
        var user3 = GenerateRandomUser();
        user3.Age = 35;
        user3.IsActive = false;
        
        await InsertUserAsync(fixture.Connection, user1, DatabaseType.SqlServer);
        await InsertUserAsync(fixture.Connection, user2, DatabaseType.SqlServer);
        await InsertUserAsync(fixture.Connection, user3, DatabaseType.SqlServer);

        // Act
        using var builder = new Sqlx.SqlBuilder(GetDialect(DatabaseType.SqlServer));
        builder.Append($"SELECT id, name, age FROM test_users WHERE age >= {30} AND is_active = {FormatBoolean(true, DatabaseType.SqlServer)}");
        var template = builder.Build();

        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = template.Sql;
        foreach (var param in template.Parameters)
        {
            AddParameter(cmd, param.Key, param.Value);
        }

        var results = new List<(long Id, string Name, int Age)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetInt64(0), reader.GetString(1), reader.GetInt32(2)));
        }

        // Assert
        Assert.AreEqual(1, results.Count);
        Assert.AreEqual(35, results[0].Age);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlBuilder")]
    [TestCategory("SQLite")]
    public async Task SQLite_SqlBuilder_MultipleConditions_FiltersCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.SQLite));
        
        var user1 = GenerateRandomUser();
        user1.Age = 25;
        user1.IsActive = true;
        var user2 = GenerateRandomUser();
        user2.Age = 35;
        user2.IsActive = true;
        var user3 = GenerateRandomUser();
        user3.Age = 35;
        user3.IsActive = false;
        
        await InsertUserAsync(fixture.Connection, user1, DatabaseType.SQLite);
        await InsertUserAsync(fixture.Connection, user2, DatabaseType.SQLite);
        await InsertUserAsync(fixture.Connection, user3, DatabaseType.SQLite);

        // Act
        using var builder = new Sqlx.SqlBuilder(GetDialect(DatabaseType.SQLite));
        builder.Append($"SELECT id, name, age FROM test_users WHERE age >= {30} AND is_active = {FormatBoolean(true, DatabaseType.SQLite)}");
        var template = builder.Build();

        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = template.Sql;
        foreach (var param in template.Parameters)
        {
            AddParameter(cmd, param.Key, param.Value);
        }

        var results = new List<(long Id, string Name, int Age)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetInt64(0), reader.GetString(1), reader.GetInt32(2)));
        }

        // Assert
        Assert.AreEqual(1, results.Count);
        Assert.AreEqual(35, results[0].Age);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlBuilder")]
    [TestCategory("MySQL")]
    public async Task MySQL_SqlBuilder_OrderBy_SortsCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.MySQL));
        
        var user1 = GenerateRandomUser();
        user1.Age = 30;
        var user2 = GenerateRandomUser();
        user2.Age = 20;
        var user3 = GenerateRandomUser();
        user3.Age = 25;
        
        await InsertUserAsync(fixture.Connection, user1, DatabaseType.MySQL);
        await InsertUserAsync(fixture.Connection, user2, DatabaseType.MySQL);
        await InsertUserAsync(fixture.Connection, user3, DatabaseType.MySQL);

        // Act
        using var builder = new Sqlx.SqlBuilder(GetDialect(DatabaseType.MySQL));
        builder.Append($"SELECT age FROM test_users ORDER BY age ASC");
        var template = builder.Build();

        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = template.Sql;

        var ages = new List<int>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            ages.Add(reader.GetInt32(0));
        }

        // Assert
        Assert.AreEqual(3, ages.Count);
        Assert.AreEqual(20, ages[0]);
        Assert.AreEqual(25, ages[1]);
        Assert.AreEqual(30, ages[2]);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlBuilder")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_SqlBuilder_OrderBy_SortsCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.PostgreSQL));
        
        var user1 = GenerateRandomUser();
        user1.Age = 30;
        var user2 = GenerateRandomUser();
        user2.Age = 20;
        var user3 = GenerateRandomUser();
        user3.Age = 25;
        
        await InsertUserAsync(fixture.Connection, user1, DatabaseType.PostgreSQL);
        await InsertUserAsync(fixture.Connection, user2, DatabaseType.PostgreSQL);
        await InsertUserAsync(fixture.Connection, user3, DatabaseType.PostgreSQL);

        // Act
        using var builder = new Sqlx.SqlBuilder(GetDialect(DatabaseType.PostgreSQL));
        builder.Append($"SELECT age FROM test_users ORDER BY age ASC");
        var template = builder.Build();

        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = template.Sql;

        var ages = new List<int>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            ages.Add(reader.GetInt32(0));
        }

        // Assert
        Assert.AreEqual(3, ages.Count);
        Assert.AreEqual(20, ages[0]);
        Assert.AreEqual(25, ages[1]);
        Assert.AreEqual(30, ages[2]);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlBuilder")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_SqlBuilder_OrderBy_SortsCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.SqlServer));
        
        var user1 = GenerateRandomUser();
        user1.Age = 30;
        var user2 = GenerateRandomUser();
        user2.Age = 20;
        var user3 = GenerateRandomUser();
        user3.Age = 25;
        
        await InsertUserAsync(fixture.Connection, user1, DatabaseType.SqlServer);
        await InsertUserAsync(fixture.Connection, user2, DatabaseType.SqlServer);
        await InsertUserAsync(fixture.Connection, user3, DatabaseType.SqlServer);

        // Act
        using var builder = new Sqlx.SqlBuilder(GetDialect(DatabaseType.SqlServer));
        builder.Append($"SELECT age FROM test_users ORDER BY age ASC");
        var template = builder.Build();

        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = template.Sql;

        var ages = new List<int>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            ages.Add(reader.GetInt32(0));
        }

        // Assert
        Assert.AreEqual(3, ages.Count);
        Assert.AreEqual(20, ages[0]);
        Assert.AreEqual(25, ages[1]);
        Assert.AreEqual(30, ages[2]);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlBuilder")]
    [TestCategory("SQLite")]
    public async Task SQLite_SqlBuilder_OrderBy_SortsCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetTestUsersSchema(DatabaseType.SQLite));
        
        var user1 = GenerateRandomUser();
        user1.Age = 30;
        var user2 = GenerateRandomUser();
        user2.Age = 20;
        var user3 = GenerateRandomUser();
        user3.Age = 25;
        
        await InsertUserAsync(fixture.Connection, user1, DatabaseType.SQLite);
        await InsertUserAsync(fixture.Connection, user2, DatabaseType.SQLite);
        await InsertUserAsync(fixture.Connection, user3, DatabaseType.SQLite);

        // Act
        using var builder = new Sqlx.SqlBuilder(GetDialect(DatabaseType.SQLite));
        builder.Append($"SELECT age FROM test_users ORDER BY age ASC");
        var template = builder.Build();

        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = template.Sql;

        var ages = new List<int>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            ages.Add(reader.GetInt32(0));
        }

        // Assert
        Assert.AreEqual(3, ages.Count);
        Assert.AreEqual(20, ages[0]);
        Assert.AreEqual(25, ages[1]);
        Assert.AreEqual(30, ages[2]);
    }
}
