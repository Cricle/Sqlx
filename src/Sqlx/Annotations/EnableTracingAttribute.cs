// -----------------------------------------------------------------------
// <copyright file="EnableTracingAttribute.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#nullable enable

namespace Sqlx.Annotations
{
    /// <summary>
    /// 控制是否为方法或类生成Activity追踪代码
    /// <para>
    /// 当应用到类时，影响该类中所有方法的追踪代码生成。
    /// 当应用到方法时，覆盖类级别的设置。
    /// </para>
    /// <para>
    /// 如果不指定此特性，默认行为由条件编译符号决定：
    /// - 未定义 SQLX_DISABLE_TRACING：生成追踪代码
    /// - 定义了 SQLX_DISABLE_TRACING：不生成追踪代码
    /// </para>
    /// </summary>
    /// <example>
    /// <code>
    /// // 类级别：禁用所有方法的追踪
    /// [EnableTracing(false)]
    /// public partial class UserRepository : IUserRepository
    /// {
    ///     // 方法级别：为特定方法启用追踪（覆盖类设置）
    ///     [EnableTracing(true)]
    ///     [Sqlx("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    ///     Task&lt;User?&gt; GetByIdAsync(int id);
    /// }
    /// </code>
    /// </example>
    [System.AttributeUsage(System.AttributeTargets.Method | System.AttributeTargets.Class,
        AllowMultiple = false, Inherited = true)]
    public sealed class EnableTracingAttribute : System.Attribute
    {
        /// <summary>
        /// 初始化 <see cref="EnableTracingAttribute"/> 的新实例
        /// </summary>
        /// <param name="enabled">是否启用Activity追踪代码生成</param>
        public EnableTracingAttribute(bool enabled = true)
        {
            Enabled = enabled;
        }

        /// <summary>
        /// 获取是否启用Activity追踪代码生成
        /// </summary>
        public bool Enabled { get; }

        /// <summary>
        /// 获取或设置Activity名称
        /// <para>
        /// 如果未指定，默认使用 "Sqlx.{ClassName}.{MethodName}" 格式
        /// </para>
        /// </summary>
        public string? ActivityName { get; set; }

        /// <summary>
        /// 获取或设置是否记录SQL语句
        /// <para>
        /// 默认为 true，将SQL语句作为 Activity 的 tag 记录
        /// </para>
        /// </summary>
        public bool LogSql { get; set; } = true;

        /// <summary>
        /// 获取或设置是否记录参数值
        /// <para>
        /// 默认为 false，避免记录敏感信息
        /// </para>
        /// </summary>
        public bool LogParameters { get; set; } = false;
    }
}

