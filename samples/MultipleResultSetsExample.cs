// <copyright file="MultipleResultSetsExample.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using System;
using System.Data.Common;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Sqlx;
using Sqlx.Annotations;

namespace Samples;

/// <summary>
/// 演示多结果集功能的示例。
/// 展示如何使用 ResultSetMapping 特性从单个 SQL 调用中返回多个标量值。
/// </summary>
public class MultipleResultSetsExample
{
    /// <summary>
    /// 用户仓储接口，演示多结果集返回。
    /// </summary>
    public interface IUserRepository
    {
        /// <summary>
        /// 插入用户并返回统计信息（使用 ResultSetMapping 特性明确指定映射）。
        /// </summary>
        /// <param name="name">用户名</param>
        /// <returns>元组：(受影响行数, 用户ID, 总用户数)</returns>
        [SqlTemplate(@"
            INSERT INTO users (name) VALUES (@name);
            SELECT last_insert_rowid();
            SELECT COUNT(*) FROM users
        ")]
        [ResultSetMapping(0, "rowsAffected")]
        [ResultSetMapping(1, "userId")]
        [ResultSetMapping(2, "totalUsers")]
        Task<(int rowsAffected, long userId, int totalUsers)> InsertAndGetStatsAsync(string name);

        /// <summary>
        /// 插入用户并返回统计信息（不使用 ResultSetMapping，使用默认映射）。
        /// 默认映射：第1个元素=受影响行数，第2个元素=第1个SELECT，第3个元素=第2个SELECT。
        /// </summary>
        /// <param name="name">用户名</param>
        /// <returns>元组：(受影响行数, 用户ID, 总用户数)</returns>
        [SqlTemplate(@"
            INSERT INTO users (name) VALUES (@name);
            SELECT last_insert_rowid();
            SELECT COUNT(*) FROM users
        ")]
        Task<(int rowsAffected, long userId, int totalUsers)> InsertAndGetStatsDefaultAsync(string name);

        /// <summary>
        /// 获取用户统计信息（多个 SELECT 语句）。
        /// </summary>
        /// <returns>元组：(总用户数, 最大ID, 最小ID)</returns>
        [SqlTemplate(@"
            SELECT COUNT(*) FROM users;
            SELECT MAX(id) FROM users;
            SELECT MIN(id) FROM users
        ")]
        [ResultSetMapping(0, "totalUsers")]
        [ResultSetMapping(1, "maxId")]
        [ResultSetMapping(2, "minId")]
        Task<(int totalUsers, long maxId, long minId)> GetUserStatsAsync();

        /// <summary>
        /// 同步方法：插入用户并返回统计信息。
        /// </summary>
        /// <param name="name">用户名</param>
        /// <returns>元组：(受影响行数, 用户ID, 总用户数)</returns>
        [SqlTemplate(@"
            INSERT INTO users (name) VALUES (@name);
            SELECT last_insert_rowid();
            SELECT COUNT(*) FROM users
        ")]
        [ResultSetMapping(0, "rowsAffected")]
        [ResultSetMapping(1, "userId")]
        [ResultSetMapping(2, "totalUsers")]
        (int rowsAffected, long userId, int totalUsers) InsertAndGetStats(string name);
    }

    /// <summary>
    /// 用户仓储实现（由源生成器自动生成）。
    /// </summary>
    [RepositoryFor(typeof(IUserRepository), Dialect = SqlDefineTypes.SQLite, TableName = "users")]
    public partial class UserRepository : IUserRepository
    {
        private readonly DbConnection _connection;

        public UserRepository(DbConnection connection)
        {
            _connection = connection;
        }
    }

    /// <summary>
    /// 运行示例。
    /// </summary>
    public static async Task RunAsync()
    {
        Console.WriteLine("=== 多结果集示例 ===\n");

        // 创建内存数据库
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        // 创建表
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = @"
                CREATE TABLE users (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL
                )";
            await cmd.ExecuteNonQueryAsync();
        }

        var repo = new UserRepository(connection);

        // 示例 1: 使用 ResultSetMapping 特性
        Console.WriteLine("示例 1: 使用 ResultSetMapping 特性");
        var (rows1, userId1, total1) = await repo.InsertAndGetStatsAsync("Alice");
        Console.WriteLine($"  插入 Alice: 受影响行数={rows1}, 用户ID={userId1}, 总用户数={total1}");

        var (rows2, userId2, total2) = await repo.InsertAndGetStatsAsync("Bob");
        Console.WriteLine($"  插入 Bob: 受影响行数={rows2}, 用户ID={userId2}, 总用户数={total2}");
        Console.WriteLine();

        // 示例 2: 使用默认映射（不指定 ResultSetMapping）
        Console.WriteLine("示例 2: 使用默认映射");
        var (rows3, userId3, total3) = await repo.InsertAndGetStatsDefaultAsync("Charlie");
        Console.WriteLine($"  插入 Charlie: 受影响行数={rows3}, 用户ID={userId3}, 总用户数={total3}");
        Console.WriteLine();

        // 示例 3: 多个 SELECT 语句
        Console.WriteLine("示例 3: 多个 SELECT 语句");
        var (totalUsers, maxId, minId) = await repo.GetUserStatsAsync();
        Console.WriteLine($"  用户统计: 总数={totalUsers}, 最大ID={maxId}, 最小ID={minId}");
        Console.WriteLine();

        // 示例 4: 同步方法
        Console.WriteLine("示例 4: 同步方法");
        var (rows4, userId4, total4) = repo.InsertAndGetStats("David");
        Console.WriteLine($"  插入 David: 受影响行数={rows4}, 用户ID={userId4}, 总用户数={total4}");
        Console.WriteLine();

        Console.WriteLine("=== 示例完成 ===");
    }
}
