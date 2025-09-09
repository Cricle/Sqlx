using System.Collections.Generic;
using System.Data.Common;
using Sqlx.Annotations;

namespace Sqlx.RepositoryExample;

/// <summary>
/// 测试泛型Repository的简单示例
/// </summary>
public interface ISimpleRepository<T> where T : class
{
    /// <summary>
    /// 获取所有实体
    /// </summary>
    IList<T> GetAll();
    
    /// <summary>
    /// 创建实体
    /// </summary>
    int Create(T entity);
}

/// <summary>
/// 实现泛型Repository
/// </summary>
[RepositoryFor(typeof(ISimpleRepository<User>))]
public partial class TestGenericRepository : ISimpleRepository<User>
{
    private readonly DbConnection connection;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    public TestGenericRepository(DbConnection connection)
    {
        this.connection = connection;
    }
}
