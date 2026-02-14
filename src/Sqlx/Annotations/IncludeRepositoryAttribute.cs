// -----------------------------------------------------------------------
// <copyright file="IncludeRepositoryAttribute.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#nullable enable

using System;

namespace Sqlx.Annotations
{
    /// <summary>
    /// Specifies a repository implementation to include in a SqlxContext.
    /// The generator will extract the entity type from the repository's interface.
    /// </summary>
    /// <remarks>
    /// <para>Use this attribute on classes marked with [SqlxContext] to specify which repositories to include.</para>
    /// <para>The source generator will:</para>
    /// <list type="bullet">
    /// <item><description>Extract the entity type from the repository's [RepositoryFor] interface</description></item>
    /// <item><description>Generate a property for the repository (e.g., User → Users)</description></item>
    /// <item><description>Generate transaction propagation logic</description></item>
    /// <item><description>Generate DI-friendly constructor if not provided</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Define repositories
    /// [RepositoryFor(typeof(IUserRepository))]
    /// [TableName("users")]
    /// public partial class UserRepository { }
    /// 
    /// [RepositoryFor(typeof(IOrderRepository))]
    /// [TableName("orders")]
    /// public partial class OrderRepository { }
    /// 
    /// // Define context with repository specifications
    /// [SqlxContext]
    /// [SqlDefine(SqlDefineTypes.SQLite)]
    /// [IncludeRepository(typeof(UserRepository))]
    /// [IncludeRepository(typeof(OrderRepository))]
    /// public partial class AppDbContext : SqlxContext
    /// {
    ///     public AppDbContext(DbConnection connection) : base(connection) { }
    /// }
    /// 
    /// // Generated code will include:
    /// // - public UserRepository Users { get; }
    /// // - public OrderRepository Orders { get; }
    /// 
    /// // Usage:
    /// await using var context = new AppDbContext(connection);
    /// var user = await context.Users.GetByIdAsync(1);
    /// var orders = await context.Orders.GetWhereAsync(o => o.UserId == user.Id);
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class IncludeRepositoryAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IncludeRepositoryAttribute"/> class.
        /// </summary>
        /// <param name="repositoryType">The repository implementation type (e.g., typeof(UserRepository)).</param>
        /// <exception cref="ArgumentNullException">Thrown when repositoryType is null.</exception>
        public IncludeRepositoryAttribute(Type repositoryType)
        {
            RepositoryType = repositoryType ?? throw new ArgumentNullException(nameof(repositoryType));
        }

        /// <summary>
        /// Gets the repository implementation type to include in the context.
        /// </summary>
        public Type RepositoryType { get; }
    }
}
