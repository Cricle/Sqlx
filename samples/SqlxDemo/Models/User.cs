using Sqlx.Annotations;

namespace SqlxDemo.Models;

[TableName("user")]
public class User
{
    public long Id { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public long Age { get; set; }
    public decimal Salary { get; set; }
    public long DepartmentId { get; set; }
    public bool IsActive { get; set; }
    public DateTime HireDate { get; set; }
    public decimal? Bonus { get; set; }
    public double PerformanceRating { get; set; }
}

