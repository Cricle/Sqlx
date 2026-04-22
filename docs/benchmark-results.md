# 性能基准测试结果

本文档包含 Sqlx 与其他主流 ORM 框架的性能对比测试结果。

**相关文档**:
- [Native AOT 性能测试结果](./aot-benchmark-results.md) - JIT vs AOT 性能对比

## 测试环境

- **BenchmarkDotNet**: v0.15.7
- **OS**: Linux Debian GNU/Linux 12 (bookworm)
- **CPU**: Intel Xeon Platinum 8457C 2.60GHz, 2 CPU, 4 logical and 4 physical cores
- **Runtime**: .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
- **GC**: Concurrent Workstation
- **测试日期**: 2026-04-22

---

## 单行查询 (SelectSingleBenchmark)

SQLite 内存数据库，10,000 行，按 ID 查询单行。

| 方法 | Mean | Error | StdDev | Ratio | Rank | Gen0 | Gen1 | Allocated | Alloc Ratio |
|------|-----:|------:|-------:|------:|-----:|-----:|-----:|----------:|------------:|
| **Sqlx** | **10.52 μs** | 0.201 μs | 0.157 μs | **1.00** | **1** | 0.0458 | - | 2.88 KB | 1.00 |
| Dapper.AOT | 11.51 μs | 0.156 μs | 0.146 μs | 1.09 | 2 | 0.0458 | - | 2.66 KB | 0.92 |
| FreeSql | 27.69 μs | 0.322 μs | 0.285 μs | 2.63 | 3 | 0.1831 | 0.1221 | 10.24 KB | 3.55 |

**结论**：Sqlx 比 Dapper.AOT 快 **8.6%**，比 FreeSql 快 **2.6×**。

---

## 批量查询 (SelectListBenchmark)

SQLite 内存数据库，10,000 行，按条件查询 N 行。

| 方法 | Limit | Mean | Error | StdDev | Ratio | Rank | Gen0 | Gen1 | Allocated | Alloc Ratio |
|------|------:|-----:|------:|-------:|------:|-----:|-----:|-----:|----------:|------------:|
| FreeSql | 10 | 38.51 μs | 0.536 μs | 0.475 μs | 0.98 | 1 | 0.1221 | 0.0610 | 8.67 KB | 1.41 |
| **Sqlx** | **10** | **39.14 μs** | 0.645 μs | 0.603 μs | **1.00** | **1** | 0.1221 | - | **6.16 KB** | **1.00** |
| Dapper.AOT | 10 | 39.53 μs | 0.762 μs | 0.935 μs | 1.01 | 1 | 0.1221 | - | 6.55 KB | 1.06 |
| | | | | | | | | | | |
| Dapper.AOT | 100 | 246.23 μs | 3.654 μs | 3.418 μs | 0.99 | 1 | 0.4883 | - | 45.66 KB | 1.19 |
| **Sqlx** | **100** | **249.45 μs** | 3.514 μs | 3.287 μs | **1.00** | **1** | 0.4883 | - | **38.48 KB** | **1.00** |
| FreeSql | 100 | 250.43 μs | 3.543 μs | 3.314 μs | 1.00 | 1 | 0.4883 | - | 37.22 KB | 0.97 |
| | | | | | | | | | | |
| FreeSql | 1000 | 2,311.99 μs | 29.142 μs | 27.260 μs | 0.94 | 1 | 3.9063 | - | 318.49 KB | 0.88 |
| Dapper.AOT | 1000 | 2,368.24 μs | 33.918 μs | 31.727 μs | 0.97 | 1 | 7.8125 | - | 432.38 KB | 1.20 |
| **Sqlx** | **1000** | **2,452.30 μs** | 32.791 μs | 29.068 μs | **1.00** | **1** | 3.9063 | - | **361.73 KB** | **1.00** |

**结论**：
- 10 行：三者性能相当，Sqlx 内存最优（比 Dapper.AOT 少 **6%**）
- 100 行：三者性能相当，Sqlx 内存比 Dapper.AOT 少 **16%**
- 1000 行：Sqlx 比 Dapper.AOT 内存少 **16.4%**，GC Gen1 压力更低

