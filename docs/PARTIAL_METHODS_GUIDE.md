# Sqlx Partial方法扩展指南

## 📚 概述

Sqlx生成的Repository代码包含3个空的partial方法，允许你在不修改生成代码的情况下，添加自定义的拦截逻辑：

```csharp
partial void OnExecuting(string operationName, IDbCommand command);
partial void OnExecuted(string operationName, IDbCommand command, object? result, long elapsedTicks);
partial void OnExecuteFail(string operationName, IDbCommand command, Exception exception, long elapsedTicks);
```

## 🎯 使用场景

### 1. 自定义日志记录
### 2. 性能监控
### 3. SQL审计
### 4. 自定义指标收集
### 5. 错误处理和告警

## 🚀 快速开始

### 示例：添加日志记录

```csharp
// UserRepository.cs（用户编写）
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Sqlx.Annotations;

namespace MyApp.Repositories;

[TableName("users")]
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository(SqliteConnection connection, ILogger<UserRepository> logger) : IUserRepository
{
    // 🔧 实现partial方法：执行前
    partial void OnExecuting(string operationName, global::System.Data.IDbCommand command)
    {
        logger.LogDebug("🔄 开始执行: {Operation}, SQL: {SQL}", 
            operationName, 
            command.CommandText);
    }

    // 🔧 实现partial方法：执行成功
    partial void OnExecuted(string operationName, global::System.Data.IDbCommand command, object? result, long elapsedTicks)
    {
        var elapsedMs = elapsedTicks * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
        logger.LogInformation("✅ 执行完成: {Operation}, 耗时: {Elapsed:F2}ms", 
            operationName, 
            elapsedMs);
    }

    // 🔧 实现partial方法：执行失败
    partial void OnExecuteFail(string operationName, global::System.Data.IDbCommand command, global::System.Exception exception, long elapsedTicks)
    {
        var elapsedMs = elapsedTicks * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
        logger.LogError(exception, "❌ 执行失败: {Operation}, 耗时: {Elapsed:F2}ms", 
            operationName, 
            elapsedMs);
    }
}
```

### 示例：自定义指标收集

```csharp
using System.Diagnostics.Metrics;

public partial class UserRepository
{
    private static readonly Meter s_meter = new("MyApp.Repositories");
    private static readonly Counter<long> s_operationCounter = s_meter.CreateCounter<long>("sqlx.operations", "次", "数据库操作次数");
    private static readonly Histogram<double> s_operationDuration = s_meter.CreateHistogram<double>("sqlx.operation.duration", "ms", "数据库操作耗时");

    partial void OnExecuting(string operationName, global::System.Data.IDbCommand command)
    {
        // 可以在这里添加标签
        s_operationCounter.Add(1, new KeyValuePair<string, object?>("operation", operationName));
    }

    partial void OnExecuted(string operationName, global::System.Data.IDbCommand command, object? result, long elapsedTicks)
    {
        var elapsedMs = elapsedTicks * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
        s_operationDuration.Record(elapsedMs, 
            new KeyValuePair<string, object?>("operation", operationName),
            new KeyValuePair<string, object?>("success", "true"));
    }

    partial void OnExecuteFail(string operationName, global::System.Data.IDbCommand command, global::System.Exception exception, long elapsedTicks)
    {
        var elapsedMs = elapsedTicks * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
        s_operationDuration.Record(elapsedMs, 
            new KeyValuePair<string, object?>("operation", operationName),
            new KeyValuePair<string, object?>("success", "false"),
            new KeyValuePair<string, object?>("error.type", exception.GetType().Name));
    }
}
```

### 示例：SQL审计

