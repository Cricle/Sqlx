# LINQ Select 匿名类型支持设计方案

## 当前问题

1. `Select(u => new { u.Id, u.Name })` 返回的是 `IQueryable<匿名类型>`
2. 匿名类型无法在编译时标记 `[SqlxEntity]`，无法使用源生成器
3. 需要手动提供 `IResultReader<匿名类型>`，但匿名类型无法引用

## 解决方案：AOT 友好的泛型静态缓存 + 源生成器

### 核心思路：利用泛型静态类的单例特性

**关键洞察：**
- 泛型静态类 `Cache<T>` 对每个 `T` 都是独立的单例
- 无需字典，泛型参数本身就是"键"
- 完全 AOT 兼容，零反射

**实现思路：**

```csharp
// 1. 泛型静态缓存（每个类型 T 一个实例）
internal static class AnonymousTypeResultReader<T>
{
    // 静态字段在类型首次访问时初始化，之后永久缓存
    public static readonly IResultReader<T>? Instance = TryCreateReader();
    
    private static IResultReader<T>? TryCreateReader()
    {
        // 尝试通过反射获取编译器生成的匿名类型构造函数
        // 但这是 AOT 不友好的...
        // 需要另一种方案
    }
}

// 问题：匿名类型在 AOT 中无法通过反射创建
// 解决：使用源生成器 + Interceptor
```

### 方案 1：源生成器 + Interceptor（.NET 8+，完全 AOT）

**优点：**
- 编译时生成，零运行时开销
- 完全 AOT 兼容
- 类型安全
- 利用泛型静态类自动缓存

**实现思路：**

```csharp
// 用户代码
var result = SqlQuery.ForSqlite<User>()
    .Select(u => new { u.Id, u.Name })  // 编译器生成匿名类型 <>f__AnonymousType0
    .ToList();

// 源生成器检测到 Select 调用，生成：
// Generated/<>f__AnonymousType0.ResultReader.g.cs
namespace Sqlx.Generated
{
    internal static class AnonymousType0ResultReader
    {
        public static readonly IResultReader<<>f__AnonymousType0<int, string>> Instance = 
            new ResultReaderImpl();
        
        private sealed class ResultReaderImpl : IResultReader<<>f__AnonymousType0<int, string>>
        {
            public <>f__AnonymousType0<int, string> Read(IDataReader reader)
            {
                return new <>f__AnonymousType0<int, string>(
                    reader.GetInt32(0),
                    reader.GetString(1)
                );
            }
            
            public <>f__AnonymousType0<int, string> Read(IDataReader reader, int[] ordinals)
            {
                return new <>f__AnonymousType0<int, string>(
                    reader.GetInt32(ordinals[0]),
                    reader.GetString(ordinals[1])
                );
            }
            
            public int[] GetOrdinals(IDataReader reader)
            {
                return new[] { reader.GetOrdinal("id"), reader.GetOrdinal("name") };
            }
        }
    }
}

// 使用 Interceptor 拦截 Select 调用
[InterceptsLocation("Program.cs", line: 10, character: 5)]
public static IQueryable<TResult> Select_Intercepted<TSource, TResult>(
    this IQueryable<TSource> source,
    Expression<Func<TSource, TResult>> selector)
{
    // 自动注入生成的 ResultReader
    var provider = ((SqlxQueryable<TSource>)source).Provider as SqlxQueryProvider<TSource>;
    var newProvider = new SqlxQueryProvider<TResult>(provider.Dialect)
    {
        Connection = provider.Connection,
        ResultReader = AnonymousType0ResultReader.Instance  // 使用生成的 Reader
    };
    return new SqlxQueryable<TResult>(newProvider, 
        Expression.Call(null, SelectMethod, source.Expression, Expression.Quote(selector)));
}
```

### 方案 2：手动注册 + 泛型静态缓存（简单但需要手动）

**优点：**
- 完全 AOT 兼容
- 实现简单
- 利用泛型静态类缓存

**缺点：**
- 需要用户手动定义投影类型（不能用匿名类型）

**实现思路：**

