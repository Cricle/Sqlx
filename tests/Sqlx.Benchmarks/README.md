# Sqlx Benchmarks

性能测试项目，对比 Sqlx 和 Dapper.AOT 的性能。

## 运行基准测试

### 标准 JIT 测试

```bash
# 运行所有基准测试
dotnet run -c Release

# 运行特定基准测试
dotnet run -c Release -- --filter "*SelectSingle*"
dotnet run -c Release -- --filter "*SelectList*"
dotnet run -c Release -- --filter "*Insert*"
dotnet run -c Release -- --filter "*Update*"
dotnet run -c Release -- --filter "*Delete*"
dotnet run -c Release -- --filter "*Count*"
dotnet run -c Release -- --filter "*Pagination*"
dotnet run -c Release -- --filter "*StaticOrdinals*"
dotnet run -c Release -- --filter "*DynamicResultReaderOrdinal*"
dotnet run -c Release -- --filter "*PureMappingResultReader*"
dotnet run -c Release -- --filter "*TypeConverter*"
dotnet run -c Release -- --filter "*ParameterExtraction*"

# 运行 ExpressionBlockResult 性能测试（新增）⚡
dotnet run -c Release -- --filter "*ExpressionBlockResult*"
# 或使用 PowerShell 脚本
pwsh run-expression-benchmark.ps1
```

### ExpressionBlockResult 性能测试（新增）⚡

**测试内容：** 对比统一表达式解析 vs 传统分离解析的性能

```bash
# 运行 ExpressionBlockResult benchmark
pwsh run-expression-benchmark.ps1
```

### DynamicResultReader Ordinals Benchmark

**测试内容：** 对比 DynamicResultReader 在不同 ordinals 消费路径下的开销

```bash
dotnet run -c Release -- --filter "*DynamicResultReaderOrdinal*"
```

**测试场景：**
1. `ToList` / `ToListAsync` 快路径
   - 扩展方法内部租借一次 ordinals 数组并整批复用
2. 手写 span 循环
   - 每行走 `Read(IDataReader, ReadOnlySpan<int>)`
   - 会触发 DynamicResultReader 的兼容性兜底路径
3. 手写数组循环
   - 直接走 `IArrayOrdinalReader<T>` 快路径
   - 用来验证扩展方法是否已经接近最优路径

**最新 ShortRun 结果（.NET 10, RowCount=1000）：**
- `ToList` fast path: **1.015 ms**
- manual span loop: `1.022 ms`
- manual pooled array loop: `1.044 ms`
- manual span loop async: `1.116 ms`
- `ToListAsync` fast path: `1.176 ms`

**结论：**
- 端到端查询里，数据库执行时间占主导，DynamicResultReader 的不同 ordinals 路径差距不会特别大
- `ToList` 快路径已经处在最优组，说明扩展方法快路径没有明显回退
- 如果要观察映射层本身的收益，更适合看下面的 Pure Mapping benchmark

### Pure Mapping ResultReader Benchmark

**测试内容：** 使用内存 `DbDataReader` 隔离 ResultReader 本身的映射成本，不受 SQLite 命令执行影响

```bash
dotnet run -c Release -- --filter "*PureMappingResultReader*"
```

**测试场景：**
1. Generated reader + cached ordinals
2. Generated reader + uncached ordinals
3. Dynamic reader + array fast path
4. Dynamic reader + span fallback

**最新 ShortRun 结果：**

| Method | RowCount | Mean | Ratio |
|--------|---------:|-----:|------:|
| Generated cached ordinals | 100 | **6.378 μs** | **1.00** |
| Dynamic array fast path | 100 | 8.770 μs | 1.38 |
| Dynamic span fallback | 100 | 10.926 μs | 1.72 |
| Generated uncached ordinals | 100 | 29.178 μs | 4.59 |
| Generated cached ordinals | 1000 | **60.158 μs** | **1.00** |
| Dynamic array fast path | 1000 | 81.224 μs | 1.35 |
| Dynamic span fallback | 1000 | 106.770 μs | 1.78 |
| Generated uncached ordinals | 1000 | 276.220 μs | 4.59 |

