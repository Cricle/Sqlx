
# Sqlx API 参考文档

## 🔐 动态占位符 API

### `[DynamicSql]` 特性

**命名空间**: `Sqlx`

**用途**: 标记参数为动态 SQL 参数，该参数的值会直接拼接到 SQL 字符串中（非参数化）。

#### 定义

```csharp
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
public sealed class DynamicSqlAttribute : Attribute
{
    /// <summary>
    /// 动态 SQL 参数的类型
    /// </summary>
    public DynamicSqlType Type { get; set; } = DynamicSqlType.Identifier;
}
```

#### 属性

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `Type` | `DynamicSqlType` | `Identifier` | 动态参数的验证类型 |

#### 使用示例

```csharp
// 默认类型（Identifier）
[Sqlx("SELECT * FROM {{@tableName}}")]
Task<List<User>> GetAsync([DynamicSql] string tableName);

// SQL 片段类型
[Sqlx("SELECT * FROM users WHERE {{@whereClause}}")]
Task<List<User>> QueryAsync([DynamicSql(Type = DynamicSqlType.Fragment)] string whereClause);

// 表名部分类型
[Sqlx("SELECT * FROM logs_{{@suffix}}")]
Task<List<Log>> GetLogsAsync([DynamicSql(Type = DynamicSqlType.TablePart)] string suffix);
```

---

### `DynamicSqlType` 枚举

**命名空间**: `Sqlx`

**用途**: 定义动态 SQL 参数的验证类型。

#### 定义

```csharp
public enum DynamicSqlType
{
    /// <summary>
    /// 标识符（表名、列名）- 最严格验证
    /// </summary>
    Identifier = 0,

    /// <summary>
    /// SQL 片段（WHERE、JOIN、ORDER BY 等子句）- 中等验证
    /// </summary>
    Fragment = 1,

    /// <summary>
    /// 表名部分（前缀、后缀）- 严格验证
    /// </summary>
    TablePart = 2
}
```

#### 验证规则

| 类型 | 验证规则 | 长度限制 | 示例 |
|------|---------|---------|------|
| `Identifier` | 只允许字母、数字、下划线；以字母或下划线开头；不包含 SQL 关键字 | 1-128 | `users`, `tenant1_users`, `user_name` |
| `Fragment` | 禁止 DDL 操作、危险函数、注释符号 | 1-4096 | `age > 18 AND status='active'`, `name ASC` |
| `TablePart` | 只允许字母和数字 | 1-64 | `2024`, `tenant1`, `shard001` |

---

### `SqlValidator` 类

**命名空间**: `Sqlx.Validation`

**用途**: 提供高性能的运行时验证方法（零 GC、AggressiveInlining）。

#### 定义

```csharp
public static class SqlValidator
{
    /// <summary>
    /// 验证标识符（表名、列名）- 零 GC 版本
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValidIdentifier(ReadOnlySpan<char> identifier);

    /// <summary>
    /// 检查是否包含危险关键字
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ContainsDangerousKeyword(ReadOnlySpan<char> text);

    /// <summary>
    /// 验证SQL片段（WHERE、JOIN等）- 优化版
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValidFragment(ReadOnlySpan<char> fragment);

    /// <summary>
    /// 验证表名部分（前缀、后缀）- 零 GC 版本
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValidTablePart(ReadOnlySpan<char> part);

    /// <summary>
    /// 根据类型验证动态 SQL 参数
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Validate(ReadOnlySpan<char> value, DynamicSqlType type);
}
```

#### 方法

##### `IsValidIdentifier(ReadOnlySpan<char> identifier)`

验证标识符（表名、列名）。

**参数**:
- `identifier`: 要验证的标识符

**返回值**:
- `true` - 有效
- `false` - 无效

**验证规则**:
- 长度：1-128 字符
- 格式：字母/数字/下划线，以字母或下划线开头
- 不包含 SQL 关键字和危险字符

**示例**:
```csharp
var isValid = SqlValidator.IsValidIdentifier("users".AsSpan());        // true
var isInvalid = SqlValidator.IsValidIdentifier("DROP TABLE".AsSpan()); // false
```

---

##### `ContainsDangerousKeyword(ReadOnlySpan<char> text)`

检查是否包含危险关键字。

**参数**:
- `text`: 要检查的文本

**返回值**:
- `true` - 包含危险关键字
- `false` - 不包含

