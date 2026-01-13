// <copyright file="ColumnMeta.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx;

using System.Data;

/// <summary>
/// Represents metadata for a database column, providing AOT-compatible column information without reflection.
/// </summary>
/// <remarks>
/// <para>
/// ColumnMeta is generated at compile-time by the source generator for entities marked with [SqlxEntity].
/// The column name can be customized using the [Column] attribute; otherwise, it defaults to snake_case
/// conversion of the property name.
/// </para>
/// </remarks>
/// <param name="Name">The SQL column name (from [Column] attribute or snake_case of PropertyName).</param>
/// <param name="PropertyName">The C# property name (PascalCase).</param>
/// <param name="DbType">The ADO.NET database type for the column.</param>
/// <param name="IsNullable">Indicates whether the column allows null values.</param>
public record ColumnMeta(
    string Name,
    string PropertyName,
    DbType DbType,
    bool IsNullable);
