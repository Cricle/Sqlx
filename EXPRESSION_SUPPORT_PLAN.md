# Expression参数支持 - 实施计划

**优先级**: 🔥 高（核心功能）  
**预计时间**: 6-8小时  
**实施方式**: TDD

---

## 🎯 功能目标

让用户使用C# Expression替代手写SQL WHERE子句，实现：
1. ✅ 类型安全（编译时检查）
2. ✅ IDE智能提示
3. ✅ 防止SQL注入
4. ✅ AOT友好
5. ✅ 支持多种表达式

---

## 📝 用户场景

### 场景1: 简单条件查询
```csharp
// 不再写SQL
var users = await repo.GetWhereAsync(u => u.Age > 18);

// 生成SQL
// SELECT * FROM users WHERE age > @p0
```

### 场景2: 复合条件
```csharp
var users = await repo.GetWhereAsync(u => 
    u.Age > 18 && u.IsActive && u.Email != null);

// 生成SQL
// SELECT * FROM users WHERE age > @p0 AND is_active = @p1 AND email IS NOT NULL
```

### 场景3: IN查询
```csharp
var validStatuses = new[] { "Active", "Pending" };
var users = await repo.GetWhereAsync(u => validStatuses.Contains(u.Status));

// 生成SQL
// SELECT * FROM users WHERE status IN (@p0, @p1)
```

### 场景4: LIKE查询
```csharp
var users = await repo.GetWhereAsync(u => u.Name.StartsWith("Alice"));

// 生成SQL
// SELECT * FROM users WHERE name LIKE @p0 + '%'
```

### 场景5: 删除操作
```csharp
await repo.DeleteWhereAsync(u => u.CreatedAt < DateTime.Now.AddDays(-30));

// 生成SQL
// DELETE FROM users WHERE created_at < @p0
```

---

## 🏗️ 架构设计

### 1. 方法签名识别

**支持的方法命名模式**:
```csharp
Task<List<T>> GetWhereAsync(Expression<Func<T, bool>> predicate);
Task<T?> GetFirstWhereAsync(Expression<Func<T, bool>> predicate);
Task<int> DeleteWhereAsync(Expression<Func<T, bool>> predicate);
Task<int> UpdateWhereAsync(T entity, Expression<Func<T, bool>> predicate);
Task<int> CountWhereAsync(Expression<Func<T, bool>> predicate);
Task<bool> ExistsWhereAsync(Expression<Func<T, bool>> predicate);
```

**或者使用特性标记**:
```csharp
[SqlTemplate("SELECT * FROM {{table}} WHERE {{where @predicate}}")]
Task<List<User>> GetActiveUsersAsync(Expression<Func<User, bool>> predicate);
```

### 2. Expression解析器

**需要支持的表达式类型**:
```csharp
public enum ExpressionType
{
    // 比较运算
    Equal,              // u.Age == 18
    NotEqual,           // u.Age != 18
    GreaterThan,        // u.Age > 18
    GreaterThanOrEqual, // u.Age >= 18
    LessThan,           // u.Age < 18
    LessThanOrEqual,    // u.Age <= 18
    
    // 逻辑运算
    AndAlso,            // u.Age > 18 && u.IsActive
    OrElse,             // u.Age > 18 || u.IsVip
    Not,                // !u.IsDeleted
    
    // 字符串操作
    StartsWith,         // u.Name.StartsWith("A")
    EndsWith,           // u.Name.EndsWith("son")
    Contains,           // u.Name.Contains("ice")
    
    // 集合操作
    In,                 // ids.Contains(u.Id)
    
    // NULL检查
    IsNull,             // u.Email == null
    IsNotNull,          // u.Email != null
}
```

### 3. SQL生成策略

**SQL片段生成**:
```csharp
private string GenerateWhereClause(Expression expr)
{
    return expr switch
    {
        BinaryExpression binary => GenerateBinary(binary),
        MethodCallExpression method => GenerateMethod(method),
        MemberExpression member => GenerateMember(member),
        ConstantExpression constant => GenerateConstant(constant),
        _ => throw new NotSupportedException($"Expression type {expr.NodeType} not supported")
    };
}
```

**参数化查询**:
```csharp
// 输入: u => u.Age > 18 && u.Name == "Alice"
// 输出:
// SQL: age > @p0 AND name = @p1
// Parameters: { p0 = 18, p1 = "Alice" }
```

