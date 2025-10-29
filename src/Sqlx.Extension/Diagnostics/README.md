# Sqlx Diagnostics (诊断和代码修复)

> **功能**: 实时代码分析和自动修复
> **状态**: ✅ 实现完成
> **优先级**: P0

---

## 📋 功能概述

Diagnostics 提供了基于 Roslyn 的实时代码分析功能，帮助开发者在编码时发现和修复 SqlTemplate 相关的问题。

---

## 🔍 诊断规则

### SQLX001: SQL 参数未找到

**严重性**: ❌ Error

**描述**: SQL 模板中使用的参数（如 `@userId`）在方法参数列表中找不到

**示例**:

```csharp
// ❌ 错误 - SQL中使用了@userId，但方法参数中没有
[SqlTemplate("SELECT * FROM users WHERE id = @userId")]
Task<User?> GetUserAsync(long id);  // ← 应该是 userId 或修改 SQL

// ✅ 正确
[SqlTemplate("SELECT * FROM users WHERE id = @id")]
Task<User?> GetUserAsync(long id);
```

**自动修复**:
- 添加缺失的参数到方法签名

---

### SQLX002: 方法参数未使用

**严重性**: ⚠️ Warning

**描述**: 方法参数在 SQL 模板中没有被使用

**示例**:

```csharp
// ⚠️ 警告 - userName参数没有在SQL中使用
[SqlTemplate("SELECT * FROM users WHERE id = @id")]
Task<User?> GetUserAsync(long id, string userName);  // ← userName 未使用

// ✅ 正确
[SqlTemplate("SELECT * FROM users WHERE id = @id")]
Task<User?> GetUserAsync(long id);
```

**自动修复**:
- 移除未使用的参数

**例外情况**:
- `CancellationToken` 参数（系统参数）
- `DbTransaction` 参数（系统参数）
- 实体类型参数（用于 `{{values}}`, `{{set}}` 等）
- 集合类型参数（用于批量操作）

---

### SQLX003: 参数类型可能不适合

**严重性**: ⚠️ Warning

**描述**: 参数类型可能不适合 SQL 操作

**示例**:

```csharp
// ⚠️ 警告 - ComplexObject 可能不适合直接用于SQL参数
public class ComplexObject
{
    public Dictionary<string, object> Data { get; set; }
    public Action Handler { get; set; }
}

[SqlTemplate("SELECT * FROM users WHERE data = @data")]
Task<List<User>> QueryAsync(ComplexObject data);  // ← 复杂类型

// ✅ 正确 - 使用简单类型
[SqlTemplate("SELECT * FROM users WHERE id = @id")]
Task<List<User>> QueryAsync(long id);
```

**SQL 友好的类型**:
- 基本类型: `int`, `long`, `bool`, `string`, `decimal`, etc.
- 日期时间: `DateTime`, `DateTimeOffset`, `TimeSpan`
- 其他: `Guid`, `byte[]`, `enum`
- 可空类型: `int?`, `long?`, etc.
- 实体类 (用于 {{values}}, {{set}})
- 集合类型 (用于批量操作)

---

## 🛠️ 自动修复功能

### 1. 添加缺失参数

**触发**: 光标在错误位置 → 按 `Ctrl+.` 或点击灯泡图标

**操作**: "Add parameter 'parameterName'"

**效果**:

```csharp
// Before
[SqlTemplate("WHERE id = @userId")]
Task<User?> GetUserAsync(long id);

// After - 自动添加参数
[SqlTemplate("WHERE id = @userId")]
Task<User?> GetUserAsync(long id, object userId);  // ← 自动添加
```

**注意**: 自动添加的参数类型默认为 `object`，需要手动修改为正确类型。

---

### 2. 移除未使用参数

**触发**: 光标在警告位置 → 按 `Ctrl+.`

**操作**: "Remove unused parameter"

**效果**:

```csharp
// Before
[SqlTemplate("WHERE id = @id")]
Task<User?> GetUserAsync(long id, string unused);

// After - 自动移除
[SqlTemplate("WHERE id = @id")]
Task<User?> GetUserAsync(long id);  // ← unused 被移除
```

---

## 🎯 使用场景

### 场景 1: 参数名不匹配

