# Sqlx AOT Benchmark

测试 Sqlx 在 Native AOT 编译后的性能表现。

## 运行测试

```bash
# 普通模式运行
dotnet run -c Release

# 发布 AOT 版本
dotnet publish -c Release -r win-x64

# 运行 AOT 版本
.\bin\Release\net9.0\win-x64\publish\Sqlx.AotBenchmarks.exe
```

## 测试结果

### 性能对比 (100,000 次迭代)

| 操作 | JIT 模式 | AOT 模式 | 提升 |
|------|----------|----------|------|
| GetById | 11.43 us | 8.32 us | **27% 更快** |
| Count | 8.53 us | 8.16 us | **4% 更快** |
| GetPaged | 81.55 us | 73.31 us | **10% 更快** |
| Insert | 18.00 us | 15.20 us | **16% 更快** |

### AOT 可执行文件大小

- **Sqlx.AotBenchmarks.exe**: 2.94 MB (包含 SQLite 和所有依赖)

### 测试环境

- .NET 9.0
- Windows 10
- AMD Ryzen 7 5800H
- SQLite 内存数据库
- 10,000 条测试数据

## 结论

1. **AOT 编译显著提升性能** - 所有操作都有 4-27% 的性能提升
2. **GetById 提升最明显** - 从 11.43us 降到 8.32us，提升 27%
3. **可执行文件体积小** - 仅 2.94 MB，包含所有依赖
4. **完全兼容 AOT** - Sqlx 的设计完全支持 Native AOT，无需任何修改

## 为什么 AOT 更快？

1. **无 JIT 编译开销** - 代码已预编译为原生机器码
2. **更好的内联优化** - AOT 编译器可以进行更激进的优化
3. **更小的内存占用** - 无需加载 JIT 编译器和元数据
4. **更快的启动时间** - 无需运行时编译
