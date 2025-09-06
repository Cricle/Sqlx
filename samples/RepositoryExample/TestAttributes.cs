// Test file to check if attributes are available
using System;
using System.Reflection;
using Sqlx.Annotations;

/// <summary>
/// Test class to check if attributes are available.
/// </summary>
public class TestAttributes
{
    /// <summary>
    /// Checks if the repository attributes are available.
    /// </summary>
    public static void CheckAttributes()
    {
        try
        {
            var repositoryForType = typeof(RepositoryForAttribute);
            Console.WriteLine($"RepositoryForAttribute found: {repositoryForType.FullName}");
            
            var tableNameType = typeof(TableNameAttribute);
            Console.WriteLine($"TableNameAttribute found: {tableNameType.FullName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
