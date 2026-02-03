# SQL Template Metrics

Sqlx provides built-in metrics instrumentation using the standard .NET `System.Diagnostics.Metrics` API.

## Overview

Metrics are automatically collected for all SQL template executions and can be consumed by:
- OpenTelemetry
- Prometheus
- Application Insights
- Any metrics provider that supports `System.Diagnostics.Metrics`

## Metrics Collected

### `sqlx.template.duration` (Histogram)
Measures SQL template execution duration in milliseconds.

### `sqlx.template.executions` (Counter)
Counts total number of SQL template executions.

### `sqlx.template.errors` (Counter)
Counts total number of SQL template execution errors.

## Tags

All metrics include the following tags for filtering and aggregation:

- `repository.class`: Full repository class name (e.g., `"MyApp.Repositories.UserRepository"`)
- `repository.method`: Method name (e.g., `"GetByIdAsync"`)
- `sql.template`: Original SQL template string (e.g., `"SELECT {{columns}} FROM {{table}} WHERE id = @id"`)
- `error.type`: Exception type name (only for error metrics)

## Usage

Metrics are collected automatically. No code changes required.

### OpenTelemetry Example

```csharp
using OpenTelemetry.Metrics;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics
        .AddMeter("Sqlx.SqlTemplate")
        .AddPrometheusExporter());

var app = builder.Build();
app.MapPrometheusScrapingEndpoint();
app.Run();
```

### Prometheus Output Example

```
# HELP sqlx_template_duration SQL template execution duration in milliseconds
# TYPE sqlx_template_duration histogram
sqlx_template_duration_bucket{repository_class="MyApp.UserRepository",repository_method="GetByIdAsync",sql_template="SELECT {{columns}} FROM {{table}} WHERE id = @id",le="1"} 245
sqlx_template_duration_bucket{repository_class="MyApp.UserRepository",repository_method="GetByIdAsync",sql_template="SELECT {{columns}} FROM {{table}} WHERE id = @id",le="5"} 298
sqlx_template_duration_bucket{repository_class="MyApp.UserRepository",repository_method="GetByIdAsync",sql_template="SELECT {{columns}} FROM {{table}} WHERE id = @id",le="10"} 300
sqlx_template_duration_sum{repository_class="MyApp.UserRepository",repository_method="GetByIdAsync",sql_template="SELECT {{columns}} FROM {{table}} WHERE id = @id"} 892.5
sqlx_template_duration_count{repository_class="MyApp.UserRepository",repository_method="GetByIdAsync",sql_template="SELECT {{columns}} FROM {{table}} WHERE id = @id"} 300

# HELP sqlx_template_executions Total number of SQL template executions
# TYPE sqlx_template_executions counter
sqlx_template_executions_total{repository_class="MyApp.UserRepository",repository_method="GetByIdAsync",sql_template="SELECT {{columns}} FROM {{table}} WHERE id = @id"} 300

# HELP sqlx_template_errors Total number of SQL template execution errors
# TYPE sqlx_template_errors counter
sqlx_template_errors_total{repository_class="MyApp.UserRepository",repository_method="GetByIdAsync",sql_template="SELECT {{columns}} FROM {{table}} WHERE id = @id",error_type="SqlException"} 2
```

## Disabling Metrics

To disable metrics collection, define `SQLX_DISABLE_METRICS` in your project:

```xml
<PropertyGroup>
  <DefineConstants>$(DefineConstants);SQLX_DISABLE_METRICS</DefineConstants>
</PropertyGroup>
```

## Framework Support

Metrics are available on .NET 8.0 or later. On earlier frameworks (netstandard2.1), the metrics methods are no-ops.
