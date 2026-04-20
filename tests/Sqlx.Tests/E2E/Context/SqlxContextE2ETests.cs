// <copyright file="SqlxContextE2ETests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;
using Sqlx.Tests.E2E.Infrastructure;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;

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

    [SqlTemplate("SELECT COUNT(*) FROM {{table}}")]
    int CountRows();

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

    private static ServiceProvider BuildServiceProvider(IDatabaseFixture fixture, DatabaseType dbType) =>
        BuildServiceProvider(fixture.Connection, dbType);

    private static ServiceProvider BuildServiceProvider(DbConnection connection, DatabaseType dbType)
    {
        var services = new ServiceCollection();
        services.AddSingleton(new ContextItemRepository(connection, GetDialect(dbType)));
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

    private static async Task RunRetrySuccessScenarioAsync(DatabaseType dbType, SqlxContextE2ETests test)
    {
        await using var fixture = await test.CreateFixtureAsync(dbType);
        await fixture.CreateSchemaAsync(GetSchema(dbType));
        await using var retryingConnection = new TransientFailingDbConnection(fixture.Connection, failuresBeforeSuccess: 1);
        using var serviceProvider = BuildServiceProvider(retryingConnection, dbType);

        var logger = new TestLogger();
        var callbackCount = 0;
        var options = new SqlxContextOptions
        {
            EnableRetry = true,
            MaxRetryCount = 2,
            InitialRetryDelay = TimeSpan.FromMilliseconds(1),
            Logger = logger,
            OnException = _ =>
            {
                callbackCount++;
                return Task.CompletedTask;
            }
        };

        await using var context = new ContextItemDbContext(retryingConnection, options, serviceProvider);
        var count = await context.ContextItems.CountRowsAsync();

        Assert.AreEqual(0, count);
        Assert.AreEqual(1, callbackCount);
        Assert.AreEqual(1, logger.Entries.Count(e => e.LogLevel == LogLevel.Error));
        Assert.AreEqual(1, logger.Entries.Count(e => e.LogLevel == LogLevel.Warning));
    }

    private static async Task RunRetrySuccessScenarioSync(DatabaseType dbType, SqlxContextE2ETests test)
    {
        await using var fixture = await test.CreateFixtureAsync(dbType);
        await fixture.CreateSchemaAsync(GetSchema(dbType));
        await using var retryingConnection = new TransientFailingDbConnection(fixture.Connection, failuresBeforeSuccess: 1);
        using var serviceProvider = BuildServiceProvider(retryingConnection, dbType);

        var logger = new TestLogger();
        var callbackCount = 0;
        var options = new SqlxContextOptions
        {
            EnableRetry = true,
            MaxRetryCount = 2,
            InitialRetryDelay = TimeSpan.FromMilliseconds(1),
            Logger = logger,
            OnException = _ =>
            {
                callbackCount++;
                return Task.CompletedTask;
            }
        };

        using var context = new ContextItemDbContext(retryingConnection, options, serviceProvider);
        var count = context.ContextItems.CountRows();

        Assert.AreEqual(0, count);
        Assert.AreEqual(1, callbackCount);
        Assert.AreEqual(1, logger.Entries.Count(e => e.LogLevel == LogLevel.Error));
        Assert.AreEqual(1, logger.Entries.Count(e => e.LogLevel == LogLevel.Warning));
    }

    private static async Task RunRetryFailureScenarioAsync(DatabaseType dbType, SqlxContextE2ETests test)
    {
        await using var fixture = await test.CreateFixtureAsync(dbType);
        await fixture.CreateSchemaAsync(GetSchema(dbType));
        await using var retryingConnection = new TransientFailingDbConnection(fixture.Connection, failuresBeforeSuccess: 2);
        using var serviceProvider = BuildServiceProvider(retryingConnection, dbType);

        var logger = new TestLogger();
        var callbackCount = 0;
        var options = new SqlxContextOptions
        {
            EnableRetry = true,
            MaxRetryCount = 2,
            InitialRetryDelay = TimeSpan.FromMilliseconds(1),
            Logger = logger,
            OnException = _ =>
            {
                callbackCount++;
                return Task.CompletedTask;
            }
        };

        await using var context = new ContextItemDbContext(retryingConnection, options, serviceProvider);
        var exception = await Assert.ThrowsExceptionAsync<SqlxException>(() => context.ContextItems.CountRowsAsync());

        Assert.AreEqual("CountRowsAsync", exception.MethodName);
        Assert.AreEqual(2, callbackCount);
        Assert.AreEqual(2, logger.Entries.Count(e => e.LogLevel == LogLevel.Error));
        Assert.AreEqual(1, logger.Entries.Count(e => e.LogLevel == LogLevel.Warning));
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
    [TestCategory("MySQL")]
    public async Task MySQL_SqlxContext_EnableRetry_RetriesAsyncRepositoryExecution()
    {
        await RunRetrySuccessScenarioAsync(DatabaseType.MySQL, this);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxContext")]
    [TestCategory("MySQL")]
    public async Task MySQL_SqlxContext_EnableRetry_RetriesSyncRepositoryExecution()
    {
        await RunRetrySuccessScenarioSync(DatabaseType.MySQL, this);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxContext")]
    [TestCategory("MySQL")]
    public async Task MySQL_SqlxContext_EnableRetry_ThrowsAfterMaxAttempts()
    {
        await RunRetryFailureScenarioAsync(DatabaseType.MySQL, this);
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
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_SqlxContext_EnableRetry_RetriesAsyncRepositoryExecution()
    {
        await RunRetrySuccessScenarioAsync(DatabaseType.PostgreSQL, this);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxContext")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_SqlxContext_EnableRetry_RetriesSyncRepositoryExecution()
    {
        await RunRetrySuccessScenarioSync(DatabaseType.PostgreSQL, this);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxContext")]
    [TestCategory("PostgreSQL")]
    public async Task PostgreSQL_SqlxContext_EnableRetry_ThrowsAfterMaxAttempts()
    {
        await RunRetryFailureScenarioAsync(DatabaseType.PostgreSQL, this);
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
    [TestCategory("SqlServer")]
    public async Task SqlServer_SqlxContext_EnableRetry_RetriesAsyncRepositoryExecution()
    {
        await RunRetrySuccessScenarioAsync(DatabaseType.SqlServer, this);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxContext")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_SqlxContext_EnableRetry_RetriesSyncRepositoryExecution()
    {
        await RunRetrySuccessScenarioSync(DatabaseType.SqlServer, this);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxContext")]
    [TestCategory("SqlServer")]
    public async Task SqlServer_SqlxContext_EnableRetry_ThrowsAfterMaxAttempts()
    {
        await RunRetryFailureScenarioAsync(DatabaseType.SqlServer, this);
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

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxContext")]
    [TestCategory("SQLite")]
    public async Task SQLite_SqlxContext_EnableRetry_RetriesAsyncRepositoryExecution()
    {
        await RunRetrySuccessScenarioAsync(DatabaseType.SQLite, this);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxContext")]
    [TestCategory("SQLite")]
    public async Task SQLite_SqlxContext_EnableRetry_RetriesSyncRepositoryExecution()
    {
        await RunRetrySuccessScenarioSync(DatabaseType.SQLite, this);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("SqlxContext")]
    [TestCategory("SQLite")]
    public async Task SQLite_SqlxContext_EnableRetry_ThrowsAfterMaxAttempts()
    {
        await RunRetryFailureScenarioAsync(DatabaseType.SQLite, this);
    }

    private sealed class TransientFailingDbConnection : DbConnection
    {
        private readonly DbConnection _innerConnection;
        private int _remainingFailures;

        public TransientFailingDbConnection(DbConnection innerConnection, int failuresBeforeSuccess)
        {
            _innerConnection = innerConnection;
            _remainingFailures = failuresBeforeSuccess;
        }

#pragma warning disable CS8765
        public override string ConnectionString
        {
            get => _innerConnection.ConnectionString;
            set => _innerConnection.ConnectionString = value;
        }
#pragma warning restore CS8765

        public override string Database => _innerConnection.Database;
        public override string DataSource => _innerConnection.DataSource;
        public override string ServerVersion => _innerConnection.ServerVersion;
        public override ConnectionState State => _innerConnection.State;
        public override void ChangeDatabase(string databaseName) => _innerConnection.ChangeDatabase(databaseName);
        public override void Close() => _innerConnection.Close();
        public override void Open() => _innerConnection.Open();
        public override Task OpenAsync(CancellationToken cancellationToken) => _innerConnection.OpenAsync(cancellationToken);
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => _innerConnection.BeginTransaction(isolationLevel);

        protected override DbCommand CreateDbCommand() =>
            new TransientFailingDbCommand(_innerConnection.CreateCommand(), ConsumeFailure);

        private void ConsumeFailure()
        {
            if (Interlocked.Decrement(ref _remainingFailures) >= 0)
            {
                throw new TimeoutException("Simulated transient failure");
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _innerConnection.Dispose();
            }

            base.Dispose(disposing);
        }

        public override async ValueTask DisposeAsync()
        {
            await _innerConnection.DisposeAsync();
            await base.DisposeAsync();
        }
    }

    private sealed class TransientFailingDbCommand : DbCommand
    {
        private readonly DbCommand _innerCommand;
        private readonly Action _consumeFailure;

        public TransientFailingDbCommand(DbCommand innerCommand, Action consumeFailure)
        {
            _innerCommand = innerCommand;
            _consumeFailure = consumeFailure;
        }

#pragma warning disable CS8765
        public override string CommandText
        {
            get => _innerCommand.CommandText;
            set => _innerCommand.CommandText = value;
        }
#pragma warning restore CS8765

        public override int CommandTimeout
        {
            get => _innerCommand.CommandTimeout;
            set => _innerCommand.CommandTimeout = value;
        }

        public override CommandType CommandType
        {
            get => _innerCommand.CommandType;
            set => _innerCommand.CommandType = value;
        }

        public override UpdateRowSource UpdatedRowSource
        {
            get => _innerCommand.UpdatedRowSource;
            set => _innerCommand.UpdatedRowSource = value;
        }

        protected override DbConnection? DbConnection
        {
            get => _innerCommand.Connection;
            set => _innerCommand.Connection = value;
        }

        protected override DbParameterCollection DbParameterCollection => _innerCommand.Parameters;

        protected override DbTransaction? DbTransaction
        {
            get => _innerCommand.Transaction;
            set => _innerCommand.Transaction = value;
        }

        public override bool DesignTimeVisible
        {
            get => _innerCommand.DesignTimeVisible;
            set => _innerCommand.DesignTimeVisible = value;
        }

        public override void Cancel() => _innerCommand.Cancel();

        public override int ExecuteNonQuery()
        {
            _consumeFailure();
            return _innerCommand.ExecuteNonQuery();
        }

        public override object? ExecuteScalar()
        {
            _consumeFailure();
            return _innerCommand.ExecuteScalar();
        }

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            _consumeFailure();
            return _innerCommand.ExecuteReader(behavior);
        }

        public override async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
        {
            _consumeFailure();
            return await _innerCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        public override async Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken)
        {
            _consumeFailure();
            return await _innerCommand.ExecuteScalarAsync(cancellationToken);
        }

        protected override async Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
        {
            _consumeFailure();
            return await _innerCommand.ExecuteReaderAsync(behavior, cancellationToken);
        }

        public override void Prepare() => _innerCommand.Prepare();
        protected override DbParameter CreateDbParameter() => _innerCommand.CreateParameter();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _innerCommand.Dispose();
            }

            base.Dispose(disposing);
        }

        public override async ValueTask DisposeAsync()
        {
            await _innerCommand.DisposeAsync();
            await base.DisposeAsync();
        }
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
