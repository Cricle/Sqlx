# 设计文档：多结果集支持

## 概述

本设计实现了通过元组返回值支持多结果集的功能，并使用 `[ResultSetMapping]` 特性明确指定每个结果集与元组元素的对应关系。这与现有的输出参数功能（Output Parameters）是互补的两种机制。

## 架构

### 核心组件

1. **ResultSetMappingAttribute** - 新增特性，用于标注结果集映射关系
2. **RepositoryGenerator** - 扩展生成器，支持元组返回值和结果集映射
3. **元组返回值处理** - 在生成的代码中处理多结果集读取

### 组件关系

```
┌─────────────────────────────────────────────────────────────┐
│                    用户接口定义                              │
│  [SqlTemplate("...")]                                       │
│  [ResultSetMapping(0, "rowsAffected")]                      │
│  [ResultSetMapping(1, "userId")]                            │
│  (int, long) Method(...)                                    │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────────┐
│              RepositoryGenerator                            │
│  - 检测元组返回类型                                         │
│  - 解析 ResultSetMapping 特性                               │
│  - 生成多结果集读取代码                                      │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────────┐
│                生成的仓储代码                                │
│  - ExecuteNonQuery() → rowsAffected                         │
│  - ExecuteReader()                                          │
│  - reader.Read() → 读取第1个结果集                          │
│  - reader.NextResult() → 移动到下一个结果集                 │
│  - 构造元组返回                                              │
└─────────────────────────────────────────────────────────────┘
```

## 数据模型

### ResultSetMappingAttribute

```csharp
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class ResultSetMappingAttribute : Attribute
{
    public int Index { get; }
    public string Name { get; }
    
    public ResultSetMappingAttribute(int index, string name)
    {
        Index = index;
        Name = name;
    }
}
```

### 元组返回类型检测

生成器需要检测以下模式：

1. **值元组（ValueTuple）**：`(int, string)`, `(int a, string b)`
2. **泛型元组**：`ValueTuple<int, string>`
3. **嵌套元组**：`(int, (string, bool))`（暂不支持）

### 结果集映射规则

| 索引 | 含义 | 数据来源 | 示例 |
|------|------|---------|------|
| 0 | 受影响行数 | `ExecuteNonQuery()` 或 `ExecuteNonQueryAsync()` | `INSERT/UPDATE/DELETE` 返回值 |
| 1 | 第1个结果集 | 第1个 `SELECT` 语句 | `SELECT last_insert_rowid()` |
| 2 | 第2个结果集 | 第2个 `SELECT` 语句 | `SELECT COUNT(*)` |
| N | 第N个结果集 | 第N个 `SELECT` 语句 | ... |

## 接口设计

### ResultSetMappingAttribute

```csharp
namespace Sqlx.Annotations;

/// <summary>
/// 指定方法返回元组中每个元素对应的结果集。
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class ResultSetMappingAttribute : Attribute
{
    /// <summary>
    /// 获取结果集的索引。
    /// 索引0通常表示受影响的行数（ExecuteNonQuery的返回值）。
    /// 索引1+表示SELECT语句返回的结果集（按顺序）。
    /// </summary>
    public int Index { get; }
    
    /// <summary>
    /// 获取元组元素的名称，用于匹配元组中的字段。
    /// </summary>
    public string Name { get; }
    
    /// <summary>
    /// 初始化 ResultSetMappingAttribute 的新实例。
    /// </summary>
    /// <param name="index">结果集的索引</param>
    /// <param name="name">元组元素的名称</param>
    public ResultSetMappingAttribute(int index, string name)
    {
        if (index < 0)
            throw new ArgumentOutOfRangeException(nameof(index), "Index must be non-negative");
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be null or whitespace", nameof(name));
            
        Index = index;
        Name = name;
    }
}
```

### 使用示例

