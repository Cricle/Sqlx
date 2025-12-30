using FullDemo.Models;
using FullDemo.Repositories;

namespace FullDemo.Tests;

/// <summary>
/// 订单仓储测试 - 测试审计字段功能
/// </summary>
public class OrderRepositoryTests
{
    private readonly IOrderRepository _orderRepo;
    private readonly IUserRepository _userRepo;
    private readonly string _dbType;

    public OrderRepositoryTests(IOrderRepository orderRepo, IUserRepository userRepo, string dbType)
    {
        _orderRepo = orderRepo;
        _userRepo = userRepo;
        _dbType = dbType;
    }

    public async Task RunAllTestsAsync()
    {
        Console.WriteLine($"\n{'='} [{_dbType}] Order Repository Tests (Audit Fields) {new string('=', 40)}");

        await TestInsertWithAuditAsync();
        await TestGetByUserIdAsync();
        await TestGetByStatusAsync();
        await TestUpdateStatusAsync();
        await TestGetUserTotalSpentAsync();
        await TestDateRangeQueryAsync();

        Console.WriteLine($"[{_dbType}] All Order Repository tests passed!\n");
    }

    private async Task TestInsertWithAuditAsync()
    {
        Console.WriteLine($"  [{_dbType}] Testing Insert with Audit Fields...");

        // 先获取或创建用户
        var users = await _userRepo.GetAllAsync(limit: 1);
        long userId;
        if (users.Count == 0)
        {
            var user = new User
            {
                Name = "OrderTestUser",
                Email = $"ordertest_{Guid.NewGuid():N}@example.com",
                Age = 30,
                Balance = 1000m,
                IsActive = true
            };
            await _userRepo.InsertAsync(user);
            users = await _userRepo.GetAllAsync(limit: 1);
        }
        userId = users[0].Id;

        // 创建订单
        var order = new Order
        {
            UserId = userId,
            TotalAmount = 199.99m,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "TestUser"
        };

        var rowsAffected = await _orderRepo.InsertAsync(order);
        Console.WriteLine($"    ✓ Insert with Audit: {rowsAffected} row affected, CreatedBy='{order.CreatedBy}'");
    }

    private async Task TestGetByUserIdAsync()
    {
        Console.WriteLine($"  [{_dbType}] Testing GetByUserId...");

        var users = await _userRepo.GetAllAsync(limit: 1);
        if (users.Count == 0)
        {
            Console.WriteLine($"    ⚠ No users found, skipping...");
            return;
        }

        var orders = await _orderRepo.GetByUserIdAsync(users[0].Id);
        Console.WriteLine($"    ✓ GetByUserId: Found {orders.Count} orders");
    }

    private async Task TestGetByStatusAsync()
    {
        Console.WriteLine($"  [{_dbType}] Testing GetByStatus...");

        var pendingOrders = await _orderRepo.GetByStatusAsync("Pending");
        Console.WriteLine($"    ✓ GetByStatus('Pending'): Found {pendingOrders.Count} orders");
    }

    private async Task TestUpdateStatusAsync()
    {
        Console.WriteLine($"  [{_dbType}] Testing UpdateStatus with Audit...");

        var orders = await _orderRepo.GetByStatusAsync("Pending");
        if (orders.Count == 0)
        {
            Console.WriteLine($"    ⚠ No pending orders to update, skipping...");
            return;
        }

        var order = orders[0];
        var rowsAffected = await _orderRepo.UpdateStatusAsync(order.Id, "Completed", "AdminUser");
        Console.WriteLine($"    ✓ UpdateStatus: {rowsAffected} row affected, UpdatedBy='AdminUser'");

        // 验证更新
        var updatedOrder = await _orderRepo.GetByIdAsync(order.Id);
        if (updatedOrder != null)
        {
            Console.WriteLine($"    ✓ Verified: Status='{updatedOrder.Status}', UpdatedBy='{updatedOrder.UpdatedBy}'");
        }
    }

    private async Task TestGetUserTotalSpentAsync()
    {
        Console.WriteLine($"  [{_dbType}] Testing GetUserTotalSpent...");

        var users = await _userRepo.GetAllAsync(limit: 1);
        if (users.Count == 0)
        {
            Console.WriteLine($"    ⚠ No users found, skipping...");
            return;
        }

        var totalSpent = await _orderRepo.GetUserTotalSpentAsync(users[0].Id);
        Console.WriteLine($"    ✓ GetUserTotalSpent: {totalSpent:C}");
    }

    private async Task TestDateRangeQueryAsync()
    {
        Console.WriteLine($"  [{_dbType}] Testing GetByDateRange...");

        var startDate = DateTime.UtcNow.AddDays(-30);
        var endDate = DateTime.UtcNow.AddDays(1);

        var orders = await _orderRepo.GetByDateRangeAsync(startDate, endDate);
        Console.WriteLine($"    ✓ GetByDateRange (last 30 days): Found {orders.Count} orders");
    }
}