```csharp
// 问题：SQL 使用 @userId，方法参数是 id
[SqlTemplate("SELECT * FROM users WHERE id = @userId")]
Task<User?> GetUserAsync(long id);
//                            ^^ 下划线标记为错误

// 解决方案1：修改方法参数名
Task<User?> GetUserAsync(long userId);

// 解决方案2：修改 SQL 参数名
[SqlTemplate("SELECT * FROM users WHERE id = @id")]

// 解决方案3：使用自动修复添加参数
// 按 Ctrl+. → "Add parameter 'userId'"
```

### 场景 2: 多余的参数

```csharp
// 问题：filter 参数未使用
[SqlTemplate("SELECT * FROM products WHERE category_id = @categoryId")]
Task<List<Product>> GetByCategoryAsync(int categoryId, string filter);
//                                                              ^^^^^^ 警告

// 解决方案：移除未使用的参数
// 按 Ctrl+. → "Remove unused parameter"
```

### 场景 3: 批量操作

```csharp
// ✅ 正确 - 集合参数用于批量操作
[SqlTemplate("INSERT INTO {{table}} {{batch_values}}")]
Task<int> BatchInsertAsync(IEnumerable<User> users);
//                                           ^^^^^ 不会警告（用于 batch_values）
```

### 场景 4: 实体参数

```csharp
// ✅ 正确 - 实体参数用于 {{set}}
[SqlTemplate("UPDATE {{table}} {{set}} WHERE id = @id")]
Task<int> UpdateAsync(User user);
//                         ^^^^ 不会警告（用于 {{set}}）
```

---

## 🔧 技术实现

### 架构

```
Diagnostics/
├── SqlTemplateParameterAnalyzer.cs       // 诊断分析器
├── SqlTemplateParameterCodeFixProvider.cs // 代码修复提供者
└── README.md                             // 本文档
```

### 关键技术

#### 1. Roslyn Analyzer

```csharp
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class SqlTemplateParameterAnalyzer : DiagnosticAnalyzer
{
    public override void Initialize(AnalysisContext context)
    {
        // 注册语法节点分析
        context.RegisterSyntaxNodeAction(
            AnalyzeMethodDeclaration,
            SyntaxKind.MethodDeclaration);
    }
}
```

#### 2. 参数提取

```csharp
// 从 SQL 模板提取参数
private static readonly Regex SqlParameterRegex =
    new Regex(@"@([a-zA-Z_][a-zA-Z0-9_]*)", RegexOptions.Compiled);

var sqlParameters = SqlParameterRegex.Matches(sqlTemplate)
    .Select(m => m.Groups[1].Value)
    .ToImmutableHashSet();
```

#### 3. 参数匹配

```csharp
// 检查 SQL 参数是否在方法参数中
foreach (var sqlParam in sqlParameters)
{
    var matchingParam = methodParameters.FirstOrDefault(mp =>
        mp.Identifier.Text.Equals(sqlParam, StringComparison.OrdinalIgnoreCase));

    if (matchingParam == null)
    {
        // 报告错误
        context.ReportDiagnostic(...);
    }
}
```

#### 4. 特殊参数识别

```csharp
private bool IsSpecialParameter(ParameterSyntax parameter)
{
    var typeName = GetTypeName(parameter);

    // 忽略系统参数
    return typeName.Contains("CancellationToken") ||
           typeName.Contains("DbTransaction");
}

private bool IsEntityParameter(ParameterSyntax parameter)
{
    // 实体类用于 {{values}}, {{set}}
    return IsCustomClass(parameter);
}
```

---

## 📊 诊断规则详情

### SQLX001 详细说明

**触发条件**:
1. 方法有 `[SqlTemplate]` 属性
2. SQL 模板中使用了 `@paramName`
3. 方法参数列表中找不到匹配的参数

**不触发情况**:
- 参数名大小写不同（会匹配）
- SQL 中没有使用参数

**修复选项**:
- 添加缺失的参数（自动）
- 修改 SQL 参数名（手动）
- 修改方法参数名（手动）

---

### SQLX002 详细说明

**触发条件**:
1. 方法有参数
2. 参数不在 SQL 模板中
3. 参数不是特殊参数
4. 参数不是实体参数

