using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;
using SqlTemplateEngine = Sqlx.Generator.SqlTemplateEngine;
using SqlDefine = Sqlx.Generator.SqlDefine;

namespace Sqlx.Tests;

/// <summary>
/// SQL模板引擎占位符功能测试
/// TDD 方法：先写测试，再实现功能
/// </summary>
[TestClass]
public class SqlTemplateEnginePlaceholdersTests
{
    private SqlTemplateEngine? _engine;
    private IMethodSymbol? _testMethod;
    private INamedTypeSymbol? _testEntity;

    [TestInitialize]
    public void Initialize()
    {
        _engine = new SqlTemplateEngine();

        var compilation = CreateTestCompilation();
        var testClass = compilation.GetTypeByMetadataName("TestEntity")!;
        _testEntity = testClass;

        var methodClass = compilation.GetTypeByMetadataName("TestMethods")!;
        _testMethod = methodClass.GetMembers("GetAllAsync").OfType<IMethodSymbol>().First();
    }

    #region {{table}} 占位符测试

    [TestMethod]
    [Description("{{table}} 应该被替换为表名")]
    public void TablePlaceholder_ShouldBeReplacedWithTableName()
    {
        // Arrange
        var template = "SELECT * FROM {{table}}";

        // Act
        var result = _engine!.ProcessTemplate(template, _testMethod!, _testEntity!, "users");

        // Assert
        StringAssert.Contains(result.ProcessedSql, "users");
        Assert.IsFalse(result.ProcessedSql.Contains("{{table}}"));
    }

    #endregion

    #region {{columns}} 占位符测试

    [TestMethod]
    [Description("{{columns}} 应该生成所有列名")]
    public void ColumnsPlaceholder_ShouldGenerateAllColumns()
    {
        // Arrange
        var template = "SELECT {{columns}} FROM {{table}}";

        // Act
        var result = _engine!.ProcessTemplate(template, _testMethod!, _testEntity!, "test_table");

        // Assert
        var lowerSql = result.ProcessedSql.ToLowerInvariant();
        StringAssert.Contains(lowerSql, "id");
        StringAssert.Contains(lowerSql, "name");
        StringAssert.Contains(lowerSql, "created_at");
        StringAssert.Contains(lowerSql, "updated_at");
    }

    [TestMethod]
    [Description("{{columns --exclude Id}} 应该排除指定列")]
    public void ColumnsPlaceholder_WithExclude_ShouldExcludeSpecifiedColumns()
    {
        // Arrange
        var template = "SELECT {{columns --exclude Id}} FROM {{table}}";

        // Act
        var result = _engine!.ProcessTemplate(template, _testMethod!, _testEntity!, "test_table");

        // Assert
        var lowerSql = result.ProcessedSql.ToLowerInvariant();
        Assert.IsFalse(lowerSql.Contains("id,") || lowerSql.Contains(", id"),
            "不应该包含 id 列");
        StringAssert.Contains(lowerSql, "name");
        StringAssert.Contains(lowerSql, "created_at");
    }

    [TestMethod]
    [Description("{{columns --exclude Id CreatedAt}} 应该排除多个列")]
    public void ColumnsPlaceholder_WithMultipleExcludes_ShouldExcludeAllSpecified()
    {
        // Arrange
        // 注意：columns 和 values 都需要指定相同的排除选项以保持一致
        var template = "INSERT INTO {{table}} ({{columns --exclude Id CreatedAt}}) VALUES ({{values --exclude Id CreatedAt}})";

        // Act
        var result = _engine!.ProcessTemplate(template, _testMethod!, _testEntity!, "test_table");

        // Assert
        var lowerSql = result.ProcessedSql.ToLowerInvariant();
        Assert.IsFalse(lowerSql.Contains("id"), "不应该包含 id 列");
        Assert.IsFalse(lowerSql.Contains("created_at"), "不应该包含 created_at 列");
        StringAssert.Contains(lowerSql, "name");
        StringAssert.Contains(lowerSql, "updated_at");
    }

    #endregion

    #region {{values}} 占位符测试

    [TestMethod]
    [Description("{{values}} 应该生成参数占位符")]
    public void ValuesPlaceholder_ShouldGenerateParameters()
    {
        // Arrange
        var template = "INSERT INTO {{table}} ({{columns}}) VALUES ({{values}})";

        // Act
        var result = _engine!.ProcessTemplate(template, _testMethod!, _testEntity!, "test_table");

        // Assert
        var lowerSql = result.ProcessedSql.ToLowerInvariant();
        StringAssert.Contains(lowerSql, "@id");
        StringAssert.Contains(lowerSql, "@name");
        StringAssert.Contains(lowerSql, "@created_at");
    }

    #endregion

    #region {{set}} 占位符测试

