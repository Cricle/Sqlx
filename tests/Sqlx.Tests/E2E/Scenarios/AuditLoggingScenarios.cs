using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;
using Sqlx.Tests.Integration;

namespace Sqlx.Tests.E2E.Scenarios
{
    #region Data Models

    public class AuditedEntity
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
    }

    #endregion

    #region Repository Interfaces

    public partial interface IAuditedEntityRepository
    {
        [SqlTemplate("INSERT INTO audited_entities (name, description, created_at, created_by) VALUES (@name, @description, @createdAt, @createdBy)")]
        [ReturnInsertedId]
        Task<long> CreateAsync(string name, string description, DateTime createdAt, string createdBy);

        [SqlTemplate("SELECT {{columns}} FROM audited_entities WHERE id = @id")]
        Task<AuditedEntity?> GetByIdAsync(long id);

        [SqlTemplate("UPDATE audited_entities SET name = @name, description = @description, updated_at = @updatedAt, updated_by = @updatedBy WHERE id = @entityId")]
        Task<int> UpdateAsync(long entityId, string name, string description, DateTime updatedAt, string updatedBy);

        [SqlTemplate("SELECT {{columns}} FROM audited_entities WHERE created_by = @createdBy ORDER BY created_at DESC")]
        Task<List<AuditedEntity>> GetByCreatorAsync(string createdBy);

        [SqlTemplate("SELECT {{columns}} FROM audited_entities WHERE updated_by = @updatedBy ORDER BY updated_at DESC")]
        Task<List<AuditedEntity>> GetByUpdaterAsync(string updatedBy);

        [SqlTemplate("SELECT {{columns}} FROM audited_entities WHERE updated_at IS NOT NULL ORDER BY updated_at DESC")]
        Task<List<AuditedEntity>> GetModifiedEntitiesAsync();

        [SqlTemplate("SELECT {{columns}} FROM audited_entities WHERE updated_at IS NULL ORDER BY created_at DESC")]
        Task<List<AuditedEntity>> GetUnmodifiedEntitiesAsync();
    }

    #endregion

    #region Repository Implementations

    [SqlDefine(SqlDefineTypes.SQLite)]
    [TableName("audited_entities")]
    [RepositoryFor(typeof(IAuditedEntityRepository))]
    public partial class AuditedEntityRepository(DbConnection connection) : IAuditedEntityRepository { }

    #endregion

    #region SQLite Implementation

    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("AuditLogging")]
    [TestCategory("SQLite")]
    public class AuditLoggingScenarios_SQLite
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
                CREATE TABLE IF NOT EXISTS audited_entities (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL,
                    description TEXT NOT NULL,
                    created_at TEXT NOT NULL,
                    created_by TEXT NOT NULL,
                    updated_at TEXT,
                    updated_by TEXT
                );

                CREATE INDEX IF NOT EXISTS idx_audited_entities_created_by ON audited_entities(created_by);
                CREATE INDEX IF NOT EXISTS idx_audited_entities_updated_by ON audited_entities(updated_by);
                CREATE INDEX IF NOT EXISTS idx_audited_entities_created_at ON audited_entities(created_at);
                CREATE INDEX IF NOT EXISTS idx_audited_entities_updated_at ON audited_entities(updated_at);
            ";

            cmd.ExecuteNonQuery();
        }

        [TestMethod]
        public async Task CreateEntity_ShouldPopulateCreatedAtAndCreatedBy()
        {
            // Arrange
            var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
            var repo = new AuditedEntityRepository(connection);

            var now = DateTime.UtcNow;

            // Act
            var entityId = await repo.CreateAsync("Test Entity", "Test Description", now, "user@example.com");
            var retrieved = await repo.GetByIdAsync(entityId);

            // Assert
            Assert.IsNotNull(retrieved, "Entity should be retrieved");
            Assert.AreEqual("Test Entity", retrieved.Name);
            Assert.AreEqual("user@example.com", retrieved.CreatedBy, "CreatedBy should be set");
            Assert.IsTrue(Math.Abs((retrieved.CreatedAt - now).TotalSeconds) < 2, "CreatedAt should be close to the specified time");
            Assert.IsNull(retrieved.UpdatedAt, "UpdatedAt should be null for new entity");
            Assert.IsNull(retrieved.UpdatedBy, "UpdatedBy should be null for new entity");
        }

        [TestMethod]
        public async Task UpdateEntity_ShouldPopulateUpdatedAtAndUpdatedBy()
        {
            // Arrange: Create entity
            var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
            var repo = new AuditedEntityRepository(connection);

            var createTime = DateTime.UtcNow;

            var entityId = await repo.CreateAsync("Original Name", "Original Description", createTime, "creator@example.com");

            // Wait a moment to ensure different timestamps
            await Task.Delay(100);

            // Act: Update entity
            var updateTime = DateTime.UtcNow;
            var retrieved = await repo.GetByIdAsync(entityId);
            Assert.IsNotNull(retrieved);

            await repo.UpdateAsync(entityId, "Updated Name", "Updated Description", updateTime, "updater@example.com");

            // Retrieve again to verify
            var updated = await repo.GetByIdAsync(entityId);

            // Assert
            Assert.IsNotNull(updated, "Updated entity should be retrieved");
            Assert.AreEqual("Updated Name", updated.Name);
            Assert.AreEqual("Updated Description", updated.Description);
            Assert.AreEqual("creator@example.com", updated.CreatedBy, "CreatedBy should not change");
            Assert.IsTrue(Math.Abs((updated.CreatedAt - createTime).TotalSeconds) < 2, "CreatedAt should not change");
            Assert.IsNotNull(updated.UpdatedAt, "UpdatedAt should be set");
            Assert.AreEqual("updater@example.com", updated.UpdatedBy, "UpdatedBy should be set");
            Assert.IsTrue(Math.Abs((updated.UpdatedAt!.Value - updateTime).TotalSeconds) < 2, "UpdatedAt should be close to update time");
        }

        [TestMethod]
        public async Task GetByCreator_ShouldReturnAllEntitiesCreatedByUser()
        {
            // Arrange: Create entities by different users
            var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
            var repo = new AuditedEntityRepository(connection);

            var user1Entities = new[]
            {
                ("Entity 1", "Desc 1"),
                ("Entity 2", "Desc 2"),
                ("Entity 3", "Desc 3")
            };

            var user2Entities = new[]
            {
                ("Entity 4", "Desc 4"),
                ("Entity 5", "Desc 5")
            };

            foreach (var (name, desc) in user1Entities)
                await repo.CreateAsync(name, desc, DateTime.UtcNow, "user1@example.com");

            foreach (var (name, desc) in user2Entities)
                await repo.CreateAsync(name, desc, DateTime.UtcNow, "user2@example.com");

            // Act
            var user1Results = await repo.GetByCreatorAsync("user1@example.com");
            var user2Results = await repo.GetByCreatorAsync("user2@example.com");

            // Assert
            Assert.AreEqual(3, user1Results.Count, "User1 should have created 3 entities");
            Assert.IsTrue(user1Results.TrueForAll(e => e.CreatedBy == "user1@example.com"), "All entities should be created by user1");

            Assert.AreEqual(2, user2Results.Count, "User2 should have created 2 entities");
            Assert.IsTrue(user2Results.TrueForAll(e => e.CreatedBy == "user2@example.com"), "All entities should be created by user2");
        }

        [TestMethod]
        public async Task GetByUpdater_ShouldReturnAllEntitiesUpdatedByUser()
        {
            // Arrange: Create entities and update them by different users
            var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
            var repo = new AuditedEntityRepository(connection);

            var entity1Id = await repo.CreateAsync("Entity 1", "Desc 1", DateTime.UtcNow, "creator@example.com");
            var entity2Id = await repo.CreateAsync("Entity 2", "Desc 2", DateTime.UtcNow, "creator@example.com");
            var entity3Id = await repo.CreateAsync("Entity 3", "Desc 3", DateTime.UtcNow, "creator@example.com");

            // Update entities by different users
            var entity1 = await repo.GetByIdAsync(entity1Id);
            Assert.IsNotNull(entity1);
            await repo.UpdateAsync(entityId: entity1.Id, name: "Updated 1", description: entity1.Description, updatedAt: DateTime.UtcNow, updatedBy: "updater1@example.com");

            var entity2 = await repo.GetByIdAsync(entity2Id);
            Assert.IsNotNull(entity2);
            await repo.UpdateAsync(entityId: entity2.Id, name: "Updated 2", description: entity2.Description, updatedAt: DateTime.UtcNow, updatedBy: "updater1@example.com");

            var entity3 = await repo.GetByIdAsync(entity3Id);
            Assert.IsNotNull(entity3);
            await repo.UpdateAsync(entityId: entity3.Id, name: "Updated 3", description: entity3.Description, updatedAt: DateTime.UtcNow, updatedBy: "updater2@example.com");

            // Act
            var updater1Results = await repo.GetByUpdaterAsync("updater1@example.com");
            var updater2Results = await repo.GetByUpdaterAsync("updater2@example.com");

            // Assert
            Assert.AreEqual(2, updater1Results.Count, "Updater1 should have updated 2 entities");
            Assert.IsTrue(updater1Results.TrueForAll(e => e.UpdatedBy == "updater1@example.com"), "All entities should be updated by updater1");

            Assert.AreEqual(1, updater2Results.Count, "Updater2 should have updated 1 entity");
            Assert.AreEqual("updater2@example.com", updater2Results[0].UpdatedBy, "Entity should be updated by updater2");
        }

        [TestMethod]
        public async Task GetModifiedEntities_ShouldOnlyReturnUpdatedEntities()
        {
            // Arrange: Create some entities and update only some of them
            var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
            var repo = new AuditedEntityRepository(connection);

            var entity1Id = await repo.CreateAsync("Entity 1", "Desc 1", DateTime.UtcNow, "user@example.com");
            var entity2Id = await repo.CreateAsync("Entity 2", "Desc 2", DateTime.UtcNow, "user@example.com");
            var entity3Id = await repo.CreateAsync("Entity 3", "Desc 3", DateTime.UtcNow, "user@example.com");

            // Update only entity1 and entity2
            var entity1 = await repo.GetByIdAsync(entity1Id);
            Assert.IsNotNull(entity1);
            await repo.UpdateAsync(entity1.Id, "Updated 1", entity1.Description, DateTime.UtcNow, "updater@example.com");

            var entity2 = await repo.GetByIdAsync(entity2Id);
            Assert.IsNotNull(entity2);
            await repo.UpdateAsync(entity2.Id, "Updated 2", entity2.Description, DateTime.UtcNow, "updater@example.com");

            // Act
            var modifiedEntities = await repo.GetModifiedEntitiesAsync();
            var unmodifiedEntities = await repo.GetUnmodifiedEntitiesAsync();

            // Assert
            Assert.AreEqual(2, modifiedEntities.Count, "Should have 2 modified entities");
            Assert.IsTrue(modifiedEntities.TrueForAll(e => e.UpdatedAt != null), "All modified entities should have UpdatedAt set");

            Assert.AreEqual(1, unmodifiedEntities.Count, "Should have 1 unmodified entity");
            Assert.IsNull(unmodifiedEntities[0].UpdatedAt, "Unmodified entity should not have UpdatedAt set");
            Assert.AreEqual("Entity 3", unmodifiedEntities[0].Name, "Unmodified entity should be Entity 3");
        }

        [TestMethod]
        public async Task AuditTrail_ShouldMaintainCompleteHistory()
        {
            // Arrange: Create entity
            var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
            var repo = new AuditedEntityRepository(connection);

            var createTime = DateTime.UtcNow;

            var entityId = await repo.CreateAsync("Original", "Original Description", createTime, "creator@example.com");

            // Act: Perform multiple updates
            await Task.Delay(100);

            var retrieved1 = await repo.GetByIdAsync(entityId);
            Assert.IsNotNull(retrieved1);
            await repo.UpdateAsync(retrieved1.Id, "First Update", retrieved1.Description, DateTime.UtcNow, "updater1@example.com");

            await Task.Delay(100);

            var retrieved2 = await repo.GetByIdAsync(entityId);
            Assert.IsNotNull(retrieved2);
            await repo.UpdateAsync(retrieved2.Id, "Second Update", retrieved2.Description, DateTime.UtcNow, "updater2@example.com");

            // Retrieve final state
            var final = await repo.GetByIdAsync(entityId);

            // Assert: Verify audit trail integrity
            Assert.IsNotNull(final);
            Assert.AreEqual("Second Update", final.Name, "Name should reflect latest update");
            Assert.AreEqual("creator@example.com", final.CreatedBy, "CreatedBy should never change");
            Assert.IsTrue(Math.Abs((final.CreatedAt - createTime).TotalSeconds) < 2, "CreatedAt should never change");
            Assert.AreEqual("updater2@example.com", final.UpdatedBy, "UpdatedBy should reflect latest updater");
            Assert.IsNotNull(final.UpdatedAt, "UpdatedAt should be set");
            Assert.IsTrue(final.UpdatedAt > final.CreatedAt, "UpdatedAt should be after CreatedAt");
        }
    }

    #endregion
}
