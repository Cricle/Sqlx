# 动态占位符使用示例

本示例演示如何在 TodoWebApi 中安全使用动态占位符功能，适用于多租户系统和分库分表场景。

---

## 🏢 示例 1：多租户表名（推荐场景）

### 场景说明
每个租户有独立的 Todo 表：`tenant1_todos`、`tenant2_todos`、`tenant3_todos`

### 实现步骤

#### 1. 定义多租户 Repository 接口

```csharp
using Sqlx;

namespace TodoWebApi.Repositories;

public interface ITenantTodoRepository
{
    // ✅ 使用 [DynamicSql] 标记动态表名参数
    [Sqlx("SELECT id, title, is_completed, created_at FROM {{@tableName}} WHERE id = @id")]
    Task<Todo?> GetByIdAsync([DynamicSql] string tableName, long id);

    [Sqlx("SELECT id, title, is_completed, created_at FROM {{@tableName}} ORDER BY created_at DESC")]
    Task<List<Todo>> GetAllAsync([DynamicSql] string tableName);

    [Sqlx("INSERT INTO {{@tableName}} (title, is_completed, created_at) VALUES (@Title, @IsCompleted, @CreatedAt) RETURNING id")]
    Task<long> CreateAsync([DynamicSql] string tableName, Todo todo);

    [Sqlx("UPDATE {{@tableName}} SET title = @Title, is_completed = @IsCompleted WHERE id = @Id")]
    Task<int> UpdateAsync([DynamicSql] string tableName, Todo todo);

    [Sqlx("DELETE FROM {{@tableName}} WHERE id = @id")]
    Task<int> DeleteAsync([DynamicSql] string tableName, long id);
}
```

#### 2. 实现类（自动生成）

```csharp
using Sqlx;
using System.Data;

namespace TodoWebApi.Repositories;

[RepositoryFor(typeof(ITenantTodoRepository))]
[SqlDefine(
    Dialect = SqlDialect.Sqlite,
    TableName = "todos"  // 默认表名（实际会被动态参数覆盖）
)]
public partial class TenantTodoRepository(IDbConnection connection) : ITenantTodoRepository;
```

#### 3. 租户服务（带白名单验证）⭐

```csharp
using System.Collections.Generic;

namespace TodoWebApi.Services;

public class TenantTodoService
{
    private readonly ITenantTodoRepository _repository;

    // ✅ 白名单：只允许这些租户
    private static readonly HashSet<string> AllowedTenants = new(StringComparer.OrdinalIgnoreCase)
    {
        "tenant1", "tenant2", "tenant3", "demo", "test"
    };

    public TenantTodoService(ITenantTodoRepository repository)
    {
        _repository = repository;
    }

    // ✅ 安全方法：内部验证
    private string GetTableName(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));

        if (!AllowedTenants.Contains(tenantId))
            throw new ArgumentException($"Invalid tenant: {tenantId}", nameof(tenantId));

        return $"{tenantId}_todos";
    }

    public async Task<Todo?> GetTodoAsync(string tenantId, long id)
    {
        var tableName = GetTableName(tenantId);  // 白名单验证
        return await _repository.GetByIdAsync(tableName, id);
    }

    public async Task<List<Todo>> GetAllTodosAsync(string tenantId)
    {
        var tableName = GetTableName(tenantId);
        return await _repository.GetAllAsync(tableName);
    }

    public async Task<long> CreateTodoAsync(string tenantId, Todo todo)
    {
        var tableName = GetTableName(tenantId);
        return await _repository.CreateAsync(tableName, todo);
    }

    public async Task<int> UpdateTodoAsync(string tenantId, Todo todo)
    {
        var tableName = GetTableName(tenantId);
        return await _repository.UpdateAsync(tableName, todo);
    }

    public async Task<int> DeleteTodoAsync(string tenantId, long id)
    {
        var tableName = GetTableName(tenantId);
        return await _repository.DeleteAsync(tableName, id);
    }
}
```

#### 4. Controller 使用（不直接暴露动态参数）

