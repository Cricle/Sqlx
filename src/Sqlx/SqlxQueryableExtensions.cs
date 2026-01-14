// -----------------------------------------------------------------------
// <copyright file="SqlxQueryableExtensions.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sqlx
{
    /// <summary>
    /// Extension methods for SqlxQueryable.
    /// </summary>
    public static class SqlxQueryableExtensions
    {
        /// <summary>Generates SQL from the query.</summary>
        public static string ToSql<T>(this IQueryable<T> query)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));

            if (query.Provider is SqlxQueryProvider provider)
            {
                return provider.ToSql(query.Expression);
            }

            throw new InvalidOperationException("ToSql() can only be called on SqlxQueryable instances.");
        }

        /// <summary>Generates SQL with parameters from the query.</summary>
        public static (string Sql, Dictionary<string, object?> Parameters) ToSqlWithParameters<T>(this IQueryable<T> query)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));

            if (query.Provider is SqlxQueryProvider provider)
            {
                return provider.ToSqlWithParameters(query.Expression);
            }

            throw new InvalidOperationException("ToSqlWithParameters() can only be called on SqlxQueryable instances.");
        }

        /// <summary>Executes the query and returns a list of results.</summary>
        public static List<T> ToList<T>(this SqlxQueryable<T> query)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            return query.ToList();
        }

        /// <summary>Executes the query and returns the first result or default.</summary>
        public static T? FirstOrDefault<T>(this SqlxQueryable<T> query)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));

            var limited = query.Take(1);
            var results = limited.ToList();
            return results.Count > 0 ? results[0] : default;
        }

        /// <summary>Executes the query and returns the first result.</summary>
        public static T First<T>(this SqlxQueryable<T> query)
        {
            var result = query.FirstOrDefault();
            if (result == null)
                throw new InvalidOperationException("Sequence contains no elements.");
            return result;
        }

        /// <summary>Executes the query and returns a single result or default.</summary>
        public static T? SingleOrDefault<T>(this SqlxQueryable<T> query)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));

            var limited = query.Take(2);
            var results = limited.ToList();

            if (results.Count > 1)
                throw new InvalidOperationException("Sequence contains more than one element.");

            return results.Count > 0 ? results[0] : default;
        }

        /// <summary>Executes the query and returns a single result.</summary>
        public static T Single<T>(this SqlxQueryable<T> query)
        {
            var result = query.SingleOrDefault();
            if (result == null)
                throw new InvalidOperationException("Sequence contains no elements.");
            return result;
        }

        /// <summary>Executes the query asynchronously and returns a list of results.</summary>
        public static async Task<List<T>> ToListAsync<T>(this SqlxQueryable<T> query, CancellationToken cancellationToken = default)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (query.Connection == null)
                throw new InvalidOperationException("No database connection. Use WithConnection() before executing.");

            var (sql, parameters) = query.ToSqlWithParameters();
            var results = new List<T>();

            using var command = query.Connection.CreateCommand();
            command.CommandText = sql;

            foreach (var param in parameters)
            {
                var dbParam = command.CreateParameter();
                dbParam.ParameterName = param.Key;
                dbParam.Value = param.Value ?? DBNull.Value;
                command.Parameters.Add(dbParam);
            }

            var wasOpen = query.Connection.State == ConnectionState.Open;
            if (!wasOpen) await OpenConnectionAsync(query.Connection, cancellationToken);

            try
            {
                using var reader = await ExecuteReaderAsync(command, cancellationToken);
                var mapper = CreateMapper<T>(reader);

                while (await ReadAsync(reader, cancellationToken))
                {
                    results.Add(mapper(reader));
                }
            }
            finally
            {
                if (!wasOpen) query.Connection.Close();
            }

            return results;
        }

        /// <summary>Executes the query asynchronously and returns the first result or default.</summary>
        public static async Task<T?> FirstOrDefaultAsync<T>(this SqlxQueryable<T> query, CancellationToken cancellationToken = default)
        {
            var limited = query.Take(1);
            var results = await ((SqlxQueryable<T>)limited).ToListAsync(cancellationToken);
            return results.Count > 0 ? results[0] : default;
        }

        /// <summary>Executes the query asynchronously and returns the first result.</summary>
        public static async Task<T> FirstAsync<T>(this SqlxQueryable<T> query, CancellationToken cancellationToken = default)
        {
            var result = await query.FirstOrDefaultAsync(cancellationToken);
            if (result == null)
                throw new InvalidOperationException("Sequence contains no elements.");
            return result;
        }

        /// <summary>Executes the query asynchronously and returns a single result or default.</summary>
        public static async Task<T?> SingleOrDefaultAsync<T>(this SqlxQueryable<T> query, CancellationToken cancellationToken = default)
        {
            var limited = query.Take(2);
            var results = await ((SqlxQueryable<T>)limited).ToListAsync(cancellationToken);

            if (results.Count > 1)
                throw new InvalidOperationException("Sequence contains more than one element.");

            return results.Count > 0 ? results[0] : default;
        }

        /// <summary>Executes the query asynchronously and returns a single result.</summary>
        public static async Task<T> SingleAsync<T>(this SqlxQueryable<T> query, CancellationToken cancellationToken = default)
        {
            var result = await query.SingleOrDefaultAsync(cancellationToken);
            if (result == null)
                throw new InvalidOperationException("Sequence contains no elements.");
            return result;
        }

        private static async Task OpenConnectionAsync(DbConnection connection, CancellationToken cancellationToken)
        {
#if NETSTANDARD2_0
            await Task.Run(() => connection.Open(), cancellationToken);
#else
            await connection.OpenAsync(cancellationToken);
#endif
        }

        private static async Task<DbDataReader> ExecuteReaderAsync(DbCommand command, CancellationToken cancellationToken)
        {
#if NETSTANDARD2_0
            return await Task.Run(() => command.ExecuteReader(), cancellationToken);
#else
            return await command.ExecuteReaderAsync(cancellationToken);
#endif
        }

        private static async Task<bool> ReadAsync(DbDataReader reader, CancellationToken cancellationToken)
        {
#if NETSTANDARD2_0
            return await Task.Run(() => reader.Read(), cancellationToken);
#else
            return await reader.ReadAsync(cancellationToken);
#endif
        }

        private static Func<IDataReader, T> CreateMapper<T>(IDataReader reader)
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

            // Handle complex objects
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
