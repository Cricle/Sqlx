// -----------------------------------------------------------------------
// <copyright file="SeamlessIntegrationDemo.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Sqlx;
using SqlxDemo.Models;

namespace SqlxDemo.Services
{
    /// <summary>
    /// SqlTemplate与ExpressionToSql无缝集成演示
    /// 展示AOT友好、高性能、零反射的完美结合
    /// </summary>
    public class SeamlessIntegrationDemo
    {
        private readonly SqlDialect _dialect;

        public SeamlessIntegrationDemo(SqlDialect? dialect = null)
        {
            _dialect = dialect ?? SqlDefine.SqlServer;
        }

        #region 基础集成演示

        /// <summary>
        /// 演示1：表达式到模板的无缝转换
        /// </summary>
        public void DemoExpressionToTemplate()
        {
            Console.WriteLine("=== 演示1：表达式到模板转换 ===");

            // 使用表达式构建查询
            var expression = ExpressionToSql<User>.ForSqlServer()
                .Select(u => new { u.Id, u.Name, u.Email })
                .Where(u => u.IsActive && u.Age > 18)
                .OrderBy(u => u.Id)
                .Take(20);

            // 零拷贝转换为模板
            var template = expression.ToTemplate();

            Console.WriteLine($"生成的SQL: {template.Sql}");
            Console.WriteLine($"参数数量: {template.Parameters.Count}");
            
            // 模板可以重复使用，修改参数
            var renderedSql = template.Render(new Dictionary<string, object?>
            {
                ["p0"] = true,  // IsActive
                ["p1"] = 25     // Age > 25
            });
            
            Console.WriteLine($"渲染后SQL: {renderedSql}");
        }

        /// <summary>
        /// 演示2：新设计 - 纯模板定义和参数绑定分离
        /// </summary>
        public void DemoTemplateToExpression()
        {
            Console.WriteLine("\n=== 演示2：纯模板设计 ===");

            // 1. 创建纯模板定义（推荐新方式）
            var template = SqlTemplate.Parse("SELECT Id, Name, Email FROM Users WHERE IsActive = @isActive");
            
            Console.WriteLine($"模板定义: {template.Sql}");
            Console.WriteLine($"是否为纯模板: {template.IsPureTemplate}");

            // 2. 执行时绑定参数 - 可重复使用同一模板
            var execution1 = template.Execute(new { isActive = true });
            var execution2 = template.Execute(new { isActive = false });

            Console.WriteLine($"活跃用户查询: {execution1.Render()}");
            Console.WriteLine($"非活跃用户查询: {execution2.Render()}");

            // 3. 流式参数绑定（另一种方式）
            var execution3 = template.Bind()
                .Param("isActive", true)
                .Build();

            Console.WriteLine($"流式绑定结果: {execution3.Render()}");
        }

        /// <summary>
        /// 演示3：集成构建器的强大功能
        /// </summary>
        public void DemoIntegratedBuilder()
        {
            Console.WriteLine("\n=== 演示3：集成构建器 ===");

            using var builder = SqlTemplateExpressionBridge.Create<User>(_dialect);

            var template = builder
                // 使用智能列选择
                .SmartSelect(ColumnSelectionMode.OptimizedForQuery)
                // 使用表达式WHERE
                .Where(u => u.IsActive)
                // 使用模板片段
                .Template(" AND Department IN ('IT', 'Sales')")
                // 条件性添加
                .TemplateIf(true, " AND Id >= @startId")
                .Parameter("startId", 1000)
                // 使用表达式排序
                .OrderBy(u => u.Id)
                .Take(50)
                .Build();

            Console.WriteLine($"集成构建的SQL: {template.Sql}");
            Console.WriteLine($"参数: {string.Join(", ", template.Parameters.Select(p => $"{p.Key}={p.Value}"))}");
        }

        #endregion

        #region 高级场景演示

