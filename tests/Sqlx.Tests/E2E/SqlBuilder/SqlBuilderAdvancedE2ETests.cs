// <copyright file="SqlBuilderAdvancedE2ETests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Tests.E2E.Infrastructure;
using System.Data.Common;

namespace Sqlx.Tests.E2E.SqlBuilder;

/// <summary>
/// Advanced E2E tests for SqlBuilder covering complex SQL operations across all supported databases.
/// Tests focus on SqlBuilder's expression parsing, parameterization, and SQL generation capabilities.
/// </summary>
[TestClass]
public class SqlBuilderAdvancedE2ETests : E2ETestBase
{
    // ==================== Schema Definitions ====================

    private static string GetComplexSchema(DatabaseType dbType)
    {
        return dbType switch
        {
            DatabaseType.MySQL => @"
                CREATE TABLE users (
                    id BIGINT AUTO_INCREMENT PRIMARY KEY,
                    name VARCHAR(255) NOT NULL,
                    age INT NOT NULL,
                    email VARCHAR(255) NOT NULL,
                    department VARCHAR(100),
                    salary DECIMAL(10,2),
                    created_at DATETIME NOT NULL
                );
                CREATE TABLE orders (
                    id BIGINT AUTO_INCREMENT PRIMARY KEY,
                    user_id BIGINT NOT NULL,
                    total DECIMAL(10,2) NOT NULL,
                    status VARCHAR(50) NOT NULL,
                    order_date DATETIME NOT NULL
                );
                CREATE TABLE products (
                    id BIGINT AUTO_INCREMENT PRIMARY KEY,
                    name VARCHAR(255) NOT NULL,
                    price DECIMAL(10,2) NOT NULL,
                    category VARCHAR(100)
                );",
            DatabaseType.PostgreSQL => @"
                CREATE TABLE users (
                    id BIGSERIAL PRIMARY KEY,
                    name VARCHAR(255) NOT NULL,
                    age INT NOT NULL,
                    email VARCHAR(255) NOT NULL,
                    department VARCHAR(100),
                    salary DECIMAL(10,2),
                    created_at TIMESTAMP NOT NULL
                );
                CREATE TABLE orders (
                    id BIGSERIAL PRIMARY KEY,
                    user_id BIGINT NOT NULL,
                    total DECIMAL(10,2) NOT NULL,
                    status VARCHAR(50) NOT NULL,
                    order_date TIMESTAMP NOT NULL
                );
                CREATE TABLE products (
                    id BIGSERIAL PRIMARY KEY,
                    name VARCHAR(255) NOT NULL,
                    price DECIMAL(10,2) NOT NULL,
                    category VARCHAR(100)
                );",
            DatabaseType.SqlServer => @"
                CREATE TABLE users (
                    id BIGINT IDENTITY(1,1) PRIMARY KEY,
                    name NVARCHAR(255) NOT NULL,
                    age INT NOT NULL,
                    email NVARCHAR(255) NOT NULL,
                    department NVARCHAR(100),
                    salary DECIMAL(10,2),
                    created_at DATETIME2 NOT NULL
                );
                CREATE TABLE orders (
                    id BIGINT IDENTITY(1,1) PRIMARY KEY,
                    user_id BIGINT NOT NULL,
                    total DECIMAL(10,2) NOT NULL,
                    status NVARCHAR(50) NOT NULL,
                    order_date DATETIME2 NOT NULL
                );
                CREATE TABLE products (
                    id BIGINT IDENTITY(1,1) PRIMARY KEY,
                    name NVARCHAR(255) NOT NULL,
                    price DECIMAL(10,2) NOT NULL,
                    category NVARCHAR(100)
                );",
            DatabaseType.SQLite => @"
                CREATE TABLE users (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL,
                    age INTEGER NOT NULL,
                    email TEXT NOT NULL,
                    department TEXT,
                    salary REAL,
                    created_at TEXT NOT NULL
                );
                CREATE TABLE orders (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    user_id INTEGER NOT NULL,
                    total REAL NOT NULL,
                    status TEXT NOT NULL,
                    order_date TEXT NOT NULL
                );
                CREATE TABLE products (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL,
                    price REAL NOT NULL,
                    category TEXT
                );",
            _ => throw new NotSupportedException($"Database type {dbType} is not supported"),
        };
    }

