// -----------------------------------------------------------------------
// <copyright file="SqlxQueryable.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
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
        T> : IQueryable<T>, IOrderedQueryable<T>, IAsyncEnumerable<T>
    {
        private readonly SqlxQueryProvider _provider;
        private readonly Expression _expression;
        private DbConnection? _connection;
        private Func<IDataReader, T>? _mapper;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlxQueryable{T}"/> class.
        /// </summary>
        /// <param name="provider">The query provider.</param>
        internal SqlxQueryable(SqlxQueryProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _expression = Expression.Constant(this);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlxQueryable{T}"/> class.
        /// </summary>
        /// <param name="provider">The query provider.</param>
        /// <param name="expression">The expression tree.</param>
        internal SqlxQueryable(SqlxQueryProvider provider, Expression expression)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _expression = expression ?? throw new ArgumentNullException(nameof(expression));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlxQueryable{T}"/> class.
        /// </summary>
        /// <param name="provider">The query provider.</param>
        /// <param name="expression">The expression tree.</param>
        /// <param name="connection">The database connection.</param>
        /// <param name="mapper">The result mapper function.</param>
        internal SqlxQueryable(
            SqlxQueryProvider provider,
            Expression expression,
            DbConnection? connection,
            Func<IDataReader, T>? mapper)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _expression = expression ?? throw new ArgumentNullException(nameof(expression));
            _connection = connection;
            _mapper = mapper;
        }

        /// <inheritdoc/>
        public Type ElementType => typeof(T);

        /// <inheritdoc/>
        public Expression Expression => _expression;

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
            set => _connection = value;
        }

        /// <summary>
        /// Gets or sets the mapper function.
        /// </summary>
        internal Func<IDataReader, T>? Mapper
        {
            get => _mapper;
            set => _mapper = value;
        }

        /// <inheritdoc/>
        public IEnumerator<T> GetEnumerator()
        {
            if (_connection == null)
            {
                throw new InvalidOperationException("No database connection. Use WithConnection().");
            }

            if (_mapper == null)
            {
                throw new InvalidOperationException("No mapper function. Use WithMapper().");
            }

            var (sql, parameters) = _provider.ToSqlWithParameters(_expression);
            return DbExecutor.ExecuteReader(_connection, sql, parameters, _mapper).GetEnumerator();
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <inheritdoc/>
        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            if (_connection == null)
            {
                throw new InvalidOperationException("No database connection. Use WithConnection().");
            }

            if (_mapper == null)
            {
                throw new InvalidOperationException("No mapper function. Use WithMapper().");
            }

            var (sql, parameters) = _provider.ToSqlWithParameters(_expression);
            return DbExecutor.ExecuteReaderAsync(_connection, sql, parameters, _mapper, cancellationToken);
        }
    }
}
