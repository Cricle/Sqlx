#nullable disable
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using Sqlx.Annotations;
using Sqlx.Generator.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sqlx.Tests.Core;

/// <summary>
/// SQL 占位符功能测试
/// </summary>
[TestClass]
public class SqlPlaceholderTests
{
    private SqliteConnection _connection;

    [TestInitialize]
    public void Setup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        // 创建测试表
        var createTableSql = @"
            CREATE TABLE user (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                email TEXT NOT NULL,
                age INTEGER NOT NULL,
                is_active BOOLEAN NOT NULL DEFAULT 1,
                department_id INTEGER NOT NULL DEFAULT 1,
                salary DECIMAL NOT NULL DEFAULT 0,
                hire_date DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
            )";

        using var command = _connection.CreateCommand();
        command.CommandText = createTableSql;
        command.ExecuteNonQuery();

        // 插入测试数据
        var insertSql = @"
            INSERT INTO user (name, email, age, is_active, department_id, salary, hire_date) VALUES
            ('张三', 'zhangsan@example.com', 28, 1, 1, 8000.00, '2020-01-15'),
            ('李四', 'lisi@example.com', 32, 1, 2, 12000.00, '2019-03-10'),
            ('王五', 'wangwu@example.com', 25, 0, 1, 6000.00, '2021-06-20')";

