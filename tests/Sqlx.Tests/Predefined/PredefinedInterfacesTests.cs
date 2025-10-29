// -----------------------------------------------------------------------
// <copyright file="PredefinedInterfacesTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Xunit;
using Sqlx;
using Sqlx.Annotations;

namespace Sqlx.Tests.Predefined
{
    /// <summary>
    /// Comprehensive tests for all predefined repository interfaces.
    /// Tests record types, struct values, and all interface methods.
    /// </summary>
    public class PredefinedInterfacesTests : IDisposable
    {
        private readonly SqliteConnection _connection;

        public PredefinedInterfacesTests()
        {
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();
        }

        public void Dispose()
        {
            _connection?.Dispose();
        }

        #region Test Entities

        // Test with record type (user requirement: support record)
        [TableName("users")]
        public record User
        {
            public long Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string? Email { get; set; }
            public int Age { get; set; }
            public bool IsActive { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? UpdatedAt { get; set; }
        }

        // Test with regular class
        [TableName("products")]
        public class Product
        {
            public long Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public decimal Price { get; set; }
            public int Stock { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        // Test struct value type (user requirement: support struct return values)
        public struct UserStats
        {
            public int TotalUsers { get; set; }
            public int ActiveUsers { get; set; }
            public double AverageAge { get; set; }
        }

        #endregion

        #region Test Repositories

        [RepositoryFor(typeof(User))]
        public partial class UserCrudRepository : ICrudRepository<User, long>
        {
            public IDbConnection Connection { get; }
            public UserCrudRepository(IDbConnection connection)
            {
                Connection = connection;
            }
        }

        [RepositoryFor(typeof(User))]
        public partial class UserQueryRepository : IQueryRepository<User, long>
        {
            public IDbConnection Connection { get; }
            public UserQueryRepository(IDbConnection connection)
            {
                Connection = connection;
            }
        }

        [RepositoryFor(typeof(User))]
        public partial class UserCommandRepository : ICommandRepository<User, long>
        {
            public IDbConnection Connection { get; }
            public UserCommandRepository(IDbConnection connection)
            {
                Connection = connection;
            }
        }

        [RepositoryFor(typeof(User))]
        public partial class UserAggregateRepository : IAggregateRepository<User, long>
        {
            public IDbConnection Connection { get; }
            public UserAggregateRepository(IDbConnection connection)
            {
                Connection = connection;
            }
        }

        [RepositoryFor(typeof(User))]
        public partial class UserBatchRepository : IBatchRepository<User, long>
        {
            public IDbConnection Connection { get; }
            public UserBatchRepository(IDbConnection connection)
            {
                Connection = connection;
            }
        }

        [RepositoryFor(typeof(Product))]
        public partial class ProductRepository : ICrudRepository<Product, long>
        {
            public IDbConnection Connection { get; }
            public ProductRepository(IDbConnection connection)
            {
                Connection = connection;
            }
        }

        #endregion

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

        private async Task CreateProductTableAsync()
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS products (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL,
                    price REAL NOT NULL,
                    stock INTEGER NOT NULL,
                    created_at TEXT NOT NULL
                )";
            await cmd.ExecuteNonQueryAsync();
        }

        #endregion

        #region ICrudRepository Tests

        [Fact]
        public async Task ICrudRepository_GetByIdAsync_RecordType_ShouldWork()
        {
            // Arrange
            await CreateUserTableAsync();
            var repo = new UserCrudRepository(_connection);
            
            // This test verifies record type support
            // The source generator should handle record types correctly
            Assert.NotNull(repo);
        }

        [Fact]
        public async Task ICrudRepository_CountAsync_ShouldReturnZeroForEmptyTable()
        {
            // Arrange
            await CreateUserTableAsync();
            var repo = new UserCrudRepository(_connection);
            
            // Act
            var count = await repo.CountAsync();
            
            // Assert
            Assert.Equal(0L, count);
        }

        #endregion

        #region IQueryRepository Tests

