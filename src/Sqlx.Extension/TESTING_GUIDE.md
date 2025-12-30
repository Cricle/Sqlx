# Sqlx.Extension 测试指南

> **版本**: 0.5.0-dev
> **状态**: 所有 P0 功能完成
> **测试对象**: Visual Studio 2022 插件

---

## 🎯 测试目标

验证所有 P0 功能正常工作：
- ✅ 语法着色
- ✅ 代码片段
- ✅ 快速操作
- ✅ 参数验证

---

## 🛠️ 测试环境

### 必需软件

- **Visual Studio 2022** (17.0+)
- **VS SDK** (Visual Studio extension development 工作负载)
- **.NET Framework 4.7.2**

### 可选软件

- **Sqlx NuGet 包** (用于完整测试)
- **SQLite/MySQL/PostgreSQL** (用于运行时测试)

---

## 📝 测试前准备

### 1. 构建插件

```bash
# 方法1: 在 Visual Studio 中
1. 打开 Sqlx.sln
2. 定位到 Sqlx.Extension 项目
3. 按 Ctrl+Shift+B 构建
4. 确认输出：bin\Release\Sqlx.Extension.vsix

# 方法2: 使用 MSBuild (Developer Command Prompt)
cd src/Sqlx.Extension
msbuild Sqlx.Extension.csproj /p:Configuration=Release
```

### 2. 启动实验实例

```bash
# 在 Visual Studio 中
1. 设置 Sqlx.Extension 为启动项目
2. 按 F5 启动调试
3. 等待新的 Visual Studio 实验实例打开
```

### 3. 准备测试项目

在实验实例中：
```bash
1. 创建新的 C# 控制台项目
2. 添加 Sqlx NuGet 包
3. 创建测试文件
```

---

## 🎨 功能 1: 语法着色测试

### 测试步骤

1. **创建测试文件**

```csharp
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace TestProject
{
    [SqlDefine(SqlDefineTypes.SQLite)]
    [RepositoryFor(typeof(User))]
    public interface IUserRepository
    {
        [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
        Task<User?> GetByIdAsync(long id, CancellationToken ct = default);

        [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE age >= @minAge AND status = 'active' -- Get active users")]
        Task<List<User>> SearchAsync(int minAge, CancellationToken ct = default);
    }

    public class User
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
        public string Status { get; set; }
    }
}
```

2. **验证着色效果**

检查 `[SqlTemplate]` 属性中的字符串：

| 元素 | 预期颜色 | 示例 |
|------|---------|------|
| SQL关键字 | 🔵 蓝色 | SELECT, FROM, WHERE, AND |
| 占位符 | 🟠 橙色 | {{columns}}, {{table}} |
| 参数 | 🟢 绿色 | @id, @minAge |
| 字符串 | 🟤 棕色 | 'active' |
| 注释 | ⚪ 灰色 | -- Get active users |

### ✅ 通过标准

- [x] 所有 SQL 关键字显示为蓝色
- [x] 所有占位符 {{...}} 显示为橙色
- [x] 所有参数 @... 显示为绿色
- [x] 字符串字面量显示为棕色
- [x] 注释显示为灰色
- [x] 多行 SQL 正确着色
- [x] 无性能问题（无卡顿）

### ❌ 常见问题

**问题**: 没有颜色显示

**原因**:
- MEF 组件未加载
- 项目未正确构建
- VS 缓存问题

**解决**:
1. 重新构建项目
2. 重启实验实例
3. 清除 VS 缓存

---

## 📦 功能 2: 代码片段测试

### 测试步骤

1. **测试基本片段**

在 C# 文件中：

| 片段 | 触发 | 预期结果 |
|------|------|---------|
| sqlx-repo | 输入 `sqlx-repo` + Tab | 生成仓储接口和实现 |
| sqlx-entity | 输入 `sqlx-entity` + Tab | 生成实体类 |
| sqlx-select | 输入 `sqlx-select` + Tab | 生成 SELECT 方法 |
| sqlx-insert | 输入 `sqlx-insert` + Tab | 生成 INSERT 方法 |
| sqlx-update | 输入 `sqlx-update` + Tab | 生成 UPDATE 方法 |
| sqlx-delete | 输入 `sqlx-delete` + Tab | 生成 DELETE 方法 |

2. **验证片段内容**

```csharp
// 测试 sqlx-repo
// 输入: sqlx-repo + Tab
// 预期: 生成完整的仓储接口和类

// 输入 sqlx-select + Tab
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
Task<Entity?> GetByIdAsync(long id, CancellationToken ct = default);
```

3. **测试占位符**

- Tab 键应该在占位符之间跳转
- 修改占位符内容应该更新代码

### ✅ 通过标准

- [x] 所有片段都能触发
- [x] 生成的代码格式正确
- [x] 占位符可以 Tab 跳转
- [x] 代码可以正常编译

### ❌ 常见问题

**问题**: 片段不显示

**原因**:
- 片段文件未包含在 VSIX 中
- 文件路径错误

