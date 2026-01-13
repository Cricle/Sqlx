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
    [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class SqlxParameterAttribute : System.Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SqlxParameterAttribute"/> class.
        /// </summary>
        public SqlxParameterAttribute()
        {
        }
    }
}
