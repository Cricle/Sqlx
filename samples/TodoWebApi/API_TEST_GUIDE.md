# 📋 TodoWebApi API 测试指南

## 🚀 启动服务

```bash
cd samples/TodoWebApi
dotnet run
```

服务启动后，访问 `http://localhost:5000` 即可看到前端界面。

---

## 🧪 API 端点测试

### 1. 获取 API 信息

```http
GET http://localhost:5000/api/info
```

**预期响应：**
```json
{
  "name": "Sqlx Todo WebAPI",
  "version": "3.0.0-aot",
  "environment": "Development",
  "database": "SQLite",
  "endpoints": {
    "todos": "/api/todos"
  },
  "timestamp": "2024-10-02T10:30:00Z"
}
```

---

### 2. 获取所有待办事项

```http
GET http://localhost:5000/api/todos
```

**生成的 SQL：**
```sql
SELECT id, title, description, is_completed, priority, due_date, created_at, updated_at, completed_at, tags, estimated_minutes, actual_minutes
FROM todos
ORDER BY created_at DESC
```

---

### 3. 创建新待办事项

```http
POST http://localhost:5000/api/todos
Content-Type: application/json

{
  "title": "测试任务",
  "description": "这是一个测试任务",
  "isCompleted": false,
  "priority": 3,
  "dueDate": "2024-10-09T10:00:00Z",
  "tags": "测试,重要",
  "estimatedMinutes": 60
}
```

**生成的 SQL：**
```sql
INSERT INTO todos (title, description, is_completed, priority, due_date, created_at, updated_at, completed_at, tags, estimated_minutes, actual_minutes)
VALUES (@Title, @Description, @IsCompleted, @Priority, @DueDate, @CreatedAt, @UpdatedAt, @CompletedAt, @Tags, @EstimatedMinutes, @ActualMinutes);
SELECT last_insert_rowid()
```

---

### 4. 根据 ID 获取待办事项

```http
GET http://localhost:5000/api/todos/1
```

**生成的 SQL：**
```sql
SELECT id, title, description, is_completed, priority, due_date, created_at, updated_at, completed_at, tags, estimated_minutes, actual_minutes
FROM todos
WHERE id = @id
```

---

### 5. 更新待办事项

```http
PUT http://localhost:5000/api/todos/1
Content-Type: application/json

{
  "title": "更新后的任务",
  "description": "描述已更新",
  "isCompleted": false,
  "priority": 4,
  "tags": "更新,测试",
  "estimatedMinutes": 90
}
```

**生成的 SQL：**
```sql
UPDATE todos
SET title=@Title, description=@Description, is_completed=@IsCompleted, priority=@Priority, due_date=@DueDate, updated_at=@UpdatedAt, completed_at=@CompletedAt, tags=@Tags, estimated_minutes=@EstimatedMinutes, actual_minutes=@ActualMinutes
WHERE id = @id
```

---

### 6. 删除待办事项

```http
DELETE http://localhost:5000/api/todos/1
```

**生成的 SQL：**
```sql
DELETE FROM todos
WHERE id = @id
```

---

### 7. 搜索待办事项

```http
GET http://localhost:5000/api/todos/search?q=测试
```

**生成的 SQL：**
```sql
SELECT id, title, description, is_completed, priority, due_date, created_at, updated_at, completed_at, tags, estimated_minutes, actual_minutes
FROM todos
WHERE title LIKE @query OR description LIKE @query
ORDER BY updated_at DESC
```

---

### 8. 获取已完成的待办事项

```http
GET http://localhost:5000/api/todos/completed
```

**生成的 SQL：**
```sql
SELECT id, title, description, is_completed, priority, due_date, created_at, updated_at, completed_at, tags, estimated_minutes, actual_minutes
FROM todos
WHERE is_completed = @isCompleted
ORDER BY completed_at DESC
```

---

### 9. 获取高优先级待办事项

```http
GET http://localhost:5000/api/todos/high-priority
```

**生成的 SQL：**
```sql
SELECT id, title, description, is_completed, priority, due_date, created_at, updated_at, completed_at, tags, estimated_minutes, actual_minutes
FROM todos
WHERE priority >= @minPriority AND is_completed = @isCompleted
ORDER BY priority DESC
```

---

### 10. 获取即将到期的待办事项

```http
GET http://localhost:5000/api/todos/due-soon
```

