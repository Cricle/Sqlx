using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;
using Sqlx.Tests.Integration;

namespace Sqlx.Tests.E2E.Scenarios
{
    #region Data Models

    public class Tenant
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class TenantData
    {
        public long Id { get; set; }
        public long TenantId { get; set; }
        public string DataType { get; set; } = string.Empty;
        public string DataValue { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    #endregion

    #region Repository Interfaces

    public interface ITenantRepository
    {
        [SqlTemplate("INSERT INTO tenants (name, code, is_active, created_at) VALUES (@name, @code, @isActive, @createdAt)")]
        [ReturnInsertedId]
        Task<long> CreateTenantAsync(string name, string code, bool isActive, DateTime createdAt);

        [SqlTemplate("SELECT {{columns}} FROM tenants WHERE id = @id")]
        Task<Tenant?> GetTenantByIdAsync(long id);

        [SqlTemplate("SELECT {{columns}} FROM tenants WHERE code = @code")]
        Task<Tenant?> GetTenantByCodeAsync(string code);

        [SqlTemplate("SELECT {{columns}} FROM tenants WHERE is_active = 1 ORDER BY name")]
        Task<List<Tenant>> GetActiveTenantsAsync();
    }

    public interface ITenantDataRepository
    {
        [SqlTemplate("INSERT INTO tenant_data (tenant_id, data_type, data_value, created_at) VALUES (@tenantId, @dataType, @dataValue, @createdAt)")]
        [ReturnInsertedId]
        Task<long> CreateDataAsync(long tenantId, string dataType, string dataValue, DateTime createdAt);

        [SqlTemplate("SELECT {{columns}} FROM tenant_data WHERE tenant_id = @tenantId ORDER BY created_at DESC")]
        Task<List<TenantData>> GetDataByTenantAsync(long tenantId);

        [SqlTemplate("SELECT {{columns}} FROM tenant_data WHERE tenant_id = @tenantId AND data_type = @dataType")]
        Task<List<TenantData>> GetDataByTenantAndTypeAsync(long tenantId, string dataType);

        [SqlTemplate("SELECT COUNT(*) FROM tenant_data WHERE tenant_id = @tenantId")]
        Task<int> CountDataByTenantAsync(long tenantId);

        [SqlTemplate("DELETE FROM tenant_data WHERE tenant_id = @tenantId")]
        Task<int> DeleteDataByTenantAsync(long tenantId);
    }

    #endregion

    #region Repository Implementations

    [SqlDefine(SqlDefineTypes.SQLite)]
    [TableName("tenants")]
    [RepositoryFor(typeof(ITenantRepository))]
    public partial class TenantRepository(DbConnection connection) : ITenantRepository { }

    [SqlDefine(SqlDefineTypes.SQLite)]
    [TableName("tenant_data")]
    [RepositoryFor(typeof(ITenantDataRepository))]
    public partial class TenantDataRepository(DbConnection connection) : ITenantDataRepository { }

    #endregion

    #region SQLite Implementation

    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("MultiTenant")]
    [TestCategory("SQLite")]
    public class MultiTenantScenarios_SQLite
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
                CREATE TABLE IF NOT EXISTS tenants (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL,
                    code TEXT NOT NULL UNIQUE,
                    is_active INTEGER NOT NULL DEFAULT 1,
                    created_at TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS tenant_data (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    tenant_id INTEGER NOT NULL,
                    data_type TEXT NOT NULL,
                    data_value TEXT NOT NULL,
                    created_at TEXT NOT NULL,
                    FOREIGN KEY (tenant_id) REFERENCES tenants(id)
                );

                CREATE INDEX IF NOT EXISTS idx_tenant_data_tenant_id ON tenant_data(tenant_id);
                CREATE INDEX IF NOT EXISTS idx_tenant_data_type ON tenant_data(data_type);
            ";

            cmd.ExecuteNonQuery();
        }

        [TestMethod]
        public async Task TenantDataIsolation_ShouldOnlyReturnDataForSpecifiedTenant()
        {
            // Arrange: Create three tenants
            var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
            var tenantRepo = new TenantRepository(connection);
            var tenantDataRepo = new TenantDataRepository(connection);

            var tenant1Id = await tenantRepo.CreateTenantAsync("Tenant One", "TENANT1", true, DateTime.UtcNow);
            var tenant2Id = await tenantRepo.CreateTenantAsync("Tenant Two", "TENANT2", true, DateTime.UtcNow);
            var tenant3Id = await tenantRepo.CreateTenantAsync("Tenant Three", "TENANT3", true, DateTime.UtcNow);

            // Create data for each tenant
            await tenantDataRepo.CreateDataAsync(tenant1Id, "config", "tenant1-config-1", DateTime.UtcNow);
            await tenantDataRepo.CreateDataAsync(tenant1Id, "config", "tenant1-config-2", DateTime.UtcNow);
            await tenantDataRepo.CreateDataAsync(tenant1Id, "setting", "tenant1-setting-1", DateTime.UtcNow);

            await tenantDataRepo.CreateDataAsync(tenant2Id, "config", "tenant2-config-1", DateTime.UtcNow);
            await tenantDataRepo.CreateDataAsync(tenant2Id, "setting", "tenant2-setting-1", DateTime.UtcNow);

            await tenantDataRepo.CreateDataAsync(tenant3Id, "config", "tenant3-config-1", DateTime.UtcNow);

            // Act: Query data for each tenant
            var tenant1Results = await tenantDataRepo.GetDataByTenantAsync(tenant1Id);
            var tenant2Results = await tenantDataRepo.GetDataByTenantAsync(tenant2Id);
            var tenant3Results = await tenantDataRepo.GetDataByTenantAsync(tenant3Id);

            // Assert: Each tenant should only see their own data
            Assert.AreEqual(3, tenant1Results.Count, "Tenant 1 should have 3 data records");
            Assert.IsTrue(tenant1Results.All(d => d.TenantId == tenant1Id), "All Tenant 1 data should belong to Tenant 1");
            Assert.IsTrue(tenant1Results.All(d => d.DataValue.StartsWith("tenant1-")), "All Tenant 1 data values should start with 'tenant1-'");

            Assert.AreEqual(2, tenant2Results.Count, "Tenant 2 should have 2 data records");
            Assert.IsTrue(tenant2Results.All(d => d.TenantId == tenant2Id), "All Tenant 2 data should belong to Tenant 2");
            Assert.IsTrue(tenant2Results.All(d => d.DataValue.StartsWith("tenant2-")), "All Tenant 2 data values should start with 'tenant2-'");

            Assert.AreEqual(1, tenant3Results.Count, "Tenant 3 should have 1 data record");
            Assert.IsTrue(tenant3Results.All(d => d.TenantId == tenant3Id), "All Tenant 3 data should belong to Tenant 3");
            Assert.IsTrue(tenant3Results.All(d => d.DataValue.StartsWith("tenant3-")), "All Tenant 3 data values should start with 'tenant3-'");
        }

        [TestMethod]
        public async Task TenantDataIsolation_WithTypeFilter_ShouldOnlyReturnMatchingData()
        {
            // Arrange: Create tenant with mixed data types
            var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
            var tenantRepo = new TenantRepository(connection);
            var tenantDataRepo = new TenantDataRepository(connection);

            var tenantId = await tenantRepo.CreateTenantAsync("Test Tenant", "TEST", true, DateTime.UtcNow);

            // Create data with different types
            await tenantDataRepo.CreateDataAsync(tenantId, "config", "config-value-1", DateTime.UtcNow);
            await tenantDataRepo.CreateDataAsync(tenantId, "config", "config-value-2", DateTime.UtcNow);
            await tenantDataRepo.CreateDataAsync(tenantId, "setting", "setting-value-1", DateTime.UtcNow);
            await tenantDataRepo.CreateDataAsync(tenantId, "preference", "preference-value-1", DateTime.UtcNow);

            // Act: Query by tenant and type
            var configData = await tenantDataRepo.GetDataByTenantAndTypeAsync(tenantId, "config");
            var settingData = await tenantDataRepo.GetDataByTenantAndTypeAsync(tenantId, "setting");
            var preferenceData = await tenantDataRepo.GetDataByTenantAndTypeAsync(tenantId, "preference");

            // Assert
            Assert.AreEqual(2, configData.Count, "Should have 2 config records");
            Assert.IsTrue(configData.All(d => d.DataType == "config"), "All records should be config type");

            Assert.AreEqual(1, settingData.Count, "Should have 1 setting record");
            Assert.AreEqual("setting", settingData[0].DataType, "Record should be setting type");

            Assert.AreEqual(1, preferenceData.Count, "Should have 1 preference record");
            Assert.AreEqual("preference", preferenceData[0].DataType, "Record should be preference type");
        }

        [TestMethod]
        public async Task TenantDeletion_ShouldOnlyDeleteSpecifiedTenantData()
        {
            // Arrange: Create two tenants with data
            var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
            var tenantRepo = new TenantRepository(connection);
            var tenantDataRepo = new TenantDataRepository(connection);

            var tenant1Id = await tenantRepo.CreateTenantAsync("Tenant One", "TENANT1", true, DateTime.UtcNow);
            var tenant2Id = await tenantRepo.CreateTenantAsync("Tenant Two", "TENANT2", true, DateTime.UtcNow);

            // Create data for both tenants
            await tenantDataRepo.CreateDataAsync(tenant1Id, "config", "tenant1-data", DateTime.UtcNow);
            await tenantDataRepo.CreateDataAsync(tenant2Id, "config", "tenant2-data", DateTime.UtcNow);

            // Act: Delete tenant1 data
            var deletedCount = await tenantDataRepo.DeleteDataByTenantAsync(tenant1Id);

            // Assert: Tenant1 data deleted, Tenant2 data remains
            Assert.AreEqual(1, deletedCount, "Should have deleted 1 record");

            var tenant1Count = await tenantDataRepo.CountDataByTenantAsync(tenant1Id);
            var tenant2Count = await tenantDataRepo.CountDataByTenantAsync(tenant2Id);

            Assert.AreEqual(0, tenant1Count, "Tenant 1 should have no data");
            Assert.AreEqual(1, tenant2Count, "Tenant 2 should still have 1 data record");
        }

        [TestMethod]
        public async Task ActiveTenantsQuery_ShouldOnlyReturnActiveTenants()
        {
            // Arrange: Create active and inactive tenants
            var connection = _fixture.GetConnection(SqlDefineTypes.SQLite);
            var tenantRepo = new TenantRepository(connection);

            await tenantRepo.CreateTenantAsync("Active Tenant 1", "ACTIVE1", true, DateTime.UtcNow);
            await tenantRepo.CreateTenantAsync("Active Tenant 2", "ACTIVE2", true, DateTime.UtcNow);
            await tenantRepo.CreateTenantAsync("Inactive Tenant", "INACTIVE", false, DateTime.UtcNow);

            // Act: Query active tenants
            var activeTenantsResult = await tenantRepo.GetActiveTenantsAsync();

            // Assert: Only active tenants returned
            Assert.AreEqual(2, activeTenantsResult.Count, "Should have 2 active tenants");
            Assert.IsTrue(activeTenantsResult.All(t => t.IsActive), "All returned tenants should be active");
            Assert.IsFalse(activeTenantsResult.Any(t => t.Code == "INACTIVE"), "Inactive tenant should not be returned");
        }
    }

    #endregion

    #region MySQL Implementation (Removed - using SQLite only for now)

    // MySQL implementation removed to simplify testing
    // Can be added back when Testcontainers is properly configured

    #endregion
}
