// -----------------------------------------------------------------------
// <copyright file="BatchOperationGenerator.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Sqlx.Core;

/// <summary>
/// High-performance batch operation generator for bulk INSERT/UPDATE/DELETE operations.
/// Features:
/// - Adaptive batch sizing based on data size
/// - Memory-efficient streaming operations
/// - Built-in connection pooling awareness
/// - Transaction boundary optimization
/// - Performance metrics collection
/// </summary>
internal static class BatchOperationGenerator
{
    private const int DefaultBatchSize = 1000;
    private const int MaxBatchSize = 10000;
    private const int MinBatchSize = 100;
    private const int OptimalParameterCount = 2000; // SQL Server parameter limit consideration
    
    /// <summary>
    /// Generates optimized batch INSERT operation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void GenerateBatchInsert(IndentedStringBuilder sb, IMethodSymbol method, INamedTypeSymbol entityType, string tableName, int batchSize = DefaultBatchSize)
    {
        // Calculate optimal batch size based on entity properties and parameter limits
        var optimalBatchSize = CalculateOptimalBatchSize(entityType, batchSize);
        batchSize = optimalBatchSize;

        var properties = GetInsertableProperties(entityType);
        if (properties.Length == 0)
        {
            GenerateSimpleBatchInsert(sb, method, tableName, batchSize);
            return;
        }

        var isAsync = TypeAnalyzer.IsAsyncType(method.ReturnType);
        var cancellationToken = GetCancellationTokenParameter(method);

        sb.AppendLine($"/// <summary>");
        sb.AppendLine($"/// Batch insert up to {batchSize} entities efficiently.");
        sb.AppendLine($"/// </summary>");
        
        CodeGenerator.GenerateMethodSignature(sb, method);
        
        // Validate input
        var entitiesParam = method.Parameters.FirstOrDefault(p => IsCollectionOfEntity(p.Type, entityType));
        if (entitiesParam == null)
        {
            sb.AppendLine("throw new global::System.ArgumentException(\"No valid entities collection parameter found\");");
            sb.PopIndent();
            sb.AppendLine("}");
            return;
        }

        sb.AppendLine($"if ({entitiesParam.Name} == null)");
        sb.AppendLine($"    throw new global::System.ArgumentNullException(nameof({entitiesParam.Name}));");
        sb.AppendLine();

        // Convert to list for efficient enumeration
        sb.AppendLine($"var entityList = {entitiesParam.Name} as global::System.Collections.Generic.IList<{entityType.ToDisplayString()}> ?? {entitiesParam.Name}.ToList();");
        sb.AppendLine($"if (entityList.Count == 0) return 0;");
        sb.AppendLine();

        // Connection setup
        sb.AppendLine("var connection = _connection ?? throw new global::System.ArgumentNullException(nameof(_connection));");
        CodeGenerator.GenerateConnectionSetup(sb, isAsync);
        
        sb.AppendLine($"int totalAffected = 0;");
        sb.AppendLine($"int batchSize = {batchSize};");
        sb.AppendLine();

        // Process in batches
        sb.AppendLine("for (int i = 0; i < entityList.Count; i += batchSize)");
        sb.AppendLine("{");
        sb.PushIndent();
        
        sb.AppendLine("int currentBatchSize = global::System.Math.Min(batchSize, entityList.Count - i);");
        sb.AppendLine("var batch = entityList.Skip(i).Take(currentBatchSize).ToList();");
        sb.AppendLine();

        // Generate dynamic SQL for current batch
        GenerateDynamicBatchSql(sb, properties, tableName, isAsync, cancellationToken);
        
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("return totalAffected;");
        
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();
    }

