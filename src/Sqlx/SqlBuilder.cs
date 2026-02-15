// <copyright file="SqlBuilder.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx;

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

/// <summary>
/// High-performance SQL builder using ArrayPool for memory efficiency.
/// Must be disposed to return rented arrays to the pool.
/// Supports interpolated string syntax for safe SQL construction.
/// </summary>
/// <remarks>
/// <para>
/// SqlBuilder provides a fluent API for constructing dynamic SQL queries with automatic parameterization.
/// It uses ArrayPool&lt;char&gt; for efficient memory management and supports integration with existing
/// Sqlx infrastructure including SqlTemplate placeholders and subquery composition.
/// </para>
/// <para>
/// Example usage:
/// <code>
/// using var builder = new SqlBuilder(SqlDefine.SQLite);
/// builder.Append($"SELECT * FROM users WHERE age >= {18}");
/// var (sql, parameters) = builder.Build();
/// // sql: "SELECT * FROM users WHERE age >= @p0"
/// // parameters: { "p0": 18 }
/// </code>
/// </para>
/// </remarks>
public sealed class SqlBuilder : IDisposable
{
    private char[] _buffer;
    private int _position;
    private Dictionary<string, object?> _parameters;
    private readonly SqlDialect _dialect;
    private readonly PlaceholderContext? _context;
    private bool _disposed;
    private bool _built;
    private int _parameterCounter;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlBuilder"/> struct with the specified dialect.
    /// </summary>
    /// <param name="dialect">The SQL dialect for database-specific SQL generation.</param>
    /// <param name="initialCapacity">The initial buffer capacity in characters. Default is 1024.</param>
    /// <exception cref="ArgumentNullException">Thrown when dialect is null.</exception>
    public SqlBuilder(SqlDialect dialect, int initialCapacity = 1024)
    {
        if (dialect == null)
            throw new ArgumentNullException(nameof(dialect));

        if (initialCapacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Initial capacity must be greater than zero.");

        _dialect = dialect;
        _context = null;
        _buffer = ArrayPool<char>.Shared.Rent(initialCapacity);
        _position = 0;
        _parameters = new Dictionary<string, object?>();
        _disposed = false;
        _built = false;
        _parameterCounter = 0;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlBuilder"/> struct with the specified PlaceholderContext.
    /// </summary>
    /// <param name="context">The placeholder context containing dialect and entity metadata.</param>
    /// <param name="initialCapacity">The initial buffer capacity in characters. Default is 1024.</param>
    /// <exception cref="ArgumentNullException">Thrown when context is null.</exception>
    public SqlBuilder(PlaceholderContext context, int initialCapacity = 1024)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        if (initialCapacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Initial capacity must be greater than zero.");

        _dialect = context.Dialect;
        _context = context;
        _buffer = ArrayPool<char>.Shared.Rent(initialCapacity);
        _position = 0;
        _parameters = new Dictionary<string, object?>();
        _disposed = false;
        _built = false;
        _parameterCounter = 0;
    }

    /// <summary>
    /// Returns the rented buffer to the ArrayPool.
    /// </summary>
    /// <remarks>
    /// This method must be called to ensure proper cleanup of pooled resources.
    /// Use the 'using' statement to ensure automatic disposal.
    /// </remarks>
    public void Dispose()
    {
        if (_disposed)
            return;

        if (_buffer != null)
        {
            ArrayPool<char>.Shared.Return(_buffer);
            _buffer = null!;
        }

        _disposed = true;
    }

    /// <summary>
    /// Ensures the buffer has enough capacity for additional characters.
    /// If needed, rents a larger buffer and copies existing content.
    /// </summary>
    /// <param name="additionalChars">The number of additional characters needed.</param>
    private void EnsureCapacity(int additionalChars)
    {
        var requiredCapacity = _position + additionalChars;
        
        // Buffer has enough capacity
        if (requiredCapacity <= _buffer.Length)
            return;

        // Calculate new capacity (double the current size or required capacity, whichever is larger)
        var newCapacity = Math.Max(_buffer.Length * 2, requiredCapacity);
        
        // Rent new buffer
        var newBuffer = ArrayPool<char>.Shared.Rent(newCapacity);
        
        // Copy existing content
        if (_position > 0)
        {
            _buffer.AsSpan(0, _position).CopyTo(newBuffer);
        }
        
        // Return old buffer
        ArrayPool<char>.Shared.Return(_buffer);
        
        // Update buffer reference
        _buffer = newBuffer;
    }

    /// <summary>
    /// Throws ObjectDisposedException if the builder has been disposed.
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SqlBuilder));
    }

    /// <summary>
    /// Appends a literal string to the buffer (internal method for InterpolatedStringHandler).
    /// </summary>
    /// <param name="value">The literal string to append.</param>
    internal void AppendLiteralInternal(string value)
    {
        ThrowIfDisposed();

        if (string.IsNullOrEmpty(value))
            return;

        EnsureCapacity(value.Length);
        value.AsSpan().CopyTo(_buffer.AsSpan(_position));
        _position += value.Length;
    }

    /// <summary>
    /// Appends a parameter placeholder and adds the value to the parameters dictionary (internal method for InterpolatedStringHandler).
    /// </summary>
    /// <typeparam name="T">The type of the parameter value.</typeparam>
    /// <param name="value">The parameter value.</param>
    internal void AppendParameterInternal<T>(T value)
    {
        ThrowIfDisposed();

        // Generate unique parameter name
        var paramName = $"p{_parameterCounter++}";
        
        // Add parameter to dictionary
        _parameters[paramName] = value;
        
        // Append parameter placeholder using dialect
        var placeholder = _dialect.CreateParameter(paramName);
        EnsureCapacity(placeholder.Length);
        placeholder.AsSpan().CopyTo(_buffer.AsSpan(_position));
        _position += placeholder.Length;
    }

