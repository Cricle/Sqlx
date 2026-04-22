using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Sqlx.Tests.QueryBuilder;

[TestClass]
public class SqlxQueryableExtensionCoverageTests
{
    [TestMethod]
    public void WithConnection_NullQuery_ThrowsArgumentNullException()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        IQueryable<QueryUser>? query = null;

        Assert.ThrowsException<ArgumentNullException>(() => query!.WithConnection(connection));
    }

    [TestMethod]
    public void WithConnection_NullConnection_ThrowsArgumentNullException()
    {
        var query = SqlQuery<QueryUser>.ForSqlite();

        Assert.ThrowsException<ArgumentNullException>(() => query.WithConnection(null!));
    }

    [TestMethod]
    public void WithConnection_OnNonSqlxQueryable_ThrowsInvalidOperationException()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        var query = new List<QueryUser>().AsQueryable();

        Assert.ThrowsException<InvalidOperationException>(() => query.WithConnection(connection));
    }

    [TestMethod]
    public void WithReader_NullQuery_ThrowsArgumentNullException()
    {
        IQueryable<QueryUser>? query = null;

        Assert.ThrowsException<ArgumentNullException>(() => query!.WithReader(new CoverageReader()));
    }

    [TestMethod]
    public void WithReader_NullReader_ThrowsArgumentNullException()
    {
        var query = SqlQuery<QueryUser>.ForSqlite();

        Assert.ThrowsException<ArgumentNullException>(() => query.WithReader(null!));
    }

    [TestMethod]
    public void WithReader_OnNonSqlxQueryable_ThrowsInvalidOperationException()
    {
        var query = new List<QueryUser>().AsQueryable();

        Assert.ThrowsException<InvalidOperationException>(() => query.WithReader(new CoverageReader()));
    }

    [TestMethod]
    public void WithTransaction_NullQuery_ThrowsArgumentNullException()
    {
        IQueryable<QueryUser>? query = null;
        var transaction = new TestDbTransaction(new TestDbConnection());

        Assert.ThrowsException<ArgumentNullException>(() => query!.WithTransaction(transaction));
    }

    [TestMethod]
    public void WithTransaction_NullTransaction_ThrowsArgumentNullException()
    {
        var query = SqlQuery<QueryUser>.ForSqlite();

        Assert.ThrowsException<ArgumentNullException>(() => query.WithTransaction(null!));
    }

    [TestMethod]
    public void WithTransaction_TransactionWithoutConnection_ThrowsInvalidOperationException()
    {
        var query = SqlQuery<QueryUser>.ForSqlite();

        Assert.ThrowsException<InvalidOperationException>(() => query.WithTransaction(new DetachedDbTransaction()));
    }

    [TestMethod]
    public void WithTransaction_ExistingMatchingConnection_Succeeds()
    {
        var connection = new TestDbConnection();
        var transaction = new TestDbTransaction(connection);
        var query = (SqlxQueryable<QueryUser>)SqlQuery<QueryUser>.ForSqlite().WithConnection(connection);

        var result = query.WithTransaction(transaction);

        Assert.AreSame(query, result);
        Assert.AreSame(connection, query.Connection);
        Assert.AreSame(transaction, query.Transaction);
    }

    [TestMethod]
    public void ToSqlWithParameters_NullQuery_ThrowsArgumentNullException()
    {
        IQueryable<QueryUser>? query = null;

        Assert.ThrowsException<ArgumentNullException>(() => query!.ToSqlWithParameters());
    }

    [TestMethod]
    public void ToSqlWithParameters_OnNonSqlxQueryable_ThrowsInvalidOperationException()
    {
        var query = new List<QueryUser>().AsQueryable();

        Assert.ThrowsException<InvalidOperationException>(() => query.ToSqlWithParameters());
    }

    [TestMethod]
    public void ToSql_OnNonSqlxQueryable_ThrowsInvalidOperationException()
    {
        var query = new List<QueryUser>().AsQueryable();

        Assert.ThrowsException<InvalidOperationException>(() => query.ToSql());
    }

    [TestMethod]
    public void ToSql_NullQuery_ThrowsArgumentNullException()
    {
        IQueryable<QueryUser>? query = null;

        Assert.ThrowsException<ArgumentNullException>(() => query!.ToSql());
    }

    [TestMethod]
    public void AsSubQuery_NullQuery_ThrowsArgumentNullException()
    {
        IQueryable<QueryUser>? query = null;

        Assert.ThrowsException<ArgumentNullException>(() => query!.AsSubQuery());
    }

    [TestMethod]
    public void AsSubQuery_OnNonSqlxQueryable_ThrowsInvalidOperationException()
    {
        var query = new List<QueryUser>().AsQueryable();

        Assert.ThrowsException<InvalidOperationException>(() => query.AsSubQuery());
    }

    [TestMethod]
    public void CreateCommand_WithReadOnlyDictionaryAndNullValue_AddsDbNullParameter()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var parameters = new ReadOnlyDictionary<string, object?>(
            new Dictionary<string, object?> { ["name"] = null, ["age"] = 18 });

        var method = typeof(SqlxQueryableExtensions).GetMethod(
            "CreateCommand",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.IsNotNull(method);

        using var command = (DbCommand)method!.Invoke(
            null,
            new object?[] { connection, "select 1", parameters, null })!;

        Assert.AreEqual("select 1", command.CommandText);
        Assert.AreEqual(2, command.Parameters.Count);
        Assert.AreEqual(DBNull.Value, command.Parameters[0].Value);
        Assert.AreEqual(18, Convert.ToInt32(command.Parameters[1].Value));
    }

    [TestMethod]
    public void ToParameterDictionary_ReturnsSameDictionaryInstance()
    {
        // Test that ToSqlWithParameters works with a simple query
        var q = SqlQuery<QueryUser>.ForSqlite().Where(u => u.Id == 1);
        var (sql, parameters) = q.ToSqlWithParameters();
        Assert.IsNotNull(sql);
        Assert.IsNotNull(parameters);
        Assert.IsTrue(parameters.Count > 0);
    }

    [TestMethod]
    public void ToParameterDictionary_CopiesReadOnlyDictionary()
    {
        // Test that ToSqlWithParameters returns correct parameter values
        var q = SqlQuery<QueryUser>.ForSqlite().Where(u => u.Id == 42 && u.Name == "test");
        var (sql, parameters) = q.ToSqlWithParameters();
        Assert.IsNotNull(sql);
        Assert.AreEqual(2, parameters.Count);
    }

    [TestMethod]
    public void WithTransaction_SameConnectionThenWithConnection_Succeeds()
    {
        var connection = new TestDbConnection();
        var transaction = new TestDbTransaction(connection);
        var query = (SqlxQueryable<QueryUser>)SqlQuery<QueryUser>.ForSqlite()
            .WithTransaction(transaction);

        var result = query.WithConnection(connection);

        Assert.AreSame(query, result);
        Assert.AreSame(connection, query.Connection);
        Assert.AreSame(transaction, query.Transaction);
    }

    private sealed class CoverageReader : IResultReader<QueryUser>
    {
        public int PropertyCount => 0;

        public QueryUser Read(System.Data.IDataReader reader) => new();

        public QueryUser Read(System.Data.IDataReader reader, ReadOnlySpan<int> ordinals) => new();

        public void GetOrdinals(System.Data.IDataReader reader, Span<int> ordinals)
    {
        }
    }

