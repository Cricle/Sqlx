using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Sqlx;
using Sqlx.SqlGen;

namespace Sqlx.Samples.NewFeatures
{
    /// <summary>
    /// 全面的批处理操作示例
    /// 演示原生 DbBatch 支持、精确控制特性、性能测试等所有功能
    /// </summary>
    public class ComprehensiveBatchExample
    {
        #region 实体定义

        // 基础产品实体（使用默认行为）
        public class Product
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public decimal Price { get; set; }
            public int CategoryId { get; set; }
            public DateTime CreatedAt { get; set; }
            public bool IsActive { get; set; } = true;
        }

        // 精确控制的产品更新实体
        public class ProductUpdate
        {
            [Where] // 明确指定 WHERE 条件
            public int Id { get; set; }
            
            public string Name { get; set; } = string.Empty; // 不参与更新
            
            [Set] // 明确指定 SET 子句
            public decimal Price { get; set; }
            
            [Set] // 明确指定 SET 子句
            public int Stock { get; set; }
            
            public DateTime CreatedAt { get; set; } // 不参与更新
        }

        // 基于非主键的用户更新
        public class UserProfileUpdate
        {
            [Where] // 基于用户名而不是 ID
            public string UserName { get; set; } = string.Empty;
            
            [Set] // 只更新邮箱
            public string Email { get; set; } = string.Empty;
            
            [Set] // 只更新最后登录时间
            public DateTime LastLoginAt { get; set; }
            
            public int Id { get; set; } // 不参与更新
        }

        // 复合 WHERE 条件的库存更新
        public class InventoryUpdate
        {
            [Where] // 复合主键 - 产品ID
            public int ProductId { get; set; }
            
            [Where] // 复合主键 - 仓库ID
            public int WarehouseId { get; set; }
            
            [Set] // 更新数量
            public int Quantity { get; set; }
            
            [Set] // 更新最后修改时间
            public DateTime LastModified { get; set; }
        }

        // 自定义操作符的订单更新
        public class OrderStatusUpdate
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

        #endregion

        #region 服务接口定义

        public interface IProductBatchService
        {
            // 基础批处理操作
            [SqlExecuteType(SqlExecuteTypes.BatchInsert, "products")]
            Task<int> BatchInsertAsync(IEnumerable<Product> products);
            
            [SqlExecuteType(SqlExecuteTypes.BatchUpdate, "products")]
            Task<int> BatchUpdateAsync(IEnumerable<Product> products);
            
            [SqlExecuteType(SqlExecuteTypes.BatchDelete, "products")]
            Task<int> BatchDeleteAsync(IEnumerable<Product> products);

            // 精确控制的更新
            [SqlExecuteType(SqlExecuteTypes.BatchUpdate, "products")]
            Task<int> BatchUpdatePreciseAsync(IEnumerable<ProductUpdate> products);

            // 通用批处理（方法名推断）
            [SqlExecuteType(SqlExecuteTypes.BatchCommand, "products")]
            Task<int> BatchAddProductsAsync(IEnumerable<Product> products);

            // 同步版本
            [SqlExecuteType(SqlExecuteTypes.BatchInsert, "products")]
            int BatchInsert(IEnumerable<Product> products);

            // 单条操作（用于性能对比）
            [SqlExecuteType(SqlExecuteTypes.Insert, "products")]
            Task<int> InsertSingleAsync(Product product);
        }

        public interface IUserProfileBatchService
        {
            [SqlExecuteType(SqlExecuteTypes.BatchUpdate, "user_profiles")]
            Task<int> BatchUpdateUserProfilesAsync(IEnumerable<UserProfileUpdate> profiles);
        }

        public interface IInventoryBatchService
        {
            [SqlExecuteType(SqlExecuteTypes.BatchUpdate, "inventory")]
            Task<int> BatchUpdateInventoryAsync(IEnumerable<InventoryUpdate> items);
            
            [SqlExecuteType(SqlExecuteTypes.BatchDelete, "inventory")]
            Task<int> BatchDeleteInventoryAsync(IEnumerable<InventoryUpdate> items);
        }

