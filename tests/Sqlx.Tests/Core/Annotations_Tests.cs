using Xunit;
using Sqlx.Annotations;
using System;

namespace Sqlx.Tests.Core
{
    /// <summary>
    /// Tests for annotation attributes
    /// </summary>
    public class Annotations_Tests
    {
        #region SqlxAttribute Tests

        [Fact]
        public void SqlxAttribute_DefaultConstructor_InitializesProperties()
        {
            var attr = new SqlxAttribute();
            
            Assert.Equal(string.Empty, attr.StoredProcedureName);
            Assert.Equal(string.Empty, attr.Sql);
            Assert.False(attr.AcceptsSqlTemplate);
            Assert.Equal("template", attr.SqlTemplateParameterName);
            Assert.False(attr.UseCompileTimeTemplate);
            Assert.Null(attr.TemplateCacheKey);
        }

        [Fact]
        public void SqlxAttribute_WithStoredProcedureName_SetsProperty()
        {
            var attr = new SqlxAttribute("sp_GetUsers");
            
            Assert.Equal("sp_GetUsers", attr.StoredProcedureName);
            Assert.Equal(string.Empty, attr.Sql);
        }

        [Fact]
        public void SqlxAttribute_WithNullStoredProcedureName_SetsEmptyString()
        {
            var attr = new SqlxAttribute(null!);
            
            Assert.Equal(string.Empty, attr.StoredProcedureName);
        }

        [Fact]
        public void SqlxAttribute_SetSql_UpdatesProperty()
        {
            var attr = new SqlxAttribute { Sql = "SELECT * FROM Users" };
            
            Assert.Equal("SELECT * FROM Users", attr.Sql);
        }

        [Fact]
        public void SqlxAttribute_SetAcceptsSqlTemplate_UpdatesProperty()
        {
            var attr = new SqlxAttribute { AcceptsSqlTemplate = true };
            
            Assert.True(attr.AcceptsSqlTemplate);
        }

        [Fact]
        public void SqlxAttribute_SetSqlTemplateParameterName_UpdatesProperty()
        {
            var attr = new SqlxAttribute { SqlTemplateParameterName = "customTemplate" };
            
            Assert.Equal("customTemplate", attr.SqlTemplateParameterName);
        }

        [Fact]
        public void SqlxAttribute_SetUseCompileTimeTemplate_UpdatesProperty()
        {
            var attr = new SqlxAttribute { UseCompileTimeTemplate = true };
            
            Assert.True(attr.UseCompileTimeTemplate);
        }

        [Fact]
        public void SqlxAttribute_SetTemplateCacheKey_UpdatesProperty()
        {
            var attr = new SqlxAttribute { TemplateCacheKey = "cache_key_123" };
            
            Assert.Equal("cache_key_123", attr.TemplateCacheKey);
        }

        #endregion

        #region RepositoryForAttribute Tests

        [Fact]
        public void RepositoryForAttribute_WithServiceType_SetsProperty()
        {
            var attr = new RepositoryForAttribute(typeof(IDisposable));
            
            Assert.Equal(typeof(IDisposable), attr.ServiceType);
            Assert.Equal(SqlDefineTypes.SQLite, attr.Dialect);
            Assert.Null(attr.TableName);
        }

