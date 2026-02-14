// -----------------------------------------------------------------------
// <copyright file="SqlxContextAttribute.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#nullable enable

namespace Sqlx.Annotations
{
    /// <summary>
    /// Marks a class as a SqlxContext for source generation.
    /// </summary>
    /// <remarks>
    /// <para>Use this attribute on partial classes that inherit from SqlxContext to enable automatic generation of:</para>
    /// <list type="bullet">
    /// <item><description>Repository properties with lazy initialization</description></item>
    /// <item><description>Transaction propagation logic</description></item>
    /// <item><description>DI-friendly constructor (if not provided)</description></item>
    /// </list>
    /// <para>Use [IncludeRepository(typeof(RepositoryType))] attributes to specify which repositories to include.</para>
    /// <para>Use [SqlDefine] attribute to specify the database dialect.</para>
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
    /// // - Transaction propagation overrides
    /// </code>
    /// </example>
    [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class SqlxContextAttribute : System.Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SqlxContextAttribute"/> class.
        /// </summary>
        public SqlxContextAttribute()
        {
        }
    }
}
