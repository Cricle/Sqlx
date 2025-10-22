# Sqlx 快速参考

一页纸速查指南。

---

## 🚀 5分钟上手

```bash
# 1. 安装
dotnet add package Sqlx
dotnet add package Sqlx.Generator

# 2. 定义实体
public class User { public int Id { get; set; } public string Name { get; set; } }

# 3. 创建Repository
[RepositoryFor(typeof(User))]
public partial class UserRepository { }

# 4. 添加方法
[Sqlx("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
public partial Task<User?> GetByIdAsync(int id);

# 5. 使用
var user = await repository.GetByIdAsync(1);
```

---

## 📝 SQL模板速查

| 占位符 | 说明 | 示例 |
|--------|------|------|
| `{{table}}` | 表名 | `users` |
| `{{columns}}` | 所有列 | `id, name, email` |
| `{{columns --exclude Id}}` | 排除列 | `name, email` |
| `{{values}}` | VALUES子句 | `@Name, @Email` |
| `{{set}}` | SET子句 | `name=@Name, email=@Email` |
| `{{where}}` | WHERE条件 | `WHERE id = @Id` |
| `{{orderby name}}` | ORDER BY | `ORDER BY name ASC` |
| `{{orderby name --desc}}` | 降序 | `ORDER BY name DESC` |
| `{{limit}}` | LIMIT | `LIMIT @Limit` |

---

## ⚡ 拦截器快速使用

```csharp
// 注册拦截器
Sqlx.Interceptors.SqlxInterceptors.Add(new Sqlx.Interceptors.ActivityInterceptor());

// 自定义拦截器
public class MyInterceptor : ISqlxInterceptor
{
    public void OnExecuting(ref SqlxExecutionContext ctx) { }
    public void OnExecuted(ref SqlxExecutionContext ctx) { }
    public void OnFailed(ref SqlxExecutionContext ctx) { }
}
```

---

## 📊 性能测试

```bash
cd tests/Sqlx.Benchmarks
dotnet run -c Release
```

---

## 📚 核心文档

| 文档 | 用途 |
|------|------|
| [README.md](README.md) | 项目介绍 |
| [DOCUMENTATION_INDEX.md](DOCUMENTATION_INDEX.md) | 文档导航 |
| [DESIGN_PRINCIPLES.md](DESIGN_PRINCIPLES.md) | 设计原则 |
| [samples/TodoWebApi/INTERCEPTOR_USAGE.md](samples/TodoWebApi/INTERCEPTOR_USAGE.md) | 拦截器使用 |
| [BENCHMARKS_SUMMARY.md](BENCHMARKS_SUMMARY.md) | 性能报告 |

---

## 🎯 三大原则

1. **Fail Fast** - 异常不吞噬
2. **无运行时缓存** - 编译时计算
3. **源生成优先** - 零反射

---

## ⚡ 性能目标

- Sqlx ≈ 手写 ADO.NET (<5%差异)
- Sqlx > Dapper (快10-30%)
- 拦截器: 0B GC, <5%开销

---

**完整文档**: [DOCUMENTATION_INDEX.md](DOCUMENTATION_INDEX.md)

