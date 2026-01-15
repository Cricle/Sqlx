# LINQ Select 匿名类型支持设计方案

## 当前问题

1. `Select(u => new { u.Id, u.Name })` 返回的是 `IQueryable<匿名类型>`
2. 匿名类型无法在编译时标记 `[SqlxEntity]`，无法使用源生成器
3. 需要手动提供 `IResultReader<匿名类型>`，但匿名类型无法引用

## 解决方案：运行时动态 ResultReader + 缓存

### 方案 1：使用表达式树生成 ResultReader（推荐）

**优点：**
- 完全 AOT 兼容
- 性能接近源生成器
- 无需反射
- 自动缓存，避免重复生成

**实现思路：**

```csharp
// 1. 在 SqlxQueryProvider<T> 中检测 Select 操作
public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
{
    // 检测是否是 Select 操作
    if (IsSelectExpression(expression, out var selectLambda))
    {
        // 从 Select lambda 中提取列信息
        var columns = ExtractSelectColumns(selectLambda);
        
        // 生成或获取缓存的 ResultReader
        var reader = ResultReaderCache<TElement>.GetOrCreate(columns, Dialect);
        
        var newProvider = new SqlxQueryProvider<TElement>(Dialect)
        {
            Connection = Connection,
            ResultReader = reader
        };
        return new SqlxQueryable<TElement>(newProvider, expression);
    }
    
    // 原有逻辑...
}

// 2. ResultReader 缓存（按类型和列组合缓存）
internal static class ResultReaderCache<T>
{
    private static readonly ConcurrentDictionary<string, IResultReader<T>> _cache = new();
    
    public static IResultReader<T> GetOrCreate(List<ColumnInfo> columns, SqlDialect dialect)
    {
        var key = GenerateCacheKey(columns);
        return _cache.GetOrAdd(key, _ => CreateReader(columns, dialect));
    }
    
    private static IResultReader<T> CreateReader(List<ColumnInfo> columns, SqlDialect dialect)
    {
        // 使用表达式树生成 Read 方法
        // reader => new T { Prop1 = reader.GetXxx(0), Prop2 = reader.GetXxx(1) }
        return new ExpressionBasedResultReader<T>(columns);
    }
}

// 3. 基于表达式树的 ResultReader
internal class ExpressionBasedResultReader<T> : IResultReader<T>
{
    private readonly Func<IDataReader, T> _readFunc;
    private readonly Func<IDataReader, int[], T> _readWithOrdinalsFunc;
    private readonly string[] _columnNames;
    
    public ExpressionBasedResultReader(List<ColumnInfo> columns)
    {
        _columnNames = columns.Select(c => c.Name).ToArray();
        _readFunc = BuildReadExpression(columns);
        _readWithOrdinalsFunc = BuildReadWithOrdinalsExpression(columns);
    }
    
    private static Func<IDataReader, T> BuildReadExpression(List<ColumnInfo> columns)
    {
        var readerParam = Expression.Parameter(typeof(IDataReader), "reader");
        
        // 构建：new T { Prop1 = reader.GetXxx(ordinal), ... }
        var bindings = new List<MemberBinding>();
        for (int i = 0; i < columns.Count; i++)
        {
            var col = columns[i];
            var ordinalExpr = Expression.Constant(i);
            var getValueExpr = BuildGetValueExpression(readerParam, ordinalExpr, col);
            bindings.Add(Expression.Bind(col.Property, getValueExpr));
        }
        
        var newExpr = Expression.MemberInit(Expression.New(typeof(T)), bindings);
        return Expression.Lambda<Func<IDataReader, T>>(newExpr, readerParam).Compile();
    }
    
    public T Read(IDataReader reader) => _readFunc(reader);
    public T Read(IDataReader reader, int[] ordinals) => _readWithOrdinalsFunc(reader);
    public int[] GetOrdinals(IDataReader reader) => 
        _columnNames.Select(reader.GetOrdinal).ToArray();
}
```

### 方案 2：使用 Reflection.Emit（备选）

**优点：**
- 性能最优（接近手写代码）
- 可以生成真正的 IL 代码

**缺点：**
- 不支持 AOT
- 实现复杂度高

### 方案 3：使用 SourceGenerator + Interceptor（.NET 8+）

**优点：**
- 编译时生成，零运行时开销
- 完全类型安全

**缺点：**
- 需要 .NET 8+
- Interceptor 还是预览特性
- 实现复杂

## 推荐实现步骤

### 第一阶段：基础支持（表达式树）

