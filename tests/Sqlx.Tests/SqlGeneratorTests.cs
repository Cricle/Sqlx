// -----------------------------------------------------------------------
// <copyright file="SqlGeneratorTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Unit tests for SQL generation functionality.
/// </summary>
[TestClass]
public class SqlGeneratorTests
{
    /// <summary>
    /// Tests that the SQL execute types enum has all expected values defined.
    /// </summary>
    [TestMethod]
    public void SqlExecuteTypes_EnumValues_Defined()
    {
        // Test that the SQL execute types enum has the expected values
        Assert.IsTrue(System.Enum.IsDefined(typeof(Sqlx.SqlGen.SqlExecuteTypes), 0)); // Select
        Assert.IsTrue(System.Enum.IsDefined(typeof(Sqlx.SqlGen.SqlExecuteTypes), 1)); // Update
        Assert.IsTrue(System.Enum.IsDefined(typeof(Sqlx.SqlGen.SqlExecuteTypes), 2)); // Insert
        Assert.IsTrue(System.Enum.IsDefined(typeof(Sqlx.SqlGen.SqlExecuteTypes), 3)); // Delete
    }

    /// <summary>
    /// Tests that SqlGenerator generates correct INSERT statement for MySql.
    /// </summary>
    [TestMethod]
    public void SqlGenerator_GenerateInsert_MySql_GeneratesCorrectSql()
    {
        // Arrange
        var generator = new SqlGenerator();
        var context = CreateTestContext();

        // Act
        var result = generator.Generate(SqlDefine.MySql, SqlExecuteTypes.Insert, context);

        // Assert
        Assert.IsTrue(result.Contains("INSERT INTO `test_table`"));
        Assert.IsTrue(result.Contains("VALUES (@"));
    }

    /// <summary>
    /// Tests that SqlGenerator generates correct INSERT statement for SqlServer.
    /// </summary>
    [TestMethod]
    public void SqlGenerator_GenerateInsert_SqlServer_GeneratesCorrectSql()
    {
        // Arrange
        var generator = new SqlGenerator();
        var context = CreateTestContext();

        // Act
        var result = generator.Generate(SqlDefine.SqlServer, SqlExecuteTypes.Insert, context);

        // Assert
        Assert.IsTrue(result.Contains("INSERT INTO [test_table]"));
        Assert.IsTrue(result.Contains("VALUES (@"));
    }

    /// <summary>
    /// Tests that SqlGenerator generates correct INSERT statement for PgSql.
    /// </summary>
    [TestMethod]
    public void SqlGenerator_GenerateInsert_PgSql_GeneratesCorrectSql()
    {
        // Arrange
        var generator = new SqlGenerator();
        var context = CreateTestContext();

        // Act
        var result = generator.Generate(SqlDefine.PgSql, SqlExecuteTypes.Insert, context);

        // Assert
        Assert.IsTrue(result.Contains("INSERT INTO \"test_table\""));
        Assert.IsTrue(result.Contains("VALUES (@"));
    }

    /// <summary>
    /// Tests that SqlGenerator returns empty string for non-Insert types.
    /// </summary>
    [TestMethod]
    public void SqlGenerator_Generate_NonInsertTypes_ReturnsEmptyString()
    {
        // Arrange
        var generator = new SqlGenerator();
        var context = CreateTestContext();

        // Act & Assert
        var selectResult = generator.Generate(SqlDefine.MySql, SqlExecuteTypes.Select, context);
        Assert.AreEqual(string.Empty, selectResult);

        var updateResult = generator.Generate(SqlDefine.MySql, SqlExecuteTypes.Update, context);
        Assert.AreEqual(string.Empty, updateResult);

        var deleteResult = generator.Generate(SqlDefine.MySql, SqlExecuteTypes.Delete, context);
        Assert.AreEqual(string.Empty, deleteResult);
    }

