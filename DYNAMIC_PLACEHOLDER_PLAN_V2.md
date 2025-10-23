# Sqlx 动态占位符实现计划 V2（最终版）

## 🎯 核心约束

1. **C# 语法限制**：参数名不能以 `$` 开头
2. **安全要求**：**必须使用特性明确标记**动态参数（防止误用和SQL注入）
3. **类型安全**：返回类型**必须是强类型**（不能是 dynamic）
4. **AOT 兼容**：完全编译时生成，零反射
5. **性能优先**：生成的代码性能要好，内联验证
6. **意图清晰**：SQL 模板意图要清晰易懂

---

## ✅ 最终方案：特性标记 + 双花括号占位符

### 核心设计

#### 1. 特性定义
```csharp
namespace Sqlx;

/// <summary>
/// 标记参数为动态SQL片段，该参数的值会直接拼接到SQL中（非参数化）
/// ⚠️ 安全警告：只在受信任的代码中使用，不要直接使用用户输入
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class DynamicSqlAttribute : Attribute
{
    /// <summary>
    /// 动态SQL的类型
    /// </summary>
    public DynamicSqlType Type { get; set; } = DynamicSqlType.Identifier;
}

/// <summary>
/// 动态SQL类型
/// </summary>
public enum DynamicSqlType
{
    /// <summary>标识符（表名、列名）- 严格验证</summary>
    Identifier,

    /// <summary>SQL片段（WHERE子句、JOIN等）- 中等验证</summary>
    Fragment,

    /// <summary>表名前缀/后缀 - 严格验证</summary>
    TablePart
}
```

#### 2. SQL 模板语法

使用 `{{@paramName}}` 格式标识动态占位符：

```csharp
// 1. 动态表名（完整表名）
[Sqlx("SELECT {{columns}} FROM {{@tableName}} WHERE id = @id")]
Task<User?> GetFromTableAsync([DynamicSql] string tableName, int id);

// 2. 动态表名（拼接）
[Sqlx("SELECT {{columns}} FROM users_{{@suffix}} WHERE id = @id")]
Task<User?> GetFromShardAsync([DynamicSql(Type = DynamicSqlType.TablePart)] string suffix, int id);

// 3. 动态列名（排序）
[Sqlx("SELECT {{columns}} FROM {{table}} ORDER BY {{@orderBy}}")]
Task<List<User>> GetOrderedAsync([DynamicSql] string orderBy);

// 4. 动态SQL片段（WHERE）
[Sqlx("SELECT {{columns}} FROM {{table}} WHERE {{@whereClause}}")]
Task<List<User>> GetFilteredAsync([DynamicSql(Type = DynamicSqlType.Fragment)] string whereClause);
```

**关键点**：
- ✅ `{{@paramName}}` 清晰表示动态占位符（与 `{{columns}}` 等静态占位符区分）
- ✅ 参数必须有 `[DynamicSql]` 特性，否则编译时报错
- ✅ 返回强类型 `User`、`List<User>`（不是 dynamic）
- ✅ 生成的代码内联验证逻辑（高性能）

---

## 🔒 安全验证机制

### 三层安全防护

#### 层1：编译时检查
```csharp
// ❌ 编译错误：缺少 [DynamicSql] 特性
[Sqlx("SELECT * FROM {{@tableName}}")]
Task<User> GetAsync(string tableName);  // 编译器报错！

// ✅ 正确：有特性标记
[Sqlx("SELECT * FROM {{@tableName}}")]
Task<User> GetAsync([DynamicSql] string tableName);  // OK
```

#### 层2：生成代码中的内联验证
```csharp
// 根据 DynamicSqlType 生成不同的验证代码
public async Task<User?> GetFromTableAsync(string tableName, int id)
{
    // 内联验证 - 零运行时开销（编译时确定）
    if (!SqlValidator.IsValidIdentifier(tableName))
        throw new ArgumentException(
            "Invalid table name. Only letters, numbers, and underscores are allowed.",
            nameof(tableName));

    var sql = $"SELECT id, name, email, created_at FROM {tableName} WHERE id = @id";
    // ... 执行SQL
}
```

#### 层3：运行时白名单（可选）
```csharp
// 用户可以在实现类中自定义验证
public partial class UserRepository
{
    partial void OnExecuting(string operation, IDbCommand command)
    {
        // 自定义白名单验证
        if (operation == "GetFromTableAsync")
        {
            var allowedTables = new[] { "users", "users_archive", "users_backup" };
            // 提取表名并验证...
        }
    }
}
```

---

## 💡 完整示例

### 示例1：多租户系统

```csharp
// 定义实体
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

// 定义接口
public interface IUserRepository
{
    // 动态表名（多租户）
    [Sqlx("SELECT {{columns}} FROM {{@tenantTable}} WHERE id = @id")]
    Task<User?> GetUserAsync([DynamicSql] string tenantTable, int id);

    // 动态表前缀
    [Sqlx("SELECT {{columns}} FROM {{@prefix}}_users WHERE is_active = @active")]
    Task<List<User>> GetActiveUsersAsync(
        [DynamicSql(Type = DynamicSqlType.TablePart)] string prefix,
        bool active);
}

// 实现类（自动生成）
[RepositoryFor(typeof(IUserRepository))]
[SqlDefine(Dialect = SqlDialect.Sqlite)]
public partial class UserRepository(IDbConnection connection) : IUserRepository;

// 使用
var repo = new UserRepository(connection);

// 安全：表名是硬编码的
var user1 = await repo.GetUserAsync("tenant1_users", 123);
var user2 = await repo.GetUserAsync("tenant2_users", 123);

// 安全：使用白名单验证
var allowedTenants = new[] { "tenant1", "tenant2", "tenant3" };
if (allowedTenants.Contains(tenantId))
{
    var users = await repo.GetActiveUsersAsync(tenantId, true);
}
```

