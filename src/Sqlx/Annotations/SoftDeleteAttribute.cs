// -----------------------------------------------------------------------
// <copyright file="SoftDeleteAttribute.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace Sqlx.Annotations;

/// <summary>
/// Marks an entity class as supporting soft delete functionality.
/// When applied, DELETE operations will be converted to UPDATE operations that set a flag,
/// and SELECT operations will automatically filter out deleted records.
/// </summary>
/// <example>
/// <code>
/// [SoftDelete(FlagColumn = "IsDeleted", TimestampColumn = "DeletedAt")]
/// public class User
/// {
///     public long Id { get; set; }
///     public string Name { get; set; } = "";
///     public bool IsDeleted { get; set; }
///     public DateTime? DeletedAt { get; set; }
/// }
/// 
/// // Generated repository methods will:
/// // - SELECT: Add WHERE is_deleted = false automatically
/// // - DELETE: Convert to UPDATE SET is_deleted = true, deleted_at = NOW()
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class SoftDeleteAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the name of the boolean flag column that indicates if a record is deleted.
    /// Default: "IsDeleted"
    /// </summary>
    public string FlagColumn { get; set; } = "IsDeleted";

    /// <summary>
    /// Gets or sets the name of the timestamp column that stores when a record was deleted.
    /// If null, no timestamp will be set on delete.
    /// Default: null
    /// </summary>
    public string? TimestampColumn { get; set; }

    /// <summary>
    /// Gets or sets the name of the column that stores who deleted the record.
    /// If null, no user tracking will be performed.
    /// Default: null
    /// </summary>
    public string? DeletedByColumn { get; set; }
}

