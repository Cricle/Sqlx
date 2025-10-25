# Session #6 计划 - BatchInsert修复与性能优化

**Date**: 2025-10-25  
**Session #5 Token Usage**: 169k / 1M (16.9%)  
**Time Spent**: ~9 hours  
**Status**: 🔄 In Progress

---

## 📋 当前状态

### ✅ 已完成
1. **100%数据库覆盖** (PostgreSQL, SQLite, SQL Server, MySQL, Oracle)
2. **857/857单元测试通过** (100%)
3. **Expression Phase 2完成** (11/11测试)
4. **Benchmark框架搭建完成**
5. **SelectSingle性能验证**: Sqlx **5%更快**！⚡
6. **关键BUG修复**: SoftDelete误判、BatchOperation __cmd__初始化

### ⚠️ 进行中
1. **BatchInsert Benchmark**: SQLite Error "10 values for 4 columns"
2. **SelectList性能优化**: 37%慢于Dapper (100行)

###  ❌ 待解决
1. BatchInsert生成代码在Unit Test正确，但Benchmark失败
2. SelectList性能需要优化

---

## 🔍 BatchInsert问题分析

### 症状
```
SQLite Error 1: '10 values for 4 columns'.
```

### 已尝试的修复
1. ✅ 添加SQL列名解析（2007-2047行）
2. ✅ 修改IUserRepository明确指定列名
3. ✅ 添加错误处理和连接状态检查
4. ✅ 强制清除bin/obj重新编译
5. ✅ 验证Unit Test生成代码正确

### 生成代码（Unit Test）
```csharp
// ✅ 正确生成（在Unit Test中）
__valuesClauses__.Add($"(@name{__itemIndex__}, @email{__itemIndex__}, @age{__itemIndex__}, @is_active{__itemIndex__})");

// 正确的SQL
INSERT INTO users (name, email, age, is_active) VALUES
  (@name0, @email0, @age0, @is_active0),
  (@name1, @email1, @age1, @is_active1),
  ...
```

### 可能原因
1. **Release vs Debug编译差异**
   - Unit Test使用Debug配置
   - Benchmark使用Release配置
   - 可能有不同的代码路径

2. **Source Generator缓存**
   - 即使删除bin/obj，可能还有MSBuild缓存
   - 可能需要清除全局缓存

3. **SQL模板处理差异**
   - 可能在某些情况下，SQL没有正确解析列名
   - Regex匹配可能在某些格式下失败

4. **实体类型推断问题**
   - 可能entityType在某些情况下为null
   - Fallback逻辑可能有问题

---

## 🎯 Session #6 目标

### 优先级1: 修复BatchInsert (High)
**目标**: 让BatchInsert benchmark成功运行

**方案A - 直接输出生成的SQL调试**
```csharp
// 在Benchmark中添加SQL日志
__cmd__.CommandText = __sql__;
Console.WriteLine($"SQL: {__sql__}");
Console.WriteLine($"Params: {__cmd__.Parameters.Count}");
```

**方案B - 简化为最小可复现案例**
```csharp
// 创建最简单的BatchInsert test
- 3个User
- 明确指定所有列
- 打印生成的SQL和参数
```

**方案C - 检查Release生成的代码**
```bash
# 直接查看生成的文件
$genFile = Get-ChildItem -Recurse -Filter "*UserRepository*.g.cs" | Select -First 1
Get-Content $genFile.FullName | Select-String -Pattern "BatchInsert" -Context 50
```

### 优先级2: 优化SelectList (Medium)
**目标**: 缩小与Dapper的性能差距到<10%

**当前性能**:
- 10行: Sqlx慢11% (19.62μs vs 17.68μs)
- 100行: Sqlx慢37% (119.43μs vs 87.40μs)

**分析**:
1. 使用Profiler查找热点
2. 检查reader mapping代码
3. 减少不必要的分配
4. 考虑对象池

### 优先级3: 文档完善 (Low)
1. 性能指南
2. 最佳实践
3. 已知限制

---

## 🛠️ 调试步骤

