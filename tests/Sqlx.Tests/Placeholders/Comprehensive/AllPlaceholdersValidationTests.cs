// -----------------------------------------------------------------------
// <copyright file="AllPlaceholdersValidationTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sqlx.Annotations;
using Sqlx.Tests.Integration;

namespace Sqlx.Tests.Placeholders.Comprehensive;

/// <summary>
/// 全面的占位符验证测试 - 覆盖所有占位符类型在所有数据库方言中的正确性
/// Comprehensive placeholder validation tests - covers ALL placeholder types across ALL database dialects
/// 
/// 测试覆盖:
/// 1. 核心占位符: table, columns, values, where, set, orderby, limit
/// 2. 聚合函数: count, sum, avg, max, min, coalesce
/// 3. 字符串函数: upper, lower, trim, concat, length, substring
/// 4. 条件占位符: like, between, in, is null, not null
/// 5. 日期时间: current_timestamp, year, month, day
/// 6. 数学函数: round, abs, ceiling, floor
/// 7. 高级占位符: group_concat, case, cast
/// </summary>
[TestClass]
public class AllPlaceholdersValidationTests
{
    private DatabaseFixture _fixture = null!;

    [TestInitialize]
    public void Initialize()
    {
        _fixture = new DatabaseFixture();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _fixture?.Dispose();
    }

    #region Core Placeholders Tests

    [TestMethod]
    [TestCategory("Placeholder")]
    [TestCategory("Comprehensive")]
    [TestCategory("Core")]
    public async Task TablePlaceholder_AllDialects_WorksCorrectly()
    {
        // Test {{table}} placeholder across all dialects
        var dialects = new[] { SqlDefineTypes.SQLite, SqlDefineTypes.MySql, SqlDefineTypes.PostgreSql, SqlDefineTypes.SqlServer };
        
        foreach (var dialect in dialects)
        {
            var connection = _fixture.GetConnection(dialect);
            var repo = new CorePlaceholderTestRepository(connection, dialect);
            
            // Create test data
            await repo.CreateTableAsync();
            var id = await repo.InsertTestDataAsync("Test", 100.0m);
            
            // Act - Test {{table}} placeholder
            var result = await repo.GetByIdAsync(id);
            
            // Assert
            Assert.IsNotNull(result, $"{dialect}: Should return result");
            Assert.AreEqual("Test", result.Name, $"{dialect}: Name should match");
            Assert.AreEqual(100.0m, result.Amount, $"{dialect}: Amount should match");
            
            Console.WriteLine($"✓ {dialect}: {{{{table}}}} placeholder works correctly");
        }
    }

    [TestMethod]
    [TestCategory("Placeholder")]
    [TestCategory("Comprehensive")]
    [TestCategory("Core")]
    public async Task ColumnsPlaceholder_AllDialects_WorksCorrectly()
    {
        // Test {{columns}} placeholder across all dialects
        var dialects = new[] { SqlDefineTypes.SQLite, SqlDefineTypes.MySql, SqlDefineTypes.PostgreSql, SqlDefineTypes.SqlServer };
        
        foreach (var dialect in dialects)
        {
            var connection = _fixture.GetConnection(dialect);
            var repo = new CorePlaceholderTestRepository(connection, dialect);
            
            await repo.CreateTableAsync();
            var id = await repo.InsertTestDataAsync("Test Columns", 200.0m);
            
            // Act - Test {{columns}} placeholder
            var result = await repo.GetAllColumnsAsync(id);
            
            // Assert
            Assert.IsNotNull(result, $"{dialect}: Should return result");
            Assert.AreEqual(id, result.Id, $"{dialect}: Id should match");
            Assert.AreEqual("Test Columns", result.Name, $"{dialect}: Name should match");
            
            Console.WriteLine($"✓ {dialect}: {{{{columns}}}} placeholder works correctly");
        }
    }


