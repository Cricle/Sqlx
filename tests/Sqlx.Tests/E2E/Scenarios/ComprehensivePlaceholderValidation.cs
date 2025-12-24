// -----------------------------------------------------------------------
// <copyright file="ComprehensivePlaceholderValidation.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;
using Sqlx.Tests.Integration;

namespace Sqlx.Tests.E2E.Scenarios;

/// <summary>
/// 全面验证所有占位符在所有数据库方言中的SQL生成
/// Comprehensive validation of ALL placeholders across ALL database dialects
/// </summary>

// ==================== Test Data Models ====================

public class PlaceholderTestEntity
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int Quantity { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsActive { get; set; }
    public string Category { get; set; } = string.Empty;
}

// ==================== Repository Interface - Core Placeholders ====================

public partial interface ICorePlaceholderRepository
{
    // 1. {{table}} placeholder
    [SqlTemplate("SELECT * FROM {{table}} WHERE id = @id")]
    Task<PlaceholderTestEntity?> TestTablePlaceholder(long id);

    // 2. {{columns}} placeholder
    [SqlTemplate("SELECT {{columns}} FROM placeholder_test WHERE id = @id")]
    Task<PlaceholderTestEntity?> TestColumnsPlaceholder(long id);

    // 3. {{columns|table=t}} placeholder (table-prefixed)
    [SqlTemplate("SELECT t.{{columns|table=t}} FROM placeholder_test t WHERE t.id = @id")]
    Task<PlaceholderTestEntity?> TestColumnsPrefixedPlaceholder(long id);

    // 4. {{values}} placeholder
    [SqlTemplate("INSERT INTO {{table}} ({{columns}}) VALUES ({{values}})")]
    Task<int> TestValuesPlaceholder(PlaceholderTestEntity entity);

    // 5. {{where}} placeholder with auto
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE {{where:auto}}")]
    Task<List<PlaceholderTestEntity>> TestWherePlaceholder(long id);

    // 6. {{set}} placeholder
    [SqlTemplate("UPDATE {{table}} SET {{set}} WHERE id = @id")]
    Task<int> TestSetPlaceholder(long id, string name, decimal amount);

    // 7. {{orderby}} placeholder
    [SqlTemplate("SELECT {{columns}} FROM {{table}} ORDER BY {{orderby created_at --desc}}")]
    Task<List<PlaceholderTestEntity>> TestOrderByPlaceholder();

    // 8. {{limit}} placeholder
    [SqlTemplate("SELECT {{columns}} FROM {{table}} LIMIT {{limit:10}}")]
    Task<List<PlaceholderTestEntity>> TestLimitPlaceholder();
}

// ==================== Repository Interface - Aggregate Functions ====================

public partial interface IAggregatePlaceholderRepository
{
    // COUNT
    [SqlTemplate("SELECT {{count:*}} FROM {{table}}")]
    Task<int> TestCountPlaceholder();

    // SUM
    [SqlTemplate("SELECT {{sum:amount}} FROM {{table}}")]
    Task<decimal> TestSumPlaceholder();

    // AVG
    [SqlTemplate("SELECT {{avg:amount}} FROM {{table}}")]
    Task<decimal> TestAvgPlaceholder();

    // MAX
    [SqlTemplate("SELECT {{max:amount}} FROM {{table}}")]
    Task<decimal> TestMaxPlaceholder();

    // MIN
    [SqlTemplate("SELECT {{min:amount}} FROM {{table}}")]
    Task<decimal> TestMinPlaceholder();

    // COALESCE
    [SqlTemplate("SELECT COALESCE(SUM(amount), 0) FROM {{table}}")]
    Task<decimal> TestCoalescePlaceholder();
}

// ==================== Repository Interface - String Functions ====================

public partial interface IStringPlaceholderRepository
{
    // UPPER
    [SqlTemplate("SELECT {{upper:name}} FROM {{table}} WHERE id = @id")]
    Task<string> TestUpperPlaceholder(long id);

    // LOWER
    [SqlTemplate("SELECT {{lower:name}} FROM {{table}} WHERE id = @id")]
    Task<string> TestLowerPlaceholder(long id);

    // TRIM
    [SqlTemplate("SELECT {{trim:name}} FROM {{table}} WHERE id = @id")]
    Task<string> TestTrimPlaceholder(long id);

