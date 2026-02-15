// -----------------------------------------------------------------------
// <copyright file="SqlxContextOptions.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#nullable enable

using System;
using System.Threading.Tasks;

#if NET6_0_OR_GREATER && !NETSTANDARD
using Microsoft.Extensions.Logging;
#endif

namespace Sqlx
{
    /// <summary>
    /// Options for configuring SqlxContext behavior including exception handling, retry logic, and logging.
    /// </summary>
    /// <remarks>
    /// SqlxContextOptions allows you to configure global exception handling, automatic retry for transient failures,
    /// and logging integration for all repositories within a SqlxContext.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Basic usage with logging
    /// var options = new SqlxContextOptions
    /// {
    ///     Logger = loggerFactory.CreateLogger&lt;AppDbContext&gt;()
    /// };
    /// 
    /// // With retry enabled
    /// var options = new SqlxContextOptions
    /// {
    ///     EnableRetry = true,
    ///     MaxRetryCount = 3,
    ///     InitialRetryDelay = TimeSpan.FromMilliseconds(100),
    ///     Logger = logger
    /// };
    /// 
    /// // With custom exception handler
    /// var options = new SqlxContextOptions
    /// {
    ///     OnException = async (ex) =>
    ///     {
    ///         await telemetry.RecordException(ex);
    ///         await notificationService.NotifyAdmins(ex);
    ///     },
    ///     Logger = logger
    /// };
    /// </code>
    /// </example>
    public class SqlxContextOptions
    {
        /// <summary>
        /// Gets or sets the callback invoked when an exception occurs in any repository operation.
        /// </summary>
        /// <value>
        /// An async function that receives the SqlxException and can perform custom handling such as logging,
        /// telemetry recording, or notifications. The callback is invoked before the exception is thrown.
        /// Default is null (no callback).
        /// </value>
        /// <remarks>
        /// The callback is invoked on the failure path and should complete quickly to avoid delaying exception propagation.
        /// If the callback throws an exception, it will be logged (if a logger is available) but will not prevent
        /// the original exception from being thrown.
        /// </remarks>
        public Func<SqlxException, Task>? OnException { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to enable automatic retry for transient failures.
        /// </summary>
        /// <value>
        /// true to enable automatic retry; false to fail immediately on any error. Default is false.
        /// </value>
        /// <remarks>
        /// When enabled, the system will automatically retry operations that fail with transient errors
        /// such as connection timeouts, deadlocks, or temporary Azure SQL Database errors.
        /// Non-transient errors will not be retried.
        /// </remarks>
        public bool EnableRetry { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of retry attempts for transient failures.
        /// </summary>
        /// <value>
        /// The maximum number of retry attempts. Default is 3. Must be greater than 0.
        /// </value>
        /// <remarks>
        /// This value is only used when <see cref="EnableRetry"/> is true.
        /// The total number of execution attempts will be MaxRetryCount + 1 (initial attempt plus retries).
        /// </remarks>
        public int MaxRetryCount { get; set; } = 3;

        /// <summary>
        /// Gets or sets the initial delay for retry backoff.
        /// </summary>
        /// <value>
        /// The initial delay before the first retry attempt. Default is 100 milliseconds.
        /// </value>
        /// <remarks>
        /// This value is only used when <see cref="EnableRetry"/> is true.
        /// Subsequent retry delays are calculated using exponential backoff based on this initial delay
        /// and the <see cref="RetryBackoffMultiplier"/>.
        /// </remarks>
        public TimeSpan InitialRetryDelay { get; set; } = TimeSpan.FromMilliseconds(100);

        /// <summary>
        /// Gets or sets the multiplier for exponential backoff retry delays.
        /// </summary>
        /// <value>
        /// The multiplier applied to calculate each subsequent retry delay. Default is 2.0.
        /// </value>
        /// <remarks>
        /// This value is only used when <see cref="EnableRetry"/> is true.
        /// The delay for retry attempt N is calculated as: InitialRetryDelay * (RetryBackoffMultiplier ^ (N-1)).
        /// For example, with InitialRetryDelay=100ms and RetryBackoffMultiplier=2.0:
        /// - Retry 1: 100ms
        /// - Retry 2: 200ms
        /// - Retry 3: 400ms
        /// </remarks>
        public double RetryBackoffMultiplier { get; set; } = 2.0;

#if NET6_0_OR_GREATER && !NETSTANDARD
        /// <summary>
        /// Gets or sets the ILogger instance for automatic exception logging.
        /// </summary>
        /// <value>
        /// An ILogger instance that will be used to log exceptions and retry attempts.
        /// Default is null (no logging).
        /// </value>
        /// <remarks>
        /// When a logger is provided, exceptions will be automatically logged with structured data including
        /// SQL statements, parameters (sanitized), method names, execution duration, and correlation IDs.
        /// Retry attempts will be logged at Warning level, while final failures are logged at Error level.
        /// This property is only available when targeting .NET 6.0 or later (not .NET Standard).
        /// </remarks>
        public ILogger? Logger { get; set; }
#endif
    }
}
