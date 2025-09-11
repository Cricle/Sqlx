// -----------------------------------------------------------------------
// <copyright file="NameMapper.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx;

using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// Provides mapping name support between parameter names in C# to Stored procedures parameter names.
/// </summary>
public static class NameMapper
{
    /// <summary>
    /// Maps parameter name to the database column name.
    /// Converts parameter names from Pascal/camelCase to snake_case for database compatibility.
    /// </summary>
    /// <param name="parameterName">Name of the parameter to map.</param>
    /// <returns>Corresponding database column name in snake_case.</returns>
    public static string MapName(string parameterName)
    {
        if (parameterName == null)
            throw new System.ArgumentNullException(nameof(parameterName));

        return MapNameToSnakeCase(parameterName);
    }
    
    /// <summary>
    /// Maps parameter name to snake_case for legacy database compatibility.
    /// </summary>
    /// <param name="parameterName">Name of the parameter to map.</param>
    /// <returns>Corresponding snake_case database column name.</returns>
    public static string MapNameToSnakeCase(string parameterName)
    {
        if (parameterName == null)
            throw new System.ArgumentNullException(nameof(parameterName));

        // If the parameter contains special characters (like @ or #), convert to lowercase
        if (parameterName.Any(c => !char.IsLetterOrDigit(c) && c != '_'))
        {
            return parameterName.ToLower();
        }

        var firstname = Regex.Match(parameterName, "[^A-Z]*").Value;
        var matches = Regex.Matches(parameterName, "[A-Z][^A-Z]*").Cast<Match>().Select(_ => _.Value.ToLower());
        return string.IsNullOrEmpty(firstname) ? string.Join("_", matches) : string.Join("_", new string[] { firstname }.Union(matches));
    }
}