        public interface IOrderBatchService
        {
            [SqlExecuteType(SqlExecuteTypes.BatchUpdate, "orders")]
            Task<int> BatchUpdateOrderStatusAsync(IEnumerable<OrderStatusUpdate> orders);
        }

        #endregion

        #region 实现类

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

        [RepositoryFor(typeof(IInventoryBatchService))]
        public partial class InventoryBatchRepository : IInventoryBatchService
        {
            private readonly DbConnection connection;
            public InventoryBatchRepository(DbConnection connection) => this.connection = connection;
        }

        [RepositoryFor(typeof(IOrderBatchService))]
        public partial class OrderBatchRepository : IOrderBatchService
        {
            private readonly DbConnection connection;
            public OrderBatchRepository(DbConnection connection) => this.connection = connection;
        }

        #endregion

        #region 演示方法

        /// <summary>
        /// 运行完整的批处理演示
        /// </summary>
        public static async Task RunCompleteDemo(DbConnection connection)
        {
            Console.WriteLine("🚀 原生 DbBatch 批处理操作全面演示\n");
            
            PrintDatabaseInfo(connection);

            var productService = new ProductBatchRepository(connection);
            var userService = new UserProfileBatchRepository(connection);
            var inventoryService = new InventoryBatchRepository(connection);
            var orderService = new OrderBatchRepository(connection);

            // 1. 基础批处理操作
            await DemoBasicBatchOperations(productService);
            
            // 2. 精确控制特性演示
            await DemoPreciseControlFeatures(productService, userService, inventoryService, orderService);
            
            // 3. 性能对比测试
            await DemoPerformanceComparison(productService);
            
            // 4. 高级功能演示
            await DemoAdvancedFeatures(productService);

            Console.WriteLine("🎉 演示完成！");
        }

        private static void PrintDatabaseInfo(DbConnection connection)
        {
            var databaseType = connection.GetType().Name.Replace("Connection", "");
            var supportsDbBatch = connection is DbConnection dbConn && dbConn.CanCreateBatch;

            Console.WriteLine("🗄️ 数据库信息:");
            Console.WriteLine($"   类型: {databaseType}");
            Console.WriteLine($"   数据库: {connection.Database}");
            
            if (supportsDbBatch)
            {
                Console.WriteLine("   🚀 支持原生 DbBatch - 将使用最高性能模式");
            }
            else
            {
                Console.WriteLine("   📈 使用降级模式 - 仍可获得批处理优化");
            }
            Console.WriteLine();
        }

        private static async Task DemoBasicBatchOperations(IProductBatchService service)
        {
            Console.WriteLine("📦 基础批处理操作演示");
            Console.WriteLine("使用默认行为：主键自动识别为 WHERE 条件\n");

            var products = new[]
            {
                new Product { Id = 1, Name = "笔记本电脑", Price = 5999.99m, CategoryId = 1, CreatedAt = DateTime.Now },
                new Product { Id = 2, Name = "无线鼠标", Price = 199.99m, CategoryId = 2, CreatedAt = DateTime.Now },
                new Product { Id = 3, Name = "机械键盘", Price = 899.99m, CategoryId = 2, CreatedAt = DateTime.Now }
            };

            try
            {
                // 批量插入
                var insertCount = await service.BatchInsertAsync(products);
                Console.WriteLine($"✅ 批量插入: {insertCount} 个产品");
                
                // 批量更新（价格调整）
                foreach (var product in products)
                    product.Price *= 1.1m; // 涨价10%
                    
                var updateCount = await service.BatchUpdateAsync(products);
                Console.WriteLine($"✅ 批量更新: {updateCount} 个产品价格");
                
                // 通用批处理（方法名推断）
                var newProducts = new[]
                {
                    new Product { Id = 4, Name = "显示器", Price = 2499.99m, CategoryId = 1, CreatedAt = DateTime.Now }
                };
                var addCount = await service.BatchAddProductsAsync(newProducts);
                Console.WriteLine($"✅ BatchAddProducts (推断为INSERT): {addCount} 个产品");
                
                // 批量删除
                var deleteCount = await service.BatchDeleteAsync(new[] { products[0] });
                Console.WriteLine($"✅ 批量删除: {deleteCount} 个产品");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 操作失败: {ex.Message}");
            }
            
            Console.WriteLine();
        }

