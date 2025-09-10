// -----------------------------------------------------------------------
// <copyright file="SqlDefineExtensions.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

namespace Sqlx.Core;

/// <summary>
/// Extension methods for SqlDefine to provide additional functionality.
/// </summary>
internal static class SqlDefineExtensions
{
    /// <summary>
    /// Wraps multiple column names with the appropriate database-specific delimiters.
    /// </summary>
    /// <param name="sqlDefine">The SQL definition.</param>
    /// <param name="columnNames">The column names to wrap.</param>
    /// <returns>An array of wrapped column names.</returns>
    public static string[] WrapColumns(this SqlDefine sqlDefine, params string[] columnNames)
    {
        if (columnNames == null || columnNames.Length == 0)
            return Array.Empty<string>();

        return columnNames.Select(sqlDefine.WrapColumn).ToArray();
    }

    /// <summary>
    /// Creates a comma-separated list of wrapped column names.
    /// </summary>
    /// <param name="sqlDefine">The SQL definition.</param>
    /// <param name="columnNames">The column names to wrap and join.</param>
    /// <returns>A comma-separated string of wrapped column names.</returns>
    public static string WrapAndJoinColumns(this SqlDefine sqlDefine, params string[] columnNames)
    {
        return string.Join(", ", sqlDefine.WrapColumns(columnNames));
    }

    /// <summary>
    /// Creates a comma-separated list of wrapped column names from a collection.
    /// </summary>
    /// <param name="sqlDefine">The SQL definition.</param>
    /// <param name="columnNames">The collection of column names to wrap and join.</param>
    /// <returns>A comma-separated string of wrapped column names.</returns>
    public static string WrapAndJoinColumns(this SqlDefine sqlDefine, IEnumerable<string>? columnNames)
    {
        if (columnNames == null)
            return string.Empty;

        return string.Join(", ", columnNames.Select(sqlDefine.WrapColumn));
    }

    /// <summary>
    /// Creates a parameter placeholder with the appropriate prefix.
    /// </summary>
    /// <param name="sqlDefine">The SQL definition.</param>
    /// <param name="parameterName">The parameter name (without prefix).</param>
    /// <returns>The parameter placeholder with prefix.</returns>
    public static string CreateParameter(this SqlDefine sqlDefine, string parameterName)
    {
        if (string.IsNullOrEmpty(parameterName))
            return sqlDefine.ParameterPrefix;

        // Handle special case for SQLite's unique identifier
        var prefix = sqlDefine.ParameterPrefix == "@sqlite" ? "@" : sqlDefine.ParameterPrefix;
        return $"{prefix}{parameterName}";
    }

    /// <summary>
    /// Creates multiple parameter placeholders with the appropriate prefix.
    /// </summary>
    /// <param name="sqlDefine">The SQL definition.</param>
    /// <param name="parameterNames">The parameter names (without prefix).</param>
    /// <returns>An array of parameter placeholders with prefix.</returns>
    public static string[] CreateParameters(this SqlDefine sqlDefine, params string[] parameterNames)
    {
        if (parameterNames == null || parameterNames.Length == 0)
            return Array.Empty<string>();

        return parameterNames.Select(name => sqlDefine.CreateParameter(name)).ToArray();
    }

    /// <summary>
    /// Creates a comma-separated list of parameter placeholders.
    /// </summary>
    /// <param name="sqlDefine">The SQL definition.</param>
    /// <param name="parameterNames">The parameter names to create placeholders for.</param>
    /// <returns>A comma-separated string of parameter placeholders.</returns>
    public static string CreateAndJoinParameters(this SqlDefine sqlDefine, params string[] parameterNames)
    {
        return string.Join(", ", sqlDefine.CreateParameters(parameterNames));
    }

    /// <summary>
    /// Creates a comma-separated list of parameter placeholders from a collection.
    /// </summary>
    /// <param name="sqlDefine">The SQL definition.</param>
    /// <param name="parameterNames">The collection of parameter names to create placeholders for.</param>
    /// <returns>A comma-separated string of parameter placeholders.</returns>
    public static string CreateAndJoinParameters(this SqlDefine sqlDefine, IEnumerable<string>? parameterNames)
    {
        if (parameterNames == null)
            return string.Empty;

        return string.Join(", ", parameterNames.Select(name => sqlDefine.CreateParameter(name)));
    }

