# Sqlx 完整文档

欢迎来到 Sqlx 的完整文档！这里包含了所有你需要的信息。

---

## 📚 文档导航

### 🚀 入门指南

| 文档 | 说明 | 适合人群 |
|------|------|----------|
| [快速开始](QUICK_START_GUIDE.md) | 5 分钟上手教程 | 🔰 新手必读 |
| [快速参考](QUICK_REFERENCE.md) | 常用功能速查表 | ⚡ 快速查询 |

### 📖 核心功能

| 文档 | 说明 | 详细程度 |
|------|------|---------|
| [占位符完整列表](PLACEHOLDERS.md) | 80+ 占位符详解 | ⭐⭐⭐⭐⭐ 必读 |
| [API 参考](API_REFERENCE.md) | 所有 API 文档 | ⭐⭐⭐⭐ |
| [最佳实践](BEST_PRACTICES.md) | 推荐的使用方式 | ⭐⭐⭐⭐ |

### 📊 其他资源

| 文档 | 说明 |
|------|------|
| [更新日志](CHANGELOG.md) | 版本更新记录 |
| [性能测试](../tests/Sqlx.Benchmarks/) | Benchmark 数据 |
| [示例项目](../samples/TodoWebApi/) | 完整的 WebAPI 示例 |
| [GitHub Pages](https://cricle.github.io/Sqlx/) | 在线文档网站 |

---

## 🎯 快速查找

### 我想了解...

#### 基础使用

<details>
<summary>📦 如何安装 Sqlx？</summary>

```bash
dotnet add package Sqlx
dotnet add package Sqlx.Generator
```

详见：[快速开始](QUICK_START_GUIDE.md#安装)
</details>

<details>
<summary>🔍 如何定义查询方法？</summary>

```csharp
public interface IUserRepository
{
    [Sqlx("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    Task<User?> GetByIdAsync(int id);
}
```

详见：[快速开始](QUICK_START_GUIDE.md#定义接口)
</details>

<details>
<summary>✏️ 如何插入数据？</summary>

```csharp
[Sqlx("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values}})")]
Task<int> CreateAsync(User user);
```

详见：[快速参考](QUICK_REFERENCE.md#插入操作)
</details>

#### 高级功能

<details>
<summary>📄 如何实现分页？</summary>

```csharp
[Sqlx("SELECT {{columns}} FROM {{table}} {{page}}")]
Task<List<User>> GetPagedAsync(int page, int pageSize);
```

详见：[占位符列表](PLACEHOLDERS.md#page-分页)
</details>

<details>
<summary>🔐 如何使用动态表名？</summary>

```csharp
[Sqlx("SELECT {{columns}} FROM {{@tableName}} WHERE id = @id")]
Task<User?> GetFromTableAsync([DynamicSql] string tableName, int id);
```

⚠️ **必须**使用白名单验证！详见：[占位符列表](PLACEHOLDERS.md#动态占位符)
</details>

<details>
<summary>🚀 如何批量插入数据？</summary>

```csharp
[Sqlx("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES {{batch_values}}")]
[BatchOperation(BatchSize = 100)]
Task<int> BatchInsertAsync(List<User> users);
```

详见：[占位符列表](PLACEHOLDERS.md#batch_values-批量插入)
</details>

<details>
<summary>🌐 如何切换数据库？</summary>

```csharp
// 只需修改 Dialect 参数
[SqlDefine(Dialect = SqlDialect.MySql)]      // MySQL
[SqlDefine(Dialect = SqlDialect.PostgreSql)] // PostgreSQL
```

详见：[API 参考](API_REFERENCE.md#sqldefineattribute)
</details>

#### 性能优化

<details>
<summary>⚡ Sqlx 性能如何？</summary>

**Sqlx 比 Dapper 快 10-285%**（取决于操作类型）：

- 单行查询：快 **10%**
- 批量插入：快 **285%**
- 批量更新：快 **274%**
- 内存分配：减少 **45%**

详见：[性能测试](../tests/Sqlx.Benchmarks/)
</details>

<details>
<summary>📊 如何启用追踪？</summary>

```xml
<PropertyGroup>
    <DefineConstants>$(DefineConstants);SQLX_ENABLE_TRACING</DefineConstants>
</PropertyGroup>
```

详见：[API 参考](API_REFERENCE.md#追踪和监控)
</details>

---

## 🔗 相关链接

- 🏠 [项目主页](https://github.com/Cricle/Sqlx)
- 📦 [NuGet 包](https://www.nuget.org/packages/Sqlx/)
- 🐛 [问题反馈](https://github.com/Cricle/Sqlx/issues)
- 💡 [讨论区](https://github.com/Cricle/Sqlx/discussions)
- 🌐 [在线文档](https://cricle.github.io/Sqlx/)

---

## 📖 完整占位符列表（前20个）

| 占位符 | 说明 | 示例 |
|--------|------|------|
| `{{table}}` | 表名 | `FROM {{table}}` |
| `{{columns}}` | 列名列表 | `SELECT {{columns}}` |
| `{{values}}` | 参数占位符 | `VALUES ({{values}})` |
| `{{set}}` | SET 子句 | `SET {{set}}` |
| `{{orderby}}` | 排序子句 | `{{orderby name --desc}}` |
| `{{page}}` | 分页（自适应） | `{{page}}` |
| `{{limit}}` | LIMIT 子句 | `{{limit}}` |
| `{{offset}}` | OFFSET 子句 | `{{offset}}` |
| `{{between}}` | BETWEEN 条件 | `{{between:age\|min=@min\|max=@max}}` |
| `{{in}}` | IN 条件 | `{{in:status\|values=@statuses}}` |
| `{{like}}` | LIKE 条件 | `{{like:name\|pattern=@pattern}}` |
| `{{case}}` | CASE 表达式 | `{{case\|column=status\|...}}` |
| `{{count}}` | COUNT 聚合 | `{{count\|column=id}}` |
| `{{sum}}` | SUM 聚合 | `{{sum\|column=amount}}` |
| `{{avg}}` | AVG 聚合 | `{{avg\|column=score}}` |
| `{{row_number}}` | ROW_NUMBER 窗口 | `{{row_number\|orderby=created_at}}` |
| `{{json_extract}}` | JSON 提取 | `{{json_extract\|column=data\|path=$.id}}` |
| `{{concat}}` | 字符串连接 | `{{concat\|columns=first_name,last_name}}` |
| `{{today}}` | 当前日期 | `WHERE created_at = {{today}}` |
| `{{batch_values}}` | 批量插入值 | `VALUES {{batch_values}}` |

> **📝 查看完整列表**：[PLACEHOLDERS.md](PLACEHOLDERS.md) - 包含 80+ 占位符详解

---

## 🎓 学习路径

### 初学者（第 1 天）

1. ✅ 阅读 [快速开始](QUICK_START_GUIDE.md)
2. ✅ 学习 5 个核心占位符：`{{table}}`, `{{columns}}`, `{{values}}`, `{{set}}`, `{{orderby}}`
3. ✅ 运行 [示例项目](../samples/TodoWebApi/)
4. ✅ 完成第一个 CRUD 操作

### 进阶用户（第 2-7 天）

1. ✅ 学习 [分页占位符](PLACEHOLDERS.md#分页占位符)：`{{page}}`, `{{limit}}`, `{{offset}}`
2. ✅ 学习 [条件占位符](PLACEHOLDERS.md#条件占位符)：`{{between}}`, `{{in}}`, `{{like}}`
3. ✅ 学习 [聚合占位符](PLACEHOLDERS.md#聚合占位符)：`{{count}}`, `{{sum}}`, `{{avg}}`
4. ✅ 尝试切换不同数据库
5. ✅ 阅读 [最佳实践](BEST_PRACTICES.md)

### 高级用户（第 8+ 天）

1. ✅ 学习 [窗口函数占位符](PLACEHOLDERS.md#窗口函数)：`{{row_number}}`, `{{rank}}`
2. ✅ 学习 [JSON 占位符](PLACEHOLDERS.md#json-操作)：`{{json_extract}}`, `{{json_array}}`
3. ✅ 学习 [批量操作](PLACEHOLDERS.md#批量操作)：`{{batch_values}}`, `{{bulk_update}}`
4. ✅ 学习 [动态 SQL](PLACEHOLDERS.md#动态占位符)：`{{@tableName}}`（⚠️ 谨慎使用）
5. ✅ 启用 [追踪和监控](API_REFERENCE.md#追踪和监控)
6. ✅ 运行 [性能测试](../tests/Sqlx.Benchmarks/)
7. ✅ 参与 [社区讨论](https://github.com/Cricle/Sqlx/discussions)

---

## ❓ 常见问题

<details>
<summary>Q: Sqlx 和 Dapper 有什么区别？</summary>

**主要区别**：

| 特性 | Sqlx | Dapper |
|------|------|--------|
| **列名维护** | 自动生成 | 手写字符串 |
| **类型安全** | 编译时检查 | 运行时检查 |
| **多数据库** | 一行代码切换 | 需要重写 SQL |
| **批量操作** | 内置优化 | 手动优化 |
| **性能** | 比 Dapper 快 10-285% | 基准 |

**结论**：Sqlx 更适合大型项目和频繁变更的场景。
</details>

<details>
<summary>Q: Sqlx 支持哪些数据库？</summary>

**已支持（6种）**：
- ✅ SQLite
- ✅ SQL Server
- ✅ MySQL
- ✅ PostgreSQL
- ✅ Oracle
- ✅ IBM DB2

**计划支持**：
- 🚧 MariaDB
- 🚧 Firebird
</details>

<details>
<summary>Q: 如何迁移现有项目到 Sqlx？</summary>

**迁移步骤**（渐进式）：

1. 安装 Sqlx 包
2. 定义实体类（保持不变）
3. 创建新的 Repository 接口（使用 Sqlx）
4. 逐步替换旧的 Repository 实现
5. 删除旧代码

**优势**：可以逐步迁移，新旧代码共存。

详见：[最佳实践 - 迁移指南](BEST_PRACTICES.md#迁移现有项目)
</details>

<details>
<summary>Q: Sqlx 生成的代码在哪里？</summary>

生成的代码位于：
```
obj/Debug/net9.0/generated/Sqlx.Generator/Sqlx.Generator.SqlxSourceGenerator/
```

可以在 Visual Studio 的 "解决方案资源管理器" 中展开项目节点查看 "依赖项 → 分析器 → Sqlx.Generator"。
</details>

<details>
<summary>Q: 如何调试生成的代码？</summary>

**方法 1**：在 `.csproj` 中添加：
```xml
<PropertyGroup>
    <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
</PropertyGroup>
```

**方法 2**：在生成的代码中设置断点（Visual Studio 支持）。
</details>

---

## 🤝 贡献文档

发现文档错误或想要改进？欢迎贡献！

1. Fork 项目
2. 编辑 `docs/` 目录下的文件
3. 提交 Pull Request

**文档编写规范**：
- ✅ 使用清晰的标题和章节
- ✅ 提供代码示例
- ✅ 添加适当的警告和提示
- ✅ 保持简洁明了

---

<div align="center">

**感谢使用 Sqlx！**

如果这些文档对你有帮助，请给项目一个 ⭐ Star！

[返回首页](../README.md) · [GitHub](https://github.com/Cricle/Sqlx) · [NuGet](https://www.nuget.org/packages/Sqlx/)

</div>
