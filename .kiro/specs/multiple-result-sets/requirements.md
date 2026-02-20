# 需求文档：多结果集支持

## 介绍

Sqlx 需要支持从 SQL 查询中返回多个值的场景。当前系统已支持输出参数（Output Parameters），但需要明确区分并支持以下场景：

1. **输出参数** - 通过存储过程的 OUTPUT 参数获取值
2. **多结果集** - 通过多个 SELECT 语句返回多个结果集
3. **单结果集多列** - 通过单个 SELECT 返回多列值

## 术语表

- **Output Parameter**: 存储过程的输出参数，使用 `out`/`ref`/`OutputParameter<T>` 标记
- **Multiple Result Sets**: 多个 SELECT 语句产生的多个结果集
- **Tuple Return**: 使用元组返回多个值
- **Result Set**: SQL 查询返回的一个数据集

## 需求

### 需求 1: 输出参数（已实现）

**用户故事**: 作为开发者，我想使用输出参数调用存储过程，以便获取存储过程的输出值。

#### 验收标准

1. WHEN 使用同步方法 THEN 系统应支持 `out` 和 `ref` 参数
2. WHEN 使用异步方法 THEN 系统应支持 `OutputParameter<T>` 包装类
3. WHEN 参数标记为 `[OutputParameter]` THEN 系统应设置 `ParameterDirection.Output` 或 `ParameterDirection.InputOutput`
4. WHEN SQL 执行完成 THEN 系统应从 DbParameter 中检索输出值

### 需求 2: 多结果集返回

**用户故事**: 作为开发者，我想通过多个 SELECT 语句返回多个标量值，并使用特性明确指定每个结果集的对应关系，以便在一次数据库调用中获取多个相关数据且不会混淆。

#### 验收标准

1. WHEN 方法返回元组类型 THEN 系统应识别为多结果集返回
2. WHEN 使用 `[ResultSetMapping]` 特性 THEN 系统应按特性指定的顺序读取结果集
3. WHEN 索引为 0 且名称为 "rowsAffected" THEN 系统应使用 `ExecuteNonQuery` 的返回值
4. WHEN 索引大于 0 THEN 系统应使用 `NextResult()` 遍历到对应的结果集
5. WHEN 读取每个结果集 THEN 系统应将第一行第一列的值赋给对应的元组元素
6. WHEN 元组元素类型与数据库类型不匹配 THEN 系统应进行类型转换
7. WHEN 结果集数量少于映射数量 THEN 系统应抛出异常
8. WHEN 未使用 `[ResultSetMapping]` 特性 THEN 系统应按默认顺序读取（第一个元素=受影响行数，其余按SELECT顺序）
9. WHEN 结果集为空 THEN 系统应使用默认值或抛出异常

### 需求 3: 单结果集多列返回

**用户故事**: 作为开发者，我想通过单个 SELECT 返回多列值到元组，以便获取实体的部分字段。

#### 验收标准

1. WHEN 方法返回元组且 SQL 只有一个 SELECT THEN 系统应从单个结果集读取多列
2. WHEN 读取结果集 THEN 系统应按列索引将值赋给元组元素
3. WHEN 列数少于元组元素数量 THEN 系统应抛出异常
4. WHEN 结果集为空 THEN 系统应返回 null 或默认值元组

### 需求 4: 混合场景

**用户故事**: 作为开发者，我想同时使用输出参数和返回值，以便在一次调用中获取多种类型的数据。

#### 验收标准

1. WHEN 方法同时有输出参数和元组返回 THEN 系统应支持两者
2. WHEN 执行 SQL THEN 系统应先处理返回值，再处理输出参数
3. WHEN 同步方法使用 `out` 参数和元组返回 THEN 系统应正确处理
4. WHEN 异步方法使用 `OutputParameter<T>` 和元组返回 THEN 系统应正确处理

### 需求 5: 区分机制

**用户故事**: 作为开发者，我想系统能自动区分输出参数和多结果集，以便我不需要额外配置。

#### 验收标准