#if NET6_0_OR_GREATER
    /// <summary>
    /// Appends an interpolated string with automatic parameterization.
    /// </summary>
    /// <param name="handler">The interpolated string handler (populated by the compiler).</param>
    /// <returns>This SqlBuilder instance for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method uses C# interpolated string handler to automatically parameterize all interpolated values,
    /// preventing SQL injection attacks.
    /// </para>
    /// <para>
    /// Example:
    /// <code>
    /// builder.Append($"SELECT * FROM users WHERE id = {userId}");
    /// // Generates: "SELECT * FROM users WHERE id = @p0" with parameters { "p0": userId }
    /// </code>
    /// </para>
    /// </remarks>
    public SqlBuilder Append([InterpolatedStringHandlerArgument("")] SqlInterpolatedStringHandler handler)
    {
        // Handler is already populated by the compiler, just return this for chaining
        return this;
    }
#else
    /// <summary>
    /// Appends a FormattableString with automatic parameterization.
    /// </summary>
    /// <param name="formattable">The formattable string containing SQL and parameters.</param>
    /// <returns>This SqlBuilder instance for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method automatically parameterizes all interpolated values in the FormattableString,
    /// preventing SQL injection attacks.
    /// </para>
    /// <para>
    /// Example:
    /// <code>
    /// builder.Append($"SELECT * FROM users WHERE id = {userId}");
    /// // Generates: "SELECT * FROM users WHERE id = @p0" with parameters { "p0": userId }
    /// </code>
    /// </para>
    /// </remarks>
    public SqlBuilder Append(FormattableString formattable)
    {
        ThrowIfDisposed();

        if (formattable == null)
            throw new ArgumentNullException(nameof(formattable));

        var format = formattable.Format;
        var args = formattable.GetArguments();
        
        // Parse the format string and replace {0}, {1}, etc. with parameter placeholders
        var lastIndex = 0;
        for (var i = 0; i < args.Length; i++)
        {
            var placeholder = $"{{{i}}}";
            var index = format.IndexOf(placeholder, lastIndex, StringComparison.Ordinal);
            
            if (index >= 0)
            {
                // Append literal text before the placeholder
                if (index > lastIndex)
                {
                    var literal = format.Substring(lastIndex, index - lastIndex);
                    AppendLiteralInternal(literal);
                }
                
                // Append parameter
                AppendParameterInternal(args[i]);
                
                lastIndex = index + placeholder.Length;
            }
        }
        
        // Append any remaining literal text
        if (lastIndex < format.Length)
        {
            var literal = format.Substring(lastIndex);
            AppendLiteralInternal(literal);
        }

        return this;
    }
