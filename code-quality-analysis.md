# Sqlx 代码质量分析报告

## 1. 重复代码问题

### 1.1 PlaceholderHandler 重复模式

**问题描述：**
多个 PlaceholderHandler 类存在大量重复代码，特别是：

#### LimitPlaceholderHandler 和 OffsetPlaceholderHandler
这两个类几乎完全相同，只是关键字不同（LIMIT vs OFFSET）：

**重复代码：**
- `GetType()` 方法完全相同
- `Process()` 方法逻辑完全相同，只是输出字符串不同
- `Render()` 方法完全相同

**建议重构方案：**
```csharp
// 创建基类
public abstract class KeywordWithValuePlaceholderHandler : PlaceholderHandlerBase
{
    protected abstract string Keyword { get; }
    
    public override PlaceholderType GetType(string options) => PlaceholderType.Static;
    
    public override string Process(PlaceholderContext context, string options)
    {
        var count = ParseCount(options);
        if (count is not null)
        {
            return $"{Keyword} {count.Value}";
        }

        var paramName = ParseParam(options);
        if (paramName is not null)
        {
            return $"{Keyword} {context.Dialect.ParameterPrefix}{paramName}";
        }

        return string.Empty;
    }
    
    public override string Render(PlaceholderContext context, string options, IReadOnlyDictionary<string, object?>? parameters)
    {
        var paramName = ParseParam(options)
            ?? throw new InvalidOperationException($"{{{{{Keyword.ToLower()}}}}} requires --count or --param option.");
        var value = GetParam(parameters, paramName);
        return value is not null ? $"{Keyword} {Convert.ToInt32(value, CultureInfo.InvariantCulture)}" : string.Empty;
    }
}

// 简化实现
public sealed class LimitPlaceholderHandler : KeywordWithValuePlaceholderHandler
{
    public static LimitPlaceholderHandler Instance { get; } = new();
    public override string Name => "limit";
    protected override string Keyword => "LIMIT";
}

public sealed class OffsetPlaceholderHandler : KeywordWithValuePlaceholderHandler
{
    public static OffsetPlaceholderHandler Instance { get; } = new();
    public override string Name => "offset";
    protected override string Keyword => "OFFSET";
}
```

**预期收益：**
- 减少约 80 行重复代码
- 提高可维护性
- 降低 bug 风险

---

### 1.2 列处理重复模式

**问题描述：**
ColumnsPlaceholderHandler、ValuesPlaceholderHandler、SetPlaceholderHandler 都有类似的列过滤和处理逻辑。

**重复代码：**
- `FilterColumns()` 调用
- `ParseInlineExpressions()` 调用
- 列遍历和处理逻辑

**建议：**
这些类的差异较大，重构收益有限。建议保持现状，但可以考虑提取共同的辅助方法到基类。

---

## 2. 复杂度问题

### 2.1 WherePlaceholderHandler 复杂度过高

**问题描述：**
`WherePlaceholderHandler.RenderFromObject()` 方法复杂度较高：
- 循环复杂度：约 8
- 认知复杂度：约 12
- 行数：约 70 行

**复杂度来源：**
1. 列查找逻辑（线性搜索 vs 哈希查找）
2. 属性名匹配逻辑
3. NULL 值处理
4. 条件拼接逻辑

