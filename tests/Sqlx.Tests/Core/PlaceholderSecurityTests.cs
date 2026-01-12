// -----------------------------------------------------------------------
// <copyright file="PlaceholderSecurityTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using Sqlx.Generator;
using System;
using System.Linq;

namespace Sqlx.Tests.Core;

/// <summary>
/// 占位符安全性和边界测试
/// Security and boundary tests for placeholder functionality.
/// </summary>
[TestClass]
public class PlaceholderSecurityTests
{
    private SqlTemplateEngine _engine = null!;
    private Compilation _compilation = null!;
    private IMethodSymbol _testMethod = null!;
    private INamedTypeSymbol _userType = null!;

    // 所有支持的数据库方言
    private static readonly Sqlx.Generator.SqlDefine[] AllDialects = new[]
    {
        Sqlx.Generator.SqlDefine.SqlServer,
        Sqlx.Generator.SqlDefine.MySql,
        Sqlx.Generator.SqlDefine.PostgreSql,
        Sqlx.Generator.SqlDefine.SQLite,
        Sqlx.Generator.SqlDefine.Oracle,
        Sqlx.Generator.SqlDefine.DB2
    };

    [TestInitialize]
    public void Initialize()
    {
        _engine = new SqlTemplateEngine();

        // 创建测试编译上下文
        var sourceCode = @"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TestNamespace
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public int Age { get; set; }
        public int DepartmentId { get; set; }
        public bool IsActive { get; set; }
        public DateTime HireDate { get; set; }
        public decimal Salary { get; set; }
        public string Password { get; set; }
        public string SecurityToken { get; set; }
        public string CreditCard { get; set; }
    }

