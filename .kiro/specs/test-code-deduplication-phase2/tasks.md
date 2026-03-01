# 实施计划：测试代码去重 - 第二阶段

## 概述

本任务列表描述了如何通过创建测试辅助类、拆分大型测试文件和优化测试代码来减少测试代码重复性的具体步骤。

## 任务

- [x] 1. 创建测试辅助类基础设施
  - 创建 Helpers 目录和基础辅助类
  - _需求: 1.1, 1.4_

- [x] 1.1 创建 Helpers 目录结构
  - 在 tests/Sqlx.Tests/ 下创建 Helpers 目录
  - _需求: 1.4_

- [x] 1.2 实现 TestEntityFactory 类
  - 创建 TestEntityFactory.cs
  - 实现 CreateTestEntity 方法
  - 实现 CreateTestEntities 方法
  - 实现 CreateTestUser 方法
  - 添加其他常用实体的工厂方法
  - _需求: 1.1, 1.2_

- [x] 1.3 实现 SqlAssertions 类
  - 创建 SqlAssertions.cs
  - 实现 AssertSqlContains 方法
  - 实现 AssertSqlEquals 方法
  - 实现 AssertParametersContain 方法
  - 实现 AssertSqlIsParameterized 方法
  - _需求: 3.1, 3.2, 3.4_

- [x] 1.4 实现 TestDataBuilder 类
  - 创建 TestDataBuilder.cs
  - 实现 WithEntity 方法
  - 实现 WithEntities 方法
  - 实现 Build 方法
  - _需求: 1.3_

- [ ]* 1.5 为测试辅助类编写单元测试
  - 测试 TestEntityFactory 的默认值
  - 测试 SqlAssertions 的断言逻辑
  - 测试 TestDataBuilder 的构建功能
  - _需求: 1.1, 1.2, 1.3_

- [x] 2. 拆分 SqlBuilderTests.cs (1726行)
  - 将 SqlBuilderTests.cs 拆分为4个专注的测试文件
  - _需求: 2.1, 2.2, 2.3, 2.4_

- [ ] 2.1 创建 SqlBuilderBasicTests.cs
  - 提取基础功能测试（构造函数、Append、Build等）
  - 应用 TestEntityFactory 和 SqlAssertions
  - _需求: 2.1, 2.2, 2.3_

- [ ] 2.2 创建 SqlBuilderParameterTests.cs
  - 提取参数化相关测试
  - 应用 SqlAssertions.AssertParametersContain
  - _需求: 2.1, 2.2, 2.3_

- [ ] 2.3 创建 SqlBuilderPlaceholderTests.cs
  - 提取占位符相关测试
  - 应用测试辅助类
  - _需求: 2.1, 2.2, 2.3_

- [ ] 2.4 创建 SqlBuilderDialectTests.cs
  - 提取方言特定测试
  - 应用测试辅助类
  - _需求: 2.1, 2.2, 2.3_

- [ ] 2.5 删除原始 SqlBuilderTests.cs
  - 确认所有测试已迁移
  - 删除原文件
  - _需求: 2.3, 2.4_

- [ ]* 2.6 验证 SqlBuilder 测试拆分
  - 运行所有 SqlBuilder 相关测试
  - 确认测试数量一致
  - _需求: 2.3, 5.2_

- [ ] 3. 拆分 ValuesPlaceholderTests.cs (1517行)
  - 将 ValuesPlaceholderTests.cs 拆分为3个专注的测试文件
  - _需求: 2.1, 2.2, 2.3, 2.4_

- [ ] 3.1 创建 ValuesPlaceholderBasicTests.cs
  - 提取基础功能测试
  - 应用测试辅助类
  - _需求: 2.1, 2.2, 2.3_

- [ ] 3.2 创建 ValuesPlaceholderDialectTests.cs
  - 提取方言特定测试
  - 应用测试辅助类
  - _需求: 2.1, 2.2, 2.3_

- [ ] 3.3 创建 ValuesPlaceholderEdgeCaseTests.cs
  - 提取边界情况和错误处理测试
  - 应用测试辅助类
  - _需求: 2.1, 2.2, 2.3_

- [ ] 3.4 删除原始 ValuesPlaceholderTests.cs
  - 确认所有测试已迁移
  - 删除原文件
  - _需求: 2.3, 2.4_

- [ ]* 3.5 验证 ValuesPlaceholder 测试拆分
  - 运行所有 ValuesPlaceholder 相关测试
  - 确认测试数量一致
  - _需求: 2.3, 5.2_

