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
        if (attribute == null) return new ResolverEntry(entityType.Name, null);

        var fallbackTableName = !string.IsNullOrWhiteSpace(attribute.TableName)
            ? attribute.TableName
            : entityType.Name;

        if (string.IsNullOrWhiteSpace(attribute.Method))
            return new ResolverEntry(fallbackTableName, null);

        var method = entityType.GetMethod(
            attribute.Method,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
            binder: null,
            types: Type.EmptyTypes,
            modifiers: null);

        if (method == null || method.ReturnType != typeof(string))
            return new ResolverEntry(fallbackTableName, null);

        var resolver = (Func<string>)method.CreateDelegate(typeof(Func<string>));
        return new ResolverEntry(fallbackTableName, resolver);
    }

    private sealed class ResolverEntry(string fallbackTableName, Func<string>? dynamicResolver)
    {
        public string Resolve()
        {
            if (dynamicResolver == null) return fallbackTableName;
            var name = dynamicResolver();
            return !string.IsNullOrWhiteSpace(name) ? name : fallbackTableName;
        }
    }
}
