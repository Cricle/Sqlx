// -----------------------------------------------------------------------
// <copyright file="UnifiedDialect_MySQL_Tests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Data.Common;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MySqlConnector;
using Sqlx.Annotations;
using Sqlx.Tests.Infrastructure;

namespace Sqlx.Tests.MultiDialect;

/// <summary>
/// MySQL测试 - 只需要实现连接创建和指定方言，测试方法自动继承！
/// </summary>
[TestClass]
[DoNotParallelize] // 禁用并行执行，因为所有测试共享同一个表
[TestCategory(TestCategories.Integration)]
[TestCategory(TestCategories.MySQL)]
[TestCategory(TestCategories.CI)]
public class UnifiedDialect_MySQL_Tests : UnifiedDialectTestBase
{
    protected override string TableName => "unified_dialect_users_my";

    protected override DbConnection? CreateConnection()
    {
        return DatabaseConnectionHelper.GetMySQLConnection(nameof(UnifiedDialect_MySQL_Tests), TestContext);
    }

    protected override IUnifiedDialectUserRepository CreateRepository(DbConnection connection)
    {
        return new MySQLUnifiedUserRepository(connection);
    }

    protected override SqlDefineTypes GetDialectType() => SqlDefineTypes.MySql;

    protected override Task CreateTableAsync() => CreateUnifiedTableAsync();

    protected override Task DropTableAsync() => DropUnifiedTableAsync();

    public TestContext TestContext { get; set; } = null!;
    
    [ClassCleanup]
    public static async Task ClassCleanup()
    {
        await DatabaseConnectionHelper.CleanupContainerAsync(nameof(UnifiedDialect_MySQL_Tests));
    }
}

