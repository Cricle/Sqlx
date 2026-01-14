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
    /// IQueryable implementation for SQL generation (AOT-friendly).
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

        /// <summary>Sets the database connection and returns this queryable for chaining.</summary>
        public SqlxQueryable<T> WithConnection(DbConnection connection)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            return this;
        }

        /// <summary>Gets the enumerator by executing the query against the database.</summary>
        public IEnumerator<T> GetEnumerator()
        {
            if (_connection == null)
                throw new InvalidOperationException("No database connection. Use WithConnection() or set Connection property before enumerating.");

            return ExecuteQuery().GetEnumerator();
        }

        /// <summary>Gets the enumerator by executing the query against the database.</summary>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private List<T> ExecuteQuery()
        {
            var (sql, parameters) = _provider.ToSqlWithParameters(_expression);
            var results = new List<T>();

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
                using var reader = command.ExecuteReader();
                var mapper = CreateMapper(reader);

                while (reader.Read())
                {
                    results.Add(mapper(reader));
                }
            }
            finally
            {
                if (!wasOpen) _connection.Close();
            }

            return results;
        }

        private static Func<IDataReader, T> CreateMapper(IDataReader reader)
        {
            var type = typeof(T);

            // Handle primitive types
            if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal) ||
                type == typeof(DateTime) || type == typeof(Guid))
            {
                return r => (T)Convert.ChangeType(r.GetValue(0), type);
            }

            // Handle nullable types
            var underlyingType = Nullable.GetUnderlyingType(type);
            if (underlyingType != null)
            {
                return r =>
                {
                    var value = r.GetValue(0);
                    if (value == DBNull.Value) return default!;
                    return (T)Convert.ChangeType(value, underlyingType);
                };
            }

            // Handle anonymous types and complex objects
            var properties = type.GetProperties();
            var columnOrdinals = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < reader.FieldCount; i++)
            {
                columnOrdinals[reader.GetName(i)] = i;
            }

            return r =>
            {
                var instance = Activator.CreateInstance<T>();
                foreach (var prop in properties)
                {
                    if (!prop.CanWrite) continue;
                    if (!columnOrdinals.TryGetValue(prop.Name, out var ordinal)) continue;

                    var value = r.GetValue(ordinal);
                    if (value == DBNull.Value)
                    {
                        prop.SetValue(instance, null);
                    }
                    else
                    {
                        var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                        prop.SetValue(instance, Convert.ChangeType(value, targetType));
                    }
                }
                return instance;
            };
        }
    }
}
