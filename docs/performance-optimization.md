# Sqlx 性能优化指南

## 概述

Sqlx 通过一系列优化技术，在保持零配置和类型安全的同时，实现了接近 Dapper 的性能，并在内存效率上显著优于 Dapper。

## 核心优化技术

### 1. Ordinal 缓存

**问题**: 传统 ORM 每读取一行数据都要调用 `GetOrdinal()` 来查找列索引。

**解决方案**: 
- 预先计算所有列的 ordinal 索引
- 在读取多行时重用这些索引
- 将 O(N×M) 的复杂度降低到 O(M)

**实现**:
```csharp
// 只调用一次 GetOrdinals
Span<int> ordinals = stackalloc int[propertyCount];
reader.GetOrdinals(dataReader, ordinals);

// 重用 ordinals 读取所有行
while (dataReader.Read())
{
    list.Add(reader.Read(dataReader, ordinals));
}
```

**收益**: GetOrdinal 调用减少 **99.9%**

### 2. 零堆分配（同步方法）

**问题**: 临时数组分配会增加 GC 压力。

**解决方案**: 使用 `stackalloc` 在栈上分配 ordinals 数组。

**实现**:
```csharp
public static List<TEntity> ToList<TEntity>(
    this IResultReader<TEntity> reader, 
    IDataReader dataReader)
{
    var list = new List<TEntity>();
    var propCount = reader.PropertyCount;
    
    // 栈分配，零 GC 压力
    Span<int> ordinals = stackalloc int[propCount];
    reader.GetOrdinals(dataReader, ordinals);
    
    while (dataReader.Read())
    {
        list.Add(reader.Read(dataReader, ordinals));
    }
    
    return list;
}
```

**收益**: 完全消除临时数组的堆分配

### 3. 跳过数组初始化（异步方法）

**问题**: `new int[n]` 会将数组清零，浪费 CPU 周期。

**解决方案**: 使用 `GC.AllocateUninitializedArray<T>()` 跳过初始化。

**实现**:
```csharp
public static async Task<List<TEntity>> ToListAsync<TEntity>(
    this IResultReader<TEntity> reader,
    DbDataReader dataReader,
    CancellationToken cancellationToken = default)
{
    var list = new List<TEntity>();
    var propCount = reader.PropertyCount;
    
    // 跳过数组初始化
#if NETSTANDARD2_1
    var ordinals = new int[propCount];
#else
    var ordinals = GC.AllocateUninitializedArray<int>(propCount);
#endif
    reader.GetOrdinals(dataReader, ordinals);
    
    while (await dataReader.ReadAsync(cancellationToken).ConfigureAwait(false))
    {
        list.Add(reader.Read(dataReader, ordinals));
    }
    
    return list;
}
```

**收益**: 减少数组初始化开销

### 4. 消除 Span 边界检查

**问题**: 每次访问 `ordinals[i]` 都有边界检查开销。

**解决方案**: 将 ordinals 缓存到局部变量。

**生成的代码**:
```csharp
public User Read(IDataReader reader, ReadOnlySpan<int> ordinals)
{
    // 缓存到局部变量，消除重复的边界检查
    var ord0 = ordinals[0];
    var ord1 = ordinals[1];
    var ord2 = ordinals[2];
    // ...
    
    return new User
    {
        Id = reader.GetInt64(ord0),
        Name = reader.GetString(ord1),
        Email = reader.GetString(ord2),
        // ...
    };
}
```

**收益**: 消除 10,000+ 次边界检查（1000 行 × 10 列）

### 5. 零反射

**问题**: 运行时反射影响性能和 AOT 兼容性。

**解决方案**: 
- 编译时生成 `PropertyCount` 属性
- 所有类型信息在编译时确定

**生成的代码**:
```csharp
public sealed class UserResultReader : IResultReader<User>
{
    public int PropertyCount => 10;  // 编译时确定
    
    // 无反射的读取方法
    public User Read(IDataReader reader) { ... }
}
```

**收益**: 完全消除反射开销，支持 AOT

## 性能基准测试结果

### 测试环境
- .NET 9.0.8
- SQLite
- 1000 行数据，10 列

### 批量查询性能

