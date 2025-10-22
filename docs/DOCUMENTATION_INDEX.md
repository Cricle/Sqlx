# Sqlx 文档索引

完整的 Sqlx 文档导航。

📖 **[文档结构说明](DOCUMENTATION_STRUCTURE.md)** | 🏠 **[文档中心首页](README.md)** | 🌐 **[GitHub Pages](web/index.html)**

---

## 📚 核心文档

### 设计与原则

| 文档 | 说明 | 重点 |
|------|------|------|
| [DESIGN_PRINCIPLES.md](DESIGN_PRINCIPLES.md) | **核心设计原则** | Fail Fast、无缓存、源生成 |
| [GLOBAL_INTERCEPTOR_DESIGN.md](GLOBAL_INTERCEPTOR_DESIGN.md) | 拦截器设计 | 零GC、多拦截器、Activity集成 |
| [FRAMEWORK_COMPATIBILITY.md](FRAMEWORK_COMPATIBILITY.md) | **框架兼容性** | 所有框架版本支持 |
| [SQL_TEMPLATE_REVIEW.md](SQL_TEMPLATE_REVIEW.md) | SQL模板审查 | 性能优化、GC、安全 |

### 实施文档

| 文档 | 说明 | 内容 |
|------|------|------|
| [IMPLEMENTATION_SUMMARY.md](IMPLEMENTATION_SUMMARY.md) | **拦截器实施总结** | 完整实施过程和成果 |
| [GENERATED_CODE_REVIEW.md](GENERATED_CODE_REVIEW.md) | 生成代码审查 | 发现的Bug和修复建议 |
| [CODE_REVIEW_REPORT.md](CODE_REVIEW_REPORT.md) | 代码审查报告 | 全面的代码质量分析 |

### 性能测试

| 文档 | 说明 | 内容 |
|------|------|------|
| [BENCHMARKS_SUMMARY.md](BENCHMARKS_SUMMARY.md) | **性能测试总结** | 完整的测试覆盖和目标 |
| [tests/Sqlx.Benchmarks/README.md](tests/Sqlx.Benchmarks/README.md) | 测试使用指南 | 如何运行和分析结果 |
| [tests/Sqlx.Benchmarks/BENCHMARKS_IMPLEMENTATION.md](tests/Sqlx.Benchmarks/BENCHMARKS_IMPLEMENTATION.md) | 测试实施文档 | 测试代码的实施细节 |

### 示例和使用

| 文档 | 说明 | 内容 |
|------|------|------|
| [samples/TodoWebApi/INTERCEPTOR_USAGE.md](samples/TodoWebApi/INTERCEPTOR_USAGE.md) | **拦截器使用指南** | 完整的使用示例 |
| [samples/TodoWebApi/README.md](samples/TodoWebApi/README.md) | TodoWebApi示例 | 完整的Web API示例 |
| [samples/SqlxDemo/README.md](samples/SqlxDemo/README.md) | SqlxDemo示例 | 基础功能演示 |

---

## 🎯 快速导航

### 我想了解...

#### **设计哲学**
→ [DESIGN_PRINCIPLES.md](DESIGN_PRINCIPLES.md)
核心：Fail Fast、编译时计算、零运行时缓存

#### **如何使用拦截器**
→ [samples/TodoWebApi/INTERCEPTOR_USAGE.md](samples/TodoWebApi/INTERCEPTOR_USAGE.md)
包含：注册、自定义、性能影响

#### **性能如何**
→ [BENCHMARKS_SUMMARY.md](BENCHMARKS_SUMMARY.md)
对比：vs ADO.NET vs Dapper

#### **如何运行测试**
→ [tests/Sqlx.Benchmarks/README.md](tests/Sqlx.Benchmarks/README.md)
命令：`dotnet run -c Release`

#### **拦截器如何实现**
→ [IMPLEMENTATION_SUMMARY.md](IMPLEMENTATION_SUMMARY.md)
技术：ref struct、栈分配、Activity

#### **拦截器设计细节**
→ [GLOBAL_INTERCEPTOR_DESIGN.md](GLOBAL_INTERCEPTOR_DESIGN.md)
深入：零GC、多拦截器、性能优化

---

## 📖 阅读顺序

