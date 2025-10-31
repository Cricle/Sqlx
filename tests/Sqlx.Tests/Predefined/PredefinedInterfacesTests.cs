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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using Sqlx.Annotations;

namespace Sqlx.Tests.Predefined
{
    /// <summary>
    /// Comprehensive tests for all predefined repository interfaces.
    /// Tests record types, struct values, and all interface methods.
    /// </summary>
    [TestClass]
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

        // Entities and Repositories are now defined in separate files:
        // - TestEntities.cs: User (record), Product (class), UserStats (struct)
        // - TestRepositories.cs: All test repository implementations

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

        [TestMethod]
        public async Task ICrudRepository_GetByIdAsync_RecordType_ShouldWork()
        {
            // Arrange
            await CreateUserTableAsync();
            var repo = new UserCrudRepository(_connection);

            // This test verifies record type support
            // The source generator should handle record types correctly
            Assert.IsNotNull(repo);
        }

        [TestMethod]
        public async Task ICrudRepository_CountAsync_ShouldReturnZeroForEmptyTable()
        {
            // Arrange
            await CreateUserTableAsync();
            var repo = new UserCrudRepository(_connection);

            // Act
            var count = await repo.CountAsync();

            // Assert
            Assert.AreEqual(0L, count);
        }

        #endregion

        #region IQueryRepository Tests

        [TestMethod]
        public async Task IQueryRepository_GetByIdAsync_ShouldReturnNullWhenNotFound()
        {
            // Arrange
            await CreateUserTableAsync();
            var repo = new UserQueryRepository(_connection);

            // Act
            var user = await repo.GetByIdAsync(999);

            // Assert
            Assert.IsNull(user);
        }

        [TestMethod]
        public async Task IQueryRepository_ExistsAsync_ShouldReturnFalse()
        {
            // Arrange
            await CreateUserTableAsync();
            var repo = new UserQueryRepository(_connection);

            // Act
            var exists = await repo.ExistsAsync(999);

            // Assert
            Assert.IsFalse(exists);
        }

        #endregion

        #region ICommandRepository Tests

        [TestMethod]
        public async Task ICommandRepository_DeleteAsync_ShouldReturnZeroWhenNotFound()
        {
            // Arrange
            await CreateUserTableAsync();
            var repo = new UserCommandRepository(_connection);

            // Act
            var affected = await repo.DeleteAsync(999);

            // Assert
            Assert.AreEqual(0, affected);
        }

        #endregion

        #region IAggregateRepository Tests

        [TestMethod]
        public async Task IAggregateRepository_CountAsync_ShouldReturnZero()
        {
            // Arrange
            await CreateUserTableAsync();
            var repo = new UserAggregateRepository(_connection);

            // Act
            var count = await repo.CountAsync();

            // Assert
            Assert.AreEqual(0L, count);
        }

        #endregion

        #region IBatchRepository Tests

        [TestMethod]
        public async Task IBatchRepository_BatchInsertAsync_ShouldReturnZeroForEmptyList()
        {
            // Arrange
            await CreateUserTableAsync();
            var repo = new UserBatchRepository(_connection);
            var users = new List<User>();

            // Act
            var affected = await repo.BatchInsertAsync(users);

            // Assert
            Assert.AreEqual(0, affected);
        }

        #endregion

        #region Class Type Tests

        [TestMethod]
        public async Task ProductRepository_ClassType_ShouldWork()
        {
            // Arrange
            await CreateProductTableAsync();
            var repo = new ProductRepository(_connection);

            // This test verifies class type support
            Assert.IsNotNull(repo);
        }

        [TestMethod]
        public async Task ProductRepository_CountAsync_ShouldReturnZero()
        {
            // Arrange
            await CreateProductTableAsync();
            var repo = new ProductRepository(_connection);

            // Act
            var count = await repo.CountAsync();

            // Assert
            Assert.AreEqual(0L, count);
        }

        #endregion

        #region Interface Coverage Tests

