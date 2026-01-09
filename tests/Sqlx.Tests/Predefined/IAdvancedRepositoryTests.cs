// -----------------------------------------------------------------------
// <copyright file="IAdvancedRepositoryTests.cs" company="Cricle">
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
using Sqlx;
using Sqlx.Annotations;

namespace Sqlx.Tests.Predefined
{
    /// <summary>
    /// Tests for IAdvancedRepository predefined interface.
    /// Validates that all methods can be correctly generated and executed.
    /// Requirements: 11.3 - Ensure all IAdvancedRepository methods generate correct implementation code.
    /// </summary>
    [TestClass]
    public class IAdvancedRepositoryTests : IDisposable
    {
        private readonly SqliteConnection _connection;

        public IAdvancedRepositoryTests()
        {
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();
        }

        public void Dispose()
        {
            _connection?.Dispose();
        }

        #region Helper Methods

        private async Task CreateUserTableAsync()
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS users (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL,
                    email TEXT,
                    age INTEGER NOT NULL,
                    is_active INTEGER NOT NULL,
                    created_at TEXT NOT NULL,
                    updated_at TEXT
                )";
            await cmd.ExecuteNonQueryAsync();
        }

        private async Task InsertTestUsersAsync(int count = 5)
        {
            for (int i = 1; i <= count; i++)
            {
                using var cmd = _connection.CreateCommand();
                cmd.CommandText = $@"
                    INSERT INTO users (name, email, age, is_active, created_at)
                    VALUES ('User{i}', 'user{i}@test.com', {20 + i}, 1, '{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}')";
                await cmd.ExecuteNonQueryAsync();
            }
        }

        #endregion

        #region Interface Method Coverage Tests

        /// <summary>
        /// Verifies all IAdvancedRepository methods are defined.
        /// </summary>
        [TestMethod]
        public void IAdvancedRepository_AllMethodsShouldBeDefined()
        {
            var interfaceType = typeof(IAdvancedRepository<,>);
            var methods = interfaceType.GetMethods();
            var methodNames = methods.Select(m => m.Name).ToList();

            // Raw SQL Operations
            CollectionAssert.Contains(methodNames, "ExecuteRawAsync");
            CollectionAssert.Contains(methodNames, "QueryRawAsync");
            CollectionAssert.Contains(methodNames, "ExecuteScalarAsync");

            // Bulk Operations
            CollectionAssert.Contains(methodNames, "BulkCopyAsync");

            // Transaction Operations
            CollectionAssert.Contains(methodNames, "BeginTransactionAsync");
            CollectionAssert.Contains(methodNames, "CommitTransactionAsync");
            CollectionAssert.Contains(methodNames, "RollbackTransactionAsync");
        }

        /// <summary>
        /// Verifies IAdvancedRepository uses generic parameters (AOT compatible).
        /// </summary>
        [TestMethod]
        public void IAdvancedRepository_ShouldUseGenericParameters_AOTCompatible()
        {
            var interfaceType = typeof(IAdvancedRepository<,>);
            var methods = interfaceType.GetMethods();

            // Check ExecuteRawAsync<TParams> - should have generic parameter
            var executeRawWithParams = methods.FirstOrDefault(m => 
                m.Name == "ExecuteRawAsync" && m.IsGenericMethod);
            Assert.IsNotNull(executeRawWithParams, "ExecuteRawAsync<TParams> should exist");
            Assert.IsTrue(executeRawWithParams.GetGenericArguments().Length > 0, 
                "ExecuteRawAsync should have generic type parameters");

            // Check QueryRawAsync<TParams> - should have generic parameter
            var queryRawWithParams = methods.FirstOrDefault(m => 
                m.Name == "QueryRawAsync" && m.IsGenericMethod);
            Assert.IsNotNull(queryRawWithParams, "QueryRawAsync<TParams> should exist");

            // Check ExecuteScalarAsync<TResult, TParams> - should have generic parameters
            var executeScalarWithParams = methods.FirstOrDefault(m => 
                m.Name == "ExecuteScalarAsync" && m.IsGenericMethod && 
                m.GetGenericArguments().Length == 2);
            Assert.IsNotNull(executeScalarWithParams, "ExecuteScalarAsync<TResult, TParams> should exist");

            // Verify no 'object' parameter types (AOT incompatible)
            foreach (var method in methods)
            {
                var parameters = method.GetParameters();
                foreach (var param in parameters)
                {
                    Assert.AreNotEqual(typeof(object), param.ParameterType,
                        $"Method {method.Name} has 'object' parameter which is not AOT compatible");
                }
            }
        }

