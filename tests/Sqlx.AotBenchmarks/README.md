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
| GetById   | 5.87 us | 14.60 us | **148.8% faster** |
| Count     | 5.75 us | 7.88 us | **37.0% faster** |
| Insert    | 9.49 us | 13.89 us | **46.4% faster** |

### AOT 模式 (100,000 次迭代)

| Operation | Sqlx | Dapper.AOT | Sqlx Advantage |
|-----------|------|------------|----------------|
| GetById   | 2.47 us | 12.41 us | **402.6% faster** |
| Count     | 5.48 us | 7.71 us | **40.5% faster** |
| Insert    | 5.44 us | 11.19 us | **105.7% faster** |

### AOT vs JIT 性能提升 (Sqlx)

| Operation | JIT | AOT | 提升 |
|-----------|-----|-----|------|
| GetById   | 5.87 us | 2.47 us | **58% faster** |
| Count     | 5.75 us | 5.48 us | **5% faster** |
| Insert    | 9.49 us | 5.44 us | **43% faster** |

### 测试环境

- .NET 9.0
- Windows 10
- AMD Ryzen 7 5800H
- SQLite 内存数据库
- 10,000 条测试数据

## 结论

1. **Sqlx 全面领先** - 所有操作 Sqlx 都比 Dapper.AOT 快
2. **GetById 性能差距巨大** - AOT 模式下 Sqlx 比 Dapper.AOT 快 **5 倍**
3. **Insert 性能优秀** - 优化后 Sqlx Insert 比 Dapper.AOT 快 **2 倍**
4. **AOT 编译效果显著** - Sqlx 在 AOT 模式下性能提升 43-58%

## 为什么 Sqlx 更快？

1. **预创建命令和参数** - 避免每次调用创建新对象
2. **直接 ADO.NET 调用** - 无中间层，直接操作 DbCommand
3. **静态 SQL 模板** - 编译时处理，运行时零解析
4. **最小内存分配** - 复用命令和参数对象
5. **缓存列序号** - 避免重复查找列索引

## 优化技巧

```csharp
// 预创建命令和参数
private readonly SqliteCommand _cmd;
private readonly SqliteParameter _param;

public Repository(SqliteConnection connection)
{
    _cmd = connection.CreateCommand();
    _cmd.CommandText = "SELECT * FROM users WHERE id = @id";
    _param = _cmd.CreateParameter();
    _param.ParameterName = "@id";
    _cmd.Parameters.Add(_param);
}

public async Task<User?> GetByIdAsync(long id)
{
    _param.Value = id;  // 只更新值，不创建新对象
    using var reader = await _cmd.ExecuteReaderAsync();
    // ...
}
```