**结论：**
- generated reader 的 cached ordinals 比 uncached ordinals 快约 **4.6 倍**
- dynamic reader 的 array fast path 比 span fallback 快约 **24%**
- 这个 benchmark 明确证明了 cached ordinals、`IArrayOrdinalReader<T>` 和 ArrayPool 优化在映射层本身是有效的

### TypeConverter Benchmark

**测试内容：** 对比 `TypeConverter` 的关键转换路径与直接 BCL 调用的开销

```bash
dotnet run -c Release -- --filter "*TypeConverter*"
```

**测试场景：**
1. 相同类型直返
2. `string -> decimal`
3. `string -> DateTimeOffset`
4. `string -> DateOnly`
5. `ticks -> TimeSpan`
6. `string -> TimeOnly`
7. `byte[] -> Guid`

**用途：**
- 量化 `TypeConverter` 自身的常见转换开销
- 对比直接 BCL 调用与 `TypeConverter` 封装之间的差距
- 为后续是否继续优化 `TypeConverter` 提供数据依据

**最新 ShortRun 结果（.NET 10）：**

| Method | Mean | Allocated |
|--------|-----:|----------:|
| BCL: TimeSpan.FromTicks | 0.602 ns | - |
| BCL: new Guid(byte[]) | 2.549 ns | - |
| TypeConverter: byte[] -> Guid | 12.308 ns | - |
| TypeConverter: ticks -> TimeSpan | 14.233 ns | 24 B |
| TypeConverter: Int32 same type | 14.891 ns | 24 B |
| BCL: decimal.Parse invariant | 189.843 ns | - |
| TypeConverter: string -> decimal | 190.496 ns | - |
| TypeConverter: string -> TimeOnly | 305.121 ns | 24 B |
| BCL: TimeOnly.Parse invariant | 317.617 ns | - |
| TypeConverter: string -> DateOnly | 334.068 ns | 24 B |
| BCL: DateOnly.Parse invariant | 342.090 ns | - |
| TypeConverter: string -> DateTimeOffset | 549.464 ns | - |
| BCL: DateTimeOffset.Parse invariant | 565.486 ns | - |

**结论：**
- 同类型直返、ticks 和 `byte[] -> Guid` 这类轻量路径依然是纳秒级，包装成本很低
- `string -> DateOnly` / `string -> TimeOnly` 与直接 BCL `Parse` 已经非常接近，新增现代日期类型支持没有带来明显额外开销
- `string -> DateTimeOffset` 仍然和直接 BCL `Parse` 处于同一量级
- 当前 `TypeConverter` 更值得关注的是高层映射调用场景，而不是基础解析本身

### Parameter Extraction Benchmark

**测试内容：** 对比 `PlaceholderProcessor.ExtractParameters()` 在不同 SQL 形态下的开销

```bash
dotnet run -c Release -- --filter "*ParameterExtraction*"
```

**测试场景：**
1. 简单参数 SQL
2. 重复参数 SQL
3. 含字符串和注释的 SQL
4. 含 PostgreSQL cast / dollar-quoted string 的 SQL

**用途：**
- 为当前单次扫描实现建立性能基线
- 确认支持字符串、注释、`::cast`、dollar-quoted string 后没有明显性能回退

**测试场景：**
1. **UPDATE 表达式解析**
   - 传统方式：`ToSetClause()` + `GetSetParameters()` (2 次遍历)
   - ExpressionBlockResult：`ParseUpdate()` (1 次遍历)
   - **预期结果：快 ~2 倍** ⚡

2. **WHERE 表达式解析**
   - 传统方式：`ToWhereClause()` + `GetParameters()` (2 次遍历)
   - ExpressionBlockResult：`Parse()` (1 次遍历)
   - **预期结果：快 ~2 倍** ⚡

3. **完整场景（UPDATE + WHERE）**
   - 传统方式：4 次遍历
   - ExpressionBlockResult：2 次遍历
   - **预期结果：快 ~2 倍** ⚡

