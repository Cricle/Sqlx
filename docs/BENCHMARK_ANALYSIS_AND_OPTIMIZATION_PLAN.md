# Sqlx Benchmark 分析与优化计划

**分析日期**: 2025-10-21
**测试环境**: AMD Ryzen 7 5800H, .NET 8.0.21, Windows 10

---

## 📊 Benchmark 结果总览

### 性能对比摘要

| 场景 | Sqlx vs 原生ADO.NET | Sqlx vs Dapper |
|------|-------------------|----------------|
| **拦截器开销** | +3.6% (+64B) | N/A |
| **单行查询** | +3.8% (+296B) ⚠️ | **快25%** |
| **多行查询** | **快3%** (相同GC) ✅ | **快33%** |
| **参数化查询** | **快5%** (相同GC) ✅ | **快29%** |
| **全表查询** | +1.4% (相同GC) | **快31%** |
| **INSERT** | +1.1% (相同GC) | **快30%** |
| **UPDATE** | +6.5% (相同GC) ⚠️ | **快24%** |
| **DELETE** | **快7.2%** (相同GC) ✅ | **快16%** |
| **批量插入** | +1.7% (相同GC) | **快32%** |
| **JOIN查询** | **快4.5%** (+304B) ⚠️ | **快29%** |
| **聚合查询** | +3.2% (相同GC) | **快12%** |
| **分页查询** | **快2.3%** (相同GC) ✅ | **快30%** |
| **子查询** | **快0.8%** (相同GC) ✅ | N/A |

---

## 🎯 核心发现

### ✅ 优势（保持）

1. **vs Dapper**: 全面领先 12%-33%，GC分配少 50%-70%
2. **多行查询**: 比原生ADO.NET快 3-5%
3. **DELETE操作**: 比原生快 7.2%
4. **参数化查询**: 比原生快 5%
5. **复杂查询**: 大部分场景性能相当或更优

### ⚠️ 问题（需优化）

| 问题 | 严重性 | 影响范围 | 优化潜力 |
|------|--------|---------|---------|
| **1. 拦截器GC泄漏** | 🔴 高 | 所有操作 | 64-192B → 0B |
| **2. 单行查询GC** | 🟠 中 | 高频场景 | +296B (33%) → 0B |
| **3. JOIN查询GC** | 🟠 中 | 复杂查询 | +304B (3%) → 0B |
| **4. UPDATE性能** | 🟡 低 | CRUD操作 | +6.5% → 0% |

---

## 🔴 问题 1: 拦截器GC泄漏（最高优先级）

### 问题详情

```
| Method                  | Mean     | Allocated |
|------------------------ |---------:|----------:|
| RawAdoNet               | 3.330 μs |     648 B |  ← 基准
| NoInterceptor_Disabled  | 3.357 μs |     648 B |  ✅ 无泄漏
| NoInterceptor_Enabled   | 3.358 μs |     648 B |  ✅ 无泄漏
| OneInterceptor_Activity | 3.450 μs |     712 B |  ❌ +64B
| ThreeInterceptors       | 3.601 μs |     760 B |  ❌ +112B
| EightInterceptors_Max   | 3.569 μs |     840 B |  ❌ +192B
```

**分析**:
- 声称"零GC"，但每个拦截器泄漏 ~24-64B
- 即使使用 `ref struct SqlxExecutionContext`，仍有分配
- 可能原因：
  1. `ReadOnlySpan<char>.ToString()` 调用（字符串分配）
  2. `Activity.SetTag()` 参数装箱
  3. `Activity.Current` 访问开销
  4. 拦截器实例本身的分配

### 优化策略

#### 策略 1: 消除 ToString() 调用

**问题**:
```csharp
// ActivityInterceptor.cs
activity.SetTag("db.operation", context.OperationName.ToString()); // ❌ 分配字符串
activity.SetTag("db.statement", context.Sql.ToString());           // ❌ 分配字符串
```

