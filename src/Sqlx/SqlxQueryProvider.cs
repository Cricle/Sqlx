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

        /// <summary>Creates a new SqlxQueryProvider with the specified dialect.</summary>
        public SqlxQueryProvider(SqlDialect dialect)
        {
            _dialect = dialect ?? throw new ArgumentNullException(nameof(dialect));
        }

        /// <summary>Creates a new SqlxQueryProvider with the specified dialect and connection.</summary>
        public SqlxQueryProvider(SqlDialect dialect, DbConnection connection)
        {
            _dialect = dialect ?? throw new ArgumentNullException(nameof(dialect));
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        /// <summary>Gets the SQL dialect.</summary>
        public SqlDialect Dialect => _dialect;

        /// <summary>Gets or sets the database connection.</summary>
        public DbConnection? Connection
        {
            get => _connection;
            set => _connection = value;
        }

        /// <summary>Creates a new query with the specified expression.</summary>
        public IQueryable CreateQuery(Expression expression)
        {
            throw new NotSupportedException("Use CreateQuery<T> instead for AOT compatibility.");
        }

        /// <summary>Creates a new typed query with the specified expression.</summary>
        public IQueryable<TElement> CreateQuery<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
        TElement>(Expression expression)
        {
            var queryable = new SqlxQueryable<TElement>(this, expression);
            if (_connection != null)
            {
                queryable.Connection = _connection;
            }
            return queryable;
        }

        /// <summary>Executes the query - requires mapper to be set.</summary>
        public object? Execute(Expression expression)
        {
            throw new NotSupportedException("Use Execute<T> with a mapper for AOT compatibility.");
        }

        /// <summary>Executes the typed query - requires mapper to be set.</summary>
        public TResult Execute<TResult>(Expression expression)
        {
            throw new NotSupportedException("Use ToList/First/Single with a mapper for AOT compatibility.");
        }

        /// <summary>Generates SQL from the expression tree.</summary>
        public string ToSql(Expression expression)
        {
            var visitor = new SqlExpressionVisitor(_dialect);
            return visitor.GenerateSql(expression);
        }

        /// <summary>Generates SQL with parameters from the expression tree.</summary>
        public (string Sql, Dictionary<string, object?> Parameters) ToSqlWithParameters(Expression expression)
        {
            var visitor = new SqlExpressionVisitor(_dialect, parameterized: true);
            var sql = visitor.GenerateSql(expression);
            return (sql, visitor.GetParameters());
        }
    }
}
