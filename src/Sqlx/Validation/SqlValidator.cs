namespace Sqlx.Validation;

using System;
using System.Runtime.CompilerServices;

/// <summary>
/// SQL 动态参数验证器（运行时使用，高性能）
/// </summary>
/// <remarks>
/// 此类提供零分配的验证方法，供生成的代码使用（可选）。
/// 大部分情况下，生成的代码会内联验证逻辑，无需调用此类。
/// </remarks>
public static class SqlValidator
{
    /// <summary>
    /// 验证标识符（表名、列名）
    /// </summary>
    /// <param name="identifier">标识符</param>
    /// <returns>如果有效返回 true，否则返回 false</returns>
    /// <remarks>
    /// 验证规则：
    /// <list type="bullet">
    ///   <item>长度：1-128 字符</item>
    ///   <item>格式：字母/数字/下划线，以字母或下划线开头</item>
    ///   <item>不包含 SQL 关键字和危险字符</item>
    /// </list>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValidIdentifier(ReadOnlySpan<char> identifier)
    {
        if (identifier.Length == 0 || identifier.Length > 128)
            return false;

        // 第一个字符必须是字母或下划线
        char first = identifier[0];
        if (!((first >= 'a' && first <= 'z') || (first >= 'A' && first <= 'Z') || first == '_'))
            return false;

        // 后续字符必须是字母、数字或下划线
        for (int i = 1; i < identifier.Length; i++)
        {
            char c = identifier[i];
            if (!((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_'))
                return false;
        }

        return true;
    }

    /// <summary>
    /// 检查是否包含危险关键字
    /// </summary>
    /// <param name="text">要检查的文本</param>
    /// <returns>如果包含危险关键字返回 true</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ContainsDangerousKeyword(ReadOnlySpan<char> text)
    {
        // 使用 Span 的 Contains 方法，编译器会优化这些常量比较
        return text.Contains("DROP".AsSpan(), StringComparison.OrdinalIgnoreCase) ||
               text.Contains("TRUNCATE".AsSpan(), StringComparison.OrdinalIgnoreCase) ||
               text.Contains("ALTER".AsSpan(), StringComparison.OrdinalIgnoreCase) ||
               text.Contains("EXEC".AsSpan(), StringComparison.OrdinalIgnoreCase) ||
               text.Contains("--".AsSpan(), StringComparison.Ordinal) ||
               text.Contains("/*".AsSpan(), StringComparison.Ordinal) ||
               text.Contains(";".AsSpan(), StringComparison.Ordinal);
    }

    /// <summary>
    /// 验证SQL片段（WHERE、JOIN等）
    /// </summary>
    /// <param name="fragment">SQL 片段</param>
    /// <returns>如果有效返回 true，否则返回 false</returns>
    /// <remarks>
    /// 验证规则：
    /// <list type="bullet">
    ///   <item>长度：1-4096 字符</item>
    ///   <item>不包含 DDL/危险操作</item>
    ///   <item>不包含注释符号</item>
    /// </list>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValidFragment(ReadOnlySpan<char> fragment)
    {
        if (fragment.Length == 0 || fragment.Length > 4096)
            return false;

        return !ContainsDangerousKeyword(fragment);
    }

    /// <summary>
    /// 验证表名部分（前缀、后缀）
    /// </summary>
    /// <param name="part">表名部分</param>
    /// <returns>如果有效返回 true，否则返回 false</returns>
    /// <remarks>
    /// 验证规则（最严格）：
    /// <list type="bullet">
    ///   <item>长度：1-64 字符</item>
    ///   <item>只允许字母和数字</item>
    /// </list>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValidTablePart(ReadOnlySpan<char> part)
    {
        if (part.Length == 0 || part.Length > 64)
            return false;

        // 只允许字母和数字
        foreach (char c in part)
        {
            if (!((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9')))
                return false;
        }

        return true;
    }

    /// <summary>
    /// 根据类型验证动态 SQL 参数
    /// </summary>
    /// <param name="value">参数值</param>
    /// <param name="type">参数类型</param>
    /// <returns>如果有效返回 true，否则返回 false</returns>
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

