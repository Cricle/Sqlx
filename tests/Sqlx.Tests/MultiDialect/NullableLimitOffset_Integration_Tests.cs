// -----------------------------------------------------------------------
// <copyright file="NullableLimitOffset_Integration_Tests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

namespace Sqlx.Tests.MultiDialect;

// ==================== 测试实体 ====================

public class PaginationUser
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public int Score { get; set; }
}

// ==================== 分页仓储接口 - 测试所有 LIMIT/OFFSET 边界 ====================

/// <summary>
/// 分页仓储接口 - 覆盖所有 nullable LIMIT/OFFSET 边界场景
/// </summary>
public partial interface IPaginationUserRepository
{
    // ==================== 基础 CRUD ====================

    [SqlTemplate("INSERT INTO {{table}} (name, score) VALUES (@name, @score)")]
    [ReturnInsertedId]
    Task<long> InsertAsync(string name, int score, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM {{table}} ORDER BY id")]
    Task<List<PaginationUser>> GetAllAsync(CancellationToken ct = default);

    [SqlTemplate("DELETE FROM {{table}}")]
    Task<int> DeleteAllAsync(CancellationToken ct = default);

    // ==================== 可空 LIMIT 参数 ====================

    /// <summary>可空 limit 参数 - 核心测试场景</summary>
    [SqlTemplate("SELECT {{columns}} FROM {{table}} ORDER BY id {{limit}}")]
    Task<List<PaginationUser>> GetWithNullableLimitAsync(int? limit = null, CancellationToken ct = default);

    /// <summary>可空 limit 带默认值</summary>
    [SqlTemplate("SELECT {{columns}} FROM {{table}} ORDER BY id {{limit}}")]
    Task<List<PaginationUser>> GetWithNullableLimitDefaultAsync(int? limit = 5, CancellationToken ct = default);

    // ==================== 非可空 LIMIT 参数 ====================

    /// <summary>非可空 limit 参数</summary>
    [SqlTemplate("SELECT {{columns}} FROM {{table}} ORDER BY id {{limit}}")]
    Task<List<PaginationUser>> GetWithNonNullableLimitAsync(int limit, CancellationToken ct = default);

    // ==================== 可空 OFFSET 参数 (需要配合 LIMIT 使用) ====================
    // 注意：SQLite/MySQL/PostgreSQL 的 OFFSET 必须配合 LIMIT 使用
    // 单独的 OFFSET 只在 SQL Server 中有效

    /// <summary>可空 offset 参数 - 配合大 limit 使用以模拟只跳过</summary>
    [SqlTemplate("SELECT {{columns}} FROM {{table}} ORDER BY id LIMIT 1000000 {{offset}}")]
    Task<List<PaginationUser>> GetWithNullableOffsetAsync(int? offset = null, CancellationToken ct = default);

    // ==================== LIMIT + OFFSET 组合 ====================

    /// <summary>可空 limit + 可空 offset</summary>
    [SqlTemplate("SELECT {{columns}} FROM {{table}} ORDER BY id {{limit}} {{offset}}")]
    Task<List<PaginationUser>> GetPagedNullableAsync(int? limit = null, int? offset = null, CancellationToken ct = default);

    /// <summary>非可空 limit + 非可空 offset</summary>
    [SqlTemplate("SELECT {{columns}} FROM {{table}} ORDER BY id {{limit}} {{offset}}")]
    Task<List<PaginationUser>> GetPagedNonNullableAsync(int limit, int offset, CancellationToken ct = default);

    /// <summary>可空 limit + 非可空 offset (混合) - 需要默认 limit 以支持 SQLite</summary>
    [SqlTemplate("SELECT {{columns}} FROM {{table}} ORDER BY id {{limit}} {{offset}}")]
    Task<List<PaginationUser>> GetPagedMixedAsync(int? limit = 1000000, int offset = 0, CancellationToken ct = default);

    // ==================== 预定义模式 ====================

    /// <summary>预定义模式: tiny (5条)</summary>
    [SqlTemplate("SELECT {{columns}} FROM {{table}} ORDER BY id {{limit:tiny}}")]
    Task<List<PaginationUser>> GetTinyAsync(CancellationToken ct = default);

    /// <summary>预定义模式: small (10条)</summary>
    [SqlTemplate("SELECT {{columns}} FROM {{table}} ORDER BY id {{limit:small}}")]
    Task<List<PaginationUser>> GetSmallAsync(CancellationToken ct = default);

    /// <summary>预定义模式: medium (50条)</summary>
    [SqlTemplate("SELECT {{columns}} FROM {{table}} ORDER BY id {{limit:medium}}")]
    Task<List<PaginationUser>> GetMediumAsync(CancellationToken ct = default);

    /// <summary>预定义模式: large (100条)</summary>
    [SqlTemplate("SELECT {{columns}} FROM {{table}} ORDER BY id {{limit:large}}")]
    Task<List<PaginationUser>> GetLargeAsync(CancellationToken ct = default);

    /// <summary>预定义模式: page (20条)</summary>
    [SqlTemplate("SELECT {{columns}} FROM {{table}} ORDER BY id {{limit:page}}")]
    Task<List<PaginationUser>> GetPageAsync(CancellationToken ct = default);

    // ==================== 带 WHERE 条件的分页 ====================

    /// <summary>带条件的可空分页</summary>
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE score > @minScore ORDER BY id {{limit}} {{offset}}")]
    Task<List<PaginationUser>> GetByScorePagedAsync(int minScore, int? limit = null, int? offset = null, CancellationToken ct = default);
}

// ==================== 各数据库实现 ====================

[RepositoryFor(typeof(IPaginationUserRepository), Dialect = SqlDefineTypes.SQLite, TableName = "pagination_users_sq")]
public partial class SQLitePaginationRepository : IPaginationUserRepository
{
    private readonly DbConnection _connection;
    public SQLitePaginationRepository(DbConnection connection) => _connection = connection;
}

[RepositoryFor(typeof(IPaginationUserRepository), Dialect = SqlDefineTypes.MySql, TableName = "pagination_users_my")]
public partial class MySQLPaginationRepository : IPaginationUserRepository
{
    private readonly DbConnection _connection;
    public MySQLPaginationRepository(DbConnection connection) => _connection = connection;
}

[RepositoryFor(typeof(IPaginationUserRepository), Dialect = SqlDefineTypes.PostgreSql, TableName = "pagination_users_pg")]
public partial class PostgreSQLPaginationRepository : IPaginationUserRepository
{
    private readonly DbConnection _connection;
    public PostgreSQLPaginationRepository(DbConnection connection) => _connection = connection;
}

[RepositoryFor(typeof(IPaginationUserRepository), Dialect = SqlDefineTypes.SqlServer, TableName = "pagination_users_ss")]
public partial class SqlServerPaginationRepository : IPaginationUserRepository
{
    private readonly DbConnection _connection;
    public SqlServerPaginationRepository(DbConnection connection) => _connection = connection;
}

// ==================== 测试基类 ====================

public abstract class NullableLimitOffsetTestBase
{
    protected DbConnection? Connection;
    protected IPaginationUserRepository? Repository;
    protected abstract string TableName { get; }
    protected abstract SqlDefineTypes DialectType { get; }

