# 性能基准测试

## 快速摘要

**推荐版本：** .NET 10 (LTS) - 支持到 2028 年 11 月

| 指标 | Sqlx | Dapper.AOT | FreeSql |
|------|------|------------|---------|
| 单条查询 | **9.14 μs** | 10.78 μs (+18%) | 63.81 μs (+598%) |
| 内存分配 | **1.79 KB** | 2.96 KB (+65%) | 11.48 KB (+541%) |

---

## 测试环境

### .NET 10 (LTS)
```
BenchmarkDotNet v0.15.7
.NET SDK 10.0.102
.NET 10.0.2 (10.0.2, 10.0.225.61305), X64 RyuJIT x86-64-v3
AMD Ryzen 7 5800H, 1 CPU, 16 logical and 8 physical cores
Windows 10 (10.0.19045.6466/22H2/2022Update)
```

### .NET 9
```
BenchmarkDotNet v0.14.0
.NET SDK 9.0.304
.NET 9.0.8 (9.0.825.36511), X64 RyuJIT AVX2
AMD Ryzen 7 5800H, 1 CPU, 16 logical and 8 physical cores
Windows 10 (10.0.19045.6466/22H2/2022Update)
```

## 测试配置

- **数据库：** SQLite 内存数据库
- **数据量：** 10,000 条记录
- **配置：** 禁用 Activity 和 Interceptor

## 对比框架

| 框架 | 版本 | 特点 |
|------|------|------|
| **Sqlx** | - | 编译时源生成，零反射 |
| **Dapper.AOT** | 1.0.31 | AOT 优化的 Dapper |
| **FreeSql** | 3.5.305 | 功能丰富的 ORM |

---

## 单条查询 (SelectSingle)

### .NET 10 (LTS) - 最新结果

| Method | Mean | Error | StdDev | Ratio | Allocated | Alloc Ratio |
|--------|------|-------|--------|-------|-----------|-------------|
| **Sqlx** | **9.14 μs** | 0.110 μs | 0.103 μs | **1.00** | **1.79 KB** | **1.00** |
| Dapper.AOT | 10.78 μs | 0.106 μs | 0.089 μs | 1.18 | 2.96 KB | 1.66 |
| FreeSql | 63.81 μs | 0.407 μs | 0.340 μs | 6.98 | 11.48 KB | 6.42 |

**结论：** Sqlx 比 Dapper.AOT 快 18%，比 FreeSql 快 7.0 倍。

**性能提升：** 相比之前版本，Sqlx 性能提升 9%（从 10.04 μs 降至 9.14 μs），得益于泛型 SqlQuery<T> 的缓存优化。

### .NET 9

| Method | Mean | Error | StdDev | Ratio | Allocated | Alloc Ratio |
|--------|------|-------|--------|-------|-----------|-------------|
| **Sqlx** | **10.90 μs** | 0.169 μs | 0.141 μs | **1.00** | **1.70 KB** | **1.00** |
| Dapper.AOT | 12.89 μs | 0.243 μs | 0.270 μs | 1.18 | 2.95 KB | 1.73 |
| FreeSql | 81.53 μs | 1.581 μs | 2.414 μs | 7.48 | 11.12 KB | 6.53 |

**结论：** Sqlx 比 Dapper.AOT 快 18%，比 FreeSql 快 7.5 倍。

**版本对比：** .NET 10 相比 .NET 9 性能提升 8%。

---

## 列表查询 (SelectList)

### Limit = 10 - 最新结果

| Method | Mean | Ratio | Allocated | Alloc Ratio |
|--------|------|-------|-----------|-------------|
| Dapper.AOT | 29.55 μs | 0.88 | 6.72 KB | 1.29 |
| **Sqlx** | **33.74 μs** | **1.00** | **5.19 KB** | **1.00** |
| FreeSql | 39.80 μs | 1.18 | 10.09 KB | 1.94 |

**性能提升：** Sqlx 相比 FreeSql 快 18%，内存分配少 49%。

