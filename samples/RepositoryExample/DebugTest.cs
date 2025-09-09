using System;
using System.Linq;
using Microsoft.Data.Sqlite;

namespace Sqlx.RepositoryExample;

/// <summary>
/// 调试测试 - 验证SQLiteUserRepository是否真的有方法实现
/// </summary>
public static class DebugTest
{
    /// <summary>
    /// 测试Repository方法
    /// </summary>
    public static void TestRepository()
    {
        try
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            
            var repository = new SQLiteUserRepository(connection);
            
            // 尝试调用方法 - 如果编译通过说明方法存在
            Console.WriteLine("Testing repository methods...");
            
            // 检查类型信息
            var type = repository.GetType();
            Console.WriteLine($"Repository type: {type.FullName}");
            Console.WriteLine($"Base type: {type.BaseType?.FullName}");
            Console.WriteLine($"Interfaces: {string.Join(", ", type.GetInterfaces().Select(i => i.Name))}");
            Console.WriteLine($"Methods: {string.Join(", ", type.GetMethods().Where(m => m.DeclaringType == type).Select(m => m.Name))}");
            
            // 这里应该能看到方法是否真的存在
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
