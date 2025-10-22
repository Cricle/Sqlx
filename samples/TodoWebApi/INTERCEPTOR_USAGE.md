# Sqlx 拦截器使用指南

本文档说明如何在 TodoWebApi 中使用 Sqlx 全局拦截器。

---

## 🎯 拦截器功能

Sqlx 拦截器提供了一种轻量级的方式来监控、追踪和记录 SQL 执行：

- **执行前拦截** (`OnExecuting`) - SQL 执行前调用
- **执行成功拦截** (`OnExecuted`) - SQL 执行成功后调用
- **执行失败拦截** (`OnFailed`) - SQL 执行失败时调用

## ⚡ 性能特性

- **栈分配** - `SqlxExecutionContext` 使用 `ref struct`，零堆分配
- **零GC** - 使用 `ReadOnlySpan<char>` 避免字符串拷贝
- **Fail Fast** - 拦截器异常直接抛出，不隐藏错误
- **AggressiveInlining** - 优化调用性能

---

## 📝 使用示例

### 1. 注册拦截器（Program.cs）

```csharp
// 1. Activity 追踪（兼容 OpenTelemetry、Application Insights、Jaeger）
Sqlx.Interceptors.SqlxInterceptors.Add(new Sqlx.Interceptors.ActivityInterceptor());

// 2. 简单日志
Sqlx.Interceptors.SqlxInterceptors.Add(new TodoWebApi.Interceptors.SimpleLogInterceptor());

// 3. 自定义拦截器
Sqlx.Interceptors.SqlxInterceptors.Add(new MyCustomInterceptor());
```

### 2. 内置拦截器

#### ActivityInterceptor（推荐）

使用 `Activity.Current` 与现有 APM 工具集成：

- ✅ **OpenTelemetry** - 分布式追踪
- ✅ **Application Insights** - Azure 监控
- ✅ **Jaeger** - 微服务追踪
- ✅ **Zipkin** - 调用链追踪

**配置 OpenTelemetry**:

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource("YourServiceName")  // 你的服务名
        .AddHttpClientInstrumentation()
        .AddAspNetCoreInstrumentation()
        .AddConsoleExporter()
        .AddJaegerExporter());

// 注册 Sqlx 拦截器
Sqlx.Interceptors.SqlxInterceptors.Add(new Sqlx.Interceptors.ActivityInterceptor());
```

**工作原理**:
- 使用 `Activity.Current` 获取当前 Activity 上下文
- 仅添加 SQL 相关标签，不创建新的 Activity
- 零开销：如果没有 Activity 则跳过
- 自动继承父 Activity 的 TraceId 和 SpanId

#### SimpleLogInterceptor（示例）

简单的控制台日志输出：

```
🔄 [Sqlx] 执行: GetAllAsync
   SQL: SELECT id, title, description, ... FROM todos
✅ [Sqlx] 完成: GetAllAsync (12.34ms)
```

---

## 🔧 自定义拦截器

### 基础拦截器

```csharp
using Sqlx.Interceptors;

public class MyInterceptor : ISqlxInterceptor
{
    public void OnExecuting(ref SqlxExecutionContext context)
    {
        // 执行前：记录开始
        Console.WriteLine($"执行: {context.OperationName.ToString()}");
    }

    public void OnExecuted(ref SqlxExecutionContext context)
    {
        // 执行成功：记录结果和耗时
        Console.WriteLine($"成功: {context.ElapsedMilliseconds:F2}ms");
    }

    public void OnFailed(ref SqlxExecutionContext context)
    {
        // 执行失败：记录异常
        Console.WriteLine($"失败: {context.Exception?.Message}");
    }
}
```

### 性能监控拦截器

```csharp
using System.Threading;
using Sqlx.Interceptors;

public class PerformanceMonitorInterceptor : ISqlxInterceptor
{
    private long _totalCalls;
    private long _totalTicks;

    public void OnExecuting(ref SqlxExecutionContext context)
    {
        Interlocked.Increment(ref _totalCalls);
    }

    public void OnExecuted(ref SqlxExecutionContext context)
    {
        var elapsed = context.EndTimestamp - context.StartTimestamp;
        Interlocked.Add(ref _totalTicks, elapsed);
    }

    public void OnFailed(ref SqlxExecutionContext context)
    {
        // 也记录失败的耗时
        var elapsed = context.EndTimestamp - context.StartTimestamp;
        Interlocked.Add(ref _totalTicks, elapsed);
    }

    // 查询统计
    public (long TotalCalls, double AvgMs) GetStats()
    {
        var calls = Interlocked.Read(ref _totalCalls);
        var ticks = Interlocked.Read(ref _totalTicks);
        var avgMs = calls > 0
            ? ticks / (double)calls / TimeSpan.TicksPerMillisecond
            : 0;
        return (calls, avgMs);
    }
}
```

### 慢查询监控拦截器

```csharp
public class SlowQueryInterceptor : ISqlxInterceptor
{
    private readonly double _thresholdMs;

    public SlowQueryInterceptor(double thresholdMs = 1000)
    {
        _thresholdMs = thresholdMs;
    }