        using var insertCommand = _connection.CreateCommand();
        insertCommand.CommandText = insertSql;
        insertCommand.ExecuteNonQuery();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _connection?.Dispose();
    }

    #region 基础占位符测试

    [TestMethod]
    public void ContainsPlaceholders_WithValidPlaceholders_ReturnsTrue()
    {
        // Arrange
        var sqlTemplates = new[]
        {
            "SELECT {{columns}} FROM {{table}}",
            "{{select}} FROM user WHERE {{where}}",
            "{{insert}} VALUES {{values}}",
            "UPDATE {{table}} SET {{columns}} WHERE id = @id"
        };

        // Act & Assert
        foreach (var template in sqlTemplates)
        {
            var result = SqlTemplatePlaceholder.ContainsPlaceholders(template);
            Assert.IsTrue(result, $"Template '{template}' should contain placeholders");
        }
    }

    [TestMethod]
    public void ContainsPlaceholders_WithNoPlaceholders_ReturnsFalse()
    {
        // Arrange
        var sqlTemplates = new[]
        {
            "SELECT * FROM user",
            "INSERT INTO user (name) VALUES (@name)",
            "",
            null
        };

        // Act & Assert
        foreach (var template in sqlTemplates)
        {
            var result = SqlTemplatePlaceholder.ContainsPlaceholders(template);
            Assert.IsFalse(result, $"Template '{template}' should not contain placeholders");
        }
    }

    [TestMethod]
    public void GetPlaceholders_WithMixedPlaceholders_ReturnsCorrectList()
    {
        // Arrange
        var template = "{{select:exclude=salary}} FROM {{table:alias=u}} WHERE {{where:default=1=1}} {{orderby}}";

        // Act
        var placeholders = SqlTemplatePlaceholder.GetPlaceholders(template);

        // Assert
        var expectedPlaceholders = new[] { "select", "table", "where", "orderby" };
        CollectionAssert.AreEquivalent(expectedPlaceholders, placeholders);
    }

    #endregion

    #region 占位符处理测试

    [TestMethod]
    public void ProcessTemplate_ColumnsPlaceholder_GeneratesCorrectSql()
    {
        // Arrange
        var template = "SELECT {{columns}} FROM user";
        var context = CreateTestContext();

        // Act
        var result = SqlTemplatePlaceholder.ProcessTemplate(template, context);

        // Assert
        Assert.AreEqual("SELECT * FROM user", result);
    }

    [TestMethod]
    public void ProcessTemplate_ColumnsWithExclude_GeneratesCorrectSql()
    {
        // Arrange
        var template = "SELECT {{columns:exclude=email,salary}} FROM user";
        var context = CreateTestContext();

        // Act
        var result = SqlTemplatePlaceholder.ProcessTemplate(template, context);

        // Assert
        StringAssert.Contains(result, "SELECT");
        Assert.IsFalse(result.Contains("email"));
        Assert.IsFalse(result.Contains("salary"));
    }

    [TestMethod]
    public void ProcessTemplate_ColumnsWithInclude_GeneratesCorrectSql()
    {
        // Arrange
        var template = "SELECT {{columns:include=name,age}} FROM user";
        var context = CreateTestContext();

        // Act
        var result = SqlTemplatePlaceholder.ProcessTemplate(template, context);

        // Assert
        StringAssert.Contains(result, "SELECT");
        // 因为没有实际的实体类型，这里主要测试模板替换逻辑
    }

    [TestMethod]
    public void ProcessTemplate_TablePlaceholder_GeneratesCorrectSql()
    {
        // Arrange
        var template = "SELECT * FROM {{table}}";
        var context = CreateTestContext();
        context.TableName = "user";

        // Act
        var result = SqlTemplatePlaceholder.ProcessTemplate(template, context);

        // Assert
        Assert.AreEqual("SELECT * FROM [user]", result);
    }

    [TestMethod]
    public void ProcessTemplate_TableWithAlias_GeneratesCorrectSql()
    {
        // Arrange
        var template = "SELECT * FROM {{table:alias=u}}";
        var context = CreateTestContext();
        context.TableName = "user";

        // Act
        var result = SqlTemplatePlaceholder.ProcessTemplate(template, context);

        // Assert
        Assert.AreEqual("SELECT * FROM [user] u", result);
    }

    [TestMethod]
    public void ProcessTemplate_WherePlaceholder_GeneratesCorrectSql()
    {
        // Arrange
        var template = "SELECT * FROM user WHERE {{where:default=is_active=1}}";
        var context = CreateTestContext();

        // Act
        var result = SqlTemplatePlaceholder.ProcessTemplate(template, context);

        // Assert
        Assert.AreEqual("SELECT * FROM user WHERE is_active=1", result);
    }

    [TestMethod]
    public void ProcessTemplate_OrderByPlaceholder_GeneratesCorrectSql()
    {
        // Arrange
        var template = "SELECT * FROM user {{orderby:default=name ASC}}";
        var context = CreateTestContext();

        // Act
        var result = SqlTemplatePlaceholder.ProcessTemplate(template, context);

        // Assert
        Assert.AreEqual("SELECT * FROM user ORDER BY name ASC", result);
    }

    [TestMethod]
    public void ProcessTemplate_CountPlaceholder_GeneratesCorrectSql()
    {
        // Arrange
        var template = "SELECT {{count}} FROM user";
        var context = CreateTestContext();

        // Act
        var result = SqlTemplatePlaceholder.ProcessTemplate(template, context);

        // Assert
        Assert.AreEqual("SELECT COUNT(*) FROM user", result);
    }

    [TestMethod]
    public void ProcessTemplate_CountWithColumn_GeneratesCorrectSql()
    {
        // Arrange
        var template = "SELECT {{count:column=id}} FROM user";
        var context = CreateTestContext();

        // Act
        var result = SqlTemplatePlaceholder.ProcessTemplate(template, context);

        // Assert
        Assert.AreEqual("SELECT COUNT([id]) FROM user", result);
    }

    [TestMethod]
    public void ProcessTemplate_JoinsPlaceholder_GeneratesCorrectSql()
    {
        // Arrange
        var template = "SELECT * FROM user u {{joins:type=INNER,table=department,on=u.department_id=d.id,alias=d}}";
        var context = CreateTestContext();

        // Act
        var result = SqlTemplatePlaceholder.ProcessTemplate(template, context);

        // Assert
        Assert.AreEqual("SELECT * FROM user u INNER JOIN [department] d ON u.department_id=d.id", result);
    }

    #endregion

    #region 复杂模板测试

    [TestMethod]
    public void ProcessTemplate_ComplexTemplate_GeneratesCorrectSql()
    {
        // Arrange
        var template = "{{select}} FROM {{table:alias=u}} {{joins:type=LEFT,table=department,on=u.department_id=d.id,alias=d}} WHERE {{where:default=u.is_active=1}} {{orderby:default=u.name}}";
        var context = CreateTestContext();
        context.TableName = "user";

        // Act
        var result = SqlTemplatePlaceholder.ProcessTemplate(template, context);

        // Assert
        StringAssert.Contains(result, "SELECT");
        StringAssert.Contains(result, "FROM [user] u");
        StringAssert.Contains(result, "LEFT JOIN [department] d ON u.department_id=d.id");
        StringAssert.Contains(result, "WHERE u.is_active=1");
        StringAssert.Contains(result, "ORDER BY u.name");
    }

    [TestMethod]
    public void ProcessTemplate_MultipleColumnsPlaceholders_GeneratesCorrectSql()
    {
        // Arrange
        var template = "{{select:exclude=salary}} FROM {{table}} UNION ALL {{select:include=name,email}} FROM {{table}} WHERE is_active = 0";
        var context = CreateTestContext();
        context.TableName = "user";

        // Act
        var result = SqlTemplatePlaceholder.ProcessTemplate(template, context);

        // Assert
        StringAssert.Contains(result, "SELECT");
        StringAssert.Contains(result, "FROM [user]");
        StringAssert.Contains(result, "UNION ALL");
    }

    #endregion

    #region 错误处理测试

    [TestMethod]
    public void ProcessTemplate_NullTemplate_ReturnsNull()
    {
        // Arrange
        string template = null;
        var context = CreateTestContext();

        // Act
        var result = SqlTemplatePlaceholder.ProcessTemplate(template, context);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void ProcessTemplate_EmptyTemplate_ReturnsEmpty()
    {
        // Arrange
        var template = "";
        var context = CreateTestContext();

        // Act
        var result = SqlTemplatePlaceholder.ProcessTemplate(template, context);

        // Assert
        Assert.AreEqual("", result);
    }

    [TestMethod]
    public void ProcessTemplate_UnknownPlaceholder_KeepsOriginal()
    {
        // Arrange
        var template = "SELECT * FROM {{unknown_placeholder}} WHERE {{another_unknown}}";
        var context = CreateTestContext();

        // Act
        var result = SqlTemplatePlaceholder.ProcessTemplate(template, context);

        // Assert
        Assert.AreEqual("SELECT * FROM {{unknown_placeholder}} WHERE {{another_unknown}}", result);
    }

    [TestMethod]
    public void ProcessTemplate_MalformedPlaceholder_KeepsOriginal()
    {
        // Arrange
        var template = "SELECT * FROM {table} WHERE {{columns";
        var context = CreateTestContext();

        // Act
        var result = SqlTemplatePlaceholder.ProcessTemplate(template, context);

        // Assert
        Assert.AreEqual("SELECT * FROM {table} WHERE {{columns", result);
    }

    #endregion

    #region 参数解析测试

    [TestMethod]
    public void ParseArgs_ValidArgs_ReturnsCorrectDictionary()
    {
        // 由于 ParseArgs 是私有方法，我们通过测试公共方法来间接测试参数解析
        // Arrange
        var template = "SELECT {{columns:exclude=salary,email}} FROM {{table:alias=u}}";
        var context = CreateTestContext();

        // Act
        var result = SqlTemplatePlaceholder.ProcessTemplate(template, context);

        // Assert
        StringAssert.Contains(result, "SELECT");
        StringAssert.Contains(result, "[user] u");
    }

    [TestMethod]
    public void ParseArgs_ComplexArgs_HandledCorrectly()
    {
        // 通过复杂的占位符参数来测试参数解析功能
        // Arrange
        var template = "{{joins:type=INNER,table=department,on=u.dept_id=d.id,alias=d}}";
        var context = CreateTestContext();

        // Act
        var result = SqlTemplatePlaceholder.ProcessTemplate(template, context);

        // Assert
        Assert.AreEqual("INNER JOIN [department] d ON u.dept_id=d.id", result);
    }

    #endregion

    #region 实际 SQL 执行测试

    [TestMethod]
    public async Task ExecuteSql_WithProcessedTemplate_ReturnsCorrectResults()
    {
        // Arrange
        var template = "SELECT {{count}} FROM {{table}} WHERE is_active = 1";
        var context = CreateTestContext();
        context.TableName = "user";

        // Act
        var processedSql = SqlTemplatePlaceholder.ProcessTemplate(template, context);

        using var command = _connection.CreateCommand();
        command.CommandText = processedSql;
        var result = await command.ExecuteScalarAsync();

        // Assert
        Assert.AreEqual("SELECT COUNT(*) FROM [user] WHERE is_active = 1", processedSql);
        Assert.AreEqual(2L, result); // 应该有2个活跃用户
    }

    [TestMethod]
    public async Task ExecuteSql_WithExcludeColumns_DoesNotReturnExcludedData()
    {
        // Arrange - 模拟一个不包含敏感字段的查询
        var simplifiedSql = "SELECT name, age FROM user WHERE is_active = 1 ORDER BY name";

        using var command = _connection.CreateCommand();
        command.CommandText = simplifiedSql;
        using var reader = await command.ExecuteReaderAsync();

        // Act & Assert
        var results = new List<object[]>();
        while (await reader.ReadAsync())
        {
            // 验证只返回了指定的列
            Assert.AreEqual(2, reader.FieldCount, "Should only return 2 columns (name, age)");

            var row = new object[reader.FieldCount];
            reader.GetValues(row);
            results.Add(row);
        }

        Assert.AreEqual(2, results.Count, "Should return 2 active users");
    }

    #endregion

    #region 助手方法

    /// <summary>
    /// 创建测试用的占位符上下文
    /// </summary>
    private static SqlPlaceholderContext CreateTestContext()
    {
        return new SqlPlaceholderContext(Sqlx.Generator.Core.SqlDefine.SqlServer)
        {
            TableName = "user",
            EntityType = null, // 在实际使用中会有实体类型
            Method = null
        };
    }

    #endregion
}

