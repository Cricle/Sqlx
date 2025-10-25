# 集合支持增强 - 实施计划

**优先级**: ⭐⭐⭐ 高
**预计用时**: 3-4小时
**用户价值**: 极高（IN查询和批量操作是最常见需求）

---

## 🎯 目标

让`Sqlx`完美支持集合参数和批量操作：
1. **IN查询**: `WHERE id IN (@p0, @p1, @p2)`
2. **Expression Contains**: `x => ids.Contains(x.Id)` → IN查询
3. **批量INSERT**: 自动分批处理大数据集
4. **{{values @paramName}}占位符**: IEnumerable参数展开

---

## 📋 功能需求

### 1. IN查询 - 直接参数展开

```csharp
public interface IUserRepository
{
    // 方法1: 数组参数
    [SqlTemplate("SELECT * FROM {{table}} WHERE id IN (@ids)")]
    Task<List<User>> GetByIdsAsync(long[] ids);

    // 方法2: IEnumerable参数
    [SqlTemplate("SELECT * FROM {{table}} WHERE status IN (@statuses)")]
    Task<List<User>> GetByStatusesAsync(IEnumerable<string> statuses);
}

// 使用
var ids = new[] { 1L, 2L, 3L };
var users = await repo.GetByIdsAsync(ids);

// 生成SQL:
// SELECT * FROM users WHERE id IN (@p0, @p1, @p2)
```

### 2. Expression Contains支持

```csharp
public interface IUserRepository
{
    [SqlTemplate("SELECT * FROM {{table}} WHERE {{where @predicate}}")]
    Task<List<User>> GetWhereAsync(Expression<Func<User, bool>> predicate);
}

// 使用
var ids = new[] { 1L, 2L, 3L };
Expression<Func<User, bool>> expr = x => ids.Contains(x.Id);
var users = await repo.GetWhereAsync(expr);

// 生成SQL:
// SELECT * FROM users WHERE id IN (1, 2, 3)
```

### 3. 批量INSERT

```csharp
public interface IUserRepository
{
    [SqlTemplate("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES {{values @entities}}")]
    [BatchOperation(MaxBatchSize = 1000)]
    Task<int> BatchInsertAsync(IEnumerable<User> entities);
}

// 使用
var users = new List<User>();
for (int i = 0; i < 5000; i++)
{
    users.Add(new User { Name = $"User{i}" });
}
await repo.BatchInsertAsync(users);

// 自动分批为5次，每次1000条
// INSERT INTO users (name) VALUES (@name0), (@name1), ..., (@name999)
```

### 4. {{values @paramName}}占位符

```csharp
[SqlTemplate("INSERT INTO {{table}} (name, age) VALUES {{values @users}}")]
Task<int> InsertManyAsync(IEnumerable<User> users);

// 生成:
// INSERT INTO users (name, age) VALUES (@name0, @age0), (@name1, @age1), ...
```

---

## 🔧 实现方案

### Phase 1: IN查询参数展开 (1.5小时)

#### Step 1.1: 检测IEnumerable参数

**位置**: `SharedCodeGenerationUtilities.cs`

```csharp
private static bool IsEnumerableParameter(IParameterSymbol param)
{
    var type = param.Type;

    // 排除string（虽然是IEnumerable<char>）
    if (type.SpecialType == SpecialType.System_String)
        return false;

    // 检查是否实现IEnumerable<T>
    if (type is IArrayTypeSymbol)
        return true;

    if (type is INamedTypeSymbol namedType)
    {
        // IEnumerable<T>, List<T>, T[]等
        return namedType.AllInterfaces.Any(i =>
            i.Name == "IEnumerable" &&
            i.ContainingNamespace.ToDisplayString() == "System.Collections.Generic");
    }

    return false;
}
```

#### Step 1.2: 参数绑定时展开集合

**位置**: `SharedCodeGenerationUtilities.GenerateParameterBinding`

```csharp
public static void GenerateParameterBinding(IndentedStringBuilder sb, IMethodSymbol method)
{
    foreach (var param in method.Parameters)
    {
        if (IsEnumerableParameter(param))
        {
            // 集合参数：展开为多个参数
            sb.AppendLine($"// Expand collection parameter: {param.Name}");
            sb.AppendLine($"int __index_{param.Name} = 0;");
            sb.AppendLine($"foreach (var __item__ in {param.Name})");
            sb.AppendLine("{");
            sb.PushIndent();
            sb.AppendLine($"var __p__ = __cmd__.CreateParameter();");
            sb.AppendLine($"__p__.ParameterName = \"@{param.Name}\" + __index_{param.Name};");
            sb.AppendLine($"__p__.Value = __item__ ?? (object)global::System.DBNull.Value;");
            sb.AppendLine($"__cmd__.Parameters.Add(__p__);");
            sb.AppendLine($"__index_{param.Name}++;");
            sb.PopIndent();
            sb.AppendLine("}");
        }
        else
        {
            // 普通参数：现有逻辑
            // ...
        }
    }
}
```

