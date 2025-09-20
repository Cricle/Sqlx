# Sqlx SQL 模板引擎完整指南

## 概述

Sqlx SQL 模板引擎是一个功能强大的编译时SQL生成系统，提供了丰富的模板语法，支持条件逻辑、循环控制、内置函数等高级特性。与传统的字符串拼接不同，Sqlx模板引擎在编译时处理模板，生成高性能的SQL执行代码。

## 🚀 核心特性

### ✨ 编译时处理
- **零运行时开销**：所有模板处理在编译时完成
- **类型安全**：编译时验证SQL模板和参数
- **高性能**：生成的代码直接执行，无模板解析开销

### 🎯 丰富的模板语法
- **基础占位符**：`{{table}}`、`{{columns}}`、`{{where}}`等
- **条件逻辑**：`{{if condition}}...{{endif}}`
- **循环控制**：`{{each item in collection}}...{{endeach}}`
- **内置函数**：`{{upper(text)}}`、`{{join(',', array)}}`等
- **模板继承**：`{{extends "base_template"}}`
- **模板包含**：`{{include "partial"}}`

### 🔧 开发体验
- **语法高亮**：Visual Studio 扩展提供完整语法高亮
- **智能提示**：IntelliSense 支持模板语法
- **错误检测**：编译时模板验证和错误报告
- **调试信息**：生成的代码包含详细的模板处理信息

## 📖 基础语法

### 1. 基础占位符

#### 表名占位符
```sql
-- 基础表名
SELECT * FROM {{table}}

-- 带引号的表名
SELECT * FROM {{table:quoted}}  -- 生成: [table_name]

-- 带架构的表名
SELECT * FROM {{table:schema}}  -- 生成: dbo.table_name

-- 带别名的表名
SELECT * FROM {{table:alias}}   -- 生成: table_name t
```

#### 列名占位符
```sql
-- 自动推断列名
SELECT {{columns:auto}} FROM {{table}}

-- 带引号的列名
SELECT {{columns:quoted}} FROM {{table}}

-- 带前缀的列名
SELECT {{columns:prefixed}} FROM {{table}}  -- 生成: t.column1, t.column2

-- 带别名的列名
SELECT {{columns:aliased}} FROM {{table}}   -- 生成: column1 AS Column1
```

#### 条件占位符
```sql
-- ID 条件
SELECT * FROM {{table}} WHERE {{where:id}}  -- 生成: WHERE id = @id

-- 自动条件
SELECT * FROM {{table}} WHERE {{where:auto}}  -- 根据方法参数生成

-- 安全条件
SELECT * FROM {{table}} WHERE {{where:safe}}  -- 生成: WHERE 1=1
```

### 2. 条件逻辑

#### 基础条件
```sql
SELECT * FROM users WHERE 1=1
{{if hasName}}
    AND name = @name
{{endif}}
{{if hasAge}}
    AND age > @age
{{endif}}
```

#### If-Else 条件
```sql
SELECT {{columns}} FROM {{table}}
{{if isActive}}
    WHERE is_active = 1
{{else}}
    WHERE is_active = 0
{{endif}}
```

### 3. 循环控制

#### 遍历集合
```sql
SELECT * FROM users WHERE department_id IN (
{{each dept in departments}}
    {{dept}}{{if !@last}}, {{endif}}
{{endeach}}
)
```

#### 循环变量
- `{{@index}}`：当前索引（从0开始）
- `{{@first}}`：是否为第一个元素
- `{{@last}}`：是否为最后一个元素

### 4. 内置函数

#### 字符串函数
```sql
-- 大小写转换
SELECT {{upper(name)}} FROM users
SELECT {{lower(email)}} FROM users

-- 字符串处理
SELECT {{trim(description)}} FROM products
SELECT {{concat(first_name, ' ', last_name)}} AS full_name FROM users

-- 引用处理
INSERT INTO {{table}} (name) VALUES ({{quote(userName)}})
UPDATE {{table}} SET {{brackets(columnName)}} = @value
```

