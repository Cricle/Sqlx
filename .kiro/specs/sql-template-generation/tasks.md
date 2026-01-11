# Implementation Plan: SQL Template Generation

## Overview

本实现计划采用 TDD（测试驱动开发）方法，通过小步迭代的方式实现基于返回类型的 SQL 生成功能。每个任务都遵循"先写测试，再实现功能"的原则，确保代码质量和正确性。

## Tasks

- [x] 1. 设置测试基础设施
  - 创建测试项目结构
  - 配置测试框架（xUnit/NUnit）
  - 添加必要的测试辅助类
  - _Requirements: 8.5_

- [x] 2. 实现返回类型检测（TDD 第一轮）
  - [x] 2.1 编写返回类型检测测试
    - 测试 SqlTemplate 返回类型识别
    - 测试其他返回类型不受影响
    - 测试异步方法返回 Task<SqlTemplate>
    - _Requirements: 1.1, 1.3_

  - [x] 2.2 实现 SqlTemplate 返回类型检测
    - 在 `ReturnTypes` 枚举中添加 `SqlTemplate`
    - 在 `GetReturnType()` 中添加 SqlTemplate 检测逻辑
    - 处理 Task<SqlTemplate> 的情况
    - _Requirements: 1.1, 1.3_

  - [x] 2.3 验证测试通过
    - 运行所有测试
    - 确保新测试通过
    - 确保现有测试不受影响
    - _Requirements: 8.1, 8.2_

- [x] 3. 实现简单 SQL 生成（TDD 第二轮）
  - [x] 3.1 编写简单 SQL 生成测试
    - 测试无参数查询的 SQL 生成
    - 测试 SQL 字符串正确性
    - 测试空 SQL 的错误处理
    - _Requirements: 1.2, 1.4_

  - [x] 3.2 实现 GenerateSqlTemplateReturn 方法
    - 在 `DeclareCommand()` 中添加 SqlTemplate 分支
    - 创建 `GenerateSqlTemplateReturn()` 方法
    - 复用 `GetSql()` 获取 SQL 字符串
    - 返回 SqlTemplate 对象
    - _Requirements: 1.2, 1.4, 6.1_

  - [x] 3.3 验证测试通过
    - 运行所有测试
    - 确保 SQL 生成正确
    - _Requirements: 1.2_

- [x] 4. 实现参数字典构建（TDD 第三轮）
  - [x] 4.1 编写参数字典测试
    - 测试标量参数添加到字典
    - 测试复杂对象参数展开
    - 测试参数名称正确性
    - 测试参数值正确性
    - _Requirements: 1.2, 1.5, 4.1, 4.4_

  - [x] 4.2 实现 AddParameterToDictionary 方法
    - 创建 `AddParameterToDictionary()` 辅助方法
    - 处理标量类型参数
    - 处理复杂对象参数（展开属性）
    - 使用正确的参数名称（带前缀）
    - _Requirements: 1.2, 1.5, 4.1, 4.4_

  - [x] 4.3 集成参数字典到 GenerateSqlTemplateReturn
    - 在 `GenerateSqlTemplateReturn()` 中创建参数字典
    - 遍历 SqlParameters 添加参数
    - 返回包含参数的 SqlTemplate
    - _Requirements: 1.2, 1.5_

  - [x] 4.4 验证测试通过
    - 运行所有测试
    - 确保参数字典正确构建
    - _Requirements: 1.2, 1.5_

- [x] 5. 实现批量操作支持（TDD 第四轮）
  - [x] 5.1 编写批量插入测试
    - 测试 VALUES_PLACEHOLDER 检测
    - 测试集合参数识别
    - 测试批量 SQL 生成
    - 测试批量参数字典
    - _Requirements: 2.5_

  - [x] 5.2 实现 GenerateBatchInsertSqlTemplate 方法
    - 创建 `GenerateBatchInsertSqlTemplate()` 方法
    - 检测并处理 VALUES_PLACEHOLDER
    - 识别集合参数
    - 使用 StringBuilder 构建批量 SQL
    - 为每个元素生成参数
    - _Requirements: 2.5_

  - [x] 5.3 集成批量操作到 GenerateSqlTemplateReturn
    - 在 `GenerateSqlTemplateReturn()` 中检测批量操作
    - 调用 `GenerateBatchInsertSqlTemplate()`
    - _Requirements: 2.5_

  - [x] 5.4 验证测试通过
    - 运行所有测试
    - 确保批量操作正确
    - _Requirements: 2.5_

