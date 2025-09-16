// -----------------------------------------------------------------------
// <copyright file="BaseOperationGenerator.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
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
        sb.AppendLine("if (connection.State != global::System.Data.ConnectionState.Open)");
        sb.AppendLine("{");
        sb.PushIndent();
        if (isAsync)
        {
            var cancellationToken = GetCancellationTokenParameter(method);
            sb.AppendLine($"await connection.OpenAsync({cancellationToken});");
        }
        else
        {
            sb.AppendLine("connection.Open();");
        }
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();

        // Create command
        sb.AppendLine("__repoCmd__ = connection.CreateCommand();");
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
            "PostgreSqlDialectAttribute" => SqlDefine.PgSql,
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
            "NpgsqlConnection" => SqlDefine.PgSql,
            "SqliteConnection" or "SQLiteConnection" => SqlDefine.SQLite,
            "SqlConnection" => SqlDefine.SqlServer,
            _ => null
        };
    }
}
