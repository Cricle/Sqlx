using FullDemo.Models;
using FullDemo.Repositories;

namespace FullDemo.Tests;

/// <summary>
/// 账户仓储测试 - 测试乐观锁功能
/// </summary>
public class AccountRepositoryTests
{
    private readonly IAccountRepository _repo;
    private readonly string _dbType;

    public AccountRepositoryTests(IAccountRepository repo, string dbType)
    {
        _repo = repo;
        _dbType = dbType;
    }

    public async Task RunAllTestsAsync()
    {
        Console.WriteLine($"\n{'='} [{_dbType}] Account Repository Tests (Optimistic Locking) {new string('=', 35)}");

        await TestInsertAsync();
        await TestGetByAccountNoAsync();
        await TestUpdateWithVersionAsync();
        await TestConcurrencyConflictAsync();
        await TestIncreaseDecreaseBalanceAsync();

        Console.WriteLine($"[{_dbType}] All Account Repository tests passed!\n");
    }

    private async Task TestInsertAsync()
    {
        Console.WriteLine($"  [{_dbType}] Testing Insert...");

        var account = new Account
        {
            AccountNo = $"ACC-{Guid.NewGuid():N}",
            Balance = 1000m,
            Version = 0
        };

        var rowsAffected = await _repo.InsertAsync(account);
        Console.WriteLine($"    ✓ Insert: {rowsAffected} row affected, Version=0");
    }

    private async Task TestGetByAccountNoAsync()
    {
        Console.WriteLine($"  [{_dbType}] Testing GetByAccountNo...");

        // 创建一个新账户
        var accountNo = $"ACC-{Guid.NewGuid():N}";
        await _repo.InsertAsync(new Account
        {
            AccountNo = accountNo,
            Balance = 500m,
            Version = 0
        });

        var account = await _repo.GetByAccountNoAsync(accountNo);
        if (account == null)
            throw new Exception($"GetByAccountNo failed: account {accountNo} not found");

        Console.WriteLine($"    ✓ GetByAccountNo: Found account with balance {account.Balance:C}");
    }

    private async Task TestUpdateWithVersionAsync()
    {
        Console.WriteLine($"  [{_dbType}] Testing Update with Version (Optimistic Lock)...");

        // 创建账户
        var accountNo = $"ACC-{Guid.NewGuid():N}";
        await _repo.InsertAsync(new Account
        {
            AccountNo = accountNo,
            Balance = 1000m,
            Version = 0
        });

        // 获取账户
        var account = await _repo.GetByAccountNoAsync(accountNo);
        if (account == null)
            throw new Exception("Account not found");

        // 更新余额（正确版本）
        var rowsAffected = await _repo.UpdateBalanceWithVersionAsync(account.Id, 1500m, account.Version);
        if (rowsAffected != 1)
            throw new Exception($"Update failed: expected 1 row, got {rowsAffected}");

        Console.WriteLine($"    ✓ Update with correct version: Success, Version incremented");

        // 验证版本已增加
        var updatedAccount = await _repo.GetByIdAsync(account.Id);
        if (updatedAccount == null || updatedAccount.Version != account.Version + 1)
            throw new Exception("Version not incremented");

        Console.WriteLine($"    ✓ Verified: New version = {updatedAccount.Version}");
    }

    private async Task TestConcurrencyConflictAsync()
    {
        Console.WriteLine($"  [{_dbType}] Testing Concurrency Conflict...");

        // 创建账户
        var accountNo = $"ACC-{Guid.NewGuid():N}";
        await _repo.InsertAsync(new Account
        {
            AccountNo = accountNo,
            Balance = 2000m,
            Version = 0
        });

        var account = await _repo.GetByAccountNoAsync(accountNo);
        if (account == null)
            throw new Exception("Account not found");

        // 第一次更新成功
        var rowsAffected1 = await _repo.UpdateBalanceWithVersionAsync(account.Id, 2500m, 0);

        // 第二次更新使用旧版本（应该失败）
        var rowsAffected2 = await _repo.UpdateBalanceWithVersionAsync(account.Id, 3000m, 0);

        if (rowsAffected2 != 0)
            throw new Exception("Concurrency conflict not detected: update should have failed");

        Console.WriteLine($"    ✓ Concurrency conflict detected: Update with old version returned 0 rows");
    }

    private async Task TestIncreaseDecreaseBalanceAsync()
    {
        Console.WriteLine($"  [{_dbType}] Testing Increase/Decrease Balance...");

        // 创建账户
        var accountNo = $"ACC-{Guid.NewGuid():N}";
        await _repo.InsertAsync(new Account
        {
            AccountNo = accountNo,
            Balance = 1000m,
            Version = 0
        });

        var account = await _repo.GetByAccountNoAsync(accountNo);
        if (account == null)
            throw new Exception("Account not found");

        // 增加余额
        var increased = await _repo.IncreaseBalanceAsync(account.Id, 500m, account.Version);
        Console.WriteLine($"    ✓ IncreaseBalance(500): {increased} row affected");

        // 获取最新版本
        account = await _repo.GetByIdAsync(account.Id);
        if (account == null)
            throw new Exception("Account not found after increase");

        // 减少余额
        var decreased = await _repo.DecreaseBalanceAsync(account.Id, 200m, account.Version);
        Console.WriteLine($"    ✓ DecreaseBalance(200): {decreased} row affected");

        // 验证最终余额
        account = await _repo.GetByIdAsync(account.Id);
        Console.WriteLine($"    ✓ Final balance: {account?.Balance:C}");
    }
}
