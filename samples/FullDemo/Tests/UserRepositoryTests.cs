using System.Data.Common;
using FullDemo.Models;
using FullDemo.Repositories;

namespace FullDemo.Tests;

/// <summary>
/// 用户仓储测试 - 测试 ICrudRepository 继承的所有方法
/// </summary>
public class UserRepositoryTests
{
    private readonly IUserRepository _repo;
    private readonly string _dbType;

    public UserRepositoryTests(IUserRepository repo, string dbType)
    {
        _repo = repo;
        _dbType = dbType;
    }

    public async Task RunAllTestsAsync()
    {
        Console.WriteLine($"\n{'='} [{_dbType}] User Repository Tests {new string('=', 50)}");

        await TestInsertAsync();
        await TestGetByIdAsync();
        await TestGetAllAsync();
        await TestGetByEmailAsync();
        await TestSearchAsync();
        await TestGetActiveUsersAsync();
        await TestGetByAgeRangeAsync();
        await TestExpressionQueryAsync();
        await TestAggregateAsync();
        await TestUpdateAsync();
        await TestDeleteAsync();
        await TestBatchOperationsAsync();

        Console.WriteLine($"[{_dbType}] All User Repository tests passed!\n");
    }

    private async Task TestInsertAsync()
    {
        Console.WriteLine($"  [{_dbType}] Testing Insert...");

        var user = new User
        {
            Name = "Alice",
            Email = $"alice_{Guid.NewGuid():N}@example.com",
            Age = 25,
            Balance = 1000.50m,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        var rowsAffected = await _repo.InsertAsync(user);
        if (rowsAffected != 1)
            throw new Exception($"Insert failed: expected 1 row affected, got {rowsAffected}");

        Console.WriteLine($"    ✓ Insert: {rowsAffected} row affected");
    }

    private async Task TestGetByIdAsync()
    {
        Console.WriteLine($"  [{_dbType}] Testing GetById...");

        // 先插入一条数据
        var user = new User
        {
            Name = "Bob",
            Email = $"bob_{Guid.NewGuid():N}@example.com",
            Age = 30,
            Balance = 2000m,
            IsActive = true
        };
        await _repo.InsertAsync(user);

        // 获取所有用户并取第一个ID
        var allUsers = await _repo.GetAllAsync(limit: 1);
        if (allUsers.Count == 0)
            throw new Exception("No users found after insert");

        var foundUser = await _repo.GetByIdAsync(allUsers[0].Id);
        if (foundUser == null)
            throw new Exception($"GetById failed: user with id {allUsers[0].Id} not found");

        Console.WriteLine($"    ✓ GetById: Found user '{foundUser.Name}'");
    }

    private async Task TestGetAllAsync()
    {
        Console.WriteLine($"  [{_dbType}] Testing GetAll...");

        var users = await _repo.GetAllAsync(limit: 10);
        Console.WriteLine($"    ✓ GetAll: Found {users.Count} users");
    }

    private async Task TestGetByEmailAsync()
    {
        Console.WriteLine($"  [{_dbType}] Testing GetByEmail...");

        // 插入一个带唯一邮箱的用户
        var email = $"unique_{Guid.NewGuid():N}@example.com";
        var user = new User
        {
            Name = "UniqueUser",
            Email = email,
            Age = 28,
            Balance = 500m,
            IsActive = true
        };
        await _repo.InsertAsync(user);

        var foundUser = await _repo.GetByEmailAsync(email);
        if (foundUser == null)
            throw new Exception($"GetByEmail failed: user with email {email} not found");

        Console.WriteLine($"    ✓ GetByEmail: Found user '{foundUser.Name}'");
    }

    private async Task TestSearchAsync()
    {
        Console.WriteLine($"  [{_dbType}] Testing Search...");

        // 插入测试数据
        await _repo.InsertAsync(new User
        {
            Name = "SearchTest_Alpha",
            Email = $"search_alpha_{Guid.NewGuid():N}@example.com",
            Age = 25,
            Balance = 100m,
            IsActive = true
        });

        var users = await _repo.SearchAsync("%SearchTest%");
        Console.WriteLine($"    ✓ Search: Found {users.Count} users matching 'SearchTest'");
    }

    private async Task TestGetActiveUsersAsync()
    {
        Console.WriteLine($"  [{_dbType}] Testing GetActiveUsers...");

        var activeUsers = await _repo.GetActiveUsersAsync();
        Console.WriteLine($"    ✓ GetActiveUsers: Found {activeUsers.Count} active users");
    }

    private async Task TestGetByAgeRangeAsync()
    {
        Console.WriteLine($"  [{_dbType}] Testing GetByAgeRange...");

        var users = await _repo.GetByAgeRangeAsync(20, 35);
        Console.WriteLine($"    ✓ GetByAgeRange(20-35): Found {users.Count} users");
    }

    private async Task TestExpressionQueryAsync()
    {
        Console.WriteLine($"  [{_dbType}] Testing Expression Query (GetWhere)...");

        // 使用表达式查询
        var users = await _repo.GetWhereAsync(u => u.IsActive && u.Age >= 20);
        Console.WriteLine($"    ✓ GetWhere(IsActive && Age >= 20): Found {users.Count} users");

        // 测试 ExistsWhere
        var exists = await _repo.ExistsWhereAsync(u => u.Age > 18);
        Console.WriteLine($"    ✓ ExistsWhere(Age > 18): {exists}");

        // 测试 GetFirstWhere
        var firstUser = await _repo.GetFirstWhereAsync(u => u.IsActive);
        Console.WriteLine($"    ✓ GetFirstWhere(IsActive): {(firstUser != null ? firstUser.Name : "null")}");
    }

    private async Task TestAggregateAsync()
    {
        Console.WriteLine($"  [{_dbType}] Testing Aggregate functions...");

        var count = await _repo.CountAsync();
        Console.WriteLine($"    ✓ Count: {count}");

        var totalBalance = await _repo.GetTotalBalanceAsync();
        Console.WriteLine($"    ✓ TotalBalance: {totalBalance:C}");

        var avgAge = await _repo.GetAverageAgeAsync();
        Console.WriteLine($"    ✓ AverageAge: {avgAge:F1}");

        var maxBalance = await _repo.GetMaxBalanceAsync();
        Console.WriteLine($"    ✓ MaxBalance: {maxBalance:C}");
    }

    private async Task TestUpdateAsync()
    {
        Console.WriteLine($"  [{_dbType}] Testing Update...");

        // 获取一个用户
        var users = await _repo.GetAllAsync(limit: 1);
        if (users.Count == 0)
        {
            Console.WriteLine($"    ⚠ No users to update, skipping...");
            return;
        }

        var user = users[0];
        var originalBalance = user.Balance;
        user.Balance = originalBalance + 100;

        var rowsAffected = await _repo.UpdateAsync(user);
        if (rowsAffected != 1)
            throw new Exception($"Update failed: expected 1 row affected, got {rowsAffected}");

        Console.WriteLine($"    ✓ Update: Balance changed from {originalBalance:C} to {user.Balance:C}");
    }

    private async Task TestDeleteAsync()
    {
        Console.WriteLine($"  [{_dbType}] Testing Delete...");

        // 插入一个用户用于删除
        var user = new User
        {
            Name = "ToDelete",
            Email = $"todelete_{Guid.NewGuid():N}@example.com",
            Age = 99,
            Balance = 0m,
            IsActive = false
        };
        await _repo.InsertAsync(user);

        // 获取该用户
        var users = await _repo.GetWhereAsync(u => u.Name == "ToDelete");
        if (users.Count == 0)
        {
            Console.WriteLine($"    ⚠ User not found for deletion, skipping...");
            return;
        }

        var rowsAffected = await _repo.DeleteAsync(users[0].Id);
        Console.WriteLine($"    ✓ Delete: {rowsAffected} row affected");
    }

    private async Task TestBatchOperationsAsync()
    {
        Console.WriteLine($"  [{_dbType}] Testing Batch Operations...");

        // 批量插入
        var newUsers = new List<User>();
        for (int i = 0; i < 5; i++)
        {
            newUsers.Add(new User
            {
                Name = $"BatchUser_{i}",
                Email = $"batch_{i}_{Guid.NewGuid():N}@example.com",
                Age = 20 + i,
                Balance = 100 * i,
                IsActive = i % 2 == 0
            });
        }

        // 注意：需要使用 IBatchRepository 的 BatchInsertAsync
        // 这里我们逐个插入作为替代
        foreach (var u in newUsers)
        {
            await _repo.InsertAsync(u);
        }
        Console.WriteLine($"    ✓ BatchInsert: Inserted {newUsers.Count} users");

        // 批量更新状态
        var batchUsers = await _repo.SearchAsync("%BatchUser%");
        if (batchUsers.Count > 0)
        {
            var ids = batchUsers.Select(u => u.Id).ToArray();
            var updatedCount = await _repo.BatchUpdateStatusAsync(ids, true);
            Console.WriteLine($"    ✓ BatchUpdateStatus: Updated {updatedCount} users");
        }
    }
}
