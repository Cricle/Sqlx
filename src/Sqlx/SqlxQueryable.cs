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
        private IResultReader<T>? _resultReader;

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
        /// <param name="resultReader">The result reader.</param>
        internal SqlxQueryable(
            SqlxQueryProvider provider,
            Expression expression,
            DbConnection? connection,
            IResultReader<T>? resultReader)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _expression = expression ?? throw new ArgumentNullException(nameof(expression));
            _connection = connection;
            _resultReader = resultReader;
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
            set => _resultReader = value;
        }

        /// <inheritdoc/>
        public IEnumerator<T> GetEnumerator()
        {
            if (_connection == null)
            {
                throw new InvalidOperationException("No database connection. Use WithConnection().");
            }

            if (_resultReader == null)
            {
                throw new InvalidOperationException("No result reader. Use WithReader().");
            }

            var (sql, parameters) = _provider.ToSqlWithParameters(_expression);
            return ExecuteWithReader(sql, parameters).GetEnumerator();
        }

        private IEnumerable<T> ExecuteWithReader(string sql, IEnumerable<KeyValuePair<string, object?>> parameters)
        {
            using var command = _connection!.CreateCommand();
            command.CommandText = sql;

            foreach (var p in parameters)
            {
                var param = command.CreateParameter();
                param.ParameterName = p.Key;
                param.Value = p.Value ?? DBNull.Value;
                command.Parameters.Add(param);
            }

            var wasOpen = _connection.State == ConnectionState.Open;
            if (!wasOpen)
            {
                _connection.Open();
            }

            try
            {
                using var reader = command.ExecuteReader();
                if (!reader.Read())
                {
                    yield break;
                }

                var ordinals = _resultReader!.GetOrdinals(reader);
                do
                {
                    yield return _resultReader.Read(reader, ordinals);
                }
                while (reader.Read());
            }
            finally
            {
                if (!wasOpen)
                {
                    _connection.Close();
                }
            }
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <inheritdoc/>
        public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            if (_connection == null)
            {
                throw new InvalidOperationException("No database connection. Use WithConnection().");
            }

            if (_resultReader == null)
            {
                throw new InvalidOperationException("No result reader. Use WithReader().");
            }

            var (sql, parameters) = _provider.ToSqlWithParameters(_expression);

            var command = _connection.CreateCommand();
            command.CommandText = sql;

            foreach (var p in parameters)
            {
                var param = command.CreateParameter();
                param.ParameterName = p.Key;
                param.Value = p.Value ?? DBNull.Value;
                command.Parameters.Add(param);
            }

            var wasOpen = _connection.State == ConnectionState.Open;
            if (!wasOpen)
            {
                await _connection.OpenAsync(cancellationToken);
            }

            DbDataReader? reader = null;
            try
            {
                reader = await command.ExecuteReaderAsync(cancellationToken);
                if (!await reader.ReadAsync(cancellationToken))
                {
                    yield break;
                }

                var ordinals = _resultReader.GetOrdinals(reader);
                do
                {
                    yield return _resultReader.Read(reader, ordinals);
                }
                while (await reader.ReadAsync(cancellationToken));
            }
            finally
            {
                if (reader != null)
                {
                    await reader.DisposeAsync();
                }

                await command.DisposeAsync();

                if (!wasOpen)
                {
                    _connection.Close();
                }
            }
        }
    }
}
