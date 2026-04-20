// -----------------------------------------------------------------------
// <copyright file="DbExecutor.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
#if NET5_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace Sqlx
{
    /// <summary>
    /// ADO.NET executor
    /// </summary>
    internal static class DbExecutor
    {
        /// <summary>
        /// Executes a query and yields results lazily.
        /// </summary>
        /// <typeparam name="T">The result type.</typeparam>
        /// <param name="connection">The database connection.</param>
        /// <param name="sql">The SQL query.</param>
        /// <param name="parameters">The query parameters.</param>
        /// <param name="mapper">The result mapper function.</param>
        /// <param name="transaction">The database transaction, if any.</param>
        /// <returns>An enumerable of results.</returns>
        public static IEnumerable<T> ExecuteReader<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
            T>(
            DbConnection connection,
            string sql,
            IEnumerable<KeyValuePair<string, object?>>? parameters,
            IResultReader<T> mapper,
            DbTransaction? transaction = null)
        {
            var shouldCloseConnection = EnsureConnectionOpen(connection);

            try
            {
                using var command = CreateCommand(connection, sql, parameters, transaction);
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    yield return mapper.Read(reader);
                }
            }
            finally
            {
                CloseConnection(connection, transaction, shouldCloseConnection);
            }
        }

        /// <summary>
        /// Executes a query asynchronously and yields results.
        /// </summary>
        /// <typeparam name="T">The result type.</typeparam>
        /// <param name="connection">The database connection.</param>
        /// <param name="sql">The SQL query.</param>
        /// <param name="parameters">The query parameters.</param>
        /// <param name="mapper">The result mapper function.</param>
        /// <param name="transaction">The database transaction, if any.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>An async enumerable of results.</returns>
        public static async IAsyncEnumerator<T> ExecuteReaderAsync<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
            T>(
            DbConnection connection,
            string sql,
            IEnumerable<KeyValuePair<string, object?>>? parameters,
            IResultReader<T> mapper,
            DbTransaction? transaction = null,
            CancellationToken cancellationToken = default)
        {
            var shouldCloseConnection = await EnsureConnectionOpenAsync(connection, cancellationToken).ConfigureAwait(false);

            try
            {
                await using var command = CreateCommand(connection, sql, parameters, transaction);
                await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    yield return mapper.Read(reader);
                }
            }
            finally
            {
                CloseConnection(connection, transaction, shouldCloseConnection);
            }
        }

        /// <summary>
        /// Executes a scalar query and returns a single value.
        /// </summary>
        /// <param name="connection">The database connection.</param>
        /// <param name="sql">The SQL query.</param>
        /// <param name="parameters">The query parameters.</param>
        /// <param name="transaction">The database transaction, if any.</param>
        /// <returns>The scalar result.</returns>
        public static object? ExecuteScalar(
            DbConnection connection,
            string sql,
            IEnumerable<KeyValuePair<string, object?>>? parameters,
            DbTransaction? transaction = null)
        {
            var shouldCloseConnection = EnsureConnectionOpen(connection);

            try
            {
                using var command = CreateCommand(connection, sql, parameters, transaction);
                return command.ExecuteScalar();
            }
            finally
            {
                CloseConnection(connection, transaction, shouldCloseConnection);
            }
        }

        /// <summary>
        /// Executes a scalar query asynchronously and returns a single value.
        /// </summary>
        /// <param name="connection">The database connection.</param>
        /// <param name="sql">The SQL query.</param>
        /// <param name="parameters">The query parameters.</param>
        /// <param name="transaction">The database transaction, if any.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The scalar result.</returns>
        public static async Task<object?> ExecuteScalarAsync(
            DbConnection connection,
            string sql,
            IEnumerable<KeyValuePair<string, object?>>? parameters,
            DbTransaction? transaction = null,
            CancellationToken cancellationToken = default)
        {
            var shouldCloseConnection = await EnsureConnectionOpenAsync(connection, cancellationToken).ConfigureAwait(false);

            try
            {
                await using var command = CreateCommand(connection, sql, parameters, transaction);
                return await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                CloseConnection(connection, transaction, shouldCloseConnection);
            }
        }

        /// <summary>
        /// Executes a command and returns the number of affected rows.
        /// </summary>
        /// <param name="connection">The database connection.</param>
        /// <param name="sql">The SQL command.</param>
        /// <param name="parameters">The command parameters.</param>
        /// <param name="transaction">The database transaction, if any.</param>
        /// <returns>The number of affected rows.</returns>
        public static int ExecuteNonQuery(
            DbConnection connection,
            string sql,
            IEnumerable<KeyValuePair<string, object?>>? parameters,
            DbTransaction? transaction = null)
        {
            var shouldCloseConnection = EnsureConnectionOpen(connection);

            try
            {
                using var command = CreateCommand(connection, sql, parameters, transaction);
                return command.ExecuteNonQuery();
            }
            finally
            {
                CloseConnection(connection, transaction, shouldCloseConnection);
            }
        }

        /// <summary>
        /// Executes a command asynchronously and returns the number of affected rows.
        /// </summary>
        /// <param name="connection">The database connection.</param>
        /// <param name="sql">The SQL command.</param>
        /// <param name="parameters">The command parameters.</param>
        /// <param name="transaction">The database transaction, if any.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The number of affected rows.</returns>
        public static async Task<int> ExecuteNonQueryAsync(
            DbConnection connection,
            string sql,
            IEnumerable<KeyValuePair<string, object?>>? parameters,
            DbTransaction? transaction = null,
            CancellationToken cancellationToken = default)
        {
            var shouldCloseConnection = await EnsureConnectionOpenAsync(connection, cancellationToken).ConfigureAwait(false);

            try
            {
                await using var command = CreateCommand(connection, sql, parameters, transaction);
                return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                CloseConnection(connection, transaction, shouldCloseConnection);
            }
        }

        private static bool EnsureConnectionOpen(DbConnection connection)
        {
            if (connection.State == ConnectionState.Open)
            {
                return false;
            }

            connection.Open();
            return true;
        }

        private static async Task<bool> EnsureConnectionOpenAsync(
            DbConnection connection,
            CancellationToken cancellationToken)
        {
            if (connection.State == ConnectionState.Open)
            {
                return false;
            }

            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }

        private static void CloseConnection(
            DbConnection connection,
            DbTransaction? transaction,
            bool shouldCloseConnection)
        {
            if (shouldCloseConnection && transaction == null && connection.State != ConnectionState.Closed) connection.Close();
        }

        private static DbCommand CreateCommand(
            DbConnection connection,
            string sql,
            IEnumerable<KeyValuePair<string, object?>>? parameters,
            DbTransaction? transaction = null)
        {
            var command = connection.CreateCommand();
            command.CommandText = sql;
            if (transaction != null)
            {
                command.Transaction = transaction;
            }

            if (parameters != null)
            {
                foreach (var p in parameters)
                {
                    var param = command.CreateParameter();
                    param.ParameterName = p.Key;
                    param.Value = p.Value ?? DBNull.Value;
                    command.Parameters.Add(param);
                }
            }

            return command;
        }
    }
}
