# Sqlx 示例代码

本目录包含 Sqlx 库的各种使用示例，帮助您快速上手。

## 📁 示例列表

### 基础示例

1. **[BasicUsageExample.cs](BasicUsageExample.cs)** - 基础用法
   - SqlQuery 基本查询
   - CRUD 操作
   - 参数化查询
   - 事务处理

2. **[SqlBuilderExample.cs](SqlBuilderExample.cs)** - SqlBuilder 动态查询
   - 基本查询构建
   - 动态条件
   - 子查询
   - 复杂查询组合

### 高级功能

3. **[MultipleResultSetsExample.cs](MultipleResultSetsExample.cs)** - 多结果集
   - ResultSetMapping 特性
   - 返回多个标量值
   - 同步和异步方法

4. **[OutputParameterExample.cs](OutputParameterExample.cs)** - 输出参数
   - out 参数（同步）
   - OutputParameter 包装类（异步）
   - ref 参数（InputOutput 模式）
   - 多个输出参数

5. **[StoredProcedureExample.cs](StoredProcedureExample.cs)** - 存储过程
   - 调用存储过程
   - 输入输出参数
   - 返回值处理

### 企业级功能

6. **[ExceptionHandlingExample.cs](ExceptionHandlingExample.cs)** - 异常处理
   - SqlxException 上下文信息
   - 重试机制
   - 自定义异常回调
   - 日志集成
   - 依赖注入配置

7. **[TransactionExample.cs](TransactionExample.cs)** - 事务管理
   - 基本事务
   - 嵌套事务
   - 分布式事务
   - 事务隔离级别

### 完整应用示例

8. **[TodoWebApi/](TodoWebApi/)** - Todo Web API 完整示例
   - ASP.NET Core 集成
   - Repository 模式
   - 依赖注入
   - RESTful API 设计

## 🚀 快速开始

### 运行单个示例

```bash
# 编译并运行示例
dotnet run --project samples/BasicUsageExample.cs
```

### 运行 Web API 示例

```bash
cd samples/TodoWebApi
dotnet run
```

然后访问 `http://localhost:5000/swagger` 查看 API 文档。

## 📚 学习路径

推荐按以下顺序学习示例：

1. **初学者**: BasicUsageExample → SqlBuilderExample
2. **进阶**: MultipleResultSetsExample → OutputParameterExample
3. **高级**: ExceptionHandlingExample → TransactionExample
4. **实战**: TodoWebApi

## 💡 提示

- 所有示例都使用 SQLite 内存数据库，无需额外配置
- 示例代码包含详细的中英文注释
- 每个示例都是独立的，可以单独运行
- 示例展示了最佳实践和常见模式

## 🔗 相关资源

- [完整文档](../docs/README.md)
- [API 参考](../docs/api-reference.md)
- [快速入门](../docs/getting-started.md)
- [GitHub 仓库](https://github.com/Cricle/Sqlx)

## 📝 贡献

欢迎提交新的示例！请确保：
- 代码清晰易懂
- 包含中英文注释
- 遵循项目代码规范
- 添加到本 README 的示例列表中
