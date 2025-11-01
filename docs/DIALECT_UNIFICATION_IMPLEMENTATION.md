# 多数据库方言统一架构实施文档

## 📋 Phase 1: 占位符系统 ✅ 已完成

### 实现内容
1. **DialectPlaceholders** - 定义10个核心占位符
2. **IDatabaseDialectProvider扩展** - 新增4个方法
3. **BaseDialectProvider实现** - 实现`ReplacePlaceholders()`
4. **4个方言提供者实现** - PostgreSQL, MySQL, SQL Server, SQLite
5. **21个单元测试** - 100%通过

### 占位符列表
```
{{table}}              → 表名 (带方言特定包裹符)
{{columns}}            → 列名列表 (逗号分隔)
{{returning_id}}       → RETURNING/OUTPUT子句
{{bool_true}}          → true/1
{{bool_false}}         → false/0
{{current_timestamp}}  → CURRENT_TIMESTAMP/GETDATE()/NOW()
{{limit}}              → LIMIT/TOP子句
{{offset}}             → OFFSET子句
{{limit_offset}}       → 组合的LIMIT OFFSET
{{concat}}             → 字符串连接 (||/CONCAT/+)
```

## 🎯 Phase 2: 源生成器修改 (进行中)

### 目标
实现"写一次，多数据库运行"架构。用户只需定义一次接口，源生成器自动为每个方言生成适配代码。

### 核心思路

#### 当前架构问题
```csharp
// ❌ 当前：每个方言都要写一次完整的接口
[RepositoryFor(typeof(User), Dialect = SqlDefineTypes.PostgreSql)]
public interface IPostgreSQLUserRepository : IDialectUserRepositoryBase
{
    [SqlTemplateAttribute(@"SELECT * FROM ""dialect_users_postgresql"" WHERE id = @id")]
    new Task<User?> GetByIdAsync(int id, CancellationToken ct);

    [SqlTemplateAttribute(@"INSERT INTO ""dialect_users_postgresql"" ...")]
    new Task<int> InsertAsync(User user, CancellationToken ct);

    // ... 还有18个方法，每个方法都要重复定义
}

[RepositoryFor(typeof(User), Dialect = SqlDefineTypes.MySql)]
public interface IMySQLUserRepository : IDialectUserRepositoryBase
{
    // 又要重复定义20个方法...
}
```

#### 目标架构
```csharp
// ✅ 目标：只写一次基础接口，使用占位符
public interface IDialectUserRepositoryBase
{
    [SqlTemplate(@"SELECT * FROM {{table}} WHERE id = @id")]
    Task<User?> GetByIdAsync(int id, CancellationToken ct);

    [SqlTemplate(@"INSERT INTO {{table}} ({{columns}}) VALUES (@username, @email, @age) {{returning_id}}")]
    Task<int> InsertAsync(User user, CancellationToken ct);

    // ... 只需要定义一次
}

// 只需要一个空类型标识方言
[RepositoryFor(typeof(User), Dialect = SqlDefineTypes.PostgreSql, TableName = "dialect_users_postgresql")]
public class PostgreSQLUserRepository : IDialectUserRepositoryBase { }

[RepositoryFor(typeof(User), Dialect = SqlDefineTypes.MySql, TableName = "dialect_users_mysql")]
public class MySQLUserRepository : IDialectUserRepositoryBase { }

// 源生成器会自动：
// 1. 读取 IDialectUserRepositoryBase 上的所有 SqlTemplate
// 2. 根据 RepositoryFor 的 Dialect 和 TableName
// 3. 替换占位符生成方言特定SQL
// 4. 生成完整的仓储实现
```

### 实施步骤

#### Step 2.1: 分析源生成器架构 ✅
- 找到 `AttributeHandler.cs` - 处理 SqlTemplate 属性复制
- 找到 `CodeGenerationService.cs` - 生成仓储方法
- 找到 `SqlTemplateEngine` - 处理SQL模板

#### Step 2.2: 实现SQL模板继承逻辑
**文件**: `src/Sqlx.Generator/Core/TemplateInheritanceResolver.cs`

