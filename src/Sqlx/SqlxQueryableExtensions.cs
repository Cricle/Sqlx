// -----------------------------------------------------------------------
// <copyright file="SqlxQueryableExtensions.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
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
        /// Sets the result reader for converting database results to entities.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="query">The query.</param>
        /// <param name="reader">The result reader.</param>
        /// <returns>The query with reader set.</returns>
        public static IQueryable<T> WithReader<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
            T>(this IQueryable<T> query, IResultReader<T> reader)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            if (query is SqlxQueryable<T> sqlxQuery)
            {
                sqlxQuery.ResultReader = reader;
                return query;
            }

            throw new InvalidOperationException("WithReader() can only be called on SqlxQueryable instances.");
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

            if (query.Provider is SqlxQueryProvider<T> provider)
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

            if (query.Provider is SqlxQueryProvider<T> provider)
            {
                return provider.ToSqlWithParameters(query.Expression);
            }

            throw new InvalidOperationException("ToSqlWithParameters() can only be called on SqlxQueryable instances.");
        }

        /// <summary>
        /// Creates a new query using the current query as a subquery source.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="subQuery">The subquery.</param>
        /// <returns>A new query that uses the subquery as its source.</returns>
        public static IQueryable<T> AsSubQuery<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
            T>(this IQueryable<T> subQuery)
        {
            if (subQuery == null)
            {
                throw new ArgumentNullException(nameof(subQuery));
            }

            if (subQuery is SqlxQueryable<T> sqlxQuery)
            {
                // Create a new queryable that wraps the subquery
                var provider = (SqlxQueryProvider<T>)sqlxQuery.Provider;
                return new SqlxQueryable<T>(
                    new SqlxQueryProvider<T>(provider.Dialect, provider.EntityProvider)
                    {
                        Connection = sqlxQuery.Connection,
                        ResultReader = sqlxQuery.ResultReader
                    },
                    Expression.Constant(sqlxQuery),
                    sqlxQuery.Connection,
                    sqlxQuery.ResultReader);
            }

            throw new InvalidOperationException("AsSubQuery() can only be called on SqlxQueryable instances.");
        }
    }
}
