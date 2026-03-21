// <copyright file="SqlxQueryableTransactionTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace Sqlx.Tests.QueryBuilder;

[TestClass]
public class SqlxQueryableTransactionTests
{
    private SqlDialect _dialect = null!;
    private DynamicEntityProvider<TransactionEntity> _entityProvider = null!;
    private TrackingDbConnection _connection = null!;
    private TrackingResultReader _reader = null!;

    [TestInitialize]
    public void Setup()
    {
        _dialect = new SqlServerDialect();
        _entityProvider = new DynamicEntityProvider<TransactionEntity>();
        _connection = new TrackingDbConnection();
        _reader = new TrackingResultReader();
    }

    [TestMethod]
    public void WithTransaction_WithoutConnection_UsesTransactionConnection()
    {
        var transaction = new TrackingDbTransaction(_connection, IsolationLevel.ReadCommitted);

        var query = SqlQuery<TransactionEntity>.For(_dialect, _entityProvider)
            .WithReader(_reader)
            .WithTransaction(transaction);

        var sqlxQuery = (SqlxQueryable<TransactionEntity>)query;
        Assert.AreSame(_connection, sqlxQuery.Connection);
        Assert.AreSame(transaction, sqlxQuery.Transaction);
    }

    [TestMethod]
    public void WithTransaction_WithDifferentExistingConnection_ThrowsInvalidOperationException()
    {
        var otherConnection = new TrackingDbConnection();
        var transaction = new TrackingDbTransaction(_connection, IsolationLevel.ReadCommitted);

        var query = SqlQuery<TransactionEntity>.For(_dialect, _entityProvider)
            .WithConnection(otherConnection);

        Assert.ThrowsException<InvalidOperationException>(() => query.WithTransaction(transaction));
    }

    [TestMethod]
    public void WithConnection_WithDifferentExistingTransactionConnection_ThrowsInvalidOperationException()
    {
        var transaction = new TrackingDbTransaction(_connection, IsolationLevel.ReadCommitted);

        var query = SqlQuery<TransactionEntity>.For(_dialect, _entityProvider)
            .WithTransaction(transaction);

        Assert.ThrowsException<InvalidOperationException>(() => query.WithConnection(new TrackingDbConnection()));
    }

    [TestMethod]
    public void AsSubQuery_PreservesTransaction()
    {
        var transaction = new TrackingDbTransaction(_connection, IsolationLevel.ReadCommitted);

        var query = SqlQuery<TransactionEntity>.For(_dialect, _entityProvider)
            .WithReader(_reader)
            .WithTransaction(transaction);

        var subQuery = (SqlxQueryable<TransactionEntity>)query.AsSubQuery();

        Assert.AreSame(transaction, subQuery.Transaction);
        Assert.AreSame(_connection, subQuery.Connection);
    }

    [TestMethod]
    public void Enumeration_WithTransaction_AssignsCommandTransaction()
    {
        var transaction = new TrackingDbTransaction(_connection, IsolationLevel.ReadCommitted);

        var query = SqlQuery<TransactionEntity>.For(_dialect, _entityProvider)
            .WithReader(_reader)
            .WithTransaction(transaction);

        var result = query.ToList();

        Assert.AreEqual(1, result.Count);
        Assert.AreSame(transaction, _connection.LastCreatedCommand?.AssignedTransaction);
    }

    [TestMethod]
    public async Task AsyncEnumeration_WithTransaction_AssignsCommandTransaction()
    {
        var transaction = new TrackingDbTransaction(_connection, IsolationLevel.ReadCommitted);

        var query = (SqlxQueryable<TransactionEntity>)SqlQuery<TransactionEntity>.For(_dialect, _entityProvider)
            .WithReader(_reader)
            .WithTransaction(transaction);

        await using var enumerator = query.GetAsyncEnumerator();
        Assert.IsTrue(await enumerator.MoveNextAsync());
        Assert.AreEqual(1, enumerator.Current.Id);
        Assert.AreSame(transaction, _connection.LastCreatedCommand?.AssignedTransaction);
    }