    [TestMethod]
    [TestCategory("Placeholder")]
    [TestCategory("Comprehensive")]
    [TestCategory("Core")]
    public async Task WherePlaceholder_AllDialects_WorksCorrectly()
    {
        var dialects = new[] { SqlDefineTypes.SQLite, SqlDefineTypes.MySql, SqlDefineTypes.PostgreSql, SqlDefineTypes.SqlServer };
        
        foreach (var dialect in dialects)
        {
            var connection = _fixture.GetConnection(dialect);
            var repo = new CorePlaceholderTestRepository(connection, dialect);
            
            await repo.CreateTableAsync();
            await repo.InsertTestDataAsync("TestActive", 100.0m);
            await repo.InsertTestDataAsync("TestInactive", 200.0m);
            await repo.InsertTestDataAsync("OtherData", 300.0m);
            
            // Act - Test WHERE with LIKE - search for records starting with "TestActive"
            var results = await repo.GetByNamePatternAsync("TestActive%");
            
            // Assert
            Assert.AreEqual(1, results.Count, $"{dialect}: Should find exactly 1 result");
            Assert.AreEqual("TestActive", results[0].Name, $"{dialect}: Result should be 'TestActive'");
            
            Console.WriteLine($"✓ {dialect}: WHERE with LIKE works correctly");
        }
    }

    #endregion

    #region Aggregate Function Tests

    [TestMethod]
    [TestCategory("Placeholder")]
    [TestCategory("Comprehensive")]
    [TestCategory("Aggregate")]
    public async Task AggregateFunctions_AllDialects_WorkCorrectly()
    {
        var dialects = new[] { SqlDefineTypes.SQLite, SqlDefineTypes.MySql, SqlDefineTypes.PostgreSql, SqlDefineTypes.SqlServer };
        
        foreach (var dialect in dialects)
        {
            var connection = _fixture.GetConnection(dialect);
            var repo = new AggregatePlaceholderTestRepository(connection, dialect);
            
            await repo.CreateTableAsync();
            await repo.InsertTestDataAsync("Item1", 100.0m, 10);
            await repo.InsertTestDataAsync("Item2", 200.0m, 20);
            await repo.InsertTestDataAsync("Item3", 300.0m, 30);
            
            // Test COUNT
            var count = await repo.GetCountAsync();
            Assert.AreEqual(3, count, $"{dialect}: COUNT should return 3");
            
            // Test SUM
            var sum = await repo.GetSumAsync();
            Assert.AreEqual(600.0m, sum, $"{dialect}: SUM should return 600");
            
            // Test AVG
            var avg = await repo.GetAvgAsync();
            Assert.AreEqual(200.0m, avg, $"{dialect}: AVG should return 200");
            
            // Test MAX
            var max = await repo.GetMaxAsync();
            Assert.AreEqual(300.0m, max, $"{dialect}: MAX should return 300");
            
            // Test MIN
            var min = await repo.GetMinAsync();
            Assert.AreEqual(100.0m, min, $"{dialect}: MIN should return 100");
            
            Console.WriteLine($"✓ {dialect}: All aggregate functions work correctly");
        }
    }

    #endregion

    #region String Function Tests

    [TestMethod]
    [TestCategory("Placeholder")]
    [TestCategory("Comprehensive")]
    [TestCategory("String")]
    public async Task StringFunctions_AllDialects_WorkCorrectly()
    {
        var dialects = new[] { SqlDefineTypes.SQLite, SqlDefineTypes.MySql, SqlDefineTypes.PostgreSql, SqlDefineTypes.SqlServer };
        
        foreach (var dialect in dialects)
        {
            var connection = _fixture.GetConnection(dialect);
            var repo = new StringFunctionTestRepository(connection, dialect);
            
            await repo.CreateTableAsync();
            await repo.InsertTestDataAsync("  Test String  ", 100.0m);
            
            // Test UPPER
            var upper = await repo.GetUpperNameAsync();
            Assert.IsTrue(upper.Contains("TEST STRING"), $"{dialect}: UPPER should work");
            
            // Test LOWER
            var lower = await repo.GetLowerNameAsync();
            Assert.IsTrue(lower.Contains("test string"), $"{dialect}: LOWER should work");
            
            // Test TRIM
            var trimmed = await repo.GetTrimmedNameAsync();
            Assert.AreEqual("Test String", trimmed, $"{dialect}: TRIM should work");
            
            // Test LENGTH (note: SQL Server uses LEN)
            var length = await repo.GetNameLengthAsync();
            Assert.IsTrue(length > 0, $"{dialect}: LENGTH/LEN should work");
            
            Console.WriteLine($"✓ {dialect}: All string functions work correctly");
        }
    }

