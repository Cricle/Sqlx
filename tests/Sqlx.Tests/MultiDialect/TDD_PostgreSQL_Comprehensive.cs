// -----------------------------------------------------------------------
// <copyright file="TDD_PostgreSQL_Comprehensive.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;
using Sqlx.Tests.Infrastructure;

namespace Sqlx.Tests.MultiDialect;

/// <summary>
/// PostgreSQL 综合功能测试
/// 注意：这些测试需要真实的PostgreSQL数据库连接，目前暂时跳过
/// </summary>
[TestClass]
[TestCategory(TestCategories.PostgreSQL)]
[TestCategory(TestCategories.Integration)]
[TestCategory(TestCategories.CI)]
public class TDD_PostgreSQL_Comprehensive : ComprehensiveTestBase
{
    protected override string DialectName => "PostgreSQL";
    protected override string TableName => "dialect_users_postgresql";

    protected override DbConnection CreateConnection()
    {
        // 检查是否在CI环境
        if (!DatabaseConnectionHelper.IsCI)
        {
            Assert.Inconclusive("PostgreSQL tests are only run in CI environment.");
        }

        var conn = DatabaseConnectionHelper.GetPostgreSQLConnection();
        if (conn == null)
        {
            Assert.Inconclusive("PostgreSQL connection not available. Npgsql package not installed or connection failed.");
        }
        return conn!;
    }

    protected override void CreateTable()
    {
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = @"
            DROP TABLE IF EXISTS dialect_users_postgresql;

            CREATE TABLE dialect_users_postgresql (
                id SERIAL PRIMARY KEY,
                username VARCHAR(50) NOT NULL,
                email VARCHAR(100),
                age INT NOT NULL,
                balance DECIMAL(18,2) NOT NULL DEFAULT 0,
                is_active BOOLEAN NOT NULL DEFAULT TRUE,
                created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
            );
        ";
        cmd.ExecuteNonQuery();
    }

    protected override IDialectUserRepositoryBase CreateRepository()
    {
        return new PostgreSQLUserRepository(_connection!);
    }
}

/// <summary>
/// PostgreSQL用户仓储 - 继承基类接口，无需重复定义
/// </summary>
[RepositoryFor(typeof(IDialectUserRepositoryBase))]
[SqlDefine(SqlDefineTypes.PostgreSql)]
public partial class PostgreSQLUserRepository : IDialectUserRepositoryBase
{
    public PostgreSQLUserRepository(DbConnection connection)
    {
    }
}

