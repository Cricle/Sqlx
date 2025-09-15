// -----------------------------------------------------------------------
// <copyright file="DatabaseDialectProviderExtensions.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.Generator.Core;

/// <summary>
/// Extension methods for IDatabaseDialectProvider.
/// </summary>
internal static class DatabaseDialectProviderExtensions
{
    /// <summary>
    /// Wraps a column name with the appropriate database-specific delimiters.
    /// </summary>
    /// <param name="provider">The dialect provider.</param>
    /// <param name="columnName">The column name to wrap.</param>
    /// <returns>The wrapped column name.</returns>
    public static string WrapColumn(this IDatabaseDialectProvider provider, string columnName)
    {
        return provider.SqlDefine.WrapColumn(columnName);
    }

    /// <summary>
    /// Wraps a string value with the appropriate database-specific delimiters.
    /// </summary>
    /// <param name="provider">The dialect provider.</param>
    /// <param name="value">The string value to wrap.</param>
    /// <returns>The wrapped string value.</returns>
    public static string WrapString(this IDatabaseDialectProvider provider, string value)
    {
        return provider.SqlDefine.WrapString(value);
    }

    /// <summary>
    /// Gets the parameter prefix for the database dialect.
    /// </summary>
    /// <param name="provider">The dialect provider.</param>
    /// <returns>The parameter prefix (e.g., "@", "$", ":"). </returns>
    public static string GetParameterPrefix(this IDatabaseDialectProvider provider)
    {
        return provider.SqlDefine.ParameterPrefix;
    }
}
