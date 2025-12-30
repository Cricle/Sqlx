// -----------------------------------------------------------------------
// <copyright file="SqlxCompletionCommandHandler.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;

namespace Sqlx.Extension.IntelliSense
{
    /// <summary>
    /// Command handler to trigger IntelliSense for SqlTemplate attributes.
    /// </summary>
    [Export(typeof(IVsTextViewCreationListener))]
    [Name("SqlxCompletionHandler")]
    [ContentType("CSharp")]
    [TextViewRole(PredefinedTextViewRoles.Editable)]
    internal class SqlxCompletionHandlerProvider : IVsTextViewCreationListener
    {
        [Import]
        internal IVsEditorAdaptersFactoryService AdapterService { get; set; }

        [Import]
        internal ICompletionBroker CompletionBroker { get; set; }

        [Import]
        internal SVsServiceProvider ServiceProvider { get; set; }

        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            ITextView textView = AdapterService.GetWpfTextView(textViewAdapter);
            if (textView == null)
                return;

            Func<SqlxCompletionCommandHandler> createCommandHandler = delegate()
            {
                return new SqlxCompletionCommandHandler(textViewAdapter, textView, this);
            };

            textView.Properties.GetOrCreateSingletonProperty(createCommandHandler);
        }
    }

    /// <summary>
    /// Command handler for completion.
    /// </summary>
    internal class SqlxCompletionCommandHandler : IOleCommandTarget
    {
        private IOleCommandTarget _nextCommandHandler;
        private ITextView _textView;
        private SqlxCompletionHandlerProvider _provider;
        private ICompletionSession _session;

        internal SqlxCompletionCommandHandler(IVsTextView textViewAdapter, ITextView textView, SqlxCompletionHandlerProvider provider)
        {
            _textView = textView;
            _provider = provider;

            // Add the command to the command chain
            textViewAdapter.AddCommandFilter(this, out _nextCommandHandler);
        }

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            return _nextCommandHandler.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (VsShellUtilities.IsInAutomationFunction(_provider.ServiceProvider))
            {
                return _nextCommandHandler.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
            }

            // Make a copy of the GUID
            uint commandID = nCmdID;
            char typedChar = char.MinValue;

            // Check for typing
            if (pguidCmdGroup == VSConstants.VSStd2K && nCmdID == (uint)VSConstants.VSStd2KCmdID.TYPECHAR)
            {
                typedChar = (char)(ushort)Marshal.GetObjectForNativeVariant(pvaIn);
            }

            // Check for completion trigger characters
            if (typedChar == '{' || typedChar == '@' || typedChar == ' ')
            {
                // Pass the command to the next handler first
                int retVal = _nextCommandHandler.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);

                // Check if we should trigger completion
                if (ShouldTriggerCompletion(typedChar))
                {
                    if (_session == null || _session.IsDismissed)
                    {
                        TriggerCompletion();
                    }
                    else
                    {
                        _session.Filter();
                    }
                }

                return retVal;
            }
            // Handle Ctrl+Space (IntelliSense trigger)
            else if (pguidCmdGroup == VSConstants.VSStd2K && nCmdID == (uint)VSConstants.VSStd2KCmdID.COMPLETEWORD)
            {
                if (_session == null || _session.IsDismissed)
                {
                    TriggerCompletion();
                    return VSConstants.S_OK;
                }
            }
            // Handle Tab/Enter to commit completion
            else if (pguidCmdGroup == VSConstants.VSStd2K &&
                     (nCmdID == (uint)VSConstants.VSStd2KCmdID.RETURN ||
                      nCmdID == (uint)VSConstants.VSStd2KCmdID.TAB))
            {
                if (_session != null && !_session.IsDismissed)
                {
                    if (_session.SelectedCompletionSet.SelectionStatus.IsSelected)
                    {
                        _session.Commit();
                        return VSConstants.S_OK;
                    }
                    else
                    {
                        _session.Dismiss();
                    }
                }
            }
            // Handle Escape to dismiss completion
            else if (pguidCmdGroup == VSConstants.VSStd2K && nCmdID == (uint)VSConstants.VSStd2KCmdID.CANCEL)
            {
                if (_session != null && !_session.IsDismissed)
                {
                    _session.Dismiss();
                    return VSConstants.S_OK;
                }
            }

            return _nextCommandHandler.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
        }

        private bool ShouldTriggerCompletion(char typedChar)
        {
            // Get the current line
            var caretPosition = _textView.Caret.Position.BufferPosition;
            var line = caretPosition.GetContainingLine();
            var lineText = line.GetText();

            // Check if we're in a SqlTemplate attribute
            if (!lineText.Contains("[SqlTemplate") && !lineText.Contains("SqlTemplate("))
                return false;

            // Trigger on {{ for placeholders
            if (typedChar == '{')
            {
                var position = caretPosition.Position - line.Start.Position;
                if (position >= 2 && lineText.Substring(position - 2, 2) == "{{")
                {
                    return true;
                }
            }

            // Trigger on @ for parameters
            if (typedChar == '@')
            {
                return true;
            }

            // Trigger on space after placeholder or keyword
            if (typedChar == ' ')
            {
                var position = caretPosition.Position - line.Start.Position;
                if (position > 0)
                {
                    var textBefore = lineText.Substring(0, position);
                    // After placeholder name
                    if (textBefore.Contains("{{") && !textBefore.TrimEnd().EndsWith("}}"))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void TriggerCompletion()
        {
            var caretPosition = _textView.Caret.Position.BufferPosition;
            var snapshot = caretPosition.Snapshot;

            if (_provider.CompletionBroker.IsCompletionActive(_textView))
            {
                return;
            }

            _session = _provider.CompletionBroker.CreateCompletionSession(
                _textView,
                snapshot.CreateTrackingPoint(caretPosition, PointTrackingMode.Positive),
                true);

            _session.Dismissed += OnSessionDismissed;
            _session.Start();
        }

        private void OnSessionDismissed(object sender, EventArgs e)
        {
            if (_session != null)
            {
                _session.Dismissed -= OnSessionDismissed;
                _session = null;
            }
        }
    }
}