    [TestMethod]
    public void First_WithTransaction_AssignsCommandTransaction()
    {
        var transaction = new TrackingDbTransaction(_connection, IsolationLevel.ReadCommitted);

        var entity = SqlQuery<TransactionEntity>.For(_dialect, _entityProvider)
            .WithReader(_reader)
            .WithTransaction(transaction)
            .First();

        Assert.AreEqual(1, entity.Id);
        Assert.AreSame(transaction, _connection.LastCreatedCommand?.AssignedTransaction);
    }

    [TestMethod]
    public void Count_WithTransaction_AssignsCommandTransaction()
    {
        var transaction = new TrackingDbTransaction(_connection, IsolationLevel.ReadCommitted);

        var count = SqlQuery<TransactionEntity>.For(_dialect, _entityProvider)
            .WithReader(_reader)
            .WithTransaction(transaction)
            .Count();

        Assert.AreEqual(1, count);
        Assert.AreSame(transaction, _connection.LastCreatedCommand?.AssignedTransaction);
    }

    [TestMethod]
    public void Aggregate_WithTransaction_AssignsCommandTransaction()
    {
        var transaction = new TrackingDbTransaction(_connection, IsolationLevel.ReadCommitted);

        var maxId = SqlQuery<TransactionEntity>.For(_dialect, _entityProvider)
            .WithReader(_reader)
            .WithTransaction(transaction)
            .Max(entity => entity.Id);

        Assert.AreEqual(1, maxId);
        Assert.AreSame(transaction, _connection.LastCreatedCommand?.AssignedTransaction);
    }

    public sealed class TransactionEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private sealed class TrackingResultReader : IResultReader<TransactionEntity>
    {
        public int PropertyCount => 2;

        public TransactionEntity Read(IDataReader reader)
        {
            return new TransactionEntity
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
            };
        }

        public TransactionEntity Read(IDataReader reader, ReadOnlySpan<int> ordinals)
        {
            return new TransactionEntity
            {
                Id = reader.GetInt32(ordinals[0]),
                Name = reader.GetString(ordinals[1]),
            };
        }

        public void GetOrdinals(IDataReader reader, Span<int> ordinals)
        {
            ordinals[0] = reader.GetOrdinal("Id");
            ordinals[1] = reader.GetOrdinal("Name");
        }
    }

