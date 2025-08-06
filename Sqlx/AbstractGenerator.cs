// -----------------------------------------------------------------------
// <copyright file="AbstractGenerator.cs" company="Andrii Kurdiumov">
// Copyright (c) Andrii Kurdiumov. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Sqlx.Extensions;

/// <summary>
/// Stored procedures generator.
/// </summary>
public abstract class AbstractGenerator : ISourceGenerator
{
    private static DiagnosticDescriptor SP0001 { get; } = new DiagnosticDescriptor("SP0001", "No stored procedure attribute", "Internal analyzer error.", "Internal", DiagnosticSeverity.Error, true);

    private static DiagnosticDescriptor SP0002 { get; } = new DiagnosticDescriptor("SP0002", "No repository attribute", "Internal analyzer error.", "Internal", DiagnosticSeverity.Error, true);

    private static DiagnosticDescriptor SP0003 { get; } = new DiagnosticDescriptor("SP0003", "Id property cannot be guessed", "Cannot find id property for entity type {0}.", "Sqlx", DiagnosticSeverity.Error, true);

    private static DiagnosticDescriptor SP0004 { get; } = new DiagnosticDescriptor("SP0004", "Entity property corresponding to parameter cannot be guessed", "Cannot find property in entity type {0} corresponding to parameter {1}.", "Sqlx", DiagnosticSeverity.Error, true);

    private static DiagnosticDescriptor SP0005 { get; } = new DiagnosticDescriptor("SP0005", "Unknown method for generation", "Unknown method {0} for generation.", "Sqlx", DiagnosticSeverity.Error, true);

    /// <inheritdoc/>
    public abstract void Initialize(GeneratorInitializationContext context);

    /// <inheritdoc/>
    public void Execute(GeneratorExecutionContext context)
    {
        // Retrieve the populated receiver
        if (context.SyntaxContextReceiver is not ISqlxSyntaxReceiver receiver)
        {
            return;
        }

        INamedTypeSymbol? attributeSymbol = context.Compilation.GetTypeByMetadataName("Sqlx.Annotations.SqlxAttribute");
        if (attributeSymbol == null)
        {
            context.ReportDiagnostic(Diagnostic.Create(SP0001, null));
            return;
        }

        INamedTypeSymbol? repositoryAttributeSymbol = context.Compilation.GetTypeByMetadataName("Sqlx.Annotations.RepositoryAttribute");
        if (repositoryAttributeSymbol == null)
        {
            context.ReportDiagnostic(Diagnostic.Create(SP0002, null));
            return;
        }

        var hasNullableAnnotations = context.Compilation.Options.NullableContextOptions != NullableContextOptions.Disable;

        // Group the fields by class, and generate the source
        foreach (IGrouping<ISymbol?, IMethodSymbol> group in receiver.Methods.GroupBy(f => f.ContainingType, SymbolEqualityComparer.Default))
        {
            var key = (INamedTypeSymbol)group.Key!;
            var generationContext = new ClassGenerationContext(
                (INamedTypeSymbol)group.Key!,
                group.ToList(),
                attributeSymbol,
                repositoryAttributeSymbol,
                context);
            var sourceCode = this.ProcessClass(
                generationContext,
                (INamedTypeSymbol)group.Key!,
                attributeSymbol,
                hasNullableAnnotations);
            if (sourceCode == null)
            {
                context.ReportDiagnostic(Diagnostic.Create(SP0002, null));
                continue;
            }

            context.AddSource($"{key.ToDisplayString().Replace(".", "_")}.Sp.g.cs", SourceText.From(sourceCode, Encoding.UTF8));
        }
    }

    internal static IEnumerable<string> GetUsings(ClassGenerationContext classGenerationContext)
    {
        yield return "System";
        if (classGenerationContext.HasCollections)
        {
            yield return "System.Collections.Generic";
        }

        yield return "System.Data.Common";
        yield return "System.Linq";
        if (classGenerationContext.HasEfCore)
        {
            yield return "Microsoft.EntityFrameworkCore";
            yield return "Microsoft.EntityFrameworkCore.Storage";
        }
    }

    internal static ITypeSymbol UnwrapNullableType(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol namedTypeSymbol && type.Name == "Nullable")
        {
            return namedTypeSymbol.TypeArguments[0];
        }

