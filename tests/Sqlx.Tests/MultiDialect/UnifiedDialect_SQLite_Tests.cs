// -----------------------------------------------------------------------
// <copyright file="UnifiedDialect_SQLite_Tests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Data.Common;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sqlx.Tests.MultiDialect;

/// <summary>
/// SQLite测试 - 只需要实现连接创建和表创建，测试方法自动继承！
/// </summary>
[TestClass]
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

    protected override async Task CreateTableAsync()
    {
        var sql = $@"
            CREATE TABLE {TableName} (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                username TEXT NOT NULL,
                email TEXT NOT NULL,
                age INTEGER NOT NULL,
                balance REAL NOT NULL,
                created_at TEXT NOT NULL,
                last_login_at TEXT,
                is_active INTEGER NOT NULL
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
}

