# Sqlx 设计原则

**核心理念**: 简单、快速、安全、充分利用源生成能力

---

## 🎯 三大核心原则

### 1️⃣ **异常不吞噬 - Fail Fast** 🔥

#### ✅ 正确做法

```csharp
// ✅ 拦截器异常直接抛出
internal static void OnExecuting(ref SqlxExecutionContext context)
{
    if (!IsEnabled || _count == 0) return;

    var interceptors = _interceptors;
    var count = _count;

    for (int i = 0; i < count; i++)
    {
        interceptors[i]!.OnExecuting(ref context); // 异常直接抛出
    }
}
```

#### ❌ 错误做法

```csharp
// ❌ 不要吞噬异常
for (int i = 0; i < count; i++)
{
    try
    {
        interceptors[i]!.OnExecuting(ref context);
    }
    catch
    {
        // ❌ 静默失败，问题被隐藏
    }
}
```

#### 原因

- ✅ **问题立即可见** - 开发时能立刻发现拦截器错误
- ✅ **完整堆栈** - 异常信息不丢失，便于调试
- ✅ **强制修复** - 不让错误在生产环境隐藏
- ❌ **不做防御性编程** - 拦截器应该保证自己的正确性

**核心思想**: 如果拦截器（日志、追踪、监控）失败了，应该让整个系统失败，而不是静默继续运行。

---

### 2️⃣ **不做无意义缓存** 🔥

#### ✅ 正确做法 - 充分利用源生成

```csharp
// ✅ 源生成器在编译时完成所有计算
// 生成的代码直接硬编码：

public async Task<User?> GetUserByIdAsync(int id)
{
    var __ctx__ = new SqlxExecutionContext(
        "GetUserByIdAsync".AsSpan(),  // 编译时常量
        "UserRepository".AsSpan(),    // 编译时常量
        "SELECT id, name, email FROM users WHERE id = @id".AsSpan()  // 编译时生成
    );

    // 硬编码映射，零反射
    if (await reader.ReadAsync())
    {
        return new User
        {
            Id = reader.GetInt32(0),      // 硬编码序号
            Name = reader.GetString(1),   // 硬编码序号
            Email = reader.GetString(2)   // 硬编码序号
        };
    }
}
```

#### ❌ 错误做法 - 运行时缓存

```csharp
// ❌ 不需要！源生成器已经在编译时做了这些工作
private static readonly ConcurrentDictionary<Type, string> _tableNameCache = new();
private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _propertyCache = new();

public string GetTableName(Type type)
{
    return _tableNameCache.GetOrAdd(type, t =>
        ConvertToSnakeCase(t.Name)); // ❌ 运行时转换和缓存
}

public User MapRow(IDataReader reader, Type type)
{
    var properties = _propertyCache.GetOrAdd(type, t =>
        t.GetProperties()); // ❌ 运行时反射和缓存
    // ...
}
```

#### 什么应该缓存？什么不应该缓存？

| 类型 | 是否缓存 | 原因 |
|------|---------|------|
| **编译时已知信息** | ❌ 不缓存 | 源生成器已生成常量 |
| 表名转换 | ❌ | 生成时已转换为 `"users"` |
| 列名转换 | ❌ | 生成时已转换为 `"user_id"` |
| 实体属性 | ❌ | 生成时已硬编码映射 |
| SQL拼接 | ❌ | 生成时已完成，直接是常量字符串 |
| 参数前缀 | ❌ | 生成时已转换为 `"@id"` |
| **运行时计算结果** | ✅ 可缓存 | 如果计算昂贵且频繁 |
| 复杂正则匹配结果 | ✅ | 源生成器内部可缓存 |
| 数据库连接字符串 | ✅ | 运行时配置 |

#### 核心原则

```
┌─────────────────────────────────────┐
│   编译时（源生成器）                │
│   ✅ 做所有计算                     │
│   ✅ 生成硬编码常量                 │
│   ✅ 内部可使用缓存优化生成性能     │
└─────────────────────────────────────┘
              ↓ 生成
┌─────────────────────────────────────┐
│   生成的代码（运行时）              │
│   ✅ 只包含常量和简单逻辑           │
│   ❌ 不做字符串转换                 │
│   ❌ 不做反射                       │
│   ❌ 不缓存编译时已知信息           │
└─────────────────────────────────────┘
```

