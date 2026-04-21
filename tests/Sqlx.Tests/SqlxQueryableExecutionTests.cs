// -----------------------------------------------------------------------
// <copyright file="SqlxQueryableExecutionTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

namespace Sqlx.Tests
{
    /// <summary>
    /// Execution tests for SqlxQueryable with real SQLite database.
    /// </summary>
    [TestClass]
    public class SqlxQueryableExecutionTests
    {
        private SqliteConnection? _connection;
        private SqlDialect? _dialect;

        [TestInitialize]
        public void Setup()
        {
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();
            _dialect = SqlDefine.SQLite;

            using var cmd = _connection.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE User (
                    id INTEGER PRIMARY KEY,
                    name TEXT NOT NULL,
                    age INTEGER NOT NULL,
                    email TEXT
                )";
            cmd.ExecuteNonQuery();

            cmd.CommandText = @"
                INSERT INTO User (id, name, age, email) VALUES
                (1, 'Alice', 25, 'alice@example.com'),
                (2, 'Bob', 30, 'bob@example.com'),
                (3, 'Charlie', 35, 'charlie@example.com'),
                (4, 'David', 40, 'david@example.com'),
                (5, 'Eve', 28, NULL)";
            cmd.ExecuteNonQuery();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _connection?.Dispose();
        }

        [TestMethod]
        public void ToList_ReturnsAllRecords()
        {
            var queryable = new SqlxQueryable<User>(new SqlxQueryProvider<User>(_dialect!))
                .WithConnection(_connection!)
                .WithReader(new UserReader());

            var users = queryable.ToList();

            Assert.IsNotNull(users);
            Assert.AreEqual(5, users.Count);
            Assert.AreEqual("Alice", users[0].Name);
            Assert.AreEqual(25, users[0].Age);
            Assert.AreEqual("alice@example.com", users[0].Email);
            Assert.AreEqual("Eve", users[4].Name);
            Assert.IsNull(users[4].Email);
        }

        [TestMethod]
        public void Where_FiltersCorrectly()
        {
            var queryable = new SqlxQueryable<User>(new SqlxQueryProvider<User>(_dialect!))
                .WithConnection(_connection!)
                .WithReader(new UserReader())
                .Where(u => u.Age >= 30);

            var users = queryable.ToList();

            Assert.AreEqual(3, users.Count);
            Assert.IsTrue(users.All(u => u.Age >= 30));
            Assert.AreEqual("Bob", users[0].Name);
            Assert.AreEqual("Charlie", users[1].Name);
            Assert.AreEqual("David", users[2].Name);
        }

        [TestMethod]
        public void OrderBy_SortsCorrectly()
        {
            var queryable = new SqlxQueryable<User>(new SqlxQueryProvider<User>(_dialect!))
                .WithConnection(_connection!)
                .WithReader(new UserReader())
                .OrderBy(u => u.Name);

            var users = queryable.ToList();

            Assert.AreEqual(5, users.Count);
            Assert.AreEqual("Alice", users[0].Name);
            Assert.AreEqual("Bob", users[1].Name);
            Assert.AreEqual("Charlie", users[2].Name);
            Assert.AreEqual("David", users[3].Name);
            Assert.AreEqual("Eve", users[4].Name);
        }

        [TestMethod]
        public void Take_LimitsResults()
        {
            var queryable = new SqlxQueryable<User>(new SqlxQueryProvider<User>(_dialect!))
                .WithConnection(_connection!)
                .WithReader(new UserReader())
                .Take(3);

            var users = queryable.ToList();

            Assert.AreEqual(3, users.Count);
            Assert.AreEqual("Alice", users[0].Name);
            Assert.AreEqual("Bob", users[1].Name);
            Assert.AreEqual("Charlie", users[2].Name);
        }

        [TestMethod]
        public void ComplexQuery_WhereOrderByTake()
        {
            var queryable = new SqlxQueryable<User>(new SqlxQueryProvider<User>(_dialect!))
                .WithConnection(_connection!)
                .WithReader(new UserReader())
                .Where(u => u.Age >= 28)
                .OrderByDescending(u => u.Age)
                .Take(2);

            var users = queryable.ToList();

            Assert.AreEqual(2, users.Count);
            Assert.AreEqual("David", users[0].Name);
            Assert.AreEqual(40, users[0].Age);
            Assert.AreEqual("Charlie", users[1].Name);
            Assert.AreEqual(35, users[1].Age);
        }

