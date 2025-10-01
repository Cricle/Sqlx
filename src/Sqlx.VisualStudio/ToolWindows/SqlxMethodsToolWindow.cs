using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;

namespace Sqlx.VisualStudio.ToolWindows
{
    /// <summary>
    /// Sqlx方法工具窗口
    /// 显示项目中所有使用Sqlx特性的方法列表，支持导航和查看详情
    /// </summary>
    [Guid("4A154C7C-8E2F-4B91-9A3D-5E7F8C2B1D6A")]
    public class SqlxMethodsToolWindow : ToolWindowPane
    {
        /// <summary>
        /// 工具窗口GUID
        /// </summary>
        public const string WindowGuidString = "4A154C7C-8E2F-4B91-9A3D-5E7F8C2B1D6A";

        /// <summary>
        /// 构造函数
        /// </summary>
        public SqlxMethodsToolWindow() : base(null)
        {
            Caption = "Sqlx Methods";

            // 创建工具窗口内容
            Content = new SqlxMethodsToolWindowControl();
        }
    }
}