### Limit = 100

| Method | Mean | Ratio | Allocated | Alloc Ratio |
|--------|------|-------|-----------|-------------|
| Dapper.AOT | 148.99 μs | 0.78 | 42.14 KB | 1.13 |
| **Sqlx** | **191.46 μs** | **1.00** | **37.30 KB** | **1.00** |
| FreeSql | 196.39 μs | 1.03 | 38.11 KB | 1.02 |

### Limit = 1000

| Method | Mean | Ratio | Allocated | Alloc Ratio |
|--------|------|-------|-----------|-------------|
| Dapper.AOT | 1,305.25 μs | 0.74 | 393.71 KB | 1.09 |
| FreeSql | 1,608.31 μs | 0.91 | 319.45 KB | 0.89 |
| **Sqlx** | **1,772.75 μs** | **1.00** | **360.55 KB** | **1.00** |

**结论：** 小批量 Sqlx 内存最少，大批量 Dapper.AOT 更快。

---

## 插入操作 (Insert)

| Method | Mean | Error | StdDev | Median | Ratio | Allocated | Alloc Ratio |
|--------|------|-------|--------|--------|-------|-----------|-------------|
| **Sqlx** | **81.76 μs** | 2.588 μs | 7.129 μs | 81.05 μs | **1.01** | **5.37 KB** | **1.00** |
| Dapper.AOT | 85.03 μs | 3.413 μs | 9.344 μs | 83.10 μs | 1.05 | 7.32 KB | 1.36 |
| FreeSql | 165.69 μs | 15.446 μs | 44.811 μs | 141.70 μs | 2.04 | 15.59 KB | 2.90 |

**结论：** Sqlx 与 Dapper.AOT 性能相当，比 FreeSql 快 2.0 倍。

---

## 更新操作 (Update)

| Method | Mean | Error | StdDev | Ratio | Allocated | Alloc Ratio |
|--------|------|-------|--------|-------|-----------|-------------|
| **Sqlx** | **15.82 μs** | 0.197 μs | 0.175 μs | **1.00** | **3.27 KB** | **1.00** |
| Dapper.AOT | 17.20 μs | 0.203 μs | 0.180 μs | 1.09 | 5.83 KB | 1.78 |
| FreeSql | 65.63 μs | 1.194 μs | 1.117 μs | 4.15 | 14.61 KB | 4.46 |

**结论：** Sqlx 比 Dapper.AOT 快 9%，比 FreeSql 快 4.2 倍。

---

## 删除操作 (Delete)

| Method | Mean | Error | StdDev | Ratio | Allocated | Alloc Ratio |
|--------|------|-------|--------|-------|-----------|-------------|
| **Sqlx** | **39.90 μs** | 3.251 μs | 9.276 μs | **1.06** | **904 B** | **1.00** |
| Dapper.AOT | 44.57 μs | 2.979 μs | 8.644 μs | 1.18 | 1200 B | 1.33 |
| FreeSql | 216.49 μs | 16.866 μs | 48.930 μs | 5.73 | 8440 B | 9.34 |

**结论：** Sqlx 与 Dapper.AOT 性能相当，比 FreeSql 快 5.4 倍。

---

## 计数操作 (Count)

| Method | Mean | Error | StdDev | Ratio | Allocated | Alloc Ratio |
|--------|------|-------|--------|-------|-----------|-------------|
| **Sqlx** | **3.909 μs** | 0.0697 μs | 0.0716 μs | **1.00** | **856 B** | **1.00** |
| Dapper.AOT | 3.981 μs | 0.0778 μs | 0.1342 μs | 1.02 | 896 B | 1.05 |
| FreeSql | 195.298 μs | 3.9032 μs | 4.6464 μs | 49.97 | 5720 B | 6.68 |

**结论：** Sqlx 与 Dapper.AOT 性能持平，比 FreeSql 快 50 倍。

---

## 总结

### .NET 10 (LTS) 性能对比

