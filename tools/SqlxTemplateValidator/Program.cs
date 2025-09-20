using System;
using System.IO;
using System.Threading.Tasks;
using Sqlx.Generator.Tools;

namespace SqlxTemplateValidator;

/// <summary>
/// Simplified Sqlx template validation tool.
/// </summary>
internal class Program
{
    internal static async Task<int> Main(string[] args)
    {
        try
        {
            Console.WriteLine("🔍 Sqlx Template Validator (Simplified)");
            Console.WriteLine("======================================");
            Console.WriteLine();

            if (args.Length == 0)
            {
                ShowHelp();
                return 0;
            }

            var command = args[0].ToLowerInvariant();

            switch (command)
            {
                case "validate":
                    return await ValidateTemplates(args);
                case "help":
                case "--help":
                case "-h":
                    ShowHelp();
                    return 0;
                default:
                    Console.WriteLine($"❌ Unknown command: {command}");
                    ShowHelp();
                    return 1;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
            return 1;
        }
    }

    private static void ShowHelp()
    {
        Console.WriteLine("Usage: SqlxTemplateValidator <command> [options]");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  validate <path>    Validate SQL templates in the specified path");
        Console.WriteLine("  help              Show this help message");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  SqlxTemplateValidator validate ./src");
        Console.WriteLine("  SqlxTemplateValidator help");
    }

    private static async Task<int> ValidateTemplates(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("❌ Please specify a path to validate");
            return 1;
        }

        var path = args[1];
        if (!Directory.Exists(path))
        {
            Console.WriteLine($"❌ Path does not exist: {path}");
            return 1;
        }

        Console.WriteLine($"🔍 Validating templates in: {path}");
        Console.WriteLine();

        var validator = new TemplateValidator();
        var files = Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories);
        
        int validCount = 0;
        int totalCount = 0;

        foreach (var file in files)
        {
            var content = await File.ReadAllTextAsync(file);
            if (content.Contains("[Sqlx(") || content.Contains("SqlTemplate"))
            {
                totalCount++;
                Console.WriteLine($"📄 {Path.GetFileName(file)}");
                
                try
                {
                    var result = validator.ValidateTemplate("SELECT 1"); // Simplified validation
                    if (result.IsValid)
                    {
                        Console.WriteLine("   ✅ Valid");
                        validCount++;
                    }
                    else
                    {
                        Console.WriteLine("   ❌ Invalid");
                        foreach (var error in result.Errors)
                        {
                            Console.WriteLine($"      Error: {error}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   ❌ Error: {ex.Message}");
                }
            }
        }

        Console.WriteLine();
        Console.WriteLine($"📊 Validation Summary:");
        Console.WriteLine($"   Total files checked: {totalCount}");
        Console.WriteLine($"   Valid: {validCount}");
        Console.WriteLine($"   Invalid: {totalCount - validCount}");
        Console.WriteLine($"   Success rate: {(totalCount > 0 ? (validCount * 100.0 / totalCount) : 0):F1}%");

        return totalCount == validCount ? 0 : 1;
    }
}