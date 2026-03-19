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
    /// <para>Pass <c>SqlDialect</c> via repository constructor for runtime dialect selection.</para>
    /// <para>Use [TableName] attribute to specify the table name.</para>
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
    /// [TableName("users")]
    /// [RepositoryFor(typeof(IUserRepository))]
    /// public partial class UserRepository(DbConnection connection, SqlDialect dialect) : IUserRepository { }
    ///
    /// // Example 2: Primary constructor with runtime dialect
    /// [RepositoryFor(typeof(IUserRepository), TableName = "users")]
    /// public partial class UserRepository(DbConnection connection, SqlDialect dialect) : IUserRepository { }
    ///
    /// // Example 3: Multi-dialect support with unified interface
    /// public interface IUserRepositoryBase : ICrudRepository&lt;User, int&gt;
    /// {
    ///     [SqlTemplate("SELECT * FROM {{table}} WHERE id = @id")]
    ///     new Task&lt;User?&gt; GetByIdAsync(int id, CancellationToken ct);
    /// }
    ///
    /// [RepositoryFor(typeof(IUserRepositoryBase), TableName = "users")]
    /// public partial class PostgreSQLUserRepository(DbConnection connection, SqlDialect dialect) : IUserRepositoryBase { }
    ///
    /// [RepositoryFor(typeof(IUserRepositoryBase), TableName = "users")]
    /// public partial class MySQLUserRepository(DbConnection connection, SqlDialect dialect) : IUserRepositoryBase { }
    ///
    /// // Example 4: Multi-line SQL templates
    /// public interface IProductRepository : ICrudRepository&lt;Product, long&gt;
    /// {
    ///     [SqlTemplate(@"
    ///         SELECT {{columns}}
    ///         FROM {{table}}
    ///         WHERE category = @category
    ///           AND price BETWEEN @minPrice AND @maxPrice
    ///         ORDER BY name
    ///     ")]
    ///     Task&lt;List&lt;Product&gt;&gt; SearchAsync(string category, decimal minPrice, decimal maxPrice);
    /// }
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
        
        /// <summary>
        /// Gets or sets the database table name.
        /// If not specified, the table name will be inferred from the entity type or [TableName] attribute.
        /// </summary>
        public string? TableName { get; set; }
        
    }
}