        private static async Task DemoPreciseControlFeatures(
            IProductBatchService productService,
            IUserProfileBatchService userService,
            IInventoryBatchService inventoryService,
            IOrderBatchService orderService)
        {
            Console.WriteLine("🎯 精确控制特性演示");
            Console.WriteLine("使用 [Where] 和 [Set] 特性精确控制 SQL 生成\n");

            // 1. 精确控制的产品更新
            Console.WriteLine("1. 产品精确更新演示:");
            Console.WriteLine("   WHERE: Id = @Id");
            Console.WriteLine("   SET: Price = @Price, Stock = @Stock");
            
            var productUpdates = new[]
            {
                new ProductUpdate { Id = 1, Name = "笔记本电脑", Price = 6999.99m, Stock = 50, CreatedAt = DateTime.Now },
                new ProductUpdate { Id = 2, Name = "无线鼠标", Price = 299.99m, Stock = 200, CreatedAt = DateTime.Now }
            };

            try
            {
                var updateCount = await productService.BatchUpdatePreciseAsync(productUpdates);
                Console.WriteLine($"   ✅ 成功更新 {updateCount} 个产品（CreatedAt 字段被排除）\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ 更新失败: {ex.Message}\n");
            }

            // 2. 基于非主键的用户更新
            Console.WriteLine("2. 基于用户名的用户信息更新:");
            Console.WriteLine("   WHERE: UserName = @UserName");
            Console.WriteLine("   SET: Email = @Email, LastLoginAt = @LastLoginAt");
            
            var userUpdates = new[]
            {
                new UserProfileUpdate { UserName = "zhang_san", Email = "zhang.new@example.com", LastLoginAt = DateTime.Now },
                new UserProfileUpdate { UserName = "li_si", Email = "li.new@example.com", LastLoginAt = DateTime.Now }
            };

            try
            {
                var userUpdateCount = await userService.BatchUpdateUserProfilesAsync(userUpdates);
                Console.WriteLine($"   ✅ 成功更新 {userUpdateCount} 个用户信息（基于用户名而非ID）\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ 更新失败: {ex.Message}\n");
            }

            // 3. 复合 WHERE 条件的库存更新
            Console.WriteLine("3. 复合 WHERE 条件库存更新:");
            Console.WriteLine("   WHERE: ProductId = @ProductId AND WarehouseId = @WarehouseId");
            Console.WriteLine("   SET: Quantity = @Quantity, LastModified = @LastModified");
            
            var inventoryUpdates = new[]
            {
                new InventoryUpdate { ProductId = 1, WarehouseId = 101, Quantity = 100, LastModified = DateTime.Now },
                new InventoryUpdate { ProductId = 2, WarehouseId = 102, Quantity = 50, LastModified = DateTime.Now }
            };

            try
            {
                var inventoryUpdateCount = await inventoryService.BatchUpdateInventoryAsync(inventoryUpdates);
                Console.WriteLine($"   ✅ 成功更新 {inventoryUpdateCount} 条库存记录（复合主键）\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ 更新失败: {ex.Message}\n");
            }

            // 4. 自定义操作符的订单更新
            Console.WriteLine("4. 自定义操作符订单更新:");
            Console.WriteLine("   WHERE: OrderNumber = @OrderNumber AND OrderDate >= @OrderDate");
            Console.WriteLine("   SET: Status = @Status, ProcessedAt = @ProcessedAt");
            
            var orderUpdates = new[]
            {
                new OrderStatusUpdate 
                { 
                    OrderNumber = "ORD-2024-001", 
                    OrderDate = DateTime.Today.AddDays(-7), 
                    Status = "Shipped", 
                    ProcessedAt = DateTime.Now 
                }
            };

            try
            {
                var orderUpdateCount = await orderService.BatchUpdateOrderStatusAsync(orderUpdates);
                Console.WriteLine($"   ✅ 成功更新 {orderUpdateCount} 个订单状态（自定义操作符 >=）\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ 更新失败: {ex.Message}\n");
            }
        }

