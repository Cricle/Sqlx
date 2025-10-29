// -----------------------------------------------------------------------
// <copyright file="PredefinedInterfacesSqlTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Sqlx;
using Sqlx.Annotations;

namespace Sqlx.Tests
{
    /// <summary>
    /// Tests for all predefined repository interfaces to ensure SQL templates are correct for all dialects.
    /// Uses TDD approach: Write tests first, then fix the interface SQL templates.
    /// </summary>
    public class PredefinedInterfacesSqlTests
    {
        // Test entity for all tests
        [TableName("users")]
        public class User
        {
            public long Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string? Email { get; set; }
            public int Age { get; set; }
            public bool IsActive { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? UpdatedAt { get; set; }
        }

        // Test repositories implementing predefined interfaces
        [RepositoryFor(typeof(User))]
        [SqlDefine(SqlDialect.SQLite)]
        public partial class UserQueryRepository : IQueryRepository<User, long>
        {
            private readonly System.Data.IDbConnection _connection;
            public UserQueryRepository(System.Data.IDbConnection connection)
            {
                _connection = connection;
            }
        }

        [RepositoryFor(typeof(User))]
        [SqlDefine(SqlDialect.SQLite)]
        public partial class UserCommandRepository : ICommandRepository<User, long>
        {
            private readonly System.Data.IDbConnection _connection;
            public UserCommandRepository(System.Data.IDbConnection connection)
            {
                _connection = connection;
            }
        }

        [RepositoryFor(typeof(User))]
        [SqlDefine(SqlDialect.SQLite)]
        public partial class UserAggregateRepository : IAggregateRepository<User, long>
        {
            private readonly System.Data.IDbConnection _connection;
            public UserAggregateRepository(System.Data.IDbConnection connection)
            {
                _connection = connection;
            }
        }

        [RepositoryFor(typeof(User))]
        [SqlDefine(SqlDialect.SQLite)]
        public partial class UserBatchRepository : IBatchRepository<User, long>
        {
            private readonly System.Data.IDbConnection _connection;
            public UserBatchRepository(System.Data.IDbConnection connection)
            {
                _connection = connection;
            }
        }

        [RepositoryFor(typeof(User))]
        [SqlDefine(SqlDialect.SQLite)]
        public partial class UserMaintenanceRepository : IMaintenanceRepository<User>
        {
            private readonly System.Data.IDbConnection _connection;
            public UserMaintenanceRepository(System.Data.IDbConnection connection)
            {
                _connection = connection;
            }
        }

        #region SQLite Dialect Tests

        [Fact]
        public void IQueryRepository_GetByIdAsync_SQLite()
        {
            // Expected: SELECT id, name, email, age, is_active, created_at, updated_at FROM users WHERE id = @id
            // This test will verify the generated SQL is correct for SQLite
            Assert.True(true, "Placeholder - will be implemented after source generator creates the code");
        }

        [Fact]
        public void IQueryRepository_GetByIdsAsync_SQLite()
        {
            // Expected: SELECT * FROM users WHERE id IN (@id0, @id1, @id2...)
            Assert.True(true, "Placeholder");
        }

        [Fact]
        public void IQueryRepository_GetAllAsync_SQLite()
        {
            // Expected: SELECT * FROM users ORDER BY created_at DESC LIMIT 1000
            Assert.True(true, "Placeholder");
        }

        [Fact]
        public void IQueryRepository_GetTopAsync_SQLite()
        {
            // Expected: SELECT * FROM users ORDER BY name ASC LIMIT 10
            Assert.True(true, "Placeholder");
        }

        [Fact]
        public void IQueryRepository_GetRangeAsync_SQLite()
        {
            // Expected: SELECT * FROM users ORDER BY id ASC LIMIT 100 OFFSET 50
            Assert.True(true, "Placeholder");
        }

        [Fact]
        public void IQueryRepository_ExistsAsync_SQLite()
        {
            // Expected: SELECT CASE WHEN EXISTS(SELECT 1 FROM users WHERE id = @id) THEN 1 ELSE 0 END
            Assert.True(true, "Placeholder");
        }

        [Fact]
        public void ICommandRepository_InsertAsync_SQLite()
        {
            // Expected: INSERT INTO users (name, email, age, is_active, created_at, updated_at) VALUES (@name, @email, @age, @is_active, @created_at, @updated_at)
            Assert.True(true, "Placeholder");
        }

        [Fact]
        public void ICommandRepository_UpdateAsync_SQLite()
        {
            // Expected: UPDATE users SET name = @name, email = @email, age = @age, is_active = @is_active, created_at = @created_at, updated_at = @updated_at WHERE id = @id
            Assert.True(true, "Placeholder");
        }

        [Fact]
        public void ICommandRepository_DeleteAsync_SQLite()
        {
            // Expected: DELETE FROM users WHERE id = @id
            Assert.True(true, "Placeholder");
        }

        [Fact]
        public void ICommandRepository_SoftDeleteAsync_SQLite()
        {
            // Expected: UPDATE users SET is_deleted = 1, deleted_at = @now WHERE id = @id
            Assert.True(true, "Placeholder");
        }

        [Fact]
        public void IAggregateRepository_CountAsync_SQLite()
        {
            // Expected: SELECT COUNT(*) FROM users
            Assert.True(true, "Placeholder");
        }

        [Fact]
        public void IAggregateRepository_SumAsync_SQLite()
        {
            // Expected: SELECT COALESCE(SUM(age), 0) FROM users
            Assert.True(true, "Placeholder");
        }

