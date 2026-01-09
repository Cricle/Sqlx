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
    /// <para>Supports both generic and non-generic syntax.</para>
    /// <para><strong>Best Practice:</strong> Define a custom interface that extends ICrudRepository for better maintainability.</para>
    /// <para><strong>Supported Interface Types:</strong></para>
    /// <list type="bullet">
    /// <item><description>Custom interfaces (recommended)</description></item>
    /// <item><description>ICrudRepository&lt;TEntity, TKey&gt; (full CRUD operations)</description></item>
    /// <item><description>IReadOnlyRepository&lt;TEntity&gt; (read-only operations)</description></item>
    /// <item><description>Any interface with [SqlTemplate] methods</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Example 1: Custom interface (recommended)
    /// public interface IUserRepository : ICrudRepository&lt;User, int&gt; { }
    ///
    /// [RepositoryFor(typeof(IUserRepository))]  // Non-generic syntax
    /// public partial class UserRepository : IUserRepository { }
    ///
    /// [RepositoryFor&lt;IUserRepository&gt;]     // Generic syntax (cleaner)
    /// public partial class UserRepository : IUserRepository { }
    ///
    /// // Example 2: Multi-dialect support with unified interface
    /// public interface IUserRepositoryBase : ICrudRepository&lt;User, int&gt;
    /// {
    ///     [SqlTemplate("SELECT * FROM {{table}} WHERE id = @id")]
    ///     new Task&lt;User?&gt; GetByIdAsync(int id, CancellationToken ct);
    /// }
    ///
    /// [RepositoryFor(typeof(IUserRepositoryBase), Dialect = SqlDefineTypes.PostgreSql, TableName = "users")]
    /// public partial class PostgreSQLUserRepository : IUserRepositoryBase { }
    ///
    /// [RepositoryFor(typeof(IUserRepositoryBase), Dialect = SqlDefineTypes.MySql, TableName = "users")]
    /// public partial class MySQLUserRepository : IUserRepositoryBase { }
    /// </code>
    /// </example>
    [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
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

        /// <summary>
        /// Gets or sets the database dialect for this repository.
        /// Used for multi-dialect support with SQL template placeholders.
        /// </summary>
        public SqlDefineTypes Dialect { get; set; } = SqlDefineTypes.SQLite;

        /// <summary>
        /// Gets or sets the table name for this repository.
        /// Used for {{table}} placeholder replacement in SQL templates.
        /// If not specified, the table name is inferred from the entity type.
        /// </summary>
        public string? TableName { get; set; }
    }

    /// <summary>
    /// Generic version of RepositoryForAttribute for better type safety and cleaner syntax (C# 11+).
    /// </summary>
    /// <typeparam name="TService">Service interface type (must be an interface).</typeparam>
    /// <remarks>
    /// <para>This generic version provides compile-time type safety and cleaner syntax.</para>
    /// <para>TService must be an interface type with [SqlTemplate] methods.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Custom interface
    /// [RepositoryFor&lt;IUserRepository&gt;]
    /// public partial class UserRepository : IUserRepository { }
    ///
    /// // Direct ICrudRepository usage
    /// [RepositoryFor&lt;ICrudRepository&lt;User, int&gt;&gt;]
    /// public partial class UserRepository : ICrudRepository&lt;User, int&gt; { }
    ///
    /// // Multi-dialect support
    /// [RepositoryFor&lt;IUserRepositoryBase&gt;(Dialect = SqlDefineTypes.PostgreSql, TableName = "users")]
    /// public partial class PostgreSQLUserRepository : IUserRepositoryBase { }
    /// </code>
    /// </example>
    [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class RepositoryForAttribute<TService> : System.Attribute
        where TService : class
    {
        /// <summary>Gets the service interface or entity type.</summary>
        public System.Type ServiceType => typeof(TService);

        /// <summary>
        /// Gets or sets the database dialect for this repository.
        /// Used for multi-dialect support with SQL template placeholders.
        /// </summary>
        public SqlDefineTypes Dialect { get; set; } = SqlDefineTypes.SQLite;

        /// <summary>
        /// Gets or sets the table name for this repository.
        /// Used for {{table}} placeholder replacement in SQL templates.
        /// If not specified, the table name is inferred from the entity type.
        /// </summary>
        public string? TableName { get; set; }
    }
}