        [TestMethod]
        public async Task AsyncEnumeration_WorksCorrectly()
        {
            var queryable = (SqlxQueryable<User>)new SqlxQueryable<User>(new SqlxQueryProvider<User>(_dialect!))
                .WithConnection(_connection!)
                .WithReader(new UserReader())
                .Where(u => u.Age >= 30);

            var users = new List<User>();
            await foreach (var user in queryable)
            {
                users.Add(user);
            }

            Assert.AreEqual(3, users.Count);
            Assert.AreEqual("Bob", users[0].Name);
            Assert.AreEqual("Charlie", users[1].Name);
            Assert.AreEqual("David", users[2].Name);
        }

        [TestMethod]
        public void ToSql_GeneratesCorrectSql()
        {
            var queryable = SqlQuery<User>.ForSqlite()
                .Where(u => u.Age >= 30)
                .OrderBy(u => u.Name)
                .Take(10);

            var sql = queryable.ToSql();

            Assert.AreEqual("SELECT [id], [name], [age], [email] FROM [User] WHERE [age] >= 30 ORDER BY [name] ASC LIMIT 10", sql);
        }

        [TestMethod]
        public void ToSqlWithParameters_GeneratesParameterizedSql()
        {
            var queryable = SqlQuery<User>.ForSqlite()
                .Where(u => u.Age >= 30 && u.Name == "Bob");

            var (sql, parameters) = queryable.ToSqlWithParameters();

            Assert.AreEqual("SELECT [id], [name], [age], [email] FROM [User] WHERE ([age] >= @p0 AND [name] = @p1)", sql);
            Assert.AreEqual(2, parameters.Count());
            Assert.AreEqual(30, parameters.ElementAt(0).Value);
            Assert.AreEqual("Bob", parameters.ElementAt(1).Value);
        }

        [TestMethod]
        public void MultipleWhere_CombinesConditions()
        {
            var queryable = new SqlxQueryable<User>(new SqlxQueryProvider<User>(_dialect!))
                .WithConnection(_connection!)
                .WithReader(new UserReader())
                .Where(u => u.Age >= 25)
                .Where(u => u.Age <= 35);

            var users = queryable.ToList();

            // Alice (25), Bob (30), Charlie (35), Eve (28) = 4 users
            Assert.AreEqual(4, users.Count);
            Assert.IsTrue(users.All(u => u.Age >= 25 && u.Age <= 35));
        }

        [TestMethod]
        public void OrderByThenBy_SortsCorrectly()
        {
            var queryable = new SqlxQueryable<User>(new SqlxQueryProvider<User>(_dialect!))
                .WithConnection(_connection!)
                .WithReader(new UserReader())
                .OrderByDescending(u => u.Age)
                .ThenBy(u => u.Name);

            var users = queryable.ToList();

            Assert.AreEqual(5, users.Count);
            Assert.AreEqual("David", users[0].Name);
            Assert.AreEqual("Charlie", users[1].Name);
            Assert.AreEqual("Bob", users[2].Name);
        }

        [TestMethod]
        public void EmptyResult_ReturnsEmptyList()
        {
            var queryable = new SqlxQueryable<User>(new SqlxQueryProvider<User>(_dialect!))
                .WithConnection(_connection!)
                .WithReader(new UserReader())
                .Where(u => u.Age > 100);

            var users = queryable.ToList();

            Assert.IsNotNull(users);
            Assert.AreEqual(0, users.Count);
        }

        [TestMethod]
        public void NullValue_HandledCorrectly()
        {
            var queryable = new SqlxQueryable<User>(new SqlxQueryProvider<User>(_dialect!))
                .WithConnection(_connection!)
                .WithReader(new UserReader())
                .Where(u => u.Name == "Eve");

            var users = queryable.ToList();

            Assert.AreEqual(1, users.Count);
            Assert.AreEqual("Eve", users[0].Name);
            Assert.IsNull(users[0].Email);
        }

        [TestMethod]
        public async Task AsyncLinq_ToListAsync_WorksCorrectly()
        {
            var queryable = new SqlxQueryable<User>(new SqlxQueryProvider<User>(_dialect!))
                .WithConnection(_connection!)
                .WithReader(new UserReader())
                .Where(u => u.Age >= 30);

            var users = await queryable.ToListAsync();

            Assert.AreEqual(3, users.Count);
            Assert.AreEqual("Bob", users[0].Name);
            Assert.AreEqual("Charlie", users[1].Name);
            Assert.AreEqual("David", users[2].Name);
        }

