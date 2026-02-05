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
    private static readonly int _propertyCount;

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
            _propertyCount = parameters.Length;
            (_readFunc, _readWithOrdinalsFunc) = BuildAnonymousReadFuncs(ctor, parameters);
        }
        else
        {
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite)
                .ToArray();
            _propertyNames = properties.Select(p => p.Name).ToArray();
            _propertyCount = properties.Length;
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

    public int PropertyCount => _propertyCount;

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
        // Use the new high-performance type converter
        return TypeConverter.BuildConversion(readerParam, ordinalExpr, targetType);
    }

    public T Read(IDataReader reader) => _readFunc(reader);

    public T Read(IDataReader reader, ReadOnlySpan<int> ordinals)
    {
        // Convert span to array for the delegate
        var ordinalsArray = ordinals.ToArray();
        return _readWithOrdinalsFunc(reader, ordinalsArray);
    }

    public void GetOrdinals(IDataReader reader, Span<int> ordinals)
    {
        for (int i = 0; i < _columnNames.Length && i < ordinals.Length; i++)
        {
            ordinals[i] = reader.GetOrdinal(_columnNames[i]);
        }
    }
}