#### Step 1.3: SQL中的IN子句替换

**位置**: `GenerateCommandSetup`

```csharp
private static void GenerateCommandSetup(IndentedStringBuilder sb, string sql, IMethodSymbol method)
{
    // 检查是否有集合参数需要展开IN子句
    foreach (var param in method.Parameters)
    {
        if (IsEnumerableParameter(param))
        {
            // 替换 IN (@paramName) 为 IN (@paramName0, @paramName1, ...)
            var placeholder = $"IN (@{param.Name})";
            if (sql.Contains(placeholder))
            {
                // 生成动态SQL，运行时替换
                sb.AppendLine($"// Dynamic IN clause for {param.Name}");
                sb.AppendLine($"var __inClause_{param.Name} = string.Join(\", \", ");
                sb.AppendLine($"    {param.Name}.Select((_, i) => \"@{param.Name}\" + i));");
                sb.AppendLine($"var __sql__ = @\"{sql}\".Replace(");
                sb.AppendLine($"    \"IN (@{param.Name})\", ");
                sb.AppendLine($"    \"IN (\" + __inClause_{param.Name} + \")\");");

                sb.AppendLine($"__cmd__.CommandText = __sql__;");
                return;
            }
        }
    }

    // 没有集合参数：静态SQL
    sb.AppendLine($"__cmd__.CommandText = @\"{sql}\";");
}
```

---

### Phase 2: Expression Contains支持 (1小时)

#### Step 2.1: ExpressionToSql增强

**现状**: `ExpressionToSqlBase<T>`已经存在，需要检查是否支持`Contains()`

**检查位置**: `src/Sqlx/ExpressionToSqlBase.cs`

**如果不支持**: 添加`MethodCallExpression`处理

```csharp
protected override Expression VisitMethodCall(MethodCallExpression node)
{
    if (node.Method.Name == "Contains")
    {
        // ids.Contains(x.Id)
        // 转换为: x.Id IN (1, 2, 3)

        var member = Visit(node.Arguments[0]); // x.Id
        var collection = node.Object; // ids

        // 提取集合的值
        var values = GetCollectionValues(collection);

        _sql.Append(member);
        _sql.Append(" IN (");
        _sql.Append(string.Join(", ", values.Select(v => AddParameter(v))));
        _sql.Append(")");

        return node;
    }

    return base.VisitMethodCall(node);
}
```

**如果已支持**: 直接使用，编写测试验证

---

### Phase 3: 批量INSERT支持 (1-1.5小时)

#### Step 3.1: 创建[BatchOperation]特性

**文件**: `src/Sqlx/Annotations/BatchOperationAttribute.cs`

```csharp
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class BatchOperationAttribute : Attribute
{
    /// <summary>
    /// Maximum number of items per batch. Default is 1000.
    /// </summary>
    public int MaxBatchSize { get; set; } = 1000;

    /// <summary>
    /// Maximum number of parameters per batch (database limit).
    /// Default is 2100 for SQL Server.
    /// </summary>
    public int MaxParametersPerBatch { get; set; } = 2100;
}
```

#### Step 3.2: {{values @paramName}}占位符

**位置**: `SqlTemplateEngine.cs`

```csharp
private string ProcessValuesPlaceholder(string type, string options, IMethodSymbol method)
{
    // {{values @entities}}
    if (options?.StartsWith("@") == true)
    {
        var paramName = options.Substring(1);
        var param = method.Parameters.FirstOrDefault(p => p.Name == paramName);

        if (param != null && IsEnumerableParameter(param))
        {
            // 返回运行时标记
            return $"{{{{RUNTIME_BATCH_VALUES_{paramName}}}}}";
        }
    }

    return "VALUES ({{columns}})"; // 默认
}
```

#### Step 3.3: 批量INSERT代码生成

**位置**: `SharedCodeGenerationUtilities.cs`