        [TestMethod]
        public async Task AsyncLinq_FirstAsync_ReturnsFirstElement()
        {
            IAsyncEnumerable<User> queryable = new SqlxQueryable<User>(new SqlxQueryProvider<User>(_dialect!))
                .WithConnection(_connection!)
                .WithReader(new UserReader())
                .OrderBy(u => u.Name) as SqlxQueryable<User> ?? throw new InvalidOperationException();

            var filtered = AsyncEnumerable.Where<User>(queryable, u => u.Age >= 30);
            var user = await AsyncEnumerable.FirstAsync<User>(filtered);

            Assert.IsNotNull(user);
            Assert.AreEqual("Bob", user.Name);
            Assert.AreEqual(30, user.Age);
        }

        [TestMethod]
        public async Task AsyncLinq_FirstOrDefaultAsync_ReturnsNull()
        {
            var queryable = new SqlxQueryable<User>(new SqlxQueryProvider<User>(_dialect!))
                .WithConnection(_connection!)
                .WithReader(new UserReader())
                .Where(u => u.Age > 100);

            var user = await queryable.FirstOrDefaultAsync();

            Assert.IsNull(user);
        }

        [TestMethod]
        public async Task AsyncLinq_CountAsync_ReturnsCorrectCount()
        {
            var queryable = new SqlxQueryable<User>(new SqlxQueryProvider<User>(_dialect!))
                .WithConnection(_connection!)
                .WithReader(new UserReader())
                .Where(u => u.Age >= 30);

            var count = await queryable.CountAsync();

            Assert.AreEqual(3L, count);
        }

        [TestMethod]
        public async Task AsyncLinq_AnyAsync_ReturnsTrue()
        {
            var queryable = new SqlxQueryable<User>(new SqlxQueryProvider<User>(_dialect!))
                .WithConnection(_connection!)
                .WithReader(new UserReader())
                .Where(u => u.Age >= 30);

            var hasAny = await queryable.AnyAsync();

            Assert.IsTrue(hasAny);
        }

        [TestMethod]
        public async Task AsyncLinq_AnyAsync_ReturnsFalse()
        {
            var queryable = new SqlxQueryable<User>(new SqlxQueryProvider<User>(_dialect!))
                .WithConnection(_connection!)
                .WithReader(new UserReader())
                .Where(u => u.Age > 100);

            var hasAny = await queryable.AnyAsync();

            Assert.IsFalse(hasAny);
        }

        [TestMethod]
        public async Task AsyncLinq_WhereAsync_FiltersCorrectly()
        {
            IAsyncEnumerable<User> queryable = new SqlxQueryable<User>(new SqlxQueryProvider<User>(_dialect!))
                .WithConnection(_connection!)
                .WithReader(new UserReader())
                .Where(u => u.Age >= 25)
                .Where(u => u.Age <= 35) as SqlxQueryable<User> ?? throw new InvalidOperationException();

            var users = await AsyncEnumerable.ToListAsync<User>(queryable);

            Assert.AreEqual(4, users.Count);
            Assert.IsTrue(users.All(u => u.Age >= 25 && u.Age <= 35));
        }

        [TestMethod]
        public async Task AsyncLinq_SelectAsync_ProjectsCorrectly()
        {
            IAsyncEnumerable<User> queryable = new SqlxQueryable<User>(new SqlxQueryProvider<User>(_dialect!))
                .WithConnection(_connection!)
                .WithReader(new UserReader())
                .Where(u => u.Age >= 30) as SqlxQueryable<User> ?? throw new InvalidOperationException();

            var selected = AsyncEnumerable.Select<User, string>(queryable, u => u.Name);
            var names = await AsyncEnumerable.ToListAsync<string>(selected);

            Assert.AreEqual(3, names.Count);
            Assert.IsTrue(names.Contains("Bob"));
            Assert.IsTrue(names.Contains("Charlie"));
            Assert.IsTrue(names.Contains("David"));
        }

        [TestMethod]
        public async Task AsyncLinq_TakeAsync_LimitsResults()
        {
            IAsyncEnumerable<User> queryable = new SqlxQueryable<User>(new SqlxQueryProvider<User>(_dialect!))
                .WithConnection(_connection!)
                .WithReader(new UserReader())
                .OrderBy(u => u.Age) as SqlxQueryable<User> ?? throw new InvalidOperationException();

            var taken = AsyncEnumerable.Take<User>(queryable, 3);
            var users = await AsyncEnumerable.ToListAsync<User>(taken);

            Assert.AreEqual(3, users.Count);
            Assert.AreEqual("Alice", users[0].Name);
            Assert.AreEqual("Eve", users[1].Name);
            Assert.AreEqual("Bob", users[2].Name);
        }

