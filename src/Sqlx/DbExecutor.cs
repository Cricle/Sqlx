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

namespace Sqlx
{
    /// <summary>
    /// ADO.NET executor for query execution (AOT-friendly, no reflection).
    /// </summary>
    public static class DbExecutor
    {
        /// <summary>Executes a query and yields results lazily.</summary>
        public static IEnumerable<T> ExecuteReader<T>(
            DbConnection connection,
            string sql,
            IEnumerable<KeyValuePair<string, object?>>? parameters,
            Func<IDataReader, T> mapper)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            if (sql == null) throw new ArgumentNullException(nameof(sql));
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));

            using var command = CreateCommand(connection, sql, parameters);

            var wasOpen = connection.State == ConnectionState.Open;
            if (!wasOpen) connection.Open();

            try
            {
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    yield return mapper(reader);
                }
            }
            finally
            {
                if (!wasOpen) connection.Close();
            }
        }

        /// <summary>Executes a query and returns a scalar value.</summary>
        public static T? ExecuteScalar<T>(
            DbConnection connection,
            string sql,
            IEnumerable<KeyValuePair<string, object?>>? parameters)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            if (sql == null) throw new ArgumentNullException(nameof(sql));

            using var command = CreateCommand(connection, sql, parameters);

            var wasOpen = connection.State == ConnectionState.Open;
            if (!wasOpen) connection.Open();

            try
            {
                var result = command.ExecuteScalar();
                if (result == null || result == DBNull.Value)
                    return default;

                return (T)result;
            }
            finally
            {
                if (!wasOpen) connection.Close();
            }
        }

        /// <summary>Executes a non-query command and returns affected rows.</summary>
        public static int ExecuteNonQuery(
            DbConnection connection,
            string sql,
            IEnumerable<KeyValuePair<string, object?>>? parameters)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            if (sql == null) throw new ArgumentNullException(nameof(sql));

            using var command = CreateCommand(connection, sql, parameters);

            var wasOpen = connection.State == ConnectionState.Open;
            if (!wasOpen) connection.Open();

            try
            {
                return command.ExecuteNonQuery();
            }
            finally
            {
                if (!wasOpen) connection.Close();
            }
        }

        /// <summary>Executes a query asynchronously and returns a list of results.</summary>
        public static async Task<List<T>> ExecuteReaderListAsync<T>(
            DbConnection connection,
            string sql,
            IEnumerable<KeyValuePair<string, object?>>? parameters,
            Func<IDataReader, T> mapper,
            CancellationToken cancellationToken = default)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            if (sql == null) throw new ArgumentNullException(nameof(sql));
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));

            var results = new List<T>();

            using var command = CreateCommand(connection, sql, parameters);

            var wasOpen = connection.State == ConnectionState.Open;
            if (!wasOpen) await OpenAsync(connection, cancellationToken);

            try
            {
                using var reader = await ExecuteReaderCoreAsync(command, cancellationToken);
                while (await ReadAsync(reader, cancellationToken))
                {
                    results.Add(mapper(reader));
                }
            }
            finally
            {
                if (!wasOpen) connection.Close();
            }

            return results;
        }

#if NET8_0_OR_GREATER
        /// <summary>Executes a query asynchronously and yields results.</summary>
        public static async IAsyncEnumerator<T> ExecuteReaderAsync<T>(
            DbConnection connection,
            string sql,
            IEnumerable<KeyValuePair<string, object?>>? parameters,
            Func<IDataReader, T> mapper,
            CancellationToken cancellationToken = default)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            if (sql == null) throw new ArgumentNullException(nameof(sql));
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));

            using var command = CreateCommand(connection, sql, parameters);

            var wasOpen = connection.State == ConnectionState.Open;
            if (!wasOpen) await connection.OpenAsync(cancellationToken);

            DbDataReader? reader = null;
            try
            {
                reader = await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    yield return mapper(reader);
                }
            }
            finally
            {
                if (reader != null) await reader.DisposeAsync();
                if (!wasOpen) connection.Close();
            }
        }
#endif

        /// <summary>Executes a query asynchronously and returns a scalar value.</summary>
        public static async Task<T?> ExecuteScalarAsync<T>(
            DbConnection connection,
            string sql,
            IEnumerable<KeyValuePair<string, object?>>? parameters,
            CancellationToken cancellationToken = default)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            if (sql == null) throw new ArgumentNullException(nameof(sql));

            using var command = CreateCommand(connection, sql, parameters);

            var wasOpen = connection.State == ConnectionState.Open;
            if (!wasOpen) await OpenAsync(connection, cancellationToken);

            try
            {
                var result = await ExecuteScalarCoreAsync(command, cancellationToken);
                if (result == null || result == DBNull.Value)
                    return default;

                return (T)result;
            }
            finally
            {
                if (!wasOpen) connection.Close();
            }
        }

        /// <summary>Executes a non-query command asynchronously and returns affected rows.</summary>
        public static async Task<int> ExecuteNonQueryAsync(
            DbConnection connection,
            string sql,
            IEnumerable<KeyValuePair<string, object?>>? parameters,
            CancellationToken cancellationToken = default)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            if (sql == null) throw new ArgumentNullException(nameof(sql));

            using var command = CreateCommand(connection, sql, parameters);

            var wasOpen = connection.State == ConnectionState.Open;
            if (!wasOpen) await OpenAsync(connection, cancellationToken);

            try
            {
                return await ExecuteNonQueryCoreAsync(command, cancellationToken);
            }
            finally
            {
                if (!wasOpen) connection.Close();
            }
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
                foreach (var param in parameters)
                {
                    var dbParam = command.CreateParameter();
                    dbParam.ParameterName = param.Key;
                    dbParam.Value = param.Value ?? DBNull.Value;
                    command.Parameters.Add(dbParam);
                }
            }

            return command;
        }

        private static async Task OpenAsync(DbConnection connection, CancellationToken cancellationToken)
        {
#if NETSTANDARD2_0
            await Task.Run(() => connection.Open(), cancellationToken);
#else
            await connection.OpenAsync(cancellationToken);
#endif
        }

        private static async Task<DbDataReader> ExecuteReaderCoreAsync(DbCommand command, CancellationToken cancellationToken)
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

        private static async Task<object?> ExecuteScalarCoreAsync(DbCommand command, CancellationToken cancellationToken)
        {
#if NETSTANDARD2_0
            return await Task.Run(() => command.ExecuteScalar(), cancellationToken);
#else
            return await command.ExecuteScalarAsync(cancellationToken);
#endif
        }

        private static async Task<int> ExecuteNonQueryCoreAsync(DbCommand command, CancellationToken cancellationToken)
        {
#if NETSTANDARD2_0
            return await Task.Run(() => command.ExecuteNonQuery(), cancellationToken);
#else
            return await command.ExecuteNonQueryAsync(cancellationToken);
#endif
        }
    }
}