**建议重构方案：**
```csharp
private static string RenderFromObject(PlaceholderContext context, string objectParamName, IReadOnlyDictionary<string, object?>? parameters)
{
    var obj = GetParam(parameters, objectParamName);
    if (obj is null || obj is not IReadOnlyDictionary<string, object?> dict)
    {
        return HandleNullOrInvalidObject(obj, objectParamName);
    }

    if (dict.Count == 0)
    {
        return "1=1";
    }

    var columnLookup = BuildColumnLookup(context.Columns);
    var conditions = BuildConditions(dict, columnLookup, context.Dialect);

    return FormatConditions(conditions);
}

private static string HandleNullOrInvalidObject(object? obj, string paramName)
{
    if (obj is null)
    {
        return "1=1";
    }
    
    throw new InvalidOperationException(
        $"Parameter '{paramName}' for --object must be IReadOnlyDictionary<string, object?>. " +
        $"Use entity.ToDictionary() or create a dictionary manually.");
}

private static Dictionary<string, ColumnMeta>? BuildColumnLookup(IReadOnlyList<ColumnMeta> columns)
{
    if (columns.Count <= 4)
    {
        return null; // Use linear search for small column sets
    }

    var lookup = new Dictionary<string, ColumnMeta>(columns.Count, StringComparer.OrdinalIgnoreCase);
    foreach (var col in columns)
    {
        lookup[col.PropertyName] = col;
        lookup[col.Name] = col;
    }
    return lookup;
}

private static List<string> BuildConditions(
    IReadOnlyDictionary<string, object?> dict,
    Dictionary<string, ColumnMeta>? columnLookup,
    SqlDialect dialect)
{
    var conditions = new List<string>(dict.Count);
    
    foreach (var kvp in dict)
    {
        var column = FindColumn(kvp.Key, columnLookup, context.Columns);
        if (column is null)
        {
            continue;
        }

        var condition = BuildCondition(column, kvp.Value, dialect);
        conditions.Add(condition);
    }
    
    return conditions;
}

private static ColumnMeta? FindColumn(
    string key,
    Dictionary<string, ColumnMeta>? lookup,
    IReadOnlyList<ColumnMeta> columns)
{
    if (lookup != null)
    {
        return lookup.TryGetValue(key, out var col) ? col : null;
    }

    // Linear search for small column sets
    foreach (var col in columns)
    {
        if (string.Equals(col.PropertyName, key, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(col.Name, key, StringComparison.OrdinalIgnoreCase))
        {
            return col;
        }
    }
    
    return null;
}

private static string BuildCondition(ColumnMeta column, object? value, SqlDialect dialect)
{
    var columnName = dialect.WrapColumn(column.Name);
    
    if (value is null)
    {
        return $"{columnName} IS NULL";
    }
    
    return $"{columnName} = {dialect.CreateParameter(column.Name)}";
}

private static string FormatConditions(List<string> conditions)
{
    if (conditions.Count == 0)
    {
        return "1=1";
    }
    
    return conditions.Count == 1
        ? conditions[0]
        : $"({string.Join(" AND ", conditions)})";
}
```

**预期收益：**
- 降低循环复杂度到约 3-4
- 提高代码可读性
- 更容易测试和维护
- 每个方法职责单一

---

### 2.2 ValuesPlaceholderHandler 复杂度

**问题描述：**
`Process()` 和 `Render()` 方法都比较复杂，处理多种模式。

**建议：**
考虑使用策略模式分离不同的处理逻辑：
- StandardValuesStrategy（标准参数列表）
- InlineExpressionStrategy（内联表达式）
- CollectionExpansionStrategy（集合展开）

---

## 3. 代码度量建议

### 3.1 建议的度量标准

**方法级别：**
- 循环复杂度：≤ 10（理想 ≤ 5）
- 认知复杂度：≤ 15（理想 ≤ 10）
- 方法行数：≤ 50 行（理想 ≤ 30 行）

**类级别：**
- 类行数：≤ 300 行
- 公共方法数：≤ 10

### 3.2 当前超标项

**需要重构的类：**
1. `WherePlaceholderHandler` - 复杂度过高
2. `LimitPlaceholderHandler` + `OffsetPlaceholderHandler` - 重复代码
3. `ValuesPlaceholderHandler` - 复杂度较高

---

## 4. 优先级建议

### 高优先级（建议立即处理）
1. **合并 LimitPlaceholderHandler 和 OffsetPlaceholderHandler**
   - 影响：减少约 80 行重复代码
   - 风险：低
   - 工作量：2-3 小时

### 中优先级（建议近期处理）
2. **重构 WherePlaceholderHandler.RenderFromObject()**
   - 影响：降低复杂度，提高可维护性
   - 风险：中（需要充分测试）
   - 工作量：4-6 小时

### 低优先级（可选）
3. **重构 ValuesPlaceholderHandler**
   - 影响：提高代码组织
   - 风险：中
   - 工作量：6-8 小时

---

## 5. 测试覆盖率

**建议：**
- 在重构前确保测试覆盖率 ≥ 90%
- 重构后运行完整测试套件
- 考虑添加性能基准测试

---

## 6. 总结

**当前状态：**
- 代码质量：良好
- 主要问题：局部重复代码和复杂度过高
- 测试覆盖：充分

**建议行动：**
1. 优先处理 Limit/Offset 重复代码（快速见效）
2. 逐步重构 WherePlaceholderHandler（提高质量）
3. 建立代码度量监控（持续改进）

**预期收益：**
- 减少约 100+ 行重复代码
- 降低维护成本
- 提高代码可读性
- 降低 bug 风险
