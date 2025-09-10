// -----------------------------------------------------------------------
// <copyright file="AsyncOptimizer.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Sqlx.Core;

/// <summary>
/// High-performance async optimization utilities.
/// </summary>
internal static class AsyncOptimizer
{
    private static readonly TaskCompletionSource<bool> CompletedTaskSource = new();
    private static readonly Task CompletedTask = Task.CompletedTask;

    static AsyncOptimizer()
    {
        CompletedTaskSource.SetResult(true);
    }

    /// <summary>
    /// Returns a completed task efficiently without allocation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task GetCompletedTask() => CompletedTask;

    /// <summary>
    /// Returns a completed task with result efficiently.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<T> GetCompletedTask<T>(T result) => Task.FromResult(result);

    /// <summary>
    /// Configures await to avoid context capture for better performance.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ConfiguredTaskAwaitable ConfigureAwaitOptimized(this Task task)
        => task.ConfigureAwait(false);

    /// <summary>
    /// Configures await to avoid context capture for better performance.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ConfiguredTaskAwaitable<T> ConfigureAwaitOptimized<T>(this Task<T> task)
        => task.ConfigureAwait(false);

    /// <summary>
    /// Efficiently checks if a CancellationToken is cancelled without throwing.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsCancelledSafe(this CancellationToken cancellationToken)
        => cancellationToken.IsCancellationRequested;

    /// <summary>
    /// Throws if cancellation is requested, with optimized path.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfCancelledOptimized(this CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            cancellationToken.ThrowIfCancellationRequested();
    }
}

/// <summary>
/// Optimized async method generation helpers.
/// </summary>
internal static class AsyncCodeGenerator
{
    /// <summary>
    /// Generates optimized async method signature.
    /// </summary>
    public static void GenerateOptimizedAsyncSignature(IndentedStringBuilder sb, string returnType, string methodName, string parameters)
    {
        // Use ValueTask for better performance when result is often synchronous
        var optimizedReturnType = returnType.StartsWith("Task<")
            ? returnType.Replace("Task<", "ValueTask<")
            : returnType == "Task"
                ? "ValueTask"
                : returnType;

        sb.AppendLine($"public async {optimizedReturnType} {methodName}({parameters})");
    }

    /// <summary>
    /// Generates optimized async method body with proper ConfigureAwait usage.
    /// </summary>
    public static void GenerateOptimizedAsyncBody(IndentedStringBuilder sb, string operationCode, bool hasReturnValue)
    {
        sb.AppendLine("{");
        sb.PushIndent();

        // Add cancellation check at the beginning
        sb.AppendLine("cancellationToken.ThrowIfCancelledOptimized();");
        sb.AppendLine();

        sb.AppendLine("try");
        sb.AppendLine("{");
        sb.PushIndent();

        // Insert the operation code with optimized awaits
        var optimizedCode = operationCode.Replace(".ConfigureAwait(false)", "")
                                       .Replace("await ", "await ")
                                       .Replace(");", ").ConfigureAwaitOptimized();");

        sb.AppendLine(optimizedCode);

        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine("catch (OperationCanceledException)");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine("cancellationToken.ThrowIfCancellationRequested();");
        sb.AppendLine("throw;");
        sb.PopIndent();
        sb.AppendLine("}");

        sb.PopIndent();
        sb.AppendLine("}");
    }

    /// <summary>
    /// Generates hot path optimization for frequently called async methods.
    /// </summary>
    public static void GenerateHotPathOptimization(IndentedStringBuilder sb, string condition, string syncPath, string asyncPath)
    {
        sb.AppendLine($"// Hot path optimization");
        sb.AppendLine($"if ({condition})");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine($"// Synchronous fast path");
        sb.AppendLine(syncPath);
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("// Async path for I/O bound operations");
        sb.AppendLine(asyncPath);
    }
}

/// <summary>
/// Optimized task scheduling and execution utilities.
/// </summary>
internal static class TaskOptimizer
{
    /// <summary>
    /// Executes a task with optimized scheduling for CPU-bound work.
    /// </summary>
    public static Task ExecuteOptimized(Func<Task> taskFactory, bool preferThreadPool = true)
    {
        if (preferThreadPool)
        {
            return Task.Run(taskFactory);
        }

        return taskFactory();
    }

    /// <summary>
    /// Executes a task with result using optimized scheduling.
    /// </summary>
    public static Task<T> ExecuteOptimized<T>(Func<Task<T>> taskFactory, bool preferThreadPool = true)
    {
        if (preferThreadPool)
        {
            return Task.Run(taskFactory);
        }

        return taskFactory();
    }

    /// <summary>
    /// Creates a task continuation with optimized options.
    /// </summary>
    public static Task ContinueWithOptimized<T>(this Task<T> task, Action<Task<T>> continuation)
    {
        return task.ContinueWith(continuation,
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnRanToCompletion,
            TaskScheduler.Default);
    }
}
