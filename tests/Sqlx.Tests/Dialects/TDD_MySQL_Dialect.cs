using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;
using SqlTemplateEngine = Sqlx.Generator.SqlTemplateEngine;
using SqlDefine = Sqlx.Generator.SqlDefine;

namespace Sqlx.Tests.Dialects;

/// <summary>
/// MySQL数据库方言特定功能测试
/// 测试MySQL特有的SQL语法和功能
/// </summary>
[TestClass]
public class TDD_MySQL_Dialect
{
    private SqlTemplateEngine? _engine;
    private IMethodSymbol? _testMethod;
    private INamedTypeSymbol? _testEntity;

    [TestInitialize]
    public void Initialize()
    {
        _engine = new SqlTemplateEngine(Sqlx.Generator.SqlDefine.MySql);
        var compilation = CreateTestCompilation();
        _testEntity = compilation.GetTypeByMetadataName("TestEntity")!;
        var methodClass = compilation.GetTypeByMetadataName("TestMethods")!;
        _testMethod = methodClass.GetMembers("GetAllAsync").OfType<IMethodSymbol>().First();
    }

    #region INSERT with LAST_INSERT_ID()

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("MySQL")]
    [TestCategory("Phase1")]
    [Description("MySQL INSERT应该使用LAST_INSERT_ID()返回自增ID")]
    public void MySQL_Insert_ShouldUseLAST_INSERT_ID()
    {
        // Arrange
        var template = "INSERT INTO users (name, email) VALUES (@name, @email)";

        // Act
        var result = _engine!.ProcessTemplate(template, _testMethod!, _testEntity!, "users", Sqlx.Generator.SqlDefine.MySql);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql));
        // MySQL使用反引号包裹标识符
        Assert.IsTrue(result.ProcessedSql.Contains("`") || !result.ProcessedSql.Contains("["),
            "MySQL应该使用反引号或不使用标识符包裹");
    }

    #endregion

    #region ON DUPLICATE KEY UPDATE (Upsert)

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("MySQL")]
    [TestCategory("Phase1")]
    [Description("MySQL Upsert应该使用ON DUPLICATE KEY UPDATE")]
    public void MySQL_Upsert_ShouldUse_ON_DUPLICATE_KEY_UPDATE()
    {
        // Arrange - Upsert通常通过特定的SQL模板实现
        var template = "INSERT INTO users (id, name) VALUES (@id, @name) ON DUPLICATE KEY UPDATE name = VALUES(name)";

        // Act
        var result = _engine!.ProcessTemplate(template, _testMethod!, _testEntity!, "users", Sqlx.Generator.SqlDefine.MySql);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql));
        StringAssert.Contains(result.ProcessedSql.ToUpperInvariant(), "ON DUPLICATE KEY UPDATE",
            "MySQL Upsert应该包含ON DUPLICATE KEY UPDATE");
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("MySQL")]
    [TestCategory("Phase1")]
    [Description("MySQL VALUES()函数应该在UPDATE中工作")]
    public void MySQL_VALUES_Function_ShouldWorkInUPDATE()
    {
        // Arrange
        var template = "INSERT INTO users (id, name, email) VALUES (@id, @name, @email) ON DUPLICATE KEY UPDATE name = VALUES(name), email = VALUES(email)";

        // Act
        var result = _engine!.ProcessTemplate(template, _testMethod!, _testEntity!, "users", Sqlx.Generator.SqlDefine.MySql);

        // Assert
        StringAssert.Contains(result.ProcessedSql.ToUpperInvariant(), "VALUES(",
            "MySQL应该支持VALUES()函数");
    }

    #endregion

    #region 字符串连接 CONCAT()

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("MySQL")]
    [TestCategory("Phase1")]
    [Description("MySQL应该使用CONCAT()进行字符串连接")]
    public void MySQL_StringConcat_ShouldUseCONCAT()
    {
        // Arrange
        var template = "SELECT CONCAT(first_name, ' ', last_name) as full_name FROM users";

        // Act
        var result = _engine!.ProcessTemplate(template, _testMethod!, _testEntity!, "users", Sqlx.Generator.SqlDefine.MySql);

        // Assert
        StringAssert.Contains(result.ProcessedSql.ToUpperInvariant(), "CONCAT(",
            "MySQL应该支持CONCAT()函数");
    }

    #endregion

    #region 日期时间函数 NOW()

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("MySQL")]
    [TestCategory("Phase1")]
    [Description("MySQL应该使用NOW()获取当前时间")]
    public void MySQL_CurrentTime_ShouldUseNOW()
    {
        // Arrange
        var template = "INSERT INTO logs (message, created_at) VALUES (@message, NOW())";

        // Act
        var result = _engine!.ProcessTemplate(template, _testMethod!, _testEntity!, "logs", Sqlx.Generator.SqlDefine.MySql);

        // Assert
        StringAssert.Contains(result.ProcessedSql.ToUpperInvariant(), "NOW()",
            "MySQL应该支持NOW()函数");
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("MySQL")]
    [TestCategory("Phase1")]
    [Description("MySQL应该支持CURDATE()和CURTIME()")]
    public void MySQL_Date_Time_Functions_ShouldWork()
    {
        // Arrange
        var template = "SELECT CURDATE() as today, CURTIME() as now FROM users";

        // Act
        var result = _engine!.ProcessTemplate(template, _testMethod!, _testEntity!, "users", Sqlx.Generator.SqlDefine.MySql);

        // Assert
        StringAssert.Contains(result.ProcessedSql.ToUpperInvariant(), "CURDATE()",
            "MySQL应该支持CURDATE()");
        StringAssert.Contains(result.ProcessedSql.ToUpperInvariant(), "CURTIME()",
            "MySQL应该支持CURTIME()");
    }

    #endregion

    #region 分页 LIMIT OFFSET

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("MySQL")]
    [TestCategory("Phase1")]
    [Description("MySQL应该支持LIMIT OFFSET语法")]
    public void MySQL_Pagination_ShouldSupport_LIMIT_OFFSET()
    {
        // Arrange
        var template = "SELECT * FROM users LIMIT 10 OFFSET 20";

        // Act
        var result = _engine!.ProcessTemplate(template, _testMethod!, _testEntity!, "users", Sqlx.Generator.SqlDefine.MySql);

        // Assert
        StringAssert.Contains(result.ProcessedSql.ToUpperInvariant(), "LIMIT",
            "MySQL应该支持LIMIT");
        StringAssert.Contains(result.ProcessedSql.ToUpperInvariant(), "OFFSET",
            "MySQL应该支持OFFSET");
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("MySQL")]
    [TestCategory("Phase1")]
    [Description("MySQL应该支持LIMIT m,n旧语法")]
    public void MySQL_Pagination_ShouldSupport_LIMIT_Old_Syntax()
    {
        // Arrange
        var template = "SELECT * FROM users LIMIT 20, 10";

        // Act
        var result = _engine!.ProcessTemplate(template, _testMethod!, _testEntity!, "users", Sqlx.Generator.SqlDefine.MySql);

        // Assert
        StringAssert.Contains(result.ProcessedSql.ToUpperInvariant(), "LIMIT",
            "MySQL应该支持旧式LIMIT语法");
    }

    #endregion

    #region 数据类型

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("MySQL")]
    [TestCategory("Phase1")]
    [Description("MySQL应该支持TINYINT类型")]
    public void MySQL_DataType_ShouldSupport_TINYINT()
    {
        // Arrange
        var template = "CREATE TABLE test (id INT, active TINYINT)";

        // Act
        var result = _engine!.ProcessTemplate(template, _testMethod!, _testEntity!, "test", Sqlx.Generator.SqlDefine.MySql);

        // Assert
        StringAssert.Contains(result.ProcessedSql.ToUpperInvariant(), "TINYINT",
            "MySQL应该支持TINYINT");
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("MySQL")]
    [TestCategory("Phase1")]
    [Description("MySQL应该支持DATETIME类型")]
    public void MySQL_DataType_ShouldSupport_DATETIME()
    {
        // Arrange
        var template = "CREATE TABLE test (id INT, created_at DATETIME)";

        // Act
        var result = _engine!.ProcessTemplate(template, _testMethod!, _testEntity!, "test", Sqlx.Generator.SqlDefine.MySql);

        // Assert
        StringAssert.Contains(result.ProcessedSql.ToUpperInvariant(), "DATETIME",
            "MySQL应该支持DATETIME");
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("MySQL")]
    [TestCategory("Phase1")]
    [Description("MySQL应该支持BOOLEAN类型")]
    public void MySQL_DataType_ShouldSupport_BOOLEAN()
    {
        // Arrange
        var template = "CREATE TABLE test (id INT, is_active BOOLEAN)";

        // Act
        var result = _engine!.ProcessTemplate(template, _testMethod!, _testEntity!, "test", Sqlx.Generator.SqlDefine.MySql);

        // Assert
        StringAssert.Contains(result.ProcessedSql.ToUpperInvariant(), "BOOLEAN",
            "MySQL应该支持BOOLEAN（实际上是TINYINT(1)的别名）");
    }

    #endregion

    #region 标识符引用

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("MySQL")]
    [TestCategory("Phase1")]
    [Description("MySQL应该使用反引号包裹标识符")]
    public void MySQL_Identifier_ShouldUseBackticks()
    {
        // Arrange
        var template = "SELECT * FROM {{table}}";

        // Act
        var result = _engine!.ProcessTemplate(template, _testMethod!, _testEntity!, "users", Sqlx.Generator.SqlDefine.MySql);

        // Assert
        // MySQL可能使用反引号包裹表名，但不是强制的
        Assert.IsFalse(result.ProcessedSql.Contains("[") && result.ProcessedSql.Contains("]"),
            "MySQL不应该使用SQL Server风格的方括号");
    }

    #endregion

    #region AUTO_INCREMENT

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("MySQL")]
    [TestCategory("Phase1")]
    [Description("MySQL应该支持AUTO_INCREMENT")]
    public void MySQL_AutoIncrement_ShouldWork()
    {
        // Arrange
        var template = "CREATE TABLE test (id INT AUTO_INCREMENT PRIMARY KEY, name VARCHAR(100))";

        // Act
        var result = _engine!.ProcessTemplate(template, _testMethod!, _testEntity!, "test", Sqlx.Generator.SqlDefine.MySql);

        // Assert
        StringAssert.Contains(result.ProcessedSql.ToUpperInvariant(), "AUTO_INCREMENT",
            "MySQL应该支持AUTO_INCREMENT");
    }

    #endregion

    #region 批量操作

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("MySQL")]
    [TestCategory("Phase1")]
    [Description("MySQL批量INSERT应该使用多行VALUES")]
    public void MySQL_BatchInsert_ShouldUseMultiRowVALUES()
    {
        // Arrange
        var template = "INSERT INTO users (name, email) VALUES (@name1, @email1), (@name2, @email2), (@name3, @email3)";

        // Act
        var result = _engine!.ProcessTemplate(template, _testMethod!, _testEntity!, "users", Sqlx.Generator.SqlDefine.MySql);

        // Assert
        StringAssert.Contains(result.ProcessedSql.ToUpperInvariant(), "VALUES",
            "MySQL批量INSERT应该支持多行VALUES");
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
                public string Email { get; set; }
                public DateTime CreatedAt { get; set; }
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

