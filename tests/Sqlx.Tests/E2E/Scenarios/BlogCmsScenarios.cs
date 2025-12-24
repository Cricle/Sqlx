// -----------------------------------------------------------------------
// <copyright file="BlogCmsScenarios.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;
using Sqlx.Tests.Integration;

namespace Sqlx.Tests.E2E.Scenarios;

/// <summary>
/// E2E tests for blog/CMS scenarios.
/// **Validates: Requirements 2.3**
/// </summary>

// ==================== Data Models ====================

public class BlogPost
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public long AuthorId { get; set; }
    public string Status { get; set; } = "draft"; // draft, published
    public DateTime CreatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
}

public class BlogComment
{
    public long Id { get; set; }
    public long PostId { get; set; }
    public long UserId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class BlogTag
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class BlogPostTag
{
    public long PostId { get; set; }
    public long TagId { get; set; }
}

// ==================== Repository Interfaces ====================

public partial interface IBlogPostRepository
{
    [SqlTemplate("INSERT INTO blog_posts (title, content, author_id, status, created_at) VALUES (@title, @content, @authorId, @status, @createdAt)")]
    [ReturnInsertedId]
    Task<long> CreatePostAsync(string title, string content, long authorId, string status, DateTime createdAt);

    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    Task<BlogPost?> GetByIdAsync(long id);

    [SqlTemplate("UPDATE {{table}} SET status = @status, published_at = @publishedAt WHERE id = @id")]
    Task<int> PublishPostAsync(long id, string status, DateTime publishedAt);

    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE status = @status ORDER BY created_at DESC")]
    Task<List<BlogPost>> GetByStatusAsync(string status);
}

public partial interface IBlogCommentRepository
{
    [SqlTemplate("INSERT INTO blog_comments (post_id, user_id, content, created_at) VALUES (@postId, @userId, @content, @createdAt)")]
    [ReturnInsertedId]
    Task<long> CreateCommentAsync(long postId, long userId, string content, DateTime createdAt);

    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE post_id = @postId ORDER BY created_at")]
    Task<List<BlogComment>> GetByPostIdAsync(long postId);
}

public partial interface IBlogTagRepository
{
    [SqlTemplate("INSERT INTO blog_tags (name) VALUES (@name)")]
    [ReturnInsertedId]
    Task<long> CreateTagAsync(string name);

    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE name = @name")]
    Task<BlogTag?> GetByNameAsync(string name);
}

public partial interface IBlogPostTagRepository
{
    [SqlTemplate("INSERT INTO blog_post_tags (post_id, tag_id) VALUES (@postId, @tagId)")]
    Task<int> AssignTagToPostAsync(long postId, long tagId);

