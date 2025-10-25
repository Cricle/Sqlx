# Expression参数支持 - 实现方案

**发现时间**: 2025-10-25  
**当前进度**: 90%现有实现 + 需要10%桥接

---

## 🔍 现有实现分析

### 工作流程

1. **SQL模板解析** (`SqlTemplateEngine.ProcessWherePlaceholder`)
   ```csharp
   {{where @predicate}} → {RUNTIME_WHERE_predicate}
   ```

2. **代码生成** (`SharedCodeGenerationUtilities.GenerateDynamicSql`)
   ```csharp
   if (markerContent.StartsWith("EXPR_"))
   {
       var paramName = markerContent.Substring(5);
       sb.AppendLine($"var {varName} = {paramName}?.ToWhereClause() ?? \"1=1\";");
   }
   ```

3. **期望参数类型**:
   ```csharp
   Task<List<User>> GetUsersAsync([ExpressionToSql] ExpressionToSqlBase<User> whereExpr);
   ```

---

## 🎯 目标

支持直接使用`Expression<Func<T, bool>>`而无需手动创建`ExpressionToSqlBase<T>`：

```csharp
// 目标用法
Task<List<User>> GetWhereAsync(Expression<Func<User, bool>> predicate);

// 调用
await repo.GetWhereAsync(u => u.Age > 18);
```

---

## 🛠️ 实现方案

### 步骤1: 检测Expression参数类型

在`ProcessWherePlaceholder`中添加检测：

```csharp
// 在SqlTemplateEngine.cs的ProcessWherePlaceholder中
// 检测Expression<Func<T, bool>>类型的参数
var expressionParam = method.Parameters.FirstOrDefault(p =>
{
    if (p.Type is INamedTypeSymbol namedType &&
        namedType.Name == "Expression" &&
        namedType.ContainingNamespace.ToDisplayString() == "System.Linq.Expressions")
    {
        // 验证是Expression<Func<T, bool>>
        if (namedType.TypeArguments.Length > 0 &&
            namedType.TypeArguments[0] is INamedTypeSymbol funcType &&
            funcType.Name == "Func" &&
            funcType.TypeArguments.Length == 2 &&
            funcType.TypeArguments[1].SpecialType == SpecialType.System_Boolean)
        {
            return true;
        }
    }
    return false;
});

if (expressionParam != null)
{
    // 新标记: NATIVE_EXPR_
    return $"{{RUNTIME_WHERE_NATIVE_EXPR_{expressionParam.Name}}}";
}
```

### 步骤2: 生成桥接代码

在`GenerateDynamicSql`中处理新标记：

```csharp
else if (markerContent.StartsWith("NATIVE_EXPR_"))
{
    // Native Expression<Func<T, bool>> parameter
    var paramName = markerContent.Substring(12);
    
    // 获取实体类型
    var param = method.Parameters.FirstOrDefault(p => p.Name == paramName);
    var entityType = ExtractEntityTypeFromExpression(param.Type);
    var dialectValue = GetDialectForMethod(method);
    
    sb.AppendLine($"// Convert Expression<Func<{entityType.Name}, bool>> to SQL");
    sb.AppendLine($"var __expr_{paramName}__ = new global::Sqlx.ExpressionToSql<{entityType.ToDisplayString()}>(global::Sqlx.SqlDialect.{dialectValue});");
    sb.AppendLine($"__expr_{paramName}__.Where({paramName});");
    sb.AppendLine($"var {varName} = __expr_{paramName}__.ToWhereClause();");
    
    // 绑定参数
    sb.AppendLine($"foreach (var __p__ in __expr_{paramName}__.GetParameters())");
    sb.AppendLine("{");
    sb.PushIndent();
    sb.AppendLine("var __param__ = __cmd__.CreateParameter();");
    sb.AppendLine("__param__.ParameterName = __p__.Key;");
    sb.AppendLine("__param__.Value = __p__.Value ?? global::System.DBNull.Value;");
    sb.AppendLine("__cmd__.Parameters.Add(__param__);");
    sb.PopIndent();
    sb.AppendLine("}");
}
```

### 步骤3: 辅助方法

