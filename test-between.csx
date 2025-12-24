using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Sqlx.Generator;
using System.Linq;

var engine = new SqlTemplateEngine();
var code = @"
    using System.Threading.Tasks;
    using System.Collections.Generic;
    
    public class Product 
    { 
        public long Id { get; set; } 
        public string Name { get; set; } 
        public decimal Price { get; set; }
    }
    
    public interface IRepo 
    {
        Task<System.Collections.Generic.List<Product>> GetByPriceRangeAsync(decimal minPrice, decimal maxPrice);
    }";

var compilation = CSharpCompilation.Create("TestAssembly")
    .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
    .AddSyntaxTrees(CSharpSyntaxTree.ParseText(code));

var productType = compilation.GetTypeByMetadataName("Product") as INamedTypeSymbol;
var searchMethod = compilation.GetTypeByMetadataName("IRepo")!
    .GetMembers("GetByPriceRangeAsync").OfType<IMethodSymbol>().First();

var template = "SELECT * FROM products WHERE price {{between|min=@minPrice|max=@maxPrice}}";
var result = engine.ProcessTemplate(template, searchMethod, productType, "products", Sqlx.Generator.SqlDefine.SQLite);

Console.WriteLine("SQL: " + result.ProcessedSql);
Console.WriteLine("Errors: " + string.Join(", ", result.Errors));
Console.WriteLine("Warnings: " + string.Join(", ", result.Warnings));