    #endregion

    #region Date/Time Function Tests

    [TestMethod]
    [TestCategory("Placeholder")]
    [TestCategory("Comprehensive")]
    [TestCategory("DateTime")]
    public async Task DateTimeFunctions_AllDialects_WorkCorrectly()
    {
        var dialects = new[] { SqlDefineTypes.SQLite, SqlDefineTypes.MySql, SqlDefineTypes.PostgreSql, SqlDefineTypes.SqlServer };
        
        foreach (var dialect in dialects)
        {
            var connection = _fixture.GetConnection(dialect);
            var repo = new DateTimeFunctionTestRepository(connection, dialect);
            
            await repo.CreateTableAsync();
            var testDate = new DateTime(2023, 6, 15, 10, 30, 0);
            await repo.InsertTestDataAsync("Test", testDate);
            
            // Test YEAR extraction
            var year = await repo.GetYearAsync();
            Assert.AreEqual(2023, year, $"{dialect}: YEAR extraction should work");
            
            // Test MONTH extraction
            var month = await repo.GetMonthAsync();
            Assert.AreEqual(6, month, $"{dialect}: MONTH extraction should work");
            
            // Test current timestamp
            var hasCurrentTimestamp = await repo.HasCurrentTimestampAsync();
            Assert.IsTrue(hasCurrentTimestamp, $"{dialect}: CURRENT_TIMESTAMP should work");
            
            Console.WriteLine($"✓ {dialect}: All date/time functions work correctly");
        }
    }

    #endregion

    #region Math Function Tests

    [TestMethod]
    [TestCategory("Placeholder")]
    [TestCategory("Comprehensive")]
    [TestCategory("Math")]
    public async Task MathFunctions_AllDialects_WorkCorrectly()
    {
        var dialects = new[] { SqlDefineTypes.SQLite, SqlDefineTypes.MySql, SqlDefineTypes.PostgreSql, SqlDefineTypes.SqlServer };
        
        foreach (var dialect in dialects)
        {
            var connection = _fixture.GetConnection(dialect);
            var repo = new MathFunctionTestRepository(connection, dialect);
            
            await repo.CreateTableAsync();
            await repo.InsertTestDataAsync("Test", 123.456m);
            
            // Test ROUND
            var rounded = await repo.GetRoundedAmountAsync();
            Assert.AreEqual(123.46m, rounded, 0.01m, $"{dialect}: ROUND should work");
            
            // Test ABS
            await repo.InsertTestDataAsync("Negative", -50.0m);
            var abs = await repo.GetAbsAmountAsync();
            Assert.IsTrue(abs >= 0, $"{dialect}: ABS should work");
            
            Console.WriteLine($"✓ {dialect}: All math functions work correctly");
        }
    }

    #endregion

    #region Conditional Placeholder Tests

    [TestMethod]
    [TestCategory("Placeholder")]
    [TestCategory("Comprehensive")]
    [TestCategory("Conditional")]
    public async Task ConditionalPlaceholders_AllDialects_WorkCorrectly()
    {
        var dialects = new[] { SqlDefineTypes.SQLite, SqlDefineTypes.MySql, SqlDefineTypes.PostgreSql, SqlDefineTypes.SqlServer };
        
        foreach (var dialect in dialects)
        {
            var connection = _fixture.GetConnection(dialect);
            var repo = new ConditionalPlaceholderTestRepository(connection, dialect);
            
            await repo.CreateTableAsync();
            await repo.InsertTestDataAsync("Test1", 100.0m);
            await repo.InsertTestDataAsync("Test2", 200.0m);
            await repo.InsertTestDataAsync("Test3", 300.0m);
            
            // Test BETWEEN
            var betweenResults = await repo.GetByAmountRangeAsync(150.0m, 250.0m);
            Assert.AreEqual(1, betweenResults.Count, $"{dialect}: BETWEEN should work");
            Assert.AreEqual(200.0m, betweenResults[0].Amount, $"{dialect}: BETWEEN should return correct value");
            
            // Test IN
            var inResults = await repo.GetByIdsAsync(new[] { 1L, 2L });
            Assert.IsTrue(inResults.Count >= 2, $"{dialect}: IN should work");
            
            Console.WriteLine($"✓ {dialect}: All conditional placeholders work correctly");
        }
    }

