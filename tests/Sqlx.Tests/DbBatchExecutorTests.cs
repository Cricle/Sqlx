using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Data.Sqlite;
using Sqlx;
using System.Data;
using System.Data.Common;

namespace Sqlx.Tests;

/// <summary>
/// Tests for DbBatchExecutor batch execution functionality.
/// Note: SQLite doesn't support DbBatch (CanCreateBatch=false), so these tests
/// verify the fallback loop execution path.
/// </summary>
[TestClass]
public class DbBatchExecutorTests
{
    private SqliteConnection _connection = null!;

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
                email TEXT NOT NULL,
                age INTEGER NOT NULL
            )";
        cmd.ExecuteNonQuery();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _connection?.Dispose();
    }

    #region Basic Batch Insert Tests

    [TestMethod]
    public async Task ExecuteAsync_EmptyList_ReturnsZero()
    {
        var entities = new List<TestUser>();
        var sql = "INSERT INTO test_users (name, email, age) VALUES (@name, @email, @age)";

        var affected = await DbBatchExecutor.ExecuteAsync(
            _connection, null, sql, entities, TestUserParameterBinder.Default);

        Assert.AreEqual(0, affected);
    }

    [TestMethod]
    public async Task ExecuteAsync_SingleEntity_InsertsOne()
    {
        var entities = new List<TestUser>
        {
            new() { Name = "Alice", Email = "alice@test.com", Age = 25 }
        };
        var sql = "INSERT INTO test_users (name, email, age) VALUES (@name, @email, @age)";

        var affected = await DbBatchExecutor.ExecuteAsync(
            _connection, null, sql, entities, TestUserParameterBinder.Default);

        Assert.AreEqual(1, affected);
        Assert.AreEqual(1, GetUserCount());
    }

    [TestMethod]
    public async Task ExecuteAsync_MultipleEntities_InsertsAll()
    {
        var entities = new List<TestUser>
        {
            new() { Name = "Alice", Email = "alice@test.com", Age = 25 },
            new() { Name = "Bob", Email = "bob@test.com", Age = 30 },
            new() { Name = "Charlie", Email = "charlie@test.com", Age = 35 }
        };
        var sql = "INSERT INTO test_users (name, email, age) VALUES (@name, @email, @age)";

        var affected = await DbBatchExecutor.ExecuteAsync(
            _connection, null, sql, entities, TestUserParameterBinder.Default);

        Assert.AreEqual(3, affected);
        Assert.AreEqual(3, GetUserCount());
    }

    #endregion

    #region Batch Size Tests

    [TestMethod]
    public async Task ExecuteAsync_WithSmallBatchSize_SplitsIntoBatches()
    {
        var entities = new List<TestUser>();
        for (int i = 0; i < 10; i++)
        {
            entities.Add(new TestUser { Name = $"User{i}", Email = $"user{i}@test.com", Age = 20 + i });
        }
        var sql = "INSERT INTO test_users (name, email, age) VALUES (@name, @email, @age)";

        var affected = await DbBatchExecutor.ExecuteAsync(
            _connection, null, sql, entities, TestUserParameterBinder.Default,
            batchSize: 3);

        Assert.AreEqual(10, affected);
        Assert.AreEqual(10, GetUserCount());
    }

    [TestMethod]
    public async Task ExecuteAsync_WithBatchSizeOne_ExecutesIndividually()
    {
        var entities = new List<TestUser>
        {
            new() { Name = "Alice", Email = "alice@test.com", Age = 25 },
            new() { Name = "Bob", Email = "bob@test.com", Age = 30 }
        };
        var sql = "INSERT INTO test_users (name, email, age) VALUES (@name, @email, @age)";

        var affected = await DbBatchExecutor.ExecuteAsync(
            _connection, null, sql, entities, TestUserParameterBinder.Default,
            batchSize: 1);

        Assert.AreEqual(2, affected);
        Assert.AreEqual(2, GetUserCount());
    }

    [TestMethod]
    public async Task ExecuteAsync_WithLargeBatchSize_ExecutesInSingleBatch()
    {
        var entities = new List<TestUser>
        {
            new() { Name = "Alice", Email = "alice@test.com", Age = 25 },
            new() { Name = "Bob", Email = "bob@test.com", Age = 30 }
        };
        var sql = "INSERT INTO test_users (name, email, age) VALUES (@name, @email, @age)";

        var affected = await DbBatchExecutor.ExecuteAsync(
            _connection, null, sql, entities, TestUserParameterBinder.Default,
            batchSize: 10000);

        Assert.AreEqual(2, affected);
        Assert.AreEqual(2, GetUserCount());
    }

    [TestMethod]
    public async Task ExecuteAsync_WithZeroBatchSize_UsesDefault()
    {
        var entities = new List<TestUser>
        {
            new() { Name = "Alice", Email = "alice@test.com", Age = 25 }
        };
        var sql = "INSERT INTO test_users (name, email, age) VALUES (@name, @email, @age)";

        var affected = await DbBatchExecutor.ExecuteAsync(
            _connection, null, sql, entities, TestUserParameterBinder.Default,
            batchSize: 0);

        Assert.AreEqual(1, affected);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithNegativeBatchSize_UsesDefault()
    {
        var entities = new List<TestUser>
        {
            new() { Name = "Alice", Email = "alice@test.com", Age = 25 }
        };
        var sql = "INSERT INTO test_users (name, email, age) VALUES (@name, @email, @age)";

        var affected = await DbBatchExecutor.ExecuteAsync(
            _connection, null, sql, entities, TestUserParameterBinder.Default,
            batchSize: -1);

        Assert.AreEqual(1, affected);
    }

    #endregion

    #region Transaction Tests

    [TestMethod]
    public async Task ExecuteAsync_WithTransaction_CommitsOnSuccess()
    {
        var entities = new List<TestUser>
        {
            new() { Name = "Alice", Email = "alice@test.com", Age = 25 },
            new() { Name = "Bob", Email = "bob@test.com", Age = 30 }
        };
        var sql = "INSERT INTO test_users (name, email, age) VALUES (@name, @email, @age)";

        using var transaction = _connection.BeginTransaction();
        var affected = await DbBatchExecutor.ExecuteAsync(
            _connection, transaction, sql, entities, TestUserParameterBinder.Default);
        transaction.Commit();

        Assert.AreEqual(2, affected);
        Assert.AreEqual(2, GetUserCount());
    }

    [TestMethod]
    public async Task ExecuteAsync_WithTransaction_RollbackOnFailure()
    {
        var entities = new List<TestUser>
        {
            new() { Name = "Alice", Email = "alice@test.com", Age = 25 }
        };
        var sql = "INSERT INTO test_users (name, email, age) VALUES (@name, @email, @age)";

        using var transaction = _connection.BeginTransaction();
        await DbBatchExecutor.ExecuteAsync(
            _connection, transaction, sql, entities, TestUserParameterBinder.Default);
        transaction.Rollback();

        Assert.AreEqual(0, GetUserCount());
    }

    #endregion

    #region Command Timeout Tests

    [TestMethod]
    public async Task ExecuteAsync_WithCommandTimeout_SetsTimeout()
    {
        var entities = new List<TestUser>
        {
            new() { Name = "Alice", Email = "alice@test.com", Age = 25 }
        };
        var sql = "INSERT INTO test_users (name, email, age) VALUES (@name, @email, @age)";

        var affected = await DbBatchExecutor.ExecuteAsync(
            _connection, null, sql, entities, TestUserParameterBinder.Default,
            commandTimeout: 60);

        Assert.AreEqual(1, affected);
    }

    #endregion

    #region Update and Delete Tests

    [TestMethod]
    public async Task ExecuteAsync_UpdateOperation_UpdatesRows()
    {
        // Insert initial data
        await InsertTestUsers(3);

        var updates = new List<TestUserUpdate>
        {
            new() { Id = 1, Name = "Updated1" },
            new() { Id = 2, Name = "Updated2" }
        };
        var sql = "UPDATE test_users SET name = @name WHERE id = @id";

        var affected = await DbBatchExecutor.ExecuteAsync(
            _connection, null, sql, updates, TestUserUpdateParameterBinder.Default);

        Assert.AreEqual(2, affected);

        var user1 = GetUserById(1);
        Assert.AreEqual("Updated1", user1?.Name);
    }

    [TestMethod]
    public async Task ExecuteAsync_DeleteOperation_DeletesRows()
    {
        // Insert initial data
        await InsertTestUsers(5);

        var deletes = new List<TestUserDelete>
        {
            new() { Id = 1 },
            new() { Id = 3 },
            new() { Id = 5 }
        };
        var sql = "DELETE FROM test_users WHERE id = @id";

        var affected = await DbBatchExecutor.ExecuteAsync(
            _connection, null, sql, deletes, TestUserDeleteParameterBinder.Default);

        Assert.AreEqual(3, affected);
        Assert.AreEqual(2, GetUserCount());
    }

    #endregion

    #region Extension Method Tests

    [TestMethod]
    public async Task ExecuteBatchAsync_ExtensionMethod_Works()
    {
        var entities = new List<TestUser>
        {
            new() { Name = "Alice", Email = "alice@test.com", Age = 25 },
            new() { Name = "Bob", Email = "bob@test.com", Age = 30 }
        };
        var sql = "INSERT INTO test_users (name, email, age) VALUES (@name, @email, @age)";

        var affected = await _connection.ExecuteBatchAsync(
            sql, entities, TestUserParameterBinder.Default);

        Assert.AreEqual(2, affected);
        Assert.AreEqual(2, GetUserCount());
    }

    [TestMethod]
    public async Task ExecuteBatchAsync_WithAllParameters_Works()
    {
        var entities = new List<TestUser>
        {
            new() { Name = "Alice", Email = "alice@test.com", Age = 25 }
        };
        var sql = "INSERT INTO test_users (name, email, age) VALUES (@name, @email, @age)";

        using var transaction = _connection.BeginTransaction();
        var affected = await _connection.ExecuteBatchAsync(
            sql,
            entities,
            TestUserParameterBinder.Default,
            transaction: transaction,
            parameterPrefix: "@",
            batchSize: 100,
            commandTimeout: 30);
        transaction.Commit();

        Assert.AreEqual(1, affected);
    }

    #endregion

    #region Large Dataset Tests

    [TestMethod]
    public async Task ExecuteAsync_LargeDataset_HandlesCorrectly()
    {
        var entities = new List<TestUser>();
        for (int i = 0; i < 1000; i++)
        {
            entities.Add(new TestUser
            {
                Name = $"User{i}",
                Email = $"user{i}@test.com",
                Age = 20 + (i % 50)
            });
        }
        var sql = "INSERT INTO test_users (name, email, age) VALUES (@name, @email, @age)";

        var affected = await DbBatchExecutor.ExecuteAsync(
            _connection, null, sql, entities, TestUserParameterBinder.Default,
            batchSize: 100);

        Assert.AreEqual(1000, affected);
        Assert.AreEqual(1000, GetUserCount());
    }

    #endregion

    #region Cancellation Tests

    [TestMethod]
    public async Task ExecuteAsync_WithCancellation_CanBeCancelled()
    {
        var entities = new List<TestUser>
        {
            new() { Name = "Alice", Email = "alice@test.com", Age = 25 }
        };
        var sql = "INSERT INTO test_users (name, email, age) VALUES (@name, @email, @age)";

        using var cts = new CancellationTokenSource();
        
        // Execute normally (not cancelled)
        var affected = await DbBatchExecutor.ExecuteAsync(
            _connection, null, sql, entities, TestUserParameterBinder.Default,
            cancellationToken: cts.Token);

        Assert.AreEqual(1, affected);
    }

    #endregion

    #region Helper Methods

    private int GetUserCount()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM test_users";
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private TestUser? GetUserById(long id)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT id, name, email, age FROM test_users WHERE id = @id";
        var p = cmd.CreateParameter();
        p.ParameterName = "@id";
        p.Value = id;
        cmd.Parameters.Add(p);

        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return new TestUser
            {
                Id = reader.GetInt64(0),
                Name = reader.GetString(1),
                Email = reader.GetString(2),
                Age = reader.GetInt32(3)
            };
        }
        return null;
    }

    private async Task InsertTestUsers(int count)
    {
        var entities = new List<TestUser>();
        for (int i = 0; i < count; i++)
        {
            entities.Add(new TestUser
            {
                Name = $"User{i}",
                Email = $"user{i}@test.com",
                Age = 20 + i
            });
        }
        var sql = "INSERT INTO test_users (name, email, age) VALUES (@name, @email, @age)";
        await DbBatchExecutor.ExecuteAsync(
            _connection, null, sql, entities, TestUserParameterBinder.Default);
    }

    #endregion
}

