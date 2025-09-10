using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sqlx.Tests.Core
{
    /// <summary>
    /// Helper methods for testing code generation output
    /// </summary>
    public static class CodeGenerationTestHelpers
    {
        /// <summary>
        /// Verifies that the generated code contains all expected patterns
        /// instead of exact string matching which is brittle
        /// </summary>
        public static void VerifyGeneratedCodeContains(string generatedCode, params string[] expectedPatterns)
        {
            Assert.IsNotNull(generatedCode, "Generated code should not be null");
            Assert.IsTrue(generatedCode.Length > 0, "Generated code should not be empty");

            foreach (var pattern in expectedPatterns)
            {
                Assert.IsTrue(generatedCode.Contains(pattern),
                    $"Generated code should contain pattern: {pattern}\n\nActual generated code:\n{generatedCode}");
            }
        }

        /// <summary>
        /// Verifies that the generated code contains method signature patterns
        /// </summary>
        public static void VerifyMethodSignature(string generatedCode, string methodName, string returnType, params string[] parameters)
        {
            var signaturePattern = $"public partial {returnType} {methodName}(";
            Assert.IsTrue(generatedCode.Contains(signaturePattern),
                $"Generated code should contain method signature: {signaturePattern}");

            foreach (var param in parameters)
            {
                Assert.IsTrue(generatedCode.Contains(param),
                    $"Generated code should contain parameter: {param}");
            }
        }

        /// <summary>
        /// Verifies that the generated code contains SQL execution patterns
        /// </summary>
        public static void VerifySqlExecution(string generatedCode, string commandText, params string[] parameterNames)
        {
            // Check for connection handling
            Assert.IsTrue(generatedCode.Contains("global::System.Data.Common.DbConnection"),
                "Generated code should contain DbConnection usage");

            // Check for command creation
            Assert.IsTrue(generatedCode.Contains("CreateCommand()"),
                "Generated code should contain CreateCommand call");

            // Check for command text setting
            Assert.IsTrue(generatedCode.Contains($"CommandText = \"{commandText}\""),
                $"Generated code should set CommandText to: {commandText}");

            // Check for parameter creation
            foreach (var paramName in parameterNames)
            {
                Assert.IsTrue(generatedCode.Contains($"ParameterName = \"@{paramName}\""),
                    $"Generated code should contain parameter: @{paramName}");
            }
        }
    }
}