#pragma warning disable CS8765
    private sealed class TrackingDbConnection : DbConnection
    {
        public TrackingDbCommand? LastCreatedCommand { get; private set; }

        public override string ConnectionString { get; set; } = string.Empty;
        public override string Database => "TestDb";
        public override string DataSource => "TestSource";
        public override string ServerVersion => "1.0";
        public override ConnectionState State => ConnectionState.Open;

        public override void ChangeDatabase(string databaseName) { }
        public override void Close() { }
        public override void Open() { }

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
            return new TrackingDbTransaction(this, isolationLevel);
        }

        protected override DbCommand CreateDbCommand()
        {
            LastCreatedCommand = new TrackingDbCommand { Connection = this };
            return LastCreatedCommand;
        }
    }

    private sealed class TrackingDbTransaction : DbTransaction
    {
        private readonly TrackingDbConnection _connection;
        private readonly IsolationLevel _isolationLevel;

        public TrackingDbTransaction(TrackingDbConnection connection, IsolationLevel isolationLevel)
        {
            _connection = connection;
            _isolationLevel = isolationLevel;
        }

        public override IsolationLevel IsolationLevel => _isolationLevel;
        protected override DbConnection DbConnection => _connection;
        public override void Commit() { }
        public override void Rollback() { }
    }

    private sealed class TrackingDbCommand : DbCommand
    {
        public DbTransaction? AssignedTransaction => DbTransaction;

        public override string CommandText { get; set; } = string.Empty;
        public override int CommandTimeout { get; set; }
        public override CommandType CommandType { get; set; }
        public override bool DesignTimeVisible { get; set; }
        public override UpdateRowSource UpdatedRowSource { get; set; }
        protected override DbConnection? DbConnection { get; set; }
        protected override DbParameterCollection DbParameterCollection { get; } = new TrackingParameterCollection();
        protected override DbTransaction? DbTransaction { get; set; }

        public override void Cancel() { }
        public override int ExecuteNonQuery() => 0;
        public override object? ExecuteScalar() => 1;
        public override void Prepare() { }

        protected override DbParameter CreateDbParameter() => new TrackingDbParameter();

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            return new TrackingDbDataReader();
        }
    }

    private sealed class TrackingParameterCollection : DbParameterCollection
    {
        private readonly List<object> _parameters = new();

        public override int Count => _parameters.Count;
        public override object SyncRoot => _parameters;
        public override int Add(object value)
        {
            _parameters.Add(value);
            return _parameters.Count - 1;
        }

        public override void AddRange(Array values)
        {
            foreach (var value in values)
            {
                _parameters.Add(value);
            }
        }

        public override void Clear() => _parameters.Clear();
        public override bool Contains(object value) => _parameters.Contains(value);
        public override bool Contains(string value) => false;
        public override void CopyTo(Array array, int index) => _parameters.CopyTo((object[])array, index);
        public override System.Collections.IEnumerator GetEnumerator() => _parameters.GetEnumerator();
        public override int IndexOf(object value) => _parameters.IndexOf(value);
        public override int IndexOf(string parameterName) => -1;
        public override void Insert(int index, object value) => _parameters.Insert(index, value);
        public override void Remove(object value) => _parameters.Remove(value);
        public override void RemoveAt(int index) => _parameters.RemoveAt(index);
        public override void RemoveAt(string parameterName) { }
        protected override DbParameter GetParameter(int index) => (DbParameter)_parameters[index];
        protected override DbParameter GetParameter(string parameterName) => throw new NotImplementedException();
        protected override void SetParameter(int index, DbParameter value) => _parameters[index] = value;
        protected override void SetParameter(string parameterName, DbParameter value) { }
    }

    private sealed class TrackingDbParameter : DbParameter
    {
        public override DbType DbType { get; set; }
        public override ParameterDirection Direction { get; set; }
        public override bool IsNullable { get; set; }
        public override string ParameterName { get; set; } = string.Empty;
        public override string SourceColumn { get; set; } = string.Empty;
        public override object? Value { get; set; }
        public override bool SourceColumnNullMapping { get; set; }
        public override int Size { get; set; }
        public override void ResetDbType() { }
    }
#pragma warning restore CS8765

    private sealed class TrackingDbDataReader : DbDataReader
    {
        private int _readCount;

        public override int FieldCount => 2;
        public override int RecordsAffected => 0;
        public override bool HasRows => true;
        public override bool IsClosed => false;
        public override int Depth => 0;
        public override object this[int ordinal] => ordinal == 0 ? 1 : "Tracked";
        public override object this[string name] => name == "Id" ? 1 : "Tracked";
        public override bool GetBoolean(int ordinal) => true;
        public override byte GetByte(int ordinal) => 1;
        public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length) => 0;
        public override char GetChar(int ordinal) => 'A';
        public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length) => 0;
        public override string GetDataTypeName(int ordinal) => "int";
        public override DateTime GetDateTime(int ordinal) => DateTime.UtcNow;
        public override decimal GetDecimal(int ordinal) => 1m;
        public override double GetDouble(int ordinal) => 1d;
        public override Type GetFieldType(int ordinal) => ordinal == 0 ? typeof(int) : typeof(string);
        public override float GetFloat(int ordinal) => 1f;
        public override Guid GetGuid(int ordinal) => Guid.Empty;
        public override short GetInt16(int ordinal) => 1;
        public override int GetInt32(int ordinal) => 1;
        public override long GetInt64(int ordinal) => 1;
        public override string GetName(int ordinal) => ordinal == 0 ? "Id" : "Name";
        public override int GetOrdinal(string name) => name == "Id" ? 0 : 1;
        public override string GetString(int ordinal) => "Tracked";
        public override object GetValue(int ordinal) => ordinal == 0 ? 1 : "Tracked";
        public override int GetValues(object[] values) => 2;
        public override bool IsDBNull(int ordinal) => false;
        public override bool NextResult() => false;

        public override bool Read()
        {
            if (_readCount == 0)
            {
                _readCount++;
                return true;
            }

            return false;
        }

        public override System.Collections.IEnumerator GetEnumerator() => throw new NotImplementedException();
    }
}