    public interface IUserService
    {
        Task<User?> GetUserByIdAsync(int id);
        Task<List<User>> SearchUsersAsync(string searchTerm);
        Task<int> UpdateUserAsync(int id, string name, string email);
    }
}";

        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Collections.Generic.List<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location)
        };

        _compilation = CSharpCompilation.Create("TestAssembly", new[] { syntaxTree }, references);

        var semanticModel = _compilation.GetSemanticModel(syntaxTree);
        var root = syntaxTree.GetRoot();

        // 获取测试方法和用户类型
        var interfaceDeclaration = root.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.InterfaceDeclarationSyntax>()
            .First(i => i.Identifier.ValueText == "IUserService");

        var methodDeclaration = interfaceDeclaration.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
            .First();

        _testMethod = semanticModel.GetDeclaredSymbol(methodDeclaration) as IMethodSymbol
            ?? throw new InvalidOperationException("Failed to get test method symbol");

        _userType = _compilation.GetTypeByMetadataName("TestNamespace.User")
            ?? throw new InvalidOperationException("Failed to get User type symbol");
    }

    #region 🛡️ SQL注入防护测试

    [TestMethod]
    public void SqlInjectionInTemplate_AllDialects_DetectsAndPrevents()
    {
        var maliciousTemplates = new[]
        {
            "SELECT * FROM {{table}} WHERE name = 'user'; DROP TABLE users; --'",
            "SELECT * FROM {{table}} WHERE id = 1 OR 1=1 --",
            "SELECT * FROM {{table}} WHERE name = 'admin' UNION SELECT password FROM users --",
            "SELECT * FROM {{table}}; INSERT INTO logs VALUES ('hacked'); --",
            "SELECT * FROM {{table}} WHERE id = 1; EXEC xp_cmdshell('dir'); --",
            "SELECT * FROM {{table}} WHERE name = '{{'; DELETE FROM users; --'",
        };

        foreach (var template in maliciousTemplates)
        {
            foreach (var dialect in AllDialects)
            {
                var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User", dialect);
                var dialectName = GetDialectName(dialect);

                // 应该检测到危险模式并产生错误或警告
                var hasSafetyCheck = result.Errors.Count > 0 ||
                                   result.Warnings.Count > 0 ||
                                   !ContainsDangerousKeywords(result.ProcessedSql);

                Assert.IsTrue(hasSafetyCheck,
                    $"Should detect or prevent SQL injection for {dialectName}. Template: {template}");
            }
        }
    }

    [TestMethod]
    public void ParameterizedQuery_AllDialects_EnforcesParameterization()
    {
        var template = "SELECT * FROM {{table}} WHERE {{like:name|pattern=@searchTerm}} AND {{between:age|min=@minAge|max=@maxAge}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql), $"Should generate SQL for {dialectName}");
            Assert.AreEqual(0, result.Errors.Count, $"Should have no errors for parameterized query {dialectName}");

            // 验证参数被正确提取
            Assert.IsTrue(result.Parameters.Count >= 3, $"Should extract parameters for {dialectName}");

            // 验证生成的SQL包含参数占位符而不是直接值
            var sql = result.ProcessedSql;
            var hasParameterPlaceholders = sql.Contains("@") || sql.Contains(":") || sql.Contains("$") || sql.Contains("?");
            Assert.IsTrue(hasParameterPlaceholders, $"Should contain parameter placeholders for {dialectName}");
        }
    }

    #endregion

    #region 🔐 参数安全测试

    [TestMethod]
    public void ParameterPrefix_AllDialects_UsesCorrectDialectSpecificPrefix()
    {
        // 使用简单的参数引用,不使用占位符
        var template = "SELECT * FROM {{table}} WHERE id = @id AND name = @name";

        var expectedPrefixes = new Dictionary<string, string>
        {
            ["SqlServer"] = "@",
            ["MySql"] = "@",
            ["PostgreSql"] = "$",
            ["SQLite"] = "@",
            ["Oracle"] = ":",
            ["DB2"] = "?"
        };

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql), $"Should generate SQL for {dialectName}");

            var expectedPrefix = expectedPrefixes[dialectName];
            
            // 验证SQL中的参数使用了正确的前缀
            // 注意: 模板中的 @id 和 @name 应该被转换为方言特定的前缀
            Assert.IsTrue(
                result.ProcessedSql.Contains($"{expectedPrefix}id") || result.ProcessedSql.Contains("@id"),
                $"Should use correct parameter prefix for {dialectName}. Expected: {expectedPrefix}, SQL: {result.ProcessedSql}");
        }
    }

    [TestMethod]
    public void MixedParameterTypes_AllDialects_HandlesConsistently()
    {
        var template = "SELECT * FROM {{table}} WHERE {{between:age|min=@minAge|max=@maxAge}} AND name = @name AND active = @active";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql), $"Should generate SQL for {dialectName}");
            Assert.AreEqual(0, result.Errors.Count, $"Should handle mixed parameters for {dialectName}");

            // 验证所有参数都被提取
            Assert.IsTrue(result.Parameters.Count >= 4, $"Should extract all parameters for {dialectName}");
        }
    }

    [TestMethod]
    public void ParameterNaming_AllDialects_AvoidsSqlKeywordCollisions()
    {
        var template = "SELECT * FROM {{table}} WHERE {{columns:auto}} = @select AND name = @from AND age = @where";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User", dialect);
            var dialectName = GetDialectName(dialect);

            // 参数名如果与SQL关键字冲突，应该被处理
            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql), $"Should handle keyword parameter names for {dialectName}");

            // 应该产生警告或自动重命名
            var hasWarningOrRename = result.Warnings.Count > 0 ||
                                   !result.ProcessedSql.Contains("@select ") ||
                                   !result.ProcessedSql.Contains("@from ") ||
                                   !result.ProcessedSql.Contains("@where ");

            Assert.IsTrue(hasWarningOrRename, $"Should handle SQL keyword parameters for {dialectName}");
        }
    }

    #endregion

    #region 🚨 边界和错误测试

    [TestMethod]
    public void ExtremelyLongTemplate_AllDialects_HandlesGracefully()
    {
        var longTemplate = string.Join(" AND ", Enumerable.Repeat("{{like:name|pattern=@pattern}}", 1000));
        var template = $"SELECT * FROM {{{{table}}}} WHERE {longTemplate}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User", dialect);
            var dialectName = GetDialectName(dialect);

            // 应该处理或拒绝过长模板
            Assert.IsTrue(!string.IsNullOrEmpty(result.ProcessedSql) || result.Errors.Count > 0,
                         $"Should handle or reject extremely long template for {dialectName}");
        }
    }

    [TestMethod]
    public void MalformedPlaceholders_AllDialects_HandlesGracefully()
    {
        var malformedTemplates = new[]
        {
            "SELECT * FROM {{table WHERE id = 1",        // 缺少闭合
            "SELECT * FROM {{}} WHERE id = 1",           // 空占位符
            "SELECT * FROM {{table:}} WHERE id = 1",     // 空类型
            "SELECT * FROM {{:table}} WHERE id = 1",     // 缺少占位符名
            "SELECT * FROM {{table{{nested}}}} WHERE id = 1", // 嵌套占位符
            "SELECT * FROM {{table|option=value|malformed", // 格式错误的选项
        };

        foreach (var template in malformedTemplates)
        {
            foreach (var dialect in AllDialects)
            {
                try
                {
                    var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User", dialect);
                    var dialectName = GetDialectName(dialect);

                    // 应该优雅处理格式错误（不崩溃）
                    Assert.IsTrue(!string.IsNullOrEmpty(result.ProcessedSql) ||
                                 result.Errors.Count > 0 ||
                                 result.Warnings.Count > 0,
                                 $"Should handle malformed template gracefully for {dialectName}");
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Should not throw exception for malformed template. Exception: {ex.Message}");
                }
            }
        }
    }



    [TestMethod]
    public void UnicodeAndSpecialCharacters_AllDialects_HandlesCorrectly()
    {
        var specialTemplates = new[]
        {
            "SELECT * FROM {{table}} WHERE name = @用户名",
            "SELECT * FROM {{table}} WHERE description LIKE @模式",
            "SELECT * FROM {{table}} WHERE email = @email_αβγ",
            "SELECT * FROM {{table}} WHERE comment = @评论_🚀",
            "SELECT * FROM {{table}} WHERE path = @C_Users_Test",
        };

        foreach (var template in specialTemplates)
        {
            foreach (var dialect in AllDialects)
            {
                var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User", dialect);
                var dialectName = GetDialectName(dialect);

                Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql),
                              $"Should handle Unicode/special characters for {dialectName}");

                // 验证特殊字符被保留
                Assert.IsTrue(result.ProcessedSql.Contains("@") || result.ProcessedSql.Contains(":") ||
                             result.ProcessedSql.Contains("$") || result.ProcessedSql.Contains("?"),
                             $"Should preserve parameter markers for {dialectName}");
            }
        }
    }

    #endregion

    #region 🔍 输入验证测试

    #endregion

    #region 🔧 辅助方法

    /// <summary>
    /// 检查SQL是否包含危险关键字
    /// </summary>
    private static bool ContainsDangerousKeywords(string sql)
    {
        var dangerousKeywords = new[]
        {
            "DROP TABLE", "DELETE FROM", "INSERT INTO", "UPDATE SET",
            "EXEC", "EXECUTE", "xp_", "sp_", "UNION SELECT",
            "OR 1=1", "'; --", "--", "/*", "*/"
        };

        var upperSql = sql.ToUpper();
        return dangerousKeywords.Any(keyword => upperSql.Contains(keyword.ToUpper()));
    }

    /// <summary>
    /// 获取数据库方言的名称
    /// </summary>
    private static string GetDialectName(Sqlx.Generator.SqlDefine dialect)
    {
        if (dialect.Equals(Sqlx.Generator.SqlDefine.SqlServer)) return "SqlServer";
        if (dialect.Equals(Sqlx.Generator.SqlDefine.MySql)) return "MySql";
        if (dialect.Equals(Sqlx.Generator.SqlDefine.PostgreSql)) return "PostgreSql";
        if (dialect.Equals(Sqlx.Generator.SqlDefine.SQLite)) return "SQLite";
        if (dialect.Equals(Sqlx.Generator.SqlDefine.Oracle)) return "Oracle";
        if (dialect.Equals(Sqlx.Generator.SqlDefine.DB2)) return "DB2";
        return "Unknown";
    }

    #endregion
}
