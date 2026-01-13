// -----------------------------------------------------------------------
// <copyright file="ComprehensiveSqlValidationTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Xunit;

namespace Sqlx.Tests.CrossDialect;

/// <summary>
/// Comprehensive SQL validation tests for all predefined interfaces across all dialects.
/// Validates SQL output including column quoting, parameter format, and syntax.
/// </summary>
public class ComprehensiveSqlValidationTests
{
    #region SQLite Tests

    [Fact]
    [Trait("Category", "SQLite")]
    public void SQLite_GetByIdAsync_GeneratesCorrectSql()
    {
        var repo = new CrudRepositorySQLite(null!);
        var template = repo.GetGetByIdAsyncSql();

        Assert.Contains("[id]", template.Sql);
        Assert.Contains("test_entity", template.Sql);
        Assert.Contains("WHERE id = @id", template.Sql);
    }

    [Fact]
    [Trait("Category", "SQLite")]
    public void SQLite_GetByIdsAsync_GeneratesCorrectSql()
    {
        var repo = new CrudRepositorySQLite(null!);
        var template = repo.GetGetByIdsAsyncSql();

        Assert.Contains("SELECT", template.Sql);
        Assert.Contains("test_entity", template.Sql);
        Assert.Contains("WHERE id IN", template.Sql);
    }

    [Fact]
    [Trait("Category", "SQLite")]
    public void SQLite_GetAllAsync_GeneratesCorrectSql()
    {
        var repo = new CrudRepositorySQLite(null!);
        var template = repo.GetGetAllAsyncSql();

        Assert.Contains("SELECT", template.Sql);
        Assert.Contains("test_entity", template.Sql);
    }

    [Fact]
    [Trait("Category", "SQLite")]
    public void SQLite_GetWhereAsync_GeneratesCorrectSql()
    {
        var repo = new CrudRepositorySQLite(null!);
        var template = repo.GetGetWhereAsyncSql();

        Assert.Contains("SELECT", template.Sql);
        Assert.Contains("test_entity", template.Sql);
    }

    [Fact]
    [Trait("Category", "SQLite")]
    public void SQLite_GetFirstWhereAsync_GeneratesCorrectSql()
    {
        var repo = new CrudRepositorySQLite(null!);
        var template = repo.GetGetFirstWhereAsyncSql();

        Assert.Contains("SELECT", template.Sql);
        Assert.Contains("test_entity", template.Sql);
        Assert.Contains("LIMIT 1", template.Sql);
    }

    [Fact]
    [Trait("Category", "SQLite")]
    public void SQLite_CountAsync_GeneratesCorrectSql()
    {
        var repo = new CrudRepositorySQLite(null!);
        var template = repo.GetCountAsyncSql();

        Assert.Contains("COUNT(*)", template.Sql);
        Assert.Contains("test_entity", template.Sql);
    }

    [Fact]
    [Trait("Category", "SQLite")]
    public void SQLite_InsertAsync_GeneratesCorrectSql()
    {
        var repo = new CrudRepositorySQLite(null!);
        var template = repo.GetInsertAndGetIdAsyncSql();

        Assert.Contains("INSERT INTO", template.Sql);
        Assert.Contains("test_entity", template.Sql);
        Assert.Contains("VALUES", template.Sql);
    }

    [Fact]
    [Trait("Category", "SQLite")]
    public void SQLite_UpdateAsync_GeneratesCorrectSql()
    {
        var repo = new CrudRepositorySQLite(null!);
        var template = repo.GetUpdateAsyncSql();

        Assert.Contains("UPDATE", template.Sql);
        Assert.Contains("test_entity", template.Sql);
        Assert.Contains("SET", template.Sql);
        Assert.Contains("WHERE id = @id", template.Sql);
    }

    [Fact]
    [Trait("Category", "SQLite")]
    public void SQLite_DeleteAsync_GeneratesCorrectSql()
    {
        var repo = new CrudRepositorySQLite(null!);
        var template = repo.GetDeleteAsyncSql();

        Assert.Contains("DELETE FROM", template.Sql);
        Assert.Contains("test_entity", template.Sql);
        Assert.Contains("WHERE id = @id", template.Sql);
    }

    #endregion

    #region MySQL Tests

    [Fact]
    [Trait("Category", "MySQL")]
    public void MySQL_GetByIdAsync_GeneratesCorrectSql()
    {
        var repo = new CrudRepositoryMySql(null!);
        var template = repo.GetGetByIdAsyncSql();

        Assert.Contains("`id`", template.Sql);
        Assert.Contains("test_entity", template.Sql);
        Assert.Contains("WHERE id = @id", template.Sql);
    }

    [Fact]
    [Trait("Category", "MySQL")]
    public void MySQL_InsertAsync_GeneratesCorrectSql()
    {
        var repo = new CrudRepositoryMySql(null!);
        var template = repo.GetInsertAndGetIdAsyncSql();

        Assert.Contains("INSERT INTO", template.Sql);
        Assert.Contains("test_entity", template.Sql);
        Assert.Contains("VALUES", template.Sql);
    }

