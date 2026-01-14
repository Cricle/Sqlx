using FreeSql.DataAnnotations;

namespace Sqlx.Benchmarks.Models;

/// <summary>
/// FreeSql entity for benchmark comparison.
/// </summary>
[Table(Name = "users")]
public class FreeSqlUser
{
    [Column(IsPrimary = true, IsIdentity = true)]
    public long Id { get; set; }
    
    [Column(Name = "name")]
    public string Name { get; set; } = string.Empty;
    
    [Column(Name = "email")]
    public string Email { get; set; } = string.Empty;
    
    [Column(Name = "age")]
    public int Age { get; set; }
    
    [Column(Name = "is_active")]
    public int IsActive { get; set; }
    
    [Column(Name = "created_at")]
    public string CreatedAt { get; set; } = string.Empty;
    
    [Column(Name = "updated_at")]
    public string? UpdatedAt { get; set; }
    
    [Column(Name = "balance")]
    public double Balance { get; set; }
    
    [Column(Name = "description")]
    public string? Description { get; set; }
    
    [Column(Name = "score")]
    public int Score { get; set; }
}
