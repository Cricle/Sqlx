// -----------------------------------------------------------------------
// <copyright file="DbSetTypeAttribute.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#nullable enable

namespace Sqlx.Annotations
{
    /// <summary>
    /// Specifies the entity type for DbContext methods returning tuples or generic collections.
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Parameter |
        System.AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class DbSetTypeAttribute : System.Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DbSetTypeAttribute"/> class.
        /// </summary>
        /// <param name="type">The entity type.</param>
        public DbSetTypeAttribute(System.Type type)
        {
            Type = type ?? throw new System.ArgumentNullException(nameof(type));
        }

        /// <summary>
        /// Gets the entity type.
        /// </summary>
        public System.Type Type { get; }
    }
}
