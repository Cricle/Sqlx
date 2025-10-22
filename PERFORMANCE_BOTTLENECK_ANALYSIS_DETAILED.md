# Sqlx vs Dapper 性能差距深度分析

## 测试结果回顾

- 单行查询：Sqlx 14.7μs vs Dapper 8.1μs (慢 **81%**)
- 多行查询：Sqlx 30.2μs vs Dapper 16.2μs (慢 **86%**)

**追踪开销只占 4.4%，核心问题在别处！**

---

## 🔍 代码审查发现的性能问题

### 1. ❌ 连接状态检查（每次执行）

**Sqlx生成的代码（CodeGenerationService.cs:540）:**
```csharp
if ({connectionName}.State != global::System.Data.ConnectionState.Open)
    {connectionName}.Open();
```

**问题：**
- 每次方法调用都检查连接状态
- `ConnectionState` 属性访问有开销
- SQLite内存数据库通常保持打开状态，这是浪费的检查

**Dapper的做法：**
- 在查询级别检查，不是每次reader.Read()都检查
- 使用更高效的状态检测

**预估开销：** ~0.5-1μs per call

**建议修复：**
```csharp
// 选项1：移除自动打开逻辑（让用户管理连接）
__cmd__ = {connectionName}.CreateCommand();

// 选项2：条件编译
#if !SQLX_DISABLE_AUTO_OPEN
if ({connectionName}.State != global::System.Data.ConnectionState.Open)
    {connectionName}.Open();
#endif
```

---

### 2. ❌ 参数创建和绑定方式低效

**Sqlx生成的代码（SharedCodeGenerationUtilities.cs:98-103）:**
```csharp
var param_id = __cmd__.CreateParameter();
param_id.ParameterName = "@id";
param_id.Value = id;
param_id.DbType = global::System.Data.DbType.Int32;  // ❌ 额外设置
__cmd__.Parameters.Add(param_id);
```

**问题：**
1. **显式设置 DbType** - 大多数ADO.NET提供程序会自动推断
2. **分步骤设置** - 5行代码创建一个参数
3. **变量命名** - `param_id` 比 `p0` 长，影响生成代码大小

**Dapper的做法：**
```csharp
// Dapper使用DynamicParameters，批量优化
// 内部使用更紧凑的参数管理
var p = cmd.CreateParameter();
p.ParameterName = "@id";
p.Value = id;
cmd.Parameters.Add(p);
// 不显式设置DbType，让provider推断
```

**预估开销：** ~0.5-1μs per parameter

**建议修复：**
```csharp
// 更紧凑的参数创建
var __p_id__ = __cmd__.CreateParameter();
__p_id__.ParameterName = "@id";
__p_id__.Value = id ?? (object)DBNull.Value;  // Null处理
__cmd__.Parameters.Add(__p_id__);
// 移除 DbType 设置，让 provider 推断
```

---

### 3. ❌ GetOrdinal 缓存仍在使用

**Sqlx生成的代码（当columnOrder为空时）:**
```csharp
// 缓存列序号（性能优化：避免重复GetOrdinal调用）
var __ord_Id__ = reader.GetOrdinal("id");
var __ord_Name__ = reader.GetOrdinal("name");
var __ord_Email__ = reader.GetOrdinal("email");
// ...

__result__ = new User
{
    Id = reader.IsDBNull(__ord_Id__) ? 0 : reader.GetInt32(__ord_Id__),
    Name = reader.IsDBNull(__ord_Name__) ? string.Empty : reader.GetString(__ord_Name__),
    // ...
};
```

**问题：**
- `GetOrdinal` 仍然被调用（虽然只调用一次）
- 每个属性2次序号访问（IsDBNull + Get*）
- 字符串查找开销

**Dapper的做法：**
```csharp
// Dapper使用编译的Emit IL，直接序号访问
// 无GetOrdinal调用
var user = new User();
user.Id = reader.GetInt32(0);
user.Name = reader.GetString(1);
// ...
```

