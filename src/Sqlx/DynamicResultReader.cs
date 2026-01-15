// <copyright file="DynamicResultReader.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx;

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

/// <summary>
/// Dynamic result reader using expression trees.
/// Used for anonymous types and types without generated readers.
/// AOT-compatible (uses expression tree compilation, not Reflection.Emit).
/// </summary>
internal sealed class DynamicResultReader<
#if NET5_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(
        System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties |
        System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)]
#endif
    T> : IResultReader<T>
{
    // Cached method info for IDataRecord methods (shared across all instances)
    private static readonly MethodInfo? _getInt32Method;
    private static readonly MethodInfo? _getInt64Method;
    private static readonly MethodInfo? _getInt16Method;
    private static readonly MethodInfo? _getByteMethod;
    private static readonly MethodInfo? _getBooleanMethod;
    private static readonly MethodInfo? _getStringMethod;
    private static readonly MethodInfo? _getDateTimeMethod;
    private static readonly MethodInfo? _getDecimalMethod;
    private static readonly MethodInfo? _getDoubleMethod;
    private static readonly MethodInfo? _getFloatMethod;
    private static readonly MethodInfo? _getGuidMethod;
    private static readonly MethodInfo _getValueMethod;
    private static readonly MethodInfo _isDbNullMethod;

    private readonly Func<IDataReader, T> _readFunc;
    private readonly string[] _columnNames;

    static DynamicResultReader()
    {
        // Cache all IDataRecord methods once
        var methods = typeof(IDataRecord).GetMethods();
        
        _getInt32Method = methods.FirstOrDefault(m => m.Name == "GetInt32" && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(int));
        _getInt64Method = methods.FirstOrDefault(m => m.Name == "GetInt64" && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(int));
        _getInt16Method = methods.FirstOrDefault(m => m.Name == "GetInt16" && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(int));
        _getByteMethod = methods.FirstOrDefault(m => m.Name == "GetByte" && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(int));
        _getBooleanMethod = methods.FirstOrDefault(m => m.Name == "GetBoolean" && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(int));
        _getStringMethod = methods.FirstOrDefault(m => m.Name == "GetString" && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(int));
        _getDateTimeMethod = methods.FirstOrDefault(m => m.Name == "GetDateTime" && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(int));
        _getDecimalMethod = methods.FirstOrDefault(m => m.Name == "GetDecimal" && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(int));
        _getDoubleMethod = methods.FirstOrDefault(m => m.Name == "GetDouble" && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(int));
        _getFloatMethod = methods.FirstOrDefault(m => m.Name == "GetFloat" && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(int));
        _getGuidMethod = methods.FirstOrDefault(m => m.Name == "GetGuid" && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(int));
        
        _getValueMethod = methods.FirstOrDefault(m => m.Name == "GetValue" && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(int))
            ?? throw new InvalidOperationException("Could not find GetValue method on IDataRecord");
        _isDbNullMethod = methods.FirstOrDefault(m => m.Name == "IsDBNull" && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(int))
            ?? throw new InvalidOperationException("Could not find IsDBNull method on IDataRecord");
    }

    public DynamicResultReader(string[] columnNames)
    {
        _columnNames = columnNames;
        _readFunc = BuildReadFunc();
    }

    private static Func<IDataReader, T> BuildReadFunc()
    {
        var type = typeof(T);
        var readerParam = Expression.Parameter(typeof(IDataReader), "reader");

        // Check if anonymous type (has compiler-generated name)
        var isAnonymous = type.Name.StartsWith("<>", StringComparison.Ordinal) ||
                         (type.Name.Contains("AnonymousType") && 
                          type.GetCustomAttributes(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), false).Length > 0);

        if (isAnonymous)
        {
            // Anonymous type: use constructor
            var ctor = type.GetConstructors()[0];
            var parameters = ctor.GetParameters();
            var args = new Expression[parameters.Length];
            
            for (int i = 0; i < parameters.Length; i++)
            {
                args[i] = BuildGetValue(readerParam, Expression.Constant(i), parameters[i].ParameterType);
            }
            
            var newExpr = Expression.New(ctor, args);
            return Expression.Lambda<Func<IDataReader, T>>(newExpr, readerParam).Compile();
        }
        else
        {
            // Named type: use property initializers
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite)
                .ToArray();
            
            var bindings = new List<MemberBinding>();
            for (int i = 0; i < properties.Length; i++)
            {
                var getValue = BuildGetValue(readerParam, Expression.Constant(i), properties[i].PropertyType);
                bindings.Add(Expression.Bind(properties[i], getValue));
            }
            
            var newExpr = Expression.MemberInit(Expression.New(type), bindings);
            return Expression.Lambda<Func<IDataReader, T>>(newExpr, readerParam).Compile();
        }
    }

    private static Expression BuildGetValue(Expression readerParam, Expression ordinalExpr, Type targetType)
    {
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        var isNullable = targetType != underlyingType || !targetType.IsValueType;

        // Get the appropriate cached reader method
        MethodInfo? method = underlyingType.Name switch
        {
            "Int32" => _getInt32Method,
            "Int64" => _getInt64Method,
            "Int16" => _getInt16Method,
            "Byte" => _getByteMethod,
            "Boolean" => _getBooleanMethod,
            "String" => _getStringMethod,
            "DateTime" => _getDateTimeMethod,
            "Decimal" => _getDecimalMethod,
            "Double" => _getDoubleMethod,
            "Single" => _getFloatMethod,
            "Guid" => _getGuidMethod,
            _ => null
        };

        Expression getValue;
        if (method != null)
        {
            getValue = Expression.Call(readerParam, method, ordinalExpr);
        }
        else
        {
            // Fallback to GetValue + Convert for unsupported types
            var value = Expression.Call(readerParam, _getValueMethod, ordinalExpr);
            getValue = Expression.Convert(value, underlyingType);
        }

        // Handle nullability
        if (isNullable)
        {
            var isDbNull = Expression.Call(readerParam, _isDbNullMethod, ordinalExpr);
            var defaultValue = Expression.Constant(null, targetType);
            var convertedValue = targetType.IsValueType ? Expression.Convert(getValue, targetType) : getValue;
            return Expression.Condition(isDbNull, defaultValue, convertedValue);
        }

        return getValue;
    }

    public T Read(IDataReader reader) => _readFunc(reader);

    public T Read(IDataReader reader, int[] ordinals)
    {
        // For dynamic readers, we don't optimize with pre-computed ordinals
        // Just use the regular Read method
        return _readFunc(reader);
    }

    public int[] GetOrdinals(IDataReader reader)
    {
        var ordinals = new int[_columnNames.Length];
        for (int i = 0; i < _columnNames.Length; i++)
        {
            ordinals[i] = reader.GetOrdinal(_columnNames[i]);
        }
        return ordinals;
    }
}
