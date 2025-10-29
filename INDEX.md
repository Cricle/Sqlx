# Sqlx - Complete Documentation Index

> **📚 所有资源的完整导航索引**

欢迎来到Sqlx！这是您的一站式文档导航。

---

## 🚀 快速开始

**新用户？从这里开始：**

| 文档 | 描述 | 时间 |
|------|------|------|
| [README.md](README.md) | 项目主页，快速了解 | 5分钟 |
| [INSTALL.md](INSTALL.md) | 安装指南，4种方法 | 10分钟 |
| [docs/QUICK_START_GUIDE.md](docs/QUICK_START_GUIDE.md) | 5分钟上手教程 | 5分钟 |
| [QUICK_REFERENCE.md](QUICK_REFERENCE.md) | 一页纸速查表 | 随时查阅 |

**推荐学习路径：**
```
1. README.md (了解项目)
   ↓
2. INSTALL.md (安装配置)
   ↓
3. QUICK_START_GUIDE.md (快速上手)
   ↓
4. TUTORIAL.md (深入学习)
   ↓
5. BEST_PRACTICES.md (最佳实践)
```

---

## 📖 核心文档

### 学习教程

| 文档 | 内容 | 适合 |
|------|------|------|
| [TUTORIAL.md](TUTORIAL.md) | 10课完整教程 | 新手→高级 |
| [docs/QUICK_START_GUIDE.md](docs/QUICK_START_GUIDE.md) | 5分钟快速上手 | 快速体验 |
| [docs/API_REFERENCE.md](docs/API_REFERENCE.md) | 完整API参考 | 查阅使用 |
| [docs/PLACEHOLDERS.md](docs/PLACEHOLDERS.md) | 占位符详解 | 深入理解 |

### 最佳实践

| 文档 | 内容 | 价值 |
|------|------|------|
| [docs/BEST_PRACTICES.md](docs/BEST_PRACTICES.md) | 推荐用法 | 代码质量 |
| [docs/ADVANCED_FEATURES.md](docs/ADVANCED_FEATURES.md) | 高级特性 | 深入使用 |
| [PERFORMANCE.md](PERFORMANCE.md) | 性能优化 | 极致性能 |

---

## 🔄 迁移与对比

**从其他ORM迁移到Sqlx：**

| 文档 | 内容 | 适用 |
|------|------|------|
| [MIGRATION_GUIDE.md](MIGRATION_GUIDE.md) | 完整迁移指南 | 所有ORM |
| → 从EF Core迁移 | CRUD对比、策略 | EF用户 |
| → 从Dapper迁移 | 代码对比、步骤 | Dapper用户 |
| → 从ADO.NET迁移 | 代码简化 | ADO.NET用户 |

**性能对比：**

| 文档 | 内容 | 亮点 |
|------|------|------|
| [PERFORMANCE.md](PERFORMANCE.md) | 详细基准测试 | 7个测试场景 |
| → vs ADO.NET | 105% (快5%) | 编译时优化 |
| → vs Dapper | 100% (持平) | 相同性能 |
| → vs EF Core | 175% (快75%) | 显著优势 |
| → 批量操作 | 25倍速度 | 核心优势 |

---

## 🆘 帮助与支持

### 遇到问题？

