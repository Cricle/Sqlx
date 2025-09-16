using Microsoft.Data.Sqlite;
using SqlxDemo.Models;
using Sqlx.Annotations;

namespace SqlxDemo.Services;

/// <summary>
/// 用户服务接口 - 用于演示源生成功能
/// </summary>
public interface IUserService
{
    /// <summary>
    /// 获取所有活跃用户
    /// </summary>
    Task<IList<User>> GetActiveUsersAsync();

    /// <summary>
    /// 根据ID获取用户
    /// </summary>
    [Sqlx("SELECT * FROM [User] WHERE [Id] = @id")]
    Task<User?> GetUserByIdAsync(int id);

    /// <summary>
    /// 根据年龄范围获取用户
    /// </summary>
    [Sqlx("SELECT * FROM [User] WHERE [Age] BETWEEN @minAge AND @maxAge")]
    Task<IList<User>> GetUsersByAgeRangeAsync(int minAge, int maxAge);

    /// <summary>
    /// 根据部门获取用户数量
    /// </summary>
    [Sqlx("SELECT COUNT(*) FROM [User] WHERE [DepartmentId] = @deptId")]
    Task<int> GetUserCountByDepartmentAsync(int deptId);
}

