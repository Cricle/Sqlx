// <copyright file="DynamicEntityProvider.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Linq;
using System.Reflection;
using Sqlx.Expressions;

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
        var columnAttr = prop.GetCustomAttribute<ColumnAttribute>();
        if (columnAttr?.Name is { Length: > 0 } name) return name;
        return ExpressionHelper.ConvertToSnakeCase(prop.Name);
    }

    private static DbType GetDbType(Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
        return underlyingType.Name switch
        {
            "SByte" => DbType.SByte,
            "UInt16" => DbType.UInt16,
            "UInt32" => DbType.UInt32,
            "UInt64" => DbType.UInt64,
            "Int32" => DbType.Int32,
            "Int64" => DbType.Int64,
            "Int16" => DbType.Int16,
            "Byte" => DbType.Byte,
            "Boolean" => DbType.Boolean,
            "String" => DbType.String,
            "DateTime" => DbType.DateTime,
            "DateOnly" => DbType.Date,
            "DateTimeOffset" => DbType.DateTimeOffset,
            "TimeOnly" => DbType.Time,
            "TimeSpan" => DbType.Time,
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
            return Nullable.GetUnderlyingType(prop.PropertyType) != null;
#if NET6_0_OR_GREATER
        return new NullabilityInfoContext().Create(prop).WriteState == NullabilityState.Nullable;
#else
        return prop.CustomAttributes.Any(a => a.AttributeType.Name == "NullableAttribute");
#endif
    }
}
