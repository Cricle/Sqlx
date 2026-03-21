// <copyright file="TableNameResolver.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx;

using System;
using System.Collections.Concurrent;
using System.Reflection;
using Sqlx.Annotations;

internal static class TableNameResolver
{
    private static readonly ConcurrentDictionary<Type, ResolverEntry> Cache = new();

    public static string Resolve(Type entityType)
    {
        if (entityType == null)
        {
            throw new ArgumentNullException(nameof(entityType));
        }

        return Cache.GetOrAdd(entityType, CreateEntry).Resolve();
    }

    private static ResolverEntry CreateEntry(Type entityType)
    {
        var attribute = entityType.GetCustomAttribute<TableNameAttribute>();
        if (attribute == null)
        {
            return new ResolverEntry(entityType.Name, null, null);
        }

        var fallbackTableName = !string.IsNullOrWhiteSpace(attribute.TableName)
            ? attribute.TableName
            : entityType.Name;

        if (string.IsNullOrWhiteSpace(attribute.Method))
        {
            return new ResolverEntry(fallbackTableName, null, null);
        }

        var method = entityType.GetMethod(
            attribute.Method,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
            binder: null,
            types: Type.EmptyTypes,
            modifiers: null);

        if (method == null || method.ReturnType != typeof(string))
        {
            return new ResolverEntry(fallbackTableName, null, null);
        }

        return new ResolverEntry(
            fallbackTableName,
            method.IsPublic ? (Func<string>)method.CreateDelegate(typeof(Func<string>)) : null,
            method.IsPublic ? null : method);
    }

    private sealed class ResolverEntry
    {
        private readonly string _fallbackTableName;
        private readonly Func<string>? _dynamicResolver;
        private readonly MethodInfo? _dynamicMethod;

        public ResolverEntry(string fallbackTableName, Func<string>? dynamicResolver, MethodInfo? dynamicMethod)
        {
            _fallbackTableName = fallbackTableName;
            _dynamicResolver = dynamicResolver;
            _dynamicMethod = dynamicMethod;
        }

        public string Resolve()
        {
            if (_dynamicResolver == null && _dynamicMethod == null)
            {
                return _fallbackTableName;
            }

            var dynamicTableName = _dynamicResolver?.Invoke()
                ?? _dynamicMethod?.Invoke(null, null) as string;

            return !string.IsNullOrWhiteSpace(dynamicTableName)
                ? dynamicTableName
                : _fallbackTableName;
        }
    }
}
