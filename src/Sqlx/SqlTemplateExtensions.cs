// -----------------------------------------------------------------------
// <copyright file="SqlTemplateExtensions.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Sqlx
{
    /// <summary>
    /// High-performance ADO.NET integration extensions for SqlTemplate.
    /// </summary>
    public static class SqlTemplateExtensions
    {
        /// <summary>
        /// Creates a DbCommand from this SqlTemplate.
        /// </summary>
        public static DbCommand CreateCommand(
            this SqlTemplate template,
            DbConnection connection,
            DbTransaction? transaction = null,
            int? commandTimeout = null,
            IReadOnlyDictionary<string, object?>? parameterOverrides = null)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            var command = connection.CreateCommand();
            command.CommandText = template.Sql;

            if (transaction != null)
            {
                command.Transaction = transaction;
            }

            if (commandTimeout.HasValue)
            {
                command.CommandTimeout = commandTimeout.Value;
            }

            foreach (var param in template.Parameters)
            {
                var dbParam = command.CreateParameter();
                dbParam.ParameterName = param.Key;
                dbParam.Value = GetParameterValue(param.Key, param.Value, parameterOverrides);
                command.Parameters.Add(dbParam);
            }

            return command;
        }

        /// <summary>
        /// Creates a DbCommand asynchronously, opening the connection if needed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<DbCommand> CreateCommandAsync(
            this SqlTemplate template,
            DbConnection connection,
            DbTransaction? transaction = null,
            int? commandTimeout = null,
            IReadOnlyDictionary<string, object?>? parameterOverrides = null,
            CancellationToken cancellationToken = default)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            }

            return template.CreateCommand(connection, transaction, commandTimeout, parameterOverrides);
        }

        #region Async Methods

        /// <summary>
        /// Executes the template and returns a data reader.
        /// </summary>
        public static async Task<DbDataReader> ExecuteReaderAsync(
            this SqlTemplate template,
            DbConnection connection,
            DbTransaction? transaction = null,
            IReadOnlyDictionary<string, object?>? parameterOverrides = null,
            CommandBehavior behavior = CommandBehavior.Default,
            CancellationToken cancellationToken = default)
        {
            var command = await template.CreateCommandAsync(connection, transaction, null, parameterOverrides, cancellationToken).ConfigureAwait(false);

            try
            {
                return await command.ExecuteReaderAsync(behavior, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                command.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Executes the template and returns the number of affected rows.
        /// </summary>
        public static async Task<int> ExecuteNonQueryAsync(
            this SqlTemplate template,
            DbConnection connection,
            DbTransaction? transaction = null,
            IReadOnlyDictionary<string, object?>? parameterOverrides = null,
            CancellationToken cancellationToken = default)
        {
            using var command = await template.CreateCommandAsync(connection, transaction, null, parameterOverrides, cancellationToken).ConfigureAwait(false);
            return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the template and returns a scalar value.
        /// </summary>
        public static async Task<object?> ExecuteScalarAsync(
            this SqlTemplate template,
            DbConnection connection,
            DbTransaction? transaction = null,
            IReadOnlyDictionary<string, object?>? parameterOverrides = null,
            CancellationToken cancellationToken = default)
        {
            using var command = await template.CreateCommandAsync(connection, transaction, null, parameterOverrides, cancellationToken).ConfigureAwait(false);
            return NormalizeDbNull(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false));
        }

        /// <summary>
        /// Executes the template and returns a scalar value with type conversion.
        /// </summary>
        public static async Task<T?> ExecuteScalarAsync<T>(
            this SqlTemplate template,
            DbConnection connection,
            DbTransaction? transaction = null,
            IReadOnlyDictionary<string, object?>? parameterOverrides = null,
            CancellationToken cancellationToken = default)
        {
            using var command = await template.CreateCommandAsync(connection, transaction, null, parameterOverrides, cancellationToken).ConfigureAwait(false);
            return ConvertScalar<T>(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false));
        }

        #endregion

        #region Sync Methods

        /// <summary>
        /// Executes the template and returns a data reader.
        /// </summary>
        public static DbDataReader ExecuteReader(
            this SqlTemplate template,
            DbConnection connection,
            DbTransaction? transaction = null,
            IReadOnlyDictionary<string, object?>? parameterOverrides = null,
            CommandBehavior behavior = CommandBehavior.Default)
        {
            using var command = template.CreateCommand(connection, transaction, parameterOverrides: parameterOverrides);
            return command.ExecuteReader(behavior);
        }

        /// <summary>
        /// Executes the template and returns the number of affected rows.
        /// </summary>
        public static int ExecuteNonQuery(
            this SqlTemplate template,
            DbConnection connection,
            DbTransaction? transaction = null,
            IReadOnlyDictionary<string, object?>? parameterOverrides = null)
        {
            using var command = template.CreateCommand(connection, transaction, parameterOverrides: parameterOverrides);
            return command.ExecuteNonQuery();
        }

        /// <summary>
        /// Executes the template and returns a scalar value.
        /// </summary>
        public static object? ExecuteScalar(
            this SqlTemplate template,
            DbConnection connection,
            DbTransaction? transaction = null,
            IReadOnlyDictionary<string, object?>? parameterOverrides = null)
        {
            using var command = template.CreateCommand(connection, transaction, parameterOverrides: parameterOverrides);
            return NormalizeDbNull(command.ExecuteScalar());
        }

        /// <summary>
        /// Executes the template and returns a scalar value with type conversion.
        /// </summary>
        public static T? ExecuteScalar<T>(
            this SqlTemplate template,
            DbConnection connection,
            DbTransaction? transaction = null,
            IReadOnlyDictionary<string, object?>? parameterOverrides = null)
        {
            using var command = template.CreateCommand(connection, transaction, parameterOverrides: parameterOverrides);
            return ConvertScalar<T>(command.ExecuteScalar());
        }

        #endregion

        #region Private Helpers

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static object GetParameterValue(string key, object? value, IReadOnlyDictionary<string, object?>? overrides)
        {
            if (overrides != null && overrides.TryGetValue(key, out var overrideValue))
            {
                return overrideValue ?? DBNull.Value;
            }

            return value ?? DBNull.Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static object? NormalizeDbNull(object? result) => result == DBNull.Value ? null : result;

        private static T? ConvertScalar<T>(object? result)
        {
            if (result == null || result == DBNull.Value)
            {
                return default;
            }

            if (result is T typed)
            {
                return typed;
            }

            var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
            return (T?)Convert.ChangeType(result, targetType);
        }

        #endregion
    }
}
