# Sqlx 动态占位符分析器设计

## 🎯 设计目标

为动态占位符功能提供**全方位的编译时安全保护**，通过 Roslyn 分析器在开发阶段就发现潜在问题。

---

## 📋 诊断规则概览

### 规则分类

| 类别 | 规则数 | 严重级别 | 说明 |
|------|--------|---------|------|
| **安全性** | 4 | Error/Warning | 防止 SQL 注入和不安全使用 |
| **最佳实践** | 3 | Warning/Info | 推荐更安全的编码方式 |
| **代码质量** | 3 | Warning/Error | 确保代码正确性 |

---

## 🔒 安全性规则（最重要）

### SQLX2001 - 强制特性标记 ⭐
**严重级别**: Error  
**目的**: 确保动态参数被显式标记，避免误用

```csharp
// ❌ 编译错误
[Sqlx("SELECT * FROM {{@tableName}}")]
Task<User?> GetAsync(string tableName);  // 缺少 [DynamicSql]

// ✅ 正确
[Sqlx("SELECT * FROM {{@tableName}}")]
Task<User?> GetAsync([DynamicSql] string tableName);
```

**为什么重要？**
- 防止开发者无意中使用动态 SQL
- 强制显式声明意图
- 便于代码审查时快速识别

---

### SQLX2002 - 不安全的数据源
**严重级别**: Warning  
**目的**: 警告直接使用用户输入

```csharp
// ⚠️ 警告
public async Task Search(string userInput)
{
    // 直接使用用户输入，危险！
    return await _repo.GetFromTableAsync(userInput);
}
```

**检测逻辑**:
1. 参数名包含: `input`, `request`, `form`, `query`, `body`
2. 方法有 HTTP 特性: `[HttpGet]`, `[HttpPost]`
3. 参数来自 ASP.NET Core 绑定: `[FromBody]`, `[FromQuery]`

**建议修复**:
```csharp
// ✅ 使用白名单
var allowedTables = new[] { "users", "admins" };
if (!allowedTables.Contains(userInput))
    throw new ArgumentException();
```

---

### SQLX2003 - 缺少验证
**严重级别**: Warning  
**目的**: 确保调用前进行验证

```csharp
// ⚠️ 警告：调用前未验证
await repo.GetFromTableAsync(tableName);

// ✅ 正确：先验证
if (string.IsNullOrWhiteSpace(tableName) || tableName.Length > 128)
    throw new ArgumentException();
await repo.GetFromTableAsync(tableName);
```

**检测范围**: 调用点前 5 行代码  
**查找模式**: `if`, `throw`, `ArgumentException`, `Contains`, `Length`

---

### SQLX2007 - 危险 SQL 操作
**严重级别**: Warning  
**目的**: 警告潜在危险的 SQL 操作

```csharp
// ⚠️ 警告：包含危险操作
[Sqlx("DROP TABLE {{@tableName}}")]
Task DropTableAsync([DynamicSql] string tableName);

[Sqlx("DELETE FROM {{@tableName}}")]  // 没有 WHERE
Task DeleteAllAsync([DynamicSql] string tableName);
```

**检测模式**:
- `DROP TABLE`, `DROP DATABASE`
- `TRUNCATE TABLE`
- `DELETE FROM` (没有 WHERE)
- `UPDATE` (没有 WHERE)
- `EXEC`, `EXECUTE`

---

## 💡 最佳实践规则

### SQLX2004 - 建议白名单
**严重级别**: Info  
**目的**: 推荐更安全的验证方式

```csharp
// ⚠️ 可以改进
if (string.IsNullOrEmpty(tableName))
    throw new ArgumentException();

// ✅ 更好：使用白名单
private static readonly HashSet<string> AllowedTables = new() 
{ 
    "users", "admins", "guests" 
};

if (!AllowedTables.Contains(tableName))
    throw new ArgumentException();
```

**触发条件**: 有验证，但未使用白名单

---

### SQLX2005 - 公共 API 暴露
**严重级别**: Warning  
**目的**: 避免在公共 API 中暴露动态参数

```csharp
// ❌ 警告：公共 API 暴露
public class UserController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetUsers([FromQuery] string tableName)
    {
        var users = await _repo.GetFromTableAsync(tableName);
        return Ok(users);
    }
}

// ✅ 正确：使用内部映射
[HttpGet]
public async Task<IActionResult> GetUsers([FromQuery] string tableType)
{
    var tableName = tableType switch
    {
        "regular" => "users",
        "admin" => "admin_users",
        _ => throw new ArgumentException()
    };
    
    var users = await _repo.GetFromTableAsync(tableName);
    return Ok(users);
}
```

**检测逻辑**:
- 方法是 public
- 类继承自: `ControllerBase`, `Controller`, `ServiceBase`
- 方法有 HTTP 特性

---

### SQLX2008 - 建议测试
**严重级别**: Info  
**目的**: 提醒添加单元测试

```csharp
// ⚠️ 建议：添加单元测试
[Sqlx("SELECT * FROM {{@tableName}}")]
Task<List<User>> GetFromTableAsync([DynamicSql] string tableName);
```