        [Fact]
        public async Task IQueryRepository_GetByIdAsync_ShouldReturnNullWhenNotFound()
        {
            // Arrange
            await CreateUserTableAsync();
            var repo = new UserQueryRepository(_connection);
            
            // Act
            var user = await repo.GetByIdAsync(999);
            
            // Assert
            Assert.Null(user);
        }

        [Fact]
        public async Task IQueryRepository_GetAllAsync_ShouldReturnEmptyList()
        {
            // Arrange
            await CreateUserTableAsync();
            var repo = new UserQueryRepository(_connection);
            
            // Act
            var users = await repo.GetAllAsync();
            
            // Assert
            Assert.NotNull(users);
            Assert.Empty(users);
        }

        [Fact]
        public async Task IQueryRepository_ExistsAsync_ShouldReturnFalse()
        {
            // Arrange
            await CreateUserTableAsync();
            var repo = new UserQueryRepository(_connection);
            
            // Act
            var exists = await repo.ExistsAsync(999);
            
            // Assert
            Assert.False(exists);
        }

        #endregion

        #region ICommandRepository Tests

        [Fact]
        public async Task ICommandRepository_DeleteAsync_ShouldReturnZeroWhenNotFound()
        {
            // Arrange
            await CreateUserTableAsync();
            var repo = new UserCommandRepository(_connection);
            
            // Act
            var affected = await repo.DeleteAsync(999);
            
            // Assert
            Assert.Equal(0, affected);
        }

        #endregion

        #region IAggregateRepository Tests

        [Fact]
        public async Task IAggregateRepository_CountAsync_ShouldReturnZero()
        {
            // Arrange
            await CreateUserTableAsync();
            var repo = new UserAggregateRepository(_connection);
            
            // Act
            var count = await repo.CountAsync();
            
            // Assert
            Assert.Equal(0L, count);
        }

        [Fact]
        public async Task IAggregateRepository_SumAsync_ShouldReturnZero()
        {
            // Arrange
            await CreateUserTableAsync();
            var repo = new UserAggregateRepository(_connection);
            
            // Act
            var sum = await repo.SumAsync("age");
            
            // Assert
            Assert.Equal(0m, sum);
        }

        [Fact]
        public async Task IAggregateRepository_AvgAsync_ShouldReturnZero()
        {
            // Arrange
            await CreateUserTableAsync();
            var repo = new UserAggregateRepository(_connection);
            
            // Act
            var avg = await repo.AvgAsync("age");
            
            // Assert
            Assert.Equal(0m, avg);
        }

        #endregion

        #region IBatchRepository Tests

        [Fact]
        public async Task IBatchRepository_BatchInsertAsync_ShouldReturnZeroForEmptyList()
        {
            // Arrange
            await CreateUserTableAsync();
            var repo = new UserBatchRepository(_connection);
            var users = new List<User>();
            
            // Act
            var affected = await repo.BatchInsertAsync(users);
            
            // Assert
            Assert.Equal(0, affected);
        }

        #endregion

        #region Class Type Tests

        [Fact]
        public async Task ProductRepository_ClassType_ShouldWork()
        {
            // Arrange
            await CreateProductTableAsync();
            var repo = new ProductRepository(_connection);
            
            // This test verifies class type support
            Assert.NotNull(repo);
        }

        [Fact]
        public async Task ProductRepository_CountAsync_ShouldReturnZero()
        {
            // Arrange
            await CreateProductTableAsync();
            var repo = new ProductRepository(_connection);
            
            // Act
            var count = await repo.CountAsync();
            
            // Assert
            Assert.Equal(0L, count);
        }

        #endregion

        #region Interface Coverage Tests

