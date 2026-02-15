// SqlBuilder 使用示例
// 展示如何使用 SqlBuilder 构建动态 SQL 查询

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Threading.Tasks;
using Sqlx;

namespace Sqlx.Samples;

/// <summary>
/// SqlBuilder 示例程序
/// </summary>
public class SqlBuilderExample
{
    public static async Task Main(string[] args)
    {
        // 创建内存数据库
        await using var connection = new SQLiteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        // 创建测试表
        await CreateTestTableAsync(connection);
        await InsertTestDataAsync(connection);

        Console.WriteLine("=== SqlBuilder 示例 ===\n");

        // 示例 1: 基本查询
        await Example1_BasicQuery(connection);

        // 示例 2: 动态条件
        await Example2_DynamicConditions(connection);

        // 示例 3: SqlTemplate 集成
        await Example3_SqlTemplateIntegration(connection);

        // 示例 4: 子查询
        await Example4_Subquery(connection);

        // 示例 5: 复杂查询
        await Example5_ComplexQuery(connection);

        Console.WriteLine("\n按任意键退出...");
        Console.ReadKey();
    }

    /// <summary>
    /// 示例 1: 基本查询 - 自动参数化
    /// </summary>
    static async Task Example1_BasicQuery(SQLiteConnection connection)
    {
        Console.WriteLine("示例 1: 基本查询");
        Console.WriteLine("----------------");

        using var builder = new SqlBuilder(SqlDefine.SQLite);
        builder.Append($"SELECT * FROM users WHERE age >= {18} AND name = {"John"}");
        
        var template = builder.Build();
        
        Console.WriteLine($"SQL: {template.Sql}");
        Console.WriteLine("参数:");
        foreach (var (key, value) in template.Parameters)
        {
            Console.WriteLine($"  {key} = {value}");
        }
        Console.WriteLine();
    }

    /// <summary>
    /// 示例 2: 动态条件 - 根据输入动态构建 WHERE 子句
    /// </summary>
    static async Task Example2_DynamicConditions(SQLiteConnection connection)
    {
        Console.WriteLine("示例 2: 动态条件");
        Console.WriteLine("----------------");

        // 模拟用户输入
        string? nameFilter = "John";
        int? minAge = 25;
        bool? isActive = true;

        using var builder = new SqlBuilder(SqlDefine.SQLite);
        builder.Append($"SELECT * FROM users WHERE 1=1");

        if (!string.IsNullOrEmpty(nameFilter))
        {
            builder.Append($" AND name LIKE {"%" + nameFilter + "%"}");
        }

        if (minAge.HasValue)
        {
            builder.Append($" AND age >= {minAge.Value}");
        }

        if (isActive.HasValue)
        {
            builder.Append($" AND is_active = {isActive.Value ? 1 : 0}");
        }

        var template = builder.Build();
        
        Console.WriteLine($"SQL: {template.Sql}");
        Console.WriteLine("参数:");
        foreach (var (key, value) in template.Parameters)
        {
            Console.WriteLine($"  {key} = {value}");
        }
        Console.WriteLine();
    }

    /// <summary>
    /// 示例 3: SqlTemplate 集成 - 使用占位符
    /// </summary>
    static async Task Example3_SqlTemplateIntegration(SQLiteConnection connection)
    {
        Console.WriteLine("示例 3: SqlTemplate 集成");
        Console.WriteLine("----------------------");

        var columns = new[]
        {
            new ColumnMeta("id", "id", DbType.Int64, false),
            new ColumnMeta("name", "name", DbType.String, false),
            new ColumnMeta("age", "age", DbType.Int32, false),
            new ColumnMeta("is_active", "is_active", DbType.Boolean, false)
        };

        var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);
        using var builder = new SqlBuilder(context);

        builder.AppendTemplate(
            "SELECT {{columns}} FROM {{table}} WHERE age >= @minAge AND is_active = @isActive",
            new { minAge = 18, isActive = 1 }
        );

        var template = builder.Build();
        
