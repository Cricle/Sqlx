# 实现计划：多结果集支持

## 概述

实现通过元组返回值支持多结果集的功能，使用 `[ResultSetMapping]` 特性明确指定结果集映射关系。

## 任务

- [x] 1. 创建 ResultSetMappingAttribute 特性
  - 创建 `src/Sqlx/Annotations/ResultSetMappingAttribute.cs`
  - 定义 Index 和 Name 属性
  - 添加参数验证（Index >= 0, Name 非空）
  - 设置 AttributeUsage（Method, AllowMultiple=true, Inherited=false）
  - _需求: 2.2, 2.3_

- [ ]* 1.1 为 ResultSetMappingAttribute 编写单元测试
  - 测试构造函数参数验证
  - 测试属性值正确性
  - 测试 AttributeUsage 设置
  - 测试多个特性应用
  - _需求: 2.2_

- [x] 2. 扩展 RepositoryGenerator 支持元组返回类型检测
  - 在 RepositoryGenerator.cs 中添加 `IsTupleReturnType` 方法
  - 检测 ValueTuple<...> 类型
  - 提取元组元素类型和名称
  - 处理命名元组和匿名元组
  - _需求: 2.1_

- [ ]* 2.1 为元组类型检测编写单元测试
  - 测试 ValueTuple<int, string> 检测
  - 测试命名元组检测
  - 测试非元组类型返回 false
  - 测试嵌套元组（应返回 false）
  - _需求: 2.1_

- [x] 3. 实现 ResultSetMapping 特性解析
  - 在 RepositoryGenerator.cs 中添加 `ParseResultSetMappings` 方法
  - 从方法符号中读取所有 ResultSetMappingAttribute
  - 构建索引到名称的映射字典
  - 验证映射的完整性和正确性
  - _需求: 2.2, 2.7_

- [ ]* 3.1 为特性解析编写单元测试
  - 测试单个映射解析
  - 测试多个映射解析
  - 测试映射索引重复检测
  - 测试映射索引不连续警告
  - _需求: 2.2_

- [x] 4. 实现默认映射规则
  - 在 RepositoryGenerator.cs 中添加 `GetDefaultMappings` 方法
  - 当没有 ResultSetMapping 时生成默认映射
  - 第1个元素映射到受影响行数
  - 其余元素按顺序映射到结果集
  - _需求: 2.8_

- [x] 5. 生成同步方法的多结果集读取代码
  - 修改 RepositoryGenerator.cs 的代码生成逻辑
  - 检测元组返回类型
  - 生成 ExecuteNonQuery 调用（索引0）
  - 生成 ExecuteReader 调用
  - 生成 reader.Read() 和 reader.NextResult() 循环
  - 生成类型转换代码（使用 GetInt32, GetInt64, GetString 等）
  - 生成元组构造代码
  - _需求: 2.1, 2.3, 2.4, 2.5_

- [ ]* 5.1 为同步方法生成代码编写集成测试
  - 测试单结果集返回
  - 测试多结果集返回
  - 测试类型转换
  - 测试错误处理（空结果集）
  - _需求: 2.1, 2.5_

- [x] 6. 生成异步方法的多结果集读取代码
  - 修改 RepositoryGenerator.cs 的异步代码生成逻辑
  - 生成 ExecuteNonQueryAsync 调用
  - 生成 ExecuteReaderAsync 调用
  - 生成 reader.ReadAsync() 和 reader.NextResultAsync() 循环
  - 添加 CancellationToken 支持
  - 添加 ConfigureAwait(false)
  - _需求: 2.1, 2.3, 2.4, 2.5_

- [ ]* 6.1 为异步方法生成代码编写集成测试
  - 测试单结果集返回
  - 测试多结果集返回
  - 测试 CancellationToken 传递
  - 测试错误处理
  - _需求: 2.1, 2.5_

- [x] 7. 实现混合场景（输出参数 + 元组返回）
  - 修改代码生成逻辑同时支持输出参数和元组返回
  - 确保输出参数在元组返回之后处理
  - 生成正确的参数绑定代码
  - _需求: 4.1, 4.2, 4.3, 4.4_

- [ ]* 7.1 为混合场景编写集成测试
  - 测试同步方法：out 参数 + 元组返回
  - 测试同步方法：ref 参数 + 元组返回
  - 测试异步方法：OutputParameter<T> + 元组返回
  - 测试多个输出参数 + 元组返回
  - _需求: 4.1, 4.2, 4.3, 4.4_

- [x] 8. 添加编译时验证
  - 验证元组元素数量与映射数量匹配
  - 验证映射索引不重复
  - 验证映射名称在元组中存在
  - 生成编译警告或错误
  - _需求: 2.6, 2.7_

- [x] 9. 添加运行时错误处理
  - 生成空结果集检查代码
  - 生成结果集数量检查代码
  - 生成类型转换异常处理代码
  - 添加有意义的错误消息
  - _需求: 2.9_

- [x] 10. 创建示例代码
  - 创建 `samples/MultipleResultSetsExample.cs`
  - 演示单结果集多列返回
  - 演示多结果集返回
  - 演示混合场景（输出参数 + 元组）
  - 演示默认映射和显式映射
  - _需求: 所有_

- [x] 11. 更新文档
  - 更新 `docs/output-parameters.md` 添加多结果集章节
  - 创建 `docs/multiple-result-sets.md` 专门文档
  - 更新 `README.md` 添加示例
  - 更新 `docs/api-reference.md` 添加 API 说明
  - _需求: 所有_

- [x] 12. Checkpoint - 确保所有测试通过
  - 运行所有单元测试
  - 运行所有集成测试
  - 确保没有编译警告
  - 确保代码覆盖率达标

## 注意事项

- 任务标记 `*` 的为可选测试任务，可以跳过以加快 MVP 开发
- 每个任务都引用了对应的需求编号
- 建议按顺序执行任务，因为后续任务依赖前面的任务
- Checkpoint 任务用于验证阶段性成果

## 实现优先级

1. 高优先级：任务 1-6（核心功能）
2. 中优先级：任务 7-9（混合场景和错误处理）
3. 低优先级：任务 10-11（示例和文档）
