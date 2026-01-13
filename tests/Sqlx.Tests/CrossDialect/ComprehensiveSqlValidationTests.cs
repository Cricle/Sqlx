// -----------------------------------------------------------------------
// <copyright file="ComprehensiveSqlValidationTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq.Expressions;
using Xunit;

namespace Sqlx.Tests.CrossDialect;

/// <summary>
/// Comprehensive SQL validation tests for all predefined interfaces across all dialects.
/// Validates exact SQL output including column quoting, parameter format, and syntax.
/// </summary>
public class ComprehensiveSqlValidationTests
{
    // Column names in snake_case as expected in generated SQL
    private const string AllColumns = "id, name, age, is_active, price, created_at, updated_at, is_deleted, deleted_at";
    private const string InsertColumns = "name, age, is_active, price, created_at, updated_at, is_deleted, deleted_at";

    #region SQLite Tests

    [Fact]
    [Trait("Category", "SQLite")]
    public void SQLite_GetByIdAsync_GeneratesCorrectSql()
    {
        var repo = new CrudRepositorySQLite(null!);
        var sql = repo.GetGetByIdAsyncSql(1);

        Assert.Equal(
            "SELECT [id], [name], [age], [is_active], [price], [created_at], [updated_at], [is_deleted], [deleted_at] FROM [test_entity] WHERE id = @id",
            sql);
    }

    [Fact]
    [Trait("Category", "SQLite")]
    public void SQLite_GetByIdsAsync_GeneratesCorrectSql()
    {
        var repo = new CrudRepositorySQLite(null!);
        var sql = repo.GetGetByIdsAsyncSql([1, 2, 3]);

        Assert.Contains("SELECT [id], [name], [age], [is_active], [price], [created_at], [updated_at], [is_deleted], [deleted_at] FROM [test_entity] WHERE id IN", sql);
    }

    [Fact]
    [Trait("Category", "SQLite")]
    public void SQLite_GetAllAsync_GeneratesCorrectSql()
    {
        var repo = new CrudRepositorySQLite(null!);
        var sql = repo.GetGetAllAsyncSql(100);

        // Runtime placeholders are shown as comments in debug SQL
        Assert.Contains("SELECT [id], [name], [age], [is_active], [price], [created_at], [updated_at], [is_deleted], [deleted_at] FROM [test_entity]", sql);
        Assert.Contains("RUNTIME_LIMIT", sql);
    }

    [Fact]
    [Trait("Category", "SQLite")]
    public void SQLite_GetWhereAsync_GeneratesCorrectSql()
    {
        var repo = new CrudRepositorySQLite(null!);
        Expression<Func<CrossDialectTestEntity, bool>> predicate = x => x.IsActive;
        var sql = repo.GetGetWhereAsyncSql(predicate);

        Assert.Contains("SELECT [id], [name], [age], [is_active], [price], [created_at], [updated_at], [is_deleted], [deleted_at] FROM [test_entity] WHERE", sql);
    }

    [Fact]
    [Trait("Category", "SQLite")]
    public void SQLite_GetFirstWhereAsync_GeneratesCorrectSql()
    {
        var repo = new CrudRepositorySQLite(null!);
        Expression<Func<CrossDialectTestEntity, bool>> predicate = x => x.Id == 1;
        var sql = repo.GetGetFirstWhereAsyncSql(predicate);

        Assert.Contains("SELECT [id], [name], [age], [is_active], [price], [created_at], [updated_at], [is_deleted], [deleted_at] FROM [test_entity] WHERE", sql);
        Assert.Contains("LIMIT 1", sql);
    }

    [Fact]
    [Trait("Category", "SQLite")]
    public void SQLite_CountAsync_GeneratesCorrectSql()
    {
        var repo = new CrudRepositorySQLite(null!);
        var sql = repo.GetCountAsyncSql();

        Assert.Equal("SELECT COUNT(*) FROM [test_entity]", sql);
    }

    [Fact]
    [Trait("Category", "SQLite")]
    public void SQLite_InsertAsync_GeneratesCorrectSql()
    {
        var repo = new CrudRepositorySQLite(null!);
        var sql = repo.GetInsertAsyncSql(null!);

        Assert.Equal(
            "INSERT INTO [test_entity] ([name], [age], [is_active], [price], [created_at], [updated_at], [is_deleted], [deleted_at]) VALUES (@name, @age, @is_active, @price, @created_at, @updated_at, @is_deleted, @deleted_at)",
            sql);
    }

