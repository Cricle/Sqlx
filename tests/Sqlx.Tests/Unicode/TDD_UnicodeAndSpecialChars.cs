using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

namespace Sqlx.Tests.Unicode;

/// <summary>
/// TDD: Unicode和特殊字符测试
/// 验证Sqlx能够正确处理各种Unicode字符和特殊字符
/// </summary>
[TestClass]
public class TDD_UnicodeAndSpecialChars
{
    private SqliteConnection? _connection;

    [TestInitialize]
    public void Setup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _connection.Execute(@"
            CREATE TABLE messages (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                content TEXT NOT NULL,
                sender TEXT,
                emoji TEXT
            )
        ");
    }

    [TestCleanup]
    public void Cleanup()
    {
        _connection?.Dispose();
    }

    [TestMethod]
    [TestCategory("Unicode")]
    [TestCategory("Emoji")]
    [Description("Emoji表情应能正确存储和查询")]
    public async Task Unicode_Emoji_ShouldStoreAndRetrieve()
    {
        // Arrange
        var repo = new UnicodeTestRepository(_connection!);
        var emojis = "😀😃😄😁😆😅🤣😂🙂🙃😉😊😇🥰😍🤩😘";

        // Act
        var id = await repo.InsertMessageAsync(emojis, "emoji_user", emojis);
        var result = await repo.GetMessageByIdAsync(id);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(emojis, result.Content);
        Assert.AreEqual(emojis, result.Emoji);
    }

    [TestMethod]
    [TestCategory("Unicode")]
    [TestCategory("Chinese")]
    [Description("中文字符应能正确存储和查询")]
    public async Task Unicode_Chinese_ShouldStoreAndRetrieve()
    {
        // Arrange
        var repo = new UnicodeTestRepository(_connection!);
        var chinese = "你好世界！这是一个测试。中文字符包括：简体、繁體、漢字。";

        // Act
        var id = await repo.InsertMessageAsync(chinese, "中文用户", "🇨🇳");
        var result = await repo.GetMessageByIdAsync(id);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(chinese, result.Content);
        Assert.AreEqual("中文用户", result.Sender);
    }

    [TestMethod]
    [TestCategory("Unicode")]
    [TestCategory("Japanese")]
    [Description("日文字符应能正确存储和查询")]
    public async Task Unicode_Japanese_ShouldStoreAndRetrieve()
    {
        // Arrange
        var repo = new UnicodeTestRepository(_connection!);
        var japanese = "こんにちは世界！これはテストです。ひらがな、カタカナ、漢字。";

        // Act
        var id = await repo.InsertMessageAsync(japanese, "日本ユーザー", "🇯🇵");
        var result = await repo.GetMessageByIdAsync(id);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(japanese, result.Content);
    }

    [TestMethod]
    [TestCategory("Unicode")]
    [TestCategory("Korean")]
    [Description("韩文字符应能正确存储和查询")]
    public async Task Unicode_Korean_ShouldStoreAndRetrieve()
    {
        // Arrange
        var repo = new UnicodeTestRepository(_connection!);
        var korean = "안녕하세요 세계! 이것은 테스트입니다. 한글 문자입니다.";

        // Act
        var id = await repo.InsertMessageAsync(korean, "한국 사용자", "🇰🇷");
        var result = await repo.GetMessageByIdAsync(id);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(korean, result.Content);
    }

    [TestMethod]
    [TestCategory("Unicode")]
    [TestCategory("Arabic")]
    [Description("阿拉伯文字符应能正确存储和查询")]
    public async Task Unicode_Arabic_ShouldStoreAndRetrieve()
    {
        // Arrange
        var repo = new UnicodeTestRepository(_connection!);
        var arabic = "مرحبا بالعالم! هذا اختبار. الأحرف العربية.";

        // Act
        var id = await repo.InsertMessageAsync(arabic, "مستخدم عربي", "🇸🇦");
        var result = await repo.GetMessageByIdAsync(id);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(arabic, result.Content);
    }

    [TestMethod]
    [TestCategory("Unicode")]
    [TestCategory("Russian")]
    [Description("俄文字符应能正确存储和查询")]
    public async Task Unicode_Russian_ShouldStoreAndRetrieve()
    {
        // Arrange
        var repo = new UnicodeTestRepository(_connection!);
        var russian = "Привет мир! Это тест. Русские символы.";

        // Act
        var id = await repo.InsertMessageAsync(russian, "Русский пользователь", "🇷🇺");
        var result = await repo.GetMessageByIdAsync(id);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(russian, result.Content);
    }

    [TestMethod]
    [TestCategory("Unicode")]
    [TestCategory("MixedLanguages")]
    [Description("混合多语言字符应能正确存储和查询")]
    public async Task Unicode_MixedLanguages_ShouldStoreAndRetrieve()
    {
        // Arrange
        var repo = new UnicodeTestRepository(_connection!);
        var mixed = "Hello世界！こんにちは안녕مرحباПривет🌍🌎🌏";

        // Act
        var id = await repo.InsertMessageAsync(mixed, "Multilingual User", "🗺️");
        var result = await repo.GetMessageByIdAsync(id);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(mixed, result.Content);
    }