**核心思想**: 如果信息在编译时已知，就在编译时计算并生成常量，而不是在运行时计算并缓存。

---

### 3️⃣ **充分利用源生成能力** 🔥

#### 源生成器的优势

| 对比项 | 反射/运行时 | 源生成器 |
|--------|------------|---------|
| **性能** | 慢（反射开销） | **快**（硬编码） |
| **GC** | 高（对象分配） | **低**（常量） |
| **类型安全** | 运行时错误 | **编译时错误** |
| **调试** | 难（动态生成） | **易**（可见代码） |
| **缓存需求** | 需要 | **不需要** |

#### 正确使用源生成器

```csharp
// ✅ 源生成器应该做的事：

// 1️⃣ 在编译时计算所有常量
var tableName = ConvertToSnakeCase("User");  // → "users"
var columns = GetColumns(entityType);        // → "id, name, email"
var sql = $"SELECT {columns} FROM {tableName}";  // → 最终SQL

// 2️⃣ 生成硬编码代码
sb.AppendLine($"cmd.CommandText = @\"{sql}\";");  // 直接常量

// 3️⃣ 生成类型安全的映射
sb.AppendLine("return new User");
sb.AppendLine("{");
sb.AppendLine($"    Id = reader.GetInt32({GetColumnIndex("Id")}),");
sb.AppendLine($"    Name = reader.GetString({GetColumnIndex("Name")}),");
sb.AppendLine("};");
```

#### 生成的代码特点

```csharp
// ✅ 生成的代码应该：
// - 只包含常量和简单逻辑
// - 没有字符串拼接
// - 没有反射
// - 没有缓存
// - 像手写代码一样高效

public async Task<User?> GetUserByIdAsync(int id)
{
    // ✅ 所有这些都是编译时常量
    const string sql = "SELECT id, name, email FROM users WHERE id = @id";

    cmd.CommandText = sql;

    var p_id = cmd.CreateParameter();
    p_id.ParameterName = "@id";  // 常量
    p_id.Value = id;
    p_id.DbType = DbType.Int32;  // 常量
    cmd.Parameters.Add(p_id);

    using var reader = await cmd.ExecuteReaderAsync();
    if (await reader.ReadAsync())
    {
        return new User
        {
            Id = reader.GetInt32(0),      // 硬编码位置
            Name = reader.GetString(1),   // 硬编码位置
            Email = reader.GetString(2)   // 硬编码位置
        };
    }

    return null;
}
```

**核心思想**: 源生成器的目标是生成等同于（甚至优于）手写代码的性能。

---

## 📊 设计对比

### 传统ORM vs Sqlx

| 特性 | 传统ORM | Sqlx（源生成） |
|------|---------|---------------|
| **SQL生成** | 运行时拼接 | 编译时生成常量 |
| **实体映射** | 反射 | 硬编码 |
| **性能** | 慢（反射+缓存） | 快（手写级别） |
| **GC压力** | 高 | 低 |
| **类型安全** | 运行时 | 编译时 |
| **缓存需求** | 必需 | 不需要 |
| **调试** | 难 | 易 |

### 代码生成性能对比

```csharp
// ❌ 传统方式（每次调用都有开销）
public User GetUser(int id)
{
    var tableName = _tableNameCache.GetOrAdd(typeof(User), ...);  // 缓存查找
    var sql = $"SELECT * FROM {tableName} WHERE id = @id";        // 字符串拼接

    var user = new User();
    var properties = _propertyCache.GetOrAdd(typeof(User), ...);  // 缓存查找
    foreach (var prop in properties)  // 遍历
    {
        var value = reader[prop.Name];  // 字符串查找
        prop.SetValue(user, value);     // 反射设置
    }
    return user;
}

// ✅ Sqlx方式（零开销）
public User? GetUser(int id)
{
    cmd.CommandText = "SELECT id, name, email FROM users WHERE id = @id";  // 常量

    using var reader = cmd.ExecuteReader();
    if (reader.Read())
    {
        return new User
        {
            Id = reader.GetInt32(0),     // 直接访问
            Name = reader.GetString(1),  // 直接访问
            Email = reader.GetString(2)  // 直接访问
        };
    }
    return null;
}
```