---

## Sqlx vs Dapper.AOT vs EF Core (EfCoreComparisonBenchmark)

SQLite 内存数据库，10,000 行，EF Core 使用 `NoTracking`。

| 方法 | Mean | Error | StdDev | Ratio | Rank | Gen0 | Allocated | Alloc Ratio |
|------|-----:|------:|-------:|------:|-----:|-----:|----------:|------------:|
| Dapper.AOT: Count | 3.606 μs | 0.056 μs | 0.052 μs | 0.33 | 1 | 0.0114 | 720 B | 0.24 |
| **Sqlx: Count** | **3.651 μs** | 0.045 μs | 0.040 μs | **0.34** | **1** | 0.0114 | 776 B | 0.26 |
| **Sqlx: GetById** | **10.783 μs** | 0.182 μs | 0.243 μs | **1.00** | **2** | 0.0458 | 2,952 B | 1.00 |
| Dapper.AOT: GetById | 11.708 μs | 0.131 μs | 0.110 μs | 1.09 | 3 | 0.0458 | 2,728 B | 0.92 |
| EF Core: Count | 34.306 μs | 0.433 μs | 0.362 μs | 3.18 | 4 | 0.1221 | 9,224 B | 3.12 |
| **Sqlx: GetList(10)** | **44.271 μs** | 0.875 μs | 0.972 μs | **4.11** | **5** | 0.1221 | 6,576 B | 2.23 |
| Dapper.AOT: GetList(10) | 44.797 μs | 0.857 μs | 0.760 μs | 4.16 | 5 | 0.1221 | 7,000 B | 2.37 |
| EF Core: Find | 45.974 μs | 0.543 μs | 0.454 μs | 4.27 | 5 | 0.1221 | 12,040 B | 4.08 |
| EF Core: FromSql | 54.688 μs | 0.767 μs | 0.640 μs | 5.07 | 6 | 0.2441 | 16,440 B | 5.57 |
| EF Core: GetList(10) | 87.396 μs | 1.397 μs | 1.307 μs | 8.11 | 7 | 0.2441 | 18,584 B | 6.30 |
| **Sqlx: GetList(100)** | **272.238 μs** | 3.158 μs | 2.637 μs | **25.26** | **8** | 0.4883 | 39,256 B | 13.30 |
| Dapper.AOT: GetList(100) | 276.973 μs | 3.585 μs | 3.178 μs | 25.70 | 8 | 0.4883 | 45,896 B | 15.55 |
| EF Core: GetList(100) | 339.857 μs | 5.383 μs | 5.528 μs | 31.53 | 9 | 0.9766 | 69,688 B | 23.61 |

**结论**：
- **GetById**：Sqlx 比 Dapper.AOT 快 **8%**，比 EF Core LINQ 快 **4.3×**
- **GetList(10)**：Sqlx 与 Dapper.AOT 持平，比 EF Core 快 **2×**，内存少 **6%**
- **GetList(100)**：Sqlx 比 Dapper.AOT 快 **1.7%**，比 EF Core 快 **25%**，内存少 **14%**
- **Count**：Sqlx 与 Dapper.AOT 持平（3.6 μs），EF Core 慢 **9.4×**

---

## 其他操作

### 插入 (InsertBenchmark)

| 方法 | Mean | Ratio | Rank | Allocated | Alloc Ratio |
|------|-----:|------:|-----:|----------:|------------:|
| **Sqlx** | **85.17 μs** | 1.00 | 1 | 5.13 KB | 1.00 |
| Dapper.AOT | 108.24 μs | 1.27 | 2 | 6.36 KB | 1.24 |
| FreeSql | 159.65 μs | 1.88 | 3 | 15.28 KB | 2.98 |

### 更新 (UpdateBenchmark)

| 方法 | Mean | Ratio | Rank | Allocated | Alloc Ratio |
|------|-----:|------:|-----:|----------:|------------:|
| **Sqlx** | **19.04 μs** | 1.00 | 1 | 3.35 KB | 1.00 |
| Dapper.AOT | 21.98 μs | 1.15 | 2 | 5.20 KB | 1.55 |
| FreeSql | 75.56 μs | 3.97 | 3 | 14.67 KB | 4.38 |

