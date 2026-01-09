// -----------------------------------------------------------------------
// <copyright file="InterceptByAttribute.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#nullable enable

namespace Sqlx.Annotations
{
    /// <summary>
    /// Specifies an interceptor type to handle command execution events.
    /// </summary>
    /// <remarks>
    /// <para>The interceptor type can be either static or instance-based:</para>
    /// <list type="bullet">
    /// <item><description><strong>Static interceptors</strong> - Methods are called directly, no instance created. Best for stateless logging.</description></item>
    /// <item><description><strong>Instance interceptors</strong> - Must implement <see cref="ICommandInterceptor"/>. New instance created for each operation.</description></item>
    /// </list>
    /// <para>Multiple interceptors can be applied to the same repository class.</para>
    /// <para>Compile-time validation ensures interceptors implement required methods.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Static interceptor (recommended for stateless operations)
    /// public static class StaticLoggingInterceptor
    /// {
    ///     public static void OnExecuting(string operationName, DbCommand command)
    ///     {
    ///         Console.WriteLine($"[LOG] Executing: {operationName}");
    ///     }
    ///
    ///     public static void OnExecuted(string operationName, DbCommand command)
    ///     {
    ///         Console.WriteLine($"[LOG] Completed: {operationName}");
    ///     }
    ///
    ///     public static void OnError(string operationName, DbCommand command, Exception exception)
    ///     {
    ///         Console.WriteLine($"[LOG] Error: {exception.Message}");
    ///     }
    /// }
    ///
    /// // Instance interceptor (for stateful operations)
    /// public class PerformanceInterceptor : ICommandInterceptor
    /// {
    ///     private readonly long _startTime = DateTimeOffset.UtcNow.Ticks;
    ///     
    ///     public void OnExecuting(string operationName, DbCommand command) { }
    ///     
    ///     public void OnExecuted(string operationName, DbCommand command)
    ///     {
    ///         var elapsed = DateTimeOffset.UtcNow.Ticks - _startTime;
    ///         Console.WriteLine($"Operation took {elapsed / TimeSpan.TicksPerMillisecond}ms");
    ///     }
    ///     
    ///     public void OnError(string operationName, DbCommand command, Exception exception) { }
    /// }
    ///
    /// // Apply interceptors to repository
    /// [SqlDefine(SqlDefineTypes.MySql)]
    /// [TableName("users")]
    /// [InterceptBy(typeof(StaticLoggingInterceptor))]      // Static - no allocation
    /// [InterceptBy(typeof(PerformanceInterceptor))]        // Instance - new instance each call
    /// public partial class UserRepository : ICrudRepository&lt;User, int&gt; { }
    /// </code>
    /// </example>
    [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class InterceptByAttribute : System.Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InterceptByAttribute"/> class.
        /// </summary>
        /// <param name="interceptorType">The type of the interceptor. Must implement <see cref="ICommandInterceptor"/>.</param>
        public InterceptByAttribute(System.Type interceptorType)
        {
            InterceptorType = interceptorType;
        }

        /// <summary>
        /// Gets the type of the interceptor.
        /// </summary>
        public System.Type InterceptorType { get; }
    }
}
