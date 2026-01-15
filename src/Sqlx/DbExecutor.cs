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
        /// <returns>An enumerable of results.</returns>
        public static IEnumerable<T> ExecuteReader<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
            T>(
            DbConnection connection,
            string sql,
            IEnumerable<KeyValuePair<string, object?>>? parameters,
            IResultReader<T> mapper)
        {
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }
            using var command = CreateCommand(connection, sql, parameters);
            using var reader = command.ExecuteReader();
            var origin = mapper.GetOrdinals(reader);
            while (reader.Read())
            {
                yield return mapper.Read(reader, origin);
            }
        }

        /// <summary>
        /// Executes a scalar query.
        /// </summary>
        /// <typeparam name="T">The result type.</typeparam>
        /// <param name="connection">The database connection.</param>
        /// <param name="sql">The SQL query.</param>
        /// <param name="parameters">The query parameters.</param>
        /// <returns>The scalar result.</returns>
        public static T? ExecuteScalar<T>(
            DbConnection connection,
            string sql,
            IEnumerable<KeyValuePair<string, object?>>? parameters)
        {
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }
            using var command = CreateCommand(connection, sql, parameters);
            var result = command.ExecuteScalar();
            return result == null || result == DBNull.Value ? default : (T)result;
        }

        /// <summary>
        /// Executes a non-query command.
        /// </summary>
        /// <param name="connection">The database connection.</param>
        /// <param name="sql">The SQL command.</param>
        /// <param name="parameters">The command parameters.</param>
        /// <returns>The number of rows affected.</returns>
        public static int ExecuteNonQuery(
            DbConnection connection,
            string sql,
            IEnumerable<KeyValuePair<string, object?>>? parameters)
        {
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }
            using var command = CreateCommand(connection, sql, parameters);
            return command.ExecuteNonQuery();
        }

        /// <summary>
        /// Executes a query asynchronously and yields results.
        /// </summary>
        /// <typeparam name="T">The result type.</typeparam>
        /// <param name="connection">The database connection.</param>
        /// <param name="sql">The SQL query.</param>
        /// <param name="parameters">The query parameters.</param>
        /// <param name="mapper">The result mapper function.</param>
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
            CancellationToken cancellationToken = default)
        {

            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync(cancellationToken);
            }

            await using var command = CreateCommand(connection, sql, parameters);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var origin = mapper.GetOrdinals(reader);
            while (await reader.ReadAsync(cancellationToken))
            {
                yield return mapper.Read(reader, origin);
            }
        }

        /// <summary>
        /// Executes a scalar query asynchronously.
        /// </summary>
        /// <typeparam name="T">The result type.</typeparam>
        /// <param name="connection">The database connection.</param>
        /// <param name="sql">The SQL query.</param>
        /// <param name="parameters">The query parameters.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The scalar result.</returns>
        public static async Task<T?> ExecuteScalarAsync<T>(
            DbConnection connection,
            string sql,
            IEnumerable<KeyValuePair<string, object?>>? parameters,
            CancellationToken cancellationToken = default)
        {

            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync(cancellationToken);
            }

            await using var command = CreateCommand(connection, sql, parameters);
            var result = await command.ExecuteScalarAsync(cancellationToken);
            return result == null || result == DBNull.Value ? default : (T)result;
        }

        /// <summary>
        /// Executes a non-query command asynchronously.
        /// </summary>
        /// <param name="connection">The database connection.</param>
        /// <param name="sql">The SQL command.</param>
        /// <param name="parameters">The command parameters.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The number of rows affected.</returns>
        public static async Task<int> ExecuteNonQueryAsync(
            DbConnection connection,
            string sql,
            IEnumerable<KeyValuePair<string, object?>>? parameters,
            CancellationToken cancellationToken = default)
        {
            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync(cancellationToken);
            }

            await using var command = CreateCommand(connection, sql, parameters);
            return await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private static DbCommand CreateCommand(
            DbConnection connection,
            string sql,
            IEnumerable<KeyValuePair<string, object?>>? parameters)
        {
            var command = connection.CreateCommand();
            command.CommandText = sql;

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