1. **扩展 ExpressionParser**
   ```csharp
   public class SelectColumnInfo
   {
       public string ColumnName { get; set; }
       public string PropertyName { get; set; }
       public Type PropertyType { get; set; }
       public bool IsNullable { get; set; }
   }
   
   public List<SelectColumnInfo> ExtractSelectColumns(LambdaExpression selector)
   {
       // 从 Select lambda 中提取列信息
       // 支持：new { u.Id, u.Name }
       // 支持：new { Id = u.Id, UserName = u.Name }
       // 支持：new { u.Id, FullName = u.FirstName + " " + u.LastName }
   }
   ```

2. **实现 ExpressionBasedResultReader**
   ```csharp
   internal class ExpressionBasedResultReader<T> : IResultReader<T>
   {
       // 使用表达式树编译生成高性能的 Read 方法
   }
   ```

3. **添加缓存机制**
   ```csharp
   internal static class ResultReaderCache<T>
   {
       // 按列组合缓存，避免重复生成
   }
   ```

4. **修改 SqlxQueryProvider.CreateQuery**
   ```csharp
   // 检测 Select 操作并自动创建 ResultReader
   ```

### 第二阶段：优化

1. **支持复杂表达式**
   - 计算列：`new { Total = u.Price * u.Quantity }`
   - 函数调用：`new { Upper = u.Name.ToUpper() }`
   - 条件表达式：`new { Status = u.Age > 18 ? "Adult" : "Minor" }`

2. **性能优化**
   - 预编译常用模式
   - 使用 ArrayPool 减少分配
   - 优化缓存键生成

3. **错误处理**
   - 列不存在时的友好错误
   - 类型不匹配时的提示

### 第三阶段：高级特性（可选）

1. **支持嵌套对象**
   ```csharp
   .Select(u => new { 
       User = new { u.Id, u.Name },
       Address = new { u.City, u.Country }
   })
   ```

2. **支持集合属性**
   ```csharp
   .Select(u => new { 
       u.Id, 
       Tags = u.Tags.ToList() 
   })
   ```

## 避免重复的关键点

1. **统一的列信息提取**
   - `ExpressionParser.ExtractSelectColumns` 复用现有的列名解析逻辑
   - 与 `SqlExpressionVisitor.VisitSelect` 共享代码

2. **缓存机制**
   - 按类型 + 列组合缓存 ResultReader
   - 相同的 Select 模式只生成一次

3. **复用 ColumnMeta**
   - 如果源类型有 `IEntityProvider`，优先使用其 `ColumnMeta`
   - 避免重复的列名转换逻辑

4. **表达式树编译缓存**
   - 编译后的委托缓存在静态字段
   - 避免重复编译

## 示例用法

```csharp
// 1. 简单投影
var result = SqlQuery.ForSqlite<User>()
    .WithConnection(conn)
    .Select(u => new { u.Id, u.Name })
    .ToList();
// 自动生成 ResultReader<匿名类型>

// 2. 重命名列
var result = SqlQuery.ForSqlite<User>()
    .WithConnection(conn)
    .Select(u => new { UserId = u.Id, UserName = u.Name })
    .ToList();

// 3. 计算列
var result = SqlQuery.ForSqlite<User>()
    .WithConnection(conn)
    .Select(u => new { u.Id, FullName = u.FirstName + " " + u.LastName })
    .ToList();

// 4. 条件列
var result = SqlQuery.ForSqlite<User>()
    .WithConnection(conn)
    .Select(u => new { u.Id, Status = u.Age >= 18 ? "Adult" : "Minor" })
    .ToList();
```

## 性能对比

| 方案 | 首次调用 | 后续调用 | AOT 支持 | 内存分配 |
|------|---------|---------|---------|---------|
| 源生成器 | 0ms | ~10μs | ✅ | 最少 |
| 表达式树 | ~1-5ms | ~10μs | ✅ | 少 |
| Reflection.Emit | ~2-10ms | ~10μs | ❌ | 少 |
| 纯反射 | ~50μs | ~50μs | ⚠️ | 多 |

## 结论

**推荐使用方案 1（表达式树）**，因为：
1. 完全 AOT 兼容
2. 性能接近源生成器（首次调用后）
3. 实现相对简单
4. 可以复用现有的 ExpressionParser 逻辑
5. 通过缓存避免重复生成

实现优先级：
1. ✅ 基础支持（简单属性投影）
2. ✅ 缓存机制
3. ⏳ 复杂表达式支持
4. ⏳ 嵌套对象支持