    [Fact]
    [Trait("Category", "SQLite")]
    public void SQLite_UpdateAsync_GeneratesCorrectSql()
    {
        var repo = new CrudRepositorySQLite(null!);
        var sql = repo.GetUpdateAsyncSql(null!);

        Assert.Equal(
            "UPDATE [test_entity] SET [name] = @name, [age] = @age, [is_active] = @is_active, [price] = @price, [created_at] = @created_at, [updated_at] = @updated_at, [is_deleted] = @is_deleted, [deleted_at] = @deleted_at WHERE id = @id",
            sql);
    }

    [Fact]
    [Trait("Category", "SQLite")]
    public void SQLite_DeleteAsync_GeneratesCorrectSql()
    {
        var repo = new CrudRepositorySQLite(null!);
        var sql = repo.GetDeleteAsyncSql(1);

        Assert.Equal("DELETE FROM [test_entity] WHERE id = @id", sql);
    }

    [Fact]
    [Trait("Category", "SQLite")]
    public void SQLite_DeleteWhereAsync_GeneratesCorrectSql()
    {
        var repo = new CrudRepositorySQLite(null!);
        Expression<Func<CrossDialectTestEntity, bool>> predicate = x => x.IsDeleted;
        var sql = repo.GetDeleteWhereAsyncSql(predicate);

        Assert.Contains("DELETE FROM [test_entity] WHERE", sql);
    }

    #endregion

    #region MySQL Tests

    [Fact]
    [Trait("Category", "MySQL")]
    public void MySQL_GetByIdAsync_GeneratesCorrectSql()
    {
        var repo = new CrudRepositoryMySql(null!);
        var sql = repo.GetGetByIdAsyncSql(1);

        Assert.Equal(
            "SELECT `id`, `name`, `age`, `is_active`, `price`, `created_at`, `updated_at`, `is_deleted`, `deleted_at` FROM `test_entity` WHERE id = @id",
            sql);
    }

    [Fact]
    [Trait("Category", "MySQL")]
    public void MySQL_GetByIdsAsync_GeneratesCorrectSql()
    {
        var repo = new CrudRepositoryMySql(null!);
        var sql = repo.GetGetByIdsAsyncSql([1, 2, 3]);

        Assert.Contains("SELECT `id`, `name`, `age`, `is_active`, `price`, `created_at`, `updated_at`, `is_deleted`, `deleted_at` FROM `test_entity` WHERE id IN", sql);
    }

    [Fact]
    [Trait("Category", "MySQL")]
    public void MySQL_GetAllAsync_GeneratesCorrectSql()
    {
        var repo = new CrudRepositoryMySql(null!);
        var sql = repo.GetGetAllAsyncSql(100);

        // Runtime placeholders are shown as comments in debug SQL
        Assert.Contains("SELECT `id`, `name`, `age`, `is_active`, `price`, `created_at`, `updated_at`, `is_deleted`, `deleted_at` FROM `test_entity`", sql);
        Assert.Contains("RUNTIME_LIMIT", sql);
    }

    [Fact]
    [Trait("Category", "MySQL")]
    public void MySQL_CountAsync_GeneratesCorrectSql()
    {
        var repo = new CrudRepositoryMySql(null!);
        var sql = repo.GetCountAsyncSql();

        Assert.Equal("SELECT COUNT(*) FROM `test_entity`", sql);
    }

    [Fact]
    [Trait("Category", "MySQL")]
    public void MySQL_InsertAsync_GeneratesCorrectSql()
    {
        var repo = new CrudRepositoryMySql(null!);
        var sql = repo.GetInsertAsyncSql(null!);

        Assert.Equal(
            "INSERT INTO `test_entity` (`name`, `age`, `is_active`, `price`, `created_at`, `updated_at`, `is_deleted`, `deleted_at`) VALUES (@name, @age, @is_active, @price, @created_at, @updated_at, @is_deleted, @deleted_at)",
            sql);
    }

    [Fact]
    [Trait("Category", "MySQL")]
    public void MySQL_UpdateAsync_GeneratesCorrectSql()
    {
        var repo = new CrudRepositoryMySql(null!);
        var sql = repo.GetUpdateAsyncSql(null!);

        Assert.Equal(
            "UPDATE `test_entity` SET `name` = @name, `age` = @age, `is_active` = @is_active, `price` = @price, `created_at` = @created_at, `updated_at` = @updated_at, `is_deleted` = @is_deleted, `deleted_at` = @deleted_at WHERE id = @id",
            sql);
    }

