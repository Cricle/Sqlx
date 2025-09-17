using SqlxDemo.Models;
using Sqlx.Annotations;
using System.Data.Common;

namespace SqlxDemo.Services;

/// <summary>
/// 简单的用户仓储接口 - 实现类使用partial方法模式
/// </summary>
public interface ISimpleUserRepository
{
    Task<User> GetUserByIdAsync(int id);
    Task<List<User>> GetAllUsersAsync();
    Task<int> GetUserCountAsync();
}

/// <summary>
/// 简单的用户仓储实现 - 使用partial方法模式
/// 所有方法实现由Sqlx源生成器自动生成
/// </summary>
public partial class SimpleUserRepository : ISimpleUserRepository
{
    private readonly DbConnection _connection;

    public SimpleUserRepository(DbConnection connection)
    {
        _connection = connection;
    }

    // 使用partial方法模式，确保源生成器能正确生成实现
    [Sqlx("SELECT * FROM [user] WHERE [id] = @id")]
    public partial Task<User> GetUserByIdAsync(int id);
    
    [Sqlx("SELECT * FROM [user] ORDER BY [name]")]
    public partial Task<List<User>> GetAllUsersAsync();
    
    [Sqlx("SELECT COUNT(*) FROM [user]")]
    public partial Task<int> GetUserCountAsync();
}

/// <summary>
/// 简单的产品仓储接口 - 实现类使用partial方法模式
/// </summary>
public interface ISimpleProductRepository
{
    Task<Product> GetProductByIdAsync(int id);
    Task<List<Product>> GetActiveProductsAsync();
    Task<int> GetProductCountAsync();
}

/// <summary>
/// 简单的产品仓储实现 - 使用partial方法模式
/// 所有方法实现由Sqlx源生成器自动生成
/// </summary>
public partial class SimpleProductRepository : ISimpleProductRepository
{
    private readonly DbConnection _connection;

    public SimpleProductRepository(DbConnection connection)
    {
        _connection = connection;
    }

    // 使用partial方法模式，确保源生成器能正确生成实现
    [Sqlx("SELECT * FROM [product] WHERE [id] = @id")]
    public partial Task<Product> GetProductByIdAsync(int id);
    
    [Sqlx("SELECT * FROM [product] WHERE [is_active] = 1 ORDER BY [name]")]
    public partial Task<List<Product>> GetActiveProductsAsync();
    
    [Sqlx("SELECT COUNT(*) FROM [product] WHERE [is_active] = 1")]
    public partial Task<int> GetProductCountAsync();
}
