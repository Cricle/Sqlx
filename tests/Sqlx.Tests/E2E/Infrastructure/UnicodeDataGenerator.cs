// <copyright file="UnicodeDataGenerator.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx.Tests.E2E.Infrastructure;

/// <summary>
/// Generates Unicode and special character data for E2E testing.
/// </summary>
public static class UnicodeDataGenerator
{
    private static readonly string[] Emojis = new[]
    {
        "😀", "😃", "😄", "😁", "😆", "😅", "🤣", "😂", "🙂", "🙃",
        "😉", "😊", "😇", "🥰", "😍", "🤩", "😘", "😗", "☺️", "😚",
        "🎉", "🎊", "🎈", "🎁", "🎀", "🎂", "🎄", "🎃", "🎆", "🎇",
    };

    private static readonly string CJKCharacters =
        "你好世界こんにちは世界안녕하세요世界中文日本語한국어漢字ひらがなカタカナ";

    public static string GenerateEmoji(int count)
    {
        var random = new Random(42);
        var result = new System.Text.StringBuilder();
        for (int i = 0; i < count; i++)
        {
            result.Append(Emojis[random.Next(Emojis.Length)]);
        }
        return result.ToString();
    }

    public static string GenerateCJKText(int length)
    {
        var random = new Random(42);
        var result = new System.Text.StringBuilder();
        for (int i = 0; i < length; i++)
        {
            result.Append(CJKCharacters[random.Next(CJKCharacters.Length)]);
        }
        return result.ToString();
    }

    public static string GenerateSqlInjectionPattern()
    {
        return "'; DROP TABLE users; --";
    }

    public static string GenerateWhitespaceVariations()
    {
        return "Line 1\nLine 2\r\nLine 3\tTabbed\t\tDouble Tab  Multiple Spaces";
    }

    public static string GenerateMixedUnicode()
    {
        return $"ASCII text {GenerateEmoji(2)} {GenerateCJKText(5)} مرحبا العالم שלום עולם";
    }
}