    [Fact]
    [Trait("Category", "MySQL")]
    public void MySQL_UpdateAsync_GeneratesCorrectSql()
    {
        var repo = new CrudRepositoryMySql(null!);
        var template = repo.GetUpdateAsyncSql();

        Assert.Contains("UPDATE", template.Sql);
        Assert.Contains("test_entity", template.Sql);
        Assert.Contains("SET", template.Sql);
    }

    #endregion

    #region PostgreSQL Tests

    [Fact]
    [Trait("Category", "PostgreSQL")]
    public void PostgreSQL_GetByIdAsync_GeneratesCorrectSql()
    {
        var repo = new CrudRepositoryPostgreSql(null!);
        var template = repo.GetGetByIdAsyncSql();

        Assert.Contains("\"id\"", template.Sql);
        Assert.Contains("test_entity", template.Sql);
        // PostgreSQL uses $ prefix for parameters, but template uses @id which is converted
        Assert.Contains("WHERE id =", template.Sql);
    }

    [Fact]
    [Trait("Category", "PostgreSQL")]
    public void PostgreSQL_InsertAsync_GeneratesCorrectSql()
    {
        var repo = new CrudRepositoryPostgreSql(null!);
        var template = repo.GetInsertAndGetIdAsyncSql();

        Assert.Contains("INSERT INTO", template.Sql);
        Assert.Contains("test_entity", template.Sql);
        Assert.Contains("VALUES", template.Sql);
    }

    #endregion

    #region SqlServer Tests

    [Fact]
    [Trait("Category", "SqlServer")]
    public void SqlServer_GetByIdAsync_GeneratesCorrectSql()
    {
        var repo = new CrudRepositorySqlServer(null!);
        var template = repo.GetGetByIdAsyncSql();

        Assert.Contains("[id]", template.Sql);
        Assert.Contains("test_entity", template.Sql);
        Assert.Contains("WHERE id = @id", template.Sql);
    }

    [Fact]
    [Trait("Category", "SqlServer")]
    public void SqlServer_GetFirstWhereAsync_GeneratesCorrectSql()
    {
        var repo = new CrudRepositorySqlServer(null!);
        var template = repo.GetGetFirstWhereAsyncSql();

        Assert.Contains("SELECT", template.Sql);
        Assert.Contains("test_entity", template.Sql);
        // SqlServer uses OFFSET...FETCH for LIMIT
        Assert.Contains("OFFSET 0 ROWS FETCH NEXT 1 ROWS ONLY", template.Sql);
    }

    [Fact]
    [Trait("Category", "SqlServer")]
    public void SqlServer_InsertAsync_GeneratesCorrectSql()
    {
        var repo = new CrudRepositorySqlServer(null!);
        var template = repo.GetInsertAndGetIdAsyncSql();

        Assert.Contains("INSERT INTO", template.Sql);
        Assert.Contains("test_entity", template.Sql);
        Assert.Contains("VALUES", template.Sql);
    }

    #endregion

    #region Batch Repository Tests

    [Fact]
    [Trait("Category", "Batch")]
    public void SQLite_BatchInsertAsync_GeneratesCorrectSql()
    {
        var repo = new BatchRepositorySQLite(null!);
        var template = repo.GetBatchInsertAsyncSql();

        Assert.Contains("INSERT INTO", template.Sql);
        Assert.Contains("test_entity", template.Sql);
    }

    [Fact]
    [Trait("Category", "Batch")]
    public void MySQL_BatchInsertAsync_GeneratesCorrectSql()
    {
        var repo = new BatchRepositoryMySql(null!);
        var template = repo.GetBatchInsertAsyncSql();

        Assert.Contains("INSERT INTO", template.Sql);
        Assert.Contains("test_entity", template.Sql);
    }

    [Fact]
    [Trait("Category", "Batch")]
    public void SqlServer_BatchInsertAsync_GeneratesCorrectSql()
    {
        var repo = new BatchRepositorySqlServer(null!);
        var template = repo.GetBatchInsertAsyncSql();

        Assert.Contains("INSERT INTO", template.Sql);
        Assert.Contains("test_entity", template.Sql);
    }

    #endregion

    #region SqlTemplate Return Type Tests

    [Fact]
    [Trait("Category", "SqlTemplate")]
    public void GetXxxSql_ReturnsSqlTemplate()
    {
        var repo = new CrudRepositorySQLite(null!);
        var template = repo.GetGetByIdAsyncSql();

        Assert.IsType<SqlTemplate>(template);
        Assert.NotNull(template.Sql);
        Assert.NotNull(template.Parameters);
    }

    [Fact]
    [Trait("Category", "SqlTemplate")]
    public void SqlTemplate_CanExecute()
    {
        var repo = new CrudRepositorySQLite(null!);
        var template = repo.GetGetByIdAsyncSql();

        // SqlTemplate can be executed with parameters
        var parameterizedSql = template.Execute(new System.Collections.Generic.Dictionary<string, object?> { ["id"] = 1L });
        Assert.NotNull(parameterizedSql.Sql);
    }

    #endregion
}
