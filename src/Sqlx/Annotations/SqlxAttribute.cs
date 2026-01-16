// <copyright file="SqlxAttribute.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

#nullable enable

namespace Sqlx.Annotations
{
    /// <summary>
    /// Marks a class for source generation of IEntityProvider, IResultReader, and IParameterBinder implementations.
    /// Properties marked with [IgnoreDataMember] will be excluded.
    /// Use [Column] attribute to customize column/parameter names.
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class SqlxAttribute : System.Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SqlxAttribute"/> class.
        /// Generates for the class this attribute is applied to.
        /// </summary>
        public SqlxAttribute()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlxAttribute"/> class.
        /// Generates for the specified target type (useful for sealed/external types).
        /// </summary>
        /// <param name="targetType">The target type to generate for.</param>
        public SqlxAttribute(System.Type targetType)
        {
            TargetType = targetType;
        }

        /// <summary>
        /// Gets the target type to generate for.
        /// If null, generates for the class this attribute is applied to.
        /// </summary>
        public System.Type? TargetType { get; }

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

        /// <summary>
        /// Gets or sets whether to generate IParameterBinder implementation.
        /// Default is true.
        /// </summary>
        public bool GenerateParameterBinder { get; set; } = true;
    }
}
