using System.Collections.Generic;
using System.Data.Common;
using Sqlx.Annotations;

public class SimpleEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public interface ISimpleService
{
    [SqlExecuteType(SqlExecuteTypes.Insert, "simple_entities")]
    int Create(SimpleEntity entity);
}

[RepositoryFor(typeof(ISimpleService))]
public partial class SimpleRepository
{
    private readonly DbConnection connection;
    
    public SimpleRepository(DbConnection connection)
    {
        this.connection = connection;
    }
}