        [TestMethod]
        public async Task AsyncLinq_ComplexQuery_WorksCorrectly()
        {
            var queryable = new SqlxQueryable<User>(new SqlxQueryProvider<User>(_dialect!))
                .WithConnection(_connection!)
                .WithReader(new UserReader())
                .Where(u => u.Age >= 28)
                .OrderByDescending(u => u.Age)
                .Take(3);

            var users = await queryable.ToListAsync();

            Assert.AreEqual(3, users.Count);
            Assert.AreEqual("David", users[0].Name);
            Assert.AreEqual(40, users[0].Age);
            Assert.AreEqual("Charlie", users[1].Name);
            Assert.AreEqual(35, users[1].Age);
            Assert.AreEqual("Bob", users[2].Name);
            Assert.AreEqual(30, users[2].Age);
        }

        [TestMethod]
        public async Task SqlxQueryableExtensions_ToListAsync_WithClosedConnection_OpensAndClosesConnection()
        {
            using var keeper = new SqliteConnection("Data Source=sqlx_queryable_tolistasync;Mode=Memory;Cache=Shared");
            keeper.Open();

            using (var cmd = keeper.CreateCommand())
            {
                cmd.CommandText = @"
                    CREATE TABLE User (
                        id INTEGER PRIMARY KEY,
                        name TEXT NOT NULL,
                        age INTEGER NOT NULL,
                        email TEXT
                    )";
                cmd.ExecuteNonQuery();

                cmd.CommandText = @"
                    INSERT INTO User (id, name, age, email) VALUES
                    (1, 'Alice', 25, 'alice@example.com'),
                    (2, 'Bob', 30, 'bob@example.com')";
                cmd.ExecuteNonQuery();
            }

            using var connection = new SqliteConnection("Data Source=sqlx_queryable_tolistasync;Mode=Memory;Cache=Shared");
            connection.Open();

            connection.Close();

            var queryable = new SqlxQueryable<User>(new SqlxQueryProvider<User>(SqlDefine.SQLite))
                .WithConnection(connection)
                .WithReader(new UserReader());

            var users = await queryable.ToListAsync();

            Assert.AreEqual(2, users.Count);
            Assert.AreEqual(ConnectionState.Closed, connection.State);
        }

        [TestMethod]
        public async Task SqlxQueryableExtensions_CountAndAnyAsync_WithClosedConnection_WorkCorrectly()
        {
            using var keeper = new SqliteConnection("Data Source=sqlx_queryable_countany;Mode=Memory;Cache=Shared");
            keeper.Open();

            using (var cmd = keeper.CreateCommand())
            {
                cmd.CommandText = @"
                    CREATE TABLE User (
                        id INTEGER PRIMARY KEY,
                        name TEXT NOT NULL,
                        age INTEGER NOT NULL,
                        email TEXT
                    )";
                cmd.ExecuteNonQuery();

                cmd.CommandText = @"
                    INSERT INTO User (id, name, age, email) VALUES
                    (1, 'Alice', 25, 'alice@example.com'),
                    (2, 'Bob', 30, 'bob@example.com')";
                cmd.ExecuteNonQuery();
            }

            using var connection = new SqliteConnection("Data Source=sqlx_queryable_countany;Mode=Memory;Cache=Shared");
            connection.Open();

            connection.Close();

            var queryable = new SqlxQueryable<User>(new SqlxQueryProvider<User>(SqlDefine.SQLite))
                .WithConnection(connection)
                .WithReader(new UserReader())
                .Where(u => u.Age >= 30);

            var count = await queryable.CountAsync();
            var any = await queryable.AnyAsync();

            Assert.AreEqual(1L, count);
            Assert.IsTrue(any);
            Assert.AreEqual(ConnectionState.Closed, connection.State);
        }

        [TestMethod]
        public async Task CountAsync_SimpleQuery_UsesDirectCountSql()
        {
            var connection = new CapturingDbConnection();
            var queryable = new SqlxQueryable<User>(new SqlxQueryProvider<User>(SqlDefine.SQLite))
                .WithConnection(connection)
                .Where(u => u.Age >= 30)
                .OrderBy(u => u.Name);

            var count = await queryable.CountAsync();

            Assert.AreEqual(1L, count);
            Assert.IsNotNull(connection.LastCreatedCommand);
            Assert.AreEqual("SELECT COUNT(*) FROM [User] WHERE [age] >= @p0", connection.LastCreatedCommand.CommandText);
            Assert.AreEqual(1, connection.LastCreatedCommand.ParameterCount);
        }