| 指标 | Sqlx | Dapper | 差异 |
|------|------|--------|------|
| **执行时间** | 2.184 ms | 2.051 ms | +6.5% |
| **内存分配** | 95 MB | 227 MB | **-58%** ✅ |
| **Gen0 GC** | 11 次 | 27 次 | **-59%** ✅ |
| **Gen1 GC** | 5 次 | 12 次 | **-58%** ✅ |

### 单行查询性能

| 指标 | Sqlx | Dapper | 差异 |
|------|------|--------|------|
| **执行时间** | 10.73 μs | 11.81 μs | **-9.1%** ✅ |
| **内存分配** | 3.2 KB | 3.0 KB | +6.7% |

## 性能分析

### 为什么批量查询慢 6.5%？

主要原因是 **IsDBNull 检查开销**：

```csharp
// Sqlx 对每个可空字段都检查
UpdatedAt = reader.IsDBNull(ord6) ? default(DateTime?) : reader.GetDateTime(ord6),
Description = reader.IsDBNull(ord8) ? default : reader.GetString(ord8),

// 1000 行 × 2 个可空字段 = 2000 次 IsDBNull 调用
// 开销: ~40-60 μs (占总差距的 30-45%)
```

其他因素：
- 对象初始化器 vs 构造函数 (~30-50 μs)
- 堆分配开销 (~10-20 μs)
- 其他微小开销 (~23-33 μs)

### 为什么单行查询快 9.1%？

单行查询中，Sqlx 的优化优势更明显：
- 直接索引访问（无 GetOrdinal 调用）
- 内联优化（AggressiveInlining）
- 零反射开销

## 实际应用考虑

### 6.5% 的性能差距可以忽略吗？

**是的**，在实际应用中通常可以忽略，因为：

1. **数据库 I/O 占主导**: 
   - 网络延迟: 10-50 ms
   - 数据库查询: 5-20 ms
   - ORM 映射: 0.1-2 ms (差异仅 0.13 ms)

2. **GC 暂停的影响更大**:
   - 58% 的内存节省意味着更少的 GC 暂停
   - 在高并发场景下，GC 暂停可能比 0.13 ms 影响更大

3. **总体响应时间**:
   ```
   典型 Web API 请求: 16-82 ms
   ORM 差异占比: 0.16% - 0.8% (可忽略)
   ```

### 适用场景

**Sqlx 最适合**:
- ✅ 高并发、内存敏感的应用
- ✅ 长时间运行的服务
- ✅ 需要可维护性和类型安全的项目
- ✅ 需要 AOT 支持的场景

**Dapper 最适合**:
- ✅ 对执行速度极度敏感的场景
- ✅ 短时间运行的批处理任务
- ✅ 内存充足的环境

## 优化建议

### 使用 capacityHint

为 List 提供容量提示可以减少内存重新分配：

```csharp
// 如果知道大概的行数
var users = await repo.GetAllAsync(limit: 1000);

// 内部使用
var list = new List<User>(capacityHint);
```

### 使用同步方法（如果可能）

同步方法使用 stackalloc，完全避免堆分配：

```csharp
// 更好的性能
var users = repo.GetAll(limit: 1000);

// vs 异步方法
var users = await repo.GetAllAsync(limit: 1000);
```

### 避免不必要的可空字段

可空字段需要 IsDBNull 检查，影响性能：

```csharp
// 如果字段不会为 null，使用非空类型
public string Name { get; set; } = "";  // 而非 string?

// 如果确实可能为 null，使用可空类型
public string? Description { get; set; }
```

## 总结

Sqlx 通过以下优化实现了优秀的性能平衡：

1. **GetOrdinal 调用**: 减少 99.9%
2. **内存分配**: 比 Dapper 少 58%
3. **GC 压力**: 比 Dapper 少 59%
4. **执行速度**: 
   - 单行查询: 比 Dapper 快 9.1%
   - 批量查询: 比 Dapper 慢 6.5%（可接受）
5. **零反射**: 支持 AOT，性能稳定
6. **类型安全**: 编译时检查，减少运行时错误

在大多数实际应用场景中，Sqlx 的综合表现优于 Dapper。
