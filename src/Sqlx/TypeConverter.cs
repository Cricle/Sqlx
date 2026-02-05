// <copyright file="TypeConverter.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx;

using System;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;

/// <summary>
/// High-performance type converter for database value to CLR type conversion.
/// Uses Convert.ChangeType with TypeCode for efficient conversion without try-catch fallback.
/// </summary>
public static class TypeConverter
{
    /// <summary>
    /// Converts a database value to the target type using efficient conversion.
    /// Public API for generated code.
    /// </summary>
    public static T Convert<T>(object? value)
    {
        if (value == null || value is DBNull)
        {
            return default!;
        }

        var targetType = typeof(T);
        var sourceType = value.GetType();

        // Same type - no conversion needed
        if (sourceType == targetType)
        {
            return (T)value;
        }

        // Handle nullable types
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        // Handle enums
        if (underlyingType.IsEnum)
        {
            if (sourceType == typeof(string))
            {
                return (T)Enum.Parse(underlyingType, (string)value, ignoreCase: true);
            }
            // Convert numeric value to enum - use underlying type directly
            return (T)Enum.ToObject(underlyingType, value);
        }

        // Handle Guid
        if (underlyingType == typeof(Guid))
        {
            if (sourceType == typeof(string))
            {
                return (T)(object)Guid.Parse((string)value);
            }
            if (sourceType == typeof(byte[]))
            {
                return (T)(object)new Guid((byte[])value);
            }
            // Guid doesn't support ChangeType, return as-is
            return (T)value;
        }

        // Handle byte[]
        if (underlyingType == typeof(byte[]))
        {
            if (sourceType == typeof(string))
            {
                return (T)(object)System.Convert.FromBase64String((string)value);
            }
            return (T)value;
        }

        // Use TypeCode-based conversion for primitive types
        var typeCode = Type.GetTypeCode(underlyingType);
        if (typeCode != TypeCode.Object)
        {
            var converted = System.Convert.ChangeType(value, typeCode);
            return (T)converted;
        }

        // Fallback to direct cast
        return (T)System.Convert.ChangeType(value, underlyingType);
    }

    /// <summary>
    /// Builds an expression that converts a database value to the target type.
    /// Handles type mismatches efficiently without try-catch.
    /// </summary>
    public static Expression BuildConversion(Expression readerParam, Expression ordinalExpr, Type targetType)
    {
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        var isNullable = targetType != underlyingType || !targetType.IsValueType;

        // Build the value retrieval and conversion
        Expression getValue = BuildDirectConversion(readerParam, ordinalExpr, underlyingType);

        // Handle nullability
        if (isNullable)
        {
            var isDbNullMethod = typeof(IDataRecord).GetMethod("IsDBNull", new[] { typeof(int) })!;
            var isDbNull = Expression.Call(readerParam, isDbNullMethod, ordinalExpr);
            var defaultValue = Expression.Default(targetType);
            
            // Convert to nullable if needed
            var convertedValue = targetType.IsValueType && targetType != underlyingType 
                ? Expression.Convert(getValue, targetType) 
                : getValue;
            
            return Expression.Condition(isDbNull, defaultValue, convertedValue);
        }

        return getValue;
    }

    private static Expression BuildDirectConversion(Expression readerParam, Expression ordinalExpr, Type targetType)
    {
        // Try to get the value using the most appropriate IDataReader method
        var readerMethod = GetReaderMethod(targetType);
        
        if (readerMethod != null)
        {
            // Direct read - no conversion needed
            return Expression.Call(readerParam, readerMethod, ordinalExpr);
        }

        // Need conversion - get as object first, then convert using our Convert<T> method
        var getValueMethod = typeof(IDataRecord).GetMethod("GetValue", new[] { typeof(int) })!;
        var valueExpr = Expression.Call(readerParam, getValueMethod, ordinalExpr);
        
        // Call TypeConverter.Convert<T>(value)
        var convertMethod = typeof(TypeConverter).GetMethod(nameof(Convert))!.MakeGenericMethod(targetType);
        return Expression.Call(convertMethod, valueExpr);
    }

    private static MethodInfo? GetReaderMethod(Type targetType)
    {
        var methodName = targetType.Name switch
        {
            "Int32" => "GetInt32",
            "Int64" => "GetInt64",
            "Int16" => "GetInt16",
            "Byte" => "GetByte",
            "Boolean" => "GetBoolean",
            "String" => "GetString",
            "DateTime" => "GetDateTime",
            "Decimal" => "GetDecimal",
            "Double" => "GetDouble",
            "Single" => "GetFloat",
            "Guid" => "GetGuid",
            _ => null
        };

        return methodName != null 
            ? typeof(IDataRecord).GetMethod(methodName, new[] { typeof(int) })
            : null;
    }
}
