# Sqlx 全功能演示 (Full Feature Demo)

这是一个完整的示例程序，展示了 Sqlx 的所有核心功能。运行此程序即可看到 Sqlx 在实际场景中的强大能力。

## 🎯 演示功能

### 1. 基础 CRUD 操作
- ✅ 插入并返回 ID
- ✅ 插入并返回完整实体
- ✅ 单条查询
- ✅ 列表查询
- ✅ 条件查询
- ✅ 分页查询
- ✅ 更新操作
- ✅ 删除操作
- ✅ 聚合查询 (COUNT, SUM)

### 2. 软删除 (Soft Delete)
- ✅ 自动排除已删除记录
- ✅ 软删除操作
- ✅ 恢复已删除记录
- ✅ 查询包含已删除的记录

### 3. 审计字段 (Audit Fields)
- ✅ 自动记录创建时间和创建人
- ✅ 自动记录更新时间和更新人
- ✅ 完整的审计跟踪

### 4. 乐观锁 (Optimistic Locking)
- ✅ 版本号机制
- ✅ 并发冲突检测
- ✅ 防止数据覆盖

### 5. 批量操作 (Batch Operations)
- ✅ 高效批量插入（1000条记录）
- ✅ 自动分批处理
- ✅ 批量删除
- ✅ 性能统计

### 6. 复杂查询
- ✅ JOIN 查询
- ✅ 聚合查询 (GROUP BY, HAVING)
- ✅ 子查询
- ✅ TOP N 查询

### 7. 事务支持 (Transactions)
- ✅ 多操作原子性
- ✅ 事务提交
- ✅ 事务回滚
- ✅ 跨仓储事务

### 8. Expression 转 SQL
- ✅ Lambda 表达式
- ✅ 类型安全的条件查询
- ✅ 编译时检查

## 🚀 快速开始

### 前置要求
- .NET 9.0 或更高版本

### 运行步骤

1. **进入项目目录**
```bash
cd samples/FullFeatureDemo
```

2. **构建项目**
```bash
dotnet build
```

3. **运行示例**
```bash
dotnet run
```

### 预期输出

程序将依次演示所有功能，输出格式化的结果：

```
╔════════════════════════════════════════════════════════════════╗
║                                                                ║
║          Sqlx 全功能演示 (Full Feature Demo)                  ║
║                                                                ║
╚════════════════════════════════════════════════════════════════╝

🔧 初始化数据库...
   ✅ 数据库初始化完成

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
  1. 基础 CRUD 操作
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

📝 插入用户...
   ✅ 插入成功，ID: 1
   ✅ 插入成功，ID: 2
   ...
```

## 📚 项目结构

```
FullFeatureDemo/
├── FullFeatureDemo.csproj   # 项目配置
├── Models.cs                 # 数据模型定义
├── Repositories.cs           # 仓储接口和实现
├── Program.cs                # 演示程序入口
└── README.md                 # 本文件
```

## 🔍 代码亮点

### 1. 类型安全的查询
```csharp
// Lambda表达式自动转换为SQL
var users = await repo.SearchAsync(u => u.Age > 20 && u.Balance > 1000);
```

### 2. 零配置仓储
```csharp
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository(IDbConnection connection) : IUserRepository { }
```

### 3. 高性能批量操作
```csharp
[BatchOperation(MaxBatchSize = 1000)]
[SqlTemplate("INSERT INTO logs VALUES {{batch_values}}")]
Task<int> BatchInsertAsync(IEnumerable<(string, string, DateTime)> logs);
```

### 4. 乐观锁保护
```csharp
// 使用版本号防止并发冲突
await repo.UpdateBalanceAsync(accountId, newBalance, currentVersion);
```

## 📊 性能特点

- ⚡ 批量插入 1000 条记录: **~2-5ms**
- 🔍 单条查询: **~0.1ms**
- 💾 零反射，零运行时开销
- 🗑️ 低 GC 压力

## 🎓 学习建议

1. **先运行程序**，观察完整输出
2. **阅读 `Program.cs`**，理解每个演示的实现
3. **查看 `Repositories.cs`**，学习 Sqlx 特性使用
4. **修改代码**，尝试自己的场景

## 💡 扩展练习

尝试添加以下功能：

- [ ] 添加分类管理（Category CRUD）
- [ ] 实现订单商品关联（OrderItem）
- [ ] 添加更复杂的查询（如订单统计报表）
- [ ] 实现用户权限管理
- [ ] 切换到其他数据库（PostgreSQL, MySQL等）

## 🔗 相关链接

- [Sqlx GitHub](https://github.com/Cricle/Sqlx)
- [完整文档](../../README.md)
- [API 参考](../../docs/API_REFERENCE.md)
- [快速开始](../../QUICKSTART.md)

## 📝 许可证

MIT License - 详见项目根目录 License.txt

