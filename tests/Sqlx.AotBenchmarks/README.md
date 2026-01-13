# Sqlx vs Dapper.AOT Benchmark

对比 Sqlx 和 Dapper.AOT 在 Native AOT 编译后的性能表现。

## 运行测试

```bash
# JIT 模式运行
dotnet run -c Release

# 发布 AOT 版本
dotnet publish -c Release -r win-x64

# 运行 AOT 版本
.\bin\Release\net9.0\win-x64\publish\Sqlx.AotBenchmarks.exe
```

## 测试结果

### JIT 模式 (100,000 次迭代)

| Operation | Sqlx | Dapper.AOT | Sqlx Advantage |
|-----------|------|------------|----------------|
| GetById   | 11.20 us | 12.28 us | **9.6% faster** |
| Count     | 8.02 us | 8.06 us | **0.6% faster** |
| Insert    | 18.36 us | 14.09 us | -23.3% slower |

### AOT 模式 (100,000 次迭代)

| Operation | Sqlx | Dapper.AOT | Sqlx Advantage |
|-----------|------|------------|----------------|
| GetById   | 8.30 us | 10.75 us | **29.6% faster** |
| Count     | 7.86 us | 7.95 us | **1.2% faster** |
| Insert    | 13.18 us | 11.66 us | -11.5% slower |

### AOT vs JIT 性能提升

| Operation | Sqlx JIT | Sqlx AOT | 提升 |
|-----------|----------|----------|------|
| GetById   | 11.20 us | 8.30 us | **26% faster** |
| Count     | 8.02 us | 7.86 us | **2% faster** |
| Insert    | 18.36 us | 13.18 us | **28% faster** |

### 测试环境

- .NET 9.0
- Windows 10
- AMD Ryzen 7 5800H
- SQLite 内存数据库
- 10,000 条测试数据

## 结论

1. **查询性能 Sqlx 领先** - GetById 操作 Sqlx 比 Dapper.AOT 快 29.6%
2. **Count 性能相当** - 两者差距仅 1.2%
3. **Insert 性能 Dapper.AOT 领先** - Dapper.AOT 的参数绑定更高效
4. **AOT 编译显著提升性能** - Sqlx 在 AOT 模式下性能提升 26-28%

## 为什么 Sqlx 查询更快？

1. **预编译 SQL 模板** - SQL 在编译时处理，运行时零解析
2. **直接 ADO.NET 调用** - 无中间层，直接操作 DbCommand
3. **缓存列序号** - 避免重复查找列索引
4. **最小内存分配** - 避免不必要的对象创建

## 为什么 Insert 较慢？

Sqlx 的 Insert 使用手动参数绑定，而 Dapper.AOT 使用源生成器优化的参数绑定。
未来可以通过源生成器优化 Sqlx 的参数绑定性能。
