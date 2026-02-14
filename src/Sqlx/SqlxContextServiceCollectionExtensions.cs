// -----------------------------------------------------------------------
// <copyright file="SqlxContextServiceCollectionExtensions.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#nullable enable

#if NET6_0_OR_GREATER && !NETSTANDARD

using System;
using Sqlx;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for registering SqlxContext with dependency injection.
    /// </summary>
    /// <remarks>
    /// These extension methods are only available when targeting .NET 6.0 or later (not .NET Standard).
    /// They require the Microsoft.Extensions.DependencyInjection.Abstractions package.
    /// </remarks>
    public static class SqlxContextServiceCollectionExtensions
    {
        /// <summary>
        /// Registers a SqlxContext with the service collection.
        /// </summary>
        /// <typeparam name="TContext">The type of the SqlxContext to register.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="lifetime">The service lifetime (default is Scoped).</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// // Register with default scoped lifetime
        /// services.AddSqlxContext&lt;AppDbContext&gt;();
        /// 
        /// // Register with transient lifetime
        /// services.AddSqlxContext&lt;AppDbContext&gt;(ServiceLifetime.Transient);
        /// </code>
        /// </example>
        public static IServiceCollection AddSqlxContext<TContext>(
            this IServiceCollection services,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
            where TContext : SqlxContext
        {
            services.Add(new ServiceDescriptor(typeof(TContext), typeof(TContext), lifetime));
            return services;
        }

        /// <summary>
        /// Registers a SqlxContext with the service collection using a factory function.
        /// </summary>
        /// <typeparam name="TContext">The type of the SqlxContext to register.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="implementationFactory">The factory function to create the context.</param>
        /// <param name="lifetime">The service lifetime (default is Scoped).</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// // Register with factory
        /// services.AddSqlxContext&lt;AppDbContext&gt;(sp =>
        /// {
        ///     var connection = sp.GetRequiredService&lt;DbConnection&gt;();
        ///     var users = sp.GetRequiredService&lt;UserRepository&gt;();
        ///     var orders = sp.GetRequiredService&lt;OrderRepository&gt;();
        ///     return new AppDbContext(connection, users, orders);
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddSqlxContext<TContext>(
            this IServiceCollection services,
            Func<IServiceProvider, TContext> implementationFactory,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
            where TContext : SqlxContext
        {
            services.Add(new ServiceDescriptor(typeof(TContext), implementationFactory, lifetime));
            return services;
        }
    }
}

#endif
