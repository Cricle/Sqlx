# Sqlx 全特性演示 (Full Feature Demo)

## 📚 概述

这是一个全面展示Sqlx所有特性的示例项目，特别是**70+占位符系统**的使用。与传统SQL不同，本示例充分利用Sqlx的占位符、表达式树、批量操作等强大功能。

## ✨ 主要特性展示

### 1. 基础占位符 (7个核心)

| 占位符 | 用途 | 示例 |
|--------|------|------|
| `{{columns}}` | 自动列名列表 | `SELECT {{columns}} FROM users` |
| `{{table}}` | 表名引用 | `FROM {{table}}` |
| `{{values}}` | INSERT值 | `VALUES {{values}}` |
| `{{set}}` | UPDATE SET | `UPDATE {{table}} {{set}}` |
| `{{orderby}}` | 排序 | `{{orderby created_at --desc}}` |
| `{{limit}}` | 限制行数 | `{{limit}}` → `LIMIT @limit` |
| `{{offset}}` | 偏移量 | `{{offset}}` → `OFFSET @offset` |

### 2. 方言占位符 (跨数据库)

| 占位符 | SQLite | PostgreSQL | MySQL | SQL Server |
|--------|--------|-----------|-------|------------|
| `{{bool_true}}` | `1` | `true` | `1` | `1` |
| `{{bool_false}}` | `0` | `false` | `0` | `0` |
| `{{current_timestamp}}` | `CURRENT_TIMESTAMP` | `CURRENT_TIMESTAMP` | `CURRENT_TIMESTAMP` | `GETDATE()` |

### 3. 聚合函数占位符 (5个)

```csharp
{{count}}           // SELECT COUNT(*) FROM users
{{sum balance}}     // SELECT SUM(balance) FROM users
{{avg age}}         // SELECT AVG(age) FROM users  
{{max balance}}     // SELECT MAX(balance) FROM users
{{min age}}         // SELECT MIN(age) FROM users
```

### 4. 字符串函数占位符 (8个)

```csharp
{{like @pattern}}           // name LIKE @pattern
{{in @ids}}                 // id IN (@ids)
{{between @min, @max}}      // price BETWEEN @min AND @max
{{coalesce email, 'none'}}  // COALESCE(email, 'none')
{{distinct age}}            // SELECT DISTINCT age
{{group_concat msg, ', '}}  // GROUP_CONCAT(msg, ', ')
{{concat name, email}}      // CONCAT(name, email)
{{upper name}}              // UPPER(name)
```

### 5. 复杂查询占位符 (10+)

```csharp
// JOIN操作
{{join --type inner --table orders --on user_id = users.id}}

// 分组和过滤
{{groupby category}}
{{having --condition 'COUNT(*) > 10'}}

// 条件表达式
{{case --when 'age > 18' --then 'Adult' --else 'Minor'}}

// 子查询
{{exists --query 'SELECT 1 FROM orders WHERE user_id = users.id'}}

// 窗口函数
{{row_number --partition_by category --order_by price DESC}}

// 集合操作
{{union}}
{{union all}}
```

### 6. 批量操作占位符

```csharp
// 批量插入
[SqlTemplate("INSERT INTO {{table}} (name, age) VALUES {{batch_values}}")]
[BatchOperation(MaxBatchSize = 1000)]
Task<int> BatchInsertAsync(IEnumerable<User> users);

// 自动分批处理，支持10000+条数据
```

### 7. 表达式树查询

```csharp
// 使用 {{where}} 占位符 + C# Lambda表达式
[SqlTemplate("SELECT {{columns}} FROM {{table}} {{where}}")]
Task<List<User>> FindUsersAsync([ExpressionToSql] Expression<Func<User, bool>> predicate);

// 调用示例
await repo.FindUsersAsync(u => u.Age >= 18 && u.Balance > 5000);
// 生成: WHERE age >= 18 AND balance > 5000

await repo.FindUsersAsync(u => u.Name.Contains("张"));
// 生成: WHERE name LIKE '%张%'

await repo.FindUsersAsync(u => u.Email.EndsWith("@example.com"));
// 生成: WHERE email LIKE '%@example.com'
```