| 文档 | 解决什么 | 何时查看 |
|------|---------|---------|
| [FAQ.md](FAQ.md) | 35+常见问题 | 快速答疑 |
| [TROUBLESHOOTING.md](TROUBLESHOOTING.md) | 故障排除 | 遇到错误 |
| [INSTALL.md](INSTALL.md) | 安装问题 | 安装失败 |
| [GitHub Issues](https://github.com/Cricle/Sqlx/issues) | Bug报告 | 新问题 |
| [GitHub Discussions](https://github.com/Cricle/Sqlx/discussions) | 讨论交流 | 想法分享 |

### FAQ快速查找

**安装相关：**
- [如何安装Sqlx？](FAQ.md#q-如何安装sqlx)
- [VS Extension是必需的吗？](FAQ.md#q-vs-extension是必需的吗)
- [代码为什么没有生成？](FAQ.md#q-为什么我的代码没有生成)

**功能相关：**
- [什么是SqlTemplate？](FAQ.md#q-什么是sqltemplate)
- [什么是占位符？](FAQ.md#q-什么是占位符)
- [如何使用新的Repository接口？](FAQ.md#q-如何使用新的repository接口)

**性能相关：**
- [Sqlx真的比EF Core快吗？](FAQ.md#q-sqlx真的比ef-core快吗)
- [如何优化查询性能？](FAQ.md#q-如何优化查询性能)

---

## 🛠️ Visual Studio Extension

### Extension文档

| 文档 | 内容 | 用途 |
|------|------|------|
| [src/Sqlx.Extension/README.md](src/Sqlx.Extension/README.md) | Extension概述 | 了解功能 |
| [docs/VS_EXTENSION_ENHANCEMENT_PLAN.md](docs/VS_EXTENSION_ENHANCEMENT_PLAN.md) | 完整计划 | 功能规划 |
| [src/Sqlx.Extension/VS_EXTENSION_IMPLEMENTATION_STATUS.md](src/Sqlx.Extension/VS_EXTENSION_IMPLEMENTATION_STATUS.md) | 实现状态 | 完成度 |

### 核心功能

**14个工具窗口：**
1. SQL Preview - 实时SQL预览
2. Generated Code - 生成代码查看
3. Query Tester - 查询测试器
4. Repository Explorer - 仓储浏览器
5. SQL Execution Log - 执行日志
6. Template Visualizer - 模板可视化
7. Performance Analyzer - 性能分析器
8. Entity Mapping Viewer - 映射查看器
9. SQL Breakpoints - 断点管理
10. SQL Watch - 监视窗口
11-14. (其他辅助窗口)

**其他功能：**
- 5色SQL语法着色
- 44+项IntelliSense
- 12个代码片段
- 2个快速操作
- 实时诊断

---

## 🤝 贡献指南

### 想要贡献？

| 文档 | 内容 | 适用 |
|------|------|------|
| [CONTRIBUTING.md](CONTRIBUTING.md) | 完整贡献指南 | 所有贡献者 |
| → 行为准则 | 社区规范 | 了解规则 |
| → 报告Bug | Bug模板 | 报告问题 |
| → 建议功能 | 功能请求 | 提出想法 |
| → 代码贡献 | PR流程 | 提交代码 |
| → 文档改进 | 文档规范 | 改进文档 |

### 发布流程

| 文档 | 内容 | 适用 |
|------|------|------|
| [HOW_TO_RELEASE.md](HOW_TO_RELEASE.md) | 完整发布流程 | 维护者 |
| [VERSION](VERSION) | 版本管理 | 版本信息 |
| [CHANGELOG.md](CHANGELOG.md) | 变更日志 | 版本历史 |

---

## 📊 项目管理

### 项目状态

| 文档 | 内容 | 更新 |
|------|------|------|
| [PROJECT_STATUS.md](PROJECT_STATUS.md) | 当前状态 | 实时 |
| [PROJECT_DELIVERABLES.md](PROJECT_DELIVERABLES.md) | 交付清单 | 完整 |
| [ULTIMATE_PROJECT_COMPLETION.md](ULTIMATE_PROJECT_COMPLETION.md) | 完成报告 | 终极 |

### 版本规划

| 版本 | 状态 | 文档 |
|------|------|------|
| v0.5.0-preview | ✅ 当前 | [CHANGELOG.md](CHANGELOG.md#050-preview-2025-10-29) |
| v0.6.0 | 📅 计划 | [VERSION](VERSION#v060-planned) |
| v1.0.0 | 📅 计划 | [VERSION](VERSION#v100-planned---major) |
| v2.0.0 | 🔮 未来 | [VERSION](VERSION#v200-future) |

---

## 🎯 按使用场景导航

### 场景1: 我是新用户
```
1. README.md - 了解Sqlx是什么
2. INSTALL.md - 安装Sqlx
3. QUICK_START_GUIDE.md - 5分钟上手
4. TUTORIAL.md 第1-3课 - 基础学习
5. QUICK_REFERENCE.md - 作为速查表
```

### 场景2: 我要从EF Core迁移
```
1. MIGRATION_GUIDE.md - 阅读EF Core迁移部分
2. PERFORMANCE.md - 了解性能提升
3. FAQ.md - 查看常见问题
4. TUTORIAL.md - 学习Sqlx用法
5. BEST_PRACTICES.md - 掌握最佳实践
```

### 场景3: 我要使用VS Extension
```
1. INSTALL.md - 安装Extension
2. src/Sqlx.Extension/README.md - 了解功能
3. TUTORIAL.md 第3-6课 - 学习Extension功能
4. QUICK_REFERENCE.md - 查看快捷键和片段
5. TROUBLESHOOTING.md - 解决可能的问题
```

### 场景4: 我要优化性能
```
1. PERFORMANCE.md - 了解性能基准
2. BEST_PRACTICES.md - 学习最佳实践
3. docs/ADVANCED_FEATURES.md - 使用高级特性
4. TUTORIAL.md 第8课 - 性能优化技巧
5. 使用Performance Analyzer工具
```

### 场景5: 我遇到了问题
```
1. FAQ.md - 快速查找常见问题
2. TROUBLESHOOTING.md - 详细故障排除
3. GitHub Issues - 搜索类似问题
4. GitHub Discussions - 寻求社区帮助
5. CONTRIBUTING.md - 如果要报告新Bug
```

---

## 📁 文档分类索引

### 按文档类型

#### 📘 教程类 (4篇)
- [TUTORIAL.md](TUTORIAL.md) - 10课完整教程
- [QUICK_START_GUIDE.md](docs/QUICK_START_GUIDE.md) - 5分钟上手
- [INSTALL.md](INSTALL.md) - 安装指南
- [MIGRATION_GUIDE.md](MIGRATION_GUIDE.md) - 迁移指南

#### 📕 参考类 (5篇)
- [API_REFERENCE.md](docs/API_REFERENCE.md) - API完整参考
- [PLACEHOLDERS.md](docs/PLACEHOLDERS.md) - 占位符参考
- [QUICK_REFERENCE.md](QUICK_REFERENCE.md) - 快速参考卡
- [VERSION](VERSION) - 版本信息
- [CHANGELOG.md](CHANGELOG.md) - 变更日志

#### 📗 指南类 (5篇)
- [BEST_PRACTICES.md](docs/BEST_PRACTICES.md) - 最佳实践
- [ADVANCED_FEATURES.md](docs/ADVANCED_FEATURES.md) - 高级特性
- [PERFORMANCE.md](PERFORMANCE.md) - 性能优化
- [CONTRIBUTING.md](CONTRIBUTING.md) - 贡献指南
- [HOW_TO_RELEASE.md](HOW_TO_RELEASE.md) - 发布指南

#### 📙 帮助类 (3篇)
- [FAQ.md](FAQ.md) - 常见问题
- [TROUBLESHOOTING.md](TROUBLESHOOTING.md) - 故障排除
- [README.md](README.md) - 项目主页

---

## 🌐 在线资源

### 官方资源
- **GitHub Pages**: https://cricle.github.io/Sqlx/
- **GitHub仓库**: https://github.com/Cricle/Sqlx
- **NuGet包**: https://www.nuget.org/packages/Sqlx/
- **VS Extension**: [GitHub Releases](https://github.com/Cricle/Sqlx/releases)

### 社区
- **Issues**: https://github.com/Cricle/Sqlx/issues
- **Discussions**: https://github.com/Cricle/Sqlx/discussions
- **Pull Requests**: https://github.com/Cricle/Sqlx/pulls

---

## 🎓 学习路线

### 初级 (新手)
```
第1周:
□ README.md
□ INSTALL.md
□ QUICK_START_GUIDE.md
□ TUTORIAL.md (第1-3课)
□ 完成第一个Repository

第2周:
□ TUTORIAL.md (第4-6课)
□ 学习VS Extension基础功能
□ QUICK_REFERENCE.md (作为速查)
□ 开始小项目实践
```

### 中级 (进阶)
```
第3-4周:
□ TUTORIAL.md (第7-10课)
□ BEST_PRACTICES.md
□ ADVANCED_FEATURES.md
□ 学习批量操作
□ 性能优化实践
□ 完成中型项目
```

### 高级 (精通)
```
持续学习:
□ PERFORMANCE.md (深入性能)
□ 阅读源代码
□ 参与社区讨论
□ 贡献代码
□ 帮助他人
```

---

## 📝 文档贡献

**发现文档问题？**
1. [报告Issue](https://github.com/Cricle/Sqlx/issues/new)
2. 提交PR改进
3. 在Discussions讨论

**文档改进建议：**
- 修正拼写/语法错误
- 添加缺失的示例
- 改进说明清晰度
- 补充新的FAQ
- 翻译文档

---

## 🔍 快速查找

### 常用命令
```bash
# 安装
dotnet add package Sqlx
dotnet add package Sqlx.Generator

# 构建
dotnet build

# 测试
dotnet test

# 运行示例
cd samples/FullFeatureDemo
dotnet run
```

### 常用链接
- **主页**: [README.md](README.md)
- **安装**: [INSTALL.md](INSTALL.md)
- **教程**: [TUTORIAL.md](TUTORIAL.md)
- **API**: [docs/API_REFERENCE.md](docs/API_REFERENCE.md)
- **FAQ**: [FAQ.md](FAQ.md)

---

## 📊 文档统计

```
总文档数:    36篇
总页数:      600+
总字数:      ~200,000
语言:        中文为主
格式:        Markdown
更新频率:    持续更新
```

---

## 🎯 推荐阅读顺序

### 对于不同角色

**开发者 (使用Sqlx):**
```
1. README.md
2. INSTALL.md
3. QUICK_START_GUIDE.md
4. TUTORIAL.md
5. BEST_PRACTICES.md
6. QUICK_REFERENCE.md (常备)
7. FAQ.md (需要时)
```

**架构师 (技术选型):**
```
1. README.md
2. PERFORMANCE.md
3. MIGRATION_GUIDE.md
4. API_REFERENCE.md
5. ADVANCED_FEATURES.md
```

**贡献者 (参与开发):**
```
1. README.md
2. CONTRIBUTING.md
3. 源代码
4. HOW_TO_RELEASE.md
5. PROJECT_DELIVERABLES.md
```

---

## 💡 提示和技巧

### 高效使用文档
```
✅ 使用浏览器搜索 (Ctrl+F)
✅ 查看文档目录快速定位
✅ 收藏常用文档
✅ 使用QUICK_REFERENCE.md作为速查表
✅ 遇到问题先查FAQ和TROUBLESHOOTING
```

### 保持更新
```
✅ Watch GitHub仓库获取更新
✅ 查看CHANGELOG了解新特性
✅ 关注Discussions的公告
✅ 定期检查文档更新
```

---

## 📞 需要帮助？

### 按优先级
1. **自助**: [FAQ.md](FAQ.md), [TROUBLESHOOTING.md](TROUBLESHOOTING.md)
2. **搜索**: [GitHub Issues](https://github.com/Cricle/Sqlx/issues)
3. **询问**: [GitHub Discussions](https://github.com/Cricle/Sqlx/discussions)
4. **报告**: [New Issue](https://github.com/Cricle/Sqlx/issues/new)

---

## 🎉 开始使用！

**准备好了吗？**

```
👉 新用户: 从 README.md 开始
👉 迁移用户: 查看 MIGRATION_GUIDE.md
👉 高级用户: 直接看 API_REFERENCE.md
👉 遇到问题: 查阅 FAQ.md
```

**祝您使用愉快！** 🚀

---

**最后更新**: 2025-10-29  
**文档版本**: 1.0  
**项目版本**: v0.5.0-preview  


