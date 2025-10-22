# Sqlx 性能基准测试

验证 Sqlx 零GC拦截器和源生成性能的完整测试套件。

---

## 📊 测试概览

### 1. 拦截器性能测试（InterceptorBenchmark）

测试拦截器对 SQL 执行性能的影响：

- **无拦截器** - 原始 ADO.NET 基准
- **0个拦截器（禁用）** - 拦截器功能禁用
- **0个拦截器（启用）** - 拦截器功能启用但无注册
- **1个拦截器（Activity）** - Activity 追踪
- **1个拦截器（Counter）** - 简单计数器
- **3个拦截器** - 多拦截器场景
- **8个拦截器** - 最大拦截器数量

### 2. 查询性能测试（QueryBenchmark）

对比不同框架的查询性能：

#### 单行查询
- 原始 ADO.NET
- Dapper
- Sqlx（模拟源生成代码）

#### 多行查询（10行）
- 原始 ADO.NET
- Dapper
- Sqlx

#### 全表查询（100行）
- 原始 ADO.NET
- Dapper
- Sqlx

#### 参数化查询
- 原始 ADO.NET
- Dapper
- Sqlx

### 3. CRUD 操作测试（CrudBenchmark）

测试增删改查操作性能：

#### INSERT 操作
- 单条插入
- 批量插入（10条）

#### UPDATE 操作
- 单条更新

#### DELETE 操作
- 条件删除

### 4. 复杂查询测试（ComplexQueryBenchmark）

测试复杂 SQL 场景：

#### JOIN 查询
- INNER JOIN 多表关联

#### 聚合查询
- GROUP BY、COUNT、AVG、MAX

#### 分页查询
- LIMIT OFFSET

#### 子查询
- 嵌套查询

---

## 🚀 运行测试

### 运行所有测试

```bash
cd tests/Sqlx.Benchmarks
dotnet run -c Release
```

### 运行特定测试类

```bash
# 拦截器性能测试
dotnet run -c Release --filter "*InterceptorBenchmark*"

# 查询性能测试
dotnet run -c Release --filter "*QueryBenchmark*"

# CRUD 操作测试
dotnet run -c Release --filter "*CrudBenchmark*"

# 复杂查询测试
dotnet run -c Release --filter "*ComplexQueryBenchmark*"
```

### 运行特定方法

```bash
# 只测试拦截器的某个方法
dotnet run -c Release --filter "*InterceptorBenchmark.OneInterceptor_Activity*"
```

### 导出结果

```bash
# 导出为 HTML
dotnet run -c Release --exporters html

# 导出为 Markdown
dotnet run -c Release --exporters markdown

# 导出为 CSV
dotnet run -c Release --exporters csv
```

---

## 📊 预期结果

### 拦截器性能影响

基于设计目标，预期结果：

| 场景 | 相对开销 | GC分配 |
|------|---------|--------|
| 原始ADO.NET（基准） | 1.00x | 基准 |
| 0个拦截器（禁用） | 1.00x | +0B |
| 0个拦截器（启用） | 1.00x | +0B |
| 1个拦截器 | 1.01x | +0B |
| 3个拦截器 | 1.03x | +0B |
| 8个拦截器 | 1.05x | +0B |

**预期**: 拦截器开销 <5%，零GC分配（栈分配）

### Sqlx vs Dapper vs ADO.NET

| 操作类型 | Sqlx | Dapper | ADO.NET |
|---------|------|--------|---------|
| **单行查询** | ~1.0x | ~1.1-1.3x | 1.0x |
| **多行查询** | ~1.0x | ~1.1-1.3x | 1.0x |
| **插入** | ~1.0x | ~1.1-1.3x | 1.0x |
| **更新** | ~1.0x | ~1.1-1.3x | 1.0x |
| **JOIN查询** | ~1.0x | ~1.1-1.3x | 1.0x |

**预期**: Sqlx 性能接近手写 ADO.NET，优于 Dapper

### GC 分配对比

| 框架 | 单次查询GC |
|------|-----------|
| **Sqlx** | ~200-300B（仅实体对象） |
| **Dapper** | ~400-600B（反射+缓存） |
| **ADO.NET** | ~200-300B（仅实体对象） |

**预期**: Sqlx 的 GC 分配接近手写代码

---

## 🔍 结果分析

### 查看结果

测试完成后，在 `BenchmarkDotNet.Artifacts/results` 目录查看：

- `*.html` - HTML 格式报告（推荐）
- `*.md` - Markdown 格式
- `*-report-github.md` - GitHub 风格 Markdown
- `*.csv` - CSV 数据

### 关键指标

1. **Mean（平均值）**: 主要关注指标
2. **Allocated（分配内存）**: GC 压力指标
3. **Ratio（相对比例）**: 与基准的比较

### 示例报告解读

```
| Method              | Mean      | Allocated |
|-------------------- |----------:| ---------:|
| RawAdoNet           | 100.0 ns  | 240 B     | <- 基准
| Sqlx                | 102.3 ns  | 240 B     | <- 接近基准
| Dapper              | 125.4 ns  | 520 B     | <- 慢25%，GC多2倍
```

---

## 🎯 性能目标

### Sqlx 核心目标

1. ✅ **零GC拦截器** - 栈分配，无堆分配
2. ✅ **接近手写性能** - 生成的代码等同于手写 ADO.NET
3. ✅ **优于Dapper** - 性能至少持平或更优
4. ✅ **最小开销拦截器** - <5% 性能影响

### 测试通过标准

| 测试项 | 标准 |
|--------|------|
| **拦截器开销** | <5% |
| **拦截器GC** | 0B |
| **查询性能** | ≤ ADO.NET × 1.05 |
| **查询GC** | ≤ ADO.NET + 100B |
| **vs Dapper** | 性能 ≥ 0.9x, GC ≤ 0.6x |

---

## 📝 自定义测试

### 添加新的 Benchmark

```csharp
using BenchmarkDotNet.Attributes;

[MemoryDiagnoser]
public class MyBenchmark
{
    [GlobalSetup]
    public void Setup()
    {
        // 初始化
    }

    [Benchmark(Baseline = true)]
    public void Baseline()
    {
        // 基准测试
    }

    [Benchmark]
    public void MyTest()
    {
        // 你的测试
    }
}
```

### 参数化测试

```csharp
[Params(10, 100, 1000)]
public int RowCount;

[Benchmark]
public List<User> Query()
{
    return QueryUsers(RowCount);
}
```

---

## 🔧 环境要求

- **.NET 8.0 或更高**
- **BenchmarkDotNet 0.13.12**
- **Release 模式**（Debug 模式结果不准确）

---

## 📚 相关文档

- [BenchmarkDotNet 文档](https://benchmarkdotnet.org/)
- [IMPLEMENTATION_SUMMARY.md](../../IMPLEMENTATION_SUMMARY.md) - 拦截器实现总结
- [DESIGN_PRINCIPLES.md](../../DESIGN_PRINCIPLES.md) - 设计原则

---

## ⚠️ 注意事项

1. **必须 Release 模式**: `dotnet run -c Release`
2. **关闭其他程序**: 避免干扰测试
3. **多次运行**: 结果会有波动，多运行几次取平均值
4. **预热**: BenchmarkDotNet 会自动预热，无需手动
5. **GC模式**: 已配置 ServerGC 以获得更准确的结果

---

**提示**: 首次运行可能需要几分钟，请耐心等待。BenchmarkDotNet 会自动进行预热、实际测试和统计分析。