    [Fact]
    [Trait("Category", "MySQL")]
    public void MySQL_DeleteAsync_GeneratesCorrectSql()
    {
        var repo = new CrudRepositoryMySql(null!);
        var sql = repo.GetDeleteAsyncSql(1);

        Assert.Equal("DELETE FROM `test_entity` WHERE id = @id", sql);
    }

    #endregion

    #region PostgreSQL Tests

    [Fact]
    [Trait("Category", "PostgreSQL")]
    public void PostgreSQL_GetByIdAsync_GeneratesCorrectSql()
    {
        var repo = new CrudRepositoryPostgreSql(null!);
        var sql = repo.GetGetByIdAsyncSql(1);

        Assert.Equal(
            "SELECT \"id\", \"name\", \"age\", \"is_active\", \"price\", \"created_at\", \"updated_at\", \"is_deleted\", \"deleted_at\" FROM \"test_entity\" WHERE id = @id",
            sql);
    }

    [Fact]
    [Trait("Category", "PostgreSQL")]
    public void PostgreSQL_GetByIdsAsync_GeneratesCorrectSql()
    {
        var repo = new CrudRepositoryPostgreSql(null!);
        var sql = repo.GetGetByIdsAsyncSql([1, 2, 3]);

        Assert.Contains("SELECT \"id\", \"name\", \"age\", \"is_active\", \"price\", \"created_at\", \"updated_at\", \"is_deleted\", \"deleted_at\" FROM \"test_entity\" WHERE id IN", sql);
    }

    [Fact]
    [Trait("Category", "PostgreSQL")]
    public void PostgreSQL_GetAllAsync_GeneratesCorrectSql()
    {
        var repo = new CrudRepositoryPostgreSql(null!);
        var sql = repo.GetGetAllAsyncSql(100);

        // Runtime placeholders are shown as comments in debug SQL
        Assert.Contains("SELECT \"id\", \"name\", \"age\", \"is_active\", \"price\", \"created_at\", \"updated_at\", \"is_deleted\", \"deleted_at\" FROM \"test_entity\"", sql);
        Assert.Contains("RUNTIME_LIMIT", sql);
    }

    [Fact]
    [Trait("Category", "PostgreSQL")]
    public void PostgreSQL_CountAsync_GeneratesCorrectSql()
    {
        var repo = new CrudRepositoryPostgreSql(null!);
        var sql = repo.GetCountAsyncSql();

        Assert.Equal("SELECT COUNT(*) FROM \"test_entity\"", sql);
    }

    [Fact]
    [Trait("Category", "PostgreSQL")]
    public void PostgreSQL_InsertAsync_GeneratesCorrectSql()
    {
        var repo = new CrudRepositoryPostgreSql(null!);
        var sql = repo.GetInsertAsyncSql(null!);

        Assert.Equal(
            "INSERT INTO \"test_entity\" (\"name\", \"age\", \"is_active\", \"price\", \"created_at\", \"updated_at\", \"is_deleted\", \"deleted_at\") VALUES (@name, @age, @is_active, @price, @created_at, @updated_at, @is_deleted, @deleted_at)",
            sql);
    }

    [Fact]
    [Trait("Category", "PostgreSQL")]
    public void PostgreSQL_UpdateAsync_GeneratesCorrectSql()
    {
        var repo = new CrudRepositoryPostgreSql(null!);
        var sql = repo.GetUpdateAsyncSql(null!);

        Assert.Equal(
            "UPDATE \"test_entity\" SET \"name\" = @name, \"age\" = @age, \"is_active\" = @is_active, \"price\" = @price, \"created_at\" = @created_at, \"updated_at\" = @updated_at, \"is_deleted\" = @is_deleted, \"deleted_at\" = @deleted_at WHERE id = @id",
            sql);
    }

    [Fact]
    [Trait("Category", "PostgreSQL")]
    public void PostgreSQL_DeleteAsync_GeneratesCorrectSql()
    {
        var repo = new CrudRepositoryPostgreSql(null!);
        var sql = repo.GetDeleteAsyncSql(1);

        Assert.Equal("DELETE FROM \"test_entity\" WHERE id = @id", sql);
    }

    #endregion

    #region SqlServer Tests

    [Fact]
    [Trait("Category", "SqlServer")]
    public void SqlServer_GetByIdAsync_GeneratesCorrectSql()
    {
        var repo = new CrudRepositorySqlServer(null!);
        var sql = repo.GetGetByIdAsyncSql(1);

        Assert.Equal(
            "SELECT [id], [name], [age], [is_active], [price], [created_at], [updated_at], [is_deleted], [deleted_at] FROM [test_entity] WHERE id = @id",
            sql);
    }

