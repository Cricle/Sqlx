// -----------------------------------------------------------------------
// <copyright file="UnifiedDialect_SqlServer_Tests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Data.Common;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;
using Sqlx.Tests.Infrastructure;

namespace Sqlx.Tests.MultiDialect;

/// <summary>
/// SQL Server测试 - 只需要实现连接创建和指定方言，测试方法自动继承！
/// </summary>
[TestClass]
[DoNotParallelize] // 禁用并行执行，因为所有测试共享同一个表
[TestCategory(TestCategories.Integration)]
[TestCategory(TestCategories.SqlServer)]
[TestCategory(TestCategories.CI)]
public class UnifiedDialect_SqlServer_Tests : UnifiedDialectTestBase
{
    protected override string TableName => "unified_dialect_users_ss";

    protected override DbConnection? CreateConnection()
    {
        return DatabaseConnectionHelper.GetSqlServerConnection(nameof(UnifiedDialect_SqlServer_Tests), TestContext);
    }

    protected override IUnifiedDialectUserRepository CreateRepository(DbConnection connection)
    {
        return new SqlServerUnifiedUserRepository(connection);
    }

    protected override SqlDefineTypes GetDialectType() => SqlDefineTypes.SqlServer;

    protected override Task CreateTableAsync() => CreateUnifiedTableAsync();

    protected override Task DropTableAsync() => DropUnifiedTableAsync();

    public TestContext TestContext { get; set; } = null!;
    
    [ClassCleanup]
    public static async Task ClassCleanup()
    {
        await DatabaseConnectionHelper.CleanupContainerAsync(nameof(UnifiedDialect_SqlServer_Tests));
    }
}

