// <copyright file="SqlxContextE2ETests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;
using Sqlx.Tests.E2E.Infrastructure;
using System.Linq;

namespace Sqlx.Tests.E2E.Context;

[Sqlx]
[TableName("context_items")]
public class ContextItem
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public interface IContextItemRepository : ICrudRepository<ContextItem, long>
{
    [SqlTemplate("INSERT INTO {{table}} (name) VALUES (@name)")]
    Task<int> InsertNameAsync(string name);

    [SqlTemplate("SELECT COUNT(*) FROM {{table}}")]
    Task<int> CountRowsAsync();

    [SqlTemplate("SELECT COUNT(*) FROM context_items_missing")]
    Task<int> FailWithMissingTableAsync();
}

[RepositoryFor(typeof(IContextItemRepository), TableName = "context_items")]
public partial class ContextItemRepository : IContextItemRepository
{
    private readonly System.Data.Common.DbConnection _connection;

    public ContextItemRepository(System.Data.Common.DbConnection connection, SqlDialect dialect)
    {
        _connection = connection;
        _dialect = dialect;
    }
}

[SqlxContext]
[IncludeRepository(typeof(ContextItemRepository))]
public partial class ContextItemDbContext : SqlxContext
{
}

[TestClass]
public class SqlxContextE2ETests : E2ETestBase
{
    private static string GetSchema(DatabaseType dbType)
    {
        return dbType switch
        {
            DatabaseType.MySQL => @"
                CREATE TABLE context_items (
                    id BIGINT AUTO_INCREMENT PRIMARY KEY,
                    name VARCHAR(100) NOT NULL
                )",
            DatabaseType.PostgreSQL => @"
                CREATE TABLE context_items (
                    id BIGSERIAL PRIMARY KEY,
                    name VARCHAR(100) NOT NULL
                )",
            DatabaseType.SqlServer => @"
                CREATE TABLE context_items (
                    id BIGINT IDENTITY(1,1) PRIMARY KEY,
                    name NVARCHAR(100) NOT NULL
                )",
            DatabaseType.SQLite => @"
                CREATE TABLE context_items (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL
                )",
            _ => throw new NotSupportedException($"Database type {dbType} is not supported"),
        };
    }

    private static SqlDialect GetDialect(DatabaseType dbType)
    {
        return dbType switch
        {
            DatabaseType.MySQL => SqlDefine.MySql,
            DatabaseType.PostgreSQL => SqlDefine.PostgreSql,
            DatabaseType.SqlServer => SqlDefine.SqlServer,
            DatabaseType.SQLite => SqlDefine.SQLite,
            _ => throw new NotSupportedException($"Database type {dbType} is not supported"),
        };
    }

    private static ServiceProvider BuildServiceProvider(IDatabaseFixture fixture, DatabaseType dbType)
    {
        var services = new ServiceCollection();
        services.AddSingleton(new ContextItemRepository(fixture.Connection, GetDialect(dbType)));
        return services.BuildServiceProvider();
    }

    private static async Task RunOwnedTransactionScenario(DatabaseType dbType, SqlxContextE2ETests test)
    {
        await using var fixture = await test.CreateFixtureAsync(dbType);
        await fixture.CreateSchemaAsync(GetSchema(dbType));
        using var serviceProvider = BuildServiceProvider(fixture, dbType);
        await using (var context = new ContextItemDbContext(fixture.Connection, options: null, serviceProvider))
        {
            var repository = context.ContextItems;
            Assert.AreSame(fixture.Connection, ((ISqlxRepository)repository).Connection);
            Assert.IsNull(repository.Transaction);

            await using var transaction = await context.BeginTransactionAsync();
            Assert.AreEqual(transaction, repository.Transaction);

            var rowsAffected = await repository.InsertNameAsync("owned-transaction");
            Assert.IsTrue(rowsAffected == 1 || rowsAffected == -1);

            await transaction.CommitAsync();
        }

        await using var verificationConnection = await fixture.CreateNewConnectionAsync();
        using var verificationCommand = verificationConnection.CreateCommand();
        verificationCommand.CommandText = "SELECT COUNT(*) FROM context_items";
        var count = Convert.ToInt32(await verificationCommand.ExecuteScalarAsync());
        Assert.AreEqual(1, count);
    }

