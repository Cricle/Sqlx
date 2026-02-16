// -----------------------------------------------------------------------
// <copyright file="SqlxContextServiceCollectionExtensions.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#nullable enable

#if NET6_0_OR_GREATER

using System;
using Sqlx;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for registering SqlxContext with dependency injection.
    /// </summary>
    /// <remarks>
    /// These extension methods are available for .NET 6.0+ and .NET Standard 2.1.
    /// They require the Microsoft.Extensions.DependencyInjection.Abstractions package.
    /// </remarks>
    public static class SqlxContextServiceCollectionExtensions
    {
        /// <summary>
        /// Registers a SqlxContext with the service collection using a factory function that provides SqlxContextOptions.
        /// </summary>
        /// <typeparam name="TContext">The type of the SqlxContext to register.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="implementationFactory">The factory function to create the context with options.</param>
        /// <param name="lifetime">The service lifetime (default is Scoped).</param>
        /// <returns>The service collection for chaining.</returns>
        /// <example>
        /// <code>
        /// // Register with options
        /// services.AddSqlxContext&lt;AppDbContext&gt;((sp, options) =>
        /// {
        ///     var connection = sp.GetRequiredService&lt;DbConnection&gt;();
        ///     var logger = sp.GetRequiredService&lt;ILogger&lt;AppDbContext&gt;&gt;();
        ///     options.Logger = logger;
        ///     options.EnableRetry = true;
        ///     return new AppDbContext(connection, options, sp);
        /// });
        /// </code>
        /// </example>
        public static IServiceCollection AddSqlxContext<TContext>(
            this IServiceCollection services,
            Func<IServiceProvider, SqlxContextOptions, TContext> implementationFactory,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
            where TContext : SqlxContext
        {
            services.Add(new ServiceDescriptor(typeof(TContext), sp =>
            {
                var options = new SqlxContextOptions();
                return implementationFactory(sp, options);
            }, lifetime));
            return services;
        }
    }
}

#endif