    [SqlTemplate(@"SELECT p.{{columns|table=p}} FROM blog_posts p 
            INNER JOIN blog_post_tags pt ON p.id = pt.post_id 
            WHERE pt.tag_id = @tagId")]
    Task<List<BlogPost>> GetPostsByTagIdAsync(long tagId);
}

// ==================== SQLite Repository Implementations ====================

[SqlDefine(SqlDefineTypes.SQLite)]
[TableName("blog_posts")]
[RepositoryFor(typeof(IBlogPostRepository))]
public partial class BlogPostRepository(DbConnection connection) : IBlogPostRepository { }

[SqlDefine(SqlDefineTypes.SQLite)]
[TableName("blog_comments")]
[RepositoryFor(typeof(IBlogCommentRepository))]
public partial class BlogCommentRepository(DbConnection connection) : IBlogCommentRepository { }

[SqlDefine(SqlDefineTypes.SQLite)]
[TableName("blog_tags")]
[RepositoryFor(typeof(IBlogTagRepository))]
public partial class BlogTagRepository(DbConnection connection) : IBlogTagRepository { }

[SqlDefine(SqlDefineTypes.SQLite)]
[TableName("blog_post_tags")]
[RepositoryFor(typeof(IBlogPostTagRepository))]
public partial class BlogPostTagRepository(DbConnection connection) : IBlogPostTagRepository { }

// ==================== MySQL Repository Implementations ====================

[SqlDefine(SqlDefineTypes.MySql)]
[TableName("blog_posts")]
[RepositoryFor(typeof(IBlogPostRepository))]
public partial class MySqlBlogPostRepository(DbConnection connection) : IBlogPostRepository { }

[SqlDefine(SqlDefineTypes.MySql)]
[TableName("blog_comments")]
[RepositoryFor(typeof(IBlogCommentRepository))]
public partial class MySqlBlogCommentRepository(DbConnection connection) : IBlogCommentRepository { }

[SqlDefine(SqlDefineTypes.MySql)]
[TableName("blog_tags")]
[RepositoryFor(typeof(IBlogTagRepository))]
public partial class MySqlBlogTagRepository(DbConnection connection) : IBlogTagRepository { }

[SqlDefine(SqlDefineTypes.MySql)]
[TableName("blog_post_tags")]
[RepositoryFor(typeof(IBlogPostTagRepository))]
public partial class MySqlBlogPostTagRepository(DbConnection connection) : IBlogPostTagRepository { }

// ==================== Test Base Class ====================

public abstract class BlogCmsScenarios_Base
{
    protected DatabaseFixture _fixture = null!;
    protected abstract SqlDefineTypes DialectType { get; }

    protected abstract IBlogPostRepository CreatePostRepository(DbConnection connection);
    protected abstract IBlogCommentRepository CreateCommentRepository(DbConnection connection);
    protected abstract IBlogTagRepository CreateTagRepository(DbConnection connection);
    protected abstract IBlogPostTagRepository CreatePostTagRepository(DbConnection connection);

    [TestInitialize]
    public void Initialize()
    {
        _fixture = new DatabaseFixture();
        _fixture.SeedBlogCmsData(DialectType);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _fixture?.Dispose();
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("BlogCMS")]
    public async Task PostCreationAndPublishing_ShouldWork()
    {
        // Arrange
        var connection = _fixture.GetConnection(DialectType);
        var repo = CreatePostRepository(connection);
        var title = "My First Blog Post";
        var content = "This is the content of my first blog post.";
        var authorId = 1L;

        // Act - Create draft post
        var postId = await repo.CreatePostAsync(title, content, authorId, "draft", DateTime.UtcNow);

        // Assert - Post created as draft
        Assert.IsTrue(postId > 0, "Post ID should be greater than 0");
        var post = await repo.GetByIdAsync(postId);
        Assert.IsNotNull(post, "Post should be created");
        Assert.AreEqual(title, post.Title);
        Assert.AreEqual("draft", post.Status);
        Assert.IsNull(post.PublishedAt, "Draft post should not have published date");

        // Act - Publish post
        var publishedAt = DateTime.UtcNow;
        var updateResult = await repo.PublishPostAsync(postId, "published", publishedAt);

        // Assert - Post published
        Assert.AreEqual(1, updateResult, "Should update 1 post");
        var publishedPost = await repo.GetByIdAsync(postId);
        Assert.IsNotNull(publishedPost, "Published post should exist");
        Assert.AreEqual("published", publishedPost.Status);
        Assert.IsNotNull(publishedPost.PublishedAt, "Published post should have published date");
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("BlogCMS")]
    public async Task CommentManagement_ShouldWork()
    {
        // Arrange
        var connection = _fixture.GetConnection(DialectType);
        var postRepo = CreatePostRepository(connection);
        var commentRepo = CreateCommentRepository(connection);

        // Create a post first
        var postId = await postRepo.CreatePostAsync(
            "Post with Comments",
            "This post will have comments",
            1L,
            "published",
            DateTime.UtcNow);

        // Act - Add comments
        var comment1Id = await commentRepo.CreateCommentAsync(
            postId,
            1L,
            "Great post!",
            DateTime.UtcNow);

        var comment2Id = await commentRepo.CreateCommentAsync(
            postId,
            2L,
            "Thanks for sharing!",
            DateTime.UtcNow.AddMinutes(5));

        // Assert - Comments created
        Assert.IsTrue(comment1Id > 0, "Comment 1 ID should be greater than 0");
        Assert.IsTrue(comment2Id > 0, "Comment 2 ID should be greater than 0");

        // Act - Retrieve comments
        var comments = await commentRepo.GetByPostIdAsync(postId);

        // Assert - Comments retrieved
        Assert.AreEqual(2, comments.Count, "Should have 2 comments");
        Assert.AreEqual("Great post!", comments[0].Content);
        Assert.AreEqual("Thanks for sharing!", comments[1].Content);
    }

    [TestMethod]
    [TestCategory("E2E")]
    [TestCategory("BlogCMS")]
    public async Task TagAssignment_ShouldWork()
    {
        // Arrange
        var connection = _fixture.GetConnection(DialectType);
        var postRepo = CreatePostRepository(connection);
        var tagRepo = CreateTagRepository(connection);
        var postTagRepo = CreatePostTagRepository(connection);

        // Create tags
        var tag1Id = await tagRepo.CreateTagAsync("CSharp");
        var tag2Id = await tagRepo.CreateTagAsync("Testing");

        // Create posts
        var post1Id = await postRepo.CreatePostAsync(
            "C# Testing Guide",
            "Learn how to test C# code",
            1L,
            "published",
            DateTime.UtcNow);

        var post2Id = await postRepo.CreatePostAsync(
            "Advanced C# Features",
            "Explore advanced C# features",
            1L,
            "published",
            DateTime.UtcNow);

        // Act - Assign tags to posts
        await postTagRepo.AssignTagToPostAsync(post1Id, tag1Id);
        await postTagRepo.AssignTagToPostAsync(post1Id, tag2Id);
        await postTagRepo.AssignTagToPostAsync(post2Id, tag1Id);

        // Assert - Retrieve posts by tag
        var csharpPosts = await postTagRepo.GetPostsByTagIdAsync(tag1Id);
        Assert.AreEqual(2, csharpPosts.Count, "Should have 2 posts with CSharp tag");

        var testingPosts = await postTagRepo.GetPostsByTagIdAsync(tag2Id);
        Assert.AreEqual(1, testingPosts.Count, "Should have 1 post with Testing tag");
    }
}

// ==================== SQLite Tests ====================

/// <summary>
/// SQLite-specific blog/CMS E2E tests.
/// </summary>
[TestClass]
public class BlogCmsScenarios_SQLite : BlogCmsScenarios_Base
{
    protected override SqlDefineTypes DialectType => SqlDefineTypes.SQLite;

    protected override IBlogPostRepository CreatePostRepository(DbConnection connection)
        => new BlogPostRepository(connection);

    protected override IBlogCommentRepository CreateCommentRepository(DbConnection connection)
        => new BlogCommentRepository(connection);

    protected override IBlogTagRepository CreateTagRepository(DbConnection connection)
        => new BlogTagRepository(connection);

    protected override IBlogPostTagRepository CreatePostTagRepository(DbConnection connection)
        => new BlogPostTagRepository(connection);
}

// ==================== MySQL Tests ====================

/// <summary>
/// MySQL-specific blog/CMS E2E tests.
/// </summary>
[TestClass]
public class BlogCmsScenarios_MySQL : BlogCmsScenarios_Base
{
    protected override SqlDefineTypes DialectType => SqlDefineTypes.MySql;

    protected override IBlogPostRepository CreatePostRepository(DbConnection connection)
        => new MySqlBlogPostRepository(connection);

    protected override IBlogCommentRepository CreateCommentRepository(DbConnection connection)
        => new MySqlBlogCommentRepository(connection);

    protected override IBlogTagRepository CreateTagRepository(DbConnection connection)
        => new MySqlBlogTagRepository(connection);

    protected override IBlogPostTagRepository CreatePostTagRepository(DbConnection connection)
        => new MySqlBlogPostTagRepository(connection);
}
