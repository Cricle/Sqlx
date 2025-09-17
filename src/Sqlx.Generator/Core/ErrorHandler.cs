// -----------------------------------------------------------------------
// <copyright file="ErrorHandler.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using System;

namespace Sqlx.Generator.Core;

/// <summary>
/// Simplified error handling for source generation.
/// </summary>
public static class ErrorHandler
{
    /// <summary>
    /// Reports a diagnostic for an exception.
    /// </summary>
    /// <param name="context">The generator execution context.</param>
    /// <param name="ex">The exception.</param>
    /// <param name="errorCode">The error code.</param>
    /// <param name="title">The error title.</param>
    /// <param name="messageFormat">The error message format.</param>
    /// <param name="args">Additional arguments for the message.</param>
    public static void ReportError(
        GeneratorExecutionContext context,
        Exception ex,
        string errorCode = "SQLX9999",
        string title = "Code generation error",
        string messageFormat = "An error occurred during code generation: {0}",
        params object[] args)
    {
        var allArgs = new object[args.Length + 1];
        allArgs[0] = ex.Message;
        Array.Copy(args, 0, allArgs, 1, args.Length);

        var diagnostic = Diagnostic.Create(
            new DiagnosticDescriptor(
                errorCode,
                title,
                messageFormat,
                "CodeGeneration",
                DiagnosticSeverity.Error,
                true),
            null,
            allArgs);

        context.ReportDiagnostic(diagnostic);
    }

    /// <summary>
    /// Executes an action safely, reporting any errors.
    /// </summary>
    /// <param name="context">The generator execution context.</param>
    /// <param name="action">The action to execute.</param>
    /// <param name="errorCode">The error code for any exceptions.</param>
    /// <param name="description">Description of what operation failed.</param>
    public static void ExecuteSafely(
        GeneratorExecutionContext context,
        Action action,
        string errorCode = "SQLX9999",
        string description = "operation")
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            ReportError(context, ex, errorCode, $"Error during {description}", $"Error during {description}: {{0}}");
        }
    }

    /// <summary>
    /// Executes a function safely, returning a default value on error.
    /// </summary>
    /// <typeparam name="T">The return type.</typeparam>
    /// <param name="context">The generator execution context.</param>
    /// <param name="func">The function to execute.</param>
    /// <param name="defaultValue">The default value to return on error.</param>
    /// <param name="errorCode">The error code for any exceptions.</param>
    /// <param name="description">Description of what operation failed.</param>
    /// <returns>The function result or default value.</returns>
    public static T ExecuteSafely<T>(
        GeneratorExecutionContext context,
        Func<T> func,
        T defaultValue,
        string errorCode = "SQLX9999",
        string description = "operation")
    {
        try
        {
            return func();
        }
        catch (Exception ex)
        {
            ReportError(context, ex, errorCode, $"Error during {description}", $"Error during {description}: {{0}}");
            return defaultValue;
        }
    }
}
