# 🚀 Sqlx 增强功能实现计划

**日期**: 2024-10-23  
**版本**: v1.1.0 计划

---

## 📋 需求概述

### 1. **--regex 正则表达式筛选列名**
允许使用正则表达式动态筛选列名，适用于大表或动态列场景。

**示例**:
```csharp
// 只选择以 "user_" 开头的列
SELECT {{columns --regex ^user_}} FROM {{table}}

// 排除以 "_id" 结尾的列
SELECT {{columns --regex ^(?!.*_id$)}} FROM {{table}}
```

---

### 2. **动态返回值 List<Dictionary<string, object>>**
支持返回动态列结构，适用于：
- 动态查询（运行时不确定列）
- 报表系统
- 灵活的数据展示

**示例**:
```csharp
[Sqlx("SELECT {{columns --regex ^data_}} FROM {{table}}")]
Task<List<Dictionary<string, object>>> GetDynamicDataAsync();

// 返回示例：
// [
//   { "data_name": "张三", "data_age": 25, "data_city": "北京" },
//   { "data_name": "李四", "data_age": 30, "data_city": "上海" }
// ]
```

---

### 3. **InterpolatedSqlString - 安全的 SQL 字符串插值**
类似 C# 的字符串插值，但自动进行 SQL 参数化，防止注入。

**示例**:
```csharp
// ❌ 不安全的方式
var sql = $"SELECT * FROM users WHERE name = '{userName}'"; // SQL 注入风险

// ✅ 安全的方式
var sql = InterpolatedSqlString.Create($"SELECT * FROM users WHERE name = {userName}");
// 自动转换为: "SELECT * FROM users WHERE name = @p0" + { @p0 = userName }
```

---

### 4. **ValueStringBuilder 性能优化**
将 `StringBuilder` 替换为 `ValueStringBuilder`（栈分配），减少 GC 压力。

**优化场景**:
- SQL 生成（热路径）
- 占位符处理
- 参数拼接

**预期效果**:
- 减少 20-30% 内存分配
- 提升 5-10% 性能

---

## 🎯 功能优先级

| 功能 | 优先级 | 复杂度 | 影响范围 | 预计工作量 |
|------|--------|--------|----------|-----------|
| **--regex 列筛选** | P1 (高) | 中 | 占位符系统 | 2-3天 |
| **动态返回值** | P1 (高) | 高 | 代码生成器+核心库 | 3-4天 |
| **InterpolatedSqlString** | P2 (中) | 高 | 核心库+分析器 | 4-5天 |
| **ValueStringBuilder** | P3 (低) | 低 | 性能优化 | 1-2天 |

**总预计**: 10-14 天

---

## 📐 详细设计

### 1. --regex 正则表达式筛选 ✅ **优先实现**

#### 设计思路
在现有的 `{{columns}}` 占位符基础上添加 `--regex` 选项。

#### API 设计
```csharp
// 基础用法
{{columns --regex pattern}}

// 组合用法
{{columns --regex ^user_ --exclude Id}}  // 筛选 user_ 开头，排除 Id
{{columns --regex _at$ --only}}           // 只选择 _at 结尾的列
```

#### 实现步骤
1. **修改 `SqlTemplateEngine`**:
   - 解析 `--regex pattern` 参数
   - 使用 `Regex.IsMatch` 过滤列名
   - 缓存编译的正则表达式

2. **添加验证**:
   - 正则表达式语法验证
   - 性能警告（复杂正则）
   - 安全检查（防止 ReDoS 攻击）

3. **代码示例**:
```csharp
// src/Sqlx.Generator/Core/SqlTemplateEngine.cs
private List<string> FilterColumnsByRegex(List<string> columns, string pattern)
{
    try
    {
        // 使用超时防止 ReDoS
        var regex = new Regex(pattern, RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));
        return columns.Where(c => regex.IsMatch(c)).ToList();
    }
    catch (RegexMatchTimeoutException)
    {
        throw new InvalidOperationException($"Regex pattern '{pattern}' timeout. Please simplify the pattern.");
    }
}
```