```csharp
/// <summary>
/// 从基接口继承SQL模板，并替换占位符
/// </summary>
public class TemplateInheritanceResolver
{
    public List<MethodTemplate> ResolveInheritedTemplates(
        INamedTypeSymbol interfaceSymbol,
        INamedTypeSymbol implementationClass,
        IDatabaseDialectProvider dialectProvider)
    {
        // 1. 遍历接口的所有基接口
        // 2. 收集所有带 SqlTemplate 的方法
        // 3. 使用 dialectProvider.ReplacePlaceholders() 替换占位符
        // 4. 返回处理后的方法模板列表
    }
}
```

#### Step 2.3: 修改代码生成服务
**文件**: `src/Sqlx.Generator/Core/CodeGenerationService.cs`

```csharp
public void GenerateRepositoryClass(...)
{
    // 1. 检查类是否有 RepositoryFor 属性
    var repoFor = GetRepositoryForAttribute(classSymbol);
    if (repoFor == null) return;

    // 2. 获取方言提供者
    var dialectProvider = GetDialectProvider(repoFor.Dialect);

    // 3. 解析继承的模板
    var resolver = new TemplateInheritanceResolver();
    var inheritedTemplates = resolver.ResolveInheritedTemplates(
        interfaceSymbol,
        classSymbol,
        dialectProvider);

    // 4. 为每个继承的方法生成实现
    foreach (var template in inheritedTemplates)
    {
        GenerateRepositoryMethod(template, dialectProvider);
    }
}
```

#### Step 2.4: 扩展 RepositoryForAttribute
**文件**: `src/Sqlx/Annotations/RepositoryForAttribute.cs`

```csharp
public class RepositoryForAttribute : Attribute
{
    public Type EntityType { get; set; }
    public SqlDefineTypes Dialect { get; set; }
    public string? TableName { get; set; }  // 新增：显式指定表名

    // 如果 TableName 为 null，则从 EntityType 推断
}
```

### 测试策略

#### 单元测试
```csharp
[TestClass]
public class TemplateInheritanceTests
{
    [TestMethod]
    public void ResolveInheritedTemplates_WithPlaceholders_ShouldReplaceCorrectly()
    {
        // Given: 基接口有 "SELECT * FROM {{table}}"
        // When: 解析 PostgreSQL 方言，TableName = "users"
        // Then: 生成 "SELECT * FROM \"users\""
    }

    [TestMethod]
    public void MultiDialect_SameInterface_ShouldGenerateDifferentSQL()
    {
        // Given: 同一个基接口
        // When: PostgreSQL vs MySQL
        // Then: {{bool_true}} → true vs 1
    }
}
```

#### 集成测试
```csharp
// 使用真实的源生成器，验证生成的代码
[TestClass]
public class GeneratorIntegrationTests
{
    [TestMethod]
    public void GenerateCode_UnifiedInterface_ShouldCompile()
    {
        // Given: 统一接口定义
        // When: 运行源生成器
        // Then: 生成的代码应该编译通过
    }
}
```

## 📊 进度跟踪

| Phase | 任务 | 状态 | 测试 |
|-------|-----|------|-----|
| 1 | 占位符系统 | ✅ 100% | ✅ 21/21 |
| 2.1 | 架构分析 | ✅ 100% | - |
| 2.2 | 模板继承 | ⏳ 0% | ⏳ 0/5 |
| 2.3 | 生成器修改 | ⏳ 0% | ⏳ 0/8 |
| 2.4 | 集成测试 | ⏳ 0% | ⏳ 0/3 |
| 3 | 测试重构 | ⏳ 0% | ⏳ 0/60 |
| 4 | 文档更新 | ⏳ 0% | - |

**总进度**: ~20%

## 🎯 下一步行动

1. ✅ 创建 `TemplateInheritanceResolver.cs`
2. ✅ 实现 `ResolveInheritedTemplates()` 方法
3. ✅ 编写单元测试验证继承逻辑
4. ✅ 修改 `CodeGenerationService` 集成继承解析器
5. ✅ 运行完整测试套件
6. ✅ 重构现有测试以使用统一接口

---

*最后更新: 2025-11-01*

