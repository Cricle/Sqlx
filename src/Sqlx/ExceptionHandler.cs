// -----------------------------------------------------------------------
// <copyright file="ExceptionHandler.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#nullable enable

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Sqlx
{
    /// <summary>
    /// Internal handler for exception enrichment, logging, and retry logic.
    /// </summary>
    public static class ExceptionHandler
    {
        private static readonly HashSet<string> SensitiveParameterNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "password", "pwd", "secret", "token", "apikey", "api_key"
        };

        /// <summary>
        /// Executes an operation with exception handling, enrichment, logging, and retry support.
        /// </summary>
        /// <typeparam name="T">The return type of the operation.</typeparam>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="options">The context options for exception handling configuration.</param>
        /// <param name="methodName">The name of the repository method being executed.</param>
        /// <param name="sql">The SQL statement being executed.</param>
        /// <param name="parameters">The parameters for the SQL statement.</param>
        /// <param name="transaction">The active transaction, if any.</param>
        /// <returns>The result of the operation.</returns>
        /// <exception cref="SqlxException">Thrown when the operation fails.</exception>
        public static async Task<T> ExecuteWithHandlingAsync<T>(
            Func<Task<T>> operation,
            SqlxContextOptions options,
            string methodName,
            string sql,
            IReadOnlyDictionary<string, object?>? parameters,
            DbTransaction? transaction)
        {
            var stopwatch = Stopwatch.StartNew();
            var attemptCount = 0;

            while (true)
            {
                try
                {
                    attemptCount++;
                    return await operation();
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();

                    var sqlxEx = await HandleFailureAndRetryAsync(
                        ex,
                        options,
                        methodName,
                        sql,
                        parameters,
                        stopwatch.Elapsed,
                        transaction,
                        attemptCount).ConfigureAwait(false);

                    if (sqlxEx is null)
                    {
                        stopwatch.Restart();
                        continue;
                    }

                    throw sqlxEx;
                }
            }
        }

        /// <summary>
        /// Handles a repository async failure and returns null when the caller should retry.
        /// </summary>
        public static Task<SqlxException?> HandleFailureAndRetryAsync(
            Exception ex,
            SqlxContextOptions? options,
            string methodName,
            string sql,
            DbParameterCollection? parameters,
            DbTransaction? transaction,
            TimeSpan duration,
            int attemptCount)
            => HandleFailureAndRetryAsync(ex, options, methodName, sql, ExtractParameters(parameters), duration, transaction, attemptCount);

        private static async Task<SqlxException?> HandleFailureAndRetryAsync(
            Exception ex,
            SqlxContextOptions? options,
            string methodName,
            string sql,
            IReadOnlyDictionary<string, object?>? parameters,
            TimeSpan duration,
            DbTransaction? transaction,
            int attemptCount)
        {
            var sqlxEx = CreateSqlxException(ex, methodName, sql, parameters, duration, transaction);
            await NotifyFailureAsync(sqlxEx, options, attemptCount).ConfigureAwait(false);

            if (!ShouldRetry(options, ex, attemptCount, out var delay))
                return sqlxEx;

            options!.Logger?.LogWarning(
                "Retrying operation {MethodName} after {Delay}ms (attempt {Attempt}/{MaxAttempts})",
                methodName, delay.TotalMilliseconds, attemptCount, options.MaxRetryCount);

            await Task.Delay(delay).ConfigureAwait(false);
            return null;
        }

        /// <summary>
        /// Handles a repository sync failure and returns null when the caller should retry.
        /// </summary>
        public static SqlxException? HandleFailureAndRetry(
            Exception ex,
            SqlxContextOptions? options,
            string methodName,
            string sql,
            DbParameterCollection? parameters,
            DbTransaction? transaction,
            TimeSpan duration,
            int attemptCount)
        {
            var sqlxEx = CreateSqlxException(ex, methodName, sql, ExtractParameters(parameters), duration, transaction);
            NotifyFailure(sqlxEx, options, attemptCount);

            if (!ShouldRetry(options, ex, attemptCount, out var delay))
                return sqlxEx;

            options!.Logger?.LogWarning(
                "Retrying operation {MethodName} after {Delay}ms (attempt {Attempt}/{MaxAttempts})",
                methodName, delay.TotalMilliseconds, attemptCount, options.MaxRetryCount);

            Thread.Sleep(delay);
            return null;
        }

        /// <summary>
        /// Enriches and logs an exception captured by a generated repository async path.
        /// </summary>
        /// <param name="ex">The original exception.</param>
        /// <param name="options">The context options propagated to the repository.</param>
        /// <param name="methodName">The repository method name.</param>
        /// <param name="sql">The SQL statement that was executed.</param>
        /// <param name="parameters">The command parameters.</param>
        /// <param name="transaction">The active transaction, if any.</param>
        /// <param name="duration">The elapsed duration before failure.</param>
        /// <returns>The enriched <see cref="SqlxException"/> ready to be thrown.</returns>
        public static async Task<SqlxException> HandleExceptionAsync(
            Exception ex,
            SqlxContextOptions? options,
            string methodName,
            string sql,
            DbParameterCollection? parameters,
            DbTransaction? transaction,
            TimeSpan duration)
        {
            return await HandleFailureAndRetryAsync(
                ex,
                options,
                methodName,
                sql,
                parameters,
                transaction,
                duration,
                attemptCount: 1).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Retry handling returned no exception for a non-retry path.");
        }

        /// <summary>
        /// Enriches and logs an exception captured by a generated repository sync path.
        /// </summary>
        /// <param name="ex">The original exception.</param>
        /// <param name="options">The context options propagated to the repository.</param>
        /// <param name="methodName">The repository method name.</param>
        /// <param name="sql">The SQL statement that was executed.</param>
        /// <param name="parameters">The command parameters.</param>
        /// <param name="transaction">The active transaction, if any.</param>
        /// <param name="duration">The elapsed duration before failure.</param>
        /// <returns>The enriched <see cref="SqlxException"/> ready to be thrown.</returns>
        public static SqlxException HandleException(
            Exception ex,
            SqlxContextOptions? options,
            string methodName,
            string sql,
            DbParameterCollection? parameters,
            DbTransaction? transaction,
            TimeSpan duration)
        {
            return HandleFailureAndRetry(
                ex,
                options,
                methodName,
                sql,
                parameters,
                transaction,
                duration,
                attemptCount: 1)
                ?? throw new InvalidOperationException("Retry handling returned no exception for a non-retry path.");
        }

        /// <summary>
        /// Enriches an exception with SQL context information.
        /// </summary>
        private static SqlxException CreateSqlxException(
            Exception ex,
            string methodName,
            string sql,
            IReadOnlyDictionary<string, object?>? parameters,
            TimeSpan duration,
            DbTransaction? transaction)
        {
            return new SqlxException(
                $"SQL operation '{methodName}' failed: {ex.Message}",
                ex)
            {
                Sql = sql,
                Parameters = SanitizeParameters(parameters),
                MethodName = methodName,
                Duration = duration,
                CorrelationId = Activity.Current?.Id,
                TransactionIsolationLevel = transaction?.IsolationLevel
            };
        }

        /// <summary>
        /// Sanitizes parameters by redacting sensitive values.
        /// </summary>
        private static IReadOnlyDictionary<string, object?>? SanitizeParameters(
            IReadOnlyDictionary<string, object?>? parameters)
        {
            if (parameters == null) return null;

            var sanitized = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            foreach (var (key, value) in parameters)
            {
                sanitized[key] = SensitiveParameterNames.Contains(NormalizeParameterName(key)) ? "***REDACTED***" : value;
            }

            return sanitized;
        }

        private static IReadOnlyDictionary<string, object?>? ExtractParameters(DbParameterCollection? parameters)
        {
            if (parameters == null || parameters.Count == 0)
            {
                return null;
            }

            var values = new Dictionary<string, object?>(parameters.Count, StringComparer.OrdinalIgnoreCase);
            foreach (DbParameter parameter in parameters)
            {
                var parameterName = NormalizeParameterName(parameter.ParameterName);
                values[parameterName] = parameter.Value == DBNull.Value ? null : parameter.Value;
            }

            return values;
        }

        private static string NormalizeParameterName(string? parameterName)
        {
            if (string.IsNullOrEmpty(parameterName))
            {
                return string.Empty;
            }

            return parameterName[0] is '@' or ':' or '?'
                ? parameterName.Substring(1)
                : parameterName;
        }

        private static async Task NotifyFailureAsync(SqlxException sqlxEx, SqlxContextOptions? options, int attemptCount)
        {
            if (options == null)
            {
                return;
            }

            LogFailure(options.Logger, sqlxEx, attemptCount);

            if (options.OnException == null)
            {
                return;
            }

            try
            {
                await options.OnException(sqlxEx).ConfigureAwait(false);
            }
            catch (Exception callbackException)
            {
                options.Logger?.LogWarning(
                    callbackException,
                    "Sqlx OnException callback failed for {MethodName}",
                    sqlxEx.MethodName);
            }
        }

        private static void NotifyFailure(SqlxException sqlxEx, SqlxContextOptions? options, int attemptCount)
        {
            if (options == null)
            {
                return;
            }

            LogFailure(options.Logger, sqlxEx, attemptCount);

            if (options.OnException == null)
            {
                return;
            }

            try
            {
                options.OnException(sqlxEx).GetAwaiter().GetResult();
            }
            catch (Exception callbackException)
            {
                options.Logger?.LogWarning(
                    callbackException,
                    "Sqlx OnException callback failed for {MethodName}",
                    sqlxEx.MethodName);
            }
        }

        private static void LogFailure(ILogger? logger, SqlxException sqlxEx, int attemptCount)
        {
            logger?.LogError(
                sqlxEx,
                "SQL operation failed: {MethodName} | SQL: {Sql} | Duration: {Duration}ms | Attempt: {Attempt} | CorrelationId: {CorrelationId}",
                sqlxEx.MethodName,
                sqlxEx.Sql,
                sqlxEx.Duration?.TotalMilliseconds,
                attemptCount,
                sqlxEx.CorrelationId);
        }

        private static bool ShouldRetry(
            SqlxContextOptions? options,
            Exception ex,
            int attemptCount,
            out TimeSpan delay)
        {
            delay = default;

            if (options == null ||
                !options.EnableRetry ||
                attemptCount >= options.MaxRetryCount ||
                !IsTransientError(ex))
            {
                return false;
            }

            delay = CalculateRetryDelay(
                attemptCount,
                options.InitialRetryDelay,
                options.RetryBackoffMultiplier);

            return true;
        }

        /// <summary>
        /// Determines if an exception represents a transient error that can be retried.
        /// Common transient error codes:
        /// SQL Server: -2 (timeout), 1205 (deadlock), 40197/40501/40613 (Azure transient)
        /// PostgreSQL: 40001 (serialization failure), 40P01 (deadlock)
        /// MySQL: 1205 (lock wait timeout), 1213 (deadlock)
        /// </summary>
        private static bool IsTransientError(Exception ex) =>
            ex is TimeoutException or DbException { ErrorCode: -2 or 1205 or 1213 or 40001 or 40197 or 40501 or 40613 };

        /// <summary>
        /// Calculates the retry delay using exponential backoff.
        /// </summary>
        private static TimeSpan CalculateRetryDelay(int attemptNumber, TimeSpan initialDelay, double multiplier) =>
            TimeSpan.FromMilliseconds(initialDelay.TotalMilliseconds * Math.Pow(multiplier, attemptNumber - 1));
    }
}
