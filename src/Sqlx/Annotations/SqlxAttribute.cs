// -----------------------------------------------------------------------
// <copyright file="SqlxAttribute.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#nullable enable

namespace Sqlx.Annotations
{
    /// <summary>
    /// 指定方法的 SQL 命令文本、原始 SQL 或存储过程名称
    /// 与 SqlTemplateAttribute 配合使用时，提供编译时 SQL 生成功能
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Method | System.AttributeTargets.Parameter,
        AllowMultiple = true, Inherited = false)]
    public sealed class SqlxAttribute : System.Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SqlxAttribute"/> class.
        /// </summary>
        public SqlxAttribute()
        {
            StoredProcedureName = string.Empty;
            Sql = string.Empty;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlxAttribute"/> class with stored procedure name
        /// </summary>
        public SqlxAttribute(string storedProcedureName)
        {
            StoredProcedureName = storedProcedureName ?? string.Empty;
            Sql = string.Empty;
        }

        /// <summary>
        /// Gets or sets the stored procedure name.
        /// </summary>
        public string StoredProcedureName { get; set; }

        /// <summary>
        /// Gets or sets the raw SQL command text.
        /// </summary>
        public string Sql { get; set; }

        /// <summary>
        /// Gets or sets whether this method accepts SqlTemplate as parameter.
        /// When true, the method can accept SqlTemplate parameter for dynamic SQL generation.
        /// </summary>
        public bool AcceptsSqlTemplate { get; set; }

        /// <summary>
        /// Gets or sets the parameter name for SqlTemplate when AcceptsSqlTemplate is true.
        /// Defaults to "template" if not specified.
        /// </summary>
        public string SqlTemplateParameterName { get; set; } = "template";

        /// <summary>
        /// 指示此方法是否使用编译时 SQL 模板生成
        /// 当为 true 时，将与 SqlTemplateAttribute 协作生成高性能代码
        /// </summary>
        public bool UseCompileTimeTemplate { get; set; } = false;

        /// <summary>
        /// 编译时模板的缓存键，用于优化重复查询
        /// </summary>
        public string? TemplateCacheKey { get; set; }
    }
}