        /// <summary>
        /// 演示4：动态查询构建
        /// </summary>
        public SqlTemplate DemoDynamicQuery(UserSearchCriteria criteria)
        {
            Console.WriteLine("\n=== 演示4：动态查询构建 ===");

            using var builder = FluentSqlBuilder.Query<User>(_dialect);

            return builder
                .SmartSelect(ColumnSelectionMode.OptimizedForQuery)
                // 动态WHERE条件
                .DynamicWhere(
                    criteria.UseExactMatch,
                    u => u.Name == criteria.Name!, // 表达式方式
                    $"Name LIKE '%{criteria.Name}%'" // 模板方式
                )
                // 条件性过滤
                .TemplateIf(criteria.MinAge.HasValue, "AND Age >= @minAge")
                .TemplateIf(criteria.MaxAge.HasValue, "AND Age <= @maxAge")
                .TemplateIf(criteria.DepartmentIds?.Length > 0, 
                    $"AND DepartmentId IN ({string.Join(",", criteria.DepartmentIds ?? Array.Empty<int>())})")
                // 参数绑定
                .Parameter("minAge", criteria.MinAge)
                .Parameter("maxAge", criteria.MaxAge)
                // 动态排序
                .TemplateIf(!string.IsNullOrEmpty(criteria.SortBy),
                    $"ORDER BY {criteria.SortBy} {(criteria.SortDescending ? "DESC" : "ASC")}")
                // 分页
                .Skip(criteria.PageSize * (criteria.PageNumber - 1))
                .Take(criteria.PageSize)
                .Build();
        }

        /// <summary>
        /// 演示5：批量操作优化
        /// </summary>
        public SqlTemplate DemoBatchOperations(List<User> users)
        {
            Console.WriteLine("\n=== 演示5：批量操作优化 ===");

            using var builder = SqlTemplateExpressionBridge.Create<User>(_dialect);

            // 动态选择批量策略
            var useBulkInsert = users.Count > 1000;

            if (useBulkInsert)
            {
                return builder
                    .Template("BULK INSERT Users FROM @dataSource WITH (FIELDTERMINATOR = ',', ROWTERMINATOR = '\\n')")
                    .Parameter("dataSource", ConvertToCsv(users))
                    .Build();
            }
            else
            {
                var valuesParts = users.Select((u, i) => 
                    $"(@name{i}, @email{i}, @age{i}, @isActive{i})");

                var template = builder
                    .Template($"INSERT INTO Users (Name, Email, Age, IsActive) VALUES {string.Join(", ", valuesParts)}")
                    .Build();

                // 添加批量参数
                var templateWithParams = template;
                for (int i = 0; i < users.Count; i++)
                {
                    var user = users[i];
                    templateWithParams = templateWithParams.WithParameters(new
                    {
                        name = user.Name,
                        email = user.Email,
                        age = user.Age,
                        isActive = user.IsActive
                    });
                }

                return templateWithParams;
            }
        }

        /// <summary>
        /// 演示6：复杂JOIN查询
        /// </summary>
        public SqlTemplate DemoComplexJoinQuery(JoinQueryOptions options)
        {
            Console.WriteLine("\n=== 演示6：复杂JOIN查询 ===");

            using var smartBuilder = FluentSqlBuilder.SmartQuery<User>(_dialect);

            return smartBuilder
                // 基础SELECT
                .AddIf(true, "SELECT u.Id, u.Name, u.Email")
                // 条件性JOIN和字段
                .AddIf(options.IncludeProfile, ", p.Avatar, p.Bio")
                .AddIf(options.IncludeDepartment, ", d.Name as DepartmentName")
                .AddIf(options.IncludeManager, ", m.Name as ManagerName")
                // FROM clause
                .AddIf(true, "FROM Users u")
                // 条件性JOIN
                .AddIf(options.IncludeProfile, 
                    "LEFT JOIN UserProfiles p ON u.Id = p.UserId")
                .AddIf(options.IncludeDepartment,
                    "LEFT JOIN Departments d ON u.DepartmentId = d.Id")
                .AddIf(options.IncludeManager && options.IncludeDepartment,
                    "LEFT JOIN Users m ON d.ManagerId = m.Id")
                // 条件性WHERE
                .WhereIf(options.ActiveOnly, u => u.IsActive)
                .AddIf(options.MinDate.HasValue,
                    "AND u.CreatedAt >= @minDate", new { minDate = options.MinDate })
                .Build();
        }

