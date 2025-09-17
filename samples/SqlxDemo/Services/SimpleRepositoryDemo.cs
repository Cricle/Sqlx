using SqlxDemo.Models;
using Sqlx.Annotations;
using System.Data.Common;

namespace SqlxDemo.Services;

/// <summary>
/// 简单的RepositoryFor演示接口 - 接口中定义SQL，实现类自动生成
/// </summary>
public interface ISimpleUserRepository
{
    [Sqlx("SELECT * FROM [user] WHERE [id] = @id")]
    Task<User> GetUserByIdAsync(int id);
    
    [Sqlx("SELECT * FROM [user] ORDER BY [name]")]
    Task<List<User>> GetAllUsersAsync();
    
    [Sqlx("SELECT COUNT(*) FROM [user]")]
    Task<int> GetUserCountAsync();
}

/// <summary>
/// 简单的用户仓储实现 - 演示RepositoryFor特性
/// 所有方法实现由Sqlx源生成器自动生成
/// </summary>
[RepositoryFor(typeof(ISimpleUserRepository))]
public partial class SimpleUserRepository : ISimpleUserRepository
{
    private readonly DbConnection _connection;

    public SimpleUserRepository(DbConnection connection)
    {
        _connection = connection;
    }

    // 方法实现由RepositoryFor特性的源生成器自动生成
    // 无需手动声明partial方法
}

/// <summary>
/// 简单的产品仓储接口 - 接口中定义SQL，实现类自动生成
/// </summary>
public interface ISimpleProductRepository
{
    [Sqlx("SELECT * FROM [product] WHERE [id] = @id")]
    Task<Product> GetProductByIdAsync(int id);
    
    [Sqlx("SELECT * FROM [product] WHERE [is_active] = 1 ORDER BY [name]")]
    Task<List<Product>> GetActiveProductsAsync();
    
    [Sqlx("SELECT COUNT(*) FROM [product] WHERE [is_active] = 1")]
    Task<int> GetProductCountAsync();
}

/// <summary>
/// 简单的产品仓储实现 - 演示RepositoryFor特性
/// 所有方法实现由Sqlx源生成器自动生成
/// </summary>
[RepositoryFor(typeof(ISimpleProductRepository))]
public partial class SimpleProductRepository : ISimpleProductRepository
{
    private readonly DbConnection _connection;

    public SimpleProductRepository(DbConnection connection)
    {
        _connection = connection;
    }

    // 方法实现由RepositoryFor特性的源生成器自动生成
    // 无需手动声明partial方法
}
