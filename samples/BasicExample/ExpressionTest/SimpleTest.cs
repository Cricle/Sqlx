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
    [Sqlx("SELECT * FROM TestEntity")]
    IList<TestEntity> GetAll();
    
    [Sqlx("SELECT * FROM TestEntity WHERE Id = @id")]
    TestEntity? GetById(int id);
    
    [SqlExecuteType(SqlExecuteTypes.Insert, "TestEntity")]
    int Create(TestEntity entity);
}

[RepositoryFor(typeof(ITestService))]
public partial class SimpleTestRepository
{
    private readonly System.Data.Common.DbConnection connection;

    public SimpleTestRepository(System.Data.Common.DbConnection connection)
    {
        this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }
    
    // 此类的方法将被源生成器自动实现
}

