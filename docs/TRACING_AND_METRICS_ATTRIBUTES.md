# 追踪和指标特性控制

Sqlx 提供了细粒度的特性（Attribute）来控制生成的代码中是否包含 Activity 追踪和性能指标（Stopwatch 计时）代码。

---

## 📋 概述

### 可用的特性

1. **`[EnableTracing]`** - 控制 Activity 追踪代码生成
2. **`[EnableMetrics]`** - 控制性能指标（Stopwatch 计时）代码生成

### 应用级别

这些特性可以应用在：
- **类级别** - 影响该类中的所有方法
- **方法级别** - 覆盖类级别的设置（优先级更高）

---

## 🎯 EnableTracingAttribute

### 基本用法

```csharp
using Sqlx.Annotations;

// 类级别：禁用所有方法的追踪
[EnableTracing(false)]
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository : IUserRepository
{
    // 此类中所有方法默认不生成追踪代码
}

// 方法级别：覆盖类设置
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository : IUserRepository
{
    // 只为这个方法启用追踪
    [EnableTracing(true)]
    [Sqlx("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    Task<User?> GetByIdAsync(int id);

    // 其他方法使用默认设置（由条件编译控制）
}
```

### 高级配置

```csharp
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository : IUserRepository
{
    // 自定义 Activity 名称
    [EnableTracing(true, ActivityName = "Database.GetUser")]
    [Sqlx("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    Task<User?> GetByIdAsync(int id);

    // 不记录 SQL 语句（避免敏感信息泄露）
    [EnableTracing(true, LogSql = false)]
    [Sqlx("SELECT {{columns}} FROM {{table}} WHERE email = @email")]
    Task<User?> GetByEmailAsync(string email);

    // 记录参数值（谨慎使用，可能包含敏感信息）
    [EnableTracing(true, LogParameters = true)]
    [Sqlx("SELECT COUNT(*) FROM {{table}} WHERE age > @minAge")]
    Task<int> CountByAgeAsync(int minAge);
}
```

### 属性说明

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `Enabled` | `bool` | `true` | 是否启用追踪 |
| `ActivityName` | `string?` | `null` | 自定义 Activity 名称（默认：`Sqlx.{ClassName}.{MethodName}`） |
| `LogSql` | `bool` | `true` | 是否记录 SQL 语句 |
| `LogParameters` | `bool` | `false` | 是否记录参数值 |

---

## ⚡ EnableMetricsAttribute

### 基本用法

```csharp
// 类级别：为所有方法启用指标收集
[EnableMetrics(true)]
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository : IUserRepository
{
    // 所有方法都会生成 Stopwatch 计时代码
}

// 方法级别：选择性禁用
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository : IUserRepository
{
    // 这个方法不收集指标
    [EnableMetrics(false)]
    [Sqlx("SELECT COUNT(*) FROM {{table}}")]
    Task<int> GetCountAsync();

    // 其他方法使用默认设置
}
```

### 高级配置

```csharp
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository : IUserRepository
{
    // 设置慢查询阈值（100ms）
    [EnableMetrics(true, SlowQueryThresholdMs = 100)]
    [Sqlx("SELECT {{columns}} FROM {{table}}")]
    Task<List<User>> GetAllAsync();

    // 不将执行时间传递给 Partial 方法（减少开销）
    [EnableMetrics(true, PassElapsedToPartialMethods = false)]
    [Sqlx("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    Task<User?> GetByIdAsync(int id);
}
```

### 属性说明

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `Enabled` | `bool` | `true` | 是否启用指标收集 |
| `PassElapsedToPartialMethods` | `bool` | `true` | 是否将执行时间传递给 Partial 方法 |
| `SlowQueryThresholdMs` | `int` | `0` | 慢查询阈值（毫秒），可在 Partial 方法中检查 |

---

## 🔄 组合使用

### 场景 1：只要追踪，不要指标

```csharp
[EnableTracing(true)]
[EnableMetrics(false)]
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository : IUserRepository
{
    // 生成 Activity 追踪代码，但不生成 Stopwatch 计时代码
}
```

**生成的代码：**
```csharp
public Task<User?> GetByIdAsync(int id)
{
    #if !SQLX_DISABLE_TRACING
    var __activity__ = Activity.Current;
    if (__activity__ != null)
    {
        __activity__.SetTag("db.operation", "GetByIdAsync");
        // ... 其他追踪标签
    }
    #endif
    
    // 数据库执行...
    
    // 不会生成 Stopwatch 代码
}
```

