# 🔍 SqlTemplate 列名匹配深度分析与改进方案

## 📊 当前实现状况分析

### 🎯 现有列名匹配功能概览

经过深入分析，当前 Sqlx 中的列名匹配功能主要分布在以下几个核心组件中：

#### 1. **基础列名映射** (`NameMapper.cs`)
```csharp
// 当前实现：PascalCase/camelCase → snake_case
MapNameToSnakeCase("UserId") → "user_id"
MapNameToSnakeCase("FirstName") → "first_name" 
MapNameToSnakeCase("IsActive") → "is_active"
```

**限制性**：
- ❌ 只支持单一的命名转换模式
- ❌ 无法自定义转换规则
- ❌ 不支持复杂的映射场景
- ❌ 缺乏模式匹配功能

#### 2. **SqlTemplate 占位符处理** (`SqlTemplatePlaceholder.cs`)
```csharp
// 当前支持的占位符：
{{columns}}          // 获取所有列
{{columns:exclude=Id,CreatedAt}}  // 排除特定列
{{columns:include=Name,Email}}    // 包含特定列
{{table}}            // 表名处理
```

**限制性**：
- ❌ 列过滤只支持简单的包含/排除
- ❌ 没有正则表达式匹配
- ❌ 缺乏通配符支持
- ❌ 无法根据列属性或类型筛选

#### 3. **实体映射生成器** (`EnhancedEntityMappingGenerator.cs`)
```csharp
// 当前实现：
GetColumnName(member) → member.Name  // 直接使用属性名
```

**限制性**：
- ❌ 没有列名转换策略
- ❌ 不支持自定义映射规则
- ❌ 缺乏属性-列名映射的智能化

### 🔧 诊断消息系统现状

#### 当前诊断能力 (`DiagnosticGuidanceService.cs`)
- ✅ SQL 质量检查（SELECT *、缺少 WHERE 等）
- ✅ 安全检查（SQL 注入风险）
- ✅ 性能建议（JOIN 优化、分页建议）
- ✅ 命名约定检查

**不足之处**：
- ❌ 缺乏列名匹配相关的诊断
- ❌ 没有映射失败的详细指导
- ❌ 缺乏智能化的修复建议

---

## 🚀 改进方案设计

### 🎯 目标：让用户更专注业务而非 SQL

### 1. **增强的列名匹配模式系统**

#### A. 多种命名转换策略
```csharp
public enum ColumnNamingStrategy 
{
    PascalCase,      // UserId
    CamelCase,       // userId  
    SnakeCase,       // user_id
    KebabCase,       // user-id
    ScreamingSnake,  // USER_ID
    Custom           // 自定义规则
}

// 配置示例
[SqlTemplate(NamingStrategy = ColumnNamingStrategy.SnakeCase)]
public partial Task<List<User>> GetUsersAsync();
```

#### B. 正则表达式匹配支持
```csharp
// 支持复杂的列匹配模式
{{columns:pattern=.*_id$}}           // 匹配所有以 _id 结尾的列
{{columns:pattern=^(name|email).*}}  // 匹配以 name 或 email 开头的列
{{columns:pattern=(?i).*status.*}}   // 大小写不敏感匹配包含 status 的列
```

#### C. 通配符匹配
```csharp
{{columns:match=user_*}}      // 匹配 user_ 开头的所有列
{{columns:match=*_id}}        // 匹配以 _id 结尾的所有列
{{columns:match=is_*|has_*}}  // 匹配多种模式
```

#### D. 属性基础筛选
```csharp
{{columns:type=string}}       // 只包含字符串类型的列
{{columns:nullable=false}}    // 只包含非空列
{{columns:key=true}}         // 只包含主键列
{{columns:foreign=true}}     // 只包含外键列
```

### 2. **智能列名推断系统**

#### A. 约定优于配置
```csharp
// 自动推断常见模式
public class User 
{
    public int Id { get; set; }           // → user_id 或 id
    public string FirstName { get; set; } // → first_name
    public DateTime CreatedAt { get; set; } // → created_at
    public bool IsActive { get; set; }    // → is_active
}

// 智能SQL生成
[Sqlx("SELECT {{columns:auto}} FROM {{table:auto}}")]
public partial Task<List<User>> GetUsersAsync();

// 自动生成：
// SELECT user_id, first_name, created_at, is_active FROM users
```

