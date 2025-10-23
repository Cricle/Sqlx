# Sqlx 性能优化策略

## 🎯 核心原则

### 明确优化边界 ⭐

| 代码类型 | 执行频率 | 优化重点 | 复杂度要求 |
|---------|---------|---------|-----------|
| **源生成器代码** | 编译时 1 次 | ❌ **不需优化** | 简单清晰优先 |
| **生成的代码** | 运行时每次 | ✅ **必须优化** | 高性能、低 GC |
| **主库代码** | 运行时热路径 | ✅ **必须优化** | 高性能、低 GC |

---

## 📁 代码分类

### 1. 源生成器代码（Sqlx.Generator）

**位置**：
- `src/Sqlx.Generator/**/*.cs`

**特点**：
- ✅ 编译时执行一次
- ✅ 不影响运行时性能
- ✅ 代码可维护性优先

**优化原则**：
```csharp
// ❌ 错误：过度优化源生成器
private void GenerateCode_Optimized()
{
    Span<char> buffer = stackalloc char[256];  // 没必要
    ReadOnlySpan<char> span = text.AsSpan();   // 没意义
    // 编译时只运行一次，这些优化完全浪费！
}

// ✅ 正确：简单清晰
private void GenerateCode()
{
    StringBuilder sb = new();
    sb.AppendLine($"    if ({param}.Length > 128)");
    sb.AppendLine($"        throw new ArgumentException(...);");
    // 简单、清晰、易维护！
}
```

**允许的做法**：
- ✅ 使用 `StringBuilder` 拼接代码
- ✅ 使用 `foreach` 遍历（清晰）
- ✅ 使用 `LINQ` 查询（可读性）
- ✅ 使用正则表达式解析（方便）

**禁止的做法**：
- ❌ 使用 `Span<T>` / `stackalloc`（没意义）
- ❌ 使用 `AggressiveInlining`（无效）
- ❌ 手动优化字符串操作（浪费时间）
- ❌ 缓存计算结果（编译时只运行一次）

---

### 2. 生成的代码（用户项目中）

**位置**：
- 生成到用户项目的 `.g.cs` 文件

**特点**：
- ✅ 运行时每次调用都执行
- ✅ 性能直接影响用户体验
- ✅ 是优化的核心重点

**优化原则**：
```csharp
// ✅ 优化后的生成代码
public async Task<User?> GetFromTableAsync(string tableName, int id)
{
    // 内联验证（编译器完全优化）
    if (tableName.Length == 0 || tableName.Length > 128)
        throw new ArgumentException("Invalid table name length", nameof(tableName));

    if (!char.IsLetter(tableName[0]) && tableName[0] != '_')
        throw new ArgumentException("Table name must start with letter or underscore", nameof(tableName));

    if (tableName.Contains("DROP", StringComparison.OrdinalIgnoreCase) ||
        tableName.Contains("--") ||
        tableName.Contains("/*"))
        throw new ArgumentException("Invalid table name", nameof(tableName));

    // 直接拼接 SQL（高性能）
    var sql = $"SELECT id, name, email FROM {tableName} WHERE id = @id";

    // Activity 跟踪（内联）
    using var activity = SqlxActivitySource.Instance.StartActivity("GetFromTableAsync");
    activity?.SetTag("db.table", tableName);

    // 执行查询（直接使用序号访问）
    using var connection = new SqlConnection(_connectionString);
    await connection.OpenAsync();
    using var command = new SqlCommand(sql, connection);
    command.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = id });

    using var reader = await command.ExecuteReaderAsync();
    if (await reader.ReadAsync())
    {
        return new User
        {
            Id = reader.GetInt32(0),      // 硬编码序号（最快）
            Name = reader.GetString(1),
            Email = reader.GetString(2)
        };
    }
    return null;
}
```

**必须的优化**：
- ✅ **内联验证** - 零函数调用开销
- ✅ **硬编码序号** - `reader.GetInt32(0)` 而不是 `GetOrdinal("id")`
- ✅ **直接 SQL 拼接** - 字符串插值（编译器优化）
- ✅ **常量折叠** - 编译器优化常量比较
- ✅ **IsDBNull 检查** - 仅对可空类型
- ✅ **Activity 内联** - 直接生成跟踪代码

**禁止的做法**：
- ❌ 调用外部验证方法（有调用开销）
- ❌ 使用 `GetOrdinal` 动态查找（慢 10x）
- ❌ 为所有列做 `IsDBNull` 检查（浪费）
- ❌ 使用反射或动态代码（AOT 不兼容）

---

### 3. 主库代码（Sqlx 核心库）

**位置**：
- `src/Sqlx/**/*.cs`

**特点**：
- ✅ 运行时被生成的代码调用
- ✅ 是热路径，影响整体性能
- ✅ 需要高性能优化

**优化原则**：
```csharp
namespace Sqlx.Validation;

using System;
using System.Runtime.CompilerServices;

/// <summary>
/// 运行时验证器（可选使用）
/// </summary>
public static class SqlValidator
{
    /// <summary>
    /// 验证标识符 - 零 GC 版本
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValidIdentifier(ReadOnlySpan<char> identifier)
    {
        if (identifier.Length == 0 || identifier.Length > 128)
            return false;

        // 手动字符检查（快）
        char first = identifier[0];
        if (!((first >= 'a' && first <= 'z') || (first >= 'A' && first <= 'Z') || first == '_'))
            return false;

        for (int i = 1; i < identifier.Length; i++)
        {
            char c = identifier[i];
            if (!((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_'))
                return false;
        }

        return true;
    }

    /// <summary>
    /// 检查危险关键字
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ContainsDangerousKeyword(ReadOnlySpan<char> text)
    {
        // 常量比较（编译器优化）
        return text.Contains("DROP", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("TRUNCATE", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("--", StringComparison.Ordinal) ||
               text.Contains("/*", StringComparison.Ordinal);
    }
}
```