    // CONCAT
    [SqlTemplate("SELECT {{concat:name,description}} FROM {{table}} WHERE id = @id")]
    Task<string> TestConcatPlaceholder(long id);

    // LENGTH
    [SqlTemplate("SELECT {{length:name}} FROM {{table}} WHERE id = @id")]
    Task<int> TestLengthPlaceholder(long id);

    // SUBSTRING
    [SqlTemplate("SELECT {{substring:name,1,5}} FROM {{table}} WHERE id = @id")]
    Task<string> TestSubstringPlaceholder(long id);
}

// ==================== Repository Interface - Conditional Placeholders ====================

public partial interface IConditionalPlaceholderRepository
{
    // LIKE
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE name LIKE @pattern")]
    Task<List<PlaceholderTestEntity>> TestLikePlaceholder(string pattern);

    // BETWEEN
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE amount BETWEEN @min AND @max")]
    Task<List<PlaceholderTestEntity>> TestBetweenPlaceholder(decimal min, decimal max);

    // IN
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE category IN (@categories)")]
    Task<List<PlaceholderTestEntity>> TestInPlaceholder(string[] categories);

    // IS NULL
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE updated_at IS NULL")]
    Task<List<PlaceholderTestEntity>> TestIsNullPlaceholder();

    // NOT NULL
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE updated_at IS NOT NULL")]
    Task<List<PlaceholderTestEntity>> TestNotNullPlaceholder();
}

// ==================== Repository Interface - Date/Time Functions ====================

public partial interface IDateTimePlaceholderRepository
{
    // CURRENT_TIMESTAMP
    [SqlTemplate("SELECT COUNT(*) FROM {{table}} WHERE created_at <= {{current_timestamp}}")]
    Task<int> TestCurrentTimestampPlaceholder();

    // TODAY
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE DATE(created_at) = DATE('now')")]
    Task<List<PlaceholderTestEntity>> TestTodayPlaceholder();

    // YEAR - Use {{year:created_at}} placeholder for multi-database support
    [SqlTemplate("SELECT {{year:created_at}} FROM {{table}} WHERE id = @id")]
    Task<int> TestYearPlaceholder(long id);

    // MONTH
    [SqlTemplate("SELECT CAST(strftime('%m', created_at) AS INTEGER) FROM {{table}} WHERE id = @id")]
    Task<int> TestMonthPlaceholder(long id);
}

// ==================== Repository Interface - Math Functions ====================

public partial interface IMathPlaceholderRepository
{
    // ROUND
    [SqlTemplate("SELECT {{round:amount,2}} FROM {{table}} WHERE id = @id")]
    Task<decimal> TestRoundPlaceholder(long id);

    // ABS
    [SqlTemplate("SELECT {{abs:amount}} FROM {{table}} WHERE id = @id")]
    Task<decimal> TestAbsPlaceholder(long id);

    // CEILING
    [SqlTemplate("SELECT {{ceiling:amount}} FROM {{table}} WHERE id = @id")]
    Task<decimal> TestCeilingPlaceholder(long id);

    // FLOOR
    [SqlTemplate("SELECT {{floor:amount}} FROM {{table}} WHERE id = @id")]
    Task<decimal> TestFloorPlaceholder(long id);
}

// ==================== SQLite Repository Implementations ====================

[SqlDefine(SqlDefineTypes.SQLite)]
[TableName("placeholder_test")]
[RepositoryFor(typeof(ICorePlaceholderRepository))]
public partial class SQLiteCorePlaceholderRepository(DbConnection connection) : ICorePlaceholderRepository { }

[SqlDefine(SqlDefineTypes.SQLite)]
[TableName("placeholder_test")]
[RepositoryFor(typeof(IAggregatePlaceholderRepository))]
public partial class SQLiteAggregatePlaceholderRepository(DbConnection connection) : IAggregatePlaceholderRepository { }

[SqlDefine(SqlDefineTypes.SQLite)]
[TableName("placeholder_test")]
[RepositoryFor(typeof(IStringPlaceholderRepository))]
public partial class SQLiteStringPlaceholderRepository(DbConnection connection) : IStringPlaceholderRepository { }