#### B. 智能表名推断
```csharp
public class UserProfile { }  // → user_profiles
public class OrderItem { }    // → order_items
public class Category { }     // → categories

[Sqlx("SELECT * FROM {{table:plural}}")]  // 自动复数化
public partial Task<List<User>> GetAllUsersAsync();
```

### 3. **高级模板功能**

#### A. 条件列包含
```csharp
var template = @"
    SELECT 
    {{each column in columns}}
        {{if column.type == 'datetime'}}
            DATE({{column.name}}) as {{column.name}}_date,
        {{endif}}
        {{column.name}}{{if !@last}},{{endif}}
    {{endeach}}
    FROM {{table}}";
```

#### B. 动态JOIN生成
```csharp
var template = @"
    SELECT {{columns:prefix=u.}} 
    FROM {{table:alias=u}}
    {{if includeProfile}}
        LEFT JOIN user_profiles p ON u.id = p.user_id
        {{columns:table=user_profiles,prefix=p.,include=avatar,bio}}
    {{endif}}
    {{if includeDepartment}}
        LEFT JOIN departments d ON u.department_id = d.id
        {{columns:table=departments,prefix=d.,include=name as dept_name}}
    {{endif}}";
```

### 4. **增强的诊断和用户指导**

#### A. 智能列名匹配诊断
```csharp
// 诊断消息示例：
[SQLX4001] 智能列名建议
  检测到属性 'FirstName' 可能对应数据库列 'first_name'
  建议: 使用 [Column("first_name")] 特性明确映射
  或配置: [SqlTemplate(NamingStrategy = ColumnNamingStrategy.SnakeCase)]

[SQLX4002] 列名匹配失败
  无法找到属性 'UserName' 对应的数据库列
  可能的匹配: user_name, username, name
  建议: 检查数据库表结构或使用 {{columns:pattern=.*name.*}} 查看可用列

[SQLX4003] 性能优化建议
  检测到查询包含大量列，建议使用 {{columns:exclude=large_text_field}}
  预估性能影响: 减少 60% 数据传输量
```

#### B. 交互式修复建议
```csharp
[SQLX4004] 自动修复建议
  检测到 SELECT * 使用，是否替换为明确列名？
  
  原始: SELECT * FROM users
  建议: SELECT {{columns:exclude=password_hash,internal_notes}} FROM users
  
  [应用修复] [忽略] [了解更多]
```

#### C. 实时预览功能
```csharp
// 开发时实时预览生成的SQL
[Sqlx("SELECT {{columns:pattern=^(id|name|email).*}} FROM {{table}}")]
public partial Task<List<User>> GetUsersAsync();

// IDE 工具提示显示：
// 预览 SQL: SELECT id, name, email FROM users  
// 匹配列数: 3
// 预估性能: 优秀 ⭐⭐⭐⭐⭐
```

### 5. **业务场景导向的模板库**

#### A. 常见业务模式模板
```csharp
// 分页查询模板
public static class BusinessTemplates
{
    public const string PaginatedQuery = @"
        SELECT {{columns:exclude=large_fields}}
        FROM {{table}}
        {{if hasFilters}}
            WHERE {{filters:auto}}
        {{endif}}
        {{if hasSort}}
            ORDER BY {{sort:safe}}
        {{endif}}
        LIMIT {{pageSize}} OFFSET {{offset}}";
    
    // 审计日志查询
    public const string AuditQuery = @"
        SELECT {{columns:include=id,action,user_id,created_at}},
               {{columns:pattern=.*_before$}} as before_values,
               {{columns:pattern=.*_after$}} as after_values
        FROM {{table:audit}}
        WHERE entity_type = {{entityType}}
        {{if hasDateRange}}
            AND created_at BETWEEN {{startDate}} AND {{endDate}}
        {{endif}}";
}
```

#### B. 自动化CRUD模板
```csharp
// 自动生成完整CRUD操作
[AutoCrud(Table = "users", NamingStrategy = ColumnNamingStrategy.SnakeCase)]
public interface IUserRepository
{
    // 自动生成：
    // SELECT user_id, first_name, last_name, email, created_at, is_active FROM users WHERE user_id = @id
    Task<User?> GetByIdAsync(int id);
    
    // 自动生成：
    // INSERT INTO users (first_name, last_name, email, is_active) 
    // VALUES (@firstName, @lastName, @email, @isActive)
    Task<int> CreateAsync(User user);
    
    // 自动生成：
    // UPDATE users SET first_name = @firstName, last_name = @lastName, 
    // email = @email, is_active = @isActive WHERE user_id = @id
    Task<int> UpdateAsync(User user);
}
```

