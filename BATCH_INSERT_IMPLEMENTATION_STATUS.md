# 批量INSERT实施状态

**日期**: 2025-10-25  
**状态**: Phase 3 进行中（~30%完成）

---

## 🎯 目标

实现批量INSERT功能，支持：
1. `{{values @paramName}}`占位符
2. `[BatchOperation]`特性自动分批
3. 返回总受影响行数

---

## ✅ 已完成

### 1. 特性定义 ✅
- `src/Sqlx/Annotations/BatchOperationAttribute.cs`
- MaxBatchSize属性
- MaxParametersPerBatch属性

### 2. TDD红灯测试 ✅
- `tests/Sqlx.Tests/CollectionSupport/TDD_Phase3_BatchInsert_RedTests.cs`
- 4个测试（2通过，2失败）

**通过的测试**:
- ✅ VALUES子句基础检测
- ✅ 空集合处理检测

**失败的测试**（待实现）:
- ❌ 自动分批逻辑
- ❌ 返回总受影响行数

---

## 🔧 待实现（70%）

### 1. SqlTemplateEngine修改

**问题发现**:
当前生成的SQL:
```sql
INSERT INTO user (*) VALUES @entities
```

**期望的SQL**:
```sql
INSERT INTO user (name, age) VALUES (@name0, @age0), (@name1, @age1), ...
```

**需要修改**:
1. `{{columns --exclude Id}}`应生成`name, age`
2. `{{values @entities}}`应识别为批量操作标记
3. 返回特殊标记供代码生成器处理

**修改位置**: `src/Sqlx.Generator/Core/SqlTemplateEngine.cs`

```csharp
private string ProcessValuesPlaceholder(string type, string options, IMethodSymbol method)
{
    // {{values @paramName}}
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
    
    return "VALUES"; // 默认
}
```

---

### 2. CodeGenerationService修改

**检测批量操作**:
```csharp
// 检查SQL中是否有RUNTIME_BATCH_VALUES标记
bool hasBatchValues = sql.Contains("{RUNTIME_BATCH_VALUES_");

// 检查[BatchOperation]特性
var batchOpAttr = method.GetAttributes()
    .FirstOrDefault(a => a.AttributeClass?.Name == "BatchOperationAttribute");

if (hasBatchValues && batchOpAttr != null)
{
    // 生成批量INSERT代码
    GenerateBatchInsertCode(sb, sql, method, batchOpAttr);
}
```

**生成批量INSERT代码**:
```csharp
private static void GenerateBatchInsertCode(
    IndentedStringBuilder sb, 
    string sql, 
    IMethodSymbol method,
    AttributeData batchOpAttr)
{
    // 1. 获取MaxBatchSize
    var maxBatchSize = GetMaxBatchSize(batchOpAttr); // 默认1000
    
    // 2. 提取参数名（从RUNTIME_BATCH_VALUES_xxx中）
    var paramName = ExtractBatchParamName(sql);
    var param = method.Parameters.First(p => p.Name == paramName);
    var entityType = GetEntityType(param);
    
    // 3. 获取要插入的列
    var properties = GetInsertableProperties(entityType, sql);
    
    // 4. 生成分批逻辑
    sb.AppendLine($"int __totalAffected__ = 0;");
    sb.AppendLine($"var __batches__ = {paramName}.Chunk({maxBatchSize});");
    sb.AppendLine();
    sb.AppendLine($"foreach (var __batch__ in __batches__)");
    sb.AppendLine("{");
    sb.PushIndent();
    
    // 5. 构建VALUES子句
    GenerateValuesClause(sb, properties, "__batch__");
    
    // 6. 绑定参数
    GenerateBatchParameterBinding(sb, properties, "__batch__");
    
    // 7. 执行并累加
    sb.AppendLine("__totalAffected__ += __cmd__.ExecuteNonQuery();");
    sb.AppendLine("__cmd__.Parameters.Clear();");
    
    sb.PopIndent();
    sb.AppendLine("}");
    sb.AppendLine();
    sb.AppendLine("return __totalAffected__;");
}
```

**生成VALUES子句**:
```csharp
private static void GenerateValuesClause(
    IndentedStringBuilder sb,
    List<IPropertySymbol> properties,
    string batchVarName)
{
    sb.AppendLine("// Build VALUES clause");
    sb.AppendLine("var __valuesClauses__ = new List<string>();");
    sb.AppendLine("int __itemIndex__ = 0;");
    sb.AppendLine($"foreach (var __item__ in {batchVarName})");
    sb.AppendLine("{");
    sb.PushIndent();
    
    // 生成：(@name0, @age0)
    var paramPlaceholders = properties.Select(p => 
        $"@{ConvertToSnakeCase(p.Name)}{{__itemIndex__}}");
    var valuesClause = string.Join(", ", paramPlaceholders);
    
    sb.AppendLine($"__valuesClauses__.Add($\"({valuesClause})\");");
    sb.AppendLine("__itemIndex__++;");
    sb.PopIndent();
    sb.AppendLine("}");
    sb.AppendLine();
    sb.AppendLine("var __values__ = string.Join(\", \", __valuesClauses__);");
    
    // 替换SQL中的标记
    sb.AppendLine("__cmd__.CommandText = __baseSql__.Replace(\"{RUNTIME_BATCH_VALUES_xxx}\", __values__);");
}
```

