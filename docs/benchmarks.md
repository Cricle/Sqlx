# 性能基准测试

## 快速摘要

**推荐版本：** .NET 10 (LTS) - 支持到 2028 年 11 月

### 小数据集性能（10-100条）- Web API 主要场景

| 数据量 | Sqlx | Dapper.AOT | FreeSql | Sqlx 优势 |
|--------|------|------------|---------|-----------|
| **单条查询** | **9.106 μs** | 13.024 μs | 79.452 μs | 🥇 快 43% / 8.7x |
| **10条** | **31.250 μs** | 31.911 μs | 37.286 μs | 🥇 快 2% / 19% |
| **100条** | **181.968 μs** | 167.526 μs | - | 🥈 慢 9% |
| **1000条** | **1,665.5 μs** | 1,531.0 μs | - | 🥈 慢 9% |

### 内存效率

| 数据量 | Sqlx | Dapper.AOT | FreeSql | Sqlx 优势 |
|--------|------|------------|---------|-----------|
| **单条查询** | **1.41 KB** | 2.66 KB | 10.17 KB | 🥇 少 47% / 7.2x |
| **10条** | **4.79 KB** | 6.70 KB | 8.89 KB | 🥇 少 40% / 86% |
| **100条** | **38.48 KB** | 45.66 KB | - | 🥇 少 19% |
| **1000条** | **361.73 KB** | 432.38 KB | - | 🥇 少 20% |

### GC 压力（关键指标）

| 数据量 | Sqlx Gen0/Gen1 | Dapper.AOT Gen0/Gen1 | FreeSql Gen0/Gen1 | Sqlx 优势 |
|--------|----------------|----------------------|-------------------|-----------|
| **单条查询** | 0.17 / 0 | 0.32 / 0 | 1.22 / 0.98 | 🥇 最低 GC 压力 |
| **10条** | 0.55 / 0 | 0.79 / 0 | 1.04 / 0.98 | 🥇 最低 GC 压力 |
| **100条** | 4.64 / 0.24 | 5.37 / 0.49 | - | 🥇 最低 Gen1 GC |
| **1000条** | 42.97 / 19.53 | 52.73 / 23.44 | - | 🥇 最低 GC 压力 |

**关键洞察：**
- ✅ Sqlx 在单条和小批量查询（1-10条）上性能最优，这是 Web API 的主要场景
- ✅ Sqlx 的 GC 压力最小，Gen1 GC 在所有场景下都是最低
- ✅ Sqlx 在单条查询上比 Dapper.AOT 快 43%，比 FreeSql 快 8.7 倍
- ✅ Sqlx 内存分配在所有场景下都最少，比 Dapper.AOT 少 19-47%
- ⚠️ Dapper.AOT 在大批量查询（100+条）上快约 9%，但内存占用高 20%

**AOT 兼容性：** ✅ 完全支持 Native AOT，通过 1564 个单元测试

**最新优化：**
- 内联展开 + 局部变量提取（消除数组边界检查）
- 结构体 Ordinals 支持（更好的可读性和类型安全）
- 移除 SQL 生成中的反射（GetEntityProvider）
- 支持 JOIN 查询（INNER JOIN, LEFT JOIN）
- 永远不生成 SELECT *，显式列出所有列

> 详细 AOT 性能分析见 [AOT_PERFORMANCE_RESULTS.md](../AOT_PERFORMANCE_RESULTS.md)

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
| **Sqlx** | **9.106 μs** | 0.108 μs | 0.090 μs | **1.00** | **1.41 KB** | **1.00** |
| Dapper.AOT | 13.024 μs | 0.250 μs | 0.350 μs | 1.43 | 2.66 KB | 1.89 |
| FreeSql | 79.452 μs | 1.541 μs | 3.750 μs | 8.73 | 10.17 KB | 7.23 |

**结论：** Sqlx 比 Dapper.AOT 快 43%，比 FreeSql 快 8.7 倍。内存分配最少，GC 压力最小。

**性能提升：** 相比之前版本，Sqlx 性能进一步优化（9.106 μs），得益于：
- 泛型 SqlQuery<T> 的 EntityProvider 缓存优化
- DynamicResultReader 静态方法缓存（IDataRecord.GetInt32/GetString 等）
- 完全消除运行时反射查找（移除 GetEntityProvider 反射）
- 支持 JOIN 查询，无性能损失
- 使用 SqlDialect 方法复用，减少代码重复

**AOT 兼容性：** ✅ 完全支持 Native AOT
- 通过 1344 个单元测试（100% 通过率）
- 使用表达式树编译，无运行时反射
- 静态方法缓存，零动态代码生成
- SQL 生成过程完全无反射

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

