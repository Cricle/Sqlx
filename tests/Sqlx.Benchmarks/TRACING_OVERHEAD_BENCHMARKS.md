# Sqlx 追踪开销性能测试指南

本文档介绍如何运行 Sqlx 的追踪开销性能测试，对比不同配置下的性能差异。

---

## 📊 测试配置

### 三种 Repository 实现

1. **UserRepositoryNoTracing** - 零追踪（极致性能）
   ```csharp
   [EnableTracing(false)]  // 禁用Activity追踪
   [EnableMetrics(false)]  // 禁用性能指标
   ```
   - 不生成 Activity 追踪代码
   - 不生成 Stopwatch 计时代码
   - 极致性能，零开销

2. **UserRepositoryMetricsOnly** - 只有指标
   ```csharp
   [EnableTracing(false)]  // 禁用Activity追踪
   [EnableMetrics(true)]   // 启用性能指标
   ```
   - 不生成 Activity 追踪代码
   - 生成 Stopwatch 计时代码
   - 可以在 Partial 方法中获取执行时间

3. **UserRepositoryWithTracing** - 完整追踪
   ```csharp
   [EnableTracing(true)]  // 启用Activity追踪
   [EnableMetrics(true)]  // 启用性能指标
   ```
   - 生成完整的 Activity 追踪代码
   - 生成 Stopwatch 计时代码
   - 最完整的可观测性

---

## 🚀 运行测试

### 运行所有测试

```bash
cd tests/Sqlx.Benchmarks
dotnet run -c Release
```

### 只运行追踪开销测试

```bash
cd tests/Sqlx.Benchmarks
dotnet run -c Release --filter "*TracingOverhead*"
```

### 只运行特定的测试方法

```bash
# 只测试单行查询的追踪开销
dotnet run -c Release --filter "*TracingOverhead*SingleRow*"

# 只测试多行查询的追踪开销
dotnet run -c Release --filter "*TracingOverhead*MultiRow*"

# 只测试复杂查询的追踪开销
dotnet run -c Release --filter "*TracingOverhead*ComplexQuery*"
```

---

## 📈 预期结果

### 单行查询性能对比

| 方法 | 平均耗时 | 内存分配 | 相对性能 |
|------|----------|----------|----------|
| Raw ADO.NET (基准) | 6.60 μs | 904 B | 1.0x |
| Sqlx 零追踪 | ~8-10 μs | ~1,000 B | ~1.5x |
| Sqlx 只有指标 | ~9-11 μs | ~1,100 B | ~1.6x |
| Sqlx 完整追踪 | ~16-18 μs | ~1,240 B | ~2.5x |
| Dapper | ~10-12 μs | ~1,896 B | ~1.7x |

### 追踪开销分析

| 配置 | Activity | Stopwatch | 预计额外开销 | 内存增加 |
|------|----------|-----------|-------------|----------|
| 零追踪 | ❌ | ❌ | 0% (基准) | 0 B |
| 只有指标 | ❌ | ✅ | +10-15% | +100 B |
| 完整追踪 | ✅ | ✅ | +100-120% | +240 B |

**关键发现**：
- ✅ **零追踪配置** - 接近 Raw ADO.NET 性能
- ✅ **指标开销较小** - 只有 Stopwatch，开销 <15%
- ⚠️ **Activity 是主要开销** - 追踪代码占总开销的 80%+

---

## 🎯 测试场景

### 场景 1：单行查询

**SQL**: `SELECT ... FROM users WHERE id = 1`

**测试方法**：
- `RawAdoNet_SingleRow()` - 基准
- `Sqlx_NoTracing_SingleRow()` - 零追踪
- `Sqlx_MetricsOnly_SingleRow()` - 只有指标
- `Sqlx_WithTracing_SingleRow()` - 完整追踪
- `Dapper_SingleRow()` - 对比

**预期**：
- Sqlx 零追踪应该只比 Raw ADO.NET 慢 30-50%
- Sqlx 完整追踪应该比 Dapper 慢 40-60%