#### 测试用例
```csharp
[TestMethod]
public void Regex_MatchUserColumns_ReturnsFilteredColumns()
{
    // Arrange
    var template = "SELECT {{columns --regex ^user_}} FROM users";
    
    // Act
    var result = engine.ProcessTemplate(template, ...);
    
    // Assert
    Assert.AreEqual("SELECT user_name, user_email FROM users", result.ProcessedSql);
}
```

---

### 2. 动态返回值 List<Dictionary<string, object>> ✅ **优先实现**

#### 设计思路
支持返回动态列结构，适配运行时不确定的列。

#### API 设计
```csharp
// 方法定义
[Sqlx("SELECT {{columns --regex ^data_}} FROM {{table}}")]
Task<List<Dictionary<string, object>>> GetDynamicColumnsAsync();

// 使用
var results = await repo.GetDynamicColumnsAsync();
foreach (var row in results)
{
    foreach (var kvp in row)
    {
        Console.WriteLine($"{kvp.Key}: {kvp.Value}");
    }
}
```

#### 实现步骤
1. **检测返回类型**:
```csharp
// src/Sqlx.Generator/Core/CodeGenerationService.cs
private bool IsDynamicReturnType(ITypeSymbol returnType)
{
    // 检查是否是 List<Dictionary<string, object>> 或其变体
    if (returnType is INamedTypeSymbol namedType)
    {
        if (namedType.Name == "List" || namedType.Name == "IList")
        {
            var typeArg = namedType.TypeArguments.FirstOrDefault();
            if (typeArg is INamedTypeSymbol dictType)
            {
                return dictType.Name == "Dictionary" &&
                       dictType.TypeArguments.Length == 2 &&
                       dictType.TypeArguments[0].SpecialType == SpecialType.System_String &&
                       dictType.TypeArguments[1].SpecialType == SpecialType.System_Object;
            }
        }
    }
    return false;
}
```

2. **生成动态读取代码**:
```csharp
// 生成的代码示例
public async Task<List<Dictionary<string, object>>> GetDynamicColumnsAsync()
{
    var __result__ = new List<Dictionary<string, object>>();
    
    using var __cmd__ = __connection__.CreateCommand();
    __cmd__.CommandText = "SELECT data_name, data_age, data_city FROM users";
    
    using var __reader__ = await __cmd__.ExecuteReaderAsync();
    
    while (await __reader__.ReadAsync())
    {
        var __row__ = new Dictionary<string, object>(__reader__.FieldCount);
        
        for (int i = 0; i < __reader__.FieldCount; i++)
        {
            var __columnName__ = __reader__.GetName(i);
            var __value__ = __reader__.IsDBNull(i) ? null : __reader__.GetValue(i);
            __row__[__columnName__] = __value__;
        }
        
        __result__.Add(__row__);
    }
    
    return __result__;
}
```

3. **性能优化**:
   - 预分配 Dictionary 容量
   - 缓存列名信息
   - 避免装箱（可能）

#### 测试用例
```csharp
[TestMethod]
public async Task DynamicReturn_WithRegexFilter_ReturnsCorrectData()
{
    // Arrange
    var repo = new TestRepository(connection);
    
    // Act
    var results = await repo.GetDynamicColumnsAsync();
    
    // Assert
    Assert.AreEqual(2, results.Count);
    Assert.IsTrue(results[0].ContainsKey("data_name"));
    Assert.IsFalse(results[0].ContainsKey("id")); // 非 data_ 开头的列被过滤
}
```

---

### 3. InterpolatedSqlString - 安全的 SQL 字符串插值 ⚠️ **需谨慎设计**

#### 设计思路
提供类似 C# 字符串插值的语法，但自动进行参数化，防止 SQL 注入。

#### API 设计
```csharp
// 核心类型
public readonly ref struct InterpolatedSqlString
{
    public string Sql { get; }
    public Dictionary<string, object?> Parameters { get; }
    
    public static InterpolatedSqlString Create(
        [InterpolatedStringHandler] ref InterpolatedSqlStringHandler handler)
    {
        return handler.GetFormattedString();
    }
}

// 使用示例
var userName = "admin";
var age = 25;

var query = InterpolatedSqlString.Create(
    $"SELECT * FROM users WHERE name = {userName} AND age > {age}");

// 生成：
// Sql: "SELECT * FROM users WHERE name = @p0 AND age > @p1"
// Parameters: { "@p0": "admin", "@p1": 25 }
```

