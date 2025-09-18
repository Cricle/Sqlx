// -----------------------------------------------------------------------
// <copyright file="BaseOperationGenerator.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using System;
using System.Linq;

namespace Sqlx.Generator.Core;

/// <summary>
/// Base class for operation generators providing common functionality.
/// </summary>
public abstract class BaseOperationGenerator : IOperationGenerator
{
    /// <inheritdoc/>
    public abstract string OperationName { get; }

    /// <inheritdoc/>
    public abstract void GenerateOperation(OperationGenerationContext context);

    /// <inheritdoc/>
    public abstract bool CanHandle(IMethodSymbol method);

    /// <summary>
    /// Generates parameter null checks for the method.
    /// </summary>
    /// <param name="sb">The string builder.</param>
    /// <param name="method">The method.</param>
    protected virtual void GenerateParameterNullChecks(IndentedStringBuilder sb, IMethodSymbol method)
    {
        foreach (var parameter in method.Parameters)
        {
            if (ShouldGenerateNullCheck(parameter))
            {
                sb.AppendLine($"if ({parameter.Name} == null)");
                sb.AppendLine("{");
                sb.PushIndent();
                sb.AppendLine($"throw new global::System.ArgumentNullException(nameof({parameter.Name}));");
                sb.PopIndent();
                sb.AppendLine("}");
                sb.AppendLine();
            }
        }
    }

    /// <summary>
    /// Generates connection setup code.
    /// </summary>
    /// <param name="sb">The string builder.</param>
    /// <param name="method">The method.</param>
    /// <param name="isAsync">Whether the operation is async.</param>
    protected virtual void GenerateConnectionSetup(IndentedStringBuilder sb, IMethodSymbol method, bool isAsync)
    {
        var connectionFieldName = GetConnectionFieldName(method.ContainingType);

        sb.AppendLine($"if ({connectionFieldName}.State != global::System.Data.ConnectionState.Open)");
        sb.AppendLine("{");
        sb.PushIndent();
        if (isAsync)
        {
            var cancellationToken = GetCancellationTokenParameter(method);
            // For IDbConnection, we don't have OpenAsync, so we open synchronously
            sb.AppendLine($"{connectionFieldName}.Open();");
        }
        else
        {
            sb.AppendLine($"{connectionFieldName}.Open();");
        }
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();

        // Create command
        sb.AppendLine($"__cmd__ = {connectionFieldName}.CreateCommand();");
    }

    /// <summary>
    /// Gets the cancellation token parameter name.
    /// </summary>
    /// <param name="method">The method.</param>
    /// <returns>The cancellation token parameter or default.</returns>
    protected virtual string GetCancellationTokenParameter(IMethodSymbol method)
    {
        var cancellationTokenParam = method.Parameters
            .FirstOrDefault(p => p.Type.Name == "CancellationToken");
        return cancellationTokenParam?.Name ?? "global::System.Threading.CancellationToken.None";
    }

    /// <summary>
    /// Gets the connection field name from the repository class.
    /// </summary>
    /// <param name="repositoryClass">The repository class.</param>
    /// <returns>The connection field name.</returns>
    protected virtual string GetConnectionFieldName(INamedTypeSymbol repositoryClass)
    {
        // Find the first DbConnection field, property, or constructor parameter

        // 1. Check fields (prioritize type checking, fallback to common names)
        var connectionField = repositoryClass.GetMembers()
            .OfType<IFieldSymbol>()
            .FirstOrDefault(f => f.IsDbConnection());
        if (connectionField != null)
        {
            return connectionField.Name;
        }

        // Check by common field names if type checking didn't work
        connectionField = repositoryClass.GetMembers()
            .OfType<IFieldSymbol>()
            .FirstOrDefault(f => IsCommonConnectionFieldName(f.Name));
        if (connectionField != null)
        {
            return connectionField.Name;
        }

        // 2. Check properties
        var connectionProperty = repositoryClass.GetMembers()
            .OfType<IPropertySymbol>()
            .FirstOrDefault(p => p.IsDbConnection() || IsCommonConnectionFieldName(p.Name));
        if (connectionProperty != null)
        {
            return connectionProperty.Name;
        }

        // 3. Check primary constructor parameters
        var primaryConstructor = PrimaryConstructorAnalyzer.GetPrimaryConstructor(repositoryClass);
        if (primaryConstructor != null)
        {
            var connectionParam = primaryConstructor.Parameters.FirstOrDefault(p => p.Type.IsDbConnection());
            if (connectionParam != null)
            {
                return connectionParam.Name;
            }
        }

        // 4. Check regular constructor parameters (fallback)
        var constructor = repositoryClass.InstanceConstructors.FirstOrDefault();
        if (constructor != null)
        {
            var connectionParam = constructor.Parameters.FirstOrDefault(p => p.Type.IsDbConnection());
            if (connectionParam != null)
            {
                return connectionParam.Name;
            }
        }

        // Default fallback - common field names
        return "connection";
    }

