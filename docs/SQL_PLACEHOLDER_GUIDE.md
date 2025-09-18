# 🎯 Sqlx SQL 占位符功能指南

## 📋 概述

Sqlx 的 SQL 占位符功能允许您在 SQL 模板中使用类似 `{{columns}}`, `{{table}}`, `{{where}}` 等占位符，这些占位符会在编译时被自动替换为相应的 SQL 代码。这个功能特别适用于 RepositoryFor 模式，让一套通用的 SQL 模板能够适应不同的实体类型和仓储需求。

## 🚀 核心优势

### 1. **提高代码重用性**
- 一套 SQL 模板可以适应多个实体类型
- 标准化的查询模式可以跨项目复用
- 减少重复的 SQL 代码编写

### 2. **增强灵活性**
- 动态列选择：`{{columns:exclude=salary,email}}`
- 条件控制：`{{where:default=is_active=1}}`
- 灵活的表别名：`{{table:alias=u}}`

### 3. **保持类型安全**
- 编译时处理，零运行时开销
- 自动类型推断和验证
- 与现有 Sqlx 特性完全兼容

### 4. **简化维护**
- 修改实体结构自动更新相关 SQL
- 集中管理查询模板
- 减少因字段变更导致的 SQL 错误

## 📖 支持的占位符

### 🔧 基础占位符

| 占位符 | 功能 | 示例 |
|--------|------|------|
| `{{columns}}` | 所有列 | `SELECT {{columns}} FROM Users` |
| `{{table}}` | 表名 | `FROM {{table}}` |
| `{{where}}` | WHERE 条件 | `WHERE {{where:default=1=1}}` |
| `{{orderby}}` | 排序 | `{{orderby:default=Id ASC}}` |
| `{{select}}` | SELECT 语句 | `{{select}} FROM Users` |
| `{{count}}` | 计数 | `SELECT {{count}} FROM Users` |

### 🏗️ 操作占位符

| 占位符 | 功能 | 示例 |
|--------|------|------|
| `{{insert}}` | INSERT 语句 | `{{insert}} VALUES {{values}}` |
| `{{update}}` | UPDATE 语句 | `{{update}} SET name = @name` |
| `{{values}}` | VALUES 子句 | `VALUES {{values}}` |
| `{{joins}}` | JOIN 子句 | `{{joins:type=INNER,table=Dept,on=u.DeptId=d.Id}}` |

## 🎨 使用示例

### 基础查询模板

```csharp
public interface IFlexibleUserRepository
{
    // 基础查询 - 自动生成所有列
    [Sqlx("{{select}} FROM {{table}} WHERE {{where:default=is_active=1}}")]
    Task<IList<User>> GetActiveUsersAsync();

    // 排除敏感信息
    [Sqlx("{{select:exclude=salary,email}} FROM {{table}} WHERE department_id = @deptId")]
    Task<IList<User>> GetPublicUserInfoAsync(int deptId);

    // 只获取指定列
    [Sqlx("{{select:include=name,age}} FROM {{table}} WHERE age > @minAge")]
    Task<IList<User>> GetBasicInfoAsync(int minAge);
}
```

### 编译时转换结果

```csharp
// 原始模板
[Sqlx("{{select}} FROM {{table}} WHERE {{where:default=is_active=1}}")]

// 编译时生成
[Sqlx("SELECT [Id], [Name], [Email], [Age], [IsActive], [DepartmentId], [Salary], [HireDate] FROM [User] WHERE is_active=1")]
```

### 复杂查询示例

```csharp
public interface IAdvancedRepository
{
    // 统计查询
    [Sqlx("SELECT department_id, {{count}}, AVG(salary) FROM {{table}} GROUP BY department_id")]
    Task<IList<DepartmentStats>> GetDepartmentStatsAsync();

    // JOIN 查询
    [Sqlx(@"{{select:exclude=salary}} FROM {{table:alias=u}} 
            {{joins:type=INNER,table=Department,on=u.DepartmentId=d.Id,alias=d}}
            WHERE u.is_active = 1 {{orderby:default=u.Name}}")]
    Task<IList<UserWithDepartment>> GetUsersWithDepartmentAsync();

    // 分页查询
    [Sqlx("{{select}} FROM {{table}} WHERE {{where}} {{orderby}} LIMIT @limit OFFSET @offset")]
    Task<IList<User>> GetPagedUsersAsync(int limit, int offset);
}
```