#### 聚合函数
```sql
-- 计数和统计
SELECT {{count(id)}} FROM {{table}}
SELECT {{sum(amount)}}, {{avg(rating)}} FROM {{table}}
SELECT {{min(created_at)}}, {{max(updated_at)}} FROM {{table}}
```

#### 日期时间函数
```sql
-- 当前时间
INSERT INTO {{table}} (created_at) VALUES ({{now()}})
SELECT * FROM {{table}} WHERE DATE(created_at) = {{today()}}

-- GUID 生成
INSERT INTO {{table}} (id) VALUES ({{guid()}})
```

#### 实用函数
```sql
-- 数组连接
SELECT name FROM users WHERE id IN ({{join(',', userIds)}})

-- 空值处理
SELECT {{coalesce(nickname, first_name, 'Unknown')}} FROM users
SELECT {{isnull(description, 'No description')}} FROM products
```

## 🏗️ 高级特性

### 1. 模板继承

#### 基础模板 (crud_base.sql)
```sql
{{if operation == 'select'}}
    SELECT {{columns}} FROM {{table}}
    {{if hasWhere}}WHERE {{where}}{{endif}}
    {{if hasOrderBy}}{{orderby}}{{endif}}
{{endif}}

{{if operation == 'insert'}}
    INSERT INTO {{table}} ({{columns}}) VALUES ({{values}})
{{endif}}

{{if operation == 'update'}}
    UPDATE {{table}} SET {{set}}
    {{if hasWhere}}WHERE {{where}}{{endif}}
{{endif}}
```

#### 继承使用
```sql
{{extends "crud_base"}}
{{var operation = "select"}}
{{var hasWhere = true}}
{{var hasOrderBy = true}}
```

### 2. 模板包含

#### 部分模板 (audit_fields.sql)
```sql
created_at, created_by, updated_at, updated_by
```

#### 使用包含
```sql
SELECT id, name, email, {{include "audit_fields"}}
FROM {{table}}
```

### 3. 模板变量

```sql
{{var tableName = "users"}}
{{var includeAudit = true}}

SELECT * FROM {{tableName}}
{{if includeAudit}}
WHERE deleted_at IS NULL
{{endif}}
```

### 4. 自定义函数

在 C# 代码中注册自定义函数：

```csharp
var processor = new UnifiedTemplateProcessor();
processor.RegisterCustomFunction("formatPhone", args => 
    $"FORMAT({args[0]}, '(###) ###-####')", 
    "Format phone number");
```

在模板中使用：
```sql
SELECT name, {{formatPhone(phone)}} AS formatted_phone FROM users
```

## 🎨 使用示例

### 1. 简单查询
```csharp
[Sqlx("SELECT {{columns:auto}} FROM {{table}} WHERE {{where:id}}")]
Task<User?> GetUserByIdAsync(int id);
```

生成的SQL：
```sql
SELECT id, name, email, age, created_at FROM users WHERE id = @id
```

### 2. 动态条件查询
```csharp
[SqlTemplate(@"
    SELECT {{columns}} FROM {{table}} WHERE 1=1
    {{if hasName}}AND name LIKE @namePattern{{endif}}
    {{if hasAge}}AND age BETWEEN @minAge AND @maxAge{{endif}}
    {{if hasDepartments}}
        AND department_id IN (
        {{each dept in departments}}
            @dept{{@index}}{{if !@last}}, {{endif}}
        {{endeach}}
        )
    {{endif}}
    {{orderby:created}}
")]
Task<List<User>> SearchUsersAsync(string? namePattern = null, int? minAge = null, int? maxAge = null, int[]? departments = null);
```

### 3. 复杂插入操作
```csharp
[SqlTemplate(@"
    INSERT INTO {{table}} (
        {{columns:exclude=id,created_at,updated_at}}
    ) VALUES (
        {{values:exclude=id,created_at,updated_at}}
    )
")]
Task<int> CreateUserAsync(User user);
```

### 4. 批量操作
```csharp
[SqlTemplate(@"
    INSERT INTO {{table}} (name, email, department_id)
    VALUES 
    {{each user in users}}
        ({{quote(user.Name)}}, {{quote(user.Email)}}, {{user.DepartmentId}}){{if !@last}},{{endif}}
    {{endeach}}
")]
Task<int> CreateUsersAsync(List<User> users);
```

