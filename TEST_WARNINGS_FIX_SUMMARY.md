# 单元测试警告修复总结

## 问题描述

运行 `dotnet test` 时出现 9 个编译警告：

### 警告类型 1: CS0108 - 隐藏继承的成员（6 个）

```
warning CS0108: "IXxxRepository.GetByIdAsync(long, CancellationToken)"隐藏继承的成员
"IQueryRepository<ConnectionPriorityEntity, long>.GetByIdAsync(long, CancellationToken)"。
如果是有意隐藏，请使用关键字 new。
```

影响的接口：
- `IFieldVsPropertyRepository`
- `IFieldVsPrimaryCtorRepository`
- `IPropertyVsPrimaryCtorRepository`
- `IPrimaryCtorOnlyRepository`
- `IExplicitTransactionRepository`
- `IAutoTransactionRepository`

### 警告类型 2: CS9113 - 参数未读（3 个）

```
warning CS9113: 参数"primaryConnection"未读。
```

影响的类：
- `FieldVsPrimaryCtorRepository`
- `PropertyVsPrimaryCtorRepository`
- `AllSourcesRepository`

## 根本原因

### 原因 1: 接口方法隐藏基类方法

`ConnectionPriorityTests.cs` 中的测试接口继承了 `IQueryRepository<T, TKey>`，该接口已经定义了 `GetByIdAsync` 方法。测试接口为了自定义 SQL 模板重新定义了同名方法，导致隐藏了基类方法。

### 原因 2: 主构造函数参数未使用

某些测试类使用主构造函数参数只是为了测试优先级，但实际使用的是字段或属性，导致主构造函数参数未被读取。

## 解决方案

### 解决方案 1: 添加 `new` 关键字

在接口方法声明前添加 `new` 关键字，明确表示有意隐藏基类方法：

```csharp
// 修改前
public interface IFieldVsPropertyRepository : IQueryRepository<ConnectionPriorityEntity, long>
{
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    Task<ConnectionPriorityEntity?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
}

// 修改后
public interface IFieldVsPropertyRepository : IQueryRepository<ConnectionPriorityEntity, long>
{
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    new Task<ConnectionPriorityEntity?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
}
```

### 解决方案 2: 禁用未读参数警告

使用 `#pragma warning disable CS9113` 禁用特定警告，并将参数名改为 `_` 表示有意不使用：

```csharp
// 修改前
public partial class FieldVsPrimaryCtorRepository(SqliteConnection primaryConnection) : IFieldVsPrimaryCtorRepository
{
    private readonly SqliteConnection _fieldConnection = new SqliteConnection("Data Source=field.db");
}

// 修改后
#pragma warning disable CS9113 // Parameter is unread
public partial class FieldVsPrimaryCtorRepository(SqliteConnection _) : IFieldVsPrimaryCtorRepository
#pragma warning restore CS9113
{
    private readonly SqliteConnection _fieldConnection = new SqliteConnection("Data Source=field.db");
}
```

## 修复的文件

- `tests/Sqlx.Tests/ConnectionPriorityTests.cs`
  - 添加 6 个 `new` 关键字
  - 添加 3 处 `#pragma warning disable CS9113`
  - 将未使用的参数名改为 `_`

## 测试结果

修复后运行 `dotnet test`：

```
✅ 测试摘要: 总计: 1611; 失败: 0; 成功: 1611; 已跳过: 0
✅ 在 14.3 秒内生成 已成功（无警告）
```

- ✅ 所有 1611 个测试通过
- ✅ 0 个警告
- ✅ 0 个错误

## 关键要点

1. **接口方法隐藏**
   - 当子接口重新定义基接口的方法时，使用 `new` 关键字明确意图
   - 这在测试场景中很常见，用于自定义 SQL 模板

2. **未使用的参数**
   - 使用 `#pragma warning disable` 禁用特定警告
   - 将参数名改为 `_` 是 C# 的惯用法，表示有意不使用
   - 在测试优先级场景中，某些参数只是为了测试存在性

3. **代码质量**
   - 修复所有编译警告保持代码库清洁
   - 使用 pragma 指令精确控制警告范围
   - 明确的意图表达（`new` 关键字、`_` 参数名）提高代码可读性

## 相关文件

- `tests/Sqlx.Tests/ConnectionPriorityTests.cs` - 修复所有警告

## 测试覆盖

ConnectionPriorityTests 测试了连接获取的优先级：

1. **方法参数 > 字段** - 验证方法参数优先级最高
2. **方法参数 > 属性** - 验证方法参数优先于属性
3. **方法参数 > 主构造函数** - 验证方法参数优先于主构造函数
4. **字段 > 属性** - 验证字段优先于属性
5. **字段 > 主构造函数** - 验证字段优先于主构造函数
6. **属性 > 主构造函数** - 验证属性优先于主构造函数
7. **只有主构造函数** - 验证主构造函数作为最后选择
8. **完整优先级测试** - 验证所有来源的完整优先级
9. **显式 Transaction 属性** - 验证用户定义的 Transaction 属性
10. **自动生成 Transaction 属性** - 验证生成器自动生成 Transaction 属性

所有测试都通过，确保连接优先级逻辑正确实现。