```csharp
using Microsoft.AspNetCore.Mvc;

namespace TodoWebApi.Controllers;

[ApiController]
[Route("api/tenants")]
public class TenantTodoController : ControllerBase
{
    private readonly TenantTodoService _service;

    public TenantTodoController(TenantTodoService service)
    {
        _service = service;
    }

    // ✅ 正确：不直接暴露 tableName，通过 tenantId 映射
    [HttpGet("{tenantId}/todos")]
    public async Task<ActionResult<List<Todo>>> GetTodos(string tenantId)
    {
        try
        {
            var todos = await _service.GetAllTodosAsync(tenantId);
            return Ok(todos);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{tenantId}/todos/{id}")]
    public async Task<ActionResult<Todo>> GetTodo(string tenantId, long id)
    {
        try
        {
            var todo = await _service.GetTodoAsync(tenantId, id);
            return todo == null ? NotFound() : Ok(todo);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{tenantId}/todos")]
    public async Task<ActionResult<Todo>> CreateTodo(string tenantId, [FromBody] Todo todo)
    {
        try
        {
            var id = await _service.CreateTodoAsync(tenantId, todo);
            todo.Id = id;
            return CreatedAtAction(nameof(GetTodo), new { tenantId, id }, todo);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
```

---

## 📅 示例 2：分表后缀（按月分表）

### 场景说明
日志表按月分表：`logs_202410`、`logs_202411`、`logs_202412`

### 实现代码

```csharp
using Sqlx;

namespace TodoWebApi.Repositories;

public interface ILogRepository
{
    // ✅ 使用 TablePart 类型（只允许字母和数字）
    [Sqlx("SELECT id, level, message, created_at FROM logs_{{@monthSuffix}} WHERE created_at >= @startDate")]
    Task<List<Log>> GetLogsAsync(
        [DynamicSql(Type = DynamicSqlType.TablePart)] string monthSuffix,
        DateTime startDate);
}

[RepositoryFor(typeof(ILogRepository))]
[SqlDefine(Dialect = SqlDialect.Sqlite)]
public partial class LogRepository(IDbConnection connection) : ILogRepository;

// 使用
public class LogService
{
    private readonly ILogRepository _repository;

    public async Task<List<Log>> GetCurrentMonthLogsAsync()
    {
        // ✅ 生成月份后缀（格式：202410）
        var monthSuffix = DateTime.Now.ToString("yyyyMM");

        // ✅ 只允许字母和数字，符合 TablePart 验证规则
        var logs = await _repository.GetLogsAsync(monthSuffix, DateTime.Today);
        return logs;
    }
}
```

---

## 🔍 示例 3：动态 WHERE 子句（高级）

### 场景说明
根据用户输入构建动态查询条件

### 实现代码

```csharp
using Sqlx;

namespace TodoWebApi.Repositories;

public interface IAdvancedTodoRepository
{
    // ✅ 使用 Fragment 类型（SQL 片段）
    [Sqlx("SELECT {{columns}} FROM {{table}} WHERE {{@whereClause}}")]
    Task<List<Todo>> QueryAsync(
        [DynamicSql(Type = DynamicSqlType.Fragment)] string whereClause);
}

[RepositoryFor(typeof(IAdvancedTodoRepository))]
[SqlDefine(Dialect = SqlDialect.Sqlite, TableName = "todos")]
public partial class AdvancedTodoRepository(IDbConnection connection) : IAdvancedTodoRepository;

// 使用
public class AdvancedTodoService
{
    private readonly IAdvancedTodoRepository _repository;

    public async Task<List<Todo>> GetActiveTodosAsync()
    {
        // ✅ 硬编码的安全 WHERE 子句
        var whereClause = "is_completed = 0 AND deleted_at IS NULL";
        return await _repository.QueryAsync(whereClause);
    }

    public async Task<List<Todo>> GetRecentTodosAsync(int days)
    {
        // ✅ 参数化的日期值，WHERE 子句本身是硬编码的
        var dateThreshold = DateTime.Now.AddDays(-days).ToString("yyyy-MM-dd");
        var whereClause = $"created_at >= '{dateThreshold}'";
        return await _repository.QueryAsync(whereClause);
    }
}
```

---

## ⚠️ 安全警告和最佳实践

### ✅ 必须做

