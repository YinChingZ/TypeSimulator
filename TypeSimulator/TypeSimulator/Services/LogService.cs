using System;
using System.Windows.Controls;
using System.Windows.Threading;
using System.IO;
using System.Text;

namespace TypeSimulator.Services
{
    /// <summary>
    /// 提供日志记录服务
    /// </summary>
    public class LogService
    {
        private readonly TextBox _logTextBox;
        private readonly Dispatcher _dispatcher;
        private readonly StringBuilder _logBuffer;
        private string _logFilePath;
        private bool _enableFileLogging = false;

        public enum LogLevel
        {
            Info,
            Warning,
            Error
        }

        public LogService(TextBox logTextBox)
        {
            _logTextBox = logTextBox ?? throw new ArgumentNullException(nameof(logTextBox));
            _dispatcher = _logTextBox.Dispatcher;
            _logBuffer = new StringBuilder();

            try
            {
                // 设置日志文件路径
                string logDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "TypeSimulator",
                    "Logs");

                // 确保日志目录存在
                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }

                // 创建带时间戳的日志文件名
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                _logFilePath = Path.Combine(logDirectory, $"TypeSimulator_{timestamp}.log");
                _enableFileLogging = true;
            }
            catch (Exception ex)
            {
                // 如果无法创建日志文件，就禁用文件日志
                Console.WriteLine($"无法初始化日志文件: {ex.Message}");
                _enableFileLogging = false;
            }
        }

        public void Log(string message, LogLevel level = LogLevel.Info)
        {
            if (string.IsNullOrEmpty(message))
                return;

            string timestamp = DateTime.Now.ToString("HH:mm:ss");

            // 修改: 使用传统 switch 语句替代 switch 表达式
            string prefix;
            switch (level)
            {
                case LogLevel.Info:
                    prefix = "[信息]";
                    break;
                case LogLevel.Warning:
                    prefix = "[警告]";
                    break;
                case LogLevel.Error:
                    prefix = "[错误]";
                    break;
                default:
                    prefix = "[信息]";
                    break;
            }

            string logEntry = $"[{timestamp}] {prefix} {message}";

            // 添加到内存缓冲区
            lock (_logBuffer)
            {
                _logBuffer.AppendLine(logEntry);
            }

            // 显示在UI上
            try
            {
                _dispatcher.InvokeAsync(() =>
                {
                    try
                    {
                        if (_logTextBox.Text.Length > 0)
                        {
                            _logTextBox.AppendText(Environment.NewLine);
                        }
                        _logTextBox.AppendText(logEntry);
                        _logTextBox.ScrollToEnd();

                        // 如果文本太长，删除早期日志
                        if (_logTextBox.Text.Length > 5000)
                        {
                            int cutIndex = _logTextBox.Text.IndexOf(Environment.NewLine, 1000);
                            if (cutIndex > 0)
                            {
                                _logTextBox.Text = _logTextBox.Text.Substring(cutIndex + Environment.NewLine.Length);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"更新UI日志时出错: {ex.Message}");
                    }
                }, DispatcherPriority.Background);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"调度UI日志更新时出错: {ex.Message}");
            }

            // 写入文件
            if (_enableFileLogging)
            {
                try
                {
                    File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"写入日志文件时出错: {ex.Message}");
                    // 如果文件写入失败，禁用文件日志以避免重复错误
                    _enableFileLogging = false;
                }
            }
        }

        public void UpdateLastLine(string message)
        {
            if (string.IsNullOrEmpty(message))
                return;

            try
            {
                _dispatcher.InvokeAsync(() =>
                {
                    try
                    {
                        int lastLineIndex = _logTextBox.Text.LastIndexOf(Environment.NewLine);
                        if (lastLineIndex >= 0)
                        {
                            _logTextBox.Text = _logTextBox.Text.Substring(0, lastLineIndex) + Environment.NewLine + message;
                        }
                        else
                        {
                            _logTextBox.Text = message;
                        }
                        _logTextBox.ScrollToEnd();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"更新最后一行日志时出错: {ex.Message}");
                    }
                }, DispatcherPriority.Background);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"调度最后一行日志更新时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 将日志导出到文件
        /// </summary>
        public bool ExportLogs(string filePath)
        {
            try
            {
                // 从缓冲区获取所有日志
                string allLogs;
                lock (_logBuffer)
                {
                    allLogs = _logBuffer.ToString();
                }

                // 写入文件
                File.WriteAllText(filePath, allLogs);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"导出日志失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 清除日志缓冲区
        /// </summary>
        public void ClearLogs()
        {
            try
            {
                _dispatcher.InvokeAsync(() =>
                {
                    _logTextBox.Clear();
                }, DispatcherPriority.Background);

                lock (_logBuffer)
                {
                    _logBuffer.Clear();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"清除日志时出错: {ex.Message}");
            }
        }
    }
}