```csharp
public interface IUserRepository
{
    // 明确指定映射关系
    [SqlTemplate(@"
        INSERT INTO users (name) VALUES (@name);
        SELECT last_insert_rowid();
        SELECT COUNT(*) FROM users
    ")]
    [ResultSetMapping(0, "rowsAffected")]
    [ResultSetMapping(1, "userId")]
    [ResultSetMapping(2, "totalUsers")]
    Task<(int rowsAffected, long userId, int totalUsers)> InsertAndGetStatsAsync(string name);
    
    // 不指定映射时使用默认规则
    [SqlTemplate(@"
        INSERT INTO users (name) VALUES (@name);
        SELECT last_insert_rowid()
    ")]
    Task<(int rowsAffected, long userId)> InsertAndGetIdAsync(string name);
}
```

## 生成器实现

### 1. 检测元组返回类型

```csharp
private bool IsTupleReturnType(ITypeSymbol returnType, out ImmutableArray<ITypeSymbol> elementTypes)
{
    // 检查是否是 ValueTuple<...>
    if (returnType is INamedTypeSymbol namedType &&
        namedType.IsGenericType &&
        namedType.ConstructedFrom.ToString().StartsWith("System.ValueTuple<"))
    {
        elementTypes = namedType.TypeArguments;
        return true;
    }
    
    elementTypes = ImmutableArray<ITypeSymbol>.Empty;
    return false;
}
```

### 2. 解析 ResultSetMapping 特性

```csharp
private Dictionary<int, string> ParseResultSetMappings(IMethodSymbol method)
{
    var mappings = new Dictionary<int, string>();
    
    foreach (var attr in method.GetAttributes())
    {
        if (attr.AttributeClass?.Name == "ResultSetMappingAttribute")
        {
            var index = (int)attr.ConstructorArguments[0].Value!;
            var name = (string)attr.ConstructorArguments[1].Value!;
            mappings[index] = name;
        }
    }
    
    return mappings;
}
```

### 3. 生成多结果集读取代码

#### 同步方法

```csharp
// 生成的代码示例
public (int rowsAffected, long userId, int totalUsers) InsertAndGetStats(string name)
{
    using var cmd = _connection.CreateCommand();
    cmd.CommandText = "INSERT INTO users (name) VALUES (@name); SELECT last_insert_rowid(); SELECT COUNT(*) FROM users";
    
    var p0 = cmd.CreateParameter();
    p0.ParameterName = "@name";
    p0.Value = name;
    cmd.Parameters.Add(p0);
    
    // 索引0：受影响行数
    var rowsAffected = cmd.ExecuteNonQuery();
    
    // 索引1+：读取结果集
    using var reader = cmd.ExecuteReader();
    
    // 索引1：userId
    if (!reader.Read())
        throw new InvalidOperationException("No data returned for result set 1");
    var userId = reader.GetInt64(0);
    
    // 索引2：totalUsers
    if (!reader.NextResult() || !reader.Read())
        throw new InvalidOperationException("No data returned for result set 2");
    var totalUsers = reader.GetInt32(0);
    
    return (rowsAffected, userId, totalUsers);
}
```

#### 异步方法

```csharp
// 生成的代码示例
public async Task<(int rowsAffected, long userId, int totalUsers)> InsertAndGetStatsAsync(
    string name, 
    CancellationToken ct = default)
{
    using var cmd = _connection.CreateCommand();
    cmd.CommandText = "INSERT INTO users (name) VALUES (@name); SELECT last_insert_rowid(); SELECT COUNT(*) FROM users";
    
    var p0 = cmd.CreateParameter();
    p0.ParameterName = "@name";
    p0.Value = name;
    cmd.Parameters.Add(p0);
    
    // 索引0：受影响行数
    var rowsAffected = await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    
    // 索引1+：读取结果集
    using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
    
    // 索引1：userId
    if (!await reader.ReadAsync(ct).ConfigureAwait(false))
        throw new InvalidOperationException("No data returned for result set 1");
    var userId = reader.GetInt64(0);
    
    // 索引2：totalUsers
    if (!await reader.NextResultAsync(ct).ConfigureAwait(false) || 
        !await reader.ReadAsync(ct).ConfigureAwait(false))
        throw new InvalidOperationException("No data returned for result set 2");
    var totalUsers = reader.GetInt32(0);
    
    return (rowsAffected, userId, totalUsers);
}
```

