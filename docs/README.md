# 📚 Sqlx 文档

## 🚀 快速开始

| 文档 | 描述 |
|------|------|
| [主要文档](../README.md) | 项目介绍和快速上手 |
| [新功能快速入门](NEW_FEATURES_QUICK_START.md) | BatchCommand 和 mod 运算 |
| [ExpressionToSql 指南](expression-to-sql.md) | LINQ 表达式转 SQL |

## 📋 项目信息

| 文档 | 描述 |
|------|------|
| [更新日志](../CHANGELOG.md) | 版本更新记录 |
| [贡献指南](../CONTRIBUTING.md) | 参与开发指南 |
| [项目结构](../PROJECT_STRUCTURE.md) | 代码结构说明 |

## 🔧 开发者文档

| 文档 | 描述 |
|------|------|
| [优化总结](OPTIMIZATION_SUMMARY.md) | 性能优化详情 |
| [CI/CD 说明](../.github/workflows/README.md) | 构建和发布流程 |

## 💡 最佳实践

### BatchCommand
```csharp
// ✅ 推荐：大批量操作
[SqlExecuteType(SqlExecuteTypes.BatchCommand, "products")]
Task<int> BatchInsertAsync(IEnumerable<Product> products);
```

### ExpressionToSql
```csharp
// ✅ 推荐：结合条件使用
.Where(u => u.IsActive && u.Id % 2 == 0)
```

### 连接池配置
```csharp
// ADO.NET 内置连接池（推荐）
var connectionString = "Server=...;Pooling=true;Min Pool Size=5;Max Pool Size=100;";
```

## 🎯 常见问题

**Q: 如何发布新版本？**
```bash
git tag v1.0.0
git push origin v1.0.0  # 自动发布到 NuGet
```

**Q: 支持哪些数据库？**
- SQL Server、MySQL、PostgreSQL、SQLite

**Q: 如何配置连接池？**
- 使用 ADO.NET 内置连接池，通过连接字符串配置

## 📞 获取帮助

- 🐛 [提交 Issue](https://github.com/Cricle/Sqlx/issues)
- 💬 [讨论区](https://github.com/Cricle/Sqlx/discussions)
- 📧 联系维护者