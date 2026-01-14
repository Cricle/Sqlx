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

        public SqlxQueryProvider(SqlDialect dialect)
        {
            _dialect = dialect ?? throw new ArgumentNullException(nameof(dialect));
        }

        public SqlDialect Dialect => _dialect;
        public DbConnection? Connection { get => _connection; set => _connection = value; }
        public Func<IDataReader, object>? Mapper { get => _mapper; set => _mapper = value; }

        public IQueryable CreateQuery(Expression expression)
            => throw new NotSupportedException("Use CreateQuery<T> for AOT compatibility.");

        public IQueryable<TElement> CreateQuery<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
            TElement>(Expression expression) => new SqlxQueryable<TElement>(this, expression);

        public object? Execute(Expression expression)
        {
            if (_connection == null) throw new InvalidOperationException("No database connection. Use WithConnection().");
            if (_mapper == null) throw new InvalidOperationException("No mapper function. Use WithMapper().");
            var visitor = new SqlExpressionVisitor(_dialect, parameterized: true);
            var sql = visitor.GenerateSql(expression);
            return DbExecutor.ExecuteReader(_connection, sql, visitor.GetParameters(), _mapper);
        }

        public TResult Execute<TResult>(Expression expression) => (TResult)Execute(expression)!;

        public IAsyncEnumerator<T> ExecuteAsync<T>(Expression expression, CancellationToken cancellationToken)
        {
            if (_connection == null) throw new InvalidOperationException("No database connection. Use WithConnection().");
            if (_mapper == null) throw new InvalidOperationException("No mapper function. Use WithMapper().");
            var visitor = new SqlExpressionVisitor(_dialect, parameterized: true);
            var sql = visitor.GenerateSql(expression);
            return DbExecutor.ExecuteReaderAsync(_connection, sql, visitor.GetParameters(), r => (T)_mapper(r), cancellationToken);
        }

        public string ToSql(Expression expression, bool parameterized = false)
            => new SqlExpressionVisitor(_dialect, parameterized).GenerateSql(expression);

        public (string Sql, IEnumerable<KeyValuePair<string, object?>> Parameters) ToSqlWithParameters(Expression expression)
        {
            var visitor = new SqlExpressionVisitor(_dialect, parameterized: true);
            var sql = visitor.GenerateSql(expression);
            return (sql, visitor.GetParameters());
        }
    }
}
