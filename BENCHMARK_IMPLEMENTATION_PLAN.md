# Sqlx 性能 Benchmark 实施计划

**日期**: 2025-10-25  
**优先级**: ⭐⭐⭐ 高  
**目标**: 验证Sqlx性能达到或超越Dapper  
**预计时间**: 3-4小时  

---

## 🎯 目标

1. **性能验证**: 确保Sqlx性能 ≥ Dapper
2. **GC优化验证**: 确认零或接近零的GC压力
3. **AOT验证**: 确认AOT编译无问题
4. **性能报告**: 生成详细的性能对比报告

---

## 📋 Benchmark场景

### 场景1: 单条查询 (SelectSingle)
```csharp
SELECT * FROM users WHERE id = @id
```
**对比框架**: Sqlx, Dapper, EF Core

### 场景2: 列表查询 (SelectList)
```csharp
SELECT * FROM users WHERE age > @minAge LIMIT 100
```
**对比框架**: Sqlx, Dapper, EF Core

### 场景3: INSERT操作
```csharp
INSERT INTO users (name, email, age) VALUES (@name, @email, @age)
```
**对比框架**: Sqlx, Dapper, EF Core

### 场景4: UPDATE操作
```csharp
UPDATE users SET name = @name WHERE id = @id
```
**对比框架**: Sqlx, Dapper, EF Core

### 场景5: DELETE操作
```csharp
DELETE FROM users WHERE id = @id
```
**对比框架**: Sqlx, Dapper, EF Core

### 场景6: 批量INSERT (Batch Insert)
```csharp
INSERT INTO users (name, email, age) VALUES 
  (@name0, @email0, @age0),
  (@name1, @email1, @age1),
  ... (100 rows)
```
**对比框架**: Sqlx, Dapper (逐条), EF Core

### 场景7: 复杂查询 (JOIN)
```csharp
SELECT u.*, o.* FROM users u 
INNER JOIN orders o ON u.id = o.user_id 
WHERE u.id = @id
```
**对比框架**: Sqlx, Dapper, EF Core

### 场景8: 表达式查询 (Expression WHERE)
```csharp
users.Where(u => u.Age > 18 && u.IsActive)
```
**对比框架**: Sqlx, EF Core

---

## 📊 性能指标

### 主要指标
1. **执行时间 (Execution Time)**
   - 平均时间 (Mean)
   - P50, P95, P99
   - 最小/最大

2. **内存分配 (Memory Allocation)**
   - 每操作分配字节数
   - GC Generation 0/1/2 次数

3. **吞吐量 (Throughput)**
   - 每秒操作数 (ops/sec)

### 次要指标
4. **启动时间 (Startup Time)**
5. **编译时间 (Code Generation Time)**
6. **AOT兼容性**

---

## 🔧 实施步骤

### 步骤1: 创建Benchmark项目 (30分钟)

**文件结构**:
```
tests/Sqlx.Benchmarks/
├── Sqlx.Benchmarks.csproj
├── Program.cs
├── Benchmarks/
│   ├── SelectSingleBenchmark.cs
│   ├── SelectListBenchmark.cs
│   ├── InsertBenchmark.cs
│   ├── UpdateBenchmark.cs
│   ├── DeleteBenchmark.cs
│   ├── BatchInsertBenchmark.cs
│   ├── JoinQueryBenchmark.cs
│   └── ExpressionQueryBenchmark.cs
├── Models/
│   ├── User.cs
│   └── Order.cs
└── Database/
    └── DatabaseSetup.cs
```

**依赖项**:
```xml
<PackageReference Include="BenchmarkDotNet" Version="0.13.12" />
<PackageReference Include="Dapper" Version="2.1.28" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="9.0.0" />
<PackageReference Include="System.Data.SQLite" Version="1.0.118" />
```

### 步骤2: 实现基础Benchmark (60分钟)

#### 2.1 SelectSingleBenchmark
```csharp
[MemoryDiagnoser]
[RankColumn]
public class SelectSingleBenchmark
{
    private IDbConnection _connection = null!;
    private IUserRepository _sqlxRepo = null!;
    private DbContext _efContext = null!;
    
    [GlobalSetup]
    public void Setup()
    {
        _connection = new SQLiteConnection("Data Source=:memory:");
        _connection.Open();
        SeedDatabase(_connection);
        
        _sqlxRepo = new UserRepository(_connection);
        _efContext = new AppDbContext();
    }
    
    [Benchmark(Baseline = true)]
    public User? Dapper_SelectSingle()
    {
        return _connection.QueryFirstOrDefault<User>(
            "SELECT * FROM users WHERE id = @id", 
            new { id = 1 });
    }
    
    [Benchmark]
    public User? Sqlx_SelectSingle()
    {
        return _sqlxRepo.GetByIdAsync(1).GetAwaiter().GetResult();
    }
    
    [Benchmark]
    public User? EFCore_SelectSingle()
    {
        return _efContext.Users.FirstOrDefault(u => u.Id == 1);
    }
}
```

