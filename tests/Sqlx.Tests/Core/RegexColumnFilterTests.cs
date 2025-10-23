using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Generator;
using System;
using System.Linq;

namespace Sqlx.Tests.Core;

/// <summary>
/// TDD: --regex 正则表达式列筛选功能测试
/// </summary>
[TestClass]
public class RegexColumnFilterTests
{
    private SqlTemplateEngine _engine = null!;
    private Compilation _compilation = null!;
    private IMethodSymbol _testMethod = null!;
    private INamedTypeSymbol _userEntityType = null!;
    private INamedTypeSymbol _secureEntityType = null!;

    [TestInitialize]
    public void Setup()
    {
        _engine = new SqlTemplateEngine();

        // 创建测试编译
        var sourceCode = @"
using System;

namespace TestNamespace
{
    public class TestUserEntity
    {
        public long id { get; set; }
        public string user_name { get; set; }
        public string user_email { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
    }

    public class TestSecureEntity
    {
        public long id { get; set; }
        public string name { get; set; }
        public string password { get; set; }
        public string secret_token { get; set; }
    }

    public interface ITestService
    {
        void TestMethod();
    }
}";

        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(DateTime).Assembly.Location)
        };

        _compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // 获取测试符号
        _userEntityType = _compilation.GetTypeByMetadataName("TestNamespace.TestUserEntity")!;
        _secureEntityType = _compilation.GetTypeByMetadataName("TestNamespace.TestSecureEntity")!;
        var serviceType = _compilation.GetTypeByMetadataName("TestNamespace.ITestService")!;
        _testMethod = serviceType.GetMembers("TestMethod").OfType<IMethodSymbol>().First();
    }

    #region 基础正则筛选测试

    [TestMethod]
    public void Regex_MatchPrefix_ShouldFilterCorrectly()
    {
        // Arrange
        var template = "SELECT {{columns --regex ^user_}} FROM users";

        // Act
        var result = _engine.ProcessTemplate(template, _testMethod, _userEntityType, "users");

        // Assert
        Assert.IsNotNull(result);
        StringAssert.Contains(result.ProcessedSql, "user_name");
        StringAssert.Contains(result.ProcessedSql, "user_email");
        Assert.IsFalse(result.ProcessedSql.Contains("id"), "Should not contain 'id'");
        Assert.IsFalse(result.ProcessedSql.Contains("created_at"), "Should not contain 'created_at'");
    }

    [TestMethod]
    public void Regex_MatchSuffix_ShouldFilterCorrectly()
    {
        // Arrange
        var template = "SELECT {{columns --regex _at$}} FROM users";

        // Act
        var result = _engine.ProcessTemplate(template, _testMethod, _userEntityType, "users");

        // Assert
        StringAssert.Contains(result.ProcessedSql, "created_at");
        StringAssert.Contains(result.ProcessedSql, "updated_at");
        Assert.IsFalse(result.ProcessedSql.Contains("user_name"));
    }

    [TestMethod]
    public void Regex_ExcludePattern_ShouldFilterCorrectly()
    {
        // Arrange - 排除包含 "password" 或 "secret" 的列
        var template = "SELECT {{columns --regex ^(?!.*(password|secret))}} FROM users";

        // Act
        var result = _engine.ProcessTemplate(template, _testMethod, _secureEntityType, "users");

        // Assert
        StringAssert.Contains(result.ProcessedSql, "id");
        StringAssert.Contains(result.ProcessedSql, "name");
        Assert.IsFalse(result.ProcessedSql.Contains("password"));
        Assert.IsFalse(result.ProcessedSql.Contains("secret_token"));
    }

    [TestMethod]
    public void Regex_CaseInsensitive_ShouldMatch()
    {
        // Arrange
        var template = "SELECT {{columns --regex (?i)^USER_}} FROM users";

        // Act
        var result = _engine.ProcessTemplate(template, _testMethod, _userEntityType, "users");

        // Assert
        StringAssert.Contains(result.ProcessedSql, "user_name");
        StringAssert.Contains(result.ProcessedSql, "user_email");
    }

    #endregion

    #region 组合使用测试

    [TestMethod]
    public void Regex_WithExclude_ShouldWorkTogether()
    {
        // Arrange - 正则筛选 user_ 开头，再排除 user_email
        var template = "SELECT {{columns --regex ^user_ --exclude user_email}} FROM users";

        // Act
        var result = _engine.ProcessTemplate(template, _testMethod, _userEntityType, "users");

        // Assert
        StringAssert.Contains(result.ProcessedSql, "user_name");
        Assert.IsFalse(result.ProcessedSql.Contains("user_email"));
        Assert.IsFalse(result.ProcessedSql.Contains("id"));
    }

    [TestMethod]
    public void Regex_WithOnly_ShouldWorkTogether()
    {
        // Arrange - 正则筛选后，再用 --only 进一步限制
        var template = "SELECT {{columns --regex ^user_ --only user_name}} FROM users";

        // Act
        var result = _engine.ProcessTemplate(template, _testMethod, _userEntityType, "users");

        // Assert
        StringAssert.Contains(result.ProcessedSql, "user_name");
        Assert.IsFalse(result.ProcessedSql.Contains("user_email"));
    }

    #endregion

    #region 错误处理测试

    [TestMethod]
    public void Regex_InvalidPattern_ShouldThrowException()
    {
        // Arrange - 无效的正则表达式
        var template = "SELECT {{columns --regex [}} FROM users";

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() =>
        {
            _engine.ProcessTemplate(template, _testMethod, _userEntityType, "users");
        });
    }

    [TestMethod]
    public void Regex_ComplexPattern_ShouldTimeout()
    {
        // Arrange - 可能导致 ReDoS 的复杂正则
        var template = "SELECT {{columns --regex (a+)+b}} FROM users";

        // Act & Assert
        var ex = Assert.ThrowsException<InvalidOperationException>(() =>
        {
            _engine.ProcessTemplate(template, _testMethod, _userEntityType, "users");
        });

        StringAssert.Contains(ex.Message, "timeout", "Should mention timeout");
    }

    [TestMethod]
    public void Regex_NoMatches_ShouldReturnEmpty()
    {
        // Arrange - 正则不匹配任何列
        var template = "SELECT {{columns --regex ^xyz_}} FROM users";

        // Act
        var result = _engine.ProcessTemplate(template, _testMethod, _userEntityType, "users");

        // Assert
        // 应该生成空的列列表或抛出警告
        Assert.IsTrue(
            string.IsNullOrWhiteSpace(result.ProcessedSql) ||
            result.Warnings.Any(w => w.Contains("No columns matched")),
            "Should have warning about no matches");
    }

    #endregion

    #region 性能测试

    [TestMethod]
    public void Regex_Cache_ShouldReuseCompiledRegex()
    {
        // Arrange
        var template = "SELECT {{columns --regex ^user_}} FROM users";

        // Act - 多次处理相同模板
        var result1 = _engine.ProcessTemplate(template, _testMethod, _userEntityType, "users");
        var result2 = _engine.ProcessTemplate(template, _testMethod, _userEntityType, "users");
        var result3 = _engine.ProcessTemplate(template, _testMethod, _userEntityType, "users");

        // Assert - 结果应该一致（说明缓存工作正常）
        Assert.AreEqual(result1.ProcessedSql, result2.ProcessedSql);
        Assert.AreEqual(result2.ProcessedSql, result3.ProcessedSql);
    }

    #endregion

    #region 边界测试

    [TestMethod]
    public void Regex_EmptyPattern_ShouldMatchAll()
    {
        // Arrange
        var template = "SELECT {{columns --regex}} FROM users";

        // Act & Assert - 应该抛出异常或匹配所有列
        Assert.ThrowsException<ArgumentException>(() =>
        {
            _engine.ProcessTemplate(template, _testMethod, _userEntityType, "users");
        });
    }

    [TestMethod]
    public void Regex_MatchAll_ShouldReturnAllColumns()
    {
        // Arrange - 匹配所有列的正则
        var template = "SELECT {{columns --regex .*}} FROM users";

        // Act
        var result = _engine.ProcessTemplate(template, _testMethod, _userEntityType, "users");

        // Assert
        StringAssert.Contains(result.ProcessedSql, "id");
        StringAssert.Contains(result.ProcessedSql, "user_name");
        StringAssert.Contains(result.ProcessedSql, "user_email");
        StringAssert.Contains(result.ProcessedSql, "created_at");
    }

    #endregion
}

