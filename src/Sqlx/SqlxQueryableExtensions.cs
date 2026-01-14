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
    /// Extension methods for IQueryable.
    /// </summary>
    public static class SqlxQueryableExtensions
    {
        /// <summary>Sets the database connection.</summary>
        public static IQueryable<T> WithConnection<T>(this IQueryable<T> query, DbConnection connection)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (connection == null) throw new ArgumentNullException(nameof(connection));

            if (query.Provider is SqlxQueryProvider provider)
            {
                provider.Connection = connection;
                return query;
            }

            throw new InvalidOperationException("WithConnection() can only be called on SqlxQueryable instances.");
        }

        /// <summary>Sets the mapper function.</summary>
        public static IQueryable<T> WithMapper<T>(this IQueryable<T> query, Func<IDataReader, T> mapper)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));

            if (query.Provider is SqlxQueryProvider provider)
            {
                provider.Mapper = r => mapper(r)!;
                return query;
            }

            throw new InvalidOperationException("WithMapper() can only be called on SqlxQueryable instances.");
        }

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

        /// <summary>Gets the parameters from the query.</summary>
        public static IEnumerable<KeyValuePair<string, object?>> GetParameters<T>(this IQueryable<T> query)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));

            if (query.Provider is SqlxQueryProvider provider)
            {
                return provider.GetParameters(query.Expression);
            }

            throw new InvalidOperationException("GetParameters() can only be called on SqlxQueryable instances.");
        }

        /// <summary>Executes the query asynchronously and returns a list of results.</summary>
        public static Task<List<T>> ToListAsync<T>(this IQueryable<T> query, Func<IDataReader, T> mapper, CancellationToken cancellationToken = default)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));

            if (query.Provider is SqlxQueryProvider provider)
            {
                if (provider.Connection == null)
                    throw new InvalidOperationException("No database connection. Use WithConnection() before executing.");

                var sql = provider.ToSql(query.Expression);
                var parameters = provider.GetParameters(query.Expression);
                return DbExecutor.ExecuteReaderAsync(provider.Connection, sql, parameters, mapper, cancellationToken);
            }

            throw new InvalidOperationException("ToListAsync() can only be called on SqlxQueryable instances.");
        }

        /// <summary>Executes the query asynchronously and returns the first result or default.</summary>
        public static async Task<T?> FirstOrDefaultAsync<T>(this IQueryable<T> query, Func<IDataReader, T> mapper, CancellationToken cancellationToken = default)
        {
            var results = await query.Take(1).ToListAsync(mapper, cancellationToken);
            return results.Count > 0 ? results[0] : default;
        }

        /// <summary>Executes the query asynchronously and returns a single result or default.</summary>
        public static async Task<T?> SingleOrDefaultAsync<T>(this IQueryable<T> query, Func<IDataReader, T> mapper, CancellationToken cancellationToken = default)
        {
            var results = await query.Take(2).ToListAsync(mapper, cancellationToken);
            if (results.Count > 1)
                throw new InvalidOperationException("Sequence contains more than one element.");
            return results.Count > 0 ? results[0] : default;
        }
    }
}
