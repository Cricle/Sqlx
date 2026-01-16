// <copyright file="SqlxParameterAttribute.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

#nullable enable

namespace Sqlx.Annotations
{
    /// <summary>
    /// Marks a class for source generation of IParameterBinder implementation.
    /// Properties marked with [IgnoreDataMember] will be excluded.
    /// Use [Column] attribute to customize parameter names.
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class SqlxParameterAttribute : System.Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SqlxParameterAttribute"/> class.
        /// Generates binder for the class this attribute is applied to.
        /// </summary>
        public SqlxParameterAttribute()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlxParameterAttribute"/> class.
        /// Generates binder for the specified target type (useful for sealed/external types).
        /// </summary>
        /// <param name="targetType">The target type to generate binder for.</param>
        public SqlxParameterAttribute(System.Type targetType)
        {
            TargetType = targetType;
        }

        /// <summary>
        /// Gets the target type to generate binder for.
        /// If null, generates for the class this attribute is applied to.
        /// </summary>
        public System.Type? TargetType { get; }
    }
}