        private static async Task DemoPerformanceComparison(IProductBatchService service)
        {
            Console.WriteLine("⚡ 性能对比测试");
            Console.WriteLine("对比单条操作 vs 批量操作的性能差异\n");

            var testProducts = GenerateTestProducts(100);

            try
            {
                // 单条插入性能测试
                var sw1 = Stopwatch.StartNew();
                foreach (var product in testProducts)
                {
                    await service.InsertSingleAsync(product);
                }
                sw1.Stop();

                // 批量插入性能测试（先清理避免冲突）
                try { await service.BatchDeleteAsync(testProducts); } catch { }
                
                var sw2 = Stopwatch.StartNew();
                await service.BatchInsertAsync(testProducts);
                sw2.Stop();

                var improvement = sw1.ElapsedMilliseconds / (double)sw2.ElapsedMilliseconds;
                
                Console.WriteLine($"📊 性能对比结果 (100 条记录):");
                Console.WriteLine($"   单条插入: {sw1.ElapsedMilliseconds} ms ({100.0 / sw1.Elapsed.TotalSeconds:F0} 条/秒)");
                Console.WriteLine($"   批量插入: {sw2.ElapsedMilliseconds} ms ({100.0 / sw2.Elapsed.TotalSeconds:F0} 条/秒)");
                Console.WriteLine($"   🚀 性能提升: {improvement:F1}x");
                
                if (improvement > 10)
                    Console.WriteLine($"   🔥 惊人的性能提升！使用了原生 DbBatch");
                else if (improvement > 5)
                    Console.WriteLine($"   ⚡ 显著性能提升！");
                else if (improvement > 2)
                    Console.WriteLine($"   📈 良好性能提升！");
                
                // 清理测试数据
                try { await service.BatchDeleteAsync(testProducts); } catch { }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 性能测试失败: {ex.Message}");
            }
            
            Console.WriteLine();
        }

        private static async Task DemoAdvancedFeatures(IProductBatchService service)
        {
            Console.WriteLine("🔧 高级功能演示\n");

            // 1. 同步批处理操作
            Console.WriteLine("1. 同步批处理操作:");
            var syncProducts = new[]
            {
                new Product { Id = 100, Name = "同步产品1", Price = 99.99m, CategoryId = 1, CreatedAt = DateTime.Now },
                new Product { Id = 101, Name = "同步产品2", Price = 199.99m, CategoryId = 2, CreatedAt = DateTime.Now }
            };

            try
            {
                var syncCount = service.BatchInsert(syncProducts);
                Console.WriteLine($"   ✅ 同步插入 {syncCount} 个产品\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ 同步操作失败: {ex.Message}\n");
            }

            // 2. 大批量数据处理
            Console.WriteLine("2. 大批量数据处理演示:");
            var largeBatch = GenerateTestProducts(1000);
            
            Console.WriteLine($"   📊 准备批量插入 {largeBatch.Count} 个产品");
            var start = DateTime.Now;
            
            try
            {
                // 分批处理大数据集
                const int batchSize = 100;
                var totalInserted = 0;
                for (int i = 0; i < largeBatch.Count; i += batchSize)
                {
                    var batch = largeBatch.Skip(i).Take(batchSize);
                    var batchCount = await service.BatchInsertAsync(batch);
                    totalInserted += batchCount;
                }
                
                var elapsed = DateTime.Now - start;
                Console.WriteLine($"   ✅ 成功插入 {totalInserted} 个产品");
                Console.WriteLine($"   ⏱️ 耗时: {elapsed.TotalMilliseconds:F2} ms");
                Console.WriteLine($"   🚀 平均处理速度: {totalInserted / elapsed.TotalSeconds:F0} 条/秒");
                
                // 清理大批量测试数据
                await service.BatchDeleteAsync(largeBatch);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ 大批量处理失败: {ex.Message}");
            }
            
            Console.WriteLine();
        }

