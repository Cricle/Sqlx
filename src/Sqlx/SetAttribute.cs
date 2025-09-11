// -----------------------------------------------------------------------
// <copyright file="SetAttribute.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace Sqlx
{
    /// <summary>
    /// 标记属性用于 SET 子句
    /// 用于批量更新操作中指定要更新的字段
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class SetAttribute : Attribute
    {
        /// <summary>
        /// 初始化 SetAttribute 实例
        /// </summary>
        public SetAttribute()
        {
        }

        /// <summary>
        /// 初始化 SetAttribute 实例并指定是否忽略空值
        /// </summary>
        /// <param name="ignoreIfNull">当值为空时是否忽略此字段</param>
        public SetAttribute(bool ignoreIfNull)
        {
            IgnoreIfNull = ignoreIfNull;
        }

        /// <summary>
        /// 当值为空时是否忽略此字段
        /// </summary>
        public bool IgnoreIfNull { get; set; } = false;

        /// <summary>
        /// 自定义 SET 表达式，如 "Price = Price * 1.1"
        /// 如果指定了此属性，将使用自定义表达式而不是简单的赋值
        /// </summary>
        public string? CustomExpression { get; set; }
    }
}
