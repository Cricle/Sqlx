// -----------------------------------------------------------------------
// <copyright file="SqlxException.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#nullable enable

using System;
using System.Collections.Generic;
using System.Data;

namespace Sqlx
{
    /// <summary>
    /// Base exception for Sqlx operations with SQL context information.
    /// </summary>
    /// <remarks>
    /// SqlxException provides rich contextual information about database operation failures,
    /// including SQL statements, parameters, execution timing, and correlation data for debugging.
    /// All properties are immutable and set during exception creation.
    /// </remarks>
    public class SqlxException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SqlxException"/> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or null if no inner exception is specified.</param>
        public SqlxException(string message, Exception? innerException = null)
            : base(message, innerException)
        {
            Timestamp = DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// Gets the SQL statement that was being executed when the exception occurred.
        /// </summary>
        /// <value>
        /// The SQL statement, or null if not available.
        /// </value>
        public string? Sql { get; init; }

        /// <summary>
        /// Gets the parameters used in the SQL statement.
        /// </summary>
        /// <value>
        /// A read-only dictionary of parameter names and values, or null if no parameters were used.
        /// Sensitive parameter values (passwords, tokens, etc.) are automatically sanitized.
        /// </value>
        public IReadOnlyDictionary<string, object?>? Parameters { get; init; }

        /// <summary>
        /// Gets the repository method name that failed.
        /// </summary>
        /// <value>
        /// The method name, or null if not available.
        /// </value>
        public string? MethodName { get; init; }

        /// <summary>
        /// Gets how long the operation took before failing.
        /// </summary>
        /// <value>
        /// The execution duration, or null if timing information is not available.
        /// </value>
        public TimeSpan? Duration { get; init; }

        /// <summary>
        /// Gets the correlation ID for distributed tracing.
        /// </summary>
        /// <value>
        /// The correlation ID from Activity.Current?.Id, or null if no activity context is available.
        /// </value>
        public string? CorrelationId { get; init; }

        /// <summary>
        /// Gets when the exception occurred.
        /// </summary>
        /// <value>
        /// The UTC timestamp when the exception was created.
        /// </value>
        public DateTimeOffset Timestamp { get; init; }

        /// <summary>
        /// Gets the transaction isolation level if a transaction was active.
        /// </summary>
        /// <value>
        /// The isolation level of the active transaction, or null if no transaction was active.
        /// </value>
        public IsolationLevel? TransactionIsolationLevel { get; init; }
    }
}