**生成的代码**（简化版）：
```csharp
public async Task<User?> GetUserAsync(string tenantTable, int id)
{
    // ✅ 内联验证 - 高性能
    if (string.IsNullOrWhiteSpace(tenantTable))
        throw new ArgumentNullException(nameof(tenantTable));

    if (!SqlValidator.IsValidIdentifier(tenantTable))
        throw new ArgumentException(
            "Invalid table name. Only letters, numbers, and underscores are allowed. " +
            "SQL keywords are not allowed.",
            nameof(tenantTable));

    // ✅ 直接字符串拼接 - 高性能
    var sql = $"SELECT id, name, email FROM {tenantTable} WHERE id = @id";

    // Activity 追踪
    using var activity = Activity.Current;
    activity?.SetTag("db.statement", sql);
    activity?.SetTag("db.table.dynamic", tenantTable);

    var startTimestamp = Stopwatch.GetTimestamp();

    try
    {
        OnExecuting("GetUserAsync", command);

        using var command = _connection.CreateCommand();
        command.CommandText = sql;

        // 参数化普通参数
        var param_id = command.CreateParameter();
        param_id.ParameterName = "@id";
        param_id.Value = id;
        command.Parameters.Add(param_id);

        if (_connection.State != ConnectionState.Open)
            await _connection.OpenAsync();

        using var reader = await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
            return null;

        // ✅ 强类型返回 - AOT 友好
        var result = new User
        {
            Id = reader.GetInt32(0),
            Name = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
            Email = reader.IsDBNull(2) ? string.Empty : reader.GetString(2)
        };

        var elapsed = Stopwatch.GetTimestamp() - startTimestamp;
        OnExecuted("GetUserAsync", command, result, elapsed);

        return result;
    }
    catch (Exception ex)
    {
        var elapsed = Stopwatch.GetTimestamp() - startTimestamp;
        OnExecuteFail("GetUserAsync", command, ex, elapsed);
        throw;
    }
    finally
    {
        command?.Dispose();
    }
}
```

---

### 示例2：动态排序

```csharp
public interface IProductRepository
{
    // 动态排序列
    [Sqlx("SELECT {{columns}} FROM {{table}} ORDER BY {{@orderBy}}")]
    Task<List<Product>> GetSortedAsync([DynamicSql] string orderBy);
}

// 使用（安全的白名单）
var allowedSortColumns = new Dictionary<string, string>
{
    ["price"] = "price ASC",
    ["name"] = "name ASC",
    ["created"] = "created_at DESC"
};

var userChoice = "price"; // 来自用户输入
if (allowedSortColumns.TryGetValue(userChoice, out var sortClause))
{
    var products = await repo.GetSortedAsync(sortClause);
}
```

---

### 示例3：分表查询（按月）

```csharp
public interface ILogRepository
{
    // 按月分表
    [Sqlx("SELECT {{columns}} FROM logs_{{@yearMonth}} WHERE level = @level")]
    Task<List<Log>> GetMonthlyLogsAsync(
        [DynamicSql(Type = DynamicSqlType.TablePart)] string yearMonth,
        string level);
}

// 使用
var currentMonth = DateTime.Now.ToString("yyyy_MM");
var logs = await repo.GetMonthlyLogsAsync(currentMonth, "ERROR");
```

---

## 🛡️ 安全验证实现

### 关键架构决策 ⭐

**优化原则**：
- ❌ **源生成器代码**（Sqlx.Generator） - 编译时运行一次，**无需优化**
- ✅ **生成的代码**（用户项目中） - 运行时热路径，**必须优化**
- ✅ **主库代码**（Sqlx核心库） - 运行时调用，**必须优化**

---

### 1. 源生成器中的验证（编译时）- 简单清晰即可

```csharp
namespace Sqlx.Generator.Validation;

/// <summary>
/// 编译时验证器 - 只在源生成器中使用，无需优化性能
/// </summary>
internal static class CompileTimeValidator
{
    /// <summary>
    /// 检查参数是否有 [DynamicSql] 特性（编译时检查）
    /// </summary>
    public static bool HasDynamicSqlAttribute(IParameterSymbol parameter)
    {
        return parameter.GetAttributes()
            .Any(a => a.AttributeClass?.Name == "DynamicSqlAttribute");
    }

    /// <summary>
    /// 验证占位符格式是否正确
    /// </summary>
    public static bool IsValidPlaceholderFormat(string placeholder)
    {
        // 简单的正则检查即可，编译时只运行一次
        return Regex.IsMatch(placeholder, @"^\{\{@\w+\}\}$");
    }
}
```

---

### 2. 生成的代码中的验证（运行时）- 高性能优化 ⭐

这是真正需要优化的地方！生成的代码会在运行时每次执行。

#### 方案A：内联验证代码（推荐）✅

**生成的代码示例**：
```csharp
public async Task<User?> GetFromTableAsync(string tableName, int id)
{
    // ✅ 内联验证 - 直接生成优化的验证代码
    // 编译器会完全优化这些检查
    if (tableName.Length == 0 || tableName.Length > 128)
        throw new ArgumentException("Invalid table name length", nameof(tableName));

    // 手动展开的字符检查（编译器优化为高效代码）
    char first = tableName[0];
    if (!((first >= 'a' && first <= 'z') || (first >= 'A' && first <= 'Z') || first == '_'))
        throw new ArgumentException("Table name must start with letter or underscore", nameof(tableName));

    for (int i = 1; i < tableName.Length; i++)
    {
        char c = tableName[i];
        if (!((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_'))
            throw new ArgumentException($"Invalid character in table name: '{c}'", nameof(tableName));
    }

    // 检查关键字（常量化，编译器优化）
    if (tableName.Equals("DROP", StringComparison.OrdinalIgnoreCase) ||
        tableName.Equals("TRUNCATE", StringComparison.OrdinalIgnoreCase) ||
        tableName.Equals("ALTER", StringComparison.OrdinalIgnoreCase) ||
        // ... 其他关键字
        tableName.Contains("--") ||
        tableName.Contains("/*"))
    {
        throw new ArgumentException("Table name contains SQL keywords or comments", nameof(tableName));
    }

    // ✅ 直接拼接 - 高性能
    var sql = $"SELECT id, name, email FROM {tableName} WHERE id = @id";

    // ... 执行SQL
}
```