### CRUD 操作模板

```csharp
public interface ICrudRepository<T>
{
    // 插入操作
    [Sqlx("{{insert}} VALUES {{values}}")]
    Task<int> CreateAsync(T entity);

    // 更新操作
    [Sqlx("{{update}} SET {{columns:exclude=Id}} WHERE Id = @Id")]
    Task<int> UpdateAsync(T entity);

    // 部分更新
    [Sqlx("{{update}} SET {{columns:include=Name,Email}} WHERE Id = @Id")]
    Task<int> UpdateBasicInfoAsync(T entity);

    // 删除操作
    [Sqlx("DELETE FROM {{table}} WHERE Id = @Id")]
    Task<int> DeleteAsync(int id);
}
```

## 🔧 占位符参数详解

### 列相关参数

#### `{{columns}}` 参数
- `exclude`: 排除指定列
- `include`: 只包含指定列

```csharp
// 排除敏感字段
"{{columns:exclude=Salary,Email,Password}}"
// 生成: [Id], [Name], [Age], [IsActive], [DepartmentId], [HireDate]

// 只包含基本信息
"{{columns:include=Id,Name,Age}}"
// 生成: [Id], [Name], [Age]
```

#### `{{select}}` 参数
与 `{{columns}}` 相同的参数，但会自动添加 SELECT 关键字

```csharp
"{{select:exclude=Salary}}"
// 生成: SELECT [Id], [Name], [Email], [Age], [IsActive], [DepartmentId], [HireDate]
```

### 表相关参数

#### `{{table}}` 参数
- `alias`: 表别名

```csharp
"{{table:alias=u}}"
// 生成: [User] u
```

### 条件相关参数

#### `{{where}}` 参数
- `default`: 默认条件

```csharp
"{{where:default=is_active=1}}"
// 生成: is_active=1 (当没有其他WHERE条件时)
```

#### `{{orderby}}` 参数
- `default`: 默认排序

```csharp
"{{orderby:default=CreatedAt DESC}}"
// 生成: ORDER BY CreatedAt DESC
```

### JOIN 相关参数

#### `{{joins}}` 参数
- `type`: JOIN 类型 (INNER, LEFT, RIGHT, FULL)
- `table`: 要连接的表名
- `on`: 连接条件
- `alias`: 表别名

```csharp
"{{joins:type=INNER,table=Department,on=u.DepartmentId=d.Id,alias=d}}"
// 生成: INNER JOIN [Department] d ON u.DepartmentId=d.Id
```

### 聚合相关参数

#### `{{count}}` 参数
- `column`: 指定计数列

```csharp
"{{count:column=Id}}"
// 生成: COUNT([Id])

"{{count}}"
// 生成: COUNT(*)
```

## 🏗️ RepositoryFor 集成

### 通用仓储模式

```csharp
// 定义通用接口
public interface IGenericRepository<T>
{
    [Sqlx("{{select}} FROM {{table}}")]
    Task<IList<T>> GetAllAsync();

    [Sqlx("{{select}} FROM {{table}} WHERE Id = @id")]
    Task<T?> GetByIdAsync(int id);

    [Sqlx("{{insert}} VALUES {{values}}")]
    Task<int> CreateAsync(T entity);

    [Sqlx("{{update}} SET {{columns:exclude=Id}} WHERE Id = @Id")]
    Task<int> UpdateAsync(T entity);

    [Sqlx("DELETE FROM {{table}} WHERE Id = @id")]
    Task<int> DeleteAsync(int id);
}

// 自动实现不同实体的仓储
[RepositoryFor(typeof(IGenericRepository<User>))]
public partial class UserRepository : IGenericRepository<User> { }

[RepositoryFor(typeof(IGenericRepository<Product>))]
public partial class ProductRepository : IGenericRepository<Product> { }

[RepositoryFor(typeof(IGenericRepository<Order>))]
public partial class OrderRepository : IGenericRepository<Order> { }
```