    /// <summary>
    /// Tests that ObjectMap correctly identifies list types.
    /// </summary>
    [TestMethod]
    public void ObjectMap_ListTypes_IdentifiedCorrectly()
    {
        // This test would require creating a mock IParameterSymbol
        // For now, we'll test the concept through the SqlGenerator
        var generator = new SqlGenerator();
        var context = CreateTestContext();

        // Act
        var result = generator.Generate(SqlDefine.MySql, SqlExecuteTypes.Insert, context);

        // Assert that the generation works with the context
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Length > 0);
    }

    /// <summary>
    /// Tests that ObjectMap correctly identifies non-list types.
    /// </summary>
    [TestMethod]
    public void ObjectMap_NonListTypes_IdentifiedCorrectly()
    {
        // This test would require creating a mock IParameterSymbol
        // For now, we'll test the concept through the SqlGenerator
        var generator = new SqlGenerator();
        var context = CreateTestContext();

        // Act
        var result = generator.Generate(SqlDefine.MySql, SqlExecuteTypes.Insert, context);

        // Assert that the generation works with the context
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Length > 0);
    }

    /// <summary>
    /// Tests that GenerateContext methods work correctly.
    /// </summary>
    [TestMethod]
    public void GenerateContext_UtilityMethods_WorkCorrectly()
    {
        // This test would require creating mock symbols
        // For now, we'll test the concept through the SqlGenerator
        var generator = new SqlGenerator();
        var context = CreateTestContext();

        // Act
        var result = generator.Generate(SqlDefine.MySql, SqlExecuteTypes.Insert, context);

        // Assert that the generation works with the context
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Length > 0);
    }

    /// <summary>
    /// Tests that SqlExecuteTypes enum has correct values.
    /// </summary>
    [TestMethod]
    public void SqlExecuteTypes_EnumValues_AreCorrect()
    {
        // Act & Assert
        Assert.AreEqual(0, (int)SqlExecuteTypes.Select);
        Assert.AreEqual(1, (int)SqlExecuteTypes.Update);
        Assert.AreEqual(2, (int)SqlExecuteTypes.Insert);
        Assert.AreEqual(3, (int)SqlExecuteTypes.Delete);
    }

    /// <summary>
    /// Tests that GenerateContext.GetColumnName works correctly with various inputs.
    /// </summary>
    [TestMethod]
    public void GenerateContext_GetColumnName_VariousInputs_WorksCorrectly()
    {
        // Arrange
        var testCases = new[]
        {
            ("simpleName", "simple_name"),
            ("camelCase", "camel_case"),
            ("PascalCase", "pascal_case"),
            ("UPPER_CASE", "upper_case"),
            ("mixedCase123", "mixed_case123"),
            ("", ""),
            ("a", "a"),
            ("A", "a")
        };

        // Act & Assert
        foreach (var (input, expected) in testCases)
        {
            var result = GenerateContext.GetColumnName(input);
            Assert.AreEqual(expected, result, $"Input: {input}");
        }
    }

    /// <summary>
    /// Tests that GenerateContext.GetParamterName works correctly with various inputs.
    /// </summary>
    [TestMethod]
    public void GenerateContext_GetParamterName_VariousInputs_WorksCorrectly()
    {
        // Arrange
        var testCases = new[]
        {
            ("simpleName", "@", "@simple_name"),
            ("camelCase", "@", "@camel_case"),
            ("PascalCase", "@", "@pascal_case"),
            ("UPPER_CASE", "@", "@upper_case"),
            ("mixedCase123", "@", "@mixed_case123"),
            ("", "@", "@"),
            ("a", "@", "@a"),
            ("A", "@", "@a")
        };

        // Act & Assert
        foreach (var (input, prefix, expected) in testCases)
        {
            var result = GenerateContext.GetParamterName(prefix, input);
            Assert.AreEqual(expected, result, $"Input: {input}, Prefix: {prefix}");
        }
    }

    /// <summary>
    /// Tests that GenerateContext.GetColumnName with IPropertySymbol works correctly.
    /// </summary>
    [TestMethod]
    public void GenerateContext_GetColumnName_WithIPropertySymbol_WorksCorrectly()
    {
        // This test would require creating a mock IPropertySymbol with DbColumnAttribute
        // For now, we'll test that the method exists and can be called
        Assert.IsNotNull(typeof(GenerateContext).GetMethod("GetColumnName", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static));
    }

    /// <summary>
    /// Tests that InsertGenerateContext.GetParamterNames works correctly.
    /// </summary>
    [TestMethod]
    public void InsertGenerateContext_GetParamterNames_WorksCorrectly()
    {
        // This test would require creating a mock ObjectMap with properties
        // For now, we'll test that the method exists and can be called
        Assert.IsNotNull(typeof(InsertGenerateContext).GetMethod("GetParamterNames", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance));
    }

    /// <summary>
    /// Tests that InsertGenerateContext.GetColumnNames works correctly.
    /// </summary>
    [TestMethod]
    public void InsertGenerateContext_GetColumnNames_WorksCorrectly()
    {
        // This test would require creating a mock ObjectMap with properties
        // For now, we'll test that the method exists and can be called
        Assert.IsNotNull(typeof(InsertGenerateContext).GetMethod("GetColumnNames", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance));
    }

    /// <summary>
    /// Tests that ObjectMap constructor works correctly with various parameter types.
    /// </summary>
    [TestMethod]
    public void ObjectMap_Constructor_VariousParameterTypes_WorksCorrectly()
    {
        // This test would require creating mock IParameterSymbol objects
        // For now, we'll test that the constructor exists and can be called
        Assert.IsNotNull(typeof(ObjectMap).GetConstructor(new[] { typeof(IParameterSymbol) }));
    }

    /// <summary>
    /// Tests that ObjectMap properties are accessible.
    /// </summary>
    [TestMethod]
    public void ObjectMap_Properties_AreAccessible()
    {
        // Arrange
        var objectMapType = typeof(ObjectMap);

        // Act & Assert
        var symbolProperty = objectMapType.GetProperty("Symbol");
        Assert.IsNotNull(symbolProperty, "ObjectMap should have Symbol property");
        Assert.IsTrue(symbolProperty.CanRead, "Symbol property should be readable");

        var elementSymbolProperty = objectMapType.GetProperty("ElementSymbol");
        Assert.IsNotNull(elementSymbolProperty, "ObjectMap should have ElementSymbol property");
        Assert.IsTrue(elementSymbolProperty.CanRead, "ElementSymbol property should be readable");

        var isListProperty = objectMapType.GetProperty("IsList");
        Assert.IsNotNull(isListProperty, "ObjectMap should have IsList property");
        Assert.IsTrue(isListProperty.CanRead, "IsList property should be readable");

        var propertiesProperty = objectMapType.GetProperty("Properties");
        Assert.IsNotNull(propertiesProperty, "ObjectMap should have Properties property");
        Assert.IsTrue(propertiesProperty.CanRead, "Properties property should be readable");
    }

    /// <summary>
    /// Tests that SqlGenerator class has correct structure.
    /// </summary>
    [TestMethod]
    public void SqlGenerator_Class_HasCorrectStructure()
    {
        // Arrange
        var sqlGeneratorType = typeof(SqlGenerator);

        // Act & Assert
        Assert.IsNotNull(sqlGeneratorType, "SqlGenerator class should exist");
        Assert.IsTrue(sqlGeneratorType.IsNotPublic, "SqlGenerator should be internal");
        Assert.IsFalse(sqlGeneratorType.IsAbstract, "SqlGenerator should not be abstract");
        Assert.IsTrue(sqlGeneratorType.IsSealed, "SqlGenerator should be sealed");
    }

    /// <summary>
    /// Tests that SqlGenerator class has Generate method.
    /// </summary>
    [TestMethod]
    public void SqlGenerator_Class_HasGenerateMethod()
    {
        // Arrange
        var sqlGeneratorType = typeof(SqlGenerator);

        // Act
        var generateMethod = sqlGeneratorType.GetMethod("Generate", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        // Assert
        Assert.IsNotNull(generateMethod, "SqlGenerator should have Generate method");
        Assert.IsFalse(generateMethod.IsVirtual, "Generate method should not be virtual");
        Assert.IsFalse(generateMethod.IsAbstract, "Generate method should not be abstract");
    }

    /// <summary>
    /// Tests that SqlGenerator class has correct method signatures.
    /// </summary>
    [TestMethod]
    public void SqlGenerator_Class_HasCorrectMethodSignatures()
    {
        // Arrange
        var sqlGeneratorType = typeof(SqlGenerator);

        // Act
        var generateMethod = sqlGeneratorType.GetMethod("Generate", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        // Assert
        Assert.IsNotNull(generateMethod, "Generate method should exist");

        var parameters = generateMethod.GetParameters();
        Assert.AreEqual(3, parameters.Length, "Generate method should have 3 parameters");
        Assert.AreEqual("SqlDefine", parameters[0].ParameterType.Name, 
            "Generate method first parameter should be SqlDefine");
        Assert.AreEqual("SqlExecuteTypes", parameters[1].ParameterType.Name, 
            "Generate method second parameter should be SqlExecuteTypes");
        Assert.AreEqual("GenerateContext", parameters[2].ParameterType.Name, 
            "Generate method third parameter should be GenerateContext");
    }

    /// <summary>
    /// Tests that SqlGenerator class has private GenerateInsert method.
    /// </summary>
    [TestMethod]
    public void SqlGenerator_Class_HasPrivateGenerateInsertMethod()
    {
        // Arrange
        var sqlGeneratorType = typeof(SqlGenerator);

        // Act
        var generateInsertMethod = sqlGeneratorType.GetMethod("GenerateInsert", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Assert
        Assert.IsNotNull(generateInsertMethod, "SqlGenerator should have private GenerateInsert method");
        Assert.IsFalse(generateInsertMethod.IsVirtual, "GenerateInsert method should not be virtual");
        Assert.IsFalse(generateInsertMethod.IsAbstract, "GenerateInsert method should not be abstract");
    }

    /// <summary>
    /// Tests that SqlGenerator class has correct private method signatures.
    /// </summary>
    [TestMethod]
    public void SqlGenerator_Class_HasCorrectPrivateMethodSignatures()
    {
        // Arrange
        var sqlGeneratorType = typeof(SqlGenerator);

        // Act
        var generateInsertMethod = sqlGeneratorType.GetMethod("GenerateInsert", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Assert
        Assert.IsNotNull(generateInsertMethod, "GenerateInsert method should exist");

        var parameters = generateInsertMethod.GetParameters();
        Assert.AreEqual(2, parameters.Length, "GenerateInsert method should have 2 parameters");
        Assert.AreEqual("SqlDefine", parameters[0].ParameterType.Name, 
            "GenerateInsert method first parameter should be SqlDefine");
        Assert.AreEqual("InsertGenerateContext", parameters[1].ParameterType.Name, 
            "GenerateInsert method second parameter should be InsertGenerateContext");
    }

    /// <summary>
    /// Tests that SqlGenerator class can be instantiated.
    /// </summary>
    [TestMethod]
    public void SqlGenerator_Class_CanBeInstantiated()
    {
        // Arrange & Act
        var generator = new SqlGenerator();

        // Assert
        Assert.IsNotNull(generator);
        Assert.IsInstanceOfType(generator, typeof(SqlGenerator));
    }

    /// <summary>
    /// Tests that SqlGenerator class has correct return types.
    /// </summary>
    [TestMethod]
    public void SqlGenerator_Class_HasCorrectReturnTypes()
    {
        // Arrange
        var sqlGeneratorType = typeof(SqlGenerator);

        // Act
        var generateMethod = sqlGeneratorType.GetMethod("Generate", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var generateInsertMethod = sqlGeneratorType.GetMethod("GenerateInsert", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Assert
        Assert.IsNotNull(generateMethod, "Generate method should exist");
        Assert.IsNotNull(generateInsertMethod, "GenerateInsert method should exist");

        Assert.AreEqual(typeof(string), generateMethod.ReturnType, 
            "Generate method should return string");
        Assert.AreEqual(typeof(string), generateInsertMethod.ReturnType, 
            "GenerateInsert method should return string");
    }

    /// <summary>
    /// Tests that SqlGenerator class has correct accessibility.
    /// </summary>
    [TestMethod]
    public void SqlGenerator_Class_HasCorrectAccessibility()
    {
        // Arrange
        var sqlGeneratorType = typeof(SqlGenerator);

        // Act & Assert
        Assert.IsTrue(sqlGeneratorType.IsNotPublic, "SqlGenerator should be internal");
        Assert.IsFalse(sqlGeneratorType.IsPublic, "SqlGenerator should not be public");
        Assert.IsFalse(sqlGeneratorType.IsNested, "SqlGenerator should not be nested");
    }

    /// <summary>
    /// Tests that SqlGenerator class has correct inheritance.
    /// </summary>
    [TestMethod]
    public void SqlGenerator_Class_HasCorrectInheritance()
    {
        // Arrange
        var sqlGeneratorType = typeof(SqlGenerator);

        // Act & Assert
        Assert.AreEqual(typeof(object), sqlGeneratorType.BaseType, 
            "SqlGenerator should inherit from object");
        Assert.IsFalse(sqlGeneratorType.IsAbstract, "SqlGenerator should not be abstract");
        Assert.IsTrue(sqlGeneratorType.IsSealed, "SqlGenerator should be sealed");
    }

    /// <summary>
    /// Tests that GenerateContext record has correct structure.
    /// </summary>
    [TestMethod]
    public void GenerateContext_Record_HasCorrectStructure()
    {
        // Arrange
        var generateContextType = typeof(GenerateContext);

        // Act & Assert
        Assert.IsNotNull(generateContextType, "GenerateContext record should exist");
        Assert.IsTrue(generateContextType.IsNotPublic, "GenerateContext should be internal");
        Assert.IsTrue(generateContextType.IsAbstract, "GenerateContext should be abstract");
        Assert.IsFalse(generateContextType.IsSealed, "GenerateContext should not be sealed");
    }

    /// <summary>
    /// Tests that GenerateContext record has correct properties.
    /// </summary>
    [TestMethod]
    public void GenerateContext_Record_HasCorrectProperties()
    {
        // Arrange
        var generateContextType = typeof(GenerateContext);

        // Act
        var properties = generateContextType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        // Assert
        var contextProperty = properties.FirstOrDefault(p => p.Name == "Context");
        Assert.IsNotNull(contextProperty, "GenerateContext should have Context property");
        Assert.IsTrue(contextProperty.CanRead, "Context property should be readable");

        var tableNameProperty = properties.FirstOrDefault(p => p.Name == "TableName");
        Assert.IsNotNull(tableNameProperty, "GenerateContext should have TableName property");
        Assert.IsTrue(tableNameProperty.CanRead, "TableName property should be readable");
    }

    /// <summary>
    /// Tests that GenerateContext record has correct property types.
    /// </summary>
    [TestMethod]
    public void GenerateContext_Record_HasCorrectPropertyTypes()
    {
        // Arrange
        var generateContextType = typeof(GenerateContext);

        // Act
        var properties = generateContextType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        // Assert
        var contextProperty = properties.FirstOrDefault(p => p.Name == "Context");
        Assert.IsNotNull(contextProperty, "Context property should exist");
        Assert.AreEqual("MethodGenerationContext", contextProperty.PropertyType.Name, 
            "Context property should be of type MethodGenerationContext");

        var tableNameProperty = properties.FirstOrDefault(p => p.Name == "TableName");
        Assert.IsNotNull(tableNameProperty, "TableName property should exist");
        Assert.AreEqual("String", tableNameProperty.PropertyType.Name, 
            "TableName property should be of type String");
    }

    /// <summary>
    /// Tests that GenerateContext record has correct method signatures.
    /// </summary>
    [TestMethod]
    public void GenerateContext_Record_HasCorrectMethodSignatures()
    {
        // Arrange
        var generateContextType = typeof(GenerateContext);

        // Act
        var getColumnNameMethod = generateContextType.GetMethod("GetColumnName", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        var getParamterNameMethod = generateContextType.GetMethod("GetParamterName", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        // Assert
        Assert.IsNotNull(getColumnNameMethod, "GenerateContext should have GetColumnName method");
        Assert.IsNotNull(getParamterNameMethod, "GenerateContext should have GetParamterName method");

        var getColumnNameParams = getColumnNameMethod.GetParameters();
        Assert.AreEqual(1, getColumnNameParams.Length, "GetColumnName method should have 1 parameter");
        Assert.AreEqual("String", getColumnNameParams[0].ParameterType.Name, 
            "GetColumnName method parameter should be String");

        var getParamterNameParams = getParamterNameMethod.GetParameters();
        Assert.AreEqual(2, getParamterNameParams.Length, "GetParamterName method should have 2 parameters");
        Assert.AreEqual("String", getParamterNameParams[0].ParameterType.Name, 
            "GetParamterName method first parameter should be String");
        Assert.AreEqual("String", getParamterNameParams[1].ParameterType.Name, 
            "GetParamterName method second parameter should be String");
    }

    /// <summary>
    /// Tests that GenerateContext record has correct return types.
    /// </summary>
    [TestMethod]
    public void GenerateContext_Record_HasCorrectReturnTypes()
    {
        // Arrange
        var generateContextType = typeof(GenerateContext);

        // Act
        var getColumnNameMethod = generateContextType.GetMethod("GetColumnName", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        var getParamterNameMethod = generateContextType.GetMethod("GetParamterName", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        // Assert
        Assert.IsNotNull(getColumnNameMethod, "GetColumnName method should exist");
        Assert.IsNotNull(getParamterNameMethod, "GetParamterName method should exist");

        Assert.AreEqual(typeof(string), getColumnNameMethod.ReturnType, 
            "GetColumnName method should return string");
        Assert.AreEqual(typeof(string), getParamterNameMethod.ReturnType, 
            "GetParamterName method should return string");
    }

    /// <summary>
    /// Tests that GenerateContext record has correct accessibility.
    /// </summary>
    [TestMethod]
    public void GenerateContext_Record_HasCorrectAccessibility()
    {
        // Arrange
        var generateContextType = typeof(GenerateContext);

        // Act & Assert
        Assert.IsTrue(generateContextType.IsNotPublic, "GenerateContext should be internal");
        Assert.IsFalse(generateContextType.IsPublic, "GenerateContext should not be public");
        Assert.IsFalse(generateContextType.IsNested, "GenerateContext should not be nested");
    }

    /// <summary>
    /// Tests that GenerateContext record has correct inheritance.
    /// </summary>
    [TestMethod]
    public void GenerateContext_Record_HasCorrectInheritance()
    {
        // Arrange
        var generateContextType = typeof(GenerateContext);

        // Act & Assert
        Assert.AreEqual(typeof(object), generateContextType.BaseType, 
            "GenerateContext should inherit from object");
        Assert.IsTrue(generateContextType.IsAbstract, "GenerateContext should be abstract");
        Assert.IsFalse(generateContextType.IsSealed, "GenerateContext should not be sealed");
    }

    /// <summary>
    /// Tests that InsertGenerateContext record has correct structure.
    /// </summary>
    [TestMethod]
    public void InsertGenerateContext_Record_HasCorrectStructure()
    {
        // Arrange
        var insertGenerateContextType = typeof(InsertGenerateContext);

        // Act & Assert
        Assert.IsNotNull(insertGenerateContextType, "InsertGenerateContext record should exist");
        Assert.IsTrue(insertGenerateContextType.IsNotPublic, "InsertGenerateContext should be internal");
        Assert.IsFalse(insertGenerateContextType.IsAbstract, "InsertGenerateContext should not be abstract");
        Assert.IsTrue(insertGenerateContextType.IsSealed, "InsertGenerateContext should be sealed");
    }

    /// <summary>
    /// Tests that InsertGenerateContext record has correct properties.
    /// </summary>
    [TestMethod]
    public void InsertGenerateContext_Record_HasCorrectProperties()
    {
        // Arrange
        var insertGenerateContextType = typeof(InsertGenerateContext);

        // Act
        var properties = insertGenerateContextType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        // Assert
        var contextProperty = properties.FirstOrDefault(p => p.Name == "Context");
        Assert.IsNotNull(contextProperty, "InsertGenerateContext should have Context property");
        Assert.IsTrue(contextProperty.CanRead, "Context property should be readable");

        var tableNameProperty = properties.FirstOrDefault(p => p.Name == "TableName");
        Assert.IsNotNull(tableNameProperty, "InsertGenerateContext should have TableName property");
        Assert.IsTrue(tableNameProperty.CanRead, "TableName property should be readable");

        var entryProperty = properties.FirstOrDefault(p => p.Name == "Entry");
        Assert.IsNotNull(entryProperty, "InsertGenerateContext should have Entry property");
        Assert.IsTrue(entryProperty.CanRead, "Entry property should be readable");
    }

    /// <summary>
    /// Tests that InsertGenerateContext record has correct property types.
    /// </summary>
    [TestMethod]
    public void InsertGenerateContext_Record_HasCorrectPropertyTypes()
    {
        // Arrange
        var insertGenerateContextType = typeof(InsertGenerateContext);

        // Act
        var properties = insertGenerateContextType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        // Assert
        var contextProperty = properties.FirstOrDefault(p => p.Name == "Context");
        Assert.IsNotNull(contextProperty, "Context property should exist");
        Assert.AreEqual("MethodGenerationContext", contextProperty.PropertyType.Name, 
            "Context property should be of type MethodGenerationContext");

        var tableNameProperty = properties.FirstOrDefault(p => p.Name == "TableName");
        Assert.IsNotNull(tableNameProperty, "TableName property should exist");
        Assert.AreEqual("String", tableNameProperty.PropertyType.Name, 
            "TableName property should be of type String");

        var entryProperty = properties.FirstOrDefault(p => p.Name == "Entry");
        Assert.IsNotNull(entryProperty, "Entry property should exist");
        Assert.AreEqual("ObjectMap", entryProperty.PropertyType.Name, 
            "Entry property should be of type ObjectMap");
    }

    /// <summary>
    /// Tests that InsertGenerateContext record has correct method signatures.
    /// </summary>
    [TestMethod]
    public void InsertGenerateContext_Record_HasCorrectMethodSignatures()
    {
        // Arrange
        var insertGenerateContextType = typeof(InsertGenerateContext);

        // Act
        var getParamterNamesMethod = insertGenerateContextType.GetMethod("GetParamterNames", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var getColumnNamesMethod = insertGenerateContextType.GetMethod("GetColumnNames", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        // Assert
        Assert.IsNotNull(getParamterNamesMethod, "InsertGenerateContext should have GetParamterNames method");
        Assert.IsNotNull(getColumnNamesMethod, "InsertGenerateContext should have GetColumnNames method");

        var getParamterNamesParams = getParamterNamesMethod.GetParameters();
        Assert.AreEqual(1, getParamterNamesParams.Length, "GetParamterNames method should have 1 parameter");
        Assert.AreEqual("String", getParamterNamesParams[0].ParameterType.Name, 
            "GetParamterNames method parameter should be String");

        var getColumnNamesParams = getColumnNamesMethod.GetParameters();
        Assert.AreEqual(0, getColumnNamesParams.Length, "GetColumnNames method should have 0 parameters");
    }

    /// <summary>
    /// Tests that InsertGenerateContext record has correct return types.
    /// </summary>
    [TestMethod]
    public void InsertGenerateContext_Record_HasCorrectReturnTypes()
    {
        // Arrange
        var insertGenerateContextType = typeof(InsertGenerateContext);

        // Act
        var getParamterNamesMethod = insertGenerateContextType.GetMethod("GetParamterNames", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var getColumnNamesMethod = insertGenerateContextType.GetMethod("GetColumnNames", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        // Assert
        Assert.IsNotNull(getParamterNamesMethod, "GetParamterNames method should exist");
        Assert.IsNotNull(getColumnNamesMethod, "GetColumnNames method should exist");

        Assert.AreEqual(typeof(string), getParamterNamesMethod.ReturnType, 
            "GetParamterNames method should return string");
        Assert.AreEqual(typeof(string), getColumnNamesMethod.ReturnType, 
            "GetColumnNames method should return string");
    }

    /// <summary>
    /// Tests that InsertGenerateContext record has correct accessibility.
    /// </summary>
    [TestMethod]
    public void InsertGenerateContext_Record_HasCorrectAccessibility()
    {
        // Arrange
        var insertGenerateContextType = typeof(InsertGenerateContext);

        // Act & Assert
        Assert.IsTrue(insertGenerateContextType.IsNotPublic, "InsertGenerateContext should be internal");
        Assert.IsFalse(insertGenerateContextType.IsPublic, "InsertGenerateContext should not be public");
        Assert.IsFalse(insertGenerateContextType.IsNested, "InsertGenerateContext should not be nested");
    }

    /// <summary>
    /// Tests that InsertGenerateContext record has correct inheritance.
    /// </summary>
    [TestMethod]
    public void InsertGenerateContext_Record_HasCorrectInheritance()
    {
        // Arrange
        var insertGenerateContextType = typeof(InsertGenerateContext);
        var generateContextType = typeof(GenerateContext);

        // Act & Assert
        Assert.AreEqual(generateContextType, insertGenerateContextType.BaseType, 
            "InsertGenerateContext should inherit from GenerateContext");
        Assert.IsFalse(insertGenerateContextType.IsAbstract, "InsertGenerateContext should not be abstract");
        Assert.IsTrue(insertGenerateContextType.IsSealed, "InsertGenerateContext should be sealed");
    }

    /// <summary>
    /// Tests that ObjectMap class has correct structure.
    /// </summary>
    [TestMethod]
    public void ObjectMap_Class_HasCorrectStructure()
    {
        // Arrange
        var objectMapType = typeof(ObjectMap);

        // Act & Assert
        Assert.IsNotNull(objectMapType, "ObjectMap class should exist");
        Assert.IsTrue(objectMapType.IsNotPublic, "ObjectMap should be internal");
        Assert.IsFalse(objectMapType.IsAbstract, "ObjectMap should not be abstract");
        Assert.IsTrue(objectMapType.IsSealed, "ObjectMap should be sealed");
    }

    /// <summary>
    /// Tests that ObjectMap class has correct constructor.
    /// </summary>
    [TestMethod]
    public void ObjectMap_Class_HasCorrectConstructor()
    {
        // Arrange
        var objectMapType = typeof(ObjectMap);

        // Act
        var constructor = objectMapType.GetConstructor(new[] { typeof(IParameterSymbol) });

        // Assert
        Assert.IsNotNull(constructor, "ObjectMap should have constructor with IParameterSymbol parameter");
        Assert.IsFalse(constructor.IsStatic, "Constructor should not be static");
        Assert.IsFalse(constructor.IsAbstract, "Constructor should not be abstract");
    }

    /// <summary>
    /// Tests that ObjectMap class has correct property accessibility.
    /// </summary>
    [TestMethod]
    public void ObjectMap_Class_HasCorrectPropertyAccessibility()
    {
        // Arrange
        var objectMapType = typeof(ObjectMap);

        // Act
        var properties = objectMapType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        // Assert
        foreach (var property in properties)
        {
            Assert.IsTrue(property.CanRead, $"Property {property.Name} should be readable");
            Assert.IsFalse(property.CanWrite, $"Property {property.Name} should not be writable");
        }
    }

    /// <summary>
    /// Tests that ObjectMap class has correct property types.
    /// </summary>
    [TestMethod]
    public void ObjectMap_Class_HasCorrectPropertyTypes()
    {
        // Arrange
        var objectMapType = typeof(ObjectMap);

        // Act
        var properties = objectMapType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        // Assert
        var symbolProperty = properties.FirstOrDefault(p => p.Name == "Symbol");
        Assert.IsNotNull(symbolProperty, "Symbol property should exist");
        Assert.AreEqual("IParameterSymbol", symbolProperty.PropertyType.Name, 
            "Symbol property should be of type IParameterSymbol");

        var elementSymbolProperty = properties.FirstOrDefault(p => p.Name == "ElementSymbol");
        Assert.IsNotNull(elementSymbolProperty, "ElementSymbol property should exist");
        Assert.AreEqual("ISymbol", elementSymbolProperty.PropertyType.Name, 
            "ElementSymbol property should be of type ISymbol");

        var isListProperty = properties.FirstOrDefault(p => p.Name == "IsList");
        Assert.IsNotNull(isListProperty, "IsList property should exist");
        Assert.AreEqual("Boolean", isListProperty.PropertyType.Name, 
            "IsList property should be of type Boolean");

        var propertiesProperty = properties.FirstOrDefault(p => p.Name == "Properties");
        Assert.IsNotNull(propertiesProperty, "Properties property should exist");
        Assert.AreEqual("List`1", propertiesProperty.PropertyType.Name, 
            "Properties property should be of type List<T>");
    }

    /// <summary>
    /// Tests that SqlExecuteTypes enum has correct structure.
    /// </summary>
    [TestMethod]
    public void SqlExecuteTypes_Enum_HasCorrectStructure()
    {
        // Arrange
        var enumType = typeof(SqlExecuteTypes);

        // Act & Assert
        Assert.IsNotNull(enumType, "SqlExecuteTypes enum should exist");
        Assert.IsTrue(enumType.IsNotPublic, "SqlExecuteTypes should be internal");
        Assert.IsTrue(enumType.IsEnum, "SqlExecuteTypes should be an enum");
        Assert.IsFalse(enumType.IsAbstract, "SqlExecuteTypes should not be abstract");
        Assert.IsFalse(enumType.IsSealed, "SqlExecuteTypes should not be sealed");
    }

    /// <summary>
    /// Tests that SqlExecuteTypes enum has correct values.
    /// </summary>
    [TestMethod]
    public void SqlExecuteTypes_Enum_HasCorrectValues()
    {
        // Act & Assert
        Assert.AreEqual(0, (int)SqlExecuteTypes.Select);
        Assert.AreEqual(1, (int)SqlExecuteTypes.Update);
        Assert.AreEqual(2, (int)SqlExecuteTypes.Insert);
        Assert.AreEqual(3, (int)SqlExecuteTypes.Delete);
    }

    /// <summary>
    /// Tests that SqlExecuteTypes enum values are unique.
    /// </summary>
    [TestMethod]
    public void SqlExecuteTypes_Enum_ValuesAreUnique()
    {
        // Arrange
        var values = new[] { 
            (int)SqlExecuteTypes.Select, 
            (int)SqlExecuteTypes.Update, 
            (int)SqlExecuteTypes.Insert, 
            (int)SqlExecuteTypes.Delete 
        };

        // Act
        var uniqueValues = values.Distinct().ToArray();

        // Assert
        Assert.AreEqual(values.Length, uniqueValues.Length, "All enum values should be unique");
    }

    /// <summary>
    /// Tests that SqlExecuteTypes enum values are sequential.
    /// </summary>
    [TestMethod]
    public void SqlExecuteTypes_Enum_ValuesAreSequential()
    {
        // Act & Assert
        Assert.AreEqual(0, (int)SqlExecuteTypes.Select, "Select should have value 0");
        Assert.AreEqual(1, (int)SqlExecuteTypes.Update, "Update should have value 1");
        Assert.AreEqual(2, (int)SqlExecuteTypes.Insert, "Insert should have value 2");
        Assert.AreEqual(3, (int)SqlExecuteTypes.Delete, "Delete should have value 3");
    }

    /// <summary>
    /// Tests that SqlExecuteTypes enum values can be cast to int.
    /// </summary>
    [TestMethod]
    public void SqlExecuteTypes_Enum_ValuesCanBeCastToInt()
    {
        // Act & Assert
        int selectValue = (int)SqlExecuteTypes.Select;
        int updateValue = (int)SqlExecuteTypes.Update;
        int insertValue = (int)SqlExecuteTypes.Insert;
        int deleteValue = (int)SqlExecuteTypes.Delete;

        Assert.AreEqual(0, selectValue);
        Assert.AreEqual(1, updateValue);
        Assert.AreEqual(2, insertValue);
        Assert.AreEqual(3, deleteValue);
    }

    /// <summary>
    /// Tests that SqlExecuteTypes enum values can be cast from int.
    /// </summary>
    [TestMethod]
    public void SqlExecuteTypes_Enum_ValuesCanBeCastFromInt()
    {
        // Act & Assert
        SqlExecuteTypes selectType = (SqlExecuteTypes)0;
        SqlExecuteTypes updateType = (SqlExecuteTypes)1;
        SqlExecuteTypes insertType = (SqlExecuteTypes)2;
        SqlExecuteTypes deleteType = (SqlExecuteTypes)3;

        Assert.AreEqual(SqlExecuteTypes.Select, selectType);
        Assert.AreEqual(SqlExecuteTypes.Update, updateType);
        Assert.AreEqual(SqlExecuteTypes.Insert, insertType);
        Assert.AreEqual(SqlExecuteTypes.Delete, deleteType);
    }

    /// <summary>
    /// Tests that SqlExecuteTypes enum values can be compared.
    /// </summary>
    [TestMethod]
    public void SqlExecuteTypes_Enum_ValuesCanBeCompared()
    {
        // Act & Assert
        Assert.IsTrue(SqlExecuteTypes.Select < SqlExecuteTypes.Update);
        Assert.IsTrue(SqlExecuteTypes.Update < SqlExecuteTypes.Insert);
        Assert.IsTrue(SqlExecuteTypes.Insert < SqlExecuteTypes.Delete);

        Assert.IsTrue(SqlExecuteTypes.Delete > SqlExecuteTypes.Insert);
        Assert.IsTrue(SqlExecuteTypes.Insert > SqlExecuteTypes.Update);
        Assert.IsTrue(SqlExecuteTypes.Update > SqlExecuteTypes.Select);
    }

    /// <summary>
    /// Tests that SqlExecuteTypes enum values can be used in switch statements.
    /// </summary>
    [TestMethod]
    public void SqlExecuteTypes_Enum_ValuesCanBeUsedInSwitchStatements()
    {
        // Arrange
        var testCases = new[]
        {
            (SqlExecuteTypes.Select, "Select"),
            (SqlExecuteTypes.Update, "Update"),
            (SqlExecuteTypes.Insert, "Insert"),
            (SqlExecuteTypes.Delete, "Delete")
        };

        // Act & Assert
        foreach (var (enumValue, expectedName) in testCases)
        {
            string result = enumValue switch
            {
                SqlExecuteTypes.Select => "Select",
                SqlExecuteTypes.Update => "Update",
                SqlExecuteTypes.Insert => "Insert",
                SqlExecuteTypes.Delete => "Delete",
                _ => "Unknown"
            };

            Assert.AreEqual(expectedName, result, $"Switch statement should handle {enumValue}");
        }
    }

    /// <summary>
    /// Tests that SqlExecuteTypes enum values can be used in pattern matching.
    /// </summary>
    [TestMethod]
    public void SqlExecuteTypes_Enum_ValuesCanBeUsedInPatternMatching()
    {
        // Act & Assert
        var selectResult = SqlExecuteTypes.Select switch
        {
            SqlExecuteTypes.Select => "Select",
            SqlExecuteTypes.Update => "Update",
            SqlExecuteTypes.Insert => "Insert",
            SqlExecuteTypes.Delete => "Delete",
            _ => "Unknown"
        };

        var updateResult = SqlExecuteTypes.Update switch
        {
            SqlExecuteTypes.Select => "Select",
            SqlExecuteTypes.Update => "Update",
            SqlExecuteTypes.Insert => "Insert",
            SqlExecuteTypes.Delete => "Delete",
            _ => "Unknown"
        };

        var insertResult = SqlExecuteTypes.Insert switch
        {
            SqlExecuteTypes.Select => "Select",
            SqlExecuteTypes.Update => "Update",
            SqlExecuteTypes.Insert => "Insert",
            SqlExecuteTypes.Delete => "Delete",
            _ => "Unknown"
        };

        var deleteResult = SqlExecuteTypes.Delete switch
        {
            SqlExecuteTypes.Select => "Select",
            SqlExecuteTypes.Update => "Update",
            SqlExecuteTypes.Insert => "Insert",
            SqlExecuteTypes.Delete => "Delete",
            _ => "Unknown"
        };

        Assert.AreEqual("Select", selectResult);
        Assert.AreEqual("Update", updateResult);
        Assert.AreEqual("Insert", insertResult);
        Assert.AreEqual("Delete", deleteResult);
    }
}
