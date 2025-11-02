// -----------------------------------------------------------------------
// <copyright file="UnifiedDialect_PostgreSQL_Tests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Data.Common;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Npgsql;
using Sqlx.Tests.Infrastructure;

namespace Sqlx.Tests.MultiDialect;

/// <summary>
/// PostgreSQL测试 - 只需要实现连接创建和表创建，测试方法自动继承！
/// </summary>
[TestClass]
[TestCategory(TestCategories.Integration)]
[TestCategory(TestCategories.PostgreSQL)]
[TestCategory(TestCategories.CI)]
public class UnifiedDialect_PostgreSQL_Tests : UnifiedDialectTestBase
{
    protected override string TableName => "unified_dialect_users_pg";

    protected override DbConnection CreateConnection()
    {
        return DatabaseConnectionHelper.GetPostgreSQLConnection(TestContext)!;
    }

    protected override IUnifiedDialectUserRepository CreateRepository(DbConnection connection)
    {
        return new PostgreSQLUnifiedUserRepository(connection);
    }

    protected override async Task CreateTableAsync()
    {
        var sql = $@"
            CREATE TABLE {TableName} (
                id BIGSERIAL PRIMARY KEY,
                username TEXT NOT NULL,
                email TEXT NOT NULL,
                age INTEGER NOT NULL,
                balance DECIMAL(18, 2) NOT NULL,
                created_at TIMESTAMP NOT NULL,
                last_login_at TIMESTAMP,
                is_active BOOLEAN NOT NULL
            )";

        using var cmd = Connection!.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync();
    }

    protected override async Task DropTableAsync()
    {
        using var cmd = Connection!.CreateCommand();
        cmd.CommandText = $"DROP TABLE IF EXISTS {TableName}";
        await cmd.ExecuteNonQueryAsync();
    }

    public TestContext TestContext { get; set; } = null!;
}