    private static async Task RunLazyResolutionScenario(DatabaseType dbType, SqlxContextE2ETests test)
    {
        await using var fixture = await test.CreateFixtureAsync(dbType);
        await fixture.CreateSchemaAsync(GetSchema(dbType));
        using var serviceProvider = BuildServiceProvider(fixture, dbType);
        await using var context = new ContextItemDbContext(fixture.Connection, options: null, serviceProvider);

        var repository = context.ContextItems;
        var sqlxRepository = (ISqlxRepository)repository;

        Assert.AreSame(fixture.Connection, sqlxRepository.Connection);
        Assert.IsNull(repository.Transaction);

        var rowsAffected = await repository.InsertNameAsync("lazy-resolution");
        Assert.IsTrue(rowsAffected == 1 || rowsAffected == -1);

        var count = await repository.CountRowsAsync();
        Assert.AreEqual(1, count);
    }

    private static async Task RunExternalTransactionClearScenario(DatabaseType dbType, SqlxContextE2ETests test)
    {
        await using var fixture = await test.CreateFixtureAsync(dbType);
        await fixture.CreateSchemaAsync(GetSchema(dbType));
        using var serviceProvider = BuildServiceProvider(fixture, dbType);
        await using var context = new ContextItemDbContext(fixture.Connection, options: null, serviceProvider);

        var repository = context.ContextItems;
        await using var externalTransaction = await fixture.Connection.BeginTransactionAsync();
        context.UseTransaction(externalTransaction);
        Assert.AreEqual(externalTransaction, repository.Transaction);

        context.UseTransaction(null);
        Assert.IsNull(repository.Transaction);
        await externalTransaction.RollbackAsync();
    }

    private static async Task RunExternalTransactionScenario(DatabaseType dbType, SqlxContextE2ETests test)
    {
        await using var fixture = await test.CreateFixtureAsync(dbType);
        await fixture.CreateSchemaAsync(GetSchema(dbType));
        using var serviceProvider = BuildServiceProvider(fixture, dbType);
        await using (var context = new ContextItemDbContext(fixture.Connection, options: null, serviceProvider))
        {
            var repository = context.ContextItems;
            await using var externalTransaction = await fixture.Connection.BeginTransactionAsync();
            context.UseTransaction(externalTransaction);

            Assert.AreEqual(externalTransaction, repository.Transaction);

            var rowsAffected = await repository.InsertNameAsync("external-transaction");
            Assert.IsTrue(rowsAffected == 1 || rowsAffected == -1);

            await externalTransaction.CommitAsync();
            context.UseTransaction(null);
        }

        await using var verificationConnection = await fixture.CreateNewConnectionAsync();
        using var verificationCommand = verificationConnection.CreateCommand();
        verificationCommand.CommandText = "SELECT COUNT(*) FROM context_items";
        var count = Convert.ToInt32(await verificationCommand.ExecuteScalarAsync());
        Assert.AreEqual(1, count);
    }

