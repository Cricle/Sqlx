using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using Sqlx.Annotations;
using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using System.Threading.Tasks;

namespace Sqlx.Tests;

#region Test Entities

[Sqlx]
[TableName("primary_test")]
public partial class PrimaryTestEntity
{
    [Key]
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

#endregion

#region Repository with explicit fields (current working pattern)

public interface IPrimaryTestRepositoryWithFields : ICrudRepository<PrimaryTestEntity, long>
{
}

[TableName("primary_test")]
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IPrimaryTestRepositoryWithFields))]
public partial class PrimaryTestRepositoryWithFields(SqliteConnection connection) : IPrimaryTestRepositoryWithFields
{
    private readonly SqliteConnection _connection = connection;
    public DbTransaction? Transaction { get; set; }
    
    public IQueryable<PrimaryTestEntity> AsQueryable()
    {
        return SqlQuery<PrimaryTestEntity>.For(_staticContext.Dialect).WithConnection(_connection);
    }
}

#endregion

#region Repository without explicit fields (generator auto-generates them)

public interface IPrimaryTestRepositoryNoFields : ICrudRepository<PrimaryTestEntity, long>
{
}

[TableName("primary_test")]
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IPrimaryTestRepositoryNoFields))]
public partial class PrimaryTestRepositoryNoFields(SqliteConnection connection) : IPrimaryTestRepositoryNoFields
{
    // No explicit fields or properties - generator should auto-generate:
    // - private readonly SqliteConnection _connection = connection;
    // - public DbTransaction? Transaction { get; set; }
    
    public IQueryable<PrimaryTestEntity> AsQueryable()
    {
        return SqlQuery<PrimaryTestEntity>.For(_staticContext.Dialect).WithConnection(connection);
    }
}

#endregion

/// <summary>
/// Tests for primary constructor parameter handling in repository generation.
/// </summary>
[TestClass]
public class PrimaryConstructorTests
{
    private SqliteConnection _connection = null!;

    [TestInitialize]
    public void Setup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE primary_test (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL
            )";
        cmd.ExecuteNonQuery();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _connection?.Dispose();
    }

    [TestMethod]
    public async Task WithExplicitFields_ShouldWork()
    {
        var repo = new PrimaryTestRepositoryWithFields(_connection);

        var id = await repo.InsertAndGetIdAsync(new PrimaryTestEntity { Name = "Test1" }, default);
        Assert.AreEqual(1, id);

        var entity = await repo.GetByIdAsync(id, default);
        Assert.IsNotNull(entity);
        Assert.AreEqual("Test1", entity.Name);
    }

    [TestMethod]
    public async Task WithoutExplicitFields_ShouldWork()
    {
        var repo = new PrimaryTestRepositoryNoFields(_connection);

        var id = await repo.InsertAndGetIdAsync(new PrimaryTestEntity { Name = "Test2" }, default);
        Assert.AreEqual(1, id);

        var entity = await repo.GetByIdAsync(id, default);
        Assert.IsNotNull(entity);
        Assert.AreEqual("Test2", entity.Name);
    }

    [TestMethod]
    public async Task WithoutExplicitFields_TransactionShouldWork()
    {
        var repo = new PrimaryTestRepositoryNoFields(_connection);

        using var transaction = _connection.BeginTransaction();
        repo.Transaction = transaction;

        var id = await repo.InsertAndGetIdAsync(new PrimaryTestEntity { Name = "Test3" }, default);
        Assert.AreEqual(1, id);

        // Commit instead of rollback to verify the insert worked
        transaction.Commit();

        // Create a new repo instance without transaction to verify
        var repoForRead = new PrimaryTestRepositoryNoFields(_connection);
        var entity = await repoForRead.GetByIdAsync(id, default);
        Assert.IsNotNull(entity);
        Assert.AreEqual("Test3", entity.Name);
    }
}
