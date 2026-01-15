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
#if NET5_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace Sqlx
{
    /// <summary>
    /// Extension methods for IQueryable.
    /// </summary>
    public static class SqlxQueryableExtensions
    {
        /// <summary>
        /// Sets the database connection for query execution.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="query">The query.</param>
        /// <param name="connection">The database connection.</param>
        /// <returns>The query with connection set.</returns>
        public static IQueryable<T> WithConnection<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
            T>(this IQueryable<T> query, DbConnection connection)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            if (query is SqlxQueryable<T> sqlxQuery)
            {
                sqlxQuery.Connection = connection;
                return query;
            }

            throw new InvalidOperationException("WithConnection() can only be called on SqlxQueryable instances.");
        }

        /// <summary>
        /// Sets the mapper function for converting database results to entities.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="query">The query.</param>
        /// <param name="mapper">The mapper function.</param>
        /// <returns>The query with mapper set.</returns>
        public static IQueryable<T> WithMapper<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
            T>(this IQueryable<T> query, Func<IDataReader, T> mapper)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            if (mapper == null)
            {
                throw new ArgumentNullException(nameof(mapper));
            }

            if (query is SqlxQueryable<T> sqlxQuery)
            {
                sqlxQuery.Mapper = mapper;
                return query;
            }

            throw new InvalidOperationException("WithMapper() can only be called on SqlxQueryable instances.");
        }

        /// <summary>
        /// Generates SQL from the query (with inline values).
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="query">The query.</param>
        /// <returns>The generated SQL string.</returns>
        public static string ToSql<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
            T>(this IQueryable<T> query)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            if (query.Provider is SqlxQueryProvider provider)
            {
                return provider.ToSql(query.Expression);
            }

            throw new InvalidOperationException("ToSql() can only be called on SqlxQueryable instances.");
        }

        /// <summary>
        /// Generates parameterized SQL and parameters from the query.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="query">The query.</param>
        /// <returns>A tuple containing the SQL string and parameters.</returns>
        public static (string Sql, IEnumerable<KeyValuePair<string, object?>> Parameters) ToSqlWithParameters<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
            T>(this IQueryable<T> query)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            if (query.Provider is SqlxQueryProvider provider)
            {
                return provider.ToSqlWithParameters(query.Expression);
            }

            throw new InvalidOperationException("ToSqlWithParameters() can only be called on SqlxQueryable instances.");
        }
    }
}