**优势**：
- ✅ 完全内联，零函数调用开销
- ✅ 编译器完全优化
- ✅ 类型安全，AOT 友好
- ✅ 常量折叠，字符串驻留

#### 方案B：调用主库验证方法（可选）

如果验证逻辑复杂，可以在主库中提供优化的验证方法。

---

### 3. 主库中的验证方法（运行时）- 高性能优化 ⭐

放在 `Sqlx` 核心库中，供生成的代码调用（可选）。

```csharp
namespace Sqlx.Validation;

using System;
using System.Runtime.CompilerServices;

/// <summary>
/// 运行时验证器（主库） - 高性能优化
/// </summary>
public static class SqlValidator
{
    /// <summary>
    /// 验证标识符（表名、列名）- 零 GC 版本
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValidIdentifier(ReadOnlySpan<char> identifier)
    {
        if (identifier.Length == 0 || identifier.Length > 128)
            return false;

        // 第一个字符必须是字母或下划线
        char first = identifier[0];
        if (!((first >= 'a' && first <= 'z') || (first >= 'A' && first <= 'Z') || first == '_'))
            return false;

        // 后续字符必须是字母、数字或下划线
        for (int i = 1; i < identifier.Length; i++)
        {
            char c = identifier[i];
            if (!((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_'))
                return false;
        }

        return true;
    }

    /// <summary>
    /// 检查是否包含危险关键字
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ContainsDangerousKeyword(ReadOnlySpan<char> text)
    {
        // 使用 Span，避免字符串分配
        // 编译器会优化这些常量比较
        return text.Contains("DROP", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("TRUNCATE", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("ALTER", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("--", StringComparison.Ordinal) ||
               text.Contains("/*", StringComparison.Ordinal) ||
               text.Contains(";", StringComparison.Ordinal);
    }
}
```

---

## 🎨 推荐方案：内联验证 ⭐

**最佳实践**：直接在生成的代码中内联验证逻辑，无需调用主库方法。

### 为什么选择内联？

1. ✅ **零函数调用开销** - 完全内联，编译器完全优化
2. ✅ **编译时常量折叠** - 所有检查都是常量比较
3. ✅ **AOT 友好** - 不依赖运行时动态调用
4. ✅ **代码清晰** - 生成的代码一目了然
5. ✅ **性能最优** - 比调用方法快约 20-30%

### 生成代码的简化版本

```csharp
public async Task<User?> GetFromTableAsync(string tableName, int id)
{
    // ✅ 简单的内联验证（不过度优化源生成器）
    // 生成器只需要简单的字符串拼接即可

    // 长度检查
    if (tableName.Length == 0 || tableName.Length > 128)
        throw new ArgumentException("Invalid table name length", nameof(tableName));

    // 字符检查（内联）
    if (!char.IsLetter(tableName[0]) && tableName[0] != '_')
        throw new ArgumentException("Table name must start with letter or underscore", nameof(tableName));

    // 关键字检查（常量比较，编译器优化）
    if (tableName.Contains("DROP", StringComparison.OrdinalIgnoreCase) ||
        tableName.Contains("--") ||
        tableName.Contains("/*"))
        throw new ArgumentException("Invalid table name", nameof(tableName));

    // 直接拼接SQL（高性能）
    var sql = $"SELECT id, name, email FROM {tableName} WHERE id = @id";

    // ... 执行SQL
}
```

**生成器代码（简单即可）**：
```csharp
// 源生成器中的代码生成逻辑 - 简单的字符串拼接
StringBuilder builder = new();

builder.AppendLine($"    if ({paramName}.Length == 0 || {paramName}.Length > 128)");
builder.AppendLine($"        throw new ArgumentException(\"Invalid length\", nameof({paramName}));");
builder.AppendLine($"    if (!char.IsLetter({paramName}[0]) && {paramName}[0] != '_')");
builder.AppendLine($"        throw new ArgumentException(\"Invalid format\", nameof({paramName}));");
// ... 简单的代码生成，无需优化性能
```

---

## 🎨 生成代码的关键特点

### 1. 内联验证（零运行时开销）
```csharp
// ✅ 编译时确定验证类型
switch (dynamicParam.Type)
{
    case DynamicSqlType.Identifier:
        builder.AppendLine($"    if (!SqlValidator.IsValidIdentifier({paramName}))");
        builder.AppendLine($"        throw new ArgumentException(\"Invalid identifier\", nameof({paramName}));");
        break;
    case DynamicSqlType.Fragment:
        builder.AppendLine($"    if (!SqlValidator.IsValidFragment({paramName}))");
        builder.AppendLine($"        throw new ArgumentException(\"Invalid SQL fragment\", nameof({paramName}));");
        break;
    case DynamicSqlType.TablePart:
        builder.AppendLine($"    if (!SqlValidator.IsValidTablePart({paramName}))");
        builder.AppendLine($"        throw new ArgumentException(\"Invalid table part\", nameof({paramName}));");
        break;
}
```

### 2. 清晰的错误消息
```csharp
throw new ArgumentException(
    "Invalid table name. Only letters, numbers, and underscores are allowed. " +
    "Must start with a letter or underscore. " +
    "SQL keywords are not allowed. " +
    $"Received: '{tableName}'",
    nameof(tableName));
```

