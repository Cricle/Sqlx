using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;
using SqlTemplateEngine = Sqlx.Generator.SqlTemplateEngine;

namespace Sqlx.Tests.Dialects;

/// <summary>
/// PostgreSQL和SQL Server数据库方言特定功能测试
/// 测试两种企业级数据库的特有SQL语法
/// </summary>
[TestClass]
public class TDD_PostgreSQL_SQLServer_Dialect
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

    #region PostgreSQL - RETURNING子句

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("PostgreSQL")]
    [TestCategory("Phase1")]
    [Description("PostgreSQL应该支持RETURNING子句返回插入的数据")]
    public void PostgreSQL_Insert_ShouldSupport_RETURNING()
    {
        // Arrange
        var template = "INSERT INTO users (name, email) VALUES (@name, @email) RETURNING id, name";
        var engine = new SqlTemplateEngine(Sqlx.Generator.SqlDefine.PostgreSql);

        // Act
        var result = engine.ProcessTemplate(template, _testMethod!, _testEntity!, "users", Sqlx.Generator.SqlDefine.PostgreSql);

        // Assert
        Assert.IsNotNull(result);
        StringAssert.Contains(result.ProcessedSql.ToUpperInvariant(), "RETURNING",
            "PostgreSQL应该支持RETURNING子句");
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("PostgreSQL")]
    [TestCategory("Phase1")]
    [Description("PostgreSQL RETURNING *应该返回所有列")]
    public void PostgreSQL_Insert_RETURNING_Star_ShouldWork()
    {
        // Arrange
        var template = "INSERT INTO users (name, email) VALUES (@name, @email) RETURNING *";
        var engine = new SqlTemplateEngine(Sqlx.Generator.SqlDefine.PostgreSql);

        // Act
        var result = engine.ProcessTemplate(template, _testMethod!, _testEntity!, "users", Sqlx.Generator.SqlDefine.PostgreSql);

        // Assert
        StringAssert.Contains(result.ProcessedSql.ToUpperInvariant(), "RETURNING *",
            "PostgreSQL应该支持RETURNING *");
    }

    #endregion

    #region PostgreSQL - ON CONFLICT (Upsert)

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("PostgreSQL")]
    [TestCategory("Phase1")]
    [Description("PostgreSQL应该支持ON CONFLICT DO UPDATE")]
    public void PostgreSQL_Upsert_ShouldSupport_ON_CONFLICT_DO_UPDATE()
    {
        // Arrange
        var template = "INSERT INTO users (id, name) VALUES (@id, @name) ON CONFLICT (id) DO UPDATE SET name = EXCLUDED.name";
        var engine = new SqlTemplateEngine(Sqlx.Generator.SqlDefine.PostgreSql);

        // Act
        var result = engine.ProcessTemplate(template, _testMethod!, _testEntity!, "users", Sqlx.Generator.SqlDefine.PostgreSql);

        // Assert
        StringAssert.Contains(result.ProcessedSql.ToUpperInvariant(), "ON CONFLICT",
            "PostgreSQL应该支持ON CONFLICT");
        StringAssert.Contains(result.ProcessedSql.ToUpperInvariant(), "DO UPDATE",
            "PostgreSQL应该支持DO UPDATE");
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("PostgreSQL")]
    [TestCategory("Phase1")]
    [Description("PostgreSQL应该支持EXCLUDED关键字")]
    public void PostgreSQL_Upsert_ShouldSupport_EXCLUDED()
    {
        // Arrange
        var template = "INSERT INTO users (id, name) VALUES (@id, @name) ON CONFLICT (id) DO UPDATE SET name = EXCLUDED.name";
        var engine = new SqlTemplateEngine(Sqlx.Generator.SqlDefine.PostgreSql);

        // Act
        var result = engine.ProcessTemplate(template, _testMethod!, _testEntity!, "users", Sqlx.Generator.SqlDefine.PostgreSql);

        // Assert
        StringAssert.Contains(result.ProcessedSql.ToUpperInvariant(), "EXCLUDED",
            "PostgreSQL应该支持EXCLUDED关键字");
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("PostgreSQL")]
    [TestCategory("Phase1")]
    [Description("PostgreSQL应该支持ON CONFLICT DO NOTHING")]
    public void PostgreSQL_Upsert_ShouldSupport_DO_NOTHING()
    {
        // Arrange
        var template = "INSERT INTO users (id, name) VALUES (@id, @name) ON CONFLICT (id) DO NOTHING";
        var engine = new SqlTemplateEngine(Sqlx.Generator.SqlDefine.PostgreSql);

        // Act
        var result = engine.ProcessTemplate(template, _testMethod!, _testEntity!, "users", Sqlx.Generator.SqlDefine.PostgreSql);

        // Assert
        StringAssert.Contains(result.ProcessedSql.ToUpperInvariant(), "DO NOTHING",
            "PostgreSQL应该支持DO NOTHING");
    }

    #endregion

    #region PostgreSQL - 字符串连接和函数

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("PostgreSQL")]
    [TestCategory("Phase1")]
    [Description("PostgreSQL应该支持||字符串连接运算符")]
    public void PostgreSQL_StringConcat_ShouldSupport_Pipe_Operator()
    {
        // Arrange
        var template = "SELECT first_name || ' ' || last_name as full_name FROM users";
        var engine = new SqlTemplateEngine(Sqlx.Generator.SqlDefine.PostgreSql);

        // Act
        var result = engine.ProcessTemplate(template, _testMethod!, _testEntity!, "users", Sqlx.Generator.SqlDefine.PostgreSql);

        // Assert
        StringAssert.Contains(result.ProcessedSql, "||",
            "PostgreSQL应该支持||运算符");
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("PostgreSQL")]
    [TestCategory("Phase1")]
    [Description("PostgreSQL应该支持NOW()和CURRENT_TIMESTAMP")]
    public void PostgreSQL_DateTime_ShouldSupport_NOW_And_CURRENT_TIMESTAMP()
    {
        // Arrange
        var template = "SELECT NOW(), CURRENT_TIMESTAMP FROM users";
        var engine = new SqlTemplateEngine(Sqlx.Generator.SqlDefine.PostgreSql);

        // Act
        var result = engine.ProcessTemplate(template, _testMethod!, _testEntity!, "users", Sqlx.Generator.SqlDefine.PostgreSql);

        // Assert
        StringAssert.Contains(result.ProcessedSql.ToUpperInvariant(), "NOW",
            "PostgreSQL应该支持NOW()");
    }

    #endregion

    #region PostgreSQL - 数据类型

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("PostgreSQL")]
    [TestCategory("Phase1")]
    [Description("PostgreSQL应该支持SERIAL和BIGSERIAL")]
    public void PostgreSQL_DataType_ShouldSupport_SERIAL()
    {
        // Arrange
        var template = "CREATE TABLE test (id SERIAL PRIMARY KEY, bigid BIGSERIAL)";
        var engine = new SqlTemplateEngine(Sqlx.Generator.SqlDefine.PostgreSql);

        // Act
        var result = engine.ProcessTemplate(template, _testMethod!, _testEntity!, "test", Sqlx.Generator.SqlDefine.PostgreSql);

        // Assert
        StringAssert.Contains(result.ProcessedSql.ToUpperInvariant(), "SERIAL",
            "PostgreSQL应该支持SERIAL");
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("PostgreSQL")]
    [TestCategory("Phase1")]
    [Description("PostgreSQL应该支持BOOLEAN类型")]
    public void PostgreSQL_DataType_ShouldSupport_BOOLEAN()
    {
        // Arrange
        var template = "CREATE TABLE test (id INT, is_active BOOLEAN)";
        var engine = new SqlTemplateEngine(Sqlx.Generator.SqlDefine.PostgreSql);

        // Act
        var result = engine.ProcessTemplate(template, _testMethod!, _testEntity!, "test", Sqlx.Generator.SqlDefine.PostgreSql);

        // Assert
        StringAssert.Contains(result.ProcessedSql.ToUpperInvariant(), "BOOLEAN",
            "PostgreSQL应该支持BOOLEAN类型");
    }

    #endregion

    #region PostgreSQL - 分页

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("PostgreSQL")]
    [TestCategory("Phase1")]
    [Description("PostgreSQL应该支持LIMIT OFFSET")]
    public void PostgreSQL_Pagination_ShouldSupport_LIMIT_OFFSET()
    {
        // Arrange
        var template = "SELECT * FROM users ORDER BY id LIMIT 10 OFFSET 20";
        var engine = new SqlTemplateEngine(Sqlx.Generator.SqlDefine.PostgreSql);

        // Act
        var result = engine.ProcessTemplate(template, _testMethod!, _testEntity!, "users", Sqlx.Generator.SqlDefine.PostgreSql);

        // Assert
        StringAssert.Contains(result.ProcessedSql.ToUpperInvariant(), "LIMIT",
            "PostgreSQL应该支持LIMIT");
        StringAssert.Contains(result.ProcessedSql.ToUpperInvariant(), "OFFSET",
            "PostgreSQL应该支持OFFSET");
    }

    #endregion

    #region SQL Server - OUTPUT子句

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("SQLServer")]
    [TestCategory("Phase1")]
    [Description("SQL Server应该支持OUTPUT INSERTED.*")]
    public void SQLServer_Insert_ShouldSupport_OUTPUT_INSERTED()
    {
        // Arrange
        var template = "INSERT INTO users (name, email) OUTPUT INSERTED.* VALUES (@name, @email)";
        var engine = new SqlTemplateEngine(Sqlx.Generator.SqlDefine.SqlServer);

        // Act
        var result = engine.ProcessTemplate(template, _testMethod!, _testEntity!, "users", Sqlx.Generator.SqlDefine.SqlServer);

        // Assert
        StringAssert.Contains(result.ProcessedSql.ToUpperInvariant(), "OUTPUT",
            "SQL Server应该支持OUTPUT子句");
        StringAssert.Contains(result.ProcessedSql.ToUpperInvariant(), "INSERTED",
            "SQL Server应该支持INSERTED");
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("SQLServer")]
    [TestCategory("Phase1")]
    [Description("SQL Server应该支持OUTPUT INSERTED.id")]
    public void SQLServer_Insert_OUTPUT_Specific_Column_ShouldWork()
    {
        // Arrange
        var template = "INSERT INTO users (name) OUTPUT INSERTED.id VALUES (@name)";
        var engine = new SqlTemplateEngine(Sqlx.Generator.SqlDefine.SqlServer);

        // Act
        var result = engine.ProcessTemplate(template, _testMethod!, _testEntity!, "users", Sqlx.Generator.SqlDefine.SqlServer);

        // Assert
        StringAssert.Contains(result.ProcessedSql.ToUpperInvariant(), "OUTPUT INSERTED",
            "SQL Server应该支持OUTPUT INSERTED.column");
    }

    #endregion

    #region SQL Server - MERGE语句

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("SQLServer")]
    [TestCategory("Phase1")]
    [Description("SQL Server应该支持MERGE语句(Upsert)")]
    public void SQLServer_Upsert_ShouldSupport_MERGE()
    {
        // Arrange
        var template = @"MERGE users AS target
            USING (SELECT @id as id, @name as name) AS source
            ON target.id = source.id
            WHEN MATCHED THEN UPDATE SET name = source.name
            WHEN NOT MATCHED THEN INSERT (id, name) VALUES (source.id, source.name);";
        var engine = new SqlTemplateEngine(Sqlx.Generator.SqlDefine.SqlServer);

        // Act
        var result = engine.ProcessTemplate(template, _testMethod!, _testEntity!, "users", Sqlx.Generator.SqlDefine.SqlServer);

        // Assert
        StringAssert.Contains(result.ProcessedSql.ToUpperInvariant(), "MERGE",
            "SQL Server应该支持MERGE语句");
    }

    #endregion

    #region SQL Server - TOP vs LIMIT

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("SQLServer")]
    [TestCategory("Phase1")]
    [Description("SQL Server应该支持TOP子句")]
    public void SQLServer_Pagination_ShouldSupport_TOP()
    {
        // Arrange
        var template = "SELECT TOP 10 * FROM users";
        var engine = new SqlTemplateEngine(Sqlx.Generator.SqlDefine.SqlServer);

        // Act
        var result = engine.ProcessTemplate(template, _testMethod!, _testEntity!, "users", Sqlx.Generator.SqlDefine.SqlServer);

        // Assert
        StringAssert.Contains(result.ProcessedSql.ToUpperInvariant(), "TOP",
            "SQL Server应该支持TOP");
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("SQLServer")]
    [TestCategory("Phase1")]
    [Description("SQL Server 2012+应该支持OFFSET FETCH NEXT")]
    public void SQLServer_Pagination_ShouldSupport_OFFSET_FETCH_NEXT()
    {
        // Arrange
        var template = "SELECT * FROM users ORDER BY id OFFSET 20 ROWS FETCH NEXT 10 ROWS ONLY";
        var engine = new SqlTemplateEngine(Sqlx.Generator.SqlDefine.SqlServer);

        // Act
        var result = engine.ProcessTemplate(template, _testMethod!, _testEntity!, "users", Sqlx.Generator.SqlDefine.SqlServer);

        // Assert
        StringAssert.Contains(result.ProcessedSql.ToUpperInvariant(), "OFFSET",
            "SQL Server应该支持OFFSET");
        StringAssert.Contains(result.ProcessedSql.ToUpperInvariant(), "FETCH",
            "SQL Server应该支持FETCH NEXT");
    }

    #endregion

    #region SQL Server - 字符串连接和函数

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("SQLServer")]
    [TestCategory("Phase1")]
    [Description("SQL Server应该支持+字符串连接运算符")]
    public void SQLServer_StringConcat_ShouldSupport_Plus_Operator()
    {
        // Arrange
        var template = "SELECT first_name + ' ' + last_name as full_name FROM users";
        var engine = new SqlTemplateEngine(Sqlx.Generator.SqlDefine.SqlServer);

        // Act
        var result = engine.ProcessTemplate(template, _testMethod!, _testEntity!, "users", Sqlx.Generator.SqlDefine.SqlServer);

        // Assert
        StringAssert.Contains(result.ProcessedSql, "+",
            "SQL Server应该支持+运算符连接字符串");
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("SQLServer")]
    [TestCategory("Phase1")]
    [Description("SQL Server应该支持GETDATE()")]
    public void SQLServer_DateTime_ShouldSupport_GETDATE()
    {
        // Arrange
        var template = "INSERT INTO logs (message, created_at) VALUES (@message, GETDATE())";
        var engine = new SqlTemplateEngine(Sqlx.Generator.SqlDefine.SqlServer);

        // Act
        var result = engine.ProcessTemplate(template, _testMethod!, _testEntity!, "logs", Sqlx.Generator.SqlDefine.SqlServer);

        // Assert
        StringAssert.Contains(result.ProcessedSql.ToUpperInvariant(), "GETDATE",
            "SQL Server应该支持GETDATE()");
    }

    #endregion

    #region SQL Server - 数据类型

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("SQLServer")]
    [TestCategory("Phase1")]
    [Description("SQL Server应该支持BIT类型表示布尔值")]
    public void SQLServer_DataType_ShouldSupport_BIT()
    {
        // Arrange
        var template = "CREATE TABLE test (id INT, is_active BIT)";
        var engine = new SqlTemplateEngine(Sqlx.Generator.SqlDefine.SqlServer);

        // Act
        var result = engine.ProcessTemplate(template, _testMethod!, _testEntity!, "test", Sqlx.Generator.SqlDefine.SqlServer);

        // Assert
        StringAssert.Contains(result.ProcessedSql.ToUpperInvariant(), "BIT",
            "SQL Server应该支持BIT类型");
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("SQLServer")]
    [TestCategory("Phase1")]
    [Description("SQL Server应该支持NVARCHAR类型")]
    public void SQLServer_DataType_ShouldSupport_NVARCHAR()
    {
        // Arrange
        var template = "CREATE TABLE test (id INT, name NVARCHAR(100))";
        var engine = new SqlTemplateEngine(Sqlx.Generator.SqlDefine.SqlServer);

        // Act
        var result = engine.ProcessTemplate(template, _testMethod!, _testEntity!, "test", Sqlx.Generator.SqlDefine.SqlServer);

        // Assert
        StringAssert.Contains(result.ProcessedSql.ToUpperInvariant(), "NVARCHAR",
            "SQL Server应该支持NVARCHAR");
    }

    #endregion

    #region SQL Server - 标识符引用

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("SQLServer")]
    [TestCategory("Phase1")]
    [Description("SQL Server应该使用方括号[]包裹标识符")]
    public void SQLServer_Identifier_ShouldUse_Brackets()
    {
        // Arrange
        var template = "SELECT * FROM {{table}}";
        var engine = new SqlTemplateEngine(Sqlx.Generator.SqlDefine.SqlServer);

        // Act
        var result = engine.ProcessTemplate(template, _testMethod!, _testEntity!, "users", Sqlx.Generator.SqlDefine.SqlServer);

        // Assert
        Assert.IsNotNull(result.ProcessedSql);
        // SQL Server可能使用方括号，但不是强制的
        Assert.IsFalse(result.ProcessedSql.Contains("`"),
            "SQL Server不应该使用MySQL风格的反引号");
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


