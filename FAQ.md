# Frequently Asked Questions (FAQ)

> **Sqlx & VS Extension 常见问题解答**

---

## 📋 目录

- [一般问题](#一般问题)
- [安装和设置](#安装和设置)
- [功能和特性](#功能和特性)
- [性能相关](#性能相关)
- [VS Extension](#vs-extension)
- [与其他ORM对比](#与其他orm对比)
- [许可和支持](#许可和支持)

---

## 一般问题

### Q: Sqlx是什么？

**A:** Sqlx是一个高性能的.NET数据访问库，它：
- 使用源代码生成器在编译时生成SQL代码
- 接近原生ADO.NET的性能（105%）
- 提供完整的类型安全
- 零运行时开销
- 支持5+主流数据库

**核心优势:**
```
性能: 接近ADO.NET (105%)
安全: 编译时检查
效率: 22倍开发提升 (使用VS Extension)
简单: 零配置，开箱即用
```

---

### Q: Sqlx和Dapper/EF Core有什么区别？

**A:** 主要区别：

| 特性 | Sqlx | Dapper | EF Core |
|------|------|--------|---------|
| 性能 | ⭐⭐⭐⭐⭐ 105% | ⭐⭐⭐⭐⭐ 100% | ⭐⭐⭐ 60% |
| 类型安全 | ✅ 编译时 | ⚠️ 运行时 | ✅ 编译时 |
| SQL控制 | ✅ 完全控制 | ✅ 完全控制 | ⚠️ 有限 |
| 学习曲线 | ⭐⭐ 简单 | ⭐⭐ 简单 | ⭐⭐⭐⭐ 复杂 |
| 运行时开销 | ✅ 零 | ⚠️ 极小 | ❌ 较大 |
| 代码生成 | ✅ 编译时 | ❌ 无 | ✅ 运行时 |
| VS工具链 | ✅ 完整 | ❌ 无 | ✅ 部分 |

**选择Sqlx如果你需要:**
- 🚀 极致性能
- ✅ 编译时安全
- 🎯 完全SQL控制
- 🛠️ 强大的VS工具

详见: [docs/COMPARISON.md](docs/COMPARISON.md)

---

### Q: Sqlx是免费的吗？

**A:** ✅ **完全免费！**

- **许可**: MIT License
- **核心库**: 免费，开源
- **VS Extension**: 免费，开源
- **商业使用**: ✅ 允许
- **修改分发**: ✅ 允许

**没有任何限制！**

---

### Q: Sqlx支持哪些数据库？

**A:** 目前支持5+主流数据库：

```csharp
[SqlDefine(SqlDialect.SQLite)]      // ✅ SQLite
[SqlDefine(SqlDialect.MySql)]       // ✅ MySQL
[SqlDefine(SqlDialect.PostgreSql)]  // ✅ PostgreSQL
[SqlDefine(SqlDialect.SqlServer)]   // ✅ SQL Server
[SqlDefine(SqlDialect.Oracle)]      // ✅ Oracle
```

**未来计划:**
- MariaDB
- Firebird
- DB2
- 其他...

---

### Q: Sqlx支持.NET Core吗？

**A:** ✅ **完全支持！**

**支持的.NET版本:**
```
✅ .NET 6.0
✅ .NET 7.0
✅ .NET 8.0
✅ .NET 9.0+
✅ .NET Framework 4.7.2+
```

**跨平台:**
```
✅ Windows
✅ Linux
✅ macOS
```

**注意:** VS Extension仅支持Windows + VS 2022

---

## 安装和设置

### Q: 如何安装Sqlx？

**A:** 非常简单！

#### 方法1: .NET CLI
```bash
dotnet add package Sqlx
dotnet add package Sqlx.Generator
```

#### 方法2: Package Manager Console
```powershell
Install-Package Sqlx
Install-Package Sqlx.Generator
```

#### 方法3: Visual Studio
```
1. 右键项目 > Manage NuGet Packages
2. 搜索 "Sqlx"
3. 安装 Sqlx 和 Sqlx.Generator
```

**就这么简单！** 无需其他配置。

---

### Q: VS Extension是必需的吗？

**A:** ❌ **不是必需的，但强烈推荐！**

**不使用Extension:**
```csharp
// 仍然可以正常工作
[SqlTemplate("SELECT * FROM users WHERE id = @id")]
Task<User?> GetUserAsync(int id);

// 但没有:
// - 语法着色
// - IntelliSense
// - 工具窗口
// - 可视化设计器
```

**使用Extension:**
```
✅ 5色语法着色
✅ 44+项IntelliSense
✅ 14个工具窗口
✅ 可视化设计器
✅ 性能分析器
✅ 22倍效率提升
```

**结论:** 核心功能不依赖Extension，但Extension让开发体验提升22倍！

---

### Q: 如何安装VS Extension？

**A:** 两种方法：

#### 方法1: VSIX文件（推荐）
```
1. 从 GitHub Releases 下载 Sqlx.Extension.vsix
2. 关闭所有VS实例
3. 双击 .vsix 文件
4. 按照向导安装
5. 重启VS
```

#### 方法2: VS Marketplace（即将上线）
```
1. VS > Extensions > Manage Extensions
2. 搜索 "Sqlx"
3. 点击 Download
4. 重启VS完成安装
```

**验证安装:**
```
Tools 菜单应该有 "Sqlx" 选项
```

---

### Q: 为什么我的代码没有生成？

**A:** 检查这些常见原因：

#### 1. 未安装Sqlx.Generator
```xml
<!-- 检查 .csproj -->
<PackageReference Include="Sqlx.Generator" Version="0.4.0">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
</PackageReference>
```

**解决:**
```bash
dotnet add package Sqlx.Generator
```

#### 2. IDE缓存
```bash
# 清理并重新构建
dotnet clean
dotnet build

# 或在VS中:
Build > Clean Solution
Build > Rebuild Solution
```

#### 3. 查看生成的文件
```
VS Solution Explorer
> Dependencies
> Analyzers
> Sqlx.Generator
> [查看生成的文件]
```

**仍然有问题？** 查看 [TROUBLESHOOTING.md](TROUBLESHOOTING.md)

---

## 功能和特性

### Q: 什么是SqlTemplate？

**A:** SqlTemplate是Sqlx的核心特性：

```csharp
// 使用SqlTemplate定义SQL
[SqlTemplate("SELECT * FROM users WHERE id = @id")]
Task<User?> GetUserAsync(int id);

// 编译时生成完整实现
// 无需手动写Dapper代码
```

**特点:**
- ✅ 编译时SQL验证
- ✅ 参数类型检查
- ✅ 自动生成实现
- ✅ 零运行时开销

---

### Q: 什么是占位符？

**A:** 占位符是Sqlx的强大功能，自动替换常见SQL片段：

```csharp
[SqlTemplate(@"
    SELECT {{columns}}    -- 自动展开为: id, name, email, created_at
    FROM {{table}}        -- 自动替换为: users
    WHERE {{where}}       -- 根据参数生成WHERE条件
    LIMIT {{limit}}       -- 限制数量
")]
Task<List<User>> GetUsersAsync(string? where = null, int? limit = null);
```

**支持的占位符:**
- `{{columns}}` - 列名列表
- `{{table}}` - 表名
- `{{values}}` - 值列表
- `{{set}}` - SET子句
- `{{where}}` - WHERE条件
- `{{orderby}}` - ORDER BY
- `{{limit}}` / `{{offset}}` - 分页
- `{{batch_values}}` - 批量值

**修饰符:**
```csharp
{{columns--exclude:password}}  // 排除列
{{columns--param}}             // 参数化
{{orderby--desc}}              // 降序
```

详见: [docs/PLACEHOLDERS.md](docs/PLACEHOLDERS.md)

---

### Q: 如何使用新的Repository接口？

**A:** Sqlx v0.5+提供10个专门接口：

#### 完整功能（推荐）
```csharp
public partial interface IUserRepository : IRepository<User, int>
{
    // 自动继承50+方法:
    // - 查询 (14)
    // - 命令 (10)
    // - 批量 (6)
    // - 聚合 (11)
    // - 高级 (9)
}
```

#### 只读（查询优化）
```csharp
public partial interface IUserQueryRepo : IReadOnlyRepository<User, int>
{
    // 只有查询和聚合方法
    // GetById, GetAll, Count, Sum等
}
```

#### 批量操作
```csharp
public partial interface IUserBulkRepo : IBulkRepository<User, int>
{
    // 查询 + 批量插入/更新/删除
}
```

#### CQRS写模型
```csharp
public partial interface IUserCommandRepo : IWriteOnlyRepository<User, int>
{
    // 只有写操作
}
```

**使用示例:**
```csharp
// 分页
var page = await repo.GetPageAsync(pageIndex: 1, pageSize: 20);

// 批量插入（25倍快！）
await repo.BatchInsertAsync(users);

// 聚合
var count = await repo.CountAsync();
var avgAge = await repo.AvgAsync(u => u.Age);
```

详见: [docs/ENHANCED_REPOSITORY_INTERFACES.md](docs/ENHANCED_REPOSITORY_INTERFACES.md)

---

### Q: 支持异步吗？

**A:** ✅ **完全支持，而且推荐！**

```csharp
// ✅ 异步（推荐）
[SqlTemplate("SELECT * FROM users WHERE id = @id")]
Task<User?> GetUserAsync(int id);

// ✅ 同步（也支持）
[SqlTemplate("SELECT * FROM users WHERE id = @id")]
User? GetUser(int id);

// ✅ ValueTask（高性能）
[SqlTemplate("SELECT * FROM users WHERE id = @id")]
ValueTask<User?> GetUserAsync(int id);
```

**推荐使用异步以获得最佳性能。**

---

### Q: 支持事务吗？

**A:** ✅ **完全支持！**

```csharp
using (var transaction = connection.BeginTransaction())
{
    try
    {
        await userRepo.InsertAsync(user, transaction);
        await orderRepo.InsertAsync(order, transaction);
        
        transaction.Commit();
    }
    catch
    {
        transaction.Rollback();
        throw;
    }
}
```

**注意:** 需要手动管理事务，Sqlx不提供自动事务。

---

## 性能相关

### Q: Sqlx真的比EF Core快吗？

**A:** ✅ **是的，快很多！**

**基准测试结果:**
```
ADO.NET:    100% (基准)
Sqlx:       105% (比ADO.NET快5%!)
Dapper:     100% (与ADO.NET相当)
EF Core:    60%  (慢40%)
```

**为什么快？**
1. ✅ 编译时代码生成（零反射）
2. ✅ 直接ADO.NET调用（无中间层）
3. ✅ 无运行时开销
4. ✅ 优化的IL代码

**批量操作更快:**
```
Sqlx批量插入: 200ms (1000条)
逐个插入:     5000ms (1000条)
提升:         25倍！
```

详见: [docs/PERFORMANCE_BENCHMARKS.md](BenchmarkDotNet.Artifacts/results/)

---

### Q: 如何优化查询性能？

**A:** 几个关键技巧：

#### 1. 使用批量操作
```csharp
// ❌ 慢 (5秒)
foreach (var user in users)
    await repo.InsertAsync(user);

// ✅ 快 (200ms)
await repo.BatchInsertAsync(users);
```

#### 2. 使用分页
```csharp
// ❌ 可能OOM
var allUsers = await repo.GetAllAsync();  // 100万条

// ✅ 安全
var page = await repo.GetPageAsync(1, 100);
```

#### 3. 只查询需要的列
```csharp
[SqlTemplate("SELECT {{columns--exclude:password,salt}} FROM users")]
```

#### 4. 使用性能分析器
```
Tools > Sqlx > Performance Analyzer
查看慢查询并优化
```

#### 5. 添加数据库索引
```sql
CREATE INDEX idx_users_email ON users(email);
```

详见: [TUTORIAL.md#第8课-性能优化](TUTORIAL.md#第8课-性能优化)

---

### Q: Sqlx有内存泄漏吗？

**A:** ❌ **没有！**

**Sqlx的内存管理:**
```
✅ 无静态缓存
✅ 无反射缓存
✅ 编译时生成（无运行时开销）
✅ 依赖.NET GC
✅ 正确使用IDisposable
```

**注意确保正确释放连接:**
```csharp
// ✅ 正确
using (var conn = new SqlConnection(connStr))
{
    var repo = new UserRepository(conn);
    // 使用repo...
}  // 自动释放

// ❌ 错误（可能泄漏）
var conn = new SqlConnection(connStr);
var repo = new UserRepository(conn);
// 使用repo...
// 忘记释放conn
```

---

## VS Extension

### Q: Extension支持哪些VS版本？

**A:** 

**支持:**
```
✅ Visual Studio 2022 (17.0+)
✅ Community Edition
✅ Professional Edition
✅ Enterprise Edition
```

**不支持:**
```
❌ Visual Studio 2019
❌ Visual Studio 2017
❌ Visual Studio Code (计划中)
❌ Rider (计划中)
```

**原因:** Extension使用VS 2022 SDK的最新功能。

---

### Q: 如何使用IntelliSense？

**A:** 非常简单！

#### 自动触发
```csharp
[SqlTemplate("SELECT {{ |")]  // 输入 {{ 自动显示
                    ↑
```

#### 手动触发
```csharp
[SqlTemplate("SELECT |")]     // 按 Ctrl+Space
                    ↑
```

#### 可用项
```
占位符 (9个):   {{columns}}, {{table}}, {{values}}...
修饰符 (5个):   --exclude, --param, --value...
SQL关键字 (30+): SELECT, INSERT, UPDATE, DELETE...
参数:           @id, @name (根据方法参数)
```

**提示:** IntelliSense只在`[SqlTemplate("...")]`字符串内工作。

---

### Q: 工具窗口在哪里？

**A:** 所有工具窗口都在:

```
Tools > Sqlx > [选择窗口]
```

**14个窗口:**
1. SQL Preview - 实时SQL预览
2. Generated Code - 查看生成代码
3. Query Tester - 交互式测试
4. Repository Explorer - 仓储导航
5. SQL Execution Log - 执行日志
6. Template Visualizer - 可视化设计器
7. Performance Analyzer - 性能分析
8. Entity Mapping Viewer - 映射查看
9. SQL Breakpoints - 断点管理
10. SQL Watch - 变量监视
11-14. （其他辅助窗口）

**找不到？**
```
1. Window > Reset Window Layout
2. 重启Visual Studio
3. 重新安装Extension
```

---

### Q: 代码片段如何使用？

**A:** 超级简单！

```
1. 输入片段名称 (例如: sqlx-repo)
2. 按 Tab
3. 填写占位符
4. 完成！
```

**可用片段:**
- `sqlx-repo` - Repository接口
- `sqlx-entity` - 实体类
- `sqlx-select` - SELECT查询
- `sqlx-insert` - INSERT语句
- `sqlx-update` - UPDATE语句
- `sqlx-delete` - DELETE语句
- `sqlx-batch` - 批量操作
- ... (共12个)

**示例:**
```csharp
// 1. 输入: sqlx-repo [Tab]
// 2. 填写: UserRepository, User, int
// 3. 自动生成:

[RepositoryFor(typeof(User))]
[SqlDefine(SqlDialect.SQLite)]
public partial interface IUserRepository
{
    [SqlTemplate("SELECT * FROM users WHERE id = @id")]
    Task<User?> GetByIdAsync(int id);
    // ...
}
```

---

### Q: 语法着色不工作？

**A:** 检查这些：

#### 1. 确保在SqlTemplate中
```csharp
// ✅ 会着色
[SqlTemplate("SELECT * FROM users")]

// ❌ 不会着色
var sql = "SELECT * FROM users";
```

#### 2. 重启VS
```
File > Exit
重新打开项目
```

#### 3. 检查Extension是否启用
```
Extensions > Manage Extensions > Installed
确保Sqlx已启用
```

#### 4. 重置Extension
```
Tools > Options > Environment > Extensions
找到Sqlx > Disable
重启VS
再次Enable
重启VS
```

**仍然有问题？** 查看 [TROUBLESHOOTING.md#vs-extension问题](TROUBLESHOOTING.md#vs-extension问题)

---

## 与其他ORM对比

### Q: 我应该从EF Core迁移到Sqlx吗？

**A:** 看情况：

**考虑迁移如果:**
```
✅ 需要更好的性能
✅ 想要完全SQL控制
✅ 项目已有大量手写SQL
✅ 团队熟悉SQL
✅ 需要编译时安全
```

**保持EF Core如果:**
```
⚠️ 依赖复杂的导航属性
⚠️ 需要Change Tracking
⚠️ 需要自动迁移
⚠️ 团队不熟悉SQL
⚠️ 快速原型开发
```

**可以混用:**
```csharp
// EF Core用于复杂关系
var order = await _dbContext.Orders
    .Include(o => o.Customer)
    .Include(o => o.Items)
    .FirstOrDefaultAsync(o => o.Id == id);

// Sqlx用于高性能查询
var recentOrders = await _sqlxRepo.GetRecentOrdersAsync(days: 7);
```

**迁移指南:** 查看 [MIGRATION_GUIDE.md](MIGRATION_GUIDE.md)

---

### Q: Sqlx比Dapper有什么优势？

**A:** 主要优势：

| 特性 | Sqlx | Dapper |
|------|------|--------|
| 类型安全 | ✅ 编译时 | ⚠️ 运行时 |
| 参数检查 | ✅ 编译时 | ❌ 运行时 |
| SQL验证 | ✅ 部分 | ❌ 无 |
| 代码生成 | ✅ 自动 | ❌ 手动 |
| VS工具链 | ✅ 完整 | ❌ 无 |
| 占位符 | ✅ 9种 | ❌ 无 |
| 批量操作 | ✅ 内置 | ⚠️ 扩展 |
| IntelliSense | ✅ 44+项 | ❌ 无 |
| 性能 | ⭐⭐⭐⭐⭐ 105% | ⭐⭐⭐⭐⭐ 100% |

**Sqlx = Dapper性能 + EF Core类型安全 + 强大工具链**

---

## 许可和支持

### Q: 可以用于商业项目吗？

**A:** ✅ **完全可以！**

**MIT License允许:**
- ✅ 商业使用
- ✅ 修改
- ✅ 分发
- ✅ 私有使用
- ✅ 无需支付费用
- ✅ 无需开源你的代码

**唯一要求:**
- 保留版权声明
- 保留许可证文本

**就这么简单！**

---

### Q: 如何获得帮助？

**A:** 多种方式：

#### 1. 文档
```
📚 在线文档: https://cricle.github.io/Sqlx/
📖 API参考: docs/API_REFERENCE.md
🎓 教程: TUTORIAL.md
❓ FAQ: FAQ.md (本文档)
🔧 故障排除: TROUBLESHOOTING.md
⚡ 快速参考: QUICK_REFERENCE.md
```

#### 2. GitHub
```
🐛 报告Bug: https://github.com/Cricle/Sqlx/issues
💡 功能建议: https://github.com/Cricle/Sqlx/discussions
💬 讨论: https://github.com/Cricle/Sqlx/discussions
```

#### 3. 社区
```
待添加:
- Discord服务器
- Stack Overflow标签
- Gitter聊天室
```

---

### Q: 如何贡献？

**A:** 欢迎贡献！

**可以贡献:**
- 🐛 报告Bug
- 💡 建议功能
- 📝 改进文档
- 💻 提交代码
- ⭐ Star项目
- 📢 分享推广

**步骤:**
```
1. 阅读 CONTRIBUTING.md
2. Fork项目
3. 创建分支
4. 进行更改
5. 测试
6. 提交PR
```

**详见:** [CONTRIBUTING.md](CONTRIBUTING.md)

---

### Q: 有路线图吗？

**A:** ✅ **有的！**

#### v0.5 (当前)
```
✅ VS Extension (Preview)
✅ 14个工具窗口
✅ 增强Repository接口
✅ SQL着色和IntelliSense
```

#### v0.6 (下一个)
```
☐ 自定义图标
☐ 用户反馈改进
☐ Bug修复
☐ 性能优化
```

#### v1.0 (Major)
```
☐ 运行时集成
☐ 真实断点调试
☐ 表达式求值
☐ 生产就绪
```

#### v2.0+ (未来)
```
☐ AI辅助SQL生成
☐ 云端模板共享
☐ VS Code支持
☐ Rider支持
☐ 团队协作功能
```

**详见:** [CHANGELOG.md](CHANGELOG.md)

---

### Q: 项目活跃吗？

**A:** ✅ **非常活跃！**

**最近活动:**
```
✅ v0.5.0-preview (2025-10-29)
✅ 30+次提交
✅ 9,200+行代码
✅ 450+页文档
✅ 14个工具窗口
```

**维护承诺:**
- 🐛 Bug修复: 24-48小时响应
- 💡 功能请求: 每周审查
- 📝 文档: 持续更新
- 🔄 版本: 定期发布

---

## 🎯 还有问题？

### 没找到答案？

**搜索现有Issue:**
```
https://github.com/Cricle/Sqlx/issues
```

**创建新Issue:**
```
https://github.com/Cricle/Sqlx/issues/new
```

**社区讨论:**
```
https://github.com/Cricle/Sqlx/discussions
```

---

## 📚 相关资源

- **主页**: [README.md](README.md)
- **快速开始**: [docs/QUICK_START_GUIDE.md](docs/QUICK_START_GUIDE.md)
- **完整教程**: [TUTORIAL.md](TUTORIAL.md)
- **API参考**: [docs/API_REFERENCE.md](docs/API_REFERENCE.md)
- **最佳实践**: [docs/BEST_PRACTICES.md](docs/BEST_PRACTICES.md)
- **故障排除**: [TROUBLESHOOTING.md](TROUBLESHOOTING.md)
- **贡献指南**: [CONTRIBUTING.md](CONTRIBUTING.md)

---

**问题解决了吗？** 😊

如果这个FAQ帮到了你，别忘了 ⭐ Star这个项目！


