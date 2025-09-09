// -----------------------------------------------------------------------
// <copyright file="QueryOptimizer.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using SqlxPerformanceAnalyzer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SqlxPerformanceAnalyzer.Core;

/// <summary>
/// Analyzes SQL queries and provides optimization suggestions.
/// </summary>
public class QueryOptimizer
{
    private readonly ILogger<QueryOptimizer> _logger;
    
    // Common SQL patterns to analyze
    private static readonly Regex SelectStarRegex = new(@"SELECT\s+\*\s+FROM", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex NoWhereRegex = new(@"UPDATE|DELETE", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex LikePatternRegex = new(@"LIKE\s+['""]%.*['""]", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex SubqueryRegex = new(@"\(\s*SELECT", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex FunctionInWhereRegex = new(@"WHERE\s+\w+\s*\(", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex OrderByWithoutLimitRegex = new(@"ORDER\s+BY.*(?!LIMIT)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public QueryOptimizer(ILogger<QueryOptimizer> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Analyzes a SQL query and provides optimization suggestions.
    /// </summary>
    public Task<List<OptimizationSuggestion>> AnalyzeQueryAsync(string sqlQuery, QueryPerformanceMetrics metrics)
    {
        var suggestions = new List<OptimizationSuggestion>();

        try
        {
            // Analyze query structure
            suggestions.AddRange(AnalyzeQueryStructure(sqlQuery));
            
            // Analyze performance patterns
            suggestions.AddRange(AnalyzePerformancePatterns(sqlQuery, metrics));
            
            // Analyze indexing opportunities
            suggestions.AddRange(AnalyzeIndexingOpportunities(sqlQuery, metrics));
            
            // Analyze batch operation opportunities
            suggestions.AddRange(AnalyzeBatchOpportunities(sqlQuery, metrics));

            // Sort by priority and impact
            suggestions = suggestions.OrderBy(s => s.Priority)
                                   .ThenByDescending(s => s.Impact)
                                   .ToList();

            _logger.LogDebug("Generated {Count} suggestions for query", suggestions.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing query for optimization");
        }

        return Task.FromResult(suggestions);
    }

    private List<OptimizationSuggestion> AnalyzeQueryStructure(string sqlQuery)
    {
        var suggestions = new List<OptimizationSuggestion>();
        var upperQuery = sqlQuery.ToUpperInvariant();

        // Check for SELECT *
        if (SelectStarRegex.IsMatch(sqlQuery))
        {
            suggestions.Add(new OptimizationSuggestion
            {
                Type = SuggestionType.ReduceColumns,
                Title = "Avoid SELECT * statements",
                Description = "Using SELECT * retrieves all columns, which may include unnecessary data",
                Recommendation = "Specify only the columns you need to reduce data transfer and improve performance",
                Impact = ImpactLevel.Medium,
                Priority = 2,
                SqlExample = "SELECT id, name, email FROM users -- instead of SELECT * FROM users"
            });
        }

        // Check for UPDATE/DELETE without WHERE
        if (NoWhereRegex.IsMatch(sqlQuery) && !upperQuery.Contains("WHERE"))
        {
            suggestions.Add(new OptimizationSuggestion
            {
                Type = SuggestionType.AddWhere,
                Title = "Missing WHERE clause in UPDATE/DELETE",
                Description = "UPDATE or DELETE statements without WHERE clause affect all rows",
                Recommendation = "Always include a WHERE clause to limit the scope of modifications",
                Impact = ImpactLevel.Critical,
                Priority = 1,
                SqlExample = "UPDATE users SET status = 'active' WHERE last_login > '2023-01-01'"
            });
        }

        // Check for leading wildcard in LIKE
        if (LikePatternRegex.IsMatch(sqlQuery))
        {
            suggestions.Add(new OptimizationSuggestion
            {
                Type = SuggestionType.OptimizeQuery,
                Title = "Leading wildcard in LIKE pattern",
                Description = "LIKE patterns starting with % cannot use indexes efficiently",
                Recommendation = "Consider full-text search or restructure the query to avoid leading wildcards",
                Impact = ImpactLevel.High,
                Priority = 2,
                SqlExample = "-- Consider: WHERE name LIKE 'John%' instead of WHERE name LIKE '%John%'"
            });
        }

        // Check for functions in WHERE clause
        if (FunctionInWhereRegex.IsMatch(sqlQuery))
        {
            suggestions.Add(new OptimizationSuggestion
            {
                Type = SuggestionType.OptimizeQuery,
                Title = "Function calls in WHERE clause",
                Description = "Functions in WHERE clauses prevent index usage",
                Recommendation = "Move function calls out of WHERE clause or use computed columns",
                Impact = ImpactLevel.High,
                Priority = 2,
                SqlExample = "-- Use: WHERE created_date >= '2023-01-01' instead of WHERE YEAR(created_date) = 2023"
            });
        }

        // Check for subqueries that could be JOINs
        if (SubqueryRegex.Matches(sqlQuery).Count > 2)
        {
            suggestions.Add(new OptimizationSuggestion
            {
                Type = SuggestionType.OptimizeJoins,
                Title = "Multiple subqueries detected",
                Description = "Multiple subqueries can often be optimized using JOINs",
                Recommendation = "Consider rewriting subqueries as JOINs for better performance",
                Impact = ImpactLevel.Medium,
                Priority = 3,
                SqlExample = "-- Use JOINs instead of correlated subqueries when possible"
            });
        }

        // Check for ORDER BY without LIMIT
        if (OrderByWithoutLimitRegex.IsMatch(sqlQuery) && !upperQuery.Contains("LIMIT") && !upperQuery.Contains("TOP"))
        {
            suggestions.Add(new OptimizationSuggestion
            {
                Type = SuggestionType.OptimizeQuery,
                Title = "ORDER BY without LIMIT",
                Description = "Sorting large result sets without limiting can be expensive",
                Recommendation = "Consider adding LIMIT clause if you don't need all results",
                Impact = ImpactLevel.Medium,
                Priority = 3,
                SqlExample = "SELECT * FROM users ORDER BY created_date DESC LIMIT 100"
            });
        }

        return suggestions;
    }

    private List<OptimizationSuggestion> AnalyzePerformancePatterns(string sqlQuery, QueryPerformanceMetrics metrics)
    {
        var suggestions = new List<OptimizationSuggestion>();

        // High execution time
        if (metrics.AverageExecutionTimeMs > 1000)
        {
            suggestions.Add(new OptimizationSuggestion
            {
                Type = SuggestionType.OptimizeQuery,
                Title = "High average execution time",
                Description = $"Query averages {metrics.AverageExecutionTimeMs:F2}ms execution time",
                Recommendation = "Review query execution plan and consider adding indexes or optimizing query structure",
                Impact = ImpactLevel.High,
                Priority = 1
            });
        }

        // High variability in execution time
        if (metrics.StandardDeviation > metrics.AverageExecutionTimeMs * 0.5)
        {
            suggestions.Add(new OptimizationSuggestion
            {
                Type = SuggestionType.OptimizeQuery,
                Title = "Inconsistent execution times",
                Description = $"High variance in execution times (std dev: {metrics.StandardDeviation:F2}ms)",
                Recommendation = "Investigate query plan instability, parameter sniffing, or resource contention",
                Impact = ImpactLevel.Medium,
                Priority = 2
            });
        }

        // High error rate
        if (metrics.ErrorRate > 5)
        {
            suggestions.Add(new OptimizationSuggestion
            {
                Type = SuggestionType.OptimizeQuery,
                Title = "High error rate",
                Description = $"Query fails {metrics.ErrorRate:F1}% of the time",
                Recommendation = "Review common error messages and add proper error handling or query validation",
                Impact = ImpactLevel.Critical,
                Priority = 1
            });
        }

        // Frequently executed slow query
        if (metrics.ExecutionCount > 1000 && metrics.AverageExecutionTimeMs > 100)
        {
            suggestions.Add(new OptimizationSuggestion
            {
                Type = SuggestionType.CacheResult,
                Title = "Frequently executed slow query",
                Description = $"Query executed {metrics.ExecutionCount} times with {metrics.AverageExecutionTimeMs:F2}ms average",
                Recommendation = "Consider caching results or optimizing this hot path query",
                Impact = ImpactLevel.High,
                Priority = 1
            });
        }

        return suggestions;
    }

    private List<OptimizationSuggestion> AnalyzeIndexingOpportunities(string sqlQuery, QueryPerformanceMetrics metrics)
    {
        var suggestions = new List<OptimizationSuggestion>();
        var upperQuery = sqlQuery.ToUpperInvariant();

        // Extract WHERE conditions
        var whereConditions = ExtractWhereConditions(sqlQuery);
        
        if (whereConditions.Any() && metrics.AverageExecutionTimeMs > 100)
        {
            suggestions.Add(new OptimizationSuggestion
            {
                Type = SuggestionType.AddIndex,
                Title = "Potential indexing opportunity",
                Description = $"Query with WHERE conditions averaging {metrics.AverageExecutionTimeMs:F2}ms",
                Recommendation = $"Consider adding indexes on columns used in WHERE clauses: {string.Join(", ", whereConditions.Take(3))}",
                Impact = ImpactLevel.High,
                Priority = 1,
                SqlExample = $"CREATE INDEX IX_table_columns ON table_name ({string.Join(", ", whereConditions.Take(3))})"
            });
        }

        // Check for JOIN conditions
        var joinConditions = ExtractJoinConditions(sqlQuery);
        
        if (joinConditions.Any() && metrics.AverageExecutionTimeMs > 200)
        {
            suggestions.Add(new OptimizationSuggestion
            {
                Type = SuggestionType.AddIndex,
                Title = "JOIN performance optimization",
                Description = "Complex JOINs detected with slow execution times",
                Recommendation = "Ensure proper indexes exist on JOIN columns",
                Impact = ImpactLevel.High,
                Priority = 1,
                SqlExample = "-- Add indexes on foreign key columns used in JOINs"
            });
        }

        // Check for ORDER BY without index
        if (upperQuery.Contains("ORDER BY") && metrics.AverageExecutionTimeMs > 500)
        {
            suggestions.Add(new OptimizationSuggestion
            {
                Type = SuggestionType.AddIndex,
                Title = "ORDER BY optimization",
                Description = "ORDER BY clauses can benefit from appropriate indexes",
                Recommendation = "Consider adding an index that includes ORDER BY columns",
                Impact = ImpactLevel.Medium,
                Priority = 2,
                SqlExample = "-- CREATE INDEX IX_table_orderby ON table_name (order_column)"
            });
        }

        return suggestions;
    }

    private List<OptimizationSuggestion> AnalyzeBatchOpportunities(string sqlQuery, QueryPerformanceMetrics metrics)
    {
        var suggestions = new List<OptimizationSuggestion>();
        var upperQuery = sqlQuery.ToUpperInvariant();

        // Detect single INSERT/UPDATE/DELETE with high frequency
        if (metrics.ExecutionCount > 100 && 
            (upperQuery.StartsWith("INSERT") || upperQuery.StartsWith("UPDATE") || upperQuery.StartsWith("DELETE")))
        {
            suggestions.Add(new OptimizationSuggestion
            {
                Type = SuggestionType.UseBatch,
                Title = "Batch operation opportunity",
                Description = $"Single {GetOperationType(upperQuery)} executed {metrics.ExecutionCount} times",
                Recommendation = "Consider using batch operations for better performance with multiple records",
                Impact = ImpactLevel.High,
                Priority = 2,
                SqlExample = "// Use Sqlx batch operations: [SqlExecuteType(SqlExecuteTypes.BatchInsert)]"
            });
        }

        // Detect potential bulk operations
        if (metrics.ExecutionCount > 50 && metrics.TotalRowsAffected > metrics.ExecutionCount * 10)
        {
            suggestions.Add(new OptimizationSuggestion
            {
                Type = SuggestionType.UseBatch,
                Title = "Bulk operation candidate",
                Description = $"High volume operation: {metrics.TotalRowsAffected} total rows affected",
                Recommendation = "Consider bulk operations or batch processing for large data sets",
                Impact = ImpactLevel.Medium,
                Priority = 3
            });
        }

        return suggestions;
    }

    private List<string> ExtractWhereConditions(string sqlQuery)
    {
        var conditions = new List<string>();
        
        try
        {
            // Simple regex to extract column names from WHERE clauses
            var whereMatch = Regex.Match(sqlQuery, @"WHERE\s+(.+?)(?:\s+ORDER\s+BY|\s+GROUP\s+BY|\s+HAVING|$)", 
                RegexOptions.IgnoreCase | RegexOptions.Singleline);
            
            if (whereMatch.Success)
            {
                var whereClause = whereMatch.Groups[1].Value;
                
                // Extract column names (simplified approach)
                var columnMatches = Regex.Matches(whereClause, @"\b([a-zA-Z_][a-zA-Z0-9_]*)\s*[=<>!]", 
                    RegexOptions.IgnoreCase);
                
                foreach (Match match in columnMatches)
                {
                    var column = match.Groups[1].Value;
                    if (!IsReservedWord(column))
                    {
                        conditions.Add(column);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error extracting WHERE conditions from query");
        }

        return conditions.Distinct().ToList();
    }

    private List<string> ExtractJoinConditions(string sqlQuery)
    {
        var conditions = new List<string>();
        
        try
        {
            // Extract JOIN conditions
            var joinMatches = Regex.Matches(sqlQuery, @"JOIN\s+\w+.*?ON\s+(.+?)(?:\s+(?:INNER|LEFT|RIGHT|FULL|WHERE|ORDER|GROUP)|$)", 
                RegexOptions.IgnoreCase | RegexOptions.Singleline);
            
            foreach (Match match in joinMatches)
            {
                var joinCondition = match.Groups[1].Value;
                var columnMatches = Regex.Matches(joinCondition, @"\b([a-zA-Z_][a-zA-Z0-9_]*\.[a-zA-Z_][a-zA-Z0-9_]*)\b");
                
                foreach (Match columnMatch in columnMatches)
                {
                    conditions.Add(columnMatch.Groups[1].Value);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error extracting JOIN conditions from query");
        }

        return conditions.Distinct().ToList();
    }

    private bool IsReservedWord(string word)
    {
        var reservedWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "AND", "OR", "NOT", "IN", "EXISTS", "LIKE", "BETWEEN", "IS", "NULL",
            "SELECT", "FROM", "WHERE", "ORDER", "GROUP", "HAVING", "INSERT", "UPDATE", "DELETE"
        };
        
        return reservedWords.Contains(word);
    }

    private string GetOperationType(string upperQuery)
    {
        if (upperQuery.StartsWith("INSERT")) return "INSERT";
        if (upperQuery.StartsWith("UPDATE")) return "UPDATE";
        if (upperQuery.StartsWith("DELETE")) return "DELETE";
        if (upperQuery.StartsWith("SELECT")) return "SELECT";
        return "UNKNOWN";
    }
}
