using System.Data.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;
using Sqlx.Tests.Infrastructure;

namespace Sqlx.Tests.MultiDialect;

/// <summary>
/// SQL Server综合功能测试 - 使用统一接口
/// </summary>
[TestClass]
[TestCategory(TestCategories.SqlServer)]
[TestCategory(TestCategories.Integration)]
[TestCategory(TestCategories.CI)]
public class TDD_SqlServer_Comprehensive : ComprehensiveTestBase
{
    protected override string DialectName => "SQL Server";
    protected override string TableName => "dialect_users_sqlserver";

    protected override DbConnection CreateConnection()
    {
        // 检查是否在CI环境
        if (!DatabaseConnectionHelper.IsCI)
        {
            Assert.Inconclusive("SQL Server tests are only run in CI environment.");
        }

        var conn = DatabaseConnectionHelper.GetSqlServerConnection();
        if (conn == null)
        {
            Assert.Inconclusive("SQL Server connection not available. Microsoft.Data.SqlClient package not installed or connection failed.");
        }
        return conn!;
    }

    protected override void CreateTable()
    {
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = @"
            IF OBJECT_ID('dialect_users_sqlserver', 'U') IS NOT NULL
                DROP TABLE dialect_users_sqlserver;

            CREATE TABLE dialect_users_sqlserver (
                id BIGINT IDENTITY(1,1) PRIMARY KEY,
                username NVARCHAR(50) NOT NULL,
                email NVARCHAR(100),
                age INT NOT NULL,
                balance DECIMAL(18,2) NOT NULL DEFAULT 0,
                is_active BIT NOT NULL DEFAULT 1,
                created_at DATETIME2 NOT NULL DEFAULT GETDATE()
            );
        ";
        cmd.ExecuteNonQuery();
    }

    protected override IDialectUserRepositoryBase CreateRepository()
    {
        return new SqlServerUserRepository(_connection!);
    }
}

/// <summary>
/// SQL Server用户仓储 - 继承基类接口，无需重复定义
/// </summary>
[RepositoryFor(typeof(IDialectUserRepositoryBase))]
[SqlDefine(SqlDefineTypes.SqlServer)]
public partial class SqlServerUserRepository : IDialectUserRepositoryBase
{
    public SqlServerUserRepository(DbConnection connection)
    {
    }
}

