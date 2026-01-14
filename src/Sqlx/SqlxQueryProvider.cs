// -----------------------------------------------------------------------
// <copyright file="SqlxQueryProvider.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
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
    /// IQueryProvider implementation for SQL generation (AOT-friendly, no reflection).
    /// </summary>
    public class SqlxQueryProvider : IQueryProvider
    {
        private readonly SqlDialect _dialect;
        private DbConnection? _connection;
        private Func<IDataReader, object>? _mapper;

        /// <summary>Creates a new SqlxQueryProvider with the specified dialect.</summary>
        public SqlxQueryProvider(SqlDialect dialect)
        {
            _dialect = dialect ?? throw new ArgumentNullException(nameof(dialect));
        }

        /// <summary>Gets the SQL dialect.</summary>
        public SqlDialect Dialect => _dialect;

        /// <summary>Gets or sets the database connection.</summary>
        public DbConnection? Connection
        {
            get => _connection;
            set => _connection = value;
        }

        /// <summary>Gets or sets the mapper function.</summary>
        public Func<IDataReader, object>? Mapper
        {
            get => _mapper;
            set => _mapper = value;
        }

        /// <summary>Creates a new query with the specified expression.</summary>
        public IQueryable CreateQuery(Expression expression)
        {
            throw new NotSupportedException("Use CreateQuery<T> for AOT compatibility.");
        }

        /// <summary>Creates a new typed query with the specified expression.</summary>
        public IQueryable<TElement> CreateQuery<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
        TElement>(Expression expression)
        {
            return new SqlxQueryable<TElement>(this, expression);
        }

        /// <summary>Executes the query.</summary>
        public object? Execute(Expression expression)
        {
            if (_connection == null)
                throw new InvalidOperationException("No database connection. Use WithConnection() before executing.");
            if (_mapper == null)
                throw new InvalidOperationException("No mapper function. Use WithMapper() before executing.");

            var sql = ToSql(expression);
            var parameters = GetParameters(expression);
            return DbExecutor.ExecuteReader(_connection, sql, parameters, _mapper);
        }

        /// <summary>Executes the typed query.</summary>
        public TResult Execute<TResult>(Expression expression)
        {
            return (TResult)Execute(expression)!;
        }

#if NET8_0_OR_GREATER
        /// <summary>Executes the query asynchronously.</summary>
        public IAsyncEnumerator<T> ExecuteAsync<T>(Expression expression, CancellationToken cancellationToken)
        {
            if (_connection == null)
                throw new InvalidOperationException("No database connection. Use WithConnection() before executing.");
            if (_mapper == null)
                throw new InvalidOperationException("No mapper function. Use WithMapper() before executing.");

            var sql = ToSql(expression);
            var parameters = GetParameters(expression);
            return DbExecutor.ExecuteReaderAsync(_connection, sql, parameters, r => (T)_mapper(r), cancellationToken);
        }
#endif

        /// <summary>Generates SQL from the expression tree.</summary>
        public string ToSql(Expression expression)
        {
            var visitor = new SqlExpressionVisitor(_dialect);
            return visitor.GenerateSql(expression);
        }

        /// <summary>Gets parameters from the expression tree.</summary>
        public IEnumerable<KeyValuePair<string, object?>> GetParameters(Expression expression)
        {
            var visitor = new SqlExpressionVisitor(_dialect, parameterized: true);
            visitor.GenerateSql(expression);
            return visitor.GetParameters();
        }
    }
}