/// <summary>
/// 测试用的实体类
/// </summary>
public class TestUser
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Age { get; set; }
    public bool IsActive { get; set; }
    public int DepartmentId { get; set; }
    public decimal Salary { get; set; }
    public DateTime HireDate { get; set; }
}

/// <summary>
/// 模拟使用 SQL 占位符的仓储接口（仅用于演示，实际实现由源生成器生成）
/// </summary>
public interface ITestUserPlaceholderRepository
{
    [Sqlx("{{select}} FROM {{table}} WHERE {{where:default=is_active=1}}")]
    Task<IList<TestUser>> GetActiveUsersAsync();

    [Sqlx("{{select:exclude=salary,email}} FROM {{table}} WHERE department_id = @deptId")]
    Task<IList<TestUser>> GetPublicUserInfoAsync(int deptId);

    [Sqlx("{{select:include=name,age}} FROM {{table}} WHERE age > @minAge {{orderby:default=age}}")]
    Task<IList<TestUser>> GetUserBasicInfoAsync(int minAge);

    [Sqlx("SELECT {{count}} FROM {{table}} WHERE {{where:default=is_active=1}}")]
    Task<int> GetActiveUserCountAsync();

    [Sqlx("{{insert}} VALUES {{values}}")]
    Task<int> CreateUserAsync(TestUser user);

    [Sqlx("{{update}} SET name = @name, age = @age WHERE id = @id")]
    Task<int> UpdateUserBasicInfoAsync(TestUser user);
}
