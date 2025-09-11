using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

namespace Sqlx.PerformanceTests
{
    /// <summary>
    /// 性能测试：验证 Primary Constructor 和 Record 类型的性能表现
    /// </summary>
    [TestClass]
    public class PrimaryConstructorPerformanceTests
    {
        private SQLiteConnection? _connection;
        private const int TestDataSize = 10000; // 测试数据量

        [TestInitialize]
        public void Setup()
        {
            _connection = new SQLiteConnection("Data Source=:memory:");
            _connection.Open();
            
            // 创建测试表
            CreateTestTables();
            
            // 插入测试数据
            InsertTestData();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _connection?.Close();
            _connection?.Dispose();
        }

        /// <summary>
        /// 性能测试：传统类 vs Record 类型的数据读取性能
        /// </summary>
        [TestMethod]
        public void Performance_TraditionalClass_vs_Record_DataReading()
        {
            var traditionalRepo = new TraditionalProductRepository(_connection!);
            var recordRepo = new RecordProductRepository(_connection!);

            // 预热
            traditionalRepo.GetAllProducts().Take(100).ToList();
            recordRepo.GetAllProducts().Take(100).ToList();

            // 测试传统类性能
            var sw1 = Stopwatch.StartNew();
            var traditionalResults = traditionalRepo.GetAllProducts();
            sw1.Stop();
            var traditionalTime = sw1.ElapsedMilliseconds;

            // 测试 Record 类型性能
            var sw2 = Stopwatch.StartNew();
            var recordResults = recordRepo.GetAllProducts();
            sw2.Stop();
            var recordTime = sw2.ElapsedMilliseconds;

            // 验证结果数量相同
            Assert.AreEqual(traditionalResults.Count, recordResults.Count);
            Assert.AreEqual(TestDataSize, traditionalResults.Count);

            // 输出性能对比
            Console.WriteLine($"传统类读取时间: {traditionalTime}ms");
            Console.WriteLine($"Record类型读取时间: {recordTime}ms");
            Console.WriteLine($"性能差异: {(double)traditionalTime / recordTime:F2}x");

            // Record 类型应该有相近或更好的性能
            Assert.IsTrue(recordTime <= traditionalTime * 1.2, 
                "Record 类型性能不应显著低于传统类");
        }

        /// <summary>
        /// 性能测试：Primary Constructor 的实体创建性能
        /// </summary>
        [TestMethod]
        public void Performance_PrimaryConstructor_EntityCreation()
        {
            var primaryConstructorRepo = new PrimaryConstructorOrderRepository(_connection!);
            var traditionalRepo = new TraditionalOrderRepository(_connection!);

            // 预热
            primaryConstructorRepo.GetAllOrders().Take(100).ToList();
            traditionalRepo.GetAllOrders().Take(100).ToList();

            // 测试 Primary Constructor 性能
            var sw1 = Stopwatch.StartNew();
            var primaryResults = primaryConstructorRepo.GetAllOrders();
            sw1.Stop();
            var primaryTime = sw1.ElapsedMilliseconds;

            // 测试传统类性能
            var sw2 = Stopwatch.StartNew();
            var traditionalResults = traditionalRepo.GetAllOrders();
            sw2.Stop();
            var traditionalTime = sw2.ElapsedMilliseconds;

            // 验证结果
            Assert.AreEqual(primaryResults.Count, traditionalResults.Count);
            Assert.AreEqual(TestDataSize, primaryResults.Count);

            // 输出性能对比
            Console.WriteLine($"主构造函数读取时间: {primaryTime}ms");
            Console.WriteLine($"传统类读取时间: {traditionalTime}ms");
            Console.WriteLine($"性能提升: {(double)traditionalTime / primaryTime:F2}x");

            // Primary Constructor 应该有更好的性能
            Assert.IsTrue(primaryTime <= traditionalTime * 1.1, 
                "Primary Constructor 性能不应显著低于传统类");
        }

        /// <summary>
        /// 内存使用测试：对比不同实体类型的内存占用
        /// </summary>
        [TestMethod]
        public void Performance_Memory_Usage_Comparison()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var initialMemory = GC.GetTotalMemory(false);

            // 测试传统类内存使用
            var traditionalRepo = new TraditionalProductRepository(_connection!);
            var traditionalData = traditionalRepo.GetAllProducts();
            var traditionalMemory = GC.GetTotalMemory(false) - initialMemory;

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var midMemory = GC.GetTotalMemory(false);

            // 测试 Record 类型内存使用
            var recordRepo = new RecordProductRepository(_connection!);
            var recordData = recordRepo.GetAllProducts();
            var recordMemory = GC.GetTotalMemory(false) - midMemory;

            Console.WriteLine($"传统类内存使用: {traditionalMemory / 1024.0:F2} KB");
            Console.WriteLine($"Record类型内存使用: {recordMemory / 1024.0:F2} KB");
            Console.WriteLine($"内存效率比: {(double)traditionalMemory / recordMemory:F2}x");

            // 验证数据正确性
            Assert.AreEqual(traditionalData.Count, recordData.Count);
            
            // Record 类型通常更内存高效
            Assert.IsTrue(recordMemory <= traditionalMemory * 1.3, 
                "Record 类型内存使用不应显著高于传统类");
        }

        #region 测试实体定义