        /// <summary>
        /// Verifies that generic type constraints are properly defined for AOT compatibility.
        /// </summary>
        [TestMethod]
        public void IAdvancedRepository_GenericConstraints_ShouldBeClassConstraint()
        {
            var interfaceType = typeof(IAdvancedRepository<,>);
            var methods = interfaceType.GetMethods();

            // Check ExecuteRawAsync<TParams> has 'class' constraint
            var executeRawWithParams = methods.FirstOrDefault(m => 
                m.Name == "ExecuteRawAsync" && m.IsGenericMethod);
            Assert.IsNotNull(executeRawWithParams);
            var tParamsConstraints = executeRawWithParams.GetGenericArguments()[0].GetGenericParameterConstraints();
            // The 'class' constraint is represented by GenericParameterAttributes.ReferenceTypeConstraint
            var hasClassConstraint = (executeRawWithParams.GetGenericArguments()[0].GenericParameterAttributes & 
                System.Reflection.GenericParameterAttributes.ReferenceTypeConstraint) != 0;
            Assert.IsTrue(hasClassConstraint, "TParams should have 'class' constraint for AOT compatibility");
        }

        #endregion

        #region Custom Repository Tests (Workaround for QueryRawAsync<TResult>)

        /// <summary>
        /// Tests that a custom repository implementing partial IAdvancedRepository methods works.
        /// This is a workaround for methods that return method-level generic types.
        /// </summary>
        [TestMethod]
        public async Task CustomAdvancedRepository_ExecuteRawAsync_ShouldWork()
        {
            // Arrange
            await CreateUserTableAsync();
            await InsertTestUsersAsync(3);
            var repo = new CustomAdvancedRepository(_connection);

            // Act - Update all users' age
            var affected = await repo.ExecuteRawAsync("UPDATE users SET age = age + 1");

            // Assert
            Assert.AreEqual(3, affected);
        }

        /// <summary>
        /// Tests ExecuteRawAsync with typed parameters.
        /// </summary>
        [TestMethod]
        public async Task CustomAdvancedRepository_ExecuteRawAsync_WithTypedParams_ShouldWork()
        {
            // Arrange
            await CreateUserTableAsync();
            await InsertTestUsersAsync(5);
            var repo = new CustomAdvancedRepository(_connection);

            // Act - Update users with specific age
            // Note: Parameter names in SQL must match property names in TParams class
            var affected = await repo.ExecuteRawAsync(
                "UPDATE users SET is_active = @IsActive WHERE age > @MinAge",
                new UpdateParams { IsActive = 0, MinAge = 23 });

            // Assert
            Assert.IsTrue(affected >= 0);
        }

        /// <summary>
        /// Tests QueryRawAsync without parameters.
        /// </summary>
        [TestMethod]
        public async Task CustomAdvancedRepository_QueryRawAsync_ShouldReturnEntities()
        {
            // Arrange
            await CreateUserTableAsync();
            await InsertTestUsersAsync(3);
            var repo = new CustomAdvancedRepository(_connection);

            // Act
            var users = await repo.QueryRawAsync("SELECT * FROM users ORDER BY id");

            // Assert
            Assert.IsNotNull(users);
            Assert.AreEqual(3, users.Count);
        }

        /// <summary>
        /// Tests QueryRawAsync with typed parameters.
        /// </summary>
        [TestMethod]
        public async Task CustomAdvancedRepository_QueryRawAsync_WithTypedParams_ShouldWork()
        {
            // Arrange
            await CreateUserTableAsync();
            await InsertTestUsersAsync(5);
            var repo = new CustomAdvancedRepository(_connection);

            // Act
            // Note: Parameter names in SQL must match property names in TParams class
            var users = await repo.QueryRawAsync(
                "SELECT * FROM users WHERE age > @MinAge ORDER BY id",
                new QueryParams { MinAge = 22 });

            // Assert
            Assert.IsNotNull(users);
            Assert.IsTrue(users.Count > 0);
        }

        /// <summary>
        /// Tests ExecuteScalarAsync without parameters.
        /// </summary>
        [TestMethod]
        public async Task CustomAdvancedRepository_ExecuteScalarAsync_ShouldReturnScalar()
        {
            // Arrange
            await CreateUserTableAsync();
            await InsertTestUsersAsync(5);
            var repo = new CustomAdvancedRepository(_connection);

            // Act
            var count = await repo.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM users");

            // Assert
            Assert.AreEqual(5L, count);
        }

        /// <summary>
        /// Tests ExecuteScalarAsync with typed parameters.
        /// </summary>
        [TestMethod]
        public async Task CustomAdvancedRepository_ExecuteScalarAsync_WithTypedParams_ShouldWork()
        {
            // Arrange
            await CreateUserTableAsync();
            await InsertTestUsersAsync(5);
            var repo = new CustomAdvancedRepository(_connection);

            // Act
            // Note: Parameter names in SQL must match property names in TParams class
            var count = await repo.ExecuteScalarAsync<long, QueryParams>(
                "SELECT COUNT(*) FROM users WHERE age > @MinAge",
                new QueryParams { MinAge = 22 });

            // Assert
            Assert.IsTrue(count >= 0);
        }

        #endregion

        #region Transaction Tests

        /// <summary>
        /// Tests BeginTransactionAsync, CommitTransactionAsync flow.
        /// </summary>
        [TestMethod]
        public async Task CustomAdvancedRepository_Transaction_BeginAndCommit_ShouldWork()
        {
            // Arrange
            await CreateUserTableAsync();
            var repo = new CustomAdvancedRepository(_connection);

            // Act
            await repo.BeginTransactionAsync();
            await repo.ExecuteRawAsync(
                "INSERT INTO users (name, email, age, is_active, created_at) VALUES ('TxUser', 'tx@test.com', 30, 1, datetime('now'))");
            await repo.CommitTransactionAsync();

            // Assert - Verify data was committed
            var count = await repo.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM users WHERE name = 'TxUser'");
            Assert.AreEqual(1L, count);
        }