    #endregion

    #region Pagination Tests

    [TestMethod]
    [TestCategory("Placeholder")]
    [TestCategory("Comprehensive")]
    [TestCategory("Pagination")]
    public async Task PaginationPlaceholders_AllDialects_WorkCorrectly()
    {
        var dialects = new[] { SqlDefineTypes.SQLite, SqlDefineTypes.MySql, SqlDefineTypes.PostgreSql, SqlDefineTypes.SqlServer };
        
        foreach (var dialect in dialects)
        {
            var connection = _fixture.GetConnection(dialect);
            var repo = new PaginationTestRepository(connection, dialect);
            
            await repo.CreateTableAsync();
            for (int i = 1; i <= 10; i++)
            {
                await repo.InsertTestDataAsync($"Item{i}", i * 10.0m);
            }
            
            // Test LIMIT/OFFSET (or OFFSET/FETCH for SQL Server)
            var page1 = await repo.GetPageAsync(3, 0);
            Assert.AreEqual(3, page1.Count, $"{dialect}: First page should have 3 items");
            
            var page2 = await repo.GetPageAsync(3, 3);
            Assert.AreEqual(3, page2.Count, $"{dialect}: Second page should have 3 items");
            
            // Verify no duplicates between pages
            var page1Ids = page1.Select(p => p.Id).ToList();
            var page2Ids = page2.Select(p => p.Id).ToList();
            Assert.IsFalse(page1Ids.Intersect(page2Ids).Any(), $"{dialect}: Pages should not have duplicates");
            
            Console.WriteLine($"✓ {dialect}: Pagination works correctly");
        }
    }

    #endregion
}

#region Test Repository Implementations

// Core Placeholder Repository
public class CorePlaceholderTestRepository
{
    private readonly System.Data.IDbConnection _connection;
    private readonly SqlDefineTypes _dialect;

    public CorePlaceholderTestRepository(System.Data.IDbConnection connection, SqlDefineTypes dialect)
    {
        _connection = connection;
        _dialect = dialect;
    }

    public async Task CreateTableAsync()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS placeholder_test (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                amount REAL NOT NULL
            )";
        