        /// <summary>
        /// 演示7：纯模板重用和性能优化
        /// </summary>
        public void DemoPrecompiledTemplates()
        {
            Console.WriteLine("\n=== 演示7：纯模板重用和性能优化 ===");

            // 1. 创建纯模板定义
            var baseTemplate = SqlTemplate.Parse("SELECT Id, Name, Email FROM Users WHERE IsActive = @isActive AND Age > @minAge ORDER BY CreatedAt DESC");
            
            Console.WriteLine($"模板定义: {baseTemplate.Sql}");
            Console.WriteLine($"是否为纯模板: {baseTemplate.IsPureTemplate}");

            // 预编译以获得最佳性能
            var compiled = baseTemplate.Precompile();

            // 高性能执行 - 使用缓存优化
            var scenarios = new[]
            {
                new { columns = "Id, Name, Email", isActive = true, minAge = 18, orderBy = "CreatedAt DESC" },
                new { columns = "Id, Name", isActive = true, minAge = 25, orderBy = "Name ASC" },
                new { columns = "*", isActive = false, minAge = 0, orderBy = "UpdatedAt DESC" }
            };

            foreach (var scenario in scenarios)
            {
                var sql = compiled.Execute(scenario);
                Console.WriteLine($"场景SQL: {sql}");
            }
        }

        /// <summary>
        /// 演示8：性能监控和优化
        /// </summary>
        public void DemoPerformanceMonitoring()
        {
            Console.WriteLine("\n=== 演示8：性能监控 ===");

            var startTime = DateTime.UtcNow;

            // 执行查询并记录指标
            using var builder = FluentSqlBuilder.Query<User>(_dialect);
            var template = builder
                .SmartSelect(ColumnSelectionMode.BasicFieldsOnly)
                .Where(u => u.IsActive)
                .OrderBy(u => u.Id)
                .Take(100)
                .Build();

            var executionTime = DateTime.UtcNow - startTime;

            // 记录性能指标
            SqlTemplateMetrics.RecordMetric(
                "UserQuery",
                executionTime,
                template.Sql.Length,
                template.Parameters.Count
            );

            // 获取性能报告
            var metrics = SqlTemplateMetrics.GetMetrics();
            foreach (var metric in metrics)
            {
                Console.WriteLine($"操作: {metric.Key}");
                Console.WriteLine($"  平均执行时间: {metric.Value.AverageExecutionTime.TotalMilliseconds}ms");
                Console.WriteLine($"  平均SQL长度: {metric.Value.AverageSqlLength}");
                Console.WriteLine($"  平均参数数量: {metric.Value.AverageParameterCount}");
                Console.WriteLine($"  执行次数: {metric.Value.ExecutionCount}");
            }
        }

        #endregion

        #region 实际业务场景

        /// <summary>
        /// 业务场景1：用户管理系统查询
        /// </summary>
        public async Task<List<User>> GetUsersForAdminPanelAsync(AdminPanelFilter filter)
        {
            using var builder = FluentSqlBuilder.Query<User>(_dialect);

            var template = builder
                // 智能列选择 - 排除敏感信息
                .SelectByPattern("*", false)
                .ExcludeColumns("PasswordHash", "SecurityToken")
                // 复杂筛选条件
                .Where(u => u.IsActive)
                .TemplateIf(filter.HasRoleFilter, "AND RoleId IN @roleIds")
                .TemplateIf(filter.HasDateRange, "AND CreatedAt BETWEEN @startDate AND @endDate")
                .TemplateIf(!string.IsNullOrEmpty(filter.SearchTerm), 
                    "AND (Name LIKE @searchPattern OR Email LIKE @searchPattern)")
                // 参数绑定
                .Parameter("roleIds", filter.RoleIds)
                .Parameter("startDate", filter.StartDate)
                .Parameter("endDate", filter.EndDate)
                .Parameter("searchPattern", $"%{filter.SearchTerm}%")
                // 排序和分页
                .Template($"ORDER BY {filter.SortColumn} {(filter.SortDescending ? "DESC" : "ASC")}")
                .Skip(filter.PageSize * (filter.PageNumber - 1))
                .Take(filter.PageSize)
                .Build();

            // 这里会调用实际的数据库查询
            Console.WriteLine($"管理面板查询SQL: {template.Sql}");
            return new List<User>(); // 模拟返回
        }