**解决**:
1. 检查 .vsixmanifest 配置
2. 重新构建项目

---

## ⚡ 功能 3: 快速操作测试

### 测试 3.1: 生成仓储

1. **创建实体类**

```csharp
public class Product
{
    public long Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
}
```

2. **触发快速操作**

- 将光标放在 `Product` 类名上
- 右键 → 快速操作和重构（或 Ctrl+.）
- 选择 "Generate Sqlx Repository for 'Product'"

3. **验证生成的代码**

应该生成两个新文件：

**IProductRepository.cs**:
```csharp
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(Product))]
public interface IProductRepository
{
    Task<Product?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<List<Product>> GetAllAsync(CancellationToken ct = default);
    Task<long> InsertAsync(Product entity, CancellationToken ct = default);
    Task<int> UpdateAsync(Product entity, CancellationToken ct = default);
    Task<int> DeleteAsync(long id, CancellationToken ct = default);
    // ... 其他方法
}
```

**ProductRepository.cs**:
```csharp
public partial class ProductRepository(DbConnection connection) : IProductRepository
{
    private readonly DbConnection _connection = connection;
    // Implementation is auto-generated by Sqlx source generator
}
```

### ✅ 通过标准

- [x] 快速操作菜单出现
- [x] 生成两个文件
- [x] 接口包含 8 个方法
- [x] 方法签名正确
- [x] 包含完整的 XML 注释

### 测试 3.2: 添加 CRUD 方法

1. **创建空接口**

```csharp
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(Order))]
public interface IOrderRepository
{
    // 空接口
}
```

2. **触发快速操作**

- 将光标放在接口名上
- 右键 → 快速操作（Ctrl+.）
- 应该看到多个选项：
  - Add GetById method
  - Add GetAll method
  - Add Insert method
  - Add Update method
  - Add Delete method
  - Add Query method (Expression)
  - Add Count method
  - Add all CRUD methods

3. **测试单个方法**

选择 "Add GetById method"，验证生成：

```csharp
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
Task<TEntity?> GetByIdAsync(TKey id, CancellationToken ct = default);
```

4. **测试批量添加**

选择 "Add all CRUD methods"，验证添加所有方法。

### ✅ 通过标准

- [x] 快速操作菜单出现
- [x] 所有方法选项都可用
- [x] 单个方法添加正确
- [x] 批量添加所有方法
- [x] 方法格式正确

---

## 🔍 功能 4: 参数验证测试

### 测试 4.1: SQLX001 - 参数未找到

1. **创建错误代码**

```csharp
[SqlTemplate("SELECT * FROM users WHERE id = @userId")]
Task<User?> GetUserAsync(long id);  // ← 参数名不匹配
```

2. **验证诊断**

- 应该看到红色波浪线
- 错误消息: "SQL parameter '@userId' is used in the template but not found in method parameters"
- 严重性: Error

3. **测试自动修复**

- 将光标放在错误处
- 按 Ctrl+. 或点击灯泡图标
- 选择 "Add parameter 'userId'"
- 验证自动添加参数

### 测试 4.2: SQLX002 - 参数未使用

1. **创建警告代码**

```csharp
[SqlTemplate("SELECT * FROM users WHERE id = @id")]
Task<User?> GetUserAsync(long id, string unused);  // ← unused 未使用
```

2. **验证诊断**

- 应该看到黄色波浪线（警告）
- 警告消息: "Method parameter 'unused' is not used in the SQL template"
- 严重性: Warning

3. **测试自动修复**

- Ctrl+. → "Remove unused parameter"
- 验证自动移除参数

### 测试 4.3: SQLX003 - 类型不适合

1. **创建警告代码**

```csharp
public class ComplexType
{
    public Dictionary<string, object> Data { get; set; }
}

[SqlTemplate("SELECT * FROM users WHERE data = @data")]
Task<User?> QueryAsync(ComplexType data);  // ← 复杂类型
```

2. **验证诊断**

- 应该看到警告
- 警告消息提示类型可能不适合

### 测试 4.4: 特殊参数（不应警告）

1. **测试系统参数**

```csharp
[SqlTemplate("SELECT * FROM users WHERE id = @id")]
Task<User?> GetAsync(long id, CancellationToken ct);  // ← ct 不应警告
```

2. **测试实体参数**

```csharp
[SqlTemplate("UPDATE {{table}} {{set}} WHERE id = @id")]
Task<int> UpdateAsync(User user);  // ← user 不应警告（用于 {{set}}）
```

3. **测试批量操作**

```csharp
[SqlTemplate("INSERT INTO {{table}} {{batch_values}}")]
Task<int> BatchInsertAsync(IEnumerable<User> users);  // ← users 不应警告
```

### ✅ 通过标准

- [x] 错误和警告正确显示
- [x] 诊断消息准确
- [x] 自动修复功能工作
- [x] 特殊参数不误报
- [x] 实时分析（< 100ms）

---

## 🧪 综合测试

### 场景 1: 完整工作流

