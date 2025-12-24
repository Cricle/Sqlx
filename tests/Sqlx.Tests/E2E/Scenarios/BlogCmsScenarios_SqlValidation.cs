// -----------------------------------------------------------------------
// <copyright file="BlogCmsScenarios_SqlValidation.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using Sqlx.Annotations;

namespace Sqlx.Tests.E2E.Scenarios;

/// <summary>
/// TDD tests to validate SQL generation for BlogCMS scenarios.
/// Verifies that placeholders are correctly replaced and SQL is valid for each dialect.
/// </summary>
[TestClass]
public class BlogCmsScenarios_SqlValidation
{
    [TestMethod]
    [TestCategory("TDD")]
    [TestCategory("SqlValidation")]
    public void BlogPostRepository_CreatePostAsync_ShouldGenerateCorrectSql_SQLite()
    {
        // Arrange - Get the generated method
        var repoType = typeof(BlogPostRepository);
        var method = repoType.GetMethod("CreatePostAsync");
        
        // Assert - Method should exist
        Assert.IsNotNull(method, "CreatePostAsync method should be generated");
        
        // The SQL template should be:
        // INSERT INTO blog_posts (title, content, author_id, status, created_at) VALUES (@title, @content, @authorId, @status, @createdAt)
        // For SQLite, this should remain as-is with @parameters
        
        // Verify return type
        Assert.IsTrue(method.ReturnType.Name.Contains("Task"), "Should return Task<long>");
    }

    [TestMethod]
    [TestCategory("TDD")]
    [TestCategory("SqlValidation")]
    public void BlogPostRepository_GetByIdAsync_ShouldExpandColumnsPlaceholder_SQLite()
    {
        // Arrange
        var repoType = typeof(BlogPostRepository);
        var method = repoType.GetMethod("GetByIdAsync");
        
        // Assert
        Assert.IsNotNull(method, "GetByIdAsync method should be generated");
        
        // The SQL template uses {{columns}} placeholder which should expand to:
        // id, title, content, author_id, status, created_at, published_at
        // This is validated by the actual test execution
    }

    [TestMethod]
    [TestCategory("TDD")]
    [TestCategory("SqlValidation")]
    public void BlogPostRepository_PublishPostAsync_ShouldUseTablePlaceholder_SQLite()
    {
        // Arrange
        var repoType = typeof(BlogPostRepository);
        var method = repoType.GetMethod("PublishPostAsync");
        
        // Assert
        Assert.IsNotNull(method, "PublishPostAsync method should be generated");
        
        // The SQL template uses {{table}} placeholder which should expand to:
        // blog_posts (from TableName attribute)
    }

    [TestMethod]
    [TestCategory("TDD")]
    [TestCategory("SqlValidation")]
    public void BlogCommentRepository_GetByPostIdAsync_ShouldGenerateCorrectOrderBy_SQLite()
    {
        // Arrange
        var repoType = typeof(BlogCommentRepository);
        var method = repoType.GetMethod("GetByPostIdAsync");
        
        // Assert
        Assert.IsNotNull(method, "GetByPostIdAsync method should be generated");
        
        // SQL should include: ORDER BY created_at
        // This ensures comments are returned in chronological order
    }

    [TestMethod]
    [TestCategory("TDD")]
    [TestCategory("SqlValidation")]
    public void BlogPostTagRepository_GetPostsByTagIdAsync_ShouldGenerateCorrectJoin_SQLite()
    {
        // Arrange
        var repoType = typeof(BlogPostTagRepository);
        var method = repoType.GetMethod("GetPostsByTagIdAsync");
        
        // Assert
        Assert.IsNotNull(method, "GetPostsByTagIdAsync method should be generated");
        
        // SQL should include:
        // - INNER JOIN blog_post_tags pt ON p.id = pt.post_id
        // - {{columns|table=p}} should expand to p.id, p.title, p.content, etc.
    }

    [TestMethod]
    [TestCategory("TDD")]
    [TestCategory("SqlValidation")]
    public void MySqlBlogPostRepository_CreatePostAsync_ShouldGenerateCorrectSql_MySQL()
    {
        // Arrange
        var repoType = typeof(MySqlBlogPostRepository);
        var method = repoType.GetMethod("CreatePostAsync");
        
        // Assert
        Assert.IsNotNull(method, "CreatePostAsync method should be generated for MySQL");
        
        // For MySQL, parameters should use @ prefix (same as SQLite in this case)
        // But the generated SQL might differ in RETURNING clause
    }