**优化**:
```csharp
// 使用 TagList（栈分配）
var tags = new ActivityTagsCollection
{
    ["db.operation"] = context.OperationName.ToString(),  // 只在需要时分配
    ["db.statement"] = context.Sql.ToString()
};
```

或者更激进：
```csharp
// 在 SqlxExecutionContext 中存储 string 而非 ReadOnlySpan<char>
public ref struct SqlxExecutionContext
{
    public readonly string OperationName;  // 改为 string
    public readonly string RepositoryType;
    public readonly string Sql;
}
```

**原因**: `ReadOnlySpan<char>` 本身不分配，但调用 `ToString()` 必然分配。如果拦截器必须使用字符串，不如直接存储 string。

#### 策略 2: 条件化 Activity 集成

**问题**: `Activity.Current` 和 `SetTag` 有隐含的GC开销

**优化**:
```csharp
public sealed class ActivityInterceptor : ISqlxInterceptor
{
    private static readonly bool IsEnabled = Activity.Current != null;

    public void OnExecuting(ref SqlxExecutionContext context)
    {
        if (!IsEnabled) return; // Fail Fast

        var activity = Activity.Current;
        if (activity == null || !activity.IsAllDataRequested)
            return; // 避免不必要的工作

        // ... 设置标签
    }
}
```

#### 策略 3: 使用 ActivityEvent 代替 SetTag

**问题**: `SetTag` 每次调用可能分配

**优化**:
```csharp
// 使用 AddEvent（批量、更轻量）
activity?.AddEvent(new ActivityEvent("db.executing",
    tags: new ActivityTagsCollection
    {
        ["operation"] = context.OperationName.ToString()
    }));
```

### 预期改进

- **目标**: `OneInterceptor_Activity`: 712 B → 648 B (零额外分配)
- **收益**: 每次SQL执行节省 64B，高频场景显著

---

## 🟠 问题 2: 单行查询GC泄漏

### 问题详情

```
| Method               | Mean     | Allocated |
|--------------------- |---------:|----------:|
| RawAdoNet_SingleRow  | 6.729 μs |     904 B |  ← 基准
| Sqlx_SingleRow       | 6.982 μs |    1200 B |  ❌ +296B (33%)
| Dapper_SingleRow     | 9.255 μs |    1896 B |
```

**分析**:
- Sqlx 比原生多分配 296B (33%)
- 速度慢 3.8%，但仍比 Dapper 快 24%
- 单行查询是**最高频场景**，优化价值极高

### 可能的分配来源

1. **生成代码中的实体映射** (~200B)
   ```csharp
   var user = new User  // 对象头 + 字段
   {
       Id = reader.GetInt32(0),
       Name = reader.GetString(1),  // string 分配
       Email = reader.GetString(2), // string 分配
       // ...
   };
   ```

2. **集合初始化** (~96B)
   ```csharp
   var results = new List<User>(capacity: 1); // List<T> 开销
   results.Add(user);
   return results;
   ```

3. **参数处理** (?)
   ```csharp
   command.Parameters.Add(new SqlParameter("@id", id)); // Parameter 对象
   ```

### 优化策略

#### 策略 1: 使用对象池（Entity Pool）

**问题**: 每次查询 `new User()` 分配对象

**优化**:
```csharp
// 在生成代码中使用 ArrayPool
private static readonly ArrayPool<User> _userPool = ArrayPool<User>.Create();

public User? GetUserById(int id)
{
    // 从池中租用
    var user = _userPool.Rent();
    try
    {
        // ... 填充数据
        return user;
    }
    catch
    {
        _userPool.Return(user);
        throw;
    }
}
```

**问题**: 这会改变对象生命周期，不适合单行查询（用户会持有对象）

#### 策略 2: 直接返回单个对象，而非 List

**问题**: 生成代码可能返回 `List<User>`

