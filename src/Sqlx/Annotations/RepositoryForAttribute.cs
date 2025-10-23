// -----------------------------------------------------------------------
// <copyright file="RepositoryForAttribute.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#nullable enable

namespace Sqlx.Annotations
{
    /// <summary>
    /// Marks a class as a repository for a specified service interface.
    /// </summary>
    /// <remarks>
    /// <para>⚠️ <strong>Best Practice</strong>: The <paramref name="serviceType"/> should be an interface type.</para>
    /// <para>The repository class should implement the specified interface.</para>
    /// <para><strong>Example:</strong></para>
    /// <code>
    /// public interface IUserRepository
    /// {
    ///     [Sqlx("SELECT * FROM users WHERE id = @id")]
    ///     Task&lt;User?&gt; GetByIdAsync(int id);
    /// }
    /// 
    /// [RepositoryFor(typeof(IUserRepository))]
    /// [SqlDefine(SqlDefineTypes.SQLite)]
    /// public partial class UserRepository : IUserRepository
    /// {
    /// }
    /// </code>
    /// </remarks>
    [System.AttributeUsage(System.AttributeTargets.Class,
        AllowMultiple = false, Inherited = false)]
    public sealed class RepositoryForAttribute : System.Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RepositoryForAttribute"/> class.
        /// </summary>
        /// <param name="serviceType">
        /// The service interface type that this repository implements.
        /// Should be an interface type for best design practices.
        /// </param>
        public RepositoryForAttribute(System.Type serviceType)
        {
            ServiceType = serviceType ?? throw new System.ArgumentNullException(nameof(serviceType));
        }

        /// <summary>
        /// Gets the service interface type.
        /// </summary>
        public System.Type ServiceType { get; }
    }
}
