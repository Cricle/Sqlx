// -----------------------------------------------------------------------
// <copyright file="SoftDeleteTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;
using Sqlx.Tests.Integration;

namespace Sqlx.Tests.E2E.DataIntegrity;

/// <summary>
/// E2E tests for soft delete functionality.
/// **Validates: Requirements 4.5**
/// </summary>

#region Data Models

public class SoftDeletableTask
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}

#endregion

#region Repository Interfaces

public partial interface ISoftDeletableTaskRepository
{
    [SqlTemplate("INSERT INTO soft_deletable_tasks (title, description, is_deleted, deleted_at) VALUES (@title, @description, 0, NULL)")]
    [ReturnInsertedId]
    Task<long> CreateAsync(string title, string description);

    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    Task<SoftDeletableTask?> GetByIdAsync(long id);

    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE is_deleted = 0 ORDER BY id")]
    Task<List<SoftDeletableTask>> GetActiveAsync();

    [SqlTemplate("SELECT {{columns}} FROM {{table}} ORDER BY id")]
    Task<List<SoftDeletableTask>> GetAllIncludingDeletedAsync();

    [SqlTemplate("UPDATE {{table}} SET is_deleted = 1, deleted_at = @deletedAt WHERE id = @id")]
    Task<int> SoftDeleteAsync(long id, DateTime deletedAt);

    [SqlTemplate("UPDATE {{table}} SET is_deleted = 0, deleted_at = NULL WHERE id = @id")]
    Task<int> RestoreAsync(long id);

    [SqlTemplate("SELECT COUNT(*) FROM {{table}} WHERE is_deleted = 0")]
    Task<long> CountActiveAsync();

    [SqlTemplate("SELECT COUNT(*) FROM {{table}} WHERE is_deleted = 1")]
    Task<long> CountDeletedAsync();
}

#endregion

#region Repository Implementations

[SqlDefine(SqlDefineTypes.SQLite)]
[TableName("soft_deletable_tasks")]
[RepositoryFor(typeof(ISoftDeletableTaskRepository))]
public partial class SoftDeletableTaskRepository(DbConnection connection) : ISoftDeletableTaskRepository { }

#endregion

#region SQLite Implementation

[TestClass]
[TestCategory("E2E")]
[TestCategory("DataIntegrity")]
[TestCategory("SoftDelete")]
[TestCategory("SQLite")]
public class SoftDeleteTests_SQLite
{
    private DatabaseFixture _fixture = null!;

    [TestInitialize]
    public void Initialize()
    {
        _fixture = new DatabaseFixture();
        CreateTables();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _fixture?.Dispose();
    }

    private void CreateTables()
    {
        var conn = _fixture.GetConnection(SqlDefineTypes.SQLite);
        using var cmd = conn.CreateCommand();

        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS soft_deletable_tasks (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                title TEXT NOT NULL,
                description TEXT NOT NULL,
                is_deleted INTEGER NOT NULL DEFAULT 0,
                deleted_at DATETIME NULL
            );

            CREATE INDEX IF NOT EXISTS idx_soft_deletable_tasks_is_deleted ON soft_deletable_tasks(is_deleted);
        ";

        cmd.ExecuteNonQuery();
    }

    [TestMethod]
    public async Task SoftDelete_CreateTask_IsNotDeleted()
    {
        // Arrange & Act: Create a new task
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        var taskRepo = new SoftDeletableTaskRepository(connection);

        var taskId = await taskRepo.CreateAsync("New Task", "Task Description");

        // Assert: Task should not be deleted
        var task = await taskRepo.GetByIdAsync(taskId);
        Assert.IsNotNull(task, "Task should exist");
        Assert.IsFalse(task.IsDeleted, "New task should not be deleted");
        Assert.IsNull(task.DeletedAt, "DeletedAt should be null for new task");
    }

    [TestMethod]
    public async Task SoftDelete_DeleteTask_MarksAsDeleted()
    {
        // Arrange: Create task
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        var taskRepo = new SoftDeletableTaskRepository(connection);
        var now = DateTime.UtcNow;

        var taskId = await taskRepo.CreateAsync("Task to Delete", "Description");

        // Act: Soft delete the task
        var deleteTime = now.AddMinutes(5);
        var result = await taskRepo.SoftDeleteAsync(taskId, deleteTime);

        // Assert: Task should be marked as deleted
        Assert.AreEqual(1, result, "Should update 1 row");
        
        var task = await taskRepo.GetByIdAsync(taskId);
        Assert.IsNotNull(task);
        Assert.IsTrue(task.IsDeleted, "Task should be marked as deleted");
        Assert.IsNotNull(task.DeletedAt, "DeletedAt should be set");
    }

