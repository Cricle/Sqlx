// -----------------------------------------------------------------------
// <copyright file="UnifiedDialect_SQLite_Tests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Data.Common;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

namespace Sqlx.Tests.MultiDialect;

/// <summary>
/// SQLite测试 - 只需要实现连接创建和表创建，测试方法自动继承！
/// </summary>
[TestClass]
[DoNotParallelize] // 禁用并行执行，因为所有测试共享同一个表
[TestCategory(TestCategories.Integration)]
[TestCategory(TestCategories.SQLite)]
public class UnifiedDialect_SQLite_Tests : UnifiedDialectTestBase
{
    protected override string TableName => "unified_dialect_users_sq";

    protected override DbConnection CreateConnection()
    {
        return new SqliteConnection("Data Source=:memory:");
    }

    protected override IUnifiedDialectUserRepository CreateRepository(DbConnection connection)
    {
        return new SQLiteUnifiedUserRepository(connection);
    }

    protected override SqlDefineTypes GetDialectType() => SqlDefineTypes.SQLite;

    protected override Task CreateTableAsync() => CreateUnifiedTableAsync();

    protected override Task DropTableAsync() => DropUnifiedTableAsync();
}

