# Sqlx 完整文档

欢迎来到Sqlx文档中心！这里包含了Sqlx的完整使用指南、API参考和最佳实践。

## 📖 学习路径

### 🚀 快速开始（5分钟）

1. **[快速入门指南](QUICK_START_GUIDE.md)** ⭐ 推荐新手
   - 安装和配置
   - 第一个查询
   - 基础CRUD操作
   - 常见问题解决

2. **[快速参考](QUICK_REFERENCE.md)**
   - 常用操作速查
   - 代码片段
   - 快捷方式

### 📚 核心概念（30分钟）

3. **[API参考](API_REFERENCE.md)**
   - `[Sqlx]` 特性详解
   - `[RepositoryFor]` 特性
   - `[DynamicSql]` 动态SQL
   - `[BatchOperation]` 批量操作
   - `[ExpressionToSql]` 表达式转换

4. **[模板占位符](PLACEHOLDERS.md)** ⭐ 核心功能
   - 40+占位符完整列表
   - 基础占位符（table, columns, values, where, set）
   - 高级占位符（case, coalesce, pagination）
   - 聚合函数（count, sum, avg, max, min）
   - 字符串函数（upper, lower, trim, concat）
   - 日期函数（today, date_add, date_diff）
   - 正则筛选（--regex）
   - 示例和最佳实践

5. **[多数据库模板引擎](MULTI_DATABASE_TEMPLATE_ENGINE.md)**
   - 跨数据库兼容性
   - 方言系统设计
   - 特定数据库优化

### 🎯 高级特性（1小时）

6. **[高级功能](ADVANCED_FEATURES.md)**
   - 正则表达式列筛选
   - 动态返回值（`List<Dictionary<string, object>>`）
   - 批量操作优化
   - 表达式转SQL
   - 安全字符串插值

7. **[Partial方法指南](PARTIAL_METHODS_GUIDE.md)**
   - OnExecuting - 执行前拦截
   - OnExecuted - 执行后处理
   - OnExecuteFail - 异常处理
   - 性能追踪集成

8. **[Activity追踪与指标](FRAMEWORK_COMPATIBILITY.md)**
   - OpenTelemetry集成
   - 分布式追踪
   - 性能指标收集
   - 条件编译（SQLX_ENABLE_TRACING）

### 📐 最佳实践（30分钟）

9. **[最佳实践](BEST_PRACTICES.md)** ⭐ 重要
   - 性能优化技巧
   - 安全性建议
   - 代码组织
   - 错误处理
   - 测试策略

10. **[迁移指南](MIGRATION_GUIDE.md)**
    - 从Dapper迁移
    - 从EF Core迁移
    - 从ADO.NET迁移
    - 版本升级指南

11. **[AI辅助使用](AI_USAGE_GUIDE.md)**
    - 使用ChatGPT生成SQL模板
    - 优化查询性能
    - 自动化测试生成

---

## 📂 文档导航

### 按主题分类

<details>
<summary><strong>🎨 功能特性</strong></summary>

