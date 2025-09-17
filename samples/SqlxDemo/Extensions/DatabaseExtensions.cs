using Microsoft.Data.Sqlite;
using SqlxDemo.Models;
using Sqlx.Annotations;
using System.Data.Common;

namespace SqlxDemo.Extensions;

/// <summary>
/// 数据库连接扩展方法 - 演示源生成的扩展方法
/// </summary>
public static partial class DatabaseExtensions
{
    /// <summary>
    /// 获取活跃用户数量
    /// </summary>
    [Sqlx("SELECT COUNT(*) FROM [user] WHERE [is_active] = 1")]
    public static partial Task<int> GetActiveUserCountAsync(this SqliteConnection connection);

    /// <summary>
    /// 获取平均薪资
    /// </summary>
    [Sqlx("SELECT AVG([salary]) FROM [user] WHERE [is_active] = 1")]
    public static partial Task<decimal> GetAverageSalaryAsync(this SqliteConnection connection);

    /// <summary>
    /// 获取高绩效员工
    /// </summary>
    [Sqlx("SELECT * FROM [user] WHERE [performance_rating] >= @min_rating AND [is_active] = 1")]
    public static partial Task<IList<User>> GetTopPerformersAsync(this SqliteConnection connection, double minRating);

    /// <summary>
    /// 简单的数据库操作扩展
    /// </summary>
    public static async Task<int> ExecuteNonQueryAsync(this DbConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return await command.ExecuteNonQueryAsync();
    }
}