#endif

    /// <summary>
    /// Appends raw SQL without any processing.
    /// </summary>
    /// <param name="sql">The raw SQL string to append.</param>
    /// <returns>This SqlBuilder instance for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// WARNING: This method does not provide SQL injection protection.
    /// Only use with trusted, application-controlled SQL.
    /// </para>
    /// <para>
    /// Safe usage:
    /// <code>
    /// var tableName = dialect.WrapColumn("users");
    /// builder.AppendRaw($"SELECT * FROM {tableName}");
    /// </code>
    /// </para>
    /// <para>
    /// UNSAFE usage (DO NOT DO THIS):
    /// <code>
    /// builder.AppendRaw($"SELECT * FROM {userInput}"); // SQL injection risk!
    /// </code>
    /// </para>
    /// </remarks>
    public SqlBuilder AppendRaw(string sql)
    {
        ThrowIfDisposed();

        if (string.IsNullOrWhiteSpace(sql))
            return this;

        EnsureCapacity(sql.Length);
        sql.AsSpan().CopyTo(_buffer.AsSpan(_position));
        _position += sql.Length;

        return this;
    }

    /// <summary>
    /// Builds the final SQL template with the constructed SQL and parameters.
    /// </summary>
    /// <returns>A <see cref="SqlTemplate"/> instance containing the SQL and parameters.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the builder has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when Build() has already been called.</exception>
    /// <remarks>
    /// <para>
    /// This method creates the final SQL string from the buffer and returns it as a SqlTemplate
    /// along with the collected parameters. The builder can only be built once.
    /// </para>
    /// <para>
    /// Example:
    /// <code>
    /// var template = builder.Build();
    /// var users = await connection.QueryAsync(template.Sql, template.Parameters, UserResultReader.Default);
    /// </code>
    /// </para>
    /// </remarks>
    public SqlTemplate Build()
    {
        ThrowIfDisposed();

        if (_built)
            throw new InvalidOperationException("Build() has already been called. SqlBuilder can only be built once.");

        _built = true;

        // Create SQL string from buffer
        var sql = _position == 0 ? string.Empty : new string(_buffer, 0, _position);

        return SqlTemplate.FromBuilder(sql, _parameters);
    }

    /// <summary>
    /// Appends a SqlTemplate string with placeholder processing.
    /// </summary>
    /// <typeparam name="TParameters">The type of the parameters object.</typeparam>
    /// <param name="template">The SQL template string containing placeholders (e.g., {{columns}}, {{table}}).</param>
    /// <param name="parameters">Optional parameters for the template (anonymous object or dictionary).</param>
    /// <returns>This SqlBuilder instance for method chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown when PlaceholderContext is not available.</exception>
    /// <remarks>
    /// <para>
    /// This method integrates with existing SqlTemplate infrastructure to process placeholders
    /// like {{columns}}, {{table}}, {{values}}, {{where}}, etc.
    /// </para>
    /// <para>
    /// Example:
    /// <code>
    /// var context = new PlaceholderContext(SqlDefine.SQLite, "users", UserEntityProvider.Default.Columns);
    /// using var builder = new SqlBuilder(context);
    /// builder.AppendTemplate("SELECT {{columns}} FROM {{table}} WHERE age >= @minAge", new { minAge = 18 });
    /// </code>
    /// </para>
    /// </remarks>
    public SqlBuilder AppendTemplate<
