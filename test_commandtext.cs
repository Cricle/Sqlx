// 测试生成代码的 CommandText
using System;
using System.Data;
using Microsoft.Data.Sqlite;
using TodoWebApi.Services;
using TodoWebApi.Models;
using System.Diagnostics;
using System.Threading.Tasks;

// 自定义拦截器用于检查 CommandText
public sealed class CommandTextCheckInterceptor : Sqlx.Interceptors.ISqlxInterceptor
{
    public void OnExecuting(ref Sqlx.Interceptors.SqlxExecutionContext context)
    {
        Console.WriteLine($"=== OnExecuting ===");
        Console.WriteLine($"OperationName: {context.OperationName}");
        Console.WriteLine($"RepositoryType: {context.RepositoryType}");
        Console.WriteLine($"SQL: [{context.Sql}]");
        Console.WriteLine($"SQL Length: {context.Sql.Length}");
        Console.WriteLine($"SQL IsNullOrEmpty: {string.IsNullOrEmpty(context.Sql)}");
        Console.WriteLine($"SQL IsNullOrWhiteSpace: {string.IsNullOrWhiteSpace(context.Sql)}");
        Console.WriteLine("==================");
    }

    public void OnExecuted(ref Sqlx.Interceptors.SqlxExecutionContext context)
    {
        Console.WriteLine($"✅ OnExecuted: {context.OperationName} ({context.ElapsedMilliseconds:F2}ms)");
    }

    public void OnFailed(ref Sqlx.Interceptors.SqlxExecutionContext context)
    {
        Console.WriteLine($"❌ OnFailed: {context.OperationName} - {context.Exception?.Message}");
    }
}

class Program
{
    static async Task Main()
    {
        Console.WriteLine("测试 Sqlx 生成代码的 CommandText\n");
        
        // 注册拦截器
        Sqlx.Interceptors.SqlxInterceptors.Add(new CommandTextCheckInterceptor());
        
        // 创建数据库连接
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        
        // 初始化数据库
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = @"
                CREATE TABLE todos (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    title TEXT NOT NULL,
                    description TEXT,
                    is_completed INTEGER NOT NULL DEFAULT 0,
                    priority INTEGER NOT NULL DEFAULT 1,
                    due_date TEXT,
                    created_at TEXT NOT NULL,
                    updated_at TEXT NOT NULL,
                    completed_at TEXT,
                    tags TEXT,
                    estimated_minutes INTEGER,
                    actual_minutes INTEGER
                );";
            await cmd.ExecuteNonQueryAsync();
            Console.WriteLine("✅ 数据库表创建成功\n");
        }
        
        // 创建 TodoService
        var todoService = new TodoService(connection);
        
        // 测试各个方法
        Console.WriteLine("\n=== 测试 1: GetAllAsync ===");
        try
        {
            var allTodos = await todoService.GetAllAsync();
            Console.WriteLine($"✅ GetAllAsync 成功，返回 {allTodos.Count} 个项目\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ GetAllAsync 失败: {ex.Message}\n");
        }
        
        Console.WriteLine("\n=== 测试 2: CreateAsync ===");
        try
        {
            var newTodo = new Todo
            {
                Title = "测试任务",
                Description = "测试描述",
                IsCompleted = false,
                Priority = 1,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
            var newId = await todoService.CreateAsync(newTodo);
            Console.WriteLine($"✅ CreateAsync 成功，新ID: {newId}\n");
            
            Console.WriteLine("\n=== 测试 3: GetByIdAsync ===");
            var fetchedTodo = await todoService.GetByIdAsync(newId);
            Console.WriteLine($"✅ GetByIdAsync 成功: {fetchedTodo?.Title}\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 操作失败: {ex.Message}\n");
            Console.WriteLine($"StackTrace: {ex.StackTrace}\n");
        }
        
        Console.WriteLine("\n=== 测试完成 ===");
    }
}


