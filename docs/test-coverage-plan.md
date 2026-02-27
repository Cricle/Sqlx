# 测试覆盖率提升计划

## 目标

实现 100% 严格高质量行覆盖率的单元测试

## 当前状态

- 总测试数：3077个
- 单元测试：2691个
- E2E测试：386个
- 测试通过率：100%

## 覆盖率提升策略

### 阶段 1: 分析当前覆盖率

1. **生成覆盖率报告**
   ```bash
   ./generate-coverage-report.ps1
   ```

2. **识别未覆盖的代码**
   - 核心库 (src/Sqlx)
   - 源生成器 (src/Sqlx.Generator)

3. **分类未覆盖代码**
   - 正常执行路径
   - 异常处理路径
   - 边界条件
   - 防御性代码

### 阶段 2: 核心库覆盖率提升

#### 2.1 SqlBuilder 类

**未覆盖场景：**
- [ ] 空参数处理
- [ ] 特殊字符转义
- [ ] 大量参数场景
- [ ] 嵌套子查询
- [ ] 参数名冲突处理

**测试策略：**
```csharp
[TestClass]
public class SqlBuilderCoverageTests
{
    [TestMethod]
    public void Append_WithNullValue_HandlesCorrectly() { }
    
    [TestMethod]
    public void Append_WithSpecialCharacters_EscapesCorrectly() { }
    
    [TestMethod]
    public void Append_WithManyParameters_GeneratesUniqueNames() { }
}
```

#### 2.2 SqlQuery 类

**未覆盖场景：**
- [ ] 复杂表达式树
- [ ] 多层嵌套查询
- [ ] 所有 LINQ 操作符组合
- [ ] 类型转换边界
- [ ] NULL 传播

**测试策略：**
```csharp
[TestClass]
public class SqlQueryCoverageTests
{
    [TestMethod]
    public void Where_WithComplexExpression_TranslatesCorrectly() { }
    
    [TestMethod]
    public void Select_WithNestedProjection_HandlesCorrectly() { }
}
```

#### 2.3 SqlDialect 类

**未覆盖场景：**
- [ ] 所有数据库方言的所有方法
- [ ] 边界值处理
- [ ] 特殊字符处理
- [ ] 保留字处理

**测试策略：**
```csharp
[TestClass]
public class SqlDialectCoverageTests
{
    [TestMethod]
    public void WrapColumn_WithReservedWord_EscapesCorrectly() { }
    
    [TestMethod]
    public void FormatValue_WithBoundaryValue_HandlesCorrectly() { }
}
```

#### 2.4 异常处理

**未覆盖场景：**
- [ ] SqlxException 所有构造函数
- [ ] 异常消息格式化
- [ ] 异常上下文信息
- [ ] 重试逻辑所有路径

**测试策略：**
```csharp
[TestClass]
public class ExceptionHandlingCoverageTests
{
    [TestMethod]
    public void SqlxException_WithAllParameters_CreatesCorrectly() { }
    
    [TestMethod]
    public void RetryPolicy_WithTransientError_RetriesCorrectly() { }
}
```

### 阶段 3: 源生成器覆盖率提升

#### 3.1 代码生成逻辑

**未覆盖场景：**
- [ ] 所有特性组合
- [ ] 边界情况
- [ ] 错误诊断
- [ ] 代码格式化

**测试策略：**
```csharp
[TestClass]
public class GeneratorCoverageTests
{
    [TestMethod]
    public void Generate_WithAllAttributes_GeneratesCorrectCode() { }
    
    [TestMethod]
    public void Generate_WithInvalidInput_ReportsDiagnostic() { }
}
```

### 阶段 4: 边界条件和异常路径

#### 4.1 输入验证

**测试场景：**
- [ ] NULL 输入
- [ ] 空字符串
- [ ] 空集合
- [ ] 超长字符串
- [ ] 特殊字符
- [ ] Unicode 字符

#### 4.2 数值边界

**测试场景：**
- [ ] Int32.MinValue / MaxValue
- [ ] Int64.MinValue / MaxValue
- [ ] Decimal 边界值
- [ ] DateTime 边界值
- [ ] 零值
- [ ] 负值

#### 4.3 并发场景

**测试场景：**
- [ ] 多线程访问
- [ ] 竞态条件
- [ ] 死锁场景
- [ ] 资源竞争

### 阶段 5: 集成测试补充

#### 5.1 数据库特定功能

**测试场景：**
- [ ] MySQL 特有功能
- [ ] PostgreSQL 特有功能
- [ ] SQL Server 特有功能
- [ ] SQLite 特有功能