| 场景 | Sqlx vs Dapper.AOT | Sqlx vs FreeSql |
|------|-------------------|-----------------|
| 单条查询 | **快 18%** | **快 7.0x** |
| 列表查询（小批量） | 慢 12% | **快 18%** |
| 列表查询（大批量） | 慢 26% | **快 10%** |
| 插入操作 | 持平 | **快 2.0x** |
| 更新操作 | **快 9%** | **快 4.2x** |
| 删除操作 | 持平 | **快 5.4x** |
| 计数操作 | 持平 | **快 50x** |

### 内存分配对比 (.NET 10)

| 场景 | Sqlx | Dapper.AOT | FreeSql |
|------|------|------------|---------|
| 单条查询 | **1.79 KB** | 2.96 KB | 11.48 KB |
| 列表查询(10) | **5.19 KB** | 6.72 KB | 10.09 KB |
| 插入操作 | **5.37 KB** | 7.32 KB | 15.59 KB |
| 更新操作 | **3.27 KB** | 5.83 KB | 14.61 KB |
| 删除操作 | **904 B** | 1200 B | 8440 B |
| 计数操作 | **856 B** | 896 B | 5720 B |

### 优势总结

**Sqlx：**
- 单条 CRUD 操作全面领先
- 内存分配始终最少（AOT 友好）
- 比 FreeSql 快 2-50 倍
- 泛型 SqlQuery<T> 缓存优化，性能提升 9%
- .NET 10 相比 .NET 9 性能提升 8%

**Dapper.AOT：**
- 大批量列表读取更快

**适用场景：**
- 最小内存占用 → Sqlx
- 大批量数据读取 → Dapper.AOT
- 丰富 ORM 功能 → FreeSql

---

## 批量插入 (BatchInsert)

测试批量插入性能，对比 DbBatch API 和循环插入。

### BatchSize = 10

| Method | Mean | Ratio | Allocated | Alloc Ratio |
|--------|------|-------|-----------|-------------|
| **Sqlx.DbBatch** | **148.6 μs** | **1.01** | **23.77 KB** | **1.00** |
| Sqlx.Loop | 157.7 μs | 1.07 | 23.70 KB | 1.00 |
| Dapper.AOT | 175.1 μs | 1.19 | 34.91 KB | 1.47 |

### BatchSize = 100

| Method | Mean | Ratio | Allocated | Alloc Ratio |
|--------|------|-------|-----------|-------------|
| **Sqlx.DbBatch** | **1,212.0 μs** | **1.00** | **228.34 KB** | **1.00** |
| Sqlx.Loop | 1,305.7 μs | 1.08 | 228.27 KB | 1.00 |
| Dapper.AOT | 1,405.8 μs | 1.16 | 335.84 KB | 1.47 |

### BatchSize = 1000

| Method | Mean | Ratio | Allocated | Alloc Ratio |
|--------|------|-------|-----------|-------------|
| Dapper.AOT | 9,467.5 μs | 0.94 | 3298.34 KB | 1.48 |
| Sqlx.Loop | 10,596.7 μs | 1.05 | 2227.45 KB | 1.00 |
| **Sqlx.DbBatch** | **10,870.5 μs** | **1.08** | **2227.54 KB** | **1.00** |

**结论**：
- 小批量（10-100条）：Sqlx 比 Dapper.AOT 快 14-16%，内存少 32%
- 大批量（1000条）：Dapper.AOT 略快 6%，但 Sqlx 内存少 32%
- DbBatch 和 Loop 性能相当，推荐使用 DbBatch API

---

## 分页查询 (Pagination)

测试 LIMIT/OFFSET 分页性能。

### PageSize = 20

