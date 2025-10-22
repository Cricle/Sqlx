// -----------------------------------------------------------------------
// <copyright file="SimpleLogInterceptor.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using Sqlx.Interceptors;

namespace TodoWebApi.Interceptors
{
    /// <summary>
    /// 简单日志拦截器 - 演示如何使用拦截器
    /// </summary>
    public sealed class SimpleLogInterceptor : ISqlxInterceptor
    {
        public void OnExecuting(ref SqlxExecutionContext context)
        {
            Console.WriteLine($"🔄 [Sqlx] 执行: {context.OperationName}");
            Console.WriteLine($"   SQL: {context.Sql}");
        }

        public void OnExecuted(ref SqlxExecutionContext context)
        {
            Console.WriteLine($"✅ [Sqlx] 完成: {context.OperationName} ({context.ElapsedMilliseconds:F2}ms)");
        }

        public void OnFailed(ref SqlxExecutionContext context)
        {
            Console.WriteLine($"❌ [Sqlx] 失败: {context.OperationName} - {context.Exception?.Message}");
            Console.WriteLine($"   耗时: {context.ElapsedMilliseconds:F2}ms");
        }
    }
}