#if NET5_0_OR_GREATER
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
        TParameters>(string template, TParameters? parameters = default)
        where TParameters : class
    {
        ThrowIfDisposed();

        if (_context == null)
            throw new InvalidOperationException("AppendTemplate requires a PlaceholderContext. Use the constructor that accepts PlaceholderContext.");

        if (string.IsNullOrWhiteSpace(template))
            return this;

        // Prepare the template using existing SqlTemplate infrastructure
        var sqlTemplate = SqlTemplate.Prepare(template, _context);

        // Render the template and merge parameters
        string renderedSql;
        if (parameters != null)
        {
            if (parameters is Dictionary<string, object?> dict)
            {
                // Use dictionary directly
                renderedSql = sqlTemplate.HasDynamicPlaceholders ? sqlTemplate.Render(dict) : sqlTemplate.Sql;
                foreach (var kvp in dict)
                {
                    _parameters[kvp.Key] = kvp.Value;
                }
            }
            else if (parameters is IReadOnlyDictionary<string, object?> readOnlyDict)
            {
                // Copy from read-only dictionary directly to _parameters
                foreach (var kvp in readOnlyDict)
                {
                    _parameters[kvp.Key] = kvp.Value;
                }
                renderedSql = sqlTemplate.HasDynamicPlaceholders ? sqlTemplate.Render(_parameters) : sqlTemplate.Sql;
            }
            else
            {
                // Convert anonymous object directly to _parameters using cached expression tree
                ParameterCache<TParameters>.PopulateDictionary(parameters, _parameters);
                renderedSql = sqlTemplate.HasDynamicPlaceholders ? sqlTemplate.Render(_parameters) : sqlTemplate.Sql;
            }
        }
        else
        {
            renderedSql = sqlTemplate.Sql;
        }

        // Append rendered SQL
        EnsureCapacity(renderedSql.Length);
        renderedSql.AsSpan().CopyTo(_buffer.AsSpan(_position));
        _position += renderedSql.Length;

        return this;
    }

    /// <summary>
    /// Appends a SqlTemplate string with placeholder processing (no parameters).
    /// </summary>
    /// <param name="template">The SQL template string containing placeholders (e.g., {{columns}}, {{table}}).</param>
    /// <returns>This SqlBuilder instance for method chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown when PlaceholderContext is not available.</exception>
    public SqlBuilder AppendTemplate(string template)
    {
        return AppendTemplate<object>(template, null);
    }

    /// <summary>
    /// Appends another SqlBuilder as a subquery (wrapped in parentheses).
    /// </summary>
    /// <param name="subquery">The SqlBuilder instance to append as a subquery.</param>
    /// <returns>This SqlBuilder instance for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method builds the subquery, wraps it in parentheses, and merges its parameters
    /// into the parent builder. Parameter name conflicts are automatically resolved by renaming.
    /// </para>
    /// <para>
    /// Example:
    /// <code>
    /// using var subquery = new SqlBuilder(SqlDefine.SQLite);
    /// subquery.Append($"SELECT id FROM orders WHERE total > {1000}");
    /// 
    /// using var mainQuery = new SqlBuilder(SqlDefine.SQLite);
    /// mainQuery.Append($"SELECT * FROM users WHERE id IN ");
    /// mainQuery.AppendSubquery(subquery);
    /// // Generates: "SELECT * FROM users WHERE id IN (SELECT id FROM orders WHERE total > @p0)"
    /// </code>
    /// </para>
    /// </remarks>
    public SqlBuilder AppendSubquery(SqlBuilder subquery)
    {
        ThrowIfDisposed();

        // Build the subquery
        var subqueryTemplate = subquery.Build();
        var subquerySql = subqueryTemplate.Sql;
        var subqueryParams = subqueryTemplate.Parameters;

        if (string.IsNullOrWhiteSpace(subquerySql))
            return this;

        // Append opening parenthesis
        EnsureCapacity(1 + subquerySql.Length + 1);
        _buffer[_position++] = '(';

        // Append subquery SQL
        subquerySql.AsSpan().CopyTo(_buffer.AsSpan(_position));
        _position += subquerySql.Length;

        // Append closing parenthesis
        _buffer[_position++] = ')';

        // Merge subquery parameters, resolving conflicts
        foreach (var kvp in subqueryParams)
        {
            var paramName = kvp.Key;
            
            // Check for parameter name conflict
            if (_parameters.ContainsKey(paramName))
            {
                // Rename the parameter to avoid conflict
                var newParamName = $"p{_parameterCounter++}";
                _parameters[newParamName] = kvp.Value;
                
                // Replace the old parameter name in the buffer with the new one
                // This is a simple approach - in production, you might want a more sophisticated replacement
                var oldPlaceholder = _dialect.CreateParameter(paramName);
                var newPlaceholder = _dialect.CreateParameter(newParamName);
                
                // Find and replace in the recently added subquery portion
                var subqueryStart = _position - subquerySql.Length - 1; // -1 for closing paren
                ReplaceParameterInRange(subqueryStart, _position - 1, oldPlaceholder, newPlaceholder);
            }
            else
            {
                _parameters[paramName] = kvp.Value;
            }
        }

        return this;
    }

    /// <summary>
    /// Replaces a parameter placeholder in a specific range of the buffer.
    /// </summary>
    /// <param name="start">The start position in the buffer.</param>
    /// <param name="end">The end position in the buffer.</param>
    /// <param name="oldPlaceholder">The old parameter placeholder to replace.</param>
    /// <param name="newPlaceholder">The new parameter placeholder.</param>
    private void ReplaceParameterInRange(int start, int end, string oldPlaceholder, string newPlaceholder)
    {
        var searchSpan = _buffer.AsSpan(start, end - start);
        var oldSpan = oldPlaceholder.AsSpan();
        
        var index = searchSpan.IndexOf(oldSpan);
        while (index >= 0)
        {
            var absoluteIndex = start + index;
            
            // Check if we need to grow the buffer
            var lengthDiff = newPlaceholder.Length - oldPlaceholder.Length;
            if (lengthDiff > 0)
            {
                EnsureCapacity(lengthDiff);
                
                // Shift content after the placeholder
                var shiftStart = absoluteIndex + oldPlaceholder.Length;
                var shiftLength = _position - shiftStart;
                if (shiftLength > 0)
                {
                    _buffer.AsSpan(shiftStart, shiftLength).CopyTo(_buffer.AsSpan(shiftStart + lengthDiff));
                }
                
                _position += lengthDiff;
            }
            else if (lengthDiff < 0)
            {
                // Shift content after the placeholder
                var shiftStart = absoluteIndex + oldPlaceholder.Length;
                var shiftLength = _position - shiftStart;
                if (shiftLength > 0)
                {
                    _buffer.AsSpan(shiftStart, shiftLength).CopyTo(_buffer.AsSpan(shiftStart + lengthDiff));
                }
                
                _position += lengthDiff;
            }
            
            // Write the new placeholder
            newPlaceholder.AsSpan().CopyTo(_buffer.AsSpan(absoluteIndex));
            
            // Continue searching after this replacement
            searchSpan = _buffer.AsSpan(absoluteIndex + newPlaceholder.Length, _position - (absoluteIndex + newPlaceholder.Length));
            index = searchSpan.IndexOf(oldSpan);
            if (index >= 0)
            {
                index += absoluteIndex + newPlaceholder.Length - start;
            }
        }
    }

    /// <summary>
    /// Cache for reflection-based parameter conversion.
    /// Uses compiled expression trees for high-performance property access.
    /// </summary>
    private static class ParameterCache<
