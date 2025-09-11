using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using Sqlx;
using Sqlx.SqlGen;

namespace Sqlx.Samples.NewFeatures
{
    /// <summary>
    /// 演示使用 [Where] 和 [Set] 特性进行精确的批量更新操作
    /// </summary>
    public class BatchUpdateWithAttributes
    {
        // 示例1：电商产品更新（基于 ID 更新价格和库存）
        public class Product
        {
            [Where] // 明确指定用于 WHERE 条件
            public int Id { get; set; }
            
            public string Name { get; set; } = string.Empty;
            
            [Set] // 明确指定用于 SET 子句
            public decimal Price { get; set; }
            
            [Set] // 明确指定用于 SET 子句
            public int Stock { get; set; }
            
            public DateTime CreatedAt { get; set; } // 既不是 WHERE 也不是 SET，会被忽略
        }

        // 示例2：用户信息更新（基于用户名更新邮箱和最后登录时间）
        public class UserProfile
        {
            public int Id { get; set; } // 不参与更新
            
            [Where] // 基于用户名查找
            public string UserName { get; set; } = string.Empty;
            
            [Set] // 更新邮箱
            public string Email { get; set; } = string.Empty;
            
            [Set] // 更新最后登录时间
            public DateTime LastLoginAt { get; set; }
            
            public DateTime CreatedAt { get; set; } // 不参与更新
        }

        // 示例3：订单状态更新（支持自定义操作符）
        public class OrderStatus
        {
            [Where] // 基于订单号
            public string OrderNumber { get; set; } = string.Empty;
            
            [Where(">=")] // 自定义操作符：只更新指定日期之后的订单
            public DateTime OrderDate { get; set; }
            
            [Set] // 更新状态
            public string Status { get; set; } = string.Empty;
            
            [Set] // 更新处理时间
            public DateTime ProcessedAt { get; set; }
        }

        // 示例4：库存批量调整（复杂的 WHERE 条件）
        public class InventoryItem
        {
            [Where] // 产品ID
            public int ProductId { get; set; }
            
            [Where] // 仓库ID
            public int WarehouseId { get; set; }
            
            [Set] // 调整库存数量
            public int Quantity { get; set; }
            
            [Set] // 更新最后修改时间
            public DateTime LastModified { get; set; }
        }

        // 服务接口定义
        public interface IProductBatchService
        {
            [SqlExecuteType(SqlExecuteTypes.BatchUpdate, "products")]
            Task<int> BatchUpdateProductsAsync(IEnumerable<Product> products);
        }

        public interface IUserProfileBatchService
        {
            [SqlExecuteType(SqlExecuteTypes.BatchUpdate, "user_profiles")]
            Task<int> BatchUpdateUserProfilesAsync(IEnumerable<UserProfile> profiles);
        }

        public interface IOrderStatusBatchService
        {
            [SqlExecuteType(SqlExecuteTypes.BatchUpdate, "orders")]
            Task<int> BatchUpdateOrderStatusAsync(IEnumerable<OrderStatus> orders);
        }

        public interface IInventoryBatchService
        {
            [SqlExecuteType(SqlExecuteTypes.BatchUpdate, "inventory")]
            Task<int> BatchUpdateInventoryAsync(IEnumerable<InventoryItem> items);
            
            [SqlExecuteType(SqlExecuteTypes.BatchDelete, "inventory")]
            Task<int> BatchDeleteInventoryAsync(IEnumerable<InventoryItem> items);
        }

        // 实现类
        [RepositoryFor(typeof(IProductBatchService))]
        public partial class ProductBatchRepository : IProductBatchService
        {
            private readonly DbConnection connection;
            public ProductBatchRepository(DbConnection connection) => this.connection = connection;
        }

        [RepositoryFor(typeof(IUserProfileBatchService))]
        public partial class UserProfileBatchRepository : IUserProfileBatchService
        {
            private readonly DbConnection connection;
            public UserProfileBatchRepository(DbConnection connection) => this.connection = connection;
        }

        [RepositoryFor(typeof(IOrderStatusBatchService))]
        public partial class OrderStatusBatchRepository : IOrderStatusBatchService
        {
            private readonly DbConnection connection;
            public OrderStatusBatchRepository(DbConnection connection) => this.connection = connection;
        }