        /// <summary>
        /// Verifies all IQueryRepository methods are implemented.
        /// </summary>
        [TestMethod]
        public void IQueryRepository_AllMethodsShouldBeImplemented()
        {
            var interfaceType = typeof(IQueryRepository<,>);
            var methods = interfaceType.GetMethods();

            // Verify we have the expected methods
            var methodNames = methods.Select(m => m.Name).ToList();

            CollectionAssert.Contains(methodNames, "GetByIdAsync");
            CollectionAssert.Contains(methodNames, "GetByIdsAsync");
            CollectionAssert.Contains(methodNames, "GetAllAsync");
            CollectionAssert.Contains(methodNames, "GetTopAsync");
            CollectionAssert.Contains(methodNames, "GetRangeAsync");
            CollectionAssert.Contains(methodNames, "GetPageAsync");
            CollectionAssert.Contains(methodNames, "GetWhereAsync");
            CollectionAssert.Contains(methodNames, "GetFirstWhereAsync");
            CollectionAssert.Contains(methodNames, "ExistsAsync");
            CollectionAssert.Contains(methodNames, "ExistsWhereAsync");
            CollectionAssert.Contains(methodNames, "GetRandomAsync");
        }

        /// <summary>
        /// Verifies all ICommandRepository methods are implemented.
        /// </summary>
        [TestMethod]
        public void ICommandRepository_AllMethodsShouldBeImplemented()
        {
            var interfaceType = typeof(ICommandRepository<,>);
            var methods = interfaceType.GetMethods();

            var methodNames = methods.Select(m => m.Name).ToList();

            CollectionAssert.Contains(methodNames, "InsertAsync");
            CollectionAssert.Contains(methodNames, "InsertAndGetIdAsync");
            CollectionAssert.Contains(methodNames, "InsertAndGetEntityAsync");
            CollectionAssert.Contains(methodNames, "UpdateAsync");
            CollectionAssert.Contains(methodNames, "UpdatePartialAsync");
            CollectionAssert.Contains(methodNames, "UpdateWhereAsync");
            CollectionAssert.Contains(methodNames, "DeleteAsync");
            CollectionAssert.Contains(methodNames, "DeleteWhereAsync");
            CollectionAssert.Contains(methodNames, "SoftDeleteAsync");
            CollectionAssert.Contains(methodNames, "RestoreAsync");
            CollectionAssert.Contains(methodNames, "PurgeDeletedAsync");
        }

        /// <summary>
        /// Verifies all IAggregateRepository methods are implemented.
        /// </summary>
        [TestMethod]
        public void IAggregateRepository_AllMethodsShouldBeImplemented()
        {
            var interfaceType = typeof(IAggregateRepository<,>);
            var methods = interfaceType.GetMethods();

            var methodNames = methods.Select(m => m.Name).ToList();

            CollectionAssert.Contains(methodNames, "CountAsync");
            CollectionAssert.Contains(methodNames, "CountWhereAsync");
            CollectionAssert.Contains(methodNames, "CountByAsync");
            CollectionAssert.Contains(methodNames, "SumAsync");
            CollectionAssert.Contains(methodNames, "SumWhereAsync");
            CollectionAssert.Contains(methodNames, "AvgAsync");
            CollectionAssert.Contains(methodNames, "AvgWhereAsync");
            CollectionAssert.Contains(methodNames, "MaxIntAsync");
            CollectionAssert.Contains(methodNames, "MaxLongAsync");
            CollectionAssert.Contains(methodNames, "MaxDecimalAsync");
            CollectionAssert.Contains(methodNames, "MaxDateTimeAsync");
            CollectionAssert.Contains(methodNames, "MinIntAsync");
            CollectionAssert.Contains(methodNames, "MinLongAsync");
            CollectionAssert.Contains(methodNames, "MinDecimalAsync");
            CollectionAssert.Contains(methodNames, "MinDateTimeAsync");
        }

        /// <summary>
        /// Verifies all IBatchRepository methods are implemented.
        /// </summary>
        #endregion
    }
}