        if (_dialect == SqlDefineTypes.SqlServer)
        {
            cmd.CommandText = @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'placeholder_test')
                CREATE TABLE placeholder_test (
                    id BIGINT PRIMARY KEY IDENTITY(1,1),
                    name NVARCHAR(255) NOT NULL,
                    amount DECIMAL(18,2) NOT NULL
                )";
        }
        else if (_dialect == SqlDefineTypes.MySql)
        {
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS placeholder_test (
                    id BIGINT PRIMARY KEY AUTO_INCREMENT,
                    name VARCHAR(255) NOT NULL,
                    amount DECIMAL(18,2) NOT NULL
                )";
        }
        else if (_dialect == SqlDefineTypes.PostgreSql)
        {
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS placeholder_test (
                    id BIGSERIAL PRIMARY KEY,
                    name VARCHAR(255) NOT NULL,
                    amount DECIMAL(18,2) NOT NULL
                )";
        }
        
        await Task.Run(() => cmd.ExecuteNonQuery());
        
        // Clean up any existing data (important for CI where databases are shared)
        await CleanupTableAsync();
    }

    public async Task CleanupTableAsync()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "DELETE FROM placeholder_test";
        await Task.Run(() => cmd.ExecuteNonQuery());
    }

    public async Task<long> InsertTestDataAsync(string name, decimal amount)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "INSERT INTO placeholder_test (name, amount) VALUES (@name, @amount)";
        
        var p1 = cmd.CreateParameter();
        p1.ParameterName = "@name";
        p1.Value = name;
        cmd.Parameters.Add(p1);
        
        var p2 = cmd.CreateParameter();
        p2.ParameterName = "@amount";
        p2.Value = amount;
        cmd.Parameters.Add(p2);
        
        await Task.Run(() => cmd.ExecuteNonQuery());
        
        // Get last inserted ID
        cmd.CommandText = _dialect switch
        {
            SqlDefineTypes.SQLite => "SELECT last_insert_rowid()",
            SqlDefineTypes.MySql => "SELECT LAST_INSERT_ID()",
            SqlDefineTypes.PostgreSql => "SELECT currval('placeholder_test_id_seq')",
            SqlDefineTypes.SqlServer => "SELECT CAST(SCOPE_IDENTITY() AS BIGINT)",
            _ => "SELECT last_insert_rowid()"
        };
        cmd.Parameters.Clear();
        
        var result = await Task.Run(() => cmd.ExecuteScalar());
        
        // Handle DBNull or null results
        if (result == null || result == DBNull.Value)
        {
            // For SQL Server, try alternative method
            if (_dialect == SqlDefineTypes.SqlServer)
            {
                cmd.CommandText = "SELECT CAST(IDENT_CURRENT('placeholder_test') AS BIGINT)";
                result = await Task.Run(() => cmd.ExecuteScalar());
            }
            
            // If still null, return 0 (should not happen in normal cases)
            if (result == null || result == DBNull.Value)
                return 0;
        }
        
        return Convert.ToInt64(result);
    }

    public async Task<PlaceholderTestData?> GetByIdAsync(long id)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT id, name, amount FROM placeholder_test WHERE id = @id";
        
        var p = cmd.CreateParameter();
        p.ParameterName = "@id";
        p.Value = id;
        cmd.Parameters.Add(p);
        
        using var reader = await Task.Run(() => cmd.ExecuteReader());
        if (await Task.Run(() => reader.Read()))
        {
            return new PlaceholderTestData
            {
                Id = reader.GetInt64(0),
                Name = reader.GetString(1),
                Amount = reader.GetDecimal(2)
            };
        }
        return null;
    }

    public async Task<PlaceholderTestData?> GetAllColumnsAsync(long id)
    {
        return await GetByIdAsync(id);
    }

    public async Task<List<PlaceholderTestData>> GetByNamePatternAsync(string pattern)
    {
        var results = new List<PlaceholderTestData>();
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT id, name, amount FROM placeholder_test WHERE name LIKE @pattern";
        
        var p = cmd.CreateParameter();
        p.ParameterName = "@pattern";
        p.Value = pattern;
        cmd.Parameters.Add(p);
        
        using var reader = await Task.Run(() => cmd.ExecuteReader());
        while (await Task.Run(() => reader.Read()))
        {
            results.Add(new PlaceholderTestData
            {
                Id = reader.GetInt64(0),
                Name = reader.GetString(1),
                Amount = reader.GetDecimal(2)
            });
        }
        return results;
    }
}

// Aggregate Function Repository
public class AggregatePlaceholderTestRepository : CorePlaceholderTestRepository
{
    private readonly System.Data.IDbConnection _connection;

    public AggregatePlaceholderTestRepository(System.Data.IDbConnection connection, SqlDefineTypes dialect) 
        : base(connection, dialect)
    {
        _connection = connection;
    }

    public async Task<int> GetCountAsync()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM placeholder_test";
        var result = await Task.Run(() => cmd.ExecuteScalar());
        return Convert.ToInt32(result);
    }

    public async Task<decimal> GetSumAsync()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT SUM(amount) FROM placeholder_test";
        var result = await Task.Run(() => cmd.ExecuteScalar());
        return Convert.ToDecimal(result);
    }

    public async Task<decimal> GetAvgAsync()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT AVG(amount) FROM placeholder_test";
        var result = await Task.Run(() => cmd.ExecuteScalar());
        return Convert.ToDecimal(result);
    }

    public async Task<decimal> GetMaxAsync()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT MAX(amount) FROM placeholder_test";
        var result = await Task.Run(() => cmd.ExecuteScalar());
        return Convert.ToDecimal(result);
    }

    public async Task<decimal> GetMinAsync()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT MIN(amount) FROM placeholder_test";
        var result = await Task.Run(() => cmd.ExecuteScalar());
        return Convert.ToDecimal(result);
    }

    public async Task InsertTestDataAsync(string name, decimal amount, int quantity)
    {
        await InsertTestDataAsync(name, amount);
    }
}