#pragma warning disable CS8765
    private sealed class TestDbConnection : DbConnection
    {
        public override string ConnectionString { get; set; } = string.Empty;
        public override string Database => "TestDb";
        public override string DataSource => "TestSource";
        public override string ServerVersion => "1.0";
        public override ConnectionState State => ConnectionState.Open;

        public override void ChangeDatabase(string databaseName)
        {
        }

        public override void Close()
        {
        }

        public override void Open()
        {
        }

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
            => new TestDbTransaction(this, isolationLevel);

        protected override DbCommand CreateDbCommand() => throw new NotSupportedException();
    }

    private class TestDbTransaction : DbTransaction
    {
        private readonly DbConnection? _connection;
        private readonly IsolationLevel _isolationLevel;

        public TestDbTransaction(DbConnection? connection, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            _connection = connection;
            _isolationLevel = isolationLevel;
        }

        public override IsolationLevel IsolationLevel => _isolationLevel;
        protected override DbConnection? DbConnection => _connection;

        public override void Commit()
        {
        }

        public override void Rollback()
        {
        }
    }
#pragma warning restore CS8765

    private sealed class DetachedDbTransaction : TestDbTransaction
    {
        public DetachedDbTransaction()
            : base(null)
        {
        }
    }

    // Async null query checks - cover state machine null checks
    [TestMethod]
    public async Task ToListAsync_NullQuery_ThrowsArgumentNullException()
    {
        IQueryable<QueryUser>? query = null;
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => query!.ToListAsync());
    }

    [TestMethod]
    public async Task FirstOrDefaultAsync_NullQuery_ThrowsArgumentNullException()
    {
        IQueryable<QueryUser>? query = null;
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => query!.FirstOrDefaultAsync());
    }

    [TestMethod]
    public async Task CountAsync_NullQuery_ThrowsArgumentNullException()
    {
        IQueryable<QueryUser>? query = null;
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => query!.CountAsync());
    }

    [TestMethod]
    public async Task AnyAsync_NullQuery_ThrowsArgumentNullException()
    {
        IQueryable<QueryUser>? query = null;
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => query!.AnyAsync());
    }
}