        Console.WriteLine($"SQL: {template.Sql}");
        Console.WriteLine("参数:");
        foreach (var (key, value) in template.Parameters)
        {
            Console.WriteLine($"  {key} = {value}");
        }
        Console.WriteLine();
    }

    /// <summary>
    /// 示例 4: 子查询 - 嵌套查询构建
    /// </summary>
    static async Task Example4_Subquery(SQLiteConnection connection)
    {
        Console.WriteLine("示例 4: 子查询");
        Console.WriteLine("-------------");

        // 创建子查询
        using var subquery = new SqlBuilder(SqlDefine.SQLite);
        subquery.Append($"SELECT user_id FROM orders WHERE total > {1000}");

        // 创建主查询
        using var mainQuery = new SqlBuilder(SqlDefine.SQLite);
        mainQuery.Append($"SELECT * FROM users WHERE age >= {18} AND id IN ");
        mainQuery.AppendSubquery(subquery);

        var template = mainQuery.Build();
        
        Console.WriteLine($"SQL: {template.Sql}");
        Console.WriteLine("参数:");
        foreach (var (key, value) in template.Parameters)
        {
            Console.WriteLine($"  {key} = {value}");
        }
        Console.WriteLine();
    }

    /// <summary>
    /// 示例 5: 复杂查询 - 组合多种技术
    /// </summary>
    static async Task Example5_ComplexQuery(SQLiteConnection connection)
    {
        Console.WriteLine("示例 5: 复杂查询");
        Console.WriteLine("---------------");

        var columns = new[]
        {
            new ColumnMeta("id", "id", DbType.Int64, false),
            new ColumnMeta("name", "name", DbType.String, false),
            new ColumnMeta("age", "age", DbType.Int32, false)
        };

        var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);
        using var builder = new SqlBuilder(context);

        // 基础查询
        builder.AppendTemplate("SELECT {{columns}} FROM {{table}} WHERE 1=1");

        // 动态条件
        string? nameFilter = "John";
        int? minAge = 25;
        string? orderBy = "age";
        int pageSize = 10;
        int pageNumber = 1;

        if (!string.IsNullOrEmpty(nameFilter))
        {
            builder.Append($" AND name LIKE {"%" + nameFilter + "%"}");
        }

        if (minAge.HasValue)
        {
            builder.Append($" AND age >= {minAge.Value}");
        }

        // 排序
        if (!string.IsNullOrEmpty(orderBy))
        {
            var column = SqlDefine.SQLite.WrapColumn(orderBy);
            builder.AppendRaw($" ORDER BY {column}");
        }

        // 分页
        var offset = (pageNumber - 1) * pageSize;
        builder.Append($" LIMIT {pageSize} OFFSET {offset}");

        var template = builder.Build();
        
        Console.WriteLine($"SQL: {template.Sql}");
        Console.WriteLine("参数:");
        foreach (var (key, value) in template.Parameters)
        {
            Console.WriteLine($"  {key} = {value}");
        }
        Console.WriteLine();
    }

    /// <summary>
    /// 创建测试表
    /// </summary>
    static async Task CreateTestTableAsync(SQLiteConnection connection)
    {
        var sql = @"
            CREATE TABLE users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                age INTEGER NOT NULL,
                is_active INTEGER NOT NULL DEFAULT 1
            );

            CREATE TABLE orders (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                user_id INTEGER NOT NULL,
                total REAL NOT NULL,
                FOREIGN KEY (user_id) REFERENCES users(id)
            );
        ";

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// 插入测试数据
    /// </summary>
    static async Task InsertTestDataAsync(SQLiteConnection connection)
    {
        var sql = @"
            INSERT INTO users (name, age, is_active) VALUES
            ('John', 30, 1),
            ('Jane', 25, 1),
            ('Bob', 35, 0),
            ('Alice', 28, 1);

            INSERT INTO orders (user_id, total) VALUES
            (1, 1500.00),
            (1, 800.00),
            (2, 1200.00),
            (4, 2000.00);
        ";

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }
}