**优化**:
```csharp
// 当前生成代码（推测）
public List<User> GetUserById(int id)
{
    var list = new List<User>(1); // ❌ 额外分配 ~96B
    while (reader.Read())
    {
        list.Add(new User { ... });
    }
    return list;
}

// 优化后
public User? GetUserById(int id)
{
    while (reader.Read())
    {
        return new User { ... }; // ✅ 直接返回，无集合
    }
    return null;
}
```

#### 策略 3: 复用 SqlCommand 和 SqlParameter

**问题**: 每次执行创建新的 Command/Parameter

**优化**:
```csharp
// 使用 ThreadStatic 缓存
[ThreadStatic]
private static SqlCommand? _cachedCommand;

public User? GetUserById(int id)
{
    var cmd = _cachedCommand ??= connection.CreateCommand();
    cmd.CommandText = "SELECT ...";
    cmd.Parameters.Clear();
    cmd.Parameters.AddWithValue("@id", id);
    // ...
}
```

**注意**: 这违反了"不要缓存"原则，但对热路径可能值得

#### 策略 4: 使用 ValueTask<T> 而非 Task<T>

**问题**: `Task<T>` 异步方法有额外分配

**优化**:
```csharp
// 当前
public async Task<User?> GetUserByIdAsync(int id) { }

// 优化
public async ValueTask<User?> GetUserByIdAsync(int id) { }
```

### 预期改进

- **策略 2 (直接返回)**: 1200 B → 1100 B (-100B)
- **策略 3 (复用命令)**: 1200 B → 950 B (-250B)
- **目标**: 1200 B → 904 B (与原生相同)

---

## 🟠 问题 3: JOIN 查询GC泄漏

### 问题详情

```
| Method             | Mean     | Allocated |
|------------------- |---------:|----------:|
| RawAdoNet_Join     | 45.13 μs |   10744 B |  ← 基准
| Sqlx_Join          | 43.10 μs |   11048 B |  ❌ +304B (3%)
| Dapper_Join        | 60.80 μs |   14656 B |
```

**分析**:
- Sqlx 比原生快 4.5%（✅），但多分配 304B
- 304B ≈ 额外的临时对象/字符串

### 可能的分配来源

1. **JOIN 结果映射** - 多个对象组合
2. **临时集合** - 用于关联数据
3. **字符串拼接** - 生成复杂SQL

### 优化策略

**策略 1**: 检查生成代码中 JOIN 的实现逻辑，查找不必要的临时对象

**策略 2**: 使用 `Span<T>` 处理中间数据，避免分配

---

## 🟡 问题 4: UPDATE 性能劣化

### 问题详情

```
| Method           | Mean     | Allocated |
|----------------- |---------:|----------:|
| RawAdoNet_Update | 4.177 μs |    1176 B |  ← 基准
| Sqlx_Update      | 4.449 μs |    1176 B |  ❌ +6.5%
| Dapper_Update    | 5.858 μs |    2152 B |
```

**分析**:
- GC相同，但速度慢 6.5%
- 原因：生成代码中的额外逻辑？

### 优化策略

**策略**: 检查生成的 UPDATE 方法，查找热路径中的:
1. 不必要的条件判断
2. 重复的字符串操作
3. 额外的参数处理

---

## 🎯 优化优先级与路线图

### 第一阶段：消除拦截器GC（最高优先级）

**目标**: 实现真正的"零GC"拦截器

| 任务 | 预期收益 | 复杂度 | 时间 |
|------|---------|--------|------|
| 1. 修改 `SqlxExecutionContext` 为存储 string | -50B/次 | 低 | 1h |
| 2. 优化 ActivityInterceptor 的 SetTag 调用 | -14B/次 | 中 | 2h |
| 3. 添加条件编译，仅 Debug 时启用详细追踪 | 性能+5% | 低 | 1h |
| 4. Benchmark 验证 | - | - | 30min |