#if NET5_0_OR_GREATER
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
        T>
    {
        private static readonly Action<T, Dictionary<string, object?>>? Populator;

        static ParameterCache()
        {
            var type = typeof(T);
            var properties = type.GetProperties();
            
            if (properties.Length == 0)
            {
                Populator = null;
                return;
            }

            // Build expression tree: (obj, dict) => { dict["prop1"] = obj.prop1; dict["prop2"] = obj.prop2; ... }
            var objParameter = Expression.Parameter(type, "obj");
            var dictParameter = Expression.Parameter(typeof(Dictionary<string, object?>), "dict");
            
            // Get the indexer property (Item[string])
            var indexer = typeof(Dictionary<string, object?>).GetProperty("Item");
            if (indexer == null)
            {
                Populator = null;
                return;
            }

            // Create: dict["propName"] = (object)obj.propName for each property
            var assignments = new List<Expression>();
            
            foreach (var prop in properties)
            {
                var propAccess = Expression.Property(objParameter, prop);
                var propValue = Expression.Convert(propAccess, typeof(object));
                var propName = Expression.Constant(prop.Name);
                var indexerAccess = Expression.Property(dictParameter, indexer, propName);
                var assignment = Expression.Assign(indexerAccess, propValue);
                assignments.Add(assignment);
            }

            var block = Expression.Block(assignments);
            var lambda = Expression.Lambda<Action<T, Dictionary<string, object?>>>(block, objParameter, dictParameter);
            Populator = lambda.Compile();
        }

        public static void PopulateDictionary(T parameters, Dictionary<string, object?> dictionary)
        {
            Populator?.Invoke(parameters, dictionary);
        }
    }
}
