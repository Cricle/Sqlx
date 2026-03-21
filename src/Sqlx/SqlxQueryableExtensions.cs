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
        private static SqlxQueryable<T> GetSqlxQueryableOrThrow<T>(IQueryable<T> query)
        {
            return query as SqlxQueryable<T>
                ?? throw new InvalidOperationException("This method can only be called on SqlxQueryable instances.");
        }

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

            var sqlxQuery = GetSqlxQueryableOrThrow(query);
            EnsureConnectionMatchesTransaction(sqlxQuery.Transaction, connection);
            sqlxQuery.Connection = connection;
            return query;
        }

        /// <summary>
        /// Sets the database transaction for query execution.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="query">The query.</param>
        /// <param name="transaction">The database transaction.</param>
        /// <returns>The query with transaction set.</returns>
        public static IQueryable<T> WithTransaction<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
            T>(this IQueryable<T> query, DbTransaction transaction)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            if (transaction == null)
            {
                throw new ArgumentNullException(nameof(transaction));
            }

            var connection = transaction.Connection
                ?? throw new InvalidOperationException("The provided transaction is not associated with a connection.");

            var sqlxQuery = GetSqlxQueryableOrThrow(query);
            EnsureTransactionMatchesConnection(sqlxQuery.Connection, transaction);

            sqlxQuery.Transaction = transaction;
            sqlxQuery.Connection ??= connection;
            return query;
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

            var sqlxQuery = GetSqlxQueryableOrThrow(query);
            sqlxQuery.ResultReader = reader;
            return query;
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

            var sqlxQuery = GetSqlxQueryableOrThrow(subQuery);

            // Create a new queryable that wraps the subquery
            var provider = (SqlxQueryProvider<T>)sqlxQuery.Provider;
            return new SqlxQueryable<T>(
                new SqlxQueryProvider<T>(provider.Dialect, provider.EntityProvider)
                {
                    Connection = sqlxQuery.Connection,
                    Transaction = sqlxQuery.Transaction,
                    ResultReader = sqlxQuery.ResultReader
                },
                Expression.Constant(sqlxQuery),
                sqlxQuery.Connection,
                sqlxQuery.ResultReader,
                sqlxQuery.Transaction);
        }

        private static void EnsureTransactionMatchesConnection(DbConnection? connection, DbTransaction transaction)
        {
            var transactionConnection = transaction.Connection
                ?? throw new InvalidOperationException("The provided transaction is not associated with a connection.");

            if (connection != null && !ReferenceEquals(connection, transactionConnection))
            {
                throw new InvalidOperationException(
                    "The provided transaction belongs to a different connection than the query.");
            }
        }

        private static void EnsureConnectionMatchesTransaction(DbTransaction? transaction, DbConnection connection)
        {
            if (transaction?.Connection != null && !ReferenceEquals(transaction.Connection, connection))
            {
                throw new InvalidOperationException(
                    "The provided connection does not match the query transaction connection.");
            }
        }
    }
}