    [TestMethod]
    [TestCategory("Unicode")]
    [TestCategory("SpecialSymbols")]
    [Description("数学符号应能正确存储和查询")]
    public async Task Unicode_MathSymbols_ShouldStoreAndRetrieve()
    {
        // Arrange
        var repo = new UnicodeTestRepository(_connection!);
        var mathSymbols = "∑∏∫∮∂∇∆√∞≈≠≤≥±×÷∈∉∪∩⊂⊃⊆⊇";

        // Act
        var id = await repo.InsertMessageAsync(mathSymbols, "Math User", "🔢");
        var result = await repo.GetMessageByIdAsync(id);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(mathSymbols, result.Content);
    }

    [TestMethod]
    [TestCategory("Unicode")]
    [TestCategory("Currency")]
    [Description("货币符号应能正确存储和查询")]
    public async Task Unicode_CurrencySymbols_ShouldStoreAndRetrieve()
    {
        // Arrange
        var repo = new UnicodeTestRepository(_connection!);
        var currencies = "$€£¥₹₽₩₪₺₴₵¢₡₢₣₤₥₦₧₨₰₱₲₳₴₵";

        // Act
        var id = await repo.InsertMessageAsync(currencies, "Finance User", "💰");
        var result = await repo.GetMessageByIdAsync(id);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(currencies, result.Content);
    }

    [TestMethod]
    [TestCategory("Unicode")]
    [TestCategory("ZeroWidth")]
    [Description("零宽字符应能正确处理")]
    public async Task Unicode_ZeroWidthCharacters_ShouldHandle()
    {
        // Arrange
        var repo = new UnicodeTestRepository(_connection!);
        var zeroWidth = "Test\u200B\u200C\u200DContent"; // 零宽空格、零宽非连接符、零宽连接符

        // Act
        var id = await repo.InsertMessageAsync(zeroWidth, "ZW User", "");
        var result = await repo.GetMessageByIdAsync(id);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(zeroWidth, result.Content);
    }

    [TestMethod]
    [TestCategory("Unicode")]
    [TestCategory("ControlCharacters")]
    [Description("控制字符应能正确处理")]
    public async Task Unicode_ControlCharacters_ShouldHandle()
    {
        // Arrange
        var repo = new UnicodeTestRepository(_connection!);
        var withControl = "Line1\nLine2\rLine3\r\nLine4\tTabbed";

        // Act
        var id = await repo.InsertMessageAsync(withControl, "Control User", "");
        var result = await repo.GetMessageByIdAsync(id);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(withControl, result.Content);
    }

    [TestMethod]
    [TestCategory("Unicode")]
    [TestCategory("Whitespace")]
    [Description("各种空白字符应能正确处理")]
    public async Task Unicode_VariousWhitespace_ShouldHandle()
    {
        // Arrange
        var repo = new UnicodeTestRepository(_connection!);
        var whitespace = "Test\u0020\u00A0\u1680\u2000\u2001\u2002\u2003Content";

        // Act
        var id = await repo.InsertMessageAsync(whitespace, "WS User", "");
        var result = await repo.GetMessageByIdAsync(id);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(whitespace, result.Content);
    }

    [TestMethod]
    [TestCategory("Unicode")]
    [TestCategory("Diacritics")]
    [Description("变音符号应能正确处理")]
    public async Task Unicode_Diacritics_ShouldHandle()
    {
        // Arrange
        var repo = new UnicodeTestRepository(_connection!);
        var diacritics = "àáâãäåèéêëìíîïòóôõöùúûüýÿñç";

        // Act
        var id = await repo.InsertMessageAsync(diacritics, "Diacritic User", "");
        var result = await repo.GetMessageByIdAsync(id);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(diacritics, result.Content);
    }

    [TestMethod]
    [TestCategory("Unicode")]
    [TestCategory("RightToLeft")]
    [Description("从右到左的文字应能正确处理")]
    public async Task Unicode_RightToLeft_ShouldHandle()
    {
        // Arrange
        var repo = new UnicodeTestRepository(_connection!);
        var rtl = "\u202EReversed Text\u202C"; // RLE + text + PDF

        // Act
        var id = await repo.InsertMessageAsync(rtl, "RTL User", "");
        var result = await repo.GetMessageByIdAsync(id);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(rtl, result.Content);
    }

    [TestMethod]
    [TestCategory("Unicode")]
    [TestCategory("Combining")]
    [Description("组合字符应能正确处理")]
    public async Task Unicode_CombiningCharacters_ShouldHandle()
    {
        // Arrange
        var repo = new UnicodeTestRepository(_connection!);
        var combining = "e\u0301"; // é 使用组合字符

        // Act
        var id = await repo.InsertMessageAsync(combining, "Combining User", "");
        var result = await repo.GetMessageByIdAsync(id);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(combining, result.Content);
    }