---

## 🧪 TDD实施阶段

### Phase 1: 简单比较运算 (2小时)

**测试用例**:
```csharp
[TestMethod]
public void Expression_Equal_Should_Generate_Equal_SQL()
{
    // u => u.Age == 18
    // 期望: WHERE age = @p0
}

[TestMethod]
public void Expression_GreaterThan_Should_Generate_GreaterThan_SQL()
{
    // u => u.Age > 18
    // 期望: WHERE age > @p0
}

[TestMethod]
public void Expression_NotEqual_Should_Generate_NotEqual_SQL()
{
    // u => u.Age != 18
    // 期望: WHERE age <> @p0
}
```

### Phase 2: 逻辑运算 (1.5小时)

**测试用例**:
```csharp
[TestMethod]
public void Expression_AndAlso_Should_Generate_AND_SQL()
{
    // u => u.Age > 18 && u.IsActive
    // 期望: WHERE age > @p0 AND is_active = @p1
}

[TestMethod]
public void Expression_OrElse_Should_Generate_OR_SQL()
{
    // u => u.Age > 18 || u.IsVip
    // 期望: WHERE age > @p0 OR is_vip = @p1
}

[TestMethod]
public void Expression_Not_Should_Generate_NOT_SQL()
{
    // u => !u.IsDeleted
    // 期望: WHERE NOT is_deleted = @p0
}
```

### Phase 3: 字符串操作 (1.5小时)

**测试用例**:
```csharp
[TestMethod]
public void Expression_StartsWith_Should_Generate_LIKE_SQL()
{
    // u => u.Name.StartsWith("Alice")
    // PostgreSQL: WHERE name LIKE @p0 || '%'
    // SQL Server: WHERE name LIKE @p0 + '%'
}

[TestMethod]
public void Expression_Contains_Should_Generate_LIKE_SQL()
{
    // u => u.Name.Contains("ice")
    // WHERE name LIKE '%' || @p0 || '%'
}
```

### Phase 4: 集合操作 (1.5小时)

**测试用例**:
```csharp
[TestMethod]
public void Expression_Contains_Should_Generate_IN_SQL()
{
    // var ids = new[] { 1, 2, 3 };
    // u => ids.Contains(u.Id)
    // 期望: WHERE id IN (@p0, @p1, @p2)
}

[TestMethod]
public void Expression_Contains_EmptyList_Should_Generate_FALSE()
{
    // var ids = new int[0];
    // u => ids.Contains(u.Id)
    // 期望: WHERE 1 = 0
}
```

### Phase 5: NULL处理 (1小时)

**测试用例**:
```csharp
[TestMethod]
public void Expression_IsNull_Should_Generate_IS_NULL_SQL()
{
    // u => u.Email == null
    // 期望: WHERE email IS NULL
}

[TestMethod]
public void Expression_IsNotNull_Should_Generate_IS_NOT_NULL_SQL()
{
    // u => u.Email != null
    // 期望: WHERE email IS NOT NULL
}
```

---

## 🔧 实现细节

### 1. Expression访问者模式

```csharp
public class SqlExpressionVisitor : ExpressionVisitor
{
    private readonly StringBuilder _sql = new();
    private readonly List<object> _parameters = new();
    private readonly string _dialect;
    
    public (string Sql, List<object> Parameters) Generate(Expression expression)
    {
        Visit(expression);
        return (_sql.ToString(), _parameters);
    }
    
    protected override Expression VisitBinary(BinaryExpression node)
    {
        Visit(node.Left);
        
        _sql.Append(node.NodeType switch
        {
            ExpressionType.Equal => " = ",
            ExpressionType.NotEqual => " <> ",
            ExpressionType.GreaterThan => " > ",
            ExpressionType.GreaterThanOrEqual => " >= ",
            ExpressionType.LessThan => " < ",
            ExpressionType.LessThanOrEqual => " <= ",
            ExpressionType.AndAlso => " AND ",
            ExpressionType.OrElse => " OR ",
            _ => throw new NotSupportedException()
        });
        
        Visit(node.Right);
        return node;
    }
    
    protected override Expression VisitMember(MemberExpression node)
    {
        // 获取属性名并转换为列名（snake_case）
        var columnName = ToSnakeCase(node.Member.Name);
        _sql.Append(columnName);
        return node;
    }
    
    protected override Expression VisitConstant(ConstantExpression node)
    {
        // 添加参数化值
        _parameters.Add(node.Value);
        _sql.Append($"@p{_parameters.Count - 1}");
        return node;
    }
}
```

