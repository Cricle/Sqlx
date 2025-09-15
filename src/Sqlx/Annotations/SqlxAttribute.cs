// -----------------------------------------------------------------------
// <copyright file="SqlxAttribute.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#nullable enable

namespace Sqlx.Annotations
{
    /// <summary>
    /// Specifies SQL command text or stored procedure name for a method.
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
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlxAttribute"/> class.
        /// </summary>
        /// <param name="storedProcedureName">The stored procedure name.</param>
        public SqlxAttribute(string storedProcedureName)
        {
            StoredProcedureName = storedProcedureName ?? string.Empty;
        }

        /// <summary>
        /// Gets or sets the stored procedure name.
        /// </summary>
        public string StoredProcedureName { get; set; }
    }
}