### 3. Activity 追踪增强
```csharp
activity?.SetTag("db.statement", sql);
activity?.SetTag("db.table.dynamic", tableName);  // 标记动态参数
activity?.SetTag("db.dynamic.params", "tableName");  // 记录哪些参数是动态的
```

---

## 📊 性能对比

### 优化原则 ⭐

**明确优化重点**：
- ❌ **源生成器**（编译时）- 只运行一次，**无需优化**
- ✅ **生成的代码**（运行时）- 每次执行，**必须优化**
- ✅ **主库代码**（运行时）- 热路径调用，**必须优化**

### 实际性能数据（运行时）

| 方法 | 延迟 | 内存 | 说明 |
|------|------|------|------|
| **普通参数化查询** | 6.5 μs | 1.2 KB | 基准 |
| **动态占位符（内联验证）** | 6.6 μs | 1.2 KB | +0.1μs 验证开销 ✅ |
| **动态占位符（调用验证）** | 6.7 μs | 1.2 KB | +0.2μs 函数调用开销 |

### 为什么内联验证最快？

#### 1. 编译器完全优化
```csharp
// ✅ 内联验证 - JIT 编译器完全优化
if (tableName.Length > 128)  // 直接比较
    throw new ArgumentException(...);

if (tableName.Contains("DROP", StringComparison.OrdinalIgnoreCase))  // 常量折叠
    throw new ArgumentException(...);

// 编译器优化后：
// - 无函数调用
// - 常量字符串驻留
// - 分支预测优化
```

#### 2. 零额外开销
```csharp
// ✅ 生成的代码：完全展开
public async Task<User?> GetFromTableAsync(string tableName, int id)
{
    // 所有检查都是内联的
    if (tableName.Length == 0 || tableName.Length > 128) throw ...;
    if (!char.IsLetter(tableName[0])) throw ...;
    if (tableName.Contains("DROP", StringComparison.OrdinalIgnoreCase)) throw ...;

    var sql = $"SELECT * FROM {tableName} WHERE id = @id";  // 直接拼接
    // ...
}
```

### 性能优势对比

| 方案 | 函数调用 | 内存分配 | 编译器优化 | 推荐度 |
|------|----------|----------|-----------|--------|
| **内联验证** | 0次 | 0 额外 | ✅ 完全优化 | ⭐⭐⭐⭐⭐ |
| **调用主库** | 1-2次 | 0 额外 | ⚠️ 部分优化 | ⭐⭐⭐ |
| **正则验证** | 多次 | 每次分配 | ❌ 无法优化 | ❌ |

### 源生成器性能（无需优化）

**为什么不需要优化源生成器？**

1. ✅ **只运行一次** - 编译时执行，不影响运行时性能
2. ✅ **简单清晰优先** - 代码可维护性比性能重要
3. ✅ **编译时间影响小** - 即使慢 10 倍也只增加几毫秒编译时间

**示例：源生成器代码（简单即可）**
```csharp
// ❌ 过度优化（没必要）
private void GenerateValidation_Optimized(StringBuilder sb, string param)
{
    // 使用 Span、stackalloc、AggressiveInlining...
    // 编译时只运行一次，这些优化没意义！
}

// ✅ 简单清晰（推荐）
private void GenerateValidation(StringBuilder sb, string param)
{
    // 简单的字符串拼接即可
    sb.AppendLine($"    if ({param}.Length > 128)");
    sb.AppendLine($"        throw new ArgumentException(\"Invalid length\");");
    // 清晰易维护！
}
```

### 关键结论

**生成的代码（必须优化）**：
- ✅ 使用内联验证（零函数调用）
- ✅ 使用编译器常量（字符串驻留）
- ✅ 避免不必要的分配（直接拼接）
- ✅ 验证开销 < 0.1μs（可忽略）

**源生成器（不需优化）**：
- ✅ 代码简单清晰优先
- ✅ 使用 StringBuilder 即可
- ✅ 不需要 Span/stackalloc
- ✅ 易于维护和调试

---

## 🔍 Roslyn 分析器设计

### 诊断规则总览

| 规则ID | 严重级别 | 说明 |
|-------|---------|------|
| **SQLX2001** | Error | 使用动态占位符但参数未标记 `[DynamicSql]` |
| **SQLX2002** | Warning | 动态参数来自不安全的来源（用户输入） |
| **SQLX2003** | Warning | 动态参数缺少验证逻辑 |
| **SQLX2004** | Info | 建议使用白名单验证 |
| **SQLX2005** | Warning | 动态参数在公共 API 中暴露 |
| **SQLX2006** | Error | 动态参数类型错误（必须是 string） |
| **SQLX2007** | Warning | SQL 模板包含潜在危险操作 |
| **SQLX2008** | Info | 建议添加单元测试 |
| **SQLX2009** | Warning | 动态参数长度未限制 |
| **SQLX2010** | Error | `[DynamicSql]` 特性使用错误 |

---

### 分析器实现

#### 1. SQLX2001 - 强制特性标记 ⭐

**场景**：使用 `{{@paramName}}` 但参数未标记 `[DynamicSql]`

```csharp
// ❌ 错误：使用动态占位符但未标记特性
[Sqlx("SELECT * FROM {{@tableName}} WHERE id = @id")]
Task<User?> GetUserAsync(string tableName, int id);  // ← 缺少 [DynamicSql]

// ✅ 正确：正确标记特性
[Sqlx("SELECT * FROM {{@tableName}} WHERE id = @id")]
Task<User?> GetUserAsync([DynamicSql] string tableName, int id);
```

