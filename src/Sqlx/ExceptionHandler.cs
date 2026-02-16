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
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Sqlx
{
    /// <summary>
    /// Internal handler for exception enrichment, logging, and retry logic.
    /// </summary>
    internal static class ExceptionHandler
    {
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

                    var sqlxEx = EnrichException(
                        ex,
                        methodName,
                        sql,
                        parameters,
                        stopwatch.Elapsed,
                        transaction);

                    // Log if logger available
                    if (options.Logger != null)
                    {
                        LogException(options.Logger, sqlxEx, attemptCount);
                    }

                    // Invoke callback if configured
                    if (options.OnException != null)
                    {
                        try
                        {
                            await options.OnException(sqlxEx);
                        }
                        catch (Exception)
                        {
                            // Log callback failure but don't let it prevent exception propagation
                            // Callback exceptions are intentionally swallowed to ensure original exception is thrown
                        }
                    }

                    // Retry logic
                    if (options.EnableRetry &&
                        attemptCount < options.MaxRetryCount &&
                        IsTransientError(ex))
                    {
                        var delay = CalculateRetryDelay(
                            attemptCount,
                            options.InitialRetryDelay,
                            options.RetryBackoffMultiplier);

                        options.Logger?.LogWarning(
                            "Retrying operation {MethodName} after {Delay}ms (attempt {Attempt}/{MaxAttempts})",
                            methodName, delay.TotalMilliseconds, attemptCount, options.MaxRetryCount);

                        await Task.Delay(delay);
                        stopwatch.Restart();
                        continue;
                    }

                    throw sqlxEx;
                }
            }
        }

        /// <summary>
        /// Enriches an exception with SQL context information.
        /// </summary>
        private static SqlxException EnrichException(
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

            var sanitized = new Dictionary<string, object?>();
            var sensitiveNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "password", "pwd", "secret", "token", "apikey", "api_key"
            };

            foreach (var (key, value) in parameters)
            {
                sanitized[key] = sensitiveNames.Contains(key) ? "***REDACTED***" : value;
            }

            return sanitized;
        }

        /// <summary>
        /// Determines if an exception represents a transient error that can be retried.
        /// </summary>
        private static bool IsTransientError(Exception ex)
        {
            return ex is TimeoutException ||
                   (ex is DbException dbEx && IsTransientDbError(dbEx));
        }

        /// <summary>
        /// Determines if a database exception represents a transient error.
        /// </summary>
        private static bool IsTransientDbError(DbException ex)
        {
            // Common transient error codes
            // SQL Server: -2 (timeout), 1205 (deadlock), 40197/40501/40613 (Azure transient)
            // PostgreSQL: 40001 (serialization failure), 40P01 (deadlock)
            // MySQL: 1205 (lock wait timeout), 1213 (deadlock)
            return ex.ErrorCode switch
            {
                -2 or 1205 or 1213 or 40001 or 40197 or 40501 or 40613 => true,
                _ => false
            };
        }

        /// <summary>
        /// Calculates the retry delay using exponential backoff.
        /// </summary>
        private static TimeSpan CalculateRetryDelay(
            int attemptNumber,
            TimeSpan initialDelay,
            double multiplier)
        {
            var delayMs = initialDelay.TotalMilliseconds * Math.Pow(multiplier, attemptNumber - 1);
            return TimeSpan.FromMilliseconds(delayMs);
        }

        /// <summary>
        /// Logs an exception with structured data.
        /// </summary>
        private static void LogException(
            ILogger logger,
            SqlxException ex,
            int attemptNumber)
        {
            logger.LogError(
                ex,
                "SQL operation failed: {MethodName} | SQL: {Sql} | Duration: {Duration}ms | Attempt: {Attempt} | CorrelationId: {CorrelationId}",
                ex.MethodName,
                ex.Sql,
                ex.Duration?.TotalMilliseconds,
                attemptNumber,
                ex.CorrelationId);
        }
    }
}
