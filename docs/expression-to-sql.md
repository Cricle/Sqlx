# ExpressionToSql 指南

将 C# LINQ 表达式转换为类型安全的 SQL 查询。

## 🎯 特性

- 🎯 **类型安全** - 编译时检查
- 🔄 **动态查询** - 条件动态构建  
- 🌐 **多数据库** - 支持不同方言
- ⚡ **高性能** - 编译时优化

## 🚀 基础用法

### 创建实例

```csharp
// 不同数据库方言
var query1 = ExpressionToSql<User>.ForSqlServer();
var query2 = ExpressionToSql<User>.ForMySql();
var query3 = ExpressionToSql<User>.ForPostgreSQL();
var query4 = ExpressionToSql<User>.ForSqlite();
```

### 基本查询

```csharp
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

// 简单条件
var query = ExpressionToSql<User>.ForSqlServer()
    .Where(u => u.IsActive)
    .Where(u => u.Age >= 18);

string sql = query.ToSql();
// 生成: SELECT * FROM [User] WHERE ([IsActive] = 1) AND ([Age] >= 18)
```

## 🔍 WHERE 条件

### 比较操作
```csharp
query.Where(u => u.Id == 1);              // 等于
query.Where(u => u.Id != 1);              // 不等于
query.Where(u => u.Age > 18);             // 大于
query.Where(u => u.Age >= 18);            // 大于等于
```

### 逻辑操作
```csharp
// AND 条件
query.Where(u => u.IsActive && u.Age >= 18);

// OR 条件  
query.Where(u => u.Age < 18 || u.Age > 65);

// NOT 条件
query.Where(u => !u.IsActive);
```

### 字符串操作
```csharp
query.Where(u => u.Name.Contains("张"));    // LIKE '%张%'
query.Where(u => u.Name.StartsWith("张")); // LIKE '张%'
query.Where(u => u.Name.EndsWith("三"));    // LIKE '%三'
```

### 🆕 算术运算

```csharp
// 基础运算
query.Where(u => u.Age + 5 > 30);           // 加法
query.Where(u => u.Price * 0.8 < 100);      // 乘法
query.Where(u => u.Id % 2 == 0);            // 🆕 模运算 (偶数ID)

// UPDATE 中使用
var updateQuery = ExpressionToSql<User>.ForSqlServer()
    .Set(u => u.Age, u => u.Age + 1)         // 年龄+1
    .Where(u => u.Id == 1);
```

### 日期时间操作

```csharp
var today = DateTime.Today;
var lastWeek = DateTime.Now.AddDays(-7);

query.Where(u => u.CreatedAt >= today);     // 日期比较
query.Where(u => u.CreatedAt > lastWeek);   // 日期范围
```

## 📊 排序和分页

```csharp
// 排序
var query = ExpressionToSql<User>.ForSqlServer()
    .OrderBy(u => u.Name)                    // 升序
    .OrderByDescending(u => u.CreatedAt);    // 降序

// 分页
var pagedQuery = ExpressionToSql<User>.ForSqlServer()
    .Where(u => u.IsActive)
    .OrderBy(u => u.Id)
    .Skip(20)                                // 跳过前20条
    .Take(10);                               // 获取10条
```

## 🔄 UPDATE 操作

```csharp
// 设置值
var updateQuery = ExpressionToSql<User>.ForSqlServer()
    .Set(u => u.Name, "新名称")
    .Set(u => u.IsActive, true)
    .Where(u => u.Id == 1);

// 表达式设置
var updateQuery2 = ExpressionToSql<User>.ForSqlServer()
    .Set(u => u.Age, u => u.Age + 1)         // 年龄加1
    .Where(u => u.Id == 1);
```

## 🌐 多数据库方言

```csharp
var condition = u => u.IsActive && u.Age >= 18;

// SQL Server: [User] WHERE ([IsActive] = 1)
var sqlServer = ExpressionToSql<User>.ForSqlServer().Where(condition);

// MySQL: `User` WHERE (`IsActive` = 1) 
var mysql = ExpressionToSql<User>.ForMySql().Where(condition);

// PostgreSQL: "User" WHERE ("IsActive" = 1)
var pg = ExpressionToSql<User>.ForPostgreSQL().Where(condition);
```

## 🔗 Repository 集成

```csharp
public interface IUserService
{
    [Sqlx]
    IList<User> QueryUsers([ExpressionToSql] ExpressionToSql<User> query);
    
    [Sqlx]
    int UpdateUsers([ExpressionToSql] ExpressionToSql<User> updateQuery);
}

// 使用
var activeUsers = userRepo.QueryUsers(
    ExpressionToSql<User>.ForSqlServer()
        .Where(u => u.IsActive && u.Age >= 18)
        .OrderBy(u => u.Name)
);
```

## 💡 最佳实践

### 动态条件

```csharp
public IList<User> SearchUsers(string? name, int? minAge, bool? isActive)
{
    var query = ExpressionToSql<User>.ForSqlServer();
    
    if (!string.IsNullOrEmpty(name))
        query = query.Where(u => u.Name.Contains(name));
    
    if (minAge.HasValue)
        query = query.Where(u => u.Age >= minAge.Value);
    
    if (isActive.HasValue)
        query = query.Where(u => u.IsActive == isActive.Value);
    
    return userRepo.QueryUsers(query.OrderBy(u => u.Name));
}
```

### 性能建议

```csharp
// ✅ 推荐：结合其他条件
.Where(u => u.IsActive && u.Id % 2 == 0)

// ⚠️ 注意：大表上的纯模运算可能较慢
.Where(u => u.Id % 1000 == 1)  // 建议配合索引
```

---

完整示例和高级用法请参考项目示例代码。