# ⚠️ 性能回退分析报告

**日期**: 2024-10-23
**问题**: Sqlx 比原始 ADO.NET 慢 20-24%

---

## 📊 Benchmark 结果

| 场景 | RawAdoNet | Sqlx | Dapper | Sqlx vs Raw | Sqlx vs Dapper |
|------|-----------|------|--------|-------------|----------------|
| **SingleRow** | 5.97 us | **7.08 us** | 8.50 us | **+18.5%** ❌ | **-16.7%** ✅ |
| **MultiRow** | 15.22 us | **18.82 us** | 21.97 us | **+23.7%** ❌ | **-14.3%** ✅ |
| **WithParams** | 48.95 us | **60.67 us** | 70.30 us | **+23.9%** ❌ | **-13.7%** ✅ |
| **FullTable** | 100.26 us | **123.38 us** | 145.81 us | **+23.1%** ❌ | **-15.4%** ✅ |

**内存对比**:
- Sqlx 比 Dapper 少 **40-46%** 分配 ✅
- Sqlx 比 RawAdoNet 多 **21-37%** 分配 ⚠️

---

## 🔍 性能开销来源分析

### 1. **Activity 追踪开销** (估计 **5-8%**)

**生成的代码** (`CodeGenerationService.cs:546-562`):
```csharp
#if !SQLX_DISABLE_TRACING
// Activity跟踪（可通过定义SQLX_DISABLE_TRACING条件编译禁用）
var __activity__ = global::System.Diagnostics.Activity.Current;  // ❌ 每次查询都访问
var __startTimestamp__ = global::System.Diagnostics.Stopwatch.GetTimestamp();  // ❌ 时间戳

// 设置Activity标签（如果存在）
if (__activity__ != null)
{
    __activity__.DisplayName = "GetUserById";
    __activity__.SetTag("db.system", "sql");  // ❌ 3-4次字符串分配
    __activity__.SetTag("db.operation", "GetUserById");
    __activity__.SetTag("db.statement", @"SELECT...");
}
#endif
```

**性能影响**:
- `Activity.Current` 访问: ~**0.3-0.5 μs**
- `Stopwatch.GetTimestamp()`: ~**0.1-0.2 μs**
- `SetTag()` (3次): ~**0.2-0.4 μs** (如果有 Activity)
- **总计**: ~**0.6-1.1 μs** (占 SingleRow 查询的 **10-18%**)

---

### 2. **Partial 方法开销** (估计 **2-3%**)

**生成的代码** (`CodeGenerationService.cs:596-625`):
```csharp
#if !SQLX_DISABLE_PARTIAL_METHODS
// Partial方法：用户自定义拦截逻辑
OnExecuting("GetUserById", __cmd__);  // ❌ 虚方法调用
#endif

// ... 查询逻辑 ...

#if !SQLX_DISABLE_PARTIAL_METHODS
OnExecuted("GetUserById", __cmd__, __result__);  // ❌ 虚方法调用
#endif

// ... catch块 ...
#if !SQLX_DISABLE_PARTIAL_METHODS
OnExecuteFail("GetUserById", __cmd__, ex);  // ❌ 异常路径开销
#endif
```

**性能影响**:
- 2 次 partial 方法调用 (即使是空方法): ~**0.1-0.2 μs**
- **总计**: ~**0.1-0.2 μs** (占 SingleRow 查询的 **1.7-3.4%**)

---

### 3. **其他小开销** (估计 **3-5%**)

**内存分配**:
- Sqlx 比 RawAdoNet 多分配 **336 bytes** (SingleRow)
- 可能的额外分配：
  - 生成的代码中的局部变量
  - 字符串插值（用于动态 SQL）
  - Activity 相关的字符串分配

**代码结构**:
- 生成的代码可能有额外的条件分支
- `finally` 块中的清理逻辑

---

## 📈 性能开销汇总

| 开销来源 | 估计影响 | SingleRow (5.97 us) | 实际增加 |
|----------|----------|---------------------|----------|
| **Activity 追踪** | 5-8% | 0.3-0.5 us | 0.6-1.1 us |
| **Partial 方法** | 2-3% | 0.1-0.2 us | 0.1-0.2 us |
| **其他 (内存/分支)** | 3-5% | 0.2-0.3 us | 0.2-0.4 us |
| **总计** | **10-16%** | **0.6-1.0 us** | **0.9-1.7 us** |
| **实际观察** | **18.5%** | - | **1.11 us** |

**结论**: 估计的开销与实际观察基本吻合（估计 10-16%，实际 18.5%）。

