// -----------------------------------------------------------------------
// <copyright file="Messages.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx
{
    using Microsoft.CodeAnalysis;

    internal static class Messages
    {
        public static DiagnosticDescriptor SP0001 { get; } = new DiagnosticDescriptor("SP0001", "No stored procedure attribute", "Internal analyzer error.", "Internal", DiagnosticSeverity.Error, true);

        public static DiagnosticDescriptor SP0002 { get; } = new DiagnosticDescriptor("SP0002", "Invalid method signature", "Method signature is not supported for code generation.", "Sqlx", DiagnosticSeverity.Error, true);

        public static DiagnosticDescriptor SP0003 { get; } = new DiagnosticDescriptor("SP0003", "Id property cannot be guessed", "Cannot find id property for entity type {0}.", "Sqlx", DiagnosticSeverity.Error, true);

        public static DiagnosticDescriptor SP0004 { get; } = new DiagnosticDescriptor("SP0004", "Entity property corresponding to parameter cannot be guessed", "Cannot find property in entity type {0} corresponding to parameter {1}.", "Sqlx", DiagnosticSeverity.Error, true);

        public static DiagnosticDescriptor SP0005 { get; } = new DiagnosticDescriptor("SP0005", "Unknown method for generation", "Unknown method {0} for generation.", "Sqlx", DiagnosticSeverity.Error, true);

        public static DiagnosticDescriptor SP0006 { get; } = new DiagnosticDescriptor("SP0006", "No such DbConnection or DbContext field, property or paramter", "No connection", "Sqlx", DiagnosticSeverity.Error, true);

        public static DiagnosticDescriptor SP0007 { get; } = new DiagnosticDescriptor("SP0007", "No RawSqlAttribute or SqlxAttribute tag", "No command text", "Sqlx", DiagnosticSeverity.Error, true);

        public static DiagnosticDescriptor SP0008 { get; } = new DiagnosticDescriptor("SP0008", "Execute no query return must be int or Task<int>", "Return type error", "Sqlx", DiagnosticSeverity.Error, true);

        public static DiagnosticDescriptor SP0009 { get; } = new DiagnosticDescriptor("SP0009", "Tuple dbcontext must has DbSetTypeAttribute tag", "No dbset", "Sqlx", DiagnosticSeverity.Error, true);
    }
}