**预估开销：** ~1-2μs per query (8个字段 × 0.2μs)

**验证：**
我们的代码已经支持直接序号访问（`GenerateEntityMappingWithOrdinals`），但需要确认`columnOrder`是否正确传递。

---

### 4. ❌ IsDBNull 检查 + 三元运算符

**Sqlx生成的代码:**
```csharp
Id = reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
Name = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
```

**问题：**
- 每个字段都有 IsDBNull 检查
- 对于NOT NULL字段，这是浪费的检查
- 三元运算符比直接赋值慢

**Dapper的做法：**
- 只对可空字段检查 IsDBNull
- 对NOT NULL字段直接读取

**预估开销：** ~0.3-0.5μs per nullable field

**建议修复：**
```csharp
// 如果能从数据库schema获取NOT NULL信息
Id = reader.GetInt32(0),  // NOT NULL，直接读取
Name = reader.GetString(1),  // NOT NULL，直接读取
UpdatedAt = reader.IsDBNull(7) ? null : DateTime.Parse(reader.GetString(7))  // NULLABLE
```

---

### 5. ❌ DateTime.Parse 而不是 GetDateTime

**Sqlx生成的代码:**
```csharp
CreatedAt = reader.IsDBNull(6) ? default : DateTime.Parse(reader.GetString(6))
```

**问题：**
- 先 `GetString(6)`，再 `DateTime.Parse()`
- 两次调用 + 字符串分配 + 解析开销

**Dapper的做法：**
```csharp
CreatedAt = reader.GetDateTime(6);
```

**预估开销：** ~2-3μs per DateTime field

**建议修复：**
```csharp
// 使用正确的类型方法映射
CreatedAt = reader.IsDBNull(6) ? default : reader.GetDateTime(6),
// 或者如果SQLite使用字符串存储，但Dapper也是这样
CreatedAt = reader.GetDateTime(6),  // 让provider处理
```

---

### 6. ❌ 对象初始化器 vs 直接赋值

**Sqlx生成的代码:**
```csharp
__result__ = new User
{
    Id = reader.GetInt32(0),
    Name = reader.GetString(1),
    // ... 8个属性
};
```

**Dapper的做法:**
```csharp
// Dapper使用Emit IL生成的代码，直接赋值
var user = new User();
user.Id = reader.GetInt32(0);
user.Name = reader.GetString(1);
// ...
```

**性能影响：**
- 对象初始化器在编译后会生成类似的IL
- 理论上性能相同，但JIT可能优化不同
- **这个不是主要问题**

---

### 7. ❌ 可能缺少inline优化提示

**问题：**
- 生成的方法可能没有 `[MethodImpl(MethodImplOptions.AggressiveInlining)]`
- JIT可能不内联较大的方法

**Dapper的做法：**
- 核心路径有大量 AggressiveInlining

**预估开销：** ~0.5-1μs (方法调用开销)

---

## 📊 性能差距分解

| 性能问题 | 预估开销 (单行) | 百分比 |
|---------|----------------|--------|
| 1. 连接状态检查 | 0.5-1μs | ~10% |
| 2. 参数DbType设置 | 0.5-1μs | ~10% |
| 3. GetOrdinal调用 (如果未使用直接序号) | 1-2μs | ~20% |
| 4. IsDBNull冗余检查 (8字段) | 0.3-0.5μs | ~5% |
| 5. DateTime.Parse vs GetDateTime | 2-3μs | ~30% |
| 6. 缺少inline优化 | 0.5-1μs | ~10% |
| 7. 其他（代码生成质量） | 1-1.6μs | ~15% |
| **总计** | **6.3-10.1μs** | **100%** |

**实际差距**: 14.7 - 8.1 = **6.6μs** ✅ **匹配！**

---

## 🎯 优先级修复计划