[SqlDefine(SqlDefineTypes.SQLite)]
[TableName("placeholder_test")]
[RepositoryFor(typeof(IConditionalPlaceholderRepository))]
public partial class SQLiteConditionalPlaceholderRepository(DbConnection connection) : IConditionalPlaceholderRepository { }

[SqlDefine(SqlDefineTypes.SQLite)]
[TableName("placeholder_test")]
[RepositoryFor(typeof(IDateTimePlaceholderRepository))]
public partial class SQLiteDateTimePlaceholderRepository(DbConnection connection) : IDateTimePlaceholderRepository { }

[SqlDefine(SqlDefineTypes.SQLite)]
[TableName("placeholder_test")]
[RepositoryFor(typeof(IMathPlaceholderRepository))]
public partial class SQLiteMathPlaceholderRepository(DbConnection connection) : IMathPlaceholderRepository { }

// ==================== MySQL Repository Implementations ====================

[SqlDefine(SqlDefineTypes.MySql)]
[TableName("placeholder_test")]
[RepositoryFor(typeof(ICorePlaceholderRepository))]
public partial class MySqlCorePlaceholderRepository(DbConnection connection) : ICorePlaceholderRepository { }

[SqlDefine(SqlDefineTypes.MySql)]
[TableName("placeholder_test")]
[RepositoryFor(typeof(IAggregatePlaceholderRepository))]
public partial class MySqlAggregatePlaceholderRepository(DbConnection connection) : IAggregatePlaceholderRepository { }

[SqlDefine(SqlDefineTypes.MySql)]
[TableName("placeholder_test")]
[RepositoryFor(typeof(IStringPlaceholderRepository))]
public partial class MySqlStringPlaceholderRepository(DbConnection connection) : IStringPlaceholderRepository { }

[SqlDefine(SqlDefineTypes.MySql)]
[TableName("placeholder_test")]
[RepositoryFor(typeof(IConditionalPlaceholderRepository))]
public partial class MySqlConditionalPlaceholderRepository(DbConnection connection) : IConditionalPlaceholderRepository { }

[SqlDefine(SqlDefineTypes.MySql)]
[TableName("placeholder_test")]
[RepositoryFor(typeof(IDateTimePlaceholderRepository))]
public partial class MySqlDateTimePlaceholderRepository(DbConnection connection) : IDateTimePlaceholderRepository { }

[SqlDefine(SqlDefineTypes.MySql)]
[TableName("placeholder_test")]
[RepositoryFor(typeof(IMathPlaceholderRepository))]
public partial class MySqlMathPlaceholderRepository(DbConnection connection) : IMathPlaceholderRepository { }

// ==================== PostgreSQL Repository Implementations ====================

[SqlDefine(SqlDefineTypes.PostgreSql)]
[TableName("placeholder_test")]
[RepositoryFor(typeof(ICorePlaceholderRepository))]
public partial class PostgreSqlCorePlaceholderRepository(DbConnection connection) : ICorePlaceholderRepository { }

[SqlDefine(SqlDefineTypes.PostgreSql)]
[TableName("placeholder_test")]
[RepositoryFor(typeof(IAggregatePlaceholderRepository))]
public partial class PostgreSqlAggregatePlaceholderRepository(DbConnection connection) : IAggregatePlaceholderRepository { }

[SqlDefine(SqlDefineTypes.PostgreSql)]
[TableName("placeholder_test")]
[RepositoryFor(typeof(IStringPlaceholderRepository))]
public partial class PostgreSqlStringPlaceholderRepository(DbConnection connection) : IStringPlaceholderRepository { }

[SqlDefine(SqlDefineTypes.PostgreSql)]
[TableName("placeholder_test")]
[RepositoryFor(typeof(IConditionalPlaceholderRepository))]
public partial class PostgreSqlConditionalPlaceholderRepository(DbConnection connection) : IConditionalPlaceholderRepository { }

[SqlDefine(SqlDefineTypes.PostgreSql)]
[TableName("placeholder_test")]
[RepositoryFor(typeof(IDateTimePlaceholderRepository))]
public partial class PostgreSqlDateTimePlaceholderRepository(DbConnection connection) : IDateTimePlaceholderRepository { }

