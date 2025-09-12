# 📚 Sqlx 文档中心

<div align="center">

**完整的开发者指南 · 从入门到精通 · 中英文并行**

[![文档](https://img.shields.io/badge/文档-完整-green?style=for-the-badge)]()
[![语言](https://img.shields.io/badge/语言-中英文-blue?style=for-the-badge)]()
[![更新](https://img.shields.io/badge/更新-实时-orange?style=for-the-badge)]()

</div>

---

## 📋 文档导航

### 🚀 快速开始

| 文档 | 描述 | 难度 |
|------|------|------|
| [📖 主要文档](../README.md) | 项目介绍和快速上手 | ⭐ 入门 |
| [🆕 新功能快速入门](NEW_FEATURES_QUICK_START.md) | BatchCommand 和模运算支持 | ⭐⭐ 进阶 |
| [🎨 ExpressionToSql 指南](expression-to-sql.md) | LINQ 表达式转 SQL 详解 | ⭐⭐ 进阶 |

### 🏗️ 高级特性

| 文档 | 描述 | 难度 |
|------|------|------|
| [🚀 高级特性指南](ADVANCED_FEATURES_GUIDE.md) | 深入掌握高级功能 | ⭐⭐⭐ 高级 |
| [🏗️ 现代 C# 支持](PRIMARY_CONSTRUCTOR_RECORD_SUPPORT.md) | Primary Constructor & Record | ⭐⭐⭐ 高级 |
| [🔄 升级迁移指南](MIGRATION_GUIDE.md) | 从 v1.x 平滑升级 | ⭐⭐ 进阶 |

### 📋 项目信息

| 文档 | 描述 | 类型 |
|------|------|------|
| [📋 更新日志](../CHANGELOG.md) | 详细版本变更记录 | 📊 参考 |
| [🤝 贡献指南](../CONTRIBUTING.md) | 参与开发指南 | 🔧 开发 |
| [📊 项目结构](PROJECT_STRUCTURE.md) | 代码组织架构 | 🔧 开发 |

### 🔧 开发者资源

| 文档 | 描述 | 类型 |
|------|------|------|
| [📊 系统现状分析](../SQLX_系统现状分析报告.md) | 全面技术分析报告 | 📈 分析 |
| [🔧 Codacy 配置](CODACY_SETUP.md) | 代码质量检查配置 | 🔧 开发 |
| [🚀 下一步优化计划](../下一步优化计划.md) | 未来发展路线图 | 📋 规划 |

---

## 💻 示例项目

### 🎯 综合演示

| 项目 | 描述 | 推荐度 |
|------|------|--------|
| [📦 完整功能演示](../samples/ComprehensiveExample/) | 一站式体验所有功能 | ⭐⭐⭐⭐⭐ |
| - 🎮 交互式演示菜单 | 9个专业演示模块 | - |
| - 📊 性能基准测试 | 实测性能数据对比 | - |
| - 🏗️ 现代语法展示 | Record + Primary Constructor | - |

---

## 💡 最佳实践

### 🚀 批量操作
```csharp
// ✅ 推荐：大批量操作使用 BatchCommand
[SqlExecuteType(SqlExecuteTypes.BatchInsert, "products")]
Task<int> BatchInsertAsync(IEnumerable<Product> products);

// ✅ 推荐：性能提升 10-100 倍
const int batchSize = 1000;
var batches = products.Chunk(batchSize);
foreach (var batch in batches)
{
    await productService.BatchInsertAsync(batch);
}
```

### 🎨 动态查询
```csharp
// ✅ 推荐：类型安全的动态查询
var query = ExpressionToSql<User>.ForSqlServer()
    .Where(u => u.IsActive && u.Id % 2 == 0)  // 模运算支持
    .OrderBy(u => u.Name)
    .Take(100);

var users = await userService.QueryUsersAsync(query);
```

### 🔧 连接管理
```csharp
// ✅ 推荐：ADO.NET 内置连接池
var connectionString = "Server=...;Pooling=true;Min Pool Size=5;Max Pool Size=100;";

// ✅ 推荐：一个连接处理多个操作
using var connection = new SqlConnection(connectionString);
await connection.OpenAsync();
// 执行多个相关操作...
```

---

## 🎯 常见问题

<details>
<summary><strong>Q: 如何选择合适的实体类型？</strong></summary>

```csharp
// ✅ Record 类型 - 不可变数据传输对象
public record ProductInfo(int Id, string Name, decimal Price);

// ✅ Primary Constructor - 有业务逻辑的实体
public class Customer(int id, string name, string email)
{
    public int Id { get; } = id;
    public CustomerStatus Status { get; set; } = CustomerStatus.Active;
}

// ✅ 传统类 - 复杂继承或特殊需求
public class BaseEntity { /* 复杂基类 */ }
```
</details>

<details>
<summary><strong>Q: 支持哪些数据库？</strong></summary>

| 数据库 | 支持状态 | 特殊功能 |
|--------|----------|----------|
| **SQL Server** | ✅ 完全支持 | OFFSET/FETCH, MERGE |
| **MySQL** | ✅ 完全支持 | JSON 类型, 全文索引 |
| **PostgreSQL** | ✅ 完全支持 | 数组类型, JSONB |
| **SQLite** | ✅ 完全支持 | 内嵌式, 跨平台 |
| **Oracle** | 🔄 开发中 | 企业级特性 |
</details>

<details>
<summary><strong>Q: 如何发布新版本？</strong></summary>

```bash
# 自动化发布流程
git tag v2.0.1
git push origin v2.0.1  # 自动触发 CI/CD 发布到 NuGet
```
</details>

<details>
<summary><strong>Q: 性能优化建议？</strong></summary>

1. **批量操作** - 大数据量使用 BatchXxx 方法
2. **连接复用** - 合理使用连接池
3. **智能更新** - 使用部分更新减少数据传输
4. **索引优化** - 确保查询字段有适当索引
5. **类型安全** - 避免 object 类型减少装箱
</details>

---

## 📞 获取帮助

### 🔍 问题反馈
- 🐛 **[GitHub Issues](https://github.com/Cricle/Sqlx/issues)** - Bug 报告和功能请求
- 💬 **[GitHub Discussions](https://github.com/Cricle/Sqlx/discussions)** - 技术讨论和问答
- 📚 **[文档中心](.)** - 完整的使用指南

### 🤝 社区支持
- 📖 **[示例项目](../samples/)** - 丰富的实用示例
- 🧪 **[单元测试](../tests/)** - 1,700+ 测试用例参考
- 📊 **[性能测试](../samples/ComprehensiveExample/PerformanceTest.cs)** - 性能基准验证

---

<div align="center">

**📚 探索完整文档体系，掌握现代 .NET 数据访问技术**

**[⬆ 返回顶部](#-sqlx-文档中心)**

</div>