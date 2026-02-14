// -----------------------------------------------------------------------
// <copyright file="SqlxContext.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#nullable enable

using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Sqlx
{
    /// <summary>
    /// Base class for SqlxContext that manages multiple repositories with unified transaction handling.
    /// </summary>
    /// <remarks>
    /// <para>SqlxContext provides EF Core-style API for managing repositories while maintaining Sqlx's core principles:</para>
    /// <list type="bullet">
    /// <item><description>Zero reflection at runtime</description></item>
    /// <item><description>Full Native AOT compatibility</description></item>
    /// <item><description>Minimal memory overhead</description></item>
    /// <item><description>Compile-time code generation</description></item>
    /// </list>
    /// <para>Use [SqlxContext] attribute on derived classes to enable source generation.</para>
    /// <para>Use [IncludeRepository(typeof(RepositoryType))] attributes to specify which repositories to include.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Define context with repository specifications
    /// [SqlxContext]
    /// [SqlDefine(SqlDefineTypes.SQLite)]
    /// [IncludeRepository(typeof(UserRepository))]
    /// [IncludeRepository(typeof(OrderRepository))]
    /// public partial class AppDbContext : SqlxContext
    /// {
    ///     public AppDbContext(DbConnection connection) : base(connection) { }
    /// }
    /// 
    /// // Use the context
    /// await using var connection = new SqliteConnection("Data Source=app.db");
    /// await connection.OpenAsync();
    /// await using var context = new AppDbContext(connection);
    /// 
    /// // Access repositories directly
    /// var user = await context.Users.GetByIdAsync(1);
    /// var orders = await context.Orders.GetWhereAsync(o => o.UserId == user.Id);
    /// 
    /// // Transaction management
    /// await using var transaction = await context.BeginTransactionAsync();
    /// try
    /// {
    ///     await context.Users.InsertAsync(newUser);
    ///     await context.Orders.InsertAsync(newOrder);
    ///     await transaction.CommitAsync();
    /// }
    /// catch
    /// {
    ///     // Automatic rollback on dispose
    ///     throw;
    /// }
    /// </code>
    /// </example>
    public abstract class SqlxContext : IDisposable, IAsyncDisposable
    {
        private readonly DbConnection _connection;
        private readonly bool _ownsConnection;
        private DbTransaction? _transaction;
        private bool _ownsTransaction;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlxContext"/> class.
        /// </summary>
        /// <param name="connection">The database connection to use.</param>
        /// <param name="ownsConnection">If true, the context will dispose the connection when disposed.</param>
        /// <exception cref="ArgumentNullException">Thrown when connection is null.</exception>
        protected SqlxContext(DbConnection connection, bool ownsConnection = true)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _ownsConnection = ownsConnection;
        }

        /// <summary>
        /// Gets the database connection.
        /// </summary>
        public DbConnection Connection => _connection;

        /// <summary>
        /// Gets the current transaction, or null if no transaction is active.
        /// </summary>
        public DbTransaction? Transaction => _transaction;

        /// <summary>
        /// Gets a value indicating whether a transaction is currently active.
        /// </summary>
        public bool HasActiveTransaction => _transaction != null;

        /// <summary>
        /// Begins a new transaction asynchronously.
        /// </summary>
        /// <param name="isolationLevel">The isolation level for the transaction.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>The created transaction.</returns>
        /// <exception cref="InvalidOperationException">Thrown when a transaction is already active.</exception>
        public async Task<DbTransaction> BeginTransactionAsync(
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
            CancellationToken cancellationToken = default)
        {
            if (_transaction != null)
            {
                throw new InvalidOperationException("A transaction is already active.");
            }

            _transaction = await _connection.BeginTransactionAsync(isolationLevel, cancellationToken);
            _ownsTransaction = true;
            PropagateTransactionToRepositories();
            return _transaction;
        }

        /// <summary>
        /// Begins a new transaction synchronously.
        /// </summary>
        /// <param name="isolationLevel">The isolation level for the transaction.</param>
        /// <returns>The created transaction.</returns>
        /// <exception cref="InvalidOperationException">Thrown when a transaction is already active.</exception>
        public DbTransaction BeginTransaction(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            if (_transaction != null)
            {
                throw new InvalidOperationException("A transaction is already active.");
            }

            _transaction = _connection.BeginTransaction(isolationLevel);
            _ownsTransaction = true;
            PropagateTransactionToRepositories();
            return _transaction;
        }

        /// <summary>
        /// Sets an external transaction for the context to use.
        /// </summary>
        /// <param name="transaction">The external transaction to use, or null to clear the transaction.</param>
        /// <exception cref="InvalidOperationException">Thrown when an owned transaction is already active.</exception>
        public void UseTransaction(DbTransaction? transaction)
        {
            if (_transaction != null && _ownsTransaction)
            {
                throw new InvalidOperationException(
                    "Cannot set external transaction when an owned transaction is active. " +
                    "Commit or rollback the current transaction first.");
            }

            _transaction = transaction;
            _ownsTransaction = false;
            PropagateTransactionToRepositories();
        }

        /// <summary>
        /// Propagates the current transaction to all repositories.
        /// Override in derived classes to set the Transaction property on all repository instances.
        /// </summary>
        protected virtual void PropagateTransactionToRepositories()
        {
            // Override in generated code to set transaction on all repository properties
        }

        /// <summary>
        /// Clears the transaction from all repositories.
        /// Override in derived classes to clear the Transaction property on all repository instances.
        /// </summary>
        protected virtual void ClearRepositoryTransactions()
        {
            // Override in generated code to clear transaction on all repository properties
        }

        /// <summary>
        /// Disposes the context and its resources.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            if (_transaction != null && _ownsTransaction)
            {
                try
                {
                    _transaction.Rollback();
                }
                catch (InvalidOperationException)
                {
                    // Transaction may already be committed or rolled back
                }
                
                _transaction.Dispose();
            }

            _transaction = null;
            ClearRepositoryTransactions();

            if (_ownsConnection)
            {
                _connection.Dispose();
            }

            _disposed = true;
        }

        /// <summary>
        /// Disposes the context and its resources asynchronously.
        /// </summary>
        /// <returns>A ValueTask representing the asynchronous operation.</returns>
        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            if (_transaction != null && _ownsTransaction)
            {
                try
                {
                    await _transaction.RollbackAsync();
                }
                catch (InvalidOperationException)
                {
                    // Transaction may already be committed or rolled back
                }
                
                await _transaction.DisposeAsync();
            }

            _transaction = null;
            ClearRepositoryTransactions();

            if (_ownsConnection)
            {
                await _connection.DisposeAsync();
            }

            _disposed = true;
        }
    }
}