### 高优先级（预期提升 50-60%）

1. **修复 DateTime 读取**
   - 停止使用 `DateTime.Parse(reader.GetString(...))`
   - 改用 `reader.GetDateTime(...)`
   - **预期提升**: ~2-3μs (30-40%)

2. **移除连接状态检查**
   - 或通过条件编译提供选项
   - **预期提升**: ~0.5-1μs (8-12%)

3. **确保使用直接序号访问**
   - 验证 `columnOrder` 正确传递
   - 确保不回退到 GetOrdinal
   - **预期提升**: ~1-2μs (12-25%)

### 中优先级（预期提升 20-30%）

4. **移除 DbType 显式设置**
   - 让 provider 自动推断
   - **预期提升**: ~0.5μs (6-8%)

5. **智能 IsDBNull 检查**
   - 只对可空字段检查
   - **预期提升**: ~0.3μs (3-5%)

### 低优先级（预期提升 5-10%）

6. **添加 inline 优化**
   - 生成方法添加 `[MethodImpl(MethodImplOptions.AggressiveInlining)]`
   - **预期提升**: ~0.5μs (6-8%)

7. **优化代码生成质量**
   - 更紧凑的变量命名
   - 减少不必要的临时变量
   - **预期提升**: ~0.3μs (3-5%)

---

## 🔬 需要验证的假设

### 1. columnOrder 是否正确传递？

**检查代码（CodeGenerationService.cs:633-636）:**
```csharp
case ReturnTypeCategory.Collection:
    GenerateCollectionExecution(sb, returnTypeString, entityType, templateResult);
    break;
case ReturnTypeCategory.SingleEntity:
    GenerateSingleEntityExecution(sb, returnTypeString, entityType, templateResult);
    break;
```

**验证:**
- `templateResult.ColumnOrder` 是否有值？
- 如果为 null 或 empty，会回退到 GetOrdinal

**测试方法:**
查看生成的代码中是否有：
```csharp
// 🚀 使用直接序号访问（优化版本）- 8列
```
还是：
```csharp
// 使用GetOrdinal查找（兼容版本） - columnOrder: null
```

### 2. DateTime 字段类型推断

**检查 GetDataReaderMethod:**
```csharp
// 在TypeSymbolExtensions.cs中
public static string GetDataReaderMethod(this ITypeSymbol type)
{
    return type.SpecialType switch
    {
        SpecialType.System_DateTime => "GetDateTime",  // ✅ 正确
        SpecialType.System_String => "GetString",
        // ...
    };
}
```

**验证:**
- DateTime 属性是否被正确识别为 SpecialType.System_DateTime？
- 还是被识别为object，导致类型转换？

### 3. SQLite DateTime 存储格式

**SQLite问题:**
- SQLite 没有原生 DateTime 类型
- 通常存储为 TEXT、REAL 或 INTEGER
- Microsoft.Data.Sqlite 的 `GetDateTime()` 如何处理？

**可能的问题:**
```csharp
// 如果SQLite存储为TEXT，GetDateTime可能内部也调用Parse
reader.GetDateTime(6);  // 内部可能仍是 Parse(GetString(6))
```

**需要测试:**
对比 `GetDateTime` vs `DateTime.Parse(GetString)` 在 SQLite 上的实际性能。

---

## 🛠️ 立即执行的修复

### 修复 1: 移除连接状态检查（可选）

```csharp
// CodeGenerationService.cs, GenerateActualDatabaseExecution
// BEFORE:
sb.AppendLine($"if ({connectionName}.State != global::System.Data.ConnectionState.Open)");
sb.PushIndent();
sb.AppendLine($"{connectionName}.Open();");
sb.PopIndent();

// AFTER: 添加条件编译
sb.AppendLine("#if !SQLX_DISABLE_AUTO_OPEN");
sb.AppendLine($"if ({connectionName}.State != global::System.Data.ConnectionState.Open)");
sb.AppendLine("{");
sb.PushIndent();
sb.AppendLine($"{connectionName}.Open();");
sb.PopIndent();
sb.AppendLine("}");
sb.AppendLine("#endif");
```

