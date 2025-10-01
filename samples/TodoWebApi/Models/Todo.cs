using Sqlx.Annotations;
using System.ComponentModel.DataAnnotations;

namespace TodoWebApi.Models;

/// <summary>
/// TODO项目模型 - 演示Sqlx ORM的核心功能
/// 支持多种数据类型、索引、约束和AOT编译
/// </summary>
[TableName("todos")]
public record Todo
{
    /// <summary>主键ID</summary>
    [Key]
    public long Id { get; init; }

    /// <summary>TODO标题</summary>
    [Required]
    [StringLength(200)]
    public string Title { get; init; } = string.Empty;

    /// <summary>TODO描述</summary>
    public string? Description { get; init; }

    /// <summary>是否完成</summary>
    public bool IsCompleted { get; init; }

    /// <summary>优先级（1-低，2-中，3-高，4-紧急）</summary>
    [Range(1, 4)]
    public int Priority { get; init; } = 2;

    /// <summary>截止日期</summary>
    public DateTime? DueDate { get; init; }

    /// <summary>创建时间</summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>更新时间</summary>
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>完成时间</summary>
    public DateTime? CompletedAt { get; init; }

    /// <summary>标签（JSON格式存储）</summary>
    public string? Tags { get; init; }

    /// <summary>估计工作时间（分钟）</summary>
    public int? EstimatedMinutes { get; init; }

    /// <summary>实际工作时间（分钟）</summary>
    public int? ActualMinutes { get; init; }
}

/// <summary>
/// TODO分类模型
/// </summary>
[TableName("categories")]
public record Category
{
    [Key]
    public long Id { get; init; }

    [Required]
    [StringLength(100)]
    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    [StringLength(7)] // #RRGGBB格式
    public string Color { get; init; } = "#007bff";

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    public bool IsActive { get; init; } = true;
}

/// <summary>
/// TODO与分类的关联模型
/// </summary>
[TableName("todo_categories")]
public record TodoCategory
{
    [Key]
    public long Id { get; init; }

    public long TodoId { get; init; }

    public long CategoryId { get; init; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// TODO统计信息（用于演示聚合查询）
/// </summary>
public record TodoStats
{
    public int TotalTodos { get; init; }
    public int CompletedTodos { get; init; }
    public int PendingTodos { get; init; }
    public int HighPriorityTodos { get; init; }
    public int OverdueTodos { get; init; }
    public double CompletionRate { get; init; }
    public int TotalEstimatedMinutes { get; init; }
    public int TotalActualMinutes { get; init; }
}

/// <summary>
/// TODO查询过滤器
/// </summary>
public record TodoFilter
{
    public bool? IsCompleted { get; init; }
    public int? Priority { get; init; }
    public long? CategoryId { get; init; }
    public DateTime? DueBefore { get; init; }
    public DateTime? DueAfter { get; init; }
    public string? SearchText { get; init; }
    public string? Tags { get; init; }
    public int Skip { get; init; } = 0;
    public int Take { get; init; } = 50;
    public string? OrderBy { get; init; } = "created_at";
    public bool Descending { get; init; } = true;
}

