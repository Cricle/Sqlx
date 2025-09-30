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
        var matches = CapitalWordsRegex.Matches(parameterName).Cast<Match>().Select(_ => _.Value.ToLower());
        // 性能优化：避免重复的string.Join调用和临时数组创建
        if (string.IsNullOrEmpty(firstname))
        {
            return string.Join("_", matches);
        }

        var allParts = new string[matches.Count() + 1];
        allParts[0] = firstname;
        var index = 1;
        foreach (var match in matches)
        {
            allParts[index++] = match;
        }
        return string.Join("_", allParts);
    }
}
