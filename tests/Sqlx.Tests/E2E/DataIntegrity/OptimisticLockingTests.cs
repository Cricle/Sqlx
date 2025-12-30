// -----------------------------------------------------------------------
// <copyright file="OptimisticLockingTests.cs" company="Cricle">
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

namespace Sqlx.Tests.E2E.DataIntegrity;

/// <summary>
/// E2E tests for optimistic locking with version control.
/// **Validates: Requirements 4.4**
/// </summary>

#region Data Models

public class VersionedDocument
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public long Version { get; set; }
    public DateTime UpdatedAt { get; set; }
}

#endregion

#region Repository Interfaces

public partial interface IVersionedDocumentRepository
{
    [SqlTemplate("INSERT INTO versioned_documents (title, content, version, updated_at) VALUES (@title, @content, 0, @updatedAt)")]
    [ReturnInsertedId]
    Task<long> CreateAsync(string title, string content, DateTime updatedAt);

    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    Task<VersionedDocument?> GetByIdAsync(long id);

    [SqlTemplate("UPDATE {{table}} SET title = @title, content = @content, version = version + 1, updated_at = @updatedAt WHERE id = @id AND version = @expectedVersion")]
    Task<int> UpdateAsync(long id, string title, string content, DateTime updatedAt, long expectedVersion);

    [SqlTemplate("SELECT {{columns}} FROM {{table}} ORDER BY id")]
    Task<List<VersionedDocument>> GetAllAsync();
}

#endregion

#region Repository Implementations

[SqlDefine(SqlDefineTypes.SQLite)]
[TableName("versioned_documents")]
[RepositoryFor(typeof(IVersionedDocumentRepository))]
public partial class VersionedDocumentRepository(DbConnection connection) : IVersionedDocumentRepository { }

#endregion

#region SQLite Implementation

