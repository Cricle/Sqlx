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
    // 性能优化：预编译正则表达式
    private static readonly Regex FirstPartRegex = new("[^A-Z]*", RegexOptions.Compiled);
    private static readonly Regex CapitalWordsRegex = new("[A-Z][^A-Z]*", RegexOptions.Compiled);
    /// <summary>
    /// Maps parameter name to snake_case for database compatibility.
    /// </summary>
    /// <param name="parameterName">Name of the parameter to map.</param>
    /// <returns>Corresponding snake_case database column name.</returns>
    public static string MapName(string parameterName)
    {
        if (parameterName == null)
            throw new System.ArgumentNullException(nameof(parameterName));

        // If the parameter contains special characters (like @ or #), convert to lowercase
        if (parameterName.Any(c => !char.IsLetterOrDigit(c) && c != '_'))
        {
            return parameterName.ToLower();
        }

        var firstname = FirstPartRegex.Match(parameterName).Value;
        var matches = CapitalWordsRegex.Matches(parameterName);

        if (string.IsNullOrEmpty(firstname))
        {
            return string.Join("_", matches.Cast<Match>().Select(m => m.Value.ToLower()));
        }

        var parts = new string[matches.Count + 1];
        parts[0] = firstname;
        for (int i = 0; i < matches.Count; i++)
        {
            parts[i + 1] = matches[i].Value.ToLower();
        }
        return string.Join("_", parts);
    }
}
