// <copyright file="OutputParameterAttribute.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx.Annotations;

using System;
using System.Data;

/// <summary>
/// Marks a method parameter as an output parameter for SQL execution.
/// </summary>
/// <remarks>
/// <para>
/// This attribute is used with repository methods to indicate that a parameter
/// should be registered as an output parameter in the database command.
/// After execution, the parameter value will be updated with the value returned
/// from the database.
/// </para>
/// <para>
/// <strong>Important:</strong> C# does not allow <c>ref</c> and <c>out</c> parameters
/// in async methods. Output parameters must be used with synchronous methods only.
/// </para>
/// <para>
/// Output parameters are commonly used with:
/// </para>
/// <list type="bullet">
/// <item><description>Stored procedures that return values</description></item>
/// <item><description>Statements that return generated IDs or computed values</description></item>
/// <item><description>Any SQL operation that needs to return data via parameters</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// [RepositoryFor(typeof(IUserRepository))]
/// [SqlDefine(SqlDefineTypes.SqlServer)]
/// [TableName("users")]
/// public partial class UserRepository
/// {
///     [SqlTemplate("EXEC GetUserId @name, @userId OUT")]
///     int GetUserId(string name, [OutputParameter(DbType.Int32)] out int userId);
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
public sealed class OutputParameterAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OutputParameterAttribute"/> class.
    /// </summary>
    /// <param name="dbType">The database type of the output parameter.</param>
    public OutputParameterAttribute(DbType dbType)
    {
        DbType = dbType;
    }

    /// <summary>
    /// Gets the database type of the output parameter.
    /// </summary>
    public DbType DbType { get; }

    /// <summary>
    /// Gets or sets the size of the output parameter.
    /// </summary>
    /// <remarks>
    /// This is optional and only needed for variable-length types like strings or binary data.
    /// If not specified, the database provider will use a default size.
    /// </remarks>
    public int Size { get; set; }
}