[TestClass]
[TestCategory("E2E")]
[TestCategory("DataIntegrity")]
[TestCategory("OptimisticLocking")]
[TestCategory("SQLite")]
public class OptimisticLockingTests_SQLite
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
            CREATE TABLE IF NOT EXISTS versioned_documents (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                title TEXT NOT NULL,
                content TEXT NOT NULL,
                version INTEGER NOT NULL DEFAULT 0,
                updated_at DATETIME NOT NULL
            );

            CREATE INDEX IF NOT EXISTS idx_versioned_documents_version ON versioned_documents(version);
        ";

        cmd.ExecuteNonQuery();
    }

    [TestMethod]
    public async Task OptimisticLocking_CreateDocument_InitializesVersionToZero()
    {
        // Arrange: Create a new document
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        var docRepo = new VersionedDocumentRepository(connection);
        var now = DateTime.UtcNow;

        // Act: Create document
        var docId = await docRepo.CreateAsync("Initial Title", "Initial Content", now);

        // Assert: Version should be initialized to 0
        var doc = await docRepo.GetByIdAsync(docId);
        Assert.IsNotNull(doc, "Document should exist");
        Assert.AreEqual(0L, doc.Version, "New document should have version 0");
        Assert.AreEqual("Initial Title", doc.Title);
        Assert.AreEqual("Initial Content", doc.Content);
    }

    [TestMethod]
    public async Task OptimisticLocking_UpdateWithCorrectVersion_Succeeds()
    {
        // Arrange: Create document
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        var docRepo = new VersionedDocumentRepository(connection);
        var now = DateTime.UtcNow;

        var docId = await docRepo.CreateAsync("Original Title", "Original Content", now);
        var doc = await docRepo.GetByIdAsync(docId);
        Assert.IsNotNull(doc);
        Assert.AreEqual(0L, doc.Version);

        // Act: Update with correct version
        var updateTime = now.AddMinutes(5);
        var rowsAffected = await docRepo.UpdateAsync(docId, "Updated Title", "Updated Content", updateTime, doc.Version);

        // Assert: Update should succeed and version should increment
        Assert.AreEqual(1, rowsAffected, "Should update 1 row");
        
        var updatedDoc = await docRepo.GetByIdAsync(docId);
        Assert.IsNotNull(updatedDoc);
        Assert.AreEqual("Updated Title", updatedDoc.Title);
        Assert.AreEqual("Updated Content", updatedDoc.Content);
        Assert.AreEqual(1L, updatedDoc.Version, "Version should increment to 1");
    }

    [TestMethod]
    public async Task OptimisticLocking_UpdateWithWrongVersion_Fails()
    {
        // Arrange: Create document
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        var docRepo = new VersionedDocumentRepository(connection);
        var now = DateTime.UtcNow;

        var docId = await docRepo.CreateAsync("Original Title", "Original Content", now);
        var doc = await docRepo.GetByIdAsync(docId);
        Assert.IsNotNull(doc);

        // Act: Try to update with wrong version
        var updateTime = now.AddMinutes(5);
        var rowsAffected = await docRepo.UpdateAsync(docId, "Updated Title", "Updated Content", updateTime, 999L);

        // Assert: Update should fail (0 rows affected)
        Assert.AreEqual(0, rowsAffected, "Should not update any rows with wrong version");
        
        // Verify document is unchanged
        var unchangedDoc = await docRepo.GetByIdAsync(docId);
        Assert.IsNotNull(unchangedDoc);
        Assert.AreEqual("Original Title", unchangedDoc.Title, "Title should be unchanged");
        Assert.AreEqual("Original Content", unchangedDoc.Content, "Content should be unchanged");
        Assert.AreEqual(0L, unchangedDoc.Version, "Version should still be 0");
    }

    [TestMethod]
    public async Task OptimisticLocking_ConcurrentUpdates_DetectsConflict()
    {
        // Arrange: Create document and simulate two users reading it
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        var docRepo = new VersionedDocumentRepository(connection);
        var now = DateTime.UtcNow;

        var docId = await docRepo.CreateAsync("Shared Document", "Original Content", now);
        
        // User 1 reads the document
        var user1Doc = await docRepo.GetByIdAsync(docId);
        Assert.IsNotNull(user1Doc);
        
        // User 2 reads the document
        var user2Doc = await docRepo.GetByIdAsync(docId);
        Assert.IsNotNull(user2Doc);
        
        // Both have version 0
        Assert.AreEqual(0L, user1Doc.Version);
        Assert.AreEqual(0L, user2Doc.Version);

        // Act: User 1 updates first (should succeed)
        var user1UpdateTime = now.AddMinutes(1);
        var user1Result = await docRepo.UpdateAsync(docId, "User 1 Title", "User 1 Content", user1UpdateTime, user1Doc.Version);
        Assert.AreEqual(1, user1Result, "User 1 update should succeed");

        // User 2 tries to update with stale version (should fail)
        var user2UpdateTime = now.AddMinutes(2);
        var user2Result = await docRepo.UpdateAsync(docId, "User 2 Title", "User 2 Content", user2UpdateTime, user2Doc.Version);

        // Assert: User 2's update should fail due to version mismatch
        Assert.AreEqual(0, user2Result, "User 2 update should fail (concurrent modification detected)");
        
        // Verify final state reflects User 1's changes
        var finalDoc = await docRepo.GetByIdAsync(docId);
        Assert.IsNotNull(finalDoc);
        Assert.AreEqual("User 1 Title", finalDoc.Title, "Should have User 1's title");
        Assert.AreEqual("User 1 Content", finalDoc.Content, "Should have User 1's content");
        Assert.AreEqual(1L, finalDoc.Version, "Version should be 1 after User 1's update");
    }

    [TestMethod]
    public async Task OptimisticLocking_MultipleSequentialUpdates_IncrementsVersionCorrectly()
    {
        // Arrange: Create document
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        var docRepo = new VersionedDocumentRepository(connection);
        var now = DateTime.UtcNow;

        var docId = await docRepo.CreateAsync("Document", "Content v0", now);

        // Act: Perform multiple sequential updates
        for (int i = 0; i < 5; i++)
        {
            var doc = await docRepo.GetByIdAsync(docId);
            Assert.IsNotNull(doc);
            Assert.AreEqual((long)i, doc.Version, $"Version should be {i} before update {i + 1}");

            var updateTime = now.AddMinutes(i + 1);
            var result = await docRepo.UpdateAsync(docId, $"Title v{i + 1}", $"Content v{i + 1}", updateTime, doc.Version);
            Assert.AreEqual(1, result, $"Update {i + 1} should succeed");
        }

        // Assert: Final version should be 5
        var finalDoc = await docRepo.GetByIdAsync(docId);
        Assert.IsNotNull(finalDoc);
        Assert.AreEqual(5L, finalDoc.Version, "Version should be 5 after 5 updates");
        Assert.AreEqual("Title v5", finalDoc.Title);
        Assert.AreEqual("Content v5", finalDoc.Content);
    }

    [TestMethod]
    public async Task OptimisticLocking_RetryAfterConflict_Succeeds()
    {
        // Arrange: Create document
        var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
        var docRepo = new VersionedDocumentRepository(connection);
        var now = DateTime.UtcNow;

        var docId = await docRepo.CreateAsync("Document", "Original", now);
        
        // Simulate User 1 reading
        var user1Doc = await docRepo.GetByIdAsync(docId);
        Assert.IsNotNull(user1Doc);

        // Another user updates the document
        var otherUserDoc = await docRepo.GetByIdAsync(docId);
        Assert.IsNotNull(otherUserDoc);
        await docRepo.UpdateAsync(docId, "Other User Title", "Other User Content", now.AddMinutes(1), otherUserDoc.Version);

        // Act: User 1 tries to update with stale version (should fail)
        var firstAttempt = await docRepo.UpdateAsync(docId, "User 1 Title", "User 1 Content", now.AddMinutes(2), user1Doc.Version);
        Assert.AreEqual(0, firstAttempt, "First attempt should fail");

        // User 1 refreshes and retries with correct version
        var refreshedDoc = await docRepo.GetByIdAsync(docId);
        Assert.IsNotNull(refreshedDoc);
        Assert.AreEqual(1L, refreshedDoc.Version, "Version should be 1 after other user's update");

        var secondAttempt = await docRepo.UpdateAsync(docId, "User 1 Title", "User 1 Content", now.AddMinutes(3), refreshedDoc.Version);

        // Assert: Second attempt should succeed
        Assert.AreEqual(1, secondAttempt, "Second attempt with refreshed version should succeed");
        
        var finalDoc = await docRepo.GetByIdAsync(docId);
        Assert.IsNotNull(finalDoc);
        Assert.AreEqual("User 1 Title", finalDoc.Title);
        Assert.AreEqual("User 1 Content", finalDoc.Content);
        Assert.AreEqual(2L, finalDoc.Version, "Version should be 2 after successful retry");
    }
}

#endregion
