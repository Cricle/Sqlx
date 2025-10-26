using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

namespace Sqlx.Tests.Runtime;

/// <summary>
/// SQL列映射验证测试
/// 目的：确保SQL列和实体属性正确映射，测试边界情况
/// </summary>
[TestClass]
[TestCategory("Runtime")]
[TestCategory("ColumnMapping")]
public class TDD_ColumnMapping_Runtime
{
    private IDbConnection _connection = null!;
    private IColumnMappingRepository _repo = null!;

    [TestInitialize]
    public void Setup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        
        ExecuteSql(@"CREATE TABLE full_entity (
            id INTEGER PRIMARY KEY,
            name TEXT NOT NULL,
            email TEXT,
            age INTEGER,
            balance DECIMAL(10,2),
            created_at TEXT
        )");
        
        ExecuteSql(@"INSERT INTO full_entity VALUES 
            (1, 'Alice', 'alice@test.com', 25, 1000.50, '2025-01-01'),
            (2, 'Bob', 'bob@test.com', 30, 2500.00, '2025-01-02')");
        
        _repo = new ColumnMappingRepository(_connection);
    }

    [TestCleanup]
    public void TearDown()
    {
        _connection?.Dispose();
    }

    private void ExecuteSql(string sql)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    #region 完整列映射测试

    [TestMethod]
    public async Task Select_AllColumns_ShouldMapCorrectly()
    {
        // Act
        var results = await _repo.GetFullEntitiesAsync();

        // Assert
        Assert.AreEqual(2, results.Count);
        var alice = results[0];
        Assert.AreEqual(1L, alice.Id);
        Assert.AreEqual("Alice", alice.Name);
        Assert.AreEqual("alice@test.com", alice.Email);
        Assert.AreEqual(25, alice.Age);
        Assert.AreEqual(1000.50m, alice.Balance, 0.01m);
        Assert.IsNotNull(alice.CreatedAt);
    }

    #endregion

    #region 部分列映射测试

    [TestMethod]
    public async Task Select_PartialColumns_ShouldMapMatchingProperties()
    {
        // Act
        var results = await _repo.GetPartialEntitiesAsync();

        // Assert
        Assert.AreEqual(2, results.Count);
        var alice = results[0];
        Assert.AreEqual(1L, alice.Id);
        Assert.AreEqual("Alice", alice.Name);
        Assert.AreEqual(25, alice.Age);
        
        // 未选择的列应该是默认值
        Assert.AreEqual(null, alice.Email);
        Assert.AreEqual(0m, alice.Balance);
    }

    #endregion

    #region 列别名测试

    [TestMethod]
    public async Task Select_WithAliases_ShouldMapByAliasName()
    {
        // Act
        var results = await _repo.GetEntitiesWithAliasesAsync();

        // Assert
        Assert.AreEqual(2, results.Count);
        var alice = results[0];
        Assert.AreEqual(1L, alice.UserId);
        Assert.AreEqual("Alice", alice.UserName);
        Assert.AreEqual("alice@test.com", alice.UserEmail);
    }

    #endregion

    #region Nullable类型测试

    [TestMethod]
    public async Task Select_NullableColumns_ShouldHandleNulls()
    {
        // Arrange: 插入带NULL值的数据
        ExecuteSql("INSERT INTO full_entity (id, name, email, age, balance) VALUES (3, 'Charlie', NULL, NULL, NULL)");

        // Act
        var results = await _repo.GetNullableEntitiesAsync();

        // Assert
        var charlie = results.Find(e => e.Name == "Charlie");
        Assert.IsNotNull(charlie);
        Assert.IsNull(charlie.Email);
        Assert.IsNull(charlie.Age);
        Assert.IsNull(charlie.Balance);
    }

    #endregion

    #region 类型转换测试

    [TestMethod]
    public async Task Select_IntToLong_ShouldConvert()
    {
        // Act
        var results = await _repo.GetFullEntitiesAsync();

        // Assert
        Assert.AreEqual(1L, results[0].Id);
        Assert.AreEqual(typeof(long), results[0].Id.GetType());
    }