---

## 🛠️ 实现路线图

### 阶段 1: 核心基础设施 (2-3 周)
- [ ] 设计新的列名映射API
- [ ] 实现多种命名策略支持
- [ ] 重构现有的占位符处理器
- [ ] 添加基础的正则表达式匹配

### 阶段 2: 高级匹配功能 (3-4 周)  
- [ ] 实现通配符匹配
- [ ] 添加属性基础筛选
- [ ] 增强模板编译器
- [ ] 实现智能列名推断

### 阶段 3: 诊断和用户体验 (2-3 周)
- [ ] 扩展诊断消息系统
- [ ] 实现智能修复建议
- [ ] 添加实时预览功能
- [ ] 创建交互式指导

### 阶段 4: 业务场景优化 (3-4 周)
- [ ] 创建业务模式模板库
- [ ] 实现自动CRUD生成
- [ ] 性能优化和缓存
- [ ] 完整的测试覆盖

---

## 🎯 预期收益

### 🚀 开发效率提升
- **减少 80% 的 SQL 手写代码**：通过智能模板和自动生成
- **减少 90% 的列名映射错误**：通过智能匹配和实时诊断
- **提升 5x 开发速度**：专注业务逻辑而非 SQL 细节

### 🛡️ 代码质量改善
- **零SQL注入风险**：强制参数化查询
- **类型安全保证**：编译时验证
- **一致的命名约定**：自动化命名策略

### 📈 维护性提升
- **自动重构支持**：修改实体时自动更新SQL
- **智能错误检测**：提前发现潜在问题
- **可视化调试**：清晰的SQL生成过程

---

## 💡 使用场景示例

### 场景1：电商系统用户查询
```csharp
// 传统方式 - 需要手写大量SQL
[Sqlx(@"
    SELECT u.user_id, u.first_name, u.last_name, u.email, u.created_at,
           p.avatar_url, p.bio, p.phone_number,
           d.department_name, d.manager_id
    FROM users u
    LEFT JOIN user_profiles p ON u.user_id = p.user_id  
    LEFT JOIN departments d ON u.department_id = d.department_id
    WHERE u.is_active = 1 
    AND u.created_at >= @startDate
    ORDER BY u.created_at DESC
    LIMIT @pageSize OFFSET @offset")]
public partial Task<List<UserDto>> GetActiveUsersAsync(DateTime startDate, int pageSize, int offset);

// 新方式 - 智能化模板
[Sqlx(@"
    SELECT {{columns:main=users,profile=user_profiles,dept=departments}}
    FROM {{table:users,alias=u}}
    {{join:left,user_profiles,p,u.id=p.user_id,if=includeProfile}}
    {{join:left,departments,d,u.dept_id=d.id,if=includeDept}}
    WHERE {{filter:active,dateRange}}
    {{sort:created_at,desc}}
    {{paginate}}")]
public partial Task<List<UserDto>> GetActiveUsersAsync(UserSearchCriteria criteria);
```

### 场景2：动态报表查询
```csharp
// 传统方式 - 需要复杂的字符串拼接
public async Task<List<ReportData>> GenerateReportAsync(ReportConfig config)
{
    var sql = "SELECT ";
    // 复杂的列名拼接逻辑...
    // 复杂的WHERE条件拼接...
    // 复杂的排序逻辑...
}

// 新方式 - 声明式模板
[Sqlx(@"
    SELECT {{columns:pattern=config.ColumnPattern,format=config.Format}}
    FROM {{table:config.TableName}}
    {{if config.HasFilters}}
        WHERE {{filters:dynamic=config.Filters}}
    {{endif}}
    {{if config.HasGrouping}}
        GROUP BY {{columns:include=config.GroupColumns}}
    {{endif}}
    {{sort:dynamic=config.SortColumns}}
    {{paginate:size=config.PageSize}}")]
public partial Task<List<ReportData>> GenerateReportAsync(ReportConfig config);
```

---

这个分析和改进方案将显著提升 Sqlx 的易用性和功能性，让开发者能够真正专注于业务逻辑，而将SQL的复杂性交给智能化的模板系统处理。