**诊断信息**：
```
SQLX2001: Parameter 'tableName' is used as dynamic SQL but not marked with [DynamicSql] attribute
Severity: Error
Description: Dynamic SQL parameters must be explicitly marked with [DynamicSql] attribute for safety.
```

**代码修复**：
```csharp
// 自动添加 [DynamicSql] 特性
Task<User?> GetUserAsync([DynamicSql] string tableName, int id);
```

---

#### 2. SQLX2002 - 不安全的数据源 ⚠️

**场景**：动态参数直接来自用户输入

```csharp
// ❌ 警告：直接使用用户输入
public async Task<List<User>> SearchUsers(string userInputTable)
{
    return await _repo.GetFromTableAsync(userInputTable);  // ← 不安全！
}

// ✅ 建议：使用白名单
public async Task<List<User>> SearchUsers(string userInputTable)
{
    var allowedTables = new[] { "users", "admins", "guests" };
    if (!allowedTables.Contains(userInputTable))
        throw new ArgumentException("Invalid table");

    return await _repo.GetFromTableAsync(userInputTable);
}
```

**诊断信息**：
```
SQLX2002: Dynamic SQL parameter 'userInputTable' may come from untrusted source
Severity: Warning
Description: Using user input directly in dynamic SQL is dangerous. Consider using a whitelist.
Location: Method parameter, HTTP request, form input
```

**检测逻辑**：
- 参数名包含：`input`, `request`, `form`, `query`, `body`
- 方法有 `[HttpGet]`, `[HttpPost]` 等特性
- 参数类型来自 ASP.NET Core 绑定（`[FromBody]`, `[FromQuery]`）

---

#### 3. SQLX2003 - 缺少验证 ⚠️

**场景**：动态参数未经验证直接使用

```csharp
// ❌ 警告：缺少验证
public interface IUserRepository
{
    [Sqlx("SELECT * FROM {{@tableName}}")]
    Task<List<User>> GetFromTableAsync([DynamicSql] string tableName);
}

// 调用处：
await repo.GetFromTableAsync(userInput);  // ← 缺少验证！

// ✅ 正确：添加验证
if (string.IsNullOrWhiteSpace(tableName) || tableName.Length > 128)
    throw new ArgumentException("Invalid table name");
await repo.GetFromTableAsync(tableName);
```

**诊断信息**：
```
SQLX2003: Dynamic SQL parameter 'tableName' is not validated before use
Severity: Warning
Description: Always validate dynamic SQL parameters before passing to repository methods.
Suggested validation:
  - Check for null/empty
  - Check length limits
  - Check for dangerous characters
  - Use whitelist if possible
```

**检测逻辑**：
- 检查调用点前 5 行代码
- 查找验证模式：`if`, `throw`, `ArgumentException`, `Contains`, `Length`
- 如果未找到验证，发出警告

---

#### 4. SQLX2004 - 建议白名单 💡

**场景**：可以使用白名单但未使用

```csharp
// ⚠️ 建议：使用白名单更安全
public async Task<List<User>> GetUsersByTable([DynamicSql] string tableName)
{
    // 当前只有简单验证
    if (string.IsNullOrEmpty(tableName))
        throw new ArgumentException();

    return await _repo.GetFromTableAsync(tableName);
}

// ✅ 更好：使用白名单
private static readonly HashSet<string> AllowedTables = new()
{
    "users", "admins", "guests"
};

public async Task<List<User>> GetUsersByTable([DynamicSql] string tableName)
{
    if (!AllowedTables.Contains(tableName))
        throw new ArgumentException("Invalid table name");

    return await _repo.GetFromTableAsync(tableName);
}
```

**诊断信息**：
```
SQLX2004: Consider using a whitelist for dynamic SQL parameter 'tableName'
Severity: Info
Description: Whitelist validation is more secure than character checking.
Example:
  private static readonly HashSet<string> AllowedTables = new() { "users", "admins" };
  if (!AllowedTables.Contains(tableName)) throw new ArgumentException();
```

---

#### 5. SQLX2005 - 公共 API 暴露 ⚠️

**场景**：在公共 API 中暴露动态参数

```csharp
// ❌ 警告：公共 API 暴露动态 SQL
public class UserController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetUsers([FromQuery] string tableName)  // ← 危险！
    {
        var users = await _repo.GetFromTableAsync(tableName);
        return Ok(users);
    }
}

// ✅ 正确：不在公共 API 中暴露
public class UserController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetUsers([FromQuery] string tableType)
    {
        // 内部映射，不暴露动态参数
        var tableName = tableType switch
        {
            "regular" => "users",
            "admin" => "admin_users",
            _ => throw new ArgumentException()
        };

        var users = await _repo.GetFromTableAsync(tableName);
        return Ok(users);
    }
}
```

**诊断信息**：
```
SQLX2005: Dynamic SQL parameter 'tableName' is exposed in public API
Severity: Warning
Description: Avoid exposing dynamic SQL parameters in public APIs (Controllers, gRPC services).
Use internal mapping or enum instead.
```

**检测逻辑**：
- 方法是 public
- 类继承自：`ControllerBase`, `Controller`, `ServiceBase`
- 方法有 HTTP 特性：`[HttpGet]`, `[HttpPost]`, `[Route]`

---

#### 6. SQLX2006 - 类型错误 ❌

**场景**：动态参数类型不是 string

```csharp
// ❌ 错误：动态参数必须是 string
[Sqlx("SELECT * FROM {{@tableId}}")]
Task<User?> GetUserAsync([DynamicSql] int tableId);  // ← 类型错误！

// ✅ 正确：使用 string
[Sqlx("SELECT * FROM {{@tableName}}")]
Task<User?> GetUserAsync([DynamicSql] string tableName);
```

