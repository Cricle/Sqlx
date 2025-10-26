namespace Sqlx.Tests.ExpressionToSql;

public class ExprTestModel
{
    public long id { get; set; }
    public string name { get; set; } = string.Empty;
    public int age { get; set; }
    public double salary { get; set; }
    public bool is_active { get; set; }
    public string created_at { get; set; } = string.Empty;
    public string category { get; set; } = string.Empty;
    public double score { get; set; }
    public string department { get; set; } = string.Empty;
    public string email { get; set; } = string.Empty;
}
