// -----------------------------------------------------------------------
// <copyright file="WhereAttribute.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace Sqlx
{
    /// <summary>
    /// 标记属性用于 WHERE 条件
    /// 用于批量更新和删除操作中指定查询条件字段
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class WhereAttribute : Attribute
    {
        /// <summary>
        /// 初始化 WhereAttribute 实例
        /// </summary>
        public WhereAttribute()
        {
        }

        /// <summary>
        /// 初始化 WhereAttribute 实例并指定操作符
        /// </summary>
        /// <param name="operator">WHERE 条件操作符，默认为 "="</param>
        public WhereAttribute(string @operator)
        {
            Operator = @operator;
        }

        /// <summary>
        /// WHERE 条件操作符，默认为 "="
        /// </summary>
        public string Operator { get; set; } = "=";

        /// <summary>
        /// 是否在条件为空时忽略此字段
        /// </summary>
        public bool IgnoreIfNull { get; set; } = false;
    }
}
