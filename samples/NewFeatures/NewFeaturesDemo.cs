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
    // ADO.NET BatchCommand 批量操作
    [SqlExecuteType(SqlExecuteTypes.BatchCommand, "products")]
    Task<int> BatchInsertAsync(IEnumerable<Product> products);

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
            new Product { Name = "产品1", Price = 10.99m, CategoryId = 1 },
            new Product { Name = "产品2", Price = 20.99m, CategoryId = 2 },
            new Product { Name = "产品3", Price = 30.99m, CategoryId = 1 }
        };

        var count = await service.BatchInsertAsync(products);
        System.Console.WriteLine($"批量插入 {count} 个产品");
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