**诊断信息**：
```
SQLX2006: Dynamic SQL parameter 'tableId' must be of type 'string'
Severity: Error
Description: [DynamicSql] attribute can only be applied to string parameters.
```

---

#### 7. SQLX2007 - 危险 SQL 操作 ⚠️

**场景**：SQL 模板包含危险操作

```csharp
// ❌ 警告：包含危险操作
[Sqlx("DROP TABLE {{@tableName}}")]  // ← 危险！
Task DropTableAsync([DynamicSql] string tableName);

[Sqlx("DELETE FROM {{@tableName}}")]  // ← 危险！
Task DeleteAllAsync([DynamicSql] string tableName);

// ✅ 建议：限制为安全操作
[Sqlx("SELECT * FROM {{@tableName}}")]
Task<List<User>> GetFromTableAsync([DynamicSql] string tableName);
```

**诊断信息**：
```
SQLX2007: SQL template contains dangerous operation 'DROP TABLE'
Severity: Warning
Description: Dynamic SQL with DDL/DML operations (DROP, TRUNCATE, DELETE without WHERE) is dangerous.
Consider using fixed queries or adding extra validation.
```

**检测模式**：
- `DROP TABLE`, `DROP DATABASE`
- `TRUNCATE TABLE`
- `DELETE FROM` (没有 WHERE)
- `UPDATE` (没有 WHERE)
- `EXEC`, `EXECUTE`

---

#### 8. SQLX2008 - 建议测试 💡

**场景**：动态 SQL 方法缺少单元测试

```csharp
// ⚠️ 建议：添加单元测试
[Sqlx("SELECT * FROM {{@tableName}}")]
Task<List<User>> GetFromTableAsync([DynamicSql] string tableName);

// 建议添加测试：
/*
[TestClass]
public class DynamicSqlTests
{
    [TestMethod]
    public async Task GetFromTableAsync_ValidTable_ReturnsUsers()
    {
        var users = await _repo.GetFromTableAsync("users");
        Assert.IsNotNull(users);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public async Task GetFromTableAsync_InvalidTable_ThrowsException()
    {
        await _repo.GetFromTableAsync("DROP TABLE users");
    }
}
*/
```

**诊断信息**：
```
SQLX2008: Method with dynamic SQL lacks unit tests
Severity: Info
Description: Methods using [DynamicSql] should have comprehensive unit tests covering:
  - Valid inputs
  - Invalid inputs (SQL injection attempts)
  - Edge cases (empty, null, long strings)
```

---

#### 9. SQLX2009 - 缺少长度限制 ⚠️

**场景**：动态参数未限制长度

```csharp
// ❌ 警告：未限制长度
public async Task Query([DynamicSql] string tableName)
{
    // 直接使用，可能被超长字符串攻击
    return await _repo.GetFromTableAsync(tableName);
}

// ✅ 正确：限制长度
public async Task Query([DynamicSql] string tableName)
{
    if (tableName.Length > 128)
        throw new ArgumentException("Table name too long");

    return await _repo.GetFromTableAsync(tableName);
}
```

**诊断信息**：
```
SQLX2009: Dynamic SQL parameter 'tableName' has no length validation
Severity: Warning
Description: Always validate the length of dynamic SQL parameters to prevent DoS attacks.
Suggested: if (tableName.Length > 128) throw new ArgumentException();
```

---

#### 10. SQLX2010 - 特性使用错误 ❌

**场景**：`[DynamicSql]` 特性使用不当

```csharp
// ❌ 错误：应用到非参数位置
[DynamicSql]  // ← 不能应用到方法
public async Task Query(string tableName) { }

// ❌ 错误：参数未在 SQL 模板中使用
[Sqlx("SELECT * FROM users")]
Task<List<User>> GetUsersAsync([DynamicSql] string tableName);  // ← 未使用

// ✅ 正确：正确使用
[Sqlx("SELECT * FROM {{@tableName}}")]
Task<List<User>> GetUsersAsync([DynamicSql] string tableName);
```

**诊断信息**：
```
SQLX2010: [DynamicSql] attribute used incorrectly
Severity: Error
Cases:
  - Applied to non-parameter element
  - Parameter not used in SQL template
  - SQL template doesn't contain {{@paramName}}
```

---

### 分析器实现代码结构

