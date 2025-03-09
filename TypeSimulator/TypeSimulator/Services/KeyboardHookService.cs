using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.ComponentModel;
using System.Windows.Interop;

namespace TypeSimulator.Services
{
    /// <summary>
    /// 提供全局键盘钩子功能
    /// </summary>
    public class KeyboardHookService : IDisposable
    {
        // Win32 API常量和结构体
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        // 钩子实例和委托
        private IntPtr _hookID = IntPtr.Zero;
        private readonly LowLevelKeyboardProc _proc;
        private bool _isDisposed = false;

        // 临时禁用标志
        private bool _isTemporarilyDisabled = false;

        // 新增: 是否阻止原始按键标志
        private bool _blockOriginalKeypress = false;

        // 事件
        public event EventHandler<Keys> KeyDown;

        // 添加这一行，定义_source变量
        private HwndSource _source;

        public KeyboardHookService()
        {
            _proc = HookCallback;

            try
            {
                _hookID = SetHook(_proc);

                if (_hookID == IntPtr.Zero)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(),
                        "键盘钩子设置失败。请尝试以管理员身份运行程序。");
                }

                // 注册键盘模拟器的事件
                Utilities.KeyboardSimulator.SimulationStarting += (s, e) => _isTemporarilyDisabled = true;
                Utilities.KeyboardSimulator.SimulationCompleted += (s, e) => _isTemporarilyDisabled = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"键盘钩子初始化失败: {ex.Message}");
                throw; // 重新抛出异常，因为没有钩子程序将无法正常工作
            }
        }

        // 新增: 设置是否阻止原始按键
        public void SetBlockOriginalKeypress(bool block)
        {
            _blockOriginalKeypress = block;
        }

        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                IntPtr hookId = SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);

                if (hookId == IntPtr.Zero)
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    Console.WriteLine($"SetWindowsHookEx 失败，错误码: {errorCode}");
                }

                return hookId;
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            try
            {
                // 如果钩子临时禁用，直接传递给下一个钩子
                if (_isTemporarilyDisabled)
                {
                    return CallNextHookEx(_hookID, nCode, wParam, lParam);
                }

                if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
                {
                    int vkCode = Marshal.ReadInt32(lParam);
                    OnKeyDown((Keys)vkCode);

                    // 修改: 如果启用了阻止原始按键，返回1以阻止按键传播
                    if (_blockOriginalKeypress)
                    {
                        return (IntPtr)1;  // 返回非零值阻止按键事件传递
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"键盘钩子回调错误: {ex.Message}");
                // 即使出错也继续处理链中的下一个钩子
            }

            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        // 添加这个方法，处理窗口消息
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // 这个方法在当前实现中并不需要实际处理什么，但需要存在以避免编译错误
            return IntPtr.Zero;
        }

        protected virtual void OnKeyDown(Keys key)
        {
            try
            {
                KeyDown?.Invoke(this, key);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"KeyDown事件处理出错: {ex.Message}");
                // 继续处理，不中断钩子链
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    // 取消订阅事件
                    Utilities.KeyboardSimulator.SimulationStarting -= (s, e) => _isTemporarilyDisabled = true;
                    Utilities.KeyboardSimulator.SimulationCompleted -= (s, e) => _isTemporarilyDisabled = false;

                    // 释放托管资源
                    // 修改此部分，只有在_source不为null时才移除钩子
                    if (_source != null)
                    {
                        _source.RemoveHook(WndProc);
                        _source = null;
                    }
                }

                // 注销所有热键
                if (_hookID != IntPtr.Zero)
                {
                    try
                    {
                        UnhookWindowsHookEx(_hookID);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"注销键盘钩子失败: {ex.Message}");
                    }
                    finally
                    {
                        _hookID = IntPtr.Zero;
                    }
                }

                _isDisposed = true;
            }
        }

        ~KeyboardHookService()
        {
            Dispose(false);
        }
    }
}