#### 2.2 BatchInsertBenchmark
```csharp
[MemoryDiagnoser]
public class BatchInsertBenchmark
{
    private List<User> _users = null!;
    
    [Params(10, 100, 1000)]
    public int RowCount;
    
    [GlobalSetup]
    public void Setup()
    {
        _users = Enumerable.Range(1, RowCount)
            .Select(i => new User { Name = $"User{i}", Email = $"user{i}@test.com", Age = 20 + i % 50 })
            .ToList();
    }
    
    [Benchmark(Baseline = true)]
    public int Dapper_BatchInsert()
    {
        using var tx = _connection.BeginTransaction();
        var affected = 0;
        foreach (var user in _users)
        {
            affected += _connection.Execute(
                "INSERT INTO users (name, email, age) VALUES (@Name, @Email, @Age)", 
                user, 
                tx);
        }
        tx.Commit();
        return affected;
    }
    
    [Benchmark]
    public int Sqlx_BatchInsert()
    {
        return _sqlxRepo.BatchInsertAsync(_users).GetAwaiter().GetResult();
    }
}
```

### 步骤3: 数据库设置 (15分钟)

```csharp
public static class DatabaseSetup
{
    public static void SeedDatabase(IDbConnection connection)
    {
        // Create tables
        connection.Execute(@"
            CREATE TABLE IF NOT EXISTS users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                email TEXT NOT NULL,
                age INTEGER NOT NULL,
                is_active INTEGER DEFAULT 1,
                created_at TEXT DEFAULT CURRENT_TIMESTAMP
            )");
        
        connection.Execute(@"
            CREATE TABLE IF NOT EXISTS orders (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                user_id INTEGER NOT NULL,
                amount REAL NOT NULL,
                created_at TEXT DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (user_id) REFERENCES users(id)
            )");
        
        // Seed data
        var users = Enumerable.Range(1, 1000)
            .Select(i => new User { 
                Name = $"User{i}", 
                Email = $"user{i}@test.com", 
                Age = 20 + i % 50,
                IsActive = i % 10 != 0
            });
        
        foreach (var user in users)
        {
            connection.Execute(
                "INSERT INTO users (name, email, age, is_active) VALUES (@Name, @Email, @Age, @IsActive)", 
                user);
        }
    }
}
```

### 步骤4: 运行Benchmark (30分钟)

```bash
cd tests/Sqlx.Benchmarks
dotnet run -c Release --framework net9.0
```

**配置选项**:
```csharp
[Config(typeof(Config))]
public class SelectSingleBenchmark
{
    private class Config : ManualConfig
    {
        public Config()
        {
            AddDiagnoser(MemoryDiagnoser.Default);
            AddColumn(RankColumn.Arabic);
            AddJob(Job.Default
                .WithGcServer(false)
                .WithGcConcurrent(false)
                .WithGcForce(true));
        }
    }
}
```

### 步骤5: 分析结果 (45分钟)

**关注点**:
1. **Sqlx vs Dapper性能差异**
   - 目标：≤ 10%性能差异
   - 理想：≥ Dapper性能

2. **GC压力**
   - 目标：Gen0 收集 ≤ Dapper
   - 理想：零分配（小查询）

3. **批量操作优势**
   - Sqlx批量INSERT应显著快于Dapper逐条

4. **内存分配**
   - 单条查询：< 500 bytes
   - 列表查询(100条)：< 50 KB

### 步骤6: 性能优化（如需）(60分钟)

**潜在优化点**:
1. **字符串构建优化**
   - 使用`StringBuilder` pooling
   - 避免重复分配

2. **参数绑定优化**
   - 缓存参数对象
   - 减少装箱

3. **Reader优化**
   - 使用`Span<T>`
   - 减少虚拟调用

4. **SQL缓存**
   - 缓存处理后的SQL模板
   - 避免重复解析

---

## 📈 预期结果

### 场景1: SelectSingle
```
|     Method |      Mean |  Gen0 | Allocated |
|----------- |----------:|------:|----------:|
| Dapper     |  12.5 μs  | 0.05  |     392 B |
| Sqlx       |  12.8 μs  | 0.05  |     400 B | ✅ 目标达成
| EFCore     |  45.2 μs  | 0.15  |   1,240 B |
```

### 场景6: Batch Insert (100 rows)
```
|     Method |      Mean |  Gen0 | Allocated |
|----------- |----------:|------:|----------:|
| Dapper     | 125.0 ms  | 15.0  |  125 KB   |
| Sqlx       |  15.2 ms  |  2.5  |   18 KB   | 🚀 8x faster!
| EFCore     | 180.5 ms  | 25.0  |  210 KB   |
```

---

## ✅ 成功标准

1. **性能**: Sqlx ≥ 90% Dapper性能（单条操作）
2. **批量操作**: Sqlx显著快于Dapper（≥ 3x）
3. **内存**: Sqlx内存分配 ≤ 120% Dapper
4. **GC**: Gen0收集 ≤ Dapper
5. **AOT**: 所有Benchmark可AOT编译

---

## 📝 输出文档

1. **BENCHMARK_RESULTS.md**
   - 完整性能数据
   - 图表和对比
   - 分析和结论

2. **PERFORMANCE_TUNING_GUIDE.md**
   - 性能最佳实践
   - 常见陷阱
   - 优化技巧

3. **README.md 更新**
   - 添加性能部分
   - Benchmark结果摘要

---

## ⚠️ 注意事项

1. **数据库选择**: 使用SQLite进行Benchmark（简单、一致）
2. **预热**: 确保JIT预热后再测量
3. **环境一致性**: 关闭后台任务，固定CPU频率
4. **多次运行**: 每个Benchmark运行多次取平均值
5. **版本固定**: 锁定Dapper和EF Core版本

---

**创建时间**: 2025-10-25  
**状态**: 准备开始  
**下一步**: 创建Benchmark项目结构