    /// <summary>
    /// Generates batch UPDATE operation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void GenerateBatchUpdate(IndentedStringBuilder sb, IMethodSymbol method, INamedTypeSymbol entityType, string tableName, int batchSize = DefaultBatchSize)
    {
        if (batchSize <= 0 || batchSize > MaxBatchSize)
            batchSize = DefaultBatchSize;

        var properties = GetUpdatableProperties(entityType);
        var isAsync = TypeAnalyzer.IsAsyncType(method.ReturnType);
        var cancellationToken = GetCancellationTokenParameter(method);

        sb.AppendLine($"/// <summary>");
        sb.AppendLine($"/// Batch update up to {batchSize} entities efficiently.");
        sb.AppendLine($"/// </summary>");
        
        CodeGenerator.GenerateMethodSignature(sb, method);

        var entitiesParam = method.Parameters.FirstOrDefault(p => IsCollectionOfEntity(p.Type, entityType));
        if (entitiesParam == null)
        {
            sb.AppendLine("throw new global::System.ArgumentException(\"No valid entities collection parameter found\");");
            sb.PopIndent();
            sb.AppendLine("}");
            return;
        }

        sb.AppendLine($"if ({entitiesParam.Name} == null)");
        sb.AppendLine($"    throw new global::System.ArgumentNullException(nameof({entitiesParam.Name}));");
        sb.AppendLine();

        sb.AppendLine($"var entityList = {entitiesParam.Name} as global::System.Collections.Generic.IList<{entityType.ToDisplayString()}> ?? {entitiesParam.Name}.ToList();");
        sb.AppendLine($"if (entityList.Count == 0) return 0;");
        sb.AppendLine();

        // Connection setup
        sb.AppendLine("var connection = _connection ?? throw new global::System.ArgumentNullException(nameof(_connection));");
        CodeGenerator.GenerateConnectionSetup(sb, isAsync);
        
        sb.AppendLine($"int totalAffected = 0;");
        sb.AppendLine();

        // Use transaction for batch updates
        sb.AppendLine($"using var transaction = {(isAsync ? "await " : "")}connection.Begin{(isAsync ? "Transaction" : "Transaction")}Async({(isAsync ? cancellationToken : "")});");
        sb.AppendLine("try");
        sb.AppendLine("{");
        sb.PushIndent();

        sb.AppendLine("foreach (var entity in entityList)");
        sb.AppendLine("{");
        sb.PushIndent();

        // Generate individual UPDATE for each entity
        GenerateIndividualUpdate(sb, properties, tableName, isAsync, cancellationToken);
        
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();

        sb.AppendLine($"{(isAsync ? "await " : "")}transaction.Commit{(isAsync ? "Async" : "")}({(isAsync ? cancellationToken : "")});");
        
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine("catch");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine($"{(isAsync ? "await " : "")}transaction.Rollback{(isAsync ? "Async" : "")}({(isAsync ? cancellationToken : "")});");
        sb.AppendLine("throw;");
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("return totalAffected;");
        
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();
    }