        [RepositoryFor(typeof(IInventoryBatchService))]
        public partial class InventoryBatchRepository : IInventoryBatchService
        {
            private readonly DbConnection connection;
            public InventoryBatchRepository(DbConnection connection) => this.connection = connection;
        }

        // 演示方法
        public static async Task RunBatchUpdateDemo(DbConnection connection)
        {
            Console.WriteLine("🔄 批量更新操作演示（使用 [Where] 和 [Set] 特性）\n");

            var productService = new ProductBatchRepository(connection);
            var userService = new UserProfileBatchRepository(connection);
            var orderService = new OrderStatusBatchRepository(connection);
            var inventoryService = new InventoryBatchRepository(connection);

            // 1. 产品价格和库存更新
            await DemoProductUpdate(productService);
            
            // 2. 用户信息更新
            await DemoUserProfileUpdate(userService);
            
            // 3. 订单状态更新
            await DemoOrderStatusUpdate(orderService);
            
            // 4. 库存管理
            await DemoInventoryManagement(inventoryService);
        }

        private static async Task DemoProductUpdate(IProductBatchService service)
        {
            Console.WriteLine("📦 产品批量更新演示");
            Console.WriteLine("WHERE: Id = @Id");
            Console.WriteLine("SET: Price = @Price, Stock = @Stock\n");

            var products = new[]
            {
                new Product { Id = 1, Name = "笔记本电脑", Price = 5999.99m, Stock = 50, CreatedAt = DateTime.Now },
                new Product { Id = 2, Name = "无线鼠标", Price = 199.99m, Stock = 200, CreatedAt = DateTime.Now }
            };

            try
            {
                var updateCount = await service.BatchUpdateProductsAsync(products);
                Console.WriteLine($"✅ 成功更新 {updateCount} 个产品的价格和库存");
                Console.WriteLine("生成的 SQL 类似于:");
                Console.WriteLine("UPDATE products SET Price = @Price, Stock = @Stock WHERE Id = @Id");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 更新失败: {ex.Message}");
            }
            
            Console.WriteLine();
        }

        private static async Task DemoUserProfileUpdate(IUserProfileBatchService service)
        {
            Console.WriteLine("👤 用户信息批量更新演示");
            Console.WriteLine("WHERE: UserName = @UserName");
            Console.WriteLine("SET: Email = @Email, LastLoginAt = @LastLoginAt\n");

            var profiles = new[]
            {
                new UserProfile { UserName = "zhang_san", Email = "zhang.new@example.com", LastLoginAt = DateTime.Now },
                new UserProfile { UserName = "li_si", Email = "li.new@example.com", LastLoginAt = DateTime.Now }
            };

            try
            {
                var updateCount = await service.BatchUpdateUserProfilesAsync(profiles);
                Console.WriteLine($"✅ 成功更新 {updateCount} 个用户的信息");
                Console.WriteLine("生成的 SQL 类似于:");
                Console.WriteLine("UPDATE user_profiles SET Email = @Email, LastLoginAt = @LastLoginAt WHERE UserName = @UserName");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 更新失败: {ex.Message}");
            }
            
            Console.WriteLine();
        }

        private static async Task DemoOrderStatusUpdate(IOrderStatusBatchService service)
        {
            Console.WriteLine("📋 订单状态批量更新演示");
            Console.WriteLine("WHERE: OrderNumber = @OrderNumber AND OrderDate >= @OrderDate");
            Console.WriteLine("SET: Status = @Status, ProcessedAt = @ProcessedAt\n");

            var orders = new[]
            {
                new OrderStatus 
                { 
                    OrderNumber = "ORD-2024-001", 
                    OrderDate = DateTime.Today.AddDays(-7), 
                    Status = "Shipped", 
                    ProcessedAt = DateTime.Now 
                },
                new OrderStatus 
                { 
                    OrderNumber = "ORD-2024-002", 
                    OrderDate = DateTime.Today.AddDays(-5), 
                    Status = "Delivered", 
                    ProcessedAt = DateTime.Now 
                }
            };

            try
            {
                var updateCount = await service.BatchUpdateOrderStatusAsync(orders);
                Console.WriteLine($"✅ 成功更新 {updateCount} 个订单状态");
                Console.WriteLine("生成的 SQL 类似于:");
                Console.WriteLine("UPDATE orders SET Status = @Status, ProcessedAt = @ProcessedAt");
                Console.WriteLine("WHERE OrderNumber = @OrderNumber AND OrderDate >= @OrderDate");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 更新失败: {ex.Message}");
            }
            
            Console.WriteLine();
        }