---

### 场景 2：多行查询

**SQL**: `SELECT ... FROM users LIMIT 10`

**测试方法**：
- `RawAdoNet_MultiRow()` - 基准
- `Sqlx_NoTracing_MultiRow()` - 零追踪
- `Sqlx_MetricsOnly_MultiRow()` - 只有指标
- `Sqlx_WithTracing_MultiRow()` - 完整追踪
- `Dapper_MultiRow()` - 对比

**预期**：
- 追踪开销相对降低（因为数据库操作占主要时间）
- Sqlx 零追踪应该接近 Dapper 性能

---

### 场景 3：复杂查询

**SQL**: `SELECT ... WHERE age > @minAge AND is_active = @isActive`

**测试方法**：
- `RawAdoNet_ComplexQuery()` - 基准
- `Sqlx_NoTracing_ComplexQuery()` - 零追踪
- `Sqlx_MetricsOnly_ComplexQuery()` - 只有指标
- `Sqlx_WithTracing_ComplexQuery()` - 完整追踪
- `Dapper_ComplexQuery()` - 对比

**预期**：
- 参数化查询，追踪开销占比更小
- Sqlx 零追踪应该优于 Dapper（因为编译时生成）

---

## 📝 结果分析

### 如何解读结果

1. **基准对比** - 查看 "Ratio" 列
   - 1.0x = 与基准（Raw ADO.NET）相同
   - 1.5x = 比基准慢 50%
   - 2.0x = 比基准慢 100%（2倍时间）

2. **内存分配** - 查看 "Allocated" 列
   - 越低越好
   - Sqlx 应该比 Dapper 少 20-40%

3. **追踪开销** - 对比三种 Sqlx 配置
   - NoTracing vs MetricsOnly = Stopwatch 开销
   - MetricsOnly vs WithTracing = Activity 开销
   - NoTracing vs WithTracing = 总开销

### 示例报告解读

```
| Method                          | Mean     | Allocated |
|-------------------------------- |---------:|----------:|
| RawAdoNet_SingleRow             |  6.60 μs |     904 B |  ← 基准
| Sqlx_NoTracing_SingleRow        |  8.50 μs |   1000 B |  ← 零开销，只比基准慢 29%
| Sqlx_MetricsOnly_SingleRow      |  9.80 μs |   1100 B |  ← Stopwatch 增加 15% 开销
| Sqlx_WithTracing_SingleRow      | 16.36 μs |   1240 B |  ← Activity 增加 67% 开销
| Dapper_SingleRow                | 10.15 μs |   1896 B |  ← 参考对比
```

**分析**：
- Stopwatch 开销 = 9.80 - 8.50 = 1.30 μs (15%)
- Activity 开销 = 16.36 - 9.80 = 6.56 μs (67%)
- 总开销 = 16.36 - 8.50 = 7.86 μs (92%)

---

## 💡 性能优化建议

### 根据场景选择配置

**开发环境**：
```csharp
[EnableTracing(true)]   // 完整追踪，便于调试
[EnableMetrics(true)]   // 性能分析
```

**生产环境 - 高性能API**：
```csharp
[EnableTracing(false)]  // 零追踪
[EnableMetrics(false)]  // 极致性能
```

**生产环境 - 可观测性**：
```csharp
[EnableTracing(true, LogSql = false)]  // 追踪但不记录SQL
[EnableMetrics(false)]                 // 禁用指标减少开销
```

**生产环境 - 慢查询监控**：
```csharp
[EnableTracing(false)]  // 不需要Activity
[EnableMetrics(true)]   // 只需要执行时间
```

### 方法级别优化

```csharp
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository : IUserRepository
{
    // 高频方法：禁用追踪
    [EnableTracing(false)]
    [EnableMetrics(false)]
    [Sqlx("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    User? GetByIdSync(int id);  // 每秒1000+次调用

    // 低频但重要的方法：启用追踪
    [EnableTracing(true)]
    [EnableMetrics(true)]
    [Sqlx("INSERT INTO {{table}} ...")]
    int InsertSync(...);  // 每秒<10次调用
}
```