### 5. 继承模板
```csharp
[SqlTemplate(@"
    {{extends 'paged_query'}}
    {{var hasWhere = true}}
    {{var hasOrderBy = true}}
")]
Task<List<User>> GetPagedUsersAsync(int offset, int limit, string? filter = null);
```

## 🔧 配置选项

### SqlTemplateAttribute 配置
```csharp
[SqlTemplate(
    template: "SELECT * FROM users WHERE id = @id",
    Dialect = SqlDialectType.PostgreSql,
    SafeMode = true,
    ValidateParameters = true,
    EnableCaching = true
)]
```

### 全局配置
```csharp
var options = SqlTemplateOptions.ForPostgreSQL();
options.UseCache = true;
options.ValidateParameters = true;
options.CustomFunctions["myFunc"] = args => "CUSTOM_FUNCTION()";
```

## 🎯 最佳实践

### 1. 模板组织
- 将复杂模板拆分为多个部分
- 使用模板继承减少重复代码
- 创建可重用的部分模板

### 2. 性能优化
- 使用编译时模板处理
- 避免在模板中进行复杂计算
- 合理使用缓存

### 3. 安全性
- 启用 SafeMode 进行 SQL 注入检查
- 使用参数化查询
- 验证模板输入

### 4. 调试
- 查看生成的代码注释中的 SQL
- 使用模板验证功能
- 启用详细的错误报告

## 📚 API 参考

### 核心类

#### AdvancedSqlTemplateEngine
```csharp
public class AdvancedSqlTemplateEngine : ISqlTemplateEngine
{
    public SqlTemplateResult ProcessTemplate(string templateSql, IMethodSymbol method, INamedTypeSymbol? entityType, string tableName);
    public TemplateValidationResult ValidateTemplate(string templateSql);
}
```

#### UnifiedTemplateProcessor
```csharp
public class UnifiedTemplateProcessor
{
    public TemplateProcessingResult ProcessTemplate(TemplateProcessingRequest request);
    public void RegisterCustomFunction(string name, Func<string[], string> function, string description = "");
    public TemplateDocumentation GetTemplateDocumentation();
}
```

### 属性

#### SqlTemplateAttribute
```csharp
[SqlTemplate(
    string template,
    SqlDialectType Dialect = SqlDialectType.SqlServer,
    bool SafeMode = true,
    bool ValidateParameters = true,
    bool EnableCaching = true
)]
```

## 🔍 故障排除

### 常见错误

1. **模板语法错误**
   ```
   错误: Invalid placeholder: {{unknownPlaceholder}}
   解决: 检查占位符拼写，参考可用占位符列表
   ```

2. **参数不匹配**
   ```
   错误: Parameter 'userName' not found in method signature
   解决: 确保模板中的参数与方法参数匹配
   ```

3. **条件语法错误**
   ```
   错误: Mismatched if/endif statements
   解决: 检查 if/endif 配对，确保语法正确
   ```

### 调试技巧

1. 查看生成的代码注释
2. 使用模板验证 API
3. 启用详细错误报告
4. 检查编译器输出

## 📈 性能考虑

### 编译时优化
- 模板在编译时处理，无运行时开销
- 生成的 SQL 经过优化
- 参数绑定在编译时确定

### 内存效率
- 模板缓存机制
- 避免重复的模板处理
- 优化的字符串操作

### 执行效率
- 直接的 SQL 执行
- 无反射或动态代码生成
- AOT 友好设计

## 🔮 未来计划

- **更多数据库支持**：Oracle、DB2 等
- **可视化模板编辑器**：图形化模板设计
- **模板调试器**：步进式模板调试
- **性能分析工具**：模板性能监控
- **更多内置函数**：扩展函数库

## 📞 支持与反馈

如果您在使用过程中遇到问题或有改进建议，请：

1. 查看本文档的故障排除部分
2. 检查项目的 Issues 页面
3. 提交新的 Issue 或 Pull Request
4. 参与社区讨论

---

*Sqlx SQL 模板引擎 - 让 SQL 编写变得更加优雅和高效！*