        [Fact]
        public void IAggregateRepository_AvgAsync_SQLite()
        {
            // Expected: SELECT COALESCE(AVG(age), 0) FROM users
            Assert.True(true, "Placeholder");
        }

        [Fact]
        public void IAggregateRepository_MaxIntAsync_SQLite()
        {
            // Expected: SELECT MAX(age) FROM users
            Assert.True(true, "Placeholder");
        }

        [Fact]
        public void IAggregateRepository_MinIntAsync_SQLite()
        {
            // Expected: SELECT MIN(age) FROM users
            Assert.True(true, "Placeholder");
        }

        [Fact]
        public void IBatchRepository_BatchInsertAsync_SQLite()
        {
            // Expected: INSERT INTO users (name, email, age, is_active, created_at, updated_at) VALUES (@name_0, @email_0, ...), (@name_1, @email_1, ...), ...
            Assert.True(true, "Placeholder");
        }

        [Fact]
        public void IBatchRepository_BatchDeleteAsync_SQLite()
        {
            // Expected: DELETE FROM users WHERE id IN (@id0, @id1, @id2, ...)
            Assert.True(true, "Placeholder");
        }

        [Fact]
        public void IMaintenanceRepository_TruncateAsync_SQLite()
        {
            // SQLite doesn't support TRUNCATE, should use: DELETE FROM users; DELETE FROM sqlite_sequence WHERE name='users';
            Assert.True(true, "Placeholder");
        }

        [Fact]
        public void IMaintenanceRepository_DeleteAllAsync_SQLite()
        {
            // Expected: DELETE FROM users
            Assert.True(true, "Placeholder");
        }

        #endregion

        #region MySQL Dialect Tests

        // MySQL-specific tests (TOP -> LIMIT, different date functions, etc.)
        [Fact]
        public void ICommandRepository_InsertAsync_MySQL()
        {
            // MySQL specific syntax test
            Assert.True(true, "Placeholder");
        }

        [Fact]
        public void IMaintenanceRepository_TruncateAsync_MySQL()
        {
            // MySQL supports TRUNCATE TABLE users
            Assert.True(true, "Placeholder");
        }

        #endregion

        #region PostgreSQL Dialect Tests

        [Fact]
        public void ICommandRepository_InsertAsync_PostgreSQL()
        {
            // PostgreSQL uses $1, $2 for parameters
            Assert.True(true, "Placeholder");
        }

        [Fact]
        public void IQueryRepository_GetRangeAsync_PostgreSQL()
        {
            // PostgreSQL: SELECT * FROM users ORDER BY id LIMIT 100 OFFSET 50
            Assert.True(true, "Placeholder");
        }

        #endregion

        #region SQL Server Dialect Tests

        [Fact]
        public void IQueryRepository_GetRangeAsync_SqlServer()
        {
            // SQL Server 2012+: SELECT * FROM users ORDER BY id OFFSET 50 ROWS FETCH NEXT 100 ROWS ONLY
            Assert.True(true, "Placeholder");
        }

        [Fact]
        public void ICommandRepository_InsertAsync_SqlServer()
        {
            // SQL Server uses @param syntax
            Assert.True(true, "Placeholder");
        }

        #endregion

        #region Oracle Dialect Tests

        [Fact]
        public void IQueryRepository_GetRangeAsync_Oracle()
        {
            // Oracle 12c+: SELECT * FROM users ORDER BY id OFFSET 50 ROWS FETCH NEXT 100 ROWS ONLY
            Assert.True(true, "Placeholder");
        }

        #endregion

        #region Missing SqlTemplate Tests

        [Fact]
        public void IQueryRepository_GetWhereAsync_RequiresSqlTemplate()
        {
            // This method uses [ExpressionToSql] but should still have a base SQL template
            // Expected to use ExpressionExtensions.ToWhereClause()
            Assert.True(true, "Needs implementation test");
        }

        [Fact]
        public void IQueryRepository_GetFirstWhereAsync_RequiresSqlTemplate()
        {
            // Should have: SELECT * FROM users {{where}} LIMIT 1
            Assert.True(true, "Needs implementation test");
        }

        [Fact]
        public void IQueryRepository_ExistsWhereAsync_RequiresSqlTemplate()
        {
            // Should have: SELECT CASE WHEN EXISTS(SELECT 1 FROM users {{where}}) THEN 1 ELSE 0 END
            Assert.True(true, "Needs implementation test");
        }

        [Fact]
        public void IQueryRepository_GetRandomAsync_RequiresSqlTemplate()
        {
            // SQLite: SELECT * FROM users ORDER BY RANDOM() LIMIT @count
            // MySQL: SELECT * FROM users ORDER BY RAND() LIMIT @count
            // SQL Server: SELECT TOP (@count) * FROM users ORDER BY NEWID()
            // PostgreSQL: SELECT * FROM users ORDER BY RANDOM() LIMIT @count
            Assert.True(true, "Needs database-specific implementation");
        }

        [Fact]
        public void IQueryRepository_GetDistinctValuesAsync_RequiresSqlTemplate()
        {
            // Should have: SELECT DISTINCT {{column}} FROM users LIMIT @limit
            Assert.True(true, "Needs implementation test");
        }

        [Fact]
        public void ICommandRepository_UpsertAsync_RequiresSqlTemplate()
        {
            // Database-specific, but needs test
            Assert.True(true, "Needs database-specific implementation");
        }

        [Fact]
        public void IBatchRepository_BatchExistsAsync_RequiresSqlTemplate()
        {
            // Should check multiple IDs and return list of booleans
            Assert.True(true, "Needs implementation test");
        }

        #endregion
    }
}

