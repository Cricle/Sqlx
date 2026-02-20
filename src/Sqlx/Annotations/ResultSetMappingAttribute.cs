// <copyright file="ResultSetMappingAttribute.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx.Annotations;

using System;

/// <summary>
/// Specifies the mapping between a result set and a tuple element in the method return value.
/// Use this attribute to explicitly define which result set corresponds to which tuple element.
/// </summary>
/// <remarks>
/// <para>
/// Index 0 typically represents the affected row count (ExecuteNonQuery return value).
/// Index 1+ represents SELECT statement result sets in order.
/// </para>
/// <para>
/// Example:
/// <code>
/// [SqlTemplate(@"
///     INSERT INTO users (name) VALUES (@name);
///     SELECT last_insert_rowid();
///     SELECT COUNT(*) FROM users
/// ")]
/// [ResultSetMapping(0, "rowsAffected")]
/// [ResultSetMapping(1, "userId")]
/// [ResultSetMapping(2, "totalUsers")]
/// Task&lt;(int rowsAffected, long userId, int totalUsers)&gt; InsertAndGetStatsAsync(string name);
/// </code>
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class ResultSetMappingAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResultSetMappingAttribute"/> class.
    /// </summary>
    /// <param name="index">The zero-based index of the result set. Index 0 is typically the affected row count.</param>
    /// <param name="name">The name of the tuple element that corresponds to this result set.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when index is negative.</exception>
    /// <exception cref="ArgumentException">Thrown when name is null or whitespace.</exception>
    public ResultSetMappingAttribute(int index, string name)
    {
        if (index < 0)
            throw new ArgumentOutOfRangeException(nameof(index), "Index must be non-negative");
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be null or whitespace", nameof(name));

        Index = index;
        Name = name;
    }

    /// <summary>
    /// Gets the zero-based index of the result set.
    /// Index 0 typically represents the affected row count (ExecuteNonQuery return value).
    /// Index 1+ represents SELECT statement result sets in order.
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// Gets the name of the tuple element that corresponds to this result set.
    /// This name must match the tuple element name in the method return type.
    /// </summary>
    public string Name { get; }
}
