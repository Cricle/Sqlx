// -----------------------------------------------------------------------
// <copyright file="SqlxQueryableExecutionTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
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
            var queryable = new SqlxQueryable<User>(new SqlxQueryProvider(_dialect!))
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
            var queryable = new SqlxQueryable<User>(new SqlxQueryProvider(_dialect!))
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
            var queryable = new SqlxQueryable<User>(new SqlxQueryProvider(_dialect!))
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
            var queryable = new SqlxQueryable<User>(new SqlxQueryProvider(_dialect!))
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
            var queryable = new SqlxQueryable<User>(new SqlxQueryProvider(_dialect!))
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
            var queryable = (SqlxQueryable<User>)new SqlxQueryable<User>(new SqlxQueryProvider(_dialect!))
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
            var queryable = System.Linq.Queryable.Where(
                new SqlxQueryable<User>(new SqlxQueryProvider(_dialect!)),
                u => u.Age >= 30);

            queryable = System.Linq.Queryable.OrderBy(queryable, u => u.Name);
            queryable = System.Linq.Queryable.Take(queryable, 10);

            var sql = queryable.ToSql();

            Assert.AreEqual("SELECT * FROM [User] WHERE [age] >= 30 ORDER BY [name] ASC LIMIT 10", sql);
        }

        [TestMethod]
        public void ToSqlWithParameters_GeneratesParameterizedSql()
        {
            var queryable = System.Linq.Queryable.Where(
                new SqlxQueryable<User>(new SqlxQueryProvider(_dialect!)),
                u => u.Age >= 30 && u.Name == "Bob");

            var (sql, parameters) = queryable.ToSqlWithParameters();

            Assert.AreEqual("SELECT * FROM [User] WHERE ([age] >= @p0 AND [name] = @p1)", sql);
            Assert.AreEqual(2, parameters.Count());
            Assert.AreEqual(30, parameters.ElementAt(0).Value);
            Assert.AreEqual("Bob", parameters.ElementAt(1).Value);
        }

        [TestMethod]
        public void MultipleWhere_CombinesConditions()
        {
            var queryable = new SqlxQueryable<User>(new SqlxQueryProvider(_dialect!))
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
            var queryable = new SqlxQueryable<User>(new SqlxQueryProvider(_dialect!))
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
            var queryable = new SqlxQueryable<User>(new SqlxQueryProvider(_dialect!))
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
            var queryable = new SqlxQueryable<User>(new SqlxQueryProvider(_dialect!))
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
            IAsyncEnumerable<User> queryable = new SqlxQueryable<User>(new SqlxQueryProvider(_dialect!))
                .WithConnection(_connection!)
                .WithReader(new UserReader())
                .Where(u => u.Age >= 30) as SqlxQueryable<User> ?? throw new InvalidOperationException();

            var users = await AsyncEnumerable.ToListAsync<User>(queryable);

            Assert.AreEqual(3, users.Count);
            Assert.AreEqual("Bob", users[0].Name);
            Assert.AreEqual("Charlie", users[1].Name);
            Assert.AreEqual("David", users[2].Name);
        }

        [TestMethod]
        public async Task AsyncLinq_FirstAsync_ReturnsFirstElement()
        {
            IAsyncEnumerable<User> queryable = new SqlxQueryable<User>(new SqlxQueryProvider(_dialect!))
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
            IAsyncEnumerable<User> queryable = new SqlxQueryable<User>(new SqlxQueryProvider(_dialect!))
                .WithConnection(_connection!)
                .WithReader(new UserReader()) as SqlxQueryable<User> ?? throw new InvalidOperationException();

            var filtered = AsyncEnumerable.Where<User>(queryable, u => u.Age > 100);
            var user = await AsyncEnumerable.FirstOrDefaultAsync<User>(filtered);

            Assert.IsNull(user);
        }

        [TestMethod]
        public async Task AsyncLinq_CountAsync_ReturnsCorrectCount()
        {
            IAsyncEnumerable<User> queryable = new SqlxQueryable<User>(new SqlxQueryProvider(_dialect!))
                .WithConnection(_connection!)
                .WithReader(new UserReader())
                .Where(u => u.Age >= 30) as SqlxQueryable<User> ?? throw new InvalidOperationException();

            var count = await AsyncEnumerable.CountAsync<User>(queryable);

            Assert.AreEqual(3, count);
        }

        [TestMethod]
        public async Task AsyncLinq_AnyAsync_ReturnsTrue()
        {
            IAsyncEnumerable<User> queryable = new SqlxQueryable<User>(new SqlxQueryProvider(_dialect!))
                .WithConnection(_connection!)
                .WithReader(new UserReader())
                .Where(u => u.Age >= 30) as SqlxQueryable<User> ?? throw new InvalidOperationException();

            var hasAny = await AsyncEnumerable.AnyAsync<User>(queryable);

            Assert.IsTrue(hasAny);
        }

        [TestMethod]
        public async Task AsyncLinq_AnyAsync_ReturnsFalse()
        {
            IAsyncEnumerable<User> queryable = new SqlxQueryable<User>(new SqlxQueryProvider(_dialect!))
                .WithConnection(_connection!)
                .WithReader(new UserReader())
                .Where(u => u.Age > 100) as SqlxQueryable<User> ?? throw new InvalidOperationException();

            var hasAny = await AsyncEnumerable.AnyAsync<User>(queryable);

            Assert.IsFalse(hasAny);
        }

        [TestMethod]
        public async Task AsyncLinq_WhereAsync_FiltersCorrectly()
        {
            IAsyncEnumerable<User> queryable = new SqlxQueryable<User>(new SqlxQueryProvider(_dialect!))
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
            IAsyncEnumerable<User> queryable = new SqlxQueryable<User>(new SqlxQueryProvider(_dialect!))
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
            IAsyncEnumerable<User> queryable = new SqlxQueryable<User>(new SqlxQueryProvider(_dialect!))
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
            IAsyncEnumerable<User> queryable = new SqlxQueryable<User>(new SqlxQueryProvider(_dialect!))
                .WithConnection(_connection!)
                .WithReader(new UserReader())
                .Where(u => u.Age >= 28)
                .OrderByDescending(u => u.Age)
                .Take(3) as SqlxQueryable<User> ?? throw new InvalidOperationException();

            var users = await AsyncEnumerable.ToListAsync<User>(queryable);

            Assert.AreEqual(3, users.Count);
            Assert.AreEqual("David", users[0].Name);
            Assert.AreEqual(40, users[0].Age);
            Assert.AreEqual("Charlie", users[1].Name);
            Assert.AreEqual(35, users[1].Age);
            Assert.AreEqual("Bob", users[2].Name);
            Assert.AreEqual(30, users[2].Age);
        }

        public class User
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public int Age { get; set; }
            public string? Email { get; set; }
        }

        private class UserReader : IResultReader<User>
        {
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

            public User Read(IDataReader reader, int[] ordinals)
            {
                return new User
                {
                    Id = reader.GetInt32(ordinals[0]),
                    Name = reader.GetString(ordinals[1]),
                    Age = reader.GetInt32(ordinals[2]),
                    Email = reader.IsDBNull(ordinals[3]) ? null : reader.GetString(ordinals[3])
                };
            }

            public int[] GetOrdinals(IDataReader reader)
            {
                return new[]
                {
                    reader.GetOrdinal("id"),
                    reader.GetOrdinal("name"),
                    reader.GetOrdinal("age"),
                    reader.GetOrdinal("email")
                };
            }
        }
    }
}
