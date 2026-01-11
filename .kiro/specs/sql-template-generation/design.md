# Design Document

## Overview

本设计通过在 Sqlx 源代码生成器中添加基于返回类型的条件代码生成逻辑，实现了一个简单直观的 SQL 调试功能。当 Repository 方法返回 `SqlTemplate` 类型时，生成器将生成只构建 SQL 和参数的代码；当返回其他类型时，生成器将生成正常的数据库执行代码。

这种设计的核心优势是：
- 不需要额外的方法或特性
- 复用现有的 SQL 生成逻辑
- 通过类型系统自然表达意图
- 零运行时开销

## Architecture

### 组件架构

```
┌─────────────────────────────────────────────────────────────┐
│                    CSharpGenerator                          │
│  (Roslyn Source Generator - 编译时执行)                      │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│              MethodGenerationContext                        │
│  - 分析方法签名和返回类型                                     │
│  - 决定生成模式（SqlTemplate vs 执行）                        │
└────────────────────┬────────────────────────────────────────┘
                     │
         ┌───────────┴───────────┐
         ▼                       ▼
┌──────────────────┐    ┌──────────────────┐
│  SqlTemplate     │    │  Execute Mode    │
│  Generation      │    │  Generation      │
│  Mode            │    │  Mode            │
│                  │    │                  │
│  - 构建 SQL      │    │  - 构建 SQL      │
│  - 构建参数字典  │    │  - 构建参数      │
│  - 返回          │    │  - 执行查询      │
│    SqlTemplate   │    │  - 返回结果      │
└──────────────────┘    └──────────────────┘
         │                       │
         └───────────┬───────────┘
                     ▼
         ┌───────────────────────┐
         │   SqlGenerator        │
         │   (复用现有逻辑)       │
         │   - GenerateSelect    │
         │   - GenerateInsert    │
         │   - GenerateUpdate    │
         │   - GenerateDelete    │
         └───────────────────────┘
```

## Components and Interfaces

### 1. 返回类型检测

在 `MethodGenerationContext` 中添加新的返回类型检测：

```csharp
internal enum ReturnTypes
{
    Void,
    Scalar,
    List,
    IEnumerable,
    IAsyncEnumerable,
    ListDictionaryStringObject,
    SqlTemplate,  // 新增
}

private ReturnTypes GetReturnType()
{
    var returnType = MethodSymbol.ReturnType?.UnwrapTaskType();
    
    // 检查是否返回 SqlTemplate
    if (returnType?.Name == "SqlTemplate" && 
        returnType.ContainingNamespace?.ToDisplayString() == "Sqlx")
    {
        return ReturnTypes.SqlTemplate;
    }
    
    // 现有的返回类型检测逻辑...
}
```

### 2. SqlTemplate 生成模式

在 `MethodGenerationContext.DeclareCommand` 中添加 SqlTemplate 生成分支：

```csharp
public bool DeclareCommand(IndentedStringBuilder sb)
{
    // ... 现有的方法签名和连接设置代码 ...
    
    if (DeclareReturnType == ReturnTypes.SqlTemplate)
    {
        return GenerateSqlTemplateReturn(sb);
    }
    
    // ... 现有的执行模式代码 ...
}
```

### 3. SqlTemplate 生成实现

新增方法 `GenerateSqlTemplateReturn`：

```csharp
private bool GenerateSqlTemplateReturn(IndentedStringBuilder sb)
{
    // 1. 生成 SQL 字符串
    var sql = GetSql();
    if (string.IsNullOrEmpty(sql))
    {
        ClassGenerationContext.GeneratorExecutionContext.ReportDiagnostic(
            Diagnostic.Create(Messages.SP0007, MethodSymbol.Locations[0]));
        return false;
    }
    
    // 2. 处理批量插入的占位符
    if (sql.Contains("{{VALUES_PLACEHOLDER}}"))
    {
        return GenerateBatchInsertSqlTemplate(sb, sql);
    }
    
    // 3. 创建参数字典
    sb.AppendLine("var parameters = new global::System.Collections.Generic.Dictionary<string, object?>();");
    sb.AppendLine();
    
    // 4. 添加参数到字典
    foreach (var param in SqlParameters)
    {
        var isScalarType = param.Type.IsCachedScalarType();
        if (isScalarType)
        {
            AddParameterToDictionary(sb, param, param.Type, string.Empty);
        }
        else
        {
            // 处理复杂类型参数
            foreach (var property in param.Type.GetMembers().OfType<IPropertySymbol>())
            {
                AddParameterToDictionary(sb, property, property.Type, param.Name);
            }
        }
    }
    
    // 5. 返回 SqlTemplate
    sb.AppendLine($"return new global::Sqlx.SqlTemplate({sql}, parameters);");
    
    return true;
}

private void AddParameterToDictionary(
    IndentedStringBuilder sb, 
    ISymbol symbol, 
    ITypeSymbol type, 
    string prefix)
{
    var paramName = symbol.GetParameterName(SqlDef.ParameterPrefix);
    var visitPath = string.IsNullOrEmpty(prefix) 
        ? symbol.Name 
        : $"{prefix}?.{symbol.Name}";
    
    sb.AppendLine($"parameters[\"{paramName}\"] = {visitPath};");
}
```