**建议测试覆盖**:
1. ✅ 有效输入 - 正常返回数据
2. ✅ 无效输入 - 抛出异常
3. ✅ SQL 注入尝试 - 被拦截
4. ✅ 边界情况 - 空、null、超长字符串

---

## ⚙️ 代码质量规则

### SQLX2006 - 类型错误
**严重级别**: Error  
**目的**: 确保参数类型正确

```csharp
// ❌ 错误：类型必须是 string
[Sqlx("SELECT * FROM {{@tableId}}")]
Task<User?> GetAsync([DynamicSql] int tableId);

// ✅ 正确
[Sqlx("SELECT * FROM {{@tableName}}")]
Task<User?> GetAsync([DynamicSql] string tableName);
```

---

### SQLX2009 - 缺少长度限制
**严重级别**: Warning  
**目的**: 防止 DoS 攻击

```csharp
// ⚠️ 警告：未限制长度
public async Task Query([DynamicSql] string tableName)
{
    return await _repo.GetFromTableAsync(tableName);
}

// ✅ 正确
public async Task Query([DynamicSql] string tableName)
{
    if (tableName.Length > 128)
        throw new ArgumentException("Table name too long");
    
    return await _repo.GetFromTableAsync(tableName);
}
```

**推荐限制**:
- 表名/列名: 128 字符
- SQL 片段: 4096 字符
- 表名部分（前缀/后缀）: 64 字符

---

### SQLX2010 - 特性使用错误
**严重级别**: Error  
**目的**: 确保特性使用正确

**错误情况**:
1. 应用到非参数位置
2. 参数未在 SQL 模板中使用
3. SQL 模板不包含 `{{@paramName}}`

```csharp
// ❌ 错误 1：应用到方法
[DynamicSql]
public async Task Query(string tableName) { }

// ❌ 错误 2：参数未使用
[Sqlx("SELECT * FROM users")]
Task<List<User>> GetAsync([DynamicSql] string tableName);

// ✅ 正确
[Sqlx("SELECT * FROM {{@tableName}}")]
Task<List<User>> GetAsync([DynamicSql] string tableName);
```

---

## 🛠️ Code Fix Provider

为以下规则提供自动修复：

### 1. SQLX2001 - 自动添加特性
```csharp
// 修复前
Task<User?> GetAsync(string tableName);

// 一键修复后
Task<User?> GetAsync([DynamicSql] string tableName);
```

### 2. SQLX2003 - 自动添加验证
```csharp
// 修复前
await repo.GetFromTableAsync(tableName);

// 一键修复后
if (string.IsNullOrWhiteSpace(tableName) || tableName.Length > 128)
    throw new ArgumentException("Invalid table name", nameof(tableName));

await repo.GetFromTableAsync(tableName);
```

### 3. SQLX2009 - 自动添加长度检查
```csharp
// 修复前
public async Task Query([DynamicSql] string tableName)
{
    return await _repo.GetFromTableAsync(tableName);
}

// 一键修复后
public async Task Query([DynamicSql] string tableName)
{
    if (tableName.Length > 128)
        throw new ArgumentException("Table name too long", nameof(tableName));
    
    return await _repo.GetFromTableAsync(tableName);
}
```

---

## 📊 分析器执行流程

```
┌─────────────────────────────────────────────┐
│ 1. 用户编写代码                              │
│    [Sqlx("SELECT * FROM {{@tableName}}")]   │
│    Task<User?> GetAsync(string tableName);  │
└─────────────────┬───────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────────┐
│ 2. Roslyn 编译时分析                         │
│    - 解析语法树                              │
│    - 提取 [Sqlx] 特性                        │
│    - 查找动态占位符 {{@...}}                 │
└─────────────────┬───────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────────┐
│ 3. 规则检查                                  │
│    ✓ 参数是否标记 [DynamicSql]？             │
│    ✓ 参数类型是否为 string？                 │
│    ✓ SQL 是否包含危险操作？                   │
│    ✓ 参数是否来自不安全来源？                 │
└─────────────────┬───────────────────────────┘
                  │
          ┌───────┴────────┐
          │                │
          ▼                ▼
    ┌─────────┐      ┌─────────┐
    │ 有问题  │      │ 无问题  │
    └────┬────┘      └────┬────┘
         │                │
         ▼                ▼
┌──────────────────┐  ┌─────────┐
│ 4. 报告诊断       │  │ 通过    │
│    - Error       │  └─────────┘
│    - Warning     │
│    - Info        │
└────┬─────────────┘
     │
     ▼
┌─────────────────────────────────────────────┐
│ 5. 开发者修复                                │
│    - 查看错误信息                            │
│    - 手动修复或使用 Code Fix                 │
│    - 重新编译                                │
└─────────────────────────────────────────────┘
```

---

## 🎯 实现优先级

### Phase 1 - 核心安全（必须实现）
- ✅ SQLX2001 - 强制特性标记
- ✅ SQLX2006 - 类型检查
- ✅ SQLX2010 - 特性使用检查

