// -----------------------------------------------------------------------
// <copyright file="EnableMetricsAttribute.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#nullable enable

namespace Sqlx.Annotations
{
    /// <summary>
    /// 控制是否为方法或类生成性能指标代码（Stopwatch计时）
    /// <para>
    /// 当应用到类时，影响该类中所有方法的指标代码生成。
    /// 当应用到方法时，覆盖类级别的设置。
    /// </para>
    /// <para>
    /// 如果不指定此特性，默认行为由条件编译符号决定：
    /// - 未定义 SQLX_DISABLE_METRICS：生成指标代码
    /// - 定义了 SQLX_DISABLE_METRICS：不生成指标代码
    /// </para>
    /// </summary>
    /// <example>
    /// <code>
    /// // 类级别：为所有方法启用指标收集
    /// [EnableMetrics(true)]
    /// public partial class UserRepository : IUserRepository
    /// {
    ///     // 方法级别：禁用特定方法的指标收集
    ///     [EnableMetrics(false)]
    ///     [Sqlx("SELECT COUNT(*) FROM {{table}}")]
    ///     Task&lt;int&gt; GetCountAsync();
    /// }
    /// </code>
    /// </example>
    [System.AttributeUsage(System.AttributeTargets.Method | System.AttributeTargets.Class,
        AllowMultiple = false, Inherited = true)]
    public sealed class EnableMetricsAttribute : System.Attribute
    {
        /// <summary>
        /// 初始化 <see cref="EnableMetricsAttribute"/> 的新实例
        /// </summary>
        /// <param name="enabled">是否启用性能指标代码生成</param>
        public EnableMetricsAttribute(bool enabled = true)
        {
            Enabled = enabled;
        }

        /// <summary>
        /// 获取是否启用性能指标代码生成
        /// </summary>
        public bool Enabled { get; }

        /// <summary>
        /// 获取或设置是否将执行时间传递给Partial方法
        /// <para>
        /// 默认为 true，OnExecuted 和 OnExecuteFail 方法会收到 elapsedTicks 参数
        /// </para>
        /// </summary>
        public bool PassElapsedToPartialMethods { get; set; } = true;

        /// <summary>
        /// 获取或设置慢查询阈值（毫秒）
        /// <para>
        /// 如果设置了此值，当查询执行时间超过阈值时，
        /// 可以在Partial方法中进行特殊处理（如记录警告日志）
        /// </para>
        /// </summary>
        public int SlowQueryThresholdMs { get; set; } = 0;
    }
}

