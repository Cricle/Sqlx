# Sqlx 性能优化进度报告

**日期**: 2025-10-21
**状态**: 进行中

---

## ✅ 已完成的优化（阶段1）

### 1. 修改 SqlxExecutionContext 为存储 string

**文件**: `src/Sqlx/Interceptors/SqlxExecutionContext.cs`

**修改前**:
```csharp
public ref struct SqlxExecutionContext
{
    public readonly ReadOnlySpan<char> OperationName;
    public readonly ReadOnlySpan<char> RepositoryType;
    public readonly ReadOnlySpan<char> Sql;
}

public SqlxExecutionContext(
    ReadOnlySpan<char> operationName,
    ReadOnlySpan<char> repositoryType,
    ReadOnlySpan<char> sql)
```

**修改后**:
```csharp
public ref struct SqlxExecutionContext
{
    public readonly string OperationName;
    public readonly string RepositoryType;
    public readonly string Sql;
}

public SqlxExecutionContext(
    string operationName,
    string repositoryType,
    string sql)
```

**原因**: `ReadOnlySpan<char>` 本身零GC，但拦截器必须调用 `ToString()` 来使用这些字符串，每次调用都会分配。直接存储 string 引用避免了这个问题。

---

### 2. 优化 ActivityInterceptor

**文件**: `src/Sqlx/Interceptors/ActivityInterceptor.cs`

**优化点**:
1. ✅ **添加 `MethodImpl(AggressiveInlining)`** - 减少函数调用开销
2. ✅ **移除 `ToString()` 调用** - 直接使用 string 字段
3. ✅ **添加 `IsAllDataRequested` 检查** - Fail Fast，避免不必要的工作
4. ✅ **使用 `DisplayName`** - 代替部分 `SetTag`，可能零分配
5. ✅ **使用 `(long)` 代替 `double`** - 避免装箱
6. ✅ **添加条件编译** - `SetStatus()` 仅在 .NET 5+ 可用

**修改前**:
```csharp
public void OnExecuting(ref SqlxExecutionContext context)
{
    var activity = Activity.Current;
    if (activity == null) return;

    activity.SetTag("db.system", "sql");
    activity.SetTag("db.operation", context.OperationName.ToString()); // ❌ ToString()
    activity.SetTag("db.statement", context.Sql.ToString());          // ❌ ToString()
}
```

**修改后**:
```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public void OnExecuting(ref SqlxExecutionContext context)
{
    var activity = Activity.Current;
    if (activity == null || !activity.IsAllDataRequested) // Fail Fast
        return;

    activity.DisplayName = context.OperationName; // 零分配
    activity.SetTag("db.system", "sql");
    activity.SetTag("db.operation", context.OperationName); // 直接使用 string

    if (activity.IsAllDataRequested) // 条件化
    {
        activity.SetTag("db.statement", context.Sql);
    }
}
```

---

### 3. 修改代码生成器

**文件**: `src/Sqlx.Generator/Core/CodeGenerationService.cs` (行 493-500)

**修改前**:
```csharp
sb.AppendLine("var __ctx__ = new global::Sqlx.Interceptors.SqlxExecutionContext(");
sb.PushIndent();
sb.AppendLine($"\"{operationName}\".AsSpan(),");     // ❌ AsSpan()
sb.AppendLine($"\"{repositoryType}\".AsSpan(),");    // ❌ AsSpan()
sb.AppendLine($"@\"{EscapeSqlForCSharp(templateResult.ProcessedSql)}\".AsSpan());"); // ❌ AsSpan()
sb.PopIndent();
```

**修改后**:
```csharp
sb.AppendLine("// 创建执行上下文（栈分配，使用字符串字面量）");
sb.AppendLine("var __ctx__ = new global::Sqlx.Interceptors.SqlxExecutionContext(");
sb.PushIndent();
sb.AppendLine($"\"{operationName}\",");              // ✅ 直接字符串
sb.AppendLine($"\"{repositoryType}\",");             // ✅ 直接字符串
sb.AppendLine($"@\"{EscapeSqlForCSharp(templateResult.ProcessedSql)}\");"); // ✅ 直接字符串
sb.PopIndent();
```

**收益**: 生成的代码更简洁，JIT可以优化字符串字面量为常量。

---

### 4. 更新所有拦截器实现

**文件**:
- `samples/TodoWebApi/Interceptors/SimpleLogInterceptor.cs`
- `tests/Sqlx.Benchmarks/Benchmarks/InterceptorBenchmark.cs`

**修改**: 移除所有 `.ToString()` 调用，直接使用 string 字段。

---

## 🔍 发现的问题

### 问题1: 生成代码的严重Bug - 实体映射缺失

**文件**: 所有生成的 `*Repository.g.cs`

