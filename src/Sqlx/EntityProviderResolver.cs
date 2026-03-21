// <copyright file="EntityProviderResolver.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx;

using System;
using System.Collections.Concurrent;

internal static class EntityProviderResolver
{
    private static readonly ConcurrentDictionary<Type, IEntityProvider> DynamicProviders = new();

    public static IEntityProvider ResolveOrCreate<T>(IEntityProvider? provider = null)
    {
        return provider
            ?? SqlQuery<T>.EntityProvider
            ?? EntityProviderRegistry.Get(typeof(T))
            ?? DynamicProviderCache<T>.Instance;
    }

    public static IEntityProvider ResolveOrCreate(Type entityType, IEntityProvider? provider = null)
    {
        if (entityType == null)
        {
            throw new ArgumentNullException(nameof(entityType));
        }

        return provider
            ?? EntityProviderRegistry.Get(entityType)
            ?? DynamicProviders.GetOrAdd(entityType, CreateDynamicProvider);
    }

    public static void EnsureProviderMatches(Type entityType, IEntityProvider? provider, string paramName = "provider")
    {
        if (provider == null)
        {
            return;
        }

        if (!ReferenceEquals(provider.EntityType, entityType))
        {
            throw new ArgumentException(
                $"EntityProvider type mismatch. Expected '{entityType.FullName}', but got '{provider.EntityType.FullName}'.",
                paramName);
        }
    }

    private static IEntityProvider CreateDynamicProvider(Type entityType)
    {
        var providerType = typeof(DynamicEntityProvider<>).MakeGenericType(entityType);
        return (IEntityProvider)Activator.CreateInstance(providerType)!;
    }

    private static class DynamicProviderCache<T>
    {
        public static readonly IEntityProvider Instance = new DynamicEntityProvider<T>();
    }
}