| Method | Mean | Error | StdDev | Ratio | Allocated | Alloc Ratio |
|--------|------|-------|--------|-------|-----------|-------------|
| **Sqlx** | **31.250 μs** | 0.185 μs | 0.173 μs | **1.00** | **4.79 KB** | **1.00** |
| Dapper.AOT | 31.911 μs | 0.095 μs | 0.084 μs | 1.02 | 6.70 KB | 1.40 |
| FreeSql | 37.286 μs | 0.445 μs | 0.416 μs | 1.19 | 8.89 KB | 1.86 |

**性能提升：** Sqlx 在小批量查询中最快，比 Dapper.AOT 快 2%，比 FreeSql 快 19%，内存分配最少。

### Limit = 100

| Method | Mean | Error | StdDev | Ratio | Allocated | Alloc Ratio |
|--------|------|-------|--------|-------|-----------|-------------|
| Dapper.AOT | 167.526 μs | 1.758 μs | 1.645 μs | 0.92 | 45.66 KB | 1.19 |
| **Sqlx** | **181.968 μs** | 1.167 μs | 1.092 μs | **1.00** | **38.48 KB** | **1.00** |

**结论：** Dapper.AOT 快 9%，但 Sqlx 内存少 19%，GC 压力更小。

### Limit = 1000

| Method | Mean | Error | StdDev | Ratio | Allocated | Alloc Ratio |
|--------|------|-------|--------|-------|-----------|-------------|
| Dapper.AOT | 1,531.0 μs | 8.17 μs | 7.25 μs | 0.92 | 432.38 KB | 1.20 |
| **Sqlx** | **1,665.5 μs** | 10.33 μs | 9.66 μs | **1.00** | **361.73 KB** | **1.00** |

**结论：** Dapper.AOT 快 9%，但 Sqlx 内存少 20%，更适合长时间运行的应用。

---

## 插入操作 (Insert)

| Method | Mean | Error | StdDev | Ratio | Allocated | Alloc Ratio |
|--------|------|-------|--------|-------|-----------|-------------|
| Dapper.AOT | 91.233 μs | 228.99 μs | 12.55 μs | 0.95 | 6.35 KB | 1.24 |
| **Sqlx** | **95.867 μs** | 264.92 μs | 14.52 μs | **1.00** | **5.12 KB** | **1.00** |
| FreeSql | 249.767 μs | 675.02 μs | 37.00 μs | 2.61 | 15.24 KB | 2.98 |

**结论：** Sqlx 与 Dapper.AOT 性能相当（慢 5%），但内存少 24%。比 FreeSql 快 2.6 倍，内存少 3 倍。

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
| 单条查询 | **快 43%** | **快 8.7x** |
| 列表查询（10条） | **快 2%** | **快 19%** |
| 列表查询（100条） | 慢 9% | - |
| 列表查询（1000条） | 慢 9% | - |
| 插入操作 | 慢 5% | **快 2.6x** |
| 更新操作 | **快 9%** | **快 4.2x** |
| 删除操作 | 持平 | **快 5.4x** |
| 计数操作 | 持平 | **快 50x** |

### 内存分配对比 (.NET 10)

| 场景 | Sqlx | Dapper.AOT | FreeSql |
|------|------|------------|---------|
| 单条查询 | **1.41 KB** | 2.66 KB | 10.17 KB |
| 列表查询(10) | **4.79 KB** | 6.70 KB | 8.89 KB |
| 列表查询(100) | **38.48 KB** | 45.66 KB | - |
| 列表查询(1000) | **361.73 KB** | 432.38 KB | - |
| 插入操作 | **5.12 KB** | 6.35 KB | 15.24 KB |
| 更新操作 | **3.27 KB** | 5.83 KB | 14.61 KB |
| 删除操作 | **904 B** | 1200 B | 8440 B |
| 计数操作 | **856 B** | 896 B | 5720 B |

### 优势总结

**Sqlx：**
- 单条查询性能领先（比 Dapper.AOT 快 43%）
- 小批量查询（10条）性能最优
- 内存分配始终最少（比 Dapper.AOT 少 19-47%）
- GC 压力最小，更适合长时间运行的应用
- 比 FreeSql 快 2.6-8.7 倍
- 泛型 SqlQuery<T> 缓存优化
- DynamicResultReader 支持匿名类型投影，完全 AOT 兼容
- ✅ 通过 2076 个单元测试，Native AOT 就绪
- ✅ SQL 生成完全无反射
- ✅ 支持 JOIN 查询，无性能损失

**Dapper.AOT：**
- 大批量列表读取（100+条）快约 9%
- 但内存占用高 20%，GC 压力更大

**适用场景：**
- Web API（单条/小批量查询）→ Sqlx
- 最小内存占用和 GC 压力 → Sqlx
- 大批量数据读取（追求极致速度）→ Dapper.AOT
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
- **Select 投影**：使用 DynamicResultReader 自动处理匿名类型，完全 AOT 兼容

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
