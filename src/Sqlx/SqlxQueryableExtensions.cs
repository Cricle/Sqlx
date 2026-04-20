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
using System.Threading;
using System.Threading.Tasks;
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
        /// Executes the query and materializes all rows into a list using the query's configured reader.
        /// This avoids the per-row async-enumerator overhead of generic async LINQ adapters.
        /// </summary>
        public static async Task<List<T>> ToListAsync<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
            T>(this IQueryable<T> query, CancellationToken cancellationToken = default)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            var sqlxQuery = GetSqlxQueryableOrThrow(query);
            var reader = sqlxQuery.ResultReader
                ?? throw new InvalidOperationException("No result reader. Use WithReader().");
            var connection = sqlxQuery.Connection
                ?? throw new InvalidOperationException("No database connection. Use WithConnection().");

            var provider = (SqlxQueryProvider<T>)sqlxQuery.Provider;
            var (sql, parameters) = provider.ToSqlWithParameters(query.Expression);

            var shouldCloseConnection = await EnsureConnectionOpenAsync(connection, cancellationToken).ConfigureAwait(false);
            try
            {
                await using var command = CreateCommand(connection, sql, parameters, sqlxQuery.Transaction);
                await using var dbReader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

                var capacityHint = TryGetTakeCount(query.Expression);
                return capacityHint.HasValue
                    ? await reader.ToListAsync(dbReader, capacityHint.Value, cancellationToken).ConfigureAwait(false)
                    : await reader.ToListAsync(dbReader, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                CloseConnectionIfNeeded(connection, sqlxQuery.Transaction, shouldCloseConnection);
            }
        }

        /// <summary>
        /// Executes the query and returns the first row or default.
        /// </summary>
        public static async Task<T?> FirstOrDefaultAsync<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
            T>(this IQueryable<T> query, CancellationToken cancellationToken = default)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            var sqlxQuery = GetSqlxQueryableOrThrow(query);
            var reader = sqlxQuery.ResultReader
                ?? throw new InvalidOperationException("No result reader. Use WithReader().");
            var connection = sqlxQuery.Connection
                ?? throw new InvalidOperationException("No database connection. Use WithConnection().");

            var provider = (SqlxQueryProvider<T>)sqlxQuery.Provider;
            var (sql, parameters) = provider.ToSqlWithParameters(query.Expression);

            var shouldCloseConnection = await EnsureConnectionOpenAsync(connection, cancellationToken).ConfigureAwait(false);
            try
            {
                await using var command = CreateCommand(connection, sql, parameters, sqlxQuery.Transaction);
                await using var dbReader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                return await reader.FirstOrDefaultAsync(dbReader, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                CloseConnectionIfNeeded(connection, sqlxQuery.Transaction, shouldCloseConnection);
            }
        }

        /// <summary>
        /// Executes the query as COUNT(*) over the generated SQL.
        /// </summary>
        public static async Task<long> CountAsync<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
            T>(this IQueryable<T> query, CancellationToken cancellationToken = default)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            var sqlxQuery = GetSqlxQueryableOrThrow(query);
            var connection = sqlxQuery.Connection
                ?? throw new InvalidOperationException("No database connection. Use WithConnection().");

            var provider = (SqlxQueryProvider<T>)sqlxQuery.Provider;
            var (sql, parameters) = provider.ToSqlWithParameters(query.Expression);
            var countSql = $"SELECT COUNT(*) FROM ({sql}) AS q";
            var result = await DbExecutor.ExecuteScalarAsync(connection, countSql, parameters, sqlxQuery.Transaction, cancellationToken).ConfigureAwait(false);
            return Convert.ToInt64(result);
        }

        /// <summary>
        /// Executes the query and returns whether it yields at least one row.
        /// </summary>
        public static async Task<bool> AnyAsync<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
            T>(this IQueryable<T> query, CancellationToken cancellationToken = default)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            var sqlxQuery = GetSqlxQueryableOrThrow(query);
            var connection = sqlxQuery.Connection
                ?? throw new InvalidOperationException("No database connection. Use WithConnection().");

            var provider = (SqlxQueryProvider<T>)sqlxQuery.Provider;
            var (sql, parameters) = provider.ToSqlWithParameters(query.Expression);
            var existsSql = $"SELECT 1 FROM ({sql}) AS q";
            var result = await DbExecutor.ExecuteScalarAsync(connection, existsSql, parameters, sqlxQuery.Transaction, cancellationToken).ConfigureAwait(false);
            return result != null && result != DBNull.Value;
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

        private static DbCommand CreateCommand(
            DbConnection connection,
            string sql,
            IEnumerable<KeyValuePair<string, object?>> parameters,
            DbTransaction? transaction)
        {
            var command = connection.CreateCommand();
            command.CommandText = sql;
            if (transaction != null)
            {
                command.Transaction = transaction;
            }

            AddParameters(command, parameters);

            return command;
        }

        private static void AddParameters(
            DbCommand command,
            IEnumerable<KeyValuePair<string, object?>> parameters)
        {
            if (parameters is Dictionary<string, object?> dictionary)
            {
                foreach (var parameter in dictionary)
                {
                    AddParameter(command, parameter.Key, parameter.Value);
                }

                return;
            }

            if (parameters is IReadOnlyList<KeyValuePair<string, object?>> parameterList)
            {
                for (var i = 0; i < parameterList.Count; i++)
                {
                    var parameter = parameterList[i];
                    AddParameter(command, parameter.Key, parameter.Value);
                }

                return;
            }

            foreach (var parameter in parameters)
            {
                AddParameter(command, parameter.Key, parameter.Value);
            }
        }

        private static void AddParameter(DbCommand command, string name, object? value)
        {
            var dbParameter = command.CreateParameter();
            dbParameter.ParameterName = name;
            dbParameter.Value = value ?? DBNull.Value;
            command.Parameters.Add(dbParameter);
        }

        private static async Task<bool> EnsureConnectionOpenAsync(DbConnection connection, CancellationToken cancellationToken)
        {
            if (connection.State == System.Data.ConnectionState.Open)
            {
                return false;
            }

            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }

        private static void CloseConnectionIfNeeded(DbConnection connection, DbTransaction? transaction, bool shouldCloseConnection)
        {
            if (shouldCloseConnection &&
                transaction == null &&
                connection.State != System.Data.ConnectionState.Closed)
            {
                connection.Close();
            }
        }

        private static int? TryGetTakeCount(Expression expression)
        {
            if (expression is MethodCallExpression call)
            {
                if (string.Equals(call.Method.Name, "Take", StringComparison.Ordinal) &&
                    call.Arguments.Count > 1 &&
                    call.Arguments[1] is ConstantExpression { Value: int takeCount })
                {
                    return takeCount;
                }

                if (call.Arguments.Count > 0)
                {
                    return TryGetTakeCount(call.Arguments[0]);
                }
            }

            return null;
        }
    }
}