### 4. 默认映射规则

当没有 `[ResultSetMapping]` 特性时，使用以下默认规则：

1. 第1个元组元素 → 受影响行数（ExecuteNonQuery）
2. 第2个元组元素 → 第1个SELECT结果集
3. 第3个元组元素 → 第2个SELECT结果集
4. ...以此类推

```csharp
private Dictionary<int, string> GetDefaultMappings(ImmutableArray<ITypeSymbol> tupleElements)
{
    var mappings = new Dictionary<int, string>();
    
    for (int i = 0; i < tupleElements.Length; i++)
    {
        // 使用元组元素的名称，如果没有名称则使用 Item1, Item2, ...
        var name = $"Item{i + 1}";  // 或从元组元数据中获取实际名称
        mappings[i] = name;
    }
    
    return mappings;
}
```

## 错误处理

### 编译时错误

1. **元组元素数量与映射数量不匹配**
   ```
   错误: 元组有3个元素，但只定义了2个ResultSetMapping
   ```

2. **映射索引重复**
   ```
   错误: ResultSetMapping索引1被定义了多次
   ```

3. **映射索引不连续**
   ```
   警告: ResultSetMapping索引从0跳到2，缺少索引1
   ```

4. **元组元素名称与映射名称不匹配**
   ```
   错误: ResultSetMapping指定的名称'userId'在元组中不存在
   ```

### 运行时错误

1. **结果集为空**
   ```csharp
   throw new InvalidOperationException($"No data returned for result set {index}");
   ```

2. **结果集数量不足**
   ```csharp
   throw new InvalidOperationException($"Expected {expectedCount} result sets, but only {actualCount} were returned");
   ```

3. **类型转换失败**
   ```csharp
   throw new InvalidCastException($"Cannot convert result set {index} value to {targetType}");
   ```

## 测试策略

### 单元测试

1. **ResultSetMappingAttribute 测试**
   - 构造函数参数验证
   - 属性值正确性
   - 特性使用限制（AllowMultiple, Inherited）

2. **生成器测试**
   - 元组类型检测
   - ResultSetMapping 解析
   - 默认映射规则
   - 代码生成正确性

### 集成测试

1. **单结果集测试**
   ```csharp
   [SqlTemplate("SELECT 1, 'test'")]
   (int id, string name) GetSingleRow();
   ```

2. **多结果集测试**
   ```csharp
   [SqlTemplate("SELECT 1; SELECT 'test'")]
   [ResultSetMapping(0, "id")]
   [ResultSetMapping(1, "name")]
   (int id, string name) GetMultipleResultSets();
   ```

3. **混合输出参数测试**
   ```csharp
   [SqlTemplate("INSERT INTO t VALUES (@v); SELECT last_insert_rowid()")]
   [ResultSetMapping(0, "rows")]
   [ResultSetMapping(1, "id")]
   (int rows, long id) Insert(int v, [OutputParameter(DbType.DateTime)] out DateTime ts);
   ```

4. **错误场景测试**
   - 空结果集
   - 结果集数量不足
   - 类型转换失败

## 性能考虑

1. **零反射** - 所有代码在编译时生成
2. **最小化数据库往返** - 一次调用获取多个值
3. **类型安全** - 编译时验证类型匹配
4. **AOT 兼容** - 完全支持 Native AOT

## 限制和注意事项

1. **元组元素限制** - 最多支持8个元素（ValueTuple限制）
2. **结果集顺序** - 必须按照SQL语句中SELECT的顺序
3. **标量值** - 每个结果集只读取第一行第一列
4. **类型转换** - 依赖 `Convert.ChangeType` 或 `DbDataReader.GetXxx` 方法
5. **嵌套元组** - 暂不支持嵌套元组
6. **命名元组** - 支持命名元组，但名称必须与 ResultSetMapping 的 Name 匹配

## 相关文档

- [输出参数文档](../../docs/output-parameters.md)
- [SQL 模板文档](../../docs/sql-templates.md)
- [源生成器文档](../../docs/source-generators.md)
