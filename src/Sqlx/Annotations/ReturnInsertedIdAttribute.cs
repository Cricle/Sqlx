// -----------------------------------------------------------------------
// <copyright file="ReturnInsertedIdAttribute.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace Sqlx.Annotations
{
    /// <summary>
    /// Marks Insert method to return the newly inserted ID.
    /// Automatically generates database-specific code to retrieve the last inserted ID.
    /// </summary>
    /// <remarks>
    /// Database-specific implementations:
    /// - PostgreSQL/SQLite: RETURNING id
    /// - SQL Server: OUTPUT INSERTED.id
    /// - MySQL: LAST_INSERT_ID()
    /// - Oracle: RETURNING id INTO :out_id
    ///
    /// The method return type must match the entity's primary key type (TKey).
    ///
    /// AOT-friendly: No reflection, all code generated at compile time.
    /// GC-friendly: No boxing for value types, uses ValueTask when possible.
    /// </remarks>
    /// <example>
    /// <code>
    /// [SqlTemplate("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})")]
    /// [ReturnInsertedId]
    /// Task&lt;long&gt; InsertAndGetIdAsync(User entity);
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class ReturnInsertedIdAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the primary key column name (default: "Id").
        /// </summary>
        public string IdColumnName { get; set; } = "Id";

        /// <summary>
        /// Gets or sets whether to use ValueTask for better GC performance (default: false for compatibility).
        /// </summary>
        /// <remarks>
        /// ValueTask reduces allocations when the operation completes synchronously,
        /// but requires .NET 5.0+ and careful usage (should not be awaited multiple times).
        /// </remarks>
        public bool UseValueTask { get; set; } = false;
    }

    /// <summary>
    /// Marks Insert method to return the newly inserted entity (with ID populated).
    /// Similar to ReturnInsertedId but returns the complete entity.
    /// </summary>
    /// <remarks>
    /// The method return type must be Task&lt;TEntity&gt; or ValueTask&lt;TEntity&gt;.
    /// The entity object will have its ID property populated with the new value.
    ///
    /// AOT-friendly: Uses compile-time property access, no reflection.
    /// GC-friendly: Reuses the input entity instance when possible.
    /// </remarks>
    /// <example>
    /// <code>
    /// [SqlTemplate("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})")]
    /// [ReturnInsertedEntity]
    /// Task&lt;User&gt; InsertAndReturnAsync(User entity);
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class ReturnInsertedEntityAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the primary key column name (default: "Id").
        /// </summary>
        public string IdColumnName { get; set; } = "Id";

        /// <summary>
        /// Gets or sets whether to use ValueTask for better GC performance (default: false for compatibility).
        /// </summary>
        public bool UseValueTask { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to create a new entity instance or modify the input entity (default: false = modify input).
        /// </summary>
        /// <remarks>
        /// - false (default): Modifies the input entity and returns it (zero allocation).
        /// - true: Creates a new entity instance (safer for immutable patterns).
        /// </remarks>
        public bool CreateNewInstance { get; set; } = false;
    }

    /// <summary>
    /// Marks Insert method to automatically set the entity's ID property after insertion.
    /// The method returns void/Task but modifies the entity in-place.
    /// </summary>
    /// <remarks>
    /// Most GC-friendly option: No allocations, modifies entity in-place.
    /// The entity's ID property must be settable (not readonly).
    ///
    /// This is useful when you want to insert and continue using the entity object
    /// without creating a new instance or explicit ID retrieval.
    /// </remarks>
    /// <example>
    /// <code>
    /// [SqlTemplate("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})")]
    /// [SetEntityId]
    /// Task InsertAsync(User entity);  // entity.Id will be populated after insertion
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class SetEntityIdAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the primary key column name (default: "Id").
        /// </summary>
        public string IdColumnName { get; set; } = "Id";
    }
}

