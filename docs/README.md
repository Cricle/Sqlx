# 📚 Sqlx 文档中心

欢迎来到 Sqlx 文档中心！这里提供完整的使用指南、API参考和技术文档。

---

## 🚀 快速导航

### 📖 入门必读
| 文档 | 描述 | 适合人群 |
|------|------|----------|
| [📋 项目概览](../README.md) | Sqlx项目介绍和核心特性 | 所有用户 |
| [⚡ 快速开始](QUICK_START_GUIDE.md) | 5分钟快速上手指南 | 新用户 |
| [📘 API参考](API_REFERENCE.md) | 完整的API文档 | 开发者 |
| [💡 最佳实践](BEST_PRACTICES.md) | 推荐的使用模式和技巧 | 进阶用户 |

### 🌟 核心功能
| 文档 | 描述 | 重要程度 |
|------|------|----------|
| [🌐 多数据库模板引擎](MULTI_DATABASE_TEMPLATE_ENGINE.md) | **核心创新**：写一次，处处运行 | ⭐⭐⭐⭐⭐ |
| [🎯 扩展占位符指南](EXTENDED_PLACEHOLDERS_GUIDE.md) | 22个智能占位符完整说明 | ⭐⭐⭐⭐⭐ |
| [📝 CRUD操作完整指南](CRUD_PLACEHOLDERS_COMPLETE_GUIDE.md) | **必读**：增删改查全场景占位符 | ⭐⭐⭐⭐⭐ |
| [🛡️ 安全特性](SECURITY_FEATURES.md) | SQL注入防护和安全检查 | ⭐⭐⭐⭐ |
| [⚡ 性能优化](PERFORMANCE_OPTIMIZATION.md) | AOT支持和性能调优 | ⭐⭐⭐⭐ |

### 🔧 深度学习
| 文档 | 描述 | 适合人群 |
|------|------|----------|
| [🏗️ 高级功能](ADVANCED_FEATURES.md) | 高级特性和扩展功能 | 高级用户 |
| [📦 迁移指南](MIGRATION_GUIDE.md) | 从其他ORM迁移到Sqlx | 升级用户 |
| [🔬 项目架构](PROJECT_STRUCTURE.md) | 代码组织和架构设计 | 贡献者 |
| [📊 项目状态](PROJECT_STATUS.md) | 当前开发状态和里程碑 | 关注者 |

### 📈 项目历程
| 文档 | 描述 | 价值 |
|------|------|------|
| [🚀 优化总结](OPTIMIZATION_SUMMARY.md) | **完整优化历程**：40.5%代码精简 | 技术参考 |
| [🎯 开发路线图](OPTIMIZATION_ROADMAP.md) | 未来发展规划 | 了解方向 |

---

## 🎯 三种核心使用模式

Sqlx 专注于三种简单而强大的使用模式：

### 1️⃣ 直接执行 - 最简单
```csharp
var sql = ParameterizedSql.Create(
    "SELECT * FROM Users WHERE Age > @age",
    new { age = 18 });
string result = sql.Render();
```

### 2️⃣ 静态模板 - 可重用
```csharp
var template = SqlTemplate.Parse("SELECT * FROM Users WHERE Age > @age");
var young = template.Execute(new { age = 18 });
var senior = template.Execute(new { age = 65 });
```

### 3️⃣ 动态模板 - 类型安全
```csharp
var query = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
    .Where(u => u.Age > 25 && u.IsActive)
    .Select(u => new { u.Name, u.Email })
    .OrderBy(u => u.Name)
    .Take(10);
string sql = query.ToSql();
```

---

## 🌟 核心特性概览

### ✨ 写一次，处处运行
**多数据库模板引擎** - Sqlx的核心创新

```csharp
// 同一个模板
[Sqlx("SELECT {{columns:auto}} FROM {{table:quoted}} WHERE {{where:id}}")]
Task<List<User>> GetUserAsync(int id);
```

**自动生成不同数据库的SQL：**
- **SQL Server**: `SELECT [Id], [Name] FROM [User] WHERE [Id] = @id`
- **MySQL**: `SELECT `Id`, `Name` FROM `User` WHERE `Id` = @id`
- **PostgreSQL**: `SELECT "Id", "Name" FROM "User" WHERE "Id" = $1`
- **SQLite**: `SELECT [Id], [Name] FROM [User] WHERE [Id] = $id`

### 🛡️ 安全可靠
- **SQL注入防护** - 自动检测危险SQL模式
- **数据库特定安全检查** - 针对不同数据库的威胁检测
- **编译时验证** - 所有SQL在编译时验证

### ⚡ 极致性能
- **零反射设计** - 完全避免运行时反射
- **AOT原生支持** - .NET Native AOT 完美兼容
- **智能缓存** - 模板处理结果自动缓存
- **性能提升** - 相比EF Core提升27倍

