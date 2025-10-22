# 文档结构说明

本文档说明 Sqlx 项目的文档组织结构和访问方式。

---

## 📁 文档目录结构

```
Sqlx/
├── README.md                          # 项目主页（根目录）
├── docs/                              # 📂 文档中心（所有文档）
│   ├── README.md                      # 文档中心首页
│   ├── web/                           # 🌐 GitHub Pages 网站
│   │   ├── index.html                 # 网站主页
│   │   ├── .nojekyll                  # GitHub Pages 配置
│   │   └── README.md                  # 网站说明
│   │
│   ├── DOCUMENTATION_INDEX.md         # 📋 文档索引（完整导航）
│   ├── QUICK_REFERENCE.md             # ⚡ 快速参考
│   │
│   ├── DESIGN_PRINCIPLES.md           # 🎯 设计原则
│   ├── FRAMEWORK_COMPATIBILITY.md     # 🌐 框架兼容性
│   ├── GLOBAL_INTERCEPTOR_DESIGN.md   # 🔧 拦截器设计
│   ├── IMPLEMENTATION_SUMMARY.md      # 📝 实施总结
│   │
│   ├── BENCHMARK_ANALYSIS_AND_OPTIMIZATION_PLAN.md  # 📊 性能分析
│   ├── BENCHMARKS_SUMMARY.md                        # 📈 性能测试结果
│   ├── FINAL_OPTIMIZATION_SUMMARY.md                # ✅ 最终优化总结
│   │
│   ├── CODE_REVIEW_REPORT.md          # 🔍 代码审查
│   ├── GENERATED_CODE_REVIEW.md       # 🔍 生成代码审查
│   ├── SQL_TEMPLATE_REVIEW.md         # 🔍 SQL模板审查
│   │
│   ├── PROJECT_STATUS.md              # 📊 项目状态
│   ├── CHANGELOG.md                   # 📝 更新日志
│   └── ...                            # 其他文档
│
├── samples/                           # 💡 示例项目
│   ├── TodoWebApi/                    # Web API 示例
│   └── SqlxDemo/                      # 基础示例
│
├── tests/                             # 🧪 测试项目
│   └── Sqlx.Benchmarks/               # 性能测试
│
└── .github/
    └── workflows/
        └── pages.yml                  # GitHub Pages 自动部署
```

---

## 🌐 访问方式

### 1. GitHub 仓库

直接在 GitHub 仓库中浏览文档：

```
https://github.com/your-username/Sqlx/tree/main/docs
```

### 2. GitHub Pages 网站

访问更友好的在线文档界面：

```
https://your-username.github.io/Sqlx/
```

### 3. 本地浏览

克隆仓库后，在本地打开：

```bash
# 克隆仓库
git clone https://github.com/your-username/Sqlx.git
cd Sqlx

# 浏览文档
cd docs
```

---

## 📖 文档分类

### 🚀 新手入门

| 文档 | 说明 | 路径 |
|------|------|------|
| README.md | 项目主页 | `/README.md` |
| 快速参考 | 一页纸速查表 | `docs/QUICK_REFERENCE.md` |
| 快速开始指南 | 详细入门教程 | `docs/QUICK_START_GUIDE.md` |
| 文档索引 | 完整导航 | `docs/DOCUMENTATION_INDEX.md` |

### 🎯 核心设计

| 文档 | 说明 | 路径 |
|------|------|------|
| 设计原则 | Fail Fast、零缓存等理念 | `docs/DESIGN_PRINCIPLES.md` |
| 拦截器设计 | 零 GC 拦截器架构 | `docs/GLOBAL_INTERCEPTOR_DESIGN.md` |
| 框架兼容性 | 多框架支持说明 | `docs/FRAMEWORK_COMPATIBILITY.md` |
| 实施总结 | 功能实施详情 | `docs/IMPLEMENTATION_SUMMARY.md` |

### 📊 性能与优化

| 文档 | 说明 | 路径 |
|------|------|------|
| 性能分析计划 | 详细优化方案 | `docs/BENCHMARK_ANALYSIS_AND_OPTIMIZATION_PLAN.md` |
| 性能测试结果 | Benchmark 报告 | `docs/BENCHMARKS_SUMMARY.md` |
| 最终优化总结 | 所有优化汇总 | `docs/FINAL_OPTIMIZATION_SUMMARY.md` |
| 优化进度 | 优化任务跟踪 | `docs/OPTIMIZATION_PROGRESS.md` |

### 🔍 代码审查

| 文档 | 说明 | 路径 |
|------|------|------|
| 代码审查报告 | 核心库审查 | `docs/CODE_REVIEW_REPORT.md` |
| 生成代码审查 | 源生成器输出审查 | `docs/GENERATED_CODE_REVIEW.md` |
| SQL模板审查 | 模板引擎审查 | `docs/SQL_TEMPLATE_REVIEW.md` |

