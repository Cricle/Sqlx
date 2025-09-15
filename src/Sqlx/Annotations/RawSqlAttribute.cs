// -----------------------------------------------------------------------
// <copyright file="RawSqlAttribute.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#nullable enable

namespace Sqlx.Annotations
{
    /// <summary>
    /// Specifies raw SQL command text for methods or parameters.
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Method | System.AttributeTargets.Parameter,
        AllowMultiple = false, Inherited = false)]
    public sealed class RawSqlAttribute : System.Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RawSqlAttribute"/> class.
        /// </summary>
        public RawSqlAttribute()
        {
            Sql = string.Empty;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RawSqlAttribute"/> class.
        /// </summary>
        /// <param name="sql">The raw SQL command text.</param>
        public RawSqlAttribute(string sql)
        {
            Sql = sql ?? string.Empty;
        }

        /// <summary>
        /// Gets or sets the raw SQL command text.
        /// </summary>
        public string Sql { get; set; }
    }
}
