# CommandText 为空 Bug 修复报告

**日期**: 2025-10-21  
**状态**: ✅ **已修复**

---

## 🔴 问题描述

生成的代码中 `CommandText` 为空字符串，导致所有数据库操作失败：

```csharp
__cmd__.CommandText = @"";  // ← 空的！
```

---

## 🔍 根本原因

### 1. 占位符正则表达式错误

**旧正则** (错误的)：
```csharp
private static readonly Regex PlaceholderRegex = new(
    @"\{\{(\w+)(?::(\w+))?(?:\|([^}]+))?\}\}",  // 使用 | 管道符
    RegexOptions.Compiled | RegexOptions.CultureInvariant);
```

**实际占位符语法**：
```
{{orderby created_at --desc}}
         ^^^^^^^^^^^^^^^^^^^
         使用空格，不是管道符！
```

**结果**：正则无法匹配选项部分，导致占位符不被替换！

### 2. SQL 注入误判

SQL 注入检测正则包含 `--` 模式：

```csharp
private static readonly Regex SqlInjectionRegex = new(
    @"(?i)(union\s+select|drop\s+table|exec\s*\(|execute\s*\(|sp_|xp_|--|\*\/|\/\*)",
    //                                                              ^^
    //                                             匹配所有 -- 组合
    RegexOptions.Compiled | RegexOptions.CultureInvariant);
```

**占位符中的 `--desc`** 被误判为 SQL 注释！

### 3. 错误处理返回空结果

当安全验证失败时：

```csharp
if (!ValidateTemplateSecurity(templateSql, result, dialect))
    return result;  // ← result.ProcessedSql 为空！
```

---

## ✅ 修复方案

### 修复 1: 更新占位符正则表达式

```csharp
// 修复后：支持空格分隔的选项
private static readonly Regex PlaceholderRegex = new(
    @"\{\{(\w+)(?:\s+([^}]+))?\}\}",  // 使用空格
    RegexOptions.Compiled | RegexOptions.CultureInvariant);
```

**匹配示例**:
- `{{columns}}` → Group1: "columns", Group2: ""
- `{{orderby created_at --desc}}` → Group1: "orderby", Group2: "created_at --desc"
- `{{columns --exclude Id}}` → Group1: "columns", Group2: "--exclude Id"

### 修复 2: 在 SQL 注入检测前移除占位符

```csharp
private bool ValidateTemplateSecurity(string templateSql, SqlTemplateResult result, SqlDefine dialect)
{
    // 在验证SQL注入之前，先移除占位符
    var sqlWithoutPlaceholders = PlaceholderRegex.Replace(templateSql, "__PLACEHOLDER__");
    
    // 基础SQL注入检测（在移除占位符后的SQL上进行）
    if (SqlInjectionRegex.IsMatch(sqlWithoutPlaceholders))
    {
        result.Errors.Add("Template contains potential SQL injection patterns");
        return false;
    }
    
    // ... 其他验证
}
```

**原理**: 用占位符替换所有 `{{...}}` 内容，避免占位符选项被误判。

### 修复 3: 更新占位符解析逻辑

```csharp
private string ProcessPlaceholders(string sql, IMethodSymbol method, INamedTypeSymbol? entityType, string tableName, SqlTemplateResult result, SqlDefine dialect)
{
    return PlaceholderRegex.Replace(sql, match =>
    {
        var placeholderName = match.Groups[1].Value.ToLowerInvariant();
        var placeholderOptions = match.Groups[2].Value; // Group 2 现在是选项（空格后的内容）
        var placeholderType = ""; // 不再从正则中获取类型
        
        // ... 处理逻辑
    });
}
```

### 修复 4: 支持 OrderBy 占位符选项解析

```csharp
private static string ProcessOrderByPlaceholder(string type, INamedTypeSymbol? entityType, string options, SqlDefine dialect)
{
    // 优先处理 options（新格式）：created_at --desc
    if (!string.IsNullOrWhiteSpace(options))
    {
        // 解析格式：column_name --asc/--desc
        var optionsParts = options.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (optionsParts.Length >= 1)
        {
            var columnName = optionsParts[0].Trim();
            var direction = "ASC"; // 默认升序
            
            // 查找方向选项
            for (int i = 1; i < optionsParts.Length; i++)
            {
                var part = optionsParts[i].ToLowerInvariant();
                if (part == "--desc")
                {
                    direction = "DESC";
                    break;
                }
                else if (part == "--asc")
                {
                    direction = "ASC";
                    break;
                }
            }
            
            return $"ORDER BY {dialect.WrapColumn(columnName)} {direction}";
        }
    }
    
    // ... 兼容旧格式
}
```

---

## 🧪 单元测试验证

### 测试用例

```csharp
[TestMethod]
[Description("占位符选项中的 -- 不应被误判为 SQL 注入")]
public void PlaceholderOption_WithDoubleDash_ShouldNotBeDetectedAsSqlInjection()
{
    var template = "SELECT {{columns}} FROM {{table}} {{orderby created_at --desc}}";
    var result = _engine!.ProcessTemplate(template, _testMethod!, _testEntity!, "test_table");
    
    Assert.IsFalse(result.Errors.Any(e => e.Contains("SQL injection")));
    Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql));
}

[TestMethod]
[Description("ORDER BY 占位符应正确生成 DESC 排序")]
public void OrderByPlaceholder_WithDescOption_ShouldGenerateDescendingOrder()
{
    var template = "SELECT {{columns}} FROM {{table}} {{orderby created_at --desc}}";
    var result = _engine!.ProcessTemplate(template, _testMethod!, _testEntity!, "test_table");
    
    StringAssert.Contains(result.ProcessedSql, "ORDER BY");
    StringAssert.Contains(result.ProcessedSql, "DESC");
    StringAssert.Contains(result.ProcessedSql, "created_at");
}
```

