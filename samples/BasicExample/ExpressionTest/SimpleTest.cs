using System;
using System.Collections.Generic;
using Sqlx.Annotations;

namespace Sqlx.BasicExample;

public class TestEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public interface ITestService
{
    IList<TestEntity> GetAll();
    
    TestEntity? GetById(int id);
    
    int Create(TestEntity entity);
}

[RepositoryFor(typeof(ITestService))]
public partial class SimpleTestRepository : ITestService
{
    private readonly System.Data.Common.DbConnection connection;

    public SimpleTestRepository(System.Data.Common.DbConnection connection)
    {
        this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }
    
    // 所有方法由 RepositoryFor 源生成器自动实现
}