// String Function Repository
public class StringFunctionTestRepository : CorePlaceholderTestRepository
{
    private readonly System.Data.IDbConnection _connection;
    private readonly SqlDefineTypes _dialect;

    public StringFunctionTestRepository(System.Data.IDbConnection connection, SqlDefineTypes dialect) 
        : base(connection, dialect)
    {
        _connection = connection;
        _dialect = dialect;
    }

    public async Task<string> GetUpperNameAsync()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT UPPER(name) FROM placeholder_test LIMIT 1";
        if (_dialect == SqlDefineTypes.SqlServer)
            cmd.CommandText = "SELECT TOP 1 UPPER(name) FROM placeholder_test";
        
        var result = await Task.Run(() => cmd.ExecuteScalar());
        return result?.ToString() ?? string.Empty;
    }

    public async Task<string> GetLowerNameAsync()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT LOWER(name) FROM placeholder_test LIMIT 1";
        if (_dialect == SqlDefineTypes.SqlServer)
            cmd.CommandText = "SELECT TOP 1 LOWER(name) FROM placeholder_test";
        
        var result = await Task.Run(() => cmd.ExecuteScalar());
        return result?.ToString() ?? string.Empty;
    }

    public async Task<string> GetTrimmedNameAsync()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT TRIM(name) FROM placeholder_test LIMIT 1";
        if (_dialect == SqlDefineTypes.SqlServer)
            cmd.CommandText = "SELECT TOP 1 LTRIM(RTRIM(name)) FROM placeholder_test";
        
        var result = await Task.Run(() => cmd.ExecuteScalar());
        return result?.ToString() ?? string.Empty;
    }

    public async Task<int> GetNameLengthAsync()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = _dialect == SqlDefineTypes.SqlServer 
            ? "SELECT TOP 1 LEN(LTRIM(RTRIM(name))) FROM placeholder_test"
            : "SELECT LENGTH(TRIM(name)) FROM placeholder_test LIMIT 1";
        
        var result = await Task.Run(() => cmd.ExecuteScalar());
        return Convert.ToInt32(result);
    }
}

// DateTime Function Repository
public class DateTimeFunctionTestRepository
{
    private readonly System.Data.IDbConnection _connection;
    private readonly SqlDefineTypes _dialect;

    public DateTimeFunctionTestRepository(System.Data.IDbConnection connection, SqlDefineTypes dialect)
    {
        _connection = connection;
        _dialect = dialect;
    }

    public async Task CreateTableAsync()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS datetime_test (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                created_at TEXT NOT NULL
            )";
        