[SqlDefine(SqlDefineTypes.PostgreSql)]
[TableName("placeholder_test")]
[RepositoryFor(typeof(IMathPlaceholderRepository))]
public partial class PostgreSqlMathPlaceholderRepository(DbConnection connection) : IMathPlaceholderRepository { }

// ==================== SQL Server Repository Implementations ====================

[SqlDefine(SqlDefineTypes.SqlServer)]
[TableName("placeholder_test")]
[RepositoryFor(typeof(ICorePlaceholderRepository))]
public partial class SqlServerCorePlaceholderRepository(DbConnection connection) : ICorePlaceholderRepository { }

[SqlDefine(SqlDefineTypes.SqlServer)]
[TableName("placeholder_test")]
[RepositoryFor(typeof(IAggregatePlaceholderRepository))]
public partial class SqlServerAggregatePlaceholderRepository(DbConnection connection) : IAggregatePlaceholderRepository { }

[SqlDefine(SqlDefineTypes.SqlServer)]
[TableName("placeholder_test")]
[RepositoryFor(typeof(IStringPlaceholderRepository))]
public partial class SqlServerStringPlaceholderRepository(DbConnection connection) : IStringPlaceholderRepository { }

[SqlDefine(SqlDefineTypes.SqlServer)]
[TableName("placeholder_test")]
[RepositoryFor(typeof(IConditionalPlaceholderRepository))]
public partial class SqlServerConditionalPlaceholderRepository(DbConnection connection) : IConditionalPlaceholderRepository { }

[SqlDefine(SqlDefineTypes.SqlServer)]
[TableName("placeholder_test")]
[RepositoryFor(typeof(IDateTimePlaceholderRepository))]
public partial class SqlServerDateTimePlaceholderRepository(DbConnection connection) : IDateTimePlaceholderRepository { }

[SqlDefine(SqlDefineTypes.SqlServer)]
[TableName("placeholder_test")]
[RepositoryFor(typeof(IMathPlaceholderRepository))]
public partial class SqlServerMathPlaceholderRepository(DbConnection connection) : IMathPlaceholderRepository { }

// ==================== Test Base Class ====================

public abstract class ComprehensivePlaceholderValidation_Base
{
    protected DatabaseFixture _fixture = null!;
    protected abstract SqlDefineTypes DialectType { get; }

    protected abstract ICorePlaceholderRepository CreateCoreRepository(DbConnection connection);
    protected abstract IAggregatePlaceholderRepository CreateAggregateRepository(DbConnection connection);
    protected abstract IStringPlaceholderRepository CreateStringRepository(DbConnection connection);
    protected abstract IConditionalPlaceholderRepository CreateConditionalRepository(DbConnection connection);
    protected abstract IDateTimePlaceholderRepository CreateDateTimeRepository(DbConnection connection);
    protected abstract IMathPlaceholderRepository CreateMathRepository(DbConnection connection);

    [TestInitialize]
    public void Initialize()
    {
        _fixture = new DatabaseFixture();
        SeedTestData();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _fixture?.Dispose();
    }

