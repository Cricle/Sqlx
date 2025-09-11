using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using Sqlx.Annotations;

namespace SimpleExample;

/// <summary>
/// Sqlx v2.0 简单示例
/// 展示 Primary Constructor 和 Record 支持
/// </summary>
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("🚀 Sqlx v2.0 现代 C# 特性演示");
        Console.WriteLine(new string('=', 50));
        
        // 创建内存数据库
        using var connection = new SQLiteConnection("Data Source=:memory:");
        connection.Open();
        
        // 初始化数据库
        InitializeDatabase(connection);
        
        // 创建服务实例
        var categoryService = new CategoryRepository(connection);
        var productService = new ProductRepository(connection);
        var orderService = new OrderRepository(connection);
        
        try
        {
            // 演示传统类
            DemoTraditionalClass(categoryService);
            
            // 演示 Record 类型
            DemoRecordType(productService);
            
            // 演示主构造函数
            DemoPrimaryConstructor(orderService);
            
            Console.WriteLine("\n✅ 所有演示完成！");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ 出错: {ex.Message}");
        }
    }

    /// <summary>
    /// 演示传统类的使用
    /// </summary>
    static void DemoTraditionalClass(ICategoryService service)
    {
        Console.WriteLine("\n📁 1. 传统类演示 (Category)");
        
        // 创建分类
        var category = new Category
        {
            Name = "电子产品",
            Description = "各种电子设备和配件"
        };
        
        var categoryId = service.CreateCategory(category);
        Console.WriteLine($"创建分类，ID: {categoryId}");
        
        // 查询分类
        var categories = service.GetAllCategories();
        Console.WriteLine($"分类总数: {categories.Count}");
        
        foreach (var c in categories)
        {
            Console.WriteLine($"  - {c.Name}: {c.Description}");
        }
    }

    /// <summary>
    /// 演示 Record 类型的使用
    /// </summary>
    static void DemoRecordType(IProductService service)
    {
        Console.WriteLine("\n📦 2. Record 类型演示 (Product)");
        
        // 创建产品
        var product = new Product(0, "iPhone 15", 999.99m, 1);
        var productId = service.CreateProduct(product);
        Console.WriteLine($"创建产品: {product.Name} (ID: {productId})");
        
        // 查询产品
        var products = service.GetAllProducts();
        Console.WriteLine($"产品总数: {products.Count}");
        
        foreach (var p in products)
        {
            Console.WriteLine($"  - {p.Name}: ${p.Price}");
            
            // 展示 Record 的 with 表达式
            var discountedProduct = p with { Price = p.Price * 0.9m };
            Console.WriteLine($"    折扣价: ${discountedProduct.Price:F2}");
        }
    }

    /// <summary>
    /// 演示主构造函数的使用
    /// </summary>
    static void DemoPrimaryConstructor(IOrderService service)
    {
        Console.WriteLine("\n🛒 3. 主构造函数演示 (Order)");
        
        // 创建订单
        var order = new Order(0, "CUST001", "张三");
        order.TotalAmount = 1249.98m;
        
        var orderId = service.CreateOrder(order);
        Console.WriteLine($"创建订单: {order.CustomerName} - ${order.TotalAmount} (ID: {orderId})");
        
        // 查询订单
        var orders = service.GetAllOrders();
        Console.WriteLine($"订单总数: {orders.Count}");
        
        foreach (var o in orders)
        {
            Console.WriteLine($"  - 订单 {o.Id}: {o.CustomerName} - ${o.TotalAmount} ({o.Status})");
        }
    }

    /// <summary>
    /// 初始化数据库结构
    /// </summary>
    static void InitializeDatabase(DbConnection connection)
    {
        var sql = @"
            -- 分类表 (传统类)
            CREATE TABLE Categories (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Description TEXT
            );

            -- 产品表 (Record 类型)
            CREATE TABLE Products (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Price DECIMAL(10,2) NOT NULL,
                CategoryId INTEGER NOT NULL
            );

            -- 订单表 (主构造函数)
            CREATE TABLE Orders (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                CustomerId TEXT NOT NULL,
                CustomerName TEXT NOT NULL,
                TotalAmount DECIMAL(10,2) NOT NULL,
                Status TEXT NOT NULL DEFAULT 'Pending',
                OrderDate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
            );
        ";

        var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }
}

#region 实体定义

/// <summary>
/// 传统类 - 分类实体
/// </summary>
public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

/// <summary>
/// Record 类型 - 产品实体
/// </summary>
public record Product(int Id, string Name, decimal Price, int CategoryId);

/// <summary>
/// 主构造函数类 - 订单实体
/// </summary>
public class Order(int id, string customerId, string customerName)
{
    public int Id { get; set; } = id;
    public string CustomerId { get; } = customerId;
    public string CustomerName { get; } = customerName;
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
}

#endregion

#region 服务接口

public interface ICategoryService
{
    int CreateCategory(Category category);
    IList<Category> GetAllCategories();
}

public interface IProductService
{
    int CreateProduct(Product product);
    IList<Product> GetAllProducts();
}

public interface IOrderService
{
    int CreateOrder(Order order);
    IList<Order> GetAllOrders();
}

#endregion

#region Repository 实现

/// <summary>
/// 分类 Repository - 传统类支持
/// </summary>
[RepositoryFor(typeof(ICategoryService))]
public partial class CategoryRepository : ICategoryService
{
    private readonly DbConnection connection;

    public CategoryRepository(DbConnection connection)
    {
        this.connection = connection;
    }

    [Sqlx("INSERT INTO Categories (Name, Description) VALUES (@Name, @Description); SELECT last_insert_rowid();")]
    public partial int CreateCategory(Category category);

    [Sqlx("SELECT * FROM Categories ORDER BY Name")]
    public partial IList<Category> GetAllCategories();
}

/// <summary>
/// 产品 Repository - Record 类型支持
/// </summary>
[RepositoryFor(typeof(IProductService))]
public partial class ProductRepository : IProductService
{
    private readonly DbConnection connection;

    public ProductRepository(DbConnection connection)
    {
        this.connection = connection;
    }

    [Sqlx("INSERT INTO Products (Name, Price, CategoryId) VALUES (@Name, @Price, @CategoryId); SELECT last_insert_rowid();")]
    public partial int CreateProduct(Product product);

    [Sqlx("SELECT * FROM Products ORDER BY Name")]
    public partial IList<Product> GetAllProducts();
}

/// <summary>
/// 订单 Repository - 主构造函数支持
/// </summary>
[RepositoryFor(typeof(IOrderService))]
public partial class OrderRepository : IOrderService
{
    private readonly DbConnection connection;

    public OrderRepository(DbConnection connection)
    {
        this.connection = connection;
    }

    [Sqlx("INSERT INTO Orders (CustomerId, CustomerName, TotalAmount, Status) VALUES (@CustomerId, @CustomerName, @TotalAmount, @Status); SELECT last_insert_rowid();")]
    public partial int CreateOrder(Order order);

    [Sqlx("SELECT * FROM Orders ORDER BY OrderDate DESC")]
    public partial IList<Order> GetAllOrders();
}

#endregion
