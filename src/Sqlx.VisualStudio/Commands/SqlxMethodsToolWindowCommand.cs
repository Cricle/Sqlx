using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Sqlx.VisualStudio.ToolWindows;
using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace Sqlx.VisualStudio.Commands
{
    /// <summary>
    /// 打开Sqlx方法工具窗口的命令
    /// </summary>
    internal sealed class SqlxMethodsToolWindowCommand
    {
        /// <summary>
        /// 命令ID
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// 命令集GUID
        /// </summary>
        public static readonly Guid CommandSet = new Guid("1B2C3D4E-5F6A-7B8C-9D0E-1F2A3B4C5D6E");

        /// <summary>
        /// VS包实例
        /// </summary>
        private readonly AsyncPackage _package;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="package">包实例</param>
        /// <param name="commandService">命令服务</param>
        private SqlxMethodsToolWindowCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandId = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(Execute, menuCommandId);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// 获取命令实例
        /// </summary>
        public static SqlxMethodsToolWindowCommand Instance { get; private set; }

        /// <summary>
        /// 获取服务提供器
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider => _package;

        /// <summary>
        /// 初始化单例实例
        /// </summary>
        /// <param name="package">包实例</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new SqlxMethodsToolWindowCommand(package, commandService);
        }

        /// <summary>
        /// 显示工具窗口
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        private void Execute(object sender, EventArgs e)
        {
            _ = ExecuteAsync();
        }

        /// <summary>
        /// 异步显示工具窗口
        /// </summary>
        private async Task ExecuteAsync()
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                ToolWindowPane window = await _package.ShowToolWindowAsync(
                    typeof(SqlxMethodsToolWindow),
                    0,
                    true,
                    _package.DisposalToken);

                if (window?.Frame == null)
                {
                    throw new NotSupportedException("Cannot create tool window");
                }

                IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
                ErrorHandler.ThrowOnFailure(windowFrame.Show());
            }
            catch (Exception ex)
            {
                // 显示错误消息
                await ShowErrorAsync($"打开Sqlx方法窗口时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示错误消息
        /// </summary>
        private async Task ShowErrorAsync(string message)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            VsShellUtilities.ShowMessageBox(
                _package,
                message,
                "Sqlx Extension Error",
                OLEMSGICON.OLEMSGICON_CRITICAL,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }
}
