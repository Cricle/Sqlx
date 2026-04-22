// <copyright file="TypeConverter.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx;

using System;
using System.Data;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;

/// <summary>
/// High-performance type converter for database value to CLR type conversion.
/// Uses Convert.ChangeType with TypeCode for efficient conversion without try-catch fallback.
/// </summary>
public static class TypeConverter
{
    private static readonly Type? DateOnlyType = typeof(DateTime).Assembly.GetType("System.DateOnly");
    private static readonly Type? TimeOnlyType = typeof(DateTime).Assembly.GetType("System.TimeOnly");
    private static readonly Func<DateTime, object>? DateOnlyFromDateTime = CreateUnaryBoxingDelegate<DateTime>(DateOnlyType, "FromDateTime");
    private static readonly Func<string, IFormatProvider, object>? DateOnlyParse = CreateBinaryBoxingDelegate<string, IFormatProvider>(DateOnlyType, "Parse");
    private static readonly Func<DateTime, object>? TimeOnlyFromDateTime = CreateUnaryBoxingDelegate<DateTime>(TimeOnlyType, "FromDateTime");
    private static readonly Func<TimeSpan, object>? TimeOnlyFromTimeSpan = CreateUnaryBoxingDelegate<TimeSpan>(TimeOnlyType, "FromTimeSpan");
    private static readonly Func<string, IFormatProvider, object>? TimeOnlyParse = CreateBinaryBoxingDelegate<string, IFormatProvider>(TimeOnlyType, "Parse");

    // Pre-resolved IDataRecord reader methods - avoids reflection on every BuildConversion call
    private static readonly MethodInfo GetInt32Method   = typeof(IDataRecord).GetMethod("GetInt32",   [typeof(int)])!;
    private static readonly MethodInfo GetInt64Method   = typeof(IDataRecord).GetMethod("GetInt64",   [typeof(int)])!;
    private static readonly MethodInfo GetInt16Method   = typeof(IDataRecord).GetMethod("GetInt16",   [typeof(int)])!;
    private static readonly MethodInfo GetByteMethod    = typeof(IDataRecord).GetMethod("GetByte",    [typeof(int)])!;
    private static readonly MethodInfo GetBooleanMethod = typeof(IDataRecord).GetMethod("GetBoolean", [typeof(int)])!;
    private static readonly MethodInfo GetStringMethod  = typeof(IDataRecord).GetMethod("GetString",  [typeof(int)])!;
    private static readonly MethodInfo GetDateTimeMethod= typeof(IDataRecord).GetMethod("GetDateTime",[typeof(int)])!;
    private static readonly MethodInfo GetDecimalMethod = typeof(IDataRecord).GetMethod("GetDecimal", [typeof(int)])!;
    private static readonly MethodInfo GetDoubleMethod  = typeof(IDataRecord).GetMethod("GetDouble",  [typeof(int)])!;
    private static readonly MethodInfo GetFloatMethod   = typeof(IDataRecord).GetMethod("GetFloat",   [typeof(int)])!;
    private static readonly MethodInfo GetGuidMethod    = typeof(IDataRecord).GetMethod("GetGuid",    [typeof(int)])!;
    private static readonly MethodInfo GetValueMethod   = typeof(IDataRecord).GetMethod("GetValue",   [typeof(int)])!;
    private static readonly MethodInfo IsDBNullMethod   = typeof(IDataRecord).GetMethod("IsDBNull",   [typeof(int)])!;

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

        if (sourceType == typeof(string))
        {
            return ConvertFromString<T>((string)value, underlyingType);
        }

        // Handle DateTimeOffset
        if (underlyingType == typeof(DateTimeOffset))
        {
            if (sourceType == typeof(DateTime))
            {
                return (T)(object)new DateTimeOffset((DateTime)value);
            }

            return (T)value;
        }

        // Handle DateOnly
        if (IsDateOnlyType(underlyingType))
        {
            if (sourceType == typeof(DateTime))
            {
                return (T)CreateDateOnlyFromDateTime((DateTime)value);
            }

            if (sourceType == typeof(DateTimeOffset))
            {
                return (T)CreateDateOnlyFromDateTime(((DateTimeOffset)value).DateTime);
            }

            return (T)value;
        }