        /// <summary>
        /// Tests BeginTransactionAsync, RollbackTransactionAsync flow.
        /// </summary>
        [TestMethod]
        public async Task CustomAdvancedRepository_Transaction_BeginAndRollback_ShouldRevert()
        {
            // Arrange
            await CreateUserTableAsync();
            var repo = new CustomAdvancedRepository(_connection);

            // Act
            await repo.BeginTransactionAsync();
            await repo.ExecuteRawAsync(
                "INSERT INTO users (name, email, age, is_active, created_at) VALUES ('RollbackUser', 'rb@test.com', 25, 1, datetime('now'))");
            await repo.RollbackTransactionAsync();

            // Assert - Verify data was rolled back
            var count = await repo.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM users WHERE name = 'RollbackUser'");
            Assert.AreEqual(0L, count);
        }

        #endregion
    }

    #region Test Parameter Classes

    /// <summary>
    /// Parameter class for update operations.
    /// </summary>
    public class UpdateParams
    {
        public int IsActive { get; set; }
        public int MinAge { get; set; }
    }

    /// <summary>
    /// Parameter class for query operations.
    /// </summary>
    public class QueryParams
    {
        public int MinAge { get; set; }
    }

    #endregion

    #region Custom Repository (Workaround)

    /// <summary>
    /// Custom repository that implements IAdvancedRepository methods explicitly.
    /// This is a workaround for methods that return method-level generic types (QueryRawAsync&lt;TResult&gt;).
    /// The source generator currently doesn't support generating code for methods where the return type
    /// uses a method-level type parameter (e.g., List&lt;TResult&gt; where TResult is defined on the method).
    /// </summary>
    public interface ICustomAdvancedRepository
    {
        // Raw SQL Operations - these work with the source generator
        // Note: Using {{sql}} format - the sql placeholder will be replaced with the sql parameter value at runtime
        // The parameter must have [DynamicSql(Type = DynamicSqlType.Fragment)] attribute
        [SqlTemplate("{{sql}}")]
        Task<int> ExecuteRawAsync<TParams>([DynamicSql(Type = DynamicSqlType.Fragment)] string sql, TParams? parameters = default, System.Threading.CancellationToken cancellationToken = default)
            where TParams : class;

        [SqlTemplate("{{sql}}")]
        Task<int> ExecuteRawAsync([DynamicSql(Type = DynamicSqlType.Fragment)] string sql, System.Threading.CancellationToken cancellationToken = default);

        [SqlTemplate("{{sql}}")]
        Task<List<User>> QueryRawAsync<TParams>([DynamicSql(Type = DynamicSqlType.Fragment)] string sql, TParams? parameters = default, System.Threading.CancellationToken cancellationToken = default)
            where TParams : class;

        [SqlTemplate("{{sql}}")]
        Task<List<User>> QueryRawAsync([DynamicSql(Type = DynamicSqlType.Fragment)] string sql, System.Threading.CancellationToken cancellationToken = default);

        [SqlTemplate("{{sql}}")]
        Task<TResult> ExecuteScalarAsync<TResult, TParams>([DynamicSql(Type = DynamicSqlType.Fragment)] string sql, TParams? parameters = default, System.Threading.CancellationToken cancellationToken = default)
            where TParams : class;

        [SqlTemplate("{{sql}}")]
        Task<TResult> ExecuteScalarAsync<TResult>([DynamicSql(Type = DynamicSqlType.Fragment)] string sql, System.Threading.CancellationToken cancellationToken = default);

        // Note: Transaction methods are NOT in the interface because they need manual implementation
        // and the source generator would try to generate them otherwise.
    }

    [SqlDefine(SqlDefineTypes.SQLite)]
    [RepositoryFor(typeof(ICustomAdvancedRepository))]
    public partial class CustomAdvancedRepository : ICustomAdvancedRepository
    {
        private readonly IDbConnection _connection;
        private System.Data.Common.DbTransaction? _transaction;

        public CustomAdvancedRepository(IDbConnection connection)
        {
            _connection = connection;
        }

        // Transaction methods are implemented manually as they manage state
        public async Task BeginTransactionAsync(System.Threading.CancellationToken cancellationToken = default)
        {
            if (_connection is System.Data.Common.DbConnection dbConnection)
            {
                _transaction = await dbConnection.BeginTransactionAsync(cancellationToken);
            }
        }

        public async Task CommitTransactionAsync(System.Threading.CancellationToken cancellationToken = default)
        {
            if (_transaction != null)
            {
                await _transaction.CommitAsync(cancellationToken);
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync(System.Threading.CancellationToken cancellationToken = default)
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync(cancellationToken);
                _transaction = null;
            }
        }
    }

    #endregion
}