1. 创建实体 `Customer`
2. 使用快速操作生成仓储
3. 验证语法着色
4. 使用代码片段添加自定义方法
5. 修复参数验证错误

### 场景 2: 性能测试

1. 打开大型文件（1000+行）
2. 添加多个 SqlTemplate 方法
3. 观察编辑器响应速度
4. 验证无卡顿、无延迟

### 场景 3: 边界测试

1. **空 SQL**
```csharp
[SqlTemplate("")]
Task TestAsync();
```

2. **超长 SQL**
```csharp
[SqlTemplate(@"
    SELECT * FROM users u
    JOIN orders o ON o.user_id = u.id
    JOIN products p ON p.id = o.product_id
    WHERE u.age >= @age
    AND o.status = @status
    AND p.category IN (@categories)
    ORDER BY o.created_at DESC
    LIMIT @limit OFFSET @offset
")]
Task<List<Result>> ComplexQueryAsync(...);
```

3. **特殊字符**
```csharp
[SqlTemplate("SELECT * FROM users WHERE name LIKE '%@pattern%'")]
Task SearchAsync(string pattern);
```

---

## 📊 测试报告模板

### 测试环境

- **VS 版本**: _________
- **插件版本**: 0.5.0-dev
- **测试日期**: _________
- **测试人员**: _________

### 测试结果

| 功能 | 测试项 | 结果 | 备注 |
|------|--------|------|------|
| 语法着色 | 关键字 | ✅/❌ | |
| | 占位符 | ✅/❌ | |
| | 参数 | ✅/❌ | |
| | 字符串 | ✅/❌ | |
| | 注释 | ✅/❌ | |
| 代码片段 | sqlx-repo | ✅/❌ | |
| | sqlx-select | ✅/❌ | |
| | 其他片段 | ✅/❌ | |
| 快速操作 | 生成仓储 | ✅/❌ | |
| | 添加方法 | ✅/❌ | |
| 参数验证 | SQLX001 | ✅/❌ | |
| | SQLX002 | ✅/❌ | |
| | SQLX003 | ✅/❌ | |
| | 自动修复 | ✅/❌ | |

### 发现的问题

1. 问题描述:
   - 重现步骤:
   - 预期结果:
   - 实际结果:

### 总体评价

- **功能完整性**: ☐ 优秀 ☐ 良好 ☐ 一般 ☐ 需改进
- **性能表现**: ☐ 优秀 ☐ 良好 ☐ 一般 ☐ 需改进
- **用户体验**: ☐ 优秀 ☐ 良好 ☐ 一般 ☐ 需改进
- **稳定性**: ☐ 优秀 ☐ 良好 ☐ 一般 ☐ 需改进

---

## 🐛 故障排除

### 问题 1: 插件未加载

**症状**: 没有任何功能工作

**解决**:
1. 检查实验实例：工具 → 扩展和更新
2. 确认 Sqlx.Extension 已安装且启用
3. 重启实验实例

### 问题 2: 语法着色不工作

**症状**: SQL 字符串没有颜色

**解决**:
1. 检查是否在 `[SqlTemplate(...)]` 属性中
2. 重新打开文件
3. 清除 VS 缓存

### 问题 3: 快速操作不显示

**症状**: 右键没有 Sqlx 相关选项

**解决**:
1. 确认光标位置正确
2. 检查类/接口是否符合条件
3. 重新构建项目

### 问题 4: 诊断不工作

**症状**: 没有错误/警告提示

**解决**:
1. 检查 Roslyn 分析器是否启用
2. 工具 → 选项 → 文本编辑器 → C# → 高级
3. 确认"启用完整解决方案分析"已勾选

---

## 📞 报告问题

如果发现 bug 或有建议，请：

1. **GitHub Issues**
   - 访问: https://github.com/Cricle/Sqlx/issues
   - 创建新 issue
   - 使用模板填写

2. **包含信息**
   - VS 版本
   - 插件版本
   - 重现步骤
   - 预期/实际结果
   - 截图（如果可能）

---

## ✅ 测试完成检查清单

### 基础测试

- [ ] 插件成功构建
- [ ] 实验实例启动正常
- [ ] 所有文件正确包含在 VSIX 中

### 功能测试

- [ ] 语法着色：所有元素正确着色
- [ ] 代码片段：所有片段可用且正确
- [ ] 快速操作：生成仓储功能正常
- [ ] 快速操作：添加方法功能正常
- [ ] 参数验证：所有诊断规则正确
- [ ] 参数验证：自动修复功能正常

### 性能测试

- [ ] 语法着色无延迟（< 1ms）
- [ ] 快速操作响应快（< 200ms）
- [ ] 诊断分析实时（< 50ms）
- [ ] 大文件无卡顿

### 稳定性测试

- [ ] 无崩溃
- [ ] 无内存泄漏
- [ ] 无异常错误

### 文档测试

- [ ] README 正确显示
- [ ] 示例代码可用
- [ ] 文档完整

---

**测试版本**: 0.5.0-dev
**最后更新**: 2025-10-29
**状态**: ✅ 准备测试