1. **显式标记特性**
   ```csharp
   // ✅ 正确
   Task GetAsync([DynamicSql] string tableName);

   // ❌ 错误：编译失败
   Task GetAsync(string tableName);  // 使用了 {{@tableName}} 但未标记特性
   ```

2. **使用白名单验证**
   ```csharp
   // ✅ 正确：白名单
   private static readonly HashSet<string> AllowedTenants = new() { "tenant1", "tenant2" };
   if (!AllowedTenants.Contains(tenantId)) throw new ArgumentException();

   // ❌ 错误：直接使用
   await repo.GetAsync(userInput);
   ```

3. **在服务层验证，不在 Controller 暴露**
   ```csharp
   // ✅ 正确：在 Service 中验证
   public class TenantService
   {
       private string GetTableName(string tenantId) { /* 白名单验证 */ }
   }

   // ❌ 错误：在 Controller 中直接暴露
   [HttpGet("api/query/{tableName}")]
   public async Task<IActionResult> Query(string tableName) { /* 不安全！ */ }
   ```

### ❌ 禁止做

1. **不要直接使用用户输入**
   ```csharp
   // ❌ 危险！
   var tableName = Request.Query["table"];
   await repo.GetAsync(tableName);  // SQL 注入风险
   ```

2. **不要在公共 API 中暴露动态参数**
   ```csharp
   // ❌ 危险！
   [HttpGet("api/data/{tableName}")]
   public async Task<IActionResult> GetData(string tableName)
   {
       return Ok(await repo.GetAsync(tableName));
   }
   ```

3. **不要跳过验证**
   ```csharp
   // ❌ 危险！
   await repo.GetAsync(tenantId + "_todos");  // 没有验证
   ```

---

## 🧪 单元测试示例

```csharp
using Xunit;

namespace TodoWebApi.Tests;

public class TenantTodoServiceTests
{
    [Fact]
    public async Task GetTodoAsync_ValidTenant_ReturnsData()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.GetAllTodosAsync("tenant1");

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetTodoAsync_InvalidTenant_ThrowsException()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.GetAllTodosAsync("evil_tenant"));
    }

    [Theory]
    [InlineData("DROP TABLE users")]
    [InlineData("tenant1'; DROP TABLE todos--")]
    [InlineData("tenant1 OR 1=1")]
    public async Task GetTodoAsync_SqlInjectionAttempt_ThrowsException(string maliciousInput)
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.GetAllTodosAsync(maliciousInput));
    }
}
```

---

## 📊 数据库准备

为多租户示例创建表：

```sql
-- 创建租户表
CREATE TABLE tenant1_todos (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    title TEXT NOT NULL,
    is_completed INTEGER NOT NULL DEFAULT 0,
    created_at TEXT NOT NULL DEFAULT (datetime('now'))
);

CREATE TABLE tenant2_todos (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    title TEXT NOT NULL,
    is_completed INTEGER NOT NULL DEFAULT 0,
    created_at TEXT NOT NULL DEFAULT (datetime('now'))
);

CREATE TABLE tenant3_todos (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    title TEXT NOT NULL,
    is_completed INTEGER NOT NULL DEFAULT 0,
    created_at TEXT NOT NULL DEFAULT (datetime('now'))
);

-- 插入测试数据
INSERT INTO tenant1_todos (title, is_completed) VALUES ('Tenant 1 - Task 1', 0);
INSERT INTO tenant1_todos (title, is_completed) VALUES ('Tenant 1 - Task 2', 1);

INSERT INTO tenant2_todos (title, is_completed) VALUES ('Tenant 2 - Task 1', 0);
INSERT INTO tenant2_todos (title, is_completed) VALUES ('Tenant 2 - Task 2', 0);
```

---

## 🎯 总结

**动态占位符是高级功能，使用时必须：**

1. ✅ 显式标记 `[DynamicSql]` 特性
2. ✅ 使用白名单严格验证
3. ✅ 在服务层封装，不在 Controller 暴露
4. ✅ 编写充分的单元测试
5. ✅ 记录安全要求

**遵循以上原则，动态占位符可以安全地解决多租户和分库分表问题。**

详细文档：[占位符完整指南](../../docs/PLACEHOLDERS.md#动态占位符-前缀---高级功能)