        [Fact]
        public void RepositoryForAttribute_WithNullServiceType_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new RepositoryForAttribute(null!));
        }

        [Fact]
        public void RepositoryForAttribute_SetDialect_UpdatesProperty()
        {
            var attr = new RepositoryForAttribute(typeof(IDisposable)) 
            { 
                Dialect = SqlDefineTypes.MySql 
            };
            
            Assert.Equal(SqlDefineTypes.MySql, attr.Dialect);
        }

        [Fact]
        public void RepositoryForAttribute_SetTableName_UpdatesProperty()
        {
            var attr = new RepositoryForAttribute(typeof(IDisposable)) 
            { 
                TableName = "custom_table" 
            };
            
            Assert.Equal("custom_table", attr.TableName);
        }

        [Fact]
        public void RepositoryForAttributeGeneric_ServiceType_ReturnsCorrectType()
        {
            var attr = new RepositoryForAttribute<IDisposable>();
            
            Assert.Equal(typeof(IDisposable), attr.ServiceType);
            Assert.Equal(SqlDefineTypes.SQLite, attr.Dialect);
            Assert.Null(attr.TableName);
        }

        [Fact]
        public void RepositoryForAttributeGeneric_SetDialect_UpdatesProperty()
        {
            var attr = new RepositoryForAttribute<IDisposable> 
            { 
                Dialect = SqlDefineTypes.PostgreSql 
            };
            
            Assert.Equal(SqlDefineTypes.PostgreSql, attr.Dialect);
        }

        [Fact]
        public void RepositoryForAttributeGeneric_SetTableName_UpdatesProperty()
        {
            var attr = new RepositoryForAttribute<IDisposable> 
            { 
                TableName = "users" 
            };
            
            Assert.Equal("users", attr.TableName);
        }

        #endregion

        #region SoftDeleteAttribute Tests

        [Fact]
        public void SoftDeleteAttribute_DefaultConstructor_InitializesProperties()
        {
            var attr = new SoftDeleteAttribute();
            
            Assert.Equal("IsDeleted", attr.FlagColumn);
            Assert.Null(attr.TimestampColumn);
            Assert.Null(attr.DeletedByColumn);
        }

        [Fact]
        public void SoftDeleteAttribute_SetFlagColumn_UpdatesProperty()
        {
            var attr = new SoftDeleteAttribute { FlagColumn = "Deleted" };
            
            Assert.Equal("Deleted", attr.FlagColumn);
        }

        [Fact]
        public void SoftDeleteAttribute_SetTimestampColumn_UpdatesProperty()
        {
            var attr = new SoftDeleteAttribute { TimestampColumn = "DeletedAt" };
            
            Assert.Equal("DeletedAt", attr.TimestampColumn);
        }

        [Fact]
        public void SoftDeleteAttribute_SetDeletedByColumn_UpdatesProperty()
        {
            var attr = new SoftDeleteAttribute { DeletedByColumn = "DeletedBy" };
            
            Assert.Equal("DeletedBy", attr.DeletedByColumn);
        }

        [Fact]
        public void SoftDeleteAttribute_SetAllProperties_UpdatesAll()
        {
            var attr = new SoftDeleteAttribute 
            { 
                FlagColumn = "IsRemoved",
                TimestampColumn = "RemovedAt",
                DeletedByColumn = "RemovedBy"
            };
            
            Assert.Equal("IsRemoved", attr.FlagColumn);
            Assert.Equal("RemovedAt", attr.TimestampColumn);
            Assert.Equal("RemovedBy", attr.DeletedByColumn);
        }

        #endregion

        #region BatchOperationAttribute Tests

        [Fact]
        public void BatchOperationAttribute_DefaultConstructor_InitializesProperties()
        {
            var attr = new BatchOperationAttribute();
            
            Assert.Equal(1000, attr.MaxBatchSize);
            Assert.Equal(2100, attr.MaxParametersPerBatch);
        }

        [Fact]
        public void BatchOperationAttribute_SetMaxBatchSize_UpdatesProperty()
        {
            var attr = new BatchOperationAttribute { MaxBatchSize = 500 };
            
            Assert.Equal(500, attr.MaxBatchSize);
        }

        [Fact]
        public void BatchOperationAttribute_SetMaxParametersPerBatch_UpdatesProperty()
        {
            var attr = new BatchOperationAttribute { MaxParametersPerBatch = 5000 };
            
            Assert.Equal(5000, attr.MaxParametersPerBatch);
        }

        [Fact]
        public void BatchOperationAttribute_SetBothProperties_UpdatesBoth()
        {
            var attr = new BatchOperationAttribute 
            { 
                MaxBatchSize = 2000,
                MaxParametersPerBatch = 10000
            };
            
            Assert.Equal(2000, attr.MaxBatchSize);
            Assert.Equal(10000, attr.MaxParametersPerBatch);
        }

        #endregion
    }
}