    [TestMethod]
    public async Task SoftDelete_GetActive_ExcludesDeletedTasks()
    {
        // Arrange: Create multiple tasks and delete some
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        var taskRepo = new SoftDeletableTaskRepository(connection);
        var now = DateTime.UtcNow;

        var task1Id = await taskRepo.CreateAsync("Active Task 1", "Description 1");
        var task2Id = await taskRepo.CreateAsync("Task to Delete", "Description 2");
        var task3Id = await taskRepo.CreateAsync("Active Task 2", "Description 3");

        // Delete task 2
        await taskRepo.SoftDeleteAsync(task2Id, now);

        // Act: Get active tasks
        var activeTasks = await taskRepo.GetActiveAsync();

        // Assert: Should only return non-deleted tasks
        Assert.AreEqual(2, activeTasks.Count, "Should have 2 active tasks");
        Assert.IsTrue(activeTasks.All(t => !t.IsDeleted), "All returned tasks should not be deleted");
        Assert.IsTrue(activeTasks.Any(t => t.Id == task1Id), "Should include task 1");
        Assert.IsTrue(activeTasks.Any(t => t.Id == task3Id), "Should include task 3");
        Assert.IsFalse(activeTasks.Any(t => t.Id == task2Id), "Should not include deleted task 2");
    }

    [TestMethod]
    public async Task SoftDelete_GetAllIncludingDeleted_ReturnsAllTasks()
    {
        // Arrange: Create multiple tasks and delete some
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        var taskRepo = new SoftDeletableTaskRepository(connection);
        var now = DateTime.UtcNow;

        var task1Id = await taskRepo.CreateAsync("Active Task", "Description 1");
        var task2Id = await taskRepo.CreateAsync("Deleted Task", "Description 2");

        await taskRepo.SoftDeleteAsync(task2Id, now);

        // Act: Get all tasks including deleted
        var allTasks = await taskRepo.GetAllIncludingDeletedAsync();

        // Assert: Should return both active and deleted tasks
        Assert.AreEqual(2, allTasks.Count, "Should have 2 total tasks");
        
        var activeTask = allTasks.FirstOrDefault(t => t.Id == task1Id);
        var deletedTask = allTasks.FirstOrDefault(t => t.Id == task2Id);
        
        Assert.IsNotNull(activeTask);
        Assert.IsFalse(activeTask.IsDeleted, "Task 1 should not be deleted");
        
        Assert.IsNotNull(deletedTask);
        Assert.IsTrue(deletedTask.IsDeleted, "Task 2 should be deleted");
    }

    [TestMethod]
    public async Task SoftDelete_RestoreTask_UnmarksAsDeleted()
    {
        // Arrange: Create and delete a task
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        var taskRepo = new SoftDeletableTaskRepository(connection);
        var now = DateTime.UtcNow;

        var taskId = await taskRepo.CreateAsync("Task to Restore", "Description");
        await taskRepo.SoftDeleteAsync(taskId, now);

        // Verify it's deleted
        var deletedTask = await taskRepo.GetByIdAsync(taskId);
        Assert.IsNotNull(deletedTask);
        Assert.IsTrue(deletedTask.IsDeleted);

        // Act: Restore the task
        var result = await taskRepo.RestoreAsync(taskId);

        // Assert: Task should be restored
        Assert.AreEqual(1, result, "Should update 1 row");
        
        var restoredTask = await taskRepo.GetByIdAsync(taskId);
        Assert.IsNotNull(restoredTask);
        Assert.IsFalse(restoredTask.IsDeleted, "Task should not be deleted after restore");
        Assert.IsNull(restoredTask.DeletedAt, "DeletedAt should be null after restore");
    }

    [TestMethod]
    public async Task SoftDelete_CountActive_ReturnsCorrectCount()
    {
        // Arrange: Create multiple tasks and delete some
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        var taskRepo = new SoftDeletableTaskRepository(connection);
        var now = DateTime.UtcNow;

        await taskRepo.CreateAsync("Active 1", "Desc 1");
        var task2Id = await taskRepo.CreateAsync("To Delete", "Desc 2");
        await taskRepo.CreateAsync("Active 2", "Desc 3");
        var task4Id = await taskRepo.CreateAsync("To Delete 2", "Desc 4");

        await taskRepo.SoftDeleteAsync(task2Id, now);
        await taskRepo.SoftDeleteAsync(task4Id, now);

        // Act: Count active tasks
        var activeCount = await taskRepo.CountActiveAsync();
        var deletedCount = await taskRepo.CountDeletedAsync();

        // Assert: Counts should be correct
        Assert.AreEqual(2L, activeCount, "Should have 2 active tasks");
        Assert.AreEqual(2L, deletedCount, "Should have 2 deleted tasks");
    }

