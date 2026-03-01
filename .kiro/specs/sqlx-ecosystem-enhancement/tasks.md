# 实施计划：Sqlx 生态系统增强

## 概述

本任务列表描述了如何实现 Sqlx 生态系统增强的具体步骤，包括 ASP.NET Core 集成包、文档、示例和日志功能。

## 任务

- [ ] 1. 创建 Sqlx.AspNetCore 项目
  - 创建新的类库项目和基础结构
  - _需求: 1.1, 1.2_

- [ ] 1.1 创建项目结构
  - 创建 src/Sqlx.AspNetCore 目录
  - 创建 Sqlx.AspNetCore.csproj 文件
  - 添加必要的 NuGet 包引用（Microsoft.Extensions.DependencyInjection, Microsoft.Extensions.Logging等）
  - _需求: 1.1_

- [ ] 1.2 实现 SqlxOptions 配置类
  - 创建 SqlxOptions.cs
  - 定义连接字符串、方言、日志等配置属性
  - 添加验证逻辑
  - _需求: 1.7_

- [ ] 1.3 实现 ServiceCollectionExtensions
  - 创建 SqlxServiceCollectionExtensions.cs
  - 实现 AddSqlx() 扩展方法
  - 实现 AddSqlxRepositories() 自动注册方法
  - 实现 ISqlxBuilder 接口和实现类
  - _需求: 1.1, 1.2_

- [ ] 1.4 实现 SqlxConnectionFactory
  - 创建 ISqlxConnectionFactory 接口
  - 实现 SqlxConnectionFactory 类
  - 支持多数据库连接管理
  - 支持命名连接配置
  - _需求: 1.4_

- [ ] 2. 实现 SQL 日志中间件
  - 创建日志中间件和拦截器，包含慢查询检测
  - _需求: 1.6_

- [ ] 2.1 创建 SqlExecutionContext 模型
  - 创建 SqlExecutionContext.cs
  - 定义 SQL、参数、仓储名称、方法名称等属性
  - 添加时间戳和关联 ID
  - _需求: 1.6_

- [ ] 2.2 实现 ISqlExecutionInterceptor 接口
  - 创建 ISqlExecutionInterceptor.cs
  - 定义 OnBeforeExecute、OnAfterExecute、OnError 事件
  - _需求: 1.6_

- [ ] 2.3 实现 SqlLoggingMiddleware
  - 创建 SqlLoggingMiddleware.cs
  - 实现 SQL 执行前后的日志记录
  - 实现敏感数据掩码功能
  - 实现慢查询检测（基于阈值）
  - 集成 ASP.NET Core ILogger
  - _需求: 1.6_

- [ ] 2.4 实现日志级别配置
  - 支持 Debug、Information、Warning、Error 级别
  - 支持按仓储或方法过滤
  - _需求: 1.6_

- [ ] 3. 创建快速开始文档
  - 编写 5 分钟快速上手指南
  - _需求: 2.1, 2.2, 2.3_

- [ ] 3.1 编写 docs/getting-started.md
  - 创建文档文件
  - 编写安装说明（dotnet add package, NuGet, etc.）
  - 编写第一个示例（实体、仓储、查询）
  - 解释三个核心概念
  - 添加故障排除部分
  - 添加下一步链接
  - _需求: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6_

- [ ] 3.2 添加多数据库示例
  - 添加 SQLite 示例
  - 添加 PostgreSQL 示例
  - 添加 MySQL 示例
  - 添加 SQL Server 示例
  - _需求: 2.7_

- [ ] 4. 创建性能调优文档
  - 编写性能优化最佳实践
  - _需求: 4.1, 4.2, 4.3_

- [ ] 4.1 编写 docs/performance-tuning.md
  - 创建文档文件
  - 编写 ResultReader 优化章节
  - 编写查询设计最佳实践
  - 编写连接池配置指南
  - 编写批量操作优化
  - 编写内存分配优化
  - 编写数据库特定优化
  - 编写性能监控建议
  - 编写 async/await 最佳实践
  - _需求: 4.2, 4.3, 4.4, 4.5, 4.6, 4.7, 4.8, 4.9, 4.10_

- [ ] 4.2 添加性能基准测试示例
  - 创建 BenchmarkDotNet 示例
  - 展示如何测量查询性能
  - 提供性能对比数据
  - _需求: 4.6_

- [ ] 5. 创建故障排除文档
  - 编写常见问题解决指南
  - _需求: 8.1, 8.2, 8.3_

- [ ] 5.1 编写 docs/troubleshooting.md
  - 创建文档文件
  - 列出常见错误消息和解决方案
  - 提供源生成器调试技巧
  - 编写连接故障排除
  - 编写性能故障排除
  - 列出 AOT 编译问题和解决方案
  - 添加 FAQ 部分
  - 添加社区支持链接
  - _需求: 8.2, 8.3, 8.4, 8.5, 8.6, 8.7, 8.8_

