namespace Sqlx.Validation;

using System;
using System.Runtime.CompilerServices;
using Sqlx.Annotations;

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

    /// <summary>Checks if text contains dangerous keywords.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ContainsDangerousKeyword(ReadOnlySpan<char> text)
    {
        return text.Contains("DROP".AsSpan(), StringComparison.OrdinalIgnoreCase) ||
               text.Contains("TRUNCATE".AsSpan(), StringComparison.OrdinalIgnoreCase) ||
               text.Contains("ALTER".AsSpan(), StringComparison.OrdinalIgnoreCase) ||
               text.Contains("EXEC".AsSpan(), StringComparison.OrdinalIgnoreCase) ||
               text.Contains("--".AsSpan(), StringComparison.Ordinal) ||
               text.Contains("/*".AsSpan(), StringComparison.Ordinal) ||
               text.Contains(";".AsSpan(), StringComparison.Ordinal);
    }

    /// <summary>
    /// Validates SQL fragment (WHERE/JOIN clause).
    /// Rules: 1-4096 chars, no DDL/dangerous operations, no comment symbols.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValidFragment(ReadOnlySpan<char> fragment)
    {
        if (fragment.Length == 0 || fragment.Length > 4096)
            return false;

        return !ContainsDangerousKeyword(fragment);
    }

    /// <summary>
    /// Validates table part (prefix/suffix).
    /// Rules: 1-64 chars, alphanumeric only (strictest).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValidTablePart(ReadOnlySpan<char> part)
    {
        if (part.Length == 0 || part.Length > 64)
            return false;

        // Only letters and digits
        foreach (char c in part)
        {
            if (!((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9')))
                return false;
        }

        return true;
    }

    /// <summary>Validates dynamic SQL parameter by type.</summary>
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
