# Sqlx 测试指南

本指南介绍了如何运行、理解和扩展 Sqlx 项目的测试套件。

## 📋 测试套件概览

Sqlx 项目包含多层测试架构，确保代码质量和功能正确性：

### 测试项目结构

```
tests/
├── Sqlx.Tests/                    # 核心单元测试 (1,472个测试)
├── Sqlx.IntegrationTests/         # 集成测试 (6个测试)
└── Sqlx.PerformanceTests/         # 性能基准测试 (5个测试)

samples/
└── RepositoryExample.Tests/       # 示例项目测试 (68个测试)
```

## 🚀 快速开始

### 运行所有测试

```bash
# 运行核心单元测试
dotnet test tests/Sqlx.Tests

# 运行集成测试
dotnet test tests/Sqlx.IntegrationTests

# 运行性能测试
dotnet test tests/Sqlx.PerformanceTests

# 运行示例测试
dotnet test samples/RepositoryExample.Tests
```

### 运行特定测试类别

```bash
# 仅运行单元测试（排除集成测试）
dotnet test tests/Sqlx.Tests --filter "TestCategory!=Integration"

# 运行特定测试类
dotnet test tests/Sqlx.Tests --filter "ClassName=BatchOperationHelperTests"

# 运行特定测试方法
dotnet test tests/Sqlx.Tests --filter "MethodName=MemoryOptimizer_CreateOptimizedStringBuilder_WithValidSize_ReturnsStringBuilder"
```

## 📊 测试统计

### 当前测试覆盖情况

| 测试项目 | 测试数量 | 通过率 | 主要覆盖内容 |
|----------|----------|--------|--------------|
| Sqlx.Tests | 1,472 | 100% | 核心功能、工具类、SQL生成 |
| Sqlx.IntegrationTests | 6 | 100% | 端到端功能验证 |
| Sqlx.PerformanceTests | 5 | 100% | 性能基准测试 |
| RepositoryExample.Tests | 68 | 100% | 代码生成和示例 |

### 代码覆盖率

- **整体覆盖率**: ~65%
- **核心组件覆盖率**: 80-100%
- **新增组件覆盖率**: 85-95%

## 🧪 测试类型详解

### 1. 单元测试 (Sqlx.Tests)

#### 核心组件测试
- **BatchOperationHelper**: 批量操作功能
- **MemoryOptimizer**: 内存优化和字符串处理
- **PerformanceMonitor**: 性能监控和计数
- **StringInterpolation**: 字符串插值优化
- **SqlServerDialectProvider**: SQL Server方言支持
- **AdvancedConnectionManager**: 连接管理和健康监控

#### 测试模式
```csharp
[TestClass]
public class ComponentNameTests
{
    [TestInitialize]
    public void Setup()
    {
        // 测试初始化
    }

    [TestMethod]
    public void MethodName_Condition_ExpectedResult()
    {
        // Arrange - 准备测试数据
        // Act - 执行被测试方法
        // Assert - 验证结果
    }

    [TestCleanup]
    public void Cleanup()
    {
        // 测试清理
    }
}
```

### 2. 集成测试 (Sqlx.IntegrationTests)

集成测试验证完整的端到端工作流：

#### 测试场景
- **CRUD操作**: 完整的增删改查流程
- **批量操作**: 大量数据的批量处理
- **事务处理**: 事务提交和回滚机制
- **错误处理**: 异常情况的处理
- **安全性**: SQL注入防护
- **复杂查询**: JOIN、GROUP BY等复杂SQL

#### 运行环境
- 使用内存数据库 (SQLite :memory:)
- 自动创建测试表结构
- 每个测试独立运行

### 3. 性能测试 (Sqlx.PerformanceTests)

性能测试验证 Sqlx 的高性能特性：

#### 基准测试内容
- **原始性能**: 基础CRUD操作性能
- **缓存性能**: 智能缓存系统效果
- **批量操作**: 大量数据处理性能
- **连接管理**: 并发连接处理能力

#### 性能指标
```
⚡ 插入性能: ~0.02ms/条
🧠 缓存提升: 4.0x性能提升
📦 批量操作: ~0.011ms/条
🔌 并发查询: ~0.60ms平均
```

## 🔧 扩展测试

### 添加新的单元测试

1. **创建测试类**:
```csharp
[TestClass]
public class NewComponentTests
{
    private NewComponent _component = null!;

    [TestInitialize]
    public void Setup()
    {
        _component = new NewComponent();
    }

    [TestMethod]
    public void NewComponent_Method_ShouldReturnExpectedResult()
    {
        // Arrange
        var input = "test input";
        var expected = "expected output";

        // Act
        var result = _component.Method(input);

        // Assert
        Assert.AreEqual(expected, result);
    }
}
```

