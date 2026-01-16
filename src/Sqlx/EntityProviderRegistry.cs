// -----------------------------------------------------------------------
// <copyright file="EntityProviderRegistry.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Concurrent;

namespace Sqlx
{
    /// <summary>
    /// Global registry for IEntityProvider instances.
    /// Used by subqueries to get entity metadata without reflection.
    /// </summary>
    public static class EntityProviderRegistry
    {
        private static readonly ConcurrentDictionary<Type, IEntityProvider> _providers = new();

        /// <summary>
        /// Registers an entity provider for a type.
        /// Called automatically by source-generated code.
        /// </summary>
        public static void Register(Type type, IEntityProvider provider)
        {
            _providers.TryAdd(type, provider);
        }

        /// <summary>
        /// Gets the entity provider for a type, or null if not registered.
        /// </summary>
        public static IEntityProvider? Get(Type type)
        {
            return _providers.TryGetValue(type, out var provider) ? provider : null;
        }
    }
}
