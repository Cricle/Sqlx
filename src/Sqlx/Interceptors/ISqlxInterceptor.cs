// -----------------------------------------------------------------------
// <copyright file="ISqlxInterceptor.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------


namespace Sqlx.Interceptors
{
    /// <summary>
    /// Sqlx全局拦截器接口 - 极简设计
    /// 设计原则：简单、快速、异常不吞噬
    /// </summary>
    public interface ISqlxInterceptor
    {
        /// <summary>
        /// SQL执行前调用
        /// 注意：异常会直接抛出，不会被吞噬（Fail Fast）
        /// </summary>
        /// <param name="context">执行上下文（ref 避免拷贝）</param>
        void OnExecuting(ref SqlxExecutionContext context);

        /// <summary>
        /// SQL执行成功后调用
        /// 注意：异常会直接抛出，不会被吞噬（Fail Fast）
        /// </summary>
        /// <param name="context">执行上下文（ref 避免拷贝）</param>
        void OnExecuted(ref SqlxExecutionContext context);

        /// <summary>
        /// SQL执行失败时调用
        /// 注意：异常会直接抛出，不会被吞噬（Fail Fast）
        /// </summary>
        /// <param name="context">执行上下文（ref 避免拷贝）</param>
        void OnFailed(ref SqlxExecutionContext context);
    }
}