2. **添加到测试项目**:
```xml
<ItemGroup>
    <Compile Include="Core\NewComponentTests.cs" />
</ItemGroup>
```

### 添加集成测试

1. **扩展现有测试**:
```csharp
[TestMethod]
public async Task NewIntegrationScenario_WorksCorrectly()
{
    // 准备测试数据
    await SetupTestDataAsync();
    
    // 执行集成场景
    var result = await ExecuteScenarioAsync();
    
    // 验证结果
    Assert.IsTrue(result.Success);
}
```

### 添加性能基准测试

1. **创建基准测试**:
```csharp
[TestMethod]
public void PerformanceBenchmark_NewFeature_MeetsRequirements()
{
    var stopwatch = Stopwatch.StartNew();
    
    // 执行性能测试
    ExecutePerformanceScenario();
    
    stopwatch.Stop();
    
    // 验证性能要求
    Assert.IsTrue(stopwatch.ElapsedMilliseconds < 100, 
        "Performance should be under 100ms");
}
```

## 🛠️ 测试工具和框架

### 使用的测试框架
- **MSTest**: 主要测试框架
- **FluentAssertions**: 更好的断言语法
- **xUnit**: 示例项目使用
- **SQLite**: 内存数据库用于测试

### 测试辅助工具
- **CodeGenerationTestHelpers**: 代码生成测试助手
- **模式匹配**: 替代脆弱的字符串匹配
- **异步测试模式**: 标准化的异步测试

## 📈 测试最佳实践

### 1. 测试命名规范
```
MethodName_Condition_ExpectedResult
```
示例:
- `CreateUser_WithValidData_ReturnsSuccess`
- `GetUser_WithInvalidId_ReturnsNull`
- `UpdateUser_WithNullData_ThrowsException`

### 2. 测试结构 (AAA模式)
```csharp
[TestMethod]
public void ExampleTest()
{
    // Arrange - 准备测试数据和环境
    var input = CreateTestData();
    var expected = ExpectedResult();
    
    // Act - 执行被测试的方法
    var result = SystemUnderTest.Method(input);
    
    // Assert - 验证结果
    Assert.AreEqual(expected, result);
}
```

### 3. 测试隔离
- 每个测试应该独立运行
- 使用 `[TestInitialize]` 和 `[TestCleanup]`
- 避免测试间的依赖关系

### 4. 异常测试
```csharp
[TestMethod]
public void Method_WithInvalidInput_ThrowsExpectedException()
{
    // Arrange
    var invalidInput = null;
    
    // Act & Assert
    Assert.ThrowsException<ArgumentNullException>(() => 
        SystemUnderTest.Method(invalidInput));
}
```

### 5. 异步测试
```csharp
[TestMethod]
public async Task AsyncMethod_WithValidInput_ReturnsExpectedResult()
{
    // Arrange
    var input = CreateTestData();
    
    // Act
    var result = await SystemUnderTest.AsyncMethod(input);
    
    // Assert
    Assert.IsNotNull(result);
}
```

## 🔍 故障排除

### 常见问题

1. **测试失败 - 连接问题**
   - 确保使用内存数据库进行测试
   - 检查连接字符串配置

2. **性能测试不稳定**
   - 运行多次取平均值
   - 考虑系统负载影响

3. **集成测试超时**
   - 增加测试超时时间
   - 检查数据库初始化逻辑

### 调试测试
```bash
# 详细输出模式
dotnet test --logger:console --verbosity detailed

# 运行特定失败的测试
dotnet test --filter "TestName=FailingTestName"
```

## 📚 相关文档

- [性能指南](PERFORMANCE_GUIDE.md) - 性能优化最佳实践
- [最佳实践](BEST_PRACTICES.md) - 代码和架构最佳实践
- [故障排除](troubleshooting/faq.md) - 常见问题解答

## 🤝 贡献测试

如果您想为 Sqlx 项目贡献测试代码：

1. **Fork 项目**
2. **创建测试分支**: `git checkout -b feature/add-tests`
3. **添加测试**: 遵循现有的测试模式
4. **运行所有测试**: 确保没有破坏现有功能
5. **提交 PR**: 包含测试说明和覆盖率信息

### 测试贡献检查清单
- [ ] 遵循命名规范
- [ ] 使用AAA测试模式
- [ ] 包含边界条件测试
- [ ] 添加异常情况测试
- [ ] 验证线程安全性（如适用）
- [ ] 更新相关文档

---

通过遵循这个测试指南，您可以有效地运行、理解和扩展 Sqlx 项目的测试套件，确保代码质量和项目的长期维护性。