**生成参数绑定**:
```csharp
private static void GenerateBatchParameterBinding(
    IndentedStringBuilder sb,
    List<IPropertySymbol> properties,
    string batchVarName)
{
    sb.AppendLine("// Bind parameters");
    sb.AppendLine("__itemIndex__ = 0;");
    sb.AppendLine($"foreach (var __item__ in {batchVarName})");
    sb.AppendLine("{");
    sb.PushIndent();
    
    foreach (var prop in properties)
    {
        var sqlName = ConvertToSnakeCase(prop.Name);
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine("var __p__ = __cmd__.CreateParameter();");
        sb.AppendLine($"__p__.ParameterName = $\"@{sqlName}{{__itemIndex__}}\";");
        sb.AppendLine($"__p__.Value = __item__.{prop.Name} ?? (object)DBNull.Value;");
        sb.AppendLine("__cmd__.Parameters.Add(__p__);");
        sb.PopIndent();
        sb.AppendLine("}");
    }
    
    sb.AppendLine("__itemIndex__++;");
    sb.PopIndent();
    sb.AppendLine("}");
}
```

---

## 📝 预期生成代码示例

```csharp
public Task<int> BatchInsertAsync(IEnumerable<User> entities)
{
    int __totalAffected__ = 0;
    var __batches__ = entities.Chunk(500); // MaxBatchSize
    
    foreach (var __batch__ in __batches__)
    {
        __cmd__ = connection.CreateCommand();
        
        // Build VALUES clause
        var __valuesClauses__ = new List<string>();
        int __itemIndex__ = 0;
        foreach (var __item__ in __batch__)
        {
            __valuesClauses__.Add($"(@name{__itemIndex__}, @age{__itemIndex__})");
            __itemIndex__++;
        }
        var __values__ = string.Join(", ", __valuesClauses__);
        
        __cmd__.CommandText = $"INSERT INTO user (name, age) VALUES {__values__}";
        
        // Bind parameters
        __itemIndex__ = 0;
        foreach (var __item__ in __batch__)
        {
            {
                var __p__ = __cmd__.CreateParameter();
                __p__.ParameterName = $"@name{__itemIndex__}";
                __p__.Value = __item__.Name ?? (object)DBNull.Value;
                __cmd__.Parameters.Add(__p__);
            }
            {
                var __p__ = __cmd__.CreateParameter();
                __p__.ParameterName = $"@age{__itemIndex__}";
                __p__.Value = __item__.Age;
                __cmd__.Parameters.Add(__p__);
            }
            __itemIndex__++;
        }
        
        __totalAffected__ += __cmd__.ExecuteNonQuery();
        __cmd__.Parameters.Clear();
    }
    
    return Task.FromResult(__totalAffected__);
}
```

---

## ⚠️ 复杂度分析

**为什么这个功能复杂**:
1. **占位符解析**: 需要在SqlTemplateEngine中添加新的占位符类型
2. **实体属性提取**: 需要获取要插入的列（考虑--exclude等选项）
3. **批量SQL构建**: 动态生成`VALUES (@p0, @p1), (@p2, @p3), ...`
4. **参数绑定**: 每个批次、每个实体、每个属性都需要绑定参数
5. **分批逻辑**: 检测[BatchOperation]并使用Chunk
6. **累加结果**: 多个批次的结果需要累加

**预计完成时间**: 2-3小时
- SqlTemplateEngine修改: 30分钟
- CodeGenerationService修改: 90分钟
- 测试和调试: 60分钟

---

## 🚀 下次继续步骤

### 步骤1: 修改SqlTemplateEngine
**文件**: `src/Sqlx.Generator/Core/SqlTemplateEngine.cs`
- 添加`ProcessValuesPlaceholder`方法
- 返回`{RUNTIME_BATCH_VALUES_paramName}`标记

### 步骤2: 修改CodeGenerationService
**文件**: `src/Sqlx.Generator/Core/CodeGenerationService.cs`
- 检测`RUNTIME_BATCH_VALUES`标记
- 检测`[BatchOperation]`特性
- 调用`GenerateBatchInsertCode`

### 步骤3: 实现批量INSERT生成
**文件**: `src/Sqlx.Generator/Core/CodeGenerationService.cs`
- `GenerateBatchInsertCode`
- `GenerateValuesClause`
- `GenerateBatchParameterBinding`
- `GetMaxBatchSize`

### 步骤4: 测试
- 运行TDD测试
- 调试生成的代码
- 确保4/4测试通过

---

## 📊 当前状态

- **完成度**: ~30%
- **剩余工作**: ~70%
- **下次会话**: 继续实施上述步骤

---

**创建时间**: 2025-10-25  
**状态**: 进行中  
**预计下次会话**: 2-3小时完成

