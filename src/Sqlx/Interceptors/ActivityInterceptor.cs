// -----------------------------------------------------------------------
// <copyright file="ActivityInterceptor.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Sqlx.Interceptors
{
    /// <summary>
    /// 基于 Activity.Current 的拦截器 - 使用当前 Activity 上下文
    /// 与现有 APM 工具（OpenTelemetry、Application Insights）集成
    /// 设计原则：
    /// 1. 使用 Activity.Current - 不创建新的 Activity
    /// 2. 仅添加标签 - 不管理 Activity 生命周期
    /// 3. 零GC - 避免 ToString() 调用，直接使用 string 字段
    /// 4. Fail Fast - 使用 AggressiveInlining 和早期返回
    /// </summary>
    public sealed class ActivityInterceptor : ISqlxInterceptor
    {
        /// <summary>
        /// 执行前：在当前 Activity 上添加标签
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnExecuting(ref SqlxExecutionContext context)
        {
            var activity = Activity.Current;
            if (activity == null || !activity.IsAllDataRequested)
                return; // Fail Fast

            // 使用 DisplayName 代替部分 SetTag（零分配）
            activity.DisplayName = context.OperationName;

            // 设置标准的 OpenTelemetry 标签（直接使用 string，无 ToString()）
            activity.SetTag("db.system", "sql");
            activity.SetTag("db.operation", context.OperationName);

            // SQL 可能很长，只在需要详细追踪时设置
            if (activity.IsAllDataRequested)
            {
                activity.SetTag("db.statement", context.Sql);
            }
        }

        /// <summary>
        /// 执行成功：记录执行时间
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnExecuted(ref SqlxExecutionContext context)
        {
            var activity = Activity.Current;
            if (activity == null)
                return;

            // 使用 long 避免 double 装箱
            activity.SetTag("db.duration_ms", (long)context.ElapsedMilliseconds);
            activity.SetTag("db.success", true);

#if NET5_0_OR_GREATER
            activity.SetStatus(ActivityStatusCode.Ok);
#endif
        }

        /// <summary>
        /// 执行失败：记录错误信息
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnFailed(ref SqlxExecutionContext context)
        {
            var activity = Activity.Current;
            if (activity == null)
                return;

            // 使用 long 避免 double 装箱
            activity.SetTag("db.duration_ms", (long)context.ElapsedMilliseconds);
            activity.SetTag("db.success", false);

#if NET5_0_OR_GREATER
            activity.SetStatus(ActivityStatusCode.Error, context.Exception?.Message);
#endif

            if (context.Exception != null)
            {
                activity.SetTag("error.type", context.Exception.GetType().Name);
                activity.SetTag("error.message", context.Exception.Message);
            }
        }
    }
}
