using Sqlx.Annotations;

namespace SqlxDemo.Models;

[TableName("Department")]
public class Department
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Budget { get; set; }
    public int? ManagerId { get; set; }
}