### 特化仓储模式

```csharp
public interface IUserRepository : IGenericRepository<User>
{
    // 继承通用方法，添加特化查询
    [Sqlx("{{select:exclude=Salary}} FROM {{table}} WHERE DepartmentId = @deptId")]
    Task<IList<User>> GetByDepartmentAsync(int deptId);

    [Sqlx("{{select}} FROM {{table}} WHERE IsActive = @isActive {{orderby:default=Name}}")]
    Task<IList<User>> GetByStatusAsync(bool isActive);
}
```

## 📊 性能优势

### 编译时处理
- 占位符在编译时替换，零运行时开销
- 生成的 SQL 与手写 SQL 性能相同
- 无反射或动态 SQL 构建

### 类型安全
- 编译时验证占位符语法
- 自动类型推断和匹配
- IDE 智能提示支持

### 代码优化
- 减少重复代码 60-80%
- 统一的查询模式
- 更容易维护和修改

## 🔄 迁移指南

### 从传统 SQL 迁移

#### 迁移前
```csharp
public interface IUserRepository
{
    [Sqlx("SELECT Id, Name, Email, Age, IsActive, DepartmentId, Salary, HireDate FROM User WHERE IsActive = 1")]
    Task<IList<User>> GetActiveUsersAsync();

    [Sqlx("SELECT Id, Name, Email, Age, IsActive, DepartmentId, Salary, HireDate FROM User WHERE Age > @age")]
    Task<IList<User>> GetUsersByAgeAsync(int age);

    [Sqlx("INSERT INTO User (Name, Email, Age, IsActive, DepartmentId, Salary, HireDate) VALUES (@Name, @Email, @Age, @IsActive, @DepartmentId, @Salary, @HireDate)")]
    Task<int> CreateUserAsync(User user);
}
```

#### 迁移后
```csharp
public interface IUserRepository
{
    [Sqlx("{{select}} FROM {{table}} WHERE IsActive = 1")]
    Task<IList<User>> GetActiveUsersAsync();

    [Sqlx("{{select}} FROM {{table}} WHERE Age > @age")]
    Task<IList<User>> GetUsersByAgeAsync(int age);

    [Sqlx("{{insert}} VALUES {{values}}")]
    Task<int> CreateUserAsync(User user);
}
```

### 迁移策略

1. **渐进式迁移**: 新方法使用占位符，旧方法保持不变
2. **批量替换**: 使用查找替换工具批量更新常见模式
3. **测试验证**: 确保生成的 SQL 与原始 SQL 一致

## 🛠️ 最佳实践

### 1. 占位符命名
- 使用有意义的参数名
- 保持一致的命名约定
- 适当使用默认值

### 2. 模板复用
- 创建通用的查询模板
- 在多个仓储间共享模板
- 建立标准的占位符库

### 3. 性能考虑
- 避免过度复杂的占位符组合
- 合理使用 exclude/include 参数
- 考虑索引对生成 SQL 的影响

### 4. 维护性
- 在注释中说明复杂占位符的用途
- 定期检查生成的 SQL 质量
- 建立占位符使用规范

## 🔮 未来扩展

### 计划中的功能
- 更多聚合函数占位符
- 条件占位符 (IF/ELSE 逻辑)
- 自定义占位符支持
- 数据库方言特定优化

### 社区贡献
- 欢迎提交新的占位符需求
- 参与占位符模板库建设
- 分享最佳实践案例

## 📝 总结

Sqlx 的 SQL 占位符功能为现代 .NET 数据访问带来了革命性的改进：

- **🎯 高效开发**: 减少 60-80% 的重复 SQL 代码
- **🔧 灵活配置**: 动态控制查询的列、条件、排序等
- **🛡️ 类型安全**: 编译时验证，运行时零开销
- **🔄 易于维护**: 统一的模板管理，自动适配实体变更
- **🚀 性能优异**: 生成的 SQL 与手写代码性能相同

通过合理使用占位符，您可以构建更加灵活、可维护的数据访问层，同时保持 Sqlx 的高性能和类型安全特性。
