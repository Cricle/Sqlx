// -----------------------------------------------------------------------
// <copyright file="ValidationHelper.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>

#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Sqlx
{
    /// <summary>
    /// Shared DataAnnotations validation helpers used by generated code.
    /// </summary>
    public static class ValidationHelper
    {
        /// <summary>
        /// Validates an object instance using DataAnnotations.
        /// </summary>
        /// <param name="instance">The object instance to validate.</param>
        /// <param name="paramName">The logical parameter name for error messages.</param>
        public static void ValidateObject(object instance, string paramName)
        {
            if (instance is null)
            {
                throw new ArgumentNullException(paramName);
            }

            var context = new ValidationContext(instance);
            var results = new List<ValidationResult>();

            if (Validator.TryValidateObject(instance, context, results, validateAllProperties: true))
            {
                return;
            }

            ThrowValidationException(results, paramName);
        }

        /// <summary>
        /// Validates a scalar value using the supplied DataAnnotations validation attributes.
        /// </summary>
        /// <param name="value">The value to validate.</param>
        /// <param name="paramName">The logical parameter name for error messages.</param>
        /// <param name="attributes">The validation attributes to apply.</param>
        public static void ValidateValue(object? value, string paramName, params ValidationAttribute[] attributes)
        {
            if (attributes.Length == 0)
            {
                return;
            }

            var context = new ValidationContext(new object())
            {
                MemberName = paramName,
                DisplayName = paramName,
            };

            var results = new List<ValidationResult>();
            if (Validator.TryValidateValue(value!, context, results, attributes))
            {
                return;
            }

            ThrowValidationException(results, paramName);
        }

        private static void ThrowValidationException(IEnumerable<ValidationResult> results, string paramName)
        {
            var first = results.FirstOrDefault();
            throw new ValidationException(first?.ErrorMessage ?? $"Validation failed for '{paramName}'.");
        }
    }
}
