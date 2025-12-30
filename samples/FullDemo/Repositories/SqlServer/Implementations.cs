using System.Data.Common;
using Sqlx;
using Sqlx.Annotations;
using FullDemo.Models;

namespace FullDemo.Repositories.SqlServer;

// ==================== SQL Server 仓储实现 ====================

[SqlDefine(SqlDefineTypes.SqlServer)]
[TableName("users")]
[RepositoryFor(typeof(IUserRepository))]
public partial class SqlServerUserRepository(DbConnection connection) : IUserRepository
{
    partial void OnExecuting(string operationName, DbCommand command)
    {
        Console.WriteLine($"[SqlServer] {operationName}: {command.CommandText}");
    }
}

[SqlDefine(SqlDefineTypes.SqlServer)]
[TableName("products")]
[RepositoryFor(typeof(IProductRepository))]
public partial class SqlServerProductRepository(DbConnection connection) : IProductRepository
{
    partial void OnExecuting(string operationName, DbCommand command)
    {
        Console.WriteLine($"[SqlServer] {operationName}: {command.CommandText}");
    }
}

[SqlDefine(SqlDefineTypes.SqlServer)]
[TableName("orders")]
[RepositoryFor(typeof(IOrderRepository))]
public partial class SqlServerOrderRepository(DbConnection connection) : IOrderRepository
{
    partial void OnExecuting(string operationName, DbCommand command)
    {
        Console.WriteLine($"[SqlServer] {operationName}: {command.CommandText}");
    }
}

[SqlDefine(SqlDefineTypes.SqlServer)]
[TableName("accounts")]
[RepositoryFor(typeof(IAccountRepository))]
public partial class SqlServerAccountRepository(DbConnection connection) : IAccountRepository
{
    partial void OnExecuting(string operationName, DbCommand command)
    {
        Console.WriteLine($"[SqlServer] {operationName}: {command.CommandText}");
    }
}

[SqlDefine(SqlDefineTypes.SqlServer)]
[TableName("logs")]
[RepositoryFor(typeof(ILogRepository))]
public partial class SqlServerLogRepository(DbConnection connection) : ILogRepository
{
    partial void OnExecuting(string operationName, DbCommand command)
    {
        Console.WriteLine($"[SqlServer] {operationName}: {command.CommandText}");
    }
}

[SqlDefine(SqlDefineTypes.SqlServer)]
[TableName("categories")]
[RepositoryFor(typeof(ICategoryRepository))]
public partial class SqlServerCategoryRepository(DbConnection connection) : ICategoryRepository
{
    partial void OnExecuting(string operationName, DbCommand command)
    {
        Console.WriteLine($"[SqlServer] {operationName}: {command.CommandText}");
    }
}
