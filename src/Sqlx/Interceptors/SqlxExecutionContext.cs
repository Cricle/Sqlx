// -----------------------------------------------------------------------
// <copyright file="SqlxExecutionContext.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Diagnostics;

namespace Sqlx.Interceptors
{
    /// <summary>
    /// SQL执行上下文 - 栈分配
    /// 设计原则：ref struct 强制栈分配，存储 string 引用避免拦截器中 ToString() 分配
    /// </summary>
    public ref struct SqlxExecutionContext
    {
        /// <summary>
        /// 操作名称（方法名）
        /// </summary>
        public readonly string OperationName;

        /// <summary>
        /// Repository类型
        /// </summary>
        public readonly string RepositoryType;

        /// <summary>
        /// 执行的SQL语句
        /// </summary>
        public readonly string Sql;

        /// <summary>
        /// 开始时间戳（Ticks）
        /// </summary>
        public long StartTimestamp;

        /// <summary>
        /// 结束时间戳（Ticks）
        /// </summary>
        public long EndTimestamp;

        /// <summary>
        /// 执行结果（可选，用于传递Activity等对象）
        /// </summary>
        public object? Result;

        /// <summary>
        /// 执行异常（如果失败）
        /// </summary>
        public Exception? Exception;

        /// <summary>
        /// 构造函数 - 初始化执行上下文
        /// </summary>
        /// <param name="operationName">操作名称</param>
        /// <param name="repositoryType">Repository类型</param>
        /// <param name="sql">SQL语句</param>
        public SqlxExecutionContext(
            string operationName,
            string repositoryType,
            string sql)
        {
            OperationName = operationName;
            RepositoryType = repositoryType;
            Sql = sql;
            StartTimestamp = Stopwatch.GetTimestamp();
            EndTimestamp = 0;
            Result = null;
            Exception = null;
        }

        /// <summary>
        /// 计算执行耗时（毫秒）
        /// </summary>
        public readonly double ElapsedMilliseconds =>
            (EndTimestamp - StartTimestamp) / (double)TimeSpan.TicksPerMillisecond;
    }
}


