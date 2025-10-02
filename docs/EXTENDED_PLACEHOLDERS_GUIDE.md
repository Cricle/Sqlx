# 🚀 Sqlx 扩展占位符指南

## ✨ 概述

在核心7个占位符基础上，Sqlx 新增了**15个扩展占位符**，覆盖更多实际开发场景！

### 📊 **完整占位符列表 (22个)**

#### 🔥 **核心占位符 (7个)**
- `{{table}}` - 表名处理
- `{{columns}}` - 列名生成
- `{{values}}` - 值占位符
- `{{where}}` - WHERE条件
- `{{set}}` - SET子句
- `{{orderby}}` - ORDER BY排序
- `{{limit}}` - LIMIT分页

#### ⭐ **扩展占位符 (15个)**
- `{{join}}` - JOIN连接
- `{{groupby}}` - GROUP BY分组
- `{{having}}` - HAVING条件
- `{{select}}` - SELECT子句
- `{{insert}}` - INSERT语句
- `{{update}}` - UPDATE语句
- `{{delete}}` - DELETE语句
- `{{count}}` - COUNT函数
- `{{sum}}` - SUM求和
- `{{avg}}` - AVG平均值
- `{{max}}` - MAX最大值
- `{{min}}` - MIN最小值
- `{{distinct}}` - DISTINCT去重
- `{{union}}` - UNION联合
- `{{top}}` - TOP限制（SQL Server）
- `{{offset}}` - OFFSET偏移

---

## 🎯 **使用示例**

### **1. JOIN 连接查询**

```csharp
// INNER JOIN
[Sqlx("{{select:all}} FROM {{table}} {{join:inner|table=department|on=user.dept_id = department.id}}")]
Task<List<User>> GetUsersWithDepartmentAsync();

// LEFT JOIN
[Sqlx("SELECT u.name, d.name FROM {{table}} u {{join:left|table=department d|on=u.dept_id = d.id}}")]
Task<List<dynamic>> GetUsersWithOptionalDeptAsync();

// 生成SQL:
// SELECT * FROM user INNER JOIN department ON user.dept_id = department.id
// SELECT u.name, d.name FROM user u LEFT JOIN department d ON u.dept_id = d.id
```

### **2. GROUP BY 和 HAVING**

```csharp
// 自动推断分组
[Sqlx("SELECT dept_id, {{count:all}} FROM {{table}} {{groupby:auto}} {{having:count}}")]
Task<List<dynamic>> GetUserCountByDeptAsync(int deptId);

// 指定分组列
[Sqlx("SELECT dept_id, {{avg:salary}} FROM {{table}} {{groupby:dept_id}} {{having:auto}}")]
Task<List<dynamic>> GetAvgSalaryByDeptAsync(decimal minSalary);

// 生成SQL:
// SELECT dept_id, COUNT(*) FROM user GROUP BY dept_id HAVING COUNT(*) > 0
// SELECT dept_id, AVG(salary) FROM user GROUP BY dept_id HAVING salary > @minSalary
```

### **3. 聚合函数**

```csharp
// 计数
[Sqlx("{{select:count}} FROM {{table}} {{where:auto}}")]
Task<int> CountActiveUsersAsync(bool isActive);

// 求和
[Sqlx("SELECT {{sum:salary}} FROM {{table}} {{where:auto}}")]
Task<decimal> GetTotalSalaryAsync(int deptId);

// 统计组合
[Sqlx("SELECT {{avg:age}}, {{max:salary}}, {{min:age}} FROM {{table}} {{where:auto}}")]
Task<dynamic> GetStatsAsync(bool isActive);

// 生成SQL:
// SELECT COUNT(*) FROM user WHERE is_active = @isActive
// SELECT SUM(salary) FROM user WHERE dept_id = @deptId
// SELECT AVG(age), MAX(salary), MIN(age) FROM user WHERE is_active = @isActive
```

### **4. SELECT 变体**

```csharp
// DISTINCT 去重
[Sqlx("SELECT {{distinct:dept_id}} FROM {{table}} {{where:auto}}")]
Task<List<int>> GetDistinctDepartmentsAsync(bool isActive);

// TOP 限制（SQL Server）
[Sqlx("SELECT {{top|count=5}} * FROM {{table}} {{orderby:salary}} DESC")]
Task<List<User>> GetTopPaidUsersAsync();

// 生成SQL:
// SELECT DISTINCT dept_id FROM user WHERE is_active = @isActive
// SELECT TOP 5 * FROM user ORDER BY salary DESC
```

### **5. 完整SQL语句简化**

```csharp
// INSERT 语句 - 使用 {{insert}} 占位符
[Sqlx("{{insert}} ({{columns:auto|exclude=Id}}) VALUES ({{values:auto}})")]
Task<int> InsertUserAsync(User user);

// 或者使用 {{insert:into}} 更明确
[Sqlx("{{insert:into}} ({{columns:auto|exclude=Id}}) VALUES ({{values:auto}})")]
Task<int> InsertUserWithReturnAsync(User user);

// UPDATE 语句 - 使用 {{update}} 占位符
[Sqlx("{{update}} SET {{set:auto}} WHERE {{where:id}}")]
Task<int> UpdateUserAsync(User user);

// DELETE 语句 - 使用 {{delete}} 占位符  
[Sqlx("{{delete}} WHERE {{where:auto}}")]
Task<int> DeleteInactiveUsersAsync(bool isActive);

// 生成SQL:
// INSERT INTO user (name, email, age, salary) VALUES (@Name, @Email, @Age, @Salary)
// INSERT INTO user (name, email, age, salary) VALUES (@Name, @Email, @Age, @Salary)
// UPDATE user SET name = @Name, email = @Email WHERE id = @Id
// DELETE FROM user WHERE is_active = @isActive
```