    public void OnExecuting(ref SqlxExecutionContext context) { }

    public void OnExecuted(ref SqlxExecutionContext context)
    {
        if (context.ElapsedMilliseconds > _thresholdMs)
        {
            Console.WriteLine($"⚠️ 慢查询警告:");
            Console.WriteLine($"   操作: {context.OperationName.ToString()}");
            Console.WriteLine($"   耗时: {context.ElapsedMilliseconds:F2}ms");
            Console.WriteLine($"   SQL: {context.Sql.ToString()}");
        }
    }

    public void OnFailed(ref SqlxExecutionContext context) { }
}
```

---

## 📊 执行上下文（SqlxExecutionContext）

```csharp
public ref struct SqlxExecutionContext
{
    // 基本信息（零拷贝）
    public readonly ReadOnlySpan<char> OperationName;  // 方法名
    public readonly ReadOnlySpan<char> RepositoryType; // Repository类型
    public readonly ReadOnlySpan<char> Sql;            // SQL语句

    // 时间戳
    public long StartTimestamp;   // 开始时间（Ticks）
    public long EndTimestamp;     // 结束时间（Ticks）

    // 结果
    public object? Result;        // 执行结果（可用于传递 Activity 等）
    public Exception? Exception;  // 异常（如果失败）

    // 计算属性
    public readonly double ElapsedMilliseconds; // 耗时（毫秒）
}
```

**注意**:
- `ref struct` 强制栈分配，零堆分配
- `ReadOnlySpan<char>` 避免字符串拷贝
- 调用 `.ToString()` 时才会分配字符串

---

## 🎯 设计原则

### 1. Fail Fast - 异常不吞噬

拦截器异常会直接抛出：

```csharp
public void OnExecuting(ref SqlxExecutionContext context)
{
    // ✅ 确保不会抛异常，或者抛出有意义的异常
    try
    {
        // 你的逻辑
    }
    catch (Exception ex)
    {
        // ✅ 记录日志后重新抛出
        _logger.LogError(ex, "拦截器执行失败");
        throw new InvalidOperationException("拦截器失败", ex);
    }
}
```

**为什么不吞噬异常？**
- 拦截器通常是核心功能（日志、追踪、监控）
- 如果拦截器失败，应该让开发者知道
- 避免问题隐藏，便于调试

### 2. 零GC设计

```csharp
// ✅ 好的做法：使用 Span，避免字符串分配
public void OnExecuting(ref SqlxExecutionContext context)
{
    // 仅在需要时转换为字符串
    if (_logger.IsEnabled(LogLevel.Debug))
    {
        var operationName = context.OperationName.ToString(); // 仅Debug时分配
        _logger.LogDebug("执行: {Operation}", operationName);
    }
}

// ❌ 避免：总是转换为字符串
public void OnExecuting(ref SqlxExecutionContext context)
{
    var name = context.OperationName.ToString(); // 每次都分配
    // ...
}
```

### 3. 性能优先

拦截器应该尽可能轻量：

```csharp
// ✅ 轻量级：无锁原子操作
Interlocked.Increment(ref _counter);

// ❌ 重量级：避免复杂逻辑
await _httpClient.PostAsync(...); // 不要在拦截器中做网络请求
```

---

## 🔄 完整示例

```csharp
// Program.cs
using Sqlx.Interceptors;
using TodoWebApi.Interceptors;

var builder = WebApplication.CreateBuilder(args);

// ... 服务配置

// 配置拦截器（最多8个）
SqlxInterceptors.Add(new ActivityInterceptor());          // OpenTelemetry
SqlxInterceptors.Add(new SimpleLogInterceptor());         // 日志
SqlxInterceptors.Add(new PerformanceMonitorInterceptor());// 性能监控
SqlxInterceptors.Add(new SlowQueryInterceptor(500));      // 慢查询（500ms）

// 配置 OpenTelemetry（可选）
builder.Services.AddOpenTelemetry()
    .WithTracing(t => t
        .AddSource("Sqlx")
        .AddConsoleExporter());

var app = builder.Build();

// 启用/禁用拦截器
SqlxInterceptors.IsEnabled = true;  // 默认启用

app.Run();
```

---

## 🚀 性能影响

### 无拦截器
- 开销：**0ns**（快速退出）
- GC：**0B**

### 3个拦截器
- 开销：**~80ns**
- GC：**0B**（栈分配）
- 对 SQL 查询（通常 >1ms）影响可忽略

### 对比传统方式

| 方式 | GC | 性能 |
|------|-----|------|
| **Sqlx拦截器**（栈分配） | 0B | 80ns |
| 传统拦截器（堆分配） | 312B | 350ns |

**结论**: Sqlx 拦截器设计极致优化，对性能几乎无影响。

---

## 📚 相关文档

- [GLOBAL_INTERCEPTOR_DESIGN.md](../../GLOBAL_INTERCEPTOR_DESIGN.md) - 拦截器设计文档
- [DESIGN_PRINCIPLES.md](../../DESIGN_PRINCIPLES.md) - 设计原则

---

**提示**: 如果不需要拦截器，可以设置 `SqlxInterceptors.IsEnabled = false` 来完全禁用。

