# 🚀 Sqlx v2.0.0 - 从这里开始

欢迎使用 Sqlx！这是您的快速导航页面。

---

## 📋 快速导航

### 🎯 我想要...

| 需求 | 查看文档 | 时间 |
|-----|---------|------|
| **快速上手** | [QUICKSTART.md](QUICKSTART.md) | 5分钟 ⭐ |
| **了解项目** | [README.md](README.md) | 10分钟 |
| **一览全貌** | [PROJECT_AT_A_GLANCE.md](PROJECT_AT_A_GLANCE.md) | 1分钟 ⭐ |
| **查看状态** | [PROJECT_STATUS.md](PROJECT_STATUS.md) | 5分钟 |
| **版本历史** | [CHANGELOG.md](CHANGELOG.md) | 3分钟 |
| **完整文档** | [docs/](docs/) | 1小时+ |

---

## 🎓 推荐学习路径

### 新手路径（10分钟）
1. 📄 [PROJECT_AT_A_GLANCE.md](PROJECT_AT_A_GLANCE.md) - 1分钟了解全貌
2. 🚀 [QUICKSTART.md](QUICKSTART.md) - 5分钟快速上手
3. 📖 [README.md](README.md) - 详细了解项目

### 深度路径（1小时）
1. 📄 以上新手路径
2. 📚 [docs/API_REFERENCE.md](docs/API_REFERENCE.md) - API文档
3. 💡 [docs/BEST_PRACTICES.md](docs/BEST_PRACTICES.md) - 最佳实践
4. 🚀 [samples/TodoWebApi/](samples/TodoWebApi/) - 运行示例

---

## ⚡ 30秒快速开始

```bash
# 1. 安装
dotnet add package Sqlx
dotnet add package Sqlx.Generator

# 2. 定义接口
public interface IUserRepo {
    [SqlTemplate("SELECT * FROM users WHERE id = @id")]
    Task<User?> GetByIdAsync(long id);
}

# 3. 实现仓储
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IUserRepo))]
public partial class UserRepo(IDbConnection conn) : IUserRepo { }

# 4. 使用
var user = await repo.GetByIdAsync(1);
```

详细步骤请看：[QUICKSTART.md](QUICKSTART.md)

---

## 📊 项目状态

- ✅ **测试**: 1331个（100%通过）
- ✅ **覆盖率**: 95%
- ✅ **质量**: 5.0/5 ⭐⭐⭐⭐⭐
- ✅ **状态**: 生产就绪

详细信息：[PROJECT_STATUS.md](PROJECT_STATUS.md)

---

## 🎨 核心特性

- ✅ **零反射** - 编译时生成代码
- ✅ **零运行时开销** - 接近原生ADO.NET性能
- ✅ **类型安全** - 完整智能感知
- ✅ **多数据库** - 支持5种数据库（SQLite, PostgreSQL, MySQL, SQL Server, Oracle）
- ✅ **批量操作** - 高效的批量插入/更新
- ✅ **事务支持** - 完整的事务管理
- ✅ **占位符系统** - 灵活的SQL模板

---

## 📚 文档结构

```
Sqlx/
├── 📄 START_HERE.md (你在这里)      - 项目入口
├── 📄 PROJECT_AT_A_GLANCE.md        - 一页纸总览 ⭐
├── 📄 QUICKSTART.md                 - 5分钟快速上手 ⭐
├── 📄 README.md                     - 项目详细说明
├── 📄 PROJECT_STATUS.md             - 项目状态报告
├── 📄 CHANGELOG.md                  - 版本历史
└── 📁 docs/                         - 完整文档
    ├── API_REFERENCE.md             - API文档
    ├── BEST_PRACTICES.md            - 最佳实践
    ├── ADVANCED_FEATURES.md         - 高级特性
    ├── QUICK_START_GUIDE.md         - 详细教程
    └── ... (更多文档)
```

---

## 🔍 常见问题

### Q: 我应该先看哪个文档？
**A**: 如果您是新手，建议按顺序阅读：
1. [PROJECT_AT_A_GLANCE.md](PROJECT_AT_A_GLANCE.md) - 1分钟
2. [QUICKSTART.md](QUICKSTART.md) - 5分钟
3. 运行示例：`cd samples/TodoWebApi && dotnet run`

### Q: Sqlx支持哪些数据库？
**A**: 支持5种主流数据库：
- SQLite
- PostgreSQL
- MySQL
- SQL Server
- Oracle

### Q: 性能如何？
**A**: Sqlx性能接近原生ADO.NET：
- SELECT (1000行): ~170μs
- INSERT (100行): ~2.2ms
- 接近Dapper性能，优于EF Core

详细性能数据：[README.md#性能](README.md)

### Q: 如何运行测试？
**A**:
```bash
dotnet test tests/Sqlx.Tests
```

### Q: 在哪里找到示例代码？
**A**:
- `samples/TodoWebApi/` - 完整Web API示例
- `samples/SqlxDemo/` - 基础示例

---

## 💡 快速命令

```bash
# 构建项目
dotnet build

# 运行测试
dotnet test tests/Sqlx.Tests

# 运行示例
cd samples/TodoWebApi && dotnet run

# 性能测试
cd tests/Sqlx.Benchmarks && dotnet run -c Release
```

---

## 📞 获取帮助

- 📖 [完整文档](docs/)
- 🐛 [问题报告](https://github.com/Cricle/Sqlx/issues)
- 💬 [讨论区](https://github.com/Cricle/Sqlx/discussions)

---

## 🎯 下一步

选择您的路径：

1. **快速体验** → [QUICKSTART.md](QUICKSTART.md)
2. **深入了解** → [README.md](README.md)
3. **查看全貌** → [PROJECT_AT_A_GLANCE.md](PROJECT_AT_A_GLANCE.md)
4. **运行示例** → `cd samples/TodoWebApi && dotnet run`

---

**让我们开始吧！** 🚀

推荐首选：阅读 [QUICKSTART.md](QUICKSTART.md)（5分钟）
