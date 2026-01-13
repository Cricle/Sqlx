// <copyright file="SqlxEntityAttribute.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

#nullable enable

namespace Sqlx.Annotations
{
    /// <summary>
    /// Marks a class for source generation of IEntityProvider and IResultReader implementations.
    /// Properties marked with [IgnoreDataMember] will be excluded.
    /// Use [Column] attribute to customize column names.
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class SqlxEntityAttribute : System.Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SqlxEntityAttribute"/> class.
        /// </summary>
        public SqlxEntityAttribute()
        {
        }

        /// <summary>
        /// Gets or sets whether to generate IEntityProvider implementation.
        /// Default is true.
        /// </summary>
        public bool GenerateEntityProvider { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to generate IResultReader implementation.
        /// Default is true.
        /// </summary>
        public bool GenerateResultReader { get; set; } = true;
    }
}
