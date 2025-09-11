using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using Sqlx.Annotations;

namespace PrimaryConstructorExample
{
    // 传统类
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    // Record 类型 (C# 9+)
    public record Product(int Id, string Name, decimal Price, int CategoryId)
    {
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;
    }

    // 主构造函数类 (C# 12+)
    public class Order(int id, string customerName)
    {
        public int Id { get; } = id;
        public string CustomerName { get; } = customerName;
        public DateTime OrderDate { get; set; } = DateTime.Now;
        public decimal TotalAmount { get; set; }
        public List<OrderItem> Items { get; set; } = new();
    }

    // 主构造函数 Record (C# 12+)
    public record OrderItem(int OrderId, int ProductId, int Quantity, decimal UnitPrice)
    {
        public decimal TotalPrice => Quantity * UnitPrice;
    }

    // 服务接口定义
    public interface ICategoryService
    {
        [Sqlx("SELECT * FROM Categories")]
        IList<Category> GetAllCategories();
        
        [SqlExecuteType(SqlExecuteTypes.Insert, "Categories")]
        int AddCategory(Category category);
    }

    public interface IProductService
    {
        [Sqlx("SELECT * FROM Products")]
        IList<Product> GetAllProducts();
        
        [SqlExecuteType(SqlExecuteTypes.Insert, "Products")]
        int AddProduct(Product product);
    }

    public interface IOrderService
    {
        [Sqlx("SELECT * FROM Orders")]
        IList<Order> GetAllOrders();
        
        [SqlExecuteType(SqlExecuteTypes.Insert, "Orders")]
        int AddOrder(Order order);
    }

    // Repository 实现
    [RepositoryFor(typeof(ICategoryService))]
    public partial class CategoryRepository : ICategoryService
    {
        public CategoryRepository(IDbConnection connection)
        {
            this.connection = (DbConnection)connection;
        }
    }

    [RepositoryFor(typeof(IProductService))]
    public partial class ProductRepository : IProductService
    {
        public ProductRepository(IDbConnection connection)
        {
            this.connection = (DbConnection)connection;
        }
    }

    [RepositoryFor(typeof(IOrderService))]
    public partial class OrderRepository : IOrderService
    {
        public OrderRepository(IDbConnection connection)
        {
            this.connection = (DbConnection)connection;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("🚀 Sqlx Primary Constructor & Record 支持演示");
            Console.WriteLine(new string('=', 50));

            // 创建内存数据库
            using var connection = new SQLiteConnection("Data Source=:memory:");
            connection.Open();

            // 创建表
            CreateTables(connection);

            // 创建 repositories
            var categoryRepo = new CategoryRepository(connection);
            var productRepo = new ProductRepository(connection);
            var orderRepo = new OrderRepository(connection);

            try
            {
                Console.WriteLine("\n📁 1. 测试传统类 (Category)");
                TestCategories(categoryRepo);

                Console.WriteLine("\n📦 2. 测试 Record 类型 (Product)");
                TestProducts(productRepo);

                Console.WriteLine("\n🛒 3. 测试主构造函数类 (Order)");
                TestOrders(orderRepo);

                Console.WriteLine("\n✅ 所有测试完成！Primary Constructor 和 Record 支持正常工作。");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 错误: {ex.Message}");
                Console.WriteLine($"详细信息: {ex}");
            }
        }

        static void CreateTables(IDbConnection connection)
        {
            var commands = new[]
            {
                @"CREATE TABLE Categories (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    Description TEXT
                )",
                @"CREATE TABLE Products (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    Price DECIMAL NOT NULL,
                    CategoryId INTEGER NOT NULL,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    IsActive BOOLEAN DEFAULT 1
                )",
                @"CREATE TABLE Orders (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    CustomerName TEXT NOT NULL,
                    OrderDate DATETIME DEFAULT CURRENT_TIMESTAMP,
                    TotalAmount DECIMAL DEFAULT 0
                )"
            };

            foreach (var sql in commands)
            {
                using var cmd = connection.CreateCommand();
                cmd.CommandText = sql;
                cmd.ExecuteNonQuery();
            }
        }

        static void TestCategories(ICategoryService categoryRepo)
        {
            // 测试传统类
            var category = new Category
            {
                Name = "电子产品",
                Description = "各种电子设备和配件"
            };

            Console.WriteLine($"添加分类: {category.Name}");
            var categoryId = categoryRepo.AddCategory(category);
            Console.WriteLine($"分类ID: {categoryId}");

            var categories = categoryRepo.GetAllCategories();
            Console.WriteLine($"总分类数: {categories.Count}");
            
            foreach (var cat in categories)
            {
                Console.WriteLine($"  - {cat.Name}: {cat.Description}");
            }
        }

        static void TestProducts(IProductService productRepo)
        {
            // 测试 Record 类型
            var product = new Product(0, "iPhone 15", 999.99m, 1)
            {
                CreatedAt = DateTime.Now,
                IsActive = true
            };

            Console.WriteLine($"添加产品: {product.Name} - ${product.Price}");
            var productId = productRepo.AddProduct(product);
            Console.WriteLine($"产品ID: {productId}");

            var products = productRepo.GetAllProducts();
            Console.WriteLine($"总产品数: {products.Count}");
            
            foreach (var prod in products)
            {
                Console.WriteLine($"  - {prod.Name}: ${prod.Price} (分类: {prod.CategoryId})");
                Console.WriteLine($"    创建时间: {prod.CreatedAt}, 激活: {prod.IsActive}");
            }
        }

        static void TestOrders(IOrderService orderRepo)
        {
            // 测试主构造函数类
            var order = new Order(0, "张三")
            {
                OrderDate = DateTime.Now,
                TotalAmount = 999.99m
            };

            Console.WriteLine($"添加订单: 客户 {order.CustomerName}, 金额 ${order.TotalAmount}");
            var orderId = orderRepo.AddOrder(order);
            Console.WriteLine($"订单ID: {orderId}");

            var orders = orderRepo.GetAllOrders();
            Console.WriteLine($"总订单数: {orders.Count}");
            
            foreach (var ord in orders)
            {
                Console.WriteLine($"  - 订单 #{ord.Id}: {ord.CustomerName}");
                Console.WriteLine($"    日期: {ord.OrderDate}, 金额: ${ord.TotalAmount}");
            }
        }
    }
}