    /// <summary>
    /// Checks if the field name matches common connection field naming patterns.
    /// </summary>
    /// <param name="fieldName">The field name to check.</param>
    /// <returns>True if it's a common connection field name.</returns>
    private static bool IsCommonConnectionFieldName(string fieldName)
    {
        return fieldName == "connection" ||
               fieldName == "_connection" ||
               fieldName == "Connection" ||
               fieldName == "_Connection" ||
               fieldName.EndsWith("Connection", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines if a null check should be generated for the parameter.
    /// </summary>
    /// <param name="parameter">The parameter.</param>
    /// <returns>True if null check should be generated.</returns>
    protected virtual bool ShouldGenerateNullCheck(IParameterSymbol parameter)
        => !parameter.Type.IsValueType && parameter.Type.Name != "CancellationToken" && parameter.Type.OriginalDefinition.SpecialType != SpecialType.System_Nullable_T && parameter.NullableAnnotation != NullableAnnotation.Annotated;

    /// <summary>
    /// Gets the SQL define for the repository method.
    /// </summary>
    /// <param name="method">The method.</param>
    /// <returns>The SQL define.</returns>
    protected virtual SqlDefine GetSqlDefineForRepository(IMethodSymbol method)
    {
        // Check for SQL dialect attributes first
        var sqlDialectAttribute = method.ContainingType.GetAttributes()
            .FirstOrDefault(attr => attr.AttributeClass?.Name.EndsWith("DialectAttribute") == true);

        if (sqlDialectAttribute != null)
        {
            return ParseSqlDefineAttribute(sqlDialectAttribute);
        }

        // Infer from connection type
        return InferDialectFromConnectionType(method.ContainingType) ?? SqlDefine.SqlServer;
    }

    /// <summary>
    /// Parses SQL define attribute.
    /// </summary>
    /// <param name="attribute">The attribute.</param>
    /// <returns>The SQL define.</returns>
    protected virtual SqlDefine ParseSqlDefineAttribute(AttributeData attribute)
    {
        var attributeName = attribute.AttributeClass?.Name;
        return attributeName switch
        {
            "MySqlDialectAttribute" => SqlDefine.MySql,
            "PostgreSqlDialectAttribute" => SqlDefine.PostgreSql,
            "SQLiteDialectAttribute" => SqlDefine.SQLite,
            "SqlServerDialectAttribute" => SqlDefine.SqlServer,
            _ => SqlDefine.SqlServer
        };
    }

    /// <summary>
    /// Infers dialect from connection type.
    /// </summary>
    /// <param name="repositoryClass">The repository class.</param>
    /// <returns>The inferred SQL define.</returns>
    protected virtual SqlDefine? InferDialectFromConnectionType(INamedTypeSymbol repositoryClass)
    {
        var connectionField = repositoryClass.GetMembers()
            .OfType<IFieldSymbol>()
            .FirstOrDefault(f => f.Type.AllInterfaces
                .Any(i => i.Name == "IDbConnection"));

        if (connectionField != null)
        {
            return InferDialectFromConnectionTypeName(connectionField.Type.Name);
        }

        var connectionProperty = repositoryClass.GetMembers()
            .OfType<IPropertySymbol>()
            .FirstOrDefault(p => p.Type.AllInterfaces
                .Any(i => i.Name == "IDbConnection"));

        if (connectionProperty != null)
        {
            return InferDialectFromConnectionTypeName(connectionProperty.Type.Name);
        }

        return null;
    }

    /// <summary>
    /// Infers dialect from connection type name.
    /// </summary>
    /// <param name="connectionTypeName">The connection type name.</param>
    /// <returns>The inferred SQL define.</returns>
    protected virtual SqlDefine? InferDialectFromConnectionTypeName(string connectionTypeName)
    {
        return connectionTypeName switch
        {
            "MySqlConnection" => SqlDefine.MySql,
            "NpgsqlConnection" => SqlDefine.PostgreSql,
            "SqliteConnection" or "SQLiteConnection" => SqlDefine.SQLite,
            "SqlConnection" => SqlDefine.SqlServer,
            _ => null
        };
    }
}
