using SqlxDemo.Models;
using Sqlx.Annotations;
using System.Data.Common;

namespace SqlxDemo.Services;

/// <summary>
/// 简单的用户仓储接口 - 实现类使用partial方法模式
/// </summary>
public interface ISimpleUserRepository
{
    // 使用partial方法模式，确保源生成器能正确生成实现
    [Sqlx("SELECT * FROM [user] WHERE [id] = @id")]
    Task<User> GetUserByIdAsync(int id);

    [Sqlx("SELECT * FROM [user] ORDER BY [name]")]
    Task<List<User>> GetAllUsersAsync();

    [Sqlx("SELECT COUNT(*) FROM [user]")]
    Task<int> GetUserCountAsync();
}

/// <summary>
/// 简单的用户仓储实现 - 使用partial方法模式
/// 所有方法实现由Sqlx源生成器自动生成
/// </summary>
[RepositoryFor(typeof(ISimpleUserRepository))]
public partial class SimpleUserRepository(DbConnection connection);

/// <summary>
/// 简单的产品仓储接口 - 实现类使用partial方法模式
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
/// 简单的产品仓储实现 - 使用partial方法模式
/// 所有方法实现由Sqlx源生成器自动生成
/// </summary>
[RepositoryFor(typeof(ISimpleProductRepository))]
public partial class SimpleProductRepository(DbConnection connection);