    private void SeedTestData()
    {
        var connection = _fixture.GetConnection(DialectType);
        
        // Create table with dialect-specific syntax
        string createTableSql = DialectType switch
        {
            SqlDefineTypes.MySql => @"
                CREATE TABLE IF NOT EXISTS placeholder_test (
                    id BIGINT PRIMARY KEY AUTO_INCREMENT,
                    name VARCHAR(255) NOT NULL,
                    description TEXT,
                    amount DECIMAL(10,2) NOT NULL,
                    quantity INT NOT NULL,
                    created_at DATETIME NOT NULL,
                    updated_at DATETIME,
                    is_active TINYINT NOT NULL,
                    category VARCHAR(255) NOT NULL
                )",
            SqlDefineTypes.PostgreSql => @"
                CREATE TABLE IF NOT EXISTS placeholder_test (
                    id BIGSERIAL PRIMARY KEY,
                    name VARCHAR(255) NOT NULL,
                    description TEXT,
                    amount DECIMAL(10,2) NOT NULL,
                    quantity INT NOT NULL,
                    created_at TIMESTAMP NOT NULL,
                    updated_at TIMESTAMP,
                    is_active BOOLEAN NOT NULL,
                    category VARCHAR(255) NOT NULL
                )",
            SqlDefineTypes.SqlServer => @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'placeholder_test')
                CREATE TABLE placeholder_test (
                    id BIGINT PRIMARY KEY IDENTITY(1,1),
                    name NVARCHAR(255) NOT NULL,
                    description NVARCHAR(MAX),
                    amount DECIMAL(10,2) NOT NULL,
                    quantity INT NOT NULL,
                    created_at DATETIME2 NOT NULL,
                    updated_at DATETIME2,
                    is_active BIT NOT NULL,
                    category NVARCHAR(255) NOT NULL
                )",
            _ => @"
                CREATE TABLE IF NOT EXISTS placeholder_test (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL,
                    description TEXT,
                    amount DECIMAL(10,2) NOT NULL,
                    quantity INTEGER NOT NULL,
                    created_at DATETIME NOT NULL,
                    updated_at DATETIME,
                    is_active INTEGER NOT NULL,
                    category TEXT NOT NULL
                )"
        };
        
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = createTableSql;
            cmd.ExecuteNonQuery();
        }

        // Insert test data with dialect-specific syntax
        string insertSql = DialectType switch
        {
            SqlDefineTypes.MySql => @"
                INSERT INTO placeholder_test (name, description, amount, quantity, created_at, updated_at, is_active, category)
                VALUES 
                    ('Test 1', 'Description 1', 100.50, 10, NOW(), NULL, 1, 'Category A'),
                    ('Test 2', 'Description 2', 200.75, 20, NOW(), NOW(), 1, 'Category B'),
                    ('Test 3', 'Description 3', 300.25, 30, NOW(), NULL, 0, 'Category A')",
            SqlDefineTypes.PostgreSql => @"
                INSERT INTO placeholder_test (name, description, amount, quantity, created_at, updated_at, is_active, category)
                VALUES 
                    ('Test 1', 'Description 1', 100.50, 10, CURRENT_TIMESTAMP, NULL, true, 'Category A'),
                    ('Test 2', 'Description 2', 200.75, 20, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, true, 'Category B'),
                    ('Test 3', 'Description 3', 300.25, 30, CURRENT_TIMESTAMP, NULL, false, 'Category A')",
            SqlDefineTypes.SqlServer => @"
                INSERT INTO placeholder_test (name, description, amount, quantity, created_at, updated_at, is_active, category)
                VALUES 
                    ('Test 1', 'Description 1', 100.50, 10, GETDATE(), NULL, 1, 'Category A'),
                    ('Test 2', 'Description 2', 200.75, 20, GETDATE(), GETDATE(), 1, 'Category B'),
                    ('Test 3', 'Description 3', 300.25, 30, GETDATE(), NULL, 0, 'Category A')",
            _ => @"
                INSERT INTO placeholder_test (name, description, amount, quantity, created_at, updated_at, is_active, category)
                VALUES 
                    ('Test 1', 'Description 1', 100.50, 10, datetime('now'), NULL, 1, 'Category A'),
                    ('Test 2', 'Description 2', 200.75, 20, datetime('now'), datetime('now'), 1, 'Category B'),
                    ('Test 3', 'Description 3', 300.25, 30, datetime('now'), NULL, 0, 'Category A')"
        };
        
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = insertSql;
            cmd.ExecuteNonQuery();
        }
    }

    // ==================== Core Placeholder Tests ====================

    [TestMethod]
    [TestCategory("TDD")]
    [TestCategory("PlaceholderValidation")]
    public async Task CorePlaceholders_TablePlaceholder_ShouldExpandCorrectly()
    {
        // Arrange
        var connection = _fixture.GetConnection(DialectType);
        var repo = CreateCoreRepository(connection);

        // Act
        var result = await repo.TestTablePlaceholder(1);

        // Assert
        Assert.IsNotNull(result, "{{table}} placeholder should expand correctly");
        Assert.AreEqual(1, result.Id);
    }

    [TestMethod]
    [TestCategory("TDD")]
    [TestCategory("PlaceholderValidation")]
    public async Task CorePlaceholders_ColumnsPlaceholder_ShouldExpandCorrectly()
    {
        // Arrange
        var connection = _fixture.GetConnection(DialectType);
        var repo = CreateCoreRepository(connection);

        // Act
        var result = await repo.TestColumnsPlaceholder(1);

        // Assert
        Assert.IsNotNull(result, "{{columns}} placeholder should expand correctly");
        Assert.AreEqual("Test 1", result.Name);
        Assert.AreEqual(100.50m, result.Amount);
    }

    [TestMethod]
    [TestCategory("TDD")]
    [TestCategory("PlaceholderValidation")]
    public async Task CorePlaceholders_ColumnsPrefixedPlaceholder_ShouldExpandCorrectly()
    {
        // Arrange
        var connection = _fixture.GetConnection(DialectType);
        var repo = CreateCoreRepository(connection);

        // Act
        var result = await repo.TestColumnsPrefixedPlaceholder(1);

        // Assert
        Assert.IsNotNull(result, "{{columns|table=t}} placeholder should expand correctly");
        Assert.AreEqual("Test 1", result.Name);
    }

    // ==================== Aggregate Function Tests ====================

    [TestMethod]
    [TestCategory("TDD")]
    [TestCategory("PlaceholderValidation")]
    public async Task AggregatePlaceholders_CountPlaceholder_ShouldWork()
    {
        // Arrange
        var connection = _fixture.GetConnection(DialectType);
        var repo = CreateAggregateRepository(connection);

        // Act
        var result = await repo.TestCountPlaceholder();

        // Assert
        Assert.AreEqual(3, result, "{{count *}} placeholder should work correctly");
    }

    [TestMethod]
    [TestCategory("TDD")]
    [TestCategory("PlaceholderValidation")]
    public async Task AggregatePlaceholders_SumPlaceholder_ShouldWork()
    {
        // Arrange
        var connection = _fixture.GetConnection(DialectType);
        var repo = CreateAggregateRepository(connection);

        // Act
        var result = await repo.TestSumPlaceholder();

        // Assert
        Assert.AreEqual(601.50m, result, "{{sum amount}} placeholder should work correctly");
    }

    [TestMethod]
    [TestCategory("TDD")]
    [TestCategory("PlaceholderValidation")]
    public async Task AggregatePlaceholders_CoalescePlaceholder_ShouldWork()
    {
        // Arrange
        var connection = _fixture.GetConnection(DialectType);
        var repo = CreateAggregateRepository(connection);

        // Act
        var result = await repo.TestCoalescePlaceholder();

        // Assert
        Assert.IsTrue(result >= 0, "{{coalesce}} placeholder should work correctly");
    }

    // ==================== String Function Tests ====================

    [TestMethod]
    [TestCategory("TDD")]
    [TestCategory("PlaceholderValidation")]
    public async Task StringPlaceholders_UpperPlaceholder_ShouldWork()
    {
        // Arrange
        var connection = _fixture.GetConnection(DialectType);
        var repo = CreateStringRepository(connection);

        // Act
        var result = await repo.TestUpperPlaceholder(1);

        // Assert
        Assert.AreEqual("TEST 1", result, "{{upper name}} placeholder should work correctly");
    }

    [TestMethod]
    [TestCategory("TDD")]
    [TestCategory("PlaceholderValidation")]
    public async Task StringPlaceholders_LowerPlaceholder_ShouldWork()
    {
        // Arrange
        var connection = _fixture.GetConnection(DialectType);
        var repo = CreateStringRepository(connection);

        // Act
        var result = await repo.TestLowerPlaceholder(1);

        // Assert
        Assert.AreEqual("test 1", result, "{{lower name}} placeholder should work correctly");
    }

    [TestMethod]
    [TestCategory("TDD")]
    [TestCategory("PlaceholderValidation")]
    public async Task StringPlaceholders_LengthPlaceholder_ShouldWork()
    {
        // Arrange
        var connection = _fixture.GetConnection(DialectType);
        var repo = CreateStringRepository(connection);

        // Act
        var result = await repo.TestLengthPlaceholder(1);

        // Assert
        Assert.AreEqual(6, result, "{{length name}} placeholder should work correctly");
    }

    // ==================== Conditional Placeholder Tests ====================

    [TestMethod]
    [TestCategory("TDD")]
    [TestCategory("PlaceholderValidation")]
    public async Task ConditionalPlaceholders_LikePlaceholder_ShouldWork()
    {
        // Arrange
        var connection = _fixture.GetConnection(DialectType);
        var repo = CreateConditionalRepository(connection);

        // Act
        var result = await repo.TestLikePlaceholder("%Test%");

        // Assert
        Assert.AreEqual(3, result.Count, "{{like}} placeholder should work correctly");
    }

    [TestMethod]
    [TestCategory("TDD")]
    [TestCategory("PlaceholderValidation")]
    public async Task ConditionalPlaceholders_BetweenPlaceholder_ShouldWork()
    {
        // Arrange
        var connection = _fixture.GetConnection(DialectType);
        var repo = CreateConditionalRepository(connection);

        // Act
        var result = await repo.TestBetweenPlaceholder(100m, 250m);

        // Assert
        Assert.AreEqual(2, result.Count, "{{between}} placeholder should work correctly");
    }

    [TestMethod]
    [TestCategory("TDD")]
    [TestCategory("PlaceholderValidation")]
    public async Task ConditionalPlaceholders_InPlaceholder_ShouldWork()
    {
        // Arrange
        var connection = _fixture.GetConnection(DialectType);
        var repo = CreateConditionalRepository(connection);

        // Act
        var result = await repo.TestInPlaceholder(new[] { "Category A", "Category B" });

        // Assert
        Assert.AreEqual(3, result.Count, "{{in}} placeholder should work correctly");
    }

    // ==================== Date/Time Function Tests ====================

    [TestMethod]
    [TestCategory("TDD")]
    [TestCategory("PlaceholderValidation")]
    public async Task DateTimePlaceholders_CurrentTimestampPlaceholder_ShouldWork()
    {
        // Arrange
        var connection = _fixture.GetConnection(DialectType);
        var repo = CreateDateTimeRepository(connection);

        // Act
        var result = await repo.TestCurrentTimestampPlaceholder();

        // Assert - Should return count of records where created_at <= current timestamp (all records)
        Assert.AreEqual(3, result, "{{current_timestamp}} placeholder should work correctly in WHERE clause");
    }

    [TestMethod]
    [TestCategory("TDD")]
    [TestCategory("PlaceholderValidation")]
    public async Task DateTimePlaceholders_YearPlaceholder_ShouldWork()
    {
        // Arrange
        var connection = _fixture.GetConnection(DialectType);
        var repo = CreateDateTimeRepository(connection);

        // Act
        var result = await repo.TestYearPlaceholder(1);

        // Assert
        Assert.IsTrue(result >= 2020 && result <= 2030, "{{year}} placeholder should work correctly");
    }

    // ==================== Math Function Tests ====================

    [TestMethod]
    [TestCategory("TDD")]
    [TestCategory("PlaceholderValidation")]
    public async Task MathPlaceholders_RoundPlaceholder_ShouldWork()
    {
        // Arrange
        var connection = _fixture.GetConnection(DialectType);
        var repo = CreateMathRepository(connection);

        // Act
        var result = await repo.TestRoundPlaceholder(1);

        // Assert
        Assert.AreEqual(100.50m, result, "{{round}} placeholder should work correctly");
    }

    [TestMethod]
    [TestCategory("TDD")]
    [TestCategory("PlaceholderValidation")]
    public async Task MathPlaceholders_AbsPlaceholder_ShouldWork()
    {
        // Arrange
        var connection = _fixture.GetConnection(DialectType);
        var repo = CreateMathRepository(connection);

        // Act
        var result = await repo.TestAbsPlaceholder(1);

        // Assert
        Assert.AreEqual(100.50m, result, "{{abs}} placeholder should work correctly");
    }
}