4. **内存分配测试**
   - 对比两种方式的内存分配情况
   - ExpressionBlockResult 应该有更少的内存分配

**示例输出：**
```
| Method                                      | Mean      | Ratio | Allocated |
|-------------------------------------------- |----------:|------:|----------:|
| ExpressionBlockResult: ParseUpdate          |  1.234 μs |  0.50 |     1 KB  |
| Traditional: ToSetClause + GetSetParameters |  2.468 μs |  1.00 |     2 KB  |
```

### Native AOT 测试

```bash
# 运行 .NET 9 Native AOT 测试（对比 JIT vs AOT）
dotnet run -c Release -f net9.0 -- --aot --filter "*SelectSingle*"

# 运行 .NET 10 Native AOT 测试（LTS 版本）
dotnet run -c Release -f net10.0 -- --net10-aot --filter "*SelectSingle*"

# 运行所有测试的 .NET 10 AOT 对比
dotnet run -c Release -f net10.0 -- --net10-aot
```

**注意**：
- Native AOT 测试需要较长的编译时间（首次运行可能需要几分钟）
- .NET 10 是 LTS（长期支持）版本，支持到 2028 年 11 月
- AOT 测试会同时运行 JIT 和 Native AOT 版本进行对比

**预期结果**：
- Sqlx 在 JIT 和 AOT 模式下性能应该相近（因为使用源生成器）
- Dapper.AOT 同样在两种模式下性能相近
- 两者都应该比使用反射的 ORM（如 FreeSql）快得多
- .NET 10 相比 .NET 9 应该有额外的性能提升

## 测试配置

### AotConfig
- **JIT**: 标准 .NET 9.0 JIT 编译
- **NativeAOT-Net9**: .NET 9.0 Native AOT 编译

### Net10AotConfig
- **JIT-Net10**: .NET 10.0 JIT 编译
- **NativeAOT-Net10**: .NET 10.0 Native AOT 编译

## 基准测试结果

### 单条查询 (SelectSingleBenchmark)

| Method | Mean | Allocated | Ratio |
|--------|------|-----------|-------|
| **Sqlx** | **8.31 μs** | **1.7 KB** | **1.00** |
| Dapper.AOT | 9.94 μs | 2.95 KB | 1.20 |

**结论**: Sqlx 比 Dapper.AOT 快 20%，内存占用减少 42%

### COUNT 查询 (CountBenchmark)

| Method | Mean | Allocated | Ratio |
|--------|------|-----------|-------|
| **Sqlx** | **3.78 μs** | **856 B** | **1.00** |
| Dapper.AOT | 3.77 μs | 896 B | 1.00 |

**结论**: 性能持平，Sqlx 内存略少

### 插入操作 (InsertBenchmark)

| Method | Mean | Allocated | Ratio |
|--------|------|-----------|-------|
| Dapper.AOT | 73.29 μs | 7.32 KB | 0.97 |
| **Sqlx** | **76.61 μs** | **4.94 KB** | **1.00** |

**结论**: 性能持平，Sqlx 内存少 32%

### 更新操作 (UpdateBenchmark)

| Method | Mean | Allocated | Ratio |
|--------|------|-----------|-------|
| **Sqlx** | **14.37 μs** | **3.27 KB** | **1.00** |
| Dapper.AOT | 16.01 μs | 5.83 KB | 1.11 |

**结论**: Sqlx 快 11%，内存少 44%

### 删除操作 (DeleteBenchmark)

| Method | Mean | Allocated | Ratio |
|--------|------|-----------|-------|
| **Sqlx** | **38.20 μs** | **1.16 KB** | **1.00** |
| Dapper.AOT | 50.21 μs | 1.45 KB | 1.31 |

**结论**: Sqlx 快 24%，内存少 20%

### 列表查询 (SelectListBenchmark)

