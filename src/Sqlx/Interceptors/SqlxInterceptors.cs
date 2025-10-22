// -----------------------------------------------------------------------
// <copyright file="SqlxInterceptors.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------


using System;
using System.Runtime.CompilerServices;

namespace Sqlx.Interceptors
{
    /// <summary>
    /// Sqlx全局拦截器注册中心 - 支持多拦截器 + 零GC
    /// 设计原则：
    /// 1. 固定大小数组（最多8个拦截器）- 避免动态扩容
    /// 2. for循环遍历 - 避免枚举器分配
    /// 3. 异常不吞噬 - Fail Fast
    /// 4. AggressiveInlining - 减少调用开销
    /// </summary>
    public static class SqlxInterceptors
    {
        /// <summary>
        /// 固定大小数组（最多8个拦截器，够用了）
        /// </summary>
        private static readonly ISqlxInterceptor?[] _interceptors = new ISqlxInterceptor?[8];

        /// <summary>
        /// 拦截器计数
        /// </summary>
        private static int _count = 0;

        /// <summary>
        /// 是否启用拦截器（快速开关，避免不必要的开销）
        /// </summary>
        public static bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 添加拦截器
        /// </summary>
        /// <param name="interceptor">拦截器实例</param>
        /// <exception cref="ArgumentNullException">拦截器为null</exception>
        /// <exception cref="InvalidOperationException">超过最大拦截器数量</exception>
        public static void Add(ISqlxInterceptor interceptor)
        {
            if (interceptor == null)
                throw new ArgumentNullException(nameof(interceptor));

            if (_count >= _interceptors.Length)
                throw new InvalidOperationException($"最多支持 {_interceptors.Length} 个拦截器");

            _interceptors[_count++] = interceptor;
        }

        /// <summary>
        /// 清除所有拦截器
        /// </summary>
        public static void Clear()
        {
            Array.Clear(_interceptors, 0, _interceptors.Length);
            _count = 0;
        }

    /// <summary>
    /// 执行前拦截（第1步）
    /// Fail Fast: 异常直接抛出，不吞噬
    /// </summary>
    /// <param name="context">执行上下文</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void OnExecuting(ref SqlxExecutionContext context)
        {
            // Fail Fast: 快速退出
            if (!IsEnabled || _count == 0)
                return;

            // 零GC：for循环遍历数组（不使用foreach，避免枚举器分配）
            var interceptors = _interceptors;  // 本地副本，避免重复读取静态字段
            var count = _count;

            for (int i = 0; i < count; i++)
            {
                interceptors[i]!.OnExecuting(ref context);  // 异常直接抛出
            }
        }

    /// <summary>
    /// 执行成功拦截（第2步）
    /// Fail Fast: 异常直接抛出，不吞噬
    /// </summary>
    /// <param name="context">执行上下文</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void OnExecuted(ref SqlxExecutionContext context)
        {
            if (!IsEnabled || _count == 0)
                return;

            var interceptors = _interceptors;
            var count = _count;

            for (int i = 0; i < count; i++)
            {
                interceptors[i]!.OnExecuted(ref context);  // 异常直接抛出
            }
        }

    /// <summary>
    /// 执行失败拦截（第3步）
    /// Fail Fast: 异常直接抛出，不吞噬
    /// </summary>
    /// <param name="context">执行上下文</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void OnFailed(ref SqlxExecutionContext context)
        {
            if (!IsEnabled || _count == 0)
                return;

            var interceptors = _interceptors;
            var count = _count;

            for (int i = 0; i < count; i++)
            {
                interceptors[i]!.OnFailed(ref context);  // 异常直接抛出
            }
        }
    }
}