## 🎯 演示内容

运行 `dotnet run` 后，将依次展示以下8个演示：

### Demo 1: 基础占位符演示
- `{{columns}}` - 自动列名
- `{{table}}` - 表名引用
- `{{orderby}}` - 排序
- `{{limit}}` + `{{offset}}` - 分页
- `{{set}}` - 更新

### Demo 2: 方言占位符演示
- `{{bool_true}}` / `{{bool_false}}` - 布尔值跨数据库
- `{{current_timestamp}}` - 当前时间戳

### Demo 3: 聚合函数占位符
- `{{count}}` - 计数
- `{{sum}}` - 求和
- `{{avg}}` - 平均值
- `{{max}}` - 最大值

### Demo 4: 字符串函数占位符
- `{{like}}` - 模糊搜索
- `{{in}}` - IN查询
- `{{between}}` - 范围查询
- `{{distinct}}` - 去重
- `{{coalesce}}` - NULL处理

### Demo 5: 批量操作占位符
- `{{batch_values}}` - 批量插入（1000条数据）
- `{{group_concat}}` - 字符串聚合

### Demo 6: 复杂查询占位符
- `{{join}}` - JOIN操作
- `{{groupby}}` + `{{having}}` - 分组和过滤
- `{{case}}` - 条件表达式
- `{{exists}}` - 子查询

### Demo 7: 表达式树查询
- 简单条件: `u => u.Age >= 18`
- 字符串: `u => u.Name.Contains("张")`
- 复杂条件: `u => (u.Age >= 25 && u.Balance > 5000) || ...`
- 分页: 表达式 + `{{limit}}` + `{{offset}}`
- 聚合: 表达式 + `{{count}}`

### Demo 8: 高级特性
- **软删除** - `[SoftDelete]` 特性
- **审计字段** - `[AuditFields]` 特性
- **乐观锁** - `[ConcurrencyCheck]` 特性

## 🚀 快速开始

```bash
# 克隆仓库
git clone https://github.com/Cricle/Sqlx.git

# 进入示例目录
cd Sqlx/samples/FullFeatureDemo

# 运行示例
dotnet run
```

## 📊 输出示例

```
╔════════════════════════════════════════════════════════════════╗
║                                                                ║
║      Sqlx 全特性演示 (Full Feature with Placeholders)         ║
║         展示 70+ 占位符、表达式树、批量操作等                  ║
║                                                                ║
╚════════════════════════════════════════════════════════════════╝

🔧 初始化数据库...
   ✅ 数据库初始化完成

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
  1. 基础占位符演示 ({{columns}}, {{table}}, {{orderby}}, {{limit}})
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

📝 插入测试数据（使用 {{values}} 占位符）...
[SQL] InsertAsync: INSERT INTO [users] (name, email, age, balance, created_at, is_active) VALUES (@name, @email, @age, @balance, @createdAt, @isActive)
   ✅ 已插入 5 个用户

🔹 使用 {{columns}} 占位符查询所有列
[SQL] GetAllAsync: SELECT id, name, email, age, balance, created_at, is_active FROM [users]
   ✅ 查询到 5 个用户
   SQL: SELECT {{columns}} FROM {{table}}

🔹 使用 {{orderby balance --desc}} {{limit}} 占位符
[SQL] GetTopRichUsersAsync: SELECT id, name, email, age, balance, created_at, is_active FROM [users] ORDER BY balance DESC LIMIT @limit
   ✅ 余额最高的 3 个用户:
      - 钱七: ¥15,000.00
      - 赵六: ¥12,000.00
      - 李四: ¥8,500.00
   SQL: SELECT {{columns}} FROM {{table}} {{orderby balance --desc}} {{limit}}

...
```

## 🔍 关键代码对比

