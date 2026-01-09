#if NET7_0_OR_GREATER
// -----------------------------------------------------------------------
// <copyright file="IStaticCommandInterceptor.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Data.Common;

namespace Sqlx
{
    /// <summary>
    /// Defines a static interceptor for database command execution.
    /// </summary>
    /// <remarks>
    /// <para>This interface is for static interceptors that don't maintain state.</para>
    /// <para>Classes implementing this interface should provide static methods.</para>
    /// <para>Use with [InterceptBy(typeof(YourStaticInterceptor))] attribute on repository classes.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public static class StaticLoggingInterceptor : IStaticCommandInterceptor
    /// {
    ///     public static void OnExecuting(string operationName, DbCommand command)
    ///     {
    ///         Console.WriteLine($"Executing: {operationName}");
    ///     }
    ///
    ///     public static void OnExecuted(string operationName, DbCommand command)
    ///     {
    ///         Console.WriteLine($"Completed: {operationName}");
    ///     }
    ///
    ///     public static void OnError(string operationName, DbCommand command, Exception exception)
    ///     {
    ///         Console.WriteLine($"Error: {exception.Message}");
    ///     }
    /// }
    ///
    /// [InterceptBy(typeof(StaticLoggingInterceptor))]
    /// public partial class UserRepository : ICrudRepository&lt;User, int&gt; { }
    /// </code>
    /// </example>
    public interface IStaticCommandInterceptor
    {
        /// <summary>
        /// Called before executing a database command.
        /// </summary>
        /// <param name="operationName">The name of the operation being executed.</param>
        /// <param name="command">The database command about to be executed.</param>
        static abstract void OnExecuting(string operationName, DbCommand command);

        /// <summary>
        /// Called after successfully executing a database command.
        /// </summary>
        /// <param name="operationName">The name of the operation that was executed.</param>
        /// <param name="command">The database command that was executed.</param>
        static abstract void OnExecuted(string operationName, DbCommand command);

        /// <summary>
        /// Called when an error occurs during command execution.
        /// </summary>
        /// <param name="operationName">The name of the operation that failed.</param>
        /// <param name="command">The database command that failed.</param>
        /// <param name="exception">The exception that was thrown.</param>
        static abstract void OnError(string operationName, DbCommand command, Exception exception);
    }
}
#endif
