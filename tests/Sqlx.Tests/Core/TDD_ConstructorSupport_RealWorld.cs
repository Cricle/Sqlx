// -----------------------------------------------------------------------
// <copyright file="TDD_ConstructorSupport_RealWorld.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

namespace Sqlx.Tests.Core;

// ==================== 真实世界场景：博客系统 ====================

public class BlogPost
{
    public long Id { get; set; }
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public string AuthorName { get; set; } = "";
    public DateTime PublishedAt { get; set; }
    public int ViewCount { get; set; }
    public bool IsPublished { get; set; }
    public string? Tags { get; set; }
}

public class BlogComment
{
    public long Id { get; set; }
    public long PostId { get; set; }
    public string AuthorName { get; set; } = "";
    public string Content { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public bool IsApproved { get; set; }
}

public class BlogCategory
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public int PostCount { get; set; }
}

// ==================== 博客仓储接口 ====================

public partial interface IBlogPostRepository
{
    [SqlTemplate("INSERT INTO blog_posts (title, content, author_name, published_at, view_count, is_published, tags) VALUES (@title, @content, @authorName, @publishedAt, @viewCount, @isPublished, @tags)")]
    [ReturnInsertedId]
    Task<long> CreatePostAsync(string title, string content, string authorName, DateTime publishedAt, int viewCount, bool isPublished, string? tags, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM blog_posts WHERE id = @id")]
    Task<BlogPost?> GetPostByIdAsync(long id, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM blog_posts WHERE is_published = 1 ORDER BY published_at DESC LIMIT @limit")]
    Task<List<BlogPost>> GetRecentPostsAsync(int limit, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM blog_posts WHERE author_name = @authorName ORDER BY published_at DESC")]
    Task<List<BlogPost>> GetPostsByAuthorAsync(string authorName, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM blog_posts WHERE tags LIKE '%' || @tag || '%'")]
    Task<List<BlogPost>> SearchPostsByTagAsync(string tag, CancellationToken ct = default);

    [SqlTemplate("UPDATE blog_posts SET view_count = view_count + 1 WHERE id = @id")]
    Task<int> IncrementViewCountAsync(long id, CancellationToken ct = default);

    [SqlTemplate("UPDATE blog_posts SET is_published = @isPublished WHERE id = @id")]
    Task<int> UpdatePublishStatusAsync(long id, bool isPublished, CancellationToken ct = default);

    [SqlTemplate("SELECT COUNT(*) FROM blog_posts WHERE is_published = 1")]
    Task<int> CountPublishedPostsAsync(CancellationToken ct = default);

    [SqlTemplate("SELECT SUM(view_count) FROM blog_posts WHERE author_name = @authorName")]
    Task<long> GetTotalViewsByAuthorAsync(string authorName, CancellationToken ct = default);

    [SqlTemplate("DELETE FROM blog_posts WHERE id = @id")]
    Task<int> DeletePostAsync(long id, CancellationToken ct = default);
}

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IBlogPostRepository))]
public partial class BlogPostRepository(DbConnection connection) : IBlogPostRepository
{
}

public partial interface IBlogCommentRepository
{
    [SqlTemplate("INSERT INTO blog_comments (post_id, author_name, content, created_at, is_approved) VALUES (@postId, @authorName, @content, @createdAt, @isApproved)")]
    [ReturnInsertedId]
    Task<long> CreateCommentAsync(long postId, string authorName, string content, DateTime createdAt, bool isApproved, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM blog_comments WHERE post_id = @postId AND is_approved = 1 ORDER BY created_at ASC")]
    Task<List<BlogComment>> GetApprovedCommentsAsync(long postId, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM blog_comments WHERE is_approved = 0 ORDER BY created_at DESC")]
    Task<List<BlogComment>> GetPendingCommentsAsync(CancellationToken ct = default);

    [SqlTemplate("UPDATE blog_comments SET is_approved = @isApproved WHERE id = @id")]
    Task<int> UpdateApprovalStatusAsync(long id, bool isApproved, CancellationToken ct = default);

    [SqlTemplate("SELECT COUNT(*) FROM blog_comments WHERE post_id = @postId")]
    Task<int> CountCommentsByPostAsync(long postId, CancellationToken ct = default);

    [SqlTemplate("DELETE FROM blog_comments WHERE post_id = @postId")]
    Task<int> DeleteCommentsByPostAsync(long postId, CancellationToken ct = default);
}

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IBlogCommentRepository))]
public partial class BlogCommentRepository(DbConnection connection) : IBlogCommentRepository
{
}