        return type;
    }

    /// <summary>
    /// Gets parameters list.
    /// </summary>
    /// <param name="methodSymbol">Method symbol from which we copy parameters.</param>
    /// <returns>Generated syntax node for the parameters list.</returns>
    protected abstract SyntaxNode GetParameters(IMethodSymbol methodSymbol);

    private static IPropertySymbol? GetDbSetField(IFieldSymbol? dbContextSymbol, ITypeSymbol itemTypeSymbol)
    {
        if (dbContextSymbol == null)
        {
            return null;
        }

        foreach (var fieldSymbol in dbContextSymbol.Type.GetMembers().OfType<IPropertySymbol>())
        {
            if (fieldSymbol.Type is INamedTypeSymbol namedTypeSymbol)
            {
                namedTypeSymbol = namedTypeSymbol.UnwrapNullableType();
                if (namedTypeSymbol.Name == "DbSet"
                    && namedTypeSymbol.TypeArguments.Length == 1
                    && namedTypeSymbol.TypeArguments[0].Name == itemTypeSymbol.Name)
                {
                    return fieldSymbol;
                }
            }
        }

        return null;
    }

    private static string GetParameterPassing(IParameterSymbol parameter)
    {
        var parameterName = NameMapper.MapName(parameter.Name);
        if (parameter.RefKind == RefKind.Out || parameter.RefKind == RefKind.Ref)
        {
            return "@" + parameterName + " OUTPUT";
        }

        return "@" + parameterName;
    }

    private static void DeclareParameter(IndentedStringBuilder source, bool hasNullableAnnotations, IParameterSymbol parameter, bool isFirst)
    {
        var requireParameterNullCheck = parameter.Type.CanHaveNullValue(hasNullableAnnotations);

        source.Append($@"{(isFirst ? "var " : string.Empty)}parameter = command.CreateParameter();
            parameter.ParameterName = ""@{NameMapper.MapName(parameter.Name)}"";
");
        if (parameter.RefKind == RefKind.Out || parameter.RefKind == RefKind.Ref)
        {
            var parameterSqlDbType = parameter.Type.GetParameterSqlDbType();
            source.AppendLine($@"parameter.DbType = {parameterSqlDbType};");
            var direction = parameter.RefKind == RefKind.Out ? "global::System.Data.ParameterDirection.Output" : "global::System.Data.ParameterDirection.InputOutput";
            source.AppendLine($@"parameter.Direction = {direction};");
            if (parameter.Type.SpecialType == SpecialType.System_String)
            {
                const int StringSize = 4000;
                source.AppendLine($@"parameter.Size = {StringSize};");
            }
        }

        if (parameter.RefKind == RefKind.None || parameter.RefKind == RefKind.Ref)
        {
            if (requireParameterNullCheck)
            {
                source.AppendLine($@"parameter.Value = {parameter.Name} == null ? (global::System.Object)global::System.DBNull.Value : {parameter.Name};");
            }
            else
            {
                source.AppendLine($@"parameter.Value = {parameter.Name};");
            }
        }

        source.AppendLine("command.Parameters.Add(parameter);");
    }

    private static void MarshalOutputParameters(IndentedStringBuilder source, IEnumerable<IParameterSymbol> parameterSymbols, bool hasNullableAnnotations)
    {
        foreach (var parameter in parameterSymbols)
        {
            var requireReadOutput = parameter.RefKind == RefKind.Out || parameter.RefKind == RefKind.Ref;
            if (!requireReadOutput)
            {
                continue;
            }

            source.AppendLine($@"{parameter.Name} = {MarshalValue($"parameter.Value" + (hasNullableAnnotations ? "!" : string.Empty), hasNullableAnnotations, parameter.Type)};");
        }
    }

    private static string MarshalValue(string identifier, bool hasNullableAnnotations, ITypeSymbol returnType)
    {
        if (returnType.CanHaveNullValue(hasNullableAnnotations))
        {
            var nonNullExpression = CastExpression(
                ParseTypeName(returnType.UnwrapNullableType().ToDisplayString()),
                IdentifierName(identifier));
            var nullExpression = CastExpression(
                ParseTypeName(returnType.ToDisplayString()),
                LiteralExpression(SyntaxKind.NullLiteralExpression));
            var mappingExpression = ConditionalExpression(
                BinaryExpression(SyntaxKind.EqualsExpression, IdentifierName(identifier), MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("DBNull"), IdentifierName("Value"))),
                nullExpression,
                nonNullExpression);

            // return mappingExpression.NormalizeWhitespace().ToFullString();
            var nullableReturnType = returnType.ToDisplayString();
            if (!hasNullableAnnotations && returnType.IsReferenceType && returnType.NullableAnnotation != NullableAnnotation.Annotated)
            {
                nullableReturnType += "?";
            }

            return $@"{identifier} == DBNull.Value ? ({nullableReturnType})null : ({UnwrapNullableType(returnType).ToDisplayString()}){identifier}";
        }
        else
        {
            return CastExpression(
                ParseTypeName(returnType.ToDisplayString()),
                IdentifierName(identifier)).NormalizeWhitespace().ToFullString();
        }
    }

    private static void ExecuteSimpleQuery(
        IndentedStringBuilder source,
        MethodGenerationContext methodGenerationContext,
        bool hasNullableAnnotations,
        bool isTask,
        ITypeSymbol returnType)
    {
        var hasResult = returnType.SpecialType != SpecialType.System_Void && returnType.Name != "Task";
        var cancellationToken = methodGenerationContext.CancellationTokenParameter?.Name ?? string.Empty;
        if (!hasResult)
        {
            if (isTask)
            {
                source.AppendLine($@"await command.ExecuteNonQueryAsync({cancellationToken}).ConfigureAwait(false);");
            }
            else
            {
                source.AppendLine($@"command.ExecuteNonQuery();");
            }
        }
        else
        {
            if (isTask)
            {
                source.AppendLine($@"var __result = await command.ExecuteScalarAsync({cancellationToken}).ConfigureAwait(false);");
            }
            else
            {
                source.AppendLine($@"var __result = command.ExecuteScalar();");
            }
        }

        MarshalOutputParameters(source, methodGenerationContext.MethodSymbol.Parameters, hasNullableAnnotations);

        if (hasResult)
        {
            source.AppendLine($@"return {MarshalValue("__result!", hasNullableAnnotations, returnType)};");
        }
    }

    private static string? GetProcedureName(IMethodSymbol methodSymbol, ISymbol attributeSymbol)
    {
        AttributeData? attributeData = methodSymbol.GetAttributes().FirstOrDefault(ad => ad.AttributeClass!.Equals(attributeSymbol, SymbolEqualityComparer.Default));
        if (attributeData == null)
        {
            return null;
        }

        TypedConstant overridenNameOpt = attributeData.NamedArguments.SingleOrDefault(kvp => kvp.Key == "PropertyName").Value;
        var procedureNameConstraint = attributeData.ConstructorArguments.ElementAtOrDefault(0);
        object? procedureName = procedureNameConstraint.Value;
        return (string?)procedureName;
    }

    private string? ProcessClass(
        ClassGenerationContext classGenerationContext,
        INamedTypeSymbol classSymbol,
        ISymbol attributeSymbol,
        bool hasNullableAnnotations)
    {
        if (!classSymbol.ContainingSymbol.Equals(classSymbol.ContainingNamespace, SymbolEqualityComparer.Default))
        {
            // TODO: issue a diagnostic that it must be top level
            return null;
        }

        var namespaceStart = string.Empty;
        var hasNamespace = false;

        if (!classSymbol.ContainingNamespace.IsGlobalNamespace)
        {
            namespaceStart = $@"namespace {classSymbol.ContainingNamespace.ToDisplayString()}
{{
";
            hasNamespace = true;
        }

        var hasEfCore = classGenerationContext.HasEfCore;
        IndentedStringBuilder source = new IndentedStringBuilder($@"// <auto-generated>
// Code generated by Stored Procedures Code Generator.
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.
// </auto-generated>
#nullable enable
#pragma warning disable 1591

{namespaceStart}
");
        source.PushIndent();
        foreach (var usedNamespace in GetUsings(classGenerationContext))
        {
            source.AppendLine($"using {usedNamespace};");
        }

        source.AppendLine();
        source.AppendLine($"{(classSymbol.IsStatic ? "static " : string.Empty)}partial class {classSymbol.Name}");
        source.AppendLine("{");

        // workaround.
        source.PopIndent();

        // Create properties for each field
        foreach (MethodGenerationContext methodGenerationContext in classGenerationContext.Methods)
        {
            this.ProcessMethod(
                source,
                methodGenerationContext,
                methodGenerationContext.MethodSymbol,
                hasNullableAnnotations);
            source.AppendLine();
        }

        // workaround.
        source.PushIndent();
        source.AppendLine("}");
        source.PopIndent();

        if (hasNamespace)
        {
            source.Append("}");
        }

        return source.ToString();
    }

    private string GetConnectionStatement(MethodGenerationContext methodGenerationContext)
    {
        var connectionParameterSymbol = methodGenerationContext.ConnectionParameter;
        if (connectionParameterSymbol != null)
        {
            if (connectionParameterSymbol.Name == "connection")
            {
                return string.Empty;
            }

            return $"var connection = {connectionParameterSymbol.Name};";
        }

        var connectionSymbol = methodGenerationContext.ClassGenerationContext.DbConnectionSymbol;
        if (connectionSymbol != null)
        {
            return $"var connection = this.{connectionSymbol.Name};";
        }

        if (methodGenerationContext.UseDbConnection || methodGenerationContext.HasTransaction)
        {
            var dbContextParameterSymbol = methodGenerationContext.DbContextParameter;
            if (dbContextParameterSymbol != null)
            {
                return $"var connection = {dbContextParameterSymbol.Name}.Database.GetDbConnection();";
            }

            var contextName = methodGenerationContext.ClassGenerationContext.DbContextName;
            return $"var connection = this.{contextName}.Database.GetDbConnection();";
        }

        return string.Empty;
    }

    private string GetOpenConnectionStatement(MethodGenerationContext methodGenerationContext)
    {
        var dbContextParameterSymbol = methodGenerationContext.DbContextParameter;
        if (dbContextParameterSymbol != null)
        {
            return $"{dbContextParameterSymbol.Name}.Database.OpenConnection();";
        }

        var connectionSymbol = methodGenerationContext.ClassGenerationContext.DbConnectionSymbol;
        if (connectionSymbol != null)
        {
            return $"this.{connectionSymbol.Name}.Open();";
        }

        var contextName = methodGenerationContext.ClassGenerationContext.DbContextName;
        return $"this.{contextName}.Database.OpenConnection();";
    }

    private string GetDbTransactionStatement(MethodGenerationContext methodGenerationContext)
    {
        var transactionParameterSymbol = methodGenerationContext.TransactionParameter;
        if (transactionParameterSymbol != null)
        {
            if (methodGenerationContext.UseDbConnection)
            {
                return $"command.Transaction = {transactionParameterSymbol.Name};";
            }
            else
            {
                var dbContextName = methodGenerationContext.ClassGenerationContext.DbContextName;
                return $"this.{dbContextName}.Database.UseTransaction({transactionParameterSymbol.Name});";
            }
        }

        var dbContextParameterSymbol = methodGenerationContext.DbContextParameter;
        if (dbContextParameterSymbol != null)
        {
            return $"command.Transaction = {dbContextParameterSymbol.Name}.Database.CurrentTransaction?.GetDbTransaction();";
        }

        var connectionSymbol = methodGenerationContext.ClassGenerationContext.DbConnectionSymbol;
        if (connectionSymbol != null)
        {
            return string.Empty;
        }

        var connectionParameterSymbol = methodGenerationContext.ConnectionParameter;
        if (connectionParameterSymbol != null)
        {
            return string.Empty;
        }

        var contextName = methodGenerationContext.ClassGenerationContext.DbContextName;
        return $"command.Transaction = this.{contextName}.Database.CurrentTransaction?.GetDbTransaction();";
    }

    private string GetCloseConnectionStatement(MethodGenerationContext methodGenerationContext)
    {
        var dbContextParameterSymbol = methodGenerationContext.DbContextParameter;
        if (dbContextParameterSymbol != null)
        {
            return $"{dbContextParameterSymbol.Name}.Database.CloseConnection();";
        }

        var connectionSymbol = methodGenerationContext.ClassGenerationContext.DbConnectionSymbol;
        if (connectionSymbol != null)
        {
            return $"this.{connectionSymbol.Name}.Close();";
        }

        var contextName = methodGenerationContext.ClassGenerationContext.DbContextName;
        return $"this.{contextName}.Database.CloseConnection();";
    }

    private void MapResults(
        IndentedStringBuilder source,
        MethodGenerationContext methodGenerationContext,
        IMethodSymbol methodSymbol,
        System.Collections.Immutable.ImmutableArray<IParameterSymbol> parameters,
        ITypeSymbol itemType,
        bool hasNullableAnnotations,
        bool isList,
        bool isTask)
    {
        var useDbConnection = methodGenerationContext.UseDbConnection || (isList && (IsTuple(itemType) || IsScalarType(itemType)));
        var cancellationToken = methodGenerationContext.CancellationTokenParameter?.Name ?? string.Empty;
        if (useDbConnection)
        {
            string additionalReaderParameters = isList ? string.Empty : "global::System.Data.CommandBehavior.SingleResult | global::System.Data.CommandBehavior.SingleRow";
            if (isTask && !string.IsNullOrEmpty(cancellationToken))
            {
                if (string.IsNullOrEmpty(additionalReaderParameters))
                {
                    additionalReaderParameters = cancellationToken;
                }
                else
                {
                    additionalReaderParameters = additionalReaderParameters + "," + cancellationToken;
                }
            }

            if (isTask)
            {
                source.AppendLine($"using global::System.Data.Common.DbDataReader reader = await command.ExecuteReaderAsync({additionalReaderParameters}).ConfigureAwait(false);");
            }
            else
            {
                source.AppendLine($"using global::System.Data.Common.DbDataReader reader = command.ExecuteReader({additionalReaderParameters});");
            }

            if (isList)
            {
                if (!methodGenerationContext.IsAsyncEnumerable && !methodGenerationContext.IsEnumerable)
                {
                    source.AppendLine($@"var __result = new List<{(IsTuple(itemType) ? itemType.ToDisplayString() : itemType.Name)}>();");
                }

                if (isTask)
                {
                    source.AppendLine($"while (await reader.ReadAsync({cancellationToken}).ConfigureAwait(false))");
                }
                else
                {
                    source.AppendLine("while (reader.Read())");
                }

                source.AppendLine("{");
                source.PushIndent();
                if (IsScalarType(itemType))
                {
                    source.AppendLine($@"__result.Add(reader.{itemType.GetDataReaderMethod()}(0))");
                }
                else if (IsTuple(itemType))
                {
                    var types = ((INamedTypeSymbol)itemType).TypeArguments;

                    source.AppendLine("__result.Add((");
                    source.PushIndent();
                    for (var i = 0; i < types.Length; i++)
                    {
                        var callExp = $@"reader.{types[i].GetDataReaderMethod()}({i})";
                        if (i < types.Length - 1)
                        {
                            callExp += ",";
                        }

                        source.AppendLine(callExp);
                    }

                    source.PopIndent();
                    source.AppendLine("));");
                }
                else if (methodGenerationContext.IsEnumerable || methodGenerationContext.IsAsyncEnumerable)
                {
                    CreateTheItem();
                    source.AppendLine($"yield return item;");
                }
                else
                {
                    CreateTheItem();
                    source.AppendLine("__result.Add(item);");
                }

                void CreateTheItem()
                {
                    source.AppendLine($@"var item = new {itemType.Name}();");
                    int i = 0;
                    foreach (var propertyName in itemType.GetMembers().OfType<IPropertySymbol>())
                    {
                        source.AppendLine($@"item.{propertyName.Name} = reader.{propertyName.Type.GetDataReaderMethod()}({i});");
                        i++;
                    }
                }

                source.PopIndent();
                source.AppendLine("}");
                source.AppendLine();
                if (isTask)
                {
                    source.AppendLine($"await reader.CloseAsync().ConfigureAwait(false);");
                }
                else
                {
                    source.AppendLine("reader.Close();");
                }
            }
            else
            {
                if (isTask)
                {
                    source.AppendLine($"if (!(await reader.ReadAsync({cancellationToken}).ConfigureAwait(false)))");
                }
                else
                {
                    source.AppendLine("if (!reader.Read())");
                }

                source.AppendLine("{");
                source.PushIndent();
                if (hasNullableAnnotations && methodSymbol.ReturnType.NullableAnnotation != NullableAnnotation.Annotated)
                {
                    source.AppendLine("throw new InvalidOperationException(\"No data returned from command.\");");
                }
                else
                {
                    source.AppendLine("return default;");
                }

                source.PopIndent();
                source.AppendLine("}");
                source.AppendLine();
                source.AppendLine($@"var __result = new {itemType.Name}();");
                int i = 0;
                foreach (var propertyName in itemType.GetMembers().OfType<IPropertySymbol>())
                {
                    source.AppendLine($@"__result.{propertyName.Name} = reader.{propertyName.Type.GetDataReaderMethod()}({i});");
                    i++;
                }

                if (isTask)
                {
                    source.AppendLine($"await reader.CloseAsync().ConfigureAwait(false);");
                }
                else
                {
                    source.AppendLine("reader.Close();");
                }
            }
        }
        else
        {
            var dbContextSymbol = methodGenerationContext.ClassGenerationContext.DbContextSymbol;
            var contextName = methodGenerationContext.ClassGenerationContext.DbContextName;
            var dbsetField = GetDbSetField(dbContextSymbol, itemType);
            var itemTypeProperty = dbsetField?.Name ?? itemType.Name + "s";
            var nullableAnnotations = dbsetField?.NullableAnnotation == NullableAnnotation.Annotated && methodGenerationContext.ClassGenerationContext.NullableContextOptions.AnnotationsEnabled() ? "!" : string.Empty;
            var paramters = $"new global::System.Object[]{{ {string.Join(", ", parameters.Select(x => x.Name))} }}";
            if (isTask)
            {
                if (isList)
                {
                    source.AppendLine($"var __result = await this.{contextName}.{itemTypeProperty}{nullableAnnotations}.FromSqlRaw(sqlQuery{(parameters.Length == 0 ? string.Empty : $", {paramters}")}).ToListAsync({cancellationToken}).ConfigureAwait(false);");
                }
                else
                {
                    source.AppendLine($"{itemType} __result = null!;");
                    source.AppendLine($"var asyncEnumerable = this.{contextName}.{itemTypeProperty}{nullableAnnotations}.FromSqlRaw(sqlQuery{(parameters.Length == 0 ? string.Empty : $", {paramters}")}).AsAsyncEnumerable();");
                    source.AppendLine($"await foreach (var current in asyncEnumerable)");
                    source.AppendLine("{");
                    source.PushIndent();
                    source.AppendLine($"__result = current;");
                    source.AppendLine($"break;");
                    source.PopIndent();
                    source.AppendLine("}");
                }
            }
            else
            {
                string parameterString = parameters.Length == 0 ? string.Empty : $", {paramters}";
                if (methodGenerationContext.IsEnumerable || methodGenerationContext.IsAsyncEnumerable)
                {
                    var isAsync = methodGenerationContext.IsAsyncEnumerable;
                    source.AppendLine($"{(isAsync ? "await " : string.Empty)}foreach(var __item in this.{contextName}.{itemTypeProperty}{nullableAnnotations}.FromSqlRaw(sqlQuery{(parameters.Length == 0 ? string.Empty : $", {paramters}")}).{(isAsync ? "AsAsyncEnumerable" : "AsEnumerable")}())");
                    source.AppendLine("{");
                    source.PushIndent();
                    source.AppendLine("yield return __item;");
                    source.PopIndent();
                    source.AppendLine("}");
                }
                else
                {
                    string materializeResults = isList
                        ? "ToList"
                        : methodGenerationContext.ClassGenerationContext.NullableContextOptions == NullableContextOptions.Enable ? "AsEnumerable().First" : "AsEnumerable().FirstOrDefault";
                    source.AppendLine($"var __result = this.{contextName}.{itemTypeProperty}{nullableAnnotations}.FromSqlRaw(sqlQuery{parameterString}).{materializeResults}();");
                }
            }
        }
    }

    private string? GetQueryForRepositoryMethod(MethodGenerationContext methodGenerationContext)
    {
        var canonicalOperationName = methodGenerationContext.MethodSymbol.Name;
        var entityType = methodGenerationContext.ClassGenerationContext.RepositoryEntityType;
        if (entityType is null)
        {
            return null;
        }

        if (canonicalOperationName == "FindAll")
        {
            var builder = new StringBuilder();
            builder.Append("SELECT");
            var properties = entityType.GetMembers().OfType<IPropertySymbol>().ToList();
            for (var i = 0; i < properties.Count; i++)
            {
                builder.Append(" ");
                builder.Append(properties[i].GetSqlName());
                if (i != properties.Count - 1)
                {
                    builder.Append(",");
                }
            }

            builder.Append(" FROM ");
            builder.Append(entityType.GetSqlName());
            return builder.ToString();
        }

        if (canonicalOperationName == "FindById")
        {
            var builder = new StringBuilder();
            builder.Append("SELECT");
            var properties = entityType.GetMembers().OfType<IPropertySymbol>().ToList();
            for (var i = 0; i < properties.Count; i++)
            {
                builder.Append(" ");
                builder.Append(properties[i].GetSqlName());
                if (i != properties.Count - 1)
                {
                    builder.Append(",");
                }
            }

            builder.Append(" FROM ");
            builder.Append(entityType.GetSqlName());
            AppendFilterById(builder);
            return builder.ToString();
        }

        if (canonicalOperationName == "Count")
        {
            var builder = new StringBuilder();
            builder.Append("SELECT COUNT(1) FROM ");
            builder.Append(entityType.GetSqlName());
            return builder.ToString();
        }

        if (canonicalOperationName == "DeleteAll")
        {
            var builder = new StringBuilder();
            builder.Append("DELETE FROM ");
            builder.Append(entityType.GetSqlName());
            return builder.ToString();
        }

        if (canonicalOperationName == "DeleteById")
        {
            var builder = new StringBuilder();
            builder.Append("DELETE FROM ");
            builder.Append(entityType.GetSqlName());
            AppendFilterById(builder);
            return builder.ToString();
        }

        if (canonicalOperationName == "Update")
        {
            var builder = new StringBuilder();
            builder.Append("UPDATE ");
            builder.Append(entityType.GetSqlName());
            builder.Append(" SET ");
            bool first = true;
            foreach (var parameter in methodGenerationContext.SqlParameters)
            {
                if (parameter.IsPrimaryKey())
                {
                    continue;
                }

                if (!first)
                {
                    builder.Append(", ");
                }

                var entityProperty = entityType.FindMember(parameter.Name);
                if (entityProperty == null)
                {
                    methodGenerationContext.ClassGenerationContext.GeneratorExecutionContext.ReportDiagnostic(Diagnostic.Create(SP0004, parameter.Locations.FirstOrDefault(), entityType.ToDisplayString(), parameter.Name));
                    continue;
                }

                builder.Append(entityProperty.GetSqlName());
                builder.Append(" = ");
                builder.Append(parameter.GetParameterName());
                first = false;
            }

            AppendFilterById(builder);
            return builder.ToString();
        }

        if (canonicalOperationName == "Insert")
        {
            var builder = new StringBuilder();
            builder.Append("INSERT INTO ");
            builder.Append(entityType.GetSqlName());
            builder.Append("(");
            bool first = true;
            foreach (var parameter in methodGenerationContext.SqlParameters)
            {
                if (!first)
                {
                    builder.Append(", ");
                }

                var entityProperty = entityType.FindMember(parameter.Name);
                if (entityProperty == null)
                {
                    methodGenerationContext.ClassGenerationContext.GeneratorExecutionContext.ReportDiagnostic(Diagnostic.Create(SP0004, parameter.Locations.FirstOrDefault(), entityType.ToDisplayString(), parameter.Name));
                    continue;
                }

                builder.Append(entityProperty.GetSqlName());
                first = false;
            }

            builder.Append(") VALUES (");
            first = true;
            foreach (var parameter in methodGenerationContext.SqlParameters)
            {
                if (!first)
                {
                    builder.Append(", ");
                }

                var entityProperty = entityType.FindMember(parameter.Name);
                if (entityProperty == null)
                {
                    methodGenerationContext.ClassGenerationContext.GeneratorExecutionContext.ReportDiagnostic(Diagnostic.Create(SP0004, parameter.Locations.FirstOrDefault(), entityType.ToDisplayString(), parameter.Name));
                    continue;
                }

                builder.Append(parameter.GetParameterName());
                first = false;
            }

            builder.Append(")");
            return builder.ToString();
        }

        methodGenerationContext.ClassGenerationContext.GeneratorExecutionContext.ReportDiagnostic(Diagnostic.Create(SP0005, methodGenerationContext.MethodSymbol.Locations.FirstOrDefault(), methodGenerationContext.MethodSymbol.ToDisplayString()));
        return null;

        void AppendFilterById(StringBuilder builder)
        {
            builder.Append(" WHERE ");
            var idMember = entityType.FindIdMember();
            if (idMember == null)
            {
                methodGenerationContext.ClassGenerationContext.GeneratorExecutionContext.ReportDiagnostic(
                    Diagnostic.Create(SP0003, methodGenerationContext.MethodSymbol.Locations.FirstOrDefault(), new object[] { entityType.ToDisplayString() }));
                return;
            }

            builder.Append(idMember.GetSqlName());
            builder.Append(" = ");
            builder.Append(idMember.GetParameterName());
        }
    }

    private void ProcessMethod(
        IndentedStringBuilder source,
        MethodGenerationContext methodGenerationContext,
        IMethodSymbol methodSymbol,
        bool hasNullableAnnotations)
    {
        // get the name and type of the field
        string fieldName = methodSymbol.Name;
        ITypeSymbol returnType = methodGenerationContext.ReturnType;
        var symbol = (ISymbol)methodSymbol;
        var isTask = methodGenerationContext.IsTask;

        string? procedureName = GetProcedureName(methodSymbol, methodGenerationContext.ClassGenerationContext.AttributeSymbol);
        var parameters = methodGenerationContext.SqlParameters;
        var originalParameters = methodSymbol.Parameters;

        bool hasCustomSql = methodGenerationContext.CustomSqlParameter != null;
        var signature = this.GetParameters(methodSymbol).ToString();
        var itemType = methodGenerationContext.ItemType;
        var getConnection = this.GetConnectionStatement(methodGenerationContext);
        var isList = methodGenerationContext.IsList || methodGenerationContext.IsEnumerable;
        var isScalarType = IsScalarType(UnwrapNullableType(returnType))
            || returnType.SpecialType == SpecialType.System_Void
            || returnType.Name == "Task";
        var returnTypeName = methodSymbol.ReturnType.ToString();
        if (!hasNullableAnnotations && methodSymbol.ReturnType.IsReferenceType && !isScalarType && !isList)
        {
            if (methodSymbol.ReturnType.Name == "Task")
            {
                var x = methodGenerationContext.ClassGenerationContext.CreateTaskType(returnType);
                returnTypeName = x.ToString();
            }
            else if (!methodGenerationContext.IsDataReader)
            {
                if (!returnTypeName.EndsWith("?"))
                {
                    returnTypeName += "?";
                }
            }
        }

        var isNotDbContext = methodGenerationContext.IsNotDbContext;

        source.AppendLine($@"        {symbol.DeclaredAccessibility.GetAccessibility()} {(methodSymbol.IsStatic ? "static " : string.Empty)}partial {(isTask ? "async " : string.Empty)}{returnTypeName} {methodSymbol.Name}{signature}");
        source.PushIndent();
        source.PushIndent();
        source.AppendLine("{");
        source.PushIndent();
        if (!string.IsNullOrWhiteSpace(getConnection))
        {
            source.AppendLine(getConnection);
        }

        var hasTransactionsDbContext = methodGenerationContext.HasTransaction;
        if (hasTransactionsDbContext || methodGenerationContext.UseDbConnection)
        {
            var connParName = methodGenerationContext.ConnectionParameter?.Name ?? "connection";
            if (methodGenerationContext.UseDbConnection)
            {
                source.AppendLine($"if({connParName}.State != global::System.Data.ConnectionState.Open)");
                source.AppendLine("{");
                source.PushIndent();
                if (methodGenerationContext.IsTask || methodGenerationContext.IsAsyncEnumerable)
                {
                    if (methodGenerationContext.CancellationTokenParameter == null)
                    {
                        source.AppendLine($"await {connParName}.OpenAsync().ConfigureAwait(false);");
                    }
                    else
                    {
                        source.AppendLine($"await {connParName}.OpenAsync({methodGenerationContext.CancellationTokenParameter.Name}).ConfigureAwait(false);");
                    }
                }
                else
                {
                    source.AppendLine($"{connParName}.Open();");
                }

                source.PopIndent();
                source.AppendLine("}");
            }

            source.AppendLine($"using var command = {connParName}.CreateCommand();");
        }

        if (parameters.Length > 0 && isNotDbContext)
        {
            for (int i = 0; i < parameters.Length; i++)
            {
                DeclareParameter(source, hasNullableAnnotations, parameters[i], i == 0);
                source.AppendLine();
            }
        }

        if (!hasCustomSql)
        {
            if (procedureName == null)
            {
                source.AppendLine($@"var sqlQuery = @""{this.GetQueryForRepositoryMethod(methodGenerationContext)}"";");
            }
            else
            {
                if (parameters.Length == 0)
                {
                    source.AppendLine($@"var sqlQuery = @""{procedureName}"";");
                }
                else
                {
                    string parametersList = string.Join(", ", parameters.Select(parameter => GetParameterPassing(parameter)));
                    source.AppendLine($@"var sqlQuery = @""{procedureName} {parametersList}"";");
                }
            }
        }

        bool useDbConnection = methodGenerationContext.UseDbConnection;
        var requireDbCommandParameters = isScalarType || useDbConnection || IsTuple(itemType) || IsScalarType(itemType);
        if (requireDbCommandParameters)
        {
            if (!hasCustomSql)
            {
                source.AppendLine($@"command.CommandText = sqlQuery;");
            }
            else
            {
                source.AppendLine($@"command.CommandText = {methodGenerationContext.CustomSqlParameter!.Name};");
            }
        }

        if (hasTransactionsDbContext || methodGenerationContext.UseDbConnection)
        {
            var transactionStatment = this.GetDbTransactionStatement(methodGenerationContext);
            if (!string.IsNullOrWhiteSpace(transactionStatment))
            {
                source.AppendLine(transactionStatment);
            }
        }

        if (isScalarType)
        {
            this.GenerateScalarMethod(source, methodGenerationContext, hasNullableAnnotations, returnType);
        }
        else if (methodGenerationContext.IsDataReader)
        {
            var resultDeclaration = VariableDeclarator(identifier: Identifier("__result"), argumentList: null, initializer: EqualsValueClause(ParseExpression("command.ExecuteReader()")));
            source.AppendLine(LocalDeclarationStatement(VariableDeclaration(
                type: IdentifierName(Identifier("var")),
                variables: SeparatedList(new[] { resultDeclaration }))).NormalizeWhitespace().ToFullString());
            source.AppendLine(ReturnStatement(IdentifierName("__result")).NormalizeWhitespace().ToFullString());
        }
        else
        {
            if (methodGenerationContext.IsNotDbContext)
            {
                source.Append($@"{this.GetOpenConnectionStatement(methodGenerationContext)}
            try
            {{
");
                source.PushIndent();
                this.MapResults(source, methodGenerationContext, methodSymbol, parameters, itemType, hasNullableAnnotations, isList, isTask);

                MarshalOutputParameters(source, parameters, hasNullableAnnotations);
                source.AppendLine(ReturnStatement(IdentifierName("__result")).NormalizeWhitespace().ToFullString());
                source.PopIndent();
                source.Append($@"}}
            finally
            {{
                {this.GetCloseConnectionStatement(methodGenerationContext)}
            }}
");
            }
            else
            {
                this.MapResults(source, methodGenerationContext, methodSymbol, parameters, itemType, hasNullableAnnotations, isList, isTask);
                MarshalOutputParameters(source, parameters, hasNullableAnnotations);
                if (!methodGenerationContext.IsEnumerable && !methodGenerationContext.IsAsyncEnumerable)
                {
                    source.AppendLine(ReturnStatement(IdentifierName("__result")).NormalizeWhitespace().ToFullString());
                }
            }
        }

        source.PopIndent();
        source.PopIndent();
        source.PopIndent();
        source.AppendLine($@"        }}");
    }

    private void GenerateScalarMethod(IndentedStringBuilder source, MethodGenerationContext methodGenerationContext, bool hasNullableAnnotations, ITypeSymbol returnType)
    {
        var isTask = methodGenerationContext.IsTask;
        bool useDbConnection = methodGenerationContext.UseDbConnection;
        if (useDbConnection)
        {
            ExecuteSimpleQuery(source, methodGenerationContext, hasNullableAnnotations, isTask, returnType);
        }
        else
        {
            source.Append($@"{this.GetOpenConnectionStatement(methodGenerationContext)}
            try
            {{
");
            source.PushIndent();
            ExecuteSimpleQuery(source, methodGenerationContext, hasNullableAnnotations, isTask, returnType);

            source.PopIndent();
            source.Append($@"}}
            finally
            {{
                {this.GetCloseConnectionStatement(methodGenerationContext)}
            }}
");
        }
    }
}
