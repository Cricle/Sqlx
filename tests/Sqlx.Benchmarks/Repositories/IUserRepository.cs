using Sqlx.Annotations;
using Sqlx.Benchmarks.Models;

namespace Sqlx.Benchmarks.Repositories;

/// <summary>
/// 用户Repository接口 - 定义数据访问方法
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// 根据ID查询单个用户（同步方法用于benchmark）
    /// </summary>
    [Sqlx("SELECT {{columns}} FROM users WHERE id = @id")]
    User? GetByIdSync(int id);

    /// <summary>
    /// 查询前N条记录（同步方法用于benchmark）
    /// </summary>
    [Sqlx("SELECT {{columns}} FROM users LIMIT @limit")]
    List<User> GetTopNSync(int limit);

    /// <summary>
    /// 查询所有用户（同步方法用于benchmark）
    /// </summary>
    [Sqlx("SELECT {{columns}} FROM users")]
    List<User> GetAllSync();

    /// <summary>
    /// 参数化查询 - 根据年龄和激活状态过滤（同步方法用于benchmark）
    /// </summary>
    [Sqlx("SELECT {{columns}} FROM users WHERE age > @minAge AND is_active = @isActive")]
    List<User> GetByAgeAndStatusSync(int minAge, int isActive);

    /// <summary>
    /// 插入用户（同步方法用于benchmark）
    /// </summary>
    [Sqlx("INSERT INTO users (name, email, age, salary, is_active, created_at) VALUES (@name, @email, @age, @salary, @isActive, @createdAt)")]
    int InsertSync(string name, string email, int age, decimal salary, int isActive, string createdAt);

    /// <summary>
    /// 更新用户（同步方法用于benchmark）
    /// </summary>
    [Sqlx("UPDATE users SET name = @name, email = @email, age = @age, salary = @salary, updated_at = @updatedAt WHERE id = @id")]
    int UpdateSync(int id, string name, string email, int age, decimal salary, string updatedAt);

    /// <summary>
    /// 删除用户（同步方法用于benchmark）
    /// </summary>
    [Sqlx("DELETE FROM users WHERE id = @id")]
    int DeleteSync(int id);

    /// <summary>
    /// 复杂查询 - JOIN、聚合、排序（同步方法用于benchmark）
    /// </summary>
    [Sqlx(@"
        SELECT id, name, email, age, salary, is_active, created_at, updated_at 
        FROM users 
        WHERE is_active = 1 AND age BETWEEN @minAge AND @maxAge
        ORDER BY salary DESC, name ASC
        LIMIT @limit")]
    List<User> ComplexQuerySync(int minAge, int maxAge, int limit);
}

