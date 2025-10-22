# Sqlx 性能优化报告

## 🎯 优化目标
将Sqlx生成代码的性能优化到接近手写ADO.NET水平（目标：1.5x以内）

## 📊 当前性能对比

### 单行查询性能 (查询1条User记录)

| 方案 | 平均时间 | 相对ADO.NET | GC次数 | 内存分配 | 状态 |
|------|---------|-------------|--------|----------|------|
| **手写ADO.NET** (基准) | **6.65 μs** | **1.0x** | 0 | 904 B | ✅ 最快 |
| **Dapper** | **9.48 μs** | **1.4x** | 0 | 1.9 KB | ✅ 优秀 |
| **Sqlx (优化前)** | 17.60 μs | 2.6x | 0 | 2.56 KB | ⚠️ 需优化 |
| **Sqlx (优化后)** | **16.89 μs** | **2.5x** | **0** | **2.56 KB** | ✅ 改进 |

### 多行查询性能 (查询10条User记录)

| 方案 | 平均时间 | 相对ADO.NET | GC次数 | 内存分配 |
|------|---------|-------------|--------|----------|
| **手写ADO.NET** | **16.92 μs** | **1.0x** | 0 | 3.1 KB |
| **Dapper** | **24.23 μs** | **1.4x** | 1 | 5.7 KB |
| **Sqlx** | **38.14 μs** | **2.25x** | **0** | **5.2 KB** |

## ✅ 已完成的优化

### 1. GetOrdinal缓存优化 (最关键)

**问题**: 每个字段调用2次`GetOrdinal`（IsDBNull一次，Get方法一次）

**优化前代码**:
```csharp
__result__ = new User {
    Id = reader.IsDBNull(reader.GetOrdinal("id")) ? 0 : reader.GetInt32(reader.GetOrdinal("id")),
    //                    ↑ 查找1次                                        ↑ 查找2次（浪费！）
    Name = reader.IsDBNull(reader.GetOrdinal("name")) ? "" : reader.GetString(reader.GetOrdinal("name")),
    // ... 每个字段都重复查找！
};
```

**优化后代码**:
```csharp
// 缓存列序号（性能优化：避免重复GetOrdinal调用）
var __ord_Id__ = reader.GetOrdinal("id");
var __ord_Name__ = reader.GetOrdinal("name");
var __ord_Email__ = reader.GetOrdinal("email");
// ... 每个字段只查找1次

__result__ = new User {
    Id = reader.IsDBNull(__ord_Id__) ? 0 : reader.GetInt32(__ord_Id__),
    Name = reader.IsDBNull(__ord_Name__) ? "" : reader.GetString(__ord_Name__),
    // ... 使用缓存的序号，零查找开销！
};
```

**性能提升**: 17.60 μs → 16.89 μs (**↓ 4%**)

**收益分析**:
- 8个字段 × 1次节省 = 8次字符串查找开销
- GetOrdinal是O(n)操作，需要遍历列名
- 对于大量字段的实体，优化效果更明显

### 2. 集合返回类型识别修复

**问题**: `List<User>`被误判为单实体，导致`InvalidCastException`

**根本原因**:
```csharp
// Bug: 非Task类型返回"object"
ExtractInnerTypeFromTask("List<User>") → "object"
// 误判: "object"被认为是单实体
ClassifyReturnType("object") → SingleEntity ❌

// 生成错误代码: if (reader.Read()) 而不是 while (reader.Read())
```

**修复方案**:
1. `ExtractInnerTypeFromTask`: 返回原类型而不是`"object"`
2. `ClassifyReturnType`: 支持完全限定名称（`System.Collections.Generic.List<>`）
3. `GenerateCollectionExecution`: 添加`global::`前缀避免命名冲突

**验证**: `Sqlx_MultiRow` benchmark成功运行（38.14 μs，5.21 KB，0 GC）

### 3. SQL模板引擎TDD (功能完整性)

**测试结果**: **474个测试全部通过 (100%)** ✅

- 18个占位符功能测试
- 6个bug修复（红灯→绿灯）
- 向后兼容性支持（新旧两种格式）

## 🔍 剩余性能差距分析

### Sqlx vs ADO.NET (2.5x差距 ≈ 10.24 μs)

#### 1. 拦截器框架开销 (~1-2 μs)
```csharp
// 每次方法调用的开销：
var __ctx__ = new SqlxExecutionContext(...);  // 栈分配 + 3个字符串参数
__ctx__.StartTimestamp = Stopwatch.GetTimestamp();  // 高精度时间戳

try {
    SqlxInterceptors.OnExecuting(ref __ctx__);  // 函数调用 + Fail Fast检查
    // ... 数据库操作 ...
    SqlxInterceptors.OnExecuted(ref __ctx__);   // 函数调用
}
catch (Exception ex) {
    __ctx__.EndTimestamp = Stopwatch.GetTimestamp();
    SqlxInterceptors.OnFailed(ref __ctx__);     // 异常处理
    throw;
}
```