```csharp
using System.Collections.Concurrent;

public partial class UserRepository
{
    private static readonly ConcurrentQueue<SqlAuditEntry> s_auditLog = new();

    partial void OnExecuting(string operationName, global::System.Data.IDbCommand command)
    {
        // 记录执行前的信息
    }

    partial void OnExecuted(string operationName, global::System.Data.IDbCommand command, object? result, long elapsedTicks)
    {
        var entry = new SqlAuditEntry
        {
            Timestamp = DateTime.UtcNow,
            OperationName = operationName,
            SqlStatement = command.CommandText,
            ElapsedTicks = elapsedTicks,
            Success = true,
            Parameters = command.Parameters.Cast<System.Data.IDataParameter>()
                .Select(p => new { p.ParameterName, p.Value })
                .ToArray()
        };
        
        s_auditLog.Enqueue(entry);
        
        // 定期持久化审计日志
        if (s_auditLog.Count > 1000)
        {
            _ = Task.Run(FlushAuditLog);
        }
    }

    partial void OnExecuteFail(string operationName, global::System.Data.IDbCommand command, global::System.Exception exception, long elapsedTicks)
    {
        var entry = new SqlAuditEntry
        {
            Timestamp = DateTime.UtcNow,
            OperationName = operationName,
            SqlStatement = command.CommandText,
            ElapsedTicks = elapsedTicks,
            Success = false,
            ErrorMessage = exception.Message,
            ErrorType = exception.GetType().Name
        };
        
        s_auditLog.Enqueue(entry);
    }

    private static async Task FlushAuditLog()
    {
        // 将审计日志批量写入持久化存储
    }

    record SqlAuditEntry
    {
        public DateTime Timestamp { get; init; }
        public string OperationName { get; init; } = "";
        public string SqlStatement { get; init; } = "";
        public long ElapsedTicks { get; init; }
        public bool Success { get; init; }
        public object[]? Parameters { get; init; }
        public string? ErrorMessage { get; init; }
        public string? ErrorType { get; init; }
    }
}
```

### 示例：慢查询告警

```csharp
using Microsoft.Extensions.Logging;

public partial class UserRepository
{
    private const double SlowQueryThresholdMs = 100.0; // 100ms阈值

    partial void OnExecuted(string operationName, global::System.Data.IDbCommand command, object? result, long elapsedTicks)
    {
        var elapsedMs = elapsedTicks * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
        
        if (elapsedMs > SlowQueryThresholdMs)
        {
            logger.LogWarning("🐌 慢查询检测: {Operation}, 耗时: {Elapsed:F2}ms (阈值: {Threshold}ms)\n  SQL: {SQL}", 
                operationName, 
                elapsedMs,
                SlowQueryThresholdMs,
                command.CommandText);
            
            // 可选：发送告警到监控系统
            _ = Task.Run(() => SendSlowQueryAlert(operationName, elapsedMs, command.CommandText));
        }
    }

    private async Task SendSlowQueryAlert(string operationName, double elapsedMs, string sql)
    {
        // 发送告警到监控系统（如钉钉、企业微信、PagerDuty等）
    }
}
```

## 🔄 内置Activity跟踪

Sqlx生成的代码已经内联了`Activity.Current`跟踪，无需额外配置：

```csharp
// 生成的代码（自动包含）
var __activity__ = System.Diagnostics.Activity.Current;
if (__activity__ != null)
{
    __activity__.DisplayName = "GetByIdSync";
    __activity__.SetTag("db.system", "sql");
    __activity__.SetTag("db.operation", "GetByIdSync");
    __activity__.SetTag("db.statement", "SELECT ...");
    __activity__.SetTag("db.duration_ms", elapsedMs);
}
```

### 启用Activity跟踪

```csharp
// 在应用启动时配置ActivitySource
using System.Diagnostics;

var activitySource = new ActivitySource("MyApp");

// 启动Activity
using (var activity = activitySource.StartActivity("UserOperation"))
{
    // 调用Repository方法时，会自动在当前Activity中添加标签
    var user = await userRepository.GetByIdAsync(1);
}
```

### 集成OpenTelemetry

```csharp
// Program.cs
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .AddSource("MyApp")
            .AddConsoleExporter(); // 或其他导出器（如Jaeger, Zipkin）
    });
```

## 📊 性能注意事项

### ✅ 推荐做法