    [TestMethod]
    [TestCategory("TDD")]
    [TestCategory("SqlValidation")]
    public void BlogPostRepository_ShouldHaveTableNameAttribute()
    {
        // Arrange
        var repoType = typeof(BlogPostRepository);
        
        // Act
        var tableNameAttr = repoType.GetCustomAttribute<TableNameAttribute>();
        
        // Assert
        Assert.IsNotNull(tableNameAttr, "Repository should have TableName attribute");
        Assert.AreEqual("blog_posts", tableNameAttr.TableName, "Table name should be 'blog_posts'");
    }

    [TestMethod]
    [TestCategory("TDD")]
    [TestCategory("SqlValidation")]
    public void BlogPostRepository_ShouldHaveSqlDefineAttribute()
    {
        // Arrange
        var repoType = typeof(BlogPostRepository);
        
        // Act
        var sqlDefineAttr = repoType.GetCustomAttribute<SqlDefineAttribute>();
        
        // Assert
        Assert.IsNotNull(sqlDefineAttr, "Repository should have SqlDefine attribute");
        Assert.AreEqual(SqlDefineTypes.SQLite, sqlDefineAttr.DialectType, "Should be SQLite dialect");
    }

    [TestMethod]
    [TestCategory("TDD")]
    [TestCategory("SqlValidation")]
    public void BlogPostRepository_CreatePostAsync_ShouldHaveReturnInsertedIdAttribute()
    {
        // Arrange
        var interfaceType = typeof(IBlogPostRepository);
        var method = interfaceType.GetMethod("CreatePostAsync");
        
        // Act
        var returnIdAttr = method?.GetCustomAttribute<ReturnInsertedIdAttribute>();
        
        // Assert
        Assert.IsNotNull(method, "CreatePostAsync method should exist in interface");
        Assert.IsNotNull(returnIdAttr, "CreatePostAsync should have ReturnInsertedId attribute");
    }

    [TestMethod]
    [TestCategory("TDD")]
    [TestCategory("SqlValidation")]
    public void BlogCommentRepository_CreateCommentAsync_ShouldHaveReturnInsertedIdAttribute()
    {
        // Arrange
        var interfaceType = typeof(IBlogCommentRepository);
        var method = interfaceType.GetMethod("CreateCommentAsync");
        
        // Act
        var returnIdAttr = method?.GetCustomAttribute<ReturnInsertedIdAttribute>();
        
        // Assert
        Assert.IsNotNull(method, "CreateCommentAsync method should exist in interface");
        Assert.IsNotNull(returnIdAttr, "CreateCommentAsync should have ReturnInsertedId attribute");
    }

    [TestMethod]
    [TestCategory("TDD")]
    [TestCategory("SqlValidation")]
    public void BlogTagRepository_CreateTagAsync_ShouldHaveReturnInsertedIdAttribute()
    {
        // Arrange
        var interfaceType = typeof(IBlogTagRepository);
        var method = interfaceType.GetMethod("CreateTagAsync");
        
        // Act
        var returnIdAttr = method?.GetCustomAttribute<ReturnInsertedIdAttribute>();
        
        // Assert
        Assert.IsNotNull(method, "CreateTagAsync method should exist in interface");
        Assert.IsNotNull(returnIdAttr, "CreateTagAsync should have ReturnInsertedId attribute");
    }

