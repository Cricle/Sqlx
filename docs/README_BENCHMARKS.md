# Sqlx 性能基准测试

全面的性能测试套件，验证 Sqlx 的零GC拦截器和高性能设计。

---

## 快速开始

```bash
cd tests/Sqlx.Benchmarks
dotnet run -c Release
```

---

## 测试覆盖

### 拦截器性能 (7个测试)
验证零GC栈分配和低开销设计

- 无拦截器 vs 1/3/8个拦截器
- Activity追踪 vs 简单计数器
- **目标**: <5%开销，0B GC分配

### 查询性能 (12个测试)
对比 Sqlx vs Dapper vs ADO.NET

- 单行/多行/全表查询
- 参数化查询
- **目标**: Sqlx ≈ ADO.NET，优于 Dapper

### CRUD操作 (9个测试)
增删改查性能测试

- INSERT/UPDATE/DELETE
- 批量操作（事务）
- **目标**: 接近手写代码性能

### 复杂查询 (12个测试)
真实业务场景

- JOIN/聚合/分页/子查询
- **目标**: 各场景性能稳定

---

## 运行测试

```bash
# 全部测试（~30分钟）
dotnet run -c Release --exporters html markdown

# 快速验证（~3分钟）
dotnet run -c Release --filter "*InterceptorBenchmark*"

# 单个测试
dotnet run -c Release --filter "*QueryBenchmark.Sqlx_SingleRow*"
```

---

## 查看结果

```bash
# 结果位置
cd BenchmarkDotNet.Artifacts/results

# 打开报告
start *-report.html  # Windows
open *-report.html   # macOS
```

---

## 性能目标

| 指标 | 目标 | 优先级 |
|------|------|--------|
| 拦截器GC | 0B | P0 ❌ |
| 拦截器开销 | <5% | P0 ❌ |
| Sqlx vs ADO.NET | <5% | P1 ⚠️ |
| Sqlx vs Dapper | 快10%+ | P1 ⚠️ |

---

## 项目结构

```
tests/Sqlx.Benchmarks/
├── Benchmarks/
│   ├── InterceptorBenchmark.cs      # 拦截器性能
│   ├── QueryBenchmark.cs            # 查询对比
│   ├── CrudBenchmark.cs             # CRUD操作
│   └── ComplexQueryBenchmark.cs     # 复杂查询
├── Models/User.cs                   # 测试实体
└── Program.cs                       # 入口
```

---

## 依赖

- .NET 8.0+
- BenchmarkDotNet 0.14.0
- Microsoft.Data.Sqlite 8.0.8
- Dapper 2.1.35

---

## 注意事项

⚠️ **必须 Release 模式** - Debug 模式结果不准确
⚠️ **关闭其他程序** - 避免干扰测试
⚠️ **多次运行** - 结果有波动，取平均值

---

## 相关文档

- [BENCHMARKS_SUMMARY.md](../BENCHMARKS_SUMMARY.md) - 完整总结
- [DESIGN_PRINCIPLES.md](../DESIGN_PRINCIPLES.md) - 设计原则
- [IMPLEMENTATION_SUMMARY.md](../IMPLEMENTATION_SUMMARY.md) - 拦截器实施

---

**总测试数**: 40个
**总代码量**: ~2,750行
**测试框架**: BenchmarkDotNet

