using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using Sqlx.Extension.ToolWindows;
using Task = System.Threading.Tasks.Task;

namespace Sqlx.Extension.Commands
{
    /// <summary>
    /// 显示SQL断点窗口的命令
    /// </summary>
    internal sealed class ShowSqlBreakpointCommand
    {
        public const int CommandId = 0x0108;
        public static readonly Guid CommandSet = new Guid("a1b2c3d4-5e6f-7890-abcd-123456789000");

        private readonly AsyncPackage package;

        private ShowSqlBreakpointCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        public static ShowSqlBreakpointCommand Instance { get; private set; }

        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider => package;

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new ShowSqlBreakpointCommand(package, commandService);
        }

        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var window = package.FindToolWindow(typeof(SqlBreakpointWindow), 0, true);
            if (window?.Frame == null)
            {
                throw new NotSupportedException("Cannot create Sqlx SQL Breakpoint window");
            }

            var windowFrame = (IVsWindowFrame)window.Frame;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }
    }
}

