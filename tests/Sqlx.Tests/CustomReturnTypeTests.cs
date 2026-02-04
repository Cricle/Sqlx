using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using Sqlx.Annotations;

namespace Sqlx.Tests;

/// <summary>
/// Tests for SqlTemplate with custom return types (non-entity types).
/// Verifies that the generator correctly handles methods returning types different from the repository entity type.
/// </summary>
[TestClass]
public class CustomReturnTypeTests
{
    private SqliteConnection _connection = null!;

    [TestInitialize]
    public void Setup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _connection?.Dispose();
    }

    [TestMethod]
    public void SqlTemplate_WithCustomReturnType_ShouldReturnCorrectType()
    {
        // Arrange
        var repo = new CustomReturnTypeRepository(_connection);

        // Act
        var result = repo.GetCustomType();

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(CustomType));
        Assert.AreEqual(1, result.Id);
        Assert.AreEqual("Test1", result.Name);
    }

    [TestMethod]
    public void SqlTemplate_WithCustomReturnTypeList_ShouldReturnCorrectList()
    {
        // Arrange
        var repo = new CustomReturnTypeRepository(_connection);

        // Act
        var results = repo.GetCustomTypeList();

        // Assert
        Assert.IsNotNull(results);
        Assert.AreEqual(2, results.Count);
        Assert.AreEqual(1, results[0].Id);
        Assert.AreEqual("Test1", results[0].Name);
        Assert.AreEqual(2, results[1].Id);
        Assert.AreEqual("Test2", results[1].Name);
    }

    [TestMethod]
    public async Task SqlTemplate_WithCustomReturnTypeAsync_ShouldReturnCorrectType()
    {
        // Arrange
        var repo = new CustomReturnTypeRepository(_connection);

        // Act
        var result = await repo.GetCustomTypeAsync();

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(CustomType));
        Assert.AreEqual(1, result.Id);
        Assert.AreEqual("Test1", result.Name);
    }

    [TestMethod]
    public async Task SqlTemplate_WithCustomReturnTypeListAsync_ShouldReturnCorrectList()
    {
        // Arrange
        var repo = new CustomReturnTypeRepository(_connection);

        // Act
        var results = await repo.GetCustomTypeListAsync();

        // Assert
        Assert.IsNotNull(results);
        Assert.AreEqual(2, results.Count);
        Assert.AreEqual(1, results[0].Id);
        Assert.AreEqual("Test1", results[0].Name);
        Assert.AreEqual(2, results[1].Id);
        Assert.AreEqual("Test2", results[1].Name);
    }
}

// Test entity (repository entity type)
[Sqlx]
public class CustomTestEntity
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
}

// Custom return type (different from entity type)
[Sqlx]
public class CustomType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

// Repository interface with custom return types
public interface ICustomReturnTypeRepository : ICrudRepository<CustomTestEntity, int>
{
    [SqlTemplate("SELECT 1 AS Id, 'Test1' AS Name")]
    CustomType GetCustomType();

    [SqlTemplate("SELECT 1 AS Id, 'Test1' AS Name UNION ALL SELECT 2, 'Test2'")]
    List<CustomType> GetCustomTypeList();

    [SqlTemplate("SELECT 1 AS Id, 'Test1' AS Name")]
    Task<CustomType> GetCustomTypeAsync();

    [SqlTemplate("SELECT 1 AS Id, 'Test1' AS Name UNION ALL SELECT 2, 'Test2'")]
    Task<List<CustomType>> GetCustomTypeListAsync();
}

// Repository implementation
[TableName("test_entities")]
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(ICustomReturnTypeRepository))]
public partial class CustomReturnTypeRepository(SqliteConnection connection) : ICustomReturnTypeRepository
{
}
