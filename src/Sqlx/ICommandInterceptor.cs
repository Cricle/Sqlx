// -----------------------------------------------------------------------
// <copyright file="ICommandInterceptor.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#nullable enable

using System.Data.Common;

namespace Sqlx
{
    /// <summary>
    /// Defines an interceptor for database command execution.
    /// </summary>
    /// <remarks>
    /// <para>Implement this interface to create reusable command interceptors.</para>
    /// <para>Use with [InterceptBy(typeof(YourInterceptor))] attribute on repository classes.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public class LoggingInterceptor : ICommandInterceptor
    /// {
    ///     public void OnExecuting(string operationName, DbCommand command)
    ///     {
    ///         Console.WriteLine($"Executing: {operationName}");
    ///         Console.WriteLine($"SQL: {command.CommandText}");
    ///     }
    ///
    ///     public void OnExecuted(string operationName, DbCommand command)
    ///     {
    ///         Console.WriteLine($"Completed: {operationName}");
    ///     }
    ///
    ///     public void OnError(string operationName, DbCommand command, Exception exception)
    ///     {
    ///         Console.WriteLine($"Error in {operationName}: {exception.Message}");
    ///     }
    /// }
    ///
    /// [InterceptBy(typeof(LoggingInterceptor))]
    /// public partial class UserRepository : ICrudRepository&lt;User, int&gt; { }
    /// </code>
    /// </example>
    public interface ICommandInterceptor
    {
        /// <summary>
        /// Called before executing a database command.
        /// </summary>
        /// <param name="operationName">The name of the operation being executed.</param>
        /// <param name="command">The database command about to be executed.</param>
        void OnExecuting(string operationName, DbCommand command);

        /// <summary>
        /// Called after successfully executing a database command.
        /// </summary>
        /// <param name="operationName">The name of the operation that was executed.</param>
        /// <param name="command">The database command that was executed.</param>
        void OnExecuted(string operationName, DbCommand command);

        /// <summary>
        /// Called when an error occurs during command execution.
        /// </summary>
        /// <param name="operationName">The name of the operation that failed.</param>
        /// <param name="command">The database command that failed.</param>
        /// <param name="exception">The exception that was thrown.</param>
        void OnError(string operationName, DbCommand command, System.Exception exception);
    }
}
