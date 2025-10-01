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

    [TestMethod]
    public void SensitiveFieldExclusion_AllDialects_ExcludesByDefault()
    {
        var template = "SELECT {{columns:auto}} FROM {{table}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql), $"Should generate SQL for {dialectName}");

            // 验证敏感字段不在结果中
            var sql = result.ProcessedSql.ToLower();
            Assert.IsFalse(sql.Contains("password"), $"Should not include password field for {dialectName}");
            Assert.IsFalse(sql.Contains("security_token"), $"Should not include security_token field for {dialectName}");
            Assert.IsFalse(sql.Contains("credit_card"), $"Should not include credit_card field for {dialectName}");
        }
    }

    [TestMethod]
    public void ExplicitSensitiveFieldInclusion_AllDialects_RequiresExplicitRequest()
    {
        var template = "SELECT {{columns:auto|include=Password}} FROM {{table}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User", dialect);
            var dialectName = GetDialectName(dialect);

            // 即使显式包含，也应该产生警告
            Assert.IsTrue(result.Warnings.Count > 0, $"Should warn about including sensitive fields for {dialectName}");
        }
    }

    #endregion

    #region 🔐 参数安全测试

    [TestMethod]
    public void ParameterPrefix_AllDialects_UsesCorrectDialectSpecificPrefix()
    {
        var template = "SELECT * FROM {{table}} WHERE {{where:id}} AND name = @name";

        var expectedPrefixes = new Dictionary<string, string[]>
        {
            ["SqlServer"] = new[] { "@" },
            ["MySql"] = new[] { "@" },
            ["PostgreSql"] = new[] { "$", "@" }, // 支持两种
            ["SQLite"] = new[] { "$", "@" }, // 支持两种
            ["Oracle"] = new[] { ":" },
            ["DB2"] = new[] { "?" }
        };

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql), $"Should generate SQL for {dialectName}");

            var allowedPrefixes = expectedPrefixes[dialectName];
            var hasCorrectPrefix = allowedPrefixes.Any(prefix => result.ProcessedSql.Contains(prefix));

            Assert.IsTrue(hasCorrectPrefix, $"Should use correct parameter prefix for {dialectName}. SQL: {result.ProcessedSql}");
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
    public void NullAndEmptyInputs_AllDialects_HandlesSafely()
    {
        var problematicInputs = new[]
        {
            (Template: null, TableName: "User", Description: "null template"),
            (Template: "", TableName: "User", Description: "empty template"),
            (Template: "   ", TableName: "User", Description: "whitespace template"),
            (Template: "SELECT * FROM {{table}}", TableName: null, Description: "null table name"),
            (Template: "SELECT * FROM {{table}}", TableName: "", Description: "empty table name"),
            (Template: "SELECT * FROM {{table}}", TableName: "   ", Description: "whitespace table name"),
        };

        foreach (var input in problematicInputs)
        {
            foreach (var dialect in AllDialects)
            {
                try
                {
                    // 跳过null输入，因为它们会抛出ArgumentNullException
                    if (input.Template != null && input.TableName != null)
                    {
                        var result = _engine.ProcessTemplate(input.Template, _testMethod, _userType, input.TableName, dialect);
                        var dialectName = GetDialectName(dialect);

                        // 应该优雅处理有问题的输入
                        Assert.IsTrue(!string.IsNullOrEmpty(result.ProcessedSql) ||
                                     result.Errors.Count > 0 ||
                                     result.Warnings.Count > 0,
                                     $"Should handle {input.Description} gracefully for {dialectName}");
                    }
                    else
                    {
                        // 对于null输入，期望ArgumentNullException
                        Assert.ThrowsException<ArgumentNullException>(() =>
                            _engine.ProcessTemplate(input.Template!, _testMethod, _userType, input.TableName!, dialect));
                    }
                }
                catch (ArgumentNullException)
                {
                    // ArgumentNullException 是可以接受的
                    Assert.IsTrue(true, "ArgumentNullException is acceptable for null inputs");
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Should not throw unexpected exception for {input.Description}. Exception: {ex.Message}");
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

    [TestMethod]
    public void PlaceholderOptionValidation_AllDialects_ValidatesOptions()
    {
        var invalidOptionTemplates = new[]
        {
            "SELECT * FROM {{table}} WHERE {{between:age|min=|max=@maxAge}}", // 空值
            "SELECT * FROM {{table}} WHERE {{like:name|invalid_option=value}}", // 无效选项
            "SELECT * FROM {{table}} WHERE {{round:salary|decimals=abc}}", // 无效数值
            "SELECT * FROM {{table}} WHERE {{limit:invalid_type|default=20}}", // 无效类型
        };

        foreach (var template in invalidOptionTemplates)
        {
            foreach (var dialect in AllDialects)
            {
                var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User", dialect);
                var dialectName = GetDialectName(dialect);

                // 应该检测到无效选项并产生警告或错误
                Assert.IsTrue(result.Warnings.Count > 0 || result.Errors.Count > 0,
                             $"Should validate placeholder options for {dialectName}. Template: {template}");
            }
        }
    }

    [TestMethod]
    public void TypeMismatch_AllDialects_DetectsAndWarns()
    {
        var typeMismatchTemplates = new[]
        {
            "SELECT {{sum:name}} FROM {{table}}", // 对字符串字段求和
            "SELECT {{round:is_active|decimals=2}} FROM {{table}}", // 对布尔字段四舍五入
            "SELECT {{upper:age}} FROM {{table}}", // 对数字字段转大写
            "SELECT {{today:name}} FROM {{table}}", // 对字符串字段使用日期函数
        };

        foreach (var template in typeMismatchTemplates)
        {
            foreach (var dialect in AllDialects)
            {
                var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User", dialect);
                var dialectName = GetDialectName(dialect);

                // 类型不匹配应该产生警告
                Assert.IsTrue(result.Warnings.Count > 0,
                             $"Should warn about type mismatch for {dialectName}. Template: {template}");
            }
        }
    }

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
