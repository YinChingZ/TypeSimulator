using System;
using System.Windows;
using System.Threading;
using System.Security.Principal;
using System.Diagnostics;

namespace TypeSimulator
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            CheckAdminRights();
        }

        /// <summary>
        /// 检查是否以管理员权限运行，若不是则提示用户
        /// </summary>
        private void CheckAdminRights()
        {
            bool isAdmin = new WindowsPrincipal(WindowsIdentity.GetCurrent())
                .IsInRole(WindowsBuiltInRole.Administrator);

            if (!isAdmin)
            {
                MessageBox.Show(
                    "本程序需要管理员权限才能正常使用键盘钩子功能。\n" +
                    "请右键点击程序，选择「以管理员身份运行」。",
                    "权限不足",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// 处理未捕获的异常
        /// </summary>
        private void Application_DispatcherUnhandledException(object sender,
            System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            // 记录异常信息
            string errorMessage = $"发生未处理的异常：{e.Exception.Message}\n{e.Exception.StackTrace}";
            try
            {
                string logPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "TypeSimulator",
                    "error.log");

                // 确保目录存在
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(logPath));

                // 追加错误日志
                System.IO.File.AppendAllText(
                    logPath,
                    $"[{DateTime.Now}] {errorMessage}\n\n");
            }
            catch
            {
                // 无法写入日志，忽略
            }

            // 显示错误消息
            MessageBox.Show(
                $"程序遇到错误，即将关闭。\n\n错误详情：{e.Exception.Message}",
                "程序错误",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            // 标记异常已处理，防止程序崩溃
            e.Handled = true;

            // 关闭应用程序
            Current.Shutdown();
        }
    }
}