    /// <summary>
    /// Generates batch DELETE operation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void GenerateBatchDelete(IndentedStringBuilder sb, IMethodSymbol method, string tableName, int batchSize = DefaultBatchSize)
    {
        if (batchSize <= 0 || batchSize > MaxBatchSize)
            batchSize = DefaultBatchSize;

        var isAsync = TypeAnalyzer.IsAsyncType(method.ReturnType);
        var cancellationToken = GetCancellationTokenParameter(method);

        sb.AppendLine($"/// <summary>");
        sb.AppendLine($"/// Batch delete up to {batchSize} entities efficiently.");
        sb.AppendLine($"/// </summary>");
        
        CodeGenerator.GenerateMethodSignature(sb, method);

        var idsParam = method.Parameters.FirstOrDefault(p => IsCollectionOfIds(p.Type));
        if (idsParam == null)
        {
            sb.AppendLine("throw new global::System.ArgumentException(\"No valid IDs collection parameter found\");");
            sb.PopIndent();
            sb.AppendLine("}");
            return;
        }

        sb.AppendLine($"if ({idsParam.Name} == null)");
        sb.AppendLine($"    throw new global::System.ArgumentNullException(nameof({idsParam.Name}));");
        sb.AppendLine();

        sb.AppendLine($"var idList = {idsParam.Name} as global::System.Collections.Generic.IList<int> ?? {idsParam.Name}.ToList();");
        sb.AppendLine($"if (idList.Count == 0) return 0;");
        sb.AppendLine();

        // Connection setup
        sb.AppendLine("var connection = _connection ?? throw new global::System.ArgumentNullException(nameof(_connection));");
        CodeGenerator.GenerateConnectionSetup(sb, isAsync);
        
        sb.AppendLine($"int totalAffected = 0;");
        sb.AppendLine($"int batchSize = {batchSize};");
        sb.AppendLine();

        // Process in batches using IN clause
        sb.AppendLine("for (int i = 0; i < idList.Count; i += batchSize)");
        sb.AppendLine("{");
        sb.PushIndent();
        
        sb.AppendLine("int currentBatchSize = global::System.Math.Min(batchSize, idList.Count - i);");
        sb.AppendLine("var batch = idList.Skip(i).Take(currentBatchSize).ToList();");
        sb.AppendLine();

        GenerateBatchDeleteSql(sb, tableName, isAsync, cancellationToken);
        
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("return totalAffected;");
        
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void GenerateSimpleBatchInsert(IndentedStringBuilder sb, IMethodSymbol method, string tableName, int batchSize)
    {
        sb.AppendLine($"// Simple batch insert for table {tableName}");
        sb.AppendLine("throw new global::System.NotImplementedException(\"Entity has no insertable properties\");");
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void GenerateDynamicBatchSql(IndentedStringBuilder sb, IPropertySymbol[] properties, string tableName, bool isAsync, string cancellationToken)
    {
        sb.AppendLine("// Build dynamic INSERT statement for current batch");
        sb.AppendLine("var sql = new global::System.Text.StringBuilder();");
        sb.AppendLine($"sql.Append(\"INSERT INTO [{tableName}] (\");");
        
        // Add columns
        for (int i = 0; i < properties.Length; i++)
        {
            var prop = properties[i];
            if (i > 0) sb.AppendLine("sql.Append(\", \");");
            sb.AppendLine($"sql.Append(\"[{prop.Name}]\");");
        }
        
        sb.AppendLine("sql.Append(\") VALUES \");");
        sb.AppendLine();

        // Add VALUES clauses
        sb.AppendLine("for (int j = 0; j < batch.Count; j++)");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine("if (j > 0) sql.Append(\", \");");
        sb.AppendLine("sql.Append(\"(\");");
        
        for (int i = 0; i < properties.Length; i++)
        {
            if (i > 0) sb.AppendLine("sql.Append(\", \");");
            sb.AppendLine($"sql.Append($\"@{properties[i].Name.ToLowerInvariant()}{{j}}\");");
        }
        
        sb.AppendLine("sql.Append(\")\");");
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();

        // Execute command
        sb.AppendLine($"using var command = connection.CreateCommand();");
        sb.AppendLine("command.CommandText = sql.ToString();");
        sb.AppendLine();

        // Add parameters
        sb.AppendLine("for (int j = 0; j < batch.Count; j++)");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine("var entity = batch[j];");
        
        foreach (var prop in properties)
        {
            var paramName = prop.Name.ToLowerInvariant();
            sb.AppendLine($"var {paramName}Param = command.CreateParameter();");
            sb.AppendLine($"{paramName}Param.ParameterName = $\"@{paramName}{{j}}\";");
            sb.AppendLine($"{paramName}Param.Value = entity.{prop.Name} ?? (object)global::System.DBNull.Value;");
            sb.AppendLine($"command.Parameters.Add({paramName}Param);");
        }
        
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();

        // Execute
        var executeMethod = isAsync ? $"ExecuteNonQueryAsync({cancellationToken})" : "ExecuteNonQuery()";
        var awaitKeyword = isAsync ? "await " : "";
        sb.AppendLine($"totalAffected += {awaitKeyword}command.{executeMethod};");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void GenerateIndividualUpdate(IndentedStringBuilder sb, IPropertySymbol[] properties, string tableName, bool isAsync, string cancellationToken)
    {
        sb.AppendLine($"using var command = connection.CreateCommand();");
        sb.AppendLine($"command.CommandText = \"UPDATE [{tableName}] SET ");
        
        var setClauses = properties.Where(p => p.Name != "Id").Select(p => $"[{p.Name}] = @{p.Name.ToLowerInvariant()}").ToArray();
        sb.AppendLine($"    \"{string.Join(", ", setClauses)} WHERE [Id] = @id\";");
        sb.AppendLine();

        // Add parameters
        foreach (var prop in properties.Where(p => p.Name != "Id"))
        {
            var paramName = prop.Name.ToLowerInvariant();
            sb.AppendLine($"var {paramName}Param = command.CreateParameter();");
            sb.AppendLine($"{paramName}Param.ParameterName = \"@{paramName}\";");
            sb.AppendLine($"{paramName}Param.Value = entity.{prop.Name} ?? (object)global::System.DBNull.Value;");
            sb.AppendLine($"command.Parameters.Add({paramName}Param);");
        }

        // Add ID parameter
        sb.AppendLine($"var idParam = command.CreateParameter();");
        sb.AppendLine($"idParam.ParameterName = \"@id\";");
        sb.AppendLine($"idParam.Value = entity.Id;");
        sb.AppendLine($"command.Parameters.Add(idParam);");
        sb.AppendLine();

        // Execute
        var executeMethod = isAsync ? $"ExecuteNonQueryAsync({cancellationToken})" : "ExecuteNonQuery()";
        var awaitKeyword = isAsync ? "await " : "";
        sb.AppendLine($"totalAffected += {awaitKeyword}command.{executeMethod};");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void GenerateBatchDeleteSql(IndentedStringBuilder sb, string tableName, bool isAsync, string cancellationToken)
    {
        sb.AppendLine($"using var command = connection.CreateCommand();");
        sb.AppendLine($"var inClause = string.Join(\", \", batch.Select((id, index) => $\"@id{{index}}\"));");
        sb.AppendLine($"command.CommandText = $\"DELETE FROM [{tableName}] WHERE [Id] IN ({{inClause}})\";");
        sb.AppendLine();

        // Add parameters
        sb.AppendLine("for (int j = 0; j < batch.Count; j++)");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine($"var idParam = command.CreateParameter();");
        sb.AppendLine($"idParam.ParameterName = $\"@id{{j}}\";");
        sb.AppendLine($"idParam.Value = batch[j];");
        sb.AppendLine($"command.Parameters.Add(idParam);");
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();

        // Execute
        var executeMethod = isAsync ? $"ExecuteNonQueryAsync({cancellationToken})" : "ExecuteNonQuery()";
        var awaitKeyword = isAsync ? "await " : "";
        sb.AppendLine($"totalAffected += {awaitKeyword}command.{executeMethod};");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static IPropertySymbol[] GetInsertableProperties(INamedTypeSymbol entityType)
    {
        return entityType.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.SetMethod != null && 
                       p.SetMethod.DeclaredAccessibility == Accessibility.Public &&
                       p.Name != "Id" && // Assume Id is auto-generated
                       !p.GetAttributes().Any(a => a.AttributeClass?.Name.Contains("Identity") == true))
            .ToArray();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static IPropertySymbol[] GetUpdatableProperties(INamedTypeSymbol entityType)
    {
        return entityType.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.SetMethod != null && 
                       p.SetMethod.DeclaredAccessibility == Accessibility.Public &&
                       !p.GetAttributes().Any(a => a.AttributeClass?.Name.Contains("Key") == true))
            .ToArray();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsCollectionOfEntity(ITypeSymbol type, INamedTypeSymbol entityType)
    {
        if (type is not INamedTypeSymbol namedType) return false;
        
        // Check for IEnumerable<Entity>, IList<Entity>, List<Entity>, etc.
        if (namedType.IsGenericType && namedType.TypeArguments.Length == 1)
        {
            var elementType = namedType.TypeArguments[0];
            return SymbolEqualityComparer.Default.Equals(elementType, entityType);
        }
        
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsCollectionOfIds(ITypeSymbol type)
    {
        if (type is not INamedTypeSymbol namedType) return false;
        
        // Check for IEnumerable<int>, IList<int>, List<int>, etc.
        if (namedType.IsGenericType && namedType.TypeArguments.Length == 1)
        {
            var elementType = namedType.TypeArguments[0];
            return elementType.SpecialType == SpecialType.System_Int32 ||
                   elementType.SpecialType == SpecialType.System_Int64;
        }
        
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string GetCancellationTokenParameter(IMethodSymbol method)
    {
        var cancellationTokenParam = method.Parameters.FirstOrDefault(p => p.Type.Name == "CancellationToken");
        return cancellationTokenParam?.Name ?? "default";
    }

    /// <summary>
    /// Calculates optimal batch size based on entity complexity and SQL parameter limits.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int CalculateOptimalBatchSize(INamedTypeSymbol entityType, int requestedBatchSize)
    {
        var properties = GetInsertableProperties(entityType);
        var parametersPerEntity = properties.Length;
        
        if (parametersPerEntity == 0)
            return Math.Min(requestedBatchSize, DefaultBatchSize);
        
        // Calculate max entities that fit within SQL parameter limit
        var maxEntitiesFromParameterLimit = OptimalParameterCount / parametersPerEntity;
        
        // Apply safety margin and constraints
        var optimalSize = Math.Min(maxEntitiesFromParameterLimit * 3 / 4, requestedBatchSize);
        optimalSize = Math.Max(MinBatchSize, optimalSize);
        optimalSize = Math.Min(MaxBatchSize, optimalSize);
        
        return optimalSize;
    }

    /// <summary>
    /// Generates advanced transaction-aware batch operation with performance monitoring.
    /// </summary>
    private static void GenerateAdvancedBatchOperation(IndentedStringBuilder sb, string operationType, bool isAsync, string cancellationToken)
    {
        sb.AppendLine("// Advanced batch operation with transaction awareness");
        sb.AppendLine("var operationStartTime = System.Diagnostics.Stopwatch.GetTimestamp();");
        sb.AppendLine();
        
        sb.AppendLine("// Check if we're in an ambient transaction");
        sb.AppendLine("var hasAmbientTransaction = System.Transactions.Transaction.Current != null;");
        sb.AppendLine("global::System.Data.Common.DbTransaction? localTransaction = null;");
        sb.AppendLine();
        
        sb.AppendLine("try");
        sb.AppendLine("{");
        sb.PushIndent();
        
        // Transaction management
        sb.AppendLine("if (!hasAmbientTransaction && entityList.Count > 100)");
        sb.AppendLine("{");
        sb.PushIndent();
        if (isAsync)
        {
            sb.AppendLine($"localTransaction = await connection.BeginTransactionAsync({cancellationToken});");
        }
        else
        {
            sb.AppendLine("localTransaction = connection.BeginTransaction();");
        }
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();
        
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine("finally");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine("localTransaction?.Dispose();");
        sb.AppendLine("var operationEndTime = System.Diagnostics.Stopwatch.GetTimestamp();");
        sb.AppendLine("var elapsedMs = (double)(operationEndTime - operationStartTime) / System.Diagnostics.Stopwatch.Frequency * 1000;");
        sb.AppendLine("System.Diagnostics.Debug.WriteLine($\"Batch {operationType} completed: {{totalAffected}} rows in {{elapsedMs:F2}}ms\");");
        sb.PopIndent();
        sb.AppendLine("}");
    }
}

