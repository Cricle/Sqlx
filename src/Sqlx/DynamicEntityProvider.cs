// <copyright file="DynamicEntityProvider.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx;

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

/// <summary>
/// Dynamic entity provider using reflection.
/// Used for types without generated entity providers.
/// </summary>
internal sealed class DynamicEntityProvider<
#if NET5_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(
        System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
    T> : IEntityProvider
{
    private static readonly IReadOnlyList<ColumnMeta> _columns;
    private static readonly Type _entityType = typeof(T);

    static DynamicEntityProvider()
    {
        var type = typeof(T);
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite)
            .ToArray();

        var columns = new List<ColumnMeta>(properties.Length);
        foreach (var prop in properties)
        {
            var columnName = GetColumnName(prop);
            var dbType = GetDbType(prop.PropertyType);
            var isNullable = IsNullable(prop);
            columns.Add(new ColumnMeta(columnName, prop.Name, dbType, isNullable));
        }
        _columns = columns;
    }

    public Type EntityType => _entityType;
    public IReadOnlyList<ColumnMeta> Columns => _columns;

    private static string GetColumnName(PropertyInfo prop)
    {
        // Check for Column attribute
        var columnAttr = prop.GetCustomAttributes()
            .FirstOrDefault(a => a.GetType().Name == "ColumnAttribute");
        if (columnAttr != null)
        {
            var nameProp = columnAttr.GetType().GetProperty("Name");
            if (nameProp?.GetValue(columnAttr) is string name && !string.IsNullOrEmpty(name))
                return name;
        }
        return ToSnakeCase(prop.Name);
    }

    private static string ToSnakeCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        var result = new System.Text.StringBuilder(name.Length + 4);
        for (int i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsUpper(c) && i > 0)
                result.Append('_');
            result.Append(char.ToLowerInvariant(c));
        }
        return result.ToString();
    }

    private static DbType GetDbType(Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
        return underlyingType.Name switch
        {
            "Int32" => DbType.Int32,
            "Int64" => DbType.Int64,
            "Int16" => DbType.Int16,
            "Byte" => DbType.Byte,
            "Boolean" => DbType.Boolean,
            "String" => DbType.String,
            "DateTime" => DbType.DateTime,
            "Decimal" => DbType.Decimal,
            "Double" => DbType.Double,
            "Single" => DbType.Single,
            "Guid" => DbType.Guid,
            "Byte[]" => DbType.Binary,
            _ => DbType.Object
        };
    }

    private static bool IsNullable(PropertyInfo prop)
    {
        if (prop.PropertyType.IsValueType)
        {
            return Nullable.GetUnderlyingType(prop.PropertyType) != null;
        }
        // Reference types - check nullable annotation
        var nullableAttr = prop.CustomAttributes
            .FirstOrDefault(a => a.AttributeType.Name == "NullableAttribute");
        return nullableAttr != null;
    }
}