    protected abstract DbConnection? CreateConnection();
    protected abstract IPaginationUserRepository CreateRepository(DbConnection connection);

    [TestInitialize]
    public async Task Initialize()
    {
        Connection = CreateConnection();
        if (Connection == null)
        {
            Assert.Inconclusive($"{DialectType} 数据库连接不可用");
            return;
        }

        if (Connection.State != System.Data.ConnectionState.Open)
            await Connection.OpenAsync();

        Repository = CreateRepository(Connection);
        await CreateTableAsync();
        await SeedTestDataAsync();
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        if (Connection != null)
            await Connection.DisposeAsync();
    }

    protected async Task CreateTableAsync()
    {
        using var cmd = Connection!.CreateCommand();
        
        // 先删除表
        cmd.CommandText = DialectType == SqlDefineTypes.SqlServer
            ? $"IF OBJECT_ID(N'{TableName}', N'U') IS NOT NULL DROP TABLE {TableName}"
            : $"DROP TABLE IF EXISTS {TableName}";
        await cmd.ExecuteNonQueryAsync();

        // 创建表
        cmd.CommandText = DialectType switch
        {
            SqlDefineTypes.SQLite => $"CREATE TABLE {TableName} (id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT NOT NULL, score INTEGER NOT NULL)",
            SqlDefineTypes.MySql => $"CREATE TABLE {TableName} (id BIGINT AUTO_INCREMENT PRIMARY KEY, name VARCHAR(255) NOT NULL, score INT NOT NULL)",
            SqlDefineTypes.PostgreSql => $"CREATE TABLE {TableName} (id BIGSERIAL PRIMARY KEY, name TEXT NOT NULL, score INTEGER NOT NULL)",
            SqlDefineTypes.SqlServer => $"CREATE TABLE {TableName} (id BIGINT IDENTITY(1,1) PRIMARY KEY, name NVARCHAR(255) NOT NULL, score INT NOT NULL)",
            _ => throw new NotSupportedException()
        };
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>插入测试数据: 15条记录 (User1-User15, Score 10-150)</summary>
    protected async Task SeedTestDataAsync()
    {
        for (int i = 1; i <= 15; i++)
        {
            await Repository!.InsertAsync($"User{i}", i * 10);
        }
    }

    // ==================== 核心测试: 可空 LIMIT ====================

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task NullableLimit_WithNull_ShouldReturnAll()
    {
        var result = await Repository!.GetWithNullableLimitAsync(null);
        Assert.AreEqual(15, result.Count, $"[{DialectType}] limit=null 应返回所有15条记录");
    }

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task NullableLimit_WithValue_ShouldReturnLimited()
    {
        var result = await Repository!.GetWithNullableLimitAsync(5);
        Assert.AreEqual(5, result.Count, $"[{DialectType}] limit=5 应返回5条记录");
        Assert.AreEqual("User1", result[0].Name);
        Assert.AreEqual("User5", result[4].Name);
    }

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task NullableLimit_WithZero_ShouldReturnEmpty()
    {
        var result = await Repository!.GetWithNullableLimitAsync(0);
        Assert.AreEqual(0, result.Count, $"[{DialectType}] limit=0 应返回0条记录");
    }

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task NullableLimit_WithOne_ShouldReturnOne()
    {
        var result = await Repository!.GetWithNullableLimitAsync(1);
        Assert.AreEqual(1, result.Count, $"[{DialectType}] limit=1 应返回1条记录");
        Assert.AreEqual("User1", result[0].Name);
    }

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task NullableLimit_ExceedsTotal_ShouldReturnAll()
    {
        var result = await Repository!.GetWithNullableLimitAsync(100);
        Assert.AreEqual(15, result.Count, $"[{DialectType}] limit=100 超过总数应返回所有15条");
    }

    // ==================== 非可空 LIMIT ====================

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task NonNullableLimit_ShouldWork()
    {
        var result = await Repository!.GetWithNonNullableLimitAsync(3);
        Assert.AreEqual(3, result.Count, $"[{DialectType}] 非可空 limit=3 应返回3条记录");
    }

    // ==================== 可空 OFFSET ====================

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task NullableOffset_WithNull_ShouldReturnFromStart()
    {
        var result = await Repository!.GetWithNullableOffsetAsync(null);
        Assert.AreEqual(15, result.Count, $"[{DialectType}] offset=null 应返回所有记录");
        Assert.AreEqual("User1", result[0].Name);
    }

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task NullableOffset_WithValue_ShouldSkip()
    {
        var result = await Repository!.GetWithNullableOffsetAsync(5);
        Assert.AreEqual(10, result.Count, $"[{DialectType}] offset=5 应跳过5条返回10条");
        Assert.AreEqual("User6", result[0].Name);
    }

    // ==================== LIMIT + OFFSET 组合 ====================

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task PagedNullable_BothNull_ShouldReturnAll()
    {
        var result = await Repository!.GetPagedNullableAsync(null, null);
        Assert.AreEqual(15, result.Count, $"[{DialectType}] limit=null, offset=null 应返回所有");
    }

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task PagedNullable_LimitOnly_ShouldWork()
    {
        var result = await Repository!.GetPagedNullableAsync(5, null);
        Assert.AreEqual(5, result.Count, $"[{DialectType}] limit=5, offset=null 应返回前5条");
        Assert.AreEqual("User1", result[0].Name);
    }

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task PagedNullable_OffsetOnly_ShouldWork()
    {
        var result = await Repository!.GetPagedNullableAsync(null, 10);
        Assert.AreEqual(5, result.Count, $"[{DialectType}] limit=null, offset=10 应跳过10条返回5条");
        Assert.AreEqual("User11", result[0].Name);
    }

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task PagedNullable_BothSet_ShouldWork()
    {
        var result = await Repository!.GetPagedNullableAsync(3, 5);
        Assert.AreEqual(3, result.Count, $"[{DialectType}] limit=3, offset=5 应返回第6-8条");
        Assert.AreEqual("User6", result[0].Name);
        Assert.AreEqual("User8", result[2].Name);
    }

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task PagedNonNullable_ShouldWork()
    {
        var result = await Repository!.GetPagedNonNullableAsync(4, 2);
        Assert.AreEqual(4, result.Count, $"[{DialectType}] 非可空 limit=4, offset=2 应返回第3-6条");
        Assert.AreEqual("User3", result[0].Name);
    }

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task PagedMixed_WithDefaultLimit_ShouldWork()
    {
        // limit 默认值是 1000000，offset=3，应该跳过3条返回剩余12条
        var result = await Repository!.GetPagedMixedAsync(null, 3);
        Assert.AreEqual(12, result.Count, $"[{DialectType}] 混合: limit=default(1000000), offset=3 应跳过3条返回12条");
        Assert.AreEqual("User4", result[0].Name);
    }

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task PagedMixed_WithExplicitLimit_ShouldWork()
    {
        var result = await Repository!.GetPagedMixedAsync(5, 3);
        Assert.AreEqual(5, result.Count, $"[{DialectType}] 混合: limit=5, offset=3 应返回5条");
        Assert.AreEqual("User4", result[0].Name);
    }

    // ==================== 预定义模式 ====================

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task PredefinedMode_Tiny_ShouldReturn5()
    {
        var result = await Repository!.GetTinyAsync();
        Assert.AreEqual(5, result.Count, $"[{DialectType}] tiny 模式应返回5条");
    }

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task PredefinedMode_Small_ShouldReturn10()
    {
        var result = await Repository!.GetSmallAsync();
        Assert.AreEqual(10, result.Count, $"[{DialectType}] small 模式应返回10条");
    }

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task PredefinedMode_Page_ShouldReturn15()
    {
        // page=20, 但只有15条数据
        var result = await Repository!.GetPageAsync();
        Assert.AreEqual(15, result.Count, $"[{DialectType}] page 模式应返回所有15条(不足20)");
    }

    // ==================== 带条件的分页 ====================

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task ConditionPaged_WithNullLimitOffset_ShouldWork()
    {
        // score > 50 的有 User6-User15 (10条)
        var result = await Repository!.GetByScorePagedAsync(50, null, null);
        Assert.AreEqual(10, result.Count, $"[{DialectType}] score>50, 无分页应返回10条");
    }

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task ConditionPaged_WithLimitOffset_ShouldWork()
    {
        // score > 50 的有 User6-User15, 取第3-5条 (offset=2, limit=3)
        var result = await Repository!.GetByScorePagedAsync(50, 3, 2);
        Assert.AreEqual(3, result.Count, $"[{DialectType}] score>50, limit=3, offset=2 应返回3条");
        Assert.AreEqual("User8", result[0].Name); // 第3条 (0-indexed: User6, User7, User8)
    }

    // ==================== 边界测试 ====================

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task Pagination_FirstPage_ShouldWork()
    {
        var result = await Repository!.GetPagedNullableAsync(5, 0);
        Assert.AreEqual(5, result.Count);
        Assert.AreEqual("User1", result[0].Name);
        Assert.AreEqual("User5", result[4].Name);
    }

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task Pagination_MiddlePage_ShouldWork()
    {
        var result = await Repository!.GetPagedNullableAsync(5, 5);
        Assert.AreEqual(5, result.Count);
        Assert.AreEqual("User6", result[0].Name);
        Assert.AreEqual("User10", result[4].Name);
    }

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task Pagination_LastPage_ShouldWork()
    {
        var result = await Repository!.GetPagedNullableAsync(5, 10);
        Assert.AreEqual(5, result.Count);
        Assert.AreEqual("User11", result[0].Name);
        Assert.AreEqual("User15", result[4].Name);
    }

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task Pagination_PartialLastPage_ShouldWork()
    {
        var result = await Repository!.GetPagedNullableAsync(5, 12);
        Assert.AreEqual(3, result.Count, "最后一页只有3条");
        Assert.AreEqual("User13", result[0].Name);
        Assert.AreEqual("User15", result[2].Name);
    }

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task Pagination_BeyondEnd_ShouldReturnEmpty()
    {
        var result = await Repository!.GetPagedNullableAsync(5, 20);
        Assert.AreEqual(0, result.Count, "超出范围应返回空");
    }

    // ==================== 顺序验证 ====================

    [TestMethod]
    [TestCategory(TestCategories.Integration)]
    public async Task Pagination_OrderShouldBeConsistent()
    {
        // 验证分页顺序一致性
        var page1 = await Repository!.GetPagedNullableAsync(5, 0);
        var page2 = await Repository!.GetPagedNullableAsync(5, 5);
        var page3 = await Repository!.GetPagedNullableAsync(5, 10);

        var all = page1.Concat(page2).Concat(page3).ToList();
        Assert.AreEqual(15, all.Count);

        for (int i = 0; i < 15; i++)
        {
            Assert.AreEqual($"User{i + 1}", all[i].Name, $"第{i + 1}条记录顺序不正确");
        }
    }
}


// ==================== SQLite 测试 ====================

[TestClass]
public class NullableLimitOffset_SQLite_Tests : NullableLimitOffsetTestBase
{
    protected override string TableName => "pagination_users_sq";
    protected override SqlDefineTypes DialectType => SqlDefineTypes.SQLite;

    protected override DbConnection? CreateConnection()
    {
        var conn = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
        return conn;
    }

    protected override IPaginationUserRepository CreateRepository(DbConnection connection)
        => new SQLitePaginationRepository(connection);
}

// ==================== MySQL 测试 ====================

[TestClass]
public class NullableLimitOffset_MySQL_Tests : NullableLimitOffsetTestBase
{
    protected override string TableName => "pagination_users_my";
    protected override SqlDefineTypes DialectType => SqlDefineTypes.MySql;

    protected override DbConnection? CreateConnection()
    {
        try
        {
            var conn = new MySqlConnector.MySqlConnection(
                "Server=localhost;Port=3307;Database=sqlx_test;User=root;Password=root;AllowUserVariables=true");
            return conn;
        }
        catch
        {
            return null;
        }
    }

    protected override IPaginationUserRepository CreateRepository(DbConnection connection)
        => new MySQLPaginationRepository(connection);
}