### Phase 2 - 运行时安全（高优先级）
- ✅ SQLX2002 - 不安全数据源
- ✅ SQLX2003 - 缺少验证
- ✅ SQLX2007 - 危险操作
- ✅ SQLX2009 - 长度限制

### Phase 3 - 最佳实践（中优先级）
- ✅ SQLX2004 - 建议白名单
- ✅ SQLX2005 - 公共 API 暴露

### Phase 4 - 代码质量（低优先级）
- ✅ SQLX2008 - 建议测试

---

## 📝 配置选项

### .editorconfig 配置

```ini
# Sqlx 动态占位符分析器配置

# SQLX2001 - 强制特性标记（不可配置，始终 Error）
dotnet_diagnostic.SQLX2001.severity = error

# SQLX2002 - 不安全数据源（可调整）
dotnet_diagnostic.SQLX2002.severity = warning  # 或 error

# SQLX2003 - 缺少验证（可调整）
dotnet_diagnostic.SQLX2003.severity = warning  # 或 error

# SQLX2004 - 建议白名单（可禁用）
dotnet_diagnostic.SQLX2004.severity = suggestion  # 或 none

# SQLX2005 - 公共 API 暴露（可调整）
dotnet_diagnostic.SQLX2005.severity = warning  # 或 error

# SQLX2006 - 类型错误（不可配置，始终 Error）
dotnet_diagnostic.SQLX2006.severity = error

# SQLX2007 - 危险操作（可调整）
dotnet_diagnostic.SQLX2007.severity = warning  # 或 error

# SQLX2008 - 建议测试（可禁用）
dotnet_diagnostic.SQLX2008.severity = suggestion  # 或 none

# SQLX2009 - 长度限制（可调整）
dotnet_diagnostic.SQLX2009.severity = warning

# SQLX2010 - 特性使用错误（不可配置，始终 Error）
dotnet_diagnostic.SQLX2010.severity = error
```

---

## 🧪 测试策略

### 单元测试覆盖

每个规则至少需要以下测试：

1. **正向测试** - 不触发诊断
2. **负向测试** - 触发诊断
3. **边界测试** - 边界条件
4. **Code Fix 测试** - 自动修复正确性

**示例（SQLX2001）**:
```csharp
[Fact]
public void SQLX2001_NoDiagnostic_WhenAttributePresent()
{
    var source = @"
[Sqlx(""SELECT * FROM {{@tableName}}"")]
Task<User?> GetAsync([DynamicSql] string tableName);
";
    VerifyNoDiagnostic(source);
}

[Fact]
public void SQLX2001_Diagnostic_WhenAttributeMissing()
{
    var source = @"
[Sqlx(""SELECT * FROM {{@tableName}}"")]
Task<User?> GetAsync(string tableName);  // ← 应该报错
";
    var expected = Diagnostic("SQLX2001").WithLocation(3, 22);
    VerifyDiagnostic(source, expected);
}

[Fact]
public void SQLX2001_CodeFix_AddsAttribute()
{
    var source = @"
[Sqlx(""SELECT * FROM {{@tableName}}"")]
Task<User?> GetAsync(string tableName);
";
    var fixedSource = @"
[Sqlx(""SELECT * FROM {{@tableName}}"")]
Task<User?> GetAsync([DynamicSql] string tableName);
";
    VerifyCodeFix(source, fixedSource);
}
```

---

## 📚 文档要求

### 规则文档模板

每个规则需要提供：

1. **规则 ID 和标题**
2. **类别**（Security/Usage/Design/Performance）
3. **严重级别**（Error/Warning/Info）
4. **描述**（为什么需要这个规则）
5. **示例代码**（违规和正确）
6. **如何修复**
7. **配置选项**（如果有）
8. **相关规则**

---

## 🚀 实施计划

### Week 1-2: 核心框架
- [ ] 创建分析器项目结构
- [ ] 实现 10 个诊断规则
- [ ] 单元测试（100% 覆盖）

### Week 3: Code Fix
- [ ] 实现 SQLX2001 自动修复
- [ ] 实现 SQLX2003 自动修复
- [ ] 实现 SQLX2009 自动修复

### Week 4: 文档和发布
- [ ] 编写规则文档
- [ ] 更新 README
- [ ] 集成到 Sqlx.Generator
- [ ] 发布 NuGet 包

---

## ✅ 成功标准

1. ✅ 所有 10 个规则正常工作
2. ✅ 单元测试覆盖率 100%
3. ✅ 至少 3 个 Code Fix 可用
4. ✅ 性能影响 < 5% 编译时间
5. ✅ 文档完整且易懂
6. ✅ 与现有 Sqlx 功能无缝集成

---

## 📖 参考资料

- [Roslyn Analyzers 官方文档](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/)
- [编写 Code Fix Provider](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/tutorials/how-to-write-csharp-analyzer-code-fix)
- [分析器性能最佳实践](https://github.com/dotnet/roslyn-analyzers/blob/main/docs/Analyzer%20Configuration.md)