#### 实现步骤
1. **InterpolatedStringHandler 实现**:
```csharp
[InterpolatedStringHandler]
public ref struct InterpolatedSqlStringHandler
{
    private ValueStringBuilder _builder;
    private Dictionary<string, object?> _parameters;
    private int _parameterCount;
    
    public InterpolatedSqlStringHandler(int literalLength, int formattedCount)
    {
        _builder = new ValueStringBuilder(stackalloc char[256]);
        _parameters = new Dictionary<string, object?>(formattedCount);
        _parameterCount = 0;
    }
    
    public void AppendLiteral(string value)
    {
        _builder.Append(value);
    }
    
    public void AppendFormatted<T>(T value)
    {
        var paramName = $"@p{_parameterCount++}";
        _builder.Append(paramName);
        _parameters[paramName] = value;
    }
    
    public InterpolatedSqlString GetFormattedString()
    {
        return new InterpolatedSqlString(_builder.ToString(), _parameters);
    }
}
```

2. **安全验证**:
   - 禁止插值危险的 SQL 关键字
   - 添加 Roslyn 分析器检测不安全用法
   - 文档警告和最佳实践

3. **分析器规则**:
```csharp
// SQLX3001: 不要在 InterpolatedSqlString 中直接插值表名
var tableName = "users";
var query = InterpolatedSqlString.Create($"SELECT * FROM {tableName}"); // 警告

// 正确做法：使用 DynamicSql
[Sqlx("SELECT * FROM {{@tableName}}")]
Task<List<User>> GetFromTableAsync([DynamicSql] string tableName);
```

#### 测试用例
```csharp
[TestMethod]
public void InterpolatedSql_WithParameters_GeneratesCorrectSql()
{
    // Arrange
    var name = "admin";
    var age = 25;
    
    // Act
    var query = InterpolatedSqlString.Create(
        $"SELECT * FROM users WHERE name = {name} AND age > {age}");
    
    // Assert
    Assert.AreEqual("SELECT * FROM users WHERE name = @p0 AND age > @p1", query.Sql);
    Assert.AreEqual("admin", query.Parameters["@p0"]);
    Assert.AreEqual(25, query.Parameters["@p1"]);
}
```

---

### 4. ValueStringBuilder 性能优化 🚀 **按需优化**

#### 设计思路
将热路径中的 `StringBuilder` 替换为 `ValueStringBuilder`（栈分配）。

#### 优化范围
**只优化真正的热路径**:
1. ✅ SQL 生成逻辑（每次查询都执行）
2. ✅ 占位符处理（模板处理）
3. ❌ 代码生成器（编译时执行一次，不优化）

#### 实现示例
```csharp
// 之前（堆分配）
public string GenerateSql()
{
    var sb = new StringBuilder(256);
    sb.Append("SELECT ");
    sb.Append(string.Join(", ", columns));
    sb.Append(" FROM ");
    sb.Append(tableName);
    return sb.ToString();
}

// 之后（栈分配）
public string GenerateSql()
{
    var sb = new ValueStringBuilder(stackalloc char[256]);
    try
    {
        sb.Append("SELECT ");
        sb.Append(string.Join(", ", columns));
        sb.Append(" FROM ");
        sb.Append(tableName);
        return sb.ToString();
    }
    finally
    {
        sb.Dispose();
    }
}
```

#### 优化原则
**不要过度优化**:
- ❌ 不优化代码生成器（编译时执行）
- ❌ 不优化冷路径（很少执行）
- ❌ 不优化可读性差的代码
- ✅ 只优化运行时热路径
- ✅ 保持代码简洁
- ✅ 添加性能注释说明

---

## 📊 实现阶段

### Phase 1: 核心功能 (Week 1-2)