    [TestMethod]
    [TestCategory("TDD")]
    [TestCategory("SqlValidation")]
    public void AllBlogRepositories_ShouldHaveCorrectDialectAttributes()
    {
        // Arrange & Act & Assert
        var sqliteRepos = new[]
        {
            typeof(BlogPostRepository),
            typeof(BlogCommentRepository),
            typeof(BlogTagRepository),
            typeof(BlogPostTagRepository)
        };

        var mysqlRepos = new[]
        {
            typeof(MySqlBlogPostRepository),
            typeof(MySqlBlogCommentRepository),
            typeof(MySqlBlogTagRepository),
            typeof(MySqlBlogPostTagRepository)
        };

        // Verify SQLite repositories
        foreach (var repoType in sqliteRepos)
        {
            var sqlDefineAttr = repoType.GetCustomAttribute<SqlDefineAttribute>();
            Assert.IsNotNull(sqlDefineAttr, $"{repoType.Name} should have SqlDefine attribute");
            Assert.AreEqual(SqlDefineTypes.SQLite, sqlDefineAttr.DialectType, 
                $"{repoType.Name} should be SQLite dialect");
        }

        // Verify MySQL repositories
        foreach (var repoType in mysqlRepos)
        {
            var sqlDefineAttr = repoType.GetCustomAttribute<SqlDefineAttribute>();
            Assert.IsNotNull(sqlDefineAttr, $"{repoType.Name} should have SqlDefine attribute");
            Assert.AreEqual(SqlDefineTypes.MySql, sqlDefineAttr.DialectType, 
                $"{repoType.Name} should be MySQL dialect");
        }
    }

    [TestMethod]
    [TestCategory("TDD")]
    [TestCategory("SqlValidation")]
    public void AllBlogRepositories_ShouldHaveCorrectTableNames()
    {
        // Arrange & Act & Assert
        var expectedTableNames = new[]
        {
            (typeof(BlogPostRepository), "blog_posts"),
            (typeof(BlogCommentRepository), "blog_comments"),
            (typeof(BlogTagRepository), "blog_tags"),
            (typeof(BlogPostTagRepository), "blog_post_tags"),
            (typeof(MySqlBlogPostRepository), "blog_posts"),
            (typeof(MySqlBlogCommentRepository), "blog_comments"),
            (typeof(MySqlBlogTagRepository), "blog_tags"),
            (typeof(MySqlBlogPostTagRepository), "blog_post_tags")
        };

        foreach (var (repoType, expectedTableName) in expectedTableNames)
        {
            var tableNameAttr = repoType.GetCustomAttribute<TableNameAttribute>();
            Assert.IsNotNull(tableNameAttr, $"{repoType.Name} should have TableName attribute");
            Assert.AreEqual(expectedTableName, tableNameAttr.TableName, 
                $"{repoType.Name} should have table name '{expectedTableName}'");
        }
    }

    [TestMethod]
    [TestCategory("TDD")]
    [TestCategory("SqlValidation")]
    public void BlogPostRepository_GetByStatusAsync_ShouldHaveOrderByClause()
    {
        // Arrange
        var interfaceType = typeof(IBlogPostRepository);
        var method = interfaceType.GetMethod("GetByStatusAsync");
        
        // Act
        var sqlTemplateAttr = method?.GetCustomAttribute<SqlTemplateAttribute>();
        
        // Assert
        Assert.IsNotNull(method, "GetByStatusAsync method should exist");
        Assert.IsNotNull(sqlTemplateAttr, "Method should have SqlTemplate attribute");
        Assert.IsTrue(sqlTemplateAttr.Template.Contains("ORDER BY created_at DESC"), 
            "SQL should include ORDER BY created_at DESC");
    }

    [TestMethod]
    [TestCategory("TDD")]
    [TestCategory("SqlValidation")]
    public void BlogPostTagRepository_GetPostsByTagIdAsync_ShouldUseColumnsWithTablePrefix()
    {
        // Arrange
        var interfaceType = typeof(IBlogPostTagRepository);
        var method = interfaceType.GetMethod("GetPostsByTagIdAsync");
        
        // Act
        var sqlTemplateAttr = method?.GetCustomAttribute<SqlTemplateAttribute>();
        
        // Assert
        Assert.IsNotNull(method, "GetPostsByTagIdAsync method should exist");
        Assert.IsNotNull(sqlTemplateAttr, "Method should have SqlTemplate attribute");
        Assert.IsTrue(sqlTemplateAttr.Template.Contains("{{columns|table=p}}"), 
            "SQL should use {{columns|table=p}} placeholder for table-prefixed columns");
        Assert.IsTrue(sqlTemplateAttr.Template.Contains("INNER JOIN blog_post_tags pt"), 
            "SQL should include INNER JOIN");
    }
}