```csharp
private static void GenerateBatchInsert(IndentedStringBuilder sb, IMethodSymbol method, string paramName)
{
    sb.AppendLine($"// Batch insert for {paramName}");
    sb.AppendLine($"var __batchSize__ = 1000; // From [BatchOperation]");
    sb.AppendLine($"var __batches__ = {paramName}.Chunk(__batchSize__);");
    sb.AppendLine($"int __totalAffected__ = 0;");
    sb.AppendLine();
    sb.AppendLine($"foreach (var __batch__ in __batches__)");
    sb.AppendLine("{");
    sb.PushIndent();

    // 生成VALUES子句
    sb.AppendLine("var __valuesClauses__ = new List<string>();");
    sb.AppendLine("int __itemIndex__ = 0;");
    sb.AppendLine("foreach (var __item__ in __batch__)");
    sb.AppendLine("{");
    sb.PushIndent();
    sb.AppendLine("var __clause__ = $\"(@name{__itemIndex__}, @age{__itemIndex__})\";");
    sb.AppendLine("__valuesClauses__.Add(__clause__);");
    sb.AppendLine("__itemIndex__++;");
    sb.PopIndent();
    sb.AppendLine("}");

    sb.AppendLine("var __values__ = string.Join(\", \", __valuesClauses__);");
    sb.AppendLine("__cmd__.CommandText = __baseSql__ + __values__;");

    // 绑定参数
    sb.AppendLine("__itemIndex__ = 0;");
    sb.AppendLine("foreach (var __item__ in __batch__)");
    sb.AppendLine("{");
    sb.PushIndent();
    sb.AppendLine("// Bind parameters for item");
    sb.AppendLine("{ var __p__ = __cmd__.CreateParameter(); __p__.ParameterName = $\"@name{__itemIndex__}\"; __p__.Value = __item__.Name; __cmd__.Parameters.Add(__p__); }");
    sb.AppendLine("__itemIndex__++;");
    sb.PopIndent();
    sb.AppendLine("}");

    // 执行
    sb.AppendLine("__totalAffected__ += await __cmd__.ExecuteNonQueryAsync();");
    sb.AppendLine("__cmd__.Parameters.Clear();");

    sb.PopIndent();
    sb.AppendLine("}");
    sb.AppendLine("return __totalAffected__;");
}
```

---

## 🧪 TDD测试计划

### Red Phase Tests

#### Test 1: IN查询 - 数组参数展开
```csharp
[TestMethod]
public void IN_Query_Array_Parameter_Should_Expand()
{
    var source = @"
        [SqlTemplate(""SELECT * FROM {{table}} WHERE id IN (@ids)"")]
        Task<List<User>> GetByIdsAsync(long[] ids);
    ";

    var generatedCode = GetCSharpGeneratedOutput(source);

    // 应该展开为多个参数
    StringAssert.Contains(generatedCode, "@ids");
    StringAssert.Contains(generatedCode, "foreach");
    StringAssert.Contains(generatedCode, "IN (");
}
```

#### Test 2: IN查询 - IEnumerable参数
```csharp
[TestMethod]
public void IN_Query_IEnumerable_Should_Work()
{
    var source = @"
        [SqlTemplate(""SELECT * FROM {{table}} WHERE status IN (@statuses)"")]
        Task<List<User>> GetByStatusesAsync(IEnumerable<string> statuses);
    ";

    var generatedCode = GetCSharpGeneratedOutput(source);

    StringAssert.Contains(generatedCode, "IEnumerable");
    StringAssert.Contains(generatedCode, "IN (");
}
```

#### Test 3: Expression Contains转IN
```csharp
[TestMethod]
public void Expression_Contains_Should_Generate_IN()
{
    var source = @"
        [SqlTemplate(""SELECT * FROM {{table}} WHERE {{where @predicate}}"")]
        Task<List<User>> GetWhereAsync(Expression<Func<User, bool>> predicate);
    ";

    var generatedCode = GetCSharpGeneratedOutput(source);

    // 应该使用ExpressionToSql引擎（已有Contains支持）
    StringAssert.Contains(generatedCode, "ExpressionToSql");
}
```

#### Test 4: 批量INSERT基本功能
```csharp
[TestMethod]
public void BatchInsert_Should_Generate_Multiple_VALUES()
{
    var source = @"
        [SqlTemplate(""INSERT INTO {{table}} (name) VALUES {{values @entities}}"")]
        [BatchOperation]
        Task<int> BatchInsertAsync(IEnumerable<User> entities);
    ";

    var generatedCode = GetCSharpGeneratedOutput(source);

    StringAssert.Contains(generatedCode, "VALUES");
    StringAssert.Contains(generatedCode, "Chunk");
}
```

