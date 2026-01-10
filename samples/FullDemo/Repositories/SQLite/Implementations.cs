using System.Data.Common;
using Sqlx;
using Sqlx.Annotations;
using FullDemo.Models;

namespace FullDemo.Repositories.SQLite;

// ==================== SQLite 仓储实现 ====================

[SqlDefine(SqlDefineTypes.SQLite)]
[TableName("users")]
[RepositoryFor(typeof(IUserRepository))]
public partial class SQLiteUserRepository(DbConnection connection) : IUserRepository
{
    partial void OnExecuting(string operationName, DbCommand command)
    {
        Console.WriteLine($"[SQLite] {operationName}: {command.CommandText}");
    }
}

[SqlDefine(SqlDefineTypes.SQLite)]
[TableName("products")]
[RepositoryFor(typeof(IProductRepository))]
public partial class SQLiteProductRepository(DbConnection connection) : IProductRepository
{
    partial void OnExecuting(string operationName, DbCommand command)
    {
        Console.WriteLine($"[SQLite] {operationName}: {command.CommandText}");
    }
}

[SqlDefine(SqlDefineTypes.SQLite)]
[TableName("orders")]
[RepositoryFor(typeof(IOrderRepository))]
public partial class SQLiteOrderRepository(DbConnection connection) : IOrderRepository
{
    partial void OnExecuting(string operationName, DbCommand command)
    {
        Console.WriteLine($"[SQLite] {operationName}: {command.CommandText}");
    }
}

[SqlDefine(SqlDefineTypes.SQLite)]
[TableName("accounts")]
[RepositoryFor(typeof(IAccountRepository))]
public partial class SQLiteAccountRepository(DbConnection connection) : IAccountRepository
{
    partial void OnExecuting(string operationName, DbCommand command)
    {
        Console.WriteLine($"[SQLite] {operationName}: {command.CommandText}");
    }
}

[SqlDefine(SqlDefineTypes.SQLite)]
[TableName("logs")]
[RepositoryFor(typeof(ILogRepository))]
public partial class SQLiteLogRepository(DbConnection connection) : ILogRepository
{
    partial void OnExecuting(string operationName, DbCommand command)
    {
        Console.WriteLine($"[SQLite] {operationName}: {command.CommandText}");
    }
}

[SqlDefine(SqlDefineTypes.SQLite)]
[TableName("categories")]
[RepositoryFor(typeof(ICategoryRepository))]
public partial class SQLiteCategoryRepository(DbConnection connection) : ICategoryRepository
{
    partial void OnExecuting(string operationName, DbCommand command)
    {
        Console.WriteLine($"[SQLite] {operationName}: {command.CommandText}");
    }
}