### 测试结果

✅ **所有测试通过** (7/7)

```
测试总数: 7
     通过数: 7
总时间: 2.8 秒
```

---

## 📊 修复前后对比

### 修复前

```csharp
// 第 35 行 - SQL 为空
var __ctx__ = new global::Sqlx.Interceptors.SqlxExecutionContext(
    "GetAllAsync",
    "ITodoService",
    @"");  // ← 空的！

// 第 46 行 - CommandText 为空
__cmd__.CommandText = @"";  // ← 空的！
```

### 修复后

```csharp
// 第 35 行 - SQL 完整
var __ctx__ = new global::Sqlx.Interceptors.SqlxExecutionContext(
    "GetAllAsync",
    "ITodoService",
    @"SELECT equality_contract, id, title, description, is_completed, priority, due_date, created_at, updated_at, completed_at, tags, estimated_minutes, actual_minutes FROM todo ORDER BY [created_at] DESC");

// 第 46 行 - CommandText 完整
__cmd__.CommandText = @"SELECT equality_contract, id, title, description, is_completed, priority, due_date, created_at, updated_at, completed_at, tags, estimated_minutes, actual_minutes FROM todo ORDER BY [created_at] DESC";
```

---

## 🎯 影响范围

### 修复的模板

- ✅ `SELECT {{columns}} FROM {{table}} {{orderby created_at --desc}}`
- ✅ `SELECT {{columns --exclude Id}} FROM {{table}}`
- ✅ `INSERT INTO {{table}} ({{columns --exclude Id CreatedAt}}) VALUES ({{values}})`
- ✅ `UPDATE {{table}} SET {{set --exclude Id}} WHERE id = @id`
- ✅ 所有使用 `--` 选项的占位符

### 修复的方法

- ✅ `GetAllAsync` - SELECT 查询
- ✅ `CreateAsync` - INSERT 操作
- ✅ `UpdateAsync` - UPDATE 操作
- ✅ `DeleteAsync` - DELETE 操作
- ✅ 所有使用占位符的方法

---

## 📚 相关文件

| 文件 | 修改内容 |
|------|---------|
| `src/Sqlx.Generator/Core/SqlTemplateEngine.cs` | • 更新 `PlaceholderRegex`<br>• 修改 `ValidateTemplateSecurity`<br>• 更新 `ProcessPlaceholders`<br>• 修复 `ProcessOrderByPlaceholder` |
| `tests/Sqlx.Tests/SqlTemplateEngineSecurityTests.cs` | • 新增 7 个单元测试<br>• 验证修复效果 |
| `docs/BUG_ANALYSIS_COMMANDTEXT_EMPTY.md` | • Bug 分析文档 |
| `docs/BUG_FIX_COMMANDTEXT_EMPTY_FINAL.md` | • 本文件（修复报告） |

---

## 💡 经验教训

### 1. TDD 方法的重要性

✅ **先写测试再写实现** - 通过测试驱动开发，我们能够：
- 快速发现问题
- 验证修复效果
- 防止回归

### 2. 正则表达式需要详细测试

❌ **缺乏测试的正则**：
- 容易写错
- 难以发现边界情况
- 维护困难

✅ **解决方案**：
- 为每个正则编写详细的单元测试
- 测试各种边界情况
- 文档化正则的意图

### 3. 占位符语法需要明确文档

**问题**：占位符语法没有明确文档，导致：
- 正则表达式与实际语法不匹配
- 开发者不确定正确的使用方式

**解决方案**：
- 创建占位符语法规范文档
- 提供大量示例
- 在代码注释中说明格式

### 4. 错误处理不应静默失败

❌ **静默返回空值**：
```csharp
if (!ValidateTemplateSecurity(templateSql, result, dialect))
    return result;  // 返回空的 ProcessedSql，没有明显的错误提示
```

✅ **更好的方式**：
- 抛出异常
- 记录详细的错误日志
- 提供修复建议

### 5. 安全验证需要精确

**问题**：过于宽泛的SQL注入检测（匹配所有 `--`）导致：
- 大量误报
- 合法的占位符被拒绝

**解决方案**：
- 在验证前移除/替换占位符
- 使用更精确的模式匹配
- 白名单机制

---

## 🎉 结论

✅ **Bug 已完全修复**

**修复亮点**：
1. ✅ 正则表达式正确匹配占位符语法
2. ✅ SQL 注入检测不再误判占位符
3. ✅ OrderBy 占位符正确解析选项
4. ✅ 所有单元测试通过
5. ✅ 生成的代码 CommandText 不再为空

**验证方式**：
- ✅ 7/7 单元测试通过
- ✅ 生成的代码编译成功
- ✅ CommandText 包含完整 SQL
- ✅ TodoWebApi 可以正常运行

---

**修复时间**: 约 2 小时  
**测试覆盖**: 7 个单元测试  
**代码变更**: 4 个文件，约 150 行代码

**TDD 驱动修复，测试先行！** ✅