#### Test 5: 批量INSERT自动分批
```csharp
[TestMethod]
public void BatchInsert_Should_Auto_Chunk()
{
    var source = @"
        [SqlTemplate(""INSERT INTO {{table}} (name) VALUES {{values @entities}}"")]
        [BatchOperation(MaxBatchSize = 500)]
        Task<int> BatchInsertAsync(IEnumerable<User> entities);
    ";

    var generatedCode = GetCSharpGeneratedOutput(source);

    StringAssert.Contains(generatedCode, "Chunk");
    StringAssert.Contains(generatedCode, "500");
}
```

#### Test 6: 空集合处理
```csharp
[TestMethod]
public void Empty_Collection_Should_Handle_Gracefully()
{
    var source = @"
        [SqlTemplate(""SELECT * FROM {{table}} WHERE id IN (@ids)"")]
        Task<List<User>> GetByIdsAsync(long[] ids);
    ";

    var generatedCode = GetCSharpGeneratedOutput(source);

    // 应该有空集合检查
    StringAssert.Contains(generatedCode, "Any()");
}
```

#### Test 7: 字符串不被当作集合
```csharp
[TestMethod]
public void String_Parameter_Should_Not_Be_Treated_As_Collection()
{
    var source = @"
        [SqlTemplate(""SELECT * FROM {{table}} WHERE name = @name"")]
        Task<List<User>> GetByNameAsync(string name);
    ";

    var generatedCode = GetCSharpGeneratedOutput(source);

    // string不应该被展开
    Assert.IsFalse(generatedCode.Contains("foreach"));
}
```

#### Test 8: 多数据库支持
```csharp
[TestMethod]
public void IN_Query_Should_Work_Across_Databases()
{
    // PostgreSQL, SQL Server, SQLite都应该支持IN
    // ...
}
```

---

## 📊 实施检查清单

### Phase 1: IN查询支持
- [ ] `IsEnumerableParameter`方法
- [ ] 参数展开逻辑
- [ ] IN子句动态SQL生成
- [ ] 空集合处理
- [ ] TDD红灯测试（3个）
- [ ] TDD绿灯实现
- [ ] 单元测试通过

### Phase 2: Expression Contains
- [ ] 检查ExpressionToSql引擎
- [ ] Contains()方法支持
- [ ] 参数化查询
- [ ] TDD测试（1个）

### Phase 3: 批量INSERT
- [ ] `[BatchOperation]`特性
- [ ] `{{values @paramName}}`占位符
- [ ] Chunk分批逻辑
- [ ] 参数绑定
- [ ] TDD红灯测试（4个）
- [ ] TDD绿灯实现

---

## ⚠️ 注意事项

### 1. 数据库参数限制
- **SQL Server**: 最多2100个参数
- **PostgreSQL**: 最多32767个绑定参数
- **SQLite**: 最多999个参数（默认）
- **MySQL**: 最多65535个占位符

**解决方案**: 自动计算每批大小，避免超出限制

### 2. 空集合处理
```csharp
if (!ids.Any())
{
    // IN ()是非法SQL，需要特殊处理
    // 方案1: 返回空结果
    // 方案2: WHERE 1=0
}
```

### 3. SQL注入防护
所有集合元素必须参数化，不能直接拼接字符串。

### 4. GC优化
- 使用`ArrayPool<T>`减少临时数组分配
- 使用`StringBuilder`构建动态SQL
- 避免不必要的字符串拼接

---

## 🎯 成功标准

- ✅ 8个TDD测试全部通过
- ✅ IN查询正常工作（数组、IEnumerable）
- ✅ Expression Contains正常工作
- ✅ 批量INSERT自动分批
- ✅ 空集合优雅处理
- ✅ 字符串不被误判为集合
- ✅ AOT友好（无反射）
- ✅ GC优化

---

## 📝 使用示例

### 简单IN查询
```csharp
var ids = new[] { 1L, 2L, 3L };
var users = await repo.GetByIdsAsync(ids);
// WHERE id IN (@ids0, @ids1, @ids2)
```

### Expression Contains
```csharp
var ids = new List<long> { 1, 2, 3 };
var users = await repo.GetWhereAsync(x => ids.Contains(x.Id));
// WHERE id IN (1, 2, 3)
```

### 批量INSERT
```csharp
var users = Enumerable.Range(1, 5000)
    .Select(i => new User { Name = $"User{i}" })
    .ToList();

var affected = await repo.BatchInsertAsync(users);
// 自动分5批，每批1000条
// 返回5000
```

---

## 🚀 预期用时

- Phase 1 (IN查询): 1.5小时
- Phase 2 (Expression Contains): 1小时
- Phase 3 (批量INSERT): 1.5小时

**总计**: ~4小时

---

**创建时间**: 2025-10-25
**状态**: 准备开始实施