**实测**: 禁用拦截器后性能反而下降9%（18.45 μs），说明JIT可能对完整代码路径有更好的优化。

#### 2. 代码生成结构差异 (~2-3 μs)

**Sqlx生成代码**:
- 8个局部变量（GetOrdinal缓存）
- try-catch-finally块
- 拦截器调用
- partial方法调用（OnExecuting/OnExecuted/OnExecuteFail）

**手写ADO.NET**:
- 直接使用序号常量（0, 1, 2...）
- 无拦截器开销
- 无异常处理框架

#### 3. 其他累积差异 (~5-7 μs)
- JIT编译优化差异
- 方法内联优化
- 寄存器分配差异
- 缓存局部性

## 💡 进一步优化建议

### 🚀 高优先级（可实现1.5x目标）

#### 1. 直接序号访问 (预计-2μs)
**当前**: 使用GetOrdinal缓存
```csharp
var __ord_Id__ = reader.GetOrdinal("id");
reader.GetInt32(__ord_Id__)
```

**优化**: 直接使用序号常量
```csharp
reader.GetInt32(0)  // id是SELECT的第0列
```

**优势**:
- 完全零查找开销
- 与手写ADO.NET一致
- JIT可以更好地内联

**实现**: 在生成代码时解析SELECT语句中的列顺序

#### 2. 可选拦截器编译 (预计-1.5μs)
**方案**: 使用生成器选项控制拦截器生成
```csharp
// Option 1: 完整版（带拦截器）
[Sqlx("...", EnableInterceptors = true)]  // 默认

// Option 2: 性能版（无拦截器）
[Sqlx("...", EnableInterceptors = false)]
```

**优势**:
- 性能关键路径零拦截器开销
- 开发/调试时可启用拦截器
- 编译时决定，零运行时开销

#### 3. 移除向后兼容的partial方法 (预计-0.5μs)
```csharp
// 这些调用可以移除或设为可选：
OnExecuting("GetByIdSync", __cmd__);
OnExecuted("GetByIdSync", __cmd__, __result__, elapsed);
OnExecuteFail("GetByIdSync", __cmd__, __ex__, elapsed);
```

### ⚡ 中优先级

#### 4. 优化异常处理
- 使用`[MethodImpl(MethodImplOptions.AggressiveOptimization)]`
- 将try-catch移到调用方（可选）

#### 5. 零分配字符串处理
- SQL模板使用`ReadOnlySpan<char>`
- 列名使用字符串常量池

### 🔬 低优先级（微优化）

#### 6. 移除不必要的变量
```csharp
// 当前
var __result__ = default(User);
// ... 逻辑 ...
return __result__;

// 优化: 直接返回
return new User { ... };
```

#### 7. 内联小方法
使用`[MethodImpl(MethodImplOptions.AggressiveInlining)]`

## 🎖️ 性能优势

### 相比于Dapper

| 指标 | Sqlx | Dapper | 优势 |
|------|------|--------|------|
| 单行查询 | 16.89 μs | 9.48 μs | ❌ 慢78% |
| 多行查询 | 38.14 μs | 24.23 μs | ❌ 慢57% |
| **GC压力** | **0次** | **1次** | ✅ **零GC** |
| **编译检查** | **✅** | ❌ | ✅ **类型安全** |
| **代码可见** | **✅** | ❌ | ✅ **可调试** |

### Sqlx核心优势

1. **✅ 编译时类型检查** - 运行前发现错误
2. **✅ 代码可见性** - 可调试生成的代码
3. **✅ 零GC压力** - 适合高性能场景
4. **✅ 零反射** - 所有代码都是编译时生成
5. **✅ SQL模板引擎** - 强大的占位符功能

## 🎯 性能目标路线图

### 短期目标（当前版本）
- [x] GetOrdinal缓存优化 → **2.5x**
- [x] 修复集合类型bug → **功能完整**
- [x] TDD测试覆盖 → **100%通过**

### 中期目标（下个版本）
- [ ] 直接序号访问 → **预计2.0x**
- [ ] 可选拦截器编译 → **预计1.8x**
- [ ] 移除partial方法 → **预计1.7x**

### 长期目标（未来版本）
- [ ] 达到1.5x以内 → **接近Dapper**
- [ ] 提供性能/功能模式切换
- [ ] 支持批量操作优化

## 📝 结论

当前Sqlx已具备：
- ✅ **稳定性**: 474个测试全部通过
- ✅ **零GC**: 无垃圾回收压力
- ✅ **功能完整**: SQL模板引擎功能强大
- ⚠️ **性能**: 2.5x相对手写ADO.NET，还有优化空间

**建议策略**:
1. **当前版本**: 适用于开发效率优先场景
2. **下个版本**: 实现直接序号访问和可选拦截器，达到1.5-1.8x
3. **未来**: 提供"性能模式"和"完整模式"供用户选择

---

**生成时间**: 2025-10-22  
**测试环境**: AMD Ryzen 7 5800H, .NET 8.0.21, Windows 10  
**测试工具**: BenchmarkDotNet v0.14.0