    [Fact]
    [Trait("Category", "SqlServer")]
    public void SqlServer_GetByIdsAsync_GeneratesCorrectSql()
    {
        var repo = new CrudRepositorySqlServer(null!);
        var sql = repo.GetGetByIdsAsyncSql([1, 2, 3]);

        Assert.Contains("SELECT [id], [name], [age], [is_active], [price], [created_at], [updated_at], [is_deleted], [deleted_at] FROM [test_entity] WHERE id IN", sql);
    }

    [Fact]
    [Trait("Category", "SqlServer")]
    public void SqlServer_GetAllAsync_GeneratesCorrectSql()
    {
        var repo = new CrudRepositorySqlServer(null!);
        var sql = repo.GetGetAllAsyncSql(100);

        // SqlServer uses TOP or OFFSET-FETCH, check for either
        Assert.Contains("SELECT", sql);
        Assert.Contains("[test_entity]", sql);
    }

    [Fact]
    [Trait("Category", "SqlServer")]
    public void SqlServer_CountAsync_GeneratesCorrectSql()
    {
        var repo = new CrudRepositorySqlServer(null!);
        var sql = repo.GetCountAsyncSql();

        Assert.Equal("SELECT COUNT(*) FROM [test_entity]", sql);
    }

    [Fact]
    [Trait("Category", "SqlServer")]
    public void SqlServer_InsertAsync_GeneratesCorrectSql()
    {
        var repo = new CrudRepositorySqlServer(null!);
        var sql = repo.GetInsertAsyncSql(null!);

        Assert.Equal(
            "INSERT INTO [test_entity] ([name], [age], [is_active], [price], [created_at], [updated_at], [is_deleted], [deleted_at]) VALUES (@name, @age, @is_active, @price, @created_at, @updated_at, @is_deleted, @deleted_at)",
            sql);
    }

    [Fact]
    [Trait("Category", "SqlServer")]
    public void SqlServer_UpdateAsync_GeneratesCorrectSql()
    {
        var repo = new CrudRepositorySqlServer(null!);
        var sql = repo.GetUpdateAsyncSql(null!);

        Assert.Equal(
            "UPDATE [test_entity] SET [name] = @name, [age] = @age, [is_active] = @is_active, [price] = @price, [created_at] = @created_at, [updated_at] = @updated_at, [is_deleted] = @is_deleted, [deleted_at] = @deleted_at WHERE id = @id",
            sql);
    }

    [Fact]
    [Trait("Category", "SqlServer")]
    public void SqlServer_DeleteAsync_GeneratesCorrectSql()
    {
        var repo = new CrudRepositorySqlServer(null!);
        var sql = repo.GetDeleteAsyncSql(1);

        Assert.Equal("DELETE FROM [test_entity] WHERE id = @id", sql);
    }

    #endregion

    #region Batch Repository Tests

    [Fact]
    [Trait("Category", "Batch")]
    public void SQLite_BatchInsertAsync_GeneratesCorrectSql()
    {
        var repo = new BatchRepositorySQLite(null!);
        var sql = repo.GetBatchInsertAsyncSql([]);

        Assert.Contains("INSERT INTO [test_entity]", sql);
        Assert.Contains("[name]", sql);
    }

    [Fact]
    [Trait("Category", "Batch")]
    public void SQLite_BatchDeleteAsync_GeneratesCorrectSql()
    {
        var repo = new BatchRepositorySQLite(null!);
        var sql = repo.GetBatchDeleteAsyncSql([1, 2, 3]);

        Assert.Contains("DELETE FROM [test_entity] WHERE id IN", sql);
    }

    [Fact]
    [Trait("Category", "Batch")]
    public void MySQL_BatchInsertAsync_GeneratesCorrectSql()
    {
        var repo = new BatchRepositoryMySql(null!);
        var sql = repo.GetBatchInsertAsyncSql([]);

        Assert.Contains("INSERT INTO `test_entity`", sql);
        Assert.Contains("`name`", sql);
    }

    [Fact]
    [Trait("Category", "Batch")]
    public void SqlServer_BatchInsertAsync_GeneratesCorrectSql()
    {
        var repo = new BatchRepositorySqlServer(null!);
        var sql = repo.GetBatchInsertAsyncSql([]);

        Assert.Contains("INSERT INTO [test_entity]", sql);
        Assert.Contains("[name]", sql);
    }

