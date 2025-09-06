// 测试 RepositoryFor 源生成功能
using System;
using System.Linq;
using Microsoft.Data.Sqlite;

class Program
{
    static void Main()
    {
        Console.WriteLine("=== RepositoryFor 源生成功能测试 ===\n");
        
        try
        {
            // 创建内存数据库连接用于测试
            using var connection = new SqliteConnection("Data Source=:memory:");
            
            // 测试我们生成的仓储是否存在和工作
            var repo = new Sqlx.BasicExample.SimpleTestRepository(connection);
            Console.WriteLine("✅ SimpleTestRepository 类生成成功");
            
            // 检查是否实现了接口
            if (repo is Sqlx.BasicExample.ITestService testService)
            {
                Console.WriteLine("✅ 正确实现了 ITestService 接口");
                
                // 测试方法是否存在（通过接口调用）
                try
                {
                    var results = testService.GetAll();
                    Console.WriteLine($"✅ GetAll() 方法可调用，返回类型: {results?.GetType().Name ?? "null"}");
                }
                catch (NotImplementedException)
                {
                    Console.WriteLine("⚠️ GetAll() 方法存在但尚未实现（抛出 NotImplementedException）");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ GetAll() 方法调用失败: {ex.Message}");
                }
                
                try
                {
                    var entity = new Sqlx.BasicExample.TestEntity { Id = 1, Name = "Test" };
                    var result = testService.Create(entity);
                    Console.WriteLine($"✅ Create() 方法可调用，返回值: {result}");
                }
                catch (NotImplementedException)
                {
                    Console.WriteLine("⚠️ Create() 方法存在但尚未实现（抛出 NotImplementedException）");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Create() 方法调用失败: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("❌ 未实现 ITestService 接口");
                Console.WriteLine($"实际类型: {repo.GetType().FullName}");
                Console.WriteLine($"基类型: {repo.GetType().BaseType?.FullName}");
                Console.WriteLine($"实现的接口: {string.Join(", ", repo.GetType().GetInterfaces().Select(i => i.Name))}");
            }
            
            Console.WriteLine("\n测试完成");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 错误: {ex.Message}");
        }
    }
}