// -----------------------------------------------------------------------
// <copyright file="RepositoryForAttribute.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#nullable enable

namespace Sqlx.Annotations
{
    /// <summary>
    /// Marks a class as repository for a specified service interface.
    /// </summary>
    /// <remarks>
    /// Best practice: Use interface type. Repository class should implement the specified interface.
    /// Supports both generic and non-generic syntax.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Non-generic syntax
    /// [RepositoryFor(typeof(IUserRepository))]
    /// public partial class UserRepository : IUserRepository { }
    ///
    /// // Generic syntax (recommended)
    /// [RepositoryFor&lt;IUserRepository&gt;]
    /// public partial class UserRepository : IUserRepository { }
    ///
    /// // For ICrudRepository
    /// [RepositoryFor&lt;User&gt;]
    /// public partial class UserRepository : ICrudRepository&lt;User, int&gt; { }
    /// </code>
    /// </example>
    [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class RepositoryForAttribute : System.Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RepositoryForAttribute"/> class.
        /// </summary>
        /// <param name="serviceType">Service interface type that this repository implements.</param>
        public RepositoryForAttribute(System.Type serviceType)
        {
            ServiceType = serviceType ?? throw new System.ArgumentNullException(nameof(serviceType));
        }

        /// <summary>Gets the service interface type.</summary>
        public System.Type ServiceType { get; }
    }

    /// <summary>
    /// Generic version of RepositoryForAttribute for better type safety and cleaner syntax.
    /// </summary>
    /// <typeparam name="TService">Service interface or entity type.</typeparam>
    /// <example>
    /// <code>
    /// // For interface
    /// [RepositoryFor&lt;IUserRepository&gt;]
    /// public partial class UserRepository : IUserRepository { }
    ///
    /// // For entity (will implement ICrudRepository)
    /// [RepositoryFor&lt;User&gt;]
    /// public partial class UserRepository : ICrudRepository&lt;User, int&gt; { }
    /// </code>
    /// </example>
    [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class RepositoryForAttribute<TService> : System.Attribute
        where TService : class
    {
        /// <summary>Gets the service interface or entity type.</summary>
        public System.Type ServiceType => typeof(TService);
    }
}