**代码位置**:
- `src/Sqlx/Interceptors/SqlxExecutionContext.cs`
- `src/Sqlx/Interceptors/ActivityInterceptor.cs`
- `src/Sqlx.Generator/Core/CodeGenerationService.cs` (生成拦截器调用的代码)

**预期结果**:
```
| Method                  | Allocated |
|------------------------ |----------:|
| NoInterceptor_Enabled   |     648 B |  ← 基准
| OneInterceptor_Activity |     648 B |  ✅ 零额外分配
| ThreeInterceptors       |     648 B |  ✅ 零额外分配
| EightInterceptors_Max   |     648 B |  ✅ 零额外分配
```

---

### 第二阶段：优化单行查询（高价值）

**目标**: Sqlx_SingleRow 从 1200B → 904B

| 任务 | 预期收益 | 复杂度 | 时间 |
|------|---------|--------|------|
| 1. 修改生成器，单行查询直接返回对象 | -100B | 中 | 3h |
| 2. 复用 SqlCommand (ThreadStatic) | -150B | 中 | 2h |
| 3. 使用 ValueTask 代替 Task | -50B | 低 | 1h |
| 4. Benchmark 验证 | - | - | 30min |

**代码位置**:
- `src/Sqlx.Generator/Core/CodeGenerationService.cs`
- `src/Sqlx.Generator/Core/SqlTemplateEngine.cs`

**预期结果**:
```
| Method              | Mean     | Allocated |
|-------------------- |---------:|----------:|
| RawAdoNet_SingleRow | 6.729 μs |     904 B |
| Sqlx_SingleRow      | 6.750 μs |     904 B |  ✅ 相同GC
| Dapper_SingleRow    | 9.255 μs |    1896 B |
```

---

### 第三阶段：优化JOIN查询

**目标**: Sqlx_Join 从 11048B → 10744B

| 任务 | 预期收益 | 复杂度 | 时间 |
|------|---------|--------|------|
| 1. 审查生成的 JOIN 查询代码 | - | 中 | 2h |
| 2. 使用 Span 处理中间数据 | -304B | 高 | 4h |
| 3. Benchmark 验证 | - | - | 30min |

**代码位置**:
- `src/Sqlx.Generator/Core/CodeGenerationService.cs`（JOIN 映射逻辑）

---

### 第四阶段：优化UPDATE性能

**目标**: Sqlx_Update 从 4.449 μs → 4.177 μs

| 任务 | 预期收益 | 复杂度 | 时间 |
|------|---------|--------|------|
| 1. Profile UPDATE 方法，找到热点 | - | 中 | 2h |
| 2. 优化生成代码的热路径 | -6.5% | 中 | 3h |
| 3. Benchmark 验证 | - | - | 30min |

---

## 🔬 具体实施计划

### 第一步：修复拦截器GC泄漏

#### 1.1 修改 SqlxExecutionContext

**当前设计** (ref struct + ReadOnlySpan):
```csharp
public ref struct SqlxExecutionContext
{
    public readonly ReadOnlySpan<char> OperationName;  // 零拷贝，但 ToString() 分配
    public readonly ReadOnlySpan<char> RepositoryType;
    public readonly ReadOnlySpan<char> Sql;
}
```

**问题**: 拦截器必须调用 `ToString()` 来使用这些字符串，导致分配

**方案A: 改为 string（推荐）**:
```csharp
public ref struct SqlxExecutionContext
{
    public readonly string OperationName;  // 直接存储 string
    public readonly string RepositoryType;
    public readonly string Sql;
    // 其他字段保持不变
}
```

**优点**:
- 拦截器无需 ToString()，减少分配
- 生成代码更简单：`"MethodName"` 而非 `"MethodName".AsSpan()`
- string 是引用类型，ref struct 可以持有

**缺点**:
- 失去"零拷贝"特性
- 但实际上，方法名/SQL 都是编译时常量，JIT 会优化