```csharp
// 1. 用户定义投影类型（代替匿名类型）
[SqlxProjection(typeof(User))]  // 标记源类型
public partial class UserProjection
{
    public int Id { get; set; }
    public string Name { get; set; }
}

// 2. 源生成器生成 ResultReader
// Generated/UserProjection.ResultReader.g.cs
partial class UserProjection
{
    static UserProjection()
    {
        // 自动注册到泛型静态缓存
        ProjectionResultReader<UserProjection>.Instance = new UserProjectionResultReader();
    }
}

internal static class ProjectionResultReader<T>
{
    public static IResultReader<T>? Instance { get; set; }
}

// 3. 使用
var result = SqlQuery.ForSqlite<User>()
    .Select(u => new UserProjection { Id = u.Id, Name = u.Name })
    .ToList();
// 自动使用 ProjectionResultReader<UserProjection>.Instance
```

### 方案 3：编译时常量 + 泛型静态缓存（推荐，平衡方案）

**优点：**
- 完全 AOT 兼容
- 支持匿名类型（通过编译时分析）
- 利用泛型静态类缓存
- 无需 Interceptor

**实现思路：**

```csharp
// 1. 源生成器分析所有 Select 调用
// 为每个唯一的投影生成 ResultReader

// 2. 生成泛型静态缓存类
// Generated/SelectProjections.g.cs
namespace Sqlx.Generated
{
    // 为每个投影类型生成一个静态类
    internal static class SelectProjection_User_Id_Name<T>
    {
        public static readonly IResultReader<T> Instance = CreateReader();
        
        private static IResultReader<T> CreateReader()
        {
            var type = typeof(T);
            
            // 检查是否是匹配的匿名类型
            if (type.Name.StartsWith("<>f__AnonymousType") && 
                type.GetProperties().Length == 2 &&
                type.GetProperty("Id") != null &&
                type.GetProperty("Name") != null)
            {
                return new AnonymousTypeReader() as IResultReader<T>;
            }
            
            return null;
        }
        
        private sealed class AnonymousTypeReader : IResultReader<T>
        {
            // 使用 Unsafe.As 或其他 AOT 友好的方式
            public T Read(IDataReader reader)
            {
                // 通过构造函数创建匿名类型
                var ctor = typeof(T).GetConstructors()[0];
                return (T)ctor.Invoke(new object[] 
                { 
                    reader.GetInt32(0), 
                    reader.GetString(1) 
                });
            }
        }
    }
}

// 3. SqlxQueryProvider 使用
public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
{
    if (IsSelectExpression(expression, out var columns))
    {
        // 根据列组合查找对应的静态缓存
        var reader = FindProjectionReader<TElement>(columns);
        // ...
    }
}
```

### 方案 4：最简单的 AOT 方案 - 委托注册（推荐实现）

**优点：**
- 完全 AOT 兼容
- 实现极其简单
- 利用泛型静态类自动缓存
- 无需源生成器扩展

**实现思路：**

```csharp
// 1. 泛型静态缓存类（核心）
public static class SelectResultReader<T>
{
    // 每个类型 T 都有独立的静态字段，自动缓存
    private static IResultReader<T>? _instance;
    
    public static IResultReader<T>? Instance
    {
        get => _instance;
        set => _instance ??= value;  // 只能设置一次
    }
}

// 2. 用户手动注册（在查询前）
SelectResultReader<UserIdName>.Instance = new DelegateResultReader<UserIdName>(
    reader => new UserIdName 
    { 
        Id = reader.GetInt32(0), 
        Name = reader.GetString(1) 
    },
    new[] { "id", "name" }
);

// 3. 或者使用辅助方法
public static class SelectExtensions
{
    public static IQueryable<TResult> SelectWithReader<TSource, TResult>(
        this IQueryable<TSource> source,
        Expression<Func<TSource, TResult>> selector,
        Func<IDataReader, TResult> readerFunc,
        string[] columnNames)
    {
        // 自动注册到泛型静态缓存
        SelectResultReader<TResult>.Instance = new DelegateResultReader<TResult>(readerFunc, columnNames);
        
        return source.Select(selector);
    }
}

// 4. 使用
var result = SqlQuery.ForSqlite<User>()
    .SelectWithReader(
        u => new { u.Id, u.Name },
        reader => new { Id = reader.GetInt32(0), Name = reader.GetString(1) },
        new[] { "id", "name" }
    )
    .ToList();

// 5. DelegateResultReader 实现
internal class DelegateResultReader<T> : IResultReader<T>
{
    private readonly Func<IDataReader, T> _readFunc;
    private readonly string[] _columnNames;
    
    public DelegateResultReader(Func<IDataReader, T> readFunc, string[] columnNames)
    {
        _readFunc = readFunc;
        _columnNames = columnNames;
    }
    
    public T Read(IDataReader reader) => _readFunc(reader);
    
    public T Read(IDataReader reader, int[] ordinals)
    {
        // 简化版：直接调用 Read，因为委托内部已经知道如何读取
        return _readFunc(reader);
    }
    
    public int[] GetOrdinals(IDataReader reader)
    {
        var ordinals = new int[_columnNames.Length];
        for (int i = 0; i < _columnNames.Length; i++)
        {
            ordinals[i] = reader.GetOrdinal(_columnNames[i]);
        }
        return ordinals;
    }
}
```