        [TestMethod]
        public async Task AnyAsync_SimpleQuery_UsesDirectExistsSql()
        {
            var connection = new CapturingDbConnection();
            var queryable = new SqlxQueryable<User>(new SqlxQueryProvider<User>(SqlDefine.SQLite))
                .WithConnection(connection)
                .Where(u => u.Age >= 30)
                .OrderBy(u => u.Name);

            var any = await queryable.AnyAsync();

            Assert.IsTrue(any);
            Assert.IsNotNull(connection.LastCreatedCommand);
            Assert.AreEqual("SELECT 1 FROM [User] WHERE [age] >= @p0 LIMIT 1", connection.LastCreatedCommand.CommandText);
            Assert.AreEqual(1, connection.LastCreatedCommand.ParameterCount);
        }

        [TestMethod]
        public async Task CountAsync_PaginatedQuery_UsesWrappedSubquerySql()
        {
            var connection = new CapturingDbConnection();
            var queryable = new SqlxQueryable<User>(new SqlxQueryProvider<User>(SqlDefine.SQLite))
                .WithConnection(connection)
                .OrderBy(u => u.Name)
                .Skip(5)
                .Take(10);

            var count = await queryable.CountAsync();

            Assert.AreEqual(1L, count);
            Assert.IsNotNull(connection.LastCreatedCommand);
            Assert.AreEqual("SELECT COUNT(*) FROM (SELECT [id], [name], [age], [email] FROM [User] ORDER BY [name] ASC LIMIT 10 OFFSET 5) AS q", connection.LastCreatedCommand.CommandText);
        }

        [TestMethod]
        public async Task AnyAsync_DistinctQuery_UsesWrappedSubquerySql()
        {
            var connection = new CapturingDbConnection();
            var queryable = new SqlxQueryable<User>(new SqlxQueryProvider<User>(SqlDefine.SQLite))
                .WithConnection(connection)
                .Select(u => u.Name)
                .Distinct();

            var any = await queryable.AnyAsync();

            Assert.IsTrue(any);
            Assert.IsNotNull(connection.LastCreatedCommand);
            Assert.AreEqual("SELECT 1 FROM (SELECT DISTINCT [name] FROM [User]) AS q", connection.LastCreatedCommand.CommandText);
        }

        [TestMethod]
        public void Any_SimpleQuery_UsesDirectExistsSql()
        {
            var connection = new CapturingDbConnection();
            var queryable = new SqlxQueryable<User>(new SqlxQueryProvider<User>(SqlDefine.SQLite))
                .WithConnection(connection)
                .Where(u => u.Age >= 30)
                .OrderBy(u => u.Name);

            var any = queryable.Any();

            Assert.IsTrue(any);
            Assert.IsNotNull(connection.LastCreatedCommand);
            Assert.AreEqual("SELECT 1 FROM [User] WHERE [age] >= @p0 LIMIT 1", connection.LastCreatedCommand.CommandText);
            Assert.AreEqual(1, connection.LastCreatedCommand.ParameterCount);
        }

        [TestMethod]
        public void Any_WithPredicate_UsesDirectExistsSql()
        {
            var connection = new CapturingDbConnection();
            var queryable = new SqlxQueryable<User>(new SqlxQueryProvider<User>(SqlDefine.SQLite))
                .WithConnection(connection);

            var any = queryable.Any(u => u.Age >= 30);

            Assert.IsTrue(any);
            Assert.IsNotNull(connection.LastCreatedCommand);
            Assert.AreEqual("SELECT 1 FROM [User] WHERE [age] >= @p0 LIMIT 1", connection.LastCreatedCommand.CommandText);
            Assert.AreEqual(1, connection.LastCreatedCommand.ParameterCount);
        }

        [Sqlx]
        public partial class User
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public int Age { get; set; }
            public string? Email { get; set; }
        }

        private class UserReader : IResultReader<User>
        {
            public int PropertyCount => 4;

            public User Read(IDataReader reader)
            {
                return new User
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Age = reader.GetInt32(2),
                    Email = reader.IsDBNull(3) ? null : reader.GetString(3)
                };
            }