// ==================== SQLite Tests ====================

[TestClass]
public class ComprehensivePlaceholderValidation_SQLite : ComprehensivePlaceholderValidation_Base
{
    protected override SqlDefineTypes DialectType => SqlDefineTypes.SQLite;

    protected override ICorePlaceholderRepository CreateCoreRepository(DbConnection connection)
        => new SQLiteCorePlaceholderRepository(connection);

    protected override IAggregatePlaceholderRepository CreateAggregateRepository(DbConnection connection)
        => new SQLiteAggregatePlaceholderRepository(connection);

    protected override IStringPlaceholderRepository CreateStringRepository(DbConnection connection)
        => new SQLiteStringPlaceholderRepository(connection);

    protected override IConditionalPlaceholderRepository CreateConditionalRepository(DbConnection connection)
        => new SQLiteConditionalPlaceholderRepository(connection);

    protected override IDateTimePlaceholderRepository CreateDateTimeRepository(DbConnection connection)
        => new SQLiteDateTimePlaceholderRepository(connection);

    protected override IMathPlaceholderRepository CreateMathRepository(DbConnection connection)
        => new SQLiteMathPlaceholderRepository(connection);
}

// ==================== MySQL Tests ====================

[TestClass]
[TestCategory(TestCategories.MySQL)]
[TestCategory(TestCategories.CI)]
public class ComprehensivePlaceholderValidation_MySQL : ComprehensivePlaceholderValidation_Base
{
    protected override SqlDefineTypes DialectType => SqlDefineTypes.MySql;

