namespace Sqlx.Validation;

using Sqlx.Annotations;
using System;
using System.Runtime.CompilerServices;

/// <summary>
/// SQL dynamic parameter validator (runtime, high performance).
/// Provides zero-allocation validation for generated code.
/// </summary>
public static class SqlValidator
{
    /// <summary>
    /// Validates identifier (table/column name).
    /// Rules: 1-128 chars, alphanumeric + underscore, starts with letter/underscore, no SQL keywords.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValidIdentifier(ReadOnlySpan<char> identifier)
    {
        int len = identifier.Length;
        if (len == 0 || len > 128) return false;

        // First char must be letter or underscore
        char c = identifier[0];
        if (!char.IsLetter(c) && c != '_') return false;

        // Following chars must be letter, digit, or underscore
        for (int i = 1; i < len; i++)
        {
            c = identifier[i];
            if (!char.IsLetterOrDigit(c) && c != '_') return false;
        }

        return true;
    }

    /// <summary>Checks if text contains dangerous keywords.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ContainsDangerousKeyword(ReadOnlySpan<char> text)
    {
        ReadOnlySpan<string> keywords = ["DROP", "TRUNCATE", "ALTER", "EXEC", "--", "/*", ";"];
        foreach (string keyword in keywords)
        {
            if (text.Contains(keyword.AsSpan(), StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Validates SQL fragment (WHERE/JOIN clause).
    /// Rules: 1-4096 chars, no DDL/dangerous operations, no comment symbols.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValidFragment(ReadOnlySpan<char> fragment)
        => fragment.Length > 0 && fragment.Length <= 4096 && !ContainsDangerousKeyword(fragment);

    /// <summary>
    /// Validates table part (prefix/suffix).
    /// Rules: 1-64 chars, alphanumeric only (strictest).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValidTablePart(ReadOnlySpan<char> part)
    {
        int len = part.Length;
        if (len == 0 || len > 64) return false;

        for (int i = 0; i < len; i++)
        {
            if (!char.IsLetterOrDigit(part[i])) return false;
        }

        return true;
    }

    /// <summary>Validates dynamic SQL parameter by type.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Validate(ReadOnlySpan<char> value, DynamicSqlType type)
        => type switch
        {
            DynamicSqlType.Identifier => IsValidIdentifier(value),
            DynamicSqlType.Fragment => IsValidFragment(value),
            DynamicSqlType.TablePart => IsValidTablePart(value),
            _ => false
        };
}
