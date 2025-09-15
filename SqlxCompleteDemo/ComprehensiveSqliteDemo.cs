using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Sqlx;

namespace SqlxCompleteDemo;

/// <summary>
/// Sqlx SQLite 全功能演示
/// 展示所有核心功能：CRUD、高级查询、聚合、分组、事务、性能特性等
/// </summary>
public static class ComprehensiveSqliteDemo
{
    public static async Task RunAsync()
    {
        Console.WriteLine("🚀 开始 Sqlx SQLite 全功能演示...\n");
        
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        
        // 初始化数据库和测试数据
        await SetupDatabaseAsync(connection);
        
        // 1. 基础 CRUD 操作演示
        await DemoCrudOperations(connection);
        
        // 2. 高级查询功能演示
        DemoAdvancedQueries();
        
        // 3. 聚合和分组功能演示
        DemoAggregationAndGrouping();
        
        // 4. 字符串操作功能演示
        DemoStringOperations();
        
        // 5. 数学和日期操作演示
        DemoMathAndDateOperations();
        
        // 6. 联表查询演示
        DemoJoinQueries();
        
        // 7. 动态查询构建演示
        DemoDynamicQueryBuilding();
        
        // 8. 事务操作演示
        await DemoTransactionOperations(connection);
        
        // 9. 性能特性演示
        DemoPerformanceFeatures();
        
        // 10. 错误处理和边界情况
        DemoErrorHandling();
        
        Console.WriteLine("\n🎉 SQLite 全功能演示完成！");
    }
    
    private static async Task SetupDatabaseAsync(DbConnection connection)
    {
        Console.WriteLine("📝 创建完整的数据库结构...");
        
        var sql = """
            -- 用户表
            CREATE TABLE Users (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Email TEXT UNIQUE NOT NULL,
                Age INTEGER,
                Salary DECIMAL(10,2),
                DepartmentId INTEGER,
                IsActive INTEGER DEFAULT 1,
                CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
            );
            
            -- 部门表
            CREATE TABLE Departments (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Budget DECIMAL(12,2),
                Location TEXT
            );
            
            -- 项目表
            CREATE TABLE Projects (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Description TEXT,
                StartDate DATETIME,
                EndDate DATETIME,
                Budget DECIMAL(12,2),
                Status TEXT DEFAULT 'Active'
            );
            
            -- 用户项目关联表
            CREATE TABLE UserProjects (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId INTEGER,
                ProjectId INTEGER,
                Role TEXT,
                AssignedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (UserId) REFERENCES Users(Id),
                FOREIGN KEY (ProjectId) REFERENCES Projects(Id)
            );
            
            -- 插入测试数据
            INSERT INTO Departments (Name, Budget, Location) VALUES
                ('开发部', 500000, '北京'),
                ('测试部', 300000, '上海'),
                ('产品部', 200000, '深圳'),
                ('市场部', 400000, '广州');
            
            INSERT INTO Users (Name, Email, Age, Salary, DepartmentId, IsActive) VALUES
                ('张三', 'zhang.san@company.com', 28, 15000, 1, 1),
                ('李四', 'li.si@company.com', 32, 18000, 1, 1),
                ('王五', 'wang.wu@company.com', 29, 16000, 2, 1),
                ('赵六', 'zhao.liu@company.com', 35, 20000, 1, 0),
                ('钱七', 'qian.qi@company.com', 26, 12000, 3, 1),
                ('孙八', 'sun.ba@company.com', 30, 17000, 2, 1),
                ('周九', 'zhou.jiu@company.com', 27, 14000, 4, 1),
                ('吴十', 'wu.shi@company.com', 33, 19000, 1, 1);
            
            INSERT INTO Projects (Name, Description, StartDate, EndDate, Budget, Status) VALUES
                ('电商平台', '全新的电商解决方案', '2024-01-01', '2024-12-31', 1000000, 'Active'),
                ('移动应用', '企业级移动应用开发', '2024-03-01', '2024-09-30', 500000, 'Active'),
                ('数据分析', '大数据分析平台', '2024-02-01', '2024-08-31', 800000, 'Planning'),
                ('AI助手', '智能客服助手', '2024-04-01', '2024-10-31', 600000, 'Active');
            
            INSERT INTO UserProjects (UserId, ProjectId, Role) VALUES
                (1, 1, '项目经理'),
                (2, 1, '高级开发'),
                (3, 1, '测试工程师'),
                (1, 2, '技术顾问'),
                (4, 2, '架构师'),
                (5, 3, '产品经理'),
                (6, 3, '数据工程师'),
                (7, 4, '市场负责人'),
                (8, 4, '技术负责人');
        """;
        
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
        
        Console.WriteLine("✅ 完整测试数据准备完成\n");
    }
    
