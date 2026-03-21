// <copyright file="PlaceholderContext.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx;

using System;
using System.Collections.Generic;
#if NET5_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

/// <summary>
/// Provides context information for placeholder processing in SQL templates.
/// </summary>
/// <remarks>
/// <para>
/// PlaceholderContext contains all the metadata needed by placeholder handlers to generate
/// dialect-specific SQL fragments. It is typically created once per entity type and reused
/// across multiple SQL template preparations.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var context = new PlaceholderContext(
///     dialect: SqlDefine.SQLite,
///     tableName: "users",
///     columns: UserEntityProvider.Default.Columns);
/// </code>
/// </example>
public sealed class PlaceholderContext
{
    /// <summary>
    /// Creates a placeholder context from an entity type using its registered or dynamic metadata.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="dialect">The SQL dialect for database-specific SQL generation.</param>
    /// <param name="entityProvider">Optional explicit entity provider. If omitted, Sqlx uses cached, registered, or dynamic metadata.</param>
    /// <param name="varProvider">Optional variable provider function for {{var}} placeholder support.</param>
    /// <param name="instance">Optional repository instance for variable provider invocation.</param>
    /// <returns>A placeholder context initialized from the entity metadata.</returns>
    public static PlaceholderContext Create<
#if NET5_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
        T>(
        SqlDialect dialect,
        IEntityProvider? entityProvider = null,
        Func<object, string, string>? varProvider = null,
        object? instance = null)
    {
        if (dialect == null)
        {
            throw new ArgumentNullException(nameof(dialect));
        }

        var provider = EntityProviderResolver.ResolveOrCreate<T>(entityProvider);
        EntityProviderResolver.EnsureProviderMatches(typeof(T), provider, nameof(entityProvider));

        return new PlaceholderContext(
            dialect,
            TableNameResolver.Resolve(typeof(T)),
            provider.Columns,
            varProvider,
            instance);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaceholderContext"/> class.
    /// </summary>
    /// <param name="dialect">The SQL dialect for database-specific SQL generation.</param>
    /// <param name="tableName">The database table name.</param>
    /// <param name="columns">The column metadata for the entity.</param>
    /// <exception cref="ArgumentNullException">Thrown when dialect or columns is null.</exception>
    /// <exception cref="ArgumentException">Thrown when tableName is null, empty, or whitespace.</exception>
    public PlaceholderContext(SqlDialect dialect, string tableName, IReadOnlyList<ColumnMeta> columns)
        : this(dialect, tableName, columns, null, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaceholderContext"/> class with optional VarProvider support.
    /// </summary>
    /// <param name="dialect">The SQL dialect for database-specific SQL generation.</param>
    /// <param name="tableName">The database table name.</param>
    /// <param name="columns">The column metadata for the entity.</param>
    /// <param name="varProvider">Optional variable provider function for {{var}} placeholder support.</param>
    /// <param name="instance">Optional repository instance for variable provider invocation.</param>
    /// <exception cref="ArgumentNullException">Thrown when dialect or columns is null.</exception>
    /// <exception cref="ArgumentException">Thrown when tableName is null, empty, or whitespace.</exception>
    public PlaceholderContext(
        SqlDialect dialect,
        string tableName,
        IReadOnlyList<ColumnMeta> columns,
        Func<object, string, string>? varProvider,
        object? instance)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            throw new ArgumentException("Table name cannot be null, empty, or whitespace.", nameof(tableName));

        Dialect = dialect ?? throw new ArgumentNullException(nameof(dialect));
        TableName = tableName;
        Columns = columns ?? throw new ArgumentNullException(nameof(columns));
        VarProvider = varProvider;
        Instance = instance;
    }

    /// <summary>
    /// Gets the SQL dialect used for database-specific SQL generation.
    /// </summary>
    public SqlDialect Dialect { get; }

    /// <summary>
    /// Gets the database table name.
    /// </summary>
    public string TableName { get; }

    /// <summary>
    /// Gets the column metadata for the entity.
    /// </summary>
    public IReadOnlyList<ColumnMeta> Columns { get; }

    /// <summary>
    /// Gets the variable provider function for {{var}} placeholder support.
    /// </summary>
    /// <remarks>
    /// This function is called by VarPlaceholderHandler to resolve variable values at runtime.
    /// The function signature is: (object instance, string variableName) => string value.
    /// </remarks>
    public Func<object, string, string>? VarProvider { get; }

    /// <summary>
    /// Gets the repository instance for variable provider invocation.
    /// </summary>
    /// <remarks>
    /// This instance is passed to VarProvider when resolving {{var}} placeholders.
    /// It typically contains the repository with [SqlxVar] methods.
    /// </remarks>
    public object? Instance { get; }
}