### 方案 5：源生成器自动注册（最佳方案）

**结合方案 2 和方案 4：**

```csharp
// 1. 用户定义投影类型
[SqlxProjection(typeof(User), "id", "name")]  // 指定源类型和列
public partial class UserIdName
{
    public int Id { get; set; }
    public string Name { get; set; }
}

// 2. 源生成器自动生成
// Generated/UserIdName.Projection.g.cs
partial class UserIdName
{
    static UserIdName()
    {
        // 自动注册到泛型静态缓存
        global::Sqlx.SelectResultReader<UserIdName>.Instance = new UserIdNameResultReader();
    }
    
    private sealed class UserIdNameResultReader : global::Sqlx.IResultReader<UserIdName>
    {
        public UserIdName Read(IDataReader reader)
        {
            return new UserIdName
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1)
            };
        }
        
        public UserIdName Read(IDataReader reader, int[] ordinals)
        {
            return new UserIdName
            {
                Id = reader.GetInt32(ordinals[0]),
                Name = reader.GetString(ordinals[1])
            };
        }
        
        public int[] GetOrdinals(IDataReader reader)
        {
            return new[] 
            { 
                reader.GetOrdinal("id"), 
                reader.GetOrdinal("name") 
            };
        }
    }
}

// 3. SqlxQueryProvider 自动使用
public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
{
    // 检查泛型静态缓存
    var reader = SelectResultReader<TElement>.Instance;
    if (reader != null)
    {
        var newProvider = new SqlxQueryProvider<TElement>(Dialect)
        {
            Connection = Connection,
            ResultReader = reader
        };
        return new SqlxQueryable<TElement>(newProvider, expression);
    }
    
    // 原有逻辑...
}

// 4. 使用（完全类型安全）
var result = SqlQuery.ForSqlite<User>()
    .Select(u => new UserIdName { Id = u.Id, Name = u.Name })
    .ToList();
// 自动使用 SelectResultReader<UserIdName>.Instance
```

## 推荐实现步骤

### 第一阶段：基础设施（立即实现）

1. **添加泛型静态缓存类**
   ```csharp
   // src/Sqlx/SelectResultReader.cs
   public static class SelectResultReader<T>
   {
       private static IResultReader<T>? _instance;
       public static IResultReader<T>? Instance
       {
           get => _instance;
           set => _instance ??= value;
       }
   }
   ```

2. **修改 SqlxQueryProvider.CreateQuery**
   ```csharp
   public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
   {
       // 优先检查泛型静态缓存
       var reader = SelectResultReader<TElement>.Instance;
       if (reader != null)
       {
           var newProvider = new SqlxQueryProvider<TElement>(Dialect)
           {
               Connection = Connection,
               ResultReader = reader
           };
           return new SqlxQueryable<TElement>(newProvider, expression);
       }
       
       // 原有逻辑...
   }
   ```

3. **添加 DelegateResultReader**
   ```csharp
   // src/Sqlx/DelegateResultReader.cs
   public class DelegateResultReader<T> : IResultReader<T>
   {
       // 实现...
   }
   ```

### 第二阶段：源生成器支持（推荐）

1. **添加 SqlxProjectionAttribute**
   ```csharp
   [AttributeUsage(AttributeTargets.Class)]
   public class SqlxProjectionAttribute : Attribute
   {
       public Type SourceType { get; }
       public string[] Columns { get; }
       
       public SqlxProjectionAttribute(Type sourceType, params string[] columns)
       {
           SourceType = sourceType;
           Columns = columns;
       }
   }
   ```

2. **扩展 EntityProviderGenerator**
   - 检测 `[SqlxProjection]` 标记的类
   - 生成 ResultReader 实现
   - 生成静态构造函数自动注册

### 第三阶段：便利方法（可选）

1. **添加 SelectWithReader 扩展方法**
2. **添加更多辅助方法**

