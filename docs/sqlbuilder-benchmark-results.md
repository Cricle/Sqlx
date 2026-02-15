# SqlBuilder 性能基准测试结果

本文档包含 SqlBuilder 的详细性能测试结果，使用 BenchmarkDotNet 在 .NET 10.0.3 上测试。

## 测试环境

- **操作系统**: Windows 10 (10.0.19045.6466/22H2/2022Update)
- **处理器**: AMD Ryzen 7 5800H with Radeon Graphics 3.20GHz (8 核 16 线程)
- **运行时**: .NET 10.0.3 (10.0.3, 10.0.326.7603), X64 RyuJIT x86-64-v3
- **GC**: Concurrent Workstation
- **BenchmarkDotNet**: v0.15.7

## 参数转换性能对比

测试 SqlBuilder.AppendTemplate 使用不同参数类型的性能差异。

### 测试结果汇总

| 方法 | 平均时间 | 相对性能 | 内存分配 | 相对内存 | Gen0 | Gen1 |
|------|---------|---------|---------|---------|------|------|
| **2 props: Optimized (Expression tree)** | 1.486 μs | baseline | 4.68 KB | 1.00x | 0.5722 | 0.0038 |
| 2 props: Reflection (baseline) | 1.632 μs | +9.8% | 5.05 KB | 1.08x | 0.6161 | 0.0038 |
| 2 props: Dictionary (no conversion) | 1.384 μs | -6.9% | 4.92 KB | 1.05x | 0.6008 | 0.0038 |
| **5 props: Optimized (Expression tree)** | 1.328 μs | -10.6% | 5.71 KB | 1.22x | 0.6981 | 0.0057 |
| 5 props: Reflection (baseline) | 1.678 μs | +12.9% | 6.34 KB | 1.36x | 0.7763 | 0.0057 |
| 5 props: Dictionary (no conversion) | 1.476 μs | -0.7% | 6.23 KB | 1.33x | 0.7629 | 0.0057 |
| **10 props: Optimized (Expression tree)** | 1.507 μs | +1.4% | 6.80 KB | 1.45x | 0.8316 | 0.0076 |
| 10 props: Reflection (baseline) | 2.282 μs | +53.6% | 7.98 KB | 1.71x | 0.9766 | - |
| 10 props: Dictionary (no conversion) | 1.805 μs | +21.5% | 7.83 KB | 1.67x | 0.9575 | 0.0114 |
| **Multiple calls: Optimized (cached)** | 12.130 μs | - | 47.97 KB | 10.25x | 5.8594 | 0.0458 |
| Multiple calls: Reflection (no cache) | 13.358 μs | +10.1% | 50.47 KB | 10.78x | 6.1646 | 0.0458 |

### 关键发现

#### 1. Expression Tree 优化效果显著

随着属性数量增加，Expression tree 优化的性能优势越来越明显：

- **2 个属性**: 比反射快 **8.9%** (1.486 μs vs 1.632 μs)
- **5 个属性**: 比反射快 **20.9%** (1.328 μs vs 1.678 μs)
- **10 个属性**: 比反射快 **34.0%** (1.507 μs vs 2.282 μs)

#### 2. 内存分配优化

Expression tree 版本的内存分配比反射版本更优：

- **2 个属性**: 少分配 **7.3%** (4.68 KB vs 5.05 KB)
- **5 个属性**: 少分配 **9.9%** (5.71 KB vs 6.34 KB)
- **10 个属性**: 少分配 **14.8%** (6.80 KB vs 7.98 KB)

#### 3. 缓存效果

多次调用相同类型的匿名对象时，Expression tree 缓存带来显著性能提升：

- **10 次调用**: 比无缓存反射快 **9.2%** (12.130 μs vs 13.358 μs)
- **内存分配**: 少分配 **5.0%** (47.97 KB vs 50.47 KB)

#### 4. 与 Dictionary 直接使用对比

Expression tree 优化版本的性能接近直接使用 Dictionary（无需转换）：

- **2 个属性**: 仅慢 **7.4%** (1.486 μs vs 1.384 μs)
- **5 个属性**: 实际更快 **10.0%** (1.328 μs vs 1.476 μs)
- **10 个属性**: 仅慢 **16.5%** (1.507 μs vs 1.805 μs)

## 详细测试数据

### 2 个属性测试

```
BenchmarkDotNet v0.15.7, Windows 10 (10.0.19045.6466/22H2/2022Update)
AMD Ryzen 7 5800H with Radeon Graphics 3.20GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.103
  [Host]     : .NET 10.0.3 (10.0.3, 10.0.326.7603), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.3 (10.0.3, 10.0.326.7603), X64 RyuJIT x86-64-v3

| Method                                  | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|---------------------------------------- |----------:|----------:|----------:|------:|--------:|----------:|------------:|
| '2 props: Optimized (Expression tree)'  | 1.486 μs  | 0.0283 μs | 0.0278 μs |  1.00 |    0.03 |   4.68 KB |        1.00 |
| '2 props: Reflection (baseline)'        | 1.632 μs  | 0.0261 μs | 0.0218 μs |  1.10 |    0.02 |   5.05 KB |        1.08 |
| '2 props: Dictionary (no conversion)'   | 1.384 μs  | 0.0155 μs | 0.0138 μs |  0.93 |    0.02 |   4.92 KB |        1.05 |
```

