using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Data.Sqlite;
using System.Data;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Sqlx.Annotations;

namespace Sqlx.Tests.AdvancedSQL;

/// <summary>
/// CASE表达式测试 - Requirement 46
/// 包括：简单CASE、搜索CASE、嵌套CASE、NULL处理
/// </summary>
[TestClass]
public class TDD_CaseExpression
{
    private SqliteConnection? _connection;

    [TestInitialize]
    public void Setup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _connection?.Close();
        _connection?.Dispose();
    }

    #region 简单CASE表达式测试 (Requirement 46.1)

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Phase2")]
    [TestCategory("CaseExpression")]
    [Description("简单CASE表达式 - 基础值匹配")]
    public async Task SimpleCaseExpression_BasicValueMatch_ShouldWork()
    {
        // Arrange
        _connection!.Execute("CREATE TABLE orders (id INTEGER, status INTEGER)");
        _connection.Execute("INSERT INTO orders VALUES (1, 1), (2, 2), (3, 3), (4, 0)");

        var repo = new CaseExpressionRepo(_connection);

        // Act
        var results = await repo.GetOrdersWithStatusText();

        // Assert
        Assert.AreEqual(4, results.Count);
        Assert.AreEqual("Pending", results.First(o => o.Id == 1).StatusText);
        Assert.AreEqual("Processing", results.First(o => o.Id == 2).StatusText);
        Assert.AreEqual("Completed", results.First(o => o.Id == 3).StatusText);
        Assert.AreEqual("Unknown", results.First(o => o.Id == 4).StatusText);
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Phase2")]
    [TestCategory("CaseExpression")]
    [Description("简单CASE表达式 - 字符串值匹配")]
    public async Task SimpleCaseExpression_StringValueMatch_ShouldWork()
    {
        // Arrange
        _connection!.Execute("CREATE TABLE products (id INTEGER, category TEXT)");
        _connection.Execute("INSERT INTO products VALUES (1, 'electronics'), (2, 'clothing'), (3, 'food'), (4, 'other')");

        var repo = new CaseExpressionRepo(_connection);

        // Act
        var results = await repo.GetProductsWithCategoryLabel();

        // Assert
        Assert.AreEqual(4, results.Count);
        Assert.AreEqual("Electronic Devices", results.First(p => p.Id == 1).CategoryLabel);
        Assert.AreEqual("Apparel", results.First(p => p.Id == 2).CategoryLabel);
        Assert.AreEqual("Food & Beverage", results.First(p => p.Id == 3).CategoryLabel);
        Assert.AreEqual("Miscellaneous", results.First(p => p.Id == 4).CategoryLabel);
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Phase2")]
    [TestCategory("CaseExpression")]
    [Description("简单CASE表达式 - 无ELSE子句")]
    public async Task SimpleCaseExpression_WithoutElse_ShouldReturnNull()
    {
        // Arrange
        _connection!.Execute("CREATE TABLE items (id INTEGER, type INTEGER)");
        _connection.Execute("INSERT INTO items VALUES (1, 1), (2, 2), (3, 99)");

        var repo = new CaseExpressionRepo(_connection);

        // Act
        var results = await repo.GetItemsWithTypeNameNoElse();

        // Assert
        Assert.AreEqual(3, results.Count);
        Assert.AreEqual("Type A", results.First(i => i.Id == 1).TypeName);
        Assert.AreEqual("Type B", results.First(i => i.Id == 2).TypeName);
        Assert.IsNull(results.First(i => i.Id == 3).TypeName); // 无匹配时返回NULL
    }

    #endregion

    #region 搜索CASE表达式测试 (Requirement 46.2)

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Phase2")]
    [TestCategory("CaseExpression")]
    [Description("搜索CASE表达式 - 范围条件")]
    public async Task SearchedCaseExpression_RangeCondition_ShouldWork()
    {
        // Arrange
        _connection!.Execute("CREATE TABLE students (id INTEGER, score INTEGER)");
        _connection.Execute("INSERT INTO students VALUES (1, 95), (2, 85), (3, 75), (4, 65), (5, 55)");

        var repo = new CaseExpressionRepo(_connection);

        // Act
        var results = await repo.GetStudentsWithGrade();

        // Assert
        Assert.AreEqual(5, results.Count);
        Assert.AreEqual("A", results.First(s => s.Id == 1).Grade);
        Assert.AreEqual("B", results.First(s => s.Id == 2).Grade);
        Assert.AreEqual("C", results.First(s => s.Id == 3).Grade);
        Assert.AreEqual("D", results.First(s => s.Id == 4).Grade);
        Assert.AreEqual("F", results.First(s => s.Id == 5).Grade);
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Phase2")]
    [TestCategory("CaseExpression")]
    [Description("搜索CASE表达式 - 多条件组合")]
    public async Task SearchedCaseExpression_MultipleConditions_ShouldWork()
    {
        // Arrange
        _connection!.Execute("CREATE TABLE employees (id INTEGER, salary REAL, years_of_service INTEGER)");
        _connection.Execute("INSERT INTO employees VALUES (1, 80000, 10), (2, 50000, 3), (3, 60000, 8), (4, 40000, 1)");

        var repo = new CaseExpressionRepo(_connection);

        // Act
        var results = await repo.GetEmployeesWithBonusLevel();

        // Assert
        Assert.AreEqual(4, results.Count);
        Assert.AreEqual("High", results.First(e => e.Id == 1).BonusLevel); // 高薪+资深
        Assert.AreEqual("Low", results.First(e => e.Id == 2).BonusLevel);  // 中薪+新人
        Assert.AreEqual("Medium", results.First(e => e.Id == 3).BonusLevel); // 中薪+资深
        Assert.AreEqual("Low", results.First(e => e.Id == 4).BonusLevel);  // 低薪+新人
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Phase2")]
    [TestCategory("CaseExpression")]
    [Description("搜索CASE表达式 - 在WHERE子句中使用")]
    public async Task SearchedCaseExpression_InWhereClause_ShouldWork()
    {
        // Arrange
        _connection!.Execute("CREATE TABLE tasks (id INTEGER, priority INTEGER, is_urgent INTEGER)");
        _connection.Execute("INSERT INTO tasks VALUES (1, 1, 1), (2, 2, 0), (3, 3, 1), (4, 1, 0)");

        var repo = new CaseExpressionRepo(_connection);

        // Act
        var results = await repo.GetHighPriorityTasks();

        // Assert
        Assert.IsTrue(results.Count >= 2);
        // 应该返回priority=1或is_urgent=1的任务
        Assert.IsTrue(results.All(t => t.Priority == 1 || t.IsUrgent == 1));
    }

    #endregion

    #region 嵌套CASE表达式测试 (Requirement 46.3)

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Phase2")]
    [TestCategory("CaseExpression")]
    [Description("嵌套CASE表达式 - 两层嵌套")]
    public async Task NestedCaseExpression_TwoLevels_ShouldWork()
    {
        // Arrange
        _connection!.Execute("CREATE TABLE transactions (id INTEGER, type TEXT, amount REAL)");
        _connection.Execute("INSERT INTO transactions VALUES (1, 'credit', 1000), (2, 'credit', 100), (3, 'debit', 500), (4, 'debit', 50)");

        var repo = new CaseExpressionRepo(_connection);

        // Act
        var results = await repo.GetTransactionsWithCategory();

        // Assert
        Assert.AreEqual(4, results.Count);
        Assert.AreEqual("Large Credit", results.First(t => t.Id == 1).Category);
        Assert.AreEqual("Small Credit", results.First(t => t.Id == 2).Category);
        Assert.AreEqual("Large Debit", results.First(t => t.Id == 3).Category);
        Assert.AreEqual("Small Debit", results.First(t => t.Id == 4).Category);
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Phase2")]
    [TestCategory("CaseExpression")]
    [Description("嵌套CASE表达式 - 三层嵌套")]
    public async Task NestedCaseExpression_ThreeLevels_ShouldWork()
    {
        // Arrange
        _connection!.Execute("CREATE TABLE shipments (id INTEGER, region TEXT, weight REAL, is_express INTEGER)");
        _connection.Execute("INSERT INTO shipments VALUES (1, 'domestic', 5, 1), (2, 'domestic', 20, 0), (3, 'international', 3, 1), (4, 'international', 15, 0)");

        var repo = new CaseExpressionRepo(_connection);

        // Act
        var results = await repo.GetShipmentsWithShippingCost();

        // Assert
        Assert.AreEqual(4, results.Count);
        // 验证不同组合的运费计算
        Assert.IsTrue(results.First(s => s.Id == 1).ShippingCost > 0);
        Assert.IsTrue(results.First(s => s.Id == 3).ShippingCost > results.First(s => s.Id == 1).ShippingCost); // 国际快递更贵
    }

    #endregion

    #region CASE与NULL处理测试 (Requirement 46.4)

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Phase2")]
    [TestCategory("CaseExpression")]
    [Description("CASE表达式 - IS NULL条件")]
    public async Task CaseExpression_IsNullCondition_ShouldWork()
    {
        // Arrange
        _connection!.Execute("CREATE TABLE contacts (id INTEGER, email TEXT, phone TEXT)");
        _connection.Execute("INSERT INTO contacts VALUES (1, 'a@b.com', '123'), (2, NULL, '456'), (3, 'c@d.com', NULL), (4, NULL, NULL)");

        var repo = new CaseExpressionRepo(_connection);

        // Act
        var results = await repo.GetContactsWithPreferredMethod();

        // Assert
        Assert.AreEqual(4, results.Count);
        Assert.AreEqual("Email", results.First(c => c.Id == 1).PreferredMethod);
        Assert.AreEqual("Phone", results.First(c => c.Id == 2).PreferredMethod);
        Assert.AreEqual("Email", results.First(c => c.Id == 3).PreferredMethod);
        Assert.AreEqual("None", results.First(c => c.Id == 4).PreferredMethod);
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Phase2")]
    [TestCategory("CaseExpression")]
    [Description("CASE表达式 - COALESCE结合使用")]
    public async Task CaseExpression_WithCoalesce_ShouldWork()
    {
        // Arrange
        _connection!.Execute("CREATE TABLE users (id INTEGER, nickname TEXT, full_name TEXT, email TEXT)");
        _connection.Execute("INSERT INTO users VALUES (1, 'Johnny', 'John Doe', 'john@example.com'), (2, NULL, 'Jane Doe', 'jane@example.com'), (3, NULL, NULL, 'unknown@example.com')");

        var repo = new CaseExpressionRepo(_connection);

        // Act
        var results = await repo.GetUsersWithDisplayName();

        // Assert
        Assert.AreEqual(3, results.Count);
        Assert.AreEqual("Johnny", results.First(u => u.Id == 1).DisplayName);
        Assert.AreEqual("Jane Doe", results.First(u => u.Id == 2).DisplayName);
        Assert.AreEqual("unknown@example.com", results.First(u => u.Id == 3).DisplayName);
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Phase2")]
    [TestCategory("CaseExpression")]
    [Description("CASE表达式 - NULL值比较")]
    public async Task CaseExpression_NullComparison_ShouldWork()
    {
        // Arrange
        _connection!.Execute("CREATE TABLE inventory (id INTEGER, quantity INTEGER, reorder_level INTEGER)");
        _connection.Execute("INSERT INTO inventory VALUES (1, 100, 50), (2, 30, 50), (3, NULL, 50), (4, 10, NULL)");

        var repo = new CaseExpressionRepo(_connection);

        // Act
        var results = await repo.GetInventoryWithStockStatus();

        // Assert
        Assert.AreEqual(4, results.Count);
        Assert.AreEqual("In Stock", results.First(i => i.Id == 1).StockStatus);
        Assert.AreEqual("Low Stock", results.First(i => i.Id == 2).StockStatus);
        Assert.AreEqual("Unknown", results.First(i => i.Id == 3).StockStatus);
        Assert.AreEqual("Unknown", results.First(i => i.Id == 4).StockStatus);
    }

    #endregion

    #region CASE表达式在聚合函数中使用 (Requirement 46.5)

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Phase2")]
    [TestCategory("CaseExpression")]
    [Description("CASE表达式 - 在SUM中使用")]
    public async Task CaseExpression_InSumAggregate_ShouldWork()
    {
        // Arrange
        _connection!.Execute("CREATE TABLE sales (id INTEGER, region TEXT, amount REAL)");
        _connection.Execute("INSERT INTO sales VALUES (1, 'North', 100), (2, 'South', 200), (3, 'North', 150), (4, 'South', 50)");

        var repo = new CaseExpressionRepo(_connection);

        // Act
        var result = await repo.GetSalesSummaryByRegion();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(250, result.NorthTotal, 0.01); // 100 + 150
        Assert.AreEqual(250, result.SouthTotal, 0.01); // 200 + 50
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Phase2")]
    [TestCategory("CaseExpression")]
    [Description("CASE表达式 - 在COUNT中使用")]
    public async Task CaseExpression_InCountAggregate_ShouldWork()
    {
        // Arrange
        _connection!.Execute("CREATE TABLE tickets (id INTEGER, status TEXT)");
        _connection.Execute("INSERT INTO tickets VALUES (1, 'open'), (2, 'closed'), (3, 'open'), (4, 'pending'), (5, 'closed')");

        var repo = new CaseExpressionRepo(_connection);

        // Act
        var result = await repo.GetTicketCountByStatus();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(2, result.OpenCount);
        Assert.AreEqual(2, result.ClosedCount);
        Assert.AreEqual(1, result.PendingCount);
    }

    #endregion
}


#region CASE表达式Repository接口和模型

// CASE表达式Repository
public interface ICaseExpressionRepo
{
    // 简单CASE表达式 - 状态码转文本
    [SqlTemplate(@"SELECT id, status, 
        CASE status 
            WHEN 1 THEN 'Pending' 
            WHEN 2 THEN 'Processing' 
            WHEN 3 THEN 'Completed' 
            ELSE 'Unknown' 
        END as status_text 
        FROM orders")]
    Task<List<OrderWithStatusText>> GetOrdersWithStatusText();

    // 简单CASE表达式 - 字符串匹配
    [SqlTemplate(@"SELECT id, category,
        CASE category
            WHEN 'electronics' THEN 'Electronic Devices'
            WHEN 'clothing' THEN 'Apparel'
            WHEN 'food' THEN 'Food & Beverage'
            ELSE 'Miscellaneous'
        END as category_label
        FROM products")]
    Task<List<ProductWithCategoryLabel>> GetProductsWithCategoryLabel();

    // 简单CASE表达式 - 无ELSE子句
    [SqlTemplate(@"SELECT id, type,
        CASE type
            WHEN 1 THEN 'Type A'
            WHEN 2 THEN 'Type B'
        END as type_name
        FROM items")]
    Task<List<ItemWithTypeName>> GetItemsWithTypeNameNoElse();

    // 搜索CASE表达式 - 成绩等级
    [SqlTemplate(@"SELECT id, score,
        CASE 
            WHEN score >= 90 THEN 'A'
            WHEN score >= 80 THEN 'B'
            WHEN score >= 70 THEN 'C'
            WHEN score >= 60 THEN 'D'
            ELSE 'F'
        END as grade
        FROM students")]
    Task<List<StudentWithGrade>> GetStudentsWithGrade();

    // 搜索CASE表达式 - 多条件组合
    [SqlTemplate(@"SELECT id, salary, years_of_service,
        CASE 
            WHEN salary >= 70000 AND years_of_service >= 5 THEN 'High'
            WHEN salary >= 50000 AND years_of_service >= 5 THEN 'Medium'
            ELSE 'Low'
        END as bonus_level
        FROM employees")]
    Task<List<EmployeeWithBonusLevel>> GetEmployeesWithBonusLevel();

    // 搜索CASE表达式 - WHERE子句中使用
    [SqlTemplate(@"SELECT id, priority, is_urgent FROM tasks 
        WHERE CASE WHEN priority = 1 THEN 1 WHEN is_urgent = 1 THEN 1 ELSE 0 END = 1")]
    Task<List<TaskWithPriority>> GetHighPriorityTasks();

    // 嵌套CASE表达式 - 两层
    [SqlTemplate(@"SELECT id, type, amount,
        CASE type
            WHEN 'credit' THEN 
                CASE WHEN amount >= 500 THEN 'Large Credit' ELSE 'Small Credit' END
            WHEN 'debit' THEN
                CASE WHEN amount >= 200 THEN 'Large Debit' ELSE 'Small Debit' END
            ELSE 'Unknown'
        END as category
        FROM transactions")]
    Task<List<TransactionWithCategory>> GetTransactionsWithCategory();

    // 嵌套CASE表达式 - 三层
    [SqlTemplate(@"SELECT id, region, weight, is_express,
        CASE region
            WHEN 'domestic' THEN
                CASE WHEN is_express = 1 THEN
                    CASE WHEN weight <= 10 THEN 10.0 ELSE 20.0 END
                ELSE
                    CASE WHEN weight <= 10 THEN 5.0 ELSE 10.0 END
                END
            WHEN 'international' THEN
                CASE WHEN is_express = 1 THEN
                    CASE WHEN weight <= 10 THEN 30.0 ELSE 50.0 END
                ELSE
                    CASE WHEN weight <= 10 THEN 15.0 ELSE 25.0 END
                END
            ELSE 0.0
        END as shipping_cost
        FROM shipments")]
    Task<List<ShipmentWithCost>> GetShipmentsWithShippingCost();

    // CASE与NULL - IS NULL条件
    [SqlTemplate(@"SELECT id, email, phone,
        CASE 
            WHEN email IS NOT NULL THEN 'Email'
            WHEN phone IS NOT NULL THEN 'Phone'
            ELSE 'None'
        END as preferred_method
        FROM contacts")]
    Task<List<ContactWithPreferredMethod>> GetContactsWithPreferredMethod();

    // CASE与COALESCE结合
    [SqlTemplate(@"SELECT id, nickname, full_name, email,
        COALESCE(nickname, full_name, email) as display_name
        FROM users")]
    Task<List<UserWithDisplayName>> GetUsersWithDisplayName();

    // CASE与NULL比较
    [SqlTemplate(@"SELECT id, quantity, reorder_level,
        CASE 
            WHEN quantity IS NULL OR reorder_level IS NULL THEN 'Unknown'
            WHEN quantity > reorder_level THEN 'In Stock'
            ELSE 'Low Stock'
        END as stock_status
        FROM inventory")]
    Task<List<InventoryWithStockStatus>> GetInventoryWithStockStatus();

    // CASE在SUM聚合中
    [SqlTemplate(@"SELECT 
        SUM(CASE WHEN region = 'North' THEN amount ELSE 0 END) as north_total,
        SUM(CASE WHEN region = 'South' THEN amount ELSE 0 END) as south_total
        FROM sales")]
    Task<SalesSummary> GetSalesSummaryByRegion();

    // CASE在COUNT聚合中
    [SqlTemplate(@"SELECT 
        SUM(CASE WHEN status = 'open' THEN 1 ELSE 0 END) as open_count,
        SUM(CASE WHEN status = 'closed' THEN 1 ELSE 0 END) as closed_count,
        SUM(CASE WHEN status = 'pending' THEN 1 ELSE 0 END) as pending_count
        FROM tickets")]
    Task<TicketCountSummary> GetTicketCountByStatus();
}

[RepositoryFor(typeof(ICaseExpressionRepo))]
public partial class CaseExpressionRepo(IDbConnection connection) : ICaseExpressionRepo { }

// 模型类
public class OrderWithStatusText
{
    public long Id { get; set; }
    public int Status { get; set; }
    public string StatusText { get; set; } = "";
}

public class ProductWithCategoryLabel
{
    public long Id { get; set; }
    public string Category { get; set; } = "";
    public string CategoryLabel { get; set; } = "";
}

public class ItemWithTypeName
{
    public long Id { get; set; }
    public int Type { get; set; }
    public string? TypeName { get; set; }
}

public class StudentWithGrade
{
    public long Id { get; set; }
    public int Score { get; set; }
    public string Grade { get; set; } = "";
}

public class EmployeeWithBonusLevel
{
    public long Id { get; set; }
    public double Salary { get; set; }
    public int YearsOfService { get; set; }
    public string BonusLevel { get; set; } = "";
}

public class TaskWithPriority
{
    public long Id { get; set; }
    public int Priority { get; set; }
    public int IsUrgent { get; set; }
}

public class TransactionWithCategory
{
    public long Id { get; set; }
    public string Type { get; set; } = "";
    public double Amount { get; set; }
    public string Category { get; set; } = "";
}

public class ShipmentWithCost
{
    public long Id { get; set; }
    public string Region { get; set; } = "";
    public double Weight { get; set; }
    public int IsExpress { get; set; }
    public double ShippingCost { get; set; }
}

public class ContactWithPreferredMethod
{
    public long Id { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string PreferredMethod { get; set; } = "";
}

public class UserWithDisplayName
{
    public long Id { get; set; }
    public string? Nickname { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string DisplayName { get; set; } = "";
}

public class InventoryWithStockStatus
{
    public long Id { get; set; }
    public int? Quantity { get; set; }
    public int? ReorderLevel { get; set; }
    public string StockStatus { get; set; } = "";
}

public class SalesSummary
{
    public double NorthTotal { get; set; }
    public double SouthTotal { get; set; }
}

public class TicketCountSummary
{
    public int OpenCount { get; set; }
    public int ClosedCount { get; set; }
    public int PendingCount { get; set; }
}

#endregion