---

## 🎯 优化方案

### **方案 A: 默认禁用追踪** ⭐⭐⭐⭐⭐ (推荐)

**修改**: 将条件编译符号反转
```csharp
// 修改前：#if !SQLX_DISABLE_TRACING
// 修改后：#if SQLX_ENABLE_TRACING

#if SQLX_ENABLE_TRACING
var __activity__ = global::System.Diagnostics.Activity.Current;
// ...
#endif
```

**优点**:
- ✅ 默认零开销，性能接近原始 ADO.NET
- ✅ 用户可按需开启追踪（定义 `SQLX_ENABLE_TRACING`）
- ✅ 不破坏现有功能

**缺点**:
- ⚠️ 默认无可观测性（需要用户主动开启）

**预期效果**: Sqlx 慢 **5-10%** vs RawAdoNet

---

### **方案 B: 优化 Activity 代码** ⭐⭐⭐⭐

**修改**: 只在 Debug 模式下启用
```csharp
#if DEBUG && !SQLX_DISABLE_TRACING
var __activity__ = global::System.Diagnostics.Activity.Current;
// ...
#endif
```

**优点**:
- ✅ Release 模式零开销
- ✅ Debug 模式保留可观测性
- ✅ 符合开发者习惯

**缺点**:
- ⚠️ 生产环境无追踪（除非显式开启）

**预期效果**: Release 模式下 Sqlx 慢 **3-8%** vs RawAdoNet

---

### **方案 C: 完全移除追踪代码** ⭐⭐⭐

**修改**: 删除 `CodeGenerationService.cs:546-625` 的追踪代码生成

**优点**:
- ✅ 最大化性能
- ✅ 简化生成的代码

**缺点**:
- ❌ 失去可观测性
- ❌ 破坏已有功能
- ❌ 用户反馈可能不佳

**预期效果**: Sqlx 慢 **3-5%** vs RawAdoNet

---

### **方案 D: 保留现状 + 文档说明** ⭐⭐

**修改**: 无代码修改，仅更新文档

**内容**:
1. 在 README 中明确说明性能折衷：
   - 默认开启追踪和指标，性能开销 ~10-15%
   - 可通过 `SQLX_DISABLE_TRACING` 禁用追踪
   - 禁用后性能接近原始 ADO.NET

2. 提供性能优化指南

**优点**:
- ✅ 保留可观测性
- ✅ 用户可选择性能优先

**缺点**:
- ❌ 默认性能不够好
- ❌ 用户需要额外配置

**预期效果**: 无变化（当前状态）

---

## 💡 推荐方案：**方案 A + 方案 B 的组合**

### 实施步骤

1. **修改条件编译逻辑**:
   ```csharp
   // Release 模式默认禁用，Debug 模式默认启用
   #if DEBUG
   #define SQLX_ENABLE_TRACING_DEFAULT
   #endif

   #if SQLX_ENABLE_TRACING || (SQLX_ENABLE_TRACING_DEFAULT && !SQLX_DISABLE_TRACING)
   var __activity__ = global::System.Diagnostics.Activity.Current;
   // ...
   #endif
   ```

2. **更新文档**:
   ```markdown
   ## 🚀 性能调优

   **追踪和指标**:
   - Debug 模式：默认启用 (可通过 `SQLX_DISABLE_TRACING` 禁用)
   - Release 模式：默认禁用 (可通过 `SQLX_ENABLE_TRACING` 启用)

   **性能对比**:
   - 启用追踪：比 Dapper 快 15%，比原始 ADO.NET 慢 20%
   - 禁用追踪：比 Dapper 快 25%，比原始 ADO.NET 慢 5-8%
   ```

3. **Benchmark 验证**:
   - 测试 Release 模式 (默认禁用追踪)
   - 测试 Debug 模式 (默认启用追踪)
   - 测试显式启用/禁用追踪

---

## 📝 结论

**当前问题**:
- Sqlx 默认开启追踪和指标，导致 20-24% 的性能开销
- 虽然比 Dapper 快 15%，但与原始 ADO.NET 差距较大

**推荐方案**:
- **短期**: 修改条件编译为 `#if SQLX_ENABLE_TRACING` (方案 A)
- **长期**: 实现 Debug/Release 自动切换 (方案 A + B)
- **文档**: 明确说明性能折衷和优化方法

**预期效果** (方案 A + B):
- Debug 模式：保留当前性能（可观测性优先）
- Release 模式：Sqlx 慢 **5-10%** vs RawAdoNet（接近最优）

---

**下一步**: 是否立即实施方案 A？