- [模板占位符完整列表](PLACEHOLDERS.md)
- [正则表达式列筛选](ADVANCED_FEATURES.md#正则表达式列筛选)
- [动态返回值](ADVANCED_FEATURES.md#动态返回值)
- [批量操作](ADVANCED_FEATURES.md#批量操作)
- [表达式转SQL](ADVANCED_FEATURES.md#表达式转sql)
- [Partial方法拦截](PARTIAL_METHODS_GUIDE.md)
- [Activity追踪](FRAMEWORK_COMPATIBILITY.md)

</details>

<details>
<summary><strong>🗃️ 数据库支持</strong></summary>

- [SQL Server](MULTI_DATABASE_TEMPLATE_ENGINE.md#sql-server)
- [MySQL](MULTI_DATABASE_TEMPLATE_ENGINE.md#mysql)
- [PostgreSQL](MULTI_DATABASE_TEMPLATE_ENGINE.md#postgresql)
- [SQLite](MULTI_DATABASE_TEMPLATE_ENGINE.md#sqlite)
- [Oracle](MULTI_DATABASE_TEMPLATE_ENGINE.md#oracle)
- [跨数据库兼容性](MULTI_DATABASE_TEMPLATE_ENGINE.md#跨数据库兼容性)

</details>

<details>
<summary><strong>⚡ 性能优化</strong></summary>

- [性能最佳实践](BEST_PRACTICES.md#性能优化)
- [硬编码列索引](BEST_PRACTICES.md#硬编码列索引)
- [Nullable类型优化](BEST_PRACTICES.md#nullable优化)
- [批量操作优化](ADVANCED_FEATURES.md#批量操作优化)
- [GC压力减少](BEST_PRACTICES.md#gc优化)

</details>

<details>
<summary><strong>🛡️ 安全性</strong></summary>

- [SQL注入防护](BEST_PRACTICES.md#sql注入防护)
- [参数化查询](QUICK_START_GUIDE.md#参数化查询)
- [Roslyn分析器警告](BEST_PRACTICES.md#roslyn分析器)
- [敏感字段保护](BEST_PRACTICES.md#敏感字段)

</details>

<details>
<summary><strong>🔧 工具和集成</strong></summary>

- [Roslyn Analyzer](BEST_PRACTICES.md#roslyn分析器)
- [OpenTelemetry](FRAMEWORK_COMPATIBILITY.md#opentelemetry)
- [AI辅助开发](AI_USAGE_GUIDE.md)
- [单元测试](BEST_PRACTICES.md#测试策略)

</details>

---

## 🔍 快速查找

### 常见任务

| 任务 | 文档链接 |
|------|----------|
| 执行基础查询 | [快速入门](QUICK_START_GUIDE.md#基础查询) |
| 插入数据 | [快速入门](QUICK_START_GUIDE.md#插入操作) |
| 更新数据 | [快速入门](QUICK_START_GUIDE.md#更新操作) |
| 删除数据 | [快速入门](QUICK_START_GUIDE.md#删除操作) |
| 批量操作 | [高级功能](ADVANCED_FEATURES.md#批量操作) |
| 动态查询 | [高级功能](ADVANCED_FEATURES.md#动态sql) |
| 性能优化 | [最佳实践](BEST_PRACTICES.md#性能优化) |
| 多数据库支持 | [多数据库引擎](MULTI_DATABASE_TEMPLATE_ENGINE.md) |
| 追踪和监控 | [框架兼容性](FRAMEWORK_COMPATIBILITY.md) |
| 迁移到Sqlx | [迁移指南](MIGRATION_GUIDE.md) |

### 常见问题

| 问题 | 解决方案 |
|------|----------|
| 如何使用正则筛选列？ | [正则筛选](ADVANCED_FEATURES.md#正则表达式列筛选) |
| 如何处理动态列？ | [动态返回值](ADVANCED_FEATURES.md#动态返回值) |
| 如何优化查询性能？ | [性能最佳实践](BEST_PRACTICES.md#性能优化) |
| 如何防止SQL注入？ | [SQL注入防护](BEST_PRACTICES.md#sql注入防护) |
| 如何支持Nullable类型？ | [Nullable支持](BEST_PRACTICES.md#nullable优化) |
| 如何添加自定义拦截？ | [Partial方法](PARTIAL_METHODS_GUIDE.md) |
| 如何集成OpenTelemetry？ | [Activity追踪](FRAMEWORK_COMPATIBILITY.md) |

---

## 📊 API文档

### 核心特性

- **[Sqlx]** - 核心特性，标记SQL模板
  - `string Template` - SQL模板字符串
  - `SqlDefine Dialect` - 数据库方言
  - `string? TableName` - 表名（可选）

- **[RepositoryFor<T>]** - 仓储特性，指定实体类型
  - `Type EntityType` - 实体类型
  - `string? TableName` - 表名覆盖

- **[DynamicSql]** - 动态SQL参数
  - 标记参数为动态SQL片段
  - 安全性：必须显式标记

- **[BatchOperation]** - 批量操作参数
  - 标记集合参数为批量操作
  - 性能优化：一次性处理

- **[ExpressionToSql]** - 表达式转SQL
  - 将LINQ表达式转换为SQL
  - 类型安全的查询构建

### Partial方法签名

```csharp
partial void OnExecuting(string operationName, DbCommand command);
partial void OnExecuted(string operationName, object? result, TimeSpan elapsed);
partial void OnExecuteFail(string operationName, Exception exception);
```

---

## 🆕 更新日志

查看 [CHANGELOG.md](CHANGELOG.md) 了解版本历史和更新内容。

---

## 🤝 社区和支持

- **GitHub**: [提Issue](https://github.com/Cricle/Sqlx/issues)
- **讨论**: [GitHub Discussions](https://github.com/Cricle/Sqlx/discussions)
- **Email**: [support@sqlx.dev](mailto:support@sqlx.dev)

---

## 📝 贡献文档

发现文档问题或想要改进？欢迎提交PR！

1. Fork仓库
2. 修改文档（Markdown格式）
3. 提交PR并描述更改

---

**导航提示**：
- 🚀 新手从 [快速入门](QUICK_START_GUIDE.md) 开始
- ⭐ 查看完整功能请阅读 [高级功能](ADVANCED_FEATURES.md)
- 📖 API详情参考 [API文档](API_REFERENCE.md)
- 💡 优化技巧见 [最佳实践](BEST_PRACTICES.md)

祝使用愉快！🎉
