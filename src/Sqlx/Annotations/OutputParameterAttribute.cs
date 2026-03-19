// <copyright file="OutputParameterAttribute.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx.Annotations;

using System;
using System.Data;

/// <summary>
/// Optionally marks a method parameter as an output parameter for SQL execution.
/// </summary>
/// <remarks>
/// <para>
/// This attribute is OPTIONAL. Sqlx automatically detects output parameters by checking
/// for <c>ref</c> and <c>out</c> keywords. The attribute is only needed if you want to:
/// </para>
/// <list type="bullet">
/// <item><description>Explicitly specify a DbType (otherwise auto-inferred from parameter type)</description></item>
/// <item><description>Set a Size for variable-length types like strings</description></item>
/// </list>
/// <para>
/// After execution, the parameter value will be updated with the value returned
/// from the database.
/// </para>
/// <para>
/// <strong>Important:</strong> C# does not allow <c>ref</c> and <c>out</c> parameters
/// in async methods. Output parameters must be used with synchronous methods only,
/// or use <c>OutputParameter&lt;T&gt;</c> wrapper for async methods.
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
/// [TableName("users")]
/// public partial class UserRepository(DbConnection connection, SqlDialect dialect)
/// {
///     // Attribute is optional - DbType is automatically inferred from int type
///     [SqlTemplate("EXEC GetUserId @name, @userId OUT")]
///     int GetUserId(string name, out int userId);
///     
///     // You can still use the attribute to specify DbType explicitly if needed
///     [SqlTemplate("EXEC GetUserIdExplicit @name, @userId OUT")]
///     int GetUserIdExplicit(string name, [OutputParameter(DbType.Int64)] out long userId);
///     
///     // Or use it to specify Size for variable-length types
///     [SqlTemplate("EXEC GetUserName @id, @name OUT")]
///     int GetUserName(int id, [OutputParameter(Size = 100)] out string name);
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
public sealed class OutputParameterAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OutputParameterAttribute"/> class
    /// with automatic DbType inference from the parameter type.
    /// </summary>
    public OutputParameterAttribute()
    {
        DbType = null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OutputParameterAttribute"/> class
    /// with an explicit DbType.
    /// </summary>
    /// <param name="dbType">The database type of the output parameter.</param>
    public OutputParameterAttribute(DbType dbType)
    {
        DbType = dbType;
    }

    /// <summary>
    /// Gets the database type of the output parameter, or null if it should be inferred.
    /// </summary>
    public DbType? DbType { get; }

    /// <summary>
    /// Gets or sets the size of the output parameter.
    /// </summary>
    /// <remarks>
    /// This is optional and only needed for variable-length types like strings or binary data.
    /// If not specified, the database provider will use a default size.
    /// </remarks>
    public int Size { get; set; }
}