        // Handle TimeSpan
        if (underlyingType == typeof(TimeSpan))
        {
            if (value is IConvertible convertible)
            {
                return (T)(object)TimeSpan.FromTicks(convertible.ToInt64(CultureInfo.InvariantCulture));
            }

            return (T)value;
        }

        // Handle TimeOnly
        if (IsTimeOnlyType(underlyingType))
        {
            if (sourceType == typeof(TimeSpan))
            {
                return (T)CreateTimeOnlyFromTimeSpan((TimeSpan)value);
            }

            if (sourceType == typeof(DateTime))
            {
                return (T)CreateTimeOnlyFromDateTime((DateTime)value);
            }

            if (sourceType == typeof(DateTimeOffset))
            {
                return (T)CreateTimeOnlyFromDateTime(((DateTimeOffset)value).DateTime);
            }

            return (T)value;
        }

        // Use TypeCode-based conversion for primitive types
        var typeCode = Type.GetTypeCode(underlyingType);
        if (typeCode != TypeCode.Object)
        {
            var converted = System.Convert.ChangeType(value, typeCode, CultureInfo.InvariantCulture);
            return (T)converted;
        }

        // Fallback to direct cast
        return (T)System.Convert.ChangeType(value, underlyingType, CultureInfo.InvariantCulture);
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    private static T ConvertFromString<T>(string value, Type targetType)
    {
        if (targetType == typeof(string))
        {
            return (T)(object)value;
        }

        if (targetType == typeof(int))
        {
            return (T)(object)int.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
        }

        if (targetType == typeof(long))
        {
            return (T)(object)long.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
        }

        if (targetType == typeof(short))
        {
            return (T)(object)short.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
        }

        if (targetType == typeof(byte))
        {
            return (T)(object)byte.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
        }

        if (targetType == typeof(decimal))
        {
            return (T)(object)decimal.Parse(value, NumberStyles.Number, CultureInfo.InvariantCulture);
        }

        if (targetType == typeof(double))
        {
            return (T)(object)double.Parse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture);
        }