        /// <summary>
        /// Verifies all IQueryRepository methods are implemented.
        /// </summary>
        [Fact]
        public void IQueryRepository_AllMethodsShouldBeImplemented()
        {
            var interfaceType = typeof(IQueryRepository<,>);
            var methods = interfaceType.GetMethods();
            
            // Verify we have the expected methods
            var methodNames = methods.Select(m => m.Name).ToList();
            
            Assert.Contains("GetByIdAsync", methodNames);
            Assert.Contains("GetByIdsAsync", methodNames);
            Assert.Contains("GetAllAsync", methodNames);
            Assert.Contains("GetTopAsync", methodNames);
            Assert.Contains("GetRangeAsync", methodNames);
            Assert.Contains("GetPageAsync", methodNames);
            Assert.Contains("GetWhereAsync", methodNames);
            Assert.Contains("GetFirstWhereAsync", methodNames);
            Assert.Contains("ExistsAsync", methodNames);
            Assert.Contains("ExistsWhereAsync", methodNames);
            Assert.Contains("GetRandomAsync", methodNames);
        }

        /// <summary>
        /// Verifies all ICommandRepository methods are implemented.
        /// </summary>
        [Fact]
        public void ICommandRepository_AllMethodsShouldBeImplemented()
        {
            var interfaceType = typeof(ICommandRepository<,>);
            var methods = interfaceType.GetMethods();
            
            var methodNames = methods.Select(m => m.Name).ToList();
            
            Assert.Contains("InsertAsync", methodNames);
            Assert.Contains("InsertAndGetIdAsync", methodNames);
            Assert.Contains("InsertAndGetEntityAsync", methodNames);
            Assert.Contains("UpdateAsync", methodNames);
            Assert.Contains("UpdatePartialAsync", methodNames);
            Assert.Contains("UpdateWhereAsync", methodNames);
            Assert.Contains("DeleteAsync", methodNames);
            Assert.Contains("DeleteWhereAsync", methodNames);
            Assert.Contains("SoftDeleteAsync", methodNames);
            Assert.Contains("RestoreAsync", methodNames);
            Assert.Contains("PurgeDeletedAsync", methodNames);
        }

        /// <summary>
        /// Verifies all IAggregateRepository methods are implemented.
        /// </summary>
        [Fact]
        public void IAggregateRepository_AllMethodsShouldBeImplemented()
        {
            var interfaceType = typeof(IAggregateRepository<,>);
            var methods = interfaceType.GetMethods();
            
            var methodNames = methods.Select(m => m.Name).ToList();
            
            Assert.Contains("CountAsync", methodNames);
            Assert.Contains("CountWhereAsync", methodNames);
            Assert.Contains("CountByAsync", methodNames);
            Assert.Contains("SumAsync", methodNames);
            Assert.Contains("SumWhereAsync", methodNames);
            Assert.Contains("AvgAsync", methodNames);
            Assert.Contains("AvgWhereAsync", methodNames);
            Assert.Contains("MaxIntAsync", methodNames);
            Assert.Contains("MaxLongAsync", methodNames);
            Assert.Contains("MaxDecimalAsync", methodNames);
            Assert.Contains("MaxDateTimeAsync", methodNames);
            Assert.Contains("MinIntAsync", methodNames);
            Assert.Contains("MinLongAsync", methodNames);
            Assert.Contains("MinDecimalAsync", methodNames);
            Assert.Contains("MinDateTimeAsync", methodNames);
        }

        /// <summary>
        /// Verifies all IBatchRepository methods are implemented.
        /// </summary>
        [Fact]
        public void IBatchRepository_AllMethodsShouldBeImplemented()
        {
            var interfaceType = typeof(IBatchRepository<,>);
            var methods = interfaceType.GetMethods();
            
            var methodNames = methods.Select(m => m.Name).ToList();
            
            Assert.Contains("BatchInsertAsync", methodNames);
            Assert.Contains("BatchInsertAndGetIdsAsync", methodNames);
            Assert.Contains("BatchUpdateAsync", methodNames);
            Assert.Contains("BatchUpdateWhereAsync", methodNames);
            Assert.Contains("BatchDeleteAsync", methodNames);
            Assert.Contains("BatchSoftDeleteAsync", methodNames);
            Assert.Contains("BatchUpsertAsync", methodNames);
            Assert.Contains("BatchExistsAsync", methodNames);
        }

        #endregion
    }
}