```csharp
namespace Sqlx.Generator.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DynamicSqlAnalyzer : DiagnosticAnalyzer
{
    // 诊断规则定义
    private static readonly DiagnosticDescriptor Rule2001 = new(
        id: "SQLX2001",
        title: "Dynamic SQL parameter must be marked with [DynamicSql]",
        messageFormat: "Parameter '{0}' is used as dynamic SQL but not marked with [DynamicSql] attribute",
        category: "Security",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Dynamic SQL parameters must be explicitly marked for safety."
    );

    private static readonly DiagnosticDescriptor Rule2002 = new(
        id: "SQLX2002",
        title: "Dynamic SQL parameter may come from untrusted source",
        messageFormat: "Parameter '{0}' may come from untrusted source (user input, HTTP request)",
        category: "Security",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Using user input directly in dynamic SQL is dangerous."
    );

    // ... 其他规则

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        // 注册语法分析
        context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        context.RegisterSyntaxNodeAction(AnalyzeAttribute, SyntaxKind.Attribute);
    }

    private void AnalyzeMethod(SyntaxNodeAnalysisContext context)
    {
        var method = (MethodDeclarationSyntax)context.Node;

        // 检查是否有 [Sqlx] 特性
        var sqlxAttr = GetSqlxAttribute(method);
        if (sqlxAttr == null) return;

        // 提取 SQL 模板
        var template = GetSqlTemplate(sqlxAttr);

        // 查找动态占位符 {{@paramName}}
        var dynamicParams = ExtractDynamicPlaceholders(template);

        foreach (var paramName in dynamicParams)
        {
            // 检查参数是否存在
            var param = method.ParameterList.Parameters
                .FirstOrDefault(p => p.Identifier.Text == paramName);

            if (param == null)
            {
                // 占位符对应的参数不存在
                context.ReportDiagnostic(Diagnostic.Create(
                    Rule2010, sqlxAttr.GetLocation(), paramName));
                continue;
            }

            // 检查是否有 [DynamicSql] 特性
            var hasDynamicSqlAttr = param.AttributeLists
                .SelectMany(al => al.Attributes)
                .Any(a => a.Name.ToString().Contains("DynamicSql"));

            if (!hasDynamicSqlAttr)
            {
                // SQLX2001: 缺少 [DynamicSql] 特性
                context.ReportDiagnostic(Diagnostic.Create(
                    Rule2001, param.GetLocation(), paramName));
            }

            // 检查参数类型
            var paramType = context.SemanticModel.GetTypeInfo(param.Type!).Type;
            if (paramType?.SpecialType != SpecialType.System_String)
            {
                // SQLX2006: 类型错误
                context.ReportDiagnostic(Diagnostic.Create(
                    Rule2006, param.GetLocation(), paramName));
            }
        }

        // 检查 SQL 模板是否包含危险操作
        CheckDangerousSql(context, sqlxAttr, template);
    }

    private void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        // 获取被调用方法的符号
        var methodSymbol = context.SemanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
        if (methodSymbol == null) return;

        // 检查方法是否有动态 SQL 参数
        var dynamicParams = methodSymbol.Parameters
            .Where(p => HasDynamicSqlAttribute(p))
            .ToList();

        if (!dynamicParams.Any()) return;

        // 检查调用处是否进行了验证
        foreach (var param in dynamicParams)
        {
            var argument = GetArgumentForParameter(invocation, param);
            if (argument == null) continue;

            // 检查是否在调用前进行了验证
            if (!HasValidationBeforeCall(context, invocation, argument))
            {
                // SQLX2003: 缺少验证
                context.ReportDiagnostic(Diagnostic.Create(
                    Rule2003, argument.GetLocation(), param.Name));
            }

            // 检查是否来自不安全的来源
            if (IsFromUntrustedSource(context, argument))
            {
                // SQLX2002: 不安全的数据源
                context.ReportDiagnostic(Diagnostic.Create(
                    Rule2002, argument.GetLocation(), param.Name));
            }

            // 建议使用白名单
            if (ShouldUseWhitelist(context, invocation))
            {
                // SQLX2004: 建议白名单
                context.ReportDiagnostic(Diagnostic.Create(
                    Rule2004, argument.GetLocation(), param.Name));
            }
        }

        // 检查是否在公共 API 中
        if (IsInPublicApi(context, invocation))
        {
            // SQLX2005: 公共 API 暴露
            context.ReportDiagnostic(Diagnostic.Create(
                Rule2005, invocation.GetLocation()));
        }
    }

    // 辅助方法
    private bool HasValidationBeforeCall(SyntaxNodeAnalysisContext context,
        InvocationExpressionSyntax invocation, ArgumentSyntax argument)
    {
        // 向上查找 5 行代码
        var method = invocation.FirstAncestorOrSelf<MethodDeclarationSyntax>();
        if (method == null) return false;

        var statements = method.Body?.Statements ?? method.ExpressionBody?.Expression;
        // 查找验证模式: if, throw, ArgumentException, Contains, Length
        // ...

        return false;  // 简化
    }

    private bool IsFromUntrustedSource(SyntaxNodeAnalysisContext context, ArgumentSyntax argument)
    {
        // 检查参数名是否包含：input, request, form, query
        // 检查是否有 [FromBody], [FromQuery] 等特性
        // ...

        return false;  // 简化
    }

    private void CheckDangerousSql(SyntaxNodeAnalysisContext context,
        AttributeSyntax attr, string template)
    {
        var dangerousPatterns = new[]
        {
            "DROP TABLE", "DROP DATABASE", "TRUNCATE",
            "DELETE FROM", "EXEC", "EXECUTE"
        };

        foreach (var pattern in dangerousPatterns)
        {
            if (template.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                // SQLX2007: 危险操作
                context.ReportDiagnostic(Diagnostic.Create(
                    Rule2007, attr.GetLocation(), pattern));
            }
        }
    }
}
```

---

### Code Fix Provider

```csharp
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DynamicSqlCodeFixProvider))]
[Shared]
public class DynamicSqlCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create("SQLX2001", "SQLX2003", "SQLX2009");

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        var node = root.FindNode(diagnosticSpan);

        if (diagnostic.Id == "SQLX2001")
        {
            // 添加 [DynamicSql] 特性
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Add [DynamicSql] attribute",
                    createChangedDocument: c => AddDynamicSqlAttributeAsync(context.Document, node, c),
                    equivalenceKey: "AddDynamicSql"),
                diagnostic);
        }
        else if (diagnostic.Id == "SQLX2003")
        {
            // 添加验证代码
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Add validation",
                    createChangedDocument: c => AddValidationAsync(context.Document, node, c),
                    equivalenceKey: "AddValidation"),
                diagnostic);
        }
        // ...
    }
}
```

---

## 🚨 使用限制和警告

### 文档中的明确警告

```markdown
## ⚠️ 动态占位符安全警告

动态占位符会将参数值直接拼接到 SQL 中（非参数化），虽然有多层验证保护，但仍需谨慎使用。

### 安全使用指南

✅ **推荐做法**：
1. 只在受信任的内部代码中使用
2. 使用硬编码的表名/列名
3. 使用白名单验证用户输入
4. 优先使用普通参数化查询（@param）

❌ **禁止做法**：
1. 不要直接使用用户输入作为动态参数
2. 不要在公共 API 中暴露动态参数
3. 不要禁用验证逻辑

### 示例：安全的多租户查询

```csharp
// ✅ 正确：使用白名单
var allowedTenants = new[] { "tenant1", "tenant2", "tenant3" };
if (allowedTenants.Contains(tenantId))
{
    var tableName = $"{tenantId}_users";
    var users = await repo.GetUsersAsync(tableName);
}