// ==================== 博客服务层 ====================

public class BlogService
{
    private readonly IBlogPostRepository _postRepo;
    private readonly IBlogCommentRepository _commentRepo;

    public BlogService(DbConnection connection)
    {
        _postRepo = new BlogPostRepository(connection);
        _commentRepo = new BlogCommentRepository(connection);
    }

    public async Task<long> PublishPostAsync(string title, string content, string authorName, string? tags = null, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        return await _postRepo.CreatePostAsync(title, content, authorName, now, 0, true, tags, ct);
    }

    public async Task<BlogPost?> ViewPostAsync(long postId, CancellationToken ct = default)
    {
        await _postRepo.IncrementViewCountAsync(postId, ct);
        return await _postRepo.GetPostByIdAsync(postId, ct);
    }

    public async Task<Dictionary<string, object>> GetPostDetailsAsync(long postId, CancellationToken ct = default)
    {
        var post = await _postRepo.GetPostByIdAsync(postId, ct);
        if (post == null)
            throw new InvalidOperationException("Post not found");

        var comments = await _commentRepo.GetApprovedCommentsAsync(postId, ct);
        var commentCount = await _commentRepo.CountCommentsByPostAsync(postId, ct);

        return new Dictionary<string, object>
        {
            ["PostId"] = post.Id,
            ["Title"] = post.Title,
            ["Author"] = post.AuthorName,
            ["ViewCount"] = post.ViewCount,
            ["ApprovedComments"] = comments.Count,
            ["TotalComments"] = commentCount,
            ["PublishedAt"] = post.PublishedAt
        };
    }

    public async Task<bool> DeletePostWithCommentsAsync(long postId, CancellationToken ct = default)
    {
        await _commentRepo.DeleteCommentsByPostAsync(postId, ct);
        var deleted = await _postRepo.DeletePostAsync(postId, ct);
        return deleted > 0;
    }
}

// ==================== 真实世界场景：任务管理系统 ====================

