// -----------------------------------------------------------------------
// <copyright file="UnifiedDialect_PostgreSQL_Tests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Data.Common;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Npgsql;
using Sqlx.Annotations;
using Sqlx.Tests.Infrastructure;

namespace Sqlx.Tests.MultiDialect;

/// <summary>
/// PostgreSQL测试 - 只需要实现连接创建和表创建，测试方法自动继承！
/// </summary>
[TestClass]
[DoNotParallelize] // 禁用并行执行，因为所有测试共享同一个表
[TestCategory(TestCategories.Integration)]
[TestCategory(TestCategories.PostgreSQL)]
[TestCategory(TestCategories.CI)]
public class UnifiedDialect_PostgreSQL_Tests : UnifiedDialectTestBase
{
    protected override string TableName => "unified_dialect_users_pg";

    protected override DbConnection? CreateConnection()
    {
        return DatabaseConnectionHelper.GetPostgreSQLConnection(nameof(UnifiedDialect_PostgreSQL_Tests), TestContext);
    }

    protected override IUnifiedDialectUserRepository CreateRepository(DbConnection connection)
    {
        return new PostgreSQLUnifiedUserRepository(connection);
    }

    protected override SqlDefineTypes GetDialectType() => SqlDefineTypes.PostgreSql;

    protected override Task CreateTableAsync() => CreateUnifiedTableAsync();

    protected override Task DropTableAsync() => DropUnifiedTableAsync();

    public TestContext TestContext { get; set; } = null!;
    
    [ClassCleanup]
    public static async Task ClassCleanup()
    {
        await DatabaseConnectionHelper.CleanupContainerAsync(nameof(UnifiedDialect_PostgreSQL_Tests));
    }
}