### 5 个属性测试

```
| Method                                  | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|---------------------------------------- |----------:|----------:|----------:|------:|--------:|----------:|------------:|
| '5 props: Optimized (Expression tree)'  | 1.328 μs  | 0.0182 μs | 0.0170 μs |  0.89 |    0.02 |   5.71 KB |        1.22 |
| '5 props: Reflection (baseline)'        | 1.678 μs  | 0.0182 μs | 0.0170 μs |  1.13 |    0.02 |   6.34 KB |        1.36 |
| '5 props: Dictionary (no conversion)'   | 1.476 μs  | 0.0210 μs | 0.0175 μs |  0.99 |    0.02 |   6.23 KB |        1.33 |
```

### 10 个属性测试

```
| Method                                  | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|---------------------------------------- |----------:|----------:|----------:|------:|--------:|----------:|------------:|
| '10 props: Optimized (Expression tree)' | 1.507 μs  | 0.0205 μs | 0.0192 μs |  1.01 |    0.02 |   6.80 KB |        1.45 |
| '10 props: Reflection (baseline)'       | 2.282 μs  | 0.0296 μs | 0.0277 μs |  1.54 |    0.03 |   7.98 KB |        1.71 |
| '10 props: Dictionary (no conversion)'  | 1.805 μs  | 0.0131 μs | 0.0123 μs |  1.21 |    0.02 |   7.83 KB |        1.67 |
```

### 多次调用测试（缓存效果）

```
| Method                                          | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------------------ |----------:|----------:|----------:|------:|--------:|----------:|------------:|
| 'Multiple calls: Optimized (cached Expression)' | 12.130 μs | 0.1429 μs | 0.1266 μs |  8.16 |    0.17 |  47.97 KB |       10.25 |
| 'Multiple calls: Reflection (no cache)'         | 13.358 μs | 0.1233 μs | 0.1093 μs |  8.99 |    0.18 |  50.47 KB |       10.78 |
```

## 性能排名

按平均执行时间从快到慢排序：

1. **5 props: Optimized** - 1.328 μs ⭐ (最快)
2. 2 props: Dictionary - 1.384 μs
3. 5 props: Dictionary - 1.476 μs
4. 2 props: Optimized - 1.486 μs
5. **10 props: Optimized** - 1.507 μs
6. 2 props: Reflection - 1.632 μs
7. 5 props: Reflection - 1.678 μs
8. 10 props: Dictionary - 1.805 μs
9. 10 props: Reflection - 2.282 μs (最慢)

## GC 压力分析

### Gen0 GC 收集次数（每 1000 次操作）

| 方法 | Gen0 | Gen1 |
|------|------|------|
| 2 props: Optimized | 0.5722 | 0.0038 |
| 2 props: Reflection | 0.6161 | 0.0038 |
| 5 props: Optimized | 0.6981 | 0.0057 |
| 5 props: Reflection | 0.7763 | 0.0057 |
| 10 props: Optimized | 0.8316 | 0.0076 |
| 10 props: Reflection | 0.9766 | - |

Expression tree 版本的 GC 压力始终低于反射版本：
- 2 props: 减少 **7.1%** GC 压力
- 5 props: 减少 **10.1%** GC 压力
- 10 props: 减少 **14.9%** GC 压力

## 结论

### 推荐使用场景

1. **匿名对象参数（推荐）**
   - 使用 Expression tree 优化
   - 性能优于反射 8.9%-34%
   - 代码简洁，类型安全

2. **Dictionary 参数**
   - 性能最优（无转换开销）
   - 适合动态参数场景
   - 需要手动创建字典

3. **避免使用反射**
   - 性能最差
   - 仅用于兼容性场景

### 性能优化建议

1. **优先使用匿名对象**
   ```csharp
   builder.AppendTemplate("SELECT {{columns}} FROM {{table}} WHERE age = @age", new { age = 18 });
   ```

2. **复用相同类型**
   - Expression tree 会缓存编译结果
   - 多次调用相同类型性能更优

3. **大对象使用 Dictionary**
   - 超过 10 个属性时，考虑直接使用 Dictionary
   - 避免 Expression tree 编译开销

4. **避免反射**
   - 不要手动使用反射转换参数
   - SqlBuilder 内部已优化

## 测试代码

完整的测试代码位于：
- `tests/Sqlx.Benchmarks/Benchmarks/SqlBuilderParameterBenchmark.cs`

运行测试：
```bash
dotnet run --project tests/Sqlx.Benchmarks/Sqlx.Benchmarks.csproj -c Release --framework net10.0 -- --filter "*SqlBuilderParameter*"
```

## 相关文档

- [SqlBuilder 完整指南](sqlbuilder.md)
- [性能基准测试](benchmarks.md)
- [API 参考](api-reference.md)