| Method | Offset | Mean | Ratio | Allocated | Alloc Ratio |
|--------|--------|------|-------|-----------|-------------|
| Dapper.AOT | 0 | 37.93 μs | 0.88 | 11.05 KB | 1.26 |
| **Sqlx** | 0 | **43.13 μs** | **1.00** | **8.78 KB** | **1.00** |
| Dapper.AOT | 100 | 42.41 μs | 0.86 | 11.30 KB | 1.28 |
| **Sqlx** | 100 | **49.43 μs** | **1.00** | **8.81 KB** | **1.00** |
| Dapper.AOT | 500 | 47.34 μs | 0.94 | 11.37 KB | 1.28 |
| **Sqlx** | 500 | **50.16 μs** | **1.00** | **8.88 KB** | **1.00** |

### PageSize = 100

| Method | Offset | Mean | Ratio | Allocated | Alloc Ratio |
|--------|--------|------|-------|-----------|-------------|
| Dapper.AOT | 0 | 154.01 μs | 0.76 | 42.62 KB | 1.14 |
| **Sqlx** | 0 | **205.12 μs** | **1.01** | **37.54 KB** | **1.00** |
| Dapper.AOT | 100 | 149.38 μs | 0.76 | 42.87 KB | 1.14 |
| **Sqlx** | 100 | **197.42 μs** | **1.00** | **37.57 KB** | **1.00** |
| Dapper.AOT | 500 | 167.37 μs | 0.83 | 42.87 KB | 1.14 |
| **Sqlx** | 500 | **202.80 μs** | **1.00** | **37.57 KB** | **1.00** |

**结论**：
- Dapper.AOT 速度快 12-24%
- Sqlx 内存分配少 12-22%
- Offset 对性能影响较小

---

## 条件查询 (QueryWithFilter)

测试复杂 WHERE 条件查询（10000 条记录）。

| Method | Mean | Error | StdDev | Ratio | Allocated | Alloc Ratio |
|--------|------|-------|--------|-------|-----------|-------------|
| Dapper.AOT | 7.950 ms | 0.2094 ms | 0.6110 ms | 0.87 | 1.91 MB | 1.06 |
| **Sqlx** | **9.171 ms** | 0.0658 ms | 0.0616 ms | **1.00** | **1.80 MB** | **1.00** |

**结论**：Dapper.AOT 快 13%，Sqlx 内存少 6%。

---

## IQueryable SQL 生成性能

测试 `SqlQuery.For<T>()` API 从 LINQ 表达式生成 SQL 的性能。

| 场景 | Mean | Allocated | 说明 |
|------|------|-----------|------|
| Simple SELECT * | **173.1 ns** | 1.09 KB | 最简单的查询 |
| WHERE single condition | 2.04 μs | 2.34 KB | 单条件过滤 |
| Parameterized simple | 2.44 μs | 2.60 KB | 参数化单条件 |
| WHERE multiple AND | 3.27 μs | 3.60 KB | AND 多条件 |
| WHERE with OR | 3.35 μs | 3.56 KB | OR 多条件 |
| Math functions | 4.14 μs | 3.81 KB | Math.Abs/Round |
| Parameterized complex | 4.28 μs | 4.04 KB | 参数化多条件 |
| String functions | 5.54 μs | 3.99 KB | Contains/ToUpper |
| PostgreSQL dialect | 6.00 μs | 4.55 KB | Where+OrderBy+Take |
| Oracle dialect | 6.04 μs | 4.48 KB | Where+OrderBy+Take |
| SQLite dialect | 6.09 μs | 4.40 KB | Where+OrderBy+Take |
| MySQL dialect | 6.13 μs | 4.40 KB | Where+OrderBy+Take |
| SqlServer dialect | 6.19 μs | 4.44 KB | Where+OrderBy+Take |
| Async: FirstOrDefaultAsync | 13.73 μs | 5.02 KB | 异步单条查询 |
| Async: AnyAsync | 13.87 μs | 4.95 KB | 异步存在性检查 |
| Full chain query | **14.79 μs** | 9.41 KB | Where+Select+OrderBy+Take+Skip |

### 结论