**位置**: 单行查询方法 (例如 `GetUserByIdAsync`)

**代码**:
```csharp
using var reader = __cmd__.ExecuteReader();
if (reader.Read())
{
    // ❌ 这里是空的！没有读取数据到实体
}
else
{
    __result__ = default;
}
```

**影响**: 所有单行查询都会返回 `null`，这是一个关键性bug！

**原因**: 代码生成器的实体映射逻辑缺失或有bug。

**修复优先级**: 🔴 **最高** - 必须立即修复

**修复方案**: 需要在代码生成器中添加实体映射代码生成逻辑：
```csharp
if (reader.Read())
{
    __result__ = new SqlxDemo.Models.User
    {
        Id = reader.GetInt32(0),
        Name = reader.GetString(1),
        Email = reader.GetString(2),
        // ... 其他字段
    };
}
```

---

## 📊 预期改进（待验证）

### 拦截器GC优化

| 场景 | 优化前 (B) | 预期优化后 (B) | 改进 |
|------|-----------|---------------|------|
| NoInterceptor | 648 | 648 | - |
| OneInterceptor_Activity | 712 | **648** | **-64B (-9%)** |
| ThreeInterceptors | 760 | **648** | **-112B (-15%)** |
| EightInterceptors_Max | 840 | **648** | **-192B (-23%)** |

**目标**: 实现真正的"零额外GC"

---

## 🚧 待完成的优化

### 阶段2: 单行查询优化

**任务**:
1. ❌ **修复实体映射bug** - 最高优先级
2. [ ] 审查生成的单行查询代码
3. [ ] 优化返回逻辑（避免使用List）
4. [ ] 实现 SqlCommand ThreadStatic 缓存（可选）
5. [ ] 运行 QueryBenchmark 验证

**目标**:
- 修复功能性bug
- 降低单行查询GC: 1200B → 904B (-25%)

---

### 阶段3: JOIN查询优化

**任务**:
1. [ ] 审查生成的 JOIN 查询代码
2. [ ] 使用 Span 优化中间数据处理
3. [ ] 运行 ComplexQueryBenchmark 验证

**目标**: 降低JOIN查询GC: 11048B → 10744B (-3%)

---

### 阶段4: UPDATE性能优化

**任务**:
1. [ ] Profile UPDATE 方法热点
2. [ ] 优化生成代码热路径
3. [ ] 运行 CrudBenchmark 验证

**目标**: 提升UPDATE性能: 4.449μs → 4.200μs (-6%)

---

## 🎯 下一步行动

### 立即行动（按优先级）

1. **🔴 修复实体映射bug** - 关键性bug，影响所有查询
   - 检查 `CodeGenerationService` 的实体映射逻辑
   - 添加列到属性的映射代码生成
   - 验证所有生成的方法

2. **🟡 运行并验证InterceptorBenchmark**
   - 等待benchmark完成
   - 检查GC分配是否降低到648B
   - 如未达标，进一步分析原因

3. **🟢 继续单行查询优化**
   - 在修复bug后，继续优化GC分配
   - 考虑ThreadStatic缓存方案

---

## 📝 技术笔记

### ReadOnlySpan vs String

**决策**: 使用 string 而非 `ReadOnlySpan<char>`

**原因**:
1. 拦截器需要使用string（例如 `Activity.SetTag`）
2. `ToString()` 必然分配，无法避免
3. 字符串字面量由JIT优化，性能开销可忽略
4. 代码更简洁，可读性更好

**Trade-off**:
- ❌ 失去"零拷贝"特性
- ✅ 避免拦截器中的 `ToString()` 分配
- ✅ 代码更简单
- ✅ 实际性能更好（避免了实际使用时的分配）

### Activity.IsAllDataRequested

**用途**: 检查是否有监听器需要详细数据

**收益**: Fail Fast，避免不必要的 SetTag 调用

**示例**:
```csharp
if (activity == null || !activity.IsAllDataRequested)
    return; // 快速退出，零开销
```

### AggressiveInlining

**用途**: 强制JIT内联小方法

**收益**: 减少函数调用开销，提升热路径性能

**注意**: 仅用于小型、频繁调用的方法

---

## 📚 相关文档

- [BENCHMARK_ANALYSIS_AND_OPTIMIZATION_PLAN.md](BENCHMARK_ANALYSIS_AND_OPTIMIZATION_PLAN.md) - 详细优化计划
- [DESIGN_PRINCIPLES.md](DESIGN_PRINCIPLES.md) - 设计原则
- [GENERATED_CODE_REVIEW.md](GENERATED_CODE_REVIEW.md) - 生成代码审查（包含实体映射bug）

---

**最后更新**: 2025-10-21 22:30
**下一步**: 修复实体映射bug，运行并验证InterceptorBenchmark