---

## 🔍 故障排查

### 性能不如预期

1. **确认编译模式**
   ```bash
   # 必须使用 Release 模式
   dotnet run -c Release
   ```

2. **检查条件编译符号**
   ```bash
   # 查看是否定义了 SQLX_DISABLE_TRACING
   dotnet build -c Release /p:DefineConstants="SQLX_DISABLE_TRACING"
   ```

3. **查看生成的代码**
   ```bash
   # 检查 obj/Debug/net8.0/generated/ 目录
   # 确认是否真的生成了不同的代码
   ```

### 性能差异过大

如果 Sqlx 零追踪版本仍比 Raw ADO.NET 慢很多：
1. 检查是否使用了正确的 Repository 实现
2. 查看生成的代码是否有 Activity/Stopwatch 代码
3. 确认数据库连接状态（是否每次都重新连接）

---

## 📊 生成的代码对比

### 零追踪配置生成的代码

```csharp
public User? GetByIdSync(int id)
{
    User? __result__ = default!;
    IDbCommand? __cmd__ = null;

    if (_connection.State != ConnectionState.Open)
        _connection.Open();
    
    __cmd__ = _connection.CreateCommand();
    __cmd__.CommandText = "SELECT ... FROM users WHERE id = @id";
    
    var __p_id__ = __cmd__.CreateParameter();
    __p_id__.ParameterName = "@id";
    __p_id__.Value = id;
    __cmd__.Parameters.Add(__p_id__);

    using var reader = __cmd__.ExecuteReader();
    if (reader.Read())
    {
        __result__ = new User
        {
            Id = reader.GetInt32(0),
            Name = reader.GetString(1),
            // ... 序号访问
        };
    }

    return __result__;
}
```

### 完整追踪配置生成的代码

```csharp
public User? GetByIdSync(int id)
{
    #if !SQLX_DISABLE_TRACING
    var __activity__ = Activity.Current;
    var __startTimestamp__ = Stopwatch.GetTimestamp();
    
    if (__activity__ != null)
    {
        __activity__.SetTag("db.operation", "GetByIdSync");
        __activity__.SetTag("db.statement", "SELECT ...");
    }
    #endif

    User? __result__ = default!;
    IDbCommand? __cmd__ = null;

    try
    {
        // ... 数据库操作（同上）...

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
        OnExecuted("GetByIdSync", __cmd__, __result__, __elapsedTicks__);
        #endif
    }
    catch (Exception __ex__)
    {
        #if !SQLX_DISABLE_TRACING
        // ... Activity 错误标记 ...
        #endif
        throw;
    }

    return __result__;
}
```

---

## 🎉 总结

### 关键点

1. ✅ **三种配置对比** - 零追踪、只有指标、完整追踪
2. ✅ **量化开销** - 精确测量 Stopwatch 和 Activity 的性能影响
3. ✅ **实战指导** - 根据场景选择合适的配置
4. ✅ **性能基准** - 与 Raw ADO.NET 和 Dapper 对比

### 预期发现

- **Stopwatch 开销较小** - 约 10-15%
- **Activity 是主要开销** - 约 60-80%
- **零追踪接近原生性能** - 只比 Raw ADO.NET 慢 30-50%
- **内存效率优于 Dapper** - 少 20-40% 内存分配

### 使用建议

- **性能关键路径** → 使用零追踪
- **需要慢查询监控** → 使用只有指标
- **需要APM集成** → 使用完整追踪
- **开发调试** → 使用完整追踪

---

## 🔗 相关文档

- [追踪和指标特性控制](../../docs/TRACING_AND_METRICS_ATTRIBUTES.md)
- [性能分析报告](../../PERFORMANCE_ANALYSIS.md)
- [Partial 方法指南](../../docs/PARTIAL_METHODS_GUIDE.md)

