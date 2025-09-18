// -----------------------------------------------------------------------
// <copyright file="DbSetTypeAttribute.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#nullable enable

namespace Sqlx.Annotations
{
    /// <summary>
    /// Specifies the DbSet type for a property or field.
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Property | System.AttributeTargets.Field,
        AllowMultiple = false, Inherited = false)]
    public sealed class DbSetTypeAttribute : System.Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DbSetTypeAttribute"/> class.
        /// </summary>
        /// <param name="entityType">The entity type.</param>
        public DbSetTypeAttribute(System.Type entityType)
        {
            EntityType = entityType ?? throw new System.ArgumentNullException(nameof(entityType));
        }

        /// <summary>
        /// Gets the entity type.
        /// </summary>
        public System.Type EntityType { get; }
    }
}