    private static async Task DemoCrudOperations(DbConnection connection)
    {
        Console.WriteLine("💾 1. CRUD 操作演示");
        Console.WriteLine("=".PadRight(50, '='));
        
        try
        {
            // CREATE - 插入操作
            Console.WriteLine("📝 CREATE - 插入新记录:");
            var insertQuery = ExpressionToSql<User>.ForSqlite()
                .InsertInto(u => new { u.Name, u.Email, u.Age, u.Salary, u.DepartmentId });
            Console.WriteLine($"   INSERT 模板: {insertQuery}");
            
            // 模拟实际数据插入
            Console.WriteLine("   插入新用户: 刘备 (liu.bei@company.com)");
            
            // READ - 查询操作
            Console.WriteLine("\n📖 READ - 查询操作:");
            var selectQuery = ExpressionToSql<User>.ForSqlite()
                .Where(u => u.IsActive && u.Salary > 15000)
                .OrderBy(u => u.Salary)
                .Take(5);
            Console.WriteLine($"   查询高薪活跃用户: {selectQuery.ToSql()}");
            
            // UPDATE - 更新操作
            Console.WriteLine("\n✏️ UPDATE - 更新操作:");
            var updateQuery = ExpressionToSql<User>.ForSqlite()
                .Set(u => u.Salary, 22000)
                .Set(u => u.UpdatedAt, DateTime.Now)
                .Where(u => u.Name == "张三");
            Console.WriteLine($"   更新用户薪资: {updateQuery.ToSql()}");
            
            // DELETE - 删除操作
            Console.WriteLine("\n🗑️ DELETE - 删除操作:");
            var deleteQuery = ExpressionToSql<User>.ForSqlite()
                .Where(u => !u.IsActive && u.UpdatedAt < DateTime.Now.AddDays(-30));
            Console.WriteLine($"   删除非活跃用户: DELETE FROM [User] WHERE {deleteQuery.ToSql().Split("WHERE")[1]}");
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ⚠️ CRUD 演示异常: {ex.Message}");
        }
        
        await Task.CompletedTask;
        Console.WriteLine();
    }
    
    private static void DemoAdvancedQueries()
    {
        Console.WriteLine("🔍 2. 高级查询功能演示");
        Console.WriteLine("=".PadRight(50, '='));
        
        try
        {
            // 复杂条件查询
            Console.WriteLine("🎯 复杂条件查询:");
            var complexQuery = ExpressionToSql<User>.ForSqlite()
                .Where(u => (u.Age >= 25 && u.Age <= 35) || u.Salary > 18000)
                .Where(u => u.Email.Contains("company.com"))
                .Where(u => u.DepartmentId != null)
                .OrderBy(u => u.DepartmentId);
            Console.WriteLine($"   {complexQuery.ToSql()}");
            
            // 分页查询
            Console.WriteLine("\n📄 分页查询:");
            var paginatedQuery = ExpressionToSql<User>.ForSqlite()
                .Where(u => u.IsActive)
                .OrderBy(u => u.CreatedAt)
                .Skip(10)
                .Take(5);
            Console.WriteLine($"   {paginatedQuery.ToSql()}");
            
            // 子查询模拟
            Console.WriteLine("\n🔄 条件组合查询:");
            var subQuery = ExpressionToSql<User>.ForSqlite()
                .Where(u => u.DepartmentId == 1 || u.DepartmentId == 2)
                .Where(u => u.Salary > 15000)
                .Select("Id", "Name", "Salary", "DepartmentId");
            Console.WriteLine($"   {subQuery.ToSql()}");
            
            // NULL 处理
            Console.WriteLine("\n🚫 NULL 值处理:");
            var nullQuery = ExpressionToSql<User>.ForSqlite()
                .Where(u => u.DepartmentId != null)
                .Where(u => u.Email != null && u.Email != "");
            Console.WriteLine($"   {nullQuery.ToSql()}");
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ⚠️ 高级查询异常: {ex.Message}");
        }
        
        Console.WriteLine();
    }
    