---

### 场景 2：只要指标，不要追踪

```csharp
[EnableTracing(false)]
[EnableMetrics(true)]
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository : IUserRepository
{
    // 生成 Stopwatch 计时代码，但不生成 Activity 追踪代码
}
```

**生成的代码：**
```csharp
public Task<User?> GetByIdAsync(int id)
{
    // 只生成计时代码
    var __startTimestamp__ = Stopwatch.GetTimestamp();
    
    // 数据库执行...
    
    var __endTimestamp__ = Stopwatch.GetTimestamp();
    var __elapsedTicks__ = __endTimestamp__ - __startTimestamp__;
    
    // 传递给 Partial 方法
    OnExecuted("GetByIdAsync", __cmd__, __result__, __elapsedTicks__);
}
```

---

### 场景 3：两者都要

```csharp
[EnableTracing(true)]
[EnableMetrics(true)]
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository : IUserRepository
{
    // 生成完整的追踪和指标代码（默认行为）
}
```

---

### 场景 4：两者都不要（极致性能）

```csharp
[EnableTracing(false)]
[EnableMetrics(false)]
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository : IUserRepository
{
    // 不生成任何追踪和指标代码，零开销
}
```

**生成的代码：**
```csharp
public Task<User?> GetByIdAsync(int id)
{
    // 直接执行数据库操作，无追踪和指标代码
    User? __result__ = default!;
    IDbCommand? __cmd__ = null;
    
    // 数据库执行...
    
    return Task.FromResult(__result__);
}
```

---

## ⚙️ 与条件编译的交互

### 优先级

特性设置的优先级 **高于** 条件编译符号：

```csharp
// 即使定义了 SQLX_DISABLE_TRACING，这个方法仍会生成追踪代码
[EnableTracing(true)]
[Sqlx("SELECT {{columns}} FROM {{table}}")]
Task<List<User>> GetAllAsync();

// 即使没有定义 SQLX_DISABLE_TRACING，这个方法也不会生成追踪代码
[EnableTracing(false)]
[Sqlx("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
Task<User?> GetByIdAsync(int id);
```

### 条件编译符号

- `SQLX_DISABLE_TRACING` - 全局禁用 Activity 追踪（但特性可以覆盖）
- `SQLX_DISABLE_METRICS` - 全局禁用性能指标（计划中）
- `SQLX_DISABLE_PARTIAL_METHODS` - 全局禁用 Partial 方法调用

---

## 📊 性能影响

### 不同配置的性能开销

| 配置 | Activity | Stopwatch | 预估开销 |
|------|----------|-----------|----------|
| 两者都禁用 | ❌ | ❌ | 0% (基准) |
| 只启用指标 | ❌ | ✅ | ~1-2% |
| 只启用追踪 | ✅ | ❌ | ~2-3% |
| 两者都启用 | ✅ | ✅ | ~3-5% |

### 推荐配置

**开发环境：**
```csharp
[EnableTracing(true)]  // 完整的调试信息
[EnableMetrics(true)]  // 性能分析
```

**生产环境（性能优先）：**
```csharp
[EnableTracing(false)]  // 减少开销
[EnableMetrics(false)]  // 极致性能
```

**生产环境（可观测性优先）：**
```csharp
[EnableTracing(true, LogSql = false)]  // APM 集成，但不记录敏感 SQL
[EnableMetrics(true)]   // 性能监控
```

---

## 💡 最佳实践

### 1. 按环境配置

使用条件编译在不同环境使用不同配置：

```csharp
#if DEBUG
[EnableTracing(true, LogSql = true, LogParameters = true)]
[EnableMetrics(true)]
#else
[EnableTracing(true, LogSql = false, LogParameters = false)]
[EnableMetrics(false)]
#endif
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository : IUserRepository
{
}
```

### 2. 针对性能敏感的方法禁用

```csharp
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository : IUserRepository
{
    // 高频调用的方法：禁用追踪和指标
    [EnableTracing(false)]
    [EnableMetrics(false)]
    [Sqlx("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    Task<User?> GetByIdAsync(int id);  // 每秒调用1000+次

    // 低频但重要的方法：启用完整追踪
    [EnableTracing(true)]
    [EnableMetrics(true)]
    [Sqlx("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values}})")]
    Task<long> CreateAsync(User user);  // 每秒调用<10次
}
```

