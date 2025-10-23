namespace Sqlx;

using System;

/// <summary>
/// Marks parameter as dynamic SQL - value is directly concatenated into SQL string (not parameterized).
/// </summary>
/// <remarks>
/// WARNING: Dynamic SQL bypasses parameterization and has SQL injection risk!
/// Only use for: multi-tenant table names, sharding suffixes, dynamic ORDER BY.
/// Always validate: format, length, character set, whitelist.
/// </remarks>
/// <example>
/// <code>
/// [Sqlx("SELECT {{columns}} FROM {{@tableName}} WHERE id = @id")]
/// Task&lt;User?&gt; GetUserAsync([DynamicSql] string tableName, int id);
/// 
/// // Must validate at call site
/// if (!allowedTables.Contains(tableName))
///     throw new ArgumentException("Invalid table");
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
public sealed class DynamicSqlAttribute : Attribute
{
    /// <summary>Dynamic SQL parameter type - different validation rules apply.</summary>
    public DynamicSqlType Type { get; set; } = DynamicSqlType.Identifier;
}

/// <summary>Dynamic SQL parameter type.</summary>
public enum DynamicSqlType
{
    /// <summary>
    /// Identifier (table/column name) - strictest validation.
    /// Rules: alphanumeric + underscore, must start with letter/underscore, 1-128 chars, no SQL keywords.
    /// </summary>
    Identifier = 0,

    /// <summary>
    /// SQL fragment (WHERE/JOIN/ORDER BY clause) - medium validation.
    /// Rules: no DDL (DROP/TRUNCATE/ALTER/CREATE), no dangerous functions (EXEC/xp_/sp_), no comments (--, /*), 1-4096 chars.
    /// </summary>
    Fragment = 1,

    /// <summary>
    /// Table part (prefix/suffix) - strict validation.
    /// Rules: alphanumeric only, no underscore/space/special chars, 1-64 chars.
    /// </summary>
    TablePart = 2
}