1. **避免阻塞操作** - partial方法中不要进行长时间的同步IO操作
2. **异步日志** - 使用异步日志记录，避免阻塞数据库操作
3. **条件执行** - 使用日志级别和开关控制是否执行expensive的操作
4. **局部变量** - 尽量使用局部变量避免闭包分配

### ❌ 避免的做法

```csharp
// ❌ 不推荐：阻塞IO
partial void OnExecuted(string operationName, IDbCommand command, object? result, long elapsedTicks)
{
    File.AppendAllText("audit.log", $"{operationName}\n"); // 阻塞！
}

// ✅ 推荐：异步写入
partial void OnExecuted(string operationName, IDbCommand command, object? result, long elapsedTicks)
{
    _auditQueue.Enqueue(new AuditEntry(operationName, command.CommandText));
}

// ❌ 不推荐：过度分配
partial void OnExecuting(string operationName, IDbCommand command)
{
    var tags = new Dictionary<string, object> { ... }; // 每次分配！
    _meter.Record(tags);
}

// ✅ 推荐：复用或栈分配
partial void OnExecuting(string operationName, IDbCommand command)
{
    _meter.Record(
        new KeyValuePair<string, object?>("operation", operationName)
    ); // TagList内部优化
}
```

## 🎓 最佳实践

### 1. 使用依赖注入

```csharp
public partial class UserRepository(
    SqliteConnection connection, 
    ILogger<UserRepository> logger,
    IMetricsCollector metrics) : IUserRepository
{
    partial void OnExecuted(string operationName, IDbCommand command, object? result, long elapsedTicks)
    {
        logger.LogDebug("完成: {Op}", operationName);
        metrics.RecordDatabaseOperation(operationName, elapsedTicks);
    }
}
```

### 2. 条件编译

```csharp
partial void OnExecuting(string operationName, IDbCommand command)
{
#if DEBUG
    // 开发环境：详细日志
    logger.LogDebug("SQL: {SQL}\n参数: {Params}", 
        command.CommandText, 
        string.Join(", ", command.Parameters.Cast<IDataParameter>().Select(p => $"{p.ParameterName}={p.Value}")));
#else
    // 生产环境：简化日志
    logger.LogDebug("执行: {Op}", operationName);
#endif
}
```

### 3. 使用ThreadStatic减少分配

```csharp
[ThreadStatic]
private static StringBuilder? t_sqlBuilder;

partial void OnExecuting(string operationName, IDbCommand command)
{
    t_sqlBuilder ??= new StringBuilder(256);
    t_sqlBuilder.Clear();
    t_sqlBuilder.Append("执行: ").Append(operationName);
    logger.LogDebug(t_sqlBuilder.ToString());
}
```

## 🆚 对比：Partial方法 vs 拦截器框架

| 特性 | Partial方法 | 拦截器框架 |
|------|------------|----------|
| **性能开销** | 零（内联） | 小（虚函数调用） |
| **灵活性** | 每个Repository独立 | 全局统一 |
| **配置复杂度** | 简单 | 需要注册 |
| **类型安全** | ✅ 编译检查 | ⚠️ 运行时 |
| **适用场景** | 单个Repository定制 | 全局统一策略 |

## 📝 总结

Partial方法提供了一种**轻量级、高性能、类型安全**的扩展机制：

- ✅ **零开销** - 编译时内联，无虚函数调用
- ✅ **类型安全** - 编译时检查
- ✅ **灵活** - 每个Repository可独立定制
- ✅ **简单** - 无需额外配置
- ✅ **可测试** - 易于单元测试

**推荐使用场景**：
- 日志记录
- 性能监控
- SQL审计
- 慢查询检测
- 自定义指标收集

---

**相关文档**：
- [PERFORMANCE_OPTIMIZATION_REPORT.md](../PERFORMANCE_OPTIMIZATION_REPORT.md) - 性能优化报告
- [Activity跟踪集成](./ACTIVITY_TRACING.md) - 分布式追踪
- [OpenTelemetry集成](./OPENTELEMETRY.md) - 可观测性

**生成时间**: 2025-10-22

