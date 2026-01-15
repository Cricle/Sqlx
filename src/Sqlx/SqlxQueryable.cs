// -----------------------------------------------------------------------
// <copyright file="SqlxQueryable.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
#if NET5_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace Sqlx
{
    /// <summary>
    /// IQueryable implementation for SQL generation (AOT-friendly, no reflection).
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    public class SqlxQueryable<
#if NET5_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
        T> :  IOrderedQueryable<T>, IAsyncEnumerable<T>
    {
        private readonly SqlxQueryProvider _provider;
        private DbConnection? _connection;
        private IResultReader<T>? _resultReader;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlxQueryable{T}"/> class.
        /// </summary>
        /// <param name="provider">The query provider.</param>
        internal SqlxQueryable(SqlxQueryProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            Expression = Expression.Constant(this);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlxQueryable{T}"/> class.
        /// </summary>
        /// <param name="provider">The query provider.</param>
        /// <param name="expression">The expression tree.</param>
        internal SqlxQueryable(SqlxQueryProvider provider, Expression expression)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            Expression = expression ?? throw new ArgumentNullException(nameof(expression));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlxQueryable{T}"/> class.
        /// </summary>
        /// <param name="provider">The query provider.</param>
        /// <param name="expression">The expression tree.</param>
        /// <param name="connection">The database connection.</param>
        /// <param name="resultReader">The result reader.</param>
        internal SqlxQueryable(
            SqlxQueryProvider provider,
            Expression expression,
            DbConnection? connection,
            IResultReader<T>? resultReader)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            Expression = expression ?? throw new ArgumentNullException(nameof(expression));
            _connection = connection;
            ResultReader = resultReader;
        }

        /// <inheritdoc/>
        public Type ElementType => typeof(T);

        /// <inheritdoc/>
        public Expression Expression { get; }

        /// <inheritdoc/>
        public IQueryProvider Provider => _provider;

        /// <summary>
        /// Gets the SQL dialect.
        /// </summary>
        public SqlDialect Dialect => _provider.Dialect;

        /// <summary>
        /// Gets or sets the database connection.
        /// </summary>
        internal DbConnection? Connection
        {
            get => _connection;
            set
            {
                _connection = value;
                _provider.Connection = value;
            }
        }

        /// <summary>
        /// Gets or sets the result reader.
        /// </summary>
        internal IResultReader<T>? ResultReader
        {
            get => _resultReader;
            set
            {
                _resultReader = value;
                _provider.ResultReader = value;
            }
        }

        /// <inheritdoc/>
        public IEnumerator<T> GetEnumerator()
        {
            if (Connection == null)
            {
                throw new InvalidOperationException("No database connection. Use WithConnection().");
            }

            if (ResultReader == null)
            {
                throw new InvalidOperationException("No result reader. Use WithReader().");
            }

            var (sql, parameters) = _provider.ToSqlWithParameters(Expression);
            return DbExecutor.ExecuteReader(Connection, sql, parameters, ResultReader).GetEnumerator();
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <inheritdoc/>
        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            if (Connection == null)
            {
                throw new InvalidOperationException("No database connection. Use WithConnection().");
            }

            if (ResultReader == null)
            {
                throw new InvalidOperationException("No result reader. Use WithReader().");
            }

            var (sql, parameters) = _provider.ToSqlWithParameters(Expression);
            return DbExecutor.ExecuteReaderAsync(Connection, sql, parameters, ResultReader, cancellationToken);
        }
    }
}
