# SQL Template Metrics

Sqlx provides built-in, high-performance metrics collection for SQL template execution monitoring. The metrics system is AOT-compatible, thread-safe, and designed for production use with minimal overhead.

## Features

- **Execution Tracking**: Count, duration (min/avg/max), and success rate for each SQL template
- **Error Monitoring**: Track error counts and types for each template
- **Global Aggregation**: Overall statistics across all templates
- **Zero Allocation**: Lock-free concurrent data structures
- **AOT Compatible**: No reflection, fully trimming-safe
- **Conditional Compilation**: Can be disabled with `SQLX_DISABLE_METRICS`

## Quick Start

```csharp
using Sqlx.Diagnostics;

// Metrics are collected automatically by generated repositories
var repo = new UserRepository(connection);
await repo.GetByIdAsync(1);
await repo.InsertAsync(new User { Name = "John" });

// View global metrics
var global = SqlTemplateMetrics.GetGlobalMetrics();
Console.WriteLine($"Total Executions: {global.TotalExecutions}");
Console.WriteLine($"Success Rate: {global.SuccessRate:F2}%");
Console.WriteLine($"Avg Execution: {global.AverageExecutionMs:F3} ms");

// View specific template metrics
var metrics = SqlTemplateMetrics.GetMetrics("UserRepository.GetByIdAsync");
if (metrics.HasValue)
{
    Console.WriteLine(MetricsFormatter.FormatTemplateMetrics(metrics.Value));
}

// Get top slowest templates
var slowest = SqlTemplateMetrics.GetSlowestTemplates(10);
Console.WriteLine(MetricsFormatter.FormatMetricsTable(slowest));
```

## API Reference

### SqlTemplateMetrics

Static class for metrics collection and retrieval.

#### Methods

- `RecordExecution(string templateKey, long elapsedTicks)` - Record successful execution
- `RecordError(string templateKey, long elapsedTicks, Exception exception)` - Record failed execution
- `GetMetrics(string templateKey)` - Get metrics for specific template
- `GetAllMetrics()` - Get metrics for all templates
- `GetGlobalMetrics()` - Get aggregated global metrics
- `GetSlowestTemplates(int count = 10)` - Get top N slowest templates
- `GetMostExecutedTemplates(int count = 10)` - Get top N most executed templates
- `GetMostErrorProneTemplates(int count = 10)` - Get templates with most errors
- `Reset()` - Clear all metrics (use with caution)

### TemplateMetricSnapshot

Immutable snapshot of metrics for a single template.

```csharp
public readonly record struct TemplateMetricSnapshot(
    string TemplateKey,
    long ExecutionCount,
    long ErrorCount,
    double SuccessRate,
    double AverageExecutionMs,
    double MinExecutionMs,
    double MaxExecutionMs,
    double TotalExecutionMs,
    IReadOnlyDictionary<string, int> ErrorTypes,
    DateTime FirstExecutionUtc,
    DateTime LastExecutionUtc);
```

### GlobalMetricSnapshot

Immutable snapshot of global aggregated metrics.

```csharp
public readonly record struct GlobalMetricSnapshot(
    long TotalExecutions,
    long TotalErrors,
    double SuccessRate,
    double AverageExecutionMs,
    int TemplateCount);
```

### MetricsFormatter

Utility class for formatting metrics output.

#### Methods

- `FormatGlobalMetrics(GlobalMetricSnapshot)` - Format global metrics as text
- `FormatTemplateMetrics(TemplateMetricSnapshot)` - Format template metrics as text
- `FormatMetricsTable(TemplateMetricSnapshot[])` - Format multiple templates as table
- `FormatAsJson(TemplateMetricSnapshot)` - Format template metrics as JSON
- `FormatAsJson(GlobalMetricSnapshot)` - Format global metrics as JSON

## Manual Instrumentation

For custom SQL execution outside generated repositories:

```csharp
using Sqlx.Diagnostics;

// Using scope (recommended)
using (var scope = MetricsExtensions.BeginMetricsScope("CustomQuery"))
{
    // Your SQL execution
    await command.ExecuteNonQueryAsync();
}

// Using helper methods
var result = MetricsExtensions.ExecuteWithMetrics("CustomQuery", () =>
{
    return command.ExecuteScalar();
});

// Manual recording
var sw = Stopwatch.GetTimestamp();
try
{
    // Your code
    SqlTemplateMetrics.RecordExecution("CustomQuery", Stopwatch.GetTimestamp() - sw);
}
catch (Exception ex)
{
    SqlTemplateMetrics.RecordError("CustomQuery", Stopwatch.GetTimestamp() - sw, ex);
    throw;
}
```

