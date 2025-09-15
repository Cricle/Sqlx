# Sqlx SQLite 全功能演示

这是一个全面展示 Sqlx 与 SQLite 集成的演示项目，自动演示所有核心功能和特性。

## 🚀 快速开始

```bash
# 运行演示
dotnet run --project SqlxCompleteDemo

# 或者构建后运行
dotnet build SqlxCompleteDemo
dotnet run --project SqlxCompleteDemo --no-build
```

## 📋 演示内容

### 💾 1. CRUD 操作演示
- **CREATE**: 插入新记录的 SQL 模板生成
- **READ**: 复杂条件查询，包括排序和分页
- **UPDATE**: 字段更新和条件WHERE子句
- **DELETE**: 条件删除操作

### 🔍 2. 高级查询功能
- **复杂条件查询**: 多条件组合、逻辑运算符
- **分页查询**: LIMIT/OFFSET 实现
- **子查询模拟**: 条件组合查询
- **NULL 值处理**: 空值安全检查

### 📊 3. 聚合和分组功能
- **基础聚合**: COUNT, SUM, AVG, MAX, MIN
- **GROUP BY**: 分组查询和聚合函数
- **HAVING**: 分组后条件过滤
- **多级分组**: 按不同字段分组统计

### 🔤 4. 字符串操作
- **字符串函数**: Length, Contains, StartsWith, EndsWith
- **大小写转换**: ToLower, ToUpper
- **字符串连接**: + 操作符转换为 SQLite 语法
- **字符串处理**: Trim, Replace, Substring

### 🧮 5. 数学和日期操作
- **数学函数**: Math.Abs, 算术运算
- **日期函数**: DateTime.AddDays, AddMonths
- **数值范围**: 范围查询和比较
- **类型转换**: 安全的类型处理

### 🔗 6. 联表查询演示
- **关联条件**: 外键关系查询
- **多表条件**: 不同实体的查询组合
- **JOIN 模拟**: 通过条件模拟联表查询

### 🔧 7. 动态查询构建
- **条件组合**: 运行时动态添加WHERE条件
- **排序控制**: 动态ORDER BY
- **分页控制**: 动态LIMIT/OFFSET
- **参数化**: 安全的参数传递

### 💳 8. 事务操作
- **事务管理**: 事务开启、提交、回滚
- **批量操作**: 事务中的多个操作
- **数据验证**: 事务中的数据完整性检查
- **错误恢复**: 异常处理和回滚

### ⚡ 9. 性能特性
- **索引优化**: 主键和唯一索引查询
- **批量查询**: 大数据量处理
- **选择性字段**: 只查询需要的字段
- **计数优化**: 高效的COUNT查询

### 🛡️ 10. 错误处理和边界情况
- **空值安全**: NULL值的安全处理
- **数据验证**: 业务规则验证
- **边界值**: 极限值处理
- **异常处理**: 优雅的错误处理

## 🗃️ 数据库结构

演示使用内存 SQLite 数据库，包含以下表结构：

### Users 表
```sql
CREATE TABLE Users (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Email TEXT UNIQUE NOT NULL,
    Age INTEGER,
    Salary DECIMAL(10,2),
    DepartmentId INTEGER,
    IsActive INTEGER DEFAULT 1,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
);
```

### Departments 表
```sql
CREATE TABLE Departments (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Budget DECIMAL(12,2),
    Location TEXT
);
```

### Projects 表
```sql
CREATE TABLE Projects (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Description TEXT,
    StartDate DATETIME,
    EndDate DATETIME,
    Budget DECIMAL(12,2),
    Status TEXT DEFAULT 'Active'
);
```

### UserProjects 关联表
```sql
CREATE TABLE UserProjects (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId INTEGER,
    ProjectId INTEGER,
    Role TEXT,
    AssignedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (UserId) REFERENCES Users(Id),
    FOREIGN KEY (ProjectId) REFERENCES Projects(Id)
);
```

## 🎯 演示特点

### 自动化演示
- 无需手动输入，自动运行所有功能
- 清晰的输出格式，易于理解
- 实际的 SQL 输出，便于学习

### 完整功能覆盖
- 涵盖 Sqlx 的所有主要功能
- 从基础到高级的循序渐进
- 实际业务场景的模拟

### SQLite 优化
- 针对 SQLite 的语法优化
- 内存数据库，无需外部依赖
- 跨平台兼容性

## 🚀 Sqlx 核心优势

✅ **类型安全**: 编译时检查，运行时安全  
✅ **高性能**: 零反射，原生 SQL 执行  
✅ **智能转换**: LINQ 表达式到 SQL 的完美映射  
✅ **SQLite 优化**: 针对 SQLite 的特殊优化  
✅ **易于使用**: 简洁的 API，强大的功能  
✅ **功能全面**: 从基础 CRUD 到高级聚合分析

## 📚 相关文档

- [Sqlx 核心文档](../docs/README.md)
- [表达式转 SQL 指南](../docs/expression-to-sql.md)
- [高级功能指南](../docs/ADVANCED_FEATURES_GUIDE.md)
- [项目结构说明](../docs/PROJECT_STRUCTURE_UPDATED.md)

---

这个演示项目展示了 Sqlx 在实际项目中的强大功能和灵活性，是学习和理解 Sqlx 的最佳起点。