    [TestMethod]
    public async Task Select_TextToString_ShouldMapCorrectly()
    {
        // Act
        var results = await _repo.GetFullEntitiesAsync();

        // Assert
        Assert.IsInstanceOfType(results[0].Name, typeof(string));
        Assert.AreEqual("Alice", results[0].Name);
    }

    [TestMethod]
    public async Task Select_DecimalColumn_ShouldPreservePrecision()
    {
        // Act
        var results = await _repo.GetFullEntitiesAsync();

        // Assert
        Assert.AreEqual(1000.50m, results[0].Balance, 0.001m);
        Assert.AreEqual(2500.00m, results[1].Balance, 0.001m);
    }

    #endregion

    #region 大小写不敏感测试

    [TestMethod]
    public async Task Select_MixedCase_ShouldMapCaseInsensitive()
    {
        // Act - SQL列名是小写，属性名是PascalCase
        var results = await _repo.GetFullEntitiesAsync();

        // Assert
        Assert.IsNotNull(results[0].Name); // 'name' -> Name
        Assert.IsNotNull(results[0].Email); // 'email' -> Email
    }

    #endregion

    #region Snake_case到PascalCase映射

    [TestMethod]
    public async Task Select_SnakeCase_ShouldMapToPascalCase()
    {
        // Act - created_at -> CreatedAt
        var results = await _repo.GetFullEntitiesAsync();

        // Assert
        Assert.IsNotNull(results[0].CreatedAt);
    }

    #endregion

    #region 空结果集测试

    [TestMethod]
    public async Task Select_EmptyResult_ShouldReturnEmptyList()
    {
        // Act
        var results = await _repo.GetEntitiesWithNoMatchAsync();

        // Assert
        Assert.IsNotNull(results);
        Assert.AreEqual(0, results.Count);
    }

    #endregion

    #region 单行结果测试

    [TestMethod]
    public async Task SelectSingle_ExistingRecord_ShouldReturnEntity()
    {
        // Act
        var result = await _repo.GetEntityByIdAsync(1);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("Alice", result.Name);
    }

    [TestMethod]
    public async Task SelectSingle_NonExisting_ShouldReturnNull()
    {
        // Act
        var result = await _repo.GetEntityByIdAsync(999);

        // Assert
        Assert.IsNull(result);
    }

    #endregion
}

#region Test Models

public class FullEntity
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string? Email { get; set; }
    public int Age { get; set; }
    public decimal Balance { get; set; }
    public string? CreatedAt { get; set; }
}

public class PartialEntity
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string? Email { get; set; }
    public int Age { get; set; }
    public decimal Balance { get; set; }
}

public class EntityWithAliases
{
    public long UserId { get; set; }
    public string UserName { get; set; } = "";
    public string? UserEmail { get; set; }
}

public class NullableEntity
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string? Email { get; set; }
    public int? Age { get; set; }
    public decimal? Balance { get; set; }
}

#endregion

#region Test Repository

public interface IColumnMappingRepository
{
    [SqlTemplate("SELECT id, name, email, age, balance, created_at FROM full_entity")]
    Task<List<FullEntity>> GetFullEntitiesAsync();

    [SqlTemplate("SELECT id, name, age FROM full_entity")]
    Task<List<PartialEntity>> GetPartialEntitiesAsync();

    [SqlTemplate("SELECT id as UserId, name as UserName, email as UserEmail FROM full_entity")]
    Task<List<EntityWithAliases>> GetEntitiesWithAliasesAsync();

    [SqlTemplate("SELECT id, name, email, age, balance FROM full_entity")]
    Task<List<NullableEntity>> GetNullableEntitiesAsync();

    [SqlTemplate("SELECT id, name, email, age, balance, created_at FROM full_entity WHERE id = @id")]
    Task<FullEntity?> GetEntityByIdAsync(long id);

    [SqlTemplate("SELECT id, name, email, age, balance, created_at FROM full_entity WHERE id > 1000")]
    Task<List<FullEntity>> GetEntitiesWithNoMatchAsync();
}

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IColumnMappingRepository))]
public partial class ColumnMappingRepository(IDbConnection connection) : IColumnMappingRepository { }

#endregion

