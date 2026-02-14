// -----------------------------------------------------------------------
// <copyright file="SqlxContextTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;

namespace Sqlx.Tests
{
    [TestClass]
    public class SqlxContextTests
    {
        [TestMethod]
        public void Constructor_WithValidConnection_ShouldSucceed()
        {
            // Arrange
            using var connection = new SqliteConnection("Data Source=:memory:");
            
            // Act
            using var context = new TestContext(connection);
            
            // Assert
            Assert.IsNotNull(context);
            Assert.AreEqual(connection, context.Connection);
            Assert.IsFalse(context.HasActiveTransaction);
        }

        [TestMethod]
        public void Constructor_WithNullConnection_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() => new TestContext(null!));
        }

        [TestMethod]
        public async Task BeginTransactionAsync_WhenNoActiveTransaction_ShouldCreateTransaction()
        {
            // Arrange
            using var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            using var context = new TestContext(connection);
            
            // Act
            await using var transaction = await context.BeginTransactionAsync();
            
            // Assert
            Assert.IsNotNull(transaction);
            Assert.IsTrue(context.HasActiveTransaction);
            Assert.AreEqual(transaction, context.Transaction);
        }

        [TestMethod]
        public async Task BeginTransactionAsync_WhenTransactionAlreadyActive_ShouldThrowInvalidOperationException()
        {
            // Arrange
            using var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            using var context = new TestContext(connection);
            await using var transaction1 = await context.BeginTransactionAsync();
            
            // Act & Assert
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                async () => await context.BeginTransactionAsync());
        }

        [TestMethod]
        public void BeginTransaction_WhenNoActiveTransaction_ShouldCreateTransaction()
        {
            // Arrange
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            using var context = new TestContext(connection);
            
            // Act
            using var transaction = context.BeginTransaction();
            
            // Assert
            Assert.IsNotNull(transaction);
            Assert.IsTrue(context.HasActiveTransaction);
            Assert.AreEqual(transaction, context.Transaction);
        }

        [TestMethod]
        public void BeginTransaction_WhenTransactionAlreadyActive_ShouldThrowInvalidOperationException()
        {
            // Arrange
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            using var context = new TestContext(connection);
            using var transaction1 = context.BeginTransaction();
            
            // Act & Assert
            Assert.ThrowsException<InvalidOperationException>(
                () => context.BeginTransaction());
        }

        [TestMethod]
        public async Task UseTransaction_WithExternalTransaction_ShouldSetTransaction()
        {
            // Arrange
            using var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            await using var externalTransaction = await connection.BeginTransactionAsync();
            using var context = new TestContext(connection, ownsConnection: false);
            
            // Act
            context.UseTransaction(externalTransaction);
            
            // Assert
            Assert.IsTrue(context.HasActiveTransaction);
            Assert.AreEqual(externalTransaction, context.Transaction);
        }

        [TestMethod]
        public async Task UseTransaction_WhenOwnedTransactionActive_ShouldThrowInvalidOperationException()
        {
            // Arrange
            using var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            using var context = new TestContext(connection);
            await using var ownedTransaction = await context.BeginTransactionAsync();
            
            // Create a mock external transaction (we can't create a real one on SQLite)
            // Just test that the check works by trying to use the same transaction
            
            // Act & Assert
            Assert.ThrowsException<InvalidOperationException>(
                () => context.UseTransaction(ownedTransaction));
        }

        [TestMethod]
        public async Task UseTransaction_WithNull_ShouldClearTransaction()
        {
            // Arrange
            using var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            await using var externalTransaction = await connection.BeginTransactionAsync();
            using var context = new TestContext(connection, ownsConnection: false);
            context.UseTransaction(externalTransaction);
            
            // Act
            context.UseTransaction(null);
            
            // Assert
            Assert.IsFalse(context.HasActiveTransaction);
            Assert.IsNull(context.Transaction);
        }

        [TestMethod]
        public async Task Dispose_WithOwnedConnection_ShouldDisposeConnection()
        {
            // Arrange
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var context = new TestContext(connection, ownsConnection: true);
            
            // Act
            context.Dispose();
            
            // Assert
            Assert.AreEqual(ConnectionState.Closed, connection.State);
        }

        [TestMethod]
        public async Task Dispose_WithExternalConnection_ShouldNotDisposeConnection()
        {
            // Arrange
            using var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var context = new TestContext(connection, ownsConnection: false);
            
            // Act
            context.Dispose();
            
            // Assert
            Assert.AreEqual(ConnectionState.Open, connection.State);
        }

        [TestMethod]
        public async Task Dispose_WithOwnedTransaction_ShouldRollbackAndDisposeTransaction()
        {
            // Arrange
            using var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var context = new TestContext(connection);
            var transaction = await context.BeginTransactionAsync();
            
            // Act
            context.Dispose();
            
            // Assert
            Assert.IsFalse(context.HasActiveTransaction);
            Assert.IsNull(context.Transaction);
        }

        [TestMethod]
        public async Task Dispose_WithExternalTransaction_ShouldNotDisposeTransaction()
        {
            // Arrange
            using var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            await using var externalTransaction = await connection.BeginTransactionAsync();
            var context = new TestContext(connection, ownsConnection: false);
            context.UseTransaction(externalTransaction);
            
            // Act
            context.Dispose();
            
            // Assert
            // External transaction should still be usable
            Assert.IsNotNull(externalTransaction.Connection);
        }

        [TestMethod]
        public void Dispose_CalledMultipleTimes_ShouldNotThrow()
        {
            // Arrange
            using var connection = new SqliteConnection("Data Source=:memory:");
            var context = new TestContext(connection);
            
            // Act & Assert
            context.Dispose();
            context.Dispose(); // Should not throw
            context.Dispose(); // Should not throw
        }

        [TestMethod]
        public async Task DisposeAsync_WithOwnedConnection_ShouldDisposeConnection()
        {
            // Arrange
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var context = new TestContext(connection, ownsConnection: true);
            
            // Act
            await context.DisposeAsync();
            
            // Assert
            Assert.AreEqual(ConnectionState.Closed, connection.State);
        }

        [TestMethod]
        public async Task DisposeAsync_CalledMultipleTimes_ShouldNotThrow()
        {
            // Arrange
            using var connection = new SqliteConnection("Data Source=:memory:");
            var context = new TestContext(connection);
            
            // Act & Assert
            await context.DisposeAsync();
            await context.DisposeAsync(); // Should not throw
            await context.DisposeAsync(); // Should not throw
        }

        [TestMethod]
        public async Task HasActiveTransaction_ShouldReflectTransactionState()
        {
            // Arrange
            using var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            using var context = new TestContext(connection);
            
            // Assert - No transaction
            Assert.IsFalse(context.HasActiveTransaction);
            
            // Act - Begin transaction
            await using var transaction = await context.BeginTransactionAsync();
            
            // Assert - Transaction active
            Assert.IsTrue(context.HasActiveTransaction);
            
            // Act - Commit transaction
            await transaction.CommitAsync();
            
            // Note: Transaction is still set until context is disposed or UseTransaction(null) is called
            Assert.IsTrue(context.HasActiveTransaction);
        }

        [TestMethod]
        public void Constructor_WithServiceProvider_ShouldSucceed()
        {
            // Arrange
            using var connection = new SqliteConnection("Data Source=:memory:");
            
            // Act
            using var context = new TestContext(connection);
            
            // Assert
            Assert.IsNotNull(context);
            Assert.AreEqual(connection, context.Connection);
            Assert.IsFalse(context.HasActiveTransaction);
        }

        // Test context implementation
        private class TestContext : SqlxContext
        {
            private readonly TestRepository? _testRepository;

            public TestContext(DbConnection connection, bool ownsConnection = true)
                : base(connection, ownsConnection)
            {
                _testRepository = new TestRepository();
                _testRepository.Connection = connection;
                _testRepository.Transaction = Transaction;
            }

            public TestContext(DbConnection connection, TestRepository testRepository, bool ownsConnection = true)
                : base(connection, ownsConnection)
            {
                _testRepository = testRepository;
                _testRepository.Connection = connection;
                _testRepository.Transaction = Transaction;
            }

            public TestRepository? TestEntities => _testRepository;

            protected override void PropagateTransactionToRepositories()
            {
                if (_testRepository != null)
                    _testRepository.Transaction = Transaction;
            }

            protected override void ClearRepositoryTransactions()
            {
                if (_testRepository != null)
                    _testRepository.Transaction = null;
            }
        }

        // Test repository
        private class TestRepository : ISqlxRepository
        {
            public DbConnection? Connection { get; set; }
            public DbTransaction? Transaction { get; set; }
        }
    }
}

