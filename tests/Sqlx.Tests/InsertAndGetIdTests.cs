using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using Sqlx.Annotations;
using System.ComponentModel.DataAnnotations;
using System.Data.Common;

namespace Sqlx.Tests;

#region Test Entity and Repository

[Sqlx]
[TableName("test_entities")]
public partial class InsertTestEntity
{
    [Key]
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
}

public interface IInsertTestEntityRepository : ICrudRepository<InsertTestEntity, long>
{
    // Inherited from ICrudRepository: all query and command methods
}

[TableName("test_entities")]
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IInsertTestEntityRepository))]
public partial class InsertTestEntityRepository(SqliteConnection connection) : IInsertTestEntityRepository
{
    private readonly SqliteConnection _connection = connection;
    public DbTransaction? Transaction { get; set; }
}

#endregion

/// <summary>
/// Integration tests for InsertAndGetIdAsync functionality.
/// </summary>
[TestClass]
public class InsertAndGetIdTests
{
    private SqliteConnection _connection = null!;

    [TestInitialize]
    public void Setup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE test_entities (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                value INTEGER NOT NULL
            )";
        cmd.ExecuteNonQuery();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _connection?.Dispose();
    }

    #region Basic Insert Tests

    [TestMethod]
    public async Task InsertAndGetIdAsync_SingleInsert_ReturnsCorrectId()
    {
        var repo = new InsertTestEntityRepository(_connection);

        var id = await repo.InsertAndGetIdAsync(new InsertTestEntity { Name = "Test1", Value = 100 }, default);

        Assert.AreEqual(1, id);
    }

    [TestMethod]
    public async Task InsertAndGetIdAsync_MultipleInserts_ReturnsIncrementingIds()
    {
        var repo = new InsertTestEntityRepository(_connection);

        var id1 = await repo.InsertAndGetIdAsync(new InsertTestEntity { Name = "Test1", Value = 100 }, default);
        var id2 = await repo.InsertAndGetIdAsync(new InsertTestEntity { Name = "Test2", Value = 200 }, default);
        var id3 = await repo.InsertAndGetIdAsync(new InsertTestEntity { Name = "Test3", Value = 300 }, default);

        Assert.AreEqual(1, id1);
        Assert.AreEqual(2, id2);
        Assert.AreEqual(3, id3);
    }

    [TestMethod]
    public async Task InsertAndGetIdAsync_CanRetrieveInsertedEntity()
    {
        var repo = new InsertTestEntityRepository(_connection);

        var id = await repo.InsertAndGetIdAsync(new InsertTestEntity { Name = "TestEntity", Value = 42 }, default);
        var entity = await repo.GetByIdAsync(id, default);

        Assert.IsNotNull(entity);
        Assert.AreEqual("TestEntity", entity.Name);
        Assert.AreEqual(42, entity.Value);
    }

    #endregion

    #region Concurrent Insert Tests

    [TestMethod]
    public async Task InsertAndGetIdAsync_ConcurrentInserts_AllGetUniqueIds()
    {
        var repo = new InsertTestEntityRepository(_connection);
        var tasks = new List<Task<long>>();

        // Insert 10 entities concurrently
        for (int i = 0; i < 10; i++)
        {
            var index = i;
            tasks.Add(repo.InsertAndGetIdAsync(new InsertTestEntity { Name = $"Test{index}", Value = index }, default));
        }

        var ids = await Task.WhenAll(tasks);

        // All IDs should be unique
        Assert.AreEqual(10, ids.Distinct().Count());
        
        // All IDs should be between 1 and 10
        Assert.IsTrue(ids.All(id => id >= 1 && id <= 10));
    }

    #endregion

    #region Data Integrity Tests

    [TestMethod]
    public async Task InsertAndGetIdAsync_DataIntegrity_AllFieldsStoredCorrectly()
    {
        var repo = new InsertTestEntityRepository(_connection);

        var originalEntity = new InsertTestEntity { Name = "IntegrityTest", Value = 999 };
        var id = await repo.InsertAndGetIdAsync(originalEntity, default);

        // Verify using raw SQL
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT name, value FROM test_entities WHERE id = @id";
        var param = cmd.CreateParameter();
        param.ParameterName = "@id";
        param.Value = id;
        cmd.Parameters.Add(param);

        using var reader = await cmd.ExecuteReaderAsync();
        Assert.IsTrue(await reader.ReadAsync());
        Assert.AreEqual("IntegrityTest", reader.GetString(0));
        Assert.AreEqual(999, reader.GetInt32(1));
    }

    #endregion
}

