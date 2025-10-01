using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using Task = System.Threading.Tasks.Task;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;

namespace Sqlx.VisualStudio
{
    /// <summary>
    /// Sqlx Visual Studio Package - 独立版本
    /// 提供SQL悬浮提示功能
    /// </summary>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(SqlxVSPackage.PackageGuidString)]
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

            // Sqlx QuickInfo功能会通过MEF自动注册，无需额外初始化代码
            // 这是一个极简版本的VS插件，专注于SQL悬浮提示功能
        }

        #endregion
    }
}
