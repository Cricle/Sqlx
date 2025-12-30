using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

namespace Sqlx.Tests.Runtime;

[TestClass]
public class TDD_SQLite_ReturnInsertedId
{
    private IDbConnection _connection = null!;
    private ITestInsertRepository _repo = null!;

    [TestInitialize]
    public void Setup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE test_users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                age INTEGER NOT NULL
            )";
        cmd.ExecuteNonQuery();

        _repo = new TestInsertRepository(_connection);
    }

    [TestCleanup]
    public void TearDown()
    {
        _connection?.Dispose();
    }

    [TestMethod]
    [TestCategory("SQLite")]
    [TestCategory("ReturnInsertedId")]
    public async Task SQLite_InsertAsync_Should_Return_Correct_Id()
    {
        // Act
        var id1 = await _repo.InsertUserAsync("Alice", 25);
        var id2 = await _repo.InsertUserAsync("Bob", 30);
        var id3 = await _repo.InsertUserAsync("Charlie", 35);

        // Assert
        Assert.AreNotEqual(0, id1, "第一次插入应该返回非零ID");
        Assert.AreNotEqual(0, id2, "第二次插入应该返回非零ID");
        Assert.AreNotEqual(0, id3, "第三次插入应该返回非零ID");
        
        Assert.AreEqual(1, id1, "第一次插入应该返回ID 1");
        Assert.AreEqual(2, id2, "第二次插入应该返回ID 2");
        Assert.AreEqual(3, id3, "第三次插入应该返回ID 3");

        // Verify data was actually inserted
        var user1 = await _repo.GetUserByIdAsync(id1);
        Assert.IsNotNull(user1);
        Assert.AreEqual("Alice", user1.Name);
        Assert.AreEqual(25, user1.Age);
    }
}

public class TestUser
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
}

public interface ITestInsertRepository
{
    [SqlTemplate("INSERT INTO test_users (name, age) VALUES (@name, @age)")]
    [ReturnInsertedId]
    Task<long> InsertUserAsync(string name, int age);

    [SqlTemplate("SELECT * FROM test_users WHERE id = @id")]
    Task<TestUser?> GetUserByIdAsync(long id);
}

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(ITestInsertRepository))]
public partial class TestInsertRepository(IDbConnection connection) : ITestInsertRepository { }

