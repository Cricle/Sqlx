namespace Sqlx.Validation;

using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using Sqlx.Annotations;

/// <summary>
/// SQL dynamic parameter validator (runtime, high performance).
/// Provides zero-allocation validation for generated code.
/// </summary>
public static class SqlValidator
{
    /// <summary>
    /// Validates whether a string is a valid SQL identifier (table or column name).
    /// </summary>
    /// <param name="identifier">The identifier to validate.</param>
    /// <returns><c>true</c> if the identifier is valid; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// <para><strong>Validation Rules:</strong></para>
    /// <list type="number">
    /// <item><description>Length: 1-128 characters</description></item>
    /// <item><description>First character: letter (a-z, A-Z) or underscore (_)</description></item>
    /// <item><description>Subsequent characters: letters, digits (0-9), or underscore</description></item>
    /// <item><description>No SQL reserved keywords (checked separately)</description></item>
    /// </list>
    /// <para><strong>Valid Examples:</strong> user_id, TableName, _temp, Column1</para>
    /// <para><strong>Invalid Examples:</strong> 1user (starts with digit), user-id (contains hyphen), "" (empty)</para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValidIdentifier(ReadOnlySpan<char> identifier)
    {
        if (identifier.Length == 0 || identifier.Length > 128)
            return false;

        // First char must be letter or underscore
        char first = identifier[0];
        if (!((first >= 'a' && first <= 'z') || (first >= 'A' && first <= 'Z') || first == '_'))
            return false;

        // Following chars must be letter, digit, or underscore
        for (int i = 1; i < identifier.Length; i++)
        {
            char c = identifier[i];
            if (!((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_'))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Checks if the text contains any dangerous SQL keywords that could indicate SQL injection attempts.
    /// </summary>
    /// <param name="text">The text to check for dangerous keywords.</param>
    /// <returns><c>true</c> if dangerous keywords are found; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// <para>Dangerous keywords include:</para>
    /// <list type="bullet">
    /// <item><description>DDL operations: DROP, TRUNCATE, ALTER</description></item>
    /// <item><description>Execution commands: EXEC</description></item>
    /// <item><description>Comment symbols: --, /*</description></item>
    /// <item><description>Statement terminators: ;</description></item>
    /// </list>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ContainsDangerousKeyword(ReadOnlySpan<char> text)
    {
        // Check each dangerous keyword individually for better performance
        return text.Contains("DROP".AsSpan(), StringComparison.OrdinalIgnoreCase) ||
               text.Contains("TRUNCATE".AsSpan(), StringComparison.OrdinalIgnoreCase) ||
               text.Contains("ALTER".AsSpan(), StringComparison.OrdinalIgnoreCase) ||
               text.Contains("EXEC".AsSpan(), StringComparison.OrdinalIgnoreCase) ||
               text.Contains("--".AsSpan(), StringComparison.Ordinal) ||
               text.Contains("/*".AsSpan(), StringComparison.Ordinal) ||
               text.Contains(";".AsSpan(), StringComparison.Ordinal);
    }

    /// <summary>
    /// Validates whether a string is a valid SQL fragment (WHERE clause, JOIN condition, etc.).
    /// </summary>
    /// <param name="fragment">The SQL fragment to validate.</param>
    /// <returns><c>true</c> if the fragment is valid; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// <para><strong>Validation Rules:</strong></para>
    /// <list type="number">
    /// <item><description>Length: 1-4096 characters</description></item>
    /// <item><description>No DDL operations (DROP, TRUNCATE, ALTER)</description></item>
    /// <item><description>No dangerous commands (EXEC)</description></item>
    /// <item><description>No comment symbols (--, /*)</description></item>
    /// <item><description>No statement terminators (;)</description></item>
    /// </list>
    /// <para><strong>Valid Examples:</strong> "age > 18", "status = 'active' AND deleted = 0"</para>
    /// <para><strong>Invalid Examples:</strong> "1=1; DROP TABLE users--", "age > 18 /* comment */"</para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValidFragment(ReadOnlySpan<char> fragment)
    {
        if (fragment.Length == 0 || fragment.Length > 4096)
            return false;

        return !ContainsDangerousKeyword(fragment);
    }

    /// <summary>
    /// Validates whether a string is a valid table name part (prefix or suffix for dynamic table names).
    /// </summary>
    /// <param name="part">The table name part to validate.</param>
    /// <returns><c>true</c> if the part is valid; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// <para><strong>Validation Rules (Strictest):</strong></para>
    /// <list type="number">
    /// <item><description>Length: 1-64 characters</description></item>
    /// <item><description>Only letters (a-z, A-Z) and digits (0-9)</description></item>
    /// <item><description>No underscores, hyphens, or special characters</description></item>
    /// </list>
    /// <para><strong>Use Case:</strong> Dynamic table names like "users_2024", "logs_archive"</para>
    /// <para><strong>Valid Examples:</strong> "2024", "archive", "temp1"</para>
    /// <para><strong>Invalid Examples:</strong> "2024_01" (contains underscore), "temp-1" (contains hyphen)</para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValidTablePart(ReadOnlySpan<char> part)
    {
        if (part.Length == 0 || part.Length > 64)
            return false;

        // Only letters and digits
        foreach (char c in part)
        {
            if (!char.IsLetterOrDigit(c))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Validates a dynamic SQL parameter based on its type.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="type">The type of dynamic SQL parameter.</param>
    /// <returns><c>true</c> if the value is valid for the specified type; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// <para>This method delegates to the appropriate validation method based on the parameter type:</para>
    /// <list type="bullet">
    /// <item><description><see cref="DynamicSqlType.Identifier"/>: Uses <see cref="IsValidIdentifier"/></description></item>
    /// <item><description><see cref="DynamicSqlType.Fragment"/>: Uses <see cref="IsValidFragment"/></description></item>
    /// <item><description><see cref="DynamicSqlType.TablePart"/>: Uses <see cref="IsValidTablePart"/></description></item>
    /// </list>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Validate(ReadOnlySpan<char> value, DynamicSqlType type)
    {
        return type switch
        {
            DynamicSqlType.Identifier => IsValidIdentifier(value),
            DynamicSqlType.Fragment => IsValidFragment(value),
            DynamicSqlType.TablePart => IsValidTablePart(value),
            _ => false
        };
    }
}