**方案B: 添加 string 缓存字段**:
```csharp
public ref struct SqlxExecutionContext
{
    public readonly ReadOnlySpan<char> OperationName;
    private string? _operationNameString;

    public readonly string OperationNameString =>
        _operationNameString ??= OperationName.ToString(); // 懒加载
}
```

**缺点**: ref struct 不能有自动属性，实现复杂

**推荐**: 方案A，直接使用 string

#### 1.2 优化 ActivityInterceptor

```csharp
public sealed class ActivityInterceptor : ISqlxInterceptor
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void OnExecuting(ref SqlxExecutionContext context)
    {
        var activity = Activity.Current;
        if (activity == null || !activity.IsAllDataRequested)
            return; // Fail Fast

        // 使用字段而非方法调用（减少开销）
        activity.DisplayName = context.OperationName; // 零分配

        // 批量设置标签（减少调用次数）
        activity.SetTag("db.system", "sql");
        activity.SetTag("db.operation", context.OperationName);

        // SQL 可能很长，只在需要时设置
        if (activity.IsAllDataRequested)
        {
            activity.SetTag("db.statement", context.Sql);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void OnExecuted(ref SqlxExecutionContext context)
    {
        var activity = Activity.Current;
        if (activity == null) return;

        // 使用 long 避免 double 装箱
        activity.SetTag("db.duration_ms", (long)context.ElapsedMilliseconds);
        activity.SetStatus(ActivityStatusCode.Ok);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void OnFailed(ref SqlxExecutionContext context)
    {
        var activity = Activity.Current;
        if (activity == null) return;

        activity.SetTag("db.duration_ms", (long)context.ElapsedMilliseconds);
        activity.SetStatus(ActivityStatusCode.Error, context.Exception?.Message);

        // 异常信息只在失败时设置
        if (context.Exception != null)
        {
            activity.SetTag("error.type", context.Exception.GetType().Name);
            activity.SetTag("error.message", context.Exception.Message);
        }
    }
}
```

**优化点**:
1. `IsAllDataRequested` 检查 - 避免不必要的工作
2. `DisplayName` 代替部分 SetTag - 零分配
3. `(long)` 代替 `double` - 避免装箱
4. `AggressiveInlining` - 减少调用开销

#### 1.3 修改生成器

**当前生成代码**:
```csharp
var __ctx__ = new global::Sqlx.Interceptors.SqlxExecutionContext(
    "GetUserById".AsSpan(),  // AsSpan() 调用
    "UserRepository".AsSpan(),
    @$"SELECT ...".AsSpan());
```

**优化后**:
```csharp
var __ctx__ = new global::Sqlx.Interceptors.SqlxExecutionContext(
    "GetUserById",  // 直接传字符串字面量
    "UserRepository",
    "SELECT ...");   // SQL 模板也是常量
```

**代码位置**: `src/Sqlx.Generator/Core/CodeGenerationService.cs` 的 `GenerateActualDatabaseExecution` 方法

---

### 第二步：优化单行查询

#### 2.1 检查生成代码

**需要检查**:
- `samples/SqlxDemo/Generated/...Repository.g.cs`
- 查找返回单个对象的方法，看是否使用了 List

#### 2.2 修改生成器模板

**如果发现使用 List**:
```csharp
// 当前（推测）
public User? GetUserById(int id)
{
    var list = new List<User>();
    while (reader.Read())
    {
        list.Add(new User { ... });
    }
    return list.FirstOrDefault();
}

// 优化
public User? GetUserById(int id)
{
    if (reader.Read())
    {
        return new User { ... };
    }
    return null;
}
```

#### 2.3 复用 SqlCommand

**在生成代码中**:
```csharp
// 为高频方法添加命令缓存
[ThreadStatic]
private static SqlCommand? _getUserByIdCommand;

public User? GetUserById(int id)
{
    var cmd = _getUserByIdCommand;
    if (cmd == null)
    {
        cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT ...";
        cmd.Parameters.Add(new SqlParameter("@id", SqlDbType.Int));
        _getUserByIdCommand = cmd;
    }

    cmd.Parameters[0].Value = id;

    using var reader = cmd.ExecuteReader();
    // ...
}
```