**检查项**:
- DDL 操作：`DROP`, `TRUNCATE`, `ALTER`, `EXEC`
- 注释符号：`--`, `/*`, `;`

**示例**:
```csharp
var hasDanger = SqlValidator.ContainsDangerousKeyword("DROP TABLE".AsSpan());  // true
var safe = SqlValidator.ContainsDangerousKeyword("age > 18".AsSpan());         // false
```

---

##### `IsValidFragment(ReadOnlySpan<char> fragment)`

验证 SQL 片段（WHERE、JOIN 等）。

**参数**:
- `fragment`: SQL 片段

**返回值**:
- `true` - 有效
- `false` - 无效

**验证规则**:
- 长度：1-4096 字符
- 不包含 DDL/危险操作
- 不包含注释符号

**示例**:
```csharp
var isValid = SqlValidator.IsValidFragment("age > 18 AND status = 'active'".AsSpan());  // true
var isInvalid = SqlValidator.IsValidFragment("age > 18; DROP TABLE users".AsSpan());   // false
```

---

##### `IsValidTablePart(ReadOnlySpan<char> part)`

验证表名部分（前缀、后缀）。

**参数**:
- `part`: 表名部分

**返回值**:
- `true` - 有效
- `false` - 无效

**验证规则**:
- 长度：1-64 字符
- 只允许字母和数字

**示例**:
```csharp
var isValid = SqlValidator.IsValidTablePart("202410".AsSpan());     // true
var isInvalid = SqlValidator.IsValidTablePart("2024_10".AsSpan()); // false（包含下划线）
```

---

##### `Validate(ReadOnlySpan<char> value, DynamicSqlType type)`

根据类型验证动态 SQL 参数。

**参数**:
- `value`: 要验证的值
- `type`: 验证类型

**返回值**:
- `true` - 有效
- `false` - 无效

**示例**:
```csharp
var isValid = SqlValidator.Validate("users".AsSpan(), DynamicSqlType.Identifier);  // true
var isValid2 = SqlValidator.Validate("202410".AsSpan(), DynamicSqlType.TablePart); // true
```

---

## 🎯 性能特性

### 零 GC 设计

所有 `SqlValidator` 方法使用 `ReadOnlySpan<char>` 参数：
- ✅ 零字符串分配
- ✅ 栈上操作
- ✅ 零 GC 压力

### AggressiveInlining

所有方法标记 `AggressiveInlining`：
- ✅ 消除函数调用开销
- ✅ 编译器完全优化
- ✅ 接近手写代码性能

### 性能数据

| 操作 | 延迟 | 内存分配 | 说明 |
|------|------|---------|------|
| `IsValidIdentifier` | ~0.09μs | 0 bytes | 零 GC |
| `IsValidFragment` | ~0.18μs | 0 bytes | 零 GC |
| `ContainsDangerousKeyword` | ~0.06μs | 0 bytes | 零 GC |

---

## 📚 相关文档

- [动态占位符完整指南](PLACEHOLDERS.md#动态占位符-前缀---高级功能)
- [Roslyn 分析器设计](../ANALYZER_DESIGN.md)
- [TodoWebApi 使用示例](../samples/TodoWebApi/DYNAMIC_PLACEHOLDER_EXAMPLE.md)

---

## ⚠️ 安全警告

**动态占位符会绕过参数化查询，存在 SQL 注入风险！**

**使用前必须：**
1. ✅ 显式标记 `[DynamicSql]` 特性（否则编译错误）
2. ✅ 在调用前进行严格验证（白名单）
3. ✅ 不要在公共 API 中暴露
4. ✅ 生成的代码会包含内联验证

**Roslyn 分析器支持：**

Sqlx 提供 10 个诊断规则来检测不安全的使用：
- SQLX2001 (Error): 使用 `{{@}}` 但参数未标记 `[DynamicSql]`
- SQLX2002 (Warning): 动态参数来自不安全来源
- SQLX2003 (Warning): 调用前缺少验证
- SQLX2004 (Info): 建议使用白名单验证
- SQLX2005 (Warning): 在公共 API 中暴露动态参数
- SQLX2006 (Error): 动态参数类型不是 string
- SQLX2007 (Warning): SQL 模板包含危险操作
- SQLX2008 (Info): 建议添加单元测试
- SQLX2009 (Warning): 缺少长度限制检查
- SQLX2010 (Error): `[DynamicSql]` 特性使用错误

详见：[分析器设计文档](../ANALYZER_DESIGN.md)
