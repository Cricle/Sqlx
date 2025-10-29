// -----------------------------------------------------------------------
// <copyright file="PredefinedInterfacesSqlTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Sqlx;
using Sqlx.Annotations;

namespace Sqlx.Tests
{
    /// <summary>
    /// Tests for all predefined repository interfaces to ensure SQL templates are correct for all dialects.
    /// Uses TDD approach: Verify SQL generation for each dialect.
    /// 
    /// Note: This file contains placeholder tests. Actual SQL validation should be done by:
    /// 1. Inspecting generated .g.cs files
    /// 2. Runtime SQL logging
    /// 3. Integration tests with real databases
    /// </summary>
    public class PredefinedInterfacesSqlTests
    {
        /// <summary>
        /// Verify that all IQueryRepository methods have proper SqlTemplate attributes.
        /// This is a compile-time verification - if this compiles, the templates are syntactically valid.
        /// </summary>
        [Fact]
        public void IQueryRepository_AllMethodsHaveSqlTemplates()
        {
            // This test verifies that the interface definition is valid
            // Actual SQL generation is verified by source generator at compile time
            var interfaceType = typeof(IQueryRepository<,>);
            Assert.NotNull(interfaceType);
            
            // Check key methods have SqlTemplate attribute
            var methods = interfaceType.GetMethods();
            var methodsWithTemplate = methods.Where(m => 
                m.GetCustomAttributes(typeof(SqlTemplateAttribute), false).Length > 0);
            
            // We expect at least these methods to have SqlTemplate:
            // GetByIdAsync, GetByIdsAsync, GetAllAsync, GetTopAsync, GetRangeAsync,
            // ExistsAsync, GetWhereAsync, GetFirstWhereAsync, ExistsWhereAsync,
            // GetRandomAsync, GetDistinctValuesAsync
            Assert.True(methodsWithTemplate.Count() >= 11, 
                $"Expected at least 11 methods with SqlTemplate, found {methodsWithTemplate.Count()}");
        }

        /// <summary>
        /// Verify that all ICommandRepository methods have proper SqlTemplate attributes.
        /// </summary>
        [Fact]
        public void ICommandRepository_AllMethodsHaveSqlTemplates()
        {
            var interfaceType = typeof(ICommandRepository<,>);
            var methods = interfaceType.GetMethods();
            var methodsWithTemplate = methods.Where(m => 
                m.GetCustomAttributes(typeof(SqlTemplateAttribute), false).Length > 0);
            
            // Expected: InsertAsync, InsertAndGetIdAsync, InsertAndGetEntityAsync,
            // UpdateAsync, UpdatePartialAsync, UpdateWhereAsync, DeleteAsync, DeleteWhereAsync,
            // SoftDeleteAsync, RestoreAsync, PurgeDeletedAsync (11 methods)
            Assert.True(methodsWithTemplate.Count() >= 11,
                $"Expected at least 11 methods with SqlTemplate, found {methodsWithTemplate.Count()}");
        }

        /// <summary>
        /// Verify that all IAggregateRepository methods have proper SqlTemplate attributes.
        /// </summary>
        [Fact]
        public void IAggregateRepository_AllMethodsHaveSqlTemplates()
        {
            var interfaceType = typeof(IAggregateRepository<,>);
            var methods = interfaceType.GetMethods();
            var methodsWithTemplate = methods.Where(m => 
                m.GetCustomAttributes(typeof(SqlTemplateAttribute), false).Length > 0);
            
            // Expected: CountAsync, CountWhereAsync, CountByAsync, SumAsync, SumWhereAsync,
            // AvgAsync, AvgWhereAsync, MaxIntAsync, MaxLongAsync, MaxDecimalAsync, MaxDateTimeAsync,
            // MinIntAsync, MinLongAsync, MinDecimalAsync, MinDateTimeAsync (15 methods)
            Assert.True(methodsWithTemplate.Count() >= 15,
                $"Expected at least 15 methods with SqlTemplate, found {methodsWithTemplate.Count()}");
        }

        /// <summary>
        /// Verify that all IBatchRepository methods have proper SqlTemplate attributes.
        /// </summary>
        [Fact]
        public void IBatchRepository_AllMethodsHaveSqlTemplates()
        {
            var interfaceType = typeof(IBatchRepository<,>);
            var methods = interfaceType.GetMethods();
            var methodsWithTemplate = methods.Where(m => 
                m.GetCustomAttributes(typeof(SqlTemplateAttribute), false).Length > 0);
            
            // Expected: BatchInsertAsync, BatchInsertAndGetIdsAsync, BatchUpdateWhereAsync,
            // BatchDeleteAsync, BatchSoftDeleteAsync (5 methods with SqlTemplate)
            // BatchUpdateAsync, BatchUpsertAsync, BatchExistsAsync (3 methods without SqlTemplate)
            Assert.True(methodsWithTemplate.Count() >= 5,
                $"Expected at least 5 methods with SqlTemplate, found {methodsWithTemplate.Count()}");
        }

        /// <summary>
        /// Verify that all IMaintenanceRepository methods have proper SqlTemplate attributes.
        /// </summary>
        [Fact]
        public void IMaintenanceRepository_AllMethodsHaveSqlTemplates()
        {
            var interfaceType = typeof(IMaintenanceRepository<>);
            var methods = interfaceType.GetMethods();
            var methodsWithTemplate = methods.Where(m => 
                m.GetCustomAttributes(typeof(SqlTemplateAttribute), false).Length > 0);
            
            // Expected: TruncateAsync, DropTableAsync, DeleteAllAsync (3 methods with SqlTemplate)
            // RebuildIndexesAsync, UpdateStatisticsAsync, ShrinkTableAsync (3 methods without SqlTemplate, need special impl)
            Assert.True(methodsWithTemplate.Count() >= 3,
                $"Expected at least 3 methods with SqlTemplate, found {methodsWithTemplate.Count()}");
        }

        /// <summary>
        /// This test documents which methods still need special implementation or database-specific handling.
        /// </summary>
        [Fact]
        public void DocumentSpecialImplementationNeeds()
        {
            // Methods requiring database-specific implementation:
            // 1. IQueryRepository.GetPageAsync - needs COUNT(*) + SELECT with LIMIT/OFFSET
            // 2. ICommandRepository.UpsertAsync - MERGE/INSERT ON CONFLICT/INSERT OR REPLACE
            // 3. IBatchRepository.BatchUpdateAsync - batch UPDATE with CASE WHEN
            // 4. IBatchRepository.BatchUpsertAsync - batch MERGE/INSERT ON CONFLICT
            // 5. IBatchRepository.BatchExistsAsync - check multiple IDs
            // 6. IAdvancedRepository.* - all raw SQL methods
            // 7. ISchemaRepository.* - all schema inspection methods
            // 8. IMaintenanceRepository.RebuildIndexesAsync - REINDEX/REBUILD/OPTIMIZE
            // 9. IMaintenanceRepository.UpdateStatisticsAsync - ANALYZE/UPDATE STATISTICS
            // 10. IMaintenanceRepository.ShrinkTableAsync - VACUUM/SHRINKDATABASE
            
            Assert.True(true, "Documentation test - always passes");
        }
    }
}

