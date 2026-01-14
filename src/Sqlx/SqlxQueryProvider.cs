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
    /// IQueryProvider implementation for SQL generation.
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
            var elementType = GetElementType(expression.Type);
            var queryableType = typeof(SqlxQueryable<>).MakeGenericType(elementType);
            var queryable = Activator.CreateInstance(queryableType, this, expression)!;

            // Propagate connection to new queryable
            if (_connection != null)
            {
                var connectionProp = queryableType.GetProperty("Connection");
                connectionProp?.SetValue(queryable, _connection);
            }

            return (IQueryable)queryable;
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

        /// <summary>Executes the query and returns a single result.</summary>
        public object? Execute(Expression expression)
        {
            if (_connection == null)
                throw new InvalidOperationException("No database connection. Set Connection property before executing.");

            return ExecuteScalar(expression);
        }

        /// <summary>Executes the typed query and returns a single result.</summary>
        public TResult Execute<TResult>(Expression expression)
        {
            if (_connection == null)
                throw new InvalidOperationException("No database connection. Set Connection property before executing.");

            var result = ExecuteScalar(expression);
            if (result == null || result == DBNull.Value)
                return default!;

            return (TResult)Convert.ChangeType(result, typeof(TResult));
        }

        private object? ExecuteScalar(Expression expression)
        {
            var (sql, parameters) = ToSqlWithParameters(expression);

            using var command = _connection!.CreateCommand();
            command.CommandText = sql;

            foreach (var param in parameters)
            {
                var dbParam = command.CreateParameter();
                dbParam.ParameterName = param.Key;
                dbParam.Value = param.Value ?? DBNull.Value;
                command.Parameters.Add(dbParam);
            }

            var wasOpen = _connection.State == ConnectionState.Open;
            if (!wasOpen) _connection.Open();

            try
            {
                return command.ExecuteScalar();
            }
            finally
            {
                if (!wasOpen) _connection.Close();
            }
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

        private static Type GetElementType(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IQueryable<>))
                return type.GetGenericArguments()[0];

            foreach (var iface in type.GetInterfaces())
            {
                if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IQueryable<>))
                    return iface.GetGenericArguments()[0];
            }

            return type;
        }
    }
}
