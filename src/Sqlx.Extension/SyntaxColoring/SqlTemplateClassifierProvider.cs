using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace Sqlx.Extension.SyntaxColoring
{
    /// <summary>
    /// Provider for SqlTemplate classifier
    /// </summary>
    [Export(typeof(IClassifierProvider))]
    [ContentType("CSharp")]
    internal class SqlTemplateClassifierProvider : IClassifierProvider
    {
        [Import]
        internal IClassificationTypeRegistryService ClassificationRegistry { get; set; }

        public IClassifier GetClassifier(ITextBuffer textBuffer)
        {
            return textBuffer.Properties.GetOrCreateSingletonProperty(
                () => new SqlTemplateClassifier(ClassificationRegistry));
        }
    }
}

