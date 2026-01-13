# Sqlx Benchmarks

性能测试项目，对比 Sqlx、Dapper 和原生 ADO.NET 的性能。

## 运行基准测试

```bash
# 运行所有基准测试
dotnet run -c Release

# 运行特定基准测试
dotnet run -c Release -- --filter "*SelectSingle*"
dotnet run -c Release -- --filter "*SelectList*"
dotnet run -c Release -- --filter "*Insert*"
dotnet run -c Release -- --filter "*Update*"
dotnet run -c Release -- --filter "*Count*"
dotnet run -c Release -- --filter "*Pagination*"
```

## 基准测试结果

### 单条查询 (SelectSingleBenchmark)

| Method | Mean | Allocated | Ratio |
|--------|------|-----------|-------|
| **Sqlx GetById** | **7.677 us** | **1.49 KB** | **1.00** |
| ADO.NET Manual | 7.880 us | 1.4 KB | 1.03 |
| Dapper QueryFirstOrDefault | 11.229 us | 3.15 KB | 1.46 |

**结论**: Sqlx 比 Dapper 快 ~46%，内存占用减少 ~53%

### COUNT 查询 (CountBenchmark)

| Method | Mean | Allocated | Ratio |
|--------|------|-----------|-------|
| ADO.NET Manual | 3.488 us | 784 B | 0.98 |
| **Sqlx Count** | **3.544 us** | **856 B** | **1.00** |
| Dapper ExecuteScalar | 3.641 us | 856 B | 1.03 |

**结论**: Sqlx 与 ADO.NET 性能相当，比 Dapper 快 ~3%

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
5. **QueryWithFilterBenchmark** - 带 WHERE 条件的查询
6. **CountBenchmark** - COUNT 聚合查询
7. **PaginationBenchmark** - 分页查询

## 为什么 Sqlx 更快？

1. **编译时生成** - SQL 模板在编译时处理，运行时零解析开销
2. **静态 SqlTemplate** - 预编译的 SQL 模板，避免重复字符串处理
3. **直接 ADO.NET** - 生成的代码直接调用 ADO.NET，无中间层
4. **最小内存分配** - 避免不必要的对象创建和装箱
5. **缓存列序号** - ResultReader 缓存列序号，避免重复查找
6. **高效 Render** - 动态占位符渲染使用预计算的段落拼接