#### 阶段 1.1: --regex 列筛选 (2-3天)
- [ ] 修改 `SqlTemplateEngine` 支持 `--regex`
- [ ] 添加正则表达式缓存
- [ ] 添加超时保护（防 ReDoS）
- [ ] 编写单元测试（20+个）
- [ ] 更新文档和示例

#### 阶段 1.2: 动态返回值 (3-4天)
- [ ] 检测返回类型是否为 `List<Dictionary<string, object>>`
- [ ] 生成动态读取代码
- [ ] 优化性能（预分配、缓存）
- [ ] 编写单元测试（15+个）
- [ ] 编写集成测试
- [ ] 更新文档和示例

---

### Phase 2: 高级功能 (Week 3)

#### 阶段 2.1: InterpolatedSqlString (4-5天)
- [ ] 实现 `InterpolatedSqlStringHandler`
- [ ] 实现 `InterpolatedSqlString` 类型
- [ ] 添加 Roslyn 分析器（安全检查）
- [ ] 编写单元测试（25+个）
- [ ] 编写安全测试（SQL 注入尝试）
- [ ] 详细的安全文档和警告

---

### Phase 3: 性能优化 (Week 4)

#### 阶段 3.1: ValueStringBuilder (1-2天)
- [ ] 识别热路径代码
- [ ] 替换 `StringBuilder` 为 `ValueStringBuilder`
- [ ] 运行 Benchmark 验证提升
- [ ] 确保没有性能回退
- [ ] 添加性能注释

---

## 🔍 技术细节

### --regex 实现细节

```csharp
// src/Sqlx.Generator/Core/SqlTemplateEngine.cs

// 1. 解析 regex 参数
private (string pattern, bool isRegex) ParseColumnOptions(string options)
{
    var match = Regex.Match(options, @"--regex\s+([^\s]+)");
    if (match.Success)
    {
        return (match.Groups[1].Value, true);
    }
    return (string.Empty, false);
}

// 2. 过滤列（带缓存）
private static readonly ConcurrentDictionary<string, Regex> RegexCache = new();

private List<string> FilterColumns(List<string> columns, string pattern)
{
    var regex = RegexCache.GetOrAdd(pattern, p => 
        new Regex(p, RegexOptions.Compiled, TimeSpan.FromMilliseconds(100)));
    
    return columns.Where(c => regex.IsMatch(c)).ToList();
}
```

### 动态返回值实现细节

```csharp
// src/Sqlx.Generator/Core/CodeGenerationService.cs

private void GenerateDynamicReaderCode(IndentedStringBuilder sb, IMethodSymbol method)
{
    sb.AppendLine("var __result__ = new global::System.Collections.Generic.List<global::System.Collections.Generic.Dictionary<string, object?>>();");
    sb.AppendLine();
    sb.AppendLine("using var __reader__ = await __cmd__.ExecuteReaderAsync();");
    sb.AppendLine();
    sb.AppendLine("// 缓存列名信息");
    sb.AppendLine("var __columnCount__ = __reader__.FieldCount;");
    sb.AppendLine("var __columnNames__ = new string[__columnCount__];");
    sb.AppendLine("for (int __i__ = 0; __i__ < __columnCount__; __i__++)");
    sb.AppendLine("{");
    sb.PushIndent();
    sb.AppendLine("__columnNames__[__i__] = __reader__.GetName(__i__);");
    sb.PopIndent();
    sb.AppendLine("}");
    sb.AppendLine();
    sb.AppendLine("while (await __reader__.ReadAsync())");
    sb.AppendLine("{");
    sb.PushIndent();
    sb.AppendLine("// 预分配 Dictionary 容量");
    sb.AppendLine("var __row__ = new global::System.Collections.Generic.Dictionary<string, object?>(__columnCount__);");
    sb.AppendLine();
    sb.AppendLine("for (int __i__ = 0; __i__ < __columnCount__; __i__++)");
    sb.AppendLine("{");
    sb.PushIndent();
    sb.AppendLine("var __value__ = __reader__.IsDBNull(__i__) ? null : __reader__.GetValue(__i__);");
    sb.AppendLine("__row__[__columnNames__[__i__]] = __value__;");
    sb.PopIndent();
    sb.AppendLine("}");
    sb.AppendLine();
    sb.AppendLine("__result__.Add(__row__);");
    sb.PopIndent();
    sb.AppendLine("}");
}
```

