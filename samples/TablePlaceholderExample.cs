// Example: Using {{table}} placeholder with dynamic table names
using Sqlx;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace Sqlx.Samples;

[Sqlx]
public class LogEntry
{
    [Key]
    public long Id { get; set; }
    public string Message { get; set; } = "";
    public DateTime Timestamp { get; set; }
}

public interface ILogRepository
{
    [SqlTemplate("SELECT {{columns}} FROM {{table}} ORDER BY timestamp DESC {{limit --param count}}")]
    Task<List<LogEntry>> GetRecentLogsAsync(int count);
    
    [SqlTemplate("SELECT {{columns}} FROM {{table --param tableName}} WHERE timestamp >= @startDate")]
    Task<List<LogEntry>> GetLogsFromTableAsync(string tableName, DateTime startDate);
    
    [SqlTemplate("INSERT INTO {{table --param tableName}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})")]
    Task<int> InsertIntoTableAsync(string tableName, LogEntry entry);
}

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(ILogRepository))]
public partial class LogRepository(IDbConnection connection) : ILogRepository { }

public class LogRepositoryUsageExamples
{
    public static async Task ExampleUsage(ILogRepository repo)
    {
        // Example 1: Static table name
        var recentLogs = await repo.GetRecentLogsAsync(10);
        // SQL: SELECT [id], [message], [timestamp] FROM [LogEntry] ORDER BY timestamp DESC LIMIT 10
        
        // Example 2: Dynamic table name - query from archived logs
        var archivedLogs = await repo.GetLogsFromTableAsync("logs_2024", DateTime.Parse("2024-01-01"));
        // SQL: SELECT [id], [message], [timestamp] FROM [logs_2024] WHERE timestamp >= @startDate
        
        // Example 3: Dynamic table name - insert into current month's partition
        var currentMonth = DateTime.Now.ToString("yyyy_MM");
        var tableName = $"logs_{currentMonth}";
        var newLog = new LogEntry { Message = "Application started", Timestamp = DateTime.Now };
        await repo.InsertIntoTableAsync(tableName, newLog);
        // SQL: INSERT INTO [logs_2025_01] ([message], [timestamp]) VALUES (@message, @timestamp)
    }
    
    // Use case: Multi-tenant application with separate tables per tenant
    public static async Task MultiTenantExample(ILogRepository repo, string tenantId)
    {
        var tableName = $"logs_tenant_{tenantId}";
        var logs = await repo.GetLogsFromTableAsync(tableName, DateTime.Now.AddDays(-7));
        // SQL: SELECT [id], [message], [timestamp] FROM [logs_tenant_abc123] WHERE timestamp >= @startDate
    }
    
    // Use case: Time-based partitioning
    public static async Task TimePartitioningExample(ILogRepository repo)
    {
        var months = new[] { "2024_11", "2024_12", "2025_01" };
        var allLogs = new List<LogEntry>();
        foreach (var month in months)
        {
            var tableName = $"logs_{month}";
            var logs = await repo.GetLogsFromTableAsync(tableName, DateTime.MinValue);
            allLogs.AddRange(logs);
        }
    }
}
