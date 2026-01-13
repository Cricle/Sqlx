using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using Sqlx.Annotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.Common;
using System.Diagnostics;

namespace Sqlx.Tests;

/// <summary>
/// Database integration tests for Activity tracking and Interceptors.
/// Uses in-memory SQLite for testing.
/// </summary>
[TestClass]
public class DatabaseIntegrationTests
{
    private SqliteConnection _connection = null!;

    [TestInitialize]
    public void Setup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        
        // Create test table
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE test_users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                email TEXT,
                is_active INTEGER NOT NULL DEFAULT 1,
                created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
            )";
        cmd.ExecuteNonQuery();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _connection?.Dispose();
    }

    #region Test Entity

    [SqlxEntity]
    [SqlxParameter]
    public class DbTestUser
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Email { get; set; }
        
        [Column("is_active")]
        public bool IsActive { get; set; }
        
        [Column("created_at")]
        public string CreatedAt { get; set; } = string.Empty;
    }

    #endregion

    #region Generated Code Simulation Tests

    [TestMethod]
    public async Task GeneratedResultReader_CanReadEntities()
    {
        // Insert test data
        using (var cmd = _connection.CreateCommand())
        {
            cmd.CommandText = "INSERT INTO test_users (name, email, is_active) VALUES ('Alice', 'alice@test.com', 1)";
            await cmd.ExecuteNonQueryAsync();
            
            cmd.CommandText = "INSERT INTO test_users (name, email, is_active) VALUES ('Bob', NULL, 0)";
            await cmd.ExecuteNonQueryAsync();
        }

        // Read using generated-style code
        using var readCmd = _connection.CreateCommand();
        readCmd.CommandText = "SELECT id, name, email, is_active, created_at FROM test_users ORDER BY id";
        
        using var reader = await readCmd.ExecuteReaderAsync();
        var users = ReadUsers(reader).ToList();

        Assert.AreEqual(2, users.Count);
        Assert.AreEqual("Alice", users[0].Name);
        Assert.AreEqual("alice@test.com", users[0].Email);
        Assert.IsTrue(users[0].IsActive);
        Assert.AreEqual("Bob", users[1].Name);
        Assert.IsNull(users[1].Email);
        Assert.IsFalse(users[1].IsActive);
    }

    [TestMethod]
    public async Task GeneratedParameterBinder_CanBindParameters()
    {
        var user = new DbTestUser
        {
            Name = "Charlie",
            Email = "charlie@test.com",
            IsActive = true
        };

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "INSERT INTO test_users (name, email, is_active) VALUES (@name, @email, @is_active)";
        
        // Simulate generated parameter binding
        BindUserParameters(cmd, user);
        
        var result = await cmd.ExecuteNonQueryAsync();
        Assert.AreEqual(1, result);

        // Verify insertion
        cmd.CommandText = "SELECT name, email FROM test_users WHERE name = 'Charlie'";
        cmd.Parameters.Clear();
        using var reader = await cmd.ExecuteReaderAsync();
        Assert.IsTrue(await reader.ReadAsync());
        Assert.AreEqual("Charlie", reader.GetString(0));
        Assert.AreEqual("charlie@test.com", reader.GetString(1));
    }

    [TestMethod]
    public async Task SqlTemplate_WithPlaceholders_GeneratesCorrectSql()
    {
        // Simulate what generated repository would do
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("name", "Name", DbType.String, false),
            new ColumnMeta("email", "Email", DbType.String, true),
            new ColumnMeta("is_active", "IsActive", DbType.Boolean, false),
            new ColumnMeta("created_at", "CreatedAt", DbType.String, false),
        };
        
        var context = new PlaceholderContext(SqlDefine.SQLite, "test_users", columns);
        
        // Prepare template (done once at static initialization)
        var template = SqlTemplate.Prepare(
            "SELECT {{columns}} FROM {{table}} WHERE is_active = @isActive",
            context);

        // Insert test data
        using (var cmd = _connection.CreateCommand())
        {
            cmd.CommandText = "INSERT INTO test_users (name, is_active) VALUES ('Active User', 1)";
            await cmd.ExecuteNonQueryAsync();
            cmd.CommandText = "INSERT INTO test_users (name, is_active) VALUES ('Inactive User', 0)";
            await cmd.ExecuteNonQueryAsync();
        }

        // Execute query
        using var queryCmd = _connection.CreateCommand();
        queryCmd.CommandText = template.Sql;
        
        var param = queryCmd.CreateParameter();
        param.ParameterName = "@isActive";
        param.Value = 1;
        queryCmd.Parameters.Add(param);

        using var reader = await queryCmd.ExecuteReaderAsync();
        var users = ReadUsers(reader).ToList();

        Assert.AreEqual(1, users.Count);
        Assert.AreEqual("Active User", users[0].Name);
    }

    #endregion

    #region Activity Tracking Tests

    [TestMethod]
    public void ActivityTracking_CanCreateAndSetTags()
    {
        // Test that Activity API works as expected
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        var source = new ActivitySource("Sqlx.Tests");
        using var activity = source.StartActivity("TestOperation");
        
        Assert.IsNotNull(activity);
        
        activity.SetTag("db.system", "sqlite");
        activity.SetTag("db.operation", "sqlx.execute");
        activity.SetTag("db.rows_affected", "5"); // Use string for consistency
        
        // Use FirstOrDefault to safely check tags
        var systemTag = activity.Tags.FirstOrDefault(t => t.Key == "db.system");
        var operationTag = activity.Tags.FirstOrDefault(t => t.Key == "db.operation");
        var rowsTag = activity.Tags.FirstOrDefault(t => t.Key == "db.rows_affected");
        
        Assert.AreEqual("sqlite", systemTag.Value);
        Assert.AreEqual("sqlx.execute", operationTag.Value);
        Assert.AreEqual("5", rowsTag.Value);
    }

    [TestMethod]
    public void ActivityTracking_CanSetErrorStatus()
    {
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        var source = new ActivitySource("Sqlx.Tests");
        using var activity = source.StartActivity("FailingOperation");
        
        Assert.IsNotNull(activity);
        
        activity.SetStatus(ActivityStatusCode.Error, "Test error message");
        
        Assert.AreEqual(ActivityStatusCode.Error, activity.Status);
        Assert.AreEqual("Test error message", activity.StatusDescription);
    }

    #endregion

    #region Interceptor Pattern Tests

    [TestMethod]
    public async Task InterceptorPattern_OnExecuting_CalledBeforeExecution()
    {
        var interceptor = new TestInterceptor();
        
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT 1";
        
        // Simulate interceptor call
        interceptor.OnExecuting("TestMethod", cmd);
        
        await cmd.ExecuteScalarAsync();
        
        Assert.IsTrue(interceptor.OnExecutingCalled);
        Assert.AreEqual("TestMethod", interceptor.LastOperationName);
    }

    [TestMethod]
    public async Task InterceptorPattern_OnExecuted_CalledAfterSuccess()
    {
        var interceptor = new TestInterceptor();
        var startTime = Stopwatch.GetTimestamp();
        
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT 1";
        
        interceptor.OnExecuting("TestMethod", cmd);
        var result = await cmd.ExecuteScalarAsync();
        var elapsed = Stopwatch.GetTimestamp() - startTime;
        
        interceptor.OnExecuted("TestMethod", cmd, result, elapsed);
        
        Assert.IsTrue(interceptor.OnExecutedCalled);
        Assert.AreEqual(1L, interceptor.LastResult);
        Assert.IsTrue(interceptor.LastElapsedTicks > 0);
    }

    [TestMethod]
    public void InterceptorPattern_OnExecuteFail_CalledOnException()
    {
        var interceptor = new TestInterceptor();
        var startTime = Stopwatch.GetTimestamp();
        
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM nonexistent_table";
        
        interceptor.OnExecuting("TestMethod", cmd);
        
        try
        {
            cmd.ExecuteScalar();
        }
        catch (Exception ex)
        {
            var elapsed = Stopwatch.GetTimestamp() - startTime;
            interceptor.OnExecuteFail("TestMethod", cmd, ex, elapsed);
        }
        
        Assert.IsTrue(interceptor.OnExecuteFailCalled);
        Assert.IsNotNull(interceptor.LastException);
        Assert.IsTrue(interceptor.LastException.Message.Contains("nonexistent_table") || 
                      interceptor.LastException.Message.Contains("no such table"));
    }

    private class TestInterceptor
    {
        public bool OnExecutingCalled { get; private set; }
        public bool OnExecutedCalled { get; private set; }
        public bool OnExecuteFailCalled { get; private set; }
        public string? LastOperationName { get; private set; }
        public object? LastResult { get; private set; }
        public long LastElapsedTicks { get; private set; }
        public Exception? LastException { get; private set; }

        public void OnExecuting(string operationName, DbCommand command)
        {
            OnExecutingCalled = true;
            LastOperationName = operationName;
        }

        public void OnExecuted(string operationName, DbCommand command, object? result, long elapsedTicks)
        {
            OnExecutedCalled = true;
            LastOperationName = operationName;
            LastResult = result;
            LastElapsedTicks = elapsedTicks;
        }

        public void OnExecuteFail(string operationName, DbCommand command, Exception exception, long elapsedTicks)
        {
            OnExecuteFailCalled = true;
            LastOperationName = operationName;
            LastException = exception;
            LastElapsedTicks = elapsedTicks;
        }
    }

    #endregion

    #region Helper Methods (Simulating Generated Code)

    private static IEnumerable<DbTestUser> ReadUsers(DbDataReader reader)
    {
        var ordId = reader.GetOrdinal("id");
        var ordName = reader.GetOrdinal("name");
        var ordEmail = reader.GetOrdinal("email");
        var ordIsActive = reader.GetOrdinal("is_active");
        var ordCreatedAt = reader.GetOrdinal("created_at");

        while (reader.Read())
        {
            yield return new DbTestUser
            {
                Id = reader.GetInt64(ordId),
                Name = reader.GetString(ordName),
                Email = reader.IsDBNull(ordEmail) ? null : reader.GetString(ordEmail),
                IsActive = reader.GetInt64(ordIsActive) == 1,
                CreatedAt = reader.GetString(ordCreatedAt)
            };
        }
    }

    private static void BindUserParameters(DbCommand command, DbTestUser user)
    {
        var pName = command.CreateParameter();
        pName.ParameterName = "@name";
        pName.Value = user.Name;
        command.Parameters.Add(pName);

        var pEmail = command.CreateParameter();
        pEmail.ParameterName = "@email";
        pEmail.Value = user.Email ?? (object)DBNull.Value;
        command.Parameters.Add(pEmail);

        var pIsActive = command.CreateParameter();
        pIsActive.ParameterName = "@is_active";
        pIsActive.Value = user.IsActive ? 1 : 0;
        command.Parameters.Add(pIsActive);
    }

    #endregion
}