        #endregion

        #region 辅助方法

        private static List<Product> GenerateTestProducts(int count, int startId = 1000)
        {
            var products = new List<Product>();
            var random = new Random();
            var categories = new[] { 1, 2, 3, 4, 5 };
            var productNames = new[]
            {
                "笔记本电脑", "台式电脑", "显示器", "键盘", "鼠标",
                "耳机", "音响", "摄像头", "打印机", "扫描仪",
                "平板电脑", "手机", "智能手表", "充电器", "数据线"
            };

            for (int i = 0; i < count; i++)
            {
                products.Add(new Product
                {
                    Id = startId + i,
                    Name = $"{productNames[random.Next(productNames.Length)]} {startId + i:D4}",
                    Price = (decimal)(random.NextDouble() * 5000 + 100),
                    CategoryId = categories[random.Next(categories.Length)],
                    CreatedAt = DateTime.Now.AddDays(-random.Next(365)),
                    IsActive = random.Next(10) > 1 // 90% 概率为 true
                });
            }

            return products;
        }

        /// <summary>
        /// 快速性能验证
        /// </summary>
        public static async Task QuickPerformanceTest(DbConnection connection)
        {
            var service = new ProductBatchRepository(connection);
            var products = GenerateTestProducts(50, 2000);

            Console.WriteLine("⚡ 快速性能验证 (50 条记录)");

            var sw1 = Stopwatch.StartNew();
            foreach (var product in products)
                await service.InsertSingleAsync(product);
            sw1.Stop();

            try { await service.BatchDeleteAsync(products); } catch { }
            
            var sw2 = Stopwatch.StartNew();
            await service.BatchInsertAsync(products);
            sw2.Stop();

            var improvement = sw1.ElapsedMilliseconds / (double)sw2.ElapsedMilliseconds;
            
            Console.WriteLine($"单条: {sw1.ElapsedMilliseconds}ms, 批量: {sw2.ElapsedMilliseconds}ms, 提升: {improvement:F1}x");
            
            try { await service.BatchDeleteAsync(products); } catch { }
        }

        /// <summary>
        /// 展示新特性的优势
        /// </summary>
        public static void ShowFeatureAdvantages()
        {
            Console.WriteLine("🎯 新特性优势总结:\n");
            
            Console.WriteLine("1. 🚀 原生 DbBatch 支持:");
            Console.WriteLine("   - 使用真正的 DbConnection.CreateBatch()");
            Console.WriteLine("   - 在支持的数据库上获得 10-100 倍性能提升");
            Console.WriteLine("   - 智能检测和自动降级机制");
            
            Console.WriteLine("\n2. 🎯 精确控制特性:");
            Console.WriteLine("   - [Where] 特性明确指定查询条件字段");
            Console.WriteLine("   - [Set] 特性明确指定要更新的字段");
            Console.WriteLine("   - 支持自定义操作符和复合条件");
            
            Console.WriteLine("\n3. 🔧 灵活配置:");
            Console.WriteLine("   - 支持基于非主键字段的更新");
            Console.WriteLine("   - 复合 WHERE 条件支持");
            Console.WriteLine("   - 方法名自动推断操作类型");
            
            Console.WriteLine("\n4. 🛡️ 企业级特性:");
            Console.WriteLine("   - 类型安全，编译时检查");
            Console.WriteLine("   - 完善的错误处理和验证");
            Console.WriteLine("   - 事务和超时支持");
            Console.WriteLine("   - 向后完全兼容");
            
            Console.WriteLine("\n5. 🗄️ 数据库兼容性:");
            Console.WriteLine("   - SQL Server 2012+, PostgreSQL 3.0+, MySQL 8.0+ (原生 DbBatch)");
            Console.WriteLine("   - SQLite 及其他数据库 (自动降级)");
        }

        #endregion
    }
}