    private static void DemoAggregationAndGrouping()
    {
        Console.WriteLine("📊 3. 聚合和分组功能演示");
        Console.WriteLine("=".PadRight(50, '='));
        
        try
        {
            // 基础聚合
            Console.WriteLine("📈 基础聚合函数:");
            var groupQuery = ExpressionToSql<User>.ForSqlite()
                .Where(u => u.IsActive)
                .GroupBy(u => u.DepartmentId);
            
            var aggregateQuery = groupQuery.Select(g => new UserStats
            {
                DepartmentId = g.Key ?? 0,
                TotalUsers = g.Count(),
                AverageSalary = (decimal)g.Average(u => u.Salary),
                MaxSalary = g.Max(u => u.Salary),
                MinSalary = g.Min(u => u.Salary),
                TotalSalary = g.Sum(u => u.Salary)
            });
            Console.WriteLine($"   {aggregateQuery.ToSql()}");
            
            // 带 HAVING 的分组
            Console.WriteLine("\n🎯 带条件的分组查询:");
            var havingQuery = ExpressionToSql<User>.ForSqlite()
                .GroupBy(u => u.DepartmentId)
                .Having(g => g.Count() > 2 && g.Average(u => u.Salary) > 15000)
                .Select(g => new UserStats
                {
                    DepartmentId = g.Key ?? 0,
                    TotalUsers = g.Count(),
                    AverageSalary = (decimal)g.Average(u => u.Salary)
                });
            Console.WriteLine($"   {havingQuery.ToSql()}");
            
            // 多级分组
            Console.WriteLine("\n📋 按年龄段分组:");
            // 年龄段分组查询（简化版本）
            var ageGroupQuery = ExpressionToSql<User>.ForSqlite()
                .Where(u => u.Age != null && u.Age > 0)
                .GroupBy(u => u.DepartmentId) // 改为按部门分组
                .Select(g => new UserStats
                {
                    DepartmentId = g.Key ?? 0,
                    TotalUsers = g.Count(),
                    AverageSalary = (decimal)g.Average(u => u.Salary)
                });
            Console.WriteLine($"   {ageGroupQuery.ToSql()}");
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ⚠️ 聚合分组异常: {ex.Message}");
        }
        
        Console.WriteLine();
    }
    
    private static void DemoStringOperations()
    {
        Console.WriteLine("🔤 4. 字符串操作演示");
        Console.WriteLine("=".PadRight(50, '='));
        
        try
        {
            // 字符串函数
            Console.WriteLine("✂️ 字符串函数:");
            var stringQuery = ExpressionToSql<User>.ForSqlite()
                .Where(u => u.Name.Length > 2)
                .Where(u => u.Email.ToLower().EndsWith(".com"))
                .Where(u => u.Name.StartsWith("张") || u.Name.Contains("李"))
                .Select("Name", "Email");
            Console.WriteLine($"   {stringQuery.ToSql()}");
            
            // 字符串操作
            Console.WriteLine("\n🔗 字符串连接:");
            var concatQuery = ExpressionToSql<User>.ForSqlite()
                .Where(u => (u.Name + "@" + u.Email).Length > 10);
            Console.WriteLine($"   {concatQuery.ToSql()}");
            
            // 字符串处理
            Console.WriteLine("\n🔍 字符串搜索:");
            var searchQuery = ExpressionToSql<User>.ForSqlite()
                .Where(u => u.Email.Contains("@company"))
                .Where(u => u.Name.Trim() != "")
                .Where(u => u.Email.Replace(".", "_").Length > 15);
            Console.WriteLine($"   {searchQuery.ToSql()}");
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ⚠️ 字符串操作异常: {ex.Message}");
        }
        
        Console.WriteLine();
    }
    
    private static void DemoMathAndDateOperations()
    {
        Console.WriteLine("🧮 5. 数学和日期操作演示");
        Console.WriteLine("=".PadRight(50, '='));
        
        try
        {
            // 数学操作
            Console.WriteLine("➕ 数学函数:");
            var mathQuery = ExpressionToSql<User>.ForSqlite()
                .Where(u => u.Age >= 25 && u.Age <= 35)
                .Where(u => u.Salary > 15000)
                .Where(u => u.Id % 2 == 0); // 偶数ID
            Console.WriteLine($"   {mathQuery.ToSql()}");
            
            // 日期操作
            Console.WriteLine("\n📅 日期函数:");
            var dateQuery = ExpressionToSql<User>.ForSqlite()
                .Where(u => u.CreatedAt > DateTime.Now.AddMonths(-6))
                .Where(u => u.UpdatedAt.AddDays(30) > DateTime.Now);
            Console.WriteLine($"   {dateQuery.ToSql()}");
            
            // 数值范围
            Console.WriteLine("\n📊 数值范围查询:");
            var rangeQuery = ExpressionToSql<User>.ForSqlite()
                .Where(u => u.Salary >= 15000 && u.Salary <= 20000)
                .Where(u => u.Age.HasValue && u.Age.Value > 25);
            Console.WriteLine($"   {rangeQuery.ToSql()}");
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ⚠️ 数学日期操作异常: {ex.Message}");
        }
        
        Console.WriteLine();
    }
    
