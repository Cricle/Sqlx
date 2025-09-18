# SqlExecuteType 迁移指南

## 📋 概述

从 Sqlx v2.1 开始，`SqlExecuteTypeAttribute` 特性已被弃用。其功能已完全迁移到 `ExpressionToSql` 中，以简化框架架构并提供更一致的 API 体验。

## 🔄 迁移对照表

### INSERT 操作

**旧方式 (已弃用):**
```csharp
[SqlExecuteType(SqlOperation.Insert, "users")]
[Sqlx("INSERT INTO users (name, email, age) VALUES (@name, @email, @age)")]
public partial Task<int> CreateUserAsync(string name, string email, int age);
```

**新方式 (推荐):**
```csharp
// 方式1：通过方法名自动推断 + 实体参数
public partial Task<int> InsertUserAsync(User user);

// 方式2：使用 ExpressionToSql 参数指定列
public partial Task<int> CreateUserAsync(
    [ExpressionToSql] Expression<Func<User, object>> columns,
    object values);

// 方式3：结合部分 SQL 模板
[Sqlx("INSERT INTO users")]
public partial Task<int> CreateUserAsync(User user);
```

### UPDATE 操作

**旧方式 (已弃用):**
```csharp
[SqlExecuteType(SqlOperation.Update, "users")]
[Sqlx("UPDATE users SET salary = @salary WHERE id = @id")]
public partial Task<int> UpdateUserSalaryAsync(int id, decimal salary);
```

**新方式 (推荐):**
```csharp
// 方式1：通过方法名自动推断
public partial Task<int> UpdateUserAsync(int id, User updates);

// 方式2：使用 ExpressionToSql 参数
public partial Task<int> UpdateUserSalaryAsync(
    [ExpressionToSql] Expression<Func<User, object>> setValues,
    [ExpressionToSql] Expression<Func<User, bool>> whereCondition);

// 方式3：链式 API (高级用法)
var updateSql = ExpressionToSql<User>.ForSqlServer()
    .Update()
    .Set(u => u.Salary, 50000)
    .Where(u => u.Id == id)
    .ToSql();
```

### DELETE 操作

**旧方式 (已弃用):**
```csharp
[SqlExecuteType(SqlOperation.Delete, "users")]
[Sqlx("DELETE FROM users WHERE id = @id")]
public partial Task<int> DeleteUserAsync(int id);
```

**新方式 (推荐):**
```csharp
// 方式1：通过方法名自动推断
public partial Task<int> DeleteUserAsync(int id);

// 方式2：使用 ExpressionToSql WHERE 条件
public partial Task<int> DeleteUserAsync(
    [ExpressionToSql] Expression<Func<User, bool>> whereCondition);

// 方式3：链式 API
var deleteSql = ExpressionToSql<User>.ForSqlServer()
    .Delete()
    .Where(u => u.Id == id)
    .ToSql();
```

## 🚀 新功能优势

### 1. 更简洁的 API
```csharp
// 旧方式需要两个特性
[SqlExecuteType(SqlOperation.Insert, "users")]
[Sqlx("INSERT INTO users...")]
public partial Task<int> CreateUserAsync(...);

// 新方式只需要一个或零个特性
public partial Task<int> InsertUserAsync(User user); // 零特性
// 或
[Sqlx("INSERT INTO users")] // 部分 SQL，其余自动生成
public partial Task<int> CreateUserAsync(User user);
```

### 2. 更强的类型安全
```csharp
// 编译时验证列名和类型
public partial Task<int> UpdateUserAsync(
    [ExpressionToSql] Expression<Func<User, object>> setColumns, // 编译时验证
    [ExpressionToSql] Expression<Func<User, bool>> whereCondition);

// 使用示例
await service.UpdateUserAsync(
    u => new { u.Name, u.Email }, // 类型安全的列选择
    u => u.Id == 1);              // 类型安全的条件
```

### 3. 智能操作推断
```csharp
// 源生成器智能推断操作类型
public partial Task<int> InsertUserAsync(User user);     // INSERT
public partial Task<int> UpdateUserAsync(int id, User user); // UPDATE  
public partial Task<int> DeleteUserAsync(int id);        // DELETE
public partial Task<User?> GetUserByIdAsync(int id);     // SELECT
```

## 📋 迁移检查清单

### 步骤 1: 识别使用 SqlExecuteType 的方法
```bash
# 搜索项目中的 SqlExecuteType 使用
grep -r "SqlExecuteType" --include="*.cs" .
```

### 步骤 2: 按操作类型分组迁移
1. **INSERT 操作** → 使用 `InsertXxxAsync` 方法名或 ExpressionToSql 列选择
2. **UPDATE 操作** → 使用 `UpdateXxxAsync` 方法名或 ExpressionToSql SET/WHERE
3. **DELETE 操作** → 使用 `DeleteXxxAsync` 方法名或 ExpressionToSql WHERE
4. **SELECT 操作** → 保持原有 Sqlx 特性或使用 ExpressionToSql

### 步骤 3: 更新方法签名
```csharp
// 迁移前
[SqlExecuteType(SqlOperation.Insert, "users")]
[Sqlx("INSERT INTO users (name, email) VALUES (@name, @email)")]
public partial Task<int> CreateUserAsync(string name, string email);

// 迁移后
public partial Task<int> InsertUserAsync(User user);
```

### 步骤 4: 验证功能
- 确保所有 CRUD 操作正常工作
- 验证返回值类型正确（int 用于 INSERT/UPDATE/DELETE）
- 检查异步方法的取消令牌支持

## 🔧 高级迁移场景

### 批量操作
```csharp
// 旧方式
[SqlExecuteType(SqlOperation.BatchInsert, "users")]
public partial Task<int> BatchInsertUsersAsync(IEnumerable<User> users);

// 新方式
public partial Task<int> BatchInsertUsersAsync(IEnumerable<User> users);
// 或使用 ExpressionToSql 链式 API
var batchInsert = ExpressionToSql<User>.ForSqlServer()
    .InsertInto()
    .BatchValues(users)
    .ToSql();
```

### 复杂 WHERE 条件
```csharp
// 新方式支持复杂的类型安全条件
public partial Task<int> DeleteInactiveUsersAsync(
    [ExpressionToSql] Expression<Func<User, bool>> condition);

// 调用示例
await service.DeleteInactiveUsersAsync(
    u => !u.IsActive && u.LastLoginDate < DateTime.Now.AddYears(-1));
```

## ⚠️ 注意事项

### 1. 向后兼容性
- `SqlExecuteTypeAttribute` 仍然可用，但会产生编译警告
- 建议在主要版本升级时完成迁移
- 新项目应直接使用新 API

### 2. 性能影响
- 新 API 性能相同或更好
- ExpressionToSql 在编译时生成，无运行时开销
- 方法名推断在源生成阶段完成

### 3. 调试和错误处理
```csharp
// 新 API 提供更好的编译时错误信息
public partial Task<int> UpdateUserAsync(
    [ExpressionToSql] Expression<Func<User, object>> setValues); // 编译时验证列名
```

## 📚 相关文档

- [ExpressionToSql 完整指南](expression-to-sql.md)
- [源生成器最佳实践](ADVANCED_FEATURES_GUIDE.md)
- [API 参考文档](SQLX_COMPLETE_FEATURE_GUIDE.md)

---

**迁移支持**: 如果在迁移过程中遇到问题，请参考示例项目或在 GitHub 上创建 issue。

