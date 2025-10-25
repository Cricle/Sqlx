// -----------------------------------------------------------------------
// <copyright file="AuditFieldsAttribute.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace Sqlx.Annotations;

/// <summary>
/// Marks an entity class as having audit fields that should be automatically populated.
/// When applied, INSERT operations will set CreatedAt (and optionally CreatedBy),
/// and UPDATE operations will set UpdatedAt (and optionally UpdatedBy).
/// </summary>
/// <example>
/// <code>
/// [AuditFields(CreatedByColumn = "CreatedBy", UpdatedByColumn = "UpdatedBy")]
/// public class User
/// {
///     public long Id { get; set; }
///     public string Name { get; set; } = "";
///     public DateTime CreatedAt { get; set; }
///     public string? CreatedBy { get; set; }
///     public DateTime UpdatedAt { get; set; }
///     public string? UpdatedBy { get; set; }
/// }
/// 
/// // Generated repository methods will:
/// // - INSERT: Add created_at = NOW(), created_by = @createdBy
/// // - UPDATE: Add updated_at = NOW(), updated_by = @updatedBy
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class AuditFieldsAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the name of the column that stores when a record was created.
    /// Default: "CreatedAt"
    /// </summary>
    public string CreatedAtColumn { get; set; } = "CreatedAt";

    /// <summary>
    /// Gets or sets the name of the column that stores who created the record.
    /// If null, CreatedBy tracking will not be performed.
    /// Default: null
    /// </summary>
    public string? CreatedByColumn { get; set; }

    /// <summary>
    /// Gets or sets the name of the column that stores when a record was last updated.
    /// Default: "UpdatedAt"
    /// </summary>
    public string UpdatedAtColumn { get; set; } = "UpdatedAt";

    /// <summary>
    /// Gets or sets the name of the column that stores who last updated the record.
    /// If null, UpdatedBy tracking will not be performed.
    /// Default: null
    /// </summary>
    public string? UpdatedByColumn { get; set; }
}