- [x] 6. 验证方言支持（TDD 第五轮）
  - [x] 6.1 编写方言测试
    - 测试 SQL Server 方言（[] 和 @）
    - 测试 MySQL 方言（`` 和 @）
    - 测试 PostgreSQL 方言（"" 和 $）
    - 测试 SQLite 方言（[] 和 @）
    - _Requirements: 5.1, 5.2, 5.3, 5.4_

  - [x] 6.2 验证方言逻辑复用
    - 确认 `SqlDef` 正确传递
    - 确认参数前缀正确使用
    - 确认列名包装正确
    - _Requirements: 5.1, 5.2, 5.3, 6.1, 6.2_

  - [x] 6.3 验证测试通过
    - 运行所有方言测试
    - 确保所有方言正确支持
    - _Requirements: 5.1, 5.2, 5.3, 5.4_

- [ ] 7. 编写属性测试（TDD 第六轮）
  - [ ] 7.1 编写 SQL 一致性属性测试
    - **Property 1: SQL 一致性**
    - **Validates: Requirements 1.4, 4.1, 4.4**
    - 对于任意方法定义和参数，SqlTemplate 版本和执行版本的 SQL 应该相同

  - [ ] 7.2 编写参数完整性属性测试
    - **Property 2: 参数完整性**
    - **Validates: Requirements 1.2, 1.5, 3.2, 3.3**
    - 对于任意参数组合，生成的字典应该包含所有参数

  - [ ] 7.3 编写类型安全性属性测试
    - **Property 3: 类型安全性**
    - **Validates: Requirements 6.1, 6.2**
    - 对于任意参数类型不匹配，编译器应该报告错误

  - [ ] 7.4 编写方言一致性属性测试
    - **Property 4: 方言一致性**
    - **Validates: Requirements 5.1, 5.2, 5.3**
    - 对于任意方言配置，生成的 SQL 应该使用正确的语法

  - [ ] 7.5 编写无副作用属性测试
    - **Property 5: 无副作用**
    - **Validates: Requirements 1.1, 1.5**
    - 对于任意 SqlTemplate 方法，调用不应该产生副作用

  - [ ] 7.6 编写批量操作属性测试
    - **Property 6: 批量操作支持**
    - **Validates: Requirements 2.5**
    - 对于任意批量插入，生成的 SQL 和参数应该完整

  - [ ] 7.7 编写向后兼容性属性测试
    - **Property 7: 向后兼容性**
    - **Validates: Requirements 8.1, 8.2, 8.3**
    - 对于任意现有方法，行为应该保持不变

  - [ ] 7.8 运行所有属性测试
    - 运行所有属性测试（最少 100 次迭代）
    - 修复发现的问题
    - 确保所有属性都满足

- [ ] 8. 集成测试和端到端验证
  - [x] 8.1 创建完整的测试 Repository
    - 定义包含 SqlTemplate 方法的接口
    - 定义对应的执行方法
    - 使用不同的方言配置
    - _Requirements: 1.1, 2.1, 2.2, 2.3, 2.4_

  - [x] 8.2 编写端到端测试
    - 测试 SqlTemplate 方法调用
    - 测试 SQL 和参数正确性
    - 测试 Render() 生成可执行 SQL
    - _Requirements: 1.2, 1.5_

  - [x] 8.3 编写集成测试
    - 测试 SqlTemplate 和执行方法共存
    - 测试复杂场景（多参数、批量操作）
    - _Requirements: 8.1, 8.2, 8.3_

  - [x] 8.4 验证所有测试通过
    - 运行完整的测试套件
    - 确保所有测试通过
    - 检查代码覆盖率
    - _Requirements: 8.5_

- [x] 9. 文档和示例
  - [x] 9.1 更新 API 文档
    - 添加 SqlTemplate 返回类型说明
    - 添加使用示例
    - 添加最佳实践建议
    - _Requirements: 7.3, 7.4_

  - [x] 9.2 创建示例项目
    - 创建简单的示例 Repository
    - 展示 SqlTemplate 和执行模式对比
    - 展示调试场景
    - _Requirements: 7.1, 7.2_

  - [x] 9.3 更新 README
    - 添加功能说明
    - 添加快速开始指南
    - _Requirements: 7.1, 7.2_

- [x] 10. 最终验证和发布准备
  - [x] 运行完整的测试套件 (3260 tests, 3250 passed, 10 skipped)
  - [x] 检查代码质量（StyleCop、分析器）- 无警告
  - [x] 验证向后兼容性 - 所有现有测试通过
  - [x] 准备发布说明 - 文档已完成

## Notes

- 所有测试任务都遵循 TDD 原则：先写测试，再实现功能
- 属性测试配置为最少 100 次迭代
- 每个任务完成后都要运行测试，确保没有破坏现有功能
- 代码生成逻辑复用现有的 SqlGenerator、GenerateContext 等
- 不修改现有的 SqlTemplate、ParameterizedSql 类