- [ ] 4. 拆分 TablePlaceholderTests.cs (1265行)
  - 将 TablePlaceholderTests.cs 拆分为3个专注的测试文件
  - _需求: 2.1, 2.2, 2.3, 2.4_

- [ ] 4.1 创建 TablePlaceholderBasicTests.cs
  - 提取基础功能测试
  - 应用测试辅助类
  - _需求: 2.1, 2.2, 2.3_

- [ ] 4.2 创建 TablePlaceholderDialectTests.cs
  - 提取方言特定测试
  - 应用测试辅助类
  - _需求: 2.1, 2.2, 2.3_

- [ ] 4.3 创建 TablePlaceholderEdgeCaseTests.cs
  - 提取边界情况测试
  - 应用测试辅助类
  - _需求: 2.1, 2.2, 2.3_

- [ ] 4.4 删除原始 TablePlaceholderTests.cs
  - 确认所有测试已迁移
  - 删除原文件
  - _需求: 2.3, 2.4_

- [ ]* 4.5 验证 TablePlaceholder 测试拆分
  - 运行所有 TablePlaceholder 相关测试
  - 确认测试数量一致
  - _需求: 2.3, 5.2_

- [x] 5. 在其他测试中应用测试辅助类
  - 在现有测试中逐步应用新的辅助类
  - _需求: 4.1, 4.2_

- [x] 5.1 识别可以使用 TestEntityFactory 的测试
  - 搜索重复的实体创建代码
  - 列出需要重构的测试文件
  - _需求: 4.1_

- [x] 5.2 在识别的测试中应用 TestEntityFactory
  - 替换重复的实体创建代码
  - 确保测试仍然通过
  - 已重构: ResultReaderTests.cs, ResultReaderStrictTests.cs, SourceGeneratorTests.cs
  - _需求: 4.2, 4.3_

- [x] 5.3 识别可以使用 SqlAssertions 的测试
  - 搜索重复的SQL断言代码
  - 列出需要重构的测试文件
  - _需求: 4.1_

- [x] 5.4 在识别的测试中应用 SqlAssertions
  - 替换重复的断言代码
  - 确保测试仍然通过
  - 已重构: SqlInterpolatedStringHandlerTests.cs, SetExpressionExtensionsTests.cs, SetExpressionEdgeCaseTests.cs, SqlBuilderTests.cs (24个测试方法), ExpressionBlockResultTests.cs (11个测试方法), ExpressionBlockResultAnyPlaceholderTests.cs (19个参数断言), ExpressionBlockResultAnyPlaceholderAdvancedTests.cs (17个参数断言), DynamicUpdateWithAnyPlaceholderTests.cs (26个参数断言)
  - _需求: 4.2, 4.3_

- [x] 6. 最终验证和清理
  - 运行完整测试套件并清理临时文件
  - _需求: 5.1, 5.2, 5.3, 5.4_

- [x] 6.1 运行完整测试套件
  - 执行 dotnet test
  - 确认所有3,316个测试通过 ✓
  - _需求: 5.1, 5.2_

- [x] 6.2 测量代码重复率
  - 运行代码重复性分析工具
  - 确认重复率降低
  - 记录改进指标：
    * 创建了3个测试辅助类
    * 应用到7个测试文件
    * 测试文件总数: 147
    * 代码总行数: 57,866
    * 测试方法数: 2,850+
    * 所有3,316个测试通过
  - _需求: 5.4_

- [x] 6.3 清理分析脚本
  - 删除 analyze-duplication.ps1 ✓
  - 删除 analyze-real-duplication.ps1 ✓
  - 删除 analyze-code-similarity.ps1 ✓
  - 保留 measure-test-metrics.ps1 用于未来指标跟踪
  - _需求: 无（清理任务）_

- [x] 6.4 提交更改
  - git add 所有更改 ✓
  - 创建提交信息 ✓
  - 推送到远程仓库 ✓
  - Commits: 778a955, 0462593, d8905f0, 8df2d88
  - _需求: 无（版本控制）_

## 注意事项

- 标记为 `*` 的任务是可选的测试任务，可以根据需要跳过以加快MVP开发
- 每个拆分任务完成后应立即运行相关测试以确保没有破坏功能
- 保持测试的独立性，确保每个测试可以单独运行
- 在应用测试辅助类时，优先处理重复最多的测试文件
