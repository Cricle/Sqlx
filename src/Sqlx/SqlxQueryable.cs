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
    T> : IQueryable<T>, IOrderedQueryable<T>
    {
        private readonly SqlxQueryProvider _provider;
        private readonly Expression _expression;
        private DbConnection? _connection;
        private Func<IDataReader, T>? _mapper;

        /// <summary>Creates a new SqlxQueryable with the specified provider.</summary>
        internal SqlxQueryable(SqlxQueryProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _expression = Expression.Constant(this);
        }

        /// <summary>Creates a new SqlxQueryable with the specified provider and expression.</summary>
        internal SqlxQueryable(SqlxQueryProvider provider, Expression expression)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _expression = expression ?? throw new ArgumentNullException(nameof(expression));
        }

        /// <summary>Gets the element type.</summary>
        public Type ElementType => typeof(T);

        /// <summary>Gets the expression tree.</summary>
        public Expression Expression => _expression;

        /// <summary>Gets the query provider.</summary>
        public IQueryProvider Provider => _provider;

        /// <summary>Gets the SQL dialect.</summary>
        public SqlDialect Dialect => _provider.Dialect;

        /// <summary>Gets or sets the database connection for execution.</summary>
        public DbConnection? Connection
        {
            get => _connection;
            set => _connection = value;
        }

        /// <summary>Gets or sets the custom mapper function for reading results.</summary>
        public Func<IDataReader, T>? Mapper
        {
            get => _mapper;
            set => _mapper = value;
        }

        /// <summary>Sets the database connection and returns this queryable for chaining.</summary>
        public SqlxQueryable<T> WithConnection(DbConnection connection)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            return this;
        }

        /// <summary>Sets the mapper function and returns this queryable for chaining.</summary>
        public SqlxQueryable<T> WithMapper(Func<IDataReader, T> mapper)
        {
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            return this;
        }

        /// <summary>Gets the enumerator by executing the query against the database.</summary>
        public IEnumerator<T> GetEnumerator()
        {
            if (_connection == null)
                throw new InvalidOperationException("No database connection. Use WithConnection() before enumerating.");

            if (_mapper == null)
                throw new InvalidOperationException("No mapper function. Use WithMapper() before enumerating.");

            return ExecuteReader().GetEnumerator();
        }

        /// <summary>Gets the enumerator by executing the query against the database.</summary>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private IEnumerable<T> ExecuteReader()
        {
            var (sql, parameters) = _provider.ToSqlWithParameters(_expression);
            return DbExecutor.ExecuteReader(_connection!, sql, parameters, _mapper!);
        }
    }
}