### 3. 敏感操作不记录 SQL

```csharp
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository : IUserRepository
{
    // 登录操作：追踪但不记录 SQL（避免泄露密码）
    [EnableTracing(true, LogSql = false)]
    [Sqlx("SELECT {{columns}} FROM {{table}} WHERE username = @username")]
    Task<User?> GetByUsernameAsync(string username);
}
```

### 4. 慢查询监控

```csharp
[RepositoryFor(typeof(IReportRepository))]
public partial class ReportRepository : IReportRepository
{
    // 设置慢查询阈值
    [EnableMetrics(true, SlowQueryThresholdMs = 1000)]
    [Sqlx("SELECT ... FROM ... WHERE ... GROUP BY ...")]
    Task<Report> GenerateComplexReportAsync();

    // 在 Partial 方法中处理慢查询
    partial void OnExecuted(string operation, IDbCommand command, 
                           object? result, long elapsedTicks)
    {
        var elapsedMs = elapsedTicks * 1000.0 / Stopwatch.Frequency;
        
        // 检查是否超过阈值
        if (operation == "GenerateComplexReportAsync" && elapsedMs > 1000)
        {
            Logger.Warning($"Slow query detected: {operation} took {elapsedMs:F2}ms");
        }
    }
}
```

---

## 🔍 生成的代码示例

### 完整追踪和指标

```csharp
[EnableTracing(true)]
[EnableMetrics(true)]
```

**生成：**
```csharp
public async Task<User?> GetByIdAsync(int id)
{
    #if !SQLX_DISABLE_TRACING
    var __activity__ = Activity.Current;
    var __startTimestamp__ = Stopwatch.GetTimestamp();
    
    if (__activity__ != null)
    {
        __activity__.DisplayName = "GetByIdAsync";
        __activity__.SetTag("db.system", "sql");
        __activity__.SetTag("db.operation", "GetByIdAsync");
        __activity__.SetTag("db.statement", "SELECT ...");
    }
    #endif
    
    User? __result__ = default!;
    IDbCommand? __cmd__ = null;
    
    try
    {
        // 数据库执行...
        
        #if !SQLX_DISABLE_TRACING
        var __endTimestamp__ = Stopwatch.GetTimestamp();
        var __elapsedTicks__ = __endTimestamp__ - __startTimestamp__;
        
        if (__activity__ != null)
        {
            var __elapsedMs__ = __elapsedTicks__ * 1000.0 / Stopwatch.Frequency;
            __activity__.SetTag("db.duration_ms", (long)__elapsedMs__);
            __activity__.SetTag("db.success", true);
        }
        #endif
        
        #if !SQLX_DISABLE_PARTIAL_METHODS
        OnExecuted("GetByIdAsync", __cmd__, __result__, __elapsedTicks__);
        #endif
    }
    catch (Exception __ex__)
    {
        #if !SQLX_DISABLE_TRACING
        var __endTimestamp__ = Stopwatch.GetTimestamp();
        var __elapsedTicks__ = __endTimestamp__ - __startTimestamp__;
        
        if (__activity__ != null)
        {
            __activity__.SetTag("db.success", false);
            __activity__.SetTag("error.type", __ex__.GetType().Name);
        }
        #endif
        
        #if !SQLX_DISABLE_PARTIAL_METHODS
        OnExecuteFail("GetByIdAsync", __cmd__, __ex__, __elapsedTicks__);
        #endif
        
        throw;
    }
    
    return __result__;
}
```

---

## 📝 总结

### 关键点

1. ✅ **细粒度控制** - 可以针对每个方法单独设置
2. ✅ **优先级明确** - 方法级别 > 类级别 > 条件编译
3. ✅ **性能友好** - 未使用的代码完全不生成
4. ✅ **易于使用** - 通过特性配置，无需修改生成器

### 何时使用

- **开发阶段** - 启用所有追踪和指标，便于调试
- **性能测试** - 禁用追踪，只保留指标
- **生产环境（高性能）** - 全部禁用
- **生产环境（可观测性）** - 启用追踪（不记录SQL），禁用指标

---

## 🔗 相关文档

- [Partial 方法指南](PARTIAL_METHODS_GUIDE.md) - 如何使用 Partial 方法处理追踪数据
- [性能分析](../PERFORMANCE_ANALYSIS.md) - 不同配置的性能对比
- [最佳实践](BEST_PRACTICES.md) - 更多使用建议