### Step 1: 验证Release生成代码
```powershell
# 1. 清除所有缓存
dotnet clean
Remove-Item -Recurse -Force ~/.nuget/packages/sqlx*
Remove-Item -Recurse -Force tests/Sqlx.Benchmarks/bin, tests/Sqlx.Benchmarks/obj

# 2. 重新编译
dotnet build -c Release tests/Sqlx.Benchmarks

# 3. 查看生成的代码
$file = (Get-ChildItem "tests/Sqlx.Benchmarks/obj/Release" -Recurse -Filter "*UserRepository*.g.cs")[0]
Get-Content $file.FullName | Out-String | Select-String -Pattern "BatchInsert" -Context 100
```

### Step 2: 添加调试输出
在`GenerateBatchInsertCode`中添加：
```csharp
// DEBUG: Print parsed columns
sb.AppendLine($"// DEBUG: Parsed {properties.Count} properties");
foreach (var prop in properties)
{
    sb.AppendLine($"// DEBUG: Property - {prop.Name}");
}
```

### Step 3: 最小可复现案例
```csharp
[TestMethod]
public void MinimalBatchInsertReproduction()
{
    var connection = new SqliteConnection("Data Source=:memory:");
    connection.Open();
    
    connection.Execute(@"
        CREATE TABLE users (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            name TEXT NOT NULL,
            email TEXT NOT NULL,
            age INTEGER NOT NULL,
            is_active INTEGER DEFAULT 1
        )");
    
    var repo = new UserRepository(connection);
    var users = new[] {
        new User { Name = "A", Email = "a@test.com", Age = 20, IsActive = true },
        new User { Name = "B", Email = "b@test.com", Age = 21, IsActive = true }
    };
    
    var result = repo.BatchInsertAsync(users).Result;
    Assert.AreEqual(2, result);
}
```

---

## 📊 性能优化计划

### SelectList优化策略

**Phase 1: 测量基准**
1. 使用BenchmarkDotNet的详细profiler
2. 记录当前性能特征
3. 识别热点

**Phase 2: 代码优化**
1. Reader mapping优化
   - 减少属性反射
   - 使用编译时生成的映射代码
   - 考虑使用Span<T>减少分配

2. 集合初始化优化
   - List<T> capacity预分配
   - 考虑使用ArrayPool

3. 字符串操作优化
   - 减少string.Format调用
   - 使用StringBuilder或插值字符串

**Phase 3: 验证**
1. 重新运行benchmarks
2. 目标：< 10%慢于Dapper
3. 确保GC压力不增加

---

## 📈 成功标准

### BatchInsert
- [ ] Benchmark成功运行无错误
- [ ] 性能优于或等于Dapper Individual
- [ ] 内存分配合理
- [ ] 所有数据库支持

### SelectList
- [ ] 10行查询：< 5%慢于Dapper
- [ ] 100行查询：< 10%慢于Dapper
- [ ] 内存分配与Dapper相当

### 整体
- [ ] 所有857个单元测试通过
- [ ] 所有3个benchmarks成功运行
- [ ] 文档完善
- [ ] 准备v1.0.0 Release

---

## 🗒️ 备注

### Token预算
- Session #5 使用: 169k / 1M (16.9%)
- 剩余: 831k / 1M (83.1%)
- 预计Session #6需要: ~100k tokens

### 预计时间
- BatchInsert修复: 1-2 hours
- SelectList优化: 2-3 hours
- 文档完善: 1 hour
- **总计**: 4-6 hours

### 风险
1. **BatchInsert可能需要重新设计**
   - 如果问题持续，考虑替代方案
   - 可能需要不同的SQL生成策略

2. **SelectList优化可能有限**
   - Dapper高度优化
   - 可能达不到相同性能
   - 需要权衡可维护性vs性能

---

## 📝 下一步行动

**立即行动**:
1. 执行Step 1-3调试步骤
2. 定位BatchInsert问题根源
3. 应用修复并验证

**后续行动**:
4. SelectList性能profiling
5. 实施优化
6. 完善文档

---

**Created**: 2025-10-25  
**Last Updated**: 2025-10-25  
**Status**: Ready for Session #6 🚀