        if (targetType == typeof(float))
        {
            return (T)(object)float.Parse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture);
        }

        if (targetType == typeof(bool))
        {
            return (T)(object)bool.Parse(value);
        }

        if (targetType == typeof(DateTime))
        {
            return (T)(object)DateTime.Parse(value, CultureInfo.InvariantCulture);
        }

        if (targetType == typeof(DateTimeOffset))
        {
            return (T)(object)DateTimeOffset.Parse(value, CultureInfo.InvariantCulture);
        }

        if (IsDateOnlyType(targetType))
        {
            return (T)ParseDateOnly(value);
        }

        if (targetType == typeof(TimeSpan))
        {
            return (T)(object)TimeSpan.Parse(value, CultureInfo.InvariantCulture);
        }

        if (IsTimeOnlyType(targetType))
        {
            return (T)ParseTimeOnly(value);
        }

        if (targetType == typeof(Guid))
        {
            return (T)(object)Guid.Parse(value);
        }

        if (targetType == typeof(byte[]))
        {
            return (T)(object)System.Convert.FromBase64String(value);
        }

        return (T)System.Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Builds an expression that converts a database value to the target type.
    /// Handles type mismatches efficiently without try-catch.
    /// </summary>
    public static Expression BuildConversion(Expression readerParam, Expression ordinalExpr, Type targetType)
    {
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        var isNullable = targetType != underlyingType || !targetType.IsValueType;

        Expression getValue = BuildDirectConversion(readerParam, ordinalExpr, underlyingType);

        if (isNullable)
        {
            var isDbNull = Expression.Call(readerParam, IsDBNullMethod, ordinalExpr);
            var defaultValue = Expression.Default(targetType);
            var convertedValue = targetType.IsValueType && targetType != underlyingType
                ? Expression.Convert(getValue, targetType)
                : getValue;
            return Expression.Condition(isDbNull, defaultValue, convertedValue);
        }

        return getValue;
    }

    private static readonly MethodInfo ConvertOpenMethod = typeof(TypeConverter).GetMethod(nameof(Convert))!;
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, MethodInfo> ConvertMethodCache = new();

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    private static Expression BuildDirectConversion(Expression readerParam, Expression ordinalExpr, Type targetType)
    {
        var readerMethod = GetReaderMethod(targetType);
        if (readerMethod != null)
            return Expression.Call(readerParam, readerMethod, ordinalExpr);

        var valueExpr = Expression.Call(readerParam, GetValueMethod, ordinalExpr);
        var convertMethod = ConvertMethodCache.GetOrAdd(targetType, static t => ConvertOpenMethod.MakeGenericMethod(t));
        return Expression.Call(convertMethod, valueExpr);
    }

    private static MethodInfo? GetReaderMethod(Type targetType) => targetType.Name switch
    {
        "Int32"   => GetInt32Method,
        "Int64"   => GetInt64Method,
        "Int16"   => GetInt16Method,
        "Byte"    => GetByteMethod,
        "Boolean" => GetBooleanMethod,
        "String"  => GetStringMethod,
        "DateTime"=> GetDateTimeMethod,
        "Decimal" => GetDecimalMethod,
        "Double"  => GetDoubleMethod,
        "Single"  => GetFloatMethod,
        "Guid"    => GetGuidMethod,
        _         => null
    };

    private static bool IsDateOnlyType(Type type) => DateOnlyType != null && type == DateOnlyType;

    private static bool IsTimeOnlyType(Type type) => TimeOnlyType != null && type == TimeOnlyType;

    private static object CreateDateOnlyFromDateTime(DateTime value)
    {
        return DateOnlyFromDateTime?.Invoke(value)
            ?? throw new InvalidOperationException("DateOnly conversion is not available on this target framework.");
    }

    private static object CreateTimeOnlyFromDateTime(DateTime value)
    {
        return TimeOnlyFromDateTime?.Invoke(value)
            ?? throw new InvalidOperationException("TimeOnly conversion is not available on this target framework.");
    }

    private static object CreateTimeOnlyFromTimeSpan(TimeSpan value)
    {
        return TimeOnlyFromTimeSpan?.Invoke(value)
            ?? throw new InvalidOperationException("TimeOnly conversion is not available on this target framework.");
    }

    private static object ParseDateOnly(string value)
    {
        return DateOnlyParse?.Invoke(value, CultureInfo.InvariantCulture)
            ?? throw new InvalidOperationException("DateOnly parsing is not available on this target framework.");
    }

    private static object ParseTimeOnly(string value)
    {
        return TimeOnlyParse?.Invoke(value, CultureInfo.InvariantCulture)
            ?? throw new InvalidOperationException("TimeOnly parsing is not available on this target framework.");
    }

    private static Func<TArg, object>? CreateUnaryBoxingDelegate<TArg>(Type? declaringType, string methodName)
    {
        if (declaringType == null)
        {
            return null;
        }

        var method = declaringType.GetMethod(
            methodName,
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: new[] { typeof(TArg) },
            modifiers: null);

        if (method == null)
        {
            return null;
        }

        var arg = Expression.Parameter(typeof(TArg), "arg");
        var call = Expression.Call(method, arg);
        var box = Expression.Convert(call, typeof(object));
        return Expression.Lambda<Func<TArg, object>>(box, arg).Compile();
    }

    private static Func<TArg1, TArg2, object>? CreateBinaryBoxingDelegate<TArg1, TArg2>(Type? declaringType, string methodName)
    {
        if (declaringType == null)
        {
            return null;
        }

        var method = declaringType.GetMethod(
            methodName,
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: new[] { typeof(TArg1), typeof(TArg2) },
            modifiers: null);

        if (method == null)
        {
            return null;
        }

        var arg1 = Expression.Parameter(typeof(TArg1), "arg1");
        var arg2 = Expression.Parameter(typeof(TArg2), "arg2");
        var call = Expression.Call(method, arg1, arg2);
        var box = Expression.Convert(call, typeof(object));
        return Expression.Lambda<Func<TArg1, TArg2, object>>(box, arg1, arg2).Compile();
    }
}
