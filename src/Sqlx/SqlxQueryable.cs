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

        internal SqlxQueryable(SqlxQueryProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _expression = Expression.Constant(this);
        }

        internal SqlxQueryable(SqlxQueryProvider provider, Expression expression)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _expression = expression ?? throw new ArgumentNullException(nameof(expression));
        }

        internal SqlxQueryable(SqlxQueryProvider provider, Expression expression, DbConnection? connection, Func<IDataReader, T>? mapper)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _expression = expression ?? throw new ArgumentNullException(nameof(expression));
            _connection = connection;
            _mapper = mapper;
        }

        public Type ElementType => typeof(T);
        public Expression Expression => _expression;
        public IQueryProvider Provider => _provider;
        public SqlDialect Dialect => _provider.Dialect;
        internal DbConnection? Connection { get => _connection; set => _connection = value; }
        internal Func<IDataReader, T>? Mapper { get => _mapper; set => _mapper = value; }

        public IEnumerator<T> GetEnumerator()
        {
            if (_connection == null) throw new InvalidOperationException("No database connection. Use WithConnection().");
            if (_mapper == null) throw new InvalidOperationException("No mapper function. Use WithMapper().");
            var (sql, parameters) = _provider.ToSqlWithParameters(_expression);
            return DbExecutor.ExecuteReader(_connection, sql, parameters, _mapper).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            if (_connection == null) throw new InvalidOperationException("No database connection. Use WithConnection().");
            if (_mapper == null) throw new InvalidOperationException("No mapper function. Use WithMapper().");
            var (sql, parameters) = _provider.ToSqlWithParameters(_expression);
            return DbExecutor.ExecuteReaderAsync(_connection, sql, parameters, _mapper, cancellationToken);
        }
    }
}
