using System.Data.Common;
using Sqlx;
using Sqlx.Annotations;
using FullDemo.Models;

namespace FullDemo.Repositories.PostgreSQL;

// ==================== PostgreSQL 仓储实现 ====================

[SqlDefine(SqlDefineTypes.PostgreSql)]
[TableName("users")]
[RepositoryFor(typeof(IUserRepository))]
public partial class PostgreSQLUserRepository(DbConnection connection) : IUserRepository
{
    partial void OnExecuting(string operationName, DbCommand command)
    {
        Console.WriteLine($"[PostgreSQL] {operationName}: {command.CommandText}");
    }
}

[SqlDefine(SqlDefineTypes.PostgreSql)]
[TableName("products")]
[RepositoryFor(typeof(IProductRepository))]
public partial class PostgreSQLProductRepository(DbConnection connection) : IProductRepository
{
    partial void OnExecuting(string operationName, DbCommand command)
    {
        Console.WriteLine($"[PostgreSQL] {operationName}: {command.CommandText}");
    }
}

[SqlDefine(SqlDefineTypes.PostgreSql)]
[TableName("orders")]
[RepositoryFor(typeof(IOrderRepository))]
public partial class PostgreSQLOrderRepository(DbConnection connection) : IOrderRepository
{
    partial void OnExecuting(string operationName, DbCommand command)
    {
        Console.WriteLine($"[PostgreSQL] {operationName}: {command.CommandText}");
    }
}

[SqlDefine(SqlDefineTypes.PostgreSql)]
[TableName("accounts")]
[RepositoryFor(typeof(IAccountRepository))]
public partial class PostgreSQLAccountRepository(DbConnection connection) : IAccountRepository
{
    partial void OnExecuting(string operationName, DbCommand command)
    {
        Console.WriteLine($"[PostgreSQL] {operationName}: {command.CommandText}");
    }
}

[SqlDefine(SqlDefineTypes.PostgreSql)]
[TableName("logs")]
[RepositoryFor(typeof(ILogRepository))]
public partial class PostgreSQLLogRepository(DbConnection connection) : ILogRepository
{
    partial void OnExecuting(string operationName, DbCommand command)
    {
        Console.WriteLine($"[PostgreSQL] {operationName}: {command.CommandText}");
    }
}

[SqlDefine(SqlDefineTypes.PostgreSql)]
[TableName("categories")]
[RepositoryFor(typeof(ICategoryRepository))]
public partial class PostgreSQLCategoryRepository(DbConnection connection) : ICategoryRepository
{
    partial void OnExecuting(string operationName, DbCommand command)
    {
        Console.WriteLine($"[PostgreSQL] {operationName}: {command.CommandText}");
    }
}
