using Sqlx;
using Sqlx.Annotations;
using System.Data.Common;

namespace FullDemo.Repositories.MySQL;

// ==================== MySQL 仓储实现 ====================

[SqlDefine(SqlDefineTypes.MySql)]
[TableName("users")]
[RepositoryFor(typeof(IUserRepository))]
public partial class MySQLUserRepository(DbConnection connection) : IUserRepository
{
    partial void OnExecuting(string operationName, DbCommand command)
    {
        Console.WriteLine($"[MySQL] {operationName}: {command.CommandText}");
    }
}

[SqlDefine(SqlDefineTypes.MySql)]
[TableName("products")]
[RepositoryFor(typeof(IProductRepository))]
public partial class MySQLProductRepository(DbConnection connection) : IProductRepository
{
    partial void OnExecuting(string operationName, DbCommand command)
    {
        Console.WriteLine($"[MySQL] {operationName}: {command.CommandText}");
    }
}

[SqlDefine(SqlDefineTypes.MySql)]
[TableName("orders")]
[RepositoryFor(typeof(IOrderRepository))]
public partial class MySQLOrderRepository(DbConnection connection) : IOrderRepository
{
    partial void OnExecuting(string operationName, DbCommand command)
    {
        Console.WriteLine($"[MySQL] {operationName}: {command.CommandText}");
    }
}

[SqlDefine(SqlDefineTypes.MySql)]
[TableName("accounts")]
[RepositoryFor(typeof(IAccountRepository))]
public partial class MySQLAccountRepository(DbConnection connection) : IAccountRepository
{
    partial void OnExecuting(string operationName, DbCommand command)
    {
        Console.WriteLine($"[MySQL] {operationName}: {command.CommandText}");
    }
}

[SqlDefine(SqlDefineTypes.MySql)]
[TableName("logs")]
[RepositoryFor(typeof(ILogRepository))]
public partial class MySQLLogRepository(DbConnection connection) : ILogRepository
{
    partial void OnExecuting(string operationName, DbCommand command)
    {
        Console.WriteLine($"[MySQL] {operationName}: {command.CommandText}");
    }
}

[SqlDefine(SqlDefineTypes.MySql)]
[TableName("categories")]
[RepositoryFor(typeof(ICategoryRepository))]
public partial class MySQLCategoryRepository(DbConnection connection) : ICategoryRepository
{
    partial void OnExecuting(string operationName, DbCommand command)
    {
        Console.WriteLine($"[MySQL] {operationName}: {command.CommandText}");
    }
}
