using SqlxDemo.Models;
using Sqlx.Annotations;

namespace SqlxDemo.Services;

/// <summary>
/// 产品服务接口 - 演示复杂查询
/// </summary>
public interface IProductService
{
    /// <summary>
    /// 获取所有活跃产品
    /// </summary>
    [Sqlx("SELECT * FROM [product] WHERE [is_active] = 1 ORDER BY [name]")]
    Task<IList<Product>> GetActiveProductsAsync();

    /// <summary>
    /// 根据ID获取产品
    /// </summary>
    [Sqlx("SELECT * FROM [product] WHERE [id] = @id")]
    Task<Product?> GetProductByIdAsync(int id);

    /// <summary>
    /// 搜索产品 - 演示全文搜索
    /// </summary>
    [Sqlx("SELECT * FROM [product] WHERE ([name] LIKE @search OR [description] LIKE @search OR [tags] LIKE @search) AND [is_active] = 1 ORDER BY [name] LIMIT @limit OFFSET @offset")]
    Task<IList<Product>> SearchProductsAsync(string search, int limit, int offset);

    /// <summary>
    /// 获取价格范围内的产品 - 演示范围查询
    /// </summary>
    [Sqlx("SELECT * FROM [product] WHERE [price] BETWEEN @min_price AND @max_price AND [is_active] = 1 ORDER BY [price]")]
    Task<IList<Product>> GetProductsByPriceRangeAsync(decimal minPrice, decimal maxPrice);

    /// <summary>
    /// 更新库存 - 演示UPDATE操作
    /// </summary>
    [Sqlx("UPDATE [product] SET [stock_quantity] = [stock_quantity] + @quantity WHERE [id] = @product_id")]
    Task<int> UpdateStockAsync(int productId, int quantity);

    /// <summary>
    /// 获取产品总数
    /// </summary>
    [Sqlx("SELECT COUNT(*) FROM [product] WHERE [is_active] = 1")]
    Task<int> GetActiveProductCountAsync();

    /// <summary>
    /// 根据分类获取产品数量
    /// </summary>
    [Sqlx("SELECT COUNT(*) FROM [product] WHERE [category_id] = @category_id AND [is_active] = 1")]
    Task<int> GetProductCountByCategoryAsync(int categoryId);
}

/// <summary>
/// 订单服务接口 - 演示事务和复杂业务逻辑
/// </summary>
public interface IOrderService
{
    /// <summary>
    /// 创建订单
    /// </summary>
    [Sqlx("INSERT INTO [order] ([order_number], [user_id], [total_amount], [discount_amount], [shipping_cost], [status], [created_at], [shipping_address], [billing_address], [notes]) VALUES (@order_number, @user_id, @total_amount, @discount_amount, @shipping_cost, @status, @created_at, @shipping_address, @billing_address, @notes)")]
    Task<int> CreateOrderAsync(string orderNumber, int userId, decimal totalAmount,
        decimal discountAmount, decimal shippingCost, int status, DateTime createdAt,
        string shippingAddress, string billingAddress, string? notes);

    /// <summary>
    /// 更新订单状态
    /// </summary>
    [Sqlx("UPDATE [order] SET [status] = @status WHERE [id] = @order_id")]
    Task<int> UpdateOrderStatusAsync(int orderId, int status);

    /// <summary>
    /// 获取用户订单 - 演示分页
    /// </summary>
    [Sqlx(@"SELECT * FROM [order] 
            WHERE [user_id] = @user_id 
            ORDER BY [created_at] DESC 
            LIMIT @page_size OFFSET @offset")]
    Task<IList<Order>> GetUserOrdersAsync(int userId, int pageSize, int offset);

    /// <summary>
    /// 获取订单总数
    /// </summary>
    [Sqlx("SELECT COUNT(*) FROM [order]")]
    Task<int> GetTotalOrderCountAsync();

    /// <summary>
    /// 获取销售总额
    /// </summary>
    [Sqlx("SELECT COALESCE(SUM([total_amount]), 0) FROM [order] WHERE [status] != 5")]
    Task<decimal> GetTotalSalesAsync();
}