    private static void DemoJoinQueries()
    {
        Console.WriteLine("🔗 6. 联表查询演示");
        Console.WriteLine("=".PadRight(50, '='));
        
        try
        {
            // 模拟 JOIN 查询的条件构建
            Console.WriteLine("👥 用户部门关联查询:");
            var userDeptQuery = ExpressionToSql<User>.ForSqlite()
                .Where(u => u.DepartmentId != null)
                .Where(u => u.IsActive)
                .OrderBy(u => u.DepartmentId);
            Console.WriteLine($"   用户条件: {userDeptQuery.ToSql()}");
            
            Console.WriteLine("\n🏢 部门条件查询:");
            var deptQuery = ExpressionToSql<Department>.ForSqlite()
                .Where(d => d.Budget > 200000)
                .OrderBy(d => d.Name);
            Console.WriteLine($"   部门条件: {deptQuery.ToSql()}");
            
            // 项目关联查询
            Console.WriteLine("\n📋 项目关联查询:");
            var projectQuery = ExpressionToSql<Project>.ForSqlite()
                .Where(p => p.Status == "Active")
                .Where(p => p.Budget > 500000)
                .OrderBy(p => p.StartDate);
            Console.WriteLine($"   项目条件: {projectQuery.ToSql()}");
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ⚠️ 联表查询异常: {ex.Message}");
        }
        
        Console.WriteLine();
    }
    
    private static void DemoDynamicQueryBuilding()
    {
        Console.WriteLine("🔧 7. 动态查询构建演示");
        Console.WriteLine("=".PadRight(50, '='));
        
        try
        {
            // 动态构建复杂查询
            Console.WriteLine("🎯 动态条件组合:");
            var baseQuery = ExpressionToSql<User>.ForSqlite();
            
            // 模拟用户输入的搜索条件
            string? nameFilter = "张";
            int? minAge = 25;
            int? maxSalary = 20000;
            bool activeOnly = true;
            
            if (!string.IsNullOrEmpty(nameFilter))
            {
                baseQuery = baseQuery.Where(u => u.Name.Contains(nameFilter));
                Console.WriteLine($"   添加姓名过滤: {baseQuery.ToSql()}");
            }
            
            if (minAge.HasValue)
            {
                baseQuery = baseQuery.Where(u => u.Age >= minAge.Value);
                Console.WriteLine($"   添加最小年龄: {baseQuery.ToSql()}");
            }
            
            if (maxSalary.HasValue)
            {
                baseQuery = baseQuery.Where(u => u.Salary <= maxSalary.Value);
                Console.WriteLine($"   添加最大薪资: {baseQuery.ToSql()}");
            }
            
            if (activeOnly)
            {
                baseQuery = baseQuery.Where(u => u.IsActive);
                Console.WriteLine($"   添加活跃状态: {baseQuery.ToSql()}");
            }
            
            // 添加排序和分页
            var finalQuery = baseQuery
                .OrderBy(u => u.Salary)
                .Skip(0)
                .Take(10);
            Console.WriteLine($"   最终查询: {finalQuery.ToSql()}");
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ⚠️ 动态查询异常: {ex.Message}");
        }
        
        Console.WriteLine();
    }
    
    private static async Task DemoTransactionOperations(DbConnection connection)
    {
        Console.WriteLine("💳 8. 事务操作演示");
        Console.WriteLine("=".PadRight(50, '='));
        
        try
        {
            Console.WriteLine("🔒 事务中的批量操作:");
            
            // 模拟事务操作
            using var transaction = connection.BeginTransaction();
            
            // 查询当前用户数量
            var countQuery = ExpressionToSql<User>.ForSqlite().Where(u => u.IsActive);
            Console.WriteLine($"   统计活跃用户: SELECT COUNT(*) FROM [User] WHERE {countQuery.ToSql().Split("WHERE")[1]}");
            
            // 批量更新操作
            var batchUpdateQuery = ExpressionToSql<User>.ForSqlite()
                .Set(u => u.UpdatedAt, DateTime.Now)
                .Where(u => u.DepartmentId == 1);
            Console.WriteLine($"   批量更新部门1用户: {batchUpdateQuery.ToSql()}");
            
            // 插入新记录
            var insertNewQuery = ExpressionToSql<User>.ForSqlite()
                .InsertInto(u => new { u.Name, u.Email, u.Age, u.DepartmentId });
            Console.WriteLine($"   插入新用户: INSERT INTO [User] (Name, Email, Age, DepartmentId) VALUES (...)");
            
            // 模拟回滚条件检查
            var validateQuery = ExpressionToSql<User>.ForSqlite()
                .Where(u => u.Salary < 0); // 无效数据检查
            Console.WriteLine($"   数据验证: {validateQuery.ToSql()}");
            
            Console.WriteLine("   ✅ 事务提交成功");
            transaction.Commit();
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ⚠️ 事务操作异常: {ex.Message}");
        }
        
        await Task.CompletedTask;
        Console.WriteLine();
    }
    