        private static async Task DemoInventoryManagement(IInventoryBatchService service)
        {
            Console.WriteLine("📦 库存管理批量操作演示");
            Console.WriteLine("WHERE: ProductId = @ProductId AND WarehouseId = @WarehouseId");
            Console.WriteLine("SET: Quantity = @Quantity, LastModified = @LastModified\n");

            var inventoryItems = new[]
            {
                new InventoryItem { ProductId = 1, WarehouseId = 101, Quantity = 100, LastModified = DateTime.Now },
                new InventoryItem { ProductId = 2, WarehouseId = 101, Quantity = 50, LastModified = DateTime.Now },
                new InventoryItem { ProductId = 1, WarehouseId = 102, Quantity = 75, LastModified = DateTime.Now }
            };

            try
            {
                // 批量更新库存
                var updateCount = await service.BatchUpdateInventoryAsync(inventoryItems);
                Console.WriteLine($"✅ 成功更新 {updateCount} 条库存记录");
                
                // 批量删除库存（基于相同的 WHERE 条件）
                var deleteItems = new[] { inventoryItems[0] }; // 删除第一条
                var deleteCount = await service.BatchDeleteInventoryAsync(deleteItems);
                Console.WriteLine($"✅ 成功删除 {deleteCount} 条库存记录");
                
                Console.WriteLine("生成的 SQL 类似于:");
                Console.WriteLine("UPDATE inventory SET Quantity = @Quantity, LastModified = @LastModified");
                Console.WriteLine("WHERE ProductId = @ProductId AND WarehouseId = @WarehouseId");
                Console.WriteLine();
                Console.WriteLine("DELETE FROM inventory");
                Console.WriteLine("WHERE ProductId = @ProductId AND WarehouseId = @WarehouseId");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 操作失败: {ex.Message}");
            }
            
            Console.WriteLine();
        }

        // 展示新特性的优势
        public static void ShowAttributeAdvantages()
        {
            Console.WriteLine("🎯 新特性的优势:\n");
            
            Console.WriteLine("1. 🎯 精确控制:");
            Console.WriteLine("   - [Where] 特性明确指定查询条件字段");
            Console.WriteLine("   - [Set] 特性明确指定要更新的字段");
            Console.WriteLine("   - 不再依赖主键自动推断");
            
            Console.WriteLine("\n2. 🔧 灵活配置:");
            Console.WriteLine("   - 支持自定义 WHERE 操作符：[Where(\">=\")]");
            Console.WriteLine("   - 支持复合 WHERE 条件：多个字段同时作为条件");
            Console.WriteLine("   - 支持忽略空值：[Set(ignoreIfNull: true)]");
            
            Console.WriteLine("\n3. 🛡️ 类型安全:");
            Console.WriteLine("   - 编译时检查，避免运行时错误");
            Console.WriteLine("   - 明确的意图表达，代码更易理解");
            Console.WriteLine("   - 自动参数绑定，防止 SQL 注入");
            
            Console.WriteLine("\n4. 📈 向后兼容:");
            Console.WriteLine("   - 没有特性时使用默认行为（主键作为 WHERE 条件）");
            Console.WriteLine("   - 现有代码无需修改即可继续工作");
            Console.WriteLine("   - 可以渐进式采用新特性");
            
            Console.WriteLine("\n5. 🚀 性能优化:");
            Console.WriteLine("   - 原生 DbBatch 支持（如果数据库支持）");
            Console.WriteLine("   - 智能降级到兼容模式");
            Console.WriteLine("   - 批量操作比单条操作快 10-100 倍");
        }
    }
}