            public User Read(IDataReader reader, ReadOnlySpan<int> ordinals)
            {
                return new User
                {
                    Id = reader.GetInt32(ordinals[0]),
                    Name = reader.GetString(ordinals[1]),
                    Age = reader.GetInt32(ordinals[2]),
                    Email = reader.IsDBNull(ordinals[3]) ? null : reader.GetString(ordinals[3])
                };
            }

            public void GetOrdinals(IDataReader reader, Span<int> ordinals)
            {
                ordinals[0] = reader.GetOrdinal("id");
                ordinals[1] = reader.GetOrdinal("name");
                ordinals[2] = reader.GetOrdinal("age");
                ordinals[3] = reader.GetOrdinal("email");
            }
        }

#pragma warning disable CS8765
        private sealed class CapturingDbConnection : DbConnection
        {
            public CapturingDbCommand? LastCreatedCommand { get; private set; }

            public override string ConnectionString { get; set; } = string.Empty;
            public override string Database => "Captured";
            public override string DataSource => "Captured";
            public override string ServerVersion => "1.0";
            public override ConnectionState State => ConnectionState.Open;

            public override void ChangeDatabase(string databaseName) { }
            public override void Close() { }
            public override void Open() { }
            protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => throw new NotImplementedException();

            protected override DbCommand CreateDbCommand()
            {
                LastCreatedCommand = new CapturingDbCommand { Connection = this };
                return LastCreatedCommand;
            }
        }

        private sealed class CapturingDbCommand : DbCommand
        {
            public int ParameterCount => DbParameterCollection.Count;

            public override string CommandText { get; set; } = string.Empty;
            public override int CommandTimeout { get; set; }
            public override CommandType CommandType { get; set; }
            public override bool DesignTimeVisible { get; set; }
            public override UpdateRowSource UpdatedRowSource { get; set; }
            protected override DbConnection? DbConnection { get; set; }
            protected override DbParameterCollection DbParameterCollection { get; } = new CapturingParameterCollection();
            protected override DbTransaction? DbTransaction { get; set; }

            public override void Cancel() { }
            public override int ExecuteNonQuery() => 0;
            public override object? ExecuteScalar() => 1L;
            public override void Prepare() { }
            protected override DbParameter CreateDbParameter() => new CapturingDbParameter();
            protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => throw new NotSupportedException();
        }

        private sealed class CapturingParameterCollection : DbParameterCollection
        {
            private readonly List<DbParameter> _parameters = new();

            public override int Count => _parameters.Count;
            public override object SyncRoot => _parameters;
            public override int Add(object value)
            {
                _parameters.Add((DbParameter)value);
                return _parameters.Count - 1;
            }

            public override void AddRange(Array values)
            {
                foreach (var value in values)
                {
                    _parameters.Add((DbParameter)value);
                }
            }

            public override void Clear() => _parameters.Clear();
            public override bool Contains(object value) => _parameters.Contains((DbParameter)value);
            public override bool Contains(string value) => _parameters.Any(p => p.ParameterName == value);
            public override void CopyTo(Array array, int index) => _parameters.ToArray().CopyTo(array, index);
            public override System.Collections.IEnumerator GetEnumerator() => _parameters.GetEnumerator();
            public override int IndexOf(object value) => _parameters.IndexOf((DbParameter)value);
            public override int IndexOf(string parameterName) => _parameters.FindIndex(p => p.ParameterName == parameterName);
            public override void Insert(int index, object value) => _parameters.Insert(index, (DbParameter)value);
            public override void Remove(object value) => _parameters.Remove((DbParameter)value);
            public override void RemoveAt(int index) => _parameters.RemoveAt(index);
            public override void RemoveAt(string parameterName)
            {
                var index = IndexOf(parameterName);
                if (index >= 0)
                {
                    _parameters.RemoveAt(index);
                }
            }

            protected override DbParameter GetParameter(int index) => _parameters[index];
            protected override DbParameter GetParameter(string parameterName) => _parameters[IndexOf(parameterName)];
            protected override void SetParameter(int index, DbParameter value) => _parameters[index] = value;
            protected override void SetParameter(string parameterName, DbParameter value)
            {
                var index = IndexOf(parameterName);
                if (index >= 0)
                {
                    _parameters[index] = value;
                }
                else
                {
                    _parameters.Add(value);
                }
            }
        }

        private sealed class CapturingDbParameter : DbParameter
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
    }
}