### 📚 参考手册

| 文档 | 说明 | 路径 |
|------|------|------|
| API 参考 | API 详细说明 | `docs/API_REFERENCE.md` |
| 占位符参考 | SQL 模板占位符 | `docs/PLACEHOLDERS.md` |
| 高级特性 | 高级用法 | `docs/ADVANCED_FEATURES.md` |
| 最佳实践 | 推荐做法 | `docs/BEST_PRACTICES.md` |

### 📋 项目信息

| 文档 | 说明 | 路径 |
|------|------|------|
| 项目状态 | 当前进度 | `docs/PROJECT_STATUS.md` |
| 更新日志 | 版本历史 | `docs/CHANGELOG.md` |
| 迁移指南 | 版本迁移 | `docs/MIGRATION_GUIDE.md` |

---

## 🔗 链接规则

### 根目录 README.md 引用文档

```markdown
[文档标题](docs/DOCUMENT_NAME.md)
```

### docs 文件夹内互相引用

```markdown
[文档标题](DOCUMENT_NAME.md)
```

### docs 引用根目录文件

```markdown
[项目主页](../README.md)
[示例代码](../samples/)
```

### GitHub Pages 引用文档

```html
<!-- 引用 docs 文件夹中的文档 -->
<a href="../DOCUMENT_NAME.md">文档标题</a>
```

---

## 🚀 GitHub Pages 配置

### 启用 GitHub Pages

1. 进入仓库 **Settings** → **Pages**
2. **Source** 设置：
   - Branch: `main` (或 `master`)
   - Folder: `/docs/web`
3. 点击 **Save**
4. 等待部署完成

### 自动部署

已配置 GitHub Actions 自动部署：

- 文件：`.github/workflows/pages.yml`
- 触发条件：`docs/**` 文件变更时自动部署
- 部署目标：`docs/web` 文件夹

---

## 📝 文档维护指南

### 添加新文档

1. 在 `docs/` 目录创建新的 `.md` 文件
2. 更新 `docs/DOCUMENTATION_INDEX.md` 添加索引
3. 如需在网站展示，更新 `docs/web/index.html`

### 修改文档链接

修改文档时，注意更新所有相关链接：

- ✅ 使用相对路径
- ✅ 检查所有引用该文档的地方
- ✅ 验证链接有效性

### 文档命名规范

- 使用大写字母和下划线：`DOCUMENT_NAME.md`
- 简洁明确：描述文档内容
- 避免空格：使用下划线分隔

---

## 💡 最佳实践

### 1. 文档导航

每个文档应包含：

```markdown
# 文档标题

[← 返回文档中心](README.md) | [📋 文档索引](DOCUMENTATION_INDEX.md)

---

## 目录
- [章节1](#章节1)
- [章节2](#章节2)

---
```

### 2. 跨文档引用

使用相对路径引用：

```markdown
详见：[设计原则](DESIGN_PRINCIPLES.md)
```

### 3. 代码示例

使用代码块，指定语言：

```markdown
​```csharp
public class Example { }
​```
```

### 4. 文档更新

在文档顶部添加更新日期：

```markdown
# 文档标题

**最后更新**: 2025-10-21

---
```

---

## 🔄 迁移记录

### 2025-10-21: 文档重组

**变更**:
- ✅ 创建 `docs/` 文件夹
- ✅ 移动所有 `.md` 文档到 `docs/`
- ✅ 创建 `docs/web/` GitHub Pages 网站
- ✅ 更新根目录 `README.md` 链接
- ✅ 添加 GitHub Actions 自动部署

**影响**:
- 所有文档链接已更新
- GitHub Pages 网站已配置
- 文档结构更清晰

---

## ❓ 常见问题

### Q: 为什么移动文档到 docs 文件夹？

**A**: 为了更好的组织结构：
- ✅ 将文档与代码分离
- ✅ 支持 GitHub Pages
- ✅ 更清晰的项目结构
- ✅ 更容易维护

### Q: 旧的文档链接会失效吗？

**A**: 是的，需要更新链接：
- 根目录不再包含文档文件（除 README.md）
- 所有文档链接已更新为 `docs/` 路径
- 建议更新收藏/书签

### Q: 如何本地预览 GitHub Pages？

**A**: 使用 HTTP 服务器：

```bash
# 方法1: Python
cd docs/web
python -m http.server 8000

# 方法2: Node.js
cd docs/web
npx http-server

# 方法3: .NET
cd docs/web
dotnet serve
```

访问 http://localhost:8000

---

**提示**: 如有疑问，请查看 [文档中心](README.md) 或 [文档索引](DOCUMENTATION_INDEX.md)。