| Method | Limit | Mean | Allocated | Ratio |
|--------|-------|------|-----------|-------|
| Dapper.AOT | 10 | 21.13 μs | 6.55 KB | 0.89 |
| **Sqlx** | 10 | **23.85 μs** | **6.45 KB** | **1.00** |
| Dapper.AOT | 100 | 128.74 μs | 42.14 KB | 0.81 |
| **Sqlx** | 100 | **159.80 μs** | **39.9 KB** | **1.00** |
| Dapper.AOT | 1000 | 1,131 μs | 393.71 KB | 0.76 |
| **Sqlx** | 1000 | **1,491 μs** | **370.19 KB** | **1.00** |

**结论**: Dapper.AOT 批量读取更快，Sqlx 内存更少

### SqlTemplate Render (SqlTemplateRenderBenchmark)

**最新 ShortRun 结果（.NET 10）**

| Method | Mean | Allocated | 说明 |
|--------|------|-----------|------|
| Single param: Dictionary Render | **889.6 ns** | 2.01 KB | 预先构造字典并复用 |
| Single param: Optimized Render(key, value) | 978.4 ns | **2.01 KB** | 避免调用方每次新建字典 |
| Single param: New Dictionary each call | 970.8 ns | 2.22 KB | 每次调用都分配新字典 |
| Double param: Optimized Render(k1,v1,k2,v2) | **1.639 μs** | **3.48 KB** | 线程本地小型参数包 |
| Double param: Dictionary Render | 1.881 μs | 3.48 KB | 预先构造字典并复用 |
| Double param: New Dictionary each call | 1.953 μs | 3.69 KB | 每次调用都分配新字典 |

**结论**
- 单参数场景下，传入一个预先构造并复用的字典仍然是最快路径
- 单参数优化重载的主要价值是避免调用方手动创建字典，时间上接近字典路径，分配低于“每次新建字典”
- 双参数场景下，优化重载已经是最快路径，同时避免了每次构造字典的额外分配

### ResultReader / Ordinals 热路径

**DynamicResultReader Ordinals Benchmark（ShortRun, .NET 10, RowCount=1000）**

| Method | Mean | Allocated |
|--------|------|-----------|
| DynamicReader: ToList fast path | **1.015 ms** | 174.22 KB |
| DynamicReader: manual span loop | 1.022 ms | 165.88 KB |
| DynamicReader: manual pooled array loop | 1.044 ms | 165.88 KB |
| DynamicReader: manual span loop async | 1.116 ms | 166.05 KB |
| DynamicReader: ToListAsync fast path | 1.176 ms | 174.43 KB |

**Pure Mapping ResultReader Benchmark（ShortRun, .NET 10）**

| Method | RowCount | Mean | Ratio |
|--------|---------:|-----:|------:|
| Generated cached ordinals | 100 | **6.378 μs** | **1.00** |
| Dynamic array fast path | 100 | 8.770 μs | 1.38 |
| Dynamic span fallback | 100 | 10.926 μs | 1.72 |
| Generated uncached ordinals | 100 | 29.178 μs | 4.59 |
| Generated cached ordinals | 1000 | **60.158 μs** | **1.00** |
| Dynamic array fast path | 1000 | 81.224 μs | 1.35 |
| Dynamic span fallback | 1000 | 106.770 μs | 1.78 |
| Generated uncached ordinals | 1000 | 276.220 μs | 4.59 |

**结论**
- 真实 SQLite 查询路径里，ResultReader 优化收益会被数据库执行时间稀释
- 纯映射 benchmark 明确表明：generated cached ordinals 比 uncached ordinals 快约 4.6 倍
- DynamicResultReader 的 array fast path 比 span fallback 快约 24%，说明 `IArrayOrdinalReader<T>` 和 ArrayPool 优化命中了映射层热路径

### ColumnNameResolver 热路径

**ColumnNameResolver Benchmark（ShortRun, .NET 10）**

| Method | Mean | Allocated |
|--------|------|-----------|
| Resolve fallback snake_case | **21.09 ns** | 0 B |
| Resolve second mapped column | 25.28 ns | 0 B |
| Resolve mapped column | 42.20 ns | 0 B |

