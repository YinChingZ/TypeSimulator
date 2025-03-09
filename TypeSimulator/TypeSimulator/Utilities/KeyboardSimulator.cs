using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace TypeSimulator.Utilities
{
    /// <summary>
    /// 提供键盘输入模拟功能
    /// </summary>
    public static class KeyboardSimulator
    {
        // Win32 API 导入
        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);

        // 输入类型
        private const int KEYEVENTF_KEYDOWN = 0x0000;
        private const int KEYEVENTF_KEYUP = 0x0002;
        private const int KEYEVENTF_UNICODE = 0x0004;

        // 互斥锁，确保同一时间只发送一个键盘输入
        private static readonly object _lockObject = new object();

        // 新增: 事件系统用于暂时禁用键盘钩子
        public static event EventHandler SimulationStarting;
        public static event EventHandler SimulationCompleted;

        /// <summary>
        /// 模拟键盘按下和释放单个按键
        /// </summary>
        public static void PressKey(byte keyCode)
        {
            lock (_lockObject)
            {
                try
                {
                    // 触发模拟开始事件
                    OnSimulationStarting();

                    keybd_event(keyCode, 0, KEYEVENTF_KEYDOWN, 0);
                    Thread.Sleep(5); // 短暂延迟
                    keybd_event(keyCode, 0, KEYEVENTF_KEYUP, 0);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"按键模拟失败: {ex.Message}");
                }
                finally
                {
                    // 触发模拟完成事件
                    OnSimulationCompleted();
                }
            }
        }

        /// <summary>
        /// 模拟键入单个字符
        /// </summary>
        public static void TypeCharacter(char character)
        {
            lock (_lockObject)
            {
                try
                {
                    // 触发模拟开始事件
                    OnSimulationStarting();

                    // 使用SendKeys实现单个字符输入
                    // SendKeys可能在某些应用程序中不工作，但它是最通用的解决方案
                    string keyString;

                    // 处理特殊字符
                    switch (character)
                    {
                        case '{': keyString = @"{{}}"; break;
                        case '}': keyString = @"{}}"; break;
                        case '(': keyString = @"{(}"; break;
                        case ')': keyString = @"{)}"; break;
                        case '[': keyString = @"{[}"; break;
                        case ']': keyString = @"{]}"; break;
                        case '+': keyString = @"{+}"; break;
                        case '^': keyString = @"{^}"; break;
                        case '%': keyString = @"{%}"; break;
                        case '~': keyString = @"{~}"; break;
                        case '\n': keyString = @"{ENTER}"; break;
                        case '\t': keyString = @"{TAB}"; break;
                        default: keyString = character.ToString(); break;
                    }

                    System.Windows.Forms.SendKeys.SendWait(keyString);

                    // 如果字符是大写，可能需要按住Shift键，这是SendKeys自动处理的
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"字符输入失败 '{character}': {ex.Message}");

                    // 尝试备用方法
                    try
                    {
                        System.Windows.Forms.SendKeys.Send(character.ToString());
                    }
                    catch
                    {
                        // 忽略备用方法的错误
                    }
                }
                finally
                {
                    // 触发模拟完成事件
                    OnSimulationCompleted();
                }
            }
        }

        /// <summary>
        /// 模拟键入一段文本
        /// </summary>
        public static void TypeText(string text, int delayBetweenCharsMs = 0)
        {
            if (string.IsNullOrEmpty(text))
                return;

            lock (_lockObject)
            {
                try
                {
                    // 触发模拟开始事件
                    OnSimulationStarting();

                    foreach (char c in text)
                    {
                        string keyString;

                        // 处理特殊字符
                        switch (c)
                        {
                            case '{': keyString = @"{{}}"; break;
                            case '}': keyString = @"{}}"; break;
                            case '(': keyString = @"{(}"; break;
                            case ')': keyString = @"{)}"; break;
                            case '[': keyString = @"{[}"; break;
                            case ']': keyString = @"{]}"; break;
                            case '+': keyString = @"{+}"; break;
                            case '^': keyString = @"{^}"; break;
                            case '%': keyString = @"{%}"; break;
                            case '~': keyString = @"{~}"; break;
                            case '\n': keyString = @"{ENTER}"; break;
                            case '\t': keyString = @"{TAB}"; break;
                            default: keyString = c.ToString(); break;
                        }

                        System.Windows.Forms.SendKeys.SendWait(keyString);

                        if (delayBetweenCharsMs > 0)
                        {
                            Thread.Sleep(delayBetweenCharsMs);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"文本输入失败: {ex.Message}");
                }
                finally
                {
                    // 触发模拟完成事件
                    OnSimulationCompleted();
                }
            }
        }

        private static void OnSimulationStarting()
        {
            SimulationStarting?.Invoke(null, EventArgs.Empty);
        }

        private static void OnSimulationCompleted()
        {
            SimulationCompleted?.Invoke(null, EventArgs.Empty);
        }
    }
}