    [TestMethod]
    [TestCategory("Unicode")]
    [TestCategory("QueryByUnicode")]
    [Description("应能通过Unicode字符查询")]
    public async Task Unicode_QueryByUnicode_ShouldWork()
    {
        // Arrange
        var repo = new UnicodeTestRepository(_connection!);
        await repo.InsertMessageAsync("Hello", "Alice", "😀");
        await repo.InsertMessageAsync("World", "Bob", "😃");
        await repo.InsertMessageAsync("Test", "Charlie", "😄");

        // Act
        var results = await repo.SearchByEmojiAsync("😃");

        // Assert
        Assert.AreEqual(1, results.Count);
        Assert.AreEqual("World", results[0].Content);
    }

    [TestMethod]
    [TestCategory("Unicode")]
    [TestCategory("BatchWithUnicode")]
    [Description("批量操作应支持Unicode")]
    public async Task Unicode_BatchOperationsWithUnicode_ShouldWork()
    {
        // Arrange
        var repo = new UnicodeTestRepository(_connection!);
        var messages = new List<UnicodeMessage>
        {
            new() { Content = "你好", Sender = "用户1", Emoji = "🇨🇳" },
            new() { Content = "こんにちは", Sender = "ユーザー2", Emoji = "🇯🇵" },
            new() { Content = "안녕하세요", Sender = "사용자3", Emoji = "🇰🇷" },
            new() { Content = "مرحبا", Sender = "مستخدم4", Emoji = "🇸🇦" },
            new() { Content = "Привет", Sender = "Пользователь5", Emoji = "🇷🇺" }
        };

        // Act
        var inserted = await repo.BatchInsertMessagesAsync(messages);

        // Assert
        Assert.AreEqual(5, inserted);

        var allMessages = await repo.GetAllMessagesAsync();
        Assert.AreEqual(5, allMessages.Count);
        Assert.IsTrue(allMessages.Any(m => m.Content == "你好"));
        Assert.IsTrue(allMessages.Any(m => m.Content == "こんにちは"));
    }

    [TestMethod]
    [TestCategory("Unicode")]
    [TestCategory("LongUnicode")]
    [Description("超长Unicode字符串应能正确处理")]
    public async Task Unicode_VeryLongString_ShouldHandle()
    {
        // Arrange
        var repo = new UnicodeTestRepository(_connection!);
        var longString = string.Join("", Enumerable.Repeat("测试Test🌏", 1000));

        // Act
        var id = await repo.InsertMessageAsync(longString, "Long User", "📏");
        var result = await repo.GetMessageByIdAsync(id);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(longString, result.Content);
        Assert.IsTrue(result.Content.Length >= 10000);
    }

    [TestMethod]
    [TestCategory("Unicode")]
    [TestCategory("UpdateWithUnicode")]
    [Description("更新操作应支持Unicode")]
    public async Task Unicode_UpdateWithUnicode_ShouldWork()
    {
        // Arrange
        var repo = new UnicodeTestRepository(_connection!);
        var id = await repo.InsertMessageAsync("Original", "User", "");

        // Act
        var updated = await repo.UpdateContentAsync(id, "更新后的内容🎉");
        var result = await repo.GetMessageByIdAsync(id);

        // Assert
        Assert.AreEqual(1, updated);
        Assert.IsNotNull(result);
        Assert.AreEqual("更新后的内容🎉", result.Content);
    }
}

// 测试模型
public class UnicodeMessage
{
    public long Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? Sender { get; set; }
    public string? Emoji { get; set; }
}

// 测试仓储接口
public interface IUnicodeTestRepository
{
    [SqlTemplate("INSERT INTO messages (content, sender, emoji) VALUES (@content, @sender, @emoji)")]
    [ReturnInsertedId]
    Task<long> InsertMessageAsync(string content, string? sender, string? emoji);

    [SqlTemplate("SELECT * FROM messages WHERE id = @id")]
    Task<UnicodeMessage?> GetMessageByIdAsync(long id);

    [SqlTemplate("SELECT * FROM messages")]
    Task<List<UnicodeMessage>> GetAllMessagesAsync();

    [SqlTemplate("SELECT * FROM messages WHERE emoji = @emoji")]
    Task<List<UnicodeMessage>> SearchByEmojiAsync(string emoji);

    [BatchOperation(MaxBatchSize = 100)]
    [SqlTemplate("INSERT INTO messages (content, sender, emoji) VALUES {{batch_values}}")]
    Task<int> BatchInsertMessagesAsync(IEnumerable<UnicodeMessage> messages);

    [SqlTemplate("UPDATE messages SET content = @content WHERE id = @id")]
    Task<int> UpdateContentAsync(long id, string content);
}

// 测试仓储实现
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IUnicodeTestRepository))]
public partial class UnicodeTestRepository(IDbConnection connection) : IUnicodeTestRepository { }

// 扩展方法
public static class UnicodeTestExtensions
{
    public static void Execute(this IDbConnection connection, string sql)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }
}