1. WHEN 参数标记为 `[OutputParameter]` THEN 系统应识别为输出参数
2. WHEN 方法返回元组且无 `[OutputParameter]` 标记 THEN 系统应识别为多结果集返回
3. WHEN 方法返回元组且有 `[OutputParameter]` 标记 THEN 系统应同时支持两者
4. WHEN SQL 只有一个 SELECT 且返回元组 THEN 系统应识别为单结果集多列返回

## 示例

### 示例 1: 输出参数（已实现）

```csharp
public interface IUserRepository
{
    // 同步方法
    [SqlTemplate("INSERT INTO users (name) VALUES (@name); SELECT last_insert_rowid()")]
    int Insert(string name, [OutputParameter(DbType.Int32)] out int id);
    
    // 异步方法
    [SqlTemplate("INSERT INTO users (name) VALUES (@name); SELECT last_insert_rowid()")]
    Task<int> InsertAsync(string name, [OutputParameter(DbType.Int32)] OutputParameter<int> id);
}
```

### 示例 2: 多结果集返回（新功能）

```csharp
public interface IUserRepository
{
    // 使用 ResultSetMapping 特性明确指定每个结果集的对应关系
    [SqlTemplate(@"
        INSERT INTO users (name) VALUES (@name);
        SELECT last_insert_rowid();
        SELECT COUNT(*) FROM users;
        SELECT MAX(id) FROM users
    ")]
    [ResultSetMapping(0, "rowsAffected")]  // 第0个：INSERT 受影响的行数
    [ResultSetMapping(1, "userId")]         // 第1个结果集：last_insert_rowid()
    [ResultSetMapping(2, "totalUsers")]     // 第2个结果集：COUNT(*)
    [ResultSetMapping(3, "maxId")]          // 第3个结果集：MAX(id)
    Task<(int rowsAffected, long userId, int totalUsers, long maxId)> InsertAndGetStatsAsync(string name);
    
    // 不使用特性时，按默认顺序：第一个元素=受影响行数，其余按SELECT顺序
    [SqlTemplate(@"
        INSERT INTO users (name) VALUES (@name);
        SELECT last_insert_rowid();
        SELECT COUNT(*) FROM users
    ")]
    Task<(int rowsAffected, long userId, int totalUsers)> InsertAndGetStatsSimpleAsync(string name);
}

// 使用
var (rows, userId, total, maxId) = await repo.InsertAndGetStatsAsync("Alice");
```

### 示例 3: 单结果集多列返回（新功能）

```csharp
public interface IUserRepository
{
    // 单个 SELECT，返回多列
    [SqlTemplate("SELECT id, name, email FROM users WHERE id = @id")]
    Task<(int id, string name, string email)?> GetUserBasicInfoAsync(int id);
}

// 使用
var userInfo = await repo.GetUserBasicInfoAsync(123);
if (userInfo.HasValue)
{
    var (id, name, email) = userInfo.Value;
}
```

### 示例 4: 混合场景（新功能）

```csharp
public interface IUserRepository
{
    // 同时使用输出参数和元组返回
    [SqlTemplate(@"
        INSERT INTO users (name, created_at) VALUES (@name, @createdAt);
        SELECT last_insert_rowid();
        SELECT COUNT(*) FROM users
    ")]
    [ResultSetMapping(0, "rowsAffected")]
    [ResultSetMapping(1, "userId")]
    [ResultSetMapping(2, "totalUsers")]
    (int rowsAffected, long userId, int totalUsers) InsertAndGetStats(
        string name,
        [OutputParameter(DbType.DateTime)] ref DateTime createdAt);
}

// 使用
DateTime created = DateTime.UtcNow;
var (rows, userId, total) = repo.InsertAndGetStats("Bob", ref created);
```

## 技术约束

1. 元组返回值仅支持值元组（ValueTuple），不支持引用元组（Tuple）
2. 元组元素必须是可从数据库类型转换的类型
3. 多结果集的 SELECT 语句必须返回至少一行一列
4. 单结果集多列的 SELECT 必须返回足够的列数
5. 输出参数和元组返回可以同时使用
6. 异步方法不能使用 `out`/`ref` 参数，必须使用 `OutputParameter<T>`

## 优先级

1. 高优先级：多结果集返回（需求 2）
2. 高优先级：单结果集多列返回（需求 3）
3. 中优先级：混合场景（需求 4）
4. 低优先级：区分机制优化（需求 5）