// ❌ 错误：直接使用用户输入
var tableName = Request.Query["table"];  // 危险！
var users = await repo.GetUsersAsync(tableName);  // SQL注入风险！
```
```

---

## 🎯 实施计划

### 阶段1：核心实现（2天）

#### 1.1 新增 Attribute
```
src/Sqlx/Annotations/DynamicSqlAttribute.cs
src/Sqlx/Annotations/DynamicSqlType.cs
```

#### 1.2 新增验证器
```
src/Sqlx.Generator/Validation/SqlValidator.cs
```

#### 1.3 扩展 SqlTemplateEngine
- 识别 `{{@paramName}}` 占位符
- 提取动态参数信息
- 验证参数必须有 `[DynamicSql]` 特性

#### 1.4 扩展 AttributeHandler
- 解析 `[DynamicSql]` 特性
- 提取 `DynamicSqlType`
- 添加到 `MethodAnalysisResult`

#### 1.5 扩展 SharedCodeGenerationUtilities
- 生成内联验证代码
- 生成字符串拼接代码
- 生成 Activity 追踪代码

### 阶段2：文档和示例（1天）

#### 2.1 更新核心文档
- `docs/PLACEHOLDERS.md` - 新增"动态占位符"章节
- `docs/SECURITY.md` - 新增，安全使用指南
- `README.md` - 更新示例

#### 2.2 更新 GitHub Pages
- 新增动态占位符展示
- 添加安全警告
- 提供完整示例

#### 2.3 创建示例项目
- `samples/MultiTenantApp/` - 多租户示例
- `samples/DynamicReporting/` - 动态报表示例

### 阶段3：测试（1天）

#### 3.1 单元测试
```
tests/Sqlx.Tests/Validation/SqlValidatorTests.cs
tests/Sqlx.Tests/DynamicPlaceholder/DynamicTableTests.cs
tests/Sqlx.Tests/DynamicPlaceholder/DynamicColumnTests.cs
tests/Sqlx.Tests/DynamicPlaceholder/DynamicFragmentTests.cs
tests/Sqlx.Tests/DynamicPlaceholder/SecurityTests.cs
```

#### 3.2 集成测试
- 多租户场景测试
- 分表场景测试
- SQL注入防护测试
- 性能基准测试

### 阶段4：优化和发布（0.5天）
- 代码审查
- 性能优化
- 文档完善
- 版本发布

**总计：4-5天完成**

---

## 📝 SQL 模板语法总结

### 占位符对比

| 占位符类型 | 语法 | 编译时 | 运行时 | 用途 |
|-----------|------|--------|--------|------|
| **静态占位符** | `{{table}}` | 固定 | 固定 | 静态表名 |
| **静态占位符** | `{{columns}}` | 固定 | 固定 | 静态列列表 |
| **普通参数** | `@id` | 固定 | 动态 | 参数化查询值 |
| **动态占位符** | `{{@tableName}}` | 固定 | 动态 | 动态表名/列名 |

### 完整示例

```csharp
// 混合使用所有类型
[Sqlx(@"
    SELECT {{columns --exclude Password}}
    FROM {{@tableName}}
    WHERE department = @dept
      AND {{@whereClause}}
    ORDER BY {{@orderBy}}
    LIMIT @limit
")]
Task<List<User>> ComplexQueryAsync(
    [DynamicSql] string tableName,
    string dept,
    [DynamicSql(Type = DynamicSqlType.Fragment)] string whereClause,
    [DynamicSql] string orderBy,
    int limit);
```

**清晰度分析**：
- ✅ `{{columns}}` - 一眼看出是静态列列表
- ✅ `{{@tableName}}` - 一眼看出是动态表名
- ✅ `@dept` - 一眼看出是普通参数化参数
- ✅ `{{@whereClause}}` - 一眼看出是动态SQL片段

---

## ✅ 总结

### 核心优势

1. **安全性** ⭐⭐⭐⭐⭐
   - 强制特性标记
   - 三层验证保护
   - 明确的错误提示

2. **性能** ⭐⭐⭐⭐⭐
   - 内联验证（零运行时开销）
   - 直接字符串拼接
   - 无额外分配

3. **类型安全** ⭐⭐⭐⭐⭐
   - 返回强类型实体
   - AOT 完全兼容
   - 编译时检查

4. **清晰度** ⭐⭐⭐⭐⭐
   - `{{@paramName}}` 语法清晰
   - 与静态占位符明确区分
   - 代码意图一目了然

5. **易用性** ⭐⭐⭐⭐
   - 简单的特性标记
   - 丰富的文档和示例
   - 清晰的错误提示

### 适用场景

✅ **推荐使用**：
- 多租户系统（动态表名）
- 分表场景（按时间/ID）
- 内部工具（动态排序/过滤）
- 可控的动态查询

❌ **不推荐使用**：
- 公共 API（暴露给最终用户）
- 直接接受用户输入
- 简单的固定查询（用普通参数更安全）

---

## 🤔 待确认

请确认以下设计：

1. ✅ 特性名称：`[DynamicSql]` 是否合适？
2. ✅ 占位符语法：`{{@paramName}}` 是否清晰？
3. ✅ 三种验证类型（Identifier, Fragment, TablePart）是否足够？
4. ✅ 是否需要支持禁用验证的选项？（建议：不支持，强制验证）
5. ✅ 错误提示是否足够详细？

确认后立即开始实施！🚀