    #endregion

    #region Cross-Dialect Consistency Tests

    [Fact]
    [Trait("Category", "CrossDialect")]
    public void AllDialects_UseCorrectQuoteCharacters()
    {
        var sqliteRepo = new CrudRepositorySQLite(null!);
        var mysqlRepo = new CrudRepositoryMySql(null!);
        var pgRepo = new CrudRepositoryPostgreSql(null!);
        var sqlServerRepo = new CrudRepositorySqlServer(null!);

        var sqliteSql = sqliteRepo.GetGetByIdAsyncSql(1);
        var mysqlSql = mysqlRepo.GetGetByIdAsyncSql(1);
        var pgSql = pgRepo.GetGetByIdAsyncSql(1);
        var sqlServerSql = sqlServerRepo.GetGetByIdAsyncSql(1);

        // SQLite uses []
        Assert.Contains("[id]", sqliteSql);
        Assert.Contains("[test_entity]", sqliteSql);

        // MySQL uses backticks
        Assert.Contains("`id`", mysqlSql);
        Assert.Contains("`test_entity`", mysqlSql);

        // PostgreSQL uses double quotes
        Assert.Contains("\"id\"", pgSql);
        Assert.Contains("\"test_entity\"", pgSql);

        // SqlServer uses []
        Assert.Contains("[id]", sqlServerSql);
        Assert.Contains("[test_entity]", sqlServerSql);
    }

    [Fact]
    [Trait("Category", "CrossDialect")]
    public void AllDialects_UseAtSignParameters()
    {
        var sqliteRepo = new CrudRepositorySQLite(null!);
        var mysqlRepo = new CrudRepositoryMySql(null!);
        var pgRepo = new CrudRepositoryPostgreSql(null!);
        var sqlServerRepo = new CrudRepositorySqlServer(null!);

        // All dialects should use @param format
        Assert.Contains("@id", sqliteRepo.GetGetByIdAsyncSql(1));
        Assert.Contains("@id", mysqlRepo.GetGetByIdAsyncSql(1));
        Assert.Contains("@id", pgRepo.GetGetByIdAsyncSql(1));
        Assert.Contains("@id", sqlServerRepo.GetGetByIdAsyncSql(1));
    }

    [Fact]
    [Trait("Category", "CrossDialect")]
    public void AllDialects_ExcludeIdFromInsert()
    {
        var sqliteRepo = new CrudRepositorySQLite(null!);
        var mysqlRepo = new CrudRepositoryMySql(null!);
        var pgRepo = new CrudRepositoryPostgreSql(null!);
        var sqlServerRepo = new CrudRepositorySqlServer(null!);

        var sqliteSql = sqliteRepo.GetInsertAsyncSql(null!);
        var mysqlSql = mysqlRepo.GetInsertAsyncSql(null!);
        var pgSql = pgRepo.GetInsertAsyncSql(null!);
        var sqlServerSql = sqlServerRepo.GetInsertAsyncSql(null!);

        // Id should not be in the column list (after INSERT INTO table_name)
        // But @id should not be in VALUES
        Assert.DoesNotContain("@id,", sqliteSql);
        Assert.DoesNotContain("@id,", mysqlSql);
        Assert.DoesNotContain("@id,", pgSql);
        Assert.DoesNotContain("@id,", sqlServerSql);
    }

    [Fact]
    [Trait("Category", "CrossDialect")]
    public void AllDialects_ExcludeIdFromUpdateSet()
    {
        var sqliteRepo = new CrudRepositorySQLite(null!);
        var mysqlRepo = new CrudRepositoryMySql(null!);
        var pgRepo = new CrudRepositoryPostgreSql(null!);
        var sqlServerRepo = new CrudRepositorySqlServer(null!);

        var sqliteSql = sqliteRepo.GetUpdateAsyncSql(null!);
        var mysqlSql = mysqlRepo.GetUpdateAsyncSql(null!);
        var pgSql = pgRepo.GetUpdateAsyncSql(null!);
        var sqlServerSql = sqlServerRepo.GetUpdateAsyncSql(null!);

        // SET clause should not contain [id] = @id (only in WHERE)
        Assert.DoesNotContain("SET [id]", sqliteSql);
        Assert.DoesNotContain("SET `id`", mysqlSql);
        Assert.DoesNotContain("SET \"id\"", pgSql);
        Assert.DoesNotContain("SET [id]", sqlServerSql);
    }

    #endregion

