using System.Data.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;
using Sqlx.Tests.Infrastructure;

namespace Sqlx.Tests.MultiDialect;

/// <summary>
/// MySQL综合功能测试 - 使用统一接口
/// </summary>
[TestClass]
[TestCategory(TestCategories.MySQL)]
[TestCategory(TestCategories.Integration)]
[TestCategory(TestCategories.CI)]
public class TDD_MySQL_Comprehensive : ComprehensiveTestBase
{
    protected override string DialectName => "MySQL";
    protected override string TableName => "dialect_users_mysql";

    protected override DbConnection CreateConnection()
    {
        // 检查是否在CI环境
        if (!DatabaseConnectionHelper.IsCI)
        {
            Assert.Inconclusive("MySQL tests are only run in CI environment.");
        }

        var conn = DatabaseConnectionHelper.GetMySQLConnection();
        if (conn == null)
        {
            Assert.Inconclusive("MySQL connection not available. MySqlConnector package not installed or connection failed.");
        }
        return conn!;
    }

    protected override void CreateTable()
    {
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = @"
            DROP TABLE IF EXISTS dialect_users_mysql;

            CREATE TABLE dialect_users_mysql (
                id BIGINT AUTO_INCREMENT PRIMARY KEY,
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
        return new MySQLUserRepository(_connection!);
    }
}

/// <summary>
/// MySQL用户仓储 - 继承基类接口，无需重复定义
/// </summary>
[RepositoryFor(typeof(IDialectUserRepositoryBase))]
[SqlDefine(SqlDefineTypes.MySql)]
public partial class MySQLUserRepository : IDialectUserRepositoryBase
{
    public MySQLUserRepository(DbConnection connection)
    {
    }
}

