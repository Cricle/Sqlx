using System.ComponentModel.DataAnnotations;
using Sqlx.Annotations;

namespace Sqlx.Benchmarks.Models;

/// <summary>
/// Benchmark entity for performance testing.
/// Uses source generator to create EntityProvider, ResultReader, and ParameterBinder.
/// </summary>
[SqlxEntity]
[SqlxParameter]
[TableName("users")]
public class BenchmarkUser
{
    [Key]
    public long Id { get; set; }
    
    public string Name { get; set; } = string.Empty;
    
    public string Email { get; set; } = string.Empty;
    
    public int Age { get; set; }
    
    public bool IsActive { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime? UpdatedAt { get; set; }
    
    public decimal Balance { get; set; }
    
    public string? Description { get; set; }
    
    public int Score { get; set; }
}