### 删除 (DeleteBenchmark)

| 方法 | Mean | Ratio | Rank | Allocated | Alloc Ratio |
|------|-----:|------:|-----:|----------:|------------:|
| **Sqlx** | **43.45 μs** | 1.00 | 1 | 768 B | 1.00 |
| Dapper.AOT | 48.73 μs | 1.12 | 1 | 1,024 B | 1.33 |
| FreeSql | 246.48 μs | 5.67 | 2 | 8,000 B | 10.42 |

### 计数 (CountBenchmark)

| 方法 | Mean | Ratio | Rank | Allocated | Alloc Ratio |
|------|-----:|------:|-----:|----------:|------------:|
| **Sqlx** | **4.014 μs** | 1.00 | 1 | 936 B | 1.00 |
| Dapper.AOT | 4.042 μs | 1.01 | 1 | 976 B | 1.04 |
| FreeSql | 215.656 μs | 53.72 | 2 | 6,144 B | 6.56 |

### 批量插入 (BatchInsertBenchmark)

| 方法 | BatchSize | Mean | Ratio | Rank | Allocated | Alloc Ratio |
|------|----------:|-----:|------:|-----:|----------:|------------:|
| **Sqlx.Loop** | 10 | **179.1 μs** | 0.97 | 1 | 24.22 KB | 1.00 |
| Sqlx.DbBatch | 10 | 187.1 μs | 1.01 | 1 | 24.29 KB | 1.00 |
| Dapper.AOT | 10 | 208.6 μs | 1.13 | 2 | 29.80 KB | 1.23 |
| **Sqlx.DbBatch** | 100 | **1,309.8 μs** | 1.00 | 1 | 234.52 KB | 1.00 |
| Sqlx.Loop | 100 | 1,393.9 μs | 1.06 | 1 | 234.45 KB | 1.00 |
| Dapper.AOT | 100 | 1,568.0 μs | 1.20 | 2 | 285.73 KB | 1.22 |

### 复杂查询 (QueryWithFilterBenchmark)

| 方法 | Mean | Ratio | Rank | Allocated | Alloc Ratio |
|------|-----:|------:|-----:|----------:|------------:|
| **Sqlx** | **2.289 ms** | 1.00 | 1 | 363.23 KB | 1.00 |
| Dapper.AOT | 9.961 ms | 4.35 | 2 | 2,149.85 KB | 5.92 |

---

## 纯映射层性能 (PureMappingResultReaderBenchmark)

使用内存 `DbDataReader`，隔离映射层开销，1000 行。

| 方法 | Mean | Ratio | Rank |
|------|-----:|------:|-----:|
| **Generated cached ordinals** | **60.158 μs** | 1.00 | 1 |
| Dynamic array fast path | 81.224 μs | 1.35 | 2 |
| Dynamic span fallback | 106.770 μs | 1.78 | 3 |
| Generated uncached ordinals | 276.220 μs | 4.59 | 4 |

**结论**：cached ordinals 比 uncached 快 **4.6×**，DynamicResultReader array fast path 比 span fallback 快 **24%**。

---

## 综合结论

| 场景 | Sqlx vs Dapper.AOT | Sqlx vs EF Core | Sqlx vs FreeSql |
|------|:-----------------:|:---------------:|:---------------:|
| 单行查询 | **快 8.6%** | **快 4.3×** | **快 2.6×** |
| 列表查询 (10行) | 持平，内存少 6% | **快 2×** | 持平 |
| 列表查询 (100行) | 持平，内存少 16% | **快 25%** | 持平 |
| 列表查询 (1000行) | 内存少 16.4% | — | 慢 6% |
| Count | 持平 | **快 9.4×** | **快 53×** |
| 插入 | **快 21%** | — | **快 47%** |
| 更新 | **快 13%** | — | **快 4×** |
| 删除 | 持平 | — | **快 5.7×** |
| 批量插入 | **快 14-16%** | — | — |
| 复杂查询 | **快 4.4×** | — | — |

**测试日期**: 2026-04-22 · **Runtime**: .NET 10.0.5 · **DB**: SQLite in-memory