### 4. 批量插入的 SqlTemplate 支持

```csharp
private bool GenerateBatchInsertSqlTemplate(IndentedStringBuilder sb, string sqlTemplate)
{
    var collectionParameter = SqlParameters.FirstOrDefault(p => !p.Type.IsCachedScalarType());
    if (collectionParameter == null)
    {
        // 报告错误
        return false;
    }
    
    // 生成批量插入的 SQL 和参数
    var objectMap = new ObjectMap(collectionParameter);
    var baseSql = sqlTemplate.Replace("{{VALUES_PLACEHOLDER}}", "");
    
    sb.AppendLine($"var baseSql = \"{baseSql.Trim('"')}\";");
    sb.AppendLine("var sqlBuilder = new global::System.Text.StringBuilder(baseSql);");
    sb.AppendLine("var parameters = new global::System.Collections.Generic.Dictionary<string, object?>();");
    sb.AppendLine("var paramIndex = 0;");
    sb.AppendLine("var isFirst = true;");
    sb.AppendLine();
    
    sb.AppendLine($"foreach (var item in {collectionParameter.Name})");
    sb.AppendLine("{");
    sb.PushIndent();
    
    sb.AppendLine("if (!isFirst) sqlBuilder.Append(\", \");");
    sb.AppendLine("else isFirst = false;");
    sb.AppendLine("sqlBuilder.Append(\"(\");");
    
    var properties = objectMap.Properties;
    for (int i = 0; i < properties.Count; i++)
    {
        var property = properties[i];
        var paramName = $"{SqlDef.ParameterPrefix}{property.GetParameterName(string.Empty)}_{{paramIndex}}";
        
        if (i > 0) sb.AppendLine("sqlBuilder.Append(\", \");");
        sb.AppendLine($"sqlBuilder.Append(\"{paramName}\");");
        sb.AppendLine($"parameters[\"{paramName}\"] = item.{property.Name};");
    }
    
    sb.AppendLine("sqlBuilder.Append(\")\");");
    sb.AppendLine("paramIndex++;");
    
    sb.PopIndent();
    sb.AppendLine("}");
    sb.AppendLine();
    
    sb.AppendLine("return new global::Sqlx.SqlTemplate(sqlBuilder.ToString(), parameters);");
    
    return true;
}
```

## Data Models

### SqlTemplate (现有)

```csharp
public readonly record struct SqlTemplate(
    string Sql, 
    IReadOnlyDictionary<string, object?> Parameters)
{
    public ParameterizedSql Execute(IReadOnlyDictionary<string, object?>? parameters = null);
    public SqlTemplateBuilder Bind();
}
```

### 使用示例

```csharp
// 定义 Repository 接口
[RepositoryFor<User>]
public partial interface IUserRepository
{
    // 返回 SqlTemplate - 只生成 SQL，不执行
    [Sqlx("SELECT * FROM Users WHERE Id = @id")]
    SqlTemplate GetUserByIdSql(int id);
    
    // 返回 User - 正常执行查询
    [Sqlx("SELECT * FROM Users WHERE Id = @id")]
    User? GetUserById(int id);
    
    // 批量插入 - 返回 SqlTemplate
    [Sqlx("INSERT INTO Users (Name, Email) VALUES")]
    SqlTemplate InsertUsersSql(List<User> users);
}

// 使用
var repo = new UserRepository(connection);

// 调试模式 - 只生成 SQL
var template = repo.GetUserByIdSql(123);
Console.WriteLine(template.Sql);  // SELECT * FROM Users WHERE Id = @id
Console.WriteLine(template.Parameters["@id"]);  // 123

// 可以渲染为最终 SQL
var rendered = template.Execute().Render();
Console.WriteLine(rendered);  // SELECT * FROM Users WHERE Id = 123

// 执行模式 - 正常查询
var user = repo.GetUserById(123);
```