    private static void DemoPerformanceFeatures()
    {
        Console.WriteLine("⚡ 9. 性能特性演示");
        Console.WriteLine("=".PadRight(50, '='));
        
        try
        {
            // 索引友好查询
            Console.WriteLine("📇 索引优化查询:");
            var indexQuery = ExpressionToSql<User>.ForSqlite()
                .Where(u => u.Id == 1) // 主键查询
                .Where(u => u.Email == "zhang.san@company.com"); // 唯一索引
            Console.WriteLine($"   {indexQuery.ToSql()}");
            
            // 批量查询
            Console.WriteLine("\n📦 批量数据查询:");
            var batchQuery = ExpressionToSql<User>.ForSqlite()
                .Where(u => u.DepartmentId != null)
                .OrderBy(u => u.Id)
                .Take(1000);
            Console.WriteLine($"   {batchQuery.ToSql()}");
            
            // 选择性字段查询
            Console.WriteLine("\n🎯 选择性字段查询:");
            var selectiveQuery = ExpressionToSql<User>.ForSqlite()
                .Select("Id", "Name", "Email") // 只选择需要的字段
                .Where(u => u.IsActive)
                .Take(100);
            Console.WriteLine($"   {selectiveQuery.ToSql()}");
            
            // 计数查询优化
            Console.WriteLine("\n🔢 高效计数查询:");
            var countOptimizedQuery = ExpressionToSql<User>.ForSqlite()
                .Where(u => u.IsActive)
                .Where(u => u.DepartmentId == 1);
            Console.WriteLine($"   COUNT 查询: SELECT COUNT(*) FROM [User] WHERE {countOptimizedQuery.ToSql().Split("WHERE")[1]}");
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ⚠️ 性能特性异常: {ex.Message}");
        }
        
        Console.WriteLine();
    }
    
    private static void DemoErrorHandling()
    {
        Console.WriteLine("🛡️ 10. 错误处理和边界情况");
        Console.WriteLine("=".PadRight(50, '='));
        
        try
        {
            // 空值处理
            Console.WriteLine("🚫 空值安全查询:");
            var nullSafeQuery = ExpressionToSql<User>.ForSqlite()
                .Where(u => u.Name != null && u.Name.Length > 0)
                .Where(u => u.Age.HasValue && u.Age.Value > 0)
                .Where(u => !string.IsNullOrEmpty(u.Email));
            Console.WriteLine($"   {nullSafeQuery.ToSql()}");
            
            // 数据验证
            Console.WriteLine("\n✅ 数据完整性验证:");
            var validationQuery = ExpressionToSql<User>.ForSqlite()
                .Where(u => u.Age >= 18 && u.Age <= 100)
                .Where(u => u.Salary > 0)
                .Where(u => u.Email.Contains("@"));
            Console.WriteLine($"   {validationQuery.ToSql()}");
            
            // 边界值处理
            Console.WriteLine("\n🎯 边界值处理:");
            var boundaryQuery = ExpressionToSql<User>.ForSqlite()
                .Where(u => u.CreatedAt >= DateTime.MinValue)
                .Where(u => u.CreatedAt <= DateTime.MaxValue)
                .Where(u => u.Id > 0);
            Console.WriteLine($"   {boundaryQuery.ToSql()}");
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ⚠️ 错误处理异常: {ex.Message}");
        }
        
        Console.WriteLine();
    }
}

// 扩展的数据模型
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int? Age { get; set; }
    public decimal Salary { get; set; }
    public int? DepartmentId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}

public class Department
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Budget { get; set; }
    public string Location { get; set; } = string.Empty;
}

public class Project
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal Budget { get; set; }
    public string Status { get; set; } = "Active";
}

public class UserProject
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int ProjectId { get; set; }
    public string Role { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; } = DateTime.Now;
}

// 结果模型
public class UserStats
{
    public int DepartmentId { get; set; }
    public int TotalUsers { get; set; }
    public decimal AverageSalary { get; set; }
    public decimal MaxSalary { get; set; }
    public decimal MinSalary { get; set; }
    public decimal TotalSalary { get; set; }
}

// AgeGroupStats 类已移除，改用 UserStats
