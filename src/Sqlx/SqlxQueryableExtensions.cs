// -----------------------------------------------------------------------
// <copyright file="SqlxQueryableExtensions.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
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
        public static Task<List<T>> ToListAsync<T>(this SqlxQueryable<T> query, Func<IDataReader, T> mapper, CancellationToken cancellationToken = default)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));
            if (query.Connection == null)
                throw new InvalidOperationException("No database connection. Use WithConnection() before executing.");

            var sql = query.ToSql();
            var parameters = query.GetParameters().ToDictionary(x => x.Key, x => x.Value);
            return DbExecutor.ExecuteReaderAsync(query.Connection, sql, parameters, mapper, cancellationToken);
        }

        /// <summary>Executes the query asynchronously and returns the first result or default.</summary>
        public static async Task<T?> FirstOrDefaultAsync<T>(this SqlxQueryable<T> query, Func<IDataReader, T> mapper, CancellationToken cancellationToken = default)
        {
            var limited = (SqlxQueryable<T>)query.Take(1);
            limited.Connection = query.Connection;
            var results = await limited.ToListAsync(mapper, cancellationToken);
            return results.Count > 0 ? results[0] : default;
        }

        /// <summary>Executes the query asynchronously and returns a single result or default.</summary>
        public static async Task<T?> SingleOrDefaultAsync<T>(this SqlxQueryable<T> query, Func<IDataReader, T> mapper, CancellationToken cancellationToken = default)
        {
            var limited = (SqlxQueryable<T>)query.Take(2);
            limited.Connection = query.Connection;
            var results = await limited.ToListAsync(mapper, cancellationToken);
            if (results.Count > 1)
                throw new InvalidOperationException("Sequence contains more than one element.");
            return results.Count > 0 ? results[0] : default;
        }
    }
}
