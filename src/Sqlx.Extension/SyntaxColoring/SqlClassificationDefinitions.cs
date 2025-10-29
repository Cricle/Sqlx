using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using System.Windows.Media;

namespace Sqlx.Extension.SyntaxColoring
{
    /// <summary>
    /// Classification type definitions for SQL elements
    /// </summary>
    internal static class SqlClassificationDefinitions
    {
        // SQL Keyword Classification
        [Export(typeof(ClassificationTypeDefinition))]
        [Name("SqlKeyword")]
        internal static ClassificationTypeDefinition SqlKeywordType = null;

        // SQL Placeholder Classification
        [Export(typeof(ClassificationTypeDefinition))]
        [Name("SqlPlaceholder")]
        internal static ClassificationTypeDefinition SqlPlaceholderType = null;

        // SQL Parameter Classification
        [Export(typeof(ClassificationTypeDefinition))]
        [Name("SqlParameter")]
        internal static ClassificationTypeDefinition SqlParameterType = null;

        // SQL String Classification
        [Export(typeof(ClassificationTypeDefinition))]
        [Name("SqlString")]
        internal static ClassificationTypeDefinition SqlStringType = null;

        // SQL Comment Classification
        [Export(typeof(ClassificationTypeDefinition))]
        [Name("SqlComment")]
        internal static ClassificationTypeDefinition SqlCommentType = null;
    }

    /// <summary>
    /// Format definition for SQL keywords (Blue)
    /// </summary>
    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "SqlKeyword")]
    [Name("SqlKeyword")]
    [UserVisible(true)]
    [Order(Before = Priority.Default)]
    internal sealed class SqlKeywordFormat : ClassificationFormatDefinition
    {
        public SqlKeywordFormat()
        {
            DisplayName = "SQL Keyword (Sqlx)";
            ForegroundColor = Color.FromRgb(0x56, 0x9C, 0xD6); // VS Blue
            IsBold = false;
        }
    }

    /// <summary>
    /// Format definition for SQL placeholders (Orange)
    /// </summary>
    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "SqlPlaceholder")]
    [Name("SqlPlaceholder")]
    [UserVisible(true)]
    [Order(Before = Priority.Default)]
    internal sealed class SqlPlaceholderFormat : ClassificationFormatDefinition
    {
        public SqlPlaceholderFormat()
        {
            DisplayName = "SQL Placeholder (Sqlx)";
            ForegroundColor = Color.FromRgb(0xCE, 0x91, 0x78); // VS Orange
            IsBold = false;
        }
    }

    /// <summary>
    /// Format definition for SQL parameters (Green)
    /// </summary>
    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "SqlParameter")]
    [Name("SqlParameter")]
    [UserVisible(true)]
    [Order(Before = Priority.Default)]
    internal sealed class SqlParameterFormat : ClassificationFormatDefinition
    {
        public SqlParameterFormat()
        {
            DisplayName = "SQL Parameter (Sqlx)";
            ForegroundColor = Color.FromRgb(0x4E, 0xC9, 0xB0); // VS Teal/Green
            IsBold = false;
        }
    }

    /// <summary>
    /// Format definition for SQL strings (Brown)
    /// </summary>
    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "SqlString")]
    [Name("SqlString")]
    [UserVisible(true)]
    [Order(Before = Priority.Default)]
    internal sealed class SqlStringFormat : ClassificationFormatDefinition
    {
        public SqlStringFormat()
        {
            DisplayName = "SQL String (Sqlx)";
            ForegroundColor = Color.FromRgb(0xD6, 0x9D, 0x85); // VS Brown
            IsBold = false;
        }
    }

    /// <summary>
    /// Format definition for SQL comments (Gray/Green)
    /// </summary>
    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "SqlComment")]
    [Name("SqlComment")]
    [UserVisible(true)]
    [Order(Before = Priority.Default)]
    internal sealed class SqlCommentFormat : ClassificationFormatDefinition
    {
        public SqlCommentFormat()
        {
            DisplayName = "SQL Comment (Sqlx)";
            ForegroundColor = Color.FromRgb(0x6A, 0x99, 0x55); // VS Comment Green
            IsBold = false;
        }
    }
}

