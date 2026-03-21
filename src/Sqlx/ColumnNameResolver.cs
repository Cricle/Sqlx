// <copyright file="ColumnNameResolver.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Sqlx.Expressions;

internal static class ColumnNameResolver
{
    private static readonly ConditionalWeakTable<IEntityProvider, Dictionary<string, string>> Cache = new();

    public static bool TryResolveMappedColumnName(
        IEntityProvider? entityProvider,
        string propertyName,
        out string columnName)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            throw new ArgumentException("Property name cannot be null or whitespace.", nameof(propertyName));
        }

        if (entityProvider?.Columns.Count > 0)
        {
            var mappings = Cache.GetValue(entityProvider, static provider => CreateMappings(provider.Columns));
            if (mappings.TryGetValue(propertyName, out var mappedColumnName) && mappedColumnName != null)
            {
                columnName = mappedColumnName;
                return true;
            }
        }

        columnName = string.Empty;
        return false;
    }

    public static string Resolve(IEntityProvider? entityProvider, string propertyName)
    {
        if (TryResolveMappedColumnName(entityProvider, propertyName, out var columnName))
        {
            return columnName;
        }

        return ExpressionHelper.ConvertToSnakeCase(propertyName);
    }

    private static Dictionary<string, string> CreateMappings(IReadOnlyList<ColumnMeta> columns)
    {
        var mappings = new Dictionary<string, string>(columns.Count, StringComparer.Ordinal);
        for (var i = 0; i < columns.Count; i++)
        {
            mappings[columns[i].PropertyName] = columns[i].Name;
        }

        return mappings;
    }
}