    // ==================== Helper Methods ====================

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

    private static async Task<long> InsertUserAsync(
        DbConnection connection,
        string name,
        int age,
        string email,
        string? department,
        decimal? salary,
        DateTime createdAt,
        DatabaseType dbType)
    {
        using var cmd = connection.CreateCommand();

        if (dbType == DatabaseType.PostgreSQL)
        {
            cmd.CommandText = @"
                INSERT INTO users (name, age, email, department, salary, created_at)
                VALUES (@name, @age, @email, @department, @salary, @created_at)
                RETURNING id";
        }
        else if (dbType == DatabaseType.SqlServer)
        {
            cmd.CommandText = @"
                INSERT INTO users (name, age, email, department, salary, created_at)
                VALUES (@name, @age, @email, @department, @salary, @created_at);
                SELECT CAST(SCOPE_IDENTITY() AS BIGINT)";
        }
        else
        {
            cmd.CommandText = @"
                INSERT INTO users (name, age, email, department, salary, created_at)
                VALUES (@name, @age, @email, @department, @salary, @created_at)";
        }

        AddParameter(cmd, "@name", name);
        AddParameter(cmd, "@age", age);
        AddParameter(cmd, "@email", email);
        AddParameter(cmd, "@department", department);
        AddParameter(cmd, "@salary", salary);
        AddParameter(cmd, "@created_at", FormatDateTime(createdAt, dbType));

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

    private static async Task<long> InsertOrderAsync(
        DbConnection connection,
        long userId,
        decimal total,
        string status,
        DateTime orderDate,
        DatabaseType dbType)
    {
        using var cmd = connection.CreateCommand();

        if (dbType == DatabaseType.PostgreSQL)
        {
            cmd.CommandText = @"
                INSERT INTO orders (user_id, total, status, order_date)
                VALUES (@user_id, @total, @status, @order_date)
                RETURNING id";
        }
        else if (dbType == DatabaseType.SqlServer)
        {
            cmd.CommandText = @"
                INSERT INTO orders (user_id, total, status, order_date)
                VALUES (@user_id, @total, @status, @order_date);
                SELECT CAST(SCOPE_IDENTITY() AS BIGINT)";
        }
        else
        {
            cmd.CommandText = @"
                INSERT INTO orders (user_id, total, status, order_date)
                VALUES (@user_id, @total, @status, @order_date)";
        }

        AddParameter(cmd, "@user_id", userId);
        AddParameter(cmd, "@total", total);
        AddParameter(cmd, "@status", status);
        AddParameter(cmd, "@order_date", FormatDateTime(orderDate, dbType));

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

    private static async Task<long> InsertProductAsync(
        DbConnection connection,
        string name,
        decimal price,
        string? category,
        DatabaseType dbType)
    {
        using var cmd = connection.CreateCommand();

        if (dbType == DatabaseType.PostgreSQL)
        {
            cmd.CommandText = @"
                INSERT INTO products (name, price, category)
                VALUES (@name, @price, @category)
                RETURNING id";
        }
        else if (dbType == DatabaseType.SqlServer)
        {
            cmd.CommandText = @"
                INSERT INTO products (name, price, category)
                VALUES (@name, @price, @category);
                SELECT CAST(SCOPE_IDENTITY() AS BIGINT)";
        }
        else
        {
            cmd.CommandText = @"
                INSERT INTO products (name, price, category)
                VALUES (@name, @price, @category)";
        }

        AddParameter(cmd, "@name", name);
        AddParameter(cmd, "@price", price);
        AddParameter(cmd, "@category", category);

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

    // ==================== JOIN Operation Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlBuilder")]
    [TestCategory("Advanced")]
    [TestCategory("MySQL")]
    public async Task MySQL_SqlBuilder_InnerJoin_TwoTables_ReturnsMatchingRows()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetComplexSchema(DatabaseType.MySQL));

        var userId1 = await InsertUserAsync(fixture.Connection, "Alice", 30, "alice@test.com", "Engineering", 75000m, DateTime.Now, DatabaseType.MySQL);
        var userId2 = await InsertUserAsync(fixture.Connection, "Bob", 25, "bob@test.com", "Sales", 60000m, DateTime.Now, DatabaseType.MySQL);
        var userId3 = await InsertUserAsync(fixture.Connection, "Charlie", 35, "charlie@test.com", "Engineering", 80000m, DateTime.Now, DatabaseType.MySQL);

        await InsertOrderAsync(fixture.Connection, userId1, 100.50m, "completed", DateTime.Now, DatabaseType.MySQL);
        await InsertOrderAsync(fixture.Connection, userId1, 200.75m, "pending", DateTime.Now, DatabaseType.MySQL);
        await InsertOrderAsync(fixture.Connection, userId2, 150.00m, "completed", DateTime.Now, DatabaseType.MySQL);
        // userId3 has no orders

        // Act - Build INNER JOIN query with SqlBuilder
        using var builder = new Sqlx.SqlBuilder(GetDialect(DatabaseType.MySQL));
        builder.Append($"SELECT u.name, o.total FROM users u INNER JOIN orders o ON u.id = o.user_id WHERE o.status = {"completed"}");
        var template = builder.Build();

        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = template.Sql;
        foreach (var param in template.Parameters)
        {
            AddParameter(cmd, param.Key, param.Value);
        }

        var results = new List<(string Name, decimal Total)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.GetDecimal(1)));
        }

        // Assert - Only users with completed orders should be returned
        Assert.AreEqual(2, results.Count, "Should return 2 rows (Alice and Bob with completed orders)");
        Assert.IsTrue(results.Any(r => r.Name == "Alice" && r.Total == 100.50m));
        Assert.IsTrue(results.Any(r => r.Name == "Bob" && r.Total == 150.00m));
        Assert.IsFalse(results.Any(r => r.Name == "Charlie"), "Charlie has no orders");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlBuilder")]
    [TestCategory("Advanced")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_SqlBuilder_LeftJoin_ReturnsAllLeftRows()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetComplexSchema(DatabaseType.PostgreSQL));

        var userId1 = await InsertUserAsync(fixture.Connection, "Alice", 30, "alice@test.com", "Engineering", 75000m, DateTime.Now, DatabaseType.PostgreSQL);
        var userId2 = await InsertUserAsync(fixture.Connection, "Bob", 25, "bob@test.com", "Sales", 60000m, DateTime.Now, DatabaseType.PostgreSQL);
        var userId3 = await InsertUserAsync(fixture.Connection, "Charlie", 35, "charlie@test.com", "Engineering", 80000m, DateTime.Now, DatabaseType.PostgreSQL);

        await InsertOrderAsync(fixture.Connection, userId1, 100.50m, "completed", DateTime.Now, DatabaseType.PostgreSQL);
        await InsertOrderAsync(fixture.Connection, userId2, 150.00m, "completed", DateTime.Now, DatabaseType.PostgreSQL);
        // userId3 has no orders

        // Act - Build LEFT JOIN query with SqlBuilder
        using var builder = new Sqlx.SqlBuilder(GetDialect(DatabaseType.PostgreSQL));
        builder.Append($"SELECT u.name, o.total FROM users u LEFT JOIN orders o ON u.id = o.user_id ORDER BY u.name");
        var template = builder.Build();

        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = template.Sql;
        foreach (var param in template.Parameters)
        {
            AddParameter(cmd, param.Key, param.Value);
        }

        var results = new List<(string Name, decimal? Total)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var total = reader.IsDBNull(1) ? (decimal?)null : reader.GetDecimal(1);
            results.Add((reader.GetString(0), total));
        }

        // Assert - All users should be returned, Charlie with NULL total
        Assert.AreEqual(3, results.Count, "Should return all 3 users");
        Assert.IsTrue(results.Any(r => r.Name == "Alice" && r.Total == 100.50m));
        Assert.IsTrue(results.Any(r => r.Name == "Bob" && r.Total == 150.00m));
        Assert.IsTrue(results.Any(r => r.Name == "Charlie" && r.Total == null), "Charlie should have NULL total");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlBuilder")]
    [TestCategory("Advanced")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_SqlBuilder_MultipleJoins_CombinesThreeTables()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetComplexSchema(DatabaseType.SqlServer));

        var userId = await InsertUserAsync(fixture.Connection, "Alice", 30, "alice@test.com", "Engineering", 75000m, DateTime.Now, DatabaseType.SqlServer);
        var orderId = await InsertOrderAsync(fixture.Connection, userId, 100.50m, "completed", DateTime.Now, DatabaseType.SqlServer);
        var productId = await InsertProductAsync(fixture.Connection, "Laptop", 999.99m, "Electronics", DatabaseType.SqlServer);

        // Act - Build query with multiple JOINs
        using var builder = new Sqlx.SqlBuilder(GetDialect(DatabaseType.SqlServer));
        builder.Append($@"
            SELECT u.name, o.total, p.name as product_name, p.price
            FROM users u
            INNER JOIN orders o ON u.id = o.user_id
            CROSS JOIN products p
            WHERE u.age >= {25} AND p.category = {"Electronics"}
        ");
        var template = builder.Build();

        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = template.Sql;
        foreach (var param in template.Parameters)
        {
            AddParameter(cmd, param.Key, param.Value);
        }

        var results = new List<(string UserName, decimal OrderTotal, string ProductName, decimal ProductPrice)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((
                reader.GetString(0),
                reader.GetDecimal(1),
                reader.GetString(2),
                reader.GetDecimal(3)
            ));
        }

        // Assert - Should combine data from all three tables
        Assert.AreEqual(1, results.Count);
        Assert.AreEqual("Alice", results[0].UserName);
        Assert.AreEqual(100.50m, results[0].OrderTotal);
        Assert.AreEqual("Laptop", results[0].ProductName);
        Assert.AreEqual(999.99m, results[0].ProductPrice);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlBuilder")]
    [TestCategory("Advanced")]
    [TestCategory("SQLite")]
    public async Task SQLite_SqlBuilder_JoinWithParameters_ParameterizesCorrectly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetComplexSchema(DatabaseType.SQLite));

        var userId1 = await InsertUserAsync(fixture.Connection, "Alice", 30, "alice@test.com", "Engineering", 75000m, DateTime.Now, DatabaseType.SQLite);
        var userId2 = await InsertUserAsync(fixture.Connection, "Bob", 25, "bob@test.com", "Sales", 60000m, DateTime.Now, DatabaseType.SQLite);

        await InsertOrderAsync(fixture.Connection, userId1, 100.50m, "completed", DateTime.Now, DatabaseType.SQLite);
        await InsertOrderAsync(fixture.Connection, userId1, 50.25m, "pending", DateTime.Now, DatabaseType.SQLite);
        await InsertOrderAsync(fixture.Connection, userId2, 200.00m, "completed", DateTime.Now, DatabaseType.SQLite);

        // Act - Build JOIN query with multiple parameters
        var minTotal = 75.00m;
        var status = "completed";
        var minAge = 28;

        using var builder = new Sqlx.SqlBuilder(GetDialect(DatabaseType.SQLite));
        builder.Append($@"
            SELECT u.name, u.age, o.total, o.status
            FROM users u
            INNER JOIN orders o ON u.id = o.user_id
            WHERE o.total >= {minTotal} AND o.status = {status} AND u.age >= {minAge}
        ");
        var template = builder.Build();

        // Assert - Verify parameters are created
        Assert.AreEqual(3, template.Parameters.Count, "Should have 3 parameters");
        Assert.IsTrue(template.Parameters.Values.Contains(minTotal));
        Assert.IsTrue(template.Parameters.Values.Contains(status));
        Assert.IsTrue(template.Parameters.Values.Contains(minAge));

        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = template.Sql;
        foreach (var param in template.Parameters)
        {
            AddParameter(cmd, param.Key, param.Value);
        }

        var results = new List<(string Name, int Age, decimal Total, string Status)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((
                reader.GetString(0),
                Convert.ToInt32(reader.GetInt64(1)),
                Convert.ToDecimal(reader.GetDouble(2)),
                reader.GetString(3)
            ));
        }

        // Assert - Only Alice's completed order >= 75 should be returned
        Assert.AreEqual(1, results.Count);
        Assert.AreEqual("Alice", results[0].Name);
        Assert.AreEqual(30, results[0].Age);
        Assert.AreEqual(100.50m, results[0].Total);
        Assert.AreEqual("completed", results[0].Status);
    }

    // ==================== SQL Injection Prevention Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlBuilder")]
    [TestCategory("Advanced")]
    [TestCategory("MySQL")]
    public async Task MySQL_SqlBuilder_SqlInjection_ParameterizesAsLiteral()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetComplexSchema(DatabaseType.MySQL));

        var userId = await InsertUserAsync(fixture.Connection, "Alice", 30, "alice@test.com", "Engineering", 75000m, DateTime.Now, DatabaseType.MySQL);
        await InsertUserAsync(fixture.Connection, "Bob", 25, "bob@test.com", "Sales", 60000m, DateTime.Now, DatabaseType.MySQL);

        // Act - Try SQL injection with malicious input
        var maliciousInput = "Alice' OR '1'='1";
        
        using var builder = new Sqlx.SqlBuilder(GetDialect(DatabaseType.MySQL));
        builder.Append($"SELECT name, age FROM users WHERE name = {maliciousInput}");
        var template = builder.Build();

        // Assert - Verify input is parameterized
        Assert.AreEqual(1, template.Parameters.Count, "Should have 1 parameter");
        Assert.IsTrue(template.Parameters.Values.Contains(maliciousInput), "Malicious input should be parameterized as literal");

        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = template.Sql;
        foreach (var param in template.Parameters)
        {
            AddParameter(cmd, param.Key, param.Value);
        }

        var results = new List<(string Name, int Age)>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add((reader.GetString(0), reader.GetInt32(1)));
        }

        // Assert - Should return 0 rows (no user with that exact name)
        Assert.AreEqual(0, results.Count, "SQL injection should be prevented, no rows returned");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlBuilder")]
    [TestCategory("Advanced")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_SqlBuilder_UnionInjection_ParameterizesAsLiteral()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetComplexSchema(DatabaseType.PostgreSQL));

        await InsertUserAsync(fixture.Connection, "Alice", 30, "alice@test.com", "Engineering", 75000m, DateTime.Now, DatabaseType.PostgreSQL);

        // Act - Try UNION-based SQL injection
        var maliciousInput = "Alice' UNION SELECT name, age FROM users WHERE '1'='1";
        
        using var builder = new Sqlx.SqlBuilder(GetDialect(DatabaseType.PostgreSQL));
        builder.Append($"SELECT name, age FROM users WHERE name = {maliciousInput}");
        var template = builder.Build();

        // Assert - Verify UNION keyword is parameterized as literal
        Assert.IsTrue(template.Parameters.Values.Contains(maliciousInput));
        Assert.IsFalse(template.Sql.Contains("UNION"), "UNION should not appear in generated SQL");

        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = template.Sql;
        foreach (var param in template.Parameters)
        {
            AddParameter(cmd, param.Key, param.Value);
        }

        var results = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(reader.GetString(0));
        }

        // Assert - Should return 0 rows
        Assert.AreEqual(0, results.Count, "UNION injection should be prevented");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlBuilder")]
    [TestCategory("Advanced")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_SqlBuilder_CommentInjection_ParameterizesAsLiteral()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetComplexSchema(DatabaseType.SqlServer));

        await InsertUserAsync(fixture.Connection, "Alice", 30, "alice@test.com", "Engineering", 75000m, DateTime.Now, DatabaseType.SqlServer);
        await InsertUserAsync(fixture.Connection, "Bob", 25, "bob@test.com", "Sales", 60000m, DateTime.Now, DatabaseType.SqlServer);

        // Act - Try comment-based SQL injection
        var maliciousInput = "Alice' --";
        
        using var builder = new Sqlx.SqlBuilder(GetDialect(DatabaseType.SqlServer));
        builder.Append($"SELECT name FROM users WHERE name = {maliciousInput} AND age > {20}");
        var template = builder.Build();

        // Assert - Verify comment is parameterized
        Assert.AreEqual(2, template.Parameters.Count);
        Assert.IsTrue(template.Parameters.Values.Contains(maliciousInput));

        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = template.Sql;
        foreach (var param in template.Parameters)
        {
            AddParameter(cmd, param.Key, param.Value);
        }

        var results = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(reader.GetString(0));
        }

        // Assert - Should return 0 rows (comment is treated as literal)
        Assert.AreEqual(0, results.Count, "Comment injection should be prevented");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlBuilder")]
    [TestCategory("Advanced")]
    [TestCategory("SQLite")]
    public async Task SQLite_SqlBuilder_DropTableInjection_ParameterizesAsLiteral()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetComplexSchema(DatabaseType.SQLite));

        await InsertUserAsync(fixture.Connection, "Alice", 30, "alice@test.com", "Engineering", 75000m, DateTime.Now, DatabaseType.SQLite);

        // Act - Try DROP TABLE injection
        var maliciousInput = "Alice'; DROP TABLE users; --";
        
        using var builder = new Sqlx.SqlBuilder(GetDialect(DatabaseType.SQLite));
        builder.Append($"SELECT name FROM users WHERE name = {maliciousInput}");
        var template = builder.Build();

        // Assert - Verify DROP TABLE is parameterized
        Assert.IsTrue(template.Parameters.Values.Contains(maliciousInput));
        Assert.IsFalse(template.Sql.Contains("DROP"), "DROP should not appear in generated SQL");

        using var cmd = fixture.Connection.CreateCommand();
        cmd.CommandText = template.Sql;
        foreach (var param in template.Parameters)
        {
            AddParameter(cmd, param.Key, param.Value);
        }

        await cmd.ExecuteNonQueryAsync();

        // Assert - Verify table still exists
        using var checkCmd = fixture.Connection.CreateCommand();
        checkCmd.CommandText = "SELECT COUNT(*) FROM users";
        var count = Convert.ToInt64(await checkCmd.ExecuteScalarAsync());
        Assert.AreEqual(1, count, "Table should still exist with 1 user");
    }

    // ==================== Special Characters Tests ====================

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlBuilder")]
    [TestCategory("Advanced")]
    [TestCategory("MySQL")]
    public async Task MySQL_SqlBuilder_SingleQuotes_PreservesExactly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.MySQL);
        await fixture.CreateSchemaAsync(GetComplexSchema(DatabaseType.MySQL));

        var nameWithQuotes = "O'Brien's \"Special\" Name";
        
        // Act - Insert using SqlBuilder
        using (var builder = new Sqlx.SqlBuilder(GetDialect(DatabaseType.MySQL)))
        {
            builder.Append($"INSERT INTO users (name, age, email, created_at) VALUES ({nameWithQuotes}, {30}, {"test@test.com"}, {FormatDateTime(DateTime.Now, DatabaseType.MySQL)})");
            var template = builder.Build();

            using var cmd = fixture.Connection.CreateCommand();
            cmd.CommandText = template.Sql;
            foreach (var param in template.Parameters)
            {
                AddParameter(cmd, param.Key, param.Value);
            }
            await cmd.ExecuteNonQueryAsync();
        }

        // Assert - Retrieve and verify exact match
        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT name FROM users";
        using var reader = await selectCmd.ExecuteReaderAsync();
        Assert.IsTrue(await reader.ReadAsync());
        var retrieved = reader.GetString(0);
        
        Assert.AreEqual(nameWithQuotes, retrieved, "Name with quotes should be preserved exactly");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlBuilder")]
    [TestCategory("Advanced")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_SqlBuilder_NewlinesAndTabs_PreservesExactly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.PostgreSQL);
        await fixture.CreateSchemaAsync(GetComplexSchema(DatabaseType.PostgreSQL));

        var textWithWhitespace = "Line1\nLine2\tTabbed\rCarriageReturn";
        
        // Act - Insert using SqlBuilder
        using (var builder = new Sqlx.SqlBuilder(GetDialect(DatabaseType.PostgreSQL)))
        {
            builder.Append($"INSERT INTO users (name, age, email, created_at) VALUES ({textWithWhitespace}, {30}, {"test@test.com"}, {FormatDateTime(DateTime.Now, DatabaseType.PostgreSQL)})");
            var template = builder.Build();

            using var cmd = fixture.Connection.CreateCommand();
            cmd.CommandText = template.Sql;
            foreach (var param in template.Parameters)
            {
                AddParameter(cmd, param.Key, param.Value);
            }
            await cmd.ExecuteNonQueryAsync();
        }

        // Assert - Retrieve and verify exact match
        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT name FROM users";
        using var reader = await selectCmd.ExecuteReaderAsync();
        Assert.IsTrue(await reader.ReadAsync());
        var retrieved = reader.GetString(0);
        
        Assert.AreEqual(textWithWhitespace, retrieved, "Whitespace characters should be preserved exactly");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlBuilder")]
    [TestCategory("Advanced")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_SqlBuilder_Backslashes_PreservesExactly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SqlServer);
        await fixture.CreateSchemaAsync(GetComplexSchema(DatabaseType.SqlServer));

        var pathWithBackslashes = @"C:\Users\Test\Documents\file.txt";
        
        // Act - Insert using SqlBuilder
        using (var builder = new Sqlx.SqlBuilder(GetDialect(DatabaseType.SqlServer)))
        {
            builder.Append($"INSERT INTO users (name, age, email, created_at) VALUES ({pathWithBackslashes}, {30}, {"test@test.com"}, {FormatDateTime(DateTime.Now, DatabaseType.SqlServer)})");
            var template = builder.Build();

            using var cmd = fixture.Connection.CreateCommand();
            cmd.CommandText = template.Sql;
            foreach (var param in template.Parameters)
            {
                AddParameter(cmd, param.Key, param.Value);
            }
            await cmd.ExecuteNonQueryAsync();
        }

        // Assert - Retrieve and verify exact match
        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT name FROM users";
        using var reader = await selectCmd.ExecuteReaderAsync();
        Assert.IsTrue(await reader.ReadAsync());
        var retrieved = reader.GetString(0);
        
        Assert.AreEqual(pathWithBackslashes, retrieved, "Backslashes should be preserved exactly");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlBuilder")]
    [TestCategory("Advanced")]
    [TestCategory("SQLite")]
    public async Task SQLite_SqlBuilder_UnicodeCharacters_PreservesExactly()
    {
        // Arrange
        await using var fixture = await CreateFixtureAsync(DatabaseType.SQLite);
        await fixture.CreateSchemaAsync(GetComplexSchema(DatabaseType.SQLite));

        var unicodeText = "Hello 世界 🌍 مرحبا שלום";
        
        // Act - Insert using SqlBuilder
        using (var builder = new Sqlx.SqlBuilder(GetDialect(DatabaseType.SQLite)))
        {
            builder.Append($"INSERT INTO users (name, age, email, created_at) VALUES ({unicodeText}, {30}, {"test@test.com"}, {FormatDateTime(DateTime.Now, DatabaseType.SQLite)})");
            var template = builder.Build();

            using var cmd = fixture.Connection.CreateCommand();
            cmd.CommandText = template.Sql;
            foreach (var param in template.Parameters)
            {
                AddParameter(cmd, param.Key, param.Value);
            }
            await cmd.ExecuteNonQueryAsync();
        }

        // Assert - Retrieve and verify exact match
        using var selectCmd = fixture.Connection.CreateCommand();
        selectCmd.CommandText = "SELECT name FROM users";
        using var reader = await selectCmd.ExecuteReaderAsync();
        Assert.IsTrue(await reader.ReadAsync());
        var retrieved = reader.GetString(0);
        
        Assert.AreEqual(unicodeText, retrieved, "Unicode characters should be preserved exactly");
    }
}
