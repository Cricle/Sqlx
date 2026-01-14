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
    /// Extension methods for SqlxQueryable (AOT-friendly, no reflection).
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
        public static List<T> ToList<T>(this SqlxQueryable<T> query, Func<IDataReader, T> mapper)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));
            if (query.Connection == null)
                throw new InvalidOperationException("No database connection. Use WithConnection() before executing.");

            var (sql, parameters) = query.ToSqlWithParameters();
            return DbExecutor.ExecuteList(query.Connection, sql, parameters, mapper);
        }

        /// <summary>Executes the query asynchronously and returns a list of results.</summary>
        public static Task<List<T>> ToListAsync<T>(this SqlxQueryable<T> query, Func<IDataReader, T> mapper, CancellationToken cancellationToken = default)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));
            if (query.Connection == null)
                throw new InvalidOperationException("No database connection. Use WithConnection() before executing.");

            var (sql, parameters) = query.ToSqlWithParameters();
            return DbExecutor.ExecuteListAsync(query.Connection, sql, parameters, mapper, cancellationToken);
        }
    }
}