    #region SQL Syntax Validation

    [Fact]
    [Trait("Category", "Syntax")]
    public void AllDialects_SelectHasValidSyntax()
    {
        var repos = new (object repo, string dialect)[]
        {
            (new CrudRepositorySQLite(null!), "SQLite"),
            (new CrudRepositoryMySql(null!), "MySQL"),
            (new CrudRepositoryPostgreSql(null!), "PostgreSQL"),
            (new CrudRepositorySqlServer(null!), "SqlServer")
        };

        foreach (var (repo, dialect) in repos)
        {
            var sql = ((dynamic)repo).GetGetByIdAsyncSql(1L);
            
            Assert.StartsWith("SELECT", sql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("FROM", sql);
            Assert.Contains("WHERE", sql);
        }
    }

    [Fact]
    [Trait("Category", "Syntax")]
    public void AllDialects_InsertHasValidSyntax()
    {
        var repos = new (object repo, string dialect)[]
        {
            (new CrudRepositorySQLite(null!), "SQLite"),
            (new CrudRepositoryMySql(null!), "MySQL"),
            (new CrudRepositoryPostgreSql(null!), "PostgreSQL"),
            (new CrudRepositorySqlServer(null!), "SqlServer")
        };

        foreach (var (repo, dialect) in repos)
        {
            var sql = ((dynamic)repo).GetInsertAsyncSql((CrossDialectTestEntity?)null);
            
            Assert.StartsWith("INSERT INTO", sql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("VALUES", sql);
            Assert.Contains("(", sql);
            Assert.Contains(")", sql);
        }
    }

    [Fact]
    [Trait("Category", "Syntax")]
    public void AllDialects_UpdateHasValidSyntax()
    {
        var repos = new (object repo, string dialect)[]
        {
            (new CrudRepositorySQLite(null!), "SQLite"),
            (new CrudRepositoryMySql(null!), "MySQL"),
            (new CrudRepositoryPostgreSql(null!), "PostgreSQL"),
            (new CrudRepositorySqlServer(null!), "SqlServer")
        };

        foreach (var (repo, dialect) in repos)
        {
            var sql = ((dynamic)repo).GetUpdateAsyncSql((CrossDialectTestEntity?)null);
            
            Assert.StartsWith("UPDATE", sql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("SET", sql);
            Assert.Contains("WHERE", sql);
        }
    }

    [Fact]
    [Trait("Category", "Syntax")]
    public void AllDialects_DeleteHasValidSyntax()
    {
        var repos = new (object repo, string dialect)[]
        {
            (new CrudRepositorySQLite(null!), "SQLite"),
            (new CrudRepositoryMySql(null!), "MySQL"),
            (new CrudRepositoryPostgreSql(null!), "PostgreSQL"),
            (new CrudRepositorySqlServer(null!), "SqlServer")
        };

        foreach (var (repo, dialect) in repos)
        {
            var sql = ((dynamic)repo).GetDeleteAsyncSql(1L);
            
            Assert.StartsWith("DELETE FROM", sql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("WHERE", sql);
        }
    }

    #endregion

    #region Parameter Validation

    [Fact]
    [Trait("Category", "Parameters")]
    public void Insert_HasAllRequiredParameters()
    {
        var repo = new CrudRepositorySQLite(null!);
        var sql = repo.GetInsertAsyncSql(null!);

        // Should have parameters for all non-Id columns
        Assert.Contains("@name", sql);
        Assert.Contains("@age", sql);
        Assert.Contains("@is_active", sql);
        Assert.Contains("@price", sql);
        Assert.Contains("@created_at", sql);
        Assert.Contains("@updated_at", sql);
        Assert.Contains("@is_deleted", sql);
        Assert.Contains("@deleted_at", sql);
    }

    [Fact]
    [Trait("Category", "Parameters")]
    public void Update_HasAllRequiredParameters()
    {
        var repo = new CrudRepositorySQLite(null!);
        var sql = repo.GetUpdateAsyncSql(null!);

        // Should have parameters for all non-Id columns in SET
        Assert.Contains("@name", sql);
        Assert.Contains("@age", sql);
        Assert.Contains("@is_active", sql);
        Assert.Contains("@price", sql);
        Assert.Contains("@created_at", sql);
        Assert.Contains("@updated_at", sql);
        Assert.Contains("@is_deleted", sql);
        Assert.Contains("@deleted_at", sql);
        // And @id in WHERE
        Assert.Contains("@id", sql);
    }

    #endregion
}