```csharp
private static INamedTypeSymbol ExtractEntityTypeFromExpression(ITypeSymbol expressionType)
{
    // Expression<Func<TEntity, bool>>
    //                  ^^^^^^^ extract this
    if (expressionType is INamedTypeSymbol namedType &&
        namedType.TypeArguments.Length > 0 &&
        namedType.TypeArguments[0] is INamedTypeSymbol funcType &&
        funcType.TypeArguments.Length > 0)
    {
        return (INamedTypeSymbol)funcType.TypeArguments[0];
    }
    throw new InvalidOperationException("Cannot extract entity type from Expression parameter");
}

private static string GetDialectForMethod(IMethodSymbol method)
{
    var classSymbol = method.ContainingType;
    var sqlDefineAttr = classSymbol.GetAttributes()
        .FirstOrDefault(a => a.AttributeClass?.Name == "SqlDefineAttribute");
    
    if (sqlDefineAttr != null && sqlDefineAttr.ConstructorArguments.Length > 0)
    {
        return sqlDefineAttr.ConstructorArguments[0].Value.ToString();
    }
    
    return "SqlServer"; // Default
}
```

---

## 🧪 测试验证

修改后，以下代码应该工作：

```csharp
[SqlDefine(SqlDefineTypes.PostgreSql)]
public interface IUserRepository
{
    [SqlTemplate("SELECT * FROM {{table}} WHERE {{where @predicate}}")]
    Task<List<User>> GetWhereAsync(Expression<Func<User, bool>> predicate);
}

// 生成的代码
public async Task<List<User>> GetWhereAsync(Expression<Func<User, bool>> predicate)
{
    __cmd__ = _connection.CreateCommand();
    
    // Convert Expression<Func<User, bool>> to SQL
    var __expr_predicate__ = new global::Sqlx.ExpressionToSql<User>(global::Sqlx.SqlDialect.PostgreSQL);
    __expr_predicate__.Where(predicate);
    var __whereClause_0__ = __expr_predicate__.ToWhereClause();
    
    foreach (var __p__ in __expr_predicate__.GetParameters())
    {
        var __param__ = __cmd__.CreateParameter();
        __param__.ParameterName = __p__.Key;
        __param__.Value = __p__.Value ?? global::System.DBNull.Value;
        __cmd__.Parameters.Add(__param__);
    }
    
    __cmd__.CommandText = $@"SELECT * FROM users WHERE {__whereClause_0__}";
    
    // ... Execute and map results
}
```

---

## ✅ 实现清单

### Phase 1: 核心桥接 (2小时)
- [ ] 修改`ProcessWherePlaceholder`检测`Expression<Func<T, bool>>`
- [ ] 添加`NATIVE_EXPR_`标记处理
- [ ] 修改`GenerateDynamicSql`生成桥接代码
- [ ] 添加辅助方法`ExtractEntityTypeFromExpression`
- [ ] 添加辅助方法`GetDialectForMethod`

### Phase 2: 测试验证 (1小时)
- [ ] 修复失败的测试
- [ ] 添加更多测试（逻辑运算、字符串等）
- [ ] 验证参数绑定正确性

### Phase 3: 增强功能 (1.5小时)
- [ ] 支持智能方法名（`GetWhereAsync`无需{{where}}）
- [ ] 错误处理和友好提示
- [ ] 性能优化

### Phase 4: 文档 (0.5小时)
- [ ] 更新使用文档
- [ ] 添加示例
- [ ] 性能对比

---

## 🎯 预期成果

### 用法对比

**旧方式（仍然支持）**:
```csharp
var expr = new ExpressionToSql<User>().Where(u => u.Age > 18);
await repo.GetUsersAsync(expr);
```

**新方式（更简洁）**:
```csharp
await repo.GetWhereAsync(u => u.Age > 18);
```

### 向后兼容

- ✅ 现有的`[ExpressionToSql]`特性继续工作
- ✅ 现有的`ExpressionToSqlBase<T>`参数继续工作
- ✅ 新增的`Expression<Func<T, bool>>`参数是额外支持

---

## 📊 时间估算

- **步骤1**: 1小时（参数检测）
- **步骤2**: 1小时（桥接代码生成）
- **步骤3**: 0.5小时（辅助方法）
- **测试验证**: 1小时
- **增强功能**: 1.5小时
- **文档**: 0.5小时

**总计**: 5.5小时

---

## 🚀 开始实施

下一步: 修改`SqlTemplateEngine.ProcessWherePlaceholder`方法添加Expression检测逻辑。