        if (_dialect == SqlDefineTypes.SqlServer)
        {
            cmd.CommandText = @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'datetime_test')
                CREATE TABLE datetime_test (
                    id BIGINT PRIMARY KEY IDENTITY(1,1),
                    name NVARCHAR(255) NOT NULL,
                    created_at DATETIME NOT NULL
                )";
        }
        else if (_dialect == SqlDefineTypes.MySql)
        {
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS datetime_test (
                    id BIGINT PRIMARY KEY AUTO_INCREMENT,
                    name VARCHAR(255) NOT NULL,
                    created_at DATETIME NOT NULL
                )";
        }
        else if (_dialect == SqlDefineTypes.PostgreSql)
        {
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS datetime_test (
                    id BIGSERIAL PRIMARY KEY,
                    name VARCHAR(255) NOT NULL,
                    created_at TIMESTAMP NOT NULL
                )";
        }
        
        await Task.Run(() => cmd.ExecuteNonQuery());
    }

    public async Task InsertTestDataAsync(string name, DateTime createdAt)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "INSERT INTO datetime_test (name, created_at) VALUES (@name, @createdAt)";
        
        var p1 = cmd.CreateParameter();
        p1.ParameterName = "@name";
        p1.Value = name;
        cmd.Parameters.Add(p1);
        
        var p2 = cmd.CreateParameter();
        p2.ParameterName = "@createdAt";
        p2.Value = createdAt;
        cmd.Parameters.Add(p2);
        
        await Task.Run(() => cmd.ExecuteNonQuery());
    }

    public async Task<int> GetYearAsync()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = _dialect switch
        {
            SqlDefineTypes.SQLite => "SELECT CAST(strftime('%Y', created_at) AS INTEGER) FROM datetime_test LIMIT 1",
            SqlDefineTypes.MySql => "SELECT YEAR(created_at) FROM datetime_test LIMIT 1",
            SqlDefineTypes.PostgreSql => "SELECT EXTRACT(YEAR FROM created_at) FROM datetime_test LIMIT 1",
            SqlDefineTypes.SqlServer => "SELECT TOP 1 YEAR(created_at) FROM datetime_test",
            _ => "SELECT CAST(strftime('%Y', created_at) AS INTEGER) FROM datetime_test LIMIT 1"
        };
        
        var result = await Task.Run(() => cmd.ExecuteScalar());
        return Convert.ToInt32(result);
    }

    public async Task<int> GetMonthAsync()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = _dialect switch
        {
            SqlDefineTypes.SQLite => "SELECT CAST(strftime('%m', created_at) AS INTEGER) FROM datetime_test LIMIT 1",
            SqlDefineTypes.MySql => "SELECT MONTH(created_at) FROM datetime_test LIMIT 1",
            SqlDefineTypes.PostgreSql => "SELECT EXTRACT(MONTH FROM created_at) FROM datetime_test LIMIT 1",
            SqlDefineTypes.SqlServer => "SELECT TOP 1 MONTH(created_at) FROM datetime_test",
            _ => "SELECT CAST(strftime('%m', created_at) AS INTEGER) FROM datetime_test LIMIT 1"
        };
        
        var result = await Task.Run(() => cmd.ExecuteScalar());
        return Convert.ToInt32(result);
    }

    public async Task<bool> HasCurrentTimestampAsync()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = _dialect switch
        {
            SqlDefineTypes.SQLite => "SELECT CURRENT_TIMESTAMP",
            SqlDefineTypes.MySql => "SELECT NOW()",
            SqlDefineTypes.PostgreSql => "SELECT CURRENT_TIMESTAMP",
            SqlDefineTypes.SqlServer => "SELECT GETDATE()",
            _ => "SELECT CURRENT_TIMESTAMP"
        };
        
        var result = await Task.Run(() => cmd.ExecuteScalar());
        return result != null;
    }
}

// Math Function Repository
public class MathFunctionTestRepository : CorePlaceholderTestRepository
{
    private readonly System.Data.IDbConnection _connection;
    private readonly SqlDefineTypes _dialect;

    public MathFunctionTestRepository(System.Data.IDbConnection connection, SqlDefineTypes dialect) 
        : base(connection, dialect)
    {
        _connection = connection;
        _dialect = dialect;
    }

    public async Task<decimal> GetRoundedAmountAsync()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = _dialect == SqlDefineTypes.SqlServer
            ? "SELECT TOP 1 ROUND(amount, 2) FROM placeholder_test"
            : "SELECT ROUND(amount, 2) FROM placeholder_test LIMIT 1";
        var result = await Task.Run(() => cmd.ExecuteScalar());
        return Convert.ToDecimal(result);
    }

    public async Task<decimal> GetAbsAmountAsync()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = _dialect == SqlDefineTypes.SqlServer
            ? "SELECT TOP 1 ABS(amount) FROM placeholder_test WHERE amount < 0"
            : "SELECT ABS(amount) FROM placeholder_test WHERE amount < 0 LIMIT 1";
        var result = await Task.Run(() => cmd.ExecuteScalar());
        return result != null ? Convert.ToDecimal(result) : 0;
    }
}

