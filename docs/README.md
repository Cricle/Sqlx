# Sqlx 文档中心

欢迎来到 Sqlx 的文档中心！这里包含了所有的技术文档、设计文档和使用指南。

---

## 📖 文档导航

### 🚀 快速开始

- **[快速参考指南](QUICK_REFERENCE.md)** - 一页纸快速上手 Sqlx
- **[文档索引](DOCUMENTATION_INDEX.md)** - 完整的文档导航目录

### 📚 核心文档

#### 设计与架构

- **[设计原则](DESIGN_PRINCIPLES.md)** - Fail Fast、零缓存、源生成等核心理念
- **[拦截器设计](GLOBAL_INTERCEPTOR_DESIGN.md)** - 零 GC 拦截器架构详解
- **[简化版拦截器设计](GLOBAL_INTERCEPTOR_DESIGN_SIMPLIFIED.md)** - 简化版设计说明
- **[框架兼容性](FRAMEWORK_COMPATIBILITY.md)** - netstandard2.0 到 .NET 9.0 完整支持

#### 实施与优化

- **[实施总结](IMPLEMENTATION_SUMMARY.md)** - 拦截器功能实施详情
- **[优化进度](OPTIMIZATION_PROGRESS.md)** - 性能优化实施跟踪
- **[最终优化总结](FINAL_OPTIMIZATION_SUMMARY.md)** - 所有优化和Bug修复的完整总结
- **[最终总结](FINAL_SUMMARY.md)** - 项目整体总结

#### 代码审查

- **[代码审查报告](CODE_REVIEW_REPORT.md)** - 核心库代码审查
- **[生成代码审查](GENERATED_CODE_REVIEW.md)** - 源代码生成器输出审查
- **[SQL模板审查](SQL_TEMPLATE_REVIEW.md)** - SQL模板引擎审查

### 📊 性能与测试

- **[Benchmark 分析与优化计划](BENCHMARK_ANALYSIS_AND_OPTIMIZATION_PLAN.md)** - 详细的性能分析和优化方案
- **[Benchmark 总结](BENCHMARKS_SUMMARY.md)** - 性能测试结果汇总
- **[Benchmark 使用指南](README_BENCHMARKS.md)** - 如何运行性能测试

### 📋 项目信息

- **[项目状态](PROJECT_STATUS.md)** - 当前进度和未来规划
- **[更新日志](CHANGELOG.md)** - 版本更新历史
- **[VS 扩展说明](README-VS-EXTENSION.md)** - Visual Studio 扩展使用指南

---

## 🌐 在线文档

访问我们的 [GitHub Pages](https://your-username.github.io/Sqlx/) 查看更友好的在线文档界面。

---

## 📝 文档维护

### 文档结构

```
docs/
├── README.md                                    # 本文件（文档中心首页）
├── web/                                         # GitHub Pages 网站
│   └── index.html                               # 网站首页
├── QUICK_REFERENCE.md                           # 快速参考
├── DOCUMENTATION_INDEX.md                       # 文档索引
├── DESIGN_PRINCIPLES.md                         # 设计原则
├── GLOBAL_INTERCEPTOR_DESIGN.md                 # 拦截器设计
├── FRAMEWORK_COMPATIBILITY.md                   # 框架兼容性
├── FINAL_OPTIMIZATION_SUMMARY.md                # 优化总结
├── BENCHMARK_ANALYSIS_AND_OPTIMIZATION_PLAN.md  # 性能分析
└── ...                                          # 其他文档
```

### 贡献文档

如果您想贡献文档，请：

1. Fork 本仓库
2. 在 `docs/` 目录下创建或修改文档
3. 更新 `DOCUMENTATION_INDEX.md` 索引
4. 提交 Pull Request

---

## 🔗 相关链接

- **GitHub 仓库**: [https://github.com/your-username/Sqlx](https://github.com/your-username/Sqlx)
- **NuGet 包**: [https://www.nuget.org/packages/Sqlx](https://www.nuget.org/packages/Sqlx)
- **问题反馈**: [GitHub Issues](https://github.com/your-username/Sqlx/issues)

---

**提示**: 如果您是第一次使用 Sqlx，建议从 [快速参考指南](QUICK_REFERENCE.md) 开始！
