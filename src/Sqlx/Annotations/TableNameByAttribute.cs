// -----------------------------------------------------------------------
// <copyright file="TableNameByAttribute.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#nullable enable

namespace Sqlx.Annotations
{
    /// <summary>
    /// Specifies a method to dynamically determine the table name at runtime.
    /// </summary>
    /// <remarks>
    /// <para>The specified method must be accessible from the repository class and return a string.</para>
    /// <para>This is useful for scenarios like:</para>
    /// <list type="bullet">
    /// <item><description>Multi-tenant databases with tenant-specific tables</description></item>
    /// <item><description>Sharded tables based on date or ID ranges</description></item>
    /// <item><description>Dynamic table routing based on business logic</description></item>
    /// </list>
    /// <para>Method signature: <c>string MethodName()</c> or <c>static string MethodName()</c></para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Example 1: Tenant-specific tables
    /// [SqlDefine(SqlDefineTypes.MySql)]
    /// [TableNameBy(nameof(GetTenantTableName))]
    /// public partial class UserRepository : ICrudRepository&lt;User, int&gt;
    /// {
    ///     private readonly string _tenantId;
    ///     
    ///     public UserRepository(DbConnection connection, string tenantId)
    ///     {
    ///         _tenantId = tenantId;
    ///     }
    ///     
    ///     private string GetTenantTableName()
    ///     {
    ///         return $"users_{_tenantId}";
    ///     }
    /// }
    ///
    /// // Example 2: Date-based sharding
    /// [SqlDefine(SqlDefineTypes.PostgreSql)]
    /// [TableNameBy(nameof(GetShardedTableName))]
    /// public partial class LogRepository : ICrudRepository&lt;Log, int&gt;
    /// {
    ///     private readonly DateTime _targetDate;
    ///     
    ///     public LogRepository(DbConnection connection, DateTime targetDate)
    ///     {
    ///         _targetDate = targetDate;
    ///     }
    ///     
    ///     private string GetShardedTableName()
    ///     {
    ///         return $"logs_{_targetDate:yyyyMM}";
    ///     }
    /// }
    ///
    /// // Example 3: Static method
    /// [SqlDefine(SqlDefineTypes.SqlServer)]
    /// [TableNameBy(nameof(GetTableName))]
    /// public partial class ProductRepository : ICrudRepository&lt;Product, int&gt;
    /// {
    ///     private static string GetTableName()
    ///     {
    ///         return Environment.GetEnvironmentVariable("PRODUCT_TABLE") ?? "products";
    ///     }
    /// }
    /// </code>
    /// </example>
    [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class TableNameByAttribute : System.Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TableNameByAttribute"/> class.
        /// </summary>
        /// <param name="methodName">The name of the method that returns the table name.</param>
        public TableNameByAttribute(string methodName)
        {
            MethodName = methodName;
        }

        /// <summary>
        /// Gets the name of the method that returns the table name.
        /// </summary>
        public string MethodName { get; }
    }
}