**注意**: 需要在生成器中添加逻辑判断哪些方法是"高频"（例如带 `[Cached]` 特性）

---

## 📊 预期总体改进

### 优化前 vs 优化后

| 场景 | 当前 (μs / B) | 目标 (μs / B) | 改进 |
|------|--------------|--------------|------|
| **拦截器 (1个)** | 3.450 / 712 | 3.350 / 648 | -3% / -9% |
| **拦截器 (3个)** | 3.601 / 760 | 3.450 / 648 | -4% / -15% |
| **拦截器 (8个)** | 3.569 / 840 | 3.450 / 648 | -3% / -23% |
| **单行查询** | 6.982 / 1200 | 6.750 / 904 | -3% / -25% |
| **JOIN查询** | 43.10 / 11048 | 42.50 / 10744 | -1% / -3% |
| **UPDATE操作** | 4.449 / 1176 | 4.200 / 1176 | -6% / 0% |

### 关键指标

- **零GC拦截器**: ✅ 实现
- **单行查询GC**: 1200B → 904B (-25%)
- **整体性能**: 与原生ADO.NET相当
- **vs Dapper**: 保持 12-33% 领先

---

## 🔧 实施检查清单

### 第一阶段（零GC拦截器）- 4.5小时

- [ ] 修改 `SqlxExecutionContext` 为存储 string
- [ ] 更新 `ActivityInterceptor` 优化 SetTag
- [ ] 修改 `CodeGenerationService` 生成代码
- [ ] 运行 InterceptorBenchmark 验证
- [ ] 确认所有拦截器 Allocated = 648B

### 第二阶段（单行查询）- 6.5小时

- [ ] 审查生成的单行查询代码
- [ ] 修改生成器，直接返回对象而非 List
- [ ] 实现 SqlCommand ThreadStatic 缓存
- [ ] 运行 QueryBenchmark 验证
- [ ] 确认 Sqlx_SingleRow Allocated = 904B

### 第三阶段（JOIN查询）- 6.5小时

- [ ] 审查生成的 JOIN 查询代码
- [ ] 使用 Span 优化中间数据处理
- [ ] 运行 ComplexQueryBenchmark 验证
- [ ] 确认 Sqlx_Join Allocated = 10744B

### 第四阶段（UPDATE性能）- 5.5小时

- [ ] Profile UPDATE 方法热点
- [ ] 优化生成代码热路径
- [ ] 运行 CrudBenchmark 验证
- [ ] 确认 Sqlx_Update Mean ≤ 4.200 μs

---

## 🎯 成功标准

### 最低目标（MVP）

- ✅ 拦截器零额外GC（648B）
- ✅ 单行查询GC < 1000B（当前 1200B）
- ✅ 所有场景 vs Dapper 保持领先

### 理想目标

- ✅ 所有场景 vs 原生ADO.NET GC相同
- ✅ 90%+ 场景性能相当或更优（±3%）
- ✅ 拦截器开销 < 5%

---

## 📚 相关文档

- [BENCHMARKS_SUMMARY.md](BENCHMARKS_SUMMARY.md) - Benchmark 结果汇总
- [DESIGN_PRINCIPLES.md](DESIGN_PRINCIPLES.md) - 设计原则
- [GLOBAL_INTERCEPTOR_DESIGN.md](GLOBAL_INTERCEPTOR_DESIGN.md) - 拦截器设计

---

**总结**: 通过 4 个阶段、约 23 小时的优化工作，预计可将 Sqlx 的 GC 分配降低 15-25%，性能提升 3-6%，达到与原生 ADO.NET 相当的水平，同时保持对 Dapper 的全面领先。

核心优化点：**消除拦截器GC泄漏** 和 **优化单行查询路径**。