### ❌ 传统方式（原生SQL）
```csharp
[SqlTemplate("SELECT * FROM users WHERE age >= @minAge ORDER BY balance DESC LIMIT @limit")]
Task<List<User>> GetTopUsersAsync(int minAge, int limit);
```

### ✅ Sqlx方式（全特性）
```csharp
[SqlTemplate("SELECT {{columns}} FROM {{table}} {{where}} {{orderby balance --desc}} {{limit}}")]
Task<List<User>> GetTopUsersAsync(
    [ExpressionToSql] Expression<Func<User, bool>> predicate,
    int? limit = null);

// 调用
await repo.GetTopUsersAsync(u => u.Age >= 18, 10);
```

**优势**：
- ✅ 自动列名管理（`{{columns}}`）
- ✅ 类型安全的表名（`{{table}}`）
- ✅ C#表达式代替SQL字符串（`{{where}}`）
- ✅ 跨数据库兼容（`{{orderby}}`, `{{limit}}`）
- ✅ 可选参数支持（`int? limit`）

## 📖 详细文档

| 文档 | 说明 |
|------|------|
| [PLACEHOLDER_REFERENCE.md](../../docs/PLACEHOLDER_REFERENCE.md) | 70+ 占位符完整参考 |
| [PLACEHOLDERS.md](../../docs/PLACEHOLDERS.md) | 占位符详细教程 |
| [TUTORIAL.md](../../TUTORIAL.md) | 完整教程（10课） |
| [API_REFERENCE.md](../../docs/API_REFERENCE.md) | API参考手册 |
| [BEST_PRACTICES.md](../../docs/BEST_PRACTICES.md) | 最佳实践指南 |

## 💡 核心优势

### 1. 跨数据库兼容
```csharp
// 同一SQL模板，支持4种数据库
[SqlDefine(SqlDefineTypes.SQLite)]    // SQLite
[SqlDefine(SqlDefineTypes.PostgreSql)] // PostgreSQL  
[SqlDefine(SqlDefineTypes.MySql)]      // MySQL
[SqlDefine(SqlDefineTypes.SqlServer)]  // SQL Server
```

### 2. 类型安全
```csharp
// 编译时验证参数
[SqlTemplate("SELECT {{columns}} WHERE id = @id")]
Task<User?> GetByIdAsync(long id);  // ✅ 参数类型匹配

[SqlTemplate("SELECT {{columns}} WHERE id = @userId")]
Task<User?> GetByIdAsync(long id);  // ❌ 编译错误：参数名不匹配
```

### 3. 极致性能
- 编译时代码生成，零反射
- 接近原生ADO.NET性能（仅慢5%）
- 批量操作比循环快25倍

### 4. 易于维护
```csharp
// 修改实体类，自动更新SQL
public class User 
{
    public long Id { get; set; }
    public string Name { get; set; }
    // 添加新字段
    public string Phone { get; set; }  // ✅ {{columns}} 自动包含
}
```

## 🎓 学习路径

1. **初学者**: 运行本示例 → 阅读 [QUICK_START_GUIDE.md](../../docs/QUICK_START_GUIDE.md)
2. **进阶**: 学习 [TUTORIAL.md](../../TUTORIAL.md) 第1-5课
3. **高级**: 阅读 [PLACEHOLDER_REFERENCE.md](../../docs/PLACEHOLDER_REFERENCE.md) 掌握70+占位符
4. **实践**: 参考 [TodoWebApi](../TodoWebApi/) 构建真实项目

## 🤝 贡献

发现问题或有改进建议？欢迎：
- [提交Issue](https://github.com/Cricle/Sqlx/issues)
- [提交PR](https://github.com/Cricle/Sqlx/pulls)
- [参与讨论](https://github.com/Cricle/Sqlx/discussions)

## 📄 许可证

MIT License - 详见 [LICENSE.txt](../../License.txt)

---

**开始使用Sqlx，让数据访问回归简单！** 🚀