### 🟢 新手入门

1. [DESIGN_PRINCIPLES.md](DESIGN_PRINCIPLES.md) - 了解设计理念
2. [samples/TodoWebApi/INTERCEPTOR_USAGE.md](samples/TodoWebApi/INTERCEPTOR_USAGE.md) - 学习如何使用
3. [samples/TodoWebApi/README.md](samples/TodoWebApi/README.md) - 运行示例

### 🟡 深入理解

4. [IMPLEMENTATION_SUMMARY.md](IMPLEMENTATION_SUMMARY.md) - 实施细节
5. [GLOBAL_INTERCEPTOR_DESIGN.md](GLOBAL_INTERCEPTOR_DESIGN.md) - 设计深入
6. [SQL_TEMPLATE_REVIEW.md](SQL_TEMPLATE_REVIEW.md) - SQL模板优化

### 🔴 性能优化

7. [BENCHMARKS_SUMMARY.md](BENCHMARKS_SUMMARY.md) - 性能总结
8. [tests/Sqlx.Benchmarks/README.md](tests/Sqlx.Benchmarks/README.md) - 运行测试
9. [CODE_REVIEW_REPORT.md](CODE_REVIEW_REPORT.md) - 代码优化

---

## 🔥 核心概念

### 三大设计原则

1. **Fail Fast** - 异常不吞噬，问题立即可见
2. **无运行时缓存** - 编译时计算，生成硬编码常量
3. **充分利用源生成** - 零反射、零动态字符串

### 拦截器特性

- ✅ **零GC** - ref struct 栈分配
- ✅ **低开销** - <5% 性能影响
- ✅ **多拦截器** - 最多8个
- ✅ **Activity集成** - 兼容 OpenTelemetry

### 性能目标

- ✅ **vs ADO.NET** - <5% 性能差异
- ✅ **vs Dapper** - 快10-30%，GC少50%+
- ✅ **拦截器** - 0B GC，<5% 开销

---

## 📊 文档统计

| 类别 | 文档数 | 总行数 |
|------|--------|--------|
| **设计文档** | 3 | ~2,050 |
| **实施文档** | 3 | ~2,195 |
| **性能测试** | 3 | ~1,700 |
| **示例文档** | 3 | ~800 |
| **总计** | 12 | **~6,745** |

---

## 🎯 文档状态

| 文档 | 状态 | 最后更新 |
|------|------|----------|
| DESIGN_PRINCIPLES.md | ✅ 完成 | 2025-10-21 |
| GLOBAL_INTERCEPTOR_DESIGN.md | ✅ 完成 | 2025-10-21 |
| IMPLEMENTATION_SUMMARY.md | ✅ 完成 | 2025-10-21 |
| BENCHMARKS_SUMMARY.md | ✅ 完成 | 2025-10-21 |
| SQL_TEMPLATE_REVIEW.md | ✅ 完成 | 2025-10-21 |
| tests/Sqlx.Benchmarks/README.md | ✅ 完成 | 2025-10-21 |
| samples/TodoWebApi/INTERCEPTOR_USAGE.md | ✅ 完成 | 2025-10-21 |

---

## 🚀 快速命令

```bash
# 查看所有文档
ls -l *.md docs/*.md samples/*/README.md tests/*/README.md

# 搜索特定内容
grep -r "零GC" *.md

# 生成文档目录
find . -name "*.md" -type f | sort

# 统计文档行数
wc -l *.md
```

---

## 📝 贡献指南

### 文档风格

- ✅ 使用清晰的标题层级
- ✅ 代码示例带注释
- ✅ 性能数据用表格
- ✅ 关键概念加粗
- ✅ 中文为主，技术术语英文

### 更新文档

1. 修改相关 `.md` 文件
2. 更新本索引文件
3. 检查交叉引用链接
4. 提交 PR

---

## 🔗 外部资源

- [BenchmarkDotNet 文档](https://benchmarkdotnet.org/)
- [OpenTelemetry .NET](https://opentelemetry.io/docs/instrumentation/net/)
- [.NET Activity](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.activity)
- [Source Generators](https://docs.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/source-generators-overview)

---

**文档总览** | **12篇文档** | **~6,745行** | **完全覆盖设计、实施、测试、使用**