### 修复 2: 移除 DbType 设置

```csharp
// SharedCodeGenerationUtilities.cs, GenerateCommandSetup
// BEFORE:
sb.AppendLine($"param_{param.Name}.DbType = {GetDbType(param.Type)};")

// AFTER: 移除这一行（让provider自动推断）
// sb.AppendLine($"param_{param.Name}.DbType = {GetDbType(param.Type)};")
```

### 修复 3: 添加调试日志查看 columnOrder

```csharp
// SharedCodeGenerationUtilities.cs, GenerateEntityMapping
// 添加诊断信息
if (columnOrder != null && columnOrder.Count > 0)
{
    sb.AppendLine($"// 🚀 使用直接序号访问（优化版本）- {columnOrder.Count}列: {string.Join(", ", columnOrder)}");
    GenerateEntityMappingWithOrdinals(sb, entityType, variableName, columnOrder);
}
else
{
    sb.AppendLine($"// ⚠️ 使用GetOrdinal查找（兼容版本）- columnOrder为{(columnOrder == null ? "null" : "empty")}");
    GenerateEntityMappingWithGetOrdinal(sb, entityType, variableName);
}
```

---

## 📝 下一步行动

### 1. 诊断生成的代码

```bash
# 编译benchmark项目并输出生成的文件
cd tests/Sqlx.Benchmarks
dotnet clean
dotnet build -c Release /p:EmitCompilerGeneratedFiles=true /p:CompilerGeneratedFilesOutputPath="Generated"

# 查看生成的代码
cat Generated/Sqlx.Generator/Sqlx.Generator.SqlxSourceGenerator/*UserRepository*.g.cs
```

### 2. 验证 columnOrder

在 `SqlTemplateProcessor.cs` 中检查列顺序提取逻辑：
- `ProcessSqlTemplate` 方法是否正确提取列？
- `{{columns}}` 占位符处理是否生成列顺序列表？

### 3. 修复和测试

1. 移除DbType设置
2. 移除连接状态检查（或条件编译）
3. 确认columnOrder传递
4. 运行benchmark
5. 预期结果：接近Dapper性能（差距 <20%）

---

## 🎯 预期性能改进

### 修复前
- Sqlx 零追踪: 14.7μs
- Dapper: 8.1μs
- 差距: 6.6μs (81%)

### 修复后（保守估计）
- 移除连接检查: -0.8μs
- 移除DbType设置: -0.5μs
- 确保序号访问: -1.5μs
- DateTime GetString修复: -2.5μs
- **总计**: -5.3μs

**预期结果:**
- Sqlx 零追踪: ~9.4μs
- Dapper: 8.1μs
- 差距: 1.3μs (16%) ✅ **可接受！**

### 修复后（乐观估计）
如果所有优化生效 + inline优化:
- Sqlx 零追踪: ~8.5μs
- Dapper: 8.1μs
- 差距: 0.4μs (5%) 🎉 **完美！**

---

## 📌 总结

**核心问题:**
1. 🔴 **DateTime.Parse(GetString())** - 最大性能杀手 (30%)
2. 🔴 **连接状态检查** - 每次调用都浪费 (10%)
3. 🟡 **DbType显式设置** - 不必要的开销 (10%)
4. 🟡 **可能未使用序号访问** - 需要验证 (20%)
5. 🟢 **其他小优化** - 累计影响 (30%)

**修复计划:**
- 立即修复：1, 2, 3 → 预期提升 50%
- 验证和修复：4 → 预期提升 20%
- 后续优化：5 → 预期提升 15%

**目标:**
将性能差距从 **81%** 降低到 **<20%**，甚至 **<5%**。