    [TestMethod]
    public async Task SoftDelete_MultipleDeleteAndRestore_WorksCorrectly()
    {
        // Arrange: Create task
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        var taskRepo = new SoftDeletableTaskRepository(connection);
        var now = DateTime.UtcNow;

        var taskId = await taskRepo.CreateAsync("Task", "Description");

        // Act & Assert: Delete and restore multiple times
        for (int i = 0; i < 3; i++)
        {
            // Delete
            await taskRepo.SoftDeleteAsync(taskId, now.AddMinutes(i * 2));
            var deletedTask = await taskRepo.GetByIdAsync(taskId);
            Assert.IsNotNull(deletedTask);
            Assert.IsTrue(deletedTask.IsDeleted, $"Task should be deleted in iteration {i}");

            // Restore
            await taskRepo.RestoreAsync(taskId);
            var restoredTask = await taskRepo.GetByIdAsync(taskId);
            Assert.IsNotNull(restoredTask);
            Assert.IsFalse(restoredTask.IsDeleted, $"Task should be restored in iteration {i}");
        }

        // Final state should be restored
        var finalTask = await taskRepo.GetByIdAsync(taskId);
        Assert.IsNotNull(finalTask);
        Assert.IsFalse(finalTask.IsDeleted, "Final state should be not deleted");
    }

    [TestMethod]
    public async Task SoftDelete_FilteringWorkflow_CompleteScenario()
    {
        // Arrange: Create a complete workflow scenario
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        var taskRepo = new SoftDeletableTaskRepository(connection);
        var now = DateTime.UtcNow;

        // Create 5 tasks
        var task1Id = await taskRepo.CreateAsync("Task 1", "Active");
        var task2Id = await taskRepo.CreateAsync("Task 2", "To be deleted");
        var task3Id = await taskRepo.CreateAsync("Task 3", "Active");
        var task4Id = await taskRepo.CreateAsync("Task 4", "To be deleted then restored");
        var task5Id = await taskRepo.CreateAsync("Task 5", "Active");

        // Initial state: all active
        var initialActive = await taskRepo.GetActiveAsync();
        Assert.AreEqual(5, initialActive.Count, "Initially all 5 tasks should be active");

        // Delete tasks 2 and 4
        await taskRepo.SoftDeleteAsync(task2Id, now);
        await taskRepo.SoftDeleteAsync(task4Id, now.AddMinutes(1));

        // Check active tasks (should be 3)
        var afterDelete = await taskRepo.GetActiveAsync();
        Assert.AreEqual(3, afterDelete.Count, "Should have 3 active tasks after deleting 2");
        Assert.IsTrue(afterDelete.All(t => new[] { task1Id, task3Id, task5Id }.Contains(t.Id)),
            "Active tasks should be 1, 3, and 5");

        // Restore task 4
        await taskRepo.RestoreAsync(task4Id);

        // Check active tasks (should be 4)
        var afterRestore = await taskRepo.GetActiveAsync();
        Assert.AreEqual(4, afterRestore.Count, "Should have 4 active tasks after restoring 1");
        Assert.IsTrue(afterRestore.All(t => new[] { task1Id, task3Id, task4Id, task5Id }.Contains(t.Id)),
            "Active tasks should be 1, 3, 4, and 5");

        // Verify counts
        var activeCount = await taskRepo.CountActiveAsync();
        var deletedCount = await taskRepo.CountDeletedAsync();
        Assert.AreEqual(4L, activeCount, "Should have 4 active tasks");
        Assert.AreEqual(1L, deletedCount, "Should have 1 deleted task");

        // Verify all tasks including deleted
        var allTasks = await taskRepo.GetAllIncludingDeletedAsync();
        Assert.AreEqual(5, allTasks.Count, "Should have 5 total tasks");
        Assert.AreEqual(4, allTasks.Count(t => !t.IsDeleted), "4 should be active");
        Assert.AreEqual(1, allTasks.Count(t => t.IsDeleted), "1 should be deleted");
    }
}

#endregion