---

## 🎯 实践指南

### 开发拦截器时

```csharp
// ✅ 正确的拦截器
public class MyInterceptor : ISqlxInterceptor
{
    public void OnExecuting(ref SqlxExecutionContext context)
    {
        // ✅ 确保不会抛异常（或者抛出有意义的异常）
        try
        {
            // 你的逻辑
        }
        catch (Exception ex)
        {
            // ✅ 记录日志后重新抛出，或者转换为更有意义的异常
            _logger.LogError(ex, "拦截器执行失败");
            throw new InvalidOperationException("拦截器执行失败，详见日志", ex);
        }
    }
}

// ❌ 错误的拦截器
public class BadInterceptor : ISqlxInterceptor
{
    public void OnExecuting(ref SqlxExecutionContext context)
    {
        // ❌ 可能抛出异常但不处理
        var data = File.ReadAllText("config.json");  // 文件不存在会抛异常
        // ❌ 依赖框架吞噬异常是不对的
    }
}
```

### 开发源生成器时

```csharp
// ✅ 源生成器优化
public class CodeGenerationService
{
    // ✅ 可以在源生成器内部使用缓存优化生成性能
    private static readonly ConcurrentDictionary<INamedTypeSymbol, EntityMetadata> _metadataCache = new();

    public void GenerateCode(...)
    {
        // ✅ 编译时缓存是可以的（减少重复计算）
        var metadata = _metadataCache.GetOrAdd(entityType, et =>
        {
            return new EntityMetadata
            {
                TableName = ConvertToSnakeCase(et.Name),
                Columns = et.GetMembers()
                    .OfType<IPropertySymbol>()
                    .Select(p => new ColumnInfo
                    {
                        PropertyName = p.Name,
                        ColumnName = ConvertToSnakeCase(p.Name),
                        Index = ...
                    })
                    .ToList()
            };
        });

        // ✅ 生成硬编码代码
        sb.AppendLine($"cmd.CommandText = @\"SELECT {metadata.ColumnNames} FROM {metadata.TableName}\";");

        // ✅ 生成硬编码映射
        foreach (var col in metadata.Columns)
        {
            sb.AppendLine($"    {col.PropertyName} = reader.Get{col.Type}({col.Index}),");
        }
    }
}
```

---

## 📋 检查清单

### 拦截器开发

- [ ] 拦截器方法不会抛出意外异常
- [ ] 如果可能抛异常，已经适当处理
- [ ] 不依赖框架吞噬异常
- [ ] 异常信息有意义，便于调试

### 源生成器开发

- [ ] 所有可以在编译时计算的都在编译时完成
- [ ] 生成的代码只包含常量和简单逻辑
- [ ] 不在运行时做字符串转换
- [ ] 不在运行时做反射
- [ ] 不在生成的代码中缓存编译时已知信息
- [ ] 生成的代码性能接近手写代码

### 性能检查

- [ ] 无不必要的字符串分配
- [ ] 无不必要的反射调用
- [ ] 无不必要的缓存
- [ ] 充分利用源生成能力
- [ ] GC压力最小化

---

## 🎯 总结

### 三大原则的本质

1. **Fail Fast（异常不吞噬）**
   - 让问题在开发阶段暴露
   - 不隐藏错误
   - 便于调试和修复

2. **不做无意义缓存**
   - 编译时已知的不在运行时缓存
   - 充分利用源生成器
   - 减少内存占用和复杂度

3. **充分利用源生成能力**
   - 编译时做所有计算
   - 生成硬编码常量
   - 达到手写代码的性能

### 核心目标

**让Sqlx成为最快的.NET ORM**，通过：
- ✅ 源生成器生成优化的代码
- ✅ 零运行时开销
- ✅ 零GC（栈分配）
- ✅ 编译时类型安全
- ✅ 简单直接的设计

**Remember**: 简单 > 复杂，快速 > 功能多，安全 > 方便

