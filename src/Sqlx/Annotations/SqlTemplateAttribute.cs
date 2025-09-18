// -----------------------------------------------------------------------
// <copyright file="SqlTemplateAttribute.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#nullable enable

namespace Sqlx.Annotations
{
    /// <summary>
    /// 标记方法使用编译时 SQL 模板，提供安全的 SQL 拼接功能
    /// 与 SqlxAttribute 结合使用，在编译时生成高性能的 SQL 代码
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class SqlTemplateAttribute : System.Attribute
    {
        /// <summary>
        /// 初始化 SqlTemplateAttribute
        /// </summary>
        /// <param name="template">SQL 模板字符串，支持 @{参数名} 占位符</param>
        public SqlTemplateAttribute(string template)
        {
            Template = template ?? throw new System.ArgumentNullException(nameof(template));
            Dialect = SqlDialectType.SqlServer;
            SafeMode = true;
            ValidateParameters = true;
        }

        /// <summary>
        /// SQL 模板字符串，使用 @{参数名} 作为占位符
        /// 例如: "SELECT * FROM Users WHERE Id = @{userId} AND Name = @{userName}"
        /// </summary>
        public string Template { get; }

        /// <summary>
        /// 数据库方言类型，默认为 SqlServer
        /// </summary>
        public SqlDialectType Dialect { get; set; }

        /// <summary>
        /// 是否启用安全模式，默认为 true
        /// 安全模式下会进行 SQL 注入检查和参数验证
        /// </summary>
        public bool SafeMode { get; set; }

        /// <summary>
        /// 是否验证参数，默认为 true
        /// </summary>
        public bool ValidateParameters { get; set; }

        /// <summary>
        /// 是否缓存生成的 SQL，默认为 true
        /// </summary>
        public bool EnableCaching { get; set; } = true;

        /// <summary>
        /// 执行类型，用于优化生成的代码
        /// </summary>
        public SqlOperation Operation { get; set; } = SqlOperation.Select;
    }

}
