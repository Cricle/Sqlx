using Microsoft.Data.Sqlite;
using SqlxDemo.Models;
using Sqlx.Annotations;

namespace SqlxDemo.Services;

/// <summary>
/// 用户服务实现 - 使用Sqlx特性和partial方法进行源生成
/// 注意：这个类被标记为partial，源生成器会自动生成标记了[Sqlx]特性的partial方法的实现
/// </summary>
public partial class UserService(SqliteConnection connection) : IUserService
{
    // 实现接口方法，使用partial方法和Sqlx特性
    [Sqlx("SELECT * FROM [User] WHERE [IsActive] = 1")]
    public partial Task<IList<User>> GetActiveUsersAsync();
    
    [Sqlx("SELECT * FROM [User] WHERE [Id] = @id")]
    public partial Task<User?> GetUserByIdAsync(int id);
    
    [Sqlx("SELECT * FROM [User] WHERE [Age] BETWEEN @minAge AND @maxAge")]
    public partial Task<IList<User>> GetUsersByAgeRangeAsync(int minAge, int maxAge);
    
    [Sqlx("SELECT COUNT(*) FROM [User] WHERE [DepartmentId] = @deptId")]
    public partial Task<int> GetUserCountByDepartmentAsync(int deptId);
}