- [ ] 6. 创建真实世界示例项目
  - 构建完整的 Web API 示例
  - _需求: 3.1, 3.2, 3.3_

- [ ] 6.1 创建项目结构
  - 创建 samples/RealWorldExample 目录
  - 创建 API、Core、Infrastructure 项目
  - 创建测试项目
  - 配置解决方案文件
  - _需求: 3.1_

- [ ] 6.2 实现核心实体和仓储
  - 创建 User、Order、Product 实体
  - 创建对应的仓储接口和实现
  - 使用 Sqlx 源生成器
  - _需求: 3.2_

- [ ] 6.3 实现 API 控制器
  - 创建 UsersController（CRUD 操作）
  - 创建 OrdersController（事务示例）
  - 创建 ProductsController
  - 实现分页、过滤、排序
  - _需求: 3.2, 3.9_

- [ ] 6.4 配置依赖注入
  - 在 Program.cs 中配置 Sqlx.AspNetCore
  - 注册所有仓储
  - 配置健康检查
  - 配置日志中间件
  - _需求: 3.3_

- [ ] 6.5 实现认证和授权
  - 添加 JWT 认证
  - 实现基于角色的授权
  - 保护 API 端点
  - _需求: 3.4_

- [ ] 6.6 实现事务管理
  - 在 OrdersController 中演示事务
  - 展示事务回滚
  - 展示嵌套事务
  - _需求: 3.5_

- [ ] 6.7 添加 Swagger/OpenAPI 文档
  - 配置 Swashbuckle
  - 添加 API 文档注释
  - 配置示例请求/响应
  - _需求: 3.6_

- [ ] 6.8 实现错误处理和验证
  - 创建全局异常处理器
  - 添加模型验证
  - 返回标准化错误响应
  - _需求: 3.7_

- [ ] 6.9 编写单元测试
  - 测试仓储方法
  - 测试业务逻辑
  - 使用内存数据库
  - _需求: 3.8_

- [ ] 6.10 编写集成测试
  - 测试 API 端点
  - 测试事务行为
  - 使用 TestContainers
  - _需求: 3.8_

- [ ] 6.11 添加 Docker 支持
  - 创建 Dockerfile
  - 创建 docker-compose.yml
  - 配置 PostgreSQL 容器
  - 测试容器化部署
  - _需求: 3.10_

- [ ] 6.12 编写示例 README
  - 创建 samples/RealWorldExample/README.md
  - 解释项目结构
  - 提供运行说明
  - 列出实现的功能
  - 提供 API 文档链接
  - _需求: 3.1_

- [ ] 7. 编写 Sqlx.AspNetCore 单元测试
  - 测试集成包的核心功能
  - _需求: 1.1, 1.2, 5.1_

- [ ] 7.1 测试 ServiceCollectionExtensions
  - 测试 AddSqlx() 方法
  - 测试 AddSqlxRepositories() 方法
  - 测试配置验证
  - _需求: 1.1, 1.2_

- [ ] 7.2 测试 SqlLoggingMiddleware
  - 测试 SQL 日志记录
  - 测试敏感数据掩码
  - 测试日志级别过滤
  - _需求: 1.6_

- [ ] 8. 更新主 README
  - 添加 Sqlx.AspNetCore 介绍
  - _需求: 1.1, 2.1_

- [ ] 8.1 更新 README.md
  - 在核心特性中添加 ASP.NET Core 集成
  - 添加快速开始链接
  - 添加文档链接
  - 添加示例项目链接
  - _需求: 1.1, 2.6_

- [ ] 9. 最终验证和文档审查
  - 确保所有功能正常工作
  - _需求: 所有_

- [ ] 9.1 运行所有测试
  - 运行 Sqlx.AspNetCore 单元测试
  - 运行真实示例集成测试
  - 确保所有测试通过
  - _需求: 所有_

- [ ] 9.2 审查所有文档
  - 检查文档完整性
  - 验证代码示例可运行
  - 检查链接有效性
  - 修正拼写和语法错误
  - _需求: 2.1, 4.1, 8.1_

- [ ] 9.3 性能验证
  - 验证 SQL 日志中间件开销 < 1ms
  - 运行性能基准测试
  - _需求: 性能需求_

- [ ] 9.4 创建发布检查清单
  - 确认所有功能已实现
  - 确认所有文档已完成
  - 确认所有测试通过
  - _需求: 所有_

## 注意事项

- 每个任务完成后应运行相关测试确保功能正常
- 文档应包含完整的代码示例，确保可运行
- 真实示例应展示最佳实践
- 性能优化应有基准测试数据支持
- 所有代码应遵循 Sqlx 编码规范
- 使用 StyleCop 和 EditorConfig 保持代码一致性
