# 预定义Repository接口问题分析

## 🔍 发现的问题

### 1. ❌ 泛型方法问题（源生成器不支持）

**IAggregateRepository.cs:**
```csharp
Task<T> MaxAsync<T>(string column, ...);
Task<T> MinAsync<T>(string column, ...);
Task<(T Min, T Max)> MinMaxAsync<T>(string column, ...);
Task<T> AggregateAsync<T>(string function, string column, ...);
```

**问题**: 源生成器无法生成泛型方法的实现
**影响**: 导致编译错误，用户无法使用这些方法

---

### 2. ❌ 参数传递方式错误

**多处使用 `@column` 作为动态参数：**
```csharp
[SqlTemplate("SELECT @column, COUNT(*) FROM {{table}} GROUP BY @column")]
Task<Dictionary<string, long>> CountByAsync(string column, ...);

[SqlTemplate("SELECT COALESCE(SUM(@column), 0) FROM {{table}}")]
Task<decimal> SumAsync(string column, ...);
```

**问题**: 
- `@column` 会被当作参数化查询，生成 `SELECT 'price'` 而不是 `SELECT price`
- 列名应该用 `{{column}}` 或 `[DynamicSql]` 特性

**正确方式**:
```csharp
// 方式1: 使用占位符
[SqlTemplate("SELECT {{column}}, COUNT(*) FROM {{table}} GROUP BY {{column}}")]

// 方式2: 使用DynamicSql特性
Task<decimal> SumAsync([DynamicSql] string column, ...);
```

---

### 3. ❌ 缺少重要方法

**缺失的常用方法：**

1. **GetRandom** - 随机获取N条记录
2. **GetDistinct** - 获取去重后的值
3. **BulkUpdatePartial** - 批量部分更新
4. **Transaction支持** - 事务相关方法
5. **Exists批量检查** - 批量检查ID是否存在

---

### 4. ⚠️  不合理的方法签名

**IQueryRepository:**
```csharp
Task<TEntity> GetFirstAsync(CancellationToken cancellationToken = default);
Task<TEntity> GetSingleAsync(CancellationToken cancellationToken = default);
```

**问题**: 
- 没有WHERE条件，GetFirst会随机返回任意一条记录（不稳定）
- GetSingle在表有多条记录时会抛异常（几乎总是失败）

**建议**: 
- 移除无条件的GetFirst/GetSingle
- 或者添加强制的orderBy参数

---

### 5. ❌ 方法重复（ICrudRepository）

**ICrudRepository包含大量`new`关键字方法：**
```csharp
new Task<TEntity?> GetByIdAsync(...);
new Task<int> InsertAsync(...);
new Task<int> UpdateAsync(...);
new Task<int> DeleteAsync(...);
new Task<bool> ExistsAsync(...);
```

**问题**: 
- 已经从IQueryRepository/ICommandRepository继承了这些方法
- 使用`new`关键字隐藏基类方法会造成混乱
- 增加维护成本

---

### 6. ❌ IAdvancedRepository的危险方法

```csharp
Task TruncateAsync(...);  // 截断表
Task DropTableAsync(...);  // 删除表
```

**问题**: 
- 极度危险的操作
- 容易误用导致数据丢失
- 不应该在常规Repository中出现

**建议**: 移到单独的 `ISchemaRepository` 或 `IMaintenanceRepository`

---

### 7. ⚠️  Missing合理的默认值

**IQueryRepository:**
```csharp
Task<List<TEntity>> GetAllAsync(string? orderBy = null, ...);
```

**问题**: 
- 没有限制数量，可能一次加载百万条数据导致OOM
- 应该强制分页或至少有默认limit

---

### 8. ❌ 返回类型不一致

**有些方法返回List，有些应该返回IEnumerable：**
```csharp
Task<List<TEntity>> GetAllAsync(...);  // ✅ 合理
Task<int> BulkCopyAsync(IEnumerable<TEntity> entities, ...);  // ⚠️ 参数用IEnumerable，返回值却不一致
```

---

## 📋 建议的改进方案

### 改进1: 移除泛型方法，提供具体类型重载

```csharp
// 移除
Task<T> MaxAsync<T>(string column, ...);

// 改为
Task<int> MaxIntAsync(string column, ...);
Task<long> MaxLongAsync(string column, ...);
Task<decimal> MaxDecimalAsync(string column, ...);
Task<DateTime> MaxDateTimeAsync(string column, ...);
Task<string> MaxStringAsync(string column, ...);
```

### 改进2: 修复参数传递方式

```csharp
// 所有列名参数使用DynamicSql特性
Task<decimal> SumAsync([DynamicSql(Type = DynamicSqlType.Identifier)] string column, ...);
```

### 改进3: 添加缺失方法

```csharp
// IQueryRepository新增
Task<List<TEntity>> GetRandomAsync(int count, ...);
Task<List<T>> GetDistinctAsync<T>(string column, ...);

// IBatchRepository新增
Task<List<bool>> BatchExistsAsync(List<TKey> ids, ...);
Task<int> BatchUpdatePartialAsync(List<TKey> ids, object updates, ...);
```

### 改进4: 清理ICrudRepository

```csharp
// 移除所有new方法，直接继承即可
public interface ICrudRepository<TEntity, TKey> :
    IQueryRepository<TEntity, TKey>,
    ICommandRepository<TEntity, TKey>
{
    // 只保留ICrudRepository特有的组合方法
}
```

### 改进5: 分离危险操作

```csharp
// 新建ISchemaRepository
public interface ISchemaRepository<TEntity>
{
    Task<bool> TableExistsAsync(...);
    Task CreateTableIfNotExistsAsync(...);
    Task<string> GenerateCreateTableSqlAsync();
}

// 新建IMaintenanceRepository（需要特殊权限）
public interface IMaintenanceRepository<TEntity>
{
    Task TruncateAsync(...);  // 清空表
    Task DropTableAsync(...);  // 删除表
}
```

### 改进6: 添加安全的默认值

```csharp
// 强制分页
Task<List<TEntity>> GetAllAsync(
    int limit = 1000,  // 默认最多1000条
    string? orderBy = null, 
    ...);
```

---

## 📊 优先级

### P0 - 必须修复（阻塞编译）
1. ✅ 移除泛型方法（MaxAsync<T>等）
2. ✅ 修复参数传递方式（@column改为DynamicSql）

### P1 - 应该修复（影响使用）
3. 清理ICrudRepository的重复方法
4. 添加安全的默认值
5. 修复不合理的方法签名

### P2 - 可以改进（最佳实践）
6. 分离危险操作到单独接口
7. 添加缺失的常用方法
8. 统一返回类型风格


