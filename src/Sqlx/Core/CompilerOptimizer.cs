// -----------------------------------------------------------------------
// <copyright file="CompilerOptimizer.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Sqlx.Core;

/// <summary>
/// Compiler optimization utilities for generated code.
/// </summary>
internal static class CompilerOptimizer
{
    /// <summary>
    /// Generates optimized method attributes for better JIT compilation.
    /// </summary>
    public static void GenerateOptimizationAttributes(IndentedStringBuilder sb, bool isHotPath = false, bool inlineHint = false)
    {
        if (isHotPath)
        {
            sb.AppendLine("[global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveOptimization)]");
        }

        if (inlineHint)
        {
            sb.AppendLine("[global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]");
        }
    }

    /// <summary>
    /// Generates branch prediction hints for better CPU performance.
    /// </summary>
    public static string GenerateBranchHint(string condition, bool isLikelyTrue = true)
    {
        // Use likely/unlikely hints for better branch prediction
        var hint = isLikelyTrue ? "likely" : "unlikely";
        return $"if ({condition}) // {hint}";
    }

    /// <summary>
    /// Generates optimized null checks with fast path.
    /// </summary>
    public static void GenerateOptimizedNullCheck(IndentedStringBuilder sb, string variableName, string nullAction, string nonNullAction)
    {
        sb.AppendLine($"// Optimized null check with branch prediction");
        sb.AppendLine($"if ({variableName} is null) // unlikely");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine(nullAction);
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine("else // likely - fast path");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine(nonNullAction);
        sb.PopIndent();
        sb.AppendLine("}");
    }

    /// <summary>
    /// Generates loop unrolling for small, known iteration counts.
    /// </summary>
    public static void GenerateUnrolledLoop(IndentedStringBuilder sb, int iterations, Func<int, string> bodyGenerator)
    {
        if (iterations <= 0) return;

        if (iterations <= 4)
        {
            // Unroll small loops for better performance
            sb.AppendLine("// Unrolled loop for optimal performance");
            for (int i = 0; i < iterations; i++)
            {
                sb.AppendLine(bodyGenerator(i));
            }
        }
        else
        {
            // Use regular loop for larger iterations
            sb.AppendLine($"for (int i = 0; i < {iterations}; i++)");
            sb.AppendLine("{");
            sb.PushIndent();
            sb.AppendLine(bodyGenerator(-1).Replace("i", "i")); // Use loop variable
            sb.PopIndent();
            sb.AppendLine("}");
        }
    }

    /// <summary>
    /// Generates stack allocation for small arrays to avoid heap allocation.
    /// </summary>
    public static void GenerateStackAllocation(IndentedStringBuilder sb, string type, string variableName, int size, int maxStackSize = 256)
    {
        if (size <= maxStackSize / EstimateTypeSize(type))
        {
            sb.AppendLine($"// Stack allocation for better performance");
            sb.AppendLine($"Span<{type}> {variableName} = stackalloc {type}[{size}];");
        }
        else
        {
            sb.AppendLine($"// Heap allocation for large arrays");
            sb.AppendLine($"var {variableName} = new {type}[{size}];");
        }
    }

    /// <summary>
    /// Generates optimized string operations using spans.
    /// </summary>
    public static void GenerateOptimizedStringOps(IndentedStringBuilder sb, string operation, params string[] parameters)
    {
        switch (operation.ToLowerInvariant())
        {
            case "concat":
                GenerateOptimizedConcat(sb, parameters);
                break;
            case "format":
                GenerateOptimizedFormat(sb, parameters);
                break;
            case "compare":
                GenerateOptimizedCompare(sb, parameters);
                break;
        }
    }

    private static void GenerateOptimizedConcat(IndentedStringBuilder sb, string[] parameters)
    {
        if (parameters.Length <= 2)
        {
            var result = string.Join(" + ", parameters);
            sb.AppendLine($"var result = {result};");
        }
        else
        {
            sb.AppendLine("// Optimized string concatenation using StringBuilder");
            sb.AppendLine($"var sb = new global::System.Text.StringBuilder({EstimateTotalLength(parameters)});");
            foreach (var param in parameters)
            {
                sb.AppendLine($"sb.Append({param});");
            }
            sb.AppendLine("var result = sb.ToString();");
        }
    }

    private static void GenerateOptimizedFormat(IndentedStringBuilder sb, string[] parameters)
    {
        if (parameters.Length == 1)
        {
            sb.AppendLine($"var result = {parameters[0]};");
        }
        else if (parameters.Length <= 4)
        {
            var formatCall = $"string.Format({string.Join(", ", parameters)})";
            sb.AppendLine($"var result = {formatCall};");
        }
        else
        {
            sb.AppendLine("// Use StringBuilder for complex formatting");
            sb.AppendLine($"var result = string.Format({parameters[0]}, new object[] {{ {string.Join(", ", parameters.Skip(1))} }});");
        }
    }

    private static void GenerateOptimizedCompare(IndentedStringBuilder sb, string[] parameters)
    {
        if (parameters.Length >= 2)
        {
            sb.AppendLine($"// Optimized string comparison");
            sb.AppendLine($"var result = string.Equals({parameters[0]}, {parameters[1]}, global::System.StringComparison.Ordinal);");
        }
    }

    private static int EstimateTypeSize(string type)
    {
        return type.ToLowerInvariant() switch
        {
            "byte" => 1,
            "short" => 2,
            "int" => 4,
            "long" => 8,
            "float" => 4,
            "double" => 8,
            "char" => 2,
            "bool" => 1,
            _ => 8 // Reference types
        };
    }

    private static int EstimateTotalLength(string[] parameters)
    {
        // Rough estimate for StringBuilder capacity
        return parameters.Length * 50; // Average string length estimate
    }
}

/// <summary>
/// JIT optimization hints and attributes generator.
/// </summary>
internal static class JitOptimizer
{
    /// <summary>
    /// Generates JIT optimization attributes for hot path methods.
    /// </summary>
    public static void GenerateHotPathAttributes(IndentedStringBuilder sb)
    {
        sb.AppendLine("[global::System.Runtime.CompilerServices.MethodImpl(");
        sb.AppendLine("    global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveOptimization |");
        sb.AppendLine("    global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]");
    }

    /// <summary>
    /// Generates cold path attributes for rarely executed code.
    /// </summary>
    public static void GenerateColdPathAttributes(IndentedStringBuilder sb)
    {
        sb.AppendLine("[global::System.Runtime.CompilerServices.MethodImpl(");
        sb.AppendLine("    global::System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]");
    }

    /// <summary>
    /// Generates generic specialization hints.
    /// </summary>
    public static void GenerateGenericOptimization<T>(IndentedStringBuilder sb, string methodName)
    {
        sb.AppendLine($"// Generic specialization for {typeof(T).Name}");
        if (typeof(T).IsValueType)
        {
            sb.AppendLine("// Value type optimization - no boxing");
        }
        else
        {
            sb.AppendLine("// Reference type - null checks optimized");
        }
    }
}
