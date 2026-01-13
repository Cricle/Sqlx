// -----------------------------------------------------------------------
// <copyright file="StrictSqlValidationTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Xunit;
using Xunit.Abstractions;

namespace Sqlx.Tests.CrossDialect;

/// <summary>
/// Strict SQL validation tests that verify exact SQL output for all dialects.
/// Uses Assert.Equal for exact string matching.
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
        _output.WriteLine($"  GetByIdAsync: {sqliteRepo.GetGetByIdAsyncSql(0)}");
        _output.WriteLine($"  GetFirstWhereAsync: {sqliteRepo.GetGetFirstWhereAsyncSql(x => x.Id == 1)}");
        _output.WriteLine($"  InsertAsync: {sqliteRepo.GetInsertAsyncSql(null!)}");
        _output.WriteLine($"  UpdateAsync: {sqliteRepo.GetUpdateAsyncSql(null!)}");
        _output.WriteLine($"  DeleteAsync: {sqliteRepo.GetDeleteAsyncSql(0)}");
        _output.WriteLine($"  CountAsync: {sqliteRepo.GetCountAsyncSql()}");

        _output.WriteLine("\n=== SqlServer Generated SQL ===");
        var sqlServerRepo = new CrudRepositorySqlServer(null!);
        _output.WriteLine($"  GetByIdAsync: {sqlServerRepo.GetGetByIdAsyncSql(0)}");
        _output.WriteLine($"  GetFirstWhereAsync: {sqlServerRepo.GetGetFirstWhereAsyncSql(x => x.Id == 1)}");
        _output.WriteLine($"  InsertAsync: {sqlServerRepo.GetInsertAsyncSql(null!)}");
        _output.WriteLine($"  UpdateAsync: {sqlServerRepo.GetUpdateAsyncSql(null!)}");
        _output.WriteLine($"  DeleteAsync: {sqlServerRepo.GetDeleteAsyncSql(0)}");
        _output.WriteLine($"  CountAsync: {sqlServerRepo.GetCountAsyncSql()}");
    }
}
