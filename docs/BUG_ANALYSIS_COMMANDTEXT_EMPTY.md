# CommandText 为空 Bug 分析报告

**日期**: 2025-10-21  
**状态**: 🔍 调查中

---

## 🔴 问题描述

生成的代码中 `CommandText` 为空字符串：

```csharp
__cmd__.CommandText = @"";  // ← 空的！
```

导致数据库操作失败。

---

## 🔍 根本原因分析

### 1. SQL 注入误判

**问题**: `SqlTemplateEngine` 的安全验证逻辑将占位符选项中的 `--` 误判为 SQL 注释：

```csharp
// SQL模板
"SELECT {{columns}} FROM {{table}} {{orderby created_at --desc}}"
//                                                          ^^
//                                             这里的 -- 被误判为 SQL 注释！
```

### 2. SQL 注入检测正则

```csharp
private static readonly Regex SqlInjectionRegex = new(
    @"(?i)(union\s+select|drop\s+table|exec\s*\(|execute\s*\(|sp_|xp_|--|\*\/|\/\*)", 
    //                                                              ^^
    //                                                   匹配所有 -- 组合
    RegexOptions.Compiled | RegexOptions.CultureInvariant);
```

### 3. 错误处理逻辑

当安全验证失败时，`ProcessTemplate` 返回空的 `SqlTemplateResult`：

```csharp
// src/Sqlx.Generator/Core/SqlTemplateEngine.cs:92-93
if (!ValidateTemplateSecurity(templateSql, result, dialect))
    return result;  // ← result.ProcessedSql 是空的！
```

---

## ✅ 修复方案

### 方案 1: 在验证前移除占位符（已实施）

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

**原理**: 在 SQL 注入检测之前，用占位符替换所有 `{{...}}` 内容，避免占位符选项被误判。

---

## 🧪 单元测试验证

### 测试用例

```csharp
[TestMethod]
[Description("占位符选项中的 -- 不应被误判为 SQL 注入")]
public void PlaceholderOption_WithDoubleDash_ShouldNotBeDetectedAsSqlInjection()
{
    // Arrange
    var template = "SELECT {{columns}} FROM {{table}} {{orderby created_at --desc}}";
    
    // Act
    var result = _engine!.ProcessTemplate(template, _testMethod!, _testEntity!, "test_table");
    
    // Assert
    Assert.IsFalse(result.Errors.Any(e => e.Contains("SQL injection")));
    Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql));
}
```

### 测试结果（修复前）

```
❌ 失败: 4/7
- ProcessedSql 为空
- 占位符中的 -- 仍被误判为 SQL 注入
```

**问题**: 修复未生效，需要进一步调查。

---

## 🔬 进一步调查

### 可能原因

1. ✅ **测试使用了旧的 DLL** - 需要清理并重新编译
2. ❓ **PlaceholderRegex 模式不正确** - 需要验证正则表达式
3. ❓ **占位符替换逻辑有问题** - 需要调试验证

### PlaceholderRegex 验证

```csharp
// 当前正则
private static readonly Regex PlaceholderRegex = new(
    @"\{\{(\w+)(?::(\w+))?(?:\|([^}]+))?\}\}", 
    RegexOptions.Compiled | RegexOptions.CultureInvariant);
```

**测试模式**:
- `{{columns}}` ✅ 应匹配
- `{{orderby created_at --desc}}` ❓ 需要验证
  - `\w+` 匹配 `orderby`
  - `(?::(\w+))?` 可选类型
  - `(?:\|([^}]+))?` 可选选项 - **问题**：`[^}]` 不匹配空格后的内容！

### 🔴 发现新问题！

PlaceholderRegex 的选项部分使用了 `\|` 管道符，但占位符语法使用空格：

```
{{orderby created_at --desc}}
         ^^^^^^^^^^^^^^^^^^^
         这部分应该被捕获，但正则可能不匹配！
```

**正确的占位符语法应该是**:
- `{{placeholder}}` - 无选项
- `{{placeholder|option}}` - 有选项（使用管道符）
- `{{placeholder option}}` - 有选项（使用空格）？

需要检查实际的占位符语法规范。

---

## 🎯 下一步行动

1. ✅ 清理编译缓存并重新编译
2. ✅ 运行单元测试验证修复
3. 🔄 检查 PlaceholderRegex 是否正确匹配所有占位符格式
4. 🔄 如果正则不匹配，更新正则表达式或占位符语法
5. 🔄 验证生成的代码 CommandText 不为空

---

## 📊 影响范围

**影响的方法**:
- ✅ `GetAllAsync` - SELECT 查询
- ✅ `CreateAsync` - INSERT 操作  
- ✅ `UpdateAsync` - UPDATE 操作
- ✅ `DeleteAsync` - DELETE 操作
- ✅ 所有使用占位符选项的方法

**严重性**: 🔴 **致命** - 导致所有数据库操作失败

---

## 💡 经验教训

1. **正则表达式测试**: 所有正则表达式都应该有详细的单元测试
2. **错误处理**: 验证失败时应该抛出异常而不是静默返回空值
3. **TDD 方法**: 先写测试再写实现，可以更早发现问题
4. **占位符语法**: 需要明确文档化占位符的语法规范

---

**待续...** 继续调查 PlaceholderRegex 匹配问题