## Correctness Properties

*属性是一个特征或行为，应该在系统的所有有效执行中保持为真——本质上是关于系统应该做什么的正式陈述。属性作为人类可读规范和机器可验证正确性保证之间的桥梁。*

### Property 1: SQL 一致性

*对于任何* Repository 方法，如果该方法有两个版本（一个返回 SqlTemplate，一个返回实体类型），并且使用相同的参数调用，那么 SqlTemplate 版本生成的 SQL 应该与执行版本使用的 SQL 完全相同。

**Validates: Requirements 1.4, 4.1, 4.4**

### Property 2: 参数完整性

*对于任何* 返回 SqlTemplate 的方法和任意参数值，生成的 SqlTemplate.Parameters 字典应该包含 SQL 中所有参数占位符对应的键值对。

**Validates: Requirements 1.2, 1.5, 3.2, 3.3**

### Property 3: 类型安全性

*对于任何* 返回 SqlTemplate 的方法，如果传入的参数类型不匹配方法签名，编译器应该报告类型错误。

**Validates: Requirements 6.1, 6.2**

### Property 4: 方言一致性

*对于任何* 配置了 SqlDialect 的 Repository，返回 SqlTemplate 的方法生成的 SQL 应该使用正确的列名包装符和参数前缀。

**Validates: Requirements 5.1, 5.2, 5.3**

### Property 5: 无副作用

*对于任何* 返回 SqlTemplate 的方法，调用该方法不应该产生任何数据库连接、查询执行或其他副作用。

**Validates: Requirements 1.1, 1.5**

### Property 6: 批量操作支持

*对于任何* 批量插入方法（包含 VALUES_PLACEHOLDER），如果返回 SqlTemplate，生成的 SQL 应该包含所有集合元素的 VALUES 子句，参数字典应该包含所有元素的所有属性值。

**Validates: Requirements 2.5**

### Property 7: 向后兼容性

*对于任何* 现有的非 SqlTemplate 返回类型的方法，添加 SqlTemplate 支持后，这些方法的行为和生成的代码应该保持不变。

**Validates: Requirements 8.1, 8.2, 8.3**

## Error Handling

### 编译时错误

1. **缺少 SQL 定义**
   - 条件：方法返回 SqlTemplate 但没有 SQL 定义
   - 处理：报告诊断错误 SP0007
   - 消息："Method must have SQL definition"

2. **批量操作缺少集合参数**
   - 条件：SQL 包含 VALUES_PLACEHOLDER 但没有集合参数
   - 处理：报告诊断错误
   - 消息："Batch INSERT requires a collection parameter"

3. **不支持的返回类型组合**
   - 条件：异步方法返回 SqlTemplate（应该返回 Task<SqlTemplate>）
   - 处理：报告诊断警告
   - 消息："Async methods should return Task<SqlTemplate>"

### 运行时错误

SqlTemplate 生成模式不应该产生运行时错误，因为它不执行数据库操作。所有错误应该在编译时捕获。

## Testing Strategy

### TDD 开发方法

本功能将采用测试驱动开发（TDD）方法：
1. **先写测试** - 在实现功能之前编写测试用例
2. **红-绿-重构** - 测试失败 → 实现功能 → 测试通过 → 重构代码
3. **小步迭代** - 每次只实现一个小功能，确保测试通过后再继续

### 单元测试（先写）

1. **返回类型检测测试**
   ```csharp
   [Test]
   public void GetReturnType_WhenReturnsSqlTemplate_ReturnsSqlTemplateType()
   {
       // 先写测试，验证返回类型检测逻辑
       // 然后实现 GetReturnType() 中的 SqlTemplate 检测
   }
   ```

2. **SQL 生成测试**
   ```csharp
   [Test]
   public void GenerateSqlTemplateReturn_SimpleQuery_GeneratesCorrectSql()
   {
       // 测试简单查询的 SQL 生成
   }
   
   [Test]
   public void GenerateSqlTemplateReturn_WithParameters_GeneratesParameterDictionary()
   {
       // 测试带参数的查询
   }
   
   [Test]
   public void GenerateBatchInsertSqlTemplate_WithCollection_GeneratesValuesClause()
   {
       // 测试批量插入的 SQL 生成
   }
   ```

3. **参数字典生成测试**
   ```csharp
   [Test]
   public void AddParameterToDictionary_ScalarParameter_AddsToDict()
   {
       // 测试标量参数
   }
   
   [Test]
   public void AddParameterToDictionary_ComplexObject_AddsAllProperties()
   {
       // 测试复杂对象参数
   }
   ```

