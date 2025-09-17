using Sqlx.Annotations;

namespace SqlxDemo.Models;

[TableName("user")]
public class User
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public int Age { get; set; }
    public decimal Salary { get; set; }
    public int DepartmentId { get; set; }
    public bool IsActive { get; set; }
    public DateTime HireDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal? Bonus { get; set; }
    public double PerformanceRating { get; set; }
}

