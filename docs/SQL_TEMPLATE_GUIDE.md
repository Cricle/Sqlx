# 🎯 Sqlx SQL模板引擎完整指南

<div align="center">

**高性能、安全且可扩展的SQL模板系统**

**条件逻辑 · 循环控制 · 内置函数 · 自定义扩展**

[![性能](https://img.shields.io/badge/性能-毫秒级响应-green)]()
[![安全](https://img.shields.io/badge/安全-参数化查询-blue)]()
[![扩展](https://img.shields.io/badge/扩展-自定义函数-orange)]()

</div>

---

## 📋 目录

- [🚀 快速开始](#-快速开始)
- [💡 核心概念](#-核心概念)
- [🔧 基础语法](#-基础语法)
- [🏗️ 高级特性](#️-高级特性)
- [🛡️ 安全特性](#️-安全特性)
- [⚡ 性能优化](#-性能优化)
- [🎨 最佳实践](#-最佳实践)

---

## 🚀 快速开始

### 基础变量替换

```csharp
// 简单变量替换
var template = "SELECT * FROM users WHERE id = {{userId}} AND name = {{userName}}";
var result = SqlTemplate.Render(template, new { 
    userId = 123, 
    userName = "张三" 
});

// 输出:
// SQL: "SELECT * FROM users WHERE id = @p0 AND name = @p1"
// 参数: { "p0": 123, "p1": "张三" }
```

### 条件逻辑

```csharp
var template = @"
    SELECT * FROM users 
    {{if includeInactive}}
        WHERE 1=1  -- 包含所有用户
    {{else}}
        WHERE is_active = 1  -- 仅活跃用户
    {{endif}}
    {{if sortByName}}
        ORDER BY name ASC
    {{endif}}";

var result = SqlTemplate.Render(template, new {
    includeInactive = false,
    sortByName = true
});
```

### 循环处理

```csharp
var template = @"
    SELECT * FROM users 
    WHERE department_id IN (
    {{each dept in departments}}
        {{dept}}{{if !@last}}, {{endif}}
    {{endeach}}
    )";

var result = SqlTemplate.Render(template, new {
    departments = new[] { 1, 2, 3, 4, 5 }
});

// 生成: WHERE department_id IN (@p0, @p1, @p2, @p3, @p4)
```

---

## 💡 核心概念

### 模板引擎架构

Sqlx 模板引擎采用**编译时优化 + 运行时缓存**的架构：

```
模板字符串 → 词法分析 → AST构建 → 编译优化 → 缓存存储 → 快速执行
```

### 关键特性

| 特性 | 描述 | 性能影响 |
|------|------|----------|
| **编译缓存** | 模板编译后缓存复用 | 🚀 10-100x 提升 |
| **参数化查询** | 自动生成安全的参数 | 🛡️ 防止SQL注入 |
| **类型安全** | 编译时类型检查 | ✅ 零运行时错误 |
| **多方言支持** | 自动适配数据库语法 | 🌐 跨数据库兼容 |

---

## 🔧 基础语法

### 1. 变量表达式

```csharp
// 基础语法: {{variableName}}
var template = "SELECT {{columns}} FROM {{tableName}} WHERE {{condition}}";

var result = SqlTemplate.Render(template, new {
    columns = "id, name, email",
    tableName = "users", 
    condition = "is_active = 1"
});
```

### 2. 条件表达式

```csharp
// if-else 语法
var template = @"
    SELECT * FROM users
    {{if hasAgeFilter}}
        WHERE age BETWEEN {{minAge}} AND {{maxAge}}
    {{else}}
        WHERE 1=1
    {{endif}}
    {{if hasNameFilter}}
        AND name LIKE {{namePattern}}
    {{endif}}";

var result = SqlTemplate.Render(template, new {
    hasAgeFilter = true,
    minAge = 18,
    maxAge = 65,
    hasNameFilter = false,
    namePattern = "%张%"
});
```

### 3. 循环表达式

```csharp
// each-in 语法
var template = @"
    INSERT INTO users (name, email, age) VALUES
    {{each user in users}}
        ({{user.Name}}, {{user.Email}}, {{user.Age}})
        {{if !@last}}, {{endif}}
    {{endeach}}";

var users = new[] {
    new { Name = "张三", Email = "zhang@test.com", Age = 25 },
    new { Name = "李四", Email = "li@test.com", Age = 30 },
    new { Name = "王五", Email = "wang@test.com", Age = 28 }
};

var result = SqlTemplate.Render(template, new { users });
```

### 4. 内置函数

```csharp
var template = @"
    SELECT 
        {{upper(firstName)}} as FirstName,
        {{lower(lastName)}} as LastName,
        {{len(description)}} as DescLength,
        {{table(tableName)}} as QuotedTable,
        {{column(columnName)}} as QuotedColumn
    FROM {{table(tableName)}}
    WHERE {{column(statusColumn)}} = {{status}}";

var result = SqlTemplate.Render(template, new {
    firstName = "john",
    lastName = "DOE", 
    description = "用户描述信息",
    tableName = "user_profiles",
    columnName = "first_name",
    statusColumn = "status",
    status = "active"
});

// 输出 (SQL Server 方言):
// SELECT 
//     JOHN as FirstName,
//     doe as LastName, 
//     6 as DescLength,
//     [user_profiles] as QuotedTable,
//     [first_name] as QuotedColumn
// FROM [user_profiles]
// WHERE [status] = @p0
```

---

## 🏗️ 高级特性

### 1. 模板编译与重用

```csharp
// 编译一次，重复使用 - 极致性能
var compiled = SqlTemplate.Compile(@"
    SELECT {{columns}} FROM {{table(tableName)}}
    {{if hasConditions}}
        WHERE 1=1
        {{each condition in conditions}}
            AND {{condition.Field}} {{condition.Operator}} {{condition.Value}}
        {{endeach}}
    {{endif}}
    {{if hasOrderBy}}
        ORDER BY {{orderBy}}
    {{endif}}
    {{if hasPaging}}
        LIMIT {{pageSize}} OFFSET {{offset}}
    {{endif}}");

// 高性能执行 - 毫秒级响应
var result1 = compiled.Execute(new {
    columns = "id, name, email",
    tableName = "users",
    hasConditions = true,
    conditions = new[] {
        new { Field = "age", Operator = ">", Value = 18 },
        new { Field = "status", Operator = "=", Value = "active" }
    },
    hasOrderBy = true,
    orderBy = "created_at DESC",
    hasPaging = true,
    pageSize = 20,
    offset = 0
});

var result2 = compiled.Execute(new {
    columns = "id, name",
    tableName = "products", 
    hasConditions = false,
    hasOrderBy = false,
    hasPaging = false
});
```

### 2. 嵌套条件与复杂逻辑

```csharp
var template = @"
    SELECT * FROM orders o
    {{if includeJoins}}
        {{if joinUsers}}
            INNER JOIN users u ON o.user_id = u.id
        {{endif}}
        {{if joinProducts}}
            INNER JOIN products p ON o.product_id = p.id
        {{endif}}
    {{endif}}
    WHERE 1=1
    {{if hasDateRange}}
        AND o.created_at BETWEEN {{startDate}} AND {{endDate}}
    {{endif}}
    {{if hasStatusFilter}}
        AND o.status IN (
        {{each status in statusList}}
            {{status}}{{if !@last}}, {{endif}}
        {{endeach}}
        )
    {{endif}}
    {{if hasUserFilter}}
        {{if userIds}}
            AND o.user_id IN (
            {{each userId in userIds}}
                {{userId}}{{if !@last}}, {{endif}}
            {{endeach}}
            )
        {{else}}
            AND o.user_id = {{singleUserId}}
        {{endif}}
    {{endif}}";

var result = SqlTemplate.Render(template, new {
    includeJoins = true,
    joinUsers = true,
    joinProducts = false,
    hasDateRange = true,
    startDate = DateTime.Today.AddDays(-30),
    endDate = DateTime.Today,
    hasStatusFilter = true,
    statusList = new[] { "pending", "processing", "completed" },
    hasUserFilter = true,
    userIds = new[] { 1, 2, 3, 4, 5 },
    singleUserId = (int?)null
});
```

### 3. 自定义函数扩展

```csharp
// 定义自定义函数
var options = new SqlTemplateOptions {
    CustomFunctions = new Dictionary<string, Func<object?[], object?>> {
        ["formatDate"] = args => {
            if (args[0] is DateTime date) {
                var format = args.Length > 1 ? args[1]?.ToString() : "yyyy-MM-dd";
                return date.ToString(format);
            }
            return "NULL";
        },
        ["coalesce"] = args => {
            foreach (var arg in args) {
                if (arg != null && !string.IsNullOrEmpty(arg.ToString()))
                    return arg;
            }
            return "NULL";
        },
        ["pluralize"] = args => {
            if (args.Length >= 2) {
                var count = Convert.ToInt32(args[0]);
                var word = args[1]?.ToString() ?? "";
                var plural = args.Length > 2 ? args[2]?.ToString() : word + "s";
                return count == 1 ? word : plural;
            }
            return "";
        }
    }
};

var template = @"
    SELECT 
        COUNT(*) as Total,
        '{{pluralize(count, ""record"", ""records"")}}' as Description,
        '{{formatDate(startDate, ""yyyy-MM-dd HH:mm:ss"")}}' as FormattedDate,
        {{coalesce(nickname, firstName, ""Unknown"")}} as DisplayName
    FROM users 
    WHERE created_at >= '{{formatDate(startDate)}}'";

var result = SqlTemplate.Render(template, new {
    count = 5,
    startDate = DateTime.Today,
    nickname = (string?)null,
    firstName = "张三"
}, options);
```

---

## 🛡️ 安全特性

### 1. 自动参数化查询

```csharp
// ✅ 安全 - 自动参数化
var template = "SELECT * FROM users WHERE name = {{userName}} AND age > {{minAge}}";
var result = SqlTemplate.Render(template, new { 
    userName = "'; DROP TABLE users; --",  // SQL注入尝试
    minAge = 18 
});

// 输出: SELECT * FROM users WHERE name = @p0 AND age > @p1
// 参数: { "p0": "'; DROP TABLE users; --", "p1": 18 }
// 🛡️ SQL注入被自动阻止
```

### 2. 安全模式配置

```csharp
var options = new SqlTemplateOptions {
    SafeMode = true,                    // 启用严格安全检查
    UseParameterizedQueries = true,     // 强制参数化查询
    ValidateTemplates = true            // 模板语法验证
};

var template = "SELECT * FROM {{tableName}} WHERE id = {{userId}}";
var result = SqlTemplate.Render(template, new { 
    tableName = "users",  // 表名会被验证
    userId = 123 
}, options);
```

### 3. 字符串转义处理

```csharp
// 特殊字符自动转义
var template = "SELECT * FROM users WHERE description = {{desc}}";
var result = SqlTemplate.Render(template, new { 
    desc = "用户's \"特殊\" 描述\n内容" 
});

// 自动转义单引号、双引号、换行符等特殊字符
```

---

## ⚡ 性能优化

### 1. 编译缓存策略

```csharp
// 全局缓存配置
var options = new SqlTemplateOptions {
    EnableCaching = true,           // 启用模板编译缓存
    CacheSize = 1000,              // 缓存大小限制
    CacheEvictionPolicy = LRU       // 缓存淘汰策略
};

// 首次编译 - 较慢（毫秒级）
var template = "SELECT * FROM {{table}} WHERE {{condition}}";
var result1 = SqlTemplate.Render(template, data1, options);

// 后续使用 - 极快（微秒级）
var result2 = SqlTemplate.Render(template, data2, options);
var result3 = SqlTemplate.Render(template, data3, options);
```

### 2. 批量操作优化

```csharp
// 批量插入优化
var template = @"
    INSERT INTO users (name, email, age) VALUES
    {{each user in users}}
        ({{user.Name}}, {{user.Email}}, {{user.Age}})
        {{if !@last}}, {{endif}}
    {{endeach}}";

// 支持大批量数据 - 自动分批处理
var largeUserList = GenerateUsers(10000);  // 1万条记录
var result = SqlTemplate.Render(template, new { 
    users = largeUserList 
});

// 自动生成高效的批量插入SQL
```

### 3. 内存优化

```csharp
// 使用编译模板减少内存分配
using var compiled = SqlTemplate.Compile(template);

// 重复执行不会产生额外内存分配
for (int i = 0; i < 10000; i++) {
    var result = compiled.Execute(new { id = i });
    // 处理结果...
}
// 编译模板自动释放资源
```

---

## 🎨 最佳实践

### 1. 模板组织结构

```csharp
// 📁 Templates/
// ├── UserQueries.cs
// ├── OrderQueries.cs  
// └── ReportQueries.cs

public static class UserQueries
{
    public const string GetActiveUsers = @"
        SELECT {{columns}} FROM users 
        {{if hasAgeFilter}}
            WHERE age BETWEEN {{minAge}} AND {{maxAge}}
        {{endif}}
        {{if sortByName}}
            ORDER BY name ASC
        {{endif}}";
    
    public const string BulkInsert = @"
        INSERT INTO users ({{join(',', columns)}}) VALUES
        {{each user in users}}
            ({{join(',', user.Values)}}){{if !@last}}, {{endif}}
        {{endeach}}";
}

// 使用
var result = SqlTemplate.Render(UserQueries.GetActiveUsers, parameters);
```

### 2. 参数验证与错误处理

```csharp
public static class SafeTemplateRenderer 
{
    public static SqlTemplate RenderUserQuery(object parameters)
    {
        try {
            // 参数验证
            ValidateParameters(parameters);
            
            // 安全渲染
            var options = new SqlTemplateOptions { 
                SafeMode = true,
                ValidateTemplates = true 
            };
            
            return SqlTemplate.Render(UserQueries.GetActiveUsers, parameters, options);
        }
        catch (SqlTemplateException ex) {
            // 记录错误日志
            _logger.LogError(ex, "模板渲染失败: {Template}", UserQueries.GetActiveUsers);
            throw;
        }
    }
    
    private static void ValidateParameters(object parameters)
    {
        // 实现参数验证逻辑
        if (parameters == null)
            throw new ArgumentNullException(nameof(parameters));
            
        // 其他验证...
    }
}
```

### 3. 多数据库兼容

```csharp
public class DatabaseAwareTemplateService
{
    private readonly SqlDialectType _dialectType;
    
    public DatabaseAwareTemplateService(SqlDialectType dialectType)
    {
        _dialectType = dialectType;
    }
    
    public SqlTemplate RenderPaginatedQuery<T>(
        string baseTemplate, 
        T parameters, 
        int pageSize, 
        int pageNumber)
    {
        var paginationTemplate = _dialectType switch {
            SqlDialectType.SqlServer => baseTemplate + " OFFSET {{offset}} ROWS FETCH NEXT {{pageSize}} ROWS ONLY",
            SqlDialectType.MySQL => baseTemplate + " LIMIT {{pageSize}} OFFSET {{offset}}",
            SqlDialectType.PostgreSql => baseTemplate + " LIMIT {{pageSize}} OFFSET {{offset}}",
            SqlDialectType.SQLite => baseTemplate + " LIMIT {{pageSize}} OFFSET {{offset}}",
            _ => throw new NotSupportedException($"不支持的数据库方言: {_dialectType}")
        };
        
        var options = new SqlTemplateOptions { 
            Dialect = _dialectType 
        };
        
        return SqlTemplate.Render(paginationTemplate, new {
            parameters,
            pageSize,
            offset = pageSize * (pageNumber - 1)
        }, options);
    }
}
```

### 4. 性能监控与调优

```csharp
public class PerformanceAwareTemplateService
{
    private readonly IMetrics _metrics;
    private readonly ILogger _logger;
    
    public SqlTemplate RenderWithMetrics(string template, object parameters)
    {
        using var timer = _metrics.StartTimer("sqltemplate.render");
        
        try {
            var result = SqlTemplate.Render(template, parameters);
            
            // 记录性能指标
            _metrics.Counter("sqltemplate.success").Increment();
            _metrics.Histogram("sqltemplate.sql_length").Record(result.Sql.Length);
            _metrics.Histogram("sqltemplate.parameter_count").Record(result.Parameters.Count);
            
            return result;
        }
        catch (Exception ex) {
            _metrics.Counter("sqltemplate.error").Increment();
            _logger.LogError(ex, "模板渲染失败");
            throw;
        }
    }
}
```

---

## 🔍 故障排除

### 常见问题与解决方案

| 问题 | 原因 | 解决方案 |
|------|------|----------|
| 模板编译失败 | 语法错误 | 检查 `{{}}` 配对和语法正确性 |
| 参数不匹配 | 参数名错误 | 确保参数名与模板中的变量名一致 |
| 性能问题 | 未使用编译缓存 | 启用 `EnableCaching = true` |
| SQL注入风险 | 直接字符串拼接 | 使用参数化查询模式 |

### 调试技巧

```csharp
// 启用详细日志
var options = new SqlTemplateOptions {
    EnableDebugLogging = true,
    LogLevel = LogLevel.Debug
};

var result = SqlTemplate.Render(template, parameters, options);

// 检查生成的SQL和参数
Console.WriteLine($"生成的SQL: {result.Sql}");
Console.WriteLine($"参数: {string.Join(", ", result.Parameters.Select(p => $"{p.Key}={p.Value}"))}");
```

---

<div align="center">

**🎯 掌握 Sqlx 模板引擎，释放SQL的无限可能！**

**[⬆️ 返回顶部](#-sqlx-sql模板引擎完整指南) · [🏠 回到首页](../README.md) · [📚 文档中心](README.md)**

</div>