4. **方言支持测试**
   ```csharp
   [Test]
   public void GenerateSqlTemplateReturn_SqlServerDialect_UsesCorrectSyntax()
   {
       // 测试 SQL Server 方言
   }
   
   [Test]
   public void GenerateSqlTemplateReturn_MySqlDialect_UsesBackticks()
   {
       // 测试 MySQL 方言
   }
   
   [Test]
   public void GenerateSqlTemplateReturn_PostgreSqlDialect_UsesDollarPrefix()
   {
       // 测试 PostgreSQL 方言
   }
   ```

### 属性测试（验证通用规则）

1. **SQL 一致性属性测试**
   ```csharp
   [Property]
   public Property SqlTemplate_And_Execute_GenerateSameSql()
   {
       // 对于任意方法定义和参数
       // SqlTemplate 版本和执行版本的 SQL 应该相同
   }
   ```

2. **参数完整性属性测试**
   ```csharp
   [Property]
   public Property SqlTemplate_ContainsAllParameters()
   {
       // 对于任意参数组合
       // 生成的字典应该包含所有参数
   }
   ```

3. **方言一致性属性测试**
   ```csharp
   [Property]
   public Property SqlTemplate_RespectsDialect()
   {
       // 对于任意方言配置
       // 生成的 SQL 应该使用正确的语法
   }
   ```

### 集成测试（验证端到端功能）

1. **端到端测试**
   ```csharp
   [Test]
   public void EndToEnd_SqlTemplateMethod_CanBeRendered()
   {
       // 创建完整的 Repository
       // 调用 SqlTemplate 方法
       // 验证可以渲染为可执行 SQL
   }
   ```

2. **与现有功能集成测试**
   ```csharp
   [Test]
   public void Integration_SqlTemplateAndExecute_CoexistCorrectly()
   {
       // 测试两种模式共存
   }
   ```

### TDD 实施步骤

1. **第一轮：返回类型检测**
   - 写测试：`GetReturnType_WhenReturnsSqlTemplate_ReturnsSqlTemplateType`
   - 实现：在 `GetReturnType()` 中添加 SqlTemplate 检测
   - 验证：测试通过

2. **第二轮：简单 SQL 生成**
   - 写测试：`GenerateSqlTemplateReturn_SimpleQuery_GeneratesCorrectSql`
   - 实现：`GenerateSqlTemplateReturn()` 基本框架
   - 验证：测试通过

3. **第三轮：参数字典**
   - 写测试：`AddParameterToDictionary_ScalarParameter_AddsToDict`
   - 实现：`AddParameterToDictionary()` 方法
   - 验证：测试通过

4. **第四轮：批量操作**
   - 写测试：`GenerateBatchInsertSqlTemplate_WithCollection_GeneratesValuesClause`
   - 实现：`GenerateBatchInsertSqlTemplate()` 方法
   - 验证：测试通过

5. **第五轮：方言支持**
   - 写测试：各种方言的测试
   - 验证：现有方言逻辑已支持
   - 确认：测试通过

6. **第六轮：属性测试**
   - 写属性测试：验证通用规则
   - 运行：确保所有属性都满足
   - 修复：如果发现问题，修复并重新测试

## Implementation Notes

### 代码生成位置

修改文件：`src/Sqlx.Generator/MethodGenerationContext.cs`

主要修改点：
1. `GetReturnType()` - 添加 SqlTemplate 检测
2. `DeclareCommand()` - 添加 SqlTemplate 分支
3. 新增 `GenerateSqlTemplateReturn()` 方法
4. 新增 `GenerateBatchInsertSqlTemplate()` 方法
5. 新增 `AddParameterToDictionary()` 辅助方法

### 复用现有逻辑

- `GetSql()` - 复用现有的 SQL 获取逻辑
- `SqlDef` - 复用现有的方言配置
- `SqlParameters` - 复用现有的参数处理
- `SqlGenerator` - 间接复用（通过 GetSql）

### 性能考虑

- SqlTemplate 生成不涉及数据库操作，性能开销极小
- 参数字典构建使用 Dictionary 初始化，避免多次重分配
- 批量插入使用 StringBuilder，避免字符串拼接开销

### 向后兼容性

- 不修改现有的 SqlTemplate 类
- 不修改现有的执行模式代码生成
- 只添加新的代码生成分支
- 现有测试应该全部通过