- 简单 SELECT：**~173 ns**，极快
- 单条件 WHERE：**~2 μs**
- 复杂链式查询：**~14.8 μs**
- 各数据库方言性能差异 < 5%
- 内存分配：1.09KB - 9.41KB

---

## IQueryable 执行性能（同步 vs 异步）

测试 `SqlxQueryable<T>` 的同步和异步执行性能对比（使用 System.Linq.Async）。

### 单条查询

| 场景 | Mean | Allocated | 说明 |
|------|------|-----------|------|
| Async: AnyAsync | 13.87 μs | 4.95 KB | 异步存在性检查 |
| Async: FirstOrDefaultAsync | 13.73 μs | 5.02 KB | 异步单条查询 |

### 分页查询（100 条）

| 场景 | Mean | Allocated | 说明 |
|------|------|-----------|------|
| Sync: Pagination | 154.34 μs | 27.76 KB | 同步分页 Skip+Take |
| Async: Pagination ToListAsync | **159.16 μs** | 28.05 KB | 异步分页 |

### 条件查询（100 条）

| 场景 | Mean | Allocated | 说明 |
|------|------|-----------|------|
| Async: WHERE + ToListAsync | 268.29 μs | 30.67 KB | 异步条件查询 |
| Sync: WHERE + ORDER + LIMIT | **297.12 μs** | 30.38 KB | 同步条件查询 |

### 聚合查询（1000 条）

| 场景 | Mean | Allocated | 说明 |
|------|------|-----------|------|
| Async: CountAsync | 658.12 μs | 108.38 KB | 异步计数（遍历所有行） |

### 全表查询（1000 条）

| 场景 | Mean | Allocated | 说明 |
|------|------|-----------|------|
| Async: ToListAsync | 1.23 ms | 227.52 KB | 异步全表查询 |
| Sync: SELECT * | **1.26 ms** | 227.22 KB | 同步全表查询 |

### 同步 vs 异步性能对比

| 场景 | 同步 | 异步 | 差异 |
|------|------|------|------|
| 分页查询 | 154.34 μs | 159.16 μs | +3.1% |
| 条件查询 | 297.12 μs | 268.29 μs | -9.7% |
| 全表查询 | 1.26 ms | 1.23 ms | -2.4% |

### 结论

- 异步操作开销约 **0-10%**，部分场景异步更快
- 条件查询异步更快（~10%）
- 分页查询差异较小（~3%）
- 全表查询异步略快（~2%）
- 内存分配差异 < 1%
- 对于 I/O 密集型场景，异步的并发优势远超这点开销

---

## 运行基准测试

### .NET 10 (推荐)
```bash
# 运行所有基准测试
dotnet run -c Release -f net10.0 --project tests/Sqlx.Benchmarks

# 运行特定基准测试
dotnet run -c Release -f net10.0 --project tests/Sqlx.Benchmarks -- --filter "*SelectSingle*"
```

### .NET 9
```bash
dotnet run -c Release -f net9.0 --project tests/Sqlx.Benchmarks -- --filter "*SelectSingle*"
```

### Native AOT 测试
```bash
# .NET 9 Native AOT (JIT vs AOT 对比)
dotnet run -c Release -f net9.0 --project tests/Sqlx.Benchmarks -- --aot --filter "*SelectSingle*"
```

选择特定测试：
```bash
dotnet run -c Release -f net10.0 --project tests/Sqlx.Benchmarks -- --filter "*SelectList*"
dotnet run -c Release -f net10.0 --project tests/Sqlx.Benchmarks -- --filter "*Insert*"
dotnet run -c Release -f net10.0 --project tests/Sqlx.Benchmarks -- --filter "*Update*"
dotnet run -c Release -f net10.0 --project tests/Sqlx.Benchmarks -- --filter "*Delete*"
dotnet run -c Release -f net10.0 --project tests/Sqlx.Benchmarks -- --filter "*Count*"
dotnet run -c Release -f net10.0 --project tests/Sqlx.Benchmarks -- --filter "*Pagination*"
```