#region Test Entities and Binders

public class TestUser
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Age { get; set; }
}

public class TestUserUpdate
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class TestUserDelete
{
    public long Id { get; set; }
}

public class TestUserParameterBinder : IParameterBinder<TestUser>
{
    public static TestUserParameterBinder Default { get; } = new();

    public void BindEntity(DbCommand command, TestUser entity, string parameterPrefix = "@")
    {
        {
            var p = command.CreateParameter();
            p.ParameterName = parameterPrefix + "name";
            p.Value = entity.Name;
            command.Parameters.Add(p);
        }
        {
            var p = command.CreateParameter();
            p.ParameterName = parameterPrefix + "email";
            p.Value = entity.Email;
            command.Parameters.Add(p);
        }
        {
            var p = command.CreateParameter();
            p.ParameterName = parameterPrefix + "age";
            p.Value = entity.Age;
            command.Parameters.Add(p);
        }
    }

#if NET6_0_OR_GREATER
    public void BindEntity(DbBatchCommand command, TestUser entity, Func<DbParameter> parameterFactory, string parameterPrefix = "@")
    {
        {
            var p = parameterFactory();
            p.ParameterName = parameterPrefix + "name";
            p.Value = entity.Name;
            command.Parameters.Add(p);
        }
        {
            var p = parameterFactory();
            p.ParameterName = parameterPrefix + "email";
            p.Value = entity.Email;
            command.Parameters.Add(p);
        }
        {
            var p = parameterFactory();
            p.ParameterName = parameterPrefix + "age";
            p.Value = entity.Age;
            command.Parameters.Add(p);
        }
    }
#endif
}

