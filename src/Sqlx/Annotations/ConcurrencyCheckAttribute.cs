// -----------------------------------------------------------------------
// <copyright file="ConcurrencyCheckAttribute.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace Sqlx.Annotations;

/// <summary>
/// Marks a property as a concurrency token for optimistic locking.
/// When applied to a property (typically named "Version" or "RowVersion"),
/// UPDATE operations will automatically:
/// 1. Check that the current version matches (WHERE version = @version)
/// 2. Increment the version (SET version = version + 1)
/// 
/// If the version doesn't match (i.e., another user updated the record),
/// the UPDATE will affect 0 rows, indicating a concurrency conflict.
/// </summary>
/// <example>
/// <code>
/// public class Product
/// {
///     public long Id { get; set; }
///     public string Name { get; set; } = "";
///     
///     [ConcurrencyCheck]
///     public int Version { get; set; }
/// }
/// 
/// // Original SQL template:
/// // UPDATE product SET name = @name WHERE id = @id
/// 
/// // Generated SQL:
/// // UPDATE product SET name = @name, version = version + 1 
/// // WHERE id = @id AND version = @version
/// 
/// // Returns 0 if version mismatch (conflict detected)
/// // Returns 1 if successful
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class ConcurrencyCheckAttribute : Attribute
{
}