**必须的优化**：
- ✅ 使用 `ReadOnlySpan<char>` 参数（零拷贝）
- ✅ 使用 `AggressiveInlining`（消除调用开销）
- ✅ 手动字符检查（比正则快 5x）
- ✅ 避免字符串分配（`ToUpperInvariant` 等）
- ✅ 常量比较（编译器优化）

---

## 🎯 实际优化重点对比

### 动态占位符功能示例

#### 源生成器代码（简单即可）
```csharp
// Sqlx.Generator 中的代码生成
private string GenerateDynamicSqlMethod(MethodInfo method, string template)
{
    StringBuilder sb = new();

    // 简单的字符串拼接
    sb.AppendLine($"public async Task<{returnType}> {methodName}({parameters})");
    sb.AppendLine("{");

    // 查找动态参数
    foreach (var param in method.Parameters)
    {
        if (HasDynamicSqlAttribute(param))  // 简单的检查即可
        {
            // 生成验证代码（字符串拼接）
            sb.AppendLine($"    if ({param.Name}.Length > 128)");
            sb.AppendLine($"        throw new ArgumentException(\"Invalid length\", nameof({param.Name}));");
        }
    }

    // 替换占位符
    string sql = template.Replace("{{@tableName}}", $"{{{tableName}}}");
    sb.AppendLine($"    var sql = $\"{sql}\";");

    // 生成执行代码
    sb.AppendLine("    // ... 执行 SQL");
    sb.AppendLine("}");

    return sb.ToString();
}
```

#### 生成的代码（高性能优化）
```csharp
// 生成到用户项目中的代码 - 必须高性能
public async Task<User?> GetFromTableAsync(string tableName, int id)
{
    // ✅ 内联验证（编译器完全优化）
    if (tableName.Length > 128)
        throw new ArgumentException("Invalid table name length", nameof(tableName));

    if (!char.IsLetter(tableName[0]) && tableName[0] != '_')
        throw new ArgumentException("Table name must start with letter", nameof(tableName));

    if (tableName.Contains("DROP", StringComparison.OrdinalIgnoreCase))
        throw new ArgumentException("Invalid table name", nameof(tableName));

    // ✅ 直接拼接（字符串驻留）
    var sql = $"SELECT id, name, email FROM {tableName} WHERE id = @id";

    // ✅ Activity 内联
    using var activity = SqlxActivitySource.Instance.StartActivity("GetFromTableAsync");
    activity?.SetTag("db.table", tableName);

    // ✅ 硬编码序号访问
    // ... reader.GetInt32(0), reader.GetString(1) ...
}
```

---

## 📊 性能影响对比

| 优化点 | 源生成器优化 | 生成代码优化 | 性能影响 |
|-------|------------|------------|---------|
| 使用 Span | 编译时 -0.1ms | 运行时 -0.5μs × 1M 次 | **生成代码影响大 500,000 倍** |
| AggressiveInlining | 无效 | 运行时 -0.2μs × 1M 次 | **生成代码影响大** |
| 字符串优化 | 编译时 -0.05ms | 运行时 -0.3μs × 1M 次 | **生成代码影响大 300,000 倍** |
| 代码清晰度 | 维护性重要 | 性能优先 | **不同优先级** |

**结论**：
- ✅ 源生成器即使慢 10 倍，总编译时间只增加几毫秒
- ✅ 生成代码慢 0.1μs，执行 100 万次就是 100ms 差异
- ✅ **优化重点必须放在生成的代码上！**

---

## 🚀 实施指南

### 阶段 1：审查现有代码
1. 检查 Sqlx.Generator - **移除不必要的性能优化**
2. 检查生成的代码模板 - **确保生成高性能代码**
3. 检查 Sqlx 核心库 - **确保热路径优化**

### 阶段 2：重构优化
1. 简化源生成器代码（提高可维护性）
2. 优化生成代码模板（内联、硬编码序号、零 GC）
3. 优化主库验证方法（Span、AggressiveInlining）

### 阶段 3：性能验证
1. Benchmark 测试生成的代码
2. 对比 Dapper / EF Core
3. 确保零 GC 压力

---

## ✅ 检查清单

### 源生成器代码（Sqlx.Generator）
- [ ] 代码简单清晰，易于维护
- [ ] 使用 StringBuilder 拼接代码
- [ ] 不使用 Span/stackalloc/AggressiveInlining
- [ ] 注释充分，逻辑清晰

### 生成的代码（.g.cs）
- [ ] 验证逻辑完全内联
- [ ] 使用硬编码序号访问（`reader.GetInt32(0)`）
- [ ] 仅对可空类型做 `IsDBNull` 检查
- [ ] Activity 跟踪内联
- [ ] 零额外函数调用

### 主库代码（Sqlx）
- [ ] 热路径方法使用 `AggressiveInlining`
- [ ] 参数使用 `ReadOnlySpan<char>`
- [ ] 避免字符串分配
- [ ] 手动字符检查（避免正则）

---

## 📝 总结

**核心原则**：
1. ✅ **源生成器** - 简单清晰优先，无需性能优化
2. ✅ **生成的代码** - 必须高性能，零 GC，内联验证
3. ✅ **主库代码** - 热路径优化，Span，AggressiveInlining

**不要过度优化源生成器！它只在编译时运行一次，优化它没有任何实际意义。**

**重点优化生成的代码！它在运行时每次调用都执行，性能影响放大百万倍。**

