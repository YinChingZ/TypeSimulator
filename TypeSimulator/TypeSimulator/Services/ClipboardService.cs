using System;
using System.Windows;
using System.Threading;

namespace TypeSimulator.Services
{
    /// <summary>
    /// 提供剪贴板操作功能
    /// </summary>
    public static class ClipboardService
    {
        // 防止剪贴板访问冲突的锁对象
        private static readonly object _clipboardLock = new object();
        // 剪贴板操作重试次数和延迟
        private const int MaxRetryAttempts = 5;
        private const int RetryDelayMs = 100;
        
        /// <summary>
        /// 从剪贴板获取文本
        /// </summary>
        public static string GetText()
        {
            lock (_clipboardLock)
            {
                for (int attempt = 1; attempt <= MaxRetryAttempts; attempt++)
                {
                    try
                    {
                        if (Clipboard.ContainsText())
                        {
                            return Clipboard.GetText();
                        }
                        return string.Empty;
                    }
                    catch (Exception ex)
                    {
                        // 如果不是最后一次尝试，则等待后重试
                        if (attempt < MaxRetryAttempts)
                        {
                            Console.WriteLine($"剪贴板访问失败 (尝试 {attempt}/{MaxRetryAttempts}): {ex.Message}");
                            Thread.Sleep(RetryDelayMs);
                        }
                        else
                        {
                            // 最后一次尝试失败，报告错误
                            Console.WriteLine($"无法访问剪贴板: {ex.Message}");
                            return string.Empty;
                        }
                    }
                }
            }
            
            return string.Empty; // 所有尝试都失败
        }
        
        /// <summary>
        /// 将文本写入剪贴板
        /// </summary>
        public static bool SetText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }
            
            lock (_clipboardLock)
            {
                for (int attempt = 1; attempt <= MaxRetryAttempts; attempt++)
                {
                    try
                    {
                        Clipboard.SetText(text);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        // 如果不是最后一次尝试，则等待后重试
                        if (attempt < MaxRetryAttempts)
                        {
                            Console.WriteLine($"剪贴板写入失败 (尝试 {attempt}/{MaxRetryAttempts}): {ex.Message}");
                            Thread.Sleep(RetryDelayMs);
                        }
                        else
                        {
                            // 最后一次尝试失败，报告错误
                            Console.WriteLine($"无法写入剪贴板: {ex.Message}");
                            return false;
                        }
                    }
                }
            }
            
            return false; // 所有尝试都失败
        }
    }
}