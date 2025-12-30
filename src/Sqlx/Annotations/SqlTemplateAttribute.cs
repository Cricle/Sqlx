// -----------------------------------------------------------------------
// <copyright file="SqlTemplateAttribute.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#nullable enable

namespace Sqlx.Annotations
{
    /// <summary>
    /// Mark method to use compile-time SQL template, providing safe SQL concatenation functionality
    /// Combined with SqlxAttribute, generates high-performance SQL code at compile time
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class SqlTemplateAttribute : System.Attribute
    {
        /// <summary>
        /// Initialize SqlTemplateAttribute
        /// </summary>
        /// <param name="template">SQL template string, supports @{parameterName} placeholders</param>
        public SqlTemplateAttribute(string template)
        {
            Template = template ?? throw new System.ArgumentNullException(nameof(template));
            Dialect = SqlDefineTypes.SqlServer;
            SafeMode = true;
            ValidateParameters = true;
        }

        /// <summary>
        /// SQL template string, uses @{parameterName} as placeholders
        /// Example: "SELECT * FROM Users WHERE Id = @{userId} AND Name = @{userName}"
        /// </summary>
        public string Template { get; }

        /// <summary>
        /// Database dialect type, defaults to SqlServer
        /// </summary>
        public SqlDefineTypes Dialect { get; set; }

        /// <summary>
        /// Whether to enable safe mode, defaults to true
        /// Safe mode performs SQL injection checks and parameter validation
        /// </summary>
        public bool SafeMode { get; set; }

        /// <summary>
        /// Whether to validate parameters, defaults to true
        /// </summary>
        public bool ValidateParameters { get; set; }

        /// <summary>
        /// Whether to cache generated SQL, defaults to true
        /// </summary>
        public bool EnableCaching { get; set; } = true;
    }

}