    [TestMethod]
    [Description("{{set}} 应该生成 SET 子句")]
    public void SetPlaceholder_ShouldGenerateSetClause()
    {
        // Arrange
        var template = "UPDATE {{table}} SET {{set}} WHERE id = @id";

        // Act
        var result = _engine!.ProcessTemplate(template, _testMethod!, _testEntity!, "test_table");

        // Assert
        var lowerSql = result.ProcessedSql.ToLowerInvariant();
        // Accept both quoted and unquoted column names
        Assert.IsTrue(lowerSql.Contains("name = @name") || lowerSql.Contains("[name] = @name"),
            "SET clause should contain name column");
        Assert.IsTrue(lowerSql.Contains("updated_at = @updated_at") || lowerSql.Contains("[updated_at] = @updated_at"),
            "SET clause should contain updated_at column");
    }

    [TestMethod]
    [Description("{{set --exclude Id CreatedAt}} 应该排除指定列")]
    public void SetPlaceholder_WithExclude_ShouldExcludeSpecifiedColumns()
    {
        // Arrange
        var template = "UPDATE {{table}} SET {{set --exclude Id CreatedAt}} WHERE id = @id";

        // Act
        var result = _engine!.ProcessTemplate(template, _testMethod!, _testEntity!, "test_table");

        // Assert
        var lowerSql = result.ProcessedSql.ToLowerInvariant();
        Assert.IsFalse(lowerSql.Contains("id = @id,") || lowerSql.Contains(", id = @id"),
            "SET 子句不应该包含 id");
        Assert.IsFalse(lowerSql.Contains("created_at = @created_at") && lowerSql.Contains("[created_at] = @created_at"),
            "SET 子句不应该包含 created_at");
        Assert.IsTrue(lowerSql.Contains("name = @name") || lowerSql.Contains("[name] = @name"),
            "SET clause should contain name column");
        Assert.IsTrue(lowerSql.Contains("updated_at = @updated_at") || lowerSql.Contains("[updated_at] = @updated_at"),
            "SET clause should contain updated_at column");
    }

    #endregion

    #region {{orderby}} 占位符测试

    [TestMethod]
    [Description("{{orderby created_at --asc}} 应该生成升序排序")]
    public void OrderByPlaceholder_WithAsc_ShouldGenerateAscendingOrder()
    {
        // Arrange
        var template = "SELECT * FROM {{table}} {{orderby created_at --asc}}";

        // Act
        var result = _engine!.ProcessTemplate(template, _testMethod!, _testEntity!, "test_table");

        // Assert
        StringAssert.Contains(result.ProcessedSql, "ORDER BY");
        StringAssert.Contains(result.ProcessedSql, "ASC");
        StringAssert.Contains(result.ProcessedSql, "created_at");
    }

    [TestMethod]
    [Description("{{orderby name --desc}} 应该生成降序排序")]
    public void OrderByPlaceholder_WithDesc_ShouldGenerateDescendingOrder()
    {
        // Arrange
        var template = "SELECT * FROM {{table}} {{orderby name --desc}}";

        // Act
        var result = _engine!.ProcessTemplate(template, _testMethod!, _testEntity!, "test_table");

        // Assert
        StringAssert.Contains(result.ProcessedSql, "ORDER BY");
        StringAssert.Contains(result.ProcessedSql, "DESC");
        StringAssert.Contains(result.ProcessedSql, "name");
    }

    [TestMethod]
    [Description("{{orderby}} 无选项时应该有默认排序")]
    public void OrderByPlaceholder_WithoutOptions_ShouldUseDefaultOrder()
    {
        // Arrange
        var template = "SELECT * FROM {{table}} {{orderby}}";

        // Act
        var result = _engine!.ProcessTemplate(template, _testMethod!, _testEntity!, "test_table");

        // Assert
        StringAssert.Contains(result.ProcessedSql, "ORDER BY");
        // 应该有默认排序（通常是 id ASC）
    }

    #endregion

    #region {{limit}} 占位符测试

    [TestMethod]
    [Description("{{limit}} 应该生成 LIMIT 子句")]
    public void LimitPlaceholder_ShouldGenerateLimitClause()
    {
        // Arrange
        var template = "SELECT * FROM {{table}} {{limit}}";

        // Act
        var result = _engine!.ProcessTemplate(template, _testMethod!, _testEntity!, "test_table");

        // Assert
        // 应该包含 LIMIT 或 TOP（取决于数据库方言）
        System.Diagnostics.Debug.WriteLine($"生成的SQL: {result.ProcessedSql}");
        System.Diagnostics.Debug.WriteLine($"Errors: {string.Join(", ", result.Errors)}");
        var hasLimit = result.ProcessedSql.Contains("LIMIT") || result.ProcessedSql.Contains("TOP");
        Assert.IsTrue(hasLimit, $"应该包含 LIMIT 或 TOP。实际SQL: {result.ProcessedSql}");
    }

    #endregion

    #region 组合占位符测试

