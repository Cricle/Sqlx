// -----------------------------------------------------------------------
// <copyright file="SqlxDebuggerAttribute.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#nullable enable

namespace Sqlx.Annotations
{
    /// <summary>
    /// Marks a method or class to generate SQL template getter methods for debugging and testing.
    /// When applied to a method, generates a Get[MethodName]Sql method that returns the processed SQL template
    /// without executing the query.
    /// When applied to a class, generates SQL getter methods for all repository methods in that class.
    /// </summary>
    /// <example>
    /// <code>
    /// // On method:
    /// [SqlxDebugger]
    /// [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    /// Task&lt;User?&gt; GetByIdAsync(long id);
    /// 
    /// // On class:
    /// [SqlxDebugger]
    /// [RepositoryFor&lt;IUserRepository&gt;]
    /// public partial class UserRepository : IUserRepository { }
    /// 
    /// // Generated method:
    /// string GetGetByIdAsyncSql(long id, SqlDefine? dialect = null);
    /// </code>
    /// </example>
    [System.AttributeUsage(System.AttributeTargets.Method | System.AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class SqlxDebuggerAttribute : System.Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SqlxDebuggerAttribute"/> class.
        /// </summary>
        public SqlxDebuggerAttribute()
        {
        }
    }
}
