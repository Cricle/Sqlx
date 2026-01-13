// <copyright file="ColumnMeta.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx;

using System.Data;

/// <summary>
/// Column metadata without reflection.
/// Name can be customized via [Column] attribute.
/// </summary>
/// <param name="Name">SQL column name (from [Column] or snake_case of PropertyName).</param>
/// <param name="PropertyName">C# property name (PascalCase).</param>
/// <param name="DbType">Database type for the column.</param>
/// <param name="IsNullable">Whether the column allows null values.</param>
public record ColumnMeta(
    string Name,
    string PropertyName,
    DbType DbType,
    bool IsNullable);