    private static async Task RunOptionsExceptionScenario(DatabaseType dbType, SqlxContextE2ETests test)
    {
        await using var fixture = await test.CreateFixtureAsync(dbType);
        await fixture.CreateSchemaAsync(GetSchema(dbType));
        using var serviceProvider = BuildServiceProvider(fixture, dbType);
        var logger = new TestLogger();
        SqlxException? callbackException = null;
        var options = new SqlxContextOptions
        {
            Logger = logger,
            OnException = ex =>
            {
                callbackException = ex;
                return Task.CompletedTask;
            }
        };

        await using var context = new ContextItemDbContext(fixture.Connection, options, serviceProvider);
        var repository = context.ContextItems;
        var sqlxRepository = (ISqlxRepository)repository;

        Assert.AreSame(options, sqlxRepository.Options);

        var exception = await Assert.ThrowsExceptionAsync<SqlxException>(() => repository.FailWithMissingTableAsync());

        Assert.AreSame(exception, callbackException);
        Assert.AreEqual("FailWithMissingTableAsync", exception.MethodName);
        Assert.IsTrue(exception.Sql!.Contains("context_items_missing", StringComparison.OrdinalIgnoreCase));
        Assert.AreEqual(1, logger.Entries.Count(e => e.LogLevel == LogLevel.Error));
        Assert.AreSame(exception, logger.Entries.Single(e => e.LogLevel == LogLevel.Error).Exception);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxContext")]
    [TestCategory("MySQL")]
    public async Task MySQL_SqlxContext_OwnedTransaction_PropagatesToRepository()
    {
        await RunOwnedTransactionScenario(DatabaseType.MySQL, this);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxContext")]
    [TestCategory("MySQL")]
    public async Task MySQL_SqlxContext_ExternalTransaction_PropagatesToRepository()
    {
        await RunExternalTransactionScenario(DatabaseType.MySQL, this);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxContext")]
    [TestCategory("MySQL")]
    public async Task MySQL_SqlxContext_LazyResolution_AssignsConnection()
    {
        await RunLazyResolutionScenario(DatabaseType.MySQL, this);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxContext")]
    [TestCategory("MySQL")]
    public async Task MySQL_SqlxContext_UseTransactionNull_ClearsRepositoryTransaction()
    {
        await RunExternalTransactionClearScenario(DatabaseType.MySQL, this);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxContext")]
    [TestCategory("MySQL")]
    public async Task MySQL_SqlxContext_Options_AreAppliedToRepositoryExceptions()
    {
        await RunOptionsExceptionScenario(DatabaseType.MySQL, this);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxContext")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_SqlxContext_OwnedTransaction_PropagatesToRepository()
    {
        await RunOwnedTransactionScenario(DatabaseType.PostgreSQL, this);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxContext")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_SqlxContext_ExternalTransaction_PropagatesToRepository()
    {
        await RunExternalTransactionScenario(DatabaseType.PostgreSQL, this);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxContext")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_SqlxContext_LazyResolution_AssignsConnection()
    {
        await RunLazyResolutionScenario(DatabaseType.PostgreSQL, this);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxContext")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_SqlxContext_UseTransactionNull_ClearsRepositoryTransaction()
    {
        await RunExternalTransactionClearScenario(DatabaseType.PostgreSQL, this);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxContext")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_SqlxContext_Options_AreAppliedToRepositoryExceptions()
    {
        await RunOptionsExceptionScenario(DatabaseType.PostgreSQL, this);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxContext")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_SqlxContext_OwnedTransaction_PropagatesToRepository()
    {
        await RunOwnedTransactionScenario(DatabaseType.SqlServer, this);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxContext")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_SqlxContext_ExternalTransaction_PropagatesToRepository()
    {
        await RunExternalTransactionScenario(DatabaseType.SqlServer, this);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxContext")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_SqlxContext_LazyResolution_AssignsConnection()
    {
        await RunLazyResolutionScenario(DatabaseType.SqlServer, this);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxContext")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_SqlxContext_UseTransactionNull_ClearsRepositoryTransaction()
    {
        await RunExternalTransactionClearScenario(DatabaseType.SqlServer, this);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxContext")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_SqlxContext_Options_AreAppliedToRepositoryExceptions()
    {
        await RunOptionsExceptionScenario(DatabaseType.SqlServer, this);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxContext")]
    [TestCategory("SQLite")]
    public async Task SQLite_SqlxContext_OwnedTransaction_PropagatesToRepository()
    {
        await RunOwnedTransactionScenario(DatabaseType.SQLite, this);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxContext")]
    [TestCategory("SQLite")]
    public async Task SQLite_SqlxContext_ExternalTransaction_PropagatesToRepository()
    {
        await RunExternalTransactionScenario(DatabaseType.SQLite, this);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxContext")]
    [TestCategory("SQLite")]
    public async Task SQLite_SqlxContext_LazyResolution_AssignsConnection()
    {
        await RunLazyResolutionScenario(DatabaseType.SQLite, this);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxContext")]
    [TestCategory("SQLite")]
    public async Task SQLite_SqlxContext_UseTransactionNull_ClearsRepositoryTransaction()
    {
        await RunExternalTransactionClearScenario(DatabaseType.SQLite, this);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxContext")]
    [TestCategory("SQLite")]
    public async Task SQLite_SqlxContext_Options_AreAppliedToRepositoryExceptions()
    {
        await RunOptionsExceptionScenario(DatabaseType.SQLite, this);
    }

    private sealed class TestLogger : ILogger
    {
        public List<LogEntry> Entries { get; } = new();

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add(new LogEntry(logLevel, eventId, formatter(state, exception), exception));
        }
    }

    private sealed record LogEntry(LogLevel LogLevel, EventId EventId, string Message, Exception? Exception);

    private sealed class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new();

        public void Dispose()
        {
        }
    }
}