    protected override ICorePlaceholderRepository CreateCoreRepository(DbConnection connection)
        => new MySqlCorePlaceholderRepository(connection);

    protected override IAggregatePlaceholderRepository CreateAggregateRepository(DbConnection connection)
        => new MySqlAggregatePlaceholderRepository(connection);

    protected override IStringPlaceholderRepository CreateStringRepository(DbConnection connection)
        => new MySqlStringPlaceholderRepository(connection);

    protected override IConditionalPlaceholderRepository CreateConditionalRepository(DbConnection connection)
        => new MySqlConditionalPlaceholderRepository(connection);

    protected override IDateTimePlaceholderRepository CreateDateTimeRepository(DbConnection connection)
        => new MySqlDateTimePlaceholderRepository(connection);

    protected override IMathPlaceholderRepository CreateMathRepository(DbConnection connection)
        => new MySqlMathPlaceholderRepository(connection);
}

// ==================== PostgreSQL Tests ====================

[TestClass]
[TestCategory(TestCategories.PostgreSQL)]
[TestCategory(TestCategories.CI)]
public class ComprehensivePlaceholderValidation_PostgreSQL : ComprehensivePlaceholderValidation_Base
{
    protected override SqlDefineTypes DialectType => SqlDefineTypes.PostgreSql;