**INSERT占位符说明**：
- `{{insert}}` - 生成 `INSERT INTO table_name`
- `{{insert:into}}` - 同上，更明确的语法
- 配合 `{{columns:auto|exclude=Id}}` 自动生成列名列表
- 配合 `{{values:auto}}` 自动生成参数占位符

### **6. 高级查询**

```csharp
// UNION 联合
[Sqlx(@"SELECT name, 'Active' as status FROM {{table}} WHERE is_active = 1
         {{union:all}}
         SELECT name, 'Inactive' as status FROM {{table}} WHERE is_active = 0")]
Task<List<dynamic>> GetAllUsersWithStatusAsync();

// OFFSET 分页（跨数据库）
[Sqlx("SELECT * FROM {{table}} {{orderby:id}} {{offset:sqlserver|offset=10|rows=5}}")]
Task<List<User>> GetUsersPagedSqlServerAsync();

[Sqlx("SELECT * FROM {{table}} {{orderby:name}} {{offset:mysql|offset=0|rows=10}}")]
Task<List<User>> GetUsersPagedMySqlAsync();

// 生成SQL:
// SELECT name, 'Active' as status FROM user WHERE is_active = 1
// UNION ALL
// SELECT name, 'Inactive' as status FROM user WHERE is_active = 0

// SQL Server: SELECT * FROM user ORDER BY id OFFSET 10 ROWS FETCH NEXT 5 ROWS ONLY
// MySQL: SELECT * FROM user ORDER BY name LIMIT 10 OFFSET 0
```

---

## 🎨 **占位符选项详解**

### **JOIN 占位符选项**
- `type`: `inner|left|right|full`
- `table`: 连接表名
- `on`: 连接条件

### **聚合函数选项**
- `all`: 使用 `*`
- `distinct`: 使用 `DISTINCT`
- `column`: 指定列名

### **OFFSET 占位符选项**
- `sqlserver`: SQL Server语法
- `mysql`: MySQL语法
- `offset`: 偏移量
- `rows`: 行数

### **通用选项**
- `exclude`: 排除列名（逗号分隔）
- `default`: 默认值
- `condition`: 自定义条件

---

## ⚡ **性能特性**

### ✅ **编译时优化**
- 所有占位符在编译时处理
- 零运行时开销
- 智能缓存机制

### ✅ **类型安全**
- 编译时验证SQL语法
- 强类型参数绑定
- 智能类型推断

### ✅ **多数据库支持**
- 自动方言适配
- 跨数据库兼容
- 智能SQL生成

---

## 🛡️ **最佳实践**

### **1. 合理组合使用**
```csharp
// ✅ 推荐：清晰的占位符组合
[Sqlx(@"{{select:all}} FROM {{table}}
         {{join:inner|table=dept|on=user.dept_id = dept.id}}
         {{where:auto}}
         {{groupby:dept_id}}
         {{having:count}}
         {{orderby:dept_name}}
         {{limit:mysql|default=20}}")]
```

### **2. 避免过度复杂**
```csharp
// ❌ 避免：过于复杂的单一查询
// 建议拆分为多个简单查询
```

### **3. 利用类型安全**
```csharp
// ✅ 推荐：强类型返回
Task<List<UserStats>> GetUserStatsAsync(bool isActive);

// ❌ 避免：过度使用dynamic
Task<List<dynamic>> GetComplexDataAsync();
```

---

## 🎯 **升级指南**

### **从核心7个占位符升级**
1. **逐步替换**：先在新功能中使用扩展占位符
2. **保持兼容**：现有核心占位符继续工作
3. **性能提升**：扩展占位符提供更好的性能和类型安全

### **迁移建议**
```csharp
// 旧方式：手写复杂SQL
[Sqlx("SELECT u.*, d.name as dept_name FROM user u INNER JOIN department d ON u.dept_id = d.id WHERE u.is_active = @isActive")]

// 新方式：使用扩展占位符
[Sqlx("{{select:all}} FROM {{table}} {{join:inner|table=department d|on=user.dept_id = d.id}} {{where:auto}}")]
```

---

## 📈 **功能对比**

| 功能 | 核心7个占位符 | +15个扩展占位符 |
|------|-------------|----------------|
| 基础CRUD | ✅ | ✅ |
| JOIN查询 | ❌ | ✅ |
| 聚合函数 | ❌ | ✅ |
| GROUP BY/HAVING | ❌ | ✅ |
| UNION操作 | ❌ | ✅ |
| 跨数据库分页 | ❌ | ✅ |
| SQL语句模板 | ❌ | ✅ |
| 性能优化 | Good | Excellent |

---

## 🎉 **总结**

通过**15个扩展占位符**，Sqlx 模板引擎现在支持：

- 📊 **复杂查询**：JOIN、GROUP BY、HAVING、UNION
- 🔢 **聚合统计**：COUNT、SUM、AVG、MAX、MIN
- 🎯 **精准控制**：DISTINCT、TOP、OFFSET
- 🛠️ **SQL模板**：INSERT、UPDATE、DELETE语句生成
- ⚡ **高性能**：编译时处理，零运行时开销
- 🛡️ **类型安全**：强类型验证，智能推断

**让复杂SQL编写变得简单而优雅！** ✨