**结论**
- provider 映射解析已经稳定在几十纳秒级，并且没有额外分配
- `TableNameResolver` / `EntityProviderResolver` 之外，`ColumnNameResolver` 现在也具备很低的元数据访问成本
- 表达式解析、聚合选择器、`SetClause` 等多处共享这条热路径，缓存化映射表让这些场景的重复列名解析更轻

### TableNameResolver 热路径

**TableNameResolver Benchmark（ShortRun, .NET 10）**

| Method | Mean | Allocated |
|--------|------|-----------|
| Resolve type-name fallback | **16.78 ns** | 0 B |
| Resolve static table name | 31.24 ns | 0 B |
| Resolve dynamic method table name | 44.62 ns | 0 B |

**结论**
- `TableNameResolver` 现在把 attribute/method 元数据查找缓存下来，静态表名解析稳定在几十纳秒级
- 动态表名方法仍然保持“每次调用都重新求值”的语义，但公开静态方法路径已经避免了每次 `MethodInfo.Invoke` 的反射调用成本
- 表名解析本身没有额外分配，适合继续作为查询翻译主路径的一部分

### SnakeCaseConversion 热路径

**SnakeCaseConversion Benchmark（ShortRun, .NET 10）**

| Method | Mean | Allocated |
|--------|------|-----------|
| SnakeCase cached acronym | **12.29 ns** | 0 B |
| SnakeCase cached lowercase | 14.07 ns | 0 B |
| SnakeCase core mixed case | 48.10 ns | 48 B |
| SnakeCase core acronym | 69.52 ns | 56 B |

**结论**
- `ConvertToSnakeCase` 的缓存命中路径已经稳定在十几纳秒级，没有额外分配
- 新的 acronym 兼容算法虽然比简单大小写场景更重，但 uncached 路径仍然保持在几十纳秒级
- 运行时反射 fallback 现在和源生成器使用同一套 acronym 边界规则，性能和一致性都更可控

### 测试环境

- BenchmarkDotNet v0.14.0
- .NET 9.0.8
- SQLite (内存数据库)
- 10,000 条测试数据
- AMD Ryzen 7 5800H

## 测试场景

1. **SelectSingleBenchmark** - 按 ID 查询单条记录
2. **SelectListBenchmark** - 查询多条记录 (10/100/1000 条)
3. **InsertBenchmark** - 插入单条记录并返回 ID
4. **UpdateBenchmark** - 更新单条记录
5. **DeleteBenchmark** - 删除单条记录
6. **QueryWithFilterBenchmark** - 带 WHERE 条件的查询
7. **CountBenchmark** - COUNT 聚合查询
8. **PaginationBenchmark** - 分页查询
9. **ColumnNameResolverBenchmark** - provider 元数据列名解析热路径
10. **TableNameResolverBenchmark** - 表名元数据解析与动态表名方法热路径
11. **SnakeCaseConversionBenchmark** - snake_case 转换缓存命中与核心算法热路径
12. **StaticOrdinalsBenchmark** - 静态列序号 vs 动态列序号

## 为什么 Sqlx 更快？

1. **编译时生成** - SQL 模板在编译时处理，运行时零解析开销
2. **静态 SqlTemplate** - 预编译的 SQL 模板，避免重复字符串处理
3. **直接 ADO.NET** - 生成的代码直接调用 ADO.NET，无中间层
4. **最小内存分配** - 避免不必要的对象创建和装箱
5. **缓存列序号** - ResultReader 缓存列序号，避免重复查找
6. **高效 Render** - 动态占位符渲染使用预计算的段落拼接

## AOT 兼容性

Sqlx 和 Dapper.AOT 都支持 Native AOT 编译。两者都使用源生成器在编译时生成代码，避免运行时反射。

在 AOT 场景下，两者的性能差异与 JIT 模式相似，因为：
- 两者都不使用运行时反射
- 两者都在编译时生成所有必要的代码
- 性能差异主要来自代码生成策略和内存分配模式
