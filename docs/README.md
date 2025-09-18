# Sqlx 文档中心

欢迎来到 Sqlx ORM 框架的文档中心！这里包含了所有您需要了解的信息，从快速开始到高级特性，应有尽有。

## 📚 文档导航

### 🚀 快速开始
- [30秒快速开始](../README.md#-30秒快速开始) - 立即上手 Sqlx
- [项目结构](PROJECT_STRUCTURE.md) - 了解项目组织方式
- [快速特性指南](NEW_FEATURES_QUICK_START.md) - 核心功能速览

### 🏗️ 核心功能
- [SqlTemplate 完全指南](SQL_TEMPLATE_GUIDE.md) - 模板引擎详细说明
- [ExpressionToSql 指南](expression-to-sql.md) - 类型安全查询构建
- [SqlTemplate 设计革新](SQLTEMPLATE_DESIGN_FIXED.md) - 纯模板设计原理
- [无缝集成指南](SEAMLESS_INTEGRATION_GUIDE.md) - ExpressionToSql ↔ SqlTemplate

### 🆕 现代 C# 支持
- [Primary Constructor & Record 支持](PRIMARY_CONSTRUCTOR_RECORD_SUPPORT.md) - C# 12+ 特性
- [高级特性指南](ADVANCED_FEATURES_GUIDE.md) - AOT、性能优化等

### 📖 完整参考
- [完整特性指南](SQLX_COMPLETE_FEATURE_GUIDE.md) - 所有功能详细说明
- [迁移指南](MIGRATION_GUIDE.md) - 从其他 ORM 迁移
- [优化路线图](OPTIMIZATION_ROADMAP.md) - 性能优化建议

### 🔧 高级主题
- [诊断和指导](DiagnosticGuidance.md) - 代码质量分析
- [SQL 占位符指南](SQL_PLACEHOLDER_GUIDE.md) - 占位符功能详解
- [项目状态](PROJECT_STATUS.md) - 开发状态和计划

## 🎯 按使用场景分类

### 👨‍💻 开发者
- **新手**: [快速开始](../README.md#-30秒快速开始) → [核心特性](../README.md#-核心特性详解)
- **进阶**: [SqlTemplate 指南](SQL_TEMPLATE_GUIDE.md) → [ExpressionToSql](expression-to-sql.md)
- **专家**: [高级特性](ADVANCED_FEATURES_GUIDE.md) → [性能优化](OPTIMIZATION_ROADMAP.md)

### 🏢 架构师
- **技术选型**: [完整特性指南](SQLX_COMPLETE_FEATURE_GUIDE.md)
- **性能评估**: [性能对比](../README.md#-性能对比)
- **迁移计划**: [迁移指南](MIGRATION_GUIDE.md)

### 🚀 DevOps
- **部署配置**: [AOT 支持](ADVANCED_FEATURES_GUIDE.md#aot-支持)
- **云原生**: [云原生最佳实践](ADVANCED_FEATURES_GUIDE.md#云原生部署)
- **监控诊断**: [诊断工具](DiagnosticGuidance.md)

## ✨ 最新特性亮点

### 🔥 SqlTemplate 革新设计
> **核心理念**: "模板是模板，参数是参数" - 完全分离，性能翻倍

```csharp
// ✅ NEW: 纯模板设计
var template = SqlTemplate.Parse("SELECT * FROM users WHERE id = @id");
var user1 = template.Execute(new { id = 1 });  // 重用模板
var user2 = template.Execute(new { id = 2 });  // 高性能
```

**文档**: [SqlTemplate 设计革新](SQLTEMPLATE_DESIGN_FIXED.md)

### 🔄 无缝集成
> ExpressionToSql 与 SqlTemplate 的完美融合

```csharp
using var builder = SqlTemplateExpressionBridge.Create<User>();
var result = builder
    .Where(u => u.IsActive)           // 表达式
    .Template("AND age > @minAge")    // 模板
    .Param("minAge", 18)              // 参数
    .Build();
```

**文档**: [无缝集成指南](SEAMLESS_INTEGRATION_GUIDE.md)

### 🏗️ C# 12+ 完整支持
> Primary Constructor 和 Record 类型的原生支持

```csharp
// Record 类型
public record User(int Id, string Name);

// Primary Constructor
public class Service(IDbConnection connection) { }
```

**文档**: [现代 C# 支持](PRIMARY_CONSTRUCTOR_RECORD_SUPPORT.md)

## 🔍 文档搜索指南

### 按关键词查找
- **模板**: [SqlTemplate 指南](SQL_TEMPLATE_GUIDE.md)
- **查询**: [ExpressionToSql](expression-to-sql.md)
- **性能**: [优化指南](OPTIMIZATION_ROADMAP.md)
- **AOT**: [高级特性](ADVANCED_FEATURES_GUIDE.md)
- **迁移**: [迁移指南](MIGRATION_GUIDE.md)

### 按问题类型
- **如何使用**: [快速开始](../README.md#-30秒快速开始)
- **性能问题**: [性能优化](OPTIMIZATION_ROADMAP.md)
- **兼容性**: [迁移指南](MIGRATION_GUIDE.md)
- **错误排查**: [诊断指南](DiagnosticGuidance.md)

## 📖 贡献文档

我们欢迎社区贡献文档！请参考：

1. [贡献指南](../CONTRIBUTING.md)
2. 文档格式规范
3. 示例代码标准

## 🆘 需要帮助？

- **GitHub Issues**: [问题反馈](https://github.com/your-repo/sqlx/issues)
- **Discussions**: [讨论交流](https://github.com/your-repo/sqlx/discussions)  
- **商业支持**: business@sqlx.dev

---

<div align="center">

**📚 探索 Sqlx 的强大功能，从这些文档开始您的旅程！**

</div>