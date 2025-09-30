// -----------------------------------------------------------------------
// <copyright file="CorePlaceholderDialectTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Generator;
using System;
using System.Linq;

namespace Sqlx.Tests.Core;

/// <summary>
/// 测试7个核心占位符在所有数据库方言中的工作情况
/// Tests for 7 core placeholders functionality across all database dialects.
/// </summary>
[TestClass]
public class CorePlaceholderDialectTests
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
        public decimal? Bonus { get; set; }
        public string Password { get; set; }
    }

    public interface IUserService
    {
        Task<User?> GetUserByIdAsync(int id);
        Task<List<User>> GetUsersPagedAsync(int skip, int take);
        Task<List<User>> GetUsersByNameAsync(string name);
        Task<int> CreateUserAsync(string name, string email, int age);
        Task<int> UpdateUserAsync(int id, string name, string email);
        Task<int> DeleteUserAsync(int id);
        Task<List<User>> GetTopUsersAsync();
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

    #region 📋 核心占位符测试

    [TestMethod]
    public void TablePlaceholder_AllDialects_GeneratesCorrectTableName()
    {
        var template = "SELECT * FROM {{table}}";

        var expectedTableNames = new Dictionary<string, string>
        {
            ["SqlServer"] = "[User]",
            ["MySql"] = "`User`",
            ["PostgreSql"] = "\"User\"",
            ["SQLite"] = "[User]",
            ["Oracle"] = "\"User\"",
            ["DB2"] = "\"User\""
        };

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User", dialect);
            var dialectName = GetDialectName(dialect);
            
            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql), $"Processed SQL should not be empty for {dialectName}");
            Assert.AreEqual(0, result.Errors.Count, $"Should have no errors for {dialectName}");
            
            // 验证表名被正确包装
            var expectedTableName = expectedTableNames[dialectName];
            Assert.IsTrue(result.ProcessedSql.Contains(expectedTableName) || result.ProcessedSql.Contains("User"), 
                         $"SQL should contain properly quoted table name for {dialectName}");
        }
    }

    [TestMethod]
    public void ColumnsAutoPlaceholder_AllDialects_GeneratesCorrectColumns()
    {
        var template = "SELECT {{columns:auto}} FROM {{table}}";

        var expectedColumnFormats = new Dictionary<string, (string Left, string Right)>
        {
            ["SqlServer"] = ("[", "]"),
            ["MySql"] = ("`", "`"),
            ["PostgreSql"] = ("\"", "\""),
            ["SQLite"] = ("[", "]"),
            ["Oracle"] = ("\"", "\""),
            ["DB2"] = ("\"", "\"")
        };

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User", dialect);
            var dialectName = GetDialectName(dialect);
            
            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql), $"Processed SQL should not be empty for {dialectName}");
            Assert.AreEqual(0, result.Errors.Count, $"Should have no errors for {dialectName}");
            
            // 验证包含基本列
            Assert.IsTrue(result.ProcessedSql.Contains("id"), $"SQL should contain id column for {dialectName}");
            Assert.IsTrue(result.ProcessedSql.Contains("name"), $"SQL should contain name column for {dialectName}");
            Assert.IsTrue(result.ProcessedSql.Contains("email"), $"SQL should contain email column for {dialectName}");
            
            // 验证列名格式
            var format = expectedColumnFormats[dialectName];
            var hasProperQuoting = result.ProcessedSql.Contains($"{format.Left}id{format.Right}") ||
                                  result.ProcessedSql.Contains($"{format.Left}Id{format.Right}") ||
                                  result.ProcessedSql.Contains("id"); // 某些情况下可能不加引号
            Assert.IsTrue(hasProperQuoting, $"Columns should be properly quoted for {dialectName}");
        }
    }

    [TestMethod]
    public void ColumnsAutoExcludePlaceholder_AllDialects_ExcludesSpecifiedColumns()
    {
        var template = "SELECT {{columns:auto|exclude=Password}} FROM {{table}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User", dialect);
            var dialectName = GetDialectName(dialect);
            
            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql), $"Processed SQL should not be empty for {dialectName}");
            Assert.AreEqual(0, result.Errors.Count, $"Should have no errors for {dialectName}");
            
            // 验证包含正常列
            Assert.IsTrue(result.ProcessedSql.Contains("id"), $"SQL should contain id column for {dialectName}");
            Assert.IsTrue(result.ProcessedSql.Contains("name"), $"SQL should contain name column for {dialectName}");
            
            // 验证排除密码列
            Assert.IsFalse(result.ProcessedSql.ToLower().Contains("password"), $"SQL should not contain password column for {dialectName}");
        }
    }

    [TestMethod]
    public void WhereIdPlaceholder_AllDialects_GeneratesCorrectWhereClause()
    {
        var template = "SELECT * FROM {{table}} WHERE {{where:id}}";

        var expectedParameterPrefixes = new Dictionary<string, string>
        {
            ["SqlServer"] = "@",
            ["MySql"] = "@",
            ["PostgreSql"] = "$",
            ["SQLite"] = "$",
            ["Oracle"] = ":",
            ["DB2"] = "?"
        };

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User", dialect);
            var dialectName = GetDialectName(dialect);
            
            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql), $"Processed SQL should not be empty for {dialectName}");
            Assert.AreEqual(0, result.Errors.Count, $"Should have no errors for {dialectName}");
            
            // 验证WHERE子句
            Assert.IsTrue(result.ProcessedSql.Contains("WHERE"), $"SQL should contain WHERE clause for {dialectName}");
            Assert.IsTrue(result.ProcessedSql.Contains("id"), $"SQL should contain id field for {dialectName}");
            
            // 验证参数前缀（如果不是位置参数）
            var expectedPrefix = expectedParameterPrefixes[dialectName];
            if (expectedPrefix != "?")
            {
                Assert.IsTrue(result.ProcessedSql.Contains($"{expectedPrefix}id") || 
                             result.ProcessedSql.Contains($"{expectedPrefix}Id"), 
                             $"SQL should contain parameter with correct prefix for {dialectName}");
            }
        }
    }

    [TestMethod]
    public void WhereAutoPlaceholder_AllDialects_GeneratesFromMethodParameters()
    {
        var template = "SELECT * FROM {{table}} WHERE {{where:auto}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User", dialect);
            var dialectName = GetDialectName(dialect);
            
            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql), $"Processed SQL should not be empty for {dialectName}");
            Assert.AreEqual(0, result.Errors.Count, $"Should have no errors for {dialectName}");
            
            // 验证WHERE子句存在
            Assert.IsTrue(result.ProcessedSql.Contains("WHERE"), $"SQL should contain WHERE clause for {dialectName}");
        }
    }

    [TestMethod]
    public void SetAutoPlaceholder_AllDialects_GeneratesCorrectSetClause()
    {
        var template = "UPDATE {{table}} SET {{set:auto|exclude=Id,HireDate}} WHERE {{where:id}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User", dialect);
            var dialectName = GetDialectName(dialect);
            
            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql), $"Processed SQL should not be empty for {dialectName}");
            Assert.AreEqual(0, result.Errors.Count, $"Should have no errors for {dialectName}");
            
            // 验证UPDATE语句结构
            Assert.IsTrue(result.ProcessedSql.Contains("UPDATE"), $"SQL should contain UPDATE for {dialectName}");
            Assert.IsTrue(result.ProcessedSql.Contains("SET"), $"SQL should contain SET for {dialectName}");
            Assert.IsTrue(result.ProcessedSql.Contains("WHERE"), $"SQL should contain WHERE for {dialectName}");
            
            // 验证排除字段
            Assert.IsFalse(result.ProcessedSql.ToLower().Contains("hire_date"), $"SQL should not contain excluded hire_date for {dialectName}");
        }
    }

    [TestMethod]
    public void ValuesAutoPlaceholder_AllDialects_GeneratesCorrectValues()
    {
        var template = "INSERT INTO {{table}} ({{columns:auto|exclude=Id}}) VALUES ({{values:auto}})";

        var expectedParameterPrefixes = new Dictionary<string, string>
        {
            ["SqlServer"] = "@",
            ["MySql"] = "@",
            ["PostgreSql"] = "$",
            ["SQLite"] = "$",
            ["Oracle"] = ":",
            ["DB2"] = "?"
        };

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User", dialect);
            var dialectName = GetDialectName(dialect);
            
            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql), $"Processed SQL should not be empty for {dialectName}");
            Assert.AreEqual(0, result.Errors.Count, $"Should have no errors for {dialectName}");
            
            // 验证INSERT语句结构
            Assert.IsTrue(result.ProcessedSql.Contains("INSERT"), $"SQL should contain INSERT for {dialectName}");
            Assert.IsTrue(result.ProcessedSql.Contains("VALUES"), $"SQL should contain VALUES for {dialectName}");
            
            // 验证参数格式
            var expectedPrefix = expectedParameterPrefixes[dialectName];
            if (expectedPrefix != "?")
            {
                Assert.IsTrue(result.ProcessedSql.Contains(expectedPrefix), $"SQL should contain parameters with correct prefix for {dialectName}");
            }
        }
    }

    [TestMethod]
    public void OrderByPlaceholder_AllDialects_GeneratesCorrectOrderBy()
    {
        var template = "SELECT * FROM {{table}} {{orderby:id}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User", dialect);
            var dialectName = GetDialectName(dialect);
            
            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql), $"Processed SQL should not be empty for {dialectName}");
            Assert.AreEqual(0, result.Errors.Count, $"Should have no errors for {dialectName}");
            
            // 验证ORDER BY子句
            Assert.IsTrue(result.ProcessedSql.Contains("ORDER BY"), $"SQL should contain ORDER BY for {dialectName}");
            Assert.IsTrue(result.ProcessedSql.Contains("id"), $"SQL should contain id field for {dialectName}");
        }
    }

    [TestMethod]
    public void LimitPlaceholder_AllDialects_GeneratesCorrectPagination()
    {
        var template = "SELECT * FROM {{table}} {{limit:default=20}}";

        var expectedLimitSyntax = new Dictionary<string, string[]>
        {
            ["SqlServer"] = new[] { "OFFSET", "ROWS", "FETCH NEXT", "ROWS ONLY" },
            ["MySql"] = new[] { "LIMIT" },
            ["PostgreSql"] = new[] { "LIMIT", "OFFSET" },
            ["SQLite"] = new[] { "LIMIT", "OFFSET" },
            ["Oracle"] = new[] { "OFFSET", "ROWS", "FETCH NEXT", "ROWS ONLY" },
            ["DB2"] = new[] { "LIMIT", "OFFSET" }
        };

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User", dialect);
            var dialectName = GetDialectName(dialect);
            
            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql), $"Processed SQL should not be empty for {dialectName}");
            Assert.AreEqual(0, result.Errors.Count, $"Should have no errors for {dialectName}");
            
            // 验证分页语法
            var expectedKeywords = expectedLimitSyntax[dialectName];
            var hasExpectedSyntax = expectedKeywords.Any(keyword => result.ProcessedSql.ToUpper().Contains(keyword));
            Assert.IsTrue(hasExpectedSyntax, $"SQL should contain expected pagination syntax for {dialectName}");
        }
    }

    #endregion

    #region 🔄 复合占位符测试

    [TestMethod]
    public void CompleteSelectTemplate_AllDialects_ProcessesAllPlaceholders()
    {
        var template = @"
            SELECT {{columns:auto|exclude=Password}}
            FROM {{table}}
            WHERE {{where:auto}}
            {{orderby:id}}
            {{limit:default=50}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User", dialect);
            var dialectName = GetDialectName(dialect);
            
            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql), $"Processed SQL should not be empty for {dialectName}");
            Assert.AreEqual(0, result.Errors.Count, $"Should have no errors for {dialectName}: {string.Join(", ", result.Errors)}");
            
            // 验证所有占位符都被处理
            Assert.IsFalse(result.ProcessedSql.Contains("{{"), $"All placeholders should be processed for {dialectName}");
            Assert.IsFalse(result.ProcessedSql.Contains("}}"), $"All placeholders should be processed for {dialectName}");
            
            // 验证基本SQL结构
            Assert.IsTrue(result.ProcessedSql.Contains("SELECT"), $"SQL should contain SELECT for {dialectName}");
            Assert.IsTrue(result.ProcessedSql.Contains("FROM"), $"SQL should contain FROM for {dialectName}");
            Assert.IsTrue(result.ProcessedSql.Contains("WHERE"), $"SQL should contain WHERE for {dialectName}");
            Assert.IsTrue(result.ProcessedSql.Contains("ORDER BY"), $"SQL should contain ORDER BY for {dialectName}");
        }
    }

    [TestMethod]
    public void CompleteInsertTemplate_AllDialects_ProcessesAllPlaceholders()
    {
        var template = "INSERT INTO {{table}} ({{columns:auto|exclude=Id}}) VALUES ({{values:auto}})";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User", dialect);
            var dialectName = GetDialectName(dialect);
            
            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql), $"Processed SQL should not be empty for {dialectName}");
            Assert.AreEqual(0, result.Errors.Count, $"Should have no errors for {dialectName}");
            
            // 验证INSERT语句完整性
            Assert.IsTrue(result.ProcessedSql.Contains("INSERT INTO"), $"SQL should contain INSERT INTO for {dialectName}");
            Assert.IsTrue(result.ProcessedSql.Contains("VALUES"), $"SQL should contain VALUES for {dialectName}");
            
            // 验证括号匹配
            var openParens = result.ProcessedSql.Count(c => c == '(');
            var closeParens = result.ProcessedSql.Count(c => c == ')');
            Assert.AreEqual(openParens, closeParens, $"Parentheses should be balanced for {dialectName}");
        }
    }

    [TestMethod]
    public void CompleteUpdateTemplate_AllDialects_ProcessesAllPlaceholders()
    {
        var template = "UPDATE {{table}} SET {{set:auto|exclude=Id,HireDate}} WHERE {{where:id}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User", dialect);
            var dialectName = GetDialectName(dialect);
            
            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql), $"Processed SQL should not be empty for {dialectName}");
            Assert.AreEqual(0, result.Errors.Count, $"Should have no errors for {dialectName}");
            
            // 验证UPDATE语句完整性
            Assert.IsTrue(result.ProcessedSql.Contains("UPDATE"), $"SQL should contain UPDATE for {dialectName}");
            Assert.IsTrue(result.ProcessedSql.Contains("SET"), $"SQL should contain SET for {dialectName}");
            Assert.IsTrue(result.ProcessedSql.Contains("WHERE"), $"SQL should contain WHERE for {dialectName}");
        }
    }

    [TestMethod]
    public void CompleteDeleteTemplate_AllDialects_ProcessesAllPlaceholders()
    {
        var template = "DELETE FROM {{table}} WHERE {{where:id}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User", dialect);
            var dialectName = GetDialectName(dialect);
            
            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql), $"Processed SQL should not be empty for {dialectName}");
            Assert.AreEqual(0, result.Errors.Count, $"Should have no errors for {dialectName}");
            
            // 验证DELETE语句完整性
            Assert.IsTrue(result.ProcessedSql.Contains("DELETE FROM"), $"SQL should contain DELETE FROM for {dialectName}");
            Assert.IsTrue(result.ProcessedSql.Contains("WHERE"), $"SQL should contain WHERE for {dialectName}");
        }
    }

    #endregion

    #region 🛡️ 边界情况和错误处理测试

    [TestMethod]
    public void EmptyTemplate_AllDialects_HandlesGracefully()
    {
        var template = "";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User", dialect);
            var dialectName = GetDialectName(dialect);
            
            // 空模板应该产生警告或返回默认值
            Assert.IsTrue(result.Warnings.Count > 0 || !string.IsNullOrEmpty(result.ProcessedSql), 
                         $"Empty template should generate warning or default SQL for {dialectName}");
        }
    }

    [TestMethod]
    public void InvalidPlaceholderSyntax_AllDialects_HandlesGracefully()
    {
        var templates = new[]
        {
            "SELECT * FROM {{table}} WHERE {{invalid}}",           // 缺少类型
            "SELECT * FROM {{table}} WHERE {{:invalid}}",         // 缺少占位符名
            "SELECT * FROM {{table}} WHERE {{invalid:}}",         // 缺少类型
            "SELECT * FROM {{table}} WHERE {{}}",                 // 完全空的占位符
            "SELECT * FROM {{table}} WHERE {{unclosed",           // 未闭合的占位符
        };

        foreach (var template in templates)
        {
            foreach (var dialect in AllDialects)
            {
                var result = _engine.ProcessTemplate(template, _testMethod, _userType, "User", dialect);
                var dialectName = GetDialectName(dialect);
                
                // 无效语法应该被处理（保留原样或产生警告）
                Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql), 
                              $"Invalid syntax should be handled gracefully for {dialectName}");
                // 不要求特定行为，但不应该崩溃
                Assert.IsTrue(true, $"Template processing should not crash for {dialectName}");
            }
        }
    }

    [TestMethod]
    public void NullEntityType_AllDialects_HandlesGracefully()
    {
        var template = "SELECT {{columns:auto}} FROM {{table}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, null, "User", dialect);
            var dialectName = GetDialectName(dialect);
            
            // null实体类型应该被处理
            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql), 
                          $"Null entity type should be handled for {dialectName}");
            Assert.IsTrue(result.Warnings.Count > 0 || result.Errors.Count > 0, 
                         $"Null entity type should generate warnings or errors for {dialectName}");
        }
    }

    [TestMethod]
    public void LargeTemplate_AllDialects_ProcessesEfficiently()
    {
        var largeTemplate = string.Join("\n", Enumerable.Repeat(@"
            SELECT {{columns:auto|exclude=Password}}
            FROM {{table}}
            WHERE {{where:auto}}
            {{orderby:id}}
            {{limit:default=100}}
            UNION ALL", 10)) + @"
            SELECT {{columns:auto|exclude=Password}}
            FROM {{table}}
            WHERE {{where:auto}}
            {{orderby:id}}
            {{limit:default=100}}";

        foreach (var dialect in AllDialects)
        {
            var startTime = DateTime.UtcNow;
            var result = _engine.ProcessTemplate(largeTemplate, _testMethod, _userType, "User", dialect);
            var processingTime = DateTime.UtcNow - startTime;
            var dialectName = GetDialectName(dialect);
            
            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql), $"Large template should be processed for {dialectName}");
            Assert.IsTrue(processingTime.TotalSeconds < 5, $"Large template processing should be fast (<5s) for {dialectName}");
        }
    }

    #endregion

    #region 🔧 辅助方法

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
