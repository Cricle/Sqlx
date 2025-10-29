using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;

namespace Sqlx.Extension.Diagnostics
{
    /// <summary>
    /// Analyzer that validates SqlTemplate parameters match method parameters
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class SqlTemplateParameterAnalyzer : DiagnosticAnalyzer
    {
        // Diagnostic IDs
        public const string MissingParameterDiagnosticId = "SQLX001";
        public const string UnusedParameterDiagnosticId = "SQLX002";
        public const string TypeMismatchDiagnosticId = "SQLX003";

        // Diagnostic descriptors
        private static readonly DiagnosticDescriptor MissingParameterRule = new DiagnosticDescriptor(
            id: MissingParameterDiagnosticId,
            title: "SQL parameter not found in method parameters",
            messageFormat: "SQL parameter '@{0}' is used in the template but not found in method parameters",
            category: "Sqlx.Usage",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "All SQL parameters (e.g., @paramName) must have corresponding method parameters.");

        private static readonly DiagnosticDescriptor UnusedParameterRule = new DiagnosticDescriptor(
            id: UnusedParameterDiagnosticId,
            title: "Method parameter not used in SQL template",
            messageFormat: "Method parameter '{0}' is not used in the SQL template",
            category: "Sqlx.Usage",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Method parameters should be used in the SQL template or removed.");

        private static readonly DiagnosticDescriptor TypeMismatchRule = new DiagnosticDescriptor(
            id: TypeMismatchDiagnosticId,
            title: "Parameter type mismatch",
            messageFormat: "Parameter '{0}' type may not be suitable for SQL ({1})",
            category: "Sqlx.Usage",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Parameter types should be appropriate for SQL operations.");

        // Regular expression to extract SQL parameters
        private static readonly Regex SqlParameterRegex = new Regex(
            @"@([a-zA-Z_][a-zA-Z0-9_]*)",
            RegexOptions.Compiled);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(MissingParameterRule, UnusedParameterRule, TypeMismatchRule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            // Register syntax node action for method declarations
            context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
        }

        private void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
        {
            var methodDeclaration = (MethodDeclarationSyntax)context.Node;

            // Find SqlTemplate attribute
            var sqlTemplateAttribute = methodDeclaration.AttributeLists
                .SelectMany(al => al.Attributes)
                .FirstOrDefault(a => a.Name.ToString().Contains("SqlTemplate"));

            if (sqlTemplateAttribute == null)
                return;

            // Extract SQL template string
            var sqlTemplate = ExtractSqlTemplate(sqlTemplateAttribute);
            if (string.IsNullOrEmpty(sqlTemplate))
                return;

            // Extract SQL parameters from template
            var sqlParameters = ExtractSqlParameters(sqlTemplate);

            // Get method parameters (excluding CancellationToken, DbTransaction, etc.)
            var methodParameters = methodDeclaration.ParameterList.Parameters
                .Where(p => !IsSpecialParameter(p, context.SemanticModel))
                .ToList();

            // Check for missing parameters
            foreach (var sqlParam in sqlParameters)
            {
                var matchingMethodParam = methodParameters.FirstOrDefault(mp =>
                    mp.Identifier.Text.Equals(sqlParam, System.StringComparison.OrdinalIgnoreCase));

                if (matchingMethodParam == null)
                {
                    var diagnostic = Diagnostic.Create(
                        MissingParameterRule,
                        sqlTemplateAttribute.GetLocation(),
                        sqlParam);
                    context.ReportDiagnostic(diagnostic);
                }
            }

            // Check for unused method parameters
            foreach (var methodParam in methodParameters)
            {
                var paramName = methodParam.Identifier.Text;
                var isUsedInSql = sqlParameters.Any(sp =>
                    sp.Equals(paramName, System.StringComparison.OrdinalIgnoreCase));

                // Check if parameter is used in placeholders or is an entity parameter
                var isEntityParam = IsEntityParameter(methodParam, context.SemanticModel);
                var isUsedInPlaceholder = CheckIfUsedInPlaceholder(methodParam, sqlTemplate, context.SemanticModel);

                if (!isUsedInSql && !isEntityParam && !isUsedInPlaceholder)
                {
                    var diagnostic = Diagnostic.Create(
                        UnusedParameterRule,
                        methodParam.GetLocation(),
                        paramName);
                    context.ReportDiagnostic(diagnostic);
                }
            }

            // Check parameter types
            foreach (var methodParam in methodParameters)
            {
                var paramType = context.SemanticModel.GetTypeInfo(methodParam.Type).Type;
                if (paramType != null && !IsSuitableForSql(paramType))
                {
                    var diagnostic = Diagnostic.Create(
                        TypeMismatchRule,
                        methodParam.GetLocation(),
                        methodParam.Identifier.Text,
                        paramType.ToDisplayString());
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        /// <summary>
        /// Extracts SQL template string from attribute
        /// </summary>
        private string ExtractSqlTemplate(AttributeSyntax attribute)
        {
            var argument = attribute.ArgumentList?.Arguments.FirstOrDefault();
            if (argument == null)
                return null;

            if (argument.Expression is LiteralExpressionSyntax literalExpression)
            {
                return literalExpression.Token.ValueText;
            }

            return null;
        }

        /// <summary>
        /// Extracts SQL parameter names from template
        /// </summary>
        private ImmutableHashSet<string> ExtractSqlParameters(string sqlTemplate)
        {
            var parameters = SqlParameterRegex.Matches(sqlTemplate)
                .Cast<Match>()
                .Select(m => m.Groups[1].Value)
                .Distinct()
                .ToImmutableHashSet();

            return parameters;
        }

        /// <summary>
        /// Checks if parameter is a special system parameter that should be ignored
        /// </summary>
        private bool IsSpecialParameter(ParameterSyntax parameter, SemanticModel semanticModel)
        {
            var paramType = semanticModel.GetTypeInfo(parameter.Type).Type;
            if (paramType == null)
                return false;

            var typeName = paramType.ToDisplayString();

            // Ignore CancellationToken, DbTransaction, etc.
            return typeName.Contains("CancellationToken") ||
                   typeName.Contains("DbTransaction") ||
                   typeName.Contains("IDbTransaction");
        }

        /// <summary>
        /// Checks if parameter is an entity parameter (used in {{values}}, {{set}}, etc.)
        /// </summary>
        private bool IsEntityParameter(ParameterSyntax parameter, SemanticModel semanticModel)
        {
            var paramType = semanticModel.GetTypeInfo(parameter.Type).Type;
            if (paramType == null)
                return false;

            // Check if it's a custom class (likely an entity)
            return paramType.TypeKind == TypeKind.Class &&
                   !paramType.ToDisplayString().StartsWith("System.");
        }

        /// <summary>
        /// Checks if parameter is used in placeholders like {{values}}, {{set}}
        /// </summary>
        private bool CheckIfUsedInPlaceholder(ParameterSyntax parameter, string sqlTemplate, SemanticModel semanticModel)
        {
            // Parameters used in {{values}}, {{set}}, {{batch_values}} are implicitly used
            return sqlTemplate.Contains("{{values}}") ||
                   sqlTemplate.Contains("{{set}}") ||
                   sqlTemplate.Contains("{{batch_values}}");
        }

        /// <summary>
        /// Checks if type is suitable for SQL parameters
        /// </summary>
        private bool IsSuitableForSql(ITypeSymbol type)
        {
            var typeName = type.ToDisplayString();

            // Primitive types are fine
            if (type.TypeKind == TypeKind.Enum)
                return true;

            // Common SQL-friendly types
            var sqlFriendlyTypes = new[]
            {
                "int", "long", "short", "byte",
                "uint", "ulong", "ushort", "sbyte",
                "float", "double", "decimal",
                "bool", "char", "string",
                "System.DateTime", "System.DateTimeOffset", "System.TimeSpan",
                "System.Guid", "System.Byte[]"
            };

            foreach (var sqlType in sqlFriendlyTypes)
            {
                if (typeName.Contains(sqlType))
                    return true;
            }

            // Nullable versions are also fine
            if (type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
                return true;

            // Custom classes might be entities (used in {{values}}, {{set}})
            if (type.TypeKind == TypeKind.Class)
                return true;

            // Collections might be for batch operations
            if (typeName.Contains("IEnumerable") || typeName.Contains("List"))
                return true;

            return false;
        }
    }
}