### 2. 属性名转列名

```csharp
private string ToSnakeCase(string propertyName)
{
    // 支持自定义列名映射
    // Age -> age
    // IsActive -> is_active
    // CreatedAt -> created_at
    
    return string.Concat(
        propertyName.Select((c, i) =>
            i > 0 && char.IsUpper(c) ? "_" + char.ToLower(c) : char.ToLower(c).ToString()
        )
    );
}
```

### 3. 源生成器集成

```csharp
// 在CodeGenerationService中
if (HasExpressionParameter(method, out var predicateParam))
{
    // 生成Expression解析代码
    sb.AppendLine($"var __visitor__ = new SqlExpressionVisitor(\"{dialect}\");");
    sb.AppendLine($"var (__whereSql__, __whereParams__) = __visitor__.Generate({predicateParam.Name});");
    sb.AppendLine($"__cmd__.CommandText = __cmd__.CommandText.Replace(\"{{{{where}}}}\", __whereSql__);");
    
    // 添加WHERE参数
    sb.AppendLine("foreach (var __param__ in __whereParams__)");
    sb.AppendLine("{");
    sb.AppendLine("    var __p__ = __cmd__.CreateParameter();");
    sb.AppendLine("    __p__.Value = __param__;");
    sb.AppendLine("    __cmd__.Parameters.Add(__p__);");
    sb.AppendLine("}");
}
```

---

## ⚠️ 技术挑战

### 1. AOT兼容性
**问题**: Expression.Compile()不支持AOT  
**解决**: 在编译时解析Expression树，生成静态SQL

### 2. 属性名映射
**问题**: C#属性名 vs 数据库列名  
**解决**: 
- 默认: snake_case转换
- 支持: `[Column("custom_name")]` 特性

### 3. 多数据库方言
**问题**: 不同数据库的SQL语法差异  
**解决**: 
- LIKE连接: PostgreSQL (`||`) vs SQL Server (`+`)
- 参数占位符: PostgreSQL (`$1`) vs Others (`@p0`)

### 4. 复杂表达式
**问题**: 嵌套的复杂表达式  
**解决**: 递归访问者模式 + 括号处理

---

## 📊 性能考虑

### 编译时生成 vs 运行时解析

**方案A: 编译时生成**（推荐）
```csharp
// 源生成器在编译时解析Expression
// 生成静态SQL，零运行时开销
Task<List<User>> GetActiveUsersAsync()
{
    __cmd__.CommandText = "SELECT * FROM users WHERE age > @p0 AND is_active = @p1";
    // ... 参数绑定
}
```

**方案B: 运行时解析**（灵活但有性能开销）
```csharp
// 运行时解析Expression树
Task<List<User>> GetWhereAsync(Expression<Func<User, bool>> predicate)
{
    var visitor = new SqlExpressionVisitor();
    var (sql, params) = visitor.Generate(predicate);
    // ... 执行查询
}
```

**推荐**: 优先使用方案A（编译时），方案B作为fallback

---

## 🎯 里程碑

### Milestone 1: 基础比较运算 (2h)
- ✅ `==`, `!=`, `>`, `>=`, `<`, `<=`
- ✅ AOT友好
- ✅ 参数化查询

### Milestone 2: 逻辑运算 (1.5h)
- ✅ `&&`, `||`, `!`
- ✅ 括号优先级

### Milestone 3: 字符串操作 (1.5h)
- ✅ `StartsWith`, `EndsWith`, `Contains`
- ✅ 多数据库LIKE语法

### Milestone 4: 集合操作 (1.5h)
- ✅ `Contains` -> `IN`
- ✅ 空集合处理

### Milestone 5: NULL处理 (1h)
- ✅ `IS NULL`, `IS NOT NULL`

**总计**: 6-8小时

---

## 🚀 开始实施

**下一步**: 创建TDD红灯测试（Phase 1: 简单比较运算）

1. 创建测试文件: `tests/Sqlx.Tests/Expression/TDD_Phase1_Comparison_RedTests.cs`
2. 编写失败的测试
3. 实现Expression访问者
4. 集成到源生成器
5. 让测试通过（绿灯）

**准备好开始了吗？** 🎯