## Monitoring Dashboard Example

```csharp
public class MetricsDashboard
{
    public static void PrintDashboard()
    {
        var global = SqlTemplateMetrics.GetGlobalMetrics();
        
        Console.WriteLine("=== SQL Metrics Dashboard ===\n");
        Console.WriteLine(MetricsFormatter.FormatGlobalMetrics(global));
        
        Console.WriteLine("\n=== Top 10 Slowest Templates ===");
        var slowest = SqlTemplateMetrics.GetSlowestTemplates(10);
        Console.WriteLine(MetricsFormatter.FormatMetricsTable(slowest));
        
        Console.WriteLine("\n=== Top 10 Most Executed ===");
        var mostExecuted = SqlTemplateMetrics.GetMostExecutedTemplates(10);
        Console.WriteLine(MetricsFormatter.FormatMetricsTable(mostExecuted));
        
        Console.WriteLine("\n=== Templates with Errors ===");
        var errors = SqlTemplateMetrics.GetMostErrorProneTemplates(10);
        if (errors.Length > 0)
        {
            Console.WriteLine(MetricsFormatter.FormatMetricsTable(errors));
        }
        else
        {
            Console.WriteLine("No errors recorded.");
        }
    }
}
```

## JSON Export

```csharp
// Export global metrics as JSON
var globalJson = MetricsFormatter.FormatAsJson(SqlTemplateMetrics.GetGlobalMetrics());
File.WriteAllText("metrics-global.json", globalJson);

// Export all template metrics
var allMetrics = SqlTemplateMetrics.GetAllMetrics();
var jsonArray = "[" + string.Join(",", allMetrics.Select(MetricsFormatter.FormatAsJson)) + "]";
File.WriteAllText("metrics-templates.json", jsonArray);
```

## Performance Considerations

- **Overhead**: < 100ns per operation using lock-free atomics
- **Memory**: ~200 bytes per unique template
- **Thread Safety**: Fully thread-safe, no locks
- **AOT**: Zero reflection, fully trimmable

## Disabling Metrics

To disable metrics collection at compile time:

```xml
<PropertyGroup>
  <DefineConstants>$(DefineConstants);SQLX_DISABLE_METRICS</DefineConstants>
</PropertyGroup>
```

This removes all metrics code from generated repositories, resulting in zero overhead.

## Best Practices

1. **Monitor in Production**: Metrics have minimal overhead and provide valuable insights
2. **Set Alerts**: Monitor error rates and slow queries
3. **Regular Review**: Check slowest templates weekly
4. **Capacity Planning**: Use execution counts for load estimation
5. **Error Analysis**: Review error types to identify systemic issues

## Integration with Monitoring Systems

```csharp
// Prometheus-style export
public static string ExportPrometheus()
{
    var sb = new StringBuilder();
    var global = SqlTemplateMetrics.GetGlobalMetrics();
    
    sb.AppendLine($"# HELP sqlx_executions_total Total SQL executions");
    sb.AppendLine($"# TYPE sqlx_executions_total counter");
    sb.AppendLine($"sqlx_executions_total {global.TotalExecutions}");
    
    sb.AppendLine($"# HELP sqlx_errors_total Total SQL errors");
    sb.AppendLine($"# TYPE sqlx_errors_total counter");
    sb.AppendLine($"sqlx_errors_total {global.TotalErrors}");
    
    sb.AppendLine($"# HELP sqlx_execution_duration_ms Average execution duration");
    sb.AppendLine($"# TYPE sqlx_execution_duration_ms gauge");
    sb.AppendLine($"sqlx_execution_duration_ms {global.AverageExecutionMs}");
    
    foreach (var metric in SqlTemplateMetrics.GetAllMetrics())
    {
        var key = metric.TemplateKey.Replace(".", "_").Replace("<", "_").Replace(">", "_");
        sb.AppendLine($"sqlx_template_executions{{template=\"{metric.TemplateKey}\"}} {metric.ExecutionCount}");
        sb.AppendLine($"sqlx_template_errors{{template=\"{metric.TemplateKey}\"}} {metric.ErrorCount}");
        sb.AppendLine($"sqlx_template_duration_ms{{template=\"{metric.TemplateKey}\"}} {metric.AverageExecutionMs}");
    }
    
    return sb.ToString();
}
```