    /// <summary>
    /// Creates SET clauses for UPDATE statements.
    /// </summary>
    /// <param name="sqlDefine">The SQL definition.</param>
    /// <param name="columnNames">The column names to create SET clauses for.</param>
    /// <returns>A comma-separated string of SET clauses.</returns>
    public static string CreateSetClauses(this SqlDefine sqlDefine, params string[] columnNames)
    {
        if (columnNames == null || columnNames.Length == 0)
            return string.Empty;

        var setClauses = columnNames.Select(col => 
            $"{sqlDefine.WrapColumn(col)} = {sqlDefine.CreateParameter(col)}");
        
        return string.Join(", ", setClauses);
    }

    /// <summary>
    /// Creates SET clauses for UPDATE statements from a collection.
    /// </summary>
    /// <param name="sqlDefine">The SQL definition.</param>
    /// <param name="columnNames">The collection of column names to create SET clauses for.</param>
    /// <returns>A comma-separated string of SET clauses.</returns>
    public static string CreateSetClauses(this SqlDefine sqlDefine, IEnumerable<string>? columnNames)
    {
        if (columnNames == null)
            return string.Empty;

        var setClauses = columnNames.Select(col => 
            $"{sqlDefine.WrapColumn(col)} = {sqlDefine.CreateParameter(col)}");
        
        return string.Join(", ", setClauses);
    }

    /// <summary>
    /// Creates WHERE conditions for SQL statements.
    /// </summary>
    /// <param name="sqlDefine">The SQL definition.</param>
    /// <param name="columnNames">The column names to create WHERE conditions for.</param>
    /// <returns>A string of WHERE conditions joined with AND.</returns>
    public static string CreateWhereConditions(this SqlDefine sqlDefine, params string[] columnNames)
    {
        if (columnNames == null || columnNames.Length == 0)
            return string.Empty;

        var conditions = columnNames.Select(col => 
            $"{sqlDefine.WrapColumn(col)} = {sqlDefine.CreateParameter(col)}");
        
        return string.Join(" AND ", conditions);
    }

    /// <summary>
    /// Creates WHERE conditions for SQL statements from a collection.
    /// </summary>
    /// <param name="sqlDefine">The SQL definition.</param>
    /// <param name="columnNames">The collection of column names to create WHERE conditions for.</param>
    /// <returns>A string of WHERE conditions joined with AND.</returns>
    public static string CreateWhereConditions(this SqlDefine sqlDefine, IEnumerable<string>? columnNames)
    {
        if (columnNames == null)
            return string.Empty;

        var conditions = columnNames.Select(col => 
            $"{sqlDefine.WrapColumn(col)} = {sqlDefine.CreateParameter(col)}");
        
        return string.Join(" AND ", conditions);
    }

    /// <summary>
    /// Determines if this SQL definition uses a specific parameter prefix.
    /// </summary>
    /// <param name="sqlDefine">The SQL definition.</param>
    /// <param name="prefix">The prefix to check for.</param>
    /// <returns>True if the definition uses the specified prefix.</returns>
    public static bool UsesParameterPrefix(this SqlDefine sqlDefine, string prefix)
    {
        if (string.IsNullOrEmpty(prefix))
            return string.IsNullOrEmpty(sqlDefine.ParameterPrefix);

        // Handle special case for SQLite's unique identifier
        if (sqlDefine.ParameterPrefix == "@sqlite")
            return prefix == "@" || prefix == "@sqlite";

        return sqlDefine.ParameterPrefix == prefix;
    }

    /// <summary>
    /// Gets the effective parameter prefix (handles SQLite special case).
    /// </summary>
    /// <param name="sqlDefine">The SQL definition.</param>
    /// <returns>The effective parameter prefix for SQL generation.</returns>
    public static string GetEffectiveParameterPrefix(this SqlDefine sqlDefine)
    {
        // Handle special case for SQLite's unique identifier
        return sqlDefine.ParameterPrefix == "@sqlite" ? "@" : sqlDefine.ParameterPrefix;
    }
}
