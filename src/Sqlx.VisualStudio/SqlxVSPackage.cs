using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using Task = System.Threading.Tasks.Task;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;
using Sqlx.VisualStudio.ToolWindows;
using Sqlx.VisualStudio.Commands;

namespace Sqlx.VisualStudio
{
    /// <summary>
    /// Sqlx Visual Studio Package - 主扩展包
    /// 提供SQL悬浮提示、方法详情窗口、智能导航等功能
    /// </summary>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(SqlxVSPackage.PackageGuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideToolWindow(typeof(SqlxMethodsToolWindow),
        Style = VsDockStyle.Tabbed,
        Window = "3ae79031-e1fa-4b8d-907f-fe0953668891", // Output Window GUID
        Transient = true)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExistsAndFullyLoaded_string, PackageAutoLoadFlags.BackgroundLoad)]
    public sealed class SqlxVSPackage : AsyncPackage
    {
        /// <summary>
        /// SqlxVSPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "E7C44F2A-3D5B-4F8E-9A1C-2E6F7B8A9C3D";

        #region Package Members

        /// <summary>
        /// 包初始化 - 在包被载入后调用
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <param name="progress">进度报告器</param>
        /// <returns>初始化任务</returns>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            // 基类初始化
            await base.InitializeAsync(cancellationToken, progress);

            // 切换到主线程进行UI操作
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            // 初始化命令
            await SqlxMethodsToolWindowCommand.InitializeAsync(this);

            // 输出初始化信息
            await WriteToOutputAsync("Sqlx Visual Studio Extension 已成功初始化");
        }

        /// <summary>
        /// 写入输出窗口
        /// </summary>
        private async Task WriteToOutputAsync(string message)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            try
            {
                var dte = await GetServiceAsync(typeof(SDTE)) as SDTE;
                if (dte?.DTE?.ToolWindows?.OutputWindow != null)
                {
                    var outputWindow = dte.DTE.ToolWindows.OutputWindow;
                    var pane = outputWindow.OutputWindowPanes.Add("Sqlx Extension");
                    pane.OutputString($"[{DateTime.Now:HH:mm:ss}] {message}\n");
                }
            }
            catch
            {
                // 静默处理输出窗口异常
            }
        }

        /// <summary>
        /// 包销毁
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // 清理托管资源
            }
            base.Dispose(disposing);
        }

        #endregion
    }
}
