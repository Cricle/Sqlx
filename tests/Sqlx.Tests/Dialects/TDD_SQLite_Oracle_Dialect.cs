using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;
using SqlTemplateEngine = Sqlx.Generator.SqlTemplateEngine;

namespace Sqlx.Tests.Dialects;

/// <summary>
/// SQLite和Oracle数据库方言特定功能测试
/// 测试SQLite轻量级和Oracle企业级数据库的特有SQL语法
/// </summary>
[TestClass]
public class TDD_SQLite_Oracle_Dialect
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

    #region SQLite - last_insert_rowid()

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("SQLite")]
    [TestCategory("Phase1")]
    [Description("SQLite应该使用last_insert_rowid()返回自增ID")]
    public void SQLite_Insert_ShouldUse_last_insert_rowid()
    {
        // Arrange
        var template = "INSERT INTO users (name) VALUES (@name); SELECT last_insert_rowid()";
        var engine = new SqlTemplateEngine(Sqlx.Generator.SqlDefine.SQLite);

        // Act
        var result = engine.ProcessTemplate(template, _testMethod!, _testEntity!, "users", Sqlx.Generator.SqlDefine.SQLite);

        // Assert
        Assert.IsNotNull(result);
        StringAssert.Contains(result.ProcessedSql.ToLowerInvariant(), "last_insert_rowid",
            "SQLite应该支持last_insert_rowid()");
    }

    #endregion

    #region SQLite - INSERT OR REPLACE

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("SQLite")]
    [TestCategory("Phase1")]
    [Description("SQLite应该支持INSERT OR REPLACE (Upsert)")]
    public void SQLite_Upsert_ShouldSupport_INSERT_OR_REPLACE()
    {
        // Arrange
        var template = "INSERT OR REPLACE INTO users (id, name) VALUES (@id, @name)";
        var engine = new SqlTemplateEngine(Sqlx.Generator.SqlDefine.SQLite);

        // Act
        var result = engine.ProcessTemplate(template, _testMethod!, _testEntity!, "users", Sqlx.Generator.SqlDefine.SQLite);

        // Assert
        StringAssert.Contains(result.ProcessedSql.ToUpperInvariant(), "INSERT OR REPLACE",
            "SQLite应该支持INSERT OR REPLACE");
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("SQLite")]
    [TestCategory("Phase1")]
    [Description("SQLite应该支持INSERT OR IGNORE")]
    public void SQLite_Upsert_ShouldSupport_INSERT_OR_IGNORE()
    {
        // Arrange
        var template = "INSERT OR IGNORE INTO users (id, name) VALUES (@id, @name)";
        var engine = new SqlTemplateEngine(Sqlx.Generator.SqlDefine.SQLite);

        // Act
        var result = engine.ProcessTemplate(template, _testMethod!, _testEntity!, "users", Sqlx.Generator.SqlDefine.SQLite);

        // Assert
        StringAssert.Contains(result.ProcessedSql.ToUpperInvariant(), "INSERT OR IGNORE",
            "SQLite应该支持INSERT OR IGNORE");
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("SQLite")]
    [TestCategory("Phase1")]
    [Description("SQLite应该支持ON CONFLICT子句")]
    public void SQLite_Upsert_ShouldSupport_ON_CONFLICT()
    {
        // Arrange
        var template = "INSERT INTO users (id, name) VALUES (@id, @name) ON CONFLICT(id) DO UPDATE SET name=excluded.name";
        var engine = new SqlTemplateEngine(Sqlx.Generator.SqlDefine.SQLite);

        // Act
        var result = engine.ProcessTemplate(template, _testMethod!, _testEntity!, "users", Sqlx.Generator.SqlDefine.SQLite);

        // Assert
        StringAssert.Contains(result.ProcessedSql.ToUpperInvariant(), "ON CONFLICT",
            "SQLite应该支持ON CONFLICT");
    }

    #endregion

    #region SQLite - 字符串连接

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("SQLite")]
    [TestCategory("Phase1")]
    [Description("SQLite应该支持||字符串连接运算符")]
    public void SQLite_StringConcat_ShouldSupport_Pipe_Operator()
    {
        // Arrange
        var template = "SELECT first_name || ' ' || last_name as full_name FROM users";
        var engine = new SqlTemplateEngine(Sqlx.Generator.SqlDefine.SQLite);

        // Act
        var result = engine.ProcessTemplate(template, _testMethod!, _testEntity!, "users", Sqlx.Generator.SqlDefine.SQLite);

        // Assert
        StringAssert.Contains(result.ProcessedSql, "||",
            "SQLite应该支持||运算符");
    }

    #endregion

    #region SQLite - 日期时间函数

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("SQLite")]
    [TestCategory("Phase1")]
    [Description("SQLite应该支持datetime('now')")]
    public void SQLite_DateTime_ShouldSupport_datetime_now()
    {
        // Arrange
        var template = "INSERT INTO logs (message, created_at) VALUES (@message, datetime('now'))";
        var engine = new SqlTemplateEngine(Sqlx.Generator.SqlDefine.SQLite);

        // Act
        var result = engine.ProcessTemplate(template, _testMethod!, _testEntity!, "logs", Sqlx.Generator.SqlDefine.SQLite);

        // Assert
        StringAssert.Contains(result.ProcessedSql.ToLowerInvariant(), "datetime",
            "SQLite应该支持datetime函数");
    }

    #endregion

    #region SQLite - 分页

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("SQLite")]
    [TestCategory("Phase1")]
    [Description("SQLite应该支持LIMIT OFFSET")]
    public void SQLite_Pagination_ShouldSupport_LIMIT_OFFSET()
    {
        // Arrange
        var template = "SELECT * FROM users ORDER BY id LIMIT 10 OFFSET 20";
        var engine = new SqlTemplateEngine(Sqlx.Generator.SqlDefine.SQLite);

        // Act
        var result = engine.ProcessTemplate(template, _testMethod!, _testEntity!, "users", Sqlx.Generator.SqlDefine.SQLite);

        // Assert
        StringAssert.Contains(result.ProcessedSql.ToUpperInvariant(), "LIMIT",
            "SQLite应该支持LIMIT");
        StringAssert.Contains(result.ProcessedSql.ToUpperInvariant(), "OFFSET",
            "SQLite应该支持OFFSET");
    }

    #endregion

    #region SQLite - 数据类型

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("SQLite")]
    [TestCategory("Phase1")]
    [Description("SQLite应该支持INTEGER PRIMARY KEY AUTOINCREMENT")]
    public void SQLite_DataType_ShouldSupport_AUTOINCREMENT()
    {
        // Arrange
        var template = "CREATE TABLE test (id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT)";
        var engine = new SqlTemplateEngine(Sqlx.Generator.SqlDefine.SQLite);

        // Act
        var result = engine.ProcessTemplate(template, _testMethod!, _testEntity!, "test", Sqlx.Generator.SqlDefine.SQLite);

        // Assert
        StringAssert.Contains(result.ProcessedSql.ToUpperInvariant(), "AUTOINCREMENT",
            "SQLite应该支持AUTOINCREMENT");
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("SQLite")]
    [TestCategory("Phase1")]
    [Description("SQLite应该支持TEXT类型")]
    public void SQLite_DataType_ShouldSupport_TEXT()
    {
        // Arrange
        var template = "CREATE TABLE test (id INTEGER, name TEXT)";
        var engine = new SqlTemplateEngine(Sqlx.Generator.SqlDefine.SQLite);

        // Act
        var result = engine.ProcessTemplate(template, _testMethod!, _testEntity!, "test", Sqlx.Generator.SqlDefine.SQLite);

        // Assert
        StringAssert.Contains(result.ProcessedSql.ToUpperInvariant(), "TEXT",
            "SQLite应该支持TEXT类型");
    }

    #endregion

    #region SQLite - 限制和特性

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("SQLite")]
    [TestCategory("Phase1")]
    [Description("SQLite不支持RIGHT JOIN")]
    public void SQLite_Limitation_NoSupport_RIGHT_JOIN()
    {
        // Arrange
        var template = "SELECT * FROM users u LEFT JOIN orders o ON u.id = o.user_id";
        var engine = new SqlTemplateEngine(Sqlx.Generator.SqlDefine.SQLite);

        // Act
        var result = engine.ProcessTemplate(template, _testMethod!, _testEntity!, "users", Sqlx.Generator.SqlDefine.SQLite);

        // Assert
        Assert.IsNotNull(result);
        // SQLite不支持RIGHT JOIN，但支持LEFT JOIN
        StringAssert.Contains(result.ProcessedSql.ToUpperInvariant(), "LEFT JOIN",
            "SQLite应该支持LEFT JOIN");
    }

    #endregion

    #region Oracle - RETURNING INTO

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("Oracle")]
    [TestCategory("Phase1")]
    [Description("Oracle应该支持RETURNING INTO子句")]
    public void Oracle_Insert_ShouldSupport_RETURNING_INTO()
    {
        // Arrange
        var template = "INSERT INTO users (name) VALUES (:name) RETURNING id INTO :newId";
        var engine = new SqlTemplateEngine(Sqlx.Generator.SqlDefine.Oracle);

        // Act
        var result = engine.ProcessTemplate(template, _testMethod!, _testEntity!, "users", Sqlx.Generator.SqlDefine.Oracle);

        // Assert
        StringAssert.Contains(result.ProcessedSql.ToUpperInvariant(), "RETURNING",
            "Oracle应该支持RETURNING");
        StringAssert.Contains(result.ProcessedSql.ToUpperInvariant(), "INTO",
            "Oracle应该支持INTO");
    }

    #endregion

    #region Oracle - MERGE语句

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("Oracle")]
    [TestCategory("Phase1")]
    [Description("Oracle应该支持MERGE语句(Upsert)")]
    public void Oracle_Upsert_ShouldSupport_MERGE()
    {
        // Arrange
        var template = @"MERGE INTO users target
            USING (SELECT :id as id, :name as name FROM DUAL) source
            ON (target.id = source.id)
            WHEN MATCHED THEN UPDATE SET name = source.name
            WHEN NOT MATCHED THEN INSERT (id, name) VALUES (source.id, source.name)";
        var engine = new SqlTemplateEngine(Sqlx.Generator.SqlDefine.Oracle);

        // Act
        var result = engine.ProcessTemplate(template, _testMethod!, _testEntity!, "users", Sqlx.Generator.SqlDefine.Oracle);

        // Assert
        StringAssert.Contains(result.ProcessedSql.ToUpperInvariant(), "MERGE",
            "Oracle应该支持MERGE");
    }

    #endregion

    #region Oracle - DUAL表

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("Oracle")]
    [TestCategory("Phase1")]
    [Description("Oracle应该支持DUAL表")]
    public void Oracle_ShouldSupport_DUAL_Table()
    {
        // Arrange
        var template = "SELECT SYSDATE FROM DUAL";
        var engine = new SqlTemplateEngine(Sqlx.Generator.SqlDefine.Oracle);

        // Act
        var result = engine.ProcessTemplate(template, _testMethod!, _testEntity!, "DUAL", Sqlx.Generator.SqlDefine.Oracle);

        // Assert
        StringAssert.Contains(result.ProcessedSql.ToUpperInvariant(), "DUAL",
            "Oracle应该支持DUAL表");
    }

    #endregion

    #region Oracle - 日期时间函数

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("Oracle")]
    [TestCategory("Phase1")]
    [Description("Oracle应该支持SYSDATE")]
    public void Oracle_DateTime_ShouldSupport_SYSDATE()
    {
        // Arrange
        var template = "INSERT INTO logs (message, created_at) VALUES (:message, SYSDATE)";
        var engine = new SqlTemplateEngine(Sqlx.Generator.SqlDefine.Oracle);

        // Act
        var result = engine.ProcessTemplate(template, _testMethod!, _testEntity!, "logs", Sqlx.Generator.SqlDefine.Oracle);

        // Assert
        StringAssert.Contains(result.ProcessedSql.ToUpperInvariant(), "SYSDATE",
            "Oracle应该支持SYSDATE");
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("Oracle")]
    [TestCategory("Phase1")]
    [Description("Oracle应该支持SYSTIMESTAMP")]
    public void Oracle_DateTime_ShouldSupport_SYSTIMESTAMP()
    {
        // Arrange
        var template = "SELECT SYSTIMESTAMP FROM DUAL";
        var engine = new SqlTemplateEngine(Sqlx.Generator.SqlDefine.Oracle);

        // Act
        var result = engine.ProcessTemplate(template, _testMethod!, _testEntity!, "DUAL", Sqlx.Generator.SqlDefine.Oracle);

        // Assert
        StringAssert.Contains(result.ProcessedSql.ToUpperInvariant(), "SYSTIMESTAMP",
            "Oracle应该支持SYSTIMESTAMP");
    }

    #endregion

    #region Oracle - 字符串连接

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("Oracle")]
    [TestCategory("Phase1")]
    [Description("Oracle应该支持||字符串连接运算符")]
    public void Oracle_StringConcat_ShouldSupport_Pipe_Operator()
    {
        // Arrange
        var template = "SELECT first_name || ' ' || last_name as full_name FROM users";
        var engine = new SqlTemplateEngine(Sqlx.Generator.SqlDefine.Oracle);

        // Act
        var result = engine.ProcessTemplate(template, _testMethod!, _testEntity!, "users", Sqlx.Generator.SqlDefine.Oracle);

        // Assert
        StringAssert.Contains(result.ProcessedSql, "||",
            "Oracle应该支持||运算符");
    }

    #endregion

    #region Oracle - SEQUENCE

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("Oracle")]
    [TestCategory("Phase1")]
    [Description("Oracle应该支持SEQUENCE")]
    public void Oracle_ShouldSupport_SEQUENCE()
    {
        // Arrange
        var template = "INSERT INTO users (id, name) VALUES (user_seq.NEXTVAL, :name)";
        var engine = new SqlTemplateEngine(Sqlx.Generator.SqlDefine.Oracle);

        // Act
        var result = engine.ProcessTemplate(template, _testMethod!, _testEntity!, "users", Sqlx.Generator.SqlDefine.Oracle);

        // Assert
        StringAssert.Contains(result.ProcessedSql.ToUpperInvariant(), "NEXTVAL",
            "Oracle应该支持SEQUENCE的NEXTVAL");
    }

    #endregion

    #region Oracle - 分页

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("Oracle")]
    [TestCategory("Phase1")]
    [Description("Oracle应该支持FETCH FIRST...ROWS ONLY")]
    public void Oracle_Pagination_ShouldSupport_FETCH_FIRST()
    {
        // Arrange
        var template = "SELECT * FROM users ORDER BY id FETCH FIRST 10 ROWS ONLY";
        var engine = new SqlTemplateEngine(Sqlx.Generator.SqlDefine.Oracle);

        // Act
        var result = engine.ProcessTemplate(template, _testMethod!, _testEntity!, "users", Sqlx.Generator.SqlDefine.Oracle);

        // Assert
        StringAssert.Contains(result.ProcessedSql.ToUpperInvariant(), "FETCH",
            "Oracle应该支持FETCH FIRST");
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("Oracle")]
    [TestCategory("Phase1")]
    [Description("Oracle应该支持OFFSET...ROWS FETCH NEXT...ROWS ONLY")]
    public void Oracle_Pagination_ShouldSupport_OFFSET_FETCH()
    {
        // Arrange
        var template = "SELECT * FROM users ORDER BY id OFFSET 20 ROWS FETCH NEXT 10 ROWS ONLY";
        var engine = new SqlTemplateEngine(Sqlx.Generator.SqlDefine.Oracle);

        // Act
        var result = engine.ProcessTemplate(template, _testMethod!, _testEntity!, "users", Sqlx.Generator.SqlDefine.Oracle);

        // Assert
        StringAssert.Contains(result.ProcessedSql.ToUpperInvariant(), "OFFSET",
            "Oracle应该支持OFFSET");
        StringAssert.Contains(result.ProcessedSql.ToUpperInvariant(), "FETCH",
            "Oracle应该支持FETCH NEXT");
    }

    #endregion

    #region Oracle - 数据类型

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("Oracle")]
    [TestCategory("Phase1")]
    [Description("Oracle应该支持NUMBER类型")]
    public void Oracle_DataType_ShouldSupport_NUMBER()
    {
        // Arrange
        var template = "CREATE TABLE test (id NUMBER(10), amount NUMBER(10,2))";
        var engine = new SqlTemplateEngine(Sqlx.Generator.SqlDefine.Oracle);

        // Act
        var result = engine.ProcessTemplate(template, _testMethod!, _testEntity!, "test", Sqlx.Generator.SqlDefine.Oracle);

        // Assert
        StringAssert.Contains(result.ProcessedSql.ToUpperInvariant(), "NUMBER",
            "Oracle应该支持NUMBER类型");
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Dialect")]
    [TestCategory("Oracle")]
    [TestCategory("Phase1")]
    [Description("Oracle应该支持VARCHAR2类型")]
    public void Oracle_DataType_ShouldSupport_VARCHAR2()
    {
        // Arrange
        var template = "CREATE TABLE test (id NUMBER, name VARCHAR2(100))";
        var engine = new SqlTemplateEngine(Sqlx.Generator.SqlDefine.Oracle);

        // Act
        var result = engine.ProcessTemplate(template, _testMethod!, _testEntity!, "test", Sqlx.Generator.SqlDefine.Oracle);

        // Assert
        StringAssert.Contains(result.ProcessedSql.ToUpperInvariant(), "VARCHAR2",
            "Oracle应该支持VARCHAR2类型");
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