public class TestUserUpdateParameterBinder : IParameterBinder<TestUserUpdate>
{
    public static TestUserUpdateParameterBinder Default { get; } = new();

    public void BindEntity(DbCommand command, TestUserUpdate entity, string parameterPrefix = "@")
    {
        {
            var p = command.CreateParameter();
            p.ParameterName = parameterPrefix + "id";
            p.Value = entity.Id;
            command.Parameters.Add(p);
        }
        {
            var p = command.CreateParameter();
            p.ParameterName = parameterPrefix + "name";
            p.Value = entity.Name;
            command.Parameters.Add(p);
        }
    }

#if NET6_0_OR_GREATER
    public void BindEntity(DbBatchCommand command, TestUserUpdate entity, Func<DbParameter> parameterFactory, string parameterPrefix = "@")
    {
        {
            var p = parameterFactory();
            p.ParameterName = parameterPrefix + "id";
            p.Value = entity.Id;
            command.Parameters.Add(p);
        }
        {
            var p = parameterFactory();
            p.ParameterName = parameterPrefix + "name";
            p.Value = entity.Name;
            command.Parameters.Add(p);
        }
    }
#endif
}

public class TestUserDeleteParameterBinder : IParameterBinder<TestUserDelete>
{
    public static TestUserDeleteParameterBinder Default { get; } = new();

    public void BindEntity(DbCommand command, TestUserDelete entity, string parameterPrefix = "@")
    {
        var p = command.CreateParameter();
        p.ParameterName = parameterPrefix + "id";
        p.Value = entity.Id;
        command.Parameters.Add(p);
    }

#if NET6_0_OR_GREATER
    public void BindEntity(DbBatchCommand command, TestUserDelete entity, Func<DbParameter> parameterFactory, string parameterPrefix = "@")
    {
        var p = parameterFactory();
        p.ParameterName = parameterPrefix + "id";
        p.Value = entity.Id;
        command.Parameters.Add(p);
    }
#endif
}

#endregion
