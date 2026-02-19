// <copyright file="OutputParameter.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx;

using System.Data;

/// <summary>
/// Wrapper class for output parameters in async methods.
/// Since C# doesn't allow ref/out parameters in async methods,
/// this class provides a way to pass and retrieve output parameter values.
/// </summary>
/// <typeparam name="T">The type of the output parameter value.</typeparam>
public sealed class OutputParameter<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OutputParameter{T}"/> class.
    /// </summary>
    public OutputParameter()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OutputParameter{T}"/> class with an initial value.
    /// Used for InputOutput parameters (ref behavior).
    /// </summary>
    /// <param name="initialValue">The initial value.</param>
    public OutputParameter(T initialValue)
    {
        Value = initialValue;
        HasValue = true;
    }

    /// <summary>
    /// Gets or sets the parameter value.
    /// </summary>
    public T? Value { get; set; }

    /// <summary>
    /// Gets a value indicating whether the parameter has been set.
    /// </summary>
    public bool HasValue { get; internal set; }

    /// <summary>
    /// Implicitly converts the wrapper to its value.
    /// </summary>
    /// <param name="parameter">The output parameter wrapper.</param>
    public static implicit operator T?(OutputParameter<T> parameter) => parameter.Value;

    /// <summary>
    /// Creates an output parameter with an initial value (for InputOutput mode).
    /// </summary>
    /// <param name="initialValue">The initial value.</param>
    /// <returns>An output parameter with the initial value.</returns>
    public static OutputParameter<T> WithValue(T initialValue) => new(initialValue);
}
