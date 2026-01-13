; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
SQLX001 | Usage | Info | Entity type used in repository should have [SqlxEntity] and/or [SqlxParameter] attribute
SQLX002 | Usage | Error | Unknown placeholder in SqlTemplate
SQLX003 | Usage | Info | Consider adding [Column] attribute for column name customization