**生成的 SQL：**
```sql
SELECT id, title, description, is_completed, priority, due_date, created_at, updated_at, completed_at, tags, estimated_minutes, actual_minutes
FROM todos
WHERE due_date IS NOT NULL AND due_date <= @maxDueDate AND is_completed = @isCompleted
ORDER BY due_date
```

---

### 11. 获取待办事项总数

```http
GET http://localhost:5000/api/todos/count
```

**生成的 SQL：**
```sql
SELECT COUNT(*)
FROM todos
```

**预期响应：**
```json
{
  "count": 5
}
```

---

### 12. 批量更新优先级

```http
PUT http://localhost:5000/api/todos/batch/priority
Content-Type: application/json

{
  "ids": [1, 2, 3],
  "newPriority": 5
}
```

**生成的 SQL：**
```sql
UPDATE todos
SET priority=@Priority, updated_at=@UpdatedAt
WHERE id IN (SELECT value FROM json_each(@idsJson))
```

**预期响应：**
```json
{
  "updated": 3
}
```

---

### 13. 归档过期任务

```http
POST http://localhost:5000/api/todos/archive-expired
```

**生成的 SQL：**
```sql
UPDATE todos
SET is_completed=@IsCompleted, completed_at=@CompletedAt, updated_at=@UpdatedAt
WHERE due_date < @maxDueDate AND is_completed = @isCompleted
```

**预期响应：**
```json
{
  "archived": 2
}
```

---

## 🎯 完整测试流程

### 使用 PowerShell 测试

```powershell
# 1. 获取 API 信息
Invoke-RestMethod -Uri "http://localhost:5000/api/info" -Method GET

# 2. 创建待办事项
$todo = @{
    title = "测试任务1"
    description = "这是一个测试任务"
    isCompleted = $false
    priority = 3
    dueDate = (Get-Date).AddDays(7).ToString("o")
    tags = "测试,重要"
    estimatedMinutes = 60
} | ConvertTo-Json

$result = Invoke-RestMethod -Uri "http://localhost:5000/api/todos" -Method POST -Body $todo -ContentType "application/json"
$todoId = $result.id

# 3. 获取单个待办
Invoke-RestMethod -Uri "http://localhost:5000/api/todos/$todoId" -Method GET

# 4. 更新待办
$update = @{
    title = "更新后的任务"
    description = "描述已更新"
    isCompleted = $false
    priority = 4
    tags = "更新,测试"
    estimatedMinutes = 90
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:5000/api/todos/$todoId" -Method PUT -Body $update -ContentType "application/json"

# 5. 搜索
Invoke-RestMethod -Uri "http://localhost:5000/api/todos/search?q=测试" -Method GET

# 6. 获取统计
Invoke-RestMethod -Uri "http://localhost:5000/api/todos/count" -Method GET

# 7. 删除
Invoke-RestMethod -Uri "http://localhost:5000/api/todos/$todoId" -Method DELETE
```

---

## ✅ 测试检查清单

- [ ] **基础 CRUD**
  - [ ] ✅ 获取所有待办
  - [ ] ➕ 创建待办
  - [ ] 🔍 根据ID获取
  - [ ] ✏️ 更新待办
  - [ ] 🗑️ 删除待办

- [ ] **高级查询**
  - [ ] 🔎 关键词搜索
  - [ ] ✅ 获取已完成
  - [ ] ⚡ 获取高优先级
  - [ ] ⏰ 获取即将到期
  - [ ] 📊 获取总数

- [ ] **批量操作**
  - [ ] 🔄 批量更新优先级
  - [ ] 📦 归档过期任务

- [ ] **边界测试**
  - [ ] 搜索空字符串（应返回400错误）
  - [ ] 获取不存在的ID（应返回404错误）
  - [ ] 更新不存在的ID（应返回404错误）
  - [ ] 删除不存在的ID（应返回404错误）

---

## 🎉 所有 API 测试通过！

如果所有端点都返回正确的响应，说明：
- ✅ Sqlx 源代码生成器工作正常
- ✅ 所有占位符解析正确
- ✅ 数据库连接正常
- ✅ JSON 序列化/反序列化正常
- ✅ AOT 兼容性配置正确

---

## 📚 相关文档

- [📘 TodoWebApi README](README.md)
- [🎯 Sqlx 主文档](../../README.md)
- [💡 最佳实践](../../docs/BEST_PRACTICES.md)