        /// <summary>
        /// 业务场景2：报表数据聚合
        /// </summary>
        public async Task<List<UserStatistics>> GenerateUserStatisticsAsync(StatisticsRequest request)
        {
            using var builder = SqlTemplateExpressionBridge.Create<User>(_dialect);

            var template = builder
                .Template(@"
                    SELECT 
                        DATEPART(month, CreatedAt) as Month,
                        COUNT(*) as TotalUsers,
                        COUNT(CASE WHEN IsActive = 1 THEN 1 END) as ActiveUsers,
                        AVG(CAST(Age as DECIMAL(5,2))) as AverageAge")
                .TemplateIf(request.IncludeDepartmentStats, 
                    ", DepartmentId, COUNT(DISTINCT DepartmentId) as DepartmentCount")
                .Template("FROM Users")
                .Template("WHERE CreatedAt >= @startDate AND CreatedAt <= @endDate")
                .Parameter("startDate", request.StartDate)
                .Parameter("endDate", request.EndDate)
                .Template("GROUP BY DATEPART(month, CreatedAt)")
                .TemplateIf(request.IncludeDepartmentStats, ", DepartmentId")
                .Template("ORDER BY Month")
                .Build();

            Console.WriteLine($"统计报表SQL: {template.Sql}");
            return new List<UserStatistics>(); // 模拟返回
        }

        #endregion

        #region 辅助方法和类型

        private static string ConvertToCsv(List<User> users)
        {
            // 简单的CSV转换示例
            return string.Join("\n", users.Select(u => $"{u.Name},{u.Email},{u.Age},{u.IsActive}"));
        }

        /// <summary>
        /// 用户搜索条件
        /// </summary>
        public class UserSearchCriteria
        {
            public string? Name { get; set; }
            public bool UseExactMatch { get; set; }
            public int? MinAge { get; set; }
            public int? MaxAge { get; set; }
            public int[]? DepartmentIds { get; set; }
            public string SortBy { get; set; } = "CreatedAt";
            public bool SortDescending { get; set; } = true;
            public int PageSize { get; set; } = 20;
            public int PageNumber { get; set; } = 1;
        }

        /// <summary>
        /// JOIN查询选项
        /// </summary>
        public class JoinQueryOptions
        {
            public bool IncludeProfile { get; set; }
            public bool IncludeDepartment { get; set; }
            public bool IncludeManager { get; set; }
            public bool ActiveOnly { get; set; } = true;
            public DateTime? MinDate { get; set; }
        }

        /// <summary>
        /// 管理面板筛选器
        /// </summary>
        public class AdminPanelFilter
        {
            public string? SearchTerm { get; set; }
            public int[]? RoleIds { get; set; }
            public DateTime? StartDate { get; set; }
            public DateTime? EndDate { get; set; }
            public string SortColumn { get; set; } = "CreatedAt";
            public bool SortDescending { get; set; } = true;
            public int PageSize { get; set; } = 50;
            public int PageNumber { get; set; } = 1;

            public bool HasRoleFilter => RoleIds?.Length > 0;
            public bool HasDateRange => StartDate.HasValue && EndDate.HasValue;
        }

        /// <summary>
        /// 统计请求
        /// </summary>
        public class StatisticsRequest
        {
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public bool IncludeDepartmentStats { get; set; }
            public string GroupBy { get; set; } = "month";
        }

        /// <summary>
        /// 用户统计结果
        /// </summary>
        public class UserStatistics
        {
            public int Month { get; set; }
            public int TotalUsers { get; set; }
            public int ActiveUsers { get; set; }
            public decimal AverageAge { get; set; }
            public int? DepartmentId { get; set; }
            public int? DepartmentCount { get; set; }
        }

        #endregion

        /// <summary>
        /// 运行所有演示
        /// </summary>
        public void RunAllDemos()
        {
            Console.WriteLine("🚀 SqlTemplate与ExpressionToSql无缝集成演示");
            Console.WriteLine(new string('=', 60));

            try
            {
                DemoExpressionToTemplate();
                DemoTemplateToExpression();
                DemoIntegratedBuilder();

                var criteria = new UserSearchCriteria
                {
                    Name = "John",
                    UseExactMatch = false,
                    MinAge = 25,
                    DepartmentIds = new[] { 1, 2, 3 },
                    PageSize = 20
                };
                var dynamicTemplate = DemoDynamicQuery(criteria);
                Console.WriteLine($"\n动态查询结果: {dynamicTemplate.Sql}");

                DemoPrecompiledTemplates();
                DemoPerformanceMonitoring();

                Console.WriteLine("\n✅ 所有演示完成！");
                Console.WriteLine("\n🎯 关键特性:");
                Console.WriteLine("• AOT友好 - 零反射，原生编译支持");
                Console.WriteLine("• 高性能 - 零拷贝转换，内存优化");
                Console.WriteLine("• 代码整洁 - 流畅API，类型安全");
                Console.WriteLine("• 扩展性强 - 插件式架构，易于扩展");
                Console.WriteLine("• 使用方便 - 统一API，学习成本低");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 演示过程中出现错误: {ex.Message}");
            }
        }
    }
}
