using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using Sqlx.SqlGen;

namespace NewFeaturesDemo;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int CategoryId { get; set; }
}

public interface IProductService
{
    // 具体批处理操作类型 - 使用原生 DbBatch
    [SqlExecuteType(SqlExecuteTypes.BatchInsert, "products")]
    Task<int> BatchInsertAsync(IEnumerable<Product> products);
    
    [SqlExecuteType(SqlExecuteTypes.BatchUpdate, "products")]
    Task<int> BatchUpdateAsync(IEnumerable<Product> products);
    
    [SqlExecuteType(SqlExecuteTypes.BatchDelete, "products")]
    Task<int> BatchDeleteAsync(IEnumerable<Product> products);

    // 通用批处理 - 根据方法名推断操作类型
    [SqlExecuteType(SqlExecuteTypes.BatchCommand, "products")]
    Task<int> BatchAddProductsAsync(IEnumerable<Product> products);

    // ExpressionToSql 支持 mod 运算
    [Sqlx]
    IList<Product> GetProducts([ExpressionToSql] ExpressionToSql<Product> filter);
}

[RepositoryFor(typeof(IProductService))]
public partial class ProductRepository : IProductService
{
    private readonly DbConnection connection;
    public ProductRepository(DbConnection connection) => this.connection = connection;
}

public class Demo
{
    public static async Task RunBatchDemo(IProductService service)
    {
        var products = new[]
        {
            new Product { Id = 1, Name = "产品1", Price = 10.99m, CategoryId = 1 },
            new Product { Id = 2, Name = "产品2", Price = 20.99m, CategoryId = 2 },
            new Product { Id = 3, Name = "产品3", Price = 30.99m, CategoryId = 1 }
        };

        System.Console.WriteLine("=== 原生 DbBatch 批处理演示 ===");
        
        // 批量插入
        var insertCount = await service.BatchInsertAsync(products);
        System.Console.WriteLine($"✅ 批量插入 {insertCount} 个产品（使用原生 DbBatch）");
        
        // 批量更新
        foreach (var product in products)
            product.Price *= 1.1m; // 涨价10%
        var updateCount = await service.BatchUpdateAsync(products);
        System.Console.WriteLine($"✅ 批量更新 {updateCount} 个产品价格");
        
        // 通用批处理（方法名推断）
        var newProducts = new[]
        {
            new Product { Id = 4, Name = "新产品1", Price = 99.99m, CategoryId = 2 }
        };
        var addCount = await service.BatchAddProductsAsync(newProducts);
        System.Console.WriteLine($"✅ BatchAddProducts 推断为 INSERT，添加 {addCount} 个产品");
        
        // 批量删除
        var deleteCount = await service.BatchDeleteAsync(new[] { products[0] });
        System.Console.WriteLine($"✅ 批量删除 {deleteCount} 个产品");
        
        System.Console.WriteLine("🚀 性能: 原生 DbBatch 比单条操作快 10-100 倍");
    }

    public static void RunModDemo(IProductService service)
    {
        // 偶数 ID 产品
        var evenProducts = service.GetProducts(
            ExpressionToSql<Product>.ForSqlServer()
                .Where(p => p.Id % 2 == 0)
                .Where(p => p.Price > 15)
                .OrderBy(p => p.Name)
        );

        // 分组查询（每10个一组的第一个）
        var groupLeaders = service.GetProducts(
            ExpressionToSql<Product>.ForSqlServer()
                .Where(p => p.Id % 10 == 1)
                .OrderBy(p => p.Id)
        );

        System.Console.WriteLine($"偶数产品: {evenProducts.Count}, 分组首项: {groupLeaders.Count}");
    }
}