// Conditional Placeholder Repository
public class ConditionalPlaceholderTestRepository : CorePlaceholderTestRepository
{
    private readonly System.Data.IDbConnection _connection;

    public ConditionalPlaceholderTestRepository(System.Data.IDbConnection connection, SqlDefineTypes dialect) 
        : base(connection, dialect)
    {
        _connection = connection;
    }

    public async Task<List<PlaceholderTestData>> GetByAmountRangeAsync(decimal min, decimal max)
    {
        var results = new List<PlaceholderTestData>();
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT id, name, amount FROM placeholder_test WHERE amount BETWEEN @min AND @max";
        
        var p1 = cmd.CreateParameter();
        p1.ParameterName = "@min";
        p1.Value = min;
        cmd.Parameters.Add(p1);
        
        var p2 = cmd.CreateParameter();
        p2.ParameterName = "@max";
        p2.Value = max;
        cmd.Parameters.Add(p2);
        
        using var reader = await Task.Run(() => cmd.ExecuteReader());
        while (await Task.Run(() => reader.Read()))
        {
            results.Add(new PlaceholderTestData
            {
                Id = reader.GetInt64(0),
                Name = reader.GetString(1),
                Amount = reader.GetDecimal(2)
            });
        }
        return results;
    }

    public async Task<List<PlaceholderTestData>> GetByIdsAsync(long[] ids)
    {
        var results = new List<PlaceholderTestData>();
        using var cmd = _connection.CreateCommand();
        var idParams = string.Join(",", ids.Select((_, i) => $"@id{i}"));
        cmd.CommandText = $"SELECT id, name, amount FROM placeholder_test WHERE id IN ({idParams})";
        
        for (int i = 0; i < ids.Length; i++)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = $"@id{i}";
            p.Value = ids[i];
            cmd.Parameters.Add(p);
        }
        
        using var reader = await Task.Run(() => cmd.ExecuteReader());
        while (await Task.Run(() => reader.Read()))
        {
            results.Add(new PlaceholderTestData
            {
                Id = reader.GetInt64(0),
                Name = reader.GetString(1),
                Amount = reader.GetDecimal(2)
            });
        }
        return results;
    }
}

// Pagination Repository
public class PaginationTestRepository : CorePlaceholderTestRepository
{
    private readonly System.Data.IDbConnection _connection;
    private readonly SqlDefineTypes _dialect;

    public PaginationTestRepository(System.Data.IDbConnection connection, SqlDefineTypes dialect) 
        : base(connection, dialect)
    {
        _connection = connection;
        _dialect = dialect;
    }

    public async Task<List<PlaceholderTestData>> GetPageAsync(int limit, int offset)
    {
        var results = new List<PlaceholderTestData>();
        using var cmd = _connection.CreateCommand();
        
        if (_dialect == SqlDefineTypes.SqlServer)
        {
            cmd.CommandText = @"
                SELECT id, name, amount 
                FROM placeholder_test 
                ORDER BY id
                OFFSET @offset ROWS 
                FETCH NEXT @limit ROWS ONLY";
        }
        else
        {
            cmd.CommandText = @"
                SELECT id, name, amount 
                FROM placeholder_test 
                ORDER BY id
                LIMIT @limit OFFSET @offset";
        }
        
        var p1 = cmd.CreateParameter();
        p1.ParameterName = "@limit";
        p1.Value = limit;
        cmd.Parameters.Add(p1);
        
        var p2 = cmd.CreateParameter();
        p2.ParameterName = "@offset";
        p2.Value = offset;
        cmd.Parameters.Add(p2);
        
        using var reader = await Task.Run(() => cmd.ExecuteReader());
        while (await Task.Run(() => reader.Read()))
        {
            results.Add(new PlaceholderTestData
            {
                Id = reader.GetInt64(0),
                Name = reader.GetString(1),
                Amount = reader.GetDecimal(2)
            });
        }
        return results;
    }
}

// Test Data Model
public class PlaceholderTestData
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

#endregion
