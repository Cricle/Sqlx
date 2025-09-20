// -----------------------------------------------------------------------
// <copyright file="SqlxAttribute.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#nullable enable

namespace Sqlx.Annotations
{
    /// <summary>
    /// Specify the SQL command text, raw SQL, or stored procedure name for the method
    /// When used with SqlTemplateAttribute, provides compile-time SQL generation functionality
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
        /// Indicates whether this method uses compile-time SQL template generation
        /// When true, will collaborate with SqlTemplateAttribute to generate high-performance code
        /// </summary>
        public bool UseCompileTimeTemplate { get; set; } = false;

        /// <summary>
        /// Cache key for compile-time templates, used to optimize repeated queries
        /// </summary>
        public string? TemplateCacheKey { get; set; }
    }
}
