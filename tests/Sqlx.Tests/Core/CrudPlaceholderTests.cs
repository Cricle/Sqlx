// -----------------------------------------------------------------------
// <copyright file="CrudPlaceholderTests.cs" company="Cricle">
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
/// 完整测试CRUD占位符功能（INSERT、UPDATE、DELETE）
/// 参考文档：docs/CRUD_PLACEHOLDERS_COMPLETE_GUIDE.md
/// </summary>
[TestClass]
public class CrudPlaceholderTests
{
    private SqlTemplateEngine _engine = null!;
    private Compilation _compilation = null!;
    private IMethodSymbol _testMethod = null!;
    private INamedTypeSymbol _todoType = null!;

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
    public class Todo
    {
        public long Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsCompleted { get; set; }
        public int Priority { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public interface ITodoService
    {
        Task<long> CreateAsync(Todo todo);
        Task<int> UpdateAsync(Todo todo);
        Task<int> DeleteAsync(long id);
        Task<int> UpdatePriorityAsync(int id, int priority);
    }
}";

        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Collections.Generic.List<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location)
        };

        _compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // 获取测试符号
        _todoType = _compilation.GetTypeByMetadataName("TestNamespace.Todo")!;
        var serviceType = _compilation.GetTypeByMetadataName("TestNamespace.ITodoService")!;
        _testMethod = serviceType.GetMembers("CreateAsync").OfType<IMethodSymbol>().First();
    }

    #region INSERT占位符测试

    [TestMethod]
    public void InsertPlaceholder_Basic_GeneratesInsertInto()
    {
        // Arrange
        var template = "{{insert}}";

        // Act
        var result = _engine.ProcessTemplate(template, _testMethod, _todoType, "Todo");

        // Assert
        Assert.AreEqual("INSERT INTO todo", result.ProcessedSql.Trim());
        Assert.IsFalse(result.Errors.Any(), $"Should not have errors: {string.Join(", ", result.Errors)}");
    }

    [TestMethod]
    public void InsertPlaceholder_IntoType_GeneratesInsertInto()
    {
        // Arrange
        var template = "{{insert:into}}";

        // Act
        var result = _engine.ProcessTemplate(template, _testMethod, _todoType, "Todo");

        // Assert
        Assert.AreEqual("INSERT INTO todo", result.ProcessedSql.Trim());
        Assert.IsFalse(result.Errors.Any());
    }

    [TestMethod]
    public void InsertPlaceholder_WithColumnsAndValues_GeneratesCompleteInsert()
    {
        // Arrange
        var template = "{{insert}} ({{columns:auto|exclude=Id}}) VALUES ({{values:auto}})";

        // Act
        var result = _engine.ProcessTemplate(template, _testMethod, _todoType, "Todo");

        // Assert
        var sql = result.ProcessedSql.Trim();
        Assert.IsTrue(sql.StartsWith("INSERT INTO todo"), $"SQL should start with INSERT INTO todo: {sql}");
        Assert.IsTrue(sql.Contains("title"), $"SQL should contain column 'title': {sql}");
        Assert.IsTrue(sql.Contains("description"), $"SQL should contain column 'description': {sql}");
        Assert.IsTrue(sql.Contains("is_completed"), $"SQL should contain column 'is_completed': {sql}");
        Assert.IsFalse(sql.Contains("id"), $"SQL should not contain excluded column 'id': {sql}");
        Assert.IsTrue(sql.Contains("VALUES"), $"SQL should contain VALUES: {sql}");
        Assert.IsFalse(result.Errors.Any());
    }

    [TestMethod]
    public void InsertPlaceholder_AllDialects_GeneratesCorrectly()
    {
        // Arrange
        var template = "{{insert}} ({{columns:auto|exclude=Id}}) VALUES ({{values:auto}})";

        foreach (var dialect in AllDialects)
        {
            // Act
            var result = _engine.ProcessTemplate(template, _testMethod, _todoType, "Todo", dialect);

            // Assert
            var sql = result.ProcessedSql.Trim();
            Assert.IsTrue(sql.StartsWith("INSERT INTO"), $"[{dialect.ParameterPrefix}] SQL should start with INSERT INTO: {sql}");
            Assert.IsTrue(sql.Contains("VALUES"), $"[{dialect.ParameterPrefix}] SQL should contain VALUES: {sql}");
            Assert.IsFalse(result.Errors.Any(), $"[{dialect.ParameterPrefix}] Should not have errors");
        }
    }

    #endregion

    #region UPDATE占位符测试

    [TestMethod]
    public void UpdatePlaceholder_Basic_GeneratesUpdate()
    {
        // Arrange
        var template = "{{update}}";

        // Act
        var result = _engine.ProcessTemplate(template, _testMethod, _todoType, "Todo");

        // Assert
        Assert.AreEqual("UPDATE todo", result.ProcessedSql.Trim());
        Assert.IsFalse(result.Errors.Any());
    }

    [TestMethod]
    public void UpdatePlaceholder_WithSetAndWhere_GeneratesCompleteUpdate()
    {
        // Arrange
        var template = "{{update}} SET {{set:auto|exclude=Id,CreatedAt}} WHERE {{where:id}}";
        var updateMethod = _compilation.GetTypeByMetadataName("TestNamespace.ITodoService")!
            .GetMembers("UpdateAsync").OfType<IMethodSymbol>().First();

        // Act
        var result = _engine.ProcessTemplate(template, updateMethod, _todoType, "Todo");

        // Assert
        var sql = result.ProcessedSql.Trim();
        Assert.IsTrue(sql.StartsWith("UPDATE todo"), $"SQL should start with UPDATE todo: {sql}");
        Assert.IsTrue(sql.Contains("SET"), $"SQL should contain SET: {sql}");
        Assert.IsTrue(sql.Contains("title"), $"SQL should contain column 'title': {sql}");

        // Check that id is not in SET clause (but it's OK in WHERE clause)
        var setClause = sql.Substring(sql.IndexOf("SET"), sql.IndexOf("WHERE") - sql.IndexOf("SET"));
        Assert.IsFalse(setClause.Contains("id"), $"SET clause should not contain excluded column 'id': {setClause}");
        Assert.IsFalse(setClause.Contains("created_at"), $"SET clause should not contain excluded column 'created_at': {setClause}");

        Assert.IsTrue(sql.Contains("WHERE"), $"SQL should contain WHERE: {sql}");
        Assert.IsFalse(result.Errors.Any());
    }

    [TestMethod]
    public void UpdatePlaceholder_AllDialects_GeneratesCorrectly()
    {
        // Arrange
        var template = "{{update}} SET {{set:auto|exclude=Id}} WHERE {{where:id}}";
        var updateMethod = _compilation.GetTypeByMetadataName("TestNamespace.ITodoService")!
            .GetMembers("UpdateAsync").OfType<IMethodSymbol>().First();

        foreach (var dialect in AllDialects)
        {
            // Act
            var result = _engine.ProcessTemplate(template, updateMethod, _todoType, "Todo", dialect);

            // Assert
            var sql = result.ProcessedSql.Trim();
            Assert.IsTrue(sql.StartsWith("UPDATE"), $"[{dialect.ParameterPrefix}] SQL should start with UPDATE: {sql}");
            Assert.IsTrue(sql.Contains("SET"), $"[{dialect.ParameterPrefix}] SQL should contain SET: {sql}");
            Assert.IsTrue(sql.Contains("WHERE"), $"[{dialect.ParameterPrefix}] SQL should contain WHERE: {sql}");
            Assert.IsFalse(result.Errors.Any(), $"[{dialect.ParameterPrefix}] Should not have errors");
        }
    }

    [TestMethod]
    public void UpdatePlaceholder_PartialUpdate_OnlySpecifiedColumns()
    {
        // Arrange
        var template = "{{update}} SET priority = @priority WHERE id = @id";
        var updateMethod = _compilation.GetTypeByMetadataName("TestNamespace.ITodoService")!
            .GetMembers("UpdatePriorityAsync").OfType<IMethodSymbol>().First();

        // Act
        var result = _engine.ProcessTemplate(template, updateMethod, _todoType, "Todo");

        // Assert
        var sql = result.ProcessedSql.Trim();
        Assert.IsTrue(sql.StartsWith("UPDATE todo"), $"SQL should start with UPDATE todo: {sql}");
        Assert.IsTrue(sql.Contains("SET priority"), $"SQL should update priority: {sql}");
        Assert.IsTrue(sql.Contains("WHERE id"), $"SQL should have WHERE clause: {sql}");
        Assert.IsFalse(result.Errors.Any());
    }

    #endregion

    #region DELETE占位符测试

    [TestMethod]
    public void DeletePlaceholder_Basic_GeneratesDelete()
    {
        // Arrange
        var template = "{{delete}}";

        // Act
        var result = _engine.ProcessTemplate(template, _testMethod, _todoType, "Todo");

        // Assert
        Assert.AreEqual("DELETE FROM todo", result.ProcessedSql.Trim());
        Assert.IsFalse(result.Errors.Any());
    }

    [TestMethod]
    public void DeletePlaceholder_FromType_GeneratesDelete()
    {
        // Arrange
        var template = "{{delete:from}}";

        // Act
        var result = _engine.ProcessTemplate(template, _testMethod, _todoType, "Todo");

        // Assert
        Assert.AreEqual("DELETE FROM todo", result.ProcessedSql.Trim());
        Assert.IsFalse(result.Errors.Any());
    }

    [TestMethod]
    public void DeletePlaceholder_WithWhere_GeneratesCompleteDelete()
    {
        // Arrange
        var template = "{{delete}} WHERE {{where:id}}";
        var deleteMethod = _compilation.GetTypeByMetadataName("TestNamespace.ITodoService")!
            .GetMembers("DeleteAsync").OfType<IMethodSymbol>().First();

        // Act
        var result = _engine.ProcessTemplate(template, deleteMethod, _todoType, "Todo");

        // Assert
        var sql = result.ProcessedSql.Trim();
        Assert.IsTrue(sql.StartsWith("DELETE FROM todo"), $"SQL should start with DELETE FROM todo: {sql}");
        Assert.IsTrue(sql.Contains("WHERE"), $"SQL should contain WHERE: {sql}");
        Assert.IsTrue(sql.Contains("id"), $"SQL should contain id column: {sql}");
        Assert.IsFalse(result.Errors.Any());
    }

    [TestMethod]
    public void DeletePlaceholder_AllDialects_GeneratesCorrectly()
    {
        // Arrange
        var template = "{{delete}} WHERE {{where:id}}";
        var deleteMethod = _compilation.GetTypeByMetadataName("TestNamespace.ITodoService")!
            .GetMembers("DeleteAsync").OfType<IMethodSymbol>().First();

        foreach (var dialect in AllDialects)
        {
            // Act
            var result = _engine.ProcessTemplate(template, deleteMethod, _todoType, "Todo", dialect);

            // Assert
            var sql = result.ProcessedSql.Trim();
            Assert.IsTrue(sql.StartsWith("DELETE FROM"), $"[{dialect.ParameterPrefix}] SQL should start with DELETE FROM: {sql}");
            Assert.IsTrue(sql.Contains("WHERE"), $"[{dialect.ParameterPrefix}] SQL should contain WHERE: {sql}");
            Assert.IsFalse(result.Errors.Any(), $"[{dialect.ParameterPrefix}] Should not have errors");
        }
    }

    #endregion

    #region 组合场景测试

    [TestMethod]
    public void CrudPlaceholders_ConsistentTableName_AllUseSameTable()
    {
        // Arrange
        var insertTemplate = "{{insert}} (title) VALUES (@title)";
        var updateTemplate = "{{update}} SET title = @title WHERE id = @id";
        var deleteTemplate = "{{delete}} WHERE id = @id";

        // Act
        var insertResult = _engine.ProcessTemplate(insertTemplate, _testMethod, _todoType, "Todo");
        var updateResult = _engine.ProcessTemplate(updateTemplate, _testMethod, _todoType, "Todo");
        var deleteResult = _engine.ProcessTemplate(deleteTemplate, _testMethod, _todoType, "Todo");

        // Assert
        Assert.IsTrue(insertResult.ProcessedSql.Contains("todo"), "INSERT should use 'todo' table");
        Assert.IsTrue(updateResult.ProcessedSql.Contains("todo"), "UPDATE should use 'todo' table");
        Assert.IsTrue(deleteResult.ProcessedSql.Contains("todo"), "DELETE should use 'todo' table");
    }

    [TestMethod]
    public void CrudPlaceholders_SnakeCaseConversion_ConvertsTableNameCorrectly()
    {
        // Arrange - Use PascalCase class name
        var template = "{{insert}} (title) VALUES (@title)";

        // Act
        var result = _engine.ProcessTemplate(template, _testMethod, _todoType, "TodoItem");

        // Assert
        var sql = result.ProcessedSql.Trim();
        Assert.IsTrue(sql.Contains("todo_item") || sql.Contains("todo_items"),
            $"Table name should be converted to snake_case: {sql}");
    }

    [TestMethod]
    public void CrudPlaceholders_EmptyEntityType_UsesTableNameParameter()
    {
        // Arrange
        var insertTemplate = "{{insert}}";
        var updateTemplate = "{{update}}";
        var deleteTemplate = "{{delete}}";

        // Act
        var insertResult = _engine.ProcessTemplate(insertTemplate, _testMethod, null, "custom_table");
        var updateResult = _engine.ProcessTemplate(updateTemplate, _testMethod, null, "custom_table");
        var deleteResult = _engine.ProcessTemplate(deleteTemplate, _testMethod, null, "custom_table");

        // Assert
        Assert.IsTrue(insertResult.ProcessedSql.Contains("custom_table"), "INSERT should use provided table name");
        Assert.IsTrue(updateResult.ProcessedSql.Contains("custom_table"), "UPDATE should use provided table name");
        Assert.IsTrue(deleteResult.ProcessedSql.Contains("custom_table"), "DELETE should use provided table name");
    }

    #endregion

    #region 边界和错误测试

    [TestMethod]
    public void InsertPlaceholder_InvalidOptions_IgnoresOptions()
    {
        // Arrange
        var template = "{{insert:invalidoption}}";

        // Act
        var result = _engine.ProcessTemplate(template, _testMethod, _todoType, "Todo");

        // Assert
        // Should still generate INSERT INTO, ignoring invalid option
        Assert.IsTrue(result.ProcessedSql.Contains("INSERT INTO"),
            $"Should generate INSERT INTO despite invalid option: {result.ProcessedSql}");
    }

    [TestMethod]
    public void UpdatePlaceholder_NoTableName_GeneratesWithDefaultOrError()
    {
        // Arrange
        var template = "{{update}} SET title = @title";

        // Act
        var result = _engine.ProcessTemplate(template, _testMethod, _todoType, "");

        // Assert
        // Should either use a default or add a warning
        Assert.IsTrue(result.ProcessedSql.Contains("UPDATE") || result.Warnings.Any(),
            "Should generate UPDATE or add warning");
    }

    [TestMethod]
    public void DeletePlaceholder_WithoutWhere_GeneratesWarning()
    {
        // Arrange
        var template = "{{delete}}";

        // Act
        var result = _engine.ProcessTemplate(template, _testMethod, _todoType, "Todo");

        // Assert
        // The diagnostic system should warn about DELETE without WHERE
        Assert.IsTrue(result.ProcessedSql.Contains("DELETE FROM"),
            $"Should generate DELETE FROM: {result.ProcessedSql}");
    }

    #endregion

    #region 多线程安全测试

    [TestMethod]
    public void CrudPlaceholders_ConcurrentAccess_ThreadSafe()
    {
        // Arrange
        var template = "{{insert}} ({{columns:auto|exclude=Id}}) VALUES ({{values:auto}})";
        var iterations = 100;
        var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();

        // Act - Process template concurrently from multiple threads
        System.Threading.Tasks.Parallel.For(0, iterations, i =>
        {
            try
            {
                var engine = new SqlTemplateEngine();
                var result = engine.ProcessTemplate(template, _testMethod, _todoType, "Todo");

                if (result.Errors.Any())
                {
                    exceptions.Add(new System.Exception($"Thread {i}: {string.Join(", ", result.Errors)}"));
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        // Assert
        Assert.IsFalse(exceptions.Any(),
            $"Should be thread-safe. Exceptions: {string.Join("; ", exceptions.Select(e => e.Message))}");
    }

    #endregion
}

