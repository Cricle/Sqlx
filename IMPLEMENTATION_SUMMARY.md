# Table 占位符和 RepositoryFor 增强实现总结

## 实现内容

成功实现了以下功能：

1. **`{{table}}` 占位符的动态表名支持**
2. **`RepositoryForAttribute` 的 `TableName` 和 `Dialect` 属性**
3. **自动表名推断**（从实体类型推断）
4. **多行 SQL 模板支持**（C# 原生支持）

## 修改的文件

### 1. 核心实现

#### src/Sqlx/Placeholders/TablePlaceholderHandler.cs
- 添加了 `--param` 选项支持动态表名
- 实现了 `GetType()` 方法来区分静态和动态占位符
- 更新了 `Process()` 方法处理静态表名
- 实现了 `Render()` 方法处理动态表名

#### src/Sqlx/Annotations/RepositoryForAttribute.cs
- 添加了 `TableName` 属性（可选）
- 添加了 `Dialect` 属性（int 类型，可选）
- 更新了文档和示例

#### src/Sqlx.Generator/RepositoryGenerator.cs
- 修复了 `GetTableName` 方法，不再返回 "unknown"
- 添加了从实体类型自动推断表名的逻辑
- 添加了 `GetSqlDefineFromRepositoryFor` 方法
- 添加了 `GetTableNameFromRepositoryFor` 方法
- 支持从 `RepositoryForAttribute` 读取 `TableName` 和 `Dialect`

### 2. 测试文件

#### tests/Sqlx.Tests/TablePlaceholderTests.cs (新增)
- 19 个测试用例，覆盖所有功能
- 测试静态表名生成（6个测试）
- 测试动态表名生成（7个测试）
- 测试与 SqlTemplate 的集成（6个测试）
- 测试所有支持的数据库方言

### 3. 文档更新

#### docs/sql-templates.md
- 添加了动态表名的使用说明和示例
- 说明了 `--param` 选项的用法

#### AI-VIEW.md
- 在占位符速查表中添加了 `{{table --param tableName}}` 条目

#### src/Sqlx/Annotations/RepositoryForAttribute.cs
- 更新了示例代码，展示新的简化语法

### 4. 示例代码

#### samples/TablePlaceholderExample.cs (新增)
- 展示了静态和动态表名的使用场景
- 包含多租户应用和时间分区的实际用例

## 功能特性

### 1. 动态表名占位符

#### 静态表名（默认行为）
```csharp
[SqlTemplate("SELECT * FROM {{table}}")]
// 输出: SELECT * FROM [users]
```

#### 动态表名（新功能）
```csharp
[SqlTemplate("SELECT * FROM {{table --param tableName}}")]
Task<List<User>> GetFromTableAsync(string tableName);

// 使用
await repo.GetFromTableAsync("users_archive");
// 输出: SELECT * FROM [users_archive]
```

### 2. RepositoryFor 简化语法

#### 传统方式（仍然支持）
```csharp
[SqlDefine(SqlDefineTypes.SQLite)]
[TableName("users")]
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository : IUserRepository { }
```

#### 新的简化方式
```csharp
[RepositoryFor(typeof(IUserRepository), TableName = "users", Dialect = 0)] // 0 = MySql
public partial class UserRepository : IUserRepository { }
```

注意：`Dialect` 使用 int 值：
- 0 = MySql
- 1 = SqlServer
- 2 = PostgreSql
- 3 = Oracle
- 4 = DB2
- 5 = SQLite

### 3. 自动表名推断

如果没有指定 `TableName`，生成器会自动从实体类型推断：

```csharp
// 接口定义
public interface IUserRepository : ICrudRepository<User, long> { }

// 不需要 [TableName] 属性，自动使用 "User" 作为表名
[RepositoryFor(typeof(IUserRepository), Dialect = 5)] // SQLite
public partial class UserRepository : IUserRepository { }
```

推断优先级：
1. `[RepositoryFor(TableName = "...")]`
2. `[TableName("...")]` 属性
3. 从实体类型名称推断（如 `User` → `"User"`）
4. 从仓储类名推断（如 `UserRepository` → `"User"`）

### 4. 多行 SQL 模板支持

C# 原生支持多行字符串，可以直接使用：

```csharp
// 使用 @"..." 语法
[SqlTemplate(@"
    SELECT {{columns}}
    FROM {{table}}
    WHERE category = @category
      AND price BETWEEN @minPrice AND @maxPrice
    ORDER BY name
")]
Task<List<Product>> SearchAsync(string category, decimal minPrice, decimal maxPrice);

// 或使用 C# 11+ 的原始字符串字面量
[SqlTemplate("""
    SELECT {{columns}}
    FROM {{table}}
    WHERE category = @category
      AND price BETWEEN @minPrice AND @maxPrice
    ORDER BY name
""")]
Task<List<Product>> SearchAsync(string category, decimal minPrice, decimal maxPrice);
```

## 支持的数据库方言

所有方言都正确处理表名引号：
- **SQLite**: `[table_name]`
- **PostgreSQL**: `"table_name"`
- **MySQL**: `` `table_name` ``
- **SQL Server**: `[table_name]`
- **Oracle**: `"table_name"`
- **DB2**: `"table_name"`

## 使用场景

### 1. 多租户应用
```csharp
var tableName = $"logs_tenant_{tenantId}";
await repo.GetLogsFromTableAsync(tableName, startDate);
```

### 2. 时间分区
```csharp
var tableName = $"logs_{DateTime.Now:yyyy_MM}";
await repo.InsertIntoTableAsync(tableName, entry);
```

### 3. 归档表
```csharp
await repo.GetFromTableAsync("users_archive");
```

### 4. 简化仓储定义
```csharp
// 旧方式：3个属性
[SqlDefine(SqlDefineTypes.SQLite)]
[TableName("users")]
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository : IUserRepository { }

// 新方式：1个属性
[RepositoryFor(typeof(IUserRepository), TableName = "users", Dialect = 5)]
public partial class UserRepository : IUserRepository { }
```

## 测试结果

✅ 所有 1363 个测试通过（包括新增的 19 个测试）
✅ 无编译错误或警告
✅ 与现有功能完全兼容

## 向后兼容性

✅ 完全向后兼容
- 不带 `--param` 选项的 `{{table}}` 占位符行为保持不变
- 传统的 `[SqlDefine]` 和 `[TableName]` 属性仍然有效
- 现有代码无需修改即可继续工作

## API 设计

遵循了现有占位符和属性的设计模式：
- `{{table}}` 占位符与 `{{limit --param count}}` 保持一致
- `RepositoryForAttribute` 属性遵循 C# 属性命名约定
- 使用 int 类型的 `Dialect` 属性以支持属性参数