public class ProjectTask
{
    public long Id { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string AssignedTo { get; set; } = "";
    public string Status { get; set; } = ""; // Todo, InProgress, Done
    public int Priority { get; set; } // 1=Low, 2=Medium, 3=High
    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public partial interface ITaskRepository
{
    [SqlTemplate("INSERT INTO tasks (title, description, assigned_to, status, priority, due_date, created_at, completed_at) VALUES (@title, @description, @assignedTo, @status, @priority, @dueDate, @createdAt, @completedAt)")]
    [ReturnInsertedId]
    Task<long> CreateTaskAsync(string title, string? description, string assignedTo, string status, int priority, DateTime? dueDate, DateTime createdAt, DateTime? completedAt, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM tasks WHERE id = @id")]
    Task<ProjectTask?> GetTaskByIdAsync(long id, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM tasks WHERE assigned_to = @assignedTo AND status != 'Done' ORDER BY priority DESC, due_date ASC")]
    Task<List<ProjectTask>> GetActiveTasksByUserAsync(string assignedTo, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM tasks WHERE status = @status ORDER BY priority DESC")]
    Task<List<ProjectTask>> GetTasksByStatusAsync(string status, CancellationToken ct = default);

    [SqlTemplate("SELECT {{columns}} FROM tasks WHERE due_date <= @date AND status != 'Done' ORDER BY due_date ASC")]
    Task<List<ProjectTask>> GetOverdueTasksAsync(DateTime date, CancellationToken ct = default);

    [SqlTemplate("UPDATE tasks SET status = @status, completed_at = @completedAt WHERE id = @id")]
    Task<int> UpdateTaskStatusAsync(long id, string status, DateTime? completedAt, CancellationToken ct = default);

    [SqlTemplate("UPDATE tasks SET priority = @priority WHERE id = @id")]
    Task<int> UpdateTaskPriorityAsync(long id, int priority, CancellationToken ct = default);

    [SqlTemplate("SELECT COUNT(*) FROM tasks WHERE assigned_to = @assignedTo AND status = 'Done'")]
    Task<int> CountCompletedTasksByUserAsync(string assignedTo, CancellationToken ct = default);

    [SqlTemplate("SELECT status, COUNT(*) as count FROM tasks GROUP BY status")]
    Task<List<Dictionary<string, object>>> GetTaskStatisticsAsync(CancellationToken ct = default);
}

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(ITaskRepository))]
public partial class TaskRepository(DbConnection connection) : ITaskRepository
{
}

// ==================== 测试类 ====================

/// <summary>
/// 主构造函数和有参构造函数的真实世界场景测试
/// </summary>
[TestClass]
public class TDD_ConstructorSupport_RealWorld : IDisposable
{
    private readonly DbConnection _connection;

    public TDD_ConstructorSupport_RealWorld()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE blog_posts (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                title TEXT NOT NULL,
                content TEXT NOT NULL,
                author_name TEXT NOT NULL,
                published_at TEXT NOT NULL,
                view_count INTEGER DEFAULT 0,
                is_published INTEGER DEFAULT 0,
                tags TEXT
            );

            CREATE TABLE blog_comments (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                post_id INTEGER NOT NULL,
                author_name TEXT NOT NULL,
                content TEXT NOT NULL,
                created_at TEXT NOT NULL,
                is_approved INTEGER DEFAULT 0,
                FOREIGN KEY (post_id) REFERENCES blog_posts(id)
            );

            CREATE TABLE tasks (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                title TEXT NOT NULL,
                description TEXT,
                assigned_to TEXT NOT NULL,
                status TEXT NOT NULL,
                priority INTEGER NOT NULL,
                due_date TEXT,
                created_at TEXT NOT NULL,
                completed_at TEXT
            );
        ";
        cmd.ExecuteNonQuery();
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }

    // ==================== 博客系统测试 ====================

    [TestMethod]
    public async Task BlogPost_CreateAndRetrieve_ShouldWork()
    {
        // Arrange
        var repo = new BlogPostRepository(_connection);
        var now = DateTime.UtcNow;

        // Act
        var postId = await repo.CreatePostAsync(
            "My First Post",
            "This is the content of my first blog post.",
            "John Doe",
            now,
            0,
            true,
            "tech,programming",
            CancellationToken.None);

        var post = await repo.GetPostByIdAsync(postId);

        // Assert
        Assert.IsNotNull(post);
        Assert.AreEqual("My First Post", post.Title);
        Assert.AreEqual("John Doe", post.AuthorName);
        Assert.IsTrue(post.IsPublished);
        Assert.AreEqual("tech,programming", post.Tags);
    }

    [TestMethod]
    public async Task BlogPost_GetRecentPosts_ShouldReturnInOrder()
    {
        // Arrange
        var repo = new BlogPostRepository(_connection);
        var baseTime = DateTime.UtcNow;

        // Act - 创建3篇文章，时间递增
        await repo.CreatePostAsync("Post 1", "Content 1", "Author", baseTime.AddDays(-2), 0, true, null);
        await repo.CreatePostAsync("Post 2", "Content 2", "Author", baseTime.AddDays(-1), 0, true, null);
        await repo.CreatePostAsync("Post 3", "Content 3", "Author", baseTime, 0, true, null);

        var recent = await repo.GetRecentPostsAsync(2);

        // Assert
        Assert.AreEqual(2, recent.Count);
        Assert.AreEqual("Post 3", recent[0].Title); // 最新的
        Assert.AreEqual("Post 2", recent[1].Title);
    }

    [TestMethod]
    public async Task BlogPost_ViewCount_ShouldIncrement()
    {
        // Arrange
        var repo = new BlogPostRepository(_connection);
        var now = DateTime.UtcNow;

        var postId = await repo.CreatePostAsync("Test Post", "Content", "Author", now, 0, true, null);

        // Act - 多次访问
        await repo.IncrementViewCountAsync(postId);
        await repo.IncrementViewCountAsync(postId);
        await repo.IncrementViewCountAsync(postId);

        var post = await repo.GetPostByIdAsync(postId);

        // Assert
        Assert.AreEqual(3, post.ViewCount);
    }

    [TestMethod]
    public async Task BlogPost_SearchByTag_ShouldFindMatches()
    {
        // Arrange
        var repo = new BlogPostRepository(_connection);
        var now = DateTime.UtcNow;

        await repo.CreatePostAsync("Post 1", "Content", "Author", now, 0, true, "tech,ai,ml");
        await repo.CreatePostAsync("Post 2", "Content", "Author", now, 0, true, "sports,news");
        await repo.CreatePostAsync("Post 3", "Content", "Author", now, 0, true, "tech,programming");

        // Act
        var techPosts = await repo.SearchPostsByTagAsync("tech");

        // Assert
        Assert.AreEqual(2, techPosts.Count);
        Assert.IsTrue(techPosts.All(p => p.Tags!.Contains("tech")));
    }

    [TestMethod]
    public async Task BlogPost_GetPostsByAuthor_ShouldFilter()
    {
        // Arrange
        var repo = new BlogPostRepository(_connection);
        var now = DateTime.UtcNow;

        await repo.CreatePostAsync("Post 1", "Content", "Alice", now, 0, true, null);
        await repo.CreatePostAsync("Post 2", "Content", "Bob", now, 0, true, null);
        await repo.CreatePostAsync("Post 3", "Content", "Alice", now, 0, true, null);

        // Act
        var alicePosts = await repo.GetPostsByAuthorAsync("Alice");

        // Assert
        Assert.AreEqual(2, alicePosts.Count);
        Assert.IsTrue(alicePosts.All(p => p.AuthorName == "Alice"));
    }

    [TestMethod]
    public async Task BlogPost_TotalViewsByAuthor_ShouldAggregate()
    {
        // Arrange
        var repo = new BlogPostRepository(_connection);
        var now = DateTime.UtcNow;

        await repo.CreatePostAsync("Post 1", "Content", "Author", now, 100, true, null);
        await repo.CreatePostAsync("Post 2", "Content", "Author", now, 250, true, null);
        await repo.CreatePostAsync("Post 3", "Content", "Other", now, 50, true, null);

        // Act
        var totalViews = await repo.GetTotalViewsByAuthorAsync("Author");

        // Assert
        Assert.AreEqual(350, totalViews);
    }

    [TestMethod]
    public async Task BlogComment_CreateAndRetrieve_ShouldWork()
    {
        // Arrange
        var postRepo = new BlogPostRepository(_connection);
        var commentRepo = new BlogCommentRepository(_connection);
        var now = DateTime.UtcNow;

        var postId = await postRepo.CreatePostAsync("Test Post", "Content", "Author", now, 0, true, null);

        // Act
        var commentId = await commentRepo.CreateCommentAsync(postId, "Commenter", "Great post!", now, true);
        var comments = await commentRepo.GetApprovedCommentsAsync(postId);

        // Assert
        Assert.AreEqual(1, comments.Count);
        Assert.AreEqual("Great post!", comments[0].Content);
    }

    [TestMethod]
    public async Task BlogComment_ApprovalWorkflow_ShouldWork()
    {
        // Arrange
        var postRepo = new BlogPostRepository(_connection);
        var commentRepo = new BlogCommentRepository(_connection);
        var now = DateTime.UtcNow;

        var postId = await postRepo.CreatePostAsync("Test Post", "Content", "Author", now, 0, true, null);

        // Act - 创建待审核评论
        var commentId = await commentRepo.CreateCommentAsync(postId, "User", "Comment", now, false);

        var pendingBefore = await commentRepo.GetPendingCommentsAsync();
        Assert.AreEqual(1, pendingBefore.Count);

        // 批准评论
        await commentRepo.UpdateApprovalStatusAsync(commentId, true);

        var pendingAfter = await commentRepo.GetPendingCommentsAsync();
        var approved = await commentRepo.GetApprovedCommentsAsync(postId);

        // Assert
        Assert.AreEqual(0, pendingAfter.Count);
        Assert.AreEqual(1, approved.Count);
    }

    [TestMethod]
    public async Task BlogService_PublishPost_ShouldWork()
    {
        // Arrange
        var service = new BlogService(_connection);

        // Act
        var postId = await service.PublishPostAsync(
            "Service Post",
            "Content via service",
            "Service Author",
            "service,test");

        var post = await service.ViewPostAsync(postId);

        // Assert
        Assert.IsNotNull(post);
        Assert.AreEqual("Service Post", post.Title);
        Assert.AreEqual(1, post.ViewCount); // ViewPostAsync increments
    }

    [TestMethod]
    public async Task BlogService_GetPostDetails_ShouldAggregateData()
    {
        // Arrange
        var service = new BlogService(_connection);
        var commentRepo = new BlogCommentRepository(_connection);
        var now = DateTime.UtcNow;

        var postId = await service.PublishPostAsync("Test", "Content", "Author");

        await commentRepo.CreateCommentAsync(postId, "User1", "Comment1", now, true);
        await commentRepo.CreateCommentAsync(postId, "User2", "Comment2", now, true);
        await commentRepo.CreateCommentAsync(postId, "User3", "Comment3", now, false); // 待审核

        // Act
        var details = await service.GetPostDetailsAsync(postId);

        // Assert
        Assert.AreEqual("Test", details["Title"]);
        Assert.AreEqual(2, details["ApprovedComments"]); // 只有2个已批准
        Assert.AreEqual(3, details["TotalComments"]); // 总共3个
    }

    [TestMethod]
    public async Task BlogService_DeletePostWithComments_ShouldCascade()
    {
        // Arrange
        var service = new BlogService(_connection);
        var commentRepo = new BlogCommentRepository(_connection);
        var postRepo = new BlogPostRepository(_connection);
        var now = DateTime.UtcNow;

        var postId = await service.PublishPostAsync("To Delete", "Content", "Author");
        await commentRepo.CreateCommentAsync(postId, "User", "Comment", now, true);

        // Act
        var deleted = await service.DeletePostWithCommentsAsync(postId);

        // Assert
        Assert.IsTrue(deleted);

        var post = await postRepo.GetPostByIdAsync(postId);
        var comments = await commentRepo.GetApprovedCommentsAsync(postId);

        Assert.IsNull(post);
        Assert.AreEqual(0, comments.Count);
    }

    // ==================== 任务管理系统测试 ====================

    [TestMethod]
    public async Task Task_CreateAndRetrieve_ShouldWork()
    {
        // Arrange
        var repo = new TaskRepository(_connection);
        var now = DateTime.UtcNow;
        var dueDate = now.AddDays(7);

        // Act
        var taskId = await repo.CreateTaskAsync(
            "Implement Feature",
            "Add new feature to the system",
            "developer@example.com",
            "Todo",
            3, // High priority
            dueDate,
            now,
            null);

        var task = await repo.GetTaskByIdAsync(taskId);

        // Assert
        Assert.IsNotNull(task);
        Assert.AreEqual("Implement Feature", task.Title);
        Assert.AreEqual(3, task.Priority);
        Assert.AreEqual("Todo", task.Status);
    }

    [TestMethod]
    public async Task Task_GetActiveTasksByUser_ShouldFilterAndSort()
    {
        // Arrange
        var repo = new TaskRepository(_connection);
        var now = DateTime.UtcNow;

        // 创建不同优先级和截止日期的任务
        await repo.CreateTaskAsync("Low Priority", null, "user@test.com", "Todo", 1, now.AddDays(5), now, null);
        await repo.CreateTaskAsync("High Priority", null, "user@test.com", "Todo", 3, now.AddDays(10), now, null);
        await repo.CreateTaskAsync("Medium Priority", null, "user@test.com", "InProgress", 2, now.AddDays(2), now, null);
        await repo.CreateTaskAsync("Done Task", null, "user@test.com", "Done", 3, now, now, now);
        await repo.CreateTaskAsync("Other User", null, "other@test.com", "Todo", 3, now, now, null);

        // Act
        var activeTasks = await repo.GetActiveTasksByUserAsync("user@test.com");

        // Assert
        Assert.AreEqual(3, activeTasks.Count); // 不包括Done和其他用户的任务
        Assert.AreEqual("High Priority", activeTasks[0].Title); // 优先级最高
        Assert.AreEqual("Medium Priority", activeTasks[1].Title);
        Assert.AreEqual("Low Priority", activeTasks[2].Title);
    }

    [TestMethod]
    public async Task Task_GetOverdueTasks_ShouldFindOverdue()
    {
        // Arrange
        var repo = new TaskRepository(_connection);
        var now = DateTime.UtcNow;

        await repo.CreateTaskAsync("Overdue 1", null, "user", "Todo", 2, now.AddDays(-2), now, null);
        await repo.CreateTaskAsync("Overdue 2", null, "user", "InProgress", 3, now.AddDays(-1), now, null);
        await repo.CreateTaskAsync("Future", null, "user", "Todo", 2, now.AddDays(5), now, null);
        await repo.CreateTaskAsync("Overdue Done", null, "user", "Done", 2, now.AddDays(-3), now, now);

        // Act
        var overdue = await repo.GetOverdueTasksAsync(now);

        // Assert
        Assert.AreEqual(2, overdue.Count); // 不包括Done的任务
        Assert.IsTrue(overdue.All(t => t.DueDate <= now && t.Status != "Done"));
    }

    [TestMethod]
    public async Task Task_UpdateStatus_ShouldWork()
    {
        // Arrange
        var repo = new TaskRepository(_connection);
        var now = DateTime.UtcNow;

        var taskId = await repo.CreateTaskAsync("Task", null, "user", "Todo", 2, null, now, null);

        // Act - 更新为InProgress
        await repo.UpdateTaskStatusAsync(taskId, "InProgress", null);
        var task1 = await repo.GetTaskByIdAsync(taskId);

        // Act - 完成任务
        var completedAt = DateTime.UtcNow;
        await repo.UpdateTaskStatusAsync(taskId, "Done", completedAt);
        var task2 = await repo.GetTaskByIdAsync(taskId);

        // Assert
        Assert.AreEqual("InProgress", task1.Status);
        Assert.IsNull(task1.CompletedAt);

        Assert.AreEqual("Done", task2.Status);
        Assert.IsNotNull(task2.CompletedAt);
    }

    [TestMethod]
    public async Task Task_UpdatePriority_ShouldWork()
    {
        // Arrange
        var repo = new TaskRepository(_connection);
        var now = DateTime.UtcNow;

        var taskId = await repo.CreateTaskAsync("Task", null, "user", "Todo", 1, null, now, null);

        // Act
        await repo.UpdateTaskPriorityAsync(taskId, 3); // 升级为高优先级
        var task = await repo.GetTaskByIdAsync(taskId);

        // Assert
        Assert.AreEqual(3, task.Priority);
    }

    [TestMethod]
    public async Task Task_CountCompletedByUser_ShouldAggregate()
    {
        // Arrange
        var repo = new TaskRepository(_connection);
        var now = DateTime.UtcNow;

        await repo.CreateTaskAsync("Task 1", null, "user1", "Done", 2, null, now, now);
        await repo.CreateTaskAsync("Task 2", null, "user1", "Done", 2, null, now, now);
        await repo.CreateTaskAsync("Task 3", null, "user1", "Todo", 2, null, now, null);
        await repo.CreateTaskAsync("Task 4", null, "user2", "Done", 2, null, now, now);

        // Act
        var count = await repo.CountCompletedTasksByUserAsync("user1");

        // Assert
        Assert.AreEqual(2, count);
    }

    [TestMethod]
    public async Task Task_GetStatistics_ShouldGroupByStatus()
    {
        // Arrange
        var repo = new TaskRepository(_connection);
        var now = DateTime.UtcNow;

        await repo.CreateTaskAsync("Task 1", null, "user", "Todo", 2, null, now, null);
        await repo.CreateTaskAsync("Task 2", null, "user", "Todo", 2, null, now, null);
        await repo.CreateTaskAsync("Task 3", null, "user", "InProgress", 2, null, now, null);
        await repo.CreateTaskAsync("Task 4", null, "user", "Done", 2, null, now, now);
        await repo.CreateTaskAsync("Task 5", null, "user", "Done", 2, null, now, now);
        await repo.CreateTaskAsync("Task 6", null, "user", "Done", 2, null, now, now);

        // Act
        var stats = await repo.GetTaskStatisticsAsync();

        // Assert
        Assert.AreEqual(3, stats.Count); // Todo, InProgress, Done

        var todoStat = stats.FirstOrDefault(s => s["status"].ToString() == "Todo");
        var inProgressStat = stats.FirstOrDefault(s => s["status"].ToString() == "InProgress");
        var doneStat = stats.FirstOrDefault(s => s["status"].ToString() == "Done");

        Assert.IsNotNull(todoStat);
        Assert.IsNotNull(inProgressStat);
        Assert.IsNotNull(doneStat);

        Assert.AreEqual(2L, todoStat["count"]);
        Assert.AreEqual(1L, inProgressStat["count"]);
        Assert.AreEqual(3L, doneStat["count"]);
    }

    // ==================== 跨系统集成测试 ====================

    [TestMethod]
    public async Task MultiSystem_BlogAndTask_ShouldCoexist()
    {
        // Arrange
        var blogService = new BlogService(_connection);
        var taskRepo = new TaskRepository(_connection);
        var now = DateTime.UtcNow;

        // Act - 创建博客文章
        var postId = await blogService.PublishPostAsync("New Feature", "We added tasks!", "Admin");

        // Act - 创建相关任务
        var taskId = await taskRepo.CreateTaskAsync(
            "Document new feature",
            $"Write documentation for blog post {postId}",
            "tech-writer",
            "Todo",
            2,
            now.AddDays(3),
            now,
            null);

        // Assert - 两个系统独立工作
        var post = await blogService.ViewPostAsync(postId);
        var task = await taskRepo.GetTaskByIdAsync(taskId);

        Assert.IsNotNull(post);
        Assert.IsNotNull(task);
        Assert.IsTrue(task.Description!.Contains(postId.ToString()));
    }

    [TestMethod]
    public async Task RealWorld_ComplexWorkflow_ShouldHandleAllOperations()
    {
        // 模拟真实的工作流程
        var blogService = new BlogService(_connection);
        var blogPostRepo = new BlogPostRepository(_connection);
        var commentRepo = new BlogCommentRepository(_connection);
        var taskRepo = new TaskRepository(_connection);
        var now = DateTime.UtcNow;

        // Step 1: 创建任务计划写博客
        var taskId = await taskRepo.CreateTaskAsync(
            "Write blog post about C# 12",
            "Cover new features including primary constructors",
            "author@blog.com",
            "Todo",
            3,
            now.AddDays(7),
            now,
            null);

        // Step 2: 开始写作
        await taskRepo.UpdateTaskStatusAsync(taskId, "InProgress", null);

        // Step 3: 发布博客
        var postId = await blogService.PublishPostAsync(
            "C# 12 Primary Constructors",
            "Primary constructors are a new feature...",
            "author@blog.com",
            "csharp,dotnet,programming");

        // Step 4: 完成任务
        await taskRepo.UpdateTaskStatusAsync(taskId, "Done", DateTime.UtcNow);

        // Step 5: 读者访问和评论
        await blogService.ViewPostAsync(postId);
        await blogService.ViewPostAsync(postId);
        await commentRepo.CreateCommentAsync(postId, "reader1", "Great article!", now, true);
        await commentRepo.CreateCommentAsync(postId, "reader2", "Very helpful!", now, true);

        // Step 6: 验证最终状态
        var post = await blogPostRepo.GetPostByIdAsync(postId);
        var task = await taskRepo.GetTaskByIdAsync(taskId);
        var comments = await commentRepo.GetApprovedCommentsAsync(postId);
        var completedTasks = await taskRepo.CountCompletedTasksByUserAsync("author@blog.com");

        // Assert
        Assert.IsNotNull(post);
        Assert.AreEqual(2, post.ViewCount);
        Assert.AreEqual(2, comments.Count);
        Assert.AreEqual("Done", task.Status);
        Assert.AreEqual(1, completedTasks);
    }
}