#### 5.2 性能边界

**测试场景：**
- [ ] 大数据量查询
- [ ] 深层嵌套查询
- [ ] 复杂 JOIN
- [ ] 大量参数

## 实施计划

### 第 1 周：分析和规划

- [ ] 生成详细覆盖率报告
- [ ] 识别所有未覆盖代码
- [ ] 创建详细测试清单
- [ ] 优先级排序

### 第 2-3 周：核心功能覆盖

- [ ] SqlBuilder 100% 覆盖
- [ ] SqlQuery 100% 覆盖
- [ ] SqlDialect 100% 覆盖
- [ ] 异常处理 100% 覆盖

### 第 4 周：源生成器覆盖

- [ ] 代码生成逻辑 100% 覆盖
- [ ] 诊断报告 100% 覆盖
- [ ] 特性处理 100% 覆盖

### 第 5 周：边界和异常

- [ ] 所有边界条件测试
- [ ] 所有异常路径测试
- [ ] 并发场景测试

### 第 6 周：验证和优化

- [ ] 验证 100% 覆盖率
- [ ] 测试质量审查
- [ ] 性能优化
- [ ] 文档更新

## 质量标准

### 测试质量要求

1. **可读性**
   - 清晰的测试名称
   - 详细的注释
   - AAA 模式（Arrange-Act-Assert）

2. **独立性**
   - 测试之间无依赖
   - 可以任意顺序运行
   - 可以并行执行

3. **可维护性**
   - 避免重复代码
   - 使用辅助方法
   - 清晰的测试数据

4. **完整性**
   - 覆盖所有代码路径
   - 测试正常和异常情况
   - 验证边界条件

### 覆盖率指标

- **行覆盖率**: 100%
- **分支覆盖率**: 100%
- **方法覆盖率**: 100%
- **类覆盖率**: 100%

## 工具和资源

### 覆盖率工具

- **Coverlet**: 收集覆盖率数据
- **ReportGenerator**: 生成HTML报告
- **dotCover**: JetBrains 覆盖率工具（可选）

### 安装命令

```bash
# 安装 ReportGenerator
dotnet tool install -g dotnet-reportgenerator-globaltool

# 生成覆盖率报告
./generate-coverage-report.ps1
```

### CI/CD 集成

```yaml
# .github/workflows/ci-cd.yml
- name: Run tests with coverage
  run: |
    dotnet test --collect:"XPlat Code Coverage" \
                --results-directory ./TestResults

- name: Generate coverage report
  run: |
    reportgenerator \
      -reports:"**/coverage.cobertura.xml" \
      -targetdir:"./CoverageReport" \
      -reporttypes:"Html;Badges"

- name: Upload coverage to Codacy
  uses: codacy/codacy-coverage-reporter-action@v1
  with:
    project-token: ${{ secrets.CODACY_PROJECT_TOKEN }}
    coverage-reports: "**/coverage.cobertura.xml"
```

## 注意事项

### 不应追求覆盖的代码

1. **自动生成的代码**
   - 源生成器生成的代码
   - 编译器生成的代码

2. **第三方库代码**
   - NuGet 包
   - 外部依赖

3. **不可测试的代码**
   - 某些平台特定代码
   - 某些硬件相关代码

### 覆盖率排除配置

```xml
<!-- coverlet.runsettings -->
<RunSettings>
  <DataCollectionRunSettings>
    <DataCollectors>
      <DataCollector friendlyName="XPlat Code Coverage">
        <Configuration>
          <Exclude>
            [*.Tests]*
            [*]*.Generated.*
          </Exclude>
        </Configuration>
      </DataCollector>
    </DataCollectors>
  </DataCollectionRunSettings>
</RunSettings>
```

## 进度跟踪

| 模块 | 当前覆盖率 | 目标覆盖率 | 状态 | 负责人 | 完成日期 |
|------|-----------|-----------|------|--------|---------|
| SqlBuilder | TBD | 100% | 🔴 待开始 | - | - |
| SqlQuery | TBD | 100% | 🔴 待开始 | - | - |
| SqlDialect | TBD | 100% | 🔴 待开始 | - | - |
| Exceptions | TBD | 100% | 🔴 待开始 | - | - |
| Generator | TBD | 100% | 🔴 待开始 | - | - |

## 参考资源

- [Microsoft 测试最佳实践](https://docs.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices)
- [xUnit 文档](https://xunit.net/)
- [Coverlet 文档](https://github.com/coverlet-coverage/coverlet)
- [ReportGenerator 文档](https://github.com/danielpalme/ReportGenerator)
