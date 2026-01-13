// -----------------------------------------------------------------------
// <copyright file="StrictSqlValidationTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Xunit;
using Xunit.Abstractions;

namespace Sqlx.Tests.CrossDialect;

/// <summary>
/// Strict SQL validation tests that verify SQL output for all dialects.
/// </summary>
public class StrictSqlValidationTests
{
    private readonly ITestOutputHelper _output;

    public StrictSqlValidationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    [Trait("Category", "CrossDialect")]
    [Trait("Category", "Debug")]
    public void PrintAllGeneratedSql()
    {
        _output.WriteLine("=== SQLite Generated SQL ===");
        var sqliteRepo = new CrudRepositorySQLite(null!);
        _output.WriteLine($"  GetByIdAsync: {sqliteRepo.GetGetByIdAsyncSql().Sql}");
        _output.WriteLine($"  GetFirstWhereAsync: {sqliteRepo.GetGetFirstWhereAsyncSql().Sql}");
        _output.WriteLine($"  InsertAndGetIdAsync: {sqliteRepo.GetInsertAndGetIdAsyncSql().Sql}");
        _output.WriteLine($"  UpdateAsync: {sqliteRepo.GetUpdateAsyncSql().Sql}");
        _output.WriteLine($"  DeleteAsync: {sqliteRepo.GetDeleteAsyncSql().Sql}");
        _output.WriteLine($"  CountAsync: {sqliteRepo.GetCountAsyncSql().Sql}");

        _output.WriteLine("\n=== SqlServer Generated SQL ===");
        var sqlServerRepo = new CrudRepositorySqlServer(null!);
        _output.WriteLine($"  GetByIdAsync: {sqlServerRepo.GetGetByIdAsyncSql().Sql}");
        _output.WriteLine($"  GetFirstWhereAsync: {sqlServerRepo.GetGetFirstWhereAsyncSql().Sql}");
        _output.WriteLine($"  InsertAndGetIdAsync: {sqlServerRepo.GetInsertAndGetIdAsyncSql().Sql}");
        _output.WriteLine($"  UpdateAsync: {sqlServerRepo.GetUpdateAsyncSql().Sql}");
        _output.WriteLine($"  DeleteAsync: {sqlServerRepo.GetDeleteAsyncSql().Sql}");
        _output.WriteLine($"  CountAsync: {sqlServerRepo.GetCountAsyncSql().Sql}");

        _output.WriteLine("\n=== MySQL Generated SQL ===");
        var mysqlRepo = new CrudRepositoryMySql(null!);
        _output.WriteLine($"  GetByIdAsync: {mysqlRepo.GetGetByIdAsyncSql().Sql}");
        _output.WriteLine($"  InsertAndGetIdAsync: {mysqlRepo.GetInsertAndGetIdAsyncSql().Sql}");

        _output.WriteLine("\n=== PostgreSQL Generated SQL ===");
        var pgRepo = new CrudRepositoryPostgreSql(null!);
        _output.WriteLine($"  GetByIdAsync: {pgRepo.GetGetByIdAsyncSql().Sql}");
        _output.WriteLine($"  InsertAndGetIdAsync: {pgRepo.GetInsertAndGetIdAsyncSql().Sql}");
    }

    [Fact]
    [Trait("Category", "CrossDialect")]
    public void AllDialects_GetByIdAsync_ContainsCorrectColumnQuoting()
    {
        // SQLite uses []
        var sqliteRepo = new CrudRepositorySQLite(null!);
        Assert.Contains("[id]", sqliteRepo.GetGetByIdAsyncSql().Sql);

        // MySQL uses ``
        var mysqlRepo = new CrudRepositoryMySql(null!);
        Assert.Contains("`id`", mysqlRepo.GetGetByIdAsyncSql().Sql);

        // PostgreSQL uses ""
        var pgRepo = new CrudRepositoryPostgreSql(null!);
        Assert.Contains("\"id\"", pgRepo.GetGetByIdAsyncSql().Sql);

        // SqlServer uses []
        var sqlServerRepo = new CrudRepositorySqlServer(null!);
        Assert.Contains("[id]", sqlServerRepo.GetGetByIdAsyncSql().Sql);
    }

    [Fact]
    [Trait("Category", "CrossDialect")]
    public void AllDialects_GetFirstWhereAsync_ContainsCorrectLimitSyntax()
    {
        // SQLite uses LIMIT
        var sqliteRepo = new CrudRepositorySQLite(null!);
        Assert.Contains("LIMIT 1", sqliteRepo.GetGetFirstWhereAsyncSql().Sql);

        // MySQL uses LIMIT
        var mysqlRepo = new CrudRepositoryMySql(null!);
        Assert.Contains("LIMIT 1", mysqlRepo.GetGetFirstWhereAsyncSql().Sql);

        // PostgreSQL uses LIMIT
        var pgRepo = new CrudRepositoryPostgreSql(null!);
        Assert.Contains("LIMIT 1", pgRepo.GetGetFirstWhereAsyncSql().Sql);

        // SqlServer uses OFFSET...FETCH
        var sqlServerRepo = new CrudRepositorySqlServer(null!);
        Assert.Contains("OFFSET 0 ROWS FETCH NEXT 1 ROWS ONLY", sqlServerRepo.GetGetFirstWhereAsyncSql().Sql);
    }
}
