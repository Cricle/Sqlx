using FullDemo.Models;
using FullDemo.Repositories;

namespace FullDemo.Tests;

/// <summary>
/// 日志仓储测试 - 测试批量操作功能
/// </summary>
public class LogRepositoryTests
{
    private readonly ILogRepository _repo;
    private readonly string _dbType;

    public LogRepositoryTests(ILogRepository repo, string dbType)
    {
        _repo = repo;
        _dbType = dbType;
    }

    public async Task RunAllTestsAsync()
    {
        Console.WriteLine($"\n{'='} [{_dbType}] Log Repository Tests (Batch Operations) {new string('=', 40)}");

        await TestBatchInsertAsync();
        await TestGetRecentAsync();
        await TestGetByLevelAsync();
        await TestCountByLevelAsync();
        await TestDeleteOldLogsAsync();

        Console.WriteLine($"[{_dbType}] All Log Repository tests passed!\n");
    }

    private async Task TestBatchInsertAsync()
    {
        Console.WriteLine($"  [{_dbType}] Testing Batch Insert...");

        var logs = new List<Log>();
        var levels = new[] { "DEBUG", "INFO", "WARN", "ERROR", "FATAL" };
        var random = new Random();

        for (int i = 0; i < 100; i++)
        {
            logs.Add(new Log
            {
                Level = levels[random.Next(levels.Length)],
                Message = $"Test log message #{i}: {Guid.NewGuid()}",
                Timestamp = DateTime.UtcNow.AddMinutes(-random.Next(1000))
            });
        }

        // 使用批量插入
        var rowsAffected = await _repo.BatchInsertLogsAsync(logs);
        Console.WriteLine($"    ✓ BatchInsert: {rowsAffected} rows affected");
    }

    private async Task TestGetRecentAsync()
    {
        Console.WriteLine($"  [{_dbType}] Testing GetRecent...");

        var recentLogs = await _repo.GetRecentAsync(10);
        Console.WriteLine($"    ✓ GetRecent(10): Found {recentLogs.Count} logs");

        if (recentLogs.Count > 0)
        {
            Console.WriteLine($"    ✓ Most recent: [{recentLogs[0].Level}] {recentLogs[0].Message.Substring(0, Math.Min(50, recentLogs[0].Message.Length))}...");
        }
    }

    private async Task TestGetByLevelAsync()
    {
        Console.WriteLine($"  [{_dbType}] Testing GetByLevel...");

        var errorLogs = await _repo.GetByLevelAsync("ERROR");
        Console.WriteLine($"    ✓ GetByLevel('ERROR'): Found {errorLogs.Count} logs");

        var infoLogs = await _repo.GetByLevelAsync("INFO");
        Console.WriteLine($"    ✓ GetByLevel('INFO'): Found {infoLogs.Count} logs");
    }

    private async Task TestCountByLevelAsync()
    {
        Console.WriteLine($"  [{_dbType}] Testing CountByLevel...");

        var levels = new[] { "DEBUG", "INFO", "WARN", "ERROR", "FATAL" };
        foreach (var level in levels)
        {
            var count = await _repo.CountByLevelAsync(level);
            Console.WriteLine($"    ✓ CountByLevel('{level}'): {count}");
        }
    }

    private async Task TestDeleteOldLogsAsync()
    {
        Console.WriteLine($"  [{_dbType}] Testing DeleteOldLogs...");

        // 删除一小时前的日志
        var before = DateTime.UtcNow.AddMinutes(-30);
        var rowsDeleted = await _repo.DeleteOldLogsAsync(before);
        Console.WriteLine($"    ✓ DeleteOldLogs (before {before:HH:mm}): {rowsDeleted} rows deleted");

        // 验证剩余数量
        var totalCount = await _repo.CountAsync();
        Console.WriteLine($"    ✓ Remaining logs: {totalCount}");
    }
}