**不触发情况**:
- `CancellationToken` 参数
- `DbTransaction` 参数
- 实体类参数（用于占位符）
- 集合参数（用于批量操作）
- SQL 模板包含 `{{values}}` 或 `{{set}}`

**修复选项**:
- 移除未使用的参数（自动）
- 在 SQL 中使用参数（手动）

---

### SQLX003 详细说明

**触发条件**:
1. 参数类型不在 SQL 友好类型列表中
2. 不是实体类
3. 不是集合类型

**常见问题类型**:
- 复杂对象（Dictionary, Tuple）
- 委托类型（Action, Func）
- 接口类型（非数据接口）

**修复建议**:
- 使用简单类型
- 提取需要的属性作为参数
- 使用实体类

---

## 💡 最佳实践

### 1. 参数命名

✅ **推荐**: 使用一致的命名

```csharp
// Good - 方法参数名和SQL参数名一致
[SqlTemplate("WHERE user_id = @userId")]
Task GetUserDataAsync(long userId);
```

❌ **不推荐**: 不一致的命名

```csharp
// Bad - 容易混淆
[SqlTemplate("WHERE user_id = @id")]
Task GetUserDataAsync(long userId);  // ← 参数名不匹配
```

### 2. 参数类型

✅ **推荐**: 使用SQL友好类型

```csharp
// Good
Task QueryAsync(int id, string name, DateTime date);
```

❌ **不推荐**: 复杂类型

```csharp
// Bad - 除非是实体或批量操作
Task QueryAsync(Dictionary<string, object> filters);
```

### 3. 实体参数

✅ **推荐**: 明确的实体参数

```csharp
// Good - 实体参数用于更新
[SqlTemplate("UPDATE {{table}} {{set}}")]
Task UpdateAsync(User user);
```

### 4. 特殊参数

✅ **推荐**: 使用标准命名

```csharp
// Good - 标准参数名
Task GetAsync(long id, CancellationToken ct = default);
```

---

## 🎯 性能考虑

### 分析性能

- **触发时机**: 编辑代码时实时分析
- **分析时间**: < 50ms per file
- **内存占用**: 最小（增量分析）

### 优化措施

1. **缓存**: 缓存分析结果
2. **增量**: 只分析修改的方法
3. **并发**: 支持并发分析
4. **过滤**: 只分析有 SqlTemplate 的方法

---

## 🐛 已知限制

### 1. 默认参数类型

**问题**: 自动添加的参数类型默认为 `object`
**解决**: 手动修改为正确类型
**影响**: 低（一次性修改）

### 2. 动态SQL

**问题**: 不支持字符串插值的SQL
**解决**: 使用占位符代替
**影响**: 低（不推荐动态SQL）

### 3. 复杂类型检测

**问题**: 可能误报某些自定义类型
**解决**: 抑制特定警告
**影响**: 低（罕见情况）

---

## 📈 使用统计（预期）

| 指标 | 目标值 |
|------|--------|
| **错误检测率** | 95%+ |
| **误报率** | < 5% |
| **修复成功率** | 90%+ |
| **用户满意度** | 85%+ |

---

## 🔮 未来改进

### 短期（v0.6.0）

- [ ] 更智能的类型推断
- [ ] 自定义诊断规则配置
- [ ] 更多自动修复选项

### 中期（v0.7.0）

- [ ] SQL 语法检查
- [ ] 参数类型验证
- [ ] 占位符验证

### 长期（v0.8.0+）

- [ ] 数据库模式验证
- [ ] 性能建议
- [ ] 安全性检查

---

## 📞 相关资源

### 代码

- [SqlTemplateParameterAnalyzer.cs](SqlTemplateParameterAnalyzer.cs)
- [SqlTemplateParameterCodeFixProvider.cs](SqlTemplateParameterCodeFixProvider.cs)

### 文档

- [VS Extension Plan](../../../docs/VSCODE_EXTENSION_PLAN.md)
- [Build Instructions](../BUILD.md)

### 外部链接

- [Roslyn Analyzers](https://docs.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/tutorials/how-to-write-csharp-analyzer-code-fix)
- [Diagnostic Severity](https://docs.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.diagnosticseverity)

---

**状态**: ✅ 实现完成
**版本**: 0.5.0-dev
**最后更新**: 2025-10-29

