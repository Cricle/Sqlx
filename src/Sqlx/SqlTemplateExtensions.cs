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
    /// Thread-safe, low-allocation, and debugger-friendly.
    /// </summary>
    public static class SqlTemplateExtensions
    {
        /// <summary>
        /// Creates a DbCommand from this SqlTemplate with optional parameter overrides.
        /// </summary>
        /// <param name="template">The SQL template</param>
        /// <param name="connection">The database connection (must not be null)</param>
        /// <param name="transaction">Optional transaction</param>
        /// <param name="commandTimeout">Optional command timeout in seconds</param>
        /// <param name="parameterOverrides">Optional parameter value overrides</param>
        /// <returns>A configured DbCommand ready to execute</returns>
        /// <exception cref="ArgumentNullException">Thrown when connection is null</exception>
        public static DbCommand CreateCommand(
            this SqlTemplate template,
            DbConnection connection,
            DbTransaction? transaction = null,
            int? commandTimeout = null,
            IReadOnlyDictionary<string, object?>? parameterOverrides = null)
        {
            // Parameter validation (debugger-friendly)
            if (connection == null)
                throw new ArgumentNullException(nameof(connection), "Database connection cannot be null");

            // Create Command
            var command = connection.CreateCommand();
            command.CommandText = template.Sql;
            
            if (transaction != null)
                command.Transaction = transaction;
            
            if (commandTimeout.HasValue)
                command.CommandTimeout = commandTimeout.Value;

            // Add parameters (performance optimized)
            var paramCount = template.Parameters.Count;
            if (paramCount > 0)
            {
                foreach (var param in template.Parameters)
                {
                    var dbParam = command.CreateParameter();
                    dbParam.ParameterName = param.Key;
                    
                    // Performance optimization: use TryGetValue to avoid two dictionary lookups
                    object? value;
                    if (parameterOverrides != null && parameterOverrides.TryGetValue(param.Key, out var overrideValue))
                    {
                        value = overrideValue;
                    }
                    else
                    {
                        value = param.Value;
                    }
                    
                    dbParam.Value = value ?? DBNull.Value;
                    command.Parameters.Add(dbParam);
                }
            }

            return command;
        }

        /// <summary>
        /// Creates a DbCommand asynchronously, opening the connection if needed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async ValueTask<DbCommand> CreateCommandAsync(
            this SqlTemplate template,
            DbConnection connection,
            DbTransaction? transaction = null,
            int? commandTimeout = null,
            IReadOnlyDictionary<string, object?>? parameterOverrides = null,
            CancellationToken cancellationToken = default)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection), "Database connection cannot be null");

            // Only open connection if needed
            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            }

            return template.CreateCommand(connection, transaction, commandTimeout, parameterOverrides);
        }

        /// <summary>
        /// Executes the template and returns a data reader (async).
        /// </summary>
        public static async ValueTask<DbDataReader> ExecuteReaderAsync(
            this SqlTemplate template,
            DbConnection connection,
            DbTransaction? transaction = null,
            IReadOnlyDictionary<string, object?>? parameterOverrides = null,
            CommandBehavior behavior = CommandBehavior.Default,
            CancellationToken cancellationToken = default)
        {
            var command = await template.CreateCommandAsync(
                connection, transaction, commandTimeout: null, parameterOverrides, 
                cancellationToken).ConfigureAwait(false);
            
            try
            {
                return await command.ExecuteReaderAsync(behavior, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                command?.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Executes the template and returns the number of affected rows (async).
        /// </summary>
        public static async ValueTask<int> ExecuteNonQueryAsync(
            this SqlTemplate template,
            DbConnection connection,
            DbTransaction? transaction = null,
            IReadOnlyDictionary<string, object?>? parameterOverrides = null,
            CancellationToken cancellationToken = default)
        {
            var command = await template.CreateCommandAsync(
                connection, transaction, commandTimeout: null, parameterOverrides,
                cancellationToken).ConfigureAwait(false);
            
            try
            {
                return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                command?.Dispose();
            }
        }

        /// <summary>
        /// Executes the template and returns a scalar value (async, non-generic).
        /// </summary>
        public static async ValueTask<object?> ExecuteScalarAsync(
            this SqlTemplate template,
            DbConnection connection,
            DbTransaction? transaction = null,
            IReadOnlyDictionary<string, object?>? parameterOverrides = null,
            CancellationToken cancellationToken = default)
        {
            var command = await template.CreateCommandAsync(
                connection, transaction, commandTimeout: null, parameterOverrides,
                cancellationToken).ConfigureAwait(false);
            
            try
            {
                var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                return result == DBNull.Value ? null : result;
            }
            finally
            {
                command?.Dispose();
            }
        }

        /// <summary>
        /// Executes the template and returns a scalar value with type conversion (async).
        /// Supports nullable value types, reference types, and automatic conversions.
        /// </summary>
        public static async ValueTask<T?> ExecuteScalarAsync<T>(
            this SqlTemplate template,
            DbConnection connection,
            DbTransaction? transaction = null,
            IReadOnlyDictionary<string, object?>? parameterOverrides = null,
            CancellationToken cancellationToken = default)
        {
            var command = await template.CreateCommandAsync(
                connection, transaction, commandTimeout: null, parameterOverrides,
                cancellationToken).ConfigureAwait(false);
            
            try
            {
                var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                
                if (result == null || result == DBNull.Value)
                    return default;
                
                // Performance optimization: avoid unnecessary type conversion
                if (result is T typedResult)
                    return typedResult;
                
                // Handle Nullable<T>
                var targetType = typeof(T);
                var underlyingType = Nullable.GetUnderlyingType(targetType);
                if (underlyingType != null)
                {
                    // T is Nullable<TValue>, convert to TValue
                    return (T?)Convert.ChangeType(result, underlyingType);
                }
                
                return (T)Convert.ChangeType(result, targetType);
            }
            finally
            {
                command?.Dispose();
            }
        }

        // ========== Synchronous versions ==========
        
        /// <summary>
        /// Executes the template and returns a data reader (sync).
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
        /// Executes the template and returns the number of affected rows (sync).
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
        /// Executes the template and returns a scalar value (sync, non-generic).
        /// </summary>
        public static object? ExecuteScalar(
            this SqlTemplate template,
            DbConnection connection,
            DbTransaction? transaction = null,
            IReadOnlyDictionary<string, object?>? parameterOverrides = null)
        {
            using var command = template.CreateCommand(connection, transaction, parameterOverrides: parameterOverrides);
            var result = command.ExecuteScalar();
            return result == DBNull.Value ? null : result;
        }

        /// <summary>
        /// Executes the template and returns a scalar value with type conversion (sync).
        /// Supports nullable value types, reference types, and automatic conversions.
        /// </summary>
        public static T? ExecuteScalar<T>(
            this SqlTemplate template,
            DbConnection connection,
            DbTransaction? transaction = null,
            IReadOnlyDictionary<string, object?>? parameterOverrides = null)
        {
            using var command = template.CreateCommand(connection, transaction, parameterOverrides: parameterOverrides);
            var result = command.ExecuteScalar();
            
            if (result == null || result == DBNull.Value)
                return default;
            
            if (result is T typedResult)
                return typedResult;
            
            // Handle Nullable<T>
            var targetType = typeof(T);
            var underlyingType = Nullable.GetUnderlyingType(targetType);
            if (underlyingType != null)
            {
                return (T?)Convert.ChangeType(result, underlyingType);
            }
            
            return (T)Convert.ChangeType(result, targetType);
        }
    }
}
