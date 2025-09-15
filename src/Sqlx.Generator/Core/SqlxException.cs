// -----------------------------------------------------------------------
// <copyright file="SqlxException.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace Sqlx;

/// <summary>
/// Base exception class for all Sqlx-related exceptions.
/// </summary>
public abstract class SqlxException : Exception
{
    /// <summary>
    /// Gets the error code associated with this exception.
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlxException"/> class.
    /// </summary>
    /// <param name="errorCode">The error code.</param>
    /// <param name="message">The error message.</param>
    protected SqlxException(string errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlxException"/> class.
    /// </summary>
    /// <param name="errorCode">The error code.</param>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    protected SqlxException(string errorCode, string message, Exception innerException) : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}

/// <summary>
/// Exception thrown when SQL generation fails.
/// </summary>
public sealed class SqlGenerationException : SqlxException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqlGenerationException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public SqlGenerationException(string message) : base("SQLX001", message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlGenerationException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public SqlGenerationException(string message, Exception innerException) : base("SQLX001", message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when method signature is invalid for SQL generation.
/// </summary>
public sealed class InvalidMethodSignatureException : SqlxException
{
    /// <summary>
    /// Gets the method name that caused the exception.
    /// </summary>
    public string MethodName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidMethodSignatureException"/> class.
    /// </summary>
    /// <param name="methodName">The method name.</param>
    /// <param name="message">The error message.</param>
    public InvalidMethodSignatureException(string methodName, string message) : base("SQLX002", message)
    {
        MethodName = methodName;
    }
}

/// <summary>
/// Exception thrown when database dialect is not supported.
/// </summary>
public sealed class UnsupportedDialectException : SqlxException
{
    /// <summary>
    /// Gets the dialect name that is not supported.
    /// </summary>
    public string DialectName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnsupportedDialectException"/> class.
    /// </summary>
    /// <param name="dialectName">The unsupported dialect name.</param>
    public UnsupportedDialectException(string dialectName)
        : base("SQLX003", $"Database dialect '{dialectName}' is not supported. Supported dialects: MySQL, SQL Server, PostgreSQL, SQLite")
    {
        DialectName = dialectName;
    }
}