        // 传统产品类
        public class TraditionalProduct
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public decimal Price { get; set; }
            public int CategoryId { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        // Record 产品类
        public record RecordProduct(int Id, string Name, decimal Price, int CategoryId)
        {
            public DateTime CreatedAt { get; set; } = DateTime.Now;
        }

        // Primary Constructor 订单类
        public class PrimaryConstructorOrder(int id, string customerName)
        {
            public int Id { get; } = id;
            public string CustomerName { get; } = customerName;
            public DateTime OrderDate { get; set; } = DateTime.Now;
            public decimal TotalAmount { get; set; }
        }

        // 传统订单类
        public class TraditionalOrder
        {
            public int Id { get; set; }
            public string CustomerName { get; set; } = string.Empty;
            public DateTime OrderDate { get; set; }
            public decimal TotalAmount { get; set; }
        }

        #endregion

        #region Repository 接口定义

        public interface ITraditionalProductService
        {
            IList<TraditionalProduct> GetAllProducts();
        }

        public interface IRecordProductService
        {
            IList<RecordProduct> GetAllProducts();
        }

        public interface IPrimaryConstructorOrderService
        {
            IList<PrimaryConstructorOrder> GetAllOrders();
        }

        public interface ITraditionalOrderService
        {
            IList<TraditionalOrder> GetAllOrders();
        }

        #endregion

        #region Repository 实现

        [RepositoryFor(typeof(ITraditionalProductService))]
        public partial class TraditionalProductRepository : ITraditionalProductService
        {
            private readonly IDbConnection connection;

            public TraditionalProductRepository(IDbConnection connection)
            {
                this.connection = connection;
            }

            [Sqlx("SELECT * FROM Products")]
            public partial IList<TraditionalProduct> GetAllProducts();
        }

        [RepositoryFor(typeof(IRecordProductService))]
        public partial class RecordProductRepository : IRecordProductService
        {
            private readonly IDbConnection connection;

            public RecordProductRepository(IDbConnection connection)
            {
                this.connection = connection;
            }

            [Sqlx("SELECT * FROM Products")]
            public partial IList<RecordProduct> GetAllProducts();
        }

        [RepositoryFor(typeof(IPrimaryConstructorOrderService))]
        public partial class PrimaryConstructorOrderRepository : IPrimaryConstructorOrderService
        {
            private readonly IDbConnection connection;

            public PrimaryConstructorOrderRepository(IDbConnection connection)
            {
                this.connection = connection;
            }

            [Sqlx("SELECT * FROM Orders")]
            public partial IList<PrimaryConstructorOrder> GetAllOrders();
        }

        [RepositoryFor(typeof(ITraditionalOrderService))]
        public partial class TraditionalOrderRepository : ITraditionalOrderService
        {
            private readonly IDbConnection connection;

            public TraditionalOrderRepository(IDbConnection connection)
            {
                this.connection = connection;
            }

            [Sqlx("SELECT * FROM Orders")]
            public partial IList<TraditionalOrder> GetAllOrders();
        }

        #endregion

        #region 测试数据设置

        private void CreateTestTables()
        {
            var cmd = _connection!.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE Products (
                    Id INTEGER PRIMARY KEY,
                    Name TEXT NOT NULL,
                    Price DECIMAL(10,2) NOT NULL,
                    CategoryId INTEGER NOT NULL,
                    CreatedAt DATETIME NOT NULL
                );

                CREATE TABLE Orders (
                    Id INTEGER PRIMARY KEY,
                    CustomerName TEXT NOT NULL,
                    OrderDate DATETIME NOT NULL,
                    TotalAmount DECIMAL(10,2) NOT NULL
                );
            ";
            cmd.ExecuteNonQuery();
        }

        private void InsertTestData()
        {
            var random = new Random(42); // 固定种子确保可重复性
            
            // 插入产品数据
            for (int i = 1; i <= TestDataSize; i++)
            {
                var cmd = _connection!.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO Products (Id, Name, Price, CategoryId, CreatedAt) 
                    VALUES (@id, @name, @price, @categoryId, @createdAt)";
                
                cmd.Parameters.Add(new SQLiteParameter("@id", i));
                cmd.Parameters.Add(new SQLiteParameter("@name", $"Product {i}"));
                cmd.Parameters.Add(new SQLiteParameter("@price", random.Next(10, 1000) + random.NextDouble()));
                cmd.Parameters.Add(new SQLiteParameter("@categoryId", random.Next(1, 10)));
                cmd.Parameters.Add(new SQLiteParameter("@createdAt", DateTime.Now.AddDays(-random.Next(365))));
                
                cmd.ExecuteNonQuery();
            }

            // 插入订单数据
            for (int i = 1; i <= TestDataSize; i++)
            {
                var cmd = _connection!.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO Orders (Id, CustomerName, OrderDate, TotalAmount) 
                    VALUES (@id, @customerName, @orderDate, @totalAmount)";
                
                cmd.Parameters.Add(new SQLiteParameter("@id", i));
                cmd.Parameters.Add(new SQLiteParameter("@customerName", $"Customer {i}"));
                cmd.Parameters.Add(new SQLiteParameter("@orderDate", DateTime.Now.AddDays(-random.Next(30))));
                cmd.Parameters.Add(new SQLiteParameter("@totalAmount", random.Next(50, 5000) + random.NextDouble()));
                
                cmd.ExecuteNonQuery();
            }
        }

        #endregion
    }
}