---

## ⚠️ 风险评估

| 风险 | 等级 | 缓解措施 |
|------|------|---------|
| **ReDoS 攻击** | 高 | 添加正则超时、验证复杂度 |
| **SQL 注入** | 高 | InterpolatedSqlString 强制参数化 |
| **性能回退** | 中 | 完整的 Benchmark 测试 |
| **兼容性破坏** | 低 | 所有新功能都是可选的 |
| **代码复杂度** | 中 | 充分的文档和注释 |

---

## 📋 检查清单

### 代码质量
- [ ] 所有新代码都有单元测试
- [ ] 测试覆盖率 ≥ 80%
- [ ] 无编译警告
- [ ] 通过所有现有测试
- [ ] 性能 Benchmark 验证

### 文档
- [ ] 更新 README.md
- [ ] 更新 docs/PLACEHOLDERS.md
- [ ] 更新 docs/API_REFERENCE.md
- [ ] 添加使用示例
- [ ] 添加安全警告

### 安全
- [ ] SQL 注入测试
- [ ] ReDoS 防护测试
- [ ] Roslyn 分析器验证
- [ ] 安全审查

---

## 🎯 成功标准

### Phase 1 完成标准
- ✅ `--regex` 功能正常工作
- ✅ 动态返回值正确读取数据
- ✅ 所有测试通过
- ✅ 性能无回退
- ✅ 文档完整

### Phase 2 完成标准
- ✅ InterpolatedSqlString 安全可靠
- ✅ Roslyn 分析器能检测不安全用法
- ✅ 通过安全审查
- ✅ 文档包含详细警告

### Phase 3 完成标准
- ✅ Benchmark 显示性能提升
- ✅ 内存分配减少 20-30%
- ✅ 代码仍然可读
- ✅ 无性能回退

---

## 📝 后续计划

### v1.2.0 可能的功能
- [ ] `--transform` 列转换（大小写、前缀等）
- [ ] `--aggregate` 聚合函数支持
- [ ] `--join` 智能连接
- [ ] 更多动态返回类型支持

---

## 💡 最佳实践建议

### --regex 使用
```csharp
// ✅ 推荐：简单、高效的正则
{{columns --regex ^user_}}

// ⚠️ 谨慎：复杂正则可能影响性能
{{columns --regex ^(?!.*(password|secret|token))}}

// ❌ 避免：过于复杂的正则（ReDoS 风险）
{{columns --regex (a+)+b}}
```

### 动态返回值使用
```csharp
// ✅ 推荐：明确的场景
[Sqlx("SELECT {{columns --regex ^data_}} FROM {{table}}")]
Task<List<Dictionary<string, object>>> GetDynamicReportDataAsync();

// ❌ 避免：滥用动态返回（应该用强类型）
[Sqlx("SELECT id, name, email FROM users")]
Task<List<Dictionary<string, object>>> GetUsersAsync(); // 应该用 List<User>
```

### InterpolatedSqlString 使用
```csharp
// ✅ 推荐：参数值插值
var name = "admin";
var sql = InterpolatedSqlString.Create($"SELECT * FROM users WHERE name = {name}");

// ❌ 禁止：表名、列名插值（不安全）
var table = "users";
var sql = InterpolatedSqlString.Create($"SELECT * FROM {table}"); // 使用 [DynamicSql] 代替
```

---

## 🏆 总结

本计划旨在为 Sqlx 添加强大的新功能，同时保持：
- ✅ **安全性第一**: 防止 SQL 注入和 ReDoS 攻击
- ✅ **性能优先**: 只优化热路径，不过度优化
- ✅ **简洁性**: 保持代码可读和可维护
- ✅ **渐进式**: 所有新功能都是可选的，不破坏兼容性

**预计时间**: 10-14 天  
**风险**: 中等（通过充分测试和文档缓解）  
**收益**: 高（大幅提升灵活性和易用性）

---

<div align="center">

**让我们开始实现这些激动人心的新功能！** 🚀

</div>

