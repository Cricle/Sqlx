using SqlxDemo.Models;
using Sqlx.Annotations;
using Microsoft.Data.Sqlite;
using System.Data.Common;

namespace SqlxDemo.Services;

/// <summary>
/// 产品服务实现 - 演示复杂SQL和多数据库方言
/// </summary>
public partial class ProductService : IProductService
{
    private readonly DbConnection connection;

    public ProductService(DbConnection connection)
    {
        this.connection = connection;
    }

    [Sqlx("SELECT * FROM [product] WHERE [is_active] = 1 ORDER BY [name]")]
    public partial Task<IList<Product>> GetActiveProductsAsync();

    [Sqlx("SELECT * FROM [product] WHERE [id] = @id")]
    public partial Task<Product?> GetProductByIdAsync(int id);

    [Sqlx("SELECT * FROM [product] WHERE ([name] LIKE @search OR [description] LIKE @search OR [tags] LIKE @search) AND [is_active] = 1 ORDER BY [name] LIMIT @limit OFFSET @offset")]
    public partial Task<IList<Product>> SearchProductsAsync(string search, int limit, int offset);

    [Sqlx("SELECT * FROM [product] WHERE [price] BETWEEN @min_price AND @max_price AND [is_active] = 1 ORDER BY [price]")]
    public partial Task<IList<Product>> GetProductsByPriceRangeAsync(decimal minPrice, decimal maxPrice);

    [Sqlx("UPDATE [product] SET [stock_quantity] = [stock_quantity] + @quantity WHERE [id] = @product_id")]
    public partial Task<int> UpdateStockAsync(int productId, int quantity);

    [Sqlx("SELECT COUNT(*) FROM [product] WHERE [is_active] = 1")]
    public partial Task<int> GetActiveProductCountAsync();

    [Sqlx("SELECT COUNT(*) FROM [product] WHERE [category_id] = @category_id AND [is_active] = 1")]
    public partial Task<int> GetProductCountByCategoryAsync(int categoryId);
}

/// <summary>
/// 订单服务实现 - 演示事务处理和复杂业务逻辑
/// </summary>
public partial class OrderService : IOrderService
{
    private readonly DbConnection connection;

    public OrderService(DbConnection connection)
    {
        this.connection = connection;
    }

    [Sqlx("INSERT INTO [order] ([order_number], [user_id], [total_amount], [discount_amount], [shipping_cost], [status], [created_at], [shipping_address], [billing_address], [notes]) VALUES (@order_number, @user_id, @total_amount, @discount_amount, @shipping_cost, @status, @created_at, @shipping_address, @billing_address, @notes)")]
    public partial Task<int> CreateOrderAsync(string orderNumber, int userId, decimal totalAmount,
        decimal discountAmount, decimal shippingCost, int status, DateTime createdAt,
        string shippingAddress, string billingAddress, string? notes);

    [Sqlx("UPDATE [order] SET [status] = @status WHERE [id] = @order_id")]
    public partial Task<int> UpdateOrderStatusAsync(int orderId, int status);

    [Sqlx(@"SELECT * FROM [order] 
            WHERE [user_id] = @user_id 
            ORDER BY [created_at] DESC 
            LIMIT @page_size OFFSET @offset")]
    public partial Task<IList<Order>> GetUserOrdersAsync(int userId, int pageSize, int offset);

    [Sqlx("SELECT COUNT(*) FROM [order]")]
    public partial Task<int> GetTotalOrderCountAsync();

    [Sqlx("SELECT COALESCE(SUM([total_amount]), 0) FROM [order] WHERE [status] != 5")]
    public partial Task<decimal> GetTotalSalesAsync();

    /// <summary>
    /// 业务逻辑方法 - 演示事务处理
    /// </summary>
    public async Task<int> CreateOrderWithProductAsync(string orderNumber, int userId,
        int productId, int quantity, decimal unitPrice)
    {
        using var transaction = await connection.BeginTransactionAsync();
        try
        {
            // 创建订单
            var rowsAffected = await CreateOrderAsync(
                orderNumber, userId, unitPrice * quantity, 0, 0, 1,
                DateTime.Now, "默认地址", "默认地址", null);

            // 更新库存
            var productService = new ProductService(connection);
            await productService.UpdateStockAsync(productId, -quantity);

            await transaction.CommitAsync();
            return rowsAffected; // 返回受影响的行数而不是ID
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}