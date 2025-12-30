// -----------------------------------------------------------------------
// <copyright file="SqlxCompletionSourceProvider.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;

namespace Sqlx.Extension.IntelliSense
{
    /// <summary>
    /// Provides completion sources for SqlTemplate attributes.
    /// </summary>
    [Export(typeof(ICompletionSourceProvider))]
    [ContentType("CSharp")]
    [Name("SqlxCompletion")]
    internal class SqlxCompletionSourceProvider : ICompletionSourceProvider
    {
        [Import]
        internal ITextStructureNavigatorSelectorService NavigatorService { get; set; }

        public ICompletionSource TryCreateCompletionSource(ITextBuffer textBuffer)
        {
            return new SqlxCompletionSource(textBuffer, NavigatorService);
        }
    }
}