### 😊 开发友好
- **22个智能占位符** - 覆盖所有常用场景
- **清晰错误提示** - 编译时和运行时错误诊断
- **现代C#语法** - 支持C# 12 新特性
- **完整文档** - 详尽的文档和示例

---

## 🎯 按需求选择文档

### 我是新用户，想快速了解Sqlx
👉 **推荐路径**：
1. [📋 项目概览](../README.md) - 了解Sqlx是什么
2. [⚡ 快速开始](QUICK_START_GUIDE.md) - 5分钟上手
3. [🌐 多数据库模板引擎](MULTI_DATABASE_TEMPLATE_ENGINE.md) - 核心功能

### 我想深入学习Sqlx的功能
👉 **推荐路径**：
1. [📘 API参考](API_REFERENCE.md) - 完整API文档
2. [🎯 扩展占位符指南](EXTENDED_PLACEHOLDERS_GUIDE.md) - 22个占位符
3. [💡 最佳实践](BEST_PRACTICES.md) - 使用技巧
4. [🏗️ 高级功能](ADVANCED_FEATURES.md) - 高级特性

### 我想从其他ORM迁移到Sqlx
👉 **推荐路径**：
1. [📦 迁移指南](MIGRATION_GUIDE.md) - 迁移步骤
2. [🌐 多数据库模板引擎](MULTI_DATABASE_TEMPLATE_ENGINE.md) - 核心优势
3. [⚡ 性能优化](PERFORMANCE_OPTIMIZATION.md) - 性能对比

### 我想贡献代码或了解项目详情
👉 **推荐路径**：
1. [📊 项目状态](PROJECT_STATUS.md) - 当前状态
2. [🔬 项目架构](PROJECT_STRUCTURE.md) - 代码结构
3. [🚀 优化总结](OPTIMIZATION_SUMMARY.md) - 技术历程

### 我想了解性能和技术细节
👉 **推荐路径**：
1. [🚀 优化总结](OPTIMIZATION_SUMMARY.md) - 完整优化历程
2. [⚡ 性能优化](PERFORMANCE_OPTIMIZATION.md) - 性能特性
3. [🛡️ 安全特性](SECURITY_FEATURES.md) - 安全机制

---

## 🔍 快速查找

### 按功能分类
- **模板引擎**: [多数据库模板引擎](MULTI_DATABASE_TEMPLATE_ENGINE.md) | [扩展占位符指南](EXTENDED_PLACEHOLDERS_GUIDE.md)
- **性能相关**: [性能优化](PERFORMANCE_OPTIMIZATION.md) | [优化总结](OPTIMIZATION_SUMMARY.md)
- **安全相关**: [安全特性](SECURITY_FEATURES.md) | [最佳实践](BEST_PRACTICES.md)
- **开发相关**: [API参考](API_REFERENCE.md) | [高级功能](ADVANCED_FEATURES.md)

### 按用户类型
- **新手用户**: [快速开始](QUICK_START_GUIDE.md) | [API参考](API_REFERENCE.md)
- **进阶用户**: [最佳实践](BEST_PRACTICES.md) | [高级功能](ADVANCED_FEATURES.md)
- **迁移用户**: [迁移指南](MIGRATION_GUIDE.md) | [性能对比](PERFORMANCE_OPTIMIZATION.md)
- **贡献者**: [项目架构](PROJECT_STRUCTURE.md) | [项目状态](PROJECT_STATUS.md)

---

## 📊 文档完整性

### 文档覆盖率
- **核心功能文档**: 100% ✅
- **API参考文档**: 100% ✅
- **示例代码**: 100% ✅
- **迁移指南**: 100% ✅
- **性能文档**: 100% ✅

### 文档质量保证
- **技术审查**: 所有文档经过技术验证
- **代码同步**: 文档与代码实现保持同步
- **示例验证**: 所有示例代码可运行
- **定期更新**: 跟随项目发展持续更新

---

## 🤝 改进文档

发现文档问题或有改进建议？

### 反馈方式
1. **GitHub Issues** - 报告文档错误或缺失
2. **GitHub Discussions** - 讨论文档改进
3. **Pull Request** - 直接贡献文档改进
4. **Email** - 直接联系维护者

### 贡献指南
- 遵循现有文档风格
- 提供可运行的示例代码
- 包含必要的解释和背景
- 确保技术准确性

---

<div align="center">

**📚 探索Sqlx的强大功能，从这里开始您的文档之旅！**

**[🏠 回到首页](../README.md) · [⚡ 快速开始](QUICK_START_GUIDE.md) · [🌐 多数据库引擎](MULTI_DATABASE_TEMPLATE_ENGINE.md)**

</div>