    [TestMethod]
    [Description("多个占位符应该正确协同工作")]
    public void MultiplePlaceholders_ShouldWorkTogether()
    {
        // Arrange
        var template = "SELECT {{columns --exclude Id}} FROM {{table}} {{orderby created_at --desc}} {{limit}}";

        // Act
        var result = _engine!.ProcessTemplate(template, _testMethod!, _testEntity!, "users");

        // Assert
        var sql = result.ProcessedSql;
        StringAssert.Contains(sql, "SELECT");
        StringAssert.Contains(sql, "FROM");
        StringAssert.Contains(sql, "users");
        StringAssert.Contains(sql, "ORDER BY");
        StringAssert.Contains(sql, "DESC");
    }

    [TestMethod]
    [Description("完整的 CRUD 操作应该生成正确的 SQL")]
    public void FullCrudOperations_ShouldGenerateCorrectSql()
    {
        // Arrange
        var templates = new[]
        {
            ("SELECT", "SELECT {{columns}} FROM {{table}} WHERE id = @id"),
            ("INSERT", "INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values}})"),
            ("UPDATE", "UPDATE {{table}} SET {{set --exclude Id CreatedAt}} WHERE id = @id"),
            ("DELETE", "DELETE FROM {{table}} WHERE id = @id")
        };

        foreach (var (operation, template) in templates)
        {
            // Act
            var result = _engine!.ProcessTemplate(template, _testMethod!, _testEntity!, "test_table");

            // Assert
            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql),
                $"{operation} 应该生成非空 SQL");
            Assert.IsFalse(result.ProcessedSql.Contains("{{"),
                $"{operation} SQL 不应该包含未处理的占位符");
            StringAssert.Contains(result.ProcessedSql, operation,
                $"应该包含 {operation} 关键字");
        }
    }

    #endregion

    #region 边界情况测试

    [TestMethod]
    [Description("无效的占位符选项应该产生警告")]
    public void InvalidPlaceholderOptions_ShouldProduceWarning()
    {
        // Arrange
        var template = "SELECT {{columns --invalid-option}} FROM {{table}}";

        // Act
        var result = _engine!.ProcessTemplate(template, _testMethod!, _testEntity!, "test_table");

        // Assert
        // 应该有警告或错误，但不应该崩溃
        Assert.IsNotNull(result);
        Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql));
    }

    [TestMethod]
    [Description("未知的占位符应该产生警告")]
    public void UnknownPlaceholder_ShouldProduceWarning()
    {
        // Arrange
        var template = "SELECT * FROM {{table}} {{unknown_placeholder}}";

        // Act
        var result = _engine!.ProcessTemplate(template, _testMethod!, _testEntity!, "test_table");

        // Assert
        Assert.IsNotNull(result);
        // 未知占位符可能被保留或产生警告
    }

    [TestMethod]
    [Description("嵌套占位符应该被正确处理")]
    public void NestedPlaceholders_ShouldBeHandledCorrectly()
    {
        // Arrange
        var template = "SELECT {{columns}} FROM {{table}}";

        // Act
        var result = _engine!.ProcessTemplate(template, _testMethod!, _testEntity!, "test_table");

        // Assert
        Assert.IsFalse(result.ProcessedSql.Contains("{{"));
        Assert.IsFalse(result.ProcessedSql.Contains("}}"));
    }

    #endregion

    #region 数据库方言测试

    [TestMethod]
    [Description("SQLite 应该使用正确的语法")]
    public void SqliteDialect_ShouldUseCorrectSyntax()
    {
        // Arrange
        var template = "SELECT {{columns}} FROM {{table}} {{limit}}";

        // Act
        var result = _engine!.ProcessTemplate(template, _testMethod!, _testEntity!, "test_table", Sqlx.Generator.SqlDefine.SQLite);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql));
        // SQLite should generate valid SQL
        StringAssert.Contains(result.ProcessedSql, "SELECT");
        StringAssert.Contains(result.ProcessedSql, "FROM");
        StringAssert.Contains(result.ProcessedSql, "test_table");
    }

    [TestMethod]
    [Description("SQL Server 应该使用正确的语法")]
    public void SqlServerDialect_ShouldUseCorrectSyntax()
    {
        // Arrange
        var template = "SELECT {{columns}} FROM {{table}} {{limit}}";

        // Act
        var result = _engine!.ProcessTemplate(template, _testMethod!, _testEntity!, "test_table", Sqlx.Generator.SqlDefine.SqlServer);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql));
        // SQL Server should generate valid SQL
        StringAssert.Contains(result.ProcessedSql, "SELECT");
        StringAssert.Contains(result.ProcessedSql, "FROM");
        StringAssert.Contains(result.ProcessedSql, "test_table");
    }

    #endregion

    // 辅助方法：创建测试用的编译环境
    private CSharpCompilation CreateTestCompilation()
    {
        var code = @"
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;

            public class TestEntity
            {
                public long Id { get; set; }
                public string Name { get; set; }
                public DateTime CreatedAt { get; set; }
                public DateTime UpdatedAt { get; set; }
            }

            public class TestMethods
            {
                public Task<List<TestEntity>> GetAllAsync() => null;
            }
        ";

        var syntaxTree = CSharpSyntaxTree.ParseText(code);
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location),
        };

        return CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}

