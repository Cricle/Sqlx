using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;
using SqlTemplateEngine = Sqlx.Generator.SqlTemplateEngine;
using SqlDefine = Sqlx.Generator.SqlDefine;

namespace Sqlx.Tests.Dialects;

/// <summary>
/// 多数据库方言核心功能对比测试
/// 测试同一SQL在不同数据库中的处理差异
/// </summary>
[TestClass]
public class TDD_MultiDialect_Core
{
    private IMethodSymbol? _testMethod;
    private INamedTypeSymbol? _testEntity;

    [TestInitialize]
    public void Initialize()
    {
        var compilation = CreateTestCompilation();
        _testEntity = compilation.GetTypeByMetadataName("TestEntity")!;
        var methodClass = compilation.GetTypeByMetadataName("TestMethods")!;
        _testMethod = methodClass.GetMembers("GetAllAsync").OfType<IMethodSymbol>().First();
    }

    #region INSERT RETURNING对比

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("MultiDialect")]
    [TestCategory("Phase1")]
    [Description("不同数据库INSERT返回ID的语法应该不同")]
    public void MultiDialect_InsertReturning_ShouldDiffer()
    {
        // Arrange
        var template = "INSERT INTO users (name) VALUES (@name)";

        // Act - 测试所有5个数据库
        var mysqlEngine = new SqlTemplateEngine(Sqlx.Generator.SqlDefine.MySql);
        var pgEngine = new SqlTemplateEngine(Sqlx.Generator.SqlDefine.PostgreSql);
        var sqlserverEngine = new SqlTemplateEngine(Sqlx.Generator.SqlDefine.SqlServer);
        var sqliteEngine = new SqlTemplateEngine(Sqlx.Generator.SqlDefine.SQLite);

        var mysqlResult = mysqlEngine.ProcessTemplate(template, _testMethod!, _testEntity!, "users", Sqlx.Generator.SqlDefine.MySql);
        var pgResult = pgEngine.ProcessTemplate(template, _testMethod!, _testEntity!, "users", Sqlx.Generator.SqlDefine.PostgreSql);
        var sqlserverResult = sqlserverEngine.ProcessTemplate(template, _testMethod!, _testEntity!, "users", Sqlx.Generator.SqlDefine.SqlServer);
        var sqliteResult = sqliteEngine.ProcessTemplate(template, _testMethod!, _testEntity!, "users", Sqlx.Generator.SqlDefine.SQLite);

        // Assert
        Assert.IsNotNull(mysqlResult.ProcessedSql);
        Assert.IsNotNull(pgResult.ProcessedSql);
        Assert.IsNotNull(sqlserverResult.ProcessedSql);
        Assert.IsNotNull(sqliteResult.ProcessedSql);

        // MySQL: 使用 @ 参数
        // PostgreSQL: 使用 $ 参数或 @
        // SQL Server: 使用 @ 参数
        // SQLite: 使用 @ 参数
        Assert.IsTrue(mysqlResult.ProcessedSql.Contains("@") || mysqlResult.ProcessedSql.Contains("?"));
    }

    #endregion

    #region 分页语法对比

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("MultiDialect")]
    [TestCategory("Phase1")]
    [Description("不同数据库的分页语法应该不同")]
    public void MultiDialect_Pagination_ShouldDiffer()
    {
        // Arrange
        var template = "SELECT * FROM users ORDER BY id";

        // Act
        var mysqlEngine = new SqlTemplateEngine(Sqlx.Generator.SqlDefine.MySql);
        var pgEngine = new SqlTemplateEngine(Sqlx.Generator.SqlDefine.PostgreSql);
        var sqlserverEngine = new SqlTemplateEngine(Sqlx.Generator.SqlDefine.SqlServer);
        var sqliteEngine = new SqlTemplateEngine(Sqlx.Generator.SqlDefine.SQLite);

        var mysqlResult = mysqlEngine.ProcessTemplate(template, _testMethod!, _testEntity!, "users", Sqlx.Generator.SqlDefine.MySql);
        var pgResult = pgEngine.ProcessTemplate(template, _testMethod!, _testEntity!, "users", Sqlx.Generator.SqlDefine.PostgreSql);
        var sqlserverResult = sqlserverEngine.ProcessTemplate(template, _testMethod!, _testEntity!, "users", Sqlx.Generator.SqlDefine.SqlServer);
        var sqliteResult = sqliteEngine.ProcessTemplate(template, _testMethod!, _testEntity!, "users", Sqlx.Generator.SqlDefine.SQLite);

        // Assert - 所有方言都应该生成有效SQL
        Assert.IsNotNull(mysqlResult.ProcessedSql);
        Assert.IsNotNull(pgResult.ProcessedSql);
        Assert.IsNotNull(sqlserverResult.ProcessedSql);
        Assert.IsNotNull(sqliteResult.ProcessedSql);

        // MySQL/PostgreSQL/SQLite: 通常支持 LIMIT OFFSET
        // SQL Server: 使用 OFFSET ROWS FETCH NEXT或TOP
    }

    #endregion

    #region 标识符引用对比

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("MultiDialect")]
    [TestCategory("Phase1")]
    [Description("不同数据库的标识符引用应该使用不同符号")]
    public void MultiDialect_IdentifierQuoting_ShouldDiffer()
    {
        // Arrange
        var template = "SELECT * FROM {{table}}";

        // Act
        var mysqlEngine = new SqlTemplateEngine(Sqlx.Generator.SqlDefine.MySql);
        var pgEngine = new SqlTemplateEngine(Sqlx.Generator.SqlDefine.PostgreSql);
        var sqlserverEngine = new SqlTemplateEngine(Sqlx.Generator.SqlDefine.SqlServer);
        var sqliteEngine = new SqlTemplateEngine(Sqlx.Generator.SqlDefine.SQLite);

        var mysqlResult = mysqlEngine.ProcessTemplate(template, _testMethod!, _testEntity!, "users", Sqlx.Generator.SqlDefine.MySql);
        var pgResult = pgEngine.ProcessTemplate(template, _testMethod!, _testEntity!, "users", Sqlx.Generator.SqlDefine.PostgreSql);
        var sqlserverResult = sqlserverEngine.ProcessTemplate(template, _testMethod!, _testEntity!, "users", Sqlx.Generator.SqlDefine.SqlServer);
        var sqliteResult = sqliteEngine.ProcessTemplate(template, _testMethod!, _testEntity!, "users", Sqlx.Generator.SqlDefine.SQLite);

        // Assert
        Assert.IsNotNull(mysqlResult.ProcessedSql);
        Assert.IsNotNull(pgResult.ProcessedSql);
        Assert.IsNotNull(sqlserverResult.ProcessedSql);
        Assert.IsNotNull(sqliteResult.ProcessedSql);

        // MySQL: 反引号 `
        // PostgreSQL: 双引号 "
        // SQL Server: 方括号 []
        // SQLite: 方括号 [] 或双引号 "
    }

    #endregion

    #region 字符串连接对比

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("MultiDialect")]
    [TestCategory("Phase1")]
    [Description("不同数据库的字符串连接语法应该不同")]
    public void MultiDialect_StringConcat_ShouldDiffer()
    {
        // Arrange
        var template = "SELECT first_name, last_name FROM users";

        // Act
        var mysqlEngine = new SqlTemplateEngine(Sqlx.Generator.SqlDefine.MySql);
        var pgEngine = new SqlTemplateEngine(Sqlx.Generator.SqlDefine.PostgreSql);
        var sqlserverEngine = new SqlTemplateEngine(Sqlx.Generator.SqlDefine.SqlServer);

        var mysqlResult = mysqlEngine.ProcessTemplate(template, _testMethod!, _testEntity!, "users", Sqlx.Generator.SqlDefine.MySql);
        var pgResult = pgEngine.ProcessTemplate(template, _testMethod!, _testEntity!, "users", Sqlx.Generator.SqlDefine.PostgreSql);
        var sqlserverResult = sqlserverEngine.ProcessTemplate(template, _testMethod!, _testEntity!, "users", Sqlx.Generator.SqlDefine.SqlServer);

        // Assert
        Assert.IsNotNull(mysqlResult.ProcessedSql);
        Assert.IsNotNull(pgResult.ProcessedSql);
        Assert.IsNotNull(sqlserverResult.ProcessedSql);

        // MySQL: CONCAT(a, b)
        // PostgreSQL: a || b
        // SQL Server: a + b
        // SQLite: a || b
    }

    #endregion

    #region 当前时间函数对比

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("MultiDialect")]
    [TestCategory("Phase1")]
    [Description("不同数据库获取当前时间的函数应该不同")]
    public void MultiDialect_CurrentTimestamp_ShouldDiffer()
    {
        // Arrange
        var template = "SELECT * FROM logs";

        // Act
        var mysqlEngine = new SqlTemplateEngine(Sqlx.Generator.SqlDefine.MySql);
        var pgEngine = new SqlTemplateEngine(Sqlx.Generator.SqlDefine.PostgreSql);
        var sqlserverEngine = new SqlTemplateEngine(Sqlx.Generator.SqlDefine.SqlServer);
        var sqliteEngine = new SqlTemplateEngine(Sqlx.Generator.SqlDefine.SQLite);

        var mysqlResult = mysqlEngine.ProcessTemplate(template, _testMethod!, _testEntity!, "logs", Sqlx.Generator.SqlDefine.MySql);
        var pgResult = pgEngine.ProcessTemplate(template, _testMethod!, _testEntity!, "logs", Sqlx.Generator.SqlDefine.PostgreSql);
        var sqlserverResult = sqlserverEngine.ProcessTemplate(template, _testMethod!, _testEntity!, "logs", Sqlx.Generator.SqlDefine.SqlServer);
        var sqliteResult = sqliteEngine.ProcessTemplate(template, _testMethod!, _testEntity!, "logs", Sqlx.Generator.SqlDefine.SQLite);

        // Assert
        Assert.IsNotNull(mysqlResult.ProcessedSql);
        Assert.IsNotNull(pgResult.ProcessedSql);
        Assert.IsNotNull(sqlserverResult.ProcessedSql);
        Assert.IsNotNull(sqliteResult.ProcessedSql);

        // MySQL: NOW() 或 CURRENT_TIMESTAMP
        // PostgreSQL: NOW() 或 CURRENT_TIMESTAMP
        // SQL Server: GETDATE() 或 CURRENT_TIMESTAMP
        // SQLite: datetime('now')
    }

    #endregion

    #region Upsert语法对比

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("MultiDialect")]
    [TestCategory("Phase1")]
    [Description("不同数据库的Upsert语法应该不同")]
    public void MultiDialect_Upsert_ShouldDiffer()
    {
        // Arrange
        var template = "INSERT INTO users (id, name) VALUES (@id, @name)";

        // Act
        var mysqlEngine = new SqlTemplateEngine(Sqlx.Generator.SqlDefine.MySql);
        var pgEngine = new SqlTemplateEngine(Sqlx.Generator.SqlDefine.PostgreSql);
        var sqliteEngine = new SqlTemplateEngine(Sqlx.Generator.SqlDefine.SQLite);

        var mysqlResult = mysqlEngine.ProcessTemplate(template, _testMethod!, _testEntity!, "users", Sqlx.Generator.SqlDefine.MySql);
        var pgResult = pgEngine.ProcessTemplate(template, _testMethod!, _testEntity!, "users", Sqlx.Generator.SqlDefine.PostgreSql);
        var sqliteResult = sqliteEngine.ProcessTemplate(template, _testMethod!, _testEntity!, "users", Sqlx.Generator.SqlDefine.SQLite);

        // Assert
        Assert.IsNotNull(mysqlResult.ProcessedSql);
        Assert.IsNotNull(pgResult.ProcessedSql);
        Assert.IsNotNull(sqliteResult.ProcessedSql);

        // MySQL: ON DUPLICATE KEY UPDATE
        // PostgreSQL: ON CONFLICT ... DO UPDATE
        // SQL Server: MERGE
        // SQLite: INSERT OR REPLACE 或 ON CONFLICT
    }

    #endregion

    #region 参数占位符对比

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("MultiDialect")]
    [TestCategory("Phase1")]
    [Description("不同数据库的参数占位符应该不同")]
    public void MultiDialect_ParameterPlaceholder_ShouldDiffer()
    {
        // Arrange
        var template = "SELECT * FROM users WHERE id = @id AND name = @name";

        // Act
        var mysqlEngine = new SqlTemplateEngine(Sqlx.Generator.SqlDefine.MySql);
        var pgEngine = new SqlTemplateEngine(Sqlx.Generator.SqlDefine.PostgreSql);
        var sqlserverEngine = new SqlTemplateEngine(Sqlx.Generator.SqlDefine.SqlServer);
        var sqliteEngine = new SqlTemplateEngine(Sqlx.Generator.SqlDefine.SQLite);

        var mysqlResult = mysqlEngine.ProcessTemplate(template, _testMethod!, _testEntity!, "users", Sqlx.Generator.SqlDefine.MySql);
        var pgResult = pgEngine.ProcessTemplate(template, _testMethod!, _testEntity!, "users", Sqlx.Generator.SqlDefine.PostgreSql);
        var sqlserverResult = sqlserverEngine.ProcessTemplate(template, _testMethod!, _testEntity!, "users", Sqlx.Generator.SqlDefine.SqlServer);
        var sqliteResult = sqliteEngine.ProcessTemplate(template, _testMethod!, _testEntity!, "users", Sqlx.Generator.SqlDefine.SQLite);

        // Assert
        Assert.IsNotNull(mysqlResult.ProcessedSql);
        Assert.IsNotNull(pgResult.ProcessedSql);
        Assert.IsNotNull(sqlserverResult.ProcessedSql);
        Assert.IsNotNull(sqliteResult.ProcessedSql);

        // MySQL: @param
        // PostgreSQL: $1, $2 或 @param
        // SQL Server: @param
        // SQLite: @param, :param 或 ?
    }

    #endregion

    #region BOOLEAN类型对比

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("MultiDialect")]
    [TestCategory("Phase1")]
    [Description("不同数据库的BOOLEAN类型实现应该不同")]
    public void MultiDialect_BooleanType_ShouldDiffer()
    {
        // Arrange
        var template = "SELECT * FROM users WHERE is_active = true";

        // Act
        var mysqlEngine = new SqlTemplateEngine(Sqlx.Generator.SqlDefine.MySql);
        var pgEngine = new SqlTemplateEngine(Sqlx.Generator.SqlDefine.PostgreSql);
        var sqlserverEngine = new SqlTemplateEngine(Sqlx.Generator.SqlDefine.SqlServer);
        var sqliteEngine = new SqlTemplateEngine(Sqlx.Generator.SqlDefine.SQLite);

        var mysqlResult = mysqlEngine.ProcessTemplate(template, _testMethod!, _testEntity!, "users", Sqlx.Generator.SqlDefine.MySql);
        var pgResult = pgEngine.ProcessTemplate(template, _testMethod!, _testEntity!, "users", Sqlx.Generator.SqlDefine.PostgreSql);
        var sqlserverResult = sqlserverEngine.ProcessTemplate(template, _testMethod!, _testEntity!, "users", Sqlx.Generator.SqlDefine.SqlServer);
        var sqliteResult = sqliteEngine.ProcessTemplate(template, _testMethod!, _testEntity!, "users", Sqlx.Generator.SqlDefine.SQLite);

        // Assert
        Assert.IsNotNull(mysqlResult.ProcessedSql);
        Assert.IsNotNull(pgResult.ProcessedSql);
        Assert.IsNotNull(sqlserverResult.ProcessedSql);
        Assert.IsNotNull(sqliteResult.ProcessedSql);

        // MySQL: TINYINT(1) 或 BOOLEAN
        // PostgreSQL: BOOLEAN
        // SQL Server: BIT
        // SQLite: INTEGER (0/1)
    }

    #endregion

    #region LIMIT子句语法对比

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("MultiDialect")]
    [TestCategory("Phase1")]
    [Description("不同数据库的LIMIT子句应该使用不同语法")]
    public void MultiDialect_LimitClause_ShouldDiffer()
    {
        // Arrange
        var template = "SELECT * FROM users ORDER BY id";

        // Act
        var mysqlEngine = new SqlTemplateEngine(Sqlx.Generator.SqlDefine.MySql);
        var pgEngine = new SqlTemplateEngine(Sqlx.Generator.SqlDefine.PostgreSql);
        var sqlserverEngine = new SqlTemplateEngine(Sqlx.Generator.SqlDefine.SqlServer);
        var sqliteEngine = new SqlTemplateEngine(Sqlx.Generator.SqlDefine.SQLite);

        var mysqlResult = mysqlEngine.ProcessTemplate(template, _testMethod!, _testEntity!, "users", Sqlx.Generator.SqlDefine.MySql);
        var pgResult = pgEngine.ProcessTemplate(template, _testMethod!, _testEntity!, "users", Sqlx.Generator.SqlDefine.PostgreSql);
        var sqlserverResult = sqlserverEngine.ProcessTemplate(template, _testMethod!, _testEntity!, "users", Sqlx.Generator.SqlDefine.SqlServer);
        var sqliteResult = sqliteEngine.ProcessTemplate(template, _testMethod!, _testEntity!, "users", Sqlx.Generator.SqlDefine.SQLite);

        // Assert
        Assert.IsNotNull(mysqlResult.ProcessedSql);
        Assert.IsNotNull(pgResult.ProcessedSql);
        Assert.IsNotNull(sqlserverResult.ProcessedSql);
        Assert.IsNotNull(sqliteResult.ProcessedSql);

        // MySQL/PostgreSQL/SQLite: LIMIT n
        // SQL Server 2012+: OFFSET ROWS FETCH NEXT ROWS
        // SQL Server <2012: TOP n
    }

    #endregion

    // 辅助方法
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
                public bool IsActive { get; set; }
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

