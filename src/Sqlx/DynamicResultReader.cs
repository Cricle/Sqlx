// <copyright file="DynamicResultReader.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx;

using System;
using System.Collections.Concurrent;
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
    // Static cached delegates - generated once per type
    private static readonly Func<IDataReader, T> _readFunc;
    private static readonly Func<IDataReader, int[], T> _readWithOrdinalsFunc;
    private static readonly string[] _propertyNames;

    private readonly string[] _columnNames;

    static DynamicResultReader()
    {
        var type = typeof(T);
        
        // Check if anonymous type
        var isAnonymous = type.Name.StartsWith("<>", StringComparison.Ordinal) ||
                         (type.Name.Contains("AnonymousType") && 
                          type.GetCustomAttributes(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), false).Length > 0);

        if (isAnonymous)
        {
            var ctor = type.GetConstructors()[0];
            var parameters = ctor.GetParameters();
            _propertyNames = parameters.Select(p => p.Name!).ToArray();
            (_readFunc, _readWithOrdinalsFunc) = BuildAnonymousReadFuncs(ctor, parameters);
        }
        else
        {
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite)
                .ToArray();
            _propertyNames = properties.Select(p => p.Name).ToArray();
            (_readFunc, _readWithOrdinalsFunc) = BuildNamedTypeReadFuncs(properties);
        }
    }

    public DynamicResultReader(string[] columnNames)
    {
        _columnNames = columnNames;
    }

    /// <summary>
    /// Creates a DynamicResultReader using the type's property names as column names.
    /// </summary>
    public DynamicResultReader() : this(_propertyNames)
    {
    }

    private static (Func<IDataReader, T>, Func<IDataReader, int[], T>) BuildAnonymousReadFuncs(
        ConstructorInfo ctor, ParameterInfo[] parameters)
    {
        // Build read func without ordinals (uses index directly)
        var readerParam1 = Expression.Parameter(typeof(IDataReader), "reader");
        var args1 = new Expression[parameters.Length];
        for (int i = 0; i < parameters.Length; i++)
        {
            args1[i] = BuildGetValue(readerParam1, Expression.Constant(i), parameters[i].ParameterType);
        }
        var newExpr1 = Expression.New(ctor, args1);
        var readFunc = Expression.Lambda<Func<IDataReader, T>>(newExpr1, readerParam1).Compile();

        // Build read func with ordinals
        var readerParam2 = Expression.Parameter(typeof(IDataReader), "reader");
        var ordinalsParam = Expression.Parameter(typeof(int[]), "ordinals");
        var args2 = new Expression[parameters.Length];
        for (int i = 0; i < parameters.Length; i++)
        {
            var ordinalAccess = Expression.ArrayIndex(ordinalsParam, Expression.Constant(i));
            args2[i] = BuildGetValue(readerParam2, ordinalAccess, parameters[i].ParameterType);
        }
        var newExpr2 = Expression.New(ctor, args2);
        var readWithOrdinalsFunc = Expression.Lambda<Func<IDataReader, int[], T>>(newExpr2, readerParam2, ordinalsParam).Compile();

        return (readFunc, readWithOrdinalsFunc);
    }

    private static (Func<IDataReader, T>, Func<IDataReader, int[], T>) BuildNamedTypeReadFuncs(PropertyInfo[] properties)
    {
        var type = typeof(T);

        // Build read func without ordinals (uses index directly)
        var readerParam1 = Expression.Parameter(typeof(IDataReader), "reader");
        var bindings1 = new List<MemberBinding>();
        for (int i = 0; i < properties.Length; i++)
        {
            var getValue = BuildGetValue(readerParam1, Expression.Constant(i), properties[i].PropertyType);
            bindings1.Add(Expression.Bind(properties[i], getValue));
        }
        var newExpr1 = Expression.MemberInit(Expression.New(type), bindings1);
        var readFunc = Expression.Lambda<Func<IDataReader, T>>(newExpr1, readerParam1).Compile();

        // Build read func with ordinals
        var readerParam2 = Expression.Parameter(typeof(IDataReader), "reader");
        var ordinalsParam = Expression.Parameter(typeof(int[]), "ordinals");
        var bindings2 = new List<MemberBinding>();
        for (int i = 0; i < properties.Length; i++)
        {
            var ordinalAccess = Expression.ArrayIndex(ordinalsParam, Expression.Constant(i));
            var getValue = BuildGetValue(readerParam2, ordinalAccess, properties[i].PropertyType);
            bindings2.Add(Expression.Bind(properties[i], getValue));
        }
        var newExpr2 = Expression.MemberInit(Expression.New(type), bindings2);
        var readWithOrdinalsFunc = Expression.Lambda<Func<IDataReader, int[], T>>(newExpr2, readerParam2, ordinalsParam).Compile();

        return (readFunc, readWithOrdinalsFunc);
    }

    private static Expression BuildGetValue(Expression readerParam, Expression ordinalExpr, Type targetType)
    {
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        var isNullable = targetType != underlyingType || !targetType.IsValueType;

        // Get the appropriate reader method
        var methodName = underlyingType.Name switch
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

        Expression getValue;
        if (methodName != null)
        {
            var method = typeof(IDataRecord).GetMethod(methodName, new[] { typeof(int) });
            getValue = Expression.Call(readerParam, method!, ordinalExpr);
        }
        else
        {
            // Fallback to GetValue + Convert for unsupported types
            var getValueMethod = typeof(IDataRecord).GetMethod("GetValue", new[] { typeof(int) })!;
            var value = Expression.Call(readerParam, getValueMethod, ordinalExpr);
            getValue = Expression.Convert(value, underlyingType);
        }

        // Handle nullability
        if (isNullable)
        {
            var isDbNullMethod = typeof(IDataRecord).GetMethod("IsDBNull", new[] { typeof(int) })!;
            var isDbNull = Expression.Call(readerParam, isDbNullMethod, ordinalExpr);
            var defaultValue = Expression.Default(targetType);
            var convertedValue = targetType.IsValueType && targetType != underlyingType 
                ? Expression.Convert(getValue, targetType) 
                : getValue;
            return Expression.Condition(isDbNull, defaultValue, convertedValue);
        }

        return getValue;
    }

    public T Read(IDataReader reader) => _readFunc(reader);

    public T Read(IDataReader reader, int[] ordinals) => _readWithOrdinalsFunc(reader, ordinals);

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