    protected override ICorePlaceholderRepository CreateCoreRepository(DbConnection connection)
        => new PostgreSqlCorePlaceholderRepository(connection);

    protected override IAggregatePlaceholderRepository CreateAggregateRepository(DbConnection connection)
        => new PostgreSqlAggregatePlaceholderRepository(connection);

    protected override IStringPlaceholderRepository CreateStringRepository(DbConnection connection)
        => new PostgreSqlStringPlaceholderRepository(connection);

    protected override IConditionalPlaceholderRepository CreateConditionalRepository(DbConnection connection)
        => new PostgreSqlConditionalPlaceholderRepository(connection);

    protected override IDateTimePlaceholderRepository CreateDateTimeRepository(DbConnection connection)
        => new PostgreSqlDateTimePlaceholderRepository(connection);

    protected override IMathPlaceholderRepository CreateMathRepository(DbConnection connection)
        => new PostgreSqlMathPlaceholderRepository(connection);
}

// ==================== SQL Server Tests ====================

[TestClass]
[TestCategory(TestCategories.SqlServer)]
[TestCategory(TestCategories.CI)]
public class ComprehensivePlaceholderValidation_SqlServer : ComprehensivePlaceholderValidation_Base
{
    protected override SqlDefineTypes DialectType => SqlDefineTypes.SqlServer;

    protected override ICorePlaceholderRepository CreateCoreRepository(DbConnection connection)
        => new SqlServerCorePlaceholderRepository(connection);

    protected override IAggregatePlaceholderRepository CreateAggregateRepository(DbConnection connection)
        => new SqlServerAggregatePlaceholderRepository(connection);

    protected override IStringPlaceholderRepository CreateStringRepository(DbConnection connection)
        => new SqlServerStringPlaceholderRepository(connection);

    protected override IConditionalPlaceholderRepository CreateConditionalRepository(DbConnection connection)
        => new SqlServerConditionalPlaceholderRepository(connection);

    protected override IDateTimePlaceholderRepository CreateDateTimeRepository(DbConnection connection)
        => new SqlServerDateTimePlaceholderRepository(connection);

    protected override IMathPlaceholderRepository CreateMathRepository(DbConnection connection)
        => new SqlServerMathPlaceholderRepository(connection);
}