## 避免重复的关键点

1. **利用泛型静态类的单例特性**
   - `SelectResultReader<T>` 对每个 `T` 都是独立的单例
   - 无需字典，泛型参数本身就是"键"
   - 完全 AOT 兼容

2. **复用现有的源生成器**
   - 扩展 `EntityProviderGenerator` 支持 `[SqlxProjection]`
   - 生成代码结构与 `[SqlxEntity]` 类似
   - 自动注册到 `SelectResultReader<T>.Instance`

3. **复用 ColumnMeta 和列名转换**
   - 投影类型的列名转换复用 `ToSnakeCase` 逻辑
   - 可选支持 `[Column]` 属性自定义列名

4. **零运行时开销**
   - 静态构造函数在类型首次访问时执行一次
   - 之后直接使用缓存的 `Instance`
   - 无字典查找，无锁竞争

## 示例用法

### 方案 4：手动注册（简单场景）

```csharp
// 定义投影类型（普通类，无需 partial）
public class UserIdName
{
    public int Id { get; set; }
    public string Name { get; set; }
}

// 手动注册（应用启动时）
SelectResultReader<UserIdName>.Instance = new DelegateResultReader<UserIdName>(
    reader => new UserIdName 
    { 
        Id = reader.GetInt32(0), 
        Name = reader.GetString(1) 
    },
    new[] { "id", "name" }
);

// 使用
var result = SqlQuery.ForSqlite<User>()
    .WithConnection(conn)
    .Select(u => new UserIdName { Id = u.Id, Name = u.Name })
    .ToList();
```

### 方案 5：源生成器自动注册（推荐）

```csharp
// 1. 定义投影类型（partial 类）
[SqlxProjection(typeof(User))]
public partial class UserIdName
{
    public int Id { get; set; }
    public string Name { get; set; }
}

// 2. 源生成器自动生成注册代码（无需手动操作）

// 3. 直接使用
var result = SqlQuery.ForSqlite<User>()
    .WithConnection(conn)
    .Select(u => new UserIdName { Id = u.Id, Name = u.Name })
    .ToList();

// 4. 支持复杂投影
[SqlxProjection(typeof(User))]
public partial class UserSummary
{
    public int Id { get; set; }
    public string Name { get; set; }
    
    [Column("created_at")]  // 自定义列名
    public DateTime CreatedAt { get; set; }
}

// 5. 支持计算列（SQL 端计算）
var result = SqlQuery.ForSqlite<User>()
    .WithConnection(conn)
    .Select(u => new UserSummary 
    { 
        Id = u.Id, 
        Name = u.Name,
        CreatedAt = u.CreatedAt
    })
    .ToList();
```

## 性能对比

| 方案 | 首次调用 | 后续调用 | AOT 支持 | 内存分配 | 实现复杂度 |
|------|---------|---------|---------|---------|-----------|
| 源生成器（方案 5） | 0ms | ~10μs | ✅ | 最少 | 中 |
| 手动注册（方案 4） | 0ms | ~10μs | ✅ | 最少 | 低 |
| 表达式树 + 字典 | ~1-5ms | ~10μs | ✅ | 少 | 高 |
| Interceptor（方案 1） | 0ms | ~10μs | ✅ | 最少 | 高 |

## 结论

**推荐实现顺序：**

1. **立即实现方案 4（手动注册）**
   - 添加 `SelectResultReader<T>` 泛型静态类
   - 添加 `DelegateResultReader<T>` 实现
   - 修改 `SqlxQueryProvider.CreateQuery` 检查缓存
   - 工作量：~50 行代码，30 分钟

2. **后续实现方案 5（源生成器）**
   - 添加 `[SqlxProjection]` 特性
   - 扩展 `EntityProviderGenerator` 支持投影类型
   - 自动生成注册代码
   - 工作量：~200 行代码，2-3 小时

**核心优势：**
- ✅ 完全 AOT 兼容（无反射、无字典）
- ✅ 利用泛型静态类自动缓存（零开销）
- ✅ 复用现有源生成器架构（零重复）
- ✅ 类型安全（编译时检查）
- ✅ 性能最优（接近手写代码）

**与原方案对比：**
- ❌ 不使用 `ConcurrentDictionary`（字典查找有开销）
- ❌ 不使用表达式树编译（运行时开销）
- ✅ 使用泛型静态类（编译时确定，零开销）
- ✅ 使用源生成器（编译时生成，零运行时开销）
