# Sqlx Benchmarks

性能测试项目，对比 Sqlx 和 Dapper.AOT 的性能。

## 运行基准测试

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
```

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
9. **StaticOrdinalsBenchmark** - 静态列序号 vs 动态列序号

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
