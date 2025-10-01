// Sqlx核心特性定义 - VS插件独立版本
using System;

namespace Sqlx
{
    /// <summary>
    /// 标记方法为Sqlx ORM方法，支持SQL参数化查询
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class SqlxAttribute : Attribute
    {
        /// <summary>
        /// SQL查询语句
        /// </summary>
        public string Sql { get; }

        /// <summary>
        /// 创建Sqlx特性
        /// </summary>
        /// <param name="sql">SQL查询语句</param>
        public SqlxAttribute(string sql)
        {
            Sql = sql;
        }
    }

    /// <summary>
    /// 标记方法使用SQL模板引擎，支持占位符替换
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class SqlTemplateAttribute : Attribute
    {
        /// <summary>
        /// SQL模板字符串
        /// </summary>
        public string Template { get; }

        /// <summary>
        /// 创建SQL模板特性
        /// </summary>
        /// <param name="template">SQL模板字符串</param>
        public SqlTemplateAttribute(string template)
        {
            Template = template;
        }
    }

    /// <summary>
    /// 标记方法使用Expression转SQL功能，支持LINQ表达式
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ExpressionToSqlAttribute : Attribute
    {
        /// <summary>
        /// 基础SQL模板
        /// </summary>
        public string? BaseTemplate { get; }

        /// <summary>
        /// 创建Expression转SQL特性
        /// </summary>
        /// <param name="baseTemplate">基础SQL模板</param>
        public ExpressionToSqlAttribute(string? baseTemplate = null)
        {
            BaseTemplate = baseTemplate;
        }
    }
}
