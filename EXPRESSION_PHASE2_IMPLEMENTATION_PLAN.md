# Expression Phase 2 - 更多运算符支持

**日期**: 2025-10-25  
**预计时间**: 2-3小时  
**优先级**: ⭐⭐⭐ 高

---

## 🎯 目标

扩展Expression支持，添加更多常用运算符和方法。

---

## ✅ 已完成（Phase 1）

- `==` (Equal)
- `>` (GreaterThan)
- `<` (LessThan)
- `Contains()` (集合 - Phase 2已完成)

---

## 📋 Phase 2 实施内容

### 1. 比较运算符
- `>=` (GreaterThanOrEqual) - **已有，但需测试**
- `<=` (LessThanOrEqual)
- `!=` (NotEqual)

### 2. 逻辑运算符
- `&&` (AndAlso)
- `||` (OrElse)
- `!` (Not)

### 3. 字符串方法
- `StartsWith()`
- `EndsWith()`
- `Contains()` (字符串版本，映射到LIKE)

### 4. NULL检查
- `== null` / `!= null`
- `HasValue` (Nullable<T>)

---

## 🧪 TDD测试计划

### Phase 2A: 比较运算符 (6个测试)
```csharp
[TestMethod]
public void Expression_GreaterThanOrEqual_Should_Generate_SQL()
{
    // x => x.Age >= 18
    // SQL: WHERE age >= @p0
}

[TestMethod]
public void Expression_LessThanOrEqual_Should_Generate_SQL()
{
    // x => x.Age <= 65
    // SQL: WHERE age <= @p0
}

[TestMethod]
public void Expression_NotEqual_Should_Generate_SQL()
{
    // x => x.Status != "Deleted"
    // SQL: WHERE status <> @p0 或 WHERE status != @p0
}
```

### Phase 2B: 逻辑运算符 (6个测试)
```csharp
[TestMethod]
public void Expression_And_Should_Generate_SQL()
{
    // x => x.Age >= 18 && x.Age <= 65
    // SQL: WHERE age >= @p0 AND age <= @p1
}

[TestMethod]
public void Expression_Or_Should_Generate_SQL()
{
    // x => x.Status == "Active" || x.Status == "Pending"
    // SQL: WHERE status = @p0 OR status = @p1
}

[TestMethod]
public void Expression_Not_Should_Generate_SQL()
{
    // x => !(x.Age > 65)
    // SQL: WHERE NOT (age > @p0)
}

[TestMethod]
public void Expression_ComplexLogic_Should_Generate_SQL()
{
    // x => (x.Age >= 18 && x.Age <= 65) || x.IsVIP
    // SQL: WHERE (age >= @p0 AND age <= @p1) OR is_vip = @p2
}
```

### Phase 2C: 字符串方法 (6个测试)
```csharp
[TestMethod]
public void Expression_StartsWith_Should_Generate_LIKE()
{
    // x => x.Name.StartsWith("John")
    // SQL: WHERE name LIKE @p0 (value: "John%")
}

[TestMethod]
public void Expression_EndsWith_Should_Generate_LIKE()
{
    // x => x.Name.EndsWith("son")
    // SQL: WHERE name LIKE @p0 (value: "%son")
}

[TestMethod]
public void Expression_StringContains_Should_Generate_LIKE()
{
    // x => x.Name.Contains("oh")
    // SQL: WHERE name LIKE @p0 (value: "%oh%")
}
```

### Phase 2D: NULL检查 (4个测试)
```csharp
[TestMethod]
public void Expression_EqualNull_Should_Generate_IS_NULL()
{
    // x => x.DeletedAt == null
    // SQL: WHERE deleted_at IS NULL
}

[TestMethod]
public void Expression_NotEqualNull_Should_Generate_IS_NOT_NULL()
{
    // x => x.DeletedAt != null
    // SQL: WHERE deleted_at IS NOT NULL
}
```

---

## 🔧 实施步骤

### 步骤1: 创建TDD测试文件 (30分钟)
**文件**: `tests/Sqlx.Tests/Expression/TDD_Phase2_Operators_RedTests.cs`

- 22个测试（分4组）
- 全部红灯（预期失败）
- 测试各种运算符组合

### 步骤2: 修改ExpressionToSqlBase (60分钟)
**文件**: `src/Sqlx/ExpressionToSqlBase.cs`

**需要修改的方法**:

1. **ParseBinaryExpression**
```csharp
// 添加更多ExpressionType支持
case ExpressionType.GreaterThanOrEqual:
    return $"{left} >= {right}";
case ExpressionType.LessThanOrEqual:
    return $"{left} <= {right}";
case ExpressionType.NotEqual:
    return $"{left} <> {right}"; // 或 !=，根据数据库方言
case ExpressionType.AndAlso:
    return $"({left} AND {right})";
case ExpressionType.OrElse:
    return $"({left} OR {right})";
```

2. **ParseUnaryExpression**
```csharp
case ExpressionType.Not:
    var operand = ParseExpression(unary.Operand);
    return $"NOT ({operand})";
```

3. **ParseMethodCallExpression**
```csharp
// String methods
if (methodCall.Method.DeclaringType == typeof(string))
{
    switch (methodCall.Method.Name)
    {
        case "StartsWith":
            // value LIKE 'prefix%'
        case "EndsWith":
            // value LIKE '%suffix'
        case "Contains":
            // value LIKE '%substring%'
    }
}
```

4. **NULL检查特殊处理**
```csharp
// In ParseBinaryExpression
if (right is ConstantExpression constExpr && constExpr.Value == null)
{
    if (binary.NodeType == ExpressionType.Equal)
        return $"{left} IS NULL";
    if (binary.NodeType == ExpressionType.NotEqual)
        return $"{left} IS NOT NULL";
}
```

### 步骤3: 运行测试，修复bug (30分钟)
- 运行TDD测试
- 确保22/22通过
- 调试和修正

### 步骤4: 集成测试 (30分钟)
- 运行所有819个现有测试
- 确保无回归
- 性能验证

---

## 📝 预期生成代码示例

### 示例1: 复杂逻辑
```csharp
var users = await repo.GetWhereAsync(x => 
    (x.Age >= 18 && x.Age <= 65) || x.IsVIP);

// 生成SQL:
// WHERE (age >= @p0 AND age <= @p1) OR is_vip = @p2
// 参数: @p0=18, @p1=65, @p2=true
```

### 示例2: 字符串搜索
```csharp
var users = await repo.GetWhereAsync(x => 
    x.Name.StartsWith("John") && !x.Email.EndsWith("@spam.com"));

// 生成SQL:
// WHERE name LIKE @p0 AND NOT (email LIKE @p1)
// 参数: @p0='John%', @p1='%@spam.com'
```

### 示例3: NULL检查
```csharp
var users = await repo.GetWhereAsync(x => 
    x.DeletedAt == null && x.ApprovedAt != null);

// 生成SQL:
// WHERE deleted_at IS NULL AND approved_at IS NOT NULL
```

---

## ⚠️ 注意事项

### 1. 数据库方言差异
**NotEqual运算符**:
- SQL Server/PostgreSQL: `<>` 和 `!=` 都支持
- MySQL: 推荐 `!=`
- SQLite: 两者都支持
- **决策**: 使用 `<>`（更标准）

### 2. LIKE通配符转义
**问题**: 用户输入可能包含`%`或`_`  
**解决**: 
```csharp
private string EscapeLikePattern(string pattern)
{
    return pattern.Replace("%", "\\%").Replace("_", "\\_");
}
```

### 3. NULL比较的SQL标准
- **永远不要**: `column = NULL` ❌
- **正确做法**: `column IS NULL` ✅

### 4. 括号优先级
**AND和OR混合时必须加括号**:
```sql
-- ❌ 错误（优先级不明确）
WHERE a = 1 AND b = 2 OR c = 3

-- ✅ 正确
WHERE (a = 1 AND b = 2) OR c = 3
```

---

## 🎯 成功标准

- ✅ 22/22测试通过
- ✅ 无现有测试回归
- ✅ 生成的SQL正确且优化
- ✅ 参数正确绑定
- ✅ NULL处理符合SQL标准
- ✅ 括号优先级正确

---

## 📊 预期进度

**开始**: 70% (8.4/12)  
**完成**: 72% (8.6/12)  
**新增测试**: 22个  
**总测试**: 841个

---

## 🚀 后续Phase

**Phase 3** (未来):
- Math方法: `Math.Abs()`, `Math.Round()`
- 日期方法: `DateTime.Now`, `AddDays()`
- 聚合支持: `Count()`, `Sum()`, `Average()`
- 子查询支持

---

**创建时间**: 2025-10-25  
**状态**: 准备开始  
**下一步**: 创建TDD红灯测试

